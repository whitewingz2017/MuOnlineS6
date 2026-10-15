#nullable enable
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(223)]
[SkillVisualEffect(224)]
[SkillVisualEffect(225)]
public sealed class SummonerBookSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context)
    {
        if(context.World == null || context.Caster == null) return null;
        Vector3? target=context.TargetPosition;
        if(context.TargetId != 0 && context.World.TryGetWalkerById((ushort)(context.TargetId & 0x7FFF),out var victim))
            target=victim.WorldPosition.Translation;
        if(!target.HasValue) return null;
        var position=target.Value;
        position.Z=context.World.Terrain.RequestTerrainHeight(position.X,position.Y);
        return new SummonerBookCastEffect(context.Caster,position,SkillVisualEffectRegistry.GetBaseSkillId(context.SkillId));
    }
}
