"""Approved T3 production export. Frozen inputs; no foreign-region outputs.
Run without flags to validate all proposals; --write installs JSON only.
Unity T3Production.Build owns the 13 scene/index export (no build settings).
"""
import copy,gzip,json,hashlib,sys,math
from pathlib import Path
import numpy as np
from scipy import ndimage
import t3_boundaries as t3
ROOT=t3.ROOT
BASE=ROOT/'Assets/PocketCaravan'
BUNDLE=Path(__file__).with_name('t3_baseline.json.gz')
STATE=Path(__file__).with_name('t3_installed.json')
RES=['Resources/GoryeoLayout.json','Resources/Goryeo/PlaceLabels.json','Resources/Goryeo/Life/Anchors.json','Resources/Goryeo/VillageLife/Anchors.json','Resources/Goryeo/Stride/Surfaces.json','Resources/Goryeo/Ferry/Config.json','Resources/Goryeo/Daily/Config.json','Resources/Goryeo/Seats/Config.json','Data/Goryeo/MarketLayout.json','Data/Goryeo/LogPlacement.json','Resources/WorldMap/GoryeoLayout.json','Resources/WorldMap/GoryeoRegionalLayout.json','World/Connections/region_links.json']
DROPPED_WALKS=[]
def digest(d):return hashlib.sha256(json.dumps(d,ensure_ascii=False,sort_keys=True).encode()).hexdigest()
def baseline():
 raw=BUNDLE.read_bytes();config=json.loads(t3.CONFIG.read_text())
 if hashlib.sha256(raw).hexdigest()!=config['baselineBundleSha256']:raise ValueError('Frozen approved bundle changed')
 return json.loads(gzip.decompress(raw))
def freeze():
 if BUNDLE.exists():raise ValueError('Frozen input already exists')
 config,sources=t3.read_sources()
 paths={m:str(next((BASE/'Maps').rglob(m+'.json')).relative_to(BASE))for m in sources}
 data={'commit':'9536d0d29','maps':sources,'paths':paths,'resources':{p:json.loads((BASE/p).read_text())for p in RES}}
 BUNDLE.write_bytes(gzip.compress(json.dumps(data,ensure_ascii=False,separators=(',',':')).encode(),mtime=0))
def proposal():
 b=baseline();old=b['maps'];config=json.loads(t3.CONFIG.read_text())
 if config.get('approval',{}).get('status')!='approved':raise ValueError('Boundary approval required')
 maps,report,*_=t3.generate(config,old)
 out={};res=copy.deepcopy(b['resources'])
 def owner(x,y):
  choices=[m for m,d in maps.items()if d['terrainX']<=x<d['terrainX']+d['w']and d['terrainY']<=y<d['terrainY']+d['h']]
  if len(choices)!=1:raise ValueError(('unowned coordinate',x,y,choices))
  return choices[0]
 def xy(mid,x,y,bottom=False,cell=False):
  s=old[mid];wx=s['terrainX']+x;wy=s['terrainY']+(s['h']-1-y if cell else s['h']-y)if bottom else s['terrainY']+y
  target=owner(wx+(0.5 if cell else 0),wy+(0.5 if cell else 0));d=maps[target]
  xx=wx-d['terrainX'];yy=wy-d['terrainY']
  if bottom:yy=d['h']-1-yy if cell else d['h']-yy
  return target,xx,yy
 for mid,d in maps.items():
  d['previewOnly']=False;d['boundaryLayoutVersion']=3
  for o in d['objects']:
   if mid=='transit_crossroads'and o['kind']=='sign'and (d['terrainX']+o['x'],d['terrainY']+o['y'])in ((28,-15),(7,-4),(7,4)):o['sheet']='Assets/PocketCaravan/Art/Goryeo/Markets/sign.png'
  # Keep old internal spawn aliases for scripted journeys/existing references.
  existing={o.get('id')for o in d['objects']if o['kind']=='spawn'}
  fallback=next(o for o in d['objects']if o['kind']=='spawn'and o['id']=='S0')
  for o in old[mid]['objects']:
   if o['kind']!='spawn'or o['id']in existing:continue
   q=copy.deepcopy(fallback);q['id']=o['id'];d['objects'].append(q);existing.add(q['id'])
  out[b['paths'][mid]]=d
 for p,key,cell in [('Resources/Goryeo/PlaceLabels.json','places',False),('Resources/Goryeo/Life/Anchors.json','points',True),('Resources/Goryeo/VillageLife/Anchors.json','points',False)]:
  rows={m:{'id':m,'w':d['w'],'h':d['h'],key:[]}for m,d in maps.items()}
  for row in res[p]['maps']:
   for point in row[key]:
    q=copy.deepcopy(point);m,x,y=xy(row['id'],q['x'],q['y'],True,cell);q.update(x=x,y=y)
    if 'bx'in q:
     target,bx,by=xy(row['id'],q['bx'],q['by'],True,cell)
     # v4 (11/08): a decorative walk whose far end now lies in another scene stays at its start point.
     if m!=target:bx,by=q['x'],q['y'];DROPPED_WALKS.append((row['id'],point.get('kind')))
     q.update(bx=bx,by=by)
    rows[m][key].append(q)
  res[p]['maps']=list(rows.values())
 # Preserve stride surface at every physical cell; source overlaps must agree.
 surface={}
 p='Resources/Goryeo/Stride/Surfaces.json'
 for row in res[p]['maps']:
  s=old[row['id']]
  for i,c in enumerate(row['data']):
   if c=='O':continue
   key=s['terrainX']+i%row['w'],s['terrainY']+s['h']-1-i//row['w']
   if key in surface and surface[key]!=c:raise ValueError(('surface conflict',key))
   surface[key]=c
 res[p]['maps']=[dict(id=m,w=d['w'],h=d['h'],data=''.join(surface.get((d['terrainX']+x,d['terrainY']+d['h']-1-y),'O')for y in range(d['h'])for x in range(d['w'])))for m,d in maps.items()]
 p='Resources/Goryeo/Ferry/Config.json'
 for port in res[p]['ports']:
  mid=port['mapId']
  for prefix in ('npc','arrival'):
   m,x,y=xy(mid,port[prefix+'X'],port[prefix+'Y']);assert m==mid;port[prefix+'X']=x;port[prefix+'Y']=y
  port.update(width=maps[mid]['w'],height=maps[mid]['h'])
 for p in ('Resources/Goryeo/Daily/Config.json','Data/Goryeo/LogPlacement.json'):
  v=res[p];m,x,y=xy(v['mapId'],v['x'],v['y']);assert m==v['mapId'];v.update(x=x,y=y)
  if 'memoKey'in v:v.update(width=maps[m]['w'],height=maps[m]['h'])
 for seat in res['Resources/Goryeo/Seats/Config.json']['seats']:
  mid=seat['mapId'];m,x,y=xy(mid,seat['x'],seat['y']);assert m==mid;seat.update(x=x,y=y)
  if seat['fire']:
   m,x,y=xy(mid,seat['propX'],seat['propY']);assert m==mid;seat.update(propX=x,propY=y)
 p='Data/Goryeo/MarketLayout.json';rows={}
 for row in res[p]['maps']:
  for space in row['spaces']:
   grouped={}
   for prop in space['props']:
    q=copy.deepcopy(prop);m,x,y=xy(row['mapId'],q['x'],q['y']);q.update(x=x,y=y)
    d=maps[m]
    if not(0<=x<=d['w']-q['w']and 0<=y<=d['h']-q['h']):raise ValueError(('whole prop straddles border',m,q))
    grouped.setdefault(m,[]).append(q)
   for m,props in grouped.items():
    rowout=rows.setdefault(m,dict(mapId=m,width=maps[m]['w'],height=maps[m]['h'],spaces=[]))
    rowout['spaces'].append(dict(id=space['id']if m==row['mapId']else row['mapId']+'_'+space['id'],props=props))
 res[p]['maps']=list(rows.values())
 # Freeze v2 coordinate frames and legacy fallback positions; main/recovery masks.
 migration={'version':4,'maps':[]}
 prev=json.loads(Path(__file__).with_name('t3_v3_frames.json').read_text())['maps']   # v3 saves
 legacy={v['id']:v for v in res['Resources/GoryeoLayout.json']['maps']}
 for mid,d in maps.items():
  mask=np.array(d['blocked']).reshape(d['h'],d['w'])==0
  labels,_=ndimage.label(mask);s0=next(o for o in d['objects']if o['kind']=='spawn'and o['id']=='S0');main=labels==labels[s0['y'],s0['x']]
  d['reachable']=main.astype(int).ravel().tolist()
  safe=main.copy();stay=main.copy()
  for o in d['objects']:
   if o['kind']in ('spawn','anim','ambience','lantern','sign'):continue
   if o['kind']=='exit':stay[o['y']:o['y']+o.get('h',1),o['x']:o['x']+o.get('w',1)]=False
   radius=0 if o['kind']=='exit'else 1
   x,y=o['x'],o['y'];safe[max(0,y-radius):min(d['h'],y+o.get('h',1)+radius),max(0,x-radius):min(d['w'],x+o.get('w',1)+radius)]=False
  market=rows.get(mid)
  if market:
   for space in market['spaces']:
    for q in space['props']:safe[q['y']+q['h']-1,q['x']:q['x']+q['w']]=False
  if not safe.any():raise ValueError('No safe save recovery '+mid)
  s=old[mid];v=legacy[mid]
  migration['maps'].append(dict(id=mid,oldX=s['terrainX'],oldY=s['terrainY'],oldWidth=s['w'],oldHeight=s['h'],x=d['terrainX'],y=d['terrainY'],width=d['w'],height=d['h'],prevX=prev[mid]['x'],prevY=prev[mid]['y'],prevWidth=prev[mid]['width'],prevHeight=prev[mid]['height'],legacyX=v['spawnX'],legacyY=v['spawnY'],legacyDropX=v['dropX'],legacyDropY=v['dropY'],main=''.join('1'if c else'0'for c in main.ravel()),stay=''.join('1'if c else'0'for c in stay.ravel()),safe=''.join('1'if c else'0'for c in safe.ravel())))
  # Current fallback metadata: older positions are migrated from separate frozen resource.
  v.update(height=d['h'],legacyHeight=d['h'],spawnX=s0['x']+.5,spawnY=d['h']-s0['y']-.18,dropX=s0['x']+.5,dropY=d['h']-s0['y']-.18)
 out['Resources/Goryeo/BoundaryLayout.json']=migration
 # Reframe only the two owned map layouts. Label/settlement world positions stay fixed.
 for name in ('Goryeo','GoryeoRegional'):
  p='Resources/WorldMap/'+name+'Layout.json';layout=res[p];subset=[maps[r['mapId']]for r in layout['maps']]
  oldox=min(old[r['mapId']]['terrainX']-r['x']for r in layout['maps']);oldoy=min(old[r['mapId']]['terrainY']-r['y']for r in layout['maps'])
  ox=min(d['terrainX']for d in subset);oy=min(d['terrainY']for d in subset)
  layout.update(width=max(d['terrainX']+d['w']for d in subset)-ox,height=max(d['terrainY']+d['h']for d in subset)-oy,terrainSource='Tools/WorldLayout/t3_install.py')
  for r in layout['maps']:
   d=maps[r['mapId']];r.update(x=d['terrainX']-ox,y=d['terrainY']-oy,w=d['w'],h=d['h'])
   r['labelX']+=oldox-ox;r['labelY']+=oldoy-oy
  for key in ('annotations','settlements'):
   for r in layout.get(key,[]):r['x']+=oldox-ox;r['y']+=oldoy-oy
 # Shared connection source: update only our endpoints/signs. Foreign halves are byte-for-byte equivalent JSON.
 spec=res['World/Connections/region_links.json']
 for link in spec['links']:
  for side,other in (('a','b'),('b','a')):
   endpoint=link[side]
   if endpoint['map']not in maps:continue
   mid=endpoint['map'];target=link[other]['map'];d=maps[mid]
   exit=next(o for o in d['objects']if o['kind']=='exit'and o['target']==target)
   endpoint.update(x=exit['x'],y=exit['y'],w=exit['w'],h=exit['h'],edge=exit['edge'],by='Tools/WorldLayout/t3_install.py')
 for sign in spec.get('signs',[]):
  if sign['map']not in maps:continue
  d=maps[sign['map']]
  match=[o for o in d['objects']if o['kind']=='sign'and o.get('text')==sign['text']]
  if len(match)!=1:raise ValueError('Connection sign missing/ambiguous')
  sign.update(x=match[0]['x'],y=match[0]['y'])
 # Runtime-spawned tutorial people must use the same authored frame as map objects.
 intro=json.loads(Path(__file__).with_name('goryeo_intro.json').read_text())
 mid=intro['mapId'];d=maps[mid];x=intro['worldX']-d['terrainX'];y=intro['worldY']-d['terrainY']
 if not(0<=x<d['w']and 0<=y<d['h'])or d['blocked'][y*d['w']+x]or not d['reachable'][y*d['w']+x]:
  raise ValueError('Tutorial guide is not on reachable land')
 out['Resources/Goryeo/Intro.json']=dict(mapId=mid,width=d['w'],height=d['h'],x=x,y=y)
 out.update(res)
 return out,report

def install(write=False):
 out,report=proposal();b=baseline();previous=json.loads(STATE.read_text())if STATE.exists()else{}
 original={**{b['paths'][m]:d for m,d in b['maps'].items()},**b['resources']}
 for p,d in out.items():
  path=BASE/p
  if path.exists():
   got=digest(json.loads(path.read_text()));allowed={digest(d),previous.get(p)}
   if p in original:allowed.add(digest(original[p]))
   if got not in allowed:raise ValueError('Refusing to overwrite newer authoring changes: '+p)
 if write:
  for p,d in out.items():
   path=BASE/p;path.parent.mkdir(parents=True,exist_ok=True);path.write_text((json.dumps(d,ensure_ascii=False,indent=2) if p=='World/Connections/region_links.json' else json.dumps(d,ensure_ascii=False,separators=(',',':')))+'\n')
  STATE.write_text(json.dumps({p:digest(d)for p,d in out.items()},indent=2)+'\n')
 print(json.dumps(dict(outputs=len(out),maps=len(report['maps']),exits=sum(m['exits']for m in report['maps']),write=write),ensure_ascii=False))
def worldmaps():
 sys.path.insert(0,str(ROOT/'Tools/MapGen'))
 import unified_map as u
 maps=u.load_maps()
 for name in ('Goryeo','GoryeoRegional'):
  layout=json.loads((Path(u.WM)/(name+'Layout.json')).read_text())
  grid,road=u.compose(layout,maps);layout,grid,road=u.crop(layout,grid,road)
  u.draw(grid,road).save(Path(u.OUT)/(name+'Map.png'))
  # Base atlas water mask uses the same physical frame (M pin/hydrology consumers).
  from PIL import Image
  water=np.zeros((layout['height'],layout['width']),np.uint8)
  terrain=Image.new('RGBA',(layout['width']*16,layout['height']*16),(225,211,171,255))
  for row in layout['maps']:
   d=maps[row['mapId']]
   native=Image.fromarray(u.render(d,['ground','deco','obj','high']).astype(np.uint8))
   terrain.alpha_composite(native,(row['x']*16,row['y']*16))
   a=np.array(d['water'],np.uint8).reshape(d['h'],d['w'])*255
   water[row['y']:row['y']+d['h'],row['x']:row['x']+d['w']]=a
  terrain.resize((layout['width']*4,layout['height']*4),Image.Resampling.BOX).convert('RGB').save(Path(u.WM)/(name+'Terrain.png'))
  Image.fromarray(water).resize((layout['width']*4,layout['height']*4),Image.Resampling.NEAREST).save(Path(u.WM)/(name+'WaterMask.png'))
  (Path(u.OUT)/(name+'MapLayout.json')).write_text(json.dumps(layout,ensure_ascii=False,separators=(',',':'))+'\n')

if __name__=='__main__':
 if '--freeze'in sys.argv:freeze()
 else:
  install('--write'in sys.argv)
  if '--worldmaps'in sys.argv:worldmaps()
