using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Models;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(70, "Queen Rainer")]
    public class QueenRainier : MonsterObject
    {
        // The entire body is the source client's unlit blend pass, not an
        // optional overlay. Keep it on low-quality/mobile render paths too.
        public override WorldObjectRenderPolicy RenderPolicy =>
            base.RenderPolicy.With(preserveBlendMeshesInLowQuality: true);

        // Use the CPU-skinned, textured blend path on all devices. Terrain-lit
        // skinning makes the luminous body black in unlit Icarus sky cells.
        protected override bool AllowDynamicLightingShader => false;
        protected override bool AllowGpuSkinning => false;

        public QueenRainier()
        {
            Scale = 1.3f;
            MoveSpeed = 250f;
            BlendMesh = -2;
            BlendMeshLight = 1.0f;
            LightEnabled = false;
            Light = Vector3.One;
            RenderShadow = false;
            Children.Add(new MonsterBoneLightningEffect
            {
                LineScale = 0.28f,
                BonePairs = new[]
                {
                    2, 3, 3, 4, 4, 5,
                    2, 10, 10, 11,
                    2, 18, 18, 22, 23, 22, 24, 23, 25, 24,
                    18, 31, 32, 31, 33, 32, 34, 33
                }
            });

            // SourceMain5.2 RenderCharacter(MONSTER_QUEEN_RAINER): BITMAP_LIGHT sprite
            // (scale 0.8) on bone 20.
            Children.Add(new MonsterBoneSpriteEffect
            {
                BoneIndices = new[] { 20 },
                PrimaryTexturePath = "Effect/light.jpg",
                PrimaryScale = 0.8f,
                LightColor = Color.White
            });
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster52.bmd");
            await base.Load();
            SetActionSpeed(MonsterActionType.Stop1, 0.25f);
            SetActionSpeed(MonsterActionType.Stop2, 0.20f);
            SetActionSpeed(MonsterActionType.Walk, 0.34f);
            SetActionSpeed(MonsterActionType.Attack1, 0.33f);
            SetActionSpeed(MonsterActionType.Attack2, 0.33f);
            SetActionSpeed(MonsterActionType.Shock, 0.50f);
            SetActionSpeed(MonsterActionType.Die, 0.22f);
        }

        // Sound mapping based on C++ SetMonsterSound(MODEL_MONSTER01 + Type, 171, -1, 172, 172, 173);
        protected override void OnIdle()
        {
            base.OnIdle();
            Vector3 listenerPosition = ((WalkableWorldControl)World).Walker.Position;
            SoundController.Instance.PlayBufferWithAttenuation("Sound/mRainner1.wav", Position, listenerPosition); // Sound 171
        }

        public override void OnPerformAttack(int attackType = 1)
        {
            base.OnPerformAttack(attackType);
            Vector3 listenerPosition = ((WalkableWorldControl)World).Walker.Position;
            SoundController.Instance.PlayBufferWithAttenuation("Sound/mRainnerAttack1.wav", Position, listenerPosition); // Sound 172

            // SourceMain5.2 AttackEffect MONSTER_QUEEN_RAINER (CheckAttackTime(5)):
            // 20x BITMAP_BLIZZARD on the attack target.
            if (World != null && TryConsumeAttackEffectWindow() &&
                TryGetAttackTargetPosition(out Vector3 targetPosition))
            {
                var blizzard = new Effects.MonsterBlizzardStrikeEffect(targetPosition);
                World.Objects.Add(blizzard);
                _ = blizzard.Load();
            }
        }

        public override void OnDeathAnimationStart()
        {
            base.OnDeathAnimationStart();
            Vector3 listenerPosition = ((WalkableWorldControl)World).Walker.Position;
            SoundController.Instance.PlayBufferWithAttenuation("Sound/mRainnerDie.wav", Position, listenerPosition); // Sound 173
        }
    }
}
