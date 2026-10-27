"""Authored village landmarks at the FINAL tile scale, never a picture-only map overlay."""
from tiles import STAMPS, K
from compact import road_path


def add(m):
    if m.id not in ('Gaegyeong','Naru','Dakmok','goryeo_songak','goryeo_jangdan'):
        return
    protected=set()
    for o in m.objects:
        protected.add((o['x'],o['y']))
        for a,b in zip(o.get('path',[]),o.get('path',[])[1:]):
            steps=max(abs(a[0]-b[0]),abs(a[1]-b[1]))
            for i in range(steps+1):protected.add((round(a[0]+(b[0]-a[0])*i/max(1,steps)),round(a[1]+(b[1]-a[1])*i/max(1,steps))))
    placements=[]
    def place(name,x,y,radius=4,connect=False):
        full='goryeo_'+name;_,_,_,w,h,*_=STAMPS[full]
        candidates=[(x+dx,y+dy) for dy in range(-radius,radius+1) for dx in range(-radius,radius+1)]
        candidates.sort(key=lambda q:(abs(q[0]-x)+abs(q[1]-y),q[1],q[0]))
        for a,b in candidates:
            if a<3 or b<3 or a+w>m.w-3 or b+h>m.h-3:continue
            cells={(a+dx,b+dy) for dx in range(w) for dy in range(h)}
            if cells & protected or any(m.road[yy][xx] or m.water[yy][xx] for xx,yy in cells):continue
            overlaps=[stamp for stamp in m.stamps if cells & {(stamp[1]+dx,stamp[2]+dy) for dx in range(STAMPS[stamp[0]][3]) for dy in range(STAMPS[stamp[0]][4])}]
            if any(STAMPS[n][0]!='nature' for n,_,_ in overlaps):continue
            foot=(a+w//2,b+h)
            if connect and (not m.inside(*foot) or m.blocked[foot[1]][foot[0]]):continue
            # Clear whole trees for an authored clearing, never leave isolated trunk/canopy tiles.
            for n,tx,ty in overlaps:
                for dy in range(STAMPS[n][4]):
                    for dx in range(STAMPS[n][3]):
                        xx,yy=tx+dx,ty+dy;m.obj[yy][xx]=None;m.high[yy][xx]=None
                        m.blocked[yy][xx]=m.water[yy][xx];m.reserved[yy][xx]=bool(m.road[yy][xx] or m.water[yy][xx])
                m.stamps.remove((n,tx,ty))
            if not m.stamp(full,a,b,force=True):continue
            placements.append((name,a,b))
            if connect:connect_to_road(m,foot)
            if name=='lantern':m.add('lantern',a,b,id='goryeo_stone_lantern')
            return a,b
        raise ValueError(('No space for authored landmark',m.id,name,x,y))

    if m.id=='Gaegyeong':
        for x,y in [(14,9),(25,10),(14,23),(25,23)]:place('lantern',x,y,radius=2)
        for x,y in [(7,3),(28,3)]:place('wall',x,y,radius=1)
        place('celadon',14,16,radius=2)
        # Lotus is a non-blocking overlay on existing water; do not turn it into land.
        for x,y in [(9,10),(29,10)]:
            if all(m.water[y][x+dx] for dx in range(2)):
                for dx in range(2):m.deco[y][x+dx]=K('goryeo_lotus',dx,0)
    elif m.id=='Naru':
        place('manor',27,4,radius=5,connect=True)
        place('celadon',23,6,radius=4)
        place('lantern',24,8,radius=3)
        # Put the retained boat IN the river, alongside a walkable timber landing.
        for name,x,y in m.stamps:
            if name=='boat':
                w,h=STAMPS[name][3:5]
                for dy in range(h):
                    for dx in range(w):
                        m.water[y+dy][x+dx]=True;m.blocked[y+dy][x+dx]=True
        for x in (11,12,18,19,20,23,25,5,3):
            cells={(a,b) for a in range(x,x+2) for b in range(13,19)}
            if any(m.obj[b][a] or m.high[b][a] for a,b in cells) or cells & protected:continue
            m.bridge(x,13,x+1,18,dock=True)
            for a,b in cells:m.blocked[b][a]=False
            connect_to_road(m,(x,13),exclude=cells)
            m.add('pastime',x,18,id='goryeo_fishing',name='나루 낚시',character='NPC/npc_naru_fishing_child/npc_naru_fishing_child_tall')
            break
        else:raise ValueError('Naru needs a clear pier')
    elif m.id=='Dakmok':
        place('manor',24,4,radius=5,connect=True)
        place('paper',20,7,radius=4,connect=True)
        place('paper',25,11,radius=4,connect=True)
        place('lantern',21,10,radius=3)
    elif m.id=='goryeo_songak':
        place('pavilion',27,6,radius=5,connect=True)
        for x,y in [(26,12),(33,12)]:place('lantern',x,y,radius=4)
        place('wall',27,4,radius=4)
    elif m.id=='goryeo_jangdan':
        place('cottage',18,5,radius=3,connect=True)
        place('harvest',14,11,radius=3)
        place('harvest',4,15,radius=3)
        place('lantern',18,12,radius=3)
    m.authored_details=placements


def connect_to_road(m,start,exclude=None):
    exclude=exclude or set()
    candidates=[(x,y) for y in range(2,m.h-2) for x in range(2,m.w-2) if m.road[y][x] and not m.blocked[y][x] and (x,y)!=start and (x,y) not in exclude]
    end=min(candidates,key=lambda p:abs(p[0]-start[0])+abs(p[1]-start[1]))
    path=road_path(m,start,end,carve=True)
    for x,y in path:
        m.road[y][x]=max(2,m.road[y][x]);m.reserved[y][x]=True
        if m.water[y][x]:
            m.decks[x,y]=K('water',1,13,'woodturn');m.water[y][x]=False;m.blocked[y][x]=False
