"""Goryeo geometry/key bindings and frozen whole-sprite access.
Original atlases are retained for provenance; map generation never extracts or tints them.
"""
from functools import lru_cache
from pathlib import Path
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
def sprite(name,w,h):
    from frozen_art import sprite as whole
    return whole(name,w,h)


@lru_cache(None)
def image_for(key):
    from frozen_art import tile
    return tile(key)


@lru_cache(None)
def tone(base,theme):
    # Compatibility for offline callers; live tile export does not call this function.
    from frozen_art import tile
    return tile(base+'@goryeo_'+theme)


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
    """Copy preserved frames to isolated exports; no live pixel conversion."""
    import shutil
    folder=Path(root)/'Assets/PocketCaravan/Art/NinjaAdventure/Animated'
    folder.mkdir(parents=True,exist_ok=True)
    for i in range(4):
        source=ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Animated/goryeo_ripples_{i}.png'
        target=folder/source.name
        if source.resolve()!=target.resolve():shutil.copyfile(source,target)
