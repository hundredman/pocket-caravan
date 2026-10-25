"""Check the approved composite and exact allowed map differences; write A2 replacement projection."""
from pathlib import Path
import hashlib,json,subprocess,sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/Validation/A4-Pond-Production-2026-10-25'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
import world_registry,pond_art

def check():
    OUT.mkdir(parents=True,exist_ok=True)
    before_path=OUT/'Gaegyeong-before.json'
    if not before_path.exists():
        before_path.write_bytes(subprocess.check_output(['git','show','570e5f7b:Assets/PocketCaravan/Maps/Gaegyeong.json'],cwd=ROOT))
    before=json.loads(before_path.read_text());after=json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text());layout=json.loads(pond_art.LAYOUT.read_text());positions={(p['x'],p['y']):p['piece'] for p in layout['placements']}
    visual={'palette','ground','deco','obj','high'}
    assert {k:v for k,v in before.items() if k not in visual}=={k:v for k,v in after.items() if k not in visual}
    changes=[]
    for layer in ('ground','deco','obj','high'):
        for index,(a,b) in enumerate(zip(before[layer],after[layer])):
            ka,kb=before['palette'][a],after['palette'][b]
            if ka!=kb:changes.append((layer,index%after['w'],index//after['w']))
    assert set(changes)=={('ground',x,y) for x,y in positions} and len(changes)==22
    for (x,y),piece in positions.items():
        module=pond_art.pieces()[piece];composition=module['composition']
        im=Image.open(ROOT/composition['paving']).convert('RGBA');im.alpha_composite(Image.open(ROOT/'ArtReview/Approved/A4-Pond/pieces'/(composition['waterPiece']+'.png')).convert('RGBA'))
        actual=Image.open(ROOT/module['image']).convert('RGBA');assert im.tobytes()==actual.tobytes()
        key=after['palette'][after['ground'][y*after['w']+x]];assert key=='pond_'+piece
        assert im.tobytes()==Image.open(ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/(key+'.png')).convert('RGBA').tobytes()
    frozen=ROOT/'ArtReview/Approved/A4-Pond';approval=json.loads((frozen/'approval.json').read_text())
    for r in approval['files']:assert hashlib.sha256((frozen/r['file']).read_bytes()).hexdigest()==r['sha256']
    entries,maps=world_registry.atlas_content();m=next(m for m in maps if m.id=='Gaegyeong');palette=['']+sorted({'pond_'+p['piece'] for p in layout['placements']});ground=[0]*(m.w*m.h)
    for (x,y),name in positions.items():ground[y*m.w+x]=palette.index('pond_'+name)
    projection=dict(id=m.id,w=m.w,h=m.h,palette=palette,ground=ground,deco=[0]*(m.w*m.h),obj=[0]*(m.w*m.h),high=[0]*(m.w*m.h),assetPlacementVersion=1,assetPlacements=m.approved_pond_placements,coverage='Approved replacement22 cells only; remaining legacy art not catalogue-migrated')
    (OUT/'replacement-projection.json').write_text(json.dumps(projection,separators=(',',':'))+'\n')
    report=dict(sourceBefore='570e5f7b',changedMap='Gaegyeong',changedVisualCells=22,otherLayersExact=True,collisionWaterRoadReachabilityObjectsAndIdsExact=True,nativeCompositeMatchesApprovedLayers=True,approvalHashesExact=True,largeLakeTilesChanged=False,coverage='Only approved 22-cell replacement',sourceApproval=approval['statement'])
    (OUT/'static-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))

if __name__=='__main__':check()
