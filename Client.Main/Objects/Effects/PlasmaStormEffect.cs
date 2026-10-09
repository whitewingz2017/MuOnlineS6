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
        private readonly WalkerObject?[] _targets = new WalkerObject?[11];
        private readonly int _targetCount;
        private readonly int _fenrirType;
        public PlasmaStormEffect(WalkerObject caster, WalkerObject? target, Vector3? position) : base(caster, 25f)
        {
            _target = target;
            short vehicle = caster is PlayerObject player ? player.Vehicle.ItemIndex : (short)-1;
            _fenrirType = vehicle is 11 or 15 ? 1 : vehicle is 12 or 16 ? 2 : vehicle is 13 or 17 ? 3 : 0;
            int targets = 0;
            if (_target != null)
            {
                _targets[targets++] = _target;
            }
            float range = SkillDatabase.GetSkillRange(76) * Constants.TERRAIN_SCALE;
            if (range <= 0f)
                range = 600f;
            // Capture recipients on the main thread, before asynchronous content loading.
            int nearbyCount = 0;
            if (caster.World != null)
                foreach (var walker in caster.World.WalkerObjectsById.Values)
                {
                    if (nearbyCount >= 10)
                        break;
                    if (walker is not MonsterObject { IsDead: false })
                        continue;
                    Vector3 delta = walker.WorldPosition.Translation - Origin;
                    if (delta.X * delta.X + delta.Y * delta.Y > range * range)
                        continue;
                    _targets[targets++] = walker;
                    nearbyCount++;
                }
            _targetCount = targets;
        }

        protected override void BuildParts()
        {
            Vector3 head = Origin + Forward * 140f + Vector3.UnitZ * 130f;
            for (int i = 0; i < _targetCount; i++)
                AddTarget(head, _targets[i]!, i == 0 && _target != null);
            if (_targetCount == 0)
                return;
            for (int i = 0; i < 6; i++)
                // MuMain implements FLARE_FORCE 11-13; gold's subtype 14 has no
                // corresponding movement branch, so do not invent a gold flare.
                if (_fenrirType < 3)
                    AddPart(SourceJointEffect.FenrirFlare(Origin - Forward * (10f + MuGame.Random.Next(-20, 20)) +
                        Vector3.UnitZ * 130f, _fenrirType), 0f);
        }

        private void AddTarget(Vector3 head, WalkerObject target, bool explicitTarget) =>
            AddArcs(head, () => target.WorldPosition.Translation, explicitTarget);

        private void AddArcs(Vector3 head, System.Func<Vector3> target, bool explicitTarget = false)
        {
            for (int i = 0; i < 4; i++)
            {
                int subtype = i % 2 == 0 ? _fenrirType : (explicitTarget ? 3 : 4) + _fenrirType;
                bool flash = subtype >= 4;
                Vector3 tint = subtype switch
                {
                    0 => new Vector3(0.7f, 1f, 0.7f), 1 => new Vector3(1f, 0.6f, 0.6f),
                    2 => new Vector3(0.7f, 0.7f, 1f), 3 => new Vector3(0.9f, 0.9f, 0.3f),
                    4 => new Vector3(0.1f, 0.8f, 0.1f), 5 => new Vector3(1f, 0.3f, 0.2f),
                    6 => new Vector3(0.2f, 0.3f, 1f), _ => new Vector3(0.8f, 0.8f, 0.1f)
                };
                var arc = SourceJointEffect.FenrirThunder(head, target, i % 2 == 0 ? 100f : 80f, tint, flash);
                AddPart(arc, 0f);
            }
        }
    }
}
