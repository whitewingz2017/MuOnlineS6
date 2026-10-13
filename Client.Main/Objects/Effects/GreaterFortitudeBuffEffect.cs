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
        public override WorldObjectRenderPolicy RenderPolicy => base.RenderPolicy.With(forceVisible: true, alwaysUpdate: true);
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[ParticleCount * 6];
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
            IsTransparent = false;
            AffectedByTransparency = false;
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
            DrawParticles();
        }

        private void DrawParticles()
        {
            if (_texture == null) return;
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            int count = 0;
            for (int i = 0; i < ParticleCount; i++)
            {
                ref var particle = ref _particles[i];
                if (particle.Life <= 0f || !Owner.TryGetBoneWorldMatrix(particle.Bone, out var bone)) continue;
                Vector3 center = bone.Translation + Vector3.UnitZ * particle.Gravity;
                Vector3 right = camera.Right * _texture.Width * particle.Scale * .5f;
                Vector3 up = camera.Up * _texture.Height * particle.Scale * .5f;
                Color tint = new Color(1f, .5f, .1f) * particle.Brightness;
                var a = new VertexPositionColorTexture(center - right + up, tint, new Vector2(0, 0));
                var b = new VertexPositionColorTexture(center + right + up, tint, new Vector2(1, 0));
                var c = new VertexPositionColorTexture(center + right - up, tint, new Vector2(1, 1));
                var d = new VertexPositionColorTexture(center - right - up, tint, new Vector2(0, 1));
                _vertices[count++] = a; _vertices[count++] = b; _vertices[count++] = c;
                _vertices[count++] = a; _vertices[count++] = c; _vertices[count++] = d;
            }
            ElfSupportBuffEffect.DrawWorldGeometry(_texture, _vertices, count);
        }
    }
}
