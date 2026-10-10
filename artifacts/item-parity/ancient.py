from pathlib import Path
import json
base=Path('C:/MuOnline_Source/MuMain-main/src/bin/Data/Local')
def decode(data): return bytes(x^[252,207,171][i%3] for i,x in enumerate(data))
b=(base/'Eng/itemsetoption_eng.bmd').read_bytes(); names=[decode(b[n*110:(n+1)*110])[:64].split(b'\0')[0].decode('utf-8',errors='replace') for n in range(64)]
b=(base/'itemsettype.bmd').read_bytes();entries={}
for n in range(8192):
 row=decode(b[n*4:(n+1)*4])
 for variant,setid in enumerate(row[:2],1):
  if setid>0 and setid<64 and names[setid]: entries[n*4+variant]=names[setid]
s=['// Ancient set names from MuMain Local/ItemSetType.bmd and Eng/ItemSetOption_eng.bmd.','namespace Client.Main.Core.Utilities;','internal static class MuMainAncientNames','{','    public static string Get(int group, int number, int discriminator) => ((group * 512 + number) * 4 + discriminator) switch','    {']
for name in sorted(set(entries.values())):
 s+=['        '+' or '.join(str(k) for k,v in entries.items() if v==name)+' => '+json.dumps(name)+',']
s+=['        _ => null,','    };','}']
Path('Client.Main/Core/Utilities/MuMainAncientNames.cs').write_text('\n'.join(s)+'\n')
