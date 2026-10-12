#nullable enable
using Client.Main.Core.Utilities;

namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(19)]
[SkillVisualEffect(20)]
[SkillVisualEffect(21)]
[SkillVisualEffect(22)]
[SkillVisualEffect(23)]
public sealed class WeaponSkillFlashFactory : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) =>
        context.Caster == null || context.World == null ? null : new WeaponSkillFlashEffect(context.Caster);
}
