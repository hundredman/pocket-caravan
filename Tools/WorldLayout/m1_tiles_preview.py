"""M1 지형을 기존 타일·개체만으로 칠한 검토 그림(본편 아님).

바닥은 기존 조립 타일 세 가지만 쓴다: 풀밭, 물(바다·강·호수 같은 물), 흙길. 다리는 기존 판자.
숲·산·언덕·습지·밭·꽃밭은 바닥 경계를 새로 만들지 않고, 기존 개체(나무·바위·갈대·논·꽃)를 심어서 표현한다.
색은 고려 맵이 쓰는 goryeo_jade. 이 색으로 구워진 조각이 없는 흙길 조각 몇 개는 기존 jade 굽기와 같은
색 대응표(원본 색 → jade 색, 30색, 겹침 없음)로 검토용으로만 굽는다(승인 대기).

요소별로 끌 수 있다: --no forest,mountain,hill,marsh,field,meadow,grass
출력: ArtReview/M1-Tiles/ (world.png 1/4 축소, world-full.png 실제 크기, cam-*.png 480×270 ×2, report.json)
"""
import argparse,collections,hashlib,json,math,os,sys
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw,ImageFont

HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(HERE))
import terrain_v3 as v
from world_layout import WorldLayout

OUT=ROOT/'ArtReview/M1-Tiles'
BAKED=ROOT/'Assets/PocketCaravan/Art/Baked'
PACK=Path(os.environ.get('NA_PACK',ROOT.parent/'Ninja Adventure - Asset Pack'))/'Backgrounds/Tilesets'
SKIN='goryeo_jade'
T=16
INDEX=json.loads((BAKED/'index.json').read_text())

# ---------- 타일 ----------
_sheets={}
def _native(sheet,x,y):
    name={'floor':'TilesetFloor','water':'TilesetWater','detail':'TilesetFloorDetail','element':'TilesetElement','nature':'TilesetNature'}[sheet]
    if name not in _sheets:_sheets[name]=Image.open(PACK/(name+'.png')).convert('RGBA')
    return _sheets[name].crop((x*T,y*T,(x+1)*T,(y+1)*T))

def _jade_map():
    m={}
    for k,item in INDEX['tiles'].items():
        base,_,skin=k.partition('@')
        if skin!=SKIN:continue
        s,x,y=base.split(':')
        if s not in('floor','water','detail','element','nature'):continue
        nat=_native(s,int(x),int(y));jd=Image.open(ROOT/item['path']).convert('RGBA')
        for a,b in zip(nat.getdata(),jd.getdata()):
            if m.get(a,b)!=b:raise ValueError('jade 색 대응이 하나로 정해지지 않음')
            m[a]=b
    return m

BAKED_FOR_REVIEW={}
_cache={}
def tile(key):
    """key 'floor:1:8' → jade 조각. 구워진 것이 있으면 그대로, 없으면 같은 색 대응표로 검토용 굽기."""
    if key in _cache:return _cache[key]
    fk=key+'@'+SKIN
    if fk in INDEX['tiles']:
        im=Image.open(ROOT/INDEX['tiles'][fk]['path']).convert('RGBA')
    else:
        global JADE
        if 'JADE' not in globals():JADE=_jade_map()
        s,x,y=key.split(':');nat=_native(s,int(x),int(y))
        miss=sorted({p for p in nat.getdata() if p[3] and p not in JADE})
        if miss:raise ValueError(('jade 대응표에 없는 색',key,miss[:5]))
        im=Image.new('RGBA',nat.size);im.putdata([JADE.get(p,p) if p[3] else p for p in nat.getdata()])
        BAKED_FOR_REVIEW[key]=hashlib.sha256(im.tobytes()).hexdigest()
    _cache[key]=im;return im

# 승인된 C7 그림(팀 그림체로 다시 그린 것). 이름:폭:높이(칸)
C7={'willow':'ArtReview/Approved/C7-Batch1-v1/willow.png','forest_bent':'ArtReview/Approved/C7-Batch2-v1/forest_bent.png',
    'forest_round':'ArtReview/Approved/C7-Batch2-v1/forest_round.png','forest_wide':'ArtReview/Approved/C7-Batch2-v1/forest_wide.png',
    'broadleaf':'ArtReview/Approved/C7-Batch3-v1/broadleaf.png','golden_tree':'ArtReview/Approved/C7-Batch3-v1/golden_tree.png',
    'light_tree':'ArtReview/Approved/C7-Batch3-v1/light_tree.png','fallen_log':'ArtReview/Approved/C7-LowNature-v1/fallen_log.png',
    'moss_rock':'ArtReview/Approved/C7-LowNature-v1/moss_rock.png','shrub':'ArtReview/Approved/C7-LowNature-v1/shrub.png',
    'tall_grass':'ArtReview/Approved/C7-LowNature-v1/tall_grass.png','harvest':'ArtReview/Approved/C7-WorkshopProps-v4/harvest.png',
    **{n:f'ArtReview/C7-RemainingBatch-20261027/Native/{n}.png' for n in ('pine_west','pine_east','round_west','round_east','small_rock','lotus','sheaf_jangdan','sheaf_naru')}}
# M3 마을 건물·소품(승인된 C7). 기와집은 v6이 v5를 대신한다.
C7B={'cottage':'C7-Batch1-v1','pavilion':'C7-Batch1-v1','thatch':'C7-Buildings-v5','tile-house':'C7-Buildings-v6',
     'garden_wall':'C7-CourtyardProps-v1','onggi':'C7-CourtyardProps-v1','paper_large':'C7-CourtyardProps-v1',
     'gaekju':'C7-Gaekju-v1','gate':'C7-GateProps-v1','stone_lantern':'C7-GateProps-v1','well':'C7-GateProps-v1',
     'palace':'C7-Palace-v1','shop':'C7-Shop-v1','celadon':'C7-WorkshopProps-v4','harvest':'C7-WorkshopProps-v4','paper_rack':'C7-WorkshopProps-v4'}
def c7b(name):
    p=ROOT/'ArtReview/Approved'/C7B[name]/f'{name}.png' if name in C7B else ROOT/f'ArtReview/C7-RemainingBatch-20261027/Native/{name}.png'
    return Image.open(p).convert('RGBA')
def sprite(name):
    nm=name.split(':')[0]
    if nm in C7:
        im=Image.open(ROOT/C7[nm]).convert('RGBA')
        assert im.size==(int(name.split(':')[1])*T,int(name.split(':')[2])*T),name
        return im
    return Image.open(ROOT/INDEX['sprites'][name]).convert('RGBA')

GRASS=['floor:0:12']*14+['floor:1:12','floor:2:12','floor:3:12','floor:4:12','floor:2:11','floor:3:11']
TUFTS=['detail:0:2','detail:1:2','detail:2:2','detail:3:2']
FLOWERS=['detail:5:2','detail:6:2']

def autorel(m,y,x):
    """tiles.autotile과 같은 규칙. m: (dy,dx)->bool"""
    n,s,w,e=m(-1,0),m(1,0),m(0,-1),m(0,1)
    if n and s and w and e:
        if not m(1,1):return(5,1)
        if not m(1,-1):return(6,1)
        if not m(-1,1):return(5,2)
        if not m(-1,-1):return(6,2)
        return(1,1)
    if n and s and e:return(0,1)
    if n and s and w:return(2,1)
    if s and w and e:return(1,0)
    if n and w and e:return(1,2)
    if s and e:return(0,0)
    if s and w:return(2,0)
    if n and e:return(0,2)
    if n and w:return(2,2)
    return None   # 2×2 규칙을 지키면 여기 오지 않는다

def blocks2(mask):
    """2×2 덩어리에 속하는 칸"""
    a=mask[:-1,:-1]&mask[1:,:-1]&mask[:-1,1:]&mask[1:,1:];c=np.zeros_like(mask)
    c[:-1,:-1]|=a;c[1:,:-1]|=a;c[:-1,1:]|=a;c[1:,1:]|=a;return c

def fill_diagonals(mask,allowed):
    """두 옆 칸이 같은 지형이면 그 사이 대각선 칸도 같은 지형이어야 한다(가장자리+안쪽 모서리를 한 칸에 겹치는 조각이 없음).
    예외: 네 옆이 모두 같으면 빠진 대각선 하나는 안쪽 모서리 조각으로 그린다."""
    mask=mask.copy();H,W=mask.shape
    for _ in range(20):
        p=np.pad(mask,1)
        n,s,w,e=p[:-2,1:-1],p[2:,1:-1],p[1:-1,:-2],p[1:-1,2:]
        ne,nw,se,sw=p[:-2,2:],p[:-2,:-2],p[2:,2:],p[2:,:-2]
        four=n&s&w&e
        need=[(n&e&~ne,(-1,1)),(n&w&~nw,(-1,-1)),(s&e&~se,(1,1)),(s&w&~sw,(1,-1))]
        miss=sum(m_.astype(int) for m_,_ in need)
        changed=False
        for m_,(dy,dx) in need:
            bad=mask&m_&~(four&(miss==1))
            for y,x in np.argwhere(bad):
                yy,xx=y+dy,x+dx
                if 0<=yy<H and 0<=xx<W:
                    if allowed[yy,xx]:mask[yy,xx]=True;changed=True
                    else:mask[y,x]=False;changed=True
        if not changed:break
    return mask

def tileready(mask,allowed):
    """모든 칸이 2×2 덩어리에 속하게 넓힌다(1칸 줄기·외톨이 없음 → 기존 조립 조각만으로 그려짐)."""
    mask=mask.copy()
    for _ in range(12):
        lone=mask&~blocks2(mask)
        if not lone.any():break
        for y,x in np.argwhere(lone):
            best=None
            for dy in(-1,0):
                for dx in(-1,0):
                    y0,x0=y+dy,x+dx
                    if y0<0 or x0<0 or y0+2>mask.shape[0] or x0+2>mask.shape[1]:continue
                    sq=(slice(y0,y0+2),slice(x0,x0+2))
                    if not allowed[sq].all():continue
                    sc=mask[sq].sum()
                    if best is None or sc>best[0]:best=(sc,sq)
            if best:mask[best[1]]=True
            else:mask[y,x]=False
    # 대각선으로만 닿는 체커보드 모양 정리
    for _ in range(4):
        d=(mask[:-1,:-1]&mask[1:,1:]&~mask[:-1,1:]&~mask[1:,:-1])|(mask[:-1,1:]&mask[1:,:-1]&~mask[:-1,:-1]&~mask[1:,1:])
        if not d.any():break
        ys,xs=np.nonzero(d)
        for y,x in zip(ys,xs):
            for yy,xx in((y,x),(y,x+1),(y+1,x),(y+1,x+1)):
                if allowed[yy,xx]:mask[yy,xx]=True
    return mask

# ---------- 개체 ----------
# 이름: (스프라이트, 줄기 칸 폭)  줄기 = 그림 맨 아랫줄 가운데
TREES_FOREST=['forest_round:3:4','forest_bent:3:4','forest_wide:4:3','broadleaf:4:4','light_tree:3:4','round_west:4:3','round_east:4:3','golden_tree:3:4','fallen_log:3:2','shrub:2:2']
TREES_MOUNT=['pine_west:4:3','pine_east:4:3','pine_west:4:3','pine_east:4:3','forest_bent:3:4','moss_rock:3:2','small_rock:2:2']
TREES_HILL=['shrub:2:2','shrub:2:2','small_rock:2:2','forest_round:3:4','light_tree:3:4']
TREES_LONE=['broadleaf:4:4','willow:4:4','light_tree:3:4','golden_tree:3:4','shrub:2:2','forest_round:3:4']
REEDS='tall_grass:2:2';LOTUS='lotus:2:1';SHEAF='harvest:3:2'

# 다리별 진입로 조정(세계 좌표 중심 → 걷어 낼 반지름, 이을 길 최대 거리, 곧게 나오는 길이). 은샘 지시로 나루 다리(2번)만.
APPROACH={(252,42,-1):(11,22,9),(252,42,1):(7,16,5)}   # 끝: +1 = 다리 방향 앞쪽, -1 = 뒤쪽
P=6   # 숲 합성 블록 크기(칸). 이 블록이 반복되는 '숲 타일'
FOREST_BLOCK=['forest_round:3:4','forest_bent:3:4','broadleaf:4:4','round_west:4:3','round_east:4:3','forest_wide:4:3','light_tree:3:4']
MOUNT_BLOCK=['pine_west:4:3','pine_east:4:3','pine_west:4:3','pine_east:4:3','forest_bent:3:4']
BLOCKS={};FMASK={}

def noise_field(H,W,scale,seed=21):
    from scipy import ndimage as nd
    r=np.random.default_rng(seed);g=r.random((H//scale+2,W//scale+2))
    z=nd.zoom(g,(H/g.shape[0]*1.02,W/g.shape[1]*1.02),order=3)[:H,:W]
    return (z-z.min())/(z.max()-z.min()+1e-9)

def clean(m,minsize):
    m=ndimage.binary_closing(ndimage.binary_opening(m),iterations=1)&m|ndimage.binary_opening(m)
    lab,n=ndimage.label(m)
    if n:
        sizes=ndimage.sum(np.ones_like(lab),lab,range(1,n+1))
        for i,sz in enumerate(sizes,1):
            if sz<minsize:m[lab==i]=False
    return m

def forest_block(kinds,seed,sparse=False):
    """승인된 나무 그림을 빽빽하게 겹쳐 P×P칸 반복 블록을 만든다(가장자리가 이어지도록 감아 그림).
    lat: 블록 안 나무 자리(밑동 칸) → 나무 이름. 같은 자리를 숲 가장자리에서 통째로 다시 그려 경계를 자연스럽게."""
    r=np.random.default_rng(seed);lat={}
    # 2칸마다 한 그루 + 엇갈린 줄에 한 그루 더(틈이 보이지 않을 만큼 빽빽하게)
    for gy in range(0,P,2):
        for gx in range(0,P,2):
            for oy,ox in((0,0),(1,1)):
                if sparse and (oy,ox)==(1,1) and r.random()<.65:continue   # 성긴 숲: 엇갈린 줄 나무를 많이 뺌
                lat[((gy+oy)%P,(gx+ox)%P)]=kinds[int(r.integers(len(kinds)))]
    ims={k:sprite(k) for k in set(lat.values())}
    # 바탕: 나무 그림의 가장 어두운 잎 색(그림 사이 틈을 숲 그늘처럼)
    px=[p for k in ims for p in ims[k].getdata() if p[3]==255]
    import collections as C
    dark=[c for c,_ in C.Counter(px).most_common(40) if sum(c[:3])<240 and c[1]>c[0]]
    bg=dark[0] if dark else (40,70,50,255)
    blk=Image.new('RGBA',(P*T,P*T),bg)
    inst=sorted((y+oy,x+ox,k) for (y,x),k in lat.items() for oy in(-P,0,P) for ox in(-P,0,P))   # 감아 그린 것까지 밑동 줄 순서로
    for y,x,k in inst:
        im=ims[k];sw,sh=im.width//T,im.height//T
        _paste(blk,im,(x-sw//2)*T,(y-sh+1)*T)
    return blk,lat,bg

def piece_sheet(kind,lat,bg):
    """숲 타일 조각: 네모난 숲을 그려 가장자리별로 나눠 보여 준다.
    위 가장자리(풀밭 위로 나온 나뭇잎), 이어지는 안쪽(P×P 반복), 아래 시작 부분(풀밭 위 밑동), 왼쪽·오른쪽 가장자리."""
    m0,mx=4,3;Wc,Hc=3*P+2*mx,3*P+m0+2
    im=Image.new('RGBA',(Wc*T,Hc*T))
    for y in range(Hc):
        for x in range(Wc):im.alpha_composite(tile('floor:0:12'),(x*T,y*T))
    y0,y1,x0,x1=m0,m0+3*P-1,mx,mx+3*P-1
    for y in range(y0,y1-1):
        for x in range(x0+1,x1):im.paste(bg,(x*T,y*T,(x+1)*T,(y+1)*T))
    inst=[]
    for y in range(y0,y1+1):
        for x in range(x0,x1+1):
            k=lat.get(((y-y0)%P,(x-x0)%P))
            if k:inst.append((y,x,k))
    for y,x,k in sorted(inst):
        s_=sprite(k);sw,sh=s_.width//T,s_.height//T;_paste(im,s_,(x-sw//2)*T,(y-sh+1)*T)
    Z=2;big=Image.new('RGBA',(im.width*Z+300,im.height*Z),(30,34,40,255));big.paste(im.resize((im.width*Z,im.height*Z),Image.NEAREST),(0,0))
    d=ImageDraw.Draw(big);f=ImageFont.truetype(str(v.FONT),22)
    def band(r0,r1,c,label):
        d.rectangle([2,r0*T*Z,im.width*Z-3,r1*T*Z],outline=c,width=3);d.text((im.width*Z+10,(r0*T*Z+r1*T*Z)//2-12),label,fill=c,font=f)
    band(y0-3,y0,(255,230,80,255),'위 가장자리')
    band(y0,y0+P,(120,220,140,255),'이어지는 부분')
    band(y1-2,y1+1,(255,140,90,255),'아래 시작 부분')
    for xx in(x0-2,x0+2,x1-1,x1+3):d.line([(xx*T*Z,y0*T*Z),(xx*T*Z,(y1-2)*T*Z)],fill=(120,200,255,255),width=2)
    d.text((im.width*Z+10,(y0+P+3)*T*Z),'파란 선: 왼쪽·',fill=(120,200,255,255),font=f);d.text((im.width*Z+10,(y0+P+3)*T*Z+28),'오른쪽 가장자리',fill=(120,200,255,255),font=f)
    return big

def _paste(dst,im,x,y):
    sx,sy=max(0,-x),max(0,-y)
    if sx>=im.width or sy>=im.height:return
    dst.alpha_composite(im.crop((sx,sy,im.width,im.height)),(x+sx,y+sy))

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--no',default='');ap.add_argument('--zones',action='store_true',help='M3: M2 구역도의 건물·광장을 그린다(ArtReview/M3-Village)');a=ap.parse_args()
    global OUT
    if a.zones:OUT=ROOT/'ArtReview/M3-Village'
    if os.environ.get('M4_REVIEW_ROOT'):OUT=Path(os.environ['M4_REVIEW_ROOT'])
    asked=set(filter(None,a.no.split(',')));off=asked|{'field'}   # 들판(논)은 2026-10-28 은샘 지시로 뺌
    v.VARIANT['area']=v.SMALL_CENTRAL;v.VARIANT['hamlets']=v.HAMLETS
    layout=WorldLayout.read();w=v.build(layout);r=v.roads(layout,w);v.partition(layout,w,r)
    # 다른 문명 연결 자리(m4_links): 대각선 경계를 곧게 깎는다. 깎은 담당 범위(own_n·mapid_n)는 층을 내보낼 때만 쓴다.
    # 나무·풀포기 같은 무작위 배치는 연결 자리가 없던 때와 같은 마스크로 정하고(다른 맵 그림이 바뀌지 않게),
    # 연결 자리·새 흙길과 겹친 것만 뺀다.
    LINKS=[];own_n=w['owned'];mapid_n=w['mapid']
    if a.zones:
        import m4_links;own_n=w['owned'].copy();mapid_n=w['mapid'].copy();LINKS=m4_links.notch(own_n,mapid_n,w['keys'])
    t=w['t'];own=w['owned'];H,W=t.shape;rng=np.random.default_rng(11)
    def inside(y,x):return 0<=y<H and 0<=x<W
    water0=np.isin(t,[v.DEEP,v.SHALLOW,v.RIVER,v.LAKE])&own
    road0=r['road']&own
    # 물: 2×2 규칙(가는 개울은 2칸으로 넓힘). 길은 물 위에서 다리로.
    # 물과 땅 둘 다 2×2 규칙을 지켜야 모서리 조각이 맞는다(땅 1칸 돌기가 있으면 물에 네모난 홈이 생김)
    water=water0.copy()
    for _ in range(4):
        water=fill_diagonals(tileready(water,own),own)
        land_=tileready(own&~water,own)
        if not (own&~water&~land_).any():break
        water|=own&~land_
    bridge=road0&water
    road=road0&~water
    for _ in range(4):
        road=fill_diagonals(tileready(road,own&~water),own&~water)
        rest=tileready(own&~water&~road,own&~water)
        if not (own&~water&~road&~rest).any():break
        road|=own&~water&~road&~rest
    # 다리: 자리마다 길 방향(각도)에 맞춰 따로 만든다. 지금 게임의 목조 다리(3칸 폭 판자 + 난간)를
    # 필요한 길이로 이어 붙인 뒤 그 각도로 돌려 통짜 개체로 굽는다. 길이는 길 방향으로 실제 물을 건너는 길이 + 양 끝 1칸.
    spans=[]   # (중심 y, 중심 x, 각도(도, 화면 좌표), 길이 칸)
    for path in r['paths']:
        wet=[i for i,q in enumerate(path) if water[q]]
        if not wet:continue
        runs=[];st=wet[0];pv=wet[0]
        for i in wet[1:]:
            if i>pv+2:runs.append((st,pv));st=i
            pv=i
        runs.append((st,pv))
        for i0,i1 in runs:
            a_=path[max(0,i0-4)];b_=path[min(len(path)-1,i1+4)]
            th=math.atan2(b_[0]-a_[0],b_[1]-a_[1])
            cy,cx=(path[i0][0]+path[i1][0])/2,(path[i0][1]+path[i1][1])/2
            if any(abs(cy-c[0])<4 and abs(cx-c[1])<4 for c in spans):continue   # 겹친 길(합류)은 한 번만
            uy,ux=math.sin(th),math.cos(th)
            def reach(sg):
                k=0.0
                while True:
                    yy,xx=int(round(cy+sg*uy*(k+.5))),int(round(cx+sg*ux*(k+.5)))
                    if not inside(yy,xx) or not water[yy,xx]:return k
                    k+=.5
            L=reach(1)+reach(-1)+2
            # 물 칸 중심이 아니라 건너는 구간의 가운데로 맞춘다
            sh2=(reach(1)-reach(-1))/2;cy+=uy*sh2;cx+=ux*sh2
            # 양 끝 칸이 물(물가 조각)이면 땅이 나올 때까지 늘린다(최대 3칸)
            for _ in range(6):
                n_=int(math.ceil(L))+1
                ends=[(int(round(cy+sg*uy*(n_/2-.5))),int(round(cx+sg*ux*(n_/2-.5)))) for sg in(1,-1)]
                if all(inside(*e) and not water[e] for e in ends):break
                L+=1
            spans.append((cy,cx,math.degrees(th),L))
    bridge_mask=np.zeros_like(water)
    for cy,cx,deg,L in spans:
        uy,ux=math.sin(math.radians(deg)),math.cos(math.radians(deg))
        for k in np.arange(-L/2-1,L/2+1.01,.5):
            for wv in np.arange(-2,2.01,.5):
                yy,xx=int(round(cy+uy*k-ux*wv)),int(round(cx+ux*k+uy*wv))
                if inside(yy,xx):bridge_mask[yy,xx]=True
    # 물 위의 길은 없앤다(다리가 대신). 다리 근처가 아닌 물가 2칸 안의 길도 지운다(물가를 따라가지 않게).
    road&=~water
    road&=~(ndimage.binary_dilation(water,iterations=2)&~ndimage.binary_dilation(bridge_mask,iterations=2))
    # 다리 양 끝에서 길 방향으로 곧게 이어 길과 만나게 한다(최대 8칸)
    for cy,cx,deg,L in spans:
        uy,ux=math.sin(math.radians(deg)),math.cos(math.radians(deg))
        for sg in(1,-1):
            k=L/2-.5;hit=False
            while k<L/2+8 and not hit:
                yy,xx=int(round(cy+sg*uy*k)),int(round(cx+sg*ux*k))
                if not inside(yy,xx):break
                if k>L/2+1 and road[yy,xx]:hit=True
                for dy in(-1,0,1):
                    for dx in(-1,0,1):
                        if inside(yy+dy,xx+dx) and own[yy+dy,xx+dx] and not water[yy+dy,xx+dx]:road[yy+dy,xx+dx]=True
                k+=.5
    road=fill_diagonals(tileready(road,own&~water),own&~water)
    deck=np.zeros_like(water)
    bridge=bridge_mask
    # 외톨이 길 조각(4칸 미만 덩어리) 제거
    lab,n_=ndimage.label(road&~bridge)
    for i_,sl in enumerate(ndimage.find_objects(lab),1):
        if (lab[sl]==i_).sum()<4:road[sl][lab[sl]==i_]=False
    # 다리 진입로 다듬기: 다리 끝 둘레(반지름 5칸)의 길을 걷어 내고, 다리 방향 그대로 3칸 곧게 나온 뒤
    # 바깥쪽 길(5~14칸, 다리 앞쪽)로 3칸 폭 곧은 길로 잇는다. 다리 밑 기슭 칸도 길로 둔다.
    def stroke(p0,p1):
        n=int(max(abs(p1[0]-p0[0]),abs(p1[1]-p0[1]))*2)+1
        for i in range(n+1):
            yy=p0[0]+(p1[0]-p0[0])*i/n;xx=p0[1]+(p1[1]-p0[1])*i/n
            for dy in(-1,0,1):
                for dx in(-1,0,1):
                    q=(int(round(yy+dy)),int(round(xx+dx)))
                    if inside(*q) and own[q] and not water[q]:road[q]=True
    yy_,xx_=np.mgrid[:H,:W]
    for cy,cx,deg,L in spans:
        uy,ux=math.sin(math.radians(deg)),math.cos(math.radians(deg));half=(int(math.ceil(L))+2)/2
        for sg in(1,-1):
            ey,ex=cy+sg*uy*(half-.5),cx+sg*ux*(half-.5)
            dd=np.hypot(yy_-ey,xx_-ex)
            # 바깥 길: 다리 앞쪽(다리 방향과 같은 쪽)에서 5~14칸
            ahead=((yy_-ey)*sg*uy+(xx_-ex)*sg*ux)>0
            R0,R1,A=APPROACH.get((int(round(cx+v.X0)),int(round(cy+v.Y0)),sg),(5,14,3))
            cand=np.argwhere(road&(dd>=R0)&(dd<=R1)&ahead&~water)
            if not len(cand):continue
            # 다리 축에 가까우면서 가까운 길
            perp_=np.abs((cand[:,0]-ey)*ux-(cand[:,1]-ex)*uy)
            sc=np.hypot(cand[:,0]-ey,cand[:,1]-ex)+1.5*perp_
            ty,tx=cand[int(np.argmin(sc))]
            road[(dd<R0)&ahead&~water]=False   # 이 끝 앞쪽만 걷어 냄(반대쪽 끝 진입로는 그대로)
            road[(dd<3)&~ahead&~water]=False   # 다리 끝 옆으로 남은 길 돌기도 걷어 냄
            a_=(ey+sg*uy*A,ex+sg*ux*A)
            stroke((ey,ex),a_);stroke(a_,(ty,tx))
        for k in np.arange(-half,half+.01,.5):
            for wv in(0,):   # 다리 밑은 가운데 줄만(기울어진 다리 옆으로 길이 삐져나오지 않게)
                q=(int(round(cy+uy*k-ux*wv)),int(round(cx+ux*k+uy*wv)))
                if inside(*q) and own[q] and not water[q]:road[q]=True
    road=fill_diagonals(tileready(road,own&~water),own&~water)
    # 진입로를 고치며 떨어져 나온 작은 길 조각(12칸 미만) 제거
    lab,n_=ndimage.label(road,structure=np.ones((3,3)))
    for i_,sl in enumerate(ndimage.find_objects(lab),1):
        if (lab[sl]==i_).sum()<12:road[sl][lab[sl]==i_]=False
    Z=None;ZR=None;linkban=np.zeros((H,W),bool)
    if a.zones:
        Z=zones_masks(own,water,road,H,W,LINKS)
        ZR=zones_masks(own,water,road,H,W) if LINKS else Z
        road=Z['road']
        linkban=ndimage.binary_dilation((Z['road']^ZR['road'])|(Z['reserve']&~ZR['reserve'])|(own&~own_n),iterations=1)
    roadR=ZR['road'] if ZR is not None else road   # 무작위 배치용(연결 자리 전)
    land=own&~water
    rep=dict(waterAdded=int((water&~water0).sum()),roadAdded=int((road&~road0).sum()),bridgeCells=int(bridge.sum()))
    img=Image.new('RGBA',(W*T,H*T),(28,34,40,255))
    LAY=os.environ.get('M4_LAYERS')   # M4: 게임 층(바닥·장식·아래 개체·위 개체)으로 따로 그린다
    PARTS={}   # M4 인증: (층, y, x) → [(원본, dx, dy)] 원본 그림을 칸 왼쪽 위 기준 어디에 통째로 놓았는지(그린 순서)
    def part(layer,y,x,src,dx=0,dy=0):
        if LAY:PARTS.setdefault((layer,int(y),int(x)),[]).append((src,int(dx),int(dy)))
    pieces=collections.Counter();bad=collections.Counter()
    def mk(mask,y,x,center):
        def m(dy,dx):
            yy,xx=y+dy,x+dx
            if not inside(yy,xx) or not own[yy,xx]:return center   # 담당 밖은 이어진 것으로
            return bool(mask[yy,xx])
        return m
    for y in range(H):
        for x in range(W):
            if not own[y,x]:continue
            g=GRASS[(x*7+y*13+(x*y)%5)%len(GRASS)]
            if water[y,x]:
                # 물 조각: '풀 위의 물' 조립 세트(원점 water 0,6)
                rel=autorel(mk(water,y,x,True),y,x)
                if rel is None:bad['water']+=1;rel=(1,1)
                key=f'water:{rel[0]}:{rel[1]+6}'
                img.alpha_composite(tile(key),(x*T,y*T));pieces[key]+=1;part('ground',y,x,'tile:'+key)
            elif road[y,x]:
                rel=autorel(mk(road,y,x,True),y,x)
                if rel is None:bad['road']+=1;img.alpha_composite(tile(g),(x*T,y*T));part('ground',y,x,'tile:'+g);continue
                key=f'floor:{rel[0]}:{rel[1]+7}'
                img.alpha_composite(tile(key),(x*T,y*T));pieces[key]+=1;part('ground',y,x,'tile:'+key)
            else:
                img.alpha_composite(tile(g),(x*T,y*T));part('ground',y,x,'tile:'+g)
    if LAY:
        gimg=img.copy();dimg=Image.new('RGBA',img.size,(0,0,0,0))
        deck=np.zeros((H,W),bool)
    BRIDGES=[]
    for cy,cx,deg,L in spans:
        n_=max(3,int(math.ceil(L))+2)   # 양쪽 기슭 길 끝을 반 칸씩 덮게
        im=Image.new('RGBA',(3*T,n_*T))
        for y in range(n_):
            for x in range(3):
                sy=12 if y==0 else 14 if y==n_-1 else 13
                im.alpha_composite(gameart(f'water_{x}_{sy}_wood'),(x*T,y*T))
                if x in(0,2):im.alpha_composite(gameart(f"wood_rail{'vleft' if x==0 else 'vright'}_{1 if y in(0,n_-1) else 0}"),(x*T,y*T))
        rot=im.rotate(90-deg,resample=Image.NEAREST,expand=True)   # 세로 그림(아래 방향=90°)을 길 각도로
        BRIDGES.append(rot)
        bx_,by_=int(round((cx+.5)*T-rot.width/2)),int(round((cy+.5)*T-rot.height/2))
        img.alpha_composite(rot,(bx_,by_))
        if LAY:
            dimg.alpha_composite(rot,(bx_,by_))
            bi=len(BRIDGES)-1;ra_=np.array(rot)[:,:,3]
            for cyy in range(by_//T,(by_+rot.height-1)//T+1):
                for cxx in range(bx_//T,(bx_+rot.width-1)//T+1):
                    sub=ra_[max(0,cyy*T-by_):max(0,(cyy+1)*T-by_),max(0,cxx*T-bx_):max(0,(cxx+1)*T-bx_)]
                    if sub.size and sub.max()>0:part('deco',cyy,cxx,f'bridge:{bi}',bx_-cxx*T,by_-cyy*T)
            # 다리 판자가 칸의 절반 이상 덮으면 걸을 수 있는 칸
            al=np.zeros((H*T,W*T),np.uint8);ra=np.array(rot)[:,:,3]
            y0_,x0_=max(0,by_),max(0,bx_);y1_,x1_=min(H*T,by_+ra.shape[0]),min(W*T,bx_+ra.shape[1])
            al[y0_:y1_,x0_:x1_]=ra[y0_-by_:y1_-by_,x0_-bx_:x1_-bx_]
            cov=(al>0).reshape(H,T,W,T).mean(axis=(1,3))
            deck|=cov>=.5
    rep['bridges']=[dict(x=round(cx+v.X0,1),y=round(cy+v.Y0,1),angle=round((deg+360)%180,1),length=int(math.ceil(L))+2) for cy,cx,deg,L in spans]
    rep['pieces']=dict(pieces);rep['unmatched']=dict(bad)
    def seams(mask):
        p=np.pad(mask,1,mode='edge');n,s_,w_,e=p[:-2,1:-1],p[2:,1:-1],p[1:-1,:-2],p[1:-1,2:]
        ne,nw,se,sw=p[:-2,2:],p[:-2,:-2],p[2:,2:],p[2:,:-2]
        need=[n&e&~ne,n&w_&~nw,s_&e&~se,s_&w_&~sw];miss=sum(x_.astype(int) for x_ in need);four=n&s_&w_&e
        return int((mask&own&((miss>1)|((miss==1)&~four))).sum())
    rep['seamCells']=dict(water=seams(water),road=seams(road))
    if os.environ.get('SEAMDBG'):
        p_=np.pad(road,1,mode='edge');n,s_,w_,e=p_[:-2,1:-1],p_[2:,1:-1],p_[1:-1,:-2],p_[1:-1,2:];ne,nw,se,sw=p_[:-2,2:],p_[:-2,:-2],p_[2:,2:],p_[2:,:-2]
        miss=sum(x_.astype(int) for x_ in [n&e&~ne,n&w_&~nw,s_&e&~se,s_&w_&~sw]);four=n&s_&w_&e
        print('SEAM',[(int(x+v.X0),int(y+v.Y0)) for y,x in np.argwhere(road&own&((miss>1)|((miss==1)&~four)))])
    # ---------- 개체 심기 ----------
    near_road=ndimage.binary_dilation(roadR,iterations=1);near_water=ndimage.binary_dilation(water,iterations=1)
    village=w['village']
    free=land&~near_road&~near_water
    occ=ndimage.binary_dilation(bridge_mask,iterations=1).copy()
    if Z is not None:
        free&=~ZR['reserve'];occ|=ZR['reserve']
        objs_z=Z['objs']   # 줄기·덮인 칸. 다리 밑·둘레엔 풀포기·꽃·나무를 두지 않음(다리 위에 풀이 그려지지 않게)
    objs=[]
    def place(name,y,x,margin=0):
        nm,sw,sh=name.split(':');sw,sh=int(sw),int(sh)
        x0=x-sw//2;y0=y-sh+1
        ys,xs=slice(max(0,y0),y+1),slice(max(0,x0),x0+sw)
        if y0<0 or x0<0 or x0+sw>W:return False
        foot=(slice(y-min(sh-1,1),y+1),slice(x0,x0+sw))   # 아래 두 줄
        if not free[foot].all() or occ[ys,xs].any():return False
        occ[max(0,y0-margin):y+1+margin,max(0,x0-margin):x0+sw+margin]=True
        if not linkban[ys,xs].any():objs.append((y,x0,y0,name))
        return True
    def scatter(mask,kinds,density,margin=0,weights=None):
        cand=np.argwhere(mask);rng.shuffle(cand);n=0
        target=int(len(cand)*density)
        for y,x in cand:
            if n>=target:break
            k=kinds[rng.choice(len(kinds),p=weights)] if weights else kinds[rng.integers(len(kinds))]
            if place(k,y,x,margin):n+=1
        return n
    cls=lambda *ks:np.isin(t,ks)&own&~village
    counts={}
    for kind,ks,mask_ in (('mountain',MOUNT_BLOCK,cls(v.MOUNTAIN,v.PEAK)&land),('forest',FOREST_BLOCK,cls(v.FOREST)&land)):
        if kind in off:continue
        fm=clean(mask_&~near_road&~near_water&~occ&(~ZR['canopy'] if Z is not None else True),12)
        blk,lat,bg=forest_block(ks,seed=3 if kind=='forest' else 5)
        # 바탕(숲 그늘): 아래 2칸까지 숲인 칸만. 숲 맨 아래 줄은 풀밭 위에 밑동이 보인다(시작 부분).
        below=np.zeros_like(fm);below[:-2]=fm[1:-1]&fm[2:]
        under=ndimage.binary_erosion(fm,iterations=1)&below
        for y,x in np.argwhere(under&~linkban):
            img.paste(bg,(x*T,y*T,(x+1)*T,(y+1)*T))
            if LAY:
                gimg.paste(bg,(x*T,y*T,(x+1)*T,(y+1)*T))
                PARTS[('ground',int(y),int(x))]=[('fill:%02x%02x%02x%02x'%tuple(bg),0,0)]   # 숲 그늘: 칸 전체를 한 색으로(바닥 조각을 덮음)
        FMASK[kind]=fm
        # 나무는 모두 통째로, 밑동 줄 순서대로(뒤 나무 위에 앞 나무). 블록과 같은 자리라 안쪽은 숲 타일과 같은 그림이 된다.
        n=0
        for y,x in np.argwhere(fm):
            k=lat.get((y%P,x%P))
            if k is None:continue
            nm,sw,sh=k.split(':');sw,sh=int(sw),int(sh);x0=x-sw//2;y0=y-sh+1
            if x0<0 or y0<0 or x0+sw>W:continue
            if linkban[max(0,y0):y+1,x0:x0+sw].any():continue
            objs.append((y,x0,y0,k));n+=1
        occ|=ndimage.binary_dilation(fm,iterations=1);counts[kind]=dict(cells=int(fm.sum()),trees=n)
        BLOCKS[kind]=(blk,lat,bg,ks)
    if 'source' not in off:
        # 강의 수원지(강 줄기마다 가장 높은 곳)를 조금 성긴 숲으로 덮는다: 강이 숲 속에서 시작하는 것처럼
        rv=(t==v.RIVER)&water0;lab,nr=ndimage.label(rv,structure=np.ones((3,3)))
        srcs=[];near_lake=ndimage.binary_dilation(w['lake'],iterations=3)
        # 호수로 드는 강: 수원 산 둘레(산·봉우리 가까이)에서 시작
        inflow_mask=ndimage.binary_dilation(np.isin(t,[v.MOUNTAIN,v.PEAK]),iterations=12)
        for i_ in range(1,nr+1):
            cc=np.argwhere(lab==i_)
            if len(cc)<6:continue
            if w['lake'][ndimage.binary_dilation(lab==i_,iterations=3)].any():
                # 호수에서 나가는 강은 호수 쪽이 아니라 바다 쪽으로 흐르므로 수원지가 없다. 호수로 드는 강은 호수 반대 끝이 수원지.
                far=np.hypot(*(cc-np.argwhere(w['lake']).mean(0)).T)
                top=tuple(cc[int(np.argmax(far))])
                if not inflow_mask[top]:continue
            else:
                top=tuple(cc[int(np.argmax(w['elev'][tuple(cc.T)]))])
            srcs.append(top)
        yy2,xx2=np.mgrid[:H,:W];sn=noise_field(H,W,5)
        sm=np.zeros((H,W),bool)
        for sy,sx in srcs:sm|=np.hypot(yy2-sy,xx2-sx)<8+3*(sn-.5)
        taken=np.zeros((H,W),bool)
        for fm_ in FMASK.values():taken|=fm_
        sm&=land&~roadR&~ndimage.binary_dilation(roadR,iterations=1)&~village&~taken
        if Z is not None:sm&=~ZR['canopy']
        blk,lat,bg=forest_block(FOREST_BLOCK,seed=9,sparse=True);n=0
        for y,x in np.argwhere(sm):
            k=lat.get((y%P,x%P))
            if k is None:continue
            nm,sw,sh=k.split(':');sw,sh=int(sw),int(sh);x0=x-sw//2;y0=y-sh+1
            if x0<0 or y0<0 or x0+sw>W:continue
            if linkban[max(0,y0):y+1,x0:x0+sw].any():continue
            objs.append((y,x0,y0,k));n+=1
        occ|=ndimage.binary_dilation(sm,iterations=1)
        counts['source']=dict(sources=[[int(sx+v.X0),int(sy+v.Y0)] for sy,sx in srcs],trees=n)
        BLOCKS['source']=(blk,lat,bg,FOREST_BLOCK);FMASK['source']=sm
    if 'hill' not in off:counts['hill']=scatter(cls(v.HILL)&free,TREES_HILL,.025,margin=1)
    if 'marsh' not in off:
        mm=np.isin(t,[v.MARSH])&own&land&~near_road
        counts['marsh']=scatter(mm&~occ,[REEDS],.12)
        # 호수 물가 연꽃
        lake_edge=w['lake']&water&ndimage.binary_dilation(land,iterations=3)&~ndimage.binary_dilation(land,iterations=1)
        n=0;cand=np.argwhere(lake_edge);rng.shuffle(cand)
        for y,x in cand[:30]:
            if x+1<W and lake_edge[y,x+1] and not occ[y,x:x+2].any():
                objs.append((y,x,y,LOTUS));occ[y,x:x+2]=True;n+=1
        counts['lotus']=n
    if 'field' not in off:
        # 논: 사람이 만든 구획처럼 같은 크기 직사각형을 줄 맞춰 놓는다(사이 1칸 둑).
        # 한 구획 = 기존 볏짚 바닥 3×3 조각(element 13~15, 4~6)을 가장자리·모서리·가운데로 나눠 늘린 직사각형.
        fm=np.isin(t,[v.FIELD])&land&~near_road&~village&~occ&~ndimage.binary_dilation(water,iterations=0) if False else np.isin(t,[v.FIELD])&land&~near_road&~near_water&~village&~occ
        fm=ndimage.binary_closing(fm,iterations=2)&land&~near_road&~near_water&~village&~occ   # 들판 안 작은 틈 메움
        PW,PH=6,4;n=0;sh_=0
        lab,nc=ndimage.label(fm)
        for ci,sl in enumerate(ndimage.find_objects(lab),1):
            comp=lab==ci
            # 구획 줄의 시작점을 들판마다 골라 구획이 가장 많이 들어가게(줄은 반듯하게 유지)
            best=[];bsz=(PW,PH)
            for pw,ph in((6,4),(4,6),(5,3),(3,5)):   # 들판 모양에 맞는 구획 크기·방향(한 들판 안에서는 모두 같게)
                for oy in range(ph+1):
                    for ox in range(pw+1):
                        got=[]
                        for y in range(sl[0].start+oy,sl[0].stop-ph+1,ph+1):
                            for x in range(sl[1].start+ox,sl[1].stop-pw+1,pw+1):
                                if comp[y:y+ph,x:x+pw].all():got.append((y,x))
                        if len(got)*pw*ph>len(best)*bsz[0]*bsz[1]:best=got;bsz=(pw,ph)
            PW,PH=bsz
            for y,x in best:
                for dy in range(PH):
                    for dx in range(PW):
                        ex=13 if dx==0 else 15 if dx==PW-1 else 14
                        ey=4 if dy==0 else 6 if dy==PH-1 else 5
                        img.alpha_composite(tile(f'element:{ex}:{ey}'),((x+dx)*T,(y+dy)*T))
                occ[y:y+PH,x:x+PW]=True;n+=1
                if rng.random()<.35 and x+PW<W and not occ[y+PH-2:y+PH,x+PW].any():
                    objs.append((y+PH-1,x+PW,y+PH-2,'oldsheaf'));sh_+=1
        counts['field']=dict(plots=n,sheaves=sh_)
    if 'meadow' not in off:
        mm=np.isin(t,[v.MEADOW])&free&~occ;n=0
        for y,x in np.argwhere(mm):
            if rng.random()<.10:
                fk=FLOWERS[rng.integers(2)];tt=tile(fk)
                if linkban[y,x]:continue
                img.alpha_composite(tt,(x*T,y*T));n+=1
                if LAY:dimg.alpha_composite(tt,(x*T,y*T));part('deco',y,x,'tile:'+fk)
        counts['flowers']=n
    if 'grass' not in off:
        gm=land&~roadR&~occ;n=0
        for y,x in np.argwhere(gm):
            if rng.random()<.035:
                fk=TUFTS[rng.integers(4)];tt=tile(fk)
                if linkban[y,x]:continue
                img.alpha_composite(tt,(x*T,y*T));n+=1
                if LAY:dimg.alpha_composite(tt,(x*T,y*T));part('deco',y,x,'tile:'+fk)
        counts['tufts']=n
        counts['loneTrees']=scatter(cls(v.GRASS,v.MEADOW)&free&~occ,TREES_LONE,.004,margin=2)
    if Z is not None:objs+=objs_z
    if LAY:Path(LAY).mkdir(parents=True,exist_ok=True);(Path(LAY)/'links.json').write_text(json.dumps([dict(id=l['id'],other=l['other'],edge=l['edge'],cells=[[int(y),int(x)] for y,x in l['cells']]) for l in LINKS],ensure_ascii=False))
    if LAY:
        w=dict(w,mapid=mapid_n)
        export_layers(LAY,img,gimg,dimg,deck,objs,water,road,road0,Z,FMASK,own_n,w,H,W,PARTS,BRIDGES)
    for y,x0,y0,name in sorted(objs):
        if name.startswith('c7:'):img.alpha_composite(c7b(name[3:]),(x0*T,y0*T));continue
        if name=='oldsheaf':
            img.alpha_composite(tile('element:12:4'),(x0*T,(y-1)*T));img.alpha_composite(tile('element:12:5'),(x0*T,y*T));continue
        im=sprite(name);img.alpha_composite(im,(x0*T,(y-(im.height//T)+1)*T))
    rep['objects']=counts;rep['reviewBakedPieces']=BAKED_FOR_REVIEW
    OUT.mkdir(parents=True,exist_ok=True)
    sfx=('-no-'+'-'.join(sorted(asked))) if asked else ''
    for k,(b,lat,bg,ks) in (BLOCKS.items() if Z is None else []):
        b.save(OUT/f'block-{k}.png')
        piece_sheet(k,lat,bg).save(OUT/f'pieces-{k}.png')
    # 타일로 나누면 몇 종류가 되는지(숲이 걸친 칸을 16px로 잘라 같은 그림끼리 묶음)
    for k,fm in FMASK.items():
        area=ndimage.binary_dilation(fm,iterations=3)&own;uniq=set()
        for y,x in np.argwhere(area):uniq.add(img.crop((x*T,y*T,(x+1)*T,(y+1)*T)).tobytes())
        rep.setdefault('forestUniqueTiles',{})[k]=dict(cells=int(area.sum()),unique=len(uniq))
    img.convert('RGB').save(OUT/f'world-full{sfx}.png')
    img.resize((W*T//4,H*T//4),Image.BOX).convert('RGB').save(OUT/f'world{sfx}.png')
    # 게임 크기 화면(480×270 = 30×17칸) — 세계 좌표(칸) 중심
    cams=dict(lake=(88,46),source=(114,-6),songak=(186,-26),dakmok=(236,-62),naru=(258,40),jangdan=(214,28),wetroad=(140,-70),forest=(100,-40))
    for k,(cx,cy) in (cams.items() if Z is None else []):
        iy,ix=v.idx(cx,cy);x0=(ix-15)*T;y0=(iy-8)*T
        img.crop((x0,y0,x0+480,y0+272)).resize((960,544),Image.NEAREST).convert('RGB').save(OUT/f'cam-{k}{sfx}.png')
    if Z is not None:
        rep['village']=zones_check(Z,road,water,land,own,H,W)
        zones_images(img,Z,H,W)
    if not asked and Z is None:np.savez_compressed(OUT/'masks.npz',water=water,road=road,land=land,own=own,village=w['village'],vlabel=w['vlabel'],mapid=w['mapid'],
        trees=occ,terrain=t,elev=w['elev'],bridges=np.array([[cy,cx,deg,L] for cy,cx,deg,L in spans]),keys=np.array(w['keys']),towns=np.array([tw['key'] for tw in w['towns']]))
    (OUT/f'report{sfx}.json').write_text(json.dumps(rep,ensure_ascii=False,indent=1))
    print(json.dumps({k:v_ for k,v_ in rep.items() if k!='pieces'},ensure_ascii=False))

# ---------- M3: M2 구역도 → 건물·광장 ----------
ZDIR=ROOT/'ArtReview/M2-Zones'
def zones_masks(own,water,road,H,W,links=()):
    zs=json.loads((ZDIR/'zones.json').read_text())['villages']
    lot=np.zeros((H,W),bool);flat=np.zeros((H,W),bool);lane=np.zeros((H,W),bool);spot=np.zeros((H,W),bool)
    coll=np.zeros((H,W),bool);resv=np.zeros((H,W),bool);grass_keep=np.zeros((H,W),bool);objs=[];items=[]
    for key,vz in zs.items():
        ln=np.load(ZDIR/f"{vz['slug']}-lanes.npy");lane|=ln
        for r in vz['items']:
            if r.get('missing'):continue
            iy,ix=v.idx(r['x'],r['y']);sl=(slice(iy,iy+r['h']),slice(ix,ix+r['w']))
            r=dict(r,village=key,iy=iy,ix=ix);items.append(r)
            k=r['kind']
            if k=='lot':
                lot[sl]=True
                if r.get('asset') in('palace','gate'):
                    # 계단·대문이 그림 가운데(두 칸 사이)에 있어, 문 앞 흙길을 가운데 두 칸에 맞춘다(은샘 2026-10-28, 개경 궁궐)
                    cx=ix+r['w']//2;flat[iy+r['h']:iy+r['h']+2,cx-2:cx+2]=True
                    # 건물 바로 아래 줄은 입구 양옆을 같은 너비의 풀밭으로 둔다(은샘 2026-10-28)
                    # 양옆 풀밭은 바깥 가장자리가 아래로 한 칸씩 비스듬히 줄어든다(직각으로 깎지 않음)
                    dy0=iy+r['h']
                    grass_keep[dy0,ix:cx-2]=True;grass_keep[dy0,cx+2:ix+r['w']]=True
                    grass_keep[dy0+1,ix+1:cx-2]=True;grass_keep[dy0+1,cx+2:ix+r['w']-1]=True
            elif k in('plaza','yard','harbor') or (k=='shore' and not r.get('asset') and r['name']!='낚시터'):flat[sl]=True
            elif k=='reserve':spot[sl]=True;resv[sl]=True   # 나중에 쓸 빈 땅: 풀밭 그대로, 나무만 두지 않음   # 선착장만 흙. 빨래터·낚시터는 풀밭 물가   # 낚시터는 풀밭 물가 그대로
            elif k in('spot','lantern','npc','start','growth'):spot[sl]=True
            a=r.get('asset')
            if a:
                objs.append((iy+r['h']-1,ix,iy,'c7:'+a))
                # 충돌: 건물은 지붕 맨 윗줄을 뺀 아래(뒤로 지나가면 지붕에 가림), 1~2칸 소품은 밑동 1줄. 배는 물 위라 충돌 없음
                if k=='lot':coll[iy+1:iy+r['h'],ix:ix+r['w']]=True
                elif k!='moor':coll[iy+r['h']-1,ix:ix+r['w']]=True
    # 골목은 2칸 폭(조립 흙길 2×2 규칙). 건물 자리는 비운다
    lane2=lane.copy();lane2[:,1:]|=lane[:,:-1];lane2[1:,:]|=lane[:-1,:];lane2[1:,1:]|=lane[:-1,:-1]   # 2×2 붓
    lane2&=~lot&~coll
    # 다른 문명 연결 자리(m4_links): 출구 줄 앞 2칸은 흙길 마당, 거기서 마을 흙길까지 2칸 폭 흙길
    for L_ in links:
        dy_,dx_={'N':(1,0),'W':(0,1)}[L_['edge']]
        for y_,x_ in L_['cells']:
            for k_ in range(2):flat[y_+dy_*k_,x_+dx_*k_]=True
        net=road|lane2
        for r in items:
            if r['kind'] in('plaza','yard'):net[r['iy']:r['iy']+r['h'],r['ix']:r['ix']+r['w']]=True
        walk_=own&~water&~lot&~coll
        cy,cx=L_['cells'][len(L_['cells'])//2];cy+=dy_*1;cx+=dx_*1
        gy,gx=np.argwhere(net&walk_)[int(np.argmin(((np.argwhere(net&walk_)-[cy,cx])**2).sum(1)))]
        cost=np.where(walk_,1.0,50.0)
        path=v.astar((int(cy),int(cx)),(int(gy),int(gx)),cost,blocked=~walk_)
        for y_,x_ in path:lane2[y_:y_+2,x_:x_+2]|=walk_[y_:y_+2,x_:x_+2]
    road=road&~grass_keep
    ok=own&~water&~lot&~resv&~grass_keep   # 우물·석등 같은 작은 소품은 흙길 위에 서도 된다(밑에 풀 1칸이 남지 않게)
    road2=road|((flat|lane2)&ok)
    doors=np.zeros((H,W),bool)
    for r in items:
        if r['kind']=='lot':
            dy,dx=r['iy']+r['h'],r['ix']+r['w']//2
            if 0<=dy<H:doors[dy,dx]=True
    zone=ndimage.binary_dilation(lot|flat|lane2|spot,iterations=8)&own   # 정리는 마을 둘레에서만(마을 밖 큰길은 M1 그대로)
    doors_n=ndimage.binary_dilation(doors,iterations=1)
    def seamcells(m):
        p_=np.pad(m,1,mode='edge');n,s_,w_,e=p_[:-2,1:-1],p_[2:,1:-1],p_[1:-1,:-2],p_[1:-1,2:];ne,nw,se,sw=p_[:-2,2:],p_[:-2,:-2],p_[2:,2:],p_[2:,:-2]
        miss=sum(x_.astype(int) for x_ in [n&e&~ne,n&w_&~nw,s_&e&~se,s_&w_&~sw]);four=n&s_&w_&e
        return m&own&((miss>1)|((miss==1)&~four))
    ban=np.zeros_like(ok)
    for _round in range(4):
        road2=tidy_dirt(road2,road,ok&~ban,flat,lot|resv,doors,water,own,zone)
        for _ in range(8):
            okb=ok&~ban;road2&=okb|road
            road2=fill_diagonals(tileready(road2,okb),okb)
            sc=seamcells(road2)
            if not sc.any():break
            ban|=sc&~road;road2&=~(sc&~road)   # 건물 모서리·물가에 막혀 조각이 안 맞는 새 흙길 칸은 금지하고 다시 맞춘다
        # 광장·마당·문 골목이 모두 큰길과 이어졌으면 끝. 아니면 금지 칸을 피해 다시 잇는다
        lab,n=ndimage.label(road2,structure=np.ones((3,3)))
        main=set(np.unique(lab[road&road2]))-{0}
        lost=[j for j in range(1,n+1) if j not in main and (flat[lab==j].any() or doors_n[lab==j].any())]
        if not lost:break
    road2=drop_fragments(road2,road,flat,doors)
    if os.environ.get('DIRTDBG'):np.savez_compressed('/tmp/dirt.npz',own=own,water=water,road=road,road2=road2,lot=lot,coll=coll,spot=spot,flat=flat,lane=lane2,ok=ok)
    reserve=ndimage.binary_dilation(lot|flat|spot|coll,iterations=1)|lane2
    # 나무 그림은 밑동에서 위로 3칸까지 올라가므로, 예약 칸 아래 3칸·옆 2칸에는 나무 밑동을 두지 않는다
    canopy=reserve.copy()
    for dy in(1,2,3):canopy[dy:,:]|=reserve[:-dy,:]
    canopy=ndimage.binary_dilation(canopy,structure=np.ones((1,5)))
    return dict(road=road2,lot=lot,flat=flat,lane=lane2,spot=spot,coll=coll,reserve=reserve,canopy=canopy,objs=objs,items=items,zones=zs)

def _bfs_path(start_mask,goal_mask,walk):
    """start_mask에서 goal_mask까지 walk 위 가장 짧은 4방향 길(칸 목록). 없으면 None"""
    from collections import deque
    H,W=walk.shape;prev={};q=deque()
    for y,x in np.argwhere(start_mask):prev[(y,x)]=None;q.append((y,x))
    while q:
        c=q.popleft()
        if goal_mask[c] and not start_mask[c]:
            out=[]
            while c is not None:out.append(c);c=prev[c]
            return out
        for oy,ox in((1,0),(-1,0),(0,1),(0,-1)):
            n=(c[0]+oy,c[1]+ox)
            if 0<=n[0]<H and 0<=n[1]<W and n not in prev and walk[n]:prev[n]=c;q.append(n)
    return None

def drop_fragments(dirt,road,flat,doors):
    """큰길(M1)과 이어지지 않은 작은 흙길 조각을 지운다(광장·마당·문 골목은 이미 이어 붙였으므로 남는 것은 찌꺼기)."""
    lab,n=ndimage.label(dirt,structure=np.ones((3,3)))
    keep=set(np.unique(lab[road&dirt]))-{0}
    for j in range(1,n+1):
        if j in keep:continue
        m=lab==j
        if m.sum()<30 and not flat[m].any():dirt=dirt&~m
    # 모서리로만 닿은 작은 조각(상하좌우로 이어지지 않음)도 끊긴 것으로 보고 지운다(은샘 2026-10-28, 개경 항구)
    lab,n=ndimage.label(dirt);sz=ndimage.sum(np.ones_like(lab),lab,range(1,n+1))
    for j,z in enumerate(sz,1):
        m=lab==j
        if z<12 and not flat[m].any() and not doors[m].any():dirt=dirt&~m
    return dirt

def tidy_dirt(dirt,road,ok,flat,objs,doors,water,own,zone):
    """마을 흙길 다듬기(은샘 2026-10-28): 끊어진 곳은 잇고, 쓸모없는 조각·막다른 돌기는 풀밭으로,
    흙길에 갇힌 풀밭 섬·좁은 틈은 흙길로 메운다."""
    from skimage.morphology import skeletonize
    H,W=dirt.shape;near_obj=ndimage.binary_dilation(objs,iterations=1)
    ok2=blocks2(ok)   # 2×2 흙길 덩어리가 들어가는 칸
    def brush(cells):
        m=np.zeros((H,W),bool)
        for y,x in cells:
            for dy in(0,-1):
                for dx in(0,-1):
                    if y+dy>=0 and x+dx>=0 and ok[y+dy:y+dy+2,x+dx:x+dx+2].all() and ok[y+dy:y+dy+2,x+dx:x+dx+2].size==4:
                        m[y+dy:y+dy+2,x+dx:x+dx+2]=True;break
                else:continue
                break
        return m
    for _ in range(2):
        # 1) 큰길과 끊어진 흙길 덩어리(광장·마당·골목)는 가장 가까운 큰길까지 2칸 폭으로 잇는다
        lab,n=ndimage.label(dirt,structure=np.ones((3,3)))
        main=np.isin(lab,list(set(np.unique(lab[road]))-{0}))
        for j in range(1,n+1):
            m=lab==j
            if main[m].any():continue
            if not (flat[m].any() or ndimage.binary_dilation(doors,iterations=1)[m].any()):continue
            path=_bfs_path(m,main,ok2|main)
            if path and len(path)<=45:dirt|=brush(path);main|=brush(path)|m
        # 2) 흙길에 갇힌 풀밭(물건 없음, 60칸 이하)과 거의 갇힌 풀밭(둘레 85% 이상 흙길, 40칸 이하)은 흙길로
        g=own&~water&~dirt&zone;gl,gn=ndimage.label(g)
        for j in range(1,gn+1):
            m=gl==j;z=int(m.sum())
            if z>60 or near_obj[m].any():continue
            ring=ndimage.binary_dilation(m)&~m;frac=dirt[ring].mean()
            if (frac>=.999 and z<=60) or (frac>=.85 and z<=40):dirt|=m&ok
        # 3) 흙길 사이 1~2칸 좁은 풀밭 틈·홈은 흙길로(넓은 풀밭은 그대로)(물건 둘레는 그대로)
        cl=ndimage.binary_closing(dirt,structure=np.ones((3,3)),iterations=1)
        dirt|=cl&ok&~near_obj&zone&~dirt
        # 4) 막다른 돌기: 뼈대 끝에서 갈림길까지 8칸 이하이고, 끝이 문·광장·물가·담당 경계 가까이가 아니면 풀밭으로
        sk=skeletonize(dirt)
        nb=sum(np.roll(np.roll(sk,dy,0),dx,1) for dy in(-1,0,1) for dx in(-1,0,1) if dy or dx)
        purpose=ndimage.binary_dilation(doors|flat|water|~own,iterations=2)
        junction=sk&(nb>=3);rm=np.zeros((H,W),bool)
        for ey,ex in np.argwhere(sk&(nb==1)&zone&~purpose):
            br=[(ey,ex)];seen={(ey,ex)};c=(ey,ex)
            while len(br)<=9:
                nx=[(c[0]+dy,c[1]+dx) for dy in(-1,0,1) for dx in(-1,0,1) if (dy or dx) and 0<=c[0]+dy<H and 0<=c[1]+dx<W and sk[c[0]+dy,c[1]+dx] and (c[0]+dy,c[1]+dx) not in seen]
                if not nx or junction[c]:break
                c=nx[0];seen.add(c);br.append(c)
            if len(br)<=8 and junction[c]:
                for y,x in br[:-2]:rm[y,x]=True
        if rm.any():
            # 돌기 뼈대에 가장 가까운 흙길 칸만 지운다(큰길 M1 칸은 남김)
            keep=sk&~rm;_,(iy,ix)=ndimage.distance_transform_edt(~(keep|rm),return_indices=True)
            own_rm=rm[iy,ix]&dirt&~road&~flat
            dirt&=~own_rm
    return dirt

def zones_check(Z,road,water,land,own,H,W):
    """정적 검사: 문·기능 자리가 막히지 않고 길에서 걸어서 닿는지(충돌 칸·물 제외)."""
    walk=land&~Z['coll']
    lab,_=ndimage.label(walk)
    roadlab=set(np.unique(lab[road&walk]))-{0}
    res={}
    for r in Z['items']:
        k=r['kind'];pts=[]
        if k=='lot':
            dy,dx=v.idx(*r['door']) if 'door' in r else (r['iy']+r['h'],r['ix']+r['w']//2);pts=[(dy,dx)]
        elif k in('npc','start','growth','plaza','yard','shore','harbor','reserve'):pts=[(r['iy'],r['ix'])]
        elif k=='spot' and not r.get('asset'):pts=[(r['iy'],r['ix'])]
        elif k in('spot','lantern') and r.get('asset'):pts=[(r['iy']+r['h'],r['ix'])]   # 앞 칸
        bad=[p for p in pts if not(0<=p[0]<H and 0<=p[1]<W) or not walk[p] or lab[p] not in roadlab]
        if bad:res.setdefault(r['village'],[]).append(r['name'])
    out=dict(blocked=res,collisionCells=int(Z['coll'].sum()),lotCells=int(Z['lot'].sum()),plazaYardCells=int(Z['flat'].sum()),laneCells=int((Z['lane']&~Z['flat']).sum()))
    coll=[]
    for r in Z['items']:
        if r.get('asset') and r['kind']!='moor':
            hh=r['h']-1 if r['kind']=='lot' else 1
            coll.append(dict(village=r['village'],name=r['name'],asset=r['asset'],x=r['x'],y=r['y']+r['h']-hh,w=r['w'],h=hh))
    (OUT).mkdir(parents=True,exist_ok=True)
    (OUT/'collision.json').write_text(json.dumps(coll,ensure_ascii=False,indent=1))
    return out

ZCOL={'harbor':(40,200,220),'reserve':(200,200,255),'plaza':(255,214,90),'yard':(255,170,60),'lot':(240,110,90),'shore':(90,190,255),'spot':(255,255,255),'moor':(60,120,255),
      'npc':(255,120,220),'lantern':(255,250,170),'growth':(150,240,140),'start':(120,255,255)}
def zones_images(img,Z,H,W):
    font=str(ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf');fs=ImageFont.truetype(font,12);fb=ImageFont.truetype(font,18)
    for key,vz in Z['zones'].items():
        its=[r for r in Z['items'] if r['village']==key]
        ys=[r['iy'] for r in its]+[r['iy']+r['h'] for r in its];xs=[r['ix'] for r in its]+[r['ix']+r['w'] for r in its]
        y0,y1,x0,x1=min(ys)-5,max(ys)+5,min(xs)-6,max(xs)+6
        c=img.crop((x0*T,y0*T,x1*T,y1*T)).convert('RGBA');c.convert('RGB').save(OUT/f"{vz['slug']}.png")
        # 표시판: 기능 자리 점(NPC·게시판·시작·성장)과 이름
        ov=c.copy();dr=ImageDraw.Draw(ov)
        for r in its:
            bx=[(r['ix']-x0)*T,(r['iy']-y0)*T,(r['ix']-x0+r['w'])*T-1,(r['iy']-y0+r['h'])*T-1]
            if r['kind'] in('npc','spot','start','growth','reserve') and not r.get('asset'):
                dr.rectangle(bx,outline=ZCOL[r['kind']],width=2)
            if r['kind'] in('npc','spot','start','growth','lantern'):lab=r['name'].replace('성장 자리 ','성')
            else:lab=r['name']
            dr.text((bx[0]+1,bx[1]-13 if r['kind'] in('npc','spot','start','growth','lantern') else bx[1]+2),lab,fill='white',font=fs,stroke_width=2,stroke_fill='black')
        ov.convert('RGB').save(OUT/f"{vz['slug']}-labels.png")
        # 게임 크기 화면(480×272) — 광장 중심, 2배
        pz=next((r for r in its if r['kind']=='plaza'),its[0]);cy,cx=pz['iy']+pz['h']//2,pz['ix']+pz['w']//2
        img.crop(((cx-15)*T,(cy-8)*T,(cx+15)*T,(cy+9)*T)).resize((960,544),Image.NEAREST).convert('RGB').save(OUT/f"cam-{vz['slug']}.png")

def _sprite_img(name):
    if name.startswith('c7:'):return c7b(name[3:])
    return sprite(name)

def export_layers(out,img,gimg,dimg,deck,objs,water,road,road0,Z,FMASK,own,w,H,W,PARTS,BRIDGES):
    """M4: 게임 맵 층으로 나눠 저장한다(검토용).
    - 막힘: 물(다리 판자 아래 제외), 숲·산·수원지 숲 칸, 건물·소품 충돌, 나무·덤불·바위 밑동
    - 개체 그림은 칸마다 나눈다: 막힌 칸이거나 밑동 줄이 그 칸보다 위·같은 줄이면 '아래 개체'(플레이어 아래),
      걸을 수 있는 칸인데 밑동이 더 아래인 개체 그림은 '위 개체'(플레이어를 가림). 각 층 안에서는 밑동 줄 순서.
    """
    out=Path(out);out.mkdir(parents=True,exist_ok=True)
    blocked=water&~deck
    for fm in FMASK.values():blocked|=fm
    if Z is not None:blocked|=Z['coll']
    # 나무·덤불·바위 밑동(마을 건물은 Z['coll']이 이미 막음)
    LOW=('tall_grass','lotus')
    for y,x0,y0,name in objs:
        nm=name.split(':')[0] if not name.startswith('c7:') else None
        if nm is None or nm in LOW:continue
        sw=int(name.split(':')[1])
        if nm in('shrub','small_rock','moss_rock','fallen_log'):blocked[y,x0:x0+sw]=True
        else:blocked[y,x0+(sw-1)//2:x0+sw//2+1]=True   # 나무 줄기 가운데 1~2칸
    blocked&=own
    oimg=Image.new('RGBA',img.size,(0,0,0,0));himg=Image.new('RGBA',img.size,(0,0,0,0))
    for y,x0,y0,name in sorted(objs):
        im=_sprite_img(name);sh=im.height//T;sw=im.width//T;top=y-sh+1
        for r_ in range(sh):
            cy=top+r_
            for c_ in range(sw):
                cx=x0+c_
                if not(0<=cy<H and 0<=cx<W):continue
                piece=im.crop((c_*T,r_*T,(c_+1)*T,(r_+1)*T))
                if piece.getbbox() is None:continue
                lay_='obj' if (blocked[cy,cx] or y<=cy) else 'high'
                (oimg if lay_=='obj' else himg).alpha_composite(piece,(cx*T,cy*T))
                PARTS.setdefault((lay_,cy,cx),[]).append(('sprite:'+name,-c_*T,-r_*T))
    for nm_,im_ in(('ground',gimg),('deco',dimg),('obj',oimg),('high',himg),('composite',img)):im_.save(out/f'{nm_}.png')
    grade=np.zeros((H,W),np.uint8)
    if Z is not None:grade[Z['road']]=1
    grade[road0&road]=2;grade[deck]=2
    np.savez_compressed(out/'masks.npz',blocked=blocked,water=water&~deck,grade=grade,own=own,mapid=w['mapid'],keys=np.array(w['keys']),deck=deck)
    # 인증 기록: 칸별 원본 목록 + 원본 그림 경로(새로 만든 원본은 여기서 굽는다)
    srcs={}
    def src_path(sid):
        kind,_,nm=sid.partition(':')
        if kind=='tile':
            fk=nm+'@'+SKIN
            if fk in INDEX['tiles']:return INDEX['tiles'][fk]['path']
            p_=f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{nm.replace(":","_")}_{SKIN}.png'
            if (ROOT/p_).exists():
                if Image.open(ROOT/p_).convert('RGBA').tobytes()!=tile(nm).tobytes():raise ValueError('기존 타일과 그림이 다름: '+p_)
                return p_   # 이미 게임에 있는 jade 흙길 조각(같은 그림)
            tile(nm).save(ROOT/p_);return p_
        if kind=='sprite':
            if nm.startswith('c7:'):
                a_=nm[3:];return str((ROOT/'ArtReview/Approved'/C7B[a_]/f'{a_}.png').relative_to(ROOT)) if a_ in C7B else f'ArtReview/C7-RemainingBatch-20261027/Native/{a_}.png'
            n0=nm.split(':')[0]
            return C7[n0] if n0 in C7 else INDEX['sprites'][nm]
        d_=ROOT/'Assets/PocketCaravan/Art/Baked/Objects/M4';d_.mkdir(parents=True,exist_ok=True)
        if kind=='bridge':
            p_=d_/f'bridge_{nm}.png';im_=BRIDGES[int(nm)]
            if not p_.exists() or Image.open(p_).convert('RGBA').tobytes()!=im_.convert('RGBA').tobytes():im_.save(p_)
            return str(p_.relative_to(ROOT))
        if kind=='fill':
            p_=d_/f'shade_{nm}.png';im_=Image.new('RGBA',(T,T),tuple(int(nm[i:i+2],16) for i in(0,2,4,6)))
            if not p_.exists() or Image.open(p_).convert('RGBA').tobytes()!=im_.tobytes():im_.save(p_)
            return str(p_.relative_to(ROOT))
        raise ValueError(sid)
    for v_ in PARTS.values():
        for sid,_,_ in v_:
            if sid not in srcs:srcs[sid]=src_path(sid)
    (out/'parts.json').write_text(json.dumps(dict(sources=srcs,parts={f'{l}:{y}:{x}':v_ for (l,y,x),v_ in PARTS.items()}),ensure_ascii=False))
    if os.environ.get('M4_NATURE')=='review':
        import m4_nature
        print('승인 자연 배치 검토:',m4_nature.apply_layers(out,FMASK))
    else:
        import m4_nature
        if m4_nature.LIVE.exists():
            print('승인 자연 배치 본편:',m4_nature.apply_layers(out,FMASK,production=True,path=m4_nature.LIVE))
    print('M4 층 저장:',out)

def gameart(name):
    d=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    p=d/(name+'_'+SKIN+'.png')
    return Image.open(p if p.exists() else d/(name+'.png')).convert('RGBA')

_pl=None
def _plank():
    global _pl
    if _pl is None:_pl=Image.open(ROOT/INDEX['tiles']['water:1:13@wood@'+SKIN]['path']).convert('RGBA')
    return _pl

if __name__=='__main__':main()
