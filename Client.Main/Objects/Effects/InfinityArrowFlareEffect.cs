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

/// <summary>Bounded pool for MuMain's bone-emitted FLARE+1 subtype-16 trails.</summary>
public sealed class InfinityArrowFlareEffect : EffectObject
{
    private const int Capacity = 384;
    private const int MaxHistory = 200;
    private static readonly BlendState FlareBlend = new()
    {
        ColorSourceBlend = Blend.InverseSourceColor, ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.InverseSourceColor, AlphaDestinationBlend = Blend.One
    };
    private struct Trail
    {
        public Vector3 Position, Angle, Light;
        public float Life, Velocity;
        public int Head, Count;
    }
    private readonly ModelObject _anchor;
    private readonly Trail[] _trails = new Trail[Capacity];
    private readonly Vector3[,] _history = new Vector3[Capacity, MaxHistory];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(MaxHistory - 1) * 6];
    private Texture2D? _texture;
    private float _age;
    private int _next;
    private int _tailCount;
    public InfinityArrowFlareEffect(ModelObject anchor)
    {
        _anchor = anchor;
        Position = anchor.Position;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        BoundingBoxLocal = new BoundingBox(new Vector3(-2000f), new Vector3(2000f));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Flare02.jpg");
        _tailCount = Math.Clamp((int)(8f / MathF.Max(.01f, FPSCounter.Instance.FPS_ANIMATION_FACTOR)), 2, MaxHistory);
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _age += f;
        if (_anchor.Status == GameControlStatus.Ready)
        {
            Position = _anchor.Position;
            var bones = _anchor.GetBoneTransforms();
            if (bones != null)
                for (int bone = 1; bone <= 9 && bone < bones.Length; bone++)
                {
                    if (bone == 5 || !FPSCounter.Instance.RandFPSCheck(1)) continue;
                    ref var trail = ref _trails[_next];
                    if (trail.Life > 0f) continue;
                    trail = new Trail
                    {
                        Position = Vector3.Transform(bones[bone].Translation, _anchor.WorldPosition),
                        Angle = new Vector3(MathHelper.ToDegrees(_anchor.Angle.X), MathHelper.ToDegrees(_anchor.Angle.Y), MathHelper.ToDegrees(_anchor.Angle.Z)),
                        Light = new Vector3(1f, .5f, .3f), Life = 35f, Velocity = 7f
                    };
                    _next = (_next + 1) % Capacity;
                }
        }
        for (int i = 0; i < Capacity; i++)
        {
            ref var trail = ref _trails[i];
            if (trail.Life <= 0f) continue;
            if (trail.Life >= 25f)
            {
                trail.Angle.Z -= 10f * f;
                trail.Position += Vector3.UnitZ * (2.5f * f);
            }
            else
            {
                trail.Velocity = MathF.Min(30f, trail.Velocity + 5f * f);
                float oldYaw = trail.Angle.Z;
                float distance = SourceJointMath.MuMainMoveHumming(ref trail.Position, ref trail.Angle, Position, trail.Velocity, f);
                if (distance <= 70f && MathF.Abs(oldYaw - trail.Angle.Z) > 20f && trail.Velocity >= 20f) trail.Velocity -= 10f * f;
                if (trail.Life < 10f) trail.Light = Vector3.One * (trail.Light.X * MathF.Pow(1f / 1.2f, f));
            }
            trail.Position += SourceJointMath.MuMainRotate(new Vector3(0f, -trail.Velocity, 0f), trail.Angle) * f;
            _history[i, trail.Head] = trail.Position;
            trail.Head = (trail.Head + 1) % _tailCount;
            trail.Count = Math.Min(_tailCount, trail.Count + 1);
            trail.Life -= f;
        }
        if (_age >= 75f) { World?.RemoveObject(this); Dispose(); }
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready || _texture == null) return;
        var cameraWorld = Matrix.Invert(Camera.Instance.View);
        Vector3 width = cameraWorld.Right * 10f;
        for (int i = 0; i < Capacity; i++)
        {
            ref var trail = ref _trails[i];
            if (trail.Life <= 0f) continue;
            int count = 0;
            for (int j = 0; j < trail.Count - 1; j++)
            {
                Vector3 a = _history[i, (trail.Head - 1 - j + _tailCount) % _tailCount];
                Vector3 b = _history[i, (trail.Head - 2 - j + _tailCount) % _tailCount];
                if (Vector3.DistanceSquared(a, b) > 3600f) continue;
                float u = (trail.Count - j) / (float)(_tailCount - 1), v = (trail.Count - j - 1) / (float)(_tailCount - 1);
                Color tint = new(trail.Light);
                var p = new VertexPositionColorTexture(a - width, tint, new Vector2(u, 0));
                var q = new VertexPositionColorTexture(a + width, tint, new Vector2(u, 1));
                var r = new VertexPositionColorTexture(b + width, tint, new Vector2(v, 1));
                var s = new VertexPositionColorTexture(b - width, tint, new Vector2(v, 0));
                _vertices[count++] = p; _vertices[count++] = q; _vertices[count++] = r;
                _vertices[count++] = p; _vertices[count++] = r; _vertices[count++] = s;
            }
            ElfSupportBuffEffect.DrawWorldGeometry(_texture, _vertices, count, FlareBlend);
        }
    }
}
