#nullable enable
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    public sealed class ImpaleEffect : StagedCombatEffect
    {
        public ImpaleEffect(WalkerObject caster) : base(caster, 36f) { }

        protected override void BuildParts()
        {
            Vector3 start = Origin + Forward * 50f + Vector3.UnitZ * 110f;
            AddModel("Item/Spear02.bmd", start, 4f, 5f, 1.2f, new Vector3(1f, 1f, 0.5f));
            for (int i = 0; i < 2; i++)
                AddModel("Item/Spear01.bmd", start, 8f, 10f, 1.2f, Vector3.One,
                    Forward * 100f, yawOffset: i * 0.1f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(MuGame.Random.Next(-30, 31), MuGame.Random.Next(-30, 31), 0f);
                AddModel("Skill/RidingSpear01.bmd", Origin + Forward * 145f + Vector3.UnitZ * 110f + offset,
                    13f, 20f, 1.5f, new Vector3(0.3f), Forward * 125f);
                var arc = SourceJointEffect.ThunderHoming(start + offset, () => start + Forward * 250f, 50f, 10f);
                arc.JointTexturePath = "Effect/flare01.jpg";
                arc.LightTint = Vector3.One;
                AddPart(arc, 8f);
            }
        }
    }
}
