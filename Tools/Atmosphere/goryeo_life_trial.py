"""A7 preview source. Existing land/roads/whole approved sprites remain unchanged."""
from pathlib import Path
import json,math
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A7-20261105'
def build():
 audio=json.loads((ROOT/'ArtReview/Goryeo-Atmosphere-A3-20261105/source.json').read_text());cert=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json').read_text());maps=[]
 for row in audio['maps']:
  id=row['id'];d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f'{id}.json').read_text());w,h=d['w'],d['h'];points=[]
  def point(kind,x,y,bx=None,by=None,reason=''):
   points.append(dict(kind=kind,x=x,y=y,bx=x if bx is None else bx,by=y if by is None else by,reason=reason))
  def open_cell(x,y):return 0<=x<w and 0<=y<h and d['reachable'][y*w+x]and not d['blocked'][y*w+x]and not d['water'][y*w+x]
  for p in row['points']:
   if p['kind']in ('chicken','dog'):point(p['kind'],p['x']+.5,p['y']+.5,reason='same approved A3 village yard anchor')
   elif p['kind']=='bird':
    for i in range(3):point('bird',p['x']+.5+i*.55,p['y']+.5-i*.35,reason='existing rice/grass anchor; airborne, no land edit')
  markets=[o for o in d['objects']if o['kind']=='market'];market=markets[0]if markets else None
  origins=set()
  for layer in ('obj','high'):
   for i,index in enumerate(d[layer]):
    key=d['palette'][index]if index else'';tile=cert['tiles'].get(key,{})
    for name,dx,dy in tile.get('parts',[]):
     if name in ('sprite:c7:cottage','sprite:c7:tile-house'):
      origins.add((name,i%w*16+dx,i//w*16+dy))
  if market:
   origins=sorted(origins,key=lambda t:(t[1]/16-market['x'])**2+(t[2]/16-market['y'])**2)[:3]
  else:origins=[]
  for name,x,y in origins:
   path=cert['sources'][name]['path']
   with Image.open(ROOT/path)as im:sw,sh=im.size
   point('smoke',(x+sw*.78)/16,h-(y+sh*.32)/16,reason='approved house side flue; morning/evening only')
  if market:
   occupied={(o['x'],o['y'])for o in d['objects']if o['kind']not in ('anim','exit','spawn')}
   for offset in (-5,5):
    candidates=[(x,y)for y in range(max(1,market['y']-4),min(h-1,market['y']+5))for x in range(max(1,market['x']-8),min(w-1,market['x']+9))if open_cell(x,y)and d['road'][y*w+x]==0 and(x,y)not in occupied]
    if candidates:
     x,y=min(candidates,key=lambda t:(t[0]-market['x']-offset)**2+(t[1]-market['y']-2)**2);point('lamp',x+.5,h-y-.5,reason='small extra village lantern; approved whole stone lantern');occupied.add((x,y))
  if id.startswith('transit_'):
   candidates=[]
   for y in range(3,h-3):
    for x in range(3,w-9):
     if all(open_cell(xx,yy)and d['road'][yy*w+xx]>=1 for yy in range(y-1,y+2)for xx in range(x,x+7)):
      candidates.append((x,y))
   if candidates:
    x,y=min(candidates,key=lambda t:(t[0]+3-w*.5)**2+(t[1]-h*.5)**2);point('walker',x+.5,h-y-.5,x+6.5,h-y-.5,reason='existing wide reachable road, no blockers/exits changed');point('cart',x+.5,h-y-.5,x+6.5,h-y-.5,reason='same verified road, staggered from walking trader')
  if id in ('Naru','transit_ferry','transit_southport'):
   # Whole 80x32 boat needs a clear 8x3 water corridor for its 3-cell travel.
   candidates=[]
   for y in range(2,h-4):
    for x in range(2,w-9):
     if all(d['water'][yy*w+xx]and not d['obj'][yy*w+xx]and not d['high'][yy*w+xx]for yy in range(y,y+3)for xx in range(x,x+8)):candidates.append((x,y))
   if candidates:
    x,y=min(candidates,key=lambda t:(t[0]-w*.65)**2+(t[1]-h*.5)**2);point('boat',x+2.5,h-y-1.5,x+5.5,h-y-1.5,reason='whole approved boat on verified clear water corridor')
   near=next((o for o in d['objects']if o['kind']=='ambience'),None)
   if near:
    for i in range(2):point('gull',near['x']+i*2,h-near['y']-1+i,reason='water-side gull, airborne')
  maps.append(dict(id=id,w=w,h=h,points=points))
 assets={
  'chicken_0':str((OUT/'Native/chicken_0.png').relative_to(ROOT)),'chicken_1':str((OUT/'Native/chicken_1.png').relative_to(ROOT)),
  'dog_0':str((OUT/'Native/dog_0.png').relative_to(ROOT)),'dog_1':str((OUT/'Native/dog_1.png').relative_to(ROOT)),
  'bird_0':'Assets/PocketCaravan/Resources/Venice/Life/pigeon_0.png','bird_1':'Assets/PocketCaravan/Resources/Venice/Life/pigeon_1.png',
  'gull_0':'Assets/PocketCaravan/Resources/Venice/Life/gull_0.png','gull_1':'Assets/PocketCaravan/Resources/Venice/Life/gull_1.png',
  'smoke':'Assets/PocketCaravan/Resources/Venice/Life/puff.png','boat':'ArtReview/Approved/BuildingShip-Renewal-v1/boat.png',
  'cart':'Assets/PocketCaravan/Art/Baked/Objects/c6_cart_e1a728ea.png','lamp':cert['sources']['sprite:c7:stone_lantern']['path']}
 (OUT/'source.json').write_text(json.dumps(dict(status='review',maps=maps,assets=[dict(key=k,path=v)for k,v in assets.items()],renderLimit=64,note='New animals from built-in imagegen, finalized via Aseprite native32x32/2frames/binary alpha/palette. Existing Venice effect/bird sprites reused whole for same purposes. Whole approved boat/cart/lantern preserved. No production hook.'),ensure_ascii=False,indent=2)+'\n')
 print([(m['id'],{k:sum(p['kind']==k for p in m['points'])for k in sorted({p['kind']for p in m['points']})})for m in maps])
if __name__=='__main__':build()
