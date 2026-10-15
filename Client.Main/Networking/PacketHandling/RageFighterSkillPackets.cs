using System;
using System.Buffers.Binary;

namespace Client.Main.Networking.PacketHandling;

/// <summary>The two distinct MuMain Rage Fighter server message layouts.</summary>
public static class RageFighterSkillPackets
{
    public static bool TryReadTargets(ReadOnlySpan<byte> packet, Span<ushort> targets, out ushort skill)
    {
        skill = 0;
        if (packet.Length != 16 || packet[0] != 0xC1 || packet[1] != 16 || packet[2] != 0x4B || targets.Length < 5)
            return false;
        skill = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(4, 2));
        for (int i = 0; i < 5; i++) targets[i] = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(6 + i * 2, 2));
        return true;
    }

    public static bool TryReadAttack(ReadOnlySpan<byte> packet, out ushort skill, out ushort source, out ushort target)
    {
        skill = source = target = 0;
        if (packet.Length != 9 || packet[0] != 0xC1 || packet[1] != 9 || packet[2] != 0x4A) return false;
        skill = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(3, 2));
        source = (ushort)(BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(5, 2)) & 0x7FFF);
        target = (ushort)(BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(7, 2)) & 0x7FFF);
        return true;
    }

    public static bool TryGetTarget(ushort raw, out ushort target)
    {
        target = (ushort)(raw & 0x7FFF);
        return raw is not (10000 or 0xFFFF);
    }
}
