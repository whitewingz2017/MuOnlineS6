#nullable enable
using System;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>BITMAP_MAGIC+1 sub11, shared by Berserker and Thorns casting.</summary>
    public sealed class BuffCastCircleEffect : SpriteObject
    {
        public override string TexturePath => "Effect/Magic_Ground2.jpg";
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[6];
        private readonly PlayerObject _owner;
        private readonly Color _color;
        private float _frames = 20f;
        public BuffCastCircleEffect(PlayerObject contextOwner, bool berserker)
        {
            _owner = contextOwner;
            _color = berserker ? new Color(1f, .1f, .2f) : new Color(.8f, .5f, .2f);
            IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
            BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            _frames -= (float)gameTime.ElapsedGameTime.TotalSeconds * 25f;
            Position = _owner.WorldPosition.Translation;
            if (_frames <= 0 || _owner.IsDead) { World?.RemoveObject(this); Dispose(); }
        }
        public override void Draw(GameTime gameTime)
        {
            if (!Visible || SpriteTexture == null || _owner.Hidden) return;
            float size = (20f - _frames) * .15f * Constants.TERRAIN_SCALE * .5f;
            var center = Position + Vector3.UnitZ * 5;
            // Source sub11 fades alpha during its final ten frames.
            var tint = _color * (_frames < 10 ? MathF.Pow(.8f, 10f - _frames) : 1f);
            var a = new VertexPositionColorTexture(center + new Vector3(-size, -size, 0), tint, new Vector2(0, 0));
            var b = new VertexPositionColorTexture(center + new Vector3(size, -size, 0), tint, new Vector2(1, 0));
            var c = new VertexPositionColorTexture(center + new Vector3(size, size, 0), tint, new Vector2(1, 1));
            var d = new VertexPositionColorTexture(center + new Vector3(-size, size, 0), tint, new Vector2(0, 1));
            _vertices[0] = a; _vertices[1] = b; _vertices[2] = c;
            _vertices[3] = a; _vertices[4] = c; _vertices[5] = d;
            ElfSupportBuffEffect.DrawWorldGeometry(SpriteTexture, _vertices, 6);
        }
        public override void DrawAfter(GameTime gameTime) { }
    }
}
