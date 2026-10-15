using Client.Main.Objects.Effects.Joints;
using Client.Main.Objects.Effects.Skills;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

int checks = 0;
void Near(Vector3 actual, Vector3 expected)
{
    if (Vector3.Distance(actual, expected) > .001f)
        throw new Exception($"Rotation mismatch: expected {expected}, got {actual}");
    checks++;
}
// Independent sequential axis rotations, including mixed angles: the former
// transposed basis sends +X toward -Y for +90 yaw and fails these cases.
Vector3 RotateAxes(Vector3 v, Vector3 degrees)
{
    float x = MathHelper.ToRadians(degrees.X), y = MathHelper.ToRadians(degrees.Y), z = MathHelper.ToRadians(degrees.Z);
    v = new(v.X, v.Y * MathF.Cos(x) - v.Z * MathF.Sin(x), v.Y * MathF.Sin(x) + v.Z * MathF.Cos(x));
    v = new(v.X * MathF.Cos(y) + v.Z * MathF.Sin(y), v.Y, -v.X * MathF.Sin(y) + v.Z * MathF.Cos(y));
    return new(v.X * MathF.Cos(z) - v.Y * MathF.Sin(z), v.X * MathF.Sin(z) + v.Y * MathF.Cos(z), v.Z);
}
foreach (var angle in new[] { Vector3.Zero, new Vector3(0, 0, 90), new Vector3(90, 0, 0), new Vector3(0, 90, 0), new Vector3(17, -36, 125), new Vector3(-10, 0, 270) })
    foreach (var v in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, new Vector3(13, -27, 43) })
        Near(SourceJointMath.Rotate(v, angle), RotateAxes(v, angle));
SkillVisualEffectRegistry.Initialize();
foreach (var entry in ServerMasterSkills.All)
{
    bool expected = entry.Replaced != 0 && SkillVisualEffectRegistry.HasEffect(SkillVisualEffectRegistry.GetBaseSkillId(entry.Number));
    if (SkillVisualEffectRegistry.HasEffect(entry.Number) != expected)
        throw new Exception($"Wrong visual route for master skill {entry.Number}: {entry.Name}");
    checks++;
}
// Check factory identity too: a mere registered-ID check misses the former
// Earthquake aliases colliding with Critical Damage and Fire Scream.
var field = typeof(SkillVisualEffectRegistry).GetField("_effects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
var factories = (Dictionary<ushort, ISkillVisualEffect>)field.GetValue(null)!;
foreach (var entry in ServerMasterSkills.All.Where(e => e.Replaced != 0))
{
    ushort baseId = SkillVisualEffectRegistry.GetBaseSkillId(entry.Number);
    if (factories.TryGetValue(baseId, out var factory) && !ReferenceEquals(factory, factories[entry.Number]))
        throw new Exception($"Master skill {entry.Number} does not share its base factory {baseId}");
    checks++;
}
Console.WriteLine($"PASS: {checks} rotation and master visual routing checks.");

await SkillDatabase.Initialize();
int cooldownSkills=0;
foreach(ushort id in SkillVisualEffectRegistry.GetRegisteredSkillIds())
{
    int delay=SkillDatabase.GetSkillCooldown(id);
    if(delay<=0)continue;
    Client.Main.Core.Client.SkillCooldownTracker.ResetAll();
    var tracker=typeof(Client.Main.Core.Client.SkillCooldownTracker);
    if(!Client.Main.Core.Client.SkillCooldownTracker.TryConsume(id,10000) ||
       Client.Main.Core.Client.SkillCooldownTracker.TryConsume(id,10000+delay/2d) ||
       Math.Abs(Client.Main.Core.Client.SkillCooldownTracker.GetCooldownRatio(id,10000+delay/2d)-.5f)>.001f ||
       !Client.Main.Core.Client.SkillCooldownTracker.TryConsume(id,10000+delay))
        throw new Exception($"Cooldown repeat/expiry failed for skill {id}");
    Client.Main.Core.Client.SkillCooldownTracker.ResetAll();
    if(Client.Main.Core.Client.SkillCooldownTracker.GetRemainingMs(id,10000)!=0)
        throw new Exception($"Cooldown reset failed for skill {id}");
    cooldownSkills++;
}
if(cooldownSkills==0)throw new Exception("No cooldown skills exercised.");
Console.WriteLine($"PASS: cooldown consumption, half-time HUD ratio, expiry and reset for {cooldownSkills} registered skills.");
