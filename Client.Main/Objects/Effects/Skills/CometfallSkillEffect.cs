#nullable enable
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(13)]
public sealed class CometfallSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context)
    {
        if (context.World == null || context.Caster == null) return null;
        Vector3 target = context.TargetPosition ?? context.Caster.WorldPosition.Translation;
        if (context.TargetPosition == null && context.TargetId != 0 && context.World.TryGetWalkerById(context.TargetId, out var victim))
            target = victim.WorldPosition.Translation;
        return new CometfallEffect(target);
    }
}
