"""원형을 격자에 교체/배치하는 제작 도구. 그림만 회전하면 나무가 조각나므로 회전은 지원하지 않는다."""
import json,sys
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
from catalogue import ROOT
BASE=ROOT/'Assets/PocketCaravan/MapModules'
sys.path.insert(0,str(ROOT/'Tools/Integration'))
from obstacle_validation import validate_placements

def check_obstacles(data):
    if data.get('obstacles'):
        defs=json.loads((ROOT/'Assets/PocketCaravan/Obstacles/definitions.json').read_text())['definitions']
        errors=validate_placements(data,{d['id']:d for d in defs})
        if errors:raise ValueError('; '.join(errors))

OPPOSITE={'E':'W','W':'E','N':'S','S':'N'}


def compose(recipe):
    entries={e['id']:e for e in json.loads((BASE/'catalogue.json').read_text())['entries']}
    cells={}
    for c in recipe['cells']:
        p=(c['x'],c['y'])
        if any(type(v) is not int or v<0 for v in p) or p in cells:raise ValueError('음수/중복 격자 좌표')
        if c.get('rotation',0)!=0:raise ValueError('원형 회전은 source 경로/배치를 수정해 새 원형으로 만드세요.')
        cells[p]=entries[c['module']]
    if not cells:raise ValueError('배치할 원형이 없습니다.')
    connections={p:set() for p in cells}
    for (x,y),entry in cells.items():
        for edge,(dx,dy) in {'E':(1,0),'W':(-1,0),'N':(0,-1),'S':(0,1)}.items():
            p=(x+dx,y+dy)
            if p not in cells:continue
            a=edge in entry['ports'];b=OPPOSITE[edge] in cells[p]['ports']
            if a!=b:raise ValueError(f'연결부 불일치: {(x,y)} {edge} {p}')
            if a:connections[x,y].add(p)
    first=min(cells,key=lambda p:(p[1],p[0]));seen={first};queue=[first]
    for p in queue:
        for q in connections[p]:
            if q not in seen:seen.add(q);queue.append(q)
    if len(seen)!=len(cells):raise ValueError('설계된 교역로로 연결되지 않은 구역이 있습니다.')
    w=(max(x for x,y in cells)+1)*40;h=(max(y for x,y in cells)+1)*40
    out=dict(id=recipe['id'],name=recipe['name'],w=w,h=h,hours=0,palette=[''],objects=[])
    for layer in ['ground','deco','obj','high','road','blocked']:out[layer]=[1 if layer=='blocked' else 0]*(w*h)
    palette={'':0}
    for (cx,cy),entry in cells.items():
        f=json.loads((ROOT/entry['jsonPath']).read_text());check_obstacles(f);remap={0:0}
        for hazard in f.get('obstacles',[]):
            placed=dict(hazard);placed['id']=f'c{cx}_{cy}_'+hazard['id'];placed['x']+=cx*40;placed['y']+=cy*40;out.setdefault('obstacles',[]).append(placed)
        for i,key in enumerate(f['palette'][1:],1):
            if key not in palette:palette[key]=len(out['palette']);out['palette'].append(key)
            remap[i]=palette[key]
        for y in range(40):
            for x in range(40):
                src=y*40+x;dst=(cy*40+y)*w+cx*40+x
                for layer in ['ground','deco','obj','high','road','blocked']:
                    value=f[layer][src];out[layer][dst]=remap[value] if layer in ['ground','deco','obj','high'] else value
    out['objects']=[dict(kind='spawn',x=first[0]*40+2,y=first[1]*40+20,id='S0',edge='W')]
    return out

if __name__=='__main__':
    recipe=Path(sys.argv[1]) if len(sys.argv)>1 else BASE/'Recipes/four_crossroads.json'
    output=Path(sys.argv[2]) if len(sys.argv)>2 else BASE/'Compositions/four_crossroads.json'
    output.parent.mkdir(parents=True,exist_ok=True);data=compose(json.loads(recipe.read_text()));output.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n');print(f'{data["w"]}x{data["h"]} composition -> {output}')
