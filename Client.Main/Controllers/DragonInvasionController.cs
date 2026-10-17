using Client.Main.Controls;
using Client.Main.Objects.Worlds;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;

namespace Client.Main.Controllers
{
    /// <summary>Ambient dragons are local visuals, not server-spawned monsters.</summary>
    internal sealed class DragonInvasionController
    {
        private const int MaximumDragons = 5;
        private readonly InvasionDragonObject[] _dragons = new InvasionDragonObject[MaximumDragons];
        private int _activeEvent;
        private readonly InvasionFireEffect[] _fireballs = new InvasionFireEffect[6];
        private float _tickClock;

        public void Update(WalkableWorldControl world, GameTime time)
        {
            int activeEvent = MuGame.Network?.GetCharacterState()?.DragonInvasion.ActiveEvent ?? 0;
            if (_activeEvent != activeEvent)
            {
                Clear(world);
                _activeEvent = activeEvent;
                _tickClock = 0f;
            }

            if (activeEvent == 0 || world.Walker == null)
                return;

            for (int i = 0; i < _dragons.Length; i++)
            {
                if (_dragons[i] is { Expired: true } old)
                {
                    world.RemoveObject(old);
                    old.Dispose();
                    _dragons[i] = null;
                }

            }
            for (int i = 0; i < _fireballs.Length; i++)
            {
                if (_fireballs[i] is not { Expired: true } fire) continue;
                world.RemoveObject(fire);
                fire.Dispose();
                _fireballs[i] = null;
            }

            // MuMain rand_fps_check runs at a 25 Hz baseline, independently of
            // rendering FPS: each free boid slot rolls 1/300; fire rolls 1/40.
            _tickClock += MathF.Min((float)time.ElapsedGameTime.TotalSeconds, 0.2f);
            while (_tickClock >= 0.04f)
            {
                _tickClock -= 0.04f;
                if (MuGame.Random.Next(40) == 0)
                    SpawnFireball(world);
                for (int i = 0; i < _dragons.Length; i++)
                {
                    if (_dragons[i] != null || MuGame.Random.Next(300) != 0) continue;

                    // GOBoid spawn range and scale; render yaw includes its +90 correction.
                    var position = world.Walker.Position + new Vector3(
                        MuGame.Random.Next(600) - 100, MuGame.Random.Next(400) + 200, 300f);
                    var dragon = new InvasionDragonObject(activeEvent == 3)
                    {
                        Position = position,
                        Scale = (MuGame.Random.Next(3) + 6) * 0.1f
                    };
                    _dragons[i] = dragon;
                    world.Objects.Add(dragon);
                    _ = dragon.Load();
                }
            }
        }

        private void SpawnFireball(WalkableWorldControl world)
        {
            for (int i = 0; i < _fireballs.Length; i++)
            {
                if (_fireballs[i] != null) continue;
                var fire = new InvasionFireEffect
                {
                    Position = world.Walker.Position + new Vector3(
                        MuGame.Random.Next(600) - 200, MuGame.Random.Next(400) + 200, 300f)
                };
                _fireballs[i] = fire;
                world.Objects.Add(fire);
                _ = fire.Load();
                SoundController.Instance.PlayBufferWithAttenuation("Sound/eMeteorite.wav", fire.Position, world.Walker.Position);
                break;
            }
        }

        private void Clear(WalkableWorldControl world)
        {
            for (int i = 0; i < _fireballs.Length; i++)
            {
                if (_fireballs[i] is not { } fire) continue;
                world.RemoveObject(fire);
                fire.Dispose();
                _fireballs[i] = null;
            }
            for (int i = 0; i < _dragons.Length; i++)
            {
                if (_dragons[i] is not { } dragon)
                    continue;
                world.RemoveObject(dragon);
                dragon.Dispose();
                _dragons[i] = null;
            }
        }
    }
}
