#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;
[SkillVisualEffect(260)]
[SkillVisualEffect(261)]
[SkillVisualEffect(262)]
[SkillVisualEffect(263)]
[SkillVisualEffect(264)]
[SkillVisualEffect(265)]
[SkillVisualEffect(266)]
[SkillVisualEffect(267)]
[SkillVisualEffect(268)]
[SkillVisualEffect(269)]
[SkillVisualEffect(270)]
public sealed class RageFighterSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context)
    {
        if(context.Caster==null||context.World==null)return null;
        WalkerObject? target=null;
        if(context.TargetId!=0)context.World.TryGetWalkerById((ushort)(context.TargetId&0x7FFF),out target);
        return new RageFighterCastEffect(context.Caster,target,SkillVisualEffectRegistry.GetBaseSkillId(context.SkillId),context.TargetPosition);
    }
}
