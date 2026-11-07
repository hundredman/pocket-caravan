"""Read-only dock/route proposal. Does not regenerate maps, scenes or shared data."""
import json
from pathlib import Path
from collections import deque
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'WorldLayout'))
from t3_history import map_at
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Ferry-20261105'
IDS=[('Naru','예성 나루','naru'),('transit_ferry','버들나루','transit_ferry'),('transit_southport','남동 포구','transit_southport')]
def build(write=False,historical=False):
 ports=[]
 for mid,name,town in IDS:
  data=(map_at((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json')) if historical else json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text()));w,h=data['w'],data['h'];people=[o for o in data['objects']if o['kind']=='talker'and o.get('name')=='뱃사공'];assert len(people)==1;person=people[0];x,y=person['x'],person['y'];arrival=(x-1,y)
  def open_cell(c):
   a,b=c
   return 0<=a<w and 0<=b<h and data['ground'][b*w+a]!=0 and not data['blocked'][b*w+a]and not any(o['kind']in['npc','talker','market','board','sign']and(o['x'],o['y'])==c for o in data['objects'])
  assert open_cell(arrival),(mid,arrival);market=next(o for o in data['objects']if o['kind']=='market');target=(market['x'],market['y']);q=deque([arrival]);seen={arrival};found=False
  while q:
   a,b=q.popleft()
   if abs(a-target[0])+abs(b-target[1])<=2:found=True;break
   for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
    c=(a+dx,b+dy)
    if c not in seen and open_cell(c):seen.add(c);q.append(c)
  assert found,(mid,'no static route to market')
  ports.append(dict(mapId=mid,name=name,townId=town,width=w,height=h,npcX=x,npcY=y,arrivalX=arrival[0],arrivalY=arrival[1]))
 routes=json.loads((ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text());comparisons=[v for v in routes['walkingDistances']if v['from']in[t[2]for t in IDS]and v['to']in[t[2]for t in IDS]]
 result=dict(status='review',fare=8,hours=1.0,ports=ports,proposal='All three ports connect directly; cargo/vehicle unchanged; no minigame/new unlock.',walkingSeconds=comparisons)
 if write:
  OUT.mkdir(parents=True,exist_ok=True);(OUT/'source.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 return result
if __name__=='__main__':print(json.dumps(build(write=True)['ports'],ensure_ascii=False))
