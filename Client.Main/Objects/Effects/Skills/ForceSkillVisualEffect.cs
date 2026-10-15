#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(60)]
[SkillVisualEffect(66)]
public sealed class ForceSkillVisualEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) =>
        context.Caster == null || context.World == null ? null : new ForceSkillEffect(context.Caster);
}
