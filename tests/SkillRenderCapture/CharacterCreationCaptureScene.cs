using Client.Main;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.SelectCharacter;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Worlds.SelectWrold;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using System.Reflection;

// Offline regression capture of production selection UI and class preview models.
public sealed class CharacterCreationCaptureScene : BaseScene
{
    private int _frame;
    private bool _ready;
    private string _capture = "";
    private CreateRoleFaceObject _face;
    private Client.Main.Controls.UI.Game.Inventory.InventoryControl _inventoryCapture;
    public override async Task Load()
    {
        await ChangeWorldAsync<Client.Main.Worlds.SelectWorld>();
        await base.Load();
        MuGame.Instance.ApplyGraphicsConfiguration(new Client.Main.Configuration.GraphicsSettings
        {
            Width = 1280, Height = 720, UiVirtualWidth = 1280, UiVirtualHeight = 720, IsFullScreen = false
        });
        UiScaler.Configure(1280, 720, 1280, 720);
        Constants.RENDER_SCALE = 1f;
        UiThemeManager.Initialize("Classic");
        Constants.ENABLE_GPU_SKINNING = false;
        SetCamera();
        _capture = Environment.GetEnvironmentVariable("TWINMU_CREATION_CAPTURE")!;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var slots = new ClassicCharacterSelectionControl();
        var rectangles = (System.Collections.ICollection)typeof(ClassicCharacterSelectionControl)
            .GetField("_slotRects", flags)!.GetValue(slots)!;
        if (rectangles.Count != 5) throw new Exception("Mobile selection must have exactly five slots.");
        if (_capture.StartsWith("inventory"))
        {
            slots.Dispose();
            if (_capture.Contains("desktop")) UiThemeManager.Initialize("Modern");
            var inventory = new Client.Main.Controls.UI.Game.Inventory.InventoryControl(MuGame.Network);
            _inventoryCapture = inventory;
            Controls.Add(inventory); await inventory.Initialize(); inventory.Show();
            inventory.ZenAmount = 50000;
            await inventory.EnsureSeason6AssetsAsync();
            if (_capture.EndsWith("tooltip"))
            {
                var items = (List<InventoryItem>)typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl)
                    .GetField("_items", flags)!.GetValue(inventory)!;
                var cells = (InventoryItem[,])typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl)
                    .GetField("_itemGrid", flags)!.GetValue(inventory)!;
                foreach (var sample in new[]{(8,29,0,0,15),(0,0,3,0,9),(14,4,5,0,0)})
                {
                    var definition = Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition((byte)sample.Item1,(short)sample.Item2);
                    if(definition == null) continue;
                    var item = new InventoryItem(definition,new Microsoft.Xna.Framework.Point(sample.Item3,sample.Item4),
                        durability:sample.Item1==14?25:50,level:sample.Item5);
                    items.Add(item);
                    for(int y=0;y<definition.Height;y++)for(int x=0;x<definition.Width;x++)cells[sample.Item3+x,sample.Item4+y]=item;
                }
            }
            if (!UiScaler.WindowBounds.Contains(inventory.DisplayRectangle))
                throw new Exception("Inventory does not fit the viewport.");
            var image = await Client.Main.Content.TextureLoader.Instance.Prepare("Interface/newui_item_upper.ozt");
            if(image.ContentWidth!=46 || image.ContentHeight!=66 || image.Width!=64 || image.Height!=128)
                throw new Exception("Armor OZT content bounds were lost.");
            Console.WriteLine("PASS: native inventory fits screen; armor crops 46x66 artwork from padded 64x128 texture.");
        }
        else if (_capture == "slots")
        {
            Controls.Add(slots); await slots.Initialize();
        }
        else
        {
            slots.Dispose();
            var dialog = new CharacterCreationDialog();
            for (int i = 0; i < 7; i++)
            {
                if (dialog.SelectedClassPreviewModel != $"NewFace{i + 1:00}")
                    throw new Exception($"Missing class preview at index {i}.");
                typeof(CharacterCreationDialog).GetMethod("ChangeClass", flags)!.Invoke(dialog, [1]);
            }
            int selected = int.Parse(_capture);
            for (int i = 0; i < selected; i++)
                typeof(CharacterCreationDialog).GetMethod("ChangeClass", flags)!.Invoke(dialog, [1]);
            Controls.Add(dialog); await dialog.Initialize();
            var face = new CreateRoleFaceObject { World = World };
            _face = face;
            if (face.Light != Vector3.One) throw new Exception("CPU class preview must have full texture light.");
            Vector3 forward = Vector3.Normalize(Camera.Instance.Target - Camera.Instance.Position);
            Vector3 left = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, forward));
            await face.SetClass(dialog.SelectedClassPreviewModel, Camera.Instance.Position + forward * 900f + left * 60f);
            if (face.Status != GameControlStatus.Ready || face.Model == null)
                throw new Exception($"Class preview failed to load: {dialog.SelectedClassPreviewModel}");
            face.Hidden = false;
            World.Objects.Add(face);
            typeof(Client.Main.Controls.WorldControl).GetMethod("ActivateObjectForRendering", flags)!
                .Invoke(World, [face, true]);
        }
        Console.WriteLine("PASS: five mobile slots; all seven class mappings; lit CPU preview loads.");
        _ready = true;
    }

    private static void SetCamera()
    {
        Camera.Instance.AspectRatio = 1280f / 720f;
        Camera.Instance.FOV = 29f;
        Camera.Instance.Position = new Vector3(14000, 11200, 850);
        Camera.Instance.Target = new Vector3(14229, 12340, 380);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SetCamera();
        if (_ready && ++_frame > 32) MuGame.Instance.Exit();
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        if (_ready && _capture.EndsWith("tooltip"))
        {
            var definition = Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition(2,1);
            if(definition != null)
            {
                var item = new InventoryItem(definition,Microsoft.Xna.Framework.Point.Zero,durability:25);
                var lines = Client.Main.Controls.UI.Game.Common.ItemUiHelper.BuildTooltipLines(item);
                using var scope = new Client.Main.Helpers.SpriteBatchScope(GraphicsManager.Instance.Sprite,
                    Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred,
                    Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
                    Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp, transform: UiScaler.SpriteTransform);
                Client.Main.Controls.UI.Game.Common.ItemUiHelper.DrawTooltipPanel(GraphicsManager.Instance.Sprite,
                    GraphicsManager.Instance.Pixel,GraphicsManager.Instance.Font,lines,new Microsoft.Xna.Framework.Rectangle(450,270,280,300),.44f);
            }
        }
        if (_ready && _inventoryCapture != null && _capture.EndsWith("grid"))
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl);
            var grid = (Microsoft.Xna.Framework.Rectangle)type.GetField("_gridRect", flags)!.GetValue(_inventoryCapture)!;
            grid.Offset(_inventoryCapture.DisplayRectangle.Location);
            var cells = (InventoryItem[,])type.GetField("_itemGrid", flags)!.GetValue(_inventoryCapture)!;
            cells[0, 6] = new InventoryItem(new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(0,"Occupied",1,1), new Microsoft.Xna.Framework.Point(0,6));
            var occupied = new InventoryItem(new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(0,"Occupied armor",2,2), new Microsoft.Xna.Framework.Point(3,2));
            for(int y=2;y<4;y++)for(int x=3;x<5;x++)cells[x,y]=occupied;
            var incoming = new InventoryItem(new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(0,"Incoming",2,2),Microsoft.Xna.Framework.Point.Zero);
            var blocked = type.GetMethod("IsPlacementCellBlocked",flags)!;
            int red=0,blue=0;
            for(int y=2;y<4;y++)for(int x=2;x<4;x++)
                if((bool)blocked.Invoke(_inventoryCapture,[incoming,x,y])!)red++;else blue++;
            if(red!=2 || blue!=2)throw new Exception("Partial overlap must mark two blocked cells red and two free cells blue.");
            if(_frame==24)Console.WriteLine("PASS: 2x2 drag overlapping half an occupied 2x2 item gives two red and two blue cells.");
            var hover = type.GetField("_hoveredSlot", flags)!;
            var paint = type.GetMethod("DrawNativeGridHighlights", flags)!;
            using var scope = new Client.Main.Helpers.SpriteBatchScope(GraphicsManager.Instance.Sprite,
                Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred,
                Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
                Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp, transform: UiScaler.SpriteTransform);
            foreach (var sample in new[]{(0,1,1,1),(2,2,2,2),(7,7,2,3)})
            {
                hover.SetValue(_inventoryCapture,new Microsoft.Xna.Framework.Point(sample.Item1,sample.Item2));
                var item = new InventoryItem(new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(0,"Drag preview",sample.Item3,sample.Item4),Microsoft.Xna.Framework.Point.Zero);
                paint.Invoke(_inventoryCapture,[GraphicsManager.Instance.Sprite,grid,item,true]);
            }
        }
        if (!_ready || _frame != 24) return;
        if (_inventoryCapture != null)
        {
            var surface = (Microsoft.Xna.Framework.Graphics.Texture2D)typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl)
                .GetField("_staticSurface", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_inventoryCapture)!;
            using(var output = File.Create(Path.Combine(SkillCaptureScene.OutputDirectory, "inventory-surface.png")))
                surface.SaveAsPng(output, surface.Width, surface.Height);
            var side = Client.Main.Content.TextureLoader.Instance.GetTexture2D("Interface/newui_item_back02-L.ozt");
            using(var output = File.Create(Path.Combine(SkillCaptureScene.OutputDirectory, "inventory-side.png")))
                side.SaveAsPng(output, side.Width, side.Height);
        }
        if (_face != null)
            Console.WriteLine($"Preview: {_face.Status}, hidden={_face.Hidden}, position={_face.Position}, scale={_face.Scale}, bounds={_face.BoundingBoxWorld}, camera={Camera.Instance.Position}, target={Camera.Instance.Target}");
        var device = GraphicsManager.Instance.GraphicsDevice;
        var pixels = new Microsoft.Xna.Framework.Color[device.Viewport.Width * device.Viewport.Height];
        device.GetBackBufferData(pixels);
        using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(device, device.Viewport.Width, device.Viewport.Height);
        texture.SetData(pixels);
        using var stream = File.Create(Path.Combine(SkillCaptureScene.OutputDirectory, $"creation-{_capture}.png"));
        texture.SaveAsPng(stream, texture.Width, texture.Height);
    }
}
