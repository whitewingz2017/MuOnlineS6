using System;
using System.Collections.Generic;
using Client.Main.Controls.UI.Game.Inventory;

namespace Client.Main.Core.Utilities;

public readonly record struct ItemDisplayStats(int DamageMin, int DamageMax, int Defense, int DefenseRate, int MagicPower);

/// <summary>Display attributes and option interpretation from MuMain ZzzInfomation.cpp.</summary>
public static class MuMainItemStats
{
    public static bool IsScepter(ItemDefinition d) => d.Group == 2 && (d.Id is >= 8 and <= 15 or 17 or 18);

    public static ItemDisplayStats Calculate(InventoryItem item)
    {
        var d = item.Definition;
        var o = item.Details;
        int levelBonus = Math.Min(9, o.Level) * 3 + (o.Level > 9 ? o.Level - 6 : 0);
        int chaosBonus = (d.Group, d.Id) switch { (2, 6) => 15, (4, 6) => 30, (5, 7) => 25, _ => 0 };
        int Damage(int basis)
        {
            if (basis == 0) return 0;
            int value = basis;
            if (o.IsExcellent && d.DropLevel > 0) value += chaosBonus > 0 ? chaosBonus : basis * 25 / d.DropLevel + 5;
            if (o.IsAncient) value += 5 + (d.DropLevel + 30) / 40;
            return value + levelBonus;
        }
        int defense = d.Group == 13 && d.Id == 30 ? 15 : d.Defense;
        int baseDefense = defense, setLevel = d.DropLevel + 30;
        if (defense > 0)
        {
            if (d.Group == 6)
            {
                defense += o.Level;
                if (o.IsAncient) defense += defense * 20 / setLevel + 2;
            }
            else
            {
                if (o.IsExcellent && d.DropLevel > 0) defense += baseDefense * 12 / d.DropLevel + 4 + d.DropLevel / 5;
                if (o.IsAncient) defense += baseDefense + defense * 3 / setLevel + 2 + setLevel / 30;
                int multiplier = MuMainItemMetadata.IsSecondWing(d) || (d.Group == 13 && d.Id == 30) || (d.Group == 12 && d.Id == 49) ? 2
                    : MuMainItemMetadata.IsThirdWing(d) ? 4 : 3;
                defense += Math.Min(9, o.Level) * multiplier;
                if (o.Level > 9) defense += o.Level - (MuMainItemMetadata.IsThirdWing(d) ? 5 : 6);
            }
        }
        int blocking = d.DefenseRate;
        if (blocking > 0)
        {
            if (o.IsExcellent && d.DropLevel > 0) blocking += d.DefenseRate * 25 / d.DropLevel + 5;
            // The checked MuMain revision intentionally uses the base item's Level here.
            blocking += Math.Min(9, d.DropLevel) * 3 + (d.DropLevel > 9 ? d.DropLevel - 6 : 0);
        }
        int magic = d.MagicPower;
        if (magic > 0)
        {
            if (o.IsExcellent && d.DropLevel > 0) magic += chaosBonus > 0 ? chaosBonus : d.MagicPower * 25 / d.DropLevel + 5;
            if (o.IsAncient) magic += 2 + setLevel / 60;
            magic = (magic + levelBonus) / 2;
            if (!IsScepter(d)) magic += o.Level * 2;
        }
        int min = Damage(d.DamageMin), max = Damage(d.DamageMax);
        int category = d.Group < 5 ? 0 : d.Group == 5 ? 1 : d.Group < 12 ? 2 : -1;
        int option = o.HarmonyOption >> 4, harmonyLevel = o.HarmonyOption & 15;
        if (harmonyLevel < 14 && o.Level >= harmonyLevel)
        {
            int bonus = MuMainItemOptionMetadata.HarmonyReduction(category, option, harmonyLevel);
            if (category == 0 && option is 1 or 5) min += bonus;
            if (category == 0 && option is 2 or 5) max += bonus;
            if (category == 2 && option == 1) defense += bonus;
        }
        return new(Math.Min(min, max), max, defense, blocking, magic);
    }

    public static IEnumerable<string> AdditionalOptions(InventoryItem item)
    {
        var d = item.Definition;
        var o = item.Details;
        int n = o.OptionLevel;
        if (n == 0) yield break;
        if (d.Group < 5 && !(d.Group == 4 && d.Id is 7 or 15)) yield return $"Additional Damage +{n * 4}";
        else if (d.Group == 5) yield return $"Additional {(MuMainItemMetadata.IsSummonerBook(d) ? "Curse" : "Wizardry")} Damage +{n * 4}";
        else if (d.Group == 6) yield return $"Additional Defense Rate +{n * 5}";
        else if (d.Group is >= 7 and <= 11) yield return $"Additional Defense +{n * 4}";
        else if (d.Group == 13 && d.Id == 30) yield return $"Additional Damage +{n * 4}";
        else if (d.Group == 13 && d.Id >= 8)
            yield return d.Id switch { 24 => $"Maximum Mana Increase +{n}%", 28 => $"Maximum AG Increase +{n}%", _ => $"Automatic HP Recovery +{n}%" };
        else if (d.Group == 12)
        {
            string kind = (d.Id, o.OptionType) switch
            {
                (0, _) => "Recovery", (1 or 41, _) => "Wizardry Damage", (2, _) => "Damage",
                (3 or 5 or 49, 0) => "Recovery", (3 or 5 or 49, 2) => "Damage",
                (4, 0) => "Recovery", (4, 2) => "Wizardry Damage",
                (6, 0) => "Wizardry Damage", (6, 2) => "Damage",
                (42, 0) => "Curse Damage", (42, 2) => "Wizardry Damage",
                (36 or 37 or 38 or 39 or 40 or 43 or 50, 0) => "Recovery",
                (36 or 37 or 38 or 40 or 50, 2) => "Defense",
                (39, 2) => "Wizardry Damage", (43, 2) => "Curse Damage",
                (37 or 43, 3) => "Wizardry Damage", (36 or 38 or 39 or 40 or 50, 3) => "Damage", _ => null,
            };
            if (kind == "Recovery") yield return $"Automatic HP Recovery +{n}%";
            else if (kind != null) yield return $"Additional {kind} +{n * 4}";
        }
    }

    public static IEnumerable<string> ExcellentOptions(InventoryItem item)
    {
        var d = item.Definition;
        var o = item.Details;
        int flags = o.ExcellentFlags & 63;
        string[] options;
        if (MuMainItemMetadata.IsSecondWing(d))
            options = [$"Maximum HP Increase +{50 + o.Level * 5}", $"Maximum MP Increase +{50 + o.Level * 5}", "Ignore Enemy Defense 3%", "Maximum AG Increase +50", "Attack Speed Increase +5"];
        else if ((d.Group == 13 && d.Id == 30) || (d.Group == 12 && d.Id == 49))
            options = [$"Maximum HP Increase +{50 + o.Level * 5}", $"Maximum MP Increase +{50 + o.Level * 5}", "Ignore Enemy Defense 3%", d.Group == 13 ? $"Command Increase +{10 + o.Level * 5}" : ""];
        else if (MuMainItemMetadata.IsThirdWing(d))
            options = ["Ignore Enemy Defense 5%", "Reflect Damage 5%", "Full HP Recovery Chance 5%", "Full MP Recovery Chance 5%"];
        else if (d.Group is >= 6 and <= 11 || (d.Group == 13 && d.Id is 8 or 9 or >= 21 and <= 24))
            options = ["Zen After Hunt Increase +30%", "Defense Success Rate Increase +10%", "Reflect Damage +5%", "Damage Decrease +4%", "Maximum Mana Increase +4%", "Maximum Life Increase +4%"];
        else if (d.Group <= 5 || (d.Group == 13 && d.Id is 12 or 13 or >= 25 and <= 28))
        {
            bool magic = d.Group == 5 || (d.Group == 13 && d.Id is 12 or 25 or 27);
            string damage = magic ? "Wizardry Damage" : "Damage";
            options = ["Mana After Hunt +Mana/8", "Life After Hunt +Life/8", "Attack Speed Increase +7", $"{damage} Increase +2%", $"{damage} Increase +Level/20", "Excellent Damage Rate +10%"];
        }
        else yield break;
        // MuMain lists equipment excellent options from bit 5 down to bit 0; wings use ascending bits.
        bool wings = MuMainItemMetadata.IsWing(d);
        for (int n = 0; n < options.Length; n++)
        {
            int bit = wings ? n : options.Length - 1 - n;
            if ((flags & (1 << bit)) != 0 && options[bit].Length > 0) yield return options[bit];
        }
    }
}
