using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.NPCS.Effects
{
    /// <summary>MuMain's bone-attached cherry spirit lights, petals and sparkles.</summary>
    public sealed class CherryBlossomVisualEffect : EffectObject
    {
        private const int ParticleCapacity = 96;
        private static readonly int[] GlowBones = [43, 3, 53, 56, 59, 62];
        private static readonly float[] GlowScales = [2f, 5f, 1.5f, 1.5f, 1.5f, 1.5f];
        private readonly Vector3[] _origins = new Vector3[6];
        private readonly Particle[] _particles = new Particle[ParticleCapacity];
        private Texture2D _light, _spark, _petal;
        private float _sparkClock, _petalClock;
        private int _nextParticle;
        private bool _hasPose;

        private struct Particle
        {
            public Vector3 Position, Velocity;
            public float Remaining, Duration, Rotation, Scale;
            public Color Color;
            public bool Petal;
        }

        public CherryBlossomVisualEffect()
        {
            Interactive = false;
            LightEnabled = false;
            AffectedByTransparency = false;
            IsTransparent = true;
            DepthState = DepthStencilState.DepthRead;
            BoundingBoxLocal = new BoundingBox(new Vector3(-180), new Vector3(180));
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            _light = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
            _spark = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Spark03.jpg");
            _petal = await TextureLoader.Instance.PrepareAndGetTexture("Effect/cherryblossom/sakuras01.jpg");
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
            if (Parent is not ModelObject model || model.GetBoneTransforms() is not { } bones)
                return;
            _hasPose = true;
            for (int i = 0; i < GlowBones.Length; i++)
            {
                int bone = GlowBones[i];
                _origins[i] = bone < bones.Length ? bones[bone].Translation : Vector3.Zero;
            }
            _origins[0].Z += 20f;
            float dt = (float)time.ElapsedGameTime.TotalSeconds;
            for (int i = 0; i < _particles.Length; i++)
            {
                ref Particle p = ref _particles[i];
                if (p.Remaining <= 0f) continue;
                p.Remaining -= dt;
                p.Position += p.Velocity * dt;
                if (p.Petal) p.Rotation += dt;
            }

            _sparkClock += dt;
            _petalClock += dt;
            // Fixed emission rates preserve the original 25 Hz effect on fast devices.
            if (_sparkClock >= 0.04f)
            {
                _sparkClock %= 0.04f;
                float pulse = MathF.Abs(MathF.Sin((float)time.TotalGameTime.TotalSeconds * 2f)) * 0.2f;
                Emit(false, 0, pulse + 0.2f);
                Emit(false, 0, pulse + 0.3f);
                int flare = MuGame.Random.Next(4) + 2;
                Emit(false, flare, pulse + 0.4f);
            }
            if (_petalClock >= 0.08f)
            {
                _petalClock %= 0.08f;
                Emit(true, 0, 0.3f);
            }
        }

        private void Emit(bool petal, int origin, float scale)
        {
            ref Particle p = ref _particles[_nextParticle];
            _nextParticle = (_nextParticle + 1) % _particles.Length;
            p = new Particle
            {
                Position = _origins[origin],
                Velocity = new Vector3(MuGame.Random.Next(6) - 2, MuGame.Random.Next(4), MuGame.Random.Next(4)) * 25f,
                Duration = petal ? 2.8f : 0.8f,
                Remaining = petal ? 2.8f : 0.8f,
                Rotation = petal ? MathHelper.ToRadians(MuGame.Random.Next(360)) : 0f,
                Scale = scale,
                Color = petal ? new Color(1f, 0.6f, 0.8f) : Color.White,
                Petal = petal
            };
        }

        public override void Draw(GameTime time)
        {
            if (!Visible || !_hasPose || Parent is not ModelObject model || Camera.Instance == null)
                return;
            var batch = GraphicsManager.Instance.Sprite;
            using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.DepthRead))
            {
                for (int i = 0; i < GlowBones.Length; i++)
                {
                    Color tint = i == 0 ? new Color(0.7f, 0.5f, 0.2f)
                        : i == 1 ? new Color(0.7f, 0.2f, 0.6f) : new Color(0.9f, 0.4f, 0.8f);
                    DrawSprite(_light, _origins[i], GlowScales[i], tint, 0f, model);
                }
                for (int i = 0; i < _particles.Length; i++)
                {
                    ref Particle p = ref _particles[i];
                    if (p.Remaining <= 0f) continue;
                    DrawSprite(p.Petal ? _petal : _spark, p.Position, p.Scale,
                        p.Color * MathHelper.Clamp(p.Remaining / p.Duration, 0f, 1f), p.Rotation, model);
                }
            }
        }

        private void DrawSprite(Texture2D texture, Vector3 local, float scale, Color tint, float rotation, ModelObject model)
        {
            if (texture == null) return;
            Vector3 world = Vector3.Transform(local, model.WorldPosition);
            var camera = Camera.Instance;
            Vector3 projected = GraphicsDevice.Viewport.Project(world, camera.Projection, camera.View, Matrix.Identity);
            if (!float.IsFinite(projected.X) || projected.Z < 0f || projected.Z >= 1f) return;
            // MuMain billboards are texture.Width * scale world units across.
            // Project that extent through the actual camera; a distance-only
            // estimate loses viewport/FOV scaling and makes mobile glows tiny.
            Matrix view = camera.View;
            Vector3 cameraRight = new Vector3(view.M11, view.M21, view.M31);
            Vector3 edge = GraphicsDevice.Viewport.Project(
                world + cameraRight * (texture.Width * scale * 0.5f),
                camera.Projection, view, Matrix.Identity);
            float pixels = Vector2.Distance(new Vector2(edge.X, edge.Y),
                new Vector2(projected.X, projected.Y)) * 2f / texture.Width;
            GraphicsManager.Instance.Sprite.Draw(texture, new Vector2(projected.X, projected.Y), null,
                tint * model.TotalAlpha, rotation, new Vector2(texture.Width, texture.Height) * 0.5f,
                pixels, SpriteEffects.None, projected.Z);
        }

        public override void Dispose()
        {
            base.Dispose();
            _light = _spark = _petal = null; // Textures belong to the shared loader.
        }
    }
}
