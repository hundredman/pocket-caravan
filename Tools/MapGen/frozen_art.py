"""C6: read registered, baked PNGs; never extract, resize or recolor source art."""
from functools import lru_cache
from pathlib import Path
import hashlib,json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
FOLDER=ROOT/'Assets/PocketCaravan/Art/Baked'

@lru_cache(None)
def index():return json.loads((FOLDER/'index.json').read_text())

@lru_cache(None)
def catalogue():
    from asset_manifest import validate_catalogue
    doc=json.loads((FOLDER/'catalogue.json').read_text());errors,assets=validate_catalogue(doc,ROOT)
    if errors:raise ValueError(('Baked A2 catalogue invalid',errors))
    return assets

@lru_cache(None)
def tile(key):
    item=index()['tiles'].get(key)
    if item is None:raise ValueError('Unbaked tile; author/register it explicitly: '+key)
    path=(ROOT/item['path']).resolve()
    if not path.is_relative_to(ROOT) or hashlib.sha256(path.read_bytes()).hexdigest()!=item['sha256']:
        raise ValueError('Frozen tile changed: '+key)
    with Image.open(path) as source:im=source.convert('RGBA')
    if im.size!=(16,16):raise ValueError('Frozen tile dimensions: '+key)
    return im

@lru_cache(None)
def sprite(name,w,h):
    path=index()['sprites'].get(f'{name}:{w}:{h}')
    if not path:raise ValueError('Unbaked whole sprite: '+name)
    with Image.open(ROOT/path) as source:im=source.convert('RGBA')
    if im.size!=(w*16,h*16):raise ValueError('Whole sprite dimensions changed')
    return im

def signature(m,x,y,w,h):
    cells=[]
    for dy in range(h):
        for dx in range(w):
            xx,yy=x+dx,y+dy
            if not(0<=xx<m.w and 0<=yy<m.h):raise ValueError('Whole object clipped')
            cells.append(dict(dx=dx,dy=dy,key=m.obj[yy][xx] or m.high[yy][xx],layer='high' if m.high[yy][xx] else 'obj'))
    return json.dumps([w,h,cells],sort_keys=True)

def claims(m):
    """Compile complete object placements plus purpose-specific modular cells. No image operations."""
    from tiles import STAMPS
    assets=catalogue();idx=index();result=[];covered=set()
    def add(aid,x,y,piece=None):
        asset=assets[aid];definition=asset['pieces'][piece] if piece is not None else asset
        for t in definition['tiles']:
            slot=(t['layer'],x+t['dx'],y+t['dy'])
            if slot in covered:raise ValueError(('Overlapping object claim',m.id,slot))
            covered.add(slot)
        p=dict(id='c6_'+str(len(result)),assetId=aid,purpose=asset['purpose'],x=x,y=y)
        if piece is not None:p['piece']=piece
        result.append(p)
    for name,x,y in m.stamps:
        _,_,_,w,h,*_=STAMPS[name];aid=idx['templates'].get(signature(m,x,y,w,h))
        if aid is None:raise ValueError(('Unregistered/partial whole object',m.id,name,x,y))
        add(aid,x,y)
    for y,row in enumerate(m.deco):
        for x,key in enumerate(row):
            if key=='goryeo_lotus:0:0':add('c6_lotus',x,y)
    for layer in ('ground','deco','obj','high'):
        for y,row in enumerate(getattr(m,layer)):
            for x,key in enumerate(row):
                if not key or (layer,x,y) in covered:continue
                binding=idx['byTile'].get(key)
                if binding is None:raise ValueError(('Unrecorded partial object/tile',m.id,layer,x,y,key))
                add(binding['assetId'],x,y,binding.get('piece'))
    for obj in m.objects:
        if obj['kind']=='growth':tile(obj['id'])
        if obj.get('character'):
            for d in 'dulr':
                for frame in range(4):
                    path=f'Assets/PocketCaravan/Art/Characters/{obj["character"]}_{d}{frame}.png'
                    if path not in idx['visualImages']:raise ValueError('Unregistered character: '+path)
        if obj.get('sheet'):
            for frame in range(obj.get('frames',0)):
                path=f'Assets/PocketCaravan/Art/NinjaAdventure/Animated/{obj["sheet"]}_{frame}.png'
                if path not in idx['visualImages']:raise ValueError('Unregistered animation: '+path)
    return dict(assetPlacementVersion=1,assetPlacements=result,
                terrainTags=['water'if value else'grass'for row in m.water for value in row])

def validate_export(m,data):
    from asset_manifest import validate_map
    bound=dict(data,**claims(m));errors=validate_map(bound,catalogue(),True)
    if errors:raise ValueError(('Baked map placement failed',m.id,errors[:10]))
    return bound
