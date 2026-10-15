using Client.Main;
using Client.Main.Scenes;

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
