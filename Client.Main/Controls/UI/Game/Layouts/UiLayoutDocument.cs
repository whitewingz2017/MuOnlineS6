using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Client.Main.Controls.UI.Game.Layouts
{
    /// <summary>
    /// Serializable design document shared by the UI editor and runtime layout loader.
    /// Element coordinates and dimensions are expressed in the document's canvas units.
    /// </summary>
    public sealed class UiLayoutDocument
    {
        public int Version { get; set; } = 1;
        public string Name { get; set; } = "Untitled";
        public int Width { get; set; } = 600;
        public int Height { get; set; } = 610;
        public List<UiLayoutElement> Elements { get; set; } = new();
    }

    public sealed class UiLayoutElement
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public string Asset { get; set; }
        public string Text { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Opacity { get; set; } = 1f;
        public float FontSize { get; set; }
        public int Layer { get; set; }
        public bool Visible { get; set; } = true;
        public bool Locked { get; set; }

        [JsonIgnore]
        public string SourcePath { get; set; }

        [JsonIgnore]
        public string SourceControlType { get; set; }

        [JsonIgnore]
        public string DesignerPageId { get; set; }

        [JsonIgnore]
        public string DesignerPageName { get; set; }

        [JsonIgnore]
        public bool IsSourceBacked { get; set; }

        [JsonIgnore]
        public bool IsDesignerAdded { get; set; }

        [JsonIgnore]
        public bool CanEditGeometry { get; set; } = true;

        [JsonIgnore]
        public bool CanEditText { get; set; } = true;

        [JsonIgnore]
        public bool CanEditAsset { get; set; } = true;

        [JsonIgnore]
        public bool CanEditOpacity { get; set; } = true;

        [JsonIgnore]
        public bool CanEditFontSize { get; set; } = true;

        [JsonIgnore]
        public bool CanEditVisibility { get; set; } = true;

        [JsonIgnore]
        public bool CanReorder { get; set; } = true;

        [JsonIgnore]
        public string UnsupportedReason { get; set; }
    }
}
