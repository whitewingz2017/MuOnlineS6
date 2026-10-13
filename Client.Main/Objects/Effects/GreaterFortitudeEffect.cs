#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Models;
using Client.Main.Graphics;
using Client.Main.Controllers;
using Client.Main.Content;
using Client.Main.Objects.Player;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>MuMain SWELL_LIFE: SPIRIT sub2, MAGIC+1 sub4 and FLARE sub2.</summary>
    public sealed class GreaterFortitudeEffect : EffectObject
    {
        private const int SpiritCount = 36;
        private const int SpiritTails = 3;
        private const int FlareTails = 20;
        private const float SourceFps = 25f;
        private readonly WalkerObject _caster;
        private readonly Spirit[] _spirits = new Spirit[SpiritCount];
        private readonly Flare[] _flares = new Flare[SpiritCount];
        private readonly Vector3[,] _spiritTails = new Vector3[SpiritCount, SpiritTails];
        private readonly Vector3[,] _flareTails = new Vector3[SpiritCount, FlareTails];
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[SpiritCount * (FlareTails - 1) * 12];
        private readonly float[] _circleScale = new float[2];
        private Texture2D? _spiritTexture, _flareTexture, _headTexture, _groundTexture;
        private float _accumulator;
        private float _attackTime = 1f;
        private int _circleLife;
        private bool _impactSpawned;
        private Vector3 _origin;
        private struct Spirit
        {
            public Vector3 Position, Direction, Width;
            public int Life, TailHead, TailCount;
            public float Velocity, Brightness, HeadRotation;
            public bool HeadVisible;
        }
        private struct Flare
        {
            public Vector3 Position, Width;
            public int Life, TailHead, TailCount;
            public float Speed, Brightness;
        }

        public GreaterFortitudeEffect(WalkerObject caster)
        {
            _caster = caster;
            Position = caster.WorldPosition.Translation;
            IsTransparent = false; AffectedByTransparency = false;
            BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
            LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-2400, -2400, -300), new Vector3(2400, 2400, 2400));
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            _spiritTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointSpirit01.jpg");
            _flareTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Flare.jpg");
            _headTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
            _groundTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Magic_Ground2.jpg");
        }

        private void SpawnImpact()
        {
            _impactSpawned = true;
            Position = _caster.WorldPosition.Translation;
            _origin = Position + Vector3.UnitZ * 100f;
            _circleLife = 40;
            for (int i = 0; i < 2; i++) _circleScale[i] = MuGame.Random.Next(50, 100) / 100f * 4f;
            for (int i = 0; i < SpiritCount; i++)
            {
                var angle = new Vector3(-10f, 0f, i * 10f);
                _spirits[i] = new Spirit {
                    Position = _origin, Direction = SourceJointMath.MuMainRotate(new Vector3(0, -1, 0), angle),
                    // MuMain sub2 renders FACE_TWO, the rotated X pair only.
                    Width = SourceJointMath.MuMainRotate(Vector3.UnitX * 30f, angle),
                    Velocity = 50f, Life = 20, Brightness = .5f, TailCount = 1 };
                _spiritTails[i, 0] = _origin;
            }
            SoundController.Instance.PlayBuffer("Sound/eSwellLife.wav");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            if (_caster.Status == GameControlStatus.Disposed || _caster is PlayerObject { IsDead: true })
            { Remove(); return; }
            _accumulator = MathF.Min(_accumulator + (float)gameTime.ElapsedGameTime.TotalSeconds * SourceFps, 10f);
            while (_accumulator >= 1f)
            {
                _accumulator -= 1f;
                if (!_impactSpawned)
                {
                    // AttackStage raises AttackTime to 15 after tick nine for VITALITY;
                    // other actions reach the normal tick-15 impact. Start at one as MuMain does.
                    _attackTime += 1f;
                    if (GreaterFortitudeRules.ShouldEmit(_caster.CurrentAction == (int)PlayerAction.PlayerSkillVitality, _attackTime))
                        SpawnImpact();
                    continue;
                }
                bool alive = --_circleLife > 0;
                for (int i = 0; i < SpiritCount; i++)
                {
                    ref var spirit = ref _spirits[i];
                    spirit.HeadVisible = false;
                    if (spirit.Life >= 0)
                    {
                        // Generic MoveJoint flight precedes the subtype switch and acceleration.
                        spirit.Position += spirit.Direction * GreaterFortitudeRules.TakeVelocity(ref spirit.Velocity);
                        GreaterFortitudeRules.FadeTrail(spirit.Life, ref spirit.Brightness);
                        if (spirit.Life == 19) SpawnFlare(i);
                        spirit.HeadVisible = true;
                        spirit.HeadRotation = MathHelper.ToRadians(MuGame.Random.Next(360));
                        spirit.TailHead = (spirit.TailHead + 1) % SpiritTails;
                        spirit.TailCount = Math.Min(spirit.TailCount + 1, SpiritTails);
                        _spiritTails[i, spirit.TailHead] = spirit.Position;
                        spirit.Life--; alive = true;
                    }
                    ref var flare = ref _flares[i];
                    if (flare.Life < 0 || flare.TailCount == 0) continue;
                    if (flare.Life <= 25) { flare.Speed += 5f; flare.Position.Z += flare.Speed; }
                    if (flare.Life < 10) flare.Brightness /= 1.3f;
                    flare.TailHead = (flare.TailHead + 1) % FlareTails;
                    flare.TailCount = Math.Min(flare.TailCount + 1, FlareTails);
                    _flareTails[i, flare.TailHead] = flare.Position;
                    flare.Life--; alive = true;
                }
                if (!alive) { Remove(); return; }
            }
        }

        private void SpawnFlare(int i)
        {
            Vector3 position = _origin + new Vector3(MuGame.Random.Next(-100, 100), MuGame.Random.Next(-100, 100), -200);
            float yaw = MathHelper.ToRadians(MuGame.Random.Next(360));
            _flares[i] = new Flare { Position = position, Life = MuGame.Random.Next(25, 75),
                Speed = MuGame.Random.Next(35, 55), Width = new Vector3(MathF.Cos(yaw), MathF.Sin(yaw), 0) * 20f,
                Brightness = 1f, TailCount = 1 };
            _flareTails[i, 0] = position;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || !_impactSpawned) return;
            int count = 0;
            for (int i = 0; i < SpiritCount; i++)
            {
                ref var spirit = ref _spirits[i];
                if (spirit.Life < 0) continue;
                var tint = new Color(spirit.Brightness, spirit.Brightness, spirit.Brightness);
                for (int tail = 0; tail < spirit.TailCount - 1; tail++)
                {
                    Vector3 a = _spiritTails[i, (spirit.TailHead - tail + SpiritTails) % SpiritTails];
                    Vector3 b = _spiritTails[i, (spirit.TailHead - tail - 1 + SpiritTails) % SpiritTails];
                    Ribbon(ref count, a, b, spirit.Width, tint, (spirit.TailCount - 1 - tail) / (float)(SpiritTails - 1), 1f / (SpiritTails - 1));
                }
            }
            DrawGeometry(_spiritTexture, count); count = 0;
            for (int i = 0; i < SpiritCount; i++)
            {
                ref var flare = ref _flares[i];
                if (flare.Life < 0 || flare.TailCount == 0) continue;
                var tint = new Color(flare.Brightness, flare.Brightness, flare.Brightness);
                for (int tail = 0; tail < flare.TailCount - 1; tail++)
                {
                    Vector3 a = _flareTails[i, (flare.TailHead - tail + FlareTails) % FlareTails];
                    Vector3 b = _flareTails[i, (flare.TailHead - tail - 1 + FlareTails) % FlareTails];
                    float u = (flare.TailCount - 1 - tail) / (float)(FlareTails - 1);
                    Ribbon(ref count, a, b, flare.Width, tint, u, 1f / (FlareTails - 1));
                    Ribbon(ref count, a, b, Vector3.UnitZ * 20f, tint, u, 1f / (FlareTails - 1));
                }
            }
            DrawGeometry(_flareTexture, count);
            DrawCircles();
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || !_impactSpawned || _headTexture == null) return;
            int count = 0;
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            for (int i = 0; i < SpiritCount; i++)
            {
                ref var spirit = ref _spirits[i];
                if (!spirit.HeadVisible) continue;
                int sourceLife = spirit.Life + 1;
                Color tint = sourceLife < 10 ? new Color(spirit.Brightness, spirit.Brightness, spirit.Brightness) : new Color(1f, .5f, .1f);
                float scale = 4f + (20f - sourceLife) / 5f;
                float rotation = spirit.HeadRotation;
                Vector3 right = (camera.Right * MathF.Cos(rotation) + camera.Up * MathF.Sin(rotation)) * _headTexture.Width * scale * .5f;
                Vector3 up = (camera.Up * MathF.Cos(rotation) - camera.Right * MathF.Sin(rotation)) * _headTexture.Height * scale * .5f;
                Quad(ref count, spirit.Position - right + up, spirit.Position + right + up,
                    spirit.Position + right - up, spirit.Position - right - up, tint, Vector2.Zero, Vector2.One);
            }
            DrawGeometry(_headTexture, count);
        }

        private void DrawCircles()
        {
            if (_circleLife <= 0) return;
            int count = 0;
            float brightness = _circleLife < 5 ? _circleLife / 5f : MathF.Sin((60 - _circleLife) * .05f) + .5f;
            Color color = new Color(1f, .5f, .1f) * brightness;
            for (int circle = 0; circle < 2; circle++)
            {
                float rotation = MathHelper.ToRadians(circle == 0 ? 0 : -200);
                // RenderTerrainAlphaBitmap's full width is Scale*TERRAIN_SCALE.
                float size = _circleScale[circle] * Constants.TERRAIN_SCALE;
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                    {
                        Vector2 uv0 = new Vector2(x / 4f, y / 4f), uv1 = uv0 + new Vector2(.25f);
                        Quad(ref count, GroundPoint(uv0, size, rotation), GroundPoint(new Vector2(uv1.X, uv0.Y), size, rotation),
                            GroundPoint(uv1, size, rotation), GroundPoint(new Vector2(uv0.X, uv1.Y), size, rotation), color, uv0, uv1);
                    }
            }
            DrawGeometry(_groundTexture, count);
        }
        private Vector3 GroundPoint(Vector2 uv, float size, float rotation)
        {
            Vector2 offset = (uv - new Vector2(.5f)) * size;
            Vector3 point = Position + new Vector3(offset.X * MathF.Cos(rotation) - offset.Y * MathF.Sin(rotation),
                offset.X * MathF.Sin(rotation) + offset.Y * MathF.Cos(rotation), 0);
            point.Z = (World?.Terrain?.RequestTerrainHeight(point.X, point.Y) ?? Position.Z) + 5f;
            return point;
        }
        private void DrawGeometry(Texture2D? texture, int count)
        {
            if (texture != null) ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count);
        }
        private void Ribbon(ref int count, Vector3 a, Vector3 b, Vector3 width, Color tint, float u, float step)
        {
            var v0 = new VertexPositionColorTexture(a - width, tint, new Vector2(u, 0));
            var v1 = new VertexPositionColorTexture(a + width, tint, new Vector2(u, 1));
            var v2 = new VertexPositionColorTexture(b + width, tint, new Vector2(u - step, 1));
            var v3 = new VertexPositionColorTexture(b - width, tint, new Vector2(u - step, 0));
            _vertices[count++] = v0; _vertices[count++] = v1; _vertices[count++] = v2;
            _vertices[count++] = v0; _vertices[count++] = v2; _vertices[count++] = v3;
        }
        private void Quad(ref int count, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector2 uv0, Vector2 uv1)
        {
            var v0 = new VertexPositionColorTexture(a, color, uv0);
            var v1 = new VertexPositionColorTexture(b, color, new Vector2(uv1.X, uv0.Y));
            var v2 = new VertexPositionColorTexture(c, color, uv1);
            var v3 = new VertexPositionColorTexture(d, color, new Vector2(uv0.X, uv1.Y));
            _vertices[count++] = v0; _vertices[count++] = v1; _vertices[count++] = v2;
            _vertices[count++] = v0; _vertices[count++] = v2; _vertices[count++] = v3;
        }
        private void Remove() { World?.RemoveObject(this); Dispose(); }
    }
}
