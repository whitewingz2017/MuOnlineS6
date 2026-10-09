using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Short blue ground effect used by the normal level-up animation.
    /// </summary>
    public class LevelUpMagicCircle : SpriteObject
    {
        private const float SourceFrameRate = 25f;
        private const float LifetimeFrames = 20f;
        private const float _lifeTotal = LifetimeFrames / SourceFrameRate;
        private float _life = _lifeTotal;
        private readonly bool _fortitude;
        private readonly float _fortitudeScale;

        public override string TexturePath => "Effect/Magic_Ground2.jpg";

        public LevelUpMagicCircle(Vector3 startPos, bool fortitude = false, float rotation = 0f)
        {
            _fortitude = fortitude;
            _fortitudeScale = fortitude ? MuGame.Random.Next(50, 100) / 100f * 4f : 0f;
            if (fortitude)
                _life = 40f / SourceFrameRate;
            Position = startPos;
            Angle = new Vector3(0f, 0f, rotation);
            IsTransparent = true;
            BlendState = Blendings.OneOneAdditive;
            Scale = 0f;
            LightEnabled = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _life -= dt;
            if (_life <= 0f)
            {
                World?.RemoveObject(this);
                Dispose();
                return;
            }

            float lifeFrames = MathHelper.Clamp(_life * SourceFrameRate, 0f, _fortitude ? 40f : LifetimeFrames);
            if (_fortitude)
            {
                Scale = _fortitudeScale;
                Alpha = lifeFrames < 5f ? lifeFrames / 5f : MathF.Sin((60f - lifeFrames) * 0.05f) + 0.5f;
                return;
            }
            Scale = (LifetimeFrames - lifeFrames) * 0.15f;
            Alpha = lifeFrames < 5f ? lifeFrames / 5f : 1f;

            Angle = Vector3.Zero;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || SpriteTexture == null) return;

            var gd = GraphicsManager.Instance.GraphicsDevice;
            var effect = GraphicsManager.Instance.AlphaTestEffect3D;

            BlendState originalBlend = gd.BlendState;
            DepthStencilState originalDepth = gd.DepthStencilState;
            RasterizerState originalRasterizer = gd.RasterizerState;
            SamplerState originalSampler = gd.SamplerStates[0];
            Matrix originalWorld = effect.World;
            Matrix originalView = effect.View;
            Matrix originalProjection = effect.Projection;
            Texture2D originalTexture = effect.Texture;
            Vector3 originalDiffuse = effect.DiffuseColor;
            float originalAlpha = effect.Alpha;
            bool originalVertexColorEnabled = effect.VertexColorEnabled;

            try
            {
                gd.BlendState = Blendings.OneOneAdditive;
                gd.DepthStencilState = DepthStencilState.DepthRead;
                gd.RasterizerState = RasterizerState.CullNone;
                gd.SamplerStates[0] = SamplerState.LinearClamp;

                effect.World = Matrix.CreateScale(Scale * Constants.TERRAIN_SCALE)
                                  * Matrix.CreateRotationX(-MathHelper.PiOver2)
                                  * Matrix.CreateRotationZ(-Angle.Z)
                                  * Matrix.CreateTranslation(Position + new Vector3(0f, 0f, 5f));
                effect.View = Camera.Instance.View;
                effect.Projection = Camera.Instance.Projection;
                effect.Texture = SpriteTexture;
                effect.VertexColorEnabled = false;
                effect.DiffuseColor = _fortitude ? new Vector3(1f, 0.5f, 0.1f) : new Vector3(0.4f, 0.6f, 1f);
                effect.Alpha = this.Alpha;

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    gd.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        GroundVertices,
                        0,
                        GroundVertices.Length,
                        GroundIndices,
                        0,
                        2);
                }
            }
            finally
            {
                gd.BlendState = originalBlend;
                gd.DepthStencilState = originalDepth;
                gd.RasterizerState = originalRasterizer;
                gd.SamplerStates[0] = originalSampler;
                effect.World = originalWorld;
                effect.View = originalView;
                effect.Projection = originalProjection;
                effect.Texture = originalTexture;
                effect.DiffuseColor = originalDiffuse;
                effect.Alpha = originalAlpha;
                effect.VertexColorEnabled = originalVertexColorEnabled;
            }
        }

        private static readonly VertexPositionTexture[] GroundVertices =
        {
            new VertexPositionTexture(new Vector3(-1f, 0f, -1f), new Vector2(0f, 0f)),
            new VertexPositionTexture(new Vector3(1f, 0f, -1f), new Vector2(1f, 0f)),
            new VertexPositionTexture(new Vector3(-1f, 0f, 1f), new Vector2(0f, 1f)),
            new VertexPositionTexture(new Vector3(1f, 0f, 1f), new Vector2(1f, 1f))
        };

        private static readonly short[] GroundIndices = { 0, 1, 2, 2, 1, 3 };
    }
}
