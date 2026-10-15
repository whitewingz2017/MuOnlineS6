#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(63)]
public sealed class PartySummonSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) =>
        context.Caster == null || context.World == null ? null : new PartySummonEffect(context.Caster);
}
