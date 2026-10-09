#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>Preloaded, bounded parts with source-client timing (25 ticks per second).</summary>
    public abstract class StagedCombatEffect : EffectObject
    {
        protected readonly WalkerObject Caster;
        protected readonly Vector3 Origin;
        protected readonly Vector3 Forward;
        private readonly WorldObject?[] _parts = new WorldObject?[64];
        private readonly float[] _delays = new float[64];
        private readonly bool[] _started = new bool[64];
        private int _count;
        private float _elapsed;
        private readonly float _duration;

        protected StagedCombatEffect(WalkerObject caster, float durationFrames)
        {
            Caster = caster;
            Origin = caster.WorldPosition.Translation;
            Forward = new Vector3(MathF.Sin(caster.Angle.Z), -MathF.Cos(caster.Angle.Z), 0f);
            Position = Origin;
            _duration = durationFrames / 25f;
            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;
            LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-2500f), new Vector3(2500f));
        }

        protected abstract void BuildParts();

        protected void AddPart(WorldObject part, float delayFrames)
        {
            if (_count == _parts.Length)
            {
                part.Dispose();
                return;
            }
            _parts[_count] = part;
            _delays[_count++] = delayFrames / 25f;
        }

        protected void AddModel(string path, Vector3 position, float delay, float life,
            float scale, Vector3 light, Vector3 velocity = default, bool rush = false, float yawOffset = 0f)
        {
            AddPart(new CombatModelPiece(path, life, velocity, rush)
            {
                Position = position, Angle = new Vector3(0f, 0f, Caster.Angle.Z + yawOffset),
                Scale = scale, Light = light
            }, delay);
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            BuildParts();
            for (int i = 0; i < _count; i++)
            {
                if (IsDisposeRequested || World == null)
                    return;
                var part = _parts[i]!;
                part.World = World;
                await part.Load();
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_elapsed >= _duration || Caster.Status == GameControlStatus.Disposed || Caster is PlayerObject { IsDead: true })
            {
                World?.RemoveObject(this);
                Dispose();
                return;
            }
            for (int i = 0; i < _count; i++)
                if (!_started[i] && _elapsed >= _delays[i] && _parts[i]?.Status == GameControlStatus.Ready)
                {
                    _started[i] = true;
                    World?.Objects.Add(_parts[i]!);
                }
        }

        public override void Dispose()
        {
            for (int i = 0; i < _count; i++)
            {
                var part = _parts[i];
                part?.World?.RemoveObject(part);
                part?.Dispose();
            }
            base.Dispose();
        }

        private sealed class CombatModelPiece : ModelObject
        {
            private readonly string _path;
            private readonly float _duration;
            private readonly Vector3 _velocity;
            private readonly bool _rush;
            private float _age;

            public CombatModelPiece(string path, float lifeFrames, Vector3 velocity, bool rush)
            {
                _path = path;
                _duration = lifeFrames / 25f;
                _velocity = velocity;
                _rush = rush;
                RenderShadow = false;
                ContinuousAnimation = true;
                AnimationSpeed = 6.25f;
                LightEnabled = true;
                IsTransparent = true;
                AffectedByTransparency = true;
                BlendState = BlendState.Additive;
                DepthState = DepthStencilState.DepthRead;
                BlendMeshState = BlendState.Additive;
                BlendMesh = 0;
            }

            public override async Task Load()
            {
                Model = await BMDLoader.Instance.Prepare(_path);
                await base.Load();
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (Status != GameControlStatus.Ready)
                    return;
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _age += dt;
                float frames = _age * 25f;
                Position += _velocity * dt * (_rush ? 10f + frames * 2f : 1f);
                if (_rush)
                    Scale = frames < 3f ? frames * 0.9f : MathF.Max(0f, 2.7f - (frames - 3f) * 0.05f);
                Alpha = MathHelper.Clamp((_duration - _age) / 0.2f, 0f, 1f);
                BlendMeshLight = _rush ? MathF.Max(0f, (15f - frames) / 18f) : Alpha;
                if (_age >= _duration)
                {
                    World?.RemoveObject(this);
                    Dispose();
                }
            }
        }
    }
}
