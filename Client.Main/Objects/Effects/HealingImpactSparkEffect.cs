#nullable enable
using Client.Main.Graphics;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>One-source-frame SHINY+1 flash at the healing joint's recipient.</summary>
    public sealed class HealingImpactSparkEffect : SpriteObject
    {
        private float _remaining = 1f / 25f;
        public override string TexturePath => "Effect/Shiny02.jpg";
        public HealingImpactSparkEffect(Vector3 position, Vector3 color)
        {
            Position = position;
            LightEnabled = true;
            Light = color;
            Scale = MuGame.Random.Next(8, 16) * 0.2f;
            Angle = new Vector3(0, 0, MathHelper.ToRadians(MuGame.Random.Next(360)));
            BlendState = Blendings.OneOneAdditive;
            DepthState = DepthStencilState.DepthRead;
            IsTransparent = false;
            AffectedByTransparency = false;
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;
            _remaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_remaining <= 0f)
            {
                World?.RemoveObject(this);
                Dispose();
            }
        }
    }
}
