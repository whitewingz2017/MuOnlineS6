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

/// <summary>Party Teleport's MODEL_CIRCLE subtype 2 and MODEL_CIRCLE_LIGHT subtype 3.</summary>
public sealed class PartySummonEffect : StagedCombatEffect
{
    public PartySummonEffect(WalkerObject caster) : base(caster, 326f) { }
    protected override void BuildParts()
    {
        AddPart(new SummonCircle(Origin, Caster.Angle, false), 0f);
        AddPart(new SummonCircle(Origin, Caster.Angle, true), 0f);
        AddPart(new PartySummonParticlesEffect(Origin), 0f);
    }

    private sealed class SummonCircle : ModelObject
    {
        private readonly bool _column;
        private readonly Scroll _scroll = new();
        private float _life = 250f;
        public SummonCircle(Vector3 position, Vector3 angle, bool column)
        {
            _column = column;
            Position = position; Angle = angle; Scale = 1f;
            BlendMesh = 0; BlendMeshLight = 0f;
            BlendState = BlendState.Opaque; BlendMeshState = Blendings.OneOneAdditive;
            DepthState = DepthStencilState.DepthRead;
            // MuMain overrides BodyLight for these models; terrain lighting must not wash out that tint.
            LightEnabled = false; Light = column ? new Vector3(.1f, .1f, 10f) : new Vector3(.5f, .5f, 1f);
            RenderShadow = false; IsTransparent = true; AffectedByTransparency = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-600f), new Vector3(600f));
        }
        protected override IVertexDeformer GetVertexDeformer() => _scroll;
        // MuMain supplies an explicit BodyLight here. The terrain shader's lighting
        // replaces that model color; use the vertex-colored effect path instead.
        protected override bool AllowDynamicLightingShader => false;
        public override void DrawMesh(int mesh)
        {
            // Other sprite/model passes share this effect and can disable vertex colors.
            // The circle's source BodyLight is baked into its vertices, so restore its
            // required shader state locally rather than changing unrelated render passes.
            var effect = GraphicsManager.Instance.AlphaTestEffect3D;
            bool vertexColors = effect.VertexColorEnabled;
            Vector3 diffuse = effect.DiffuseColor;
            try
            {
                effect.VertexColorEnabled = true;
                effect.DiffuseColor = Vector3.One;
                base.DrawMesh(mesh);
            }
            finally { effect.VertexColorEnabled = vertexColors; effect.DiffuseColor = diffuse; }
        }
        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare(_column ? "Skill/Circle02.bmd" : "Skill/Circle01.bmd");
            await base.Load();
            if (!_column && Model != null)
            {
                var emblem = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Magic_b.jpg");
                if (emblem != null)
                    for (int i = 0; i < Model.Meshes.Length; i++) SetMeshTextureOverride(i, emblem);
            }
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            _life -= FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            float luminosity = _life > 240f ? (250f - _life) * .1f : _life * .1f;
            BlendMeshLight = _column ? MathF.Min(.5f, luminosity) : luminosity;
            if (_column) { _scroll.OffsetU = -_life * .01f; InvalidateBuffers(BufferFlagTexture); }
            if (_life <= 0f) { World?.RemoveObject(this); Dispose(); }
        }
        private sealed class Scroll : IVertexDeformer, ITexCoordDeformer
        {
            public float OffsetU;
            public Vector3 DeformVertex(in Client.Data.BMD.BMDTextureVertex vertex, in Vector3 transformedPosition) => transformedPosition;
            public Vector2 DeformTexCoord(float u, float v) => new(u + OffsetU, v);
        }
    }
}
