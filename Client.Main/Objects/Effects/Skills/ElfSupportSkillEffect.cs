#nullable enable
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(26)]
    [SkillVisualEffect(27)]
    [SkillVisualEffect(28)]
    [SkillVisualEffect(413)]
    [SkillVisualEffect(417)]
    [SkillVisualEffect(420)]
    [SkillVisualEffect(422)]
    [SkillVisualEffect(423)]
    public sealed class ElfSupportSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            WalkerObject target = context.Caster;
            if (context.TargetId != 0 && context.World.TryGetWalkerById(context.TargetId, out var selected))
                target = selected;

            int subtype = context.SkillId switch
            {
                26 or 413 => 1,
                27 or 417 or 423 => 2,
                _ => 3,
            };
            return new ElfSkillGlowEffect(target, subtype);
        }
    }
}
