using System;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Skills;

internal static class SkillCooldownRenderer
{
    // MuMain RenderSkillDelay: the lower part of the icon shrinks as the delay expires.
    internal static void Draw(SpriteBatch sprite, ushort skillId, Rectangle icon, float alpha = 1f, bool circular = false)
    {
        double now = MuGame.Instance?.GameTime?.TotalGameTime.TotalMilliseconds ?? Environment.TickCount64;
        float ratio = Math.Clamp(SkillCooldownTracker.GetCooldownRatio(skillId, now), 0f, 1f);
        var pixel = GraphicsManager.Instance.Pixel;
        if (ratio <= 0f || pixel == null || icon.Width <= 0 || icon.Height <= 0) return;
        int height = Math.Clamp((int)MathF.Ceiling(icon.Height * ratio), 1, icon.Height);
        Color tint = new Color(255, 128, 128, 128) * alpha;
        if (!circular)
        {
            sprite.Draw(pixel, new Rectangle(icon.X, icon.Bottom - height, icon.Width, height), tint);
            return;
        }
        float radiusX = icon.Width * .5f, radiusY = icon.Height * .5f;
        for (int y = icon.Height - height; y < icon.Height; y++)
        {
            float normalizedY = (y + .5f - radiusY) / radiusY;
            int halfWidth = (int)(radiusX * MathF.Sqrt(MathF.Max(0f, 1f - normalizedY * normalizedY)));
            if (halfWidth > 0) sprite.Draw(pixel, new Rectangle(icon.X + icon.Width / 2 - halfWidth, icon.Y + y, halfWidth * 2, 1), tint);
        }
    }
}
