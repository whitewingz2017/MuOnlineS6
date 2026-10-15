"""Compose actual capture timelines; these sheets do not certify parity."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1] / 'Publish/SkillComparison'
out = root / 'Sheets'
out.mkdir(exist_ok=True)
for folder in sorted((root / 'TwinMU').iterdir()):
    if not folder.is_dir() or folder.name.split('-', 1)[-1] not in ('timed', 'comet-fixed', 'lightning', 'lightning-fixed', 'knight', 'knight-geared', 'elf', 'mg', 'dl', 'dl-corrected', 'summoner', 'rf', 'rf-complete', 'finished'):
        continue
    skill = folder.name.split('-')[0]
    native = root / 'MuMain' / f'{skill}-stable'
    if skill == '13': native = root / 'MuMain/13-in-range'
    if skill == '40': native = root / 'MuMain/40-tap'
    if skill == '16': native = root / 'MuMain/16-self'
    knight = '-knight' in folder.name
    if knight:
        native = root / 'MuMain' / (f'{skill}-knight-self' if skill == '48' else f'{skill}-knight')
    if folder.name.endswith('-knight-geared'):
        native = root / 'MuMain' / folder.name
    elf = folder.name.endswith('-elf')
    if elf:
        native = root / 'MuMain' / (f'{skill}-elf-self' if skill in ('26','27','28','30','77','234') else f'{skill}-elf')
        if skill in ('31','32','33','34','35','36'):
            native = root / 'MuMain' / f'{skill}-elf-single'
    mg = folder.name.endswith('-mg')
    if mg:
        native = root / 'MuMain' / folder.name
        if skill == '10': native = root / 'MuMain/10-mg-single'
        if skill in ('14','237'): native = root / 'MuMain' / f'{skill}-mg-energy-ready'
    dl = folder.name.endswith(('-dl', '-dl-corrected'))
    if dl:
        native = root / 'MuMain' / (f'{skill}-dl-self' if skill in ('63','64','72') else f'{skill}-dl')
    summoner = folder.name.endswith('-summoner')
    if summoner:
        native = root / 'MuMain' / (f'{skill}-summoner-self' if skill in ('217','218') else f'{skill}-summoner')
    rf = folder.name.endswith(('-rf','-rf-complete')) or folder.name.endswith('-finished') and 260 <= int(skill) <= 270
    if rf:
        native = root / 'MuMain' / (f'{skill}-rf-self' if skill in ('266','267','268') else f'{skill}-rf')
        if skill in ('263','264','265','270'):
            native = root / 'MuMain' / f'{skill}-rf-targeted'
    if rf and skill == '269': native = root / 'MuMain/269-rf-single'
    if folder.name == '230-finished': native = root / 'MuMain/230-summoner'
    rows = [sorted(native.glob('*.jpg')), sorted(folder.glob('*.png'))]
    canvas = Image.new('RGB', (1536, 470), '#181b20')
    draw = ImageDraw.Draw(canvas)
    draw.text((10, 8), f'Skill {skill}: MuMain above / TwinMU below. Visual comparison pending.', fill='white')
    draw.text((10, 26), 'Different gear, target models and camera. Native elapsed time includes cast wind-up; TwinMU frames start at effect load.', fill='white')
    for row, files in enumerate(rows):
        if not files:
            draw.text((10, 70 + row * 210), 'NO CAPTURE / NO REGISTRY RENDERER', fill='#ffbe60')
        for column, path in enumerate(files):
            frame = Image.open(path).convert('RGB').resize((256, 192))
            x, y = column * 256, 58 + row * 210
            canvas.paste(frame, (x, y))
            draw.text((x + 3, y + 193), path.stem, fill='white')
    suffix = '-finished' if folder.name.endswith('-finished') else '-rf-complete' if folder.name.endswith('-rf-complete') else '-rf' if rf else '-summoner' if summoner else '-dl' if dl else '-mg' if mg else '-elf' if elf else '-knight-geared' if folder.name.endswith('-knight-geared') else '-knight' if knight else '-lightning' if folder.name.endswith('-lightning') else ''
    if folder.name.endswith('-lightning-fixed'): suffix = '-lightning-fixed'
    if folder.name.endswith('-comet-fixed'): suffix = '-comet-fixed'
    if folder.name.endswith('-dl-corrected'): suffix = '-dl-corrected'
    canvas.save(out / f'skill-{skill}{suffix}-timeline.png')
