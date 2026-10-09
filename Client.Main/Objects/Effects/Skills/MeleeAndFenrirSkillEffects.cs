#nullable enable
using Client.Main.Core.Utilities;
namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(44)]
    public sealed class CrescentMoonSlashSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context) =>
            context.Caster == null || context.World == null ? null : new CrescentMoonSlashEffect(context.Caster);
    }

    [SkillVisualEffect(47)]
    public sealed class ImpaleSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context) =>
            context.Caster == null || context.World == null ? null : new ImpaleEffect(context.Caster);
    }

    [SkillVisualEffect(232)]
    [SkillVisualEffect(337)]
    public sealed class StrikeOfDestructionSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context) =>
            context.Caster == null || context.World == null ? null : new StrikeOfDestructionEffect(context.Caster, context.TargetPosition);
    }

    [SkillVisualEffect(76)]
    public sealed class PlasmaStormSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;
            WalkerObject? target = null;
            if (context.TargetId != 0)
                context.World.TryGetWalkerById(context.TargetId, out target);
            return new PlasmaStormEffect(context.Caster, target, context.TargetPosition);
        }
    }
}
