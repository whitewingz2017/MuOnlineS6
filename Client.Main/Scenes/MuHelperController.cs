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
    /// Client-local MU Helper loop. Reuses character, skill, movement, inventory,
    /// and pickup paths. Pipeline mirrors MuMain CMuHelper::Work order:
    /// Pet → Buff → Party → Potion/Heal → Loot → Regroup → Combo/Attack → Repair.
    /// </summary>
    internal sealed class MuHelperController
    {
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(180);
        private static readonly TimeSpan PotionCooldown = TimeSpan.FromMilliseconds(900);
        private static readonly TimeSpan PickupCooldown = TimeSpan.FromMilliseconds(850);
        private static readonly TimeSpan FallbackBuffInterval = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan RepairCooldown = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan PetCooldown = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan PartySupportCooldown = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan ComboStepTimeout = TimeSpan.FromSeconds(2);
        private const int PickupReachTiles = 1;

        private readonly GameScene _scene;
        private readonly GameSceneSkillController _skillController;
        private readonly ILogger _logger;
        private DateTime _nextActionAt;
        private DateTime _nextPotionAt;
        private DateTime _nextPickupAt;
        private DateTime _nextRegroupAt;
        private DateTime _nextRepairAt;
        private DateTime _nextPetAt;
        private DateTime _nextPartyBuffAt;
        private DateTime _awaySince;
        private Vector2 _originalPosition;
        private MonsterObject _currentTarget;
        private volatile bool _pickupInFlight;
        private bool _active;
        private bool _startRequestPending;
        private readonly DateTime[] _nextActivationCastAt = new DateTime[2];
        private readonly DateTime[] _nextBuffCastAt = new DateTime[3];
        private int _comboStep;
        private DateTime _comboStepAt;

        public MuHelperConfig Config { get; private set; } = new();
        public bool IsActive => _active;
        public bool IsStartRequestPending => _startRequestPending;
        public uint LastMuHelperZenCost { get; private set; }
        public event Action StateChanged;

        public MuHelperController(GameScene scene, GameSceneSkillController skillController, ILogger logger)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _skillController = skillController ?? throw new ArgumentNullException(nameof(skillController));
            _logger = logger;

            if (MuGame.Network is { } network)
            {
                network.MuHelperStatusUpdated += OnMuHelperStatusUpdate;
                network.MuHelperConfigurationDataReceived += OnMuHelperConfigurationData;
            }

            byte[] serverConfiguration = MuGame.Network?.GetCachedMuHelperConfigurationData();
            if (serverConfiguration is { Length: 257 })
            {
                try
                {
                    Load(MuHelperConfig.FromHelperDataBytes(serverConfiguration));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load cached server MU Helper configuration; using local settings.");
                    Load(MuGame.AppSettings?.MuHelper);
                }
            }
            else
            {
                Load(MuGame.AppSettings?.MuHelper);
            }
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
            byte[] helperData = Config.ToHelperDataBytes();
            _ = MuGame.Network?.GetCharacterService().SendMuHelperSaveDataAsync(helperData);
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
            if (_active || _startRequestPending)
                return;

            Config.Normalize();
            if (!CanOperate(out string reason))
            {
                _logger?.LogInformation("MU Helper cannot start: {Reason}", reason);
                StateChanged?.Invoke();
                return;
            }

            CharacterService service = MuGame.Network?.GetCharacterService();
            if (service == null)
            {
                _logger?.LogInformation("MU Helper cannot start: character service is unavailable.");
                StateChanged?.Invoke();
                return;
            }

            _startRequestPending = true;
            _logger?.LogInformation("Requesting MU Helper start from server.");
            _ = service.SendMuHelperStatusChangeAsync(pause: false);
            StateChanged?.Invoke();
        }

        public void Stop()
        {
            _startRequestPending = false;
            StopLocal();
            _ = MuGame.Network?.GetCharacterService().SendMuHelperStatusChangeAsync(pause: true);
        }

        public void Toggle()
        {
            if (_active || _startRequestPending)
                Stop();
            else
                Start();
        }

        private void StartLocal()
        {
            if (_active)
                return;

            Config.Normalize();
            if (!CanOperate(out string reason))
            {
                _logger?.LogInformation("MU Helper server start rejected locally: {Reason}", reason);
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
            _nextRepairAt = DateTime.MinValue;
            _nextPetAt = DateTime.MinValue;
            _nextPartyBuffAt = DateTime.MinValue;
            _comboStep = 0;
            _comboStepAt = DateTime.MinValue;
            Array.Clear(_nextActivationCastAt);
            Array.Clear(_nextBuffCastAt);

            _scene.DisableAutoAttack();
            _skillController.CancelPersistentTarget();
            _active = true;
            _logger?.LogInformation("MU Helper started at tile ({X}, {Y})", (int)_originalPosition.X, (int)_originalPosition.Y);
            StateChanged?.Invoke();
        }

        private void StopLocal()
        {
            if (!_active)
            {
                StateChanged?.Invoke();
                return;
            }

            _active = false;
            _currentTarget = null;
            _comboStep = 0;
            _skillController.CancelPersistentTarget();
            _logger?.LogInformation("MU Helper stopped");
            StateChanged?.Invoke();
        }

        private void OnMuHelperStatusUpdate(bool consumeMoney, uint money, bool pause)
        {
            _startRequestPending = false;
            if (consumeMoney)
                LastMuHelperZenCost = money;

            if (pause)
            {
                StopLocal();
                return;
            }

            if (consumeMoney)
            {
                StateChanged?.Invoke();
                return;
            }

            LastMuHelperZenCost = 0;
            StartLocal();
        }

        private void OnMuHelperConfigurationData(byte[] helperData)
        {
            try
            {
                Config = MuHelperConfig.FromHelperDataBytes(helperData);
                Config.Normalize();
                MuGame.PersistMuHelperConfig(Config);
                StateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply server MU Helper configuration data.");
            }
        }

        public void Dispose()
        {
            if (MuGame.Network is { } network)
            {
                network.MuHelperStatusUpdated -= OnMuHelperStatusUpdate;
                network.MuHelperConfigurationDataReceived -= OnMuHelperConfigurationData;
            }
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

            // MuMain Work order: Pet → Buff → Party → Heal → Loot → Regroup → Attack → Repair
            if (TryActivatePet(characterState, now))
                return;
            if (TryCastBuff(characterState, now))
                return;
            if (TryPartySupport(characterState, now))
                return;
            if (TryUsePotion(characterState, now))
                return;
            if (TryUseHealSkill(characterState, now))
                return;
            if (TryPickupOrApproachItem(characterState, now))
                return;
            if (TryRegroup(hero, now))
                return;

            MonsterObject target = SelectTarget(hero, characterState);
            _currentTarget = target;
            if (target == null || hero.IsAttackOrSkillAnimationPlaying())
            {
                TryRepairEquipment(characterState, now);
                return;
            }

            if (TryCombo(characterState, target, now))
                return;
            if (TryCastActivationSkill(characterState, target, now))
                return;
            if (Config.BasicSkillId != 0 && TryCastSkill(characterState, Config.BasicSkillId, target))
                return;
            if (Config.FallbackBasicAttack)
                hero.Attack(target);

            TryRepairEquipment(characterState, now);
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

        private bool TryActivatePet(CharacterState state, DateTime now)
        {
            if (!Config.UseDarkRaven || now < _nextPetAt)
                return false;

            var petSkill = state.GetSkills().FirstOrDefault(skill =>
            {
                string name = SkillDatabase.GetSkillName(skill.SkillId);
                return name.Contains("raven", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("dark raven", StringComparison.OrdinalIgnoreCase);
            });

            if (petSkill == null)
                return false;

            if (!_skillController.CastSkillFromHotbar(petSkill, null))
                return false;

            _nextPetAt = now + PetCooldown;
            return true;
        }

        private bool TryUsePotion(CharacterState state, DateTime now)
        {
            if (!Config.UseHealPotion || now < _nextPotionAt || state.MaximumHealth == 0)
                return false;

            int hpPercent = (int)(state.CurrentHealth * 100L / state.MaximumHealth);
            if (hpPercent > Config.PotionThreshold)
                return false;

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
        private bool TryPartySupport(CharacterState state, DateTime now)
        {
            // No PartyMembers / player-target cast API yet — pipeline placeholder.
            if (!Config.SupportParty && !Config.AutoHealParty)
                return false;

            return false;
        }
        /// <summary>
        /// Party heal / buff. Requires CharacterState party APIs; no-ops if unavailable.
        /// </summary>
        // private bool TryPartySupport(CharacterState state, DateTime now)
        // {
        //     if ((!Config.SupportParty && !Config.AutoHealParty) || now < _nextPartyBuffAt)
        //         return false;

        //     // Adjust property names to your CharacterState party model if different.
        //     var partyMembers = state.PartyMembers;
        //     if (partyMembers == null || partyMembers.Count == 0)
        //         return false;

        //     var hero = _scene.Hero;
        //     if (hero == null)
        //         return false;

        //     if (Config.AutoHealParty && Config.HealPartyThreshold > 0 && state.MaximumHealth > 0)
        //     {
        //         var healSkill = state.GetSkills().FirstOrDefault(skill =>
        //         {
        //             string name = SkillDatabase.GetSkillName(skill.SkillId);
        //             return name.Contains("heal", StringComparison.OrdinalIgnoreCase);
        //         });

        //         if (healSkill != null)
        //         {
        //             foreach (var member in partyMembers)
        //             {
        //                 if (member == null || member.IsSelf)
        //                     continue;

        //                 // Expected shape: member has CurrentHealth/MaximumHealth and can resolve to a world target.
        //                 if (member.MaximumHealth <= 0)
        //                     continue;

        //                 int hpPercent = (int)(member.CurrentHealth * 100L / member.MaximumHealth);
        //                 if (hpPercent > Config.HealPartyThreshold)
        //                     continue;

        //                 PlayerObject partyPlayer = FindPartyPlayer(member.NetworkId);
        //                 if (partyPlayer == null)
        //                     continue;

        //                 if (ChebyshevDistance(partyPlayer.Location, hero.Location) > Config.HuntingRange + 2)
        //                     continue;

        //                 if (_skillController.CastSkillFromHotbar(healSkill, partyPlayer))
        //                 {
        //                     _nextPartyBuffAt = now + PartySupportCooldown;
        //                     return true;
        //                 }
        //             }
        //         }
        //     }

        //     if (Config.SupportParty)
        //     {
        //         for (int index = 0; index < Config.BuffSkillIds.Length; index++)
        //         {
        //             ushort skillId = Config.BuffSkillIds[index];
        //             if (skillId == 0)
        //                 continue;

        //             var skill = state.GetSkills().FirstOrDefault(candidate => candidate.SkillId == skillId);
        //             if (skill == null)
        //                 continue;

        //             foreach (var member in partyMembers)
        //             {
        //                 if (member == null || member.IsSelf)
        //                     continue;

        //                 PlayerObject partyPlayer = FindPartyPlayer(member.NetworkId);
        //                 if (partyPlayer == null)
        //                     continue;

        //                 if (ChebyshevDistance(partyPlayer.Location, hero.Location) > Config.HuntingRange + 2)
        //                     continue;

        //                 if (_skillController.CastSkillFromHotbar(skill, partyPlayer))
        //                 {
        //                     _nextPartyBuffAt = now + PartySupportCooldown;
        //                     return true;
        //                 }
        //             }
        //         }
        //     }

        //     return false;
        // }

        private PlayerObject FindPartyPlayer(ushort networkId)
        {
            if (_scene.World == null)
                return null;

            return _scene.World.VisibleObjects
                .OfType<PlayerObject>()
                .FirstOrDefault(player => player.NetworkId == networkId && !player.IsDead);
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

        private bool TryCombo(CharacterState state, MonsterObject target, DateTime now)
        {
            if (!Config.UseCombo || Config.ComboSkillIds == null || Config.ComboSkillIds.Length == 0)
                return false;

            if (now > _comboStepAt + ComboStepTimeout)
                _comboStep = 0;

            if (_comboStep >= Config.ComboSkillIds.Length)
                _comboStep = 0;

            ushort skillId = Config.ComboSkillIds[_comboStep];
            if (skillId == 0)
            {
                _comboStep = 0;
                return false;
            }

            if (!TryCastSkill(state, skillId, target))
                return false;

            _comboStep++;
            _comboStepAt = now;
            if (_comboStep >= Config.ComboSkillIds.Length)
                _comboStep = 0;

            return true;
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

        private bool TryPickupOrApproachItem(CharacterState state, DateTime now)
        {
            if (Config.ObtainingRange <= 0 || _pickupInFlight)
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

            var itemPos = new Vector2(candidate.PositionX, candidate.PositionY);
            int dist = ChebyshevDistance(itemPos, hero.Location);

            // MuMain-style approach: walk into reach before sending pickup.
            if (dist > PickupReachTiles)
            {
                hero.MoveTo(new Vector2((int)itemPos.X, (int)itemPos.Y));
                return true;
            }

            if (now < _nextPickupAt)
                return true;

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

            // 1) Pick everything
            if (Config.PickAllItems)
                return true;

            bool nameMatch = Config.ExtraItems != null &&
                Config.ExtraItems.Any(name =>
                    !string.IsNullOrWhiteSpace(name) &&
                    item.ItemDescription.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase));

            // 2) Extra list — ONLY when "Add Extra Item" is checked
            if (Config.PickExtraItems && nameMatch)
                return true;

            // 3) Category filters — only in "Pick Selected Items" mode
            if (!Config.PickSelectedItems)
                return false;

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

        /// <summary>
        /// Equipment repair. Wire to CharacterService repair API when available.
        /// </summary>
        private bool TryRepairEquipment(CharacterState state, DateTime now)
        {
            if (!Config.RepairItem || now < _nextRepairAt)
                return false;

            var service = MuGame.Network?.GetCharacterService();
            if (service == null)
                return false;

            // TODO: call your real repair method when it exists, e.g.:
            // bool repaired = service.SendRepairEquippedItemsAsync(...).GetAwaiter().GetResult();
            // if (!repaired) return false;

            _nextRepairAt = now + RepairCooldown;
            return false;
        }

        private bool IsValidMonster(MonsterObject monster)
        {
            return monster != null && !monster.IsDead && monster.World == _scene.World;
        }

        private static int ChebyshevDistance(Vector2 left, Vector2 right)
        {
            return Math.Max(
                Math.Abs((int)left.X - (int)right.X),
                Math.Abs((int)left.Y - (int)right.Y));
        }
    }
}