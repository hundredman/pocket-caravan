"""Three complete C7 trials submitted together, with per-asset human review."""
import argparse,json,sys
from pathlib import Path
from PIL import Image,ImageDraw
from c7_catalogue import ROOT,rgba_evidence
from c7_palace import COLORS as BASE_BUILDING
BUILDING=BASE_BUILDING+['33475e','304d47']
from c7_review import text,sprite
sys.path[:0]=[str(ROOT/'Tools/ArtPrep'),str(ROOT/'Tools/MapGen')]
from prepare_art import prepare
from asset_manifest import validate
OUT=ROOT/'ArtReview/C7-WorkshopProps-v3-20261027'
NATURE=['2e241d','52673c','7d9156','a2b775','68452d','98653b','bc8850']
DEFS=[dict(name='paper_rack',label='한지 건조대',size=[48,32],bottom=31,oldPath='Assets/PocketCaravan/Art/Baked/Objects/c6_paper_rack_665314e5.png',purpose='paper_drying_rack',profile='prop',x=1,view=[3,8]),dict(name='harvest',label='수확 꾸러미',size=[48,32],bottom=31,oldPath='Assets/PocketCaravan/Art/Baked/Objects/c6_goryeo_harvest_168ed569.png',purpose='harvest_goods',profile='grain',x=7,view=[14,8]),dict(name='celadon',label='청자 진열 소품',size=[48,32],bottom=31,oldPath='Assets/PocketCaravan/Art/Baked/Objects/c6_goryeo_celadon_54def069.png',purpose='celadon_display',profile='jade',x=14,view=[23,8])]
LIGHT=['2e241d','52673c','7d9156','a2b775','77746c','a29b87','e0c99b']
GOLDEN=['2e241d','b78137','daa34a','f0c365','68452d','98653b','bc8850']
PALETTES=dict(prop=BUILDING,grain=['2e241d','b78137','daa34a','f0c365','68452d','98653b','bc8850','e0c99b','f4dfb5'],jade=['2e241d','304d47','547e6a','86a28b','b6cfac','68452d','98653b','bc8850'])
def register():
    palette=[''];obj=[0]*160;assets=[];placements=[]
    decisions=OUT/'review-decisions.json';reviews=json.loads(decisions.read_text()) if decisions.exists() else {}
    for d in DEFS:
        n=d['name'];im=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA');w,h=[v//16 for v in d['size']];aid='c7_workshopv3_'+n;review=reviews.get(aid,dict(status='pending'))
        if review['status']=='approved':assert review['rgbaEvidence']==rgba_evidence(OUT/'Native'/(n+'.png')),'Approved image changed'
        tiles=[]
        for y in range(h):
            for x in range(w):
                key=aid+f'_{x}_{y}';p=OUT/'Cells'/(key+'.png');im.crop((x*16,y*16,x*16+16,y*16+16)).save(p)
                tiles.append(dict(dx=x,dy=y,layer='obj',tile=key,image=str(p.relative_to(ROOT))))
                palette.append(key);obj[(y+1)*20+d['x']+x]=len(palette)-1
        assets.append(dict(id=aid,kind='object',purpose=d['purpose'],size=[w,h],image=str((OUT/'Native'/(n+'.png')).relative_to(ROOT)),profile=d['profile'],source=dict(kind='authored' if n!='paper_rack' else 'ai_processed',path=str((OUT/'Native'/(n+'.aseprite')).relative_to(ROOT))),review=review,tiles=tiles))
        placements.append(dict(id=n,assetId=aid,purpose=d['purpose'],x=d['x'],y=1))
    profiles={k:dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#'+c for c in v]) for k,v in PALETTES.items()}
    doc=dict(schemaVersion=1,profiles=profiles,assets=assets)
    layout=dict(id='c7_workshopv3_trial',assetPlacementVersion=1,w=20,h=8,palette=palette,ground=[0]*160,deco=[0]*160,obj=obj,high=[0]*160,road=[0]*160,terrainTags=['grass']*160,assetPlacements=placements)
    errors=validate(doc,[('batch',layout)],ROOT,False);assert not errors,errors
    for n,v in [('catalogue',doc),('layout',layout),('a2',dict(errors=errors,statuses={a['id']:a['review']['status'] for a in assets})),('pixels',{d['name']:rgba_evidence(OUT/'Native'/(d['name']+'.png')) for d in DEFS})]:(OUT/(n+'.json')).write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
    print('A2 preview0errors; '+str(len(assets))+' objects / '+str(sum(len(a['tiles']) for a in assets))+' render cells; '+str({a['id']:a['review']['status'] for a in assets}))
def style():
    page=Image.new('RGBA',(1120,1000),'#f6f0e3');dr=ImageDraw.Draw(page)
    text(dr,(16,12),'볏단 두께 / 청자 입구 정리 · 크기 유지',26)
    for x,label in [(18,'기존 게임'),(385,'직전 시안'),(750,'두께·입구 수정안')]:text(dr,(x,58),label,21)
    for d,y in [(DEFS[1],110),(DEFS[2],390)]:
        text(dr,(18,y),d['label']+' · 모두6배',19)
        for x,p in [(18,ROOT/d['oldPath']),(385,ROOT/'ArtReview/C7-WorkshopProps-v2-20261027/Native'/(d['name']+'.png')),(750,OUT/'Native'/(d['name']+'.png'))]:
            sprite(page,p,(x,y+40),6)
        for x,p in [(18,ROOT/d['oldPath']),(385,ROOT/'ArtReview/C7-WorkshopProps-v2-20261027/Native'/(d['name']+'.png')),(750,OUT/'Native'/(d['name']+'.png'))]:
            sprite(page,p,(x,y+245),1)
    text(dr,(18,750),'한지 건조대 · 이전 시안 픽셀 그대로 유지',21)
    sprite(page,OUT/'Native/paper_rack.png',(18,800),4)
    sprite(page,OUT/'Native/paper_rack.png',(250,880),1)
    text(dr,(420,795),'48×32 캔버스·발밑 유지 / 두 부위만 네이티브 수정',18)
    text(dr,(420,835),'실제 크기1배는 각 확대 그림 아래에 표시',18)
    text(dr,(18,950),'본편 미적용 · 물품 크기·형태 검토 대기 · 공통 팔레트 변경 아님',18)
    page.convert('RGB').save(OUT/'style-comparison.png')
def board():
    style()
    states={a['id']:a['review']['status'] for a in json.loads((OUT/'catalogue.json').read_text())['assets']}
    game=Image.new('RGBA',(1000,480),'#f6f0e3');dr=ImageDraw.Draw(game);text(dr,(14,12),'Unity 실제 Play · 세 그림 묶음 비교',25)
    for x,n,label in [(12,'before','직전 시안'),(508,'trial','볏단·입구 수정 · 건조대 유지')]:
        text(dr,(x,54),label,20);im=Image.open(OUT/'Screens'/(n+'.png')).convert('RGBA');assert im.size==(480,270);game.alpha_composite(im,(x,90))
    text(dr,(14,385),'왼쪽부터 한지 건조대 · 수확 꾸러미 · 청자 소품 / 승인 건물·인물·연못은 양쪽 동일',18)
    text(dr,(14,424),'같은 크기 캔버스·발밑·PPU16·Point / 검토 시험장, 본편 배치·보행 검사 아님',18)
    game.convert('RGB').save(OUT/'game-comparison.png')
    (OUT/'submission.json').write_text(json.dumps(dict(statuses={d['name']:states['c7_workshopv3_'+d['name']] for d in DEFS},productionApplied=False,files={str(p.relative_to(OUT)):rgba_evidence(p) for p in [OUT/'style-comparison.png',OUT/'game-comparison.png',OUT/'Screens/before.png',OUT/'Screens/trial.png']+[OUT/'Native'/(d['name']+'.png') for d in DEFS]}),indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('step',choices=['register','style','board']);globals()[p.parse_args().step]()
