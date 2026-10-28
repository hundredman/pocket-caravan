"""M2 마을 구역도(검토용). M1② 결과(ArtReview/M1-Tiles/masks.npz, world-full.png) 위에 마을 기능 자리를 잡는다.

마을마다 요소 목록(크기·바라는 위치·조건)을 적으면, 바라는 위치에서 가장 가까운 알맞은 자리를 찾는다.
- 모든 요소: 그 마을 맵 안, 물·다리 밖, 서로 1칸 띄움
- 건물(lot): 마을 범위 안, 길 위가 아님, 문(아래 가운데) 앞 3칸 안에 길이 있어야 함
- 광장·마당(open): 마을 범위 안, 길 위가 아님
- 물가(shore): 물에 닿은 땅 칸(나루터·낚시터)
- 점(spot): NPC·게시판·시작 위치 등 1칸
출력: ArtReview/M2-Zones/<마을>.json, <마을>.png(구역도), <마을>-cam.png(게임 크기 480×270 ×2)
"""
import json,math,sys
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];sys.path.insert(0,str(HERE))
import terrain_v3 as v
SRC=ROOT/'ArtReview/M1-Tiles';OUT=ROOT/'ArtReview/M2-Zones';T=16
FONT=str(ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf')

KIND_COL={'open':(255,214,90),'lot':(240,120,90),'shore':(90,190,255),'spot':(255,255,255),'npc':(255,120,220),'growth':(150,240,140)}

# 나루: 큰 강 하구 양쪽 기슭에 걸친 나루터 마을(튜토리얼 시작). 기존 ID는 M0 대응표.
# 위치는 왼쪽 위 칸의 세계 좌표(x,y). 나루 둘러보기(ArtReview/M2-Zones/naru-grid.png)를 보고 직접 잡았다.
# 강 남쪽 기슭(x 224~257, y 47~70)에 마을, 북동쪽 하구에 나루터와 다리. 길은 서쪽에서 들어와 북동쪽 다리로 꺾인다.
NARU=[
    ('장터 광장','open',12,6,(230,50),'market naru — 강과 길 사이 넓은 터'),
    ('게시판','spot',1,1,(243,54),'board naru — 광장 동쪽, 길가'),
    ('촌장 집','lot',5,4,(243,47),'npc naru_chief — 다리 어귀'),
    ('나루터','shore',3,3,(251,54),'나룻배 정박. 뱃사공·짐꾼'),
    ('집 1','lot',4,3,(225,51),''),
    ('집 2','lot',4,3,(232,62),''),('집 3','lot',4,3,(237,62),''),
    ('집 4','lot',4,3,(250,59),''),('집 5','lot',4,3,(251,64),''),
    ('우물','spot',1,2,(245,61),'naru_well_woman'),
    ('이벤트 마당','open',8,5,(242,65),'모임·행사'),
    ('낚시터','shore',2,1,(256,63),'pastime goryeo_fishing'),
    ('시작 위치','spot',1,1,(239,59),'튜토리얼 시작(길 위)'),
    ('촌장','npc',1,1,(245,52),'naru_chief: 촌장 집 문 앞'),
    ('우물가 아낙','npc',1,1,(247,62),'naru_well_woman'),
    ('뱃사공','npc',1,1,(249,55),'talker'),
    ('나루 짐꾼','npc',1,1,(248,57),'worker'),
    ('농부','npc',1,1,(228,57),'talker — 마을 서쪽 입구'),
    ('성장 자리 1','growth',1,1,(225,48),'마을 성장 소품'),('성장 자리 2','growth',1,1,(246,58),''),
    ('성장 자리 3','growth',1,1,(232,58),''),('성장 자리 4','growth',1,1,(256,60),''),('성장 자리 5','growth',1,1,(251,69),''),
]
VILLAGES={'goryeo/나루':('naru',NARU)}
FIXED=True   # 직접 잡은 위치를 쓴다(검사만)

def load():
    d=np.load(SRC/'masks.npz')
    return {k:d[k] for k in d.files}

def plan(key,items,d):
    keys=list(d['keys']);towns=list(d['towns'])
    own=d['own'];water=d['water'];road=d['road'];mid=d['mapid']
    mapm=mid==keys.index(key);vm=d['vlabel']==towns.index(key)
    H,W=own.shape
    # 다리 칸(물 위)
    bridge=np.zeros_like(own)
    for cy,cx,deg,L in d['bridges']:
        uy,ux=math.sin(math.radians(deg)),math.cos(math.radians(deg))
        for k in np.arange(-L/2-1,L/2+1.01,.5):
            for wv in(-2,-1,0,1,2):
                q=(int(round(cy+uy*k-ux*wv)),int(round(cx+ux*k+uy*wv)))
                if 0<=q[0]<H and 0<=q[1]<W:bridge[q]=True
    land=mapm&~water&~bridge
    shore=land&ndimage.binary_dilation(water,iterations=1)
    used=np.zeros_like(own);out=[]
    def ok(kind,y0,x0,w,h):
        if y0<0 or x0<0 or y0+h>H or x0+w>W:return False
        sl=(slice(y0,y0+h),slice(x0,x0+w))
        pad=(slice(max(0,y0-1),y0+h+1),slice(max(0,x0-1),x0+w+1))
        if used[pad].any():return False
        if kind in('open','lot'):
            if not (land[sl]&vm[sl]&~road[sl]).all():return False
            if kind=='lot':
                dy,dx=y0+h,x0+w//2   # 문 앞: 7칸 안에 길이 있어야(없으면 골목을 낸다)
                if not road[max(0,dy-7):dy+8,max(0,dx-7):dx+8].any():return False
            return True
        if kind=='shore':return land[sl].all() and shore[sl].any()
        return land[sl].all() and (vm[sl].all() or kind!='spot')
    named={}
    for name,kind,w,h,pref,memo in items:
        if pref is None:continue
        if FIXED:
            iy,ix=v.idx(*pref);ok_=ok(kind if kind!='npc' and kind!='growth' else 'spot',iy,ix,w,h) if kind!='lot' else ok('open',iy,ix,w,h)
            used[iy:iy+h,ix:ix+w]=True
            rec=dict(name=name,kind=kind,x=int(pref[0]),y=int(pref[1]),w=w,h=h,memo=memo,valid=bool(ok_))
            out.append(rec);named[name]=rec;continue
        py,px=v.idx(*pref);best=None
        for r_ in range(0,25):
            for y0 in range(py-r_-h//2,py+r_-h//2+1):
                for x0 in range(px-r_-w//2,px+r_-w//2+1):
                    if max(abs(y0+h//2-py),abs(x0+w//2-px))!=r_:continue
                    if ok(kind,y0,x0,w,h):
                        dd=math.hypot(y0+h/2-py,x0+w/2-px)
                        if best is None or dd<best[0]:best=(dd,y0,x0)
            if best:break
        if not best:out.append(dict(name=name,kind=kind,missing=True,memo=memo));continue
        _,y0,x0=best;used[y0:y0+h,x0:x0+w]=True
        rec=dict(name=name,kind=kind,x=int(x0+v.X0),y=int(y0+v.Y0),w=w,h=h,memo=memo,moved=round(_,1))
        out.append(rec);named[name]=rec
    # NPC: 기준 요소 옆
    def near(ref,dx,dy):
        r=named.get(ref)
        if not r:return None
        return (r['x']+dx,r['y']+dy)
    hint={'촌장':near('촌장 집',2,4),'우물가 아낙':near('우물',1,1),'뱃사공':near('나루터',-1,2),'나루 짐꾼':near('나루터',-1,4)}
    for i,(name,kind,w,h,pref,memo) in enumerate(items):
        if pref is not None or name not in hint or hint[name] is None:continue
        py,px=v.idx(*hint[name]);best=None
        for r_ in range(0,8):
            for y0 in range(py-r_,py+r_+1):
                for x0 in range(px-r_,px+r_+1):
                    if ok('spot',y0,x0,1,1) and (best is None or math.hypot(y0-py,x0-px)<best[0]):best=(math.hypot(y0-py,x0-px),y0,x0)
            if best:break
        if best:
            _,y0,x0=best;used[y0,x0]=True;rec=dict(name=name,kind=kind,x=int(x0+v.X0),y=int(y0+v.Y0),w=1,h=1,memo=memo)
            out.append(rec);named[name]=rec
    lanes=np.zeros_like(own)
    for r in out:
        if r.get('kind')!='lot' or r.get('missing'):continue
        iy,ix=v.idx(r['x'],r['y']);dy,dx=iy+r['h'],ix+r['w']//2
        if road[dy:dy+2,dx-1:dx+2].any():continue
        cand=np.argwhere(road&mapm);tgt=cand[np.argmin(((cand-[dy,dx])**2).sum(1))]
        y,x=dy,dx
        while y!=tgt[0]:lanes[y,x:x+2]=True;y+=1 if tgt[0]>y else -1
        while x!=tgt[1]:lanes[y:y+2,x]=True;x+=1 if tgt[1]>x else -1
    lanes&=~road&~water
    return out,mapm,vm,lanes

def draw(key,slug,out,mapm,vm,lanes=None):
    im=Image.open(SRC/'world-full.png').convert('RGBA')
    ys,xs=np.nonzero(mapm);x0,x1,y0,y1=xs.min()-1,xs.max()+2,ys.min()-1,ys.max()+2
    c=im.crop((x0*T,y0*T,x1*T,y1*T));ov=Image.new('RGBA',c.size,(0,0,0,0));dr=ImageDraw.Draw(ov)
    for yy in range(y0,y1):
        for xx in range(x0,x1):
            if not mapm[yy,xx]:dr.rectangle([(xx-x0)*T,(yy-y0)*T,(xx-x0+1)*T-1,(yy-y0+1)*T-1],fill=(0,0,0,120))
    if lanes is not None:
        for yy,xx in np.argwhere(lanes):dr.rectangle([(xx-x0)*T,(yy-y0)*T,(xx-x0+1)*T-1,(yy-y0+1)*T-1],fill=(176,140,92,230))
    # 마을 범위 테두리
    edge=vm&~ndimage.binary_erosion(vm)
    for yy,xx in np.argwhere(edge):dr.rectangle([(xx-x0)*T,(yy-y0)*T,(xx-x0+1)*T-1,(yy-y0+1)*T-1],fill=(255,230,120,90))
    f=ImageFont.truetype(FONT,22);fs=ImageFont.truetype(FONT,16)
    for r in out:
        if r.get('missing'):continue
        iy,ix=v.idx(r['x'],r['y']);col=KIND_COL[r['kind']]
        bx=[(ix-x0)*T,(iy-y0)*T,(ix-x0+r['w'])*T-1,(iy-y0+r['h'])*T-1]
        dr.rectangle(bx,fill=col+(90,),outline=col+(255,),width=3)
    c.alpha_composite(ov);d2=ImageDraw.Draw(c)
    for r in out:
        if r.get('missing'):continue
        iy,ix=v.idx(r['x'],r['y'])
        d2.text(((ix-x0)*T+2,(iy-y0)*T-18 if r['kind'] in('npc','spot','growth') else (iy-y0)*T+3),r['name'],fill='white',font=fs if r['kind'] in('npc','spot','growth') else f,stroke_width=3,stroke_fill='black')
    OUT.mkdir(parents=True,exist_ok=True)
    c.save(OUT/f'{slug}.png')
    small=c.resize((c.width//2,c.height//2),Image.BOX);small.save(OUT/f'{slug}-half.png')
    return c

if __name__=='__main__':
    d=load()
    for key,(slug,items) in VILLAGES.items():
        out,mapm,vm,lanes=plan(key,items,d)
        (OUT).mkdir(parents=True,exist_ok=True)
        (OUT/f'{slug}.json').write_text(json.dumps(dict(map=key,items=out),ensure_ascii=False,indent=1))
        draw(key,slug,out,mapm,vm,lanes);np.save(OUT/f'{slug}-lanes.npy',lanes)
        print(slug,[ (r['name'],r.get('moved'),r.get('missing')) for r in out])
