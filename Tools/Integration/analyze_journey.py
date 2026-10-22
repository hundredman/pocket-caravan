"""고려·중간권 장터 사이 정적 이동 비용을 계산한다. Asset/씬/저장은 수정하지 않는다.
실행: python3 Tools/Integration/analyze_journey.py
직교·화물5·기본 걷기, NPC/대화/매매/달리기/가속/로딩은 제외한다.
"""
import json,re,heapq,itertools
from pathlib import Path
R=Path(__file__).resolve().parents[2];D=R/'Assets/PocketCaravan/Data/Goryeo'
world={}
for rid in ('goryeo','transit'):
 for e in json.loads((R/f'Assets/PocketCaravan/World/Regions/{rid}.json').read_text())['maps']:
  world[e['mapId']]=json.loads((R/e['jsonPath']).read_text())
def inside(m,x,y):return 0<=x<m['w'] and 0<=y<m['h'] and not m['blocked'][y*m['w']+x]
def market_cells(mid):
 m=world[mid];o=next(o for o in m['objects'] if o['kind']=='market')
 return [(mid,x,y) for x,y in ((o['x']+1,o['y']),(o['x']-1,o['y']),(o['x'],o['y']+1),(o['x'],o['y']-1)) if inside(m,x,y)]
jumps={}
for mid,m in world.items():
 for o in m['objects']:
  if o['kind']!='exit':continue
  target=world[o['target']];sp=next(p for p in target['objects'] if p['kind']=='spawn' and p['id']==o['spawn'])
  for dy in range(o.get('h',1)):
   for dx in range(o.get('w',1)):
    jumps.setdefault((mid,o['x']+dx,o['y']+dy),[]).append((o['target'],sp['x']+min(dx,sp.get('w',1)-1),sp['y']+min(dy,sp.get('h',1)-1)))
def cost(a,b,vehicle,cargo_weight=5):
 text=(D/f'Vehicle_{vehicle}.asset').read_text();speeds=[float(x) for x in re.findall(r'^  - ([0-9.]+)$',text.split('speedByGrade:')[1],re.M)]
 capacity=float(re.search(r'capacity: (\S+)',text)[1]);load=1-.3*min(1,max(0,cargo_weight)/capacity)
 q=[];dist={};targets=set(market_cells(b));prev={}
 for p in market_cells(a):dist[p]=0.;heapq.heappush(q,(0.,p))
 while q:
  c,p=heapq.heappop(q)
  if c!=dist[p]:continue
  if p in targets:
   transitions=0;cur=p;map_path=[p[0]]
   while cur in prev:
    last=prev[cur];transitions+=last[0]!=cur[0]
    if last[0]!=cur[0]:map_path.append(last[0])
    cur=last
   return dict(seconds=round(c,1),transitions=transitions,maps=list(reversed(map_path)))
  mid,x,y=p;m=world[mid];i=y*m['w']+x
  neighbors=[]
  for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
   if inside(m,xx,yy):neighbors.append(((mid,xx,yy),(.5/speeds[m['road'][i]]+.5/speeds[m['road'][yy*m['w']+xx]])/(3.8*load)))
  for jump in jumps.get(p,[]):neighbors.append((jump,.30))
  for target,delta in neighbors:
   nc=c+delta
   if nc<dist.get(target,float('inf')):dist[target]=nc;prev[target]=p;heapq.heappush(q,(nc,target))
 raise RuntimeError((a,b))
if __name__=='__main__':
 pairs=[('Naru','Gaegyeong'),('Gaegyeong','Dakmok'),('Gaegyeong','goryeo_songak'),('Dakmok','goryeo_jangdan'),('goryeo_songak','Dakmok'),('Naru','goryeo_jangdan'),('goryeo_jangdan','transit_ferry')]
 results=[]
 for a,b in pairs:results.append({'from':a,'to':b,**{v:cost(a,b,v) for v in ('Walk','Handcart')}})
 goryeo=set(world)-{k for k in world if k.startswith('transit_')}
 edges={tuple(sorted((mid,o['target']))) for mid,m in world.items() for o in m['objects'] if o['kind']=='exit' and mid in goryeo and o['target'] in goryeo}
 report={'scope':'Current JSON orthogonal shortest walkable routes between market interaction cells; cargo weight 5; no running/origin bonus/acceleration/stamina/NPC/learning/trade/dialogue; .30 second transition fade only; load latency excluded. Calculation, not observed human playtime. Diagonal/free movement may be faster.','routes':results,'goryeoGraph':{'nodes':len(goryeo),'undirectedEdges':len(edges),'independentCycles':len(edges)-len(goryeo)+1,'edges':sorted(edges)}}
 print(json.dumps(report,ensure_ascii=False,indent=2))
