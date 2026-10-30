"""User 2026-10-30: missing art, everyday squares, replaceable event yards, more rice.
Authoring source only: approved whole images, no cropping/recoloring/resizing.
Writes an isolated review manifest and nature recipe; never changes live maps.
"""
from pathlib import Path
import hashlib,json,shutil,sys
import numpy as np
from scipy import ndimage
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
OUT=ROOT/'ArtReview/VillageFill-20261030';PREV=ROOT/'Assets/PocketCaravan/Editor/B2Preview/VillageFill'
sys.path.insert(0,str(HERE))
import building_bindings
ASSETS={
 'sign':ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/element_0_2.png',
 'board':ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/element_1_2.png',
 'stall':building_bindings.path('shop'),
 'cart':ROOT/'Assets/PocketCaravan/Art/Baked/Objects/c6_cart_f74ffdac.png',
 'bench':ROOT/'ArtReview/C7-RemainingBatch-20261027/Native/bench.png',
 'harvest':ROOT/'ArtReview/Approved/C7-WorkshopProps-v4/harvest.png',
 'thatch':building_bindings.path('thatch')}
def sha(p):return hashlib.sha256(Image.open(p).convert('RGBA').tobytes()).hexdigest()
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def design():
 OUT.mkdir(exist_ok=True);(PREV/'Whole').mkdir(parents=True,exist_ok=True)
 z=json.loads((ROOT/'ArtReview/M2-Zones/zones.json').read_text())['villages']
 maps={p.stem:json.loads(p.read_text())for p in (ROOT/'Assets/PocketCaravan/Maps').glob('*.json')}
 # Full source field clearings may remove vegetation, but never roads, buildings, water,
 # existing features, mountain overlays, exits or arrival cells.
 base=ROOT/'ArtReview/BuildingNatureBinding-20261029/BuildingLayers'
 m=np.load(base/'masks.npz');H,W=m['own'].shape
 d=json.loads((HERE/'nature_live.json').read_text());d['status']='review';d['description']='User: additional Goryeo rice, village dressing review; live unchanged.'
 hard=np.zeros((H,W),bool)
 parts=json.loads((base/'parts.json').read_text())['parts']
 for key,items in parts.items():
  layer,y,x=key.split(':')
  if layer in ('obj','high') and any(a[0].startswith('sprite:c7:')for a in items):hard[int(y),int(x)]=True
 for mid,md in maps.items():
  if not mid in ['Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan']:continue
  for o in md['objects']:
   if o['kind']=='anim':continue
   x,y=md['terrainX']+o['x'],md['terrainY']+o['y']+130
   hard[max(0,y-2):min(H,y+o.get('h',1)+2),max(0,x-2):min(W,x+o.get('w',1)+2)]=True
 occupied=np.zeros((H,W),bool)
 import m4_nature
 _,nature_assets=m4_nature.load_layout(HERE/'nature_live.json',True)
 for p in d['placements']:
  im=nature_assets[p['asset']]['image'];x,y=p['x'],p['y']+130
  occupied[max(0,y-1):y+im.height//16+1,max(0,x-1):x+im.width//16+1]=True
 allowed=m['own']&~m['water']&(m['grade']==0)&~hard
 road_distance=ndimage.distance_transform_edt(m['grade']==0)
 water_distance=ndimage.distance_transform_edt(~m['water'])
 added=[]
 for key,cx,cy,quota in [('goryeo/장단',202,47,5),('goryeo/나루',236,40,2),('goryeo/개경',259,7,1),('goryeo/송악',185,-6,1)]:
  ki=list(map(str,m['keys'])).index(key);candidates=[]
  for y in range(108,H-8):
   for x in range(156,min(W-12,290)):
    view=np.s_[y:y+8,x:x+12]
    if not allowed[view].all() or not (m['mapid'][view]==ki).all() or occupied[view].any():continue
    if water_distance[view].min()<1.5 or road_distance[y+4,x+6]>18:continue
    score=(x+6-cx)**2+(y-126-cy)**2+water_distance[y+4,x+6]*3
    candidates.append((score,x,y))
  count=0
  for _,x,y in sorted(candidates):
   if count>=quota:break
   if occupied[max(0,y-1):y+9,max(0,x-1):x+13].any():continue
   ident='village-fill-rice-'+str(len(added)+1);p=dict(id=ident,asset='small-plot',role='rice',x=x,y=y-130)
   d['placements'].append(p);d['clearings'].append(dict(id=ident,x=x,y=y-130,w=12,h=8));occupied[y:y+8,x:x+12]=True;added.append(dict(p,map=key));count+=1
 write(HERE/'nature_village_review.json',d)
 entries=[];missing=[];gaps=[];assets=[]
 for name,p in ASSETS.items():
  im=Image.open(p);dest=PREV/'Whole'/f'{name}.png'
  if not dest.exists()or sha(dest)!=sha(p):shutil.copyfile(p,dest)
  native=OUT/'Native'/f'{name}.png';native.parent.mkdir(exist_ok=True)
  if not native.exists()or sha(native)!=sha(p):shutil.copyfile(p,native)
  assets.append(dict(id=name,reviewImage=str(native.relative_to(ROOT)),source=str(p.relative_to(ROOT)),image=str(dest.relative_to(ROOT)),size=[im.width//16,im.height//16],rgbaSha256=sha(p)))
 sizes={a['id']:a['size']for a in assets}
 for key,v in z.items():
  mid={'rest_north':'transit_central_approach','rest_lake':'transit_south_inland'}.get(v['map'],v['map'])
  md=maps[mid];tx,ty=md['terrainX'],md['terrainY'];w,h=md['w'],md['h'];its={i['name']:i for i in v['items']if not i.get('missing')}
  used=[];reserved=set()
  for o in md['objects']:
   if o['kind'] in ['anim','growth','ambience','lantern']:continue
   for dy in range(o.get('h',1)):
    for dx in range(o.get('w',1)):reserved.add((o['x']+dx,o['y']+dy))
  for i in its.values():
   if i.get('asset'):used.append((i['x'],i['y'],i['w'],i['h']))
  groups=[]
  def place(asset,x,y,group,board=False):
   sw,sh=sizes[asset];lx,ly=x-tx,y-ty
   if lx<0 or ly<0 or lx+sw>w or ly+sh>h:return False
   if any(md['blocked'][(ly+dy)*w+lx+dx]for dy in range(sh)for dx in range(sw)):return False
   if any((lx+dx,ly+dy) in reserved for dy in range(sh)for dx in range(sw)):return False
   if any(x<rx+rw and x+sw>rx and y<ry+rh and y+sh>ry for rx,ry,rw,rh in used):return False
   if any(x<r['x']+12 and x+sw>r['x'] and y<r['y']+8 and y+sh>r['y'] for r in added):return False
   group['props'].append(dict(asset=asset,x=lx,y=ly,w=sw,h=sh));used.append((x,y,sw,sh));return True
  b=its.get('게시판')
  if b and any(o['kind']=='board'for o in md['objects']):
   g=dict(id='board',props=[])
   if not place('board',b['x'],b['y'],g):gaps.append(dict(village=key,feature='게시판',reason='existing terrain or placement conflict'))
   groups.append(g)
  for name,aid in [('장터 광장','market'),('이벤트 마당','event-yard')]:
   if name not in its:continue
   it=its[name];g=dict(id=aid,props=[])
   wants=['bench','cart']if aid=='market' and not any(o['kind']=='market'for o in md['objects'])else ['stall','harvest']if aid=='market' and key=='goryeo/장단' else ['stall','cart']if aid=='market'else ['bench','cart','harvest']if key=='goryeo/장단'else ['bench','cart']
   for asset in wants:
    sw,sh=sizes[asset];ok=False
    for y in range(it['y'],it['y']+it['h']-sh+1):
     for x in range(it['x'],it['x']+it['w']-sw+1):
      if place(asset,x,y,g):ok=True;break
     if ok:break
    if not ok:gaps.append(dict(village=key,feature=name,asset=asset,reason='keep existing access and artwork clear'))
   groups.append(g)
  for it in v['items']:
   if it.get('missing'):missing.append(dict(village=key,feature=it['name'],reason='M2 placement failed; not a missing texture',resolved=False))
  sign_group=dict(id='signs',props=[])
  for o in md['objects']:
   if o['kind']!='sign':continue
   ok=False
   for dx,dy in [(0,-1),(-1,0),(1,0),(0,1)]:
    if place('sign',tx+o['x']+dx,ty+o['y']+dy,sign_group):ok=True;break
   if not ok:gaps.append(dict(village=key,feature='이정표',reason='no safe adjacent visual position'))
  if sign_group['props']:groups.append(sign_group)
  entries.append(dict(mapId=mid,village=key,spaces=groups))
 # Signs on transit maps without a settlement also need a visible post.
 own_ids={e['mapId']for region in ('goryeo','transit')for e in json.loads((ROOT/f'Assets/PocketCaravan/World/Regions/{region}.json').read_text())['maps']}
 for mid in sorted(own_ids-{e['mapId']for e in entries}):
  md=maps[mid];w,h=md['w'],md['h'];group=dict(id='signs',props=[])
  protected={(o['x']+dx,o['y']+dy)for o in md['objects']if o['kind']!='anim'for dx in range(o.get('w',1))for dy in range(o.get('h',1))}
  for o in md['objects']:
   if o['kind']!='sign':continue
   for dx,dy in [(0,-1),(-1,0),(1,0),(0,1)]:
    x,y=o['x']+dx,o['y']+dy
    if 0<=x<w and 0<=y<h and not md['blocked'][y*w+x] and (x,y)not in protected:
     group['props'].append(dict(asset='sign',x=x,y=y,w=1,h=1));protected.add((x,y));break
   else:gaps.append(dict(village=mid,feature='이정표',reason='no safe adjacent visual position'))
  if group['props']:entries.append(dict(mapId=mid,village=mid,spaces=[group]))
 inputs={mid:dict(metadata={k:md[k]for k in ['id','name','w','h','terrainX','terrainY','worldX','worldY','travelHours']if k in md},kinds=sorted({o['kind']for o in md['objects']}),objectSignatures=[hashlib.sha256(json.dumps(o,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()).hexdigest()for o in md['objects']])for mid,md in maps.items()if mid in own_ids}
 result=dict(inputMaps=inputs,status='review',assets=assets,maps=entries,addedRice=added,missingSlots=missing,unplacedProps=gaps,policy='Everyday event-yard props are a separate runtime root; SetEvent replaces and restores them. No automatic event invented.')
 write(OUT/'manifest.json',result)
 print('Added rice',[(p['map'],p['x'],p['y'])for p in added]);print('Props',sum(len(s['props'])for e in entries for s in e['spaces']),'unplaced',len(gaps),'unplaced houses',len(missing))
if __name__=='__main__':design()
