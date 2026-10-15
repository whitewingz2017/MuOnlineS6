using System;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Client;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Core.Utilities;

public readonly record struct ItemRequirements(int Level, int Strength, int Agility, int Vitality, int Energy, int Command);

/// <summary>MuMain ZzzInfomation.cpp CalcRequirements/SetItemAttributes and NewUIMyInventory::IsEquipable.</summary>
public static class MuMainItemRules
{
    public static ItemRequirements Requirements(InventoryItem item)
    {
        var d = item.Definition;
        var o = item.Details;
        if (ServerItemRequirements.Profiles.TryGetValue((d.Group, d.Id), out var profile))
        {
            int serverBasis = profile.DropLevel + (o.IsAncient ? 30 : o.IsExcellent ? 25 : 0);
            int Requirement(int index)
            {
                int raw = profile.Values[index];
                if (raw == 0 || !profile.Wearable || (profile.Mapped & (1 << index)) == 0)
                    return raw;
                if (index == 4 && profile.Book)
                    return 20 + raw * (serverBasis + o.Level) * 3 / 100;
                int value = 20 + raw * (serverBasis + o.Level * 3) * (index == 4 ? 4 : 3) / 100;
                if (index == 1) value += o.OptionLevel * 4;
                return value;
            }

            int serverStrength = Requirement(1), serverAgility = Requirement(2);
            if (profile.Wearable)
                ApplyRequirementReductions(item, ref serverStrength, ref serverAgility);
            return new(Requirement(0), serverStrength, serverAgility, Requirement(3), Requirement(4), Requirement(5));
        }
        return MuMainReferenceRequirements(item);
    }

    public static ItemRequirements MuMainReferenceRequirements(InventoryItem item)
    {
        var d = item.Definition;
        var o = item.Details;
        int basis = d.DropLevel + (o.IsExcellent ? 25 : o.IsAncient ? 30 : 0);
        int Stat(int value, int factor = 3) => value == 0 ? 0 : 20 + value * (basis + o.Level * 3) * factor / 100;
        int strength = Stat(d.RequiredStrength), agility = Stat(d.RequiredDexterity);
        if (d.Group < 12 && !(d.Group == 4 && d.Id is 7 or 15))
            strength += o.OptionLevel * 5;
        int energy = Stat(d.RequiredEnergy, 4);
        if (d.RequiredEnergy > 0 && MuMainItemMetadata.IsSummonerBook(d))
            energy = 20 + d.RequiredEnergy * (basis + o.Level) * 3 / 100;
        else if (d.RequiredEnergy > 0 && d.Group == 15 && d.RequiredLevel > 0)
            energy = 20 + d.RequiredEnergy * d.RequiredLevel * 4 / 100;
        // OpenMU uses raw requirements for consumable scrolls, unlike wearable
        // items. Applying the equipment formula understates Power Wave (56 -> 40).
        if (d.Group == 15)
            energy = d.RequiredEnergy;
        if (d.Group == 12 && d.Id == 11)
            energy = o.Level switch { 0 => 30, 1 => 60, 2 => 90, 3 => 130, 4 => 170, 5 => 210, 6 => 300, 7 => 500, _ => 0 };

        int level = 0;
        if (d.RequiredLevel > 0)
        {
            bool fixedLevel = d.Group < 12 || (d.Group == 13 && d.Id == 37)
                || (d.Group == 12 && d.Id >= 7 && d.Id <= 40);
            bool scales = (d.Group == 12 && (d.Id <= 7 || d.Id is 41 or 42 or 49)) || d.Group >= 13;
            level = fixedLevel ? d.RequiredLevel : scales ? d.RequiredLevel + o.Level * (MuMainItemMetadata.IsSecondWing(d) ? 5 : 4) : 0;
        }
        if (d.Group == 13 && d.Id == 10) level = o.Level <= 2 ? 20 : 50;
        bool excellentLevelExempt = (d.Group is >= 7 and <= 11 && (d.Id is >= 29 and <= 33 || d.Id == 43 || d.Id is >= 62 and <= 72))
            || (d.Group == 0 && d.Id is 22 or 23) || (d.Group == 2 && d.Id == 14)
            || (d.Group == 4 && d.Id == 21) || (d.Group == 5 && d.Id is 12 or 19);
        if (o.IsExcellent && level > 0 && !excellentLevelExempt && !MuMainItemMetadata.IsWing(d) && !(d.Group == 13 && d.Id == 37))
            level += 20;
        // MuMain replaces the calculated Charisma requirement with the BMD value.
        int command = d.RequiredCommand;
        ApplyRequirementReductions(item, ref strength, ref agility);
        return new(level, strength, agility, Stat(d.RequiredVitality), energy, command);
    }

    private static void ApplyRequirementReductions(InventoryItem item, ref int strength, ref int agility)
    {
        var d = item.Definition;
        var o = item.Details;
        int harmonyLevel = o.HarmonyOption & 15, option = o.HarmonyOption >> 4;
        int category = d.Group < 5 ? 0 : d.Group == 5 ? 1 : 2;
        if (harmonyLevel < 14 && o.Level >= harmonyLevel && option > 0 && category < 2)
        {
            int reduction = MuMainItemOptionMetadata.HarmonyReduction(category, option, harmonyLevel);
            if (option == (category == 0 ? 3 : 2)) strength -= reduction;
            if (option == (category == 0 ? 4 : 3)) agility -= reduction;
        }
        if (o.SocketCount > 0 && ItemDataParser.TryParseExtendedItemData(item.RawData, out var ext))
        {
            var sockets = item.RawData.AsSpan(ext.Length - ext.SocketCount, ext.SocketCount);
            foreach (byte socket in sockets)
            {
                if (socket >= 254) continue;
                int seed = socket % 50, sphere = socket / 50;
                if (sphere >= 5) continue;
                if (seed == 38) strength -= MuMainItemOptionMetadata.SocketStrength[sphere];
                if (seed == 39) agility -= MuMainItemOptionMetadata.SocketDexterity[sphere];
            }
        }
        strength = Math.Max(0, strength);
        agility = Math.Max(0, agility);
    }

    public static (int Family, int Step) ClassJob(CharacterClassNumber c) => c switch
    {
        CharacterClassNumber.DarkWizard => (0, 1), CharacterClassNumber.SoulMaster => (0, 2), CharacterClassNumber.GrandMaster => (0, 3),
        CharacterClassNumber.DarkKnight => (1, 1), CharacterClassNumber.BladeKnight => (1, 2), CharacterClassNumber.BladeMaster => (1, 3),
        CharacterClassNumber.FairyElf => (2, 1), CharacterClassNumber.MuseElf => (2, 2), CharacterClassNumber.HighElf => (2, 3),
        CharacterClassNumber.MagicGladiator => (3, 1), CharacterClassNumber.DuelMaster => (3, 3),
        CharacterClassNumber.DarkLord => (4, 1), CharacterClassNumber.LordEmperor => (4, 3),
        CharacterClassNumber.Summoner => (5, 1), CharacterClassNumber.BloodySummoner => (5, 2), CharacterClassNumber.DimensionMaster => (5, 3),
        CharacterClassNumber.RageFighter => (6, 1), CharacterClassNumber.FistMaster => (6, 3), _ => (-1, 0),
    };

    public static bool CanUseClass(ItemDefinition d, CharacterClassNumber c)
    {
        var (family, step) = ClassJob(c);
        var classes = d.ClassRequirements;
        if (family < 0 || family >= classes.Length) return false;
        int requirement = classes[family];
        return requirement > 0 && requirement <= step
            || family == 3 && requirement == 0 && classes[0] > 0 && classes[1] > 0;
    }

    public static string RequiredClassName(int family, int step) => (family, step) switch
    {
        (0, 2) => "Soul Master", (0, 3) => "Grand Master", (1, 2) => "Blade Knight", (1, 3) => "Blade Master",
        (2, 2) => "Muse Elf", (2, 3) => "High Elf", (3, > 1) => "Duel Master", (4, > 1) => "Lord Emperor",
        (5, 2) => "Bloody Summoner", (5, 3) => "Dimension Master", (6, > 1) => "Fist Master",
        _ => family switch { 0 => "Dark Wizard", 1 => "Dark Knight", 2 => "Fairy Elf", 3 => "Magic Gladiator", 4 => "Dark Lord", 5 => "Summoner", 6 => "Rage Fighter", 7 => "Grow Lancer", 8 => "Rune Wizard", 9 => "Slayer", 10 => "Gun Crusher", _ => "Unknown" },
    };

    public static bool MeetsRequirements(InventoryItem item, CharacterState state)
    {
        if (state == null || !CanUseClass(item.Definition, state.Class)) return false;
        var r = Requirements(item);
        return state.Level >= r.Level && state.TotalStrength >= r.Strength && state.TotalAgility >= r.Agility
            && state.TotalVitality >= r.Vitality && state.TotalEnergy >= r.Energy && state.TotalLeadership >= r.Command;
    }
}
