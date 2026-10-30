"""Stage2a: native north/south curves joined to frozen S1 modules."""
from pathlib import Path
import sys,json,hashlib,subprocess,tempfile
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/MountainRange-S2'
FROZEN=ROOT/'ArtReview/Approved/MountainRange-S1'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from tiles import autotile
SPEC={n:(f'ArtReview/Approved/MountainRange-S1/{n}.png',w,192,0,0) for n,w in [('west-cap',128),('body-a',192),('body-b',64),('east-cap',128)]}
SPEC.update({n:(f'ArtReview/MountainRange-S2/Native/{n}.png',192,256,left,right) for n,left,right in [('bend-north',64,0),('bend-south',0,64)]})
SEQUENCES={'reference':['west-cap','body-a','east-cap'],
    'north':['west-cap','bend-north','east-cap'],
    'south':['west-cap','bend-south','east-cap'],
    's-bend':['west-cap','bend-north','body-b','bend-south','east-cap']}
VIEWS=[('reference','reference',0),('north','north',0),('south','south',0),('s-west','s-bend',0),('s-east','s-bend',240)]
def rgba(p):return Image.open(p).convert('RGBA')
def assemble(names):
    records=[];x=0;anchor=0
    for n in names:
        path,w,h,left,right=SPEC[n];y=anchor-left
        records.append(dict(name=n,path=path,x=x,y=y,leftAnchor=anchor,rightAnchor=y+right))
        anchor=y+right;x+=w
    y0=min(p['y'] for p in records);height=max(p['y']+SPEC[p['name']][2] for p in records)-y0
    for p in records:
        p['y']-=y0;p['leftAnchor']-=y0;p['rightAnchor']-=y0
    im=Image.new('RGBA',(x,height))
    for p in records:im.alpha_composite(rgba(ROOT/p['path']),(p['x'],p['y']))
    return im,records
def placements(name):
    mountain,records=assemble(SEQUENCES[name]);width=736 if name=='s-bend' else 480
    result=[]
    def put(path,x,y,order):
        assert (ROOT/path).exists(),path
        result.append(dict(path=path,x=x,y=y,order=order))
    folder='Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    def road(x,y):return y==16
    for y in range(17):
        for x in range(width//16):
            put(f'{folder}/floor_0_12_goryeo_jade.png',x*16,y*16,0)
            if road(x,y):
                _,tx,ty=autotile(road,x,y,'floor',0,7).split(':')
                put(f'{folder}/floor_{tx}_{ty}_goryeo_jade.png',x*16,y*16,1)
    for p in records:put(p['path'],16+p['x'],p['y'],4)
    for x in ([224,544] if name=='s-bend' else [224]):
        put('Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',x,240,8)
        put('Assets/PocketCaravan/Art/Characters/NPC/npc_naru_well_woman/npc_naru_well_woman_tall_d0.png',x+28,242,8)
    return result,width
def render(objects,width):
    im=Image.new('RGBA',(width,270),(122,156,102,255))
    for p in objects:im.alpha_composite(rgba(ROOT/p['path']),(p['x'],p['y']))
    return im
def main():
    for a in json.loads((FROZEN/'approval.json').read_text())['assets']:
        im=rgba(FROZEN/f'{a["name"]}.png')
        assert list(im.size)==a['size'] and hashlib.sha256(im.tobytes()).hexdigest()==a['rgbaSha256']
        original=rgba(ROOT/'ArtReview/MountainRange-S1/Native'/f'{a["name"]}.png')
        assert original.size==im.size and original.tobytes()==im.tobytes()
    port=rgba(FROZEN/'port-template.png');pairs=[]
    for n in ('bend-north','bend-south'):
        pairs.extend((a,n) for a in ('west-cap','body-a','body-b'))
        pairs.extend((n,b) for b in ('body-a','body-b','east-cap'))
    pairs.extend((a,b) for a in ('bend-north','bend-south') for b in ('bend-north','bend-south'))
    joins=[]
    for a,b in pairs:
        im,records=assemble([a,b]);x=SPEC[a][1];anchor=records[0]['rightAnchor']
        assert anchor==records[1]['leftAnchor']
        actual=im.crop((x-8,anchor,x+8,anchor+192))
        assert actual.size==port.size and actual.tobytes()==port.tobytes(),(a,b)
        joins.append(dict(left=a,right=b,portDecodedRGBAEqual=True))
    for name,names in SEQUENCES.items():
        im,records=assemble(names);im.save(OUT/f'assembled-{name}.png')
        (OUT/f'assembly-{name}.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
    palette=set(zip(*[iter(rgba(ROOT/'ArtReview/Approved/C7-LowNature-v1/moss_rock.png').tobytes())]*4));assets=[]
    with tempfile.TemporaryDirectory(prefix='mountain-s2-ase-') as temp:
        for name in ('bend-north','bend-south'):
            im=rgba(ROOT/SPEC[name][0]);assert im.size==(192,256)
            assert set(im.getchannel('A').tobytes())<={0,255} and set(zip(*[iter(im.tobytes())]*4))<=palette
            target=Path(temp)/f'{name}.png'
            subprocess.run(['/Applications/Aseprite.app/Contents/MacOS/aseprite','-b',str((ROOT/SPEC[name][0]).with_suffix('.aseprite')),'--save-as',str(target)],check=True,capture_output=True)
            check=rgba(target);assert check.size==im.size and check.tobytes()==im.tobytes()
            assets.append(dict(name=name,size=list(im.size),rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest(),asepriteRGBAEqual=True))
    worlds={}
    for name in SEQUENCES:
        objects,width=placements(name);worlds[name]=render(objects,width)
        (OUT/f'placements-{name}.json').write_text(json.dumps(objects,ensure_ascii=False,indent=2)+'\n')
        (OUT/f'placements-{name}.tsv').write_text(''.join('\t'.join(str(p[k]) for k in ('path','x','y','order'))+'\n' for p in objects))
    views={};captures=[]
    for view,world,x0 in VIEWS:
        expected=worlds[world].crop((x0,0,x0+480,270));expected.save(OUT/f'preview-{view}.png')
        capture=OUT/f'unity-{view}-480x270.png'
        if capture.exists():
            im=rgba(capture);assert im.size==expected.size and im.tobytes()==expected.tobytes(),view
            captures.append(view);views[view]=im
        else:views[view]=expected
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
    board=Image.new('RGB',(992,636),(36,42,34));d=ImageDraw.Draw(board)
    for i,(view,label) in enumerate([('north','북쪽으로 굽음 · 안쪽/바깥쪽 윤곽'),('south','남쪽으로 굽음 · 같은 빛 방향 유지'),('s-west','직선 + 두 굽힘 조립 · 서쪽'),('s-east','같은 S자 능선 · 동쪽 (겹침 구간 포함)')]):
        x=16+(i%2)*496;y=12+(i//2)*318
        d.text((x,y),label,font=font,fill='white');board.paste(views[view],(x,y+30))
    board.save(OUT/'game-board.png')
    board=Image.new('RGB',(992,318),(36,42,34));d=ImageDraw.Draw(board)
    for i,(view,label) in enumerate([('reference','기준: S1 직선 능선'),('north','신규: 북쪽으로 굽는 연결 조각')]):
        d.text((16+i*496,12),label,font=font,fill='white');board.paste(views[view],(16+i*496,42))
    board.save(OUT/'comparison.png')
    report=dict(stage='2a',status='pending_visual_approval',productionApplied=False,assets=assets,joins=joins,
        s1FrozenPixelsUnchanged=True,sameLightingNoRotation=True,planViewDisplacementPixels=64,
        unity=dict(mode='Edit',captures=captures,wholeFrameRGBADifferences=0 if captures else None),
        ninetyDegreeTurns=False,independentInnerOuterCornerTiles=False,branches=False,valleys=False,playTest=False)
    (OUT/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('PASS: 2 curve sprites; 16 connections exact RGBA; S1 frozen images unchanged; ASE2 round trips; Unity captures',len(captures))
if __name__=='__main__':main()
