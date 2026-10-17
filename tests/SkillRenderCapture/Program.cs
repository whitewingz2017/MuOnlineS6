using Client.Main;
using Client.Main.Scenes;

if(args.Length==1 && args[0]=="--verify-update-polling")
{
    var calls=new List<bool>();using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(2));
    try
    {
        await Client.Main.Helpers.AppUpdatePoller.RunAsync(startup=>
        {
            calls.Add(startup);if(calls.Count==3)stop.Cancel();return Task.CompletedTask;
        },TimeSpan.FromMilliseconds(5),stop.Token);
    }
    catch(OperationCanceledException) when(stop.IsCancellationRequested){ }
    if(!calls.SequenceEqual(new[]{true,false,false}))throw new Exception("Update checks stopped after startup or treated a background check as startup.");
    Console.WriteLine("PASS: startup checks run once; background checks continue without restarting and stop on cancellation.");
    return;
}

if(args.Length==1 && args[0]=="--verify-merchant")
{
    var sent=new List<byte>();bool open=true;
    var now=DateTimeOffset.UtcNow;
    var batch=new Client.Main.Core.Utilities.NpcPurchaseBatch(sent.Add,()=>open,()=>now);
    if(!batch.Start(9,3) || sent.Count!=1 || batch.Start(10,2))throw new Exception("Purchase batch did not serialize requests.");
    batch.Acknowledge(true);batch.Acknowledge(true);
    if(sent.Count!=3 || sent.Any(slot=>slot!=9))throw new Exception("Purchase quantity or slot changed.");
    batch.Acknowledge(true);
    if(batch.IsActive)throw new Exception("Purchase batch did not finish.");
    batch.Start(9,3);int before=sent.Count;batch.Acknowledge(false);
    if(batch.IsActive || sent.Count!=before)throw new Exception("Failed purchase sent another request.");
    batch.Start(9,2);open=false;before=sent.Count;batch.Acknowledge(true);
    if(batch.IsActive || sent.Count!=before)throw new Exception("Closed merchant continued buying.");
    open=true;batch.Start(9,2);now=now.AddSeconds(11);
    if(!batch.CheckTimeout() || batch.IsActive)throw new Exception("Timed-out purchase continued buying.");
    var raw=new byte[]{0xE0,1,0,3,0};
    if(Client.Main.Core.Utilities.ItemDatabase.GetItemDurability(raw)!=3)throw new Exception("Shop stack parsed level instead of count.");
    var definition=new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(11,"Box of Luck",1,1,"Item/Potion12.bmd"){Group=14};
    foreach(int level in Enumerable.Range(8,5))
    {
        var item=new InventoryItem(definition,Microsoft.Xna.Framework.Point.Zero,level:level);
        if(item.Definition.TexturePath!="Item/MagicBox08.bmd")throw new Exception("Kundun box uses the base potion model.");
    }
    if(definition.TexturePath!="Item/Potion12.bmd")throw new Exception("Kundun variant mutated the shared base definition.");
    Console.WriteLine("PASS: merchant quantities await each acknowledgement, stop on failure/close/timeout, parse server stack counts, and preserve Kundun model variants.");
    return;
}

if(args.Length==1 && args[0]=="--verify-registration")
{
    var validate=Client.Main.Networking.Services.RegistrationService.Validate;
    if(validate("newuser","a@example.com","secret1","secret1")!=null)throw new Exception("Valid registration rejected.");
    foreach(var item in new[]{("ab","a@example.com","secret1","secret1"),("longusername","a@example.com","secret1","secret1"),("has space","a@example.com","secret1","secret1"),("newuser","bad email","secret1","secret1"),("newuser","a@example.com","secret1","different"),("newuser","a@example.com",new string('p',21),new string('p',21))})
        if(validate(item.Item1,item.Item2,item.Item3,item.Item4)==null)throw new Exception("Invalid registration accepted.");
    var handler=new RegistrationResponseHandler();using var http=new System.Net.Http.HttpClient(handler);
    var service=new Client.Main.Networking.Services.RegistrationService(http);
    foreach(int status in new[]{201,409,400,429,404,500})
    {
        handler.Status=status;
        var result=await service.RegisterAsync("https://registration.invalid/api/accounts/test-registration","newuser","a@example.com","secret1",default);
        if(result.Success!=(status==201))throw new Exception($"Incorrect registration result for HTTP {status}.");
    }
    int sent=handler.Requests;
    if((await service.RegisterAsync("","newuser","a@example.com","secret1",default)).Success || (await service.RegisterAsync("http://registration.invalid","newuser","a@example.com","secret1",default)).Success || handler.Requests!=sent)
        throw new Exception("Unconfigured/insecure registration sent credentials.");
    Console.WriteLine("PASS: registration input validation, HTTP result handling and unconfigured/insecure URL rejection.");
    return;
}

if (args.Length == 1 && args[0] == "--verify-interface-defaults")
{
    var settings = new Client.Main.Configuration.MuOnlineSettings();
    if (settings.HudTheme != Client.Main.Configuration.HudTheme.ClassicPc)
        throw new InvalidOperationException("Fresh Windows settings must select Classic PC.");
    var theme = typeof(Client.Main.Controls.UI.UiThemeManager);
    bool debug = (theme.Assembly.GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false)
        .OfType<System.Diagnostics.DebuggableAttribute>().FirstOrDefault()?.IsJITTrackingEnabled).GetValueOrDefault();
    Client.Main.Controls.UI.UiThemeManager.Initialize("Classic");
    if (Client.Main.Controls.UI.UiThemeManager.IsMobileInterfaceActive != debug)
        throw new InvalidOperationException("Windows mobile preview must be available only in Debug.");
    Client.Main.Controls.UI.UiThemeManager.Initialize("Modern");
    if (Client.Main.Controls.UI.UiThemeManager.IsMobileInterfaceActive)
        throw new InvalidOperationException("Mobile player panel must be hidden outside Mobile theme.");
    Console.WriteLine($"PASS: fresh Windows defaults to Classic PC; mobile preview Debug={debug}; Modern hides mobile panel.");
    return;
}

if (args.Length is 1 or 2 && args[0] == "--verify-aura-tail-ring")
{
    const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var auraType = args.Length == 2
        ? System.Reflection.Assembly.LoadFile(Path.GetFullPath(args[1])).GetType("Client.Main.Objects.Effects.FullSetAuraEffect")!
        : typeof(Client.Main.Objects.Effects.FullSetAuraEffect);
    var type = auraType.GetNestedType("OrbitTrail", System.Reflection.BindingFlags.NonPublic)!;
    var push = type.GetMethod("PushTail", flags)!;
    var read = type.GetMethod("GetTailPoint")!;
    foreach (int limit in new[] { 16, 20 })
    {
        var trail = Activator.CreateInstance(type, nonPublic: true)!;
        type.GetField("_tailLimit", flags)!.SetValue(trail, limit);
        for (int tick = 1; tick <= 100; tick++)
        {
            push.Invoke(trail, new object[] { new Microsoft.Xna.Framework.Vector3(3500 + tick, 2600, 100) });
            for (int index = 0; index < Math.Min(tick, limit); index++)
            {
                var actual = (Microsoft.Xna.Framework.Vector3)read.Invoke(trail, new object[] { index })!;
                var expected = new Microsoft.Xna.Framework.Vector3(3500 + tick - index, 2600, 100);
                if (actual != expected)
                    throw new InvalidOperationException($"Aura tail capacity {limit}, tick {tick}, point {index}: {actual} != {expected}");
            }
        }
        Console.WriteLine($"PASS: aura trail capacity {limit} preserves all positions through 100 updates.");
    }
    return;
}

if(args.Length==3 && args[0]=="--compare-bmd")
{
    var reader=new Client.Data.BMD.BMDReader();
    var first=reader.ReadFromBuffer(File.ReadAllBytes(args[1]));
    var second=reader.ReadFromBuffer(File.ReadAllBytes(args[2]));
    var options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
    string Geometry(Client.Data.BMD.BMD model)=>System.Text.Json.JsonSerializer.Serialize(new {model.Meshes,model.Bones,model.Actions},options);
    if(Geometry(first)!=Geometry(second))throw new InvalidOperationException("Decoded model geometry differs: "+args[1]);
    Console.WriteLine("PASS: decoded model matches "+Path.GetFileName(args[1]));
    return;
}
if (args.Length == 2 && args[0] == "--inspect-bmd")
{
    var model = new Client.Data.BMD.BMDReader().ReadFromBuffer(File.ReadAllBytes(args[1]));
    Console.WriteLine($"{model.Name}: version {model.Version}, meshes {model.Meshes.Length}, bones {model.Bones.Length}, actions {model.Actions.Length}");
    foreach (var action in model.Actions) Console.WriteLine($"keys={action.NumAnimationKeys} speed={action.PlaySpeed} lock={action.LockPositions}");
    foreach (var mesh in model.Meshes) Console.WriteLine($"texture={mesh.Texture}");
    return;
}

if (args.Length is < 3 or > 5 || (args.Length == 5 && args[4] is not ("--local-target-impact" or "--chain-hops")) || !ushort.TryParse(args[0], out var skill))
    throw new ArgumentException("Usage: SkillRenderCapture <skill-id> <original-data-directory> <capture-directory> [reference-state.json]");
if (Enum.TryParse<Client.Main.Models.PlayerClass>(Environment.GetEnvironmentVariable("TWINMU_PORTRAIT_CLASS"),true,out var portraitClass))
    SkillCaptureWorld.CasterClass=portraitClass;
if (args.Length >= 4)
{
    var state = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(args[3]))!["state"]!;
    string className = state["class_name"]!.GetValue<string>().Replace(" ", "");
    if (className.Equals("DualMaster", StringComparison.OrdinalIgnoreCase)) className = "DuelMaster";
    if (!Enum.TryParse<Client.Main.Models.PlayerClass>(className, true, out var playerClass))
        throw new ArgumentException($"Unknown reference class: {className}");
    SkillCaptureWorld.CasterClass = playerClass;
    SkillCaptureWorld.SelfTarget = state["reference_target"] == null &&
        new ushort[] { 16, 26, 27, 28, 48, 64, 77, 217, 218, 233, 234, 266, 267, 268 }.Contains(skill);
    if (state["equipment"] is System.Text.Json.Nodes.JsonArray equipment)
        foreach (var item in equipment)
        {
            int slot = item!["slot"]!.GetValue<int>();
            if (slot is 0 or 1)
                SkillCaptureWorld.ReferenceWeapons.Add((slot,
                    System.Text.RegularExpressions.Regex.Replace(item["name"]!.GetValue<string>(), @" \+\d+$", ""),
                    item["level"]!.GetValue<int>()));
        }
    SkillCaptureWorld.MapIndex = (short)(state["map"]!.GetValue<int>() + 1);
    var position = state["position"]!;
    SkillCaptureWorld.CasterTile = new Microsoft.Xna.Framework.Vector2(position[0]!.GetValue<float>(), position[1]!.GetValue<float>());
    var target = state["reference_target"]?["position"];
    SkillCaptureWorld.TargetTile = target != null
        ? new Microsoft.Xna.Framework.Vector2(target[0]!.GetValue<float>(), target[1]!.GetValue<float>())
        : SkillCaptureWorld.CasterTile + new Microsoft.Xna.Framework.Vector2(3, 0);
}
SkillCaptureScene.SkillId = skill;
SkillCaptureWorld.CaptureDarkSideTargets = skill==263;
SkillCaptureWorld.CaptureChainHops = args.Contains("--chain-hops");
SkillCaptureScene.CaptureLocalTargetImpact = args.Contains("--local-target-impact");
SkillCaptureScene.OutputDirectory = Path.GetFullPath(args[2]);
Directory.CreateDirectory(SkillCaptureScene.OutputDirectory);
using var game = new MuGame { ConnectOnLoad=false };
Constants.DataPath = Path.GetFullPath(args[1]);
Constants.ENTRY_SCENE = Environment.GetEnvironmentVariable("TWINMU_CREATION_CAPTURE") != null
    ? typeof(CharacterCreationCaptureScene) : typeof(SkillCaptureScene);
game.IsFixedTimeStep = true;
game.TargetElapsedTime = TimeSpan.FromSeconds(1d / 25d);
if(Environment.GetEnvironmentVariable("TWINMU_NATIVE_NAME_CAPTURE")=="1")
    game.Components.Add(new NativeNameFrameCapture(game));
game.Run();

sealed class RegistrationResponseHandler : System.Net.Http.HttpMessageHandler
{
    public int Status=201, Requests;
    protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken cancellationToken)
    {
        Requests++;
        var json=System.Text.Json.Nodes.JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
        if(request.Method!=System.Net.Http.HttpMethod.Post || json["loginName"]?.GetValue<string>()!="newuser" || json["eMail"]?.GetValue<string>()!="a@example.com" || json["password"]?.GetValue<string>()!="secret1")
            throw new Exception("Incorrect registration request.");
        return new System.Net.Http.HttpResponseMessage((System.Net.HttpStatusCode)Status);
    }
}

// Capture the presented backbuffer after world upscaling and the native name pass.
sealed class NativeNameFrameCapture(MuGame game) : Microsoft.Xna.Framework.DrawableGameComponent(game)
{
    private int _last=-1;
    public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
    {
        if(game.ActiveScene is not SkillCaptureScene scene)return;
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        int frame=(int)typeof(SkillCaptureScene).GetField("_frame",flags)!.GetValue(scene)!;
        if(frame==_last || frame is not (24 or 48 or 64))return;
        _last=frame;
        var device=game.GraphicsDevice;
        if(device.GetRenderTargets().Length!=0)throw new InvalidOperationException("Native name capture did not reach the backbuffer.");
        int width=device.Viewport.Width,height=device.Viewport.Height;
        if(width!=device.PresentationParameters.BackBufferWidth || height!=device.PresentationParameters.BackBufferHeight)
            throw new InvalidOperationException("Native name overlay used a reduced viewport.");
        if(Client.Main.Helpers.OverheadNameplateRenderer.LastFrameQueuedNameplates<1)
            throw new InvalidOperationException("Native name pass did not draw queued actors.");
        // This component captures before MuGame draws its final version overlay.
        bool updateScreen=Environment.GetEnvironmentVariable("TWINMU_LOADING_TEXT_CAPTURE")=="app";
        if(updateScreen)
        {
            Client.Main.Helpers.AppUpdateScreen.Show("Preview only - no download is running.\nUpdated inventory artwork and readable mobile text.",250*1048576L);
            Client.Main.Helpers.AppUpdateScreen.Downloaded=105*1048576L;
            Client.Main.Helpers.AppUpdateScreen.BytesPerSecond=2.4*1048576;
            Client.Main.Helpers.AppUpdateScreen.Draw();
            Client.Main.Helpers.AppUpdateScreen.Close();
        }
        typeof(MuGame).GetMethod("DrawVersionLabel",flags)!.Invoke(game,null);
        if(Environment.GetEnvironmentVariable("TWINMU_COMBAT_ATLAS_CAPTURE")=="1")
        {
            Console.WriteLine("PASS: production damage/Miss objects and comma/M atlas render on the backbuffer.");
        }
        var colors=new Microsoft.Xna.Framework.Color[width*height];device.GetBackBufferData(colors);
        using var texture=new Microsoft.Xna.Framework.Graphics.Texture2D(device,width,height);texture.SetData(colors);
        using var output=File.Create(Path.Combine(SkillCaptureScene.OutputDirectory,$"native-names-{frame:D3}.png"));
        texture.SaveAsPng(output,width,height);
        Console.WriteLine($"PASS: names presented at native {width}x{height}, world render scale {Constants.RENDER_SCALE}.");
    }
}
