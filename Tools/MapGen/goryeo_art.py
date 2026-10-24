"""Goryeo-only authored sprite atlas and restrained pixel palette.
Source atlas is retained; slices are deterministic build outputs at 16 pixels per cell.
"""
from functools import lru_cache
from pathlib import Path
import colorsys
from PIL import Image, ImageOps
from tiles import STAMPS, K

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/PocketCaravan/Art/Goryeo/Source/Architecture.png'
# Rectangles are measured from the generated atlas, not assumed equal grid cells.
RECTS = {'hall': (10, 30, 578, 494), 'house': (578, 100, 1057, 488),
         'shop': (1060, 125, 1530, 490), 'thatch': (12, 577, 565, 953),
         'pavilion': (558, 510, 1001, 956), 'pine': (1001, 513, 1535, 970)}
DETAIL_RECTS = {'wall': (48,225,498,414), 'lantern': (674,103,862,449),
                'lotus': (1090,204,1465,405), 'paper': (44,594,505,932),
                'harvest': (565,610,984,924), 'celadon': (1063,590,1503,935)}
CUSTOM = {'royal': ('hall',10,7), 'manor': ('house',6,4), 'stall': ('shop',6,4), 'cottage': ('thatch',5,4), 'pavilion': ('pavilion',5,5)}
CUSTOM.update({'wall':('wall',4,2), 'lantern':('lantern',1,2), 'lotus':('lotus',2,1), 'paper':('paper',4,3), 'harvest':('harvest',3,2), 'celadon':('celadon',3,2), 'paving':('paving',4,4)})
WOODLAND_COLUMNS=(0,410,785,1150,1536)
WOODLAND_ROWS=(0,365,727,1024)
NATURE_SIZES=[(4,3),(3,4),(3,4),(4,3),(4,4),(4,4),(3,4),(3,4),(3,2),(2,2),(2,2),(3,2)]
for i,(w,h) in enumerate(NATURE_SIZES):CUSTOM['woodland'+str(i)]=('woodland'+str(i),w,h)
for key,(asset,w,h) in CUSTOM.items():
    STAMPS['goryeo_'+key]=('goryeo_'+key,0,0,w,h,0,0,None)

REPLACE = {'house_tile': 'house', 'house_plain': 'house', 'house_wall': 'house',
           'house3': 'house', 'shop': 'shop', 'store': 'shop',
           'house_thatch': 'thatch', 'hall': 'hall', 'pine': 'pine', 'pinebig': 'pine'}

@lru_cache(None)
def sprite(name, w, h):
    if name.startswith('woodland'):
        i=int(name[8:]);col,row=i%4,i//4
        with Image.open(SOURCE.parent/('PineAncient.png' if i==0 else 'Woodland.png')) as atlas:
            im=(atlas.copy() if i==0 else atlas.crop((WOODLAND_COLUMNS[col],355 if i==4 else WOODLAND_ROWS[row],WOODLAND_COLUMNS[col+1],WOODLAND_ROWS[row+1]))).convert('RGBA')
            bounds=im.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
            if bounds:
                if i and (bounds[0]==0 or bounds[1]==0 or bounds[2]==im.width or bounds[3]==im.height):
                    raise ValueError(('Woodland extraction clips silhouette',i,bounds))
                im=im.crop(bounds)
        # Fit the complete silhouette, preserving its proportions and a transparent margin.
        fitted=ImageOps.contain(im,(w*16-2,h*16-2),Image.Resampling.NEAREST)
        im=Image.new('RGBA',(w*16,h*16));im.alpha_composite(fitted,((w*16-fitted.width)//2,h*16-fitted.height-1))
        alpha=im.getchannel('A').point(lambda a:255 if a>=128 else 0)
        im=im.convert('RGB').quantize(colors=18).convert('RGBA');im.putalpha(alpha)
        return im
    source=SOURCE.parent/('CourtyardStone.png' if name=='paving' else 'VillageDetails.png') if name in DETAIL_RECTS or name=='paving' else SOURCE
    with Image.open(source) as atlas:
        im = (atlas if name=='paving' else atlas.crop(DETAIL_RECTS[name] if name in DETAIL_RECTS else RECTS[name])).convert('RGBA')
    # Pixel assets are baked at the actual engine resolution, never kept as HD overlays.
    im = im.resize((w * 16, h * 16), Image.Resampling.LANCZOS)
    # Snap alpha for crisp pixel edges; the generation's faint transparent fringe is not artwork.
    alpha = im.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    # Transparent RGB must not consume the limited pixel palette.
    from collections import Counter
    rgb=im.convert('RGB');pixels=list(rgb.getdata());mask=list(alpha.getdata())
    opaque=Counter(p for p,a in zip(pixels,mask) if a)
    background=opaque.most_common(1)[0][0] if opaque else (0,0,0)
    rgb.putdata([p if a else background for p,a in zip(pixels,mask)])
    im = rgb.quantize(colors=12 if name=='paving' else 28, method=Image.Quantize.MEDIANCUT).convert('RGBA')
    im.putalpha(alpha)
    return im

@lru_cache(None)
def image_for(key):
    if key.startswith('goryeo_'):
        sheet,tx,ty=key.split(':');name,sw,sh=CUSTOM[sheet[7:]]
    else:
        _, name, sw, sh, tx, ty = key.split(':')
    return sprite(name, int(sw), int(sh)).crop((int(tx)*16, int(ty)*16, (int(tx)+1)*16, (int(ty)+1)*16))

@lru_cache(None)
def tone(base, theme):
    from tiles import tile_image
    im = tile_image(base).copy()
    result=[]
    for r,g,b,a in im.getdata():
        h,s,v=colorsys.rgb_to_hsv(r/255,g/255,b/255)
        if .12 < h < .40 and s>.22:  # vegetation: remove acid yellow, keep light/dark structure
            h = .27 if theme != 'harvest' else .115
            s *= .50 if theme != 'harvest' else .70
            v *= .83 if theme != 'harvest' else .94
        elif .40 <= h <= .68 and s>.20: # water: celadon, never neon cyan
            h=.47; s*=.60; v*=.78
        elif h < .13 and s>.24: # roads, earth, timber: muted sandstone
            s*=.49; v*=.88
        r,g,b=colorsys.hsv_to_rgb(h,s,v)
        result.append((int(r*255),int(g*255),int(b*255),a))
    im.putdata(result)
    return im


def apply(maps):
    for m in maps:
        if m.id.startswith('transit_'): continue
        theme='harvest' if m.id in ('goryeo_jangdan','goryeo_fieldroad') else 'jade'
        def tint(key,x,y,theme=theme):
            return key+'@goryeo_'+theme if key and not key.startswith(('goryeo:', 'goryeo_')) else key
        # Keep collision geometry and the fully placed stamp footprint unchanged.
        for name,x,y in m.stamps:
            if name not in REPLACE: continue
            _,_,_,w,h,_,high,_=STAMPS[name]
            for dy in range(h):
                for dx in range(w):
                    layer=m.high if dy<high else m.obj
                    layer[y+dy][x+dx]=f'goryeo:{REPLACE[name]}:{w}:{h}:{dx}:{dy}'
        for layer in (m.ground,m.deco,m.obj,m.high):
            for y,row in enumerate(layer):
                for x,key in enumerate(row):row[x]=tint(key,x,y)
        if m.id=='Gaegyeong':
            ox,oy=getattr(m,'core_offset',(0,0))
            for y in range(8+oy,30+oy):
                for x in range(5+ox,35+ox):
                    if not m.water[y][x] and (x,y) not in m.decks:
                        m.ground[y][x]=f'goryeo_paving:{x%4}:{y%4}'
        for o in m.objects:
            if o['kind']=='anim' and o.get('sheet')=='ripples':o['sheet']='goryeo_ripples'
        previous=getattr(m,'tint_key',lambda k,x,y:k)
        def expected(key,x,y,m=m,previous=previous,tint=tint):
            for name,sx,sy in m.stamps:
                if name in REPLACE:
                    _,_,_,w,h,*_=STAMPS[name]
                    if sx<=x<sx+w and sy<=y<sy+h:return f'goryeo:{REPLACE[name]}:{w}:{h}:{x-sx}:{y-sy}'
            return tint(previous(key,x,y),x,y)
        m.tint_key=expected
    return maps


def export_ripples(root):
    """Keep existing ripple timing/silhouettes, remove the old opaque cyan square."""
    folder=Path(root)/'Assets/PocketCaravan/Art/NinjaAdventure/Animated'
    for i in range(4):
        with Image.open(folder/f'ripples_{i}.png') as source:
            source=source.convert('RGBA');background=source.getpixel((0,0))
            result=Image.new('RGBA',source.size)
            result.putdata([(151,193,180,90) if p!=background and p[3] else (0,0,0,0) for p in source.getdata()])
            result.save(folder/f'goryeo_ripples_{i}.png')
