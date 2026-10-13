#nullable enable
using Client.Main.Objects.Player;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(64)]
    [SkillVisualEffect(217)]
    [SkillVisualEffect(218)]
    [SkillVisualEffect(469)]
    [SkillVisualEffect(470)]
    [SkillVisualEffect(472)]
    [SkillVisualEffect(511)]
    [SkillVisualEffect(515)]
    [SkillVisualEffect(517)]
    [SkillVisualEffect(522)]
    public sealed class ClassBuffSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster || context.World == null) return null;
            ushort skill = SkillVisualEffectRegistry.GetBaseSkillId(context.SkillId);
            PlayerObject target = caster;
            if (skill == 217 && context.TargetId != 0 && context.World.TryGetWalkerById(context.TargetId, out var selected) && selected is PlayerObject player)
                target = player;
            return new BuffCastRoot(target, skill is 64 or 511 or 515 or 517 or 522, skill is 218 or 469 or 470 or 472);
        }

        private sealed class BuffCastRoot : EffectObject
        {
            private float _remaining = 1.5f;
            public override WorldObjectRenderPolicy RenderPolicy => base.RenderPolicy.With(forceVisible: true, alwaysUpdate: true);
            public BuffCastRoot(PlayerObject owner, bool critical, bool berserker)
            {
                Position = Vector3.Zero;
                IsTransparent = true; AffectedByTransparency = false;
                BlendState = BlendState.Additive; DepthState = DepthStencilState.DepthRead; LightEnabled = false;
                BoundingBoxLocal = new BoundingBox(new Vector3(-50000), new Vector3(50000));
                if (!critical) Children.Add(new BuffCastCircleEffect(contextOwner: owner, berserker: berserker));
                for (int part = 0; part < 2; part++)
                {
                    var weapon = part == 0 ? owner.Weapon1 : owner.Weapon2;
                    if (critical && (weapon.Hidden || weapon.Model == null || weapon.ItemGroup == 6)) continue;
                    Children.Add(new BuffCastModelEffect(owner, critical, part, berserker));
                }
            }
            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (Status != GameControlStatus.Ready) return;
                _remaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_remaining <= 0) { World?.RemoveObject(this); Dispose(); }
            }
        }
    }
}
