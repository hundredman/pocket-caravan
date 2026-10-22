"""One authoritative terrain field, cut into playable scene sectors and the same atlas.
Templates contribute settlements, not independent shorelines or rectangular forest borders.
"""
import collections,heapq,json,math
from pathlib import Path
from tiles import Map,STAMPS,K

CONFIG=Path(__file__).resolve().parents[2]/'Assets/PocketCaravan/World/Terrain/continuous.json'
DIRECTIONS={'E':(1,0),'W':(-1,0),'N':(0,-1),'S':(0,1)}
OPPOSITE={'E':'W','W':'E','N':'S','S':'N'}

def inside_polygon(x,y,points):
    result=False
    for i,(ax,ay) in enumerate(points):
        bx,by=points[(i+1)%len(points)]
        if (ay>y)!=(by>y) and x<(bx-ax)*(y-ay)/(by-ay)+ax:result=not result
    return result

def skin_key(key,x,y):
    if not key or key.split(':')[0] not in ('floor','detail','water','nature','field'):return key
    if '@' in key and not ('@skin_' in key or '@blend_' in key):return key
    key=key.split('@')[0]
    dryness=max(0,min(4,int(round((-x-120)/25))))
    return key+('@skin_steppe' if dryness==4 else '@blend_steppe_'+str(dryness*25)) if dryness else key

def build(templates):
    config=json.loads(CONFIG.read_text());w,h=config['chunkSize'];origins={mid:(xy[0]*w,xy[1]*h) for mid,xy in config['maps'].items()}
    if set(origins)!={m.id for m in templates}:raise ValueError('Continuous terrain/registry map set mismatch')
    maps={};anchors={};protected=set();ponds=set();original_decks={}
    def strip(key):return key.split('@')[0] if key and ('@skin_' in key or '@blend_' in key) else key
    for t in templates:
        m=Map(t.id,t.name,w,h,seed=sum(map(ord,t.id)));m.legacy_height=config.get("legacyHeights",{}).get(t.id,h);m.world_origin=origins[t.id];m.skip_smooth=True;m.hours=t.hours
        if hasattr(t,'atlas_label'):m.atlas_label=t.atlas_label
        if t.id=='Gaegyeong':m.road_origin=lambda x,y:('floor',0,0) if x<64 and y<42 else ('floor',0,7)
        ox,oy=m.world_origin
        # Clear complete border trees; never retain only canopy/trunk fragments.
        removed=set()
        for name,x,y in t.stamps:
            _,_,_,sw,sh,*_=STAMPS[name]
            if STAMPS[name][0]=='nature' and (x<=2 or y<=2 or x+sw>=t.w-2 or y+sh>=t.h-2):
                removed.update((x+dx,y+dy) for dx in range(sw) for dy in range(sh))
            else:m.stamps.append((name,x,y))
        for y in range(t.h):
            for x in range(t.w):
                if (x,y) in removed:continue
                for layer in ('obj','high','deco'):getattr(m,layer)[y][x]=strip(getattr(t,layer)[y][x])
                # Preserve deliberate city floors/palace foundation, not rectangular countryside plateaus.
                if t.id=='Gaegyeong':m.ground[y][x]=strip(t.ground[y][x])
                m.road[y][x]=t.road[y][x]
                m.blocked[y][x]=t.blocked[y][x] and not t.water[y][x]
                if t.ground[y][x] and t.ground[y][x].startswith('field:') and t.id!='Gaegyeong' and not m.obj[y][x]:m.blocked[y][x]=False
                m.reserved[y][x]=bool(m.obj[y][x] or m.high[y][x] or m.road[y][x])
                if (m.obj[y][x] or m.high[y][x]) and not any(sx<=x<sx+STAMPS[n][3] and sy<=y<sy+STAMPS[n][4] for n,sx,sy in m.stamps if STAMPS[n][0]=='nature'):protected.add((ox+x,oy+y))
        m.objects=[dict(o) for o in t.objects if o['kind'] not in ('exit','spawn','anim') or o['kind']=='spawn' and o['id']=='S0']
        for o in t.objects:
            if o['kind']=='exit':anchors[(m.id,o['edge'])]=(o['x'],o['y'])
        for o in m.objects:
            if o['kind'] not in ('ambience','bandit'):
                protected.add((ox+o['x'],oy+o['y']));m.reserve(o['x'],o['y'],o['x'],o['y'])
            if o['kind']=='worker':
                for (ax,ay),(bx,by) in zip(o['path'],o['path'][1:]):
                    n=max(abs(bx-ax),abs(by-ay))
                    for i in range(n+1):
                        xx,yy=round(ax+(bx-ax)*i/max(1,n)),round(ay+(by-ay)*i/max(1,n))
                        protected.add((ox+xx,oy+yy));m.reserve(xx,yy,xx,yy)
        for p,key in t.decks.items():original_decks[(ox+p[0],oy+p[1])]=key
        # Closed ponds survive; old edge-to-edge rivers are replaced by world-space channels.
        wet={(x,y) for y in range(t.h) for x in range(t.w) if t.water[y][x]}
        while wet:
            start=wet.pop();q=[start];component={start}
            for x,y in q:
                for a,b in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                    if (a,b) in wet:wet.remove((a,b));component.add((a,b));q.append((a,b))
            if all(0<x<t.w-1 and 0<y<t.h-1 for x,y in component):ponds.update((ox+x,oy+y) for x,y in component)
        maps[m.id]=m
    river=set()
    for feature in config['rivers']:
        points=[(origins[mid][0]+x,origins[mid][1]+y) for mid,x,y in feature['points']]
        radius=feature['width']/2
        for (ax,ay),(bx,by) in zip(points,points[1:]):
            n=max(abs(bx-ax),abs(by-ay))*2
            for i in range(n+1):
                cx=ax+(bx-ax)*i/max(1,n);cy=ay+(by-ay)*i/max(1,n)
                for y in range(math.floor(cy-radius),math.ceil(cy+radius)+1):
                    for x in range(math.floor(cx-radius),math.ceil(cx+radius)+1):
                        if (x-cx)**2+(y-cy)**2<=radius**2:river.add((x,y))
    def land(x,y):return x<0 or y<0 or x<80 and y<64 or inside_polygon(x+.5+2.0*math.sin(y/10)+1.2*math.sin(y/4.5),y+.5,config['peninsula'])
    def raw_water(x,y):return not land(x,y) or (x,y) in river or (x,y) in ponds
    def water(x,y):return raw_water(x,y) and (x,y) not in protected
    for m in maps.values():
        ox,oy=m.world_origin
        for y in range(h):
            for x in range(w):
                m.water[y][x]=water(ox+x,oy+y)
                if m.water[y][x]:m.blocked[y][x]=True;m.ground[y][x]=None;m.deco[y][x]=None
                if not land(ox+x,oy+y):m.road[y][x]=0
        # Old stamps crossing a newly traced river are removed as a whole.
        keep=[]
        for name,x,y in m.stamps:
            _,_,_,sw,sh,*_=STAMPS[name]
            if STAMPS[name][0]=='nature' and any(raw_water(ox+x+dx,oy+y+dy) for dx in range(sw) for dy in range(sh)):
                for dy in range(sh):
                    for dx in range(sw):
                        xx,yy=x+dx,y+dy;m.obj[yy][xx]=None;m.high[yy][xx]=None;m.blocked[yy][xx]=m.water[yy][xx];m.reserved[yy][xx]=bool(m.road[yy][xx])
            else:keep.append((name,x,y))
        m.stamps=keep
    # Route extensions use structure-aware pathfinding; they never cut through buildings/trees.
    def route(m,start,end,grade=2,width=1):
        q=[(0,start)];dist={start:0};prev={};found=False
        while q:
            c,p=heapq.heappop(q)
            if c!=dist[p]:continue
            if p==end:found=True;break
            x,y=p
            for a,b in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if not m.inside(a,b) or m.blocked[b][a] and not m.water[b][a]:continue
                # Ocean is never bridged to force a road. River crossings are allowed.
                ox,oy=m.world_origin
                if not land(ox+a,oy+b):continue
                wx,wy=ox+a,oy+b
                variation=.22*math.sin(wx/11)*math.sin(wy/13)+.12*math.sin((wx+wy)/7)
                nc=c+(4 if m.water[b][a] else .6 if m.road[b][a] else 1+variation)
                if nc<dist.get((a,b),float('inf')):dist[(a,b)]=nc;prev[(a,b)]=p;heapq.heappush(q,(nc,(a,b)))
        if not found:raise ValueError(('Route blocked',m.id,start,end))
        path=[end]
        while path[-1]!=start:path.append(prev[path[-1]])
        for x,y in path:
            for dx in range(width):
                for dy in range(width):
                    a,b=x+dx,y+dy
                    if m.inside(a,b) and (not m.blocked[b][a] or m.water[b][a]) and land(m.world_origin[0]+a,m.world_origin[1]+b):
                        m.road[b][a]=max(m.road[b][a],grade);m.reserved[b][a]=True
        return path
    def nearest_road(m,p):
        candidates=[(x,y) for y in range(h) for x in range(w) if m.road[y][x] and (not m.blocked[y][x] or m.water[y][x])]
        if not candidates:candidates=[(x,y) for y in range(3,h-3) for x in range(3,w-3) if not m.blocked[y][x]]
        return min(candidates,key=lambda c:abs(c[0]-p[0])+abs(c[1]-p[1]))
    logical={};port_starts={}
    for a,b in config['roadConnections']:
        ax,ay=config['maps'][a];bx,by=config['maps'][b];edge=next((d for d,v in DIRECTIONS.items() if v==(bx-ax,by-ay)),None)
        if edge is None:raise ValueError(('Nonadjacent trade road',a,b))
        for mid,d in ((a,edge),(b,OPPOSITE[edge])):
            m=maps[mid];port=config.get('roadPortStarts',{}).get('|'.join(sorted((a,b))),31 if d in 'EW' else 39)
            port_starts[(mid,d)]=port
            point=(w-1,port) if d=='E' else (0,port) if d=='W' else (port,0) if d=='N' else (port,h-1)
            start=anchors.get((mid,d),nearest_road(m,point))
            if m.blocked[start[1]][start[0]] and not m.water[start[1]][start[0]]:start=nearest_road(m,point)
            route(m,start,point,grade=2,width=3)
            x0,y0,x1,y1=(w-2,port,w-1,port+2) if d=='E' else (0,port,1,port+2) if d=='W' else (port,0,port+2,1) if d=='N' else (port,h-2,port+2,h-1)
            m.road_rect(x0,y0,x1,y1,grade=2);logical[(mid,d)]=True
    for m in maps.values():
        # Coast sectors get a small contour path, not an invented town or market.
        if not any(g for row in m.road for g in row):
            good=[(x,y) for y in range(4,h-4) for x in range(4,w-4) if not m.blocked[y][x]]
            start=min(good,key=lambda p:abs(p[0]-w/2)+abs(p[1]-h/2));end=min(good,key=lambda p:abs(p[0]-w+8)+abs(p[1]-8));route(m,start,end,grade=1)
    roads={(m.world_origin[0]+x,m.world_origin[1]+y):m.road[y][x] for m in maps.values() for y in range(h) for x in range(w) if m.road[y][x]}
    decks=dict(original_decks)
    # Bridge components are drawn once in world coordinates, including across a sector boundary.
    crossings={p for p in roads if water(*p)}
    while crossings:
        start=crossings.pop();component={start};q=[start]
        for x,y in q:
            for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if p in crossings:crossings.remove(p);component.add(p);q.append(p)
        x0=min(p[0] for p in component);x1=max(p[0] for p in component);y0=min(p[1] for p in component);y1=max(p[1] for p in component)
        horizontal=x1-x0>=y1-y0
        for x,y in component:
            sx=0 if x==x0 else 2 if x==x1 else 1;sy=12 if y==y0 else 14 if y==y1 else 13
            if horizontal:sx=0 if y==y1 else 2 if y==y0 else 1;sy=12 if x==x0 else 14 if x==x1 else 13
            decks[(x,y)]=K('water',sx,sy,'woodturn' if horizontal else 'wood')
    by_grid={tuple(xy):mid for mid,xy in config['maps'].items()}
    for m in maps.values():
        ox,oy=m.world_origin;gx,gy=config['maps'][m.id]
        for y in range(h):
            for x in range(w):
                if (ox+x,oy+y) in decks:
                    m.decks[(x,y)]=decks[(ox+x,oy+y)];m.ground[y][x]=m.decks[(x,y)];m.water[y][x]=False;m.blocked[y][x]=False
                if m.water[y][x]:m.reserved[y][x]=True
        # Outside the current prototype: irregular visible rocks, not invisible grass walls.
        for edge,(dx,dy) in DIRECTIONS.items():
            if (gx+dx,gy+dy) in by_grid:continue
            for t in range(0,(h if edge in 'EW' else w)-2,2):
                x,y=(0,t) if edge=='W' else (w-2,t) if edge=='E' else (t,0) if edge=='N' else (t,h-2)
                m.stamp('module_rock',x,y)
        # Natural vegetation uses global hashed positions; no forest ring per scene.
        for y in range(h-1):
            for x in range(w-1):
                wx,wy=ox+x,oy+y
                n=(wx*73856093 ^ wy*19349663)&0xffffffff
                n^=n>>16;n=(n*2246822519)&0xffffffff;n^=n>>13
                forest=math.sin(wx/17)+.7*math.cos(wy/14)+.4*math.sin((wx+wy)/31)
                density=(20 if forest>.5 else 43 if forest>-.5 else 100) if wx>=-120 else 110
                if n%density==0 and not m.reserved[y][x]:
                    if wx<-190:m.stamp('module_rock',x,y)
                    else:m.stamp('pinebig' if n%3 else 'roundbig',x,y)
        # Sample both sides of scene borders from this same world field.
        m.outside_water=lambda x,y,ox=ox,oy=oy:water(ox+x,oy+y) or (ox+x,oy+y) in decks
        m.outside_road=lambda x,y,ox=ox,oy=oy:(ox+x,oy+y) in roads or (ox+x,oy+y) in decks
        m.finish()
        m.tint_key=lambda key,x,y,ox=ox,oy=oy:skin_key(key,ox+x,oy+y)
        for layer in (m.ground,m.deco,m.obj,m.high):
            for y,row in enumerate(layer):
                for x,key in enumerate(row):row[x]=m.tint_key(key,x,y)
    # Ignore enclosed decorative ground pockets that cannot be reached from the starting world.
    # Rivers and forests may separate local sectors, so connectivity is evaluated across the mosaic.
    owners={(m.world_origin[0]+x,m.world_origin[1]+y):(m,x,y) for m in maps.values() for y in range(h) for x in range(w) if not m.blocked[y][x]}
    start=next(o for o in maps['Naru'].objects if o['kind']=='spawn' and o['id']=='S0')
    ox,oy=maps['Naru'].world_origin;seen={(ox+start['x'],oy+start['y'])};queue=collections.deque(seen)
    while queue:
        x,y=queue.popleft()
        for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if q in owners and q not in seen:seen.add(q);queue.append(q)
    for m in maps.values():
        ox,oy=m.world_origin;m.world_reachable={(x,y) for y in range(h) for x in range(w) if (ox+x,oy+y) in seen}
    # Every walkable seam run has a reciprocal exit, not only the road mouth.
    for (gx,gy),aid in by_grid.items():
        for edge,(dx,dy) in [('E',(1,0)),('S',(0,1))]:
            bid=by_grid.get((gx+dx,gy+dy))
            if bid is None:continue
            a,b=maps[aid],maps[bid];other=OPPOSITE[edge];length=h if edge=='E' else w
            def cells(t):return ((w-1,t),(w-2,t),(0,t),(1,t)) if edge=='E' else ((t,h-1),(t,h-2),(t,0),(t,1))
            valid=[]
            for t in range(length):
                aa,ai,bb,bi=cells(t)
                if all((x,y) in m.world_reachable for m,ps in ((a,(aa,ai)),(b,(bb,bi))) for x,y in ps):valid.append(t)
            canonical=port_starts.get((aid,edge),31 if edge=='E' else 39)
            spans=[]
            if logical.get((aid,edge)):
                if not all(t in valid for t in range(canonical,canonical+3)):raise ValueError(('Road seam blocked',aid,bid))
                spans.append((canonical,3,edge,other));valid=[t for t in valid if not canonical<=t<canonical+3]
            runs=[]
            for t in valid:
                if runs and runs[-1][-1]+1==t:runs[-1].append(t)
                else:runs.append([t])
            for i,run in enumerate(runs):spans.append((run[0],len(run),f'seam_{edge}_{i}',f'seam_{other}_{i}'))
            for start,n,sa,sb in spans:
                a.link(edge,start,n,bid,sb,sa);b.link(other,start,n,aid,sa,sb)
    for m in maps.values():
        if not any(o['kind']=='spawn' and o['id']=='S0' for o in m.objects):
            x,y=nearest_road(m,(w//2,h//2));m.add('spawn',x,y,id='S0')
        if not hasattr(m,'atlas_label') and not any(o['kind']=='market' for o in m.objects):
            x,y=nearest_road(m,(w//2,h//2));m.atlas_label=dict(x=x,y=y)
    return [maps[t.id] for t in templates]
