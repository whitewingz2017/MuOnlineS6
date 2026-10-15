#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(219)]
[SkillVisualEffect(221)]
[SkillVisualEffect(222)]
public sealed class SummonerDebuffCastSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context)
    {
        if (context.Caster == null || context.World == null) return null;
        ushort skill = SkillVisualEffectRegistry.GetBaseSkillId(context.SkillId);
        WalkerObject? target = null;
        if (context.TargetId != 0) context.World.TryGetWalkerById((ushort)(context.TargetId & 0x7FFF), out target);
        return new SummonerDebuffCastEffect(context.Caster, target, skill);
    }
}
