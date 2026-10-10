from pathlib import Path
p=Path('Client.Main/Controls/UI/Game/Common/ItemUiHelper.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('dmgMax += baseMin * 25 / dropLevel + 5;', 'dmgMax += baseMax * 25 / dropLevel + 5;').replace('dmgMin += level * 3;', 'dmgMin += Math.Min(9, level) * 3 + (level > 9 ? level - 6 : 0);').replace('dmgMax += level * 3;', 'dmgMax += Math.Min(9, level) * 3 + (level > 9 ? level - 6 : 0);').replace('defense += level * 3; // Non-shields: +level*3', 'defense += Math.Min(9, level) * 3 + (level > 9 ? level - 6 : 0);')
# The detailed overloads use all of the actual item's options.
start=s.index('            lines.Add((name, Color.White));')
end=s.index('            // Show "Right-click to use"',start)
s=s[:start]+'''            var def = item.Definition;
            var blue = new Color(128, 179, 255);
            var green = new Color(26, 255, 128);
            var yellow = new Color(255, 204, 26);
            Color nameColor = details.IsAncient ? blue : details.IsExcellent ? green
                : details.Level >= 7 || details.HasBlueOptions ? yellow : Color.White;
            lines.Add((name, nameColor));
            lines.Add((" ", Color.White));
            bool isStaff = def.Group == 5;
            var stats = MuMainItemStats.Calculate(item);
            if (stats.DamageMin > 0 || stats.DamageMax > 0)
                lines.Add(($"{(def.TwoHanded ? "Two-handed" : "One-handed")} Damage: {stats.DamageMin} ~ {stats.DamageMax}", Color.White));
            if (stats.Defense > 0) lines.Add(($"Defense: {stats.Defense}", Color.White));
            if (stats.DefenseRate > 0) lines.Add(($"Defense Rate: {stats.DefenseRate}", Color.White));
            if (def.AttackSpeed > 0) lines.Add(($"Attack Speed: {def.AttackSpeed}", Color.White));
            int maxDurability = CalculateMaxDurability(def, details, isStaff);
            if (maxDurability > 0)
                lines.Add(($"Durability: {item.Durability}/{maxDurability}", Color.White));

            var requirements = MuMainItemRules.Requirements(item);
            void Requirement(string label, int required, int? available)
            {
                if (required <= 0) return;
                bool lacking = available.HasValue && available.Value < required;
                lines.Add(($"{label}: {required}", lacking ? Color.Red : Color.White));
                if (lacking) lines.Add(($"Lacking {required - available.Value}", Color.Red));
            }
            Requirement("Minimum Level", requirements.Level, characterState?.Level);
            Requirement("Required Strength", requirements.Strength, characterState?.TotalStrength);
            Requirement("Required Agility", requirements.Agility, characterState?.TotalAgility);
            Requirement("Required Vitality", requirements.Vitality, characterState?.TotalVitality);
            Requirement("Required Energy", requirements.Energy, characterState?.TotalEnergy);
            Requirement("Required Command", requirements.Command, characterState?.TotalLeadership);
            if (def.ClassRequirements.Length > 0)
            {
                lines.Add((" ", Color.White));
                var job = characterState == null ? (-1, 0) : MuMainItemRules.ClassJob(characterState.Class);
                for (int family = 0; family < Math.Min(7, def.ClassRequirements.Length); family++)
                {
                    int step = def.ClassRequirements[family];
                    if (step == 0) continue;
                    bool matches = job.Item1 == family && job.Item2 >= step;
                    lines.Add(($"Can be equipped by {MuMainItemRules.RequiredClassName(family, step)}", matches ? Color.White : new Color(178, 51, 26)));
                }
            }
            if (def.Group == 11 && details.Level >= 5) lines.Add(("Increases moving speed", blue));
            if (def.Group == 10 && details.Level >= 5) lines.Add(("Increases swimming speed", blue));
            if (stats.MagicPower > 0)
                lines.Add(($"{(MuMainItemStats.IsScepter(def) ? "Pet Attack" : MuMainItemMetadata.IsSummonerBook(def) ? "Curse Damage" : "Wizardry Damage")} Increase: {stats.MagicPower}%", blue));
            if (details.HasSkill && def.SkillIndex != 0)
                lines.Add(($"{SkillDatabase.GetSkillName(def.SkillIndex)} Skill", blue));
            if (details.HasLuck)
            {
                lines.Add(("Luck (success rate of Jewel of Soul +25%)", blue));
                lines.Add(("Luck (critical damage rate +5%)", blue));
            }
            foreach (string option in MuMainItemStats.AdditionalOptions(item)) lines.Add((option, blue));
            foreach (string option in MuMainItemStats.ExcellentOptions(item)) lines.Add((option, green));
            if (details.IsAncient)
                lines.Add(($"Ancient Option: +{details.AncientBonusOption}", blue));

'''+s[end:]
p.write_text(s,encoding='utf-8')
# Use source model rotations rather than guessing one per item group.
p=Path('Client.Main/Controls/UI/Game/Inventory/BmdPreviewRenderer.cs');s=p.read_text(encoding='utf-8-sig')
a=s.index('            short group = (short)definition.Group;',s.index('public static Matrix GetInventoryBaseRotation'))
b=s.index('\n        public static Quaternion',a)
s=s[:a]+'''            Vector3 angles = MuMainItemMetadata.InventoryRotation(definition);
            return MuRotationConverter.ConvertToMonoGame(angles.X, angles.Y, angles.Z);
        }
'''+s[b:]
p.write_text(s,encoding='utf-8')
# Check requirements before changing the equipment optimistically; same-slot restoration remains allowed.
p=Path('Client.Main/Controls/UI/Game/Inventory/InventoryControl.cs');s=p.read_text(encoding='utf-8-sig')
a=s.index('            _equippedItems[toSlot] = itemToPlace;',s.index('// If moving to the same slot'))
a=s.index('            _equippedItems[toSlot] = itemToPlace;',a+1)
s=s[:a]+'''            var equipState = _networkManager?.GetCharacterState();
            if (equipState != null && !MuMainItemRules.MeetsRequirements(itemToPlace, equipState))
                return false;

'''+s[a:]
# Match MuMain's black tooltip with a thin white outline and actual rarity-colored title.
a=s.index('            // Drop shadow',s.index('private void DrawTooltip('));b=s.index('            // ═',a+20)
# Keep text rendering below, replace the decorative panel/border section.
b=s.index('            int textY =',a)
s=s[:a]+'''            spriteBatch.Draw(pixel, tooltipRect, Color.Black * 0.8f);
            Color borderColor = Color.White;
            const int borderThickness = 1;
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Y, tooltipRect.Width, borderThickness), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Bottom - borderThickness, tooltipRect.Width, borderThickness), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Y, borderThickness, tooltipRect.Height), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.Right - borderThickness, tooltipRect.Y, borderThickness, tooltipRect.Height), borderColor);

'''+s[b:]
s=s.replace('Color lineColor = isFirstLine ? borderColor : color;', 'Color lineColor = color;')
# Guard oversized tooltips from a negative Math.Clamp maximum.
s=s.replace('screenBounds.Right - tooltipRect.Width - 10)', 'Math.Max(10, screenBounds.Right - tooltipRect.Width - 10))').replace('screenBounds.Bottom - tooltipRect.Height - 10)', 'Math.Max(10, screenBounds.Bottom - tooltipRect.Height - 10))')
p.write_text(s,encoding='utf-8')
