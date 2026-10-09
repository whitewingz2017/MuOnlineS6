#nullable enable
using Client.Main.Core.Utilities;
using Client.Main.Objects.Effects.Joints;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    public sealed class PlasmaStormEffect : StagedCombatEffect
    {
        private readonly WalkerObject? _target;
        private readonly Vector3? _targetPosition;
        private readonly WalkerObject?[] _targets = new WalkerObject?[10];
        private readonly int _targetCount;
        private readonly Vector3 _tint;
        private readonly Vector3 _flashTint;
        public PlasmaStormEffect(WalkerObject caster, WalkerObject? target, Vector3? position) : base(caster, 25f)
        {
            _target = target;
            _targetPosition = position;
            short vehicle = caster is PlayerObject player ? player.Vehicle.ItemIndex : (short)-1;
            (_tint, _flashTint) = vehicle switch
            {
                11 or 15 => (new Vector3(1f, 0.6f, 0.6f), new Vector3(1f, 0.3f, 0.2f)),
                12 or 16 => (new Vector3(0.7f, 0.7f, 1f), new Vector3(0.2f, 0.3f, 1f)),
                13 or 17 => (new Vector3(0.9f, 0.9f, 0.3f), new Vector3(0.8f, 0.8f, 0.1f)),
                _ => (new Vector3(0.7f, 1f, 0.7f), new Vector3(0.1f, 0.8f, 0.1f))
            };
            int targets = 0;
            if (_target != null)
            {
                _targets[targets++] = _target;
            }
            float range = SkillDatabase.GetSkillRange(76) * Constants.TERRAIN_SCALE;
            if (range <= 0f)
                range = 600f;
            // Capture recipients on the main thread, before asynchronous content loading.
            if (caster.World != null)
                foreach (var walker in caster.World.WalkerObjectsById.Values)
                {
                    if (targets >= 10)
                        break;
                    if (walker is not MonsterObject { IsDead: false } || walker == _target)
                        continue;
                    Vector3 delta = walker.WorldPosition.Translation - Origin;
                    if (System.MathF.Abs(delta.X) > range || System.MathF.Abs(delta.Y) > range)
                        continue;
                    _targets[targets++] = walker;
                }
            _targetCount = targets;
        }

        protected override void BuildParts()
        {
            Vector3 head = Origin + Forward * 140f + Vector3.UnitZ * 130f;
            for (int i = 0; i < _targetCount; i++)
                AddTarget(head, _targets[i]!);
            if (_targetCount == 0)
            {
                Vector3 end = _targetPosition ?? Origin + Forward * 400f;
                if (Vector2.Distance(new Vector2(end.X, end.Y), new Vector2(Origin.X, Origin.Y)) < 50f)
                    end = Origin + Forward * 400f;
                AddArcs(head, () => end);
            }
            for (int i = 0; i < 6; i++)
                AddPart(new LightEffect
                {
                    Position = head + new Vector3(MuGame.Random.Next(-50, 51), MuGame.Random.Next(-50, 51), MuGame.Random.Next(-30, 31)),
                    Scale = 1.5f, Light = _tint
                }, 0f);
        }

        private void AddTarget(Vector3 head, WalkerObject target) =>
            AddArcs(head, () => target.WorldPosition.Translation);

        private void AddArcs(Vector3 head, System.Func<Vector3> target)
        {
            for (int i = 0; i < 4; i++)
            {
                bool flash = i % 2 != 0;
                var arc = SourceJointEffect.FenrirThunder(head, target, flash ? 80f : 100f,
                    flash ? _flashTint : _tint, flash);
                AddPart(arc, 0f);
            }
        }
    }
}
