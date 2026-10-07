using System.Collections.Generic;
using Client.Main.Controls;

namespace Client.Main.Controls.UI.Game.Editor
{
    internal sealed record GameUiDesignerPageDefinition(
        string Id,
        string Name,
        GameControl Root,
        bool IsWindowRoot = false);

    /// <summary>
    /// Runtime bridge implemented by real OpenMU UI roots that the in-game designer can inspect.
    /// Page definitions point at live control instances; no controls are fabricated for page preview.
    /// </summary>
    internal interface IGameUiDesignerRuntimeSource
    {
        string DesignerClassName { get; }
        string DesignerDefaultPageId { get; }
        string DesignerBindingWarning { get; }
        IReadOnlyList<GameUiDesignerPageDefinition> GetVisualDesignerPages();
        object CaptureVisualDesignerState();
        void ActivateVisualDesignerPage(string pageId);
        void RestoreVisualDesignerState(object state);
        bool TryGetVisualDesignerBinding(GameControl control, out string id, out string sourcePath);
        void RegisterVisualDesignerBinding(GameControl control, string id, string sourcePath);
        bool IsVisualDesignerTextDynamic(GameControl control);
        bool IsVisualDesignerVisibilityDynamic(GameControl control);
        bool IsVisualDesignerAssetEditable(GameControl control);
        bool IsVisualDesignerRoot(GameControl control);
        void SetVisualDesignerBindingWarning(string warning);
        void SetVisualDesignerEditing(bool editing);
    }
}
