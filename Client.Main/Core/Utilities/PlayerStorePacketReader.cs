using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Client.Main.Core.Utilities;

public static class PlayerStorePacketReader
{
    public sealed record StoreList(ushort Owner, string Name, string Title, List<(byte Slot, byte[] ItemData, uint Price)> Items);

    public static bool TryReadList(ReadOnlySpan<byte> packet, bool extended, out StoreList list)
    {
        list = null;
        if (packet.Length < 55 || packet[5] == 0) return false;
        int count = packet[54];
        if (count > 32) return false;
        int position = 55;
        var items = new List<(byte Slot, byte[] ItemData, uint Price)>();
        for (int i = 0; i < count; i++)
        {
            if (!extended)
            {
                if (position + 20 > packet.Length) return false;
                items.Add((packet[position], packet.Slice(position + 1, 12).ToArray(), BinaryPrimitives.ReadUInt32LittleEndian(packet[(position + 16)..])));
                position += 20;
                continue;
            }
            if (position + 14 > packet.Length) return false;
            uint price = BinaryPrimitives.ReadUInt32LittleEndian(packet[position..]);
            ushort currency = BinaryPrimitives.ReadUInt16LittleEndian(packet[(position + 4)..]);
            ushort amount = BinaryPrimitives.ReadUInt16LittleEndian(packet[(position + 6)..]);
            byte slot = packet[position + 8];
            if (!ItemDataParser.TryGetExtendedItemLength(packet[(position + 9)..], out int size) || position + 9 + size > packet.Length) return false;
            // Currency the UI cannot present accurately must not be offered as a Zen purchase.
            if (amount == 0 && currency < 0xFFF0) items.Add((slot, packet.Slice(position + 9, size).ToArray(), price));
            position += 9 + size;
        }
        if (position != packet.Length) return false;
        list = new StoreList(BinaryPrimitives.ReadUInt16BigEndian(packet[6..]),
            Encoding.UTF8.GetString(packet.Slice(8, 10)).TrimEnd('\0'),
            Encoding.UTF8.GetString(packet.Slice(18, 36)).TrimEnd('\0'), items);
        return true;
    }

    public static bool TryReadPurchase(ReadOnlySpan<byte> packet, bool extended, out byte result, out byte slot, out byte[] item)
    {
        result = slot = 0;
        item = Array.Empty<byte>();
        if (packet.Length < (extended ? 8 : 7)) return false;
        result = packet[extended ? 6 : 4];
        if (result != 1) return true;
        if (extended)
        {
            if (!ItemDataParser.TryGetExtendedItemLength(packet[8..], out int size) || size > packet.Length - 8) return false;
            // OpenMU allocates the serializer's maximum 15 bytes for this result.
            // Keep only the actual item, accepting its zero-filled unused buffer.
            if (size != packet.Length - 8)
            {
                if (packet.Length - 8 != 15) return false;
                foreach (byte padding in packet[(8 + size)..]) if (padding != 0) return false;
            }
            slot = packet[7];
            item = packet.Slice(8, size).ToArray();
        }
        else
        {
            if (packet.Length != 21) return false;
            slot = packet[20];
            item = packet.Slice(7, 12).ToArray();
        }
        return true;
    }
}
