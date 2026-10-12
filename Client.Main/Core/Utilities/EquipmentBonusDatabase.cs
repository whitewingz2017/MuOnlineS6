using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Client.Main.Core.Utilities;

/// <summary>Season 6 bonus descriptions exported from MuMain's English data files.</summary>
public static class EquipmentBonusDatabase
{
    public sealed class Data
    {
        public Dictionary<string, int[]> Types { get; set; } = new();
        public List<Set> Sets { get; set; } = new();
        public List<Socket> Sockets { get; set; } = new();
    }
    public sealed class Set
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public int[] Classes { get; set; } = Array.Empty<int>();
        public List<Option> Options { get; set; } = new();
    }
    public sealed class Option
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public int Pieces { get; set; }
    }
    public sealed class Socket
    {
        public int Group { get; set; }
        public int Index { get; set; }
        public int Category { get; set; }
        public string Name { get; set; } = "";
        public int Type { get; set; }
        public int[] Values { get; set; } = Array.Empty<int>();
        public int[] Required { get; set; } = Array.Empty<int>();
    }
    private static readonly Lazy<Data> Database = new(() =>
    {
        using var stream = typeof(EquipmentBonusDatabase).Assembly.GetManifestResourceStream("Client.Main.Data.equipment_bonuses.json");
        return stream == null ? new Data() : JsonSerializer.Deserialize<Data>(stream) ?? new Data();
    });
    private static readonly string[] SetOptionNames =
    {
        "Strength", "Agility", "Energy", "Vitality", "Command", "Minimum damage", "Maximum damage",
        "Wizardry damage", "Damage", "Attack success rate", "Defense", "Maximum HP", "Maximum MP",
        "Maximum AG", "AG recovery", "Critical damage rate %", "Critical damage", "Excellent damage rate %",
        "Excellent damage", "Skill damage", "Double damage rate %", "Ignore defense rate %",
        "Shield defense %", "Two-handed sword damage %", "Skill mastery", "Attack", "Attack",
        "Defense", "Defense", "Wizardry", "Ice mastery", "Poison mastery", "Lightning mastery",
        "Fire mastery", "Earth mastery", "Wind mastery", "Water mastery"
    };

    public static string DescribeSets(IEnumerable<InventoryItem> equipment, global::Client.Main.Core.Client.CharacterState character = null)
    {
        var groups = new Dictionary<int, HashSet<int>>();
        foreach (var item in equipment)
        {
            if (item.Durability == 0 || !item.Details.IsAncient || item.Details.AncientDiscriminator is < 1 or > 2) continue;
            if (!ItemDatabase.TryGetItemGroupAndNumber(item.RawData, out byte group, out short number)) continue;
            int id = group * 512 + number;
            if (!Database.Value.Types.TryGetValue(id.ToString(), out int[] types)) continue;
            int setId = types[item.Details.AncientDiscriminator - 1];
            if (!groups.TryGetValue(setId, out var members)) groups[setId] = members = new();
            members.Add(id); // Identical rings/weapons do not count twice.
        }
        var lines = new List<string> { "Ancient set options" };
        foreach (var group in groups)
        {
            var set = Database.Value.Sets.FirstOrDefault(s => s.Id == group.Key);
            if (set == null) continue;
            lines.Add($"{set.Name} ({group.Value.Count}/{set.Count} pieces)");
            foreach (var option in set.Options)
            {
                string name = option.Id < SetOptionNames.Length ? SetOptionNames[option.Id] : $"Option {option.Id}";
                bool classAllowed = true;
                if (character != null && option.Id > 23)
                {
                    var job = MuMainItemRules.ClassJob(character.Class);
                    classAllowed = job.Family >= 0 && job.Family < set.Classes.Length && set.Classes[job.Family] > 0 && job.Step >= set.Classes[job.Family];
                }
                string status = !classAllowed ? "Other class" : group.Value.Count >= option.Pieces ? "Active" : $"{option.Pieces} pieces";
                lines.Add($"{status}: {name} +{option.Value}");
            }
        }
        if (lines.Count == 1) lines.Add("No ancient set pieces equipped.");
        return string.Join('\n', lines);
    }

    public static byte[] GetSocketBytes(byte[] raw)
    {
        if (ItemDataParser.TryParseExtendedItemData(raw, out var item) && item.Length <= raw.Length)
            return item.HasSockets ? raw.AsSpan(item.Length - item.SocketCount, item.SocketCount).ToArray() : Array.Empty<byte>();
        return raw.Length >= 12 ? raw.AsSpan(7, 5).ToArray() : Array.Empty<byte>();
    }

    public static string DescribeSocket(byte value)
    {
        if (value == 255) return "Empty socket";
        if (value == 254) return "No socket";
        int index = value % 50;
        int sphere = value / 50;
        var option = Database.Value.Sockets.FirstOrDefault(s => s.Group == 0 && s.Index == index);
        if (option == null || sphere >= option.Values.Length) return $"Unknown socket ({value})";
        string suffix = option.Type switch { 2 => $"+{option.Values[sphere]}%", 3 => $"level/{option.Values[sphere]}", 4 => $"maximum HP/{option.Values[sphere]}", 5 => $"maximum MP/{option.Values[sphere]}", _ => $"+{option.Values[sphere]}" };
        return $"{option.Name} {suffix}";
    }

    public static string DescribeSockets(IEnumerable<InventoryItem> equipment)
    {
        var lines = new List<string> { "Equipped socket options" };
        var categories = new int[6];
        foreach (var item in equipment)
        {
            if (item.Durability == 0) continue;
            var seeds = GetSocketBytes(item.RawData).Where(seed => seed < 254).ToArray();
            if (seeds.Length == 0) continue;
            lines.Add(item.Definition.Name);
            foreach (byte seed in seeds)
            {
                lines.Add(DescribeSocket(seed));
                var option = Database.Value.Sockets.FirstOrDefault(s => s.Group == 0 && s.Index == seed % 50);
                if (option?.Category is >= 1 and <= 6) categories[option.Category - 1]++;
            }
            if (ItemDataParser.TryParseExtendedItemData(item.RawData, out var extended) && extended.HasSockets)
            {
                var mix = Database.Value.Sockets.FirstOrDefault(s => s.Group == 1 && s.Index == extended.SocketBonusOption);
                if (mix != null) lines.Add($"Item bonus: {mix.Name} +{mix.Values[0]}{(mix.Type == 2 ? "%" : "")}");
            }
        }
        if (lines.Count == 1) lines.Add("No socket seeds equipped.");
        foreach (var bonus in Database.Value.Sockets.Where(s => s.Group == 2))
        {
            if (bonus.Required.Length != 6 || !bonus.Required.Any(r => r > 0)) continue;
            bool active = bonus.Required.Select((required, i) => categories[i] >= required).All(met => met);
            lines.Add($"{(active ? "Active" : "Locked")} set bonus: {bonus.Name} +{bonus.Values[0]}{(bonus.Type == 2 ? "%" : "")}");
        }
        if (lines.Count == 1) lines.Add("No socket seeds equipped.");
        return string.Join('\n', lines);
    }
}
