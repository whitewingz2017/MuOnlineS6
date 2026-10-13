using System.Reflection;
using System.Runtime.CompilerServices;
using Client.Main;
using Client.Main.Controls;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Client.Main.Networking.PacketHandling.Handlers;
using Client.Main.Scenes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xna.Framework;
using MUnique.OpenMU.Network.Packets;
using MUnique.OpenMU.Network.Packets.ServerToClient;

try
{
int assertions = 0;
void Check(bool result, string message)
{
    if (!result) throw new InvalidOperationException(message);
    assertions++;
}
byte[] loginPacket = new byte[92];
loginPacket[0] = 0xC3; loginPacket[1] = 92; loginPacket[2] = 0xF3; loginPacket[3] = 3;
System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(loginPacket.AsSpan(44), 8032);
System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(loginPacket.AsSpan(68), 55000412);
Check(Client.Main.Core.Utilities.CharacterInformationPacketReader.TryReadExtended(loginPacket, out var loginInfo) && loginInfo.Money == 55000412 && loginInfo.CurrentMana == 8032, "Read OpenMU's 92-byte login packet as extended: Zen must not come from the mana field.");
Check(loginPacket[1] == 92, "Reading an incoming character packet must not rewrite its header.");
byte[] longerLogin = new byte[96]; loginPacket.CopyTo(longerLogin, 0); longerLogin[1] = 96;
Check(Client.Main.Core.Utilities.CharacterInformationPacketReader.TryReadExtended(longerLogin, out loginInfo) && loginInfo.Money == 55000412, "Continue accepting the package's 96-byte extended login variant.");
Check(!Client.Main.Core.Utilities.CharacterInformationPacketReader.TryReadExtended(new byte[CharacterInformation.Length], out _), "Keep standard character packets on the legacy decoder.");

// No window or GraphicsDevice: exercise the real movement, registration and
// frustum code, with only content loading and the application shell omitted.
var game = (MuGame)RuntimeHelpers.GetUninitializedObject(typeof(MuGame));
var graphics = (GraphicsDeviceManager)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDeviceManager));
graphics.PreferredBackBufferWidth = 1280;
graphics.PreferredBackBufferHeight = 720;
typeof(MuGame).GetField("_graphics", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, graphics);
typeof(MuGame).GetProperty(nameof(MuGame.Instance))!.SetValue(null, game);
var world = new TestWorld();
var scene = new TestScene(world);
typeof(MuGame).GetProperty(nameof(MuGame.ActiveScene))!.SetValue(game, scene);
Constants.ENABLE_CROWD_SPATIAL_CULLING = true;

void InvokeWorld(string name, params object[] arguments) =>
    typeof(WorldControl).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(world, arguments);
bool InSnapshot(WalkerObject player) => (bool)typeof(WorldControl)
    .GetMethod("IsObjectVisibleInSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(world, [player])!;
int Sector(PlayerObject player)
{
    var sectors = (Dictionary<WorldObject, int>)typeof(WorldControl)
        .GetField("_spatialObjectSectors", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(world)!;
    return sectors[player];
}
void Recull()
{
    InvokeWorld("FlushSpatialUpdates");
    InvokeWorld("RebuildVisibleObjects");
}
void CameraAt(Vector2 tile, float aspect)
{
    var target = new Vector3((tile.X + .5f) * Constants.TERRAIN_SCALE, (tile.Y + .5f) * Constants.TERRAIN_SCALE, 0);
    float pitch = Constants.DEFAULT_CAMERA_PITCH, yaw = Constants.DEFAULT_CAMERA_YAW;
    var camera = Camera.Instance;
    camera.AspectRatio = aspect;
    camera.FOV = 35;
    camera.ViewFar = 1800;
    camera.Position = target + Constants.DEFAULT_CAMERA_DISTANCE * new Vector3(
        MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Cos(pitch) * MathF.Cos(yaw), MathF.Sin(pitch));
    camera.Target = target;
}
PlayerObject Spawn(ushort id, Vector2 tile)
{
    var player = new PlayerObject { NetworkId = id, CharacterClass = CharacterClassNumber.BladeKnight, Location = tile, World = world };
    typeof(WorldObject).GetProperty(nameof(WorldObject.Status))!.SetValue(player, GameControlStatus.Ready);
    player.MoveTargetPosition = player.TargetPosition;
    player.Position = player.TargetPosition;
    world.Objects.Add(player);
    return player;
}

// Expected model actions are taken from MuMain SkillCast.cpp, WSclient.cpp and MonkSystem.cpp.
// Exercise the public cast resolver, not just its data table: server echoes and remote casts use it too.
var skillCaster = Spawn(0x7F0, new Vector2(127, 115));
var expectedSkillActions = new Dictionary<ushort, int>
{
    [18]=186, [19]=60, [20]=61, [21]=62, [22]=63, [23]=64,
    [6]=151, [10]=154, [12]=152, [14]=153, [40]=73, [58]=72,
    [41]=65, [42]=66, [43]=71, [44]=137, [47]=70, [48]=67, [49]=68,
    [55]=65, [56]=145, [57]=136, [60]=80, [61]=80, [62]=87, [63]=81,
    [64]=146, [65]=152, [66]=80, [67]=67, [68]=139, [69]=67, [70]=67,
    [71]=139, [72]=67, [73]=138, [74]=80, [75]=146, [78]=80,
    [214]=168, [215]=160, [216]=164, [217]=156, [218]=156, [219]=156,
    [220]=156, [221]=156, [222]=156, [223]=172, [224]=172, [225]=172,
    [230]=185, [232]=176, [233]=177, [234]=246, [236]=184, [237]=183, [238]=80,
    [260]=247, [261]=248, [262]=249, [263]=250, [264]=253, [265]=252, [269]=137, [270]=254
};
foreach (var (skill, action) in expectedSkillActions)
    Check((int)skillCaster.GetSkillAction(skill, false) == action, $"Skill {skill} must use MuMain action {action}.");
foreach (var master in Client.Main.Core.Utilities.ServerMasterSkills.All.Where(x => x.Replaced != 0))
{
    var baseId = Client.Main.Objects.Effects.Skills.SkillVisualEffectRegistry.GetBaseSkillId(master.Number);
    if (expectedSkillActions.TryGetValue(baseId, out int expectedAction))
        Check((int)skillCaster.GetSkillAction(master.Number, false) == expectedAction,
            $"Master skill {master.Number} must inherit base {baseId}'s action {expectedAction}.");
    Check(Client.Main.Objects.Effects.Skills.SkillVisualEffectRegistry.HasEffect(master.Number) ==
        Client.Main.Objects.Effects.Skills.SkillVisualEffectRegistry.HasEffect(baseId),
        $"Master skill {master.Number} must inherit base {baseId}'s visual coverage.");
}
Check(skillCaster.GetSkillAction(13, false) is PlayerAction.PlayerSkillHand1 or PlayerAction.PlayerSkillHand2,
    "Cometfall must use normal casting, not the Hellfire animation.");
foreach (ushort skill in new ushort[] { 19,20,21,22,23 })
    Check(Client.Main.Objects.Effects.Skills.SkillVisualEffectRegistry.HasEffect(skill), $"Weapon skill {skill} must have its MuMain cast flash.");
Check(!Client.Main.Core.Utilities.WeaponSkillVisualRules.ShouldEmitTrail(PlayerAction.PlayerWalkMale, 0, true, 3), "Walking must not emit a combat weapon trail.");
Check(Client.Main.Core.Utilities.WeaponSkillVisualRules.ShouldEmitTrail(PlayerAction.PlayerAttackSkillSword2, 0, true, 3), "Lunge must emit a right-weapon trail.");
Check(!Client.Main.Core.Utilities.WeaponSkillVisualRules.ShouldEmitTrail(PlayerAction.PlayerAttackSkillSword2, 0, false, 3), "Lunge must not duplicate the right-weapon trail on the left hand.");
Check(!Client.Main.Core.Utilities.WeaponSkillVisualRules.ShouldEmitTrail(PlayerAction.PlayerSkillBlowOfDestruction, 0, true, 1) &&
    Client.Main.Core.Utilities.WeaponSkillVisualRules.ShouldEmitTrail(PlayerAction.PlayerSkillBlowOfDestruction, 0, true, 4), "Destruction trails must respect MuMain's active frames.");
var ridingField = typeof(PlayerObject).GetField("_isRiding", BindingFlags.NonPublic | BindingFlags.Instance)!;
var vehicleField = typeof(PlayerObject).GetField("_currentVehicleIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
ridingField.SetValue(skillCaster, true);
foreach (short vehicle in new short[] { 7, 8, 11 })
{
    vehicleField.SetValue(skillCaster, vehicle);
    int offset = vehicle == 7 ? 1 : vehicle == 8 ? 2 : 3;
    foreach (ushort skill in new ushort[] { 214,215,216,218,219,221,222,223,224,225 })
    {
        Check((int)skillCaster.GetSkillAction(skill, false) == expectedSkillActions[skill] + offset,
            $"Summoner skill {skill} must select mount variant {offset}.");
        Check((int)skillCaster.GetSkillAction(skill, true) == expectedSkillActions[skill],
            $"Summoner skill {skill} must use grounded action in a safe zone.");
    }
}
Check(skillCaster.GetSkillAction(329, false) == PlayerAction.PlayerFenrirAttackMagic, "Master Lunge must respect the Fenrir casting variant.");
ridingField.SetValue(skillCaster, false);
var remoteSkillMethod = typeof(CharacterDataHandler).GetMethod("SpawnRemoteRegisteredSkill", BindingFlags.NonPublic | BindingFlags.Static)!;
remoteSkillMethod.Invoke(null, [world, (ushort)0x7F0, (ushort)13, (ushort)0, null, null]);
Check((PlayerAction)skillCaster.CurrentAction is PlayerAction.PlayerSkillHand1 or PlayerAction.PlayerSkillHand2,
    "Remote skills without an effect factory must still animate.");
remoteSkillMethod.Invoke(null, [world, (ushort)0x7F0, (ushort)20, (ushort)0, null, null]);
Check(skillCaster.CurrentAction == PlayerAction.PlayerAttackSkillSword2,
    "Remote Lunge packets must select the thrust animation.");
foreach (var effect in world.Objects.OfType<Client.Main.Objects.Effects.WeaponSkillFlashEffect>().ToArray())
{
    world.RemoveObject(effect);
    effect.Dispose();
}
world.RemoveObject(skillCaster);

var pc = Spawn(0x123, new Vector2(131, 116));
foreach (float aspect in new[] { 16f / 9f, 20f / 9f })
{
    CameraAt(new Vector2(127, 115), aspect);
    Recull();
    Check(InSnapshot(pc), $"Nearby PC Blade Knight must pass the camera frustum at aspect {aspect}.");
    Check(world.FindPlayerById(0x123) == pc, "Player lookup must resolve the registered object.");
    Check(Vector3.Distance(pc.WorldPosition.Translation, new Vector3(13150, 11650, 0)) < .01f, "Spawn world coordinates must match the packet tile.");
}

// The old location is deliberately in a different spatial sector and outside
// the current view. Receiving a walk path alone cannot refresh this position.
world.RemoveObject(pc);
pc = Spawn(0x123, new Vector2(100, 115));
CameraAt(new Vector2(127, 115), 20f / 9f);
Recull();
Check(!InSnapshot(pc), "The remote player must begin outside the view.");
int oldSector = Sector(pc);
pc.MoveTo(new Vector2(131, 116), sendToServer: false, usePathfinding: false);
Recull();
Check(!InSnapshot(pc), "A queued walk must reproduce the stale-position precondition.");
for (int frame = 0; frame < 120; frame++)
{
    // This is the old world simulation path: only the culled render list updates.
    InvokeWorld("UpdateVisibleObjects", new GameTime(TimeSpan.FromSeconds(frame / 60d), TimeSpan.FromSeconds(1d / 60d)));
    Recull();
}
Check(!InSnapshot(pc) && pc.Position.X == 10050,
    "The old visible-only update must reproduce the invisible player stuck at its old location.");
var movementPump = typeof(WorldControl).GetMethod("UpdateCulledWalkerMovement", BindingFlags.NonPublic | BindingFlags.Instance)!;
for (int frame = 0; frame < 1500 && !InSnapshot(pc); frame++)
{
    movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(frame / 60d), TimeSpan.FromSeconds(1d / 60d))]);
    Recull();
}
Check(InSnapshot(pc), "Culled movement must bring the player back into the view without moving the camera.");
Check(pc.WorldPosition.Translation.X > 10050, "Rendered position must advance from the old tile.");
Vector3 beforeVisiblePump = pc.Position;
movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(.1))]);
Check(pc.Position == beforeVisiblePump, "Visible players must not advance again in the culled movement pass.");

// Finish the same path while culled, then pan away and back repeatedly.
CameraAt(new Vector2(80, 80), 20f / 9f);
Recull();
for (int frame = 0; frame < 1500 && (pc.IsMoving || pc.MovementIntent); frame++)
    movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(31 + frame / 60d), TimeSpan.FromSeconds(1d / 60d))]);
Check(pc.Location == new Vector2(131, 116), "The full remote path must complete while culled.");
Check(Vector3.Distance(pc.Position, new Vector3(13150, 11650, 0)) < .01f, "The final rendered position must equal the current tile.");
Recull();
Check(Sector(pc) != oldSector, "Spatial registration must leave the old sector.");
for (int repeat = 0; repeat < 5; repeat++)
{
    CameraAt(new Vector2(80, 80), 20f / 9f); Recull();
    Check(!InSnapshot(pc), "Leaving the POV must continue to cull the player.");
    CameraAt(new Vector2(127, 115), 20f / 9f); Recull();
    Check(InSnapshot(pc), "Returning to the POV must restore the player at its current location.");
}

// Hidden/loading roots must retain lifecycle isolation, even with a pending path.
CameraAt(new Vector2(80, 80), 20f / 9f); Recull();
pc.MoveTo(new Vector2(132, 116), sendToServer: false, usePathfinding: false);
pc.Hidden = true;
Vector3 hiddenPosition = pc.Position;
movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1))]);
Check(pc.Position == hiddenPosition && pc.Hidden, "Hidden roots must not be moved or activated by the culling fix.");
world.RemoveObject(pc);
Check(world.FindPlayerById(0x123) == null, "Removal must still unregister the remote player.");

// Monsters crossing the culling boundary must follow their network path too;
// otherwise they can attack on the server while remaining invisible on the client.
var approachingMonster = new TestMonster { NetworkId = 0x345, World = world, Location = new Vector2(100, 115) };
typeof(WorldObject).GetProperty(nameof(WorldObject.Status))!.SetValue(approachingMonster, GameControlStatus.Ready);
approachingMonster.MoveTargetPosition = approachingMonster.TargetPosition;
approachingMonster.Position = approachingMonster.TargetPosition;
world.Objects.Add(approachingMonster);
CameraAt(new Vector2(127, 115), 20f / 9f);
Recull();
Check(!InSnapshot(approachingMonster), "Approaching monster must start outside the view.");
approachingMonster.MoveTo(new Vector2(131, 116), sendToServer: false, usePathfinding: false);
for (int frame = 0; frame < 1500 && !InSnapshot(approachingMonster); frame++)
{
    movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(65 + frame / 60d), TimeSpan.FromSeconds(1d / 60d))]);
    Recull();
}
Check(InSnapshot(approachingMonster), "A monster walking toward the hero must enter view without a camera movement.");
Vector3 monsterVisiblePosition = approachingMonster.Position;
movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(95), TimeSpan.FromSeconds(.1))]);
Check(approachingMonster.Position == monsterVisiblePosition, "Visible monsters must not advance twice.");
approachingMonster.Hidden = true;
movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(96), TimeSpan.FromSeconds(1))]);
Check(approachingMonster.Position == monsterVisiblePosition, "Hidden/loading monsters must remain isolated from movement updates.");
world.RemoveObject(approachingMonster);
approachingMonster.Hidden = false;
movementPump.Invoke(world, [new GameTime(TimeSpan.FromSeconds(97), TimeSpan.FromSeconds(1))]);
Check(approachingMonster.Position == monsterVisiblePosition, "Removed monsters must no longer be simulated.");

// Local login publication must recover independently of movement or camera changes.
var hero = new HeroObject(default) { World = world, Location = new Vector2(143, 119), Hidden = true };
typeof(WorldObject).GetProperty(nameof(WorldObject.Status))!.SetValue(hero, GameControlStatus.Ready);
world.Walker = hero;
var gameScene = (GameScene)RuntimeHelpers.GetUninitializedObject(typeof(GameScene));
typeof(GameScene).GetField("_hero", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gameScene, hero);
typeof(BaseScene).GetProperty(nameof(BaseScene.World))!.SetValue(gameScene, world);
bool RepairHero() => (bool)typeof(GameScene).GetMethod("EnsureHeroRenderPublication", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(gameScene, null)!;
world.Visible = false;
Check(!RepairHero() && hero.Hidden && !world.Objects.Contains(hero), "Hidden loading worlds must not publish the local hero.");
world.Visible = true;
Check(RepairHero(), "Activated gameplay must republish a ready local hero missing from Objects.");
Recull();
Check(!hero.Hidden && InSnapshot(hero), "Recovered hero must be unhidden and enter the visibility snapshot.");
Check(!RepairHero(), "Healthy hero publication must not trigger repeated repairs.");
hero.Hidden = true;
Check(RepairHero(), "A stale hidden flag after login must recover without movement.");
Recull();
InvokeWorld("RemoveVisibleObject", hero);
Check(RepairHero(), "A local hero omitted from the cached snapshot must recover.");
Recull();
Check(InSnapshot(hero), "Snapshot recovery must restore the hero to rendering.");
world.Walker = null;
hero.Hidden = true;
Check(!RepairHero() && hero.Hidden, "A hero detached for a world transition must remain hidden.");
world.RemoveObject(hero);

// A mobile skill tap retains its own slot/target while an animation blocks the
// first cast. Releasing the pointer must not depend on the desktop selected slot.
hero.Hidden = false;
hero.CurrentAction = PlayerAction.PlayerAttackFist;
hero.Position = new Vector3(14350, 11950, 0);
hero.MoveTargetPosition = hero.TargetPosition;
var skillControllerType = typeof(GameScene).Assembly.GetType("Client.Main.Scenes.GameSceneSkillController")!;
var hud = RuntimeHelpers.GetUninitializedObject(typeof(Client.Main.Controls.UI.Game.Hud.ModernBottomHud));
// Classic PC assigns Ctrl+number; Hybrid retains click-to-choose assignment.
var hudType = hud.GetType();
void HudField(string name, object value) => hudType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(hud, value);
HudField("_slotSkills", new SkillEntryState[13]);
HudField("_hotbarSkills", new SkillEntryState[10]);
HudField("_activeSet", -1);
HudField("_activeSkillSlot", 3);
var picker = new Client.Main.Controls.UI.Game.Skills.SkillSelectionPanel { Visible = false };
HudField("_skillPanel", picker);
var assignedSkill = new SkillEntryState { SkillId = 20 };
HudField("_selectedSkillOverride", assignedSkill);
typeof(MuGame).GetProperty(nameof(MuGame.Keyboard))!.SetValue(game,
    new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.LeftControl, Microsoft.Xna.Framework.Input.Keys.D2));
typeof(MuGame).GetProperty(nameof(MuGame.PrevKeyboard))!.SetValue(game, new Microsoft.Xna.Framework.Input.KeyboardState());
hudType.GetMethod("HandleKeyboard", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(hud, null);
Check(((Client.Main.Controls.UI.Game.Hud.ModernBottomHud)hud).GetHotbarSkillAt(1) == null,
    "Hybrid Ctrl+number must not rewrite click-to-choose assignments.");
HudField("_selectedSkillOverride", assignedSkill);
((Client.Main.Controls.UI.Game.Hud.ModernBottomHud)hud).SetHotbarSkillAt(1, assignedSkill);
var classicBar = RuntimeHelpers.GetUninitializedObject(hudType.Assembly.GetType("Client.Main.Controls.UI.Game.Hud.Classic.ClassicPcBottomBarControl")!);
classicBar.GetType().GetField("_hud", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(classicBar, hud);
classicBar.GetType().GetField("_skillRects", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(classicBar, new Rectangle[5]);
typeof(MuGame).GetProperty(nameof(MuGame.Keyboard))!.SetValue(game,
    new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.LeftControl, Microsoft.Xna.Framework.Input.Keys.D3));
classicBar.GetType().GetMethod("HandleKeyboard", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(classicBar, null);
Check(ReferenceEquals(((Client.Main.Controls.UI.Game.Hud.ModernBottomHud)hud).GetHotbarSkillAt(2), assignedSkill)
    && ((Client.Main.Controls.UI.Game.Hud.ModernBottomHud)hud).GetHotbarSkillAt(1) == null,
    "Classic PC Ctrl+3 must move the skill assignment even with a Modern skin.");
// Ctrl+Q/W/E/R registers the hovered inventory potion, including the fourth slot.
var potionInventory = (Client.Main.Controls.UI.Game.Inventory.InventoryControl)RuntimeHelpers.GetUninitializedObject(typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl));
potionInventory.Visible = true;
var potion = new InventoryItem(new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(3, "Large Healing Potion", 1, 1, "") { Group = 14 }, Point.Zero);
typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl).GetField("_hoveredItem", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(potionInventory, potion);
typeof(GameScene).GetField("_inventoryControl", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gameScene, potionInventory);
((GameControl)classicBar).Parent = gameScene;
HudField("_potionAssignments", new (byte Group, int Id)?[5]);
var potionKeys = new[] { Microsoft.Xna.Framework.Input.Keys.Q, Microsoft.Xna.Framework.Input.Keys.W, Microsoft.Xna.Framework.Input.Keys.E, Microsoft.Xna.Framework.Input.Keys.R };
for (int slot = 0; slot < potionKeys.Length; slot++)
{
    typeof(MuGame).GetProperty(nameof(MuGame.Keyboard))!.SetValue(game, new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.LeftControl, potionKeys[slot]));
    classicBar.GetType().GetMethod("HandleKeyboard", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(classicBar, null);
    Check(((Client.Main.Controls.UI.Game.Hud.ModernBottomHud)hud).GetItemAssignmentAt(slot) == ((byte)14, 3),
        "Classic Ctrl+potion key must register the hovered potion in each Q/W/E/R slot.");
}
((GameControl)classicBar).Parent = null;
typeof(GameControl).GetProperty(nameof(GameControl.Controls))!.SetValue(classicBar, Activator.CreateInstance(typeof(Client.Main.Helpers.ChildrenCollection<GameControl>), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [classicBar], null));
classicBar.GetType().GetField("_buttonRects", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(classicBar, new Rectangle[5]);
classicBar.GetType().GetField("_skillListHits", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(classicBar, new List<(Rectangle, SkillEntryState)>());
foreach (var windowSize in new[] { new Point(1280, 720), new Point(800, 900), new Point(1920, 600), new Point(640, 480) })
{
    Client.Main.Controllers.UiScaler.Configure(windowSize.X, windowSize.Y, 1280, 720);
    classicBar.GetType().GetMethod("RefreshLayout", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(classicBar, null);
    var bar = (GameControl)classicBar;
    var bottom = Client.Main.Controllers.UiScaler.ToActual(new Point(bar.X, bar.Y + bar.ViewSize.Y));
    Check(Math.Abs(bottom.Y - windowSize.Y) <= 1, "Classic PC HUD must touch the physical bottom after changing window proportions.");
    int drawnFrameHeight = (int)classicBar.GetType().GetMethod("ScaleValue", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(classicBar, [51])!;
    var visibleFrameBottom = Client.Main.Controllers.UiScaler.ToActual(new Point(bar.X, bar.Y + drawnFrameHeight));
    Check(Math.Abs(visibleFrameBottom.Y - windowSize.Y) <= 1,
        "The drawn HUD frame must reach the window bottom, without invisible padding below it.");
    var left = Client.Main.Controllers.UiScaler.ToActual(new Point(bar.X, bar.Y));
    var right = Client.Main.Controllers.UiScaler.ToActual(new Point(bar.X + bar.ViewSize.X, bar.Y));
    Check(left.X >= -1 && right.X <= windowSize.X + 1, "Classic PC HUD must fit inside the resized window.");
    var corner = Client.Main.Controllers.UiScaler.ToActual(Client.Main.Controllers.UiScaler.WindowBounds.Location);
    Check(Math.Abs(corner.X) <= 1 && Math.Abs(corner.Y) <= 1, "Helper corner must map to the physical top-left through letterboxing.");
}
Client.Main.Controllers.UiScaler.Configure(1280, 720, 1280, 720);
typeof(MuGame).GetProperty(nameof(MuGame.Keyboard))!.SetValue(game, new Microsoft.Xna.Framework.Input.KeyboardState());
var skillController = Activator.CreateInstance(skillControllerType, gameScene, hud, NullLogger.Instance,
    new Func<PlayerObject, bool>(_ => false))!;
typeof(GameScene).GetField("_skillController", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gameScene, skillController);
var touchSkill = new SkillEntryState { SkillId = 4 };
var touchTarget = new TestMonster { World = world, Location = hero.Location };
var touchStart = skillControllerType.GetMethod("StartSkillFromTouchHotbar", BindingFlags.NonPublic | BindingFlags.Instance)!;
var touchFlag = skillControllerType.GetField("_persistentSkillFromTouch", BindingFlags.NonPublic | BindingFlags.Instance)!;
var lockedTarget = skillControllerType.GetField("_persistentSkillTarget", BindingFlags.NonPublic | BindingFlags.Instance)!;
touchStart.Invoke(skillController, [touchSkill, touchTarget]);
Check((bool)touchFlag.GetValue(skillController)! && ReferenceEquals(lockedTarget.GetValue(skillController), touchTarget),
    "A mobile skill tap must retain its target even when the first cast is animation-blocked.");
skillControllerType.GetMethod("Update")!.Invoke(skillController, null);
Check((bool)touchFlag.GetValue(skillController)!, "The next pointer-independent update must retain mobile skill repetition.");
typeof(GameScene).GetMethod("CancelAutoAttack", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(gameScene, null);
Check(!(bool)touchFlag.GetValue(skillController)! && lockedTarget.GetValue(skillController) == null,
    "Movement/action cancellation must clear mobile skill repetition.");
touchStart.Invoke(skillController, [touchSkill, touchTarget]);
touchTarget.World = new TestWorld();
skillControllerType.GetMethod("Update")!.Invoke(skillController, null);
Check(!(bool)touchFlag.GetValue(skillController)!, "Changing the target's world must end mobile skill repetition.");

// Offensive area skills retain a monster target on desktop, just like mobile.
var canLock = skillControllerType.GetMethod("CanLockSkillTarget", BindingFlags.NonPublic | BindingFlags.Instance)!;
touchTarget.World = world;
Check((bool)canLock.Invoke(skillController, [new SkillEntryState { SkillId = 10 }, touchTarget])!,
    "Hellfire must retain a monster target after RMB release despite ignoring target range.");
Check((bool)canLock.Invoke(skillController, [new SkillEntryState { SkillId = 14 }, touchTarget])!,
    "Inferno must retain a monster target after RMB release.");
Check(!(bool)canLock.Invoke(skillController, [new SkillEntryState { SkillId = 18 }, touchTarget])!,
    "Self buffs must remain single-use.");
var layout = typeof(Client.Main.Controls.UI.Game.Hud.InventoryLayout);
int Layout(string name) => (int)layout.GetProperty(name)!.GetValue(null)!;
Check(Layout("GridCols") * Layout("GridRows") == 64,
    "The base inventory must expose all 64 server-supported bag cells.");
Check(Layout("GridY") + Layout("GridRows") * Layout("GridCellSize") <= Layout("FooterY"),
    "The final inventory row must not overlap Zen or footer controls.");
Check(Layout("MoneyY") + 30 <= Layout("PanelH"), "The full Zen field must fit in the inventory panel.");

// Repeated attacks must preserve an asynchronous approach instead of cancelling
// and recreating pathfinding on every 75ms combat tick.
var intent = typeof(WalkerObject).GetField("_movementIntent", BindingFlags.NonPublic | BindingFlags.Instance)!;
var moveVersion = typeof(WalkerObject).GetField("_moveRequestVersion", BindingFlags.NonPublic | BindingFlags.Instance)!;
intent.SetValue(hero, true);
touchTarget.Location = hero.Location + new Vector2(10, 0);
var initialMoveVersion = moveVersion.GetValue(hero);
var startAttack = typeof(GameScene).GetMethod("TryStartAutoAttack", BindingFlags.NonPublic | BindingFlags.Instance)!;
Check((bool)startAttack.Invoke(gameScene, [touchTarget])!, "A normal attack tap must retain its target during approach.");
var autoTarget = typeof(GameScene).GetField("_autoAttackTarget", BindingFlags.NonPublic | BindingFlags.Instance)!;
var repeatAttack = typeof(GameScene).GetMethod("UpdateAutoAttack", BindingFlags.NonPublic | BindingFlags.Instance)!;
for (int i = 0; i < 4; i++)
    repeatAttack.Invoke(gameScene, [new GameTime(TimeSpan.FromSeconds(i), TimeSpan.FromMilliseconds(100))]);
Check(ReferenceEquals(autoTarget.GetValue(gameScene), touchTarget), "Released-input attack ticks must retain the same live monster.");
Check(Equals(initialMoveVersion, moveVersion.GetValue(hero)), "Combat retries must not restart an approach with movement pending.");
typeof(GameScene).GetMethod("CancelAutoAttack", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(gameScene, null);
Check(autoTarget.GetValue(gameScene) == null, "Movement or another combat action must cancel normal attack repetition.");
intent.SetValue(hero, false);

// Preserve previous generation/map protections in the authoritative scope manager.
var state = new CharacterState(NullLoggerFactory.Instance);
var scope = new ScopeManager(NullLoggerFactory.Instance, state);
Check(scope.AddOrUpdatePlayerInScope(0x123, 0x123, 131, 116, "PC", CharacterClassNumber.BladeKnight), "Accept player scope.");
Check(scope.TryUpdateScopeObjectPosition(0x123, 132, 116), "Accept current network position.");
Check(scope.GetScopeObjectByMaskedId(0x123)!.PositionX == 132, "Scope records must retain the latest position.");
scope.BeginWorldTransition();
Check(!scope.AddOrUpdatePlayerInScope(0x123, 0x123, 131, 116, "Old PC"), "Late source-map spawn must remain rejected.");
scope.CompleteWorldTransition();
Check(scope.AddOrUpdatePlayerInScope(0x123, 0x123, 131, 116, "New PC"), "New lifecycle may enter scope.");

// Replay a real 0x12 packet through the public handler after the initial import
// snapshot was drained, while the world is still loading. Hold the async worker
// only to inspect the queued request without loading GPU assets in this test.
var handlerType = typeof(ScopeHandler);
var handler = (ScopeHandler)RuntimeHelpers.GetUninitializedObject(handlerType);
void SetHandler(string name, object value) => handlerType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(handler, value);
void SetHandlerStatic(string name, object value) => handlerType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, value);
object InvokeHandlerStatic(string name, params object[] arguments) => handlerType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments)!;
SetHandler("_logger", NullLogger<ScopeHandler>.Instance);
SetHandler("_scopeManager", scope);
SetHandler("_characterState", state);
SetHandler("_buffManager", new BuffManager(NullLoggerFactory.Instance));
SetHandler("_targetVersion", TargetProtocolVersion.Season6);
SetHandler("_useExtendedCharacterScopeFormat", true);
SetHandlerStatic("_activeInstance", handler);
SetHandlerStatic("_playerSpawnWorkerRunning", 1);
var pendingPlayers = (List<PlayerScopeObject>)handlerType.GetField("_pendingPlayers", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
InvokeHandlerStatic("TakePendingPlayers");
world.SetStatus(GameControlStatus.Initializing);
byte[] spawnBytes = new byte[54];
spawnBytes[0] = 0xC3; spawnBytes[1] = 54; spawnBytes[2] = 0x12;
var wire = new AddCharacterToScopeExtended(spawnBytes);
wire.Id = 0x321;
wire.CurrentPositionX = 131; wire.CurrentPositionY = 116;
spawnBytes[26] = (byte)CharacterClassNumber.BladeKnight;
await handler.HandleAddCharacterToScopeAsync(spawnBytes);
Check(scope.GetScopeObjectByMaskedId(0x321) is PlayerScopeObject received && received.Class == CharacterClassNumber.BladeKnight && received.PositionX == 131,
    "A serialized PC spawn packet must reach parsing and authoritative scope management.");
Check(pendingPlayers.Count == 1, "A late player packet must be buffered after the initial snapshot.");
InvokeHandlerStatic("PumpPendingPlayerSpawns", world);
Check(pendingPlayers.Count == 1, "The live pump must not drain a world that is still loading.");
scope.TryUpdateScopeObjectPosition(0x321, 132, 117);
world.SetStatus(GameControlStatus.Ready);
InvokeHandlerStatic("PumpPendingPlayerSpawns", world);
Check(pendingPlayers.Count == 0, "The live scene must drain a late buffered spawn.");
var queue = handlerType.GetField("_playerSpawnQueue", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
var queued = ((System.Collections.IEnumerable)queue).Cast<object>().ToArray();
Check(queued.Length == 1, "The buffered player must enter the normal lifecycle-protected spawn queue exactly once.");
Check((byte)queued[0].GetType().GetProperty("X")!.GetValue(queued[0])! == 132 &&
    (byte)queued[0].GetType().GetProperty("Y")!.GetValue(queued[0])! == 117,
    "The late spawn must use the newest position, not the initial packet coordinates.");
InvokeHandlerStatic("PumpPendingPlayerSpawns", world);
Check((int)queue.GetType().GetProperty("Count")!.GetValue(queue)! == 1, "Repeated pumps must not duplicate the request.");
pendingPlayers.Add((PlayerScopeObject)scope.GetScopeObjectByMaskedId(0x321)!);
scope.RemoveObjectFromScope(0x321);
InvokeHandlerStatic("PumpPendingPlayerSpawns", world);
Check((int)queue.GetType().GetProperty("Count")!.GetValue(queue)! == 1, "A removed player must not be resurrected by a late buffer.");

// Inventory feature wire regressions: dynamic item sizes and S6 store storage offsets.
state.InventoryExpansionState = 230;
Check(state.InventoryExpansionState == 0, "An invalid server extension count must never unlock inventory pages.");
state.InventoryExpansionState = 4;
Check(state.InventoryExpansionState == 4, "Retain the maximum legitimate Season 6 expansion count.");
var shopState = new PersonalShopState();
shopState.PendingPrices[204] = 12345;
shopState.ConfirmPrice(204, false);
Check(!shopState.Prices.ContainsKey(204), "A rejected store price must not appear as confirmed.");
shopState.PendingPrices[204] = 23456;
shopState.ConfirmPrice(204, true);
Check(shopState.Prices[204] == 23456 && shopState.PendingPrices.Count == 0, "Commit the acknowledged store price exactly once.");
if (OperatingSystem.IsWindows())
{
    var loginStore = typeof(CharacterState).Assembly.GetType("Client.Main.Configuration.LoginDetailsStore")!;
    var protect = loginStore.GetMethod("ProtectWindows", BindingFlags.NonPublic | BindingFlags.Static)!;
    byte[] sample = System.Text.Encoding.UTF8.GetBytes("test-user\ntest-password");
    var protectedBytes = (byte[])protect.Invoke(null, new object[] { sample, true })!;
    Check(!protectedBytes.SequenceEqual(sample), "Windows remembered details are encrypted, not stored as plain text.");
    Check(((byte[])protect.Invoke(null, new object[] { protectedBytes, false })!).SequenceEqual(sample), "Windows user-protected credentials round trip correctly without saving real login details.");
}
var moveState = new CharacterState(NullLoggerFactory.Instance);
byte[] originalStoreItem = { 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
moveState.AddOrUpdateInventoryItem(204, originalStoreItem);
moveState.PersonalShop.Prices[204] = 1000;
moveState.StashPendingInventoryMove(204, 12);
var moveHandler = new InventoryHandler(NullLoggerFactory.Instance, moveState, null!, TargetProtocolVersion.Season6);
byte[] rejectedMove = new byte[16];
rejectedMove[0] = 0xC3; rejectedMove[1] = 16; rejectedMove[2] = 0x24; rejectedMove[3] = 0xFF;
await moveHandler.HandleItemMovedOrFailedAsync(rejectedMove);
Check(moveState.GetInventoryItems()[204].SequenceEqual(originalStoreItem), "A rejected store move must preserve the original item, not substitute failure-packet data.");
Check(moveState.PendingMoveFromSlot == null && moveState.PersonalShop.Prices[204] == 1000, "A rejected move releases the pending operation and preserves the store price.");
moveState.StashPendingInventoryMove(204, 12);
byte[] confirmedMove = new byte[17];
confirmedMove[0] = 0xC3; confirmedMove[1] = 17; confirmedMove[2] = 0x24; confirmedMove[3] = 0; confirmedMove[4] = 12;
originalStoreItem.CopyTo(confirmedMove, 5);
await moveHandler.HandleItemMovedOrFailedAsync(confirmedMove);
Check(!moveState.HasInventoryItem(204) && moveState.GetInventoryItems()[12].SequenceEqual(originalStoreItem), "A confirmed store removal moves the exact item to the inventory once.");
Check(!moveState.PersonalShop.Prices.ContainsKey(204), "Clear a removed item's price only after successful movement.");
byte[] announcements = new byte[82];
announcements[5] = 2;
announcements[6] = 0x81; announcements[7] = 0x23; announcements[8] = (byte)'A';
announcements[44] = 0x04; announcements[45] = 0x56; announcements[46] = (byte)'B';
Check(shopState.ApplyAnnouncements(announcements, 0x123), "Accept all nearby store announcements.");
var loginShopState = new CharacterState(NullLoggerFactory.Instance);
loginShopState.PersonalShop.ApplyAnnouncements(announcements, 0);
loginShopState.UpdateCoreCharacterInfo(0x123, "StoreOwner", default, 100, 1, 1, 0);
Check(loginShopState.PersonalShop.IsOpen && loginShopState.PersonalShop.Title == "A", "Resolve an existing store announced before the character's identity is initialized.");
loginShopState.PersonalShop.Prices[204] = 1000;
loginShopState.UpdateCoreCharacterInfo(0x123, "StoreOwner", default, 100, 1, 1, 0);
Check(loginShopState.PersonalShop.IsOpen && loginShopState.PersonalShop.Prices[204] == 1000, "Loading the game scene preserves the server's open store and confirmed prices.");
loginShopState.PersonalShop.Close(0x123, 0x123);
loginShopState.UpdateCoreCharacterInfo(0x123, "StoreOwner", default, 100, 1, 1, 0);
Check(!loginShopState.PersonalShop.IsOpen, "Refreshing character information must not reopen a closed store.");
Check(shopState.GetVisibleTitle(0x8123, 0x123) == "A" && shopState.GetVisibleTitle(0x456, 0x123) == "B", "Show both owner and remote titles with masked ids.");
Check(!shopState.ApplyAnnouncements(announcements.AsSpan(0, 81), 0x123) && shopState.NearbyTitles.Count == 2, "Reject truncated announcements without partial mutation.");
shopState.Close(0x8456, 0x123);
Check(shopState.GetVisibleTitle(0x456, 0x123) == null && shopState.IsOpen, "Closing a remote store keeps the owner's sign.");
shopState.Close(0x123, 0x123);
Check(shopState.GetVisibleTitle(0x123, 0x123) == null, "Hide the owner's sign when closed.");
shopState.Reset();
Check(shopState.NearbyTitles.Count == 0, "Clear store titles across character sessions.");
byte[] storeList = new byte[69];
storeList[0] = 0xC2; storeList[2] = 69; storeList[3] = 0x3F; storeList[4] = 5;
storeList[5] = 1; storeList[6] = 1; storeList[7] = 35; storeList[8] = (byte)'A'; storeList[18] = (byte)'S'; storeList[54] = 1;
System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(storeList.AsSpan(55), 123456);
storeList[63] = 204;
byte[] plainItem = { 0x80, 1, 0, 255, 0 };
plainItem.CopyTo(storeList, 64);
Check(Client.Main.Core.Utilities.PlayerStorePacketReader.TryReadList(storeList, true, out var readStore) && readStore.Owner == 0x123 && readStore.Items[0].Slot == 204 && readStore.Items[0].Price == 123456, "Parse a dynamic personal-store item without confusing price bytes with item data.");
Check(!Client.Main.Core.Utilities.PlayerStorePacketReader.TryReadList(storeList.AsSpan(0, 68), true, out _), "Reject a truncated personal-store item atomically.");
storeList[59] = 0xF0; storeList[60] = 0xFF;
Check(Client.Main.Core.Utilities.PlayerStorePacketReader.TryReadList(storeList, true, out readStore) && readStore.Items.Count == 0, "Never offer an account-coin item as a Zen purchase.");
byte[] purchase = new byte[13];
purchase[0] = 0xC1; purchase[1] = 13; purchase[2] = 0x3F; purchase[3] = 6;
purchase[4] = 0x23; purchase[5] = 1; purchase[6] = 1; purchase[7] = 76; plainItem.CopyTo(purchase, 8);
Check(Client.Main.Core.Utilities.PlayerStorePacketReader.TryReadPurchase(purchase, true, out byte buyResult, out byte buySlot, out var bought) && buyResult == 1 && buySlot == 76 && bought.SequenceEqual(plainItem), "Use the extended purchase result and inventory-slot fields, not the seller id or trailing item byte.");
byte[] paddedPurchase = new byte[23];
purchase.CopyTo(paddedPurchase, 0); paddedPurchase[1] = 23;
Check(Client.Main.Core.Utilities.PlayerStorePacketReader.TryReadPurchase(paddedPurchase, true, out buyResult, out buySlot, out bought) && bought.SequenceEqual(plainItem), "Accept OpenMU's maximum-sized purchase buffer and trim unused serializer bytes.");
paddedPurchase[22] = 1;
Check(!Client.Main.Core.Utilities.PlayerStorePacketReader.TryReadPurchase(paddedPurchase, true, out _, out _, out _), "Reject unexpected nonzero bytes after a purchased item.");
var saleState = new CharacterState(NullLoggerFactory.Instance);
saleState.UpdateInventoryZen(100);
saleState.AddOrUpdateInventoryItem(12, plainItem);
saleState.StashPendingSellSlot(12);
var saleHandler = new ShopHandler(NullLoggerFactory.Instance, saleState, null!, TargetProtocolVersion.Season6);
byte[] salePacket = new byte[8];
salePacket[0] = 0xC1; salePacket[1] = 8; salePacket[2] = 0x33; salePacket[3] = 1;
System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(salePacket.AsSpan(4), 123456);
await saleHandler.HandleNpcItemSellResultAsync(salePacket);
Check(saleState.InventoryZen == 123456 && !saleState.HasInventoryItem(12), "Use the server-confirmed merchant balance and remove the sold item together.");
byte[] socketItem = { 0x80, 1, 0, 255, 0x80, 0xF2, 1, 51 };
Check(Client.Main.Core.Utilities.EquipmentBonusDatabase.GetSocketBytes(socketItem).SequenceEqual(new byte[] {1, 51}), "Extract seeds after variable item options and socket metadata.");
Check(Client.Main.Core.Utilities.EquipmentBonusDatabase.DescribeSocket(1).Contains("+7") && Client.Main.Core.Utilities.EquipmentBonusDatabase.DescribeSocket(51).Contains("+8"), "Resolve English seed descriptions and sphere-level values from the embedded MuMain data.");
Check(Client.Main.Core.Utilities.EquipmentBonusDatabase.DescribeSets(Array.Empty<InventoryItem>()).Contains("No ancient"), "Report empty equipment without inventing set bonuses.");
var setHelmet = new InventoryItem(new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(5, "Warrior helm", 2, 2), Point.Zero, new byte[] { 0x70, 5, 0, 255, 0x10, 1 });
var setArmor = new InventoryItem(new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(5, "Warrior armor", 2, 3), Point.Zero, new byte[] { 0x80, 5, 0, 255, 0x10, 1 });
string singleSet = Client.Main.Core.Utilities.EquipmentBonusDatabase.DescribeSets(new[] { setHelmet });
Check(singleSet.Contains("Warrior's (1/7") && !singleSet.Contains("Active: Strength"), "A single ancient item must not enable the two-piece set bonus.");
string pairedSet = Client.Main.Core.Utilities.EquipmentBonusDatabase.DescribeSets(new[] { setHelmet, setArmor });
Check(pairedSet.Contains("Warrior's (2/7") && pairedSet.Contains("Active: Strength +10"), "Activate the actual MuMain two-piece ancient bonus.");
string duplicateSet = Client.Main.Core.Utilities.EquipmentBonusDatabase.DescribeSets(new[] { setHelmet, setHelmet });
Check(duplicateSet.Contains("(1/7"), "Duplicate items must not falsely complete an ancient set.");
var storeUpdate = typeof(ShopHandler).GetMethod("HandlePlayerStoreUpdateAsync")!;
Check(storeUpdate.GetCustomAttribute<Client.Main.Core.Utilities.PacketHandlerAttribute>()!.SubCode == 0x13, "Route store updates separately from the initial list response.");

var dragInventory = new Client.Main.Controls.UI.Game.Inventory.InventoryControl(null, NullLoggerFactory.Instance) { Visible = true };
var otherWindow = new Client.Main.Controls.UI.Game.Inventory.PlayerShopControl();
typeof(BaseScene).GetField("_pointerGestureControl", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gameScene, otherWindow);
var receivePointer = typeof(BaseScene).GetMethod("CanReceivePointerInput", BindingFlags.NonPublic | BindingFlags.Instance)!;
Check(!(bool)receivePointer.Invoke(gameScene, [dragInventory])!, "An ordinary inventory must respect another window's pointer ownership.");
dragInventory._pickedItemRenderer.PickUpItem(setHelmet);
Check((bool)receivePointer.Invoke(gameScene, [dragInventory])!, "A held inventory item retains input when the pointer moves over a merchant window.");
dragInventory._pickedItemRenderer.ReleaseItem();
Check(!(bool)receivePointer.Invoke(gameScene, [dragInventory])!, "Releasing the item restores normal window pointer routing.");
Console.WriteLine($"PASS: {assertions} remote-player visibility regression assertions.");
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    Environment.Exit(1);
}

sealed class TestWorld : WalkableWorldControl
{
    public TestWorld() : base(1) { Status = GameControlStatus.Ready; }
    public void SetStatus(GameControlStatus status) => Status = status;
}
sealed class TestMonster : MonsterObject { }
sealed class TestScene : BaseScene
{
    public TestScene(WorldControl world) { World = world; Controls.Add(world); }
}
