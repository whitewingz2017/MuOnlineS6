using Client.Main.Controls.UI.Common;
using Client.Main.Data;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Client.Main.Core.Client;
using Client.Main.Networking;
using MUnique.OpenMU.Network.Packets;
using Client.Main.Scenes;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Hud;
using Client.Main.Helpers;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game
{
    public class MoveCommandWindow : UIControl
    {
        private const int WINDOW_TARGET_WIDTH = 250;
        private const int WINDOW_TARGET_HEIGHT = 550;
        private const int WINDOW_X_OFFSET = 10;
        private const int WINDOW_Y_OFFSET_FROM_TOP = 20;

        private const int TITLE_AREA_HEIGHT = 30;
        private const int CLOSE_BUTTON_AREA_HEIGHT = 30;
        private const int ITEM_HEIGHT = 16;
        private const int SCROLLBAR_WIDTH = 17;

        private const int PADDING_GENERAL = 5;
        private const int PADDING_TITLE_TOP = 6;
        private const int PADDING_LIST_TOP_FROM_TITLE = 2;
        private const int PADDING_LIST_BOTTOM_TO_CLOSE = 5;
        private const int PADDING_SCROLLBAR_RIGHT = 5;
        private const int SCROLLBAR_VISUAL_WIDTH = 7;
        private const int SCROLLBAR_THUMB_WIDTH = 15;

        private const int COL_MAP_NAME_X = 0;
        private const int COL_LEVEL_X = 105;
        private const int COL_ZEN_X = 145;


        private LabelControl _titleLabel;
        private List<LabelControl> _mapNameLabels;
        private List<LabelControl> _mapLevelLabels;
        private List<LabelControl> _mapZenLabels;
        private List<Rectangle> _mapClickAreas;
        private int _hoveredMapIndex = -1;

        private ScrollBarControl _scrollBar;
        private ButtonControl _closeButton;

        private List<MoveCommandInfo> _availableMapsFullList;
        private List<MoveCommandInfo> _currentlyDisplayableMaps;

        private int _currentScrollOffset = 0;
        private int _maxVisibleItems;

        private readonly ILogger<MoveCommandWindow> _logger;
        private readonly NetworkManager _networkManager;

        /// <summary>
        /// Raised when the player selects a map to warp to.
        /// Provides the target map index and its display name.
        /// </summary>
        public event Action<int, string> MapWarpRequested;

        public MoveCommandWindow(ILoggerFactory loggerFactory, NetworkManager networkManager)
        {
            _logger = loggerFactory.CreateLogger<MoveCommandWindow>();
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));

            _mapNameLabels = new List<LabelControl>();
            _mapLevelLabels = new List<LabelControl>();
            _mapZenLabels = new List<LabelControl>();
            _mapClickAreas = new List<Rectangle>();

            _availableMapsFullList = new List<MoveCommandInfo>();
            _currentlyDisplayableMaps = new List<MoveCommandInfo>();

            Visible = false;
            Interactive = true;
            AutoViewSize = false;

            ControlSize = new Point(WINDOW_TARGET_WIDTH, WINDOW_TARGET_HEIGHT);
            ViewSize = ControlSize;
            X = WINDOW_X_OFFSET;
            Y = WINDOW_Y_OFFSET_FROM_TOP;
            BackgroundColor = new Color(15, 15, 25, 220);
        }

        public override async Task Load()
        {

            _titleLabel = new LabelControl
            {
                Text = "Warp Command Window",
                Align = ControlAlign.Top | ControlAlign.HorizontalCenter,
                X = 0,
                Y = PADDING_TITLE_TOP,
                FontSize = 13f,
                TextColor = new Color(255, 230, 190),
                ControlSize = new Point(ControlSize.X - (PADDING_GENERAL * 2), TITLE_AREA_HEIGHT - PADDING_TITLE_TOP)
            };
            Controls.Add(_titleLabel);

            int listAreaY = PADDING_TITLE_TOP + (TITLE_AREA_HEIGHT - PADDING_TITLE_TOP) + PADDING_LIST_TOP_FROM_TITLE;
            int listAvailableHeight = ControlSize.Y - listAreaY - CLOSE_BUTTON_AREA_HEIGHT - PADDING_BOTTOM;
            _maxVisibleItems = Math.Max(1, listAvailableHeight / ITEM_HEIGHT);

            _scrollBar = new ScrollBarControl
            {
                X = ControlSize.X - SCROLLBAR_THUMB_WIDTH - PADDING_SCROLLBAR_RIGHT, // Użyj SCROLLBAR_THUMB_WIDTH dla X
                Y = listAreaY,
                Height = listAvailableHeight,
                Width = SCROLLBAR_THUMB_WIDTH,
                Visible = false,
                ThumbHeightAdjustmentFactor = 0.2f
            };
            _scrollBar.ValueChanged += (s, e) => OnScrollChanged();
            Controls.Add(_scrollBar);

            _closeButton = new ButtonControl
            {
                Text = "Close",
                X = (ControlSize.X - 60) / 2,
                Y = ControlSize.Y - PADDING_BOTTOM - (ITEM_HEIGHT + 2), // Na samym dole
                ViewSize = new Point(60, ITEM_HEIGHT + 2),
                ControlSize = new Point(60, ITEM_HEIGHT + 2),
                FontSize = 9.5f,
                TextColor = Color.WhiteSmoke,
                BackgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.8f), // Ciemne tło
                HoverBackgroundColor = new Color(0.2f, 0.2f, 0.35f, 0.9f),
                PressedBackgroundColor = new Color(0.05f, 0.05f, 0.15f, 0.9f),
                BorderColor = new Color(100, 100, 120),
                BorderThickness = 1,
            };
            _closeButton.Click += (s, e) => ToggleVisibility();
            Controls.Add(_closeButton);

            await base.Load();
            LoadMapData();
        }

        private void LoadMapData()
        {
            _logger.LogDebug("Loading map data...");
            _availableMapsFullList = MoveCommandDataManager.Instance.GetMoveCommandDataList();
            _logger.LogDebug($"Loaded {_availableMapsFullList.Count} maps from data manager.");
            FilterAndSortMaps();
            RefreshMapListUI();
            UpdateScrollbarState();
        }

        private void FilterAndSortMaps()
        {
            var playerState = _networkManager.GetCharacterState();
            _currentlyDisplayableMaps = _availableMapsFullList.Select(map =>
            {
                map.CanMove = CanPlayerMoveTo(map, playerState);
                return map;
            }).ToList();
            _currentlyDisplayableMaps = _currentlyDisplayableMaps.OrderBy(m => m.Index).ToList();
        }

        private bool CanPlayerMoveTo(MoveCommandInfo mapInfo, CharacterState playerState)
        {
            if (playerState == null) return false;
            int playerLevel = playerState.Level;
            uint playerZen = playerState.InventoryZen;
            int requiredLevel = mapInfo.RequiredLevel;
            var charClass = playerState.Class;

            if (charClass == CharacterClassNumber.MagicGladiator || charClass == CharacterClassNumber.DuelMaster ||
                charClass == CharacterClassNumber.DarkLord || charClass == CharacterClassNumber.LordEmperor ||
                charClass == CharacterClassNumber.RageFighter || charClass == CharacterClassNumber.FistMaster)
            {
                if (mapInfo.RequiredLevel != 400)
                {
                    requiredLevel = (int)(mapInfo.RequiredLevel * 2.0f / 3.0f);
                }
            }
            if (playerLevel < requiredLevel) return false;
            if (playerZen < mapInfo.RequiredZen) return false;
            if (playerState.HeroState == CharacterHeroState.PlayerKiller1stStage || playerState.HeroState == CharacterHeroState.PlayerKiller2ndStage)
                return false;

            return true;
        }

        private void ClearMapListUI()
        {
            foreach (var lbl in _mapNameLabels) { Controls.Remove(lbl); lbl.Dispose(); }
            foreach (var lbl in _mapLevelLabels) { Controls.Remove(lbl); lbl.Dispose(); }
            foreach (var lbl in _mapZenLabels) { Controls.Remove(lbl); lbl.Dispose(); }
            _mapNameLabels.Clear();
            _mapLevelLabels.Clear();
            _mapZenLabels.Clear();
            _mapClickAreas.Clear();
        }

        private void RefreshMapListUI()
        {
            // _logger.LogDebug($"Refreshing map list UI. ScrollOffset: {_currentScrollOffset}, MaxVisible: {_maxVisibleItems}, DisplayableMaps: {_currentlyDisplayableMaps.Count}");
            ClearMapListUI();

            int listAreaY = PADDING_TOP_CONTENT;
            int mapContentWidth = ControlSize.X - PADDING_LEFT - PADDING_RIGHT - (_scrollBar.Visible ? SCROLLBAR_THUMB_WIDTH + PADDING_SCROLLBAR_RIGHT : 0);


            for (int i = 0; i < _maxVisibleItems; i++)
            {
                int currentMapDataIndex = _currentScrollOffset + i;
                if (currentMapDataIndex >= _currentlyDisplayableMaps.Count) break;

                var mapInfo = _currentlyDisplayableMaps[currentMapDataIndex];

                int currentY = listAreaY + (i * ITEM_HEIGHT);
                var clickArea = new Rectangle(
                    DisplayPosition.X + PADDING_LEFT,
                    DisplayPosition.Y + currentY,
                    mapContentWidth,
                    ITEM_HEIGHT - 1);
                _mapClickAreas.Add(clickArea);

                int effectiveRequiredLevel = mapInfo.RequiredLevel;
                if (playerState != null &&
                    (playerState.Class == CharacterClassNumber.MagicGladiator || playerState.Class == CharacterClassNumber.DuelMaster ||
                     playerState.Class == CharacterClassNumber.DarkLord || playerState.Class == CharacterClassNumber.LordEmperor ||
                     playerState.Class == CharacterClassNumber.RageFighter || playerState.Class == CharacterClassNumber.FistMaster))
                {
                    if (mapInfo.RequiredLevel != 400)
                    {
                        effectiveRequiredLevel = (int)(mapInfo.RequiredLevel * 2.0f / 3.0f);
                    }
                }

                Color nameColor = mapInfo.CanMove ? (mapInfo.IsSelected ? Color.Gold : Color.WhiteSmoke) : new Color(100, 100, 100);
                Color levelColor = (playerState != null && playerState.Level >= effectiveRequiredLevel) ? (mapInfo.CanMove ? new Color(180, 220, 255) : new Color(90, 90, 90)) : Color.Red;
                Color zenColor = (playerState != null && playerState.InventoryZen >= mapInfo.RequiredZen) ? (mapInfo.CanMove ? new Color(180, 255, 180) : new Color(90, 90, 90)) : Color.Red;
                if (mapInfo.IsStrifeMap && !mapInfo.CanMove) nameColor = new Color(120, 60, 60);


                var nameLabel = new LabelControl
                {
                    X = PADDING_LEFT + COL_MAP_NAME_X,
                    Y = currentY,
                    Text = mapInfo.DisplayName,
                    FontSize = 9.5f,
                    TextColor = nameColor,
                    ControlSize = new Point(COL_LEVEL_X - COL_MAP_NAME_X - 5, ITEM_HEIGHT - 1)
                };
                Controls.Add(nameLabel); _mapNameLabels.Add(nameLabel);

                // Wymagany Poziom
                var levelLabel = new LabelControl
                {
                    X = PADDING_LEFT + COL_LEVEL_X,
                    Y = currentY,
                    Text = effectiveRequiredLevel.ToString(),
                    FontSize = 9.5f,
                    TextColor = levelColor,
                    ControlSize = new Point(COL_ZEN_X - COL_LEVEL_X - 5, ITEM_HEIGHT - 1)
                };
                Controls.Add(levelLabel); _mapLevelLabels.Add(levelLabel);

                var zenLabel = new LabelControl
                {
                    X = PADDING_LEFT + COL_ZEN_X,
                    Y = currentY,
                    Text = mapInfo.RequiredZen.ToString(),
                    FontSize = 9.5f,
                    TextColor = zenColor,
                    ControlSize = new Point(mapContentWidth - COL_ZEN_X, ITEM_HEIGHT - 1)
                };
                Controls.Add(zenLabel); _mapZenLabels.Add(zenLabel);
            }
        }

        private void OnMapClicked(int visualIndex)
        {
            int actualMapIndexInList = _currentScrollOffset + visualIndex;
            if (actualMapIndexInList < _currentlyDisplayableMaps.Count)
            {
                var mapInfo = _currentlyDisplayableMaps[actualMapIndexInList];
                if (mapInfo.CanMove)
                {
                    _logger.LogInformation($"Warp to map index: {mapInfo.Index} ({mapInfo.DisplayName}) requested by player.");
                    // Pass both index and display name so listeners know the exact map
                    MapWarpRequested?.Invoke(mapInfo.Index, mapInfo.DisplayName);
                    Visible = false;
                }
                else
                {
                    _logger.LogInformation($"Cannot warp to {mapInfo.DisplayName}, requirements not met.");
                    // error sound?
                }
            }
        }


        private void UpdateScrollbarState()
        {
            bool needsScrollbar = _currentlyDisplayableMaps.Count > _maxVisibleItems;

            if (_scrollBar.Visible != needsScrollbar)
            {
                _scrollBar.Visible = needsScrollbar;
                RefreshMapListUI();
            }

            if (needsScrollbar)
            {
                _scrollBar.Min = 0;
                _scrollBar.Max = Math.Max(0, _currentlyDisplayableMaps.Count - _maxVisibleItems);
                _scrollBar.Value = _currentScrollOffset;
                _scrollBar.SetListMetrics(_maxVisibleItems, _currentlyDisplayableMaps.Count); // Przekaż aktualne metryki
                                                                                              // _logger.LogDebug($"Scrollbar updated: Min: {_scrollBar.Min}, Max: {_scrollBar.Max}, Value: {_scrollBar.Value}");
            }
        }

        private void OnScrollChanged()
        {
            if (_currentScrollOffset != _scrollBar.Value)
            {
                _currentScrollOffset = _scrollBar.Value;
                RefreshMapListUI();
            }
        }

        public void ToggleVisibility()
        {
            Visible = !Visible;
            if (Visible)
            {
                _logger.LogDebug("MoveCommandWindow toggled ON. Refreshing map data.");
                _currentScrollOffset = 0;
                if (_scrollBar != null) _scrollBar.Value = 0;
                LoadMapData();
                BringToFront();
                Scene.FocusControl = this;
            }
            else
            {
                if (Scene?.FocusControl == this) Scene.FocusControl = null;
            }
        }

        private CharacterState playerState => _networkManager.GetCharacterState();

        public int PADDING_BOTTOM = 30;
        public int PADDING_LEFT = 30;
        public int PADDING_RIGHT = 30;
        public int PADDING_TOP_CONTENT = 30;

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            if(UiThemeManager.IsMobileInterfaceActive){UpdateMobileDropdown();return;}
            base.Update(gameTime);

            var mouse = MuGame.Instance.UiMouseState;
            if (IsMouseOver && (mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed))
            {
                Scene?.SetMouseInputConsumed();
            }

            var playerState = _networkManager.GetCharacterState();
            bool listNeedsVisualRefresh = false;
            for (int i = 0; i < _currentlyDisplayableMaps.Count; ++i)
            {
                var map = _currentlyDisplayableMaps[i];
                bool canMoveNow = CanPlayerMoveTo(map, playerState);
                if (map.CanMove != canMoveNow)
                {
                    map.CanMove = canMoveNow;
                    _currentlyDisplayableMaps[i] = map;
                    listNeedsVisualRefresh = true;
                }
            }

            int oldHoveredMapIndex = _hoveredMapIndex;
            _hoveredMapIndex = -1;

            for (int i = 0; i < _mapClickAreas.Count; i++)
            {
                if (_mapClickAreas[i].Contains(mouse.Position))
                {
                    _hoveredMapIndex = _currentScrollOffset + i;
                    if (mouse.LeftButton == ButtonState.Pressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released)
                    {
                        OnMapClicked(i);
                        if (Scene is BaseScene baseScene) baseScene.SetMouseInputConsumed(); // signal consumption
                        return;
                    }
                    break;
                }
            }

            if (oldHoveredMapIndex != _hoveredMapIndex || listNeedsVisualRefresh)
            {
                if (oldHoveredMapIndex != -1 && oldHoveredMapIndex >= _currentScrollOffset && (oldHoveredMapIndex - _currentScrollOffset) < _mapNameLabels.Count)
                {
                    int visualIdx = oldHoveredMapIndex - _currentScrollOffset;
                    if (visualIdx >= 0 && visualIdx < _currentlyDisplayableMaps.Count)
                    {
                        var mapInfo = _currentlyDisplayableMaps[oldHoveredMapIndex];
                        mapInfo.IsSelected = false;
                        _currentlyDisplayableMaps[oldHoveredMapIndex] = mapInfo;
                        _mapNameLabels[visualIdx].TextColor = mapInfo.CanMove ? Color.WhiteSmoke : (mapInfo.IsStrifeMap ? new Color(120, 60, 60) : new Color(100, 100, 100));

                    }
                }
                if (_hoveredMapIndex != -1 && _hoveredMapIndex >= _currentScrollOffset && (_hoveredMapIndex - _currentScrollOffset) < _mapNameLabels.Count)
                {
                    int visualIdx = _hoveredMapIndex - _currentScrollOffset;
                    if (visualIdx >= 0 && visualIdx < _currentlyDisplayableMaps.Count)
                    {
                        var mapInfo = _currentlyDisplayableMaps[_hoveredMapIndex];
                        mapInfo.IsSelected = true;
                        _currentlyDisplayableMaps[_hoveredMapIndex] = mapInfo;
                        _mapNameLabels[visualIdx].TextColor = mapInfo.CanMove ? Color.Gold : _mapNameLabels[visualIdx].TextColor; // Podświetl na złoto jeśli można
                    }
                }
                else if (listNeedsVisualRefresh)
                {
                    RefreshMapListUI();
                }
            }
        }

        public override bool ProcessMouseScroll(int scrollDelta)
        {
            if (!Visible || !IsMouseOver) // only process if visible and mouse is over this control
                return false;

            int scrollDirection = scrollDelta > 0 ? 1 : -1;

            if (_scrollBar != null && _scrollBar.Visible && _scrollBar.IsMouseOver)
            {
                // if the scrollbar handles it, then the MoveCommandWindow also considers it handled.
                if (_scrollBar.ProcessMouseScroll(scrollDirection))
                {
                    Scene?.SetMouseInputConsumed();
                    return true;
                }
            }
            // scrollbar didn't handle (or wasn't applicable), the MoveCommandWindow itself doesn't have other scrollable content here.
            // However, if it had its own direct scrollable list without a scrollbar, logic would go here.
            // For now, if mouse is over the window but not its scrollbar, and scroll happens,
            // we could argue it's still "consuming" it for this window context even if no direct action.
            // But return false if no action taken by the window itself.
            // For simplicity and to prevent pass-through if the window is clearly the focus:
            if (_scrollBar != null && _scrollBar.Visible)  // a scrollbar exists, assume scroll should be for it
            {
                _scrollBar.Value -= scrollDirection; // scrollDelta: positive for up, negative for down
                Scene?.SetMouseInputConsumed();
                return true;
            }

            return false; // window didn't use the scroll (e.g., no scrollbar visible)
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible) return;
            if(UiThemeManager.IsMobileInterfaceActive){DrawMobileDropdown();return;}
            base.Draw(gameTime);
        }

        private bool _mobilePressed,_mobileDragged;
        private int _mobilePressY,_mobileStartOffset;
        private const int MobileRowHeight=52;
        private void MobileDropdownLayout()
        {
            var map=TouchMenuControl.MobileMapRectangle;
            X=map.X;Y=map.Y;
            ControlSize=ViewSize=new Point(map.Width,Math.Max(100,Math.Min(520,UiScaler.WindowBounds.Bottom-Y-28)));
            _maxVisibleItems=Math.Max(1,(ViewSize.Y-25)/MobileRowHeight);
            foreach(var child in Controls)child.Visible=false;
        }
        private void UpdateMobileDropdown()
        {
            MobileDropdownLayout();
            var mouse=MuGame.Instance.UiMouseState;var previous=MuGame.Instance.PrevUiMouseState;
            if(mouse.LeftButton==ButtonState.Pressed && previous.LeftButton==ButtonState.Released)
            {
                if(!DisplayRectangle.Contains(mouse.Position))
                {
                    var map=TouchMenuControl.MobileMapRectangle;
                    if(new Rectangle(map.X,map.Y-34,map.Width,34).Contains(mouse.Position))return;
                    Visible=false;Scene?.SetMouseInputConsumed();return;
                }
                _mobilePressed=true;_mobileDragged=false;_mobilePressY=mouse.Y;_mobileStartOffset=_currentScrollOffset;
            }
            if(_mobilePressed)
            {
                Scene?.SetMouseInputConsumed();
                if(Math.Abs(mouse.Y-_mobilePressY)>8)_mobileDragged=true;
                if(_mobileDragged)_currentScrollOffset=Math.Clamp(_mobileStartOffset+(_mobilePressY-mouse.Y)/MobileRowHeight,0,Math.Max(0,_currentlyDisplayableMaps.Count-_maxVisibleItems));
                if(mouse.LeftButton==ButtonState.Released)
                {
                    _mobilePressed=false;
                    int row=(mouse.Y-Y-25)/MobileRowHeight;
                    if(!_mobileDragged && mouse.Y>=Y+25 && DisplayRectangle.Contains(mouse.Position) && row<_maxVisibleItems)
                    {FilterAndSortMaps();OnMapClicked(row);}
                }
            }
            if(DisplayRectangle.Contains(mouse.Position) && mouse.ScrollWheelValue!=previous.ScrollWheelValue)
            {
                _currentScrollOffset=Math.Clamp(_currentScrollOffset-Math.Sign(mouse.ScrollWheelValue-previous.ScrollWheelValue),0,Math.Max(0,_currentlyDisplayableMaps.Count-_maxVisibleItems));
                Scene?.SetMouseInputConsumed();
            }
        }
        private void DrawMobileDropdown()
        {
            MobileDropdownLayout();
            var batch=GraphicsManager.Instance.Sprite;var pixel=GraphicsManager.Instance.Pixel;
            using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,transform:UiScaler.SpriteTransform);
            batch.Draw(pixel,DisplayRectangle,new Color(0,0,0,235));
            MobileHudLayout.Text(batch,"Move to",new Rectangle(X,Y,ViewSize.X,24),13,new Color(235,200,125));
            for(int row=0;row<_maxVisibleItems && row+_currentScrollOffset<_currentlyDisplayableMaps.Count;row++)
            {
                var map=_currentlyDisplayableMaps[row+_currentScrollOffset];
                bool canMove=CanPlayerMoveTo(map,playerState);
                var bounds=new Rectangle(X+2,Y+25+row*MobileRowHeight,ViewSize.X-4,MobileRowHeight-3);
                batch.Draw(pixel,bounds,new Color(80,71,48));
                batch.Draw(pixel,new Rectangle(bounds.X+1,bounds.Y+1,bounds.Width-2,bounds.Height-2),new Color(7,9,10,245));
                int level=map.RequiredLevel;
                if(playerState.Class is CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster or CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor or CharacterClassNumber.RageFighter or CharacterClassNumber.FistMaster && level!=400)level=level*2/3;
                var color=canMove?new Color(235,200,125):Color.Gray;
                MobileHudLayout.Text(batch,map.DisplayName,new Rectangle(bounds.X,bounds.Y+3,bounds.Width,23),17,color);
                MobileHudLayout.Text(batch,$"Min. Lv {level:N0} | {map.RequiredZen:N0} Zen",new Rectangle(bounds.X,bounds.Y+27,bounds.Width,18),13,color);
            }
            if(_currentlyDisplayableMaps.Count>_maxVisibleItems)
            {
                int height=ViewSize.Y-26;
                int thumb=Math.Max(22,height*_maxVisibleItems/_currentlyDisplayableMaps.Count);
                int top=Y+25+(height-thumb)*_currentScrollOffset/Math.Max(1,_currentlyDisplayableMaps.Count-_maxVisibleItems);
                batch.Draw(pixel,new Rectangle(X+ViewSize.X-3,top,2,thumb),Color.Goldenrod);
            }
        }

        public void ProcessKeyInput(Keys key, bool isRepeated)
        {
            if (!Visible) return;

            if (key == Keys.Escape)
            {
                Visible = false;
                if (Scene?.FocusControl == this) Scene.FocusControl = null;
            }
            else if (_scrollBar.Visible)
            {
                if (key == Keys.Up) _scrollBar.Value--;
                else if (key == Keys.Down) _scrollBar.Value++;
                else if (key == Keys.PageUp) _scrollBar.Value -= _maxVisibleItems;
                else if (key == Keys.PageDown) _scrollBar.Value += _maxVisibleItems;
            }
        }
    }
}
