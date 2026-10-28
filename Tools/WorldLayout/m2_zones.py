"""M2 마을 구역도(검토용). M1② 결과(ArtReview/M1-Tiles/masks.npz, world-full.png) 위에 마을 기능 자리를 잡는다.

마을마다 요소 목록을 적으면 빈 땅에서 알맞은 자리를 찾는다. 나루는 은샘이 승인한 위치(2026-10-28)를 그대로 쓴다.
- 모든 요소: 마을 범위 안(물가 요소는 물에 닿은 땅), 물·다리·길 밖, 건물·마당끼리 1칸 띄움
- 건물(lot): 문(아래 가운데 바로 앞 칸)에서 길까지 걸어서 갈 수 있어야 함. 멀면 골목을 낸다
- 장터 광장(plaza)·마당(yard): 광장은 길에 닿게, 마당은 광장에서 조금 떨어지게
- 점(spot·npc·lantern·growth): 1칸(우물·석등은 1×2). NPC는 기준 건물 문 앞이나 기준 요소 옆
마을 범위 안 나무는 M3에서 건물·광장 자리만 치운다.
출력: ArtReview/M2-Zones/<slug>.json, <slug>.png(1:1), <slug>-half.png, zones.json(전체), check.json
"""
import json,math,sys
from collections import deque
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];sys.path.insert(0,str(HERE))
import terrain_v3 as v
SRC=ROOT/'ArtReview/M1-Tiles';OUT=ROOT/'ArtReview/M2-Zones';T=16
FONT=str(ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf')

# 승인된 C7 그림의 칸 크기(그림 크기/16). 건물 그림은 나중에 바뀌어도 자리는 그대로 쓴다.
ASSET={'cottage':(5,4),'thatch':(4,3),'tile-house':(4,3),'small_store':(4,3),'shop':(6,4),'gaekju':(6,4),
       'palace':(10,7),'gate':(6,5),'pavilion':(5,5),'paper_large':(4,3),'paper_rack':(3,2),'celadon':(3,2),
       'harvest':(3,2),'boat':(5,2),'well':(1,2),'stone_lantern':(1,2),'bench':(3,1),'onggi':(1,1),'laundry':(2,2),
       'willow':(4,4),'garden_wall':(4,2)}
KIND_COL={'moor':(60,120,255),'plaza':(255,214,90),'yard':(255,170,60),'lot':(240,110,90),'shore':(90,190,255),'spot':(255,255,255),
          'npc':(255,120,220),'lantern':(255,250,170),'growth':(150,240,140),'start':(120,255,255)}

def L(name,asset,anchor='plaza',**k):
    w,h=ASSET[asset];return dict(name=name,kind='lot',asset=asset,w=w,h=h,anchor=anchor,**k)
def P(name,asset,anchor,kind='spot',**k):
    w,h=ASSET[asset];return dict(name=name,kind=kind,asset=asset,w=w,h=h,anchor=anchor,**k)
def N(name,anchor,**k):return dict(name=name,kind='npc',w=1,h=1,anchor=anchor,**k)
def G(n):return [dict(name=f'성장 자리 {i+1}',kind='growth',w=1,h=1,anchor='spread') for i in range(n)]

def village(mid,plaza,yard,houses,special,npcs,lanterns=2,growth=4):
    """모든 마을: 장터 광장(시장), 게시판, 이벤트 마당, 집, 우물, 석등, 성장 자리 + 마을별 건물·NPC."""
    it=[dict(name='장터 광장',kind='plaza',w=plaza[0],h=plaza[1],anchor='center',id=f'market:{mid}')]
    it+=special
    it+=[dict(name='이벤트 마당',kind='yard',w=yard[0],h=yard[1],anchor='away')]
    it+=[L(f'집 {i+1}',a,'plaza') for i,a in enumerate(houses)]
    it+=[dict(name='게시판',kind='spot',w=1,h=1,anchor='plaza',id=f'board:{mid}'),P('우물','well','houses')]
    it+=[P(f'석등 {i+1}','stone_lantern','plaza',kind='lantern') for i in range(lanterns)]
    return it+npcs+G(growth)

# ── 마을별 목록 ── id는 M0 대응표의 기존 ID. NEW_IDS는 M2에서 새로 정한 ID 제안(런타임 등록은 M4).
VILLAGES={
 'goryeo/개경':('gaegyeong','Gaegyeong',village('gaegyeong',(14,8),(10,6),
    ['tile-house','tile-house','tile-house','tile-house','thatch','thatch'],
    [L('궁성','palace','center',memo='수도 궁궐'),L('객주','gaekju','plaza',id='npc:gaekju'),
     L('시전 1','shop','plaza'),L('시전 2','shop','plaza'),P('청자 공방','celadon','plaza')],
    [N('객주 주인','객주',id='gaekju'),N('시전 짐꾼','시전 1',memo='worker'),N('시전 행상','시전 2',memo='worker')],
    lanterns=4,growth=5)),
 'goryeo/송악':('songak','goryeo_songak',village('goryeo_songak',(12,7),(8,5),
    ['thatch','tile-house','thatch','tile-house'],
    [L('상점','shop','plaza'),L('정자','pavilion','away',memo='산록 쉼터')],
    [N('송악 상인','상점',id='goryeo_songak_merchant')])),
 'goryeo/닥목':('dakmok','Dakmok',village('dakmok',(12,7),(8,5),
    ['thatch','thatch','thatch'],
    [L('촌장 집','cottage','plaza',id='npc:dakmok_chief'),L('한지 작업장','paper_large','plaza'),
     P('한지 건조대 1','paper_rack','한지 작업장'),P('한지 건조대 2','paper_rack','한지 작업장')],
    [N('닥목 촌장','촌장 집',id='dakmok_chief'),N('종이 일꾼','한지 작업장',memo='worker')],lanterns=1)),
 'goryeo/장단':('jangdan','goryeo_jangdan',village('goryeo_jangdan',(12,7),(8,5),
    ['thatch','thatch','thatch','thatch','cottage'],
    [L('곳간','small_store','plaza'),P('수확 더미','harvest','곳간')],
    [N('장단 농부','곳간',id='goryeo_jangdan_farmer')],lanterns=1)),
 'east/동문 마을':('woodtown','transit_woodtown',village('transit_woodtown',(12,7),(8,5),
    ['thatch','cottage','thatch','tile-house'],
    [L('상점','shop','plaza'),L('곳간','small_store','plaza')],
    [N('길목 상인','상점',memo='새 NPC ID는 M4'),N('주민','houses',memo='talker')])),
 'east/북동 포구':('ferry','transit_ferry',village('transit_ferry',(12,7),(8,5),
    ['thatch','tile-house','thatch','cottage'],
    [L('상점','shop','plaza'),L('창고','small_store','water'),
     dict(name='나루터',kind='shore',w=3,h=2,anchor='water',memo='선착장 자리'),P('배','boat','나루터',kind='moor'),
     P('빨래터','laundry','water',kind='shore')],
    [N('포구 상인','상점',memo='새 NPC ID는 M4'),N('뱃사공','나루터',memo='talker')])),
 'east/남동 포구':('southport','transit_southport',village('transit_southport',(12,7),(8,5),
    ['thatch','tile-house','thatch','cottage'],
    [L('상점','shop','plaza'),L('창고','small_store','water'),
     dict(name='나루터',kind='shore',w=3,h=2,anchor='water',memo='선착장 자리'),P('배','boat','나루터',kind='moor'),
     P('빨래터','laundry','water',kind='shore')],
    [N('포구 상인','상점',memo='새 NPC ID는 M4'),N('뱃사공','나루터',memo='talker')])),
 'east/중앙 길목 마을':('crossroads','transit_crossroads',village('transit_crossroads',(12,7),(8,5),
    ['thatch','tile-house','thatch','tile-house','cottage'],
    [L('객주','gaekju','plaza',memo='길손 숙소'),L('상점','shop','plaza')],
    [N('길목 상인','상점',memo='새 NPC ID는 M4'),N('객주 주인','객주',memo='새 NPC ID는 M4')])),
 'hamlet/북쪽 쉼터 마을':('rest_north','rest_north',village('rest_north',(8,5),(6,4),
    ['thatch','thatch'],
    [L('쉼터 주막','small_store','plaza',memo='작은 가게·쉼터'),P('평상','bench','plaza')],
    [N('쉼터 주인','쉼터 주막',memo='새 NPC ID는 M4')],lanterns=1,growth=2)),
 'hamlet/남쪽 호숫가 마을':('rest_lake','rest_lake',village('rest_lake',(8,5),(6,4),
    ['thatch','thatch'],
    [L('쉼터 주막','small_store','plaza',memo='작은 가게·쉼터'),dict(name='나루터',kind='shore',w=3,h=2,anchor='water'),P('나룻배','boat','나루터',kind='moor'),
     P('빨래터','laundry','water',kind='shore')],
    [N('쉼터 주인','쉼터 주막',memo='새 NPC ID는 M4'),N('어부','나루터',memo='talker')],lanterns=1,growth=2)),
}
NEW_IDS={'transit_southport':'남동 포구 시장·게시판 ID(제안). 맵 ID는 M0 확정',
         'transit_crossroads':'중앙 길목 마을 시장·게시판 ID(제안). 맵 ID는 M0 확정',
         'rest_north':'북쪽 쉼터 마을 시장·게시판 ID(제안). 맵은 transit_central_approach',
         'rest_lake':'남쪽 호숫가 마을 시장·게시판 ID(제안). 맵은 transit_south_inland'}

# 나루: 은샘 승인(2026-10-28) 위치. (이름, 종류, 그림, x, y, w, h, id, 메모). x,y는 왼쪽 위 칸의 세계 좌표
NARU_FIXED=[
    ('장터 광장','plaza',None,230,50,12,6,'market:naru','강과 길 사이 넓은 터'),
    ('게시판','spot',None,243,54,1,1,'board:naru','광장 동쪽, 길가'),
    ('촌장 집','lot','cottage',243,47,5,4,'npc:naru_chief','다리 어귀'),
    ('나루터','shore',None,251,54,3,3,'','나룻배 정박. 뱃사공·짐꾼'),
    ('집 1','lot','thatch',225,51,4,3,'',''),('집 2','lot','thatch',232,62,4,3,'',''),('집 3','lot','tile-house',237,62,4,3,'',''),
    ('집 4','lot','tile-house',250,59,4,3,'',''),('집 5','lot','thatch',251,64,4,3,'',''),
    ('우물','spot','well',245,61,1,2,'','naru_well_woman'),
    ('이벤트 마당','yard',None,242,65,8,5,'','모임·행사'),
    ('낚시터','shore',None,256,63,2,1,'pastime:goryeo_fishing',''),
    ('시작 위치','start',None,239,59,1,1,'','튜토리얼 시작(길 위)'),
    ('촌장','npc',None,245,52,1,1,'naru_chief','촌장 집 문 앞'),
    ('우물가 아낙','npc',None,247,62,1,1,'naru_well_woman',''),
    ('뱃사공','npc',None,249,55,1,1,'','talker'),('나루 짐꾼','npc',None,248,57,1,1,'','worker'),
    ('농부','npc',None,228,57,1,1,'','talker'),
    ('성장 자리 1','growth',None,225,48,1,1,'',''),('성장 자리 2','growth',None,246,58,1,1,'',''),
    ('성장 자리 3','growth',None,232,58,1,1,'',''),('성장 자리 4','growth',None,256,60,1,1,'',''),('성장 자리 5','growth',None,251,69,1,1,'',''),
]
VILLAGES['goryeo/나루']=('naru','Naru',None)

def load():
    d=np.load(SRC/'masks.npz');return {k:d[k] for k in d.files}

def bridge_mask(d,shape):
    H,W=shape;b=np.zeros(shape,bool)
    for cy,cx,deg,Ln in d['bridges']:
        uy,ux=math.sin(math.radians(deg)),math.cos(math.radians(deg))
        for k in np.arange(-Ln/2-1,Ln/2+1.01,.5):
            for wv in(-2,-1,0,1,2):
                q=(int(round(cy+uy*k-ux*wv)),int(round(cx+ux*k+uy*wv)))
                if 0<=q[0]<H and 0<=q[1]<W:b[q]=True
    return b

class Planner:
    def __init__(s,d,key):
        s.d=d;keys=list(d['keys']);towns=list(d['towns'])
        s.vm=d['vlabel']==towns.index(key)
        mid=np.unique(d['mapid'][s.vm]);s.mapm=d['mapid']==mid[0];s.mapkey=str(keys[mid[0]])
        s.water=d['water'];s.road=d['road'];s.H,s.W=s.water.shape
        s.bridge=bridge_mask(d,s.water.shape)
        s.land=s.mapm&~s.water&~s.bridge
        s.free0=s.vm&s.land&~s.road
        s.shore=s.land&ndimage.binary_dilation(s.water,iterations=1)&~s.road
        s.hard=np.zeros_like(s.vm);s.lot=np.zeros_like(s.vm);s.soft=np.zeros_like(s.vm);s.door=np.zeros_like(s.vm)
        s.droad=ndimage.distance_transform_edt(~(s.road&s.mapm))
        ys,xs=np.nonzero(s.vm);s.cen=(ys.mean(),xs.mean())
        s.dwater=ndimage.distance_transform_edt(~s.water)
        s.out=[];s.named={}
    def rect_ok(s,y0,x0,w,h,need=None,pad=True):
        if y0<1 or x0<1 or y0+h>=s.H or x0+w>=s.W:return False
        sl=(slice(y0,y0+h),slice(x0,x0+w))
        m=s.free0 if need is None else need
        if not m[sl].all():return False
        if s.door[sl].any() or s.soft[sl].any():return False
        if pad:
            if s.hard[y0-1:y0+h+1,x0-1:x0+w+1].any():return False
        elif s.hard[sl].any():return False
        return True
    def anchor_pt(s,a):
        if a in s.named:r=s.named[a];return (r['iy']+r['h']/2,r['ix']+r['w']/2)
        if a=='plaza' and '장터 광장' in s.named:return s.anchor_pt('장터 광장')
        if a=='houses':
            hs=[r for r in s.out if r['name'].startswith('집 ') and not r.get('missing')]
            if hs:return (np.mean([r['iy']+r['h']/2 for r in hs]),np.mean([r['ix']+r['w']/2 for r in hs]))
        return s.cen
    def put(s,it,iy,ix):
        r=dict(name=it['name'],kind=it['kind'],asset=it.get('asset'),iy=int(iy),ix=int(ix),w=it['w'],h=it['h'],
               id=it.get('id',''),memo=it.get('memo',''))
        sl=(slice(iy,iy+it['h']),slice(ix,ix+it['w']))
        if it['kind'] in('lot','plaza','yard'):s.hard[sl]=True
        if it['kind']=='lot':s.lot[sl]=True
        else:s.soft[sl]=True
        if it['kind']=='lot':
            dy,dx=iy+it['h'],ix+it['w']//2;s.door[dy,dx]=True;r['door']=(int(dy),int(dx))
        s.out.append(r);s.named[it['name']]=r;return r
    def wet(s,a,b):
        """두 점 사이 직선이 물을 건너면 True(다리로만 이어지는 맞은편 기슭에 소품이 가지 않게)"""
        n=int(max(abs(a[0]-b[0]),abs(a[1]-b[1])))+1
        for i in range(n+1):
            y=int(round(a[0]+(b[0]-a[0])*i/n));x=int(round(a[1]+(b[1]-a[1])*i/n))
            if s.water[y,x]:return True
        return False
    def miss(s,it):s.out.append(dict(name=it['name'],kind=it['kind'],missing=True,memo=it.get('memo','')))
    def place_rect(s,it):
        w,h,k=it['w'],it['h'],it['kind'];a=it.get('anchor','center')
        ay,ax=s.anchor_pt(a);best=None;pz=s.named.get('장터 광장');cands=[]
        ys,xs=np.nonzero(s.vm);Y0,Y1,X0,X1=ys.min()-3,ys.max()+3,xs.min()-3,xs.max()+3
        open_=s.land&~s.road
        for y0 in range(Y0,Y1-h+2):
            for x0 in range(X0,X1-w+2):
                if k=='moor':
                    sl=(slice(y0,y0+h),slice(x0,x0+w))
                    if not (s.water[sl].all() and s.mapm[sl].all()) or s.bridge[y0-1:y0+h+1,x0-1:x0+w+1].any():continue
                    if not (s.land&~s.road)[y0-1:y0+h+1,x0-1:x0+w+1].any():continue
                    if s.soft[y0-1:y0+h+1,x0-1:x0+w+1].any():continue
                    sc=math.hypot(y0+h/2-ay,x0+w/2-ax)
                    cands.append((sc,y0,x0))
                    continue
                if k=='shore':
                    if not s.shore[y0:y0+h,x0:x0+w].any():continue
                    if not s.vm[max(0,y0-3):y0+h+3,max(0,x0-3):x0+w+3].any():continue
                    if not s.rect_ok(y0,x0,w,h,need=open_):continue
                elif not s.rect_ok(y0,x0,w,h):continue
                cy,cx=y0+h/2,x0+w/2
                if a=='away' and pz:
                    dp=math.hypot(cy-pz['iy']-pz['h']/2,cx-pz['ix']-pz['w']/2);sc=abs(dp-13)+.3*math.hypot(cy-s.cen[0],cx-s.cen[1])
                elif a=='water':sc=s.dwater[y0:y0+h,x0:x0+w].min()*2+.4*math.hypot(cy-s.cen[0],cx-s.cen[1])
                else:sc=math.hypot(cy-ay,cx-ax)
                if k=='plaza' and s.droad[y0-1:y0+h+1,x0-1:x0+w+1].min()>1.5:continue
                if k=='lot':
                    dy,dx=y0+h,x0+w//2
                    if dy>=s.H or not s.land[dy,dx] or s.hard[dy,dx] or s.soft[dy,dx]:continue
                    if dy+1<s.H and s.water[dy+1,dx]:continue
                    dr=s.droad[dy,dx]
                    if dr>9:continue
                    sc=(.3*sc+1.2*dr) if it['name'].startswith('집 ') else sc+.6*dr
                if a not in('center','away','water') and k!='moor' and s.wet((int(cy),int(cx)),(int(ay),int(ax))):sc+=30
                if k=='yard' and s.droad[y0-1:y0+h+1,x0-1:x0+w+1].min()>6:continue
                cands.append((sc,y0,x0))
        cands.sort()
        for sc,y0,x0 in cands[:3000]:
            if k=='lot' and s.lane_len(y0+h,x0+w//2,(y0,x0,w,h))>24:continue
            if k=='shore' and s.lane_len(y0+h-1,x0,(0,0,0,0),start_rect=(y0,x0,w,h))>24:continue
            if k=='shore' and it.get('asset') and not s.keeps_paths((y0,x0,w,h)):continue
            return s.put(it,y0,x0)
        return s.miss(it)
    def keeps_paths(s,rect):
        """물가 소품(빨래터 등)을 놓아도 둘레 건물 문·나루터에서 길로 가는 골목이 막히지 않는지"""
        y0,x0,w,h=rect
        for r in s.out:
            if r.get('missing') or abs(r['iy']-y0)>12 or abs(r['ix']-x0)>12:continue
            if r['kind']=='lot' and s.lane_len(r['door'][0],r['door'][1],rect)>24:return False
            if r['kind']=='shore' and not r.get('asset'):
                s.lot[y0:y0+h,x0:x0+w]=True
                try:
                    if s.lane_len(0,0,(0,0,0,0),start_rect=(r['iy'],r['ix'],r['w'],r['h']))>24:return False
                finally:s.lot[y0:y0+h,x0:x0+w]=False
        return True
    def lane_len(s,dy,dx,newlot,start_rect=None):
        """문 앞(또는 자리)에서 길까지 2칸 폭 골목으로 가는 길이. 못 가면 999"""
        walk=s.land&~s.lot
        if newlot[2]:walk=walk.copy();walk[newlot[0]:newlot[0]+newlot[3],newlot[1]:newlot[1]+newlot[2]]=False
        # 2칸 폭: 칸이 들어가는 2×2 걷기 덩어리가 있어야
        a=walk[:-1,:-1]&walk[1:,:-1]&walk[:-1,1:]&walk[1:,1:];w2=np.zeros_like(walk)
        w2[:-1,:-1]|=a;w2[1:,:-1]|=a;w2[:-1,1:]|=a;w2[1:,1:]|=a
        q=deque();seen=set()
        if start_rect:
            y0,x0,w,h=start_rect
            for yy in range(y0,y0+h):
                for xx in range(x0,x0+w):q.append(((yy,xx),0));seen.add((yy,xx))
        else:
            if not(0<=dy<s.H) or not w2[dy,dx]:return 999
            q.append(((dy,dx),0));seen.add((dy,dx))
        while q:
            c,dd=q.popleft()
            if s.road[c]:return dd
            if dd>30:return 999
            for oy,ox in((1,0),(-1,0),(0,1),(0,-1)):
                n=(c[0]+oy,c[1]+ox)
                if 0<=n[0]<s.H and 0<=n[1]<s.W and n not in seen and (w2[n] or s.road[n]):seen.add(n);q.append((n,dd+1))
        return 999
    def place_spot(s,it):
        k=it['kind'];a=it.get('anchor','plaza');w,h=it['w'],it['h']
        ref=s.named.get(a) or (s.named.get('장터 광장') if a=='plaza' else None)
        near=s.vm|ndimage.binary_dilation(s.vm,iterations=1)
        need=s.land&~s.road&near
        cands=[]
        for y0,x0 in np.argwhere(ndimage.binary_dilation(s.vm,iterations=2)):
            if not s.rect_ok(y0,x0,w,h,need=need,pad=False):continue
            if s.soft[y0-1:y0+h+1,x0-1:x0+w+1].any():continue
            if it.get('asset'):   # 그림이 있는 소품은 앞 칸이 비어 있어야(걸어서 닿게)
                fy,fx=y0+h,x0
                if fy>=s.H or not s.land[fy,fx] or s.lot[fy,fx] or s.door[fy,fx]:continue
            if k=='npc' and ref and ref.get('door'):
                dy,dx=ref['door'];sc=math.hypot(y0-dy,x0-dx-1.5)
            elif k=='npc' and ref:
                sc=abs(math.hypot(y0-ref['iy']-ref['h']/2,x0-ref['ix']-ref['w']/2)-max(ref['w'],ref['h'])/2-1.5)
            elif k=='lantern' and ref:
                if s.droad[y0+h-1,x0]>1.5:continue
                sc=math.hypot(y0-ref['iy']-ref['h']/2,x0-ref['ix']-ref['w']/2)
                ls=[r for r in s.out if r['kind']=='lantern']
                if ls:sc+=40/(1+min(math.hypot(y0-r['iy'],x0-r['ix']) for r in ls))
            elif it['name']=='게시판' and ref:
                if s.droad[y0,x0]>1.5:continue
                sc=math.hypot(y0-ref['iy']-ref['h']/2,x0-ref['ix']-ref['w']/2)
            else:
                ay,ax=s.anchor_pt(a);sc=math.hypot(y0-ay,x0-ax)
                if s.hard[y0-1:y0+h+1,x0-1:x0+w+1].any():sc+=3
            if not s.vm[y0,x0]:sc+=4
            if ref and s.wet((y0,x0),(ref['iy']+ref['h']//2,ref['ix']+ref['w']//2)):sc+=30
            cands.append((sc,y0,x0))
        if not cands:return s.miss(it)
        sc,y0,x0=min(cands);return s.put(it,y0,x0)
    def place_growth(s,items):
        ok=s.free0&~ndimage.binary_dilation(s.hard|s.soft|s.door,iterations=1)
        for it in items:
            pts=np.argwhere(ok)
            if not len(pts):s.miss(it);continue
            ex=np.array([(r['iy'],r['ix']) for r in s.out if not r.get('missing')])
            dd=np.min(np.hypot(pts[:,None,0]-ex[None,:,0],pts[:,None,1]-ex[None,:,1]),axis=1)
            j=int(np.argmax(np.minimum(dd,8)-.05*np.hypot(pts[:,0]-s.cen[0],pts[:,1]-s.cen[1])))
            r=s.put(it,*pts[j]);ok[r['iy']-1:r['iy']+2,r['ix']-1:r['ix']+2]=False
    def lanes(s):
        lane=np.zeros_like(s.vm);walk=s.land&~s.lot
        flat=s.hard&~s.lot   # 광장·마당은 걸어서 지나간다(골목으로 칠하지 않음)
        for r in s.out:
            if r.get('kind')!='lot' or r.get('missing'):continue
            dy,dx=r['door']
            if s.road[dy,dx]:r['laneLen']=0;continue
            prev={(dy,dx):None};q=deque([(dy,dx)]);hit=None
            while q:
                c=q.popleft()
                if s.road[c]:hit=c;break
                for oy,ox in((1,0),(0,1),(0,-1),(-1,0)):
                    n=(c[0]+oy,c[1]+ox)
                    if n in prev or not(0<=n[0]<s.H and 0<=n[1]<s.W) or not walk[n]:continue
                    if s.soft[n] and not s.road[n]:continue
                    prev[n]=c;q.append(n)
            if hit is None:r['blocked']=True;continue
            c=prev[hit];r['laneLen']=0
            while c is not None:lane[c]=True;r['laneLen']+=1;c=prev[c]
        return lane&~s.road&~flat
    def finish(s):
        for r in s.out:
            if not r.get('missing'):r['x']=r['ix']+v.X0;r['y']=r['iy']+v.Y0
        return s.out

def plan(d,key,items):
    p=Planner(d,key)
    if items is None:
        for name,kind,asset,x,y,w,h,id_,memo in NARU_FIXED:
            iy,ix=v.idx(x,y);p.put(dict(name=name,kind=kind,asset=asset,w=w,h=h,id=id_,memo=memo),iy,ix)
        # 석등은 승인 뒤 추가: 광장 앞 길가
        p.place_spot(P('석등 1','stone_lantern','plaza',kind='lantern'))
        p.place_rect(P('나룻배','boat','나루터',kind='moor'))
    else:
        order=('plaza','lot','shore','yard')
        for k in order:
            for it in items:
                if it['kind']==k:
                    if k=='lot' or it['kind']==k:pass
        # 순서: 광장 → 마을별 특별 건물(목록 순) → 물가 → 마당 → 집
        rects=[i for i in items if i['kind'] in('plaza','lot','yard','shore','moor')]
        first=[i for i in rects if not i['name'].startswith('집 ')];houses=[i for i in rects if i['name'].startswith('집 ')]
        for it in first+houses:
            if it['kind'] in('plaza','yard'):
                w,h=it['w'],it['h'];tried=[]
                for ww,hh in((w,h),(h,w),(w-2,h-1),(w-2,h),(h,w-2),(w-4,h-1),(w-4,h-2)):
                    it2=dict(it,w=ww,h=hh)
                    if p.place_rect(it2) is not None:break
                    p.out.pop()
                else:p.miss(it)
            else:p.place_rect(it)
        for it in [i for i in items if i['kind']=='spot' and i.get('asset') not in('well',)]:p.place_spot(it)
        for it in [i for i in items if i['kind']=='spot' and i.get('asset')=='well']:p.place_spot(it)
        for it in [i for i in items if i['kind']=='lantern']:p.place_spot(it)
        for it in [i for i in items if i['kind']=='npc']:p.place_spot(it)
        p.place_growth([i for i in items if i['kind']=='growth'])
    lane=p.lanes()
    return p,p.finish(),lane

def draw(p,out,lane,slug,title):
    im=Image.open(SRC/'world-full.png').convert('RGBA')
    ys,xs=np.nonzero(p.vm);m=6;x0,x1,y0,y1=xs.min()-m,xs.max()+m+1,ys.min()-m,ys.max()+m+1
    c=im.crop((x0*T,y0*T,x1*T,y1*T));ov=Image.new('RGBA',c.size,(0,0,0,0));dr=ImageDraw.Draw(ov)
    def box(yy,xx,hh=1,ww=1):return [(xx-x0)*T,(yy-y0)*T,(xx-x0+ww)*T-1,(yy-y0+hh)*T-1]
    for yy in range(y0,y1):
        for xx in range(x0,x1):
            if not p.mapm[yy,xx]:dr.rectangle(box(yy,xx),fill=(0,0,0,130))
    for yy,xx in np.argwhere(lane):dr.rectangle(box(yy,xx),fill=(176,140,92,230))
    edge=p.vm&~ndimage.binary_erosion(p.vm)
    for yy,xx in np.argwhere(edge):dr.rectangle(box(yy,xx),fill=(255,230,120,80))
    for r in out:
        if r.get('missing'):continue
        col=KIND_COL[r['kind']];dr.rectangle(box(r['iy'],r['ix'],r['h'],r['w']),fill=col+(90,),outline=col+(255,),width=3)
        if r.get('door'):dr.rectangle(box(*r['door']),outline=(255,255,255,200),width=1)
    c.alpha_composite(ov);d2=ImageDraw.Draw(c)
    f=ImageFont.truetype(FONT,20);fs=ImageFont.truetype(FONT,12)
    for r in out:
        if r.get('missing'):continue
        small=r['kind'] in('npc','spot','growth','lantern','start')
        lab=r['name'].replace('성장 자리 ','성') if small else r['name']
        d2.text(((r['ix']-x0)*T+2,(r['iy']-y0)*T-13 if small else (r['iy']-y0)*T+3),lab,fill='white',font=fs if small else f,stroke_width=3,stroke_fill='black')
    hd=Image.new('RGBA',(c.width,34),(20,20,24,255));ImageDraw.Draw(hd).text((8,6),title,fill='white',font=f)
    full=Image.new('RGBA',(c.width,c.height+34));full.paste(hd,(0,0));full.paste(c,(0,34))
    OUT.mkdir(parents=True,exist_ok=True);full.save(OUT/f'{slug}.png')
    full.resize((full.width//2,full.height//2),Image.BOX).save(OUT/f'{slug}-half.png')
    return full

def connectivity(d):
    """정적 검사: 마을이 모두 한 길망에 있는지, 길이 맵 경계를 넘는 자리(출입구)."""
    road=d['road'];br=bridge_mask(d,road.shape);net=road|br
    lab,n=ndimage.label(net,structure=np.ones((3,3)))
    towns=list(d['towns']);keys=list(d['keys']);mid=d['mapid'];H,W=road.shape
    comp={}
    for i,t in enumerate(towns):
        m=ndimage.binary_dilation(d['vlabel']==i,iterations=2)&net
        comp[str(t)]=sorted(int(x) for x in np.unique(lab[m]) if x)
    allc=set(sum(comp.values(),[]))
    pts={}
    for oy,ox in((0,1),(1,0)):
        a=net[:H-oy,:W-ox]&net[oy:,ox:];ma=mid[:H-oy,:W-ox];mb=mid[oy:,ox:]
        for yy,xx in np.argwhere(a&(ma!=mb)&(ma>=0)&(mb>=0)):
            k=tuple(sorted((str(keys[ma[yy,xx]]),str(keys[mb[yy,xx]]))));pts.setdefault(k,[]).append((yy,xx))
    gates=[]
    for k,pl in pts.items():
        m_=np.zeros((H,W),bool)
        for yy,xx in pl:m_[yy,xx]=True
        l2,n2=ndimage.label(ndimage.binary_dilation(m_,iterations=2))
        for j in range(1,n2+1):
            yy,xx=np.nonzero(l2==j);gates.append(dict(maps=list(k),x=int(round(xx.mean()))+v.X0,y=int(round(yy.mean()))+v.Y0))
    return dict(allVillagesOneNetwork=len(allc)==1,components=comp,gates=gates)

if __name__=='__main__':
    d=load();allz={};rep={}
    for key,(slug,mapid,items) in VILLAGES.items():
        p,out,lane=plan(d,key,items)
        miss=[r['name'] for r in out if r.get('missing')];blk=[r['name'] for r in out if r.get('blocked')]
        title=f"{key.split('/')[1]} · 맵 {mapid if not key.startswith('hamlet') else p.mapkey} · 마을 {int(p.vm.sum())}칸"
        draw(p,out,lane,slug,title)
        clean=[{k:v_ for k,v_ in r.items() if k not in('iy','ix')} for r in out]
        for r in clean:
            if 'door' in r:r['door']=[r['door'][1]+v.X0,r['door'][0]+v.Y0]
        (OUT/f'{slug}.json').write_text(json.dumps(dict(village=key,map=mapid,items=clean,laneCells=int(lane.sum())),ensure_ascii=False,indent=1))
        np.save(OUT/f'{slug}-lanes.npy',lane)
        allz[key]=dict(slug=slug,map=mapid,items=clean)
        rep[key]=dict(missing=miss,laneBlocked=blk,n=len(out),laneCells=int(lane.sum()))
        print(key,'빠짐',miss,'골목막힘',blk,'골목',int(lane.sum()))
    con=connectivity(d)
    (OUT/'zones.json').write_text(json.dumps(dict(villages=allz,newIds=NEW_IDS),ensure_ascii=False,indent=1))
    (OUT/'check.json').write_text(json.dumps(dict(villages=rep,roads=con),ensure_ascii=False,indent=1))
    print('한 길망',con['allVillagesOneNetwork'],'출입구',len(con['gates']))
