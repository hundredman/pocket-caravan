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
ALLOWED={'Naru','Gaegyeong','Dakmok','goryeo_jangdan','goryeo_songak','transit_woodtown','transit_ferry','transit_crossroads','transit_pass','transit_southport','transit_central_approach','transit_south_inland','transit_wetroad'}
# 11/03 E2: 새 장터에 승인된 그림을 더 놓는 배치. status가 review면 은샘 그림 검토 중(본편에 넣어 화면으로 확인).
EXTENSIONS=[ROOT/'ArtReview/MarketCourts-E2-20261103/layout.json']
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
 ext=[]
 for path in EXTENSIONS:
  e=json.loads(path.read_text())
  if e['status'] not in('review','approved'):continue
  if e.get('assets'):
   approved=json.loads((ROOT/e['approval']).read_text())
   digest=hashlib.sha256(json.dumps(e['maps'],sort_keys=True,separators=(',',':')).encode()).hexdigest()
   if e['status']!='approved' or approved['status']!='approved' or approved['layoutSha256']!=digest or approved['assets']!=e['assets']:raise ValueError('Extension changed after approval')
   if {m['mapId']for m in e['maps']}!=set(approved['maps']):raise ValueError('Extension map scope changed')
   for a in e['assets']:
    src=ROOT/a['source'];dest=ART/(a['id']+'.png')
    if a['id'] in used or rgba(src)!=a['rgbaSha256']:raise ValueError('Unapproved/duplicate extension art: '+a['id'])
    with Image.open(src)as im:
     if im.size!=tuple(n*16 for n in a['size']):raise ValueError('Wrong extension native size')
    shutil.copyfile(src,dest);used.add(a['id'])
    assets.append(dict(id=a['id'],image=dest.relative_to(ROOT).as_posix(),size=a['size'],source=a['source'],rgbaSha256=rgba(dest)))
  ext.append(dict(source=path.relative_to(ROOT).as_posix(),status=e['status']))
  for em in e['maps']:
   if em['mapId']not in ALLOWED:raise ValueError('Unexpected region: '+em['mapId'])
   if any(p['asset']not in used for s in em['spaces']for p in s['props']):raise ValueError('Extension uses unapproved art: '+em['mapId'])
   cur=next((m for m in maps if m['mapId']==em['mapId']),None)
   if cur is None:
    d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{em['mapId']}.json").read_text())
    cur=dict(mapId=em['mapId'],width=d['w'],height=d['h'],spaces=[]);maps.append(cur)
   have={s['id']for s in cur['spaces']}
   if have&{s['id']for s in em['spaces']}:raise ValueError('Extension overlaps reviewed space: '+em['mapId'])
   cur['spaces']+=em['spaces']
 result=dict(schemaVersion=1,status='approved',approval='User 2026-11-02: integrate the reviewed market assets into production.',reviewSource=SOURCE.relative_to(ROOT).as_posix(),extensions=ext,assets=assets,maps=maps)
 LAYOUT.parent.mkdir(parents=True,exist_ok=True);LAYOUT.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 print(f"Production market source: {len(maps)} maps, {len(assets)} whole images, {sum(len(s['props'])for m in maps for s in m['spaces'])} placements")
def promote_fair():
 source=ROOT/'ArtReview/CentralFair-Task4-20261104'
 approval=json.loads((source/'approval.json').read_text())
 if approval['status']!='approved' or approval['mapIds']!=['transit_woodtown','transit_ferry'] or approval['target']!='grain_stall':raise ValueError('Unapproved fair scope')
 assets=[]
 for row in approval['variants']:
  name=row['asset']
  if name not in ('fair_indigo','fair_teal'):raise ValueError('Unknown fair art')
  src=source/'Native'/(name+'.png');dest=ART/(name+'.png')
  with Image.open(src)as im:
   if im.size!=(48,48):raise ValueError('Fair size changed')
  if rgba(src)!=row['rgbaSha256']:raise ValueError('Approved fair pixels changed')
  ART.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dest)
  assets.append(dict(id=name,image=dest.relative_to(ROOT).as_posix(),size=[3,3],rgbaSha256=rgba(dest)))
 if len(assets)!=2:raise ValueError('Expected two variants')
 layout=dict(schemaVersion=1,status='approved',reviewSource=(source/'approval.json').relative_to(ROOT).as_posix(),mapIds=approval['mapIds'],target=approval['target'],assets=assets)
 (LAYOUT.parent/'FairLayout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n')
 print('Approved fair art: two maps, two whole 48x48 variants; footprints unchanged')
if __name__=='__main__':
 import argparse
 parser=argparse.ArgumentParser();parser.add_argument('--fair-only',action='store_true');args=parser.parse_args()
 if args.fair_only:promote_fair()
 else:
  promote()
  if (ROOT/'ArtReview/CentralFair-Task4-20261104/approval.json').exists():promote_fair()
