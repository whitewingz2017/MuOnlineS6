using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Controls.UI.Game.Layouts;

namespace Client.Main.Controls.UI.Game.Editor
{
    internal sealed class GameUiEditorDocumentState
    {
        public string Name { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public List<UiLayoutElement> Elements { get; init; } = new();
        public List<string> SelectedIds { get; init; } = new();

        public GameUiEditorDocumentState Clone() => new()
        {
            Name = Name,
            Width = Width,
            Height = Height,
            Elements = Elements.Select(CloneElement).ToList(),
            SelectedIds = SelectedIds.ToList()
        };

        public bool HasSameDocument(GameUiEditorDocumentState other)
        {
            if (other == null || !string.Equals(Name, other.Name, StringComparison.Ordinal) ||
                Width != other.Width || Height != other.Height || Elements.Count != other.Elements.Count)
                return false;

            for (int i = 0; i < Elements.Count; i++)
                if (!HaveSameElement(Elements[i], other.Elements[i]))
                    return false;
            return true;
        }

        public static UiLayoutElement CloneElement(UiLayoutElement item) => new()
        {
            Id = item.Id,
            Type = item.Type,
            Name = item.Name,
            ParentId = item.ParentId,
            Asset = item.Asset,
            Text = item.Text,
            X = item.X,
            Y = item.Y,
            Width = item.Width,
            Height = item.Height,
            Opacity = item.Opacity,
            FontSize = item.FontSize,
            Layer = item.Layer,
            Visible = item.Visible,
            Locked = item.Locked,
            SourcePath = item.SourcePath,
            SourceControlType = item.SourceControlType,
            DesignerPageId = item.DesignerPageId,
            DesignerPageName = item.DesignerPageName,
            IsSourceBacked = item.IsSourceBacked,
            IsDesignerAdded = item.IsDesignerAdded,
            CanEditGeometry = item.CanEditGeometry,
            CanEditText = item.CanEditText,
            CanEditAsset = item.CanEditAsset,
            CanEditOpacity = item.CanEditOpacity,
            CanEditFontSize = item.CanEditFontSize,
            CanEditVisibility = item.CanEditVisibility,
            CanReorder = item.CanReorder,
            UnsupportedReason = item.UnsupportedReason
        };

        private static bool HaveSameElement(UiLayoutElement left, UiLayoutElement right) =>
            string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
            string.Equals(left.Type, right.Type, StringComparison.Ordinal) &&
            string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
            string.Equals(left.ParentId, right.ParentId, StringComparison.Ordinal) &&
            string.Equals(left.Asset, right.Asset, StringComparison.Ordinal) &&
            string.Equals(left.Text, right.Text, StringComparison.Ordinal) &&
            left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height &&
            left.Opacity.Equals(right.Opacity) && left.FontSize.Equals(right.FontSize) && left.Layer == right.Layer &&
            left.Visible == right.Visible && left.Locked == right.Locked &&
            string.Equals(left.SourcePath, right.SourcePath, StringComparison.Ordinal) &&
            string.Equals(left.SourceControlType, right.SourceControlType, StringComparison.Ordinal) &&
            string.Equals(left.DesignerPageId, right.DesignerPageId, StringComparison.Ordinal) &&
            string.Equals(left.DesignerPageName, right.DesignerPageName, StringComparison.Ordinal) &&
            left.IsSourceBacked == right.IsSourceBacked && left.IsDesignerAdded == right.IsDesignerAdded &&
            left.CanEditGeometry == right.CanEditGeometry && left.CanEditText == right.CanEditText &&
            left.CanEditAsset == right.CanEditAsset && left.CanEditOpacity == right.CanEditOpacity &&
            left.CanEditFontSize == right.CanEditFontSize && left.CanEditVisibility == right.CanEditVisibility &&
            left.CanReorder == right.CanReorder &&
            string.Equals(left.UnsupportedReason, right.UnsupportedReason, StringComparison.Ordinal);
    }

    internal sealed class GameUiEditorHistory
    {
        private sealed record Entry(
            GameUiEditorDocumentState Before,
            GameUiEditorDocumentState After,
            string Description,
            string CoalesceKey,
            DateTime CreatedUtc);

        private const int MaximumEntries = 100;
        private static readonly TimeSpan CoalesceWindow = TimeSpan.FromMilliseconds(750);
        private readonly List<Entry> _undo = new();
        private readonly List<Entry> _redo = new();
        private GameUiEditorDocumentState _savedState;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;
        public string UndoDescription => _undo.Count == 0 ? null : _undo[^1].Description;
        public bool IsDirty(GameUiEditorDocumentState current) =>
            _savedState != null && !_savedState.HasSameDocument(current);

        public void Reset(GameUiEditorDocumentState state)
        {
            _undo.Clear();
            _redo.Clear();
            _savedState = state?.Clone();
        }

        public void MarkSaved(GameUiEditorDocumentState state) => _savedState = state?.Clone();

        public bool Commit(GameUiEditorDocumentState before, GameUiEditorDocumentState after, string description, string coalesceKey = null)
        {
            if (before == null || after == null || before.HasSameDocument(after))
                return false;

            DateTime now = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(coalesceKey) && _undo.Count > 0)
            {
                Entry previous = _undo[^1];
                if (string.Equals(previous.CoalesceKey, coalesceKey, StringComparison.Ordinal) &&
                    now - previous.CreatedUtc <= CoalesceWindow)
                {
                    _undo[^1] = previous with { After = after.Clone(), CreatedUtc = now };
                    _redo.Clear();
                    return true;
                }
            }

            _undo.Add(new Entry(before.Clone(), after.Clone(), description ?? "Edit", coalesceKey, now));
            if (_undo.Count > MaximumEntries)
                _undo.RemoveAt(0);
            _redo.Clear();
            return true;
        }

        public bool TryUndo(out GameUiEditorDocumentState state)
        {
            if (_undo.Count == 0)
            {
                state = null;
                return false;
            }

            Entry entry = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            _redo.Add(entry);
            state = entry.Before.Clone();
            return true;
        }

        public bool TryRedo(out GameUiEditorDocumentState state)
        {
            if (_redo.Count == 0)
            {
                state = null;
                return false;
            }

            Entry entry = _redo[^1];
            _redo.RemoveAt(_redo.Count - 1);
            _undo.Add(entry);
            state = entry.After.Clone();
            return true;
        }
    }
}
