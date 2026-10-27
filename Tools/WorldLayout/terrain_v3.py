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
VOID,DEEP,SHALLOW,SAND,ROCKCOAST,RIVER,MARSH,FIELD,GRASS,MEADOW,HILL,FOREST,MOUNTAIN,PEAK,VILLAGE,LAKE=range(16)
NAMES=['밖','깊은 바다','얕은 바다','모래 해변','바위 해안','강','습지','들판(밭)','풀밭','꽃풀밭','언덕','숲','산(통행 불가)','봉우리(통행 불가)','마을 범위','호수']
COLORS={DEEP:(52,98,140),SHALLOW:(96,156,186),SAND:(236,222,170),ROCKCOAST:(150,142,128),RIVER:(84,146,196),
        MARSH:(96,128,96),FIELD:(204,176,84),GRASS:(118,164,88),MEADOW:(142,182,98),HILL:(134,150,86),
        FOREST:(52,98,56),MOUNTAIN:(128,114,96),PEAK:(184,178,170),VILLAGE:(210,190,140),VOID:(28,34,40),LAKE:(70,136,186)}
BLOCKED={DEEP,SHALLOW,RIVER,MOUNTAIN,PEAK,ROCKCOAST,LAKE}
# 호수(세계 좌표 중심, 대략 면적): 남쪽 쉼터 마을 북쪽(2026-10-27 은샘 제안)
LAKES=[dict(label='남쪽 큰 호수',area=950,box=(95,22,150,72))]   # 이 범위(x0,y0,x1,y1) 안에서 가장 낮은 내륙 분지를 고른다
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


VIA={frozenset(('east/동문 마을','east/북동 포구')):'east/북동 길',frozenset(('east/동문 마을','east/남동 포구')):'east/남동 길',
     frozenset(('east/북동 포구','east/중앙 길목 마을')):'east/북쪽 내륙길',frozenset(('east/남동 포구','east/중앙 길목 마을')):'east/남쪽 내륙길'}
# 검토안 선택: 중앙 마을 크기와 추가 쉼터 마을
VARIANT={'area':{},'hamlets':[]}
SMALL_CENTRAL={'east/동문 마을':750,'east/북동 포구':800,'east/남동 포구':800,'east/중앙 길목 마을':850}
HAMLETS=[('east/북쪽 내륙길','북쪽 쉼터 마을',320),('east/남쪽 내륙길','남쪽 호숫가 마을',320)]


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
    # 호수: 중앙 동쪽 남부에서 가장 낮은 내륙 분지를 찾아, 낮은 땅부터 물을 채우듯 넓힌다(호숫가가 등고선을 따름).
    #       둘레 언덕에서 흘러드는 개울 하나, 가장 낮은 물가에서 바다로 빠지는 개울 하나를 만든다.
    lake=np.zeros((H,W),bool);E=ndimage.gaussian_filter(elev,3)
    east=np.zeros((H,W),bool)
    for i,k in enumerate(keys):
        if k.startswith('east/'):east|=mapid==i
    yy_,xx_=np.ogrid[:H,:W];far=np.ones((H,W),bool)
    for town in layout.data['towns']:
        ty,tx=idx(int(town['position'][0]),int(town['position'][1]));far&=((yy_-ty)**2+(xx_-tx)**2)>22**2
    inner=ndimage.binary_erosion(east,iterations=6)   # 담당 경계에 잘린 직선 호숫가가 생기지 않게
    for lk in LAKES:
        bx0,by0,bx1,by1=lk['box'];south=((xx_+X0)>=bx0)&((xx_+X0)<bx1)&((yy_+Y0)>=by0)&((yy_+Y0)<by1)
        # 범위 안쪽(가장자리에서 10칸 이상)에서 가장 낮은 곳을 중심으로
        core=south&inner&land&far&(to_sea>14)&(d_river>6)&~np.isin(t,[MOUNTAIN,PEAK])
        core&=((xx_+X0)>=bx0+10)&((xx_+X0)<bx1-10)&((yy_+Y0)>=by0+10)&((yy_+Y0)<by1-10)
        seed=np.unravel_index(int(np.argmin(np.where(core,E,np.inf))),E.shape)
        # 모양: 길쭉한 타원에 만·곶을 만드는 굴곡(각도에 따른 낮은 주파수 사인 합)
        r0=math.sqrt(lk['area']/math.pi);asp=1.55;ang=rng.uniform(0,math.pi)
        ph=rng.uniform(0,2*math.pi,4);amp=(.16,.10,.06,.04)
        dy_=yy_-seed[0];dx_=xx_-seed[1]
        u=dx_*math.cos(ang)+dy_*math.sin(ang);v=-dx_*math.sin(ang)+dy_*math.cos(ang)
        # 좌표를 노이즈로 비틀어(도메인 워핑) 만·곶이 여러 크기로 생기게 하고, 낮은 땅일수록 물이 더 들어오게
        wy=noise((H,W),11,rng,3)-.5;wx=noise((H,W),11,rng,3)-.5;frac=noise((H,W),6,rng,4)-.5
        u=u+13*wx;v=v+13*wy
        theta=np.arctan2(v,u);rr_=np.sqrt((u/math.sqrt(asp))**2+(v*math.sqrt(asp))**2)
        wob=1+sum(a_*np.sin((k_+2)*theta+ph[k_]) for k_,a_ in enumerate(amp))
        En=(E-E[seed])/(np.ptp(E[land])+1e-9)
        field=rr_/(r0*wob)+1.6*En+.32*frac
        # 목표 면적이 되도록 문턱값을 찾는다
        cand_=land&inner&~river
        # 원형 잘림(직선 호숫가)이 생기지 않게 거리 제한은 넉넉히 두고, 먼 곳은 부드럽게 불리하게
        dist_=np.sqrt(dy_**2+dx_**2);field=field+np.clip((dist_-r0*1.8)/(r0*.8),0,None)**2
        reach=cand_&(dist_<r0*3.4)
        vals=np.sort(field[reach])
        thr=vals[min(len(vals)-1,lk['area'])]
        m=(field<thr)&reach
        # 물가를 둥글게 다듬는다(계단·직선 모서리 제거)
        shore_n=noise((H,W),7,rng,3)-.5   # 물가에 작은 만·곶
        m=((ndimage.gaussian_filter(m.astype(float),2.2)+.55*shore_n)>.5)&cand_
        m=ndimage.binary_closing(ndimage.binary_opening(m),iterations=1)&cand_
        lab,n_=ndimage.label(m)
        if n_>1:m=lab==(1+int(np.argmax(ndimage.sum(np.ones_like(lab),lab,range(1,n_+1)))))
        # 호수 안의 땅: 작은 것은 메우고 큰 것(작은 섬)은 남긴다
        holes=ndimage.binary_fill_holes(m)&~m;hl,hn=ndimage.label(holes)
        for j in range(1,hn+1):
            if (hl==j).sum()<10:m|=hl==j
        lake|=m;lk['cells']=int(m.sum());lk['center']=[int(seed[1]+X0),int(seed[0]+Y0)]
        cost=1+10*elev
        # 나가는 개울: 가장 낮은 물가 → 가장 가까운 바다
        rim=ndimage.binary_dilation(m)&~m&land
        r0=tuple(np.argwhere(rim)[np.argmin(E[rim])])
        sy,sx=np.nonzero(sea);k_=int(np.argmin((sy-r0[0])**2+(sx-r0[1])**2))
        out=astar(r0,(int(sy[k_]),int(sx[k_])),cost,blocked=np.isin(t,[MOUNTAIN,PEAK]),eight=True)
        # 호수가 성립하는 지형: 큰 호수는 둘레가 높은 분지여야 물이 고인다. 출구(바다 쪽) 한 곳만 낮게 두고
        # 나머지 둘레에 언덕·숲을 세워 집수 지역을 만들고, 그 언덕에서 개울 둘이 흘러든다.
        cy,cx=np.argwhere(m).mean(0);oy,ox=r0
        dl=ndimage.distance_transform_edt(~m)
        ang_out=math.atan2(oy-cy,ox-cx);ang=np.arctan2(yy_-cy,xx_-cx)
        gap=np.abs((ang-ang_out+np.pi)%(2*np.pi)-np.pi)<math.radians(38)   # 출구 쪽은 열어 둔다
        ringz=(dl>=3)&(dl<=16)&~gap&east&land&~river&far
        bump=np.exp(-((dl-8)/4.5)**2)*(.7+.6*forest_n)
        hillring=ringz&(bump>.55)
        t[hillring&(forest_n>.42)]=FOREST;t[hillring&(forest_n<=.42)]=HILL
        elev=np.where(ringz,np.maximum(elev,np.quantile(elev[land],.82)*bump+elev*(1-bump)),elev)
        lk['hillCells']=int(hillring.sum())
        # 수원지: 출구 반대편에 작은 산 하나(봉우리 + 산자락 숲)를 두고,
        # 그 산 높은 비탈에서 시작한 개울 1줄기가 호수로 든다.
        R_l=math.sqrt(m.sum()/math.pi);back=ang_out+math.pi
        ctr_=None
        for dist_ in (R_l+21,R_l+19,R_l+23,R_l+17):
            mcx,mcy=cx+math.cos(back)*dist_,cy+math.sin(back)*dist_
            iy,ix=int(round(mcy)),int(round(mcx))
            if 0<=iy<H and 0<=ix<W and east[iy,ix] and land[iy,ix] and far[iy,ix] and not m[iy,ix]:
                ctr_=(mcy,mcx);break
        inflow=[]
        if ctr_:
            mcy,mcx=ctr_
            # 둥근 원뿔이 되지 않게: 한쪽으로 조금 길쭉하고(능선 방향은 무작위), 좌표를 비틀어 산자락이 불규칙하게 퍼진다
            wob_=noise((H,W),5,rng,3);wy2=noise((H,W),9,rng,3)-.5;wx2=noise((H,W),9,rng,3)-.5
            a_m=rng.uniform(0,math.pi);du=(xx_-mcx)+6*wx2;dv=(yy_-mcy)+6*wy2
            pu=du*math.cos(a_m)+dv*math.sin(a_m);pv=-du*math.sin(a_m)+dv*math.cos(a_m)
            dr_=np.hypot(pu/1.45,pv*1.1)*(1+.35*(wob_-.5))
            prof=np.exp(-(dr_/4.5)**2)+.45*np.exp(-(dr_/9)**2)
            zone=(dr_<11)&east&land&~m&~river
            # 산자락 높이도 노이즈로 흔들어 동심원 무늬가 보이지 않게
            elev=np.where(zone,np.maximum(elev,.55+.45*prof*(.8+.4*wob_)),elev)
            t[zone&(dr_<9.5)&(forest_n>.33)]=FOREST;t[zone&(dr_<9.5)&(forest_n<=.33)]=HILL
            t[zone&(dr_<5.2)]=MOUNTAIN;t[zone&(dr_<2.4)]=PEAK
            lk['sourceMountain']=[round(mcx+X0,1),round(mcy+Y0,1)]
            # 개울 1줄기: 산의 호수 쪽 비탈, 산 바로 바깥의 가장 높은 곳에서 시작
            lakeward=((yy_-mcy)*(cy-mcy)+(xx_-mcx)*(cx-mcx))>0
            cand_=np.argwhere(zone&lakeward&~np.isin(t,[MOUNTAIN,PEAK])&(dr_<7.5))
            if len(cand_):
                h0=tuple(cand_[np.argmax(elev[tuple(cand_.T)])])
                mc=np.argwhere(m);m0=tuple(mc[np.argmin(((mc-np.array(h0))**2).sum(1))])
                seg=astar(h0,m0,1+12*elev,blocked=np.isin(t,[MOUNTAIN,PEAK]),eight=True)
                inflow.append(seg)
                yy3,xx3=m0;t[(((yy_-yy3)**2+(xx_-xx3)**2)<3.5**2)&land&~m&(marsh_n>.35)]=MARSH
        def meander(path,amp_=2.6,lam_=13):
            # A* 길을 따라가며 옆으로 흔들어 굽이치게(땅 위에서만)
            if len(path)<8:return path
            out_=[];n=len(path);phs=rng.uniform(0,6.28)
            for i_,(y,x) in enumerate(path):
                a0=path[max(0,i_-3)];a1=path[min(n-1,i_+3)];ty,tx=a1[0]-a0[0],a1[1]-a0[1];L_=math.hypot(ty,tx) or 1
                env=math.sin(math.pi*i_/(n-1))
                o=env*amp_*math.sin(2*math.pi*i_/lam_+phs)
                q=(int(round(y-tx/L_*o)),int(round(x+ty/L_*o)))
                out_.append(q if 0<=q[0]<H and 0<=q[1]<W and (land[q] or sea[q]) and not m[q] else (y,x))
            return smooth_path(out_,np.zeros((H,W),bool))
        for path,width in [(out,1.2)]+[(q_,.8) for q_ in inflow]:
            pts=meander(path)
            for i_,(y,x) in enumerate(pts):
                rad=width*(.7+.5*i_/max(1,len(pts))) if path is out else width
                rr=int(math.ceil(rad))
                for dy in range(-rr,rr+1):
                    for dx in range(-rr,rr+1):
                        yy2,xx2=y+dy,x+dx
                        if dy*dy+dx*dx<=rad*rad and 0<=yy2<H and 0<=xx2<W and land[yy2,xx2] and not m[yy2,xx2]:river[yy2,xx2]=True
        lk['outletCells']=len(out);lk['inflowCells']=[len(q_) for q_ in inflow]
    t[river]=RIVER
    d_river=ndimage.distance_transform_edt(~(river|lake))
    shore=ndimage.binary_dilation(lake,iterations=2)&land&~lake&~river
    t[shore&(marsh_n>.55)]=MARSH;t[shore&(marsh_n<.25)]=SAND
    t[lake]=LAKE

    # 4) 마을 자리와 범위(v4): 승인 위치 가까이에서 마을이 들어서기 좋은 땅을 고르고, 둥글고 자연스러운 모양으로 넓힌다
    region_of=np.full((H,W),'',object)
    for i,k in enumerate(keys):region_of[mapid==i]=k.split('/')[0]
    flat=ndimage.generic_filter(elev,np.std,size=5)
    passable=land&~np.isin(t,list(BLOCKED))
    towns=[dict(key=tw['key'],label=tw.get('label') or tw['key'].split('/')[1],position=tw['position']) for tw in layout.data['towns']]
    byk={tw['key']:tw for tw in towns}
    for mkey,label,area in VARIANT['hamlets']:
        pair=next(kk for kk,v in VIA.items() if v==mkey);a1,b1=sorted(pair)
        pa,pb=byk[a1]['position'],byk[b1]['position']
        towns.append(dict(key='hamlet/'+label,label=label,position=[(pa[0]+pb[0])/2,(pa[1]+pb[1])/2],area=area,hamlet=mkey))
    COAST={'east/북동 포구','east/남동 포구','goryeo/나루','goryeo/개경'}
    RIVERSIDE={'goryeo/개경','goryeo/장단','goryeo/나루'}
    yy,xx=np.ogrid[:H,:W]
    village=np.zeros((H,W),bool);village_info=[];vlabel=np.full((H,W),-1,int)
    shape_n=noise((H,W),9,rng,2)
    for vi,town in enumerate(towns):
        key=town['key'];x,y=town['position'];oy,ox=idx(int(math.floor(x)),int(math.floor(y)))
        reg='east' if key.startswith(('east','hamlet')) else 'goryeo'
        R=20 if key in COAST or key.startswith('hamlet') else 12
        near=((yy-oy)**2+(xx-ox)**2)<=R*R
        cand=passable&near&(region_of==reg)&~village
        if key.startswith('hamlet'):cand&=(mapid==keys.index(town['hamlet']))
        score=4*flat+1.2*elev+.02*np.sqrt((yy-oy)**2+(xx-ox)**2)+.6*(t==FOREST)+.8*(t==MARSH)
        if key in COAST:score+=.12*np.abs(to_sea-5)          # 바다에서 5칸쯤: 포구·나루
        if key in RIVERSIDE or key.startswith('hamlet'):score+=.05*np.minimum(d_river,20)  # 물가
        score=np.where(cand,score,np.inf);s=np.unravel_index(int(np.argmin(score)),score.shape)
        target=town.get('area') or VARIANT['area'].get(key) or VILLAGE_AREA[key]
        got={s};q=[(0.0,s)];seen={s};okreg=(region_of==reg)
        while q and len(got)<target:
            _,p=heapq.heappop(q);got.add(p)
            for dy,dx in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)):
                n=(p[0]+dy,p[1]+dx)
                if n in seen or not(0<=n[0]<H and 0<=n[1]<W):continue
                seen.add(n)
                if not okreg[n] or not land[n] or t[n] in (PEAK,MOUNTAIN,ROCKCOAST,RIVER,LAKE) or village[n]:continue
                d=math.hypot(n[0]-s[0],n[1]-s[1])
                heapq.heappush(q,(d*(1+.35*(shape_n[n]-.5))+5*max(0,elev[n]-.5)+2*flat[n],n))
        m=np.zeros((H,W),bool)
        for p in got:m[p]=True
        m=ndimage.binary_closing(ndimage.binary_opening(m,iterations=1),iterations=2)&land&~river&~np.isin(t,[PEAK,MOUNTAIN,LAKE])
        village|=m;vlabel[m]=vi
        village_info.append(dict(key=key,label=town['label'],target=target,center=[int(s[1]+X0),int(s[0]+Y0)],
                                 moved=round(math.hypot(s[0]-oy,s[1]-ox),1)))
    t[village&np.isin(t,[FOREST,HILL])]=GRASS

    for v,tw in zip(village_info,towns):
        v['area']=int((vlabel==towns.index(tw)).sum())
    return dict(t=t,elev=elev,owned=owned,land=land,sea=sea,river=river,village=village,mapid=mapid,keys=keys,
                rivers=river_info,villages=village_info,maps=[],towns=towns,lake=lake,lakes=LAKES,vlabel=vlabel,region_of=region_of,passable=passable)


def partition(layout,w,road=None):
    t=w['t'];sea=w['sea'];owned=w['owned'];keys=w['keys'];towns=w['towns'];vlabel=w['vlabel'];region_of=w['region_of']
    passable=w['passable'];village=w['village'];village_info=w['villages']
    # 5) 맵 경계(v4): 마을(이동 구역은 쉼터·중간 지점)을 씨앗으로, 지형을 따라 퍼지는 거리로 땅을 나눈다.
    #    강·산을 건너는 비용을 크게 해 경계가 강·산줄기·숲 가장자리를 따르게 하고, 면적은 원래 목표에 맞춘다.
    # 경계는 길과 마을 기준(2026-10-28 은샘): 지형과 상관없이 같은 거리로 퍼지되 길 위는 빨리 퍼진다.
    # 그래서 맵은 마을과 그 마을로 이어지는 길을 중심으로 잡히고, 경계는 두 마을 사이 길의 중간에서 길을 가로지른다.
    rinfo=road;road=road['center'] if isinstance(road,dict) else road
    step_cost=np.ones((H,W))
    if road is not None:step_cost[road]=.35
    targets={k:layout.maps[k]['area'] for k in keys}
    seeds={}
    for i,k in enumerate(keys):
        v=next((j for j,tw in enumerate(towns) if tw['key']==k or tw.get('hamlet')==k),None)
        if v is not None:seeds[k]=vlabel==v
        else:
            pair=next(kk for kk,vv in VIA.items() if vv==k)
            sm=np.zeros((H,W),bool)
            if isinstance(rinfo,dict):
                # 이동 구역 맵은 그 구간 길의 가운데 부분(마을 밖)을 씨앗으로
                for rec,path in zip(rinfo['records'],rinfo['paths']):
                    if frozenset((rec['fromKey'],rec['toKey']))==pair:
                        outside=[p_ for p_ in path if not village[p_]];n_=len(outside)
                        for p_ in outside[int(n_*.3):int(n_*.7)+1]:sm[p_]=True
            if not sm.any():
                a1,b1=sorted(pair)
                ca=village_info[[tw['key'] for tw in towns].index(a1)]['center'];cb=village_info[[tw['key'] for tw in towns].index(b1)]['center']
                my,mx=idx(int((ca[0]+cb[0])/2),int((ca[1]+cb[1])/2))
                c=np.argwhere(passable&(region_of=='east')&~village);c=c[np.argmin(((c-[my,mx])**2).sum(1))]
                sm[max(0,c[0]-2):c[0]+3,max(0,c[1]-2):c[1]+3]=True
            seeds[k]=sm&owned
    def geodesic(seed):
        dist=np.full((H,W),np.inf);q=[]
        for p in zip(*np.nonzero(seed)):dist[p]=0;q.append((0.0,p))
        heapq.heapify(q)
        while q:
            g,p=heapq.heappop(q)
            if g!=dist[p]:continue
            for dy,dx in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)):
                n=(p[0]+dy,p[1]+dx)
                if not(0<=n[0]<H and 0<=n[1]<W) or not owned[n]:continue
                v=g+math.hypot(dy,dx)*(step_cost[p]+step_cost[n])/2
                if v<dist[n]:dist[n]=v;heapq.heappush(q,(v,n))
        return dist
    D=np.stack([geodesic(seeds[k]) for k in keys])
    regmask=np.stack([region_of==k.split('/')[0] for k in keys])
    wgt=np.zeros(len(keys))
    for it in range(250):
        score=np.where(regmask,D+wgt[:,None,None],np.inf);new=np.argmin(score,0);new[~owned]=-1
        areas=np.array([(new==i).sum() for i in range(len(keys))])
        # 면적은 자연 경계를 우선하고, 너무 작거나 큰 맵만 고친다(2,600~5,400칸)
        lo,hi=2600,5400
        err=np.where(areas<lo,(areas-lo)/lo,np.where(areas>hi,(areas-hi)/hi,0))
        if not np.any(err):break
        wgt+=err*(40/(1+it*.04))
    # 경계 다듬기: 같은 지역 안에서 주변 다수 맵으로(톱니 제거)
    for _ in range(3):
        cnt=np.stack([ndimage.uniform_filter((new==i).astype(float),size=5) for i in range(len(keys))])
        cnt=np.where(regmask,cnt,-1);sm=np.argmax(cnt,0);sm[~owned]=-1;new=sm
    # 마을 둘레 여유: 마을 범위에서 10칸 안은 그 마을 맵(경계가 마을에 바짝 붙지 않게)
    MARGIN=10;best=np.full((H,W),np.inf);own=np.full((H,W),-1)
    for i,k in enumerate(keys):
        if not any(tw['key']==k or tw.get('hamlet')==k for tw in towns):continue
        dd=ndimage.distance_transform_edt(~seeds[k])
        z=(dd<=MARGIN)&regmask[i]&owned&(dd<best);best[z]=dd[z];own[z]=i
    new[own>=0]=own[own>=0]
    # 마을은 반드시 자기 맵 안에
    for i,k in enumerate(keys):new[seeds[k]]=i
    # 작은 떨어진 조각은 둘러싼 맵으로
    for i in range(len(keys)):
        lab,n=ndimage.label(new==i)
        if n>1:
            sizes=ndimage.sum(np.ones_like(lab),lab,range(1,n+1));keep=int(np.argmax(sizes))+1
            for j in range(1,n+1):
                if j==keep:continue
                comp=lab==j;ring=ndimage.binary_dilation(comp)&~comp&(new>=0)&(new!=i)
                if ring.any():new[comp]=np.bincount(new[ring]).argmax()
    map_report=[dict(key=k,b1Area=targets[k],area=int((new==i).sum())) for i,k in enumerate(keys)]
    mapid=new
    if road is not None:pass
    w['mapid']=mapid;w['maps']=map_report
    return w


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
    road=np.zeros((H,W),bool);records=[];paths=[]
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
        paths.append(path)
        bridges=sum(1 for p in path if t[p]==RIVER);passes=sum(1 for p in path if t[p]==MOUNTAIN)
        maps_on=sorted({w['keys'][w['mapid'][p]] for p in path if w['mapid'][p]>=0})
        records.append(dict(fromKey=r['fromKey'],toKey=r['toKey'],cells=len(path),bridgeCells=bridges,mountainPassCells=passes,maps=maps_on))
    cross=np.array([[0,1,0],[1,1,1],[0,1,0]])
    wide=ndimage.binary_dilation(road,structure=cross)&w['owned']&~np.isin(t,[DEEP,SHALLOW,PEAK,MOUNTAIN])
    # 강 위로는 실제로 건너는 곳(다리)만 넓힌다. 강가를 따라가는 길이 물 위로 번지지 않게
    river=t==RIVER;crossing=ndimage.binary_dilation(road&river,structure=cross,iterations=2)
    wide&=~river|crossing
    return dict(road=wide,center=road,records=records,paths=paths)


def render(w,path,road=None,scale=4,title='',tint=False):
    t=w['t'];elev=w['elev']
    rgb=np.zeros((H,W,3),float)
    for k,c in COLORS.items():rgb[t==k]=c
    # 언덕 그림자(북서쪽 빛)
    gy,gx=np.gradient(ndimage.gaussian_filter(elev,1.2))
    shade=np.clip(1+(-gx-gy)*6,0.72,1.22)
    landish=w['land']&(t!=RIVER)&(t!=LAKE)
    if w.get('lake') is not None and w['lake'].any():   # 호수 깊이: 물가는 밝고 가운데는 짙게
        dep=ndimage.distance_transform_edt(w['lake']);lk_=w['lake']
        k_=np.clip(dep/6,0,1)[lk_,None]
        rgb[lk_]=np.array((118,178,206))*(1-k_)+np.array((52,108,160))*k_
    rgb[landish]*=shade[landish,None]
    # 마을 범위: 옅게 덮고 테두리
    vm=w['village'];rgb[vm]=rgb[vm]*.7+np.array((255,236,170))*.3
    edge=vm&~ndimage.binary_erosion(vm)
    rgb[edge]=(255,226,110)
    if road is not None:
        rgb[road['road']&(t==RIVER)]=(150,110,70)          # 다리
        rgb[road['road']&(t!=RIVER)]=(176,140,92)
    # 맵 경계
    mid=w['mapid'];b=np.zeros((H,W),bool)
    b[:,1:]|=(mid[:,1:]!=mid[:,:-1])&(mid[:,1:]>=0)&(mid[:,:-1]>=0);b[1:,:]|=(mid[1:,:]!=mid[:-1,:])&(mid[1:,:]>=0)&(mid[:-1,:]>=0)
    if tint:
        pal=[(255,120,120),(120,200,255),(255,210,90),(150,255,150),(220,140,255),(255,160,60),(90,230,210),(255,120,200),(170,170,255),(200,255,90),(255,255,140),(140,200,140),(230,180,140)]
        for i in range(len(w['keys'])):
            m=w['mapid']==i;rgb[m]=rgb[m]*.62+np.array(pal[i%len(pal)])*.38
    b=ndimage.binary_dilation(b)&w['owned'] if tint else b
    rgb[b]=(20,20,24) if tint else rgb[b]*.45
    ys,xs=np.nonzero(w["owned"]);y0,y1,x0,x1=max(0,ys.min()-3),min(H,ys.max()+4),max(0,xs.min()-3),min(W,xs.max()+4)
    img=Image.fromarray(np.clip(rgb[y0:y1,x0:x1],0,255).astype(np.uint8)).resize(((x1-x0)*scale,(y1-y0)*scale),Image.NEAREST)
    d=ImageDraw.Draw(img);f=ImageFont.truetype(str(FONT),14);fs=ImageFont.truetype(str(FONT),11)
    if tint:
        for i,k in enumerate(w['keys']):
            ys_,xs_=np.nonzero(w['mapid']==i);cy,cx=(ys_.mean()-y0)*scale,(xs_.mean()-x0)*scale
            lab=k.split('/')[1]+f" {len(ys_):,}칸"
            if any(v['key']==k for v in w['villages']):cy+=22
            d.text((cx-len(lab)*6,cy),lab,font=ImageFont.truetype(str(FONT),12),fill=(20,20,20),stroke_width=2,stroke_fill=(255,255,255))
    for v in w['villages']:
        cx,cy=(v['center'][0]-X0-x0)*scale,(v['center'][1]-Y0-y0)*scale
        d.text((cx-len(v['label'])*7,cy-8),v['label'],font=f,fill=(30,24,20),stroke_width=2,stroke_fill=(255,250,230))
    # 범례
    items=[SHALLOW,SAND,ROCKCOAST,RIVER,LAKE,MARSH,FIELD,GRASS,MEADOW,HILL,FOREST,MOUNTAIN,PEAK]
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
    r=roads(layout,w) if a.roads else None
    partition(layout,w,r)
    counts={NAMES[k]:int((w['t']==k).sum()) for k in range(1,16)}
    render(w,OUT/f'terrain-villages{sfx}.png',title='M1 지형 v3 + 마을 범위'+sfx)
    report=dict(seed=SEED,landCells=land_before,classCounts=counts,rivers=w['rivers'],villages=w['villages'],maps=w['maps'],lakes=[{k:v for k,v in l.items()} for l in w['lakes']])
    if r:
        render(w,OUT/f'terrain-villages-roads{sfx}.png',road=r,title='M1 지형 v3 + 마을 + 길'+sfx)
        render(w,OUT/f'maps{sfx}.png',road=r,title='맵 경계(색=맵)'+sfx,tint=True)
        # 길이 맵 경계를 몇 번 넘는지(적을수록 좋음)
        for rec,path in zip(r['records'],r['paths']):
            ids=[int(w['mapid'][p]) for p in path];rec['boundaryCrossings']=sum(1 for a_,b_ in zip(ids,ids[1:]) if a_!=b_)
        report['roads']=r['records'];report['roadCells']=int(r['road'].sum())
    (OUT/f'report{sfx}.json').write_text(json.dumps(report,ensure_ascii=False,indent=1))
    print(json.dumps(report,ensure_ascii=False)[:3000])


if __name__=='__main__':main()
