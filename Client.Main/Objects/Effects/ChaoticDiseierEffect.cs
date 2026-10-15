#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain's skill-238 packet effect: eight 2LINE_GHOST joints and five bone flashes.</summary>
public sealed class ChaoticDiseierEffect : EffectObject
{
    private const int GhostCount = 8, MaxHistory = 200, SmokeCapacity = 512;
    internal static readonly BlendState DarkBlend = new()
    {
        ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One
    };
    private struct Ghost
    {
        public Vector3 Position;
        public float Life, Yaw, Speed, Width;
        public int Head, Count;
    }
    private struct Smoke
    {
        public Vector3 Position;
        public float Life, Scale, Rotation;
    }
    private struct BombSpark
    {
        public Vector3 Position, Velocity;
        public float Life, Gravity, Scale;
    }
    private readonly WalkerObject _caster;
    private readonly Ghost[] _ghosts = new Ghost[GhostCount];
    private readonly Vector3[,] _history = new Vector3[GhostCount, MaxHistory];
    private readonly Smoke[] _smoke = new Smoke[SmokeCapacity];
    private readonly BirdModel?[] _birds = new BirdModel?[GhostCount];
    private readonly FeatherModel?[,] _feathers = new FeatherModel?[GhostCount, 10];
    private readonly bool[,] _featherStarted = new bool[GhostCount, 10];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(MaxHistory - 1) * 12];
    private readonly Vector3[] _flashes = new Vector3[5];
    private readonly BombSpark[] _bombSparks = new BombSpark[20];
    private readonly Vector3? _bombPosition;
    private Texture2D? _sparkTexture, _explosionTexture;
    private Texture2D? _trailTexture, _smokeTexture, _flashTexture;
    private int _tailLimit, _nextSmoke;
    private float _age;
    private bool _started;
    public ChaoticDiseierEffect(WalkerObject caster, float? yaw, Vector3? bombPosition = null)
    {
        _caster = caster; Position = caster.WorldPosition.Translation;
        _bombPosition = bombPosition.HasValue ? bombPosition.Value + Vector3.UnitZ * 80f : null;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new BoundingBox(new Vector3(-6000f), new Vector3(6000f));
        for (int i = 0; i < GhostCount; i++)
            _ghosts[i] = new Ghost
            {
                Position = Position + new Vector3(MuGame.Random.Next(-119, 121), 0f, MuGame.Random.Next(49, 109)),
                Life = FPSCounter.Instance.RandFPSCheck(2) ? 67f : 75f,
                Yaw = MathHelper.ToDegrees(yaw ?? caster.Angle.Z), Speed = MuGame.Random.Next(40, 60), Width = MuGame.Random.Next(21, 221)
            };
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        _trailTexture = await TextureLoader.Instance.PrepareAndGetTexture("Skill/2line_gost.jpg");
        _smokeTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/smoke01.jpg");
        _flashTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/shiny05.jpg");
        if (_bombPosition.HasValue)
        {
            _sparkTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Spark02.jpg");
            _explosionTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Explotion01.jpg");
            for (int i = 0; i < _bombSparks.Length; i++)
                _bombSparks[i] = new BombSpark
                {
                    Position = _bombPosition.Value, Life = MuGame.Random.Next(24, 40), Gravity = MuGame.Random.Next(6, 22), Scale = MuGame.Random.Next(4, 8) * .2f,
                    Velocity = SourceJointMath.MuMainRotate(new Vector3(0f, MuGame.Random.Next(20, 40) * .3f, 0f), new Vector3(MuGame.Random.Next(150, 210), 0f, MuGame.Random.Next(360)))
                };
        }
        _tailLimit = Math.Clamp((int)(26f / MathF.Max(.01f, FPSCounter.Instance.FPS_ANIMATION_FACTOR)), 2, MaxHistory);
        for (int i = 0; i < GhostCount; i++)
        {
            if (MuGame.Random.Next(3) >= 2) continue;
            var bird = new BirdModel(this, i) { World = World };
            _birds[i] = bird; await bird.Load();
            for (int j = 0; j < 10; j++)
            {
                var feather = new FeatherModel(j % 2 == 0) { World = World };
                _feathers[i, j] = feather; await feather.Load();
            }
        }
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _age += f;
        if (!_started)
        {
            _started = true;
            for (int i = 0; i < GhostCount; i++) if (_birds[i] != null) World?.Objects.Add(_birds[i]!);
        }
        for (int i = 0; i < GhostCount; i++)
        {
            ref var ghost = ref _ghosts[i];
            if (ghost.Life > 0f)
            {
                // MoveJoint applies forward motion before the subtype's alternating yaw.
                ghost.Position += SourceJointMath.MuMainRotate(new Vector3(0f, -ghost.Speed * f, 0f), new Vector3(0f, 0f, ghost.Yaw));
                ghost.Yaw += ((int)ghost.Life % 16 <= 7 ? 10f : -10f) * f;
                if (FPSCounter.Instance.RandFPSCheck(1))
                {
                    _smoke[_nextSmoke] = new Smoke { Position = ghost.Position, Life = 40f, Scale = MuGame.Random.Next(64, 128) * .01f, Rotation = MathHelper.ToRadians(MuGame.Random.Next(360)) };
                    _nextSmoke = (_nextSmoke + 1) % SmokeCapacity;
                }
                _history[i, ghost.Head] = ghost.Position;
                ghost.Head = (ghost.Head + 1) % _tailLimit;
                ghost.Count = Math.Min(_tailLimit, ghost.Count + 1);
                ghost.Life -= f;
            }
            if (_birds[i] != null)
                for (int j = 0; j < 10; j++)
                    if (!_featherStarted[i, j] && _age >= 2f + (j / 2) * 10f)
                    {
                        var feather = _feathers[i, j]!;
                        feather.Start(ghost.Position, f);
                        _featherStarted[i, j] = true;
                        World?.Objects.Add(feather);
                    }
        }
        for (int i = 0; i < SmokeCapacity; i++)
            if (_smoke[i].Life > 0f) { _smoke[i].Scale += .09f * f; _smoke[i].Life -= f; }
        for (int i = 0; i < _bombSparks.Length; i++)
        {
            ref var spark = ref _bombSparks[i];
            if (spark.Life <= 0f) continue;
            spark.Position += spark.Velocity * f + Vector3.UnitZ * (spark.Gravity * f);
            spark.Gravity -= 2f * f;
            float ground = World?.Terrain?.RequestTerrainHeight(spark.Position.X, spark.Position.Y) ?? 0f;
            if (spark.Position.Z < ground) { spark.Position.Z = ground; spark.Gravity = -spark.Gravity * .6f; }
            spark.Life -= f;
        }
        if (_age < 24f)
        {
            var bones = _caster.GetBoneTransforms();
            for (int i = 0; i < _flashes.Length; i++)
            {
                Vector3 bone = bones is { Length: > 0 } ? Vector3.Transform(Vector3.Zero, bones[MuGame.Random.Next(bones.Length)]) * _caster.Scale : Vector3.UnitZ * 100f;
                _flashes[i] = _caster.WorldPosition.Translation + bone + SourceJointMath.MuMainRotate(new Vector3(0f, -20f, 0f), new Vector3(0f, 0f, MathHelper.ToDegrees(_caster.Angle.Z)));
            }
        }
        if (_age >= 145f || _caster.Status == GameControlStatus.Disposed) { World?.RemoveObject(this); Dispose(); }
    }
    private void Billboard(Texture2D texture, Vector3 position, float scale, float rotation, Color tint, BlendState blend)
    {
        Matrix camera = Matrix.Invert(Camera.Instance.View);
        Vector3 right = (camera.Right * MathF.Cos(rotation) + camera.Up * MathF.Sin(rotation)) * (texture.Width * scale * .5f);
        Vector3 up = (camera.Up * MathF.Cos(rotation) - camera.Right * MathF.Sin(rotation)) * (texture.Height * scale * .5f);
        int count = 0;
        SourceEffectGeometry.Quad(_vertices, ref count, position - up, position + up, right, tint, 0f, 1f);
        ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count, blend, SamplerState.LinearClamp);
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        if (_trailTexture != null)
            for (int i = 0; i < GhostCount; i++)
            {
                ref var ghost = ref _ghosts[i];
                if (ghost.Life <= 0f) continue;
                int count = 0;
                Vector3 side = SourceJointMath.MuMainRotate(Vector3.UnitX * (ghost.Width * .5f), new Vector3(0f, 0f, ghost.Yaw));
                for (int j = 0; j < ghost.Count - 1; j++)
                {
                    Vector3 a = _history[i, (ghost.Head - 1 - j + _tailLimit) % _tailLimit], b = _history[i, (ghost.Head - 2 - j + _tailLimit) % _tailLimit];
                    if (Vector3.DistanceSquared(a, b) > 3600f) continue;
                    float u = (ghost.Count - j) / (float)(_tailLimit - 1), v = (ghost.Count - j - 1) / (float)(_tailLimit - 1);
                    SourceEffectGeometry.Quad(_vertices, ref count, a, b, side, Color.White, u, v);
                    SourceEffectGeometry.Quad(_vertices, ref count, a, b, Vector3.UnitZ * (ghost.Width * .5f), Color.White, u, v);
                }
                ElfSupportBuffEffect.DrawWorldGeometry(_trailTexture, _vertices, count, DarkBlend, SamplerState.LinearClamp);
            }
        if (_smokeTexture != null)
            for (int i = 0; i < SmokeCapacity; i++)
                if (_smoke[i].Life > 0f) Billboard(_smokeTexture, _smoke[i].Position, _smoke[i].Scale, _smoke[i].Rotation, new Color(Vector3.One * (.9f * _smoke[i].Life / 40f)), DarkBlend);
        if (_flashTexture != null && _age < 24f)
            for (int i = 0; i < _flashes.Length; i++) Billboard(_flashTexture, _flashes[i], 2f, 0f, new Color(new Vector3(.5f)), DarkBlend);
        if (_sparkTexture != null)
            for (int i = 0; i < _bombSparks.Length; i++)
                if (_bombSparks[i].Life > 0f) Billboard(_sparkTexture, _bombSparks[i].Position, _bombSparks[i].Scale, 0f, new Color(Vector3.One * MathF.Min(1f, _bombSparks[i].Life / 16f)), Blendings.OneOneAdditive);
        if (_explosionTexture != null && _bombPosition.HasValue && _age < 20f)
        {
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            Vector3 up = camera.Up * (_explosionTexture.Height * .5f);
            int count = 0, frame = Math.Clamp((int)(_age / 2f), 0, 9);
            SourceEffectGeometry.Quad(_vertices, ref count, _bombPosition.Value - up, _bombPosition.Value + up,
                camera.Right * (_explosionTexture.Width * .5f), new Color(new Vector3(.7f)), 0f, 1f);
            for (int i = 0; i < count; i++)
                _vertices[i].TextureCoordinate = new Vector2(frame % 4 * .25f + .005f, frame / 4 * .25f + .005f) + _vertices[i].TextureCoordinate * .24f;
            ElfSupportBuffEffect.DrawWorldGeometry(_explosionTexture, _vertices, count, Blendings.OneOneAdditive, SamplerState.LinearClamp);
        }
    }
    public override void Dispose()
    {
        for (int i = 0; i < GhostCount; i++)
        {
            _birds[i]?.World?.RemoveObject(_birds[i]!); _birds[i]?.Dispose();
            for (int j = 0; j < 10; j++) { _feathers[i, j]?.World?.RemoveObject(_feathers[i, j]!); _feathers[i, j]?.Dispose(); }
        }
        base.Dispose();
    }
    private sealed class BirdModel : ModelObject
    {
        private readonly ChaoticDiseierEffect _owner;
        private readonly int _index;
        private float _life = 52f;
        public BirdModel(ChaoticDiseierEffect owner, int index)
        {
            _owner = owner; _index = index; Position = owner._ghosts[index].Position; Scale = 1.4f;
            LightEnabled = false; Light = Vector3.One; RenderShadow = false;
            IsTransparent = true; AffectedByTransparency = false;
            BlendState = DarkBlend; BlendMeshState = DarkBlend; BlendMesh = -2;
            DepthState = DepthStencilState.DepthRead; ContinuousAnimation = true; AnimationSpeed = 6.25f;
        }
        protected override bool AllowDynamicLightingShader => false;
        public override async Task Load() { Model = await BMDLoader.Instance.Prepare("Skill/desair.bmd"); await base.Load(); }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            Position = _owner._ghosts[_index].Position; Angle = new Vector3(0f, 0f, MathHelper.ToRadians(_owner._ghosts[_index].Yaw));
            _life -= FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_life <= 0f) { World?.RemoveObject(this); Dispose(); }
        }
    }
    private sealed class FeatherModel : ModelObject
    {
        private readonly bool _shrink;
        private readonly Vector3 _spin;
        private float _life = 100f;
        public FeatherModel(bool shrink)
        {
            _shrink = !shrink; _spin = new Vector3(MuGame.Random.Next(-5, 5), MuGame.Random.Next(-5, 5), MuGame.Random.Next(-5, 5)) * MathHelper.Pi / 180f;
            Scale = 1.4f * (1f + MuGame.Random.Next(-10, 10) * .03f);
            Angle = new Vector3(MuGame.Random.Next(360), MuGame.Random.Next(360), MuGame.Random.Next(360)) * MathHelper.Pi / 180f;
            Alpha = .6f + MuGame.Random.Next(10) * .02f;
            LightEnabled = false; Light = new Vector3(.5f); RenderShadow = false;
            IsTransparent = true; AffectedByTransparency = false; BlendState = BlendState.AlphaBlend;
            DepthState = DepthStencilState.DepthRead;
        }
        public void Start(Vector3 position, float factor) => Position = position + new Vector3(MuGame.Random.Next(-10, 10), MuGame.Random.Next(-10, 10), MuGame.Random.Next(-10, 10)) * (4f * factor);
        protected override bool AllowDynamicLightingShader => false;
        public override async Task Load() { Model = await BMDLoader.Instance.Prepare("Skill/darkwing_hetachi.bmd"); await base.Load(); }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR, fade = MathF.Pow(.97f, f);
            Light *= fade; Alpha *= fade; if (_shrink) Scale *= fade;
            Angle += _spin; _life -= f;
            if (_life <= 0f) { World?.RemoveObject(this); Dispose(); }
        }
    }
}
