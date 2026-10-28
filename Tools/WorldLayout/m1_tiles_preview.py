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

P=6   # 숲 합성 블록 크기(칸). 이 블록이 반복되는 '숲 타일'
FOREST_BLOCK=['forest_round:3:4','forest_bent:3:4','broadleaf:4:4','round_west:4:3','round_east:4:3','forest_wide:4:3','light_tree:3:4']
MOUNT_BLOCK=['pine_west:4:3','pine_east:4:3','pine_west:4:3','pine_east:4:3','forest_bent:3:4']
BLOCKS={};FMASK={}

def clean(m,minsize):
    m=ndimage.binary_closing(ndimage.binary_opening(m),iterations=1)&m|ndimage.binary_opening(m)
    lab,n=ndimage.label(m)
    if n:
        sizes=ndimage.sum(np.ones_like(lab),lab,range(1,n+1))
        for i,sz in enumerate(sizes,1):
            if sz<minsize:m[lab==i]=False
    return m

def forest_block(kinds,seed):
    """승인된 나무 그림을 빽빽하게 겹쳐 P×P칸 반복 블록을 만든다(가장자리가 이어지도록 감아 그림).
    lat: 블록 안 나무 자리(밑동 칸) → 나무 이름. 같은 자리를 숲 가장자리에서 통째로 다시 그려 경계를 자연스럽게."""
    r=np.random.default_rng(seed);lat={}
    # 2칸마다 한 그루 + 엇갈린 줄에 한 그루 더(틈이 보이지 않을 만큼 빽빽하게)
    for gy in range(0,P,2):
        for gx in range(0,P,2):
            for oy,ox in((0,0),(1,1)):
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
    ap=argparse.ArgumentParser();ap.add_argument('--no',default='');a=ap.parse_args()
    asked=set(filter(None,a.no.split(',')));off=asked|{'field'}   # 들판(논)은 2026-10-28 은샘 지시로 뺌
    v.VARIANT['area']=v.SMALL_CENTRAL;v.VARIANT['hamlets']=v.HAMLETS
    layout=WorldLayout.read();w=v.build(layout);r=v.roads(layout,w);v.partition(layout,w,r)
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
    # 마지막 확인: 다리 끝마다 5칸 안의 가장 가까운 길까지 3칸 폭으로 곧게 잇는다(반올림 때문에 생기는 틈 메움)
    for cy,cx,deg,L in spans:
        uy,ux=math.sin(math.radians(deg)),math.cos(math.radians(deg));half=(int(math.ceil(L))+1)/2
        for sg in(1,-1):
            ey,ex=cy+sg*uy*(half-.5),cx+sg*ux*(half-.5)
            cand=np.argwhere(road)
            d2=((cand-[ey,ex])**2).sum(1);j=int(np.argmin(d2))
            if d2[j]>5**2:continue
            ty,tx=cand[j];n=int(max(abs(ty-ey),abs(tx-ex))*2)+1
            for i in range(n+1):
                yy,xx=int(round(ey+(ty-ey)*i/n)),int(round(ex+(tx-ex)*i/n))
                for dy in(-1,0,1):
                    for dx in(-1,0,1):
                        if inside(yy+dy,xx+dx) and own[yy+dy,xx+dx] and not water[yy+dy,xx+dx]:road[yy+dy,xx+dx]=True
        # 다리 밑 기슭 칸도 길로(다리 끝 아래로 길이 들어가 틈이 안 보이게)
        for k in np.arange(-half,half+.01,.5):
            for wv in(-1,0,1):
                yy,xx=int(round(cy+uy*k-ux*wv)),int(round(cx+ux*k+uy*wv))
                if inside(yy,xx) and own[yy,xx] and not water[yy,xx]:road[yy,xx]=True
    road=fill_diagonals(tileready(road,own&~water),own&~water)
    land=own&~water
    rep=dict(waterAdded=int((water&~water0).sum()),roadAdded=int((road&~road0).sum()),bridgeCells=int(bridge.sum()))
    img=Image.new('RGBA',(W*T,H*T),(28,34,40,255))
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
                img.alpha_composite(tile(key),(x*T,y*T));pieces[key]+=1
            elif road[y,x]:
                rel=autorel(mk(road,y,x,True),y,x)
                if rel is None:bad['road']+=1;img.alpha_composite(tile(g),(x*T,y*T));continue
                key=f'floor:{rel[0]}:{rel[1]+7}'
                img.alpha_composite(tile(key),(x*T,y*T));pieces[key]+=1
            else:
                img.alpha_composite(tile(g),(x*T,y*T))
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
        img.alpha_composite(rot,(int(round((cx+.5)*T-rot.width/2)),int(round((cy+.5)*T-rot.height/2))))
        BRIDGES.append(rot)
    rep['bridges']=[dict(x=round(cx+v.X0,1),y=round(cy+v.Y0,1),angle=round((deg+360)%180,1),length=int(math.ceil(L))+2) for cy,cx,deg,L in spans]
    rep['pieces']=dict(pieces);rep['unmatched']=dict(bad)
    def seams(mask):
        p=np.pad(mask,1,mode='edge');n,s_,w_,e=p[:-2,1:-1],p[2:,1:-1],p[1:-1,:-2],p[1:-1,2:]
        ne,nw,se,sw=p[:-2,2:],p[:-2,:-2],p[2:,2:],p[2:,:-2]
        need=[n&e&~ne,n&w_&~nw,s_&e&~se,s_&w_&~sw];miss=sum(x_.astype(int) for x_ in need);four=n&s_&w_&e
        return int((mask&own&((miss>1)|((miss==1)&~four))).sum())
    rep['seamCells']=dict(water=seams(water),road=seams(road))
    # ---------- 개체 심기 ----------
    near_road=ndimage.binary_dilation(road,iterations=1);near_water=ndimage.binary_dilation(water,iterations=1)
    village=w['village']
    free=land&~near_road&~near_water
    occ=np.zeros((H,W),bool)   # 줄기·덮인 칸
    objs=[]
    def place(name,y,x,margin=0):
        nm,sw,sh=name.split(':');sw,sh=int(sw),int(sh)
        x0=x-sw//2;y0=y-sh+1
        ys,xs=slice(max(0,y0),y+1),slice(max(0,x0),x0+sw)
        if y0<0 or x0<0 or x0+sw>W:return False
        foot=(slice(y-min(sh-1,1),y+1),slice(x0,x0+sw))   # 아래 두 줄
        if not free[foot].all() or occ[ys,xs].any():return False
        occ[max(0,y0-margin):y+1+margin,max(0,x0-margin):x0+sw+margin]=True
        objs.append((y,x0,y0,name));return True
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
        fm=clean(mask_&~near_road&~near_water&~occ,12)
        blk,lat,bg=forest_block(ks,seed=3 if kind=='forest' else 5)
        # 바탕(숲 그늘): 아래 2칸까지 숲인 칸만. 숲 맨 아래 줄은 풀밭 위에 밑동이 보인다(시작 부분).
        below=np.zeros_like(fm);below[:-2]=fm[1:-1]&fm[2:]
        under=ndimage.binary_erosion(fm,iterations=1)&below
        for y,x in np.argwhere(under):img.paste(bg,(x*T,y*T,(x+1)*T,(y+1)*T))
        FMASK[kind]=fm
        # 나무는 모두 통째로, 밑동 줄 순서대로(뒤 나무 위에 앞 나무). 블록과 같은 자리라 안쪽은 숲 타일과 같은 그림이 된다.
        n=0
        for y,x in np.argwhere(fm):
            k=lat.get((y%P,x%P))
            if k is None:continue
            nm,sw,sh=k.split(':');sw,sh=int(sw),int(sh);x0=x-sw//2;y0=y-sh+1
            if x0<0 or y0<0 or x0+sw>W:continue
            objs.append((y,x0,y0,k));n+=1
        occ|=ndimage.binary_dilation(fm,iterations=1);counts[kind]=dict(cells=int(fm.sum()),trees=n)
        BLOCKS[kind]=(blk,lat,bg,ks)
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
            if rng.random()<.10:img.alpha_composite(tile(FLOWERS[rng.integers(2)]),(x*T,y*T));n+=1
        counts['flowers']=n
    if 'grass' not in off:
        gm=land&~road&~occ;n=0
        for y,x in np.argwhere(gm):
            if rng.random()<.035:img.alpha_composite(tile(TUFTS[rng.integers(4)]),(x*T,y*T));n+=1
        counts['tufts']=n
        counts['loneTrees']=scatter(cls(v.GRASS,v.MEADOW)&free&~occ,TREES_LONE,.004,margin=2)
    for y,x0,y0,name in sorted(objs):
        if name=='oldsheaf':
            img.alpha_composite(tile('element:12:4'),(x0*T,(y-1)*T));img.alpha_composite(tile('element:12:5'),(x0*T,y*T));continue
        im=sprite(name);img.alpha_composite(im,(x0*T,(y-(im.height//T)+1)*T))
    rep['objects']=counts;rep['reviewBakedPieces']=BAKED_FOR_REVIEW
    OUT.mkdir(parents=True,exist_ok=True)
    sfx=('-no-'+'-'.join(sorted(asked))) if asked else ''
    for k,(b,lat,bg,ks) in BLOCKS.items():
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
    for k,(cx,cy) in cams.items():
        iy,ix=v.idx(cx,cy);x0=(ix-15)*T;y0=(iy-8)*T
        img.crop((x0,y0,x0+480,y0+272)).resize((960,544),Image.NEAREST).convert('RGB').save(OUT/f'cam-{k}{sfx}.png')
    (OUT/f'report{sfx}.json').write_text(json.dumps(rep,ensure_ascii=False,indent=1))
    print(json.dumps({k:v_ for k,v_ in rep.items() if k!='pieces'},ensure_ascii=False))

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
