using System;
using MUnique.OpenMU.Network.Packets.ServerToClient;

namespace Client.Main.Core.Utilities;

public static class CharacterInformationPacketReader
{
    public static bool TryReadExtended(Memory<byte> packet, out CharacterInformationExtended info)
    {
        info = default;
        // OpenMU's 92-byte packet and the package's 96-byte variant share
        // all fields consumed by this client (through InventoryExtensions at 88).
        if (packet.Length < 92) return false;
        info = packet;
        return true;
    }
}
