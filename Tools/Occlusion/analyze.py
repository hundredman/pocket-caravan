"""Count opaque player/upper-prop overlap at positions reached by the actual Unity motor.
Pixel projection cross-check; this is not itself a rendered Play test.
"""
import json,math
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[2];folder=root/'ArtReview/C3a-2026-10-26';moves=json.load(open(folder/'moves.json'));maps={p.stem:json.load(open(p)) for p in (folder/'before').glob('*.json')};cache={};hits=[]
for r in json.load(open(folder/'walking.json'))['approaches']:
 if r['result']!='reached':continue
 d={'west':'r','east':'l','north':'d','south':'u'}[r['side']];im=Image.open(root/f'Assets/PocketCaravan/Art/Characters/tanuki_tall_{d}0.png').convert('RGBA');m=maps[r['map']];where={(p['x'],p['y']):p['key'].replace(':','_').replace('@','_') for p in moves[r['map']] if r['prop'].startswith(p['name']+'@')};n=0
 for y in range(im.height):
  for x in range(im.width):
   if not im.getpixel((x,y))[3]:continue
   wx=r['x']+(x+.5-im.width*.5)/16;wy=r['y']+.32+(im.height*.75-y-.5)/16;cell=(math.floor(wx),m['h']-1-math.floor(wy));key=where.get(cell)
   if not key:continue
   if key not in cache:cache[key]=Image.open(root/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{key}.png').convert('RGBA')
   px=math.floor((wx%1)*16);py=15-math.floor((wy%1)*16)
   if cache[key].getpixel((px,py))[3]:n+=1
 if n:hits.append(dict(r,overlapPixels=n))
hits.sort(key=lambda r:-r['overlapPixels']);(folder/'overlaps.json').write_text(json.dumps(hits,ensure_ascii=False,indent=2)+'\n');print('poses with opaque overlap',len(hits));print(json.dumps(hits[:12],indent=2))
