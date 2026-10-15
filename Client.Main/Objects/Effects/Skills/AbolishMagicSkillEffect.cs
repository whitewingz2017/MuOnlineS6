#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(72)]
public sealed class AbolishMagicSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) =>
        context.Caster == null || context.World == null ? null : new AbolishMagicEffect(context.Caster);
}
