using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>
    /// Shared renderer and reference geometry for MuMain's classic PC skill-bar cells.
    /// </summary>
    internal static class ClassicSkillSlotRenderer
    {
        public const int CellWidth = 32;
        public const int CellHeight = 38;
        public const int IconInsetX = 6;
        public const int IconInsetY = 6;
        public const int IconWidth = 20;
        public const int IconHeight = 28;

        public const string NormalFramePath = "Interface/newui_skillbox.OZJ";
        public const string ActiveFramePath = "Interface/newui_skillbox2.OZJ";

        public static Rectangle GetIconRectangle(Rectangle cell)
        {
            return new Rectangle(
                cell.X + Math.Max(1, (int)MathF.Round(cell.Width * IconInsetX / (float)CellWidth)),
                cell.Y + Math.Max(1, (int)MathF.Round(cell.Height * IconInsetY / (float)CellHeight)),
                Math.Max(1, (int)MathF.Round(cell.Width * IconWidth / (float)CellWidth)),
                Math.Max(1, (int)MathF.Round(cell.Height * IconHeight / (float)CellHeight)));
        }

        public static void Draw(
            SpriteBatch spriteBatch,
            Rectangle cell,
            ushort? skillId,
            bool active,
            Texture2D normalFrame,
            Texture2D activeFrame,
            Color tint)
        {
            Texture2D frame = active ? activeFrame ?? normalFrame : normalFrame ?? activeFrame;
            if (frame != null)
            {
                spriteBatch.Draw(frame, cell, new Rectangle(0, 0, frame.Width, frame.Height), tint);
            }

            if (skillId is > 0)
            {
                SkillIconRenderer.DrawSkillRect(spriteBatch, skillId.Value, GetIconRectangle(cell), tint);
            }
        }
    }
}
