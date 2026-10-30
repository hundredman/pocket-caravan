"""S2b: fixed-light orthogonal ridge modules, geometric/decoded-pixel review."""
from pathlib import Path
import json,hashlib,sys,subprocess,tempfile
from PIL import Image,ImageDraw,ImageFont
import numpy as np
from scipy import ndimage
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/MountainRange-S2b'
EW=ROOT/'ArtReview/Approved/MountainRange-S1'
CURVES=ROOT/'ArtReview/Approved/MountainRange-S2a'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from tiles import autotile
def rgba(p):return Image.open(p).convert('RGBA')
def port(side,offset,w,h):
    return dict(side=side,x=0 if side=='W' else w if side=='E' else offset,
                y=0 if side=='N' else h if side=='S' else offset,template='ew' if side in 'WE' else 'ns')
ASSETS={}
def register(name,path,w,h,ports):
    ASSETS[name]=dict(path=path,size=[w,h],ports={s:port(s,o,w,h) for s,o in ports.items()})
for n,w,p in [('west-cap',128,{'E':0}),('body-a',192,{'W':0,'E':0}),('body-b',64,{'W':0,'E':0}),('east-cap',128,{'W':0})]:
    register(n,f'ArtReview/Approved/MountainRange-S1/{n}.png',w,192,p)
for n,l,r in [('bend-north',64,0),('bend-south',0,64)]:
    register(n,f'ArtReview/Approved/MountainRange-S2a/{n}.png',192,256,{'W':l,'E':r})
for n,h,p in [('north-cap',128,{'S':0}),('north-south-body',192,{'N':0,'S':0}),('south-cap',128,{'N':0})]:
    register(n,f'ArtReview/MountainRange-S2b/Native/{n}.png',192,h,p)
for n,p in [('turn-w-n',{'W':64,'N':64}),('turn-w-s',{'W':0,'S':64}),('turn-e-n',{'E':64,'N':0}),('turn-e-s',{'E':0,'S':0})]:
    register(n,f'ArtReview/MountainRange-S2b/Native/{n}.png',256,256,p)
NEW=list(ASSETS)[6:]
def image(name):return rgba(ROOT/ASSETS[name]['path'])
def connected(a,sa,b,sb,xy=(0,0)):
    p=ASSETS[a]['ports'][sa];q=ASSETS[b]['ports'][sb]
    assert p['template']==q['template'] and {sa,sb} in ({'W','E'},{'N','S'})
    return xy[0]+p['x']-q['x'],xy[1]+p['y']-q['y']
def compose(items):
    x0=min(x for n,x,y in items);y0=min(y for n,x,y in items)
    w=max(x+ASSETS[n]['size'][0] for n,x,y in items)-x0
    h=max(y+ASSETS[n]['size'][1] for n,x,y in items)-y0
    im=Image.new('RGBA',(w,h))
    for n,x,y in items:im.alpha_composite(image(n),(x-x0,y-y0))
    return im,(-x0,-y0)
def fixtures():
    result={}
    for key,name in [('wn','turn-w-n'),('ws','turn-w-s'),('en','turn-e-n'),('es','turn-e-s')]:
        north=key.endswith('n');xy=(128,320 if north else 128);items=[(name,*xy)]
        ew='W' if key.startswith('w') else 'E';end='west-cap' if ew=='W' else 'east-cap'
        endside='E' if ew=='W' else 'W';items.append((end,*connected(name,ew,end,endside,xy)))
        side='N' if north else 'S';other='S' if north else 'N'
        bodyxy=connected(name,side,'north-south-body',other,xy);items.append(('north-south-body',*bodyxy))
        cap='north-cap' if north else 'south-cap'
        items.append((cap,*connected('north-south-body',side,cap,other,bodyxy)))
        notch=(xy[0]+(24 if ew=='W' else 208),xy[1]+(40 if north else 216))
        result[key]=dict(size=[512,592 if north else 720],items=items,notch=notch,
                         view=[16,304 if north else 128,480,270])
    items=[('turn-w-n',320,448),('north-south-body',384,256),('turn-e-s',384,0),
           ('bend-north',128,512),('west-cap',0,576),('east-cap',640,0)]
    result['mixed']=dict(size=[784,784],items=[(n,x+8,y+8) for n,x,y in items],notch=(300,320),view=[0,0,784,784])
    return result
def scene(name,fixture):
    placements=[];width,height=fixture['size']
    def put(path,x,y,order):
        assert (ROOT/path).exists(),path
        placements.append(dict(path=path,x=x,y=y,order=order))
    grass='Assets/PocketCaravan/Art/NinjaAdventure/Tiles/floor_0_12_goryeo_jade.png'
    for y in range(0,height,16):
        for x in range(0,width,16):put(grass,x,y,0)
    for n,x,y in fixture['items']:put(ASSETS[n]['path'],x,y,4)
    px,py=fixture['notch']
    put('Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',px,py,8)
    put('Assets/PocketCaravan/Art/Characters/NPC/npc_naru_well_woman/npc_naru_well_woman_tall_d0.png',px+24,py+2,8)
    if name=='mixed':
        put('ArtReview/Approved/C7-Batch2-v1/forest_round.png',80,160,4)
        put('ArtReview/Approved/C7-LowNature-v1/moss_rock.png',232,360,4)
    im=Image.new('RGBA',(width,height),(122,156,102,255))
    for p in placements:im.alpha_composite(rgba(ROOT/p['path']),(p['x'],p['y']))
    return placements,im
def main():
    for folder in (EW,CURVES):
        for a in json.loads((folder/'approval.json').read_text())['assets']:
            im=rgba(folder/f'{a["name"]}.png')
            assert list(im.size)==a['size'] and hashlib.sha256(im.tobytes()).hexdigest()==a['rgbaSha256']
    templates={'ew':rgba(EW/'port-template.png'),'ns':rgba(OUT/'north-south-port.png')}
    palette=set(zip(*[iter(rgba(ROOT/'ArtReview/Approved/C7-LowNature-v1/moss_rock.png').tobytes())]*4))
    native=[]
    with tempfile.TemporaryDirectory(prefix='s2b-ase-') as temp:
        for name in NEW:
            a=ASSETS[name];im=image(name)
            assert list(im.size)==a['size'] and set(im.getchannel('A').tobytes())<={0,255}
            assert set(zip(*[iter(im.tobytes())]*4))<=palette
            target=Path(temp)/f'{name}.png'
            subprocess.run(['/Applications/Aseprite.app/Contents/MacOS/aseprite','-b',str((ROOT/a['path']).with_suffix('.aseprite')),'--save-as',str(target)],check=True,capture_output=True)
            check=rgba(target);assert check.size==im.size and check.tobytes()==im.tobytes()
            # Every port's central solid pixel belongs to the same connected mountain mass.
            labels,_=ndimage.label(np.array(im.getchannel('A'))>0,structure=np.ones((3,3)));components=[]
            for side,p in a['ports'].items():
                t=np.array(templates[p['template']].getchannel('A'))>0
                if side in 'WE':
                    col=8 if side=='W' else 7;ys=np.flatnonzero(t[:,col]);cross=int(ys[len(ys)//2])
                    x=0 if side=='W' else im.width-1;y=p['y']+cross
                else:
                    row=8 if side=='N' else 7;xs=np.flatnonzero(t[row]);cross=int(xs[len(xs)//2])
                    x=p['x']+cross;y=0 if side=='N' else im.height-1
                components.append(int(labels[y,x]))
            assert components and len(set(components))==1 and components[0]>0,(name,components)
            if name.startswith('turn-'):
                left=name.startswith('turn-w');north=name.endswith('-n')
                box=(16 if left else 192,16 if north else 192,64 if left else 240,64 if north else 240)
                void=np.array(im.getchannel('A').crop(box))==0
                assert void.mean()>=.7,(name,'inner notch filled',void.mean())
            native.append(dict(name=name,size=a['size'],rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest(),
                               asepriteRGBAEqual=True,portsOnSameConnectedMass=True))
    joins=[]
    for an,a in ASSETS.items():
        for aside,p in a['ports'].items():
            if aside not in ('E','S'):continue
            bside='W' if aside=='E' else 'N'
            for bn,b in ASSETS.items():
                if bside not in b['ports'] or b['ports'][bside]['template']!=p['template']:continue
                xy=connected(an,aside,bn,bside);im,shift=compose([(an,0,0),(bn,*xy)])
                x=p['x']+shift[0];y=p['y']+shift[1]
                box=(x-8,y,x+8,y+192) if aside=='E' else (x,y-8,x+192,y+8)
                actual=im.crop(box);expected=templates[p['template']]
                assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),(an,aside,bn)
                joins.append(dict(first=an,side=aside,second=bn,decodedRGBAEqual=True))
    master=rgba(OUT/'north-south-master.png');expected=Image.new('RGBA',(192,448))
    for n,y in [('north-cap',0),('north-south-body',128),('south-cap',320)]:expected.alpha_composite(image(n),(0,y))
    assert master.size==expected.size and master.tobytes()==expected.tobytes()
    views={};captures=[];definitions=fixtures()
    (OUT/'views.tsv').write_text(''.join(n+'\t'+'\t'.join(str(v) for v in f['view'])+'\n' for n,f in definitions.items()))
    for name,f in definitions.items():
        objects,full=scene(name,f)
        (OUT/f'placements-{name}.json').write_text(json.dumps(objects,ensure_ascii=False,indent=2)+'\n')
        (OUT/f'placements-{name}.tsv').write_text(''.join('\t'.join(str(p[k]) for k in ('path','x','y','order'))+'\n' for p in objects))
        x,y,w,h=f['view'];expected=full.crop((x,y,x+w,y+h));expected.save(OUT/f'preview-{name}.png')
        capture=OUT/f'unity-{name}-{w}x{h}.png'
        if capture.exists():
            actual=rgba(capture);assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),name
            captures.append(name);views[name]=actual
        else:views[name]=expected
    board=Image.new('RGB',(992,636),(36,42,34));draw=ImageDraw.Draw(board)
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
    for i,(name,label) in enumerate([('wn','서쪽 ↔ 북쪽 · 안쪽 오목/바깥쪽 볼록'),('en','동쪽 ↔ 북쪽 · 같은 왼쪽 위 빛'),('ws','서쪽 ↔ 남쪽 · 직각 방향 전환'),('es','동쪽 ↔ 남쪽 · 기존 능선과 연결')]):
        x=16+(i%2)*496;y=12+(i//2)*318
        draw.text((x,y),label,font=font,fill='white');board.paste(views[name],(x,y+30))
    board.save(OUT/'game-board.png')
    (OUT/'catalogue.json').write_text(json.dumps(dict(status='pending_visual_approval',productionApplied=False,assets=ASSETS,
        newAssets=NEW,fixtureViews={n:f['view'] for n,f in definitions.items()}),ensure_ascii=False,indent=2)+'\n')
    (OUT/'checks.json').write_text(json.dumps(dict(stage='2b',status='pending_visual_approval',productionApplied=False,
        assets=native,compatiblePortPairs=joins,northSouthMasterRGBAEqual=True,frozenS1S2aPixelsUnchanged=True,
        ninetyDegreeTurns=True,concaveConvexPairedCornerModules=True,independentFilledAreaAutotiles=False,
        unity=dict(mode='Edit',captures=captures,wholeFrameRGBADifferences=0 if captures else None),
        branches=False,passes=False,streams=False,playTest=False),ensure_ascii=False,indent=2)+'\n')
    print('PASS:',len(native),'native/ASE sprites;',len(joins),'port pairs; 4 notches; connected masses; frozen pixels; Unity',len(captures))
if __name__=='__main__':main()
