#nullable enable
using System;
using Client.Main.Core.Client;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>Persistent elf buff trails and attack sparkles following the owner.</summary>
    public sealed class ElfSupportBuffEffect : EffectObject
    {
        public override WorldObjectRenderPolicy RenderPolicy => base.RenderPolicy.With(
            forceVisible: true, alwaysUpdate: true);

        private readonly BuffManager _buffs;
        private readonly bool _soulBarrier;
        private readonly VertexPositionColorTexture[] _armVertices = new VertexPositionColorTexture[6 * 6];
        private readonly VertexPositionColorTexture[] _auraSparkVertices = new VertexPositionColorTexture[5 * 6];
        private static readonly int[] UpperBones = { 33, 27, 26, 42, 36, 35 };
        public PlayerObject Owner { get; }
        private Texture2D? _texture;
        private Texture2D? _auraTexture;
        private const int TrailCount = 5;
        private const int TailCount = 30;
        private readonly Vector3[,] _tails = new Vector3[TrailCount, TailCount];
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[TrailCount * (TailCount - 1) * 12];
        private int _tailHead;
        private int _tailLength;
        private float _frameAccumulator;
        private float _leftBrightness = 1f;
        private float _rightBrightness = 1f;

        public ElfSupportBuffEffect(PlayerObject owner, BuffManager buffs, bool soulBarrier = false)
        {
            Owner = owner;
            _buffs = buffs;
            _soulBarrier = soulBarrier;
            Position = owner.WorldPosition.Translation;
            IsTransparent = false;
            AffectedByTransparency = false;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;
            LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-160f, -160f, 0f), new Vector3(160f, 160f, 300f));
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Shiny02.jpg");
            _auraTexture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flareBlue.jpg");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;
            Position = Owner.WorldPosition.Translation;
            if (_soulBarrier && !_buffs.HasBuff(Owner.NetworkId, BuffEffectId.ManaShield))
            {
                _tailLength = 0;
                return;
            }
            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _frameAccumulator = MathF.Min(_frameAccumulator + seconds * 25f, 10f);
            while (_frameAccumulator >= 1f)
            {
                _frameAccumulator -= 1f;
                RecordTails((int)(gameTime.TotalGameTime.TotalMilliseconds / 40) - (int)_frameAccumulator);
            }
            if (Owner.Status == GameControlStatus.Disposed || Owner.IsDead)
            {
                World?.RemoveObject(this);
                Dispose();
                return;
            }

        }

        private void RecordTails(int frame)
        {
            _leftBrightness = MuGame.Random.Next(70, 100) * 0.01f;
            _rightBrightness = MuGame.Random.Next(70, 100) * 0.01f;
            _tailHead = (_tailHead + 1) % TailCount;
            _tailLength = Math.Min(_tailLength + 1, TailCount);
            for (int trail = 0; trail < TrailCount; trail++)
            {
                // ZzzEffectJoint.cpp, MODEL_SPEARSKILL sub4: the joint pool index
                // supplies phase and reverses alternate trails.
                int phase = ((trail % 2 != 0) ? frame : -frame) + trail * 53731;
                float x = MathF.Sin((phase + 55555) * 0.048f) * MathF.Cos(phase * 0.0613f);
                float y = MathF.Sin((phase + 55555) * 0.048f) * MathF.Sin(phase * 0.0613f);
                float z = MathF.Cos((phase + 55555) * 0.048f);
                float sin = MathF.Sin((phase + 11111) * 0.1113f);
                float cos = MathF.Cos((phase + 11111) * 0.1113f);
                // Store offsets: old tails follow the recipient when they move.
                _tails[trail, _tailHead] = new Vector3((cos * y - sin * z) * 80f,
                    (sin * y + cos * z) * 80f, (_soulBarrier ? 120f : 110f) + x * 120f);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (Owner.Hidden || Owner.IsDead || Status != GameControlStatus.Ready || _auraTexture == null || _tailLength < 2)
                return;
            DrawWorldGeometry(_auraTexture, _vertices, BuildAuraVertices());
        }

        internal static void DrawWorldGeometry(Texture2D drawTexture, VertexPositionColorTexture[] vertices, int count, BlendState? blendOverride = null, SamplerState? samplerOverride = null)
        {
            if (count == 0)
                return;
            var gd = GraphicsManager.Instance.GraphicsDevice;
            var effect = GraphicsManager.Instance.AlphaTestEffect3D;
            var blend = gd.BlendState;
            var depth = gd.DepthStencilState;
            var raster = gd.RasterizerState;
            var sampler = gd.SamplerStates[0];
            var world = effect.World;
            var view = effect.View;
            var projection = effect.Projection;
            var texture = effect.Texture;
            var diffuse = effect.DiffuseColor;
            float alpha = effect.Alpha;
            bool vertexColor = effect.VertexColorEnabled;
            int referenceAlpha = effect.ReferenceAlpha;
            try
            {
                gd.BlendState = blendOverride ?? Blendings.OneOneAdditive;
                gd.DepthStencilState = DepthStencilState.DepthRead;
                gd.RasterizerState = RasterizerState.CullNone;
                gd.SamplerStates[0] = samplerOverride ?? SamplerState.LinearWrap;
                effect.World = Matrix.Identity;
                effect.View = Camera.Instance.View;
                effect.Projection = Camera.Instance.Projection;
                effect.Texture = drawTexture;
                effect.DiffuseColor = Vector3.One;
                effect.Alpha = 1f;
                effect.VertexColorEnabled = true;
                effect.ReferenceAlpha = 0;
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    gd.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count / 3);
                }

            }
            finally
            {
                gd.BlendState = blend;
                gd.DepthStencilState = depth;
                gd.RasterizerState = raster;
                gd.SamplerStates[0] = sampler;
                effect.World = world;
                effect.View = view;
                effect.Projection = projection;
                effect.Texture = texture;
                effect.DiffuseColor = diffuse;
                effect.Alpha = alpha;
                effect.VertexColorEnabled = vertexColor;
                effect.ReferenceAlpha = referenceAlpha;
            }
        }

        private int BuildArmVertices()
        {
            if (_soulBarrier || _texture == null || !_buffs.HasBuff(Owner.NetworkId, BuffEffectId.GreaterDamage))
                return 0;
            // RenderSprite in MuMain uses bitmap dimensions * Scale as world-space
            // billboard dimensions. Keep this independent of SpriteObject's screen scaling.
            Matrix cameraWorld = Matrix.Invert(Camera.Instance.View);
            Vector3 right = cameraWorld.Right * (_texture.Width * 1.5f * 0.5f);
            Vector3 up = cameraWorld.Up * (_texture.Height * 1.5f * 0.5f);
            int count = 0;
            for (int i = 0; i < UpperBones.Length; i++)
            {
                if (!Owner.TryGetBoneWorldMatrix(UpperBones[i], out var bone))
                    continue;
                Vector3 center = bone.Translation;
                var color = new Color(1f, 0.3f, 0.2f) * (i < 3 ? _leftBrightness : _rightBrightness);
                var a = new VertexPositionColorTexture(center - right + up, color, new Vector2(0, 0));
                var b = new VertexPositionColorTexture(center + right + up, color, new Vector2(1, 0));
                var c = new VertexPositionColorTexture(center + right - up, color, new Vector2(1, 1));
                var d = new VertexPositionColorTexture(center - right - up, color, new Vector2(0, 1));
                _armVertices[count++] = a; _armVertices[count++] = b; _armVertices[count++] = c;
                _armVertices[count++] = a; _armVertices[count++] = c; _armVertices[count++] = d;
            }
            return count;
        }

        private Color AuraColor(float height) => _soulBarrier
            ? new Color(MathF.Max(0f, 1f - height), MathF.Max(0f, 1f - height), MathF.Max(0f, 1f - height))
            : new Color(MathF.Max(0f, 0.4f - height), MathF.Max(0f, 0.8f - height), MathF.Max(0f, 0.2f - height));

        private int BuildAuraVertices()
        {
            int count = 0;
            Vector3 center = Owner.WorldPosition.Translation;
            // WSclient creates Soul Barrier's subtype-0 joints at scale 50;
            // elf aura joints use scale 20. Each face spans half that width.
            float halfWidth = _soulBarrier ? 25f : 10f;
            for (int trail = 0; trail < TrailCount; trail++)
            {
                for (int tail = 0; tail < _tailLength - 1; tail++)
                {
                    Vector3 a = center + _tails[trail, (_tailHead - tail + TailCount) % TailCount];
                    Vector3 b = center + _tails[trail, (_tailHead - tail - 1 + TailCount) % TailCount];
                    float height = MathF.Max(0f, (a.Z - center.Z - 50f) * 0.01f);
                    var color = AuraColor(height);
                    float u = (_tailLength - tail) / 16f;
                    AddFace(ref count, a, b, Vector3.UnitX * halfWidth, u, color);
                    AddFace(ref count, a, b, Vector3.UnitZ * halfWidth, u, color);
                }
            }
            return count;
        }

        private void AddFace(ref int count, Vector3 a, Vector3 b, Vector3 halfWidth, float u, Color color)
        {
            var v0 = new VertexPositionColorTexture(a - halfWidth, color, new Vector2(u, 0));
            var v1 = new VertexPositionColorTexture(a + halfWidth, color, new Vector2(u, 1));
            var v2 = new VertexPositionColorTexture(b + halfWidth, color, new Vector2(u - 1f / 16f, 1));
            var v3 = new VertexPositionColorTexture(b - halfWidth, color, new Vector2(u - 1f / 16f, 0));
            _vertices[count++] = v0; _vertices[count++] = v1; _vertices[count++] = v2;
            _vertices[count++] = v0; _vertices[count++] = v2; _vertices[count++] = v3;
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (!Visible || _tailLength == 0 || _texture == null || Owner.Hidden || Owner.IsDead)
                return;
            // MuMain submits SHINY+1 sprites after character geometry; drawing these
            // with the aura's earlier world pass lets later character meshes cover them.
            DrawWorldGeometry(_texture, _armVertices, BuildArmVertices());
            if (_auraTexture == null || _tailLength < 2) return;
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            Vector3 right = camera.Right * (_auraTexture.Width * .7f * .5f);
            Vector3 up = camera.Up * (_auraTexture.Height * .7f * .5f);
            int count = 0;
            for (int trail = 0; trail < TrailCount; trail++)
            {
                Vector3 offset = _tails[trail, (_tailHead - _tailLength / 2 + TailCount) % TailCount];
                Vector3 center = Owner.WorldPosition.Translation + offset;
                Color tint = AuraColor(MathF.Max(0f, (offset.Z - 50f) * .01f));
                var a = new VertexPositionColorTexture(center - right + up, tint, new Vector2(0, 0));
                var b = new VertexPositionColorTexture(center + right + up, tint, new Vector2(1, 0));
                var c = new VertexPositionColorTexture(center + right - up, tint, new Vector2(1, 1));
                var d = new VertexPositionColorTexture(center - right - up, tint, new Vector2(0, 1));
                _auraSparkVertices[count++] = a; _auraSparkVertices[count++] = b; _auraSparkVertices[count++] = c;
                _auraSparkVertices[count++] = a; _auraSparkVertices[count++] = c; _auraSparkVertices[count++] = d;
            }
            DrawWorldGeometry(_auraTexture, _auraSparkVertices, count);
        }
    }
}
