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

/// <summary>MuMain's six MODEL_SPEARSKILL subtype-5/6/7 removal ribbons.</summary>
public sealed class AbolishMagicEffect : EffectObject
{
    private const int TrailCount = 6;
    private const int Capacity = 200;
    private struct Trail
    {
        public Vector3 Center, Angle, Light;
        public float Radius, Width, Weapon;
        public int Head, Count;
    }
    private readonly Trail[] _trails = new Trail[TrailCount];
    private readonly Vector3[,] _left = new Vector3[TrailCount, Capacity];
    private readonly Vector3[,] _right = new Vector3[TrailCount, Capacity];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(Capacity - 1) * 6];
    private Texture2D? _texture;
    private int _tails;
    private float _life = 60f;
    public AbolishMagicEffect(WalkerObject caster)
    {
        Position = caster.WorldPosition.Translation;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        BoundingBoxLocal = new BoundingBox(new Vector3(-1500f), new Vector3(1500f));
        for (int i = 0; i < TrailCount; i++)
        {
            int subtype = i % 3;
            _trails[i] = new Trail
            {
                Center = Position + Vector3.UnitZ * (100f - Math.Min(i, 2) * 10f - Math.Max(0, i - 3) * 10f),
                Angle = new Vector3(0f, 0f, i < 3 ? 45f + i * 90f : 90f + (i - 3) * 90f),
                Light = subtype == 0 ? new Vector3(1f, 1f, .8f) : subtype == 1 ? new Vector3(1f, .8f, 1f) : new Vector3(.8f, 1f, 1f),
                Radius = 800f, Width = 170f
            };
        }
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Flare02.jpg");
        _tails = Math.Clamp((int)(30f / MathF.Max(.01f, FPSCounter.Instance.FPS_ANIMATION_FACTOR)), 2, Capacity);
        for (int i = 0; i < TrailCount; i++) PushTail(i);
    }
    private void PushTail(int index)
    {
        ref var trail = ref _trails[index];
        var center = trail.Center + SourceJointMath.MuMainRotate(new Vector3(0f, trail.Radius, 0f), trail.Angle);
        // RENDER_FACE_ONE uses source tail vertices 2/3: the local Z axis.
        SourceJointMath.MuMainAngleBasis(trail.Angle, out _, out _, out var up);
        var half = up * (trail.Width * .5f);
        _left[index, trail.Head] = center - half;
        _right[index, trail.Head] = center + half;
        trail.Head = (trail.Head + 1) % _tails;
        trail.Count = Math.Min(trail.Count + 1, _tails);
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        for (int i = 0; i < TrailCount; i++)
        {
            ref var trail = ref _trails[i];
            for (int step = 0; step < 3; step++)
            {
                trail.Angle.Z += 10f * f;
                PushTail(i);
                if (trail.Weapon == 0f)
                {
                    trail.Radius *= MathF.Pow(.95f, f);
                    if (trail.Radius < 10f) trail.Radius = -10f;
                }
                if (trail.Weapon > 40f)
                {
                    trail.Radius -= 20f * f;
                    trail.Angle.Z -= 5f * f;
                    trail.Width += 15f * f;
                }
                if (trail.Radius < 0f)
                {
                    trail.Weapon += f;
                    if (trail.Weapon < 20f) { trail.Radius = -30f; trail.Width = 40f; }
                    else trail.Center += Vector3.UnitZ * (5f * f);
                }
            }
            if (_life < 10f) trail.Light *= MathF.Pow(1f / 1.2f, f);
            trail.Width -= 5f * f;
        }
        _life -= f;
        if (_life < 0f) { World?.RemoveObject(this); Dispose(); }
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready || _texture == null) return;
        for (int i = 0; i < TrailCount; i++)
        {
            ref var trail = ref _trails[i];
            Color tint = new(trail.Light);
            int count = 0;
            for (int j = 0; j < trail.Count - 1; j++)
            {
                int current = (trail.Head - 1 - j + _tails) % _tails, next = (trail.Head - 2 - j + _tails) % _tails;
                var center = (_left[i, current] + _right[i, current]) * .5f;
                var nextCenter = (_left[i, next] + _right[i, next]) * .5f;
                if (Vector3.DistanceSquared(center, nextCenter) > 3600f) continue;
                float u = (trail.Count - j) / (float)(_tails - 1), v = (trail.Count - j - 1) / (float)(_tails - 1);
                var a = new VertexPositionColorTexture(_left[i, current], tint, new Vector2(u, 0));
                var b = new VertexPositionColorTexture(_right[i, current], tint, new Vector2(u, 1));
                var c = new VertexPositionColorTexture(_right[i, next], tint, new Vector2(v, 1));
                var d = new VertexPositionColorTexture(_left[i, next], tint, new Vector2(v, 0));
                _vertices[count++] = a; _vertices[count++] = b; _vertices[count++] = c;
                _vertices[count++] = a; _vertices[count++] = c; _vertices[count++] = d;
            }
            ElfSupportBuffEffect.DrawWorldGeometry(_texture, _vertices, count);
        }
    }
}
