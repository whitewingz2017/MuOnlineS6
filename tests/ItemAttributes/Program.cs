using System.Reflection;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
using MUnique.OpenMU.Network.Packets;

int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
InventoryItem Item(int group = 0, int id = 0, int level = 0, byte flags = 0, params byte[] options)
{
    var definition = new ItemDefinition(id, "Reference item", 2, 3) { Group = group, DropLevel = 50,
        RequiredStrength = 100, RequiredDexterity = 80, RequiredEnergy = 50, RequiredVitality = 40, RequiredCommand = 30,
        DamageMin = 100, DamageMax = 200, Defense = 100, MagicPower = 100, ClassRequirements = [0, 1, 0, 0, 0, 0, 0] };
    byte[] raw = [(byte)(group << 4), (byte)id, (byte)level, 100, flags, ..options];
    return new InventoryItem(definition, Point.Zero, raw, 100, level);
}
var normal = Item();
Check(MuMainItemRules.Requirements(normal) == new ItemRequirements(0, 170, 140, 80, 120, 30), "Base requirement vector differs from MuMain.");
var excellent = Item(level: 15, flags: 9, options: [4, 32]);
Check(MuMainItemRules.Requirements(excellent) == new ItemRequirements(0, 400, 308, 164, 260, 30), "+15 excellent/additional-option requirements are wrong.");
var ancient = Item(level: 15, flags: 17, options: [4, 17]);
Check(MuMainItemRules.Requirements(ancient) == new ItemRequirements(0, 415, 320, 170, 270, 30), "Ancient +30 item level requirement bonus missing.");
Check(MuMainItemStats.Calculate(excellent) == new ItemDisplayStats(191, 341, 174, 0, 125), "+15 excellent damage/defense/magic vector differs from MuMain.");
Check(MuMainItemStats.Calculate(ancient).Defense == 243, "Ancient defense bonus differs from MuMain.");
Check(MuMainItemStats.Calculate(Item(6, level: 15, flags: 16, options: [1])).Defense == 145, "Ancient shield formula differs from MuMain.");
var staff = Item(5, level: 15, flags: 8, options: [32]);
Check(MuMainItemStats.Calculate(staff).DamageMax == 341 && MuMainItemStats.Calculate(staff).MagicPower == 125, "Physical staff damage was incorrectly replaced by magic damage.");
var chaos = Item(4, 6, 15, 8, 32);
Check(MuMainItemStats.Calculate(chaos).DamageMax == 266, "Chaos weapon excellent override missing.");
var scroll = Item(15); scroll.Definition.RequiredLevel = 50;
Check(MuMainItemRules.Requirements(scroll).Energy == 120, "Scroll energy must use required level and divisor 100.");
var book = Item(5, 21, 15);
Check(MuMainItemRules.Requirements(book).Energy == 117, "Summoner book energy formula incorrect.");
int[] summonEnergy = [30, 60, 90, 130, 170, 210, 300, 500];
for (int level = 0; level < 8; level++) Check(MuMainItemRules.Requirements(Item(12, 11, level)).Energy == summonEnergy[level], "Summoning orb energy table mismatch.");
var wing2 = Item(12, 3, 15); wing2.Definition.RequiredLevel = 180; wing2.Definition.Defense = 30;
Check(MuMainItemRules.Requirements(wing2).Level == 255 && MuMainItemStats.Calculate(wing2).Defense == 57, "Second-tier wing upgrade formula mismatch.");
var wing3 = Item(12, 36, 15); wing3.Definition.RequiredLevel = 180; wing3.Definition.Defense = 40;
Check(MuMainItemRules.Requirements(wing3).Level == 180 && MuMainItemStats.Calculate(wing3).Defense == 86, "Third-tier wing formula mismatch.");
var fenrir = Item(13, 37, 15, 8, 32); fenrir.Definition.RequiredLevel = 300;
Check(MuMainItemRules.Requirements(fenrir).Level == 300, "Fenrir level must not scale with upgrade/excellent options.");
var ring = Item(13, 10, 3); Check(MuMainItemRules.Requirements(ring).Level == 50, "Transformation ring required level mismatch.");
var highArmor = Item(8, 29, 15, 8, 32); highArmor.Definition.RequiredLevel = 380;
Check(MuMainItemRules.Requirements(highArmor).Level == 380, "Level 380 equipment must not receive excellent +20 level.");
var regularArmor = Item(8, 1, 15, 8, 32); regularArmor.Definition.RequiredLevel = 100;
Check(MuMainItemRules.Requirements(regularArmor).Level == 120, "Normal excellent equipment lacks +20 required level.");
Check(MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeMaster), "Blade Master incorrectly rejected from Dark Knight equipment.");
normal.Definition.ClassRequirements[1] = 2;
Check(!MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.DarkKnight) && MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeKnight), "Second-job minimum is not enforced.");
normal.Definition.ClassRequirements[1] = 3;
Check(!MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeKnight) && MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeMaster), "Third-job minimum is not enforced.");
normal.Definition.ClassRequirements = [1,1,0,0,0,0,0];
Check(MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.MagicGladiator), "MuMain dual DW/DK equipment rule for Magic Gladiator missing.");
var harmony = Item(flags: 32, options: [0x30]);
Check(MuMainItemRules.Requirements(harmony).Strength == 164, "Harmony strength reduction missing.");
var harmonyStaff = Item(5, flags: 32, options: [0x30]);
Check(MuMainItemRules.Requirements(harmonyStaff).Agility == 134, "Staff harmony agility option was interpreted as weapon strength.");
var inactiveHarmony = Item(flags: 32, options: [0x31]);
Check(MuMainItemRules.Requirements(inactiveHarmony).Strength == 170, "Harmony reduction applied below required item upgrade.");
var shield = Item(6, flags: 1, options: [4]);
Check(MuMainItemStats.AdditionalOptions(shield).Single() == "Additional Defense Rate +20", "Shield additional option displayed as damage/defense.");
Check(MuMainItemStats.ExcellentOptions(excellent).Single() == "Excellent Damage Rate +10%", "Excellent option flags read from durability byte.");
var excArmor = Item(8, flags: 8, options: [32]);
Check(MuMainItemStats.ExcellentOptions(excArmor).Single() == "Maximum Life Increase +4%", "Excellent armor interpreted as weapon options.");
var lines = ItemUiHelper.BuildTooltipLines(staff);
Check(lines.Any(x => x.text.Contains("100/")) == false, "Zero base durability must not invent upgrade-only durability.");
Check(lines.Any(x => x.text == "One-handed Damage: 191 ~ 341"), "Tooltip omits staff physical min/max damage.");
Check(lines.Any(x => x.text == "Required Vitality: 164") && lines.Any(x => x.text == "Required Command: 30"), "Tooltip drops Vitality or Command requirements.");
var orientations = typeof(BmdPreviewRenderer).Assembly.GetType("Client.Main.Core.Utilities.MuMainItemMetadata")!;
var rotate = orientations.GetMethod("InventoryRotation", BindingFlags.Public|BindingFlags.Static)!;
Check((Vector3)rotate.Invoke(null, [Item(4, 0).Definition])! == new Vector3(180, 270, 25), "Bow must use MuMain bow rotation rather than armor rotation.");
Check((Vector3)rotate.Invoke(null, [Item(4, 8).Definition])! == new Vector3(90, 180, 20), "Crossbow must use its own rotation.");
Check((Vector3)rotate.Invoke(null, [Item(4, 7).Definition])! == new Vector3(0, 270, 15), "Ammunition rotation differs from MuMain.");
Check(new InventoryItem(normal.Definition, Point.Zero, level: 15).Details.Level == 15, "Preview-only item lost its explicit upgrade level.");
var state = (Client.Main.Core.Client.CharacterState)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Client.Main.Core.Client.CharacterState));
state.Class = CharacterClassNumber.BladeMaster; state.Level = 400;
state.Strength = 169; state.AddedStrength = 1; state.Agility = 140; state.Vitality = 80; state.Energy = 120; state.Leadership = 30;
Check(MuMainItemRules.MeetsRequirements(normal, state), "Equip check ignores added stats or promoted classes.");
state.AddedStrength = 0;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Strength."); state.AddedStrength = 1;
state.Vitality = 79;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Vitality."); state.Vitality = 80;
state.Energy = 119;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Energy."); state.Energy = 120;
state.Leadership = 29;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Command."); state.Leadership = 30;
state.Agility = 139;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Agility."); state.Agility = 140;
normal.Definition.RequiredLevel = 401;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Level."); normal.Definition.RequiredLevel = 0;
state.Class = CharacterClassNumber.HighElf;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows the wrong character class.");
if (args.Length > 0)
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
Console.WriteLine($"PASS: {checks} MuMain item requirement, class, tooltip, option, and orientation checks.");
