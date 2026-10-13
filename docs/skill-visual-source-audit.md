# Skill visual source audit — 2026-10-13

This is a development inventory and source comparison, not a certificate of visual parity. Every decorated skill factory is accounted for below, with master replacement coverage checked against the current OpenMU Season 6 tree. Some findings are complete parameter comparisons; others identify the remaining port work. No in-game side-by-side capture was performed.

## References and scope

- Original visual reference: `C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source` (`ZzzCharacter.cpp`, `ZzzEffect.cpp`, `ZzzEffectJoint.cpp`, `ZzzEffectParticle.cpp`, `ZzzOpenData.cpp`).
- Cross-check for corrected poison/meteor/math behavior: `C:/MuOnline_Source/MuMain-main/src/source` (render effects and math).
- Current server skill IDs/master replacements: generated `Client.Main/Core/Utilities/ServerMasterSkills.cs` and `C:/OpenMU/OpenMU/src/Persistence/Initialization/VersionSeasonSix/SkillsInitializer.cs`. Old Main 5.2 master numbers cannot be copied directly onto this tree.
- Runtime factory inventory: 30 factory files, 33 factory classes, 69 explicit skill IDs after stale aliases were removed. Master replacements are resolved by the registry; Soul Barrier is driven by server buff state instead of a dedicated cast factory.
- Scope is player skill visuals and their buff controllers. Monster-specific effect variants, weapon/item enhancements, event buffs, and a complete audit of every buff enum value remain outside this pass.

## Corrections in this pass

Seven problem groups were corrected: shared inverse rotation; stale master aliases; Poison smoke/core; Combo invisible joints and fade; Meteorite trajectory/invented impact layers; Aqua Beam size/projection; Fire Scream left-stream yaw. These do not mean every affected skill now matches every reference parameter.

## Factory-by-factory findings

### AquaBeamSkillEffect — 12

Corrected scale 64 -> source 16 and replaced screen scaling with world billboards; twenty samples, 50-unit spacing, twenty-frame life. Terrain lighting remains an approximation.

Client: [AquaBeamSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/AquaBeamSkillEffect.cs:1>). Reference symbol: `BITMAP_BOSS_LASER`; [ZzzEffect.cpp:1116](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1116>).

### ArrowSkillEffect — 24,25,46,51,52,235 + master variants

Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.

Client: [ArrowSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs:1>). Reference symbol: `MODEL_ARROW`; [ZzzEffect.cpp:381](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:381>).

### ClassBuffSkillEffect — 64,217,218 + master variants

Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.

Client: [ClassBuffSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs:1>). Reference symbol: `MODEL_ALICE_BUFFSKILL_EFFECT`; [ZzzEffect.cpp:1012](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1012>).

### ComboSkillEffectFactory — 59

Corrected: previously the sixty child objects had no drawing implementation. Now actual BITMAP_LIGHT cross ribbons, original random angles/widths/speeds, four frames of ten flight samples, twenty-frame lifetime, /1.2 light fade, /1.4 model fade, fixed 25 Hz model growth. Burst stays at its original location.

Client: [ComboSkillEffectFactory.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ComboSkillEffectFactory.cs:1>). Reference symbol: `MODEL_COMBO`; [ZzzEffect.cpp:3385](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:3385>).

### DarkHorseEarthquakeSkillEffect — 62 + registry master replacements

Animation-based shockwave, ground-stone, and earthquake sequence exists. Removed five stale master aliases (515..519); they conflict with the current server tree. Remaining mount-animation alignment is unverified.

Client: [DarkHorseEarthquakeSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DarkHorseEarthquakeSkillEffect.cs:1>). Reference symbol: `MODEL_SKILL_FURY_STRIKE`; [ZzzEffect.cpp:2047](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2047>).

### DeathStabSkillEffect — 43

Mismatch remains: current three-phase energy/spike/lightning composition is not the source attack-counter sequence. Reference uses weapon bone transforms at attack frames 2..8 and repeated spear spawns at 6..12; the replacement lightning is also custom.

Client: [DeathStabSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DeathStabSkillEffect.cs:1>). Reference symbol: `MODEL_SPEAR`; [ZzzEffect.cpp:756](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:756>).

### DecaySkillEffect — 38

Two hidden subtype-6 falling emitters and source inferno impact exist. Corrected shared inverse rotation affecting its ribbons/smoke. Remaining particle sizing and settle/impact behavior need comparison; source likeness is not certified.

Client: [DecaySkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DecaySkillEffect.cs:1>). Reference symbol: `MODEL_FIRE`; [ZzzEffect.cpp:399](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:399>).

### ElfSupportSkillEffect — 26,27,28 + master variants

Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.

Client: [ElfSupportSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs:1>). Reference symbol: `BITMAP_MAGIC+1`; [ZzzEffect.cpp:1341](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1341>).

### EnergyBallSkillEffect — 17

Mismatch remains: source direction is -60 per frame, lifetime 20 and launch height +100; current speed is 2400/sec with custom glow/spark tails and impact. Requires source projectile/particle port, not only a texture change.

Client: [EnergyBallSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/EnergyBallSkillEffect.cs:1>). Reference symbol: `BITMAP_ENERGY`; [ZzzEffect.cpp:407](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:407>).

### EvilSpiritSkillEffect — 9

Mismatch remains: source casts 80- and 20-width spirit joints; current main width is 84 and has custom opacity, shrink, and terrain-darkening controls. Source negative-blend behavior still needs a dedicated port.

Client: [EvilSpiritSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/EvilSpiritSkillEffect.cs:1>). Reference symbol: `BITMAP_JOINT_SPIRIT`; [ZzzEffect.cpp:1](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1>).

### FireBallSkillEffect — 4

Mismatch remains: original MODEL_FIRE subtype 1 projectile is replaced by flare/firehik sprites, added sparks and custom speed. Requires original model, homing/target impact and BITMAP_FIRE subtype-5 emission.

Client: [FireBallSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireBallSkillEffect.cs:1>). Reference symbol: `MODEL_FIRE`; [ZzzEffect.cpp:399](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:399>).

### FireBurstSkillEffect — 61,508,514 + registry replacements

Three homing streams, stationary fire pieces, and DarkLordSkill cast pieces exist with 25 Hz simulation. Reference structure is present; full tail sampling, model light, and moving-target comparisons remain unverified.

Client: [FireBurstSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireBurstSkillEffect.cs:1>). Reference symbol: `MODEL_PIER_PART`; [ZzzEffect.cpp:3607](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:3607>).

### FireScreamSkillEffect — 78 + registry replacements

Corrected cumulative yaw: center 0, right +10, left -10 (previously -20). Source adds 10 then subtracts 20 from the same angle. Original paired models are present; the custom 0.55 body brightness and remaining particle settings are not certified matches.

Client: [FireScreamSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireScreamSkillEffect.cs:1>). Reference symbol: `MODEL_DARK_SCREAM`; [ZzzEffect.cpp:391](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:391>).

### FlameSkillEffect — 5

Source structure present: terrain bitmap plus Flame01 particles, area lifetime 40 frames, targeted lifetime 20, source subtype-specific emission counts. Lighting, terrain projection, and particle rendering still need in-game comparison.

Client: [FlameSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FlameSkillEffect.cs:1>). Reference symbol: `BITMAP_FLAME`; [ZzzEffect.cpp:1221](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1221>).

### GreaterFortitudeSkillEffect — 48,356,360,363

Previous correction retained: attack-counter gated 36 spirit joints, subtype-2 movement, flare tails and two terrain circles. Persistent orange upper-body particles are separate. Headless timing/physics checks pass; visual comparison still needed.

Client: [GreaterFortitudeSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/GreaterFortitudeSkillEffect.cs:1>). Reference symbol: `BITMAP_JOINT_SPIRIT`; [ZzzEffect.cpp:1](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1>).

### HellFireSkillEffect — 10

Source models and 45/40-frame lifetimes match selected parameters. Mismatch remains: current EdgeTrimDeformer modifies model UVs by a 5% margin; source uses the original UVs and its life-driven scroll. Needs a targeted rendering port.

Client: [HellFireSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/HellFireSkillEffect.cs:1>). Reference symbol: `MODEL_CIRCLE`; [ZzzEffect.cpp:2347](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2347>).

### IceSkillEffect — 7

Mismatch remains: core 48 frames/scale .88 versus source 50/.8. Source stops animation at frame 5, emits smoke, fades Alpha, and uses randomized shard gravity/lifetime. Current shards use custom drift and linear lighting fade.

Client: [IceSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/IceSkillEffect.cs:1>). Reference symbol: `MODEL_ICE`; [ZzzEffect.cpp:2401](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2401>).

### IceStormSkillEffect — 39 + registry replacements

Ten Blizzard falling models, source assets and 25 Hz steps exist. Removed stale master aliases 302..306, which are passive skills in the current server tree. Detailed shard/particle/render parity remains unverified.

Client: [IceStormSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/IceStormSkillEffect.cs:1>). Reference symbol: `MODEL_BLIZZARD`; [ZzzEffect.cpp:3324](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:3324>).

### InfernoSkillEffect — 14

Core model and fifteen-frame lifetime agree with selected source parameters. Current custom burst/spark composition requires comparison with the exact source inferno move branch; not approved as a full match.

Client: [InfernoSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/InfernoSkillEffect.cs:1>). Reference symbol: `MODEL_SKILL_INFERNO`; [ZzzEffect.cpp:1534](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1534>).

### LightningSkillEffect — 3

Mismatch remains: current effect intentionally adds branches, coronas, spark systems, glow and an impact flash. Reference uses thunder joints with subtype-specific movement and widths. Must replace the custom composition to achieve parity.

Client: [LightningSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/LightningSkillEffect.cs:1>). Reference symbol: `BITMAP_JOINT_THUNDER`; [ZzzEffect.cpp:1478](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1478>).

### MeleeAndFenrirSkillEffects — 44 / Crescent Moon Slash

Source model present, but fixed staged parts approximate the source sword-force movement and repeated emissions. Verify changing weapon/action bones and acceleration, not just model filename.

Client: [MeleeAndFenrirSkillEffects.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs:1>). Reference symbol: `MODEL_SWORD_FORCE`; [ZzzEffect.cpp:14767](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:14767>).

### MeleeAndFenrirSkillEffects — 47 / Impale

Mismatch remains: fixed Item/Spear01/02 and generic homing thunder pieces approximate weapon-specific MODEL_SPEAR/MODEL_SPEARSKILL branches and source frame/bone-driven behavior.

Client: [MeleeAndFenrirSkillEffects.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs:1>). Reference symbol: `MODEL_SPEAR`; [ZzzEffect.cpp:756](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:756>).

### MeleeAndFenrirSkillEffects — 232,337 / Strike of Destruction

Source crack models are present, but generic LevelUpMagicCircle and LightEffect pieces are substitutes. Requires original effect subtypes and exact target/caster timing.

Client: [MeleeAndFenrirSkillEffects.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs:1>). Reference symbol: `MODEL_BLOW_OF_DESTRUCTION`; [ZzzEffect.cpp:5024](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:5024>).

### MeleeAndFenrirSkillEffects — 76 / Plasma Storm

Original Fenrir joint family has a 3D render path and per-mount variants. Source wide/narrow paired joints exist. Target selection, subtype widths and attack-frame alignment still need live verification.

Client: [MeleeAndFenrirSkillEffects.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs:1>). Reference symbol: `MODEL_FENRIR_SKILL_THUNDER`; [ZzzEffect.cpp:1](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1>).

### MeteoriteSkillEffect — 2

Corrected spawn from +1200 to +400 Z, X offset 130..161, direction (0,0,-50) rotated by (0,20,0), model angle, removed three invented lingering fire columns and reduced four impact explosions to the original one at +80 Z. Trail particles, stone physics and explosion size/lifetime still differ; not a complete port.

Client: [MeteoriteSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeteoriteSkillEffect.cs:1>). Reference symbol: `MODEL_FIRE`; [ZzzEffect.cpp:399](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:399>).

### NovaSkillEffect — 40

Mismatch remains: source creates invisible MODEL_CIRCLE subtype 1, then 36 spirit joints subtype 6 and a subtype-7 set controlled by skill charge. Current expanding torus particles are a substitute.

Client: [NovaSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/NovaSkillEffect.cs:1>). Reference symbol: `MODEL_CIRCLE`; [ZzzEffect.cpp:2347](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2347>).

### NovaStartSkillEffect — 58

Mismatch remains: current custom inward particles/core are not an exact port of the original charge-state model/joint effects. Server stage updates are used; the renderer must be ported separately.

Client: [NovaStartSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/NovaStartSkillEffect.cs:1>). Reference symbol: `MODEL_CIRCLE_LIGHT`; [ZzzEffect.cpp:2375](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2375>).

### PoisonSkillEffect — 1

Corrected Spark03 -> smoke01, ten subtype-1 smoke billboards, fifty-frame smoke lifetime, source offsets/scale/velocity damping/colour/growth; core lifetime 40, scale 1, BlendMesh 1 and life*.1 glow. Terrain light/animation mapping remain engine adaptations.

Client: [PoisonSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PoisonSkillEffect.cs:1>). Reference symbol: `MODEL_POISON`; [ZzzEffect.cpp:3113](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:3113>).

### PowerWaveSkillEffect — 11

Magic02 model, twenty-frame life, -60/frame motion and source smoke branch exist. Corrected shared rotation for smoke. Lighting, source subtype/monster variations and model animation remain unverified.

Client: [PowerWaveSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PowerWaveSkillEffect.cs:1>). Reference symbol: `MODEL_MAGIC2`; [ZzzEffect.cpp:2185](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2185>).

### RagefulBlowSkillEffect — 42

Original fury/earthquake models and companion effects exist. Detailed source sequence is implemented, but animation-frame alignment, model rendering and particle texture/size still need live comparison.

Client: [RagefulBlowSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/RagefulBlowSkillEffect.cs:1>). Reference symbol: `MODEL_SKILL_FURY_STRIKE`; [ZzzEffect.cpp:2047](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2047>).

### TwisterSkillEffect — 8

Mismatch remains: source -10/frame (250/sec), source smoke01 subtype 3 every frame and random left/right thunder joints; current speed 240/sec, smoke02 at .035 sec and custom bolts/spin. Current client damage logic must be preserved when porting visuals.

Client: [TwisterSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/TwisterSkillEffect.cs:1>). Reference symbol: `MODEL_STORM`; [ZzzEffect.cpp:2194](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2194>).

### TwistingSlashSkillEffect — 41

Mismatch remains: source wheel1 (5 frames) emits wheel2 weapon-shadow effects (25 frames); current rotating flare arcs, Shiny05, sparks and custom shadow settings are an artistic substitute.

Client: [TwistingSlashSkillEffect.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/TwistingSlashSkillEffect.cs:1>). Reference symbol: `MODEL_SKILL_WHEEL1`; [ZzzEffect.cpp:2039](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:2039>).

### WeaponSkillFlashFactory — 19..23

Source SHINY+2 texture Shiny03, bone-10 offset and sine pulse are present. Current SpriteObject projection still needs comparison against source world-size sprites; weapon trail rendering is outside this flash factory.

Client: [WeaponSkillFlashFactory.cs:1](<C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs:1>). Reference symbol: `BITMAP_SHINY`; [ZzzEffect.cpp:1](<C:/MuOnline_Source/Main5.2-main/Source Main 5.2/source/ZzzEffect.cpp:1>).

## Missing or separate families

No factory was found for these master replacement families: Expansion of Wizardry (233); Blast (13); Infinity Arrow (77); Sleep (219); Chain Lightning (215); Lightning Shock (230); Drain Life (214); Power Slash (56); Blood Attack (55); Force Wave (66); Rage Fighter Killing Blow (260), Beast Uppercut (261), Chain Drive (262), Dark Side (263), Dragon Roar (264), Defense Rate buff (268), Stamina buff (267). Soul Barrier (16) also has no factory but IS handled by ClassBuffController, so it must not be counted as a missing persistent aura.

Expansion of Wizardry is a confirmed missing persistent visual: reference `ZzzCharacter.cpp` around 10550 creates/deletes MODEL_SWELL_OF_MAGICPOWER_BUFF_EFF according to eBuff_SwellOfMagicPower; the current buff controllers do not cover it. Rage Fighter buff effects have reference implementations in MonkSystem.cpp and ZzzEffect.cpp that also need ports. The existence of a skill animation or icon does not supply these world effects.

Other enum/status issues remain: BuffEffectId.HealOverTime is currently 6, while the original status 6 is Infinity Arrow. Do not interpret that status as healing or invent a healing aura. The legacy BuffEffectController contains additional approximate visuals but is not instantiated by PacketRouter; its presence does not prove that those effects run in game.

## Timing and cleanup

Buff status lifetime and particle lifetime are separate. The server decides when a buff ends; original visual joints/particles are renewed while the buff is active. Increasing Energy can affect the SERVER duration formula for particular skills; it does not make each visual particle live for that duration. Prior countdown/unknown-duration handling remains unchanged by this pass.

The existing buff controllers reconcile late scope entry, player replacement, death and world changes. New Combo ribbon children belong to the Combo root and are disposed with it. Poison retains bounded particles and removes its dynamic light. Meteorite still owns its separately spawned trail/stones, whose remaining source differences are listed above.

## Validation

- SkillVisualReference: 266 independent axis-rotation and actual master-factory routing checks passed.
- BuffState: source IDs, recipient masking, authoritative lifetime, refresh, independent removal and scope cleanup passed.
- FortitudeReference: attack-frame thresholds, source spirit travel and late fade passed.
- WindowsDX and DesktopGL builds passed with zero errors. Existing compiler warnings remain.
- Updated desktop executable: `C:/OpenMU/MuOnlineS6/buff-reference-build/TwinMU.exe`.
- In-game validation remains required. Test each corrected cast on self/another player, overlap buffs, refresh before expiry, let expiry/removal occur, move targets, enter/leave scope and change maps. Compare at the same camera, equipment and source-client settings.

## Remaining work order

1. Missing persistent class buffs (Expansion of Wizardry and Rage Fighter buffs), plus exact registration/status definitions.
2. Replace Fire Ball, Lightning, Energy Ball, Twisting Slash and both Nova renderers with original effect primitives.
3. Port Ice/Twister and complete Meteorite particle/stone behavior.
4. Replace staged Impale/Death Stab/Strike of Destruction substitutes; verify weapon-specific branches.
5. Remove remaining custom UV, brightness, opacity and trail adjustments after reference comparison.
6. Capture side-by-side in-game results for every family before declaring visual parity.
