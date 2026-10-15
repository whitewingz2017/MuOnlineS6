#nullable enable
using Client.Main.Controls;
using Client.Main.Core.Utilities;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Factory for Lightning skill visual effect (Skill ID 3 - Scroll of Lightning).
    /// Uses MuMain's two subtype-0 thunder ribbons between caster and target.
    /// </summary>
    [SkillVisualEffect(3)] // Lightning / Scroll of Lightning
    public sealed class LightningSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            var caster = context.Caster;
            var world = context.World;
            ushort targetId = context.TargetId;

            // The joint adds MuMain's target height itself.
            Vector3 TargetProvider()
            {
                if (targetId != 0 && world.TryGetWalkerById(targetId, out var target))
                    return target.WorldPosition.Translation;

                return context.TargetPosition ?? caster.WorldPosition.Translation;
            }

            // Note: Don't call Load() here - caller adds to world first, then loads
            return new PlayerLightningEffect(caster, TargetProvider);
        }
    }
}
