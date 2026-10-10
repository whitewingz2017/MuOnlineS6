from pathlib import Path
import struct,json
base=Path('C:/MuOnline_Source/MuMain-main/src/bin/Data/Local/Eng')
b=bytes(x^[252,207,171][i%3] for i,x in enumerate((base/'JewelOfHarmonyOption_eng.bmd').read_bytes()))
s=['// Values from MuMain Local/Eng/JewelOfHarmonyOption_eng.bmd and socketitem_eng.bmd.','namespace Client.Main.Core.Utilities;','internal static class MuMainItemOptionMetadata','{','    internal sealed record Harmony(string Name, int MinimumLevel, int[] Values);','    private static readonly Harmony[] HarmonyOptions =','    [']
for i in range(30):
 row=b[i*180:(i+1)*180];name=row[4:64].split(b'\0')[0].decode('utf-8');minimum=struct.unpack_from('<i',row,64)[0];vals=struct.unpack_from('<14i',row,68)
 s+=['        new('+json.dumps(name)+', '+str(minimum)+', ['+', '.join(map(str,vals))+']),']
s+=['    ];','    public static Harmony GetHarmony(int category, int option) => category is >= 0 and < 3 && option is >= 1 and <= 10 ? HarmonyOptions[category * 10 + option - 1] : null;','    public static int HarmonyReduction(int category, int option, int level) => level is >= 0 and < 14 ? GetHarmony(category, option)?.Values[level] ?? 0 : 0;','    public static readonly int[] SocketStrength = [0, 0, 0, 0, 0];','    public static readonly int[] SocketDexterity = [0, 0, 0, 0, 0];','}']
Path('Client.Main/Core/Utilities/MuMainItemOptionMetadata.cs').write_text('\n'.join(s)+'\n')
p=Path('Client.Main/Controls/UI/Game/Common/ItemUiHelper.cs');s=p.read_text();s=s.replace('details.IsExcellent ? $"Excellent {item.Definition.Name}"', 'details.IsExcellent && !MuMainItemMetadata.IsWing(item.Definition) ? $"Excellent {item.Definition.Name}"')
s=s.replace('            // Show "Right-click to use"', '''            int harmonyCategory = def.Group < 5 ? 0 : def.Group == 5 ? 1 : def.Group < 12 ? 2 : -1;
            var harmony = MuMainItemOptionMetadata.GetHarmony(harmonyCategory, details.HarmonyOption >> 4);
            int harmonyLevel = details.HarmonyOption & 15;
            if (harmony != null && harmony.Name.Length > 0 && harmonyLevel < 14)
                lines.Add(($"{harmony.Name} +{harmony.Values[harmonyLevel]}", details.Level >= harmonyLevel ? yellow : Color.Gray));

            // Show "Right-click to use"''')
p.write_text(s)
# Consistent compact line spacing, including MuMain half-height separators.
for rel in ['Inventory/InventoryControl.cs','Vault/VaultControl.cs','NpcShop/NpcShopControl.cs','Trade/TradeControl.cs']:
 p=Path('Client.Main/Controls/UI/Game')/rel;s=p.read_text(encoding='utf-8-sig');start=s.index('        private void DrawTooltip(');end=s.index('\n        }',start)+10;block=s[start:end]
 block=block.replace('            const int lineSpacing = 4;','')
 block=block.replace('totalHeight += (int)MathF.Ceiling(sz.Y) + lineSpacing;', 'totalHeight += ItemUiHelper.TooltipLineHeight(_font, text, scale);')
 block=block.replace('textY += (int)textSize.Y + lineSpacing;', 'textY += ItemUiHelper.TooltipLineHeight(_font, text, scale);')
 block=block.replace('            // Add separator after the first line\n','').replace('                // Separator after item name\n','')
 s=s[:start]+block+s[end:];p.write_text(s,encoding='utf-8')
