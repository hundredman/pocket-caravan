"""고려·중앙 동쪽 생활 보강(2026-11-03): 말 거는 주민과 방향 이정표를 본편 맵 JSON에 덧붙인다.

- 맵을 다시 내보내지 않는다(A7d 자연 지형·장터 등 이후 작업을 지키기 위해). 이미 있는 맵 JSON에 객체·칸만 더한다.
- 다시 실행해도 같다: 앞서 붙인 것(id가 life_로 시작)과 바꾼 칸을 기록(goryeo_life_applied.json)으로 되돌린 뒤 다시 붙인다.
- 주민: goryeo_life.json. 자리는 M2 구역도 자리 이름(spot)이나 길 맵의 길 가운데(road). 길이 아닌 빈칸을 먼저 고른다.
- 이정표: 맵마다 이웃 맵(같은 지역) 한 곳당 하나. 그 이웃으로 가는 출구 가운데에서 안쪽으로 3~7칸, 길 옆 빈칸.
  그림은 승인된 장터 이정표와 같은 원본 타일(element_0_2)을 위 개체층에 놓고 그 칸을 막는다. 글은 화살표 + 방향 + 맵 이름.
- 사람과 이정표가 걸을 수 있는 칸을 둘로 나누거나 출구·기능 객체를 막지 않는지 확인한다(아니면 다른 칸).
- 다른 문명 연결 출구(link_)는 이정표를 세우지 않는다(통합 브랜치 region_links.json의 표지판이 맡는다).
사용: python3 Tools/WorldLayout/goryeo_life.py
"""
import json,collections
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SPEC=json.loads((Path(__file__).parent/'goryeo_life.json').read_text())
APPLIED=Path(__file__).parent/'goryeo_life_applied.json'
SIGN_TILE='element_0_2'
STEP={'N':(0,-1),'S':(0,1),'W':(-1,0),'E':(1,0)}
ARROW={'N':'↑ 북쪽','S':'↓ 남쪽','W':'← 서쪽','E':'→ 동쪽'}
EMPTY=dict(w=1,h=1,id='',npc='',town='',target='',spawn='',edge='',text='',name='',character='',lines=[],variant=0,idx=0,
           sheet='',frames=0,fps=0.0,radius=0.0,type='',path=[])

def O(kind,x,y,**k):
    o=dict(EMPTY);o.update(k);o['kind']=kind;o['x']=int(x);o['y']=int(y);return o

def regions():
    ids=[]
    for r in('goryeo','transit'):
        ids+=[(e['mapId'],ROOT/e['jsonPath']) for e in json.loads((ROOT/f'Assets/PocketCaravan/World/Regions/{r}.json').read_text())['maps']]
    return ids

class Map:
    def __init__(s,path):
        s.path=path;s.d=json.loads(path.read_text());s.w,s.h=s.d['w'],s.d['h'];s.props=set()
        # 승인 장터(Data/Goryeo/MarketLayout.json)의 좌판·게시판·이정표 그림 자리와 그 둘레 1칸
        ml=json.loads((ROOT/'Assets/PocketCaravan/Data/Goryeo/MarketLayout.json').read_text())
        for mm in ml['maps']:
            if mm['mapId']!=s.d['id']:continue
            for sp in mm['spaces']:
                for pr in sp['props']:
                    for a in range(-1,pr['w']+1):
                        for b in range(-1,pr['h']+1):s.props.add((pr['x']+a,pr['y']+b))
    def i(s,x,y):return y*s.w+x
    def inside(s,x,y):return 0<=x<s.w and 0<=y<s.h
    def walk(s,x,y):return s.inside(x,y) and not s.d['blocked'][s.i(x,y)]
    def exits(s):
        out={}
        for o in s.d['objects']:
            if o['kind']=='exit':
                for a in range(o['w']):
                    for b in range(o['h']):out[(o['x']+a,o['y']+b)]=o
        return out
    def occupied(s):
        return s.props | {(o['x'],o['y']) for o in s.d['objects'] if o['kind'] not in('exit','spawn','anim','ambience')} | \
               {(o['x']+a,o['y']+b) for o in s.d['objects'] if o['kind']=='spawn' for a in range(o['w']) for b in range(o['h'])}
    def components(s,extra_block=()):
        blk=set(extra_block);lab={};n=0
        for y in range(s.h):
            for x in range(s.w):
                if (x,y) in lab or not s.walk(x,y) or (x,y) in blk:continue
                n+=1;q=collections.deque([(x,y)]);lab[(x,y)]=n
                while q:
                    cx,cy=q.popleft()
                    for dx,dy in STEP.values():
                        p=(cx+dx,cy+dy)
                        if p not in lab and s.walk(*p) and p not in blk:lab[p]=n;q.append(p)
        return lab
    def safe_block(s,cell,base):
        """cell을 막아도 걸을 수 있는 칸이 나뉘지 않는가(이웃 칸들이 여전히 같은 덩어리)"""
        x,y=cell;nb=[(x+dx,y+dy) for dx,dy in STEP.values() if s.walk(x+dx,y+dy)]
        if len(nb)<=1:return True
        # 이웃끼리 cell을 빼고 8칸 반경 안에서 이어지는지 확인(전체 다시 세기보다 빠름)
        start=nb[0];seen={start};q=collections.deque([start])
        while q:
            p=q.popleft()
            for dx,dy in STEP.values():
                n_=(p[0]+dx,p[1]+dy)
                if n_ in seen or n_==cell or not s.walk(*n_) or abs(n_[0]-x)>10 or abs(n_[1]-y)>10:continue
                seen.add(n_);q.append(n_)
        return all(p in seen for p in nb)

def revert(maps):
    if not APPLIED.exists():return
    rec=json.loads(APPLIED.read_text())
    for mid,cells in rec.items():
        m=maps.get(mid)
        if not m:continue
        for c in cells:
            i=m.i(c['x'],c['y']);m.d['obj'][i]=m.d['palette'].index(c['obj']) if c['obj'] in m.d['palette'] else 0
            m.d['blocked'][i]=c['blocked'];m.d['reachable'][i]=c['reachable']
    for m in maps.values():m.d['objects']=[o for o in m.d['objects'] if not str(o.get('id','')).startswith('life_')]

def main():
    maps={mid:Map(p) for mid,p in regions()}
    revert(maps)
    zones=json.loads((ROOT/'ArtReview/M2-Zones/zones.json').read_text())['villages']
    spots=collections.defaultdict(dict)
    for vz in zones.values():
        for it in vz['items']:
            if it.get('missing') or 'x' not in it:continue
            for mid,m in maps.items():
                lx,ly=it['x']-m.d['terrainX'],it['y']-m.d['terrainY']
                if m.walk(lx,ly) or (m.inside(lx,ly) and any(m.walk(lx+dx,ly+dy) for dx,dy in STEP.values())):
                    # 맵 직사각형이 겹치므로 그 칸이 이 맵에서 걸을 수 있는(=이 맵 구역) 경우만
                    if m.walk(lx,ly) or m.d['ground'][m.i(lx,ly)]:spots[mid].setdefault(it['name'],(lx,ly))
    # 건물 문 앞(문 칸과 그 양옆·한 칸 앞)은 비운다
    doors=collections.defaultdict(set)
    for vz in zones.values():
        for it in vz['items']:
            if it.get('kind')!='lot' or it.get('missing') or 'x' not in it:continue
            dx_,dy_=it['x']+it['w']//2,it['y']+it['h']
            for mid,m in maps.items():
                for a_ in(-1,0,1):
                    for b_ in(0,1):doors[mid].add((dx_+a_-m.d['terrainX'],dy_+b_-m.d['terrainY']))
    for mid,m in maps.items():m.props|=doors[mid]
    applied=collections.defaultdict(list);report=collections.defaultdict(list)
    def nearest(m,x,y,avoid,prefer_offroad=True,maxr=8):
        ex=m.exits()
        for offroad in((True,False) if prefer_offroad else (False,)):
            for r in range(0,maxr+1):
                ring=[(x+dx,y+dy) for dx in range(-r,r+1) for dy in range(-r,r+1) if max(abs(dx),abs(dy))==r]
                for p in ring:
                    if not m.walk(*p) or p in ex or p in avoid or not m.d['reachable'][m.i(*p)]:continue
                    if offroad and m.d['road'][m.i(*p)]:continue
                    if any(q in ex for q in [(p[0]+dx,p[1]+dy) for dx,dy in STEP.values()]):continue
                    if m.safe_block(p,None):return p
        return None
    for t in SPEC['talkers']:
        m=maps[t['map']];occ=m.occupied()
        if t.get('road'):
            rc=[(i%m.w,i//m.w) for i,g in enumerate(m.d['road']) if g and not m.d['blocked'][i]]
            cx=sum(p[0] for p in rc)/len(rc);cy=sum(p[1] for p in rc)/len(rc);x,y=min(rc,key=lambda p:(p[0]-cx)**2+(p[1]-cy)**2)
        else:
            if t['spot'] not in spots[t['map']]:report[t['map']].append('자리 없음: '+t['spot']);continue
            x,y=spots[t['map']][t['spot']]
        p=nearest(m,x,y,occ)
        if not p:report[t['map']].append('빈칸 없음: '+t['name']);continue
        m.d['objects'].append(O('talker',*p,id='life_'+t['name'],name=t['name'],character=t['character'],lines=t['lines']))
        report[t['map']].append(f"주민 {t['name']} {p}")
    # 이정표
    for mid,m in maps.items():
        ex=m.exits();runs=collections.defaultdict(list)
        for o in m.d['objects']:
            if o['kind']=='exit' and o['target'] in maps and not str(o.get('spawn','')).startswith('link_'):
                cells=[(o['x']+a,o['y']+b) for a in range(o['w']) for b in range(o['h'])]
                roadn=sum(1 for c in cells if m.d['road'][m.i(*c)])
                runs[o['target']].append((roadn,len(cells),o,cells))
        if 'SIGN_TILE' not in m.d['palette']:pass
        for target,lst in sorted(runs.items()):
          lst.sort(key=lambda r:(-r[0],-r[1]));placed=None
          for roadn,n,o,cells in lst:
            dx,dy=STEP[o['edge']];mx,my=cells[len(cells)//2]
            occ=m.occupied()
            for k in range(3,9):
                bx,by=mx-dx*k,my-dy*k
                for off in(1,-1,2,-2,3,-3,0):
                    px,py=(bx+off,by) if dx==0 else (bx,by+off)
                    p=(px,py)
                    if not m.walk(*p) or p in ex or p in occ or m.d['road'][m.i(*p)] or not m.d['reachable'][m.i(*p)]:continue
                    if any(m.d[l][m.i(*p)] for l in('obj','high')):continue
                    if roadn and not any(m.walk(p[0]+a,p[1]+b) and m.d['road'][m.i(p[0]+a,p[1]+b)] for a,b in STEP.values()):continue   # 길 옆
                    if any((p[0]+a,p[1]+b) in ex or (p[0]+a,p[1]+b) in occ for a,b in STEP.values()):continue
                    if not m.safe_block(p,None):continue
                    placed=p;break
                if placed:break
            if placed:break
          if True:
            name=json.loads((maps[target].path).read_text())['name'] if False else maps[target].d['name']
            if not placed:report[mid].append(f'이정표 자리 없음: {name}');continue
            i=m.i(*placed)
            applied[mid].append(dict(x=placed[0],y=placed[1],obj=m.d['palette'][m.d['obj'][i]],blocked=m.d['blocked'][i],reachable=m.d['reachable'][i]))
            if SIGN_TILE not in m.d['palette']:m.d['palette'].append(SIGN_TILE)
            m.d['obj'][i]=m.d['palette'].index(SIGN_TILE);m.d['blocked'][i]=1;m.d['reachable'][i]=0
            m.d['objects'].append(O('sign',*placed,id='life_sign_'+target,text=f"{ARROW[o['edge']]}: {name}"))
            report[mid].append(f"이정표 {ARROW[o['edge']]} {name} {placed}")
    # 확인: 기능 객체·출구가 여전히 같은 덩어리에서 닿는지(주민 칸은 막힌 것으로 본다)
    for mid,m in maps.items():
        tk=[(o['x'],o['y']) for o in m.d['objects'] if o['kind']=='talker' and str(o.get('id','')).startswith('life_')]
        before=len(set(m.components().values()))
        after=len(set(m.components(tk).values()))
        if after>before:raise SystemExit(f'{mid}: 주민이 길을 막음 ({before}→{after})')
        m.path.write_text(json.dumps(m.d,ensure_ascii=False,separators=(',',':')))
    APPLIED.write_text(json.dumps(applied,ensure_ascii=False,indent=1))
    for mid in maps:
        print(mid,'\n  '+'\n  '.join(report[mid]) if report[mid] else '')
if __name__=='__main__':main()
