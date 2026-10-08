using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Helper;
using Client.Main.Controls.UI.Game.Layouts;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Editor
{
    internal sealed class GameUiEditorCanvasControl : UIControl
    {
        private sealed class RuntimeElement
        {
            public UiLayoutElement Data;
            public GameControl Control;
            public string LoadedAsset;
        }

        private readonly List<RuntimeElement> _elements = new();
        private readonly List<RuntimeElement> _selection = new();
        private readonly HashSet<string> _liveElementIds = new(StringComparer.Ordinal);
        private readonly GameUiEditorHistory _history = new();
        private RuntimeElement _selected;
        private GameUiEditorDocumentState _gestureBefore;
        private Dictionary<string, Rectangle> _gestureStartBounds;
        private bool _restoringState;
        private GameControl _sourceRoot;
        private IGameUiDesignerRuntimeSource _runtimeSource;
        private IReadOnlyList<GameUiDesignerPageDefinition> _pages = Array.Empty<GameUiDesignerPageDefinition>();
        private string _currentPageId = "root";
        private object _designerState;
        private bool _sourceMode;
        private bool _moving;
        private bool _resizing;
        private bool _selecting;
        private Point _selectionStart;
        private Rectangle _selectionRect;
        private bool _resizeFromLeft;
        private bool _resizeFromTop;
        private Point _pointerStart;
        private Point _elementStart;
        private Point _sizeStart;
        private string _pendingDropPath;
        private readonly List<UiLayoutElement> _clipboard = new();
        private float _zoom = 1f;
        private int _panX;
        private int _panY;
        private bool _gridEnabled = true;
        private bool _snapEnabled = true;
        private int _gridSize = 5;
        private readonly List<int> _verticalGuides = new();
        private readonly List<int> _horizontalGuides = new();
        private bool _guideDragging;
        private bool _guideVertical;
        private int _guideCoordinate;
        private bool _panning;
        private Point _panStart;
        private int _panOriginX;
        private int _panOriginY;
        private int _previousScrollWheel;
        private int _documentLoadGeneration;
        private const int HandleSize = 12;
        private const int MoveHandleSize = 20;
        private const int MoveHandleGap = 4;
        private const int MinimumSize = 8;

        public event Action<UiLayoutElement> ElementSelected;
        public event Action<IReadOnlyList<UiLayoutElement>> SelectionChanged;
        public event Action<UiLayoutElement> ElementChanged;
        public event Action ElementsChanged;
        public event Action<string> DocumentLoadWarning;
        public event Action<bool> DirtyStateChanged;
        public event Action HistoryStateChanged;
        public event Action SaveRequested;
        public UiLayoutElement SelectedData => _selected?.Data;
        public IReadOnlyList<UiLayoutElement> SelectedElements => _selection.Select(item => item.Data).ToList();
        public bool CanUndo => _history.CanUndo;
        public bool CanRedo => _history.CanRedo;
        public bool IsDirty => _history.IsDirty(CaptureDocumentState());
        public int ZoomPercent => (int)Math.Round(_zoom * 100f);
        public bool GridEnabled => _gridEnabled;
        public int GridSize => _gridSize;

        public void SetZoom(int percent)
        {
            int[] allowed = { 25, 50, 75, 100, 150, 200, 400, 800 };
            int nearest = allowed.OrderBy(value => Math.Abs(value - percent)).First();
            _zoom = nearest / 100f;
            ApplyViewportTransform();
            ElementsChanged?.Invoke();
        }

        public void ToggleGrid() { _gridEnabled = !_gridEnabled; }
        public void ToggleSnap() { _snapEnabled = !_snapEnabled; }
        public void SetGridSize(int size) { _gridSize = Math.Clamp(size, 1, 64); }
        public void PanBy(int dx, int dy)
        {
            _panX += dx;
            _panY += dy;
            ApplyViewportTransform();
        }

        public void AddVerticalGuide(int coordinate)
        {
            if (!_verticalGuides.Contains(coordinate)) _verticalGuides.Add(coordinate);
        }

        public void AddHorizontalGuide(int coordinate)
        {
            if (!_horizontalGuides.Contains(coordinate)) _horizontalGuides.Add(coordinate);
        }

        public void ClearGuides()
        {
            _verticalGuides.Clear();
            _horizontalGuides.Clear();
        }
        public IReadOnlyList<UiLayoutElement> Elements => _elements
            .Where(item => IsVisibleOnCurrentDesignerPage(item.Data))
            .Select(item => item.Data)
            .ToList();

        private bool IsVisibleOnCurrentDesignerPage(UiLayoutElement data) =>
            !_sourceMode || string.Equals(data.DesignerPageId, "root", StringComparison.Ordinal) ||
            string.Equals(data.DesignerPageId, _currentPageId, StringComparison.Ordinal);

        public GameUiEditorCanvasControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(600, 610);
            ViewSize = ControlSize;
            Interactive = true;
            BackgroundColor = new Color(14, 19, 28, 245);
            BorderColor = ModernHudTheme.BorderInner;
            BorderThickness = 1;
        }

        public bool IsSourceMode => _sourceMode;
        public GameControl SourceRoot => _sourceRoot;
        public IReadOnlyList<GameUiDesignerPageDefinition> Pages => _pages;
        public string CurrentPageId => _currentPageId;
        public string CurrentPageName => _pages.FirstOrDefault(page => string.Equals(page.Id, _currentPageId, StringComparison.Ordinal))?.Name ?? "Root / Shared";

        public void BindLiveSourceTree(IGameUiDesignerRuntimeSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (_sourceMode)
                ReleaseLiveSourceTree();
            ClearOwnedElements();
            _liveElementIds.Clear();

            _runtimeSource = source;
            _pages = source.GetVisualDesignerPages() ?? Array.Empty<GameUiDesignerPageDefinition>();
            _currentPageId = _pages.FirstOrDefault(page => string.Equals(page.Id, source.DesignerDefaultPageId, StringComparison.Ordinal))?.Id
                             ?? _pages.FirstOrDefault()?.Id
                             ?? "root";
            _designerState = source.CaptureVisualDesignerState();
            source.ActivateVisualDesignerPage(_currentPageId);
            source.SetVisualDesignerEditing(true);
            _sourceRoot = _pages.FirstOrDefault(page => page.IsWindowRoot)?.Root ?? _pages.FirstOrDefault()?.Root;
            if (_sourceRoot == null)
                throw new InvalidOperationException("The OpenMU UI source did not expose a root control for the designer.");

            _sourceMode = true;
            _sourceRoot.X = 0;
            _sourceRoot.Y = 0;
            _sourceRoot.Visible = true;
            _sourceRoot.Interactive = false;
            Controls.Add(_sourceRoot);
            AddLiveTreeElements(_sourceRoot, null, string.Empty);
            ApplyViewportTransform();
            _history.Reset(CaptureDocumentState());
            NotifyHistoryStateChanged();
            ElementsChanged?.Invoke();
            Select(null);
        }

        public GameControl ReleaseLiveSourceTree()
        {
            if (!_sourceMode)
                return null;

            GameControl root = _sourceRoot;
            _zoom = 1f;
            _panX = 0;
            _panY = 0;
            ApplyViewportTransform();
            if (ReferenceEquals(root?.Parent, this))
                Controls.Remove(root);
            if (_runtimeSource != null && _designerState != null)
            {
                _runtimeSource.RestoreVisualDesignerState(_designerState);
                _runtimeSource.SetVisualDesignerEditing(false);
            }
            _runtimeSource = null;
            _pages = Array.Empty<GameUiDesignerPageDefinition>();
            _sourceRoot = null;
            _sourceMode = false;
            _elements.Clear();
            _liveElementIds.Clear();
            _selection.Clear();
            _selected = null;
            NotifySelectionChanged();
            ElementsChanged?.Invoke();
            return root;
        }

        public void SelectPage(string pageId)
        {
            if (!_sourceMode || _runtimeSource == null || string.IsNullOrWhiteSpace(pageId) ||
                !_pages.Any(page => string.Equals(page.Id, pageId, StringComparison.Ordinal)))
                return;
            _runtimeSource.ActivateVisualDesignerPage(pageId);
            _currentPageId = pageId;
            RebuildLivePageElements();
        }

        private void RebuildLivePageElements()
        {
            _elements.Clear();
            _liveElementIds.Clear();
            _selection.Clear();
            _selected = null;
            AddLiveTreeElements(_sourceRoot, null, string.Empty);
            ApplyViewportTransform();
            NotifySelectionChanged();
            ElementsChanged?.Invoke();
        }

        public UiLayoutDocument CreateSourceDocument(string name)
        {
            if (!_sourceMode)
                throw new InvalidOperationException("Open a C# UI source before saving C# layout changes.");

            return new UiLayoutDocument
            {
                Name = name,
                Width = ControlSize.X,
                Height = ControlSize.Y,
                Elements = _elements.Select(item => item.Data).ToList()
            };
        }

        private void ApplyViewportTransform()
        {
            if (!_sourceMode || _sourceRoot == null)
                return;
            _viewportTransforming = true;
            try
            {
                foreach (RuntimeElement item in _elements)
                {
                    UiLayoutElement data = item.Data;
                    if (ReferenceEquals(item.Control, _sourceRoot))
                    {
                        item.Control.X = _panX + (int)Math.Round(data.X * _zoom);
                        item.Control.Y = _panY + (int)Math.Round(data.Y * _zoom);
                    }
                    else
                    {
                        item.Control.X = (int)Math.Round(data.X * _zoom);
                        item.Control.Y = (int)Math.Round(data.Y * _zoom);
                    }
                    Point size = new(
                        Math.Max(MinimumSize, (int)Math.Round(data.Width * _zoom)),
                        Math.Max(MinimumSize, (int)Math.Round(data.Height * _zoom)));
                    item.Control.ViewSize = size;
                    item.Control.ControlSize = size;
                }
                UiLayoutElement rootData = _elements.FirstOrDefault(item => ReferenceEquals(item.Control, _sourceRoot))?.Data;
                if (rootData != null)
                {
                    Point rootSize = new(Math.Max(MinimumSize, (int)Math.Round(rootData.Width * _zoom)),
                        Math.Max(MinimumSize, (int)Math.Round(rootData.Height * _zoom)));
                    _sourceRoot.ViewSize = rootSize;
                    _sourceRoot.ControlSize = rootSize;
                }
            }
            finally
            {
                _viewportTransforming = false;
            }
        }

        private bool _viewportTransforming;

        private int ToLogical(int value) => (int)Math.Round(value / _zoom);

        internal GameUiEditorDocumentState CaptureDocumentState()
        {
            if (!_viewportTransforming && Math.Abs(_zoom - 1f) < 0.001f && _panX == 0 && _panY == 0)
                foreach (RuntimeElement item in _elements)
                    SyncDataFromControl(item, notify: false);

            return new GameUiEditorDocumentState
            {
                Name = _sourceMode ? _runtimeSource?.DesignerClassName : "Untitled",
                Width = ControlSize.X,
                Height = ControlSize.Y,
                Elements = _elements.Select(item => GameUiEditorDocumentState.CloneElement(item.Data)).ToList(),
                SelectedIds = _selection.Select(item => item.Data.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToList()
            };
        }

        private void NotifyHistoryStateChanged()
        {
            DirtyStateChanged?.Invoke(IsDirty);
            HistoryStateChanged?.Invoke();
        }

        private bool CommitHistory(GameUiEditorDocumentState before, string description, string coalesceKey = null)
        {
            if (_restoringState || before == null)
                return false;

            bool committed = _history.Commit(before, CaptureDocumentState(), description, coalesceKey);
            if (committed)
                NotifyHistoryStateChanged();
            return committed;
        }

        public void MarkSaved()
        {
            _history.MarkSaved(CaptureDocumentState());
            NotifyHistoryStateChanged();
        }

        public void Undo()
        {
            if (!_history.TryUndo(out GameUiEditorDocumentState state))
                return;
            RestoreDocumentState(state);
        }

        public void Redo()
        {
            if (!_history.TryRedo(out GameUiEditorDocumentState state))
                return;
            RestoreDocumentState(state);
        }

        private void RestoreDocumentState(GameUiEditorDocumentState state)
        {
            if (state == null || _restoringState)
                return;

            _restoringState = true;
            try
            {
                List<UiLayoutElement> restoreElements = state.Elements
                    .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                    .GroupBy(item => item.Id, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .ToList();
                if (restoreElements.Count != state.Elements.Count)
                    DocumentLoadWarning?.Invoke("Undo history contained duplicate or empty object IDs. Duplicate entries were skipped to keep the editor running.");

                var targetById = restoreElements.ToDictionary(item => item.Id, StringComparer.Ordinal);

                foreach (RuntimeElement item in _elements.ToArray())
                {
                    if (item.Data.IsDesignerAdded && !targetById.ContainsKey(item.Data.Id))
                    {
                        item.Control.Dispose();
                        _elements.Remove(item);
                    }
                }

                foreach (UiLayoutElement target in restoreElements)
                {
                    RuntimeElement runtime = _elements.FirstOrDefault(item => string.Equals(item.Data.Id, target.Id, StringComparison.Ordinal));
                    if (runtime == null && target.IsDesignerAdded)
                    {
                        UiLayoutElement copy = GameUiEditorDocumentState.CloneElement(target);
                        AddRuntime(copy, select: false);
                        runtime = _elements.FirstOrDefault(item => string.Equals(item.Data.Id, target.Id, StringComparison.Ordinal));
                    }
                    if (runtime == null)
                        continue;
                    ApplyElementState(runtime, target);
                }

                // Reapply each saved layer order to the actual control collection.
                // Merely refreshing indices here would overwrite the historical order
                // with the current (post-edit) visual order, making layer undo appear inert.
                SyncControlZOrder();
                _selection.Clear();
                foreach (string id in state.SelectedIds ?? new List<string>())
                {
                    RuntimeElement runtime = _elements.FirstOrDefault(item => string.Equals(item.Data.Id, id, StringComparison.Ordinal));
                    if (runtime != null && IsVisibleOnCurrentDesignerPage(runtime.Data))
                        _selection.Add(runtime);
                }
                _selected = _selection.LastOrDefault();
                ApplyViewportTransform();
                NotifySelectionChanged();
                ElementsChanged?.Invoke();
                NotifyHistoryStateChanged();
            }
            finally
            {
                _restoringState = false;
            }
        }

        private void ApplyElementState(RuntimeElement runtime, UiLayoutElement source)
        {
            UiLayoutElement data = runtime.Data;
            data.Name = source.Name;
            data.Asset = source.Asset;
            data.Text = source.Text;
            data.X = source.X;
            data.Y = source.Y;
            data.Width = source.Width;
            data.Height = source.Height;
            data.Opacity = source.Opacity;
            data.FontSize = source.FontSize;
            data.Layer = source.Layer;
            data.Visible = source.Visible;
            data.Locked = source.Locked;
            runtime.Control.Name = data.Name;
            runtime.Control.X = data.X;
            runtime.Control.Y = data.Y;
            Point size = new(Math.Max(MinimumSize, data.Width), Math.Max(MinimumSize, data.Height));
            runtime.Control.ViewSize = size;
            runtime.Control.ControlSize = size;
            runtime.Control.Visible = data.Visible;
            runtime.Control.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            if (runtime.Control is TextureControl texture)
                texture.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            if (runtime.Control is LabelControl label)
            {
                if (data.CanEditText) label.Text = data.Text ?? string.Empty;
                if (data.CanEditFontSize) label.FontSize = Math.Clamp(data.FontSize, 1f, 256f);
                label.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            }
            if (runtime.Control is ButtonControl button)
            {
                if (data.CanEditText) button.Text = data.Text ?? string.Empty;
                if (data.CanEditFontSize) button.FontSize = Math.Clamp(data.FontSize, 1f, 256f);
            }
            if (data.CanEditAsset && runtime.Control is SpriteControl sprite &&
                !string.Equals(runtime.LoadedAsset, data.Asset ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                _ = UpdateElementAssetAsync(runtime, sprite, data.Asset ?? string.Empty);
        }

        private void AddLiveTreeElements(GameControl control, string parentId, string sourcePath)
        {
            bool isRoot = ReferenceEquals(control, _sourceRoot);
            GameUiDesignerPageDefinition owningPage = FindOwningPage(control);
            bool hasSourceBinding = _runtimeSource.TryGetVisualDesignerBinding(control, out string savedId, out string savedPath);
            bool isDesignerAdded = hasSourceBinding && savedPath == null;
            string requestedId = hasSourceBinding
                ? savedId
                : (sourcePath.Length == 0 ? "muhelper-root" : "muhelper-live/" + sourcePath);
            string stableId = MakeUniqueLiveElementId(requestedId);
            string controlType = control.GetType().Name;
            string elementType = control is ButtonControl ? "button"
                : control is LabelControl ? "text"
                : control is SpriteControl ? "image"
                : control is UIControl ? "container"
                : "unsupported";
            string asset = control is TextureControl textureControl ? textureControl.TexturePath ?? string.Empty : string.Empty;
            string text = control switch
            {
                ButtonControl button => button.Text ?? string.Empty,
                LabelControl label => label.Text ?? string.Empty,
                _ => string.Empty
            };
            bool dynamicText = (control is ButtonControl || control is LabelControl) && _runtimeSource.IsVisualDesignerTextDynamic(control);
            bool dynamicVisibility = _runtimeSource.IsVisualDesignerVisibilityDynamic(control);
            bool assetEditable = _runtimeSource.IsVisualDesignerAssetEditable(control);
            string unsupported = dynamicText ? "Text is refreshed from gameplay state by MuHelperWindow."
                : dynamicVisibility ? "Visibility is controlled by the active Helper tab/state."
                : elementType == "unsupported" ? $"OpenMU control type '{controlType}' has no visual inspector adapter yet."
                : control is ButtonControl && control.GetType() != typeof(ButtonControl) ? $"Custom button '{controlType}' keeps its own behavior and renderer."
                : null;

            var data = new UiLayoutElement
            {
                Id = stableId,
                ParentId = parentId,
                // Source paths are runtime tree addresses. A generated descriptor can
                // be stale when conditional Helper presentations change the child order;
                // retain stable IDs where they still bind, but always save the current
                // live path so the next build rebinds this descriptor to this control.
                SourcePath = isDesignerAdded ? null : sourcePath,
                IsSourceBacked = !isDesignerAdded,
                IsDesignerAdded = isDesignerAdded,
                DesignerPageId = owningPage?.Id ?? "root",
                DesignerPageName = owningPage?.Name ?? "Root / Shared",
                Type = elementType,
                SourceControlType = controlType,
                Name = string.IsNullOrWhiteSpace(control.Name)
                    ? (string.IsNullOrWhiteSpace(text) ? controlType : text)
                    : control.Name,
                Asset = asset,
                Text = text,
                X = isRoot ? 0 : control.X,
                Y = isRoot ? 0 : control.Y,
                Width = control.ViewSize.X,
                Height = control.ViewSize.Y,
                Opacity = control is LabelControl textLabel ? textLabel.Alpha : control is TextureControl tex ? tex.Alpha : control.Alpha,
                FontSize = control is LabelControl labelControl ? labelControl.FontSize : control is ButtonControl buttonControl ? buttonControl.FontSize : 0,
                Layer = control.Parent == null ? 0 : Array.IndexOf(control.Parent.Controls.GetSnapshotArray(), control),
                Visible = control.Visible,
                Locked = isRoot,
                // The window root is a real layout object: it can be unlocked and
                // repositioned/resized as one unit, but it remains the protected host
                // for the source-backed control tree and cannot be hidden/reordered.
                CanEditGeometry = true,
                CanEditText = !dynamicText && (control is ButtonControl || control is LabelControl),
                CanEditAsset = assetEditable,
                CanEditOpacity = true,
                CanEditFontSize = !dynamicText && (control is ButtonControl || control is LabelControl),
                CanEditVisibility = !isRoot && !dynamicVisibility,
                CanReorder = !isRoot,
                UnsupportedReason = unsupported
            };
            _elements.Add(new RuntimeElement { Data = data, Control = control, LoadedAsset = asset });

            GameControl[] children = control.Controls.GetSnapshotArray();
            for (int i = 0; i < children.Length; i++)
            {
                string childPath = sourcePath.Length == 0 ? i.ToString(CultureInfo.InvariantCulture) : $"{sourcePath}/{i}";
                AddLiveTreeElements(children[i], stableId, childPath);
            }
        }

        private string MakeUniqueLiveElementId(string requestedId)
        {
            string baseId = string.IsNullOrWhiteSpace(requestedId) ? "muhelper-live/element" : requestedId;
            string candidate = baseId;
            int suffix = 2;
            while (!_liveElementIds.Add(candidate))
                candidate = $"{baseId}#{suffix++}";
            return candidate;
        }

        private GameUiDesignerPageDefinition FindOwningPage(GameControl control)
        {
            if (_pages == null || _pages.Count == 0)
                return null;
            if (ReferenceEquals(control, _sourceRoot))
                return _pages.FirstOrDefault(page => page.IsWindowRoot) ?? _pages.FirstOrDefault();

            for (GameControl current = control; current != null && !ReferenceEquals(current, _sourceRoot); current = current.Parent)
            {
                GameUiDesignerPageDefinition page = _pages.FirstOrDefault(candidate =>
                    !candidate.IsWindowRoot && ReferenceEquals(candidate.Root, current));
                if (page != null)
                    return page;
            }
            return _pages.FirstOrDefault(page => page.IsWindowRoot);
        }

        private void ClearOwnedElements()
        {
            foreach (RuntimeElement item in _elements.Where(item => !item.Data.IsSourceBacked &&
                         !item.Data.IsDesignerAdded && ReferenceEquals(item.Control.Parent, this)).ToArray())
            {
                if (item.Control.Status != GameControlStatus.Disposed)
                    item.Control.Dispose();
            }
            _elements.Clear();
            _selection.Clear();
            _selected = null;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
            {
                Scene?.Cursor?.SetEditorCursorMode(EditorCursorMode.Default);
                return;
            }
            base.Update(gameTime);
            KeyboardState keyboard = MuGame.Instance.Keyboard;
            KeyboardState previousKeyboard = MuGame.Instance.PrevKeyboard;
            HandleKeyboard(keyboard, previousKeyboard);

            MouseState mouse = MuGame.Instance.UiMouseState;
            MouseState previous = MuGame.Instance.PrevUiMouseState;
            Point pointer = mouse.Position;
            bool pressed = mouse.LeftButton == ButtonState.Pressed;
            bool justPressed = pressed && previous.LeftButton == ButtonState.Released;
            bool justReleased = !pressed && previous.LeftButton == ButtonState.Pressed;
            bool middlePressed = mouse.MiddleButton == ButtonState.Pressed;
            bool middleJustReleased = !middlePressed && previous.MiddleButton == ButtonState.Pressed;
            bool space = keyboard.IsKeyDown(Keys.Space);
            bool control = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            if (control && mouse.ScrollWheelValue != _previousScrollWheel)
                SetZoom(ZoomPercent + (mouse.ScrollWheelValue > _previousScrollWheel ? 25 : -25));
            _previousScrollWheel = mouse.ScrollWheelValue;

            if (justPressed && DisplayRectangle.Contains(pointer) && !middlePressed && !space &&
                (pointer.Y <= DisplayRectangle.Y + 10 || pointer.X <= DisplayRectangle.X + 10))
            {
                _guideDragging = true;
                _guideVertical = pointer.Y <= DisplayRectangle.Y + 10;
                _guideCoordinate = _guideVertical
                    ? ToLogical(pointer.X - DisplayRectangle.X - _panX)
                    : ToLogical(pointer.Y - DisplayRectangle.Y - _panY);
                Scene?.SetMouseInputConsumed();
            }
            if (_guideDragging && pressed)
            {
                _guideCoordinate = _guideVertical
                    ? ToLogical(pointer.X - DisplayRectangle.X - _panX)
                    : ToLogical(pointer.Y - DisplayRectangle.Y - _panY);
                Scene?.SetMouseInputConsumed();
            }
            if (justPressed && DisplayRectangle.Contains(pointer) && (middlePressed || (space && pressed)))
            {
                _panning = true;
                _panStart = pointer;
                _panOriginX = _panX;
                _panOriginY = _panY;
                Scene?.SetMouseInputConsumed();
            }
            if (_panning && (middlePressed || (space && pressed)))
            {
                _panX = _panOriginX + pointer.X - _panStart.X;
                _panY = _panOriginY + pointer.Y - _panStart.Y;
                ApplyViewportTransform();
                Scene?.SetMouseInputConsumed();
            }
            if (pressed && TryStartDrop(pointer))
            {
                Scene?.SetMouseInputConsumed();
            }
            else if (!_panning && justPressed && DisplayRectangle.Contains(pointer))
            {
                // Selection chrome owns its own hit targets. This keeps body clicks
                // selection-only while making the move handle the sole move target.
                if (TryGetMoveHandle(pointer, out _))
                {
                    if (GetMovableSelection().Count > 0)
                        BeginMoveGesture(pointer);
                    Scene?.SetMouseInputConsumed();
                }
                else if (_selection.Count == 1 && _selected != null &&
                         !_selected.Data.Locked && _selected.Data.CanEditGeometry &&
                         IsResizeHandle(_selected.Control, pointer, out _resizeFromLeft, out _resizeFromTop))
                {
                    BeginResizeGesture(pointer);
                    Scene?.SetMouseInputConsumed();
                }
                else if (TrySelect(pointer, out RuntimeElement element))
                {
                    if (!(!control && !shift && _selection.Count > 1 && _selection.Contains(element)))
                        Select(element, toggle: control, extend: shift && !control);
                    // Intentionally do not start a move gesture from the object body.
                    // A plain click selects; movement begins only from the move handle.
                    Scene?.SetMouseInputConsumed();
                }
                else if (!control && !shift)
                {
                    _gestureBefore = null;
                    _gestureStartBounds = null;
                    Select(null);
                    _selecting = true;
                    _selectionStart = pointer;
                    _selectionRect = new Rectangle(pointer.X, pointer.Y, 0, 0);
                    Scene?.SetMouseInputConsumed();
                }
            }

            if (pressed && _selecting)
            {
                _selectionRect = NormalizeRectangle(_selectionStart, pointer);
                Scene?.SetMouseInputConsumed();
            }

            if (pressed && (_moving || _resizing) && _selection.Count > 0)
            {
                int dx = ToLogical(pointer.X - _pointerStart.X);
                int dy = ToLogical(pointer.Y - _pointerStart.Y);
                if (_moving)
                {
                    List<RuntimeElement> movable = GetMovableSelection();
                    RuntimeElement anchor = movable.FirstOrDefault();
                    if (anchor != null && _gestureStartBounds.TryGetValue(anchor.Data.Id, out Rectangle anchorStart))
                    {
                        int snappedX = anchorStart.X + dx;
                        int snappedY = anchorStart.Y + dy;
                        SnapPosition(anchor, ref snappedX, ref snappedY, _selection);
                        int snappedDx = snappedX - anchorStart.X;
                        int snappedDy = snappedY - anchorStart.Y;
                        foreach (RuntimeElement item in movable)
                        {
                            Rectangle start = _gestureStartBounds[item.Data.Id];
                            int logicalX = start.X + snappedDx;
                            int logicalY = start.Y + snappedDy;
                            item.Control.X = ToDisplayX(item, logicalX);
                            item.Control.Y = ToDisplayY(item, logicalY);
                            SyncDataFromControl(item, notify: false);
                        }
                    }
                }
                else if (_selected != null)
                {
                    int width = Math.Max(MinimumSize, _sizeStart.X + (_resizeFromLeft ? -dx : dx));
                    int height = Math.Max(MinimumSize, _sizeStart.Y + (_resizeFromTop ? -dy : dy));
                    Point size = new(width, height);
                    _selected.Control.ViewSize = new Point(Math.Max(MinimumSize, (int)Math.Round(width * _zoom)), Math.Max(MinimumSize, (int)Math.Round(height * _zoom)));
                    _selected.Control.ControlSize = _selected.Control.ViewSize;
                    int logicalX = _elementStart.X + (_resizeFromLeft ? _sizeStart.X - width : 0);
                    int logicalY = _elementStart.Y + (_resizeFromTop ? _sizeStart.Y - height : 0);
                    _selected.Control.X = ToDisplayX(_selected, logicalX);
                    _selected.Control.Y = ToDisplayY(_selected, logicalY);
                    SyncDataFromControl(_selected, notify: false);
                }
                ElementChanged?.Invoke(_selected?.Data);
                Scene?.SetMouseInputConsumed();
            }

            if (justReleased || middleJustReleased)
            {
                if (_guideDragging)
                {
                    _guideDragging = false;
                    if (DisplayRectangle.Contains(pointer))
                    {
                        if (_guideVertical) AddVerticalGuide(_guideCoordinate);
                        else AddHorizontalGuide(_guideCoordinate);
                    }
                }
                _panning = false;
                if (_selecting)
                {
                    _selecting = false;
                    if (_selectionRect.Width >= 4 || _selectionRect.Height >= 4)
                        SelectInRectangle(_selectionRect, control, shift);
                }
                if (_gestureBefore != null)
                    // Each completed pointer gesture is one independent undo entry.
                    CommitHistory(_gestureBefore, _moving ? $"Move {_selection.Count} object(s)" : "Resize object");
                _gestureBefore = null;
                _gestureStartBounds = null;
                _moving = false;
                _resizing = false;
                if (GameUiEditorDragState.IsDragging && !DisplayRectangle.Contains(pointer)) GameUiEditorDragState.Cancel();
            }

            UpdateEditorCursor(pointer);
        }

        private void UpdateEditorCursor(Point pointer)
        {
            if (Scene?.Cursor == null)
                return;

            EditorCursorMode mode = EditorCursorMode.Default;
            if (_moving)
                mode = EditorCursorMode.Move;
            else if (_resizing)
                mode = _resizeFromLeft == _resizeFromTop
                    ? EditorCursorMode.ResizeDiagonalNWSE
                    : EditorCursorMode.ResizeDiagonalNESW;
            else if (TryGetMoveHandle(pointer, out _))
                mode = EditorCursorMode.Move;
            else if (_selection.Count == 1 && _selected != null && !_selected.Data.Locked &&
                     _selected.Data.CanEditGeometry && IsResizeHandle(_selected.Control, pointer, out bool fromLeft, out bool fromTop))
                mode = fromLeft == fromTop
                    ? EditorCursorMode.ResizeDiagonalNWSE
                    : EditorCursorMode.ResizeDiagonalNESW;

            Scene.Cursor.SetEditorCursorMode(mode);
        }

        private List<RuntimeElement> GetVisibleSelection() => _selection
            .Where(item => IsVisibleOnCurrentDesignerPage(item.Data) && item.Data.Visible && item.Control.Visible)
            .ToList();

        private List<RuntimeElement> GetMovableSelection() => GetVisibleSelection()
            .Where(item => item.Data.CanEditGeometry && !item.Data.Locked)
            .ToList();

        private bool TryGetMoveHandle(Point pointer, out Rectangle handle)
        {
            handle = Rectangle.Empty;
            List<RuntimeElement> visibleSelection = GetVisibleSelection();
            if (visibleSelection.Count == 0 || GetMovableSelection().Count == 0)
                return false;

            Rectangle bounds = visibleSelection.Select(item => item.Control.DisplayRectangle).Aggregate(Rectangle.Union);
            handle = GetMoveHandleRectangle(bounds);
            return handle.Contains(pointer);
        }

        private Rectangle GetMoveHandleRectangle(Rectangle bounds)
        {
            Rectangle canvas = DisplayRectangle;
            // Place the move handle on the right-center of the selection so it stays
            // separate from the corner resize handles and does not cover the object.
            int x = bounds.Right + MoveHandleGap;
            int y = bounds.Center.Y - MoveHandleSize / 2;

            // If the selection is near the right canvas edge, keep the handle visible
            // by placing it just inside the selection bounds instead.
            if (x + MoveHandleSize > canvas.Right)
                x = bounds.Right - MoveHandleGap - MoveHandleSize;

            x = Math.Clamp(x, canvas.Left, Math.Max(canvas.Left, canvas.Right - MoveHandleSize));
            y = Math.Clamp(y, canvas.Top, Math.Max(canvas.Top, canvas.Bottom - MoveHandleSize));
            return new Rectangle(x, y, MoveHandleSize, MoveHandleSize);
        }

        private void BeginMoveGesture(Point pointer)
        {
            _gestureBefore = CaptureDocumentState();
            _gestureStartBounds = _selection.ToDictionary(item => item.Data.Id, item =>
                new Rectangle(item.Data.X, item.Data.Y, item.Data.Width, item.Data.Height), StringComparer.Ordinal);
            _pointerStart = pointer;
            _moving = true;
            _resizing = false;
        }

        private void BeginResizeGesture(Point pointer)
        {
            _gestureBefore = CaptureDocumentState();
            _gestureStartBounds = _selection.ToDictionary(item => item.Data.Id, item =>
                new Rectangle(item.Data.X, item.Data.Y, item.Data.Width, item.Data.Height), StringComparer.Ordinal);
            _pointerStart = pointer;
            _elementStart = new Point(_selected.Data.X, _selected.Data.Y);
            _sizeStart = new Point(_selected.Data.Width, _selected.Data.Height);
            _moving = false;
            _resizing = true;
        }

        private void SnapPosition(RuntimeElement moving, ref int x, ref int y, IReadOnlyCollection<RuntimeElement> ignored = null)
        {
            if (!_snapEnabled)
                return;
            if (_gridEnabled && _gridSize > 1)
            {
                x = (int)Math.Round(x / (double)_gridSize) * _gridSize;
                y = (int)Math.Round(y / (double)_gridSize) * _gridSize;
            }

            const int threshold = 5;
            int width = moving.Data.Width;
            int height = moving.Data.Height;
            int canvasWidth = Math.Max(MinimumSize, ToLogical(DisplayRectangle.Width));
            int canvasHeight = Math.Max(MinimumSize, ToLogical(DisplayRectangle.Height));
            int[] canvasX = { 0, canvasWidth - width, (canvasWidth - width) / 2 };
            int[] canvasY = { 0, canvasHeight - height, (canvasHeight - height) / 2 };
            SnapNearest(canvasX, ref x, threshold);
            SnapNearest(canvasY, ref y, threshold);

            int currentX = x;
            int currentY = y;
            int nearestVertical = _verticalGuides.OrderBy(candidate => Math.Abs(candidate - currentX)).FirstOrDefault(int.MinValue);
            int nearestHorizontal = _horizontalGuides.OrderBy(candidate => Math.Abs(candidate - currentY)).FirstOrDefault(int.MinValue);
            if (nearestVertical != int.MinValue && Math.Abs(nearestVertical - x) <= threshold) x = nearestVertical;
            if (nearestHorizontal != int.MinValue && Math.Abs(nearestHorizontal - y) <= threshold) y = nearestHorizontal;
            foreach (RuntimeElement other in _elements.Where(item => item != moving &&
                         (ignored == null || !ignored.Contains(item)) && item.Data.Visible && item.Control.Visible &&
                         IsVisibleOnCurrentDesignerPage(item.Data) && string.Equals(item.Data.ParentId, moving.Data.ParentId, StringComparison.Ordinal)))
            {
                int[] xCandidates = { other.Data.X, other.Data.X + other.Data.Width, other.Data.X + other.Data.Width / 2 - width / 2,
                    other.Data.X - width, other.Data.X + other.Data.Width - width };
                int[] yCandidates = { other.Data.Y, other.Data.Y + other.Data.Height, other.Data.Y + other.Data.Height / 2 - height / 2,
                    other.Data.Y - height, other.Data.Y + other.Data.Height - height };
                SnapNearest(xCandidates, ref x, threshold);
                SnapNearest(yCandidates, ref y, threshold);
            }
        }

        private static void SnapNearest(IEnumerable<int> candidates, ref int value, int threshold)
        {
            int current = value;
            int nearest = candidates.OrderBy(candidate => Math.Abs(candidate - current)).FirstOrDefault(int.MinValue);
            if (nearest != int.MinValue && Math.Abs(nearest - current) <= threshold)
                value = nearest;
        }

        private static Rectangle NormalizeRectangle(Point start, Point end) => new(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));

        private void SelectInRectangle(Rectangle rectangle, bool toggle, bool extend)
        {
            List<RuntimeElement> matches = _elements.Where(item =>
                IsVisibleOnCurrentDesignerPage(item.Data) && item.Data.Visible && item.Control.Visible &&
                !item.Data.Locked && rectangle.Intersects(item.Control.DisplayRectangle)).ToList();
            if (!toggle && !extend)
                _selection.Clear();
            foreach (RuntimeElement item in matches)
                if (!_selection.Contains(item))
                    _selection.Add(item);
            _selected = _selection.LastOrDefault();
            NotifySelectionChanged();
        }

        private static bool IsKeyPressed(KeyboardState keyboard, KeyboardState previous, Keys key) =>
            keyboard.IsKeyDown(key) && previous.IsKeyUp(key);

        private void HandleKeyboard(KeyboardState keyboard, KeyboardState previous)
        {
            bool control = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

            // Ctrl+Z/Ctrl+Y must work even when the last click left focus in a
            // property TextFieldControl. Text fields do not implement document
            // history, so allowing these commands here restores editor-wide undo.
            if (control && IsKeyPressed(keyboard, previous, Keys.Z)) { Undo(); return; }
            if (control && IsKeyPressed(keyboard, previous, Keys.Y)) { Redo(); return; }

            if (Scene?.FocusControl is TextFieldControl textField && textField.Visible)
                return;

            if (control && IsKeyPressed(keyboard, previous, Keys.S)) { SaveRequested?.Invoke(); return; }
            if (control && IsKeyPressed(keyboard, previous, Keys.C)) { CopySelected(); return; }
            if (control && IsKeyPressed(keyboard, previous, Keys.V)) { Paste(); return; }
            if (control && IsKeyPressed(keyboard, previous, Keys.X)) { CutSelected(); return; }
            if (control && IsKeyPressed(keyboard, previous, Keys.D)) { DuplicateSelected(); return; }
            if (IsKeyPressed(keyboard, previous, Keys.Delete)) { DeleteSelected(); return; }

            int dx = 0;
            int dy = 0;
            if (IsKeyPressed(keyboard, previous, Keys.Left)) dx = -1;
            else if (IsKeyPressed(keyboard, previous, Keys.Right)) dx = 1;
            else if (IsKeyPressed(keyboard, previous, Keys.Up)) dy = -1;
            else if (IsKeyPressed(keyboard, previous, Keys.Down)) dy = 1;
            if (dx != 0 || dy != 0)
            {
                NudgeSelected(dx * (shift ? 10 : 1), dy * (shift ? 10 : 1));
                // Editor focus owns this keyboard command while the canvas is active.
            }
        }

        private void TryUpdateDocument(Action action, string description, string coalesceKey = null)
        {
            GameUiEditorDocumentState before = CaptureDocumentState();
            action();
            CommitHistory(before, description, coalesceKey);
            ElementsChanged?.Invoke();
        }

        private void TryUpdateDocument(Action action, string description, IReadOnlyList<RuntimeElement> changed)
        {
            TryUpdateDocument(action, description);
        }

        private int ToDisplayX(RuntimeElement element, int logicalX) =>
            (ReferenceEquals(element.Control, _sourceRoot) ? _panX : 0) + (int)Math.Round(logicalX * _zoom);

        private int ToDisplayY(RuntimeElement element, int logicalY) =>
            (ReferenceEquals(element.Control, _sourceRoot) ? _panY : 0) + (int)Math.Round(logicalY * _zoom);

        private void SyncDataFromControl(RuntimeElement element, bool notify = true)
        {
            if (ReferenceEquals(element.Control, _sourceRoot))
            {
                element.Data.X = ToLogical(element.Control.X - _panX);
                element.Data.Y = ToLogical(element.Control.Y - _panY);
                element.Data.Width = ToLogical(element.Control.ViewSize.X);
                element.Data.Height = ToLogical(element.Control.ViewSize.Y);
                if (notify)
                    ElementChanged?.Invoke(element.Data);
                return;
            }
            element.Data.X = ToLogical(element.Control.X);
            element.Data.Y = ToLogical(element.Control.Y);
            element.Data.Width = ToLogical(element.Control.ViewSize.X);
            element.Data.Height = ToLogical(element.Control.ViewSize.Y);
            GameControl[] actualOrder = element.Control.Parent?.Controls.GetSnapshotArray() ?? Array.Empty<GameControl>();
            int actualLayer = Array.IndexOf(actualOrder, element.Control);
            if (actualLayer >= 0)
                element.Data.Layer = actualLayer;
            if (notify)
                ElementChanged?.Invoke(element.Data);
        }

        private bool TryStartDrop(Point pointer)
        {
            if (!GameUiEditorDragState.IsDragging || !DisplayRectangle.Contains(pointer)) return false;
            _pendingDropPath = GameUiEditorDragState.Take();
            if (_selected != null && _selected.Data.CanEditAsset && _selected.Control is SpriteControl &&
                _selected.Control.DisplayRectangle.Contains(pointer))
            {
                AssignSelectedAsset(_pendingDropPath);
            }
            else
            {
                Point local = new(
                    ToLogical(pointer.X - DisplayRectangle.X - _panX),
                    ToLogical(pointer.Y - DisplayRectangle.Y - _panY));
                if (_sourceMode)
                {
                    GameUiDesignerPageDefinition page = _pages.FirstOrDefault(candidate =>
                        string.Equals(candidate.Id, _currentPageId, StringComparison.Ordinal));
                    if (page != null && !page.IsWindowRoot)
                        local = new Point(local.X - ToLogical(page.Root.X), local.Y - ToLogical(page.Root.Y));
                }
                _ = AddImageAsync(_pendingDropPath, local);
            }
            _pendingDropPath = null;
            return true;
        }

        public bool AssignSelectedAsset(string relativePath)
        {
            if (_selected == null || !_selected.Data.CanEditAsset || _selected.Control is not SpriteControl)
                return false;
            GameUiEditorDocumentState before = CaptureDocumentState();
            _selected.Data.Asset = relativePath ?? string.Empty;
            ApplySelectedDataInternal(_selected.Data);
            CommitHistory(before, "Change asset", $"asset:{_selected.Data.Id}");
            return true;
        }

        private async Task AddImageAsync(string path, Point localPosition)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            Texture2D texture;
            try { texture = await TextureLoader.Instance.PrepareAndGetTexture(path); }
            catch { return; }
            if (texture == null) return;
            var data = new UiLayoutElement
            {
                Id = Guid.NewGuid().ToString("N"), Type = "image", Name = $"Image_{_elements.Count + 1}", Asset = path, FontSize = 14f,
                DesignerPageId = _currentPageId,
                DesignerPageName = CurrentPageName,
                X = Math.Max(0, localPosition.X - texture.Width / 2), Y = Math.Max(0, localPosition.Y - texture.Height / 2),
                Width = texture.Width, Height = texture.Height, Layer = _elements.Count
            };
            MuGame.ScheduleOnMainThread(() => AddRuntime(data, texture), MainThreadDispatcher.WorkPriority.High, "UiEditor.AddImage");
        }

        public void AddText() => AddRuntime(new UiLayoutElement { Id = Guid.NewGuid().ToString("N"), Type = "text", Name = $"Text_{_elements.Count + 1}", Text = "New Text", X = 60, Y = 80, Width = 180, Height = 30, FontSize = 14f, Layer = _elements.Count, DesignerPageId = _currentPageId, DesignerPageName = CurrentPageName });
        public void AddButton() => AddRuntime(new UiLayoutElement { Id = Guid.NewGuid().ToString("N"), Type = "button", Name = $"Button_{_elements.Count + 1}", Text = "Button", X = 80, Y = 130, Width = 150, Height = 36, FontSize = 11f, Layer = _elements.Count, DesignerPageId = _currentPageId, DesignerPageName = CurrentPageName });
        public void AddPanel() => AddRuntime(new UiLayoutElement { Id = Guid.NewGuid().ToString("N"), Type = "panel", Name = $"Panel_{_elements.Count + 1}", X = 100, Y = 190, Width = 240, Height = 140, Layer = _elements.Count, DesignerPageId = _currentPageId, DesignerPageName = CurrentPageName });

        private void AddRuntime(UiLayoutElement data, Texture2D texture = null, bool select = true, int? targetLayerIndex = null)
        {
            GameControl control = CreateControl(data, texture);
            if (control == null) return;

            GameControl parent = this;
            if (_sourceMode && _sourceRoot != null)
            {
                data.DesignerPageId ??= _currentPageId;
                data.DesignerPageName ??= CurrentPageName;
                UiLayoutElement rootData = _elements.FirstOrDefault(item => item.Control == _sourceRoot)?.Data;
                RuntimeElement pageRootElement = _pages
                    .FirstOrDefault(page => string.Equals(page.Id, _currentPageId, StringComparison.Ordinal)) is { IsWindowRoot: false } pageDefinition
                    ? _elements.FirstOrDefault(item => ReferenceEquals(item.Control, pageDefinition.Root))
                    : null;
                RuntimeElement parentElement = string.IsNullOrEmpty(data.ParentId)
                    ? pageRootElement
                    : _elements.FirstOrDefault(item => string.Equals(item.Data.Id, data.ParentId, StringComparison.Ordinal));
                parent = parentElement?.Control ?? _sourceRoot;
                data.ParentId = parentElement?.Data.Id ?? rootData?.Id;
                data.SourcePath = null;
                data.IsSourceBacked = false;
                data.IsDesignerAdded = true;
                data.CanEditVisibility = true;
                data.CanEditGeometry = true;
                data.CanEditText = control is ButtonControl or LabelControl;
                data.CanEditAsset = control is SpriteControl;
                data.CanReorder = true;
                data.SourceControlType = control.GetType().Name;
                data.UnsupportedReason = control is ButtonControl
                    ? "No click handler is generated for a newly added button. Add its behavior to the owning UI source."
                    : null;
            }

            parent.Controls.Add(control);
            if (_sourceMode && _runtimeSource != null && data.IsDesignerAdded)
                _runtimeSource.RegisterVisualDesignerBinding(control, data.Id, sourcePath: null);
            var runtime = new RuntimeElement { Data = data, Control = control, LoadedAsset = data.Asset ?? string.Empty };
            int index = targetLayerIndex.HasValue
                ? Math.Clamp(targetLayerIndex.Value, 0, _elements.Count)
                : _elements.Count;
            _elements.Insert(index, runtime);
            ApplyViewportTransform();
            if (_sourceMode)
                UpdateSourceSiblingLayers(data.ParentId);
            else
            {
                SyncControlZOrder();
                UpdateLayerIndices();
            }
            if (select)
                Select(runtime);
            ElementsChanged?.Invoke();
        }

        private GameControl CreateControl(UiLayoutElement data, Texture2D texture)
        {
            GameControl control;
            switch (data.Type)
            {
                case "image":
                    var image = new SpriteControl { TexturePath = data.Asset, AutoViewSize = false };
                    if (texture != null) image.SetTexture(texture);
                    control = image;
                    break;
                case "text":
                    control = new LabelControl { Text = data.Text ?? string.Empty, FontSize = data.FontSize <= 0f ? 14f : data.FontSize, TextColor = ModernHudTheme.TextWhite, IsBold = true };
                    break;
                case "button":
                    var button = new ButtonControl { Text = data.Text ?? "Button", TexturePath = string.IsNullOrWhiteSpace(data.Asset) ? null : data.Asset, FontSize = data.FontSize <= 0f ? 11f : data.FontSize, TextColor = ModernHudTheme.TextWhite, HoverTextColor = ModernHudTheme.TextGold, BackgroundColor = ModernHudTheme.BgMid, HoverBackgroundColor = ModernHudTheme.BgLight, PressedBackgroundColor = ModernHudTheme.BgDark };
                    if (texture != null) button.SetTexture(texture);
                    control = button;
                    break;
                case "panel":
                    control = new EditorPanelControl();
                    break;
                default:
                    return null;
            }
            control.Name = data.Name;
            control.AutoViewSize = false;
            control.X = data.X; control.Y = data.Y;
            control.ViewSize = new Point(Math.Max(MinimumSize, data.Width), Math.Max(MinimumSize, data.Height));
            control.ControlSize = control.ViewSize;
            control.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            if (control is TextureControl textureControl)
                textureControl.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            if (control is LabelControl labelControl)
                labelControl.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            control.Visible = data.Visible;
            control.Interactive = false;
            return control;
        }

        private bool TrySelect(Point pointer, out RuntimeElement result)
        {
            var topmostFirst = new List<RuntimeElement>(_elements.Count);
            CollectHitTestOrder(this, topmostFirst);
            foreach (RuntimeElement element in topmostFirst)
            {
                if (element.Control.DisplayRectangle.Contains(pointer))
                {
                    result = element;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private void CollectHitTestOrder(GameControl parent, List<RuntimeElement> topmostFirst)
        {
            if (!parent.Visible)
                return;

            GameControl[] children = parent.Controls.GetSnapshotArray();
            for (int i = children.Length - 1; i >= 0; i--)
                CollectHitTestOrder(children[i], topmostFirst);

            RuntimeElement element = _elements.Find(item => ReferenceEquals(item.Control, parent));
            bool isPageSurface = _pages.Any(page => ReferenceEquals(page.Root, parent));
            if (element != null && !ReferenceEquals(parent, _sourceRoot) && !isPageSurface)
                topmostFirst.Add(element);
        }

        private bool IsResizeHandle(GameControl control, Point pointer, out bool fromLeft, out bool fromTop)
        {
            Rectangle rect = control.DisplayRectangle;
            int half = HandleSize / 2;
            Rectangle[] handles =
            {
                new(rect.Left - half, rect.Top - half, HandleSize, HandleSize),
                new(rect.Right - half, rect.Top - half, HandleSize, HandleSize),
                new(rect.Left - half, rect.Bottom - half, HandleSize, HandleSize),
                new(rect.Right - half, rect.Bottom - half, HandleSize, HandleSize)
            };
            for (int i = 0; i < handles.Length; i++)
            {
                if (!handles[i].Contains(pointer))
                    continue;
                fromLeft = i is 0 or 2;
                fromTop = i is 0 or 1;
                return true;
            }

            fromLeft = false;
            fromTop = false;
            return false;
        }

        private void Select(RuntimeElement element, bool toggle = false, bool extend = false)
        {
            // Selection never mutates the runtime control collection or its Z-order.
            if (element == null)
            {
                if (!toggle && !extend)
                    _selection.Clear();
                _selected = _selection.LastOrDefault();
                NotifySelectionChanged();
                return;
            }

            if (extend && _selected != null)
            {
                List<RuntimeElement> selectable = _elements.Where(item => IsVisibleOnCurrentDesignerPage(item.Data) &&
                    item.Data.CanEditGeometry && !item.Data.Locked && item.Data.Visible && item.Control.Visible).ToList();
                int start = selectable.IndexOf(_selected);
                int end = selectable.IndexOf(element);
                if (start >= 0 && end >= 0)
                {
                    if (!toggle)
                        _selection.Clear();
                    int step = start <= end ? 1 : -1;
                    for (int i = start; ; i += step)
                    {
                        if (!_selection.Contains(selectable[i]))
                            _selection.Add(selectable[i]);
                        if (i == end)
                            break;
                    }
                    _selected = element;
                    NotifySelectionChanged();
                    return;
                }
            }

            if (toggle)
            {
                if (_selection.Contains(element))
                    _selection.Remove(element);
                else
                    _selection.Add(element);
                _selected = _selection.Contains(element) ? element : _selection.LastOrDefault();
            }
            else
            {
                _selection.Clear();
                _selection.Add(element);
                _selected = element;
            }
            NotifySelectionChanged();
        }

        private void NotifySelectionChanged()
        {
            SelectionChanged?.Invoke(SelectedElements);
            ElementSelected?.Invoke(_selected?.Data);
        }

        public void SelectById(string id, bool control = false, bool shift = false)
        {
            RuntimeElement element = _elements.Find(item => string.Equals(item.Data.Id, id, StringComparison.OrdinalIgnoreCase));
            if (element != null)
                Select(element, toggle: control, extend: shift);
        }

        public void ToggleVisibility(UiLayoutElement data)
        {
            List<RuntimeElement> targets = GetCommandTargets(data);
            if (targets.Count == 0)
                return;
            TryUpdateDocument(() =>
            {
                foreach (RuntimeElement element in targets.Where(item => item.Data.CanEditVisibility))
                {
                    element.Data.Visible = !element.Data.Visible;
                    element.Control.Visible = element.Data.Visible;
                    ElementChanged?.Invoke(element.Data);
                }
            }, targets.Count == 1 ? "Toggle visibility" : $"Toggle visibility ({targets.Count} objects)");
        }

        public void ToggleLock(UiLayoutElement data)
        {
            List<RuntimeElement> targets = GetCommandTargets(data);
            if (targets.Count == 0)
                return;
            TryUpdateDocument(() =>
            {
                foreach (RuntimeElement element in targets)
                {
                    element.Data.Locked = !element.Data.Locked;
                    ElementChanged?.Invoke(element.Data);
                }
            }, targets.Count == 1 ? "Toggle lock" : $"Toggle lock ({targets.Count} objects)");
        }

        private List<RuntimeElement> GetCommandTargets(UiLayoutElement data = null)
        {
            if (data != null)
            {
                RuntimeElement target = _elements.FirstOrDefault(item => ReferenceEquals(item.Data, data));
                if (target != null && !_selection.Contains(target))
                    return new List<RuntimeElement> { target };
            }
            return _selection.ToList();
        }

        public void NudgeSelected(int dx, int dy)
        {
            List<RuntimeElement> targets = GetCommandTargets();
            if (targets.Count == 0 || (dx == 0 && dy == 0))
                return;
            TryUpdateDocument(() =>
            {
                foreach (RuntimeElement item in targets.Where(item => item.Data.CanEditGeometry && !item.Data.Locked))
                {
                    item.Control.X += dx;
                    item.Control.Y += dy;
                    SyncDataFromControl(item, notify: false);
                }
            }, targets.Count == 1 ? "Nudge object" : $"Nudge {targets.Count} objects", "nudge");
        }

        public void AlignSelected(string mode)
        {
            List<RuntimeElement> targets = GetCommandTargets().Where(item => item.Data.CanEditGeometry && !item.Data.Locked).ToList();
            if (targets.Count == 0)
                return;
            int reference = mode switch
            {
                "left" => targets.Min(item => item.Control.X),
                "hcenter" => (int)Math.Round(targets.Average(item => item.Control.X + item.Control.ViewSize.X / 2d)),
                "right" => targets.Max(item => item.Control.X + item.Control.ViewSize.X),
                "top" => targets.Min(item => item.Control.Y),
                "vcenter" => (int)Math.Round(targets.Average(item => item.Control.Y + item.Control.ViewSize.Y / 2d)),
                "bottom" => targets.Max(item => item.Control.Y + item.Control.ViewSize.Y),
                _ => 0
            };
            TryUpdateDocument(() =>
            {
                foreach (RuntimeElement item in targets)
                {
                    if (mode == "left") item.Control.X = reference;
                    else if (mode == "hcenter") item.Control.X = reference - item.Control.ViewSize.X / 2;
                    else if (mode == "right") item.Control.X = reference - item.Control.ViewSize.X;
                    else if (mode == "top") item.Control.Y = reference;
                    else if (mode == "vcenter") item.Control.Y = reference - item.Control.ViewSize.Y / 2;
                    else if (mode == "bottom") item.Control.Y = reference - item.Control.ViewSize.Y;
                    SyncDataFromControl(item, notify: false);
                }
            }, $"Align {mode}");
        }

        public void DistributeSelected(bool horizontal)
        {
            List<RuntimeElement> targets = GetCommandTargets().Where(item => item.Data.CanEditGeometry && !item.Data.Locked).ToList();
            if (targets.Count < 3)
                return;
            List<RuntimeElement> sorted = horizontal
                ? targets.OrderBy(item => item.Control.X).ToList()
                : targets.OrderBy(item => item.Control.Y).ToList();
            int start = horizontal ? sorted[0].Control.X : sorted[0].Control.Y;
            int end = horizontal
                ? sorted[^1].Control.X + sorted[^1].Control.ViewSize.X
                : sorted[^1].Control.Y + sorted[^1].Control.ViewSize.Y;
            int occupied = sorted.Sum(item => horizontal ? item.Control.ViewSize.X : item.Control.ViewSize.Y);
            double gap = (end - start - occupied) / (double)(sorted.Count - 1);
            TryUpdateDocument(() =>
            {
                double cursor = start;
                foreach (RuntimeElement item in sorted)
                {
                    if (horizontal) item.Control.X = (int)Math.Round(cursor);
                    else item.Control.Y = (int)Math.Round(cursor);
                    cursor += (horizontal ? item.Control.ViewSize.X : item.Control.ViewSize.Y) + gap;
                    SyncDataFromControl(item, notify: false);
                }
            }, horizontal ? "Distribute horizontally" : "Distribute vertically");
        }

        public void CopySelected()
        {
            _clipboard.Clear();
            _clipboard.AddRange(GetCommandTargets().Select(item => GameUiEditorDocumentState.CloneElement(item.Data)));
        }

        public void CutSelected()
        {
            CopySelected();
            DeleteSelected();
        }

        public void Paste()
        {
            if (_clipboard.Count == 0)
                return;
            GameUiEditorDocumentState before = CaptureDocumentState();
            var pasted = new List<RuntimeElement>();
            foreach (UiLayoutElement source in _clipboard)
            {
                UiLayoutElement copy = GameUiEditorDocumentState.CloneElement(source);
                copy.Id = Guid.NewGuid().ToString("N");
                copy.Name = MakeUniqueName((source.Name ?? source.Type ?? "Object") + "_Copy");
                copy.X += 10;
                copy.Y += 10;
                copy.SourcePath = null;
                copy.IsSourceBacked = false;
                copy.IsDesignerAdded = true;
                AddRuntime(copy, select: false);
                RuntimeElement item = _elements.FirstOrDefault(candidate => string.Equals(candidate.Data.Id, copy.Id, StringComparison.Ordinal));
                if (item != null) pasted.Add(item);
            }
            SetSelection(pasted);
            CommitHistory(before, $"Paste {pasted.Count} object(s)");
            ElementsChanged?.Invoke();
        }

        private string MakeUniqueName(string requested)
        {
            string baseName = string.IsNullOrWhiteSpace(requested) ? "Object" : requested;
            string candidate = baseName;
            int suffix = 2;
            while (_elements.Any(item => string.Equals(item.Data.Name, candidate, StringComparison.OrdinalIgnoreCase)))
                candidate = $"{baseName}_{suffix++}";
            return candidate;
        }

        private void SetSelection(IEnumerable<RuntimeElement> items)
        {
            _selection.Clear();
            _selection.AddRange(items.Where(item => item != null && IsVisibleOnCurrentDesignerPage(item.Data)));
            _selected = _selection.LastOrDefault();
            NotifySelectionChanged();
        }

        public void AlignSelectedToCanvas(string mode)
        {
            List<RuntimeElement> targets = GetCommandTargets().Where(item => item.Data.CanEditGeometry && !item.Data.Locked).ToList();
            if (targets.Count == 0)
                return;
            int width = _sourceRoot?.ViewSize.X ?? ControlSize.X;
            int height = _sourceRoot?.ViewSize.Y ?? ControlSize.Y;
            TryUpdateDocument(() =>
            {
                foreach (RuntimeElement item in targets)
                {
                    if (mode == "left") item.Control.X = 0;
                    else if (mode == "center") item.Control.X = (width - item.Control.ViewSize.X) / 2;
                    else if (mode == "right") item.Control.X = width - item.Control.ViewSize.X;
                    else if (mode == "top") item.Control.Y = 0;
                    else if (mode == "middle") item.Control.Y = (height - item.Control.ViewSize.Y) / 2;
                    else if (mode == "bottom") item.Control.Y = height - item.Control.ViewSize.Y;
                    SyncDataFromControl(item, notify: false);
                }
            }, $"Align to canvas {mode}");
        }

        public void Reorder(UiLayoutElement data, int direction)
        {
            RuntimeElement element = _elements.Find(item => ReferenceEquals(item.Data, data));
            if (element == null || !data.CanReorder || data.Locked)
                return;

            List<RuntimeElement> siblings = GetSiblings(data);
            int currentIndex = siblings.IndexOf(element);
            if (currentIndex >= 0)
                ReorderTo(data, currentIndex + Math.Sign(direction));
        }

        public void ReorderTo(UiLayoutElement data, int targetLayerIndex)
        {
            GameUiEditorDocumentState before = CaptureDocumentState();
            RuntimeElement moving = _elements.Find(item => ReferenceEquals(item.Data, data));
            if (moving == null || !data.CanReorder || data.Locked)
                return;

            List<RuntimeElement> siblings = GetSiblings(data);
            int current = siblings.IndexOf(moving);
            if (current < 0)
                return;
            int destination = Math.Clamp(targetLayerIndex, 0, siblings.Count - 1);
            if (current == destination)
                return;

            siblings.RemoveAt(current);
            siblings.Insert(destination, moving);
            for (int i = 0; i < siblings.Count; i++)
                siblings[i].Data.Layer = i;
            foreach (RuntimeElement sibling in siblings)
                sibling.Control.BringToFront();

            UpdateSiblingLayerIndices();
            ElementChanged?.Invoke(moving.Data);
            CommitHistory(before, "Reorder object");
            ElementsChanged?.Invoke();
        }

        private List<RuntimeElement> GetSiblings(UiLayoutElement data) => _elements
            .Where(item => string.Equals(item.Data.ParentId ?? string.Empty, data.ParentId ?? string.Empty, StringComparison.Ordinal))
            .OrderBy(item => item.Data.Layer)
            .ToList();

        public void BringSelectedToFront(UiLayoutElement data)
        {
            if (data != null)
                ReorderTo(data, GetSiblings(data).Count - 1);
        }

        public void SendSelectedToBack(UiLayoutElement data)
        {
            if (data != null)
                ReorderTo(data, 0);
        }

        private void SyncControlZOrder()
        {
            // Layer indices are bottom-to-top and scoped to siblings under one parent.
            foreach (IGrouping<string, RuntimeElement> group in _elements.GroupBy(item => item.Data.ParentId ?? string.Empty, StringComparer.Ordinal))
            {
                foreach (RuntimeElement item in group.OrderBy(item => item.Data.Layer))
                    item.Control.BringToFront();
            }
            UpdateSiblingLayerIndices();
        }

        private void UpdateLayerIndices() => UpdateSiblingLayerIndices();

        private void UpdateSiblingLayerIndices()
        {
            foreach (IGrouping<string, RuntimeElement> group in _elements.GroupBy(item => item.Data.ParentId ?? string.Empty, StringComparer.Ordinal))
            {
                foreach (RuntimeElement item in group)
                {
                    GameControl[] actualOrder = item.Control.Parent?.Controls.GetSnapshotArray() ?? Array.Empty<GameControl>();
                    int index = Array.IndexOf(actualOrder, item.Control);
                    if (index >= 0)
                        item.Data.Layer = index;
                }
            }
        }

        private void UpdateSourceSiblingLayers(string parentId)
        {
            foreach (RuntimeElement sibling in _elements.Where(item => string.Equals(item.Data.ParentId ?? string.Empty, parentId ?? string.Empty, StringComparison.Ordinal)))
            {
                GameControl[] actualOrder = sibling.Control.Parent?.Controls.GetSnapshotArray() ?? Array.Empty<GameControl>();
                int index = Array.IndexOf(actualOrder, sibling.Control);
                if (index >= 0)
                    sibling.Data.Layer = index;
            }
        }

        public void ApplySelectedData(IReadOnlyList<UiLayoutElement> data)
        {
            if (data == null || data.Count == 0)
                return;
            GameUiEditorDocumentState before = CaptureDocumentState();
            foreach (UiLayoutElement item in data)
                ApplySelectedDataInternal(item);
            CommitHistory(before, data.Count == 1 ? "Change properties" : $"Change properties ({data.Count} objects)", data.Count == 1 ? $"properties:{data[0].Id}" : null);
        }

        public void ApplySelectedData(UiLayoutElement data)
        {
            if (data == null)
                return;
            GameUiEditorDocumentState before = CaptureDocumentState();
            ApplySelectedDataInternal(data);
            CommitHistory(before, "Change properties", $"properties:{data.Id}");
        }

        private void ApplySelectedDataInternal(UiLayoutElement data)
        {
            RuntimeElement runtime = _elements.FirstOrDefault(item => ReferenceEquals(item.Data, data) ||
                string.Equals(item.Data.Id, data.Id, StringComparison.Ordinal));
            if (runtime == null)
                return;
            UiLayoutElement target = runtime.Data;
            target.Name = data.Name;
            target.Asset = data.Asset;
            target.Text = data.Text;
            target.X = data.X;
            target.Y = data.Y;
            target.Width = data.Width;
            target.Height = data.Height;
            target.Opacity = data.Opacity;
            target.FontSize = data.FontSize;
            target.Visible = data.Visible;
            target.Locked = data.Locked;
            data = target;
            runtime.Control.Name = data.Name;
            if (data.CanEditGeometry && !data.Locked)
            {
                runtime.Control.X = ToDisplayX(runtime, data.X);
                runtime.Control.Y = ToDisplayY(runtime, data.Y);
                Point size = new(Math.Max(MinimumSize, data.Width), Math.Max(MinimumSize, data.Height));
                runtime.Control.ViewSize = size;
                runtime.Control.ControlSize = size;
            }
            if (data.CanEditOpacity)
            {
                runtime.Control.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
                if (runtime.Control is TextureControl textureControl)
                    textureControl.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            }
            if (runtime.Control is LabelControl label)
            {
                if (data.CanEditText)
                    label.Text = data.Text ?? string.Empty;
                if (data.CanEditFontSize && data.FontSize > 0f)
                    label.FontSize = data.FontSize;
                if (data.CanEditOpacity)
                    label.Alpha = Math.Clamp(data.Opacity, 0f, 1f);
            }
            if (runtime.Control is ButtonControl button)
            {
                if (data.CanEditText)
                    button.Text = data.Text ?? string.Empty;
                if (data.CanEditFontSize && data.FontSize > 0f)
                    button.FontSize = data.FontSize;
            }
            if (data.CanEditVisibility)
                runtime.Control.Visible = data.Visible;
            if (runtime.Control is EditorPanelControl panel) panel.BackgroundColor = ModernHudTheme.BgMid;
            ApplyViewportTransform();
            string requestedAsset = data.Asset ?? string.Empty;
            if (data.CanEditAsset && runtime.Control is SpriteControl sprite && !string.Equals(runtime.LoadedAsset, requestedAsset, StringComparison.OrdinalIgnoreCase))
                _ = UpdateElementAssetAsync(runtime, sprite, requestedAsset);
            ElementChanged?.Invoke(data);
        }

        private async Task UpdateElementAssetAsync(RuntimeElement runtime, SpriteControl control, string asset)
        {
            runtime.LoadedAsset = asset;
            Texture2D texture = null;
            if (!string.IsNullOrWhiteSpace(asset))
            {
                try
                {
                    texture = await TextureLoader.Instance.PrepareAndGetTexture(asset);
                }
                catch
                {
                    // Invalid or unavailable assets leave the existing editor element intact.
                    return;
                }
                if (texture == null)
                    return;
            }

            MuGame.ScheduleOnMainThread(() =>
            {
                if (_elements.Contains(runtime) && string.Equals(runtime.Data.Asset ?? string.Empty, asset, StringComparison.OrdinalIgnoreCase))
                {
                    control.TexturePath = string.IsNullOrWhiteSpace(asset) ? null : asset;
                    control.SetTexture(texture);
                    control.ViewSize = new Point(Math.Max(MinimumSize, runtime.Data.Width), Math.Max(MinimumSize, runtime.Data.Height));
                    control.ControlSize = control.ViewSize;
                }
            }, MainThreadDispatcher.WorkPriority.High, "UiEditor.UpdateAsset");
        }

        public void DeleteSelected()
        {
            List<RuntimeElement> targets = GetCommandTargets();
            if (targets.Count == 0)
                return;

            if (targets.Any(item => ReferenceEquals(item.Control, _sourceRoot)))
            {
                DocumentLoadWarning?.Invoke("MuHelperWindow is the protected designer root and cannot be deleted. Select a child object to delete or hide it from Layers / Objects.");
                return;
            }

            List<RuntimeElement> removable = targets.Where(item => !_sourceMode || item.Data.IsDesignerAdded).ToList();
            List<RuntimeElement> hideOnly = targets.Where(item => _sourceMode && !item.Data.IsDesignerAdded).ToList();
            if (hideOnly.Any(item => !item.Data.CanEditVisibility))
            {
                DocumentLoadWarning?.Invoke("One or more selected source controls cannot be deleted or hidden safely.");
                hideOnly = hideOnly.Where(item => item.Data.CanEditVisibility).ToList();
            }

            GameUiEditorDocumentState before = CaptureDocumentState();
            foreach (RuntimeElement item in hideOnly)
            {
                item.Data.Visible = false;
                item.Control.Visible = false;
            }
            foreach (RuntimeElement item in removable)
            {
                item.Control.Dispose();
                _elements.Remove(item);
            }
            SetSelection(Array.Empty<RuntimeElement>());
            UpdateSiblingLayerIndices();
            if (hideOnly.Count > 0 || removable.Count > 0)
            {
                CommitHistory(before, targets.Count == 1 ? "Delete object" : $"Delete {targets.Count} objects");
                ElementsChanged?.Invoke();
            }
        }

        public void DuplicateSelected()
        {
            List<RuntimeElement> targets = GetCommandTargets();
            if (targets.Count == 0)
                return;
            GameUiEditorDocumentState before = CaptureDocumentState();
            var copies = new List<RuntimeElement>();
            foreach (RuntimeElement sourceRuntime in targets)
            {
                UiLayoutElement source = sourceRuntime.Data;
                UiLayoutElement copy = GameUiEditorDocumentState.CloneElement(source);
                copy.Id = Guid.NewGuid().ToString("N");
                copy.ParentId = source.ParentId;
                copy.Name = MakeUniqueName((source.Name ?? source.Type ?? "Object") + "_Copy");
                copy.X += 10;
                copy.Y += 10;
                copy.SourcePath = null;
                copy.IsSourceBacked = false;
                copy.IsDesignerAdded = true;
                AddRuntime(copy, sourceRuntime.Control is SpriteControl sprite ? sprite.Texture : null, select: false);
                RuntimeElement runtime = _elements.FirstOrDefault(item => string.Equals(item.Data.Id, copy.Id, StringComparison.Ordinal));
                if (runtime != null)
                    copies.Add(runtime);
            }
            SetSelection(copies);
            CommitHistory(before, targets.Count == 1 ? "Duplicate object" : $"Duplicate {targets.Count} objects");
            ElementsChanged?.Invoke();
        }

        public void MoveSelectedLayer(int direction)
        {
            if (_selected != null)
                Reorder(_selected.Data, direction);
        }

        public UiLayoutDocument CreateDocument(string name)
        {
            var document = new UiLayoutDocument
            {
                Name = name,
                Width = ControlSize.X,
                Height = ControlSize.Y
            };

            foreach (RuntimeElement item in _elements)
            {
                UiLayoutElement data = item.Data;
                document.Elements.Add(new UiLayoutElement
                {
                    Id = data.Id,
                    Name = data.Name,
                    Type = data.Type,
                    Asset = data.Asset,
                    Text = data.Text,
                    X = item.Control.X,
                    Y = item.Control.Y,
                    Width = item.Control.ViewSize.X,
                    Height = item.Control.ViewSize.Y,
                    Opacity = data.Opacity,
                    FontSize = data.FontSize,
                    Layer = _elements.IndexOf(item),
                    Visible = item.Control.Visible,
                    Locked = data.Locked
                });
            }

            UiLayoutSerializer.ValidateAndNormalize(document);
            return document;
        }

        public async Task LoadDocumentAsync(UiLayoutDocument document)
        {
            if (document == null)
                throw new InvalidOperationException("No UI layout was loaded.");

            UiLayoutSerializer.ValidateAndNormalize(document);
            int generation = Interlocked.Increment(ref _documentLoadGeneration);
            foreach (RuntimeElement item in _elements)
                item.Control.Dispose();
            _elements.Clear();
            _selected = null;
            ControlSize = new Point(document.Width, document.Height);
            ViewSize = ControlSize;
            ElementSelected?.Invoke(null);
            ElementsChanged?.Invoke();

            var textures = new Texture2D[document.Elements.Count];
            var warnings = new List<string>();
            var loadTasks = new List<Task>();
            for (int i = 0; i < document.Elements.Count; i++)
            {
                UiLayoutElement element = document.Elements[i];
                if (string.IsNullOrWhiteSpace(element.Asset) || element.Type is not ("image" or "button"))
                    continue;

                int index = i;
                loadTasks.Add(LoadDocumentTextureAsync(element.Asset, index, textures, warnings));
            }
            await Task.WhenAll(loadTasks).ConfigureAwait(false);
            if (generation != Volatile.Read(ref _documentLoadGeneration))
                return;

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action apply = () =>
            {
                if (generation != Volatile.Read(ref _documentLoadGeneration))
                {
                    completion.TrySetResult(false);
                    return;
                }

                for (int i = 0; i < document.Elements.Count; i++)
                {
                    UiLayoutElement element = document.Elements[i];
                    if (element.Type == "image" && textures[i] == null)
                        continue;
                    AddRuntime(element, textures[i], false, i);
                }

                _selection.Clear();
                _selected = null;
                NotifySelectionChanged();
                _history.Reset(CaptureDocumentState());
                NotifyHistoryStateChanged();
                if (warnings.Count > 0)
                    DocumentLoadWarning?.Invoke(string.Join("; ", warnings));
                completion.TrySetResult(true);
            };

            if (MuGame.IsMainThread)
                apply();
            else
                MuGame.ScheduleOnMainThread(apply, MainThreadDispatcher.WorkPriority.High, "UiEditor.LoadElements");
            await completion.Task.ConfigureAwait(false);
        }

        private static async Task LoadDocumentTextureAsync(string asset, int index, Texture2D[] textures, List<string> warnings)
        {
            try
            {
                textures[index] = await TextureLoader.Instance.PrepareAndGetTexture(asset).ConfigureAwait(false);
                if (textures[index] == null)
                    lock (warnings) warnings.Add($"Failed to load UI asset: {asset}");
            }
            catch (Exception ex)
            {
                lock (warnings) warnings.Add($"Failed to load UI asset: {asset} ({ex.Message})");
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible) return;
            base.Draw(gameTime);
            SpriteBatch sprite = GraphicsManager.Instance.Sprite;
            Texture2D pixel = GraphicsManager.Instance.Pixel;
            if (sprite == null || pixel == null)
                return;

            DrawViewportOverlay(sprite, pixel);
            List<RuntimeElement> visibleSelection = _selection.Where(item => item.Control.Visible && IsVisibleOnCurrentDesignerPage(item.Data)).ToList();
            if (visibleSelection.Count > 0)
            {
                Rectangle combined = visibleSelection.Select(item => item.Control.DisplayRectangle)
                    .Aggregate(Rectangle.Union);
                foreach (RuntimeElement item in visibleSelection)
                    DrawOutline(sprite, pixel, item.Control.DisplayRectangle, ModernHudTheme.AccentBright * 0.75f, 1);
                DrawOutline(sprite, pixel, combined, ModernHudTheme.AccentBright, 2);
                if (visibleSelection.Count == 1 && _selected == visibleSelection[0] &&
                    _selected.Data.CanEditGeometry && !_selected.Data.Locked)
                    DrawResizeHandles(sprite, pixel, combined);
                // Always show the handle for a visible selection. For locked selections
                // it is rendered disabled, making the non-movable state explicit instead
                // of making the handle appear to be missing.
                DrawMoveHandle(sprite, pixel, GetMoveHandleRectangle(combined), GetMovableSelection().Count > 0);
            }

            if (_selecting && _selectionRect.Width > 0 && _selectionRect.Height > 0)
            {
                DrawOutline(sprite, pixel, _selectionRect, ModernHudTheme.AccentBright, 1);
                sprite.Draw(pixel, _selectionRect, new Color(90, 140, 220, 45));
            }
        }

        private void DrawViewportOverlay(SpriteBatch sprite, Texture2D pixel)
        {
            Rectangle bounds = DisplayRectangle;
            int originX = bounds.X + _panX;
            int originY = bounds.Y + _panY;
            if (_gridEnabled)
            {
                int spacing = Math.Max(1, (int)Math.Round(_gridSize * _zoom));
                Color gridColor = new Color(75, 90, 115, 45);
                for (int x = originX; x < bounds.Right; x += spacing)
                    sprite.Draw(pixel, new Rectangle(x, bounds.Y, 1, bounds.Height), gridColor);
                for (int y = originY; y < bounds.Bottom; y += spacing)
                    sprite.Draw(pixel, new Rectangle(bounds.X, y, bounds.Width, 1), gridColor);
            }

            Color guideColor = new Color(90, 180, 245, 160);
            foreach (int guide in _verticalGuides)
            {
                int x = originX + (int)Math.Round(guide * _zoom);
                if (x >= bounds.X && x < bounds.Right)
                    sprite.Draw(pixel, new Rectangle(x, bounds.Y, 1, bounds.Height), guideColor);
            }
            foreach (int guide in _horizontalGuides)
            {
                int y = originY + (int)Math.Round(guide * _zoom);
                if (y >= bounds.Y && y < bounds.Bottom)
                    sprite.Draw(pixel, new Rectangle(bounds.X, y, bounds.Width, 1), guideColor);
            }
            if (_guideDragging)
            {
                if (_guideVertical)
                {
                    int x = originX + (int)Math.Round(_guideCoordinate * _zoom);
                    sprite.Draw(pixel, new Rectangle(x, bounds.Y, 1, bounds.Height), ModernHudTheme.AccentBright);
                }
                else
                {
                    int y = originY + (int)Math.Round(_guideCoordinate * _zoom);
                    sprite.Draw(pixel, new Rectangle(bounds.X, y, bounds.Width, 1), ModernHudTheme.AccentBright);
                }
            }

            Color rulerColor = new Color(120, 135, 160, 130);
            sprite.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), rulerColor);
            sprite.Draw(pixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), rulerColor);
            for (int logical = 0; logical <= 1000; logical += 50)
            {
                int x = originX + (int)Math.Round(logical * _zoom);
                int y = originY + (int)Math.Round(logical * _zoom);
                if (x >= bounds.X && x < bounds.Right)
                    sprite.Draw(pixel, new Rectangle(x, bounds.Y, 1, 6), rulerColor);
                if (y >= bounds.Y && y < bounds.Bottom)
                    sprite.Draw(pixel, new Rectangle(bounds.X, y, 6, 1), rulerColor);
            }
        }

        private void DrawResizeHandles(SpriteBatch sprite, Texture2D pixel, Rectangle rect)
        {
            int half = HandleSize / 2;
            Color handleColor = ModernHudTheme.AccentBright;
            sprite.Draw(pixel, new Rectangle(rect.Left - half, rect.Top - half, HandleSize, HandleSize), handleColor);
            sprite.Draw(pixel, new Rectangle(rect.Right - half, rect.Top - half, HandleSize, HandleSize), handleColor);
            sprite.Draw(pixel, new Rectangle(rect.Left - half, rect.Bottom - half, HandleSize, HandleSize), handleColor);
            sprite.Draw(pixel, new Rectangle(rect.Right - half, rect.Bottom - half, HandleSize, HandleSize), handleColor);
        }

        private void DrawMoveHandle(SpriteBatch sprite, Texture2D pixel, Rectangle rect, bool enabled)
        {
            bool hovered = enabled && rect.Contains(MuGame.Instance.UiMouseState.Position);
            Color active = new Color(255, 196, 64, 255);
            Color disabled = new Color(115, 125, 140, 190);
            Color fill = enabled ? (hovered ? Color.White : active) : ModernHudTheme.BgDarkest;
            Color icon = enabled ? (hovered ? active : ModernHudTheme.BgDarkest) : disabled;
            Color border = enabled ? active : disabled;
            sprite.Draw(pixel, rect, fill);
            DrawOutline(sprite, pixel, rect, border, 1);

            int centerX = rect.Center.X;
            int centerY = rect.Center.Y;
            int left = rect.Left + 4;
            int right = rect.Right - 5;
            int top = rect.Top + 4;
            int bottom = rect.Bottom - 5;
            sprite.Draw(pixel, new Rectangle(centerX - 1, top, 3, bottom - top + 1), icon);
            sprite.Draw(pixel, new Rectangle(left, centerY - 1, right - left + 1, 3), icon);
            sprite.Draw(pixel, new Rectangle(centerX - 3, top, 7, 2), icon);
            sprite.Draw(pixel, new Rectangle(centerX - 3, bottom - 1, 7, 2), icon);
            sprite.Draw(pixel, new Rectangle(left, centerY - 3, 2, 7), icon);
            sprite.Draw(pixel, new Rectangle(right - 1, centerY - 3, 2, 7), icon);
        }

        private static void DrawOutline(SpriteBatch sprite, Texture2D pixel, Rectangle rect, Color color, int thickness)
        {
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color); sprite.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color); sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color); sprite.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private sealed class EditorPanelControl : UIControl
        {
            public EditorPanelControl() { BackgroundColor = ModernHudTheme.BgMid; BorderColor = ModernHudTheme.BorderInner; BorderThickness = 1; }
        }
    }
}
