using System.Reflection;
using System.Text.Json;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
using MUnique.OpenMU.Network.Packets;

if (args.Contains("--preview-containment-only"))
{
    var fit = typeof(BmdPreviewRenderer).GetMethod("FitPreviewToSlot", BindingFlags.NonPublic | BindingFlags.Static)!;
    var bounds = new BoundingBox(new Vector3(-20, -10, -30), new Vector3(20, 10, 30));
    int checkedPoses = 0;
    foreach (int width in new[] { 32, 64, 128 })
    foreach (int height in new[] { 32, 96, 128 })
    foreach (int angle in new[] { 0, 37, 90, 145 })
    foreach (float scale in new[] { .1f, 1f, 4f })
    {
        var world = Matrix.CreateScale(scale) * Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
            Matrix.CreateRotationZ(MathHelper.ToRadians(angle)) * Matrix.CreateTranslation(60, -40, 0);
        var contained = (Matrix)fit.Invoke(null, new object[] { world, bounds, width, height })!;
        foreach (var corner in bounds.GetCorners())
        {
            var point = Vector3.Transform(corner, contained);
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) ||
                MathF.Abs(point.X) > width / 2f - 1.99f || MathF.Abs(point.Y) > height / 2f - 1.99f)
                throw new Exception($"Preview escapes its {width}x{height} slot at angle {angle}, scale {scale}.");
        }
        checkedPoses++;
    }
    Console.WriteLine($"Verified {checkedPoses} item poses stay inside their slots, including rotated and offset models.");
    return;
}

if (args.Contains("--shop-prices-only"))
{
    InventoryItem PriceItem(int group, int id, byte level, byte durability, byte flags = 0) =>
        new(new ItemDefinition(id, "Price regression", 2, 2) { Group = group }, Point.Zero,
            [(byte)(group << 4), (byte)id, level, durability, flags], durability, level);
    void Expect(string name, int actual, int expected)
    {
        if (actual != expected) throw new Exception($"{name}: expected {expected}, got {actual}.");
    }
    var pants = PriceItem(9, 2, 3, 26, 2);
    Expect("Pad Pants +3 luck 26/31", ItemPriceCalculator.CalculateSellPrice(pants), 810);
    Expect("255 large healing +1", ItemPriceCalculator.CalculateBuyPrice(PriceItem(14, 3, 1, 255)), 765000);
    Expect("Single large healing +1", ItemPriceCalculator.CalculateBuyPrice(PriceItem(14, 3, 1, 1)), 3000);
    Expect("255 small healing", ItemPriceCalculator.CalculateBuyPrice(PriceItem(14, 1, 0, 255)), 20400);
    Expect("Sell large healing stack", ItemPriceCalculator.CalculateSellPrice(PriceItem(14, 3, 1, 255)), 255000);
    Console.WriteLine("Verified 5 shop price cases against the server transaction formula.");
    return;
}

if (args.Contains("--potion-slots-only"))
{
    var hudType = typeof(Client.Main.Controls.UI.Game.Hud.ModernBottomHud);
    var resolve = hudType.GetMethod("ResolveAvailablePotion", BindingFlags.NonPublic | BindingFlags.Static)!;
    byte[] Potion(byte id, byte count) => [0xE0, id, 0, count, 0];
    var inventory = new Dictionary<byte, byte[]>();
    void Expect(int preferred, int expected)
    {
        var actual = ((byte Group, int Id))resolve.Invoke(null, new object[] { ((byte)14, preferred), inventory })!;
        if (actual != ((byte)14, expected))
            throw new Exception($"Potion {preferred}: expected {expected}, got {actual}.");
    }
    inventory[12] = Potion(3, 255);
    inventory[13] = Potion(6, 126);
    Expect(1, 3); // HP falls back without selecting MP.
    Expect(4, 6); // MP falls back without selecting HP.
    inventory[14] = Potion(1, 3);
    Expect(1, 1); // Preserve the chosen size while available.
    inventory[14] = Potion(1, 0);
    Expect(1, 3); // Ignore an exhausted stack awaiting removal.
    inventory.Remove(12);
    inventory[0] = Potion(3, 255);
    Expect(1, 1); // Equipment slots cannot supply a potion.
    Expect(70, 70); // Special consumables keep their exact assignment.
    inventory.Clear();
    Expect(4, 4); // Empty inventory retains the saved preference.
    Console.WriteLine("Verified 7 potion shortcut fallback cases.");
    return;
}

if (args.Contains("--server-requirements-only"))
{
    string casesPath = Path.Combine(AppContext.BaseDirectory, "server-requirement-cases.json");
    using var document = JsonDocument.Parse(File.ReadAllText(casesPath));
    int checkedCases = 0;
    foreach (var row in document.RootElement.EnumerateArray())
    {
        int group = row.GetProperty("Group").GetInt32(), id = row.GetProperty("Id").GetInt32();
        int level = row.GetProperty("Level").GetInt32(), variant = row.GetProperty("Variant").GetInt32();
        int option = row.GetProperty("Option").GetInt32();
        var definition = new ItemDefinition(id, "Server item", 1, 1) { Group = group };
        var rawBytes = new List<byte> { (byte)((group << 4) | ((id >> 8) & 15)), (byte)id, (byte)level, 100,
            (byte)((option > 0 ? 1 : 0) | (variant == 1 ? 8 : 0) | (variant == 2 ? 16 : 0)) };
        if (option > 0) rawBytes.Add((byte)option);
        if (variant == 1) rawBytes.Add(1);
        if (variant == 2) rawBytes.Add(1);
        byte[] raw = rawBytes.ToArray();
        var actual = MuMainItemRules.Requirements(new InventoryItem(definition, Point.Zero, raw));
        int[] observed = [actual.Level, actual.Strength, actual.Agility, actual.Vitality, actual.Energy, actual.Command];
        int[] expected = row.GetProperty("Expected").EnumerateArray().Select(x => x.GetInt32()).ToArray();
        if (!observed.SequenceEqual(expected))
            throw new Exception($"Item {group}:{id} +{level} variant {variant} option {option}: [{string.Join(",", observed)}] vs server [{string.Join(",", expected)}]");
        checkedCases++;
    }
    Console.WriteLine($"Verified {checkedCases} server item requirement cases.");
    return;
}

int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
// Armor previews must use MuMain's Player textures, while Item-only custom assets remain valid.
var resolveModel = typeof(ItemDatabase).GetMethod("ResolveItemModelPath", BindingFlags.NonPublic | BindingFlags.Static)!;
string assetRoot = Path.Combine(Path.GetTempPath(), "MuArmorPaths-" + Guid.NewGuid());
string oldDataPath = Client.Main.Constants.DataPath;
try
{
    Client.Main.Constants.DataPath = assetRoot;
    Directory.CreateDirectory(Path.Combine(assetRoot, "Player", "LuckyItem", "65"));
    File.WriteAllBytes(Path.Combine(assetRoot, "Player", "ArmorElf03.bmd"), []);
    File.WriteAllBytes(Path.Combine(assetRoot, "Player", "LuckyItem", "65", "new_Armor04.bmd"), []);
    Directory.CreateDirectory(Path.Combine(assetRoot, "Item", "Ingameshop"));
    File.WriteAllBytes(Path.Combine(assetRoot, "Item", "Ingameshop", "necklace_red.bmd"), []);
    string Resolve(int group, int id, string path) => (string)resolveModel.Invoke(null, [group, id, path])!;
    Check(Resolve(8, 12, "Item/ArmorElf03.bmd") == "Player/ArmorElf03.bmd", "Wind Armor previews load conflicting Item textures.");
    Check(Resolve(8, 12, "Item/armorelf03.bmd") == "Player/armorelf03.bmd", "Armor lookup must tolerate Android filename casing.");
    Check(Resolve(8, 65, "Item/new_Armor04.bmd") == "Player/LuckyItem/65/new_Armor04.bmd", "Lucky armor does not use its own MuMain texture folder.");
    Check(Resolve(8, 100, "Item/CustomArmor.bmd") == "Item/CustomArmor.bmd", "Item-only custom armor path was lost.");
    Check(Resolve(0, 12, "Item/ArmorElf03.bmd") == "Item/ArmorElf03.bmd", "Non-armor item was redirected to Player.");
    Check(Resolve(13, 113, "Item/necklace_red.bmd") == "Item/Ingameshop/necklace_red.bmd", "Shop necklace uses conflicting root Item textures.");
}
finally
{
    Client.Main.Constants.DataPath = oldDataPath;
    Directory.Delete(assetRoot, true);
}
InventoryItem Item(int group = 0, int id = 0, int level = 0, byte flags = 0, params byte[] options)
{
    var definition = new ItemDefinition(id, "Reference item", 2, 3) { Group = group, DropLevel = 50,
        RequiredStrength = 100, RequiredDexterity = 80, RequiredEnergy = 50, RequiredVitality = 40, RequiredCommand = 30,
        DamageMin = 100, DamageMax = 200, Defense = 100, MagicPower = 100, ClassRequirements = [0, 1, 0, 0, 0, 0, 0] };
    byte[] raw = [(byte)(group << 4), (byte)id, (byte)level, 100, flags, ..options];
    return new InventoryItem(definition, Point.Zero, raw, 100, level);
}
var normal = Item();
var wizardRing = ItemDatabase.GetItemDefinition(13, 20);
Check(wizardRing?.TexturePath == "Item/MagicRing00.bmd", "Wizard's Ring has no MuMain event model.");
var previewModelPath = typeof(BmdPreviewRenderer).GetMethod("GetPreviewModelPath", BindingFlags.Static | BindingFlags.NonPublic)!;
Check((string)previewModelPath.Invoke(null, [wizardRing, 0])! == "Item/MagicRing00.bmd", "Wizard's Ring +0 uses the wrong event model.");
for (int ringLevel = 1; ringLevel <= 3; ringLevel++)
    Check((string)previewModelPath.Invoke(null, [wizardRing, ringLevel])! == "Item/RingOfLordEvent00.bmd", "Wizard's Ring variant is missing its original model.");
Check(MuMainItemRules.MuMainReferenceRequirements(normal) == new ItemRequirements(0, 170, 140, 80, 120, 30), "Base requirement vector differs from MuMain.");
var excellent = Item(level: 15, flags: 9, options: [4, 32]);
Check(MuMainItemRules.MuMainReferenceRequirements(excellent) == new ItemRequirements(0, 400, 308, 164, 260, 30), "+15 excellent/additional-option requirements are wrong.");
var ancient = Item(level: 15, flags: 17, options: [4, 17]);
Check(MuMainItemRules.MuMainReferenceRequirements(ancient) == new ItemRequirements(0, 415, 320, 170, 270, 30), "Ancient +30 item level requirement bonus missing.");
Check(MuMainItemStats.Calculate(excellent) == new ItemDisplayStats(191, 341, 174, 0, 125), "+15 excellent damage/defense/magic vector differs from MuMain.");
Check(MuMainItemStats.Calculate(ancient).Defense == 243, "Ancient defense bonus differs from MuMain.");
Check(MuMainItemStats.Calculate(Item(6, level: 15, flags: 16, options: [1])).Defense == 145, "Ancient shield formula differs from MuMain.");
var staff = Item(5, level: 15, flags: 8, options: [32]);
Check(MuMainItemStats.Calculate(staff).DamageMax == 341 && MuMainItemStats.Calculate(staff).MagicPower == 125, "Physical staff damage was incorrectly replaced by magic damage.");
var chaos = Item(4, 6, 15, 8, 32);
Check(MuMainItemStats.Calculate(chaos).DamageMax == 266, "Chaos weapon excellent override missing.");
var scroll = Item(15); scroll.Definition.RequiredLevel = 50;
Check(MuMainItemRules.MuMainReferenceRequirements(scroll).Energy == 50, "Consumable scroll energy must use its raw requirement.");
var book = Item(5, 21, 15);
Check(MuMainItemRules.MuMainReferenceRequirements(book).Energy == 117, "Summoner book energy formula incorrect.");
int[] summonEnergy = [30, 60, 90, 130, 170, 210, 300, 500];
for (int level = 0; level < 8; level++) Check(MuMainItemRules.MuMainReferenceRequirements(Item(12, 11, level)).Energy == summonEnergy[level], "Summoning orb energy table mismatch.");
var wing2 = Item(12, 3, 15); wing2.Definition.RequiredLevel = 180; wing2.Definition.Defense = 30;
Check(MuMainItemRules.MuMainReferenceRequirements(wing2).Level == 255 && MuMainItemStats.Calculate(wing2).Defense == 57, "Second-tier wing upgrade formula mismatch.");
var wing3 = Item(12, 36, 15); wing3.Definition.RequiredLevel = 180; wing3.Definition.Defense = 40;
Check(MuMainItemRules.MuMainReferenceRequirements(wing3).Level == 180 && MuMainItemStats.Calculate(wing3).Defense == 86, "Third-tier wing formula mismatch.");
var fenrir = Item(13, 37, 15, 8, 32); fenrir.Definition.RequiredLevel = 300;
Check(MuMainItemRules.MuMainReferenceRequirements(fenrir).Level == 300, "Fenrir level must not scale with upgrade/excellent options.");
var ring = Item(13, 10, 3); Check(MuMainItemRules.MuMainReferenceRequirements(ring).Level == 50, "Transformation ring required level mismatch.");
var highArmor = Item(8, 29, 15, 8, 32); highArmor.Definition.RequiredLevel = 380;
Check(MuMainItemRules.MuMainReferenceRequirements(highArmor).Level == 380, "Level 380 equipment must not receive excellent +20 level.");
var regularArmor = Item(8, 1, 15, 8, 32); regularArmor.Definition.RequiredLevel = 100;
Check(MuMainItemRules.MuMainReferenceRequirements(regularArmor).Level == 120, "Normal excellent equipment lacks +20 required level.");
Check(MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeMaster), "Blade Master incorrectly rejected from Dark Knight equipment.");
normal.Definition.ClassRequirements[1] = 2;
Check(!MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.DarkKnight) && MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeKnight), "Second-job minimum is not enforced.");
normal.Definition.ClassRequirements[1] = 3;
Check(!MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeKnight) && MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.BladeMaster), "Third-job minimum is not enforced.");
normal.Definition.ClassRequirements = [1,1,0,0,0,0,0];
Check(MuMainItemRules.CanUseClass(normal.Definition, CharacterClassNumber.MagicGladiator), "MuMain dual DW/DK equipment rule for Magic Gladiator missing.");
var harmony = Item(flags: 32, options: [0x30]);
Check(MuMainItemRules.MuMainReferenceRequirements(harmony).Strength == 164, "Harmony strength reduction missing.");
var harmonyStaff = Item(5, flags: 32, options: [0x30]);
Check(MuMainItemRules.MuMainReferenceRequirements(harmonyStaff).Agility == 134, "Staff harmony agility option was interpreted as weapon strength.");
var inactiveHarmony = Item(flags: 32, options: [0x31]);
Check(MuMainItemRules.MuMainReferenceRequirements(inactiveHarmony).Strength == 170, "Harmony reduction applied below required item upgrade.");
var shield = Item(6, flags: 1, options: [4]);
Check(MuMainItemStats.AdditionalOptions(shield).Single() == "Additional Defense Rate +20", "Shield additional option displayed as damage/defense.");
Check(MuMainItemStats.ExcellentOptions(excellent).Single() == "Excellent Damage Rate +10%", "Excellent option flags read from durability byte.");
var excArmor = Item(8, flags: 8, options: [32]);
Check(MuMainItemStats.ExcellentOptions(excArmor).Single() == "Maximum Life Increase +4%", "Excellent armor interpreted as weapon options.");
var lines = ItemUiHelper.BuildTooltipLines(staff);
Check(lines.Any(x => x.text.Contains("100/")) == false, "Zero base durability must not invent upgrade-only durability.");
Check(lines.Any(x => x.text == "One-handed Damage: 191 ~ 341"), "Tooltip omits staff physical min/max damage.");
var staffRequirements = MuMainItemRules.Requirements(staff);
Check((staffRequirements.Vitality == 0 || lines.Any(x => x.text == $"Required Vitality: {staffRequirements.Vitality}"))
    && (staffRequirements.Command == 0 || lines.Any(x => x.text == $"Required Command: {staffRequirements.Command}")),
    "Tooltip drops a server-defined Vitality or Command requirement.");
var orientations = typeof(BmdPreviewRenderer).Assembly.GetType("Client.Main.Core.Utilities.MuMainItemMetadata")!;
var rotate = orientations.GetMethod("InventoryRotation", BindingFlags.Public|BindingFlags.Static)!;
Check((Vector3)rotate.Invoke(null, [Item(4, 0).Definition])! == new Vector3(180, 270, 25), "Bow must use MuMain bow rotation rather than armor rotation.");
Check((Vector3)rotate.Invoke(null, [Item(4, 8).Definition])! == new Vector3(90, 180, 20), "Crossbow must use its own rotation.");
Check((Vector3)rotate.Invoke(null, [Item(4, 7).Definition])! == new Vector3(0, 270, 15), "Ammunition rotation differs from MuMain.");
Check(new InventoryItem(normal.Definition, Point.Zero, level: 15).Details.Level == 15, "Preview-only item lost its explicit upgrade level.");
var state = (Client.Main.Core.Client.CharacterState)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Client.Main.Core.Client.CharacterState));
var equipItem = Item(0, 500); // Synthetic MuMain reference item, outside the server item catalog.
state.Class = CharacterClassNumber.BladeMaster; state.Level = 400;
state.Strength = 169; state.AddedStrength = 1; state.Agility = 140; state.Vitality = 80; state.Energy = 120; state.Leadership = 30;
Check(MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check ignores added stats or promoted classes.");
state.AddedStrength = 0;
Check(!MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check allows insufficient Strength."); state.AddedStrength = 1;
state.Vitality = 79;
Check(!MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check allows insufficient Vitality."); state.Vitality = 80;
state.Energy = 119;
Check(!MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check allows insufficient Energy."); state.Energy = 120;
state.Leadership = 29;
Check(!MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check allows insufficient Command."); state.Leadership = 30;
state.Agility = 139;
Check(!MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check allows insufficient Agility."); state.Agility = 140;
equipItem.Definition.RequiredLevel = 401;
Check(!MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check allows insufficient Level."); equipItem.Definition.RequiredLevel = 0;
state.Class = CharacterClassNumber.HighElf;
Check(!MuMainItemRules.MeetsRequirements(equipItem, state), "Equip check allows the wrong character class.");
var displayType = typeof(BmdPreviewRenderer).Assembly.GetType("Client.Main.Core.Utilities.MuMainInventoryDisplay")!;
var getDisplay = displayType.GetMethod("Get", BindingFlags.Public | BindingFlags.Static)!;
var worldMatrix = displayType.GetMethod("WorldMatrix")!;
var rotationMatrix = displayType.GetMethod("RotationMatrix", BindingFlags.Public | BindingFlags.Static)!;
Vector3 MuRotate(Vector3 point, Vector3 degrees)
{
    float x = MathHelper.ToRadians(degrees.X), y = MathHelper.ToRadians(degrees.Y), z = MathHelper.ToRadians(degrees.Z);
    float sr = MathF.Sin(x), cr = MathF.Cos(x), sp = MathF.Sin(y), cp = MathF.Cos(y), sy = MathF.Sin(z), cy = MathF.Cos(z);
    // Independent column-vector AngleMatrix/VectorRotate from MuMain ZzzMathLib.
    return new(cp * cy * point.X + (sr * sp * cy - cr * sy) * point.Y + (cr * sp * cy + sr * sy) * point.Z,
        cp * sy * point.X + (sr * sp * sy + cr * cy) * point.Y + (cr * sp * sy - sr * cy) * point.Z,
        -sp * point.X + sr * cp * point.Y + cr * cp * point.Z);
}
foreach (int group in new[] { 0, 4, 8, 12, 14 })
    for (int id = 0; id < 512; id++)
    {
        var item = Item(group, id);
        item.Durability = 2;
        bool expected = group == 4 && id is 7 or 15 || group == 14 &&
            (id is >= 0 and <= 8 or >= 35 and <= 40 or >= 46 and <= 50 or 70 or 71
                or >= 78 and <= 82 or >= 88 and <= 90 or 94 or 100 or 133 or >= 153 and <= 156);
        var tooltip = ItemUiHelper.BuildTooltipLines(item);
        Check(tooltip.Count(line => line.text == "Number of items: 2") == (expected ? 1 : 0), $"Quantity tooltip mismatch for {group}:{id}.");
        Check(!expected || !tooltip.Any(line => line.text.StartsWith("Durability:")), $"Stack quantity also displayed as durability for {group}:{id}.");
    }
Check(ItemUiHelper.ShowsItemCount(Item(14, 21, 3)) && !ItemUiHelper.ShowsItemCount(Item(14, 21, 2)), "Rena quantity must apply only to level 3.");
foreach (var screen in new[] { new Rectangle(0, 0, 1247, 720), new Rectangle(0, 0, 320, 640) })
    foreach (var itemRect in new[] { new Rectangle(10, 10, 34, 34), new Rectangle(screen.Right - 44, screen.Bottom - 44, 34, 34), new Rectangle(100, 280, 34, 34) })
    {
        var bounds = ItemUiHelper.GetTooltipBounds(itemRect, new Point(240, 200), screen);
        Check(screen.Contains(bounds), "Item tooltip escaped the screen.");
        Check(bounds.Bottom <= itemRect.Top || bounds.Top >= itemRect.Bottom, "Item tooltip was placed beside/over the item despite vertical room.");
    }
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
            var matrix = (Matrix)rotationMatrix.Invoke(null, [expected])!;
            foreach (var axis in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ })
                Check(Vector3.Distance(Vector3.Transform(axis, matrix), MuRotate(axis, expected)) < 0.00001f,
                    $"MuMain rotation transform differs for {group}:{definition.Id}.");
            float expectedScale = display.ValueKind == System.Text.Json.JsonValueKind.Object && display.TryGetProperty("scale", out var scaleValue) ? scaleValue.GetSingle() : 0.0025f;
            float bodyHeight = display.ValueKind == System.Text.Json.JsonValueKind.Object && display.TryGetProperty("bodyHeight", out var bodyValue) ? bodyValue.GetSingle() : 0f;
            Vector2 anchor = new(0.5f, 0.6f);
            Vector3 offset = Vector3.Zero;
            if (display.ValueKind == System.Text.Json.JsonValueKind.Object && display.TryGetProperty("anchor", out var anchorValue))
                anchor = new(anchorValue[0].GetSingle(), anchorValue[1].GetSingle());
            if (display.ValueKind == System.Text.Json.JsonValueKind.Object && display.TryGetProperty("offset", out var offsetValue))
                for (int i = 0; i < offsetValue.GetArrayLength(); i++)
                    if (i == 0) offset.X = offsetValue[i].GetSingle(); else if (i == 1) offset.Y = offsetValue[i].GetSingle(); else offset.Z = offsetValue[i].GetSingle();
            var actualDisplay = getDisplay.Invoke(null, [definition])!;
            Check((float)displayType.GetProperty("Scale")!.GetValue(actualDisplay)! == expectedScale, $"MuMain model scale differs for {group}:{definition.Id}.");
            var actualWorld = (Matrix)worldMatrix.Invoke(actualDisplay, [definition, 34, 34, 0f])!;
            float pixels = 480f / (400f * MathF.Tan(MathHelper.ToRadians(0.5f))) * 1.7f;
            Vector3 sourcePoint = new(3, 7, 11);
            Vector3 expectedPoint = MuRotate(sourcePoint + new Vector3(0, 0, bodyHeight), expected) * expectedScale * pixels
                + new Vector3((anchor.X - 0.5f) * 34, (0.5f - anchor.Y) * 34, 0) + offset * pixels;
            Check(Vector3.Distance(Vector3.Transform(sourcePoint, actualWorld), expectedPoint) < 0.001f, $"MuMain model position/scale differs for {group}:{definition.Id}.");
        }
    }
}
Console.WriteLine($"PASS: {checks} MuMain item requirement, class, tooltip, option, and orientation checks.");
