#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain Power Slash: five MODEL_MAGIC2 subtype-2 waves, twenty degrees apart.</summary>
public sealed class PowerSlashEffect : StagedCombatEffect
{
    public PowerSlashEffect(WalkerObject caster) : base(caster, 71f) { }
    protected override void BuildParts()
    {
        for (int i = 0; i < 5; i++)
        {
            var smoke = new PowerWaveEffect.PowerWaveSmoke(128);
            AddPart(smoke, 0f);
            AddPart(new SlashWave(Origin, Caster.Angle + Vector3.UnitZ * MathHelper.ToRadians(-40f + 20f * i), smoke), 0f);
        }
    }

    private sealed class SlashWave : ModelObject
    {
        private float _life = 20f;
        private Texture2D? _shiny, _light;
        private readonly UvScrollDeformer _scroll = new();
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[6];
        private readonly Vector3 _velocity;
        private float _rotationA, _rotationB, _rotationLight;
        private float _tick;
        private float _luminosity = .8f;
        private readonly PowerWaveEffect.PowerWaveSmoke _smoke;
        private readonly DynamicLight _terrainLight;
        private bool _lightAdded;
        public SlashWave(Vector3 origin, Vector3 angle, PowerWaveEffect.PowerWaveSmoke smoke)
        {
            _smoke = smoke;
            _terrainLight = new DynamicLight { Owner = this, Position = origin, Color = new Vector3(.3f, .6f, 1f), Radius = 300f, Intensity = .8f };
            Position = origin; Angle = angle; Scale = .9f;
            _velocity = SourceJointMath.MuMainRotate(new Vector3(0f, -60f, 0f),
                new Vector3(MathHelper.ToDegrees(angle.X), MathHelper.ToDegrees(angle.Y), MathHelper.ToDegrees(angle.Z)));
            HiddenMesh = 0; BlendMesh = 0;
            BlendMeshState = Blendings.OneOneAdditive; BlendState = Blendings.OneOneAdditive;
            LightEnabled = true; Light = Vector3.One; RenderShadow = false;
            IsTransparent = true; AffectedByTransparency = false;
            DepthState = DepthStencilState.DepthRead;
            BoundingBoxLocal = new BoundingBox(new Vector3(-400f), new Vector3(400f));
        }
        protected override IVertexDeformer GetVertexDeformer() => _scroll;
        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare("Skill/Magic02.bmd");
            _shiny = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Shiny02.jpg");
            _light = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
            await base.Load();
            if (!IsDisposeRequested && World?.Terrain != null) { World.Terrain.AddDynamicLight(_terrainLight); _lightAdded = true; }
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _smoke.EmitSlashSmoke(Position, Angle, new Vector3(.3f, .6f, 1f) * _luminosity, f);
            _terrainLight.Position = Position;
            _terrainLight.Intensity = _luminosity;
            BlendMeshLight = _life * .1f;
            _scroll.OffsetU = -_life * .2f;
            InvalidateBuffers(BufferFlagTexture);
            Position += _velocity * f;
            _tick += f;
            if (_tick >= 1f)
            {
                _tick %= 1f;
                _rotationA = MathHelper.ToRadians(MuGame.Random.Next(360));
                _rotationB = MathHelper.ToRadians(MuGame.Random.Next(360));
                _rotationLight = MathHelper.ToRadians(MuGame.Random.Next(360));
                _luminosity = MuGame.Random.Next(7, 11) * .1f;
            }
            _life -= f;
            if (_life <= 0f) { World?.RemoveObject(this); Dispose(); }
        }
        public override void Dispose()
        {
            if (_lightAdded) World?.Terrain?.RemoveDynamicLight(_terrainLight);
            _lightAdded = false;
            base.Dispose();
        }
        public override void DrawAfter(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || _shiny == null || _light == null) return;
            var cameraWorld = Matrix.Invert(Camera.Instance.View);
            Vector3 position = Position + Vector3.UnitZ * 50f;
            DrawFlash(_shiny, position, cameraWorld, 1.5f, _rotationA, Vector3.One);
            DrawFlash(_shiny, position, cameraWorld, 1.5f, _rotationB, Vector3.One);
            DrawFlash(_light, position, cameraWorld, 3.5f, _rotationLight, new Vector3(.3f, .6f, 1f) * _luminosity);
        }
        private void DrawFlash(Texture2D texture, Vector3 center, Matrix cameraWorld, float scale, float rotation, Vector3 light)
        {
            float sin = MathF.Sin(rotation), cos = MathF.Cos(rotation);
            Vector3 right = (cameraWorld.Right * cos + cameraWorld.Up * sin) * texture.Width * scale * .5f;
            Vector3 up = (cameraWorld.Up * cos - cameraWorld.Right * sin) * texture.Height * scale * .5f;
            Color tint = new(light);
            var a = new VertexPositionColorTexture(center - right - up, tint, new Vector2(0, 1));
            var b = new VertexPositionColorTexture(center + right - up, tint, new Vector2(1, 1));
            var c = new VertexPositionColorTexture(center + right + up, tint, new Vector2(1, 0));
            var d = new VertexPositionColorTexture(center - right + up, tint, new Vector2(0, 0));
            _vertices[0] = a; _vertices[1] = b; _vertices[2] = c;
            _vertices[3] = a; _vertices[4] = c; _vertices[5] = d;
            ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, 6);
        }
        private sealed class UvScrollDeformer : IVertexDeformer, ITexCoordDeformer
        {
            public float OffsetU;
            public Vector3 DeformVertex(in Client.Data.BMD.BMDTextureVertex vertex, in Vector3 transformedPosition) => transformedPosition;
            public Vector2 DeformTexCoord(float u, float v) => new(u + OffsetU, v);
        }
    }
}
