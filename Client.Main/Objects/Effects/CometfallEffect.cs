#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain MODEL_SKILL_BLAST: two falling cores and their owned impact parts.</summary>
public sealed class CometfallEffect : EffectObject
{
    private const int CoreCount = 2;
    private readonly BlastModel[] _cores = new BlastModel[CoreCount];
    private readonly SourceJointEffect[] _trails = new SourceJointEffect[CoreCount];
    private readonly ImpactSprite[,] _flashes = new ImpactSprite[CoreCount, 2];
    private readonly StoneModel[,] _stones = new StoneModel[CoreCount, 6];
    private readonly DynamicLight[] _lights = new DynamicLight[CoreCount];
    private readonly bool[] _landed = new bool[CoreCount];
    private readonly float[] _velocity = new float[CoreCount];
    private readonly Vector3 _target;
    private float _age;
    private bool _started;
    private bool _lightsAdded;

    public CometfallEffect(Vector3 target)
    {
        _target = target;
        Position = target;
        LightEnabled = false;
        IsTransparent = true;
        AffectedByTransparency = false;
        BlendState = BlendState.Additive;
        DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new BoundingBox(new Vector3(-500f, -500f, -100f), new Vector3(700f, 500f, 1000f));
    }

    public override async Task LoadContent()
    {
        await base.LoadContent();
        for (int i = 0; i < CoreCount; i++)
        {
            var core = _cores[i] = new BlastModel("Skill/Blast01.bmd")
            {
                Position = _target + new Vector3(MuGame.Random.Next(200, 300), MuGame.Random.Next(-50, 50), MuGame.Random.Next(300, 800)),
                Angle = new Vector3(0f, MathHelper.ToRadians(20f), 0f),
                Scale = MuGame.Random.Next(10, 18) * .1f
            };
            _velocity[i] = -MuGame.Random.Next(50, 100);
            _trails[i] = SourceJointEffect.BlastTrail(() => core.Position);
            _lights[i] = new DynamicLight { Owner = this, Position = core.Position, Color = new Vector3(.2f, .4f, 1f), Radius = 200f, Intensity = 0f };
            await PreparePart(core);
            await PreparePart(_trails[i]);
            for (int j = 0; j < 2; j++)
            {
                _flashes[i, j] = new ImpactSprite(j == 0);
                await PreparePart(_flashes[i, j]);
            }
            for (int j = 0; j < 6; j++)
            {
                _stones[i, j] = new StoneModel();
                await PreparePart(_stones[i, j]);
            }
            if (IsDisposeRequested) return;
        }
        if (World?.Terrain != null)
        {
            for (int i = 0; i < CoreCount; i++) World.Terrain.AddDynamicLight(_lights[i]);
            _lightsAdded = true;
        }
    }

    private async Task PreparePart(WorldObject part)
    {
        if (IsDisposeRequested) { part.Dispose(); return; }
        part.World = World;
        await part.Load();
        if (IsDisposeRequested) part.Dispose();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready || World == null) return;
        if (!_started)
        {
            _started = true;
            for (int i = 0; i < CoreCount; i++)
            {
                World.Objects.Add(_cores[i]);
                World.Objects.Add(_trails[i]);
            }
        }
        float factor = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _age += factor;
        for (int i = 0; i < CoreCount; i++)
        {
            if (_landed[i]) continue;
            var core = _cores[i];
            core.Position += SourceJointMath.MuMainRotate(new Vector3(0f, 0f, _velocity[i]), new Vector3(0f, 20f, 0f)) * factor;
            float ground = World.Terrain?.RequestTerrainHeight(core.Position.X, core.Position.Y) ?? _target.Z;
            _lights[i].Position = core.Position;
            _lights[i].Intensity = MuGame.Random.Next(7, 11) * .1f;
            if (core.Position.Z >= ground && _age < 30f) continue;
            _landed[i] = true;
            _lights[i].Intensity = 0f;
            RemovePart(core);
            RemovePart(_trails[i]);
            if (_age >= 30f) continue;
            Vector3 impact = new(core.Position.X, core.Position.Y, ground);
            for (int j = 0; j < 2; j++)
            {
                _flashes[i, j].Position = impact + Vector3.UnitZ * 80f;
                World.Objects.Add(_flashes[i, j]);
            }
            for (int j = 0; j < 6; j++)
            {
                // MuMain applies FPS probability at the impact emission site.
                if (!FPSCounter.Instance.RandFPSCheck(1)) continue;
                _stones[i, j].Position = impact;
                World.Objects.Add(_stones[i, j]);
            }
        }
        if (_age >= 80f) { World.RemoveObject(this); Dispose(); }
    }

    private static void RemovePart(WorldObject? part)
    {
        part?.World?.RemoveObject(part);
        part?.Dispose();
    }

    public override void Dispose()
    {
        for (int i = 0; i < CoreCount; i++)
        {
            RemovePart(_cores[i]); RemovePart(_trails[i]);
            for (int j = 0; j < 2; j++) RemovePart(_flashes[i, j]);
            for (int j = 0; j < 6; j++) RemovePart(_stones[i, j]);
            if (_lightsAdded) World?.Terrain?.RemoveDynamicLight(_lights[i]);
        }
        _lightsAdded = false;
        base.Dispose();
    }

    private class BlastModel : ModelObject
    {
        private readonly string _path;
        public BlastModel(string path)
        {
            _path = path;
            RenderShadow = false; LightEnabled = true; Light = Vector3.One;
            IsTransparent = true; AffectedByTransparency = false;
            BlendState = BlendState.Additive; BlendMeshState = BlendState.Additive;
            BlendMesh = 0; BlendMeshLight = 1f; DepthState = DepthStencilState.DepthRead;
        }
        public override async Task Load() { Model = await BMDLoader.Instance.Prepare(_path); await base.Load(); }
    }

    private sealed class StoneModel : BlastModel
    {
        private Vector3 _direction;
        private float _gravity = MuGame.Random.Next(8, 24);
        private float _life = MuGame.Random.Next(32, 48);
        public StoneModel() : base($"Skill/Stone0{MuGame.Random.Next(1, 3)}.bmd")
        {
            Scale = MuGame.Random.Next(8, 12) * .1f;
            Angle = new Vector3(0f, MathHelper.ToRadians(20f), MathHelper.ToRadians(MuGame.Random.Next(360)));
            _direction = SourceJointMath.MuMainRotate(new Vector3(0f, MuGame.Random.Next(64, 320) * .1f, 0f), new Vector3(0f, 20f, MathHelper.ToDegrees(Angle.Z)));
            IsTransparent = false; BlendMesh = -1; BlendState = BlendState.Opaque;
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            Position += (_direction + Vector3.UnitZ * _gravity) * f;
            _direction *= .9f; _gravity -= 3f * f; _life -= f;
            float ground = World?.Terrain?.RequestTerrainHeight(Position.X, Position.Y) ?? Position.Z;
            bool contact = Position.Z < ground;
            if (contact) { Position = new Vector3(Position.X, Position.Y, ground); _gravity *= -.5f; _life -= 4f * f; }
            Angle -= Vector3.UnitX * MathHelper.ToRadians(Scale * (contact ? 128f : 32f) * f);
            if (_life <= 0f) RemovePart(this);
        }
    }

    private sealed class ImpactSprite : SpriteObject
    {
        private readonly bool _ring;
        private float _life = 20f;
        public override string TexturePath => _ring ? "Effect/ring.jpg" : "Effect/Explotion01.jpg";
        public ImpactSprite(bool ring)
        {
            _ring = ring;
            Scale = 1f; LightEnabled = true; Light = Vector3.One;
            IsTransparent = true; AffectedByTransparency = false;
            BlendState = BlendState.Additive; DepthState = DepthStencilState.DepthRead;
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            _life -= FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_ring) { Scale = MathF.Sin(MathHelper.ToRadians(_life * 10f)) * 3f + 2f; Light = Vector3.One * (_life / 5f); }
            if (_life <= 0f) RemovePart(this);
        }
        public override void Draw(GameTime gameTime)
        {
            if (!Visible || SpriteTexture == null) return;
            var camera = Camera.Instance;
            Vector3 point = GraphicsDevice.Viewport.Project(Position, camera.Projection, camera.View, Matrix.Identity);
            if (point.Z <= 0f || point.Z >= 1f) return;
            int frame = Math.Clamp((int)((20f - _life) / 2f), 0, 9);
            int w = SpriteTexture.Width / (_ring ? 1 : 4), h = SpriteTexture.Height / (_ring ? 1 : 4);
            var source = _ring ? new Rectangle(0, 0, w, h) : new Rectangle(frame % 4 * w, frame / 4 * h, w, h);
            float scale = Scale * Constants.TERRAIN_SIZE / MathF.Max(1f, Vector3.Distance(camera.Position, Position)) * Constants.RENDER_SCALE * (_ring ? 1f : 4f);
            var color = new Color(Light);
            if (!SpriteBatchScope.BatchIsBegun)
            {
                using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred, BlendState, SamplerState.LinearClamp, DepthState);
                GraphicsManager.Instance.Sprite.Draw(SpriteTexture, new Vector2(point.X, point.Y), source, color, 0f, new Vector2(w, h) * .5f, scale, SpriteEffects.None, point.Z);
            }
            else GraphicsManager.Instance.Sprite.Draw(SpriteTexture, new Vector2(point.X, point.Y), source, color, 0f, new Vector2(w, h) * .5f, scale, SpriteEffects.None, point.Z);
        }
    }
}
