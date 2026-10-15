using Client.Main.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Core.Client;
using Client.Main.Models;
using Client.Main.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Inventory;

/// <summary>Server-backed personal store and 8 by 4 inventory extension pages.</summary>
public sealed class InventoryAuxiliaryControl : UIControl
{
    public const int StoreOffset = 204;
    private readonly NetworkManager _network;
    private readonly InventoryControl _inventory;
    private readonly Dictionary<byte, InventoryItem> _items = new();
    private readonly InventoryItem[,] _grid = new InventoryItem[8, 4];
    private InventoryItem _dragged;
    private byte _draggedSlot;
    private Point _pressedAt;
    private bool _moving;
    private Action _afterStoreClose;
    public bool IsDraggingItem => Visible && _dragged != null;
    private int _page;
    private bool _store;
    private volatile bool _itemsDirty = true;
    private readonly CharacterState _state;
    private CharacterState State => _state;
    private PersonalShopState Shop => State.PersonalShop;
    private int SlotOffset => _store ? StoreOffset : 76 + _page * 32;
    private Rectangle GridBounds => new(DisplayRectangle.X + 20, DisplayRectangle.Y + 70, 272, 136);
    private Rectangle CloseBounds => new(DisplayRectangle.Right - 38, DisplayRectangle.Y + 8, 28, 28);
    private Rectangle ActionBounds => new(DisplayRectangle.X + 20, DisplayRectangle.Y + 250, 132, 34);
    private Rectangle NameBounds => new(DisplayRectangle.X + 164, DisplayRectangle.Y + 250, 128, 34);
    public bool StoreVisible => Visible && _store;

    public InventoryAuxiliaryControl(NetworkManager network, InventoryControl inventory)
    {
        _network = network;
        _state = network.GetCharacterState();
        _state.InventoryChanged += OnInventoryChanged;
        _inventory = inventory;
        AutoViewSize = false;
        ControlSize = ViewSize = new Point(312, 310);
        Interactive = true;
        Visible = false;
    }

    private bool IsInSafeZone => Scene?.World is WalkableWorldControl world && world.Walker != null &&
        world.Terrain.RequestTerrainFlag((int)world.Walker.Location.X, (int)world.Walker.Location.Y)
            .HasFlag(Client.Data.ATT.TWFlags.SafeZone);

    public void Open(bool store)
    {
        if (store && !IsInSafeZone)
        {
            MessageWindow.Show("Personal Store is only available in a safe zone.");
            return;
        }
        if (!store && State.InventoryExpansionState == 0)
        {
            MessageWindow.Show("No expanded inventory is unlocked for this character.");
            return;
        }
        _store = store;
        _itemsDirty = true;
        _page = 0;
        Align = ControlAlign.None;
        X = Math.Max(8, _inventory.DisplayRectangle.X - 320);
        Y = Math.Max(8, _inventory.DisplayRectangle.Y + 100);
        Visible = true;
        BringToFront();
    }

    public void Hide()
    {
        _dragged = null;
        _afterStoreClose = null;
        _itemsDirty = true;
        Visible = false;
    }

    private void OnInventoryChanged() => _itemsDirty = true;

    public override void Dispose()
    {
        _state.InventoryChanged -= OnInventoryChanged;
        base.Dispose();
    }

    private void RefreshItems()
    {
        if (!_itemsDirty) return;
        _itemsDirty = false;
        _items.Clear();
        Array.Clear(_grid);
        foreach (var entry in State.GetInventoryItems())
        {
            int index = entry.Key - SlotOffset;
            if (index < 0 || index >= 32 || entry.Key == _draggedSlot && _dragged != null) continue;
            var definition = ItemDatabase.GetItemDefinition(entry.Value);
            if (definition == null) continue;
            var item = new InventoryItem(definition, new Point(index % 8, index / 8), entry.Value);
            _items[entry.Key] = item;
            for (int y = item.GridPosition.Y; y < Math.Min(4, item.GridPosition.Y + definition.Height); y++)
                for (int x = item.GridPosition.X; x < Math.Min(8, item.GridPosition.X + definition.Width); x++)
                    _grid[x, y] = item;
        }
    }

    private Point SlotAt(Point point)
    {
        var bounds = GridBounds;
        return bounds.Contains(point) ? new Point((point.X - bounds.X) / 34, (point.Y - bounds.Y) / 34) : new Point(-1);
    }

    private bool CanPlace(InventoryItem item, Point position)
    {
        if (position.X < 0 || position.Y < 0 || position.X + item.Definition.Width > 8 || position.Y + item.Definition.Height > 4) return false;
        for (int y = position.Y; y < position.Y + item.Definition.Height; y++)
            for (int x = position.X; x < position.X + item.Definition.Width; x++)
                if (_grid[x, y] != null) return false;
        return true;
    }

    public bool AcceptFromInventory(InventoryItem item, byte source, Point pointer)
    {
        if (!Visible || !DisplayRectangle.Contains(pointer)) return false;
        if (State.PendingMoveFromSlot != null) { MessageWindow.Show("Please wait for the previous item move."); return true; }
        if (_store && Shop.IsOpen) { MessageWindow.Show("Close your store before changing its items."); return true; }
        RefreshItems();
        var position = SlotAt(pointer);
        if (!CanPlace(item, position)) { MessageWindow.Show("There is not enough space at that slot."); return true; }
        byte target = (byte)(SlotOffset + position.Y * 8 + position.X);
        StartMove(item, source, target);
        return true;
    }

    private void StartMove(InventoryItem item, byte source, byte target)
    {
        if (source == target) return;
        if (!_network.IsConnected || State.PendingMoveFromSlot != null || Shop.PendingPrices.Count != 0)
        { MessageWindow.Show("Please wait for the previous update and check your connection."); return; }
        if ((source >= StoreOffset || target >= StoreOffset) && Shop.IsOpen)
        { MessageWindow.Show("Close your store before changing its items."); return; }
        var items = State.GetInventoryItems();
        if (!items.TryGetValue(source, out var raw) || !raw.SequenceEqual(item.RawData))
        { State.RaiseInventoryChanged(); return; }
        State.StashPendingInventoryMove(source, target);
        _ = SendMoveAsync(item, source, target);
    }

    private async System.Threading.Tasks.Task SendMoveAsync(InventoryItem item, byte source, byte target)
    {
        await _network.GetCharacterService().SendStorageItemMoveAsync(
            source >= StoreOffset ? MUnique.OpenMU.Network.Packets.ItemStorageKind.PlayerShop : MUnique.OpenMU.Network.Packets.ItemStorageKind.Inventory,
            source, target >= StoreOffset ? MUnique.OpenMU.Network.Packets.ItemStorageKind.PlayerShop : MUnique.OpenMU.Network.Packets.ItemStorageKind.Inventory,
            target, _network.TargetVersion, item.RawData);
    }

    private void ItemActions(byte slot)
    {
        RefreshItems();
        if (!_items.TryGetValue(slot, out var item)) return;
        if (Shop.IsOpen)
        {
            RequestDialog.Show("Close your store to edit this item?", onAccept: () =>
            {
                _afterStoreClose = () => ItemActions(slot);
                _ = _network.GetCharacterService().SendPlayerShopCloseAsync();
            }, acceptText: "Close store", rejectText: "Cancel");
            return;
        }
        RequestDialog.Show(item.Definition.Name, onAccept: () => SetPrice(slot), acceptText: "Set price", rejectText: "Cancel",
            onExtra: () => RemoveToInventory(slot), extraText: "Remove to inventory");
    }

    private void RemoveToInventory(byte slot)
    {
        if (Shop.IsOpen || State.PendingMoveFromSlot != null || !_items.TryGetValue(slot, out var item)) return;
        if (!_inventory.FindFreeBagSlot(item, out var destination))
        { MessageWindow.Show("There is no room for this item in your inventory."); return; }
        byte target = (byte)(12 + destination.Y * 8 + destination.X);
        StartMove(item, slot, target);
    }

    private void SetPrice(byte slot)
    {
        if (Shop.PendingPrices.ContainsKey(slot)) { MessageWindow.Show("Please wait for the previous price update."); return; }
        if (Shop.IsOpen) { MessageWindow.Show("Close your store before changing prices."); return; }
        InventoryTextDialog.Show("Item price in Zen", Shop.Prices.GetValueOrDefault(slot).ToString(), text =>
        {
            if (!uint.TryParse(text, out uint price) || price == 0 || price > 2_000_000_000)
            { MessageWindow.Show("Enter a price between 1 and 2,000,000,000 Zen."); return; }
            if (!State.HasInventoryItem(slot) || Shop.IsOpen) return;
            Shop.PendingPrices[slot] = price;
            _ = _network.GetCharacterService().SendPlayerShopSetItemPriceAsync(slot, price);
        });
    }

    private void StoreAction()
    {
        if (Shop.IsOpen) { _ = _network.GetCharacterService().SendPlayerShopCloseAsync(); return; }
        if (!IsInSafeZone) { MessageWindow.Show("Personal Store can only open in a safe zone."); return; }
        if (State.PendingMoveFromSlot != null || Shop.PendingPrices.Count != 0)
        { MessageWindow.Show("Please wait for the item or price update."); return; }
        if (_items.Count == 0 || _items.Keys.Any(slot => !Shop.Prices.TryGetValue(slot, out uint price) || price == 0))
        { MessageWindow.Show("Add an item and set a price for every item before opening your store."); return; }
        if (string.IsNullOrWhiteSpace(Shop.Title)) { MessageWindow.Show("Enter a store name first."); return; }
        _ = _network.GetCharacterService().SendPlayerShopOpenAsync(Shop.Title);
    }

    public override void Update(GameTime gameTime)
    {
        if (!Visible) return;
        base.Update(gameTime);
        RefreshItems();
        if (!Shop.IsOpen && _afterStoreClose != null)
        {
            var action = _afterStoreClose; _afterStoreClose = null; action(); return;
        }
        if (Scene?.Controls.GetSnapshotArray().Any(c => c is DialogControl { Visible: true }) == true) return;
        var mouse = MuGame.Instance.UiMouseState;
        var previous = MuGame.Instance.PrevUiMouseState;
        bool rightPressed = mouse.RightButton == ButtonState.Pressed && previous.RightButton == ButtonState.Released;
        bool pressed = mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released;
        bool released = mouse.LeftButton == ButtonState.Released && previous.LeftButton == ButtonState.Pressed;
        Point pointer = mouse.Position;
        if (DisplayRectangle.Contains(pointer)) Scene?.SetMouseInputConsumed();
        if (rightPressed && _store && State.PendingMoveFromSlot == null)
        {
            Point square = SlotAt(pointer);
            if (square.X >= 0 && _grid[square.X, square.Y] is { } pricedItem)
            {
                byte slot = (byte)(SlotOffset + pricedItem.GridPosition.Y * 8 + pricedItem.GridPosition.X);
                if (!Shop.IsOpen) SetPrice(slot);
                else RequestDialog.Show("Close your store to change this price?", onAccept: () =>
                {
                    _afterStoreClose = () => SetPrice(slot);
                    _ = _network.GetCharacterService().SendPlayerShopCloseAsync();
                }, acceptText: "Close store", rejectText: "Cancel");
                return;
            }
        }
        if (pressed && CloseBounds.Contains(pointer)) { Hide(); return; }
        if (pressed && ActionBounds.Contains(pointer))
        {
            if (_store) StoreAction();
            else { _page = (_page + 1) % Math.Clamp((int)State.InventoryExpansionState, 1, 4); _itemsDirty = true; }
            return;
        }
        if (pressed && _store && NameBounds.Contains(pointer) && !Shop.IsOpen)
        {
            InventoryTextDialog.Show("Personal store name", Shop.Title, text =>
            {
                if (string.IsNullOrWhiteSpace(text) || System.Text.Encoding.UTF8.GetByteCount(text) > 35)
                    MessageWindow.Show("Enter a store name of up to 35 bytes.");
                else Shop.Title = text.Trim();
            });
            return;
        }
        if (pressed && _inventory.GetDraggedItem() == null && State.PendingMoveFromSlot == null)
        {
            Point position = SlotAt(pointer);
            if (position.X >= 0 && _grid[position.X, position.Y] is { } item)
            {
                if (_store && Shop.IsOpen) { if (UiThemeManager.IsMobilePlatform) ItemActions((byte)(SlotOffset + item.GridPosition.Y * 8 + item.GridPosition.X)); else MessageWindow.Show("Close your store before moving its items. Right-click to change a price."); return; }
                _dragged = item;
                _itemsDirty = true;
                _draggedSlot = (byte)(SlotOffset + item.GridPosition.Y * 8 + item.GridPosition.X);
                _pressedAt = pointer;
                _moving = false;
            }
        }
        if (_dragged != null && Vector2.Distance(pointer.ToVector2(), _pressedAt.ToVector2()) > 8) _moving = true;
        if (released && _dragged != null)
        {
            var item = _dragged;
            byte source = _draggedSlot;
            _dragged = null;
            _itemsDirty = true;
            if (!_moving) { if (_store && UiThemeManager.IsMobilePlatform) ItemActions(source); return; }
            var destination = _inventory.GetSlotAtScreenPositionPublic(pointer);
            if (destination.X >= 0 && _inventory.CanPlaceAt(destination, item))
            {
                byte target = (byte)(12 + destination.Y * 8 + destination.X);
                StartMove(item, source, target);
            }
            else
            {
                var position = SlotAt(pointer);
                if (CanPlace(item, position))
                {
                    byte target = (byte)(SlotOffset + position.Y * 8 + position.X);
                    StartMove(item, source, target);
                }
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        var sprite = GraphicsManager.Instance.Sprite;
        var pixel = GraphicsManager.Instance.Pixel;
        var font = GraphicsManager.Instance.Font;
        if (sprite == null || pixel == null || font == null) return;
        SpriteBatchScope? scope = null;
        if (!SpriteBatchScope.BatchIsBegun) scope = new SpriteBatchScope(sprite, transform: UiScaler.SpriteTransform);
        try
        {
            sprite.Draw(pixel, DisplayRectangle, new Color(22, 18, 15, 248));
            void Text(string text, int x, int y, Color color, float scale = .44f) =>
                sprite.DrawString(font, text, new Vector2(DisplayRectangle.X + x, DisplayRectangle.Y + y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            Text(_store ? "Personal store" : $"Expanded inventory {_page + 1}/{State.InventoryExpansionState}", 20, 14, Color.Gold);
            Text("X", 280, 14, Color.White);
            Text(_store ? (Shop.IsOpen ? "Store is open" : Shop.Title) : "Drag items between this bag and inventory", 20, 44, Color.Wheat, .36f);
            var grid = GridBounds;
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 8; x++)
                    sprite.Draw(pixel, new Rectangle(grid.X + x * 34, grid.Y + y * 34, 33, 33), new Color(45, 40, 32));
            foreach (var entry in _items)
            {
                var item = entry.Value;
                var rect = new Rectangle(grid.X + item.GridPosition.X * 34, grid.Y + item.GridPosition.Y * 34, item.Definition.Width * 34, item.Definition.Height * 34);
                var texture = _inventory.ResolveAuxiliaryItemTexture(item, rect.Width, rect.Height);
                if (texture != null) sprite.Draw(texture, BmdPreviewRenderer.GetItemDrawRectangle(texture, rect), Color.White);
                else Text(item.Definition.Name, rect.X - DisplayRectangle.X, rect.Y - DisplayRectangle.Y, Color.White, .25f);
                if (_store)
                    sprite.DrawString(font, Shop.Prices.TryGetValue(entry.Key, out uint price) ? price.ToString("N0") : "Set price", new Vector2(rect.X, rect.Bottom - 12), Color.Gold, 0, Vector2.Zero, .28f, SpriteEffects.None, 0);
            }
            Text(_store ? (UiThemeManager.IsMobilePlatform ? "Tap: price / remove. Close store to edit." : "Right-click: price. Drag: move to inventory.") : "Only unlocked inventory pages are available.", 20, 220, Color.Wheat, .34f);
            sprite.Draw(pixel, ActionBounds, new Color(78, 54, 30));
            Text(_store ? (Shop.IsOpen ? "Close store" : "Open store") : "Next bag", 28, 258, Color.White);
            if (_store) { sprite.Draw(pixel, NameBounds, new Color(78, 54, 30)); Text("Store name", 172, 258, Color.White); }
            if (_dragged != null && _moving)
            {
                var texture = _inventory.ResolveAuxiliaryItemTexture(_dragged, _dragged.Definition.Width * 34, _dragged.Definition.Height * 34);
                if (texture != null) sprite.Draw(texture, BmdPreviewRenderer.GetItemDrawRectangle(texture, new Rectangle(MuGame.Instance.UiMouseState.X - 16, MuGame.Instance.UiMouseState.Y - 16, _dragged.Definition.Width * 34, _dragged.Definition.Height * 34)), Color.White);
            }
        }
        finally { scope?.Dispose(); }
    }
}
