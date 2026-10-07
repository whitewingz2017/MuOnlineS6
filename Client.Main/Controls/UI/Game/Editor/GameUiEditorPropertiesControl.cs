using System;
using System.Globalization;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Layouts;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Editor
{
    internal sealed class GameUiEditorPropertiesControl : UIControl
    {
        private readonly LabelControl _titleLabel;
        private readonly LabelControl _typeLabel;
        private readonly LabelControl _stateLabel;
        private readonly LabelControl _detailsLabel;
        private readonly ButtonControl _lockButton;
        private readonly ButtonControl _visibilityButton;
        private readonly TextFieldControl _nameField;
        private readonly TextFieldControl _xField;
        private readonly TextFieldControl _yField;
        private readonly TextFieldControl _widthField;
        private readonly TextFieldControl _heightField;
        private readonly TextFieldControl _textField;
        private readonly TextFieldControl _assetField;
        private readonly TextFieldControl _opacityField;
        private readonly TextFieldControl _fontSizeField;
        private UiLayoutElement _selected;
        private IReadOnlyList<UiLayoutElement> _selectedItems = Array.Empty<UiLayoutElement>();
        private bool _updating;

        public event Action<UiLayoutElement> Changed;
        public event Action<IReadOnlyList<UiLayoutElement>> ChangedMany;
        public event Action<UiLayoutElement> LockToggleRequested;
        public event Action<UiLayoutElement> VisibilityToggleRequested;

        public GameUiEditorPropertiesControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(240, 350);
            ViewSize = ControlSize;
            Interactive = true;
            BackgroundColor = new Color(12, 16, 24, 248);
            BorderColor = ModernHudTheme.BorderInner;
            BorderThickness = 1;

            _titleLabel = AddLabel("PROPERTIES", 12, 8, 210, 20, 13f, ModernHudTheme.TextGold, true);
            _typeLabel = AddLabel(string.Empty, 12, 30, 210, 16, 8.5f, ModernHudTheme.TextGray);
            _stateLabel = AddLabel(string.Empty, 12, 301, 216, 16, 7.5f, ModernHudTheme.TextGray);
            _detailsLabel = AddLabel(string.Empty, 12, 322, 216, 20, 6.8f, ModernHudTheme.TextGray);
            _lockButton = AddStateButton("Lock", 12, 48, 102);
            _visibilityButton = AddStateButton("Hide", 122, 48, 106);
            _lockButton.Click += (_, _) =>
            {
                if (_selected != null)
                    LockToggleRequested?.Invoke(_selected);
            };
            _visibilityButton.Click += (_, _) =>
            {
                if (_selected != null && _selected.CanEditVisibility)
                    VisibilityToggleRequested?.Invoke(_selected);
            };

            _nameField = AddField("Name", 72);
            _xField = AddField("X", 97);
            _yField = AddField("Y", 122);
            _widthField = AddField("Width", 147);
            _heightField = AddField("Height", 172);
            _textField = AddField("Text", 197);
            _assetField = AddField("Asset", 222);
            _opacityField = AddField("Opacity", 247);
            _fontSizeField = AddField("Font Size", 272);

            Subscribe(_nameField);
            Subscribe(_xField);
            Subscribe(_yField);
            Subscribe(_widthField);
            Subscribe(_heightField);
            Subscribe(_textField);
            Subscribe(_assetField);
            Subscribe(_opacityField);
            Subscribe(_fontSizeField);
            SetSelected((UiLayoutElement)null);
        }

        private LabelControl AddLabel(string text, int x, int y, int width, int height, float size, Color color, bool bold = false)
        {
            var label = new LabelControl
            {
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(width, height),
                ViewSize = new Point(width, height),
                FontSize = size,
                TextColor = color,
                IsBold = bold,
                HasShadow = true
            };
            Controls.Add(label);
            return label;
        }

        private ButtonControl AddStateButton(string text, int x, int y, int width)
        {
            var button = new ButtonControl
            {
                Text = text,
                X = x,
                Y = y,
                ControlSize = new Point(width, 22),
                ViewSize = new Point(width, 22),
                AutoViewSize = false,
                FontSize = 8f,
                TextColor = ModernHudTheme.TextWhite,
                HoverTextColor = ModernHudTheme.TextGold,
                BackgroundColor = ModernHudTheme.BgMid,
                HoverBackgroundColor = ModernHudTheme.BgLight,
                PressedBackgroundColor = ModernHudTheme.BgDark
            };
            Controls.Add(button);
            return button;
        }

        private TextFieldControl AddField(string label, int y)
        {
            AddLabel(label, 12, y + 3, 58, 22, 9.5f, ModernHudTheme.TextGray);
            var field = TextFieldControl.Create();
            field.X = 72;
            field.Y = y;
            field.ControlSize = new Point(154, 26);
            field.ViewSize = field.ControlSize;
            field.FontSize = 9f;
            field.TextColor = ModernHudTheme.TextWhite;
            field.BackgroundColor = ModernHudTheme.BgDarkest;
            field.BorderColor = ModernHudTheme.BorderInner;
            Controls.Add(field);
            return field;
        }

        private void Subscribe(TextFieldControl field) => field.ValueChanged += (_, _) => ApplyFields();

        public void SetSelected(UiLayoutElement data)
        {
            SetSelected(data == null ? Array.Empty<UiLayoutElement>() : new[] { data });
        }

        public void SetSelected(IReadOnlyList<UiLayoutElement> data)
        {
            _selectedItems = data ?? Array.Empty<UiLayoutElement>();
            _selected = _selectedItems.LastOrDefault();
            bool hasSelection = _selectedItems.Count > 0;
            _nameField.Visible = hasSelection;
            _xField.Visible = hasSelection;
            _yField.Visible = hasSelection;
            _widthField.Visible = hasSelection;
            _heightField.Visible = hasSelection;
            _textField.Visible = hasSelection;
            _assetField.Visible = hasSelection;
            _opacityField.Visible = hasSelection;
            _fontSizeField.Visible = hasSelection;
            _lockButton.Visible = hasSelection;
            _visibilityButton.Visible = hasSelection;
            _typeLabel.Visible = hasSelection;
            _stateLabel.Visible = hasSelection;
            _detailsLabel.Visible = hasSelection;
            if (!hasSelection)
            {
                _titleLabel.Text = "PROPERTIES: none";
                _detailsLabel.Text = string.Empty;
            }
            RefreshSelected(_selected, true);
        }

        public void RefreshSelected(UiLayoutElement data, bool forceFieldRefresh = false)
        {
            if (data == null)
                return;

            _selected = data;
            _updating = true;
            bool multiple = _selectedItems.Count > 1;
            _titleLabel.Text = multiple ? $"PROPERTIES: {_selectedItems.Count} objects" : "PROPERTIES";
            _typeLabel.Text = multiple
                ? "Multiple selection | shared editable properties"
                : $"Page: {data.DesignerPageName ?? data.DesignerPageId ?? "Root / Shared"} | Type: {data.SourceControlType ?? data.Type ?? "Unknown"}   Z: {data.Layer}";
            _stateLabel.Text = $"{(data.Visible ? "Visible" : "Hidden")}   |   {(data.Locked ? "Locked" : "Unlocked")}";
            _lockButton.Text = data.Locked ? "Unlock" : "Lock";
            _visibilityButton.Text = data.Visible ? "Hide" : "Show";
            _lockButton.Interactive = true;
            _visibilityButton.Interactive = data.CanEditVisibility;
            _detailsLabel.Text = data.UnsupportedReason ?? string.Empty;
            SetFieldValue(_nameField, multiple ? "[Mixed]" : data.Name ?? string.Empty, forceFieldRefresh);
            SetFieldValue(_xField, MixedValue(item => item.X.ToString(CultureInfo.InvariantCulture)), forceFieldRefresh);
            SetFieldValue(_yField, MixedValue(item => item.Y.ToString(CultureInfo.InvariantCulture)), forceFieldRefresh);
            SetFieldValue(_widthField, MixedValue(item => item.Width.ToString(CultureInfo.InvariantCulture)), forceFieldRefresh);
            SetFieldValue(_heightField, MixedValue(item => item.Height.ToString(CultureInfo.InvariantCulture)), forceFieldRefresh);
            SetFieldValue(_textField, multiple ? "[Mixed]" : data.Text ?? string.Empty, forceFieldRefresh);
            SetFieldValue(_assetField, multiple ? "[Mixed]" : data.Asset ?? string.Empty, forceFieldRefresh);
            SetFieldValue(_opacityField, MixedValue(item => item.Opacity.ToString("0.##", CultureInfo.InvariantCulture)), forceFieldRefresh);
            SetFieldValue(_fontSizeField, MixedValue(item => item.FontSize.ToString("0.##", CultureInfo.InvariantCulture)), forceFieldRefresh);
            bool geometryEditable = data.CanEditGeometry && !data.Locked;
            _xField.Interactive = geometryEditable;
            _yField.Interactive = geometryEditable;
            _widthField.Interactive = geometryEditable;
            _heightField.Interactive = geometryEditable;
            _textField.Interactive = data.CanEditText;
            _assetField.Interactive = data.CanEditAsset;
            _opacityField.Interactive = data.CanEditOpacity;
            _fontSizeField.Interactive = data.CanEditFontSize;
            _updating = false;
        }

        private string MixedValue(Func<UiLayoutElement, string> value)
        {
            if (_selectedItems.Count == 0)
                return string.Empty;
            string first = value(_selectedItems[0]);
            return _selectedItems.Skip(1).All(item => string.Equals(value(item), first, StringComparison.Ordinal)) ? first : "[Mixed]";
        }

        private void SetFieldValue(TextFieldControl field, string value, bool force)
        {
            if (force || !field.HasFocus)
                field.Value = value;
        }

        private void ApplyFields()
        {
            if (_updating || _selected == null)
                return;

            var updated = new List<UiLayoutElement>(_selectedItems.Count);
            foreach (UiLayoutElement original in _selectedItems)
            {
                UiLayoutElement item = GameUiEditorDocumentState.CloneElement(original);
                if (_selectedItems.Count == 1 && !string.Equals(_nameField.Value, "[Mixed]", StringComparison.Ordinal))
                    item.Name = _nameField.Value;
                if (item.CanEditGeometry && !item.Locked)
                {
                    if (int.TryParse(_xField.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)) item.X = x;
                    if (int.TryParse(_yField.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)) item.Y = y;
                    if (int.TryParse(_widthField.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)) item.Width = Math.Max(8, width);
                    if (int.TryParse(_heightField.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int height)) item.Height = Math.Max(8, height);
                }
                if (item.CanEditOpacity && float.TryParse(_opacityField.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float opacity))
                    item.Opacity = Math.Clamp(opacity, 0f, 1f);
                if (item.CanEditText && !string.Equals(_textField.Value, "[Mixed]", StringComparison.Ordinal))
                    item.Text = _textField.Value;
                if (item.CanEditFontSize && float.TryParse(_fontSizeField.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fontSize))
                    item.FontSize = Math.Clamp(fontSize, 1f, 256f);
                if (item.CanEditAsset && !string.Equals(_assetField.Value, "[Mixed]", StringComparison.Ordinal))
                    item.Asset = _assetField.Value;
                updated.Add(item);
            }
            if (updated.Count > 1)
                ChangedMany?.Invoke(updated);
            else if (updated.Count == 1)
                Changed?.Invoke(updated[0]);
        }
    }
}
