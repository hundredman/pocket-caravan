"""Five continuous settlement catchments; source chunks are authoring templates only."""
import copy, math, collections
from tiles import Map, STAMPS, K, DIRT_ORIGIN
from compact import road_path

# map -> global top-left, full dimensions, settlement template offset
REGIONS={
 'goryeo_songak':(40,0,60,60,12,12),
 'Gaegyeong':(100,0,70,60,16,10),
 'Naru':(40,60,80,60,12,10),
 'goryeo_jangdan':(120,60,80,55,25,15),
 'Dakmok':(170,0,50,60,5,17),
}
ALIASES={'Route01':'Naru','goryeo_ridge':'goryeo_songak','goryeo_descent':'Gaegyeong',
 'goryeo_fieldroad':'goryeo_jangdan','goryeo_westcoast':'Naru','goryeo_tidalcoast':'Naru',
 'goryeo_rivervalley':'Dakmok','goryeo_southcoast':'goryeo_jangdan'}

def wet(x,y):
    # One compact eastern peninsula. The western neck is the only continental land bridge.
    neck=12<=y<=34 and x<58
    west=49+3*math.sin(y*.075)
    north=7+3*math.sin(x*.052)
    east=216-12*(abs(y-55)/65)**1.35+2*math.sin(y*.095)
    south=111+3*math.sin(x*.057)
    sea=(x<west and not neck) or y<north or x>east or y>south
    river=(48<x<105 and abs(y-(81+3*math.sin(x*.055)))<2) or (y>40 and abs(x-(86+5*math.sin(y*.058)))<2) or (y>44 and abs(x-(176+5*math.sin(y*.048)))<2)
    return sea or river

def build(source):
    old={m.id:m for m in source};result=[]
    for mid,(gx,gy,w,h,dx,dy) in REGIONS.items():
        core=old[mid];m=Map(mid,core.name,w,h,seed=sum(map(ord,mid))+2410)
        m.world_origin=(gx,gy);m.legacy_height=core.legacy_height;m.core_offset=(dx,dy);m.skip_smooth=True;m.hours=0
        for y in range(h):
            for x in range(w):m.water[y][x]=m.blocked[y][x]=wet(gx+x,gy+y)
        nature=set()
        for name,x,y in core.stamps:
            if STAMPS[name][0]=='nature':
                for yy in range(y,y+STAMPS[name][4]):
                    for xx in range(x,x+STAMPS[name][3]):nature.add((xx,yy))
        # Keep functional buildings and authored yards at native scale; grow landscape around them.
        for y in range(core.h):
            for x in range(core.w):
                a,b=x+dx,y+dy
                m.water[b][a]=core.water[y][x] if mid=='Gaegyeong' and 7<=y<=12 else wet(gx+a,gy+b)
                m.road[b][a]=core.road[y][x]
                m.blocked[b][a]=bool((core.blocked[y][x] and not core.water[y][x] and (x,y) not in nature) or m.water[b][a])
                if (x,y) not in nature:
                    for layer in ('obj','high'):getattr(m,layer)[b][a]=getattr(core,layer)[y][x]
                if m.obj[b][a] or m.high[b][a]:m.water[b][a]=False
                if m.road[b][a]:
                    if m.water[b][a]:m.decks[a,b]=K('water',1,13,'wood')
                    m.water[b][a]=m.blocked[b][a]=False
                m.reserved[b][a]=bool(m.road[b][a] or m.water[b][a] or m.obj[b][a] or m.high[b][a])
                # Preserve agricultural ground and lotus rather than copying the old rectangular grass patch.
                if core.ground[y][x] and core.ground[y][x].startswith('field:'):m.ground[b][a]=core.ground[y][x]
                if core.deco[y][x] and core.deco[y][x].startswith('goryeo_'):m.deco[b][a]=core.deco[y][x]
        m.stamps=[(n,x+dx,y+dy) for n,x,y in core.stamps if STAMPS[n][0]!='nature']
        for (x,y),v in core.decks.items():
            # An old bridge only remains a bridge where the new river actually exists.
            if mid=='Naru' or wet(gx+x+dx,gy+y+dy):m.decks[x+dx,y+dy]=v
        for o in core.objects:
            if o['kind'] in ('exit','anim') or o['kind']=='spawn' and o['id']!='S0':continue
            n=copy.deepcopy(o);n['x']+=dx;n['y']+=dy
            if 'path' in n:n['path']=[(x+dx,y+dy) for x,y in n['path']]
            m.objects.append(n);m.reserve(n['x'],n['y'],n['x'],n['y'])
        if mid=='Naru':
            # Keep the complete moored boat afloat after replacing the old regional river.
            # The inlet meets the main river; roads/piers, whole buildings and residents stay intact.
            protected={(o['x'],o['y']) for o in m.objects}
            for name,x,y in m.stamps:
                if name!='boat':continue
                bw,bh=STAMPS[name][3:5];cx=x+bw/2+5;cy=y+bh/2
                boat={(x+a,y+b) for a in range(bw) for b in range(bh)}
                for yy in range(max(0,y-6),min(m.h,y+bh+6)):
                    for xx in range(max(0,x-6),min(m.w,x+bw+18)):
                        if ((xx+.5-cx)/14)**2+((yy+.5-cy)/7)**2>1:continue
                        if (xx,yy) not in boat and (m.obj[yy][xx] or m.high[yy][xx] or m.road[yy][xx] or (xx,yy) in protected or (xx,yy) in m.decks):continue
                        m.water[yy][xx]=m.blocked[yy][xx]=m.reserved[yy][xx]=True
        m.atlas_label={'x':dx+20,'y':dy+20}
        result.append(m)
    # Compact the owned eastern continental quarter; keep all six template dimensions/assets.
    central_positions={'transit_crossroads':(-200,-50),'transit_central_approach':(-96,-50),
        'transit_ferry':(-200,0),'transit_wetroad':(-100,0),'transit_woodtown':(-100,40),'transit_pass':(0,0)}
    for m in source:
        if m.id.startswith('transit_'):
            m.world_origin=central_positions[m.id]
            m.objects=[o for o in m.objects if o['kind']!='exit' and not(o['kind']=='spawn' and o['id']!='S0')]
            result.append(m)
    by={m.id:m for m in result}
    # Shared world coordinates determine both ends, including offset edges of unequal rectangles.
    links=[('transit_crossroads','transit_central_approach',-24),('transit_crossroads','transit_ferry',-150),
           ('transit_central_approach','transit_wetroad',-60),('transit_ferry','transit_wetroad',22),
           ('transit_wetroad','transit_woodtown',-42),('transit_wetroad','transit_pass',22),
           ('transit_pass','goryeo_songak',22),('goryeo_songak','Gaegyeong',30),
           ('goryeo_songak','Naru',70),('Gaegyeong','Dakmok',30),
           ('Gaegyeong','goryeo_jangdan',145),('Naru','goryeo_jangdan',91),
           ('Dakmok','goryeo_jangdan',185)]

    def connect(m,p):
        candidates=[(x,y) for y in range(2,m.h-2) for x in range(2,m.w-2) if m.road[y][x] and not m.blocked[y][x]]
        target=min(candidates,key=lambda q:abs(q[0]-p[0])+abs(q[1]-p[1]))
        for x,y in road_path(m,p,target,carve=True):
            for a,b in ((x,y),(x+1,y),(x,y+1)):
                if not m.inside(a,b) or m.obj[b][a] or m.high[b][a]:continue
                if m.water[b][a]:m.decks[a,b]=K('water',1,13,'wood')
                m.water[b][a]=m.blocked[b][a]=False;m.road[b][a]=max(2,m.road[b][a]);m.reserved[b][a]=True
    for aid,bid,t in links:
        a,b=by[aid],by[bid];ax,ay=a.world_origin;bx,byy=b.world_origin
        horizontal=ax+a.w==bx
        edge,other=('E','W') if horizontal else ('S','N')
        def clear_apron(m,e,k):
            for offset in range(3):
                for depth in range(3):
                    x,y=(m.w-1-depth,k+offset) if e=='E' else (depth,k+offset) if e=='W' else (k+offset,m.h-1-depth) if e=='S' else (k+offset,depth)
                    if not m.inside(x,y) or m.obj[y][x] or m.high[y][x]:return False
            return True
        lo,hi=(max(ay,byy),min(ay+a.h,byy+b.h)) if horizontal else (max(ax,bx),min(ax+a.w,bx+b.w))
        choices=sorted(range(lo+1,hi-3),key=lambda v:(abs(v-t),v))
        t=next((v for v in choices if clear_apron(a,edge,v-(ay if horizontal else ax)) and clear_apron(b,other,v-(byy if horizontal else bx))),None)
        if t is None:raise ValueError(('No intact-object portal apron',aid,bid))
        ta=t-ay if horizontal else t-ax;tb=t-byy if horizontal else t-bx
        sa='to_'+bid;sb='to_'+aid
        for m,e,k in ((a,edge,ta),(b,other,tb)):
            for d in range(3):
                p=(m.w-1,k+d) if e=='E' else (0,k+d) if e=='W' else (k+d,m.h-1) if e=='S' else (k+d,0)
                connect(m,p)
                # The exit and its landing apron must both fit the full portal width.
                # Carve water/terrain only: never remove part of a tree/building stamp.
                inward=(-1,0) if e=='E' else (1,0) if e=='W' else (0,-1) if e=='S' else (0,1)
                for depth in (1,2):
                    q=(p[0]+inward[0]*depth,p[1]+inward[1]*depth)
                    if m.obj[q[1]][q[0]] or m.high[q[1]][q[0]]:
                        raise ValueError(('Portal apron intersects complete object',m.id,q))
                    connect(m,q)
        a.link(edge,ta,3,bid,sb,sa);b.link(other,tb,3,aid,sa,sb)
    # Old boundary mouths can become dead-end fragments after rearranging templates.
    # Rejoin each fragment to the main road instead of leaving disconnected paved cells.
    for m in result:
        while True:
            road={(x,y) for y in range(m.h) for x in range(m.w) if m.road[y][x] and not m.blocked[y][x]}
            remaining=set(road);components=[]
            while remaining:
                start=min(remaining);component={start};queue=collections.deque([start]);remaining.remove(start)
                while queue:
                    x,y=queue.popleft()
                    for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                        if p in remaining:remaining.remove(p);component.add(p);queue.append(p)
                components.append(component)
            if len(components)<=1:break
            components.sort(key=lambda c:(-len(c),min(c)))
            main=components[0];fragment=components[1]
            start,end=min(((a,b) for a in sorted(fragment) for b in sorted(main)),key=lambda ab:abs(ab[0][0]-ab[1][0])+abs(ab[0][1]-ab[1][1]))
            overhead=[(x,y) for y in range(m.h) for x in range(m.w) if (m.obj[y][x] or m.high[y][x]) and not m.blocked[y][x]]
            for x,y in overhead:m.blocked[y][x]=True
            try:joined=road_path(m,start,end,carve=True)
            finally:
                for x,y in overhead:m.blocked[y][x]=False
            for x,y in joined:
                if m.water[y][x]:m.decks[x,y]=K('water',1,13,'wood')
                m.water[y][x]=m.blocked[y][x]=False;m.road[y][x]=max(2,m.road[y][x]);m.reserved[y][x]=True
    # Woodland clusters depend on the shared landscape, not rectangular perimeter rows.
    for m in result:
        if m.id not in REGIONS:continue
        gx,gy=m.world_origin
        for _ in range(m.w*m.h//3):
            x=m.rng.randrange(2,m.w-5);y=m.rng.randrange(2,m.h-5)
            wx,wy=gx+x,gy+y
            density=.45+.25*math.sin(wx*.12+math.sin(wy*.09)*2)+.22*math.cos(wy*.15-wx*.07)
            if m.rng.random()>max(.08,density)*.38:continue
            nearwater=any(m.water[b][a] for a in range(max(0,x-2),min(m.w,x+5)) for b in range(max(0,y-2),min(m.h,y+5)))
            choices=[5,5,10,10,9] if nearwater else [0,0,1,1,2,3,6,8,9,9,9,11] if m.id=='goryeo_songak' else ([4,4,4,6,6,9,9,8,11] if math.sin(wx*.05+wy*.03)>0 else [0,0,2,2,7,9,9,8,11])
            idx=m.rng.choice(choices);name='goryeo_woodland'+str(idx)
            # Keep a full-cell shoulder beside walking routes.
            sw,sh=STAMPS[name][3:5]
            if any(m.road[b][a] for a in range(max(0,x-1),min(m.w,x+sw+1)) for b in range(max(0,y-1),min(m.h,y+sh+1))):continue
            m.stamp(name,x,y)
    water={(m.world_origin[0]+x,m.world_origin[1]+y) for m in result for y in range(m.h) for x in range(m.w) if m.water[y][x] or (x,y) in m.decks}
    roads={(m.world_origin[0]+x,m.world_origin[1]+y) for m in result for y in range(m.h) for x in range(m.w) if m.road[y][x]}
    for m in result:
        # Refinish every owned map because adjacent world coordinates changed.
        gx,gy=m.world_origin
        m.outside_water=lambda x,y,gx=gx,gy=gy:(gx+x,gy+y) in water
        m.outside_road=lambda x,y,gx=gx,gy=gy:(gx+x,gy+y) in roads
        m.objects=[o for o in m.objects if o['kind']!='anim']
        for (x,y),v in m.decks.items():m.ground[y][x]=v
        m.finish()
        if m.id in REGIONS:
            # Whole-cell road fill: the traversable cell never contains half a path between two cells.
            for y in range(m.h):
                for x in range(m.w):
                    if m.road[y][x] and (x,y) not in m.decks:m.ground[y][x]=K('floor',1,8)
            # Bound dynamic decoration cost independently of map area.
            wave=0;objects=[]
            for o in m.objects:
                if o['kind']=='anim':
                    wave+=1
                    if wave>96:continue
                objects.append(o)
            m.objects=objects
        # Recompute accessibility after the new roads and vegetation.
        s=next(o for o in m.objects if o['kind']=='spawn' and o['id']=='S0')
        seen={(s['x'],s['y'])};q=collections.deque(seen)
        while q:
            x,y=q.popleft()
            for a,b in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if m.inside(a,b) and not m.blocked[b][a] and (a,b) not in seen:seen.add((a,b));q.append((a,b))
        m.world_reachable=seen
    # All physically open shared edges get reciprocal portals, including unequal edge offsets.
    owners={(m.world_origin[0]+x,m.world_origin[1]+y) for m in result for y in range(m.h) for x in range(m.w) if not m.blocked[y][x]}
    start=next(o for o in by['Naru'].objects if o['kind']=='spawn' and o['id']=='S0')
    seen={(by['Naru'].world_origin[0]+start['x'],by['Naru'].world_origin[1]+start['y'])};q=collections.deque(seen)
    while q:
        x,y=q.popleft()
        for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if p in owners and p not in seen:seen.add(p);q.append(p)
    for m in result:
        gx,gy=m.world_origin;m.world_reachable={(x,y) for y in range(m.h) for x in range(m.w) if (gx+x,gy+y) in seen}
    for a in result:
        for b in result:
            if a is b:continue
            ax,ay=a.world_origin;bx,byy=b.world_origin
            if ax+a.w==bx:edge,other='E','W';lo,hi=max(ay,byy),min(ay+a.h,byy+b.h);ao,bo=ay,byy
            elif ay+a.h==byy:edge,other='S','N';lo,hi=max(ax,bx),min(ax+a.w,bx+b.w);ao,bo=ax,bx
            else:continue
            occupied=set()
            for o in a.objects:
                if o['kind']=='exit' and o['target']==b.id and o['edge']==edge:
                    st=o['y'] if edge=='E' else o['x'];length=o.get('h',1) if edge=='E' else o.get('w',1)
                    occupied.update(range(st+ao,st+ao+length))
            valid=[]
            for t in range(lo,hi):
                ac=[(a.w-1,t-ao),(a.w-2,t-ao)] if edge=='E' else [(t-ao,a.h-1),(t-ao,a.h-2)]
                bc=[(0,t-bo),(1,t-bo)] if edge=='E' else [(t-bo,0),(t-bo,1)]
                if t not in occupied and all(p in m.world_reachable for m,ps in ((a,ac),(b,bc)) for p in ps):valid.append(t)
            runs=[]
            for t in valid:
                if runs and runs[-1][-1]+1==t:runs[-1].append(t)
                else:runs.append([t])
            for i,run in enumerate(runs):
                sa='seam_'+b.id+'_'+str(i);sb='seam_'+a.id+'_'+str(i)
                a.link(edge,run[0]-ao,len(run),b.id,sb,sa);b.link(other,run[0]-bo,len(run),a.id,sa,sb)
    assert sum(m.w*m.h for m in result if m.id in REGIONS)==20000
    return result
