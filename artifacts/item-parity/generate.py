import json,pathlib,struct
root=pathlib.Path('C:/MuOnline_Source/MuMain-main/src/bin/Data/Items')
out=pathlib.Path('Client.Main/Core/Utilities/MuMainItemMetadata.cs')
s=['// Snapshot of MuMain src/bin/Data/Items and Items/Models. Keep client Item.bmd stats authoritative.','using Microsoft.Xna.Framework;','using Client.Main.Controls.UI.Game.Inventory;','namespace Client.Main.Core.Utilities;','internal static class MuMainItemMetadata','{','    public static int Key(ItemDefinition d) => d.Group * 512 + d.Id;']
items=[(j['group']*512+i['number'],i) for p in sorted(root.glob('Group*.json')) for j in [json.loads(p.read_text(encoding='utf-8-sig'))] for i in j['items']]
for name,pred in [('SecondWing',lambda i:i.get('wingTier')=='second' and 'cape' not in i.get('tags',[])),('ThirdWing',lambda i:i.get('wingTier')=='third'),('Wing',lambda i:i.get('slot')=='wings'),('SummonerBook',lambda i:'summonerBook' in i.get('tags',[])),('Scepter',lambda i:'scepter' in i.get('tags',[])),('Archangel',lambda i:'divineArchangelWeapon' in i.get('tags',[]))]:
 keys=[str(k) for k,i in items if pred(i)]
 s+=['    public static bool Is'+name+'(ItemDefinition d) => Key(d) is '+(' or '.join(keys) if keys else '-1')+';']
s+=['    public static Vector3 InventoryRotation(ItemDefinition d) => Key(d) switch','    {']
rotations={}
for p in sorted((root/'Models').glob('Group*.json')):
 j=json.loads(p.read_text(encoding='utf-8-sig'))
 for m in j['models']:
  rot=tuple(m.get('inventory',{}).get('rotation',[270,-10,0]))
  rotations.setdefault(rot,[]).append(j['group']*512+m['number'])
for rot,keys in rotations.items():
 s+=['        '+' or '.join(map(str,keys))+' => new('+', '.join(str(x)+'f' for x in rot)+'),']
s+=['        _ => new(270f, -10f, 0f),','    };','}']
out.write_text('\n'.join(s)+'\n',encoding='utf-8')
# Extract the reference BMD tables using MuMain BuxConvert (FC CF AB).
base=root.parent/'Local/Eng'
def decode(p,per=0):
 b=bytearray(p.read_bytes());key=[0xfc,0xcf,0xab]
 for n in range(len(b)): b[n]^=key[(n%per if per else n)%3]
 return b
h=decode(base/'JewelOfHarmonyOption_eng.bmd')
s=['// Values from MuMain Local/Eng/JewelOfHarmonyOption_eng.bmd and socketitem_eng.bmd.','namespace Client.Main.Core.Utilities;','internal static class MuMainItemOptionMetadata','{','    public static int HarmonyReduction(int group, int option, int level) => (group, option) switch','    {']
for group,op in [(0,3),(0,4),(1,2),(1,3)]:
 vals=struct.unpack_from('<14i',h,(group*10+op-1)*180+68)
 s+=['        ('+str(group)+', '+str(op)+') => new int[] { '+', '.join(map(str,vals))+' }[level],']
s+=['        _ => 0,','    };']
b=decode(base/'socketitem_eng.bmd',104)
for seed,name in [(38,'Strength'),(39,'Dexterity')]:
 vals=struct.unpack_from('<5i',b,seed*104+76)
 s+=['    public static readonly int[] Socket'+name+' = ['+', '.join(map(str,vals))+'];']
s+=['}']
pathlib.Path('Client.Main/Core/Utilities/MuMainItemOptionMetadata.cs').write_text('\n'.join(s)+'\n')
print('Generated item rotations/categories and reference requirement reductions.')
print('\n'.join(s))
