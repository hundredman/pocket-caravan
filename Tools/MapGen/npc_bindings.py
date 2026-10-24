"""C1 role-specific character paths, applied to the authored final maps before export."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def roles():
    rows=json.loads((ROOT/'Tools/ArtGen/npc_roles.json').read_text())['roles']
    if len({r['target'] for r in rows})!=len(rows) or len({r['path'] for r in rows})!=len(rows):raise ValueError('Duplicate NPC role/path')
    return {r['target']:r for r in rows}
def apply(maps):
    lookup=roles()
    for m in maps:
        for o in m.objects:
            if o.get('character'):
                target=f'map:{m.id}:{o["kind"]}:{o["name"]}'
                o['character']=lookup[target]['path']
        if m.id=='Naru' and not any(o.get('npc')=='naru_well_woman' for o in m.objects):
            _,wx,wy=next(s for s in m.stamps if s[0]=='well')
            occupied={(o['x'],o['y']) for o in m.objects}
            cells=[(x,y) for y in range(max(1,wy-3),min(m.h-1,wy+5)) for x in range(max(1,wx-3),min(m.w-1,wx+5)) if (x,y) not in occupied and not m.blocked[y][x] and (x,y) in m.world_reachable]
            x,y=min(cells,key=lambda p:(abs(p[0]-wx)+abs(p[1]-(wy+2)),p[1],p[0]))
            m.add('npc',x,y,npc='naru_well_woman')
    return maps
