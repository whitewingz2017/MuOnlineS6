#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>Orange LIGHT sub4 particles following the source client's upper-body bones.</summary>
    public sealed class GreaterFortitudeBuffEffect : EffectObject
    {
        private const float SourceFrameRate = 25f;
        private const int ParticleCount = 20;
        private static readonly int[] UpperBones = { 25, 26, 27, 20, 34, 35, 36 };
        private readonly Particle[] _particles = new Particle[ParticleCount];
        public PlayerObject Owner { get; }
        private Texture2D? _texture;
        private float _frameAccumulator;
        private int _nextParticle;

        private struct Particle
        {
            public int Bone;
            public float Life;
            public float Gravity;
            public float Scale;
            public float Brightness;
        }

        public GreaterFortitudeBuffEffect(PlayerObject owner)
        {
            Owner = owner;
            Position = owner.WorldPosition.Translation;
            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;
            LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-160f, -160f, 0f), new Vector3(160f, 160f, 300f));
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;
            Position = Owner.WorldPosition.Translation;
            if (Owner.Status == GameControlStatus.Disposed || Owner.IsDead)
            {
                World?.RemoveObject(this);
                Dispose();
                return;
            }

            _frameAccumulator = MathF.Min(_frameAccumulator + (float)gameTime.ElapsedGameTime.TotalSeconds * SourceFrameRate, 10f);
            while (_frameAccumulator >= 1f)
            {
                _frameAccumulator -= 1f;
                for (int i = 0; i < ParticleCount; i++)
                {
                    ref var particle = ref _particles[i];
                    if (particle.Life <= 0f)
                        continue;
                    particle.Life -= 1f;
                    particle.Gravity += MuGame.Random.Next(60, 100) / 100f * 9.5f;
                    particle.Scale -= MuGame.Random.Next(400, 800) / 10000f;
                    particle.Brightness /= 1.35f;
                }
                int bone = MuGame.Random.Next(UpperBones.Length);
                Spawn(UpperBones[bone]);
                Spawn(UpperBones[UpperBones.Length - 1 - bone]);
            }
        }

        private void Spawn(int bone)
        {
            _particles[_nextParticle] = new Particle { Bone = bone, Life = 10f, Scale = 2f, Brightness = 1f };
            _nextParticle = (_nextParticle + 1) % ParticleCount;
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (!Visible || _texture == null || Owner.Hidden || Owner.IsDead)
                return;
            var batch = GraphicsManager.Instance.Sprite;
            if (!SpriteBatchScope.BatchIsBegun)
            {
                using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.DepthRead))
                    DrawParticles(batch);
            }
            else
                DrawParticles(batch);
        }

        private void DrawParticles(SpriteBatch batch)
        {
            var camera = Camera.Instance;
            if (camera == null || _texture == null)
                return;
            var viewport = GraphicsManager.Instance.GraphicsDevice.Viewport;
            var origin = new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f);
            for (int i = 0; i < ParticleCount; i++)
            {
                ref var particle = ref _particles[i];
                if (particle.Life <= 0f || !Owner.TryGetBoneWorldMatrix(particle.Bone, out var bone))
                    continue;
                Vector3 position = bone.Translation + Vector3.UnitZ * particle.Gravity;
                Vector3 screen = viewport.Project(position, camera.Projection, camera.View, Matrix.Identity);
                if (screen.Z < 0f || screen.Z > 1f)
                    continue;
                float screenScale = Constants.RENDER_SCALE * Constants.TERRAIN_SIZE / MathF.Max(Vector3.Distance(camera.Position, position), 1f);
                batch.Draw(_texture, new Vector2(screen.X, screen.Y), null,
                    new Color(1f, 0.5f, 0.1f) * particle.Brightness, 0f, origin,
                    particle.Scale * screenScale, SpriteEffects.None, screen.Z);
            }
        }
    }
}
