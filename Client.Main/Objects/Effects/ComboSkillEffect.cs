#nullable enable
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
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

            Position = _caster.WorldPosition.Translation;
            Angle = Vector3.Zero;

            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            float frameDelta = MathF.Max(0f,
                (float)gameTime.ElapsedGameTime.TotalSeconds * SourceFrameRate);
            _lifeFrames -= frameDelta;

            if (_comboModel != null)
            {
                // SourceMain: if (LifeTime > 4) { Scale += Gravity; Gravity += 0.1f; }
                if (_lifeFrames > 4f)
                {
                    _comboModel.Scale += _gravity * frameDelta;
                    _gravity += 0.1f * frameDelta;
                }

                // Smooth equivalent of BlendMeshLight /= 1.4 each frame
                float lifeRatio = MathHelper.Clamp(_lifeFrames / TotalLifeFrames, 0f, 1f);
                _comboModel.BlendMeshLight = lifeRatio;
                _comboModel.Alpha = lifeRatio;
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
            var rng = Random.Shared;
            // Local offset matches the model height raise
            Vector3 localOrigin = new Vector3(0f, 0f, 50f);

            for (int i = 0; i < 60; i++)
            {
                float jointScale = rng.Next(70, 110); // rand()%40 + 70

                var light = new ComboLightJoint
                {
                    Position = localOrigin,
                    Scale = jointScale * 0.01f,
                    LifeTime = 0.55f + (float)rng.NextDouble() * 0.45f,
                    Color = new Color(180, 220, 255, 200)
                };
                Children.Add(light);
                // Load is sync / no-op for this simple flash; fire-and-forget is fine
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

        /// <summary>
        /// Minimal additive light-flash that approximates SourceMain BITMAP_LIGHT joints.
        /// Lives as a child of ComboSkillEffect.
        /// </summary>
        private sealed class ComboLightJoint : EffectObject
        {
            public float LifeTime { get; set; } = 0.8f;
            public Color Color { get; set; } = Color.White;

            private float _age;

            public ComboLightJoint()
            {
                IsTransparent = true;
                AffectedByTransparency = false;
                BlendState = Blendings.OneOneAdditive;
                DepthState = DepthStencilState.DepthRead;
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                _age += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_age >= LifeTime)
                {
                    // Parent will clean us up when the whole effect is disposed;
                    // just mark ourselves dead so we stop updating.
                    Status = GameControlStatus.Disposed;
                    return;
                }

                float t = 1f - (_age / LifeTime);
                Alpha = t * t;
                Scale *= 1f + 0.8f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }
    }
}