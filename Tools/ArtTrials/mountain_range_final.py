"""Final preview library, terrain assembly and pixel/connection/clearance validation."""
from pathlib import Path
import json,hashlib,sys,subprocess,tempfile,shutil
from PIL import Image,ImageDraw,ImageFont
import numpy as np
from scipy import ndimage
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/MountainRange-Final'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from tiles import autotile

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
BASE=json.loads((ROOT/'ArtReview/MountainRange-S2b/catalogue.json').read_text())['assets']
A=json.loads(json.dumps(BASE))
def rgba(path):return Image.open(path).convert('RGBA')
def digest(im):return hashlib.sha256(im.tobytes()).hexdigest()
def add(name,w,h,ports):
    p={}
    for side,offset,template in ports:
        p[side]=dict(side=side,x=0 if side=='W' else w if side=='E' else offset,
                     y=0 if side=='N' else h if side=='S' else offset,template=template)
    A[name]=dict(path=f'ArtReview/MountainRange-Final/Native/{name}.png',size=[w,h],ports=p)
add('branch-w-e-n',384,256,[('W',64,'ew'),('E',64,'ew'),('N',96,'ns')])
add('branch-w-e-s',384,256,[('W',0,'ew'),('E',0,'ew'),('S',96,'ns')])
add('branch-n-s-w',256,384,[('N',64,'ns'),('S',64,'ns'),('W',96,'ew')])
add('branch-n-s-e',256,384,[('N',0,'ns'),('S',0,'ns'),('E',96,'ew')])
add('mountain-pass',256,256,[('W',0,'ew'),('E',0,'ew')])
add('stream-valley',256,272,[('W',16,'ew'),('E',16,'ew'),('N',96,'water'),('S',96,'water')])
add('spring-waterfall',256,256,[('W',0,'ew'),('E',0,'ew'),('S',96,'water')])
add('river-straight',64,64,[('N',0,'water'),('S',0,'water')])
add('river-outlet',64,64,[('N',0,'water'),('S',0,'water-ground')])
NEW=list(A)[len(BASE):]
TEMPLATES={'ew':rgba(ROOT/'ArtReview/Approved/MountainRange-S1/port-template.png'),
           'ns':rgba(ROOT/'ArtReview/MountainRange-S2b/north-south-port.png'),
           'water':rgba(OUT/'water-port.png'),'water-ground':rgba(OUT/'water-ground-port.png')}
def image(n):return rgba(ROOT/A[n]['path'])
def attach(an,side,bn,other,xy):
    p=A[an]['ports'][side];q=A[bn]['ports'][other]
    assert p['template']==q['template'] and {side,other} in ({'W','E'},{'N','S'})
    return xy[0]+p['x']-q['x'],xy[1]+p['y']-q['y']
def main_fixture():
    offset=(96,224)
    raw=[('west-cap',-64,384),('mountain-pass',64,384),('body-a',320,384),
         ('branch-w-e-n',512,320),('branch-n-s-e',608,-64),('north-cap',608,-192),
         ('bend-south',864,32),('east-cap',1056,96),('spring-waterfall',896,384),
         ('east-cap',1152,384),('stream-valley',896,640),('west-cap',768,656),
         ('east-cap',1152,656),('river-straight',992,912),('river-outlet',992,976)]
    return dict(size=[1440,1472],items=[(n,x+offset[0],y+offset[1]) for n,x,y in raw])
def branch_fixture(name):
    xy=(128,192);items=[(name,*xy)];w,h=A[name]['size']
    for side in A[name]['ports']:
        cap={'W':'west-cap','E':'east-cap','N':'north-cap','S':'south-cap'}[side]
        other={'W':'E','E':'W','N':'S','S':'N'}[side]
        items.append((cap,*attach(name,side,cap,other,xy)))
    return dict(size=[640,768],items=items)
VIEWS=[('branches-n','main',560,528,480,270,'갈라지는 능선 · 북쪽 갈림'),
       ('branches-e','main',624,208,480,270,'남북 능선과 동쪽 갈림'),
       ('branch-s','branch-w-e-s',80,176,480,270,'남쪽으로 갈라지는 능선'),
       ('branch-w','branch-n-s-w',48,288,480,270,'서쪽으로 갈라지는 능선'),
       ('pass','main',64,576,480,270,'고개 · 기존 흙길 통과'),
       ('spring','main',880,608,480,270,'높은 수원 → 폭포 → 아래 웅덩이'),
       ('valley','main',880,864,480,270,'계곡 · 끊기지 않는 하류 연결'),
       ('river','main',880,1184,480,270,'기존 강 타일로 합류 · 작은 호수'),
       ('overview','main',0,0,1440,1472,'전체 산맥 조립')]
def ground(width,height):
    result=[];water=np.zeros((height//16,width//16),bool);roads=np.zeros_like(water)
    for y in range(water.shape[0]):
        for x in range(water.shape[1]):
            px,py=x*16+8,y*16+8
            water[y,x]=(54<=y<=85 and x in (69,70)) or ((px-1120)/96)**2+((py-1360)/64)**2<=1
        if 0<=y<water.shape[0]:
            cx=17 if 36<=y<=51 else 17+int(round(1.5*np.sin(y/9)))
            roads[y,cx:cx+2]=True
    def mask(a,x,y):return 0<=y<a.shape[0] and 0<=x<a.shape[1] and bool(a[y,x])
    folder='Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    for y in range(water.shape[0]):
        for x in range(water.shape[1]):
            n='floor_0_12'
            if water[y,x]:
                _,tx,ty=autotile(lambda xx,yy:mask(water,xx,yy),x,y,'water',0,6).split(':');n=f'water_{tx}_{ty}'
            elif roads[y,x]:
                _,tx,ty=autotile(lambda xx,yy:mask(roads,xx,yy),x,y,'floor',0,7).split(':');n=f'floor_{tx}_{ty}'
            path=f'{folder}/{n}_goryeo_jade.png'
            assert (ROOT/path).exists(),path
            result.append(dict(path=path,x=x*16,y=y*16,order=0))
    return result,water,roads
def build_scene(name,f):
    width,height=f['size'];placements=[]
    def put(path,x,y,order):
        assert (ROOT/path).exists(),path
        placements.append(dict(path=path,x=x,y=y,order=order))
    if name=='main':placements,water,roads=ground(width,height)
    else:
        water=np.zeros((height//16,width//16),bool);roads=water.copy()
        for y in range(0,height,16):
            for x in range(0,width,16):put('Assets/PocketCaravan/Art/NinjaAdventure/Tiles/floor_0_12_goryeo_jade.png',x,y,0)
    foreground=Image.new('RGBA',(width,height))
    for n,x,y in f['items']:
        put(A[n]['path'],x,y,4);foreground.alpha_composite(image(n),(x,y))
    blocked=np.array(foreground.getchannel('A'))>0
    if name=='main':
        # Context uses approved whole sprites, placed only on free grass and away from the trail.
        trees=['ArtReview/Approved/C7-Batch2-v1/forest_round.png','ArtReview/Approved/C7-Batch2-v1/forest_bent.png','ArtReview/Approved/C7-Batch2-v1/forest_wide.png']
        mountain_blocked=blocked.copy();rng=np.random.default_rng(326)
        candidates=[]
        for cx,cy in [(120,240),(472,240),(120,944),(472,944)]:
            candidates.extend((int(cx+rng.normal(0,40)),int(cy+rng.normal(0,80))) for _ in range(16))
        candidates.sort(key=lambda p:p[1])
        for i,(x,y) in enumerate(candidates):
            p=trees[i%3];im=rgba(ROOT/p);w,h=im.size
            if x<0 or y<0 or x+w>=width or y+h>=height or mountain_blocked[y:y+h,x:x+w].any():continue
            if water[y//16:(y+h+15)//16,x//16:(x+w+15)//16].any() or roads[y//16:(y+h+15)//16,x//16:(x+w+15)//16].any():continue
            put(p,x,y,5);blocked[y:y+h,x:x+w]|=np.array(im.getchannel('A'))>0
        for p,x,y in [('ArtReview/Approved/BuildingShip-Renewal-v1/thatch.png',176,1120),
                       ('ArtReview/Approved/RiceField-v5/small-plot.png',48,1280)]:
            im=rgba(ROOT/p);put(p,x,y,5);blocked[y:y+im.height,x:x+im.width]|=np.array(im.getchannel('A'))>0
    for _,world,x0,y0,w,h,_ in VIEWS:
        if world!=name or w!=480:continue
        candidates=[(x,y) for y in range(y0+24,y0+h-40,16) for x in range(x0+16,x0+w-64,16)]
        candidates.sort(key=lambda p:(p[0]-(x0+60))**2+(p[1]-(y0+h-48))**2)
        found=next(((x,y) for x,y in candidates if not blocked[y:y+32,x:x+56].any() and not water[y//16:(y+47)//16,x//16:(x+71)//16].any()),None)
        assert found,(name,'No free character comparison position')
        x,y=found
        put('Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',x,y,8)
        put('Assets/PocketCaravan/Art/Characters/NPC/npc_naru_well_woman/npc_naru_well_woman_tall_d0.png',x+24,y+2,8)
    placements.sort(key=lambda p:p['order'])
    full=Image.new('RGBA',(width,height),(122,156,102,255));ground_only=full.copy()
    for p in placements:
        im=rgba(ROOT/p['path']);full.alpha_composite(im,(p['x'],p['y']))
        if p['order']==0:ground_only.alpha_composite(im,(p['x'],p['y']))
    return placements,full,foreground,ground_only
def water_mask(im):
    a=np.array(im);m=np.zeros(a.shape[:2],bool)
    for c in ((127,185,175),(120,160,153),(255,255,255)):m|=(a[:,:,:3]==c).all(2)&(a[:,:,3]>0)
    return m
def main():
    frozen=0
    for directory in ('ArtReview/Approved/MountainRange-S1','ArtReview/Approved/MountainRange-S2a'):
        folder=ROOT/directory
        for a in json.loads((folder/'approval.json').read_text())['assets']:
            im=rgba(folder/f'{a["name"]}.png');assert list(im.size)==a['size'] and digest(im)==a['rgbaSha256'];frozen+=1
    for a in json.loads((ROOT/'ArtReview/MountainRange-S2b/checks.json').read_text())['assets']:
        assert digest(image(a['name']))==a['rgbaSha256'];frozen+=1
    palette=set()
    for p in [ROOT/'ArtReview/Approved/C7-LowNature-v1/moss_rock.png',OUT/'water-port.png']:
        palette|=set(zip(*[iter(rgba(p).tobytes())]*4))
    assets=[];package=OUT/'Package';package.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='mountain-final-ase-') as temp:
        for name,a in A.items():
            im=image(name);assert list(im.size)==a['size'] and set(im.getchannel('A').tobytes())<={0,255}
            assert set(zip(*[iter(im.tobytes())]*4))<=palette,name
            target=Path(temp)/f'{name}.png';source=(ROOT/a['path']).with_suffix('.aseprite')
            run_aseprite(['/Applications/Aseprite.app/Contents/MacOS/aseprite','-b',str(source),'--save-as',str(target)],check=True,capture_output=True)
            check=rgba(target);assert check.size==im.size and check.tobytes()==im.tobytes(),name
            shutil.copy2(ROOT/a['path'],package/f'{name}.png');shutil.copy2(source,package/f'{name}.aseprite')
            assert digest(rgba(package/f'{name}.png'))==digest(im)
            assets.append(dict(name=name,size=a['size'],rgbaSha256=digest(im),asepriteRGBAEqual=True,packagePixelsEqual=True))
            if name.startswith('branch-'):
                labels,_=ndimage.label(np.array(im.getchannel('A'))>0,structure=np.ones((3,3)))
                nonzero=np.unique(labels[labels>0]);assert len(nonzero)==1,(name,'Disconnected fork')
    joins=[]
    for an,a in A.items():
        for side,p in a['ports'].items():
            if side not in ('E','S'):continue
            opposite='W' if side=='E' else 'N'
            for bn,b in A.items():
                if opposite not in b['ports'] or b['ports'][opposite]['template']!=p['template']:continue
                xy=attach(an,side,bn,opposite,(0,0));ai=image(an);bi=image(bn)
                ox=-min(0,xy[0]);oy=-min(0,xy[1]);w=max(ai.width,xy[0]+bi.width)+ox;h=max(ai.height,xy[1]+bi.height)+oy
                combined=Image.new('RGBA',(w,h));combined.alpha_composite(ai,(ox,oy));combined.alpha_composite(bi,(xy[0]+ox,xy[1]+oy))
                x=p['x']+ox;y=p['y']+oy;template=TEMPLATES[p['template']]
                box=(x-8,y,x+8,y+template.height) if side=='E' else (x,y-8,x+template.width,y+8)
                actual=combined.crop(box);assert actual.size==template.size and actual.tobytes()==template.tobytes(),(an,bn,side)
                joins.append(dict(first=an,side=side,second=bn,template=p['template'],decodedRGBAEqual=True))
    passage=image('mountain-pass');assert not np.array(passage.getchannel('A'))[:,104:152].any()
    # Regression: passage clearance must not trim the two original inner walls.
    pass_source=np.array(rgba(OUT/'mountain-pass-source-master.png'))
    pass_actual=np.array(passage)
    for source_x,target_x,width in [(64,40,64),(144,152,64)]:
        original=pass_source[:,source_x:source_x+width]
        actual=pass_actual[:,target_x:target_x+width]
        mask=original[:,:,3]>0
        assert np.array_equal(original[mask],actual[mask]),'Pass inner wall was cut'
    wall_master=np.array(rgba(OUT/'mountain-pass-whole-walls-master.png'))
    assert (wall_master[:,:,3]>0).sum()==(pass_source[:,:,3]>0).sum(),'Whole wall source pixels lost'
    rock_image=rgba(ROOT/'ArtReview/Approved/C7-LowNature-v1/moss_rock.png')
    rock_colors=set(zip(*[iter(rock_image.tobytes())]*4))-{(0,0,0,0)}
    original=np.array(rgba(OUT/'stream-valley-source-master.png').crop((64,0,320,256)))[:,40:216]
    actual=np.array(image('stream-valley'))[16:272,40:216]
    mask=np.zeros(original.shape[:2],bool)
    for c in rock_colors:mask|=(original==c).all(2)
    assert np.array_equal(original[mask],actual[mask]),'Valley bank rocks overwritten'
    head=water_mask(image('stream-valley'))[:40]
    widths=[]
    for row in head:
        xs=np.flatnonzero(row);assert len(xs)>0,'Missing inlet water'
        widths.append(int(xs[-1]-xs[0]+1))
    assert max(widths)-min(widths)<=16,'Abrupt inlet widening'
    for name in ('spring-waterfall','stream-valley'):
        m=water_mask(image(name));lab,_=ndimage.label(m,structure=np.ones((3,3)))
        end=set(lab[-1]);end.discard(0)
        assert any((lab[:100]==k).any() and (name!='stream-valley' or (lab[0]==k).any()) for k in end),(name,'Water gap')
    fixtures={'main':main_fixture(),'branch-w-e-s':branch_fixture('branch-w-e-s'),'branch-n-s-w':branch_fixture('branch-n-s-w')}
    worlds={};main_ground=None
    for name,f in fixtures.items():
        objects,full,foreground,ground_only=build_scene(name,f);worlds[name]=full
        (OUT/f'placements-{name}.json').write_text(json.dumps(objects,ensure_ascii=False,indent=2)+'\n')
        (OUT/f'placements-{name}.tsv').write_text(''.join('\t'.join(str(p[k]) for k in ('path','x','y','order'))+'\n' for p in objects))
        if name=='main':
            # Actual alpha clearance through the authored pass, no gameplay claim.
            assert not np.array(foreground.getchannel('A'))[608:864,272:304].any()
            labels,_=ndimage.label(water_mask(full),structure=np.ones((3,3)))
            head=labels[640:708,1096:1144];lake=labels[1328:1392,1088:1152]
            ids=set(head.ravel())&set(lake.ravel());ids.discard(0);assert ids,'Source does not reach lake in decoded pixels'
            outlet=image('river-outlet');joined=Image.new('RGBA',(64,16))
            joined.alpha_composite(outlet.crop((0,56,64,64)),(0,0));joined.alpha_composite(ground_only.crop((1088,1264,1152,1272)),(0,8))
            assert joined.tobytes()==TEMPLATES['water-ground'].tobytes(),'Ground river transition changed'
            main_ground=ground_only
    (OUT/'views.tsv').write_text(''.join('\t'.join(str(v) for v in (name,world,x,y,w,h))+'\n' for name,world,x,y,w,h,_ in VIEWS))
    views={};captures=[]
    for name,world,x,y,w,h,label in VIEWS:
        expected=worlds[world].crop((x,y,x+w,y+h));expected.save(OUT/f'preview-{name}.png')
        capture=OUT/f'unity-{name}-{w}x{h}.png'
        if capture.exists() and '--prepare' not in sys.argv:
            actual=rgba(capture);assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),name
            captures.append(name);views[name]=actual
        else:views[name]=expected
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
    board=Image.new('RGB',(1488,954),(36,42,34));d=ImageDraw.Draw(board)
    for i,(name,world,x,y,w,h,label) in enumerate(VIEWS[:-1]):
        bx=16+(i%3)*496;by=12+(i//3)*318
        d.text((bx,by),label,font=font,fill='white');board.paste(views[name],(bx,by+30))
    d.text((1008,654),'전체 조립은 별도 원본 크기 화면 참고',font=font,fill='white')
    board.save(OUT/'final-game-board.png')
    sheet=Image.new('RGB',(1728,2592),(122,156,102));d=ImageDraw.Draw(sheet)
    for i,(name,a) in enumerate(A.items()):
        x=(i%4)*432;y=(i//4)*432;im=image(name)
        d.text((x+12,y+12),name,font=font,fill='white')
        d.text((x+12,y+38),str(im.width)+' x '+str(im.height),font=font,fill='white')
        sheet.paste(im,(x+12,y+72),im)
    sheet.save(OUT/'all-assets-sheet.png')
    corrections=OUT/'Corrections-v2';before=corrections/'Before'
    if (before/'pass.png').exists() and 'pass' in captures and 'valley' in captures:
        comparison=Image.new('RGB',(992,636),(36,42,34));d=ImageDraw.Draw(comparison)
        for row,(name,label) in enumerate([('pass','고개 암벽'),('valley','계곡 상류 연결')]):
            for col,(im,title) in enumerate([(rgba(before/f'{name}.png'),label+' · 수정 전'),(views[name],label+' · 수정 후')]):
                x=16+col*496;y=12+row*318
                d.text((x,y),title,font=font,fill='white');comparison.paste(im,(x,y+30))
        comparison.save(corrections/'unity-before-after.png')
    approval_path=ROOT/'ArtReview/Approved/MountainRange-Final-v2/approval.json'
    approved=False
    if approval_path.exists():
        frozen_assets=json.loads(approval_path.read_text())['assets']
        approved={a['name']:(a['size'],a['rgbaSha256']) for a in assets}=={a['name']:(a['size'],a['rgbaSha256']) for a in frozen_assets}
    status='approved' if approved else 'pending_final_visual_approval'
    approval_record=str(approval_path.relative_to(ROOT)) if approved else None
    catalogue=dict(status=status,visualApprovalRecord=approval_record,productionApplied=False,assets=A,newAssets=NEW,
        package='Package/',frozenApprovedAssets=['west-cap','body-a','body-b','east-cap','bend-north','bend-south'],
        finalReviewAssets=list(A),pixelSize=16,previewOnly=True)
    (OUT/'catalogue.json').write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+'\n')
    (package/'catalogue.json').write_text(json.dumps(dict(catalogue,assets={n:dict(a,path=n+'.png',source=n+'.aseprite') for n,a in A.items()}),ensure_ascii=False,indent=2)+'\n')
    changed=[]
    baseline=OUT/'Corrections-v2/Before/checks.json'
    if baseline.exists():
        previous={a['name']:a['rgbaSha256'] for a in json.loads(baseline.read_text())['assets']}
        changed=[a['name'] for a in assets if a['rgbaSha256']!=previous[a['name']]]
        assert set(changed)=={'mountain-pass','stream-valley'},changed
    report=dict(status=status,visualApprovalRecord=approval_record,productionApplied=False,assets=assets,compatiblePortPairs=joins,
        preservedEarlierSprites=frozen,passClearancePixels=48,roadCoreClearancePixels=32,
        sourceWaterfallValleyRiverLakeConnectedInRGBA=True,existingGroundRiverPortRGBAEqual=True,
        corrections=dict(passInnerWallPixelsPreserved=True,wholeWallMasterPixelCountPreserved=True,
                         valleyRockBankPixelsPreserved=True,valleyLeadInPixels=16,inletWidthRange=[min(widths),max(widths)],
                         changedAssetImages=changed),
        unity=dict(mode='Edit',captures=captures,wholeFrameRGBADifferences=0 if captures else None),
        runtimeGameplay=False,waterfallAnimation=False,productionMapsUnchanged=True)
    (OUT/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('PASS:',len(assets),'PNG/ASE package pairs;',len(joins),'compatible port pairs; pass clearance; source-to-lake water; ground-river pixels; Unity captures',len(captures))
if __name__=='__main__':main()
