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

/// <summary>MuMain NextGradeObjectRender's +15 hand lights and subtype-zero mega lightning.</summary>
public sealed class Grade15WeaponEffect : EffectObject
{
    private struct Particle
    {
        public Vector3 Position;
        public float Life, Alpha, Scale, Rotation;
        public int Texture;
    }
    private readonly PlayerObject _owner;
    private readonly Texture2D?[] _textures = new Texture2D?[6];
    private readonly Particle[] _particles = new Particle[32];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[6];
    private readonly Matrix[] _hand = new Matrix[2], _tip = new Matrix[2];
    private readonly bool[] _active = new bool[2];
    private readonly float[] _flareScale = new float[2];
    private Task? _preparation;
    private int _next;
    private float _time;
    public int ActiveHands { get; private set; }
    public int LiveParticles { get; private set; }
    public string Diagnostic => _preparation?.Exception?.ToString() ?? _preparation?.Status.ToString() ?? "inactive";

    public Grade15WeaponEffect(PlayerObject owner)
    {
        _owner = owner;
        IsTransparent = true;
        AffectedByTransparency = true;
        LightEnabled = false;
        BlendState = BlendState.Additive;
        DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new BoundingBox(new Vector3(-200), new Vector3(200));
        Hidden = true;
    }

    private async Task PrepareTextures()
    {
        string[] names = ["flare01.jpg", "Magic_Ground1.jpg", "flareRed.jpg",
            "lighting_mega01.jpg", "lighting_mega02.jpg", "lighting_mega03.jpg"];
        for (int i = 0; i < names.Length; i++)
        {
            var texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/" + names[i]);
            if (IsDisposeRequested) return;
            _textures[i] = texture;
        }
    }

    public override void Update(GameTime time)
    {
        base.Update(time);
        if (Status != GameControlStatus.Ready) return;
        ActiveHands = 0;
        _time = (float)time.TotalGameTime.TotalSeconds;
        for (int i = 0; i < 2; i++)
        {
            WeaponObject weapon = i == 0 ? _owner.Weapon1 : _owner.Weapon2;
            // MuMain treats bows in either inventory hand as using the first hand's bones.
            bool bow = weapon.ItemGroup == 4;
            int bone = bow || i == 0 ? 27 : 36;
            _active[i] = weapon.ItemLevel == 15 && weapon.Model != null && !weapon.Hidden &&
                _owner.TryGetBoneWorldMatrix(bone, out _hand[i]) &&
                _owner.TryGetBoneWorldMatrix(bone + 1, out _tip[i]);
            if (_active[i]) ActiveHands++;
        }
        if (ActiveHands == 0)
        {
            Hidden = true;
            LiveParticles = 0;
            Array.Clear(_particles);
            return;
        }
        _preparation ??= PrepareTextures();
        Hidden = !_preparation.IsCompletedSuccessfully;
        if (Hidden) return;
        float factor = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        LiveParticles = 0;
        for (int i = 0; i < _particles.Length; i++)
        {
            ref var particle = ref _particles[i];
            if (particle.Life <= 0) continue;
            particle.Life -= factor;
            particle.Alpha = MathF.Max(0, particle.Alpha - .15f * factor);
            if (particle.Life > 0 && particle.Alpha >= .1f) LiveParticles++;
        }
        for (int i = 0; i < 2; i++)
        {
            if (!_active[i]) continue;
            _flareScale[i] = .6f * MuGame.Random.Next(60) * .01f + .4f;
            if (!FPSCounter.Instance.RandFPSCheck(1)) continue;
            _particles[_next] = new Particle
            {
                Position = Vector3.Transform(new Vector3(10, 0, 0), _tip[i]),
                Life = 5, Alpha = 1,
                Scale = MuGame.Random.Next(10, 90) * .01f,
                Rotation = MathHelper.ToRadians(MuGame.Random.Next(360)),
                Texture = MuGame.Random.Next(3, 6)
            };
            _next = (_next + 1) % _particles.Length;
            LiveParticles++;
        }
    }

    private void Quad(int textureIndex, Vector3 position, float scale, Vector3 color, Matrix camera, float rotation = 0)
    {
        var texture = _textures[textureIndex];
        if (texture == null) return;
        Vector3 right = (camera.Right * MathF.Cos(rotation) + camera.Up * MathF.Sin(rotation)) * (texture.Width * scale * .5f);
        Vector3 up = (camera.Up * MathF.Cos(rotation) - camera.Right * MathF.Sin(rotation)) * (texture.Height * scale * .5f);
        int count = 0;
        SourceEffectGeometry.Quad(_vertices, ref count, position - up, position + up, right,
            new Color(color * TotalAlpha), 0, 1);
        ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count, samplerOverride: SamplerState.LinearClamp);
    }

    public override void Draw(GameTime time)
    {
        if (!Visible || Status != GameControlStatus.Ready || Hidden) return;
        Matrix camera = Matrix.Invert(Camera.Instance.View);
        float pulse = MathF.Abs(MathF.Sin(_time * 2));
        for (int i = 0; i < 2; i++)
        {
            if (!_active[i]) continue;
            for (int offset = 0; offset <= 20; offset += 10)
                Quad(0, Vector3.Transform(new Vector3(offset, 0, 0), _hand[i]), .6f, new Vector3(1, .6f, 0), camera);
            Vector3 tip = Vector3.Transform(new Vector3(10, 0, 0), _tip[i]);
            Quad(1, tip, .35f, new Vector3(.7f * pulse + .3f, .1f * pulse + .1f, 0), camera);
            Quad(2, tip, _flareScale[i], Vector3.One, camera);
        }
        for (int i = 0; i < _particles.Length; i++)
        {
            ref var particle = ref _particles[i];
            if (particle.Life <= 0 || particle.Alpha < .1f) continue;
            Quad(particle.Texture, particle.Position, particle.Scale, new Vector3(1, .2f, 0) * particle.Alpha, camera, particle.Rotation);
        }
    }
}
