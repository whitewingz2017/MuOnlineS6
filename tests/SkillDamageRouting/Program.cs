using System.Reflection;
using System.Text.RegularExpressions;
using Client.Data.BMD;
using Client.Main;
using Client.Main.Controls;
using Client.Main.Objects.Effects;
using Client.Main.Objects.Effects.Skills;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

// Compare runtime client routing against the actual server configuration, including
// inherited master-skill types. This catches missing entries and default Target routes.
try
{
    if (args.Length != 1)
        throw new ArgumentException("Pass the OpenMU server repository directory.");
    string initialization = Path.Combine(args[0], "src", "Persistence", "Initialization");
    var ids = Regex.Matches(File.ReadAllText(Path.Combine(initialization, "Skills", "SkillNumber.cs")), @"(\w+)\s*=\s*(\d+)")
        .ToDictionary(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value));
    string source = File.ReadAllText(Path.Combine(initialization, "VersionSeasonSix", "SkillsInitializer.cs"));
    var areaSkills = new Dictionary<string, string>();
    foreach (Match match in Regex.Matches(source, @"CreateSkill\(SkillNumber\.(\w+),[^\r\n]*skillType: SkillType\.(AreaSkill\w+)"))
    {
        // Monster-only attack definitions are not part of a player's skill hotbar.
        if (match.Groups[1].Value.StartsWith("Selupan")) continue;
        areaSkills[match.Groups[1].Value] = match.Groups[2].Value;
    }
    var replacements = new Dictionary<string, string>();
    foreach (Match match in Regex.Matches(source, @"AddMasterSkillDefinition\(([^\r\n]+)\);"))
    {
        var fields = match.Groups[1].Value.Split(',');
        replacements[fields[0].Trim().Replace("SkillNumber.", "")] = fields[5].Trim().Replace("SkillNumber.", "");
    }
    for (int pass = 0; pass < replacements.Count; pass++)
        foreach (var replacement in replacements)
            if (areaSkills.TryGetValue(replacement.Value, out string type))
                areaSkills[replacement.Key] = type;

    if (areaSkills.Count < 50) throw new Exception("Server audit did not find the expected skill definitions.");
    int assertions = 0;
    foreach (var skill in areaSkills)
    {
        int id = ids[skill.Key];
        if (!SkillDefinitions.IsAreaSkill(id))
            throw new Exception($"{skill.Key} ({id}) requires an AreaSkill packet; runtime client routes {SkillDefinitions.GetSkillType(id)}.");
        assertions++;
    }

    var rules = typeof(MuGame).Assembly.GetType("Client.Main.Core.Utilities.SkillCastRules")!;
    bool Rule(string name, int id) => (bool)rules.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [id])!;
    foreach (int id in new[] { 24, 52, 56, 235, 236, 411, 414, 416, 418, 431, 482 })
    {
        if (!Rule("UsesCasterAreaPosition", id)) throw new Exception($"Directional skill {id} must send caster coordinates.");
        assertions++;
    }
    foreach (var skill in areaSkills.Where(s => s.Value == "AreaSkillExplicitTarget"))
    {
        if (!Rule("RequiresExplicitAreaTarget", ids[skill.Key]))
            throw new Exception($"Explicit area skill {skill.Key} must require ExtraTargetId.");
        assertions++;
    }
    foreach (int id in new[] { 19, 20, 21, 22, 23, 43, 47, 51, 260, 261, 262, 263 })
    {
        if (!SkillDefinitions.IsTargetSkill(id)) throw new Exception($"Direct skill {id} must retain targeted routing.");
        assertions++;
    }
    // No graphics device or content loading: verify the actual visual factory's
    // free-shot direction and absence of an artificial collision target at the caster.
    var game = (MuGame)RuntimeHelpers.GetUninitializedObject(typeof(MuGame));
    var graphics = (GraphicsDeviceManager)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDeviceManager));
    graphics.PreferredBackBufferWidth = 1280;
    graphics.PreferredBackBufferHeight = 720;
    typeof(MuGame).GetField("_graphics", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, graphics);
    typeof(MuGame).GetProperty(nameof(MuGame.Instance))!.SetValue(null, game);
    var world = new TestArrowWorld();
    var shooter = new PlayerObject { World = world, Position = new Vector3(12750, 11550, 100) };
    foreach (ushort id in new ushort[] { 24, 414, 418, 52, 416, 235, 411, 431 })
    {
        foreach (float yaw in new[] { 0f, MathHelper.PiOver2, MathHelper.Pi, -MathHelper.PiOver2 })
        {
            var context = new SkillEffectContext
            {
                Caster = shooter, World = world, SkillId = id, TargetId = 0,
                TargetPosition = shooter.Position, LaunchYaw = yaw
            };
            if (!SkillVisualEffectRegistry.TrySpawn(id, context, out var visual) || visual is not ArrowProjectileEffect effect)
                throw new Exception($"Arrow skill {id} is missing its visual factory.");
            object[] collisionArgs = [Vector3.Zero];
            if ((bool)typeof(ArrowProjectileEffect).GetMethod("TryResolveTargetPosition", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(effect, collisionArgs)!)
                throw new Exception($"Arrow skill {id} incorrectly treats the area origin as a collision target.");
            var endpoint = (Vector3)typeof(ArrowProjectileEffect).GetMethod("ResolveTargetPosition", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(effect, [shooter.Position])!;
            var expected = shooter.Position + new Vector3(MathF.Sin(yaw), -MathF.Cos(yaw), 0) * 2100;
            if (Vector3.Distance(endpoint, expected) > .01f)
                throw new Exception($"Arrow skill {id} did not retain the packet launch direction.");
            float actualYaw = (float)typeof(ArrowProjectileEffect).GetMethod("ResolveBaseYaw", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(effect, [shooter.Position, endpoint])!;
            if (MathF.Abs(actualYaw - yaw) > .001f)
                throw new Exception($"Arrow skill {id} reconstructed a bearing from the caster origin.");
            assertions += 4;
        }
    }
    foreach (string styleName in new[] { "BestCrossbow", "Drill", "Ring", "DarkStinger", "Gamble", "Basic" })
    {
        var styleType = typeof(ArrowProjectileEffect).GetNestedType("ProjectileStyle", BindingFlags.NonPublic)!;
        object profile = typeof(ArrowProjectileEffect).GetMethod("CreateProfile", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [Enum.Parse(styleType, styleName)])!;
        var volley = new ArrowProjectileEffect(shooter, world, 0, null, ArrowVolleyKind.TripleShot, 0);
        typeof(ArrowProjectileEffect).GetField("_profile", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(volley, profile);
        typeof(ArrowProjectileEffect).GetMethod("InitializeVolley", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(volley, null);
        int count = (int)typeof(ArrowProjectileEffect).GetField("_projectileCount", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(volley)!;
        if (count != (styleName == "Basic" ? 3 : 4))
            throw new Exception($"Triple Shot weapon profile {styleName} produced {count} arrows instead of MuMain's volley size.");
        assertions++;
    }
    foreach (var master in Client.Main.Core.Utilities.ServerMasterSkills.All)
    {
        if (master.Replaced == 0)
        {
            if (SkillVisualEffectRegistry.HasEffect(master.Number))
                throw new Exception($"Passive master {master.Number} incorrectly has an active spell visual.");
        }
        else
        {
            ushort baseId = SkillVisualEffectRegistry.GetBaseSkillId(master.Number);
            if (SkillVisualEffectRegistry.HasEffect(master.Number) != SkillVisualEffectRegistry.HasEffect(baseId))
                throw new Exception($"Master {master.Number} does not inherit base skill {baseId}'s visual coverage.");
            if (Client.Main.Core.Utilities.SkillDatabase.GetSkillAnimation(master.Number) != SkillDefinitions.GetSkillAnimation(baseId))
                throw new Exception($"Master {master.Number} does not use the base skill cast animation.");
        }
        assertions++;
    }
    var factories = (System.Collections.IDictionary)typeof(SkillVisualEffectRegistry)
        .GetField("_effects", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    if (factories[(ushort)518] is not FireScreamSkillEffect || SkillVisualEffectRegistry.HasEffect(515))
        throw new Exception("Fire Scream / Critical Damage master IDs must not use Earthquake effects.");
    assertions++;
    foreach (float yaw in new[] { 0f, MathHelper.PiOver2, MathHelper.Pi, -MathHelper.PiOver2 })
    {
        shooter.Angle = Vector3.Zero;
        var context = new SkillEffectContext { Caster = shooter, World = world, LaunchYaw = yaw, TargetPosition = null };
        var twister = new TwisterSkillEffect().CreateEffect(context)!;
        var beam = new AquaBeamSkillEffect().CreateEffect(context)!;
        shooter.Angle = new Vector3(0, 0, yaw + MathHelper.Pi);
        Vector3 direction = new(MathF.Sin(yaw), -MathF.Cos(yaw), 0);
        var actualTwister = (Vector3)twister.GetType().GetMethod("ResolveMoveDirection", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(twister, null)!;
        var actualBeam = (Vector3)beam.GetType().GetMethod("GetForwardDirection", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(beam, [shooter.Position])!;
        if (Vector3.Distance(actualTwister, direction) > .001f || Vector3.Distance(actualBeam, direction) > .001f)
            throw new Exception("Twister / Aqua Beam changed direction after the caster turned during asset loading.");
        assertions += 2;
    }
    shooter.NetworkId = 0x123;
    world.Objects.Add(shooter);
    var spawnRemote = typeof(MuGame).Assembly.GetType("Client.Main.Networking.PacketHandling.Handlers.CharacterDataHandler")!
        .GetMethod("SpawnRemoteRegisteredSkill", BindingFlags.NonPublic | BindingFlags.Static)!;
    foreach (ushort id in new ushort[] { 8, 12, 14, 381 })
    {
        int before = world.Objects.Count;
        spawnRemote.Invoke(null, [world, shooter.NetworkId, id, (ushort)0, null, (float?)MathHelper.PiOver2]);
        if (world.Objects.Count != before + 1)
            throw new Exception($"Remote registered spell {id} was not added to the game world.");
        assertions++;
    }
    Console.WriteLine($"PASS: {assertions} routing, visual-direction, master-inheritance and remote-spawn assertions; {areaSkills.Count} player area skills audited.");
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    Environment.Exit(1);
}

sealed class TestArrowWorld : WalkableWorldControl
{
    public TestArrowWorld() : base(1) { }
}
