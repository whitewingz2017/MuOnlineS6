#nullable enable
using Client.Main.Core.Utilities;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Factory for the server-confirmed Blade Knight combo visual effect (Skill ID 59).
    /// </summary>
    [SkillVisualEffect(59)]
    public sealed class ComboSkillEffectFactory : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            return new ComboSkillEffect(context.Caster);
        }
    }
}
