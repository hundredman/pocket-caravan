"""M1 지형 v3 검토안: B1 해안선·면적·마을 위치·강 경유점은 그대로 두고, 지형을 실제 땅처럼 만든다.

- 높이: 산맥 능선(흔들린 선) + 산자락 + 해안에서 멀어질수록 높아짐 + 넓은 기복 노이즈
- 강: 경유점은 지키되 낮은 땅을 따라 굽이치게, 하류로 갈수록 넓게, 강 따라 골짜기
- 지형 종류: 깊은/얕은 바다, 모래 해변, 바위 해안, 강, 습지(하구), 들판, 풀밭, 언덕, 숲, 산(못 지나감), 봉우리(못 지나감)
- 마을 범위: 마을마다 목표 면적만큼 평평한 땅을 자연스러운 모양으로 잡는다(본편 아님)
- 길(--roads): 새 지형 비용을 따라 마을 12쌍을 잇는다. 기존 길에 합류하면 싸고, 숲·언덕·습지는 비싸고, 강은 다리로 건넌다.

본편 Maps/·Scenes/는 바꾸지 않는다. 결과는 ArtReview/M1-Terrain-v3/.
"""
import argparse,heapq,json,math,sys
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).parent))
from world_layout import WorldLayout

OUT=ROOT/'ArtReview/M1-Terrain-v3'
FONT=ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf'
SEED=7
X0,Y0,W,H=0,-130,300,260   # 배열 좌표: ix=x-X0, iy=y-Y0

# 지형 종류
VOID,DEEP,SHALLOW,SAND,ROCKCOAST,RIVER,MARSH,FIELD,GRASS,MEADOW,HILL,FOREST,MOUNTAIN,PEAK,VILLAGE=range(15)
NAMES=['밖','깊은 바다','얕은 바다','모래 해변','바위 해안','강','습지','들판(밭)','풀밭','꽃풀밭','언덕','숲','산(통행 불가)','봉우리(통행 불가)','마을 범위']
COLORS={DEEP:(52,98,140),SHALLOW:(96,156,186),SAND:(236,222,170),ROCKCOAST:(150,142,128),RIVER:(84,146,196),
        MARSH:(96,128,96),FIELD:(204,176,84),GRASS:(118,164,88),MEADOW:(142,182,98),HILL:(134,150,86),
        FOREST:(52,98,56),MOUNTAIN:(128,114,96),PEAK:(184,178,170),VILLAGE:(210,190,140),VOID:(28,34,40)}
BLOCKED={DEEP,SHALLOW,RIVER,MOUNTAIN,PEAK,ROCKCOAST}
# 마을 목표 면적(칸): 수도는 크게, 중앙 마을은 장터 광장·이벤트 마당·시설이 들어갈 만큼(WorldLayout 3절 약 2,060)
VILLAGE_AREA={'goryeo/개경':1100,'goryeo/장단':700,'goryeo/나루':700,'goryeo/송악':650,'goryeo/닥목':600,
              'east/동문 마을':1400,'east/북동 포구':1400,'east/남동 포구':1400,'east/중앙 길목 마을':1500}


def idx(x,y):return y-Y0,x-X0


def noise(shape,scale,rng,octaves=4):
    out=np.zeros(shape);amp=1;total=0
    for o in range(octaves):
        s=max(2,int(shape[0]/scale*2**o)+2),max(2,int(shape[1]/scale*2**o)+2)
        g=rng.random(s)
        z=ndimage.zoom(g,(shape[0]/s[0]*1.02,shape[1]/s[1]*1.02),order=3)[:shape[0],:shape[1]]
        if z.shape!=shape:z=np.pad(z,((0,shape[0]-z.shape[0]),(0,shape[1]-z.shape[1])),mode='edge')
        out+=amp*z;total+=amp;amp*=.5
    out/=total;return (out-out.min())/(out.max()-out.min()+1e-9)


def draw_line(mask,a,b):
    n=int(max(abs(b[0]-a[0]),abs(b[1]-a[1]))*2)+1
    for i in range(n+1):
        t=i/n;x=a[0]+(b[0]-a[0])*t;y=a[1]+(b[1]-a[1])*t
        iy,ix=idx(int(math.floor(x)),int(math.floor(y)))
        if 0<=iy<H and 0<=ix<W:mask[iy,ix]=True


def astar(start,goal,cost,blocked=None,eight=True):
    """격자 A*. cost는 칸마다 양수. blocked는 못 지나가는 칸. 대각선 모서리 끼어들기 금지."""
    dirs=[(1,0),(-1,0),(0,1),(0,-1)]+([(1,1),(1,-1),(-1,1),(-1,-1)] if eight else [])
    sy,sx=start;gy,gx=goal
    best={start:0.0};parent={start:None};q=[(0.0,0.0,start)];minc=float(cost.min())
    while q:
        _,g,p=heapq.heappop(q)
        if g!=best[p]:continue
        if p==goal:
            path=[]
            while p is not None:path.append(p);p=parent[p]
            return path[::-1]
        y,x=p
        for dy,dx in dirs:
            ny,nx=y+dy,x+dx
            if not(0<=ny<H and 0<=nx<W):continue
            if blocked is not None and blocked[ny,nx] and (ny,nx)!=goal:continue
            if dy and dx and blocked is not None and (blocked[y+dy,x] or blocked[y,x+dx]):continue
            v=g+math.hypot(dy,dx)*(cost[y,x]+cost[ny,nx])/2
            if v<best.get((ny,nx),math.inf):
                best[(ny,nx)]=v;parent[(ny,nx)]=p
                heapq.heappush(q,(v+minc*math.hypot(ny-gy,nx-gx),v,(ny,nx)))
    raise ValueError('경로 없음')


# 검토안 선택: 중앙 마을 크기와 추가 쉼터 마을
VARIANT={'area':{},'hamlets':[]}
SMALL_CENTRAL={'east/동문 마을':750,'east/북동 포구':800,'east/남동 포구':800,'east/중앙 길목 마을':850}
HAMLETS=[('east/북쪽 내륙길','북쪽 쉼터 마을',320),('east/남쪽 내륙길','남쪽 쉼터 마을',320)]


def build(layout):
    rng=np.random.default_rng(SEED)
    owned=np.zeros((H,W),bool);land=np.zeros((H,W),bool);mapid=np.full((H,W),-1,int)
    keys=list(layout.maps)
    for k,cells in layout.map_cells.items():
        for x,y in cells:
            iy,ix=idx(x,y);owned[iy,ix]=True;mapid[iy,ix]=keys.index(k)
    for x,y in layout.land:
        iy,ix=idx(x,y)
        if 0<=iy<H and 0<=ix<W:land[iy,ix]=True
    land_full=land.copy()           # 담당 밖 육지(다른 팀 땅)도 땅으로 본다: 경계선을 해안으로 착각하지 않게
    land&=owned
    sea=owned&~land

    # 1) 높이
    to_sea=ndimage.distance_transform_edt(land_full)
    ridge=np.zeros((H,W),bool)
    pts=layout.data['mountains']
    groups=(pts[:6],pts[6:9],pts[9:12])   # B2와 같은 산맥 묶음. 남쪽 참고 봉우리 2개는 담당 밖이라 쓰지 않음
    jit=noise((H,W),18,rng,3)
    for g in groups:
        dense=[]
        for a,b in zip(g,g[1:]):
            for i in range(12):
                t=i/12;dense.append((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t))
        dense.append(g[-1])
        for i,(a,b) in enumerate(zip(dense,dense[1:])):
            vx,vy=b[0]-a[0],b[1]-a[1];L=math.hypot(vx,vy) or 1;px,py=-vy/L,vx/L
            def off(p):
                iy,ix=idx(int(p[0]),int(p[1]));iy=min(max(iy,0),H-1);ix=min(max(ix,0),W-1)
                return (jit[iy,ix]-.5)*7
            draw_line(ridge,(a[0]+px*off(a),a[1]+py*off(a)),(b[0]+px*off(b),b[1]+py*off(b)))
        # 곁가지 능선(산맥이 한 줄로 보이지 않게)
        for a in dense[2:-2:5]:
            ang=rng.uniform(0,2*math.pi);L=rng.uniform(5,11)
            draw_line(ridge,a,(a[0]+math.cos(ang)*L,a[1]+math.sin(ang)*L))
    d_ridge=ndimage.distance_transform_edt(~ridge)
    broad=noise((H,W),55,rng,4);fine=noise((H,W),14,rng,3)
    elev=(.55*np.exp(-(d_ridge/7)**2)+.32*np.exp(-(d_ridge/20)**2)
          +.16*np.minimum(to_sea/30,1)+.38*broad+.09*fine)

    # 2) 강: 경유점은 그대로, 사이는 낮은 땅을 따라 굽이침
    river=np.zeros((H,W),bool);river_info=[]
    meander=noise((H,W),9,rng,3)
    for r in layout.data['rivers']:
        wps=[idx(int(math.floor(x)),int(math.floor(y))) for x,y in r['waypoints']]
        # 발원지는 가까운 능선
        ry,rx=np.nonzero(ridge);s=wps[0]
        k=int(np.argmin((ry-s[0])**2+(rx-s[1])**2));src=(int(ry[k]),int(rx[k]))
        # 경유점 사이를 사인파 + 노이즈로 굽이치게(진폭은 구간 길이에 비례, 바다로 새지 않게)
        anchors=[src]+wps;path=[]
        for si,(a,b) in enumerate(zip(anchors,anchors[1:])):
            L=math.hypot(b[0]-a[0],b[1]-a[1]);n=max(2,int(L/2.5))
            uy,ux=(b[0]-a[0])/max(L,1e-6),(b[1]-a[1])/max(L,1e-6);py,px=-ux,uy
            amp=min(5.5,L/5);lam=rng.uniform(14,24);ph=rng.uniform(0,2*math.pi)
            last=si==len(anchors)-2
            for i in range(n+1):
                u=i/n;env=math.sin(math.pi*u)   # 경유점에서는 0
                o=env*(amp*math.sin(2*math.pi*u*L/lam+ph)+(meander[min(H-1,max(0,int(a[0]+(b[0]-a[0])*u))),min(W-1,max(0,int(a[1]+(b[1]-a[1])*u)))]-.5)*4)
                y=a[0]+(b[0]-a[0])*u+py*o;x=a[1]+(b[1]-a[1])*u+px*o
                iy,ix=int(round(y)),int(round(x))
                if not last and not land[min(H-1,max(0,iy)),min(W-1,max(0,ix))]:
                    y=a[0]+(b[0]-a[0])*u;x=a[1]+(b[1]-a[1])*u
                path.append((int(round(y)),int(round(x))))
        # 계단 모양을 없애려고 중심선을 여러 번 부드럽게 한 뒤 다시 칸으로
        pts=[(float(y),float(x)) for y,x in path[::2]]+[(float(path[-1][0]),float(path[-1][1]))]
        for _ in range(4):
            new=[pts[0]]
            for a,b in zip(pts,pts[1:]):new+=[(a[0]*.75+b[0]*.25,a[1]*.75+b[1]*.25),(a[0]*.25+b[0]*.75,a[1]*.25+b[1]*.75)]
            new.append(pts[-1]);pts=new
        dense=[]
        for a,b in zip(pts,pts[1:]):
            n=max(1,int(math.hypot(b[0]-a[0],b[1]-a[1])*3))
            dense+=[(a[0]+(b[0]-a[0])*i/n,a[1]+(b[1]-a[1])*i/n) for i in range(n)]
        path=[];seen_p=set()
        for y,x in dense:
            p=(int(round(y)),int(round(x)))
            if p not in seen_p:seen_p.add(p);path.append(p)
        # 바다에 닿은 뒤 다시 땅으로 들어가면 거기서 자른다
        out=[];at_sea=False
        for p in path:
            if sea[p]:at_sea=True
            elif at_sea:break
            out.append(p)
        if not at_sea:raise ValueError('강이 바다에 닿지 않음: '+r['label'])
        n=len(out)
        for i,(y,x) in enumerate(out):
            rad=.75+1.9*(i/n)**1.3   # 상류 좁게 → 하구 넓게
            rr=int(math.ceil(rad))
            for dy in range(-rr,rr+1):
                for dx in range(-rr,rr+1):
                    if dy*dy+dx*dx<=rad*rad and 0<=y+dy<H and 0<=x+dx<W and owned[y+dy,x+dx]:river[y+dy,x+dx]=True
        river_info.append(dict(label=r['label'],cells=n,mouth=[out[-1][1]+X0,out[-1][0]+Y0]))
    river&=owned
    d_river=ndimage.distance_transform_edt(~river)
    elev=elev-.22*np.exp(-(d_river/6)**2)   # 강 골짜기
    elev=(elev-elev[land].min())/(elev[land].max()-elev[land].min())

    # 3) 지형 종류
    t=np.full((H,W),VOID,int)
    to_land=ndimage.distance_transform_edt(~land_full)
    t[sea]=DEEP;t[sea&(to_land<=3)]=SHALLOW
    forest_n=noise((H,W),16,rng,4);flower=noise((H,W),8,rng,2);marsh_n=noise((H,W),6,rng,2)
    L=land.copy();q=lambda v:float(np.quantile(elev[L],v))
    t[L]=GRASS
    t[L&(flower>.68)]=MEADOW
    t[L&(elev>q(.80))]=HILL
    t[L&(elev>q(.25))&(elev<q(.95))&(forest_n>.54)]=FOREST
    t[L&(elev>q(.90))&(forest_n>.40)]=FOREST
    t[L&(elev>q(.935))]=MOUNTAIN
    t[L&(elev>q(.965))]=PEAK
    # 들판: 마을 가까운 낮은 강가 평지. 장단(곡창)은 넓게
    near=np.zeros((H,W),bool);yy,xx=np.ogrid[:H,:W]
    for town in layout.data['towns']:
        ty,tx=idx(int(town['position'][0]),int(town['position'][1]));rr=44 if town['key']=='goryeo/장단' else 26
        near|=((yy-ty)**2+(xx-tx)**2)<rr*rr
    t[L&near&(elev<q(.40))&(d_river<10)&(forest_n<.6)&~np.isin(t,[MOUNTAIN,PEAK])]=FIELD
    # 해안: 낮으면 모래, 높으면 바위
    coast=L&(to_sea<=2)
    beach_n=noise((H,W),12,rng,2)
    t[coast&(elev<.42)&(beach_n>.42)]=SAND
    t[coast&(elev>=.55)&(to_sea<=1)]=ROCKCOAST
    # 하구 습지
    mouth_zone=np.zeros((H,W),bool)
    for ri in river_info:
        my,mx=idx(*ri['mouth']);yy,xx=np.ogrid[:H,:W];mouth_zone|=((yy-my)**2+(xx-mx)**2)<16**2
    t[L&mouth_zone&(d_river<=5)&(d_river>0)&(elev<.35)&(marsh_n>.45)]=MARSH
    t[river]=RIVER

    # 4) 마을 범위: 마을 칸에서 평평하고 지나갈 수 있는 땅으로 넓혀 목표 면적을 채운다
    village=np.zeros((H,W),bool);village_info=[]
    shape_n=noise((H,W),7,rng,2)
    towns=[dict(key=tw['key'],label=tw.get('label'),position=tw['position']) for tw in layout.data['towns']]
    for mkey,label,area in VARIANT['hamlets']:
        k=keys.index(mkey);cand=np.argwhere((mapid==k)&land&~np.isin(t,list(BLOCKED)))
        cy,cx=cand.mean(0);c=cand[np.argmin(((cand-[cy,cx])**2).sum(1))]
        towns.append(dict(key='hamlet/'+label,label=label,position=[float(c[1]+X0),float(c[0]+Y0)],area=area))
    for town in towns:
        key=town['key'];x,y=town['position'];s=idx(int(math.floor(x)),int(math.floor(y)))
        if not land[s] or t[s] in BLOCKED:
            # 강·산 위면 가장 가까운 평지로
            cand=np.argwhere(land&~np.isin(t,list(BLOCKED))&(mapid==mapid[s]))
            s=tuple(cand[np.argmin(((cand-np.array(s))**2).sum(1))])
        mine=mapid==mapid[s];target=town.get('area') or VARIANT['area'].get(key) or VILLAGE_AREA[key]
        got={s};q=[(0.0,s)];seen={s}
        while q and len(got)<target:
            _,p=heapq.heappop(q);got.add(p)
            for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):
                n=(p[0]+dy,p[1]+dx)
                if n in seen or not(0<=n[0]<H and 0<=n[1]<W):continue
                seen.add(n)
                if not mine[n] or not land[n] or t[n] in (PEAK,MOUNTAIN,ROCKCOAST):continue
                if t[n]==RIVER:continue  # 강은 마을을 지나가도 강으로 둔다(다리는 길 단계)
                d=math.hypot(n[0]-s[0],n[1]-s[1])
                heapq.heappush(q,(d*(1+.55*(shape_n[n]-.5))+6*max(0,elev[n]-.45),n))
        for p in got:village[p]=True
        village_info.append(dict(key=key,label=town.get('label') or key.split('/')[1],area=len(got),target=target,center=[int(s[1]+X0),int(s[0]+Y0)]))
    village=ndimage.binary_closing(village,iterations=2)&land&~river&(t!=PEAK)&(t!=MOUNTAIN)
    # 마을 안은 평평하게: 숲·언덕을 풀밭으로(들판·해변·습지는 그대로 둔다)
    t[village&np.isin(t,[FOREST,HILL])]=GRASS
    for v in village_info:
        mk=v['key'] if v['key'] in keys else next(m for m,l,a in VARIANT['hamlets'] if 'hamlet/'+l==v['key'])
        v['area']=int(((mapid==keys.index(mk))&village).sum())
    return dict(t=t,elev=elev,owned=owned,land=land,sea=sea,river=river,village=village,mapid=mapid,keys=keys,
                rivers=river_info,villages=village_info)


VIA={frozenset(('east/동문 마을','east/북동 포구')):'east/북동 길',frozenset(('east/동문 마을','east/남동 포구')):'east/남동 길',
     frozenset(('east/북동 포구','east/중앙 길목 마을')):'east/북쪽 내륙길',frozenset(('east/남동 포구','east/중앙 길목 마을')):'east/남쪽 내륙길'}
ROAD_COST={GRASS:1.0,MEADOW:1.0,VILLAGE:.7,FIELD:1.25,SAND:1.7,HILL:2.6,FOREST:3.4,MARSH:5.0,RIVER:22.0,MOUNTAIN:14.0}


def smooth_path(raw,blocked,water=None,rounds=4):
    """A* 결과의 꺾인 직선을 부드러운 곡선으로. 막힌 칸에 걸리면 그 부분은 원래 경로를 쓴다."""
    if len(raw)<5:return raw
    pts=[(float(y),float(x)) for y,x in raw[::3]]
    if pts[-1]!=(float(raw[-1][0]),float(raw[-1][1])):pts.append((float(raw[-1][0]),float(raw[-1][1])))
    for _ in range(rounds):
        new=[pts[0]]
        for a,b in zip(pts,pts[1:]):new+=[(a[0]*.75+b[0]*.25,a[1]*.75+b[1]*.25),(a[0]*.25+b[0]*.75,a[1]*.25+b[1]*.75)]
        new.append(pts[-1]);pts=new
    out=[]
    for a,b in zip(pts,pts[1:]):
        n=max(1,int(math.hypot(b[0]-a[0],b[1]-a[1])*3))
        for i in range(n):
            p=(int(round(a[0]+(b[0]-a[0])*i/n)),int(round(a[1]+(b[1]-a[1])*i/n)))
            if not out or out[-1]!=p:out.append(p)
    out.append(raw[-1])
    if any(blocked[p] for p in out):return raw
    if water is not None and sum(water[p] for p in out)>sum(water[p] for p in raw):return raw  # 곡선이 강 위로 올라가면 원래 경로
    # 4방향으로 이어지게 빈틈 메우기(대각선만 닿는 칸 사이)
    filled=[out[0]]
    for p in out[1:]:
        q=filled[-1]
        if abs(p[0]-q[0])==1 and abs(p[1]-q[1])==1:
            mid=(q[0],p[1]) if not blocked[q[0],p[1]] else (p[0],q[1])
            filled.append(mid)
        filled.append(p)
    return filled


def roads(layout,w):
    t=w['t'];rng=np.random.default_rng(SEED+1)
    # 사람이 다니며 생긴 길: 완만한 땅을 따라 돌아가도록 넓은 기복(경사)과 큰 노이즈를 비용에 넣는다
    wig=noise((H,W),26,rng,3);fine=noise((H,W),8,rng,2)
    gy,gx=np.gradient(ndimage.gaussian_filter(w['elev'],1.5));slope=np.hypot(gy,gx);slope/=slope[w['land']].max()
    cost=np.full((H,W),1e3);blocked=np.ones((H,W),bool)
    for k,v in ROAD_COST.items():
        m=t==k;cost[m]=v;blocked[m]=False
    cost=cost*(1+1.1*wig**2+.25*fine)+6*slope
    centers={v['key']:(v['center'][1]-Y0,v['center'][0]-X0) for v in w['villages']}
    road=np.zeros((H,W),bool);records=[]
    order=sorted(layout.data['roads'],key=lambda r:math.dist(layout.towns[r['fromKey']]['position'],layout.towns[r['toKey']]['position']))
    for r in order:
        c=cost.copy();c[road]*=.7   # 이미 난 길은 조금 싸다: 가까우면 합류해 갈림길이 생김
        via=VIA.get(frozenset((r['fromKey'],r['toKey'])))
        hamlet=next((v for v in w['villages'] if v['key'].startswith('hamlet/') and via and next(m for m,l,a in VARIANT['hamlets'] if 'hamlet/'+l==v['key'])==via),None)
        if hamlet:
            mid=(hamlet['center'][1]-Y0,hamlet['center'][0]-X0)
            raw=astar(centers[r['fromKey']],mid,c,blocked)+astar(mid,centers[r['toKey']],c,blocked)[1:]
        elif via:
            # 이동 구역 맵을 지나는 길은 그 맵 가운데를 지나게(그 맵에 길이 없으면 이동 구역이 비게 됨)
            k=w['keys'].index(via);cand=np.argwhere((w['mapid']==k)&~blocked)
            cy,cx=cand.mean(0);mid=tuple(cand[np.argmin(((cand-[cy,cx])**2).sum(1))])
            raw=astar(centers[r['fromKey']],mid,c,blocked)+astar(mid,centers[r['toKey']],c,blocked)[1:]
        else:raw=astar(centers[r['fromKey']],centers[r['toKey']],c,blocked)
        path=smooth_path(raw,blocked,t==RIVER)
        for p in path:road[p]=True
        bridges=sum(1 for p in path if t[p]==RIVER);passes=sum(1 for p in path if t[p]==MOUNTAIN)
        maps_on=sorted({w['keys'][w['mapid'][p]] for p in path if w['mapid'][p]>=0})
        records.append(dict(fromKey=r['fromKey'],toKey=r['toKey'],cells=len(path),bridgeCells=bridges,mountainPassCells=passes,maps=maps_on))
    cross=np.array([[0,1,0],[1,1,1],[0,1,0]])
    wide=ndimage.binary_dilation(road,structure=cross)&w['owned']&~np.isin(t,[DEEP,SHALLOW,PEAK,MOUNTAIN])
    # 강 위로는 실제로 건너는 곳(다리)만 넓힌다. 강가를 따라가는 길이 물 위로 번지지 않게
    river=t==RIVER;crossing=ndimage.binary_dilation(road&river,structure=cross,iterations=2)
    wide&=~river|crossing
    return dict(road=wide,center=road,records=records)


def render(w,path,road=None,scale=4,title=''):
    t=w['t'];elev=w['elev']
    rgb=np.zeros((H,W,3),float)
    for k,c in COLORS.items():rgb[t==k]=c
    # 언덕 그림자(북서쪽 빛)
    gy,gx=np.gradient(ndimage.gaussian_filter(elev,1.2))
    shade=np.clip(1+(-gx-gy)*6,0.72,1.22)
    landish=w['land']&(t!=RIVER)
    rgb[landish]*=shade[landish,None]
    if road is not None:
        rgb[road['road']&(t==RIVER)]=(150,110,70)          # 다리
        rgb[road['road']&(t!=RIVER)]=(198,168,120)
    # 마을 범위: 옅게 덮고 테두리
    vm=w['village'];rgb[vm]=rgb[vm]*.7+np.array((255,236,170))*.3
    edge=vm&~ndimage.binary_erosion(vm)
    rgb[edge]=(255,226,110)
    # 맵 경계
    mid=w['mapid'];b=np.zeros((H,W),bool)
    b[:,1:]|=(mid[:,1:]!=mid[:,:-1])&(mid[:,1:]>=0)&(mid[:,:-1]>=0);b[1:,:]|=(mid[1:,:]!=mid[:-1,:])&(mid[1:,:]>=0)&(mid[:-1,:]>=0)
    rgb[b]=rgb[b]*.45
    ys,xs=np.nonzero(w["owned"]);y0,y1,x0,x1=max(0,ys.min()-3),min(H,ys.max()+4),max(0,xs.min()-3),min(W,xs.max()+4)
    img=Image.fromarray(np.clip(rgb[y0:y1,x0:x1],0,255).astype(np.uint8)).resize(((x1-x0)*scale,(y1-y0)*scale),Image.NEAREST)
    d=ImageDraw.Draw(img);f=ImageFont.truetype(str(FONT),14);fs=ImageFont.truetype(str(FONT),11)
    for v in w['villages']:
        cx,cy=(v['center'][0]-X0-x0)*scale,(v['center'][1]-Y0-y0)*scale
        d.text((cx-len(v['label'])*7,cy-8),v['label'],font=f,fill=(30,24,20),stroke_width=2,stroke_fill=(255,250,230))
    # 범례
    items=[SHALLOW,SAND,ROCKCOAST,RIVER,MARSH,FIELD,GRASS,MEADOW,HILL,FOREST,MOUNTAIN,PEAK]
    lx,ly=8,img.height-16*len(items)-30
    d.rectangle((lx-4,ly-24,lx+150,img.height-6),fill=(250,248,240))
    d.text((lx,ly-20),title,font=fs,fill=(30,30,30))
    for i,k in enumerate(items):
        d.rectangle((lx,ly+i*16,lx+12,ly+i*16+12),fill=COLORS[k]);d.text((lx+18,ly+i*16-1),NAMES[k],font=fs,fill=(30,30,30))
    i=len(items);d.rectangle((lx,ly+i*16,lx+12,ly+i*16+12),outline=(230,200,90),width=2);d.text((lx+18,ly+i*16-1),'마을 범위',font=fs,fill=(30,30,30))
    img.save(path)


def main():
    ap=argparse.ArgumentParser();ap.add_argument('--roads',action='store_true')
    ap.add_argument('--variant',choices=['current','small','small-hamlets'],default='current');a=ap.parse_args()
    if a.variant!='current':VARIANT['area']=SMALL_CENTRAL
    if a.variant=='small-hamlets':VARIANT['hamlets']=HAMLETS
    sfx='' if a.variant=='current' else '-'+a.variant
    layout=WorldLayout.read();w=build(layout);OUT.mkdir(parents=True,exist_ok=True)
    # 검사: 해안선·면적 불변, 마을 칸 통행 가능
    land_before=sum(1 for c in layout.land if c in layout.owners)
    assert int(w['land'].sum())==land_before,'육지 면적이 바뀜'
    counts={NAMES[k]:int((w['t']==k).sum()) for k in range(1,15)}
    render(w,OUT/f'terrain-villages{sfx}.png',title='M1 지형 v3 + 마을 범위'+sfx)
    report=dict(seed=SEED,landCells=land_before,classCounts=counts,rivers=w['rivers'],villages=w['villages'])
    if a.roads:
        r=roads(layout,w);render(w,OUT/f'terrain-villages-roads{sfx}.png',road=r,title='M1 지형 v3 + 마을 + 길'+sfx)
        report['roads']=r['records'];report['roadCells']=int(r['road'].sum())
    (OUT/f'report{sfx}.json').write_text(json.dumps(report,ensure_ascii=False,indent=1))
    print(json.dumps(report,ensure_ascii=False)[:3000])


if __name__=='__main__':main()
