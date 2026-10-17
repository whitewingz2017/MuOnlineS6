using Client.Main;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.NPCS;
using Client.Main.Scenes;
using Client.Main.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

try
{
    using var game = new MuGame { ConnectOnLoad = false };
    Constants.DataPath = Environment.GetEnvironmentVariable("TWINMU_DATA") ?? @"C:\OpenMU\Data";
    Constants.SETTINGS_PATH = Path.GetFullPath("Client.Main/appsettings.json");
    Constants.ENTRY_SCENE = typeof(CaptureScene);
    CaptureScene.Output = Path.GetFullPath(args.Length > 0 ? args[0] : "npc-shadows.png");
    game.Components.Add(new CaptureFrame(game));
    game.Run();
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}

public sealed class CaptureWorld : NoriaWorld
{
    public CaptureWorld()
    {
        Walker = new Pasi { Location = new Vector2(172, 100) };
        Objects.Add(Walker);
    }
    public override void AfterLoad()
    {
        base.AfterLoad();
        Walker.Location = new Vector2(172, 100);
        Walker.Position = Walker.MoveTargetPosition = Walker.TargetPosition;
    }
}

public sealed class CaptureScene : BaseScene
{
    public static string Output = "npc-shadows.png";
    public int Frames;
    public bool Ready; private Client.Main.Objects.ModelObject _dragon; private Client.Main.Objects.Effects.InvasionFireEffect _fire;
    private bool _merchantChecked;
    public override async Task Load()
    {
        await ChangeWorldAsync<CaptureWorld>();
        await base.Load();
        var cherry = new CherryBlossomSpirit { World = World, Location = new Vector2(172, 100) };
        World.Objects.Add(cherry);
        await cherry.Load();
        var lala = new ElfLala { World = World, Location = new Vector2(174, 100) };
        World.Objects.Add(lala);
        await lala.Load();
        if (Environment.GetEnvironmentVariable("TWINMU_CAPTURE_MERCHANT") == "1")
        {
            Client.Main.Controls.UI.UiThemeManager.SetTheme(Client.Main.Controls.UI.UiThemeId.Classic, persist: false);
            var shop = Client.Main.Controls.UI.Game.NpcShopControl.Instance;
            shop.SetRepairShop(Environment.GetEnvironmentVariable("TWINMU_CAPTURE_REPAIR") == "1");
            Controls.Add(shop);
            await shop.Load();
            var items = (List<InventoryItem>)typeof(Client.Main.Controls.UI.Game.NpcShopControl)
                .GetField("_items", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(shop)!;
            foreach (var entry in new[] { (14,3,0,0), (14,6,1,0), (7,2,0,3), (8,2,2,3), (9,2,4,3),
                (10,2,6,3), (11,2,0,7), (5,0,2,7), (15,0,0,12), (5,1,4,13) })
            {
                var definition = Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition((byte)entry.Item1, (short)entry.Item2);
                if (definition != null) items.Add(new InventoryItem(definition, new Point(entry.Item3,entry.Item4), durability:3));
            }
            shop.Align = ControlAlign.None;
            shop.X = 12; shop.Y = 2; shop.Visible = true;
            typeof(Client.Main.Controls.UI.Game.NpcShopControl).GetField("_shopScrollOffset",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(shop, 16f);
        }
        if (Environment.GetEnvironmentVariable("TWINMU_CAPTURE_INVASION") == "1") { _dragon = (Client.Main.Objects.ModelObject)Activator.CreateInstance(typeof(MuGame).Assembly.GetType("Client.Main.Objects.Worlds.InvasionDragonObject")!, new object[]{true})!; _dragon.World=World; _dragon.Scale=.7f; World.Objects.Add(_dragon); await _dragon.Load(); _fire = new Client.Main.Objects.Effects.InvasionFireEffect { World=World }; World.Objects.Add(_fire); await _fire.Load(); } Constants.RENDER_SCALE = 1;
        Constants.ENABLE_TERRAIN_GPU_LIGHTING = false;
        Constants.OPTIMIZE_FOR_INTEGRATED_GPU = true;
        Constants.ApplyShadowQualityPreset(Constants.ShadowQuality.Medium);
        Ready = true;
    }
    public override void Update(GameTime time)
    {
        if (Environment.GetEnvironmentVariable("TWINMU_CAPTURE_MERCHANT") == "1")
            UiScaler.Configure(1280, 720, 1280, 540, ScaleMode.Stretch);
        base.Update(time);
        if (!Ready) return;
        if (!_merchantChecked && Environment.GetEnvironmentVariable("TWINMU_CAPTURE_MERCHANT") == "1")
        {
            var shop = Client.Main.Controls.UI.Game.NpcShopControl.Instance;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var grid = (Rectangle)shop.GetType().GetField("_gridRect", flags)!.GetValue(shop)!;
            var footer = (Rectangle)shop.GetType().GetField("_footerRect", flags)!.GetValue(shop)!;
            var repair = (Rectangle)shop.GetType().GetField("_repairButtonRect", flags)!.GetValue(shop)!;
            if (grid.Bottom > footer.Top || shop.IsRepairShop && grid.Bottom > repair.Top)
                throw new Exception("The compact merchant grid overlaps its footer or repair controls.");
            var hit = shop.GetType().GetMethod("GetItemAt", flags)!;
            object? Hit(int x, int y) => hit.Invoke(shop, new object[] { new Point(shop.DisplayRectangle.X + grid.X + x, shop.DisplayRectangle.Y + grid.Y + y) });
            if (Hit(8,-1) != null || Hit(8,grid.Height) != null)
                throw new Exception("Merchant items remain selectable outside the clipped grid.");
            if (Hit(8,4) == null) throw new Exception("The visible half of the scrolled potion cannot be selected.");
            var scroll = shop.GetType().GetMethod("UpdateMerchantScroll", flags)!;
            var start = new Point(shop.DisplayRectangle.X + grid.X + 8, shop.DisplayRectangle.Y + grid.Y + 100);
            scroll.Invoke(shop, new object[] { start, true, true, false });
            var end = new Point(start.X, start.Y - 40);
            scroll.Invoke(shop, new object[] { end, true, false, false });
            if ((bool)scroll.Invoke(shop, new object[] { end, false, false, true })!)
                throw new Exception("A merchant scroll gesture was mistaken for a purchase tap.");
            shop.GetType().GetField("_shopScrollOffset", flags)!.SetValue(shop, 16f);
            _merchantChecked = true;
            Console.WriteLine("PASS: Scrolled hit testing respects both grid edges, visible partial items, and drag-versus-tap input.");
        }
        Vector3 center = ((CaptureWorld)World).Walker.Position;
        if(_dragon != null) _dragon.Position = center + new Vector3(0,0,300); if(_fire != null) _fire.Position = center + new Vector3(-130,-100,300); Camera.Instance.Target = center;
        Camera.Instance.Position = center + (_dragon != null ? new Vector3(0, -1800, 2200) : new Vector3(0, -900, 1000));
        Frames++;
    }
}

sealed class CaptureFrame(MuGame game) : DrawableGameComponent(game)
{
    public override void Draw(GameTime time)
    {
        if (game.ActiveScene is not CaptureScene { Ready: true, Frames: > 100 }) return;
        var device = game.GraphicsDevice;
        if (device.GetRenderTargets().Length > 0) return;
        // Exit even when readback fails: MuGame contains draw exceptions and would
        // otherwise retry this diagnostic forever.
        try
        {
            var shadow = GraphicsManager.Instance.ShadowMapRenderer.ShadowMap
                ?? throw new InvalidOperationException("No shadow texture was generated.");
            var depths = new float[shadow.Width * shadow.Height];
            if (shadow.Format == SurfaceFormat.HalfSingle)
            {
                var packed = new HalfSingle[depths.Length];
                shadow.GetData(packed);
                for (int i = 0; i < packed.Length; i++) depths[i] = packed[i].ToSingle();
            }
            else shadow.GetData(depths);
            int occupied = depths.Count(v => float.IsFinite(v) && v < 0.999f);
            foreach(var obj in game.ActiveScene.World.Objects.Where(o=>o.GetType().Name.Contains("Invasion"))) Console.WriteLine(obj.GetType().Name+":"+obj.Status+":"+obj.Visible+":"+obj.WorldPosition.Translation); if (occupied == 0)
                throw new InvalidOperationException("Shadow draw calls produced an empty shadow texture.");
            Console.WriteLine($"PASS: {occupied} shadow pixels contain geometry; depth minimum {depths.Min():F5}.");
            Save(device, shadow.Width, shadow.Height,
                depths.Select(v => new Color(v, v, v)).ToArray(), CaptureScene.Output + ".shadow.png");
            int width = device.PresentationParameters.BackBufferWidth;
            int height = device.PresentationParameters.BackBufferHeight;
            var pixels = new Color[width * height];
            device.GetBackBufferData(pixels);
            Save(device, width, height, pixels, CaptureScene.Output);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            Environment.ExitCode = 1;
        }
        finally { game.Exit(); }
    }
    private static void Save(GraphicsDevice device, int width, int height, Color[] pixels, string path)
    {
        using var texture = new Texture2D(device, width, height);
        texture.SetData(pixels);
        using var output = File.Create(path);
        texture.SaveAsPng(output, width, height);
    }
}



