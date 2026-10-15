#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>Fixed pool for MuMain's short-lived wolf subtypes; the caster is never mutated.</summary>
public sealed class RageFighterWolfEchoEffect : EffectObject
{
    private const int Capacity = 24;
    private readonly Echo[] _echoes = new Echo[Capacity];
    private readonly WalkerObject _caster;
    private readonly ushort _skill;
    private int _next;
    private float _age;
    public RageFighterWolfEchoEffect(WalkerObject caster, ushort skill)
    {
        _caster = caster; _skill = skill; Position = caster.WorldPosition.Translation;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new(new(-1500), new(1500));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        var model = await BMDLoader.Instance.Prepare(_skill == 260 ? "Effect/wolf_head_effect.bmd" : "Effect/wolf_head_effect2.bmd");
        for (int i = 0; i < Capacity; i++)
        {
            if(IsDisposeRequested || World==null)return;
            var echo = new Echo { Model = model, World = World };
            _echoes[i] = echo;
            await echo.Load();
            if(IsDisposeRequested || World==null){echo.Dispose();return;}
            World.Objects.Add(echo);
        }
    }
    public override void Update(GameTime time)
    {
        base.Update(time);
        if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _age += f;
        float frame = (float)_caster.GetAnimationTime();
        bool active = _skill == 260
            ? _caster.CurrentAction == (int)PlayerAction.PlayerSkillThrust && frame < 10
            : _caster.CurrentAction == (int)PlayerAction.PlayerSkillGiantswing && frame > 6 && frame < 10;
        if (active && FPSCounter.Instance.RandFPSCheck(1))
        {
            int bone = _skill == 260 ? 27 : 4;
            Vector3 position = RageFighterBoneSampler.Sample(_caster.Model, _caster.CurrentAction, frame, bone, _caster.WorldPosition);
            int random = MuGame.Random.Next(6);
            float amount = (_skill == 260 ? 7 : 2) + MuGame.Random.Next(3);
            var angle = _caster.Angle;
            angle.Z += MathHelper.ToRadians(amount * (random % 3 * 2 + 1) * (random / 3 == 0 ? -1 : 1));
            int pitch = MuGame.Random.Next(3);
            if (pitch != 0) angle.X += MathHelper.ToRadians(amount * (random % 3 * 2 + 1) * (pitch == 1 ? 1 : -1));
            Spawn(position, angle, 1.2f + MuGame.Random.Next(5) * .1f, 2 + MuGame.Random.Next(2), 50);
            Spawn(position, angle, (_skill == 260 ? .8f : .7f) + MuGame.Random.Next(3) * .1f,
                _skill == 260 ? 3 + MuGame.Random.Next(3) : 5 + MuGame.Random.Next(2), 40);
        }
        if (_age >= 100 || _caster.Status == GameControlStatus.Disposed) { World?.RemoveObject(this); Dispose(); }
    }
    private void Spawn(Vector3 position, Vector3 angle, float scale, float life, float speed)
    {
        _echoes[_next].Activate(position, angle, scale, life, speed, _skill == 260 ? Vector3.One : new(.12f, .12f, .18f));
        _next = (_next + 1) % Capacity;
    }
    public override void Dispose()
    {
        foreach (var echo in _echoes) { echo?.World?.RemoveObject(echo); echo?.Dispose(); }
        base.Dispose();
    }
    private sealed class Echo : ModelObject
    {
        private float _life, _speed;
        internal Echo()
        {
            Alpha = 0; LightEnabled = false; RenderShadow = false;
            BlendMesh = -2; BlendMeshLight = 1; BlendMeshState = Blendings.OneOneAdditive;
            BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
            IsTransparent = true; AffectedByTransparency = false; ContinuousAnimation = true;
            AnimationSpeed = 2.5f;
        }
        protected override bool AllowDynamicLightingShader => false;
        protected override bool ShouldRenderMesh(int mesh) => _life > 0;
        internal void Activate(Vector3 position, Vector3 angle, float scale, float life, float speed, Vector3 light)
        { SetStaticAnimationPose(0); Position = position; Angle = angle; Scale = scale; _life = life; _speed = speed; Light = light; Alpha = 1; }
        public override void Update(GameTime time)
        {
            if (_life <= 0) return;
            base.Update(time);
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            Position += new Vector3(MathF.Sin(Angle.Z), -MathF.Cos(Angle.Z), 0) * _speed * f;
            _life -= f; Alpha = _life > 0 ? 1 : 0;
        }
        public override void DrawMesh(int mesh)
        {
            var shader = GraphicsManager.Instance.AlphaTestEffect3D;
            bool colors = shader.VertexColorEnabled;
            try { shader.VertexColorEnabled = true; base.DrawMesh(mesh); }
            finally { shader.VertexColorEnabled = colors; }
        }
    }
}
