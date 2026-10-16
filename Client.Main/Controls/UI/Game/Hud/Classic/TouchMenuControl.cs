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
    /// <summary>Mobile minimap shortcuts with a collapsible function menu.</summary>
    public sealed class TouchMenuControl : UIControl
    {
        private const int Size = 48;
        private const int Step = 64;
        private const int RightMargin = 16;
        private const int TopMargin = 12;
        private sealed record MenuEntry(string Icon, int Column, int Row, Action OnTap, bool AlwaysVisible = false);
        public static Rectangle MobileMapRectangle => new(UiScaler.WindowBounds.Right - RightMargin - 240, UiScaler.WindowBounds.Top + TopMargin + 34, 240, 160);
        public static Rectangle MobileBatteryRectangle => new(MobileMapRectangle.X-88,MobileMapRectangle.Y-30,80,24);
        private readonly MenuEntry[] _entries;
        private readonly Texture2D[] _textures;
        private readonly Rectangle[] _rects;
        private Texture2D _menuTexture, _mapTexture, _chatTexture;
        private Rectangle _menuRect, _mapRect, _chatRect;
        private Rectangle TravelHeader=>new(_mapRect.X,_mapRect.Y-34,_mapRect.Width,34);
        private bool _open;
        private float _menuProgress;
        private float _chatProgress;


        private static float Ease(float value) => value * value * (3f - 2f * value);
        private static float Advance(float value, bool open, float seconds) =>
            MathHelper.Clamp(value + (open ? 1f : -1f) * seconds / 0.24f, 0f, 1f);
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
                new MenuEntry("inventory", 0, 0, () => (Scene as GameScene)?.ToggleClassicInventory(), true),

                new MenuEntry("mail", 2, 0, () => MessageWindow.Show("Inbox is not available yet."), true),
                new MenuEntry("recharge", 0, 1, null),
                new MenuEntry("benefits", 1, 1, null),
                new MenuEntry("ranking", 2, 1, null),
                new MenuEntry("quest", 0, 2, null),
                new MenuEntry("skill", 1, 2, OpenImprint),
                new MenuEntry("master_skill", 2, 2, OpenMastery),
                new MenuEntry("events", 0, 3, null),
                new MenuEntry("friends", 1, 3, null),
                new MenuEntry("guild", 2, 3, null),
                new MenuEntry("party", 0, 4, () => (Scene as GameScene)?.ToggleClassicParty()),
                new MenuEntry("settings", 1, 4, OpenSettings),
                new MenuEntry("exit", 2, 4, ConfirmAndQuit),
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
                    "inventory" => "Interface/DH/upscale_inventory.png",
                    "settings"  => "Interface/DH/img_btn_setting.png",
                    "character" => "Interface/DH/Character.png",
                    "skill"     => "Interface/DH/img_new_btn_ji.png",
                    "exit"     => "Interface/DH/btn_exit.png",
                    _           => IconPath(_entries[i].Icon)
                };

                _textures[i] = await UiThemeManager.LoadThemeTextureAsync(path);
            }
                
            _loadedTheme = UiThemeManager.CurrentId;
        }
        private void LayoutRects()
        {
            _mapRect = MobileMapRectangle;
            int chatX = MobileHudLayout.ChatX+MobileHudLayout.ChatWidth+8;
            int chatY = MobileHudLayout.ChatBottom-Size;
            _chatRect = new Rectangle(chatX, chatY, Size, Size);
            LayoutShortcutGrid(UiScaler.WindowBounds,MobilePlayerPanel.LayoutBounds,_mapRect,_rects);
            _menuRect=new Rectangle(MobileBatteryRectangle.Left-Size-8,_mapRect.Y-34,Size,32);
            float blend=Ease(_menuProgress);
            for(int i=0;i<_rects.Length;i++)
            {
                if(i==0)continue; // Inventory remains available with the list closed.
                var target=_rects[i];
                _rects[i]=new Rectangle((int)MathHelper.Lerp(_menuRect.Left,target.X,blend),
                    (int)MathHelper.Lerp(_menuRect.Top,target.Y,blend),target.Width,target.Height);
            }
            // Bounds cover all shortcuts; hit testing below only captures the icons.
            X = 0; Y = 0; ControlSize = ViewSize = UiScaler.VirtualSize;
        }
        internal static void LayoutShortcutGrid(Rectangle window,Rectangle profile,Rectangle map,Rectangle[] slots)
        {
            if(slots.Length==0)return;
            const int gap=6;
            var toggle=new Rectangle(map.Left-88-Size-8,map.Top-34,Size,32);
            int left=profile.Right+gap,right=toggle.Left-gap;
            var area=new Rectangle(left,toggle.Top,Math.Max(0,right-left),Math.Max(144,map.Bottom-toggle.Top));
            int columns=1,icon=0;
            Fit(area);
            if(icon<24)
            {
                // On narrow screens the two top HUDs leave no side strip.
                // Keep the toggle fixed and put the list immediately below them.
                area=new Rectangle(window.Left+12,Math.Max(profile.Bottom,map.Bottom)+12,window.Width-24,164);
                icon=0;Fit(area);
            }
            for(int i=0;i<slots.Length;i++)
                slots[i]=new Rectangle(area.Right-icon-(i%columns)*(icon+gap),area.Top+(i/columns)*(icon+gap),icon,icon);
            void Fit(Rectangle available)
            {
                for(int count=1;count<=Math.Min(8,slots.Length);count++)
                {
                    int rows=(slots.Length+count-1)/count;
                    if(rows>3)continue;
                    int size=Math.Min(Size,Math.Min((available.Width-(count-1)*gap)/count,(available.Height-(rows-1)*gap)/rows));
                    if(size>=icon && size>0){icon=size;columns=count;}
                }
            }
        }
        public override bool ContainsPointerPoint(Point point)
        {
            if (_menuRect.Contains(point) || _mapRect.Contains(point) || TravelHeader.Contains(point) || _chatRect.Contains(point)) return true;
            for (int i = 0; i < _entries.Length; i++)
                if ((i==0 || _open || _menuProgress > 0f) && _rects[i].Contains(point)) return true;
            return false;
        }
        public override void Update(GameTime gameTime)
        {
            if (Visible)
            {
                float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _menuProgress = Advance(_menuProgress, _open, seconds);
                _chatProgress = Advance(_chatProgress, (Scene as GameScene)?.ChatInput?.Visible == true, seconds);
            }
            LayoutRects();
            base.Update(gameTime);
            if (!Visible) return;
            if (HotbarToHide != null)
                HotbarToHide.MasterAlpha = _open || ImprintPanel?.Visible == true || PotionPanel?.Visible == true ? 0f : 1f;
            var mouse = MuGame.Instance.UiMouseState;
            if (mouse.LeftButton != ButtonState.Pressed || MuGame.Instance.PrevUiMouseState.LeftButton != ButtonState.Released) return;
            if (!ContainsPointerPoint(mouse.Position)) return;
            if(Scene?.MouseHoverControl is { } hovered && hovered!=this && hovered!=Scene.World)return;
            Scene?.SetMouseInputConsumed();
            // Defer window changes so the same tap cannot also operate the newly opened window.
            if (_menuRect.Contains(mouse.Position)) { _open = !_open; if(_open)BringToFront(); return; }
            if (_mapRect.Contains(mouse.Position) || TravelHeader.Contains(mouse.Position)) { MuGame.ScheduleOnMainThread(() => (Scene as GameScene)?.ToggleMobileMap()); return; }
            if (_chatRect.Contains(mouse.Position)) { MuGame.ScheduleOnMainThread(() => (Scene as GameScene)?.OpenMobileChat()); return; }
            for (int i = 0; i < _entries.Length; i++)
                    if ((i==0 || _open) && _rects[i].Contains(mouse.Position))
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
                void DrawIcon(Texture2D texture, Rectangle rect, float opacity = 1f,float rotation=0f)
                {
                    if(texture==null || texture.IsDisposed || rect.Width<=0 || rect.Height<=0)return;
                    float c=MathF.Abs(MathF.Cos(rotation)),s=MathF.Abs(MathF.Sin(rotation));
                    float fit=Math.Min(1f,Math.Min(rect.Width/(rect.Width*c+rect.Height*s),rect.Height/(rect.Width*s+rect.Height*c)));
                    sb.Draw(texture,rect.Center.ToVector2(),null,Color.White*(Alpha*opacity),rotation,
                        new Vector2(texture.Width/2f,texture.Height/2f),new Vector2(rect.Width/(float)texture.Width,rect.Height/(float)texture.Height)*fit,SpriteEffects.None,0);
                }
                DrawIcon(_menuTexture, _menuRect,rotation:MathHelper.TwoPi*Ease(_menuProgress)); DrawIcon(_chatTexture, _chatRect);
                for (int i = 0; i < _entries.Length; i++)
                    if (i==0 || _menuProgress > 0f)
                        DrawIcon(_textures[i], _rects[i], i==0?1f:Ease(_menuProgress));
            }
        }
    }
}
