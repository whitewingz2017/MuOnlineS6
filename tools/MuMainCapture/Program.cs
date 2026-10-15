using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

const string socketPath = "C:/MuOnline_Source/MuMain-main/out/visual-reference.sock";
double[] captureSeconds = [0.04, 0.20, 0.48, 0.96, 1.92, 2.56];

async Task<JsonObject> Request(JsonObject command)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
    using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), timeout.Token);
    using var stream = new NetworkStream(socket);
    byte[] packet = Encoding.UTF8.GetBytes(command.ToJsonString() + "\n");
    await stream.WriteAsync(packet, timeout.Token);
    using var reader = new StreamReader(stream, Encoding.UTF8);
    string line = await reader.ReadLineAsync(timeout.Token)
        ?? throw new IOException("MuMain closed the connection without replying");
    return JsonNode.Parse(line)!.AsObject();
}

var response = await Request(new JsonObject { ["cmd"] = "state" });
if (response["ok"]?.GetValue<bool>() != true)
    throw new InvalidOperationException(response.ToJsonString());
var state = response["result"]!.AsObject();
var evidenceState = new JsonObject();
foreach (string field in new[] { "scene", "class", "class_name", "map", "map_name", "position", "skills", "safe_zone", "equipment", "buffs", "stats", "level", "mana", "max_mana", "ag", "max_ag" })
    if (state.TryGetPropertyValue(field, out var value)) evidenceState[field] = value?.DeepClone();
if (args is ["--move", var moveX, var moveY])
{
    Console.WriteLine((await Request(new JsonObject { ["cmd"] = "move", ["x"] = int.Parse(moveX), ["y"] = int.Parse(moveY) })).ToJsonString());
    return;
}
if (args is ["--snapshot", var snapshot])
{
    Console.WriteLine((await Request(new JsonObject { ["cmd"] = "screenshot", ["out"] = Path.GetFullPath(snapshot), ["quality"] = 100 })).ToJsonString());
    return;
}
if (args is ["--state"])
{
    Console.WriteLine(evidenceState.ToJsonString(new() { WriteIndented = true }));
    return;
}
if (args is ["--nearby"])
{
    Console.WriteLine(state["nearby"]?.ToJsonString(new() { WriteIndented = true }));
    return;
}
if (args.Length is < 2 or > 4 || !int.TryParse(args[0], out int skill))
    throw new ArgumentException("Usage: MuMainCapture --state | <skill-id> <output-folder> [target-id]");
if (state["scene"]?.GetValue<string>() != "world")
    throw new InvalidOperationException("Log the reference character into the game first");
string output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
var castCommand = new JsonObject { ["cmd"] = "skill", ["skill"] = skill };
if (args.Length >= 3 && args[2] is "--click" or "--click-target")
{
    // Select and confirm the intended skill through the native UI first.
    // Synthetic skill-card clicks were unreliable and must not label evidence.
    castCommand = new JsonObject { ["cmd"] = "click-ui", ["x"] = 574, ["y"] = 346, ["button"] = "right" };
    if (args[2] == "--click-target")
    {
        var fresh = (await Request(new JsonObject { ["cmd"] = "state" }))["result"]!.AsObject();
        int? requestedTarget = args.Length == 4 ? int.Parse(args[3]) : null;
        var monster = fresh["nearby"]!.AsArray().FirstOrDefault(item =>
            item?["kind"]?.GetValue<string>() == "monster" && item["alive"]?.GetValue<bool>() == true && item["pixel"] is JsonObject
            && (!requestedTarget.HasValue || item["id"]!.GetValue<int>() == requestedTarget.Value));
        if (monster == null) throw new InvalidOperationException("No visible living monster to aim the reference spell at");
        castCommand["x"] = monster["pixel"]!["x"]!.DeepClone();
        castCommand["y"] = monster["pixel"]!["y"]!.DeepClone();
        evidenceState["reference_target"] = new JsonObject
        {
            ["id"] = monster["id"]!.DeepClone(), ["kind"] = "monster", ["position"] = monster["position"]?.DeepClone()
        };
    }
}
else if (args.Length >= 3 && args[2] != "--observe")
{
    int targetId = int.Parse(args[2]);
    castCommand["target"] = targetId;
    var target = state["nearby"]?.AsArray().FirstOrDefault(item => item?["id"]?.GetValue<int>() == targetId);
    if (target != null)
        evidenceState["reference_target"] = new JsonObject
        {
            ["id"] = target["id"]!.DeepClone(), ["kind"] = target["kind"]?.DeepClone(),
            ["position"] = target["position"]?.DeepClone()
        };
}
bool observe = args.Contains("--observe");
var pendingCast = Request(castCommand);
async Task StopUnacknowledgedRetry()
{
    await Task.Delay(75);
    if (!pendingCast.IsCompleted)
        await Request(new JsonObject { ["cmd"] = "halt" });
}
// Some native casts send their packet without setting the automation success
// latch. Stop that retry loop before it toggles a summon repeatedly.
var stopRetry = observe ? StopUnacknowledgedRetry() : Task.CompletedTask;
JsonObject? cast = observe ? null : await pendingCast;
if (!observe && cast?["ok"]?.GetValue<bool>() != true)
{
    await File.WriteAllTextAsync(Path.Combine(output, "cast-failed.json"), cast!.ToJsonString());
    throw new InvalidOperationException("MuMain refused the cast: " + cast.ToJsonString());
}
var clock = System.Diagnostics.Stopwatch.StartNew();
var captures = new JsonArray();
foreach (double seconds in captureSeconds)
{
    double remaining = seconds - clock.Elapsed.TotalSeconds;
    if (remaining > 0) await Task.Delay(TimeSpan.FromSeconds(remaining));
    string path = Path.Combine(output, $"skill-{skill}-elapsed-{Math.Round(seconds * 1000):0000}ms.jpg");
    double sent = clock.Elapsed.TotalSeconds;
    var result = await Request(new JsonObject { ["cmd"] = "screenshot", ["out"] = path, ["quality"] = 100 });
    captures.Add(new JsonObject { ["planned_seconds"] = seconds, ["sent_seconds"] = sent, ["response"] = result });
}
await stopRetry;
cast ??= await pendingCast;
var evidence = new JsonObject
{
    ["skill"] = skill, ["state"] = evidenceState, ["cast"] = cast, ["captures"] = captures,
    ["cast_input"] = castCommand.DeepClone(),
    ["status"] = observe ? "observed during pending cast; inspect images independently of acknowledgement" : "captured; visual parity unverified",
    ["timing"] = observe ? "relative to sending input; captures run while the native acknowledgement is pending" : "relative to cast acknowledgement, including wind-up; not renderer frame numbers"
};
await File.WriteAllTextAsync(Path.Combine(output, "capture-state.json"), evidence.ToJsonString(new() { WriteIndented = true }));
if (captures.Any(row => row?["response"]?["ok"]?.GetValue<bool>() != true))
    throw new IOException("A screenshot failed; review capture-state.json");
Console.WriteLine($"Saved {captures.Count} MuMain captures for skill {skill} to {output}");
