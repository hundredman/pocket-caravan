"""Three complete C7 trials submitted together, with per-asset human review."""
import argparse,json,sys
from pathlib import Path
from PIL import Image,ImageDraw
from c7_catalogue import ROOT,rgba_evidence
from c7_palace import COLORS as BASE_BUILDING
BUILDING=BASE_BUILDING+['33475e']
from c7_review import text,sprite
sys.path[:0]=[str(ROOT/'Tools/ArtPrep'),str(ROOT/'Tools/MapGen')]
from prepare_art import prepare
from asset_manifest import validate
OUT=ROOT/'ArtReview/C7-GateProps-20261027'
NATURE=['2e241d','52673c','7d9156','a2b775','68452d','98653b','bc8850']
DEFS=[dict(name='gate',label='성문 · 신규 그림 후보',size=[96,80],bottom=80,oldPath=None,purpose='city_gatehouse',profile='prop',x=1,view=[2,9]),dict(name='well',label='우물 · 앞 시안 재확인',size=[16,32],bottom=32,oldPath='Assets/PocketCaravan/Art/Baked/Objects/c6_well_a3de50d0.png',purpose='village_well',profile='prop',x=9,view=[15,8]),dict(name='stone_lantern',label='석등 · 앞 시안 재확인',size=[16,32],bottom=32,oldPath='Assets/PocketCaravan/Art/Baked/Sprites/lantern_1_2.png',purpose='stone_lantern',profile='prop',x=14,view=[23,8])]
LIGHT=['2e241d','52673c','7d9156','a2b775','77746c','a29b87','e0c99b']
GOLDEN=['2e241d','b78137','daa34a','f0c365','68452d','98653b','bc8850']
PALETTES=dict(prop=BUILDING)
def prep():
    with Image.open(OUT/'Prepared/gate-seed.png') as im:im,report=prepare(im,(96,80),[tuple(bytes.fromhex(c)) for c in BUILDING])
    im.save(OUT/'Prepared/gate.png');(OUT/'Prepared/gate-a5.json').write_text(json.dumps(report,indent=2)+'\n')
def register():
    palette=[''];obj=[0]*160;assets=[];placements=[]
    decisions=OUT/'review-decisions.json';reviews=json.loads(decisions.read_text()) if decisions.exists() else {}
    for d in DEFS:
        n=d['name'];im=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA');w,h=[v//16 for v in d['size']];aid='c7_gateprops_'+n;review=reviews.get(aid,dict(status='pending'))
        if review['status']=='approved':assert review['rgbaEvidence']==rgba_evidence(OUT/'Native'/(n+'.png')),'Approved image changed'
        tiles=[]
        for y in range(h):
            for x in range(w):
                key=aid+f'_{x}_{y}';p=OUT/'Cells'/(key+'.png');im.crop((x*16,y*16,x*16+16,y*16+16)).save(p)
                tiles.append(dict(dx=x,dy=y,layer='obj',tile=key,image=str(p.relative_to(ROOT))))
                palette.append(key);obj[(y+1)*20+d['x']+x]=len(palette)-1
        assets.append(dict(id=aid,kind='object',purpose=d['purpose'],size=[w,h],image=str((OUT/'Native'/(n+'.png')).relative_to(ROOT)),profile=d['profile'],source=dict(kind='ai_processed',path=str((OUT/'Native'/(n+'.aseprite')).relative_to(ROOT))),review=review,tiles=tiles))
        placements.append(dict(id=n,assetId=aid,purpose=d['purpose'],x=d['x'],y=1))
    profiles={k:dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#'+c for c in v]) for k,v in PALETTES.items()}
    doc=dict(schemaVersion=1,profiles=profiles,assets=assets)
    layout=dict(id='c7_gateprops_trial',assetPlacementVersion=1,w=20,h=8,palette=palette,ground=[0]*160,deco=[0]*160,obj=obj,high=[0]*160,road=[0]*160,terrainTags=['grass']*160,assetPlacements=placements)
    errors=validate(doc,[('batch',layout)],ROOT,False);assert not errors,errors
    for n,v in [('catalogue',doc),('layout',layout),('a2',dict(errors=errors,statuses={a['id']:a['review']['status'] for a in assets})),('pixels',{d['name']:rgba_evidence(OUT/'Native'/(d['name']+'.png')) for d in DEFS})]:(OUT/(n+'.json')).write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
    print('A2 preview0errors; '+str(len(assets))+' objects / '+str(sum(len(a['tiles']) for a in assets))+' render cells; '+str({a['id']:a['review']['status'] for a in assets}))
def style():
    states={a['id']:a['review']['status'] for a in json.loads((OUT/'catalogue.json').read_text())['assets']}
    page=Image.new('RGBA',(1120,1150),'#f6f0e3');dr=ImageDraw.Draw(page)
    text(dr,(16,12),'C7 성문 / 우물 / 석등 · 묶음 검토',26)
    for x,label in [(18,'기존 · 6배'),(328,'성문3배 / 소품6배'),(650,'팀 그림체 참고')]:text(dr,(x,54),label,19)
    for d,y in zip(DEFS,[90,410,650]):
        text(dr,(18,y),d['label'],20)

        if d['oldPath']:sprite(page,ROOT/d['oldPath'],(18,y+30),6)
        else:text(dr,(18,y+55),'기존 독립 성문 그림 없음',19)
        sprite(page,OUT/'Native'/(d['name']+'.png'),(328,y+30),3 if d['name']=='gate' else 6)
    for y,n in [(90,'figma-78-11.png'),(405,'figma-92-31.png')]:
        im=Image.open(ROOT/'ArtReview/C7-TeamStyle-20261027/Reference'/n).convert('RGBA');s=min(440/im.width,260/im.height);im=im.resize((round(im.width*s),round(im.height*s)),Image.Resampling.NEAREST);page.alpha_composite(im,(650,y))
    # Complete team board, reference-only scaled view; no pixels enter trial sprites.
    im=Image.open(ROOT/'Docs/Images/team-style-reference.png').convert('RGBA');im=im.crop((0,0,im.width,720));im=im.resize((440,round(im.height*440/im.width)),Image.Resampling.NEAREST);page.alpha_composite(im,(650,790))
    text(dr,(18,950),'새 그림 실제 크기 · 1배',18)
    for d,x in zip(DEFS,[18,240,400]):sprite(page,OUT/'Native'/(d['name']+'.png'),(x,1080-d['size'][1]),1)
    text(dr,(18,1110),'그림별 승인·수정 의견을 한 번에 검토 · 본편 미적용 / 팀 자료의 표시 배율은 동일하지 않음',17)
    page.convert('RGB').save(OUT/'style-comparison.png')
def board():
    style()
    states={a['id']:a['review']['status'] for a in json.loads((OUT/'catalogue.json').read_text())['assets']}
    game=Image.new('RGBA',(1000,480),'#f6f0e3');dr=ImageDraw.Draw(game);text(dr,(14,12),'Unity 실제 Play · 세 그림 묶음 비교',25)
    for x,n,label in [(12,'before','성문 없음 + 기존 소품'),(508,'trial','성문 신규 + 앞 소품 시안')]:
        text(dr,(x,54),label,20);im=Image.open(OUT/'Screens'/(n+'.png')).convert('RGBA');assert im.size==(480,270);game.alpha_composite(im,(x,90))
    text(dr,(14,385),'왼쪽부터 성문 · 우물 · 석등 / 아래 승인 버드나무·가게·인물과 연못은 양쪽 동일',18)
    text(dr,(14,424),'성문96×80 시험 크기·기존 소품 크기 유지·PPU16·Point / 검토 시험장, 본편 배치·보행 검사 아님',18)
    game.convert('RGB').save(OUT/'game-comparison.png')
    (OUT/'submission.json').write_text(json.dumps(dict(statuses={d['name']:states['c7_gateprops_'+d['name']] for d in DEFS},productionApplied=False,files={str(p.relative_to(OUT)):rgba_evidence(p) for p in [OUT/'style-comparison.png',OUT/'game-comparison.png',OUT/'Screens/before.png',OUT/'Screens/trial.png']+[OUT/'Native'/(d['name']+'.png') for d in DEFS]}),indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('step',choices=['prep','register','style','board']);globals()[p.parse_args().step]()
