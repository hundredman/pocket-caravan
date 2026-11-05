"""A5 read-only place anchors derived from certified whole well/inn sprites and existing water access."""
from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A5-20261105'
def build():
 OUT.mkdir(parents=True,exist_ok=True);source=json.loads((ROOT/'ArtReview/Goryeo-Atmosphere-A7-20261105/source.json').read_text());cert=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json').read_text());rows=[]
 for m in source['maps']:
  d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{m['id']}.json").read_text());w,h=d['w'],d['h'];origins=set();places=[]
  for layer in ('obj','high'):
   for i,index in enumerate(d[layer]):
    tile=cert['tiles'].get(d['palette'][index],{})if index else{}
    for name,dx,dy in tile.get('parts',[]):
     if name in ('sprite:c7:well','sprite:c7:gaekju'):origins.add((name,i%w*16+dx,i//w*16+dy))
  for name,x,y in sorted(origins):
   with Image.open(ROOT/cert['sources'][name]['path'])as im:sw,sh=im.size
   places.append(dict(text='우물'if name.endswith(':well')else'객주',x=(x+sw*.5)/16,y=h-(y+sh)/16+.5,source=name))
  for o in d['objects']:
   if o['kind']=='ambience'and m['id']in ('Naru','transit_ferry','transit_southport'):
    land=[(x,y)for y in range(max(0,o['y']-6),min(h,o['y']+7))for x in range(max(0,o['x']-6),min(w,o['x']+7))if d['reachable'][y*w+x]and not d['blocked'][y*w+x]]
    if land:
     x,y=min(land,key=lambda p:(p[0]-o['x'])**2+(p[1]-o['y'])**2);places.append(dict(text='선착장',x=x+.5,y=h-y-.5,source='existing waterside ambience and reachable bank'))
  rows.append(dict(id=m['id'],w=w,h=h,places=places))
 (OUT/'source.json').write_text(json.dumps(dict(status='review',near=4,maps=rows,note='Only existing well/inn/river-bank labels; markets, boards, workshops and signs read live Interactables. No facilities/gameplay added.'),ensure_ascii=False,indent=2)+'\n')
 print([(m['id'],len(m['places']))for m in rows])
if __name__=='__main__':build()
