using System;
using System.Collections.Generic;
using Client.Data.ATT;
using Client.Main.Controls;
using Client.Main.Controls.UI.Game.Hud;
using Client.Main.Controls.UI.Game.Skills;
using Client.Main.Core.Utilities;
using Client.Main.Objects;
using Client.Main.Objects.Effects;
using Client.Main.Objects.Player;
using Client.Main.Objects.Pets;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MUnique.OpenMU.Network.Packets;
using MUnique.OpenMU.Network.Packets.ClientToServer;

namespace Client.Main.Scenes
{
    internal sealed class GameSceneSkillController
    {
        private const ushort TeleportSkillId = 6;
        private const ushort TwisterSkillId = 8;
        private const ushort HellFireSkillId = 10;
        private const ushort InfernoSkillId = 14;
        private const ushort EvilSpiritSkillId = 9;
        private const ushort NovaSkillId = 40;
        private const ushort NovaStartSkillId = 58;
        private const ushort DarkRavenCommandFirstSkillId = 120;
        private const ushort DarkRavenCommandLastSkillId = 123;

        private readonly GameScene _scene;
        private readonly ModernBottomHud _hud;
        private readonly ILogger _logger;
        private readonly Func<PlayerObject, bool> _isDuelAttackTarget;

        private Core.Client.SkillEntryState _pendingSkill;
        private ushort _pendingSkillTargetId;
        private Vector2 _pendingSkillTargetLocation;
        private bool _pendingSkillHasLocation;
        private double _nextSkillApproachMs;
        private bool _pendingSkillIsArea;
        private bool _pendingSkillTargetIsPlayer;
        // A single RMB click locks a targeted skill onto the selected monster/player.
        // The target remains active after RMB is released until it dies, the skill changes,
        // or the player clicks with LMB.
        private Core.Client.SkillEntryState _persistentSkill;
        private WalkerObject _persistentSkillTarget;
        private readonly Dictionary<ushort, double> _nextSkillAllowedMs = new();
        private byte _nextAreaSkillAnimationCounter;
        private bool _novaCharging;

        public GameSceneSkillController(
            GameScene scene,
            ModernBottomHud hud,
            ILogger logger,
            Func<PlayerObject, bool> isDuelAttackTarget)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _logger = logger;
            _isDuelAttackTarget = isDuelAttackTarget ?? (_ => false);
        }

        public void Update()
        {
            UpdatePendingSkill();
            UpdateNovaState();
        }

        public void CancelPersistentTarget()
        {
            ClearPersistentSkill();
        }

        public void ClearPending()
        {
            ClearPendingSkill();
            ClearPersistentSkill();
            if (_scene.World is WalkableWorldControl world && _scene.Hero != null)
                ForceReleaseNovaCharge(world, _scene.Hero);
            else
                _novaCharging = false;
        }

        public void NotifyLocalSkillAnimation(ushort skillId)
        {
            if (skillId == NovaStartSkillId)
            {
                _novaCharging = true;
                return;
            }

            if (skillId == NovaSkillId)
            {
                _novaCharging = false;
            }
        }

        public void HandleRightClickSkillUsage()
        {
            var mouse = MuGame.Instance.Mouse;
            var prevMouse = MuGame.Instance.PrevMouseState;
            bool rightPressed = mouse.RightButton == ButtonState.Pressed;
            bool rightJustPressed = rightPressed && prevMouse.RightButton == ButtonState.Released;
            bool rightJustReleased = !rightPressed && prevMouse.RightButton == ButtonState.Pressed;
            bool leftPressed = mouse.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = leftPressed && prevMouse.LeftButton == ButtonState.Released;

            var skill = _hud.SelectedSkill;
            var hero = _scene.Hero;
            var walkableForSkills = _scene.World as WalkableWorldControl;

            // ── Nova charge release (SourceMain SkillKeyPush + RMB release) ──────────
            if (_novaCharging && (rightJustReleased || leftJustPressed))
            {
                TryReleaseNovaCharge(hero, walkableForSkills);
                return;
            }

            if (_scene.IsMouseInputConsumedThisFrame)
                return;

            if (IsMouseOverUi())
                return;

            // ── SourceMain: LMB always aborts skill casting / clears locked target ───
            // Mirror of:
            //   if (MouseLButtonPush) { clear all MouseRButton*; return; }
            // and the guard:
            //   (MouseRButton…) && !(MouseLButtonPush || MouseLButton)
            if (leftPressed)
            {
                ClearPersistentSkill();   // also clears pending
                // Do NOT consume the frame – let the normal LMB attack/move path run.
                return;
            }

            if (skill == null)
            {
                ClearPersistentSkill();
                return;
            }

            if (hero == null || hero.IsDead || walkableForSkills == null)
            {
                ClearPersistentSkill();
                return;
            }

            if (IsDarkRavenCommandSkill(skill.SkillId))
            {
                if (!rightPressed)
                    return;

                TryUseDarkRavenCommand(skill.SkillId, hero, rightJustPressed);
                return;
            }

            if (skill.SkillId == NovaSkillId)
            {
                TryStartNovaCharge(skill, hero, walkableForSkills, rightJustPressed);
                return;
            }

            // SafeZone check
            var terrainFlags = walkableForSkills.Terrain.RequestTerrainFlag((int)hero.Location.X, (int)hero.Location.Y);
            if (terrainFlags.HasFlag(TWFlags.SafeZone))
            {
                _logger?.LogDebug("Cannot use skill in SafeZone");
                ClearPersistentSkill();
                _scene.SetMouseInputConsumed();
                return;
            }

            // Elf buffs, healing and Soul Barrier can target another player or self.
            if (SkillCastRules.IsFriendlyTargetSkill(skill.SkillId))
            {
                ClearPersistentSkill();
                if (rightJustPressed)
                {
                    var friendly = _scene.MouseHoverObject as PlayerObject;
                    if (friendly == null || friendly == hero)
                        UseSelfSkill(skill, hero);
                    else if (!friendly.IsDead && friendly.World == hero.World)
                    {
                        if (IsInSkillRange(friendly.Location, skill.SkillId))
                            UseSkillOnPlayerTarget(skill, friendly);
                        else
                            QueueSkillCast(skill, friendly, SkillDatabase.GetSkillRange(skill.SkillId), isAreaSkill: false);
                    }
                }
                _scene.SetMouseInputConsumed();
                return;
            }

            // Self skills have no target-distance gate.
            if (SkillDatabase.IsSelfSkill(skill.SkillId))
            {
                ClearPersistentSkill();
                if (rightJustPressed)
                    UseSelfSkill(skill, hero);
                _scene.SetMouseInputConsumed();
                return;
            }

            // Lock target on RMB click
            if (rightJustPressed)
            {
                var clickedTarget = GetHoveredSkillTarget();
                if (CanLockSkillTarget(skill, clickedTarget))
                    SetPersistentSkill(skill, clickedTarget);
                else
                    ClearPersistentSkill();
            }

            // Persistent locked target (hold or click-lock)
            if (HasValidPersistentSkill(skill))
            {
                UsePersistentSkill(skill, _persistentSkillTarget, walkableForSkills);
                _scene.SetMouseInputConsumed();
                return;
            }

            // Hold-to-cast for non-targeted / area skills
            if (!rightPressed)
                return;

            ClearPendingSkill();
            uint allowedRange = SkillDatabase.GetSkillRange(skill.SkillId);

            if (skill.SkillId == TeleportSkillId)
            {
                var mouseTile = new Vector2(walkableForSkills.MouseTileX, walkableForSkills.MouseTileY);
                if (IsInSkillRange(mouseTile, skill.SkillId, hasTarget: false))
                    UseAreaSkill(skill, 0, mouseTile);
                else
                    _logger?.LogDebug("Teleport target out of range. Target=({X},{Y}) Range={Range}",
                        mouseTile.X, mouseTile.Y, allowedRange);

                _scene.SetMouseInputConsumed();
                return;
            }

            var hoveredTarget = GetHoveredSkillTarget();
            if (IsAreaSkill(skill.SkillId))
            {
                if (SkillCastRules.IgnoresTargetRange(skill.SkillId, hoveredTarget != null))
                {
                    UseAreaSkill(skill);
                    _scene.SetMouseInputConsumed();
                    return;
                }

                var skillTarget = hoveredTarget;
                var mouseTile = new Vector2(walkableForSkills.MouseTileX, walkableForSkills.MouseTileY);
                if (skillTarget == null)
                {
                    if (IsInSkillRange(mouseTile, skill.SkillId, hasTarget: false))
                        UseAreaSkill(skill, 0, mouseTile);
                    else
                        QueueAreaSkillCast(skill, mouseTile, allowedRange);
                }
                else if (IsInSkillRange(skillTarget.Location, skill.SkillId))
                {
                    UseAreaSkill(skill, skillTarget.NetworkId);
                }
                else
                {
                    QueueSkillCast(skill, skillTarget, allowedRange, isAreaSkill: true);
                }
            }
            else
            {
                if (hoveredTarget is MonsterObject targetMonster)
                {
                    if (IsInSkillRange(targetMonster.Location, skill.SkillId))
                        UseSkillOnTarget(skill, targetMonster);
                    else
                        QueueSkillCast(skill, targetMonster, allowedRange, isAreaSkill: false);
                }
                else if (hoveredTarget is PlayerObject targetPlayer)
                {
                    if (IsInSkillRange(targetPlayer.Location, skill.SkillId))
                        UseSkillOnPlayerTarget(skill, targetPlayer);
                    else
                        QueueSkillCast(skill, targetPlayer, allowedRange, isAreaSkill: false);
                }
            }

            _scene.SetMouseInputConsumed();
        }

        private void TryUseDarkRavenCommand(ushort skillId, PlayerObject hero, bool rightJustPressed)
        {
            if (!rightJustPressed)
                return;

            if (hero.EquippedHelper?.Kind != FlyingHelperKind.DarkRaven)
            {
                _logger?.LogDebug("Dark Raven command ignored because Dark Raven is not equipped.");
                _scene.SetMouseInputConsumed();
                return;
            }

            PetCommandMode commandMode = (PetCommandMode)(skillId - DarkRavenCommandFirstSkillId);
            ushort targetId = 0xFFFF;
            if (commandMode == PetCommandMode.AttackTarget)
            {
                WalkerObject target = GetHoveredSkillTarget();
                if (target == null)
                {
                    _logger?.LogDebug("Dark Raven target command ignored because no valid target is hovered.");
                    _scene.SetMouseInputConsumed();
                    return;
                }

                targetId = target.NetworkId;
            }

            _ = MuGame.Network.GetCharacterService().SendDarkRavenCommandAsync(commandMode, targetId);
            _scene.SetMouseInputConsumed();
        }

        private void UpdateNovaState()
        {
            if (!_novaCharging)
                return;

            var hero = _scene.Hero;
            if (hero == null || hero.IsDead)
            {
                if (_scene.World is WalkableWorldControl deadWorld && hero != null)
                    ScrollOfNovaChargeEffect.StopForCaster(deadWorld, hero.NetworkId);

                _novaCharging = false;
                return;
            }

            var selectedSkill = _hud.SelectedSkill;
            if (selectedSkill?.SkillId == NovaSkillId)
                return;

            // If player switched skill while charging, stop local charging state and visuals.
            if (_scene.World is WalkableWorldControl world)
                ForceReleaseNovaCharge(world, hero);
            else
                _novaCharging = false;
        }

        private void TryStartNovaCharge(Core.Client.SkillEntryState skill, PlayerObject hero, WalkableWorldControl world, bool rightJustPressed)
        {
            if (_novaCharging || !rightJustPressed)
                return;

            var terrainFlags = world.Terrain.RequestTerrainFlag((int)hero.Location.X, (int)hero.Location.Y);
            if (terrainFlags.HasFlag(TWFlags.SafeZone))
            {
                _logger?.LogDebug("Cannot use Nova in SafeZone");
                _scene.SetMouseInputConsumed();
                return;
            }

            if (!CanStartCast(hero))
                return;

            if (!TryConsumeSkillDelay(NovaSkillId))
                return;

            if (!HasResourcesForNovaStart())
                return;

            _novaCharging = true;
            ClearPendingSkill();

            var startAction = hero.GetSkillAction(NovaStartSkillId, isInSafeZone: false);
            hero.PlayAction((ushort)startAction);
            hero.TriggerVehicleSkillAnimation();

            ushort targetId = hero.NetworkId != 0 ? hero.NetworkId : (ushort)_scene.Hero.NetworkId;
            _ = MuGame.Network.GetCharacterService().SendSkillRequestAsync(NovaStartSkillId, targetId);

            // Start local charging visuals immediately; server packets will refine stage.
            ScrollOfNovaChargeEffect.GetOrCreate(world, hero);

            _logger?.LogInformation("Started Nova charge (skill {SkillId} -> start {StartSkillId})", skill.SkillId, NovaStartSkillId);
            _scene.SetMouseInputConsumed();
        }

        private void TryReleaseNovaCharge(PlayerObject hero, WalkableWorldControl world)
        {
            if (!_novaCharging)
                return;

            _novaCharging = false;

            if (hero == null || hero.IsDead || world == null)
                return;

            var releaseAction = hero.GetSkillAction(NovaSkillId, isInSafeZone: false);
            hero.PlayAction((ushort)releaseAction);
            hero.TriggerVehicleSkillAnimation();

            ushort targetId = ResolveNovaReleaseTargetId(hero);
            _ = MuGame.Network.GetCharacterService().SendSkillRequestAsync(NovaSkillId, targetId);

            SpawnNovaExplosion(world, hero);

            _logger?.LogInformation("Released Nova charge with target {TargetId}", targetId);
            _scene.SetMouseInputConsumed();
        }

        private void ForceReleaseNovaCharge(WalkableWorldControl world, PlayerObject hero)
        {
            if (!_novaCharging || hero == null || hero.IsDead || world == null)
            {
                _novaCharging = false;
                return;
            }

            _novaCharging = false;

            ushort targetId = hero.NetworkId != 0 ? hero.NetworkId : ResolveNovaReleaseTargetId(hero);
            _ = MuGame.Network.GetCharacterService().SendSkillRequestAsync(NovaSkillId, targetId);
            SpawnNovaExplosion(world, hero);
        }

        /// <summary>
        /// Consumes the current Nova charge stage and spawns the release explosion
        /// (matches remote-player path via NovaSkillEffect factory).
        /// </summary>
        private void SpawnNovaExplosion(WalkableWorldControl world, PlayerObject hero)
        {
            byte stage = ScrollOfNovaChargeEffect.ConsumeStageAndStop(world, hero.NetworkId);
            var explosion = new ScrollOfNovaExplosionEffect(hero, hero.WorldPosition.Translation, stage);
            world.Objects.Add(explosion);
            _ = explosion.Load();
        }

        private ushort ResolveNovaReleaseTargetId(PlayerObject hero)
        {
            var hoveredTarget = GetHoveredSkillTarget();
            if (hoveredTarget is MonsterObject monster && !monster.IsDead)
            {
                hero.FaceTowards(monster.Location, immediate: true);
                return monster.NetworkId;
            }

            if (hoveredTarget is PlayerObject player && !player.IsDead && _isDuelAttackTarget(player))
            {
                hero.FaceTowards(player.Location, immediate: true);
                return player.NetworkId;
            }

            return hero.NetworkId != 0 ? hero.NetworkId : _characterStateSafeIdFallback();

            ushort _characterStateSafeIdFallback()
            {
                var state = MuGame.Network?.GetCharacterState();
                return state?.Id ?? (ushort)0;
            }
        }

        private bool HasResourcesForNovaStart()
        {
            var characterState = MuGame.Network?.GetCharacterState();
            if (characterState == null)
                return true;

            ushort manaCost = SkillDatabase.GetSkillManaCost(NovaSkillId);
            ushort agCost = SkillDatabase.GetSkillAGCost(NovaStartSkillId);
            if (agCost == 0)
                agCost = SkillDatabase.GetSkillAGCost(NovaSkillId);

            if (characterState.CurrentMana < manaCost)
            {
                _logger?.LogDebug("Not enough mana for Nova charge start. Required: {Required}, Current: {Current}",
                    manaCost, characterState.CurrentMana);
                return false;
            }

            if (characterState.CurrentAbility < agCost)
            {
                _logger?.LogDebug("Not enough AG for Nova charge start. Required: {Required}, Current: {Current}",
                    agCost, characterState.CurrentAbility);
                return false;
            }

            return true;
        }

        private bool CanLockSkillTarget(Core.Client.SkillEntryState skill, WalkerObject target)
        {
            if (skill == null || target == null || skill.SkillId == TeleportSkillId ||
                skill.SkillId == NovaSkillId || SkillDatabase.IsSelfSkill(skill.SkillId))
            {
                return false;
            }

            if (SkillCastRules.IgnoresTargetRange(skill.SkillId, hasTarget: true))
            {
                return false;
            }

            if (target is MonsterObject monster)
                return !monster.IsDead && monster.World == _scene.World;

            return target is PlayerObject player &&
                player != _scene.Hero &&
                !player.IsDead &&
                player.World == _scene.World &&
                _isDuelAttackTarget(player);
        }

        private bool HasValidPersistentSkill(Core.Client.SkillEntryState selectedSkill)
        {
            if (_persistentSkill == null || _persistentSkillTarget == null ||
                selectedSkill == null || selectedSkill.SkillId != _persistentSkill.SkillId)
            {
                ClearPersistentSkill();
                return false;
            }

            return CanLockSkillTarget(_persistentSkill, _persistentSkillTarget);
        }

        private void SetPersistentSkill(Core.Client.SkillEntryState skill, WalkerObject target)
        {
            _persistentSkill = skill;
            _persistentSkillTarget = target;
            ClearPendingSkill();
        }

        private void ClearPersistentSkill()
        {
            _persistentSkill = null;
            _persistentSkillTarget = null;
            ClearPendingSkill();
        }

        private void UsePersistentSkill(
            Core.Client.SkillEntryState skill,
            WalkerObject target,
            WalkableWorldControl world)
        {
            if (!CanLockSkillTarget(skill, target))
            {
                ClearPersistentSkill();
                return;
            }

            uint allowedRange = SkillDatabase.GetSkillRange(skill.SkillId);
            if (!IsInSkillRange(target.Location, skill.SkillId))
            {
                QueueSkillCast(skill, target, allowedRange, IsAreaSkill(skill.SkillId));
                return;
            }

            ClearPendingSkill();
            bool sent = target is PlayerObject targetPlayer
                ? (IsAreaSkill(skill.SkillId)
                    ? UseAreaSkill(skill, targetPlayer.NetworkId)
                    : UseSkillOnPlayerTarget(skill, targetPlayer))
                : (IsAreaSkill(skill.SkillId)
                    ? UseAreaSkill(skill, target.NetworkId)
                    : UseSkillOnTarget(skill, (MonsterObject)target));

            if (!sent && world == null)
                ClearPersistentSkill();
        }

        private WalkerObject GetHoveredSkillTarget()
        {
            if (_scene.World != null)
            {
                MonsterObject targetedMonster = WorldHoverSystem.FindBestLiveMonster(
                    _scene.World.VisibleObjects,
                    MuGame.Instance.MouseRay,
                    _scene.World);
                if (targetedMonster != null)
                    return targetedMonster;
            }

            if (_scene.MouseHoverObject is MonsterObject monster)
            {
                if (!monster.IsDead && monster.World == _scene.World)
                    return monster;
                return null;
            }

            if (_scene.MouseHoverObject is PlayerObject player)
            {
                if (player != _scene.Hero &&
                    !player.IsDead &&
                    player.World == _scene.World &&
                    _isDuelAttackTarget(player))
                {
                    return player;
                }
            }

            return null;
        }

        private bool IsMouseOverUi()
        {
            return _scene.MouseHoverControl != null && _scene.MouseHoverControl != _scene.World;
        }

        private static bool IsAreaSkill(ushort skillId)
        {
            return SkillDatabase.IsAreaSkill(skillId);
        }

        private static bool IsDarkRavenCommandSkill(ushort skillId)
        {
            return skillId >= DarkRavenCommandFirstSkillId &&
                   skillId <= DarkRavenCommandLastSkillId;
        }

        private float GetCastRange(ushort skillId, bool hasTarget)
        {
            var state = MuGame.Network?.GetCharacterState();
            ushort map = _scene.World?.MapId ?? 0;
            return SkillCastRules.GetRange(skillId, SkillDatabase.GetSkillRange(skillId),
                state?.IsDarkHorseEquipped == true, map is >= 11 and <= 17 or 52, hasTarget);
        }

        private bool IsInSkillRange(Vector2 targetLocation, ushort skillId, bool hasTarget = true)
        {
            var hero = _scene.Hero;
            if (hero == null)
                return false;

            if (SkillCastRules.IgnoresTargetRange(skillId, hasTarget))
                return true;

            return SkillCastRules.IsInRange(hero.Position, targetLocation, GetCastRange(skillId, hasTarget));
        }

        private static bool CanStartCast(PlayerObject hero) =>
            !hero.IsMoving && !hero.MovementIntent &&
            SkillCastRules.CanStartCast((Models.PlayerAction)hero.CurrentAction);

        private void QueueSkillCast(Core.Client.SkillEntryState skill, WalkerObject target, uint allowedRange, bool isAreaSkill)
        {
            var hero = _scene.Hero;
            if (hero == null || target == null || hero.IsDead ||
                SkillCastRules.BlocksWalking((Models.PlayerAction)hero.CurrentAction))
                return;

            _pendingSkill = skill;
            _pendingSkillTargetId = target.NetworkId;
            _pendingSkillTargetIsPlayer = target is PlayerObject;
            _pendingSkillIsArea = isAreaSkill;
            _pendingSkillHasLocation = false;
            ApproachSkillTarget(target.Location, skill.SkillId);
        }

        private void ApproachSkillTarget(Vector2 location, ushort skillId)
        {
            var hero = _scene.Hero;
            if (hero == null || hero.IsMoving || hero.MovementIntent || GetNowMs() < _nextSkillApproachMs)
                return;

            _nextSkillApproachMs = GetNowMs() + 250;
            hero.MoveTo(location, stopWithinRange: GetCastRange(skillId, hasTarget: true));
        }

        private void UpdatePendingSkill()
        {
            var hero = _scene.Hero;
            if (_pendingSkill == null || hero == null || hero.IsDead)
            {
                ClearPendingSkill();
                return;
            }

            if (MuGame.Instance.Mouse.LeftButton == ButtonState.Pressed)
            {
                ClearPendingSkill();
                return;
            }

            if (_pendingSkill.SkillId == TeleportSkillId)
            {
                // Teleport is an instant skill; it shouldn't path towards the target.
                ClearPendingSkill();
                return;
            }

            if (_pendingSkillTargetId == 0 && !_pendingSkillHasLocation)
            {
                ClearPendingSkill();
                return;
            }

            if (_hud.SelectedSkill == null || _hud.SelectedSkill.SkillId != _pendingSkill.SkillId)
            {
                ClearPendingSkill();
                return;
            }

            if (_scene.World is not WalkableWorldControl walkableWorld)
            {
                ClearPendingSkill();
                return;
            }

            var terrainFlags = walkableWorld.Terrain.RequestTerrainFlag((int)hero.Location.X, (int)hero.Location.Y);
            if (terrainFlags.HasFlag(TWFlags.SafeZone))
            {
                ClearPendingSkill();
                return;
            }

            if (_pendingSkillHasLocation)
            {
                if (IsInSkillRange(_pendingSkillTargetLocation, _pendingSkill.SkillId, hasTarget: false))
                {
                    bool sent = UseAreaSkill(_pendingSkill, 0, _pendingSkillTargetLocation);
                    if (sent)
                        ClearPendingSkill();
                }
                else
                {
                    // Never move from RMB skill input when the target is out of range.
                    ClearPendingSkill();
                }
                return;
            }

            if (!walkableWorld.WalkerObjectsById.TryGetValue(_pendingSkillTargetId, out var walker))
            {
                ClearPendingSkill();
                return;
            }

            if (_pendingSkillTargetIsPlayer)
            {
                if (walker is not PlayerObject targetPlayer || targetPlayer.IsDead ||
                    (!SkillCastRules.IsFriendlyTargetSkill(_pendingSkill.SkillId) && !_isDuelAttackTarget(targetPlayer)))
                {
                    ClearPendingSkill();
                    return;
                }

                if (IsInSkillRange(targetPlayer.Location, _pendingSkill.SkillId))
                {
                    bool sent = _pendingSkillIsArea
                        ? UseAreaSkill(_pendingSkill, targetPlayer.NetworkId)
                        : UseSkillOnPlayerTarget(_pendingSkill, targetPlayer);
                    if (sent)
                        ClearPendingSkill();
                }
                else
                {
                    ApproachSkillTarget(targetPlayer.Location, _pendingSkill.SkillId);
                }
                return;
            }

            if (walker is not MonsterObject targetMonster || targetMonster.IsDead || targetMonster.World != _scene.World)
            {
                ClearPendingSkill();
                return;
            }

            if (IsInSkillRange(targetMonster.Location, _pendingSkill.SkillId))
            {
                bool sent = _pendingSkillIsArea
                    ? UseAreaSkill(_pendingSkill, targetMonster.NetworkId)
                    : UseSkillOnTarget(_pendingSkill, targetMonster);
                if (sent)
                    ClearPendingSkill();
            }
            else
            {
                ApproachSkillTarget(targetMonster.Location, _pendingSkill.SkillId);
            }
        }

        private void ClearPendingSkill()
        {
            _pendingSkill = null;
            _pendingSkillTargetId = 0;
            _pendingSkillTargetLocation = Vector2.Zero;
            _pendingSkillHasLocation = false;
            _pendingSkillIsArea = false;
            _pendingSkillTargetIsPlayer = false;
        }

        private bool UseSkillOnTarget(Core.Client.SkillEntryState skill, MonsterObject target)
        {
            var hero = _scene.Hero;
            if (skill == null || target == null || hero == null)
                return false;

            if (hero.IsDead)
                return false;

            if (!IsInSkillRange(target.Location, skill.SkillId))
                return false;

            if (!TryBeginSkillCast(skill, hero))
                return false;

            hero.FaceTowards(target.Location, immediate: true);

            _logger?.LogInformation("Using targeted skill {SkillId} (Level {Level}) on target {TargetId}",
                skill.SkillId, skill.SkillLevel, target.NetworkId);

            _ = MuGame.Network.GetCharacterService().SendSkillRequestAsync(
                skill.SkillId,
                target.NetworkId);

            return true;
        }

        /// <summary>
        /// Casts a skill activated by the Classic touch hotbar using its declared usage type.
        /// Target skills use the nearest monster, area skills use that monster's position,
        /// and self skills target the player's own character.
        /// </summary>
        internal bool CastSkillFromHotbar(Core.Client.SkillEntryState skill, MonsterObject target)
        {
            var hero = _scene.Hero;
            if (skill == null || hero == null || hero.IsDead)
                return false;

            if (IsDarkRavenCommandSkill(skill.SkillId))
            {
                TryUseDarkRavenCommand(skill.SkillId, hero, rightJustPressed: true);
                return hero.EquippedHelper?.Kind == FlyingHelperKind.DarkRaven;
            }

            if (SkillCastRules.IsFriendlyTargetSkill(skill.SkillId))
                return UseSelfSkill(skill, hero);

            if (SkillDatabase.IsSelfSkill(skill.SkillId))
                return UseSelfSkill(skill, hero);

            if (SkillDatabase.IsAreaSkill(skill.SkillId))
            {
                Vector2 targetLocation = target?.Location ?? hero.Location;
                ushort targetId = target?.NetworkId ?? (ushort)0;
                return UseAreaSkill(skill, targetId, targetLocation);
            }

            return target != null && UseSkillOnTarget(skill, target);
        }

        private bool UseSelfSkill(Core.Client.SkillEntryState skill, PlayerObject hero)
        {
            if (skill == null || hero == null || hero.IsDead)
                return false;

            if (!TryBeginSkillCast(skill, hero))
                return false;

            ushort targetId = hero.NetworkId;
            if (targetId == 0)
                targetId = MuGame.Network?.GetCharacterState()?.Id ?? (ushort)0;

            _logger?.LogInformation("Using self skill {SkillId} (Level {Level}) on player {TargetId}",
                skill.SkillId, skill.SkillLevel, targetId);

            _ = MuGame.Network.GetCharacterService().SendSkillRequestAsync(skill.SkillId, targetId);
            return true;
        }

        private bool UseSkillOnPlayerTarget(Core.Client.SkillEntryState skill, PlayerObject target)
        {
            var hero = _scene.Hero;
            if (skill == null || target == null || hero == null)
                return false;

            if (hero.IsDead || target.IsDead)
                return false;

            if (!SkillCastRules.IsFriendlyTargetSkill(skill.SkillId) && !_isDuelAttackTarget(target))
                return false;

            if (!IsInSkillRange(target.Location, skill.SkillId))
                return false;

            if (!TryBeginSkillCast(skill, hero))
                return false;

            hero.FaceTowards(target.Location, immediate: true);

            _logger?.LogInformation("Using targeted skill {SkillId} (Level {Level}) on duel target player {TargetId}",
                skill.SkillId, skill.SkillLevel, target.NetworkId);

            _ = MuGame.Network.GetCharacterService().SendSkillRequestAsync(
                skill.SkillId,
                target.NetworkId);

            return true;
        }

        private bool UseAreaSkill(Core.Client.SkillEntryState skill, ushort extraTargetId = 0, Vector2? targetLocationOverride = null)
        {
            var hero = _scene.Hero;
            if (skill == null || hero == null)
                return false;

            if (hero.IsDead)
                return false;

            Vector2 targetTile = hero.Location;
            if (targetLocationOverride.HasValue)
                targetTile = targetLocationOverride.Value;
            else if (_scene.World is WalkableWorldControl aimWorld)
            {
                if (extraTargetId != 0 && aimWorld.TryGetWalkerById(extraTargetId, out var target))
                    targetTile = target.Location;
                else
                    targetTile = new Vector2(aimWorld.MouseTileX, aimWorld.MouseTileY);
            }

            // Validate the aimed tile before changing the packet origin for skills
            // such as Twister/Evil Spirit, which send the caster's coordinates.
            if (!IsInSkillRange(targetTile, skill.SkillId, extraTargetId != 0))
                return false;

            byte targetX = (byte)Math.Clamp((int)targetTile.X, 0, Constants.TERRAIN_SIZE - 1);
            byte targetY = (byte)Math.Clamp((int)targetTile.Y, 0, Constants.TERRAIN_SIZE - 1);
            byte requestTargetX = targetX;
            byte requestTargetY = targetY;

            if (skill.SkillId == TeleportSkillId)
            {
                if (_scene.World is WorldControl worldForTeleport &&
                    !worldForTeleport.IsWalkable(new Vector2(targetX, targetY)))
                {
                    _logger?.LogDebug("Teleport target ({X},{Y}) is not walkable.", targetX, targetY);
                    return false;
                }
            }

            if (!TryBeginSkillCast(skill, hero))
                return false;

            hero.FaceTowards(new Vector2(targetX, targetY), immediate: true);

            var characterState = MuGame.Network?.GetCharacterState();

            if (SkillCastRules.UsesCasterAreaPosition(skill.SkillId))
            {
                requestTargetX = (byte)Math.Clamp((int)hero.Location.X, 0, Constants.TERRAIN_SIZE - 1);
                requestTargetY = (byte)Math.Clamp((int)hero.Location.Y, 0, Constants.TERRAIN_SIZE - 1);
            }

            if (skill.SkillId == TeleportSkillId)
            {
                _logger?.LogInformation("Using teleport skill {SkillId} (Level {Level}) to position ({X},{Y})",
                    skill.SkillId, skill.SkillLevel, targetX, targetY);

                characterState?.BeginTeleport();

                hero.StopMovement();
                hero.Hidden = true; // Hide hero until server responds

                _ = MuGame.Network.GetCharacterService().SendEnterGateRequestAsync(0, targetX, targetY);
                return true;
            }

            byte animationCounter = NextAreaSkillAnimationCounter();
            if (characterState != null)
            {
                characterState.LastAreaSkillId = skill.SkillId;
                characterState.LastAreaSkillTargetX = requestTargetX;
                characterState.LastAreaSkillTargetY = requestTargetY;
                characterState.LastAreaSkillAnimationCounter = animationCounter;
                characterState.LastAreaSkillSentAtMs = GetNowMs();
            }

            if (extraTargetId != 0)
            {
                _logger?.LogInformation("Using skill {SkillId} (Level {Level}) at position ({X},{Y}) with target {TargetId}",
                    skill.SkillId, skill.SkillLevel, requestTargetX, requestTargetY, extraTargetId);
            }
            else
            {
                _logger?.LogInformation("Using area skill {SkillId} (Level {Level}) at position ({X},{Y})",
                    skill.SkillId, skill.SkillLevel, requestTargetX, requestTargetY);
            }

            float angleZ = MathHelper.WrapAngle(hero.Angle.Z);
            if (angleZ < 0f)
            {
                angleZ += MathHelper.TwoPi;
            }
            byte rotation = (byte)(angleZ / MathHelper.TwoPi * 256f);

            _ = MuGame.Network.GetCharacterService().SendAreaSkillRequestAsync(
                skill.SkillId,
                requestTargetX,
                requestTargetY,
                rotation,
                extraTargetId: extraTargetId,
                animationCounter: animationCounter);

            return true;
        }

        private void QueueAreaSkillCast(Core.Client.SkillEntryState skill, Vector2 targetLocation, uint allowedRange)
        {
            // RMB is skill-only. Do not queue a movement request for an area
            // skill whose location is outside the current cast range.
            ClearPendingSkill();
        }

        private bool TryBeginSkillCast(Core.Client.SkillEntryState skill, PlayerObject hero)
        {
            if (skill.SkillId == 76 && MuGame.Network?.GetCharacterState()?.IsFenrirEquipped != true)
                return false;
            if (!CanStartCast(hero))
                return false;

            if (!TryConsumeSkillDelay(skill.SkillId))
                return false;

            // Check player resources and stat requirements (mirrors SourceMain CSkillManager checks)
            var characterState = MuGame.Network?.GetCharacterState();
            if (characterState != null)
            {
                ushort manaCost = SkillDatabase.GetSkillManaCost(skill.SkillId);
                ushort agCost = SkillDatabase.GetSkillAGCost(skill.SkillId);

                if (characterState.CurrentMana < manaCost)
                {
                    _logger?.LogDebug("Not enough mana to use skill {SkillId}. Required: {Required}, Current: {Current}",
                        skill.SkillId, manaCost, characterState.CurrentMana);
                    return false;
                }

                if (characterState.CurrentAbility < agCost)
                {
                    _logger?.LogDebug("Not enough AG to use skill {SkillId}. Required: {Required}, Current: {Current}",
                        skill.SkillId, agCost, characterState.CurrentAbility);
                    return false;
                }

                // Stat requirement check (SourceMain: DemendConditionCheckSkill)
                var def = SkillDatabase.GetSkillDefinition(skill.SkillId);
                if (def != null)
                {
                    if (def.RequiredLevel > 0 && characterState.Level < def.RequiredLevel)
                    {
                        _logger?.LogDebug("Level too low for skill {SkillId}. Required: {Required}, Current: {Current}",
                            skill.SkillId, def.RequiredLevel, characterState.Level);
                        return false;
                    }

                    if (def.RequiredStrength > 0 && characterState.TotalStrength < def.RequiredStrength)
                    {
                        _logger?.LogDebug("Not enough Strength for skill {SkillId}. Required: {Required}, Current: {Current}",
                            skill.SkillId, def.RequiredStrength, characterState.TotalStrength);
                        return false;
                    }

                    if (def.RequiredDexterity > 0 && characterState.TotalAgility < def.RequiredDexterity)
                    {
                        _logger?.LogDebug("Not enough Dexterity for skill {SkillId}. Required: {Required}, Current: {Current}",
                            skill.SkillId, def.RequiredDexterity, characterState.TotalAgility);
                        return false;
                    }

                    if (def.RequiredEnergy > 0)
                    {
                        int requiredEnergy = Core.Client.SkillManager.CalculateRequiredEnergy(def, characterState.Class);

                        if (characterState.TotalEnergy < requiredEnergy)
                        {
                            _logger?.LogDebug("Not enough Energy for skill {SkillId}. Required: {Required}, Current: {Current}",
                                skill.SkillId, requiredEnergy, characterState.TotalEnergy);
                            return false;
                        }
                    }

                    if (def.RequiredLeadership > 0 && characterState.TotalLeadership < def.RequiredLeadership)
                    {
                        _logger?.LogDebug("Not enough Leadership for skill {SkillId}. Required: {Required}, Current: {Current}",
                            skill.SkillId, def.RequiredLeadership, characterState.TotalLeadership);
                        return false;
                    }
                }
            }

            bool isInSafeZone = false;
            if (_scene.World is WalkableWorldControl walkableWorld)
            {
                var flags = walkableWorld.Terrain.RequestTerrainFlag((int)hero.Location.X, (int)hero.Location.Y);
                isInSafeZone = flags.HasFlag(TWFlags.SafeZone);
            }

            var action = hero.GetSkillAction(skill.SkillId, isInSafeZone);
            hero.PlayAction((ushort)action);
            hero.TriggerVehicleSkillAnimation();
            return true;
        }

        private bool TryConsumeSkillDelay(ushort skillId)
        {
            double now = GetNowMs();
            if (!Core.Client.SkillCooldownTracker.TryConsume(skillId, now))
                return false;

            // Mirror to local tracking for backward compat
            int delayMs = SkillDatabase.GetSkillCooldown(skillId);
            if (delayMs <= 0)
                return true;

            _nextSkillAllowedMs[skillId] = now + delayMs;
            return true;
        }

        private static double GetNowMs()
        {
            var gameTime = MuGame.Instance?.GameTime;
            if (gameTime != null)
                return gameTime.TotalGameTime.TotalMilliseconds;

            return Environment.TickCount64;
        }

        private byte NextAreaSkillAnimationCounter()
        {
            // Mirrors original client behavior: a small rolling serial number is used
            // to tie AreaSkillHit packets to the AreaSkill animation.
            _nextAreaSkillAnimationCounter++;
            if (_nextAreaSkillAnimationCounter > 50)
                _nextAreaSkillAnimationCounter = 1;

            return _nextAreaSkillAnimationCounter;
        }
    }
}
