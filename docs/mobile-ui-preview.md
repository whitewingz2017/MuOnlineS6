# Offline UI previews

From `C:\OpenMU\MuOnlineS6`, double-click:

- `Preview-Inventory.bat` for inventory and merchant layout, using your editable game Data files.
- `Preview-Mobile-Updates.bat` for the mobile update icon and release-notes panel, using the inventory artwork bundled with Android.

Each launcher rebuilds the latest UI, runs a short offline scene with sample items,
then opens the screenshot. No account login or server connection is used.
The update preview uses a sample offer and never downloads or installs an APK.

Edit `Client.Main/Controls/UI/Game/Hud/Classic/InventoryLayout.cs`, then rerun the launcher
to see new slot, Zen and button positions. Android's bundled inventory artwork is in
`Client.Main/Data/InventoryUi`; Windows normally reads inventory artwork from the game Data folder.

Screenshots and build/preview logs are saved in a dated folder under `Publish/InventoryPreview`.
The same .NET SDK and compiled Windows Content used by the normal client build are required.

Optional PowerShell example for a phone-resolution preview:

```powershell
.\Preview-Inventory.ps1 -Updates -Width 2880 -Height 1800
```
