"""Read-only survey and isolated C3a proposal export. Production maps stay unchanged."""
from collections import Counter,deque
import json,runpy,shutil,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from world_registry import atlas_content
import world_registry
from tiles import STAMPS
from occlusion import desired_rows,apply,BUILDINGS
OUT=ROOT/'ArtReview/C3a-2026-10-26'


def survey(entries,maps):
    plan=[]
    for entry,m in zip(entries,maps):
        spawn=next(o for o in m.objects if o['kind']=='spawn' and o['id']=='S0');start=(spawn['x'],spawn['y'])
        seen={start};todo=deque([start])
        while todo:
            x,y=todo.popleft()
            for p in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
                xx,yy=p
                if 0<=xx<m.w and 0<=yy<m.h and not m.blocked[yy][xx] and p not in seen:seen.add(p);todo.append(p)
        props=[]
        for name,x,y in m.stamps:
            if name not in STAMPS:continue
            _,_,_,w,h,_,high,_=STAMPS[name]
            kind='building' if name in BUILDINGS else 'tree' if name.startswith('goryeo_woodland') and h>=3 else None
            if not kind:continue
            sides=[]
            for side,px,py,dx,dy in [('north',x+w//2,y-1,0,-1),('south',x+w//2,y+h,0,1),('west',x-1,y+h//2,-1,0),('east',x+w,y+h//2,1,0)]:
                if (px,py) in seen:sides.append(dict(side=side,x=px,y=py,dx=dx,dy=dy))
            props.append(dict(name=name,kind=kind,x=x,y=y,w=w,h=h,beforeRows=high,afterRows=desired_rows(name,w,h,high),sides=sides))
        plan.append(dict(map=m.id,entry=entry,w=m.w,h=m.h,props=props))
    return plan


def main():
    entries,maps=atlas_content(include_occlusion=False);plan=survey(entries,maps);OUT.mkdir(exist_ok=True);(OUT/'before').mkdir(exist_ok=True)
    for e in entries:shutil.copy2(ROOT/e['jsonPath'],OUT/'before'/(e['mapId']+'.json'))
    (OUT/'plan.json').write_text(json.dumps(dict(maps=plan),ensure_ascii=False,indent=2)+'\n')
    result=apply(maps)
    (OUT/'moves.json').write_text(json.dumps({m.id:m.occlusion_moves for m in result},ensure_ascii=False,indent=2)+'\n')
    target=OUT/'candidate';animated=target/'Assets/PocketCaravan/Art/NinjaAdventure/Animated';animated.mkdir(parents=True,exist_ok=True)
    for i in range(4):shutil.copy2(ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Animated/ripples_{i}.png',animated/f'ripples_{i}.png')
    world_registry.atlas_content=lambda:(entries,result)
    sys.argv=['export.py',str(target)];runpy.run_path(str(ROOT/'Tools/MapGen/export.py'),run_name='__main__')
    # Assert the proposal changes only layer allocation, not collisions, roads or gameplay references.
    for e in entries:
        a=json.loads((ROOT/e['jsonPath']).read_text());b=json.loads((target/e['jsonPath']).read_text())
        for k in a:
            if k not in ('palette','ground','deco','obj','high') and a[k]!=b[k]:raise ValueError(('Gameplay changed',a['id'],k))
        for layer in ('ground','deco'):
            if [a['palette'][i] for i in a[layer]]!=[b['palette'][i] for i in b[layer]]:raise ValueError(('Ground art changed',a['id']))
    print('proposed layer cells',sum(len(m.occlusion_moves) for m in result),'props',sum(len(p['props']) for p in plan),'reachable sides',sum(len(p['sides']) for m in plan for p in m['props']))


if __name__=='__main__':main()
