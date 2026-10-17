using System.Reflection;
using System.Runtime.CompilerServices;
using Client.Data.ATT;
using Client.Main;
using Client.Main.Controls;
using Client.Main.Controls.Terrain;
using Client.Main.Core.Client;
using Client.Main.Configuration;
using Client.Main.Networking;
using Client.Main.Networking.PacketHandling.Handlers;
using Client.Main.Networking.Services;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xna.Framework;

try
{
int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
void Set(object obj, string field, object value) => obj.GetType()
    .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(obj, value);
object Empty(Type type) => RuntimeHelpers.GetUninitializedObject(type);

var state = new CharacterState(NullLoggerFactory.Instance) { Level = 400 };
var game = (MuGame)Empty(typeof(MuGame));
var graphics = (Microsoft.Xna.Framework.GraphicsDeviceManager)Empty(typeof(Microsoft.Xna.Framework.GraphicsDeviceManager));
graphics.PreferredBackBufferWidth = 1280;
graphics.PreferredBackBufferHeight = 720;
Set(game, "_graphics", graphics);
typeof(MuGame).GetProperty(nameof(MuGame.Instance))!.SetValue(null, game);
var handler = new MiscGamePacketHandler(NullLoggerFactory.Instance, null!, null!, state, null!, TargetProtocolVersion.Season6);
async Task Event(byte kind, bool enable) => await handler.HandleMapEventStateAsync(
    new byte[] { 0xC1, 5, 0x0B, enable ? (byte)1 : (byte)0, kind });
await Event(1, true);
Check(state.DragonInvasion.ActiveEvent == 1, "Red invasion packet was ignored.");
await Event(3, true);
Check(state.DragonInvasion.ActiveEvent == 3, "Golden invasion packet was ignored.");
await Event(1, false);
Check(state.DragonInvasion.ActiveEvent == 3, "Late red stop canceled a golden invasion.");
await handler.HandleMapEventStateAsync(new byte[] { 0xC1, 5, 0x0B });
Check(state.DragonInvasion.ActiveEvent == 3, "Short packet corrupted invasion state.");
await Event(9, true);
Check(state.DragonInvasion.ActiveEvent == 3, "Unknown event changed dragon state.");
await Event(3, false);
Check(state.DragonInvasion.ActiveEvent == 0, "Golden stop was ignored.");

// Use the actual Helper eligibility and update paths with real terrain flags.
var network = (NetworkManager)Empty(typeof(NetworkManager));
Set(network, "_characterState", state);
var connections = (ConnectionManager)Empty(typeof(ConnectionManager));
Set(network, "_characterService", new CharacterService(connections, NullLogger<CharacterService>.Instance));
typeof(MuGame).GetProperty(nameof(MuGame.Network))!.SetValue(null, network);
var world = new TestWorld();
var data = new TerrainData { Attributes = new TerrainAttribute() };
var physics = new TerrainPhysics(data, null!);
Set(world.Terrain, "_physics", physics);
var hero = (HeroObject)Empty(typeof(HeroObject));
// Set backing fields directly: the fixture intentionally has no graphics device.
typeof(Client.Main.Objects.WorldObject).GetField("_position", BindingFlags.Instance | BindingFlags.NonPublic)!
    .SetValue(hero, new Vector3(1050, 1050, 0));
// Location is safe to set on a normal PlayerObject; use its base backing field here.
var locationProperty = typeof(Client.Main.Objects.WalkerObject).GetProperty("Location")!;
locationProperty.SetValue(hero, new Vector2(11, 10));
var scene = (GameScene)Empty(typeof(GameScene));
Set(scene, "_hero", hero);
typeof(BaseScene).GetProperty("World")!.SetValue(scene, world);
var assembly = typeof(GameScene).Assembly;
var skills = Empty(assembly.GetType("Client.Main.Scenes.GameSceneSkillController")!);
var helperType = assembly.GetType("Client.Main.Scenes.MuHelperController")!;
var helper = Empty(helperType);
Set(helper, "_scene", scene);
Set(helper, "_skillController", skills);
helperType.GetProperty("Config")!.SetValue(helper, new MuHelperConfig());
var canOperate = helperType.GetMethod("CanOperate", BindingFlags.Instance | BindingFlags.NonPublic)!;
bool CanOperate() => (bool)canOperate.Invoke(helper, new object?[] { null })!;
Check(CanOperate(), "Helper blocked an ordinary field tile.");
data.Attributes.TerrainWall[10 * Constants.TERRAIN_SIZE + 10] = TWFlags.SafeZone;
Check(!CanOperate(), "Helper ignored the rendered safe tile while Location pointed outside.");
Set(helper, "_startRequestPending", true);
helperType.GetMethod("Update")!.Invoke(helper, new object[] { new GameTime() });
Check(!(bool)helperType.GetProperty("IsStartRequestPending")!.GetValue(helper)!, "Pending helper start survived entering safety.");
Set(helper, "_active", true);
helperType.GetMethod("Update")!.Invoke(helper, new object[] { new GameTime() });
Check(!(bool)helperType.GetProperty("IsActive")!.GetValue(helper)!, "Active Helper survived entering safety.");
helperType.GetMethod("OnMuHelperStatusUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!
    .Invoke(helper, new object[] { false, 0u, false });
Check(!(bool)helperType.GetProperty("IsActive")!.GetValue(helper)!, "Late server start reactivated Helper inside safety.");
data.Attributes.TerrainWall[10 * Constants.TERRAIN_SIZE + 10] = 0;
data.Attributes.TerrainWall[10 * Constants.TERRAIN_SIZE + 11] = TWFlags.SafeZone;
Check(!CanOperate(), "Helper ignored the logical safe tile while walking in.");
Console.WriteLine($"PASS: {checks} invasion packet and Helper safe-zone checks.");
// Exercise the shadow renderer's selection, rather than only checking flags.
var queue = new PriorityQueue<Client.Main.Objects.ModelObject, float>();
var selectCaster = typeof(Client.Main.Graphics.ShadowMapRenderer).GetMethod(
    "TryQueueCasterCandidate", BindingFlags.Static | BindingFlags.NonPublic)!;
var scenery = new Client.Main.Objects.MapTileObject();
typeof(Client.Main.Objects.WorldObject).GetProperty("Status")!.SetValue(scenery, Client.Main.Models.GameControlStatus.Ready);
void SelectScenery() => selectCaster.Invoke(null, new object[]
{
    scenery, new BoundingFrustum(Matrix.Identity), 0f, Vector3.Zero, 1000000f, 15, queue, false
});
SelectScenery();
Check(queue.Count == 1 && !scenery.RenderShadow, "Map scenery did not enter directional shadows independently of projected shadows.");
queue.Clear();
scenery.BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive;
SelectScenery();
Check(queue.Count == 0, "Additive scenery consumed the shadow caster budget.");
scenery.BlendState = Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;
scenery.Alpha = 0;
SelectScenery();
Check(queue.Count == 0, "Invisible scenery consumed the shadow caster budget.");
Console.WriteLine($"PASS: {checks} total checks including directional shadow selection.");
var dragonType = assembly.GetType("Client.Main.Objects.Worlds.InvasionDragonObject")!;
var flyover = (Client.Main.Objects.ModelObject)Activator.CreateInstance(dragonType, new object[] { true })!;
Vector3 modelForward = Vector3.TransformNormal(-Vector3.UnitY, Matrix.CreateRotationZ(flyover.Angle.Z));
Check(Vector3.Dot(modelForward, -Vector3.UnitY) > 0.999f,
    "Flyover model faces sideways relative to its negative-Y travel.");
flyover.Update(new GameTime(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)));
Check(!(bool)dragonType.GetProperty("Expired")!.GetValue(flyover)!,
    "A flyover expired while its assets were still loading.");
var fireball = new Client.Main.Objects.Effects.InvasionFireEffect();
fireball.Update(new GameTime(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)));
Check(!fireball.Expired, "An invasion fireball expired before loading finished.");
Console.WriteLine($"PASS: {checks} total checks including flyover facing and effect loading lifetimes.");
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.Exit(1);
}

sealed class TestWorld : WalkableWorldControl
{
    public TestWorld() : base(4) { }
}
