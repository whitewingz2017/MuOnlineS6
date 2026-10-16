using Client.Main;
using Client.Main.Scenes;

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
using var game = new MuGame();
Constants.DataPath = Path.GetFullPath(args[1]);
Constants.ENTRY_SCENE = typeof(SkillCaptureScene);
game.IsFixedTimeStep = true;
game.TargetElapsedTime = TimeSpan.FromSeconds(1d / 25d);
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
