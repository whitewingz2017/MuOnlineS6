# MuMain skill visual comparison inventory

Reference: C:/MuOnline_Source/MuMain-main/src/source. This report inventories every skill created by the current Season 6 configuration, including monster, event and passive entries; those are not all player hotbar skills.

The comparison distinguishes an existing renderer, a missing registry route, a known implementation difference and an unverified rendered result. No skill is certified pixel-equivalent. Prior detailed findings from the Main 5.2 audit are retained as historical findings, not silently relabelled as newly verified MuMain parity. Direct MuMain cast excerpts are in skill-visual-mumain-inventory.json.

## Checked differences directly confirmed against MuMain

- Fire Ball: MuMain ZzzCharacter creates MODEL_FIRE subtype 1. Current renderer replaces it with flare/firehik sprites and sparks.
- Twisting Slash: MuMain creates MODEL_SKILL_WHEEL1 and subsequent weapon-shadow wheel effects. Current renderer adds rotating flare arcs, Shiny05 and sparks.
- Nova: MuMain creates MODEL_CIRCLE subtype 1. Current release and charge effects use custom particle compositions.
- Cometfall: the new factory creates two Blast01 models with energy trails and impact parts from MuMain source. Rendering and full parity must be checked separately; registration is not certification.
- Force/Force Wave: MuMain creates two MODEL_WAVES and one MODEL_PIERCING2. No registry factory exists.
- Evil Spirit: see its entry; matching selected motion parameters does not fix the ghost rendering.

## Every configured skill

### 1 — Poison

Base route: 1. registered visual; parity not certified.
Client factory: [PoisonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PoisonSkillEffect.cs).
Live MuMain and TwinMU timelines inspected. Core family is present, but smoke and brightness differ. Terrain light now uses MuMain RGB(.3,1,.6), radius two tiles and core remaining lifetime*.1; the custom pulse and longer smoke-based lighting lifetime were removed. This source correction was rendered again; exact brightness and smoke parity remain unverified.
MuMain symbols: `AT_SKILL_POISON`.
MuMain source: [WSclient.cpp:3929](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3929), [WSclient.cpp:4334](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4334), [SkillCast.cpp:503](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:503), [ZzzCharacter.cpp:4992](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4992).

### 2 — Meteorite

Base route: 2. registered visual; parity not certified.
Client factory: [MeteoriteSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeteoriteSkillEffect.cs).
Corrected spawn from +1200 to +400 Z, X offset 130..161, direction (0,0,-50) rotated by (0,20,0), model angle, removed three invented lingering fire columns and reduced four impact explosions to the original one at +80 Z. Trail particles, stone physics and explosion size/lifetime still differ; not a complete port.
MuMain symbols: `AT_SKILL_METEO`.
MuMain source: [WSclient.cpp:4328](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4328), [SkillCast.cpp:505](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:505), [ZzzCharacter.cpp:5007](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5007).

### 3 — Lightning

Base route: 3. registered visual; parity not certified.
Client factory: [LightningSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/LightningSkillEffect.cs).
MuMain player Lightning emits two BITMAP_JOINT_THUNDER subtype-0 ribbons at scales 50 and 10 plus BITMAP_ENERGY. The factory now uses the two source ribbons instead of the custom branching screen-space bolt. First-frame renders, cast cadence, energy/impact particles and terrain light still require validation; this is not a certified match.
MuMain symbols: `AT_SKILL_LIGHTNING`.
MuMain source: [WSclient.cpp:4319](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4319), [SkillCast.cpp:506](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:506), [ZzzCharacter.cpp:2253](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2253).

### 4 — FireBall

Base route: 4. registered visual; parity not certified.
Client factory: [FireBallSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireBallSkillEffect.cs).
Mismatch remains: original MODEL_FIRE subtype 1 projectile is replaced by flare/firehik sprites, added sparks and custom speed. Requires original model, homing/target impact and BITMAP_FIRE subtype-5 emission.
MuMain symbols: `AT_SKILL_FIREBALL`.
MuMain source: [WSclient.cpp:4327](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4327), [SkillCast.cpp:514](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:514), [ZzzCharacter.cpp:5031](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5031).

### 5 — Flame

Base route: 5. registered visual; parity not certified.
Client factory: [FlameSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FlameSkillEffect.cs).
Source structure present: terrain bitmap plus Flame01 particles, area lifetime 40 frames, targeted lifetime 20, source subtype-specific emission counts. Lighting, terrain projection, and particle rendering still need in-game comparison.
MuMain symbols: `AT_SKILL_FLAME`.
MuMain source: [WSclient.cpp:4336](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4336), [ClassAttack.cpp:1378](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1378), [ZzzCharacter.cpp:4479](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4479), [ZzzCharacter.cpp:4485](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4485), [ZzzCharacter.cpp:5035](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5035).

### 6 — Teleport

Base route: 6. no registry visual; alternate path must be checked.
Teleport is driven by movement/visibility packet handling. A dedicated cast factory is absent; original disappear/appear visuals still need comparison.
MuMain symbols: `AT_SKILL_TELEPORT`.
MuMain source: [WSclient.cpp:4212](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4212), [WSclient.cpp:4362](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4362), [ClassAttack.cpp:1480](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1480), [ClassAttack.cpp:1493](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1493).

### 7 — Ice

Base route: 7. registered visual; parity not certified.
Client factory: [IceSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/IceSkillEffect.cs).
Core lifetime 50 frames and scale .8 corrected from MuMain. Animation, smoke, alpha fade and randomized shard physics still need comparison; current shards retain custom drift and lighting fade. No paired rendering match has been verified.
MuMain symbols: `AT_SKILL_ICE`.
MuMain source: [WSclient.cpp:3933](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3933), [WSclient.cpp:4329](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4329), [SkillCast.cpp:511](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:511), [ZzzCharacter.cpp:4955](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4955).

### 8 — Twister

Base route: 8. registered visual; parity not certified.
Client factory: [TwisterSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/TwisterSkillEffect.cs).
Mismatch remains: source -10/frame (250/sec), source smoke01 subtype 3 every frame and random left/right thunder joints; current speed 240/sec, smoke02 at .035 sec and custom bolts/spin. Current client damage logic must be preserved when porting visuals.
MuMain symbols: `AT_SKILL_STORM`.
MuMain source: [ClassAttack.cpp:1354](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1354), [ZzzCharacter.cpp:4494](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4494), [ZzzCharacter.cpp:4495](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4495).

### 9 — EvilSpirit

Base route: 9. registered visual; parity not certified.
Client factory: [EvilSpiritSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/EvilSpiritSkillEffect.cs).
Four paired spirits match the MuMain cast structure. Motion constants, subtractive blend factors, single scale-1.3 static head, life*.1 brightness, joint expiry, terrain darkness .04..07, floor-based FPS-scaled tail capacity up to 200, tail UVs and pitch signs corrected against MuMain source. Paired live reference and TwinMU capture timelines exist in Publish/SkillComparison/Sheets/skill-9-timeline.png. The corrected heads and tails remain above terrain. Trail shape, brightness, persistent heads versus one-frame MODEL_LASER emissions, lighting falloff and random/timing behavior remain different or unverified. This is not a certified rendering match.
MuMain symbols: `AT_SKILL_EVIL_SPIRIT`.
MuMain source: [ClassAttack.cpp:1355](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1355), [ZzzCharacter.cpp:4584](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4584).

### 10 — Hellfire

Base route: 10. registered visual; parity not certified.
Client factory: [HellFireSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/HellFireSkillEffect.cs).
Source models and 45/40-frame lifetimes match selected parameters. Mismatch remains: current EdgeTrimDeformer modifies model UVs by a 5% margin; source uses the original UVs and its life-driven scroll. Needs a targeted rendering port.
MuMain symbols: `AT_SKILL_HELL_FIRE`.
MuMain source: [WSclient.cpp:5250](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5250), [ClassAttack.cpp:1200](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1200), [ZzzCharacter.cpp:4423](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4423).

### 11 — PowerWave

Base route: 11. registered visual; parity not certified.
Client factory: [PowerWaveSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PowerWaveSkillEffect.cs).
Magic02 model, twenty-frame life, -60/frame motion and source smoke branch exist. Corrected shared rotation for smoke. Lighting, source subtype/monster variations and model animation remain unverified.
MuMain symbols: `AT_SKILL_POWERWAVE`.
MuMain source: [WSclient.cpp:4333](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4333), [SkillCast.cpp:510](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:510), [ZzzCharacter.cpp:5044](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5044).

### 12 — AquaBeam

Base route: 12. registered visual; parity not certified.
Client factory: [AquaBeamSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/AquaBeamSkillEffect.cs).
Corrected scale 64 -> source 16 and replaced screen scaling with world billboards; twenty samples, 50-unit spacing, twenty-frame life. Terrain lighting remains an approximation.
MuMain symbols: `AT_SKILL_FLASH`.
MuMain source: [WSclient.cpp:5246](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5246), [ClassAttack.cpp:1363](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1363), [ZzzCharacter.cpp:4550](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4550), [ZzzCharacter.cpp:4552](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4552).

### 13 — Cometfall

Base route: 13. registered visual; parity not certified.
Client factory: [CometfallSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/CometfallSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_BLAST`.
MuMain source: [SkillCast.cpp:526](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:526), [ZzzCharacter.cpp:4556](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4556).

### 14 — Inferno

Base route: 14. registered visual; parity not certified.
Client factory: [InfernoSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/InfernoSkillEffect.cs).
Core model and fifteen-frame lifetime agree with selected source parameters. Current custom burst/spark composition requires comparison with the exact source inferno move branch; not approved as a full match.
MuMain symbols: `AT_SKILL_INFERNO`.
MuMain source: [WSclient.cpp:5255](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5255), [ClassAttack.cpp:1210](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1210), [ZzzCharacter.cpp:4573](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4573).

### 15 — TeleportAlly

Base route: 15. no registry visual; alternate path must be checked.
Teleport is driven by movement/visibility packet handling. A dedicated cast factory is absent; original disappear/appear visuals still need comparison.
MuMain symbols: `AT_SKILL_TELEPORT_ALLY`.
MuMain source: [WSclient.cpp:4212](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4212), [WSclient.cpp:4355](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4355), [ClassAttack.cpp:1405](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1405).

### 16 — SoulBarrier

Base route: 16. no registry visual; alternate path must be checked.
Soul Barrier has a separate persistent ClassBuffController path; absence of a factory does not mean absence of its aura.
MuMain symbols: `AT_SKILL_SOUL_BARRIER`.
MuMain source: [WSclient.cpp:3986](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3986), [WSclient.cpp:4309](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4309), [ClassAttack.cpp:1151](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1151), [ZzzCharacter.cpp:4972](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4972), [ZzzCharacter.cpp:5268](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5268).

### 17 — EnergyBall

Base route: 17. registered visual; parity not certified.
Client factory: [EnergyBallSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/EnergyBallSkillEffect.cs).
Mismatch remains: source direction is -60 per frame, lifetime 20 and launch height +100; current speed is 2400/sec with custom glow/spark tails and impact. Requires source projectile/particle port, not only a texture change.
MuMain symbols: `AT_SKILL_ENERGYBALL`.
MuMain source: [WSclient.cpp:4332](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4332), [SkillCast.cpp:509](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:509), [ZzzCharacter.cpp:2025](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2025), [ZzzCharacter.cpp:5087](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5087).

### 18 — Defense

Base route: 18. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_BLOCKING`.
MuMain source: [WSclient.cpp:3653](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3653), [ClassAttack.cpp:1276](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1276), [SkillCast.cpp:225](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:225).

### 19 — FallingSlash

Base route: 19. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_FALLING_SLASH`.
MuMain source: [WSclient.cpp:4413](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4413), [WSclient.cpp:4415](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4415), [WSclient.cpp:4422](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4422), [WSclient.cpp:4428](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4428), [WSclient.cpp:4436](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4436).

### 20 — Lunge

Base route: 20. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_LUNGE`.
MuMain source: [WSclient.cpp:4420](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4420), [WSclient.cpp:4422](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4422), [ClassAttack.cpp:1279](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1279), [SkillCast.cpp:236](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:236).

### 21 — Uppercut

Base route: 21. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_UPPERCUT`.
MuMain source: [WSclient.cpp:4427](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4427), [WSclient.cpp:4428](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4428), [ClassAttack.cpp:1281](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1281), [SkillCast.cpp:238](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:238).

### 22 — Cyclone

Base route: 22. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_CYCLONE`.
MuMain source: [WSclient.cpp:4433](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4433), [WSclient.cpp:4436](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4436), [ClassAttack.cpp:1282](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1282), [SkillCast.cpp:239](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:239).

### 23 — Slash

Base route: 23. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_SLASH`.
MuMain source: [WSclient.cpp:4441](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4441), [WSclient.cpp:4445](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4445), [ClassAttack.cpp:1285](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1285), [SkillCast.cpp:88](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:88), [SkillCast.cpp:242](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:242).

### 24 — TripleShot

Base route: 24. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_TRIPLE_SHOT`.
MuMain source: [WSclient.cpp:1537](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:1537), [WSclient.cpp:4479](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4479), [WSclient.cpp:5227](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5227), [SkillCast.cpp:1152](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1152), [SkillCast.cpp:1187](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1187).

### 26 — Heal

Base route: 26. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_HEALING`.
MuMain source: [WSclient.cpp:4287](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4287), [ClassAttack.cpp:155](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:155), [ClassAttack.cpp:211](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:211), [ClassAttack.cpp:250](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:250), [SkillCast.cpp:559](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:559).

### 27 — GreaterDefense

Base route: 27. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_DEFENSE`.
MuMain source: [WSclient.cpp:3947](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3947), [WSclient.cpp:4183](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4183), [WSclient.cpp:4292](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4292), [ClassAttack.cpp:160](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:160), [ClassAttack.cpp:216](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:216).

### 28 — GreaterDamage

Base route: 28. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_ATTACK`.
MuMain source: [WSclient.cpp:3942](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3942), [WSclient.cpp:4183](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4183), [WSclient.cpp:4289](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4289), [ClassAttack.cpp:157](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:157), [ClassAttack.cpp:213](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:213).

### 30 — SummonGoblin

Base route: 30. registered visual; parity not certified.
Client factory: [ElfSummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_SUMMON`.
MuMain source: [WSclient.cpp:4295](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4295), [WSclient.cpp:4296](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4296), [WSclient.cpp:4297](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4297), [WSclient.cpp:4298](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4298), [WSclient.cpp:4299](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4299).

### 31 — SummonStoneGolem

Base route: 31. registered visual; parity not certified.
Client factory: [ElfSummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 32 — SummonAssassin

Base route: 32. registered visual; parity not certified.
Client factory: [ElfSummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 33 — SummonEliteYeti

Base route: 33. registered visual; parity not certified.
Client factory: [ElfSummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 34 — SummonDarkKnight

Base route: 34. registered visual; parity not certified.
Client factory: [ElfSummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 35 — SummonBali

Base route: 35. registered visual; parity not certified.
Client factory: [ElfSummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 36 — SummonSoldier

Base route: 36. registered visual; parity not certified.
Client factory: [ElfSummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 38 — Decay

Base route: 38. registered visual; parity not certified.
Client factory: [DecaySkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DecaySkillEffect.cs).
Two hidden subtype-6 falling emitters and source inferno impact exist. Corrected shared inverse rotation affecting its ribbons/smoke. Remaining particle sizing and settle/impact behavior need comparison; source likeness is not certified.
MuMain symbols: `AT_SKILL_DECAY`.
MuMain source: [WSclient.cpp:3991](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3991), [WSclient.cpp:5235](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5235), [ClassAttack.cpp:1384](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1384), [ZzzCharacter.cpp:4440](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4440).

### 39 — IceStorm

Base route: 39. registered visual; parity not certified.
Client factory: [IceStormSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/IceStormSkillEffect.cs).
Ten Blizzard falling models, source assets and 25 Hz steps exist. Removed stale master aliases 302..306, which are passive skills in the current server tree. Detailed shard/particle/render parity remains unverified.
MuMain symbols: `AT_SKILL_ICE_STORM`.
MuMain source: [WSclient.cpp:3995](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3995), [WSclient.cpp:5237](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5237), [ClassAttack.cpp:1386](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1386), [ZzzCharacter.cpp:4456](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4456).

### 40 — Nova

Base route: 40. registered visual; parity not certified.
Client factory: [NovaSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/NovaSkillEffect.cs).
Mismatch remains: source creates invisible MODEL_CIRCLE subtype 1, then 36 spirit joints subtype 6 and a subtype-7 set controlled by skill charge. Current expanding torus particles are a substitute.
MuMain symbols: `AT_SKILL_NOVA`.
MuMain source: [WSclient.cpp:4881](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4881), [WSclient.cpp:5728](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5728), [WSclient.cpp:5999](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5999), [WSclient.cpp:6011](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:6011), [ClassAttack.cpp:1077](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1077).

### 41 — TwistingSlash

Base route: 41. registered visual; parity not certified.
Client factory: [TwistingSlashSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/TwistingSlashSkillEffect.cs).
Mismatch remains: source wheel1 (5 frames) emits wheel2 weapon-shadow effects (25 frames); current rotating flare arcs, Shiny05, sparks and custom shadow settings are an artistic substitute.
MuMain symbols: `AT_SKILL_TWISTING_SLASH`.
MuMain source: [WSclient.cpp:5260](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5260), [ClassAttack.cpp:506](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:506), [ClassAttack.cpp:602](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:602), [SkillCast.cpp:130](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:130), [SkillCast.cpp:307](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:307).

### 42 — RagefulBlow

Base route: 42. registered visual; parity not certified.
Client factory: [RagefulBlowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/RagefulBlowSkillEffect.cs).
Original fury/earthquake models and companion effects exist. Detailed source sequence is implemented, but animation-frame alignment, model rendering and particle texture/size still need live comparison.
MuMain symbols: `AT_SKILL_RAGEFUL_BLOW`.
MuMain source: [WSclient.cpp:5316](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5316), [ClassAttack.cpp:965](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:965), [ZzzCharacter.cpp:4159](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4159).

### 43 — DeathStab

Base route: 43. registered visual; parity not certified.
Client factory: [DeathStabSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DeathStabSkillEffect.cs).
Mismatch remains: current three-phase energy/spike/lightning composition is not the source attack-counter sequence. Reference uses weapon bone transforms at attack frames 2..8 and repeated spear spawns at 6..12; the replacement lightning is also custom.
MuMain symbols: `AT_SKILL_DEATHSTAB`.
MuMain source: [WSclient.cpp:4470](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4470), [WSclient.cpp:5358](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5358), [SkillCast.cpp:134](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:134), [SkillCast.cpp:157](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:157), [SkillCast.cpp:303](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:303).

### 44 — CrescentMoonSlash

Base route: 44. registered visual; parity not certified.
Client factory: [CrescentMoonSlashSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_RUSH`.
MuMain source: [WSclient.cpp:4545](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4545), [SkillCast.cpp:143](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:143), [SkillCast.cpp:334](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:334), [SkillCast.cpp:423](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:423), [ZzzCharacter.cpp:2934](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2934).

### 45 — Lance

Base route: 45. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_JAVELIN`.
MuMain source: [WSclient.cpp:4551](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4551), [SkillCast.cpp:515](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:515), [ZzzCharacter.cpp:5011](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5011).

### 46 — Starfall

Base route: 46. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_DEEPIMPACT`.
MuMain source: [WSclient.cpp:4570](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4570), [ClassAttack.cpp:264](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:264), [SkillCast.cpp:588](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:588), [ZzzCharacter.cpp:4906](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4906).

### 47 — Impale

Base route: 47. registered visual; parity not certified.
Client factory: [ImpaleSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_IMPALE`.
MuMain source: [WSclient.cpp:4463](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4463), [WSclient.cpp:5326](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5326), [ClassAttack.cpp:1287](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1287), [SkillCast.cpp:136](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:136), [SkillCast.cpp:150](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:150).

### 48 — SwellLife

Base route: 48. registered visual; parity not certified.
Client factory: [GreaterFortitudeSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/GreaterFortitudeSkillEffect.cs).
Previous correction retained: attack-counter gated 36 spirit joints, subtype-2 movement, flare tails and two terrain circles. Persistent orange upper-body particles are separate. Headless timing/physics checks pass; visual comparison still needed.
MuMain symbols: `AT_SKILL_SWELL_LIFE`.
MuMain source: [WSclient.cpp:3973](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3973), [WSclient.cpp:4843](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4843), [ClassAttack.cpp:488](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:488), [ClassAttack.cpp:958](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:958), [ZzzCharacter.cpp:2851](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2851).

### 49 — FireBreath

Base route: 49. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_RIDER`.
MuMain source: [WSclient.cpp:4859](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4859), [ClassAttack.cpp:546](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:546), [SkillCast.cpp:127](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:127), [SkillCast.cpp:244](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:244), [SkillCast.cpp:315](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:315).

### 50 — FlameofEvil

Base route: 50. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_BOSS`.
MuMain source: [WSclient.cpp:4868](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4868), [ZzzCharacter.cpp:1660](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:1660), [ZzzCharacter.cpp:1713](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:1713), [ZzzCharacter.cpp:1788](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:1788), [ZzzCharacter.cpp:1820](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:1820).

### 51 — IceArrow

Base route: 51. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_ICE_ARROW`.
MuMain source: [ZzzEffect.cpp:5582](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5582), [ZzzEffect.cpp:5673](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5673), [WSclient.cpp:3978](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3978), [WSclient.cpp:4522](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4522), [ClassAttack.cpp:264](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:264).

### 52 — Penetration

Base route: 52. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_PENETRATION`.
MuMain source: [ZzzEffect.cpp:5570](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5570), [ZzzEffect.cpp:5613](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5613), [ZzzEffect.cpp:5623](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5623), [ZzzEffect.cpp:5649](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5649), [WSclient.cpp:4516](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4516).

### 55 — FireSlash

Base route: 55. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_FIRE_SLASH`.
MuMain source: [WSclient.cpp:3982](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3982), [WSclient.cpp:4497](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4497), [WSclient.cpp:5332](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5332), [ClassAttack.cpp:628](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:628), [SkillCast.cpp:128](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:128).

### 56 — PowerSlash

Base route: 56. registered visual; parity not certified.
Client factory: [PowerSlashSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PowerSlashSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_POWER_SLASH`.
MuMain source: [WSclient.cpp:1537](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:1537), [WSclient.cpp:4456](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4456), [WSclient.cpp:5354](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5354), [ClassAttack.cpp:658](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:658), [ZzzCharacter.cpp:2819](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2819).

### 57 — SpiralSlash

Base route: 57. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_SPIRAL_SLASH`.
MuMain source: [WSclient.cpp:4611](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4611), [SkillCast.cpp:144](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:144), [SkillCast.cpp:164](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:164), [SkillCast.cpp:422](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:422), [ZzzCharacter.cpp:2960](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2960).

### 58 — NovaStart

Base route: 58. registered visual; parity not certified.
Client factory: [NovaStartSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/NovaStartSkillEffect.cs).
Mismatch remains: current custom inward particles/core are not an exact port of the original charge-state model/joint effects. Server stage updates are used; the renderer must be ported separately.
MuMain symbols: `AT_SKILL_NOVA_BEGIN`.
MuMain source: [WSclient.cpp:4876](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4876), [WSclient.cpp:5725](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5725), [ClassAttack.cpp:1077](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1077), [ClassAttack.cpp:1110](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1110), [ClassAttack.cpp:1129](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1129).

### 60 — Force

Base route: 60. registered visual; parity not certified.
Client factory: [ForceSkillVisualEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ForceSkillVisualEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_FORCE`.
MuMain source: [WSclient.cpp:4376](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4376), [SkillCast.cpp:137](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:137), [SkillCast.cpp:326](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:326), [SkillCast.cpp:358](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:358), [SkillCast.cpp:419](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:419).

### 61 — FireBurst

Base route: 61. registered visual; parity not certified.
Client factory: [FireBurstSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireBurstSkillEffect.cs).
Three homing streams, stationary fire pieces, and DarkLordSkill cast pieces exist with 25 Hz simulation. Reference structure is present; full tail sampling, model light, and moving-target comparisons remain unverified.
MuMain symbols: `AT_SKILL_FIREBURST`.
MuMain source: [WSclient.cpp:4394](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4394), [SkillCast.cpp:140](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:140), [SkillCast.cpp:329](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:329), [SkillCast.cpp:361](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:361), [SkillCast.cpp:456](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:456).

### 62 — Earthshake

Base route: 62. registered visual; parity not certified.
Client factory: [DarkHorseEarthquakeSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DarkHorseEarthquakeSkillEffect.cs).
Animation-based shockwave, ground-stone, and earthquake sequence exists. Removed five stale master aliases (515..519); they conflict with the current server tree. Remaining mount-animation alignment is unverified.
MuMain symbols: `AT_SKILL_EARTHSHAKE`.
MuMain source: [WSclient.cpp:5310](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5310), [ClassAttack.cpp:497](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:497), [ClassAttack.cpp:534](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:534), [ClassAttack.cpp:543](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:543), [ClassAttack.cpp:556](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:556).

### 63 — Summon

Base route: 63. registered visual; parity not certified.
Client factory: [PartySummonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PartySummonSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_PARTY_TELEPORT`.
MuMain source: [WSclient.cpp:4823](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4823), [ClassAttack.cpp:495](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:495), [ClassAttack.cpp:514](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:514), [ClassAttack.cpp:524](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:524), [ClassAttack.cpp:529](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:529).

### 64 — IncreaseCriticalDamage

Base route: 64. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ADD_CRITICAL`.
MuMain source: [WSclient.cpp:3967](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3967), [WSclient.cpp:4804](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4804), [ClassAttack.cpp:491](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:491), [ClassAttack.cpp:853](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:853), [ZzzCharacter.cpp:4366](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4366).

### 65 — ElectricSpike

Base route: 65. registered visual; parity not certified.
Client factory: [ElectricSparkSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElectricSparkSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_THUNDER_STRIKE`.
MuMain source: [WSclient.cpp:5304](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5304), [ClassAttack.cpp:496](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:496), [ClassAttack.cpp:913](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:913), [ClassAttack.cpp:931](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:931), [ZzzCharacter.cpp:2884](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2884).

### 66 — ForceWave

Base route: 66. registered visual; parity not certified.
Client factory: [ForceSkillVisualEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ForceSkillVisualEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_FORCE_WAVE`.
MuMain source: [WSclient.cpp:4377](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4377), [SkillCast.cpp:138](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:138), [SkillCast.cpp:327](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:327), [SkillCast.cpp:359](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:359), [SkillCast.cpp:420](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:420).

### 67 — Stun

Base route: 67. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_STUN`.
MuMain source: [WSclient.cpp:1523](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:1523), [WSclient.cpp:3952](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3952), [WSclient.cpp:4617](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4617), [WSclient.cpp:5363](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5363), [ClassAttack.cpp:236](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:236).

### 68 — CancelStun

Base route: 68. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_REMOVAL_STUN`.
MuMain source: [WSclient.cpp:4641](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4641), [ClassAttack.cpp:236](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:236), [ClassAttack.cpp:1329](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1329), [ClassAttack.cpp:1826](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1826), [ZzzCharacter.cpp:2923](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2923).

### 69 — SwellMana

Base route: 69. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_MANA`.
MuMain source: [WSclient.cpp:3958](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3958), [WSclient.cpp:4728](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4728), [ClassAttack.cpp:236](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:236), [ClassAttack.cpp:1330](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1330), [ClassAttack.cpp:1863](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1863).

### 70 — Invisibility

Base route: 70. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_INVISIBLE`.
MuMain source: [WSclient.cpp:3955](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3955), [WSclient.cpp:4670](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4670), [WSclient.cpp:5367](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5367), [ClassAttack.cpp:236](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:236), [ClassAttack.cpp:1330](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1330).

### 71 — CancelInvisibility

Base route: 71. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_REMOVAL_INVISIBLE`.
MuMain source: [WSclient.cpp:4702](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4702), [ClassAttack.cpp:236](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:236), [ClassAttack.cpp:1331](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1331), [ClassAttack.cpp:1927](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1927), [ZzzCharacter.cpp:2924](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2924).

### 72 — AbolishMagic

Base route: 72. registered visual; parity not certified.
Client factory: [AbolishMagicSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/AbolishMagicSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_REMOVAL_BUFF`.
MuMain source: [WSclient.cpp:1523](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:1523), [WSclient.cpp:4756](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4756), [ClassAttack.cpp:1958](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1958), [ZzzCharacter.cpp:2927](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2927), [ZzzCharacter.cpp:4295](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4295).

### 73 — ManaRays

Base route: 73. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DEATH_CANNON`.
MuMain source: [WSclient.cpp:4564](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4564), [SkillCast.cpp:483](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:483), [SkillCast.cpp:520](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:520), [ZzzCharacter.cpp:2990](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2990), [ZzzCharacter.cpp:5018](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5018).

### 74 — FireBlast

Base route: 74. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_SPACE_SPLIT`.
MuMain source: [WSclient.cpp:4575](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4575), [SkillCast.cpp:145](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:145), [SkillCast.cpp:332](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:332), [SkillCast.cpp:364](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:364), [SkillCast.cpp:459](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:459).

### 76 — PlasmaStorm

Base route: 76. registered visual; parity not certified.
Client factory: [PlasmaStormSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_PLASMA_STORM_FENRIR`.
MuMain source: [WSclient.cpp:4216](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4216), [WSclient.cpp:5216](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5216), [WSclient.cpp:5371](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5371), [ClassAttack.cpp:237](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:237), [ClassAttack.cpp:413](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:413).

### 77 — InfinityArrow

Base route: 77. registered visual; parity not certified.
Client factory: [InfinityArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/InfinityArrowSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_INFINITY_ARROW`.
MuMain source: [WSclient.cpp:4925](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4925), [ClassAttack.cpp:274](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:274).

### 78 — FireScream

Base route: 78. registered visual; parity not certified.
Client factory: [FireScreamSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireScreamSkillEffect.cs).
Corrected cumulative yaw: center 0, right +10, left -10 (previously -20). Source adds 10 then subtracts 20 from the same angle. Original paired models are present; the custom 0.55 body brightness and remaining particle settings are not certified matches.
MuMain symbols: `AT_SKILL_FIRE_SCREAM`.
MuMain source: [WSclient.cpp:4893](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4893), [WSclient.cpp:5285](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5285), [ClassAttack.cpp:220](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:220), [ClassAttack.cpp:502](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:502), [ClassAttack.cpp:864](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:864).

### 79 — Explosion79

Base route: 79. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_EXPLODE`.
MuMain source: [WSclient.cpp:4911](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4911).

### 150 — MonsterSkill

Base route: 150. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 200 — SummonMonster

Base route: 200. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_MONSTER_SUMMON`.
MuMain source: [WSclient.cpp:4224](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4224).

### 201 — MagicAttackImmunity

Base route: 201. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_MONSTER_MAGIC_DEF`.
MuMain source: [WSclient.cpp:3998](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3998), [WSclient.cpp:4276](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4276).

### 202 — PhysicalAttackImmunity

Base route: 202. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_MONSTER_PHY_DEF`.
MuMain source: [WSclient.cpp:4002](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4002), [WSclient.cpp:4282](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4282).

### 203 — PotionofBless

Base route: 203. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 204 — PotionofSoul

Base route: 204. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 210 — SpellofProtection

Base route: 210. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_CURSED_TEMPLE_PRODECTION`.
MuMain source: [ZzzCharacter.cpp:11728](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:11728).

### 211 — SpellofRestriction

Base route: 211. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_CURSED_TEMPLE_RESTRAINT`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 212 — SpellofPursuit

Base route: 212. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_CURSED_TEMPLE_TELEPORT`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 213 — ShieldBurn

Base route: 213. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_CURSED_TEMPLE_SUBLIMATION`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 214 — DrainLife

Base route: 214. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_DRAINLIFE`.
MuMain source: [WSclient.cpp:5530](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5530), [ClassAttack.cpp:1659](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1659), [SkillCast.cpp:605](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:605), [SkillCast.cpp:610](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:610), [ZzzCharacter.cpp:5213](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5213).

### 215 — ChainLightning

Base route: 215. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_CHAINLIGHTNING`.
MuMain source: [WSclient.cpp:4933](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4933), [ClassAttack.cpp:1662](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1662), [SkillCast.cpp:654](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:654), [ZzzCharacter.cpp:5207](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5207).

### 217 — DamageReflection

Base route: 217. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ALICE_THORNS`.
MuMain source: [WSclient.cpp:4018](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4018), [WSclient.cpp:4972](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4972), [ClassAttack.cpp:1573](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1573), [SkillCast.cpp:689](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:689), [ZzzCharacter.cpp:5161](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5161).

### 218 — Berserker

Base route: 218. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ALICE_BERSERKER`.
MuMain source: [WSclient.cpp:4026](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4026), [WSclient.cpp:4982](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4982), [ClassAttack.cpp:1332](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1332), [ClassAttack.cpp:1620](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1620), [SkillCast.cpp:703](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:703).

### 219 — Sleep

Base route: 219. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_SLEEP`.
MuMain source: [WSclient.cpp:4940](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4940), [ClassAttack.cpp:1692](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1692), [SkillCast.cpp:686](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:686), [ZzzCharacter.cpp:5159](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5159), [ZzzCharacter.cpp:5166](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5166).

### 221 — Weakness

Base route: 221. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_WEAKNESS`.
MuMain source: [WSclient.cpp:5550](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5550), [ClassAttack.cpp:1334](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1334), [ClassAttack.cpp:1622](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1622), [SkillCast.cpp:723](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:723), [ZzzCharacter.cpp:4724](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4724).

### 222 — Innovation

Base route: 222. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_ENERVATION`.
MuMain source: [WSclient.cpp:5551](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5551), [ClassAttack.cpp:1334](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1334), [ClassAttack.cpp:1623](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1623), [SkillCast.cpp:724](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:724), [ZzzCharacter.cpp:4740](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4740).

### 223 — Explosion223

Base route: 223. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_SUMMON_EXPLOSION`.
MuMain source: [WSclient.cpp:5569](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5569), [WSclient.cpp:15531](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:15531), [WSclient.cpp:15746](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:15746), [ZzzCharacter.cpp:10935](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:10935), [ZzzCharacter.cpp:10939](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:10939).

### 224 — Requiem

Base route: 224. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_SUMMON_REQUIEM`.
MuMain source: [WSclient.cpp:5570](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5570), [WSclient.cpp:15526](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:15526), [WSclient.cpp:15740](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:15740), [ZzzCharacter.cpp:10944](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:10944), [ZzzCharacter.cpp:10948](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:10948).

### 225 — Pollution

Base route: 225. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_SUMMON_POLLUTION`.
MuMain source: [WSclient.cpp:5571](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5571).

### 230 — LightningShock

Base route: 230. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_LIGHTNING_SHOCK`.
MuMain source: [WSclient.cpp:5583](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5583), [ClassAttack.cpp:1626](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1626), [ZzzCharacter.cpp:3009](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:3009), [ZzzCharacter.cpp:4780](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4780).

### 232 — StrikeofDestruction

Base route: 232. registered visual; parity not certified.
Client factory: [StrikeOfDestructionSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_STRIKE_OF_DESTRUCTION`.
MuMain source: [WSclient.cpp:3938](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3938), [WSclient.cpp:5322](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5322), [ClassAttack.cpp:993](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:993), [ZzzCharacter.cpp:3014](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:3014), [ZzzCharacter.cpp:4169](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4169).

### 233 — ExpansionofWizardry

Base route: 233. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_EXPANSION_OF_WIZARDRY`.
MuMain source: [WSclient.cpp:4021](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4021), [WSclient.cpp:4993](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4993), [ClassAttack.cpp:1289](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1289).

### 234 — Recovery

Base route: 234. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_RECOVER`.
MuMain source: [WSclient.cpp:4227](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4227), [WSclient.cpp:5475](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5475), [ClassAttack.cpp:299](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:299), [SkillCast.cpp:563](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:563).

### 235 — MultiShot

Base route: 235. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_MULTI_SHOT`.
MuMain source: [WSclient.cpp:4485](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4485), [WSclient.cpp:5424](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5424), [ClassAttack.cpp:324](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:324), [ZzzCharacter.cpp:4826](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4826), [ZzzCharacter.cpp:4847](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4847).

### 236 — FlameStrike

Base route: 236. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_FLAME_STRIKE`.
MuMain source: [WSclient.cpp:5576](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5576), [ClassAttack.cpp:784](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:784), [ZzzCharacter.cpp:2996](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2996), [ZzzCharacter.cpp:4754](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4754).

### 237 — GiganticStorm

Base route: 237. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_GIGANTIC_STORM`.
MuMain source: [WSclient.cpp:5579](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5579), [ClassAttack.cpp:504](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:504), [ClassAttack.cpp:814](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:814), [ZzzCharacter.cpp:3001](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:3001), [ZzzCharacter.cpp:4761](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4761).

### 238 — ChaoticDiseier

Base route: 238. registered visual; parity not certified.
Client factory: [ChaoticDiseierSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ChaoticDiseierSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_CHAOTIC_DISEIER`.
MuMain source: [WSclient.cpp:5014](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5014), [WSclient.cpp:5374](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5374), [ClassAttack.cpp:505](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:505), [ClassAttack.cpp:696](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:696), [SkillCast.cpp:254](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:254).

### 239 — DoppelgangerSelfExplosion

Base route: 239. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DOPPELGANGER_SELFDESTRUCTION`.
MuMain source: [WSclient.cpp:5008](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5008).

### 250 — SelupanPoison

Base route: 250. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 251 — SelupanIceStorm

Base route: 251. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 252 — SelupanIceStrike

Base route: 252. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 253 — SelupanFall

Base route: 253. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 260 — KillingBlow

Base route: 260. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_KILLING_BLOW`.
MuMain source: [WSclient.cpp:5071](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5071), [SkillCast.cpp:367](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:367), [SkillCast.cpp:775](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:775), [SkillCast.cpp:1004](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1004), [MonkSystem.cpp:402](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:402).

### 261 — BeastUppercut

Base route: 261. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_BEAST_UPPERCUT`.
MuMain source: [WSclient.cpp:5150](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5150), [WSclient.cpp:13382](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13382), [SkillCast.cpp:370](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:370), [SkillCast.cpp:836](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:836), [SkillCast.cpp:1007](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1007).

### 262 — ChainDrive

Base route: 262. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_CHAIN_DRIVE`.
MuMain source: [WSclient.cpp:5157](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5157), [SkillCast.cpp:851](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:851), [SkillCast.cpp:1010](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1010), [MonkSystem.cpp:412](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:412), [MonkSystem.cpp:445](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:445).

### 263 — DarkSide

Base route: 263. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DARKSIDE`.
MuMain source: [WSclient.cpp:5189](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5189), [WSclient.cpp:13360](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13360), [WSclient.cpp:13398](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13398), [WSclient.cpp:13418](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13418), [SkillCast.cpp:882](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:882).

### 264 — DragonRoar

Base route: 264. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DRAGON_ROAR`.
MuMain source: [WSclient.cpp:5173](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5173), [WSclient.cpp:5587](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5587), [SkillCast.cpp:898](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:898), [SkillCast.cpp:1013](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1013), [MonkSystem.cpp:420](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:420).

### 265 — DragonSlasher

Base route: 265. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DRAGON_KICK`.
MuMain source: [WSclient.cpp:5159](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5159), [SkillCast.cpp:867](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:867), [SkillCast.cpp:1012](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1012), [MonkSystem.cpp:424](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:424), [MonkSystem.cpp:447](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:447).

### 266 — IgnoreDefense

Base route: 266. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ATT_UP_OURFORCES`.
MuMain source: [WSclient.cpp:4006](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4006), [WSclient.cpp:5085](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5085), [WSclient.cpp:5126](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5126), [WSclient.cpp:5134](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5134), [SkillCast.cpp:914](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:914).

### 267 — IncreaseHealth

Base route: 267. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_HP_UP_OURFORCES`.
MuMain source: [WSclient.cpp:4009](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4009), [WSclient.cpp:5086](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5086), [WSclient.cpp:5110](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5110), [WSclient.cpp:5137](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5137), [SkillCast.cpp:915](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:915).

### 268 — IncreaseBlock

Base route: 268. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DEF_UP_OURFORCES`.
MuMain source: [WSclient.cpp:4013](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4013), [WSclient.cpp:5088](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5088), [WSclient.cpp:5115](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5115), [WSclient.cpp:5141](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5141), [SkillCast.cpp:917](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:917).

### 269 — Charge

Base route: 269. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_OCCUPY`.
MuMain source: [WSclient.cpp:5166](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5166), [SkillCast.cpp:778](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:778), [SkillCast.cpp:792](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:792), [SkillCast.cpp:1015](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1015), [MonkSystem.cpp:1028](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:1028).

### 270 — PhoenixShot

Base route: 270. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_PHOENIX_SHOT`.
MuMain source: [SkillCast.cpp:1016](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1016), [MonkSystem.cpp:1344](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:1344).

### 300 — DurabilityReduction1

Base route: 300. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MASTER_BEGIN`, `AT_SKILL_DurabilityReduction1`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 301 — PvPDefenceRateInc

Base route: 301. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_PvPDefenceRateInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 302 — MaximumSDincrease

Base route: 302. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MaximumSDincrease`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 303 — AutomaticManaRecInc

Base route: 303. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_AutomaticManaRecInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 304 — PoisonResistanceInc

Base route: 304. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_PoisonResistanceInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 305 — DurabilityReduction2

Base route: 305. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DurabilityReduction2`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 306 — SdRecoverySpeedInc

Base route: 306. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_SdRecoverySpeedInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 307 — AutomaticHpRecInc

Base route: 307. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_AutomaticHpRecInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 308 — LightningResistanceInc

Base route: 308. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_LightningResistanceInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 309 — DefenseIncrease

Base route: 309. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DefenseIncrease`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 310 — AutomaticAgRecInc

Base route: 310. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_AutomaticAgRecInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 311 — IceResistanceIncrease

Base route: 311. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IceResistanceIncrease`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 312 — DurabilityReduction3

Base route: 312. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DurabilityReduction3`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 313 — DefenseSuccessRateInc

Base route: 313. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DefenseSuccessRateInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 314 — CastInvincibility

Base route: 314. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 315 — ArmorSetBonusInc

Base route: 315. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 316 — Vengeance

Base route: 316. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 317 — EnergyIncrease

Base route: 317. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 318 — StaminaIncrease

Base route: 318. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 319 — AgilityIncrease

Base route: 319. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 320 — StrengthIncrease

Base route: 320. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 321 — WingofStormAbsPowUp

Base route: 321. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 322 — WingofStormDefPowUp

Base route: 322. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 323 — IronDefense

Base route: 323. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 324 — WingofStormAttPowUp

Base route: 324. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 325 — AttackSuccRateInc

Base route: 325. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_AttackSuccRateInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 326 — CycloneStrengthener

Base route: 22. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_CYCLONE`.
MuMain source: [WSclient.cpp:4433](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4433), [WSclient.cpp:4436](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4436), [ClassAttack.cpp:1282](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1282), [SkillCast.cpp:239](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:239).

### 327 — SlashStrengthener

Base route: 23. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_SLASH`.
MuMain source: [WSclient.cpp:4441](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4441), [WSclient.cpp:4445](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4445), [ClassAttack.cpp:1285](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1285), [SkillCast.cpp:88](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:88), [SkillCast.cpp:242](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:242).

### 328 — FallingSlashStreng

Base route: 19. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_FALLING_SLASH`.
MuMain source: [WSclient.cpp:4413](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4413), [WSclient.cpp:4415](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4415), [WSclient.cpp:4422](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4422), [WSclient.cpp:4428](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4428), [WSclient.cpp:4436](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4436).

### 329 — LungeStrengthener

Base route: 20. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_LUNGE`.
MuMain source: [WSclient.cpp:4420](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4420), [WSclient.cpp:4422](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4422), [ClassAttack.cpp:1279](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1279), [SkillCast.cpp:236](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:236).

### 330 — TwistingSlashStreng

Base route: 41. registered visual; parity not certified.
Client factory: [TwistingSlashSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/TwistingSlashSkillEffect.cs).
Mismatch remains: source wheel1 (5 frames) emits wheel2 weapon-shadow effects (25 frames); current rotating flare arcs, Shiny05, sparks and custom shadow settings are an artistic substitute.
MuMain symbols: `AT_SKILL_TWISTING_SLASH`.
MuMain source: [WSclient.cpp:5260](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5260), [ClassAttack.cpp:506](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:506), [ClassAttack.cpp:602](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:602), [SkillCast.cpp:130](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:130), [SkillCast.cpp:307](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:307).

### 331 — RagefulBlowStreng

Base route: 42. registered visual; parity not certified.
Client factory: [RagefulBlowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/RagefulBlowSkillEffect.cs).
Original fury/earthquake models and companion effects exist. Detailed source sequence is implemented, but animation-frame alignment, model rendering and particle texture/size still need live comparison.
MuMain symbols: `AT_SKILL_RAGEFUL_BLOW`.
MuMain source: [WSclient.cpp:5316](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5316), [ClassAttack.cpp:965](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:965), [ZzzCharacter.cpp:4159](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4159).

### 332 — TwistingSlashMastery

Base route: 41. registered visual; parity not certified.
Client factory: [TwistingSlashSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/TwistingSlashSkillEffect.cs).
Mismatch remains: source wheel1 (5 frames) emits wheel2 weapon-shadow effects (25 frames); current rotating flare arcs, Shiny05, sparks and custom shadow settings are an artistic substitute.
MuMain symbols: `AT_SKILL_TWISTING_SLASH`.
MuMain source: [WSclient.cpp:5260](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5260), [ClassAttack.cpp:506](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:506), [ClassAttack.cpp:602](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:602), [SkillCast.cpp:130](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:130), [SkillCast.cpp:307](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:307).

### 333 — RagefulBlowMastery

Base route: 42. registered visual; parity not certified.
Client factory: [RagefulBlowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/RagefulBlowSkillEffect.cs).
Original fury/earthquake models and companion effects exist. Detailed source sequence is implemented, but animation-frame alignment, model rendering and particle texture/size still need live comparison.
MuMain symbols: `AT_SKILL_RAGEFUL_BLOW`.
MuMain source: [WSclient.cpp:5316](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5316), [ClassAttack.cpp:965](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:965), [ZzzCharacter.cpp:4159](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4159).

### 334 — MaximumLifeIncrease

Base route: 334. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MaximumLifeIncrease`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 335 — WeaponMasteryBladeMaster

Base route: 335. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_WeaponMasteryBladeMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 336 — DeathStabStrengthener

Base route: 43. registered visual; parity not certified.
Client factory: [DeathStabSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DeathStabSkillEffect.cs).
Mismatch remains: current three-phase energy/spike/lightning composition is not the source attack-counter sequence. Reference uses weapon bone transforms at attack frames 2..8 and repeated spear spawns at 6..12; the replacement lightning is also custom.
MuMain symbols: `AT_SKILL_DEATHSTAB`.
MuMain source: [WSclient.cpp:4470](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4470), [WSclient.cpp:5358](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5358), [SkillCast.cpp:134](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:134), [SkillCast.cpp:157](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:157), [SkillCast.cpp:303](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:303).

### 337 — StrikeofDestrStr

Base route: 232. registered visual; parity not certified.
Client factory: [StrikeOfDestructionSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/MeleeAndFenrirSkillEffects.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_STRIKE_OF_DESTRUCTION`.
MuMain source: [WSclient.cpp:3938](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3938), [WSclient.cpp:5322](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5322), [ClassAttack.cpp:993](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:993), [ZzzCharacter.cpp:3014](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:3014), [ZzzCharacter.cpp:4169](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4169).

### 338 — MaximumManaIncrease

Base route: 338. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MaximumManaIncrease`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 339 — DeathStabProficiency

Base route: 339. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 340 — StrikeofDestrProf

Base route: 340. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 341 — MaximumAgIncrease

Base route: 341. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 342 — DeathStabMastery

Base route: 342. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 343 — StrikeofDestrMast

Base route: 343. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 344 — BloodStorm

Base route: 344. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 345 — ComboStrengthener

Base route: 345. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 346 — BloodStormStrengthener

Base route: 346. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 347 — PvPAttackRate

Base route: 347. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_PvPAttackRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 348 — TwoHandedSwordStrengthener

Base route: 348. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_TwoHandedSwordStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 349 — OneHandedSwordStrengthener

Base route: 349. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_OneHandedSwordStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 350 — MaceStrengthener

Base route: 350. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MaceStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 351 — SpearStrengthener

Base route: 351. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_SpearStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 352 — TwoHandedSwordMaster

Base route: 352. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_TwoHandedSwordMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 353 — OneHandedSwordMaster

Base route: 353. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_OneHandedSwordMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 354 — MaceMastery

Base route: 354. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MaceMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 355 — SpearMastery

Base route: 355. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_SpearMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 356 — SwellLifeStrengt

Base route: 48. registered visual; parity not certified.
Client factory: [GreaterFortitudeSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/GreaterFortitudeSkillEffect.cs).
Previous correction retained: attack-counter gated 36 spirit joints, subtype-2 movement, flare tails and two terrain circles. Persistent orange upper-body particles are separate. Headless timing/physics checks pass; visual comparison still needed.
MuMain symbols: `AT_SKILL_SWELL_LIFE`.
MuMain source: [WSclient.cpp:3973](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3973), [WSclient.cpp:4843](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4843), [ClassAttack.cpp:488](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:488), [ClassAttack.cpp:958](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:958), [ZzzCharacter.cpp:2851](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2851).

### 357 — ManaReduction

Base route: 357. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ManaReduction`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 358 — MonsterAttackSdInc

Base route: 358. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MonsterAttackSdInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 359 — MonsterAttackLifeInc

Base route: 359. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MonsterAttackLifeInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 360 — SwellLifeProficiency

Base route: 48. registered visual; parity not certified.
Client factory: [GreaterFortitudeSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/GreaterFortitudeSkillEffect.cs).
Previous correction retained: attack-counter gated 36 spirit joints, subtype-2 movement, flare tails and two terrain circles. Persistent orange upper-body particles are separate. Headless timing/physics checks pass; visual comparison still needed.
MuMain symbols: `AT_SKILL_SWELL_LIFE`.
MuMain source: [WSclient.cpp:3973](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3973), [WSclient.cpp:4843](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4843), [ClassAttack.cpp:488](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:488), [ClassAttack.cpp:958](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:958), [ZzzCharacter.cpp:2851](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2851).

### 361 — MinimumAttackPowerInc

Base route: 361. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MinimumAttackPowerInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 362 — MonsterAttackManaInc

Base route: 362. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MonsterAttackManaInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 363 — SwellLifeMastery

Base route: 363. registered visual; parity not certified.
Client factory: [GreaterFortitudeSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/GreaterFortitudeSkillEffect.cs).
Previous correction retained: attack-counter gated 36 spirit joints, subtype-2 movement, flare tails and two terrain circles. Persistent orange upper-body particles are separate. Headless timing/physics checks pass; visual comparison still needed.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 364 — MaximumAttackPowerInc

Base route: 364. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 366 — Inccritdamagerate

Base route: 366. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 367 — RestoresallMana

Base route: 367. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 368 — RestoresallHp

Base route: 368. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 369 — Incexcdamagerate

Base route: 369. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 370 — Incdoubledamagerate

Base route: 370. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 371 — IncchanceofignoreDef

Base route: 371. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 372 — RestoresallSd

Base route: 372. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 373 — Inctripledamagerate

Base route: 373. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 374 — EternalWingsAbsPowUp

Base route: 374. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 375 — EternalWingsDefPowUp

Base route: 375. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 377 — EternalWingsAttPowUp

Base route: 377. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 378 — FlameStrengthener

Base route: 5. registered visual; parity not certified.
Client factory: [FlameSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FlameSkillEffect.cs).
Source structure present: terrain bitmap plus Flame01 particles, area lifetime 40 frames, targeted lifetime 20, source subtype-specific emission counts. Lighting, terrain projection, and particle rendering still need in-game comparison.
MuMain symbols: `AT_SKILL_FLAME`.
MuMain source: [WSclient.cpp:4336](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4336), [ClassAttack.cpp:1378](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1378), [ZzzCharacter.cpp:4479](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4479), [ZzzCharacter.cpp:4485](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4485), [ZzzCharacter.cpp:5035](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5035).

### 379 — LightningStrengthener

Base route: 3. registered visual; parity not certified.
Client factory: [LightningSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/LightningSkillEffect.cs).
MuMain player Lightning emits two BITMAP_JOINT_THUNDER subtype-0 ribbons at scales 50 and 10 plus BITMAP_ENERGY. The factory now uses the two source ribbons instead of the custom branching screen-space bolt. First-frame renders, cast cadence, energy/impact particles and terrain light still require validation; this is not a certified match.
MuMain symbols: `AT_SKILL_LIGHTNING`.
MuMain source: [WSclient.cpp:4319](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4319), [SkillCast.cpp:506](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:506), [ZzzCharacter.cpp:2253](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2253).

### 380 — ExpansionofWizStreng

Base route: 233. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_EXPANSION_OF_WIZARDRY`.
MuMain source: [WSclient.cpp:4021](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4021), [WSclient.cpp:4993](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4993), [ClassAttack.cpp:1289](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1289).

### 381 — InfernoStrengthener

Base route: 14. registered visual; parity not certified.
Client factory: [InfernoSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/InfernoSkillEffect.cs).
Core model and fifteen-frame lifetime agree with selected source parameters. Current custom burst/spark composition requires comparison with the exact source inferno move branch; not approved as a full match.
MuMain symbols: `AT_SKILL_INFERNO`.
MuMain source: [WSclient.cpp:5255](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5255), [ClassAttack.cpp:1210](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1210), [ZzzCharacter.cpp:4573](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4573).

### 382 — BlastStrengthener

Base route: 13. registered visual; parity not certified.
Client factory: [CometfallSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/CometfallSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_BLAST`.
MuMain source: [SkillCast.cpp:526](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:526), [ZzzCharacter.cpp:4556](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4556).

### 383 — ExpansionofWizMas

Base route: 233. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_EXPANSION_OF_WIZARDRY`.
MuMain source: [WSclient.cpp:4021](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4021), [WSclient.cpp:4993](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4993), [ClassAttack.cpp:1289](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1289).

### 384 — PoisonStrengthener

Base route: 1. registered visual; parity not certified.
Client factory: [PoisonSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PoisonSkillEffect.cs).
Live MuMain and TwinMU timelines inspected. Core family is present, but smoke and brightness differ. Terrain light now uses MuMain RGB(.3,1,.6), radius two tiles and core remaining lifetime*.1; the custom pulse and longer smoke-based lighting lifetime were removed. This source correction was rendered again; exact brightness and smoke parity remain unverified.
MuMain symbols: `AT_SKILL_POISON`.
MuMain source: [WSclient.cpp:3929](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3929), [WSclient.cpp:4334](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4334), [SkillCast.cpp:503](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:503), [ZzzCharacter.cpp:4992](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4992).

### 385 — EvilSpiritStreng

Base route: 9. registered visual; parity not certified.
Client factory: [EvilSpiritSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/EvilSpiritSkillEffect.cs).
Four paired spirits match the MuMain cast structure. Motion constants, subtractive blend factors, single scale-1.3 static head, life*.1 brightness, joint expiry, terrain darkness .04..07, floor-based FPS-scaled tail capacity up to 200, tail UVs and pitch signs corrected against MuMain source. Paired live reference and TwinMU capture timelines exist in Publish/SkillComparison/Sheets/skill-9-timeline.png. The corrected heads and tails remain above terrain. Trail shape, brightness, persistent heads versus one-frame MODEL_LASER emissions, lighting falloff and random/timing behavior remain different or unverified. This is not a certified rendering match.
MuMain symbols: `AT_SKILL_EVIL_SPIRIT`.
MuMain source: [ClassAttack.cpp:1355](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1355), [ZzzCharacter.cpp:4584](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4584).

### 386 — MagicMasteryGrandMaster

Base route: 386. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MagicMasteryGrandMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 387 — DecayStrengthener

Base route: 38. registered visual; parity not certified.
Client factory: [DecaySkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DecaySkillEffect.cs).
Two hidden subtype-6 falling emitters and source inferno impact exist. Corrected shared inverse rotation affecting its ribbons/smoke. Remaining particle sizing and settle/impact behavior need comparison; source likeness is not certified.
MuMain symbols: `AT_SKILL_DECAY`.
MuMain source: [WSclient.cpp:3991](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3991), [WSclient.cpp:5235](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5235), [ClassAttack.cpp:1384](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1384), [ZzzCharacter.cpp:4440](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4440).

### 388 — HellfireStrengthener

Base route: 10. registered visual; parity not certified.
Client factory: [HellFireSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/HellFireSkillEffect.cs).
Source models and 45/40-frame lifetimes match selected parameters. Mismatch remains: current EdgeTrimDeformer modifies model UVs by a 5% margin; source uses the original UVs and its life-driven scroll. Needs a targeted rendering port.
MuMain symbols: `AT_SKILL_HELL_FIRE`.
MuMain source: [WSclient.cpp:5250](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5250), [ClassAttack.cpp:1200](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1200), [ZzzCharacter.cpp:4423](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4423).

### 389 — IceStrengthener

Base route: 7. registered visual; parity not certified.
Client factory: [IceSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/IceSkillEffect.cs).
Core lifetime 50 frames and scale .8 corrected from MuMain. Animation, smoke, alpha fade and randomized shard physics still need comparison; current shards retain custom drift and lighting fade. No paired rendering match has been verified.
MuMain symbols: `AT_SKILL_ICE`.
MuMain source: [WSclient.cpp:3933](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3933), [WSclient.cpp:4329](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4329), [SkillCast.cpp:511](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:511), [ZzzCharacter.cpp:4955](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4955).

### 390 — MeteorStrengthener

Base route: 390. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 391 — IceStormStrengthener

Base route: 391. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 392 — NovaStrengthener

Base route: 392. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 393 — IceStormMastery

Base route: 393. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 394 — MeteorMastery

Base route: 394. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 395 — NovaCastStrengthener

Base route: 395. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 397 — OneHandedStaffStrengthener

Base route: 397. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_OneHandedStaffStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 398 — TwoHandedStaffStrengthener

Base route: 398. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_TwoHandedStaffStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 399 — ShieldStrengthenerGrandMaster

Base route: 399. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ShieldStrengthenerGrandMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 400 — OneHandedStaffMaster

Base route: 400. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_OneHandedStaffMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 401 — TwoHandedStaffMaster

Base route: 401. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_TwoHandedStaffMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 402 — ShieldMasteryGrandMaster

Base route: 402. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ShieldMasteryGrandMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 403 — SoulBarrierStrength

Base route: 16. no registry visual; alternate path must be checked.
Soul Barrier has a separate persistent ClassBuffController path; absence of a factory does not mean absence of its aura.
MuMain symbols: `AT_SKILL_SOUL_BARRIER`.
MuMain source: [WSclient.cpp:3986](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3986), [WSclient.cpp:4309](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4309), [ClassAttack.cpp:1151](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1151), [ZzzCharacter.cpp:4972](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4972), [ZzzCharacter.cpp:5268](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5268).

### 404 — SoulBarrierProficie

Base route: 16. no registry visual; alternate path must be checked.
Soul Barrier has a separate persistent ClassBuffController path; absence of a factory does not mean absence of its aura.
MuMain symbols: `AT_SKILL_SOUL_BARRIER`.
MuMain source: [WSclient.cpp:3986](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3986), [WSclient.cpp:4309](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4309), [ClassAttack.cpp:1151](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1151), [ZzzCharacter.cpp:4972](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4972), [ZzzCharacter.cpp:5268](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5268).

### 405 — MinimumWizardryInc

Base route: 405. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MinimumWizardryInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 406 — SoulBarrierMastery

Base route: 406. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 407 — MaximumWizardryInc

Base route: 407. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 409 — IllusionWingsAbsPowUp

Base route: 409. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 410 — IllusionWingsDefPowUp

Base route: 410. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 411 — MultiShotStreng

Base route: 411. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 412 — IllusionWingsAttPowUp

Base route: 412. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 413 — HealStrengthener

Base route: 26. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_HEALING`.
MuMain source: [WSclient.cpp:4287](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4287), [ClassAttack.cpp:155](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:155), [ClassAttack.cpp:211](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:211), [ClassAttack.cpp:250](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:250), [SkillCast.cpp:559](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:559).

### 414 — TripleShotStrengthener

Base route: 24. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_TRIPLE_SHOT`.
MuMain source: [WSclient.cpp:1537](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:1537), [WSclient.cpp:4479](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4479), [WSclient.cpp:5227](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5227), [SkillCast.cpp:1152](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1152), [SkillCast.cpp:1187](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1187).

### 415 — SummonedMonsterStr1

Base route: 415. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_SummonedMonsterStr1`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 416 — PenetrationStrengthener

Base route: 52. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_PENETRATION`.
MuMain source: [ZzzEffect.cpp:5570](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5570), [ZzzEffect.cpp:5613](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5613), [ZzzEffect.cpp:5623](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5623), [ZzzEffect.cpp:5649](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5649), [WSclient.cpp:4516](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4516).

### 417 — DefenseIncreaseStr

Base route: 27. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_DEFENSE`.
MuMain source: [WSclient.cpp:3947](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3947), [WSclient.cpp:4183](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4183), [WSclient.cpp:4292](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4292), [ClassAttack.cpp:160](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:160), [ClassAttack.cpp:216](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:216).

### 418 — TripleShotMastery

Base route: 24. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_TRIPLE_SHOT`.
MuMain source: [WSclient.cpp:1537](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:1537), [WSclient.cpp:4479](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4479), [WSclient.cpp:5227](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5227), [SkillCast.cpp:1152](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1152), [SkillCast.cpp:1187](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1187).

### 419 — SummonedMonsterStr2

Base route: 419. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_SummonedMonsterStr2`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 420 — AttackIncreaseStr

Base route: 28. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_ATTACK`.
MuMain source: [WSclient.cpp:3942](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3942), [WSclient.cpp:4183](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4183), [WSclient.cpp:4289](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4289), [ClassAttack.cpp:157](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:157), [ClassAttack.cpp:213](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:213).

### 421 — WeaponMasteryHighElf

Base route: 421. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_WeaponMasteryHighElf`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 422 — AttackIncreaseMastery

Base route: 28. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_ATTACK`.
MuMain source: [WSclient.cpp:3942](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3942), [WSclient.cpp:4183](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4183), [WSclient.cpp:4289](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4289), [ClassAttack.cpp:157](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:157), [ClassAttack.cpp:213](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:213).

### 423 — DefenseIncreaseMastery

Base route: 27. registered visual; parity not certified.
Client factory: [ElfSupportSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ElfSupportSkillEffect.cs).
Original subtype-specific healing joints and impact glow already ported; persistent effects are separate ElfSupportBuffController objects. Greater Damage has SHINY+1 arm glows in addition to the source shared green trails. Heal has no persistent buff aura. Previous fixes retained.
MuMain symbols: `AT_SKILL_DEFENSE`.
MuMain source: [WSclient.cpp:3947](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3947), [WSclient.cpp:4183](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4183), [WSclient.cpp:4292](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4292), [ClassAttack.cpp:160](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:160), [ClassAttack.cpp:216](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:216).

### 424 — IceArrowStrengthener

Base route: 51. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain symbols: `AT_SKILL_ICE_ARROW`.
MuMain source: [ZzzEffect.cpp:5582](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5582), [ZzzEffect.cpp:5673](C:/MuOnline_Source/MuMain-main/src/source/Render/Effects/ZzzEffect.cpp:5673), [WSclient.cpp:3978](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3978), [WSclient.cpp:4522](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4522), [ClassAttack.cpp:264](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:264).

### 425 — Cure

Base route: 425. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 426 — PartyHealing

Base route: 426. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 427 — PoisonArrow

Base route: 427. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 428 — SummonedMonsterStr3

Base route: 428. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 429 — PartyHealingStr

Base route: 429. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 430 — Bless

Base route: 430. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 431 — MultiShotMastery

Base route: 431. registered visual; parity not certified.
Client factory: [ArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ArrowSkillEffect.cs).
Original arrow models and separate volley types exist. Enhanced trails and impact particles are custom; this is not a verified original port. Check each bow model, penetration, ice impact, and Multi-Shot separately.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 432 — SummonSatyros

Base route: 432. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 433 — BlessStrengthener

Base route: 433. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 434 — PoisonArrowStr

Base route: 434. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 435 — BowStrengthener

Base route: 435. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_BowStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 436 — CrossbowStrengthener

Base route: 436. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_CrossbowStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 437 — ShieldStrengthenerHighElf

Base route: 437. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ShieldStrengthenerHighElf`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 438 — BowMastery

Base route: 438. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_BowMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 439 — CrossbowMastery

Base route: 439. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_CrossbowMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 440 — ShieldMasteryHighElf

Base route: 440. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ShieldMasteryHighElf`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 441 — InfinityArrowStr

Base route: 77. registered visual; parity not certified.
Client factory: [InfinityArrowSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/InfinityArrowSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_INFINITY_ARROW`.
MuMain source: [WSclient.cpp:4925](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4925), [ClassAttack.cpp:274](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:274).

### 442 — MinimumAttPowerInc

Base route: 442. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MinimumAttPowerInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 443 — MaximumAttPowerInc

Base route: 443. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 445 — DimensionWingsAbsPowUp

Base route: 445. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 446 — DimensionWingsDefPowUp

Base route: 446. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 447 — DimensionWingsAttPowUp

Base route: 447. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 448 — FireTomeStrengthener

Base route: 448. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_FireTomeStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 449 — WindTomeStrengthener

Base route: 449. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_WindTomeStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 450 — LightningTomeStren

Base route: 450. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_LightningTomeStren`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 451 — FireTomeMastery

Base route: 451. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_FireTomeMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 452 — WindTomeMastery

Base route: 452. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_WindTomeMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 453 — LightningTomeMastery

Base route: 453. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_LightningTomeMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 454 — SleepStrengthener

Base route: 219. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_SLEEP`.
MuMain source: [WSclient.cpp:4940](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4940), [ClassAttack.cpp:1692](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1692), [SkillCast.cpp:686](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:686), [ZzzCharacter.cpp:5159](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5159), [ZzzCharacter.cpp:5166](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5166).

### 455 — ChainLightningStr

Base route: 215. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_CHAINLIGHTNING`.
MuMain source: [WSclient.cpp:4933](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4933), [ClassAttack.cpp:1662](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1662), [SkillCast.cpp:654](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:654), [ZzzCharacter.cpp:5207](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5207).

### 456 — LightningShockStr

Base route: 230. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_LIGHTNING_SHOCK`.
MuMain source: [WSclient.cpp:5583](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5583), [ClassAttack.cpp:1626](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1626), [ZzzCharacter.cpp:3009](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:3009), [ZzzCharacter.cpp:4780](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4780).

### 457 — MagicMasterySummoner

Base route: 457. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MagicMasterySummoner`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 458 — DrainLifeStrengthener

Base route: 214. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_ALICE_DRAINLIFE`.
MuMain source: [WSclient.cpp:5530](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5530), [ClassAttack.cpp:1659](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1659), [SkillCast.cpp:605](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:605), [SkillCast.cpp:610](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:610), [ZzzCharacter.cpp:5213](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5213).

### 459 — WeaknessStrengthener

Base route: 459. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 460 — InnovationStrengthener

Base route: 460. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 461 — Blind

Base route: 461. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 462 — DrainLifeMastery

Base route: 462. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 463 — BlindStrengthener

Base route: 463. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 465 — StickStrengthener

Base route: 465. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_StickStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 466 — OtherWorldTomeStreng

Base route: 466. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_OtherWorldTomeStreng`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 467 — StickMastery

Base route: 467. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_StickMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 468 — OtherWorldTomeMastery

Base route: 468. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_OtherWorldTomeMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 469 — BerserkerStrengthener

Base route: 218. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ALICE_BERSERKER`.
MuMain source: [WSclient.cpp:4026](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4026), [WSclient.cpp:4982](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4982), [ClassAttack.cpp:1332](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1332), [ClassAttack.cpp:1620](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1620), [SkillCast.cpp:703](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:703).

### 470 — BerserkerProficiency

Base route: 218. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ALICE_BERSERKER`.
MuMain source: [WSclient.cpp:4026](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4026), [WSclient.cpp:4982](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4982), [ClassAttack.cpp:1332](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1332), [ClassAttack.cpp:1620](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1620), [SkillCast.cpp:703](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:703).

### 471 — MinimumWizCurseInc

Base route: 471. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MinimumWizCurseInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 472 — BerserkerMastery

Base route: 472. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 473 — MaximumWizCurseInc

Base route: 473. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 475 — WingofRuinAbsPowUp

Base route: 475. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 476 — WingofRuinDefPowUp

Base route: 476. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 478 — WingofRuinAttPowUp

Base route: 478. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 479 — CycloneStrengthenerDuelMaster

Base route: 22. registered visual; parity not certified.
Client factory: [WeaponSkillFlashFactory](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/WeaponSkillFlashFactory.cs).
Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.
MuMain symbols: `AT_SKILL_CYCLONE`.
MuMain source: [WSclient.cpp:4433](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4433), [WSclient.cpp:4436](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4436), [ClassAttack.cpp:1282](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1282), [SkillCast.cpp:239](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:239).

### 480 — LightningStrengthenerDuelMaster

Base route: 3. registered visual; parity not certified.
Client factory: [LightningSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/LightningSkillEffect.cs).
MuMain player Lightning emits two BITMAP_JOINT_THUNDER subtype-0 ribbons at scales 50 and 10 plus BITMAP_ENERGY. The factory now uses the two source ribbons instead of the custom branching screen-space bolt. First-frame renders, cast cadence, energy/impact particles and terrain light still require validation; this is not a certified match.
MuMain symbols: `AT_SKILL_LIGHTNING`.
MuMain source: [WSclient.cpp:4319](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4319), [SkillCast.cpp:506](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:506), [ZzzCharacter.cpp:2253](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2253).

### 481 — TwistingSlashStrengthenerDuelMaster

Base route: 41. registered visual; parity not certified.
Client factory: [TwistingSlashSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/TwistingSlashSkillEffect.cs).
Mismatch remains: source wheel1 (5 frames) emits wheel2 weapon-shadow effects (25 frames); current rotating flare arcs, Shiny05, sparks and custom shadow settings are an artistic substitute.
MuMain symbols: `AT_SKILL_TWISTING_SLASH`.
MuMain source: [WSclient.cpp:5260](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5260), [ClassAttack.cpp:506](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:506), [ClassAttack.cpp:602](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:602), [SkillCast.cpp:130](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:130), [SkillCast.cpp:307](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:307).

### 482 — PowerSlashStreng

Base route: 56. registered visual; parity not certified.
Client factory: [PowerSlashSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/PowerSlashSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_POWER_SLASH`.
MuMain source: [WSclient.cpp:1537](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:1537), [WSclient.cpp:4456](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4456), [WSclient.cpp:5354](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5354), [ClassAttack.cpp:658](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:658), [ZzzCharacter.cpp:2819](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:2819).

### 483 — FlameStrengthenerDuelMaster

Base route: 5. registered visual; parity not certified.
Client factory: [FlameSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FlameSkillEffect.cs).
Source structure present: terrain bitmap plus Flame01 particles, area lifetime 40 frames, targeted lifetime 20, source subtype-specific emission counts. Lighting, terrain projection, and particle rendering still need in-game comparison.
MuMain symbols: `AT_SKILL_FLAME`.
MuMain source: [WSclient.cpp:4336](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4336), [ClassAttack.cpp:1378](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1378), [ZzzCharacter.cpp:4479](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4479), [ZzzCharacter.cpp:4485](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4485), [ZzzCharacter.cpp:5035](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:5035).

### 484 — BlastStrengthenerDuelMaster

Base route: 13. registered visual; parity not certified.
Client factory: [CometfallSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/CometfallSkillEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_BLAST`.
MuMain source: [SkillCast.cpp:526](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:526), [ZzzCharacter.cpp:4556](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4556).

### 485 — WeaponMasteryDuelMaster

Base route: 485. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_WeaponMasteryDuelMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 486 — InfernoStrengthenerDuelMaster

Base route: 14. registered visual; parity not certified.
Client factory: [InfernoSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/InfernoSkillEffect.cs).
Core model and fifteen-frame lifetime agree with selected source parameters. Current custom burst/spark composition requires comparison with the exact source inferno move branch; not approved as a full match.
MuMain symbols: `AT_SKILL_INFERNO`.
MuMain source: [WSclient.cpp:5255](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5255), [ClassAttack.cpp:1210](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1210), [ZzzCharacter.cpp:4573](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4573).

### 487 — EvilSpiritStrengthenerDuelMaster

Base route: 9. registered visual; parity not certified.
Client factory: [EvilSpiritSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/EvilSpiritSkillEffect.cs).
Four paired spirits match the MuMain cast structure. Motion constants, subtractive blend factors, single scale-1.3 static head, life*.1 brightness, joint expiry, terrain darkness .04..07, floor-based FPS-scaled tail capacity up to 200, tail UVs and pitch signs corrected against MuMain source. Paired live reference and TwinMU capture timelines exist in Publish/SkillComparison/Sheets/skill-9-timeline.png. The corrected heads and tails remain above terrain. Trail shape, brightness, persistent heads versus one-frame MODEL_LASER emissions, lighting falloff and random/timing behavior remain different or unverified. This is not a certified rendering match.
MuMain symbols: `AT_SKILL_EVIL_SPIRIT`.
MuMain source: [ClassAttack.cpp:1355](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:1355), [ZzzCharacter.cpp:4584](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4584).

### 488 — MagicMasteryDuelMaster

Base route: 488. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_MagicMasteryDuelMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 489 — IceStrengthenerDuelMaster

Base route: 7. registered visual; parity not certified.
Client factory: [IceSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/IceSkillEffect.cs).
Core lifetime 50 frames and scale .8 corrected from MuMain. Animation, smoke, alpha fade and randomized shard physics still need comparison; current shards retain custom drift and lighting fade. No paired rendering match has been verified.
MuMain symbols: `AT_SKILL_ICE`.
MuMain source: [WSclient.cpp:3933](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3933), [WSclient.cpp:4329](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4329), [SkillCast.cpp:511](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:511), [ZzzCharacter.cpp:4955](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4955).

### 490 — BloodAttackStrengthen

Base route: 55. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_FIRE_SLASH`.
MuMain source: [WSclient.cpp:3982](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3982), [WSclient.cpp:4497](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4497), [WSclient.cpp:5332](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5332), [ClassAttack.cpp:628](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:628), [SkillCast.cpp:128](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:128).

### 491 — IceMasteryDuelMaster

Base route: 491. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 492 — FlameStrikeStrengthen

Base route: 492. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 493 — FireSlashMastery

Base route: 493. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 494 — FlameStrikeMastery

Base route: 494. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 495 — EarthPrison

Base route: 495. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 496 — GiganticStormStr

Base route: 496. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 497 — EarthPrisonStr

Base route: 497. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 504 — EmperorCapeAbsPowUp

Base route: 504. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 505 — EmperorCapeDefPowUp

Base route: 505. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 506 — AddsCommandStat

Base route: 506. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 507 — EmperorCapeAttPowUp

Base route: 507. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 508 — FireBurstStreng

Base route: 61. registered visual; parity not certified.
Client factory: [FireBurstSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireBurstSkillEffect.cs).
Three homing streams, stationary fire pieces, and DarkLordSkill cast pieces exist with 25 Hz simulation. Reference structure is present; full tail sampling, model light, and moving-target comparisons remain unverified.
MuMain symbols: `AT_SKILL_FIREBURST`.
MuMain source: [WSclient.cpp:4394](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4394), [SkillCast.cpp:140](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:140), [SkillCast.cpp:329](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:329), [SkillCast.cpp:361](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:361), [SkillCast.cpp:456](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:456).

### 509 — ForceWaveStreng

Base route: 66. registered visual; parity not certified.
Client factory: [ForceSkillVisualEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ForceSkillVisualEffect.cs).
Factory exists. Exact movement, render and timing parity has not been demonstrated.
MuMain symbols: `AT_SKILL_FORCE_WAVE`.
MuMain source: [WSclient.cpp:4377](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4377), [SkillCast.cpp:138](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:138), [SkillCast.cpp:327](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:327), [SkillCast.cpp:359](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:359), [SkillCast.cpp:420](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:420).

### 510 — DarkHorseStreng1

Base route: 510. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DarkHorseStreng1`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 511 — CriticalDmgIncPowUp

Base route: 64. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ADD_CRITICAL`.
MuMain source: [WSclient.cpp:3967](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3967), [WSclient.cpp:4804](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4804), [ClassAttack.cpp:491](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:491), [ClassAttack.cpp:853](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:853), [ZzzCharacter.cpp:4366](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4366).

### 512 — EarthshakeStreng

Base route: 62. registered visual; parity not certified.
Client factory: [DarkHorseEarthquakeSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DarkHorseEarthquakeSkillEffect.cs).
Animation-based shockwave, ground-stone, and earthquake sequence exists. Removed five stale master aliases (515..519); they conflict with the current server tree. Remaining mount-animation alignment is unverified.
MuMain symbols: `AT_SKILL_EARTHSHAKE`.
MuMain source: [WSclient.cpp:5310](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5310), [ClassAttack.cpp:497](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:497), [ClassAttack.cpp:534](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:534), [ClassAttack.cpp:543](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:543), [ClassAttack.cpp:556](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:556).

### 513 — WeaponMasteryLordEmperor

Base route: 513. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_WeaponMasteryLordEmperor`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 514 — FireBurstMastery

Base route: 61. registered visual; parity not certified.
Client factory: [FireBurstSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireBurstSkillEffect.cs).
Three homing streams, stationary fire pieces, and DarkLordSkill cast pieces exist with 25 Hz simulation. Reference structure is present; full tail sampling, model light, and moving-target comparisons remain unverified.
MuMain symbols: `AT_SKILL_FIREBURST`.
MuMain source: [WSclient.cpp:4394](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4394), [SkillCast.cpp:140](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:140), [SkillCast.cpp:329](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:329), [SkillCast.cpp:361](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:361), [SkillCast.cpp:456](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:456).

### 515 — CritDmgIncPowUp2

Base route: 64. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ADD_CRITICAL`.
MuMain source: [WSclient.cpp:3967](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3967), [WSclient.cpp:4804](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4804), [ClassAttack.cpp:491](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:491), [ClassAttack.cpp:853](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:853), [ZzzCharacter.cpp:4366](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4366).

### 516 — EarthshakeMastery

Base route: 62. registered visual; parity not certified.
Client factory: [DarkHorseEarthquakeSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/DarkHorseEarthquakeSkillEffect.cs).
Animation-based shockwave, ground-stone, and earthquake sequence exists. Removed five stale master aliases (515..519); they conflict with the current server tree. Remaining mount-animation alignment is unverified.
MuMain symbols: `AT_SKILL_EARTHSHAKE`.
MuMain source: [WSclient.cpp:5310](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5310), [ClassAttack.cpp:497](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:497), [ClassAttack.cpp:534](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:534), [ClassAttack.cpp:543](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:543), [ClassAttack.cpp:556](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:556).

### 517 — CritDmgIncPowUp3

Base route: 64. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain symbols: `AT_SKILL_ADD_CRITICAL`.
MuMain source: [WSclient.cpp:3967](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:3967), [WSclient.cpp:4804](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4804), [ClassAttack.cpp:491](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:491), [ClassAttack.cpp:853](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:853), [ZzzCharacter.cpp:4366](C:/MuOnline_Source/MuMain-main/src/source/Engine/Object/ZzzCharacter.cpp:4366).

### 518 — FireScreamStren

Base route: 78. registered visual; parity not certified.
Client factory: [FireScreamSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/FireScreamSkillEffect.cs).
Corrected cumulative yaw: center 0, right +10, left -10 (previously -20). Source adds 10 then subtracts 20 from the same angle. Original paired models are present; the custom 0.55 body brightness and remaining particle settings are not certified matches.
MuMain symbols: `AT_SKILL_FIRE_SCREAM`.
MuMain source: [WSclient.cpp:4893](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4893), [WSclient.cpp:5285](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5285), [ClassAttack.cpp:220](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:220), [ClassAttack.cpp:502](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:502), [ClassAttack.cpp:864](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/ClassAttack.cpp:864).

### 519 — ElectricSparkStreng

Base route: 519. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 520 — FireScreamMastery

Base route: 520. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 521 — IronDefenseLordEmperor

Base route: 521. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 522 — CriticalDamageIncM

Base route: 522. registered visual; parity not certified.
Client factory: [ClassBuffSkillEffect](C:/OpenMU/MuOnlineS6/Client.Main/Objects/Effects/Skills/ClassBuffSkillEffect.cs).
Original DarkLordSkill and elshildring models are used. Critical Damage, Reflection, and Berserker persistent visuals use ClassBuffController. Previous corrections retained; bone attachment and overlapping buffs require in-game comparison.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 523 — ChaoticDiseierStr

Base route: 523. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 524 — IronDefenseStr

Base route: 524. no registry visual; alternate path must be checked.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 526 — DarkSpiritStr

Base route: 526. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DarkSpiritStr`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 527 — ScepterStrengthener

Base route: 527. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ScepterStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 528 — ShieldStrengthenerLordEmperor

Base route: 528. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ShieldStrengthenerLordEmperor`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 529 — UseScepterPetStr

Base route: 529. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_UseScepterPetStr`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 530 — DarkSpiritStr2

Base route: 530. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DarkSpiritStr2`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 531 — ScepterMastery

Base route: 531. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ScepterMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 532 — ShieldMastery

Base route: 532. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_ShieldMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 533 — CommandAttackInc

Base route: 533. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_CommandAttackInc`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 534 — DarkSpiritStr3

Base route: 534. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DarkSpiritStr3`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 535 — PetDurabilityStr

Base route: 535. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_PetDurabilityStr`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 536 — DarkSpiritStr4

Base route: 536. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 538 — DarkSpiritStr5

Base route: 538. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 539 — SpiritLord

Base route: 539. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 551 — KillingBlowStrengthener

Base route: 260. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_KILLING_BLOW`.
MuMain source: [WSclient.cpp:5071](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5071), [SkillCast.cpp:367](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:367), [SkillCast.cpp:775](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:775), [SkillCast.cpp:1004](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1004), [MonkSystem.cpp:402](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:402).

### 552 — BeastUppercutStrengthener

Base route: 261. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_BEAST_UPPERCUT`.
MuMain source: [WSclient.cpp:5150](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5150), [WSclient.cpp:13382](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13382), [SkillCast.cpp:370](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:370), [SkillCast.cpp:836](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:836), [SkillCast.cpp:1007](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1007).

### 554 — KillingBlowMastery

Base route: 260. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_KILLING_BLOW`.
MuMain source: [WSclient.cpp:5071](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5071), [SkillCast.cpp:367](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:367), [SkillCast.cpp:775](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:775), [SkillCast.cpp:1004](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1004), [MonkSystem.cpp:402](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:402).

### 555 — BeastUppercutMastery

Base route: 261. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_BEAST_UPPERCUT`.
MuMain source: [WSclient.cpp:5150](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5150), [WSclient.cpp:13382](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13382), [SkillCast.cpp:370](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:370), [SkillCast.cpp:836](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:836), [SkillCast.cpp:1007](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1007).

### 557 — WeaponMasteryFistMaster

Base route: 557. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_WeaponMasteryFistMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 558 — ChainDriveStrengthener

Base route: 262. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_CHAIN_DRIVE`.
MuMain source: [WSclient.cpp:5157](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5157), [SkillCast.cpp:851](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:851), [SkillCast.cpp:1010](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1010), [MonkSystem.cpp:412](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:412), [MonkSystem.cpp:445](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:445).

### 559 — DarkSideStrengthener

Base route: 263. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DARKSIDE`.
MuMain source: [WSclient.cpp:5189](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5189), [WSclient.cpp:13360](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13360), [WSclient.cpp:13398](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13398), [WSclient.cpp:13418](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:13418), [SkillCast.cpp:882](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:882).

### 560 — DragonRoarStrengthener

Base route: 264. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DRAGON_ROAR`.
MuMain source: [WSclient.cpp:5173](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5173), [WSclient.cpp:5587](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5587), [SkillCast.cpp:898](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:898), [SkillCast.cpp:1013](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:1013), [MonkSystem.cpp:420](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Social/MonkSystem.cpp:420).

### 568 — EquippedWeaponStrengthener

Base route: 568. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_EquippedWeaponStrengthener`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 569 — DefSuccessRateIncPowUp

Base route: 268. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DEF_UP_OURFORCES`.
MuMain source: [WSclient.cpp:4013](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4013), [WSclient.cpp:5088](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5088), [WSclient.cpp:5115](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5115), [WSclient.cpp:5141](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5141), [SkillCast.cpp:917](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:917).

### 571 — EquippedWeaponMastery

Base route: 571. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_EquippedWeaponMastery`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 572 — DefSuccessRateIncMastery

Base route: 268. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_DEF_UP_OURFORCES`.
MuMain source: [WSclient.cpp:4013](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4013), [WSclient.cpp:5088](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5088), [WSclient.cpp:5115](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5115), [WSclient.cpp:5141](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5141), [SkillCast.cpp:917](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:917).

### 573 — StaminaIncreaseStrengthener

Base route: 267. no registry visual; alternate path must be checked.
No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.
MuMain symbols: `AT_SKILL_HP_UP_OURFORCES`.
MuMain source: [WSclient.cpp:4009](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:4009), [WSclient.cpp:5086](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5086), [WSclient.cpp:5110](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5110), [WSclient.cpp:5137](C:/MuOnline_Source/MuMain-main/src/source/Network/Server/WSclient.cpp:5137), [SkillCast.cpp:915](C:/MuOnline_Source/MuMain-main/src/source/GameLogic/Combat/SkillCast.cpp:915).

### 578 — DurabilityReduction1FistMaster

Base route: 578. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DurabilityReduction1FistMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 579 — IncreasePvPDefenseRate

Base route: 579. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreasePvPDefenseRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 580 — IncreaseMaximumSd

Base route: 580. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseMaximumSd`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 581 — IncreaseManaRecoveryRate

Base route: 581. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseManaRecoveryRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 582 — IncreasePoisonResistance

Base route: 582. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreasePoisonResistance`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 583 — DurabilityReduction2FistMaster

Base route: 583. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DurabilityReduction2FistMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 584 — IncreaseSdRecoveryRate

Base route: 584. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseSdRecoveryRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 585 — IncreaseHpRecoveryRate

Base route: 585. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseHpRecoveryRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 586 — IncreaseLightningResistance

Base route: 586. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseLightningResistance`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 587 — IncreasesDefense

Base route: 587. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreasesDefense`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 588 — IncreasesAgRecoveryRate

Base route: 588. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreasesAgRecoveryRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 589 — IncreaseIceResistance

Base route: 589. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseIceResistance`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 590 — DurabilityReduction3FistMaster

Base route: 590. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DurabilityReduction3FistMaster`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 591 — IncreaseDefenseSuccessRate

Base route: 591. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseDefenseSuccessRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 592 — CastInvincibilityFistMaster

Base route: 592. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 599 — IncreaseAttackSuccessRate

Base route: 599. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseAttackSuccessRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 600 — IncreaseMaximumHp

Base route: 600. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseMaximumHp`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 601 — IncreaseMaximumMana

Base route: 601. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseMaximumMana`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 602 — IncreaseMaximumAg

Base route: 602. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 603 — IncreasePvPAttackRate

Base route: 603. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreasePvPAttackRate`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 604 — DecreaseMana

Base route: 604. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_DecreaseMana`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 605 — RecoverSDfromMonsterKills

Base route: 605. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_RecoverSDfromMonsterKills`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 606 — RecoverHPfromMonsterKills

Base route: 606. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_RecoverHPfromMonsterKills`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 607 — IncreaseMinimumAttackPower

Base route: 607. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_IncreaseMinimumAttackPower`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 608 — RecoverManaMonsterKills

Base route: 608. passive; no cast visual expected.
MuMain symbols: `AT_SKILL_RecoverManaMonsterKills`, `AT_SKILL_MASTER_END`.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 609 — IncreaseMaximumAttackPower

Base route: 609. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 610 — IncreasesCritDamageChance

Base route: 610. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 611 — RecoverManaFully

Base route: 611. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 612 — RecoversHpFully

Base route: 612. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 613 — IncreaseExcDamageChance

Base route: 613. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 614 — IncreaseDoubleDamageChance

Base route: 614. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 615 — IncreaseIgnoreDefChance

Base route: 615. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 616 — RecoversSdFully

Base route: 616. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..

### 617 — IncreaseTripleDamageChance

Base route: 617. passive; no cast visual expected.
MuMain source: No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity..
