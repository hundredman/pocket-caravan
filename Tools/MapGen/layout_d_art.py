"""Native terrain modules for the isolated B2 review; review status remains pending."""
import hashlib
import json
from pathlib import Path
from PIL import Image
from layout_d import ROOT, OUT

PACK=ROOT.parent/'Ninja Adventure - Asset Pack'


def family(tile):
    if tile.startswith('water_'):return 'water','coast_river_water','water'
    if tile.startswith('b2_cliff'):return 'cliff','mountain_cliffs','cliff'
    if tile.startswith('b2_bridge'):return 'bridge','river_bridge','bridge'
    if tile=='floor_0_12':return 'grass','ground_cover',None
    return 'road','walking_roads','road'


def piece(tile):
    aliases={'water_1_7':'center','water_1_6':'edge','water_0_6':'outer_corner','water_5_7':'inner_corner',
             'floor_1_8':'center','floor_1_7':'edge','floor_0_7':'outer_corner','floor_5_8':'inner_corner','floor_3_7':'end',
             'b2_cliff_center':'center','b2_cliff_edge':'edge','b2_cliff_outer_corner':'outer_corner','b2_cliff_inner_corner':'inner_corner',
             'b2_bridge_start':'start','b2_bridge_center':'center','b2_bridge_end':'end'}
    return aliases.get(tile,tile)


def definition(tile,out):
    image=(out/'Tiles'/(tile+'.png')).relative_to(ROOT).as_posix()
    return dict(size=[1,1],image=image,tiles=[dict(dx=0,dy=0,layer='ground',tile=tile,image=image)])


def prepare(maps,out=OUT):
    out=Path(out);(out/'Tiles').mkdir(exist_ok=True)
    used={k for m in maps for k in m['palette'] if k}
    # Minimum module shapes are registered even when this layout does not place them.
    used.update(['water_1_7','water_1_6','water_0_6','water_5_7',
                 'floor_1_8','floor_1_7','floor_0_7','floor_5_8','floor_3_7',
                 'b2_cliff_center','b2_cliff_edge','b2_cliff_outer_corner','b2_cliff_inner_corner',
                 'b2_bridge_start','b2_bridge_center','b2_bridge_end'])
    sources={};sheets={};provenance=[]
    def native(name,x,y):
        filename=PACK/'Backgrounds/Tilesets'/('Tileset'+name+'.png')
        if name not in sheets:
            sheets[name]=Image.open(filename).convert('RGBA')
            sources[filename.name]=hashlib.sha256(filename.read_bytes()).hexdigest()
        return sheets[name].crop((x*16,y*16,(x+1)*16,(y+1)*16))
    for tile in sorted(used):
        layers=[]
        if tile.startswith('b2_cliff'):
            image=Image.open(ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/floor_0_12_goryeo_jade.png').convert('RGBA');layers.append(['frozen_floor_0_12_goryeo_jade',0,0])
            if tile!='b2_cliff_center':
                x,y={'b2_cliff_edge':(5,1),'b2_cliff_outer_corner':(4,1),'b2_cliff_inner_corner':(9,1)}[tile]
                image.alpha_composite(native('Relief',x,y));layers.append(['Relief',x,y])
        elif tile.startswith('b2_bridge'):
            x={'b2_bridge_start':0,'b2_bridge_center':1,'b2_bridge_end':2}[tile]
            image=native('Water',x,13);layers.append(['Water',x,13])
        else:
            name,x,y=tile.split('_');image=native(name.capitalize(),int(x),int(y));layers.append([name.capitalize(),int(x),int(y)])
        # Reuse already baked in-game colors when available; never tone/recolor here.
        existing=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/(tile+'_goryeo_jade.png')
        frozen=None
        if existing.is_file():
            image=Image.open(existing).convert('RGBA');frozen=existing.relative_to(ROOT).as_posix()
        image.save(out/'Tiles'/(tile+'.png'))
        provenance.append(dict(tile=tile,nativeLayers=layers,pixels=16,scaled=False,recolored=False,frozenProductionPng=frozen))
    (out/'LICENSE.txt').write_bytes((PACK/'LICENSE.txt').read_bytes())
    (out/'provenance.json').write_text(json.dumps(dict(sourceSheetsSha256=sources,tiles=provenance),ensure_ascii=False,indent=2)+'\n')
    assets=[]
    for name in ('grass','water','road','cliff','bridge'):
        keys=sorted(k for k in used if family(k)[0]==name);_,purpose,module=family(keys[0])
        asset=dict(id='b2_'+name,purpose=purpose,profile='native16',
                   source=dict(kind='external',path=(out/'provenance.json').relative_to(ROOT).as_posix(),license='CC0-1.0; LICENSE.txt beside this catalogue'),
                   review=dict(status='pending'),rules={})
        if module:
            asset.update(kind='modular',moduleType=module,pieces={piece(k):definition(k,out) for k in keys},
                         requiredPieces=['start','center','end'] if name=='bridge' else ['center','edge','outer_corner','inner_corner']+(['end'] if name=='road' else []))
        else:asset.update(kind='object',**definition(keys[0],out))
        assets.append(asset)
    catalogue=dict(schemaVersion=1,profiles=dict(native16=dict(pixelsPerCell=[16,16],binaryAlpha=True)),assets=assets)
    (out/'catalogue.json').write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+'\n')
    for m in maps:
        bind_map(m)
        (out/'Maps'/(m['id']+'.json')).write_text(json.dumps(m,ensure_ascii=False,separators=(',',':'))+'\n')
    return catalogue


def bind_map(m):
    for p in m['assetPlacements']:
        key=m['palette'][m['ground'][p['y']*m['w']+p['x']]]
        name,purpose,module=family(key);p.update(assetId='b2_'+name,purpose=purpose)
        if module:p['piece']=piece(key)
        else:p.pop('piece',None)


def mosaic(layout,t,out):
    from types import SimpleNamespace
    from layout_d import split_maps
    owned=set(layout.owners);xs=[x for x,y in owned];ys=[y for x,y in owned]
    fake=SimpleNamespace(maps={'review/world':dict(bounds=[min(xs),min(ys),max(xs)+1,max(ys)+1],label='B2 static road review mosaic')},map_cells={'review/world':owned})
    m=split_maps(fake,t)[0];m['id']='layoutD_review_mosaic'
    path=next(r['cells'] for r in t['roadRecords'] if r['fromKey']=='east/동문 마을' and r['toKey']=='east/중앙 길목 마을')
    center=path[len(path)//2];m['objects']=[dict(kind='spawn',id='S0',x=center[0]-m['terrainX'],y=center[1]-m['terrainY'],w=1,h=1)]
    bind_map(m)
    (out/'review-mosaic.json').write_text(json.dumps(m,ensure_ascii=False,separators=(',',':'))+'\n')
    (out/'camera-sample.json').write_text(json.dumps(dict(globalCell=center,terrainX=m['terrainX'],terrainY=m['terrainY'],purpose='static same-scale road comparison; not runtime scene layout'),ensure_ascii=False,indent=2)+'\n')


if __name__=='__main__':
    from layout_d import export
    from asset_manifest import validate
    layout,t,maps,report=export();catalogue=prepare(maps);mosaic(layout,t,OUT)
    errors=validate(catalogue,[(m['id'],m) for m in maps],ROOT,False)
    result=dict(geometry=report,mode='technical_preview',errors=errors,errorCount=len(errors),userApproved=False)
    (OUT/'asset-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(result,ensure_ascii=False));raise SystemExit(bool(errors))
