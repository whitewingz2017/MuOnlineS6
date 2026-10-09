#nullable enable
using Client.Main.Core.Utilities;
using Client.Main.Objects.Player;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(24)]  // Triple Shot
    [SkillVisualEffect(25)]  // Basic bow skill
    [SkillVisualEffect(46)]  // Deep Impact
    [SkillVisualEffect(51)]  // Ice Arrow
    [SkillVisualEffect(52)]  // Penetration
    [SkillVisualEffect(235)] // Multi-Shot
    [SkillVisualEffect(411)] // Multi-Shot Strengthener
    [SkillVisualEffect(414)] // Triple Shot Strengthener
    [SkillVisualEffect(416)] // Penetration Strengthener
    [SkillVisualEffect(418)] // Triple Shot Mastery
    [SkillVisualEffect(431)] // Multi-Shot Mastery
    public sealed class ArrowSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject shooter || context.World == null)
                return null;

            return new ArrowProjectileEffect(
                shooter,
                context.World,
                context.TargetId,
                context.LaunchYaw.HasValue ? null : context.TargetPosition,
                ArrowProjectileSpawner.GetVolleyKind(context.SkillId),
                context.LaunchYaw);
        }
    }
}
