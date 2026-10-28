"""M4 ③ 맵 객체: 시작 위치, 맵 사이 출입구, 시장·게시판, 퀘스트 NPC, 일꾼·주민, 석등 불빛, 주변음, 마을 발전 소품, 도적.

- 출입구: 두 맵이 맞닿은 경계에서 양쪽 칸이 모두 걸을 수 있는 곳을 같은 줄로 묶어(가로·세로 직선) 출구로 만든다.
  출구는 경계 바로 안쪽 칸, 도착 자리는 상대 맵의 경계에서 한 칸 더 들어간 칸. 출구와 도착 자리는 같은 폭.
  (배치안 D: 이웃 맵과는 길 하나가 아니라 걸을 수 있는 경계 전체로 이어진다)
- 기능 객체: M2 구역도(ArtReview/M2-Zones/zones.json)의 자리, 옛 맵 객체(ArtReview/M0/old-objects.json)의 이름·그림·대사를 그대로 옮긴다.
  옛 맵에 있던 객체는 빠짐없이 새 맵에 둔다(위치만 새 배치). 새 마을의 시장·NPC ID는 정하지 않았으므로 넣지 않는다(E2·M5).
"""
import json
from pathlib import Path
import numpy as np
from scipy import ndimage

ROOT=Path(__file__).resolve().parents[2]
EMPTY=dict(w=1,h=1,id='',npc='',town='',target='',spawn='',edge='',text='',name='',character='',lines=[],variant=0,idx=0,
           sheet='',frames=0,fps=0.0,radius=0.0,type='',path=[])
def O(kind,x,y,**k):
    o=dict(EMPTY);o.update(k);o['kind']=kind;o['x']=int(x);o['y']=int(y);return o

DIRS={'N':(-1,0),'S':(1,0),'W':(0,-1),'E':(0,1)};OPP={'N':'S','S':'N','W':'E','E':'W'}

def seams(mapid,walk,mids,ox=0,oy=0):
    """경계 출입구. 반환: {mapId:[객체...]}
    출구는 서 있기만 해도(경계 쪽을 보면) 바로 쓰이므로, 도착 자리는 어느 출구 칸과도 겹치지 않게 경계에서 2~6칸 안쪽으로 잡는다.
    (계단 모양 경계나 세 맵이 만나는 모서리에서 도착하자마자 다른 출구로 넘어가는 것을 막음. M4 ⑦ Unity 왕복에서 발견)"""
    H,W=mapid.shape;out={m:[] for m in mids if m};count={}
    def ok(y,x):return 0<=y<H and 0<=x<W
    runs_all=[]
    for dname,(dy,dx) in DIRS.items():
        pairs={}
        for y in range(H):
            for x in range(W):
                a=mapid[y,x]
                if a<0 or not walk[y,x]:continue
                ny,nx=y+dy,x+dx
                if not ok(ny,nx):continue
                b=mapid[ny,nx]
                if b<0 or b==a or a>b:continue   # 경계 하나는 번호가 작은 쪽에서 한 번만
                if not walk[ny,nx]:continue
                pairs.setdefault((a,b),[]).append((y,x))
        for (a,b),cells in pairs.items():
            cells.sort(key=(lambda c:(c[0],c[1])) if dname in'NS' else (lambda c:(c[1],c[0])))
            runs=[];cur=[cells[0]]
            for c in cells[1:]:
                p=cur[-1]
                if (dname in'NS' and c[0]==p[0] and c[1]==p[1]+1) or (dname in'WE' and c[1]==p[1] and c[0]==p[0]+1):cur.append(c)
                else:runs.append(cur);cur=[c]
            runs.append(cur)
            for run in runs:runs_all.append((dname,a,b,run))
    # 모든 출구 칸(맵별): A 쪽은 경계 안쪽 칸, B 쪽은 그 건너편 칸
    exitcells=set()
    for dname,a,b,run in runs_all:
        dy,dx=DIRS[dname]
        for y,x in run:exitcells.add((y,x));exitcells.add((y+dy,x+dx))
    def arrival(cells,m,dy,dx,start):
        """경계 칸 목록에서 (dy,dx) 방향으로 start칸부터 들어가며, 줄 전체가 걸을 수 있고 같은 맵이며 출구가 아닌 첫 깊이"""
        for k in range(start,start+5):
            pts=[(y+dy*k,x+dx*k) for y,x in cells]
            if all(ok(*p_) and walk[p_] and mapid[p_]==m and p_ not in exitcells for p_ in pts):return pts
        return None
    for dname,a,b,run in runs_all:
        dy,dx=DIRS[dname];A,B=mids[a],mids[b]
        inB=arrival(run,b,dy,dx,2)            # B 안쪽(경계 건너편 칸에서 한 칸 더부터)
        inA=arrival(run,a,-dy,-dx,1)          # A 안쪽
        if inB is None or inA is None:continue
        ka=count.get((A,B),0);count[(A,B)]=ka+1;kb=count.get((B,A),0);count[(B,A)]=kb+1
        n=len(run);horiz=dname in'NS';w_,h_=(n,1) if horiz else (1,n)
        (y0,x0)=run[0];W0=lambda p_:(p_[1]+ox,p_[0]+oy)
        out[A].append(dict(kind='exit',world=(x0+ox,y0+oy),w=w_,h=h_,target=B,spawn=f'seam_{A}_{ka}',edge=dname))
        out[B].append(dict(kind='spawn',world=W0(inB[0]),w=w_,h=h_,id=f'seam_{A}_{ka}',edge=dname))
        out[B].append(dict(kind='exit',world=(x0+dx+ox,y0+dy+oy),w=w_,h=h_,target=A,spawn=f'seam_{B}_{kb}',edge=OPP[dname]))
        out[A].append(dict(kind='spawn',world=W0(inA[0]),w=w_,h=h_,id=f'seam_{B}_{kb}',edge=OPP[dname]))
    return out

def build(mid,key,box,walk_local,grade_local,water_local,inside,zones,old,seam_list,old_signs=()):
    """한 맵의 객체 목록. box=(x0,y0) 세계 좌표 왼쪽 위, *_local은 맵 직사각형 배열."""
    X0,Y0=box;h,w=walk_local.shape;objs=[];report=dict(placed=[],moved=[],missing=[])
    def L(X,Y):return X-X0,Y-Y0
    def walkable(x,y):return 0<=y<h and 0<=x<w and walk_local[y,x] and inside[y,x]
    def nearest(x,y,avoid=()):
        if walkable(x,y) and (x,y) not in avoid:return x,y
        for r in range(1,12):
            for yy in range(y-r,y+r+1):
                for xx in range(x-r,x+r+1):
                    if max(abs(yy-y),abs(xx-x))==r and walkable(xx,yy) and (xx,yy) not in avoid:return xx,yy
        return None
    taken=set()
    def put(kind,X,Y,label,**k):
        x,y=L(X,Y);p=nearest(x,y,taken)
        if p is None:report['missing'].append(label);return None
        if p!=(x,y):report['moved'].append(label)
        taken.add(p);o=O(kind,p[0],p[1],**k);objs.append(o);report['placed'].append(label);return o
    for s in seam_list:
        x,y=L(*s['world'])
        if s['kind']=='exit':objs.append(O('exit',x,y,w=s['w'],h=s['h'],target=s['target'],spawn=s['spawn'],edge=s['edge']))
        else:objs.append(O('spawn',x,y,w=s['w'],h=s['h'],id=s['id'],edge=s['edge']))
    items={}
    for vz in zones:
        for it in vz['items']:
            if not it.get('missing'):items[it['name']]=it
    oldk={}
    for o in old:oldk.setdefault(o['kind'],[]).append(o)
    def cen(it):return it['x']+it['w']//2,it['y']+it['h']//2
    def front(it):return it['x']+it['w']//2,it['y']+it['h']   # 소품·건물 앞 칸
    # 시작 위치
    if '시작 위치' in items:put('spawn',items['시작 위치']['x'],items['시작 위치']['y'],'S0',id='S0')
    # 시장·게시판(기존 마을 ID만)
    for o in oldk.get('market',[]):
        pz=items.get('장터 광장')
        if pz:put('market',pz['x']+pz['w']//2,pz['y']+1,'market:'+o['town'],town=o['town'])
    for o in oldk.get('board',[]):
        b=items.get('게시판')
        if b:put('board',b['x'],b['y']+1,'board:'+o['town'],town=o['town'])   # 게시판 앞 칸에서 상호작용
    # 퀘스트 NPC: M2 자리 이름 ↔ 옛 NPC ID
    NPCSPOT={'naru_chief':'촌장','naru_well_woman':'우물가 아낙','gaekju':'객주 주인','dakmok_chief':'닥목 촌장',
             'goryeo_songak_merchant':'송악 상인','goryeo_jangdan_farmer':'장단 농부'}
    for o in oldk.get('npc',[]):
        sp=items.get(NPCSPOT.get(o['npc'],''))
        if sp:put('npc',sp['x'],sp['y'],'npc:'+o['npc'],npc=o['npc'])
        else:report['missing'].append('npc:'+o['npc'])
    # 대화 주민·일꾼·낚시: 이름이 같은 M2 자리, 없으면 비슷한 역할 자리
    TALKSPOT={'뱃사공':['뱃사공'],'농부':['농부'],'땔감 장수':['길목 상인'],'나루 상인':['포구 상인'],'길목 안내인':['길목 상인'],'나루 짐꾼':['나루 짐꾼'],
              '시전 짐꾼':['시전 짐꾼'],'시전 행상':['시전 행상'],'종이 일꾼':['종이 일꾼'],'나루 낚시':['낚시터']}
    road_cells=np.argwhere((grade_local>0)&walk_local&inside)
    def road_mid():
        c=road_cells.mean(0);j=int(np.argmin(((road_cells-c)**2).sum(1)));return road_cells[j][1]+X0,road_cells[j][0]+Y0
    for kind in('talker','worker','pastime'):
        for o in oldk.get(kind,[]):
            sp=next((items[n] for n in TALKSPOT.get(o['name'],[]) if n in items),None)
            X,Y=(sp['x'],sp['y']) if sp else road_mid()
            k=dict(name=o['name'],character=o['character'],lines=o['lines'])
            p=put(kind,X,Y,f'{kind}:{o["name"]}',**k)
            if p and kind=='worker':
                # 두 작업 지점: 지금 자리와 걸을 수 있는 2~4칸 옆(직선 전체 통행 가능)
                for dx_,dy_ in((3,0),(-3,0),(0,3),(0,-3),(2,0),(-2,0),(0,2),(0,-2)):
                    xs=[p['x']+dx_*i//max(abs(dx_),abs(dy_)) for i in range(max(abs(dx_),abs(dy_))+1)] if dx_ else [p['x']]*(abs(dy_)+1)
                    ys=[p['y']+dy_*i//max(abs(dx_),abs(dy_)) for i in range(max(abs(dx_),abs(dy_))+1)] if dy_ else [p['y']]*(abs(dx_)+1)
                    if all(walkable(a_,b_) for a_,b_ in zip(xs,ys)):p['path']=[p['x'],p['y'],p['x']+dx_,p['y']+dy_];break
            if not sp:report['moved'].append(o['name']+'(길 가운데)')
    # 상인 행렬(개경 walker): 장터 광장 네 귀퉁이를 돈다
    pz=items.get('장터 광장')
    for o in oldk.get('walker',[]):
        if not pz:report['missing'].append('walker');continue
        x0,y0=L(pz['x']+1,pz['y']+1);x1,y1=L(pz['x']+pz['w']-2,pz['y']+pz['h']-2)
        pts=[(x0,y0),(x1,y0),(x1,y1),(x0,y1)] if o['idx']%2==0 else [(x1,y1),(x0,y1),(x0,y0),(x1,y0)]
        objs.append(O('walker',pts[0][0],pts[0][1],idx=o['idx'],path=[v for p_ in pts for v in p_]));report['placed'].append(f'walker:{o["idx"]}')
    # 지역 주민(villager): 광장·흙길 위에 고르게
    vil=oldk.get('villager',[])
    if vil:
        dirt=np.argwhere((grade_local==1)&walk_local&inside)
        chosen=[]
        for o in vil:
            if chosen:
                dd=np.min(np.hypot(*(dirt[:,None,:]-np.array(chosen)[None,:,:]).transpose(2,0,1)),axis=1)
                j=int(np.argmax(np.minimum(dd,10)))
            else:j=len(dirt)//2
            c=dirt[j];chosen.append(tuple(c))
            put('villager',c[1]+X0,c[0]+Y0,f'villager:{o["idx"]}',variant=o['variant'],idx=o['idx'])
    # 석등 불빛: 석등 그림마다(옛 맵의 석등 수보다 많아도 됨)
    for n,it in items.items():
        if it.get('asset')=='stone_lantern':
            objs.append(O('lantern',*L(it['x'],it['y']),id='goryeo_stone_lantern'));report['placed'].append('lantern:'+n)
    # 마을 발전 소품: 성장 자리에 옛 객체를 차례로(남으면 광장 가장자리)
    gro=oldk.get('growth',[]);spots=[items[n] for n in sorted(items) if n.startswith('성장 자리')]
    for i,o in enumerate(gro):
        if i<len(spots):X,Y=spots[i]['x'],spots[i]['y']
        elif pz:X,Y=pz['x']+(i-len(spots))*3,pz['y']+pz['h']
        else:continue
        put('growth',X,Y,f'growth:{i}',id=o['id'],town=o['town'],variant=o['variant'])
    # 주변음: 물에서 가까운 마을 자리
    for o in oldk.get('ambience',[]):
        wy,wx=np.nonzero(water_local&inside)
        if not len(wy):continue
        ref=items.get('나루터') or items.get('장터 광장')
        if ref:rx,ry=L(*cen(ref))
        else:ry,rx=h//2,w//2
        j=int(np.argmin((wy-ry)**2+(wx-rx)**2))
        objs.append(O('ambience',wx[j],wy[j],id=o['id'],radius=o['radius']));report['placed'].append('ambience')
    # 낚시(pastime)는 위에서. 도적: 길 가운데(위치는 E3·M5에서 다시)
    for o in oldk.get('bandit',[]):
        X,Y=road_mid();put('bandit',X,Y,'bandit',radius=o['radius'])
        # 도적 경고 이정표(옛 남동 길 이정표의 뜻을 새 배치에 맞게): 도적 자리 가까운 길 위
        put('sign',X+3,Y,'sign:bandit',text='솔바람 고개\n고개 큰길에는 도적이 나타날 수 있습니다.\n짐을 싣고 지날 때는 조심하세요.')
    # 방향과 상관없는 옛 이정표만 옮긴다(방향 안내 이정표는 옛 배치 기준이라 M5에서 새로 쓴다). 옛 이정표처럼 그림 없이 길 위 대화 자리.
    SIGNSPOT={'나루 하역장':'나루터','종이 작업장':'한지 작업장','약재 집산 마당':'장터 광장','장단 들마을':'장터 광장'}
    for o in old_signs:
        title=o['text'].split('\n')[0]
        sp=items.get(SIGNSPOT.get(title,''))
        if sp:put('sign',sp['x']+sp['w'],sp['y']+sp['h'],'sign:'+title,text=o['text'])
    return objs,report
