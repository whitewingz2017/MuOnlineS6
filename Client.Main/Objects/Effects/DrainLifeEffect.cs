#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain ALICE_DRAIN_LIFE's red motes, outward ghosts and returning energy joints.</summary>
public sealed class DrainLifeEffect : EffectObject
{
    private const int ParticleCapacity = 192, JointCapacity = 128, MaxHistory = 200;
    private struct Mote { public Vector3 Position, Velocity, Light; public float Life, Scale, Alpha; }
    private struct Joint
    {
        public Vector3 Position, Angle, Light, Target;
        public float Life, Speed, Width, ExtraLife;
        public int Head, Count, Limit;
        public bool Returning;
    }
    private readonly WalkerObject _caster, _victim;
    private readonly Mote[] _motes = new Mote[ParticleCapacity];
    private readonly Joint[] _joints = new Joint[JointCapacity];
    private readonly Vector3[,] _history = new Vector3[JointCapacity, MaxHistory];
    private readonly Vector3[,] _tailAngles = new Vector3[JointCapacity, MaxHistory];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(MaxHistory - 1) * 12];
    private Texture2D? _ghost, _energy, _light, _shiny;
    private readonly DrainReturnModel[] _returnModels = new DrainReturnModel[2];
    private float _life = 70f, _age;
    private bool _returned, _modelsStarted;
    private int _nextMote, _nextJoint;
    public DrainLifeEffect(WalkerObject caster, WalkerObject victim)
    {
        _caster = caster; _victim = victim; Position = caster.WorldPosition.Translation;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new BoundingBox(new Vector3(-5000f), new Vector3(5000f));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        _ghost = await TextureLoader.Instance.PrepareAndGetTexture("Effect/gostmark01.jpg");
        _energy = await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointLaser01.jpg");
        _light = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
        _shiny = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Shiny02.jpg");
        for (int i = 0; i < 2; i++)
        {
            _returnModels[i] = new DrainReturnModel(_caster, i == 1) { World = World };
            await _returnModels[i].Load();
        }
    }
    private Vector3 CasterChest() => _caster is PlayerObject player && player.TryGetBoneWorldMatrix(18, out var bone)
        ? bone.Translation : _caster.WorldPosition.Translation + Vector3.UnitZ * 100f;
    private void EmitMote(WalkerObject owner, float factor)
    {
        float angle = MathHelper.ToRadians(MuGame.Random.Next(360));
        _motes[_nextMote] = new Mote
        {
            Position = owner.WorldPosition.Translation + new Vector3(MuGame.Random.Next(-40, 40), MuGame.Random.Next(-30, 30), MuGame.Random.Next(-20, 160)),
            Velocity = new Vector3(-MathF.Sin(angle), MathF.Cos(angle), 0f) * (MuGame.Random.Next(60, 62) * .1f) + Vector3.UnitZ * (2f * factor),
            Light = new Vector3(1f, .2f, .2f), Life = 18f, Scale = 1.8f + MuGame.Random.Next(8, 12) * .01f, Alpha = 1f
        };
        _nextMote = (_nextMote + 1) % ParticleCapacity;
    }
    private void EmitJoint(Vector3 origin, Vector3 target, bool returning, float factor)
    {
        for (int n = 0; n < JointCapacity; n++)
        {
            int index = (_nextJoint + n) % JointCapacity;
            if (_joints[index].Life > 0f) continue;
            int extra = returning ? MuGame.Random.Next(10) : 0;
            Vector3 angle = new(MathHelper.ToDegrees(_caster.Angle.X), MathHelper.ToDegrees(_caster.Angle.Y), MathHelper.ToDegrees(_caster.Angle.Z));
            angle.Z += FPSCounter.Instance.RandFPSCheck(2) ? 90f : -90f;
            angle += new Vector3(MuGame.Random.Next(-50, 50), MuGame.Random.Next(-50, 50), MuGame.Random.Next(-50, 50));
            _joints[index] = new Joint
            {
                Position = origin, Target = target, Returning = returning, Angle = angle,
                Light = returning ? new Vector3(.2f, .2f, 1f) : new Vector3(.8f, .1f, .2f),
                Life = returning ? 30f + extra : MuGame.Random.Next(20, 40), ExtraLife = extra,
                Width = returning ? 30f : MuGame.Random.Next(10, 70), Speed = 1f + MuGame.Random.Next(10) * .2f,
                Limit = Math.Clamp((int)((returning ? 15f : MuGame.Random.Next(15, 25)) / MathF.Max(.01f, factor)), 2, MaxHistory)
            };
            _nextJoint = (index + 1) % JointCapacity;
            return;
        }
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready) return;
        if (_caster.Status == GameControlStatus.Disposed || _victim.Status == GameControlStatus.Disposed) { Remove(); return; }
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _age += f;
        if (_life > 0f)
        {
            int roll = MuGame.Random.Next(10), count = roll == 0 ? 0 : roll < 8 ? 1 : 2;
            for (int i = 0; i < count; i++)
                if (FPSCounter.Instance.RandFPSCheck(1)) { EmitMote(_caster, f); if (_life <= 60f) EmitMote(_victim, f); }
            if (_life >= 66f && _life <= 70f)
            {
                int ghostRoll = MuGame.Random.Next(10), ghosts = ghostRoll == 0 ? 0 : ghostRoll < 4 ? 1 : ghostRoll < 9 ? 3 : 4;
                for (int i = 0; i < ghosts; i++)
                    if (FPSCounter.Instance.RandFPSCheck(1))
                    {
                        Vector3 source = CasterChest() + SourceJointMath.MuMainRotate(Vector3.UnitY * 100f, new Vector3(0f, 0f, MathHelper.ToDegrees(_caster.Angle.Z)))
                            + new Vector3(MuGame.Random.Next(10), MuGame.Random.Next(10), MuGame.Random.Next(10)) * 5f;
                        EmitJoint(source, _victim.WorldPosition.Translation + Vector3.UnitZ * (100f + MuGame.Random.Next(-5, 5) * 4f), false, f);
                    }
            }
            if (!_returned && _life <= 64f)
            {
                _returned = true;
                var bones = _victim.GetBoneTransforms();
                for (int i = 0; i < Math.Min(bones?.Length ?? 0, 128); i++)
                    if (FPSCounter.Instance.RandFPSCheck(2))
                        EmitJoint(_victim.WorldPosition.Translation + Vector3.Transform(Vector3.Zero, bones![i]) * _victim.Scale,
                            _caster.WorldPosition.Translation + Vector3.UnitZ * 80f, true, f);
            }
            _life -= f;
        }
        for (int i = 0; i < ParticleCapacity; i++)
        {
            ref var mote = ref _motes[i];
            if (mote.Life <= 0f) continue;
            mote.Velocity.Z -= .1f * f;
            mote.Position += mote.Velocity * f;
            mote.Scale += .04f * f;
            if (mote.Life < 10f) mote.Alpha -= .1f * f;
            if (mote.Life < 20f) { mote.Velocity.X *= MathF.Pow(.6f, f); mote.Velocity.Y *= MathF.Pow(.6f, f); }
            mote.Light *= MathF.Max(0f, mote.Alpha);
            mote.Life -= f;
        }
        for (int i = 0; i < JointCapacity; i++)
        {
            ref var joint = ref _joints[i];
            if (joint.Life <= 0f) continue;
            if (!joint.Returning)
            {
                joint.Position += SourceJointMath.MuMainRotate(new Vector3(0f, -joint.Speed, 0f), joint.Angle) * f;
                Vector3 target = _victim.WorldPosition.Translation; target.Z = joint.Target.Z;
                SourceJointMath.MuMainMoveHumming(ref joint.Position, ref joint.Angle, target, joint.Speed, f);
                joint.Speed += 2f * f;
                if (joint.Life < 10f) joint.Light *= MathF.Pow(1f / 1.2f, f);
            }
            else
            {
                float frame = (int)(gameTime.TotalGameTime.TotalMilliseconds / 40f);
                frame = (i % 2 == 1 ? frame : -frame) + i * 53731f;
                float a = MathF.Sin((frame + 55555f) * .048f), b = frame * .0613f, c = (frame + 11111f) * .1113f;
                Vector3 temp = new(a * MathF.Cos(b), a * MathF.Sin(b), MathF.Cos((frame + 55555f) * .048f));
                Vector3 dir = new(MathF.Cos(c) * temp.Y - MathF.Sin(c) * temp.Z, MathF.Sin(c) * temp.Y + MathF.Cos(c) * temp.Z, temp.X);
                float life = joint.Life * 40f / 30f;
                float positionWeight = (life < 10f ? life * 7f : life + 60f) / (30f + joint.ExtraLife) * 30f;
                float circle = MathF.Min(MathF.Max(0f, 40f - life) * 15f, 150f);
                Vector3 wobble = new(MathF.Cos((i * 51231f + frame / 10f) * .01f), MathF.Cos((i * 51231f + 3711f + frame / 10f) * .01f), MathF.Cos((i * 51231f + 7422f + frame / 10f) * .01f));
                joint.Position = ((joint.Target + dir * circle) * positionWeight + (_caster.WorldPosition.Translation + wobble * 25f) * (100f - positionWeight)) * .01f + Vector3.UnitZ * 100f;
                if (joint.Life <= 1f && !_modelsStarted)
                {
                    _modelsStarted = true;
                    for (int m = 0; m < 2; m++) { _returnModels[m].Start(); World?.Objects.Add(_returnModels[m]); }
                }
            }
            _history[i, joint.Head] = joint.Position; _tailAngles[i, joint.Head] = joint.Angle;
            joint.Head = (joint.Head + 1) % joint.Limit; joint.Count = Math.Min(joint.Count + 1, joint.Limit);
            joint.Life -= f;
        }
        if (_age >= 110f) Remove();
    }
    private void Sprite(Texture2D texture, Vector3 position, Vector3 light, float scale)
    {
        Matrix camera = Matrix.Invert(Camera.Instance.View);
        int count = 0;
        Vector3 up = camera.Up * (texture.Height * scale * .5f);
        SourceEffectGeometry.Quad(_vertices, ref count, position - up, position + up, camera.Right * (texture.Width * scale * .5f), new Color(light), 0f, 1f);
        ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count, samplerOverride: SamplerState.LinearClamp);
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        if (_light != null) for (int i = 0; i < ParticleCapacity; i++) if (_motes[i].Life > 0f) Sprite(_light, _motes[i].Position, _motes[i].Light, _motes[i].Scale);
        for (int i = 0; i < JointCapacity; i++)
        {
            ref var joint = ref _joints[i];
            Texture2D? texture = joint.Returning ? _energy : _ghost;
            if (joint.Life <= 0f || texture == null) continue;
            int count = 0;
            for (int j = 0; j < joint.Count - 1; j++)
            {
                int current = (joint.Head - 1 - j + joint.Limit) % joint.Limit, next = (current - 1 + joint.Limit) % joint.Limit;
                Vector3 a = _history[i, current], b = _history[i, next];
                if (Vector3.DistanceSquared(a, b) > 3600f) continue;
                float u = (joint.Count - j) / (float)(joint.Limit - 1), v = (joint.Count - j - 1) / (float)(joint.Limit - 1);
                SourceEffectGeometry.Quad(_vertices, ref count, a, b, SourceJointMath.MuMainRotate(Vector3.UnitX * (joint.Width * .5f), _tailAngles[i, current]), new Color(joint.Light), u, v);
                SourceEffectGeometry.Quad(_vertices, ref count, a, b, SourceJointMath.MuMainRotate(Vector3.UnitZ * (joint.Width * .5f), _tailAngles[i, current]), new Color(joint.Light), u, v);
            }
            ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count, samplerOverride: SamplerState.LinearClamp);
            if (joint.Returning)
            {
                if (_shiny != null) Sprite(_shiny, joint.Position, new Vector3(.5f, .5f, 1f), .85f);
                if (_light != null) Sprite(_light, joint.Position, new Vector3(.5f, .5f, 1f), 1.5f);
            }
        }
    }
    private void Remove() { World?.RemoveObject(this); Dispose(); }
    public override void Dispose()
    {
        for (int i = 0; i < 2; i++) { _returnModels[i]?.World?.RemoveObject(_returnModels[i]); _returnModels[i]?.Dispose(); }
        base.Dispose();
    }
    private sealed class DrainReturnModel : ModelObject
    {
        private readonly WalkerObject _owner;
        private readonly bool _cylinder;
        private float _life = 10f;
        public DrainReturnModel(WalkerObject owner, bool cylinder)
        {
            _owner = owner; _cylinder = cylinder;
            Scale = cylinder ? .1f : .4f; BlendMesh = -2; BlendMeshLight = cylinder ? 1f : .7f;
            Light = cylinder ? Vector3.Zero : Vector3.One; LightEnabled = false;
            BlendState = Blendings.OneOneAdditive; BlendMeshState = Blendings.OneOneAdditive;
            IsTransparent = true; AffectedByTransparency = false; RenderShadow = false; DepthState = DepthStencilState.DepthRead;
        }
        public void Start() => Position = _owner.WorldPosition.Translation + Vector3.UnitZ * (_cylinder ? 0f : 22f);
        protected override bool AllowDynamicLightingShader => false;
        public override async Task Load() { Model = await BMDLoader.Instance.Prepare(_cylinder ? "Effect/clinderlight.bmd" : "Effect/Change_Up_Eff.bmd"); await base.Load(); }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            if (_cylinder)
            {
                BlendMeshLight = _life * .015f;
                Scale += .08f * FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            }
            _life -= FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_life <= 0f) { World?.RemoveObject(this); Dispose(); }
        }
    }
}
