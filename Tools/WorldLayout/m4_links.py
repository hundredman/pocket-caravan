"""다른 문명과 잇는 자리(통합 브랜치 World/Connections/region_links.json의 고려 쪽 끝).

은샘 2026-10-28: 새 중앙 길목 마을(transit_crossroads)의 서쪽 끝(X자 4등분 경계라 대각선 계단 모양)을
조금 평평하게 깎아 곧은 출구 자리를 만들고, 마을 흙길에서 그 자리까지 흙길을 잇는다.
- 깎기: 출구 줄보다 바깥쪽에 있는 우리 칸만 담당 밖(그리지 않고 막힘)으로 돌린다. 바깥(다른 팀) 칸은 늘리지 않는다.
- 폭: 상대 쪽 출구 폭에 맞춘다(⑪ 북유럽 생성기 4칸, ⑧ 아라비아 3칸, ④ 아라비아 2칸).
- 출구·도착 객체는 통합 브랜치의 Tools/Integration/connect_regions.py가 넣는다(맵을 다시 만들면 그 스크립트를 다시 실행).
  m4_export가 report.json의 links에 맵 안 좌표를 적는다. region_links.json의 고려 쪽 x·y·w·h는 그 값을 쓴다.
at: 세계 좌표(terrain_v3) 대략 자리. 실제 줄은 그 근처 경계에서 정한다.
"""
import numpy as np
import terrain_v3 as v

MAP='east/중앙 길목 마을'
LINKS=[dict(id='⑪',other='north_c_road_se',edge='N',at=(29,-26),n=4),
       dict(id='⑧',other='arabia_central_steppe',edge='W',at=(1,-1),n=3),
       dict(id='④',other='arabia_central_eastvale',edge='W',at=(13,11),n=2)]

def notch(own,mapid,keys):
    """own·mapid를 고친다(제자리). 반환: [{id,other,edge,cells:[(iy,ix)...]}] 배열 좌표의 출구 칸"""
    k=keys.index(MAP);mine=(mapid==k)&own;out=[]
    for L in LINKS:
        ay,ax=v.idx(*L['at']);n=L['n']
        if L['edge']=='W':
            rows=range(ay-(n-1)//2,ay-(n-1)//2+n)
            first=[int(np.argmax(mine[r])) for r in rows];X=max(first)
            for r in rows:
                cut=np.zeros(own.shape[1],bool);cut[:X]=True;cut&=mine[r]
                own[r,cut]=False;mapid[r,cut]=-1;mine[r,cut]=False
            cells=[(r,X) for r in rows]
        else:
            cols=range(ax-(n-1)//2,ax-(n-1)//2+n)
            first=[int(np.argmax(mine[:,c])) for c in cols];Y=max(first)
            for c in cols:
                cut=np.zeros(own.shape[0],bool);cut[:Y]=True;cut&=mine[:,c]
                own[cut,c]=False;mapid[cut,c]=-1;mine[cut,c]=False
            cells=[(Y,c) for c in cols]
        out.append(dict(L,cells=cells))
    return out
