#nullable enable
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Client.Main.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// SourceMain5.2 MODEL_COMBO (skill 59) effect.
    ///
    /// Loads Data/Skill/combo.bmd, raises it +50, 20-frame lifetime.
    /// Matches SourceMain scale growth (after frame 4) and the 60 BITMAP_LIGHT joints.
    /// </summary>
    public sealed class ComboSkillEffect : EffectObject
    {
        private const float SourceFrameRate = 25f;
        private const float TotalLifeFrames = 20f;
        private const string ComboModelPath = "Skill/combo.bmd";

        private readonly WalkerObject _caster;
        private float _lifeFrames = TotalLifeFrames;
        private float _frameAccumulator;
        private float _gravity = 0.1f;          // SourceMain initial Gravity
        private ComboModelPiece? _comboModel;

        public ComboSkillEffect(WalkerObject caster)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            Position = caster.WorldPosition.Translation;
            Angle = Vector3.Zero;
            IsTransparent = true;
            AffectedByTransparency = false;
            BlendState = Blendings.OneOneAdditive;
            DepthState = DepthStencilState.DepthRead;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-500f, -500f, -80f),
                new Vector3(500f, 500f, 650f));
        }

        public override async Task Load()
        {
            await base.Load();
            if (Status != GameControlStatus.Ready)
                return;

            var model = await BMDLoader.Instance.Prepare(ComboModelPath);
            if (model == null)
            {
                Dispose();
                return;
            }

            _comboModel = new ComboModelPiece(model)
            {
                Position = new Vector3(0f, 0f, 50f),   // SourceMain: Position[2] += 50
                Scale = 1f
            };
            Children.Add(_comboModel);
            await _comboModel.Load();

            // SourceMain SubType==0: 60 BITMAP_LIGHT joints
            SpawnLightJoints();
        }

        public override void Update(GameTime gameTime)
        {
            if (_caster.Status == GameControlStatus.Disposed ||
                _caster.World == null ||
                !ReferenceEquals(_caster.World, World))
            {
                World?.RemoveObject(this);
                Dispose();
                return;
            }

            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            float frameDelta = MathF.Max(0f,
                (float)gameTime.ElapsedGameTime.TotalSeconds * SourceFrameRate);
            _frameAccumulator += frameDelta;
            while (_frameAccumulator >= 1f && _lifeFrames > 0)
            {
                _frameAccumulator -= 1f;
                if (_comboModel != null)
                {
                    if (_lifeFrames > 4f)
                    {
                        _comboModel.Scale += _gravity;
                        _gravity += .1f;
                    }
                    _comboModel.BlendMeshLight /= 1.4f;
                }
                _lifeFrames -= 1f;
            }

            if (_lifeFrames <= 0f)
            {
                World?.RemoveObject(this);
                Dispose();
            }
        }

        /// <summary>
        /// SourceMain: for (j = 0; j < 60; ++j)
        ///     CreateJoint(BITMAP_LIGHT, pos, pos, angle, 0, NULL, rand()%40 + 70);
        /// Added as Children so they live/die with this effect (no World.AddObject needed).
        /// </summary>
        private void SpawnLightJoints()
        {
            for (int i = 0; i < 60; i++)
            {
                var light = new ComboLightJoint(Position + new Vector3(0, 0, 50));
                Children.Add(light);
                _ = light.Load();
            }
        }

        private sealed class ComboModelPiece : ModelObject
        {
            private readonly Client.Data.BMD.BMD _model;

            public ComboModelPiece(Client.Data.BMD.BMD model)
            {
                _model = model;
                RenderShadow = false;
                IsTransparent = true;
                AffectedByTransparency = false;
                BlendState = Blendings.OneOneAdditive;
                BlendMesh = -2;
                BlendMeshState = Blendings.OneOneAdditive;
                BlendMeshLight = 1f;
                DepthState = DepthStencilState.DepthRead;
                ContinuousAnimation = true;
                AnimationSpeed = 1f;
            }

            public override Task Load()
            {
                Model = _model;
                return base.Load();
            }
        }

        // BITMAP_LIGHT subtype 0: cross ribbons, four frames of flight (ten
        // tail samples per frame), then stationary fading through frame twenty.
        private sealed class ComboLightJoint : EffectObject
        {
            private readonly Vector3[] _tails = new Vector3[30];
            private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[29 * 12];
            private readonly Vector3 _step, _right, _up;
            private Vector3 _position;
            private Vector3 _light = new(.1f, .5f, 1f);
            private float _accumulator;
            private int _life = 20, _head = -1, _count;
            private Texture2D? _texture;

            public ComboLightJoint(Vector3 origin)
            {
                _position = origin;
                float width = MuGame.Random.Next(40) + 70;
                float speed = MuGame.Random.Next(10) + 10;
                Vector3 angle = new(30 - MuGame.Random.Next(40), 0, MuGame.Random.Next(360));
                SourceJointMath.AngleBasis(angle, out var right, out var forward, out var up);
                _step = -forward * speed;
                _right = right * width * .5f;
                _up = up * width * .5f;
                IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
                BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
                BoundingBoxLocal = new BoundingBox(new Vector3(-1000), new Vector3(1000));
            }

            public override async Task LoadContent()
            {
                await base.LoadContent();
                _texture = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (Status != GameControlStatus.Ready) return;
                _accumulator += (float)gameTime.ElapsedGameTime.TotalSeconds * 25f;
                while (_accumulator >= 1f && _life > 0)
                {
                    _accumulator -= 1;
                    if (_life > 16)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            _head = (_head + 1) % 30;
                            _tails[_head] = _position;
                            _count = Math.Min(30, _count + 1);
                            _position += _step;
                        }
                    }
                    _light /= 1.2f;
                    _life--;
                }
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || _texture == null || _count < 2 || _life <= 0) return;
                int count = 0;
                Color color = new(_light);
                for (int i = 0; i < _count - 1; i++)
                {
                    Vector3 a = _tails[(_head - i + 30) % 30];
                    Vector3 b = _tails[(_head - i - 1 + 30) % 30];
                    float u = (_count - 1 - i) / 29f;
                    AddFace(ref count, a, b, _right, u, color);
                    AddFace(ref count, a, b, _up, u, color);
                }
                ElfSupportBuffEffect.DrawWorldGeometry(_texture, _vertices, count);
            }

            private void AddFace(ref int count, Vector3 a, Vector3 b, Vector3 width, float u, Color color)
            {
                var v0 = new VertexPositionColorTexture(a - width, color, new Vector2(u, 0));
                var v1 = new VertexPositionColorTexture(a + width, color, new Vector2(u, 1));
                var v2 = new VertexPositionColorTexture(b + width, color, new Vector2(u - 1f / 29f, 1));
                var v3 = new VertexPositionColorTexture(b - width, color, new Vector2(u - 1f / 29f, 0));
                _vertices[count++] = v0; _vertices[count++] = v1; _vertices[count++] = v2;
                _vertices[count++] = v0; _vertices[count++] = v2; _vertices[count++] = v3;
            }
        }
    }
}
