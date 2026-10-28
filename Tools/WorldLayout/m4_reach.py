"""M4 ③ 정적 검사: 시작 위치(나루 S0)에서 걸어서(출입구 포함) 모든 맵·시장·게시판·NPC에 닿는지.
입력: Assets/PocketCaravan/Editor/B2Preview/M4/Maps/*.json (또는 --maps 폴더)"""
import argparse,json,collections
from pathlib import Path
import numpy as np
from scipy import ndimage
ROOT=Path(__file__).resolve().parents[2]
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--maps',default=str(ROOT/'Assets/PocketCaravan/Editor/B2Preview/M4/Maps'));a=ap.parse_args()
    maps={};lab={}
    for f in sorted(Path(a.maps).glob('*.json')):
        d=json.loads(f.read_text());maps[d['id']]=d
        walk=np.array(d['blocked']).reshape(d['h'],d['w'])==0
        lab[d['id']]=ndimage.label(walk)[0]
    seen=set();q=collections.deque()
    start=next((m,o) for m,d in maps.items() for o in d['objects'] if o['kind']=='spawn' and o['id']=='S0')
    def comp(m,o):return lab[m][o['y'],o['x']]
    q.append((start[0],comp(*start)))
    while q:
        m,c=q.popleft()
        if (m,c) in seen or c==0:continue
        seen.add((m,c));d=maps[m]
        for o in d['objects']:
            if o['kind']!='exit':continue
            cells=[lab[m][o['y']+j,o['x']+i] for i in range(o['w']) for j in range(o['h'])]
            if c not in cells:continue
            t=maps[o['target']];s=next(p for p in t['objects'] if p['kind']=='spawn' and p['id']==o['spawn'])
            for i in range(s['w']):
                for j in range(s['h']):q.append((o['target'],lab[o['target']][s['y']+j,s['x']+i]))
    reached={m for m,_ in seen}
    rep=dict(mapsReached=sorted(reached),mapsMissing=sorted(set(maps)-reached),unreached=[])
    for m,d in maps.items():
        for o in d['objects']:
            if o['kind'] in('market','board','npc','talker','worker','pastime','villager'):
                if (m,lab[m][o['y'],o['x']]) not in seen:rep['unreached'].append(f"{m}:{o['kind']}:{o.get('npc') or o.get('town') or o.get('name')}")
    print(json.dumps(rep,ensure_ascii=False,indent=1));return 1 if rep['mapsMissing'] or rep['unreached'] else 0
if __name__=='__main__':raise SystemExit(main())
