"""Prepare/register a complete isolated gaekju trial. Approval stays human input."""
from pathlib import Path
import argparse,json,sys
from PIL import Image,ImageDraw
from c7_catalogue import ROOT,rgba_evidence
from c7_team_prepare import HOUSE
from c7_review import text,sprite
sys.path[:0]=[str(ROOT/'Tools/ArtPrep'),str(ROOT/'Tools/MapGen')]
from prepare_art import prepare
from asset_manifest import validate
OUT=ROOT/'ArtReview/C7-Gaekju-20261027'
COLORS=HOUSE+['794b3b','ad624c','d69062','547e6a','86a28b']
def prep():
    with Image.open(OUT/'Prepared/gaekju-seed.png') as im:im,report=prepare(im,(96,64),[tuple(bytes.fromhex(c)) for c in COLORS])
    im.save(OUT/'Prepared/gaekju.png')
    (OUT/'a5.json').write_text(json.dumps(report,indent=2)+'\n')
    (OUT/'candidate-palette.txt').write_text('// Trial only; not D3 approval\n'+'\n'.join('#'+c for c in COLORS)+'\n')
def register():
    im=Image.open(OUT/'Native/gaekju.png').convert('RGBA');tiles=[];palette=[''];obj=[0]*48
    for y in range(4):
        for x in range(6):
            key=f'c7_gaekju_{x}_{y}';p=OUT/'Cells'/(key+'.png')
            im.crop((x*16,y*16,x*16+16,y*16+16)).save(p)
            tiles.append(dict(dx=x,dy=y,layer='obj',tile=key,image=str(p.relative_to(ROOT))))
            palette.append(key);obj[(y+1)*8+x+1]=len(palette)-1
    asset=dict(id='c7_gaekju_candidate',kind='object',purpose='merchant_lodging_candidate',size=[6,4],image=str((OUT/'Native/gaekju.png').relative_to(ROOT)),profile='gaekju',source=dict(kind='ai_processed',path=str((OUT/'Native/gaekju.aseprite').relative_to(ROOT))),review=dict(status='pending'),tiles=tiles)
    decision=OUT/'review-decision.json'
    if decision.exists():
        saved=json.loads(decision.read_text())
        assert saved['rgbaEvidence']==rgba_evidence(OUT/'Native/gaekju.png'),'Approved pixels changed; create new trial instead'
        asset['review']=saved
    doc=dict(schemaVersion=1,profiles=dict(gaekju=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#'+c for c in COLORS])),assets=[asset])
    layout=dict(id='c7_gaekju_trial',assetPlacementVersion=1,w=8,h=6,palette=palette,ground=[0]*48,deco=[0]*48,obj=obj,high=[0]*48,road=[0]*48,terrainTags=['grass']*48,assetPlacements=[dict(id='gaekju',assetId='c7_gaekju_candidate',purpose='merchant_lodging_candidate',x=1,y=1)])
    errors=validate(doc,[('gaekju',layout)],ROOT,False);assert not errors,errors
    for name,value in [('catalogue',doc),('layout',layout),('a2',dict(errors=errors,approval=asset['review']['status'])),('pixels',rgba_evidence(OUT/'Native/gaekju.png'))]:(OUT/(name+'.json')).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n')
    print('A2: whole gaekju 24 cells, preview0errors; art status '+asset['review']['status'])
def board():
    decision=OUT/'review-decision.json'
    label='그림 승인' if decision.exists() and json.loads(decision.read_text())['status']=='approved' else '검토 대기'
    # Reference images are shown complete, never copied into the trial asset.
    old=ROOT/'Assets/PocketCaravan/Art/Baked/Sprites/house_6_4.png'
    page=Image.new('RGBA',(1480,850),'#f6f0e3');d=ImageDraw.Draw(page)
    text(d,(18,12),'C7 객주 후보 · 기존 / 수정안 / 팀 그림체 참고',26)
    for x,label in [(18,'기존 큰 기와집 · 공용'),(518,'새 객주 후보 · '+label)]:text(d,(x,58),label,20)
    sprite(page,old,(18,105),4);sprite(page,OUT/'Native/gaekju.png',(518,105),4)
    text(d,(1020,58),'팀 자료 전체 이미지',20)
    for y,n in [(95,'figma-78-11.png'),(395,'figma-92-31.png')]:
        im=Image.open(ROOT/'ArtReview/C7-TeamStyle-20261027/Reference'/n).convert('RGBA')
        scale=min(430/im.width,270/im.height)
        im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.NEAREST);page.alpha_composite(im,(1020,y))
    text(d,(18,455),'고려 객주 후보은 동일 96×64 캔버스 · 위 그림은 정수 4배 확대',19)
    text(d,(18,495),'승인된 기와집 · 인물과 실제 도트 크기 비교',20)
    sprite(page,OUT/'Native/gaekju.png',(18,588),1)
    sprite(page,ROOT/'ArtReview/Approved/C7-Buildings-v5/tile-house.png',(212,604),1)
    sprite(page,ROOT/'Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',(306,628),1)
    text(d,(18,705),'참고 자료는 전체 비율을 유지해 표시했으며, 팀 자료의 원본 도트 배율은 동일하지 않을 수 있습니다.',17)
    text(d,(18,746),'원래 땅·승인된 인물/집은 유지 · 객주 후보은 본편 미적용 · 후보 팔레트는 D3 확정 아님',18)
    page.convert('RGB').save(OUT/'style-comparison.png')
    game=Image.new('RGBA',(1000,480),'#f6f0e3');d=ImageDraw.Draw(game)
    text(d,(14,12),'Unity 실제 Play 카메라 · 객주 후보 전후',25)
    for x,name,label in [(12,'before','기존 큰 기와집 · 공용'),(508,'trial','새 객주 후보 · '+label)]:
        text(d,(x,54),label,20)
        im=Image.open(OUT/'Screens'/(name+'.png')).convert('RGBA');assert im.size==(480,270)
        game.alpha_composite(im,(x,90))
    text(d,(14,385),'같은 지형·위치·인물·승인된 가게/궁궐 / 연못 v13은 양쪽 동일한 검토 시안',18)
    text(d,(14,424),'객주 후보 6×4칸 · PPU16 / Point · 검토용 시험장, 본편 배치·보행 검사 아님',18)
    game.convert('RGB').save(OUT/'game-comparison.png')
    (OUT/'submission.json').write_text(json.dumps(dict(status=json.loads((OUT/'review-decision.json').read_text())['status'] if (OUT/'review-decision.json').exists() else 'pending',productionApplied=False,files={p.name:rgba_evidence(p) for p in [OUT/'Native/gaekju.png',OUT/'Screens/before.png',OUT/'Screens/trial.png',OUT/'style-comparison.png',OUT/'game-comparison.png']}),indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('step',choices=['prep','register','board']);globals()[p.parse_args().step]()
