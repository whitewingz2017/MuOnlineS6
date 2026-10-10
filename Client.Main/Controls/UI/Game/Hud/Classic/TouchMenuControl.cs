using System;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Controls.UI;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>PNG mobile menu arranged in three columns, with separate map and chat shortcuts.</summary>
    public sealed class TouchMenuControl : UIControl
    {
        private const int Size = 48;
        private const int Step = 64;
        private const int RightMargin = 16;
        private const int TopMargin = 12;
        private sealed record MenuEntry(string Icon, int Column, int Row, Action OnTap);
        private readonly MenuEntry[] _entries;
        private readonly Texture2D[] _textures;
        private readonly Rectangle[] _rects;
        private Texture2D _menuTexture, _mapTexture, _chatTexture;
        private Rectangle _menuRect, _mapRect, _chatRect;
        private bool _open = true;
        private UiThemeId _loadedTheme = (UiThemeId)(-1);
        public SkillImprintControl ImprintPanel { get; set; }
        public PotionImprintControl PotionPanel { get; set; }
        public TouchActionButtonsControl HotbarToHide { get; set; }

        public TouchMenuControl()
        {
            Interactive = true;
            AutoViewSize = false;
            _entries = new[]
            {
                new MenuEntry("recharge", 0, 0, null),
                new MenuEntry("benefits", 1, 0, null),
                new MenuEntry("ranking", 0, 1, null),
                new MenuEntry("character", 1, 1, () => (Scene as GameScene)?.ToggleClassicCharacterInfo()),
                new MenuEntry("inventory", 2, 1, () => (Scene as GameScene)?.ToggleClassicInventory()),
                new MenuEntry("quest", 0, 2, null),
                new MenuEntry("skill", 1, 2, OpenImprint),
                new MenuEntry("master_skill", 2, 2, OpenMastery),
                new MenuEntry("events", 0, 3, null),
                new MenuEntry("friends", 1, 3, null),
                new MenuEntry("guild", 2, 3, null),
                new MenuEntry("mail", 1, 4, null),
                new MenuEntry("party", 2, 4, () => (Scene as GameScene)?.ToggleClassicParty()),
                new MenuEntry("settings", 1, 5, OpenSettings),
                new MenuEntry("exit", 2, 5, ConfirmAndQuit),
            };
            _textures = new Texture2D[_entries.Length];
            _rects = new Rectangle[_entries.Length];
            LayoutRects();
        }

        public void Close() { _open = false; }
        public void Open() { _open = true; }
        private void OpenImprint() { ImprintPanel?.Toggle(); if (ImprintPanel?.Visible == true) ImprintPanel.BringToFront(); }
        private void OpenMastery()
        {
            if (Scene is not GameScene scene) return;
            var tree = scene.ClassicMasteryTree;
            tree?.Toggle();
            if (tree?.Visible == true) { tree.BringToFront(); Scene.FocusControl = tree; }
        }
        private void OpenSettings()
        {
            var menu = (Scene as GameScene)?.PauseMenu;
            if (menu == null) return;
            menu.Visible = true; menu.BringToFront(); Scene.FocusControl = menu;
        }
        private void ConfirmAndQuit()
        {
            RequestDialog.Show("Do you want to exit the game?", () =>
            {
#if !IOS
                MuGame.ScheduleOnMainThread(() => MuGame.Instance.Exit());
#endif
            });
        }
        private static string IconPath(string name) => $"Interface/DH/mobile_{name}.png";
        public override async Task Load()
        {
            await base.Load();
            if (_loadedTheme == UiThemeManager.CurrentId) return;
            _menuTexture = await UiThemeManager.LoadThemeTextureAsync("Interface/DH/img_more.png");
            _mapTexture = await UiThemeManager.LoadThemeTextureAsync(IconPath("map"));
            _chatTexture = await UiThemeManager.LoadThemeTextureAsync(IconPath("chat"));
            for (int i = 0; i < _entries.Length; i++)
            {
                string path = _entries[i].Icon switch
                {
                    "inventory" => "Interface/DH/img_new_btn_bao.png",
                    "settings"  => "Interface/DH/img_btn_setting.png.png",
                    "character" => "Interface/DH/icon_RankGE.png",
                    "skill"     => "Interface/DH/img_new_btn_ji.png",
                    _           => IconPath(_entries[i].Icon)
                };

                _textures[i] = await UiThemeManager.LoadThemeTextureAsync(path);
            }
                
            _loadedTheme = UiThemeManager.CurrentId;
        }
        private void LayoutRects()
        {
            int rightX = UiScaler.VirtualSize.X - RightMargin - Size;
            _menuRect = new Rectangle(rightX, TopMargin, Size, Size);
            _mapRect = new Rectangle(16, 42, Size, Size);
            _chatRect = new Rectangle(UiScaler.VirtualSize.X / 2 - 176, UiScaler.VirtualSize.Y - 116, Size, Size);
            for (int i = 0; i < _entries.Length; i++)
                _rects[i] = new Rectangle(rightX - (2 - _entries[i].Column) * Step,
                    TopMargin + _entries[i].Row * Step, Size, Size);
            // Bounds cover all shortcuts; hit testing below only captures the icons.
            X = 0; Y = 0; ControlSize = ViewSize = UiScaler.VirtualSize;
        }
        public override bool ContainsPointerPoint(Point point)
        {
            if (_menuRect.Contains(point) || _mapRect.Contains(point) || _chatRect.Contains(point)) return true;
            if (_open)
                foreach (var rect in _rects) if (rect.Contains(point)) return true;
            return false;
        }
        public override void Update(GameTime gameTime)
        {
            LayoutRects();
            base.Update(gameTime);
            if (!Visible) return;
            if (HotbarToHide != null)
                HotbarToHide.MasterAlpha = _open || ImprintPanel?.Visible == true || PotionPanel?.Visible == true ? 0f : 1f;
            var mouse = MuGame.Instance.UiMouseState;
            if (mouse.LeftButton != ButtonState.Pressed || MuGame.Instance.PrevUiMouseState.LeftButton != ButtonState.Released) return;
            if (!ContainsPointerPoint(mouse.Position)) return;
            Scene?.SetMouseInputConsumed();
            // Defer window changes so the same tap cannot also operate the newly opened window.
            if (_menuRect.Contains(mouse.Position)) { _open = !_open; return; }
            if (_mapRect.Contains(mouse.Position)) { MuGame.ScheduleOnMainThread(() => (Scene as GameScene)?.ToggleMobileMap()); return; }
            if (_chatRect.Contains(mouse.Position)) { MuGame.ScheduleOnMainThread(() => (Scene as GameScene)?.OpenMobileChat()); return; }
            if (_open)
                for (int i = 0; i < _entries.Length; i++)
                    if (_rects[i].Contains(mouse.Position))
                    {
                        var action = _entries[i].OnTap;
                        if (action != null) MuGame.ScheduleOnMainThread(action);
                        return;
                    }
        }
        public override void Draw(GameTime gameTime)
        {
            if (!Visible) return;
            var sb = GraphicsManager.Instance?.Sprite;
            if (sb == null) return;
            LayoutRects();
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, transform: UiScaler.SpriteTransform))
            {
                void DrawIcon(Texture2D texture, Rectangle rect)
                {
                    if (texture != null && !texture.IsDisposed) sb.Draw(texture, rect, Color.White * Alpha);
                }
                DrawIcon(_menuTexture, _menuRect); DrawIcon(_mapTexture, _mapRect); DrawIcon(_chatTexture, _chatRect);
                if (_open) for (int i = 0; i < _entries.Length; i++) DrawIcon(_textures[i], _rects[i]);
            }
        }
    }
}
