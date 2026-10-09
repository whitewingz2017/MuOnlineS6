#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Client.Main.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Registry for skill visual effects. Auto-discovers effect classes decorated with
    /// <see cref="SkillVisualEffectAttribute"/> at startup.
    /// </summary>
    public static class SkillVisualEffectRegistry
    {
        private static readonly Dictionary<ushort, ISkillVisualEffect> _effects = new();
        private static readonly ILogger? _logger;
        private static bool _initialized;
        private static readonly Dictionary<ushort, ushort> _masterReplacements = ServerMasterSkills.All
            .Where(s => s.Replaced != 0).ToDictionary(s => s.Number, s => s.Replaced);

        public static ushort GetBaseSkillId(ushort skillId)
        {
            for (int depth = 0; depth < ServerMasterSkills.All.Count; depth++)
            {
                if (!_masterReplacements.TryGetValue(skillId, out var replaced) || replaced == skillId)
                    break;
                skillId = replaced;
            }
            return skillId;
        }

        static SkillVisualEffectRegistry()
        {
            _logger = MuGame.AppLoggerFactory?.CreateLogger("SkillVisualEffectRegistry");
        }

        /// <summary>
        /// Initializes the registry by discovering all effect classes.
        /// Call this once during game startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            DiscoverEffects();
            // Season 6 master IDs replace base skills; legacy numeric aliases can
            // otherwise select an unrelated visual (e.g. Fire Scream 518 -> Earthquake).
            foreach (var entry in ServerMasterSkills.All)
            {
                if (entry.Replaced == 0)
                {
                    _effects.Remove(entry.Number);
                    continue;
                }
                if (_effects.TryGetValue(GetBaseSkillId(entry.Number), out var factory))
                    _effects[entry.Number] = factory;
                else
                    _effects.Remove(entry.Number);
            }
        }

        /// <summary>
        /// Tries to spawn a visual effect for the given skill.
        /// </summary>
        /// <param name="skillId">The skill ID.</param>
        /// <param name="context">The effect context with caster, target, and world.</param>
        /// <param name="effect">The spawned effect, or null if no effect registered.</param>
        /// <returns>True if an effect was spawned, false otherwise.</returns>
        public static bool TrySpawn(ushort skillId, SkillEffectContext context, out WorldObject? effect)
        {
            if (!_initialized)
                Initialize();

            if (_effects.TryGetValue(skillId, out var factory))
            {
                try
                {
                    effect = factory.CreateEffect(context);
                    if (effect != null)
                    {
                        _logger?.LogDebug("Spawned visual effect for skill {SkillId}: {EffectType}",
                            skillId, effect.GetType().Name);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error creating visual effect for skill {SkillId}", skillId);
                }
            }

            effect = null;
            return false;
        }

        /// <summary>
        /// Checks if a visual effect is registered for the given skill.
        /// </summary>
        public static bool HasEffect(ushort skillId)
        {
            if (!_initialized)
                Initialize();

            return _effects.ContainsKey(skillId);
        }

        /// <summary>
        /// Gets all registered skill IDs with visual effects.
        /// </summary>
        public static IEnumerable<ushort> GetRegisteredSkillIds()
        {
            if (!_initialized)
                Initialize();

            return _effects.Keys;
        }

        private static void DiscoverEffects()
        {
            var assembly = Assembly.GetExecutingAssembly();
            int registered = 0;

            var effectTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(ISkillVisualEffect).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttributes<SkillVisualEffectAttribute>().Any());

            foreach (var type in effectTypes)
            {
                try
                {
                    var instance = (ISkillVisualEffect)Activator.CreateInstance(type)!;
                    var attributes = type.GetCustomAttributes<SkillVisualEffectAttribute>();

                    foreach (var attr in attributes)
                    {
                        if (_effects.TryAdd(attr.SkillId, instance))
                        {
                            registered++;
                            _logger?.LogTrace("Registered skill effect: {SkillId} => {Type}",
                                attr.SkillId, type.Name);
                        }
                        else
                        {
                            _logger?.LogWarning(
                                "Duplicate skill effect for ID {SkillId}: {Type} (already registered)",
                                attr.SkillId, type.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to instantiate skill effect: {Type}", type.Name);
                }
            }

            _logger?.LogInformation("Registered {Count} skill visual effects.", registered);
        }
    }
}
