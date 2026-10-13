using System.Reflection;
using System.Runtime.CompilerServices;
using Client.Main;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controls.UI.Game.Hud;
using Client.Data.Texture;
using Client.Main.Content;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

try
{
    int checks = 0;
    void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    if (args.Contains("--experience-only"))
    {
        foreach (int level in new[] { 1, 2, 31, 255, 256, 257, 399 })
        {
            ulong start = Client.Main.Core.Utilities.ExperienceProgress.LevelStart(level);
            ulong next = Client.Main.Core.Utilities.ExperienceProgress.LevelStart(level + 1);
            Check(Client.Main.Core.Utilities.ExperienceProgress.Ratio(start, next, start) == 0, $"Level {level} did not start at zero.");
            Check(Client.Main.Core.Utilities.ExperienceProgress.Ratio(next, next, start) == 1, $"Level {level} did not end at 100%.");
            Check(Math.Abs(Client.Main.Core.Utilities.ExperienceProgress.Ratio(start + (next - start) / 2, next, start) - 0.5) < 0.001, $"Level {level} midpoint is incorrect.");
        }
        Check(Client.Main.Core.Utilities.ExperienceProgress.LevelStart(257) == 173680400UL, "High-level EXP formula differs from MuMain.");
        Check(Client.Main.Core.Utilities.ExperienceProgress.Digit(0.75) == 7, "MuMain EXP digit is incorrect.");
        Check(Client.Main.Core.Utilities.ExperienceProgress.Segment(0.75) == 0.5, "MuMain EXP segment is incorrect.");
        Check(Client.Main.Core.Utilities.ExperienceProgress.Digit(1) == 9 && Client.Main.Core.Utilities.ExperienceProgress.Segment(1) == 1, "Full EXP bar overflowed.");
        Check(Client.Main.Core.Utilities.ExperienceProgress.Ratio(0, 100, 50) == 0, "EXP underflowed.");
        Console.WriteLine($"PASS: {checks} EXP progress checks.");
        return;
    }
    await Client.Main.Core.Utilities.SkillDatabase.Initialize();
    var rosterType = typeof(GameScene).Assembly.GetType("Client.Main.Controls.UI.Game.Skills.ClassSkillRoster")!;
    var rosterFor = rosterType.GetMethod("For", BindingFlags.Static | BindingFlags.Public)!;
    foreach (var (classNumber, skillNumber, damage) in new (int, int, int)[]
        { (4, 44, 90), (0, 45, 90), (8, 46, 120), (12, 57, 75), (16, 74, 150), (20, 45, 90), (24, 269, 90) })
    {
        var roster = (ushort[])rosterFor.Invoke(null, [(MUnique.OpenMU.Network.Packets.CharacterClassNumber)classNumber])!;
        Check(roster.Contains((ushort)skillNumber), $"Starting skill {skillNumber} is missing from class {classNumber}'s UI roster.");
        Check(Client.Main.Core.Utilities.SkillDatabase.GetSkillDefinition(skillNumber)?.Damage == damage,
            $"Starting skill {skillNumber} does not match MuMain's base damage.");
    }
    InventoryItem DropItem(int group, int id, int level = 0, byte flags = 0, byte option = 0)
    {
        byte[] raw = flags == 0 ? [] : [(byte)(group << 4), (byte)id, (byte)level, 100, flags, option];
        return new InventoryItem(new ItemDefinition(id, "Drop test", 1, 1) { Group = group }, Point.Zero, raw, 100, level);
    }
    for (int group = 0; group < 12; group++)
        for (int level = 0; level <= 15; level++)
            Check((Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(group, 0, level)) != null) == (level >= 7),
                $"Incorrect normal equipment drop rule for group {group}, level {level}.");
    Check(Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(0, 0, 0, 8, 1)) != null, "Excellent item can be dropped.");
    Check(Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(0, 0, 0, 16, 1)) != null, "Ancient item can be dropped.");
    Check(Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(0, 0, 0, 32, 1)) != null, "Harmony item can be dropped.");
    foreach (var jewel in new[] { 13, 14, 16, 22, 31 })
        Check(Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(14, jewel)) != null, "Valuable jewel can be dropped.");
    Check(Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(12, 0)) != null, "Wings can be dropped.");
    Check(Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(13, 37)) != null, "Fenrir can be dropped.");
    Check(Client.Main.Core.Utilities.ItemDropPolicy.GetBlockReason(DropItem(14, 0, 15)) == null, "Upgrade cutoff incorrectly applies to potions.");
    var game = (MuGame)RuntimeHelpers.GetUninitializedObject(typeof(MuGame));
    var graphics = (GraphicsDeviceManager)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDeviceManager));
    graphics.PreferredBackBufferWidth = 1280; graphics.PreferredBackBufferHeight = 720;
    typeof(MuGame).GetField("_graphics", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, graphics);
    typeof(MuGame).GetProperty(nameof(MuGame.Instance))!.SetValue(null, game);
    var scene = new TestScene();
    var holdType = typeof(GameScene).Assembly.GetType("Client.Main.Scenes.PlayerMenuHoldGesture")!;
    var hold = Activator.CreateInstance(holdType, nonPublic: true)!;
    var updateHold = holdType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)!;
    bool Hold(ushort? target, bool down, bool wasDown, Point position, double seconds) =>
        (bool)updateHold.Invoke(hold, [target, down, wasDown, position, seconds])!;
    Check(!Hold(7, true, false, Point.Zero, 0.1), "A short player tap opened the menu.");
    Check(!Hold(7, true, true, new Point(3, 2), 0.8), "Player menu opened before one second.");
    Check(Hold(7, true, true, new Point(4, 2), 0.2), "One-second hold did not open the player menu.");
    Check(!Hold(7, true, true, Point.Zero, 2), "The same held touch reopened the menu.");
    Check(!Hold(7, false, true, Point.Zero, 1), "Releasing a hold opened the menu.");
    Check(!Hold(7, true, false, Point.Zero, 0.5) && !Hold(7, true, true, new Point(20, 0), 0.6),
        "Dragging away from a player did not cancel the hold.");
    Check(!Hold(7, true, true, Point.Zero, 2), "Returning a dragged touch restarted the hold without a fresh press.");
    Check(!Hold(7, true, false, Point.Zero, 0.5) && !Hold(8, true, true, Point.Zero, 0.6),
        "Changing players while holding opened a different player's menu.");
    Check(!Hold(null, true, false, Point.Zero, 2), "A hold without a player opened the menu.");
    var stepDirection = typeof(VirtualJoystickControl).GetMethod("GetStepDirection", BindingFlags.Static | BindingFlags.NonPublic)!;
    foreach (var direction in new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1),
        Vector2.Normalize(new Vector2(1, 1)), Vector2.Normalize(new Vector2(-1, 1)),
        Vector2.Normalize(new Vector2(1, -1)), Vector2.Normalize(new Vector2(-1, -1)) })
    {
        var step = (Vector2)stepDirection.Invoke(null, [direction])!;
        Check(step != Vector2.Zero && Math.Abs(step.X) <= 1 && Math.Abs(step.Y) <= 1,
            "Joystick projected farther than one neighboring tile.");
    }
    Check((Vector2)stepDirection.Invoke(null, [new Vector2(-1, 0)])! == new Vector2(-1, -1),
        "Dragging left no longer moves toward screen-left.");
    var joystickTarget = typeof(VirtualJoystickControl).GetMethod("GetMovementTarget", BindingFlags.Static | BindingFlags.NonPublic)!;
    Vector2 MovementTarget(Vector2 origin, Vector2 step, Func<Vector2, bool> walkable) =>
        (Vector2)joystickTarget.Invoke(null, [origin, step, walkable])!;
    Check(MovementTarget(new Vector2(100, 100), new Vector2(-1, -1), _ => true) == new Vector2(99, 99),
        "Joystick did not request exactly one tile toward the left.");
    Check(MovementTarget(new Vector2(100, 100), new Vector2(-1, -1), tile => tile.X != 99) == new Vector2(100, 100),
        "One-tile joystick movement skipped a blocked tile.");
    Check(MovementTarget(new Vector2(100, 100), new Vector2(-1, -1), tile => tile.X != 98) == new Vector2(99, 99),
        "Joystick did not stop at one tile when the second tile was blocked.");
    Check(MovementTarget(new Vector2(100, 100), new Vector2(-1, -1), tile => tile.X != 97) == new Vector2(99, 99),
        "Joystick requested more than one tile.");
    Check(MovementTarget(new Vector2(1, 1), new Vector2(-1, -1), _ => true) == Vector2.Zero,
        "Joystick projected outside the map boundary.");
    var walkerType = typeof(Client.Main.Objects.WalkerObject);
    var boundary = walkerType.GetMethod("NotifyMovementStepCompleted", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var movementEvent = walkerType.GetEvent("MovementStepCompleted", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var intentField = walkerType.GetField("_movementIntent", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var pathField = walkerType.GetField("_currentPath", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var walking = (TestWalker)RuntimeHelpers.GetUninitializedObject(typeof(TestWalker));
    int continuations = 0;
    Action<Client.Main.Objects.WalkerObject> nextTile = current =>
    {
        continuations++;
        intentField.SetValue(current, true);
        pathField.SetValue(current, new Queue<Vector2>([new Vector2(1, 0)]));
    };
    movementEvent.GetAddMethod(true)!.Invoke(walking, [nextTile]);
    boundary.Invoke(walking, [false]);
    Check(continuations == 0, "Idle walker issued a movement completion.");
    walking.MoveTargetPosition = new Vector3(1, 0, 0);
    boundary.Invoke(walking, [true]);
    Check(continuations == 0, "Movement continued before reaching its tile.");
    walking.MoveTargetPosition = Vector3.Zero;
    boundary.Invoke(walking, [true]);
    Check(continuations == 1 && walking.MovementIntent,
        "Held movement did not publish the next tile before idle animation selection.");
    boundary.Invoke(walking, [true]);
    Check(continuations == 1, "Movement completion duplicated a pending tile command.");
    intentField.SetValue(walking, false);
    pathField.SetValue(walking, null);
    movementEvent.GetRemoveMethod(true)!.Invoke(walking, [nextTile]);
    boundary.Invoke(walking, [true]);
    Check(continuations == 1, "Detached joystick still continued walking.");
    var updateMove = walkerType.GetMethod("UpdateMoveTargetPosition", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var cachedTarget = walkerType.GetField("_cachedTargetPosition", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var dirtyTarget = walkerType.GetField("_targetPositionDirty", BindingFlags.Instance | BindingFlags.NonPublic)!;
    cachedTarget.SetValue(walking, new Vector3(18050, 12950, 0));
    dirtyTarget.SetValue(walking, false);
    walking.MoveTargetPosition = new Vector3(18050, 12950, 0);
    pathField.SetValue(walking, new Queue<Vector2>());
    intentField.SetValue(walking, true);
    updateMove.Invoke(walking, [new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 120))]);
    Check(!walking.MovementIntent, "Reached tile left joystick movement permanently pending.");
    walking.MoveTargetPosition = new Vector3(18050.002f, 12950, 0);
    intentField.SetValue(walking, true);
    updateMove.Invoke(walking, [new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 120))]);
    Check(!walking.MovementIntent && walking.MoveTargetPosition == walking.TargetPosition,
        "Near-target rounding left joystick movement permanently pending.");
    pathField.SetValue(walking, null);
    intentField.SetValue(walking, true);
    updateMove.Invoke(walking, [new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 120))]);
    Check(walking.MovementIntent, "Idle update cancelled an initial asynchronous movement request.");
    walking.StopMovement();
    Check(!walking.MovementIntent, "Stopping movement left the next joystick command blocked.");
    var runWithoutEquipment = typeof(Client.Main.Objects.Player.PlayerObject).GetMethod("CanRunWithoutEquipment", BindingFlags.Static | BindingFlags.NonPublic)!;
    var equipmentState = new Client.Main.Core.Client.CharacterState(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
    var equipmentLevel = equipmentState.GetType().GetMethod("GetInventoryItemLevel", BindingFlags.Instance | BindingFlags.NonPublic)!;
    byte[] equippedBoots = [0xB0, 0, 5, 100, 0];
    equipmentState.AddOrUpdateInventoryItem(6, equippedBoots);
    Check((int)equipmentLevel.Invoke(equipmentState, [(byte)6])! == 5, "Running check did not read the equipped boots' upgrade level.");
    equipmentState.RemoveInventoryItem(6);
    Check((int)equipmentLevel.Invoke(equipmentState, [(byte)6])! == 0, "Unequipped boots left a stale running upgrade level.");
    foreach (var classNumber in Enum.GetValues<MUnique.OpenMU.Network.Packets.CharacterClassNumber>())
    {
        int baseClass = (int)classNumber & ~3;
        Check((bool)runWithoutEquipment.Invoke(null, [classNumber])! == (baseClass is 12 or 16 or 24),
            $"Incorrect equipment-free running eligibility for {classNumber}.");
    }
    intentField.SetValue(walking, true);
    pathField.SetValue(walking, new Queue<Vector2>([new Vector2(190, 159)]));
    var sendGeneration = walkerType.GetField("_walkSendGeneration", BindingFlags.Instance | BindingFlags.NonPublic)!;
    int oldSendGeneration = (int)sendGeneration.GetValue(walking)!;
    walkerType.GetMethod("ApplyServerPosition", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(walking, [new Vector2(184, 162)]);
    Check(walking.Location == new Vector2(184, 162) && !walking.MovementIntent,
        "Server correction did not replace the predicted position and clear movement intent.");
    Check(pathField.GetValue(walking) == null && (int)sendGeneration.GetValue(walking)! > oldSendGeneration,
        "Server correction left an old movement path or queued walk packet active.");
    Check((int)PlayerAction.PlayerHighShock == 141 && (int)PlayerAction.PlayerShock == 230,
        "Normal damage reaction must use MuMain PLAYER_SHOCK, not PLAYER_HIGH_SHOCK.");
    using var playerAnimationStream = typeof(GameScene).Assembly.GetManifestResourceStream("Client.Main.Data.S6.player.bmd")!;
    using var playerAnimationBuffer = new MemoryStream();
    playerAnimationStream.CopyTo(playerAnimationBuffer);
    var playerAnimationModel = new Client.Data.BMD.BMDReader().ReadFromBuffer(playerAnimationBuffer.ToArray());
    var buildMonsterBones = typeof(Client.Main.Objects.MonsterObject)
        .GetMethod("BuildBoneArray", BindingFlags.Static | BindingFlags.NonPublic)!;
    var buildMonsterActions = typeof(Client.Main.Objects.MonsterObject)
        .GetMethod("BuildActionArray", BindingFlags.Static | BindingFlags.NonPublic)!;
    foreach (var skeletonType in new[] {
        typeof(Client.Main.Objects.Monsters.SkeletonWarrior),
        typeof(Client.Main.Objects.Monsters.SkeletonArcher),
        typeof(Client.Main.Objects.Monsters.EliteSkeleton) })
    {
        var actionMap = (System.Collections.Generic.Dictionary<MonsterActionType, PlayerAction>)skeletonType
            .GetField("_actionMap", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check((int)actionMap[MonsterActionType.Shock] == 230,
            $"{skeletonType.Name} maps damage to the wrong Player.bmd animation.");
        Check(actionMap[MonsterActionType.Die] == PlayerAction.PlayerDie1,
            $"{skeletonType.Name} death mapping was changed by the damage fix.");
        var indices = actionMap.ToDictionary(p => (int)p.Key, p => (int)p.Value);
        var mappedActions = (Client.Data.BMD.BMDTextureAction[])buildMonsterActions.Invoke(null,
            [playerAnimationModel, Enum.GetValues<MonsterActionType>().Length, indices])!;
        var mappedBones = (Client.Data.BMD.BMDTextureBone[])buildMonsterBones.Invoke(null,
            [playerAnimationModel, Enum.GetValues<MonsterActionType>().Length, indices])!;
        Check(mappedActions[(int)MonsterActionType.Shock].NumAnimationKeys == playerAnimationModel.Actions[230].NumAnimationKeys,
            $"{skeletonType.Name} damage animation has the wrong frame count.");
        for (int boneIndex = 0; boneIndex < mappedBones.Length; boneIndex++)
        {
            if (ReferenceEquals(mappedBones[boneIndex], Client.Data.BMD.BMDTextureBone.Dummy)) continue;
            Check(ReferenceEquals(mappedBones[boneIndex].Matrixes[(int)MonsterActionType.Shock].Position,
                playerAnimationModel.Bones[boneIndex].Matrixes[230].Position),
                $"{skeletonType.Name} bone {boneIndex} does not use the normal damage pose.");
        }
    }
    Check(Client.Main.Core.Utilities.PlayerActionMapper.GetClientAction(
        (byte)ServerPlayerActionType.Damage1, MUnique.OpenMU.Network.Packets.CharacterClassNumber.DarkKnight)
        == PlayerAction.PlayerShock, "Server damage animation did not select normal player shock.");
    var monster = new TestMonster();
    var damageReaction = typeof(Client.Main.Objects.MonsterObject).GetMethod("PlayDamageReaction", BindingFlags.Instance | BindingFlags.NonPublic)!;
    damageReaction.Invoke(monster, [0u]);
    Check(monster.CurrentAction != (int)MonsterActionType.Shock, "A missed attack triggered a monster hit reaction.");
    damageReaction.Invoke(monster, [1u]);
    var animTime = walkerType.GetField("_animTime", BindingFlags.Instance | BindingFlags.NonPublic)!;
    animTime.SetValue(monster, Convert.ChangeType(0.25, animTime.FieldType));
    damageReaction.Invoke(monster, [1u]);
    Check(Convert.ToDouble(animTime.GetValue(monster)) == 0.25, "Repeated damage restarted the same monster hit animation.");
    monster.PlayAction((ushort)MonsterActionType.Attack1);
    damageReaction.Invoke(monster, [1u]);
    Check(monster.CurrentAction == (int)MonsterActionType.Attack1, "A normal monster attack was interrupted by a hit reaction.");
    var animationController = new Client.Main.Controllers.AnimationController(monster);
    monster.MoveTargetPosition = monster.TargetPosition + new Microsoft.Xna.Framework.Vector3(100f, 0f, 0f);
    foreach (var action in new[] { MonsterActionType.Shock, MonsterActionType.Attack1, MonsterActionType.Attack2 })
    {
        animationController.PlayAnimation((ushort)action, false);
        animationController.NotifyAnimationCompleted();
        Check(monster.CurrentAction == (int)MonsterActionType.Walk,
            $"Moving monster did not resume walking after {action}.");
    }
    monster.MoveTargetPosition = monster.TargetPosition;
    animationController.PlayAnimation((ushort)MonsterActionType.Shock, false);
    animationController.NotifyAnimationCompleted();
    Check(monster.CurrentAction == (int)MonsterActionType.Stop1,
        "Stationary monster did not return to idle after damage.");
    int monsterTypesChecked = 0;
    foreach (var type in typeof(Client.Main.Objects.MonsterObject).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.Namespace?.StartsWith("Client.Main.Objects.Monsters", StringComparison.Ordinal) == true && typeof(Client.Main.Objects.MonsterObject).IsAssignableFrom(t)))
    {
        var instance = (Client.Main.Objects.MonsterObject)Activator.CreateInstance(type)!;
        Check(instance.AnimationSpeed == 25f, $"{type.Name} does not use the original 25 Hz animation clock.");
        var controller = new Client.Main.Controllers.AnimationController(instance);
        foreach (var action in new[] { MonsterActionType.Shock, MonsterActionType.Attack1, MonsterActionType.Attack2, MonsterActionType.Appear })
        {
            instance.MoveTargetPosition = instance.TargetPosition + new Vector3(100f, 0f, 0f);
            controller.PlayAnimation((ushort)action, false);
            controller.NotifyAnimationCompleted();
            Check(instance.CurrentAction == (int)MonsterActionType.Walk, $"{type.Name} slides after {action}.");
            instance.MoveTargetPosition = instance.TargetPosition;
            controller.PlayAnimation((ushort)action, false);
            controller.NotifyAnimationCompleted();
            Check(instance.CurrentAction == (int)MonsterActionType.Stop1, $"{type.Name} keeps walking after {action} while stationary.");
        }
        var synchronize = walkerType.GetMethod("SynchronizeMonsterLocomotion", BindingFlags.Instance | BindingFlags.NonPublic)!;
        instance.MoveTargetPosition = instance.TargetPosition + new Vector3(100f, 0f, 0f);
        instance.PlayAction((ushort)MonsterActionType.Shock);
        synchronize.Invoke(instance, null);
        Check(instance.CurrentAction == (int)MonsterActionType.Walk, $"{type.Name} retained hit pose while moving.");
        Check(!instance.IsOneShotPlaying, $"{type.Name} retained the hit animation lock while walking.");
        instance.Position = instance.TargetPosition + new Vector3(12f, 7f, 0f);
        instance.PlayAction((ushort)MonsterActionType.Attack1, fromServer: true);
        Check(!instance.MovementIntent && !instance.IsMoving, $"{type.Name} moved during a server attack.");
        Check(instance.CurrentAction == (int)MonsterActionType.Attack1, $"{type.Name} lost its attack pose.");
        controller.PlayAnimation((ushort)MonsterActionType.Die, false);
        typeof(Client.Main.Objects.MonsterObject).GetField("_isFading", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, true);
        controller.NotifyAnimationCompleted();
        Check(instance.CurrentAction == (int)MonsterActionType.Die, $"{type.Name} resumed walking after death.");
        monsterTypesChecked++;
    }
    Console.WriteLine($"Checked animation transitions for {monsterTypesChecked} concrete monster types.");
    if (args.Contains("--monster-animation-only"))
    {
        Console.WriteLine($"PASS: {checks} checks, including moving and stationary monster animation recovery.");
        return;
    }
    var login = new Client.Main.Controls.UI.Login.LoginDialog();
    var back = (OkButton)typeof(Client.Main.Controls.UI.Login.LoginDialog)
        .GetField("_classicCancelButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(login)!;
    var ok = (OkButton)typeof(Client.Main.Controls.UI.Login.LoginDialog)
        .GetField("_okButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(login)!;
    Check(back.Visible && back.Interactive && back.TexturePath == "Interface/newui_button_cancel.OZT",
        "Login Back button is unavailable or uses the wrong texture.");
    Check(back.Y == ok.Y && back.X >= ok.X + ok.ViewSize.X && back.Y + back.ViewSize.Y <= login.ControlSize.Y,
        "Login Back overlaps OK or falls outside the dialog.");
    int backRequests = 0;
    login.CancelAttempt += (_, _) => backRequests++;
    var cancel = typeof(Client.Main.Controls.UI.Login.LoginDialog)
        .GetMethod("CancelButton_Click", BindingFlags.Instance | BindingFlags.NonPublic)!;
    login.CanGoBack = false;
    cancel.Invoke(login, [back, EventArgs.Empty]);
    Check(backRequests == 0, "Disabled Back allowed navigation during a connection or login.");
    login.CanGoBack = true;
    cancel.Invoke(login, [back, EventArgs.Empty]);
    Check(backRequests == 1, "Back did not request return to server selection.");
    scene.Controls.Add(login);
    var loginError = (MessageWindow)Activator.CreateInstance(typeof(MessageWindow), nonPublic: true)!;
    scene.Controls.Add(loginError);
    var raiseLogin = typeof(Client.Main.Controls.UI.Login.LoginDialog)
        .GetMethod("BringToFrontBelowMessages", BindingFlags.Instance | BindingFlags.NonPublic)!;
    raiseLogin.Invoke(login, null);
    raiseLogin.Invoke(login, null); // Repeated queued state updates after a failed login.
    Check(scene.Controls.Last() == loginError, "Connection-state updates covered the login error.");
    loginError.Close();
    raiseLogin.Invoke(login, null);
    Check(scene.Controls.Last() == login, "Closing the error did not restore the login form.");
    scene.Controls.Remove(login);
    var underneath = new TestControl();
    var window = new TestControl();
    var child = new TestControl { X = 10, Y = 10, ControlSize = new Point(20, 20), ViewSize = new Point(20, 20) };
    scene.Controls.Add(underneath); scene.Controls.Add(window); window.Controls.Add(child);
    MouseState previous = default;
    void Frame(int x, int y, ButtonState button, int scroll = 0)
    {
        var current = new MouseState(x, y, scroll, button, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released);
        typeof(MuGame).GetProperty(nameof(MuGame.PrevUiMouseState))!.SetValue(game, previous);
        typeof(MuGame).GetProperty(nameof(MuGame.UiMouseState))!.SetValue(game, current);
        game.Mouse = current;
        scene.Update(new GameTime()); previous = current;
    }
    Frame(15, 15, ButtonState.Pressed);
    Check(underneath.RawPresses == 0 && !underneath.IsMouseOver, "Covered control received the press/hover.");
    Check(child.RawPresses == 1 && window.RawPresses == 1, "Topmost child or its container lost input.");
    Frame(15, 15, ButtonState.Released);
    Check(underneath.Clicks == 0 && child.Clicks == 1, "Click passed through overlapping controls.");
    Frame(50, 50, ButtonState.Released, 120);
    Check(underneath.RawScrolls == 0, "Scroll passed through the window.");
    Frame(50, 50, ButtonState.Pressed, 120);
    Frame(150, 150, ButtonState.Pressed, 120);
    Check(window.LastPosition == new Point(150, 150), "Drag owner lost input outside its bounds.");
    Frame(150, 150, ButtonState.Released, 120);
    Check(window.RawReleases == 2 && underneath.RawReleases == 0, "Drag release went to another control.");
    window.Visible = false;
    Frame(50, 50, ButtonState.Pressed, 120);
    Frame(50, 50, ButtonState.Released, 120);
    Check(underneath.RawPresses == 1 && underneath.Clicks == 1, "Hiding the overlay did not restore input.");
    window.Visible = true; window.Interactive = false; window.CapturePointerWhenNonInteractive = true;
    Frame(50, 50, ButtonState.Pressed, 120);
    Frame(50, 50, ButtonState.Released, 120);
    Check(underneath.RawPresses == 1 && underneath.Clicks == 1, "Non-interactive blocking overlay leaked input.");

    // Exercise the exact selection gate used by inventory/equipment, without assets.
    var inventory = (InventoryControl)RuntimeHelpers.GetUninitializedObject(typeof(InventoryControl));
    var select = typeof(InventoryControl).GetMethod("SelectBeforePickup", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var selected = typeof(InventoryControl).GetField("_mobileSelectedItem", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var first = (InventoryItem)RuntimeHelpers.GetUninitializedObject(typeof(InventoryItem));
    var second = (InventoryItem)RuntimeHelpers.GetUninitializedObject(typeof(InventoryItem));
    bool Tap(InventoryItem item, int slot, bool mobile) => (bool)select.Invoke(inventory, [item, slot, mobile])!;
    Check(Tap(first, -1, true) && ReferenceEquals(selected.GetValue(inventory), first), "First mobile tap did not select.");
    Check(Tap(second, -1, true) && ReferenceEquals(selected.GetValue(inventory), second), "Another item did not replace selection.");
    Check(!Tap(second, -1, true) && selected.GetValue(inventory) == null, "Second mobile tap did not enable pickup/clear selection.");
    Check(Tap(first, 3, true) && !Tap(first, 3, true), "Equipment tap selection differs from inventory.");
    Check(!Tap(first, -1, false) && selected.GetValue(inventory) == null, "Desktop pickup now requires a selection tap.");
    // A dialog must never operate on a replacement item or a moved inventory slot.
    var actionItem = new InventoryItem(new ItemDefinition(1, "Potion", 1, 1) { Group = 14 }, new Point(2, 3));
    var currentItems = new List<InventoryItem> { actionItem };
    typeof(InventoryControl).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, currentItems);
    var isCurrent = typeof(InventoryControl).GetMethod("IsCurrentItem", BindingFlags.Instance | BindingFlags.NonPublic)!;
    bool Current(Point original) => (bool)isCurrent.Invoke(inventory, [actionItem, -1, original])!;
    Check(Current(new Point(2, 3)), "Selected item cannot be used from its original slot.");
    actionItem.GridPosition = new Point(3, 3);
    Check(!Current(new Point(2, 3)), "Mobile dialog acts on an item moved after opening.");
    currentItems.Clear();
    Check(!Current(new Point(3, 3)), "Mobile dialog acts on an item removed after opening.");
    var actionDialog = (RequestDialog)Activator.CreateInstance(typeof(RequestDialog), nonPublic: true)!;
    var extraButton = (ButtonControl)typeof(RequestDialog).GetField("_extraButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actionDialog)!;
    var acceptButton = (ButtonControl)typeof(RequestDialog).GetField("_acceptButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actionDialog)!;
    var cancelButton = (ButtonControl)typeof(RequestDialog).GetField("_rejectButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actionDialog)!;
    extraButton.Visible = true;
    typeof(RequestDialog).GetMethod("AdjustSizeAndLayout", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(actionDialog, null);
    Check(acceptButton.X + acceptButton.ViewSize.X < extraButton.X && extraButton.X + extraButton.ViewSize.X < cancelButton.X,
        "Use / Drop / Cancel buttons overlap.");
    Check(acceptButton.X >= 0 && cancelButton.X + cancelButton.ViewSize.X <= actionDialog.ViewSize.X,
        "Mobile action buttons exceed the dialog bounds.");
    actionDialog.Dispose();
    var mobileMenu = new TouchMenuControl();
    var menuRects = (Rectangle[])typeof(TouchMenuControl).GetField("_rects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mobileMenu)!;
    Check(menuRects.Length == 15 && menuRects.Distinct().Count() == 15, "Mobile menu grid overlaps or omits entries.");
    foreach (var rect in menuRects)
        Check(mobileMenu.ContainsPointerPoint(rect.Center), "Visible mobile icon does not capture input.");
    Check(!mobileMenu.ContainsPointerPoint(new Point(640, 300)), "Mobile shortcut bounds block the world between icons.");
    var expandedIconCenter = menuRects[0].Center;
    mobileMenu.Close();
    var progressField = typeof(TouchMenuControl).GetField("_menuProgress", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var layoutMenu = typeof(TouchMenuControl).GetMethod("LayoutRects", BindingFlags.Instance | BindingFlags.NonPublic)!;
    Check(mobileMenu.ContainsPointerPoint(menuRects[0].Center), "Closing animation lets touches pass through visible icons.");
    progressField.SetValue(mobileMenu, 0f);
    layoutMenu.Invoke(mobileMenu, null);
    Check(!mobileMenu.ContainsPointerPoint(expandedIconCenter), "Collapsed grid still captures clicks.");
    var mapRect = (Rectangle)typeof(TouchMenuControl).GetField("_mapRect", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mobileMenu)!;
    var chatRect = (Rectangle)typeof(TouchMenuControl).GetField("_chatRect", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mobileMenu)!;
    Check(mobileMenu.ContainsPointerPoint(mapRect.Center) && mobileMenu.ContainsPointerPoint(chatRect.Center), "Map/chat shortcuts disappear when the menu collapses.");
    var advance = typeof(TouchMenuControl).GetMethod("Advance", BindingFlags.Static | BindingFlags.NonPublic)!;
    float Advance(float progress, bool opening, float seconds) => (float)advance.Invoke(null, [progress, opening, seconds])!;
    Check(Math.Abs(Advance(0f, true, .12f) - .5f) < .001f, "Menu transition duration depends on frame rate.");
    Check(Advance(.5f, false, .12f) == 0f && Advance(.5f, true, .5f) == 1f, "Menu reversal or clamping failed.");
    typeof(TouchMenuControl).GetField("_chatProgress", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(mobileMenu, 1f);
    layoutMenu.Invoke(mobileMenu, null);
    var openChatRect = (Rectangle)typeof(TouchMenuControl).GetField("_chatRect", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mobileMenu)!;
    Check(openChatRect.X == chatRect.X - 56 && openChatRect.Y == chatRect.Y, "Chat icon does not slide left beside the bottom chat input.");
    float previousScale = Constants.RENDER_SCALE;
    Constants.RENDER_SCALE = .75f;
    Client.Main.Controllers.UiScaler.Configure(1920, 1080, 1280, 720);
    var nativeUi = typeof(Client.Main.Controllers.UiScaler).GetProperty("DrawingAtNativeResolution", BindingFlags.Static | BindingFlags.NonPublic)!;
    Check(Math.Abs(Client.Main.Controllers.UiScaler.SpriteTransform.M11 - 1.125f) < .001f, "World UI transform lost render scaling.");
    nativeUi.SetValue(null, true);
    Check(Math.Abs(Client.Main.Controllers.UiScaler.SpriteTransform.M11 - 1.5f) < .001f, "Native HUD still uses reduced world resolution.");
    var actualPoint = Client.Main.Controllers.UiScaler.ToActual(new Point(100, 100));
    Check(Client.Main.Controllers.UiScaler.ToVirtual(actualPoint) == new Point(100, 100), "Native HUD changed touch coordinates.");
    nativeUi.SetValue(null, false);
    Constants.RENDER_SCALE = previousScale;
    Client.Main.Controllers.UiScaler.Configure(1280, 720, 1280, 720);

    string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
    string assets = Path.Combine(root, "data", "patch", "files-e.g", "Interface", "DH");
    var reader = new OZPReader();
    foreach (var path in Directory.GetFiles(assets, "mobile_*.png"))
    {
        var plain = await reader.Load(path);
        var prepared = await TextureLoader.Instance.Prepare(path);
        Check(prepared != null && prepared.Data.SequenceEqual(plain.Data), "Texture loader failed to locate PNG: " + path);
        var bundled = await TextureLoader.Instance.Prepare("Interface/DH/" + Path.GetFileName(path));
        Check(bundled != null && bundled.Data.SequenceEqual(plain.Data), "Relative/bundled PNG lookup failed: " + path);
        byte[] png = File.ReadAllBytes(path);
        byte[] ozp = new byte[png.Length + 4];
        Array.Copy(png, ozp, 4); Array.Copy(png, 0, ozp, 4, png.Length);
        using var stream = new MemoryStream(ozp);
        var wrapped = await reader.Load(stream);
        Check(plain.Width > 0 && plain.Height > 0 && plain.Data.SequenceEqual(wrapped.Data), "PNG/OZP decoding differs: " + path);
    }
    var legacy = await reader.Load(Path.Combine(assets, "mi_btn_bag.OZP"));
    Check(legacy.Width > 0 && legacy.Data.Length > 0, "Existing OZP icons no longer load.");
    scene.Controls.Clear();
    var joystick = new VirtualJoystickControl();
    typeof(Client.Main.Controls.GameControl).GetProperty("Status")!.SetValue(joystick, GameControlStatus.Ready);
    scene.Controls.Add(joystick);
    // Exercise the real joystick through scene hit testing and scoped pointer dispatch.
    var center = joystick.DisplayRectangle.Center;
    Frame(center.X, center.Y, ButtonState.Released);
    Frame(center.X, center.Y, ButtonState.Pressed);
    Frame(center.X + 50, center.Y, ButtonState.Pressed);
    var knobField = typeof(VirtualJoystickControl).GetField("_knob", BindingFlags.NonPublic | BindingFlags.Instance)!;
    Check(((Vector2)knobField.GetValue(joystick)!).X > 40, "Actual joystick did not move its knob on drag.");
    Frame(center.X + 200, center.Y, ButtonState.Pressed);
    Check(Math.Abs(((Vector2)knobField.GetValue(joystick)!).Length() - 87f) < .01f, "Joystick lost drag capture outside its bounds.");
    Frame(center.X + 200, center.Y, ButtonState.Released);
    Check((Vector2)knobField.GetValue(joystick)! == Vector2.Zero, "Joystick did not recenter on release.");
    UiThemeManager.SetTheme(UiThemeId.Classic, persist: false);
    var chatOverlay = new ChatLogWindow();
    var anchorAbove = typeof(ChatLogWindow).GetMethod("AnchorAbove", BindingFlags.Instance | BindingFlags.NonPublic)!;
    anchorAbove.Invoke(chatOverlay, [460, 552]);
    Check(chatOverlay.X == 460 && chatOverlay.Y + chatOverlay.ViewSize.Y == 552, "Chat log is not above its toolbar.");
    chatOverlay.SetShowingLines(15);
    Check(chatOverlay.Y + chatOverlay.ViewSize.Y == 552, "Growing the chat log pushed messages below the toolbar.");
    chatOverlay.SetShowingLines(3);
    Check(chatOverlay.Y + chatOverlay.ViewSize.Y == 552, "Shrinking the chat log moved its bottom anchor.");
    var mipPolicy = typeof(TextureLoader).GetMethod("ShouldGenerateMipMaps", BindingFlags.Static | BindingFlags.NonPublic)!;
    bool oldTextureQuality = Constants.HIGH_QUALITY_TEXTURES;
    Constants.HIGH_QUALITY_TEXTURES = false;
    var largeIcon = new TextureData { Width = 256, Height = 256, Components = 4, Data = new byte[256 * 256 * 4] };
    Check((bool)mipPolicy.Invoke(null, ["Interface/DH/Character.png", largeIcon])!, "Large mobile PNG has no downsampling filter on Low quality.");
    Check(!(bool)mipPolicy.Invoke(null, ["Interface/other.png", largeIcon])!, "Icon filtering changed unrelated UI textures.");
    var prepareMips = typeof(TextureLoader).GetMethod("PrepareMipData", BindingFlags.Static | BindingFlags.NonPublic)!;
    prepareMips.Invoke(null, [largeIcon, "Interface/DH/Character.png"]);
    Check(largeIcon.MipData.Length == 9 && largeIcon.MipData[1].Length == 128 * 128 * 4 && largeIcon.MipData[8].Length == 4,
        "Large mobile icon did not generate a complete downsampling chain.");
    Constants.HIGH_QUALITY_TEXTURES = oldTextureQuality;
    Check(!chatOverlay.Interactive, "Unframed mobile chat steals joystick touches.");
    chatOverlay.ShowFrame(true);
    Check(chatOverlay.Interactive, "Explicitly opened chat frame lost its interactions.");
    chatOverlay.ShowFrame(false);
    Check(!chatOverlay.Interactive, "Closing the mobile chat frame still blocks the joystick.");
    chatOverlay.AutoViewSize = false;
    chatOverlay.X = center.X - 100; chatOverlay.Y = center.Y - 100;
    chatOverlay.ControlSize = chatOverlay.ViewSize = new Point(300, 300);
    typeof(Client.Main.Controls.GameControl).GetProperty("Status")!.SetValue(chatOverlay, GameControlStatus.Ready);
    scene.Controls.Add(chatOverlay); // Chat is above the joystick, as in GameScene.
    Frame(center.X, center.Y, ButtonState.Pressed);
    Frame(center.X + 50, center.Y, ButtonState.Pressed);
    Check(ReferenceEquals(scene.MouseHoverControl, joystick) && ((Vector2)knobField.GetValue(joystick)!).X > 40,
        "Chat overlay prevented the real joystick from receiving its drag.");
    Frame(center.X + 50, center.Y, ButtonState.Released);
    scene.Controls.Clear();
    var heldMenu = new Client.Main.Controls.UI.Game.PlayerContextMenu { Visible = true };
    typeof(Client.Main.Controls.GameControl).GetProperty("Status")!.SetValue(heldMenu, GameControlStatus.Ready);
    var heldMenuAction = new TestControl();
    heldMenu.Controls.Add(heldMenuAction);
    scene.Controls.Add(heldMenu);
    var setMenuInput = heldMenu.GetType().GetMethod("SetPointerInteraction", BindingFlags.Instance | BindingFlags.NonPublic)!;
    setMenuInput.Invoke(heldMenu, [false]);
    Frame(50, 50, ButtonState.Pressed);
    Frame(50, 50, ButtonState.Released);
    Check(heldMenuAction.Clicks == 0, "Releasing the menu-opening hold selected a player action.");
    setMenuInput.Invoke(heldMenu, [true]);
    Frame(50, 50, ButtonState.Pressed);
    Frame(50, 50, ButtonState.Released);
    Check(heldMenuAction.Clicks == 1, "Player menu actions did not accept a new tap after the hold.");
    Console.WriteLine($"PASS: {checks} UI routing, mobile selection, menu layout, joystick drag and PNG/OZP checks.");
}
catch (Exception exception) { Console.Error.WriteLine(exception); Environment.Exit(1); }

sealed class TestScene : BaseScene
{
    public TestScene() { Controls.Clear(); Status = GameControlStatus.Ready; }
}
sealed class TestWalker : Client.Main.Objects.WalkerObject { }
sealed class TestMonster : Client.Main.Objects.MonsterObject { }
sealed class TestControl : UIControl
{
    public int RawPresses, RawReleases, RawScrolls, Clicks;
    public Point LastPosition;
    public TestControl()
    {
        Interactive = true; AutoViewSize = false;
        ControlSize = ViewSize = new Point(100, 100); Status = GameControlStatus.Ready;
    }
    public override void Update(GameTime time)
    {
        base.Update(time);
        var now = MuGame.Instance.UiMouseState; var before = MuGame.Instance.PrevUiMouseState;
        if (now.LeftButton == ButtonState.Pressed && before.LeftButton == ButtonState.Released) RawPresses++;
        if (now.LeftButton == ButtonState.Released && before.LeftButton == ButtonState.Pressed) RawReleases++;
        if (now.ScrollWheelValue != before.ScrollWheelValue) RawScrolls++;
        LastPosition = now.Position;
    }
    public override bool OnClick() { Clicks++; return true; }
}
