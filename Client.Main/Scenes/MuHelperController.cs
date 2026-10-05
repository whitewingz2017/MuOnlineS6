using System;
using System.Linq;
using System.Threading.Tasks;
using Client.Data.ATT;
using Client.Main.Configuration;
using Client.Main.Controls;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Core.Utilities;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Client.Main.Networking.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Scenes
{
    /// <summary>
    /// Client-local MU Helper loop. It deliberately reuses the current character,
    /// skill, movement, inventory, and pickup request paths; it adds no protocol.
    /// </summary>
    internal sealed class MuHelperController
    {
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(180);
        private static readonly TimeSpan PotionCooldown = TimeSpan.FromMilliseconds(900);
        private static readonly TimeSpan PickupCooldown = TimeSpan.FromMilliseconds(850);
        private static readonly TimeSpan FallbackBuffInterval = TimeSpan.FromMinutes(3);

        private readonly GameScene _scene;
        private readonly GameSceneSkillController _skillController;
        private readonly ILogger _logger;
        private DateTime _nextActionAt;
        private DateTime _nextPotionAt;
        private DateTime _nextPickupAt;
        private DateTime _nextRegroupAt;
        private DateTime _awaySince;
        private Vector2 _originalPosition;
        private MonsterObject _currentTarget;
        private volatile bool _pickupInFlight;
        private bool _active;
        private readonly DateTime[] _nextActivationCastAt = new DateTime[2];
        private readonly DateTime[] _nextBuffCastAt = new DateTime[3];

        public MuHelperConfig Config { get; private set; } = new();
        public bool IsActive => _active;
        public event Action StateChanged;

        public MuHelperController(GameScene scene, GameSceneSkillController skillController, ILogger logger)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _skillController = skillController ?? throw new ArgumentNullException(nameof(skillController));
            _logger = logger;
            Load(MuGame.AppSettings?.MuHelper);
        }

        public void Load(MuHelperConfig config = null)
        {
            Config = (config ?? new MuHelperConfig()).Clone();
            Config.Normalize();
            StateChanged?.Invoke();
        }

        public void Save()
        {
            Config.Normalize();
            MuGame.PersistMuHelperConfig(Config);
            StateChanged?.Invoke();
        }

        public void Reset()
        {
            Stop();
            Config = new MuHelperConfig();
            Config.Normalize();
            StateChanged?.Invoke();
        }

        public void Start()
        {
            if (_active)
                return;

            Config.Normalize();
            if (!CanOperate(out string reason))
            {
                _logger?.LogInformation("MU Helper cannot start: {Reason}", reason);
                StateChanged?.Invoke();
                return;
            }

            var hero = _scene.Hero;
            _originalPosition = hero.Location;
            _currentTarget = null;
            _awaySince = DateTime.UtcNow;
            _nextActionAt = DateTime.MinValue;
            _nextPotionAt = DateTime.MinValue;
            _nextPickupAt = DateTime.MinValue;
            _nextRegroupAt = DateTime.MinValue;
            Array.Clear(_nextActivationCastAt);
            Array.Clear(_nextBuffCastAt);

            // Helper owns automated combat; clear both legacy/manual persistent targets.
            _scene.DisableAutoAttack();
            _skillController.CancelPersistentTarget();
            _active = true;
            _logger?.LogInformation("MU Helper started at tile ({X}, {Y})", (int)_originalPosition.X, (int)_originalPosition.Y);
            StateChanged?.Invoke();
        }

        public void Stop()
        {
            if (!_active)
                return;

            _active = false;
            _currentTarget = null;
            _skillController.CancelPersistentTarget();
            _logger?.LogInformation("MU Helper stopped");
            StateChanged?.Invoke();
        }

        public void Toggle()
        {
            if (_active)
                Stop();
            else
                Start();
        }

        public void Update(GameTime gameTime)
        {
            if (!_active)
                return;

            if (!CanOperate(out string reason))
            {
                _logger?.LogInformation("MU Helper stopping: {Reason}", reason);
                Stop();
                return;
            }

            var now = DateTime.UtcNow;
            if (now < _nextActionAt)
                return;
            _nextActionAt = now + UpdateInterval;

            var hero = _scene.Hero;
            var characterState = MuGame.Network?.GetCharacterState();
            if (hero == null || characterState == null)
                return;

            if (ChebyshevDistance(hero.Location, _originalPosition) > 1)
            {
                if (_awaySince == DateTime.MinValue)
                    _awaySince = now;
            }
            else
            {
                _awaySince = now;
            }

            if (TryUsePotion(characterState, now))
                return;
            if (TryUseHealSkill(characterState, now))
                return;
            if (TryCastBuff(characterState, now))
                return;
            if (TryPickupNearbyItem(characterState, now))
                return;
            if (TryRegroup(hero, now))
                return;

            MonsterObject target = SelectTarget(hero, characterState);
            _currentTarget = target;
            if (target == null || hero.IsAttackOrSkillAnimationPlaying())
                return;

            if (TryCastActivationSkill(characterState, target, now))
                return;

            if (Config.BasicSkillId != 0 && TryCastSkill(characterState, Config.BasicSkillId, target))
                return;

            if (Config.FallbackBasicAttack)
                hero.Attack(target);
        }

        private bool CanOperate(out string reason)
        {
            reason = null;
            var hero = _scene.Hero;
            if (hero == null || hero.IsDead)
            {
                reason = "character is unavailable or dead";
                return false;
            }

            if (_scene.World is not WalkableWorldControl world)
            {
                reason = "walkable world is unavailable";
                return false;
            }

            var flags = world.Terrain.RequestTerrainFlag((int)hero.Location.X, (int)hero.Location.Y);
            if (flags.HasFlag(TWFlags.SafeZone))
            {
                reason = "entered a SafeZone";
                return false;
            }

            ushort mapId = MuGame.Network?.GetCharacterState()?.MapId ?? ushort.MaxValue;
            if (Config.BlockedMapIds.Contains(mapId))
            {
                reason = $"map {mapId} is configured as blocked";
                return false;
            }

            return true;
        }

        private bool TryUsePotion(CharacterState state, DateTime now)
        {
            if (!Config.UseHealPotion || now < _nextPotionAt || state.MaximumHealth == 0)
                return false;

            int hpPercent = (int)(state.CurrentHealth * 100L / state.MaximumHealth);
            if (hpPercent > Config.PotionThreshold)
                return false;

            // The existing HUD uses inventory slots 12+ for consumables. MU Helper
            // healing potions are item group 14, IDs 0-2 in the embedded Season 6 data.
            // Use the healing potion assigned to Q/W/E. This keeps Helper automation
            // aligned with the player's existing quick-slot preferences and inventory lookup.
            if (_scene.ModernHud?.TryConsumeHealPotionForHelper(Config.PotionHotbarSlot) != true)
                return false;

            _nextPotionAt = now + PotionCooldown;
            return true;
        }

        private bool TryUseHealSkill(CharacterState state, DateTime now)
        {
            if ((!Config.AutoHeal && !Config.UseDrainLife) || _scene.Hero?.IsAttackOrSkillAnimationPlaying() == true)
                return false;

            if (Config.AutoHeal && state.MaximumHealth > 0)
            {
                int hpPercent = (int)(state.CurrentHealth * 100L / state.MaximumHealth);
                if (hpPercent <= Config.HealThreshold)
                {
                    var healingSkill = state.GetSkills().FirstOrDefault(skill =>
                    {
                        string name = SkillDatabase.GetSkillName(skill.SkillId);
                        return name.Contains("heal", StringComparison.OrdinalIgnoreCase);
                    });
                    if (healingSkill != null && _skillController.CastSkillFromHotbar(healingSkill, null))
                        return true;
                }
            }

            if (Config.UseDrainLife)
            {
                var target = SelectTarget(_scene.Hero, state);
                var drainSkill = state.GetSkills().FirstOrDefault(skill =>
                {
                    string name = SkillDatabase.GetSkillName(skill.SkillId);
                    return name.Contains("drain life", StringComparison.OrdinalIgnoreCase);
                });
                if (target != null && drainSkill != null && TryCastSkill(state, drainSkill.SkillId, target))
                    return true;
            }

            return false;
        }

        private bool TryCastBuff(CharacterState state, DateTime now)
        {
            for (int index = 0; index < Config.BuffSkillIds.Length; index++)
            {
                ushort skillId = Config.BuffSkillIds[index];
                if (skillId == 0 || now < _nextBuffCastAt[index])
                    continue;

                var skill = state.GetSkills().FirstOrDefault(candidate => candidate.SkillId == skillId);
                if (skill == null)
                    continue;

                bool cast = _skillController.CastSkillFromHotbar(skill, null);
                if (!cast)
                    continue;

                TimeSpan interval = Config.BuffDuration
                    ? TimeSpan.FromSeconds(Config.BuffCastIntervalSeconds > 0
                        ? Config.BuffCastIntervalSeconds
                        : FallbackBuffInterval.TotalSeconds)
                    : TimeSpan.FromDays(1);
                _nextBuffCastAt[index] = now + interval;
                return true;
            }

            return false;
        }

        private bool TryCastActivationSkill(CharacterState state, MonsterObject target, DateTime now)
        {
            int nearbyCount = _scene.World.VisibleObjects
                .OfType<MonsterObject>()
                .Count(monster => IsValidMonster(monster) && ChebyshevDistance(monster.Location, _scene.Hero.Location) <= Config.HuntingRange);

            var slots = new[] { Config.ActivationSkill1, Config.ActivationSkill2 };
            for (int index = 0; index < slots.Length; index++)
            {
                var activation = slots[index];
                if (activation.SkillId == 0)
                    continue;

                if (now < _nextActivationCastAt[index])
                    continue;

                bool timerDue = activation.UseTimer;
                bool conditionMet = activation.UseCondition && nearbyCount >= activation.MinimumNearbyMonsters;
                if (!timerDue && !conditionMet)
                    continue;
                if (activation.MobsAttacking && !target.LastAttackTargetId.Equals(_scene.Hero.NetworkId))
                    continue;

                if (!TryCastSkill(state, activation.SkillId, target))
                    continue;

                TimeSpan delay = TimeSpan.FromSeconds(Math.Max(1, activation.DelaySeconds));
                _nextActivationCastAt[index] = now + delay;
                return true;
            }

            return false;
        }

        private bool TryCastSkill(CharacterState state, ushort skillId, MonsterObject target)
        {
            var skill = state.GetSkills().FirstOrDefault(candidate => candidate.SkillId == skillId);
            if (skill == null)
                return false;

            if (target != null && !SkillDatabase.IsSelfSkill(skillId))
            {
                uint range = SkillDatabase.GetSkillRange(skillId);
                if (range > 0 && ChebyshevDistance(target.Location, _scene.Hero.Location) > range + 1)
                    return false;
            }

            return _skillController.CastSkillFromHotbar(skill, target);
        }

        private MonsterObject SelectTarget(PlayerObject hero, CharacterState state)
        {
            if (hero == null || _scene.World == null)
                return null;

            bool IsInHuntRange(MonsterObject monster)
            {
                int distance = ChebyshevDistance(monster.Location, hero.Location);
                bool counterAttackingHero = Config.LongRangeCounterAttack &&
                    monster.LastAttackTargetId == hero.NetworkId;
                return distance <= Config.HuntingRange || counterAttackingHero;
            }

            if (_currentTarget != null && IsValidMonster(_currentTarget) && IsInHuntRange(_currentTarget))
                return _currentTarget;

            return _scene.World.VisibleObjects
                .OfType<MonsterObject>()
                .Where(monster => IsValidMonster(monster) && IsInHuntRange(monster))
                .OrderBy(monster => ChebyshevDistance(monster.Location, hero.Location))
                .ThenBy(monster => monster.NetworkId)
                .FirstOrDefault();
        }

        private bool TryPickupNearbyItem(CharacterState state, DateTime now)
        {
            if (now < _nextPickupAt || _pickupInFlight || Config.ObtainingRange <= 0)
                return false;

            var network = MuGame.Network;
            var scopeManager = network?.GetScopeManager();
            var service = network?.GetCharacterService();
            var hero = _scene.Hero;
            if (scopeManager == null || service == null || hero == null)
                return false;

            var candidate = scopeManager.GetScopeItems(ScopeObjectType.Item)
                .Concat(Config.PickZen ? scopeManager.GetScopeItems(ScopeObjectType.Money) : Enumerable.Empty<ScopeObject>())
                .Where(scopeObject => scopeObject.PositionX != 0 || scopeObject.PositionY != 0)
                .Where(scopeObject => ChebyshevDistance(
                    new Vector2(scopeObject.PositionX, scopeObject.PositionY), hero.Location) <= Config.ObtainingRange)
                .Where(ShouldObtain)
                .OrderBy(scopeObject => ChebyshevDistance(
                    new Vector2(scopeObject.PositionX, scopeObject.PositionY), hero.Location))
                .FirstOrDefault();

            if (candidate == null)
                return false;

            var currentEntry = scopeManager.GetScopeObjectByMaskedId((ushort)(candidate.RawId & 0x7FFF));
            if (currentEntry == null)
                return false;

            state.SetPendingPickupRawId(candidate.RawId);
            if (currentEntry is ItemScopeObject item)
                state.StashPickedItem(item.ItemData.ToArray());
            else if (currentEntry is not MoneyScopeObject)
                return false;

            _pickupInFlight = true;
            _nextPickupAt = now + PickupCooldown;
            _ = SendPickupAsync(service, network.TargetVersion, state, candidate.RawId);
            return true;
        }

        private bool ShouldObtain(ScopeObject scopeObject)
        {
            if (scopeObject is MoneyScopeObject)
                return Config.PickZen;
            if (scopeObject is not ItemScopeObject item)
                return false;

            bool selectedName = Config.ExtraItems.Any(name =>
                !string.IsNullOrWhiteSpace(name) && item.ItemDescription.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (Config.PickAllItems)
                return true;
            if (selectedName && (Config.PickSelectedItems || Config.PickExtraItems))
                return true;
            if (!ItemDatabase.TryGetItemGroupAndNumber(item.ItemData.Span, out byte group, out short number))
                return false;

            var details = ItemDatabase.ParseItemDetails(item.ItemData.Span);
            return (Config.PickJewel && ItemDatabase.IsJewelItem(group, number)) ||
                   (Config.PickAncient && details.IsAncient) ||
                   (Config.PickExcellent && details.IsExcellent);
        }

        private async Task SendPickupAsync(CharacterService service, TargetProtocolVersion version, CharacterState state, ushort rawId)
        {
            try
            {
                bool sent = await service.SendPickupItemRequestAsync(rawId, version).ConfigureAwait(false);
                if (!sent)
                    MuGame.ScheduleOnMainThread(state.ClearPendingPickupRawId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "MU Helper pickup request failed for raw ID {RawId:X4}", rawId);
                MuGame.ScheduleOnMainThread(state.ClearPendingPickupRawId);
            }
            finally
            {
                _pickupInFlight = false;
            }
        }

        private bool TryRegroup(PlayerObject hero, DateTime now)
        {
            if (!Config.ReturnToOriginalPosition || Config.MaxSecondsAway <= 0 ||
                now - _awaySince < TimeSpan.FromSeconds(Config.MaxSecondsAway) ||
                now < _nextRegroupAt || ChebyshevDistance(hero.Location, _originalPosition) <= 1)
                return false;

            _nextRegroupAt = now + TimeSpan.FromSeconds(2);
            _currentTarget = null;
            hero.MoveTo(new Vector2((int)_originalPosition.X, (int)_originalPosition.Y));
            return true;
        }

        private bool IsValidMonster(MonsterObject monster)
        {
            return monster != null && !monster.IsDead && monster.World == _scene.World;
        }

        private static int ChebyshevDistance(Vector2 left, Vector2 right)
        {
            return Math.Max(Math.Abs((int)left.X - (int)right.X), Math.Abs((int)left.Y - (int)right.Y));
        }
    }
}
