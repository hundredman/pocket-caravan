"""Compile the existing world into the team's exact cell budgets.
Columns/rows remain shared, so every neighbouring edge and atlas coordinate agrees.
Whole structures are relocated (never resized into fragments); trade identities survive.
"""
import collections, copy, heapq, json
from pathlib import Path
from tiles import Map, STAMPS
ROOT=Path(__file__).resolve().parents[2]

def build(source):
    profile=json.loads((ROOT/'Assets/PocketCaravan/World/Terrain/continuous.json').read_text())
    budget=json.loads((ROOT/'Assets/PocketCaravan/World/Terrain/area_budget.json').read_text())
    widths={int(k):v for k,v in budget['columnWidths'].items()}
    heights={int(k):v for k,v in budget['rowHeights'].items()}
    def offset(i,sizes):return sum(sizes[j] for j in range(i)) if i>=0 else -sum(sizes[j] for j in range(i,0))
    result=[]
    for old in source:
        gx,gy=profile['maps'][old.id];w,h=widths[gx],heights[gy]
        m=Map(old.id,old.name,w,h,seed=sum(map(ord,old.id)))
        m.world_origin=(offset(gx,widths),offset(gy,heights));m.legacy_height=old.legacy_height;m.hours=old.hours;m.skip_smooth=True
        def point(x,y):return (min(w-1,max(0,int(x*w/old.w))),min(h-1,max(0,int(y*h/old.h))))
        def inverse(x,y):return min(old.w-1,int((x+.5)*old.w/w)),min(old.h-1,int((y+.5)*old.h/h))
        for y in range(h):
            for x in range(w):
                sx,sy=inverse(x,y)
                m.water[y][x]=old.water[sy][sx];m.blocked[y][x]=m.water[y][x]
        # Rasterize every old road segment, preserving its grade and continuous connectivity.
        for y in range(old.h):
            for x in range(old.w):
                grade=old.road[y][x]
                if not grade:continue
                a,b=point(x,y)
                m.road_rect(a,b,*point(x+1,y+1),grade=grade)
        # Canonical mouths retain three walkable cells even after compacting.
        for o in old.objects:
            if o['kind']=='exit' and o.get('spawn') in ('N','S','E','W'):
                x,y=point(o['x'],o['y']);edge=o['edge']
                t=min((h if edge in 'EW' else w)-4,max(1,y if edge in 'EW' else x))
                if edge in 'EW':m.road_rect(0 if edge=='W' else w-2,t,1 if edge=='W' else w-1,t+2,grade=2)
                else:m.road_rect(t,0 if edge=='N' else h-2,t+2,1 if edge=='N' else h-1,grade=2)
        # Bridges remain real tiles, and never retain an invisible water blocker.
        for y in range(h):
            for x in range(w):
                if m.road[y][x] and m.water[y][x]:
                    from tiles import K
                    m.decks[x,y]=K('water',1,13,'wood');m.water[y][x]=False;m.blocked[y][x]=False
        if m.id=='Gaegyeong':capital(m)
        # Preserve construction as indivisible stamps; prioritize buildings over vegetation.
        stamps=sorted(old.stamps,key=lambda s:STAMPS[s[0]][0]=='nature')
        for name,sx,sy in stamps:
            if m.id=='Gaegyeong':continue
            if name=='module_rock' and (sx<=2 or sy<=2 or sx+2>=old.w-2 or sy+2>=old.h-2):continue
            _,_,_,sw,sh,*_=STAMPS[name];x,y=point(sx,sy)
            nature=STAMPS[name][0]=='nature'
            candidates=[(x+dx,y+dy) for dy in range(-5,6) for dx in range(-5,6)] if not nature else [(x,y)]
            candidates.sort(key=lambda p:abs(p[0]-x)+abs(p[1]-y))
            for a,b in candidates:
                if a<2 or b<2 or a+sw>w-2 or b+sh>h-2:continue
                if m.stamp(name,a,b):break
        def nearest(p,used=None):
            used=used or set()
            candidates=[(x,y) for y in range(2,h-2) for x in range(2,w-2) if not m.blocked[y][x] and (x,y) not in used and m.road[y][x]]
            return min(candidates,key=lambda q:abs(q[0]-p[0])+abs(q[1]-p[1]))
        used=set();villager_index=0
        for o in old.objects:
            kind=o['kind']
            if kind in ('exit','anim') or kind=='spawn' and o['id']!='S0':continue
            n=copy.deepcopy(o);p=nearest(point(o['x'],o['y']),used)
            if m.id=='Gaegyeong':
                if kind=='market':p=(10,18)
                elif kind=='npc':p=(28,27)
                elif kind=='board':p=(15,21)
                elif kind=='villager':
                    p=[(6,12),(32,12),(8,22),(16,24),(30,22),(32,30),(13,31),(25,9)][villager_index%8]
                    villager_index+=1
            n['x'],n['y']=p
            if kind in ('market','npc','talker','sign','board','villager'):used.add(p)
            if 'path' in n:
                points=[nearest(point(x,y)) for x,y in o['path']]
                full=[]
                for a,b in zip(points,points[1:]):
                    path=road_path(m,a,b)
                    full.extend(path[:-1])
                full.append(points[-1])
                if len(full)<2 or len(set(full))<2:
                    x,y=full[0]
                    q=next(p for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)) if m.inside(*p) and m.road[p[1]][p[0]] and not m.blocked[p[1]][p[0]])
                    full.append(q)
                # Keep bends, not every cell: workers pause at work stations/waypoints.
                simplified=[full[0]]
                for i in range(1,len(full)-1):
                    a,b,c=full[i-1:i+2]
                    if (b[0]-a[0],b[1]-a[1])!=(c[0]-b[0],c[1]-b[1]):simplified.append(b)
                simplified.append(full[-1])
                # Return around the same bends instead of cutting through scenery.
                n['path']=simplified+simplified[-2:0:-1]
                n['x'],n['y']=full[0]
            m.objects.append(n)
        if not any(o['kind']=='spawn' and o['id']=='S0' for o in m.objects):
            x,y=nearest((w//2,h//2));m.add('spawn',x,y,id='S0')
        if hasattr(old,'atlas_label'):
            x,y=nearest(point(old.atlas_label['x'],old.atlas_label['y']));m.atlas_label=dict(x=x,y=y)
        from settlement_details import add as add_details
        add_details(m)
        result.append(m)
    by_id={m.id:m for m in result};by_grid={tuple(xy):by_id[mid] for mid,xy in profile['maps'].items()}
    # Rejoin canonical roads displaced by rounding, before computing global reachability.
    for aid,bid in profile['roadConnections']:
        a,b=by_id[aid],by_id[bid];ax,ay=profile['maps'][aid];bx,by=profile['maps'][bid]
        edge='E' if bx>ax else 'W' if bx<ax else 'S' if by>ay else 'N'
        original=next(o for o in next(m for m in source if m.id==aid).objects if o['kind']=='exit' and o['target']==bid and o.get('spawn') in ('N','S','E','W'))
        old=next(m for m in source if m.id==aid)
        t=int(original['y']*a.h/old.h) if edge in 'EW' else int(original['x']*a.w/old.w)
        t=min((a.h if edge in 'EW' else a.w)-4,max(1,t))
        for m,d in ((a,edge),(b,{'E':'W','W':'E','N':'S','S':'N'}[edge])):
            for j in range(t,t+3):
                p=(m.w-1,j) if d=='E' else (0,j) if d=='W' else (j,0) if d=='N' else (j,m.h-1)
                near=min(((x,y) for y in range(2,m.h-2) for x in range(2,m.w-2) if m.road[y][x] and not m.blocked[y][x]),key=lambda q:abs(p[0]-q[0])+abs(p[1]-q[1]))
                for x,y in road_path(m,p,near,carve=True):
                    m.road[y][x]=max(2,m.road[y][x]);m.blocked[y][x]=False
                    if m.water[y][x]:
                        from tiles import K
                        m.water[y][x]=False;m.decks[x,y]=K('water',1,13,'wood')
    wet={(m.world_origin[0]+x,m.world_origin[1]+y) for m in result for y in range(m.h) for x in range(m.w) if m.water[y][x] or (x,y) in m.decks}
    roads={(m.world_origin[0]+x,m.world_origin[1]+y) for m in result for y in range(m.h) for x in range(m.w) if m.road[y][x]}
    for m in result:
        ox,oy=m.world_origin
        m.outside_water=lambda x,y,ox=ox,oy=oy:(ox+x,oy+y) in wet
        m.outside_road=lambda x,y,ox=ox,oy=oy:(ox+x,oy+y) in roads
        for (x,y),tile in m.decks.items():m.ground[y][x]=tile
        m.finish()
        # The steppe remains a separate palette as the eastern central sector opens out.
        if m.id in ('transit_central_approach','transit_crossroads'):m.apply_visual_skin('steppe')
    owners={(m.world_origin[0]+x,m.world_origin[1]+y) for m in result for y in range(m.h) for x in range(m.w) if not m.blocked[y][x]}
    a=by_id['Naru'];s=next(o for o in a.objects if o['kind']=='spawn');seen={(a.world_origin[0]+s['x'],a.world_origin[1]+s['y'])};q=collections.deque(seen)
    while q:
        x,y=q.popleft()
        for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if p in owners and p not in seen:seen.add(p);q.append(p)
    for m in result:
        ox,oy=m.world_origin;m.world_reachable={(x,y) for y in range(m.h) for x in range(m.w) if (ox+x,oy+y) in seen}
    for (gx,gy),a in by_grid.items():
        for edge,delta,other in [('E',(1,0),'W'),('S',(0,1),'N')]:
            b=by_grid.get((gx+delta[0],gy+delta[1]))
            if b is None:continue
            valid=[]
            for t in range(a.h if edge=='E' else a.w):
                ac=[(a.w-1,t),(a.w-2,t)] if edge=='E' else [(t,a.h-1),(t,a.h-2)]
                bc=[(0,t),(1,t)] if edge=='E' else [(t,0),(t,1)]
                if all(p in m.world_reachable for m,points in ((a,ac),(b,bc)) for p in points):valid.append(t)
            runs=[]
            for t in valid:
                if runs and runs[-1][-1]+1==t:runs[-1].append(t)
                else:runs.append([t])
            for i,run in enumerate(runs):
                sa=edge if i==0 else f'seam_{edge}_{i}';sb=other if i==0 else f'seam_{other}_{i}'
                a.link(edge,run[0],len(run),b.id,sb,sa);b.link(other,run[0],len(run),a.id,sa,sb)
    assert sum(m.w*m.h for m in result if not m.id.startswith('transit_'))==budget['goryeoTiles']
    assert sum(m.w*m.h for m in result if m.id.startswith('transit_'))==budget['centralEastTiles']
    return result

def road_path(m,start,end,carve=False):
    queue=[(0,start)];dist={start:0};prev={}
    while queue:
        cost,p=heapq.heappop(queue)
        if cost!=dist[p]:continue
        if p==end:
            result=[p]
            while p!=start:p=prev[p];result.append(p)
            return result[::-1]
        x,y=p
        for v in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            a,b=v
            if not m.inside(a,b):continue
            if m.blocked[b][a] and not (carve and m.water[b][a]):continue
            if not carve and not m.road[b][a]:continue
            n=cost+(1 if m.road[b][a] else 5 if not m.water[b][a] else 20)
            if n<dist.get(v,1e20):dist[v]=n;prev[v]=p;heapq.heappush(queue,(n,v))
    raise ValueError(('No path',m.id,start,end))


def capital(m):
    """Hand-authored capital composition: palace, lotus court, market and residential lanes."""
    from tiles import K
    import goryeo_art
    # Keep perimeter mouths, rebuild the interior at its intended final cell scale.
    for y in range(2,m.h-2):
        for x in range(2,m.w-2):
            m.road[y][x]=0;m.reserved[y][x]=False;m.water[y][x]=False;m.blocked[y][x]=False
            m.decks.pop((x,y),None)
    m.road_path([(4,0),(4,12),(20,12)],width=2,grade=3)
    m.road_path([(20,8),(20,39)],width=3,grade=3)
    m.road_path([(0,19),(39,19)],width=3,grade=3)
    m.road_path([(5,28),(34,28)],width=2,grade=2)
    m.road_path([(5,11),(5,28)],width=2,grade=2)
    m.road_path([(34,11),(34,28)],width=2,grade=2)
    m.road_path([(20,8),(20,11)],width=3,grade=3)
    m.road_blob(21,21,5,4,grade=3,jitter=.04)
    for name,x,y in [('goryeo_royal',15,1),('goryeo_manor',7,5),('goryeo_manor',28,5),
                     ('goryeo_stall',8,14),('goryeo_stall',27,14),
                     ('goryeo_manor',9,23),('goryeo_manor',27,23),
                     ('goryeo_cottage',7,31),('goryeo_manor',28,31)]:
        if not m.stamp(name,x,y):raise ValueError(('Capital landmark blocked',name,x,y))
    m.water_blob(10,10,2.5,1.5,jitter=.02);m.water_blob(30,10,2.5,1.5,jitter=.02)
    for x,y in [(13,33),(25,33),(1,7),(35,7),(1,24),(36,24)]:m.stamp('pine',x,y)
    for x,y in [(13,8),(25,8),(15,26),(24,26)]:m.stamp('cherry',x,y)
    m.stamp('cart',14,31)
    # A broad pale stone courtyard distinguishes the capital from village dirt paths.
    m.road_origin=lambda x,y:('floor',0,0)
    for y in range(8,30):
        for x in range(5,35):
            if not m.water[y][x] and not m.road[y][x]:m.ground[y][x]=K('floor',0,5)
    # Edge roads retained from the geographic source connect to the redesigned streets.
    for y in range(m.h):
        for x in range(m.w):
            if m.road[y][x]:m.water[y][x]=False;m.blocked[y][x]=False
