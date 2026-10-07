namespace Client.Main.Controls.UI.Game.Editor
{
    internal static class GameUiEditorDragState
    {
        public static string RelativePath { get; private set; }
        public static bool IsDragging => !string.IsNullOrWhiteSpace(RelativePath);

        public static void Begin(string relativePath)
        {
            RelativePath = relativePath;
        }

        public static string Take()
        {
            string path = RelativePath;
            RelativePath = null;
            return path;
        }

        public static void Cancel()
        {
            RelativePath = null;
        }
    }
}
