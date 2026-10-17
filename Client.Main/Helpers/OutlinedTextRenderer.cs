using System;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Helpers;

/// <summary>Draws readable world and message text with a crisp dark edge.</summary>
public static class OutlinedTextRenderer
{
    public static void Draw(SpriteBatch batch, SpriteFont font, string text,
        Vector2 position, Color color, float scale, float depth = 0f, bool pixelCoordinates = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        font = GraphicsManager.GetSmallTextFont(font, ref scale, pixelCoordinates);
        Matrix transform = pixelCoordinates ? Matrix.Identity : UiScaler.SpriteTransform;
        float pixelScale = Math.Max(.1f, MathF.Abs(transform.M11));
        position = new Vector2(
            (MathF.Round(position.X * pixelScale + transform.M41) - transform.M41) / pixelScale,
            (MathF.Round(position.Y * pixelScale + transform.M42) - transform.M42) / pixelScale);
        Color outline = new Color(0, 0, 0, (int)color.A);
        for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            batch.DrawString(font, text, position + new Vector2(x, y) * 1f / pixelScale, outline,
                0f, Vector2.Zero, scale, SpriteEffects.None, depth);
        }
        batch.DrawString(font, text, position, color,
            0f, Vector2.Zero, scale, SpriteEffects.None, depth);
    }
}
