#nullable enable
using Client.Main.Core.Utilities;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Factory for Dark Lord's Dark Horse Earthquake/Earth Shake visual sequence.
    /// Season 6 master variants are resolved by the registry replacement map.
    /// </summary>
    [SkillVisualEffect(62)]
    public sealed class DarkHorseEarthquakeSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            return new DarkHorseEarthquakeEffect(context.Caster);
        }
    }
}
