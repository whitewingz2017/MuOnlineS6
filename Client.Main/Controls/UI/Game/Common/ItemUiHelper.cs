using System;
using System.Collections.Generic;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Controls.UI.Game.Inventory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Client.Main.Content;
using Client.Main;

namespace Client.Main.Controls.UI.Game.Common
{
    public readonly struct ItemGlowPalette
    {
        public ItemGlowPalette(Color normal, Color magic, Color excellent, Color ancient, Color legendary)
        {
            Normal = normal;
            Magic = magic;
            Excellent = excellent;
            Ancient = ancient;
            Legendary = legendary;
        }

        public Color Normal { get; }
        public Color Magic { get; }
        public Color Excellent { get; }
        public Color Ancient { get; }
        public Color Legendary { get; }
    }

    public static class ItemUiHelper
    {
        public static Rectangle GetTooltipBounds(Rectangle itemRect, Point size, Rectangle screen)
        {
            const int margin = 10;
            const int gap = 8;
            int x = itemRect.Center.X - size.X / 2;
            int above = itemRect.Top - gap - size.Y;
            int below = itemRect.Bottom + gap;
            int y = above >= screen.Top + margin ? above : below;
            if (y + size.Y > screen.Bottom - margin)
                y = itemRect.Top - screen.Top > screen.Bottom - itemRect.Bottom ? above : below;
            return new Rectangle(
                Math.Clamp(x, screen.Left + margin, Math.Max(screen.Left + margin, screen.Right - size.X - margin)),
                Math.Clamp(y, screen.Top + margin, Math.Max(screen.Top + margin, screen.Bottom - size.Y - margin)),
                size.X, size.Y);
        }

        public static float FitTooltipScale(SpriteFont font, List<(string text, Color color)> lines, float scale, Point screen)
        {
            // Keep even long ancient/socket tooltips inside the UI viewport.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                float width = 0;
                int height = 0;
                foreach (var (text, _) in lines)
                {
                    width = Math.Max(width, font.MeasureString(text).X * scale);
                    height += TooltipLineHeight(font, text, scale);
                }
                float fit = Math.Min((screen.X - 28f) / Math.Max(1, width),
                                     (screen.Y - 28f) / Math.Max(1, height));
                if (fit >= 1f) break;
                scale *= Math.Max(0.01f, fit * 0.98f);
            }
            return scale;
        }

        public static bool ShowsItemCount(InventoryItem item) => item.Definition.Group switch
        {
            4 => item.Definition.Id is 7 or 15, // Arrow and bolt ammunition.
            14 => item.Definition.Id is >= 0 and <= 8 or >= 35 and <= 40 or >= 46 and <= 50
                or 70 or 71 or >= 78 and <= 82 or >= 88 and <= 90 or 94 or 100 or 133
                or >= 153 and <= 156 || (item.Definition.Id == 21 && item.Details.Level == 3),
            _ => false,
        };

        public static int TooltipLineHeight(SpriteFont font, string text, float scale) =>
            (int)MathF.Ceiling(font.MeasureString("Q").Y * scale * (string.IsNullOrWhiteSpace(text) ? 0.55f : 1.1f));
        /// <summary>
        /// Calculates max durability based on item level, excellent, and ancient options.
        /// Formula from ZzzInventory.cpp:CalcMaxDurability()
        /// </summary>
        public static int CalculateMaxDurability(ItemDefinition def, ItemDatabase.ItemDetails details, bool isStaff)
        {
            // Start with base durability (staffs use MagicDurability, others use BaseDurability)
            int maxDur = isStaff ? def.MagicDurability : def.BaseDurability;

            // Add level bonuses
            int level = details.Level;
            for (int i = 0; i < level; i++)
            {
                if (def.Group * 512 + def.Id >= 13 * 512 + 51) break;
                if (i >= 14)      // Level 15
                    maxDur = Math.Min(maxDur + 8, 255);
                else if (i >= 13) // Level 14
                    maxDur = Math.Min(maxDur + 7, 255);
                else if (i >= 12) // Level 13
                    maxDur += 6;
                else if (i >= 11) // Level 12
                    maxDur += 5;
                else if (i >= 10) // Level 11
                    maxDur += 4;
                else if (i >= 9)  // Level 10
                    maxDur += 3;
                else if (i >= 4)  // Level 5-9
                    maxDur += 2;
                else              // Level 1-4
                    maxDur++;
            }

            if (def.Group == 13 && def.Id is 4 or 5) maxDur = 255;
            // Ancient items: +20 durability
            if (details.IsAncient)
            {
                maxDur += 20;
            }
            // Excellent items (non-wings): +15 durability
            else if (details.IsExcellent && !MuMainItemMetadata.IsArchangel(def)
                && !(def.Group == 13 && def.Id == 30)
                && !(def.Group == 12 && (def.Id is >= 3 and <= 6 or >= 36 and <= 40 or 42 or 43 or 49 or 50)))
            {
                maxDur += 15;
            }

            if (def.Group is >= 7 and <= 11 && def.Id is >= 62 and <= 72) maxDur = 255;
            return maxDur;
        }

        /// <summary>
        /// Calculates damage with level bonus.
        /// Formula from ZzzInfomation.cpp (line 501-514)
        /// </summary>
        public static (int min, int max) CalculateDamageWithLevel(int baseMin, int baseMax, int level, bool isExcellent, int dropLevel)
        {
            int dmgMin = baseMin;
            int dmgMax = baseMax;

            if (baseMin > 0)
            {
                if (isExcellent && dropLevel > 0)
                    dmgMin += baseMin * 25 / dropLevel + 5;
                dmgMin += Math.Min(9, level) * 3 + (level > 9 ? level - 6 : 0);
            }

            if (baseMax > 0)
            {
                if (isExcellent && dropLevel > 0)
                    dmgMax += baseMax * 25 / dropLevel + 5;
                dmgMax += Math.Min(9, level) * 3 + (level > 9 ? level - 6 : 0);
            }

            return (dmgMin, dmgMax);
        }

        /// <summary>
        /// Calculates defense with level bonus.
        /// Formula from ZzzInfomation.cpp (line 516-530)
        /// </summary>
        public static int CalculateDefenseWithLevel(int baseDefense, int level, bool isShield, bool isExcellent, int dropLevel)
        {
            int defense = baseDefense;

            if (defense > 0)
            {
                if (isShield)
                {
                    defense += level; // Shields: +level
                }
                else
                {
                    if (isExcellent && dropLevel > 0)
                        defense += baseDefense * 12 / dropLevel + 4 + dropLevel / 5;
                    defense += Math.Min(9, level) * 3 + (level > 9 ? level - 6 : 0);
                }
            }

            return defense;
        }

        /// <summary>
        /// Calculates requirements with level and excellent bonuses.
        /// Formula from ZzzInfomation.cpp (line 540-551)
        /// </summary>
        public static int CalculateRequirement(int baseRequirement, int level, bool isExcellent, int dropLevel)
        {
            if (baseRequirement == 0) return 0;

            int itemLevel = dropLevel;
            if (isExcellent)
                itemLevel += 25; // Excellent adds +25 to ItemLevel for requirements

            return 20 + baseRequirement * (itemLevel + level * 3) * 3 / 100;
        }

        public static Color GetItemGlowColor(InventoryItem item, ItemGlowPalette palette)
        {
            if (item?.Details is null)
                return palette.Normal;

            if (item.Details.IsExcellent) return palette.Excellent;
            if (item.Details.IsAncient) return palette.Ancient;
            if (item.Details.Level >= 9) return palette.Legendary;
            if (item.Details.Level >= 5) return palette.Magic;
            return palette.Normal;
        }

        public static void DrawItemGlow(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int glowSize = 4)
        {
            if (spriteBatch == null || pixel == null) return;

            for (int i = glowSize; i > 0; i--)
            {
                float alpha = (float)(glowSize - i + 1) / glowSize * 0.6f;
                Color layerColor = color * alpha;

                var glowRect = new Rectangle(rect.X - i, rect.Y - i, rect.Width + i * 2, rect.Height + i * 2);

                spriteBatch.Draw(pixel, new Rectangle(glowRect.X, glowRect.Y, glowRect.Width, 1), layerColor);
                spriteBatch.Draw(pixel, new Rectangle(glowRect.X, glowRect.Bottom - 1, glowRect.Width, 1), layerColor);
                spriteBatch.Draw(pixel, new Rectangle(glowRect.X, glowRect.Y, 1, glowRect.Height), layerColor);
                spriteBatch.Draw(pixel, new Rectangle(glowRect.Right - 1, glowRect.Y, 1, glowRect.Height), layerColor);
            }
        }

        public static List<(string text, Color color)> BuildTooltipLines(InventoryItem item)
        {
            var lines = new List<(string, Color)>();
            if (item?.Definition == null || item?.Details is null)
                return lines;

            var details = item.Details;

            var characterState = MuGame.Network?.GetCharacterState();

            string name = details.IsExcellent && !MuMainItemMetadata.IsWing(item.Definition) ? $"Excellent {item.Definition.Name}"
                        : details.IsAncient ? $"{MuMainAncientNames.Get(item.Definition.Group, item.Definition.Id, details.AncientDiscriminator) ?? "Ancient"} {item.Definition.Name}"
                        : item.Definition.Name;

            if (details.Level > 0)
                name += $" +{details.Level}";

            var def = item.Definition;
            var blue = new Color(128, 179, 255);
            var green = new Color(26, 255, 128);
            var yellow = new Color(255, 204, 26);
            Color nameColor = details.IsAncient ? new Color(0, 255, 204) : details.IsExcellent ? green
                : details.Level >= 7 ? yellow : details.HasBlueOptions ? blue : Color.White;
            if (details.SocketCount > 0 && !details.IsAncient) nameColor = new Color(204, 128, 255);
            if (MuMainItemMetadata.IsWing(def)) nameColor = details.Level >= 7 ? yellow
                : details.HasBlueOptions || details.IsExcellent ? blue : Color.White;
            if (MuMainItemMetadata.IsArchangel(def)) nameColor = new Color(255, 26, 255);
            lines.Add((name, nameColor));
            lines.Add((" ", Color.White));
            bool isStaff = def.Group == 5;
            var stats = MuMainItemStats.Calculate(item);
            if (stats.DamageMin > 0 || stats.DamageMax > 0)
                lines.Add(($"{(def.TwoHanded ? "Two-handed" : "One-handed")} Damage: {stats.DamageMin} ~ {stats.DamageMax}", details.IsExcellent ? blue : Color.White));
            if (stats.Defense > 0) lines.Add(($"Defense: {stats.Defense}", details.IsExcellent && def.Group is >= 7 and <= 11 ? blue : Color.White));
            if (stats.DefenseRate > 0) lines.Add(($"Defense Rate: {stats.DefenseRate}", details.IsExcellent ? blue : Color.White));
            if (def.WalkSpeed > 0) lines.Add(($"Movement Speed: {def.WalkSpeed}", Color.White));
            if (def.AttackSpeed > 0) lines.Add(($"Attack Speed: {def.AttackSpeed}", Color.White));
            int maxDurability = CalculateMaxDurability(def, details, isStaff);
            if (ShowsItemCount(item))
                lines.Add(($"Number of items: {item.Durability}", def.Group == 14 &&
                    (def.Id is 70 or 71 or >= 78 and <= 82 or >= 88 and <= 90 or 94 or 133) ? blue : Color.White));
            else if ((isStaff ? def.MagicDurability : def.BaseDurability) > 0)
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
            if (details.IsAncient && details.AncientBonusOption > 0 && def.SetAttribute is >= 0 and <= 3)
            {
                string attribute = def.SetAttribute switch { 0 => "Strength", 1 => "Agility", 2 => "Energy", _ => "Vitality" };
                lines.Add(($"{attribute} Increase +{details.AncientBonusOption * 5}", blue));
            }

            int harmonyCategory = def.Group < 5 ? 0 : def.Group == 5 ? 1 : def.Group < 12 ? 2 : -1;
            var harmony = MuMainItemOptionMetadata.GetHarmony(harmonyCategory, details.HarmonyOption >> 4);
            int harmonyLevel = details.HarmonyOption & 15;
            if (harmony != null && harmony.Name.Length > 0 && harmonyLevel < 14)
                lines.Add(($"{harmony.Name} +{harmony.Values[harmonyLevel]}", details.Level >= harmonyLevel ? yellow : Color.Gray));

            // Show "Right-click to use" for consumables (potions, scrolls), but not for jewels
            if (def.IsConsumable() && !def.IsJewel())
                lines.Add((Client.Main.Controls.UI.UiThemeManager.IsMobilePlatform
                    ? "Tap again for Use / Drop" : "Right-click to use", new Color(255, 215, 0)));

            // Add repair cost if in repair mode
            var npcShop = NpcShopControl.Instance;
            if (npcShop != null && npcShop.Visible && npcShop.IsRepairMode)
            {
                if (Core.Utilities.ItemPriceCalculator.IsRepairable(item))
                {
                    int repairCost = Core.Utilities.ItemPriceCalculator.CalculateRepairPrice(item, npcDiscount: true);
                    if (repairCost > 0 && item.Durability < maxDurability)
                    {
                        lines.Add(($"Repair Cost: {repairCost} Zen", new Color(212, 175, 85)));
                    }
                }
                else
                {
                    lines.Add(("Cannot be repaired", new Color(255, 100, 100)));
                }
            }

            return lines;
        }
    }
}
