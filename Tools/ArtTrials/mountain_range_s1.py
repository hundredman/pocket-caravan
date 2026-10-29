"""Stage-one straight ridge review and decoded-pixel connection checks."""
from pathlib import Path
import json,sys,hashlib,subprocess,tempfile
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/MountainRange-S1'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from tiles import autotile
WIDTHS={'west-cap':128,'body-a':192,'body-b':64,'east-cap':128}
SHORT=['west-cap','body-a','east-cap']
LONG=['west-cap','body-a','body-b','body-a','body-b','east-cap']
def rgba(p):return Image.open(p).convert('RGBA')
def piece(n):return rgba(OUT/'Native'/f'{n}.png')
def assemble(names):
    im=Image.new('RGBA',(sum(WIDTHS[n] for n in names),192));x=0
    for n in names:im.alpha_composite(piece(n),(x,0));x+=WIDTHS[n]
    return im
def placements(name):
    result=[]
    def put(path,x,y,order):
        assert (ROOT/path).exists(),path
        result.append(dict(path=path,x=x,y=y,order=order))
    width=800 if name=='long' else 480
    def road(x,y):return y in (13,14)
    folder='Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    for y in range(17):
        for x in range(width//16):
            put(f'{folder}/floor_0_12_goryeo_jade.png',x*16,y*16,0)
            if road(x,y):
                _,tx,ty=autotile(road,x,y,'floor',0,7).split(':')
                put(f'{folder}/floor_{tx}_{ty}_goryeo_jade.png',x*16,y*16,1)
    if name=='reference':
        for n,x,y in [('mountain',16,8),('ridge',216,40),('foothill',250,128)]:
            put(f'ArtReview/Approved/RockMountain-v1/{n}.png',x,y,4)
    else:
        x=16
        for n in SHORT if name=='short' else LONG:
            put(f'ArtReview/MountainRange-S1/Native/{n}.png',x,0,4);x+=WIDTHS[n]
    for x in ([232,552] if name=='long' else [232]):
        put('Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',x,196,8)
        put('Assets/PocketCaravan/Art/Characters/NPC/npc_naru_well_woman/npc_naru_well_woman_tall_d0.png',x+28,198,8)
    return result,width
def render(objects,width):
    im=Image.new('RGBA',(width,270),(122,156,102,255))
    for p in objects:im.alpha_composite(rgba(ROOT/p['path']),(p['x'],p['y']))
    return im
VIEWS=[('reference','reference',0),('short','short',0),('long-west','long',0),('long-east','long',304)]
def main():
    port=rgba(OUT/'port-template.png')
    junctions=[]
    for a,b in [('west-cap','body-a'),('west-cap','body-b'),('body-a','body-a'),('body-a','body-b'),('body-b','body-a'),('body-b','body-b'),('body-a','east-cap'),('body-b','east-cap')]:
        im=assemble([a,b]);joint=im.crop((WIDTHS[a]-8,0,WIDTHS[a]+8,192))
        assert joint.size==port.size and joint.tobytes()==port.tobytes(),(a,b)
        junctions.append(dict(left=a,right=b,decodedRGBAMatchesPort=True))
    # A solid shared rock core crosses every join; the transparent silhouette is outside it.
    spans=[port.getchannel('A').crop((x,0,x+1,192)).getbbox() for x in range(16)]
    top=max(b[1] for b in spans)+8;bottom=min(b[3] for b in spans)-8
    assert bottom-top>=16 and set(port.getchannel('A').crop((0,top,16,bottom)).tobytes())=={255}
    master=rgba(OUT/'master.png');reassembled=assemble(list(WIDTHS))
    assert master.size==reassembled.size and master.tobytes()==reassembled.tobytes()
    assemble(SHORT).save(OUT/'assembled-short.png');assemble(LONG).save(OUT/'assembled-long.png')
    palette=set(zip(*[iter(rgba(ROOT/'ArtReview/Approved/C7-LowNature-v1/moss_rock.png').tobytes())]*4))
    assets=[]
    with tempfile.TemporaryDirectory(prefix='ridge-s1-ase-') as temp:
        for name,width in WIDTHS.items():
            p=OUT/'Native'/f'{name}.png';im=rgba(p)
            assert im.size==(width,192) and set(im.getchannel('A').tobytes())<={0,255}
            assert set(zip(*[iter(im.tobytes())]*4))<=palette
            target=Path(temp)/f'{name}.png'
            subprocess.run(['/Applications/Aseprite.app/Contents/MacOS/aseprite','-b',str(p.with_suffix('.aseprite')),'--save-as',str(target)],check=True,capture_output=True)
            check=rgba(target);assert check.size==im.size and check.tobytes()==im.tobytes()
            assets.append(dict(name=name,size=list(im.size),rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest(),asepriteRGBAEqual=True))
    worlds={}
    for name in ('reference','short','long'):
        objects,width=placements(name);worlds[name]=render(objects,width)
        (OUT/f'placements-{name}.json').write_text(json.dumps(objects,ensure_ascii=False,indent=2)+'\n')
        (OUT/f'placements-{name}.tsv').write_text(''.join('\t'.join(str(p[k]) for k in ('path','x','y','order'))+'\n' for p in objects))
    board=Image.new('RGB',(992,636),(36,42,34));d=ImageDraw.Draw(board)
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
    labels=['기준: 승인된 독립 암반 3종','1단계: 서쪽 끝 + 중앙 A + 동쪽 끝','중앙 A/B 반복 확장 · 서쪽 화면','같은 긴 능선 · 동쪽 화면 (겹침 구간 포함)']
    captures=[]
    for i,(view,world,x0) in enumerate(VIEWS):
        expected=worlds[world].crop((x0,0,x0+480,270));expected.save(OUT/f'preview-{view}.png')
        capture=OUT/f'unity-{view}-480x270.png'
        if capture.exists():
            actual=rgba(capture);assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),view
            captures.append(view);im=actual
        else:im=expected
        x=16+(i%2)*496;y=12+(i//2)*318
        d.text((x,y),labels[i],font=font,fill='white');board.paste(im,(x,y+30))
    board.save(OUT/'game-board.png')
    (OUT/'checks.json').write_text(json.dumps(dict(status='pending_visual_approval',stage=1,productionApplied=False,
        purpose='west-east straight modular ridge',assets=assets,junctions=junctions,solidPortCoreRows=[top,bottom],
        masterDecodedRGBAEqualsAssembly=True,shortWidth=448,longWidth=768,
        unity=dict(mode='Edit',captures=captures,wholeFrameRGBADifferences=0 if captures else None),
        turns=False,branches=False,valleys=False,playTest=False),ensure_ascii=False,indent=2)+'\n')
    print('PASS: 4 native modules; 8 join types exact RGBA; solid cross-seam core; master assembly; 4 ASE round trips; Unity captures',len(captures))
if __name__=='__main__':main()
