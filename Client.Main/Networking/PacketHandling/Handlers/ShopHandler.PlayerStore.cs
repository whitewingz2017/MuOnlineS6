using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Client.Main.Controls.UI;
using Client.Main.Core.Utilities;
using Client.Main.Scenes;

namespace Client.Main.Networking.PacketHandling.Handlers;

public partial class ShopHandler
{
    [PacketHandler(0x3F, 0x00)]
    public Task HandleNearbyStoresAsync(Memory<byte> packet)
    {
        byte[] data = packet.ToArray();
        MuGame.ScheduleOnMainThread(() => _characterState.PersonalShop.ApplyAnnouncements(data, _characterState.Id));
        return Task.CompletedTask;
    }
    [PacketHandler(0x3F, 0x10)]
    public Task HandleStoreTitleChangedAsync(Memory<byte> packet)
    {
        if (packet.Length < 42) return Task.CompletedTask;
        byte[] announcement = new byte[44];
        announcement[5] = 1;
        packet.Span.Slice(4, 38).CopyTo(announcement.AsSpan(6));
        MuGame.ScheduleOnMainThread(() => _characterState.PersonalShop.ApplyAnnouncements(announcement, _characterState.Id));
        return Task.CompletedTask;
    }

    [PacketHandler(0x3F, 0x12)]
    public Task HandleStoreRemovedAsync(Memory<byte> packet)
    {
        if (packet.Length < 7) return Task.CompletedTask;
        ushort owner = BinaryPrimitives.ReadUInt16BigEndian(packet.Span[5..]);
        MuGame.ScheduleOnMainThread(() =>
        {
            _characterState.PersonalShop.Close(owner, _characterState.Id);
            if (MuGame.Instance?.ActiveScene is GameScene scene) scene.PlayerStore?.CloseIfOwner(owner);
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0x3F, 0x01)]
    public Task HandleStorePriceAsync(Memory<byte> packet)
    {
        if (packet.Length < 6) return Task.CompletedTask;
        byte result = packet.Span[4];
        bool success = result == 1;
        byte slot = packet.Span[5];
        if (!success) _logger.LogWarning("Store price rejected: slot={Slot}, result={Result}", slot, result);
        MuGame.ScheduleOnMainThread(() =>
        {
            _characterState.PersonalShop.ConfirmPrice(slot, success);
            if (!success) MessageWindow.Show(result switch
            {
                0 => "Close your store before changing prices. The server reports that it is still open or unavailable.",
                2 => "The server rejected this store slot. Please reopen your inventory.",
                3 => "The server cannot find this item in your store. Please log in again to refresh the items.",
                4 => "The server rejected the price. Enter a positive Zen amount.",
                5 => "The server has blocked this item from sale.",
                6 => "Your character must be level 6 or higher to set store prices.",
                _ => $"The server rejected the price (code {result}, slot {slot})."
            });
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0x3F, 0x02)]
    public Task HandleStoreOpenedAsync(Memory<byte> packet)
    {
        if (packet.Length < 5) return Task.CompletedTask;
        bool success = packet.Span[4] != 0;
        MuGame.ScheduleOnMainThread(() =>
        {
            _characterState.PersonalShop.IsOpen = success;
            MessageWindow.Show(success ? "Your personal store is open." : "The server could not open your store.");
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0x3F, 0x03)]
    public Task HandleStoreClosedAsync(Memory<byte> packet)
    {
        if (packet.Length < 7) return Task.CompletedTask;
        bool success = packet.Span[4] != 0;
        ushort owner = BinaryPrimitives.ReadUInt16BigEndian(packet.Span[5..]);
        MuGame.ScheduleOnMainThread(() =>
        {
            if (success) _characterState.PersonalShop.Close(owner, _characterState.Id);
            if (MuGame.Instance?.ActiveScene is GameScene scene) scene.PlayerStore?.CloseIfOwner(owner);
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0x3F, 0x08)]
    public Task HandleStoreSoldAsync(Memory<byte> packet)
    {
        if (packet.Length < 5) return Task.CompletedTask;
        byte slot = packet.Span[4];
        MuGame.ScheduleOnMainThread(() =>
        {
            _characterState.PersonalShop.Prices.Remove(slot);
            _characterState.RemoveInventoryItem(slot);
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0x3F, 0x13)]
    public Task HandlePlayerStoreUpdateAsync(Memory<byte> packet) => HandlePlayerStoreListAsync(packet);

    [PacketHandler(0x3F, 0x05)]
    public Task HandlePlayerStoreListAsync(Memory<byte> packet)
    {
        bool extended = _targetVersion == Client.Main.Core.Client.TargetProtocolVersion.Season6;
        if (!PlayerStorePacketReader.TryReadList(packet.Span, extended, out var list))
        {
            MuGame.ScheduleOnMainThread(() => MessageWindow.Show("This personal store is unavailable."));
            return Task.CompletedTask;
        }
        MuGame.ScheduleOnMainThread(() =>
        {
            if (MuGame.Instance?.ActiveScene is GameScene scene)
            {
                if (!scene.InventoryControl.Visible) scene.InventoryControl.Show();
                scene.PlayerStore?.OpenShop(list.Owner, list.Name, list.Title, list.Items);
            }
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0x3F, 0x06)]
    public Task HandlePlayerStoreBuyAsync(Memory<byte> packet)
    {
        bool extended = _targetVersion == Client.Main.Core.Client.TargetProtocolVersion.Season6;
        if (!PlayerStorePacketReader.TryReadPurchase(packet.Span, extended, out byte result, out byte slot, out var data))
            return Task.CompletedTask;
        MuGame.ScheduleOnMainThread(() =>
        {
            if (result == 1 && data.Length > 0) _characterState.AddOrUpdateInventoryItem(slot, data);
            else MessageWindow.Show(result == 7 ? "Not enough Zen." : "The item could not be bought. The store or item may have changed.");
        });
        return Task.CompletedTask;
    }
}
