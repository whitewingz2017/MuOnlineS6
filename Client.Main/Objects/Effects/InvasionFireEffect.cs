using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>GOBoid mouth flame and MODEL_FIRE sub3 invasion fireballs.</summary>
    public sealed class InvasionFireEffect : EffectObject
    {
        private const int ParticleCapacity = 40;
        private readonly ModelObject _dragon;
        private readonly Particle[] _particles = new Particle[ParticleCapacity];
        private Texture2D _fire, _shine, _mouth;
        private float _life = 80f / 25f, _emissionClock, _phase;
        private int _next;
        private readonly int _screwSeed = MuGame.Random.Next(1000) * 5371;
        private Vector3 _origin;
        private struct Particle { public Vector3 Position; public float Age, Rotation, Scale; }
        public bool Expired => _life <= 0f || Status == GameControlStatus.Error;

        public InvasionFireEffect(ModelObject dragon = null)
        {
            _dragon = dragon;
            Interactive = false;
            LightEnabled = false;
            AffectedByTransparency = false;
            IsTransparent = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;
            BoundingBoxLocal = new BoundingBox(new Vector3(-180), new Vector3(180));
            for (int i = 0; i < _particles.Length; i++) _particles[i].Age = 1f;
        }
        public override async Task LoadContent()
        {
            await base.LoadContent();
            _fire = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Fire01.jpg");
            _shine = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Shiny02.jpg");
            _mouth = await TextureLoader.Instance.PrepareAndGetTexture("Effect/lightning2.jpg");
        }
        public override void Update(GameTime time)
        {
            if (Status != GameControlStatus.Ready) return;
            base.Update(time);
            float dt = (float)time.ElapsedGameTime.TotalSeconds;
            _phase += dt * 12f;
            if (_dragon != null)
            {
                var bones = _dragon.GetBoneTransforms();
                if (bones == null || bones.Length <= 11) return;
                _origin = Vector3.Transform(new Vector3(0, -50, 0), bones[11] * _dragon.WorldPosition);
            }
            else
            {
                _life -= dt;
                Position += new Vector3(0, -300f * dt, 0);
                float tick = _screwSeed + (float)time.TotalGameTime.TotalMilliseconds / 40f;
                float a = MathF.Sin((tick + 55555) * .048f);
                float x = a * MathF.Cos(tick * .0613f), y = a * MathF.Sin(tick * .0613f);
                float z = MathF.Cos((tick + 55555) * .048f);
                float s = MathF.Sin((tick + 11111) * .1113f), c = MathF.Cos((tick + 11111) * .1113f);
                _origin = Position + new Vector3(c * y - s * z, s * y + c * z, x) * 50f;
            }
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i].Age += dt;
                _particles[i].Position.Z += dt * _particles[i].Age * 25f;
            }
            _emissionClock += MathF.Min(dt, 0.12f);
            while (_emissionClock >= 0.04f)
            {
                _emissionClock -= 0.04f;
                _particles[_next] = new Particle { Position = _origin,
                    Scale = _dragon == null ? .75f : MuGame.Random.Next(128, 192) * .01f,
                    Rotation = MuGame.Random.NextSingle() * MathHelper.TwoPi };
                _next = (_next + 1) % ParticleCapacity;
            }
        }
        public override void DrawAfter(GameTime time)
        {
            if (Status != GameControlStatus.Ready || Camera.Instance == null || (_dragon == null && Expired)) return;
            using (new SpriteBatchScope(GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred,
                BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullNone))
            {
                if (_dragon != null) DrawBillboard(_mouth, _origin, 1f, Color.Red, 0);
                else DrawBillboard(_shine, _origin, 0.9f, Color.White, _phase);
                for (int i = 0; i < _particles.Length; i++)
                {
                    ref Particle p = ref _particles[i];
                    if (p.Age >= 24f / 25f) continue;
                    float scale = _dragon == null ? p.Scale * MathF.Pow(.95f, p.Age * 25f) : MathF.Max(0, p.Scale - p.Age);
                    DrawBillboard(_fire, p.Position, scale,
                        Color.White * (1f - p.Age / (24f / 25f)),
                        _dragon == null ? p.Rotation + MathHelper.ToRadians(125f) * p.Age : 0,
                        Math.Clamp((int)(p.Age * 25f / 6f), 0, 3));
                }
            }
        }
        private void DrawBillboard(Texture2D texture, Vector3 position, float scale, Color tint, float rotation, int frame = -1)
        {
            if (texture == null) return;
            var camera = Camera.Instance;
            var viewport = GraphicsDevice.Viewport;
            Vector3 center = viewport.Project(position, camera.Projection, camera.View, Matrix.Identity);
            if (!float.IsFinite(center.X) || center.Z < 0 || center.Z >= 1) return;
            Matrix view = camera.View;
            Vector3 right = new Vector3(view.M11, view.M21, view.M31);
            int width = frame >= 0 ? texture.Width / 4 : texture.Width;
            Vector3 edge = viewport.Project(position + right * width * scale * 0.5f,
                camera.Projection, view, Matrix.Identity);
            float screenScale = Vector2.Distance(new Vector2(center.X, center.Y), new Vector2(edge.X, edge.Y)) * 2f / width;
            var source = new Rectangle(frame >= 0 ? frame * width : 0, 0, width, texture.Height);
            GraphicsManager.Instance.Sprite.Draw(texture, new Vector2(center.X, center.Y), source, tint,
                rotation, new Vector2(width, texture.Height) * 0.5f, screenScale, SpriteEffects.None, center.Z);
        }
        public override void Dispose()
        {
            base.Dispose();
            _fire = _shine = _mouth = null; // Shared loader owns these textures.
        }
    }
}
