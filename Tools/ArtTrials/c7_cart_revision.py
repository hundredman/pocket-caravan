"""Cart-only proportion revision; other batch4 props remain byte/pixel untouched."""
import argparse,json,sys
from PIL import Image,ImageDraw
from c7_catalogue import ROOT,rgba_evidence
from c7_batch4 import BUILDING
from c7_review import text,sprite
sys.path[:0]=[str(ROOT/'Tools/ArtPrep'),str(ROOT/'Tools/MapGen')]
from prepare_art import prepare
from asset_manifest import validate
OUT=ROOT/'ArtReview/C7-CartRevision-20261027';PREVIOUS=ROOT/'ArtReview/C7-Batch4-20261027'
def prep():
    with Image.open(OUT/'Prepared/cart-seed.png') as im:im,report=prepare(im,(32,32),[tuple(bytes.fromhex(c)) for c in BUILDING])
    im.save(OUT/'Prepared/cart.png');(OUT/'a5.json').write_text(json.dumps(report,indent=2)+'\n')
def register():
    im=Image.open(OUT/'Native/cart.png').convert('RGBA');tiles=[];palette=[''];obj=[0]*16
    for y in range(2):
        for x in range(2):
            key=f'c7_cartrev_{x}_{y}';p=OUT/'Cells'/(key+'.png');im.crop((16*x,16*y,16*x+16,16*y+16)).save(p)
            palette.append(key);obj[(y+1)*4+x+1]=len(palette)-1;tiles.append(dict(dx=x,dy=y,layer='obj',tile=key,image=str(p.relative_to(ROOT))))
    decision=OUT/'review-decision.json'
    review=json.loads(decision.read_text()) if decision.exists() else dict(status='pending')
    if review['status']=='approved':assert review['rgbaEvidence']==rgba_evidence(OUT/'Native/cart.png'),'Approved image changed'
    doc=dict(schemaVersion=1,profiles=dict(prop=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#'+c for c in BUILDING])),assets=[dict(id='c7_cartrev_cart',kind='object',purpose='handcart',size=[2,2],profile='prop',image=str((OUT/'Native/cart.png').relative_to(ROOT)),source=dict(kind='ai_processed',path=str((OUT/'Native/cart.aseprite').relative_to(ROOT))),review=review,tiles=tiles)])
    layout=dict(id='cart_revision',assetPlacementVersion=1,w=4,h=4,palette=palette,obj=obj,ground=[0]*16,deco=[0]*16,high=[0]*16,road=[0]*16,terrainTags=['grass']*16,assetPlacements=[dict(id='cart',assetId='c7_cartrev_cart',purpose='handcart',x=1,y=1)])
    errors=validate(doc,[('cart',layout)],ROOT,False);assert not errors,errors
    for n,v in [('catalogue',doc),('layout',layout),('a2',dict(errors=errors,approval=review['status'])),('pixels',rgba_evidence(OUT/'Native/cart.png'))]:(OUT/(n+'.json')).write_text(json.dumps(v,indent=2)+'\n')
def board():
    page=Image.new('RGBA',(1000,960),'#f6f0e3');d=ImageDraw.Draw(page);text(d,(16,12),'수레 비례 수정 · 같은 방향·색·캔버스',26)
    for x,folder,label in [(60,PREVIOUS,'이전 시안'),(550,OUT,'수정 시안 · 검토 대기')]:
        text(d,(x,65),label,21);sprite(page,folder/'Native/cart.png',(x,105),12)
    text(d,(16,505),'위: 32×32 원본의 정수12배 확대 / 아래: 실제 Unity 게임 크기 480×270',18)
    for x,n in [(12,'before'),(508,'trial')]:
        im=Image.open(OUT/'Screens'/(n+'.png')).convert('RGBA');assert im.size==(480,270);page.alpha_composite(im,(x,550))
    text(d,(16,850),'바퀴·적재함·손잡이 관계를 소폭 수정. 우물·석등·집·인물·지형은 같은 그림입니다.',18)
    text(d,(16,895),'본편 미적용 · 수레 그림 승인 대기 · 실제 보행·충돌 검사 아님',18)
    page.convert('RGB').save(OUT/'comparison.png')
    (OUT/'submission.json').write_text(json.dumps(dict(status=json.loads((OUT/'review-decision.json').read_text())['status'] if (OUT/'review-decision.json').exists() else 'pending',productionApplied=False,files={str(p.relative_to(OUT)):rgba_evidence(p) for p in [OUT/'Native/cart.png',OUT/'Screens/before.png',OUT/'Screens/trial.png',OUT/'comparison.png']}),indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('step',choices=['prep','register','board']);globals()[p.parse_args().step]()
