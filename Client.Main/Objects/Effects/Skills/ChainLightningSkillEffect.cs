#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(215)]
public sealed class ChainLightningSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) => context.World != null && context.Caster != null && context.TargetId != 0 &&
        context.World.TryGetWalkerById((ushort)(context.TargetId & 0x7FFF), out var target)
        ? new ChainLightningEffect(context.Caster, target, true) : null;
}
