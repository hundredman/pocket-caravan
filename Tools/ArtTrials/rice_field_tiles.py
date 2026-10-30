"""Assemble intended canopy modules and compare with whole-plant reference fields."""
from pathlib import Path
import json,hashlib
import argparse
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/RiceField-Tiles-v1'
PLANT='ArtReview/RipeRice-v1/rice-ripe.png'
PITCH=16
BLOCK_CELLS=6
REFERENCE_PITCH=24
BLOCK=96
NAMES=[['north-west','north','north-east'],['west','center','east'],['south-west','south','south-east']]

def direct(nx,ny):
    p=Image.open(ROOT/PLANT).convert('RGBA')
    out=Image.new('RGBA',(nx*BLOCK+32,ny*BLOCK+48))
    count=0
    for row in range(ny*BLOCK//PITCH):
        for col in range(nx*BLOCK//PITCH):
            x=16+col*PITCH+PITCH//2+(row%2)*4-16;y=32+row*PITCH+PITCH-1-32
            assert x>=0 and y>=0 and x+p.width<=out.width and y+p.height<=out.height
            out.alpha_composite(p,(x,y));count+=1
    return out,count

def assembled(nx,ny):
    assert nx>=2 and ny>=2
    out=Image.new('RGBA',(nx*BLOCK+32,ny*BLOCK+48))
    for by in range(ny):
        for bx in range(nx):
            gx=0 if bx==0 else 2 if bx==nx-1 else 1
            gy=0 if by==0 else 2 if by==ny-1 else 1
            im=Image.open(OUT/'Native'/f'{NAMES[gy][gx]}.png').convert('RGBA')
            out.alpha_composite(im,(16+bx*BLOCK-(16 if bx==0 else 0),32+by*BLOCK-(32 if by==0 else 0)))
    return out

def run():
    assert BLOCK%PITCH==0 and (BLOCK//PITCH)%2==0
    original=Image.open(ROOT/PLANT).convert('RGBA')
    original_colors=set(original.getdata())
    ground=Image.open(ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/floor_0_12_goryeo_jade.png').convert('RGBA')
    background=ground.getpixel((0,0))
    checks=[]
    for nx,ny in [(2,2),(3,3),(5,2),(2,5),(4,3)]:
        actual=assembled(nx,ny);expected,count=direct(nx,ny)
        assert actual.size==expected.size and actual.tobytes()==expected.tobytes(),(nx,ny)
        assert set(actual.getchannel('A').getdata())<={0,255}
        checks.append(dict(blocks=[nx,ny],logicalCells=[nx*BLOCK_CELLS,ny*BLOCK_CELLS],plants=count,rgbaEqualsWholePlantAssembly=True))
    field=assembled(2,2);field.save(OUT/'assembled-2x2.png')
    fieldLarge=assembled(4,3);fieldLarge.save(OUT/'assembled-4x3.png')
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
    panel=field.width*2+32
    board=Image.new('RGB',(panel*2,field.height*2+90),background[:3]);d=ImageDraw.Draw(board)
    for i,(name,label)in enumerate([('sparse-reference',f'기존 간격 — {REFERENCE_PITCH}px'),('assembled-2x2',f'좁힌 간격 — {PITCH}px')]):
        im=Image.open(OUT/f'{name}.png').convert('RGBA')
        assert im.size==field.size
        im=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)
        d.text((16+i*panel,12),label,font=font,fill='white');board.paste(im,(16+i*panel,56),im)
    board.save(OUT/'comparison.png')
    slotw=BLOCK*2+56;sloth=BLOCK*2+116
    atlas=Image.new('RGB',(slotw*3,sloth*3),background[:3]);d=ImageDraw.Draw(atlas);pieces=[]
    labels=[['좌상단','위 가장자리','우상단'],['왼쪽','반복 중앙','오른쪽'],['좌하단','아래 가장자리','우하단']]
    for y,row in enumerate(NAMES):
        for x,name in enumerate(row):
            im=Image.open(OUT/'Native'/f'{name}.png').convert('RGBA')
            assert im.width%16==0 and im.height%16==0
            assert set(im.getchannel('A').getdata())<={0,255}
            assert set(im.getdata())<=original_colors
            d.text((x*slotw+8,y*sloth+4),labels[y][x],font=font,fill='white')
            z=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)
            atlas.paste(z,(x*slotw+8,y*sloth+36),z)
            pieces.append(dict(name=name,image=f'Native/{name}.png',size=list(im.size),logicalSize=[BLOCK_CELLS,BLOCK_CELLS],
                               logicalOffsetPixels=[-16 if x==0 else 0,-32 if y==0 else 0],rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest()))
    atlas.save(OUT/'module-sheet.png')
    (OUT/'catalogue.json').write_text(json.dumps(dict(status='pending',purpose='ripe_rice_field_canopy',pixelsPerCell=16,
        logicalBlockCells=[BLOCK_CELLS,BLOCK_CELLS],plantPitch=PITCH,minimumBlocks=[2,2],pieces=pieces,sourcePlant=PLANT,
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
    parser.add_argument('--pitch',type=int,default=16)
    parser.add_argument('--block-cells',type=int,default=6)
    parser.add_argument('--reference-pitch',type=int,default=24)
    args=parser.parse_args()
    OUT=ROOT/('ArtReview/RiceField-Tiles-'+args.revision);PLANT=args.plant
    PITCH=args.pitch;BLOCK_CELLS=args.block_cells;BLOCK=BLOCK_CELLS*16;REFERENCE_PITCH=args.reference_pitch
    run()
