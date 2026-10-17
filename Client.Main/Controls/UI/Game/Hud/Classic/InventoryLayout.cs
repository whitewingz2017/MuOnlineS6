namespace Client.Main.Controls.UI.Game.Hud;

/// <summary>
/// Inventory geometry shared by all themes. X/Y coordinates are local to the window.
/// </summary>
public static class InventoryLayout
{
    public static int PanelW { get; set; } = 320;
    public static int PanelH { get; set; } = 716;
    public static int DragBarH { get; set; } = 82;

    public static int CloseX { get; set; } = 282;
    public static int CloseY { get; set; } = 10;
    public static int CloseW { get; set; } = 24;
    public static int CloseH { get; set; } = 26;
    public static int SetOptionX { get; set; } = 36;
    public static int SocketOptionX { get; set; } = 164;
    public static int OptionY { get; set; } = 40;
    public static int OptionW { get; set; } = 120;
    public static int OptionH { get; set; } = 25;

    public static int EquipmentX { get; set; } = 24;
    public static int EquipmentY { get; set; } = 74;
    public static int EquipmentW { get; set; } = 272;
    public static int EquipmentH { get; set; } = 240;

    public static int PaperdollX { get; set; } = 38;
    public static int PaperdollY { get; set; } = 98;
    public static int PaperdollW { get; set; } = 319;
    public static int PaperdollH { get; set; } = 240;

    public static int PetX { get; set; } = 24;
    public static int PetY { get; set; } = 74;
    public static int PetW { get; set; } = 64;
    public static int PetH { get; set; } = 64;

    public static int PendantX { get; set; } = 92;
    public static int PendantY { get; set; } = 146;
    public static int PendantW { get; set; } = 36;
    public static int PendantH { get; set; } = 36;

    public static int HelmetX { get; set; } = 132;
    public static int HelmetY { get; set; } = 74;
    public static int HelmetW { get; set; } = 64;
    public static int HelmetH { get; set; } = 64;

    public static int WingsX { get; set; } = 204;
    public static int WingsY { get; set; } = 74;
    public static int WingsW { get; set; } = 92;
    public static int WingsH { get; set; } = 64;

    public static int WeaponX { get; set; } = 24;
    public static int WeaponY { get; set; } = 146;
    public static int WeaponW { get; set; } = 64;
    public static int WeaponH { get; set; } = 100;

    public static int ArmorX { get; set; } = 132;
    public static int ArmorY { get; set; } = 146;
    public static int ArmorW { get; set; } = 64;
    public static int ArmorH { get; set; } = 100;

    public static int ShieldX { get; set; } = 232;
    public static int ShieldY { get; set; } = 146;
    public static int ShieldW { get; set; } = 64;
    public static int ShieldH { get; set; } = 100;

    public static int RingLeftX { get; set; } = 92;
    public static int RingLeftY { get; set; } = 250;
    public static int RingLeftW { get; set; } = 36;
    public static int RingLeftH { get; set; } = 36;

    public static int RingRightX { get; set; } = 196;
    public static int RingRightY { get; set; } = 250;
    public static int RingRightW { get; set; } = 36;
    public static int RingRightH { get; set; } = 36;

    public static int GlovesX { get; set; } = 24;
    public static int GlovesY { get; set; } = 250;
    public static int GlovesW { get; set; } = 64;
    public static int GlovesH { get; set; } = 64;

    public static int PantsX { get; set; } = 132;
    public static int PantsY { get; set; } = 250;
    public static int PantsW { get; set; } = 64;
    public static int PantsH { get; set; } = 64;

    public static int BootsX { get; set; } = 232;
    public static int BootsY { get; set; } = 250;
    public static int BootsW { get; set; } = 64;
    public static int BootsH { get; set; } = 64;

    // Artwork dimensions only. These do not change equip/drop hit areas or item previews.
    // Artwork stays centered on its slot. OZT dimensions include any transparent padding.
    public static int ArmorAssetW { get; set; } = 64;
    public static int ArmorAssetH { get; set; } = 100;
    public static int WeaponAssetW { get; set; } = 64;
    public static int WeaponAssetH { get; set; } = 100;
    public static int ShieldAssetW { get; set; } = 64;
    public static int ShieldAssetH { get; set; } = 100;
    public static int HelmetAssetW { get; set; } = 64;
    public static int HelmetAssetH { get; set; } = 64;
    public static int PantsAssetW { get; set; } = 64;
    public static int PantsAssetH { get; set; } = 64;
    public static int GlovesAssetW { get; set; } = 64;
    public static int GlovesAssetH { get; set; } = 64;
    public static int BootsAssetW { get; set; } = 64;
    public static int BootsAssetH { get; set; } = 64;
    public static int WingsAssetW { get; set; } = 92;
    public static int WingsAssetH { get; set; } = 64;
    public static int PetAssetW { get; set; } = 64;
    public static int PetAssetH { get; set; } = 64;
    public static int PendantAssetW { get; set; } = 36;
    public static int PendantAssetH { get; set; } = 36;
    public static int RingLeftAssetW { get; set; } = 36;
    public static int RingLeftAssetH { get; set; } = 36;
    public static int RingRightAssetW { get; set; } = 36;
    public static int RingRightAssetH { get; set; } = 36;

    public static (int Width, int Height) GetEquipmentAssetSize(byte slot) => slot switch
    {
        0 => (WeaponAssetW, WeaponAssetH),
        1 => (ShieldAssetW, ShieldAssetH),
        2 => (HelmetAssetW, HelmetAssetH),
        3 => (ArmorAssetW, ArmorAssetH),
        4 => (PantsAssetW, PantsAssetH),
        5 => (GlovesAssetW, GlovesAssetH),
        6 => (BootsAssetW, BootsAssetH),
        7 => (WingsAssetW, WingsAssetH),
        8 => (PetAssetW, PetAssetH),
        9 => (PendantAssetW, PendantAssetH),
        10 => (RingLeftAssetW, RingLeftAssetH),
        11 => (RingRightAssetW, RingRightAssetH),
        _ => (1, 1)
    };

    // The current Classic inventory has no reference side-button column, so the
    // eight-column grid is centered inside the window and under the equipment.
    // Keep the cell/column defaults before calculating GridX. Static field
    // initializers run top-to-bottom; calculating GridX before these values
    // would use their zero defaults and shift the whole grid to the right.
    public static int GridY { get; set; } = 326;
    public static int GridFrameX { get; set; } = 20;
    public static int GridFrameY { get; set; } = 322;
    public static int GridFrameW { get; set; } = 280;
    public static int GridFrameH { get; set; } = 280;
    public static int GridCellSize { get; set; } = 34;
    public static int GridCols { get; set; } = 8;
    public static int GridRows { get; set; } = 8;
    public static int GridX { get; set; } = (PanelW - GridCols * GridCellSize) / 2;

    public static int MoneyY { get; set; } = 608;
    // Reserve the bottom edge for the version label so it cannot cover a button.
    public static int FooterY { get; set; } = 652;
    public static int MoneyX { get; set; } = 24;
    public static int MoneyW { get; set; } = 272;
    public static int MoneyH { get; set; } = 45;
    public static int ZenIconX { get; set; } = 28;
    public static int ZenIconW { get; set; } = 40;
    public static int ZenTextX { get; set; } = 72;
    public static int ZenTextW { get; set; } = 220;
    public static int FooterCloseX { get; set; } = 25;
    public static int RepairX { get; set; } = 91;
    public static int StoreX { get; set; } = 157;
    public static int ExpansionX { get; set; } = 223;
    public static int FooterButtonW { get; set; } = 60;
    public static int FooterButtonH { get; set; } = 44;
}
