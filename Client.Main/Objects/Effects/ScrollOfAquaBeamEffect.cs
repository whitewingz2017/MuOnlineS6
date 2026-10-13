#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Scroll of Aqua Beam visual effect (Skill ID 12).
    /// Based on original MU client BITMAP_BOSS_LASER rendering.
    /// </summary>
    public sealed class ScrollOfAquaBeamEffect : EffectObject
    {
        private const string BeamTexturePath = "Effect/Spark03.OZJ"; // BITMAP_SPARK + 1
        private const string SoundAquaBeam = "Sound/sAquaFlash.wav";

        private const int BeamSegments = 20;
        private const float SegmentStep = 50f;
        private const float BeamScale = 16f;
        private const float BeamLifeFrames = 20f;

        private static readonly Vector3 SpawnOffset = new(-20f, -90f, 100f);

        private readonly WalkerObject _caster;
        private readonly Vector3? _targetPosition;
        private readonly float? _launchYaw;

        private Vector3 _startPosition;
        private Vector3 _directionStep;
        private float _lifeFrames = BeamLifeFrames;
        private float _time;
        private bool _initialized;
        private bool _soundPlayed;

        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[BeamSegments * 6];
        private Texture2D _beamTexture = null!;

        private readonly DynamicLight _beamLight;
        private TerrainControl? _lightTerrain;
        private bool _lightAdded;

        public ScrollOfAquaBeamEffect(WalkerObject caster, Vector3? targetPosition = null, float? launchYaw = null)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            _targetPosition = targetPosition;
            _launchYaw = launchYaw;

            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;

            BoundingBoxLocal = new BoundingBox(
                new Vector3(-1200f, -1200f, -120f),
                new Vector3(1200f, 1200f, 220f));

            _beamLight = new DynamicLight
            {
                Owner = this,
                Position = caster.WorldPosition.Translation + new Vector3(0f, 0f, 100f),
                Color = new Vector3(0.5f, 0.7f, 1f),
                Radius = 320f,
                Intensity = 1.1f
            };
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            _ = await TextureLoader.Instance.Prepare(BeamTexturePath);

            _beamTexture = TextureLoader.Instance.GetTexture2D(BeamTexturePath) ?? GraphicsManager.Instance.Pixel;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Status == GameControlStatus.NonInitialized)
                _ = Load();

            if (Status != GameControlStatus.Ready)
                return;

            EnsureDynamicLightAttached();

            if (_caster.Status == GameControlStatus.Disposed || _caster.World == null)
            {
                RemoveSelf();
                return;
            }

            if (!_initialized)
                InitializeBeam();

            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _lifeFrames -= FPSCounter.Instance.FPS_ANIMATION_FACTOR;

            if (_lifeFrames <= 0f)
            {
                RemoveSelf();
                return;
            }

            UpdateDynamicLight();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            if (Visible && _beamTexture != null && _initialized)
                DrawBeam();
        }

        private void InitializeBeam()
        {
            Vector3 casterPos = _caster.WorldPosition.Translation;
            Vector3 forward = GetForwardDirection(casterPos);
            float angle = MathF.Atan2(forward.X, -forward.Y);

            float sin = MathF.Sin(angle);
            float cos = MathF.Cos(angle);

            Vector3 rotatedOffset = new Vector3(
                SpawnOffset.X * cos - SpawnOffset.Y * sin,
                SpawnOffset.X * sin + SpawnOffset.Y * cos,
                SpawnOffset.Z);

            _startPosition = casterPos + rotatedOffset;
            _directionStep = forward * SegmentStep;
            Position = _startPosition;

            if (!_soundPlayed)
            {
                SoundController.Instance.PlayBuffer(SoundAquaBeam);
                _soundPlayed = true;
            }

            _initialized = true;
        }

        private Vector3 GetForwardDirection(Vector3 casterPos)
        {
            if (_launchYaw.HasValue)
                return new Vector3(MathF.Sin(_launchYaw.Value), -MathF.Cos(_launchYaw.Value), 0f);

            Vector3 forward;

            if (_targetPosition.HasValue)
            {
                Vector3 delta = _targetPosition.Value - casterPos;
                delta.Z = 0f;
                if (delta.LengthSquared() > 0.0001f)
                    forward = Vector3.Normalize(delta);
                else
                    forward = new Vector3(MathF.Sin(_caster.Angle.Z), -MathF.Cos(_caster.Angle.Z), 0f);
            }
            else
            {
                forward = new Vector3(MathF.Sin(_caster.Angle.Z), -MathF.Cos(_caster.Angle.Z), 0f);
            }

            return forward;
        }

        private void UpdateDynamicLight()
        {
            float pulse = 0.9f + 0.1f * MathF.Sin(_time * 12f);
            _beamLight.Position = _startPosition + _directionStep * (BeamSegments * 0.5f);
            _beamLight.Intensity = 1.1f * pulse;
        }

        private void DrawBeam()
        {
            // CreateSprite(BITMAP_SPARK+1, ..., Scale=16): world-size billboards.
            // Source colour is constant; there is no extra opacity pulse.
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            Vector3 right = camera.Right * (_beamTexture.Width * BeamScale * .5f);
            Vector3 up = camera.Up * (_beamTexture.Height * BeamScale * .5f);
            Color color = new(.5f, .7f, 1f);
            int count = 0;
            for (int i = 0; i < BeamSegments; i++)
            {
                Vector3 center = _startPosition + _directionStep * i;
                var a = new VertexPositionColorTexture(center - right + up, color, new Vector2(0, 0));
                var b = new VertexPositionColorTexture(center + right + up, color, new Vector2(1, 0));
                var c = new VertexPositionColorTexture(center + right - up, color, new Vector2(1, 1));
                var d = new VertexPositionColorTexture(center - right - up, color, new Vector2(0, 1));
                _vertices[count++] = a; _vertices[count++] = b; _vertices[count++] = c;
                _vertices[count++] = a; _vertices[count++] = c; _vertices[count++] = d;
            }
            ElfSupportBuffEffect.DrawWorldGeometry(_beamTexture, _vertices, count);
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
            if (_lightAdded)
            {
                TerrainControl? terrain = _lightTerrain ?? World?.Terrain;
                terrain?.RemoveDynamicLight(_beamLight);
                terrain?.RemoveDynamicLightsByOwner(this);
                _lightAdded = false;
                _lightTerrain = null;
            }

            base.Dispose();
        }

        private void EnsureDynamicLightAttached()
        {
            if (_lightAdded || Status != GameControlStatus.Ready)
                return;

            TerrainControl? terrain = World?.Terrain;
            if (terrain == null)
                return;

            _lightTerrain = terrain;
            _lightTerrain.AddDynamicLight(_beamLight);
            _lightAdded = true;
        }
    }
}
