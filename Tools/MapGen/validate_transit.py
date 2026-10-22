"""중간권의 경로 비용·통행·연결 거리 검사. 체감 재미/멀티 검증은 별도."""
import heapq
import json
import re
from pathlib import Path
from world_registry import atlas_content
import cartography
ROOT = Path(__file__).resolve().parents[2]


def travel_cost(m, speeds, branch):
    start,end=(7,19),(48,19)
    q=[(0.,start)];costs={start:0.}
    while q:
        c,p=heapq.heappop(q)
        if c != costs[p]:continue
        if p == end:return c
        x,y=p
        for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
            if not m.inside(xx,yy) or m.blocked[yy][xx] or not m.road[yy][xx]:continue
            if 20 <= xx <= 35:
                main = yy>=23 if m.id=='transit_pass' else yy<=16
                if (branch=='main') != main:continue
            n=c+.5/speeds[m.road[y][x]]+.5/speeds[m.road[yy][xx]]
            if n < costs.get((xx,yy),float('inf')):
                costs[xx,yy]=n;heapq.heappush(q,(n,(xx,yy)))
    raise AssertionError(f'{m.id}/{branch} unreachable')


def validate():
    _,world=atlas_content();by_id={m.id:m for m in world};pos=cartography.layout(world)
    # 새 구역이 기존 맵 위에 겹쳐 그려져 가려지지 않는가?
    for a in world:
        for b in world:
            if a.id>=b.id:continue
            ax,ay=pos[a.id];bx,by=pos[b.id]
            assert not (max(ax,bx)<min(ax+a.w,bx+b.w) and max(ay,by)<min(ay+a.h,by+b.h)),(a.id,b.id)
    checks=['atlas map rectangles do not overlap']
    route_results={}
    for mid in ['transit_pass','transit_wetroad']:
        m=by_id[mid];route_results[mid]={}
        for v,file in [('walk','Vehicle_Walk.asset'),('cart','Vehicle_Handcart.asset')]:
            text=(ROOT/'Assets/PocketCaravan/Data/Goryeo'/file).read_text()
            speeds=[float(x) for x in re.findall(r'^  - ([0-9.]+)$',text.split('speedByGrade:')[1],re.M)]
            c={b:travel_cost(m,speeds,b) for b in ['short','main']}
            expected='short' if v=='walk' else 'main'
            assert c[expected]<c['main' if expected=='short' else 'short'],(mid,v,c)
            route_results[mid][v]={k:round(val,3) for k,val in c.items()}
            checks.append(f'{mid}: {v} favors {expected}')
    # 다른 문명에 바로 도착하지 않고 여러 구역을 거친 후 종점에 도달한다.
    todo=[('goryeo_jangdan',0)];seen=set();distance=None
    for mid,n in todo:
        if mid in seen:continue
        seen.add(mid)
        if mid=='transit_ferry':distance=n;break
        todo.extend((o['target'],n+1) for o in by_id[mid].objects if o['kind']=='exit')
    assert distance==4,distance
    checks.append('Jangdan to ferry still requires four map transitions')
    todo=[('goryeo_jangdan',0)];seen=set();central=None
    for mid,n in todo:
        if mid in seen:continue
        seen.add(mid)
        if mid=='transit_crossroads':central=n;break
        todo.extend((o['target'],n+1) for o in by_id[mid].objects if o['kind']=='exit')
    assert central==6,central
    checks.append('Jangdan to central crossroads requires six distinct transitions')
    return dict(checks=checks,road_costs=route_results,scope='직교 노면 경로 비용(기본속도 배수 적용 전). 잔디 자유 이동·가속·달리기·NPC 제외. 시간/재미/온라인 성능의 실측값이 아님.')


if __name__=='__main__':
    result=validate();out=ROOT/'output/transit';out.mkdir(parents=True,exist_ok=True)
    (out/'route-checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
    print(json.dumps(result,ensure_ascii=False,indent=2))
