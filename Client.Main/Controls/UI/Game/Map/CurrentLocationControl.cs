#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Map
{
    /// <summary>
    /// Classic MU Main map/position bar.
    ///
    /// MuMain renders this control as three adjacent pieces:
    ///   Minimap_positionA.tga       22 x 25
    ///   Minimap_positionB.tga       WidenX x 25
    ///   Minimap_positionC.tga       73 x 20
    ///
    /// The middle texture is rendered with the same cropped source region as
    /// NewUIHeroPositionInfo::Render so its transparent/unused texture area is
    /// never exposed as a black gap.
    /// </summary>
    public sealed class CurrentLocationControl : UIControl
    {
        private const string FrameLeftPath = "Interface/Minimap_positionA.OZT";
        private const string FrameMiddlePath = "Interface/Minimap_positionB.OZT";
        private const string FrameRightPath = "Interface/MacroUI/Minimap_positionC.OZT";
        private const string SetupButtonPath = "Interface/MacroUI/MacroUI_Setup.OZT";
        private const string StartButtonPath = "Interface/MacroUI/MacroUI_Start.OZT";
        private const string StopButtonPath = "Interface/MacroUI/MacroUI_Stop.OZT";

        private readonly CharacterState _characterState;

        private Point _lastVirtualSize = Point.Zero;
        private SpriteFont? _font;
        private string _mapName = string.Empty;

        private Texture2D? _frameLeft;
        private Texture2D? _frameMiddle;
        private Texture2D? _frameRight;
        private Texture2D? _btnSetup;
        private Texture2D? _btnStart;
        private Texture2D? _btnStop;

        private bool _texturesLoaded;
        private bool _texturesLoading;
        private bool _helperActive;
        private bool _previousMouseDown;

        private Rectangle _hitSetup;
        private Rectangle _hitStartStop;

        // All classic bar dimensions are derived from this one scale value.
        private float _uiScale = 1.0f;
        private float _mapScale;
        private int _leftW;
        private int _middleW;
        private int _rightW;
        private int _frameH;
        private int _rightH;

        public CurrentLocationControl(CharacterState characterState)
        {
            _characterState = characterState;

            AutoViewSize = false;
            Interactive = true;
            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;
            BorderThickness = 0;

            RefreshLayout();
            RefreshData();
            _ = LoadTexturesAsync();
        }

        public Point GetBuffAnchor(int gap)
        {
            Rectangle rect = DisplayRectangle;
            return new Point(rect.Right + gap, rect.Y);
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            _lastVirtualSize = Point.Zero;
        }

        protected override void OnThemeChanged(UiThemeChangedEventArgs e)
        {
            base.OnThemeChanged(e);
            _lastVirtualSize = Point.Zero;
            _texturesLoaded = false;
            RefreshLayout();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            RefreshLayout();
            RefreshData();
            UpdateHitAreas();

            if (!_texturesLoaded && !_texturesLoading)
                _ = LoadTexturesAsync();

            _helperActive = (Scene as GameScene)?.IsMuHelperActive ?? _helperActive;
            HandleButtonClicks();
        }

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible)
                return;

            SpriteBatch? spriteBatch = GraphicsManager.Instance.Sprite;
            if (spriteBatch == null)
                return;

            SpriteBatchScope? scope = null;
            if (!SpriteBatchScope.BatchIsBegun)
            {
                scope = new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    transform: UiScaler.SpriteTransform);
            }

            try
            {
                _font ??= GraphicsManager.Instance.Font;
                if (_font == null)
                    return;

                DrawMuMainStyleBar(spriteBatch);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private void RefreshLayout()
        {
            Point virtualSize = UiScaler.VirtualSize;
            if (virtualSize == _lastVirtualSize)
                return;

            _lastVirtualSize = virtualSize;

            // MuMain uses the 20% middle-width variant below 800px and the
            // 40% variant above 800px. The source middle width is 80px.
            // The previous 1.85 multiplier enlarged the 25px MuMain plate to
            // roughly 45px and made the map label oversized. MuMain's artwork
            // is already authored at its intended pixel size; UiScaler handles
            // the physical-window scaling outside this control.
            _uiScale = Math.Clamp(virtualSize.Y / 720f, 0.85f, 1.25f);
            _mapScale = 0.55f * _uiScale;

            _leftW = Scaled(22);
            _middleW = Scaled(virtualSize.X > 800 ? 112 : 96);
            _rightW = Scaled(73);
            _frameH = Scaled(25);
            _rightH = Scaled(20);

            int totalWidth = _leftW + _middleW + _rightW;
            X = Scaled(12);
            Y = Scaled(8);
            ControlSize = new Point(totalWidth, _frameH);
            ViewSize = ControlSize;
        }

        private int Scaled(float value)
        {
            return Math.Max(1, (int)MathF.Round(value * _uiScale));
        }

        private void RefreshData()
        {
            _mapName = MapDatabase.GetMapName(_characterState.MapId);
        }

        private async Task LoadTexturesAsync()
        {
            if (_texturesLoading)
                return;

            _texturesLoading = true;
            _texturesLoaded = false;

            try
            {
                _frameLeft = await UiThemeManager.LoadThemeTextureAsync(FrameLeftPath);
                _frameMiddle = await UiThemeManager.LoadThemeTextureAsync(FrameMiddlePath);
                _frameRight = await UiThemeManager.LoadThemeTextureAsync(FrameRightPath);
                _btnSetup = await UiThemeManager.LoadThemeTextureAsync(SetupButtonPath);
                _btnStart = await UiThemeManager.LoadThemeTextureAsync(StartButtonPath);
                _btnStop = await UiThemeManager.LoadThemeTextureAsync(StopButtonPath);
            }
            catch
            {
                // The pixel/background fallback in DrawMuMainStyleBar keeps the
                // control usable while an optional texture is unavailable.
            }
            finally
            {
                _texturesLoaded = true;
                _texturesLoading = false;
            }
        }

        private void DrawMuMainStyleBar(SpriteBatch spriteBatch)
        {
            Texture2D? pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || _font == null)
                return;

            Rectangle bar = DisplayRectangle;
            int leftX = bar.X;
            int middleX = leftX + _leftW;
            int rightX = middleX + _middleW;

            Rectangle leftRect = new(leftX, bar.Y, _leftW, _frameH);
            Rectangle middleRect = new(middleX, bar.Y, _middleW, _frameH);
            Rectangle rightRect = new(rightX, bar.Y, _rightW, _rightH);

            // Draw a complete middle plate first. This guarantees an opaque,
            // dark connection between the two ornate pieces even if the B
            // texture contains transparent pixels.
            spriteBatch.Draw(pixel, middleRect, new Color(20, 14, 11, 245) * Alpha);

            if (_frameLeft != null)
            {
                spriteBatch.Draw(_frameLeft, leftRect, Color.White * Alpha);
            }

            if (_frameMiddle != null)
            {
                // MuMain's RenderImage call uses:
                //   u = 0.1f, v = 0f, width = 22.4f / 32f,
                //   height = 25f / 32f.
                Rectangle source = GetMuMainMiddleSource(_frameMiddle);
                spriteBatch.Draw(_frameMiddle, middleRect, source, Color.White * Alpha);
            }

            if (_frameRight != null)
            {
                spriteBatch.Draw(_frameRight, rightRect, Color.White * Alpha);
            }

            // Keep a thin middle border after the texture so the seams remain
            // closed and readable even when an asset has transparent edges.
            int border = Math.Max(1, Scaled(1));
            Color borderColor = new Color(151, 119, 74, 180) * Alpha;
            spriteBatch.Draw(pixel, new Rectangle(middleRect.X, middleRect.Y, middleRect.Width, border), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(middleRect.X, middleRect.Bottom - border, middleRect.Width, border), borderColor);

            // MuMain button placement is relative to the complete bar:
            // setup at x + WidenX + 41, start/stop at x + WidenX + 59.
            int buttonW = Scaled(18);
            int buttonH = Scaled(13);
            _hitSetup = new Rectangle(rightX + Scaled(19), bar.Y, buttonW, buttonH);
            _hitStartStop = new Rectangle(rightX + Scaled(37), bar.Y, buttonW, buttonH);

            if (_btnSetup != null)
            {
                spriteBatch.Draw(_btnSetup, _hitSetup, Color.White * Alpha);
            }

            Texture2D? activeButton = _helperActive ? _btnStop : _btnStart;
            if (activeButton != null)
            {
                spriteBatch.Draw(activeButton, _hitStartStop, Color.White * Alpha);
            }

            string text = $"{_mapName} ({_characterState.PositionX} , {_characterState.PositionY})";
            Vector2 textSize = _font.MeasureString(text) * _mapScale;
            float textX = middleRect.Center.X - textSize.X / 2f;
            float textY = middleRect.Center.Y - textSize.Y / 2f;

            spriteBatch.DrawString(
                _font,
                text,
                new Vector2(textX + Scaled(1), textY + Scaled(1)),
                Color.Black * 0.8f * Alpha,
                0f,
                Vector2.Zero,
                _mapScale,
                SpriteEffects.None,
                0f);
            spriteBatch.DrawString(
                _font,
                text,
                new Vector2(textX, textY),
                Color.White * Alpha,
                0f,
                Vector2.Zero,
                _mapScale,
                SpriteEffects.None,
                0f);
        }

        private static Rectangle GetMuMainMiddleSource(Texture2D texture)
        {
            int sourceX = Math.Clamp((int)MathF.Round(texture.Width * 0.1f), 0, texture.Width - 1);
            int sourceY = 0;
            int sourceWidth = Math.Max(1, (int)MathF.Round(texture.Width * (22.4f / 32f)));
            int sourceHeight = Math.Max(1, (int)MathF.Round(texture.Height * (25f / 32f)));

            sourceWidth = Math.Min(sourceWidth, texture.Width - sourceX);
            sourceHeight = Math.Min(sourceHeight, texture.Height - sourceY);
            return new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight);
        }

        private void UpdateHitAreas()
        {
            Rectangle bar = DisplayRectangle;
            int rightX = bar.X + _leftW + _middleW;
            _hitSetup = new Rectangle(rightX + Scaled(19), bar.Y, Scaled(18), Scaled(13));
            _hitStartStop = new Rectangle(rightX + Scaled(37), bar.Y, Scaled(18), Scaled(13));
        }

        private void HandleButtonClicks()
        {
            MouseState mouse = Mouse.GetState();
            bool mouseDown = mouse.LeftButton == ButtonState.Pressed;
            bool pressedThisFrame = mouseDown && !_previousMouseDown;
            _previousMouseDown = mouseDown;

            if (!pressedThisFrame)
                return;

            Point virtualMousePosition = UiScaler.ToVirtual(mouse.Position);
            GameScene? gameScene = Scene as GameScene;

            if (_hitSetup.Contains(virtualMousePosition))
            {
                gameScene?.MuHelperWindow?.ToggleVisibility();
            }
            else if (_hitStartStop.Contains(virtualMousePosition))
            {
                gameScene?.MuHelperController?.Toggle();
                _helperActive = gameScene?.IsMuHelperActive ?? !_helperActive;
            }
        }
    }
}
