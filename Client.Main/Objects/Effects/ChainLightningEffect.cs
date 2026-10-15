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

/// <summary>MuMain's MODEL_CHAIN_LIGHTNING emitter, with the server-provided hop endpoints.</summary>
public sealed class ChainLightningEffect : EffectObject
{
    private const int MaxTails = 200, RibbonCount = 8;
    private readonly WalkerObject _source, _target;
    private readonly bool _firstHop;
    private readonly Vector3[,] _points = new Vector3[RibbonCount, MaxTails];
    private readonly Vector3[,] _angles = new Vector3[RibbonCount, MaxTails];
    private readonly int[] _counts = new int[RibbonCount];
    private readonly float[] _ribbonLife = new float[RibbonCount];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(MaxTails - 1) * 12];
    private Texture2D? _texture, _light;
    private float _life = 20f;
    private bool _impact;
    private readonly Vector3[] _impactPoints = new Vector3[128];
    private readonly float[] _impactScale = new float[128];
    private int _impactCount;
    private float _impactLife;
    public ChainLightningEffect(WalkerObject source, WalkerObject target, bool firstHop)
    {
        _source = source; _target = target; _firstHop = firstHop;
        Position = source.WorldPosition.Translation;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new BoundingBox(new Vector3(-6000f), new Vector3(6000f));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointThunder01.jpg");
        _light = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
    }
    private Vector3 Origin(int ribbon)
    {
        if (_firstHop && _source is PlayerObject player && player.TryGetBoneWorldMatrix(ribbon < 4 ? 37 : 28, out var bone)) return bone.Translation;
        return _source.WorldPosition.Translation + Vector3.UnitZ * 80f;
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        if (_source.Status == GameControlStatus.Disposed || _target.Status == GameControlStatus.Disposed) { Remove(); return; }
        int ribbons = _firstHop ? 8 : 2;
        int limit = Math.Clamp((int)(50f / MathF.Max(.01f, f)), 2, MaxTails);
        for (int i = 0; i < ribbons; i++)
        {
            _ribbonLife[i] -= f;
            if (_life <= 0f || !FPSCounter.Instance.RandFPSCheck(1)) continue;
            _ribbonLife[i] = 2f;
            _counts[i] = 0;
            Vector3 position = Origin(i), target = _target.WorldPosition.Translation + Vector3.UnitZ * 80f;
            float yaw = MathHelper.ToDegrees(_source.Angle.Z);
            Vector3 angle = _firstHop
                ? new Vector3(i % 4 < 2 ? -60f : 0f, 0f, yaw + (i % 4 < 2 ? 0f : i < 4 ? 60f : -60f))
                : new Vector3(MathHelper.ToDegrees(_target.Angle.X), MathHelper.ToDegrees(_target.Angle.Y), MathHelper.ToDegrees(_target.Angle.Z));
            float width = i % 2 == 0 ? 50f : 10f;
            for (int j = 0; j < limit; j++)
            {
                float distance = SourceJointMath.MuMainMoveHumming(ref position, ref angle, target, 50f, f);
                Vector3 tailAngle = angle + new Vector3(MuGame.Random.Next(-512, 512) / width, 0f, MuGame.Random.Next(-512, 512) / width) * f;
                _points[i, j] = position; _angles[i, j] = tailAngle; _counts[i]++;
                if (distance < 75f) break;
                position += SourceJointMath.MuMainRotate(new Vector3(0f, -50f * f, 0f), tailAngle) * f;
            }
        }
        if (!_impact && _life <= 15f)
        {
            _impact = true; _impactLife = 50f;
            var bones = _target.GetBoneTransforms();
            _impactCount = Math.Min(bones?.Length ?? 0, _impactPoints.Length);
            for (int i = 0; i < _impactCount; i++)
            {
                _impactPoints[i] = _target.WorldPosition.Translation + Vector3.Transform(Vector3.Zero, bones![i]) * _target.Scale;
                _impactScale[i] = 3f + MuGame.Random.Next(-10, 10) * .1f;
            }
        }
        if (_impactLife > 0f) { _impactLife -= f; for (int i = 0; i < _impactCount; i++) _impactScale[i] *= MathF.Pow(.95f, f); }
        _life -= f;
        if (_life < -50f) Remove();
    }
    private void Remove() { World?.RemoveObject(this); Dispose(); }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        if (_texture != null)
            for (int i = 0; i < (_firstHop ? 8 : 2); i++)
            {
                if (_ribbonLife[i] <= 0f) continue;
                int count = 0;
                Color tint = new(new Vector3(.4f, .4f, 1f));
                float width = i % 2 == 0 ? 25f : 5f;
                float scroll = ((int)gameTime.TotalGameTime.TotalMilliseconds % 1000) * .001f;
                for (int j = 0; j < _counts[i] - 1; j++)
                {
                    Vector3 a = _points[i, j], b = _points[i, j + 1];
                    if (Vector3.DistanceSquared(a, b) > 3600f) continue;
                    float u = 2f * (_counts[i] - j) / MaxTails - scroll, v = 2f * (_counts[i] - j - 1) / MaxTails - scroll;
                    SourceEffectGeometry.Quad(_vertices, ref count, a, b, SourceJointMath.MuMainRotate(Vector3.UnitX * width, _angles[i, j]), tint, u, v);
                    SourceEffectGeometry.Quad(_vertices, ref count, a, b, SourceJointMath.MuMainRotate(Vector3.UnitZ * width, _angles[i, j]), tint, u, v);
                }
                ElfSupportBuffEffect.DrawWorldGeometry(_texture, _vertices, count);
            }
        if (_light != null && _impactLife > 0f)
        {
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            for (int i = 0; i < _impactCount; i++)
            {
                int count = 0;
                Vector3 up = camera.Up * (_light.Height * _impactScale[i] * .5f);
                SourceEffectGeometry.Quad(_vertices, ref count, _impactPoints[i] - up, _impactPoints[i] + up,
                    camera.Right * (_light.Width * _impactScale[i] * .5f), new Color(new Vector3(.2f, .2f, .8f)), 0f, 1f);
                ElfSupportBuffEffect.DrawWorldGeometry(_light, _vertices, count, samplerOverride: SamplerState.LinearClamp);
            }
        }
    }
}
