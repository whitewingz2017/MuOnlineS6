#nullable enable
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Factory for Ice Storm; the registry resolves Season 6 master replacements.
    /// </summary>
    [SkillVisualEffect(39)]
    public sealed class IceStormSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            Vector3 center = context.TargetPosition ?? context.Caster.WorldPosition.Translation;
            return new ScrollOfIceStormEffect(context.Caster, center);
        }
    }
}
