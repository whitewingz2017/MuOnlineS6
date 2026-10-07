using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Client.Main.Controls;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Editor;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Helper
{
    /// <summary>
    /// Generated visual overrides are isolated from MuHelperWindow's behavior and callbacks.
    /// The designer rewrites only the descriptor arrays in this file.
    /// </summary>
    internal sealed partial class MuHelperWindow : IGameUiDesignerRuntimeSource
    {
        private sealed record VisualDesignerElement(
            string Id,
            string ParentId,
            string SourcePath,
            string Type,
            string Name,
            string Asset,
            string Text,
            int X,
            int Y,
            int Width,
            int Height,
            float Opacity,
            float FontSize,
            int Layer,
            bool Visible,
            bool Locked,
            bool IsAdded,
            bool CanEditGeometry,
            bool CanEditText,
            bool CanEditAsset,
            bool CanEditOpacity,
            bool CanEditFontSize,
            bool CanEditVisibility,
            string SourceControlType = null)
        {
            public VisualDesignerElement(
                string id,
                string parentId,
                string sourcePath,
                string type,
                string name,
                string asset,
                string text,
                int x,
                int y,
                int width,
                int height,
                float opacity,
                float fontSize,
                int layer,
                bool visible,
                bool locked,
                bool isAdded,
                bool canEditText,
                bool canEditAsset,
                bool canEditVisibility)
                : this(id, parentId, sourcePath, type, name, asset, text, x, y, width, height, opacity, fontSize, layer,
                    visible, locked, isAdded, true, canEditText, canEditAsset, true, canEditText, canEditVisibility, null)
            {
            }
        }

        private readonly Dictionary<string, GameControl> _visualDesignerControls = new(StringComparer.Ordinal);
        private readonly Dictionary<GameControl, (string Id, string SourcePath)> _visualDesignerBindings = new();
        private bool _visualDesignerControlsBound;
        private bool _visualDesignerEditing;
        internal string VisualDesignerBindingWarning { get; private set; }

        public string DesignerClassName => nameof(MuHelperWindow);
        public string DesignerDefaultPageId => "root";
        public string DesignerBindingWarning => VisualDesignerBindingWarning;

        private sealed record VisualDesignerState(int ActiveTab, bool ShowPotionSettings, Point[] PageOffsets, bool RootVisible, bool RootInteractive);

        public IReadOnlyList<GameUiDesignerPageDefinition> GetVisualDesignerPages()
        {
            Name = nameof(MuHelperWindow);
            var pages = new List<GameUiDesignerPageDefinition>
            {
                new("root", "Root / Shared", this, true)
            };

            int tabCount = Math.Min(_pages.Length, _tabButtons.Length);
            for (int i = 0; i < tabCount; i++)
            {
                UIControl page = _pages[i];
                if (page == null)
                    continue;

                string pageName = _tabButtons[i]?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(pageName))
                    pageName = $"Page {i + 1}";
                page.Name = pageName;
                pages.Add(new GameUiDesignerPageDefinition($"tab:{i}", pageName, page));
            }

            if (_potionSettingsPage != null)
            {
                _potionSettingsPage.Name = "Potion Settings";
                pages.Add(new GameUiDesignerPageDefinition("potion", _potionSettingsPage.Name, _potionSettingsPage));
            }
            return pages;
        }

        public object CaptureVisualDesignerState() => new VisualDesignerState(
            _activeTab,
            _showPotionSettings,
            _pages.Select(page => page.Offset).ToArray(),
            Visible,
            Interactive);

        public void ActivateVisualDesignerPage(string pageId)
        {
            if (string.Equals(pageId, "root", StringComparison.Ordinal))
            {
                _showPotionSettings = false;
                SetActiveTab(_activeTab);
            }
            else if (string.Equals(pageId, "potion", StringComparison.Ordinal))
            {
                _showPotionSettings = true;
                UpdatePageChildVisibility();
                UpdateClassicHuntingControlVisibility();
            }
            else if (pageId.StartsWith("tab:", StringComparison.Ordinal) &&
                     int.TryParse(pageId[4..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tabIndex))
            {
                _showPotionSettings = false;
                SetActiveTab(tabIndex);
            }

            UpdatePageChildVisibility();
            UpdateClassicHuntingControlVisibility();
            RefreshValues();
        }

        public void RestoreVisualDesignerState(object state)
        {
            if (state is not VisualDesignerState saved)
                return;
            _activeTab = Math.Clamp(saved.ActiveTab, 0, _pages.Length - 1);
            _showPotionSettings = saved.ShowPotionSettings;
            for (int i = 0; i < Math.Min(_pages.Length, saved.PageOffsets.Length); i++)
                _pages[i].Offset = saved.PageOffsets[i];
            Visible = saved.RootVisible;
            Interactive = saved.RootInteractive;
            UpdatePageChildVisibility();
            UpdateClassicHuntingControlVisibility();
            RefreshValues();
        }

        public void SetVisualDesignerEditing(bool editing)
        {
            _visualDesignerEditing = editing;
        }

        private void ApplyVisualDesignerLayout()
        {
            // The canvas edits the live controls directly. Reapplying the saved descriptors while
            // editing would overwrite every drag/resize with the previous persisted geometry.
            if (_visualDesignerEditing)
                return;

            if (!_visualDesignerControlsBound)
                BindVisualDesignerControls();
            if (!string.IsNullOrWhiteSpace(VisualDesignerBindingWarning))
                return;

            foreach (VisualDesignerElement element in VisualDesignerElements)
            {
                if (!_visualDesignerControls.TryGetValue(element.Id, out GameControl control))
                    continue;

                control.Name = element.Name;
                if (element.CanEditGeometry)
                {
                    control.X = element.X;
                    control.Y = element.Y;
                    Point size = new(Math.Max(1, element.Width), Math.Max(1, element.Height));
                    control.ControlSize = size;
                    control.ViewSize = size;
                }
                if (element.CanEditVisibility)
                    control.Visible = element.Visible;
                if (element.CanEditOpacity)
                {
                    control.Alpha = Math.Clamp(element.Opacity, 0f, 1f);
                    if (control is TextureControl textureControl)
                        textureControl.Alpha = Math.Clamp(element.Opacity, 0f, 1f);
                }
                if (control is LabelControl label)
                {
                    if (element.CanEditOpacity)
                        label.Alpha = Math.Clamp(element.Opacity, 0f, 1f);
                    if (element.CanEditFontSize)
                        label.FontSize = Math.Clamp(element.FontSize, 1f, 256f);
                    if (element.CanEditText)
                        label.Text = element.Text ?? string.Empty;
                }
                if (control is ButtonControl button)
                {
                    if (element.CanEditFontSize)
                        button.FontSize = Math.Clamp(element.FontSize, 1f, 256f);
                    if (element.CanEditText)
                        button.Text = element.Text ?? string.Empty;
                }
                if (control is SpriteControl sprite && element.CanEditAsset &&
                    !string.Equals(sprite.TexturePath ?? string.Empty, element.Asset ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    sprite.TexturePath = string.IsNullOrWhiteSpace(element.Asset) ? null : element.Asset;
                }
            }

            foreach (IGrouping<string, VisualDesignerElement> siblingGroup in VisualDesignerElements
                         .GroupBy(element => element.ParentId ?? string.Empty, StringComparer.Ordinal))
            {
                foreach (VisualDesignerElement element in siblingGroup.OrderBy(element => element.Layer))
                {
                    if (_visualDesignerControls.TryGetValue(element.Id, out GameControl control))
                        control.BringToFront();
                }
            }
        }

        private void BindVisualDesignerControls()
        {
            _visualDesignerControls.Clear();
            _visualDesignerBindings.Clear();
            VisualDesignerBindingWarning = null;
            var bindingProblems = new List<string>();
            var sourcePaths = new HashSet<string>(StringComparer.Ordinal);
            var boundIds = new HashSet<string>(StringComparer.Ordinal);
            var boundControls = new HashSet<GameControl>();
            foreach (VisualDesignerElement element in VisualDesignerElements.Where(element => !element.IsAdded))
            {
                if (string.IsNullOrWhiteSpace(element.Id) || !boundIds.Add(element.Id) || !sourcePaths.Add(element.SourcePath ?? string.Empty))
                {
                    bindingProblems.Add($"Duplicate or empty binding for '{element.Name}'.");
                    continue;
                }

                GameControl control = ResolveVisualDesignerPath(element.SourcePath);
                if (control == null)
                {
                    bindingProblems.Add($"'{element.Name}' no longer resolves at source path '{element.SourcePath}'.");
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(element.SourceControlType) &&
                    !string.Equals(control.GetType().Name, element.SourceControlType, StringComparison.Ordinal))
                {
                    bindingProblems.Add($"'{element.Name}' expected {element.SourceControlType}, found {control.GetType().Name} at '{element.SourcePath}'.");
                    continue;
                }
                if (!boundControls.Add(control))
                {
                    bindingProblems.Add($"Multiple designer IDs resolve to the same control at '{element.SourcePath}'.");
                    continue;
                }

                _visualDesignerControls[element.Id] = control;
                _visualDesignerBindings[control] = (element.Id, element.SourcePath);
            }

            bool addedAny;
            do
            {
                addedAny = false;
                foreach (VisualDesignerElement element in VisualDesignerElements.Where(element => element.IsAdded))
                {
                    if (_visualDesignerControls.ContainsKey(element.Id))
                        continue;
                    GameControl parent = string.IsNullOrEmpty(element.ParentId)
                        ? this
                        : _visualDesignerControls.GetValueOrDefault(element.ParentId);
                    if (parent == null)
                        continue;

                    GameControl control = CreateVisualDesignerControl(element);
                    parent.Controls.Add(control);
                    _visualDesignerControls[element.Id] = control;
                    _visualDesignerBindings[control] = (element.Id, null);
                    addedAny = true;
                }
            }
            while (addedAny);

            foreach (VisualDesignerElement element in VisualDesignerElements.Where(element => element.IsAdded && !_visualDesignerControls.ContainsKey(element.Id)))
                bindingProblems.Add($"New control '{element.Name}' could not be attached to its saved parent ID '{element.ParentId}'.");
            if (bindingProblems.Count > 0)
                VisualDesignerBindingWarning = string.Join(" ", bindingProblems.Take(4));

            _visualDesignerControlsBound = true;
        }

        private static GameControl CreateVisualDesignerControl(VisualDesignerElement element)
        {
            GameControl control = element.Type switch
            {
                "image" => new SpriteControl { TexturePath = element.Asset, AutoViewSize = false },
                "text" => new LabelControl { Text = element.Text ?? string.Empty, FontSize = Math.Clamp(element.FontSize, 1f, 256f) },
                "button" => new ButtonControl { Text = element.Text ?? string.Empty, FontSize = Math.Clamp(element.FontSize, 1f, 256f) },
                "panel" => new VisualDesignerPanelControl(),
                _ => throw new InvalidOperationException($"Unsupported generated UI control type '{element.Type}'.")
            };
            control.Name = element.Name;
            control.X = element.X;
            control.Y = element.Y;
            control.ControlSize = new Point(Math.Max(1, element.Width), Math.Max(1, element.Height));
            control.ViewSize = control.ControlSize;
            control.Alpha = Math.Clamp(element.Opacity, 0f, 1f);
            control.Visible = element.Visible;
            control.Interactive = control is ButtonControl;
            if (control is TextureControl textureControl)
                textureControl.Alpha = Math.Clamp(element.Opacity, 0f, 1f);
            if (control is LabelControl label)
                label.Alpha = Math.Clamp(element.Opacity, 0f, 1f);
            return control;
        }

        private GameControl ResolveVisualDesignerPath(string sourcePath)
        {
            GameControl current = this;
            if (string.IsNullOrEmpty(sourcePath))
                return current;

            foreach (string segment in sourcePath.Split('/'))
            {
                if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out int childIndex) ||
                    childIndex < 0 || childIndex >= current.Controls.Count)
                    return null;
                current = current.Controls[childIndex];
            }
            return current;
        }

        internal void SetVisualDesignerBindingWarning(string warning)
        {
            VisualDesignerBindingWarning = warning;
        }

        bool IGameUiDesignerRuntimeSource.TryGetVisualDesignerBinding(GameControl control, out string id, out string sourcePath) =>
            TryGetVisualDesignerBinding(control, out id, out sourcePath);

        void IGameUiDesignerRuntimeSource.RegisterVisualDesignerBinding(GameControl control, string id, string sourcePath) =>
            RegisterVisualDesignerBinding(control, id, sourcePath);

        bool IGameUiDesignerRuntimeSource.IsVisualDesignerTextDynamic(GameControl control) =>
            IsVisualDesignerTextDynamic(control);

        bool IGameUiDesignerRuntimeSource.IsVisualDesignerVisibilityDynamic(GameControl control) =>
            IsVisualDesignerVisibilityDynamic(control);

        bool IGameUiDesignerRuntimeSource.IsVisualDesignerAssetEditable(GameControl control) =>
            IsVisualDesignerAssetEditable(control);

        bool IGameUiDesignerRuntimeSource.IsVisualDesignerRoot(GameControl control) =>
            IsVisualDesignerRoot(control);

        void IGameUiDesignerRuntimeSource.SetVisualDesignerBindingWarning(string warning) =>
            SetVisualDesignerBindingWarning(warning);

        internal bool TryGetVisualDesignerBinding(GameControl control, out string id, out string sourcePath)
        {
            if (_visualDesignerBindings.TryGetValue(control, out (string Id, string SourcePath) binding))
            {
                id = binding.Id;
                sourcePath = binding.SourcePath;
                return true;
            }

            id = null;
            sourcePath = null;
            return false;
        }

        internal void RegisterVisualDesignerBinding(GameControl control, string id, string sourcePath)
        {
            if (control == null || string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A live designer control and non-empty ID are required.");

            _visualDesignerControls[id] = control;
            _visualDesignerBindings[control] = (id, sourcePath);
        }

        internal bool IsVisualDesignerTextDynamic(GameControl control)
        {
            if (ReferenceEquals(control, _startButton) || ReferenceEquals(control, _resetButton) ||
                ReferenceEquals(control, _saveButton) || _boundButtons.Any(item => ReferenceEquals(item.Button, control)) ||
                _boundLabels.Any(item => ReferenceEquals(item.Label, control)))
                return true;

            return false;
        }

        internal bool IsVisualDesignerVisibilityDynamic(GameControl control)
        {
            if (ReferenceEquals(control, this) || ReferenceEquals(control, _potionSettingsPage) || _pages.Any(page => ReferenceEquals(page, control)))
                return true;

            for (GameControl current = control?.Parent; current != null && !ReferenceEquals(current, this); current = current.Parent)
            {
                if (ReferenceEquals(current, _potionSettingsPage) || _pages.Any(page => ReferenceEquals(page, current)))
                    return true;
            }
            return false;
        }

        internal bool IsVisualDesignerAssetEditable(GameControl control)
        {
            if (control is not SpriteControl)
                return false;
            string typeName = control.GetType().Name;
            // The skill-slot and threshold controls render from state-specific atlases instead of
            // their TexturePath. Other SpriteControl-based page controls can safely use the asset browser.
            return typeName is not ("SkillSlotButton" or "HelperThresholdSegmentButton");
        }

        internal bool IsVisualDesignerRoot(GameControl control) => ReferenceEquals(control, this);

        private sealed class VisualDesignerPanelControl : UIControl
        {
            public VisualDesignerPanelControl()
            {
                BackgroundColor = ModernHudTheme.BgMid;
                BorderColor = ModernHudTheme.BorderInner;
                BorderThickness = 1;
            }
        }
    }
}
