using Client.Main.Objects.Player;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Client.Main.Diagnostics;

/// <summary>Android debug traces and opt-in release traces use warning level for logcat.</summary>
internal static class RemotePlayerVisibilityDiagnostics
{
    public static bool Enabled
    {
        get
        {
#if ANDROID && DEBUG
            return true;
#else
            return MuGame.AppConfiguration?.GetValue<bool>("Diagnostics:RemotePlayerVisibility") == true;
#endif
        }
    }

    public static void Trace(ILogger logger, string stage, ushort id, string detail)
    {
        if (Enabled)
            logger?.LogWarning("[RemotePlayerVisibility] {Stage} Id={Id:X4} {Detail}", stage, id, detail);
    }

    public static void TracePlayer(ILogger logger, string stage, PlayerObject player, bool inSnapshot)
    {
        if (!Enabled)
            return;

        var scope = MuGame.Network?.GetScopeManager()?.GetScopeObjectByMaskedId(player.NetworkId);
        logger?.LogWarning(
            "[RemotePlayerVisibility] {Stage} Id={Id:X4} Class={Class} Scope=({ScopeX},{ScopeY}) Tile={Tile} World={Position} Status={Status} Hidden={Hidden} InSnapshot={InSnapshot} Moving={Moving} Map={Map}",
            stage, player.NetworkId, player.CharacterClass, scope?.PositionX, scope?.PositionY,
            player.Location, player.WorldPosition.Translation, player.Status, player.Hidden,
            inSnapshot, player.IsMoving || player.MovementIntent, player.World?.MapId);
    }
}
