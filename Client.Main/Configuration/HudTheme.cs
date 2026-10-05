namespace Client.Main.Configuration
{
    /// <summary>
    /// Selects the gameplay HUD shell without changing the shared game/UI systems.
    /// Hybrid is the existing default HUD; ClassicPc is the optional PC Season 6 bar.
    /// </summary>
    public enum HudTheme
    {
        Hybrid,
        ClassicPc
    }
}
