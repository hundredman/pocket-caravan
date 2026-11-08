"""Install the approved small capital-pond appearance. No lake or gameplay data writes."""
from pathlib import Path
import hashlib,json,shutil,subprocess
from PIL import Image

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
ROOT=Path(__file__).resolve().parents[2]
TRIAL=ROOT/'ArtReview/A4-Water-v1';FROZEN=ROOT/'ArtReview/Approved/A4-Pond'
ART=ROOT/'Assets/PocketCaravan/Art/Goryeo/Terrain/Pond'
ASE='/Applications/Aseprite.app/Contents/MacOS/aseprite'

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def freeze():
    FROZEN.mkdir(parents=True,exist_ok=True);(FROZEN/'pieces').mkdir(exist_ok=True)
    source=json.loads((TRIAL/'piece-index.json').read_text());names=[r['piece'] for r in source['cells'] if 'mask' in r]
    paving=sorted({line.split(',')[3]+'.png' for line in (TRIAL/'unity-pond-placement.csv').read_text().splitlines()})
    records=[]
    for relative in ['water-sheet.aseprite','water-sheet.png','piece-index.json','unity-pond-placement.csv']+['pieces/'+n+'.png' for n in names]+['paving/'+n for n in paving]:
        src=(ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/Path(relative).name) if relative.startswith('paving/') else TRIAL/relative;dst=FROZEN/relative;dst.parent.mkdir(parents=True,exist_ok=True)
        if dst.exists() and dst.read_bytes()!=src.read_bytes():raise ValueError('Approved source changed: '+relative)
        if not dst.exists():shutil.copyfile(src,dst)
        records.append(dict(file=relative,sha256=sha(dst)))
    approval=dict(date='2026-10-25',statement='작은 연못 좋은 것 같아. 승인할게.',scope='개경 작은 연못22칸/11개 조합과 기존 돌마당/연꽃만 승인. 47조각은 원본 이력 보존이며 다른 배치/큰 호수/물결 애니메이션은 승인 범위 밖이다.',approvedPlacementCount=22,approvedPieceNames=sorted({line.split(',')[2] for line in (FROZEN/'unity-pond-placement.csv').read_text().splitlines()}),files=records)
    (FROZEN/'approval.json').write_text(json.dumps(approval,ensure_ascii=False,indent=2)+'\n')
    (FROZEN/'approval.md').write_text('# 작은 연못 본편 적용 승인\n\n2026-10-25 사용자: **“작은 연못 좋은 것 같아. 승인할게.”**\n\n직전 비교의 오른쪽 작은 연못 경계를 승인했다. 큰 호수는 기존 표현 유지. 원본47조각은 이력으로 보관하고, 개경22칸/11조합과 기존 돌마당/연꽃만 승인 범위다. 다른 배치는 별도 검토다., 원본 시트의 물결4상태는 참고 보존일 뿐 새 애니메이션 승인/적용으로 취급하지 않는다. approval.json의 해시를 동결한다.\n')
    return names

def install():
    names=freeze();ART.mkdir(parents=True,exist_ok=True);(ART/'pieces').mkdir(exist_ok=True)
    specs={name:dict(waterPiece=name,paving='goryeo_paving_0_0') for name in names};placements=[]
    for line in (FROZEN/'unity-pond-placement.csv').read_text().splitlines():
        x,uy,water,paving=line.split(',');name=water if paving=='goryeo_paving_0_0' else f'{water}_stone_{paving.rsplit("_",2)[-2]}_{paving.rsplit("_",2)[-1]}'
        specs[name]=dict(waterPiece=water,paving=paving);placements.append(dict(x=int(x),y=59-int(uy),piece=name))
    sheet=Image.new('RGBA',(128,((len(specs)+7)//8)*16));pieces={};colors=set()
    for index,(name,spec) in enumerate(specs.items()):
        water=Image.open(FROZEN/'pieces'/(spec['waterPiece']+'.png')).convert('RGBA')
        paving=FROZEN/'paving'/(spec['paving']+'.png')
        ground=Image.open(paving).convert('RGBA');assert ground.size==water.size==(16,16)
        ground.alpha_composite(water);path=ART/'pieces'/(name+'.png');ground.save(path);shutil.copyfile(path,ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/('pond_'+name+'.png'));sheet.alpha_composite(ground,((index%8)*16,(index//8)*16))
        colors.update(p[:3] for p in ground.get_flattened_data());key='pond:'+name
        # Frozen ground module reproduces the approved Ground + water overlay composite exactly.
        pieces[name]=dict(size=[1,1],image=str(path.relative_to(ROOT)),tiles=[dict(dx=0,dy=0,layer='ground',tile=key.replace(':','_'),image='Assets/PocketCaravan/Art/NinjaAdventure/Tiles/'+key.replace(':','_')+'.png')],composition=dict(waterPiece=spec['waterPiece'],paving=str(paving.relative_to(ROOT))))
    sheet.save(ART/'pond-modules.png');run_aseprite([ASE,'-b',str(ART/'pond-modules.png'),'--save-as',str(ART/'pond-modules.aseprite')],check=True)
    reopened=ART/'reopened.png';run_aseprite([ASE,'-b',str(ART/'pond-modules.aseprite'),'--save-as',str(reopened)],check=True)
    assert Image.open(reopened).convert('RGBA').tobytes()==sheet.tobytes();reopened.unlink()
    catalogue=dict(schemaVersion=1,profiles={'pond_native':dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#%02x%02x%02x'%c for c in sorted(colors)])},assets=[dict(id='goryeo_capital_pond',kind='modular',moduleType='water',purpose='capital_pond',profile='pond_native',requiredPieces=list(pieces),pieces=pieces,source=dict(kind='authored',path=str((ART/'pond-modules.aseprite').relative_to(ROOT)),note='Approved authored water composited once with existing AI-processed courtyard paving; no runtime crop/recolor'),review=dict(status='approved',evidence='ArtReview/Approved/A4-Pond/approval.md'))])
    (ART/'catalogue.json').write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+'\n')
    layout=dict(schemaVersion=1,mapId='Gaegyeong',size=[70,60],assetId='goryeo_capital_pond',placements=placements)
    (ROOT/'Tools/MapGen/pond_layout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(dict(preservedSourcePieces=len(names),approvedCells=22,groundVariants=len(specs),placedCells=len(placements),masterReopenedExact=True)))

if __name__=='__main__':install()
