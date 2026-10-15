#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>Circle Light subtype 3's FLARE_BLUE particles and subtype-19 joints.</summary>
public sealed class PartySummonParticlesEffect : EffectObject
{
    private const int Capacity = 128, MaxHistory = 200;
    private struct Flare
    {
        public Vector3 Position;
        public float Life, Velocity, Brightness;
        public int Head, Count;
        public bool Joint;
    }
    private readonly Flare[] _flares = new Flare[Capacity];
    private readonly Vector3[,] _history = new Vector3[Capacity, MaxHistory];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(MaxHistory - 1) * 12];
    private readonly DynamicLight _terrainLight;
    private Texture2D? _texture;
    private float _age;
    private int _next, _tailCount;
    private bool _lightAdded;
    public PartySummonParticlesEffect(Vector3 origin)
    {
        Position = origin;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new BoundingBox(new Vector3(-600f), new Vector3(600f));
        _terrainLight = new DynamicLight { Owner = this, Position = origin, Color = Vector3.One, Radius = 400f, Intensity = 0f };
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flareBlue.jpg");
        _tailCount = Math.Clamp((int)(20f / MathF.Max(.01f, FPSCounter.Instance.FPS_ANIMATION_FACTOR)), 2, MaxHistory);
        if (!IsDisposeRequested && World?.Terrain != null) { World.Terrain.AddDynamicLight(_terrainLight); _lightAdded = true; }
    }
    private void Emit(Vector3 position, bool joint)
    {
        // At the source emission rate, the pool exceeds the maximum overlapping lifetime.
        // Search a free slot rather than replacing a still-visible flare.
        for (int n = 0; n < Capacity; n++)
        {
            int index = (_next + n) % Capacity;
            if (_flares[index].Life > 0f) continue;
            _flares[index] = new Flare
            {
                Position = position, Joint = joint, Brightness = 1f,
                Life = joint ? 25f + MuGame.Random.Next(50) : 30f + MuGame.Random.Next(10),
                Velocity = joint ? -MuGame.Random.Next(35, 55) : MuGame.Random.Next(100) / 50f
            };
            _next = (index + 1) % Capacity;
            return;
        }
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _age += f;
        float life = 250f - _age;
        _terrainLight.Intensity = MathF.Max(0f, MathF.Min(.5f, life >= 240f ? (250f - life) * .1f : life * .1f));
        if (life > 30f && FPSCounter.Instance.RandFPSCheck(2))
        {
            float angle = MathHelper.ToRadians(MuGame.Random.Next(360));
            float radius = MuGame.Random.Next(200);
            Vector3 spawn = Position + new Vector3(-MathF.Sin(angle) * radius, MathF.Cos(angle) * radius, 0f);
            Emit(spawn, false);
            if (life > 40f) Emit(spawn + Vector3.UnitZ * 600f, true);
        }
        for (int i = 0; i < Capacity; i++)
        {
            ref var flare = ref _flares[i];
            if (flare.Life <= 0f) continue;
            if (flare.Joint)
            {
                if (flare.Life <= 25f) { flare.Velocity -= 5f * f; flare.Position.Z += flare.Velocity * f; }
                _history[i, flare.Head] = flare.Position;
                flare.Head = (flare.Head + 1) % _tailCount;
                flare.Count = Math.Min(flare.Count + 1, _tailCount);
            }
            else
            {
                flare.Velocity = MathF.Min(8f * f, flare.Velocity + .4f * f);
                flare.Position.Z += flare.Velocity * f;
                if (flare.Life < 5f) flare.Brightness *= MathF.Max(0f, 1f - f);
            }
            flare.Life -= f;
        }
        if (_age >= 325f) { World?.RemoveObject(this); Dispose(); }
    }
    private static void Quad(VertexPositionColorTexture[] vertices, ref int count, Vector3 a, Vector3 b, Vector3 width, Color tint, float u, float v)
    {
        var p = new VertexPositionColorTexture(a - width, tint, new Vector2(u, 0));
        var q = new VertexPositionColorTexture(a + width, tint, new Vector2(u, 1));
        var r = new VertexPositionColorTexture(b + width, tint, new Vector2(v, 1));
        var s = new VertexPositionColorTexture(b - width, tint, new Vector2(v, 0));
        vertices[count++] = p; vertices[count++] = q; vertices[count++] = r;
        vertices[count++] = p; vertices[count++] = r; vertices[count++] = s;
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready || _texture == null) return;
        Matrix camera = Matrix.Invert(Camera.Instance.View);
        for (int i = 0; i < Capacity; i++)
        {
            ref var flare = ref _flares[i];
            if (flare.Life <= 0f) continue;
            int count = 0;
            Color tint = new(Vector3.One * flare.Brightness);
            if (!flare.Joint)
            {
                Vector3 up = camera.Up * (_texture.Height * .1f);
                Quad(_vertices, ref count, flare.Position - up, flare.Position + up, camera.Right * (_texture.Width * .1f), tint, 0f, 1f);
            }
            else
                for (int j = 0; j < flare.Count - 1; j++)
                {
                    Vector3 a = _history[i, (flare.Head - 1 - j + _tailCount) % _tailCount];
                    Vector3 b = _history[i, (flare.Head - 2 - j + _tailCount) % _tailCount];
                    if (Vector3.DistanceSquared(a, b) > 3600f) continue;
                    float u = (flare.Count - j) / (float)(_tailCount - 1), v = (flare.Count - j - 1) / (float)(_tailCount - 1);
                    Quad(_vertices, ref count, a, b, new Vector3(14.142136f, 14.142136f, 0f), tint, u, v);
                    Quad(_vertices, ref count, a, b, Vector3.UnitZ * 20f, tint, u, v);
                }
            ElfSupportBuffEffect.DrawWorldGeometry(_texture, _vertices, count);
        }
    }
    public override void Dispose()
    {
        if (_lightAdded) World?.Terrain?.RemoveDynamicLight(_terrainLight);
        _lightAdded = false;
        base.Dispose();
    }
}
