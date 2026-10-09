#nullable enable
using System;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects.Joints
{
    /// <summary>
    /// Faithful port of the SourceMain5.2 JOINT primitive (ZzzEffectJoint.cpp) for the
    /// families used by monster attacks: JOINT_SPIRIT, JOINT_THUNDER, JOINT_LASER+1 and
    /// BITMAP_FLARE. One instance mirrors one C++ JOINT object: forward flight along
    /// Angle, MoveHumming homing, tail-ribbon history, frame-based lifetime.
    /// </summary>
    public sealed class SourceJointEffect : EffectObject
    {
        public enum JointFamily { Spirit, Thunder, Laser1, Flare }

        private const int MaxRingCapacity = 50;

        private Texture2D? _texture;
        private Texture2D? _fortitudeFlare;
        private readonly Vector3[][] _rings = new Vector3[MaxRingCapacity][];
        private int _ringHead;
        private int _ringCount;
        private VertexPositionColorTexture[]? _fenrirVertices;

        public JointFamily Family { get; set; }
        public int SubType { get; set; }

        public Func<Vector3>? TargetProvider { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float Velocity { get; set; }
        public float LifeTimeFrames { get; set; }
        public float ScaleValue { get; set; } = 10f;
        public int MaxTails { get; set; }
        public Vector3 LightTint { get; set; } = Vector3.One;
        public string JointTexturePath { get; set; } = "Effect/JointThunder01.jpg";

        // C++ working state
        private Vector3 _directionWobble;
        private Vector3 _startPosition;
        private bool _collision;
        private float _luminosityBoost;

        private SourceJointEffect()
        {
            for (int i = 0; i < MaxRingCapacity; i++)
                _rings[i] = new Vector3[4];

            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-5000f, -5000f, -2000f),
                new Vector3(5000f, 5000f, 4000f));
        }

        // ---- factories mirroring CreateJoint call sites -----------------------

        /// <summary>Greater Fortitude's accelerating JOINT_SPIRIT sub2.</summary>
        public static SourceJointEffect FortitudeSpirit(Vector3 origin, float yawDeg) => new()
        {
            Family = JointFamily.Spirit,
            SubType = 2,
            Position = origin,
            TargetPosition = origin,
            Velocity = 50f,
            LifeTimeFrames = 20f,
            ScaleValue = 60f,
            MaxTails = 3,
            LightTint = new Vector3(1f, 0.5f, 0.1f),
            JointTexturePath = "Effect/JointSpirit01.jpg",
            _angle = new Vector3(-10f, 0f, yawDeg),
            _startPosition = origin
        };

        /// <summary>BITMAP_JOINT_SPIRIT sub1 with NULL target: radial burst streaks
        /// (Phantom Knight 36x, scale 60, Velocity 70, LifeTime 49).</summary>
        public static SourceJointEffect SpiritBurst(Vector3 origin, float yawDeg, float pitchDeg, float scale)
        {
            return new SourceJointEffect
            {
                Family = JointFamily.Spirit,
                SubType = 1,
                Position = origin,
                TargetPosition = origin,
                Velocity = 70f,
                LifeTimeFrames = 49f,
                ScaleValue = scale,
                MaxTails = 6,
                JointTexturePath = "Effect/JointSpirit01.jpg",
                _angle = new Vector3(pitchDeg, 0f, yawDeg),
                _startPosition = origin
            };
        }

        /// <summary>JOINT_SPIRIT sub3 / SPIRIT2: fast homing spirit (Velocity 140,
        /// LifeTime 49, MaxTails 10); SPIRIT2 renders white, SPIRIT orange.</summary>
        public static SourceJointEffect SpiritHoming(Func<Vector3> originProvider, Func<Vector3> targetProvider, bool whiteSpirit, float scale)
        {
            var joint = new SourceJointEffect
            {
                Family = JointFamily.Spirit,
                SubType = 3,
                TargetProvider = targetProvider,
                Velocity = 140f,
                LifeTimeFrames = 49f,
                ScaleValue = scale,
                MaxTails = 10,
                JointTexturePath = "Effect/JointSpirit01.jpg",
                _originProvider = originProvider,
                LightTint = whiteSpirit ? Vector3.One : new Vector3(1f, 0.5f, 0.1f)
            };
            return joint;
        }

        /// <summary>JOINT_THUNDER sub2-style homing arc toward a live target
        /// (Velocity 50, jagged tails).</summary>
        public static SourceJointEffect ThunderHoming(Vector3 origin, Func<Vector3> targetProvider, float scale, float lifeFrames = 8f)
        {
            var target = targetProvider();
            return new SourceJointEffect
            {
                Family = JointFamily.Thunder,
                SubType = 2,
                Position = origin,
                TargetPosition = target + Vector3.UnitZ * 80f,
                TargetProvider = targetProvider,
                Velocity = 50f,
                LifeTimeFrames = lifeFrames,
                ScaleValue = scale,
                MaxTails = 50,
                JointTexturePath = "Effect/JointThunder01.jpg",
                LightTint = new Vector3(1f, 0.6f, 0.2f)
            };
        }

        /// <summary>Fenrir's long, jagged thunder/flash ribbons, generated up to the target each tick.</summary>
        public static SourceJointEffect FenrirThunder(Vector3 origin, Func<Vector3> targetProvider,
            float scale, Vector3 tint, bool flash)
        {
            var joint = ThunderHoming(origin, targetProvider, scale, 20f);
            joint.SubType = 76;
            joint._startPosition = origin;
            joint._fenrirPosition = origin;
            joint._angle = new Vector3(MuGame.Random.Next(360), 0f, MuGame.Random.Next(360));
            joint._fenrirVertices = new VertexPositionColorTexture[(MaxRingCapacity - 1) * 12];
            joint.LightTint = tint;
            joint.JointTexturePath = flash ? "Effect/Flashing.jpg" : "Effect/JointThunder01.jpg";
            return joint;
        }

        /// <summary>MuMain BITMAP_FLARE_FORCE subtype 11-13 around Fenrir.</summary>
        public static SourceJointEffect FenrirFlare(Vector3 origin, int fenrirType)
        {
            int wait = MuGame.Random.Next(2, 5);
            var joint = new SourceJointEffect
            {
                Family = JointFamily.Thunder, SubType = 77, ScaleValue = 60f,
                MaxTails = 30, LifeTimeFrames = 20f + wait, Velocity = -3f,
                JointTexturePath = "Effect/JointThunder01.jpg",
                LightTint = fenrirType == 1 ? new Vector3(1f, 0.6f, 0.6f) :
                    fenrirType == 2 ? new Vector3(0.7f, 0.7f, 1f) : new Vector3(0.7f, 1f, 0.7f),
                _angle = new Vector3(MuGame.Random.Next(360), 0f, MuGame.Random.Next(360)),
                _startPosition = origin + Vector3.UnitZ * 150f,
                _flareWait = wait,
                _fenrirVertices = new VertexPositionColorTexture[(MaxRingCapacity - 1) * 12]
            };
            joint.Position = joint._startPosition + SourceJointMath.MuMainRotate(new Vector3(0f, 0f, 80f), new Vector3(80f, -180f, 0f));
            return joint;
        }

        /// <summary>BITMAP_JOINT_LASER+1 swarm beam: per-frame humming at speed 25
        /// with jittered tails (BeamKnight/Devil energy bolts).</summary>
        public static SourceJointEffect LaserSwarm(Vector3 origin, Func<Vector3> targetProvider, float scale, bool redTint)
        {
            return new SourceJointEffect
            {
                Family = JointFamily.Laser1,
                SubType = redTint ? 1 : 0,
                Position = origin,
                TargetPosition = targetProvider() + Vector3.UnitZ * 80f,
                TargetProvider = targetProvider,
                Velocity = 40f,
                LifeTimeFrames = 20f,
                ScaleValue = scale,
                MaxTails = 20,
                JointTexturePath = "Effect/JointLaser01.jpg",
                LightTint = redTint ? new Vector3(1f, 0.35f, 0.35f) : new Vector3(1f, 0.75f, 0.55f)
            };
        }

        private Vector3 _angle = new(0f, 0f, 0f);
        private float _ageFrames;
        private float _boltRefreshFrames;
        private Vector3 _fenrirPosition;
        private Func<Vector3>? _originProvider;
        private float _flareWait;
        private float _flareRadius = 80f;
        private float _flareOrbit = -180f;
        private int _flareMultiUse = 1;

        public override async Task LoadContent()
        {
            await base.LoadContent();

            _texture = await TextureLoader.Instance.PrepareAndGetTexture(JointTexturePath);
            if (Family == JointFamily.Spirit && SubType == 2)
                _fortitudeFlare = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");

            // Initial tail ring: cross of +-Scale*0.5 around the position (CreateJoint).
            if (SubType is 76 or 77)
                PushFenrirTail(Position, _angle);
            else
                PushRing(Position);
        }

        private void PushFenrirTail(Vector3 center, Vector3 angle)
        {
            SourceJointMath.MuMainAngleBasis(angle, out var right, out _, out var up);
            float half = ScaleValue * 0.5f;
            var ring = _rings[_ringHead];
            ring[0] = center - right * half;
            ring[1] = center + right * half;
            ring[2] = center - up * half;
            ring[3] = center + up * half;
            _ringHead = (_ringHead + 1) % MaxRingCapacity;
            _ringCount = Math.Min(_ringCount + 1, MaxTails);
        }

        private void PushRing(Vector3 center)
        {
            SourceJointMath.AngleBasis(_angle, out var right, out _, out var up);
            float h = ScaleValue * 0.5f;
            var ring = _rings[_ringHead];
            ring[0] = center + right * h;
            ring[1] = center - right * h;
            ring[2] = center + up * h;
            ring[3] = center - up * h;
            _ringHead = (_ringHead + 1) % MaxRingCapacity;
            if (_ringCount < (SubType == 2 && Family == JointFamily.Spirit ? MaxTails : MaxRingCapacity))
                _ringCount++;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Status != GameControlStatus.Ready)
                return;

            float f = SourceJointMath.FrameFactor;
            _ageFrames += f;

            // Live target refresh (C++ copies o->Target->Position each move).
            if (TargetProvider != null)
            {
                TargetPosition = TargetProvider();
                if (Family != JointFamily.Flare)
                    TargetPosition += Vector3.UnitZ * 80f;
            }

            switch (Family)
            {
                case JointFamily.Spirit:
                    MoveSpirit(f);
                    break;
                case JointFamily.Thunder:
                    MoveThunderHoming(f);
                    break;
                case JointFamily.Laser1:
                    MoveLaserSwarm(f);
                    break;
                case JointFamily.Flare:
                    // FLARE sub7 has no movement case: generic flight with Velocity 0
                    // leaves it coiling at the spawn point.
                    break;
            }

            // Generic pre-switch forward flight (skipped when handled above already moved).
            if (Velocity != 0f && Family == JointFamily.Spirit && SubType == 1)
            {
                Vector3 local = new(0f, -Velocity, 0f);
                Position += SourceJointMath.Rotate(local, _angle) * f;
            }

            if (SubType is not (76 or 77) && (LifeTimeFrames > 0 || Family != JointFamily.Spirit))
                PushRing(Position);

            LifeTimeFrames -= f;
            if (LifeTimeFrames < 0f || _ageFrames > 12f && _ringCount == 0)
            {
                World?.RemoveObject(this);
                Dispose();
            }
        }

        private void MoveSpirit(float f)
        {
            if (SubType == 2)
            {
                if (LifeTimeFrames < 10f)
                    LightTint *= MathF.Pow(1f / 1.2f, f);
                Velocity += 5f * f;
                Position += SourceJointMath.Rotate(new Vector3(0f, -Velocity, 0f), _angle) * f;
                return;
            }
            if (SubType == 3 && TargetProvider != null)
            {
                // sub3: MoveHumming(10) toward target + wobble, terrain clamps.
                Vector3 posS3 = Position; Vector3 angS3 = _angle;
                SourceJointMath.MoveHumming(ref posS3, ref angS3, TargetPosition, 10f, f);
                Position = posS3; _angle = angS3;
                _directionWobble.X += (MuGame.Random.Next(32) - 16) * 0.2f;
                _directionWobble.Z += (MuGame.Random.Next(32) - 16) * 0.8f;
                _angle.X += _directionWobble.X * f;
                _angle.Z += _directionWobble.Z * f;
                _directionWobble.X *= 0.6f;
                _directionWobble.Z *= 0.8f;

                if (World?.Terrain != null)
                {
                    float height = World.Terrain.RequestTerrainHeight(Position.X, Position.Y);
                    if (Position.Z < height + 100f) { _directionWobble.X = 0f; _angle.X = -5f; }
                    if (Position.Z > height + 400f) { _directionWobble.X = 0f; _angle.X = 5f; }
                }

                Vector3 local = new(0f, -Velocity, 0f);
                Position += SourceJointMath.Rotate(local, _angle) * f;
            }
            else if (SubType == 1)
            {
                // NULL-target burst: straight flight only (generic mover), plus the
                // per-frame BITMAP_LIGHT twinkle from the C++ `else if (1 == o->SubType)`
                // branch — reproduced in Draw as a flickering sprite at the head.
                _luminosityBoost = 0.9f + (float)MuGame.Random.NextDouble() * 0.1f;
            }
        }

        private void MoveThunderHoming(float f)
        {
            if (SubType == 77)
            {
                _boltRefreshFrames += f;
                while (_boltRefreshFrames >= 1f)
                {
                    _boltRefreshFrames -= 1f;
                    if (_ringCount < MaxTails)
                    {
                        if (_flareWait > 0f)
                            _flareWait -= 1f;
                        else
                        {
                            for (int i = 1; i < _flareMultiUse; i++)
                            {
                                PushFenrirTail(Position, _angle);
                                _startPosition += SourceJointMath.MuMainRotate(new Vector3(0f, Velocity, 0f), _angle);
                                _flareOrbit -= 20f;
                                Position = _startPosition + SourceJointMath.MuMainRotate(
                                    new Vector3(0f, 0f, _flareRadius), new Vector3(0f, _flareOrbit, _angle.Z));
                                Velocity -= 2f;
                                _flareRadius -= 2.5f;
                            }
                            _flareMultiUse += 2;
                        }
                    }
                    if (LifeTimeFrames < 10f)
                        LightTint /= 1.3f;
                }
                return;
            }
            if (SubType == 76)
            {
                // Run MuMain's MoveJoint at its source tick rate. High render FPS must
                // not insert extra stationary tails after arriving at the target.
                _boltRefreshFrames += f;
                while (_boltRefreshFrames >= 1f)
                {
                    _boltRefreshFrames -= 1f;
                    for (int j = 0; j < MaxTails; j++)
                    {
                        float distance = SourceJointMath.MuMainMoveHumming(ref _fenrirPosition, ref _angle, TargetPosition, 50f, 1f);
                        Vector3 jitter = new Vector3(MuGame.Random.Next(-512, 512) / ScaleValue, 0f,
                            MuGame.Random.Next(-512, 512) / ScaleValue);
                        Vector3 tailAngle = _angle + jitter;
                        PushFenrirTail(_fenrirPosition, tailAngle);
                        if (distance < Velocity * 1.5f)
                            break;
                        _fenrirPosition += SourceJointMath.MuMainRotate(new Vector3(0f, -Velocity, 0f), tailAngle);
                    }
                }
                Position = _fenrirPosition;
                return;
            }
            if (TargetProvider != null)
            {
                Vector3 posT2 = Position;
                Vector3 angT2 = _angle;
                SourceJointMath.MoveHumming(ref posT2, ref angT2, TargetPosition, 50f, f);
                Position = posT2;
                _angle = angT2;
            }

            Vector3 local = new(0f, -Velocity, 0f);
            Position += SourceJointMath.Rotate(local, _angle) * f;
        }

        private void MoveLaserSwarm(float f)
        {
            // C++ loops j < MaxTails doing humming(25)+jitter per iteration; one
            // iteration per frame keeps the same visual pace without overdraw.
            Vector3 posL1 = Position; Vector3 angL1 = _angle;
            SourceJointMath.MoveHumming(ref posL1, ref angL1, TargetPosition, 25f, f);
            Position = posL1; _angle = angL1;
            _directionWobble.X += f * (MuGame.Random.Next(256) - 128) / MathF.Max(ScaleValue, 1f);
            _directionWobble.Z += f * (MuGame.Random.Next(256) - 128) / MathF.Max(ScaleValue, 1f);
            _directionWobble *= MathF.Pow(0.8f, f);
            _angle.X += _directionWobble.X;
            _angle.Z += _directionWobble.Z;

            Vector3 local = new(0f, -Velocity, 0f);
            Position += SourceJointMath.Rotate(local, _angle) * f;

            float distance = Vector3.Distance(Position, TargetPosition);
            if (!_collision && distance <= Velocity * 2f * f)
                _collision = true; // arrival flash point (fire particle in C++)
        }

        public override void Draw(GameTime gameTime)
        {
            if (SubType is not (76 or 77) || Hidden || Status != GameControlStatus.Ready || _texture == null || _fenrirVertices == null)
                return;
            int count = BuildFenrirVertices((float)((long)gameTime.TotalGameTime.TotalMilliseconds % 1000L) * 0.001f, SourceJointMath.FrameFactor);
            if (count == 0)
                return;
            var gd = GraphicsDevice;
            var effect = GraphicsManager.Instance.AlphaTestEffect3D;
            var blend = gd.BlendState;
            var depth = gd.DepthStencilState;
            var raster = gd.RasterizerState;
            var sampler = gd.SamplerStates[0];
            var world = effect.World;
            var view = effect.View;
            var projection = effect.Projection;
            var texture = effect.Texture;
            var diffuse = effect.DiffuseColor;
            float alpha = effect.Alpha;
            bool vertexColor = effect.VertexColorEnabled;
            try
            {
                gd.BlendState = Blendings.OneOneAdditive;
                gd.DepthStencilState = DepthStencilState.DepthRead;
                gd.RasterizerState = RasterizerState.CullNone;
                gd.SamplerStates[0] = SamplerState.LinearWrap;
                effect.World = Matrix.Identity;
                effect.View = Camera.Instance.View;
                effect.Projection = Camera.Instance.Projection;
                effect.Texture = _texture;
                effect.DiffuseColor = LightTint;
                effect.Alpha = 1f;
                effect.VertexColorEnabled = true;
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    gd.DrawUserPrimitives(PrimitiveType.TriangleList, _fenrirVertices, 0, count / 3);
                }
            }
            finally
            {
                gd.BlendState = blend;
                gd.DepthStencilState = depth;
                gd.RasterizerState = raster;
                gd.SamplerStates[0] = sampler;
                effect.World = world;
                effect.View = view;
                effect.Projection = projection;
                effect.Texture = texture;
                effect.DiffuseColor = diffuse;
                effect.Alpha = alpha;
                effect.VertexColorEnabled = vertexColor;
            }
        }

        // MuMain RenderJoints: two crossed faces, newest-to-oldest tails, tiled U
        // coordinates (NumTails-j)/16, doubled and scrolled for bTileMapping.
        private int BuildFenrirVertices(float scroll, float frameFactor)
        {
            int count = 0;
            int tails = _ringCount - 1;
            float uvScale = MathF.Pow(2f, frameFactor);
            for (int j = 0; j < tails; j++)
            {
                var current = _rings[(_ringHead - 1 - j + MaxRingCapacity) % MaxRingCapacity];
                var next = _rings[(_ringHead - 2 - j + MaxRingCapacity) % MaxRingCapacity];
                var center = (current[0] + current[1]) * 0.5f;
                var nextCenter = (next[0] + next[1]) * 0.5f;
                if (Vector3.DistanceSquared(center, nextCenter) > 60f * 60f)
                    continue;
                float u1 = SubType == 77 ? (tails - j) / (float)((MaxTails - 1) / 2) - scroll : ((tails - j) / 16f - scroll) * uvScale;
                float u2 = SubType == 77 ? (tails - j - 1) / (float)((MaxTails - 1) / 2) - scroll : ((tails - j - 1) / 16f - scroll) * uvScale;
                float intensity = SubType == 77 ? MathHelper.Clamp((tails - 1 - j) / (float)MaxTails * 2f, 0f, 1f) : 1f;
                AddFenrirQuad(ref count, current[2], current[3], next[3], next[2], u1, u2, 1f, 0f, intensity);
                AddFenrirQuad(ref count, current[0], current[1], next[1], next[0], u1, u2, 0f, 1f, intensity);
            }
            return count;
        }

        private void AddFenrirQuad(ref int count, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            float u1, float u2, float v1, float v2, float intensity)
        {
            var vertices = _fenrirVertices!;
            var color = new Color(intensity, intensity, intensity, 1f);
            var va = new VertexPositionColorTexture(a, color, new Vector2(u1, v1));
            var vb = new VertexPositionColorTexture(b, color, new Vector2(u1, v2));
            var vc = new VertexPositionColorTexture(c, color, new Vector2(u2, v2));
            var vd = new VertexPositionColorTexture(d, color, new Vector2(u2, v1));
            vertices[count++] = va; vertices[count++] = vb; vertices[count++] = vc;
            vertices[count++] = va; vertices[count++] = vc; vertices[count++] = vd;
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (SubType is 76 or 77)
                return;
            if (Hidden || Status != GameControlStatus.Ready || _texture == null || _ringCount < 2)
                return;

            var spriteBatch = GraphicsManager.Instance.Sprite;
            bool ownBatch = !SpriteBatchScope.BatchIsBegun;
            if (ownBatch)
            {
                using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullNone))
                    DrawRibbon(gameTime, spriteBatch);
            }
            else
                DrawRibbon(gameTime, spriteBatch);

            base.DrawAfter(gameTime);
        }

        private void DrawRibbon(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var camera = Camera.Instance;
            var viewport = GraphicsDevice.Viewport;
            if (camera == null)
                return;

            float scroll = (float)((long)gameTime.TotalGameTime.TotalMilliseconds % 1000L) * 0.001f;
            float fade = MathHelper.Clamp(LifeTimeFrames / 10f, 0f, 1f);

            int oldest = (_ringHead - _ringCount + MaxRingCapacity) % MaxRingCapacity;
            Vector3 prevCenter = (_rings[oldest][0] + _rings[oldest][1]) * 0.5f;

            for (int j = 1; j < _ringCount; j++)
            {
                int idx = (oldest + j) % MaxRingCapacity;
                Vector3 center = (_rings[idx][0] + _rings[idx][1]) * 0.5f;

                DrawSegment(spriteBatch, viewport, camera, prevCenter, center,
                    v0: (j - 1) / (float)Math.Max(MaxTails - 1, 1),
                    v1: j / (float)Math.Max(MaxTails - 1, 1),
                    scroll: scroll,
                    fade: fade);

                prevCenter = center;
            }

            // Head sprite + SPIRIT sub1 twinkle.
            if (Family == JointFamily.Spirit && SubType == 2 && _fortitudeFlare != null &&
                TryProject(Position, viewport, camera, out var fortitudeHead))
            {
                float scale = (4f + (20f - LifeTimeFrames) / 5f) * ScreenScale(Position);
                spriteBatch.Draw(_fortitudeFlare, fortitudeHead, null, new Color(LightTint), 0f,
                    new Vector2(_fortitudeFlare.Width * 0.5f, _fortitudeFlare.Height * 0.5f),
                    scale, SpriteEffects.None, 0.45f);
            }
            if (Family == JointFamily.Spirit && SubType == 1)
            {
                if (TryProject(Position, viewport, camera, out var sp))
                {
                    float scale = 4f * ScreenScale(Position) * _luminosityBoost;
                    spriteBatch.Draw(
                        _texture,
                        sp,
                        null,
                        Color.White * 0.7f * fade,
                        MuGame.Random.Next(360) * MathHelper.Pi / 180f,
                        new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f),
                        scale,
                        SpriteEffects.None,
                        0.45f);
                }
            }
        }

        private void DrawSegment(SpriteBatch spriteBatch, Viewport viewport, Camera camera,
            Vector3 from, Vector3 to, float v0, float v1, float scroll, float fade)
        {
            Vector3 midpoint = (from + to) * 0.5f;
            if (!TryProject(midpoint, viewport, camera, out var screen))
                return;

            Vector3 fromS = ProjectOrClamp(from, viewport, camera);
            Vector3 toS = ProjectOrClamp(to, viewport, camera);

            float dx = toS.X - fromS.X;
            float dy = toS.Y - fromS.Y;
            float lengthPx = MathF.Sqrt(dx * dx + dy * dy);
            if (lengthPx < 1f)
                return;

            float widthPx = ScaleValue * ScreenScale(midpoint) * 0.9f;
            float angle = MathF.Atan2(dy, dx);

            // V progress along the ribbon with thunder-style scroll (RenderJoints).
            float light1 = v1 * 2f - scroll;
            float light2 = v0 * 2f - scroll;
            _ = light1; _ = light2; // texture sampling is single-frame; tint carries the glow

            var tint = new Color(
                LightTint.X * fade,
                LightTint.Y * fade,
                LightTint.Z * fade,
                fade);

            spriteBatch.Draw(
                _texture,
                screen,
                null,
                tint,
                angle,
                new Vector2(0f, _texture.Height * 0.5f),
                new Vector2(lengthPx / _texture.Width, widthPx / _texture.Height),
                SpriteEffects.None,
                0.45f);
        }

        private static Vector3 ProjectOrClamp(Vector3 worldPos, Viewport viewport, Camera camera)
        {
            Vector3 p = viewport.Project(worldPos, camera.Projection, camera.View, Matrix.Identity);
            if (p.Z < 0f || p.Z > 1f)
                p = new Vector3(-10000f, -10000f, 1f);
            return p;
        }

        private static bool TryProject(Vector3 worldPos, Viewport viewport, Camera camera, out Vector2 screen)
        {
            Vector3 p = viewport.Project(worldPos, camera.Projection, camera.View, Matrix.Identity);
            if (p.Z < 0f || p.Z > 1f)
            {
                screen = default;
                return false;
            }
            screen = new Vector2(p.X, p.Y);
            return true;
        }

        private static float ScreenScale(Vector3 worldPos)
        {
            float distance = Vector3.Distance(Camera.Instance.Position, worldPos);
            return 1f / (MathF.Max(distance, 0.1f) / Constants.TERRAIN_SIZE) * Constants.RENDER_SCALE;
        }
    }
}
