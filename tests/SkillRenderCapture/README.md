# Skill rendering evidence

Latest fixture runs advance the animation factor and elapsed effects at 100 simulated FPS (0.25 source ticks per update), so PNG readback cannot skip a brief effect. Total world time still comes from the host. Earlier captures use variable animation factors; do not mix their timing evidence. Lightning additionally captures source tick 2. Neither mode synchronizes native cast wind-up with fixture effect age.

This offline fixture runs the production TwinMU skill registry and renderer using supplied MuMain data. It is a capture tool, not a visual parity test.

When reference metadata includes equipment, the fixture resolves hand-slot names through the production ItemDatabase and prepares those weapon models with their recorded levels. Weapon-dependent effects must be reviewed with these equipped fixtures. Armor, wings, mounts, excellent/ancient flags and the target model are not reproduced yet, so this still does not establish identical full-frame rendering conditions.

Build the WindowsDX fixture:

```powershell
dotnet build tests/SkillRenderCapture/SkillRenderCapture.csproj -c Release -m:1 -p:NuGetAudit=false
```

Run its executable with the skill ID, original data folder, output directory, and optional MuMain `capture-state.json`. The reference metadata supplies map, caster class, caster tile and target tile. The target remains a naked player, not the original monster. Soul Barrier follows its production server-buff effect path. Other skills outside the registry require their actual alternate paths; a missing factory is not proof that the whole skill is absent.

The fixture pauses updates while loading effects and advances elapsed-time effects using the renderer's 25-Hz animation factor. Captures are taken after world and post-world drawing, from the active render target or backbuffer. Missing registry cases write `missing-renderer.txt`. Production MuGame exits the process directly with code zero, so the process exit code does not prove success. Inspect the failure artifact, images and object status metadata.

MuMain's companion tool is `tools/MuMainCapture`. It uses MuMain's documented native control socket. For manual casts, select and visually confirm the skill before `--click-target`; the tool does not verify the selected HUD skill. Native elapsed captures include casting wind-up. Do not align columns as though the effect clocks or equipped actors were identical.

`tools/skill_capture_sheets.py` composes saved screenshots into timeline sheets. Original images and metadata are preserved. Full class coverage and actual matching renders are recorded separately in `docs/skill-visual-live-checks.md`.

Run with --verify-aura-tail-ring to check production aura tail storage at capacities 16 and 20 for 100 updates. An optional second argument loads a previous Client.Main.dll to reproduce the regression without starting the renderer.

Set TWINMU_ITEM_CAPTURE_INVENTORY=1 and TWINMU_ITEM_CAPTURE_GENERAL=1 for 15 representative item categories. With TWINMU_ITEM_CAPTURE_AURA=1, capture through frame 400 also verifies padded placement and cached dragging, and saves six previews per item.

Set TWINMU_WING_CAPTURE to a group-12 wing ID alongside TWINMU_ITEM_CAPTURE_MATCH=1 and TWINMU_ITEM_CAPTURE_AURA=1 to capture the equipped wing. Enable TWINMU_ITEM_CAPTURE_INVENTORY=1 to capture its preview too. TWINMU_WING_CAPTURE_REMOVE=1 unequips it at frame350 for cleanup verification.
