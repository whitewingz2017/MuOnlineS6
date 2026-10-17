using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Client.Main.Helpers
{
    public static class OverheadNameplateRenderer
    {
        private readonly struct QueuedNameplate
        {
            public QueuedNameplate(SpriteFont font, Vector3 screen, string name, float? healthFraction, float renderScale, bool isStore, bool showHealthBar, Color nameColor)
            {
                IsStore = isStore;
                NameColor = nameColor;
                ShowHealthBar = showHealthBar;
                Font = font;
                Screen = screen;
                Name = name;
                HealthFraction = healthFraction;
                RenderScale = renderScale;
                SourceViewport = MuGame.Instance.GraphicsDevice.Viewport;
            }

            public bool IsStore { get; }
            public Color NameColor { get; }
            public bool ShowHealthBar { get; }
            public SpriteFont Font { get; }
            public Vector3 Screen { get; }
            public string Name { get; }
            public float? HealthFraction { get; }
            public float RenderScale { get; }
            public Viewport SourceViewport { get; }
        }

        private const float BaseNameScale = 0.50f;
        private const float BaseBarWidth = 70f;
        private const float BaseBarHeight = 6f;
        private const float BaseBarOffsetY = 2f;

        private const float BaseNamePaddingX = 4f;
        private const float BaseNamePaddingY = 2f;
        private const float BaseNameBarGap = 2f;

        private const int MaxRetainedQueueCapacity = 512;
        private static readonly List<QueuedNameplate> s_queue = new(256);
        internal static bool DeferToNativeOverlay { get; set; }
        public static int LastFrameQueuedNameplates { get; private set; }

        private static class Theme
        {
            public static readonly Color BgDarkest = new(8, 10, 14, 252);
            public static readonly Color BgDark = new(16, 20, 26, 250);
            public static readonly Color BgMid = new(24, 30, 38, 248);
            public static readonly Color BgLight = new(35, 42, 52, 245);
            public static readonly Color BgLighter = new(48, 56, 68, 240);

            public static readonly Color Accent = new(212, 175, 85);
            public static readonly Color AccentBright = new(255, 215, 120);
            public static readonly Color AccentDim = new(140, 115, 55);
            public static readonly Color AccentGlow = new(255, 200, 80, 40);

            public static readonly Color Secondary = new(90, 140, 200);
            public static readonly Color SecondaryBright = new(130, 180, 240);
            public static readonly Color SecondaryDim = new(50, 80, 120);

            public static readonly Color BorderOuter = new(5, 6, 8, 255);
            public static readonly Color BorderInner = new(60, 70, 85, 200);
            public static readonly Color BorderHighlight = new(100, 110, 130, 120);

            public static readonly Color SlotBg = new(12, 15, 20, 240);
            public static readonly Color SlotBorder = new(45, 52, 65, 180);
            public static readonly Color SlotHover = new(70, 85, 110, 150);
            public static readonly Color SlotSelected = new(212, 175, 85, 100);

            public static readonly Color TextWhite = new(240, 240, 245);
            public static readonly Color TextGold = new(255, 220, 130);
            public static readonly Color TextGray = new(160, 165, 175);
            public static readonly Color TextDark = new(100, 105, 115);

            public static readonly Color Success = new(80, 200, 120);
            public static readonly Color Warning = new(240, 180, 60);
            public static readonly Color Danger = new(220, 80, 80);
        }

        public static bool TryProject(BoundingBox bounds, float zOffset, out Vector3 screen)
        {
            Vector3 anchor = new(
                (bounds.Min.X + bounds.Max.X) * 0.5f,
                (bounds.Min.Y + bounds.Max.Y) * 0.5f,
                bounds.Max.Z + zOffset);

            screen = MuGame.Instance.GraphicsDevice.Viewport.Project(
                anchor,
                Camera.Instance.Projection,
                Camera.Instance.View,
                Matrix.Identity);

            return screen.Z >= 0f && screen.Z <= 1f;
        }

        public static void BeginFrame()
        {
            s_queue.Clear();
            Client.Main.Graphics.WorldLabelRenderer.BeginFrame();
            Client.Main.Objects.Effects.DamageTextObject.BeginOverlayFrame();
        }

        public static void EnqueueNameplate(SpriteFont font, Vector3 screen, string name, float? healthFraction, float renderScale, bool isStore = false, bool showHealthBar = false, Color? nameColor = null)
        {
            if (font == null || string.IsNullOrWhiteSpace(name))
                return;

            s_queue.Add(new QueuedNameplate(font, screen, name, healthFraction, renderScale, isStore, showHealthBar, nameColor ?? Color.White));
        }

        public static void FlushQueuedNameplates(SpriteBatch spriteBatch)
        {
            if (DeferToNativeOverlay || spriteBatch == null || s_queue.Count == 0)
                return;

            // Use an overlay batch even when the caller has world depth writes enabled.
            // Deferred ordering keeps the trough behind its textured health fill.
            using var scope = new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None);

            try
            {
                for (int i = 0; i < s_queue.Count; i++)
                {
                    var entry = s_queue[i];
                    // Projection was measured in the world target. Reproject into the
                    // final viewport so font pixels never pass through world upscaling.
                    var source = entry.SourceViewport;
                    var destination = spriteBatch.GraphicsDevice.Viewport;
                    float ratioX = destination.Width / (float)Math.Max(1, source.Width);
                    float ratioY = destination.Height / (float)Math.Max(1, source.Height);
                    Vector3 screen = new(
                        destination.X + (entry.Screen.X - source.X) * ratioX,
                        destination.Y + (entry.Screen.Y - source.Y) * ratioY,
                        entry.Screen.Z);
                    DrawNameplate(
                        spriteBatch,
                        entry.Font,
                        screen,
                        entry.Name,
                        entry.HealthFraction,
                        entry.RenderScale * ratioY, entry.IsStore, entry.ShowHealthBar, entry.NameColor);
                }
            }
            finally
            {
                LastFrameQueuedNameplates = s_queue.Count;
                s_queue.Clear();
                if (s_queue.Capacity > MaxRetainedQueueCapacity)
                    s_queue.Capacity = MaxRetainedQueueCapacity;
            }
        }

        public static void DrawNameplate(SpriteBatch spriteBatch, SpriteFont font, Vector3 screen, string name, float? healthFraction, float renderScale, bool isStore = false, bool showHealthBar = false, Color? nameColor = null)
        {
            if (spriteBatch == null || font == null || string.IsNullOrEmpty(name))
                return;

            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null)
                return;

            float scale = MathF.Max(renderScale, 0.1f);
            if (Client.Main.Controls.UI.UiThemeManager.IsMobilePlatform)
                scale = MathF.Max(scale, MathF.Min(UiScaler.ScaleX, UiScaler.ScaleY) * 1.25f);
            float nameScale = BaseNameScale * scale;
            font = GraphicsManager.GetSmallTextFont(font, ref nameScale);
            Vector2 nameSize = font.MeasureString(name) * nameScale;

            float padX = MathF.Max(1f, BaseNamePaddingX * scale);
            float padY = MathF.Max(1f, BaseNamePaddingY * scale);
            bool hasBar = healthFraction.HasValue || showHealthBar;
            float barWidth = MathF.Max(BaseBarWidth * scale, nameSize.X + padX * 2f);
            float barHeight = hasBar ? MathF.Max(6f, BaseBarHeight * scale) : 0f;
            float barOffset = MathF.Max(1f, BaseBarOffsetY * scale);

            float nameGap = MathF.Max(1f, BaseNameBarGap * scale);

            float barBottomY = screen.Y - barOffset;
            float barTopY = barBottomY - barHeight;

            float nameBgHeight = nameSize.Y + padY * 2f;
            float nameBgWidth = nameSize.X + padX * 2f;

            int barX = (int)MathF.Round(screen.X - barWidth * 0.5f);
            int barY = (int)MathF.Round(barTopY);
            int barW = Math.Max(1, (int)MathF.Round(barWidth));
            int barH = Math.Max(1, (int)MathF.Round(barHeight));

            int nameX = (int)MathF.Round(screen.X - nameBgWidth * 0.5f);
            float nameBaseY = hasBar ? (barTopY - nameGap) : (screen.Y - barOffset);
            int nameY = (int)MathF.Round(nameBaseY - nameBgHeight);
            int nameW = Math.Max(1, (int)MathF.Round(nameBgWidth));
            int nameH = Math.Max(1, (int)MathF.Round(nameBgHeight));

            Rectangle nameRect = new(nameX, nameY, nameW, nameH);
            Rectangle barRect = new(barX, barY, barW, barH);

            var viewport = spriteBatch.GraphicsDevice.Viewport;
            int minX = Math.Min(nameRect.Left, barRect.Left);
            int maxX = Math.Max(nameRect.Right, barRect.Right);
            int minY = Math.Min(nameRect.Top, barRect.Top);
            int maxY = Math.Max(nameRect.Bottom, barRect.Bottom);
            if (maxX < 0 || minX > viewport.Width || maxY < 0 || minY > viewport.Height)
                return;

            float depth = MathHelper.Clamp(screen.Z, 0f, 1f);
            Vector2 textPos = new(MathF.Round(nameRect.X + padX), MathF.Round(nameRect.Y + padY));

            if(isStore)
            {
                spriteBatch.Draw(pixel, nameRect, null, Theme.BgDarkest * 0.85f, 0f, Vector2.Zero, SpriteEffects.None, depth);
                DrawBorder(spriteBatch, pixel, nameRect, Theme.AccentDim, depth);
            }

            if (isStore)
            {
                float y = textPos.Y;
                foreach (string line in name.Split('\n'))
                {
                    Vector2 pos = new(screen.X - font.MeasureString(line).X * nameScale / 2f, y);
                    OutlinedTextRenderer.Draw(spriteBatch, font, line, pos, Theme.TextGold, nameScale, depth, pixelCoordinates: true);
                    y += font.LineSpacing * nameScale;
                }
            }
            else
            {
                OutlinedTextRenderer.Draw(spriteBatch, font, name, textPos, nameColor ?? Color.White, nameScale, depth, pixelCoordinates: true);
            }

            if (hasBar)
            {
                PetHealthBarRenderer.Draw(spriteBatch,barRect,healthFraction,depth);
            }
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, float depth)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
        }
    }
}
