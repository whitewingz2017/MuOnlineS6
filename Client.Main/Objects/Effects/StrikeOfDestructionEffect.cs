#nullable enable
using System;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    public sealed class StrikeOfDestructionEffect : StagedCombatEffect
    {
        private readonly Vector3 _target;
        public StrikeOfDestructionEffect(WalkerObject caster, Vector3? target) : base(caster, 48f)
        {
            _target = target ?? Origin + Forward * 400f;
        }

        protected override void BuildParts()
        {
            Vector3 start = Origin + Forward * 100f;
            Vector3 side = new Vector3(-Forward.Y, Forward.X, 0f);
            start -= side * 20f;
            Vector3 blue = new Vector3(0.5f, 0.5f, 1f);
            AddPart(new LevelUpMagicCircle(start), 16f);
            AddPart(new LevelUpMagicCircle(_target), 16f);
            AddPart(new LightEffect { Position = _target + Vector3.UnitZ * 100f, Scale = 5f, Light = blue }, 16f);
            AddModel("Effect/knight_plancrack_a.bmd", start, 17f, 23f, 1.2f, blue);
            Vector3 delta = _target - start;
            delta.Z = 0f;
            float length = delta.Length();
            Vector3 direction = length > 1f ? delta / length : Forward;
            int count = Math.Clamp((int)(length / 55f) + 1, 1, 24);
            for (int i = 0; i < count; i++)
            {
                Vector3 point = Vector3.Lerp(start, _target, count == 1 ? 0f : i / (float)(count - 1));
                float yaw = MathF.Atan2(direction.X, -direction.Y) - Caster.Angle.Z;
                AddModel("Effect/knight_plancrack_b.bmd", point, 17f, 23f, 1f, blue,
                    yawOffset: yaw + MathHelper.ToRadians((i % 2 == 0 ? 1f : -1f) * MuGame.Random.Next(10, 31)));
            }
            for (int i = 0; i < 2; i++)
            {
                AddModel("Effect/nightwater01.bmd", start, 17f, 23f, 1f, blue, yawOffset: i * MathHelper.Pi);
                AddModel("Effect/nightwater01.bmd", _target, 17f, 23f, i == 0 ? 2f : 1f, blue, yawOffset: i * MathHelper.Pi);
            }
        }
    }
}
