"""C6 one-time installer: preserve approved bytes, register whole objects/modules/frames.
Run explicitly for this migration, never from the map generation path.
"""
from pathlib import Path
from collections import defaultdict
import copy,hashlib,json,re,shutil,sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from world_registry import atlas_content
from tiles import tile_image,STAMPS
import goryeo_art as G
OUT=ROOT/'Assets/PocketCaravan/Art/Baked';DRAFT=ROOT/'ArtReview/C6-Preflight-2026-10-26'
EVIDENCE='Docs/Decisions-2026-10-25.md'
def rel(p):return p.relative_to(ROOT).as_posix()
def export(k):return k.replace(':','_').replace('@','_')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def semi(p):
 with Image.open(p) as im:return any(a not in (0,255) for a in im.convert('RGBA').getchannel('A').tobytes())
def main():
 OUT.mkdir(exist_ok=True);(OUT/'Objects').mkdir(exist_ok=True);(OUT/'Sprites').mkdir(exist_ok=True)
 entries,maps=atlas_content();doc=json.loads((DRAFT/'catalogue-draft.json').read_text());provenance=json.loads((DRAFT/'provenance.json').read_text());preflight=json.loads((DRAFT/'preflight.json').read_text())
 assets=[];templates={};used={};profiles={'native16':{'pixelsPerCell':[16,16],'binaryAlpha':True}}
 # Preserve the 46 complete object rasters, including transparent RGB and alpha exactly.
 for asset,group in zip(doc['assets'],provenance['groups']):
  a=copy.deepcopy(asset);path=OUT/'Objects'/Path(a['image']).name;shutil.copyfile(ROOT/a['image'],path)
  a['image']=rel(path);a['source']['path']=rel(path);a['review']={'status':'approved','evidence':EVIDENCE}
  assets.append(a);sig=json.dumps([group['size'][0],group['size'][1],group['cells']],sort_keys=True)
  templates[sig]=a['id']
  for c in group['cells']:used[c['key']]=('object',a['id'])
 # Frozen whole Goryeo sprites remain available to offline art-review tools; live tile export never extracts them.
 sprites={}
 variants=set(G.CUSTOM.values())
 for name in G.REPLACE:
  _,_,_,w,h,*_=STAMPS[name];variants.add((G.REPLACE[name],w,h))
 for name,w,h in sorted(variants):
  path=OUT/'Sprites'/f'{name}_{w}_{h}.png';G.sprite(name,w,h).save(path);sprites[f'{name}:{w}:{h}']=rel(path)
 # Lotus is authored as a complete 2x1 decoration even though its placement predates stamp tracking.
 lotus=OUT/'Objects/lotus_2_1.png';G.sprite('lotus',2,1).save(lotus)
 cells=[dict(dx=i,dy=0,key=f'goryeo_lotus:{i}:0',layer='deco')for i in range(2)]
 a=dict(id='c6_lotus',purpose='lotus_water_decoration',kind='object',profile='native16',size=[2,1],image=rel(lotus),source=dict(kind='ai_processed',path=rel(lotus)),review=dict(status='approved',evidence=EVIDENCE),tiles=[dict(dx=c['dx'],dy=0,layer='deco',tile=export(c['key']),image=f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{export(c["key"])}.png')for c in cells])
 assets.append(a);templates[json.dumps([2,1,cells],sort_keys=True)]=a['id']
 for c in cells:used[c['key']]=('object',a['id'])
 leftovers=defaultdict(set);allkeys=set();growth=set()
 for m in maps:
  for layer in ('ground','deco','obj','high'):
   for row in getattr(m,layer):
    for k in row:
     if not k:continue
     allkeys.add(k)
     if k not in used:leftovers[k].add(layer)
  for o in m.objects:
   if o['kind']=='growth':growth.add(o['id']);allkeys.add(o['id']);leftovers[o['id']].add('obj')
 # Approved pond is already a complete module catalogue; retain its purpose and palette.
 pond=json.loads((ROOT/'Assets/PocketCaravan/Art/Goryeo/Terrain/Pond/catalogue.json').read_text())
 profiles.update(pond['profiles']);assets.extend(pond['assets']);bytile={}
 for k in list(leftovers):
  if k.startswith('pond:'):bytile[k]=dict(assetId=pond['assets'][0]['id'],piece=k.split(':',1)[1]);del leftovers[k]
 groups=defaultdict(list)
 for k,layers in leftovers.items():
  if len(layers)!=1:raise ValueError(('Mixed layer key',k,layers))
  layer=next(iter(layers));base,*_=k.split('@');parts=base.split(':');sheet=parts[0];x,y=map(int,parts[1:3]);suffix=k[len(base):]
  if sheet=='water' and y<10:family=('water',suffix,'water',0,6,layer)
  elif sheet=='floor' and 7<=y<=10:family=('road',suffix,'road',0,7,layer)
  elif layer=='ground':family=('surface_'+sheet,suffix,'surface_pattern',0,0,layer)
  else:family=('single_'+export(k),'','object',0,0,layer)
  groups[family].append(k)
 for family,keys in groups.items():
  label,suffix,kind,ox,oy,layer=family;aid='c6_'+re.sub('[^a-z0-9_]','_',label+suffix)
  if kind in ('water','road'):
   sheet='water' if kind=='water' else 'floor';required={'center':(1,1),'edge':(1,0),'outer_corner':(0,0),'inner_corner':(5,1)}
   if kind=='road':required['end']=(3,0)
   for x,y in required.values():
    k=f'{sheet}:{ox+x}:{oy+y}'+suffix
    if k not in keys:keys.append(k)
  else:required={}
  keys=sorted(keys);pieces={}
  for k in keys:
   path=ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{export(k)}.png'
   if not path.exists():tile_image(k).save(path) # explicit one-time bake of missing complete module pieces
   allkeys.add(k)
   name=next((n for n,xy in required.items() if k.split('@')[0]==f'{k.split(":")[0]}:{ox+xy[0]}:{oy+xy[1]}'),None)
   if not name:name='center' if not pieces and not required else export(k)
   definition=dict(size=[1,1],image=rel(path),tiles=[dict(dx=0,dy=0,layer=layer,tile=export(k),image=rel(path))]);pieces[name]=definition
   bytile[k]=dict(assetId=aid,**({'piece':name}if kind!='object'else{}))
  first=next(iter(pieces.values()));source=dict(kind='external',path=first['image'],license='Ninja Adventure CC0-1.0; frozen derivation recorded in index.json')
  if label.startswith('surface_goryeo'):source=dict(kind='ai_processed',path=first['image'])
  a=dict(id=aid,purpose='preserved_'+label,source=source,review=dict(status='approved',evidence=EVIDENCE),profile='native16')
  if kind=='object':a.update(kind='object',**first)
  else:a.update(kind='modular',moduleType=kind,requiredPieces=list(required)or['center'],pieces=pieces)
  assets.append(a)
 # Reuse the full approved C1 catalogue, preserving all role-specific frames and Aseprite sources.
 npc=json.loads((ROOT/'Assets/PocketCaravan/Art/Characters/NPC/catalogue.json').read_text());assets.extend(npc['assets']);profiles.update(npc['profiles'])
 known={a.get('image')for a in assets};visual={a['image']:sha(ROOT/a['image']) for a in npc['assets']}
 for v in preflight['visualObjects']:
  path=ROOT/v['path'];visual[v['path']]=sha(path)
  if v['path'] in known:continue
  with Image.open(path) as im:size=im.size
  profile=f'pixels_{size[0]}_{size[1]}';profiles[profile]=dict(pixelsPerCell=list(size),binaryAlpha=True)
  aid='c6_visual_'+re.sub('[^a-z0-9_]','_',path.stem.lower());effect=v['kind']=='character_shadow'or'goryeo_ripples' in path.name
  assets.append(dict(id=aid,purpose=v['kind'],kind='effect'if effect else'object',profile=profile,size=[1,1],image=rel(path),source=dict(kind='authored',path=rel(path)),review=dict(status='approved',evidence=EVIDENCE),tiles=[dict(dx=0,dy=0,layer='deco',tile=aid,image=rel(path))]))
 # Serialized scene dependencies also include existing item icons and quest/alert markers.
 extras=[('Assets/PocketCaravan/Art/Items/'+name+'.png','item_icon') for name in ('firewood','ginseng','grain','najeon','paper','salt')]
 extras += [('Assets/PocketCaravan/Art/NinjaAdventure/Ui/emote_alert.png','npc_alert'),*[(f'Assets/PocketCaravan/Art/Placeholder/marker_{name}.png','quest_marker') for name in ('available','progress','turnin')]]
 for path,purpose in extras:
  file=ROOT/path;visual[path]=sha(file)
  with Image.open(file) as im:size=im.size
  profile=f'pixels_{size[0]}_{size[1]}';profiles[profile]=dict(pixelsPerCell=list(size),binaryAlpha=True)
  aid='c6_visual_'+file.stem
  source=dict(kind='external' if 'NinjaAdventure' in path else 'authored',path=path)
  if source['kind']=='external':source['license']='Ninja Adventure CC0-1.0'
  assets.append(dict(id=aid,purpose=purpose,kind='object',profile=profile,size=[1,1],image=path,source=source,review=dict(status='approved',evidence=EVIDENCE),tiles=[dict(dx=0,dy=0,layer='deco',tile=aid,image=path)]))
 index=dict(schemaVersion=1,tiles={k:dict(path=f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{export(k)}.png',sha256=sha(ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{export(k)}.png'))for k in sorted(allkeys)},templates=templates,byTile=bytile,sprites=sprites,visualImages=visual,provenance='ArtReview/C6-Preflight-2026-10-26/provenance.json')
 write(OUT/'index.json',index);write(OUT/'catalogue.json',dict(schemaVersion=1,profiles=profiles,assets=assets))
 locks=[]
 for path in sorted({ROOT/t['path'] for t in index['tiles'].values()}|{ROOT/v for v in visual}|{ROOT/a['image']for a in assets if a['kind'] in('object','effect')}):
  if semi(path):locks.append(dict(path=rel(path),sha256=sha(path),reason='C6 existing appearance preserved under Decisions10'))
 write(OUT/'legacy-alpha-exceptions.json',dict(schemaVersion=1,decision=EVIDENCE,files=locks))
 print('assets',len(assets),'frozen tiles',len(allkeys),'whole templates',len(templates),'legacy alpha files',len(locks))
if __name__=='__main__':main()
