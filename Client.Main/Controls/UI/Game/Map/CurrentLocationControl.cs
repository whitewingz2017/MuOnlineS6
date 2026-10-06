#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Configuration;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Map
{
    public sealed class CurrentLocationControl : UIControl
    {
        private readonly CharacterState _characterState;
        private readonly GameScene _scene;

        private Point _lastVirtualSize = Point.Zero;
        private SpriteFont? _font;
        private string _mapName = string.Empty;

        private float _mapScale = 0.55f;
        private float _coordsScale = 0.48f;

        private bool IsSeason6 => UiThemeManager.CurrentId == UiThemeId.Season6;
        private bool IsClassic => UiThemeManager.CurrentId == UiThemeId.Classic;

        // ============================================================
        // FRAME TEXTURES
        // ============================================================

        private Texture2D? _frameLeft;
        private Texture2D? _frameMiddle;
        private Texture2D? _frameRight;

        // ============================================================
        // MACRO UI BUTTON TEXTURES
        // ============================================================

        private Texture2D? _btnSetup;
        private Texture2D? _btnStart;
        private Texture2D? _btnStop;

        private bool _texturesLoaded;
        private bool _texturesLoading;

        // ============================================================
        // BUTTON HIT AREAS
        // ============================================================

        private Rectangle _hitSetup;
        private Rectangle _hitStartStop;

        // ============================================================
        // SCALED SIZES
        // ============================================================

        private int _leftW;
        private int _midW;
        private int _rightW;
        private int _frameH;
        private int _rightH;

        private float _uiScale = 2.85f;
        private float _frameScale = 1.15f;
        private float _middleWidthScale = 1.5f;

        // ============================================================
        // RIGHT FRAME POSITION / SCALE
        // ============================================================

        private float _frameRightScaleX = 1.0f;
        private float _frameRightScaleY = 1.0f;

        private int _frameRightOffsetX = 0;
        private int _frameRightOffsetY = 0;

        // ============================================================
        // BUTTON POSITION OFFSETS
        // ============================================================

        private int _btnSetupOffsetX = -4;
        private float _btnSetupOffsetY = 0.1f;

        private int _btnStartOffsetX = -9;
        private float _btnStartOffsetY = 0.3f;

        private int _btnStopOffsetX = -6;
        private float _btnStopOffsetY = 2f;

        // ============================================================
        // BUTTON SIZE
        // ============================================================

        // This controls the actual displayed button size.
        // Smaller values = smaller MacroUI buttons.
        private float _buttonWidth = 15f;
        private float _buttonHeight = 11.5f;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public CurrentLocationControl(
            GameScene scene,
            CharacterState characterState)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _characterState = characterState ?? throw new ArgumentNullException(nameof(characterState));

            AutoViewSize = false;
            Interactive = true;

            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;
            BorderThickness = 0;

            RefreshLayout();
            RefreshData();

            _ = LoadTexturesAsync();
        }

        // ============================================================
        // BUFF ANCHOR
        // ============================================================

        private const int OriginalBaseX = 12;
        private const int OriginalBaseY = 10;
        private const int OriginalPlateWidth = 220;
        private const int OriginalBaseHeight = 28;
        private const int OriginalPadX = 10;
        private const int OriginalButtonGap = 4;
        private const int OriginalButtonWidth = 52;
        private const int OriginalButtonHeight = 22;

        private bool UseOriginalLocationBar =>
            UiThemeManager.CurrentId == UiThemeId.Classic ||
            (MuGame.AppSettings?.HudTheme ?? HudTheme.Hybrid) != HudTheme.ClassicPc;

        public Point GetBuffAnchor(int gap)

        {
            var rect = DisplayRectangle;

            return new Point(
                rect.Right + gap,
                rect.Y);
        }

        // ============================================================
        // SCREEN SIZE
        // ============================================================

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();

            _lastVirtualSize = Point.Zero;
        }

        // ============================================================
        // THEME CHANGE
        // ============================================================

        protected override void OnThemeChanged(UiThemeChangedEventArgs e)
        {
            base.OnThemeChanged(e);

            _lastVirtualSize = Point.Zero;

            RefreshLayout();
        }

        // ============================================================
        // UPDATE
        // ============================================================

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            RefreshLayout();
            RefreshData();

            if (UseOriginalLocationBar)
            {
                HandleButtonClicks();
                return;
            }

            if (!_texturesLoaded && !_texturesLoading)
            {
                _ = LoadTexturesAsync();
            }

            HandleButtonClicks();
        }

        public void InvalidateLayout()
        {
            _lastVirtualSize = Point.Zero;
            RefreshLayout();
        }

        // ============================================================
        // DRAW
        // ============================================================

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible)
                return;

            var spriteBatch = GraphicsManager.Instance.Sprite;

            if (spriteBatch == null)
                return;

            SpriteBatchScope? scope = null;

            if (!SpriteBatchScope.BatchIsBegun)
            {
                scope = new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    UseOriginalLocationBar ? SamplerState.LinearClamp : SamplerState.PointClamp,
                    transform: UiScaler.SpriteTransform);
            }

            try
            {
                _font ??= GraphicsManager.Instance.Font;

                if (_font == null)
                    return;

                if (UseOriginalLocationBar)
                    DrawOriginalBar(spriteBatch);
                else
                    DrawMuMainStyleBar(spriteBatch);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        // ============================================================
        // LAYOUT
        // ============================================================

        private void RefreshLayout()
        {
            Point virtualSize = UiScaler.VirtualSize;

            if (virtualSize == _lastVirtualSize)
                return;

            _lastVirtualSize = virtualSize;

            if (UseOriginalLocationBar)
            {
                ApplyOriginalLayout(virtualSize);
                return;
            }

            // ========================================================
            // OVERALL UI SCALE
            // ========================================================

            _uiScale = Math.Clamp(
                virtualSize.Y / 720f * 1.85f,
                1.5f,
                2.3f);

            // ========================================================
            // TEXT SCALE
            // ========================================================

            _mapScale = 0.2f * _uiScale;

            // ========================================================
            // FRAME SIZES
            // ========================================================

            _leftW = (int)(22 * _uiScale * _frameScale);
            _rightW = (int)(93 * _uiScale * _frameScale);
            _frameH = (int)(25 * _uiScale * _frameScale);
            _rightH = (int)(25 * _uiScale * _frameScale);

            // ========================================================
            // MIDDLE WIDTH
            // ========================================================

            int midBase = (int)(55 * _uiScale * _frameScale);
            _midW = (int)(midBase * _middleWidthScale * _frameScale);

            // ========================================================
            // TOTAL CONTROL SIZE
            // ========================================================

            int totalWidth =
                _leftW +
                _midW +
                _rightW;

            X = (int)(12 * _uiScale);

            Y = (int)(8 * _uiScale);

            ControlSize = new Point(
                totalWidth,
                _frameH);

            ViewSize = ControlSize;
            Interactive = true;
        }

        // ============================================================
        // DATA
        // ============================================================

        private void RefreshData()
        {
            _mapName = MapDatabase.GetMapName(
                _characterState.MapId);
        }

        private void ApplyOriginalLayout(Point virtualSize)
        {
            Interactive = true;

            float scaleX = virtualSize.X / 1024f;
            float scaleY = virtualSize.Y / 768f;
            float scale = Math.Clamp(MathF.Min(scaleX, scaleY), 0.82f, 1.35f);

            X = ScaleOriginal(OriginalBaseX, scale);
            Y = ScaleOriginal(OriginalBaseY, scale);

            int plateWidth = ScaleOriginal(OriginalPlateWidth, scale);
            int height = ScaleOriginal(OriginalBaseHeight, scale);
            int gap = ScaleOriginal(OriginalButtonGap, scale);
            int buttonWidth = ScaleOriginal(OriginalButtonWidth, scale);
            int buttonHeight = ScaleOriginal(OriginalButtonHeight, scale);

            ControlSize = new Point(plateWidth + gap + (buttonWidth * 2) + gap, height);
            ViewSize = ControlSize;

            _mapScale = Math.Clamp(0.54f * scale, 0.46f, 0.72f);
            _coordsScale = Math.Clamp(0.47f * scale, 0.40f, 0.62f);
        }

        private void DrawOriginalBar(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || _font == null)
                return;

            Rectangle rect = DisplayRectangle;
            float scale = Math.Max(0.82f, rect.Height / (float)OriginalBaseHeight);
            int plateWidth = ScaleOriginal(OriginalPlateWidth, scale);
            var plate = new Rectangle(rect.X, rect.Y, Math.Min(plateWidth, rect.Width), rect.Height);

            spriteBatch.Draw(pixel, plate, ModernHudTheme.BorderOuter);

            var inner = new Rectangle(plate.X + 1, plate.Y + 1,
                Math.Max(1, plate.Width - 2), Math.Max(1, plate.Height - 2));
            UiDrawHelper.DrawVerticalGradient(spriteBatch, inner,
                ModernHudTheme.BgDark, ModernHudTheme.BgDarkest);

            spriteBatch.Draw(pixel,
                new Rectangle(inner.X + 1, inner.Y, Math.Max(1, inner.Width - 2), 1),
                ModernHudTheme.Accent * 0.6f * Alpha);

            spriteBatch.Draw(pixel,
                new Rectangle(inner.X, inner.Y + 1, inner.Width, 1),
                ModernHudTheme.BorderInner * 0.3f * Alpha);

            float wScale = scale;
            int padX = ScaleOriginal(OriginalPadX, wScale);

            string coords = $"X:{_characterState.PositionX}  Y:{_characterState.PositionY}";
            Vector2 coordsSize = _font.MeasureString(coords) * _coordsScale;
            float coordsX = plate.Right - padX - coordsSize.X;
            float coordsY = plate.Y + (plate.Height - coordsSize.Y) / 2f;

            spriteBatch.DrawString(_font, coords, new Vector2(coordsX + 1, coordsY + 1),
                Color.Black * 0.6f * Alpha, 0f, Vector2.Zero, _coordsScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, coords, new Vector2(coordsX, coordsY),
                ModernHudTheme.TextGray * Alpha, 0f, Vector2.Zero, _coordsScale, SpriteEffects.None, 0f);

            int separatorGap = ScaleOriginal(6, wScale);
            int mapMaxWidth = Math.Max(1, (int)(coordsX - plate.X - padX - separatorGap));
            string clippedMap = ClipTextWithEllipsis(_font, _mapName, _mapScale, mapMaxWidth);

            if (!string.IsNullOrEmpty(clippedMap))
            {
                Vector2 mapSize = _font.MeasureString(clippedMap) * _mapScale;
                float mapX = plate.X + padX;
                float mapY = plate.Y + (plate.Height - mapSize.Y) / 2f;

                spriteBatch.DrawString(_font, clippedMap, new Vector2(mapX + 1, mapY + 1),
                    Color.Black * 0.6f * Alpha, 0f, Vector2.Zero, _mapScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, clippedMap, new Vector2(mapX, mapY),
                    ModernHudTheme.TextGold * Alpha, 0f, Vector2.Zero, _mapScale, SpriteEffects.None, 0f);
            }

            UiDrawHelper.DrawCornerAccents(spriteBatch, plate,
                ModernHudTheme.Accent * 0.3f * Alpha, size: 6, thickness: 1);

            int gap = ScaleOriginal(OriginalButtonGap, wScale);
            int buttonWidth = ScaleOriginal(OriginalButtonWidth, wScale);
            int buttonHeight = ScaleOriginal(OriginalButtonHeight, wScale);
            int buttonY = plate.Y + Math.Max(0, (plate.Height - buttonHeight) / 2);
            _hitSetup = new Rectangle(plate.Right + gap, buttonY, buttonWidth, buttonHeight);
            _hitStartStop = new Rectangle(_hitSetup.Right + gap, buttonY, buttonWidth, buttonHeight);

            DrawThemedHelperButton(spriteBatch, _hitSetup, "Setup", false);
            DrawThemedHelperButton(spriteBatch, _hitStartStop,
                _scene.IsMuHelperActive ? "Stop" : "Start",
                _scene.IsMuHelperActive);
        }

        private void DrawThemedHelperButton(SpriteBatch spriteBatch, Rectangle destination, string text, bool active)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || _font == null || destination.Width <= 0 || destination.Height <= 0)
                return;

            Point mouse = MuGame.Instance.UiMouseState.Position;
            bool hovered = destination.Contains(mouse);
            bool pressed = hovered && MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
            bool classic = UiThemeManager.CurrentId == UiThemeId.Classic;

            Color fill = active
                ? (classic ? new Color(92, 34, 42, 245) : new Color(86, 37, 39, 245))
                : hovered
                    ? ModernHudTheme.SlotHover
                    : ModernHudTheme.SlotBg;
            if (pressed)
                fill = Color.Lerp(fill, Color.Black, 0.25f);

            spriteBatch.Draw(pixel, destination, ModernHudTheme.BorderOuter * Alpha);
            var inner = new Rectangle(destination.X + 1, destination.Y + 1,
                Math.Max(1, destination.Width - 2), Math.Max(1, destination.Height - 2));
            spriteBatch.Draw(pixel, inner, fill * Alpha);
            spriteBatch.Draw(pixel, new Rectangle(inner.X, inner.Y, inner.Width, 1),
                (active ? ModernHudTheme.Danger : ModernHudTheme.Accent) * (classic ? 0.85f : 0.55f) * Alpha);

            float scale = Math.Clamp(destination.Height / 28f, 0.38f, 0.62f);
            Vector2 size = _font.MeasureString(text) * scale;
            var position = new Vector2(
                destination.X + (destination.Width - size.X) / 2f,
                destination.Y + (destination.Height - size.Y) / 2f);
            spriteBatch.DrawString(_font, text, position + Vector2.One, Color.Black * 0.65f * Alpha,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, text, position,
                (active ? Color.White : ModernHudTheme.TextGold) * Alpha,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private static int ScaleOriginal(int value, float scale)
        {
            return Math.Max(1, (int)MathF.Round(value * scale));
        }

        private static string ClipTextWithEllipsis(SpriteFont font, string text, float scale, int maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (font.MeasureString(text).X * scale <= maxWidth)
                return text;

            const string ellipsis = "...";
            if (font.MeasureString(ellipsis).X * scale > maxWidth)
                return string.Empty;

            int left = 0;
            int right = text.Length;
            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                string probe = text[..mid] + ellipsis;
                if (font.MeasureString(probe).X * scale <= maxWidth)
                    left = mid;
                else
                    right = mid - 1;
            }

            return left > 0 ? text[..left] + ellipsis : ellipsis;
        }

        // ============================================================
        // LOAD TEXTURES
        // ============================================================

        private async Task LoadTexturesAsync()
        {
            if (_texturesLoaded || _texturesLoading)
                return;

            _texturesLoading = true;

            try
            {
                // ====================================================
                // MAIN BAR
                // ====================================================

                _frameLeft =
                    await UiThemeManager.LoadThemeTextureAsync(
                        "Interface/Minimap_positionA.OZT");

                _frameMiddle =
                    await UiThemeManager.LoadThemeTextureAsync(
                        "Interface/Minimap_positionB.OZT");

                _frameRight =
                    await UiThemeManager.LoadThemeTextureAsync(
                        "Interface/MacroUI/Minimap_positionC.OZT");

                // ====================================================
                // MACRO BUTTONS
                // ====================================================

                _btnSetup =
                    await UiThemeManager.LoadThemeTextureAsync(
                        "Interface/MacroUI/MacroUI_Setup.OZT");

                _btnStart =
                    await UiThemeManager.LoadThemeTextureAsync(
                        "Interface/MacroUI/MacroUI_Start.OZT");

                _btnStop =
                    await UiThemeManager.LoadThemeTextureAsync(
                        "Interface/MacroUI/MacroUI_Stop.OZT");
            }
            catch
            {
                // Missing textures → fallback rendering will be used.
            }
            finally
            {
                _texturesLoaded = true;
                _texturesLoading = false;
            }
        }

        // ============================================================
        // MAIN BAR
        // ============================================================

        private void DrawMuMainStyleBar(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;

            if (pixel == null || _font == null)
                return;

            Rectangle rect = DisplayRectangle;

            // ========================================================
            // BAR POSITIONS
            // ========================================================

            int leftX = rect.X;

            int midX =
                leftX +
                _leftW -
                (int)(10 * _uiScale);

            int rightX =
                midX +
                _midW -
                (int)(22 * _uiScale);

            // ========================================================
            // LEFT FRAME
            // ========================================================

            if (_frameLeft != null)
            {
                spriteBatch.Draw(
                    _frameLeft,
                    new Rectangle(
                        leftX,
                        rect.Y,
                        _leftW,
                        _frameH),
                    Color.White * Alpha);
            }

            // ========================================================
            // MIDDLE FRAME
            // ========================================================

            if (_frameMiddle != null)
            {
                spriteBatch.Draw(
                    _frameMiddle,
                    new Rectangle(
                        midX,
                        rect.Y,
                        _midW,
                        _frameH),
                    Color.White * Alpha);
            }

            // ========================================================
            // RIGHT FRAME
            // ========================================================

            if (_frameRight != null)
            {
                int frameRightW =
                    (int)(_rightW * _frameRightScaleX);

                int frameRightH =
                    (int)(_rightH * _frameRightScaleY);

                int frameRightX =
                    rightX +
                    (int)(_frameRightOffsetX * _uiScale);

                int frameRightY =
                    rect.Y +
                    (int)(_frameRightOffsetY * _uiScale);

                spriteBatch.Draw(
                    _frameRight,
                    new Rectangle(
                        frameRightX,
                        frameRightY,
                        frameRightW,
                        frameRightH),
                    Color.White * Alpha);
            }

            // ========================================================
            // MACRO BUTTONS
            // ========================================================

            DrawMacroButtons(
                spriteBatch,
                rect,
                rightX);

            // ========================================================
            // MAP TEXT
            // ========================================================

            string text =
                $"{_mapName} ({_characterState.PositionX}, {_characterState.PositionY})";

            Vector2 size =
                _font.MeasureString(text) *
                _mapScale;

            float textX =
                midX +
                (_midW - size.X) / 2.8f;

            float textY =
                rect.Y +
                (_frameH - size.Y) / 5.0f;

            spriteBatch.DrawString(
                _font,
                text,
                new Vector2(
                    textX,
                    textY),
                new Color(
                    255,
                    230,
                    180) * Alpha,
                0f,
                Vector2.Zero,
                _mapScale,
                SpriteEffects.None,
                0f);
        }

        // ============================================================
        // MACRO BUTTONS
        // ============================================================

        private void DrawMacroButtons(
            SpriteBatch spriteBatch,
            Rectangle rect,
            int rightX)
        {
            // ========================================================
            // BUTTON SIZE
            // ========================================================

            int btnW =
                (int)(_buttonWidth * _uiScale);

            int btnH =
                (int)(_buttonHeight * _uiScale);

            // ========================================================
            // SETUP BUTTON
            // ========================================================

            _hitSetup = new Rectangle(
                rightX +
                (int)((20 + _btnSetupOffsetX) * _uiScale),

                rect.Y +
                (int)((1 + _btnSetupOffsetY) * _uiScale),

                btnW,
                btnH);

            // ========================================================
            // START / STOP BUTTON
            // ========================================================

            _hitStartStop = new Rectangle(
                rightX +
                (int)((40 + _btnStartOffsetX) * _uiScale),

                rect.Y +
                (int)((1 + _btnStartOffsetY) * _uiScale),

                btnW,
                btnH);

            // ========================================================
            // SETUP
            // ========================================================

            DrawMacroButton(
                spriteBatch,
                _btnSetup,
                _hitSetup);

            // ========================================================
            // START / STOP
            // ========================================================

            if (_scene.IsMuHelperActive)
            {
                DrawMacroButton(
                    spriteBatch,
                    _btnStop,
                    _hitStartStop);
            }
            else
            {
                DrawMacroButton(
                    spriteBatch,
                    _btnStart,
                    _hitStartStop);
            }
        }

        // ============================================================
        // DRAW ONE MACRO BUTTON
        // ============================================================
        //
        // The MacroUI textures contain 3 vertical frames.
        //
        // Instead of drawing the whole texture, we only take the
        // FIRST frame:
        //
        // [ FRAME 1 ]
        // [ FRAME 2 ]
        // [ FRAME 3 ]
        //     ^
        //     |
        //     +---- draw only this
        //
        // The OZT loader pads textures to power-of-two dimensions, so use
        // the authored 18 x 13 frame size rather than texture.Width/Height.
        // ============================================================

        private void DrawMacroButton(
            SpriteBatch spriteBatch,
            Texture2D? texture,
            Rectangle destination)
        {
            const int frameWidth = 18;
            const int frameHeight = 13;
            const int frameCount = 3;

            if (texture == null ||
                texture.Width < frameWidth ||
                texture.Height < frameHeight * frameCount)
            {
                return;
            }

            // MacroUI textures use vertical states:
            // normal, hover, pressed. The source files are 18 x 40 and the
            // loader pads them to a power-of-two height, so use the authored
            // 13-pixel frame height and keep the unused rows out of the draw.
            Point mousePosition = MuGame.Instance.UiMouseState.Position;
            bool hovered = destination.Contains(mousePosition);
            bool pressed = hovered &&
                           MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;

            int frame = pressed ? 2 : hovered ? 1 : 0;
            int sourceY = frame * frameHeight;

            Rectangle source = new Rectangle(
                0,
                sourceY,
                frameWidth,
                frameHeight);

            spriteBatch.Draw(
                texture,
                destination,
                source,
                Color.White * Alpha);
        }

        // ============================================================
        // BUTTON CLICK HANDLING
        // ============================================================

        private void HandleButtonClicks()
        {
            var mouse =
                MuGame.Instance.UiMouseState;

            var previousMouse =
                MuGame.Instance.PrevUiMouseState;

            bool leftJustPressed =
                mouse.LeftButton == ButtonState.Pressed &&
                previousMouse.LeftButton == ButtonState.Released;

            if (!leftJustPressed)
                return;

            Point mousePos =
                mouse.Position;

            // ========================================================
            // SETUP
            // ========================================================

            if (_hitSetup.Contains(mousePos))
            {
                _scene.MuHelperWindow?.ToggleVisibility();

                _scene.SetMouseInputConsumed();

                return;
            }

            // ========================================================
            // START / STOP
            // ========================================================

            if (_hitStartStop.Contains(mousePos))
            {
                _scene.MuHelperController?.Toggle();

                _scene.SetMouseInputConsumed();
            }
        }
    }
}