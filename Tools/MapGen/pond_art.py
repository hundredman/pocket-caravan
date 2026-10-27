"""Read approved, precomposed pond modules. Keep original water/collision semantics."""
from functools import lru_cache
from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
CATALOGUE=ROOT/'Assets/PocketCaravan/Art/Goryeo/Terrain/Pond/catalogue.json'
LAYOUT=Path(__file__).with_name('pond_layout.json')

@lru_cache(None)
def pieces():
    asset=json.loads(CATALOGUE.read_text())['assets'][0]
    if asset['id']!='goryeo_capital_pond' or asset['review']['status']!='approved':raise ValueError('Pond is not approved')
    return asset['pieces']

@lru_cache(None)
def image_for(key):
    prefix,name=key.split(':',1)
    if prefix!='pond' or name not in pieces():raise ValueError('Unknown pond module: '+key)
    path=(ROOT/pieces()[name]['image']).resolve()
    if not path.is_relative_to(ROOT):raise ValueError('Pond image outside repository')
    with Image.open(path) as src:im=src.convert('RGBA')
    if im.size!=(16,16):raise ValueError('Pond native module size changed')
    return im

def apply(maps):
    layout=json.loads(LAYOUT.read_text())
    for m in maps:
        if m.id!=layout['mapId']:continue
        if [m.w,m.h]!=layout['size']:raise ValueError('Approved pond layout requires rebinding to changed map')
        placements=[]
        for p in layout['placements']:
            x,y,name=p['x'],p['y'],p['piece'];key='pond:'+name
            if name not in pieces() or not m.water[y][x] or not m.blocked[y][x]:raise ValueError('Approved pond is no longer existing blocked water')
            if not m.ground[y][x].startswith(('water:','pond:')):raise ValueError('Approved pond would replace non-water terrain')
            m.ground[y][x]=key
            placements.append(dict(id=f'capital_pond_{x}_{y}',assetId=layout['assetId'],piece=name,x=x,y=y,purpose='capital_pond'))
        m.approved_pond_placements=placements
    return maps
