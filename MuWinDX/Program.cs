using System.Windows.Forms;
using Client.Main;

// Prefer packaged assets, then the local client installation for development builds
// and locally published releases. Both configurations need the same login map.
var installedDataPath = @"C:\Games\MU_Red_1_20_61_Full\Data";
if (!System.IO.Directory.Exists(System.IO.Path.Combine(Constants.DataPath, "World95")) &&
    System.IO.Directory.Exists(System.IO.Path.Combine(installedDataPath, "World95")))
{
    Constants.DataPath = installedDataPath;
}

Application.SetHighDpiMode(HighDpiMode.SystemAware);
PerformanceRuntimeTuning.Apply();

using var game = new MuGame();
game.Run();
