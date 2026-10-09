using System.Reflection;
using System.Runtime.CompilerServices;
using Client.Main;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

try
{
    int checks = 0;
    void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    var game = (MuGame)RuntimeHelpers.GetUninitializedObject(typeof(MuGame));
    var graphics = (GraphicsDeviceManager)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDeviceManager));
    graphics.PreferredBackBufferWidth = 1280; graphics.PreferredBackBufferHeight = 720;
    typeof(MuGame).GetField("_graphics", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, graphics);
    typeof(MuGame).GetProperty(nameof(MuGame.Instance))!.SetValue(null, game);
    var scene = new TestScene();
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
    Console.WriteLine($"PASS: {checks} UI overlap, gesture capture, scroll, overlay and mobile-selection checks.");
}
catch (Exception exception) { Console.Error.WriteLine(exception); Environment.Exit(1); }

sealed class TestScene : BaseScene
{
    public TestScene() { Controls.Clear(); Status = GameControlStatus.Ready; }
}
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
