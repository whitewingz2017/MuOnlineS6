#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(238)]
public sealed class ChaoticDiseierSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) => context.Caster == null || context.World == null
        ? null : new ChaoticDiseierEffect(context.Caster, context.LaunchYaw,
            context.TargetId != 0 && context.Caster.NetworkId == MuGame.Network?.GetCharacterState()?.Id ? context.TargetPosition : null);
}
