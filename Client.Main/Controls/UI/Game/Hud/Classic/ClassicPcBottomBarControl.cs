using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Skills;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Hud.Classic
{
    /// <summary>
    /// Optional classic PC Season 6 bottom bar using the original Interface/newui_* atlases.
    /// Presents existing CharacterState and ModernBottomHud data only.
    /// </summary>
    internal sealed class ClassicPcBottomBarControl : UIControl
    {
        private float HudScale => Math.Clamp(
            Math.Min(UiScaler.ActualSize.X / 640f, UiScaler.ActualSize.Y / 480f), 1f, 2f);
        private const int ReferenceWidth = 640;
        private const int ReferenceHeight = 51;
        private const int ReferenceExpY = 41;
        private const int ReferenceSkillStartX = 222;
        private const int ReferenceSkillY = 2;
        private const int ReferenceSkillWidth = ClassicSkillSlotRenderer.CellWidth;
        private const int ReferenceSkillHeight = ClassicSkillSlotRenderer.CellHeight;
        private const int ReferenceCurrentSkillX = 385;
        private const int ReferenceCurrentSkillY = 2;
        private const int SkillListColumns = 15;
        private const int ReferenceHpX = 158;
        private const int ReferenceMpX = 437;
        private const int ReferenceSdX = 204;
        private const int ReferenceAgX = 420;
        private const int ReferenceButtonStartX = 489;
        private const int ReferenceButtonY = 0;
        private const int ReferenceButtonWidth = 30;
        private const int ReferenceButtonHeight = 41;
        private const int ReferenceGaugeY = 3;
        private const int ReferenceGaugeHeight = 39;

        private const string MainLeftPath = "Interface/newui_menu01.OZJ";
        private const string MainCenterPath = "Interface/newui_menu02.OZJ";
        private const string MainRightPath = "Interface/partCharge1/newui_menu03.OZJ";
        private const string MainExpandedPath = "Interface/newui_menu02-03.OZJ";
        private const string HpGaugePath = "Interface/newui_menu_red.OZJ";
        private const string PoisonGaugePath = "Interface/newui_menu_green.OZJ";
        private const string MpGaugePath = "Interface/newui_menu_blue.OZJ";
        private const string SdGaugePath = "Interface/newui_menu_SD.OZJ";
        private const string AgGaugePath = "Interface/newui_menu_AG.OZJ";
        private const string ExpGaugePath = "Interface/newui_Exbar.OZJ";
        private const string MasterExpGaugePath = "Interface/Exbar_Master.OZJ";
        private const string SkillFramePath = ClassicSkillSlotRenderer.NormalFramePath;
        private const string SkillFrameActivePath = ClassicSkillSlotRenderer.ActiveFramePath;
        private static readonly string[] ButtonPaths =
        {
            "Interface/partCharge1/newui_menu_Bt01.OZJ",
            "Interface/partCharge1/newui_menu_Bt02.OZJ",
            "Interface/partCharge1/newui_menu_Bt03.OZJ",
            "Interface/partCharge1/newui_menu_Bt04.OZJ",
            "Interface/partCharge1/newui_menu_Bt05.OZJ"
        };

        private readonly CharacterState _state;
        private readonly ModernBottomHud _hud;
        private readonly Rectangle[] _skillRects = new Rectangle[5];
        private readonly Rectangle[] _buttonRects = new Rectangle[5];
        private readonly List<(Rectangle Rect, SkillEntryState Skill)> _skillListHits = new();
        private Rectangle _skillTabRect;
        private Rectangle _skillPanelRect;
        private bool _skillListOpen;
        private int _hoveredSkillListIndex = -1;
        private int _hoveredButton = -1;
        private SpriteFont _font;
        private Texture2D _mainLeft;
        private Texture2D _mainCenter;
        private Texture2D _mainRight;
        private Texture2D _mainExpanded;
        private Texture2D _hpGauge;
        private Texture2D _poisonGauge;
        private Texture2D _mpGauge;
        private Texture2D _sdGauge;
        private Texture2D _agGauge;
        private Texture2D _expGauge;
        private Texture2D _masterExpGauge;
        private Texture2D _skillFrame;
        private Texture2D _skillFrameActive;
        private readonly Texture2D[] _buttonTextures = new Texture2D[5];

        public ClassicPcBottomBarControl(CharacterState state, ModernBottomHud hud)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));

            AutoViewSize = false;
            Interactive = true;
            ControlSize = new Point((int)(ReferenceWidth * HudScale), 84);
            ViewSize = ControlSize;
            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;
            BorderThickness = 0;
            RefreshLayout();
        }

        public override async Task Load()
        {
            await base.Load();

            _mainLeft = await LoadInterfaceTextureAsync(MainLeftPath);
            _mainCenter = await LoadInterfaceTextureAsync(MainCenterPath);
            _mainRight = await LoadInterfaceTextureAsync(MainRightPath);
            _mainExpanded = await LoadInterfaceTextureAsync(MainExpandedPath);
            _hpGauge = await LoadInterfaceTextureAsync(HpGaugePath);
            _poisonGauge = await LoadInterfaceTextureAsync(PoisonGaugePath);
            _mpGauge = await LoadInterfaceTextureAsync(MpGaugePath);
            _sdGauge = await LoadInterfaceTextureAsync(SdGaugePath);
            _agGauge = await LoadInterfaceTextureAsync(AgGaugePath);
            _expGauge = await LoadInterfaceTextureAsync(ExpGaugePath);
            _masterExpGauge = await LoadInterfaceTextureAsync(MasterExpGaugePath);
            _skillFrame = await LoadInterfaceTextureAsync(SkillFramePath);
            _skillFrameActive = await LoadInterfaceTextureAsync(SkillFrameActivePath);
            for (int i = 0; i < ButtonPaths.Length; i++)
                _buttonTextures[i] = await LoadInterfaceTextureAsync(ButtonPaths[i]);

            foreach (string path in SkillIconAtlas.TexturePaths)
                await LoadInterfaceTextureAsync(path);
        }

        private static async Task<Texture2D> LoadInterfaceTextureAsync(string path)
        {
            try
            {
                return await UiThemeManager.LoadThemeTextureAsync(path).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            RefreshLayout();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Update(gameTime);
            RefreshLayout();
            HandleKeyboard();
            HandleMouse();
        }

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible)
                return;

            var sprite = GraphicsManager.Instance?.Sprite;
            var pixel = GraphicsManager.Instance?.Pixel;
            _font ??= GraphicsManager.Instance?.Font;
            if (sprite == null || pixel == null || _font == null)
                return;

            Rectangle full = DisplayRectangle;

            // Bottom main bar only. Skill list (when open) occupies the top of `full`.
            int barHeight = ScaleValue(ReferenceHeight) + 6;
            Rectangle barRect = new(
                full.Left,
                full.Bottom - barHeight,
                ScaleValue(ReferenceWidth),
                barHeight);

            DrawMainFrame(sprite, pixel, barRect);
            DrawResourceGauges(sprite, barRect);
            DrawExperienceGauge(sprite, barRect);
            DrawSkillSlots(sprite, barRect);
            DrawRightButtons(sprite, barRect);
            DrawSkillTray(sprite, pixel);
            DrawPotionHints(sprite, barRect);
        }

        private void DrawMainFrame(SpriteBatch sprite, Texture2D pixel, Rectangle frameRect)
        {
            int frameHeight = ScaleValue(ReferenceHeight);
            Rectangle band = new(frameRect.Left, frameRect.Top, ScaleValue(ReferenceWidth), frameHeight);

            if (_mainLeft != null)
            {
                sprite.Draw(_mainLeft,
                    new Rectangle(band.Left, band.Top, ScaleValue(256), frameHeight),
                    new Rectangle(0, 0, _mainLeft.Width, _mainLeft.Height), Color.White);
            }

            if (_mainCenter != null)
            {
                sprite.Draw(_mainCenter,
                    new Rectangle(band.Left + ScaleValue(256), band.Top, ScaleValue(128), frameHeight),
                    new Rectangle(0, 0, _mainCenter.Width, _mainCenter.Height), Color.White);
            }

            if (_mainRight != null)
            {
                sprite.Draw(_mainRight,
                    new Rectangle(band.Left + ScaleValue(384), band.Top, ScaleValue(256), frameHeight),
                    new Rectangle(0, 0, _mainRight.Width, _mainRight.Height), Color.White);
            }

            // Do not draw _mainExpanded over slots 1–5 (that looked like a second hotbar).

            if (_mainLeft == null || _mainCenter == null || _mainRight == null)
            {
                sprite.Draw(pixel, band, new Color(13, 13, 16, 215));
                UiDrawHelper.DrawBorder(sprite, band, new Color(111, 83, 44, 220), 1);
            }
        }

        private void DrawResourceGauges(SpriteBatch sprite, Rectangle rect)
        {
            float hp = Ratio(_state.CurrentHealth, _state.MaximumHealth);
            float mp = Ratio(_state.CurrentMana, _state.MaximumMana);
            float sd = Ratio(_state.CurrentShield, _state.MaximumShield);
            float ag = Ratio(_state.CurrentAbility, _state.MaximumAbility);

            DrawVerticalGauge(sprite, _state.CurrentHealth, _state.MaximumHealth, hp,
                _hpGauge ?? _poisonGauge, rect, ReferenceHpX, ReferenceGaugeY, 45);
            DrawVerticalGauge(sprite, _state.CurrentMana, _state.MaximumMana, mp,
                _mpGauge, rect, ReferenceMpX, ReferenceGaugeY, 45);
            DrawVerticalGauge(sprite, _state.CurrentShield, _state.MaximumShield, sd,
                _sdGauge, rect, ReferenceSdX, ReferenceGaugeY, 16);
            DrawVerticalGauge(sprite, _state.CurrentAbility, _state.MaximumAbility, ag,
                _agGauge, rect, ReferenceAgX, ReferenceGaugeY, 16);

            DrawValue(sprite, $"{_state.CurrentHealth}", rect.Left + ScaleValue(178), rect.Top + ScaleValue(33), 8f);
            DrawValue(sprite, $"{_state.CurrentMana}", rect.Left + ScaleValue(464), rect.Top + ScaleValue(33), 8f);
        }

        private void DrawVerticalGauge(SpriteBatch sprite, uint current, uint maximum, float ratio,
            Texture2D texture, Rectangle rect, int referenceX, int referenceY, int referenceWidth)
        {
            if (texture == null || maximum == 0 || ratio <= 0f)
                return;

            int height = ScaleValue(ReferenceGaugeHeight);
            int width = ScaleValue(referenceWidth);
            int filledHeight = Math.Clamp((int)MathF.Round(height * ratio), 1, height);
            int top = rect.Top + ScaleValue(referenceY) + height - filledHeight;
            int sourceHeight = Math.Max(1, (int)MathF.Round(texture.Height * ratio));
            Rectangle source = new(0, Math.Max(0, texture.Height - sourceHeight), texture.Width, sourceHeight);
            Rectangle destination = new(rect.Left + ScaleValue(referenceX), top, width, filledHeight);
            sprite.Draw(texture, destination, source, Color.White);
        }

        private void DrawExperienceGauge(SpriteBatch sprite, Rectangle rect)
        {
            Texture2D texture = _state.Level >= 400 && _masterExpGauge != null ? _masterExpGauge : _expGauge;
            if (texture == null)
                return;

            double current = _state.Level >= 400 ? _state.MasterExperience : _state.Experience;
            double next = _state.Level >= 400 ? _state.MasterExperienceForNextLevel : _state.ExperienceForNextLevel;
            float ratio = next > 0 ? MathHelper.Clamp((float)(current / next), 0f, 1f) : 0f;
            int width = ScaleValue(629);
            int height = ScaleValue(4);
            int filledWidth = (int)(width * ratio);
            if (filledWidth <= 0)
                return;

            Rectangle source = new(0, 0, Math.Max(1, (int)(texture.Width * ratio)), texture.Height);
            Rectangle destination = new(rect.Left + ScaleValue(2), rect.Top + ScaleValue(ReferenceExpY), filledWidth, height);
            sprite.Draw(texture, destination, source, Color.White);
        }
        private static void DrawSkillIconRect(SpriteBatch sprite, ushort skillId, Rectangle dest)
        {
            // Classic PC follows MuMain: blit the complete 20x28 atlas cell.
            SkillIconRenderer.DrawSkillRect(sprite, skillId, dest, Color.White);
        }
        private void DrawSkillSlots(SpriteBatch sprite, Rectangle rect)
        {
            IReadOnlyList<SkillEntryState?> skills = _hud.HotbarSkills;
            int activeSlot = _hud.ActiveSkillSlot;

            for (int i = 0; i < _skillRects.Length; i++)
            {
                Rectangle slot = _skillRects[i];
                bool active = i == activeSlot;

                SkillEntryState skill = i < skills.Count ? skills[i] : null;
                ClassicSkillSlotRenderer.Draw(
                    sprite,
                    slot,
                    skill?.SkillId,
                    active,
                    _skillFrame,
                    _skillFrameActive,
                    Color.White);

                // Hotkey number bottom-right
                string label = (i + 1).ToString();
                float fontSize = 7.5f;
                float scale = fontSize / Constants.BASE_FONT_SIZE;
                Vector2 size = _font.MeasureString(label) * scale;
                float tx = slot.Right - size.X - ScaleValue(3);
                float ty = slot.Bottom - size.Y - ScaleValue(2);

                DrawValue(sprite, label, tx, ty, fontSize,
                    active ? new Color(255, 224, 138) : Color.WhiteSmoke);
            }
        }

        private void DrawRightButtons(SpriteBatch sprite, Rectangle rect)
        {
            _hoveredButton = -1;
            Point mouse = MuGame.Instance.UiMouseState.Position;
            for (int i = 0; i < _buttonRects.Length; i++)
            {
                Rectangle buttonRect = _buttonRects[i];
                if (buttonRect.Contains(mouse))
                    _hoveredButton = i;

                Texture2D texture = _buttonTextures[i];
                if (texture != null)
                {
                    int stateHeight = Math.Max(1, texture.Height / 4);
                    int state = i == _hoveredButton ? 1 : 0;
                    Rectangle source = new(0, Math.Min(state * stateHeight, texture.Height - stateHeight), texture.Width, stateHeight);
                    sprite.Draw(texture, buttonRect, source, Color.White);
                }
            }
        }

        private void DrawSkillTray(SpriteBatch sprite, Texture2D pixel)
        {
            SkillEntryState selectedSkill = _hud.SelectedSkill;

            // Current skill on the green tab: the same 20x28 rectangular cell.
            if (selectedSkill != null)
            {
                Rectangle iconRect = new(
                    _skillTabRect.X + ScaleValue(6),
                    _skillTabRect.Y + ScaleValue(6),
                    ScaleValue(20),
                    ScaleValue(28));
                DrawSkillIconRect(sprite, selectedSkill.SkillId, iconRect);
            }

            if (!_skillListOpen || _skillPanelRect.Width <= 0 || _skillPanelRect.Height <= 0)
                return;

            if (_mainExpanded != null)
            {
                sprite.Draw(_mainExpanded, _skillPanelRect,
                    new Rectangle(0, 0, _mainExpanded.Width, _mainExpanded.Height), Color.White);
            }
            else
            {
                sprite.Draw(pixel, _skillPanelRect, new Color(13, 13, 16, 235));
                UiDrawHelper.DrawBorder(sprite, _skillPanelRect, new Color(111, 83, 44, 220), 1);
            }

            for (int i = 0; i < _skillListHits.Count; i++)
            {
                (Rectangle slot, SkillEntryState skill) = _skillListHits[i];
                bool active = selectedSkill?.SkillId == skill.SkillId;
                ClassicSkillSlotRenderer.Draw(
                    sprite,
                    slot,
                    skill.SkillId,
                    active,
                    _skillFrame,
                    _skillFrameActive,
                    Color.White);
            }
        }

        private void DrawPotionHints(SpriteBatch sprite, Rectangle rect)
        {
            string[] labels = { "Q", "W", "E", "R" };
            int[] positions = { 28, 58, 88, 118 };
            for (int i = 0; i < labels.Length; i++)
            {
                DrawValue(sprite, labels[i], rect.Left + ScaleValue(positions[i]), rect.Top + ScaleValue(33), 7.5f,
                    i < 3 && _hud.GetItemAssignmentAt(i).HasValue ? new Color(255, 224, 138) : Color.WhiteSmoke);
            }
        }

        private void HandleKeyboard()
        {
            if (_hud.IsModernRenderer)
                return;

            var keyboard = MuGame.Instance.Keyboard;
            var previous = MuGame.Instance.PrevKeyboard;
            Keys[] potionKeys = { Keys.Q, Keys.W, Keys.E };
            for (int i = 0; i < potionKeys.Length; i++)
            {
                if (keyboard.IsKeyDown(potionKeys[i]) && previous.IsKeyUp(potionKeys[i]))
                    _hud.TryConsumePotionForHud(i);
            }

            Keys[] skillKeys = { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5 };
            bool controlHeld = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            for (int i = 0; i < skillKeys.Length; i++)
            {
                bool justPressed = IsJustPressed(keyboard, previous, skillKeys[i]);
                if (!justPressed)
                    continue;

                // MuMain: Ctrl+number assigns hovered list skill to hotkey 1–5.
                if (controlHeld && _skillListOpen && _hoveredSkillListIndex >= 0
                    && _hoveredSkillListIndex < _skillListHits.Count)
                {
                    AssignSkillToSlot(i, _skillListHits[_hoveredSkillListIndex].Skill);
                    continue;
                }

                _hud.SelectSkillSlot(i);
            }
        }

        private static bool IsJustPressed(KeyboardState keyboard, KeyboardState previous, Keys key)
            => keyboard.IsKeyDown(key) && previous.IsKeyUp(key);

        private void AssignSkillToSlot(int slot, SkillEntryState skill)
        {
            if (skill == null || slot < 0 || slot >= _skillRects.Length)
                return;

            for (int i = 0; i < _skillRects.Length; i++)
            {
                if (i != slot && _hud.GetHotbarSkillAt(i)?.SkillId == skill.SkillId)
                    _hud.ClearHotbarSkillAt(i);
            }

            _hud.SetHotbarSkillAt(slot, skill);
            _hud.SelectSkillSlot(slot);
        }

        private void HandleMouse()
        {
            var mouse = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            Point p = mouse.Position;

            _hoveredSkillListIndex = -1;
            if (_skillListOpen)
            {
                for (int i = 0; i < _skillListHits.Count; i++)
                {
                    if (_skillListHits[i].Rect.Contains(p))
                    {
                        _hoveredSkillListIndex = i;
                        break;
                    }
                }
            }

            bool leftClick = mouse.LeftButton == ButtonState.Pressed
                             && previous.LeftButton == ButtonState.Released;
            if (!leftClick)
                return;

            bool onSkillList = _skillListOpen &&
                               (_skillPanelRect.Contains(p) || _hoveredSkillListIndex >= 0);
            bool onBarUi =
                _skillTabRect.Contains(p)
                || onSkillList
                || Array.Exists(_skillRects, r => r.Contains(p))
                || Array.Exists(_buttonRects, r => r.Contains(p));

            if (!onBarUi)
                return;

            // Block world move/attack for any click on classic bar or skill list.
            Scene?.SetMouseInputConsumed();

            if (_skillTabRect.Contains(p))
            {
                _skillListOpen = !_skillListOpen;
                RefreshLayout();
                return;
            }

            if (_skillListOpen && _hoveredSkillListIndex >= 0)
            {
                SkillEntryState skill = _skillListHits[_hoveredSkillListIndex].Skill;
                _hud.SelectClassicSkill(skill);
                _skillListOpen = false;
                RefreshLayout();
                return;
            }

            // Empty list chrome: consumed, no move.
            if (onSkillList)
                return;

            for (int i = 0; i < _skillRects.Length; i++)
            {
                if (!_skillRects[i].Contains(p))
                    continue;

                _hud.SelectSkillSlot(i);
                return;
            }

            for (int i = 0; i < _buttonRects.Length; i++)
            {
                if (!_buttonRects[i].Contains(p))
                    continue;

                ActivateButton(i);
                return;
            }
        }

        private void ActivateButton(int index)
        {
            if (Scene is not Scenes.GameScene scene)
                return;

            switch (index)
            {
                case 0:
                    scene.ToggleClassicCharacterInfo();
                    break;
                case 1:
                    scene.ToggleClassicInventory();
                    break;
                case 2:
                    scene.ToggleClassicParty();
                    break;
                case 3:
                    scene.ShowClassicOptions();
                    break;
                case 4:
                    scene.ExitFromClassicHud();
                    break;
            }
        }

        private void RefreshLayout()
        {
            int width = ScaleValue(ReferenceWidth);
            int barHeight = ScaleValue(ReferenceHeight) + 6;
            int gap = ScaleValue(2);

            int panelHeight = 0;
            if (_skillListOpen)
            {
                int learnedCount = _state.GetSkills().Count();
                if (learnedCount > 0)
                {
                    int columns = Math.Min(SkillListColumns, learnedCount);
                    int rows = (learnedCount + columns - 1) / columns;
                    panelHeight = rows * ScaleValue(ReferenceSkillHeight);
                }
            }

            int totalHeight = barHeight + (_skillListOpen && panelHeight > 0 ? panelHeight + gap : 0);
            ControlSize = new Point(width, totalHeight);
            ViewSize = ControlSize;
            X = Math.Max(0, (UiScaler.VirtualSize.X - width) / 2);
            Y = Math.Max(0, UiScaler.VirtualSize.Y - totalHeight - ScaleValue(3));

            // Bar is the bottom band of this control; skill list sits above it (inside bounds).
            Rectangle frame = new(X, Y + (totalHeight - barHeight), width, barHeight);

            for (int i = 0; i < _skillRects.Length; i++)
            {
                _skillRects[i] = new Rectangle(
                    frame.Left + ScaleValue(ReferenceSkillStartX + i * ReferenceSkillWidth),
                    frame.Top + ScaleValue(ReferenceSkillY),
                    ScaleValue(ReferenceSkillWidth),
                    ScaleValue(ReferenceSkillHeight));
            }

            for (int i = 0; i < _buttonRects.Length; i++)
            {
                _buttonRects[i] = new Rectangle(
                    frame.Left + ScaleValue(ReferenceButtonStartX + i * ReferenceButtonWidth),
                    frame.Top + ScaleValue(ReferenceButtonY),
                    ScaleValue(ReferenceButtonWidth),
                    ScaleValue(ReferenceButtonHeight));
            }

            _skillTabRect = new Rectangle(
                frame.Left + ScaleValue(ReferenceCurrentSkillX),
                frame.Top + ScaleValue(ReferenceCurrentSkillY),
                ScaleValue(ReferenceSkillWidth),
                ScaleValue(ReferenceSkillHeight));

            RebuildSkillListLayout(frame, gap);
        }

        private void RebuildSkillListLayout(Rectangle barFrame, int gap)
        {
            _skillListHits.Clear();
            if (!_skillListOpen)
            {
                _skillPanelRect = Rectangle.Empty;
                return;
            }

            List<SkillEntryState> learnedSkills = _state.GetSkills()
                .OrderBy(skill => skill.SkillId)
                .ToList();
            if (learnedSkills.Count == 0)
            {
                _skillPanelRect = Rectangle.Empty;
                return;
            }

            int slotWidth = ScaleValue(ReferenceSkillWidth);
            int slotHeight = ScaleValue(ReferenceSkillHeight);
            int columns = Math.Min(SkillListColumns, learnedSkills.Count);
            int rows = (learnedSkills.Count + columns - 1) / columns;
            int panelWidth = columns * slotWidth;
            int panelHeight = rows * slotHeight;
            int panelCenterX = barFrame.Left + ScaleValue(ReferenceCurrentSkillX + ReferenceSkillWidth / 2);

            _skillPanelRect = new Rectangle(
                panelCenterX - panelWidth / 2,
                barFrame.Top - gap - panelHeight,
                panelWidth,
                panelHeight);

            for (int i = 0; i < learnedSkills.Count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                _skillListHits.Add((new Rectangle(
                    _skillPanelRect.Left + column * slotWidth,
                    _skillPanelRect.Top + row * slotHeight,
                    slotWidth,
                    slotHeight), learnedSkills[i]));
            }
        }

        private static float Ratio(uint current, uint maximum) => maximum == 0
            ? 0f
            : MathHelper.Clamp(current / (float)maximum, 0f, 1f);

        private int ScaleValue(int value) => Math.Max(1, (int)MathF.Round(value * HudScale));

        private void DrawValue(SpriteBatch sprite, string value, float x, float y, float size, Color? color = null)
        {
            float scale = size / Constants.BASE_FONT_SIZE;
            sprite.DrawString(_font, value, new Vector2(x + 1, y + 1), Color.Black * 0.8f,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sprite.DrawString(_font, value, new Vector2(x, y), color ?? Color.WhiteSmoke,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}