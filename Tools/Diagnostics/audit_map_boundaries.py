"""Read-only map split / exit audit. Never rewrites maps, scenes, or IDs."""
import ast,json,re,statistics
from pathlib import Path
from collections import deque,defaultdict
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Validation/Map-Boundary-Audit-2026-11-07'
OWN=[r['id'] for r in json.loads((ROOT/'Assets/PocketCaravan/Resources/Goryeo/PlaceLabels.json').read_text())['maps']]
ACTIVE=set(re.findall(r'^  - mapId: (.+)$',(ROOT/'Assets/PocketCaravan/Resources/WorldIndex.asset').read_text(),re.M))
STARTERS={'goryeo':['Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan'],'venice':['venice_rialto','venice_murano','venice_burano','venice_chioggia','venice_santerasmo'],'north':['north_birka','north_hunter','north_naru','north_laugar','north_herd'],'arabia':['arabia_sanaa','arabia_taiz','arabia_zabid','arabia_jeddah','arabia_ahsa']}

def exits(d):return [o for o in d.get('objects',[]) if o.get('kind')=='exit']
def cells(o):return [(x,y) for y in range(o['y'],o['y']+o.get('h',1)) for x in range(o['x'],o['x']+o.get('w',1))]
def inset(d,o):
 x,y=o['x'],o['y'];w,h=o.get('w',1),o.get('h',1)
 return {'N':y,'S':d['h']-y-h,'W':x,'E':d['w']-x-w}.get(o.get('edge'))
def bfs(d,sources):
 w,h=d['w'],d['h'];dist={p:0 for p in sources if 0<=p[0]<w and 0<=p[1]<h and not d['blocked'][p[1]*w+p[0]]};q=deque(dist)
 while q:
  x,y=q.popleft()
  for p in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
   if p not in dist and 0<=p[0]<w and 0<=p[1]<h and not d['blocked'][p[1]*w+p[0]]:dist[p]=dist[x,y]+1;q.append(p)
 return dist

def run():
 maps={}
 for p in sorted((ROOT/'Assets/PocketCaravan/Maps').rglob('*.json')):
  if p.stem not in ACTIVE:continue
  d=json.loads(p.read_text())
  if not all(k in d for k in ('w','h','ground','blocked','objects')):continue
  assert p.stem not in maps,p.stem
  maps[p.stem]=d
 mapping=next(ast.literal_eval(n.value) for n in ast.parse((ROOT/'Tools/WorldLayout/m4_export.py').read_text()).body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='MAPID' for t in n.targets))
 layout=json.loads((ROOT/'Tools/WorldLayout/world-layout.json').read_text());areas={mapping[m['key']]:m['area'] for m in layout['maps']};export_report=json.loads((ROOT/'ArtReview/M4-Maps/report.json').read_text())
 rows=[];sample_candidates=[]
 for mid,d in maps.items():
  if mid not in OWN and not mid.startswith(('venice_','north_','arabia_')):continue
  w,h=d['w'],d['h'];ee=exits(d);groups=defaultdict(set)
  for o in ee:groups[o.get('target','')].update(cells(o))
  start=next((o for o in d['objects'] if o['kind']=='spawn' and o.get('id')=='S0'),None)
  if start:
   seeds=cells(start);reference='default spawn S0'
  else:
   market=next((o for o in d['objects'] if o['kind']=='market'),None)
   seeds=[(market['x']+dx,market['y']+dy) for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))] if market else []
   reference='walkable cells beside first market' if market else 'none'
  dist=bfs(d,seeds);routes={target:min((dist[c] for c in cc if c in dist),default=None) for target,cc in groups.items()}
  crossing=[]
  for target,cc in groups.items():
   dd=bfs(d,cc)
   for other,opp in groups.items():
    if target<other:
     ds=[dd[c] for c in opp if c in dd]
     if ds:crossing.append({'from':target,'to':other,'steps':min(ds),'nominalWalkSeconds':round(min(ds)/3.8,2)})
  depths=[inset(d,o) for o in ee if inset(d,o) is not None]
  row={'id':mid,'name':d.get('name'), 'w':w,'h':h,'boundingArea':w*h,'b1AssignedArea':areas.get(mid),'exportReportedArea':export_report.get(mid,{}).get('cells'),'unblockedCells':sum(not b for b in d['blocked']),'paintedGroundCells':sum(bool(b) for b in d['ground']),'backdropCells':len(d.get('cameraBackdrop',[])),'exitObjects':len(ee),'uniqueTargets':len(groups),'directionalInsetExits':sum(x>1 for x in depths),'exitsAtLeastHalfScreenInside':sum(x>=(15 if o.get('edge') in 'EW' else 8.4375) for x,o in zip(depths,[o for o in ee if inset(d,o) is not None])),'maxDirectionalInset':max(depths,default=0),'routeReference':reference,'referenceToExitSteps':routes,'betweenNeighborMinRoutes':sorted(crossing,key=lambda x:x['steps'])}
  rows.append(row)
  for o in ee:
   depth=inset(d,o)
   if depth is None:continue
   reach=[dist[c] for c in cells(o) if c in dist]
   onroad=any(d.get('road',[0]*(w*h))[y*w+x]>0 for x,y in cells(o))
   dx,dy={'E':(1,0),'W':(-1,0),'N':(0,-1),'S':(0,1)}[o['edge']]
   approach=[]
   for x,y in cells(o):
    clear=all(0<=x-dx*n<w and 0<=y-dy*n<h and not d['blocked'][(y-dy*n)*w+x-dx*n] for n in (0,1,2))
    if clear:approach.append((x,y))
   if approach:sample_candidates.append({'mapId':mid,'target':o['target'],'edge':o['edge'],'x':o['x'],'y':o['y'],'w':o.get('w',1),'h':o.get('h',1),'insetCells':depth,'onRoad':onroad,'spawnDistance':min(reach,default=9999),'probeX':approach[len(approach)//2][0],'probeY':approach[len(approach)//2][1]})
 # Actual incoming spawn regions, rather than arbitrary points on the exit line.
 for row in rows:
  mid=row['id'];d=maps[mid];spawns={o.get('id'):o for o in d['objects'] if o['kind']=='spawn'};incoming=defaultdict(set)
  for source,other in maps.items():
   for ex in exits(other):
    if ex.get('target')==mid and ex.get('spawn') in spawns:incoming[source].update(cells(spawns[ex['spawn']]))
  routes=[];outgoing=defaultdict(set)
  for ex in exits(d):outgoing[ex.get('target')].update(cells(ex))
  for source,points in incoming.items():
   dd=bfs(d,points)
   for target,points_out in outgoing.items():
    if target==source:continue
    steps=[dd[c] for c in points_out if c in dd]
    if steps:routes.append({'from':source,'to':target,'steps':min(steps),'nominalWalkSeconds':round(min(steps)/3.8,2)})
  row['arrivalToDifferentNeighborRoutes']=sorted(routes,key=lambda r:r['steps'])
 ownrows=[r for r in rows if r['id'] in OWN]
 regions={}
 for region,ids in STARTERS.items():
  rr=[r for r in rows if r['id'] in ids];regions[region]={'ids':[r['id'] for r in rr],'count':len(rr),'widthRange':[min(r['w'] for r in rr),max(r['w'] for r in rr)],'heightRange':[min(r['h'] for r in rr),max(r['h'] for r in rr)],'medianBoundingArea':statistics.median(r['boundingArea'] for r in rr),'medianUnblockedCells':statistics.median(r['unblockedCells'] for r in rr),'medianB1AssignedArea':statistics.median(r['b1AssignedArea'] for r in rr) if region=='goryeo' else None,'exitObjects':sum(r['exitObjects'] for r in rr),'directionalInsetExits':sum(r['directionalInsetExits'] for r in rr)}
 samples=[]
 for mid in ('Naru','goryeo_songak'):
  cc=[c for c in sample_candidates if c['mapId']==mid and c['target'] in OWN and c['insetCells']>16]
  cc.sort(key=lambda c:(not c['onRoad'],c['spawnDistance']))
  if cc:samples.append(cc[0])
 for mid in ('north_birka','venice_rialto'):
  cc=[c for c in sample_candidates if c['mapId']==mid and c['insetCells']<=1]
  cc.sort(key=lambda c:(not c['onRoad'],c['spawnDistance']))
  if cc:samples.append(cc[0])
 report={'scope':'read-only current registered map JSON, current layout ownership, nominal four-neighbor grid routes','cameraReference':{'orthographicSize':8.4375,'aspect':16/9,'viewCells':[30,16.875]},'ownMapCount':len(OWN),'b1AssignedAreaBudget':sum(areas.values()),'exportReportedAssignedArea':sum(export_report[mid]['cells'] for mid in OWN),'ownExitObjects':sum(r['exitObjects'] for r in ownrows),'ownInteriorExitObjects':sum(r['directionalInsetExits'] for r in ownrows),'starterComparison':regions,'maps':rows,'samples':samples,'limits':['boundingArea includes neighbor context and sea; not playable/owned area','routes ignore dynamic NPC/event collisions, weather, load and diagonal shortcuts; seconds are not human play time','a one-cell inset on a team map is grouped as screen-edge-adjacent; >1 is the interior statistic','B1 areas are budgets; M4 export area is a prior generation report, not freshly regenerated ownership; routes use current production JSON']}
 OUT.mkdir(parents=True,exist_ok=True);(OUT/'metrics.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
 print(json.dumps({k:report[k] for k in ('ownExitObjects','ownInteriorExitObjects','starterComparison','samples')},ensure_ascii=False,indent=2))
if __name__=='__main__':run()
