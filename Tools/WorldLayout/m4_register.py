"""M4 ④ 지역 등록: 새 맵 13개를 World/Regions에 올린다(고려 5 + 중앙 동쪽 8).
기존 맵 ID·씬 경로·논리 좌표는 그대로 두고, 새 맵 2개(transit_southport, transit_south_inland)만 더한다.
생성기 표시는 새 원본(Tools/WorldLayout/m4_export.py)으로 바꾼다(옛 MapGen 생성기는 본편에서 은퇴)."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'Assets/PocketCaravan/World/Regions'
GEN='worldlayout:m4_export'
NEW=[dict(mapId='transit_southport',jsonPath='Assets/PocketCaravan/Maps/transit_southport.json',scenePath='Assets/PocketCaravan/Scenes/Transit/transit_southport.unity',worldX=8,worldY=1,generator=GEN),
     dict(mapId='transit_south_inland',jsonPath='Assets/PocketCaravan/Maps/transit_south_inland.json',scenePath='Assets/PocketCaravan/Scenes/Transit/transit_south_inland.unity',worldX=10,worldY=0,generator=GEN)]
for name in('goryeo.json','transit.json'):
    p=R/name;d=json.loads(p.read_text())
    for m in d['maps']:m['generator']=GEN
    if name=='transit.json':
        have={m['mapId'] for m in d['maps']}
        d['maps']+=[m for m in NEW if m['mapId'] not in have]
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
    print(name,[m['mapId'] for m in d['maps']])

# ⑤ 옛 저장 이주 표(Resources/GoryeoLayout.json): 옛 별칭은 그대로, 맵마다 도착 자리(옛 배치 저장이 다시 시작할 곳)와 떨어진 짐 자리.
def arrival(d):
    w,h=d['w'],d['h'];bl=d['blocked']
    def ok(x,y):return 0<=x<w and 0<=y<h and not bl[y*w+x]
    pick=next((o for o in d['objects'] if o['kind']=='spawn' and o['id']=='S0'),None) or next((o for o in d['objects'] if o['kind']=='market'),None)
    if pick:x,y=pick['x'],pick['y']+(1 if pick['kind']=='market' else 0)
    else:
        cells=[(i%w,i//w) for i,g in enumerate(d['road']) if g>0 and not bl[i]]
        cx=sum(c[0] for c in cells)/len(cells);cy=sum(c[1] for c in cells)/len(cells)
        x,y=min(cells,key=lambda c:(c[0]-cx)**2+(c[1]-cy)**2)
    for dx,dy in((0,0),(0,1),(1,0),(-1,0),(0,-1)):
        if ok(x+dx,y+dy):x,y=x+dx,y+dy;break
    dxy=next(((x+a,y+b) for a,b in((1,0),(-1,0),(0,1),(0,-1)) if ok(x+a,y+b)),(x,y))
    U=lambda cx,cy:(cx+.5,h-1-cy+.82)   # Unity 위치(발밑 보정은 옛 표와 같게)
    sx,sy=U(x,y);dxw,dyw=U(*dxy)
    return dict(id=d['id'],offsetX=0,offsetY=0,height=h,legacyHeight=h,spawnX=sx,spawnY=round(sy,2),dropX=dxw,dropY=round(dyw,2))
GL=ROOT/'Assets/PocketCaravan/Resources/GoryeoLayout.json'
g=json.loads(GL.read_text())
ids=[m['mapId'] for name in('goryeo.json','transit.json') for m in json.loads((R/name).read_text())['maps']]
g['maps']=[arrival(json.loads((ROOT/f'Assets/PocketCaravan/Maps/{i}.json').read_text())) for i in ids]
GL.write_text(json.dumps(g,indent=2)+'\n')
print('GoryeoLayout.json',len(g['maps']),'maps,',len(g['aliases']),'aliases')
