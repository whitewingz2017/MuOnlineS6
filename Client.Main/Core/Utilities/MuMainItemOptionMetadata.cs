// Values from MuMain Local/Eng/JewelOfHarmonyOption_eng.bmd and socketitem_eng.bmd.
namespace Client.Main.Core.Utilities;
internal static class MuMainItemOptionMetadata
{
    internal sealed record Harmony(string Name, int MinimumLevel, int[] Values);
    private static readonly Harmony[] HarmonyOptions =
    [
        new("Min. attack increase", 0, [2, 3, 4, 5, 6, 7, 9, 11, 12, 14, 15, 16, 17, 20]),
        new("Max. attack increase", 0, [3, 4, 5, 6, 7, 8, 10, 12, 14, 17, 20, 23, 26, 29]),
        new("Required strength decrease", 0, [6, 8, 10, 12, 14, 16, 20, 23, 26, 29, 32, 35, 37, 40]),
        new("Required agility decrease", 0, [6, 8, 10, 12, 14, 16, 20, 23, 26, 29, 32, 35, 37, 40]),
        new("Attack increase(Min,Max)", 6, [0, 0, 0, 0, 0, 0, 7, 8, 9, 11, 12, 14, 16, 19]),
        new("Critical damage increase", 6, [0, 0, 0, 0, 0, 0, 12, 14, 16, 18, 20, 22, 24, 30]),
        new("Skill attack increase", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 12, 14, 16, 18, 22]),
        new("Attack sucess rate increase(PVP)", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 7, 9, 11, 14]),
        new("SD decrement increase", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 5, 7, 9, 10]),
        new("SD bypass rate increase", 13, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10]),
        new("Wizardry increase", 0, [6, 8, 10, 12, 14, 16, 17, 18, 19, 21, 23, 25, 27, 31]),
        new("Required strength decrease", 0, [6, 8, 10, 12, 14, 16, 20, 23, 26, 29, 32, 35, 37, 40]),
        new("Required agility decrease", 0, [6, 8, 10, 12, 14, 16, 20, 23, 26, 29, 32, 35, 37, 40]),
        new("Skill attack increase", 6, [0, 0, 0, 0, 0, 0, 7, 10, 13, 16, 19, 22, 25, 30]),
        new("Critical damage increase", 6, [0, 0, 0, 0, 0, 0, 10, 12, 14, 16, 18, 20, 22, 28]),
        new("SD decrement increase", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 6, 8, 10, 13]),
        new("Attack success rate increase(PVP)", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 7, 9, 11, 14]),
        new("SD bypass rate increase", 13, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15]),
        new("", -1, [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
        new("", -1, [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
        new("Defense increase", 0, [3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 18, 20, 22, 25]),
        new("Max. AG increase", 3, [0, 0, 0, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 25]),
        new("Max. HP increase", 3, [0, 0, 0, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 30]),
        new("Life auto increment increase", 6, [0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8]),
        new("Mana auto increment increase", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5]),
        new("Defense success rate increase(PVP)", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 4, 5, 6, 8]),
        new("Damage decrement increase", 9, [0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 4, 5, 6, 7]),
        new("SD ratio increase", 13, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5]),
        new("", -1, [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
        new("", -1, [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1]),
    ];
    public static Harmony GetHarmony(int category, int option) => category is >= 0 and < 3 && option is >= 1 and <= 10 ? HarmonyOptions[category * 10 + option - 1] : null;
    public static int HarmonyReduction(int category, int option, int level) => level is >= 0 and < 14 ? GetHarmony(category, option)?.Values[level] ?? 0 : 0;
    public static readonly int[] SocketStrength = [0, 0, 0, 0, 0];
    public static readonly int[] SocketDexterity = [0, 0, 0, 0, 0];
}
