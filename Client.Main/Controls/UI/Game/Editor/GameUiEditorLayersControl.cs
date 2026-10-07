using System;
using System.Collections.Generic;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Layouts;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Editor
{
    internal sealed class GameUiEditorLayersControl : UIControl
    {
        private readonly List<UiLayoutElement> _elements = new();
        private readonly List<LayerRow> _rows = new();
        private readonly List<(UiLayoutElement Element, int Depth, int SiblingTopIndex, int SiblingCount)> _displayRows = new();
        private readonly TextFieldControl _searchField;
        private UiLayoutElement _selected;
        private HashSet<string> _selectedIds = new(StringComparer.Ordinal);
        private HashSet<string> _visibleElementIds;
        private int _scrollOffset;
        private const int RowHeight = 28;
        private const int RowsTop = 86;
        private const int BottomPadding = 24;

        public event Action<UiLayoutElement, bool, bool> Selected;
        public event Action<UiLayoutElement> VisibilityToggleRequested;
        public event Action<UiLayoutElement> LockToggleRequested;
        public event Action<UiLayoutElement, int> ReorderRequested;
        public event Action<UiLayoutElement> BringToFrontRequested;
        public event Action<UiLayoutElement> ForwardRequested;
        public event Action<UiLayoutElement> BackwardRequested;
        public event Action<UiLayoutElement> SendToBackRequested;

        private int VisibleRowCount => Math.Max(1, (ControlSize.Y - RowsTop - BottomPadding) / RowHeight);

        public GameUiEditorLayersControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(240, 276);
            ViewSize = ControlSize;
            Interactive = true;
            BackgroundColor = new Color(12, 16, 24, 248);
            BorderColor = ModernHudTheme.BorderInner;
            BorderThickness = 1;

            AddActionButton("Front", 4, 28, 46, () => BringToFrontRequested?.Invoke(_selected));
            AddActionButton("Forward", 52, 28, 56, () => ForwardRequested?.Invoke(_selected));
            AddActionButton("Back", 110, 28, 46, () => BackwardRequested?.Invoke(_selected));
            AddActionButton("Send Back", 158, 28, 78, () => SendToBackRequested?.Invoke(_selected));

            _searchField = new ScrollAwareTextFieldControl(ProcessMouseScroll)
            {
                X = 8,
                Y = 54,
                ControlSize = new Point(224, 24),
                ViewSize = new Point(224, 24),
                FontSize = 8f,
                TextColor = ModernHudTheme.TextWhite,
                BackgroundColor = ModernHudTheme.BgDarkest,
                BorderColor = ModernHudTheme.BorderInner,
                Placeholder = "Search objects by name or type..."
            };
            _searchField.ValueChanged += (_, _) =>
            {
                _scrollOffset = 0;
                RebuildRows();
            };
            Controls.Add(_searchField);
        }

        public void SetElements(IReadOnlyList<UiLayoutElement> elements, UiLayoutElement selected)
        {
            _selected = selected;
            _selectedIds = selected == null || string.IsNullOrWhiteSpace(selected.Id)
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(new[] { selected.Id }, StringComparer.Ordinal);
            _elements.Clear();
            if (elements != null)
                _elements.AddRange(elements);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _elements.Count - VisibleRowCount));
            RebuildRows();
        }

        public void SetSelected(UiLayoutElement selected)
        {
            SetSelected(selected == null ? Array.Empty<UiLayoutElement>() : new[] { selected });
        }

        public void SetSelected(IReadOnlyList<UiLayoutElement> selected)
        {
            _selected = selected?.LastOrDefault();
            _selectedIds = new HashSet<string>((selected ?? Array.Empty<UiLayoutElement>())
                .Where(item => !string.IsNullOrWhiteSpace(item.Id)).Select(item => item.Id), StringComparer.Ordinal);
            foreach (LayerRow row in _rows)
                row.SetSelected(_selectedIds.Contains(row.Data.Id) || IsSameElement(row.Data, _selected));
        }

        public void RefreshElement(UiLayoutElement data)
        {
            if ((_searchField?.Value?.Trim().Length ?? 0) > 0)
            {
                RebuildRows();
                return;
            }

            foreach (LayerRow row in _rows)
                if (IsSameElement(row.Data, data))
                    row.Refresh();
        }

        public override bool ProcessMouseScroll(int scrollDelta)
        {
            int direction = scrollDelta > 0 ? -1 : 1;
            int next = Math.Clamp(_scrollOffset + direction, 0, Math.Max(0, _displayRows.Count - VisibleRowCount));
            if (next == _scrollOffset)
                return false;

            _scrollOffset = next;
            RebuildRows();
            return true;
        }

        private void RebuildRows()
        {
            foreach (LayerRow row in _rows)
            {
                Controls.Remove(row);
                row.Dispose();
            }
            _rows.Clear();

            _displayRows.Clear();
            string query = _searchField?.Value?.Trim() ?? string.Empty;
            if (query.Length == 0)
            {
                _visibleElementIds = null;
            }
            else
            {
                _visibleElementIds = new HashSet<string>(StringComparer.Ordinal);
                var elementsById = _elements
                    .Where(element => !string.IsNullOrWhiteSpace(element.Id))
                    .GroupBy(element => element.Id, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                foreach (UiLayoutElement element in _elements)
                {
                    if (!MatchesSearch(element, query))
                        continue;

                    string currentId = element.Id;
                    var visited = new HashSet<string>(StringComparer.Ordinal);
                    while (!string.IsNullOrWhiteSpace(currentId) && visited.Add(currentId) && elementsById.TryGetValue(currentId, out UiLayoutElement current))
                    {
                        _visibleElementIds.Add(currentId);
                        currentId = current.ParentId;
                    }
                }
            }

            var childrenByParent = _elements
                .GroupBy(element => element.ParentId ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(element => element.Layer).ToList(), StringComparer.Ordinal);
            AppendHierarchyRows(string.Empty, 0, childrenByParent);

            _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _displayRows.Count - VisibleRowCount));
            int rowCount = Math.Min(VisibleRowCount, _displayRows.Count - _scrollOffset);
            for (int visibleIndex = 0; visibleIndex < rowCount; visibleIndex++)
            {
                var displayRow = _displayRows[_scrollOffset + visibleIndex];
                UiLayoutElement data = displayRow.Element;
                var row = new LayerRow(data, data.Layer, displayRow.SiblingTopIndex, displayRow.SiblingCount, displayRow.Depth, ProcessMouseScroll)
                {
                    X = 8,
                    Y = RowsTop + visibleIndex * RowHeight
                };
                row.Selected += OnRowSelected;
                row.VisibilityToggleRequested += data => VisibilityToggleRequested?.Invoke(data);
                row.LockToggleRequested += data => LockToggleRequested?.Invoke(data);
                row.ReorderRequested += (data, targetLayerIndex) => ReorderRequested?.Invoke(data, targetLayerIndex);
                Controls.Add(row);
                _rows.Add(row);
            }
        }

        private void AppendHierarchyRows(
            string parentId,
            int depth,
            IReadOnlyDictionary<string, List<UiLayoutElement>> childrenByParent)
        {
            if (!childrenByParent.TryGetValue(parentId, out List<UiLayoutElement> children))
                return;

            for (int index = 0; index < children.Count; index++)
            {
                UiLayoutElement element = children[index];
                if (_visibleElementIds == null || _visibleElementIds.Contains(element.Id ?? string.Empty))
                    _displayRows.Add((element, depth, index, children.Count));
                AppendHierarchyRows(element.Id ?? string.Empty, depth + 1, childrenByParent);
            }
        }

        private static bool MatchesSearch(UiLayoutElement element, string query) =>
            (element.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (element.Type?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (element.SourceControlType?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

        private void AddActionButton(string text, int x, int y, int width, Action action)
        {
            var button = MakeButton(text, x, y, width, 22, ProcessMouseScroll);
            button.FontSize = 6.5f;
            button.Click += (_, _) =>
            {
                if (_selected != null)
                    action();
            };
            Controls.Add(button);
        }

        private void OnRowSelected(UiLayoutElement data, bool control, bool shift)
        {
            _selected = data;
            Selected?.Invoke(data, control, shift);
            if (!control && !shift)
                SetSelected(data);
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Update(gameTime);
            MouseState mouse = MuGame.Instance.UiMouseState;
            MouseState previous = MuGame.Instance.PrevUiMouseState;
            if (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released &&
                _searchField.DisplayRectangle.Contains(mouse.Position))
            {
                _searchField.Focus();
                Scene?.SetMouseInputConsumed();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible) return;
            base.Draw(gameTime);
            var sprite = GraphicsManager.Instance.Sprite;
            var pixel = GraphicsManager.Instance.Pixel;
            var font = GraphicsManager.GetUiFont(11f, out float scale) ?? GraphicsManager.Instance.Font;
            if (sprite == null || font == null) return;

            Rectangle bounds = DisplayRectangle;
            sprite.DrawString(font, "LAYERS / OBJECTS", new Vector2(bounds.X + 12, bounds.Y + 8), ModernHudTheme.TextGold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            string listingStatus = _searchField.Value.Trim().Length == 0
                ? $"{_displayRows.Count} objects"
                : $"{_displayRows.Count} items shown";
            sprite.DrawString(font, listingStatus, new Vector2(bounds.X + 132, bounds.Y + 10), ModernHudTheme.TextGray, 0f, Vector2.Zero, scale * 0.62f, SpriteEffects.None, 0f);
            if (pixel != null)
            {
                sprite.Draw(pixel, new Rectangle(bounds.X + 8, bounds.Y + 82, bounds.Width - 16, 1), ModernHudTheme.BorderInner);
                sprite.Draw(pixel, new Rectangle(bounds.X + 8, bounds.Bottom - 22, bounds.Width - 16, 1), ModernHudTheme.BorderInner);
            }
            if (_displayRows.Count == 0)
                sprite.DrawString(font, "No matching objects", new Vector2(bounds.X + 18, bounds.Y + RowsTop + 8), ModernHudTheme.TextGray, 0f, Vector2.Zero, scale * 0.75f, SpriteEffects.None, 0f);
            sprite.DrawString(font, "↓ BOTTOM", new Vector2(bounds.X + 12, bounds.Bottom - 18), ModernHudTheme.TextGray, 0f, Vector2.Zero, scale * 0.65f, SpriteEffects.None, 0f);
        }

        private static bool IsSameElement(UiLayoutElement left, UiLayoutElement right)
        {
            return ReferenceEquals(left, right) || (left != null && right != null && string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase));
        }

        private static ScrollAwareButtonControl MakeButton(string text, int x, int y, int width, int height, Func<int, bool> scrollHandler)
        {
            return new ScrollAwareButtonControl(scrollHandler)
            {
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(width, height),
                ViewSize = new Point(width, height),
                AutoViewSize = false,
                FontSize = width < 50 ? 7.5f : 8.5f,
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold,
                BackgroundColor = ModernHudTheme.BgMid,
                HoverBackgroundColor = ModernHudTheme.BgLight,
                PressedBackgroundColor = ModernHudTheme.BgDark
            };
        }

        private sealed class ScrollAwareTextFieldControl : TextFieldControl
        {
            private readonly Func<int, bool> _scrollHandler;

            public ScrollAwareTextFieldControl(Func<int, bool> scrollHandler)
            {
                _scrollHandler = scrollHandler;
            }

            public override bool ProcessMouseScroll(int scrollDelta) => _scrollHandler?.Invoke(scrollDelta) ?? false;
        }

        private sealed class ScrollAwareButtonControl : ButtonControl
        {
            private readonly Func<int, bool> _scrollHandler;

            public ScrollAwareButtonControl(Func<int, bool> scrollHandler)
            {
                _scrollHandler = scrollHandler;
            }

            public override bool ProcessMouseScroll(int scrollDelta) => _scrollHandler?.Invoke(scrollDelta) ?? false;
        }

        private sealed class LayerRow : UIControl
        {
            public UiLayoutElement Data { get; }
            private readonly ButtonControl _selectButton;
            private readonly ButtonControl _visibilityButton;
            private readonly ButtonControl _lockButton;
            private readonly int _layerIndex;
            private readonly int _siblingTopIndex;
            private readonly int _siblingCount;
            private readonly int _depth;
            private readonly Func<int, bool> _scrollHandler;
            private bool _dragging;
            private int _dragStartY;
            private const int RowWidth = 224;

            public event Action<UiLayoutElement, bool, bool> Selected;
            public event Action<UiLayoutElement> VisibilityToggleRequested;
            public event Action<UiLayoutElement> LockToggleRequested;
            public event Action<UiLayoutElement, int> ReorderRequested;

            public LayerRow(UiLayoutElement data, int layerIndex, int siblingTopIndex, int siblingCount, int depth, Func<int, bool> scrollHandler)
            {
                Data = data;
                _scrollHandler = scrollHandler;
                _layerIndex = layerIndex;
                _siblingTopIndex = siblingTopIndex;
                _siblingCount = siblingCount;
                _depth = depth;
                AutoViewSize = false;
                ControlSize = new Point(RowWidth, RowHeight - 2);
                ViewSize = ControlSize;
                Interactive = true;

                _visibilityButton = MakeButton(data.Visible ? "ON" : "OFF", 0, 2, 34, 22, _scrollHandler);
                _visibilityButton.Click += (_, _) => VisibilityToggleRequested?.Invoke(Data);
                Controls.Add(_visibilityButton);

                _lockButton = MakeButton(data.Locked ? "LK" : "--", 38, 2, 34, 22, _scrollHandler);
                _lockButton.Click += (_, _) => LockToggleRequested?.Invoke(Data);
                Controls.Add(_lockButton);

                _selectButton = MakeButton(string.Empty, 76, 2, 144, 22, _scrollHandler);
                _selectButton.Click += (_, _) =>
                {
                    KeyboardState keyboard = MuGame.Instance.Keyboard;
                    bool control = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
                    bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
                    Selected?.Invoke(Data, control, shift);
                };
                Controls.Add(_selectButton);
                Refresh();
                SetSelected(false);
            }

            private static ScrollAwareButtonControl MakeButton(string text, int x, int y, int width, int height, Func<int, bool> scrollHandler)
            {
                return GameUiEditorLayersControl.MakeButton(text, x, y, width, height, scrollHandler);
            }

            public void Refresh()
            {
                _visibilityButton.Text = Data.Visible ? "ON" : "OFF";
                _visibilityButton.Interactive = Data.CanEditVisibility;
                _lockButton.Text = Data.Locked ? "LK" : "--";
                _selectButton.Text = $"{new string(' ', Math.Min(8, _depth * 2))}☰ {_layerIndex}: {Data.Name ?? Data.Type}";
            }

            public void SetSelected(bool selected)
            {
                _selectButton.BackgroundColor = selected ? ModernHudTheme.SlotSelected : ModernHudTheme.BgMid;
            }

            public override bool ProcessMouseScroll(int scrollDelta) => _scrollHandler?.Invoke(scrollDelta) ?? false;

            public override void Update(GameTime gameTime)
            {
                if (!Visible) return;
                base.Update(gameTime);

                MouseState mouse = MuGame.Instance.UiMouseState;
                MouseState previous = MuGame.Instance.PrevUiMouseState;
                bool pressed = mouse.LeftButton == ButtonState.Pressed;
                Rectangle rowBounds = DisplayRectangle;

                if (pressed && previous.LeftButton == ButtonState.Released && rowBounds.Contains(mouse.Position))
                {
                    // Drag only from the name/handle area so visibility and lock clicks stay discrete.
                    if (mouse.Position.X >= rowBounds.X + 76)
                    {
                        _dragging = true;
                        _dragStartY = mouse.Position.Y;
                    }
                    Scene?.SetMouseInputConsumed();
                }

                if (_dragging && pressed)
                    Scene?.SetMouseInputConsumed();

                if (_dragging && !pressed && previous.LeftButton == ButtonState.Pressed)
                {
                    int delta = mouse.Position.Y - _dragStartY;
                    int steps = (int)Math.Round(delta / (double)RowHeight, MidpointRounding.AwayFromZero);
                    if (steps != 0)
                    {
                        int targetTopIndex = Math.Clamp(_siblingTopIndex + steps, 0, _siblingCount - 1);
                        int targetLayerIndex = _siblingCount - 1 - targetTopIndex;
                        if (targetLayerIndex != _layerIndex)
                            ReorderRequested?.Invoke(Data, targetLayerIndex);
                    }
                    _dragging = false;
                    Scene?.SetMouseInputConsumed();
                }
            }
        }
    }
}
