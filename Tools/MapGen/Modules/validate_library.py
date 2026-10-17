"""모든 원형의 실제 통행, 소품 묶음/연결부와 atlas 교체의 구조 불변 검사."""
import sys,json,hashlib
from pathlib import Path
from collections import deque
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent));sys.path.insert(0,str(HERE))
from catalogue import ROOT,DEST
from layouts import build
from skins import SKINS,recolor
from tiles import STAMPS,K,tile_image
from PIL import Image


def reachable(m,start,roads=False):
    seen={start};q=deque([start])
    while q:
        x,y=q.popleft()
        for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
            if (xx,yy) in seen or not m.inside(xx,yy) or m.blocked[yy][xx] or (roads and not m.road[yy][xx]):continue
            seen.add((xx,yy));q.append((xx,yy))
    return seen


def validate():
    checks=[];failures=[];reports=[]
    def check(label,value):
        if not value:failures.append(label)
        checks.append(label)
    for src in sorted(DEST.glob('*.json')):
        spec=json.loads(src.read_text());m=build(spec);mid=spec['id']
        roads={(x,y) for y in range(m.h) for x in range(m.w) if m.road[y][x] and not m.blocked[y][x]}
        check(mid+': spawn accessible',(2,20) in roads)
        access=reachable(m,(2,20));roadAccess=reachable(m,(2,20),True)
        check(mid+': all roads connected',roads<=roadAccess)
        check(mid+': all bridge decks accessible',all((x,y) in access and not m.water[y][x] and not m.blocked[y][x] for x,y in m.decks))
        for edge in m.module_ports:
            cells={'W':[(0,y) for y in range(19,22)],'E':[(39,y) for y in range(19,22)],'N':[(x,0) for x in range(19,22)],'S':[(x,39) for x in range(19,22)]}[edge]
            check(mid+': port '+edge,all(p in roadAccess for p in cells))
        for name,x,y in m.stamps:
            s,sx,sy,w,h,_,high,rc=STAMPS[name]
            check(mid+': intact '+name+f'@{x},{y}',all((m.high if dy<high else m.obj)[y+dy][x+dx]==K(s,sx+dx,sy+dy,rc) for dy in range(h) for dx in range(w)))
            if name.startswith('house') or name in ['store','shop']:
                check(mid+': door accessible '+f'{x},{y}',(x+1,y+3) in access)
        check(mid+': all requested buildings placed',sum(name.startswith('house') or name in ['store','shop'] for name,x,y in m.stamps)==spec.get('houses',0))
        reports.append(dict(id=mid,roads=len(roads),ports=m.module_ports,stamps=len(m.stamps),water=sum(sum(r) for r in m.water)))
    base=ROOT/'Assets/PocketCaravan/MapModules';index=json.loads((base/'skin-index.json').read_text())
    signatures=set()
    for skin in SKINS:
        with Image.open(base/'Skins'/f'{skin[0]}.png') as atlas:
            check(skin[0]+': atlas dimensions',atlas.width==index['columns']*16 and atlas.height//16*index['columns']>=len(index['tileNames']))
            signatures.add(hashlib.sha256(atlas.tobytes()).hexdigest())
            if skin[0]=='meadow':alpha=atlas.getchannel('A').tobytes()
            else:check(skin[0]+': alpha/occupancy unchanged',atlas.getchannel('A').tobytes()==alpha)
    check('all environment atlas pixels distinct',len(signatures)==len(SKINS))
    report=dict(passed=len(checks)-len(failures),failed=failures,templates=reports,environments=len(SKINS),scope='정적 통행/묶음/연결부와 스킨 투명도 검사. 날씨의 시뮬레이션 효과/본편 등록/멀티 검증 아님.')
    out=ROOT/'output/module-library';out.mkdir(parents=True,exist_ok=True);(out/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(dict(checks=len(checks),failures=failures,templates=len(reports),environments=len(SKINS)),ensure_ascii=False,indent=2))
    if failures:raise SystemExit(1)
    return report
if __name__=='__main__':validate()
