#nullable enable
using System;
using System.Threading.Tasks;
using Client.Data.BMD;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Scroll of Evil Spirit visual effect (Skill ID 9).
    /// Creates 4 spirit bolts at 90° intervals that spiral inward towards the caster.
    /// Renders as 3D cross-shaped ribbons (two perpendicular quads per segment).
    /// Based on original MU client: ZzzEffectJoint.cpp
    /// </summary>
    public sealed class ScrollOfEvilSpiritEffect : EffectObject
    {
        private const string JointSpiritTexturePath = "Effect/JointSpirit01.jpg";
        private const string DefaultLaserModelPath = "Skill/Laser01.bmd";
        private const string SoundEvil = "Sound/sEvil.wav";

        // Original parameters from ZzzEffectJoint.cpp SubType 0
        private const float InitialZOffset = 100f;
        private const float TargetZOffset = 80f;
        private const float Velocity = 70f;
        private const float LifeTimeFrames = 49f;
        private const int BaseMaxTails = 6;
        private const int MaxTailCapacity = 200;
        private const int SpiritCount = 4;
        private const float HummingTurnRate = 10f;

        // Keep cloud-like trail volume close to prior implementation.
        private const float MainSpiritScale = 80f;
        private const float VisualSpiritScale = 20f;
        private const bool RenderVisualRibbonLayer = true;
        private const float TailOpacityRiseSpeed = 0.45f;

        // Terrain darkening for spirit wake.
        private const float DarkLightRadius = 400f;
        private const float DarkLightIntensity = 1f;

        // Subtractive blend state (RENDER_TYPE_ALPHA_BLEND_MINUS)
        private static readonly BlendState SubtractiveBlend = new BlendState
        {
            // MuMain BlendMode::Subtract is GL_ZERO, GL_ONE_MINUS_SRC_COLOR,
            // not a reverse-subtract blend equation.
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.InverseSourceColor,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            AlphaBlendFunction = BlendFunction.Add
        };

        private readonly WalkerObject _caster;

        private Texture2D? _spiritTexture;
        private BasicEffect? _effect;

        private readonly SpiritBolt[] _spirits = new SpiritBolt[SpiritCount * 2];
        private int _spiritCount;
        private int _targetTailCount;

        // Dark lights for shadow effect (one per main spirit).
        private readonly DynamicLight[] _darkLights = new DynamicLight[SpiritCount];

        // MuMain emits MODEL_LASER once per source tick for each main spirit.
        private readonly EvilSpiritGhostModel?[] _spiritHeadCore = new EvilSpiritGhostModel?[SpiritCount];
        private readonly EvilSpiritGhostModel?[] _spiritHeadBlurA = new EvilSpiritGhostModel?[SpiritCount];
        private readonly EvilSpiritGhostModel?[] _spiritHeadBlurB = new EvilSpiritGhostModel?[SpiritCount];
        private string _laserModelPath = DefaultLaserModelPath;
        private BMD? _laserModel;
        private bool _laserPathResolved;
        private bool _spiritHeadsSpawned;

        private bool _lightsAdded;

        private bool _soundPlayed;
        private bool _initialized;

        // Vertex/index buffers for 3D rendering
        private VertexPositionColorTexture[] _vertices = null!;
        private short[] _indices = null!;

        private struct SpiritBolt
        {
            public Vector3 Position;
            public Vector3 Angle;         // Euler angles (Pitch, unused, Yaw) in degrees
            public Vector3 Direction;     // Perturbation accumulator
            public TailVertex[] Tails;    // Each tail has 4 vertices (cross shape)
            public int NumTails;
            public float LifeTime;
            public float Scale;
            public float TailOpacity;
            public float TailShrinkAccumulator;
        }

        private struct TailVertex
        {
            public Vector3 V0;  // Left (-X)
            public Vector3 V1;  // Right (+X)
            public Vector3 V2;  // Down (-Z)
            public Vector3 V3;  // Up (+Z)
        }

        public ScrollOfEvilSpiritEffect(WalkerObject caster, float targetAngle)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));

            Position = caster.WorldPosition.Translation;

            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;

            BoundingBoxLocal = new BoundingBox(
                new Vector3(-600f, -600f, -150f),
                new Vector3(600f, 600f, 350f));

            // Pre-allocate vertex/index buffers.
            // Tail count is dynamic (scaled by FPS), so allocate for max capacity.
            int maxSpirits = SpiritCount * 2;
            int maxQuads = maxSpirits * MaxTailCapacity * 2;  // 2 quads per segment (cross shape)
            _vertices = new VertexPositionColorTexture[maxQuads * 4];
            _indices = QuadIndexCache.Get(maxQuads);
        }

        private void InitializeSpirits()
        {
            if (_initialized)
                return;

            _initialized = true;
            _spiritCount = 0;
            _targetTailCount = ResolveTargetTailCount(FPSCounter.Instance.FPS_ANIMATION_FACTOR);

            Vector3 casterPos = _caster.WorldPosition.Translation;
            Vector3 startPos = casterPos + new Vector3(0, 0, InitialZOffset);

            for (int i = 0; i < SpiritCount; i++)
            {
                float angleOffset = i * 90f;

                // Main bolt (larger scale)
                _spirits[_spiritCount++] = new SpiritBolt
                {
                    Position = startPos,
                    Angle = new Vector3(0f, 0f, angleOffset),
                    Direction = Vector3.Zero,
                    Tails = new TailVertex[MaxTailCapacity + 1],
                    NumTails = 0,
                    LifeTime = LifeTimeFrames,
                    Scale = MainSpiritScale,
                    TailOpacity = 0f,
                    TailShrinkAccumulator = 0f
                };

                // Visual bolt (smaller scale)
                _spirits[_spiritCount++] = new SpiritBolt
                {
                    Position = startPos,
                    Angle = new Vector3(0f, 0f, angleOffset),
                    Direction = Vector3.Zero,
                    Tails = new TailVertex[MaxTailCapacity + 1],
                    NumTails = 0,
                    LifeTime = LifeTimeFrames,
                    Scale = VisualSpiritScale,
                    TailOpacity = 0f,
                    TailShrinkAccumulator = 0f
                };

                // Create dark light for this main spirit
                _darkLights[i] = new DynamicLight
                {
                    Owner = this,
                    Position = startPos,
                    Color = Vector3.Zero,
                    Radius = DarkLightRadius,
                    Intensity = DarkLightIntensity
                };
            }
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            await ResolveLaserPath();
            _laserModel = await BMDLoader.Instance.Prepare(_laserModelPath);
            if (_laserModel == null)
            {
                string[] fallbackModels =
                [
                    "Skill/Laser01.bmd",
                    "Skill/Laser1.bmd",
                    "Skill/Laser.bmd"
                ];

                for (int i = 0; i < fallbackModels.Length && _laserModel == null; i++)
                {
                    _laserModel = await BMDLoader.Instance.Prepare(fallbackModels[i]);
                    if (_laserModel != null)
                        _laserModelPath = fallbackModels[i];
                }
            }

            _ = await TextureLoader.Instance.Prepare(JointSpiritTexturePath);
            _spiritTexture = TextureLoader.Instance.GetTexture2D(JointSpiritTexturePath) ?? GraphicsManager.Instance.Pixel;

            _effect = new BasicEffect(GraphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                LightingEnabled = false
            };

            // Add dark lights to terrain
            if (World?.Terrain != null && !_lightsAdded && _initialized)
            {
                for (int i = 0; i < SpiritCount; i++)
                {
                    if (_darkLights[i] != null)
                        World.Terrain.AddDynamicLight(_darkLights[i]);
                }
                _lightsAdded = true;
            }

            EnsureSpiritHeads();
        }

        private async Task ResolveLaserPath()
        {
            if (_laserPathResolved)
                return;

            string[] candidates =
            [
                DefaultLaserModelPath,
                "Skill/Laser1.bmd",
                "Skill/Laser.bmd"
            ];

            for (int i = 0; i < candidates.Length; i++)
            {
                if (await BMDLoader.Instance.AssestExist(candidates[i]))
                {
                    _laserModelPath = candidates[i];
                    _laserPathResolved = true;
                    return;
                }
            }

            _laserPathResolved = true;
        }

        private void RemoveDarkLights()
        {
            if (!_lightsAdded || World?.Terrain == null)
                return;

            for (int i = 0; i < SpiritCount; i++)
            {
                if (_darkLights[i] != null)
                    World.Terrain.RemoveDynamicLight(_darkLights[i]);
            }
            _lightsAdded = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Status == GameControlStatus.NonInitialized)
                _ = Load();

            if (Status != GameControlStatus.Ready)
                return;

            if (!_initialized)
                InitializeSpirits();

            // Add lights after spirits are initialized
            if (_initialized && !_lightsAdded && World?.Terrain != null)
            {
                for (int i = 0; i < SpiritCount; i++)
                {
                    if (_darkLights[i] != null)
                        World.Terrain.AddDynamicLight(_darkLights[i]);
                }
                _lightsAdded = true;
            }

            EnsureSpiritHeads();

            float frameFactor = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            int targetTailCount = _targetTailCount;

            if (!_soundPlayed)
            {
                SoundController.Instance.PlayBuffer(SoundEvil);
                _soundPlayed = true;
            }

            if (_caster.Status == GameControlStatus.Disposed || _caster.World == null)
            {
                RemoveSelf();
                return;
            }

            Vector3 targetPos = _caster.WorldPosition.Translation;
            targetPos.Z += TargetZOffset;

            bool anyAlive = false;
            for (int i = 0; i < _spiritCount; i++)
            {
                ref var spirit = ref _spirits[i];
                bool spiritActive = false;

                if (spirit.LifeTime > 0f)
                {
                    spiritActive = true;
                    spirit.TailShrinkAccumulator = 0f;
                    spirit.TailOpacity = MathHelper.Clamp(
                        spirit.TailOpacity + TailOpacityRiseSpeed * frameFactor,
                        0f,
                        1f);

                    // Shift tail positions
                    if (spirit.NumTails < targetTailCount)
                        spirit.NumTails++;

                    for (int t = spirit.NumTails - 1; t > 0; t--)
                    {
                        spirit.Tails[t] = spirit.Tails[t - 1];
                    }

                    // Create new tail at current position
                    CreateTail(ref spirit);

                    // MoveHumming - adjust angle towards target
                    MoveHumming(ref spirit.Position, ref spirit.Angle, targetPos, HummingTurnRate * frameFactor);

                    // Random perturbation
                    spirit.Direction.X += (MuGame.Random.Next(32) - 16) * 0.2f;
                    spirit.Direction.Z += (MuGame.Random.Next(32) - 16) * 0.8f;

                    spirit.Angle.X += spirit.Direction.X * frameFactor;
                    spirit.Angle.Z += spirit.Direction.Z * frameFactor;

                    spirit.Direction.X *= 0.6f;
                    spirit.Direction.Z *= 0.8f;

                    // Move forward
                    float yawRad = MathHelper.ToRadians(spirit.Angle.Z);
                    float pitchRad = MathHelper.ToRadians(spirit.Angle.X);

                    Vector3 forward = new Vector3(
                        MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                        -MathF.Cos(yawRad) * MathF.Cos(pitchRad),
                        -MathF.Sin(pitchRad)
                    );

                    spirit.Position += forward * Velocity * frameFactor;

                    // Height constraints
                    if (World?.Terrain != null)
                    {
                        float terrainHeight = World.Terrain.RequestTerrainHeight(spirit.Position.X, spirit.Position.Y);
                        if (spirit.Position.Z < terrainHeight + 100f)
                        {
                            spirit.Direction.X = 0f;
                            spirit.Angle.X = -5f;
                        }
                        if (spirit.Position.Z > terrainHeight + 400f)
                        {
                            spirit.Direction.X = 0f;
                            spirit.Angle.X = 5f;
                        }
                    }

                    spirit.LifeTime -= frameFactor;
                }
                else
                {
                    // MuMain deletes the joint when its 49-frame lifetime ends.
                    // Additional flight and a shrinking trail extended this cast.
                    spirit.NumTails = 0;
                    spirit.TailOpacity = 0f;
                }

                // Update corresponding dark light (main spirits at even indices: 0,2,4,6 -> light 0,1,2,3)
                if (i % 2 == 0)
                {
                    int lightIdx = i / 2;

                    if (_spiritHeadsSpawned)
                    {
                        bool forceFade = spirit.LifeTime <= 0f || !spiritActive;
                        UpdateSpiritHead(lightIdx, spirit, targetTailCount, forceFade);
                    }

                    if (_darkLights[lightIdx] != null)
                    {
                        _darkLights[lightIdx].Position = spirit.Position;

                        // MuMain adds -(rand() % 4 + 4) * .01 terrain light
                        // over four tiles; the old custom wake was over ten times darker.
                        float darkness = spiritActive ? -(MuGame.Random.Next(4) + 4) * 0.01f : 0f;
                        _darkLights[lightIdx].Color = new Vector3(darkness, darkness, darkness);
                        _darkLights[lightIdx].Intensity = DarkLightIntensity;
                    }
                }

                if (spiritActive)
                    anyAlive = true;
            }

            UpdateBounds();

            if (!anyAlive)
            {
                RemoveSelf();
            }
        }

        private static int ResolveTargetTailCount(float frameFactor)
        {
            float safeFactor = MathF.Max(frameFactor, 0.01f);
            int scaled = (int)(BaseMaxTails / safeFactor);
            return Math.Clamp(scaled, 2, MaxTailCapacity);
        }

        private void EnsureSpiritHeads()
        {
            if (World == null || !_laserPathResolved || _laserModel == null || _spiritCount <= 0)
                return;

            int expectedHeads = Math.Min(SpiritCount, _spiritCount / 2);
            if (expectedHeads <= 0)
                return;

            for (int i = 0; i < expectedHeads; i++)
            {
                int spiritIdx = i * 2;
                Vector3 spawnPosition = _spirits[spiritIdx].Position;

                EnsureHeadLayer(
                    ref _spiritHeadCore[i],
                    _laserModel,
                    spawnPosition,
                    1.3f,
                    1.0f,
                    1.0f,
                    1.0f);

                // MuMain emits a single MODEL_LASER at scale 1.3 per main spirit.
                // Enlarged translucent duplicate heads were a custom addition.
            }

            _spiritHeadsSpawned = true;
        }

        private void UpdateSpiritHead(int headIndex, in SpiritBolt spirit, int targetTailCount, bool forceFadeOut = false)
        {
            if ((uint)headIndex >= SpiritCount)
                return;

            // MODEL_LASER copies the joint light (LifeTime * .1) into
            // BlendMeshLight. A .45 floor left heads visible at cast expiry.
            float brightness = forceFadeOut ? 0f : spirit.LifeTime * 0.1f;

            UpdateSpiritHeadLayer(_spiritHeadCore[headIndex], spirit, 0, brightness);
            UpdateSpiritHeadLayer(_spiritHeadBlurA[headIndex], spirit, 2, brightness * 0.86f);
            UpdateSpiritHeadLayer(_spiritHeadBlurB[headIndex], spirit, 4, brightness * 0.72f);
        }

        private void RemoveSpiritHeads()
        {
            for (int i = 0; i < SpiritCount; i++)
            {
                RemoveHeadLayer(ref _spiritHeadCore[i]);
                RemoveHeadLayer(ref _spiritHeadBlurA[i]);
                RemoveHeadLayer(ref _spiritHeadBlurB[i]);
            }

            _spiritHeadsSpawned = false;
        }

        private void EnsureHeadLayer(
            ref EvilSpiritGhostModel? layer,
            BMD model,
            Vector3 position,
            float scale,
            float alphaMin,
            float alphaMax,
            float brightnessFactor)
        {
            if (layer != null && layer.Status != GameControlStatus.Disposed)
                return;

            var head = new EvilSpiritGhostModel(model, scale, alphaMin, alphaMax, brightnessFactor)
            {
                Position = position
            };

            layer = head;
            World!.Objects.Add(head);
            _ = head.Load();
        }

        private static void RemoveHeadLayer(ref EvilSpiritGhostModel? layer)
        {
            if (layer == null)
                return;

            if (layer.World != null)
                layer.World.RemoveObject(layer);

            layer.Dispose();
            layer = null;
        }

        private static void UpdateSpiritHeadLayer(
            EvilSpiritGhostModel? head,
            in SpiritBolt spirit,
            int trailOffset,
            float brightness)
        {
            if (head == null || head.Status == GameControlStatus.Disposed)
                return;

            head.Position = ResolveTrailPosition(in spirit, trailOffset);
            head.Angle = new Vector3(
                MathHelper.ToRadians(spirit.Angle.X),
                0f,
                MathHelper.ToRadians(spirit.Angle.Z));
            head.SetBrightness(brightness);
        }

        private static Vector3 ResolveTrailPosition(in SpiritBolt spirit, int trailOffset)
        {
            if (trailOffset <= 0 || spirit.NumTails <= trailOffset)
                return spirit.Position;

            ref readonly TailVertex tail = ref spirit.Tails[trailOffset];
            return (tail.V0 + tail.V1 + tail.V2 + tail.V3) * 0.25f;
        }

        /// <summary>
        /// Creates tail vertices at current position forming a cross shape.
        /// Based on original CreateTail function in ZzzEffectJoint.cpp
        /// </summary>
        private void CreateTail(ref SpiritBolt spirit)
        {
            float halfScale = spirit.Scale * 0.5f;

            // Build rotation matrix from angles
            Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(spirit.Angle.X)) *
                             Matrix.CreateRotationZ(MathHelper.ToRadians(spirit.Angle.Z));

            // Create 4 offset vectors forming a cross
            Vector3 left = Vector3.Transform(new Vector3(-halfScale, 0, 0), rotation);
            Vector3 right = Vector3.Transform(new Vector3(halfScale, 0, 0), rotation);
            Vector3 down = Vector3.Transform(new Vector3(0, 0, -halfScale), rotation);
            Vector3 up = Vector3.Transform(new Vector3(0, 0, halfScale), rotation);

            spirit.Tails[0] = new TailVertex
            {
                V0 = spirit.Position + left,
                V1 = spirit.Position + right,
                V2 = spirit.Position + down,
                V3 = spirit.Position + up
            };
        }

        private static void MoveHumming(ref Vector3 position, ref Vector3 angle, Vector3 targetPosition, float turn)
        {
            float dx = targetPosition.X - position.X;
            float dy = targetPosition.Y - position.Y;
            float targetYaw = MathHelper.ToDegrees(MathF.Atan2(dx, -dy));

            angle.Z = TurnAngle(angle.Z, targetYaw, turn);

            float horizontalDist = MathF.Sqrt(dx * dx + dy * dy);
            float dz = targetPosition.Z - position.Z;
            // MuMain rotates (0, -velocity, 0) around X: positive pitch
            // moves down. Its MoveHumming therefore negates the elevation.
            float targetPitch = -MathHelper.ToDegrees(MathF.Atan2(dz, horizontalDist));

            angle.X = TurnAngle(angle.X, targetPitch, turn);
        }

        private static float TurnAngle(float current, float target, float maxTurn)
        {
            float diff = target - current;
            while (diff > 180f) diff -= 360f;
            while (diff < -180f) diff += 360f;

            if (MathF.Abs(diff) <= maxTurn)
                return target;

            return current + MathF.Sign(diff) * maxTurn;
        }

        private void UpdateBounds()
        {
            if (_spiritCount == 0)
                return;

            Vector3 min = _spirits[0].Position;
            Vector3 max = _spirits[0].Position;

            for (int i = 1; i < _spiritCount; i++)
            {
                if (_spirits[i].LifeTime > 0)
                {
                    min = Vector3.Min(min, _spirits[i].Position);
                    max = Vector3.Max(max, _spirits[i].Position);
                }
            }

            Position = (min + max) * 0.5f;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible || _spiritTexture == null || _effect == null)
                return;

            var camera = Camera.Instance;
            if (camera == null)
                return;

            int quadIndex = 0;
            BuildVertices(ref quadIndex);

            if (quadIndex == 0)
                return;

            // Save states
            var prevBlend = GraphicsDevice.BlendState;
            var prevDepth = GraphicsDevice.DepthStencilState;
            var prevRaster = GraphicsDevice.RasterizerState;

            // Apply states for dark spirits
            GraphicsDevice.BlendState = SubtractiveBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            _effect.View = camera.View;
            _effect.Projection = camera.Projection;
            _effect.World = Matrix.Identity;
            _effect.Texture = _spiritTexture;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _vertices, 0, quadIndex * 4,
                    _indices, 0, quadIndex * 2);
            }

            // Restore states
            GraphicsDevice.BlendState = prevBlend;
            GraphicsDevice.DepthStencilState = prevDepth;
            GraphicsDevice.RasterizerState = prevRaster;
        }

        private void BuildVertices(ref int quadIndex)
        {
            for (int i = 0; i < _spiritCount; i++)
            {
                // Optional reduction of visual clutter: skip the secondary thin ribbon layer.
                if (!RenderVisualRibbonLayer && (i & 1) != 0)
                    continue;

                ref var spirit = ref _spirits[i];

                if (spirit.NumTails < 2)
                    continue;

                float luminosity = MathHelper.Clamp(spirit.LifeTime * 0.1f, 0f, 1f);
                float tailDenominator = Math.Max(1f, _targetTailCount - 1f);

                for (int j = 0; j < spirit.NumTails - 1; j++)
                {
                    ref var current = ref spirit.Tails[j];
                    ref var next = ref spirit.Tails[j + 1];

                    // MuMain's ordinary spirit joints use one face colour and
                    // alpha 1. Texture coordinates, rather than a custom vertex
                    // fade, shape the wake along the occupied tail points.
                    Color c1 = new Color(luminosity, luminosity, luminosity, 1f);
                    Color c2 = c1;
                    float u1 = (spirit.NumTails - 1 - j) / tailDenominator;
                    float u2 = (spirit.NumTails - 2 - j) / tailDenominator;

                    // FACE ONE: Quad from V2-V3 (vertical ribbon)
                    if (quadIndex * 4 + 4 <= _vertices.Length)
                    {
                        int vi = quadIndex * 4;
                        _vertices[vi + 0] = new VertexPositionColorTexture(current.V2, c1, new Vector2(u1, 1f));
                        _vertices[vi + 1] = new VertexPositionColorTexture(current.V3, c1, new Vector2(u1, 0f));
                        _vertices[vi + 2] = new VertexPositionColorTexture(next.V3, c2, new Vector2(u2, 0f));
                        _vertices[vi + 3] = new VertexPositionColorTexture(next.V2, c2, new Vector2(u2, 1f));
                        quadIndex++;
                    }

                    // FACE TWO: Quad from V0-V1 (horizontal ribbon)
                    if (quadIndex * 4 + 4 <= _vertices.Length)
                    {
                        int vi = quadIndex * 4;
                        _vertices[vi + 0] = new VertexPositionColorTexture(current.V0, c1, new Vector2(u1, 0f));
                        _vertices[vi + 1] = new VertexPositionColorTexture(current.V1, c1, new Vector2(u1, 1f));
                        _vertices[vi + 2] = new VertexPositionColorTexture(next.V1, c2, new Vector2(u2, 1f));
                        _vertices[vi + 3] = new VertexPositionColorTexture(next.V0, c2, new Vector2(u2, 0f));
                        quadIndex++;
                    }
                }
            }
        }

        private void RemoveSelf()
        {
            RemoveDarkLights();
            RemoveSpiritHeads();

            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.RemoveObject(this);

            Dispose();
        }

        public override void Dispose()
        {
            RemoveDarkLights();
            RemoveSpiritHeads();
            _effect?.Dispose();
            _effect = null;
            base.Dispose();
        }

        private sealed class EvilSpiritGhostModel : ModelObject
        {
            private readonly BMD _model;
            private readonly float _alphaMin;
            private readonly float _alphaMax;
            private readonly float _brightnessFactor;
            protected override bool AllowDynamicLightingShader => false;
            protected override bool AllowLightingUpdates => false;

            public EvilSpiritGhostModel(
                BMD model,
                float scale,
                float alphaMin,
                float alphaMax,
                float brightnessFactor)
            {
                _model = model ?? throw new ArgumentNullException(nameof(model));
                _alphaMin = MathHelper.Clamp(alphaMin, 0f, 1f);
                _alphaMax = MathHelper.Clamp(Math.Max(alphaMax, _alphaMin), 0f, 1f);
                _brightnessFactor = brightnessFactor;

                // Each native head lives one frame and starts at animation zero.
                ContinuousAnimation = false;
                AnimationSpeed = 0f;
                Scale = scale;

                LightEnabled = false;
                // Unlit meshes still use their explicit vertex colour. Zero
                // source colour makes GL_ZERO / ONE_MINUS_SRC_COLOR invisible.
                Light = Vector3.One;
                IsTransparent = true;
                DepthState = DepthStencilState.DepthRead;
                BlendState = SubtractiveBlend;
                BlendMeshState = SubtractiveBlend;
                BlendMesh = 0;
                BlendMeshLight = 0.9f;
                Alpha = (_alphaMin + _alphaMax) * 0.5f;
                UseSunLight = false;
                RenderShadow = false;
            }

            public override async Task Load()
            {
                Model = _model;
                await base.Load();
            }

            public void SetBrightness(float brightness)
            {
                float adjusted = Math.Max(0f, brightness * _brightnessFactor);
                BlendMeshLight = adjusted;
                Alpha = MathHelper.Clamp(_alphaMin + adjusted * (_alphaMax - _alphaMin), _alphaMin, _alphaMax);
            }
        }
    }
}
