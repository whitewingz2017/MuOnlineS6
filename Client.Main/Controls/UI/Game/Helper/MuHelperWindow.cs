using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Configuration;
using Client.Main.Controls;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Helper
{
    /// <summary>
    /// Separate, client-local Helper settings window with Hunting, Obtaining, and Party tabs.
    /// Settings are edited in the shared MuHelperConfig and persisted with Save Setting.
    /// </summary>
    internal sealed class MuHelperWindow : UIControl
    {
        private const int WindowWidth = 620;
        private const int WindowHeight = 610;
        private const int RowHeight = 29;

        private readonly GameScene _scene;
        private readonly MuHelperController _controller;
        private readonly ILogger _logger;
        private readonly List<(ButtonControl Button, Func<string> Text)> _boundButtons = new();
        private readonly UIControl[] _pages = new UIControl[3];
        private readonly ButtonControl[] _tabButtons = new ButtonControl[3];
        private readonly ButtonControl _startButton;
        private readonly TextBoxControl _extraItemsBox;
        private int _activeTab;

        public MuHelperWindow(GameScene scene, MuHelperController controller, ILogger logger)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _logger = logger;

            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            BackgroundColor = new Color(12, 15, 22, 248);
            BorderColor = ModernHudTheme.BorderOuter;
            BorderThickness = 2;
            Recenter();

            AddLabel(this, "MU HELPER", 20, 12, 250, 28, 16, ModernHudTheme.TextGold, bold: true);
            AddLabel(this, "Client-side automation settings", 22, 37, 330, 20, 9, ModernHudTheme.TextGray);

            string[] tabNames = { "Hunting", "Obtaining", "Party" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int tabIndex = i;
                _tabButtons[i] = CreateButton(tabNames[i], 18 + i * 130, 68, 122, 30, () => SetActiveTab(tabIndex));
                Controls.Add(_tabButtons[i]);
            }

            _pages[0] = CreatePage();
            _pages[1] = CreatePage();
            _pages[2] = CreatePage();
            foreach (var page in _pages)
                Controls.Add(page);

            _extraItemsBox = new TextBoxControl
            {
                X = 18,
                Y = 306,
                ControlSize = new Point(545, 29),
                ViewSize = new Point(545, 29),
                MaxLength = 200,
                FontSize = 10,
                PlaceholderText = "Comma-separated item name fragments",
                BackgroundColor = new Color(22, 26, 35, 245),
                BorderColor = ModernHudTheme.BorderInner,
                FocusedBorderColor = ModernHudTheme.AccentBright,
                TextColor = ModernHudTheme.TextWhite
            };

            BuildHuntingPage(_pages[0]);
            BuildObtainingPage(_pages[1]);
            BuildPartyPage(_pages[2]);

            AddLabel(this,
                "Manual input: ground LMB still moves; monster clicks and RMB skill casts are ignored while Helper is active.",
                20, 548, 580, 24, 8.5f, ModernHudTheme.TextGray);

            _startButton = CreateButton("Start Helper", 20, 574, 135, 27, _controller.Toggle);
            Controls.Add(_startButton);
            var saveButton = CreateButton("Save Setting", 165, 574, 135, 27, SaveSettings);
            Controls.Add(saveButton);
            var resetButton = CreateButton("Initialization", 310, 574, 135, 27, ResetSettings);
            Controls.Add(resetButton);
            var closeButton = CreateButton("Close", 455, 574, 135, 27, Close);
            Controls.Add(closeButton);

            _controller.StateChanged += OnControllerStateChanged;
            SetActiveTab(0);
            RefreshValues();
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
            Visible = true;
            BringToFront();
            Scene.FocusControl = this;
            _extraItemsBox.Text = string.Join(", ", _controller.Config.ExtraItems ?? new List<string>());
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

            base.Update(gameTime);
            RefreshValues();

            var mouse = MuGame.Instance.UiMouseState;
            if (IsMouseOver && (mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed))
                Scene?.SetMouseInputConsumed();
        }

        public override void Dispose()
        {
            _controller.StateChanged -= OnControllerStateChanged;
            base.Dispose();
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            Recenter();
        }

        private void BuildHuntingPage(UIControl page)
        {
            int left = 0;
            int right = 286;
            int row = 0;
            AddValueRow(page, "Hunting range", left, row++, () => $"{_controller.Config.HuntingRange} tiles", () => _controller.Config.HuntingRange = NextValue(_controller.Config.HuntingRange, 0, 15, 1));
            AddToggleRow(page, "Long-range counterattack", left, row++, () => _controller.Config.LongRangeCounterAttack, value => _controller.Config.LongRangeCounterAttack = value);
            AddToggleRow(page, "Return to original position", left, row++, () => _controller.Config.ReturnToOriginalPosition, value => _controller.Config.ReturnToOriginalPosition = value);
            AddValueRow(page, "Max. seconds away", left, row++, () => _controller.Config.MaxSecondsAway.ToString(), () => _controller.Config.MaxSecondsAway = NextValue(_controller.Config.MaxSecondsAway, 0, 120, 5));
            AddToggleRow(page, "Use healing potion", left, row++, () => _controller.Config.UseHealPotion, value => _controller.Config.UseHealPotion = value);
            AddValueRow(page, "Potion HP threshold", left, row++, () => $"{_controller.Config.PotionThreshold}%", () => _controller.Config.PotionThreshold = NextValue(_controller.Config.PotionThreshold, 0, 100, 5));
            AddValueRow(page, "Healing potion slot", left, row++, GetPotionSlotText, CyclePotionSlot);
            AddToggleRow(page, "Auto heal skill", left, row++, () => _controller.Config.AutoHeal, value => _controller.Config.AutoHeal = value);
            AddValueRow(page, "Heal skill threshold", left, row++, () => $"{_controller.Config.HealThreshold}%", () => _controller.Config.HealThreshold = NextValue(_controller.Config.HealThreshold, 0, 100, 5));
            AddToggleRow(page, "Use Drain Life", left, row++, () => _controller.Config.UseDrainLife, value => _controller.Config.UseDrainLife = value);
            AddToggleRow(page, "Repair equipment", left, row++, () => _controller.Config.RepairItem, value => _controller.Config.RepairItem = value);

            row = 0;
            AddSkillRow(page, "Basic attack skill", right, row++, () => _controller.Config.BasicSkillId, id => _controller.Config.BasicSkillId = id);
            AddSkillRow(page, "Activation skill 1", right, row++, () => _controller.Config.ActivationSkill1.SkillId, id => _controller.Config.ActivationSkill1.SkillId = id);
            AddToggleRow(page, "Activation 1: timer", right, row++, () => _controller.Config.ActivationSkill1.UseTimer, value => _controller.Config.ActivationSkill1.UseTimer = value);
            AddToggleRow(page, "Activation 1: condition", right, row++, () => _controller.Config.ActivationSkill1.UseCondition, value => _controller.Config.ActivationSkill1.UseCondition = value);
            AddValueRow(page, "Activation 1 delay", right, row++, () => $"{_controller.Config.ActivationSkill1.DelaySeconds}s", () => _controller.Config.ActivationSkill1.DelaySeconds = NextValue(_controller.Config.ActivationSkill1.DelaySeconds, 0, 60, 5));
            AddSkillRow(page, "Activation skill 2", right, row++, () => _controller.Config.ActivationSkill2.SkillId, id => _controller.Config.ActivationSkill2.SkillId = id);
            AddToggleRow(page, "Activation 2: timer", right, row++, () => _controller.Config.ActivationSkill2.UseTimer, value => _controller.Config.ActivationSkill2.UseTimer = value);
            AddToggleRow(page, "Activation 2: condition", right, row++, () => _controller.Config.ActivationSkill2.UseCondition, value => _controller.Config.ActivationSkill2.UseCondition = value);
            AddValueRow(page, "Activation 2 delay", right, row++, () => $"{_controller.Config.ActivationSkill2.DelaySeconds}s", () => _controller.Config.ActivationSkill2.DelaySeconds = NextValue(_controller.Config.ActivationSkill2.DelaySeconds, 0, 60, 5));
            AddSkillRow(page, "Buff slot 1", right, row++, () => _controller.Config.BuffSkillIds[0], id => _controller.Config.BuffSkillIds[0] = id);
            AddSkillRow(page, "Buff slot 2", right, row++, () => _controller.Config.BuffSkillIds[1], id => _controller.Config.BuffSkillIds[1] = id);
            AddSkillRow(page, "Buff slot 3", right, row++, () => _controller.Config.BuffSkillIds[2], id => _controller.Config.BuffSkillIds[2] = id);
            AddToggleRow(page, "Fallback basic attack", right, row++, () => _controller.Config.FallbackBasicAttack, value => _controller.Config.FallbackBasicAttack = value);
            AddToggleRow(page, "Use combo skills", right, row++, () => _controller.Config.UseCombo, value => _controller.Config.UseCombo = value);
            AddToggleRow(page, "Use Dark Raven", right, row++, () => _controller.Config.UseDarkRaven, value => _controller.Config.UseDarkRaven = value);
        }

        private void BuildObtainingPage(UIControl page)
        {
            int left = 0;
            int right = 286;
            AddValueRow(page, "Obtaining range", left, 0, () => $"{_controller.Config.ObtainingRange} tiles", () => _controller.Config.ObtainingRange = NextValue(_controller.Config.ObtainingRange, 0, 15, 1));
            AddToggleRow(page, "Pick Zen", left, 1, () => _controller.Config.PickZen, value => _controller.Config.PickZen = value);
            AddToggleRow(page, "Pick all items", left, 2, () => _controller.Config.PickAllItems, value => _controller.Config.PickAllItems = value);
            AddToggleRow(page, "Pick selected items", left, 3, () => _controller.Config.PickSelectedItems, value => _controller.Config.PickSelectedItems = value);
            AddToggleRow(page, "Pick jewels", left, 4, () => _controller.Config.PickJewel, value => _controller.Config.PickJewel = value);
            AddToggleRow(page, "Pick Ancient", left, 5, () => _controller.Config.PickAncient, value => _controller.Config.PickAncient = value);
            AddToggleRow(page, "Pick Excellent", left, 6, () => _controller.Config.PickExcellent, value => _controller.Config.PickExcellent = value);
            AddToggleRow(page, "Use extra name filters", left, 7, () => _controller.Config.PickExtraItems, value => _controller.Config.PickExtraItems = value);
            AddValueRow(page, "Filter entries", left, 8, () => (_controller.Config.ExtraItems?.Count ?? 0).ToString(), FocusExtraItemFilter);
            AddLabel(page, "Enter comma-separated item name fragments below:", 0, 276, 560, 22, 9, ModernHudTheme.TextGray);
            page.Controls.Add(_extraItemsBox);

            AddToggleRow(page, "Return to original position", right, 0, () => _controller.Config.ReturnToOriginalPosition, value => _controller.Config.ReturnToOriginalPosition = value);
            AddValueRow(page, "Maximum away time", right, 1, () => $"{_controller.Config.MaxSecondsAway}s", () => _controller.Config.MaxSecondsAway = NextValue(_controller.Config.MaxSecondsAway, 0, 120, 5));
            AddLabel(page, "Pickup uses live nearby scope items and the existing client request path.", right, 100, 275, 42, 8.5f, ModernHudTheme.TextGray);
        }

        private void BuildPartyPage(UIControl page)
        {
            AddToggleRow(page, "Support party members", 0, 0, () => _controller.Config.SupportParty, value => _controller.Config.SupportParty = value);
            AddToggleRow(page, "Heal party members", 0, 1, () => _controller.Config.AutoHealParty, value => _controller.Config.AutoHealParty = value);
            AddValueRow(page, "Party heal threshold", 0, 2, () => $"{_controller.Config.HealPartyThreshold}%", () => _controller.Config.HealPartyThreshold = NextValue(_controller.Config.HealPartyThreshold, 0, 100, 5));
            AddToggleRow(page, "Maintain party buffs", 0, 3, () => _controller.Config.BuffDurationParty, value => _controller.Config.BuffDurationParty = value);
            AddToggleRow(page, "Auto-accept friend requests", 0, 4, () => _controller.Config.AutoAcceptFriend, value => _controller.Config.AutoAcceptFriend = value);
            AddToggleRow(page, "Auto-accept guild requests", 0, 5, () => _controller.Config.AutoAcceptGuild, value => _controller.Config.AutoAcceptGuild = value);
            AddLabel(page,
                "Party support and invite acceptance are configuration-only until a supported target-client action path is connected. No new network packets are introduced.",
                0, 225, 550, 58, 9, ModernHudTheme.TextGray);
        }

        private UIControl CreatePage()
        {
            return new HelperPageControl
            {
                X = 20,
                Y = 110,
                ControlSize = new Point(580, 432),
                ViewSize = new Point(580, 432),
                AutoViewSize = false,
                Interactive = false,
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 0
            };
        }

        private void AddValueRow(UIControl parent, string label, int columnX, int row, Func<string> text, Action action)
        {
            int y = 4 + row * RowHeight;
            AddLabel(parent, label, columnX, y + 4, 158, 22, 9.2f, ModernHudTheme.TextWhite);
            var button = CreateButton(string.Empty, columnX + 160, y, 118, 25, () =>
            {
                action();
                _controller.Config.Normalize();
                RefreshValues();
            });
            parent.Controls.Add(button);
            _boundButtons.Add((button, text));
        }

        private void AddToggleRow(UIControl parent, string label, int columnX, int row, Func<bool> getValue, Action<bool> setValue)
        {
            AddValueRow(parent, label, columnX, row, () => getValue() ? "ON" : "OFF", () => setValue(!getValue()));
        }

        private void AddSkillRow(UIControl parent, string label, int columnX, int row, Func<ushort> getSkillId, Action<ushort> setSkillId)
        {
            AddValueRow(parent, label, columnX, row, () => GetSkillLabel(getSkillId()), () =>
            {
                SkillEntryState skill = _scene.ModernHud?.SelectedSkill;
                if (skill == null)
                {
                    _logger?.LogInformation("Select a skill on the HUD before assigning a MU Helper slot.");
                    return;
                }
                setSkillId(skill.SkillId);
            });
        }

        private void AddLabel(UIControl parent, string text, int x, int y, int width, int height, float fontSize, Color color, bool bold = false)
        {
            var label = new LabelControl
            {
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
        }

        private ButtonControl CreateButton(string text, int x, int y, int width, int height, Action action)
        {
            var button = new ButtonControl
            {
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(width, height),
                ViewSize = new Point(width, height),
                AutoViewSize = false,
                FontSize = 9.2f,
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold,
                BackgroundColor = new Color(25, 30, 40, 245),
                HoverBackgroundColor = new Color(55, 47, 31, 245),
                PressedBackgroundColor = new Color(38, 34, 29, 250),
                BorderColor = ModernHudTheme.BorderInner,
                BorderThickness = 1
            };
            button.Click += (_, _) => action();
            return button;
        }

        private void SetActiveTab(int tabIndex)
        {
            _activeTab = Math.Clamp(tabIndex, 0, _pages.Length - 1);
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].Visible = i == _activeTab;
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                bool selected = i == _activeTab;
                _tabButtons[i].BackgroundColor = selected ? ModernHudTheme.SlotSelected : new Color(18, 22, 30, 245);
                _tabButtons[i].TextColor = selected ? ModernHudTheme.TextGold : ModernHudTheme.TextGray;
            }
        }

        private void RefreshValues()
        {
            foreach (var binding in _boundButtons)
                binding.Button.Text = binding.Text();
            _startButton.Text = _controller.IsActive ? "Stop Helper" : "Start Helper";
            _startButton.BackgroundColor = _controller.IsActive
                ? new Color(86, 37, 39, 245)
                : new Color(31, 65, 48, 245);
            if (_extraItemsBox != null && !_extraItemsBox.HasFocus)
                _extraItemsBox.Text = string.Join(", ", _controller.Config.ExtraItems ?? new List<string>());
        }

        private void SaveSettings()
        {
            CommitExtraItemText();
            _controller.Save();
            RefreshValues();
        }

        private void ResetSettings()
        {
            _controller.Reset();
            _extraItemsBox.Text = string.Empty;
            _controller.Save();
            RefreshValues();
        }

        private void CommitExtraItemText()
        {
            if (_extraItemsBox == null)
                return;

            _controller.Config.ExtraItems = _extraItemsBox.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
            Scene.FocusControl = _extraItemsBox;
            _extraItemsBox.OnClick();
        }

        private void OnControllerStateChanged() => RefreshValues();

        private void Recenter()
        {
            Point virtualSize = UiScaler.VirtualSize;
            X = Math.Max(0, (virtualSize.X - WindowWidth) / 2);
            Y = Math.Max(10, (virtualSize.Y - WindowHeight) / 2);
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
