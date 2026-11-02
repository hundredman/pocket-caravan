"""Promote the user-approved market court layout to editor-build source.
Copies whole native images unchanged. Never rewrites map JSON or other regions.
"""
from pathlib import Path
import hashlib,json,shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'ArtReview/VillageFill-20261030/manifest.json'
LAYOUT=ROOT/'Assets/PocketCaravan/Data/Goryeo/MarketLayout.json'
ART=ROOT/'Assets/PocketCaravan/Art/Goryeo/Markets'
ALLOWED={'Naru','Gaegyeong','Dakmok','goryeo_jangdan','goryeo_songak','transit_woodtown','transit_ferry','transit_crossroads','transit_pass'}
def rgba(path):
 with Image.open(path)as im:return hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest()
def promote():
 source=json.loads(SOURCE.read_text())
 approval=json.loads((ROOT/'ArtReview/Approved/MarketCourts-v1/approval.json').read_text())
 rows=[dict(mapId=m['mapId'],spaces=[s for s in m['spaces']if s['props']])for m in source['maps']if any(s['props']for s in m['spaces'])]
 if approval['status']!='approved' or hashlib.sha256(json.dumps(rows,sort_keys=True,separators=(',',':')).encode()).hexdigest()!=approval['layoutSha256']:raise ValueError('Market layout changed after approval')
 used={p['asset']for m in source['maps']for s in m['spaces']for p in s['props']};assets=[]
 ART.mkdir(parents=True,exist_ok=True)
 for a in source['assets']:
  if a['id']not in used:continue
  src=ROOT/a['source'];dest=ART/(a['id']+'.png')
  if rgba(src)!=a['rgbaSha256']:raise ValueError('Reviewed art changed: '+a['id'])
  shutil.copyfile(src,dest)
  assets.append(dict(id=a['id'],image=dest.relative_to(ROOT).as_posix(),size=a['size'],source=a['source'],rgbaSha256=rgba(dest)))
 maps=[]
 for m in source['maps']:
  if not any(s['props']for s in m['spaces']):continue
  if m['mapId']not in ALLOWED:raise ValueError('Unexpected region: '+m['mapId'])
  d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{m['mapId']}.json").read_text())
  # Abort if someone has moved the underlying map since review.
  for k,v in source['inputMaps'][m['mapId']]['metadata'].items():
   if d[k]!=v:raise ValueError('Map metadata changed: '+m['mapId']+'/'+k)
  maps.append(dict(mapId=m['mapId'],width=d['w'],height=d['h'],spaces=[s for s in m['spaces']if s['props']]))
 result=dict(schemaVersion=1,status='approved',approval='User 2026-11-02: integrate the reviewed market assets into production.',reviewSource=SOURCE.relative_to(ROOT).as_posix(),assets=assets,maps=maps)
 LAYOUT.parent.mkdir(parents=True,exist_ok=True);LAYOUT.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 print(f"Production market source: {len(maps)} maps, {len(assets)} whole images, {sum(len(s['props'])for m in maps for s in m['spaces'])} placements")
if __name__=='__main__':promote()
