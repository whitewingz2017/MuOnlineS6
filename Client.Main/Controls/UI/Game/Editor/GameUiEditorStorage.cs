using Client.Main.Controls.UI.Game.Layouts;

namespace Client.Main.Controls.UI.Game.Editor
{
    /// <summary>
    /// Compatibility facade retained for editor integrations that used the old storage type.
    /// New code should use UiLayoutSerializer and named layout files.
    /// </summary>
    internal static class GameUiEditorStorage
    {
        public static string Save(UiLayoutDocument document) => UiLayoutSerializer.Save(document);

        public static UiLayoutDocument Load(string name = "CharacterWindow") => UiLayoutSerializer.Load(name);

        public static string PathFor(string name = "CharacterWindow") => UiLayoutSerializer.GetLayoutPath(name);
    }
}
