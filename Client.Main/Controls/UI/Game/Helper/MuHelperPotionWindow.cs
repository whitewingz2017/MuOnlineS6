#nullable enable
using System;
using System.Collections.Generic;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Helper
{
    /// <summary>
    /// Separate Auto Recovery / potion settings window (MuMain-style sibling of Helper).
    /// </summary>
    internal sealed class MuHelperPotionWindow : UIControl
    {
        private const int WindowWidth = 190;
        private const int WindowHeight = 320;

        private readonly GameScene _scene;
        private readonly MuHelperController _controller;

        private readonly LabelControl _titleLabel;
        private readonly ButtonControl _closeButton;
        private readonly ButtonControl _saveButton;
        private readonly ButtonControl _resetButton;
        private readonly List<SegmentButton> _hpSegments = new();
        private readonly List<SegmentButton> _mpSegments = new();
        private readonly LabelControl _hpLabel;
        private readonly LabelControl _mpLabel;

        public MuHelperPotionWindow(GameScene scene, MuHelperController controller)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;

            _titleLabel = new LabelControl
            {
                Text = "Auto Recovery",
                X = 8,
                Y = 10,
                ControlSize = new Point(WindowWidth - 40, 20),
                ViewSize = new Point(WindowWidth - 40, 20),
                FontSize = 10,
                TextColor = ModernHudTheme.TextGold
            };
            Controls.Add(_titleLabel);

            _closeButton = CreateButton("X", WindowWidth - 28, 8, 20, 20, Close);
            Controls.Add(_closeButton);

            _hpLabel = new LabelControl
            {
                Text = "Auto Potion  (HP)",
                X = 14,
                Y = 44,
                ControlSize = new Point(160, 16),
                ViewSize = new Point(160, 16),
                FontSize = 8,
                TextColor = ModernHudTheme.TextWhite
            };
            Controls.Add(_hpLabel);

            BuildSegmentRow(_hpSegments, y: 64, isHp: true);

            _mpLabel = new LabelControl
            {
                Text = "Auto Potion  (MP)",
                X = 14,
                Y = 120,
                ControlSize = new Point(160, 16),
                ViewSize = new Point(160, 16),
                FontSize = 8,
                TextColor = ModernHudTheme.TextWhite
            };
            Controls.Add(_mpLabel);

            BuildSegmentRow(_mpSegments, y: 140, isHp: false);

            _resetButton = CreateButton("Initialization", 12, WindowHeight - 36, 80, 24, ResetPotion);
            Controls.Add(_resetButton);

            _saveButton = CreateButton("Save setup", 100, WindowHeight - 36, 80, 24, SavePotion);
            Controls.Add(_saveButton);

            _controller.StateChanged += OnStateChanged;
            RefreshFromConfig();
        }

        public void OpenBeside(UIControl helperWindow)
        {
            Visible = true;
            BringToFront();

            if (helperWindow != null)
            {
                X = Math.Max(4, helperWindow.X - ViewSize.X - 8);
                Y = helperWindow.Y;
            }
            else
            {
                Point v = UiScaler.VirtualSize;
                X = Math.Max(4, (v.X - ViewSize.X) / 2 - 100);
                Y = Math.Max(10, (v.Y - ViewSize.Y) / 2);
            }

            RefreshFromConfig();
            Scene.FocusControl = this;
        }

        public void Close()
        {
            Visible = false;
            if (Scene?.FocusControl == this)
                Scene.FocusControl = null;
        }

        public override bool OnClick()
        {
            base.OnClick();
            return true;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Update(gameTime);

            var mouse = MuGame.Instance.UiMouseState;
            if (IsMouseOver && (mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed))
                Scene?.SetMouseInputConsumed();
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready)
                return;

            var sprite = GraphicsManager.Instance.Sprite;
            var pixel = GraphicsManager.Instance.Pixel;
            if (sprite == null || pixel == null)
                return;

            Rectangle rect = DisplayRectangle;
            sprite.Draw(pixel, rect, new Color(11, 13, 18, 246) * Alpha);
            DrawBorder(sprite, pixel, rect, new Color(104, 82, 45, 235));

            // Content panels
            DrawPanel(sprite, pixel, new Rectangle(rect.X + 10, rect.Y + 40, rect.Width - 20, 70));
            DrawPanel(sprite, pixel, new Rectangle(rect.X + 10, rect.Y + 116, rect.Width - 20, 70));
            DrawPanel(sprite, pixel, new Rectangle(rect.X + 10, rect.Y + WindowHeight - 48, rect.Width - 20, 36));

            base.Draw(gameTime);
        }

        public override void Dispose()
        {
            _controller.StateChanged -= OnStateChanged;
            base.Dispose();
        }

        private void BuildSegmentRow(List<SegmentButton> list, int y, bool isHp)
        {
            const int count = 10;
            const int w = 14;
            const int h = 13;
            const int gap = 2;
            int startX = 18;

            for (int i = 0; i < count; i++)
            {
                int index = i;
                int threshold = (index + 1) * 10; // 10..100
                var btn = new SegmentButton(() =>
                {
                    int current = isHp
                        ? _controller.Config.PotionThreshold
                        : _controller.Config.ManaPotionThreshold;
                    return current >= threshold;
                })
                {
                    X = startX + i * (w + gap),
                    Y = y,
                    ControlSize = new Point(w, h),
                    ViewSize = new Point(w, h),
                    BackgroundColor = Color.Transparent,
                    BorderColor = Color.Transparent
                };

                btn.Click += (_, _) =>
                {
                    if (isHp)
                    {
                        _controller.Config.PotionThreshold = threshold;
                        _controller.Config.UseHealPotion = threshold > 0;
                    }
                    else
                    {
                        _controller.Config.ManaPotionThreshold = threshold;
                    }
                    RefreshFromConfig();
                };

                list.Add(btn);
                Controls.Add(btn);
            }
        }

        private void RefreshFromConfig()
        {
            // Segment draw reads config via lambdas; force redraw by invalidating nothing extra.
        }

        private void SavePotion()
        {
            _controller.Config.Normalize();
            _controller.Save();
            RefreshFromConfig();
        }

        private void ResetPotion()
        {
            _controller.Config.PotionThreshold = 0;
            _controller.Config.ManaPotionThreshold = 0;
            _controller.Config.HealThreshold = 0;
            _controller.Config.UseHealPotion = false;
            _controller.Config.Normalize();
            _controller.Save();
            RefreshFromConfig();
        }

        private void OnStateChanged() => RefreshFromConfig();

        private static ButtonControl CreateButton(string text, int x, int y, int w, int h, Action onClick)
        {
            var button = new ButtonControl
            {
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(w, h),
                ViewSize = new Point(w, h),
                FontSize = 7.5f,
                BackgroundColor = new Color(24, 28, 36, 245),
                HoverBackgroundColor = new Color(40, 46, 58, 245),
                PressedBackgroundColor = new Color(16, 18, 24, 245),
                BorderColor = new Color(104, 82, 45, 200),
                BorderThickness = 1,
                TextColor = ModernHudTheme.TextWhite
            };
            button.Click += (_, _) => onClick();
            return button;
        }

        private static void DrawBorder(SpriteBatch sprite, Texture2D pixel, Rectangle rect, Color color)
        {
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            sprite.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
        }

        private static void DrawPanel(SpriteBatch sprite, Texture2D pixel, Rectangle rect)
        {
            sprite.Draw(pixel, rect, new Color(16, 20, 28, 230));
            DrawBorder(sprite, pixel, rect, new Color(80, 64, 40, 180));
        }

        private sealed class SegmentButton : ButtonControl
        {
            private readonly Func<bool> _isFilled;

            public SegmentButton(Func<bool> isFilled)
            {
                _isFilled = isFilled;
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready)
                    return;

                var pixel = GraphicsManager.Instance.Pixel;
                if (pixel == null)
                    return;

                Color color = _isFilled() ? new Color(250, 235, 95) : new Color(75, 69, 57);
                GraphicsManager.Instance.Sprite.Draw(pixel, DisplayRectangle, color * Alpha);
            }
        }
    }
}