"""Assemble intended canopy modules and compare with whole-plant reference fields."""
from pathlib import Path
import json,hashlib
import argparse
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/RiceField-Tiles-v1'
PLANT='ArtReview/RipeRice-v1/rice-ripe.png'
NAMES=[['north-west','north','north-east'],['west','center','east'],['south-west','south','south-east']]

def direct(nx,ny):
    p=Image.open(ROOT/PLANT).convert('RGBA')
    out=Image.new('RGBA',(nx*96+32,ny*96+48))
    count=0
    for row in range(ny*6):
        for col in range(nx*6):
            x=16+col*16+8+(row%2)*4-16;y=32+row*16+15-32
            assert x>=0 and y>=0 and x+p.width<=out.width and y+p.height<=out.height
            out.alpha_composite(p,(x,y));count+=1
    return out,count

def assembled(nx,ny):
    assert nx>=2 and ny>=2
    out=Image.new('RGBA',(nx*96+32,ny*96+48))
    for by in range(ny):
        for bx in range(nx):
            gx=0 if bx==0 else 2 if bx==nx-1 else 1
            gy=0 if by==0 else 2 if by==ny-1 else 1
            im=Image.open(OUT/'Native'/f'{NAMES[gy][gx]}.png').convert('RGBA')
            out.alpha_composite(im,(16+bx*96-(16 if bx==0 else 0),32+by*96-(32 if by==0 else 0)))
    return out

def run():
    original=Image.open(ROOT/PLANT).convert('RGBA')
    original_colors=set(original.getdata())
    ground=Image.open(ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/floor_0_12_goryeo_jade.png').convert('RGBA')
    background=ground.getpixel((0,0))
    checks=[]
    for nx,ny in [(2,2),(3,3),(5,2),(2,5),(4,3)]:
        actual=assembled(nx,ny);expected,count=direct(nx,ny)
        assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),(nx,ny)
        assert set(actual.getchannel('A').getdata())<={0,255}
        checks.append(dict(blocks=[nx,ny],logicalCells=[nx*6,ny*6],plants=count,rgbaEqualsWholePlantAssembly=True))
    field=assembled(2,2);field.save(OUT/'assembled-2x2.png')
    fieldLarge=assembled(4,3);fieldLarge.save(OUT/'assembled-4x3.png')
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
    board=Image.new('RGB',(980,570),background[:3]);d=ImageDraw.Draw(board)
    for i,(name,label)in enumerate([('sparse-reference','기존 간격 — 24px'),('assembled-2x2','타일화 — 16px 간격·엇갈림')]):
        im=Image.open(OUT/f'{name}.png').convert('RGBA').resize((448,480),Image.Resampling.NEAREST)
        d.text((16+i*490,12),label,font=font,fill='white');board.paste(im,(16+i*490,56),im)
    board.save(OUT/'comparison.png')
    atlas=Image.new('RGB',(768,918),background[:3]);d=ImageDraw.Draw(atlas);pieces=[]
    labels=[['좌상단','위 가장자리','우상단'],['왼쪽','반복 중앙','오른쪽'],['좌하단','아래 가장자리','우하단']]
    for y,row in enumerate(NAMES):
        for x,name in enumerate(row):
            im=Image.open(OUT/'Native'/f'{name}.png').convert('RGBA')
            assert im.width%16==0 and im.height%16==0
            assert set(im.getchannel('A').getdata())<={0,255}
            assert set(im.getdata())<=original_colors
            d.text((x*256+8,y*306+4),labels[y][x],font=font,fill='white')
            z=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)
            atlas.paste(z,(x*256+8,y*306+36),z)
            pieces.append(dict(name=name,image=f'Native/{name}.png',size=list(im.size),logicalSize=[6,6],
                               logicalOffsetPixels=[-16 if x==0 else 0,-32 if y==0 else 0],rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest()))
    atlas.save(OUT/'module-sheet.png')
    (OUT/'catalogue.json').write_text(json.dumps(dict(status='pending',purpose='ripe_rice_field_canopy',pixelsPerCell=16,
        logicalBlockCells=[6,6],minimumBlocks=[2,2],pieces=pieces,sourcePlant=PLANT,
        assembly='native modules with outer overhang; repeat center and correct edge/corner roles',productionApplied=False),ensure_ascii=False,indent=2)+'\n')
    report=dict(tiledAssemblies=checks,pieces=9,colorsUnchanged=True,wholePlantsPreserved=True,
        originalPlantRGBA=hashlib.sha256(original.tobytes()).hexdigest(),approval='pending',productionApplied=False)
    unity=OUT/'unity-480x270.png'
    if unity.exists():
        source=Image.open(OUT/'source-field.png').convert('RGBA')
        assert source.tobytes()==assembled(3,3).tobytes()
        expected=Image.new('RGBA',(480,270),background);expected.alpha_composite(source,(200,-32))
        actual=Image.open(unity).convert('RGBA')
        assert actual.crop((200,0,480,270)).tobytes()==expected.crop((200,0,480,270)).tobytes()
        report['unity']=dict(mode='Edit',renderSize=[480,270],modules=9,fieldRGBADifferences=0,
                             productionWrites=0,originalSceneRestored=True)
    (OUT/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('PASS: 9 modules; 5 whole-plant versus module assemblies exactly match decoded RGBA')

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--revision',default='v1')
    parser.add_argument('--plant',default=PLANT)
    args=parser.parse_args()
    OUT=ROOT/('ArtReview/RiceField-Tiles-'+args.revision);PLANT=args.plant
    run()
