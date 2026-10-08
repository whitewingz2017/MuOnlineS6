using System;
using System.Collections.Generic;
using System.IO;
using Client.Main.Controls;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Controls.UI.Game.Layouts;
using Client.Main.Controls.UI.Game.Helper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Editor
{
    internal sealed class GameUiEditorControl : UIControl
    {
        private readonly LabelControl _titleLabel;
        private readonly LabelControl _hintLabel;
        private readonly LabelControl _pageLabel;
        private readonly ButtonControl _previousPageButton;
        private readonly ButtonControl _nextPageButton;
        private readonly ButtonControl _undoButton;
        private readonly ButtonControl _redoButton;
        private readonly ButtonControl _documentSaveButton;
        private readonly LabelControl _viewportLabel;
        private readonly GameUiEditorAssetBrowserControl _assetBrowser;
        private readonly GameUiEditorCanvasControl _canvas;
        private readonly GameUiEditorLayersControl _layers;
        private readonly GameUiEditorPropertiesControl _properties;
        private readonly TextFieldControl _layoutNameField;
        private readonly TextFieldControl _sourcePathField;
        private readonly UiLayoutRuntimeControl _runtimeLayout;
        private readonly MuHelperWindow _muHelperWindow;
        private readonly OpenMuCSharpUiParser _sourceParser = new();
        private readonly OpenMuDesignerCSharpSourceWriter _sourceWriter = new();
        private OpenMuCSharpUiInspection _sourceInspection;
        private string _sourceBindingError;
        private GameControl _sourceOriginalParent;
        private int _sourceOriginalX;
        private int _sourceOriginalY;
        private bool _sourceOriginalVisible;
        private bool _sourceOriginalInteractive;
        private GameControl _previousFocusControl;
        private readonly Dictionary<GameControl, bool> _sourceInteractiveStates = new();

        public bool IsOpen => Visible;

        public GameUiEditorControl(UiLayoutRuntimeControl runtimeLayout = null, MuHelperWindow muHelperWindow = null)
        {
            AutoViewSize = false;
            ControlSize = UiScaler.VirtualSize;
            ViewSize = UiScaler.VirtualSize;
            Visible = false;
            Interactive = true;
            CapturePointerWhenNonInteractive = true;
            _runtimeLayout = runtimeLayout;
            _muHelperWindow = muHelperWindow;
            BackgroundColor = new Color(5, 7, 12, 235);
            BorderColor = ModernHudTheme.BorderHighlight;
            BorderThickness = 2;

            _titleLabel = new LabelControl
            {
                Text = "MU UI EDITOR",
                X = 20,
                Y = 16,
                FontSize = 20f,
                TextColor = ModernHudTheme.TextGold,
                IsBold = true,
                HasShadow = true
            };
            Controls.Add(_titleLabel);

            _hintLabel = new LabelControl
            {
                Text = "Phase 5-8 editor: select, edit, save, and load UI elements",
                X = 22,
                Y = 38,
                FontSize = 9.5f,
                TextColor = ModernHudTheme.TextWhite,
                IsBold = true,
                HasShadow = true
            };
            Controls.Add(_hintLabel);

            _pageLabel = new LabelControl
            {
                Text = "PAGE: Root / Shared",
                X = 400,
                Y = 38,
                ControlSize = new Point(260, 18),
                ViewSize = new Point(260, 18),
                FontSize = 10f,
                TextColor = ModernHudTheme.TextGold,
                IsBold = true,
                HasShadow = true
            };
            Controls.Add(_pageLabel);
            _previousPageButton = CreateToolbarButton("<", 666, 36, 28, PreviousPage);
            _nextPageButton = CreateToolbarButton(">", 698, 36, 28, NextPage);
            _undoButton = CreateToolbarButton("Undo", 730, 36, 52, () => _canvas.Undo());
            _redoButton = CreateToolbarButton("Redo", 786, 36, 52, () => _canvas.Redo());
            CreateToolbarButton("Zoom-", 842, 36, 52, () => _canvas.SetZoom(_canvas.ZoomPercent - 25));
            CreateToolbarButton("Zoom+", 898, 36, 52, () => _canvas.SetZoom(_canvas.ZoomPercent + 25));
            CreateToolbarButton("Grid", 954, 36, 48, () => _canvas.ToggleGrid());
            _viewportLabel = new LabelControl
            {
                Text = "100% G5",
                X = 1008,
                Y = 38,
                ControlSize = new Point(80, 18),
                ViewSize = new Point(80, 18),
                FontSize = 8f,
                TextColor = ModernHudTheme.TextGray
            };
            Controls.Add(_viewportLabel);
            CreateToolbarButton("L", 1090, 36, 32, () => _canvas.AlignSelected("left"));
            CreateToolbarButton("C", 1124, 36, 32, () => _canvas.AlignSelected("hcenter"));
            CreateToolbarButton("R", 1158, 36, 32, () => _canvas.AlignSelected("right"));
            CreateToolbarButton("DH", 1192, 36, 38, () => _canvas.DistributeSelected(true));
            CreateToolbarButton("DV", 1232, 36, 38, () => _canvas.DistributeSelected(false));

            _sourcePathField = TextFieldControl.Create();
            _sourcePathField.X = 20;
            _sourcePathField.Y = 62;
            _sourcePathField.ControlSize = new Point(235, 24);
            _sourcePathField.ViewSize = _sourcePathField.ControlSize;
            _sourcePathField.FontSize = 7.5f;
            _sourcePathField.TextColor = ModernHudTheme.TextWhite;
            _sourcePathField.BackgroundColor = ModernHudTheme.BgDarkest;
            _sourcePathField.BorderColor = ModernHudTheme.BorderInner;
            _sourcePathField.Value = "MuOnlineS6/Client.Main/Controls/UI/Game/Helper/MuHelperWindow.cs";
            Controls.Add(_sourcePathField);
            AddActionButton("Open UI", 259, 62, OpenCSharpUi, 64);
            AddActionButton("Save C#", 327, 62, SaveCSharpUi, 64);

            _assetBrowser = new GameUiEditorAssetBrowserControl { X = 20, Y = 92 };
            _assetBrowser.AssetSelected += OnAssetSelected;
            Controls.Add(_assetBrowser);

            _canvas = new GameUiEditorCanvasControl { X = 400, Y = 92 };
            _canvas.ElementSelected += OnElementSelected;
            _canvas.SelectionChanged += selection =>
            {
                _layers.SetSelected(selection);
                _properties.SetSelected(selection);
            };
            _canvas.DirtyStateChanged += dirty => UpdateDirtyTitle(dirty);
            _canvas.HistoryStateChanged += UpdateHistoryButtons;
            _canvas.SaveRequested += () =>
            {
                if (_canvas.IsSourceMode) SaveCSharpUi();
                else SaveDocument();
            };
            _canvas.DocumentLoadWarning += warning => _hintLabel.Text = warning;
            _canvas.ElementChanged += OnElementChanged;
            _canvas.ElementsChanged += OnElementsChanged;
            _canvas.DocumentLoadWarning += warning => _hintLabel.Text = warning;
            Controls.Add(_canvas);

            _layers = new GameUiEditorLayersControl { X = 1020, Y = 92 };
            _layers.Selected += (data, control, shift) => _canvas.SelectById(data?.Id, control, shift);
            _layers.VisibilityToggleRequested += _canvas.ToggleVisibility;
            _layers.LockToggleRequested += _canvas.ToggleLock;
            _layers.ReorderRequested += _canvas.ReorderTo;
            _layers.BringToFrontRequested += _canvas.BringSelectedToFront;
            _layers.ForwardRequested += data => _canvas.Reorder(data, 1);
            _layers.BackwardRequested += data => _canvas.Reorder(data, -1);
            _layers.SendToBackRequested += _canvas.SendSelectedToBack;
            Controls.Add(_layers);

            _properties = new GameUiEditorPropertiesControl { X = 1020, Y = 374 };
            _properties.Changed += data => _canvas.ApplySelectedData(data);
            _properties.ChangedMany += data => _canvas.ApplySelectedData(data);
            _properties.LockToggleRequested += _canvas.ToggleLock;
            _properties.VisibilityToggleRequested += _canvas.ToggleVisibility;
            Controls.Add(_properties);
            OnElementsChanged();
            UpdateHistoryButtons();

            _layoutNameField = TextFieldControl.Create();
            _layoutNameField.X = 974;
            _layoutNameField.Y = 62;
            _layoutNameField.ControlSize = new Point(96, 24);
            _layoutNameField.ViewSize = _layoutNameField.ControlSize;
            _layoutNameField.FontSize = 8.5f;
            _layoutNameField.TextColor = ModernHudTheme.TextWhite;
            _layoutNameField.BackgroundColor = ModernHudTheme.BgDarkest;
            _layoutNameField.BorderColor = ModernHudTheme.BorderInner;
            _layoutNameField.Placeholder = "Layout name";
            _layoutNameField.Value = "CharacterWindow";
            Controls.Add(_layoutNameField);

            // Keep document actions in the header; object actions live in the bottom toolbar
            // beside the canvas so they are visually separated from file/page operations.
            _documentSaveButton = CreateToolbarButton("Save", 1072, 62, 64, SaveCurrentDocument);
            Controls.Add(_documentSaveButton);
            AddActionButton("Load", 1138, 62, LoadDocument, 64);
            AddActionButton("Preview", 1204, 62, PreviewLayout, 64);

            // Bottom object-action toolbar: these commands operate on the current selection.
            const int objectActionsY = 678;
            AddActionButton("Text", 400, objectActionsY, _canvas.AddText);
            AddActionButton("Button", 482, objectActionsY, _canvas.AddButton);
            AddActionButton("Panel", 564, objectActionsY, _canvas.AddPanel);
            AddActionButton("Delete", 646, objectActionsY, _canvas.DeleteSelected);
            AddActionButton("Duplicate", 728, objectActionsY, _canvas.DuplicateSelected);
            AddActionButton("Back", 810, objectActionsY, () => _canvas.SendSelectedToBack(_canvas.SelectedData));
            AddActionButton("Front", 892, objectActionsY, () => _canvas.BringSelectedToFront(_canvas.SelectedData));
        }

        private void OpenCSharpUi()
        {
            try
            {
                if (_muHelperWindow == null)
                {
                    _hintLabel.Text = "MuHelperWindow is not available in this scene.";
                    return;
                }

                OpenMuCSharpUiInspection inspection = _sourceParser.InspectFile(_sourcePathField.Value);
                string expectedLiveSource = OpenMuCSharpUiParser.ResolveSourcePath("MuOnlineS6/Client.Main/Controls/UI/Game/Helper/MuHelperWindow.cs");
                if (!string.Equals(inspection.NamespaceName, "Client.Main.Controls.UI.Game.Helper", StringComparison.Ordinal) ||
                    !string.Equals(Path.GetFileName(inspection.SourcePath), "MuHelperWindow.cs", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(inspection.SourcePath, expectedLiveSource, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"This F11 canvas is bound to the compiled MuHelperWindow at '{expectedLiveSource}'. Open that exact source file to edit its live tree.");

                _sourceInspection = inspection;
                _sourceBindingError = null;
                EnterSourceMode();
                _canvas.BindLiveSourceTree(_muHelperWindow);
                _sourceBindingError = _muHelperWindow.VisualDesignerBindingWarning;
                UpdatePageSelector();
                if (!string.IsNullOrWhiteSpace(_sourceBindingError))
                    _muHelperWindow.SetVisualDesignerBindingWarning(_sourceBindingError);
                _layoutNameField.Value = _sourceInspection.ClassName;
                _hintLabel.Text = $"OPEN: {Path.GetFileName(_sourceInspection.SourcePath)} | {_sourceInspection.ClassName} — {_sourceInspection.ConstructionPatterns.Count} control patterns" +
                                  (_sourceBindingError == null ? string.Empty : $" | BINDING WARNING: {_sourceBindingError}");
            }
            catch (Exception ex)
            {
                _hintLabel.Text = $"Open UI failed: {ex.Message}";
            }
        }

        private void EnterSourceMode()
        {
            if (_muHelperWindow == null || ReferenceEquals(_sourceOriginalParent, _canvas))
                return;

            _sourceOriginalParent = _muHelperWindow.Parent;
            _sourceOriginalX = _muHelperWindow.X;
            _sourceOriginalY = _muHelperWindow.Y;
            _sourceOriginalVisible = _muHelperWindow.Visible;
            _sourceOriginalInteractive = _muHelperWindow.Interactive;
            _previousFocusControl = Scene?.FocusControl;
            _sourceInteractiveStates.Clear();
            CaptureInteractiveState(_muHelperWindow);
            _muHelperWindow.SetVisualDesignerEditing(true);
            SetSourceTreeInteractive(_muHelperWindow, false);
            _muHelperWindow.Visible = true;
            _muHelperWindow.Interactive = false;
            Scene?.Controls.Remove(_muHelperWindow);
            _canvas.Controls.Add(_muHelperWindow);
        }

        private void CaptureInteractiveState(GameControl control)
        {
            _sourceInteractiveStates[control] = control.Interactive;
            foreach (GameControl child in control.Controls.GetSnapshotArray())
                CaptureInteractiveState(child);
        }

        private static void SetSourceTreeInteractive(GameControl control, bool interactive)
        {
            control.Interactive = interactive;
            foreach (GameControl child in control.Controls.GetSnapshotArray())
                SetSourceTreeInteractive(child, interactive);
        }

        private void RestoreSourceMode()
        {
            if (_muHelperWindow == null || !ReferenceEquals(_muHelperWindow.Parent, _canvas))
                return;

            _canvas.ReleaseLiveSourceTree();
            if (_sourceOriginalParent != null && !_sourceOriginalParent.Controls.Contains(_muHelperWindow))
                _sourceOriginalParent.Controls.Add(_muHelperWindow);
            _muHelperWindow.X = _sourceOriginalX;
            _muHelperWindow.Y = _sourceOriginalY;
            _muHelperWindow.Visible = _sourceOriginalVisible;
            foreach ((GameControl control, bool interactive) in _sourceInteractiveStates)
                control.Interactive = interactive;
            _muHelperWindow.Interactive = _sourceOriginalInteractive;
            _muHelperWindow.SetVisualDesignerEditing(false);
            Scene.FocusControl = _previousFocusControl;
            _sourceOriginalParent = null;
            _sourceInteractiveStates.Clear();
        }

        private void SaveCSharpUi()
        {
            if (_sourceInspection == null || !_canvas.IsSourceMode)
            {
                _hintLabel.Text = "Open a supported C# UI source first.";
                return;
            }
            try
            {
                UiLayoutDocument document = _canvas.CreateSourceDocument(_sourceInspection.ClassName);
                bool staleBindings = !string.IsNullOrWhiteSpace(_sourceBindingError);
                string path = _sourceWriter.Save(_sourceInspection, document.Elements);
                _canvas.MarkSaved();
                string bindingNote = staleBindings
                    ? " Stale bindings were normalized to the current live control paths."
                    : string.Empty;
                _hintLabel.Text = $"C# layout saved: {path} ({document.Elements.Count} elements).{bindingNote} Rebuild/restart the client to apply the saved descriptors.";
            }
            catch (Exception ex)
            {
                _hintLabel.Text = $"C# save failed: {ex.Message}";
            }
        }

        private ButtonControl CreateToolbarButton(string text, int x, int y, int width, Action action)
        {
            var button = new ButtonControl
            {
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(width, 24),
                ViewSize = new Point(width, 24),
                AutoViewSize = false,
                FontSize = 8.5f,
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold,
                BackgroundColor = ModernHudTheme.BgMid,
                HoverBackgroundColor = ModernHudTheme.BgLight,
                PressedBackgroundColor = ModernHudTheme.BgDark
            };
            button.Click += (_, _) => action();
            Controls.Add(button);
            return button;
        }

        private void AddActionButton(string text, int x, int y, Action action, int width = 80)
        {
            CreateToolbarButton(text, x, y, width, action);
        }

        private void OnAssetSelected(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                _hintLabel.Text = "Select an asset, then drag it onto the canvas to place it";
                return;
            }

            bool assigned = _canvas.AssignSelectedAsset(relativePath);
            _hintLabel.Text = assigned
                ? $"Assigned asset to {_canvas.SelectedData.Name}: {relativePath}"
                : $"Selected asset: {relativePath} | Drag onto the canvas to add an image or onto an editable image to replace its texture";
        }

        private void OnElementSelected(UiLayoutElement data)
        {
            _hintLabel.Text = data == null
                ? "CANVAS READY | Create or select an object"
                : $"SELECTED: {data.Name} | {data.SourceControlType ?? data.Type}";
        }

        private void OnElementChanged(UiLayoutElement data)
        {
            if (data == null)
                return;

            _layers.RefreshElement(data);
            if (ReferenceEquals(data, _canvas.SelectedData))
            {
                _properties.RefreshSelected(data);
                _hintLabel.Text = $"EDITING: {data.Name} | {data.X},{data.Y} {data.Width}x{data.Height}";
            }
        }

        private void OnElementsChanged()
        {
            _layers.SetElements(_canvas.Elements, _canvas.SelectedData);
            _layers.SetSelected(_canvas.SelectedElements);
            _properties.SetSelected(_canvas.SelectedElements);
            UpdatePageSelector();
            UpdateHistoryButtons();
        }

        private void UpdateDirtyTitle(bool dirty)
        {
            _titleLabel.Text = dirty ? "MU UI EDITOR *" : "MU UI EDITOR";
        }

        private void UpdateHistoryButtons()
        {
            if (_documentSaveButton != null)
                _documentSaveButton.Text = _canvas.IsSourceMode ? "Save C#" : "Save";
            _undoButton.Interactive = _canvas.CanUndo;
            _redoButton.Interactive = _canvas.CanRedo;
            _undoButton.Text = "Undo";
            _redoButton.Text = "Redo";
            if (_viewportLabel != null)
                _viewportLabel.Text = $"{_canvas.ZoomPercent}% G{_canvas.GridSize}{(_canvas.GridEnabled ? "" : " off")}";
        }

        private void UpdatePageSelector()
        {
            int pageIndex = _canvas.Pages.ToList().FindIndex(page => string.Equals(page.Id, _canvas.CurrentPageId, StringComparison.Ordinal));
            string pagePosition = pageIndex >= 0 && _canvas.Pages.Count > 1 ? $" ({pageIndex + 1}/{_canvas.Pages.Count})" : string.Empty;
            _pageLabel.Text = $"PAGE: {_canvas.CurrentPageName}{pagePosition}";
            bool hasPages = _canvas.Pages.Count > 1;
            _previousPageButton.Visible = hasPages;
            _nextPageButton.Visible = hasPages;
            _previousPageButton.Interactive = hasPages;
            _nextPageButton.Interactive = hasPages;
        }

        private void PreviousPage()
        {
            if (_canvas.Pages.Count == 0)
                return;
            int index = Math.Max(0, _canvas.Pages.ToList().FindIndex(page => string.Equals(page.Id, _canvas.CurrentPageId, StringComparison.Ordinal)) - 1);
            _canvas.SelectPage(_canvas.Pages[index].Id);
            UpdatePageSelector();
            _hintLabel.Text = $"PAGE: {_canvas.CurrentPageName} | Select an object to edit";
        }

        private void NextPage()
        {
            if (_canvas.Pages.Count == 0)
                return;
            int current = _canvas.Pages.ToList().FindIndex(page => string.Equals(page.Id, _canvas.CurrentPageId, StringComparison.Ordinal));
            int index = Math.Min(_canvas.Pages.Count - 1, Math.Max(0, current) + 1);
            _canvas.SelectPage(_canvas.Pages[index].Id);
            UpdatePageSelector();
            _hintLabel.Text = $"PAGE: {_canvas.CurrentPageName} | Select an object to edit";
        }

        private void SaveCurrentDocument()
        {
            if (_canvas.IsSourceMode)
            {
                SaveCSharpUi();
                return;
            }

            SaveDocument();
        }

        private void SaveDocument()
        {
            try
            {
                UiLayoutDocument document = _canvas.CreateDocument(_layoutNameField.Value);
                string path = UiLayoutSerializer.Save(document);
                _canvas.MarkSaved();
                _layoutNameField.Value = document.Name;
                _hintLabel.Text = $"Layout saved: {path}";
            }
            catch (Exception ex)
            {
                _hintLabel.Text = $"Layout save failed: {ex.Message}";
            }
        }

        private async void LoadDocument()
        {
            try
            {
                UiLayoutDocument document = UiLayoutSerializer.Load(_layoutNameField.Value);
                _layoutNameField.Value = document.Name;
                await _canvas.LoadDocumentAsync(document);
                ScheduleEditorUpdate(() => _hintLabel.Text = $"Layout loaded: {document.Name}");
            }
            catch (Exception ex)
            {
                ScheduleEditorUpdate(() => _hintLabel.Text = $"Layout load failed: {ex.Message}");
            }
        }

        private async void PreviewLayout()
        {
            if (_runtimeLayout == null)
            {
                _hintLabel.Text = "Runtime layout host is not available.";
                return;
            }

            try
            {
                UiLayoutDocument document = _canvas.CreateDocument(_layoutNameField.Value);
                string path = UiLayoutSerializer.Save(document);
                bool loaded = await _runtimeLayout.LoadLayoutAsync(document.Name, show: true);
                ScheduleEditorUpdate(() =>
                {
                    if (!loaded)
                    {
                        _hintLabel.Text = _runtimeLayout.LastError ?? "Runtime preview failed to load.";
                        return;
                    }

                    _hintLabel.Text = $"Previewing saved layout: {path}";
                    Close();
                });
            }
            catch (Exception ex)
            {
                ScheduleEditorUpdate(() => _hintLabel.Text = $"Runtime preview failed: {ex.Message}");
            }
        }

        private static void ScheduleEditorUpdate(Action update)
        {
            if (MuGame.IsMainThread)
                update();
            else
                MuGame.ScheduleOnMainThread(update, MainThreadDispatcher.WorkPriority.High, "UiEditor.UpdateStatus");
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Update(gameTime);

            // The canvas intentionally defers document shortcuts while a property
            // text field is focused. Handle undo/redo here so keyboard focus never
            // disables editor history.
            KeyboardState keyboard = MuGame.Instance.Keyboard;
            KeyboardState previousKeyboard = MuGame.Instance.PrevKeyboard;
            bool control = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            if (Scene?.FocusControl is TextFieldControl textField && textField.Visible && control)
            {
                if (keyboard.IsKeyDown(Keys.Z) && previousKeyboard.IsKeyUp(Keys.Z))
                    _canvas.Undo();
                else if (keyboard.IsKeyDown(Keys.Y) && previousKeyboard.IsKeyUp(Keys.Y))
                    _canvas.Redo();
            }

            MouseState mouse = MuGame.Instance.UiMouseState;
            MouseState previous = MuGame.Instance.PrevUiMouseState;
            if (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released &&
                _layoutNameField.DisplayRectangle.Contains(mouse.Position))
            {
                _layoutNameField.Focus();
                Scene?.SetMouseInputConsumed();
            }
        }

        public void Open()
        {
            Visible = true;
            Interactive = true;
            BringToFront();
            Scene.FocusControl = this;
        }

        public void Close()
        {
            RestoreSourceMode();
            Visible = false;
            Interactive = false;
            if (Scene?.FocusControl == this)
                Scene.FocusControl = null;
        }

        public void Toggle()
        {
            if (Visible) Close(); else Open();
        }
    }
}
