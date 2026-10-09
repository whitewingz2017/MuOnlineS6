#nullable enable
using System.Threading.Tasks;
using Client.Main.Models;
using Client.Main.Controllers;
using Client.Main.Content;
using Client.Main.Objects.Player;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>Greater Fortitude: 36 radial spirits and two orange ground circles.</summary>
    public sealed class GreaterFortitudeEffect : EffectObject
    {
        private const int SpiritCount = 36;
        private const float Duration = 40f / 25f;
        private const float ImpactDelay = 9f / 25f;
        private readonly WalkerObject _caster;
        private readonly WorldObject?[] _spawned = new WorldObject?[SpiritCount + 2];
        private float _remaining = Duration + ImpactDelay;
        private bool _impactSpawned;

        public GreaterFortitudeEffect(WalkerObject caster)
        {
            _caster = caster;
            Position = caster.WorldPosition.Translation;
            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            DepthState = DepthStencilState.DepthRead;
            LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-2200f), new Vector3(2200f));
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointSpirit01.jpg");
            await TextureLoader.Instance.PrepareAndGetTexture("Effect/Magic_Ground2.jpg");
            await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
        }

        private async Task SpawnImpact()
        {
            if (Status != GameControlStatus.Ready || World == null)
                return;
            var world = World;
            Position = _caster.WorldPosition.Translation;
            SoundController.Instance.PlayBuffer("Sound/eSwellLife.wav");
            Vector3 origin = Position + Vector3.UnitZ * 100f;
            int index = 0;
            for (int i = 0; i < SpiritCount; i++)
            {
                if (Status == GameControlStatus.Disposed || world.Status == GameControlStatus.Disposed)
                    return;
                var spirit = SourceJointEffect.FortitudeSpirit(origin, i * 10f);
                _spawned[index++] = spirit;
                world.Objects.Add(spirit);
                await spirit.Load();
                if (Status == GameControlStatus.Disposed || world.Status == GameControlStatus.Disposed)
                    return;
                if (i % 20 == 0)
                {
                    var circle = new LevelUpMagicCircle(Position, fortitude: true, rotation: MathHelper.ToRadians(i * 10f));
                    _spawned[index++] = circle;
                    world.Objects.Add(circle);
                    await circle.Load();
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;
            _remaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_remaining <= 0f || _caster.Status == GameControlStatus.Disposed || _caster is PlayerObject { IsDead: true })
            {
                World?.RemoveObject(this);
                Dispose();
                return;
            }
            if (!_impactSpawned && _remaining <= Duration)
            {
                _impactSpawned = true;
                _ = SpawnImpact();
            }
        }

        public override void Dispose()
        {
            foreach (var effect in _spawned)
            {
                if (effect == null)
                    continue;
                effect.World?.RemoveObject(effect);
                effect.Dispose();
            }
            base.Dispose();
        }
    }
}
