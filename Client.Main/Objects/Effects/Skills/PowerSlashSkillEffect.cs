#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(56)]
public sealed class PowerSlashSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) =>
        context.Caster == null || context.World == null ? null : new PowerSlashEffect(context.Caster);
}
