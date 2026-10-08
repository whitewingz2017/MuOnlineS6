using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Configuration;
using Client.Main.Controls;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Hud;
using Client.Main.Controllers;
using Client.Main.Content;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Helper
{
    /// <summary>
    /// S6 Helper settings presented in the compact Hunting, Obtaining, and Other Settings tabs.
    /// Settings remain bound to the existing MuHelperConfig and controller.
    /// </summary>
    internal sealed partial class MuHelperWindow : UIControl
    {
        // MuMain CNewUIMuHelper uses a fixed 190x429 logical canvas.
        private const int ClassicPcWidth = 190;
        private const int ClassicPcHeight = 429;
        // OZTReader pads these source textures to power-of-two dimensions; draw only their
        // real pixels so transparent/right-side padding is never stretched into the UI.
        private const int ClassicHeaderTextureWidth = 190;
        private const int ClassicHeaderTextureHeight = 64;
        private const int ClassicSideTextureWidth = 21;
        private const int ClassicSideTextureHeight = 320;
        private const int ClassicFooterTextureWidth = 190;
        private const int ClassicFooterTextureHeight = 45;
        private const int ClassicPcRowHeight = 17;
        private const int ClassicPcContentTop = 70;
        private const int ClassicPcContentHeight = 298;
        private const int ClassicPcFooterTop = 388;

        // Hybrid remains its own larger, touch-oriented list presentation.
        private const int HybridWidth = 470;
        private const int HybridHeight = 540;
        private const int HybridRowHeight = 32;
        private const int HybridContentTop = 124;
        private const int HybridContentHeight = 340;
        private const int HybridFooterTop = 486;

        private int WindowWidth => IsClassicPc ? ClassicPcWidth : HybridWidth;
        private int WindowHeight => IsClassicPc ? ClassicPcHeight : HybridHeight;
        private int RowHeight => IsClassicPc ? ClassicPcRowHeight : HybridRowHeight;
        private int ContentTop => IsClassicPc ? ClassicPcContentTop : HybridContentTop;
        private int ContentHeight => IsClassicPc ? ClassicPcContentHeight : HybridContentHeight;
        private int FooterTop => IsClassicPc ? ClassicPcFooterTop : HybridFooterTop;

        private HelperPresentation _presentation;

        private HelperPresentation ResolvePresentation()
        {
            if (MuGame.AppSettings?.HudTheme == HudTheme.ClassicPc
                || UiThemeManager.CurrentId == UiThemeId.Classic)
            {
                return HelperPresentation.ClassicPc;
            }

            return HelperPresentation.Hybrid;
        }

        private bool IsClassicPc => _presentation == HelperPresentation.ClassicPc;
        private bool IsHybrid => _presentation == HelperPresentation.Hybrid;

        private enum HelperRowKind
        {
            Value,
            Toggle,
            Skill,
            Fixed
        }

        private enum HelperPresentation
        {
            ClassicPc,
            Hybrid
        }

        private readonly record struct ClassicDockControlState(
            int X, int Y, Point Offset, float Scale, float? FontSize, int? TextBoxPadding, int? TextBoxBorderThickness);

        private static readonly string[] FrameTexturePaths =
        {
            "Interface/newui_msgbox_back.OZJ",
            "Interface/newui_item_back01.OZT",
            "Interface/newui_item_back02-L.OZT",
            "Interface/newui_item_back02-R.OZT",
            "Interface/newui_item_back03.OZT"
        };

        private static readonly string[] PanelTexturePaths =
        {
            "Interface/newui_item_table01(L).OZT",
            "Interface/newui_item_table01(R).OZT",
            "Interface/newui_item_table02(L).OZT",
            "Interface/newui_item_table02(R).OZT",
            "Interface/newui_item_table03(Up).OZT",
            "Interface/newui_item_table03(Dw).OZT",
            "Interface/newui_item_table03(L).OZT",
            "Interface/newui_item_table03(R).OZT"
        };

        private const string CheckBoxTexturePath = "Interface/newui_option_check.OZT";
        private static readonly string[] HelperTexturePaths =
        {
            "Interface/MacroUI/MacroUI_RangeMinus.OZT",
            "Interface/MacroUI/MacroUI_InputNumber.OZT",
            "Interface/MacroUI/MacroUI_InputString.OZT",
            "Interface/MacroUI/MacroUI_OptionButton.OZT",
            "Interface/newui_skillbox.OZJ",
            "Interface/newui_skillbox2.OZJ",
            "Interface/InGameShop/ingame_Bt03.OZT",
            "Interface/newui_exit_00.OZT",
            "Interface/newui_chainfo_btn_level.tga"
        };

        private readonly GameScene _scene;
        private readonly MuHelperController _controller;
        private Action<SkillEntryState> _pendingSkillAssignment;
        private readonly ILogger _logger;
        private readonly List<(ButtonControl Button, Func<string> Text)> _boundButtons = new();
        private readonly List<(UIControl Page, LabelControl Label, ButtonControl Button, int X, int Row, HelperRowKind Kind)> _layoutRows = new();
        private readonly List<(LabelControl Label, Func<string> Text)> _boundLabels = new();
        private readonly List<(UIControl Page, ButtonControl Button, Action Clear)> _skillSlotButtons = new();
        private readonly UIControl[] _pages = new UIControl[3];
        private readonly ButtonControl[] _tabButtons = new ButtonControl[3];
        private readonly UIControl _potionSettingsPage;
        private readonly List<GameControl> _classicHuntingControls = new();
        private readonly List<HelperThresholdSegmentButton> _potionThresholdSegments = new();
        private readonly List<(ButtonControl Button, int TextureIndex, bool Flip)> _classicIconButtons = new();
        private HelperActionButton _potionSettingsBackButton;
        private readonly List<HelperToggleButton> _potionSettingsToggles = new();
        private HelperToggleButton _potionAutoHealToggle;
        private bool _showPotionSettings;
        private readonly LabelControl _titleLabel;
        private readonly LabelControl _inputHintLabel;
        private readonly ButtonControl _scrollUpButton;
        private readonly ButtonControl _scrollDownButton;
        private readonly ButtonControl _startButton;
        private readonly ButtonControl _saveButton;
        private readonly ButtonControl _resetButton;
        private readonly ButtonControl _closeButton;
        private readonly TextBoxControl _extraItemsBox;
        private readonly TextBoxControl _classicMaxSecondsAwayInput;
        private readonly TextBoxControl _classicActivation1DelayInput;
        private readonly TextBoxControl _classicActivation2DelayInput;
        private readonly ButtonControl _addExtraItemButton;
        private readonly ButtonControl _deleteExtraItemButton;
        private readonly LabelControl _extraItemsListLabel;
        private readonly List<ButtonControl> _extraItemRowButtons = new();
        private UIControl _extraItemsPage;
        private int _selectedExtraItemIndex = -1;
        private readonly Texture2D[] _frameTextures = new Texture2D[FrameTexturePaths.Length];
        private readonly Texture2D[] _panelTextures = new Texture2D[PanelTexturePaths.Length];
        private Texture2D _checkBoxTexture;
        private readonly Texture2D[] _helperTextures = new Texture2D[HelperTexturePaths.Length];
        private int _activeTab;
        private int _previousWheelValue;
        private int _extraItemsScrollIndex;
        private bool _texturesLoaded;
        private readonly Dictionary<GameControl, ClassicDockControlState> _classicDockControlStates = new();
        private bool _classicDockScaleReady;
        private float _appliedClassicDockScale = 1f;

        public MuHelperWindow(GameScene scene, MuHelperController controller, ILogger logger)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _logger = logger;
            _presentation = ResolvePresentation();

            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            ApplyInterfaceTheme();
            Recenter();

            _titleLabel = AddLabel(this, "Official MU Helper", 4, 11, WindowWidth - 8, 24, 9, ModernHudTheme.TextWhite, bold: true);
            _titleLabel.Visible = false;

            string[] tabNames = { "Hunting", "Obtaining", "Other Settings" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int tabIndex = i;
                int tabX = IsClassicPc ? 10 + i * 57 : 12 + i * ((WindowWidth - 24) / tabNames.Length);
                int tabY = IsClassicPc ? 48 : 55;
                int tabWidth = IsClassicPc ? 56 : (WindowWidth - 24) / tabNames.Length - 4;
                int tabHeight = IsClassicPc ? 22 : 34;
                _tabButtons[i] = CreateButton(tabNames[i], tabX, tabY, tabWidth, tabHeight, () => SetActiveTab(tabIndex));
                _tabButtons[i].FontSize = IsClassicPc ? (i == 2 ? 6.4f : 7.2f) : (i == 2 ? 9 : 11);
                Controls.Add(_tabButtons[i]);
            }

            _extraItemsBox = new TextBoxControl
            {
                X = IsClassicPc ? 17 : 0,
                Y = IsClassicPc ? 276 : 0,
                ControlSize = new Point(IsClassicPc ? 156 : WindowWidth - 88, IsClassicPc ? 22 : 30),
                ViewSize = new Point(IsClassicPc ? 156 : WindowWidth - 88, IsClassicPc ? 22 : 30),
                MaxLength = 200,
                FontSize = IsClassicPc ? 8 : 10,
                Padding = 4,
                PlaceholderText = "Item name",
                BackgroundColor = new Color(16, 20, 28, 245),
                BorderColor = ModernHudTheme.BorderInner,
                FocusedBorderColor = ModernHudTheme.AccentBright,
                TextColor = IsClassicPc ? Color.Black : ModernHudTheme.TextWhite
            };
            _extraItemsBox.Visible = false;

            _classicMaxSecondsAwayInput = CreateClassicNumericInput(
                "Max Seconds Away Input", 3, _controller.Config.MaxSecondsAway);
            _classicActivation1DelayInput = CreateClassicNumericInput(
                "Activation Skill 1 Delay Input", 4, _controller.Config.ActivationSkill1.DelaySeconds);
            _classicActivation2DelayInput = CreateClassicNumericInput(
                "Activation Skill 2 Delay Input", 4, _controller.Config.ActivationSkill2.DelaySeconds);
            ConfigureClassicNumericInput(_classicMaxSecondsAwayInput, 999,
                () => _controller.Config.MaxSecondsAway,
                value => _controller.Config.MaxSecondsAway = value);
            ConfigureClassicNumericInput(_classicActivation1DelayInput, 3600,
                () => _controller.Config.ActivationSkill1.DelaySeconds,
                value => _controller.Config.ActivationSkill1.DelaySeconds = value);
            ConfigureClassicNumericInput(_classicActivation2DelayInput, 3600,
                () => _controller.Config.ActivationSkill2.DelaySeconds,
                value => _controller.Config.ActivationSkill2.DelaySeconds = value);

            _addExtraItemButton = CreateButton("Add", 0, 0, 36, 22, AddExtraItem);
            _addExtraItemButton.Name = "Add Extra Item";
            _addExtraItemButton.FontSize = IsClassicPc ? 6.5f : 8;
            _deleteExtraItemButton = CreateButton("Delete", 0, 0, 48, 22, DeleteSelectedExtraItem);
            _deleteExtraItemButton.Name = "Delete Extra Item";
            _deleteExtraItemButton.FontSize = IsClassicPc ? 6.5f : 8;
            foreach (ButtonControl button in new[] { _addExtraItemButton, _deleteExtraItemButton })
            {
                button.BackgroundColor = Color.Transparent;
                button.HoverBackgroundColor = Color.Transparent;
                button.PressedBackgroundColor = Color.Transparent;
                button.BorderColor = Color.Transparent;
                button.BorderThickness = 0;
            }
            _extraItemsListLabel = new LabelControl
            {
                Name = "Extra Items List",
                Text = string.Empty,
                ControlSize = new Point(IsClassicPc ? 145 : WindowWidth - 68, 64),
                ViewSize = new Point(IsClassicPc ? 145 : WindowWidth - 68, 64),
                AutoViewSize = false,
                FontSize = IsClassicPc ? 6.5f : 8,
                TextColor = ModernHudTheme.TextGray,
                HasShadow = false
            };

            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i] = CreatePage();
                Controls.Add(_pages[i]);
            }

            _potionSettingsPage = CreatePage();
            _potionSettingsPage.Visible = false;
            Controls.Add(_potionSettingsPage);

            BuildHuntingPage(_pages[0]);
            BuildObtainingPage(_pages[1]);
            BuildOtherSettingsPage(_pages[2]);
            if (IsClassicPc)
                BuildClassicPotionSettingsPage(_potionSettingsPage);
            else
                BuildPotionSettingsPage(_potionSettingsPage);
            UpdatePageChildVisibility();

            _scrollUpButton = CreateExtraItemsScrollButton(pointsUp: true);
            _scrollDownButton = CreateExtraItemsScrollButton(pointsUp: false);
            _extraItemsPage.Controls.Add(_scrollUpButton);
            _extraItemsPage.Controls.Add(_scrollDownButton);
            ConfigureExtraItemsScrollButtons();
            UpdatePageChildVisibility();

            // _inputHintLabel = AddLabel(this, "Manual input keeps existing Helper ownership rules.", 15, 361, 160, 15, 6.4f, ModernHudTheme.TextGray);
            // _inputHintLabel.Visible = false;

            _startButton = CreateButton("Start", 35, FooterTop - 30, 117, 26, _controller.Toggle);
            _startButton.FontSize = 7.5f;
            // Controls.Add(_startButton);
            _saveButton = CreateButton("Save", 120, FooterTop, 52, 26, SaveSettings);
            _saveButton.FontSize = 7.5f;
            Controls.Add(_saveButton);
            _resetButton = CreateButton("Initialization", 65, FooterTop, 52, 26, ResetSettings);
            _resetButton.FontSize = 5.5f;
            Controls.Add(_resetButton);
            _closeButton = CreateButton("", 20, FooterTop, 36, 29, Close);
            _closeButton.FontSize = 7.5f;
            Controls.Add(_closeButton);

            ApplyPresentationLayout();
            if (IsClassicPc)
                UpdateClassicHuntingControlVisibility();

            _controller.StateChanged += OnControllerStateChanged;
            SetActiveTab(0);
            RefreshValues();
            ApplyVisualDesignerLayout();
            ApplyClassicDockScale();
            _classicDockScaleReady = true;
        }

        public void ToggleVisibility()
        {
            if (Visible)
                Close();
            else
                Open();
        }

        public void Open()
        {
            SyncPresentation();
            RefreshClassicDockScaleIfNeeded();
            Visible = true;
            BringToFront();
            Scene.FocusControl = this;
            _extraItemsBox.Text = string.Empty;
            UpdateExtraItemsList();
            RefreshValues();
        }

        public void Close()
        {
            CommitExtraItemText();
            Visible = false;
            if (Scene?.FocusControl == this || IsFocusedDescendant(Scene?.FocusControl))
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

            SyncPresentation();
            if (!_visualDesignerEditing)
                RefreshClassicDockScaleIfNeeded();
            EnsureTexturesLoaded();
            base.Update(gameTime);
            RefreshValues();

            var mouse = MuGame.Instance.UiMouseState;
            if (IsMouseOver && (mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed))
                Scene?.SetMouseInputConsumed();

            if (IsMouseOver && MuGame.Instance.UiMouseState.ScrollWheelValue != _previousWheelValue)
            {
                int delta = MuGame.Instance.UiMouseState.ScrollWheelValue - _previousWheelValue;
                Point mousePosition = MuGame.Instance.UiMouseState.Position;
                if (_activeTab == 1 && !_showPotionSettings &&
                    _extraItemsListLabel != null && _extraItemsListLabel.DisplayRectangle.Contains(mousePosition))
                {
                    ScrollExtraItemsList(delta > 0 ? -1 : 1);
                }
                else
                {
                    ScrollPage(delta > 0 ? -RowHeight * 3 : RowHeight * 3);
                }
            }
            _previousWheelValue = MuGame.Instance.UiMouseState.ScrollWheelValue;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            DrawFrameAndPanels();
            base.Draw(gameTime);
        }

        public override void Dispose()
        {
            _controller.StateChanged -= OnControllerStateChanged;
            base.Dispose();
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            if (_visualDesignerEditing)
                return;

            if (_classicDockScaleReady)
                ApplyClassicDockScale();
            else
                Recenter();
        }

        protected override void OnThemeChanged(UiThemeChangedEventArgs e)
        {
            base.OnThemeChanged(e);
            SyncPresentation();
            ApplyInterfaceTheme();
            SetActiveTab(_activeTab);
        }

        private async void EnsureTexturesLoaded()
        {
            if (_texturesLoaded)
                return;

            _texturesLoaded = true;
            try
            {
                for (int i = 0; i < FrameTexturePaths.Length; i++)
                    _frameTextures[i] = await UiThemeManager.LoadThemeTextureAsync(FrameTexturePaths[i]);
                for (int i = 0; i < PanelTexturePaths.Length; i++)
                    _panelTextures[i] = await UiThemeManager.LoadThemeTextureAsync(PanelTexturePaths[i]);
                _checkBoxTexture = await UiThemeManager.LoadThemeTextureAsync(CheckBoxTexturePath);
                for (int i = 0; i < HelperTexturePaths.Length; i++)
                    _helperTextures[i] = await UiThemeManager.LoadThemeTextureAsync(HelperTexturePaths[i]);

                ApplyLoadedHelperTextures();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "MU Helper interface texture loading failed; using procedural panels.");
            }
        }

        private void ApplyLoadedHelperTextures()
        {
            _potionSettingsBackButton?.SetThreeStateButtonTexture(IsClassicPc ? _helperTextures[6] : null);
            if (_extraItemsBox != null)
            {
                Texture2D inputTexture = IsClassicPc ? _helperTextures[2] : null;
                _extraItemsBox.BackgroundTexture = inputTexture;
                _extraItemsBox.BackgroundTextureSource = inputTexture != null
                    ? new Rectangle(0, 0, Math.Min(93, inputTexture.Width), Math.Min(15, inputTexture.Height))
                    : null;
                _extraItemsBox.BorderThickness = inputTexture != null ? 0 : 1;
            }
            if (_addExtraItemButton is HelperActionButton addItemButton)
                addItemButton.SetThreeStateButtonTexture(IsClassicPc ? _helperTextures[6] : null);
            if (_deleteExtraItemButton is HelperActionButton deleteItemButton)
                deleteItemButton.SetThreeStateButtonTexture(IsClassicPc ? _helperTextures[6] : null);
            if (_potionAutoHealToggle != null)
                _potionAutoHealToggle.GetCheckBoxTexture = () => _checkBoxTexture;
            foreach (HelperToggleButton toggle in _potionSettingsToggles)
                toggle.GetCheckBoxTexture = () => _checkBoxTexture;

            // ingame_Bt03 is a three-state 52x26 button atlas stored in a 64x128 texture.
            // Select one state and exclude both the other states and the power-of-two padding.
            // ((HelperActionButton)_startButton).SetThreeStateButtonTexture(IsClassicPc ? _helperTextures[6] : null);
            ((HelperActionButton)_saveButton).SetThreeStateButtonTexture(IsClassicPc ? _helperTextures[6] : null);
            ((HelperActionButton)_resetButton).SetThreeStateButtonTexture(IsClassicPc ? _helperTextures[6] : null);
            if (_closeButton is HelperActionButton closeButton)
            {
                closeButton.SetTexture(IsClassicPc ? _helperTextures[7] : null);
                closeButton.UseTopHalfTexture = IsClassicPc;
            }

            foreach ((ButtonControl button, int textureIndex, bool flip) in _classicIconButtons)
            {
                if (button is HelperActionButton iconButton)
                {
                    Texture2D texture = (uint)textureIndex < (uint)_helperTextures.Length
                        ? _helperTextures[textureIndex]
                        : null;
                    if (textureIndex == 6)
                        iconButton.SetThreeStateButtonTexture(texture);
                    else
                        iconButton.SetTexture(texture);
                    iconButton.FlipTextureHorizontally = flip;
                }
            }

            foreach (var binding in _boundButtons)
            {
                if (binding.Button is HelperActionButton actionButton)
                {
                    Texture2D texture = IsClassicPc && binding.Text() == "Setting" ? _helperTextures[6] : null;
                    actionButton.SetThreeStateButtonTexture(texture);
                }
                else if (binding.Button is HelperValueButton valueButton)
                    valueButton.SetTexture(IsClassicPc ? _helperTextures[1] : null);
                else if (binding.Button is SkillSlotButton skillButton)
                {
                    skillButton.GetSkillFrame = () => IsClassicPc ? _helperTextures[4] : null;
                    skillButton.GetActiveSkillFrame = () => IsClassicPc ? _helperTextures[5] : null;
                }
            }
        }

        private void DrawFrameAndPanels()
        {
            var sprite = GraphicsManager.Instance.Sprite;
            var pixel = GraphicsManager.Instance.Pixel;
            if (sprite == null || pixel == null)
                return;

            var rect = DisplayRectangle;
            if (!IsClassicPc)
            {
                sprite.Draw(pixel, rect, new Color(11, 13, 18, 246) * Alpha);
                DrawBorder(sprite, pixel, rect, new Color(104, 82, 45, 235));
                int margin = 16;
                var header = new Rectangle(rect.X + ScaleLogical(margin), rect.Y + ScaleLogical(44), rect.Width - ScaleLogical(margin * 2), ScaleLogical(3));
                sprite.Draw(pixel, header, new Color(125, 99, 54, 230) * Alpha);
                DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(margin), rect.Y + ScaleLogical(ContentTop - 8), rect.Width - ScaleLogical(margin * 2), ScaleLogical(ContentHeight + 16)));
                DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(margin), rect.Y + ScaleLogical(FooterTop - 8), rect.Width - ScaleLogical(margin * 2), rect.Height - ScaleLogical(FooterTop) - ScaleLogical(8)));
                return;
            }
            DrawTextureOrFill(sprite, pixel, _frameTextures[0], rect, new Color(8, 10, 16, 238));
            // Crop the power-of-two padding from the OZT source, then stretch the real pixels across the frame.
            Rectangle headerBack = new(rect.X, rect.Y, rect.Width, ScaleLogical(ClassicHeaderTextureHeight));
            if (_frameTextures[1] is { } itemBackTexture)
            {
                var source = new Rectangle(0, 0, ClassicHeaderTextureWidth, ClassicHeaderTextureHeight);
                sprite.Draw(itemBackTexture, headerBack, source, Color.White * Alpha);
            }
            else
            {
                DrawTextureOrFill(sprite, pixel, null, headerBack, new Color(18, 22, 30, 245));
            }
            int sideTop = rect.Y + ScaleLogical(64);
            int sideHeight = rect.Height - ScaleLogical(109);
            DrawCroppedTextureOrFill(sprite, pixel, _frameTextures[2],
                new Rectangle(rect.X, sideTop, ScaleLogical(ClassicSideTextureWidth), sideHeight),
                ClassicSideTextureWidth, ClassicSideTextureHeight, new Color(11, 14, 20, 245));
            DrawCroppedTextureOrFill(sprite, pixel, _frameTextures[3],
                new Rectangle(rect.Right - ScaleLogical(ClassicSideTextureWidth), sideTop,
                    ScaleLogical(ClassicSideTextureWidth), sideHeight),
                ClassicSideTextureWidth, ClassicSideTextureHeight, new Color(11, 14, 20, 245));
            DrawCroppedTextureOrFill(sprite, pixel, _frameTextures[4],
                new Rectangle(rect.X, rect.Bottom - ScaleLogical(ClassicFooterTextureHeight), rect.Width,
                    ScaleLogical(ClassicFooterTextureHeight)),
                ClassicFooterTextureWidth, ClassicFooterTextureHeight, new Color(16, 19, 26, 245));
            if (IsClassicPc)
            {
                if (_showPotionSettings)
                {
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(73), ScaleLogical(165), ScaleLogical(222)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(340), ScaleLogical(165), ScaleLogical(46)));
                }
                else if (_activeTab == 0)
                {
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(73), ScaleLogical(68), ScaleLogical(50)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(75), rect.Y + ScaleLogical(73), ScaleLogical(102), ScaleLogical(50)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(120), ScaleLogical(165), ScaleLogical(39)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(156), ScaleLogical(165), ScaleLogical(135)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(288), ScaleLogical(165), ScaleLogical(69)));
                    DrawClassicHuntingPanels(sprite, pixel, rect);
                    DrawClassicNumericInputBackground(sprite, pixel, _classicMaxSecondsAwayInput);
                    DrawClassicNumericInputBackground(sprite, pixel, _classicActivation1DelayInput);
                    DrawClassicNumericInputBackground(sprite, pixel, _classicActivation2DelayInput);
                }
                else if (_activeTab == 1)
                {
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(73), ScaleLogical(68), ScaleLogical(50)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(75), rect.Y + ScaleLogical(73), ScaleLogical(102), ScaleLogical(50)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(120), ScaleLogical(165), ScaleLogical(30)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(147), ScaleLogical(165), ScaleLogical(195)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(16), rect.Y + ScaleLogical(235), ScaleLogical(158), ScaleLogical(75)));
                    if (_extraItemsListLabel?.Visible == true)
                    {
                        Rectangle listBounds = _extraItemsListLabel.DisplayRectangle;
                        sprite.Draw(pixel, listBounds, new Color(5, 7, 11, 205) * Alpha);
                        DrawBorder(sprite, pixel, listBounds, ModernHudTheme.BorderInner);
                    }
                }
                else
                {
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(73), ScaleLogical(165), ScaleLogical(50)));
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(120), ScaleLogical(165), ScaleLogical(222)));
                }
                if (_activeTab != 0)
                    DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(340), ScaleLogical(165), ScaleLogical(46)));
            }
            else
            {
                int margin = 16;
                DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(margin), rect.Y + ScaleLogical(ContentTop - 8), ScaleLogical(WindowWidth - margin * 2), ScaleLogical(ContentHeight + 16)));
                DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(margin), rect.Y + ScaleLogical(FooterTop - 8), ScaleLogical(WindowWidth - margin * 2), ScaleLogical(WindowHeight - FooterTop - 8)));
            }
        }

        private void DrawClassicHuntingPanels(SpriteBatch sprite, Texture2D pixel, Rectangle rect)
        {
            DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(73), ScaleLogical(68), ScaleLogical(50)));
            DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(75), rect.Y + ScaleLogical(73), ScaleLogical(102), ScaleLogical(50)));
            DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(120), ScaleLogical(165), ScaleLogical(39)));
            DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(156), ScaleLogical(165), ScaleLogical(135)));
            DrawPanel(sprite, pixel, new Rectangle(rect.X + ScaleLogical(12), rect.Y + ScaleLogical(288), ScaleLogical(165), ScaleLogical(69)));
        }

        private void DrawClassicNumericInputBackground(SpriteBatch sprite, Texture2D pixel, TextBoxControl input)
        {
            if (input?.Visible == true)
                DrawCroppedTextureOrFill(sprite, pixel, _helperTextures[1], input.DisplayRectangle, 28, 15,
                    new Color(13, 17, 24, 245));
        }

        private int ScaleLogical(int value) => Math.Max(1, (int)MathF.Round(value * DisplaySize.X / (float)WindowWidth));

        private void DrawTextureOrFill(SpriteBatch sprite, Texture2D pixel, Texture2D texture, Rectangle destination, Color fallback)
        {
            if (texture != null)
            {
                sprite.Draw(texture, destination, Color.White * Alpha);
            }
            else
            {
                sprite.Draw(pixel, destination, fallback * Alpha);
            }
        }

        private void DrawCroppedTextureOrFill(SpriteBatch sprite, Texture2D pixel, Texture2D texture,
            Rectangle destination, int sourceWidth, int sourceHeight, Color fallback)
        {
            if (texture != null)
            {
                Rectangle source = new(
                    0,
                    0,
                    Math.Min(sourceWidth, texture.Width),
                    Math.Min(sourceHeight, texture.Height));
                sprite.Draw(texture, destination, source, Color.White * Alpha);
            }
            else
            {
                sprite.Draw(pixel, destination, fallback * Alpha);
            }
        }

        private void DrawPanel(SpriteBatch sprite, Texture2D pixel, Rectangle bounds)
        {
            sprite.Draw(pixel, new Rectangle(bounds.X + 3, bounds.Y + 2, Math.Max(1, bounds.Width - 6), Math.Max(1, bounds.Height - 4)), new Color(0, 0, 0, 102) * Alpha);
            if (IsClassicPc && _activeTab != 0 && bounds.Y >= DisplayRectangle.Y + ScaleLogical(340))
            {
                DrawBorder(sprite, pixel, bounds, ModernHudTheme.BorderInner);
                return;
            }
            if (IsClassicPc && _activeTab == 0 && !_showPotionSettings)
            {
                DrawClassicPanelTexture(sprite, bounds);
                return;
            }
            if (_panelTextures.All(texture => texture != null))
            {
                DrawPanelTexture(sprite, 0, new Rectangle(bounds.X, bounds.Y, 14, 14));
                DrawPanelTexture(sprite, 1, new Rectangle(bounds.Right - 14, bounds.Y, 14, 14));
                DrawPanelTexture(sprite, 2, new Rectangle(bounds.X, bounds.Bottom - 14, 14, 14));
                DrawPanelTexture(sprite, 3, new Rectangle(bounds.Right - 14, bounds.Bottom - 14, 14, 14));
                DrawPanelTexture(sprite, 4, new Rectangle(bounds.X + 6, bounds.Y, Math.Max(1, bounds.Width - 12), 14));
                DrawPanelTexture(sprite, 5, new Rectangle(bounds.X + 6, bounds.Bottom - 14, Math.Max(1, bounds.Width - 12), 14));
                DrawPanelTexture(sprite, 6, new Rectangle(bounds.X, bounds.Y + 6, 14, Math.Max(1, bounds.Height - 12)));
                DrawPanelTexture(sprite, 7, new Rectangle(bounds.Right - 14, bounds.Y + 6, 14, Math.Max(1, bounds.Height - 12)));
            }
            else
            {
                DrawBorder(sprite, pixel, bounds, ModernHudTheme.BorderInner);
            }
        }

        private void DrawClassicPanelTexture(SpriteBatch sprite, Rectangle bounds)
        {
            if (!_panelTextures.All(texture => texture != null))
            {
                DrawBorder(sprite, GraphicsManager.Instance.Pixel, bounds, ModernHudTheme.BorderInner);
                return;
            }

            DrawPanelTexture(sprite, 0, new Rectangle(bounds.X, bounds.Y, 14, 14));
            DrawPanelTexture(sprite, 1, new Rectangle(bounds.Right - 14, bounds.Y, 14, 14));
            DrawPanelTexture(sprite, 2, new Rectangle(bounds.X, bounds.Bottom - 14, 14, 14));
            DrawPanelTexture(sprite, 3, new Rectangle(bounds.Right - 14, bounds.Bottom - 14, 14, 14));
            DrawPanelTexture(sprite, 4, new Rectangle(bounds.X + 6, bounds.Y, Math.Max(1, bounds.Width - 12), 14));
            DrawPanelTexture(sprite, 5, new Rectangle(bounds.X + 6, bounds.Bottom - 14, Math.Max(1, bounds.Width - 12), 14));
            DrawPanelTexture(sprite, 6, new Rectangle(bounds.X, bounds.Y + 6, 14, Math.Max(1, bounds.Height - 12)));
            DrawPanelTexture(sprite, 7, new Rectangle(bounds.Right - 14, bounds.Y + 6, 14, Math.Max(1, bounds.Height - 12)));
        }

        private void DrawPanelTexture(SpriteBatch sprite, int index, Rectangle destination)
        {
            Texture2D texture = _panelTextures[index];
            sprite.Draw(texture, destination, new Rectangle(0, 0, texture.Width, texture.Height), Color.White * Alpha);
        }

        private void DrawBorder(SpriteBatch sprite, Texture2D pixel, Rectangle rect, Color color)
        {
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color * Alpha);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color * Alpha);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color * Alpha);
            sprite.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color * Alpha);
        }

        private void ApplyInterfaceTheme()
        {
            bool classic = UiThemeManager.CurrentId == UiThemeId.Classic;
            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;
            BorderThickness = 0;

            Color buttonBackground = classic ? new Color(18, 26, 40, 245) : new Color(25, 30, 40, 245);
            Color buttonHover = classic ? new Color(78, 58, 28, 245) : new Color(55, 47, 31, 245);
            Color buttonPressed = classic ? new Color(42, 32, 18, 250) : new Color(38, 34, 29, 250);
            foreach (var binding in _boundButtons)
            {
                if (binding.Button is HelperValueButton valueButton)
                {
                    valueButton.SetTexture(IsClassicPc ? _helperTextures[1] : null);
                    valueButton.BackgroundColor = Color.Transparent;
                    valueButton.HoverBackgroundColor = Color.Transparent;
                    valueButton.PressedBackgroundColor = Color.Transparent;
                    valueButton.BorderColor = Color.Transparent;
                    valueButton.BorderThickness = 0;
                    valueButton.TextColor = ModernHudTheme.TextWhite;
                    valueButton.HoverTextColor = ModernHudTheme.TextGold;
                    continue;
                }

                if (binding.Button is HelperToggleButton)
                {
                    binding.Button.BackgroundColor = Color.Transparent;
                    binding.Button.HoverBackgroundColor = Color.Transparent;
                    binding.Button.PressedBackgroundColor = Color.Transparent;
                    binding.Button.BorderColor = Color.Transparent;
                    binding.Button.BorderThickness = 0;
                    binding.Button.TextColor = Color.Transparent;
                    binding.Button.HoverTextColor = Color.Transparent;
                    binding.Button.DisabledTextColor = Color.Transparent;
                    continue;
                }

                binding.Button.BackgroundColor = buttonBackground;
                binding.Button.HoverBackgroundColor = buttonHover;
                binding.Button.PressedBackgroundColor = buttonPressed;
                binding.Button.BorderColor = ModernHudTheme.BorderInner;
                binding.Button.TextColor = ModernHudTheme.TextWhite;
                binding.Button.HoverTextColor = ModernHudTheme.TextGold;
            }

            if (_extraItemsBox != null)
            {
                _extraItemsBox.BackgroundColor = classic ? new Color(16, 22, 34, 245) : new Color(22, 26, 35, 245);
                _extraItemsBox.BorderColor = ModernHudTheme.BorderInner;
                _extraItemsBox.FocusedBorderColor = ModernHudTheme.AccentBright;
                _extraItemsBox.TextColor = IsClassicPc ? Color.Black : ModernHudTheme.TextWhite;
            }
        }

        private void BuildHuntingPage(UIControl page)
        {
            if (IsClassicPc)
            {
                BuildClassicHuntingPage(page);
                return;
            }

            int row = 0;
            AddLabel(page, "HUNTING RANGE", 5, 2, 130, 13, 7.2f, ModernHudTheme.TextGold, bold: true);
            AddValueRow(page, "Range", 5, row++, () => $"{_controller.Config.HuntingRange} tiles", () => _controller.Config.HuntingRange = NextValue(_controller.Config.HuntingRange, 0, 15, 1));
            AddPotionToggleSettingRow(page, row++);
            AddToggleRow(page, "Long range counterattack", 5, row++, () => _controller.Config.LongRangeCounterAttack, value => _controller.Config.LongRangeCounterAttack = value);
            AddToggleRow(page, "Return to start position", 5, row++, () => _controller.Config.ReturnToOriginalPosition, value => _controller.Config.ReturnToOriginalPosition = value);
            AddValueRow(page, "Max. seconds away", 5, row++, () => $"{_controller.Config.MaxSecondsAway}s", () => _controller.Config.MaxSecondsAway = NextValue(_controller.Config.MaxSecondsAway, 0, 999, 5));
            AddSkillRow(page, "Basic Skill", 5, row++, () => _controller.Config.BasicSkillId, id => _controller.Config.BasicSkillId = id);
            AddToggleRow(page, "Fallback basic attack", 5, row++, () => _controller.Config.FallbackBasicAttack, value => _controller.Config.FallbackBasicAttack = value);
            AddSkillRow(page, "Activation Skill 1", 5, row++, () => _controller.Config.ActivationSkill1.SkillId, id => _controller.Config.ActivationSkill1.SkillId = id);
            AddToggleRow(page, "Activation 1 timer", 5, row++, () => _controller.Config.ActivationSkill1.UseTimer, value => _controller.Config.ActivationSkill1.UseTimer = value);
            AddToggleRow(page, "Activation 1 condition", 5, row++, () => _controller.Config.ActivationSkill1.UseCondition, value => _controller.Config.ActivationSkill1.UseCondition = value);
            AddValueRow(page, "Activation 1 delay", 5, row++, () => $"{_controller.Config.ActivationSkill1.DelaySeconds}s", () => _controller.Config.ActivationSkill1.DelaySeconds = NextValue(_controller.Config.ActivationSkill1.DelaySeconds, 0, 3600, 5));
            AddSkillRow(page, "Activation Skill 2", 5, row++, () => _controller.Config.ActivationSkill2.SkillId, id => _controller.Config.ActivationSkill2.SkillId = id);
            AddToggleRow(page, "Activation 2 timer", 5, row++, () => _controller.Config.ActivationSkill2.UseTimer, value => _controller.Config.ActivationSkill2.UseTimer = value);
            AddToggleRow(page, "Activation 2 condition", 5, row++, () => _controller.Config.ActivationSkill2.UseCondition, value => _controller.Config.ActivationSkill2.UseCondition = value);
            AddValueRow(page, "Activation 2 delay", 5, row++, () => $"{_controller.Config.ActivationSkill2.DelaySeconds}s", () => _controller.Config.ActivationSkill2.DelaySeconds = NextValue(_controller.Config.ActivationSkill2.DelaySeconds, 0, 3600, 5));
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                AddSkillRow(page, $"Buff skill {i + 1}", 5, row++, () => _controller.Config.BuffSkillIds[slot], id => _controller.Config.BuffSkillIds[slot] = id);
            }
            AddToggleRow(page, "Buff duration", 5, row++, () => _controller.Config.BuffDuration, value => _controller.Config.BuffDuration = value);
            AddValueRow(page, "Buff cast interval", 5, row++, () => $"{_controller.Config.BuffCastIntervalSeconds}s", () => _controller.Config.BuffCastIntervalSeconds = NextValue(_controller.Config.BuffCastIntervalSeconds, 0, 3600, 30));
            AddToggleRow(page, "Use combo skills", 5, row++, () => _controller.Config.UseCombo, value => _controller.Config.UseCombo = value);
            AddToggleRow(page, "Use Dark Raven", 5, row++, () => _controller.Config.UseDarkRaven, value => _controller.Config.UseDarkRaven = value);
            AddToggleRow(page, "Repair equipment", 5, row, () => _controller.Config.RepairItem, value => _controller.Config.RepairItem = value);
        }

        private void BuildClassicHuntingPage(UIControl page)
        {
            // --- Range (top-left) ---
            AddClassicLabel(page, "Range", 8, 6, 34, 14, 7.5f, ModernHudTheme.TextWhite);
            AddClassicValueLabel(page, () => _controller.Config.HuntingRange.ToString(), 12, 22, 20, 18, 11, ModernHudTheme.TextGold);
            AddClassicIconButton(page, 40, 6, 16, 15,
                () => _controller.Config.HuntingRange = Math.Clamp(_controller.Config.HuntingRange + 1, 0, 15), 8);
            AddClassicIconButton(page, 40, 26, 16, 15,
                () => _controller.Config.HuntingRange = Math.Clamp(_controller.Config.HuntingRange - 1, 0, 15), 0, flip: true);

            // --- Potion + Setting (top-right of range panel) ---
            AddClassicToggle(page, "Potion", 72, 8, () => _controller.Config.UseHealPotion,
                value => _controller.Config.UseHealPotion = value, out _);
            var potionSetting = CreateButton("Setting", 118, 18, 40, 22, ShowPotionSettings);
            potionSetting.FontSize = 6.5f;
            FitButtonWidthToText(potionSetting, 30, 44);
            page.Controls.Add(potionSetting);
            _boundButtons.Add((potionSetting, () => "Setting"));
            _classicHuntingControls.Add(potionSetting);

            // --- Counter attack ---
            AddClassicToggle(page, "Long-Distance Counter Attack", 6, 50,
                () => _controller.Config.LongRangeCounterAttack,
                value => _controller.Config.LongRangeCounterAttack = value, out _);

            // --- Original Position + Distance [box] Min ---
            // Official uses "Min"; keep "Sec" if your config is seconds.
            AddClassicToggle(page, "Original Position", 6, 66,
                () => _controller.Config.ReturnToOriginalPosition,
                value => _controller.Config.ReturnToOriginalPosition = value, out _);
            AddClassicLabel(page, "Distance", 100, 66, 32, 14, 5.5f, ModernHudTheme.TextWhite);
            AddClassicNumericInput(page, _classicMaxSecondsAwayInput, 132, 65);
            AddClassicLabel(page, "Sec", 160, 66, 18, 14, 6.5f, ModernHudTheme.TextWhite);
            // If unit is clipped, move box to X=128 and unit to X=156, or shrink font further.

            // --- Basic Skill | Activation Skill 1 ---
            AddClassicSkillSlot(page, "Basic Skill", 10, 100,
                () => _controller.Config.BasicSkillId, id => _controller.Config.BasicSkillId = id, 32, 38);

            AddClassicLabel(page, "Activation Skill 1", 52, 88, 78, 12, 6.2f, ModernHudTheme.TextWhite);
            AddClassicSkillSlot(page, string.Empty, 54, 100,
                () => _controller.Config.ActivationSkill1.SkillId, id => _controller.Config.ActivationSkill1.SkillId = id, 32, 38);

            // Delay checkbox + number + "s" on one row; Con under Delay
            AddClassicToggle(page, "Delay", 92, 102, () => _controller.Config.ActivationSkill1.UseTimer,
                value => _controller.Config.ActivationSkill1.UseTimer = value, out _);
            AddClassicNumericInput(page, _classicActivation1DelayInput, 128, 102);
            AddClassicLabel(page, "s", 156, 103, 8, 14, 6.5f, ModernHudTheme.TextWhite);
            AddClassicToggle(page, "Con", 92, 120, () => _controller.Config.ActivationSkill1.UseCondition,
                value => _controller.Config.ActivationSkill1.UseCondition = value, out _);

            // Optional: Setting button like official (wire to same ShowPotionSettings or skill options)
            // var act1Setting = CreateButton("Setting", 128, 118, 36, 18, ShowPotionSettings);
            // ...

            // --- Activation Skill 2 ---
            AddClassicLabel(page, "Activation Skill 2", 52, 148, 78, 12, 6.2f, ModernHudTheme.TextWhite);
            AddClassicSkillSlot(page, string.Empty, 54, 160,
                () => _controller.Config.ActivationSkill2.SkillId, id => _controller.Config.ActivationSkill2.SkillId = id, 32, 38);

            AddClassicToggle(page, "Combo", 10, 168, () => _controller.Config.UseCombo,
                value => _controller.Config.UseCombo = value, out _);
            AddClassicToggle(page, "Delay", 92, 162, () => _controller.Config.ActivationSkill2.UseTimer,
                value => _controller.Config.ActivationSkill2.UseTimer = value, out _);
            AddClassicNumericInput(page, _classicActivation2DelayInput, 128, 162);
            AddClassicLabel(page, "s", 156, 163, 8, 14, 6.5f, ModernHudTheme.TextWhite);
            AddClassicToggle(page, "Con", 92, 180, () => _controller.Config.ActivationSkill2.UseCondition,
                value => _controller.Config.ActivationSkill2.UseCondition = value, out _);

            // --- Buff Duration + 3 slots ---
            AddClassicToggle(page, "Buff Duration", 10, 230, () => _controller.Config.BuffDuration,
                value => _controller.Config.BuffDuration = value, out _);
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                AddClassicSkillSlot(page, string.Empty, 14 + i * 36, 248,
                    () => _controller.Config.BuffSkillIds[slot], id => _controller.Config.BuffSkillIds[slot] = id, 32, 38);
            }
        }

        private LabelControl AddClassicLabel(UIControl page, string text, int x, int y, int width, int height, float fontSize, Color color)
        {
            LabelControl label = AddLabel(page, text, x, y, width, height, fontSize, color);
            _classicHuntingControls.Add(label);
            return label;
        }

        private void AddClassicNumericInput(UIControl page, TextBoxControl input, int x, int y)
        {
            input.X = x;
            input.Y = y;
            input.ControlSize = new Point(28, 16);
            input.ViewSize = input.ControlSize;
            page.Controls.Add(input);
            _classicHuntingControls.Add(input);
        }

        private void AddClassicToggle(UIControl page, string text, int x, int y, Func<bool> getValue,
            Action<bool> setValue, out HelperToggleButton toggle)
        {
            LabelControl label = AddClassicLabel(page, text, x + 17, y, Math.Max(1, 165 - x - 18), 15,
                7, ModernHudTheme.TextWhite);
            toggle = new HelperToggleButton(getValue)
            {
                Name = $"{text} Toggle",
                X = x,
                Y = y,
                ControlSize = new Point(15, 15),
                ViewSize = new Point(15, 15),
                AutoViewSize = false,
                GetCheckBoxTexture = () => _checkBoxTexture,
                LogicalCheckSize = 15,
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0,
                TextColor = Color.Transparent,
                HoverTextColor = Color.Transparent,
                DisabledTextColor = Color.Transparent
            };
            toggle.Click += (_, _) =>
            {
                setValue(!getValue());
                _controller.Config.Normalize();
                RefreshValues();
            };
            page.Controls.Add(toggle);
            _boundButtons.Add((toggle, () => string.Empty));
            _classicHuntingControls.Add(toggle);
        }

        private void AddClassicSkillSlot(UIControl page, string label, int x, int y,
            Func<ushort> getSkillId, Action<ushort> setSkillId, int width, int height)
        {
            if (!string.IsNullOrWhiteSpace(label))
            {
                LabelControl slotLabel = AddLabel(page, label, x, y - 11, width, 12, 6.5f, ModernHudTheme.TextWhite);
                _classicHuntingControls.Add(slotLabel);
            }

            var slot = new SkillSlotButton(getSkillId)
            {
                Text = string.Empty,
                X = x,
                Y = y,
                ControlSize = new Point(width, height),
                ViewSize = new Point(width, height),
                AutoViewSize = false,
                FontSize = 6.5f,
                UseClassicIconLayout = true,
                GetSkillFrame = () => _helperTextures[4],
                GetActiveSkillFrame = () => _helperTextures[5],
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0
            };
            ConfigureSkillAssignment(slot, getSkillId, setSkillId);
            page.Controls.Add(slot);
            _boundButtons.Add((slot, () => GetSkillLabel(getSkillId())));
            _classicHuntingControls.Add(slot);
        }

        private void AddClassicIconButton(UIControl page, int x, int y, int width, int height,
            Action action, int textureIndex, bool flip = false)
        {
            var button = CreateButton(string.Empty, x, y, width, height, () =>
            {
                action();
                _controller.Config.Normalize();
                RefreshValues();
            });
            Texture2D texture = (uint)textureIndex < (uint)_helperTextures.Length
                ? _helperTextures[textureIndex]
                : null;
            button.SetTexture(texture);
            button.FlipTextureHorizontally = flip;
            button.DrawRangeFallback = true;
            page.Controls.Add(button);
            _classicIconButtons.Add((button, textureIndex, flip));
            _classicHuntingControls.Add(button);
        }

        private void AddClassicValueLabel(UIControl page, Func<string> getValue, int x, int y,
            int width, int height, float fontSize, Color color)
        {
            LabelControl label = AddLabel(page, getValue(), x, y, width, height, fontSize, color);
            _boundLabels.Add((label, getValue));
            _classicHuntingControls.Add(label);
        }

        private void ConfigureSkillAssignment(SkillSlotButton button, Func<ushort> getSkillId, Action<ushort> setSkillId)
        {
            button.Click += (_, _) =>
            {
                ModernBottomHud hud = _scene.ModernHud;
                if (hud == null)
                {
                    _logger?.LogWarning("The learned-skill picker is unavailable; cannot assign MU Helper skill slot.");
                    return;
                }

                _pendingSkillAssignment = skill =>
                {
                    setSkillId(skill.SkillId);
                    RefreshValues();
                };
                hud.BeginSkillSelection(skill =>
                {
                    if (!Visible || _pendingSkillAssignment == null)
                        return false;
                    _pendingSkillAssignment(skill);
                    _pendingSkillAssignment = null;
                    return true;
                });
            };
            button.SetClearAction(() =>
            {
                setSkillId(0);
                _pendingSkillAssignment = null;
                RefreshValues();
            });
        }

        private void BuildObtainingPage(UIControl page)
        {
            _extraItemsPage = page;
            if (IsClassicPc)
            {
                BuildClassicObtainingPage(page);
                return;
            }

            int row = 0;
            AddLabel(page, "ITEM OBTAINING", 5, 2, 140, 13, 7.2f, ModernHudTheme.TextGold, bold: true);
            AddValueRow(page, "Obtaining range", 5, row++, () => $"{_controller.Config.ObtainingRange} tiles", () => _controller.Config.ObtainingRange = NextValue(_controller.Config.ObtainingRange, 0, 15, 1));
            AddToggleRow(page, "Pick Zen", 5, row++, () => _controller.Config.PickZen, value => _controller.Config.PickZen = value);
            AddToggleRow(page, "Pick all items", 5, row++, () => _controller.Config.PickAllItems, value => _controller.Config.PickAllItems = value);
            AddToggleRow(page, "Pick selected items", 5, row++, () => _controller.Config.PickSelectedItems, value => _controller.Config.PickSelectedItems = value);
            AddToggleRow(page, "Pick jewels", 5, row++, () => _controller.Config.PickJewel, value => _controller.Config.PickJewel = value);
            AddToggleRow(page, "Pick Ancient", 5, row++, () => _controller.Config.PickAncient, value => _controller.Config.PickAncient = value);
            AddToggleRow(page, "Pick Excellent", 5, row++, () => _controller.Config.PickExcellent, value => _controller.Config.PickExcellent = value);
            AddToggleRow(page, "Use extra name filters", 5, row++, () => _controller.Config.PickExtraItems, value => _controller.Config.PickExtraItems = value);
            AddValueRow(page, "Filter entries", 5, row++, () => (_controller.Config.ExtraItems?.Count ?? 0).ToString(), FocusExtraItemFilter);
            AddLabel(page, "Item name filters", 5, row * RowHeight + 4, 150, 14, 7, ModernHudTheme.TextGray);
            int inputY = row * RowHeight + 20;
            _extraItemsBox.X = 5;
            _extraItemsBox.Y = inputY;
            _extraItemsBox.ControlSize = new Point(WindowWidth - 130, IsClassicPc ? 22 : 30);
            _extraItemsBox.ViewSize = _extraItemsBox.ControlSize;
            page.Controls.Add(_extraItemsBox);
            _addExtraItemButton.X = WindowWidth - 112;
            _addExtraItemButton.Y = inputY;
            _addExtraItemButton.ControlSize = new Point(50, IsClassicPc ? 22 : 30);
            _addExtraItemButton.ViewSize = _addExtraItemButton.ControlSize;
            page.Controls.Add(_addExtraItemButton);
            _extraItemsListLabel.X = 5;
            _extraItemsListLabel.Y = inputY + (IsClassicPc ? 27 : 34);
            _extraItemsListLabel.ControlSize = new Point(WindowWidth - 68, IsClassicPc ? 48 : 72);
            _extraItemsListLabel.ViewSize = _extraItemsListLabel.ControlSize;
            page.Controls.Add(_extraItemsListLabel);
            _deleteExtraItemButton.X = WindowWidth - 112;
            _deleteExtraItemButton.Y = _extraItemsListLabel.Y + _extraItemsListLabel.ViewSize.Y + 4;
            _deleteExtraItemButton.ControlSize = new Point(50, IsClassicPc ? 22 : 30);
            _deleteExtraItemButton.ViewSize = _deleteExtraItemButton.ControlSize;
            page.Controls.Add(_deleteExtraItemButton);
            _extraItemsBox.Visible = true;
            _addExtraItemButton.Visible = true;
            _extraItemsListLabel.Visible = true;
            AddLabel(page, "Enter an item name and press Add.", 5, inputY + (IsClassicPc ? 76 : 108), 150, 18, 6.5f, ModernHudTheme.TextGray);
        }

        private void BuildOtherSettingsPage(UIControl page)
        {
            if (IsClassicPc)
            {
                BuildClassicOtherSettingsPage(page);
                return;
            }

            int row = 0;
            AddLabel(page, "OTHER SETTINGS", 5, 2, 140, 13, 7.2f, ModernHudTheme.TextGold, bold: true);
            AddToggleRow(page, "Support party members", 5, row++, () => _controller.Config.SupportParty, value => _controller.Config.SupportParty = value);
            AddToggleRow(page, "Heal party members", 5, row++, () => _controller.Config.AutoHealParty, value => _controller.Config.AutoHealParty = value);
            AddValueRow(page, "Party heal threshold", 5, row++, () => $"{_controller.Config.HealPartyThreshold}%", () => _controller.Config.HealPartyThreshold = NextValue(_controller.Config.HealPartyThreshold, 0, 100, 5));
            AddToggleRow(page, "Maintain party buffs", 5, row++, () => _controller.Config.BuffDurationParty, value => _controller.Config.BuffDurationParty = value);
            AddValueRow(page, "Buff cast interval", 5, row++, () => $"{_controller.Config.BuffCastIntervalSeconds}s", () => _controller.Config.BuffCastIntervalSeconds = NextValue(_controller.Config.BuffCastIntervalSeconds, 0, 3600, 30));
            AddToggleRow(page, "Auto-accept friend requests", 5, row++, () => _controller.Config.AutoAcceptFriend, value => _controller.Config.AutoAcceptFriend = value);
            AddToggleRow(page, "Auto-accept guild requests", 5, row++, () => _controller.Config.AutoAcceptGuild, value => _controller.Config.AutoAcceptGuild = value);
            AddToggleRow(page, "Use self-defense", 5, row++, () => _controller.Config.UseSelfDefense, value => _controller.Config.UseSelfDefense = value);
            AddToggleRow(page, "Auto heal", 5, row++, () => _controller.Config.AutoHeal, value => _controller.Config.AutoHeal = value);
            AddValueRow(page, "Heal threshold", 5, row++, () => $"{_controller.Config.HealThreshold}%", () => _controller.Config.HealThreshold = NextValue(_controller.Config.HealThreshold, 0, 100, 5));
            AddValueRow(page, "Healing potion slot", 5, row++, GetPotionSlotText, CyclePotionSlot);
            AddToggleRow(page, "Use Drain Life", 5, row++, () => _controller.Config.UseDrainLife, value => _controller.Config.UseDrainLife = value);
            AddToggleRow(page, "Dark Raven", 5, row++, () => _controller.Config.UseDarkRaven, value => _controller.Config.UseDarkRaven = value);
        }

        private void BuildClassicPotionSettingsPage(UIControl page)
        {
            AddClassicLabel(page, "Auto Recovery", 37, 6, 112, 18, 8, ModernHudTheme.TextWhite);
            AddClassicLabel(page, "Auto Potion", 14, 39, 110, 18, 8, ModernHudTheme.TextWhite);
            _potionSettingsToggles.Add(CreatePotionSettingsToggle(140, 39,
                () => _controller.Config.UseHealPotion, () => _controller.Config.UseHealPotion = !_controller.Config.UseHealPotion));
            AddThresholdTrack(page, 30, 64, () => _controller.Config.PotionThreshold,
                value => _controller.Config.PotionThreshold = value);
            AddClassicLabel(page, "HP Status", 52, 82, 100, 18, 7, ModernHudTheme.TextWhite);
            AddClassicLabel(page, "Auto Heal", 14, 112, 110, 18, 8, ModernHudTheme.TextWhite);
            _potionAutoHealToggle = CreatePotionSettingsToggle(140, 112,
                () => _controller.Config.AutoHeal, () => _controller.Config.AutoHeal = !_controller.Config.AutoHeal);
            _potionSettingsToggles.Add(_potionAutoHealToggle);
            AddThresholdTrack(page, 30, 137, () => _controller.Config.HealThreshold,
                value => _controller.Config.HealThreshold = value);
            AddClassicLabel(page, "MP Status", 52, 155, 100, 18, 7, ModernHudTheme.TextWhite);
        }

        private void BuildClassicObtainingPage(UIControl page)
        {
            // All coordinates and dimensions below are logical pixels relative to `page`.
            // AddClassicLabel/AddClassicValueLabel arguments after text/value are:
            // x, y, width, height, fontSize, color. Icon button arguments are x, y, width, height.
            // --- Range (top-left panel) ---
            AddClassicLabel(page, "Range", 10, 6, 34, 14, 7.5f, ModernHudTheme.TextWhite);
            AddClassicValueLabel(page, () => _controller.Config.ObtainingRange.ToString(), 14, 22, 20, 18, 11, ModernHudTheme.TextGold);
            AddClassicIconButton(page, 42, 8, 16, 15,
                () => _controller.Config.ObtainingRange = Math.Clamp(_controller.Config.ObtainingRange + 1, 0, 15), 8);
            AddClassicIconButton(page, 42, 28, 16, 15,
                () => _controller.Config.ObtainingRange = Math.Clamp(_controller.Config.ObtainingRange - 1, 0, 15), 0, flip: true);

            // --- Repair (top-right of range panel) ---
            AddClassicToggle(page, "Repair Item", 78, 10, () => _controller.Config.RepairItem,
                value => _controller.Config.RepairItem = value, out _);

            // --- Main pick toggles (full rows, clear of range) ---
            AddClassicToggle(page, "Pick All Near Items", 10, 52, () => _controller.Config.PickAllItems,
                value => _controller.Config.PickAllItems = value, out _);
            AddClassicToggle(page, "Pick Selected Items", 10, 70, () => _controller.Config.PickSelectedItems,
                value => _controller.Config.PickSelectedItems = value, out _);

            // --- 2x2 item filters ---
            AddClassicToggle(page, "Jewel/Gem", 10, 96, () => _controller.Config.PickJewel,
                value => _controller.Config.PickJewel = value, out _);
            AddClassicToggle(page, "Set Item", 90, 96, () => _controller.Config.PickAncient,
                value => _controller.Config.PickAncient = value, out _);   // or rename label only
            AddClassicToggle(page, "Zen", 10, 114, () => _controller.Config.PickZen,
                value => _controller.Config.PickZen = value, out _);
            AddClassicToggle(page, "Excellent Item", 90, 114, () => _controller.Config.PickExcellent,
                value => _controller.Config.PickExcellent = value, out _);

            // --- Extra item filter ---
            AddClassicToggle(page, "Add Extra Item", 10, 136, () => _controller.Config.PickExtraItems,
                value => _controller.Config.PickExtraItems = value, out _);

            _extraItemsBox.X = 12;
            _extraItemsBox.Y = 156;
            _extraItemsBox.ControlSize = new Point(110, 18);
            _extraItemsBox.ViewSize = _extraItemsBox.ControlSize;
            _extraItemsBox.FontSize = 7;
            page.Controls.Add(_extraItemsBox);

            _addExtraItemButton.X = 126;
            _addExtraItemButton.Y = 154;
            _addExtraItemButton.ControlSize = new Point(34, 22);
            _addExtraItemButton.ViewSize = _addExtraItemButton.ControlSize;
            _addExtraItemButton.FontSize = 6.5f;
            page.Controls.Add(_addExtraItemButton);

            // List area (official dark box)
            _extraItemsListLabel.X = 12;
            _extraItemsListLabel.Y = 178;
            _extraItemsListLabel.ControlSize = new Point(141, 100);
            _extraItemsListLabel.ViewSize = _extraItemsListLabel.ControlSize;
            page.Controls.Add(_extraItemsListLabel);

            _deleteExtraItemButton.X = 112;
            _deleteExtraItemButton.Y = 282;
            _deleteExtraItemButton.ControlSize = new Point(48, 22);
            _deleteExtraItemButton.ViewSize = _deleteExtraItemButton.ControlSize;
            _deleteExtraItemButton.FontSize = 6.5f;
            page.Controls.Add(_deleteExtraItemButton);

            _classicHuntingControls.Add(_extraItemsBox);
            _classicHuntingControls.Add(_addExtraItemButton);
            _classicHuntingControls.Add(_deleteExtraItemButton);
            _classicHuntingControls.Add(_extraItemsListLabel);
        }

        private void BuildClassicOtherSettingsPage(UIControl page)
        {
            // Single column, ~18px row pitch, values on the right edge of the 165px page
            AddClassicToggle(page, "Support party members", 10, 10, () => _controller.Config.SupportParty,
                value => _controller.Config.SupportParty = value, out _);

            AddClassicToggle(page, "Heal party members", 10, 30, () => _controller.Config.AutoHealParty,
                value => _controller.Config.AutoHealParty = value, out _);
            AddClassicValueLabel(page, () => $"{_controller.Config.HealPartyThreshold}%", 138, 30, 26, 15, 7, ModernHudTheme.TextWhite);

            AddClassicToggle(page, "Maintain party buffs", 10, 50, () => _controller.Config.BuffDurationParty,
                value => _controller.Config.BuffDurationParty = value, out _);

            AddClassicToggle(page, "Auto heal", 10, 78, () => _controller.Config.AutoHeal,
                value => _controller.Config.AutoHeal = value, out _);
            AddClassicValueLabel(page, () => $"{_controller.Config.HealThreshold}%", 138, 78, 26, 15, 7, ModernHudTheme.TextWhite);

            AddClassicToggle(page, "Use Drain Life", 10, 104, () => _controller.Config.UseDrainLife,
                value => _controller.Config.UseDrainLife = value, out _);
            AddClassicToggle(page, "Use Dark Raven", 10, 124, () => _controller.Config.UseDarkRaven,
                value => _controller.Config.UseDarkRaven = value, out _);
            AddClassicToggle(page, "Self defense", 10, 144, () => _controller.Config.UseSelfDefense,
                value => _controller.Config.UseSelfDefense = value, out _);
            AddClassicToggle(page, "Fallback basic attack", 10, 164, () => _controller.Config.FallbackBasicAttack,
                value => _controller.Config.FallbackBasicAttack = value, out _);
        }

        private void BuildPotionSettingsPage(UIControl page)
        {
            foreach (HelperToggleButton toggle in _potionSettingsToggles)
                page.Controls.Add(toggle);

            _potionSettingsBackButton = CreateButton("Back", 5, 5, 44, 22, HidePotionSettings);
            _potionSettingsBackButton.FontSize = IsClassicPc ? 7 : 9;
            page.Controls.Add(_potionSettingsBackButton);

            AddLabel(page, "Auto Recovery", IsClassicPc ? 52 : 56, 6, 112, 18, IsClassicPc ? 8 : 10,
                ModernHudTheme.TextWhite, bold: true);
            AddLabel(page, "Auto Potion", 14, 39, 110, 18, IsClassicPc ? 8 : 10, ModernHudTheme.TextWhite);
            _potionSettingsToggles.Add(CreatePotionSettingsToggle(140, 39, () => _controller.Config.UseHealPotion, () =>
            {
                _controller.Config.UseHealPotion = !_controller.Config.UseHealPotion;
            }));
            AddThresholdTrack(page, 30, 64, () => _controller.Config.PotionThreshold,
                value => _controller.Config.PotionThreshold = value);
            AddLabel(page, "HP Status", 52, 82, 100, 18, IsClassicPc ? 7 : 9, ModernHudTheme.TextWhite);

            AddLabel(page, "Auto Heal", 14, 112, 110, 18, IsClassicPc ? 8 : 10, ModernHudTheme.TextWhite);
            _potionAutoHealToggle = CreatePotionSettingsToggle(140, 112, () => _controller.Config.AutoHeal, () =>
            {
                _controller.Config.AutoHeal = !_controller.Config.AutoHeal;
            });
            _potionSettingsToggles.Add(_potionAutoHealToggle);
            foreach (HelperToggleButton toggle in _potionSettingsToggles)
                page.Controls.Add(toggle);
            AddThresholdTrack(page, 30, 137, () => _controller.Config.HealThreshold,
                value => _controller.Config.HealThreshold = value);
            AddLabel(page, "MP Status", 52, 155, 100, 18, IsClassicPc ? 7 : 9, ModernHudTheme.TextWhite);
        }

        private HelperToggleButton CreatePotionSettingsToggle(int x, int y, Func<bool> getValue, Action toggle)
        {
            var button = new HelperToggleButton(getValue)
            {
                X = x,
                Y = y,
                ControlSize = new Point(18, 18),
                ViewSize = new Point(18, 18),
                AutoViewSize = false,
                LogicalCheckSize = IsClassicPc ? 15 : (IsHybrid ? 20 : 22),
                GetCheckBoxTexture = () => _checkBoxTexture,
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0,
                TextColor = Color.Transparent,
                HoverTextColor = Color.Transparent,
                DisabledTextColor = Color.Transparent
            };
            button.Click += (_, _) =>
            {
                toggle();
                _controller.Config.Normalize();
                RefreshValues();
            };
            return button;
        }

        private void AddThresholdTrack(UIControl page, int x, int y, Func<int> getThreshold, Action<int> setThreshold)
        {
            const int segmentCount = 10;
            int gap = IsClassicPc ? 2 : 3;
            int segmentWidth = IsClassicPc ? 14 : 24;
            int segmentHeight = IsClassicPc ? 13 : 20;
            for (int i = 0; i < segmentCount; i++)
            {
                int segment = i;
                var button = new HelperThresholdSegmentButton(() => getThreshold() >= (segment + 1) * 10)
                {
                    X = x + i * (segmentWidth + gap),
                    Y = y,
                    ControlSize = new Point(segmentWidth, segmentHeight),
                    ViewSize = new Point(segmentWidth, segmentHeight),
                    AutoViewSize = false,
                    BackgroundColor = Color.Transparent,
                    HoverBackgroundColor = Color.Transparent,
                    PressedBackgroundColor = Color.Transparent,
                    BorderColor = Color.Transparent,
                    BorderThickness = 0
                };
                button.Click += (_, _) =>
                {
                    setThreshold((segment + 1) * 10);
                    _controller.Config.Normalize();
                    RefreshValues();
                };
                page.Controls.Add(button);
                _potionThresholdSegments.Add(button);
            }
        }

        private void ShowPotionSettings()
        {
            OpenPotionSettings();
            UpdatePageChildVisibility();
            RefreshValues();
        }

        private void OpenPotionSettings()
        {
            _showPotionSettings = false;
            if (_potionSettingsPage != null)
                _potionSettingsPage.Visible = false;

            _scene.OpenMuHelperPotionSettings();
        }

        private void HidePotionSettings()
        {
            _showPotionSettings = false;
            SetActiveTab(0);
            RefreshValues();
        }

        private void UpdateClassicHuntingControlVisibility()
        {
            foreach (GameControl control in _classicHuntingControls)
            {
                int pageIndex = Array.FindIndex(_pages, page => ReferenceEquals(page, control.Parent));
                if (pageIndex < 0)
                    continue;

                UIControl page = _pages[pageIndex];
                int top = control.Y + page.Offset.Y;
                int bottom = top + control.ViewSize.Y;
                control.Visible = IsClassicPc && !_showPotionSettings && page.Visible &&
                                  bottom > 0 && top < ContentHeight;
            }
        }

        private UIControl CreatePage()
        {
            int pageWidth = IsClassicPc ? 165 : WindowWidth - 56;
            return new HelperPageControl
            {
                X = IsClassicPc ? 12 : 18,
                Y = ContentTop,
                ControlSize = new Point(pageWidth, ContentHeight),
                ViewSize = new Point(pageWidth, ContentHeight),
                AutoViewSize = false,
                Interactive = false,
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0
            };
        }

        private void AddPotionToggleSettingRow(UIControl parent, int row)
        {
            int y = IsClassicPc ? 5 + row * RowHeight : 10 + row * RowHeight;
            int checkSize = IsClassicPc ? 18 : (IsHybrid ? 28 : 32);
            int checkX = IsClassicPc ? 86 : WindowWidth - checkSize - 28;
            float settingFontSize = IsClassicPc ? 6.8f : 9f;
            int settingMinimumWidth = IsClassicPc ? 30 : (IsHybrid ? 52 : 48);
            int settingMaximumWidth = IsClassicPc ? 165 - 120 : WindowWidth - 32;
            int settingWidth = GetButtonWidthForText("Setting", settingFontSize, settingMinimumWidth, settingMaximumWidth);
            int settingX = IsClassicPc ? 120 : checkX - 4 - settingWidth;
            AddLabel(parent, "Potion", 5, y + 1, checkX - 18, checkSize,
                IsClassicPc ? 7.2f : (IsHybrid ? 10 : 11), ModernHudTheme.TextWhite);

            var toggle = new HelperToggleButton(() => _controller.Config.UseHealPotion)
            {
                X = checkX,
                Y = y,
                ControlSize = new Point(checkSize, checkSize),
                ViewSize = new Point(checkSize, checkSize),
                AutoViewSize = false,
                GetCheckBoxTexture = () => _checkBoxTexture,
                LogicalCheckSize = IsClassicPc ? 15 : (IsHybrid ? 20 : 22),
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0,
                TextColor = Color.Transparent,
                HoverTextColor = Color.Transparent,
                DisabledTextColor = Color.Transparent
            };
            toggle.Click += (_, _) =>
            {
                _controller.Config.UseHealPotion = !_controller.Config.UseHealPotion;
                _controller.Config.Normalize();
                RefreshValues();
            };
            parent.Controls.Add(toggle);
            _boundButtons.Add((toggle, () => string.Empty));

            var settings = CreateButton("Setting", settingX, y - 1, settingWidth, IsClassicPc ? 19 : 28, ShowPotionSettings);
            settings.FontSize = settingFontSize;
            FitButtonWidthToText(settings, settingMinimumWidth, settingMaximumWidth);
            parent.Controls.Add(settings);
            _boundButtons.Add((settings, () => "Setting"));
        }

        private void AddValueRow(UIControl parent, string label, int columnX, int row, Func<string> text, Action action)
        {
            int y = IsClassicPc ? 5 + row * RowHeight : 10 + row * RowHeight;
            int labelWidth = IsClassicPc ? 119 : WindowWidth - columnX - (IsHybrid ? 112 : 100) - 38;
            int buttonWidth = IsClassicPc ? 38 : (IsHybrid ? 112 : 100);
            int buttonHeight = IsClassicPc ? 19 : (IsHybrid ? 28 : 30);
            int buttonX = IsClassicPc ? columnX + 120 : WindowWidth - buttonWidth - 28;
            LabelControl rowLabel = AddLabel(parent, label, columnX, y + 1, labelWidth, buttonHeight, IsClassicPc ? 7.2f : (IsHybrid ? 10 : 11), ModernHudTheme.TextWhite);
            var button = new HelperValueButton
            {
                Name = $"{label} Value",
                Text = string.Empty,
                X = buttonX,
                Y = y,
                ControlSize = new Point(buttonWidth, buttonHeight),
                ViewSize = new Point(buttonWidth, buttonHeight),
                AutoViewSize = false,
                GetValue = text,
                FontSize = IsClassicPc ? 7 : (IsHybrid ? 9 : 10),
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold,
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0
            };
            button.Click += (_, _) =>
            {
                action();
                _controller.Config.Normalize();
                RefreshValues();
            };
            parent.Controls.Add(button);
            _boundButtons.Add((button, text));
            _layoutRows.Add((parent, rowLabel, button, columnX, row, HelperRowKind.Value));
        }

        private void AddToggleRow(UIControl parent, string label, int columnX, int row, Func<bool> getValue, Action<bool> setValue)
        {
            int y = IsClassicPc ? 5 + row * RowHeight : 10 + row * RowHeight;
            int checkSize = IsClassicPc ? 18 : (IsHybrid ? 28 : 32);
            int checkX = IsClassicPc ? columnX + 138 : WindowWidth - checkSize - 28;
            int labelWidth = IsClassicPc ? 119 : checkX - columnX - 12;
            LabelControl rowLabel = AddLabel(parent, label, columnX, y + 1, labelWidth, checkSize, IsClassicPc ? 7.2f : (IsHybrid ? 10 : 11), ModernHudTheme.TextWhite);
            var button = new HelperToggleButton(getValue)
            {
                Name = $"{label} Toggle",
                X = checkX,
                Y = y,
                ControlSize = new Point(checkSize, checkSize),
                ViewSize = new Point(checkSize, checkSize),
                AutoViewSize = false,
                FontSize = IsClassicPc ? 6.5f : (IsHybrid ? 10 : 11),
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0,
                TextColor = Color.Transparent,
                HoverTextColor = Color.Transparent,
                DisabledTextColor = Color.Transparent,
                GetCheckBoxTexture = () => _checkBoxTexture,
                LogicalCheckSize = IsClassicPc ? 15 : (IsHybrid ? 20 : 22)
            };
            button.Click += (_, _) =>
            {
                setValue(!getValue());
                _controller.Config.Normalize();
                RefreshValues();
            };
            parent.Controls.Add(button);
            _boundButtons.Add((button, () => string.Empty));
            _layoutRows.Add((parent, rowLabel, button, columnX, row, HelperRowKind.Toggle));
        }

        private void AddSkillRow(UIControl parent, string label, int columnX, int row, Func<ushort> getSkillId, Action<ushort> setSkillId)
        {
            int y = IsClassicPc ? 5 + row * RowHeight : 10 + row * RowHeight;
            int buttonWidth = IsClassicPc ? 38 : (IsHybrid ? 148 : 132);
            int buttonHeight = IsClassicPc ? 19 : (IsHybrid ? 28 : 30);
            int buttonX = IsClassicPc ? columnX + 120 : WindowWidth - buttonWidth - 28;
            int labelWidth = IsClassicPc ? 119 : buttonX - columnX - 12;
            LabelControl rowLabel = AddLabel(parent, label, columnX, y + 1, labelWidth, buttonHeight, IsClassicPc ? 7.2f : (IsHybrid ? 10 : 11), ModernHudTheme.TextWhite);
            var button = new SkillSlotButton(getSkillId)
            {
                Name = $"{label} Skill",
                Text = string.Empty,
                X = buttonX,
                Y = y,
                ControlSize = new Point(buttonWidth, buttonHeight),
                ViewSize = new Point(buttonWidth, buttonHeight),
                AutoViewSize = false,
                FontSize = IsClassicPc ? 6.5f : 9,
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold,
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0
            };
            button.Click += (_, _) =>
            {
                ModernBottomHud hud = _scene.ModernHud;
                if (hud == null)
                {
                    _logger?.LogWarning("The learned-skill picker is unavailable; cannot assign MU Helper skill slot.");
                    return;
                }

                _pendingSkillAssignment = skill =>
                {
                    setSkillId(skill.SkillId);
                    RefreshValues();
                };
                hud.BeginSkillSelection(skill =>
                {
                    if (!Visible || _pendingSkillAssignment == null)
                        return false;

                    _pendingSkillAssignment(skill);
                    _pendingSkillAssignment = null;
                    return true;
                });
            };
            button.SetClearAction(() =>
            {
                setSkillId(0);
                _pendingSkillAssignment = null;
                RefreshValues();
            });
            parent.Controls.Add(button);
            _boundButtons.Add((button, () => GetSkillLabel(getSkillId())));
            _layoutRows.Add((parent, rowLabel, button, columnX, row, HelperRowKind.Skill));
        }

        private LabelControl AddLabel(UIControl parent, string text, int x, int y, int width, int height, float fontSize, Color color, bool bold = false)
        {
            var label = new LabelControl
            {
                Name = string.IsNullOrWhiteSpace(text) ? "Label" : text,
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(width, height),
                ViewSize = new Point(width, height),
                AutoViewSize = false,
                FontSize = fontSize,
                TextColor = color,
                IsBold = bold,
                HasShadow = true
            };
            parent.Controls.Add(label);
            return label;
        }

        private int GetButtonWidthForText(string text, float fontSize, int minimumWidth, int maximumWidth)
        {
            if (maximumWidth < minimumWidth)
                maximumWidth = minimumWidth;

            if (string.IsNullOrWhiteSpace(text))
                return minimumWidth;

            SpriteFont font = GraphicsManager.GetUiFont(fontSize, out float scale);
            float textWidth = font != null
                ? font.MeasureString(text).X * scale
                : text.Length * fontSize * 0.62f;
            return Math.Clamp((int)MathF.Ceiling(textWidth) + 10, minimumWidth, maximumWidth);
        }

        private void FitButtonWidthToText(HelperActionButton button, int minimumWidth, int maximumWidth)
        {
            if (button == null || string.IsNullOrWhiteSpace(button.Text))
                return;

            int width = GetButtonWidthForText(button.Text, button.FontSize, minimumWidth, maximumWidth);
            button.ControlSize = new Point(width, button.ControlSize.Y);
            button.ViewSize = button.ControlSize;
        }

        private HelperActionButton CreateButton(string text, int x, int y, int width, int height, Action action)
        {
            bool classic = UiThemeManager.CurrentId == UiThemeId.Classic;
            var button = new HelperActionButton
            {
                Name = string.IsNullOrWhiteSpace(text) ? "Action Button" : text,
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(width, height),
                ViewSize = new Point(width, height),
                AutoViewSize = false,
                FontSize = 9.2f,
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold,
                BackgroundColor = IsClassicPc ? Color.Transparent : (classic ? new Color(18, 26, 40, 245) : new Color(25, 30, 40, 245)),
                HoverBackgroundColor = IsClassicPc ? Color.Transparent : (classic ? new Color(78, 58, 28, 245) : new Color(55, 47, 31, 245)),
                PressedBackgroundColor = IsClassicPc ? Color.Transparent : (classic ? new Color(42, 32, 18, 250) : new Color(38, 34, 29, 250)),
                BorderColor = IsClassicPc ? Color.Transparent : ModernHudTheme.BorderInner,
                BorderThickness = IsClassicPc ? 0 : 1
            };
            button.Click += (_, _) => action();
            return button;
        }

        private HelperListScrollButton CreateExtraItemsScrollButton(bool pointsUp)
        {
            var button = new HelperListScrollButton(pointsUp)
            {
                Name = pointsUp ? "Extra Items Scroll Up" : "Extra Items Scroll Down",
                Text = string.Empty,
                AutoViewSize = false,
                ControlSize = new Point(IsClassicPc ? 15 : 24, IsClassicPc ? 15 : 24),
                ViewSize = new Point(IsClassicPc ? 15 : 24, IsClassicPc ? 15 : 24),
                FontSize = 1,
                Interactive = true,
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0,
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold
            };
            button.Click += (_, _) => ScrollExtraItemsList(pointsUp ? -1 : 1);
            return button;
        }

        private void ConfigureExtraItemsScrollButtons()
        {
            if (_scrollUpButton == null || _scrollDownButton == null || _extraItemsListLabel == null)
                return;

            int size = IsClassicPc ? 15 : 24;
            int x = _extraItemsListLabel.X + _extraItemsListLabel.ViewSize.X - size;
            _scrollUpButton.X = x;
            _scrollUpButton.Y = _extraItemsListLabel.Y + 2;
            _scrollUpButton.ControlSize = new Point(size, size);
            _scrollUpButton.ViewSize = _scrollUpButton.ControlSize;
            _scrollDownButton.X = x;
            _scrollDownButton.Y = _extraItemsListLabel.Y + _extraItemsListLabel.ViewSize.Y - size - 2;
            _scrollDownButton.ControlSize = new Point(size, size);
            _scrollDownButton.ViewSize = _scrollDownButton.ControlSize;
        }

        private int GetExtraItemsVisibleRows()
        {
            if (_extraItemsListLabel == null)
                return 1;

            int rowHeight = IsClassicPc ? 14 : 20;
            return Math.Max(1, _extraItemsListLabel.ViewSize.Y / rowHeight);
        }

        private void ScrollExtraItemsList(int amount)
        {
            List<string> items = _controller.Config.ExtraItems ?? new List<string>();
            int maximumIndex = Math.Max(0, items.Count - GetExtraItemsVisibleRows());
            int nextIndex = Math.Clamp(_extraItemsScrollIndex + amount, 0, maximumIndex);
            if (nextIndex == _extraItemsScrollIndex)
                return;

            _extraItemsScrollIndex = nextIndex;
            UpdateExtraItemsList();
        }

        private void UpdateExtraItemsScrollButtonVisibility()
        {
            if (_scrollUpButton == null || _scrollDownButton == null)
                return;

            List<string> items = _controller.Config.ExtraItems ?? new List<string>();
            int maximumIndex = Math.Max(0, items.Count - GetExtraItemsVisibleRows());
            UIControl page = _extraItemsPage;
            bool listVisible = page != null && _extraItemsListLabel != null && page.Visible &&
                _extraItemsListLabel.Y + page.Offset.Y < ContentHeight &&
                _extraItemsListLabel.Y + page.Offset.Y + _extraItemsListLabel.ViewSize.Y > 0;
            bool canScroll = Visible && !_showPotionSettings && _activeTab == 1 && listVisible && maximumIndex > 0;
            _scrollUpButton.Visible = canScroll && _extraItemsScrollIndex > 0;
            _scrollDownButton.Visible = canScroll && _extraItemsScrollIndex < maximumIndex;
        }

        private void SetActiveTab(int tabIndex)
        {
            _showPotionSettings = false;
            _activeTab = Math.Clamp(tabIndex, 0, _pages.Length - 1);
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].Visible = !_showPotionSettings && i == _activeTab;
                _pages[i].Offset = Point.Zero;
            }
            UpdatePageChildVisibility();
            UpdateClassicHuntingControlVisibility();
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                bool selected = i == _activeTab;
                bool classic = UiThemeManager.CurrentId == UiThemeId.Classic;
                _tabButtons[i].BackgroundColor = IsClassicPc ? Color.Transparent
                    : selected ? (classic ? new Color(72, 54, 24, 245) : ModernHudTheme.SlotSelected)
                    : (classic ? new Color(14, 20, 32, 245) : new Color(18, 22, 30, 245));
                _tabButtons[i].HoverBackgroundColor = IsClassicPc ? Color.Transparent : (classic ? new Color(96, 72, 32, 245) : new Color(55, 47, 31, 245));
                _tabButtons[i].BorderColor = IsClassicPc ? Color.Transparent : ModernHudTheme.BorderInner;
                _tabButtons[i].BorderThickness = IsClassicPc ? 0 : 1;
                _tabButtons[i].TextColor = selected ? ModernHudTheme.TextWhite : ModernHudTheme.TextGray;
            }
        }

        private void ScrollPage(int amount)
        {
            if (_showPotionSettings)
                return;

            var page = _pages[_activeTab];
            int minOffset = Math.Min(0, ContentHeight - GetPageContentHeight(page));
            int nextY = Math.Clamp(page.Offset.Y + amount, minOffset, 0);
            page.Offset = new Point(0, nextY);
            UpdatePageChildVisibility();
        }

        private void UpdatePageChildVisibility()
        {
            _potionSettingsPage.Visible = Visible && _showPotionSettings;
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].Visible = Visible && !_showPotionSettings && i == _activeTab;
            if (_potionSettingsBackButton != null)
                _potionSettingsBackButton.Visible = _showPotionSettings;
            foreach (HelperToggleButton toggle in _potionSettingsToggles)
                toggle.Visible = _showPotionSettings;
            SetPageChildrenVisibility(_potionSettingsPage, _showPotionSettings);

            UIControl page = _pages[_activeTab];
            if (_showPotionSettings)
            {
                foreach (GameControl child in page.Controls)
                    child.Visible = false;
            }
            else
            {
                int offsetY = page.Offset.Y;
                foreach (GameControl child in page.Controls)
                {
                    int top = child.Y + offsetY;
                    int bottom = top + child.ViewSize.Y;
                    child.Visible = bottom > 0 && top < ContentHeight;
                }
            }

            UpdateExtraItemsScrollButtonVisibility();
        }

        private static int GetPageContentHeight(UIControl page)
        {
            int bottom = 0;
            foreach (GameControl child in page.Controls)
                bottom = Math.Max(bottom, child.Y + child.ViewSize.Y);
            return bottom;
        }

        private void SetPageChildrenVisibility(UIControl page, bool visible)
        {
            foreach (GameControl child in page.Controls)
            {
                int top = child.Y + page.Offset.Y;
                int bottom = top + child.ViewSize.Y;
                child.Visible = visible && bottom > 0 && top < ContentHeight;
            }
        }

        private void RefreshValues()
        {
            UpdatePageChildVisibility();
            foreach (var binding in _boundButtons)
                binding.Button.Text = binding.Text();
            foreach (var binding in _boundLabels)
                binding.Label.Text = binding.Text();
            foreach (HelperThresholdSegmentButton segment in _potionThresholdSegments)
                segment.BackgroundColor = Color.Transparent;

            // _startButton.Text = IsClassicPc
            //     ? (_controller.IsActive ? "StopX" : "StartX")
            //     : (_controller.IsActive ? "STOP HELPER" : "START HELPER");
            // _startButton.Visible = !_showPotionSettings;
            foreach (ButtonControl tab in _tabButtons)
                tab.Visible = !_showPotionSettings;
            _resetButton.Text = "Initialization";
            _resetButton.Visible = true;
            _saveButton.Visible = true;
            SyncClassicNumericInput(_classicMaxSecondsAwayInput, _controller.Config.MaxSecondsAway);
            SyncClassicNumericInput(_classicActivation1DelayInput, _controller.Config.ActivationSkill1.DelaySeconds);
            SyncClassicNumericInput(_classicActivation2DelayInput, _controller.Config.ActivationSkill2.DelaySeconds);
            bool classic = UiThemeManager.CurrentId == UiThemeId.Classic;
            // _startButton.BackgroundColor = _controller.IsActive
            //     ? (classic ? new Color(104, 36, 46, 245) : new Color(86, 37, 39, 245))
            //     : (classic ? new Color(24, 58, 46, 245) : new Color(31, 65, 48, 245));
            UpdateExtraItemsList();
        }

        private TextBoxControl CreateClassicNumericInput(string name, int maxLength, int value)
        {
            return new TextBoxControl
            {
                Name = name,
                Interactive = true,
                AutoViewSize = false,
                ControlSize = new Point(28, 16),
                ViewSize = new Point(28, 16),
                MaxLength = maxLength,
                DigitsOnly = true,
                FontSize = 7,
                Padding = 3,
                TextColor = ModernHudTheme.TextWhite,
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0,
                FocusedBorderColor = ModernHudTheme.AccentBright,
                Text = value.ToString()
            };
        }

        private void ConfigureClassicNumericInput(TextBoxControl input, int maximum,
            Func<int> getValue, Action<int> setValue)
        {
            ((GameControl)input).Focus += (_, _) => input.Text = string.Empty;
            ((GameControl)input).Blur += (_, _) =>
                CommitClassicNumericInput(input, maximum, getValue, setValue);
            input.Click += (_, _) =>
            {
                _scene.FocusControlIfInteractive(input);
                input.OnFocus();
            };
            input.EnterKeyPressed += (_, _) =>
            {
                CommitClassicNumericInput(input, maximum, getValue, setValue);
                if (ReferenceEquals(_scene.FocusControl, input))
                    _scene.FocusControl = null;
            };
        }

        private void CommitClassicNumericInput(TextBoxControl input, int maximum,
            Func<int> getValue, Action<int> setValue)
        {
            if (input == null)
                return;

            string value = input.Text?.Trim() ?? string.Empty;
            string digitsOnly = new(value.Where(character => character >= '0' && character <= '9')
                .Take(input.MaxLength).ToArray());
            if (!int.TryParse(digitsOnly, out int parsed))
                parsed = getValue();

            setValue(Math.Clamp(parsed, 0, maximum));
            _controller.Config.Normalize();
            input.Text = getValue().ToString();
        }

        private static void SyncClassicNumericInput(TextBoxControl input, int configuredValue)
        {
            if (input == null || input.IsFocused)
                return;

            string configuredText = configuredValue.ToString();
            if (!string.Equals(input.Text, configuredText, StringComparison.Ordinal))
                input.Text = configuredText;
        }

        private void AddExtraItem()
        {
            if (_extraItemsBox == null)
                return;

            List<string> values = _extraItemsBox.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            if (values.Count == 0)
            {
                FocusExtraItemFilter();
                return;
            }

            List<string> items = _controller.Config.ExtraItems ?? new List<string>();
            foreach (string value in values)
            {
                if (!items.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
                    items.Add(value);
            }
            _controller.Config.ExtraItems = items;
            _controller.Config.Normalize();
            _controller.Save();
            _extraItemsScrollIndex = Math.Max(0, items.Count - GetExtraItemsVisibleRows());
            _extraItemsBox.Text = string.Empty;
            UpdateExtraItemsList();
            RefreshValues();
            _extraItemsBox.Text = string.Empty;
        }

        private void UpdateExtraItemsList()
        {
            if (_extraItemsListLabel == null || _extraItemsPage == null)
                return;

            if (!_visualDesignerEditing)
                RestoreClassicDockScale();
            foreach (ButtonControl row in _extraItemRowButtons)
            {
                _extraItemsPage.Controls.Remove(row);
                row.Dispose();
            }
            _extraItemRowButtons.Clear();

            List<string> items = _controller.Config.ExtraItems ?? new List<string>();
            _extraItemsListLabel.Text = items.Count == 0 ? "No item filters added." : string.Empty;
            int rowHeight = IsClassicPc ? 14 : 20;
            int listTop = _extraItemsListLabel.Y;
            int visibleRows = GetExtraItemsVisibleRows();
            _extraItemsScrollIndex = Math.Clamp(_extraItemsScrollIndex, 0, Math.Max(0, items.Count - visibleRows));
            int rowWidth = Math.Max(1, _extraItemsListLabel.ViewSize.X - (IsClassicPc ? 18 : 28));
            int endIndex = Math.Min(items.Count, _extraItemsScrollIndex + visibleRows);
            for (int index = _extraItemsScrollIndex; index < endIndex; index++)
            {
                int itemIndex = index;
                ButtonControl row = CreateButton(items[itemIndex], _extraItemsListLabel.X,
                    listTop + (itemIndex - _extraItemsScrollIndex) * rowHeight,
                    rowWidth, rowHeight, () => SelectExtraItem(itemIndex));
                row.Name = $"Extra Item {items[itemIndex]}";
                row.FontSize = IsClassicPc ? 6.2f : 8;
                row.TextColor = itemIndex == _selectedExtraItemIndex ? ModernHudTheme.TextGold : ModernHudTheme.TextWhite;
                row.HoverTextColor = ModernHudTheme.TextGold;
                row.BackgroundColor = itemIndex == _selectedExtraItemIndex
                    ? new Color(74, 57, 24, 240)
                    : new Color(8, 11, 16, 220);
                row.HoverBackgroundColor = new Color(62, 48, 24, 235);
                row.PressedBackgroundColor = new Color(45, 35, 20, 245);
                row.BorderColor = itemIndex == _selectedExtraItemIndex ? ModernHudTheme.AccentBright : ModernHudTheme.BorderInner;
                row.BorderThickness = 1;
                _extraItemsPage.Controls.Add(row);
                _extraItemRowButtons.Add(row);
            }

            _deleteExtraItemButton.Interactive = _selectedExtraItemIndex >= 0 &&
                                                  _selectedExtraItemIndex < items.Count;
            UpdateExtraItemsScrollButtonVisibility();
            if (_classicDockScaleReady && !_visualDesignerEditing)
                ApplyClassicDockScale();
        }

        private void SelectExtraItem(int index)
        {
            _selectedExtraItemIndex = index;
            UpdateExtraItemsList();
        }

        private void DeleteSelectedExtraItem()
        {
            List<string> items = _controller.Config.ExtraItems ?? new List<string>();
            if (_selectedExtraItemIndex < 0 || _selectedExtraItemIndex >= items.Count)
                return;

            items.RemoveAt(_selectedExtraItemIndex);
            _controller.Config.ExtraItems = items;
            _controller.Config.Normalize();
            _controller.Save();
            _selectedExtraItemIndex = Math.Min(_selectedExtraItemIndex, items.Count - 1);
            UpdateExtraItemsList();
            RefreshValues();
        }

        private void SaveSettings()
        {
            if (!_showPotionSettings)
                CommitExtraItemText();
            _controller.Save();
            RefreshValues();
        }

        private void ResetSettings()
        {
            if (_showPotionSettings)
            {
                _controller.Config.PotionThreshold = 0;
                _controller.Config.HealThreshold = 0;
                _controller.Config.Normalize();
                _controller.Save();
                RefreshValues();
                return;
            }

            _controller.Reset();
            _extraItemsBox.Text = string.Empty;
            _controller.Save();
            foreach (UIControl page in _pages)
                page.Offset = Point.Zero;
            SetActiveTab(_activeTab);
            RefreshValues();
        }

        private void CommitExtraItemText()
        {
            if (_extraItemsBox == null)
                return;

            List<string> items = _controller.Config.ExtraItems ?? new List<string>();
            foreach (string value in _extraItemsBox.Text
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                if (!items.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
                    items.Add(value);
            }
            _controller.Config.ExtraItems = items;
            _controller.Config.Normalize();
            _extraItemsBox.Text = string.Empty;
            UpdateExtraItemsList();
        }

        private string GetPotionSlotText()
        {
            return _controller.Config.PotionHotbarSlot switch
            {
                0 => "Q",
                1 => "W",
                2 => "E",
                _ => "Auto"
            };
        }

        private void CyclePotionSlot()
        {
            _controller.Config.PotionHotbarSlot = _controller.Config.PotionHotbarSlot switch
            {
                -1 => 0,
                0 => 1,
                1 => 2,
                _ => -1
            };
        }

        private static string GetSkillLabel(ushort skillId)
        {
            if (skillId == 0)
                return "Click to assign";
            string name = SkillDatabase.GetSkillName(skillId);
            return string.IsNullOrWhiteSpace(name) ? $"Skill {skillId}" : name;
        }

        private void FocusExtraItemFilter()
        {
            _extraItemsBox.Visible = true;
            _addExtraItemButton.Visible = true;
            _extraItemsListLabel.Visible = true;
            _extraItemsBox.BringToFront();
            _addExtraItemButton.BringToFront();
            _extraItemsListLabel.BringToFront();
            Scene.FocusControl = _extraItemsBox;
            _extraItemsBox.OnClick();
        }

        private void OnControllerStateChanged() => RefreshValues();

        private void SyncPresentation()
        {
            HelperPresentation current = ResolvePresentation();
            if (current == _presentation)
                return;

            RestoreClassicDockScale();
            _presentation = current;
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            foreach (var row in _layoutRows)
            {
                int y = IsClassicPc ? 5 + row.Row * RowHeight : 10 + row.Row * RowHeight;
                int buttonWidth = row.Kind switch
                {
                    HelperRowKind.Toggle => IsClassicPc ? 18 : (IsHybrid ? 28 : 32),
                    HelperRowKind.Skill => IsClassicPc ? 32 : (IsHybrid ? 148 : 132),
                    _ => IsClassicPc ? 38 : (IsHybrid ? 112 : 100)
                };
                int buttonHeight = row.Kind switch
                {
                    HelperRowKind.Toggle => IsClassicPc ? 16 : buttonWidth,
                    HelperRowKind.Value => IsClassicPc ? 19 : (IsHybrid ? 28 : 30),
                    _ => IsClassicPc ? 19 : (IsHybrid ? 28 : 30)
                };
                int buttonX = row.Kind == HelperRowKind.Toggle
                    ? (IsClassicPc ? row.X + 138 : WindowWidth - buttonWidth - 28)
                    : (IsClassicPc ? row.X + 120 : WindowWidth - buttonWidth - 28);
                int labelWidth = IsClassicPc ? 119 : buttonX - row.X - 12;
                row.Label.X = row.X;
                row.Label.Y = y + 1;
                row.Label.ControlSize = new Point(labelWidth, buttonHeight);
                row.Label.ViewSize = row.Label.ControlSize;
                row.Label.FontSize = IsClassicPc ? 7.2f : (IsHybrid ? 10 : 11);
                row.Button.X = buttonX;
                row.Button.Y = y;
                row.Button.ControlSize = new Point(buttonWidth, buttonHeight);
                row.Button.ViewSize = row.Button.ControlSize;
                row.Button.FontSize = row.Kind == HelperRowKind.Toggle
                    ? (IsClassicPc ? 6.5f : (IsHybrid ? 10 : 11))
                    : (IsClassicPc ? (row.Kind == HelperRowKind.Skill ? 6.5f : 7) : (row.Kind == HelperRowKind.Skill ? 9 : (IsHybrid ? 9 : 10)));
                if (row.Button is HelperToggleButton)
                    ((HelperToggleButton)row.Button).LogicalCheckSize = IsClassicPc ? 15 : 22;
                row.Label.Visible = !IsClassicPc;
            }
            ApplyInterfaceTheme();
            ApplyPresentationLayout();
            SetActiveTab(_activeTab);
            UpdateClassicHuntingControlVisibility();
            ApplyLoadedHelperTextures();
            Recenter();
            ApplyVisualDesignerLayout();
            if (_classicDockScaleReady)
                ApplyClassicDockScale();
        }

        private void ApplyPresentationLayout()
        {
            if (_potionSettingsPage != null)
            {
                _potionSettingsPage.ControlSize = new Point(IsClassicPc ? 165 : WindowWidth - 56, ContentHeight);
                _potionSettingsPage.ViewSize = _potionSettingsPage.ControlSize;
                _potionSettingsPage.X = IsClassicPc ? 12 : 18;
                _potionSettingsPage.Y = ContentTop;
            }

            if (_potionSettingsBackButton != null)
            {
                _potionSettingsBackButton.X = 5;
                _potionSettingsBackButton.Y = 5;
                _potionSettingsBackButton.ControlSize = new Point(IsClassicPc ? 44 : 80, IsClassicPc ? 22 : 30);
                _potionSettingsBackButton.ViewSize = _potionSettingsBackButton.ControlSize;
                _potionSettingsBackButton.FontSize = IsClassicPc ? 7 : 9;
            }

            if (_potionSettingsToggles != null)
            {
                foreach (HelperToggleButton toggle in _potionSettingsToggles)
                {
                    if (toggle == null)
                        continue;
                    toggle.X = IsClassicPc ? 140 : WindowWidth - 80;
                    toggle.ControlSize = new Point(IsClassicPc ? 18 : 28, IsClassicPc ? 18 : 28);
                    toggle.ViewSize = toggle.ControlSize;
                    toggle.LogicalCheckSize = IsClassicPc ? 15 : (IsHybrid ? 20 : 22);
                }
            }

            if (_potionThresholdSegments != null)
            {
                for (int i = 0; i < _potionThresholdSegments.Count; i++)
                {
                    var segment = _potionThresholdSegments[i];
                    if (segment == null)
                        continue;

                    int segmentIndex = i % 10;
                    int trackIndex = i / 10;
                    int segmentWidth = IsClassicPc ? 14 : 24;
                    int gap = IsClassicPc ? 2 : 3;
                    segment.X = (IsClassicPc ? 30 : 34) + segmentIndex * (segmentWidth + gap);
                    segment.Y = trackIndex == 0 ? (IsClassicPc ? 64 : 93) : (IsClassicPc ? 137 : 186);
                    segment.ControlSize = new Point(segmentWidth, IsClassicPc ? 13 : 20);
                    segment.ViewSize = segment.ControlSize;
                }
            }

            if (IsClassicPc)
            {
                ControlSize = new Point(ClassicPcWidth, ClassicPcHeight);
                ViewSize = ControlSize;

                if (_titleLabel != null)
                {
                    _titleLabel.X = 4;
                    _titleLabel.Y = 11;
                    _titleLabel.ControlSize = new Point(182, 20);
                    _titleLabel.ViewSize = _titleLabel.ControlSize;
                    _titleLabel.FontSize = 9;
                }

                // if (_inputHintLabel != null)
                //     _inputHintLabel.Visible = false;

                if (_classicMaxSecondsAwayInput != null)
                {
                    _classicMaxSecondsAwayInput.X = 134;
                    _classicMaxSecondsAwayInput.Y = 68;
                    _classicMaxSecondsAwayInput.ControlSize = new Point(28, 16);
                    _classicMaxSecondsAwayInput.ViewSize = _classicMaxSecondsAwayInput.ControlSize;
                    _classicMaxSecondsAwayInput.FontSize = 7;
                    _classicMaxSecondsAwayInput.TextColor = IsClassicPc ? Color.Black : ModernHudTheme.TextWhite;
                }
                if (_classicActivation1DelayInput != null)
                {
                    _classicActivation1DelayInput.X = 136;
                    _classicActivation1DelayInput.Y = 106;
                    _classicActivation1DelayInput.ControlSize = new Point(28, 16);
                    _classicActivation1DelayInput.ViewSize = _classicActivation1DelayInput.ControlSize;
                    _classicActivation1DelayInput.FontSize = 7;
                    _classicActivation1DelayInput.TextColor = IsClassicPc ? Color.Black : ModernHudTheme.TextWhite;
                }
                if (_classicActivation2DelayInput != null)
                {
                    _classicActivation2DelayInput.X = 134;
                    _classicActivation2DelayInput.Y = 159;
                    _classicActivation2DelayInput.ControlSize = new Point(28, 16);
                    _classicActivation2DelayInput.ViewSize = _classicActivation2DelayInput.ControlSize;
                    _classicActivation2DelayInput.FontSize = 7;
                    _classicActivation2DelayInput.TextColor = IsClassicPc ? Color.Black : ModernHudTheme.TextWhite;
                }

                if (_closeButton != null)
                {
                    _closeButton.X = 20;
                    _closeButton.Y = FooterTop;
                    _closeButton.ControlSize = new Point(36, 29);
                }

                if (_resetButton != null)
                {
                    _resetButton.X = 65;
                    _resetButton.Y = FooterTop;
                    _resetButton.ControlSize = new Point(52, 26);
                }

                if (_saveButton != null)
                {
                    _saveButton.X = 120;
                    _saveButton.Y = FooterTop;
                    _saveButton.ControlSize = new Point(52, 26);
                }

                if (_startButton != null)
                {
                    _startButton.X = 35;
                    _startButton.Y = FooterTop - 30;
                    _startButton.ControlSize = new Point(117, 26);
                }

                if (_tabButtons != null)
                {
                    for (int i = 0; i < _tabButtons.Length; i++)
                    {
                        var tab = _tabButtons[i];
                        if (tab == null)
                            continue;

                        tab.X = 10 + i * 57;
                        tab.Y = 48;
                        tab.ControlSize = new Point(56, 22);
                        tab.FontSize = i == 2 ? 6.4f : 7.2f;
                        tab.BackgroundColor = Color.Transparent;
                        tab.HoverBackgroundColor = Color.Transparent;
                        tab.PressedBackgroundColor = Color.Transparent;
                        tab.BorderColor = Color.Transparent;
                        tab.BorderThickness = 0;
                    }
                }

                if (_extraItemsBox != null)
                {
                    _extraItemsBox.X = 20;
                    _extraItemsBox.Y = 178;
                    _extraItemsBox.ControlSize = new Point(105, 22);
                    _extraItemsBox.ViewSize = _extraItemsBox.ControlSize;
                    _extraItemsBox.FontSize = 8;
                }
                if (_addExtraItemButton != null)
                {
                    _addExtraItemButton.X = 128;
                    _addExtraItemButton.Y = 178;
                    _addExtraItemButton.ControlSize = new Point(34, 22);
                    _addExtraItemButton.ViewSize = _addExtraItemButton.ControlSize;
                    _addExtraItemButton.FontSize = 6.5f;
                }
                if (_extraItemsListLabel != null)
                {
                    _extraItemsListLabel.X = 20;
                    _extraItemsListLabel.Y = 205;
                    _extraItemsListLabel.ControlSize = new Point(145, 90);
                    _extraItemsListLabel.ViewSize = _extraItemsListLabel.ControlSize;
                    _extraItemsListLabel.FontSize = 6.5f;
                }
                if (_deleteExtraItemButton != null)
                {
                    _deleteExtraItemButton.X = 116;
                    _deleteExtraItemButton.Y = 300;
                    _deleteExtraItemButton.ControlSize = new Point(48, 22);
                    _deleteExtraItemButton.ViewSize = _deleteExtraItemButton.ControlSize;
                    _deleteExtraItemButton.FontSize = 6.5f;
                }
            }
            else
            {
                ControlSize = new Point(WindowWidth, WindowHeight);
                ViewSize = ControlSize;

                if (_titleLabel != null)
                {
                    _titleLabel.X = 18;
                    _titleLabel.Y = 12;
                    _titleLabel.ControlSize = new Point(WindowWidth - 36, 28);
                    _titleLabel.ViewSize = _titleLabel.ControlSize;
                    _titleLabel.FontSize = 16;
                }

                // if (_inputHintLabel != null)
                //     _inputHintLabel.Visible = false;

                if (_tabButtons != null)
                {
                    int tabWidth = (WindowWidth - 36) / 3;
                    for (int i = 0; i < _tabButtons.Length; i++)
                    {
                        var tab = _tabButtons[i];
                        if (tab == null)
                            continue;

                        tab.X = 18 + i * tabWidth;
                        tab.Y = 56;
                        tab.ControlSize = new Point(tabWidth - 5, 34);
                        tab.FontSize = i == 2 ? 9 : 11;
                    }
                }

                if (_startButton != null)
                {
                    _startButton.X = 18;
                    _startButton.Y = FooterTop;
                    _startButton.ControlSize = new Point(110, 42);
                }

                if (_saveButton != null)
                {
                    _saveButton.X = 142;
                    _saveButton.Y = FooterTop;
                    _saveButton.ControlSize = new Point(95, 42);
                }

                if (_resetButton != null)
                {
                    _resetButton.X = 245;
                    _resetButton.Y = FooterTop;
                    _resetButton.ControlSize = new Point(95, 42);
                }

                if (_closeButton != null)
                {
                    _closeButton.X = WindowWidth - 62;
                    _closeButton.Y = FooterTop;
                    _closeButton.ControlSize = new Point(44, 42);
                }

                if (_extraItemsBox != null)
                {
                    _extraItemsBox.X = 5;
                    _extraItemsBox.Y = 10 + 9 * RowHeight + 20;
                    _extraItemsBox.ControlSize = new Point(WindowWidth - 130, 30);
                    _extraItemsBox.ViewSize = _extraItemsBox.ControlSize;
                    _extraItemsBox.FontSize = 10;
                }
                if (_addExtraItemButton != null)
                {
                    _addExtraItemButton.X = WindowWidth - 112;
                    _addExtraItemButton.Y = 10 + 9 * RowHeight + 20;
                    _addExtraItemButton.ControlSize = new Point(50, 30);
                    _addExtraItemButton.ViewSize = _addExtraItemButton.ControlSize;
                    _addExtraItemButton.FontSize = 8;
                }
                if (_extraItemsListLabel != null)
                {
                    _extraItemsListLabel.X = 5;
                    _extraItemsListLabel.Y = 10 + 9 * RowHeight + 54;
                    _extraItemsListLabel.ControlSize = new Point(WindowWidth - 68, 72);
                    _extraItemsListLabel.ViewSize = _extraItemsListLabel.ControlSize;
                    _extraItemsListLabel.FontSize = 8;
                }
                if (_deleteExtraItemButton != null)
                {
                    _deleteExtraItemButton.X = WindowWidth - 112;
                    _deleteExtraItemButton.Y = 10 + 9 * RowHeight + 130;
                    _deleteExtraItemButton.ControlSize = new Point(50, 30);
                    _deleteExtraItemButton.ViewSize = _deleteExtraItemButton.ControlSize;
                    _deleteExtraItemButton.FontSize = 8;
                }
            }

            if (IsClassicPc)
            {
                foreach (SkillSlotButton skillSlot in _classicHuntingControls.OfType<SkillSlotButton>())
                {
                    skillSlot.ControlSize = new Point(36, 36);
                    skillSlot.ViewSize = skillSlot.ControlSize;
                }
            }

            ConfigureExtraItemsScrollButtons();
            foreach (GameControl control in new GameControl[]
                    {
                        _startButton, _saveButton, _resetButton, _closeButton,
                        _scrollUpButton, _scrollDownButton, _addExtraItemButton, _deleteExtraItemButton
                    })
            {
                if (control == null)
                    continue;
                control.ViewSize = control.ControlSize;
            }

            Recenter();
        }

        private float GetClassicDockScale()
        {
            if (!IsClassicPc)
                return 1f;

            Point actualSize = UiScaler.ActualSize;
            if (actualSize.X <= 0 || actualSize.Y <= 0)
                return 1f;

            // MuMain's docked UI uses the 640x480 reference and a 2.25x ceiling.
            float nativeScale = Math.Clamp(Math.Min(actualSize.X / 640f, actualSize.Y / 480f), 1f, 2.25f);
            return nativeScale / Math.Max(UiScaler.Scale, 0.0001f);
        }

        private void RefreshClassicDockScaleIfNeeded()
        {
            if (!_visualDesignerEditing && _classicDockScaleReady &&
                MathF.Abs(GetClassicDockScale() - _appliedClassicDockScale) > 0.001f)
                ApplyClassicDockScale();
        }

        private void ApplyClassicDockScale()
        {
            if (_visualDesignerEditing)
                return;

            RestoreClassicDockScale();
            float scale = GetClassicDockScale();
            if (MathF.Abs(scale - 1f) > 0.001f)
                ApplyClassicDockScale(this, scale, isRoot: true);

            _appliedClassicDockScale = scale;
            Recenter();
        }

        private void ApplyClassicDockScale(GameControl control, float scale, bool isRoot = false)
        {
            float? fontSize = GetControlFontSize(control);
            int? padding = control is TextBoxControl textBox ? textBox.Padding : null;
            int? border = control is TextBoxControl borderTextBox ? borderTextBox.BorderThickness : null;
            _classicDockControlStates[control] = new ClassicDockControlState(
                control.X, control.Y, control.Offset, control.Scale, fontSize, padding, border);

            if (!isRoot)
            {
                control.X = (int)MathF.Round(control.X * scale);
                control.Y = (int)MathF.Round(control.Y * scale);
                control.Offset = new Point(
                    (int)MathF.Round(control.Offset.X * scale),
                    (int)MathF.Round(control.Offset.Y * scale));
            }

            control.Scale *= scale;
            if (fontSize.HasValue)
                SetControlFontSize(control, fontSize.Value * scale);
            if (control is TextBoxControl scaledTextBox)
            {
                scaledTextBox.Padding = Math.Max(1, (int)MathF.Round(scaledTextBox.Padding * scale));
                scaledTextBox.BorderThickness = Math.Max(1, (int)MathF.Round(scaledTextBox.BorderThickness * scale));
            }

            foreach (GameControl child in control.Controls)
                ApplyClassicDockScale(child, scale);
        }

        private void RestoreClassicDockScale()
        {
            foreach ((GameControl control, ClassicDockControlState state) in _classicDockControlStates)
            {
                control.X = state.X;
                control.Y = state.Y;
                control.Offset = state.Offset;
                control.Scale = state.Scale;
                if (state.FontSize.HasValue)
                    SetControlFontSize(control, state.FontSize.Value);
                if (control is TextBoxControl textBox)
                {
                    if (state.TextBoxPadding.HasValue)
                        textBox.Padding = state.TextBoxPadding.Value;
                    if (state.TextBoxBorderThickness.HasValue)
                        textBox.BorderThickness = state.TextBoxBorderThickness.Value;
                }
            }
            _classicDockControlStates.Clear();
        }

        private static float? GetControlFontSize(GameControl control) => control switch
        {
            LabelControl label => label.FontSize,
            ButtonControl button => button.FontSize,
            TextBoxControl textBox => textBox.FontSize,
            _ => null
        };

        private static void SetControlFontSize(GameControl control, float fontSize)
        {
            switch (control)
            {
                case LabelControl label:
                    label.FontSize = fontSize;
                    break;
                case ButtonControl button:
                    button.FontSize = fontSize;
                    break;
                case TextBoxControl textBox:
                    textBox.FontSize = fontSize;
                    break;
            }
        }

        private void Recenter()
        {
            // The helper is temporarily parented to the designer canvas, so runtime screen
            // centering would move it out of the canvas's local coordinate space.
            if (_visualDesignerEditing)
                return;

            Point virtualSize = UiScaler.VirtualSize;
            if (IsClassicPc && UiScaler.ScaleX > 0f && UiScaler.ScaleY > 0f)
            {
                Point actualSize = UiScaler.ActualSize;
                float nativeDockScale = Math.Clamp(Math.Min(actualSize.X / 640f, actualSize.Y / 480f), 1f, 2.25f);
                float nativeHudScale = Math.Clamp(Math.Min(actualSize.X / 640f, actualSize.Y / 480f), 1f, 2f);
                float physicalX = actualSize.X - ClassicPcWidth * nativeDockScale;
                float physicalY = actualSize.Y - MathF.Round(51f * nativeHudScale) - 432f * nativeDockScale;
                X = Math.Max(0, (int)MathF.Round((physicalX - UiScaler.Offset.X) / UiScaler.ScaleX));
                Y = Math.Max(0, (int)MathF.Round((physicalY - UiScaler.Offset.Y) / UiScaler.ScaleY));
                return;
            }

            Point displaySize = DisplaySize;
            X = Math.Max(0, (virtualSize.X - displaySize.X) / 2);
            Y = Math.Max(10, (virtualSize.Y - displaySize.Y) / 2);
        }

        private bool IsFocusedDescendant(GameControl focused)
        {
            for (GameControl control = focused; control != null; control = control.Parent)
            {
                if (ReferenceEquals(control, this))
                    return true;
            }
            return false;
        }

        private sealed class HelperListScrollButton : ButtonControl
        {
            private readonly bool _pointsUp;

            public HelperListScrollButton(bool pointsUp)
            {
                _pointsUp = pointsUp;
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready)
                    return;

                base.Draw(gameTime);
                Texture2D pixel = GraphicsManager.Instance.Pixel;
                if (pixel == null)
                    return;

                Rectangle bounds = DisplayRectangle;
                float centerX = bounds.Center.X;
                float centerY = bounds.Center.Y;
                float arm = Math.Max(3f, Math.Min(bounds.Width, bounds.Height) * 0.28f);
                float tipY = centerY + (_pointsUp ? -arm * 0.45f : arm * 0.45f);
                float baseY = centerY - (_pointsUp ? -arm * 0.45f : arm * 0.45f);
                Color color = (IsMouseOver ? HoverTextColor : TextColor) * Alpha;

                DrawChevronLine(pixel, new Vector2(centerX - arm, baseY), new Vector2(centerX, tipY), color);
                DrawChevronLine(pixel, new Vector2(centerX, tipY), new Vector2(centerX + arm, baseY), color);
            }

            private static void DrawChevronLine(Texture2D pixel, Vector2 start, Vector2 end, Color color)
            {
                SpriteBatch sprite = GraphicsManager.Instance.Sprite;
                Vector2 delta = end - start;
                float length = delta.Length();
                float rotation = MathF.Atan2(delta.Y, delta.X);
                sprite.Draw(pixel,
                    new Rectangle((int)MathF.Round(start.X), (int)MathF.Round(start.Y), Math.Max(1, (int)MathF.Ceiling(length)), 2),
                    null, color, rotation, Vector2.Zero, SpriteEffects.None, 0f);
            }
        }

        private sealed class HelperActionButton : ButtonControl
        {
            public bool FlipTextureHorizontally { get; set; }
            public bool DrawRangeFallback { get; set; }
            public bool UseTopHalfTexture { get; set; }
            public bool UseThreeStateButtonTexture { get; private set; }

            public new void SetTexture(Texture2D texture)
            {
                Texture = texture;
                UseThreeStateButtonTexture = false;
            }

            public void SetThreeStateButtonTexture(Texture2D texture)
            {
                Texture = texture;
                UseThreeStateButtonTexture = texture != null;
            }

            private int GetButtonState(GameTime gameTime)
            {
                if (IsMousePressed)
                    return 2;
                if (IsMouseOver)
                    return 1;
                return 0;
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready)
                    return;

                Texture2D texture = Texture;
                if (texture != null)
                {
                    // These assets are atlases. Select only the state being displayed instead
                    // of stretching the complete texture over the button rectangle.
                    Rectangle? source = UseThreeStateButtonTexture
                        ? new Rectangle(0, GetButtonState(gameTime) * 26, Math.Min(52, texture.Width), Math.Min(26, texture.Height))
                        : UseTopHalfTexture
                            ? new Rectangle(0, 0, Math.Min(36, texture.Width), Math.Min(29, texture.Height))
                            : DrawRangeFallback && texture.Height >= 30
                                ? new Rectangle(0, 0, Math.Min(16, texture.Width), 15)
                                : null;
                    if (FlipTextureHorizontally)
                    {
                        GraphicsManager.Instance.Sprite.Draw(texture, DisplayRectangle, source, Color.White * Alpha,
                            0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                    }
                    else
                    {
                        GraphicsManager.Instance.Sprite.Draw(texture, DisplayRectangle, source, Color.White * Alpha);
                    }
                }
                else if (DrawRangeFallback)
                {
                    SpriteBatch sprite = GraphicsManager.Instance.Sprite;
                    Texture2D pixel = GraphicsManager.Instance.Pixel;
                    if (pixel == null)
                        return;
                    Rectangle rect = DisplayRectangle;
                    Color color = new Color(232, 166, 45, 255) * Alpha;
                    int centerX = rect.Center.X;
                    int centerY = rect.Center.Y;
                    sprite.Draw(pixel, new Rectangle(centerX - 5, centerY - 1, 10, 2), color);
                    if (!FlipTextureHorizontally)
                        sprite.Draw(pixel, new Rectangle(centerX - 1, centerY - 5, 2, 10), color);
                }
                else
                    base.Draw(gameTime);

                SpriteFont font = GraphicsManager.GetUiFont(FontSize, out float scale);
                if (font == null || string.IsNullOrEmpty(Text))
                    return;

                Vector2 textSize = font.MeasureString(Text) * scale;
                Vector2 position = new(
                    DisplayRectangle.Center.X - textSize.X / 2f,
                    DisplayRectangle.Center.Y - textSize.Y / 2f);
                GraphicsManager.Instance.Sprite.DrawString(font, Text, position + Vector2.One, Color.Black * 0.65f * Alpha,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                GraphicsManager.Instance.Sprite.DrawString(font, Text, position,
                    (IsMouseOver ? HoverTextColor : TextColor) * Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private sealed class HelperValueButton : ButtonControl
        {
            public Func<string> GetValue { get; set; }

            public new void SetTexture(Texture2D texture)
            {
                Texture = texture;

                if (texture != null)
                {
                    TextureRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                }
                else
                {
                    TextureRectangle = Rectangle.Empty;
                }
            }


            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready)
                    return;

                base.Draw(gameTime);
                string value = GetValue?.Invoke();
                SpriteFont font = GraphicsManager.GetUiFont(FontSize, out float scale);
                if (font == null || string.IsNullOrEmpty(value))
                    return;

                Vector2 size = font.MeasureString(value) * scale;
                GraphicsManager.Instance.Sprite.DrawString(font, value,
                    new Vector2(DisplayRectangle.Center.X - size.X / 2f, DisplayRectangle.Center.Y - size.Y / 2f),
                    TextColor * Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private sealed class SkillSlotButton : ButtonControl
        {
            private static readonly ILogger _logger = MuGame.AppLoggerFactory?.CreateLogger<SkillSlotButton>();
            private readonly Func<ushort> _getSkillId;
            private Action _clearSkill;
            private bool _loggedClassicGeometry;

            public bool UseClassicIconLayout { get; set; }
            public Func<Texture2D> GetSkillFrame { get; set; }
            public Func<Texture2D> GetActiveSkillFrame { get; set; }
            public new void SetTexture(Texture2D texture)
            {
                Texture = texture;

                if (texture != null)
                {
                    TextureRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                }
                else
                {
                    TextureRectangle = Rectangle.Empty;
                }
            }

            private bool _rightPressed;
            private bool _rightPressedInside;

            public SkillSlotButton(Func<ushort> getSkillId)
            {
                _getSkillId = getSkillId;
            }

            public void SetClearAction(Action clearSkill)
            {
                _clearSkill = clearSkill;
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (!Visible || !Interactive || Status != GameControlStatus.Ready)
                    return;

                MouseState mouse = MuGame.Instance.UiMouseState;
                MouseState previousMouse = MuGame.Instance.PrevUiMouseState;
                if (IsMouseOver && mouse.RightButton == ButtonState.Pressed && previousMouse.RightButton == ButtonState.Released)
                    _rightPressedInside = true;

                if (mouse.RightButton == ButtonState.Pressed)
                    _rightPressed = true;
                else if (_rightPressed)
                {
                    if (_rightPressedInside && IsMouseOver)
                    {
                        _clearSkill?.Invoke();
                        Scene?.SetMouseInputConsumed();
                    }
                    _rightPressed = false;
                    _rightPressedInside = false;
                }
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready)
                    return;

                ushort skillId = _getSkillId();
                Texture2D skillFrame = skillId == 0 ? GetSkillFrame?.Invoke() : GetActiveSkillFrame?.Invoke();
                if (skillFrame != null)
                    GraphicsManager.Instance.Sprite.Draw(skillFrame, DisplayRectangle, Color.White * Alpha);
                else
                    base.Draw(gameTime);

                var rect = DisplayRectangle;
                bool classicSlot = UseClassicIconLayout || rect.Width <= 40;
                Rectangle iconRect = classicSlot
                    ? new Rectangle(rect.X + 1, rect.Y + 1, Math.Max(1, rect.Width - 2), Math.Max(1, rect.Height - 2))
                    : new Rectangle(rect.X + 5, rect.Y + 1, 14, 16);
                if (UseClassicIconLayout && !_loggedClassicGeometry)
                {
                    _logger?.LogInformation(
                        "MU Helper classic skill slot {Name}: skillId={SkillId}, display={DisplayWidth}x{DisplayHeight}, icon={IconWidth}x{IconHeight}, control={ControlWidth}x{ControlHeight}",
                        Name, skillId, rect.Width, rect.Height, iconRect.Width, iconRect.Height, ControlSize.X, ControlSize.Y);
                    _loggedClassicGeometry = true;
                }
                if (skillId == 0)
                    return;

                if (!Client.Main.Controls.UI.Game.Skills.SkillIconRenderer.DrawSkillRect(
                        GraphicsManager.Instance.Sprite, skillId, iconRect, Color.White * Alpha) || classicSlot)
                    return;

                SpriteFont font = GraphicsManager.GetUiFont(6.2f, out float scale);
                if (font == null)
                    return;
                string name = SkillDatabase.GetSkillName(skillId);
                if (string.IsNullOrWhiteSpace(name))
                    name = skillId.ToString();
                if (font.MeasureString(name).X * scale > rect.Width - 20)
                    name = "...";
                GraphicsManager.Instance.Sprite.DrawString(font, name,
                    new Vector2(rect.X + 20, rect.Y + (rect.Height - font.LineSpacing * scale) / 2f),
                    ModernHudTheme.TextWhite * Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private sealed class HelperToggleButton : ButtonControl
        {
            private readonly Func<bool> _isChecked;

            public Func<Texture2D> GetCheckBoxTexture { get; set; }

            public int LogicalCheckSize { get; set; } = 15;

            public HelperToggleButton(Func<bool> isChecked)
            {
                _isChecked = isChecked ?? throw new ArgumentNullException(nameof(isChecked));
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready)
                    return;

                base.Draw(gameTime);
                Texture2D checkBoxTexture = GetCheckBoxTexture?.Invoke();
                if (checkBoxTexture == null || checkBoxTexture.Width < 15 || checkBoxTexture.Height < 30)
                    return;

                // MuMain's newui_option_check.OZT is a 15x30 atlas: checked
                // in the first 15px row and unchecked in the second.
                const int sourceSize = 15;
                Rectangle bounds = DisplayRectangle;
                int logicalSize = Math.Clamp(LogicalCheckSize, 1, 32);
                int controlSize = Math.Max(1, Math.Min(ControlSize.X, ControlSize.Y));
                int size = Math.Max(1, (int)MathF.Round(Math.Min(bounds.Width, bounds.Height) * logicalSize / (float)controlSize));
                int x = bounds.X + (bounds.Width - size) / 2;
                int y = bounds.Y + (bounds.Height - size) / 2;
                int sourceY = _isChecked() ? 0 : sourceSize;
                GraphicsManager.Instance.Sprite.Draw(
                    checkBoxTexture,
                    new Rectangle(x, y, size, size),
                    new Rectangle(0, sourceY, sourceSize, sourceSize),
                    Color.White * Alpha);
            }
        }

        private sealed class HelperThresholdSegmentButton : ButtonControl
        {
            private readonly Func<bool> _isFilled;

            public Func<Texture2D> GetSegmentTexture { get; set; }

            public HelperThresholdSegmentButton(Func<bool> isFilled)
            {
                _isFilled = isFilled ?? throw new ArgumentNullException(nameof(isFilled));
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready)
                    return;

                Texture2D texture = GetSegmentTexture?.Invoke();
                Rectangle bounds = DisplayRectangle;
                if (texture == null || texture.Width < 160 || texture.Height < 16)
                {
                    Texture2D pixel = GraphicsManager.Instance.Pixel;
                    Color color = _isFilled() ? new Color(250, 235, 95) : new Color(75, 69, 57);
                    GraphicsManager.Instance.Sprite.Draw(pixel, bounds, color * Alpha);
                    return;
                }

                // MacroUI_InputString is the small nine-state tile sheet also
                // used by MuMain's threshold meter: normal/hover/pressed × filled/empty.
                int state = _isFilled() ? 1 : 6;
                if (IsMouseOver)
                    state++;
                if (IsMousePressed)
                    state++;
                int sourceX = state * 16;
                GraphicsManager.Instance.Sprite.Draw(texture, bounds, new Rectangle(sourceX, 0, 16, 16), Color.White * Alpha);
            }
        }

        private sealed class HelperPageControl : UIControl
        {
        }

        private static int NextValue(int value, int min, int max, int step)
        {
            int next = value + step;
            return next > max ? min : next;
        }
    }
}