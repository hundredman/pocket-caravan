"""Whole-rock prototypes and deterministic review placements, never production."""
from pathlib import Path
import sys,json,hashlib,subprocess,tempfile
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/RockMountain-v1'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from tiles import autotile

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
NAMES=('reference','mountain','ridge','forest')
SIZES={'mountain':(192,160),'ridge':(256,112),'foothill':(128,80)}
def rgba(path):return Image.open(path).convert('RGBA')
def layout(name):
    placements=[]
    def put(path,x,y,order):
        assert (ROOT/path).exists(),path
        placements.append(dict(path=path,x=x,y=y,order=order))
    def rock(kind,x,y):put(f'ArtReview/RockMountain-v1/Native/{kind}.png',x,y,y+SIZES[kind][1])
    def tree(kind,x,y):
        path=f'ArtReview/Approved/C7-Batch2-v1/{kind}.png'
        put(path,x,y,y+rgba(ROOT/path).height)
    def moss(x,y):put('ArtReview/Approved/C7-LowNature-v1/moss_rock.png',x,y,y+32)
    folder='Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    def road(x,y):
        return ((y in (11,12) and (name!='forest' or x<21)) or
                (name=='forest' and x in (19,20) and y>=11)) if name!='reference' else False
    for y in range(17):
        for x in range(30):
            put(f'{folder}/floor_0_12_goryeo_jade.png',x*16,y*16,0)
            if road(x,y):
                _,tx,ty=autotile(road,x,y,'floor',0,7).split(':')
                put(f'{folder}/floor_{tx}_{ty}_goryeo_jade.png',x*16,y*16,1)
    if name=='reference':
        moss(210,148);rock('mountain',12,8);rock('ridge',216,24);rock('foothill',250,160)
        tree('forest_round',412,176)
    elif name=='mountain':
        rock('mountain',160,4);rock('foothill',120,94)
        moss(350,128);moss(84,140);tree('forest_round',48,68);tree('forest_bent',400,80)
    elif name=='ridge':
        # One complete escarpment, no artificial repetition or concealed image seam.
        rock('ridge',128,48);rock('foothill',58,92)
        moss(390,130);moss(32,142);tree('forest_round',422,94)
    else:
        # Intact foreground trees overlap rocks using their foot positions.
        for row in range(3):
            for col in range(4):
                tree(('forest_bent','forest_wide','forest_round')[(row+col)%3],46+col*30+(row%2)*12,28+row*34)
        rock('mountain',250,0);rock('foothill',190,90);moss(170,144)
        tree('forest_bent',410,112)
    put('Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',240,174,198)
    put('Assets/PocketCaravan/Art/Characters/NPC/npc_naru_well_woman/npc_naru_well_woman_tall_d0.png',272,176,200)
    return sorted(placements,key=lambda p:p['order'])
def render(placements):
    im=Image.new('RGBA',(480,270),(122,156,102,255))
    for p in placements:im.alpha_composite(rgba(ROOT/p['path']),(p['x'],p['y']))
    return im
def main():
    assets=[]
    original=rgba(ROOT/'ArtReview/Approved/C7-LowNature-v1/moss_rock.png')
    palette=set(zip(*[iter(original.tobytes())]*4))
    with tempfile.TemporaryDirectory(prefix='rock-ase-') as temp:
        for name,size in SIZES.items():
            p=OUT/'Native'/f'{name}.png';im=rgba(p)
            assert im.size==size and set(im.getchannel('A').tobytes())<={0,255}
            assert set(zip(*[iter(im.tobytes())]*4))<=palette
            target=Path(temp)/f'{name}.png'
            run_aseprite(['/Applications/Aseprite.app/Contents/MacOS/aseprite','-b',str(p.with_suffix('.aseprite')),'--save-as',str(target)],check=True,capture_output=True)
            check=rgba(target);assert check.size==size and check.tobytes()==im.tobytes()
            assets.append(dict(name=name,size=list(size),occupied=list(im.getbbox()),rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest(),asepriteRGBAEqual=True))
    captures=[]
    for name in NAMES:
        placements=layout(name)
        (OUT/f'placements-{name}.json').write_text(json.dumps(placements,ensure_ascii=False,indent=2)+'\n')
        (OUT/f'placements-{name}.tsv').write_text(''.join('\t'.join(str(p[k]) for k in ('path','x','y','order'))+'\n' for p in placements))
        expected=render(placements);expected.save(OUT/f'preview-{name}.png')
        path=OUT/f'unity-{name}-480x270.png'
        if path.exists():
            actual=rgba(path);assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),name
            captures.append(name)
    board=Image.new('RGB',(992,636),(36,42,34));d=ImageDraw.Draw(board)
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
    labels=['기존 바위·나무와 새 암반 크기 비교','① 초원 위 독립 돌산','② 길 옆 낮은 암벽','③ 숲과 맞닿은 산자락']
    for i,(name,label) in enumerate(zip(NAMES,labels)):
        x=16+(i%2)*496;y=12+(i//2)*318
        path=OUT/f'unity-{name}-480x270.png'
        if not path.exists():path=OUT/f'preview-{name}.png'
        d.text((x,y),label,font=font,fill='white');board.paste(rgba(path),(x,y+30))
    board.save(OUT/'game-board.png')
    report=dict(status='pending_visual_approval',productionApplied=False,source='built-in imagegen, Aseprite native cleanup',
                wholeObjects=True,assets=assets,originalRockPalette=True,binaryAlpha=True,
                unity=dict(mode='Edit',captures=captures,wholeFrameRGBADifferences=0 if captures else None),
                modularConnectionsImplemented=False,playTest=False)
    (OUT/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('PASS: 3 native whole sprites, original rock colors, binary alpha, ASE RGBA round trips; captures:',len(captures))
if __name__=='__main__':main()
