using Client.Main;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Effects.Skills;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

public sealed class SkillCaptureScene : BaseScene
{
    private Client.Main.Objects.Effects.DamageTextObject[] _combatAtlasSamples;
    public override bool CanDrawScreenUi => _loaded;
    public static ushort SkillId { get; set; }
    public static bool CaptureLocalTargetImpact { get; set; }
    public static string OutputDirectory { get; set; } = "";
    private SkillCaptureWorld _world;
    private Client.Main.Objects.MonsterObject _captureMonster;
    private Client.Main.Controls.UI.UIControl _captureHelper;
    private WorldObject _effect;
    private Client.Main.Core.Client.BuffManager _rageBuffs;
    private Client.Main.Objects.Effects.RageFighterBuffWindEffect _rageWind;
    private InventoryItem[] _inventoryCaptureItems;
    private int _frame;
    private float _animationFrame;
    private int _warmup;
    private bool _spawned;
    private bool _loaded;
    private bool _failed;
    private bool _actorsPrepared;
    private bool _actorsPreparing;
    private int _lastCapturedFrame = -1;
    private bool _panelPk;
    private static int[] CaptureFrames => SkillId == 0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_AURA")=="1" ? AuraCaptureFrames : SkillId is >= 260 and <= 270 ? RageCaptureFrames : SkillId is 223 or 224 or 225 ? BookCaptureFrames : SkillId is 219 or 221 or 222 ? DebuffCaptureFrames : SkillId == 214 ? DrainCaptureFrames : SkillId == 215 ? ChainCaptureFrames : SkillId == 238 ? ChaoticCaptureFrames : SkillId == 63 ? SummonCaptureFrames : SkillId == 3 ? LightningCaptureFrames : SkillId is 13 or 56 or 77 ? CometCaptureFrames : DefaultCaptureFrames;
    private static readonly int[] AuraCaptureFrames = [1,24,64,156,256,400];
    private static readonly int[] RageCaptureFrames = [1, 5, 12, 24, 48, 156];
    private static readonly int[] BookCaptureFrames = [1, 12, 24, 48, 96, 236];
    private static readonly int[] DebuffCaptureFrames = [1, 5, 12, 24, 48, 82];
    private static readonly int[] DrainCaptureFrames = [1, 5, 12, 24, 48, 116];
    private static readonly int[] ChainCaptureFrames = [1, 5, 12, 24, 48, 76];
    private static readonly int[] ChaoticCaptureFrames = [1, 5, 12, 24, 48, 152];
    private static readonly int[] SummonCaptureFrames = [1, 5, 12, 24, 48, 332];
    private static readonly int[] CometCaptureFrames = [1, 5, 12, 24, 48, 96];
    private static readonly int[] DefaultCaptureFrames = [1, 5, 12, 24, 48, 64];
    private static readonly int[] LightningCaptureFrames = [1, 2, 5, 12, 24, 64];

    public override async Task Load()
    {
        await ChangeWorldAsync<SkillCaptureWorld>();
        _world = (SkillCaptureWorld)World;
        await base.Load();
        if(Environment.GetEnvironmentVariable("TWINMU_PREFERRED_INVENTORY_CAPTURE")=="1")
            typeof(Client.Main.Content.TextureLoader).GetProperty("PreferBundledInventoryArtwork",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.SetValue(null,true);
        if(Environment.GetEnvironmentVariable("TWINMU_BUBBLE_CAPTURE")=="1")
        {
            var bubble=new Client.Main.Objects.Effects.ChatBubbleObject("sdads",1,"Character",60);
            _world.Objects.Add(bubble);await bubble.Load();
            if(Controls.OfType<Client.Main.Controls.UI.LabelControl>().Any(label=>label.Text=="Character"))
                throw new InvalidOperationException("Chat bubble repeated the actor name.");
            Console.WriteLine("PASS: chat bubble adds only the message label, with one existing actor nameplate.");
        }
        MuGame.Instance.ApplyGraphicsConfiguration(new Client.Main.Configuration.GraphicsSettings
        {
            Width = int.Parse(Environment.GetEnvironmentVariable("TWINMU_CAPTURE_WIDTH") ?? "1024"), Height = int.Parse(Environment.GetEnvironmentVariable("TWINMU_CAPTURE_HEIGHT") ?? "768"), UiVirtualWidth = 1024, UiVirtualHeight = 768,
            IsFullScreen = false
        });
        if(Environment.GetEnvironmentVariable("TWINMU_COMBAT_ATLAS_CAPTURE")=="1")
        {
            await Client.Main.Helpers.CombatTextAtlas.LoadAsync();
            _combatAtlasSamples = new[] {
                new Client.Main.Objects.Effects.DamageTextObject("12,345",0,Microsoft.Xna.Framework.Color.Red),
                new Client.Main.Objects.Effects.DamageTextObject("Miss",0,Microsoft.Xna.Framework.Color.DeepSkyBlue),
                new Client.Main.Objects.Effects.DamageTextObject("987M",0,Microsoft.Xna.Framework.Color.Gold) };
            for(int i=0;i<_combatAtlasSamples.Length;i++)
                typeof(Client.Main.Objects.Effects.DamageTextObject).GetField("_screenPosition",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
                    .SetValue(_combatAtlasSamples[i],new Vector2(300,180+i*40));
        }
        Constants.RENDER_SCALE = float.Parse(Environment.GetEnvironmentVariable("TWINMU_NAME_RENDER_SCALE") ?? "1", System.Globalization.CultureInfo.InvariantCulture);
        GraphicsManager.Instance.UpdateRenderScale();
        if(Environment.GetEnvironmentVariable("TWINMU_NATIVE_NAME_CAPTURE")=="1") GraphicsManager.Instance.IsFXAAEnabled=true;
        if(Environment.GetEnvironmentVariable("TWINMU_INVENTORY_SAFETY_CHECK")=="1")
        {
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var state=new Client.Main.Core.Client.CharacterState(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            var network=(Client.Main.Networking.NetworkManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Client.Main.Networking.NetworkManager));
            typeof(Client.Main.Networking.NetworkManager).GetField("_characterState",flags)!.SetValue(network,state);
            var inventory=new Client.Main.Controls.UI.Game.Inventory.InventoryControl(network);
            inventory.Align=0;inventory.X=0;inventory.Y=0;inventory.ControlSize=inventory.ViewSize=new Point(400,600);
            var definition=new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(0,"Unusable armor",2,3){Group=8,RequiredStrength=9999};
            var item=new InventoryItem(definition,Point.Zero);
            inventory._pickedItemRenderer.PickUpItem(item);
            typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl).GetField("_pickedItemOriginalGrid",flags)!.SetValue(inventory,Point.Zero);
            bool placed=(bool)typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl).GetMethod("TryPlacePickedItemIntoEquipSlot",flags)!.Invoke(inventory,new object[]{(byte)3})!;
            if(placed || !ReferenceEquals(inventory.GetDraggedItem(),item))throw new InvalidOperationException("Rejected equipment placement lost the held item.");
            var pointer=typeof(MuGame).GetProperty("UiMousePosition")!;object old=pointer.GetValue(MuGame.Instance)!;
            pointer.SetValue(MuGame.Instance,new Point(50,50));
            typeof(Client.Main.Controls.UI.Game.Inventory.InventoryControl).GetMethod("HandleDropOutsideInventory",flags)!.Invoke(inventory,null);
            pointer.SetValue(MuGame.Instance,old);
            if(!ReferenceEquals(inventory.GetDraggedItem(),item))throw new InvalidOperationException("Empty inventory chrome dropped the held item.");
            inventory.Dispose();
            Console.WriteLine("PASS: unmet equipment requirements and inventory chrome keep the held item without sending a move/drop.");
        }
        if(Environment.GetEnvironmentVariable("TWINMU_LOGIN_CAPTURE") is {} loginCapture)
        {
            UiScaler.Configure(MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth,MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight,1024,768);
            Client.Main.Controls.UI.UiThemeManager.Initialize("Modern");
            var login=new Client.Main.Controls.UI.Login.LoginDialog{Align=ControlAlign.HorizontalCenter|ControlAlign.VerticalCenter};
            Controls.Add(login);await login.Initialize();
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            Client.Main.Controls.UI.Common.ButtonControl Button(string name)=>(Client.Main.Controls.UI.Common.ButtonControl)typeof(Client.Main.Controls.UI.Login.LoginDialog).GetField(name,flags)!.GetValue(login)!;
            Client.Main.Controls.UI.TextFieldControl Field(string name)=>(Client.Main.Controls.UI.TextFieldControl)typeof(Client.Main.Controls.UI.Login.LoginDialog).GetField(name,flags)!.GetValue(login)!;
            if(Button("_loginTab").Interactive || !Button("_registerTab").Interactive)throw new InvalidOperationException("Active login tab is not disabled.");
            login.SetRegistrationMode(true);
            if(!Button("_loginTab").Interactive || Button("_registerTab").Interactive)throw new InvalidOperationException("Active registration tab is not disabled.");
            if(new[]{"_userInput","_emailInput","_passwordInput","_confirmInput"}.Any(name=>!Field(name).Visible || !Field(name).Interactive))throw new InvalidOperationException("Registration does not expose four inputs.");
            Field("_passwordInput").Value="secret";Field("_confirmInput").Value="secret";
            login.SetSubmitting(true);login.SetRegistrationMode(false);
            if(!login.IsRegistration || Button("_classicOkButton").Interactive)throw new InvalidOperationException("Submitting registration can be submitted twice or switched away.");
            login.SetSubmitting(false);Button("_classicCancelButton").OnClick();
            if(login.IsRegistration || Field("_emailInput").Visible || login.Password.Length!=0 || login.Confirmation.Length!=0)throw new InvalidOperationException("Cancel did not return to login and clear registration passwords.");
            if(loginCapture=="register")login.SetRegistrationMode(true);
            Console.WriteLine("PASS: Login/Register tabs disable the active tab; four fields, busy guard and Cancel work.");
            var authScene=new LoginScene();
            typeof(LoginScene).GetField("_networkManager",flags)!.SetValue(authScene,MuGame.Network);
            typeof(LoginScene).GetField("_previousStateHandled",flags)!.SetValue(authScene,Client.Main.Core.Client.ClientConnectionState.ConnectedToGameServer);
            var authDialog=(Client.Main.Controls.UI.Login.LoginDialog)typeof(LoginScene).GetField("_loginDialog",flags)!.GetValue(authScene)!;
            authDialog.SetRegistrationMode(true);
            typeof(LoginScene).GetMethod("HandleConnectionStateChange",flags)!.Invoke(authScene,new object[]{MuGame.Network,Client.Main.Core.Client.ClientConnectionState.Disconnected});
            typeof(LoginScene).GetMethod("HandleNetworkError",flags)!.Invoke(authScene,new object[]{MuGame.Network,"Login timeout"});
            await MuGame.YieldToNextFrameAsync("Registration.TimeoutRegression",MainThreadDispatcher.WorkPriority.Low);
            if(!authDialog.Visible || !authDialog.IsRegistration || authScene.Controls.OfType<Client.Main.Controls.UI.MessageWindow>().Any(m=>m.Visible))
                throw new InvalidOperationException($"Login timeout regression: visible={authDialog.Visible}, registration={authDialog.IsRegistration}, messages={authScene.Controls.OfType<Client.Main.Controls.UI.MessageWindow>().Count(m=>m.Visible)}.");
            authScene.Dispose();
            Console.WriteLine("PASS: simulated login disconnect/timeout preserves registration without an exit dialog.");
        }
        if(Environment.GetEnvironmentVariable("TWINMU_MOBILE_INTERFACE_CAPTURE") is { } mobileCapture)
        {
            UiScaler.Configure(MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth,MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight,1024,768);
            Client.Main.Controls.UI.UiThemeManager.Initialize("Classic");
            if(mobileCapture=="pickup")
            {
                int pickupRequests=0;
                var pickup=new Client.Main.Controls.UI.Game.Hud.MobilePickupControl(()=>pickupRequests++);Controls.Add(pickup);await pickup.Initialize();
                var mouseProperty=typeof(MuGame).GetProperty(nameof(MuGame.UiMouseState))!;
                var previousProperty=typeof(MuGame).GetProperty(nameof(MuGame.PrevUiMouseState))!;
                var savedMouse=MuGame.Instance.UiMouseState;var savedPrevious=MuGame.Instance.PrevUiMouseState;
                var actionButtons=new Client.Main.Controls.UI.Game.Hud.TouchActionButtonsControl();Controls.Add(actionButtons);await actionButtons.Initialize();
                var center=Client.Main.Controls.UI.Game.Hud.MobilePickupControl.ButtonBounds.Center;
                var find=typeof(BaseScene).GetMethod("FindTopmostUiControlsAtPoint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                void Route(Point point,Client.Main.Controls.GameControl expected)
                {
                    object[] arguments={point,null!,null!};find.Invoke(this,arguments);
                    if(!ReferenceEquals(arguments[1],expected))throw new InvalidOperationException($"HUD tap blocked by {arguments[1]?.GetType().Name} instead of {expected.GetType().Name}.");
                    MouseHoverControl=expected;
                    typeof(BaseScene).GetField("_pointerGestureControl",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(this,expected);
                }
                Route(center,pickup);
                var released=new Microsoft.Xna.Framework.Input.MouseState(center.X,center.Y,0,Microsoft.Xna.Framework.Input.ButtonState.Released,0,0,0,0);
                var pressed=new Microsoft.Xna.Framework.Input.MouseState(center.X,center.Y,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,0,0,0,0);
                previousProperty.SetValue(MuGame.Instance,released);mouseProperty.SetValue(MuGame.Instance,pressed);
                pickup.Update(new GameTime());
                if(pickupRequests!=1)throw new InvalidOperationException("Pickup tap did not dispatch a pickup request.");
                previousProperty.SetValue(MuGame.Instance,pressed);mouseProperty.SetValue(MuGame.Instance,released);
                pickup.Update(new GameTime());
                if(pickupRequests!=1)throw new InvalidOperationException("Pickup release dispatched a duplicate request.");
                mouseProperty.SetValue(MuGame.Instance,savedMouse);previousProperty.SetValue(MuGame.Instance,savedPrevious);
                Console.WriteLine("PASS: pickup press dispatches once; release does not repeat.");
                var hud=new Client.Main.Controls.UI.Game.Hud.ModernBottomHud(MuGame.Network.GetCharacterState(),new Client.Main.Controls.UI.Game.Skills.SkillSelectionPanel());
                hud.GetConsumableCandidates();
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var candidates=(System.Collections.IList)typeof(Client.Main.Controls.UI.Game.Hud.ModernBottomHud).GetField("_consumableCandidateView",flags)!.GetValue(hud)!;
                candidates.Clear();candidates.Add(((byte)14,0,"Small Healing Potion",(string?)null,10));
                for(int i=1;i<8;i++)candidates.Add(((byte)14,i,$"Potion {i}",(string?)null,10));
                var potionBar=new Client.Main.Controls.UI.Game.Hud.BottomBarControl(MuGame.Network.GetCharacterState(),hud);
                Controls.Add(potionBar);await potionBar.Initialize();actionButtons.BringToFront();
                var mapMenu=new Client.Main.Controls.UI.Game.Hud.TouchMenuControl();
                Controls.Add(mapMenu);await mapMenu.Initialize();mapMenu.BringToFront();
                MuGame.AppSettings.MobilePotionsRight=true;
                var slot=(Rectangle)typeof(Client.Main.Controls.UI.Game.Hud.BottomBarControl).GetMethod("GetPotionSlotRectangle",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.Invoke(null,new object[]{0})!;
                potionBar.Update(new GameTime());Route(slot.Center,potionBar);
                void TapPotion(Point point)
                {
                    Route(point,potionBar);
                    previousProperty.SetValue(MuGame.Instance,new Microsoft.Xna.Framework.Input.MouseState(point.X,point.Y,0,Microsoft.Xna.Framework.Input.ButtonState.Released,0,0,0,0));
                    mouseProperty.SetValue(MuGame.Instance,new Microsoft.Xna.Framework.Input.MouseState(point.X,point.Y,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,0,0,0,0));
                    potionBar.Update(new GameTime());
                }
                TapPotion(slot.Center);
                if((int)typeof(Client.Main.Controls.UI.Game.Hud.BottomBarControl).GetField("_potionPickerSlot",flags)!.GetValue(potionBar)!=0)throw new InvalidOperationException("Potion tap did not open chooser.");
                var rows=(Rectangle[])typeof(Client.Main.Controls.UI.Game.Hud.BottomBarControl).GetField("_potionPickerItems",flags)!.GetValue(potionBar)!;
                if(!Client.Main.Controls.UI.Game.Hud.TouchMenuControl.MobileMapRectangle.Contains(rows[0].Center))
                    throw new InvalidOperationException("Potion regression must overlap the minimap.");
                TapPotion(rows[0].Center);
                mapMenu.Update(new GameTime());
                if(hud.GetItemAssignmentAt(0)!=((byte)14,0))throw new InvalidOperationException("Potion selection did not assign the chosen item.");
                TapPotion(slot.Center);
                if((int)typeof(Client.Main.Controls.UI.Game.Hud.BottomBarControl).GetField("_potionPickerSlot",flags)!.GetValue(potionBar)!=-1 || hud.GetItemAssignmentAt(0)!=((byte)14,0))
                    throw new InvalidOperationException("Occupied potion slot reopened the picker or replaced its assignment.");
                for(int i=1;i<5;i++)hud.SetItemAssignmentAt(i,((byte)14,i));
                var reset=(Rectangle)typeof(Client.Main.Controls.UI.Game.Hud.BottomBarControl).GetProperty("PotionResetBounds",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
                TapPotion(reset.Center);
                if(Enumerable.Range(0,5).Any(i=>hud.GetItemAssignmentAt(i).HasValue))throw new InvalidOperationException("Potion reset did not clear every assignment.");
                TapPotion(slot.Center);
                if((int)typeof(Client.Main.Controls.UI.Game.Hud.BottomBarControl).GetField("_potionPickerSlot",flags)!.GetValue(potionBar)!=0)throw new InvalidOperationException("Reset potion slot cannot be assigned again.");
                TapPotion(((Rectangle[])typeof(Client.Main.Controls.UI.Game.Hud.BottomBarControl).GetField("_potionPickerItems",flags)!.GetValue(potionBar)!)[0].Center);
                Console.WriteLine("PASS: occupied mobile potion slots retain assignment; R clears all five saved slots and permits reassignment.");
                mouseProperty.SetValue(MuGame.Instance,savedMouse);previousProperty.SetValue(MuGame.Instance,savedPrevious);
                MouseHoverControl=null;typeof(BaseScene).GetField("_pointerGestureControl",flags)!.SetValue(this,null);
                Console.WriteLine("PASS: stacked HUD routes pickup and potion taps; chooser opens and assigns selected potion.");
                Console.WriteLine("PASS: potion chooser captures selection over the minimap behind it.");
                mapMenu.Dispose();
                var previewScene=(GameScene)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameScene));
                var previewHero=(Client.Main.Objects.Player.HeroObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Client.Main.Objects.Player.HeroObject));
                typeof(GameScene).GetField("_hero",flags)!.SetValue(previewScene,previewHero);
                typeof(BaseScene).GetProperty("World")!.SetValue(previewScene,_world);
                var combatVisible=typeof(GameScene).GetProperty("MobileCombatControlsVisible",flags)!;
                var locationField=typeof(Client.Main.Objects.WalkerObject).GetField("_location",flags)!;
                bool sawSafe=false,sawBattle=false;
                for(int y=0;y<256 && !(sawSafe && sawBattle);y++)for(int x=0;x<256 && !(sawSafe && sawBattle);x++)
                {
                    bool safe=_world.Terrain.RequestTerrainFlag(x,y).HasFlag(Client.Data.ATT.TWFlags.SafeZone);
                    if(safe?sawSafe:sawBattle)continue;
                    locationField.SetValue(previewHero,new Vector2(x,y));
                    if((bool)combatVisible.GetValue(previewScene)! == safe)throw new InvalidOperationException("Combat HUD visibility does not follow safe-zone terrain flags.");
                    if(safe)sawSafe=true;else sawBattle=true;
                }
                if(!sawSafe || !sawBattle)throw new InvalidOperationException("Safe-zone fixture did not cover both terrain types.");
                Console.WriteLine("PASS: combat HUD visibility switches between actual safe-zone and battle terrain tiles.");
                MuGame.AppSettings.MobilePotionsRight=false;
                var fixedBounds=Client.Main.Controls.UI.Game.Hud.MobilePickupControl.ButtonBounds;
                MuGame.AppSettings.MobilePotionsRight=true;
                if(fixedBounds!=Client.Main.Controls.UI.Game.Hud.MobilePickupControl.ButtonBounds)
                    throw new InvalidOperationException("Pickup button follows the potion-side setting.");
                Console.WriteLine("PASS: pickup button stays fixed on the right when potion placement changes.");
            }
            if(mobileCapture=="menu")
            {
                var menu=new Client.Main.Controls.UI.Game.Hud.TouchMenuControl();Controls.Add(menu);await menu.Initialize();menu.Open();
                Controls.Add(new Client.Main.Controls.UI.Game.Hud.BatteryStatusControl());
                var layout=typeof(Client.Main.Controls.UI.Game.Hud.TouchMenuControl).GetMethod("LayoutRects",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var rectField=typeof(Client.Main.Controls.UI.Game.Hud.TouchMenuControl).GetField("_rects",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var menuProgress=typeof(Client.Main.Controls.UI.Game.Hud.TouchMenuControl).GetField("_menuProgress",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                menuProgress.SetValue(menu,1f);
                foreach(int width in new[]{480,640,800,1024,1280,1600,2400})
                {
                    UiScaler.Configure(width,768,width,768);layout.Invoke(menu,null);
                    var rectangles=(Microsoft.Xna.Framework.Rectangle[])rectField.GetValue(menu)!;
                    var profile=Client.Main.Controls.UI.Game.Hud.MobilePlayerPanel.LayoutBounds;
                    var map=Client.Main.Controls.UI.Game.Hud.TouchMenuControl.MobileMapRectangle;
                    var toggle=(Microsoft.Xna.Framework.Rectangle)typeof(Client.Main.Controls.UI.Game.Hud.TouchMenuControl).GetField("_menuRect",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(menu)!;
                    menuProgress.SetValue(menu,0f);layout.Invoke(menu,null);
                    var closedToggle=(Microsoft.Xna.Framework.Rectangle)typeof(Client.Main.Controls.UI.Game.Hud.TouchMenuControl).GetField("_menuRect",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(menu)!;
                    menuProgress.SetValue(menu,1f);layout.Invoke(menu,null);
                    if(toggle!=closedToggle || rectangles.Any(r=>r.Intersects(toggle)))
                        throw new InvalidOperationException("Menu toggle overlaps the adjacent shortcut list or HUD.");
                    if(rectangles.Select(r=>r.Y).Distinct().Count()>3)throw new InvalidOperationException("Responsive menu exceeds three rows.");
                    for(int i=0;i<rectangles.Length;i++)
                    {
                        if(rectangles[i].Width<24 || !UiScaler.WindowBounds.Contains(rectangles[i]) || rectangles[i].Intersects(profile) || rectangles[i].Intersects(map))
                            throw new InvalidOperationException($"Menu intersects a top HUD or screen edge at width {width}.");
                        for(int j=0;j<i;j++)if(rectangles[i].Intersects(rectangles[j]))throw new InvalidOperationException("Menu icons overlap.");
                    }
                    Console.WriteLine($"PASS: width {width}: {rectangles.Select(r=>r.X).Distinct().Count()} columns, {rectangles.Select(r=>r.Y).Distinct().Count()} rows; no HUD overlap.");
                }
                UiScaler.Configure(MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth,MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight,1024,768);layout.Invoke(menu,null);
            }
            var log=new Client.Main.Controls.UI.ChatLogWindow();Controls.Add(log);await log.Initialize();
            log.ShowFrame(true);log.SetShowingLines(mobileCapture=="expanded"?15:6);
            typeof(Client.Main.Controls.UI.ChatLogWindow).GetMethod("AnchorAbove",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(log,new object[]{Client.Main.Controls.UI.Game.Hud.MobileHudLayout.ChatX,Client.Main.Controls.UI.Game.Hud.MobileHudLayout.ChatBottom});
            if(mobileCapture=="expanded")
            {
                log.SetShowingLines(6);
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var handle=(Microsoft.Xna.Framework.Rectangle)typeof(Client.Main.Controls.UI.ChatLogWindow).GetField("_resizeHandleArea",flags)!.GetValue(log)!;
                var resize=typeof(Client.Main.Controls.UI.ChatLogWindow).GetMethod("HandleResizeInteraction",flags)!;
                Microsoft.Xna.Framework.Input.MouseState Mouse(int y,Microsoft.Xna.Framework.Input.ButtonState button)=>new(handle.Center.X,y,0,button,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
                var press=Mouse(handle.Center.Y,Microsoft.Xna.Framework.Input.ButtonState.Pressed);
                resize.Invoke(log,new object[]{press,Mouse(handle.Center.Y,Microsoft.Xna.Framework.Input.ButtonState.Released)});
                resize.Invoke(log,new object[]{Mouse(handle.Center.Y-150,Microsoft.Xna.Framework.Input.ButtonState.Pressed),press});
                if(log.NumberOfShowingLines<=6)throw new InvalidOperationException("Upward chat drag did not increase its height.");
                if(log.Y+log.ViewSize.Y!=Client.Main.Controls.UI.Game.Hud.MobileHudLayout.ChatBottom)throw new InvalidOperationException("Chat bottom moved while resizing.");
                Console.WriteLine("PASS: upward chat drag expands while keeping its bottom anchored.");
            }
            for(int i=0;i<18;i++)log.AddMessage(i%2==0?"Player":"TwinMU",$"Preview message {i+1}: Welcome to Lorencia",i%3==0?MessageType.System:MessageType.Chat);
            log.AddMessage("", "Obtained 5,175 Exp", MessageType.Experience);
            log.AddMessage("", "4,200 Zen Obtained", MessageType.Loot);
            log.AddMessage("", "Battle Two-Hand Axe Obtained", MessageType.Loot);
            log.AddMessage("", "Screenshot saved", MessageType.System);
            log.AddMessage("Friend", "Whisper preview", MessageType.Whisper);
            log.AddMessage("Party", "Party preview", MessageType.Party);
            log.AddMessage("Guild", "Guild preview", MessageType.Guild);
            log.AddMessage("World", "World preview", MessageType.World);
            const System.Reflection.BindingFlags logFlags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var messageLists=(System.Collections.Generic.Dictionary<MessageType,System.Collections.Generic.List<ChatMessage>>)typeof(Client.Main.Controls.UI.ChatLogWindow).GetField("_messages",logFlags)!.GetValue(log)!;
            if(messageLists[MessageType.All].Any(message=>message.Type is MessageType.System or MessageType.Loot or MessageType.Experience))
                throw new InvalidOperationException("System notices leaked into conversation chat.");
            Console.WriteLine("PASS: experience, loot and system messages are separate from conversation chat.");
            if(mobileCapture=="closed")
            {
                log.ShowFrame(false);
                log.Update(new GameTime(TimeSpan.Zero,TimeSpan.Zero));
                if(log.IsFrameVisible)throw new InvalidOperationException("Mobile update forced closed chat open.");
                var scrollbar=(Rectangle)typeof(Client.Main.Controls.UI.ChatLogWindow).GetField("_scrollBarArea",logFlags)!.GetValue(log)!;
                if(scrollbar!=Rectangle.Empty)throw new InvalidOperationException("Closed chat retained a scrollbar hit area.");
                Console.WriteLine("PASS: closed mobile chat stays closed and has no scrollbar.");
            }
            if(mobileCapture=="composer")
            {
                var input=new Client.Main.Controls.UI.ChatInputBoxControl(log,MuGame.AppLoggerFactory);
                Controls.Add(input);await input.Initialize();input.Show(focusInput:false);
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var field=(Client.Main.Controls.UI.TextFieldControl)typeof(Client.Main.Controls.UI.ChatInputBoxControl).GetField("_chatInput",flags)!.GetValue(input)!;
                string? sent=null;input.MessageSendRequested+=(_,args)=>sent=args.Message;
                foreach(var test in new[]{(InputMessageType.Chat,"hello","hello"),(InputMessageType.Party,"hello","~hello"),(InputMessageType.Guild,"hello","@hello"),(InputMessageType.Gens,"hello","$hello"),(InputMessageType.Chat,"@@hello","@@hello")})
                {
                    input.Show(focusInput:false);field.Value=test.Item2;
                    typeof(Client.Main.Controls.UI.ChatInputBoxControl).GetField("_currentInputType",flags)!.SetValue(input,test.Item1);
                    typeof(Client.Main.Controls.UI.ChatInputBoxControl).GetField("_lastChatTime",flags)!.SetValue(input,0L);
                    typeof(Client.Main.Controls.UI.ChatInputBoxControl).GetMethod("ProcessEnterKey",flags)!.Invoke(input,null);
                    if(sent!=test.Item3)throw new InvalidOperationException($"Chat route sent {sent}, expected {test.Item3}.");
                }
                input.Show(focusInput:false);
                Console.WriteLine("PASS: normal, party, guild, Gens, and explicit alliance chat preserve server routing prefixes.");
                input.X=log.X;input.Y=log.Y-Client.Main.Controls.UI.ChatInputBoxControl.MobileComposerHeight;
                var focusMouseProperty=typeof(MuGame).GetProperty(nameof(MuGame.UiMouseState))!;
                var focusSavedPointer=MuGame.Instance.UiMouseState;
                try
                {
                    field.Blur();FocusControl=null;
                    var tab=(Rectangle)typeof(Client.Main.Controls.UI.ChatInputBoxControl).GetMethod("MobileTabBounds",flags)!.Invoke(input,new object[]{0})!;
                    focusMouseProperty.SetValue(MuGame.Instance,new Microsoft.Xna.Framework.Input.MouseState(tab.Center.X,tab.Center.Y,0,0,0,0,0,0));
                    input.OnClick();if(field.IsFocused)throw new Exception("Chat tab focused the text field.");
                    var textBounds=field.DisplayRectangle;
                    focusMouseProperty.SetValue(MuGame.Instance,new Microsoft.Xna.Framework.Input.MouseState(textBounds.Center.X,textBounds.Center.Y,0,0,0,0,0,0));
                    input.OnClick();if(!field.IsFocused)throw new Exception("Text input tap did not focus the field.");
                    field.Blur();FocusControl=null;
                    Console.WriteLine("PASS: chat tabs do not focus text input; tapping the input does.");
                }
                finally{focusMouseProperty.SetValue(MuGame.Instance,focusSavedPointer);}
                var handle=(Rectangle)typeof(Client.Main.Controls.UI.ChatLogWindow).GetField("_resizeHandleArea",flags)!.GetValue(log)!;
                if(handle.IsEmpty || handle.Intersects(input.DisplayRectangle) || handle.Bottom>input.DisplayRectangle.Top)
                    throw new InvalidOperationException("Composer covers the mobile resize handle.");
                var find=typeof(BaseScene).GetMethod("FindTopmostUiControlsAtPoint",flags)!;
                object[] hit={handle.Center,null!,null!};find.Invoke(this,hit);
                if(!ReferenceEquals(hit[1],log))throw new InvalidOperationException("Resize handle does not receive the pointer above the composer.");
                var mouse=typeof(MuGame).GetProperty(nameof(MuGame.UiMouseState))!;
                var previous=typeof(MuGame).GetProperty(nameof(MuGame.PrevUiMouseState))!;
                var savedMouse=MuGame.Instance.UiMouseState;var savedPrevious=MuGame.Instance.PrevUiMouseState;
                var gesture=typeof(BaseScene).GetField("_pointerGestureControl",flags)!;
                object? savedGesture=gesture.GetValue(this);var savedHover=MouseHoverControl;
                Microsoft.Xna.Framework.Input.MouseState Pointer(int y,Microsoft.Xna.Framework.Input.ButtonState button)=>new(handle.Center.X,y,0,button,0,0,0,0);
                try
                {
                    MouseHoverControl=log;gesture.SetValue(this,log);
                    int lines=log.NumberOfShowingLines;
                    var release=Pointer(handle.Center.Y,Microsoft.Xna.Framework.Input.ButtonState.Released);
                    var press=Pointer(handle.Center.Y,Microsoft.Xna.Framework.Input.ButtonState.Pressed);
                    previous.SetValue(MuGame.Instance,release);mouse.SetValue(MuGame.Instance,press);log.Update(new GameTime());
                    previous.SetValue(MuGame.Instance,press);mouse.SetValue(MuGame.Instance,Pointer(handle.Center.Y-120,Microsoft.Xna.Framework.Input.ButtonState.Pressed));log.Update(new GameTime());
                    if(log.NumberOfShowingLines<=lines)throw new InvalidOperationException("Dragging resize bar with composer open did not expand chat.");
                    mouse.SetValue(MuGame.Instance,Pointer(handle.Center.Y-120,Microsoft.Xna.Framework.Input.ButtonState.Released));log.Update(new GameTime());
                    input.Y=log.Y-Client.Main.Controls.UI.ChatInputBoxControl.MobileComposerHeight;
                    handle=(Rectangle)typeof(Client.Main.Controls.UI.ChatLogWindow).GetField("_resizeHandleArea",flags)!.GetValue(log)!;
                    if(handle.Intersects(input.DisplayRectangle))throw new InvalidOperationException("Resize bar overlapped composer after dragging.");
                    Console.WriteLine("PASS: blue resize bar is above composer, receives pointer input, and expands chat through Update.");
                }
                finally
                {
                    mouse.SetValue(MuGame.Instance,savedMouse);previous.SetValue(MuGame.Instance,savedPrevious);
                    gesture.SetValue(this,savedGesture);MouseHoverControl=savedHover;
                }
            }
            Client.Main.Controls.UI.Game.Hud.ModernBottomHud? potionHud=null;
            if(Environment.GetEnvironmentVariable("TWINMU_OCCUPIED_POTIONS_CAPTURE")=="1")
            {
                potionHud=new Client.Main.Controls.UI.Game.Hud.ModernBottomHud(MuGame.Network.GetCharacterState(),new Client.Main.Controls.UI.Game.Skills.SkillSelectionPanel());
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var assignments=((byte Group,int Id)?[])typeof(Client.Main.Controls.UI.Game.Hud.ModernBottomHud).GetField("_potionAssignments",flags)!.GetValue(potionHud)!;
                for(int i=0;i<3;i++)assignments[i]=((byte)14,i);
                // Set only the fixture's in-memory assignments; never persist test quick slots.
            }
            var bar=new Client.Main.Controls.UI.Game.Hud.BottomBarControl(MuGame.Network.GetCharacterState(),potionHud);
            Controls.Add(bar);await bar.Initialize();
            if(mobileCapture=="travel")
            {
                var travel=new Client.Main.Controls.UI.Game.MoveCommandWindow(MuGame.AppLoggerFactory,MuGame.Network);
                Controls.Add(travel);await travel.Initialize();travel.ToggleVisibility();
            }
            if(mobileCapture.StartsWith("helper-"))
            {
                var assembly=typeof(MuGame).Assembly;
                var controllerType=assembly.GetType("Client.Main.Scenes.MuHelperController")!;
                object controller=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(controllerType);
                var configuration=new Client.Main.Configuration.MuHelperConfig{BasicSkillId=11};
                controllerType.GetMethod("Load")!.Invoke(controller,new object[]{configuration});
                var helperType=assembly.GetType("Client.Main.Controls.UI.Game.Helper.MobileHelperSettingsControl")!;
                var helper=(Client.Main.Controls.UI.UIControl)Activator.CreateInstance(helperType,new[]{controller})!;
                _captureHelper=helper;
                Controls.Add(helper);helperType.GetMethod("Toggle")!.Invoke(helper,null);
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var draft=(Client.Main.Configuration.MuHelperConfig)helperType.GetField("_draft",flags)!.GetValue(helper)!;
                draft.HuntingRange=12;
                helperType.GetMethod("Cancel",flags)!.Invoke(helper,null);
                var actual=(Client.Main.Configuration.MuHelperConfig)controllerType.GetProperty("Config")!.GetValue(controller)!;
                if(actual.HuntingRange!=configuration.HuntingRange)throw new InvalidOperationException("Cancel changed saved Helper settings.");
                helperType.GetMethod("Toggle")!.Invoke(helper,null);
                helperType.GetField("_tab",flags)!.SetValue(helper,mobileCapture=="helper-recovery"?1:mobileCapture=="helper-loot"?2:0);
                Console.WriteLine("PASS: mobile Helper Cancel preserves the saved configuration.");
            }
        }
        if(Environment.GetEnvironmentVariable("TWINMU_INVENTORY_LAYOUT_CAPTURE") is { } inventoryCapture)
        {
            UiScaler.Configure(MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth,MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight,1280,720);
            var state=MuGame.Network.GetCharacterState();
            state.UpdateInventoryZen(16258324);
            byte[] Item(int group,int id,int level,int count)=>new byte[]{(byte)((group<<4)|(id>>8)),(byte)id,(byte)level,(byte)count,0};
            state.AddOrUpdateInventoryItem(12,Item(14,1,0,2));
            state.AddOrUpdateInventoryItem(14,Item(14,11,8,1));
            state.AddOrUpdateInventoryItem(16,Item(8,0,0,100));
            if(Environment.GetEnvironmentVariable("TWINMU_EQUIPMENT_PREVIEW_CAPTURE")=="1")
            {
                foreach(var entry in new (byte Slot,int Group,int Id)[]{(0,0,0),(1,6,0),(2,7,29),(3,8,29),(4,9,29),(5,10,29),(6,11,29),(7,12,0)})
                    state.AddOrUpdateInventoryItem(entry.Slot,Item(entry.Group,entry.Id,15,100));
                state.AddOrUpdateInventoryItem(16,Item(8,29,15,100));
                state.AddOrUpdateInventoryItem(20,Item(0,0,15,100));
                state.AddOrUpdateInventoryItem(22,Item(2,0,15,100));
                state.AddOrUpdateInventoryItem(24,Item(6,0,15,100));
            }
            var inventory=new Client.Main.Controls.UI.Game.Inventory.InventoryControl(MuGame.Network);
            Controls.Add(inventory);await inventory.Initialize();inventory.Show();
            inventory.Align=0;inventory.X=UiScaler.WindowBounds.Right-inventory.ViewSize.X;inventory.Y=0;
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var images=(System.Collections.IDictionary)inventory.GetType().GetField("_nativeInventoryImages",flags)!.GetValue(inventory)!;
            var header=(Microsoft.Xna.Framework.Graphics.Texture2D)images["newui_item_back04"]!;
            using(var file=File.Create(Path.Combine(OutputDirectory,"inventory-header.png")))header.SaveAsPng(file,header.Width,header.Height);
            Rectangle Rect(string field)=>(Rectangle)inventory.GetType().GetField(field,flags)!.GetValue(inventory)!;
            bool validateLayout=Environment.GetEnvironmentVariable("TWINMU_LAYOUT_PREVIEW")!="1";
            if(validateLayout && (Rect("_gridRect").X!=Client.Main.Controls.UI.Game.Hud.InventoryLayout.EquipmentX || Rect("_gridRect").Width!=Client.Main.Controls.UI.Game.Hud.InventoryLayout.EquipmentW))
                throw new Exception("Bag grid does not align with the equipment area.");
            if(validateLayout && (Rect("_zenFieldRect").Intersects(Rect("_nativeFooterCloseRect")) || Rect("_zenFieldRect").Intersects(Rect("_repairOptionRect")) || Rect("_gridRect").Intersects(Rect("_zenFieldRect"))))
                throw new Exception("Inventory grid, Zen and footer overlap.");
            var slots=(System.Collections.IDictionary)inventory.GetType().GetField("_equipSlots",flags)!.GetValue(inventory)!;
            Rectangle Equip(byte slot)=>(Rectangle)slots[slot]!.GetType().GetProperty("Rect")!.GetValue(slots[slot])!;
            if(validateLayout && (Equip(2).Intersects(Equip(3)) || Client.Main.Controls.UI.Game.Hud.InventoryLayout.ArmorAssetH>Equip(3).Height))
                throw new Exception("Armor artwork covers the helmet slot.");
            state.AddOrUpdateShopItem(0,Item(14,1,0,3));
            state.AddOrUpdateShopItem(1,Item(14,4,0,3));
            var shop=Client.Main.Controls.UI.Game.NpcShopControl.Instance;
            Controls.Add(shop);await shop.Initialize();
            shop.GetType().GetMethod("RefreshShopContent",flags)!.Invoke(shop,null);
            var items=(System.Collections.IList)shop.GetType().GetField("_items",flags)!.GetValue(shop)!;
            if(items.Count!=2 || ((InventoryItem)items[0]!).Durability!=3)throw new Exception("Merchant did not use the server's stack count.");
            if(inventoryCapture=="quantity")
            {
                var dialog=Client.Main.Controls.UI.RequestDialog.ShowQuantity(q=>$"Small Healing Potion\n{q} stacks x 3 = {q*3} items\nTotal: {q*30:N0} Zen",_=>{});
                var minus=(Client.Main.Controls.UI.Common.ButtonControl)typeof(Client.Main.Controls.UI.RequestDialog).GetField("_quantityMinus",flags)!.GetValue(dialog)!;
                var plus=(Client.Main.Controls.UI.Common.ButtonControl)typeof(Client.Main.Controls.UI.RequestDialog).GetField("_quantityPlus",flags)!.GetValue(dialog)!;
                if(minus.OnClick() || dialog.Quantity!=1)throw new Exception("Disabled minus button changed quantity.");
                plus.OnClick();if(dialog.Quantity!=2)throw new Exception("Plus button did not increase quantity.");
                minus.OnClick();if(dialog.Quantity!=1)throw new Exception("Minus button did not decrease quantity.");
                var change=typeof(Client.Main.Controls.UI.RequestDialog).GetMethod("ChangeQuantity",flags)!;
                change.Invoke(dialog,new object[]{-1});if(dialog.Quantity!=1)throw new Exception("Purchase quantity went below one.");
                change.Invoke(dialog,new object[]{200});if(dialog.Quantity!=99)throw new Exception("Purchase quantity exceeded its limit.");
                change.Invoke(dialog,new object[]{-97});if(dialog.Quantity!=2)throw new Exception("Purchase stepper did not update quantity.");
            }
            Console.WriteLine("PASS: inventory separates grid/Zen/footer; merchant shows 3 while inventory holds 2; quantity stepper clamps correctly.");
        }
        if(Environment.GetEnvironmentVariable("TWINMU_UPDATE_NOTICE_CAPTURE") is { } noticeCapture)
        {
            var selection=new SelectCharacterScene(new(),MuGame.Network);
            const System.Reflection.BindingFlags selectionFlags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var selectionProgress=(Client.Main.Controls.UI.ProgressBarControl)typeof(SelectCharacterScene).GetField("_progressBar",selectionFlags)!.GetValue(selection)!;
            if(selectionProgress.Visible)throw new Exception("Character selection starts with an orphaned 0% progress bar.");
            selectionProgress.Visible=true;
            typeof(SelectCharacterScene).GetMethod("EnableInteractionAfterSelection",selectionFlags)!.Invoke(selection,null);
            if(selectionProgress.Visible)throw new Exception("Retrying character selection left the progress bar visible.");
            selection.Dispose();
            Console.WriteLine("PASS: character selection hides the separate progress control initially and after cancelled selection.");
            int downloads=0;
            var offer=new Client.Main.Helpers.AppUpdateOffer("Preview","Preview only - no download will start.\nUpdated mobile inventory artwork.\nImproved text sizes on mobile.\nKeyboard opens only for text inputs.\nBackground update checks while playing.\nTap Later to keep playing.\nRelease notes can be scrolled.\nAdditional note one.\nAdditional note two.\nAdditional note three.",250*1048576L,()=>downloads++);
            Client.Main.Helpers.AppUpdateNotice.Clear();
            var notice=new Client.Main.Controls.UI.Game.Hud.MobileUpdateControl();Controls.Add(notice);await notice.Initialize();notice.Update(new GameTime());
            if(notice.Visible)throw new Exception("Update icon appeared without an available update.");
            Client.Main.Helpers.AppUpdateNotice.Publish(offer);notice.Update(new GameTime());
            if(!notice.Visible || downloads!=0)throw new Exception("Availability check downloaded automatically or hid the icon.");
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var mouseProperty=typeof(MuGame).GetProperty(nameof(MuGame.UiMouseState))!;var saved=MuGame.Instance.UiMouseState;
            void ClickDialog(string button)
            {
                var dialog=Controls.OfType<Client.Main.Controls.UI.DialogControl>().Last();
                var rect=(Rectangle)dialog.GetType().GetField(button,flags)!.GetValue(dialog)!;
                var point=rect.Center+dialog.DisplayRectangle.Location;
                dialog.GetType().GetField("_pressOrigin",flags)!.SetValue(dialog,point);
                mouseProperty.SetValue(MuGame.Instance,new Microsoft.Xna.Framework.Input.MouseState(point.X,point.Y,0,0,0,0,0,0));
                dialog.OnClick();
            }
            try
            {
                notice.OnClick();ClickDialog("_later");
                if(downloads!=0 || Client.Main.Helpers.AppUpdateNotice.Current==null)throw new Exception("Later started a download or removed the offer.");
                notice.OnClick();
                Client.Main.Helpers.AppUpdateNotice.Publish(offer with {UpdateNow=()=>downloads+=10});
                ClickDialog("_update");if(downloads!=10)throw new Exception("Update now did not use the latest checked release.");
                Client.Main.Helpers.AppUpdateNotice.Publish(offer);
                if(noticeCapture=="details")notice.OnClick();
                Console.WriteLine("PASS: update checks and Later never download; Update now uses the latest release callback.");
            }
            finally{mouseProperty.SetValue(MuGame.Instance,saved);}
        }
        if(Environment.GetEnvironmentVariable("TWINMU_LOADING_TEXT_CAPTURE")=="assets")
        {
            var progress=new Client.Main.Controls.UI.ProgressBarControl{Progress=.42f,StatusText="Downloading game assets...",SizeText="105.0 / 250.0 MB",SpeedText="2.40 MB/s",EtaText="About 1 minute remaining",ShowStats=true};
            Controls.Add(progress);await progress.Initialize();
        }
        if(Environment.GetEnvironmentVariable("TWINMU_MOBILE_TARGET_CAPTURE") is { } targetCapture)
        {
            UiScaler.Configure(MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth,MuGame.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight,1024,768);
            Client.Main.Controls.UI.UiThemeManager.Initialize("Classic");
            if(targetCapture.StartsWith("monster"))
            {
                if(targetCapture.EndsWith("-low"))
                {
                    Constants.ENABLE_DYNAMIC_LIGHTING_SHADER=false;
                    Constants.ENABLE_TERRAIN_GPU_LIGHTING=false;
                }
                _captureMonster=targetCapture.StartsWith("monster-icequeen")
                    ? new Client.Main.Objects.Monsters.IceQueen{NetworkId=77,Location=SkillCaptureWorld.CasterTile+new Vector2(-2,2)}
                    : targetCapture.StartsWith("monster-queen")
                    ? new Client.Main.Objects.Monsters.QueenRainier{NetworkId=77,Location=SkillCaptureWorld.CasterTile+new Vector2(-2,2)}
                    : new Client.Main.Objects.Monsters.Giant{NetworkId=77,Location=SkillCaptureWorld.CasterTile+new Vector2(-2,2)};
                _world.Objects.Add(_captureMonster);await _captureMonster.Load();
                if(targetCapture!="monster-unknown")_captureMonster.UpdateHealthFractions(.64f,null);
            }
            _world.Victim.Name="Target Player";
            _world.Victim.UpdateHealthFraction(.72f);
            _world.Victim.UpdateHealthFraction(null);
            if(Math.Abs(_world.Victim.HealthFraction!.Value-.72f)>.0001f)
                throw new InvalidOperationException("Unknown health update replaced a known server value.");
            Console.WriteLine("PASS: remote player health preserves the latest known server fraction.");
            var target=new Client.Main.Controls.UI.Game.Hud.MobileTargetControl(()=>_captureMonster is not null?_captureMonster:_world.Victim);
            Controls.Add(target);await target.Initialize();
        }
        if(Environment.GetEnvironmentVariable("TWINMU_MOBILE_PANEL_CAPTURE")=="1")
        {
            var state=MuGame.Network.GetCharacterState();state.Name="KeViN";state.Level=400;
            state.CurrentHealth=7501;state.MaximumHealth=8335;state.CurrentMana=1480;state.MaximumMana=2210;
            state.CurrentShield=18567;state.MaximumShield=18587;state.CurrentAbility=2100;state.MaximumAbility=3684;
            var panel=new Client.Main.Controls.UI.Game.Hud.MobilePlayerPanel(state,()=>_world.Caster,()=>_panelPk,()=>_panelPk=!_panelPk,true);
            Controls.Add(panel);await panel.Initialize();
            if(Environment.GetEnvironmentVariable("TWINMU_MOBILE_MODE_LIST_CAPTURE")=="1")
            {
                const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var press=typeof(Client.Main.Controls.UI.Game.Hud.MobilePlayerPanel).GetMethod("HandlePointerPress",flags)!;
                var option=typeof(Client.Main.Controls.UI.Game.Hud.MobilePlayerPanel).GetMethod("ModeOptionBounds",flags)!;
                void Click(Microsoft.Xna.Framework.Point point)=>press.Invoke(panel,new object[]{point});
                if(!(bool)press.Invoke(panel,new object[]{Client.Main.Controls.UI.Game.Hud.MobilePlayerPanel.HelperSettingsBounds.Center})! ||
                   !(bool)press.Invoke(panel,new object[]{Client.Main.Controls.UI.Game.Hud.MobilePlayerPanel.HelperStartBounds.Center})!)
                    throw new InvalidOperationException("Mobile Helper buttons are outside their input owner's hit areas.");
                Console.WriteLine("PASS: portrait panel owns the mobile Helper button hit areas.");
                void Choose(int index)=>Click(((Microsoft.Xna.Framework.Rectangle)option.Invoke(panel,new object[]{index})!).Center);
                Click(panel.ModeBounds.Center);
                if(_panelPk)throw new InvalidOperationException("Opening the mode list changed attack mode.");
                Choose(1);
                if(!_panelPk)throw new InvalidOperationException("Selecting PK did not enable it.");
                Click(panel.ModeBounds.Center);Choose(1);
                if(!_panelPk)throw new InvalidOperationException("Reselecting PK toggled it off.");
                Click(panel.ModeBounds.Center);Choose(0);
                if(_panelPk)throw new InvalidOperationException("Selecting Normal did not disable PK.");
                Click(panel.ModeBounds.Center);
                Console.WriteLine("PASS: mode list opens without toggling, selects PK/Normal, and preserves repeated selections.");
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        // Loading may complete over several render ticks. Do not consume a
        // short effect's lifetime before the capture clock has started.
        if (_spawned && !_loaded) return;
        // PNG readback can stall a host frame and skip a two-tick effect.
        // This offline fixture runs at 100 simulated FPS, including the public
        // timing service consumed by production effects.
        if (_loaded)
        {
            FPSCounter.Instance.CalcFPS(new GameTime(gameTime.TotalGameTime - TimeSpan.FromMilliseconds(10), TimeSpan.Zero));
            FPSCounter.Instance.CalcFPS(new GameTime(gameTime.TotalGameTime, TimeSpan.FromMilliseconds(10)));
        }
        if (_loaded)
            gameTime = new GameTime(gameTime.TotalGameTime,
                TimeSpan.FromSeconds(FPSCounter.Instance.FPS_ANIMATION_FACTOR / 25f));
        base.Update(gameTime);
        if (_captureHelper?.Visible == true) _captureHelper.BringToFront();
        if (_world?.Caster.Status == GameControlStatus.Ready)
        {
            // Reference default zoom step is 1300; the mounted reference raises
            // the eye another 30 units. Record this fixture camera explicitly.
            Vector3 center = _world.Caster.WorldPosition.Translation;
            Camera.Instance.AspectRatio = 4f / 3f;
            Camera.Instance.FOV = MathHelper.ToDegrees(2f * MathF.Atan(MathF.Tan(MathHelper.ToRadians(20f)) / (4f / 3f)));
            Camera.Instance.Position = center + new Vector3(1300f / MathF.Sqrt(2f), -1300f / MathF.Sqrt(2f), 1180f);
            Camera.Instance.Target = center + new Vector3(0f, 0f, 30f);
        }
        if (Status != GameControlStatus.Ready || _failed) return;
        if (_world.Caster.Status != GameControlStatus.Ready || _world.Victim.Status != GameControlStatus.Ready) return;
        if (!_actorsPrepared)
        {
            if (_actorsPreparing) return;
            _actorsPreparing = true;
            MuGame.TaskScheduler.QueueTask(async () =>
            {
                // No server inventory exists in this offline scene. Restore naked
                // class models after the normal actor initialization finishes.
                await _world.Caster.UpdateBodyPartClassesAsync(SkillCaptureWorld.CasterClass);
                await _world.Victim.UpdateBodyPartClassesAsync(PlayerClass.DarkWizard);
                // The production loader intentionally embeds its player skeleton.
                // This fixture uses the supplied reference skeleton on both sides,
                // so old body meshes are not bound to a different game's bones.
                var referencePlayer = await new Client.Data.BMD.BMDReader().Load(
                    Path.Combine(Constants.DataPath, "Player", "Player.bmd"));
                _world.Caster.Model = referencePlayer;
                _world.Victim.Model = referencePlayer;
                for (int i = 1; i < _world.ChainTargets.Length; i++)
                {
                    await _world.ChainTargets[i].UpdateBodyPartClassesAsync(PlayerClass.DarkWizard);
                    _world.ChainTargets[i].Model = referencePlayer;
                    _world.ChainTargets[i].Location = SkillCaptureWorld.TargetTile + (i == 1 ? new Vector2(2f, 1f) : new Vector2(3f, -1f));
                }
                foreach (var referenceWeapon in SkillCaptureWorld.ReferenceWeapons)
                {
                    if (!Client.Main.Core.Utilities.ItemDatabase.TryGetItemDefinitionByName(referenceWeapon.Name, out var definition))
                        throw new InvalidOperationException($"Cannot resolve reference weapon: {referenceWeapon.Name}");
                    // Inventory ammunition is not a held 3D weapon.
                    if (definition.Group == 4 && definition.Id is 7 or 15)
                        continue;
                    var weapon = referenceWeapon.Slot == 0 ? _world.Caster.Weapon1 : _world.Caster.Weapon2;
                    weapon.Model = await Client.Main.Content.BMDLoader.Instance.Prepare(definition.TexturePath);
                    weapon.TexturePath = definition.TexturePath;
                    weapon.ItemGroup = (byte)definition.Group;
                    weapon.MaterialItemGroup = definition.Group;
                    weapon.MaterialItemIndex = definition.Id;
                    weapon.ItemLevel = referenceWeapon.Level;
                    weapon.Hidden = false;
                    weapon.LinkParentAnimation = false;
                    weapon.ParentBoneLink = referenceWeapon.Slot == 0 ? 33 : 42;
                    weapon.Position = Vector3.Zero;
                    weapon.Angle = Vector3.Zero;
                }
                string itemWeaponMode = Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_WEAPONS") ?? "";
                if (SkillId == 0 && itemWeaponMode.Length > 0)
                {
                    bool bow = itemWeaponMode == "bow";
                    foreach (var weapon in new[] { _world.Caster.Weapon1, _world.Caster.Weapon2 })
                    {
                        weapon.Model = await Client.Main.Content.BMDLoader.Instance.Prepare(bow ? "Item/Bow01.bmd" : "Item/Sword01.bmd");
                        weapon.ItemGroup = (byte)(bow ? 4 : 0);
                        weapon.MaterialItemGroup = weapon.ItemGroup;
                        weapon.MaterialItemIndex = 0;
                        weapon.ItemLevel = int.Parse(Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_LEVEL") ?? "15");
                        weapon.Hidden = false;
                        weapon.LinkParentAnimation = false;
                        weapon.ParentBoneLink = weapon == _world.Caster.Weapon1 ? 33 : 42;
                        weapon.Position = Vector3.Zero;
                        weapon.Angle = Vector3.Zero;
                    }
                }
                if (SkillId == 0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_MATCH") == "1")
                {
                    int armorGroup=7;
                    foreach (var part in new ModelObject[] {_world.Caster.Helm,_world.Caster.Armor,_world.Caster.Pants,_world.Caster.Gloves,_world.Caster.Boots})
                    {
                        string name=armorGroup switch {7=>"Helm",8=>"Armor",9=>"Pant",10=>"Glove",_=>"Boot"};
                        part.Model=await Client.Main.Content.BMDLoader.Instance.Prepare($"Player/HDK_{name}Male01.bmd");
                        part.MaterialItemGroup=armorGroup++;part.MaterialItemIndex=29;part.Hidden=false;
                    }
                    var wing=_world.Caster.EquippedWings;
                    short wingIndex=short.Parse(Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE") ?? "36");
                    wing.ItemIndex=wingIndex;
                    var wingDefinition=Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition(12,wingIndex)!;
                    wing.Model=await Client.Main.Content.BMDLoader.Instance.Prepare(wingDefinition.TexturePath);
                    wing.ItemLevel=15;wing.Hidden=false;
                    await wing.LoadContent();
                }
                if (SkillId==0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_GM")=="1")
                    MuGame.Network.GetCharacterState().ActivateBuff(28,_world.Caster.NetworkId);
                _actorsPrepared = true;
            }, Client.Main.Controllers.TaskScheduler.Priority.High, "SkillCapture.Actors");
            return;
        }
        if (!_spawned)
        {
            if (++_warmup < 50) return;
            _world.Caster.FaceTowards(_world.Victim.Location, immediate: true);
            if(SkillId==0)
            {
                int level=int.Parse(Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_LEVEL")??"13");
                bool excellent=Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_OPTION")=="excellent";
                bool ancient=Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_OPTION")=="ancient";
                ModelObject[] parts=[_world.Caster.Helm,_world.Caster.Armor,_world.Caster.Pants,_world.Caster.Gloves,_world.Caster.Boots];
                int group=7;
                foreach(var part in parts){part.ItemLevel=level;part.IsExcellentItem=excellent;part.IsAncientItem=ancient;part.MaterialItemGroup=group++;part.MaterialItemIndex=Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_MATCH")=="1"?29:0;}
            }
            var action = _world.Caster.GetSkillAction(SkillId, isInSafeZone: false);
            if ((int)action < _world.Caster.Model.Actions.Length)
                _world.Caster.CurrentAction = action;
            File.WriteAllText(Path.Combine(OutputDirectory, "capture-state.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                SkillId,
                CasterVisible = _world.Caster.Visible,
                CasterHidden = _world.Caster.Hidden,
                CasterClass = _world.Caster.CharacterClass.ToString(),
                CasterPosition = _world.Caster.Position.ToString(),
                CasterWorldPosition = _world.Caster.WorldPosition.Translation.ToString(),
                CasterStatus = _world.Caster.Status.ToString(),
                HelmStatus = _world.Caster.Helm.Status.ToString(),
                HelmHidden = _world.Caster.Helm.Hidden,
                HelmHasModel = _world.Caster.Helm.Model != null,
                ArmorHasModel = _world.Caster.Armor.Model != null,
                ReferenceWeapons = SkillCaptureWorld.ReferenceWeapons.Select(w => new { w.Slot, w.Name, w.Level }),
                LeftWeaponHasModel = _world.Caster.Weapon1.Model != null,
                RightWeaponHasModel = _world.Caster.Weapon2.Model != null,
                SelfTarget = SkillCaptureWorld.SelfTarget,
                CameraPosition = Camera.Instance.Position.ToString(),
                CameraTarget = Camera.Instance.Target.ToString()
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            var context = new SkillEffectContext
            {
                Caster = _world.Caster,
                TargetId = SkillCaptureWorld.SelfTarget ? _world.Caster.NetworkId : _world.Victim.NetworkId,
                TargetPosition = SkillCaptureWorld.SelfTarget ? _world.Caster.WorldPosition.Translation : _world.Victim.WorldPosition.Translation,
                SkillId = SkillId,
                World = _world
            };
            bool hasRenderer = SkillVisualEffectRegistry.TrySpawn(SkillId, context, out _effect);
            if(SkillId==0){_effect=new ChainCaptureRoot();hasRenderer=true;}
            if (SkillId == 215 && SkillCaptureWorld.CaptureChainHops && hasRenderer)
            {
                _effect.Dispose();
                var root = new ChainCaptureRoot();
                WalkerObject previous = _world.Caster;
                for (int i = 0; i < _world.ChainTargets.Length; i++)
                {
                    root.Children.Add(new Client.Main.Objects.Effects.ChainLightningEffect(previous, _world.ChainTargets[i], i == 0));
                    previous = _world.ChainTargets[i];
                }
                _effect = root;
            }
            if (SkillId == 238 && CaptureLocalTargetImpact && hasRenderer)
            {
                _effect.Dispose();
                _effect = new Client.Main.Objects.Effects.ChaoticDiseierEffect(_world.Caster, null, _world.Victim.WorldPosition.Translation);
            }
            if (SkillId is 16 or 64)
            {
                // Soul Barrier enters through server buff state, not the cast registry.
                var buffs = new Client.Main.Core.Client.BuffManager(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
                buffs.ProcessMagicEffectStatus(_world.Caster.NetworkId,
                    (byte)(SkillId == 16 ? Client.Main.Core.Client.BuffEffectId.ManaShield : Client.Main.Core.Client.BuffEffectId.CriticalDamage), true);
                if (SkillId == 16) _effect = new Client.Main.Objects.Effects.ClassBuffEffect(_world.Caster, buffs);
                else
                {
                    var persistent = new Client.Main.Objects.Effects.ClassBuffEffect(_world.Caster, buffs);
                    _effect.Children.Add(persistent);
                }
                hasRenderer = true;
            }
            if(SkillId is 266 or 267 or 268)
            {
                _rageBuffs=new Client.Main.Core.Client.BuffManager(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
                _rageBuffs.ProcessMagicEffectStatus(_world.Caster.NetworkId,(byte)(129+SkillId-266),true);
                var persistent=new Client.Main.Objects.Effects.ClassBuffEffect(_world.Caster,_rageBuffs);
                _effect.Children.Add(persistent);
            }
            if(Environment.GetEnvironmentVariable("TWINMU_LEVELUP_CAPTURE") is { } levelUp)
            {
                _effect=new Client.Main.Objects.Effects.LevelUpEffect(_world.Caster.Position,levelUp=="master",_world.Caster.Angle);
                hasRenderer=true;
            }
            if (!hasRenderer || _effect == null)
            {
                File.WriteAllText(Path.Combine(OutputDirectory, "missing-renderer.txt"), $"Skill {SkillId} has no registry renderer. This is a failed case, not a visual match.");
                _failed = true;
                MuGame.Instance.Exit();
                return;
            }
            _world.Objects.Add(_effect);
            MuGame.TaskScheduler.QueueTask(async () =>
            {
                await _effect.Load();
                if(SkillId==263)
                {
                    for(int i=0;i<_world.ChainTargets.Length;i++)
                    {
                        var ghost=new Client.Main.Objects.Effects.DarkSideAfterimageEffect(_world.Caster,_world.ChainTargets[i],i){World=_world};
                        _world.Objects.Add(ghost);await ghost.Load();
                    }
                }
                if(_rageBuffs!=null)
                    _rageWind=_effect.Children.OfType<Client.Main.Objects.Effects.ClassBuffEffect>().Single().Children.OfType<Client.Main.Objects.Effects.RageFighterBuffWindEffect>().Single();
                _loaded = true;
            }, Client.Main.Controllers.TaskScheduler.Priority.High, "SkillCapture.Load");
            _spawned = true;
        }
        // The game uses variable-rate rendering. Count the same 25-Hz animation
        // units as the effects rather than labelling every Draw as a source frame.
        if (_loaded)
        {
            _animationFrame += FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _frame = (int)_animationFrame;
            if(Environment.GetEnvironmentVariable("TWINMU_MOBILE_TARGET_CAPTURE")!=null)
            {
                if(_frame==24)_world.Victim.UpdateHealthFraction(.35f);
                if(_frame==48)_world.Victim.UpdateHealthFraction(.15f);
            }
            if(Environment.GetEnvironmentVariable("TWINMU_MOBILE_PANEL_CAPTURE")=="1")
            {
                var state=MuGame.Network.GetCharacterState();_panelPk=_frame>=48;
                state.CurrentHealth=_frame is >=24 and <48?0u:7501u;
                state.MaximumMana=_frame is >=24 and <48?0u:2210u;
                state.CurrentMana=state.MaximumMana==0?0u:1480u;
            }
            if(SkillId==0 && Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE")!=null)
            {
                // Show the full spread instead of the old edge-on fixture angle.
                _world.Caster.Direction=Client.Main.Models.Direction.West;
                if(_frame>=350 && Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE_REMOVE")=="1")
                    _world.Caster.EquippedWings.ItemIndex=-1;
            }
            if (SkillId == 0 && _frame >= 32 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_WEAPONS") == "remove")
            {
                _world.Caster.Weapon1.Model = null;
                _world.Caster.Weapon2.Model = null;
            }
            if(SkillId==0 && _frame>=256 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_GM")=="1")
                MuGame.Network.GetCharacterState().DeactivateBuff(28,_world.Caster.NetworkId);
            if(_rageBuffs!=null && _frame>=48)
                _rageBuffs.ProcessMagicEffectStatus(_world.Caster.NetworkId,(byte)(129+SkillId-266),false);
        }
        if (_frame > CaptureFrames[^1] + 1) MuGame.Instance.Exit();
    }

    public override void Draw(GameTime gameTime)
    {
        if(Environment.GetEnvironmentVariable("TWINMU_HOVER_CAPTURE")=="1" && _world!=null)
        {
            typeof(WorldObject).GetProperty("IsMouseHover")!.SetValue(_world.Caster,true);
            typeof(WorldObject).GetProperty("IsMouseHover")!.SetValue(_world.Victim,true);
            if(_captureMonster!=null)typeof(WorldObject).GetProperty("IsMouseHover")!.SetValue(_captureMonster,true);
        }
        if(_captureMonster is Client.Main.Objects.Monsters.QueenRainier or Client.Main.Objects.Monsters.IceQueen &&
            Environment.GetEnvironmentVariable("TWINMU_MOBILE_TARGET_CAPTURE")?.EndsWith("-low")==true)
            typeof(WorldObject).GetProperty(nameof(WorldObject.LowQuality))!.SetValue(_captureMonster,true);
        base.Draw(gameTime);
        if(_frame==64 && _captureMonster is Client.Main.Objects.Monsters.IceQueen iceQueen)
        {
            if(iceQueen.LightEnabled || iceQueen.Light!=Vector3.One || !iceQueen.RenderPolicy.PreserveBlendMeshesInLowQuality)
                throw new Exception("Ice Queen has black unlit vertex colours or loses her glow in low quality.");
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var meshes=(Array)typeof(ModelObject).GetField("_meshes",flags)!.GetValue(iceQueen)!;
            bool hasVisibleBody=false;
            foreach(var mesh in meshes)
            {
                var cache=mesh!.GetType().GetField("BufferCache")!.GetValue(mesh)!;
                var color=(Microsoft.Xna.Framework.Color)cache.GetType().GetField("CachedBodyColor")!.GetValue(cache)!;
                hasVisibleBody |= color.R>128 && color.G>128 && color.B>128;
            }
            if(!hasVisibleBody)throw new Exception("Ice Queen still has black CPU vertex colours.");
            Console.WriteLine("PASS: Ice Queen's low-quality body has visible vertex colours and preserves her glow layer.");
        }
        if(_frame==64 && _captureMonster is Client.Main.Objects.Monsters.QueenRainier queen)
        {
            if(queen.LightEnabled || queen.Light!=Vector3.One || !queen.RenderPolicy.PreserveBlendMeshesInLowQuality)
                throw new Exception("Queen Rainer depends on terrain lighting or loses its body in low quality.");
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var plan=(System.Collections.IDictionary)typeof(ModelObject).GetField("_transparentMeshPlan",flags)!.GetValue(queen)!;
            if(plan.Count==0)throw new Exception("Queen Rainer's blended body was omitted from the render plan.");
            Console.WriteLine("PASS: Queen Rainer keeps its self-lit textured body, including forced low-quality rendering.");
        }
        if(_frame==64 && Environment.GetEnvironmentVariable("TWINMU_EQUIPMENT_PREVIEW_CAPTURE")=="1")
        {
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var inventory=Controls.OfType<Client.Main.Controls.UI.Game.Inventory.InventoryControl>().Single();
            var equipped=(Dictionary<byte,InventoryItem>)inventory.GetType().GetField("_equippedItems",flags)!.GetValue(inventory)!;
            var slots=(System.Collections.IDictionary)inventory.GetType().GetField("_equipSlots",flags)!.GetValue(inventory)!;
            foreach(var pair in equipped)
            {
                var bounds=(Rectangle)slots[pair.Key]!.GetType().GetProperty("Rect")!.GetValue(slots[pair.Key])!;
                if(Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.TryGetCachedOverflowPreview(pair.Value,bounds.Width,bounds.Height)==null)
                    throw new Exception($"Equipment slot {pair.Key} was starved by animated inventory items.");
            }
            Console.WriteLine($"PASS: all {equipped.Count} equipped +15 previews loaded alongside animated bag items.");
        }
        if(_combatAtlasSamples!=null)
        {
            foreach(var sample in _combatAtlasSamples)sample.DrawAfter(gameTime);
            Client.Main.Graphics.WorldLabelRenderer.DrawWorldLabel(GraphicsManager.Instance.Sprite,MuGame.Instance.GraphicsDevice,
                GraphicsManager.Instance.Font,"Battle Bow +Skill",_world.Caster.Position+new Vector3(100,0,0),
                Microsoft.Xna.Framework.Color.CornflowerBlue,Microsoft.Xna.Framework.Color.Black*.55f,
                Camera.Instance.Projection,Camera.Instance.View,.4f*Constants.RENDER_SCALE);
        }
        if(Environment.GetEnvironmentVariable("TWINMU_UPDATE_SCREEN_CAPTURE")=="1")
        {
            Client.Main.Helpers.AppUpdateScreen.Show("• Fixed NPC names\n• Improved mobile chat and Helper\n• Updated target health bars\n• Added potion slot placement",104857600);
            Client.Main.Helpers.AppUpdateScreen.Downloaded=65011712;
            Client.Main.Helpers.AppUpdateScreen.BytesPerSecond=2097152;
            Client.Main.Helpers.AppUpdateScreen.Draw();
            Client.Main.Helpers.AppUpdateScreen.Close();
        }
        if (SkillId == 0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_INVENTORY") == "1")
        {
            bool general = Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_GENERAL") == "1";
            string? captureWing=Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE");
            if (_inventoryCaptureItems == null)
            {
                _inventoryCaptureItems = captureWing!=null
                    ? new[] {new InventoryItem(Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition(12,short.Parse(captureWing))!, Microsoft.Xna.Framework.Point.Zero, level:15)}
                    : general
                    ? new (int Group, short Id)[] {(0,0),(2,0),(4,0),(5,0),(6,0),(7,29),(8,29),(9,29),(10,29),(11,29),(12,0),(12,36),(13,8),(14,0),(15,0)}
                        .Select(key => new InventoryItem(Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition((byte)key.Group,key.Id)
                            ?? throw new InvalidOperationException($"Missing capture item {key.Group}:{key.Id}"), Microsoft.Xna.Framework.Point.Zero, level:key.Group<=12?13:0)).ToArray()
                    : new[] {29,59,60,61,73}.Select(id => new InventoryItem(
                    new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(id,"Source inventory armor",2,3,id==29?"Player/HDK_ArmorMale01.bmd":$"Player/ArmorMale{id+1:D2}.bmd") {Group=8},
                    Microsoft.Xna.Framework.Point.Zero, level:int.Parse(Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_LEVEL") ?? "13"))).ToArray();
            }
            foreach (var inventoryItem in _inventoryCaptureItems)
            {
                int slotWidth = general ? inventoryItem.Definition.Width*32 : 64;
                int slotHeight = general ? inventoryItem.Definition.Height*32 : 96;
                var preview = general || captureWing!=null || inventoryItem.Definition.Id==29
                    ? Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.GetOverflowPreview(inventoryItem,slotWidth,slotHeight,gameTime,false)
                    : Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.GetMaterialAnimatedPreview(inventoryItem, 256, 256, gameTime);
                if (preview != null && _loaded && CaptureFrames.Contains(_frame) && _lastCapturedFrame != _frame)
                {
                    using var stream = File.Create(Path.Combine(OutputDirectory, $"inventory-{inventoryItem.Definition.Group}-{inventoryItem.Definition.Id}-frame-{_frame:D3}.png"));
                    preview.SaveAsPng(stream, preview.Width, preview.Height);
                    if (general && _frame == CaptureFrames[^1])
                    {
                        var slot = new Microsoft.Xna.Framework.Rectangle(100,100,slotWidth,slotHeight);
                        var draw = Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.GetItemDrawRectangle(preview,slot);
                        if (draw.Width != preview.Width || draw.Height != preview.Height || draw.Center != slot.Center)
                            throw new InvalidOperationException($"Preview placement mismatch: {inventoryItem.Definition.Group}:{inventoryItem.Definition.Id}");
                        if (Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.TryGetCachedOverflowPreview(inventoryItem,slotWidth,slotHeight) != preview)
                            throw new InvalidOperationException("Drag cache did not return the padded preview.");
                        Console.WriteLine($"PASS: grid preview + drag cache {inventoryItem.Definition.Group}:{inventoryItem.Definition.Id}, {slotWidth}x{slotHeight} -> {preview.Width}x{preview.Height}");
                    }
                }
                else if (general && preview == null && _frame == CaptureFrames[^1]) throw new InvalidOperationException("General item preview failed to render.");
            }
        }
        if (!_loaded || !CaptureFrames.Contains(_frame) || _lastCapturedFrame == _frame) return;
        var bindings = MuGame.Instance.GraphicsDevice.GetRenderTargets();
        var target = bindings.Length > 0 ? bindings[0].RenderTarget as Microsoft.Xna.Framework.Graphics.RenderTarget2D : null;
        File.WriteAllText(Path.Combine(OutputDirectory, $"skill-{SkillId}-frame-{_frame:D3}.json"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                Frame = _frame,
                CaptureLocalTargetImpact,
                SkillCaptureWorld.CaptureChainHops,
                EffectType = _effect.GetType().Name,
                EffectStatus = _effect.Status.ToString(),
                EffectPosition = _effect.Position.ToString(),
                EffectWorld = _effect.World != null,
                CasterWorld = _world.Caster.World != null,
                EffectVisible = _effect.Visible,
                EffectHidden = _effect.Hidden,
                Bounds = _effect.BoundingBoxWorld.ToString(),
                GameMasterBadge = _world.Caster.Children.ToArray().OfType<Client.Main.Objects.Effects.GameMasterBadgeEffect>().Select(b=>new {b.Hidden,b.Visible,b.Status,HasModel=b.Model!=null,Position=b.WorldPosition.Translation.ToString()}).ToArray(),
                WingMaterial = new {_world.Caster.EquippedWings.MaterialItemGroup,_world.Caster.EquippedWings.MaterialItemIndex},
                WingEffects = _world.Caster.EquippedWings.Children.ToArray().OfType<Client.Main.Objects.Effects.ReferenceWingEffect>().Select(e=>new {e.Hidden,e.Visible,e.ParticleCount,e.JointCount,e.SpriteCount}).ToArray(),
                FullSetAura = new {_world.Caster.FullSetAura.AuraTier,_world.Caster.FullSetAura.ExpandingTrailSpawns,_world.Caster.FullSetAura.RisingTrailSpawns},
                GradeArmor = _world.Objects.OfType<Client.Main.Objects.Player.PlayerObject>().SelectMany(p => p.Children.ToArray()).OfType<Client.Main.Objects.Effects.Grade15ArmorEffect>().Select(g => new {g.Hidden, g.Visible, g.Status, HasModel=g.Model!=null, Position=g.WorldPosition.Translation.ToString(), Flashes=g.Children.ToArray().Select(f=>new {f.Status,f.Visible,f.Hidden,Position=f.WorldPosition.Translation.ToString(),f.Scale}).ToArray(), g.Diagnostic}).ToArray(),
                GradeWeapons = _world.Objects.OfType<Client.Main.Objects.Player.PlayerObject>().SelectMany(p => p.Children.ToArray()).OfType<Client.Main.Objects.Effects.Grade15WeaponEffect>().Select(g => new {g.Hidden, g.ActiveHands, g.LiveParticles, g.Diagnostic}).ToArray(),
                ArrowDiagnostics = CaptureArrowDiagnostics(),
                ChaoticParts = CaptureChaoticParts(),
                RageBuffWind = _rageWind==null?null:new { _rageWind.Alpha, _rageWind.Status, Meshes=_rageWind.Model?.Meshes.Length },
                BookParts = _effect is Client.Main.Objects.Effects.SummonerBookCastEffect or Client.Main.Objects.Effects.RageFighterCastEffect
                    ? ((Client.Main.Objects.WorldObject[])typeof(Client.Main.Objects.Effects.StagedCombatEffect).GetField("_parts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(_effect)!)
                        .OfType<Client.Main.Objects.ModelObject>().Select(m => new { Model = m.Model?.Name, Meshes = m.Model?.Meshes.Length, Path = m.GetType().GetField("_path", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(m)?.ToString(), m.Status, m.Hidden, m.Alpha, m.CurrentAction, Frame = m.GetAnimationTime(), Position = m.Position.ToString() }).ToArray() : null,
                WorldObjects = _world.Objects.Where(o => o is Client.Main.Objects.EffectObject || o is Client.Main.Objects.Player.PlayerObject || o is Client.Main.Objects.ModelObject && o.GetType().Namespace?.StartsWith("Client.Main.Objects.Effects")==true).Select(o => new
                {
                    Type = o.GetType().Name, Status = o.Status.ToString(), o.Visible,
                    Position = o.WorldPosition.Translation.ToString()
                }).ToArray(),
                AnimationFactor = FPSCounter.Instance.FPS_ANIMATION_FACTOR
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        using var output = File.Create(Path.Combine(OutputDirectory, $"skill-{SkillId}-frame-{_frame:D3}.png"));
        if (target != null)
            target.SaveAsPng(output, target.Width, target.Height);
        else
        {
            // At native render scale the production renderer draws directly to
            // the backbuffer; MainRenderTarget can be a stale, unused surface.
            var device = MuGame.Instance.GraphicsDevice;
            int width = device.PresentationParameters.BackBufferWidth;
            int height = device.PresentationParameters.BackBufferHeight;
            var pixels = new Microsoft.Xna.Framework.Color[width * height];
            device.GetBackBufferData(pixels);
            using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(device, width, height);
            texture.SetData(pixels);
            texture.SaveAsPng(output, width, height);
        }
        _lastCapturedFrame = _frame;
        if (SkillId == 62 && _frame == 5)
        {
            // Exercise the same authoritative forced-move entry used by packet 0x15.
            typeof(WalkerObject).GetMethod("ApplyServerForcedMove",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(_world.Victim, new object[] { _world.Victim.Location + new Vector2(3f, 0f) });
        }
    }

    private sealed class ChainCaptureRoot : EffectObject
    {
        private float _age;
        public ChainCaptureRoot()
        {
            IsTransparent = true; LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-10000f), new Vector3(10000f));
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            _age += FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_age >= 72f) { World?.RemoveObject(this); Dispose(); }
        }
    }

    private object CaptureChaoticParts()
    {
        if (_effect is not Client.Main.Objects.Effects.ChaoticDiseierEffect) return null;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var parts = new[] { "_birds", "_feathers" }.SelectMany(name =>
            ((_effect.GetType().GetField(name, flags)?.GetValue(_effect) as Array)?.Cast<object>() ?? Array.Empty<object>()))
            .OfType<ModelObject>().ToArray();
        return new
        {
            Total = parts.Length,
            Undisposed = parts.Count(p => p.Status != GameControlStatus.Disposed),
            InWorld = parts.Count(p => _world.Objects.Contains(p))
        };
    }

    private object CaptureArrowDiagnostics()
    {
        if (_effect is not Client.Main.Objects.Effects.ArrowProjectileEffect) return null;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var type = _effect.GetType();
        var renderer = type.GetField("_modelRenderer", flags)?.GetValue(_effect) as Client.Main.Objects.ModelObject;
        var projectiles = type.GetField("_projectiles", flags)?.GetValue(_effect) as Array;
        return new
        {
            Profile = type.GetField("_profile", flags)?.GetValue(_effect)?.ToString(),
            RendererStatus = renderer?.Status.ToString(), RendererVisible = renderer?.Visible,
            RendererWorld = renderer?.World != null, RendererPosition = renderer?.WorldPosition.Translation.ToString(),
            Model = renderer?.Model?.Name, ModelMeshes = renderer?.Model?.Meshes.Length,
            Projectiles = projectiles?.Cast<object>().Select(p => new
            {
                Active = p.GetType().GetField("Active")?.GetValue(p),
                Position = p.GetType().GetField("Position")?.GetValue(p)?.ToString(),
                Stopped = p.GetType().GetField("Stopped")?.GetValue(p)
            }).ToArray()
        };
    }
}
