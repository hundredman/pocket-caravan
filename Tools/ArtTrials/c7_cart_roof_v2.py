"""Paired cart geometry and roof trial; never modifies approved production art."""
import argparse,json,sys
from PIL import Image,ImageDraw
from c7_catalogue import ROOT,rgba_evidence
from c7_review import text,sprite
from c7_team_prepare import HOUSE
sys.path[:0]=[str(ROOT/'Tools/ArtPrep'),str(ROOT/'Tools/MapGen')]
from prepare_art import prepare
from asset_manifest import validate
OUT=ROOT/'ArtReview/C7-CartRoof-v2-20261027'
from c7_batch4 import BUILDING
COLORS=list(dict.fromkeys(HOUSE+BUILDING+['33475e']))
DEFS=[dict(name='cart',label='수레',size=[32,32],old='ArtReview/C7-CartRoof-20261027/Native/cart.png',purpose='handcart',x=1,kind='ai_processed'),dict(name='tile-house',label='기와집 지붕',size=[64,48],old='ArtReview/C7-CartRoof-20261027/Native/tile-house.png',purpose='house',x=5,kind='authored')]
def register():
    palette=[''];obj=[0]*72;assets=[];placements=[]
    decisions=json.loads((OUT/'review-decisions.json').read_text()) if (OUT/'review-decisions.json').exists() else {}
    for d in DEFS:
        n=d['name'];im=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA');w,h=[v//16 for v in d['size']];aid='c7_cr_'+n.replace('-','_');review=decisions.get(aid,dict(status='pending'));tiles=[]
        if review['status']=='approved':assert review['rgbaEvidence']==rgba_evidence(OUT/'Native'/(n+'.png')),'Approved pixels changed'
        for y in range(h):
            for x in range(w):
                key=aid+f'_{x}_{y}';p=OUT/'Cells'/(key+'.png');im.crop((16*x,16*y,16*x+16,16*y+16)).save(p)
                palette.append(key);obj[(y+1)*12+d['x']+x]=len(palette)-1
                tiles.append(dict(dx=x,dy=y,layer='obj',tile=key,image=str(p.relative_to(ROOT))))
        assets.append(dict(id=aid,kind='object',purpose=d['purpose'],size=[w,h],profile='candidate',image=str((OUT/'Native'/(n+'.png')).relative_to(ROOT)),source=dict(kind=d['kind'],path=str((OUT/'Native'/(n+'.aseprite')).relative_to(ROOT))),review=review,tiles=tiles))
        placements.append(dict(id=n,assetId=aid,purpose=d['purpose'],x=d['x'],y=1))
    doc=dict(schemaVersion=1,profiles=dict(candidate=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#'+c for c in COLORS])),assets=assets)
    layout=dict(id='cart_roof_trial',assetPlacementVersion=1,w=12,h=6,palette=palette,obj=obj,ground=[0]*72,deco=[0]*72,high=[0]*72,road=[0]*72,terrainTags=['grass']*72,assetPlacements=placements)
    errors=validate(doc,[('trial',layout)],ROOT,False);assert not errors,errors
    for n,v in [('catalogue',doc),('layout',layout),('a2',dict(errors=errors,statuses={a['id']:a['review']['status'] for a in assets}))]:(OUT/(n+'.json')).write_text(json.dumps(v,indent=2)+'\n')
def board():
    page=Image.new('RGBA',(1120,1040),'#f6f0e3');dr=ImageDraw.Draw(page)
    text(dr,(16,12),'수레 처음 시안 복원 / 기와집 용마루 색 정리',26)
    for x,s in [(20,'이전'),(380,'새 시안 · 검토 대기'),(760,'팀 스타일 참고')]:text(dr,(x,55),s,21)
    for d,y,scale in [(DEFS[0],100,8),(DEFS[1],390,5)]:
        text(dr,(20,y),d['label'],19)
        for x,p in [(20,ROOT/d['old']),(380,OUT/'Native'/(d['name']+'.png'))]:sprite(page,p,(x,y+28),scale)
    ref=Image.open(ROOT/'Docs/Images/team-style-reference.png').convert('RGBA')
    # Upper board contains the actual Venice/Arabia references, lower section is legacy art.
    ref=ref.crop((0,0,ref.width,720));s=340/ref.width;ref=ref.resize((340,round(ref.height*s)),Image.Resampling.NEAREST);page.alpha_composite(ref,(760,100))
    text(dr,(760,340),'같은 도트 격자 · PPU16',17);text(dr,(760,375),'건물 외곽·벽·문은 유지',17)
    text(dr,(760,410),'수레: 첫 소품 묶음 그림 복원',17);text(dr,(760,445),'지붕: 흰 윗선 → 청회색 명암',17)
    text(dr,(760,510),'복원·수정 원본 실제 크기 · 1배',17)
    sprite(page,OUT/'Native/cart.png',(780,555),1)
    sprite(page,OUT/'Native/tile-house.png',(860,539),1)
    text(dr,(20,680),'아래: 실제 Unity Play 카메라 · 480×270 원본 크기',19)
    for x,n in [(20,'before'),(600,'trial')]:
        im=Image.open(OUT/'Screens'/(n+'.png')).convert('RGBA');assert im.size==(480,270);page.alpha_composite(im,(x,720))
    text(dr,(20,1005),'본편 미적용 · 두 그림 각각 승인 대기 · 우물/석등/인물/지형 유지 · 보행 검사는 아님',17)
    page.convert('RGB').save(OUT/'comparison.png')
    (OUT/'submission.json').write_text(json.dumps(dict(productionApplied=False,statuses={a['id']:a['review']['status'] for a in json.loads((OUT/'catalogue.json').read_text())['assets']},files={str(p.relative_to(OUT)):rgba_evidence(p) for p in [OUT/'Native/cart.png',OUT/'Native/tile-house.png',OUT/'Screens/before.png',OUT/'Screens/trial.png',OUT/'comparison.png']}),indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('step',choices=['register','board']);globals()[p.parse_args().step]()
