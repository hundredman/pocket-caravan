"""원형 JSON → 일반 Map. 길/충돌 구조에는 환경/날씨를 섞지 않는다."""
from tiles import Map, K, PROPS
from collections import deque
SIZE=40
# 연결부는 모든 원형에서 공통. 모서리를 돌려 재사용할 때는 source의 paths를 편집한다.
SHAPES={
 'straight':([([(0,19),(39,19)],3,3)],'WE'),
 'bend':([([(0,19),(10,19),(10,10),(27,10),(27,19),(39,19)],3,2)],'WE'),
 'fork':([([(0,19),(20,19),(39,19)],3,2), ([(20,19),(20,0)],3,1)],'WEN'),
 'cross':([([(0,19),(39,19)],3,3), ([(19,0),(19,39)],3,2)],'WENS'),
 'loop':([([(0,19),(9,19),(9,10),(29,10),(29,19),(39,19)],3,3), ([(9,19),(9,29),(29,29),(29,19)],2,1)],'WE'),
 'split':([([(0,19),(8,19),(31,19),(39,19)],2,1), ([(8,19),(8,27),(31,27),(31,19)],3,3)],'WE'),
 'switchback':([([(0,19),(7,19),(7,8),(16,8),(16,28),(26,28),(26,19),(39,19)],3,2)],'WE'),
 'choke':([([(0,19),(12,19),(12,16),(26,16),(26,19),(39,19)],3,2)],'WE'),
 'village':([([(0,19),(39,19)],3,2), ([(19,0),(19,39)],3,2), ([(9,13),(30,13)],2,1)],'WENS'),
 'camp':([([(0,19),(39,19)],3,2), ([(12,19),(12,12),(27,12),(27,19)],2,1)],'WE'),
 'shore':([([(0,19),(8,19),(8,29),(31,29),(31,19),(39,19)],3,2)],'WE'),
 'coast':([([(0,19),(9,19),(16,15),(28,15),(34,19),(39,19)],3,2)],'WE'),
 'harbor':([([(0,19),(39,19)],3,2), ([(22,19),(22,29)],3,2)],'WE'),
 'island':([([(0,19),(39,19)],3,2)],'WE'),
 'ruin':([([(0,19),(39,19)],3,2), ([(10,19),(10,10),(28,10),(28,19)],2,1)],'WE')
}


def build(spec):
    m=Map('module_'+spec['id'],spec['label'],SIZE,SIZE,seed=spec['seed'])
    shape=spec['shape']; default,ports=SHAPES[shape]
    paths=spec.get('paths',[dict(points=p,width=w,grade=g) for p,w,g in default])
    # 물 윤곽을 먼저 만들고 길과 다리를 겹친다.
    river=spec.get('river')
    if river:m.river_v(14 if river=='wide' else 19,25 if river=='wide' else 22,wiggle=0)
    if river=='wide':
        for y in range(13,28):
            for x in range(17,23):
                m.water[y][x]=False;m.reserved[y][x]=False
    if spec.get('sea'):m.river_h(31,39,wiggle=1)
    ponds=spec.get('ponds')
    if ponds=='center':m.water_blob(20,20,6,5,jitter=.08)
    elif ponds=='lake':m.water_blob(21,11,11,10,jitter=.08)
    elif ponds=='marsh':
        for x,y,rx,ry in [(9,10,6,5),(30,29,7,5),(16,29,5,3),(32,10,5,5)]:m.water_blob(x,y,rx,ry,jitter=.12)
        m.water_rect(6,17,33,23)
    for p in paths:m.road_path(p['points'],width=p['width'],grade=p['grade'])
    for edge in spec.get('ports',list(ports)):
        if edge=='W':m.road_rect(0,19,3,21,grade=2)
        elif edge=='E':m.road_rect(36,19,39,21,grade=2)
        elif edge=='N':m.road_rect(19,0,21,3,grade=2)
        elif edge=='S':m.road_rect(19,36,21,39,grade=2)
    if shape in ('village','camp','cross','harbor'):m.road_blob(20,20,6,4,grade=2)
    # Bridges cover exact water intersection of each connected road, including island spans.
    if river:
        ys=[(19,21)]
        if shape=='bend':ys=[(10,12)]
        if spec.get('bridges')=='split':ys=[(19,20),(27,29)]
        for y0,y1 in ys:
            if river=='wide':
                m.bridge(12,y0,16,y1);m.bridge(23,y0,27,y1)
            else:m.bridge(17,y0,24,y1)
        if spec.get('bridges')=='split':
            for y in (19,20):
                for x in range(17,25):m.road[y][x]=1
    if spec.get('boardwalk'):m.bridge(5,19,34,21)
    if spec.get('dock'):m.bridge(21,28,25,35,dock=True)
    # Roads touching ponds get explicit decks rather than an invisible road through water.
    for y in range(SIZE):
        for x in range(SIZE):
            if m.water[y][x] and m.road[y][x]:m.bridge(x,y,x,y)
    if spec.get('hills'):
        for a in [(2,2,9,6),(29,3,37,8),(3,31,9,37)]:
            if not any(m.road[y][x] or m.water[y][x] for y in range(a[1],a[3]+1) for x in range(a[0],a[2]+1)):m.plateau(*a)
    # Repeatable sockets: houses are complete stamps, door paths are added before trees.
    sites=[(17,14)] if river=='wide' else ([(5,7),(27,5),(29,24),(5,24),(13,5)] if spec.get('sea') else [(5,7),(27,5),(29,25),(5,27),(15,5)])
    if ponds=='lake':sites=[(3,7),(3,25),(16,33)]
    elif spec.get('hills') and shape=='village':sites=[(5,7),(27,9),(29,25),(5,27)]
    for i,(x,y) in enumerate(sites[:spec.get('houses',0)]):
        name=['house_thatch','house_plain','store','house_tile','shop'][i]
        m.stamp(name,x,y)
        # Stamp returns None on success in legacy API: find placement in registry instead.
        if (name,x,y) not in m.stamps:continue
        start=(x+1,y+3);queue=deque([start]);previous={start:None};target=None
        while queue:
            a,b=queue.popleft()
            if m.road[b][a] and not m.blocked[b][a]:target=(a,b);break
            for p in [(a-1,b),(a+1,b),(a,b-1),(a,b+1)]:
                xx,yy=p
                if p not in previous and m.inside(xx,yy) and not m.blocked[yy][xx] and not m.water[yy][xx]:
                    previous[p]=(a,b);queue.append(p)
        if target is not None:
            route=[target]
            while previous[route[-1]] is not None:route.append(previous[route[-1]])
            if len(route)==1:m.road_rect(*start,*start,grade=1)
            else:m.road_path(list(reversed(route)),width=1,grade=1)
    if spec.get('fields'):
        for x,y in [(3,3),(7,3),(30,32),(34,32)]:m.stamp('wheat',x,y)
    if spec.get('ruins'):
        for x,y in [(14,6),(24,6),(12,25),(26,25)]:m.stamp('module_ruin',x,y)
    if spec.get('boats'):m.stamp('boat',30,35)
    m.objects.append(dict(kind='spawn',x=2,y=20,id='S0',edge='W'))
    m.reserve(0,19,3,22)
    if spec.get('trees'):m.scatter(spec['trees'],spec.get('density',20),area=(1,1,35,35),margin=0)
    for name,key in [('rocks','module_rock'),('cacti','module_cactus')]:
        if spec.get(name):m.scatter(key,spec[name],area=(1,1,36,35),margin=0)
    if spec.get('reeds'):
        for y in range(1,39):m.reeds(y,1,38,chance=.4)
    m.flowers(1,1,38,38,spec.get('flowers',5))
    m.finish()
    m.module_ports=spec.get('ports',list(ports));m.module_spec=spec
    return m
