#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>Original elshildring/elshildring2 and DarkLordSkill casting models.</summary>
    public sealed class BuffCastModelEffect : ModelObject
    {
        private readonly PlayerObject _owner;
        private readonly bool _critical;
        private readonly int _part;
        private float _frames;
        private float _accumulator;
        private float _velocity = .1f;
        private readonly string _path;
        public override WorldObjectRenderPolicy RenderPolicy => base.RenderPolicy.With(forceVisible: true, alwaysUpdate: true);

        public BuffCastModelEffect(PlayerObject owner, bool critical, int part, bool berserker = false)
        {
            _owner = owner; _critical = critical; _part = part;
            _frames = critical ? 10 : part == 0 ? 34 : 35;
            _path = critical ? "Skill/DarkLordSkill.bmd" : part == 0 ? "Effect/elshildring.bmd" : "Effect/elshildring2.bmd";
            Position = owner.WorldPosition.Translation + Vector3.UnitZ * (critical ? 0 : 100);
            Scale = critical ? .2f : part == 0 ? .1f : .15f;
            Angle = critical ? new Vector3(MathHelper.PiOver4, MathHelper.ToRadians(45 - 90 * part), 0) : Vector3.Zero;
            Light = critical ? new Vector3(1f, .6f, .3f) : berserker ? new Vector3(1f, .1f, .2f) : new Vector3(.8f, .5f, .2f);
            Alpha = critical ? 1f : 0f;
            BlendMesh = -2; BlendMeshState = Blendings.OneOneAdditive;
            BlendMeshLight = critical ? 1f : 0f;
            LightEnabled = true; IsTransparent = true; AffectedByTransparency = false;
            DepthState = DepthStencilState.DepthRead; RenderShadow = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-300), new Vector3(300));
            if (critical && owner.TryGetBoneWorldMatrix(part == 0 ? 33 : 42, out var bone)) Position = bone.Translation;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare(_path);
            await base.Load();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            if (_owner.IsDead || _owner.Status == GameControlStatus.Disposed) { Remove(); return; }
            _accumulator += (float)gameTime.ElapsedGameTime.TotalSeconds * 25f;
            while (_accumulator >= 1f)
            {
                _accumulator -= 1f;
                if (_critical)
                {
                    Scale += _velocity; _velocity += .02f;
                    if (_frames < 7) BlendMeshLight /= 1.8f;
                }
                else
                {
                    Position = _owner.WorldPosition.Translation + Vector3.UnitZ * 100;
                    Alpha = MathF.Max(0, Alpha + (_frames > 20 ? .05f : -.05f));
                    BlendMeshLight = Alpha;
                    Angle += Vector3.UnitZ * MathHelper.ToRadians(_part == 0 ? 8 : -8);
                    Scale += .035f;
                }
                _frames--;
                if (_frames <= 0) { Remove(); return; }
            }
        }
        private void Remove() { World?.RemoveObject(this); Dispose(); }
    }
}
