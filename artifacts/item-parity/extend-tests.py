from pathlib import Path
p=Path('tests/ItemAttributes/Program.cs');s=p.read_text();s=s.replace('Console.WriteLine($"PASS:', '''if (args.Length > 0)
{
    foreach (string file in Directory.EnumerateFiles(Path.Combine(args[0], "src/bin/Data/Items/Models"), "Group*.json"))
    {
        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
        int group = json.RootElement.GetProperty("group").GetInt32();
        foreach (var model in json.RootElement.GetProperty("models").EnumerateArray())
        {
            var expected = new Vector3(270, -10, 0);
            if (model.TryGetProperty("inventory", out var display) && display.TryGetProperty("rotation", out var rotation))
                expected = new Vector3(rotation[0].GetSingle(), rotation[1].GetSingle(), rotation[2].GetSingle());
            var definition = new ItemDefinition(model.GetProperty("number").GetInt32(), "Source comparison", 1, 1) { Group = group };
            Check((Vector3)rotate.Invoke(null, [definition])! == expected, $"Model rotation mismatch for {group}:{definition.Id} in {file}.");
        }
    }
}
Console.WriteLine($"PASS:''')
p.write_text(s)
