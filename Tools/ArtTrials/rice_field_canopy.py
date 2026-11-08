"""Preview rice canopy assemblies. Production files are never written."""
from pathlib import Path
import sys,json,hashlib,subprocess,tempfile
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/RiceField-v5'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from tiles import autotile

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
NAMES=[['north-west','north','north-east'],['west','center','east'],['south-west','south','south-east']]
def rgba(path):return Image.open(path).convert('RGBA')
def signature(path):
    im=rgba(path)
    return dict(size=list(im.size),rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest())
def assemble(nx,ny):
    canvas=Image.new('RGBA',(nx*64+64,ny*64+64))
    for y in range(ny):
        for x in range(nx):
            gx=0 if x==0 else 2 if x==nx-1 else 1
            gy=0 if y==0 else 2 if y==ny-1 else 1
            tile=rgba(OUT/'Native'/f'{NAMES[gy][gx]}.png')
            canvas.alpha_composite(tile,(32+x*64-(32 if x==0 else 0),48+y*64-(48 if y==0 else 0)))
    return canvas
def layout(revision):
    placements=[]
    def put(path,x,y,order=0):
        assert (ROOT/path).exists(),path
        placements.append(dict(path=path,x=x,y=y,order=order))
    folder='Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    # Grass revision removes only the soil beds; paths and whole crops stay fixed.
    def dirt(x,y):
        return (x in (14,15) or y==8 or (revision!='grass' and
                ((2<=x<=11 or 18<=x<=27) and (1<=y<=6 or 10<=y<=15))))
    for y in range(17):
        for x in range(30):
            put(f'{folder}/floor_0_12_goryeo_jade.png',x*16,y*16)
            if dirt(x,y):
                key=autotile(dirt,x,y,'floor',0,7);_,tx,ty=key.split(':')
                put(f'{folder}/floor_{tx}_{ty}_goryeo_jade.png',x*16,y*16,1)
    name='previous-small-plot' if revision=='before' else 'small-plot'
    for x,y in [(16,-8),(272,-8),(16,136),(272,136)]:
        put(f'ArtReview/RiceField-v5/{name}.png',x,y,4)
    put('Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',232,116,8)
    put('Assets/PocketCaravan/Art/Characters/NPC/npc_naru_well_woman/npc_naru_well_woman_tall_d0.png',264,118,8)
    return sorted(placements,key=lambda p:p['order'])
def render(placements):
    im=Image.new('RGBA',(480,270),(122,156,102,255))
    for p in placements:im.alpha_composite(rgba(ROOT/p['path']),(p['x'],p['y']))
    return im
def boards():
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',22)
    board=Image.new('RGB',(1952,606),(36,42,34));d=ImageDraw.Draw(board)
    for i,label in enumerate(['이전: 벼 포기만 반복','수정: 이삭 면 + 앞쪽 줄기 · 논둑 사이 길']):
        name=['before','after'][i]
        path=OUT/f'unity-{name}-480x270.png'
        if not path.exists():path=OUT/f'preview-{name}.png'
        im=rgba(path).resize((960,540),Image.Resampling.NEAREST)
        d.text((16+i*976,12),label,font=font,fill='white');board.paste(im,(16+i*976,50))
    board.save(OUT/'comparison.png')
    grass=OUT/'unity-grass-480x270.png'
    if grass.exists():
        board=Image.new('RGB',(1952,606),(36,42,34));d=ImageDraw.Draw(board)
        for i,(path,label) in enumerate([(OUT/'unity-after-480x270.png','이전: 벼 아래 흙 바닥'),(grass,'수정: 초원 위 벼 · 구획 사이 길 유지')]):
            im=rgba(path).resize((960,540),Image.Resampling.NEAREST)
            d.text((16+i*976,12),label,font=font,fill='white');board.paste(im,(16+i*976,50))
        board.save(OUT/'grass-comparison.png')
def main():
    checks=[]
    for nx,ny in [(2,2),(3,3),(5,2),(2,5),(4,3)]:
        expected=rgba(OUT/'Reference'/f'{nx*64}x{ny*64}.png');actual=assemble(nx,ny)
        assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),(nx,ny)
        checks.append(dict(blocks=[nx,ny],decodedRGBAEqual=True))
    for name in ('before','after','grass'):
        placements=layout(name)
        (OUT/f'placements-{name}.json').write_text(json.dumps(placements,ensure_ascii=False,indent=2)+'\n')
        (OUT/f'placements-{name}.tsv').write_text(''.join('\t'.join(str(p[k]) for k in ('path','x','y','order'))+'\n' for p in placements))
        expected=render(placements);expected.save(OUT/f'preview-{name}.png')
        capture=OUT/f'unity-{name}-480x270.png'
        if capture.exists():
            actual=rgba(capture)
            assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),name
    native=rgba(OUT/'canopy.png')
    assert native.size==(64,48)
    assert set(native.getchannel('A').tobytes())<={0,255}
    colors=set(zip(*[iter(native.tobytes())]*4))-{(0,0,0,0)}
    assert len(colors)<=6
    sources=list(OUT.rglob('*.aseprite'))
    with tempfile.TemporaryDirectory(prefix='rice-canopy-ase-') as temp:
        for i,source in enumerate(sources):
            target=Path(temp)/f'{i}.png'
            run_aseprite(['/Applications/Aseprite.app/Contents/MacOS/aseprite','-b',str(source),'--save-as',str(target)],check=True,capture_output=True)
            a=rgba(source.with_suffix('.png'));b=rgba(target)
            assert a.size==b.size and a.tobytes()==b.tobytes(),source
    report=dict(status='pending_visual_approval',productionApplied=False,modules=9,logicalBlockCells=[4,4],
                rectangularAssembly=checks,canopy=signature(OUT/'canopy.png'),
                foregroundPlant=signature(ROOT/'ArtReview/RipeRice-v3/rice-ripe.png'),
                unityCaptures=[n for n in ('before','after','grass') if (OUT/f'unity-{n}-480x270.png').exists()],
                asepriteRoundTrips=dict(count=len(sources),decodedRGBAEqual=True),
                playTest=False)
    if report['unityCaptures']:
        report['unity']=dict(mode='Edit',size=[480,270],captures=len(report['unityCaptures']),wholeFrameRGBADifferences=0)
    (OUT/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    boards();print('PASS: 5 RGBA module assemblies, binary alpha, 6-color canopy; preview compositions ready')
if __name__=='__main__':main()
