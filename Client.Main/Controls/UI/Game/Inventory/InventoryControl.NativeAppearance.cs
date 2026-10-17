using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Inventory
{
    public partial class InventoryControl
    {
        private readonly Dictionary<string, Texture2D> _nativeInventoryImages = new();
        private readonly Dictionary<Texture2D, Rectangle> _inventoryImageAreas = new();
        private Rectangle _nativeFooterCloseRect;
        // Native MU textures store straight RGB. Keep the cached surface alpha
        // composited correctly so transparent frame pixels do not punch holes in it.
        private static readonly BlendState InventoryImageBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        private static readonly string[] NativeInventoryImageNames = {
            "newui_item_back04", "newui_item_back02-L", "newui_item_back02-R", "newui_item_back03",
            "newui_item_money", "newui_exit_00", "newui_repair_00", "newui_Bt_openshop", "newui_expansion_btn", "newui_item_box",
            "mod_newui_item_box", "newui_inven_usebox_01", "newui_inven_usebox_02", "newui_inven_usebox_03"
        };
        private static IEnumerable<string> NativeInventoryTexturePaths => NativeInventoryImageNames.Select(name => $"Interface/{name}.ozt");

        private void RememberInventoryImageArea(Texture2D texture, string path)
        {
            if (texture == null) return;
            var data = TextureLoader.Instance.Get(path);
            _inventoryImageAreas[texture] = new Rectangle(0, 0,
                Math.Min(texture.Width, data?.ContentWidth > 0 ? data.ContentWidth : texture.Width),
                Math.Min(texture.Height, data?.ContentHeight > 0 ? data.ContentHeight : texture.Height));
        }

        private async Task LoadNativeInventoryFrameAsync()
        {
            foreach (string name in NativeInventoryImageNames)
            {
                string path = $"Interface/{name}.ozt";
                var texture = await UiThemeManager.LoadThemeTextureAsync(path);
                if (texture == null) continue;
                _nativeInventoryImages[name] = texture;
                RememberInventoryImageArea(texture, path);
            }
        }

        private Rectangle InventoryImageArea(Texture2D texture) =>
            _inventoryImageAreas.TryGetValue(texture, out var area)
                ? area : new Rectangle(0, 0, texture.Width, texture.Height);

        private void DrawInventoryImage(SpriteBatch batch, string name, Rectangle destination,
            bool button = false, bool active = false, Color? tint = null)
        {
            if (!_nativeInventoryImages.TryGetValue(name, out var texture) || !IsUsableTexture(texture)) return;
            var source = InventoryImageArea(texture);
            if (button)
            {
                source.Height /= 2;
                if (active) source.Y += source.Height;
            }
            batch.Draw(texture, destination, source, tint ?? Color.White);
        }

        private void DrawNativeInventorySurface(SpriteBatch batch)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            batch.Draw(pixel, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), new Color(15, 20, 23, 250));
            if (IsUsableTexture(_s6Panel))
                batch.Draw(_s6Panel, new Rectangle(6, 6, WINDOW_WIDTH - 12, WINDOW_HEIGHT - 12), Color.White);
            DrawInventoryImage(batch, "newui_item_back04", new Rectangle(0, 0, WINDOW_WIDTH, 115));
            DrawInventoryImage(batch, "newui_item_back02-L", new Rectangle(0, 115, 38, WINDOW_HEIGHT - 196));
            DrawInventoryImage(batch, "newui_item_back02-R", new Rectangle(WINDOW_WIDTH - 38, 115, 38, WINDOW_HEIGHT - 196));
            DrawInventoryImage(batch, "newui_item_back03", new Rectangle(0, WINDOW_HEIGHT - 81, WINDOW_WIDTH, 81));

            DrawInventoryLabel(batch, "Gear", new Rectangle(90, 8, WINDOW_WIDTH - 180, 28), Color.White, 0.48f);
            foreach (var layout in _equipSlots.Values)
            {
                var texture = GetSeason6EquipmentTexture(layout.Slot);
                if (!IsUsableTexture(texture)) { DrawModernEquipSlot(batch, layout); continue; }
                var size = Hud.InventoryLayout.GetEquipmentAssetSize(layout.Slot);
                int width = Math.Max(1, size.Width), height = Math.Max(1, size.Height);
                var destination = new Rectangle(layout.Rect.Center.X - width / 2,
                    layout.Rect.Center.Y - height / 2, width, height);
                batch.Draw(texture, destination, InventoryImageArea(texture), Color.White);
            }

            batch.Draw(pixel, new Rectangle(_gridRect.X - 3, _gridRect.Y - 3, _gridRect.Width + 6, _gridRect.Height + 6), new Color(87, 85, 72));
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                {
                    var cell = new Rectangle(_gridRect.X + col * Hud.InventoryLayout.GridCellSize,
                        _gridRect.Y + row * Hud.InventoryLayout.GridCellSize,
                        Hud.InventoryLayout.GridCellSize, Hud.InventoryLayout.GridCellSize);
                    batch.Draw(pixel, cell, new Color(13, 16, 17));
                    DrawInventoryImage(batch, "mod_newui_item_box", cell);
                    DrawInventoryImage(batch, "newui_item_box", cell, tint: new Color(110, 110, 110));
                }
            DrawInventoryImage(batch, "newui_item_money", new Rectangle(Hud.InventoryLayout.MoneyX, Hud.InventoryLayout.MoneyY - 5, Hud.InventoryLayout.MoneyW, Hud.InventoryLayout.MoneyH));
        }

        private void DrawInventoryLabel(SpriteBatch batch, string text, Rectangle rect, Color color, float scale)
        {
            if (_font == null) return;
            Vector2 size = _font.MeasureString(text) * scale;
            var position = new Vector2(rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2);
            batch.DrawString(_font, text, position + Vector2.One, Color.Black * .7f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            batch.DrawString(_font, text, position, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void DrawNativeInventoryChrome(SpriteBatch batch)
        {
            using var scope = new Client.Main.Helpers.SpriteBatchScope(batch, SpriteSortMode.Deferred,
                InventoryImageBlend, SamplerState.LinearClamp, DepthStencilState.None,
                transform: UiScaler.SpriteTransform);
            // The native header already contains its blue X. Match its hit area
            // instead of drawing a second, unrelated exit icon over it.
            if(!_nativeInventoryImages.ContainsKey("newui_item_back04"))
                DrawInventoryImage(batch, "newui_exit_00", Translate(_closeButtonRect), true, _closeHovered);
            else if(_closeHovered)batch.Draw(GraphicsManager.Instance.Pixel,Translate(_closeButtonRect),Color.White*.15f);
            DrawInventoryImage(batch, "newui_exit_00", Translate(_nativeFooterCloseRect), true,
                Translate(_nativeFooterCloseRect).Contains(MuGame.Instance.UiMouseState.Position));
            DrawInventoryImage(batch, "newui_repair_00", Translate(_repairOptionRect), true, _isRepairMode || _repairOptionHovered);
            DrawInventoryImage(batch, "newui_Bt_openshop", Translate(_footerLeftButtonRect), true, _leftFooterHovered);
            DrawInventoryImage(batch, "newui_expansion_btn", Translate(_footerRightButtonRect), true, _rightFooterHovered);
            DrawInventoryLabel(batch, "[Set option]", Translate(_setOptionRect), new Color(150, 154, 158), .38f * Scale);
            DrawInventoryLabel(batch, "[Socket option]", Translate(_socketOptionRect), new Color(150, 154, 158), .38f * Scale);
        }

        private void DrawNativeGridHighlights(SpriteBatch batch, Rectangle grid, InventoryItem dragged, bool isOverGrid)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            // Occupancy shading must remain visible even when the pointer leaves the bag.
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Columns; x++)
                    if (_itemGrid[x, y] != null)
                        batch.Draw(pixel, new Rectangle(grid.X + x * INVENTORY_SQUARE_WIDTH + 1,
                            grid.Y + y * INVENTORY_SQUARE_HEIGHT + 1,
                            INVENTORY_SQUARE_WIDTH - 2, INVENTORY_SQUARE_HEIGHT - 2), Color.Black * (.4f * Alpha));
            if (dragged == null || !isOverGrid || _hoveredSlot.X < 0 || _hoveredSlot.Y < 0) return;

            int xSlot = _hoveredSlot.X, ySlot = _hoveredSlot.Y;
            int width = dragged.Definition.Width;
            int height = dragged.Definition.Height;
            int visibleWidth = Math.Min(width, Columns - xSlot);
            int visibleHeight = Math.Min(height, Rows - ySlot);
            if (visibleWidth <= 0 || visibleHeight <= 0) return;
            var destination = new Rectangle(grid.X + xSlot * INVENTORY_SQUARE_WIDTH,
                grid.Y + ySlot * INVENTORY_SQUARE_HEIGHT,
                visibleWidth * INVENTORY_SQUARE_WIDTH, visibleHeight * INVENTORY_SQUARE_HEIGHT);

            using var scope = new Client.Main.Helpers.SpriteBatchScope(batch, SpriteSortMode.Deferred,
                InventoryImageBlend, SamplerState.LinearClamp, DepthStencilState.None, transform: UiScaler.SpriteTransform);
            Color tint = Color.White * Alpha;
            for (int row = 0; row < visibleHeight; row++)
            {
                string name = height == 1 ? "newui_inven_usebox_01" : row == 0
                    ? "newui_inven_usebox_02" : row == height - 1
                        ? "newui_inven_usebox_03" : "newui_inven_usebox_01";
                var rowRect = new Rectangle(destination.X, destination.Y + row * INVENTORY_SQUARE_HEIGHT,
                    destination.Width, INVENTORY_SQUARE_HEIGHT);
                if (_nativeInventoryImages.TryGetValue(name, out var texture) && IsUsableTexture(texture))
                {
                    var source = InventoryImageArea(texture);
                    if (height > 2 && row > 0 && row < height - 1)
                    { source.Y++; source.Height = Math.Max(1, source.Height - 2); }
                    batch.Draw(texture, rowRect, source, tint);
                }
                else batch.Draw(pixel, rowRect, Color.Cyan * .35f);
            }
            if (dragged != null)
                for (int row = 0; row < visibleHeight; row++)
                    for (int col = 0; col < visibleWidth; col++)
                        if (IsPlacementCellBlocked(dragged, xSlot + col, ySlot + row) ||
                            width > visibleWidth && col == visibleWidth - 1 ||
                            height > visibleHeight && row == visibleHeight - 1)
                            batch.Draw(pixel, new Rectangle(destination.X + col * INVENTORY_SQUARE_WIDTH,
                                destination.Y + row * INVENTORY_SQUARE_HEIGHT,
                                INVENTORY_SQUARE_WIDTH, INVENTORY_SQUARE_HEIGHT), new Color(230, 35, 25) * (.65f * Alpha));
        }

        private bool IsPlacementCellBlocked(InventoryItem dragged, int x, int y) =>
            x < 0 || y < 0 || x >= Columns || y >= Rows ||
            _itemGrid[x, y] != null && !ReferenceEquals(_itemGrid[x, y], dragged);

        private bool IsEquipmentPlacementBlocked(InventoryItem dragged, byte slot)
        {
            if (dragged == null) return false;
            if (_equippedItems.TryGetValue(slot, out var occupant) && !ReferenceEquals(occupant, dragged)) return true;
            int expected = dragged.Definition.EquipmentSlot;
            bool matches = expected == slot || expected == 10 && slot == 11 || expected == 11 && slot == 10 ||
                expected == 0 && slot == 1 && !dragged.Definition.TwoHanded;
            if (!matches) return true;
            if (slot == 0 && dragged.Definition.TwoHanded && _equippedItems.ContainsKey(1)) return true;
            var state = _networkManager?.GetCharacterState();
            return state != null && !Core.Utilities.MuMainItemRules.MeetsRequirements(dragged, state);
        }

        private void DrawNativeEquipmentHighlight(SpriteBatch batch, Rectangle rect, bool blocked)
        {
            using var scope = new Client.Main.Helpers.SpriteBatchScope(batch, SpriteSortMode.Deferred,
                InventoryImageBlend, SamplerState.LinearClamp, DepthStencilState.None, transform: UiScaler.SpriteTransform);
            if (_nativeInventoryImages.TryGetValue("newui_inven_usebox_01", out var texture) && IsUsableTexture(texture))
                batch.Draw(texture, rect, InventoryImageArea(texture), Color.White * Alpha);
            else batch.Draw(GraphicsManager.Instance.Pixel, rect, Color.Cyan * (.35f * Alpha));
            if (blocked) batch.Draw(GraphicsManager.Instance.Pixel, rect, new Color(230, 35, 25) * (.65f * Alpha));
        }

    }
}
