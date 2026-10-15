#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controls;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Scroll of Poison visual effect (Skill ID 1), based on Main 5.2 behavior:
    /// poison core model and ten smoke particles on impact.
    /// </summary>
    public sealed class ScrollOfPoisonEffect : EffectObject
    {
        private const string PoisonBaseName = "Poison";
        private const string SmokeTexturePath = "Effect/smoke01.jpg";
        private const float TotalLifeFrames = 50f;
        private const float ModelLifeFrames = 40f;
        private const int SmokeParticleCount = 10;
        private readonly VertexPositionColorTexture[] _smokeVertices = new VertexPositionColorTexture[SmokeParticleCount * 6];

        private readonly Vector3 _center;

        private string _poisonPath = "Skill/Poison01.bmd";
        private bool _pathsResolved;
        private bool _initialized;
        private float _lifeFrames = TotalLifeFrames;

        private Texture2D _smokeTexture = null!;

        private readonly SmokeParticle[] _smokeParticles = new SmokeParticle[SmokeParticleCount];
        private readonly DynamicLight _poisonLight;
        private bool _lightAdded;

        private readonly Color _smokeColor = new(0.5f, 1f, 0.8f);

        private struct SmokeParticle
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float Life;
            public float Scale;
        }

        public ScrollOfPoisonEffect(Vector3 center)
        {
            _center = center;
            Position = center;

            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;

            BoundingBoxLocal = new BoundingBox(
                new Vector3(-180f, -180f, -120f),
                new Vector3(180f, 180f, 240f));

            _poisonLight = new DynamicLight
            {
                Owner = this,
                Position = center,
                Color = new Vector3(0.3f, 1f, 0.6f),
                Radius = 2f * Constants.TERRAIN_SIZE,
                Intensity = ModelLifeFrames * .1f
            };
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            await ResolvePaths();

            _ = await TextureLoader.Instance.Prepare(SmokeTexturePath);
            _smokeTexture = TextureLoader.Instance.GetTexture2D(SmokeTexturePath) ?? GraphicsManager.Instance.Pixel;

            if (World?.Terrain != null && !_lightAdded)
            {
                World.Terrain.AddDynamicLight(_poisonLight);
                _lightAdded = true;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Status == GameControlStatus.NonInitialized)
                _ = Load();

            if (Status != GameControlStatus.Ready)
                return;

            if (!_initialized)
            {
                SpawnPoisonModel();
                InitializeSmokeParticles();
                _initialized = true;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float factor = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _lifeFrames -= factor;

            UpdateSmokeParticles(dt);
            UpdateDynamicLight();

            if (_lifeFrames <= 0f)
                RemoveSelf();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            if (Visible && _smokeTexture != null)
                DrawSmokeParticles();
        }

        private async Task ResolvePaths()
        {
            if (_pathsResolved)
                return;

            _poisonPath = await ResolveModelPath(PoisonBaseName, 1, "Skill/Poison01.bmd", "Skill/Poison1.bmd", "Skill/Poison.bmd");
            _pathsResolved = true;
        }

        private void SpawnPoisonModel()
        {
            var core = new PoisonCoreModel(_poisonPath, ModelLifeFrames)
            {
                Position = Vector3.Zero,
                Angle = Vector3.Zero,
                Scale = 1f
            };

            Children.Add(core);
            _ = core.Load();
        }

        private void InitializeSmokeParticles()
        {
            // ZzzCharacter: ten BITMAP_SMOKE subtype 1 particles at the victim.
            for (int i = 0; i < _smokeParticles.Length; i++)
            {
                _smokeParticles[i] = new SmokeParticle
                {
                    Position = new Vector3(MuGame.Random.Next(64) - 32,
                        MuGame.Random.Next(64) - 32, MuGame.Random.Next(64) + 32),
                    Velocity = SourceJointMath.Rotate(new Vector3(0, -(MuGame.Random.Next(8) + 40), 0),
                        new Vector3(MuGame.Random.Next(90) - 45, 0, MuGame.Random.Next(360))),
                    Life = 50f,
                    Scale = (MuGame.Random.Next(32) + 80) * .01f
                };
            }
        }

        private void UpdateSmokeParticles(float dt)
        {
            float factor = dt * 25f;
            for (int i = 0; i < _smokeParticles.Length; i++)
            {
                ref var particle = ref _smokeParticles[i];
                if (particle.Life <= 0) continue;
                particle.Position += particle.Velocity * factor;
                particle.Velocity *= MathF.Pow(.4f, factor);
                particle.Scale += .05f * factor;
                particle.Life -= factor;
            }
        }

        private void DrawSmokeParticles()
        {
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            int count = 0;
            foreach (ref readonly var particle in _smokeParticles.AsSpan())
            {
                if (particle.Life <= 0) continue;
                Vector3 center = _center + particle.Position;
                Vector3 right = camera.Right * (_smokeTexture.Width * particle.Scale * .5f);
                Vector3 up = camera.Up * (_smokeTexture.Height * particle.Scale * .5f);
                Color color = _smokeColor * MathHelper.Clamp(particle.Life / 50f, 0, 1);
                var a = new VertexPositionColorTexture(center - right + up, color, new Vector2(0, 0));
                var b = new VertexPositionColorTexture(center + right + up, color, new Vector2(1, 0));
                var c = new VertexPositionColorTexture(center + right - up, color, new Vector2(1, 1));
                var d = new VertexPositionColorTexture(center - right - up, color, new Vector2(0, 1));
                _smokeVertices[count++] = a; _smokeVertices[count++] = b; _smokeVertices[count++] = c;
                _smokeVertices[count++] = a; _smokeVertices[count++] = c; _smokeVertices[count++] = d;
            }
            ElfSupportBuffEffect.DrawWorldGeometry(_smokeTexture, _smokeVertices, count);
        }

        private void UpdateDynamicLight()
        {
            // MuMain Move_MODEL_POISON: terrain light follows the core's
            // remaining lifetime, rather than the longer smoke lifetime.
            float modelLife = MathF.Max(0f, _lifeFrames - (TotalLifeFrames - ModelLifeFrames));
            _poisonLight.Position = _center;
            _poisonLight.Intensity = modelLife * .1f;
        }

        private static async Task<string> ResolveModelPath(string baseName, int index, params string[] fallbackCandidates)
        {
            string zeroPath = $"Skill/{baseName}0{index}.bmd";
            if (await BMDLoader.Instance.AssestExist(zeroPath))
                return zeroPath;

            string plainPath = $"Skill/{baseName}{index}.bmd";
            if (await BMDLoader.Instance.AssestExist(plainPath))
                return plainPath;

            for (int i = 0; i < fallbackCandidates.Length; i++)
            {
                string candidate = fallbackCandidates[i];
                if (await BMDLoader.Instance.AssestExist(candidate))
                    return candidate;
            }

            return zeroPath;
        }

        private static float ComputeScreenScale(Vector3 worldPos, float baseScale)
        {
            float distance = Vector3.Distance(Camera.Instance.Position, worldPos);
            float scale = baseScale / (MathF.Max(distance, 0.1f) / Constants.TERRAIN_SIZE);
            return scale * Constants.RENDER_SCALE;
        }

        private static float RandomRange(float min, float max)
        {
            return (float)(MuGame.Random.NextDouble() * (max - min) + min);
        }

        private void RemoveSelf()
        {
            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.RemoveObject(this);

            Dispose();
        }

        public override void Dispose()
        {
            if (_lightAdded && World?.Terrain != null)
            {
                World.Terrain.RemoveDynamicLight(_poisonLight);
                _lightAdded = false;
            }

            base.Dispose();
        }

        private sealed class PoisonCoreModel : ModelObject
        {
            private readonly string _path;
            private float _lifeFrames;

            public PoisonCoreModel(string path, float lifeFrames)
            {
                _path = path;
                _lifeFrames = lifeFrames;

                ContinuousAnimation = true;
                AnimationSpeed = 3.6f;
                LightEnabled = false;
                IsTransparent = true;
                DepthState = DepthStencilState.DepthRead;
                BlendState = BlendState.Additive;
                BlendMeshState = BlendState.Additive;
                BlendMesh = 1;
            }

            public override async Task Load()
            {
                Model = await BMDLoader.Instance.Prepare(_path);
                await base.Load();
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (Status != GameControlStatus.Ready)
                    return;

                float factor = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
                _lifeFrames -= factor;
                BlendMeshLight = _lifeFrames * .1f;
                Alpha = MathHelper.Clamp(_lifeFrames * .1f, 0f, 1f);

                if (_lifeFrames <= 0f)
                    RemoveSelf();
            }

            private void RemoveSelf()
            {
                if (Parent != null)
                    Parent.Children.Remove(this);
                else
                    World?.RemoveObject(this);

                Dispose();
            }
        }
    }
}
