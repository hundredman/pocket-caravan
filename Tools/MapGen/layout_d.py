"""B2 terrain-only layoutD review generator. Never overwrites production Maps/Scenes.

B1 is the ownership/coast/town source. Generate global terrain before splitting maps,
so ownership boundaries cannot change the coast, rivers, cliffs or road tile choice.
Settlement contents and runtime boundary travel are B3/B4/B5, not this preview.
"""
import argparse
from collections import deque
import heapq
import itertools
import json
import math
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
sys.path.insert(0,str(ROOT/'Tools/Routing'))
from world_layout import WorldLayout
from export_routes import BINDINGS
from check_distance_calibration import graph_from_roads, fit_ratio, shortest_path, TOWNS
from tiles import autotile

DIRECTIONS=((1,0),(0,1),(-1,0),(0,-1))
EIGHT=DIRECTIONS+((1,1),(-1,1),(-1,-1),(1,-1))
OUT=ROOT/'ArtReview/B2-LayoutD'


def cell(point):return tuple(math.floor(v) for v in point)


def neighbors(p):return [(p[0]+dx,p[1]+dy) for dx,dy in DIRECTIONS]


def route(start,end,allowed,extra=None):
    """Deterministic four-neighbor A*: the player can walk each resulting step."""
    if start not in allowed or end not in allowed:raise ValueError('Route endpoint outside allowed cells')
    def deviation(p):
        ax,ay=start;bx,by=end;dx=bx-ax;dy=by-ay
        return abs(dy*p[0]-dx*p[1]+bx*ay-by*ax)/max(1,math.hypot(dx,dy))
    penalty=extra or (lambda p:0)
    queue=[(0,0,start)];cost={start:0};parent={start:None}
    while queue:
        _,g,p=heapq.heappop(queue)
        if g!=cost[p]:continue
        if p==end:
            path=[]
            while p is not None:path.append(p);p=parent[p]
            return path[::-1]
        for q in neighbors(p):
            if q not in allowed:continue
            value=g+1+penalty(q)+.001*deviation(q)
            if value<cost.get(q,math.inf):
                cost[q]=value;parent[q]=p
                heapq.heappush(queue,(value+abs(q[0]-end[0])+abs(q[1]-end[1]),value,q))
    raise ValueError(f'No assigned-land route: {start} -> {end}')


def worn_route(start,end,allowed,water,existing):
    """Least effort route through smooth terrain; nearby tracks share a corridor.

    Eight-direction steps follow the motor. Positive smooth costs make bends emerge
    from terrain, not arbitrary random waypoints. Diagonal corner cutting is forbidden.
    """
    def relief(p):
        x,y=p
        return (math.sin(x*.071+y*.029)+math.sin(x*.023-y*.097)+.7*math.sin(x*.193+y*.117)+2.7)/5.4
    dx,dy=end[0]-start[0],end[1]-start[1];length=math.hypot(dx,dy)
    perpendicular=(-dy/length,dx/length)
    count=max(3,math.ceil(length/10));offsets=[]
    for i in range(count+1):
        u=i/count;base=(start[0]+dx*u,start[1]+dy*u)
        choices=[]
        for offset in range(-10,11):
            point=(round(base[0]+perpendicular[0]*offset),round(base[1]+perpendicular[1]*offset))
            if point in allowed and point not in water:
                choices.append((3*relief(point)+.004*offset*offset,offset))
        offsets.append(0 if i in (0,count) or not choices else min(choices)[1])
    for _ in range(2):offsets=[0]+[(offsets[i-1]+2*offsets[i]+offsets[i+1])/4 for i in range(1,count)]+[0]
    guide=[(start[0]+dx*i/count+perpendicular[0]*offsets[i],start[1]+dy*i/count+perpendicular[1]*offsets[i]) for i in range(count+1)]
    def guide_distance(p):
        best=math.inf
        for a,b in zip(guide,guide[1:]):
            vx,vy=b[0]-a[0],b[1]-a[1]
            u=max(0,min(1,((p[0]-a[0])*vx+(p[1]-a[1])*vy)/max(.001,vx*vx+vy*vy)))
            best=min(best,math.hypot(p[0]-a[0]-u*vx,p[1]-a[1]-u*vy))
        return best
    def effort(p):
        # A gently changing desire corridor follows broad low-effort contours;
        # nearby established tracks are cheaper, while river beds are expensive.
        return (.82 if p in existing else 1.0)*(1+.7*relief(p))+.07*guide_distance(p)**2+(30 if p in water else 0)
    queue=[(0,0,start)];cost={start:0};parent={start:None}
    while queue:
        _,g,p=heapq.heappop(queue)
        if g!=cost[p]:continue
        if p==end:
            result=[]
            while p is not None:result.append(p);p=parent[p]
            return result[::-1]
        for dx,dy in EIGHT:
            q=p[0]+dx,p[1]+dy
            if q not in allowed:continue
            if dx and dy and ((p[0]+dx,p[1]) not in allowed or (p[0],p[1]+dy) not in allowed):continue
            value=g+math.hypot(dx,dy)*(effort(p)+effort(q))/2
            if value<cost.get(q,math.inf):
                cost[q]=value;parent[q]=p
                heapq.heappush(queue,(value+.48*math.dist(q,end),value,q))
    raise ValueError('No natural walking corridor')


def walking_routes(t,speed):
    """Actual shortest walks over the union of generated roads, including junctions."""
    result=[];roads=t['road'];towns=t['towns']
    for a,b in itertools.combinations(towns,2):
        start,end=towns[a],towns[b];cost={start:0};queue=[(0,start)]
        while queue:
            g,p=heapq.heappop(queue)
            if g!=cost[p]:continue
            if p==end:break
            for dx,dy in EIGHT:
                q=p[0]+dx,p[1]+dy
                if q not in roads:continue
                if dx and dy and ((p[0]+dx,p[1]) not in roads or (p[0],p[1]+dy) not in roads):continue
                value=g+math.hypot(dx,dy)
                if value<cost.get(q,math.inf):
                    cost[q]=value;heapq.heappush(queue,(value,q))
        if end not in cost:raise ValueError('Actual road network disconnected')
        result.append(dict(fromKey=a,toKey=b,seconds=cost[end]/speed))
    return result


def expand(points,radius,allowed):
    return {(x+dx,y+dy) for x,y in points for dx in range(-radius,radius+1)
            for dy in range(-radius,radius+1) if abs(dx)+abs(dy)<=radius and (x+dx,y+dy) in allowed}


def terrain(layout):
    owned=set(layout.owners);land=owned&layout.land
    towns={t['key']:cell(t['position']) for t in layout.data['towns']}
    # Three ranges explicitly grouped in the approved source comments. Southern
    # central peaks outside our assignment remain reference data and are not built.
    ranges=(layout.data['mountains'][:6],layout.data['mountains'][6:9],layout.data['mountains'][9:12])
    ridges=set()
    for points in ranges:
        for a,b in zip(points,points[1:]):ridges.update(route(cell(a),cell(b),land))
    mountains=expand(ridges,2,land)
    river_records=[];rivers=set()
    for source in layout.data['rivers']:
        points=[cell(p) for p in source['waypoints']]
        summit=min(ridges,key=lambda p:(math.dist(p,points[0]),p))
        center=route(summit,points[0],land)
        for a,b in zip(points,points[1:]):
            allowed=land if b in land else owned
            center+=route(a,b,allowed)[1:]
        if len(center)!=len(set(center)):raise ValueError('River centerline loops')
        # A terminal sea run may not return to land.
        sea_started=False
        for p in center:
            if p not in layout.land:sea_started=True
            elif sea_started:raise ValueError('River returns inland after reaching the sea')
        if not sea_started:raise ValueError('River never reaches the sea')
        water=expand(center,1,owned);rivers.update(water)
        river_records.append(dict(label=source['label'],mountainSource=list(summit),
                                  cells=[list(p) for p in center],mouth=list(center[-1])))
    speed=json.loads((ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text())['walkingCellsPerSecond']
    roads=[];road_cells=set()
    # A real valley is opened only where a river already carved the mountain.
    allowed=land-(mountains-expand(rivers,2,owned))
    for source in layout.data['roads']:
        a,b=source['fromKey'],source['toKey']
        center=worn_route(towns[a],towns[b],allowed,rivers,road_cells)
        widened=expand(center,1,allowed);road_cells.update(widened)
        roads.append(dict(fromKey=a,toKey=b,cells=[list(p) for p in center],
                          length=sum(math.dist(a,b) for a,b in zip(center,center[1:])),seconds=sum(math.dist(a,b) for a,b in zip(center,center[1:]))/speed))
    bridges=road_cells&rivers&land
    water=(owned-layout.land)|rivers
    blocked=(water|mountains)-road_cells
    for p in towns.values():
        if p in blocked:raise ValueError('Blocked town')
    return dict(owned=owned,land=land,mountains=mountains,rivers=rivers,water=water,
                road=road_cells,bridges=bridges,blocked=blocked,towns=towns,
                riverRecords=river_records,roadRecords=roads)


def tile_at(t,p):
    x,y=p
    def wet(a,b):return (a,b) in t['water'] or (a,b) not in t['land']
    def road(a,b):return (a,b) in t['road']
    def mountain(a,b):return (a,b) in t['mountains'] and (a,b) not in t['rivers']
    if p in t['bridges']:
        return 'b2_bridge_center' # independent complete 16px deck module over water
    if p in t['water']:return autotile(wet,x,y,'water',0,6).replace(':','_')
    if p in t['road']:return autotile(road,x,y,'floor',0,7).replace(':','_')
    if mountain(x,y):
        # Plateau grass with original native cliff faces on its southern edge.
        # Full cliff modules come from the purpose-made TilesetRelief, not rocks.
        if not mountain(x,y+1):return 'b2_cliff_edge'
        return 'b2_cliff_center'
    return 'floor_0_12'


def split_maps(layout,t):
    result=[]
    for key,m in layout.maps.items():
        x0,y0,x1,y1=m['bounds'];w=x1-x0;h=y1-y0;n=w*h
        data=dict(id='layoutD_'+key.replace('/','_'),name=m['label'],w=w,h=h,hours=0,
                  terrainX=x0,terrainY=y0,layoutKey=key,owned=[0]*n,ground=[0]*n,
                  deco=[0]*n,obj=[0]*n,high=[0]*n,road=[0]*n,blocked=[1]*n,
                  terrainTags=['outside']*n,palette=[''],objects=[],previewOnly=True,walkSegments=[],assetPlacementVersion=1,assetPlacements=[])
        palette={};index=1
        for x,y in sorted(layout.map_cells[key],key=lambda p:(p[1],p[0])):
            i=(y-y0)*w+x-x0;p=(x,y);tile=tile_at(t,p)
            if tile not in palette:palette[tile]=index;data['palette'].append(tile);index+=1
            data['owned'][i]=1;data['ground'][i]=palette[tile]
            data['blocked'][i]=int(p in t['blocked']);data['road'][i]=1 if p in t['road'] else 0
            data['terrainTags'][i]='bridge' if p in t['bridges'] else 'water' if p in t['water'] else 'cliff' if p in t['mountains'] else 'land'
            data['assetPlacements'].append(dict(id=f'cell_{i}',assetId='b2_terrain',x=x-x0,y=y-y0,purpose='layoutd_terrain',piece=tile))
        # Review-only spawn. No markets/NPCs/production exits are generated here.
        choices=[p for p in layout.map_cells[key] if p in t['road'] and p not in t['blocked']]
        town=t['towns'].get(key)
        spawn=town if town else min(choices) if choices else min(p for p in layout.map_cells[key] if p not in t['blocked'])
        data['objects'].append(dict(kind='spawn',id='S0',x=spawn[0]-x0,y=spawn[1]-y0,w=1,h=1))
        segments=set()
        for road in t['roadRecords']:
            for a,b in zip(road['cells'],road['cells'][1:]):
                if tuple(a) in layout.map_cells[key] and tuple(b) in layout.map_cells[key]:
                    segments.add((a[0]-x0,a[1]-y0,b[0]-x0,b[1]-y0))
        data['walkSegments']=[dict(x=a,y=b,nx=c,ny=d) for a,b,c,d in sorted(segments)]
        result.append(data)
    return result


def validate(layout,t,maps):
    seen=set();painted=0
    for m in maps:
        cells=layout.map_cells[m['layoutKey']]
        for i,own in enumerate(m['owned']):
            p=(m['terrainX']+i%m['w'],m['terrainY']+i//m['w'])
            if bool(own)!=(p in cells):raise ValueError('Ownership changed')
            if own:
                if p in seen:raise ValueError('Duplicate owned cell')
                seen.add(p);painted+=1
                if m['palette'][m['ground'][i]]!=tile_at(t,p):raise ValueError('Global tile seam mismatch')
            elif m['ground'][i] or not m['blocked'][i]:raise ValueError('Padded void is painted or walkable')
    if seen!=set(layout.owners):raise ValueError('Assigned cell missing')
    for road in t['roadRecords']:
        for a,b in zip(road['cells'],road['cells'][1:]):
            if max(abs(x-y) for x,y in zip(a,b))!=1:raise ValueError('Unwalkable route step')
        if any(tuple(p) in t['blocked'] for p in road['cells']):raise ValueError('Road blocked')
    return dict(maps=len(maps),assignedCells=painted,sourceLandCells=len(t['land']),
                nearSeaCells=len(t['owned']-t['land']),rivers=len(t['riverRecords']),roads=len(t['roadRecords']),
                bridgeCells=len(t['bridges']),mountainCells=len(t['mountains']),
                inactiveExternalLinks=len(layout.data['externalLinks']),productionChanged=False)


def export(out=OUT):
    layout=WorldLayout.read();t=terrain(layout);maps=split_maps(layout,t);report=validate(layout,t,maps)
    out=Path(out).resolve()
    if not out.is_relative_to((ROOT/'ArtReview').resolve()):raise ValueError('B2 output must stay in ArtReview until approval/B5')
    out.mkdir(parents=True,exist_ok=True);(out/'Maps').mkdir(exist_ok=True)
    for m in maps:(out/'Maps'/(m['id']+'.json')).write_text(json.dumps(m,ensure_ascii=False,separators=(',',':'))+'\n')
    (out/'routes.json').write_text(json.dumps(t['roadRecords'],ensure_ascii=False,indent=2)+'\n')
    from export_routes import build, serialized
    actual=walking_routes(t,json.loads((ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text())['walkingCellsPerSecond'])
    (out/'walking-distances.json').write_text(json.dumps(actual,ensure_ascii=False,indent=2)+'\n')
    (out/'layoutD-routes.json').write_text(serialized(build(actual_roads=t['roadRecords'],actual_distances=actual)))
    (out/'rivers.json').write_text(json.dumps(t['riverRecords'],ensure_ascii=False,indent=2)+'\n')
    (out/'geometry-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    return layout,t,maps,report


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--out',type=Path,default=OUT);args=parser.parse_args()
    print(json.dumps(export(args.out)[3],ensure_ascii=False))
