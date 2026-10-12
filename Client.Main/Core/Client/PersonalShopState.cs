using System.Collections.Generic;
using System;
using System.Buffers.Binary;
using System.Text;

namespace Client.Main.Core.Client;

/// <summary>Prices are committed only after the server acknowledges them.</summary>
public sealed class PersonalShopState
{
    public Dictionary<ushort, string> NearbyTitles { get; } = new();

    public bool ApplyAnnouncements(ReadOnlySpan<byte> packet, ushort selfId)
    {
        if (packet.Length < 6 || packet.Length < 6 + packet[5] * 38) return false;
        for (int i = 0; i < packet[5]; i++)
        {
            int offset = 6 + i * 38;
            ushort owner = (ushort)(BinaryPrimitives.ReadUInt16BigEndian(packet[offset..]) & 0x7FFF);
            var text = packet.Slice(offset + 2, 36);
            int end = text.IndexOf((byte)0);
            string title = Encoding.UTF8.GetString(end < 0 ? text : text[..end]);
            NearbyTitles[owner] = title;
            if (owner == (selfId & 0x7FFF)) { IsOpen = true; Title = title; }
        }
        return true;
    }

    public string GetVisibleTitle(ushort owner, ushort selfId)
    {
        owner &= 0x7FFF;
        if (owner == (selfId & 0x7FFF)) return IsOpen ? Title : null;
        return NearbyTitles.TryGetValue(owner, out var title) ? title : null;
    }

    public void ResolveOwner(ushort selfId)
    {
        if (selfId != 0 && selfId != 0xFFFF &&
            NearbyTitles.TryGetValue((ushort)(selfId & 0x7FFF), out var title))
        {
            IsOpen = true;
            Title = title;
        }
    }

    public void Close(ushort owner, ushort selfId)
    {
        NearbyTitles.Remove((ushort)(owner & 0x7FFF));
        if ((owner & 0x7FFF) == (selfId & 0x7FFF)) IsOpen = false;
    }

    public bool IsOpen { get; set; }
    public string Title { get; set; } = "My store";
    public Dictionary<byte, uint> Prices { get; } = new();
    public Dictionary<byte, uint> PendingPrices { get; } = new();

    public void Reset()
    {
        NearbyTitles.Clear();
        IsOpen = false;
        Title = "My store";
        Prices.Clear();
        PendingPrices.Clear();
    }

    public void ConfirmPrice(byte slot, bool success)
    {
        if (PendingPrices.Remove(slot, out uint price) && success)
            Prices[slot] = price;
    }
}
