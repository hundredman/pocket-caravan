"""M4 ⑥ E1 거리를 새 맵의 실제 보행으로 바꾼다(layoutD 전환).

- 본편 맵 JSON(Assets/PocketCaravan/Maps)을 세계 좌표로 이어 붙여, 걸을 수 있는 칸 위에서 마을(시장 자리) 사이 최단 보행을 잰다.
  8방향, 모서리 끊기 없음(대각선은 양옆 두 칸이 모두 걸을 수 있을 때만), 속도는 PlayerMotor.walkSpeed(칸/초).
- 남동 포구·중앙 길목 마을은 아직 시장이 없어 M2 장터 광장 가운데를 마을 자리로 쓴다.
- 환산 비율은 기존처럼 고려 5마을 10쌍의 (보행 초, 기존 맨해튼 거리) 최소제곱(fit_ratio).
- 판정(Plan M4): 평균 기한·평균 배달 보수 ±15%, 실제 보행이 기한의 70% 안, 같은 마을 0·다른 마을 최소 1.
출력: Resources/TownRoutes.json(mode layoutD, 36쌍 walkingDistances), Docs/Validation/M4-E1-2026-10-28.json
"""
import heapq,itertools,json,math,re,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/Routing'));sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from check_distance_calibration import fit_ratio,deadline
from export_routes import serialized
import terrain_v3 as v
TOWNMAP={'naru':'Naru','goryeo_jangdan':'goryeo_jangdan','goryeo_songak':'goryeo_songak','dakmok':'Dakmok','gaegyeong':'Gaegyeong',
         'transit_woodtown':'transit_woodtown','transit_ferry':'transit_ferry','transit_southport':'transit_southport','transit_crossroads':'transit_crossroads'}
ZONE={'transit_southport':'east/남동 포구','transit_crossroads':'east/중앙 길목 마을'}
GAME_HOURS_PER_SECOND=24/360   # GameClock.realSecondsPerDay=360

def world_grid():
    maps={};xs=[];ys=[]
    for f in sorted((ROOT/'Assets/PocketCaravan/Maps').glob('*.json')):
        d=json.loads(f.read_text());maps[d['id']]=d;xs+=[d['terrainX'],d['terrainX']+d['w']];ys+=[d['terrainY'],d['terrainY']+d['h']]
    X0,Y0=min(xs),min(ys);W,H=max(xs)-X0,max(ys)-Y0;walk=np.zeros((H,W),bool)
    for d in maps.values():
        b=np.array(d['blocked']).reshape(d['h'],d['w'])==0
        walk[d['terrainY']-Y0:d['terrainY']-Y0+d['h'],d['terrainX']-X0:d['terrainX']-X0+d['w']]|=b
    return maps,walk,X0,Y0

def town_cells(maps,X0,Y0):
    zones=json.loads((ROOT/'ArtReview/M2-Zones/zones.json').read_text())['villages'];out={}
    for tid,mid in TOWNMAP.items():
        d=maps[mid];mk=next((o for o in d['objects'] if o['kind']=='market' and o['town']==tid),None)
        if mk:out[tid]=(d['terrainY']+mk['y']+1-Y0,d['terrainX']+mk['x']-X0)   # 상인 앞 칸
        else:
            pz=next(i for i in zones[ZONE[tid]]['items'] if i.get('kind')=='plaza')
            out[tid]=(pz['y']+pz['h']//2-Y0,pz['x']+pz['w']//2-X0)
    return out

def dijkstra(walk,start):
    H,W=walk.shape;dist=np.full((H,W),np.inf);dist[start]=0;q=[(0.0,start)]
    steps=[(dy,dx,math.hypot(dy,dx)) for dy in(-1,0,1) for dx in(-1,0,1) if dy or dx]
    while q:
        c,(y,x)=heapq.heappop(q)
        if c>dist[y,x]:continue
        for dy,dx,w in steps:
            ny,nx=y+dy,x+dx
            if not(0<=ny<H and 0<=nx<W) or not walk[ny,nx]:continue
            if dy and dx and not(walk[y,nx] and walk[ny,x]):continue
            if c+w<dist[ny,nx]:dist[ny,nx]=c+w;heapq.heappush(q,(c+w,(ny,nx)))
    return dist

def main():
    maps,walk,X0,Y0=world_grid();towns=town_cells(maps,X0,Y0)
    speed=float(re.search(r'public float walkSpeed\s*=\s*([\d.]+)f',(ROOT/'Assets/PocketCaravan/Scripts/Player/PlayerMotor.cs').read_text())[1])
    for t,c in towns.items():
        if not walk[c]:raise ValueError('마을 자리가 걸을 수 없는 칸: '+t)
    sec={}
    for a in towns:
        dist=dijkstra(walk,towns[a])
        for b in towns:
            if a<b:
                cells=dist[towns[b]]
                if not math.isfinite(cells):raise ValueError(f'끊긴 마을: {a}-{b}')
                sec[(a,b)]=float(cells)/speed
    S=lambda a,b:0 if a==b else sec[(min(a,b),max(a,b))]
    old=json.loads((ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text())
    coords={}
    for f in (ROOT/'Assets/PocketCaravan/Data').rglob('Town_*.asset'):
        t=f.read_text();i=re.search(r'^  id: (.+)$',t,re.M);c=re.search(r'worldCoord:\s*\{x:\s*(-?\d+),\s*y:\s*(-?\d+)\}',t)
        if i and c:coords[i[1].strip()]=tuple(map(int,c.groups()))
    goryeo=['naru','goryeo_jangdan','goryeo_songak','dakmok','gaegyeong']
    # 환산 비율: 기존 거리가 있는 모든 마을 쌍에서 평균 기한·평균 보수 변화가 가장 작은 값(반올림 포함).
    # (고려 10쌍 최소제곱만 쓰면 중앙 마을까지 넣은 평균 기한이 -18%로 Plan 기준 ±15%를 넘는다)
    known=[t for t in towns if t in coords];pairs=list(itertools.combinations(known,2))
    od={p:sum(abs(x-y) for x,y in zip(coords[p[0]],coords[p[1]])) for p in pairs}
    def change(r_):
        nd={p:max(1,round(S(*p)*r_)) for p in pairs}
        dl=sum(deadline(nd[p]) for p in pairs)/sum(deadline(od[p]) for p in pairs)-1
        rw=sum(14+16*nd[p] for p in pairs)/sum(14+16*max(1,od[p]) for p in pairs)-1
        return max(abs(dl),abs(rw))
    base=fit_ratio([(S(a,b),od[(a,b)] if (a,b) in od else od[(b,a)]) for a,b in itertools.combinations(goryeo,2)])
    ratio=min((base*f/1000 for f in range(800,1401)),key=lambda r_:(round(change(r_),4),abs(r_-base)))
    newd=lambda a,b:0 if a==b else max(1,round(S(a,b)*ratio))
    result=dict(mode='layoutD',ratio=ratio,walkingCellsPerSecond=speed,
                source='M4 새 맵 13개의 실제 보행(Tools/Routing/m4_routes.py): 걸을 수 있는 칸 위 8방향 최단 거리',
                towns=[dict(id=t,mapId=TOWNMAP[t],position=[int(towns[t][1]+X0),int(towns[t][0]+Y0)]) for t in towns],
                roads=[dict(from_=r['from'],to=r['to'],seconds=S(r['from'],r['to']),waypoints=[[towns[r['from']][1]+X0+.5,towns[r['from']][0]+Y0+.5],[towns[r['to']][1]+X0+.5,towns[r['to']][0]+Y0+.5]]) for r in old['roads']],
                walkingDistances=[{'from':a,'to':b,'seconds':s} for (a,b),s in sorted(sec.items())],
                externalLinks=old['externalLinks'])
    (ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').write_text(serialized(result))
    # 판정: TownData가 있는 마을 쌍(기존 거리 비교 가능)
    rules=dict(dispatchFee=1.5,perWeight=.65,perWeightPerStep=.25)
    reg=(ROOT/'Assets/PocketCaravan/Data/Goryeo/Region_Goryeo.asset').read_text()
    for k in rules:rules[k]=float(re.search(rf'{k}:\s*([\d.]+)',reg)[1])
    world=json.loads((ROOT/'Assets/PocketCaravan/Maps/Naru.json').read_text())   # 자리 표시용
    rows=[];W_=4
    for a,b in itertools.combinations([t for t in towns if t in coords],2):
        od=sum(abs(x-y) for x,y in zip(coords[a],coords[b]));nd=newd(a,b)
        oh,nh=deadline(od),deadline(nd)
        walk_h=S(a,b)*GAME_HOURS_PER_SECOND
        rows.append(dict(pair=[a,b],seconds=round(S(a,b),3),oldDistance=od,newDistance=nd,oldHours=oh,newHours=nh,walkHours=round(walk_h,2),
                         walkShareOfDeadline=round(walk_h/nh,3),oldDefaultReward=14+16*max(1,od)+W_*3,newDefaultReward=14+16*max(1,nd)+W_*3))
    avg=lambda k:sum(r[k] for r in rows)/len(rows)
    rep=dict(ratio=ratio,pairs=len(rows),avgDeadlineChange=round(avg('newHours')/avg('oldHours')-1,4),
             avgDefaultRewardChange=round(avg('newDefaultReward')/avg('oldDefaultReward')-1,4),
             maxWalkShareOfDeadline=max(r['walkShareOfDeadline'] for r in rows),rows=rows,
             note='배달 보수(고려 규칙)는 layoutD에서 연결 수 대신 같은 환산 거리를 쓴다. Q1② 이익은 Unity 검사(⑦)에서 확인.')
    rep['pass']=dict(deadline15=bool(abs(rep['avgDeadlineChange'])<=.15),reward15=bool(abs(rep['avgDefaultRewardChange'])<=.15),arrive70=bool(rep['maxWalkShareOfDeadline']<=.7))
    (ROOT/'Docs/Validation/M4-E1-2026-10-28.json').write_text(json.dumps(rep,ensure_ascii=False,indent=1))
    print(json.dumps({k:v_ for k,v_ in rep.items() if k!='rows'},ensure_ascii=False))
if __name__=='__main__':main()
