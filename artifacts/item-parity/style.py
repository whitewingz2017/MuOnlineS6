from pathlib import Path
p=Path('Client.Main/Controls/UI/Game/Common/ItemUiHelper.cs');s=p.read_text()
s=s.replace('            if (def.AttackSpeed > 0) lines.Add', '            if (def.WalkSpeed > 0) lines.Add(($"Movement Speed: {def.WalkSpeed}", Color.White));\n            if (def.AttackSpeed > 0) lines.Add')
s=s.replace('            if (stats.Defense > 0) lines.Add(($"Defense: {stats.Defense}", Color.White));','            if (stats.Defense > 0) lines.Add(($"Defense: {stats.Defense}", details.IsExcellent && def.Group is >= 7 and <= 11 ? blue : Color.White));')
s=s.replace('~ {stats.DamageMax}", Color.White)', '~ {stats.DamageMax}", details.IsExcellent ? blue : Color.White)')
s=s.replace('Defense Rate: {stats.DefenseRate}", Color.White)', 'Defense Rate: {stats.DefenseRate}", details.IsExcellent ? blue : Color.White)')
p.write_text(s)
# Apply the same original tooltip treatment to inventory, vault, shop and trade.
for rel in ['Inventory/InventoryControl.cs','Vault/VaultControl.cs','NpcShop/NpcShopControl.cs','Trade/TradeControl.cs']:
 p=Path('Client.Main/Controls/UI/Game')/rel;s=p.read_text(encoding='utf-8-sig')
 start=s.index('        private void DrawTooltip(')
 # All four methods finish before the following method/section.
 end=s.index('\n        }',start)+10
 block=s[start:end]
 if rel!='Inventory/InventoryControl.cs':
  a=block.index('            var shadowRect =')
  b=block.index('            const int borderThickness =',a)
  block=block[:a]+'''            spriteBatch.Draw(pixel, tooltipRect, Color.Black * 0.8f);
            Color borderColor = Color.White;
'''+block[b:]
 block=block.replace('const int borderThickness = 2;', 'const int borderThickness = 1;')
 block=block.replace('firstLine ? borderColor : color', 'color').replace('isFirstLine ? borderColor : color', 'color')
 # Remove ornamental rule below the title; MuMain uses a half-height blank line.
 for flag in ['firstLine','isFirstLine']:
  tag='            bool '+flag+' = true;'
  block=block.replace(tag,'')
  tag='                if ('+flag+')'
  if tag in block:
   a=block.index(tag);b=block.index('\n                }',a)+18
   block=block[:a]+block[b:]
 block=block.replace('            totalHeight += 6;', '')
 block=block.replace('const int paddingX = 14;', 'const int paddingX = 4;').replace('const int paddingY = 12;', 'const int paddingY = 4;')
 block=block.replace('screenBounds.Right - tooltipRect.Width - 10)', 'Math.Max(10, screenBounds.Right - tooltipRect.Width - 10))') if 'Math.Max(10, screenBounds.Right' not in block else block
 block=block.replace('screenBounds.Bottom - tooltipRect.Height - 10)', 'Math.Max(10, screenBounds.Bottom - tooltipRect.Height - 10))') if 'Math.Max(10, screenBounds.Bottom' not in block else block
 s=s[:start]+block+s[end:];p.write_text(s,encoding='utf-8')
