namespace Client.Main.Configuration
{
    /// <summary>
    /// Selects the gameplay HUD shell without changing the shared game/UI systems.
    /// ClassicPc is the Windows default; Hybrid is enabled explicitly in settings.
    /// </summary>
    public enum HudTheme
    {
        Hybrid,
        ClassicPc
    }
}
