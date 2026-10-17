# NPC and shadow visual regression

Run from the repository root after building MuWinGL:

    dotnet run --project tests/NpcShadowCapture -c Release -- C:/OpenMU/AI-Logs/npc-shadows.png

The fixture loads Noria without connecting to the server, adds both updated NPCs,
uses mobile Medium terrain/shadow settings, captures the scene and shadow texture,
and fails if the shadow texture contains no geometry. It requires local game Data.
Set TWINMU_DATA to override C:/OpenMU/Data. The fixture references the built desktop
head; it does not restore or rebuild the production client project.

For DirectX, pass -p:CaptureRuntime=../../MuWinDX/bin/Debug/net10.0-windows/win-x64
and a separate OutputPath. Inspect the scene capture for glow size and receiver
shadows; the pixel assertion alone does not prove their appearance matches MuMain.

Set TWINMU_CAPTURE_INVASION=1 to add a golden flyover and invasion fireball.
This holds their positions for inspecting facing, gold coating, mouth flame and
fire trails; live movement and spawn cadence must be checked separately.

Set TWINMU_CAPTURE_MERCHANT=1 to inspect the inventory-style merchant panel at
a compact virtual height. It populates potion, armor, staff, and scroll previews,
scrolls half a row, and checks clipped hit testing and drag-versus-purchase taps.
Add TWINMU_CAPTURE_REPAIR=1 to check that the compact grid stays above repair buttons.

