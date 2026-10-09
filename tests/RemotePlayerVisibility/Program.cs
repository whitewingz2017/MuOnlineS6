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
bool InSnapshot(PlayerObject player) => (bool)typeof(WorldControl)
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
var movementPump = typeof(WorldControl).GetMethod("UpdateCulledPlayerMovement", BindingFlags.NonPublic | BindingFlags.Instance)!;
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
sealed class TestScene : BaseScene
{
    public TestScene(WorldControl world) { World = world; Controls.Add(world); }
}
