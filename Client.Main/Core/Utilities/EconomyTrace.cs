using System;
using System.IO;

namespace Client.Main.Core.Utilities;

internal static class EconomyTrace
{
    private static readonly object Gate = new();

    public static void Record(string source, uint balance, ReadOnlySpan<byte> packet = default)
    {
        if (!string.Equals(MuGame.AppConfiguration?["Diagnostics:EconomyTrace"], "true", StringComparison.OrdinalIgnoreCase)) return;
        // Only economy packets (never credentials or general network traffic) are recorded.
        string raw = packet.IsEmpty ? "" : Convert.ToHexString(packet);
        try
        {
            lock (Gate)
                File.AppendAllText(Path.Combine(MuGame.ConfigDirectory ?? AppContext.BaseDirectory, "economy-trace.log"),
                    $"{DateTimeOffset.Now:O} {source} balance={balance} bytes={raw}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
