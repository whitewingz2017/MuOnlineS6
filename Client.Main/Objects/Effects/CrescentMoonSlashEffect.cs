#nullable enable
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    public sealed class CrescentMoonSlashEffect : StagedCombatEffect
    {
        public CrescentMoonSlashEffect(WalkerObject caster) : base(caster, 30f) { }

        protected override void BuildParts()
        {
            Vector3 start = Origin + Vector3.UnitZ * 100f;
            AddModel("Skill/SwordForce.bmd", start, 5f, 15f, 0f, Vector3.One, Forward * 25f, rush: true);
            for (int i = 0; i < 3; i++)
                AddModel("Skill/SwordForce.bmd", start + Forward * (10f + i * 14f),
                    6f + i, 5f, 3.5f, new Vector3(1f, 0.8f, 0.6f));
        }
    }
}
