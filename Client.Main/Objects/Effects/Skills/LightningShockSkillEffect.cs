#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;
[SkillVisualEffect(230)]
public sealed class LightningShockSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) => context.Caster == null || context.World == null ? null : new LightningShockEffect(context.Caster);
}

