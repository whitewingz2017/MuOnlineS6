#nullable enable
using Client.Main.Core.Utilities;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(48)]
    [SkillVisualEffect(356)]
    [SkillVisualEffect(360)]
    [SkillVisualEffect(363)]
    public sealed class GreaterFortitudeSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context) =>
            context.Caster == null || context.World == null
                ? null
                : new GreaterFortitudeEffect(context.Caster);
    }
}
