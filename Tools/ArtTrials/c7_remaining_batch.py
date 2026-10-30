"""All12 known remaining whole-object variants; review-only, no live binding."""
import argparse,hashlib,json,sys,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
from c7_catalogue import ROOT,rgba_evidence
from c7_review import text,sprite
sys.path[:0]=[str(ROOT/'Tools/ArtPrep'),str(ROOT/'Tools/MapGen')]
from prepare_art import prepare
from asset_manifest import validate
OUT=ROOT/'ArtReview/C7-RemainingBatch-20261027'
BASE='Assets/PocketCaravan/Art/Baked/Objects/'
NATURE=['2e241d','52673c','7d9156','a2b775','68452d','98653b','bc8850']
GRAIN=['2e241d','b78137','daa34a','f0c365','68452d','98653b','bc8850','e0c99b','f4dfb5']
PALETTES=dict(wood=['2e241d','68452d','98653b','bc8850','c4a779','e0c99b'],grain=GRAIN,grain_naru=GRAIN+['7d9156'],laundry=['2e241d','68452d','98653b','bc8850','547e6a','86a28b','304d47','794b3b','ad624c','d69062','e0c99b'],nature=NATURE,building=['2e241d','b78137','daa34a','f0c365','68452d','98653b','bc8850','c4a779','e0c99b','f4dfb5','77746c','a29b87','c4bca7','33475e','4a627a','718da3','a5b9c3','547e6a','86a28b'],rock=['2e241d','77746c','a29b87','c4bca7'],flower=['2e241d','52673c','7d9156','a2b775','9b5168','d68194','f2bac3','f4dfb5'])
ROWS=[
('boat','나룻배','c6_boat_3258f818',[80,32],[77,30],30,'wood','wooden_skiff',0,[2,7]),
('sheaf_naru','볏단 · 나루형','c6_sheaf_c21644db',[16,32],[16,25],31,'grain_naru','single_rice_sheaf',0,[11,8]),
('laundry','빨래대','c6_laundry_4c045117',[32,32],[32,21],30,'laundry','clothesline',0,[18,8]),
('sheaf_jangdan','볏단 · 장단형','c6_sheaf_5994fbae',[16,32],[16,25],31,'grain','single_rice_sheaf',0,[26,8]),
('pine_west','소나무 묶음 · 기존 밝은형','c6_pinebig_a6c49380',[64,48],[64,47],47,'nature','pine_cluster',1,[1,8]),
('round_west','둥근 나무 묶음 · 기존 밝은형','c6_roundbig_0c978872',[64,48],[64,47],47,'nature','broadleaf_cluster',1,[8,8]),
('small_store','작은 가게','c6_store_499ea1ea',[64,48],[64,48],48,'building','small_shop_building',1,[16,8]),
('small_rock','작은 바위','c6_module_rock_15917f3a',[32,32],[31,22],32,'rock','small_boulder',1,[25,8]),
('pine_east','소나무 묶음 · 기존 올리브형','c6_pinebig_0cd79e57',[64,48],[64,47],47,'nature','pine_cluster',2,[1,8]),
('round_east','둥근 나무 묶음 · 기존 올리브형','c6_roundbig_b8a4adcf',[64,48],[64,47],47,'nature','broadleaf_cluster',2,[8,8]),
('bench','낮은 벤치','c6_bench_ac9f82d8',[48,16],[48,16],16,'wood','backless_bench',2,[17,8]),
('lotus','연꽃 장식','c6_lotus',[32,16],[31,15],16,'flower','lotus_water_decoration',2,[25,8])]
DEFS=[dict(name=n,label=l,oldAssetId=aid,oldPath=BASE+('lotus_2_1' if aid=='c6_lotus' else aid)+'.png',size=size,maxVisible=maximum,bottom=bottom,profile=profile,purpose=purpose,page=page,view=view)for n,l,aid,size,maximum,bottom,profile,purpose,page,view in ROWS]
def prep():
    for d in DEFS:
        n=d['name']
        with Image.open(OUT/'Prepared'/(n+'-seed.png'))as im:im,report=prepare(im,tuple(d['size']),[tuple(bytes.fromhex(c))for c in PALETTES[d['profile']]])
        im.save(OUT/'Prepared'/(n+'.png'));(OUT/'Prepared'/(n+'-a5.json')).write_text(json.dumps(report,indent=2)+'\n')
def register():
    W,H=30,18;palette=[''];obj=[0]*(W*H);assets=[];placements=[]
    reviews=json.loads((OUT/'review-decisions.json').read_text())if(OUT/'review-decisions.json').exists()else{}
    for i,d in enumerate(DEFS):
        n=d['name'];im=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA');w,h=[v//16 for v in d['size']];aid='c7_remaining_'+n;tiles=[];ox,oy=1+7*(i%4),1+6*(i//4)
        review=reviews.get(aid,dict(status='pending'))
        if review['status']=='approved':assert review['rgbaEvidence']==rgba_evidence(OUT/'Native'/(n+'.png')),'Approved pixels changed'
        for y in range(h):
            for x in range(w):
                key=aid+f'_{x}_{y}';p=OUT/'Cells'/(key+'.png');im.crop((16*x,16*y,16*x+16,16*y+16)).save(p)
                palette.append(key);obj[(oy+y)*W+ox+x]=len(palette)-1;tiles.append(dict(dx=x,dy=y,layer='obj',tile=key,image=str(p.relative_to(ROOT))))
        assets.append(dict(id=aid,kind='object',purpose=d['purpose'],size=[w,h],profile=d['profile'],image=str((OUT/'Native'/(n+'.png')).relative_to(ROOT)),source=dict(kind='authored' if n.startswith('sheaf_')else'ai_processed',path=str((OUT/'Native'/(n+'.aseprite')).relative_to(ROOT))),review=review,tiles=tiles))
        placements.append(dict(id=n,assetId=aid,purpose=d['purpose'],x=ox,y=oy))
    doc=dict(schemaVersion=1,profiles={k:dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#'+c for c in v])for k,v in PALETTES.items()},assets=assets)
    layout=dict(id='remaining_art_review_only',assetPlacementVersion=1,w=W,h=H,palette=palette,obj=obj,ground=[0]*(W*H),deco=[0]*(W*H),high=[0]*(W*H),road=[0]*(W*H),terrainTags=['grass']*(W*H),assetPlacements=placements)
    errors=validate(doc,[('review',layout)],ROOT,False);assert not errors,errors
    for name,v in [('catalogue',doc),('layout',layout),('a2',dict(errors=errors,statuses={a['id']:a['review']['status']for a in assets})),('pixels',{d['name']:rgba_evidence(OUT/'Native'/(d['name']+'.png'))for d in DEFS})]:(OUT/(name+'.json')).write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
    # Source for the isolated Unity renderer, never a live WorldIndex/scene binding.
    (OUT/'render-plan.json').write_text(json.dumps(dict(assets=[dict(name=d['name'],page=d['page'],x=d['view'][0],y=d['view'][1],before=d['oldPath'],after=str((OUT/'Native'/(d['name']+'.png')).relative_to(ROOT)))for d in DEFS]),indent=2)+'\n')
    print('A2 preview0errors;',len(assets),'objects;',sum(len(a['tiles'])for a in assets),'render cells')
def board():
    page=Image.new('RGBA',(1680,1620),'#f6f0e3');dr=ImageDraw.Draw(page);text(dr,(18,12),'C7 잔여9종·12원본 일괄 검토 · 왼쪽 기존 / 오른쪽 수정',27)
    for i,d in enumerate(DEFS):
        x=16+(i%3)*554;y=72+(i//3)*364;w,h=d['size'];scale=min(6,240//w,190//h)
        text(dr,(x,y),f"{i+1}. {d['label']} · {scale}배",20)
        sprite(page,ROOT/d['oldPath'],(x,y+35),scale);sprite(page,OUT/'Native'/(d['name']+'.png'),(x+270,y+35),scale)
        text(dr,(x,y+238),'실제 크기1배 · 기존 / 수정',16)
        sprite(page,ROOT/d['oldPath'],(x,y+266),1);sprite(page,OUT/'Native'/(d['name']+'.png'),(x+270,y+266),1)
    text(dr,(18,1550),'모두 기존 캔버스·발밑·PPU16 유지 · 개별 승인 대기 · 팀 공용 파일/본편 미변경',20)
    page.convert('RGB').save(OUT/'style-all.png')
    reference=Image.open(ROOT/'Docs/Images/team-style-reference.png').convert('RGBA');reference.crop((0,0,reference.width,720)).save(OUT/'team-reference.png')
    game=Image.new('RGBA',(1000,1190),'#f6f0e3');gd=ImageDraw.Draw(game);text(gd,(14,12),'독립 Unity 검토 카메라480×270 · Play/실제 이동 미실행',23)
    labels=['1~4. 배·볏단2형·빨래대','5~8. 나무2형·작은 가게·바위','9~12. 나무2형·벤치·연꽃']
    for p,label in enumerate(labels):
        y=65+p*350;text(gd,(14,y),f'{label} · 왼쪽 기존 / 오른쪽 수정',20)
        for x,mode in [(12,'before'),(508,'trial')]:
            im=Image.open(OUT/'Screens'/f'page{p}_{mode}.png').convert('RGBA');assert im.size==(480,270);game.alpha_composite(im,(x,y+36))
    text(gd,(14,1130),'물색 면은 배·연꽃 가독성용 배경 · 맵·지형·통행·충돌 검사가 아님',18)
    game.convert('RGB').save(OUT/'camera-all.png')
    (OUT/'submission.json').write_text(json.dumps(dict(statuses={a['id']:a['review']['status']for a in json.loads((OUT/'catalogue.json').read_text())['assets']},productionApplied=False,playModeRun=False,files={str(p.relative_to(OUT)):rgba_evidence(p)for p in [OUT/'style-all.png',OUT/'camera-all.png']+[OUT/'Native'/(d['name']+'.png')for d in DEFS]}),ensure_ascii=False,indent=2)+'\n')
def protected_path(p):
    return any(p.startswith('Assets/PocketCaravan/Scripts/'+d+'/')for d in ['Core','Economy','Quests','UI'])or p.startswith(('Assets/PocketCaravan/Resources/GameDatabase','Assets/PocketCaravan/Resources/WorldIndex','Assets/PocketCaravan/Scenes/Transit/','Assets/PocketCaravan/World/Connections','Assets/PocketCaravan/Data/Origins/','ProjectSettings/EditorBuildSettings'))
def protect_check():
    # Session-only evidence, deliberately NOT a historical shared-data CI pin.
    data=json.loads((OUT/'protected-before.json').read_text());changed=[]
    current={p for p in subprocess.check_output(['git','ls-files','-c','-o','--exclude-standard','-z'],cwd=ROOT).decode().split('\0')if protected_path(p)and(ROOT/p).is_file()}
    added=sorted(current-set(data['files']))
    for p,evidence in data['files'].items():
        file=ROOT/p
        actual=None if not file.exists()else rgba_evidence(file)if p.lower().endswith('.png')else hashlib.sha256(file.read_bytes()).hexdigest()
        if actual!=evidence:changed.append(p)
    result=dict(base=data['base'],phase='own-work-before-final-main-refresh',checkedFiles=len(data['files']),changed=changed,added=added)
    (OUT/'protected-check.json').write_text(json.dumps(result,indent=2)+'\n');assert not(changed or added),(changed,added);print('Protected files unchanged:',len(data['files']))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('step',choices=['prep','register','board','protect_check']);globals()[p.parse_args().step]()
