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

/// <summary>MuMain Force: two subtype-1 wave rings and a decelerating piercing model.</summary>
public sealed class ForceSkillEffect : StagedCombatEffect
{
    public ForceSkillEffect(WalkerObject caster) : base(caster, 26f) { }
    protected override void BuildParts()
    {
        float factor = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        Vector3 start = Origin + Vector3.UnitZ * (130f * factor);
        for (int i = 0; i < 2; i++)
        {
            var ring = new ForceModel(false, start + new Vector3(MuGame.Random.Next(-50, 50), MuGame.Random.Next(-50, 50), 0f) * factor,
                Caster.Angle, .1f + MuGame.Random.Next(50) / 100f, 15f);
            AddPart(ring, 0f);
            for (int j = 0; j < 2; j++) AddPart(new PiercingRibbon(start, Caster.Angle.Z), 0f);
        }
        AddPart(new ForceModel(true, start, Caster.Angle, 2f, 10f), 0f);
        // MODEL_PIERCING2 emits subtype-2 rings while its remaining life is >5.
        // Preload these bounded pieces, then start them at the source ticks.
        Vector3 position = start;
        for (int tick = 0; tick < 5; tick++)
        {
            float life = 10f - tick;
            AddPart(new ForceModel(false, position, Caster.Angle, life * .05f, 15f, true), tick);
            float speed = MathF.Min(0f, -60f + (tick + 1) * 12f);
            position += SourceJointMath.MuMainRotate(new Vector3(0f, speed, 0f), new Vector3(0f, 0f, MathHelper.ToDegrees(Caster.Angle.Z)));
        }
    }
    private sealed class ForceModel : ModelObject
    {
        private readonly bool _piercing, _emitted;
        private float _life, _gravity = .01f, _speed = -60f;
        public ForceModel(bool piercing, Vector3 position, Vector3 angle, float scale, float life, bool emitted = false)
        {
            _piercing = piercing; _emitted = emitted; _life = life;
            Position = position; Angle = new Vector3(piercing ? 0f : MathHelper.PiOver2, 0f, angle.Z);
            Scale = scale; BlendMesh = -2; BlendMeshLight = 1f;
            BlendState = Blendings.OneOneAdditive; BlendMeshState = Blendings.OneOneAdditive;
            DepthState = piercing || emitted ? DepthStencilState.DepthRead : DepthStencilState.None;
            RenderShadow = false; LightEnabled = true; Light = Vector3.One;
            IsTransparent = true; AffectedByTransparency = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-600f), new Vector3(600f));
        }
        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare(_piercing ? "Skill/m_Piercing.bmd" : "Skill/m_Waves.bmd");
            await base.Load();
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_piercing)
            {
                _speed = MathF.Min(0f, _speed + 12f * f);
                Position += SourceJointMath.MuMainRotate(new Vector3(0f, _speed, 0f), new Vector3(0f, 0f, MathHelper.ToDegrees(Angle.Z))) * f;
                if (_life > 5f) BlendMeshLight *= MathF.Pow(1f / 1.6f, f);
            }
            else
            {
                Scale = MathF.Min(_emitted ? 1.5f : 2f, Scale + _gravity * f);
                _gravity += (_emitted ? .01f : .07f) * f;
                BlendMeshLight *= MathF.Pow(1f / 1.5f, f);
            }
            _life -= f;
            if (_life <= 0f) { World?.RemoveObject(this); Dispose(); }
        }
    }
    private sealed class PiercingRibbon : EffectObject
    {
        private readonly Vector3[] _positions = new Vector3[30];
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[29 * 12];
        private readonly Vector3 _angle;
        private readonly Vector3 _tint;
        private readonly float _scale;
        private float _life = 10f, _velocity = 20f;
        private int _count;
        private Texture2D? _texture;
        public PiercingRibbon(Vector3 start, float yaw)
        {
            Position = start; _angle = new Vector3(-90f, 0f, MathHelper.ToDegrees(yaw));
            _scale = MuGame.Random.Next(70, 110);
            _tint = FPSCounter.Instance.RandFPSCheck(2) ? new Vector3(1f, .8f, .6f) : Vector3.One;
            IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-2000f), new Vector3(2000f));
        }
        public override async Task LoadContent()
        {
            await base.LoadContent();
            _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Piercing.jpg");
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_life > 9f)
            {
                Vector3 position = Position;
                _count = 30;
                for (int i = 0; i < _count; i++)
                {
                    _positions[i] = position;
                    position += SourceJointMath.MuMainRotate(new Vector3(0f, 0f, -_velocity * f), _angle) * f;
                }
                Position = position;
            }
            _velocity -= 2f * f; _life -= f;
            if (_life <= 0f) { World?.RemoveObject(this); Dispose(); }
        }
        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || _texture == null) return;
            SourceJointMath.MuMainAngleBasis(_angle, out var right, out _, out var up);
            var tint = new Color(_tint * MathF.Pow(1f / 1.4f, 10f - _life));
            int count = 0;
            for (int i = 0; i < _count - 1; i++)
                for (int face = 0; face < 2; face++)
                {
                    Vector3 half = (face == 0 ? right : up) * (_scale * .5f);
                    float u = (_count - i) / 29f, v = (_count - i - 1) / 29f;
                    var a = new VertexPositionColorTexture(_positions[i] - half, tint, new Vector2(u, 0));
                    var b = new VertexPositionColorTexture(_positions[i] + half, tint, new Vector2(u, 1));
                    var c = new VertexPositionColorTexture(_positions[i + 1] + half, tint, new Vector2(v, 1));
                    var d = new VertexPositionColorTexture(_positions[i + 1] - half, tint, new Vector2(v, 0));
                    _vertices[count++] = a; _vertices[count++] = b; _vertices[count++] = c;
                    _vertices[count++] = a; _vertices[count++] = c; _vertices[count++] = d;
                }
            ElfSupportBuffEffect.DrawWorldGeometry(_texture, _vertices, count);
        }
    }
}
