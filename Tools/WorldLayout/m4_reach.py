"""M4 ③ 정적 검사: 시작 위치(나루 S0)에서 걸어서(출입구 포함) 모든 맵·시장·게시판·NPC에 닿는지.
표준 라이브러리만 쓴다(CI는 Pillow만 설치). 입력: 맵 JSON 폴더(--maps, 기본 미리보기 폴더)"""
import argparse,json,collections
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def components(d):
    """걸을 수 있는 칸의 4방향 연결 덩어리 번호(0 = 막힘)"""
    w,h=d['w'],d['h'];bl=d['blocked'];lab=[0]*(w*h);n=0
    for i in range(w*h):
        if bl[i] or lab[i]:continue
        n+=1;lab[i]=n;q=collections.deque([i])
        while q:
            c=q.popleft();x,y=c%w,c//w
            for nx,ny in((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
                if 0<=nx<w and 0<=ny<h:
                    j=ny*w+nx
                    if not bl[j] and not lab[j]:lab[j]=n;q.append(j)
    return lab

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--maps',default=str(ROOT/'Assets/PocketCaravan/Editor/B2Preview/M4/Maps'));a=ap.parse_args()
    maps={};lab={}
    mine={e['mapId'] for r in('goryeo','transit') for e in json.loads((ROOT/f'Assets/PocketCaravan/World/Regions/{r}.json').read_text())['maps']}   # 고려·중앙 동쪽 13맵만(통합 브랜치에는 다른 지역 맵도 같은 폴더에 있다)
    for f in sorted(Path(a.maps).glob('*.json')):
        d=json.loads(f.read_text())
        if d['id'] not in mine:continue
        maps[d['id']]=d;lab[d['id']]=components(d)
    def at(m,x,y):d=maps[m];return lab[m][y*d['w']+x]
    seen=set();q=collections.deque()
    start=next(('Naru',o) for o in maps['Naru']['objects'] if o['kind']=='spawn' and o['id']=='S0')   # 본편 시작 맵(통합 브랜치에는 다른 지역 S0도 있다)
    q.append((start[0],at(start[0],start[1]['x'],start[1]['y'])))
    while q:
        m,c=q.popleft()
        if (m,c) in seen or c==0:continue
        seen.add((m,c))
        for o in maps[m]['objects']:
            if o['kind']!='exit' or o['target'] not in maps:continue   # 다른 문명 연결(통합 브랜치)
            if c not in {at(m,o['x']+i,o['y']+j) for i in range(o['w']) for j in range(o['h'])}:continue
            t=maps[o['target']];s=next(p for p in t['objects'] if p['kind']=='spawn' and p['id']==o['spawn'])
            for i in range(s['w']):
                for j in range(s['h']):q.append((o['target'],at(o['target'],s['x']+i,s['y']+j)))
    reached={m for m,_ in seen}
    rep=dict(mapsReached=sorted(reached),mapsMissing=sorted(set(maps)-reached),unreached=[])
    for m,d in maps.items():
        for o in d['objects']:
            if o['kind'] in('market','board','npc','talker','worker','pastime','villager') and (m,at(m,o['x'],o['y'])) not in seen:
                rep['unreached'].append(f"{m}:{o['kind']}:{o.get('npc') or o.get('town') or o.get('name')}")
    print(json.dumps(rep,ensure_ascii=False,indent=1));return 1 if rep['mapsMissing'] or rep['unreached'] else 0
if __name__=='__main__':raise SystemExit(main())
