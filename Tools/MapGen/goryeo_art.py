"""Goryeo-only authored sprite atlas and restrained pixel palette.
Source atlas is retained; slices are deterministic build outputs at 16 pixels per cell.
"""
from functools import lru_cache
from pathlib import Path
import colorsys
from PIL import Image
from tiles import STAMPS, K

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/PocketCaravan/Art/Goryeo/Source/Architecture.png'
# Rectangles are measured from the generated atlas, not assumed equal grid cells.
RECTS = {'hall': (10, 30, 578, 494), 'house': (578, 100, 1057, 488),
         'shop': (1060, 125, 1530, 490), 'thatch': (12, 577, 565, 953),
         'pavilion': (558, 510, 1001, 956), 'pine': (1001, 513, 1535, 970)}
CUSTOM = {'royal': ('hall',10,7), 'manor': ('house',6,4), 'stall': ('shop',6,4), 'cottage': ('thatch',5,4), 'pavilion': ('pavilion',5,5)}
for key,(asset,w,h) in CUSTOM.items():
    STAMPS['goryeo_'+key]=('goryeo_'+key,0,0,w,h,0,0,None)

REPLACE = {'house_tile': 'house', 'house_plain': 'house', 'house_wall': 'house',
           'house3': 'house', 'shop': 'shop', 'store': 'shop',
           'house_thatch': 'thatch', 'hall': 'hall', 'pine': 'pine', 'pinebig': 'pine'}

@lru_cache(None)
def sprite(name, w, h):
    with Image.open(SOURCE) as atlas:
        im = atlas.crop(RECTS[name]).convert('RGBA')
    # Pixel assets are baked at the actual engine resolution, never kept as HD overlays.
    im = im.resize((w * 16, h * 16), Image.Resampling.LANCZOS)
    # Snap alpha for crisp pixel edges; the generation's faint transparent fringe is not artwork.
    alpha = im.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    im = im.convert('RGB').quantize(colors=28, method=Image.Quantize.MEDIANCUT).convert('RGBA')
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
        previous=getattr(m,'tint_key',lambda k,x,y:k)
        def expected(key,x,y,m=m,previous=previous,tint=tint):
            for name,sx,sy in m.stamps:
                if name in REPLACE:
                    _,_,_,w,h,*_=STAMPS[name]
                    if sx<=x<sx+w and sy<=y<sy+h:return f'goryeo:{REPLACE[name]}:{w}:{h}:{x-sx}:{y-sy}'
            return tint(previous(key,x,y),x,y)
        m.tint_key=expected
    return maps
