#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(65)]
public sealed class ElectricSparkSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) => context.Caster == null || context.World == null
        ? null : new ElectricSparkEffect(context.Caster, context.LaunchYaw);
}
