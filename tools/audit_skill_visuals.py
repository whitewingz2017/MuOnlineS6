"""Inventory configured skill visual routes with direct MuMain source evidence."""
from pathlib import Path
import re, json

root = Path(__file__).resolve().parents[1]
reference = Path('C:/MuOnline_Source/MuMain-main/src/source')
server = root.parent / 'OpenMU/src/Persistence/Initialization'
read = lambda p: p.read_text(encoding='utf-8-sig', errors='replace')
ids = {n:int(i) for n,i in re.findall(r'(\w+)\s*=\s*(\d+)', read(server/'Skills/SkillNumber.cs'))}
configured = []
for line in read(server/'VersionSeasonSix/SkillsInitializer.cs').splitlines():
    m = re.search(r'CreateSkill\(SkillNumber\.(\w+),', line)
    if m and m[1] in ids:
        configured.append((ids[m[1]], m[1], 'PassiveBoost' in line, 'CharacterClasses.None' in line))
masters = {int(i):(name,int(base)) for i,name,base in re.findall(r'new\((\d+), "([^"]+)", \d+, \d+, \d+, \d+, (\d+),',read(root/'Client.Main/Core/Utilities/ServerMasterSkills.cs'))}
symbols={}
value=-1
enum=read(reference/'Core/Globals/_enum.h').split('enum ActionSkillType',1)[1].split('};',1)[0]
for line in enum.splitlines():
    line=line.split('//')[0].strip()
    m=re.match(r'(AT_\w+)(?:\s*=\s*(\d+))?\s*,',line)
    if m:
        value=int(m[2]) if m[2] else value+1
        symbols.setdefault(value,[]).append(m[1])
factories={}
for p in (root/'Client.Main/Objects/Effects/Skills').glob('*.cs'):
    text=read(p)
    for m in re.finditer(r'(?:\s*\[(?:Core.Utilities.)?SkillVisualEffect\(\d+\)\][^\n]*\n)+\s*public sealed class (\w+)',text):
        for i in re.findall(r'SkillVisualEffect\((\d+)\)',m[0]):
            factories[int(i)]={'file':str(p),'class':m[1]}
cpps=[p for p in reference.rglob('*.cpp') if p.name in ('ZzzCharacter.cpp','ZzzEffect.cpp','ZzzEffectJoint.cpp','ZzzEffectParticle.cpp','SkillCast.cpp','ClassAttack.cpp','MonkSystem.cpp','WSclient.cpp')]
texts={p:read(p).splitlines() for p in cpps}
rows=[]
for i,name,passive,monster in sorted(set(configured)):
    base=i; seen=set()
    while base in masters and masters[base][1] and base not in seen:
        seen.add(base); base=masters[base][1]
    evidence=[]
    for p,lines in texts.items():
        for j,line in enumerate(lines):
            if any(re.search(r'\b'+s+r'\b',line) for s in symbols.get(base,[])):
                evidence.append({'file':str(p),'line':j+1,'excerpt':'\n'.join(lines[max(0,j-1):j+22])})
    route=factories.get(base)
    status='passive; no cast visual expected' if passive or (i in masters and masters[i][1]==0) else 'registered visual; parity not certified' if route else 'no registry visual; alternate path must be checked'
    rows.append(dict(id=i,name=name,base=base,passive=passive,monster=monster,status=status,route=route,symbols=symbols.get(base,[]),reference=evidence))
out=root/'docs/skill-visual-mumain-inventory.json'
out.write_text(json.dumps(rows,indent=2),encoding='utf-8')
legacy=read(root/'docs/skill-visual-source-audit.md')
findings={}
for block in re.split(r'\n### ',legacy)[1:]:
    title,_,body=block.partition('\n')
    name=title.split(' — ')[0].strip()
    findings[name]=body.split('\nClient:',1)[0].strip()
findings['EvilSpiritSkillEffect']='Four paired spirits match the MuMain cast structure. Motion constants, subtractive blend factors, single scale-1.3 static head, life*.1 brightness, joint expiry, terrain darkness .04..07, floor-based FPS-scaled tail capacity up to 200, tail UVs and pitch signs corrected against MuMain source. Paired live reference and TwinMU capture timelines exist in Publish/SkillComparison/Sheets/skill-9-timeline.png. The corrected heads and tails remain above terrain. Trail shape, brightness, persistent heads versus one-frame MODEL_LASER emissions, lighting falloff and random/timing behavior remain different or unverified. This is not a certified rendering match.'
findings['PoisonSkillEffect']='Live MuMain and TwinMU timelines inspected. Core family is present, but smoke and brightness differ. Terrain light now uses MuMain RGB(.3,1,.6), radius two tiles and core remaining lifetime*.1; the custom pulse and longer smoke-based lighting lifetime were removed. This source correction was rendered again; exact brightness and smoke parity remain unverified.'
findings['LightningSkillEffect']='MuMain player Lightning emits two BITMAP_JOINT_THUNDER subtype-0 ribbons at scales 50 and 10 plus BITMAP_ENERGY. The factory now uses the two source ribbons instead of the custom branching screen-space bolt. First-frame renders, cast cadence, energy/impact particles and terrain light still require validation; this is not a certified match.'
findings['IceSkillEffect']='Core lifetime 50 frames and scale .8 corrected from MuMain. Animation, smoke, alpha fade and randomized shard physics still need comparison; current shards retain custom drift and lighting fade. No paired rendering match has been verified.'
findings['WeaponSkillFlashFactory']='Shiny03 texture, bone-10 offset and sine pulse are present. Sprite projection and weapon trails require rendered comparison; source similarity is not proof of visual parity.'
lines=['# MuMain skill visual comparison inventory', '',
       'Reference: C:/MuOnline_Source/MuMain-main/src/source. This report inventories every skill created by the current Season 6 configuration, including monster, event and passive entries; those are not all player hotbar skills.', '',
       'The comparison distinguishes an existing renderer, a missing registry route, a known implementation difference and an unverified rendered result. No skill is certified pixel-equivalent. Prior detailed findings from the Main 5.2 audit are retained as historical findings, not silently relabelled as newly verified MuMain parity. Direct MuMain cast excerpts are in skill-visual-mumain-inventory.json.', '',
       '## Checked differences directly confirmed against MuMain', '',
       '- Fire Ball: MuMain ZzzCharacter creates MODEL_FIRE subtype 1. Current renderer replaces it with flare/firehik sprites and sparks.',
       '- Twisting Slash: MuMain creates MODEL_SKILL_WHEEL1 and subsequent weapon-shadow wheel effects. Current renderer adds rotating flare arcs, Shiny05 and sparks.',
       '- Nova: MuMain creates MODEL_CIRCLE subtype 1. Current release and charge effects use custom particle compositions.',
       '- Cometfall: the new factory creates two Blast01 models with energy trails and impact parts from MuMain source. Rendering and full parity must be checked separately; registration is not certification.',
       '- Force/Force Wave: MuMain creates two MODEL_WAVES and one MODEL_PIERCING2. No registry factory exists.',
       '- Evil Spirit: see its entry; matching selected motion parameters does not fix the ghost rendering.', '',
       '## Every configured skill', '']
for row in rows:
    lines += [f"### {row['id']} — {row['name']}", '',f"Base route: {row['base']}. {row['status']}."]
    if row['route']:
        route=row['route']; lines += [f"Client factory: [{route['class']}]({route['file'].replace(chr(92),'/')}).",findings.get(route['class'],'Factory exists. Exact movement, render and timing parity has not been demonstrated.')]
    elif row['base']==16:
        lines += ['Soul Barrier has a separate persistent ClassBuffController path; absence of a factory does not mean absence of its aura.']
    elif row['base'] in (6,15):
        lines += ['Teleport is driven by movement/visibility packet handling. A dedicated cast factory is absent; original disappear/appear visuals still need comparison.']
    elif 30 <= row['base'] <= 36:
        lines += ['Summoned creature appearance is a server scope/spawn path, not a normal spell projectile factory. Summon cast visuals are not certified.']
    elif not row['passive'] and row['base']<300:
        lines += ['No generic skill-animation registry effect is available. Any special or buff path must be traced before claiming that this skill has its MuMain effect.']
    if row['symbols']: lines += ['MuMain symbols: '+', '.join('`'+s+'`' for s in row['symbols'])+'.']
    refs=[]
    for e in row['reference']:
        if len(refs)>=5: break
        link=e['file'].replace(chr(92),'/')
        refs.append(f"[{Path(e['file']).name}:{e['line']}]({link}:{e['line']})")
    lines += ['MuMain source: '+(', '.join(refs) if refs else 'No matching symbol occurrence located in the scanned cast/effect sources; do not infer parity.')+'.','']
(root/'docs/skill-visual-mumain-comparison.md').write_text('\n'.join(lines),encoding='utf-8')
print(json.dumps({'skills':len(rows),'active':sum(not r['passive'] for r in rows),'registered_active':sum(bool(r['route']) and not r['passive'] for r in rows),'without_registry':[(r['id'],r['name']) for r in rows if not r['route'] and not r['passive'] and r['base']<300]},indent=2))
