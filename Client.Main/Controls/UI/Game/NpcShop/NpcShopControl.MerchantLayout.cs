using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game;

public partial class NpcShopControl
{
    private static readonly RasterizerState MerchantClipRasterizer = new()
    { CullMode = CullMode.None, ScissorTestEnable = true };
    private static readonly BlendState MerchantImageBlend = new()
    {
        ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
    };
    private readonly Dictionary<string, (Texture2D Texture, Rectangle Source)> _merchantImages = new();
    private float _shopScrollOffset, _shopScrollStartOffset;
    private Point _shopScrollStart;
    private bool _shopScrollTouch, _shopScrollDragged;

    private async Task LoadNativeMerchantFrameAsync()
    {
        foreach (string name in new[] { "newui_item_back04", "newui_item_back02-L", "newui_item_back02-R",
            "newui_item_back03", "newui_item_box", "mod_newui_item_box", "newui_item_money" })
        {
            string path = $"Interface/{name}.ozt";
            var texture = await UiThemeManager.LoadThemeTextureAsync(path);
            if (texture == null) continue;
            var data = TextureLoader.Instance.Get(path);
            _merchantImages[name] = (texture, new Rectangle(0, 0,
                Math.Min(texture.Width, data?.ContentWidth > 0 ? data.ContentWidth : texture.Width),
                Math.Min(texture.Height, data?.ContentHeight > 0 ? data.ContentHeight : texture.Height)));
        }
    }

    private void DrawMerchantImage(SpriteBatch batch, string name, Rectangle destination)
    {
        if (_merchantImages.TryGetValue(name, out var image) && !image.Texture.IsDisposed)
            batch.Draw(image.Texture, destination, image.Source, Color.White);
    }

    private bool DrawNativeMerchantSurface(SpriteBatch batch)
    {
        if (!_merchantImages.ContainsKey("newui_item_back04")) return false;
        using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred, MerchantImageBlend, SamplerState.LinearClamp);
        batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(0, 0, WINDOW_WIDTH, WindowHeight), new Color(15, 20, 23, 250));
        DrawMerchantImage(batch, "newui_item_back04", new Rectangle(0, 0, WINDOW_WIDTH, 115));
        DrawMerchantImage(batch, "newui_item_back02-L", new Rectangle(0, 115, 38, WindowHeight - 196));
        DrawMerchantImage(batch, "newui_item_back02-R", new Rectangle(WINDOW_WIDTH - 38, 115, 38, WindowHeight - 196));
        DrawMerchantImage(batch, "newui_item_back03", new Rectangle(0, WindowHeight - 81, WINDOW_WIDTH, 81));
        if (_font != null)
        {
            const float scale = .48f;
            string title = _isRepairShop ? "Merchant / Repair" : "Merchant";
            var size = _font.MeasureString(title) * scale;
            batch.DrawString(_font, title, new Vector2((WINDOW_WIDTH - size.X) / 2, 12), Color.White,
                0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
        batch.Draw(GraphicsManager.Instance.Pixel, _gridFrameRect, new Color(112, 113, 90));
        batch.Draw(GraphicsManager.Instance.Pixel, _gridRect, new Color(7, 10, 12));
        return true;
    }

    private float MaximumShopScroll
    {
        get
        {
            int rows = SHOP_CONTENT_ROWS;
            foreach (var item in _items) rows = Math.Max(rows, item.GridPosition.Y + item.Definition.Height);
            return Math.Max(0, rows * SHOP_SQUARE_HEIGHT - _gridRect.Height);
        }
    }

    private bool UpdateMerchantScroll(Point pointer, bool down, bool pressed, bool released)
    {
        var grid = Translate(_gridRect);
        if (pressed && grid.Contains(pointer) && !IsModalDialogOpen())
        {
            _shopScrollTouch = true;
            _shopScrollDragged = false;
            _shopScrollStart = pointer;
            _shopScrollStartOffset = _shopScrollOffset;
        }
        if (_shopScrollTouch && down)
        {
            int delta = pointer.Y - _shopScrollStart.Y;
            if (Math.Abs(delta) > 8) _shopScrollDragged = true;
            if (_shopScrollDragged) _shopScrollOffset = Math.Clamp(_shopScrollStartOffset - delta, 0, MaximumShopScroll);
            Scene?.SetMouseInputConsumed();
        }
        bool tap = released && _shopScrollTouch && !_shopScrollDragged && grid.Contains(pointer);
        if (released) _shopScrollTouch = false;
        int wheel = MuGame.Instance.UiMouseState.ScrollWheelValue - MuGame.Instance.PrevUiMouseState.ScrollWheelValue;
        if (wheel != 0 && grid.Contains(pointer))
            _shopScrollOffset = Math.Clamp(_shopScrollOffset - wheel / 120f * SHOP_SQUARE_HEIGHT, 0, MaximumShopScroll);
        return tap;
    }

    private void DrawClippedMerchantItems(SpriteBatch batch)
    {
        var gd = batch.GraphicsDevice;
        var previous = gd.ScissorRectangle;
        var grid = Translate(_gridRect);
        var transform = UiScaler.SpriteTransform;
        Vector2 top = Vector2.Transform(new Vector2(grid.Left, grid.Top), transform);
        Vector2 bottom = Vector2.Transform(new Vector2(grid.Right, grid.Bottom), transform);
        var clip = Rectangle.Intersect(gd.Viewport.Bounds, new Rectangle((int)MathF.Ceiling(top.X), (int)MathF.Ceiling(top.Y),
            Math.Max(0, (int)MathF.Floor(bottom.X) - (int)MathF.Ceiling(top.X)),
            Math.Max(0, (int)MathF.Floor(bottom.Y) - (int)MathF.Ceiling(top.Y))));
        try
        {
            gd.ScissorRectangle = clip;
            using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, MerchantClipRasterizer, transform: transform);
            if (IsSeason6)
                for (int row = 0; row <= _gridRect.Height / SHOP_SQUARE_HEIGHT; row++)
                    for (int col = 0; col < SHOP_COLUMNS; col++)
                    {
                        var cell = new Rectangle(grid.X + col * SHOP_SQUARE_WIDTH,
                            grid.Y + row * SHOP_SQUARE_HEIGHT - (int)_shopScrollOffset % SHOP_SQUARE_HEIGHT,
                            SHOP_SQUARE_WIDTH, SHOP_SQUARE_HEIGHT);
                        DrawMerchantImage(batch, "mod_newui_item_box", cell);
                        DrawMerchantImage(batch, "newui_item_box", cell);
                    }
            var scrolledGrid = _gridRect;
            scrolledGrid.Y -= (int)_shopScrollOffset;
            ItemGridRenderHelper.DrawGridOverlays(batch, GraphicsManager.Instance.Pixel, DisplayRectangle, scrolledGrid,
                _hoveredItem, _hoveredSlot, SHOP_SQUARE_WIDTH, SHOP_SQUARE_HEIGHT, Theme.SlotHover, Theme.Accent, Alpha);
            DrawShopItems(batch);
        }
        finally { gd.ScissorRectangle = previous; }
        if (MaximumShopScroll > 0)
        {
            var track = new Rectangle(grid.Right + 5, grid.Y, 3, grid.Height);
            batch.Draw(GraphicsManager.Instance.Pixel, track, Color.Black * .7f);
            int thumbHeight = Math.Max(20, (int)(grid.Height * grid.Height / (grid.Height + MaximumShopScroll)));
            int y = grid.Y + (int)((grid.Height - thumbHeight) * _shopScrollOffset / MaximumShopScroll);
            batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(track.X, y, track.Width, thumbHeight), Theme.TextGold);
        }
    }
}
