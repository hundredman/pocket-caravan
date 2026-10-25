"""C6 read-only production survey + isolated whole-object baking feasibility proof.
No runtime source, map, scene, catalogue or original image is modified.
"""
from pathlib import Path
from collections import defaultdict,Counter
import hashlib,json,sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from world_registry import atlas_content
from tiles import STAMPS,tile_image
from asset_manifest import validate_catalogue
OUT=ROOT/'ArtReview/C6-Preflight-2026-10-26'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def alpha(path):
    with Image.open(path) as im:return {str(a):n for a,n in Counter(im.convert('RGBA').getchannel('A').tobytes()).items() if a not in (0,255)}
def main():
    OUT.mkdir(exist_ok=True);(OUT/'whole').mkdir(exist_ok=True)
    entries,maps=atlas_content();groups={};allkeys=set();refs={};inputs={};placements=[]
    for entry,m in zip(entries,maps):
        inputs[entry['jsonPath']]=sha(ROOT/entry['jsonPath'])
        for layer in ('ground','deco','obj','high'):
            for row in getattr(m,layer):allkeys.update(k for k in row if k)
        for o in m.objects:
            if o.get('kind')=='growth':allkeys.add(o['id'])
        for name,x,y in m.stamps:
            if name not in STAMPS:continue
            _,sx,sy,w,h,block,high,recolor=STAMPS[name]
            cells=[]
            for dy in range(h):
                for dx in range(w):
                    xx,yy=x+dx,y+dy
                    if not(0<=xx<m.w and 0<=yy<m.h):raise ValueError(('clipped stamp',m.id,name))
                    key=m.obj[yy][xx] or m.high[yy][xx]
                    if not key:raise ValueError(('partial stamp',m.id,name,dx,dy))
                    cells.append(dict(dx=dx,dy=dy,key=key,layer='high' if m.high[yy][xx] else 'obj'))
            sig=json.dumps([w,h,cells],sort_keys=True)
            if sig not in groups:
                aid='c6_'+name+'_'+hashlib.sha256(sig.encode()).hexdigest()[:8]
                groups[sig]=dict(id=aid,names=[],size=[w,h],cells=cells,uses=[])
            g=groups[sig]
            if name not in g['names']:g['names'].append(name)
            g['uses'].append(dict(map=m.id,x=x,y=y))
            placements.append(dict(map=m.id,name=name,assetId=g['id'],x=x,y=y))
        for o in m.objects:
            if o.get('character'):
                for d in 'dulr':
                    for i in range(4):
                        p=ROOT/f'Assets/PocketCaravan/Art/Characters/{o["character"]}_{d}{i}.png'
                        if not p.is_file():raise ValueError(('Missing character frame',p))
                        refs[str(p.relative_to(ROOT))]=dict(kind='npc_frame')
            if o.get('sheet'):
                for i in range(o.get('frames',0)):
                    p=ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Animated/{o["sheet"]}_{i}.png'
                    if not p.is_file():raise ValueError(('Missing animation frame',p))
                    refs[str(p.relative_to(ROOT))]=dict(kind='animation_frame')
    for p in (ROOT/'Assets/PocketCaravan/Art/Characters').glob('tanuki_tall_*.png'):refs[str(p.relative_to(ROOT))]=dict(kind='player_frame')
    refs['Assets/PocketCaravan/Art/Characters/shadow.png']=dict(kind='character_shadow')
    assets=[];owners={}
    for g in groups.values():
        w,h=g['size'];whole=Image.new('RGBA',(w*16,h*16));tiles=[]
        for c in g['cells']:
            key=c['key'];export=key.replace(':','_').replace('@','_');path=ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{export}.png'
            if path.is_file():image=Image.open(path).convert('RGBA')
            else:raise ValueError(('Missing baked runtime tile',key))
            whole.paste(image,(c['dx']*16,c['dy']*16))
            tiles.append(dict(dx=c['dx'],dy=c['dy'],layer=c['layer'],tile=export,image=str(path.relative_to(ROOT))))
            if export in owners and owners[export]!=g['id']:raise ValueError(('Ambiguous whole-object owner',export))
            owners[export]=g['id']
        image_path=OUT/'whole'/(g['id']+'.png');whole.save(image_path)
        ai=any(c['key'].startswith(('goryeo:','goryeo_')) for c in g['cells'])
        authored=all(c['key'].startswith(('module:','work:')) for c in g['cells'])
        source=dict(kind='ai_processed' if ai else 'authored' if authored else 'external',path=str(image_path.relative_to(ROOT)))
        if source['kind']=='external':source['license']='Ninja Adventure CC0-1.0; original attribution/source IDs in provenance.json'
        assets.append(dict(id=g['id'],purpose='preserved_'+g['names'][0],profile='native16',kind='object',size=g['size'],image=str(image_path.relative_to(ROOT)),source=source,review=dict(status='pending'),tiles=tiles))
    catalogue=dict(schemaVersion=1,profiles=dict(native16=dict(pixelsPerCell=[16,16],binaryAlpha=True)),assets=assets)
    (OUT/'catalogue-draft.json').write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+'\n')
    errors,_=validate_catalogue(catalogue,ROOT)
    visual=[]
    for path,detail in sorted(refs.items()):
        detail=dict(detail,path=path,sha256=sha(ROOT/path),nonBinaryAlpha=alpha(ROOT/path));visual.append(detail);inputs[path]=detail['sha256']
    tile_alpha=[]
    for key in sorted(allkeys):
        export=key.replace(':','_').replace('@','_');path=ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{export}.png'
        inputs[str(path.relative_to(ROOT))]=sha(path);a=alpha(path)
        if a:tile_alpha.append(dict(key=key,path=str(path.relative_to(ROOT)),sha256=sha(path),alpha=a))
    registered={e['mapId'] for e in entries}
    old=[]
    for path in sorted((ROOT/'Assets/PocketCaravan/Maps').glob('*.json')):
        m=json.loads(path.read_text())
        if m['id'] not in registered:old.append(dict(map=m['id'],json=str(path.relative_to(ROOT)),animalReferences=[o.get('character') for o in m.get('objects',[]) if o.get('character') and not o['character'].startswith('NPC/')]))
    result=dict(status='decision_required_before_production_change',registeredMaps=sorted(registered),stampPlacements=len(placements),wholeObjects=len(groups),wholeObjectTileKeys=len(owners),allTerrainAndGrowthTileKeys=len(allkeys),remainingTerrainTileKeys=len(allkeys)-len(owners),partialObjectCount=0,
                draftCatalogueErrors=errors,semiTransparentTerrainTiles=tile_alpha,visualObjects=visual,unusedMaps=old,
                productionChanged=False,notChecked=['full A2 map placement certification','live frozen-loader generation','Unity post-bake screenshot equality','unused map/save alias removal safety'])
    (OUT/'preflight.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    (OUT/'provenance.json').write_text(json.dumps(dict(groups=list(groups.values()),placements=placements),ensure_ascii=False,indent=2)+'\n')
    (OUT/'input-hashes.json').write_text(json.dumps(inputs,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(dict(wholeObjects=len(groups),stampPlacements=len(placements),terrainTiles=len(allkeys),registeredObjectKeys=len(owners),alphaTiles=len(tile_alpha),alphaVisuals=sum(bool(v['nonBinaryAlpha']) for v in visual),errors=Counter(e['code'] for e in errors),unusedMaps=len(old)),ensure_ascii=False))
if __name__=='__main__':main()
