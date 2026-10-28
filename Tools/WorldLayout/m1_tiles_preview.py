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

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--no',default='');a=ap.parse_args()
    off=set(filter(None,a.no.split(',')))
    v.VARIANT['area']=v.SMALL_CENTRAL;v.VARIANT['hamlets']=v.HAMLETS
    layout=WorldLayout.read();w=v.build(layout);r=v.roads(layout,w);v.partition(layout,w,r)
    t=w['t'];own=w['owned'];H,W=t.shape;rng=np.random.default_rng(11)
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
    # 다리: 길이 물을 건너는 덩어리마다 네모 반듯하게(물 칸만 판자)
    lab,n_=ndimage.label(ndimage.binary_dilation(bridge,iterations=1)&water)
    bridge=np.zeros_like(bridge)
    for sl in ndimage.find_objects(lab):
        bridge[sl]|=water[sl]
    road|=bridge
    # 외톨이 길 조각(4칸 미만 덩어리) 제거
    lab,n_=ndimage.label(road&~bridge)
    for i_,sl in enumerate(ndimage.find_objects(lab),1):
        if (lab[sl]==i_).sum()<4:road[sl][lab[sl]==i_]=False
    land=own&~water
    rep=dict(waterAdded=int((water&~water0).sum()),roadAdded=int((road&~road0).sum()),bridgeCells=int(bridge.sum()))
    def inside(y,x):return 0<=y<H and 0<=x<W
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
                if bridge[y,x]:img.alpha_composite(_plank(),(x*T,y*T))
            elif road[y,x]:
                rel=autorel(mk(road,y,x,True),y,x)
                if rel is None:bad['road']+=1;img.alpha_composite(tile(g),(x*T,y*T));continue
                key=f'floor:{rel[0]}:{rel[1]+7}'
                img.alpha_composite(tile(key),(x*T,y*T));pieces[key]+=1
            else:
                img.alpha_composite(tile(g),(x*T,y*T))
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
    if 'mountain' not in off:counts['mountain']=scatter(cls(v.MOUNTAIN,v.PEAK)&free,TREES_MOUNT,.16)
    if 'forest' not in off:counts['forest']=scatter(cls(v.FOREST)&free,TREES_FOREST,.07)
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
        # 논: 3×3 통짜 조각을 들판에 바둑판으로(사이 1칸 둑)
        fm=np.isin(t,[v.FIELD])&land&~near_road&~village&~occ;n=0
        for y in range(0,H-3,4):
            for x in range(0,W-3,4):
                sq=(slice(y,y+3),slice(x,x+3))
                if fm[sq].all() and not occ[sq].any():
                    for dy in range(3):
                        for dx in range(3):img.alpha_composite(tile(f'element:{13+dx}:{4+dy}'),((x+dx)*T,(y+dy)*T))
                    occ[sq]=True;n+=1
        counts['paddy']=n
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
        im=sprite(name);img.alpha_composite(im,(x0*T,(y-(im.height//T)+1)*T))
    rep['objects']=counts;rep['reviewBakedPieces']=BAKED_FOR_REVIEW
    OUT.mkdir(parents=True,exist_ok=True)
    sfx=('-no-'+'-'.join(sorted(off))) if off else ''
    img.convert('RGB').save(OUT/f'world-full{sfx}.png')
    img.resize((W*T//4,H*T//4),Image.BOX).convert('RGB').save(OUT/f'world{sfx}.png')
    # 게임 크기 화면(480×270 = 30×17칸) — 세계 좌표(칸) 중심
    cams=dict(lake=(88,46),source=(114,-6),songak=(186,-26),dakmok=(236,-62),naru=(258,40),jangdan=(214,28),wetroad=(140,-70),forest=(100,-40))
    for k,(cx,cy) in cams.items():
        iy,ix=v.idx(cx,cy);x0=(ix-15)*T;y0=(iy-8)*T
        img.crop((x0,y0,x0+480,y0+272)).resize((960,544),Image.NEAREST).convert('RGB').save(OUT/f'cam-{k}{sfx}.png')
    (OUT/f'report{sfx}.json').write_text(json.dumps(rep,ensure_ascii=False,indent=1))
    print(json.dumps({k:v_ for k,v_ in rep.items() if k!='pieces'},ensure_ascii=False))

_pl=None
def _plank():
    global _pl
    if _pl is None:_pl=Image.open(ROOT/INDEX['tiles']['water:1:13@wood@'+SKIN]['path']).convert('RGBA')
    return _pl

if __name__=='__main__':main()
