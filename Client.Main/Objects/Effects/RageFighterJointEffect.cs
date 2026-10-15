#nullable enable
using System;
using System.Threading.Tasks;
using Client.Data.BMD;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain RF joint families with bounded histories and historical bone sampling.</summary>
public sealed class RageFighterJointEffect : EffectObject
{
    private const int Capacity = 96, TailCapacity = 200;
    private struct Joint
    {
        internal Vector3 Position, Light, AxisX, AxisZ;
        internal float Life, Width, FrameOffset, Velocity;
        internal int Count, Bone, Texture, Tails;
    }
    private readonly Joint[] _joints = new Joint[Capacity];
    private readonly Vector3[] _history = new Vector3[Capacity * TailCapacity];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(TailCapacity - 1) * 12];
    private readonly Texture2D?[] _textures = new Texture2D?[5];
    private readonly WalkerObject _caster;
    private readonly ushort _skill;
    private BMD? _punchRight, _punchLeft, _kick;
    private int _next;
    private float _age;
    public RageFighterJointEffect(WalkerObject caster, ushort skill)
    {
        _caster = caster; _skill = skill; Position = caster.WorldPosition.Translation;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new(new(-2000), new(2000));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        string[] textures = ["pin_lights.jpg", "force_Pillar.jpg", "!SwordEff.jpg", "ground_wind.jpg", "Lava.jpg"];
        for (int i = 0; i < textures.Length; i++) _textures[i] = await TextureLoader.Instance.PrepareAndGetTexture("Effect/" + textures[i]);
        if (_skill == 261)
        {
            _punchRight = await BMDLoader.Instance.Prepare("Effect/down_left_punch.bmd");
            _punchLeft = await BMDLoader.Instance.Prepare("Effect/down_right_punch.bmd");
        }
        if (_skill == 265) _kick = await BMDLoader.Instance.Prepare("Effect/dragon_kick_dummy.bmd");
    }
    private void Emit(int texture, int bone, float offset, float width, float life, int tails, Vector3 light, Vector3 position, float velocity = 0)
    {
        _joints[_next] = new() { Texture = texture, Bone = bone, FrameOffset = offset, Width = width,
            Life = life, Tails = tails, Light = light, Position = position, Velocity = velocity,
            AxisX=Vector3.TransformNormal(Vector3.UnitX,Matrix.CreateRotationX(_caster.Angle.X)*Matrix.CreateRotationY(_caster.Angle.Y)*Matrix.CreateRotationZ(_caster.Angle.Z)),
            AxisZ=Vector3.TransformNormal(Vector3.UnitZ,Matrix.CreateRotationX(_caster.Angle.X)*Matrix.CreateRotationY(_caster.Angle.Y)*Matrix.CreateRotationZ(_caster.Angle.Z)) };
        _next = (_next + 1) % Capacity;
    }
    internal static float Speed(WalkerObject caster, int action, float fallback)
        => caster.Model?.Actions is { } actions && action >= 0 && action < actions.Length && actions[action].PlaySpeed > 0
            ? actions[action].PlaySpeed : fallback;
    public override void Update(GameTime time)
    {
        base.Update(time); if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _age += f;
        float frame = (float)_caster.GetAnimationTime();
        Matrix world = _caster.WorldPosition;
        bool emitting = FPSCounter.Instance.RandFPSCheck(1);
        if (_skill == 260 && _caster.CurrentAction == (int)PlayerAction.PlayerSkillThrust && frame < 10 && emitting)
        {
            Vector3 bone = RageFighterBoneSampler.Sample(_caster.Model, _caster.CurrentAction, frame, 27, world);
            for (int i = 0; i < 3; i++)
                Emit(0, -1, 0, 5, MuGame.Random.Next(1, 4), 5, Vector3.One,
                    bone + new Vector3(0, MuGame.Random.Next(-10, 10), MuGame.Random.Next(6, 34)), MuGame.Random.Next(15, 75));
        }
        if (_skill == 261 && _caster.CurrentAction == (int)PlayerAction.PlayerSkillStamp && emitting)
        {
            float punchFrame = _age * Speed(_caster, (int)PlayerAction.PlayerSkillStamp, .4f) * 2;
            var model = punchFrame < 6 ? _punchRight : _punchLeft;
            float sampleFrame = punchFrame < 6 ? punchFrame : punchFrame - 6;
            if (model != null && sampleFrame < model.Actions[0].NumAnimationKeys - 1)
                Emit(1, 0, 0, 50, 7, 5, new(.95f, .72f, .48f), RageFighterBoneSampler.Sample(model, 0, sampleFrame, 0, world));
        }
        if (_skill == 262 && _caster.CurrentAction == (int)PlayerAction.PlayerSkillGiantswing && frame < 18 && emitting)
            Emit(2, 5, 0, 100, 10, 50, new(.6f, .6f, 1), RageFighterBoneSampler.Sample(_caster.Model, _caster.CurrentAction, frame, 5, world));
        if (_skill == 265 && _caster.CurrentAction == (int)PlayerAction.PlayerSkillDragonkick && frame > 4 && frame < 5 && emitting)
        {
            float speed = Speed(_caster, (int)PlayerAction.PlayerSkillDragonkick, .4f);
            int samples = Math.Clamp((int)(speed * 20), 1, 12);
            for (int i = 0; i < samples; i++)
                Emit(3, 2, i * .1f, 150, 10, 8, new(.7f, .7f, 1), RageFighterBoneSampler.Sample(_kick, 0, _age * speed * 2 + i * .1f, 2, world));
        }
        if (_skill == 264 && _caster.CurrentAction == (int)PlayerAction.PlayerSkillDragonlore && frame > 2 && frame < 8 && emitting)
        {
            float speed = Speed(_caster, (int)PlayerAction.PlayerSkillDragonlore, .3f);
            int samples = Math.Clamp((int)(speed * 10), 1, 12);
            for (int i = 0; i < samples; i++)
            {
                float offset = -(speed * 10 - i) * .1f;
                Emit(4, 36, offset, 20, 10, 10, Vector3.One, RageFighterBoneSampler.Sample(_caster.Model, _caster.CurrentAction, frame + offset, 36, world));
                Emit(4, 28, offset, 20, 10, 10, Vector3.One, RageFighterBoneSampler.Sample(_caster.Model, _caster.CurrentAction, frame + offset, 28, world));
            }
        }
        for (int i = 0; i < Capacity; i++)
        {
            ref Joint joint = ref _joints[i];
            if (joint.Life <= 0) continue;
            if (joint.Texture == 0)
            {
                joint.Velocity += .1f * f;
                joint.Position += new Vector3(MathF.Sin(_caster.Angle.Z), -MathF.Cos(_caster.Angle.Z), 0) * joint.Velocity * f;
                joint.Light *= MathF.Pow(.9f, f);
            }
            else if (joint.Texture == 1)
            {
                float punchFrame = _age * Speed(_caster, (int)PlayerAction.PlayerSkillStamp, .4f) * 2;
                joint.Position = RageFighterBoneSampler.Sample(punchFrame < 6 ? _punchRight : _punchLeft, 0, punchFrame < 6 ? punchFrame : punchFrame - 6, 0, world);
            }
            else if (joint.Texture == 3)
            {
                joint.Position = RageFighterBoneSampler.Sample(_kick, 0, _age * Speed(_caster, (int)PlayerAction.PlayerSkillDragonkick, .4f) * 2 + joint.FrameOffset, 2, world) + Vector3.UnitZ * 20 * f;
                joint.Light *= MathF.Pow(.7f, f);
            }
            else
            {
                joint.Position = RageFighterBoneSampler.Sample(_caster.Model, _caster.CurrentAction, frame + joint.FrameOffset, joint.Bone, world);
                if (joint.Texture == 4) joint.Light *= MathF.Pow(.8f, f);
            }
            int offset = i * TailCapacity;
            int tails = Math.Clamp((int)MathF.Ceiling(joint.Tails / MathF.Max(.01f, f)), 2, TailCapacity);
            for (int j = Math.Min(joint.Count, tails - 1); j > 0; j--) _history[offset + j] = _history[offset + j - 1];
            _history[offset] = joint.Position; joint.Count = Math.Min(joint.Count + 1, tails);
            joint.Life -= f;
        }
        if (_age >= 100 || _caster.Status == GameControlStatus.Disposed) { World?.RemoveObject(this); Dispose(); }
    }
    public override void Draw(GameTime time)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        for (int i = 0; i < Capacity; i++)
        {
            var joint = _joints[i]; var texture = _textures[joint.Texture];
            if (joint.Life <= 0 || joint.Count < 2 || texture == null) continue;
            int count = 0, offset = i * TailCapacity;
            for (int j = 1; j < joint.Count; j++)
            {
                Vector3 a = _history[offset + j - 1], b = _history[offset + j];
                Color tint = new(joint.Light);
                float u = (j - 1) / (float)(joint.Count - 1), v = j / (float)(joint.Count - 1);
                SourceEffectGeometry.Quad(_vertices, ref count, a, b, joint.AxisX * joint.Width * .5f, tint, u, v);
                if(joint.Texture==0)SourceEffectGeometry.Quad(_vertices, ref count, a, b, joint.AxisZ * joint.Width * .5f, tint, u, v);
            }
            ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count, samplerOverride: joint.Texture == 1 ? SamplerState.LinearWrap : SamplerState.LinearClamp);
        }
    }
}
