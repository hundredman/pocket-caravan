"""북유럽 N2 구역도(검토용). N1 결과(ArtReview/North-N1/n1_grid.npz) 위에 마을 기능 자리와 이동 구간의 쉼터, 채집·사냥, 이벤트 자리를 잡는다.

자리 잡는 규칙은 고려 Tools/WorldLayout/m2_zones.py(은샘)의 Planner를 그대로 옮겨 왔다(건물 문 → 길 2칸 폭 골목 24칸 이하, 1칸 띄움, 물가·바닷가 조건).
북유럽에 맞춘 점
- 건물 그림 칸 크기는 북유럽 초안(롱하우스, 흙지붕 집, 오두막, 목욕 회관 등). 그림은 N3에서 정하고, 자리는 이 크기로 잡는다
- 석등 대신 횃불, 새 종류: 동굴 입구(cliff: 산·빙하에 붙은 자리), 채집·사냥(gather), 이벤트(event: 길 위), 도적(bandit: 길 위)
- 이동 구간 맵 5개: 쉼터(모닥불) + 맵별 작은 시설 + 채집·사냥 + 이벤트. 없어지는 국경 고개·피오르 길의 내용은 비르카·피오르 나루 맵의 길가로 옮긴다
- 지금 북유럽 맵에 있는 시장·게시판 ID와 NPC(npc ID, 이름 있는 talker)는 모두 같은 ID·이름으로 자리를 받는다
출력: ArtReview/North-N2/<map>.json, <map>.png, zones.json, check.json, overview.png
"""
import json, math, sys
from collections import deque
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image, ImageDraw, ImageFont

N1 = Path(sys.argv[1]); OUT = Path(sys.argv[2]); FONT = sys.argv[3]
OUT.mkdir(parents=True, exist_ok=True)
G = np.load(N1 / 'n1_grid.npz'); X0, Y0 = int(G['X0']), int(G['Y0'])
T_, MAPID, ROADW, VILLAGE, VLABEL, ELEV = G['t'], G['mapid'], G['road'], G['village'], G['vlabel'], G['elev']
H, W = T_.shape
VOID, DEEP, SHALLOW, GRAVEL, ROCKCOAST, RIVER, BOG, FIELD, GRASS, TUNDRA, HILL, FOREST, SNOW, MOUNTAIN, PEAK, LAKE, SPRING = range(17)
COLORS = {DEEP: (52, 98, 140), SHALLOW: (96, 156, 186), GRAVEL: (196, 190, 172), ROCKCOAST: (120, 118, 112), RIVER: (84, 146, 196),
          BOG: (104, 120, 92), FIELD: (196, 176, 96), GRASS: (122, 160, 96), TUNDRA: (156, 164, 124), HILL: (128, 140, 98),
          FOREST: (40, 82, 64), SNOW: (226, 232, 236), MOUNTAIN: (118, 110, 106), PEAK: (206, 226, 242), LAKE: (70, 136, 186),
          SPRING: (110, 200, 196), VOID: (28, 34, 40)}
KEYS = ['north_birka', 'north_herd', 'north_hunter', 'north_naru', 'north_laugar', 'north_icecave', 'north_runehill',
        'north_c_gatetown', 'north_c_snowwood', 'north_c_creek', 'north_c_crossing',
        'north_c_road_nw', 'north_c_road_ne', 'north_c_runeway', 'north_c_road_sw', 'north_c_road_se']
TOWNS = ['north_birka', 'north_laugar', 'north_runehill', 'north_icecave', 'north_naru', 'north_herd', 'north_hunter',
         'north_c_gatetown', 'north_c_snowwood', 'north_c_creek', 'north_c_crossing']   # N1 vlabel 순서
NAME = {'north_birka': '비르카', 'north_herd': '순록 고원 마을', 'north_hunter': '모피 사냥꾼 마을', 'north_naru': '피오르 나루 마을',
        'north_laugar': '라우가르 온천 마을', 'north_icecave': '얼음 동굴 골짜기', 'north_runehill': '룬돌 언덕',
        'north_c_gatetown': '북문 마을', 'north_c_snowwood': '눈숲 마을', 'north_c_creek': '개울 나루 마을', 'north_c_crossing': '북쪽 길목 마을',
        'north_c_road_nw': '눈숲 고갯길', 'north_c_road_ne': '개울 다리길', 'north_c_runeway': '룬돌 큰길', 'north_c_road_sw': '서남 솔숲길',
        'north_c_road_se': '동남 물가길'}

# 북유럽 건물·소품 칸 크기(초안). 그림은 N3에서 정함
# 북유럽 건물·소품 칸 크기 = N3에서 고른 실제 그림 크기(2026-10-29). ART: 그림 이름(north_tiles STAMPS, Minigame 그림, N3 새 그림)
ASSET = {'hall': (8, 6), 'longhouse': (4, 5), 'turf': (3, 2), 'hut': (3, 3), 'store': (3, 3), 'shop': (4, 4), 'bathhouse': (3, 4),
         'brewery': (3, 3), 'smithy': (4, 4), 'mill': (3, 3), 'kiln': (3, 3), 'tent': (3, 3), 'rack': (4, 3), 'boat': (5, 2),
         'well': (1, 2), 'brazier': (1, 2), 'runestone': (1, 2), 'woodpile': (2, 1), 'laundry': (2, 2), 'fire': (2, 2), 'furyard': (4, 3)}
ART = {'hall': 'new:jarlhall_A', 'longhouse': 'woodhouse', 'turf': 'h_turf', 'hut': 'h_shed', 'store': 'h_shed', 'shop': 'h_dark',
       'bathhouse': 'h_round', 'brewery': 'h_dome', 'smithy': 'new:smithy_A', 'mill': 'watermill', 'kiln': 'new:kiln_A',
       'tent': 'Minigame:stall_fur', 'rack': 'Minigame:yard_hjell_fresh', 'furyard': 'Minigame:yard_fur_work', 'boat': 'boat',
       'well': 'well', 'brazier': 'lantern', 'runestone': 'runestone', 'woodpile': 'logs', 'laundry': 'laundry', 'fire': 'fire',
       'cliff': 'new:cave_A'}
KIND_COL = {'moor': (60, 120, 255), 'harbor': (40, 200, 220), 'plaza': (255, 214, 90), 'yard': (255, 170, 60), 'rest': (255, 140, 40),
            'lot': (240, 110, 90), 'shore': (90, 190, 255), 'spot': (255, 255, 255), 'npc': (255, 120, 220), 'lantern': (255, 250, 170),
            'growth': (150, 240, 140), 'cliff': (190, 230, 255), 'gather': (120, 255, 120), 'event': (255, 60, 60), 'bandit': (60, 0, 0)}

def L(name, asset, anchor='plaza', **k):
    w, h = ASSET[asset]; return dict(name=name, kind='lot', asset=asset, w=w, h=h, anchor=anchor, **k)
def P(name, asset, anchor, kind='spot', **k):
    w, h = ASSET[asset]; return dict(name=name, kind=kind, asset=asset, w=w, h=h, anchor=anchor, **k)
def N(name, anchor, **k): return dict(name=name, kind='npc', w=1, h=1, anchor=anchor, **k)
def Gr(n): return [dict(name=f'성장 자리 {i + 1}', kind='growth', w=1, h=1, anchor='spread') for i in range(n)]

def village(mid, plaza, yard, houses, special, npcs, lanterns=2, growth=4, market=True):
    it = []
    if market: it.append(dict(name='장터 광장', kind='plaza', w=plaza[0], h=plaza[1], anchor='center', id=f'market:{mid}'))
    it += special
    if yard: it.append(dict(name='이벤트 마당', kind='yard', w=yard[0], h=yard[1], anchor='away'))
    it += [L(f'집 {i + 1}', a, 'plaza') for i, a in enumerate(houses)]
    if market: it.append(dict(name='게시판', kind='spot', w=1, h=1, anchor='plaza', id=f'board:{mid}'))
    it.append(P('우물', 'well', 'houses'))
    it += [P(f'횃불 {i + 1}', 'brazier', 'plaza', kind='lantern') for i in range(lanterns)]
    return it + npcs + Gr(growth)

# ── 마을별 목록. id = 지금 북유럽 맵의 시장·게시판 town ID, npc ID. 이름 있는 talker는 이름 그대로 ──
VILLAGES = {
 'north_birka': village('north_birka', (14, 8), (8, 5), ['turf', 'longhouse', 'turf', 'longhouse', 'turf', 'turf'],
    [L('얄의 장원', 'hall', 'center', memo='수도의 큰 집. 얄 시그리드'), L('여관', 'longhouse', 'plaza', id='npc:north_innkeeper'),
     L('상인 길드', 'shop', 'plaza'), L('대장간', 'smithy', 'plaza'),
     dict(name='훈련장', kind='yard', w=8, h=5, anchor='away', memo='훈련하는 전사')],
    [N('얄 시그리드', '얄의 장원', memo='talker'), N('여관 주인', '여관', id='north_innkeeper'), N('상인 길드장', '상인 길드', memo='talker'),
     N('훈련하는 전사', '훈련장', memo='talker'), N('north_husband', 'houses', id='north_husband'), N('north_har', 'plaza', id='north_har'),
     N('north_bold', '대장간', id='north_bold')], lanterns=4, growth=5),
 'north_herd': village('north_herd', (12, 7), (8, 5), ['turf', 'turf', 'hut', 'turf'],
    [dict(name='순록 우리', kind='yard', w=9, h=6, anchor='away', memo='순록 치는 울타리'), L('썰매 공방', 'smithy', 'plaza'),
     P('모닥불', 'fire', 'plaza')],
    [N('순록 목동 할머니', '순록 우리', memo='talker'), N('썰매 장인', '썰매 공방', memo='talker'), N('불가의 아이', '모닥불', memo='talker')]),
 'north_hunter': village('north_hunter', (12, 7), (8, 5), ['hut', 'turf', 'hut'],
    [L('털옷 공방', 'shop', 'plaza', memo='모피 2 → 털옷'), L('사냥꾼 오두막', 'hut', 'plaza'),
     P('털옷 작업 마당', 'furyard', '털옷 공방')],
    [N('늙은 사냥꾼', '사냥꾼 오두막', memo='talker'), N('north_goats', 'houses', id='north_goats'), N('north_wife', 'houses', id='north_wife')]),
 'north_naru': village('north_naru', (12, 7), (8, 5), ['turf', 'hut', 'turf', 'longhouse'],
    [dict(name='항구 부두', kind='harbor', w=3, h=8, anchor='sea', memo='피오르 선착장'), P('배 1', 'boat', '항구 부두', kind='moor'),
     P('배 2', 'boat', '항구 부두', kind='moor'), L('창고', 'store', '항구 부두', id='npc:north_warehouse'), L('선장 집', 'turf', '항구 부두'),
     P('대구 덕장 1', 'rack', 'plaza', memo='로포텐 덕장: 얼음 대구'), P('대구 덕장 2', 'rack', 'plaza')],
    [N('선장', '항구 부두', memo='talker'), N('창고지기', '창고', id='north_warehouse'), N('톰테 할아버지', 'houses', memo='talker')]),
 'north_laugar': village('north_laugar', (12, 7), (8, 5), ['turf', 'turf', 'longhouse', 'hut'],
    [L('목욕 회관', 'bathhouse', 'water', memo='온천 호숫가', first=True), dict(name='온천 김 1', kind='shore', w=2, h=1, anchor='water', memo='김 오르는 물가(anim)'),
     dict(name='온천 김 2', kind='shore', w=2, h=1, anchor='water'), L('꿀술 양조장', 'brewery', 'plaza', memo='꿀술 2 ...'),
     L('약초꾼 오두막', 'hut', 'plaza')],
    [N('목욕 회관 주인', '목욕 회관', memo='talker'), N('약초꾼', '약초꾼 오두막', memo='talker'), N('쉬어 가는 상인', 'plaza', memo='talker')]),
 'north_icecave': village('north_icecave', None, None, ['hut', 'hut'],
    [dict(name='광부 캠프 마당', kind='plaza', w=8, h=5, anchor='center', memo='시장 없음(지금과 같음)'),
     dict(name='동굴 입구', kind='cliff', w=5, h=3, asset='cliff', anchor='away', memo='아래층은 ... (지금 표지 그대로)'),
     L('광부 오두막', 'hut', 'plaza'), L('장비 창고', 'store', 'plaza'),
     dict(name='얼음 낚시 구멍', kind='shore', w=2, h=1, anchor='water')],
    [N('광부 할아범', '광부 오두막', memo='talker'), N('얼음 낚시꾼', '얼음 낚시 구멍', memo='talker')], lanterns=2, growth=2, market=False),
 'north_runehill': village('north_runehill', None, None, ['hut'],
    [dict(name='룬돌 원', kind='plaza', w=9, h=9, anchor='center', memo='옛 사람들의 룬돌. 시장 없음(지금과 같음)'),
     L('룬 지기 오두막', 'hut', 'plaza')] + [P(f'룬석 {i + 1}', 'runestone', '룬돌 원') for i in range(5)],
    [N('룬 지기 노인', '룬 지기 오두막', memo='talker'), N('음유시인', '룬돌 원', memo='talker')], lanterns=2, growth=2, market=False),
 'north_c_gatetown': village('north_post', (12, 7), (8, 5), ['turf', 'hut', 'turf', 'turf'],
    [L('촌장 집', 'turf', 'plaza'), L('여관', 'longhouse', 'plaza'), P('국경 표석', 'runestone', 'plaza'),
     L('천막 좌판 1', 'tent', 'plaza'), L('천막 좌판 2', 'tent', 'plaza')],
    [N('북문 마을 촌장', '촌장 집', memo='talker'), N('국경 표석 지기', '국경 표석', memo='talker'), N('여관 주인', '여관', memo='talker'),
     N('남쪽에서 온 향신료 상인', '천막 좌판 1', memo='talker'), N('천막 상인 아이', '천막 좌판 2', memo='talker')]),
 'north_c_snowwood': village('north_cwood', (12, 7), (8, 5), ['hut', 'turf', 'hut'],
    [L('썰매 창고', 'store', 'plaza'), L('나무꾼 집', 'turf', 'plaza'), P('장작더미 1', 'woodpile', 'plaza'), P('장작더미 2', 'woodpile', '나무꾼 집')],
    [N('썰매꾼', '썰매 창고', memo='talker'), N('숲 마을 장터지기', 'plaza', memo='talker'), N('순록 돌보는 아이', 'houses', memo='talker')]),
 'north_c_creek': village('north_ccreek', (12, 7), (8, 5), ['turf', 'hut', 'turf', 'longhouse'],
    [L('물방앗간', 'mill', 'water'), dict(name='나루터', kind='shore', w=3, h=2, anchor='water', memo='나룻배'),
     P('나룻배', 'boat', '나루터', kind='moor'), P('빨래터', 'laundry', 'water', kind='shore')],
    [N('물방앗간 주인', '물방앗간', memo='talker'), N('얼음 낚시꾼', '나루터', memo='talker'), N('빨래하는 아주머니', '빨래터', memo='talker')]),
 'north_c_crossing': village('north_cross', (14, 8), (8, 5), ['turf', 'hut', 'turf', 'longhouse'],
    [L('여관', 'longhouse', 'plaza'), L('상단 본부', 'shop', 'plaza'), L('고려 좌판', 'tent', 'plaza', memo='수입품 좌판'),
     L('베네치아 좌판', 'tent', 'plaza'), L('아라비아 좌판', 'tent', 'plaza')],
    [N('상단 우두머리', '상단 본부', memo='talker'), N('길목 문지기', 'plaza', memo='talker'), N('여관 주인', '여관', memo='talker'),
     N('고려 상단 행수', '고려 좌판', memo='talker'), N('베네치아 유리 상인', '베네치아 좌판', memo='talker'),
     N('아라비아 향신료 상인', '아라비아 좌판', memo='talker')], lanterns=4),
}

# ── 이동 구간(쉼터·채집·이벤트). 맵, 쉼터 이름, 시설, NPC, 채집, 이벤트 수, 도적, 기준 길(출발 마을, 도착 마을) ──
def REST(name, extra, npcs, gathers, events, bandit=False, road=None, mapkey=None):
    return dict(name=name, extra=extra, npcs=npcs, gathers=gathers, events=events, bandit=bandit, road=road, mapkey=mapkey)
TRAVEL = {
 'north_c_road_nw': REST('고갯마루 쉼터', [L('나무꾼 오두막', 'hut', 'rest')], [N('나무꾼', '나무꾼 오두막', memo='talker'), N('길 지키는 노인', 'rest', memo='talker')],
                         [('땔감', 'wood'), ('땔감', 'wood'), ('약초', 'herb')], 2),
 'north_c_road_ne': REST('다리목 쉼터', [], [N('다리지기', 'bridge', memo='talker'), N('길손 음유시인', 'rest', memo='talker'), N('얼음 낚시꾼', '낚시터', memo='talker')],
                         [('낚시터', 'fish'), ('땔감', 'wood'), ('약초', 'herb')], 3),
 'north_c_runeway': REST('길손 역참', [L('역참', 'longhouse', 'rest'), P('룬돌 둥지', 'runestone', 'rest', memo='옛 사람의 룬돌')],
                         [N('역참지기', '역참', memo='talker'), N('룬 읽는 노인', '룬돌 둥지', memo='talker'), N('길손 행상', 'rest', memo='talker')],
                         [('약초', 'herb'), ('사냥터', 'hunt')], 2, bandit=True),
 'north_c_road_sw': REST('숯쟁이 터', [L('숯가마', 'kiln', 'rest'), L('숯쟁이 오두막', 'hut', 'rest')],
                         [N('숯쟁이', '숯가마', memo='talker'), N('순록 치는 소녀', 'rest', memo='talker'), N('떠돌이 음유시인', 'rest', memo='talker')],
                         [('땔감', 'wood'), ('땔감', 'wood'), ('사냥터', 'hunt')], 2, bandit=True),
 'north_c_road_se': REST('물레방아 쉼터', [L('물레방아', 'mill', 'water')],
                         [N('강지기', 'rest', memo='talker'), N('물레방아 할머니', '물레방아', memo='talker'), N('남쪽 행상', 'rest', memo='talker')],
                         [('낚시터', 'fish'), ('약초', 'herb'), ('땔감', 'wood')], 2),
 # 없어지는 맵의 내용: 국경 고개 → 비르카 맵 목 길가, 피오르 길 → 피오르 나루 맵의 얼음 동굴 고갯길가
 'north_pass→north_birka': REST('국경 초소', [L('초소', 'hut', 'rest')], [N('국경 경비대장', '초소', memo='talker'), N('남쪽에서 온 상인', 'rest', memo='talker')],
                         [], 2, road=('north_birka', 'north_c_gatetown'), mapkey='north_birka'),
 'north_fjord_road→north_naru': REST('피오르 고갯길 쉼터', [], [N('길손', 'rest', memo='talker'), N('얼음 낚시꾼', '낚시터', memo='talker (피오르 길에서 옮김)')],
                         [('낚시터', 'fish')], 3, bandit=True, road=('north_naru', 'north_icecave'), mapkey='north_naru'),
}

WATER = np.isin(T_, [DEEP, SHALLOW, RIVER, LAKE, SPRING])
SEA = np.isin(T_, [DEEP, SHALLOW])
BRIDGE = ROADW & (T_ == RIVER)
ROAD = ROADW & ~WATER
HARD_T = np.isin(T_, [MOUNTAIN, PEAK, ROCKCOAST])   # 못 지나가는 땅
CLIFF = np.isin(T_, [MOUNTAIN, PEAK])

class Planner:
    """고려 m2_zones.Planner를 옮김. vm=자리 잡을 범위, mapm=맵."""
    def __init__(s, vm, mapm):
        s.vm = vm; s.mapm = mapm; s.water = WATER; s.road = ROAD & mapm | ROAD & ndimage.binary_dilation(mapm, iterations=1); s.H, s.W = H, W
        s.bridge = BRIDGE
        s.land = s.mapm & ~s.water & ~s.bridge & ~HARD_T
        s.free0 = s.vm & s.land & ~s.road
        s.fringe = ndimage.binary_dilation(s.vm, iterations=5) & s.land & ~s.road   # 집은 마을 범위 둘레 5칸까지(본토 마을이 좁아서. 고려는 범위 안만)
        s.shore = s.land & ndimage.binary_dilation(s.water, iterations=1) & ~s.road
        s.sea = SEA & s.water
        s.seashore = s.land & ndimage.binary_dilation(s.sea, iterations=1) & ~s.road
        s.cliffside = s.land & ndimage.binary_dilation(CLIFF, iterations=1) & ~s.road
        s.hard = np.zeros_like(s.vm); s.lot = np.zeros_like(s.vm); s.soft = np.zeros_like(s.vm); s.door = np.zeros_like(s.vm)
        s.droad = ndimage.distance_transform_edt(~(s.road & s.mapm))
        ys, xs = np.nonzero(s.vm); s.cen = (ys.mean(), xs.mean())
        s.dwater = ndimage.distance_transform_edt(~s.water)
        s.out = []; s.named = {}
    def rect_ok(s, y0, x0, w, h, need=None, pad=True):
        if y0 < 1 or x0 < 1 or y0 + h >= s.H or x0 + w >= s.W: return False
        sl = (slice(y0, y0 + h), slice(x0, x0 + w)); m = s.free0 if need is None else need
        if not m[sl].all(): return False
        if s.door[sl].any() or s.soft[sl].any(): return False
        if pad:
            if s.hard[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].any(): return False
        elif s.hard[sl].any(): return False
        return True
    def anchor_pt(s, a):
        if a in s.named: r = s.named[a]; return (r['iy'] + r['h'] / 2, r['ix'] + r['w'] / 2)
        for alias in ('장터 광장', '광부 캠프 마당', '룬돌 원'):
            if a == 'plaza' and alias in s.named: return s.anchor_pt(alias)
        if a == 'houses':
            hs = [r for r in s.out if r['name'].startswith('집 ') and not r.get('missing')]
            if hs: return (np.mean([r['iy'] + r['h'] / 2 for r in hs]), np.mean([r['ix'] + r['w'] / 2 for r in hs]))
        if a == 'bridge' and (BRIDGE & s.mapm).any():
            b = np.argwhere(BRIDGE & s.mapm); return tuple(b.mean(0))
        return s.cen
    def plaza(s): return next((s.named[n] for n in ('장터 광장', '광부 캠프 마당', '룬돌 원', 'rest') if n in s.named), None)
    def put(s, it, iy, ix):
        r = dict(name=it['name'], kind=it['kind'], asset=it.get('asset'), iy=int(iy), ix=int(ix), w=it['w'], h=it['h'], id=it.get('id', ''), memo=it.get('memo', ''))
        if it.get('gtype'): r['gather'] = it['gtype']
        if it.get('asset') in ART: r['art'] = ART[it['asset']]
        sl = (slice(iy, iy + it['h']), slice(ix, ix + it['w']))
        if it['kind'] in ('lot', 'plaza', 'yard', 'harbor', 'rest', 'cliff'): s.hard[sl] = True
        if it['kind'] == 'lot': s.lot[sl] = True
        elif it['kind'] not in ('event', 'bandit'): s.soft[sl] = True
        if it['kind'] == 'lot':
            dy, dx = iy + it['h'], ix + it['w'] // 2; s.door[dy, dx] = True; r['door'] = (int(dy), int(dx))
        s.out.append(r); s.named[it['name']] = r
        if it['kind'] == 'rest': s.named['rest'] = r
        return r
    def wet(s, a, b):
        n = int(max(abs(a[0] - b[0]), abs(a[1] - b[1]))) + 1
        for i in range(n + 1):
            y = int(round(a[0] + (b[0] - a[0]) * i / n)); x = int(round(a[1] + (b[1] - a[1]) * i / n))
            if s.water[y, x]: return True
        return False
    def miss(s, it): s.out.append(dict(name=it['name'], kind=it['kind'], missing=True, memo=it.get('memo', ''))); return None
    def place_rect(s, it):
        w, h, k = it['w'], it['h'], it['kind']; a = it.get('anchor', 'center')
        ay, ax = s.anchor_pt(a); pz = s.plaza(); cands = []
        ys, xs = np.nonzero(s.vm); YA, YB, XA, XB = ys.min() - 7, ys.max() + 7, xs.min() - 7, xs.max() + 7
        open_ = s.land & ~s.road
        for y0 in range(max(1, YA), min(H - h - 1, YB - h + 2)):
            for x0 in range(max(1, XA), min(W - w - 1, XB - w + 2)):
                if k == 'moor':
                    sl = (slice(y0, y0 + h), slice(x0, x0 + w))
                    if not (s.water[sl].all() and s.mapm[sl].all()) or s.bridge[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].any(): continue
                    if not (s.land & ~s.road)[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].any(): continue
                    if s.soft[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].any(): continue
                    cands.append((math.hypot(y0 + h / 2 - ay, x0 + w / 2 - ax), y0, x0)); continue
                if k == 'harbor':
                    if s.seashore[y0:y0 + h, x0:x0 + w].sum() < max(w, h) - 1: continue
                    if not s.vm[max(0, y0 - 2):y0 + h + 2, max(0, x0 - 2):x0 + w + 2].any(): continue
                    if not s.rect_ok(y0, x0, w, h, need=open_): continue
                elif k == 'shore':
                    if not s.shore[y0:y0 + h, x0:x0 + w].any(): continue
                    if not s.vm[max(0, y0 - 3):y0 + h + 3, max(0, x0 - 3):x0 + w + 3].any(): continue
                    if not s.rect_ok(y0, x0, w, h, need=open_): continue
                elif k == 'cliff':
                    if s.cliffside[y0:y0 + h, x0:x0 + w].sum() < w: continue
                    if not s.vm[max(0, y0 - 6):y0 + h + 6, max(0, x0 - 6):x0 + w + 6].any(): continue
                    if not s.rect_ok(y0, x0, w, h, need=open_): continue
                elif it.get('anchor') == 'water' and k == 'lot':
                    if not s.rect_ok(y0, x0, w, h): continue
                    if s.dwater[y0:y0 + h, x0:x0 + w].min() > 8: continue
                elif k == 'lot' and it['name'].startswith('집 '):
                    if not s.rect_ok(y0, x0, w, h, need=s.fringe): continue
                elif not s.rect_ok(y0, x0, w, h): continue
                cy, cx = y0 + h / 2, x0 + w / 2
                if a == 'away' and pz:
                    dp = math.hypot(cy - pz['iy'] - pz['h'] / 2, cx - pz['ix'] - pz['w'] / 2); sc = abs(dp - 13) + .3 * math.hypot(cy - s.cen[0], cx - s.cen[1])
                elif a == 'sea': sc = .5 * math.hypot(cy - pz['iy'] - pz['h'] / 2, cx - pz['ix'] - pz['w'] / 2) if pz else 0
                elif a == 'water': sc = s.dwater[y0:y0 + h, x0:x0 + w].min() * 2 + .4 * math.hypot(cy - s.cen[0], cx - s.cen[1])
                else: sc = math.hypot(cy - ay, cx - ax)
                if k in ('plaza', 'rest') and s.droad[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].min() > 1.5: continue
                if k == 'lot':
                    dy, dx = y0 + h, x0 + w // 2
                    if dy >= s.H or not s.land[dy, dx] or s.hard[dy, dx] or s.soft[dy, dx]: continue
                    if dy + 1 < s.H and s.water[dy + 1, dx]: continue
                    dr = s.droad[dy, dx]
                    if dr > 9: continue
                    sc = (.3 * sc + 1.2 * dr) if it['name'].startswith('집 ') else sc + .6 * dr
                if a not in ('center', 'away', 'water') and k != 'moor' and s.wet((int(cy), int(cx)), (int(ay), int(ax))): sc += 30
                if k == 'yard' and s.droad[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].min() > 6: continue
                cands.append((sc, y0, x0))
        cands.sort()
        for sc, y0, x0 in cands[:3000]:
            if k == 'lot' and s.lane_len(y0 + h, x0 + w // 2, (y0, x0, w, h)) > 24: continue
            if k in ('shore', 'harbor', 'cliff') and s.lane_len(y0 + h - 1, x0, (0, 0, 0, 0), start_rect=(y0, x0, w, h)) > 24: continue
            return s.put(it, y0, x0)
        return s.miss(it)
    def lane_len(s, dy, dx, newlot, start_rect=None):
        walk = s.land & ~s.lot
        if newlot[2]: walk = walk.copy(); walk[newlot[0]:newlot[0] + newlot[3], newlot[1]:newlot[1] + newlot[2]] = False
        a = walk[:-1, :-1] & walk[1:, :-1] & walk[:-1, 1:] & walk[1:, 1:]; w2 = np.zeros_like(walk)
        w2[:-1, :-1] |= a; w2[1:, :-1] |= a; w2[:-1, 1:] |= a; w2[1:, 1:] |= a
        q = deque(); seen = set()
        if start_rect:
            y0, x0, w, h = start_rect
            for yy in range(y0, y0 + h):
                for xx in range(x0, x0 + w): q.append(((yy, xx), 0)); seen.add((yy, xx))
        else:
            if not (0 <= dy < s.H) or not w2[dy, dx]: return 999
            q.append(((dy, dx), 0)); seen.add((dy, dx))
        while q:
            c, dd = q.popleft()
            if s.road[c]: return dd
            if dd > 30: return 999
            for oy, ox in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (c[0] + oy, c[1] + ox)
                if 0 <= n[0] < s.H and 0 <= n[1] < s.W and n not in seen and (w2[n] or s.road[n]): seen.add(n); q.append((n, dd + 1))
        return 999
    def place_spot(s, it):
        k = it['kind']; a = it.get('anchor', 'plaza'); w, h = it['w'], it['h']
        ref = s.named.get(a) or (s.plaza() if a == 'plaza' else None)
        near = s.vm | ndimage.binary_dilation(s.vm, iterations=1)
        need = s.land & ~s.road & near
        cands = []
        for y0, x0 in np.argwhere(ndimage.binary_dilation(s.vm, iterations=2)):
            if not s.rect_ok(y0, x0, w, h, need=need, pad=False): continue
            if s.soft[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].any(): continue
            if it.get('asset'):
                fy, fx = y0 + h, x0
                if fy >= s.H or not s.land[fy, fx] or s.lot[fy, fx] or s.door[fy, fx]: continue
            if k == 'npc' and ref and ref.get('door'): dy, dx = ref['door']; sc = math.hypot(y0 - dy, x0 - dx - 1.5)
            elif k == 'npc' and ref: sc = abs(math.hypot(y0 - ref['iy'] - ref['h'] / 2, x0 - ref['ix'] - ref['w'] / 2) - max(ref['w'], ref['h']) / 2 - 1.5)
            elif k == 'lantern' and ref:
                if s.droad[y0 + h - 1, x0] > 1.5: continue
                sc = math.hypot(y0 - ref['iy'] - ref['h'] / 2, x0 - ref['ix'] - ref['w'] / 2)
                ls = [r for r in s.out if r['kind'] == 'lantern']
                if ls: sc += 40 / (1 + min(math.hypot(y0 - r['iy'], x0 - r['ix']) for r in ls))
            elif it['name'].startswith('룬석') and ref:   # 룬돌 원 둘레에 고르게
                R = max(ref['w'], ref['h']) / 2 + 1.5
                dd = math.hypot(y0 - ref['iy'] - ref['h'] / 2 + .5, x0 - ref['ix'] - ref['w'] / 2 + .5); sc = abs(dd - R) * 2
                rs = [r for r in s.out if r['name'].startswith('룬석') and not r.get('missing')]
                if rs: sc += 20 * math.exp(-min(math.hypot(y0 - r['iy'], x0 - r['ix']) for r in rs) / 4)
            elif it['name'] == '게시판' and ref:
                if s.droad[y0, x0] > 1.5: continue
                sc = math.hypot(y0 - ref['iy'] - ref['h'] / 2, x0 - ref['ix'] - ref['w'] / 2)
            else:
                ay, ax = s.anchor_pt(a); sc = math.hypot(y0 - ay, x0 - ax)
                if s.hard[y0 - 1:y0 + h + 1, x0 - 1:x0 + w + 1].any(): sc += 3
            if not s.vm[y0, x0]: sc += 4
            if ref and s.wet((y0, x0), (ref['iy'] + ref['h'] // 2, ref['ix'] + ref['w'] // 2)): sc += 30
            cands.append((sc, y0, x0))
        if not cands: return s.miss(it)
        sc, y0, x0 = min(cands); return s.put(it, y0, x0)
    def place_growth(s, items):
        ok = s.free0 & ~ndimage.binary_dilation(s.hard | s.soft | s.door, iterations=1)
        for it in items:
            pts = np.argwhere(ok)
            if not len(pts): s.miss(it); continue
            ex = np.array([(r['iy'], r['ix']) for r in s.out if not r.get('missing')])
            dd = np.min(np.hypot(pts[:, None, 0] - ex[None, :, 0], pts[:, None, 1] - ex[None, :, 1]), axis=1)
            j = int(np.argmax(np.minimum(dd, 8) - .05 * np.hypot(pts[:, 0] - s.cen[0], pts[:, 1] - s.cen[1])))
            r = s.put(it, *pts[j]); ok[r['iy'] - 1:r['iy'] + 2, r['ix'] - 1:r['ix'] + 2] = False
    # ── 북유럽에서 더한 것: 채집·사냥, 이벤트·도적 ──
    def place_gather(s, label, gtype, zone):
        want = {'wood': np.isin(T_, [FOREST]), 'herb': np.isin(T_, [GRASS, TUNDRA, HILL, BOG, FIELD]), 'hunt': np.isin(T_, [FOREST, HILL, TUNDRA]),
                'fish': s.shore}[gtype]
        dr = s.droad
        ok = want & s.land & ~s.road & ~s.hard & ~s.soft & ~s.door & s.mapm & ndimage.binary_dilation(zone, iterations=14) & (dr >= 2) & (dr <= 7)
        ok &= ~VILLAGE
        others = [r for r in s.out if r['kind'] == 'gather' and not r.get('missing')]
        pts = np.argwhere(ok)
        if not len(pts): return s.miss(dict(name=label, kind='gather', memo=gtype))
        sc = dr[ok] * .3
        if others:
            ex = np.array([(r['iy'], r['ix']) for r in others])
            dmin = np.min(np.hypot(pts[:, None, 0] - ex[None, :, 0], pts[:, None, 1] - ex[None, :, 1]), axis=1)
            sc = sc - np.minimum(dmin, 14) * .8
        rp = s.plaza()
        if rp: sc = sc + .05 * np.hypot(pts[:, 0] - rp['iy'], pts[:, 1] - rp['ix'])
        j = int(np.argmin(sc)); n = sum(1 for r in others if r['name'].startswith(label))
        return s.put(dict(name=label if not n else f'{label} {n + 1}', kind='gather', w=1, h=1, gtype=gtype, memo=gtype), *pts[j])
    def place_on_road(s, kind, label, avoid_rest=8, spacing=14, far=False):
        cells = np.argwhere(s.road & s.mapm & ~VILLAGE & ~s.bridge)
        if not len(cells): return s.miss(dict(name=label, kind=kind))
        used = [r for r in s.out if r['kind'] in ('event', 'bandit') and not r.get('missing')]
        rp = s.named.get('rest')
        sc = np.zeros(len(cells))
        if rp:
            dr = np.hypot(cells[:, 0] - rp['iy'], cells[:, 1] - rp['ix']); sc += np.where(dr < avoid_rest, 100, 0) - (dr * .2 if far else -dr * .05)
        if used:
            ex = np.array([(r['iy'], r['ix']) for r in used])
            du = np.min(np.hypot(cells[:, None, 0] - ex[None, :, 0], cells[:, None, 1] - ex[None, :, 1]), axis=1)
            sc += np.where(du < spacing, 100, 0) - .1 * np.minimum(du, 30)
        j = int(np.argmin(sc)); return s.put(dict(name=label, kind=kind, w=1, h=1), *cells[j])
    def lanes(s):
        lane = np.zeros_like(s.vm); walk = s.land & ~s.lot; flat = s.hard & ~s.lot
        for r in s.out:
            if r.get('kind') != 'lot' or r.get('missing'): continue
            dy, dx = r['door']
            if s.road[dy, dx]: r['laneLen'] = 0; continue
            prev = {(dy, dx): None}; q = deque([(dy, dx)]); hit = None
            while q:
                c = q.popleft()
                if s.road[c]: hit = c; break
                for oy, ox in ((1, 0), (0, 1), (0, -1), (-1, 0)):
                    n = (c[0] + oy, c[1] + ox)
                    if n in prev or not (0 <= n[0] < s.H and 0 <= n[1] < s.W) or not walk[n]: continue
                    if s.soft[n] and not s.road[n]: continue
                    prev[n] = c; q.append(n)
            if hit is None: r['blocked'] = True; continue
            c = prev[hit]; r['laneLen'] = 0
            while c is not None: lane[c] = True; r['laneLen'] += 1; c = prev[c]
        return lane & ~s.road & ~flat

def place_village(p, items):
    rects = [i for i in items if i['kind'] in ('plaza', 'lot', 'yard', 'shore', 'moor', 'harbor', 'cliff', 'rest')]
    pri = {'plaza': 0, 'rest': 0, 'harbor': 1, 'moor': 2, 'cliff': 3}
    first = sorted([i for i in rects if not i['name'].startswith('집 ')], key=lambda i: -1 if i.get('first') else (pri.get(i['kind'], 5) if i['kind'] != 'lot' or i.get('anchor') != '항구 부두' else 2))
    houses = [i for i in rects if i['name'].startswith('집 ')]
    for it in first + houses:
        if it['kind'] in ('harbor', 'plaza', 'yard', 'rest', 'cliff'):
            w, h = it['w'], it['h']
            tries = ((w, h), (h, w), (w, h - 2), (h - 2, w), (w + 1, h - 3), (h - 3, w + 1)) if it['kind'] == 'harbor' else \
                    ((w, h), (h, w), (w - 2, h - 1), (w - 2, h), (h, w - 2), (w - 4, h - 1), (w - 4, h - 2))
            for ww, hh in tries:
                if ww < 2 or hh < 1: continue
                if p.place_rect(dict(it, w=ww, h=hh)) is not None: break
                p.out.pop()
            else: p.miss(it)
        else: p.place_rect(it)
    for it in [i for i in items if i['kind'] == 'spot' and i.get('asset') != 'well']: p.place_spot(it)
    for it in [i for i in items if i['kind'] == 'spot' and i.get('asset') == 'well']: p.place_spot(it)
    for it in [i for i in items if i['kind'] == 'lantern']: p.place_spot(it)
    for it in [i for i in items if i['kind'] == 'npc']: p.place_spot(it)
    p.place_growth([i for i in items if i['kind'] == 'growth'])
    # 나중에 놓인 것 때문에 골목이 막힌 건물은 빼고 다시 잡는다(한 번)
    p.lanes()
    for r in [r for r in p.out if r.get('blocked')]:
        p.out.remove(r); sl = (slice(r['iy'], r['iy'] + r['h']), slice(r['ix'], r['ix'] + r['w']))
        p.hard[sl] = False; p.lot[sl] = False; p.door[r['door']] = False
        it = next(i for i in items if i['name'] == r['name']); p.place_rect(it)
        for q in p.out: q.pop('blocked', None)

def road_zone(mapm, pair=None):
    """이동 구간 자리 범위: 그 맵 길(마을 밖)의 가운데 40% 둘레 10칸"""
    cells = np.argwhere(ROAD & mapm & ~VILLAGE)
    if pair:   # 특정 마을 쌍 사이 길만: 두 마을 범위에서 둘 다 18칸 넘게 떨어지지 않은 길
        a, b = (VLABEL == TOWNS.index(pair[0])), (VLABEL == TOWNS.index(pair[1]))
        da, db = ndimage.distance_transform_edt(~a), ndimage.distance_transform_edt(~b)
        keep = [(y, x) for y, x in cells if da[y, x] + db[y, x] < np.hypot(*np.subtract(np.argwhere(a).mean(0), np.argwhere(b).mean(0))) * 1.25]
        cells = np.array(keep) if keep else cells
    c = cells.mean(0); d = np.hypot(cells[:, 0] - c[0], cells[:, 1] - c[1])
    mid = cells[d <= np.quantile(d, .4)]
    m = np.zeros((H, W), bool); m[mid[:, 0], mid[:, 1]] = True
    return ndimage.binary_dilation(m, iterations=10) & mapm & ~VILLAGE

def draw(p, out, lane, key, title, S=12, extra_mask=None, show_vm=True):
    rgb = np.zeros((H, W, 3), np.uint8)
    for k, c in COLORS.items(): rgb[T_ == k] = c
    rgb[ROADW & ~WATER] = (176, 140, 92); rgb[BRIDGE] = (150, 110, 70)
    rgb[lane] = (150, 116, 76)
    ys, xs = np.nonzero(p.mapm); m = 2; x0, x1, y0, y1 = xs.min() - m, xs.max() + m + 1, ys.min() - m, ys.max() + m + 1
    c = Image.fromarray(rgb[y0:y1, x0:x1]).resize(((x1 - x0) * S, (y1 - y0) * S), Image.NEAREST).convert('RGBA')
    ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); dr = ImageDraw.Draw(ov)
    def box(yy, xx, hh=1, ww=1): return [(xx - x0) * S, (yy - y0) * S, (xx - x0 + ww) * S - 1, (yy - y0 + hh) * S - 1]
    outside = ~p.mapm[y0:y1, x0:x1]
    for yy, xx in np.argwhere(outside): dr.rectangle(box(yy + y0, xx + x0), fill=(0, 0, 0, 140))
    edge = p.vm & ~ndimage.binary_erosion(p.vm) if show_vm else np.zeros_like(p.vm)
    for yy, xx in np.argwhere(edge): dr.rectangle(box(yy, xx), fill=(255, 230, 120, 90))
    for r in out:
        if r.get('missing'): continue
        col = KIND_COL[r['kind']]
        if r['kind'] in ('event', 'bandit', 'gather'):
            bx = box(r['iy'], r['ix']); g = 4; dr.ellipse([bx[0] - g, bx[1] - g, bx[2] + g, bx[3] + g], fill=col + (230,), outline=(255, 255, 255, 255), width=2)
        else:
            dr.rectangle(box(r['iy'], r['ix'], r['h'], r['w']), fill=col + (100,), outline=col + (255,), width=3)
        if r.get('door'): dr.rectangle(box(*r['door']), outline=(255, 255, 255, 220), width=2)
    c.alpha_composite(ov); d2 = ImageDraw.Draw(c)
    f = ImageFont.truetype(FONT, 15); fs = ImageFont.truetype(FONT, 11)
    for r in out:
        if r.get('missing'): continue
        small = r['kind'] in ('npc', 'spot', 'growth', 'lantern', 'gather', 'event', 'bandit')
        lab = r['name'].replace('성장 자리 ', '성') if small else r['name']
        if r['kind'] == 'event': lab = '이벤트'
        if r['kind'] == 'bandit': lab = '도적'
        d2.text(((r['ix'] - x0) * S + 2, (r['iy'] - y0) * S - 12 if small else (r['iy'] - y0) * S + 3), lab, fill='white',
                font=fs if small else f, stroke_width=3, stroke_fill='black')
    hd = Image.new('RGBA', (c.width, 30), (20, 20, 24, 255)); ImageDraw.Draw(hd).text((8, 5), title, fill='white', font=ImageFont.truetype(FONT, 16))
    full = Image.new('RGBA', (c.width, c.height + 30)); full.paste(hd, (0, 0)); full.paste(c, (0, 30))
    full.save(OUT / f'{key}.png'); return full

def gates():
    """길이 맵 경계를 넘는 자리(출입구 후보). 고려 m2 connectivity와 같은 방식"""
    net = ROADW; out = []; pts = {}
    for oy, ox in ((0, 1), (1, 0)):
        a = net[:H - oy, :W - ox] & net[oy:, ox:]; ma = MAPID[:H - oy, :W - ox]; mb = MAPID[oy:, ox:]
        for yy, xx in np.argwhere(a & (ma != mb) & (ma >= 0) & (mb >= 0)):
            k = tuple(sorted((KEYS[ma[yy, xx]], KEYS[mb[yy, xx]]))); pts.setdefault(k, []).append((yy, xx))
    for k, pl in pts.items():
        m_ = np.zeros((H, W), bool)
        for yy, xx in pl: m_[yy, xx] = True
        l2, n2 = ndimage.label(ndimage.binary_dilation(m_, iterations=2))
        for j in range(1, n2 + 1):
            yy, xx = np.nonzero(l2 == j); out.append(dict(maps=list(k), x=int(round(xx.mean())) + X0, y=int(round(yy.mean())) + Y0))
    lab, n = ndimage.label(net, structure=np.ones((3, 3)))
    comps = sorted({int(lab[tuple(p)]) for i in range(len(TOWNS)) for p in np.argwhere(ndimage.binary_dilation(VLABEL == i, iterations=2) & net)})
    return out, comps

def clean(out):
    res = []
    for r in out:
        r2 = {k: v for k, v in r.items() if k not in ('iy', 'ix')}
        if not r.get('missing'): r2['x'] = r['ix'] + X0; r2['y'] = r['iy'] + Y0
        if 'door' in r2: r2['door'] = [r['door'][1] + X0, r['door'][0] + Y0]
        res.append(r2)
    return res

allz = {}; rep = {}; pics = {}
for key in KEYS:
    mapm = MAPID == KEYS.index(key)
    if key in VILLAGES:
        vm = VLABEL == TOWNS.index(key)
        p = Planner(vm, mapm); place_village(p, VILLAGES[key])
        kind = 'town'
    else:
        tr = TRAVEL[key]; zone = road_zone(mapm)
        p = Planner(zone, mapm)
        place_village(p, [dict(name=tr['name'], kind='rest', w=7, h=5, anchor='center', memo='쉼터 마당')] + tr['extra'])
        p.place_spot(P('모닥불', 'fire', 'rest'))
        for lab, gt in tr['gathers']: p.place_gather(lab, gt, zone)
        for it in tr['npcs']:
            if it['anchor'] == '낚시터' and '낚시터' not in p.named: it = dict(it, anchor='rest')
            p.place_spot(it)
        for i in range(tr['events']): p.place_on_road('event', f'이벤트 {i + 1}')
        if tr['bandit']: p.place_on_road('bandit', '도적', far=True)
        kind = 'travel'
    # 없어지는 맵 내용을 이 맵 길가에
    merged = []
    for tk, tr in TRAVEL.items():
        if tr.get('mapkey') != key: continue
        zone = road_zone(mapm, tr['road']); zone &= ~ndimage.binary_dilation(p.hard | p.soft, iterations=2)
        q = Planner(zone, mapm); q.hard |= p.hard; q.lot |= p.lot; q.soft |= p.soft; q.door |= p.door
        place_village(q, [dict(name=tr['name'], kind='rest', w=6, h=4, anchor='center', memo=f'{tk.split("→")[0]}에서 옮김')] + tr['extra'])
        q.place_spot(P('모닥불', 'fire', 'rest'))
        for lab, gt in tr['gathers']: q.place_gather(lab, gt, zone)
        for it in tr['npcs']:
            if it['anchor'] == '낚시터' and '낚시터' not in q.named: it = dict(it, anchor='rest')
            q.place_spot(it)
        for i in range(tr['events']): q.place_on_road('event', f'옮긴 이벤트 {i + 1}')
        if tr['bandit']: q.place_on_road('bandit', '도적', far=True)
        for r in q.out: r['from'] = tk.split('→')[0]
        p.out += q.out; p.lot |= q.lot; p.hard |= q.hard; p.soft |= q.soft; p.door |= q.door; merged.append(tk.split('→')[0])
    lane = p.lanes()
    miss = [r['name'] for r in p.out if r.get('missing')]; blk = [r['name'] for r in p.out if r.get('blocked')]
    title = f"{NAME[key]} ({key}) · 맵 {int(mapm.sum()):,}칸" + (f" · 마을 {int(p.vm.sum()):,}칸" if kind == 'town' else ' · 이동 구간') + (f" · 옮겨 온 맵: {', '.join(merged)}" if merged else '')
    pics[key] = draw(p, p.out, lane, key, title, show_vm=(kind == 'town'))
    items = clean(p.out)
    (OUT / f'{key}.json').write_text(json.dumps(dict(map=key, name=NAME[key], kind=kind, items=items, laneCells=int(lane.sum())), ensure_ascii=False, indent=1))
    allz[key] = dict(name=NAME[key], kind=kind, items=items)
    rep[key] = dict(missing=miss, laneBlocked=blk, n=len(p.out), laneCells=int(lane.sum()), merged=merged)
    print(key, len(p.out), '빠짐', miss, '골목막힘', blk)

gl, comps = gates()
# 지금 맵에 있던 ID·이름이 모두 자리를 받았는지
OLD = json.loads(Path(sys.argv[4]).read_text()) if len(sys.argv) > 4 else {}
lost = []
placed_names = {(r.get('id') or ''): k for k, z in allz.items() for r in z['items']}
placed_label = {r['name'] for z in allz.values() for r in z['items'] if not r.get('missing')}
for mk, md in OLD.items():
    for o in md['objects']:
        if o['kind'] == 'market' and f"market:{o['town']}" not in placed_names: lost.append((mk, 'market', o['town']))
        if o['kind'] == 'board' and f"board:{o['town']}" not in placed_names: lost.append((mk, 'board', o['town']))
        if o['kind'] == 'npc' and o['npc'] not in placed_names: lost.append((mk, 'npc', o['npc']))
        if o['kind'] == 'talker' and o.get('name') and o['name'] not in placed_label: lost.append((mk, 'talker', o['name']))
check = dict(maps=rep, gates=gl, gateCount=len(gl), roadNetworks=len(comps), oldContentLost=lost)
(OUT / 'zones.json').write_text(json.dumps(allz, ensure_ascii=False, indent=1))
(OUT / 'check.json').write_text(json.dumps(check, ensure_ascii=False, indent=1))
print('출입구', len(gl), '길망', len(comps), '잃은 내용', lost)
