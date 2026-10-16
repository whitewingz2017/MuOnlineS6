# Mobile target display and persistent labels

MobileTargetControl renders the selected monster/player name above a fixed top-center health bar. GameScene selects tapped players outside safe zones independently of Normal/PK attack permissions; normal attacks and targeted/area skill requests also update the selected target. Dead, hidden, removed, and previous-world targets do not draw.

PetHealthBarRenderer loads Interface/newui_Pet_HpBar.OZJ through the shared texture cache and uses it for target and overhead monster bars. Overhead monster names and HP frames are persistent for living visible monsters. Player and NPC names are persistent and are queued/flushed together after the existing world nameplate pass. Dropped-item behavior is unchanged.

Monster HP comes from existing ObjectHit fractions. The same packet now updates remote player health, which was previously discarded. Local player HP and party health can also supply a known fraction. Unknown remote health is shown as HP: -- in the selected-target display; missing data does not overwrite a known value or invent a percentage.

Validation: WindowsDX Debug build passed. Offline player captures exercised 72%, 35%, and 15% HP; null-update retention passed. Offline Giant capture checked top-center targeting and its overhead HP texture. Preview: Publish/MobileMonsterTargetPreview/skill-0-frame-064.png. Live tablet/server combat has not been tested; no Android APK built.
