#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>Sleep, Weakness and Innovation casting families from MuMain.</summary>
public sealed class SummonerDebuffCastEffect : StagedCombatEffect
{
    private readonly ushort _skill;
    private readonly WalkerObject? _target;
    public SummonerDebuffCastEffect(WalkerObject caster, WalkerObject? target, ushort skill) : base(caster, 76f)
    { _skill = skill; _target = target; }
    protected override void BuildParts()
    {
        AddPart(new CastSprites(Caster, _skill, _target), 0f);
        if (_skill == 219)
        {
            if (_target != null)
                for (int i = 0; i < 2; i++) AddPart(new CastModel(_target, i, true, new Vector3(.8f, .3f, .9f)), 0f);
        }
        else
        {
            Vector3 tint = _skill == 221 ? new Vector3(2f, .4f, .3f) : new Vector3(.25f, 1f, .7f);
            for (int i = 0; i < 3; i++) AddPart(new CastModel(Caster, i, false, tint), 0f);
        }
    }
    private sealed class CastModel : ModelObject
    {
        private readonly WalkerObject _owner;
        private readonly int _part;
        private readonly bool _sleep;
        private float _life, _sourceTick;
        public CastModel(WalkerObject owner, int part, bool sleep, Vector3 tint)
        {
            _owner = owner; _part = part; _sleep = sleep;
            _life = sleep ? part == 0 ? 34f : 35f : 40f;
            Scale = sleep ? part == 0 ? .1f : .15f : .6f;
            Position = owner.WorldPosition.Translation + Vector3.UnitZ * (sleep ? 100f : 0f);
            Angle = Vector3.Zero;
            Light = tint; LightEnabled = false; Alpha = sleep ? 0f : 1f;
            BlendMesh = sleep ? -2 : 0; BlendMeshLight = 0f;
            BlendMeshState = Blendings.OneOneAdditive; BlendState = sleep ? Blendings.OneOneAdditive : BlendState.Opaque;
            DepthState = DepthStencilState.DepthRead; RenderShadow = false;
            IsTransparent = true; AffectedByTransparency = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-600f), new Vector3(600f));
        }
        protected override bool AllowDynamicLightingShader => false;
        public override async Task Load()
        {
            string path = _sleep ? _part == 0 ? "Effect/elshildring.bmd" : "Effect/elshildring2.bmd"
                : _part == 0 ? "Effect/Suhwanzin2.bmd" : _part == 1 ? "Effect/Suhwanzin22.bmd" : "Effect/Suhwanzin222.bmd";
            Model = await BMDLoader.Instance.Prepare(path);
            await base.Load();
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            if (_owner.Status == GameControlStatus.Disposed) { Remove(); return; }
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_sleep)
            {
                Position = _owner.WorldPosition.Translation + Vector3.UnitZ * 100f;
                float delta = (_life > 20f ? .05f : -.05f) * f;
                Alpha = MathF.Max(0f, Alpha + delta); BlendMeshLight = MathF.Max(0f, BlendMeshLight + delta);
                Scale += .035f * f;
                Angle += Vector3.UnitZ * MathHelper.ToRadians((_part == 0 ? 8f : -8f) * f);
            }
            else
            {
                _sourceTick += f;
                while (_sourceTick >= 1f)
                {
                    _sourceTick -= 1f;
                    if (_life < 20f) BlendMeshLight -= .03f;
                    else if (BlendMeshLight < .5f) BlendMeshLight += .05f;
                }
                Angle += Vector3.UnitZ * MathHelper.ToRadians((_part == 1 ? -3f : 3f) * f);
            }
            _life -= f;
            if (_life <= 0f) Remove();
        }
        private void Remove() { World?.RemoveObject(this); Dispose(); }
    }
    private sealed class CastSprites : EffectObject
    {
        private struct Particle { public Vector3 Position, Light; public float Life, Scale, Rotation; public byte Kind; }
        private const int Capacity = 160;
        private readonly Particle[] _particles = new Particle[Capacity];
        private readonly WalkerObject _owner;
        private readonly WalkerObject? _sleepTarget;
        private readonly ushort _skill;
        private readonly Vector3 _origin;
        private readonly Vector3 _tint;
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[6];
        private Texture2D? _ground, _shiny, _pin, _light, _cloud, _cra;
        private readonly float[] _groundAlpha = { 1f, 0f, 1f, 1f, 1f };
        private readonly float[] _groundScale = { 2f, 7f, 1f, .2f, .1f };
        private float _age;
        private int _next;
        public CastSprites(WalkerObject owner, ushort skill, WalkerObject? sleepTarget)
        {
            _owner = owner; _skill = skill; _sleepTarget = sleepTarget; _origin = owner.WorldPosition.Translation; Position = _origin;
            _tint = skill == 219 ? new Vector3(.7f, .3f, .8f) : skill == 221 ? new Vector3(2f, .4f, .3f) : new Vector3(.25f, 1f, .7f);
            IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
            DepthState = DepthStencilState.DepthRead;
            BoundingBoxLocal = new BoundingBox(new Vector3(-2000f), new Vector3(2000f));
        }
        public override async Task LoadContent()
        {
            await base.LoadContent();
            _ground = await TextureLoader.Instance.PrepareAndGetTexture(_skill == 219 ? "Effect/Magic_Ground2.jpg" : "Effect/mzine_typer2.jpg");
            _shiny = await TextureLoader.Instance.PrepareAndGetTexture("Effect/shiny05.jpg");
            _pin = await TextureLoader.Instance.PrepareAndGetTexture("Effect/pin_lights.jpg");
            _light = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
            _cloud = await TextureLoader.Instance.PrepareAndGetTexture("Effect/clud64.jpg");
            _cra = await TextureLoader.Instance.PrepareAndGetTexture("Effect/cra_04.jpg");
        }
        private Vector3 Hand() => _owner is PlayerObject player && player.TryGetBoneWorldMatrix(37, out var bone)
            ? bone.Translation : _owner.WorldPosition.Translation + Vector3.UnitZ * 100f;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _age += f;
            if (_skill != 219)
                for (int i = 0; i < 5; i++)
                {
                    float life = (i == 0 ? 50f : i == 1 ? 40f : 30f) - _age;
                    _groundAlpha[i] = MathF.Max(0f, _groundAlpha[i] + (life < 20f ? i == 1 ? -.03f : -.05f : i == 1 ? .06f : .05f) * f);
                    _groundAlpha[i] = MathF.Min(i == 1 ? .7f : 1f, _groundAlpha[i]);
                    if (i >= 2 && _groundScale[i] < 3.5f) _groundScale[i] += .1f * f;
                }
            if (_age < 24f && FPSCounter.Instance.RandFPSCheck(1))
            {
                Vector3 tint = _skill == 219 ? new Vector3(.6f, .1f, .8f) : _skill == 221 ? new Vector3(.8f, .1f, .1f) : _tint;
                _particles[_next] = new Particle { Position = Hand(), Light = tint, Life = 16f, Scale = 1f + MuGame.Random.Next(48, 80) * .01f * f, Rotation = MathHelper.ToRadians(MuGame.Random.Next(360)), Kind = 0 };
                _next = (_next + 1) % Capacity;
                _particles[_next] = new Particle { Position = Hand(), Light = tint, Life = 50f, Scale = .5f, Rotation = MathHelper.ToRadians(MuGame.Random.Next(360)), Kind = 2 };
                _next = (_next + 1) % Capacity;
                if (_skill != 219)
                {
                    _particles[_next] = new Particle { Position = _origin + new Vector3(MuGame.Random.Next(-250, 250), MuGame.Random.Next(-250, 250), MuGame.Random.Next(50, 150)), Light = _tint, Life = 30f, Scale = .5f, Rotation = MathHelper.ToRadians(MuGame.Random.Next(360)), Kind = 1 };
                    _next = (_next + 1) % Capacity;
                }
            }
            for (int i = 0; i < Capacity; i++)
                if (_particles[i].Life > 0f)
                {
                    if (_particles[i].Kind == 1)
                    {
                        _particles[i].Rotation += MathHelper.ToRadians(5f * f);
                        _particles[i].Scale -= .02f * f;
                        _particles[i].Position.Z -= 1.3f * f;
                    }
                    _particles[i].Life -= f;
                }
            if (_age >= 70f || _owner.Status == GameControlStatus.Disposed) { World?.RemoveObject(this); Dispose(); }
        }
        private void Quad(Texture2D texture, Vector3 center, Vector3 right, Vector3 up, Vector3 tint)
        {
            int count = 0;
            SourceEffectGeometry.Quad(_vertices, ref count, center - up, center + up, right, new Color(tint), 0f, 1f);
            ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count, samplerOverride: SamplerState.LinearClamp);
        }
        private void Sprite(Texture2D texture, Vector3 center, float scale, float rotation, Vector3 tint)
        {
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            Vector3 right = (camera.Right * MathF.Cos(rotation) + camera.Up * MathF.Sin(rotation)) * (texture.Width * scale * .5f);
            Vector3 up = (camera.Up * MathF.Cos(rotation) - camera.Right * MathF.Sin(rotation)) * (texture.Height * scale * .5f);
            Quad(texture, center, right, up, tint);
        }
        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            if (_skill == 219 && _sleepTarget != null && _light != null && _age < 35f)
            {
                float alpha = MathF.Max(0f, _age <= 14f ? _age * .05f : .7f - (_age - 14f) * .05f);
                Vector3 center = _sleepTarget.WorldPosition.Translation + Vector3.UnitZ * 100f;
                Vector3 light = new Vector3(.8f, .1f, .9f) * alpha;
                Sprite(_light, center, 5f, 0f, light);
                Sprite(_light, center, 5f, 0f, light);
            }
            if (_ground != null)
            {
                if (_skill == 219 && _age < 20f)
                {
                    float halfSize = _age * .15f * Constants.TERRAIN_SCALE * .5f;
                    float fade = _age > 10f ? MathF.Pow(.8f, _age - 10f) : 1f;
                    Quad(_ground, _owner.WorldPosition.Translation + Vector3.UnitZ * 5f, Vector3.UnitX * halfSize, Vector3.UnitY * halfSize, _tint * fade);
                }
                else if (_skill != 219)
                    for (int i = 0; i < 5; i++)
                    {
                        float life = (i == 0 ? 50f : i == 1 ? 40f : 30f) - _age;
                        if (life <= 0f) continue;
                        Vector3 tint = i == 1 && _skill == 221 ? new Vector3(2f, .1f, .1f) : _tint;
                        float brightness = _groundAlpha[i] * (i == 0 ? 2f : i == 1 ? .4f : 1f);
                        float halfSize = _groundScale[i] * Constants.TERRAIN_SCALE * .5f;
                        Quad(_ground, _origin + Vector3.UnitZ * 5f, Vector3.UnitX * halfSize, Vector3.UnitY * halfSize, tint * brightness);
                    }
            }
            if (_age < 24f)
            {
                Vector3 hand = Hand();
                float rotation = (float)gameTime.TotalGameTime.TotalMilliseconds * .0006f * MathHelper.TwoPi;
                Vector3 tint = _skill == 219 ? new Vector3(.5f, .2f, .8f) : _skill == 221 ? new Vector3(.8f, .1f, .1f) : _tint;
                if (_shiny != null) { Sprite(_shiny, hand, 1f, rotation, tint); Sprite(_shiny, hand, .7f, -rotation, tint); }
                if (_pin != null) { Sprite(_pin, hand, 1.7f, rotation, tint); Sprite(_pin, hand, 1.5f, -rotation, tint); }
            }
            if (_skill != 219 && _age < 40f && _pin != null)
                Sprite(_pin, _origin, 2f, _age * .1f, _tint * MathF.Min(1f, (40f - _age) / 10f));
            for (int i = 0; i < Capacity; i++)
            {
                Texture2D? texture = _particles[i].Kind == 0 ? _cra : _particles[i].Kind == 1 ? _shiny : _cloud;
                if (_particles[i].Life > 0f && _particles[i].Scale > 0f && texture != null)
                    Sprite(texture, _particles[i].Position, _particles[i].Scale, _particles[i].Rotation, _particles[i].Light * MathF.Min(1f, _particles[i].Life / 8f));
            }
        }
    }
}
