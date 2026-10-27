"""Native 16px pond/lake proposal. Authored modules; no production map writes."""
from pathlib import Path
from collections import Counter
import json, math, subprocess, sys, tempfile
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/A4-Water-v1'
ASE='/Applications/Aseprite.app/Contents/MacOS/aseprite'
# Colors sampled from committed Gaegyeong tiles. Proposal only, not D3 approval.
COLORS={'water':(127,185,175,255),'shade':(120,160,153,255),'bank':(132,103,94,255),'light':(151,193,180,255)}
DIRS=[(0,-1),(1,0),(0,1),(-1,0),(1,-1),(1,1),(-1,1),(-1,-1)]

def canonical(mask):
    for diagonal,a,b in [(4,0,1),(5,1,2),(6,2,3),(7,3,0)]:
        if not(mask&(1<<a) and mask&(1<<b)):mask &= ~(1<<diagonal)
    return mask

def masks():return sorted({canonical(i) for i in range(256)})

def topology(wet,x,y):
    return canonical(sum(1<<i for i,(dx,dy) in enumerate(DIRS) if (x+dx,y+dy) in wet))

def wet_pixel(wet,px,py):
    cx,cy=math.floor(px/16),math.floor(py/16)
    if (cx,cy) not in wet:return False
    x,y=px-cx*16+.5,py-cy*16+.5
    n,e,s,w=[(cx+dx,cy+dy) in wet for dx,dy in DIRS[:4]]
    if (not n and y<3) or (not s and y>13) or (not w and x<3) or (not e and x>13):return False
    for a,b,diag,qx,qy in [(n,w,(-1,-1),x,y),(n,e,(1,-1),16-x,y),(s,e,(1,1),16-x,16-y),(s,w,(-1,1),x,16-y)]:
        if not a and not b and qx<13 and qy<13 and (qx-13)**2+(qy-13)**2>100:return False
        if a and b and (cx+diag[0],cy+diag[1]) not in wet and qx*qx+qy*qy<9:return False
    return True

def render_piece(mask):
    wet={(0,0)}|{d for i,d in enumerate(DIRS) if mask&(1<<i)}
    im=Image.new('RGBA',(16,16))
    for y in range(16):
        for x in range(16):
            if not wet_pixel(wet,x,y):continue
            # Evaluate across cell edges too: no per-tile border or repeated square frame.
            d=min((abs(dx)+abs(dy) for dy in range(-3,4) for dx in range(-3,4)
                   if abs(dx)+abs(dy)<=3 and not wet_pixel(wet,x+dx,y+dy)),default=4)
            color=COLORS['bank'] if d==1 else COLORS['shade'] if d==2 else COLORS['water']
            im.putpixel((x,y),color)
    return im

def names():
    result={255:'center',127:'inner_corner',239:'inner_corner_ne',223:'inner_corner_se',191:'inner_corner_sw',38:'outer_corner',76:'outer_corner_ne',137:'outer_corner_se',19:'outer_corner_sw',110:'edge',205:'edge_e',155:'edge_s',55:'edge_w'}
    # Exact topology identifiers avoid runtime mirroring, rotation or recoloring.
    for mask in masks():result.setdefault(mask,f'water_{mask:02x}')
    return result

def assemble(wet,images):
    w=max(x for x,y in wet)+1;h=max(y for x,y in wet)+1
    im=Image.new('RGBA',(w*16,h*16))
    for x,y in wet:im.alpha_composite(images[topology(wet,x,y)],(x*16,y*16))
    return im

def fixtures():
    # Explicit test masks, not a proposal for main/world coastlines.
    pond={(x,y) for y in range(3,8) for x in range(3,11) if ((x-6.5)/4)**2+((y-5)/2.4)**2<=1}
    lake={(x,y) for y in range(2,14) for x in range(2,22) if ((x-11)/10)**2+((y-7.5)/5.7)**2<=1 and not (x>=12 and y<=6)}
    lake|={(x,y) for y in range(5,9) for x in range(19,25)}
    tests={"pond":pond,"lake":lake,"island":{(x,y) for y in range(1,10) for x in range(1,14)}-{(x,y) for y in range(4,7) for x in range(6,9)},"narrow":{(x,3) for x in range(1,15)}|{(7,y) for y in range(1,8)}}
    return tests

def seam_check(images):
    # Shared border pixels must agree for all possible local 4x3 wet arrangements.
    bad=[];pairs=0
    for bits in range(1<<12):
        wet={(i%4-1,i//4-1) for i in range(12) if bits&(1<<i)}
        if not {(0,0),(1,0)}<=wet:continue
        a=images[topology(wet,0,0)];b=images[topology(wet,1,0)]
        for y in range(16):
            pairs+=1
            if a.getpixel((15,y))!=b.getpixel((0,y)):bad.append((bits,y))
    for bits in range(1<<12):
        wet={(i%3-1,i//3-1) for i in range(12) if bits&(1<<i)}
        if not {(0,0),(0,1)}<=wet:continue
        a=images[topology(wet,0,0)];b=images[topology(wet,0,1)]
        for x in range(16):
            pairs+=1
            if a.getpixel((x,15))!=b.getpixel((x,0)):bad.append((bits,x,'vertical'))
    return dict(pixelPairs=pairs,mismatches=len(bad),first=bad[:8])

def generate():
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'pieces').mkdir(exist_ok=True)
    images={m:render_piece(m) for m in masks()};lookup=names();assert len(images)==47
    sheet=Image.new('RGBA',(128,112));pieces={};tile_records=[]
    for index,(mask,im) in enumerate(images.items()):
        name=lookup[mask];im.save(OUT/'pieces'/f'{name}.png');sheet.alpha_composite(im,((index%8)*16,(index//8)*16))
        pieces[name]=dict(size=[1,1],image=f'ArtReview/A4-Water-v1/pieces/{name}.png',tiles=[dict(dx=0,dy=0,layer='deco',tile='a4_'+name,image=f'ArtReview/A4-Water-v1/pieces/{name}.png')])
        tile_records.append(dict(mask=mask,piece=name,rect=[(index%8)*16,(index//8)*16,16,16]))
    for frame in range(4):
        im=Image.new('RGBA',(16,16));draw=ImageDraw.Draw(im);radius=[2,3,4,3][frame]
        draw.line([(8-radius,7),(10,7)],fill=COLORS['light']);draw.line([(6,9),(8+radius,9)],fill=COLORS['shade'])
        name=f'ripple_{frame}';im.save(OUT/'pieces'/f'{name}.png');index=47+frame;sheet.alpha_composite(im,((index%8)*16,(index//8)*16))
        pieces[name]=dict(size=[1,1],image=f'ArtReview/A4-Water-v1/pieces/{name}.png',tiles=[dict(dx=0,dy=0,layer='high',tile='a4_'+name,image=f'ArtReview/A4-Water-v1/pieces/{name}.png')])
        tile_records.append(dict(piece=name,rect=[(index%8)*16,(index//8)*16,16,16]))
    wanted={name+'.png' for name in pieces}
    for p in (OUT/'pieces').glob('*.png'):
        if p.name not in wanted:p.unlink()  # obsolete output of this isolated generator only
    subprocess.run([ASE,'-b','--script-param',f'folder={OUT}','--script',str(ROOT/'Tools/ArtTrials/a4_water_animation.lua')],check=True)
    with tempfile.TemporaryDirectory(prefix='pocket-caravan-a4-') as temp:
        png=Path(temp)/'ripple.png';meta=Path(temp)/'ripple.json'
        subprocess.run([ASE,'-b',str(OUT/'ripple.aseprite'),'--sheet',str(png),'--sheet-type','horizontal','--data',str(meta),'--format','json-array','--list-tags'],check=True)
        reopened=Image.open(png).convert('RGBA');data=json.loads(meta.read_text())
        assert reopened.size==(64,16) and len(data['frames'])==4
        for i,frame in enumerate(data['frames']):
            assert frame['duration']==180
            assert reopened.crop((i*16,0,i*16+16,16)).tobytes()==Image.open(OUT/'pieces'/f'ripple_{i}.png').convert('RGBA').tobytes()
        assert data['meta']['frameTags'][0]['name']=='ripple'
    m=json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text());mw=m['w']
    actual_wet={(i%mw,i//mw) for i,v in enumerate(m['water']) if v}
    scope={(x,y) for x,y in actual_wet if 21<=x<=50 and 17<=y<=23}
    rows=[f'{x},{m["h"]-1-y},{lookup[topology(actual_wet,x,y)]},goryeo_paving_{x%4}_{y%4}' for x,y in sorted(scope)]
    (OUT/'unity-pond-placement.csv').write_text('\n'.join(rows)+'\n')
    sheet.save(OUT/'water-sheet.png')
    subprocess.run([ASE,'-b',str(OUT/'water-sheet.png'),'--save-as',str(OUT/'water-sheet.aseprite')],check=True)
    subprocess.run([ASE,'-b',str(OUT/'water-sheet.aseprite'),'--save-as',str(OUT/'water-sheet-reopened.png')],check=True)
    assert Image.open(OUT/'water-sheet-reopened.png').convert('RGBA').tobytes()==sheet.tobytes();(OUT/'water-sheet-reopened.png').unlink()
    palette=['#%02x%02x%02x'%p[:3] for p in COLORS.values()]
    catalogue=dict(schemaVersion=1,profiles={'trial16':dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=palette)},assets=[dict(id='trial_pond_lake_water',kind='modular',moduleType='water',purpose='pond_lake',profile='trial16',source=dict(kind='authored',path='ArtReview/A4-Water-v1/water-sheet.aseprite'),review=dict(status='changes_requested' if (OUT/'review-decision.json').exists() else 'pending'),requiredPieces=list(pieces),pieces=pieces)])
    (OUT/'catalogue.json').write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+'\n')
    (OUT/'piece-index.json').write_text(json.dumps(dict(topologyBits=['N','E','S','W','NE','SE','SW','NW'],cells=tile_records),indent=2)+'\n')
    # Fully declared preview placements: each cell is an authored module, no transforms.
    for label,wet in fixtures().items():
        w=max(x for x,y in wet)+2;h=max(y for x,y in wet)+2;pal=['']+['a4_'+n for n in pieces];deco=[0]*(w*h);placements=[]
        for x,y in sorted(wet):
            piece=lookup[topology(wet,x,y)];deco[y*w+x]=pal.index('a4_'+piece);placements.append(dict(id=f'{label}_{x}_{y}',assetId='trial_pond_lake_water',piece=piece,x=x,y=y,purpose='pond_lake'))
        data=dict(id='a4_trial_'+label,w=w,h=h,palette=pal,ground=[0]*(w*h),deco=deco,obj=[0]*(w*h),high=[0]*(w*h),assetPlacementVersion=1,assetPlacements=placements)
        (OUT/f'{label}-preview.json').write_text(json.dumps(data,separators=(',',':'))+'\n')
        assemble(wet,images).save(OUT/f'{label}-assembled.png')
    checks=seam_check(images);assert checks['mismatches']==0,checks
    (OUT/'checks.json').write_text(json.dumps(dict(nativeSize=[16,16],staticModules=47,rippleFrames=4,sourceReopenedExact=True,rippleAsepriteReopenedExact=True,rippleFrameDurationMs=180,seams=checks,userApproval='see_review-decision.json' if (OUT/'review-decision.json').exists() else 'pending',productionApplied=False,D3PaletteApproval=False),indent=2)+'\n')
    print(json.dumps(checks))

if __name__=='__main__':generate()
