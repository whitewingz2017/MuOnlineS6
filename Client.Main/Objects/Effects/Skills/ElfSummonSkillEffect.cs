#nullable enable
using Client.Main.Core.Utilities;

namespace Client.Main.Objects.Effects.Skills;

// MuMain ZzzCharacter: all seven Elf summon casts finish with
// BITMAP_MAGIC+1 subtype 3 on the caster. Creature spawning stays on
// the server's ordinary object-creation path.
[SkillVisualEffect(30)]
[SkillVisualEffect(31)]
[SkillVisualEffect(32)]
[SkillVisualEffect(33)]
[SkillVisualEffect(34)]
[SkillVisualEffect(35)]
[SkillVisualEffect(36)]
public sealed class ElfSummonSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context) =>
        context.Caster == null || context.World == null
            ? null
            : new ElfSkillGlowEffect(context.Caster, 3);
}
