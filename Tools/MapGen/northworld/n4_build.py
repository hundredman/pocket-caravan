"""북유럽 N4: N1 지형 + N2 자리 + N3 그림으로 실제 맵 JSON을 만든다. 2026-10-29 비르카 한 장 먼저(사용자 결정).
python3 Tools/MapGen/northworld/n4_build.py north_birka [더 많은 맵 id ...]

- 바닥: 북유럽 SnowMap(눈밭·눈길·얼음 물가). 밭·자갈·늪은 흙/풀 조각, 숲은 빽빽한 눈 나무, 산·빙하·바위 해안은 눈 절벽
- 건물·소품: N2 자리, N3 그림(STAMPS, new:* 는 north_build.png 시트로 등록)
- 사람: 지금 맵의 npc ID, talker 이름·그림·대사를 그대로 옮김(ArtReview/North-N4/old_objects.json에 처음 한 번 저장해 둠)
- 출입구: 고려 m4_objects.seams와 같은 방식(걸을 수 있는 경계 전부). 이웃 맵도 새로 만들어야 서로 맞음
- 다른 지역 출구(①⑤⑨⑩⑪): N1 출구 자리. region_links.json·connect_regions.py는 16장 다 만든 뒤 맞춤
- 아직 안 함(N5): 동물·낚시·룬돌 고리 같은 생활 자리, 성장 자리, 채집 자리, 이정표 글
출력: Assets/PocketCaravan/Maps/North|Center/<id>.json, Art/NinjaAdventure/Tiles/*.png(새로 쓴 타일), ArtReview/North-N4/<id>.png"""
import json, os, sys, random
from collections import deque
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); G = os.path.dirname(HERE); R = os.path.abspath(os.path.join(G, '..', '..'))
sys.path.insert(0, G); os.chdir(G)
import north_tiles as nt, tiles
sys.path.insert(0, HERE); import n5_transfer as n5
from tiles import K
OUT4 = R + '/ArtReview/North-N4'; os.makedirs(OUT4, exist_ok=True)
TILE_DIR = R + '/Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
_ti = tiles.tile_image
_tcache = {}
def _tile_from_dir(key):
    if key in _tcache: return _tcache[key]
    _tcache[key] = _tile_from_dir0(key); return _tcache[key]
def _tile_from_dir0(key):
    """이미 내보낸 타일 PNG를 먼저 읽는다(에셋 팩이 없는 컴퓨터에서도 돌게). 없으면 원래 방식"""
    p = f"{TILE_DIR}/{key.replace(':', '_').replace('@', '_')}.png"
    if os.path.exists(p): return Image.open(p).convert('RGBA')
    return _ti(key)
tiles.tile_image = _tile_from_dir

# ── N3 새 그림을 타일 시트로 등록 (north_art.png처럼 절대 경로) ──
NEW = R + '/ArtReview/North-N3/art'
SHEET = G + '/north_build.png'
NEWART = [('jarlhall', 'jarlhall_A', 8, 6), ('smithy', 'smithy_A', 4, 4), ('kiln', 'kiln_A', 3, 3), ('cave', 'cave_A', 5, 3)]
def make_sheet():
    ims = [(k, Image.open(f'{NEW}/{f}.png').convert('RGBA'), w, h) for k, f, w, h in NEWART]
    W = sum(w for *_, w, h in ims) * 16; H = max(h for *_, h in ims) * 16
    sh = Image.new('RGBA', (W, H)); x = 0; pos = {}
    for k, im, w, h in ims:
        sh.alpha_composite(im, (x * 16, (h * 16 - im.height))); pos[k] = (x, w, h); x += w
    sh.save(SHEET); return pos
POS = make_sheet()
tiles.SHEETS['northbuild'] = SHEET
for k, (x, w, h) in POS.items():
    # (시트, x, y, w, h, 막는 줄 시작, 위층 줄 수, 색): 윗줄은 위층(사람이 뒤로 지나감), 그 아래는 막힘
    tiles.STAMPS['new_' + k] = ('northbuild', x, 0, w, h, 1, 1, None)
tiles.STAMPS['new_cave'] = ('northbuild', POS['cave'][0], 0, 5, 3, 0, 0, None)

g = np.load(R + '/ArtReview/North-N1/n1_grid.npz'); X0, Y0 = int(g['X0']), int(g['Y0'])
T_, MID, ROADW, VIL = g['t'], g['mapid'], g['road'], g['village']
VOID, DEEP, SHALLOW, GRAVEL, ROCKCOAST, RIVER, BOG, FIELD, GRASS, TUNDRA, HILL, FOREST, SNOW, MOUNTAIN, PEAK, LAKE, SPRING = range(17)
KEYS = ['north_birka', 'north_herd', 'north_hunter', 'north_naru', 'north_laugar', 'north_icecave', 'north_runehill',
        'north_c_gatetown', 'north_c_snowwood', 'north_c_creek', 'north_c_crossing',
        'north_c_road_nw', 'north_c_road_ne', 'north_c_runeway', 'north_c_road_sw', 'north_c_road_se']
Z = json.load(open(R + '/ArtReview/North-N2/zones.json'))
N1R = json.load(open(R + '/ArtReview/North-N1/report.json'))
LINKS = json.load(open(R + '/Assets/PocketCaravan/World/Connections/region_links.json'))['links']

# ── 지금 맵 내용(처음 한 번 저장) ──
OLD_P = OUT4 + '/old_objects.json'
if not os.path.exists(OLD_P):
    old = {}
    for d_ in ('North', 'Center'):
        for f in os.listdir(R + f'/Assets/PocketCaravan/Maps/{d_}'):
            if f.endswith('.json') and f.startswith('north'):
                j = json.load(open(R + f'/Assets/PocketCaravan/Maps/{d_}/{f}')); old[j['id']] = dict(name=j['name'], objects=j['objects'])
    json.dump(old, open(OLD_P, 'w'), ensure_ascii=False)
OLD = json.load(open(OLD_P))
# 없어지는 맵 내용이 옮겨 간 곳
MERGED_FROM = {'north_birka': ['north_pass'], 'north_naru': ['north_fjord_road']}

def old_talker(key, name):
    for mk in [key] + MERGED_FROM.get(key, []) + list(OLD):
        for o in OLD.get(mk, {}).get('objects', []):
            if o['kind'] == 'talker' and o.get('name') == name: return o
    return None

def seams(mapid, walk, mids):
    """고려 Tools/WorldLayout/m4_objects.seams 그대로(은샘). 반환 {mapId: [객체...]}, 좌표는 배열 칸"""
    DIRS = {'N': (-1, 0), 'S': (1, 0), 'W': (0, -1), 'E': (0, 1)}; OPP = {'N': 'S', 'S': 'N', 'W': 'E', 'E': 'W'}
    H, W = mapid.shape; out = {m: [] for m in mids if m}; count = {}
    def ok(y, x): return 0 <= y < H and 0 <= x < W
    runs_all = []
    for dname, (dy, dx) in DIRS.items():
        pairs = {}
        for y in range(H):
            for x in range(W):
                a = mapid[y, x]
                if a < 0 or not walk[y, x]: continue
                ny, nx = y + dy, x + dx
                if not ok(ny, nx): continue
                b = mapid[ny, nx]
                if b < 0 or b == a or a > b or not walk[ny, nx]: continue
                pairs.setdefault((a, b), []).append((y, x))
        for (a, b), cells in pairs.items():
            cells.sort(key=(lambda c: (c[0], c[1])) if dname in 'NS' else (lambda c: (c[1], c[0])))
            runs = []; cur = [cells[0]]
            for c in cells[1:]:
                p = cur[-1]
                if (dname in 'NS' and c[0] == p[0] and c[1] == p[1] + 1) or (dname in 'WE' and c[1] == p[1] and c[0] == p[0] + 1): cur.append(c)
                else: runs.append(cur); cur = [c]
            runs.append(cur)
            for run in runs: runs_all.append((dname, a, b, run))
    exitcells = set()
    for dname, a, b, run in runs_all:
        dy, dx = DIRS[dname]
        for y, x in run: exitcells.add((y, x)); exitcells.add((y + dy, x + dx))
    def arrival(cells, m, dy, dx, start):
        for k in range(start, start + 5):
            pts = [(y + dy * k, x + dx * k) for y, x in cells]
            if all(ok(*p_) and walk[p_] and mapid[p_] == m and p_ not in exitcells for p_ in pts): return pts
        return None
    def nearest(cell, m):
        q = deque([(cell, 0)]); seen = {cell}
        while q:
            p_, d = q.popleft()
            if d and p_ not in exitcells: return [p_]
            if d >= 6: continue
            for oy, ox in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (p_[0] + oy, p_[1] + ox)
                if n not in seen and ok(*n) and walk[n] and mapid[n] == m: seen.add(n); q.append((n, d + 1))
        return None
    pieces = []
    for dname, a, b, run in runs_all:
        dy, dx = DIRS[dname]
        inB = arrival(run, b, dy, dx, 2); inA = arrival(run, a, -dy, -dx, 1)
        if inB is not None and inA is not None: pieces.append((dname, a, b, run, inB, inA)); continue
        for c in run:
            iB = arrival([c], b, dy, dx, 2) or nearest((c[0] + dy, c[1] + dx), b); iA = arrival([c], a, -dy, -dx, 1) or nearest(c, a)
            if iB is not None and iA is not None: pieces.append((dname, a, b, [c], iB, iA))
    for dname, a, b, run, inB, inA in pieces:
        dy, dx = DIRS[dname]; A, B = mids[a], mids[b]
        ka = count.get((A, B), 0); count[(A, B)] = ka + 1; kb = count.get((B, A), 0); count[(B, A)] = kb + 1
        n = len(run); w_, h_ = (n, 1) if dname in 'NS' else (1, n); (y0, x0) = run[0]
        out[A].append(dict(kind='exit', cell=(y0, x0), w=w_, h=h_, target=B, spawn=f'seam_{A}_{ka}', edge=dname))
        out[B].append(dict(kind='spawn', cell=inB[0], w=w_, h=h_, id=f'seam_{A}_{ka}', edge=dname))
        out[B].append(dict(kind='exit', cell=(y0 + dy, x0 + dx), w=w_, h=h_, target=A, spawn=f'seam_{B}_{kb}', edge=OPP[dname]))
        out[A].append(dict(kind='spawn', cell=inA[0], w=w_, h=h_, id=f'seam_{B}_{kb}', edge=OPP[dname]))
    return out

WATER_T = np.isin(T_, [DEEP, SHALLOW, RIVER, LAKE, SPRING])
WALK = ~np.isin(T_, [DEEP, SHALLOW, RIVER, LAKE, SPRING, MOUNTAIN, PEAK, FOREST]) | (ROADW)
SEAMS = seams(MID, WALK & (MID >= 0), KEYS)

# ── 2026-10-29(사용자): 어디서 맵이 바뀌는지 안 보임(길 없는 눈밭에서도 넘어감) ──
#   경계 출입구는 길이 지나는 자리만 남기고, 그 길은 경계 앞뒤로 곧게 잇는다. 나머지 경계는 build()에서 눈 나무 울타리 + 길가 이정표.
EXIT_STUB = 4      # 경계 앞뒤로 곧게 뻗는 길 길이(칸)
EXIT_GAP = 1       # 길 양옆 여유 칸(울타리 틈 = 길 폭 + 2*EXIT_GAP)
FALLBACK_MAXLEN = 30  # 이보다 긴 경계는 '골짜기 통로'가 아니라 트인 눈밭: 길 건널목이 하나라도 있으면 새 길 안 냄
FALLBACK_DIST = 15 # 통로 가운데에서 이만큼 안에 길 건널목이 없으면 통로 가운데에 새 길
REGION_NAMES = {'venice_c_vineyard': '포도밭 언덕', 'venice_c_pass': '산기슭 고갯길', 'venice_c_gate': '동문 큰길', 'arabia_central_steppe': '한가운데 초원', 'transit_crossroads': '고려 내륙 교역 길목'}
REGION_LOOK = {'venice': ('grass', ['crate_berry', 'barrel', 'crate_berry', 'barrel_s', 'stump']),   # 포도 상자·술통(낱개만)
               'arabia': ('dirt', ['rock', 'rocksmall', 'crate_goods', 'deadtree', 'rocksmall']),
               'transit': ('dirt', ['crate_goods', 'barrel', 'crate', 'barrel_s', 'rocksmall'])}
FOREST_KEEP = 0.58   # 2026-10-29(사용자: 숲이 너무 많음): 숲 지형 중 나무를 심는 비율(덩어리 단위). 나머지는 숲 사이 눈밭
MTN_KEEP = 0.70
REST_TALKERS = [   # 2026-10-29: 빈 넓은 눈밭에 세우는 말 거는 사람(초안 대사 — 기우 검토 필요)
    ('나그네 할라', 'north_p_trader_tall', ['갈림길마다 이정표가 있으니 헤맬 일은 없을 거요.', '눈이 그치면 순록 떼가 이 근처로 내려온다오.']),
    ('나무꾼 에이나르', 'north_p_smith_tall', ['이 언저리 나무는 결이 고와서 썰매 만들기에 딱이지.', '숲을 너무 베면 겨울바람이 마을까지 들이친다네.']),
    ('약초 캐는 시그네', 'north_p_girl_tall', ['눈 밑에도 풀은 자라. 벌써 봄 냄새가 나는걸.', '모닥불 가에서 쉬어 가. 손이 꽁꽁 얼었네.']),
    ('별 보는 노인 울프', 'north_p_oldman_tall', ['밤에 여기서 하늘을 보면 오로라가 길게 걸리지.', '젊을 땐 나도 저 길 끝까지 짐을 지고 다녔다네.']),
    ('길 위의 행상 토베', 'north_bear_sailor_tall', ['장터가 어느 쪽이더라… 이정표를 볼 걸 그랬군.', '짐이 무거우면 길 위로만 다니게. 눈밭은 발이 빠져.']),   # 10/30 군나르는 얼음 동굴 짐 찾기 주인공으로 옮김
    ('사냥꾼 리브', 'north_lynx_tall', ['발자국을 보니 여우가 지나갔군. 오늘은 운이 좋겠어.', '해 지기 전엔 마을로 돌아가는 게 좋아.']),
]
# 2026-10-31(기우: 같은 사람·같은 대사 겹침 정리) 맵별 쉼터 사람 고정. None이면 사람 없이 모닥불만
REST_PICK = {'north_c_gatetown': None,   # 북문 마을은 이미 말 거는 사람이 많음 (사냥꾼 리브가 세 맵에 겹쳤음)
             'north_c_runeway': ('길 위의 행상 토베', 'north_bear_sailor_tall', ['장터가 어느 쪽이더라… 이정표를 볼 걸 그랬군.', '짐이 무거우면 길 위로만 다니게. 눈밭은 발이 빠져.'])}
DROP_TALKERS = {('north_birka', '국경 경비대장'), ('north_birka', '남쪽에서 온 상인')}   # 북문 마을 국경 표석 지기·향신료 상인과 역할이 겹침
TALKER_LINES = {   # 옛 대사가 다른 맵과 겹쳐서 바꾼 것 (첫 줄만 바꾸고 나머지는 옛 대사)
    ('north_herd', '여관 주인'): ['고원 바람이 매섭지?\n꿀술 한 잔에 화롯가 자리는 공짜야.'],
    ('north_c_road_sw', '떠돌이 음유시인'): ['솔숲 바람 소리에 맞춰 한 곡 뽑아 볼까?\n산적만 안 나오면 좋은 길이지.'],
    ('north_c_road_se', '남쪽 행상'): ['물가 길은 느려도 안전하지.\n남쪽 길목에선 북쪽 소금을 찾는 사람이 많아.']}
EXTRA_REST = {   # 2026-10-31(기우: 썰렁한 마을 보강) 모닥불(왼쪽 위 칸) + 말 거는 사람. 맵 칸 좌표(지금 맵 기준)
    'north_hunter': dict(fires=[(47, 40)], talkers=[
        (51, 41, '덫 놓는 사냥꾼 울바', 'north_p_herder_tall', ['올무는 눈 위 발자국 따라 놓는 거야.\n여우는 늘 같은 길로 다니거든.', '불가에 좀 앉아 있다 가.\n여기 바람은 뼛속까지 들어와.']),
        (19, 27, '가죽 무두장이 토라', 'north_p_smith_tall', ['가죽은 소금에 절여 말려야 안 썩어.\n그래서 이 마을은 늘 소금이 모자라.', '잘 무두질한 모피는 남쪽으로 갈수록 비싸게 팔려.'])]),
    'north_c_snowwood': dict(fires=[(38, 35)], talkers=[
        (42, 36, '숲지기 노인 브란', 'north_p_oldman_tall', ['이 숲 나무는 마을 모두의 거야.\n한 그루 베면 한 그루 심는 게 규칙이지.', '장터는 동쪽 끝이야.\n썰매 언덕도 거기 있지.']),
        (14, 51, '포도밭에서 온 행상 마르코', 'north_p_trader_tall', ['서쪽 포도밭 언덕을 넘어왔소.\n포도주 한 통이면 모피 두 장이랑 바꿔 준다더군.', '눈숲 장터는 동쪽 끝이오.\n길 따라 쭉 가면 되오.'])])}
# 2026-10-31(기우: 다른 지역 가는 풀밭·흙바닥은 길 끝에 있어야) N1 출구 자리 기준이라 길 중간에 깔리던 것을 맵 끝 출구 앞으로 옮김
REGION_PATCH_FIX = {'north_c_snowwood': ('grassedge', (0, 42, 12, 56), True),   # (바닥, 새로 깔 칸 범위 x0,y0,x1,y1, 범위 밖 옛 조각 지우기)
                    'north_c_road_se': ('dirtedge', (20, 69, 35, 75), False)}  # 흙바닥은 아래 출구 끝까지 이어 붙임
REGION_ROAD = {'north_c_road_se': (24, 63, 35, 75)}   # 2026-10-31(기우) 고려 쪽 갈림길 아래 길은 흙길로 (고려 땅 길과 비슷하게)
NO_GRASS = {'north_c_road_se'}
# 2026-10-31(기우) 베네치아 가는 길 끝도 그 지역 길처럼: 포도밭 언덕·산기슭 고갯길은 흙길, 동문 큰길은 베네치아 돌판 길
EXIT_ROADS = {'north_c_snowwood': [((0, 46, 10, 51), 'dirt')],
              'north_c_road_sw': [((0, 13, 10, 20), 'dirt'), ((36, 54, 45, 65), 'slab')]}

def apply_exit_roads(m, key):
    if key not in EXIT_ROADS: return
    rng_ = random.Random(31)
    dirt = {(x, y) for y in range(m.h) for x in range(m.w) if isinstance(m.ground[y][x], str) and m.ground[y][x].endswith('@dirtedge')}
    for (x0, y0, x1, y1), kind in EXIT_ROADS[key]:
        cells = {(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1) if m.inside(x, y) and m.road[y][x] > 0}
        if kind == 'dirt': dirt |= cells
        else:
            for (x, y) in cells: m.ground[y][x] = f'vq_L_g_cap_slab{rng_.randint(0, 1)}_0_{x % 2}_{y % 2}_0_--------'
    inside = lambda a, b: (a, b) in dirt or (not m.inside(a, b) and (min(max(a, 0), m.w - 1), min(max(b, 0), m.h - 1)) in dirt)
    for (x, y) in dirt: m.ground[y][x] = tiles.autotile(inside, x, y, 'floor', 0, 7) + '@dirtedge'
   # 2026-10-31(기우) 동남 물가길은 풀밭 없이 눈으로만
REGION_PROPS = {'camp:3:5', 'camp:4:5', 'house:19:14'}   # 풀밭 위 포도 상자·술통
CRITTER_FIX = {'north_hunter': {'goat': dict(kind='reindeer', x0=37, y0=40, x1=44, y1=44)}}   # 2026-10-31(기우) 양 같던 염소 → 마을 가운데 순록

def fix_region_patch(m, key):
    if key in NO_GRASS:
        for y in range(m.h):
            for x in range(m.w):
                if isinstance(m.ground[y][x], str) and m.ground[y][x].endswith('@grassedge'): m.ground[y][x] = nt.SNOW
    if key not in REGION_PATCH_FIX: return
    mode, (bx0, by0, bx1, by1), wipe = REGION_PATCH_FIX[key]
    inb = lambda x, y: bx0 <= x <= bx1 and by0 <= y <= by1
    cur = {(x, y) for y in range(m.h) for x in range(m.w) if isinstance(m.ground[y][x], str) and m.ground[y][x].endswith('@' + mode)}
    if wipe:
        for (x, y) in [c for c in cur if not inb(*c)]:
            m.ground[y][x] = nt.SNOW
            for yy in (y, y - 1):
                if m.inside(x, yy) and m.obj[yy][x] in REGION_PROPS:
                    m.obj[yy][x] = None
                    if m.high[yy][x] is None: m.blocked[yy][x] = False
        cur = {c for c in cur if inb(*c)}
    new = {(x, y) for y in range(by0, by1 + 1) for x in range(bx0, bx1 + 1) if m.inside(x, y) and not m.blocked[y][x] and m.road[y][x] == 0
           and m.obj[y][x] is None and m.high[y][x] is None and not m.water[y][x]}
    if key in REGION_ROAD:
        rx0, ry0, rx1, ry1 = REGION_ROAD[key]
        new |= {(x, y) for y in range(ry0, ry1 + 1) for x in range(rx0, rx1 + 1) if m.inside(x, y) and m.road[y][x] > 0}
    mask = cur | new
    inside = lambda a, b: (a, b) in mask or (not m.inside(a, b) and (min(max(a, 0), m.w - 1), min(max(b, 0), m.h - 1)) in mask)
    for (x, y) in mask: m.ground[y][x] = tiles.autotile(inside, x, y, 'floor', 0, 7) + '@' + mode

# 2026-10-31(기우: 퀘스트를 마을별로) 퀘스트를 주고받는 사람은 talker → npc (World/Regions/north.json npcs에 같은 id 등록)
TALKER_TO_NPC = {('north_hunter', '가죽 무두장이 토라'): 'north_tora', ('north_naru', '선장'): 'north_captain',
                 ('north_laugar', '약초꾼'): 'north_herbalist', ('north_laugar', '목욕 회관 주인'): 'north_bathkeeper',
                 ('north_herd', '순록 목동 할머니'): 'north_herd_granny', ('north_herd', '썰매 장인'): 'north_sledmaker',
                 ('north_runehill', '룬 지기 노인'): 'north_runekeeper', ('north_icecave', '광부 할아범'): 'north_miner'}
SECRET_MOVE = {('north_naru', 5): (12, 66)}   # 2026-10-31(기우) 숨은 룬돌 5번이 2번과 5칸 거리 → 나루 남쪽 끝 바닷가 후미로
KEEPER_EXTRA = {'north_c_creek': ([41, 30], [41, 29])}   # 위쪽 좌판에 장꾼이 안 붙어 빈 좌판이던 것 (말 거는 칸, 맡은 좌판)
ROAD_CLEAR = 3    # 2026-10-29(사용자): 숲 지형에서 길 양옆 이만큼은 나무를 심지 않는다
ROAD_THIN = 6     # 그 밖 이 칸까지는 절반만 심는다(듬성한 숲)
CORRIDOR_PAD = 1   # 통로 양옆 여유 칸(출구 폭 4 + 2 = 6칸 트인 눈밭, 가운데 2칸 다진 길). 포켓몬 길처럼 나무 벽 사이 넓은 통로
EXIT_OUT = 60      # 2026-10-29(사용자: 길 중간에서 넘어감): 출구는 경계선이 아니라 그려 둔 길 끝(내 맵 가장자리 또는 옆 맵 땅이 끝나는 곳, 최대 이 값)에 둔다. 도착 자리는 같은 세계 칸
BOX = {}           # 맵별 그리는 상자(전역 배열 칸): y0, y1, x0, x1
for _k in KEYS:
    _ys, _xs = np.nonzero(MID == KEYS.index(_k)); BOX[_k] = (_ys.min() - 3, _ys.max() + 4, _xs.min() - 3, _xs.max() + 4)
def _dir(edge): return {'N': (-1, 0), 'S': (1, 0), 'W': (0, -1), 'E': (0, 1)}[edge]
def restrict_seams(se, roadw):
    """길(roadw)이 지나는 출입구만 남긴다. 맵 쌍에 길이 하나도 안 지나면 가장 긴 출입구 하나를 남긴다(길은 stub으로 그음).
    반환: (남긴 seams, 맵 쌍별 기록 {(A,B): dict(cells=[(y,x)...], edge, fallback)})"""
    H, W = roadw.shape
    def rd(c): return 0 <= c[0] < H and 0 <= c[1] < W and bool(roadw[c[0], c[1]])
    def cells_of(o):
        cy, cx = o['cell']; return [(cy + b, cx + a) for b in range(o['h']) for a in range(o['w'])]
    exits = [(mk, o) for mk, objs in se.items() for o in objs if o['kind'] == 'exit']
    spawns = {(mk, o['id']): o for mk, objs in se.items() for o in objs if o['kind'] == 'spawn'}
    # 양쪽 맵이 같은 결정을 하도록, 조각 기준 키 = 양쪽 칸 합집합
    def canon(o):
        dy, dx = _dir(o['edge']); cs = cells_of(o); return frozenset(cs) | frozenset((y + dy, x + dx) for y, x in cs)
    decide = {}; best = {}
    for mk, o in exits:
        dy, dx = _dir(o['edge']); cs = cells_of(o)
        px, py = (0, 1) if dy else (1, 0)
        def through(c):   # 경계에 '닿는' 길: 경계 칸이 길이고 내 쪽으로 2칸 이상 이어짐(상대 쪽은 없어도 stub이 잇는다). 경계를 따라 나란히 가는 길은 아님
            y, x = c
            if not rd((y, x)): return False
            back1 = any(rd((y - dy + py * s_, x - dx + px * s_)) for s_ in (-1, 0, 1))
            back2 = any(rd((y - 2 * dy + py * s_, x - 2 * dx + px * s_)) for s_ in (-1, 0, 1))
            return back1 and back2
        idx = [i for i, c in enumerate(cs) if through(c)]
        if len(idx) > 5: idx = []   # 경계에 길 칸이 6개 넘게 나란히 → 광장·나란한 길이지 건널목이 아님
        k = canon(o)
        if idx: decide[k] = (max(0, min(idx) - EXIT_GAP), min(len(cs) - 1, max(idx) + EXIT_GAP), False)
        pair = tuple(sorted((mk, o['target'])))
        if pair not in best or (len(cs), min(k)) > best[pair][0]: best[pair] = ((len(cs), min(k)), k, len(cs))
    # 2026-10-29(사용자: 원래 이어지던 골짜기가 막힘): 맵 쌍의 경계 조각을 통로(8칸 안 묶음)로 묶고,
    #   통로 가운데서 FALLBACK_DIST칸 안에 길 건널목이 없으면 통로 가운데 조각을 대체 건널목으로(길은 exit_stubs가 새로 긋고 가까운 길에 잇는다)
    pieces = {}
    for mk, o in exits: pieces.setdefault(tuple(sorted((mk, o['target']))), {})[canon(o)] = o
    for pair, pcs in pieces.items():
        ks = list(pcs); rest = list(ks)
        while rest:
            grp = [rest.pop()]; j_ = 0
            while j_ < len(grp):
                a_ = grp[j_]; j_ += 1
                near_ = [k2 for k2 in rest if min(abs(c1[0] - c2[0]) + abs(c1[1] - c2[1]) for c1 in a_ for c2 in k2) <= 8]
                for k2 in near_: rest.remove(k2); grp.append(k2)
            allc = [c for k_ in grp for c in k_]
            cy_, cx_ = sum(c[0] for c in allc) / len(allc), sum(c[1] for c in allc) / len(allc)
            if any(k_ in decide and min(abs(c[0] - cy_) + abs(c[1] - cx_) for c in k_) <= FALLBACK_DIST for k_ in grp): continue
            has_any = any(k_ in decide for k_ in pcs)
            seam_len = len({c for k_ in grp for c in k_}) // 2
            if os.environ.get('DBG_SEAM'): print('통로', pair, '길이', seam_len, '길있음', has_any)
            if has_any and (seam_len > FALLBACK_MAXLEN or seam_len < 4): continue   # 넓게 트인 눈밭 경계는 통로가 아니고(길 건널목 하나로 충분), 1~3칸 틈도 통로가 아님
            k_best = min(grp, key=lambda k_: min(abs(c[0] - cy_) + abs(c[1] - cx_) for c in k_))
            n_ = len(cells_of(pcs[k_best])); decide[k_best] = (0, n_ - 1, True)
    out = {mk: [] for mk in se}; rec = {}; cand = []
    for mk, o in exits:
        d = decide.get(canon(o))
        if d is None: continue
        i0, i1, fb = d; horiz = o['edge'] in 'NS'; cy, cx = o['cell']
        dy, dx = _dir(o['edge'])
        seam = dict(o, cell=(cy, cx + i0) if horiz else (cy + i0, cx), w=(i1 - i0 + 1) if horiz else 1, h=1 if horiz else (i1 - i0 + 1))
        if (seam['w'] if horiz else seam['h']) == 1:   # 1칸 출구는 2칸으로(곧은 길이 2칸 폭이라 반쪽이 막혀 보이던 문제)
            if horiz: seam['w'] = 2
            else: seam['h'] = 2
        # 2026-10-29(사용자: 길이 숲에 막혀 보임): 출구는 내 맵 상자의 맨 가장자리까지 — 길이 화면 끝까지 끊김 없이 이어지고 거기서 넘어간다.
        #   땅 주인(옆 맵·셋째 맵)은 가리지 않고 물만 피한다(물 앞에서 멈춤)
        by0, by1, bx0, bx1 = BOX[mk]; scs = cells_of(seam); k_out = 0
        for j in range(1, EXIT_OUT + 1):
            pts = [(y + dy * j, x + dx * j) for y, x in scs]
            if all(by0 <= y < by1 and bx0 <= x < bx1 and T_[y, x] not in (DEEP, SHALLOW, RIVER, LAKE, SPRING) for y, x in pts): k_out = j
            else: break
        # 길이 그 방향으로 얼마나 이어지는지(경계 너머 연속 길 칸 수): 계단식 경계에서 '길 방향' 조각을 고르는 기준
        run_ = 0
        for j in range(1, EXIT_OUT + 1):
            if any(rd((y + dy * j, x + dx * j)) for y, x in scs): run_ = j
            else: break
        o2 = dict(seam, cell=(seam['cell'][0] + dy * k_out, seam['cell'][1] + dx * k_out), out=k_out, run=run_)
        cand.append((mk, o, seam, o2, fb))
    # 비스듬한(계단식) 경계는 한 건널목에 N·E처럼 방향이 섞인 조각이 생긴다. 건널목(가까운 조각 묶음)마다 길 끝이 가장 먼 방향만 남긴다
    #   — 나머지 방향 조각은 경계선에서 바로 넘어가 버려서(길 중간) 뺀다
    groups = {}
    for i_, (mk, o, seam, o2, fb) in enumerate(cand): groups.setdefault((mk, o['target']), []).append(i_)
    keep_idx = set(); pre = {}
    for key_, ids in groups.items():
        rest = list(ids)
        while rest:
            grp = [rest.pop()]; j_ = 0
            while j_ < len(grp):
                a_ = cand[grp[j_]][2]['cell']; j_ += 1
                near_ = [i_ for i_ in rest if abs(cand[i_][2]['cell'][0] - a_[0]) <= 6 and abs(cand[i_][2]['cell'][1] - a_[1]) <= 6]
                for i_ in near_: rest.remove(i_); grp.append(i_)
            by_edge = {}
            for i_ in grp: by_edge.setdefault(cand[i_][1]['edge'], []).append(i_)
            best_e = max(by_edge, key=lambda e: (max(cand[i_][3]['run'] for i_ in by_edge[e]), max(cand[i_][3]['out'] for i_ in by_edge[e]), len(by_edge[e])))
            kmax = max(cand[i_][3]['out'] for i_ in by_edge[best_e])
            chosen = [i_ for i_ in by_edge[best_e] if cand[i_][3]['out'] >= kmax - 1]   # 같은 건널목에서 길 끝까지 못 나간 조각(경계에 남은 것)은 뺀다 → 길 중간 전환 방지
            pre.setdefault(key_, []).append((best_e, kmax, chosen))
    for key_, lst in pre.items():
        good = [t for t in lst if t[1] >= 3]   # 옆 맵 땅으로 3칸도 못 나가는 건널목(경계의 작은 홈)은 다른 건널목이 있으면 버린다
        for best_e, kmax, chosen in (good if good else [max(lst, key=lambda t: t[1])]):
            keep_idx |= set(chosen)
            cr = dict(cells=[], seam=[], edge=best_e, fallback=False, k=0, spawn_objs=[])   # 건널목 하나 = 묶음 하나
            for i_ in chosen:
                mk, o, seam, o2, fb = cand[i_]
                out[mk].append(o2)
                sp = spawns.get((o['target'], o['spawn']))
                if sp is not None:   # 도착 자리(임시) = 출구와 같은 세계 칸. 아래서 상대 맵 출구 복도 들머리로 옮긴다
                    sp2 = dict(sp, cell=o2['cell'], w=o2['w'], h=o2['h']); out[o['target']].append(sp2); cr['spawn_objs'].append(sp2)
                cr['cells'] += cells_of(o2); cr['seam'] += cells_of(seam); cr['fallback'] |= fb; cr['k'] = max(cr['k'], o2['out'])
            r = rec.setdefault(key_, dict(cells=[], seam=[], edges=[], fallback=False, k=0, crossings=[]))
            r['cells'] += cr['cells']; r['seam'] += cr['seam']; r['edges'].append(best_e); r['fallback'] |= cr['fallback']; r['k'] = max(r['k'], cr['k']); r['crossings'].append(cr)
    # 2026-10-29(사용자: 새 맵에서 안쪽에서 시작함): 도착 자리 = 상대 맵의 '같은 건널목' 출구 복도 들머리(그 맵 가장자리 출구 칸에서 안쪽 2칸).
    #   A 오른쪽 끝에서 나가면 B 왼쪽 끝 숲길 입구에 선다. 도착 facing은 안쪽이라 B 출구(바깥을 볼 때만 발동)에 다시 걸리지 않는다
    for (A, B), r in rec.items():
        for cr in r['crossings']:
            cr2 = next((c2 for c2 in rec.get((B, A), {}).get('crossings', []) if any(abs(a[0] - b[0]) + abs(a[1] - b[1]) <= 1 for a in cr['seam'] for b in c2['seam'])), None)
            if cr2 is None or not cr['spawn_objs']: continue
            dy2, dx2 = _dir(cr2['edge'])
            ys = sorted({c[0] for c in cr2['cells']}); xs = sorted({c[1] for c in cr2['cells']}); ex2 = set(cr2['cells'])
            for sp in cr['spawn_objs']:   # 크기는 자기 출구 그대로(런타임이 폭·축을 맞춰 봄), 자리만 옮긴다. 계단식 출구 칸과 겹치지 않을 때까지 안쪽으로
                for inward in range(2, max(2, cr2['k']) + 1):
                    cell = (ys[0] - dy2 * inward, xs[0] - dx2 * inward)
                    cells_ = {(cell[0] + b, cell[1] + a) for a in range(sp['w']) for b in range(sp['h'])}
                    if not (cells_ & ex2): break
                sp['cell'] = cell
    return out, rec
def exit_stubs(rec, roadw, terrain, mapid, keys):
    """맵 쌍마다 출입구 가운데를 지나 경계 앞뒤로 곧은 2칸 폭 길을 긋는다(물 칸은 건너뜀). 전역 roadw를 바꾼다. 반환: 그은 칸 수
    대체 건널목(원래 길이 없던 통로)은 곧은 길 양 끝에서 같은 맵 안 가장 가까운 길까지 길을 이어 붙인다"""
    H, W = roadw.shape; n = 0; done = set()
    BAD = (DEEP, SHALLOW, RIVER, LAKE, SPRING, VOID, MOUNTAIN, PEAK)
    VILG = VIL
    def connect(y, x, mid_, limit=90):
        """(y,x)가 속한 길 덩어리가 마을에 닿지 않고 작으면, mid_ 땅 안을 넓이 우선으로 걸어 덩어리 밖 가장 가까운 길 칸까지 2칸 폭 길을 긋는다"""
        if not (0 <= y < H and 0 <= x < W) or not roadw[y, x]: return 0
        comp = {(y, x)}; q = deque([(y, x)])
        while q and len(comp) < 200:
            cy_, cx_ = q.popleft()
            if VILG[cy_, cx_]: return 0   # 마을까지 이어진 길
            for oy, ox in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n_ = (cy_ + oy, cx_ + ox)
                if n_ not in comp and 0 <= n_[0] < H and 0 <= n_[1] < W and roadw[n_] and mapid[n_] == mid_: comp.add(n_); q.append(n_)
        if len(comp) >= 200: return 0   # 큰 길 그물
        q = deque(comp); prev = {c: None for c in comp}
        while q:
            cy_, cx_ = q.popleft()
            if roadw[cy_, cx_] and (cy_, cx_) not in comp:
                path = []; c = (cy_, cx_)
                while c is not None: path.append(c); c = prev[c]
                cnt = 0
                for (py_, px_) in path:
                    for (ay, ax) in ((py_, px_), (py_, px_ + 1), (py_ + 1, px_)):
                        if 0 <= ay < H and 0 <= ax < W and mapid[ay, ax] == mid_ and terrain[ay, ax] not in BAD and not roadw[ay, ax]: roadw[ay, ax] = True; cnt += 1
                return cnt
            if len(prev) > limit * limit: break
            for oy, ox in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = cy_ + oy, cx_ + ox
                if (ny, nx) in prev or not (0 <= ny < H and 0 <= nx < W) or mapid[ny, nx] != mid_ or terrain[ny, nx] in BAD or abs(ny - y) + abs(nx - x) > limit: continue
                prev[(ny, nx)] = (cy_, cx_); q.append((ny, nx))
        return 0
    for (A, B), r in rec.items():
        if (B, A) in done: continue
        done.add((A, B)); ab = {keys.index(A), keys.index(B)}
        for cr in r['crossings']:
            dy, dx = _dir(cr['edge']); perp = (0, 1) if dy else (1, 0)
            # 곧은 길은 경계 칸(건널목 폭) 가운데 2칸을 그대로 앞뒤로 늘인다(가운데값 반올림으로 옆 맵 땅에 치우치던 문제)
            sc = sorted(set(cr['seam']), key=lambda c: (c[1], c[0]) if dy else (c[0], c[1]))
            mid0 = max(0, (len(sc) - 2) // 2); lane = sc[mid0:mid0 + 2] or sc[:1]
            cy, cx = lane[0]
            kB = 0   # 상대 쪽 같은 건널목(경계 칸이 맞닿은 것)의 k
            for cr2 in rec.get((B, A), {}).get('crossings', []):
                if any(abs(c1[0] - c2[0]) + abs(c1[1] - c2[1]) <= 1 for c1 in cr['seam'] for c2 in cr2['seam']): kB = max(kB, cr2['k'])
            back = max(EXIT_STUB, kB + 1); fwd = max(EXIT_STUB, cr['k'] + 1)   # 양쪽 출구(길 끝)까지 + 1칸
            for k in range(-back, fwd + 1):
                for (ly, lx) in lane:
                    y, x = ly + dy * k, lx + dx * k
                    if 0 <= y < H and 0 <= x < W and mapid[y, x] in ab and terrain[y, x] not in (DEEP, SHALLOW, RIVER, LAKE, SPRING, VOID) and not roadw[y, x]:   # 두 맵 땅에만(세 맵 모서리에서 옆 맵 경계를 따라 길이 생기던 문제)
                        roadw[y, x] = True; n += 1
            for k, mid_ in ((-back, keys.index(A)), (fwd, keys.index(B))):   # 건널목 길이 각 맵의 길 그물(마을)에 닿는지 확인, 아니면 잇는다
                for k2 in range(abs(k), 0, -1):
                    y, x = cy + dy * k2 * (1 if k > 0 else -1), cx + dx * k2 * (1 if k > 0 else -1)
                    if 0 <= y < H and 0 <= x < W and mapid[y, x] == mid_ and roadw[y, x]: n += connect(y, x, mid_); break
    return n
SEAMS, SEAM_REC = restrict_seams(SEAMS, ROADW)
ROADW = ROADW.copy(); STUBS = exit_stubs(SEAM_REC, ROADW, T_, MID, KEYS)   # 새 길(stub·연결)은 그린 뒤 출입구를 다시 고르지 않는다(새 길이 경계를 따라가며 엉뚱한 건널목을 만들던 문제)
_SE0, _REC0 = SEAMS, SEAM_REC

# ── 2026-10-29 정리: 생활 소품, 갈 수 없는 자리 길 내기, 이름표 겹침 ──
DRESS = ['barrel', 'crate', 'firewood', 'sack', 'bucket', 'box', 'barrel_s', 'crate_goods', 'basket', 'logs', 'logs_birch']
NEED_REACH = {'stall', 'spit', 'fire', 'well', 'picturestone', 'runestone', 'table', 'cauldron', 'cauldron_snow', 'tent', 'weaponrack', 'pelt', 'rack'}

def dress_village(m, key, items, lots, L, mine, sub, P, rng):
    """집 옆(3칸 안)에 소품이 하나도 없으면 통·상자·장작 따위를 1~2개. 장터 둘레 6칸 안에 등이 없으면 모서리에 등 기둥 2개"""
    Hd, Wd = mine.shape
    npc_cells = {L(it['y'], it['x']) for it in items if it['kind'] in ('npc', 'event', 'bandit', 'spot', 'gather', 'growth')}
    def cell_ok(x, y):
        return (m.inside(x, y) and mine[y, x] and not m.blocked[y][x] and not m.water[y][x] and m.obj[y][x] is None and m.high[y][x] is None
                and m.road[y][x] == 0 and not m.reserved[y][x] and sub[y, x] not in (FOREST, MOUNTAIN, PEAK, VOID) and not P.used[y, x]
                and not any((x + a, y + b) in npc_cells for a in (-1, 0, 1) for b in (-1, 0, 1)))
    def put(name, x, y, margin=1):
        _, _, _, w, h, *_ = tiles.STAMPS[name]
        if not all(cell_ok(x + a, y + b) for a in range(w) for b in range(h)): return False
        # 아래 칸(지나가는 자리)이 막혀 있으면 안 놓는다
        if not any(m.inside(x + a, y + h) and not m.blocked[y + h][x + a] and not m.water[y + h][x + a] for a in range(w)): return False
        if not m.stamp(name, x, y, force=True): return False
        P.used[max(0, y - margin):y + h + margin, max(0, x - margin):x + w + margin] = True
        P.foot.append(('stamp', name, x, y, w, h)); return True
    near = lambda x0, y0, x1, y1, d, names=None: any(x0 - d <= f[2] + f[4] - 1 and f[2] <= x1 + d and y0 - d <= f[3] + f[5] - 1 and f[3] <= y1 + d
                                                     and f[0] == 'stamp' and (names is None or f[1] in names) for f in P.foot)
    got = []
    for it in lots:
        a = it.get('art') or ''
        if a.startswith('Minigame:') or it['name'].startswith('천막'): continue
        x, y = L(it['y'], it['x']); w, h = it['w'], it['h']
        sparse = key == 'north_c_gatetown'   # 북문 마을은 꾸밈이 적어 집마다 2~3개
        if not sparse and near(x, y, x + w - 1, y + h - 1, 3): continue
        want = rng.choice((2, 3)) if sparse else rng.choice((1, 2, 2)); n = 0
        spots = [(x - 1, y + h - 1), (x + w, y + h - 1), (x - 1, y + h - 2), (x + w, y + h - 2), (x - 2, y + h - 1), (x + w + 1, y + h - 1)]
        rng.shuffle(spots)
        for (sx, sy) in spots:
            if n >= want: break
            name = rng.choice(DRESS)
            if name.startswith('logs') and sx < x: sx -= 1
            if put(name, sx, sy, margin=0): n += 1; got.append(name)
    for it in items:
        if it['kind'] != 'plaza': continue
        x, y = L(it['y'], it['x']); w, h = it['w'], it['h']
        if near(x, y, x + w - 1, y + h - 1, 6, {'lantern', 'lamp'}) or any(j['kind'] == 'lantern' and abs(L(j['y'], j['x'])[0] - x - w // 2) < w and abs(L(j['y'], j['x'])[1] - y - h // 2) < h + 4 for j in items): continue
        n = 0
        for (sx, sy) in [(x - 1, y - 2), (x + w, y - 2), (x - 1, y + h - 1), (x + w, y + h - 1)]:
            if n < 2 and put('lantern', sx, sy): n += 1; got.append('lantern')
    return got

def reach_targets(m, items, L, P):
    """갈 수 있어야 하는 자리: (이름, 닿으면 되는 칸들)"""
    out = []
    ring = lambda x, y, w, h: {(x + a, y + b) for a in range(-1, w + 1) for b in range(-1, h + 1) if not (0 <= a < w and 0 <= b < h)}
    for (t, n, X, Y, w, h) in P.foot:
        if t == 'secret': out.append(('숨은 룬돌', {(X, Y + 2), (X - 1, Y + 1), (X + 1, Y + 1)}))
        elif t == 'spot': out.append((n, {(X, Y)} | ring(X, Y, 1, 1)))
        elif t == 'yard': out.append((n, {(X + a, Y + b) for a in range(w) for b in range(h)}))
        elif t == 'stamp' and n in NEED_REACH: out.append((n, ring(X, Y, w, h)))
    for it in items:
        if it['kind'] in ('npc', 'event', 'bandit', 'spot', 'gather', 'growth', 'yard', 'rest', 'harbor'):
            x, y = L(it['y'], it['x']); out.append((it['name'], {(x + a, y + b) for a in range(it['w']) for b in range(it['h'])} | ring(x, y, it['w'], it['h'])))
    for (x, y) in m.fishing: out.append(('낚시', ring(x, y, 1, 1)))
    # 2026-10-29(사용자: 눈숲 고갯길 쪽에서 갇힘): 옆 맵에서 들어오는 자리·나가는 자리도 길과 이어져야 한다
    for (name, cells) in getattr(m, '_edge_targets', []): out.append((name, cells))
    return out

def _reach(m, mine, extra_block=frozenset(), from_spawn=True):
    H, W = mine.shape
    def walk(c): return 0 <= c[0] < W and 0 <= c[1] < H and mine[c[1], c[0]] and not m.blocked[c[1]][c[0]] and not m.water[c[1]][c[0]] and c not in extra_block
    q = deque(c for c in ((x, y) for y in range(H) for x in range(W) if m.road[y][x] > 0) if walk(c)); seen = set(q)
    for o in (m.objects if from_spawn else []):
        if o['kind'] == 'spawn' and walk((o['x'], o['y'])) and (o['x'], o['y']) not in seen: seen.add((o['x'], o['y'])); q.append((o['x'], o['y']))
    while q:
        c = q.popleft()
        for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (c[0] + a, c[1] + b)
            if n not in seen and walk(n): seen.add(n); q.append(n)
    return seen

def open_paths(m, mine, pending, targets):
    """심기 전에: 심을 칸까지 막힌 것으로 보고 길에서 닿지 않는 자리가 있으면, 비울 수 있는(숲·산·작은 숲) 칸을 가장 적게 지나는 길을 2칸 폭으로 비운다"""
    H, W = mine.shape
    soft = set().union(*[p[0] for p in pending if p[2]]); hard = set().union(*[p[0] for p in pending if not p[2]])
    plan = soft | hard; opened = []; stuck = []
    reach = _reach(m, mine, plan)
    def passable(c):
        return 0 <= c[0] < W and 0 <= c[1] < H and mine[c[1], c[0]] and not m.blocked[c[1]][c[0]] and not m.water[c[1]][c[0]] and c not in hard
    for name, cells in targets:
        if any(c in reach for c in cells): continue
        start = [c for c in cells if passable(c)]
        dist = {c: (1 if c in soft else 0) for c in start}; prev = {c: None for c in start}; dq = deque(sorted(start, key=lambda c: dist[c])); end = None
        while dq:
            c = dq.popleft()
            if c in reach: end = c; break
            for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (c[0] + a, c[1] + b)
                if not passable(n): continue
                nd = dist[c] + (1 if n in soft else 0)
                if n not in dist or nd < dist[n]:
                    dist[n] = nd; prev[n] = c
                    (dq.appendleft if nd == dist[c] else dq.append)(n)
        if end is None:
            # 2026-10-29(갇힘): 물로 막힌 주머니 땅이면 가장 짧은 곳에 2칸 폭 다리를 놓아 잇는다
            pocket = set(); q = deque(start)
            pocket.update(start)
            while q:
                c = q.popleft()
                for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    n = (c[0] + a, c[1] + b)
                    if n not in pocket and passable(n): pocket.add(n); q.append(n)
            best = None
            def wat(c): return 0 <= c[0] < W and 0 <= c[1] < H and mine[c[1], c[0]] and m.water[c[1]][c[0]] and c not in m._springs
            for (px, py) in pocket:
                for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    for side in (0, 1):   # 폭 2칸: 옆줄
                        ox, oy = (0, side) if a else (side, 0)
                        if (px + ox, py + oy) not in pocket: continue
                        k = 1
                        while k <= 10 and wat((px + a * k, py + b * k)) and wat((px + a * k + ox, py + b * k + oy)): k += 1
                        e1, e2 = (px + a * k, py + b * k), (px + a * k + ox, py + b * k + oy)
                        if 1 < k <= 10 and e1 in reach and e2 in reach and (best is None or k < best[0]):
                            best = (k, px, py, a, b, ox, oy)
            if best is None: stuck.append(name); continue
            k, px, py, a, b, ox, oy = best
            xs = [px + a * j + d * ox for j in range(1, k) for d in (0, 1)]; ys = [py + b * j + d * oy for j in range(1, k) for d in (0, 1)]
            m.bridge(min(xs), min(ys), max(xs), max(ys))
            opened.append((name, 'bridge', k - 1))
            reach = _reach(m, mine, plan); continue
        path = []; c = end
        while c is not None: path.append(c); c = prev[c]
        cut = set()
        for (x, y) in path:
            for c in ((x, y), (x + 1, y), (x, y + 1)):
                if c in soft: cut.add(c)
        for p in pending:
            if p[2]: p[0].difference_update(cut)
        soft -= cut; plan -= cut; opened.append((name, len(cut)))
        reach = _reach(m, mine, plan)
    return dict(opened=opened, stuck=stuck)

LABELED = ('talker', 'npc', 'sign', 'board')
def tidy_people(m, mine):
    """마무리 뒤: 이름표 달린 것(사람·이정표·게시판)끼리 겹치지 않게(가로 3칸·세로 1칸 안에 둘이면 뒤엣것을 옮김),
    길에서 걸어 닿지 않는 사람은 닿는 빈 칸으로 옮긴다.
    2026-10-29: 경계 출입구·도착 칸 둘레 1칸 안에 선 사람·이정표·게시판도 옮긴다(옆 맵에서 오자마자 걸리지 않게). 이정표는 그림 칸도 같이 옮김.
    같은 칸 둘레의 모닥불 의자는 뺀다"""
    zone = {(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] in ('exit', 'spawn') for a in range(-1, o.get('w', 1) + 1) for b in range(-1, o.get('h', 1) + 1)}
    m.spots = [sp for sp in m.spots if not (sp[0] == 'seat' and (sp[1], sp[2]) in zone)]
    reach = _reach(m, mine); moved = []
    occ = {(o['x'], o['y']) for o in m.objects if o['kind'] not in ('exit', 'spawn', 'anim')}
    placed = []
    def clash(x, y): return any(abs(x - a) <= 3 and abs(y - b) <= 1 for a, b in placed)
    LR = {'sign': 1}
    def free(x, y, r=2):
        return ((x, y) in reach and m.inside(x, y + 1) and not m.blocked[y][x] and m.obj[y][x] is None and m.high[y][x] is None
                and m.road[y][x] == 0 and (x, y) not in occ and (x, y) not in zone and 2 <= x < m.w - 2 and y >= 2 and label_clear(m, x, y, r))
    for o in m.objects:
        if o['kind'] not in LABELED: continue
        x, y = o['x'], o['y']
        bad_reach = o['kind'] in ('talker', 'npc') and not any((x + a, y + b) in reach for a, b in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)))
        rr = LR.get(o['kind'], 2); bad_label = not label_clear(m, x, y, rr)   # 2026-10-30: 이름표가 그림을 덮으면 옮김
        if not clash(x, y) and not bad_reach and not bad_label and (x, y) not in zone: placed.append((x, y)); continue
        best = None
        for r in range(1, 9):
            for yy in range(y - r, y + r + 1):
                for xx in range(x - r, x + r + 1):
                    if max(abs(xx - x), abs(yy - y)) != r or not free(xx, yy, rr) or clash(xx, yy): continue
                    near_road = any(m.inside(xx + a, yy + b) and m.road[yy + b][xx + a] for a in (-1, 0, 1) for b in (-1, 0, 1))
                    k_ = (0 if near_road else 1, abs(xx - x) + abs(yy - y))
                    if best is None or k_ < best[0]: best = (k_, xx, yy)
            if best: break
        if best:
            if o['kind'] == 'sign':
                if m.obj[y][x] == tiles.SIGN: m.obj[y][x] = None; m.blocked[y][x] = False
                m.obj[best[2]][best[1]] = tiles.SIGN; m.blocked[best[2]][best[1]] = True; m.reserved[best[2]][best[1]] = True
            occ.discard((x, y)); o['x'], o['y'] = best[1], best[2]; occ.add((o['x'], o['y'])); moved.append((o.get('name') or o.get('npc') or o.get('text', '')[:8], (x, y), (o['x'], o['y'])))
        elif o['kind'] == 'sign' and (x, y) in zone and m.obj[y][x] is None:   # 그림이 치워졌는데 옮길 곳이 없으면 이정표를 뺀다
            o['kind'] = '_drop'
        placed.append((o['x'], o['y']))
    m.objects = [o for o in m.objects if o['kind'] != '_drop']
    return moved

def final_check(m, mine):
    """다 만든 뒤 길에서 닿지 않는 것 목록(보고용). 도착 자리에서 시작하지 않고 길에서만 걷는다"""
    reach = _reach(m, mine, from_spawn=False); bad = []
    near = lambda x, y: any((x + a, y + b) in reach for a in (-1, 0, 1) for b in (-1, 0, 1))
    for o in m.objects:
        if o['kind'] in LABELED + ('market', 'event', 'bandit') and not near(o['x'], o['y']): bad.append((o['kind'], o.get('name') or o.get('npc') or o.get('id'), o['x'], o['y']))
    for (x, y, i) in m.secrets:
        if (x, y + 2) not in reach and not near(x, y + 1): bad.append(('secret', i, x, y))
    for (k, x, y) in m.spots:
        if k == 'seat': continue
        if not near(x, y): bad.append(('spot', k, x, y))
    for (x, y) in m.fishing:
        if not near(x, y): bad.append(('fishing', '', x, y))
    # 출입구·도착 자리: 길과 이어진 땅이어야(도착했는데 갇히는 일 없게). 칸의 반 넘게 닿아야 통과
    for o in m.objects:
        if o['kind'] not in ('exit', 'spawn'): continue
        cells = [(o['x'] + a, o['y'] + b) for a in range(o.get('w', 1)) for b in range(o.get('h', 1))]
        ok = sum(1 for c in cells if near(*c))
        if ok * 2 < len(cells): bad.append((o['kind'], o.get('target') or o.get('id'), o['x'], o['y'], f'{ok}/{len(cells)}'))
    return bad

PLUGS = {'nature:10:12', 'nature:4:12', 'nature:5:12', 'nature:10:13', 'nature:18:9'}
def unplug_props(m, mine, targets):
    """닿지 않는 자리마다: 1칸 눈 더미를 지나가도 되는 칸으로 보고 가장 적게 지나는 길을 찾아 그 눈 더미를 치운다"""
    H, W = mine.shape; removed = []
    def plug(c): return m.blocked[c[1]][c[0]] and m.high[c[1]][c[0]] is None and m.obj[c[1]][c[0]] in PLUGS and not (m.inside(c[0], c[1] - 1) and m.high[c[1] - 1][c[0]] is not None)
    def passable(c): return 0 <= c[0] < W and 0 <= c[1] < H and mine[c[1], c[0]] and not m.water[c[1]][c[0]] and (not m.blocked[c[1]][c[0]] or plug(c))
    reach = _reach(m, mine)
    for name, cells in targets:
        if any(c in reach for c in cells): continue
        start = [c for c in cells if passable(c)]
        dist = {c: (1 if m.blocked[c[1]][c[0]] else 0) for c in start}; prev = {c: None for c in start}; dq = deque(start); end = None
        while dq:
            c = dq.popleft()
            if c in reach: end = c; break
            for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (c[0] + a, c[1] + b)
                if not passable(n): continue
                nd = dist[c] + (1 if m.blocked[n[1]][n[0]] else 0)
                if n not in dist or nd < dist[n]:
                    dist[n] = nd; prev[n] = c; (dq.appendleft if nd == dist[c] else dq.append)(n)
        if end is None: continue
        c = end
        while c is not None:
            if m.blocked[c[1]][c[0]]: m.blocked[c[1]][c[0]] = False; m.obj[c[1]][c[0]] = None; removed.append(c)
            c = prev[c]
        reach = _reach(m, mine)
    return len(removed)

# 2026-10-29(사용자 결정) 맵별 손질. 좌표는 세계 칸(N2 zones.json과 같은 기준)
MOVE_LOTS = {('north_laugar', '꿀술 양조장'): (-12, -268)}   # 라우가르: 장터 옆이 너무 복잡해 서쪽 빈 눈밭으로
EXTRA_ITEMS = {'north_herd': [   # 순록 고원: 룬돌 언덕 가는 길 북쪽이 비어 있어 여관·집·여관 주인
    dict(name='고원 여관', kind='lot', art='woodhouse', w=4, h=5, x=-47, y=-307, door=[-45, -302], id='', memo=''),
    dict(name='집 5', kind='lot', art='h_turf', w=3, h=2, x=-37, y=-306, door=[-36, -304], id='', memo=''),
    dict(name='여관 주인', kind='npc', w=1, h=1, x=-43, y=-301, id='', memo='talker')]}
EXTRA_PATHS = {'north_herd': [(-49, -279), (-40, -290), (-40, -298), (-35, -311), (-32, -318)]}   # 마을 → 여관 → 룬돌 언덕 출구 오솔길
CRAFT_NEAR = {'craft_brew': ('꿀술 양조장', 'any'), 'craft_fish': ('대구 덕장 2', 'left'), 'craft_fur': ('털옷 공방', 'any')}   # 공방 일꾼은 제 건물·덕장 옆, 길 밖

def place_crafts(m, items, L, mine, P):
    """공방 일꾼 자리(craft_*)를 맞는 건물·덕장 바로 옆 빈 칸으로(길을 막지 않게 길 옆 칸은 뒤로)"""
    moved = []
    for i_, (k, x, y) in enumerate(list(m.spots)):
        if k not in CRAFT_NEAR: continue
        name, side = CRAFT_NEAR[k]; it = next((j for j in items if j['name'] == name), None)
        if not it: continue
        bx, by = L(it['y'], it['x']); w, h = it['w'], it['h']
        best = None
        for cy in range(by - 2, by + h + 2):
            for cx in range(bx - 2, bx + w + 2):
                if bx <= cx < bx + w and by <= cy < by + h: continue
                if not (m.inside(cx, cy) and mine[cy, cx] and not m.blocked[cy][cx] and not m.water[cy][cx] and m.obj[cy][cx] is None and m.high[cy][cx] is None and m.road[cy][cx] == 0): continue
                near_road = any(m.inside(cx + a, cy + b) and m.road[cy + b][cx + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                score = (near_road, 0 if (side != 'left' or cx < bx) else 1, abs(cx - (bx + (0 if side == 'left' else w // 2))) + abs(cy - (by + h)))
                if best is None or score < best[0]: best = (score, cx, cy)
        if best:
            m.spots[i_] = (k, best[1], best[2]); moved.append((k, (x, y), (best[1], best[2])))
            P.foot = [(t, n, best[1], best[2], fw, fh) if (t == 'spot' and n == k and (X, Y) == (x, y)) else (t, n, X, Y, fw, fh) for (t, n, X, Y, fw, fh) in P.foot]
    return moved

def _vnoise(seed, cell=7):
    """덩어리 모양 값 잡음(0~1): cell칸 격자 값을 이중 선형으로 보간"""
    rg = random.Random(seed); grid = {}
    def g(a, b):
        if (a, b) not in grid: grid[(a, b)] = rg.random()
        return grid[(a, b)]
    def f(x, y):
        a, b = x / cell, y / cell; ia, ib = int(a), int(b); fa, fb = a - ia, b - ib
        fa = fa * fa * (3 - 2 * fa); fb = fb * fb * (3 - 2 * fb)
        return (g(ia, ib) * (1 - fa) + g(ia + 1, ib) * fa) * (1 - fb) + (g(ia, ib + 1) * (1 - fa) + g(ia + 1, ib + 1) * fa) * fb
    return f

# 2026-10-30(사용자: 잘린 에셋은 작은 소품으로): 뒤 단계(길 잇기·출구 정리·복도 열기)가 칸 단위로 obj/high를 지워
#   4칸 이상 도장이 일부만 남은 것 → 통째로 지우고 그 자리(없으면 둘레 3칸 안)에 1칸 소품 하나. 길 끊는 자리엔 안 놓는다
CUT_PROPS_TREE = ['rocksmall', 'stump']; CUT_PROPS_HOUSE = ['crate', 'barrel_s', 'stump']
def fix_cut_stamps(m, mine):
    def parts(n, X, Y): return [('longhall_roof', X, Y + 2), ('longhall_front', X, Y + 5)] if n == 'longhall' else [(n, X, Y)]
    def foot(pn, px, py):
        s, sx, sy, w, h, bf, hr, rc = tiles.STAMPS[pn]
        return [(px + dx, py + dy, K(s, sx + dx, sy + dy, rc), dy < hr) for dy in range(h) for dx in range(w) if m.inside(px + dx, py + dy)]
    cut = []
    for st in m.stamps:
        for (pn, px, py) in parts(*st):
            w, h = tiles.STAMPS[pn][3], tiles.STAMPS[pn][4]
            if w * h < 4: continue
            if any((m.high if hi else m.obj)[y][x] != k for x, y, k, hi in foot(pn, px, py)): cut.append(st); break
    if not cut: return []
    taken = {(o['x'], o['y']) for o in m.objects} | {(sp[1], sp[2]) for sp in m.spots}
    zone = {(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] in ('exit', 'spawn') for a in range(-1, o.get('w', 1) + 1) for b in range(-1, o.get('h', 1) + 1)}
    done = []
    for st in cut:
        cells = []
        for (pn, px, py) in parts(*st):
            for x, y, k, hi in foot(pn, px, py):
                cells.append((x, y))
                if m.obj[y][x] == k: m.obj[y][x] = None
                if m.high[y][x] == k: m.high[y][x] = None
                if m.obj[y][x] is None and m.high[y][x] is None and not m.water[y][x]: m.blocked[y][x] = False; m.reserved[y][x] = False
        m.stamps = [t for t in m.stamps if t != st]
        n, X, Y = st
        tree = tiles.STAMPS[parts(*st)[0][0]][0] == 'nature'
        kinds = CUT_PROPS_TREE if tree else CUT_PROPS_HOUSE
        prop = kinds[(int(X) * 7 + int(Y) * 13) % len(kinds)]
        bx = sum(c[0] for c in cells) / len(cells); by = max(c[1] for c in cells)   # 아랫줄 가운데부터
        def ok(x, y):
            return (m.inside(x, y) and mine[y, x] and not m.blocked[y][x] and not m.water[y][x] and m.road[y][x] == 0
                    and m.obj[y][x] is None and m.high[y][x] is None and (x, y) not in taken and (x, y) not in zone
                    and not any(m.inside(x + a, y + b) and m.road[y + b][x + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))))
        r0 = len(_reach(m, mine))
        cands = sorted({(x, y) for x, y in cells} | {(x + a, y + b) for x, y in cells for a in range(-3, 4) for b in range(-3, 4)},
                       key=lambda c: (c not in cells, abs(c[0] - bx) + abs(c[1] - by), c[1], c[0]))
        put = None
        for (x, y) in cands:
            if not ok(x, y) or len(_reach(m, mine, extra_block={(x, y)})) < r0 - 1: continue
            if m.stamp(prop, x, y, force=True): put = (prop, x, y); break
        done.append((n, (int(X), int(Y)), put))
    return done

# ── 2026-10-30(사용자: 사람은 길 말고 건물 앞에, 에셋을 뒤로 두고 그 앞에 사람 / 이름표가 에셋을 가리지 않게) ──
def label_clear(m, x, y, r=2):
    """이름표(머리 위 한 칸, 가로 ±r칸)가 건물·나무·소품 그림을 덮지 않는지"""
    return all(not m.inside(x + a, y - 1) or (m.obj[y - 1][x + a] is None and m.high[y - 1][x + a] is None) for a in range(-r, r + 1))

def _road_comps(m, mine):
    seen = set(); n = 0
    for y in range(m.h):
        for x in range(m.w):
            if (x, y) in seen or m.road[y][x] <= 0 or not mine[y, x]: continue
            n += 1; st = [(x, y)]; seen.add((x, y))
            while st:
                cx, cy = st.pop()
                for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = cx + a, cy + b
                    if m.inside(nx, ny) and (nx, ny) not in seen and m.road[ny][nx] > 0 and mine[ny, nx]: seen.add((nx, ny)); st.append((nx, ny))
    return n

def front_strip(m, lots, mine, L):
    """문 앞이 바로 길인 건물: 위가 비어 있으면 1~2칸 뒤로 물리고, 모자라면 앞 길 줄을 눈밭으로(문 앞 골목 한 칸은 남김).
    문과 길 사이에 2줄 눈밭 띠가 생겨 사람이 길 아닌 문 앞에 설 수 있다"""
    out = []; base = _road_comps(m, mine)
    for it in lots:
        if not it.get('door') or not it.get('_stamp'): continue
        name, X, Y = it['_stamp']
        if (name, X, Y) not in m.stamps or name not in tiles.STAMPS: continue
        s_, sx, sy, w, h, bf, hr, rc = tiles.STAMPS[name]
        dcx, dcy = L(it['door'][1], it['door'][0]); bot = Y + h - 1
        if dcy != bot + 1 or not (X <= dcx < X + w): continue
        cols = range(X - 1, X + w + 1)
        if not any(m.inside(c, r) and m.road[r][c] > 0 for c in cols for r in (bot + 1, bot + 2)): continue
        def up_ok(sh): return all(m.inside(X + a, Y - j) and mine[Y - j, X + a] and not m.water[Y - j][X + a] and m.road[Y - j][X + a] == 0
                                  and m.obj[Y - j][X + a] is None and m.high[Y - j][X + a] is None and not m.blocked[Y - j][X + a]
                                  for a in range(w) for j in range(1, sh + 1))
        sh = 2 if up_ok(2) else 1 if up_ok(1) else 0
        if sh:
            for dy in range(h):
                for dx in range(w):
                    k = K(s_, sx + dx, sy + dy, rc)
                    if m.obj[Y + dy][X + dx] == k: m.obj[Y + dy][X + dx] = None
                    if m.high[Y + dy][X + dx] == k: m.high[Y + dy][X + dx] = None
                    m.blocked[Y + dy][X + dx] = False; m.reserved[Y + dy][X + dx] = False
            m.stamps.remove((name, X, Y))
            if not m.stamp(name, X, Y - sh, force=True, over_road=True):   # 안 되면 제자리
                m.stamp(name, X, Y, force=True, over_road=True); sh = 0
            else:
                for dy in range(h - sh, h):   # 비운 아랫줄도 다른 자동 배치에 안 쓰이게
                    for dx in range(w): m.reserved[Y + dy][X + dx] = True
                for st in [t for t in m.stamps if t[0] in ('lantern', 'banner', 'snowpost') and X - 1 <= t[1] <= X + w and Y <= t[2] <= bot]:   # 옆 등불·깃발도 같이 뒤로
                    ln, lx, ly = st; ls_, lsx, lsy, lw, lh, *_r = tiles.STAMPS[ln]
                    if not all(m.inside(lx, ly - j) and m.obj[ly - j][lx] is None and m.high[ly - j][lx] is None and not m.water[ly - j][lx] and m.road[ly - j][lx] == 0 for j in range(1, sh + 1)): continue
                    for dy in range(lh):
                        m.obj[ly + dy][lx] = m.high[ly + dy][lx] = None; m.blocked[ly + dy][lx] = False; m.reserved[ly + dy][lx] = False
                    m.stamps.remove(st)
                    if not m.stamp(ln, lx, ly - sh, force=True, over_road=True): m.stamp(ln, lx, ly, force=True, over_road=True)
        nb = bot - sh; conv = 0
        for r in (nb + 1, nb + 2):
            for c in cols:
                if not m.inside(c, r) or m.road[r][c] <= 0 or c == dcx: continue
                m.road[r][c] = 0
                if _road_comps(m, mine) > base: m.road[r][c] = 1; continue   # 길이 끊기면 그대로
                m.reserved[r][c] = True; conv += 1
        r = nb + 1   # 문 앞 골목: 문에서 길까지
        while m.inside(dcx, r) and m.road[r][dcx] == 0 and r <= nb + 4 and not m.blocked[r][dcx] and not m.water[r][dcx]:
            m.road[r][dcx] = 1; m.reserved[r][dcx] = True; r += 1
        for r in (nb + 1, nb + 2):   # 눈밭 띠는 바닥을 정해 둬서 뒤의 '길 홈 메우기'가 다시 길로 채우지 않게
            for c in cols:
                if m.inside(c, r) and m.road[r][c] == 0 and not m.water[r][c] and m.ground[r][c] is None: m.ground[r][c] = nt.SNOW
        it['y'] -= sh; it['door'] = [it['door'][0], it['door'][1] - sh]; it['_stamp'] = (name, X, Y - sh); it['_front'] = (dcx, nb + 1)
        out.append((it['name'], sh, conv))
    return out

import re as _re
_N2SRC = open(os.path.join(HERE, 'n2_zones.py'), encoding='utf-8').read()
_VSPLIT = [(mt.group(1), mt.start()) for mt in _re.finditer(r"^ '(north_[a-z_]+)': village\(", _N2SRC, _re.M)]
NPC_HOME = {}
for _i, (_k, _st) in enumerate(_VSPLIT):
    _blk = _N2SRC[_st:_VSPLIT[_i + 1][1] if _i + 1 < len(_VSPLIT) else len(_N2SRC)]
    NPC_HOME[_k] = {(mt.group(3) or mt.group(1)): mt.group(2) for mt in _re.finditer(r"N\('([^']+)', '([^']+)'(?:, id='([^']+)')?", _blk)}

def home_npcs(m, key, mine, L):
    """건물에 딸린 사람(n2_zones의 N(이름, 건물))은 그 건물 문 옆, 문 앞 띠의 둘째 줄에 세운다(이름표가 건물을 덮지 않게).
    'houses'는 가장 서쪽 집. 광장·마당 같은 건물 아닌 자리는 그대로(이름표 규칙은 tidy_people)"""
    homes = NPC_HOME.get(key, {}); lots = {it['name']: it for it in getattr(m, '_items', []) if it.get('_stamp') and it.get('door') and it['_stamp'] in m.stamps}
    houses = sorted([it for n_, it in lots.items() if n_.startswith('집 ')], key=lambda it: it['_stamp'][1])
    reach = _reach(m, mine); occ = {(o['x'], o['y']) for o in m.objects}
    zone = {(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] in ('exit', 'spawn') for a in range(-1, o.get('w', 1) + 1) for b in range(-1, o.get('h', 1) + 1)}
    def ok(x, y):
        return ((x, y) in reach and not m.blocked[y][x] and m.road[y][x] == 0 and m.obj[y][x] is None and m.high[y][x] is None
                and (x, y) not in occ and (x, y) not in zone and label_clear(m, x, y)
                and not any(q is not cur[0] and q['kind'] in LABELED and abs(q['x'] - x) <= 3 and abs(q['y'] - y) <= 1 for q in m.objects))   # 이름표끼리 겹치지 않게
    cur = [None]
    def spot(it):
        name, X, Y = it['_stamp']; h = tiles.STAMPS[name][4]; dcx, fy = L(it['door'][1], it['door'][0])
        for (a, b) in ((1, 1), (-1, 1), (2, 1), (-2, 1), (1, 2), (-1, 2), (2, 2), (-2, 2), (3, 1), (-3, 1), (3, 2), (-3, 2)):
            if m.inside(dcx + a, fy + b) and ok(dcx + a, fy + b): return (dcx + a, fy + b)
        return None
    moved = []
    for o in m.objects:
        if o['kind'] not in ('npc', 'talker'): continue
        anc = homes.get(o.get('npc') or o.get('name'))
        if not anc: continue
        cands = houses if anc == 'houses' else [lots[anc]] if anc in lots else []; cur[0] = o
        for it in cands:
            sp = spot(it)
            if sp:
                occ.discard((o['x'], o['y'])); moved.append((o.get('name') or o.get('npc'), (o['x'], o['y']), sp)); o['x'], o['y'] = sp; occ.add(sp); o['_home'] = True; break
    return moved

def exit_signs_out(m):
    """옆 북유럽 맵 출구 이정표: 경계 안쪽에 서 있으면 복도 길을 따라 실제 출구 쪽(가운데쯤)으로 옮긴다(사용자 10/30 비르카)"""
    moved = []
    zone = {(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] in ('exit', 'spawn') for a in range(-2, o.get('w', 1) + 2) for b in range(-2, o.get('h', 1) + 2)}
    taken = {(o['x'], o['y']) for o in m.objects}
    for o in m.objects:
        if o['kind'] != 'sign' or o.get('_tag') != 'exit': continue
        side = {'↑': lambda e: e['y'] == 0, '↓': lambda e: e['y'] + e.get('h', 1) >= m.h, '←': lambda e: e['x'] == 0, '→': lambda e: e['x'] + e.get('w', 1) >= m.w}.get((o.get('text') or ' ')[0], lambda e: True)
        exs = [e for e in m.objects if e['kind'] == 'exit' and NAMES.get(e.get('target'), '@') in (o.get('text') or '') and side(e)]
        if not exs: continue
        e = min(exs, key=lambda e: abs(e['x'] - o['x']) + abs(e['y'] - o['y']))
        ex, ey = e['x'] + (e.get('w', 1) - 1) / 2, e['y'] + (e.get('h', 1) - 1) / 2
        D = abs(ex - o['x']) + abs(ey - o['y'])
        if D <= 9 or D > 30: continue   # 가까우면 그대로, 너무 멀면(갈림길 안내) 그대로
        best = None
        for y in range(m.h):
            for x in range(m.w):
                if (x, y) in zone or (x, y) in taken or m.blocked[y][x] or m.water[y][x] or m.road[y][x] > 0 or m.obj[y][x] is not None or m.high[y][x] is not None: continue
                if not any(m.inside(x + a, y + b) and m.road[y + b][x + a] > 0 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))): continue
                d1 = abs(x - o['x']) + abs(y - o['y']); d2 = abs(x - ex) + abs(y - ey)
                if d1 + d2 > D + 4 or not label_clear(m, x, y, 1): continue
                sc = abs(d2 - D * 0.45)
                if best is None or sc < best[0]: best = (sc, x, y)
        if not best: continue
        if m.obj[o['y']][o['x']] == tiles.SIGN: m.obj[o['y']][o['x']] = None; m.blocked[o['y']][o['x']] = False
        _, x, y = best; m.obj[y][x] = tiles.SIGN; m.blocked[y][x] = True; m.reserved[y][x] = True
        taken.discard((o['x'], o['y'])); moved.append((o['text'][:12], (o['x'], o['y']), (x, y))); o['x'], o['y'] = x, y; taken.add((x, y))
    return moved

# ── 2026-10-30(사용자 기획): 눈숲 마을 눈싸움 놀이터 ──
#   큰 빈 눈밭에 16×11 놀이터: 눈더미 벽 5개(2칸, 실제로 막힘), 네 귀퉁이 깃발, 아이 3명 자리. 동작은 런타임 NorthSnowball.cs
SNOW_ARENA = {'north_c_snowwood': (80, 38)}   # 놀이터를 찾기 시작할 대략의 자리(빈 눈밭 가운데)
SNOW_WALLS = [(3, 2), (11, 2), (7, 5), (3, 8), (11, 8)]   # 놀이터 안 왼쪽 칸(가로 2칸)
SNOW_KIDS = [(4, 3), (12, 3), (8, 6)]
def _free_rect(m, mine, sizes, cx, cy, avoid=()):
    """(cx, cy) 가까이에서 sizes 중 처음 맞는 크기의 빈 네모. 1칸·낮은 소품(그루터기·작은 바위·눈더미·풀포기)은 치우고 쓴다.
    나무·건물(위층 있는 것)·길·물·사람 자리는 피한다. 찾으면 걸친 소품을 통째로 치우고 (x0, y0, W, H)"""
    taken = {(o['x'], o['y']) for o in m.objects} | {(sp[1], sp[2]) for sp in m.spots} | set(avoid)
    def free(x, y): return (m.inside(x, y) and mine[y, x] and not m.water[y][x] and m.road[y][x] == 0 and m.high[y][x] is None
                            and (x, y) not in taken and (m.obj[y][x] is not None or not m.blocked[y][x])
                            and not (m.obj[y][x] is not None and m.inside(x, y - 1) and m.high[y - 1][x] is not None)
                            and not getattr(m, '_claimed', set()).__contains__((x, y)))
    best = None   # (점수, x0, y0, W, H): 바라는 자리에서 가까울수록, 큰 크기일수록 좋음
    for si, (W, H) in enumerate(sizes):
        for y0 in range(1, m.h - H - 1):
            for x0 in range(1, m.w - W - 1):
                d = abs(x0 + W // 2 - cx) + abs(y0 + H // 2 - cy) + 6 * si
                if best and d >= best[0]: continue
                if all(free(x0 + a, y0 + b) for b in range(H) for a in range(W)): best = (d, x0, y0, W, H)
    if not best: return None
    _, x0, y0, W, H = best
    inr = lambda x, y: x0 <= x < x0 + W and y0 <= y < y0 + H
    for st in list(m.stamps):   # 걸친 소품은 통째로 뺀다(잘린 그림 안 남게)
        n_, X, Y = st
        if n_ not in tiles.STAMPS: continue
        w_, h_ = tiles.STAMPS[n_][3], tiles.STAMPS[n_][4]
        cells = [(X + a, Y + b) for a in range(w_) for b in range(h_)]
        if any(inr(*c) for c in cells):
            for (a, b) in cells:
                if m.inside(a, b):
                    m.obj[b][a] = m.high[b][a] = None
                    if not m.water[b][a]: m.blocked[b][a] = False
            m.stamps.remove(st)
    for yy in range(y0, y0 + H):
        for xx in range(x0, x0 + W):
            if m.obj[yy][xx] is not None: m.obj[yy][xx] = None; m.blocked[yy][xx] = False
            m.reserved[yy][xx] = True
    if not hasattr(m, '_claimed'): m._claimed = set()
    m._claimed |= {(x0 + a, y0 + b) for a in range(-1, W + 1) for b in range(-1, H + 1)}
    return (x0, y0, W, H)

def place_snowball_arena(m, key, mine):
    if key not in SNOW_ARENA: return None
    taken = {(o['x'], o['y']) for o in m.objects} | {(sp[1], sp[2]) for sp in m.spots}
    def free(x, y): return (m.inside(x, y) and mine[y, x] and not m.blocked[y][x] and not m.water[y][x] and m.road[y][x] == 0
                            and m.obj[y][x] is None and m.high[y][x] is None and (x, y) not in taken)
    cx, cy = SNOW_ARENA[key]
    r_ = _free_rect(m, mine, ((16, 11), (14, 10), (12, 9)), cx, cy)
    if not r_: return None
    x0, y0, W, H = r_; sx, sy = W / 16, H / 11
    walls = []
    for (a, b) in SNOW_WALLS:
        a, b = int(a * sx), int(b * sy)
        for k in range(2):
            m.prop(K('nature', 10, 12), x0 + a + k, y0 + b); walls.append((x0 + a + k, y0 + b))   # 눈더미
    kids = [(x0 + int(a * sx), y0 + int(b * sy)) for (a, b) in SNOW_KIDS]
    for yy in range(y0, y0 + H):   # 놀이터 안은 다른 자동 배치(쉼터·풍경·사람)가 쓰지 않게
        for xx in range(x0, x0 + W): m.reserved[yy][xx] = True
    flags = []
    for (fx, fy) in ((x0 - 1, y0 - 2), (x0 + W, y0 - 2), (x0 - 1, y0 + H - 1), (x0 + W, y0 + H - 1)):   # 귀퉁이 바깥 깃발(1×2)
        if m.inside(fx, fy) and m.inside(fx, fy + 1) and all(free(fx, fy + j) for j in (0, 1)) and m.stamp('banner', fx, fy, force=True):
            flags.append((fx, fy))
    m._snow = dict(arena=[x0, y0, x0 + W - 1, y0 + H - 1], walls=walls, kids=kids)
    return (x0, y0, W, H, len(flags))

# ── 2026-10-30(사용자 기획): 룬돌 언덕 — 순례자 야영지(동)·음유시인 공연 자리(북)·돌탑 언덕(서남)·짐 수레 쉼터(서) ──
#   사람·돌탑은 런타임 NorthRunehill.cs가 그린다(자리만 NorthLife.json: pilgrims, bard, bardListeners, cairn, cairnDeco, cairnTower)
RUNE_SPOTS = {'camp': (76, 40), 'stage': (40, 27), 'cairn': (28, 53), 'cart': (22, 42)}
def place_runehill(m, key, mine):
    if key != 'north_runehill': return None
    out = {}; R = {}
    def st(n, x, y):
        return m.stamp(n, x, y, force=True)
    r = _free_rect(m, mine, ((10, 8), (9, 8), (9, 7)), *RUNE_SPOTS['camp'])
    if r:
        x0, y0, W, H = r
        st('tent', x0 + 1, y0); st('tent', x0 + W - 4, y0)
        st('fire', x0 + W // 2 - 1, y0 + 3)
        st('sled', x0, y0 + 4); st('crate', x0 + W - 1, y0 + 4)
        R['pilgrims'] = [[x0 + W // 2, y0 + H - 1], [x0 + 1, y0 + 4], [x0 + W - 2, y0 + 5]]   # 첫째가 이름표 달린 순례자
        out['camp'] = r
    r = _free_rect(m, mine, ((9, 7), (8, 7), (8, 6)), *RUNE_SPOTS['stage'])
    if r:
        x0, y0, W, H = r
        st('fire', x0 + 3, y0 + 2)
        st('logbench', x0 + 2, y0 + H - 1)
        R['bard'] = [x0 + 7, y0 + 3]
        R['bardListeners'] = [[x0 + 1, y0 + 2], [x0 + 2, y0 + 4], [x0 + 5, y0 + 5]]
        out['stage'] = r
        # 지도에 원래 있던 음유시인(말만 하는 사람)은 런타임 공연자로 바꾼다
        m.objects = [o for o in m.objects if not (o['kind'] == 'talker' and o.get('name') == '음유시인')]
    r = _free_rect(m, mine, ((8, 6), (7, 6), (7, 5)), *RUNE_SPOTS['cairn'])
    if r:
        x0, y0, W, H = r
        R['cairn'] = [x0 + W // 2, y0 + H - 1]          # 돌탑 쌓는 자리(Space)
        R['cairnTower'] = [x0 + W // 2, y0 + H - 3]     # 내 돌탑(최고 기록 높이로 서 있음)
        R['cairnDeco'] = [[x0 + 1, y0 + 1], [x0 + W - 2, y0 + 1], [x0 + 1, y0 + H - 2], [x0 + W - 2, y0 + H - 3]]
        out['cairn'] = r
    r = _free_rect(m, mine, ((5, 4), (4, 3)), *RUNE_SPOTS['cart'])
    if r:
        x0, y0, W, H = r
        st('sled', x0, y0 + 1); st('crate', x0 + 2, y0 + 1); st('barrel_s', x0 + 3, y0 + 2); st('logs', x0, y0 + H - 1)
        out['cart'] = r
    # 10/30(사용자: '룬돌 고리의 중심'에 아무것도 없음): 고리 중심을 룬돌들이 실제로 둥글게 둘러선 곳으로 옮기고, 런타임이 그 자리에 룬 새긴 돌판을 그린다
    stones = [(int(x), int(y)) for (n_, x, y) in m.stamps if n_.startswith(('runestone', 'picturestone'))]
    best = None
    for cy in range(5, m.h - 5):
        for cx in range(5, m.w - 5):
            d = [((sx - cx) ** 2 + (sy - cy) ** 2) ** 0.5 for (sx, sy) in stones]
            ring_n = sum(1 for v in d if 4 <= v <= 9); inner = sum(1 for v in d if v < 3)
            sc = ring_n - 3 * inner
            if best is None or sc > best[0]: best = (sc, cx, cy)
    if best and best[0] >= 5:
        _, cx, cy = best
        cell = min(((x, y) for y in range(cy - 3, cy + 4) for x in range(cx - 3, cx + 4)
                    if m.inside(x, y) and mine[y, x] and not m.blocked[y][x] and not m.water[y][x] and m.obj[y][x] is None and m.high[y][x] is None),
                   key=lambda c: (c[0] - cx) ** 2 + (c[1] - cy) ** 2, default=None)
        if cell:
            m.spots = [sp for sp in m.spots if sp[0] != 'ring'] + [('ring', cell[0], cell[1])]
            out['ring'] = (cell, best[0])
    m._rune = R
    return out

# ── 2026-10-30(사용자 기획): 북쪽 길목 마을 — 남쪽 대상 야영지(남서쪽 공터) + 북쪽 눈밭 짐 장식 ──
#   다른 지역 그림(줄무늬 천막·낙타·상자·옷걸이·항아리·통·수레)은 런타임 NorthCaravan.cs가 그리고, 생성기는 자리와 막힘만 정한다.
#   caravanDeco: [종류, x, y(그림 맨 아랫줄), ...]  종류 0 빨간 천막 1 파란 천막 2 상자 더미 3 천 걸이 4 풀 뜯는 순록 5 선 순록 6 통 7 항아리 8 수레 9 짐 썰매
#   (10/30 사용자: 낙타는 북유럽에 안 맞음 → 국경에서 낙타를 맡기고 빌린 순록·짐 썰매)
CARAVAN_DECO = {0: (2, (0, 1)), 1: (2, (0, 1)), 2: (2, (0,)), 3: (2, (0,)), 4: (2, (0,)), 5: (2, (0,)), 6: (1, (0,)), 7: (1, (0,)), 8: (2, (0,)), 9: (2, (0,))}   # 10/30 천막·상자·천 걸이는 작게(런타임 0.7~0.8배)   # 폭, 막는 줄(아래에서 위로 0,1..)
def place_caravan(m, key, mine):
    if key != 'north_c_crossing': return None
    out = {}; deco = []
    def put(kind, x, bottom):
        w, rows = CARAVAN_DECO[kind]
        for a in range(w):
            for r in rows:
                if m.inside(x + a, bottom - r): m.blocked[bottom - r][x + a] = True; m.reserved[bottom - r][x + a] = True
        deco.extend([kind, x, bottom])
    r = _free_rect(m, mine, ((12, 8), (12, 6), (11, 6)), 26, 69)
    if r:
        x0, y0, W, H = r
        # 위 3줄: 빨간 천막 · 상자 더미 · 천 걸이 · 파란 천막 / 가운데: 모닥불, 앉은 낙타·선 낙타 / 맨 아래 줄: 상인 셋(이름표는 길잡이만)
        put(0, x0 + 1, y0 + 2); put(1, x0 + W - 3, y0 + 2)
        put(2, x0 + 4, y0 + 2); put(3, x0 + W - 6, y0 + 2)
        m.stamp('fire', x0 + W // 2 - 1, y0 + 3, force=True)
        put(9, x0 + W - 4, y0 + 3)   # 10/30 사용자: 야영지 순록은 뺌(짐 썰매만)
        if H >= 8: put(6, x0 + 3, y0 + 5); put(7, x0 + W - 4, y0 + 5)
        yb = y0 + H - 1
        m._caravan = dict(merchants=[x0 + W - 2, yb, x0 + 1, yb, x0 + W // 2 - 2, yb])   # 순서: 길잡이(이름표) · 낙타 상인 · 유리 상인
        out['camp'] = r
    # 북쪽 눈밭(광장 위): 대상이 묶어 둔 짐 — 수레·풀 뜯는 순록·짐 썰매·상자 듬성듬성
    for (cx, cy, what) in ((30, 32, 'cart'), (44, 36, 'camel'), (36, 29, 'sled'), (50, 30, 'crate')):
        rr = _free_rect(m, mine, ((4, 3), (3, 3)), cx, cy)
        if not rr: continue
        x0, y0, W, H = rr
        if what == 'cart': put(8, x0 + 1, y0 + 1)
        elif what == 'camel': put(4, x0 + 1, y0 + 1)
        elif what == 'sled': m.stamp('sled', x0 + 1, y0 + 1, force=True); m.stamp('crate', x0 + 3, y0 + 1, force=True) if W > 3 else None
        else: m.stamp('crate', x0 + 1, y0 + 1, force=True); m.stamp('barrel_s', x0 + 2, y0 + 1, force=True)
        out[what] = rr
    if hasattr(m, '_caravan'): m._caravan['deco'] = deco
    else: m._caravan = dict(merchants=[], deco=deco)
    return out

# ── 2026-10-30(사용자: 북쪽 길목 — 잉그리드 앞 집을 빼고 길로 / 사람 없는 좌판은 다 치움) ──
HOUSE_STAMPS = {'h_turf', 'h_shed', 'h_dark', 'h_round', 'h_dome', 'h_stoneA', 'h_stoneB', 'woodhouse', 'new_smithy', 'new_kiln'}
def fix_stalls(m, mine):
    """장터 좌판: 장꾼이 서는 좌판만 남기고 빈 좌판은 뺀다. 장꾼 말 거는 자리 바로 앞(아래 2줄)을 막은 집은 빼고, 그 자리에서 길까지 골목을 낸다.
    m.finish() 전에 돌려야 새 골목이 길 그림으로 그려진다"""
    import sys as _s; _s.path.insert(0, G)
    from north_spots import keeper_spots
    kp = keeper_spots(m); ks = m._keeper_stalls
    keep = {(ks[i] - 1, ks[i + 1] - 1) for i in range(0, len(ks), 2)}
    out = dict(stallsRemoved=[], housesRemoved=[], paths=0)
    def clear_stamp(st):
        n_, X, Y = st; w_, h_ = tiles.STAMPS[n_][3], tiles.STAMPS[n_][4]
        for a in range(w_):
            for b in range(h_):
                if m.inside(X + a, Y + b):
                    m.obj[Y + b][X + a] = m.high[Y + b][X + a] = None
                    if not m.water[Y + b][X + a]: m.blocked[Y + b][X + a] = False
                    m.reserved[Y + b][X + a] = True
        m.stamps.remove(st)
    # (빈 좌판 빼기는 사람 정리 뒤 마지막에 한 번만: 여기서 빼면 장꾼 자리 계산이 달라져 장꾼이 줄었다)
    # 장꾼이 좌판 바로 앞에 못 서고 옆으로 밀려난 경우(앞을 집이 막음): 그 집을 빼고 좌판 앞에서 길까지 골목
    for i in range(0, len(ks), 2):
        sx, sy = ks[i] - 1, ks[i + 1] - 1
        f = (sx + 1, sy + 2)
        if i + 1 < len(kp) and (kp[i], kp[i + 1]) == f: continue
        hit_house = False
        for st in [t for t in m.stamps if t[0] in HOUSE_STAMPS and t[0] in tiles.STAMPS]:
            n_, X, Y = st; w_, h_ = tiles.STAMPS[n_][3], tiles.STAMPS[n_][4]
            if any((X + a, Y + b) in {(f[0] + c, f[1] + d) for c in (-1, 0, 1) for d in (0, 1)} for a in range(w_) for b in range(h_)):
                clear_stamp(st); out['housesRemoved'].append((n_, int(X), int(Y))); hit_house = True
        if not hit_house: continue
        prev = {f: None}; dq = deque([f]); hit = None
        while dq:
            c = dq.popleft()
            if m.road[c[1]][c[0]] > 0 and c != f: hit = c; break
            for a, b in ((0, 1), (1, 0), (-1, 0), (0, -1)):
                n = (c[0] + a, c[1] + b)
                if n in prev or not m.inside(*n) or not mine[n[1], n[0]] or m.blocked[n[1]][n[0]] or m.water[n[1]][n[0]]: continue
                if abs(n[0] - f[0]) + abs(n[1] - f[1]) > 10: continue
                prev[n] = c; dq.append(n)
        c = hit
        while c is not None:
            if m.road[c[1]][c[0]] == 0: m.road[c[1]][c[0]] = 1; m.reserved[c[1]][c[0]] = True; out['paths'] += 1
            c = prev[c]
    return out

# ── 2026-10-30(사용자 기획): 라우가르 서북쪽 — 꿀벌 겨울 오두막 ──
#   벌통·짚단 더미·양봉가는 런타임 NorthBees.cs가 그린다(자리만: beeHives, beeStraw, beeKeeper). 오두막은 판자 헛간(h_shed)
def place_bees(m, key, mine):
    if key != 'north_laugar': return None
    r = _free_rect(m, mine, ((10, 6), (9, 6)), 22, 33)
    if not r: return None
    x0, y0, W, H = r
    m.stamp('h_shed', x0, y0, force=True)
    hives = [(x0 + 4, y0 + 1), (x0 + 6, y0 + 1), (x0 + 8, y0 + 1), (x0 + 5, y0 + 3), (x0 + 7, y0 + 3)]
    hives = [h for h in hives if h[0] < x0 + W]
    for (x, y) in hives: m.blocked[y][x] = True; m.reserved[y][x] = True
    straw = (x0 + 1, y0 + 4)
    for a in range(2): m.blocked[straw[1]][straw[0] + a] = True
    m._bees = dict(beeHives=[v for h in hives for v in h], beeStraw=list(straw), beeKeeper=[x0 + 5, y0 + H - 1])
    return r

# ── 2026-10-30(사용자 기획): 남은 빈 곳 넷 ──
#   얼음 동굴 골짜기 서쪽 물가: 군나르의 잃어버린 짐 3개(런타임 NorthErrands.cs) — gunnar, gunnarPacks, gunnarHint(0 물가·1 나무 사이·2 바위 뒤)
#   개울 나루: 장작 패는 나무꾼 + 모닥불 쉼터 — woodcutter, woodBlock, woodFire
#   눈숲 고갯길: 고개 쉼터(천막·모닥불·나그네 둘, 말만 함)
#   북문 마을: 국경 경비 훈련장(짚 허수아비 셋은 런타임 그림, 경비병 둘은 말만 함) — dummies
def place_extras(m, key, mine):
    out = {}
    if key == 'north_icecave':
        r = _free_rect(m, mine, ((6, 4), (5, 4)), 20, 36)
        if r:
            x0, y0, W, H = r
            m.stamp('sled', x0 + 1, y0 + 1, force=True)
            g = (x0 + 3, y0 + 2)
            reach = _reach(m, mine)
            def kind(x, y):
                near = lambda f, rr=2: any(m.inside(x + a, y + b) and f(x + a, y + b) for a in range(-rr, rr + 1) for b in range(-rr, rr + 1))
                if near(lambda a, b: m.water[b][a]): return 0
                if near(lambda a, b: m.high[b][a] is not None and str(m.high[b][a]).startswith('nature')): return 1
                if near(lambda a, b: m.blocked[b][a] and m.obj[b][a] is not None, 1): return 2
                return -1
            cands = [(x, y) for (x, y) in reach if mine[y, x] and m.road[y][x] == 0 and m.obj[y][x] is None and m.high[y][x] is None
                     and not any(m.inside(x + a, y + b) and m.road[y + b][x + a] for a in range(-3, 4) for b in range(-3, 4))
                     and abs(x - g[0]) + abs(y - g[1]) >= 14 and kind(x, y) >= 0 and (x, y) not in getattr(m, '_claimed', set())]
            packs = []
            rng_ = random.Random(4242)
            rng_.shuffle(cands)
            for want in (2, 0, 1):   # 바위 뒤 · 물가 · 나무 사이 하나씩(없으면 아무거나)
                pool = [c for c in cands if kind(*c) == want and all(abs(c[0] - p[0]) + abs(c[1] - p[1]) >= 16 for p in packs)] or \
                       [c for c in cands if all(abs(c[0] - p[0]) + abs(c[1] - p[1]) >= 16 for p in packs)]
                pool = [c for c in pool if abs(c[0] - g[0]) + abs(c[1] - g[1]) <= 42]   # 골짜기 안쪽에 고루(너무 멀리 몰리지 않게)
                if pool:
                    c = max(pool, key=lambda c: min([abs(c[0] - q[0]) + abs(c[1] - q[1]) for q in packs + [g]]))
                    packs.append(c)
            m._extras = dict(gunnar=list(g), gunnarPacks=[v for p in packs for v in p], gunnarHint=[kind(*p) for p in packs])
            out['gunnar'] = (g, packs)
    elif key == 'north_c_creek':
        r = _free_rect(m, mine, ((8, 5), (7, 5)), 58, 48)
        if r:
            x0, y0, W, H = r
            m.stamp('logs', x0, y0, force=True); m.stamp('logs_birch', x0 + 2, y0, force=True)
            m.stamp('stump', x0 + 1, y0 + 2, force=True)               # 장작 패는 그루터기
            m.stamp('fire', x0 + W - 3, y0 + 1, force=True)
            m.stamp('logbench', x0 + W - 4, y0 + H - 1, force=True)
            m._extras = dict(woodcutter=[x0 + 2, y0 + 3], woodBlock=[x0 + 1, y0 + 2], woodFire=[x0 + W - 2, y0 + 2])
            out['wood'] = r
    elif key == 'north_c_road_nw':
        r = _free_rect(m, mine, ((9, 6), (8, 6)), 20, 45)
        if r:
            x0, y0, W, H = r
            m.stamp('tent', x0, y0, force=True)
            m.stamp('fire', x0 + 4, y0 + 2, force=True)
            m.stamp('logbench', x0 + 3, y0 + 4, force=True)
            m.stamp('sled', x0 + W - 2, y0, force=True)
            m.talker(x0 + W - 2, y0 + 3, '고개 넘는 나그네 비게', 'north_p_trader_tall',
                     '이 고개만 넘으면 눈숲 마을이라오. 불 좀 쬐고 가시게.', '밤엔 고갯길이 얼어 미끄럽다네. 해 있을 때 넘는 게 좋아.')
            m.talker(x0 + 1, y0 + H - 1, '털모자 쓴 길잡이 아스타', 'north_p_girl_tall',
                     '북문 마을은 고개 아래 동쪽, 눈숲 마을은 서쪽이에요.', '천막은 아무나 쉬어 가라고 쳐 둔 거예요. 모닥불도 꺼뜨리지 말아 주세요!')
            out['rest'] = r
    elif key == 'north_c_gatetown':
        r = _free_rect(m, mine, ((9, 5), (8, 5)), 52, 42)
        if r:
            x0, y0, W, H = r
            dummies = [(x0 + 1, y0 + 1), (x0 + 4, y0 + 1), (x0 + 7, y0 + 1)]
            dummies = [d for d in dummies if d[0] < x0 + W]
            for (x, y) in dummies: m.blocked[y][x] = True; m.reserved[y][x] = True
            m.stamp('weaponrack', x0 + W - 2, y0 + 3, force=True)
            m.talker(x0 + 1, y0 + 3, '경비병 하랄', 'north_p_shield_tall',
                     '하나, 둘! 찌르기! …아, 손님이군. 북문은 남쪽 땅으로 가는 마지막 관문이오.', '룬돌 큰길로 가면 북쪽 길목, 거기서 남쪽 끝이 네 지역이 만나는 초원이오.')
            m.talker(x0 + 6, y0 + 3, '경비병 시그문드', 'north_wolf_viking_tall',
                     '허수아비가 너무 튼튼해서 창이 휘겠어. 훈련은 매일 해야 하지.', '비르카 쪽 길은 북쪽, 눈숲 고갯길은 서쪽이야. 길 잃지 말라고.')
            m._extras = dict(dummies=[v for d in dummies for v in d])
            out['guards'] = r
    return out

def build(key):
    i = KEYS.index(key); mm = MID == i
    ys, xs = np.nonzero(mm); y0, y1, x0, x1 = ys.min() - 3, ys.max() + 4, xs.min() - 3, xs.max() + 4   # 둘레 3칸은 옆 땅을 보여 줌
    Wd, Hd = x1 - x0, y1 - y0
    m = nt.SnowMap(key, Z[key]['name'].split(' (')[0], Wd, Hd, seed=100 + i)
    L = lambda wy, wx: (wx - X0 - x0, wy - Y0 - y0)       # 세계 → 맵 칸
    sub = T_[y0:y1, x0:x1]; mine = mm[y0:y1, x0:x1]; road = ROADW[y0:y1, x0:x1]
    # 1) 물·길
    springs = []
    # 2026-10-29(사용자): 1칸짜리 비스듬한 개울이 네모 칸처럼 끊겨 보여서, 2×2 물 덩어리에 속하지 않는 개울 칸은 오른쪽·아래 칸까지 물로 넓힌다
    riv = (sub == RIVER).copy(); land_ok = ~np.isin(sub, [DEEP, SHALLOW, RIVER, LAKE, SPRING, VOID]) & ~road
    no_widen = np.zeros_like(riv)
    for it in Z[key]['items']:
        if 'x' in it and not it.get('missing'):
            x_, y_ = it['x'] - X0 - x0, it['y'] - Y0 - y0; no_widen[max(0, y_ - 1):max(0, y_ + it['h'] + 1), max(0, x_ - 1):max(0, x_ + it['w'] + 1)] = True
    def corridor(s_):
        """출입구 객체의 칸 + (출구면) 경계까지 되돌아오는 복도 칸. 맵 칸 좌표 (x, y)"""
        cy_, cx_ = s_['cell']; sx_, sy_ = cx_ - x0, cy_ - y0
        cells_ = {(sx_ + a, sy_ + b) for a in range(s_['w']) for b in range(s_['h'])}
        if s_['kind'] == 'exit':
            dy_, dx_ = _dir(s_['edge']); px_, py_ = (0, 1) if dy_ else (1, 0)
            cells_ |= {(x_ - dx_ * j, y_ - dy_ * j) for (x_, y_) in list(cells_) for j in range(0, s_.get('out', 0) + 1)}
            cells_ |= {(x_ + px_ * k_, y_ + py_ * k_) for (x_, y_) in list(cells_) for k_ in (-CORRIDOR_PAD, CORRIDOR_PAD)}   # 양옆 여유 → 넓게 트인 통로
        return cells_
    for s_ in SEAMS.get(key, []):
        for (cx_, cy_) in corridor(s_):
            if 0 <= cy_ < Hd and 0 <= cx_ < Wd: no_widen[cy_, cx_] = True
    wet = np.isin(sub, [DEEP, SHALLOW, RIVER, LAKE])
    def in2(yy, xx):
        return any(all(0 <= yy + b + j < Hd and 0 <= xx + a + i < Wd and wet[yy + b + j, xx + a + i] for i in (0, 1) for j in (0, 1)) for a in (-1, 0) for b in (-1, 0))
    widened = []
    for yy, xx in zip(*np.nonzero(riv)):
        if in2(yy, xx): continue
        for (ny, nx) in ((yy, xx + 1), (yy + 1, xx), (yy + 1, xx + 1)):
            if 0 <= ny < Hd and 0 <= nx < Wd and land_ok[ny, nx] and not no_widen[ny, nx]: widened.append((ny, nx))
    sub = sub.copy()
    for (ny, nx) in widened: sub[ny, nx] = RIVER
    # 길이 지나는 개울 칸은 다리(아래 다리 단계)로: 넓힌 칸 옆 길 칸도 다리 폭에 넣는다
    road = road.copy()
    for yy in range(Hd):
        for xx in range(Wd):
            t = sub[yy, xx]
            if t in (DEEP, SHALLOW, RIVER, LAKE) and not road[yy, xx]: m.water[yy][xx] = True; m.reserved[yy][xx] = True
            if t == SPRING: springs.append((xx, yy))
            if road[yy, xx] and t not in (DEEP, SHALLOW): m.road[yy][xx] = max(m.road[yy][xx], 1); m.reserved[yy][xx] = True
    if springs:
        for (xx, yy) in springs: m.water[yy][xx] = True; m.reserved[yy][xx] = True
        m._springs |= set(springs)
        inner = [c for c in springs if all((c[0] + a, c[1] + b) in m._springs for a in (-1, 0, 1) for b in (-1, 0, 1))]
        for (xx, yy) in random.Random(i).sample(inner, min(4, len(inner))):
            m.objects.append(dict(kind='anim', x=xx, y=yy, sheet='north_steam', frames=4, fps=3.3))
    # 다리: 강 위 길 덩어리마다 판자 다리
    br = road & (sub == RIVER); seen = set()
    for yy, xx in zip(*np.nonzero(br)):
        if (yy, xx) in seen: continue
        q = [(yy, xx)]; comp = []
        while q:
            c = q.pop()
            if c in seen or not (0 <= c[0] < Hd and 0 <= c[1] < Wd) or not br[c]: continue
            seen.add(c); comp.append(c); q += [(c[0] + a, c[1] + b) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))]
        cy = [c[0] for c in comp]; cx = [c[1] for c in comp]
        m.bridge(min(cx), min(cy), max(cx), max(cy))
    # 2) 맵 밖 칸: 그림은 그대로 두고 막는다(경계 출입구로만 넘어감)
    for yy, xx in zip(*np.nonzero(~mine)): m.blocked[yy][xx] = True; m.reserved[yy][xx] = True
    # 2026-10-29(사용자): 출구는 길 끝 — 경계~출구 복도(담당 밖)는 처음부터 열어 두고(길), 갈 수 있는지 검사도 복도까지 본다(mine2)
    mine2 = mine.copy()
    for s_ in SEAMS.get(key, []):
        for (cx_, cy_) in corridor(s_):
            if not m.inside(cx_, cy_) or m.water[cy_][cx_]: continue
            mine2[cy_, cx_] = True
            if not mine[cy_, cx_]: m.blocked[cy_][cx_] = False; m.reserved[cy_][cx_] = True   # 통로 바닥은 눈밭(가운데 다진 길은 ROADW/stub가 그린다)
    # 2026-10-29(사용자: 숲에 막힌 길): 담당 밖(울타리가 될 곳)에 닿고 끝나는 길 꼬리는 지운다. 복도·마을·광장은 두고, 꼬리 10칸까지
    corr_all = set()
    for s_ in SEAMS.get(key, []): corr_all |= corridor(s_)
    # 2026-10-29(사용자: 출구 복도가 숲 터널): 담당 밖 복도 양옆 ROAD_CLEAR칸은 눈밭으로 열고, ROAD_THIN칸까지는 울타리 나무를 절반만
    _cm = np.zeros((Hd, Wd), bool)
    for (cx_, cy_) in corr_all:
        if 0 <= cy_ < Hd and 0 <= cx_ < Wd: _cm[cy_, cx_] = True
    cdist = np.full((Hd, Wd), 99, int); cdist[_cm] = 0; _cur = _cm.copy()
    for d_ in range(1, ROAD_THIN + 1):
        _nx = _cur.copy()
        _nx[1:, :] |= _cur[:-1, :]; _nx[:-1, :] |= _cur[1:, :]; _nx[:, 1:] |= _nx[:, :-1].copy(); _nx[:, :-1] |= _nx[:, 1:].copy()
        cdist[_nx & ~_cur] = d_; _cur = _nx
    _out_ok = ~mine & (sub != VOID) & ~np.isin(sub, [DEEP, SHALLOW, RIVER, LAKE])
    # 2026-10-29(사용자: 눈숲 마을→포도밭 언덕이 숲 벽): 다른 지역 출구 — 너머(다른 지역 땅)는 숲 벽 대신 그 지역 바닥·소품을 보여 주고 길을 잇는다
    reg_open = set(); reg_thin = set(); reg_road = set(); REG = []
    for lk in LINKS:
        if lk['a']['map'] != key: continue
        e = next((e for e in N1R['exits'] if e['map'] == key and e['label'].startswith(lk['id'])), None)
        if not e: continue
        b = lk['b']; edge = lk['a']['edge']; n = b['h'] if edge in 'EW' and b['edge'] in 'EW' else b['w'] if b['edge'] in 'NS' else b['h']
        ax, ay = L(e['at'][1], e['at'][0]); dy_, dx_ = _dir(edge)
        look = REGION_LOOK.get(b['map'].split('_')[0], REGION_LOOK['transit'])
        zr = random.Random(sum(map(ord, lk['id'])) + 17 * i)
        # 출구에서 바깥쪽으로 긴 타원(앞 9칸·옆 5칸 남짓), 가장자리는 출렁. 안쪽(담당 땅) 3칸까지 풀이 번진다
        import math
        ph = [zr.uniform(0, 6.283) for _ in range(3)]
        zone = []
        # 2026-10-30(사용자: 길 끝까지 가야 넘어감): 길은 맵 끝까지. 끝 4칸은 상대 출구 폭(n) 그대로
        K_end = ax if dx_ < 0 else (Wd - 1 - ax) if dx_ > 0 else ay if dy_ < 0 else (Hd - 1 - ay)
        def _wob(k): return round(1.1 * math.sin(k / 3.5 + ph[0]) * min(1.0, (k - 2) / 4.0)) if k > 2 else 0
        c_end = _wob(K_end - 4)
        yr = range(0 if dy_ < 0 else max(0, ay - 18), Hd if dy_ > 0 else min(Hd, ay + 19))
        xr = range(0 if dx_ < 0 else max(0, ax - 18), Wd if dx_ > 0 else min(Wd, ax + 19))
        for y_ in yr:
            for x_ in xr:
                ddx, ddy = x_ - ax, y_ - ay
                k_ = ddx * dx_ + ddy * dy_; t_ = ddx * (1 - abs(dx_)) + ddy * (1 - abs(dy_))
                th = math.atan2(t_, k_ - 6)
                wob = 1 + 0.18 * math.sin(2 * th + ph[0]) + 0.12 * math.sin(3 * th + ph[1]) + 0.08 * math.sin(5 * th + ph[2])
                e_ = math.sqrt(((k_ - 6) / 10.0) ** 2 + (t_ / (n / 2 + 4.0)) ** 2) / wob
                if k_ <= 2: lane = (-(n // 2) <= t_ < n - n // 2)   # 출구 바로 앞은 출구 폭 그대로
                elif k_ >= K_end - 3: lane = (c_end - n // 2 <= t_ < c_end + n - n // 2)   # 맵 끝 4칸: 출구 폭 그대로
                else:
                    c_ = _wob(k_)   # 그 너머는 3칸 폭으로 살짝 굽이침
                    lane = (c_ - 1 <= t_ <= c_ + 1)
                if lane and k_ >= 0: reg_road.add((x_, y_))   # (10/30) 경계 칸(담당 안)도 길로 — 출구 자리가 옮겨져 끊기지 않게   # 길: 출구에서 맵 끝까지
                if mine[y_, x_]:
                    if e_ <= 1.0 and k_ >= -3: zone.append((x_, y_, k_, t_, e_, True))   # 담당 땅으로 번지는 풀(걸을 수 있음)
                    continue
                if e_ <= 1.0: reg_open.add((x_, y_)); zone.append((x_, y_, k_, t_, e_, False))
                elif e_ <= 1.35 and zr.random() < 0.6: reg_thin.add((x_, y_))
        REG.append(dict(id=lk['id'], b=b['map'], name=REGION_NAMES.get(b['map'], b['map']), ax=ax, ay=ay, dx=dx_, dy=dy_, n=n, look=look, zone=zone, K=K_end, cend=c_end))
    m._REG = REG
    # 길 양옆 1칸은 걸을 수 있게(reg_lane), 2~3칸은 덤불·바위(reg_side) — 나무 벽 터널·투명 벽 없이
    _rr = np.zeros((Hd, Wd), bool)
    for (x_, y_) in reg_road: _rr[y_, x_] = True
    def _dil(a, r):
        o = a.copy()
        for _ in range(r):
            n_ = o.copy(); n_[1:, :] |= o[:-1, :]; n_[:-1, :] |= o[1:, :]; n_[:, 1:] |= n_[:, :-1].copy(); n_[:, :-1] |= n_[:, 1:].copy(); o = n_
        return o
    _d1 = _dil(_rr, 1) & ~mine; _d3 = _dil(_rr, 3) & ~mine
    reg_lane = {(xx, yy) for yy, xx in zip(*np.nonzero(_d1))}
    reg_walk = reg_lane | {c for c in reg_open if not mine[c[1], c[0]]}   # 2026-10-30(사용자: 잔디에서 못 움직임): 출구 너머 그 지역 땅(풀·흙 타원)도 걸을 수 있게
    reg_side = {(xx, yy) for yy, xx in zip(*np.nonzero(_d3 & ~_d1))}
    _crng = random.Random(3500 + i)
    corr_band = {(xx, yy) for yy, xx in zip(*np.nonzero(_out_ok & (cdist >= 1) & (cdist <= ROAD_CLEAR)))}
    corr_thin = {(xx, yy) for yy, xx in zip(*np.nonzero(_out_ok & (cdist > ROAD_CLEAR) & (cdist <= ROAD_THIN))) if _crng.random() < 0.5}
    def roadcell(x_, y_): return m.inside(x_, y_) and m.road[y_][x_] > 0 and not m.water[y_][x_]
    def wide(x_, y_): return sum(1 for a in (-1, 0, 1) for b in (-1, 0, 1) if roadcell(x_ + a, y_ + b)) >= 8
    def near_vil(x_, y_): return any(0 <= y_ + b < Hd and 0 <= x_ + a < Wd and VIL[y_ + b + y0, x_ + a + x0] for a in (-2, -1, 0, 1, 2) for b in (-2, -1, 0, 1, 2))
    tails = set()
    for y in range(Hd):
        for x in range(Wd):
            if not roadcell(x, y) or not mine[y, x] or (x, y) in corr_all or near_vil(x, y) or wide(x, y): continue
            if not any(m.inside(x + a, y + b) and not mine2[y + b, x + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))): continue
            dq_ = deque([((x, y), 0)]); seen_ = {(x, y)}
            while dq_:
                c, d0 = dq_.popleft(); tails.add(c)
                if d0 >= 10: continue
                for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    n_ = (c[0] + a, c[1] + b)
                    if n_ in seen_ or not roadcell(*n_) or not mine[n_[1], n_[0]] or n_ in corr_all or near_vil(*n_) or wide(*n_): continue
                    seen_.add(n_); dq_.append((n_, d0 + 1))
    for (x, y) in tails: m.road[y][x] = 0; road[y, x] = False; m.reserved[y][x] = False
    m._tails = len(tails)
    for s_ in SEAMS.get(key, []):   # 출구 너머(맵 가장자리까지)의 길은 지워서 길이 출구에서 끝나 보이게. 울타리가 덮는다
        if s_['kind'] != 'exit': continue
        dy_, dx_ = _dir(s_['edge']); px_, py_ = (0, 1) if dy_ else (1, 0); cy_, cx_ = s_['cell']; sx_, sy_ = cx_ - x0, cy_ - y0
        for a in range(-2, s_['w'] + 2) if dy_ else (0,):
            for b in range(-2, s_['h'] + 2) if dx_ else (0,):
                for j in range(1, 40):
                    x_, y_ = sx_ + a + dx_ * j, sy_ + b + dy_ * j
                    if not m.inside(x_, y_): break
                    if mine[y_, x_]: continue
                    m.road[y_][x_] = 0; road[y_, x_] = False; m.reserved[y_][x_] = True
    # 3) 자리 (광장·마당·쉼터 = 다진 길 바닥, 부두 = 판자)
    items = [it for it in Z[key]['items'] if not it.get('missing') and 'x' in it]
    items = [dict(it) for it in items] + [dict(e) for e in EXTRA_ITEMS.get(key, [])]
    for it in items:
        mv = MOVE_LOTS.get((key, it['name']))
        if mv:
            dx_, dy_ = mv[0] - it['x'], mv[1] - it['y']; it['x'], it['y'] = mv
            if it.get('door'): it['door'] = [it['door'][0] + dx_, it['door'][1] + dy_]
    if key in EXTRA_PATHS:
        pts = [L(wy, wx) for (wx, wy) in EXTRA_PATHS[key]]
        m.road_path(pts, width=2, grade=1)
        for yy in range(Hd):
            for xx in range(Wd):
                if m.road[yy][xx] > 0: m.reserved[yy][xx] = True
    for it in items:
        x, y = L(it['y'], it['x'])
        if it['kind'] in ('plaza', 'yard', 'rest'): m.road_rect(x, y, x + it['w'] - 1, y + it['h'] - 1, grade=1)
        if it['kind'] == 'harbor': m.boardwalk(x, y, x + it['w'] - 1, y + it['h'] - 1)
    # 4) 절벽(산·빙하·바위 해안): 칸마다 윗면/절벽면을 고르는 눈 절벽
    # 2026-10-29(사용자): 절벽은 전부 없앤다. 산·빙하 칸은 절벽 타일 없이 눈밭 위 바위·눈 나무 덩어리(막힘)로 아래 6)에서 심는다
    mtn = np.isin(sub, [MOUNTAIN, PEAK]) & ~road
    cliff = np.zeros_like(mtn)
    # 2026-10-29(사용자: 옆 마을로 넘어가면 건물에 걸려 못 움직임): 경계 출입구·도착 칸(둘레 1칸)에 걸치는 건물·소품 자리는
    # 겹치지 않는 가장 가까운 곳(6칸 안, 물·맵 밖 아닌 곳)으로 민다
    edge_cells = set()
    for s_ in SEAMS.get(key, []):
        cy_, cx_ = s_['cell']; sx_, sy_ = cx_ - x0, cy_ - y0
        edge_cells |= {(sx_ + a, sy_ + b) for a in range(-1, s_['w'] + 1) for b in range(-1, s_['h'] + 1)}
    lot_shift = []
    for it in items:
        if it['kind'] not in ('lot', 'spot', 'npc', 'lantern') or 'x' not in it: continue
        x, y = L(it['y'], it['x']); w, h = it['w'], it['h']
        cells = lambda X, Y: [(X + a, Y + b) for a in range(w) for b in range(h + (1 if it['kind'] == 'lot' else 0))]
        if not any(c in edge_cells for c in cells(x, y)): continue
        best = None
        for r in range(1, 7):
            for dy in range(-r, r + 1):
                for dx in range(-r, r + 1):
                    if max(abs(dx), abs(dy)) != r: continue
                    cs = cells(x + dx, y + dy)
                    if all(0 <= cx < Wd and 0 <= cy < Hd and mine[cy, cx] and not m.water[cy][cx] and c not in edge_cells for c in cs for (cx, cy) in [c]):
                        if best is None or abs(dx) + abs(dy) < best[0]: best = (abs(dx) + abs(dy), dx, dy)
            if best: break
        if best:
            _, dx, dy = best; it['x'] += dx; it['y'] += dy
            if it.get('door'): it['door'] = [it['door'][0] + dx, it['door'][1] + dy]
            lot_shift.append((it['name'], dx, dy))
    # 5) 건물·소품
    m._items = items
    lots = []
    for it in items:
        a = it.get('art'); x, y = L(it['y'], it['x'])
        if not a or a.startswith('Minigame:'): continue
        name = 'new_' + a.split(':')[1].split('_')[0] if a.startswith('new:') else a
        if name == 'laundry': m.laundry(x, y); continue
        if it['kind'] in ('lot', 'cliff'):
            if m.stamp(name, x, y, force=True, over_road=True): lots.append(it); it['_stamp'] = (name, x, y)
        else: m.deco_stamp(name, x, y)
    for it in lots:
        if it.get('door'):
            dx, dy = L(it['door'][1], it['door'][0])
            # 문 앞에서 가장 가까운 길까지 골목(N2 골목과 같은 칸 수 안)
            q = deque([(dx, dy)]); prev = {(dx, dy): None}; hit = None
            while q:
                c = q.popleft()
                if m.inside(*c) and m.road[c[1]][c[0]] > 0: hit = c; break
                if len(prev) > 600: break
                for a_, b_ in ((0, 1), (1, 0), (-1, 0), (0, -1)):
                    n = (c[0] + a_, c[1] + b_)
                    if n not in prev and m.inside(*n) and not m.blocked[n[1]][n[0]] and not m.water[n[1]][n[0]]: prev[n] = c; q.append(n)
            c = prev.get(hit) if hit else None
            while c is not None: m.road[c[1]][c[0]] = max(m.road[c[1]][c[0]], 1); c = prev[c]
            if m.inside(dx, dy) and not m.blocked[dy][dx]: m.road[dy][dx] = max(m.road[dy][dx], 1)
    report = dict(talkerNoOld=[], npc=0, talker=0)
    report['lotShift'] = lot_shift
    # N5: 옛 맵의 꾸밈·생활 자리를 새 장터(쉼터) 기준으로 옮긴다
    def center_of(name_pred):
        for it in items:
            if name_pred(it): x_, y_ = L(it['y'], it['x']); return (x_ + it['w'] // 2, y_ + it['h'] // 2)
        return None
    anchor = (center_of(lambda it: it['kind'] == 'plaza') or center_of(lambda it: it['kind'] == 'rest')
              or tuple(int(v) for v in np.argwhere(mine).mean(0)[::-1]))
    for it in items:   # 없어진 맵 내용이 옮겨 온 쉼터
        if it['kind'] == 'rest' and 'from' in it:
            x_, y_ = L(it['y'], it['x']); n5.MERGED_ANCHOR[it['from']] = (x_ + it['w'] // 2, y_ + it['h'] // 2)
    n5.LAKE_CELLS[key] = {(xx, yy) for yy, xx in zip(*np.nonzero(sub == LAKE))}
    taken = np.zeros((Hd, Wd), bool)
    for it in items:
        x, y = L(it['y'], it['x']); taken[max(0, y - 1):y + it['h'] + 1, max(0, x - 1):x + it['w'] + 1] = True
    # 2026-10-29(갇힘): 경계 출입구·도착 칸과 둘레 1칸에는 소품·나무를 두지 않는다(도착하자마자 천막 속에 갇히던 문제)
    for s in SEAMS.get(key, []):
        for (cx, cy) in corridor(s):
            taken[max(0, cy - 1):cy + 2, max(0, cx - 1):cx + 2] = True
    # 2026-10-29: 옛 소품은 숲·산·다른 지역 땅에 놓지 않고, N2 자리(사람·게시판·집) 둘레 1칸도 비운다
    n5.EXTRA_BLOCK[key] = (np.isin(sub, [FOREST, MOUNTAIN, PEAK, VOID]) | taken).copy()
    n5.HARD_BLOCK[key] = (np.isin(sub, [VOID]) | taken).copy()
    report['n5'] = n5.transfer(m, key, mine, anchor, rng_seed=i)
    report['anchor'] = anchor
    P5 = n5.LAST_PLACER[key]
    # 2026-10-29: 집 옆이 비어 있으면 생활 소품(통·상자·장작 등)을 1~2개, 장터 모서리에 등 기둥
    # 2026-10-29(사용자): 피오르 나루 좌판은 덕장(공방 마당 yard_fish) 바로 위 → 그 6줄에 옮겨 온 소품은 빼서 비워 둔다
    m._stall_area = None
    yf = [(x_, y_) for (k_, x_, y_) in m.spots if k_ == 'yard_fish']
    if key == 'north_naru' and yf:
        ax0, ay0, ax1, ay1 = yf[0][0] - 1, yf[0][1] - 7, yf[0][0] + 12, yf[0][1] - 1
        m._stall_area = (ax0, ay0, ax1, ay1); drop = []
        for f_ in P5.foot:
            t_, n_, X, Y, w_, h_ = f_
            if t_ == 'stamp' and ((X + w_ > ax0 and X <= ax1 and Y + h_ > ay0 and Y <= ay1) or n_ == 'stall'):   # 옛 좌판 그림(장꾼 자리)도 빼서 장꾼이 덕장 위 좌판에 서게
                for a in range(w_):
                    for b in range(h_):
                        if m.inside(X + a, Y + b): m.obj[Y + b][X + a] = m.high[Y + b][X + a] = None; m.blocked[Y + b][X + a] = False; m.reserved[Y + b][X + a] = False
                m.stamps = [st for st in m.stamps if st != (n_, X, Y)]; drop.append(f_)
        P5.foot = [f_ for f_ in P5.foot if f_ not in drop]; report['stallAreaDropped'] = [f_[1] for f_ in drop]
    report['crafts'] = place_crafts(m, items, L, mine, P5)
    report['dressed'] = dress_village(m, key, items, lots, L, mine, sub, P5, random.Random(500 + i))
    # 비워 둘 곳: 놓은 소품·N2 자리 둘레 2칸에는 나무·바위를 심지 않는다
    keep = np.zeros((Hd, Wd), bool)
    for (t_, n_, X, Y, w_, h_) in P5.foot:
        keep[max(0, Y - 2):Y + h_ + 2, max(0, X - 2):X + w_ + 2] = True
    for it in items:
        if it['kind'] in ('lot',): continue
        x, y = L(it['y'], it['x']); keep[max(0, y - 2):y + it['h'] + 2, max(0, x - 2):x + it['w'] + 2] = True
    for it in lots:
        x, y = L(it['y'], it['x']); keep[max(0, y - 1):y + it['h'] + 2, max(0, x - 1):x + it['w'] + 1] = True
    if m._stall_area: keep[max(0, m._stall_area[1]):m._stall_area[3] + 1, max(0, m._stall_area[0]):m._stall_area[2] + 1] = True
    # 2026-10-29(사용자: 길이 너무 넓음): 마을 밖 3칸 폭 길은 2칸으로(가로 3칸 줄은 왼쪽, 세로 3칸 줄은 위쪽 칸을 뺀다). 출구 복도·광장(4칸 이상)은 그대로
    def _rd(x_, y_): return m.inside(x_, y_) and m.road[y_][x_] > 0
    keep_r = set(corr_all)
    nar = []
    for y in range(1, Hd - 1):
        for x in range(1, Wd - 1):
            if not _rd(x, y) or (x, y) in keep_r or VIL[y + y0, x + x0] or not mine[y, x] or m.water[y][x]: continue
            if not _rd(x - 1, y) and _rd(x + 1, y) and _rd(x + 2, y) and not _rd(x + 3, y): nar.append((x, y))
            elif not _rd(x, y - 1) and _rd(x, y + 1) and _rd(x, y + 2) and not _rd(x, y + 3): nar.append((x, y))
    for (x, y) in nar: m.road[y][x] = 0; m.reserved[y][x] = False
    road = road.copy()
    for (x, y) in nar: road[y, x] = False
    report['narrowed'] = len(nar)
    # 6) 숲(길·자리·물에서 1칸 띄움). 심을 칸을 먼저 모은 뒤, 갈 수 없는 자리가 생기지 않게 길을 내고 심는다
    pending = []   # (칸 집합, 나무 종류, 비울 수 있나)
    forest = set()
    # 2026-10-29(사용자: 길 양옆 숲 벽이 터널 같음): 길에서 ROAD_CLEAR칸까지는 비우고, ROAD_THIN칸까지는 절반만 심는다
    _rm = road | (np.array(m.road) > 0)
    rdist = np.full((Hd, Wd), 99, int); rdist[_rm] = 0; _cur = _rm.copy()
    for d_ in range(1, ROAD_THIN + 1):
        _nx = _cur.copy()
        _nx[1:, :] |= _cur[:-1, :]; _nx[:-1, :] |= _cur[1:, :]; _nx[:, 1:] |= _nx[:, :-1].copy(); _nx[:, :-1] |= _nx[:, 1:].copy()
        rdist[_nx & ~_cur] = d_; _cur = _nx
    band_rng = random.Random(3000 + i); band_cells = set(); fnoise = _vnoise(6100 + i, 7)
    for yy, xx in zip(*np.nonzero((sub == FOREST) & ~road)):
        if taken[yy, xx] or keep[yy, xx]: continue
        if rdist[yy, xx] <= ROAD_CLEAR:
            if rdist[yy, xx] >= 2 and mine[yy, xx]: band_cells.add((xx, yy))
            continue
        if rdist[yy, xx] <= ROAD_THIN and band_rng.random() < 0.5: continue
        if fnoise(xx, yy) > FOREST_KEEP: continue   # 숲 사이 눈밭(덩어리)
        if m.obj[yy][xx] or m.high[yy][xx] or m.water[yy][xx] or not mine[yy, xx]: continue
        forest.add((xx, yy))
    north = not key.startswith('north_c_')
    pending.append((forest, nt.DENSE_KINDS['pine' if north else 'mixed'], True))
    # 2026-10-29(사용자: 다른 지역 숲 벽이 계단처럼 딱딱함): 담당 땅에서 4칸 안 다른 지역 땅은 3×3 덩어리 단위로 듬성하게(가장자리가 들쭉날쭉)
    _mn = mine.copy(); vdist = np.full((Hd, Wd), 99, int); vdist[_mn] = 0
    for d_ in range(1, 7):
        _nx = _mn.copy(); _nx[1:, :] |= _mn[:-1, :]; _nx[:-1, :] |= _mn[1:, :]; _nx[:, 1:] |= _nx[:, :-1].copy(); _nx[:, :-1] |= _nx[:, 1:].copy()
        vdist[_nx & ~_mn] = d_; _mn = _nx
    vr = random.Random(5000 + i); blk = {}
    def blkv(x_, y_):
        k_ = (x_ // 3, y_ // 3)
        if k_ not in blk: blk[k_] = vr.random()
        return blk[k_]
    EDGE_KINDS = ['snowpine', 'snowround', 'snowboulder', 'snowpine_g', 'rock', 'snowround', 'deadtree', 'snowpine']   # 숲 가장자리: 덤불·바위·작은 나무 섞기(빈 막힌 칸은 만들지 않음)
    void_edge = set(); void_cells = set()
    for yy, xx in zip(*np.nonzero(sub == VOID)):
        (void_edge if 3 <= vdist[yy, xx] <= 4 else void_cells).add((xx, yy))   # 1~2칸은 틈 없는 숲, 3~4칸은 섞기, 그 너머는 아래에서 언덕으로
    pending.append((void_edge, EDGE_KINDS, False))
    pending.append((void_cells, nt.DENSE_KINDS['pine' if north else 'mixed'], False))   # 다른 지역 땅: 숲 벽
    # 담당 밖 북유럽 땅의 숲·산도 그림으로(막힌 칸)
    pending.append(({(xx, yy) for yy, xx in zip(*np.nonzero(np.isin(sub, [FOREST, MOUNTAIN, PEAK]) & ~mine & ~road))}, nt.DENSE_KINDS['pine' if north else 'mixed'], False))
    # 2026-10-29(사용자): 경계 울타리 — 담당 밖(옆 북유럽 맵 땅) 전부 눈 나무 숲. 출입구 길 틈은 비운다
    near = ~mine   # 담당 밖 전부(둘레 3칸 + 비스듬한 경계의 모서리). 3칸 띠만 심으면 듬성해서 벽으로 안 보였다
    gap = set()
    for s_ in SEAMS.get(key, []):   # 출입구 통로 칸만 비움(나무 벽이 통로 바로 옆에 붙어 포켓몬 길처럼 보이게)
        gap |= corridor(s_)
    fence = {(xx, yy) for yy, xx in zip(*np.nonzero(near & ~mine & (sub != VOID) & ~wet)) if (xx, yy) not in gap}   # 옆 맵 길·광장도 덮는다(복도 틈만 남김)
    for (xx, yy) in fence: m.road[yy][xx] = 0
    pending.append((fence, nt.DENSE_KINDS['pine' if north else 'mixed'], False)); report['fence'] = len(fence)
    pending.append((set(reg_thin) - reg_open, EDGE_KINDS, False))   # 다른 지역 출구 덩어리 둘레: 덤불·바위로 서서히
    pending.append((set(corr_thin), EDGE_KINDS, False))   # 복도 옆 바깥 띠: 덤불·바위·작은 나무
    pending.append((set(reg_side) - reg_open, EDGE_KINDS, False))   # 다른 지역 길 옆 2~3칸: 덤불·바위
    fence -= reg_side
    gap = gap | corr_band | reg_open | reg_lane   # 복도 옆 띠·다른 지역 출구 너머도 나무 없이
    fence -= corr_band | corr_thin
    pending = [(mask - gap if not cl else mask, kinds, cl) for (mask, kinds, cl) in pending]
    # 2026-10-29(사용자: 바깥 숲이 너무 많음): 담당 땅에서 5칸 넘게 떨어진 바깥 땅은 숲 대신 눈 덮인 언덕(절벽 윗면) + 드문드문 나무 덩어리
    pnoise = _vnoise(6600 + i, 5); plateau = set(); _np = []
    for (mask, kinds, cl) in pending:
        if cl: _np.append((mask, kinds, cl)); continue
        keep_ = set()
        for c in mask:
            x_, y_ = c
            if 0 <= y_ < Hd and 0 <= x_ < Wd and not mine[y_, x_] and vdist[y_, x_] >= 5 and pnoise(x_, y_) > 0.13 and c not in gap and c not in reg_side: plateau.add(c)
            else: keep_.add(c)
        _np.append((keep_, kinds, cl))
    pending = _np
    report['plateau'] = len(plateau)
    # (위 줄까지 2026-10-29 추가) 못 비우는 숲(담당 밖 숲·다른 지역 벽)도 출입구 복도는 비워 둔다
    # 산·빙하: 절벽 대신 바위·눈 바위·눈 나무 덩어리
    mtn_cells = {(xx, yy) for yy, xx in zip(*np.nonzero(mtn & mine)) if not keep[yy, xx] and not taken[yy, xx] and m.obj[yy][xx] is None and m.high[yy][xx] is None
                 and not m.water[yy][xx] and m.road[yy][xx] == 0 and not m.reserved[yy][xx]}
    # 2026-10-29(사용자: 얼음 동굴→룬돌 언덕 길이 터널): 산 지형도 길 양옆 ROAD_CLEAR칸은 비우고 ROAD_THIN칸까지 절반만
    band_cells |= {c for c in mtn_cells if 2 <= rdist[c[1], c[0]] <= ROAD_CLEAR}
    mtn_cells = {c for c in mtn_cells if rdist[c[1], c[0]] > ROAD_CLEAR and not (rdist[c[1], c[0]] <= ROAD_THIN and band_rng.random() < 0.5) and fnoise(c[0] + 50, c[1] + 50) <= MTN_KEEP}
    pending.append((mtn_cells, ['rock', 'snowboulder', 'rock', 'snowboulder', 'snowpine', 'snowpine_g', 'snowround', 'deadtree'], True))
    # 2026-10-29(사용자: 길이 심심함): 비운 길가 띠에 작은 장식을 듬성듬성(7칸 간격)
    deco_rng = random.Random(4000 + i); deco_pts = []; DECO = ['snowboulder', 'rocksmall', 'stump', 'snowround', 'rock', 'logs', 'stumpbig', 'deadtree']
    for (xx, yy) in sorted(band_cells, key=lambda c: (deco_rng.random(),)):
        if any(abs(xx - a) <= 7 and abs(yy - b) <= 7 for a, b in deco_pts): continue
        if m.blocked[yy][xx] or m.reserved[yy][xx] or m.water[yy][xx] or m.obj[yy][xx] or m.high[yy][xx]: continue
        nm_ = deco_rng.choice(DECO)
        if m.stamp(nm_, xx, yy): deco_pts.append((xx, yy))
    report['bandDeco'] = len(deco_pts)
    # 2026-10-29: 빈 눈밭에 작은 숲 (마을·길·소품에서 떨어진 곳만)
    rn = random.Random(1000 + i)
    plan = set().union(*[p[0] for p in pending])
    def open_(x, y):
        return (m.inside(x, y) and mine[y, x] and not m.blocked[y][x] and not m.water[y][x] and m.road[y][x] == 0 and (x, y) not in plan
                and not m.reserved[y][x] and m.obj[y][x] is None and m.high[y][x] is None and not VIL[y + y0, x + x0] and not keep[y, x])
    def far_road(x, y, d):
        return not any(m.inside(x + a, y + b) and (m.road[y + b][x + a] or taken[y + b, x + a] or keep[y + b, x + a]) for a in range(-d, d + 1) for b in range(-d, d + 1))
    opens = [(x, y) for y in range(Hd) for x in range(Wd) if open_(x, y)]
    rn.shuffle(opens); centers = []
    real_plant = m._plant_dense
    m._plant_dense = lambda mask, kinds: pending.append((set(mask), kinds, True))   # grove()가 심는 칸을 모으기만
    for (x, y) in opens:
        if len(centers) >= len(opens) // 260: break
        if far_road(x, y, 4) and all(abs(x - a) + abs(y - b) > 12 for a, b in centers):
            centers.append((x, y)); m.grove(x, y, rn.uniform(2, 4.5), rn.uniform(1.5, 3.5), 'pine' if north else rn.choice(['mixed', 'pine', 'tundra']))
    # 2026-10-29(사용자): 아무것도 없는 넓은 눈밭: 무엇에서든 6칸 넘게 떨어진 곳에 9칸 간격으로
    # 마을 밖은 작은 숲, 마을 안은 눈 바위·그루터기 같은 작은 것 몇 개
    plan_ = set().union(*[p[0] for p in pending])
    dist = np.full((Hd, Wd), 99, int); q = deque()
    for yy in range(Hd):
        for xx in range(Wd):
            if not mine[yy, xx] or m.blocked[yy][xx] or m.water[yy][xx] or m.road[yy][xx] or m.obj[yy][xx] is not None or m.high[yy][xx] is not None or (xx, yy) in plan_:
                dist[yy, xx] = 0; q.append((yy, xx))
    while q:
        yy, xx = q.popleft()
        for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = yy + b, xx + a
            if 0 <= ny < Hd and 0 <= nx < Wd and dist[ny, nx] > dist[yy, xx] + 1: dist[ny, nx] = dist[yy, xx] + 1; q.append((ny, nx))
    empt = [(xx, yy) for yy, xx in zip(*np.nonzero(dist >= 5))]; rn.shuffle(empt); fills = []; small = []
    n_pend = len(pending)
    for (xx, yy) in sorted(empt, key=lambda c: -dist[c[1], c[0]]):
        if taken[yy, xx]: continue
        if any(abs(xx - a) + abs(yy - b) < 8 for a, b in fills): continue
        fills.append((xx, yy))
        if VIL[yy + y0, xx + x0]: small.append((xx, yy))
        else: m.grove(xx, yy, rn.uniform(1.5, 3), rn.uniform(1.2, 2.5), 'pine' if north else rn.choice(['mixed', 'pine', 'tundra']))
    for p_ in pending[n_pend:]: p_[0].difference_update({c for c in p_[0] if keep[c[1], c[0]] or taken[c[1], c[0]]})   # 소품·자리 둘레는 비움
    report['filled'] = len(fills); report['distmax'] = int(dist[mine].max()) if mine.any() else 0
    if os.environ.get('DBG'): print('dist', report['distmax'], 'zeros', int((dist == 0).sum()), 'mine', int(mine.sum()), 'keep', int((keep & mine).sum()), 'taken', int((taken & mine).sum()), 'plan', len([c for c in plan_ if mine[c[1], c[0]]]))
    del m._plant_dense
    for (xx, yy) in small:   # 마을 안 빈 눈밭: 눈 바위·그루터기 1개 + 눈 더미 2~3개
        name = rn.choice(['snowboulder', 'stumpbig', 'rock', 'rocksmall', 'snowpost'])
        _, _, _, w_, h_, *_ = tiles.STAMPS[name]
        if all(m.inside(xx + a, yy + b) and not m.blocked[yy + b][xx + a] and m.obj[yy + b][xx + a] is None and m.road[yy + b][xx + a] == 0 for a in range(w_) for b in range(h_)):
            m.stamp(name, xx, yy)
        m.snow_props([c for c in ((xx + rn.randint(-3, 3), yy + rn.randint(-2, 3)) for _ in range(rn.randint(2, 3))) if m.inside(*c) and mine[c[1], c[0]] and not taken[c[1], c[0]] and not keep[c[1], c[0]] and m.road[c[1]][c[0]] == 0])
    # 갈 수 없는 자리 검사: 길에서 걸어서 닿는지. 막혔으면 사이 나무·바위 칸을 비워 2칸 폭 길을 낸다
    m._edge_targets = []
    for s in SEAMS.get(key, []):
        cy, cx = s['cell']; ex, ey = cx - x0, cy - y0
        cells = {(ex + a, ey + b) for a in range(s['w']) for b in range(s['h'])}
        m._edge_targets.append((f"{s['kind']}→{s.get('target') or s.get('id')}", {c for c in cells if 0 <= c[0] < Wd and 0 <= c[1] < Hd and mine2[c[1], c[0]]}))
    for e in N1R['exits']:
        if e['map'] == key: ex, ey = L(e['at'][1], e['at'][0]); m._edge_targets.append(('지역출구 ' + e['label'], {(ex + a, ey + b) for a in (-2, -1, 0, 1, 2) for b in (-2, -1, 0, 1, 2)}))
    targets = reach_targets(m, items, L, P5)
    # 2026-10-29(사용자: 눈숲 고갯길로 가면 갇힘): 경계 출입구·도착 자리도 길과 이어지게
    for s in SEAMS.get(key, []):
        targets.append(('경계 ' + s.get('target', s.get('id', '')), corridor(s)))
    report['opened'] = open_paths(m, mine2, pending, targets)
    # 2026-10-29(사용자: 길 옆이 숲 터널): 심기 직전 길(지금 깔린 길 전부)에서 ROAD_CLEAR칸 안은 치울 수 있는 숲·덤불숲 모두 비운다
    _rm2 = np.array(m.road) > 0; rd2 = np.full((Hd, Wd), 99, int); rd2[_rm2] = 0; _cur = _rm2.copy()
    for d_ in range(1, ROAD_CLEAR + 1):
        _nx = _cur.copy(); _nx[1:, :] |= _cur[:-1, :]; _nx[:-1, :] |= _cur[1:, :]; _nx[:, 1:] |= _nx[:, :-1].copy(); _nx[:, :-1] |= _nx[:, 1:].copy()
        rd2[_nx & ~_cur] = d_; _cur = _nx
    for (mask, kinds, cl) in pending:
        real_plant({c for c in mask if m.inside(*c) and not (cl and mine[c[1], c[0]] and rd2[c[1], c[0]] <= ROAD_CLEAR)}, kinds)
    plan = set().union(*[p[0] for p in pending])
    props = [(x, y) for (x, y) in opens if open_(x, y) and (x, y) not in plan and far_road(x, y, 2)]
    m.snow_props(rn.sample(props, min(len(props), len(opens) // 70)))
    # 2026-10-29(갇힘): 눈 더미(1칸 소품)가 좁은 목을 막아 주머니 땅이 생기면, 막은 눈 더미만 치운다
    report['unplug'] = unplug_props(m, mine2, targets)
    tuft_ok = [(x, y) for (x, y) in opens if open_(x, y) and (x, y) not in plan]
    m._late.extend(((x, y), rn.choice(nt.TUFTS)) for (x, y) in rn.sample(tuft_ok, min(len(tuft_ok), len(opens) // 18)))
    # 7) 바닥 조각: 밭·자갈은 흙, 늪은 녹은 풀
    # 2026-10-29(사용자): 밭이 큰 갈색 흙 덩어리로 보여서 흙 바닥은 깔지 않는다(눈 덮인 밭)
    m.ground_patch([(xx, yy) for yy, xx in zip(*np.nonzero((sub == BOG) & ~road & mine)) if not m.blocked[yy][xx]], 'grass')
    for R_ in REG:   # 다른 지역 땅: 출구 너머 타원 + 담당 땅 3칸까지 번지는 그 지역 바닥. 길 옆 1칸은 눈으로 남겨 길 가장자리가 보이게
        lane = lambda x_, y_: (x_, y_) in reg_road or m.road[y_][x_] > 0   # 풀은 길 바로 옆까지(빈 눈 줄 없이)
        cells_ = [(x_, y_) for (x_, y_, k_, t_, e_, inn) in R_['zone']
                  if not lane(x_, y_) and m.obj[y_][x_] is None and m.high[y_][x_] is None and not m.water[y_][x_] and (not inn or (not m.blocked[y_][x_] and e_ <= 1.0 - 0.12 * (k_ + 3)))]
        m.ground_patch(cells_, R_['look'][0])
    for (x_, y_) in reg_road:
        if not m.water[y_][x_]: m.road[y_][x_] = max(m.road[y_][x_], 1); m.reserved[y_][x_] = True
    report['frontStrip'] = front_strip(m, lots, mine, L)
    # 8) 사람·장터·게시판·이벤트·도적
    for it in items:
        x, y = L(it['y'], it['x']); k = it['kind']
        if k == 'plaza' and it.get('id', '').startswith('market:'):
            mx, my = x + it['w'] // 2, y + it['h'] // 2; m.add('market', mx, my, town=it['id'].split(':', 1)[1])
            m.add('spawn', mx, my + 1, id='S0') if key == 'north_birka' else None
        elif it['name'] == '게시판' and it.get('id', '').startswith('board:'): m.board(x, y, it['id'].split(':', 1)[1])
        elif k == 'npc' and it.get('id') and not it.get('memo', '').startswith('talker'):
            m.add('npc', x, y, npc=it['id']); report['npc'] += 1
        elif k == 'npc':
            if (key, it['name']) in DROP_TALKERS: continue
            o = old_talker(key, it['name'])
            if o:
                ln_ = list(o.get('lines', []))
                for i_, t_ in enumerate(TALKER_LINES.get((key, it['name']), [])): ln_[i_:i_ + 1] = [t_]
                m.talker(x, y, it['name'], o.get('character', ''), *ln_)
            else: m.talker(x, y, it['name'], 'north_p_trader_tall', '...'); report['talkerNoOld'].append(it['name'])
            report['talker'] += 1
        elif k == 'event': m.event(x, y)
        elif k == 'bandit': m.bandit(x, y)
    # 9) 출입구(경계 전부)와 다른 지역 출구
    for s in SEAMS.get(key, []):
        cy, cx = s['cell']; x, y = cx - x0, cy - y0
        if s['kind'] == 'exit': m.objects.append(dict(kind='exit', x=x, y=y, w=s['w'], h=s['h'], target=s['target'], spawn=s['spawn'], edge=s['edge']))
        else: m.objects.append(dict(kind='spawn', x=x, y=y, w=s['w'], h=s['h'], id=s['id'], edge=s['edge']))
    for e in N1R['exits']:
        if e['map'] != key: continue
        x, y = L(e['at'][1], e['at'][0]); report.setdefault('regionExits', []).append(dict(label=e['label'], x=x, y=y))
    # 2026-10-29(사용자: 길이 출구 앞에서 끊김): 출구 칸에서 안쪽으로 곧게 12칸 안에 길이 있으면 그 사이를 길로 잇는다
    report['exitRoad'] = 0
    for o in [o for o in m.objects if o['kind'] == 'exit']:
        dy_, dx_ = _dir(o['edge'])
        for a in range(o['w']):
            for b in range(o['h']):
                x_, y_ = o['x'] + a, o['y'] + b; path = []
                for k in range(13):
                    cx_, cy_ = x_ - dx_ * k, y_ - dy_ * k
                    if not m.inside(cx_, cy_) or m.water[cy_][cx_] or str(m.high[cy_][cx_] or '').startswith(('house', 'northbuild')): path = None; break
                    if k > 0 and m.road[cy_][cx_] > 0: break
                    path.append((cx_, cy_))
                else: path = None
                if not path: continue
                for (cx_, cy_) in path:
                    if m.road[cy_][cx_] > 0: continue
                    m.road[cy_][cx_] = 1; m.blocked[cy_][cx_] = False; m.obj[cy_][cx_] = m.high[cy_][cx_] = None; m.reserved[cy_][cx_] = True; report['exitRoad'] += 1
                ps = set(path); m.objects = [q for q in m.objects if q['kind'] in ('exit', 'spawn') or (q['x'], q['y']) not in ps]
    for R_ in REG:   # 다른 지역 출구도: 출구 폭 그대로 안쪽으로 곧게 가다 길을 만나면(25칸 안) 그 사이를 길로
        for t_ in range(-(R_['n'] // 2), R_['n'] - R_['n'] // 2):
            x_ = R_['ax'] + (t_ if R_['dy'] else 0); y_ = R_['ay'] + (t_ if R_['dx'] else 0); path = []
            for k in range(26):
                cx_, cy_ = x_ - R_['dx'] * k, y_ - R_['dy'] * k
                if not m.inside(cx_, cy_) or m.water[cy_][cx_] or str(m.high[cy_][cx_] or '').startswith(('house', 'northbuild')): path = None; break
                if k > 0 and m.road[cy_][cx_] > 0: break
                path.append((cx_, cy_))
            else: path = None
            for (cx_, cy_) in (path or []):
                m.road[cy_][cx_] = max(1, m.road[cy_][cx_]); m.blocked[cy_][cx_] = not mine[cy_, cx_]; m.obj[cy_][cx_] = m.high[cy_][cx_] = None; m.reserved[cy_][cx_] = True; report['exitRoad'] += 1
    report['signs'] = []
    report['tails'] = m._tails
    report['roads'] = dress_roads(m, key, mine, mine2, x0, y0, random.Random(7000 + i))
    m.box = (x0, y0); m.mine = mine
    # 런타임이 그리는 미니게임 그림(덕장·천막 좌판 등) 자리: 좌판 찾을 때 피함
    m._mg_rects = [(L(it['y'], it['x'])[0], L(it['y'], it['x'])[1], it['w'], it['h']) for it in items if (it.get('art') or '').startswith('Minigame:')]
    # 2026-10-29(사용자): 피오르 나루 좌판은 대구 덕장 위쪽에
    #   (실제로 보이는 덕장은 공방 마당 yard_fish 그림. 구역은 n5 옮기기 직후에 정함)
    # 2026-10-29(갇힘): 마무리의 '물 홈 메우기'가 넓힌 개울 옆 땅을 물로 바꿔 경계 칸을 가두던 문제 → 길 홈만 메운다
    def _smooth_road_only(passes=2, _m=m):
        for _ in range(passes):
            add = []
            for y in range(1, _m.h - 1):
                for x in range(1, _m.w - 1):
                    if _m.blocked[y][x] or _m.obj[y][x] is not None or _m.road[y][x] > 0 or _m.water[y][x] or _m.ground[y][x] is not None: continue
                    nb_ = [_m.road[y + dy][x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dx or dy]; on = [v for v in nb_ if v]
                    n4 = [_m.road[y - 1][x], _m.road[y + 1][x], _m.road[y][x - 1], _m.road[y][x + 1]]
                    if len(on) >= 5 or (n4[0] and n4[1]) or (n4[2] and n4[3]): add.append((x, y, max(on)))
            for x, y, v in add: _m.road[y][x] = v; _m.reserved[y][x] = True
    m.smooth = _smooth_road_only
    for (x_, y_) in plateau:   # 바깥 언덕 윗면(막힌 칸)
        if m.inside(x_, y_) and m.obj[y_][x_] is None and m.high[y_][x_] is None and not m.water[y_][x_] and m.road[y_][x_] == 0:
            m.ground[y_][x_] = K('relief', 2, 1); m.blocked[y_][x_] = True; m.reserved[y_][x_] = True
    # 2026-10-29(사용자: 넓은데 빈 맵): 무엇에서든 8칸 넘게 떨어진 넓은 눈밭에 쉼터(모닥불)·작은 풍경·말 거는 사람을 하나씩
    rr = random.Random(7700 + i); report['rest'] = []
    walk_ = lambda x_, y_: m.inside(x_, y_) and mine[y_, x_] and not m.blocked[y_][x_] and not m.water[y_][x_] and m.road[y_][x_] == 0 and m.obj[y_][x_] is None and m.high[y_][x_] is None
    busy = np.zeros((Hd, Wd), bool)
    for o in m.objects: busy[max(0, o['y'] - 1):o['y'] + 2, max(0, o['x'] - 1):o['x'] + 2] = True
    for (nm_, X, Y) in m.stamps: busy[max(0, Y):Y + 2, max(0, X):X + 2] = True
    for (k_, X, Y) in m.spots: busy[max(0, Y - 1):Y + 2, max(0, X - 1):X + 2] = True
    bd = np.full((Hd, Wd), 99, int); bd[busy] = 0; _cur = busy.copy()
    for d_ in range(1, 12):
        _nx = _cur.copy(); _nx[1:, :] |= _cur[:-1, :]; _nx[:-1, :] |= _cur[1:, :]; _nx[:, 1:] |= _nx[:, :-1].copy(); _nx[:, :-1] |= _nx[:, 1:].copy()
        bd[_nx & ~_cur] = d_; _cur = _nx
    cands = sorted(((int(bd[y_, x_]), x_, y_) for y_ in range(3, Hd - 4) for x_ in range(3, Wd - 4) if walk_(x_, y_) and bd[y_, x_] >= 6
                    and all(walk_(x_ + a, y_ + b) for a in (-2, -1, 0, 1, 2, 3) for b in (-1, 0, 1, 2))), reverse=True)
    n_walk = int((mine & ~np.array(m.blocked)).sum()); want = 1 + (n_walk > 1800) + (n_walk > 2600)
    picks = []
    for (d_, x_, y_) in cands:
        if len(picks) >= want: break
        if any(abs(x_ - a) + abs(y_ - b) < 18 for a, b in picks): continue
        picks.append((x_, y_))
    for j_, (x_, y_) in enumerate(picks):
        kind_ = ['rest', 'talker', 'scene'][(i + j_) % 3]
        if kind_ == 'rest' and m.stamp('fire', x_, y_, force=True):
            m.stamp('logbench', x_ - 1, y_ + 2, force=True); m.stamp('logs', x_ + 3, y_, force=True)
        elif kind_ == 'talker':
            pick_ = REST_PICK.get(key, REST_TALKERS[(KEYS.index(key) * 5 + j_ * 3) % len(REST_TALKERS)])
            if pick_: m.talker(x_, y_, pick_[0], pick_[1], *pick_[2])
            m.stamp('fire', x_ + 2, y_, force=True)
        else:
            for (a, b, st_) in ((0, 0, 'logs'), (3, 0, 'stump'), (1, 2, 'crate_berry'), (-2, 1, 'lantern')):
                m.stamp(st_, x_ + a, y_ + b, force=True)
        report['rest'].append((kind_, x_, y_))
    for (fx_, fy_) in EXTRA_REST.get(key, {}).get('fires', []): m.stamp('fire', fx_, fy_, force=True)   # 의자 자리는 stamp가 같이 남김
    for (tx_, ty_, nm_, ch_, ln_) in EXTRA_REST.get(key, {}).get('talkers', []): m.talker(tx_, ty_, nm_, ch_, *ln_)
    fix_region_patch(m, key)
    apply_exit_roads(m, key)
    for (sx_, sy_, si_) in list(m.secrets):
        if (key, si_) in SECRET_MOVE:
            m.high[sy_][sx_] = None; m.obj[sy_ + 1][sx_] = None; m.blocked[sy_ + 1][sx_] = False
            m.secrets.remove((sx_, sy_, si_)); m.secret_rune(si_, *SECRET_MOVE[(key, si_)])
    for o_ in m.objects:
        if o_.get('kind') == 'talker' and (key, o_.get('name')) in TALKER_TO_NPC: o_['kind'] = 'npc'; o_['npc'] = TALKER_TO_NPC[(key, o_['name'])]
    report['stallFix'] = fix_stalls(m, mine)
    m.finish()
    # 2026-10-29(사용자): 모닥불 의자가 길 바로 옆이면 길을 막아서 뺀다(라우가르 불 쬐기 오른쪽 의자)
    m.spots = [sp for sp in m.spots if not (sp[0] == 'seat' and any(m.inside(sp[1] + a, sp[2] + b) and m.road[sp[2] + b][sp[1] + a] > 0 for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))))]
    # 사람이 건물 지붕 위에 서 보이지 않게: 발밑 아래 칸이 막혀 있거나(건물 바로 위) 그 칸에 그림이 있으면 가까운 빈 칸으로
    taken_ = {(o['x'], o['y']) for o in m.objects}
    def free_(x, y):
        return (m.inside(x, y) and m.inside(x, y + 1) and mine[y, x] and not m.blocked[y][x] and not m.blocked[y + 1][x] and not m.water[y][x]
                and m.obj[y][x] is None and m.high[y][x] is None and m.road[y][x] == 0 and (x, y) not in taken_)
    for o in m.objects:
        if o['kind'] not in ('talker', 'npc') or free_(o['x'], o['y']): continue
        best = None
        for r in range(1, 7):
            for yy in range(o['y'] - r, o['y'] + r + 1):
                for xx in range(o['x'] - r, o['x'] + r + 1):
                    if max(abs(xx - o['x']), abs(yy - o['y'])) == r and free_(xx, yy):
                        near_road = any(m.inside(xx + a, yy + b) and m.road[yy + b][xx + a] for a in (-1, 0, 1) for b in (-1, 0, 1))
                        k_ = (0 if near_road else 1, abs(xx - o['x']) + abs(yy - o['y']))
                        if best is None or k_ < best[0]: best = (k_, xx, yy)
            if best: break
        if best: taken_.discard((o['x'], o['y'])); o['x'], o['y'] = best[1], best[2]; taken_.add((o['x'], o['y']))
    # 담당 밖 칸은 고려처럼 그리지 않고 막는다(경계 출입구로만 옆 맵으로)
    # 2026-10-29 바꿈: 잘려 보여서 담당 밖도 그린다. 옆 북유럽 맵 땅은 그대로(경계를 걸어 넘으면 그 맵으로), 다른 지역 땅은 숲 벽. 모두 막힘
    for yy, xx in zip(*np.nonzero(~mine)):
        m.blocked[yy][xx] = True
    m.objects = [o for o in m.objects if o['kind'] in ('exit', 'spawn') or mine[min(Hd - 1, o['y']), min(Wd - 1, o['x'])]]
    # 2026-10-29(사용자): 출구는 길 끝 — 경계~출구 복도(담당 밖)를 열고 길을 깐다
    report['corridorStuck'] = []
    for s in SEAMS.get(key, []):
        if s['kind'] != 'exit': continue
        for (cx, cy) in corridor(s):
            if not m.inside(cx, cy): report['corridorStuck'].append((s['target'], cx, cy, '맵 밖')); continue
            if m.water[cy][cx]: report['corridorStuck'].append((s['target'], cx, cy, '물')); continue
            m.blocked[cy][cx] = False; m.obj[cy][cx] = m.high[cy][cx] = None
            if m.ground[cy][cx] is None or str(m.ground[cy][cx]).startswith(('relief', 'water')): m.ground[cy][cx] = nt.SNOW
    for (cx, cy) in corr_band:   # 복도 옆 띠: 걸을 수 있는 눈밭(경계 칸은 출구만 전환하므로 옆으로 새도 괜찮음)
        if not m.inside(cx, cy) or m.water[cy][cx] or m.obj[cy][cx] is not None or m.high[cy][cx] is not None: continue
        m.blocked[cy][cx] = False
        if m.ground[cy][cx] is None or str(m.ground[cy][cx]).startswith(('relief', 'water')): m.ground[cy][cx] = nt.SNOW
    for (cx, cy) in reg_walk:   # 다른 지역 길과 양옆 1칸·그 지역 땅 타원: 담당 밖이어도 걸을 수 있게(맵 끝 출구까지)
        if not m.inside(cx, cy) or m.water[cy][cx] or m.obj[cy][cx] is not None or m.high[cy][cx] is not None: continue
        m.blocked[cy][cx] = False
        if m.ground[cy][cx] is None or str(m.ground[cy][cx]).startswith(('relief', 'water')): m.ground[cy][cx] = nt.SNOW
    # 다른 지역 출구: region_links.json에서 북유럽 쪽("by")은 이 생성기가 만든다. 폭은 상대 쪽과 같게, 방향은 links의 edge
    # 2026-10-30(사용자): 출구는 길 끝 = 맵 가장자리 칸, 도착 자리는 한 칸 안쪽
    _regby = {R_['id']: R_ for R_ in REG}
    for lk in LINKS:
        if lk['a']['map'] != key: continue
        e = next((e for e in N1R['exits'] if e['map'] == key and e['label'].startswith(lk['id'])), None)
        if not e: report.setdefault('linkMissing', []).append(lk['id']); continue
        b = lk['b']; edge = lk['a']['edge']; n = b['h'] if edge in 'EW' and b['edge'] in 'EW' else b['w'] if b['edge'] in 'NS' else b['h']
        ax, ay = L(e['at'][1], e['at'][0])
        R_ = _regby.get(lk['id'])
        if R_:
            ax, ay = ax + R_['dx'] * R_['K'] + (R_['cend'] if R_['dy'] else 0), ay + R_['dy'] * R_['K'] + (R_['cend'] if R_['dx'] else 0)
        if edge in 'EW':
            cells = [(ax, ay - n // 2 + k) for k in range(n)]; sp = [(c[0] + (1 if edge == 'W' else -1), c[1]) for c in cells]
            ex_ = dict(x=cells[0][0], y=cells[0][1], w=1, h=n); spawn = dict(x=sp[0][0], y=sp[0][1], w=1, h=n)
        else:
            cells = [(ax - n // 2 + k, ay) for k in range(n)]; sp = [(c[0], c[1] + (-1 if edge == 'S' else 1)) for c in cells]
            ex_ = dict(x=cells[0][0], y=cells[0][1], w=n, h=1); spawn = dict(x=sp[0][0], y=sp[0][1], w=n, h=1)
        for (cx, cy) in cells + sp:   # 지나갈 수 있게 다듬기
            if not m.inside(cx, cy): continue
            m.blocked[cy][cx] = False; m.obj[cy][cx] = m.high[cy][cx] = None; m.road[cy][cx] = max(m.road[cy][cx], 1)
            if m.ground[cy][cx] is None or str(m.ground[cy][cx]).startswith(('relief', 'water')): m.ground[cy][cx] = nt.SNOW
        m.objects = [o for o in m.objects if not any(o['x'] <= cx < o['x'] + o.get('w', 1) and o['y'] <= cy < o['y'] + o.get('h', 1) for cx, cy in cells + sp)]
        m.objects.append(dict(kind='exit', target=b['map'], spawn='link_' + key, edge=edge, **ex_))
        m.objects.append(dict(kind='spawn', id='link_' + b['map'], edge=edge, **spawn))
        report.setdefault('links', []).append((lk['id'], b['map'], ex_))
    report['region'] = []
    for R_ in REG:   # 다른 지역 출구 소품(너머 듬성듬성 + 안쪽 출구 앞 2개)과 이정표
        pr = random.Random(9000 + i + len(report['region'])); placed = []
        cand = [(x_, y_) for (x_, y_, k_, t_, e_, inn) in R_['zone'] if not inn and 0.35 <= e_ <= 0.95 and abs(t_) >= R_['n'] // 2 + 2]
        pr.shuffle(cand)
        for (x_, y_) in cand:
            if len(placed) >= 5: break
            if any(abs(x_ - a) < 4 and abs(y_ - b) < 4 for a, b in placed) or not m.inside(x_, y_) or m.road[y_][x_] > 0 or m.water[y_][x_]: continue
            if m.stamp(pr.choice(R_['look'][1]), x_, y_, force=True): placed.append((x_, y_))
        done_ = False
        for k_ in (3, 2, 4, 5, 6):   # 출구에서 안쪽 k칸, 길 옆(가까운 쪽부터)
            for off in range(R_['n'] // 2 + 1, R_['n'] // 2 + 6):
                for sgn in (1, -1):
                    tx = R_['ax'] - R_['dx'] * k_ + (off * sgn if R_['dy'] else 0); ty = R_['ay'] - R_['dy'] * k_ + (off * sgn if R_['dx'] else 0)
                    if not (m.inside(tx, ty) and m.mine[ty, tx] if hasattr(m, 'mine') else m.inside(tx, ty)): continue
                    if m.blocked[ty][tx] or m.road[ty][tx] > 0 or m.water[ty][tx] or m.obj[ty][tx] is not None or m.high[ty][tx] is not None: continue
                    if not any(m.inside(tx + a, ty + b) and m.road[ty + b][tx + a] > 0 for a in (-1, 0, 1) for b in (-1, 0, 1)): continue
                    m.prop(tiles.SIGN, tx, ty); m.add('sign', tx, ty, text=ARROW[(R_['dy'], R_['dx'])] + ' ' + R_['name'] + ' 가는 길', _new=True, _tag='exit'); done_ = True; break
                if done_: break
            if done_: break
        if not done_: report.setdefault('regionSignFail', []).append(R_['id'])
        report['region'].append((R_['id'], R_['b'], len(placed)))
    report['n5after'] = n5.after_finish(m, key, mine, None, NAMES, report['anchor'], rng_seed=i)
    report['boards'] = rewrite_boards(m, report['roads'].get('gate_pos', {}))
    # 2026-10-29(갇힘): 출입구·도착 칸 위에 남은 1칸 소품(눈 더미·작은 돌 등)은 치운다. 건물·나무(위층 있는 것)는 보고만
    report['edgeClear'] = []; report['edgeStuck'] = []
    for o in m.objects:
        if o['kind'] not in ('exit', 'spawn'): continue
        for a in range(o.get('w', 1)):
            for b in range(o.get('h', 1)):
                cx, cy = o['x'] + a, o['y'] + b
                if not m.inside(cx, cy) or not m.blocked[cy][cx] or m.water[cy][cx]: continue
                if m.high[cy][cx] is None and not (m.inside(cx, cy - 1) and m.high[cy - 1][cx] is not None):
                    report['edgeClear'].append((m.obj[cy][cx], cx, cy)); m.obj[cy][cx] = None; m.blocked[cy][cx] = False
                else: report['edgeStuck'].append((o['kind'], cx, cy, m.obj[cy][cx], m.high[cy][cx]))
    # 2026-10-29(사용자: 나룻배 사공이 눈밭 한가운데): 나룻배 자리는 배 옆(없으면 가장 가까운) 물가 걸을 수 있는 칸으로
    boats_ = [(X + 2, Y + 1) for (nm_, X, Y) in m.stamps if nm_ == 'boat']
    taken_sp = {(o['x'], o['y']) for o in m.objects}
    def shore(x_, y_):
        return (m.inside(x_, y_) and mine[y_, x_] and not m.blocked[y_][x_] and not m.water[y_][x_] and m.obj[y_][x_] is None and (x_, y_) not in taken_sp
                and any(m.inside(x_ + a, y_ + b) and m.water[y_ + b][x_ + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))))
    shores = [(x_, y_) for y_ in range(m.h) for x_ in range(m.w) if shore(x_, y_)]
    for si, sp in enumerate(m.spots):
        if sp[0] != 'ferry' or not shores: continue
        ref = min(boats_, key=lambda b: abs(b[0] - sp[1]) + abs(b[1] - sp[2])) if boats_ else (sp[1], sp[2])
        bx, by = min(shores, key=lambda c: abs(c[0] - ref[0]) + abs(c[1] - ref[1]))
        report.setdefault('ferryMoved', []).append(((sp[1], sp[2]), (bx, by))); m.spots[si] = (sp[0], bx, by) + tuple(sp[3:])
    # 2026-10-29(사용자): 이정표는 출구마다 1개 + 마을 맵만 종합 안내판 1개. 나머지(옛 이정표·n5 이정표)는 뺀다
    _signs = [o for o in m.objects if o['kind'] == 'sign']
    _board = None
    if (VIL[y0:y1, x0:x1] & mine).any():
        _cands = [o for o in _signs if o.get('_tag') != 'exit' and '\n' in (o.get('text') or '')]
        if _cands:
            ax_, ay_ = report['anchor']; _board = min(_cands, key=lambda o: abs(o['x'] - ax_) + abs(o['y'] - ay_))
    for o in _signs:
        if o.get('_tag') == 'exit' or o is _board: continue
        x_, y_ = o['x'], o['y']
        if m.inside(x_, y_) and m.obj[y_][x_] == tiles.SIGN: m.obj[y_][x_] = None; m.blocked[y_][x_] = False
        m.objects.remove(o)
    report['signsKept'] = (sum(1 for o in _signs if o.get('_tag') == 'exit'), _board is not None, len(_signs))
    report['snowArena'] = place_snowball_arena(m, key, mine)
    report['runehill'] = place_runehill(m, key, mine)
    report['caravan'] = place_caravan(m, key, mine)
    report['bees'] = place_bees(m, key, mine)
    report['extras'] = place_extras(m, key, mine)
    report['cutFixed'] = fix_cut_stamps(m, mine)
    report['homeNpc'] = home_npcs(m, key, mine, L)
    report['exitSignMoved'] = exit_signs_out(m)
    # 2026-10-30(사용자: 아래 출구로 내려오면 지도 붉은 점이 이상함): 출구 복도 칸은 그 복도가 내 땅을 벗어나는 칸(경계 건널목)에 찍히게
    def _near_own(c):
        if 0 <= c[1] < Hd and 0 <= c[0] < Wd and mine[c[1], c[0]]: return c
        cs = [(x_, y_) for yy_, xx_ in [(0, 0)] for y_ in range(max(0, c[1] - 12), min(Hd, c[1] + 13)) for x_ in range(max(0, c[0] - 12), min(Wd, c[0] + 13)) if mine[y_, x_]]
        return min(cs, key=lambda q: (q[0] - c[0]) ** 2 + (q[1] - c[1]) ** 2) if cs else None
    seed = {}
    for s_ in sorted([q for q in SEAMS.get(key, []) if q['kind'] == 'exit'], key=lambda q: -q.get('out', 0)):
        dy_, dx_ = _dir(s_['edge']); cy_, cx_ = s_['cell']; o_ = s_.get('out', 0)
        cr = _near_own((cx_ - x0 - dx_ * o_ + (s_['w'] - 1) // 2, cy_ - y0 - dy_ * o_ + (s_['h'] - 1) // 2))
        if not cr: continue
        for c in corridor(s_):
            if m.inside(*c) and not mine[c[1], c[0]]: seed[c] = cr
    for R_ in REG:
        cr = _near_own((R_['ax'], R_['ay']))
        if cr:
            for c in reg_walk | reg_road:
                if m.inside(*c) and not mine[c[1], c[0]] and abs(c[0] - R_['ax']) + abs(c[1] - R_['ay']) <= R_['K'] + R_['n'] + 6: seed.setdefault(c, cr)
    m._anchor_seed = seed
    report['moved'] = tidy_people(m, mine)
    # 사람 정리 뒤 장꾼 자리가 바뀌었을 수 있어 한 번 더: 장꾼 없는 좌판은 뺀다(그림만 지움, 바닥은 그대로)
    from north_spots import keeper_spots as _ksp
    _ksp(m); _keep = {(m._keeper_stalls[j] - 1, m._keeper_stalls[j + 1] - 1) for j in range(0, len(m._keeper_stalls), 2)}
    for st in [t for t in m.stamps if t[0] == 'stall' and (t[1], t[2]) not in _keep]:
        for a_ in range(3):
            for b_ in range(2):
                if m.inside(st[1] + a_, st[2] + b_): m.obj[st[2] + b_][st[1] + a_] = m.high[st[2] + b_][st[1] + a_] = None; m.blocked[st[2] + b_][st[1] + a_] = False
        m.stamps.remove(st); report['stallFix']['stallsRemoved'].append((int(st[1]), int(st[2])))
    from n6_fix import ux_fixes as _ux   # 2026-10-30 유저테스트 반영
    report['ux'] = _ux(m, key, mine)
    _mf = mine2.copy()   # (10/30) 다른 지역 길(담당 밖)도 걷는 땅으로 보고 검사
    for (cx, cy) in reg_walk: _mf[cy, cx] = True
    report['unreach'] = final_check(m, _mf)
    return m, report


# ── 2026-10-29(사용자): 길 안내 — 어느 길로 가면 어디인지 보이게 ──
#   마을 어귀 이정표(이 길은 ○○ 가는 길), 마을 밖 갈림길 이정표(방향 화살표), 길가 돌기둥(눈밭에서 길 윤곽), 출구 양옆 등불
POST_GAP = 6   # 돌기둥 간격(칸)
ARROW = {(-1, 0): '↑', (1, 0): '↓', (0, -1): '←', (0, 1): '→'}
def dress_roads(m, key, mine, mine2, x0, y0, rng):
    Wd, Hd = m.w, m.h; rep = dict(gate=[], fork=[], posts=0, lamps=0)
    def vil(x, y): return bool(VIL[y + y0, x + x0])
    roadc = {(x, y) for y in range(Hd) for x in range(Wd) if m.road[y][x] > 0 and mine2[y, x] and not m.blocked[y][x]}
    def nb4(c): return [(c[0] + a, c[1] + b) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))]
    # 1) 출구에서 길을 따라 거슬러: 길 칸마다 가장 가까운 출구(=이 길의 목적지)
    srcs = {}
    for (A, B), r in SEAM_REC.items():
        if A != key: continue
        for (gy, gx) in r['seam'] + r['cells']:
            c = (gx - x0, gy - y0)
            if c in roadc: srcs[c] = B
    for o in m.objects:
        if o['kind'] == 'exit' and str(o.get('spawn', '')).startswith('link_'):
            for a in range(o.get('w', 1)):
                for b in range(o.get('h', 1)):
                    if (o['x'] + a, o['y'] + b) in roadc: srcs[(o['x'] + a, o['y'] + b)] = o['target']
    label = dict(srcs); dq = deque(srcs)
    while dq:
        c = dq.popleft()
        for n in nb4(c):
            if n in roadc and n not in label: label[n] = label[c]; dq.append(n)
    def name_of(b): return NAMES.get(b, b)
    zone_ = {(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] in ('exit', 'spawn') for a in range(-2, o.get('w', 1) + 2) for b in range(-2, o.get('h', 1) + 2)}
    def free_prop(x, y, h=1):   # 길 옆 빈 칸(위로 h칸짜리 소품용)
        return all(m.inside(x, y - j) and mine[y - j, x] and not m.blocked[y - j][x] and not m.water[y - j][x] and m.road[y - j][x] == 0
                   and m.obj[y - j][x] is None and m.high[y - j][x] is None and not m.reserved[y - j][x] and (x, y - j) not in zone_ for j in range(h)) \
               and m.inside(x, y + 1) and not (m.blocked[y + 1][x] and m.high[y + 1][x] is None and m.obj[y + 1][x] is not None and False)
    def beside_road(x, y): return any(n in roadc for n in nb4((x, y)))
    def clusters_(cells, d=4):
        rest = list(cells); out = []
        while rest:
            grp = [rest.pop()]; k = 0
            while k < len(grp):
                a = grp[k]; k += 1
                near_ = [c for c in rest if abs(c[0] - a[0]) <= d and abs(c[1] - a[1]) <= d]
                for c in near_: rest.remove(c); grp.append(c)
            out.append(grp)
        return out
    def place_sign(cx, cy, text, tag=None):
        for r_ in range(1, 6):
            for yy in range(cy - r_, cy + r_ + 1):
                for xx in range(cx - r_, cx + r_ + 1):
                    if max(abs(xx - cx), abs(yy - cy)) != r_ or not free_prop(xx, yy) or not beside_road(xx, yy): continue
                    if any(o['kind'] == 'sign' and abs(o['x'] - xx) <= 5 and abs(o['y'] - yy) <= 5 for o in m.objects): return None
                    if any(o['kind'] == 'sign' and o.get('text') == text and abs(o['x'] - xx) <= 25 and abs(o['y'] - yy) <= 25 for o in m.objects): return None   # 같은 글 이정표가 가까이 있으면 생략
                    m.prop(tiles.SIGN, xx, yy); m.add('sign', xx, yy, text=text, _new=True, _tag=tag); return (xx, yy)
        return None
    # 2) 마을 어귀: 마을 안 길 칸 옆에 마을 밖 길 칸 → 그 길의 목적지
    gate = [c for c in roadc if vil(*c) and any(n in roadc and not vil(*n) for n in nb4(c))]
    gate_pos = {}
    def reach_dests(grp):   # 이 어귀에서 마을 밖 길만 따라가 닿는 출구들
        seen = set(grp); dq = deque(grp); found = set()
        while dq:
            c = dq.popleft()
            if c in srcs: found.add(srcs[c])
            for n in nb4(c):
                if n in roadc and n not in seen and not vil(*n): seen.add(n); dq.append(n)
        return sorted(found, key=name_of)
    for grp in clusters_(gate):
        dests = reach_dests(grp)
        if not dests: continue
        for b in dests: gate_pos.setdefault(b, []).extend(grp)
        cx, cy = round(sum(c[0] for c in grp) / len(grp)), round(sum(c[1] for c in grp) / len(grp))
        # 2026-10-29(사용자): 마을 어귀 이정표는 세우지 않는다(출구마다 1개만). 자리(gate_pos)는 안내판 화살표용으로만 씀
    rep['gate_pos'] = gate_pos
    # 2c) 마을 어귀 이정표가 없는 목적지(마을 없는 길 맵 등)는 출구 복도 들머리(경계 안쪽 2~4칸) 길가에 이정표
    for (A, B), r in SEAM_REC.items():
        if A != key: continue
        for cr in r['crossings']:
            grp = [(gx - x0, gy - y0) for (gy, gx) in cr['seam']]; dy, dx = _dir(cr['edge'])
            cx, cy = round(sum(c[0] for c in grp) / len(grp)), round(sum(c[1] for c in grp) / len(grp))
            pos = place_sign(cx - dx * 3, cy - dy * 3, ARROW[(dy, dx)] + ' ' + name_of(B) + ' 가는 길', tag='exit')   # 2026-10-29(사용자): 출구 쪽 화살표
            if pos: rep.setdefault('exitsign', []).append((B, pos))
    # 3) 마을 밖 갈림길: 반지름 3 고리 위 길 칸이 3덩어리 이상(곧은 길 2, 광장 1)
    forks = []
    for (x, y) in roadc:
        if vil(x, y) or (x, y) in zone_: continue
        for R_ in (3, 4, 5):   # 길이 넓으면 작은 고리는 길 안에 잠겨서 못 가른다
            ring = [(x + a, y + b) for a in range(-R_, R_ + 1) for b in range(-R_, R_ + 1) if max(abs(a), abs(b)) == R_ and (x + a, y + b) in roadc]
            if len(ring) < 3: continue
            rs = set(ring); comps = []
            while rs:
                st = [rs.pop()]; comp = set(st)
                while st:
                    c = st.pop()
                    for a in range(-1, 2):
                        for b in range(-1, 2):
                            n = (c[0] + a, c[1] + b)
                            if n in rs: rs.remove(n); comp.add(n); st.append(n)
                comps.append(comp)
            if len(comps) >= 3: forks.append(((x, y), comps, R_)); break
    rep['forkcells'] = len(forks); rep['forkfail'] = []
    for grp in clusters_([f[0] for f in forks], d=4):
        cx, cy = round(sum(c[0] for c in grp) / len(grp)), round(sum(c[1] for c in grp) / len(grp))
        def parts_of(f0):   # 갈림 칸 하나에서: 가지마다 갈림목을 빼고 길을 따라가 닿는 출구·마을(가장 가까운 가지에만 적는다)
            comps, R_ = f0[1], f0[2]; fx, fy = f0[0]; br = []
            for comp in comps:
                seen_ = set(comp); dq_ = deque((c, 0) for c in comp); found = {}
                while dq_:
                    c, d0 = dq_.popleft()
                    if c in srcs and srcs[c] not in found: found[srcs[c]] = d0
                    if vil(*c) and key not in found: found[key] = d0
                    for n in nb4(c):
                        if n in roadc and n not in seen_ and max(abs(n[0] - fx), abs(n[1] - fy)) > R_: seen_.add(n); dq_.append((n, d0 + 1))
                mx, my = sum(c[0] for c in comp) / len(comp) - fx, sum(c[1] for c in comp) / len(comp) - fy
                d_ = (0, 1 if mx > 0 else -1) if abs(mx) > abs(my) else (1 if my > 0 else -1, 0)
                br.append((ARROW[d_], found))
            best_br = {}
            for i_, (_, found) in enumerate(br):
                for b_, d0 in found.items():
                    if b_ not in best_br or d0 < best_br[b_][0]: best_br[b_] = (d0, i_)
            parts = []
            for i_, (arrow, found) in enumerate(br):
                mine_ = sorted((b_ for b_, (d0, j_) in best_br.items() if j_ == i_), key=name_of)
                if mine_: parts.append((arrow, ' · '.join(name_of(b_) for b_ in mine_[:2])))
            return parts
        gs = set(grp)
        cands = sorted((f for f in forks if f[0] in gs), key=lambda f: abs(f[0][0] - cx) + abs(f[0][1] - cy))[:10]
        best_f, parts = None, []
        for f in cands:   # 가지 목적지가 가장 많이 갈리는 칸이 진짜 갈림목
            p_ = parts_of(f)
            if len(p_) > len(parts): best_f, parts = f, p_
        if len(parts) < 2: rep.setdefault('forkskip', []).append((cx, cy, parts)); continue
        cx, cy = best_f[0]
        # 2026-10-29(사용자): 갈림길 이정표는 세우지 않는다(출구마다 1개만)
    # 4) 길가 돌기둥: 마을 밖 길 옆, POST_GAP 간격, 양옆 번갈아
    posts = []
    for (x, y) in sorted(roadc, key=lambda c: (c[1] // 3, c[0])):
        if vil(x, y) or (x, y) in zone_: continue
        if any(abs(px - x) <= POST_GAP and abs(py - y) <= POST_GAP for px, py in posts): continue
        cands = [n for n in nb4((x, y)) if n not in roadc and free_prop(n[0], n[1], 2) and not vil(*n)]
        if not cands: continue
        nx, ny = rng.choice(cands)
        if m.stamp('snowpost', nx, ny - 1): posts.append((nx, ny)); rep['posts'] += 1
    # 5) 출구 문: 경계 칸 양옆 등불
    for (A, B), r in SEAM_REC.items():
        if A != key: continue
        for cr in r['crossings']:
            grp = [(gx - x0, gy - y0) for (gy, gx) in cr['seam']]; dy, dx = _dir(cr['edge'])
            if dy: xs_ = [c[0] for c in grp]; y_ = grp[0][1]; spots = [(min(xs_) - 1, y_), (max(xs_) + 1, y_)]
            else: ys_ = [c[1] for c in grp]; x_ = grp[0][0]; spots = [(x_, min(ys_) - 1), (x_, max(ys_) + 1)]
            for (lx, ly) in spots:
                if m.inside(lx, ly) and m.inside(lx, ly - 1) and not m.water[ly][lx] and m.road[ly][lx] == 0 and m.obj[ly][lx] is None and m.high[ly][lx] is None and m.obj[ly - 1][lx] is None and m.high[ly - 1][lx] is None:
                    if m.stamp('lantern', lx, ly - 1, force=True): rep['lamps'] += 1
    return rep

def rewrite_boards(m, gate_pos):
    """옛 마을 안내판(화살표 글)을 새 배치 기준으로: 목적지마다 마을 어귀 쪽 방향 화살표. n5가 옛 이정표를 옮긴 뒤에 부른다"""
    n = 0
    for o in m.objects:
        if o['kind'] != 'sign' or o.get('_new') or not any(ch in (o.get('text') or '') for ch in '↑↓←→'): continue
        lines = [ln for ln in o['text'].split('\n') if not any(ch in ln for ch in '↑↓←→')]
        for b, cells in sorted(gate_pos.items(), key=lambda kv: NAMES.get(kv[0], kv[0])):
            mx = sum(c[0] for c in cells) / len(cells) - o['x']; my = sum(c[1] for c in cells) / len(cells) - o['y']
            d_ = (0, 1 if mx > 0 else -1) if abs(mx) > abs(my) else (1 if my > 0 else -1, 0)
            lines.append(f"{ARROW[d_]} {NAMES.get(b, b)}")
        if len(lines) <= 1 and not gate_pos: continue
        o['text'] = '\n'.join(lines); n += 1
    return n

def export(m):
    # 이 컴퓨터에 없는(에셋 팩에서만 만들 수 있는) 타일은 가까운 그림으로 바꾼다
    def have(k):
        nm = k.replace(':', '_').replace('@', '_')
        return os.path.exists(f'{TILE_DIR}/{nm}.png') or ('@' in k and os.path.exists(f"{TILE_DIR}/{k.split('@')[0].replace(':', '_')}.png")) or k.startswith(('northbuild', 'northart'))
    SUB = lambda k: ('water:14:1' if k.startswith('water:') else 'floor:1:8@dirtedge' if k.endswith('@dirtedge') else 'floor:1:8@grassedge' if k.endswith('@grassedge') else nt.SNOW)
    subs = set()
    for layer in (m.ground, m.deco, m.obj, m.high):
        for row in layer:
            for x_, k in enumerate(row):
                if k and not have(k): subs.add(k); row[x_] = SUB(k) if layer is m.ground else None
    if subs: print('  바꾼 타일', sorted(subs))
    used = set(); palette, index = [''], {'': 0}
    def name_of(k): return k.replace(':', '_').replace('@', '_')
    def flat(layer):
        out = []
        for row in layer:
            for k in row:
                k = k or ''
                if k: used.add(k)
                n = name_of(k) if k else ''
                if n not in index: index[n] = len(palette); palette.append(n)
                out.append(index[n])
        return out
    DEF = dict(w=1, h=1, id='', npc='', town='', target='', spawn='', edge='', text='', name='', character='', lines=[], variant=0, idx=0,
               sheet='', frames=0, fps=0.0, radius=0.0, type='', path=[])
    objs = []
    for o in m.objects:
        d = dict(DEF); d.update(o); d['path'] = [v for pt in o.get('path', []) for v in pt]; objs.append(d)
    # 2026-10-29: 옛 export_north.py처럼 좌판(stall) 타일은 빼고 런타임(NorthCraft)이 새 좌판 그림을 그린다. 막힘은 그대로.
    # (빼지 않아서 타일 좌판 지붕(위층)이 계산대 안 장꾼 머리를 덮어 장꾼이 안 보였다)
    _sc = {(x + dx, y + dy) for (n, x, y) in m.stamps if n == 'stall' for dx in range(3) for dy in range(2)}
    _obj = [[None if (x, y) in _sc else k for x, k in enumerate(row)] for y, row in enumerate(m.obj)]
    _high = [[None if (x, y) in _sc else k for x, k in enumerate(row)] for y, row in enumerate(m.high)]
    d = dict(id=m.id, name=m.name, w=m.w, h=m.h, hours=m.hours, ground=flat(m.ground), deco=flat(m.deco), obj=flat(_obj), high=flat(_high),
             road=[gr for row in m.road for gr in row], blocked=[1 if b else 0 for row in m.blocked for b in row], objects=objs, palette=palette)
    sub = 'Center' if m.id.startswith('north_c_') else 'North'
    txt = json.dumps(d, ensure_ascii=False, separators=(',', ':'), default=lambda v: v.item() if hasattr(v, 'item') else str(v))   # 다 만든 뒤에 씀(중간에 실패해도 원래 파일 안 깨짐)
    open(R + f'/Assets/PocketCaravan/Maps/{sub}/{m.id}.json', 'w').write(txt)
    missing = []
    for k in used:
        p = f'{TILE_DIR}/{name_of(k)}.png'
        if os.path.exists(p): continue
        try:
            if '@' in k and os.path.exists(f"{TILE_DIR}/{name_of(k.split('@')[0])}.png"):   # 색 바꾼 타일: 원본 PNG에서 만든다
                img = tiles.recolor(Image.open(f"{TILE_DIR}/{name_of(k.split('@')[0])}.png").convert('RGBA'), k.split('@')[1])
            else: img = _ti(k)
            if img is None: raise ValueError
            img.save(p)
        except Exception: missing.append(k)
    return missing

NAMES = {k: Z[k]['name'].split(' (')[0] for k in KEYS}
NAMES.update({'venice_c_vineyard': '베네치아 포도밭', 'venice_c_pass': '베네치아 산기슭 고갯길', 'venice_c_gate': '베네치아 동문 큰길',
              'transit_crossroads': '고려 교역 길목', 'arabia_central_steppe': '한가운데 초원 (네 지역이 만나는 곳)'})
LIFE_KEYS = ['chimneys', 'fires', 'spits', 'glints', 'lamps', 'springs', 'runes', 'crystals', 'fishing', 'fishingWater', 'fishEdge', 'sledStart',
             'secrets', 'secretStones', 'ring', 'baths', 'bathEdge', 'feed', 'guild', 'festival', 'events', 'museum', 'fortune', 'ferry', 'seats',
             'stalls', 'crowd', 'inns', 'keepers', 'keeperStall', 'stallArt', 'sides', 'spitters', 'craft', 'yard', 'market', 'sledTrack']
HOUSES = {'woodhouse', 'h_dark', 'h_turf', 'h_shed', 'h_round', 'h_dome', 'new_jarlhall', 'new_smithy', 'longhall'}
CHIMNEY = {"longhall": (2, 3), "woodhouse": (2, 0), "h_dark": (3, 0), "h_turf": (1, 0), "h_round": (1, 0), "h_dome": (1, 0),
           "h_shed": (1, 0), "new_jarlhall": (6, 0), "new_smithy": (3, 0)}
def life_of(m):
    """N5: 옛 생성기(export_north.py)와 같은 계산을 새 맵에 그대로 돌린다 (north_spots.py 함수)"""
    from north_spots import (fishing_cells, fish_edge, secret_cells, spot_cells, museum_row, market_spots, inn_spots, keeper_spots,
                             keeper_stalls, stall_art, side_spots, spit_sellers, nearest_walk)
    st = m.stamps; R_ = random.Random(m.id)
    walk = [(x, y) for y in range(m.h) for x in range(m.w) if m.mine[y, x] and not m.blocked[y][x] and not m.water[y][x]
            and not m.road[y][x] and m.obj[y][x] is None and m.high[y][x] is None]
    # 2026-10-29(사용자): 좌판이 맵 경계 출입구에 붙어 있으면 지나가다 옆 맵으로 넘어가 버림 → 출입구 둘레를 잠깐 막고 자리를 찾는다(4칸, 자리가 없으면 3·2칸)
    for buf in (4, 3, 2, 0):
        edge = {(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] in ('exit', 'spawn') for a in range(-buf, o.get('w', 1) + buf) for b in range(-buf, o.get('h', 1) + buf)} if buf else set()
        # 나룻배·숨은 룬돌 같은 Space 자리 둘레 3칸도 피한다(좌판에서 Space를 누르면 나룻배로 다른 맵에 가 버림)
        edge |= {(x + a, y + b) for (rx, ry, rw, rh) in getattr(m, '_mg_rects', []) for x in range(rx - 1, rx + rw + 1) for y in range(ry - 1, ry + rh + 2) for a, b in ((0, 0),)}
        if getattr(m, '_stall_area', None):   # 이 구역 밖은 막아 좌판이 구역 안에만 서게
            ax0, ay0, ax1, ay1 = m._stall_area; edge |= {(x, y) for y in range(m.h) for x in range(m.w) if not (ax0 <= x <= ax1 and ay0 <= y <= ay1 + 2)}
        edge |= {(x + a, y + b) for (k, x, y) in m.spots if k != 'seat' for a in range(-3, 4) for b in range(-3, 4)} | {(x + a, y + b) for (k, x, y) in m.spots if k == 'seat' for a in range(-2, 3) for b in range(-2, 3)} | {(x + a, y + b) for (x, y, i) in m.secrets for a in range(-3, 4) for b in range(-2, 5)}
        tmp = [(x, y) for (x, y) in edge if m.inside(x, y) and not m.blocked[y][x]]
        for (x, y) in tmp: m.blocked[y][x] = True
        ms = market_spots(m)
        mk = [o for o in m.objects if o["kind"] == "market"]
        if mk and len(ms[0]) < 4:   # 새 장터 광장이 넓어 장터 칸 둘레가 다 길이면 좌판 자리를 못 찾음: 광장 가장자리 쪽에서도 찾아 둘을 채운다
            ox_, oy_ = mk[0]["x"], mk[0]["y"]
            for dx_, dy_ in ((-6, 0), (6, 0), (0, 4), (0, -4), (-8, 3), (8, 3), (0, 8), (0, -8), (5, 8), (-5, 8), (5, -8), (-5, -8), (10, 0), (0, 12), (0, -12)):
                mk[0]["x"], mk[0]["y"] = ox_ + dx_, oy_ + dy_
                st2, cr2 = market_spots(m)
                for j in range(0, len(st2), 2):
                    if len(ms[0]) >= 4: break
                    if all(abs(st2[j] - ms[0][k]) + abs(st2[j + 1] - ms[0][k + 1]) > 4 for k in range(0, len(ms[0]), 2)): ms[0].extend(st2[j:j + 2])
                if len(ms[0]) >= 4: break
            mk[0]["x"], mk[0]["y"] = ox_, oy_
        for (x, y) in tmp: m.blocked[y][x] = False
        if len(ms[0]) >= 4 or not mk: break
    if getattr(m, '_stall_area', None):   # 2026-10-29(사용자): 정한 구역(나루: 덕장 위) 안에서 좌판 둘을 직접 고른다
        ax0, ay0, ax1, ay1 = m._stall_area
        people = [(o['x'], o['y']) for o in m.objects if o['kind'] in ('talker', 'npc', 'board', 'sign')]
        zone_ = {(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] in ('exit', 'spawn') for a in range(-3, o.get('w', 1) + 3) for b in range(-3, o.get('h', 1) + 3)}
        def fr(x, y): return m.inside(x, y) and m.mine[y, x] and not m.blocked[y][x] and not m.water[y][x] and m.obj[y][x] is None and m.high[y][x] is None and m.road[y][x] == 0 and (x, y) not in zone_
        cand = []
        for y in range(ay0 + 2, ay1 + 1):
            for x in range(ax0 + 1, ax1):
                if all(fr(x + a, y + b) for a in (-1, 0, 1) for b in (-2, -1, 0)) and fr(x, y + 1) and not any(abs(px - x) <= 2 and y - 3 <= py <= y + 2 for (px, py) in people):
                    cand.append((ay1 - y, abs(x - (ax0 + ax1) / 2), x, y))
        got = []
        for rule in (lambda x, y, gx, gy: abs(x - gx) >= 5 and abs(y - gy) <= 1, lambda x, y, gx, gy: abs(x - gx) >= 5 or abs(y - gy) >= 4):   # 나란히 먼저
            pairs = sorted(((c1[0] + c2[0] + abs(c1[2] + c2[2] - ax0 - ax1) * 0.3, (c1[2], c1[3]), (c2[2], c2[3])) for i_, c1 in enumerate(cand) for c2 in cand[i_ + 1:] if rule(c1[2], c1[3], c2[2], c2[3])), key=lambda p_: p_[0])
            got = list(pairs[0][1:]) if pairs else []
            if len(got) == 2: break
        if len(got) == 2: ms = ([v for c in got for v in c], ms[1])
    return dict(mapId=m.id, h=m.h,
        chimneys=[[x + CHIMNEY[n][0], y + CHIMNEY[n][1]] for (n, x, y) in st if n in CHIMNEY],
        fires=[[x + 1, y + 1] for (n, x, y) in st if n == "fire"],
        spits=[[x + 1, y + 1] for (n, x, y) in st if n == "spit"],
        glints=[[x, y] for (x, y) in R_.sample(walk, min(70, len(walk)))],
        lamps=[[x, y] for (n, x, y) in st if n in ("lamp", "lantern")],
        springs=[[x, y] for (x, y) in sorted(m._springs)][::6],
        runes=[[x, y] for (n, x, y) in st if m.id in ("north_runehill", "north_c_runeway") and n in ("runestone", "runestone_b", "picturestone", "snowpost")] + [[x, y] for (x, y, i) in m.secrets],
        crystals=[[x, y] for (n, x, y) in st if n.startswith(("crys_", "gem_"))],
        fishing=[list(c) for c in fishing_cells(m)], fishingWater=[[x, y] for (x, y) in m.fishing], fishEdge=fish_edge(m),
        sledStart=list(m.sled_track[0]) if getattr(m, "sled_track", None) else [],
        secrets=[list(c) for c in secret_cells(m)], secretStones=[[x, y] for (x, y, i) in m.secrets],
        ring=[v for c in spot_cells(m, "ring") for v in c], baths=[list(c) for c in spot_cells(m, "bath")],
        bathEdge=[v for (x, y) in sorted({(x + a, y + b) for (x, y) in m._springs for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))
                                          if 0 <= x + a < m.w and 0 <= y + b < m.h and (x + a, y + b) not in m._springs
                                          and not m.blocked[y + b][x + a] and not m.water[y + b][x + a]}) for v in (x, y)],
        feed=[v for c in spot_cells(m, "feed") for v in c], guild=[v for c in spot_cells(m, "guild") for v in c],
        festival=[v for c in spot_cells(m, "festival") for v in c],
        events=[[o["x"], o["y"]] for o in m.objects if o["kind"] == "event"],
        museum=[v for c in spot_cells(m, "museum") for v in c] + (list(museum_row(m)) if spot_cells(m, "museum") else []),
        fortune=[v for c in spot_cells(m, "fortune") for v in c], ferry=[v for c in spot_cells(m, "ferry") for v in c],
        seats=[v for (k, x, y) in getattr(m, "spots", []) if k == "seat" for v in (x, y)],
        stalls=ms[0], crowd=ms[1], inns=inn_spots(m), keepers=keeper_spots(m) + KEEPER_EXTRA.get(m.id, ([], []))[0], keeperStall=keeper_stalls(m) + KEEPER_EXTRA.get(m.id, ([], []))[1], stallArt=stall_art(m),
        sides=side_spots(m), spitters=spit_sellers(m),
        craft=[v for i, k in enumerate(("craft_fish", "craft_fur", "craft_brew")) for c in spot_cells(m, k) for v in (c[0], c[1], i)],
        yard=[v for i, k in enumerate(("yard_fish", "yard_fur", "yard_brew")) for (kk, x, y) in getattr(m, "spots", []) if kk == k for v in (x, y, i)],
        market=[v for o in m.objects if o["kind"] == "market" for v in nearest_walk(m, o["x"] + 2, o["y"] + 1, avoid={(o["x"], o["y"])})],
        sledTrack=[[x, y] for (x, y) in getattr(m, "sled_track", []) or []],
        critters=[dict(c_, **CRITTER_FIX.get(m.id, {}).get(c_.get('kind'), {})) if isinstance(c_, dict) else c_ for c_ in m.life])

def _corridor_anchor(m):
    """담당 밖인데 걸을 수 있는 칸(출구 복도 등) → 지도에 찍을 내 땅 칸. 'x,y,ax,ay' (위에서부터 y)
    복도 칸은 그 복도의 경계 건널목(build가 _anchor_seed로 넘김), 나머지 바깥 칸은 걸어서 가까운 쪽을 따른다"""
    walk = lambda x, y: m.inside(x, y) and not m.blocked[y][x] and not m.water[y][x]
    N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
    src = {c: a for c, a in getattr(m, '_anchor_seed', {}).items() if walk(*c)}
    dq = deque(src)
    for y in range(m.h):
        for x in range(m.w):
            if m.mine[y, x] and walk(x, y): src[(x, y)] = (x, y); dq.append((x, y))
    while dq:
        c = dq.popleft()
        for a, b in N4:
            n = (c[0] + a, c[1] + b)
            if n in src or not walk(*n) or m.mine[n[1], n[0]]: continue
            src[n] = src[c]; dq.append(n)
    return [f'{x},{y},{a[0]},{a[1]}' for (x, y), a in src.items() if not m.mine[y, x]]

_life_of_base = life_of
def life_of(m):
    d = _life_of_base(m)
    sn = getattr(m, '_snow', None)
    if sn: d.update(snowArena=sn['arena'], snowWalls=[list(c) for c in sn['walls']], snowKids=[list(c) for c in sn['kids']])
    ex = getattr(m, '_extras', None)
    if ex: d.update({k: [int(v) for v in val] for k, val in ex.items()})
    bz = getattr(m, '_bees', None)
    if bz: d.update({k: [int(v) for v in val] for k, val in bz.items()})
    cv = getattr(m, '_caravan', None)
    if cv: d.update(caravanMerchants=[int(v) for v in cv['merchants']], caravanDeco=[int(v) for v in cv['deco']])
    rn = getattr(m, '_rune', None)
    if rn: d.update({k: [int(v) for v in np.array(val).ravel()] for k, val in rn.items()})
    return d

def rune_hints(maps):
    """숨은 룬돌 7개의 실제 자리 → 순례자·룬돌 고리가 말해 줄 귀띔 ("라우가르 온천 마을 북동쪽, 물가 바위 뒤")"""
    hints = [''] * 8
    for m in maps:
        ys, xs = np.nonzero(m.mine)
        if len(xs) == 0: continue
        cx, cy = xs.mean(), ys.mean()
        for (x, y, i) in m.secrets:
            if not (1 <= i <= 7): continue
            dx, dy = x - cx, y - cy
            ns = '북' if dy < -m.h * 0.12 else '남' if dy > m.h * 0.12 else ''
            ew = '서' if dx < -m.w * 0.12 else '동' if dx > m.w * 0.12 else ''
            side = (ns + ew + '쪽') if (ns or ew) else '한가운데 가까이'
            near = lambda f, r=3: any(m.inside(x + a, y + b) and f(x + a, y + b) for a in range(-r, r + 1) for b in range(-r, r + 1))
            if near(lambda a, b: m.water[b][a]): what = '물가'
            elif near(lambda a, b: m.high[b][a] is not None and str(m.high[b][a]).startswith('nature'), 2): what = '나무 사이'
            elif near(lambda a, b: m.road[b][a] > 0, 4): what = '길에서 조금 벗어난 곳'
            else: what = '바위·덤불 뒤'
            hints[i] = f"{NAMES.get(m.id, m.id)} {side}, {what}"
    json.dump(dict(hints=hints), open(R + '/Assets/PocketCaravan/Resources/North/RuneHints.json', 'w'), ensure_ascii=False, indent=1)
    return hints

def world_outputs(maps):
    """M 지도(Resources/WorldMap/NorthTerrain.png, NorthLayout.json)와 NorthLife.json을 새 맵으로"""
    S = 4
    minx = min(m.box[0] for m in maps); miny = min(m.box[1] for m in maps)
    maxx = max(m.box[0] + m.w for m in maps); maxy = max(m.box[1] + m.h for m in maps)
    W, H = maxx - minx, maxy - miny
    img = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0)); lay = []
    own_all = np.zeros((H, W), bool); owner = np.full((H, W), -1, int)
    for i_, m in enumerate(maps):
        ox_, oy_ = int(m.box[0] - minx), int(m.box[1] - miny); own_all[oy_:oy_ + m.h, ox_:ox_ + m.w] |= m.mine
        owner[oy_:oy_ + m.h, ox_:ox_ + m.w][m.mine] = i_
    def between(wx, wy, r=6):   # 북유럽 맵 두 곳 사이 틈(다른 지역으로 나가는 복도는 그리지 않음)
        o_ = owner[max(0, wy - r):wy + r + 1, max(0, wx - r):wx + r + 1]
        return len(set(o_[o_ >= 0].tolist())) >= 2
    for m in maps:
        big = Image.new('RGBA', (m.w * 16, m.h * 16), (0, 0, 0, 0))
        ox_, oy_ = int(m.box[0] - minx), int(m.box[1] - miny)
        # 2026-10-30(유저테스트: 라우가르~피오르 나루 사이가 지도에서 얼음으로 끊겨 보임): 어느 맵 땅도 아닌 출구 복도 길도 그린다
        corr = lambda x, y: (m.road[y][x] > 0 and not m.water[y][x] and not m.blocked[y][x] and not own_all[oy_ + y, ox_ + x] and between(ox_ + x, oy_ + y))
        for layer in (m.ground, m.deco, m.obj, m.high):
            for y in range(m.h):
                for x in range(m.w):
                    k = layer[y][x]
                    if k and (m.mine[y, x] or corr(x, y)):
                        try: big.alpha_composite(_tile_from_dir(k), (x * 16, y * 16))
                        except Exception: pass
        small = big.resize((m.w * S, m.h * S), Image.BOX)   # 담당 칸만 그려서 겹치는 둘레가 옆 맵을 덮지 않음
        ox, oy = m.box[0] - minx, m.box[1] - miny
        img.alpha_composite(small, (int(ox) * S, int(oy) * S))
        ys, xs = np.nonzero(m.mine)
        lay.append(dict(mapId=m.id, x=int(ox), y=int(oy), w=m.w, h=m.h, labelX=float(ox + xs.mean()), labelY=float(oy + ys.mean()),
                        own=[''.join('1' if v else '0' for v in row) for row in m.mine], out=_corridor_anchor(m)))   # 2026-10-30: 지도 붉은 점을 내 땅 안으로(출구 복도는 옆 맵 땅 위)
    img.save(R + '/Assets/PocketCaravan/Resources/WorldMap/NorthTerrain.png')
    J = lambda o, **k: json.dumps(o, ensure_ascii=False, default=lambda v: v.item() if hasattr(v, 'item') else str(v), **k)
    t1 = J(dict(width=int(W), height=int(H), maps=lay), indent=1); t2 = J(dict(maps=[life_of(m) for m in maps]))
    open(R + '/Assets/PocketCaravan/Resources/WorldMap/NorthLayout.json', 'w').write(t1)
    open(R + '/Assets/PocketCaravan/Resources/North/NorthLife.json', 'w').write(t2)
    print('M 지도', W, H, 'NorthLife', len(maps))
    print('룬돌 귀띔', rune_hints(maps)[1:])

if __name__ == '__main__':
    built = []; world_only = '--world' in sys.argv   # --world: 16장을 만들어 M 지도·NorthLife만 쓴다(맵 JSON은 따로 맵별로)
    for key in [a for a in sys.argv[1:] if not a.startswith('--')] or KEYS:
        m, rep = build(key)
        if world_only: built.append(m); continue
        miss = export(m)
        try: m.render(OUT4 + f'/{key}.png')
        except Exception as ex: print('render 실패', ex)
        built.append(m)
        n5r = rep.get('n5', {})
        print(key, m.w, m.h, '옮김', len(n5r.get('placed', [])), '못옮김', n5r.get('dropped'), '낚시', n5r.get('fishing'), '동물', n5r.get('critters'), rep.get('n5after'), 'missing', len(miss))
        print('   소품', len(rep.get('dressed', [])), '길냄', len(rep.get('opened', {}).get('opened', [])), '옮긴사람', len(rep.get('moved', [])), '숲속', rep.get('n5', {}).get('in_forest'), '채움', rep.get('filled'), '못감', len(rep.get('unreach', [])), sorted({(u[0], str(u[1])[:24]) for u in rep.get('unreach', [])})[:8], '막힘', rep.get('opened', {}).get('stuck'), '이정표', len(rep.get('signs', [])), rep.get('signMissing'), '길안내', {k_: (len(v_) if isinstance(v_, (list, dict)) else v_) for k_, v_ in rep.get('roads', {}).items()}, '안내판', rep.get('boards'), '꼬리', rep.get('tails'), '경계치움', len(rep.get('edgeClear', [])), '경계남음', rep.get('edgeStuck'), '건물밂', rep.get('lotShift'), '잘림고침', rep.get('cutFixed'), '눈싸움', rep.get('snowArena'), '룬돌', rep.get('runehill'), '대상', rep.get('caravan'), '좌판정리', rep.get('stallFix'), '꿀벌', rep.get('bees'), '더하기', rep.get('extras'))
    if len(built) == len(KEYS): world_outputs(built)
