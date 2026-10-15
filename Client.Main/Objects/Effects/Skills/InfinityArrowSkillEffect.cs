#nullable enable
using Client.Main.Core.Utilities;

namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(77)]
public sealed class InfinityArrowSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) =>
        context.Caster == null || context.World == null ? null : new InfinityArrowEffect(context.Caster);
}
