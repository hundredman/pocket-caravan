"""북유럽 N1 지형 검토안: 고려 Tools/WorldLayout/terrain_v3.py와 같은 방식(높이 → 강 → 지형 종류 → 마을 범위 → 길 → 길·마을 기준 맵 경계).
N0(ArtReview/North-N0/n0_masks.npz)의 해안선, 담당 범위, 맵 목표 면적을 그대로 쓴다.

북유럽다운 점
- 본토(피오르 반도): 등뼈 산맥(룬돌 언덕 → 라우가르와 얼음 동굴 사이 → 피오르 나루 북쪽), 높은 곳은 만년설과 빙하, 낮은 곳은 툰드라와 침엽수림
- 라우가르: 온천 호수. 얼음 동굴: 빙하 골짜기. 순록 고원: 높고 평평한 고원. 피오르 나루: 피오르 안쪽 물가
- 중앙 북쪽: 남쪽으로 갈수록 따뜻함(침엽수 → 풀밭과 밭). 개울 하나가 북동 바다로 흐름(개울 나루, 개울 다리길)
본편 Maps/·Scenes/는 바꾸지 않는다. 결과: ArtReview/North-N1/
"""
import heapq, json, math, sys
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image, ImageDraw, ImageFont

N0 = Path(sys.argv[1]); OUT = Path(sys.argv[2]); FONT = sys.argv[3]
OUT.mkdir(parents=True, exist_ok=True)
SEED = 11
Z = np.load(N0 / 'n0_masks.npz'); X0, Y0 = int(Z['X0']), int(Z['Y0'])
north_land, north, cnorth, central_land, east = (Z[k] for k in ('north_land', 'north', 'cnorth', 'central_land', 'east'))
own_main0, own_cen0 = Z['own_main'], Z['own_cen']
SEEDS = json.loads((N0 / 'n0_seeds.json').read_text())
H, W = north.shape
def idx(x, y): return int(round(y - Y0)), int(round(x - X0))
def wxy(p): return (p[1] + X0, p[0] + Y0)

VOID, DEEP, SHALLOW, GRAVEL, ROCKCOAST, RIVER, BOG, FIELD, GRASS, TUNDRA, HILL, FOREST, SNOW, MOUNTAIN, PEAK, LAKE, SPRING = range(17)
NAMES = ['밖', '깊은 바다', '얕은 바다', '자갈 해변', '바위 해안', '강', '늪', '밭', '풀밭', '툰드라', '언덕', '침엽수림', '눈밭',
         '산(통행 불가)', '빙하·만년설(통행 불가)', '호수', '온천']
COLORS = {DEEP: (52, 98, 140), SHALLOW: (96, 156, 186), GRAVEL: (196, 190, 172), ROCKCOAST: (120, 118, 112), RIVER: (84, 146, 196),
          BOG: (104, 120, 92), FIELD: (196, 176, 96), GRASS: (122, 160, 96), TUNDRA: (156, 164, 124), HILL: (128, 140, 98),
          FOREST: (40, 82, 64), SNOW: (226, 232, 236), MOUNTAIN: (118, 110, 106), PEAK: (206, 226, 242), LAKE: (70, 136, 186),
          SPRING: (110, 200, 196), VOID: (28, 34, 40)}
BLOCKED = {DEEP, SHALLOW, RIVER, MOUNTAIN, PEAK, ROCKCOAST, LAKE, SPRING}
ROAD_COST = {GRASS: 1.0, TUNDRA: 1.1, FIELD: 1.25, GRAVEL: 1.7, HILL: 2.6, SNOW: 3.0, FOREST: 3.4, BOG: 5.0, RIVER: 22.0, MOUNTAIN: 14.0}

KEYS = [s['key'] for s in SEEDS]; NAME = {s['key']: s['name'] for s in SEEDS}
TARGET = {s['key']: s['area'] for s in SEEDS}
REGION = {s['key']: ('main' if s['kind'] == 'main' else 'cen') for s in SEEDS}
TOWNS = {  # 마을 범위 목표 면적 (고려: 수도 1,100 · 마을 600~700 · 중앙 750~850)
    'north_birka': 850, 'north_laugar': 600, 'north_runehill': 520, 'north_icecave': 520, 'north_naru': 600,
    'north_herd': 560, 'north_hunter': 520,
    'north_c_gatetown': 800, 'north_c_snowwood': 800, 'north_c_creek': 800, 'north_c_crossing': 850}
ROADS = [  # (출발, 도착, 지나는 이동 구간 맵)
    ('north_birka', 'north_laugar', None), ('north_laugar', 'north_runehill', None), ('north_birka', 'north_herd', None),
    ('north_herd', 'north_laugar', None), ('north_herd', 'north_hunter', None), ('north_birka', 'north_hunter', None),
    ('north_birka', 'north_naru', None), ('north_naru', 'north_icecave', None),
    ('north_birka', 'north_c_gatetown', None),                       # 국경 고개(목)를 지나는 길
    ('north_c_gatetown', 'north_c_snowwood', 'north_c_road_nw'), ('north_c_gatetown', 'north_c_creek', 'north_c_road_ne'),
    ('north_c_gatetown', 'north_c_crossing', 'north_c_runeway'), ('north_c_snowwood', 'north_c_crossing', 'north_c_road_sw'),
    ('north_c_creek', 'north_c_crossing', 'north_c_road_se')]
COAST_EXIT = {'north_c_road_ne'}   # 고려 출구는 해안 쪽으로(2026-10-28 결정)
PASSES = {('north_naru', 'north_icecave'), ('north_c_gatetown', 'north_c_snowwood')}   # 산을 깎은 눈 덮인 고개(2026-10-28 결정)
EXITS = [  # 다른 지역으로 나가는 자리: (맵, 바깥 지역, 바라는 세계 좌표, 이름)
    ('north_c_road_sw', 'other', (-80, -72), '⑨ 베네치아 (venice_c_pass)'),
    ('north_c_road_ne', 'east', (70, -150), '고려 북동 포구'),
    ('north_c_crossing', 'other', (0, -12), '⑤ 한가운데 초원')]


def noise(shape, scale, rng, octaves=4):
    out = np.zeros(shape); amp = 1; total = 0
    for o in range(octaves):
        s = max(2, int(shape[0] / scale * 2 ** o) + 2), max(2, int(shape[1] / scale * 2 ** o) + 2)
        g = rng.random(s)
        z = ndimage.zoom(g, (shape[0] / s[0] * 1.02, shape[1] / s[1] * 1.02), order=3)[:shape[0], :shape[1]]
        if z.shape != shape: z = np.pad(z, ((0, shape[0] - z.shape[0]), (0, shape[1] - z.shape[1])), mode='edge')
        out += amp * z; total += amp; amp *= .5
    out /= total; return (out - out.min()) / (out.max() - out.min() + 1e-9)


def draw_line(mask, a, b):
    n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1])) * 2) + 1
    for i in range(n + 1):
        t = i / n; iy, ix = idx(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
        if 0 <= iy < H and 0 <= ix < W: mask[iy, ix] = True


def astar(start, goal, cost, blocked=None):
    dirs = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)]
    gy, gx = goal; best = {start: 0.0}; parent = {start: None}; q = [(0.0, 0.0, start)]; minc = float(cost.min())
    while q:
        _, g, p = heapq.heappop(q)
        if g != best[p]: continue
        if p == goal:
            path = []
            while p is not None: path.append(p); p = parent[p]
            return path[::-1]
        y, x = p
        for dy, dx in dirs:
            ny, nx = y + dy, x + dx
            if not (0 <= ny < H and 0 <= nx < W): continue
            if blocked is not None and blocked[ny, nx] and (ny, nx) != goal: continue
            if dy and dx and blocked is not None and (blocked[y + dy, x] or blocked[y, x + dx]): continue
            v = g + math.hypot(dy, dx) * (cost[y, x] + cost[ny, nx]) / 2
            if v < best.get((ny, nx), math.inf):
                best[(ny, nx)] = v; parent[(ny, nx)] = p
                heapq.heappush(q, (v + minc * math.hypot(ny - gy, nx - gx), v, (ny, nx)))
    raise ValueError('경로 없음')


def smooth_path(raw, blocked, water=None, rounds=4):
    if len(raw) < 5: return raw
    pts = [(float(y), float(x)) for y, x in raw[::3]]
    if pts[-1] != (float(raw[-1][0]), float(raw[-1][1])): pts.append((float(raw[-1][0]), float(raw[-1][1])))
    for _ in range(rounds):
        new = [pts[0]]
        for a, b in zip(pts, pts[1:]): new += [(a[0] * .75 + b[0] * .25, a[1] * .75 + b[1] * .25), (a[0] * .25 + b[0] * .75, a[1] * .25 + b[1] * .75)]
        new.append(pts[-1]); pts = new
    out = []
    for a, b in zip(pts, pts[1:]):
        n = max(1, int(math.hypot(b[0] - a[0], b[1] - a[1]) * 3))
        for i in range(n):
            p = (int(round(a[0] + (b[0] - a[0]) * i / n)), int(round(a[1] + (b[1] - a[1]) * i / n)))
            if not out or out[-1] != p: out.append(p)
    out.append(raw[-1])
    if any(blocked[p] for p in out): return raw
    if water is not None and sum(water[p] for p in out) > sum(water[p] for p in raw): return raw
    filled = [out[0]]
    for p in out[1:]:
        q = filled[-1]
        if abs(p[0] - q[0]) == 1 and abs(p[1] - q[1]) == 1:
            filled.append((q[0], p[1]) if not blocked[q[0], p[1]] else (p[0], q[1]))
        filled.append(p)
    return filled


rng = np.random.default_rng(SEED)
owned = north | cnorth
land_full = north_land | central_land
land = land_full & owned
sea = owned & ~land
region = np.full((H, W), '', object); region[north] = 'main'; region[cnorth] = 'cen'
main = north; cen = cnorth
seed_xy = {s['key']: (s['x'], s['y']) for s in SEEDS}

# 반도 몸통 (N0과 같은 식): 중심 BY, 반지름 RX·RY
ys_, xs_ = np.nonzero(north_land); BY = float((ys_.min() + Y0 + -200) / 2); RY = (-200 - (ys_.min() + Y0)) / 2; RX = RY * 92 / 105
def P(fx, fy): return (4 + fx * RX, BY + fy * RY)

# ---------- 1) 높이 ----------
to_sea = ndimage.distance_transform_edt(land_full)
ridge = np.zeros((H, W), bool); hills = np.zeros((H, W), bool)
RIDGES = [  # 본토 등뼈 산맥: 룬돌 언덕 서북 → 라우가르와 얼음 동굴 사이 → 피오르 나루 북쪽
    [P(0.18, -0.92), P(0.24, -0.62), P(0.30, -0.36), P(0.32, -0.12), P(0.30, 0.16), P(0.40, 0.36)],
    [P(0.30, -0.28), P(0.55, -0.42), P(0.78, -0.30)],            # 얼음 동굴을 둘러싼 북쪽 곁줄기(빙하 골짜기)
    [P(-0.80, -0.50), P(-0.62, -0.62), P(-0.40, -0.70)]]         # 서북 해안 산
HILLS = [  # 중앙 북쪽의 낮은 언덕줄기(눈숲 고갯길 고개, 북문 동쪽 언덕)
    [(-70, -150), (-52, -138), (-30, -132)], [(14, -168), (24, -150), (22, -128)], [(-20, -95), (-8, -80)]]
jit = noise((H, W), 18, rng, 3)
def trace(groups, mask, branch=True):
    for g in groups:
        dense = []
        for a, b in zip(g, g[1:]):
            for i in range(12): t = i / 12; dense.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
        dense.append(g[-1])
        for a, b in zip(dense, dense[1:]):
            vx, vy = b[0] - a[0], b[1] - a[1]; L = math.hypot(vx, vy) or 1; px, py = -vy / L, vx / L
            def off(p):
                iy, ix = idx(*p); iy = min(max(iy, 0), H - 1); ix = min(max(ix, 0), W - 1); return (jit[iy, ix] - .5) * 7
            draw_line(mask, (a[0] + px * off(a), a[1] + py * off(a)), (b[0] + px * off(b), b[1] + py * off(b)))
        if branch:
            for a in dense[2:-2:5]:
                ang = rng.uniform(0, 2 * math.pi); L = rng.uniform(5, 11)
                draw_line(mask, a, (a[0] + math.cos(ang) * L, a[1] + math.sin(ang) * L))
trace(RIDGES, ridge); trace(HILLS, hills, branch=False)
d_ridge = ndimage.distance_transform_edt(~ridge); d_hill = ndimage.distance_transform_edt(~hills)
broad = noise((H, W), 55, rng, 4); fine = noise((H, W), 14, rng, 3)
elev = (.60 * np.exp(-(d_ridge / 7) ** 2) + .34 * np.exp(-(d_ridge / 22) ** 2) + .30 * np.exp(-(d_hill / 9) ** 2)
        + .16 * np.minimum(to_sea / 30, 1) + .34 * broad + .09 * fine)
# 순록 고원: 높고 평평한 땅
hx, hy = seed_xy['north_herd']; hy_, hx_ = idx(hx, hy); yy, xx = np.ogrid[:H, :W]
plateau = np.exp(-(((yy - hy_) ** 2 + (xx - hx_) ** 2) / 26 ** 2))
elev = elev * (1 - .6 * plateau) + .50 * plateau
# 위도: 북쪽일수록 춥다(지형 종류 정할 때 씀)
cold = np.clip((-120 - (yy + Y0)) / 220, 0, 1)

# ---------- 2) 강 ----------
river = np.zeros((H, W), bool); river_info = []
def run_river(label, src_xy, mouth_xy, via=None):
    s = idx(*src_xy); my, mx = idx(*mouth_xy)
    sy, sx = np.nonzero(sea); k = int(np.argmin((sy - my) ** 2 + (sx - mx) ** 2)); g = (int(sy[k]), int(sx[k]))
    cost = 1 + 14 * np.clip(elev, 0, None) ** 2 + 3 * noise((H, W), 10, rng, 2)
    if via:   # 꼭 지나갈 자리(마을 옆)
        v = idx(*via); raw = astar(s, v, cost, blocked=~owned) + astar(v, g, cost, blocked=~owned)[1:]
    else: raw = astar(s, g, cost, blocked=~owned)
    path = smooth_path(raw, ~owned)
    out = []
    for p in path:
        out.append(p)
        if sea[p]: break
    n = len(out)
    for i, (y, x) in enumerate(out):
        rad = .75 + 1.7 * (i / n) ** 1.3; rr = int(math.ceil(rad))
        for dy in range(-rr, rr + 1):
            for dx in range(-rr, rr + 1):
                if dy * dy + dx * dx <= rad * rad and 0 <= y + dy < H and 0 <= x + dx < W and land[y + dy, x + dx]: river[y + dy, x + dx] = True
    river_info.append(dict(label=label, cells=n, source=list(map(round, wxy(out[0]))), mouth=list(map(round, wxy(out[-1])))))
run_river('얼음강 (빙하 → 피오르 나루 앞바다)', P(0.52, -0.20), P(0.62, 0.95))
run_river('북쪽 개울 (북문 동쪽 언덕 → 개울 나루 마을 옆 → 북동 바다)', (16, -128), (100, -140), via=(43, -112))   # 2026-10-29: 개울 나루 마을 옆을 지나게
d_river = ndimage.distance_transform_edt(~river)
elev = elev - .2 * np.exp(-(d_river / 6) ** 2)
elev = (elev - elev[land].min()) / (elev[land].max() - elev[land].min())

# ---------- 3) 지형 종류 ----------
t = np.full((H, W), VOID, int)
to_land = ndimage.distance_transform_edt(~land_full)
t[sea] = DEEP; t[sea & (to_land <= 3)] = SHALLOW
forest_n = noise((H, W), 16, rng, 4); bog_n = noise((H, W), 7, rng, 2); beach_n = noise((H, W), 12, rng, 2)
L = land.copy(); q = lambda v, m=L: float(np.quantile(elev[m], v))
Lm, Lc = L & main, L & cen
# 본토: 툰드라 바탕, 침엽수림은 낮은 골짜기, 높은 곳은 눈밭 → 산 → 빙하
t[Lm] = TUNDRA
t[Lm & (elev < q(.45, Lm)) & (forest_n > .50)] = FOREST
t[Lm & (elev > q(.62, Lm))] = HILL
t[Lm & (elev > q(.62, Lm)) & (forest_n > .62)] = FOREST
t[Lm & (elev > q(.74, Lm))] = SNOW
t[Lm & (elev > q(.86, Lm))] = MOUNTAIN
t[Lm & (elev > q(.93, Lm))] = PEAK
t[Lm & (plateau > .5) & np.isin(t, [FOREST, MOUNTAIN, PEAK, SNOW])] = TUNDRA      # 고원은 트인 땅
t[Lm & (plateau > .5) & (elev > q(.80, Lm))] = HILL
# 중앙 북쪽: 풀밭 바탕, 북쪽은 침엽수림이 많고 남쪽은 풀밭·밭
t[Lc] = GRASS
t[Lc & (forest_n > .62 - .18 * cold)] = FOREST
t[Lc & (elev > q(.80, Lc))] = HILL
t[Lc & (elev > q(.80, Lc)) & (forest_n > .5)] = FOREST
t[Lc & (elev > q(.95, Lc))] = MOUNTAIN
t[Lc & (cold > .2) & (elev > q(.90, Lc))] = SNOW
near = np.zeros((H, W), bool)
for k in ('north_c_crossing', 'north_c_creek', 'north_c_snowwood', 'north_birka'):
    ty, tx = idx(*seed_xy[k]); near |= ((yy - ty) ** 2 + (xx - tx) ** 2) < 24 ** 2
t[L & near & (elev < q(.45)) & (d_river < 14) & (forest_n < .62) & ~np.isin(t, [MOUNTAIN, PEAK])] = FIELD
# 해안: 낮으면 자갈 해변, 높으면 바위 해안(피오르 벽)
coast = L & (to_sea <= 2)
t[coast & (elev < .42) & (beach_n > .45)] = GRAVEL
t[coast & ((elev >= .48) | Lm & (beach_n < .35)) & (to_sea <= 1)] = ROCKCOAST
# 하구·골짜기 늪
for ri in river_info:
    my, mx = idx(*ri['mouth']); t[L & ((yy - my) ** 2 + (xx - mx) ** 2 < 14 ** 2) & (d_river <= 5) & (d_river > 0) & (elev < .35) & (bog_n > .5)] = BOG
t[river] = RIVER
# 라우가르 온천: 마을 북쪽 가까이 작은 김 나는 호수(약 110칸) + 둘레 늪
spring = np.zeros((H, W), bool)
ly, lx = idx(*seed_xy['north_laugar']); cy, cx = ly - 11, lx + 6
sw = noise((H, W), 6, rng, 3) - .5
rr = np.sqrt(((yy - cy) / 1.25) ** 2 + ((xx - cx) / .85) ** 2) + 3 * sw
spring = (rr < 5.9) & L & ~river
t[spring] = SPRING
t[ndimage.binary_dilation(spring, iterations=2) & ~spring & L & ~river & (bog_n > .55)] = BOG
# 동남 물가길 연못(2026-10-29): 길가 작은 호수와 흘러드는 개울. 다른 무작위 값이 바뀌지 않게 따로 뽑는다
rngp = np.random.default_rng(SEED + 7)
pond = np.zeros((H, W), bool)
py0, px0 = idx(46, -70)
wp = noise((H, W), 6, rngp, 3) - .5
rp = np.sqrt(((yy - py0) / .9) ** 2 + ((xx - px0) / 1.3) ** 2) + 3.5 * wp
pond = (rp < 6.2) & L & cen & ~river
lp, npd = ndimage.label(pond)
if npd > 1: pond = lp == (1 + int(np.argmax(ndimage.sum(pond, lp, range(1, npd + 1)))))
t[pond] = LAKE
src = idx(56, -84)
seg = astar(src, tuple(np.argwhere(pond)[np.argmin(((np.argwhere(pond) - src) ** 2).sum(1))]), 1 + 10 * elev + 3 * noise((H, W), 8, rngp, 2), blocked=~(L & cen))
for (y_, x_) in seg[:-1]:
    if not pond[y_, x_]: t[y_, x_] = RIVER; river[y_, x_] = True
t[ndimage.binary_dilation(pond, iterations=2) & ~pond & L & ~river & (bog_n > .5)] = BOG
lake = spring | pond
river_info.append(dict(label='동남 물가길 연못 개울', cells=len(seg), pondCells=int(pond.sum())))

# ---------- 4) 마을 범위 ----------
flat = ndimage.generic_filter(elev, np.std, size=5)
passable = land & ~np.isin(t, list(BLOCKED))
COASTAL = {'north_naru', 'north_hunter'}
VSHIFT = {'north_hunter': (-14, 10), 'north_naru': (14, 8), 'north_runehill': (0, -6), 'north_herd': (-6, -6), 'north_birka': (0, 4)}   # 마을 자리를 맵 씨앗에서 조금 옮김(바닷가 쪽)
village = np.zeros((H, W), bool); vlabel = np.full((H, W), -1, int); villages = []
shape_n = noise((H, W), 9, rng, 2)
for vi, (key, target) in enumerate(TOWNS.items()):
    sx0, sy0 = seed_xy[key]; dx0, dy0 = VSHIFT.get(key, (0, 0)); oy, ox = idx(sx0 + dx0, sy0 + dy0); reg = REGION[key]
    R = 18 if key in COASTAL else 13
    cand = passable & (((yy - oy) ** 2 + (xx - ox) ** 2) <= R * R) & (region == reg) & ~village
    score = 4 * flat + 1.2 * elev + .02 * np.sqrt((yy - oy) ** 2 + (xx - ox) ** 2) + .6 * (t == FOREST) + .8 * (t == BOG) + .5 * (t == SNOW)
    if key in COASTAL: score = score + .12 * np.abs(to_sea - 5)
    if key == 'north_laugar': score = score + .1 * np.abs(ndimage.distance_transform_edt(~spring) - 6)
    if key == 'north_c_creek': score = score + .25 * np.abs(np.minimum(ndimage.distance_transform_edt(~river), 30) - 4)   # 개울에서 4칸쯤
    score = np.where(cand, score, np.inf); s = np.unravel_index(int(np.argmin(score)), score.shape)
    got = {s}; pq = [(0.0, s)]; seen = {s}; okreg = region == reg
    while pq and len(got) < target:
        _, p = heapq.heappop(pq); got.add(p)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            n = (p[0] + dy, p[1] + dx)
            if n in seen or not (0 <= n[0] < H and 0 <= n[1] < W): continue
            seen.add(n)
            if not okreg[n] or not land[n] or t[n] in (PEAK, MOUNTAIN, ROCKCOAST, RIVER, LAKE, SPRING) or village[n]: continue
            d = math.hypot(n[0] - s[0], n[1] - s[1])
            heapq.heappush(pq, (d * (1 + .35 * (shape_n[n] - .5)) + 5 * max(0, elev[n] - .5) + 2 * flat[n], n))
    m = np.zeros((H, W), bool)
    for p in got: m[p] = True
    m = ndimage.binary_closing(ndimage.binary_opening(m, iterations=1), iterations=2) & land & ~river & ~np.isin(t, [PEAK, MOUNTAIN, LAKE, SPRING])
    village |= m; vlabel[m] = vi
    villages.append(dict(key=key, label=NAME[key], target=target, area=int(m.sum()), center=[int(s[1] + X0), int(s[0] + Y0)],
                         moved=round(math.hypot(s[0] - oy, s[1] - ox), 1)))
t[village & main & np.isin(t, [FOREST, HILL, SNOW, BOG])] = TUNDRA
t[village & cen & np.isin(t, [FOREST, HILL, SNOW, BOG])] = GRASS
vcenter = {v['key']: idx(*v['center']) for v in villages}

# ---------- 5) 길 ----------
rng2 = np.random.default_rng(SEED + 1)
wig = noise((H, W), 26, rng2, 3); fine2 = noise((H, W), 8, rng2, 2)
gy, gx = np.gradient(ndimage.gaussian_filter(elev, 1.5)); slope = np.hypot(gy, gx); slope /= slope[land].max()
cost = np.full((H, W), 1e3); blocked = np.ones((H, W), bool)
for k, v in ROAD_COST.items(): m = t == k; cost[m] = v; blocked[m] = False
cost[village] = .7; blocked[village] = False
cost = cost * (1 + 1.1 * wig ** 2 + .25 * fine2) + 6 * slope
d_wat = ndimage.distance_transform_edt(~np.isin(t, [RIVER, LAKE, SPRING]))
cost += np.where((d_wat > 0) & (d_wat <= 6), 3.5 * (7 - d_wat) / 6, 0)
road = np.zeros((H, W), bool); records = []; paths = []
order = sorted(ROADS, key=lambda r: math.dist(seed_xy[r[0]], seed_xy[r[1]]))
for a, b, via in order:
    c = cost.copy(); c[road] *= .7
    if via:
        k = KEYS.index(via); own0 = own_cen0 if REGION[via] == 'cen' else own_main0
        ci = [i for i, s in enumerate(SEEDS) if s['kind'] != 'main'].index(k) if REGION[via] == 'cen' else k
        cand = np.argwhere((own0 == ci) & ~blocked); cy_, cx_ = cand.mean(0)
        mid = tuple(cand[np.argmin(((cand - [cy_, cx_]) ** 2).sum(1))])
        raw = astar(vcenter[a], mid, c, blocked) + astar(mid, vcenter[b], c, blocked)[1:]
    else:
        raw = astar(vcenter[a], vcenter[b], c, blocked)
    path = smooth_path(raw, blocked, t == RIVER)
    if (a, b) in PASSES or (b, a) in PASSES:   # 눈 덮인 고개: 길과 양옆 1칸의 산을 눈밭으로 깎는다
        pm = np.zeros((H, W), bool)
        for p in path: pm[p] = True
        pm = ndimage.binary_dilation(pm, iterations=1) & np.isin(t, [MOUNTAIN, PEAK])
        pass_cells = int(pm.sum()); t[pm] = SNOW; cost[pm] = ROAD_COST[SNOW]; blocked[pm] = False
    for p in path: road[p] = True
    paths.append(path)
    records.append(dict(fromKey=a, toKey=b, via=via, cells=len(path), bridgeCells=sum(1 for p in path if t[p] == RIVER),
                        mountainPassCells=sum(1 for p in raw if t[p] == MOUNTAIN), snowCells=sum(1 for p in path if t[p] == SNOW)))
    if (a, b) in PASSES or (b, a) in PASSES: records[-1]['passCarvedCells'] = pass_cells

# ---------- 6) 다른 지역으로 나가는 길 끝: 그 맵의 길에서 담당 경계까지 (경계 나누기 전에 정해서 맵 씨앗에 넣는다) ----------
exits = []; exit_paths = {}
for mkey, other, (ex, ey), label in EXITS:
    outside = (east if other == 'east' else (central_land & ~owned & ~east))
    rim = land & (region == REGION[mkey]) & ndimage.binary_dilation(outside, iterations=1) & passable & ~village
    if mkey in COAST_EXIT:   # 해안 쪽 경계 끝(바다에서 3~6칸)으로
        rim2 = rim & (to_sea >= 3) & (to_sea <= 6)
        if rim2.any(): rim = rim2
    ty, tx = idx(ex, ey); pts = np.argwhere(rim); g = tuple(pts[np.argmin(((pts - [ty, tx]) ** 2).sum(1))])
    mypath = [p for (a_, b_, via), path in zip(order, paths) if via == mkey for p in path if not village[p]]
    if not mypath:  # 마을 맵: 그 마을 범위에서 출발
        mypath = [tuple(p) for p in np.argwhere(vlabel == list(TOWNS).index(mkey))]
    rp = np.array(mypath); s_ = tuple(rp[np.argmin(((rp - g) ** 2).sum(1))])
    c = cost.copy(); c[road] *= .7
    if mkey in COAST_EXIT:
        c = c + 1.5 * np.clip(to_sea - 5, 0, 12)                       # 바닷가를 따라가는 길
        vv = ndimage.distance_transform_edt(~village); c = c + np.where(vv < 8, 6, 0)   # 다른 마을 둘레는 피함
        rp = np.array([p for p in mypath if to_sea[p] < 30] or mypath); s_ = tuple(rp[np.argmin(((rp - g) ** 2).sum(1))])
    path = smooth_path(astar(s_, g, c, blocked), blocked, t == RIVER)
    for p in path: road[p] = True
    exit_paths[mkey] = path
    exits.append(dict(map=mkey, label=label, at=list(map(int, wxy(g))), cells=len(path)))

# ---------- 7) 맵 경계: 마을과 길 기준 (고려 partition과 같은 방식). 땅을 먼저 나누고 앞바다는 가장 가까운 땅의 맵으로 ----------
step_cost = np.ones((H, W)); step_cost[road] = .35; step_cost[sea] = 4.0   # 앞바다는 느리게 퍼짐: 바다 띠가 먼 맵으로 가지 않게
seedmask = {}
for k in KEYS:
    if k in TOWNS:   # 마을 범위 + 둘레 6칸을 씨앗으로 (경계가 마을에 바짝 붙지 않게)
        vm = vlabel == list(TOWNS).index(k)
        seedmask[k] = ndimage.binary_dilation(vm, iterations=6) & (region == REGION[k]) & land & ~(village & ~vm)
        for p in exit_paths.get(k, []): seedmask[k][p] = True
    else:
        sm = np.zeros((H, W), bool)
        for (a_, b_, via), path in zip(order, paths):
            if via == k:
                outside = [p for p in path if not village[p]]; n = len(outside); lo_, hi_ = int(n * .3), int(n * .7) + 1
                if k in exit_paths:   # 나가는 길이 갈라지는 자리까지 씨앗을 늘려 한 덩어리로
                    st = exit_paths[k][0]
                    if st in outside: j = outside.index(st); lo_, hi_ = min(lo_, j), max(hi_, j + 1)
                for p in outside[lo_:hi_]: sm[p] = True
        for p in exit_paths.get(k, []): sm[p] = True
        if k in COAST_EXIT: sm = ndimage.binary_dilation(sm, iterations=2) & ~ndimage.binary_dilation(village, iterations=6) | sm
        seedmask[k] = sm & land
    lab_, n_ = ndimage.label(seedmask[k], structure=np.ones((3, 3)))
    if n_ > 1: print('씨앗이 여러 덩어리', k, n_, file=sys.stderr)
def geodesic(seed, reg, block=None):
    dist = np.full((H, W), np.inf); q = []
    for p in zip(*np.nonzero(seed)): dist[p] = 0; q.append((0.0, p))
    heapq.heapify(q); okr = (region == reg) & owned
    if block is not None: okr = okr & ~block
    while q:
        g, p = heapq.heappop(q)
        if g != dist[p]: continue
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            n = (p[0] + dy, p[1] + dx)
            if not (0 <= n[0] < H and 0 <= n[1] < W) or not okr[n]: continue
            v = g + math.hypot(dy, dx) * (step_cost[p] + step_cost[n]) / 2
            if v < dist[n]: dist[n] = v; heapq.heappush(q, (v, n))
    return dist
allseed = np.zeros((H, W), bool)
for k in KEYS: allseed |= seedmask[k]
D = np.stack([geodesic(seedmask[k], REGION[k], allseed & ~seedmask[k]) for k in KEYS])   # 다른 맵 씨앗은 못 지나감: 조각 안 생김
for i, k in enumerate(KEYS): D[i][seedmask[k]] = -1e5
regmask = np.stack([(region == REGION[k]) & owned for k in KEYS])
def assign(wgt):
    score = np.where(regmask, D + wgt[:, None, None], np.inf); new = np.argmin(score, 0); new[~owned] = -1
    return new
tg = np.array([TARGET[k] for k in KEYS], float); wgt = np.zeros(len(KEYS))
for it in range(3000):
    new = assign(wgt)
    areas = np.bincount(new[new >= 0], minlength=len(KEYS))
    lo, hi = tg * .85, tg * 1.2     # 고려처럼 자연 경계 우선: 목표의 85~120% 안이면 둔다
    err = np.where(areas < lo, (areas - lo) / lo, np.where(areas > hi, (areas - hi) / hi, 0))
    if not np.any(err): break
    wgt += np.clip(err, -.3, .3) * 8 / (1 + it * .01)
print('partition', it, np.round(err, 3), file=sys.stderr)
def post(new):
    for _ in range(0):   # 톱니 정리(길쭉한 띠를 끊어서 뺌)
        cnt = np.stack([ndimage.uniform_filter((new == i).astype(float), size=5) for i in range(len(KEYS))])
        rm = np.stack([region == REGION[k] for k in KEYS])
        cnt = np.where(rm, cnt, -1); sm = np.argmax(cnt, 0); sm[~owned] = -1; new = sm
    for i, k in enumerate(KEYS):
        new[seedmask[k]] = i
    # 폭 2칸 이하 띠는 옆 맵으로(2026-10-29: 라우가르 맵 1칸 띠). 씨앗 칸은 그대로
    for _ in range(3):
        for i in range(len(KEYS)):
            mi = new == i
            thin = mi & ~ndimage.binary_opening(mi, structure=np.ones((3, 3))) & ~seedmask[KEYS[i]]
            for y, x in np.argwhere(thin):
                nb = new[max(0, y - 1):y + 2, max(0, x - 1):x + 2].ravel(); nb = nb[(nb >= 0) & (nb != i)]
                if len(nb): new[y, x] = np.bincount(nb).argmax()
    for i in range(len(KEYS)):
        lab, n = ndimage.label(new == i, structure=np.ones((3, 3)))
        if n > 1:
            sizes = ndimage.sum(seedmask[KEYS[i]].astype(float) * 1e6 + 1, lab, range(1, n + 1)); keep = int(np.argmax(sizes)) + 1
            print('조각', KEYS[i], n, sorted(ndimage.sum(np.ones_like(lab), lab, range(1, n + 1)).astype(int))[-3:], file=sys.stderr)
            for j in range(1, n + 1):
                if j == keep: continue
                comp = lab == j; ring = ndimage.binary_dilation(comp) & ~comp & (new >= 0) & (new != i)
                if ring.any(): new[comp] = np.bincount(new[ring]).argmax()
    return new
new = post(new)
mapid = new
for k, path in exit_paths.items(): paths.append(path)
cross = np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]])
wide = ndimage.binary_dilation(road, structure=cross) & owned & ~np.isin(t, [DEEP, SHALLOW, PEAK, MOUNTAIN, SPRING])
crossing = ndimage.binary_dilation(road & (t == RIVER), structure=cross, iterations=2)
wide &= (t != RIVER) | crossing
for rec, path in zip(records, paths):
    ids = [int(mapid[p]) for p in path]; rec['boundaryCrossings'] = sum(1 for a_, b_ in zip(ids, ids[1:]) if a_ != b_)
    rec['maps'] = sorted({KEYS[i] for i in ids if i >= 0})

# ---------- 8) 그림 ----------
def render(path, with_road=True, tint=False, title=''):
    S = 3
    rgb = np.zeros((H, W, 3), float)
    for k, c in COLORS.items(): rgb[t == k] = c
    # 담당 밖: 다른 지역 육지는 흐리게
    other = land_full & ~owned; rgb[other] = (170, 166, 150); rgb[east & land_full] = (132, 168, 104)
    seafar = ~land_full & ~owned; rgb[seafar] = (44, 86, 124)
    gy, gx = np.gradient(ndimage.gaussian_filter(elev, 1.2)); shade = np.clip(1 + (-gx - gy) * 6, .72, 1.22)
    landish = land & ~np.isin(t, [RIVER, LAKE, SPRING]); rgb[landish] *= shade[landish, None]
    vm = village; rgb[vm] = rgb[vm] * .7 + np.array((255, 236, 170)) * .3
    rgb[vm & ~ndimage.binary_erosion(vm)] = (255, 226, 110)
    if with_road:
        rgb[wide & (t == RIVER)] = (150, 110, 70); rgb[wide & (t != RIVER)] = (176, 140, 92)
    b = np.zeros((H, W), bool)
    b[:, 1:] |= (mapid[:, 1:] != mapid[:, :-1]) & (mapid[:, 1:] >= 0) & (mapid[:, :-1] >= 0)
    b[1:, :] |= (mapid[1:, :] != mapid[:-1, :]) & (mapid[1:, :] >= 0) & (mapid[:-1, :] >= 0)
    if tint:
        pal = [(255, 120, 120), (120, 200, 255), (255, 210, 90), (150, 255, 150), (220, 140, 255), (255, 160, 60), (90, 230, 210), (255, 120, 200),
               (170, 170, 255), (200, 255, 90), (255, 255, 140), (140, 200, 140), (230, 180, 140), (120, 160, 255), (255, 190, 190), (190, 255, 220)]
        for i in range(len(KEYS)): m = mapid == i; rgb[m] = rgb[m] * .62 + np.array(pal[i % len(pal)]) * .38
        b = ndimage.binary_dilation(b) & owned
    rgb[b] = (20, 20, 24) if tint else rgb[b] * .45
    rim = owned & ~ndimage.binary_erosion(owned) ; rgb[rim & land_full] = (200, 60, 50)
    ys, xs = np.nonzero(owned); y0, y1, x0, x1 = max(0, ys.min() - 6), min(H, ys.max() + 7), max(0, xs.min() - 70), min(W, xs.max() + 60)
    img = Image.fromarray(np.clip(rgb[y0:y1, x0:x1], 0, 255).astype(np.uint8)).resize(((x1 - x0) * S, (y1 - y0) * S), Image.NEAREST)
    d = ImageDraw.Draw(img); f = ImageFont.truetype(FONT, 15); fs = ImageFont.truetype(FONT, 12)
    if tint:
        for i, k in enumerate(KEYS):
            ys_, xs_ = np.nonzero(mapid == i); cy, cx = (ys_.mean() - y0) * S, (xs_.mean() - x0) * S
            if k in TOWNS:
                v = next(v for v in villages if v['key'] == k); cx, cy = (v['center'][0] - X0 - x0) * S, (v['center'][1] - Y0 - y0) * S + 20
                d.text((cx, cy), f"{len(ys_):,}칸", font=ImageFont.truetype(FONT, 13), fill=(20, 20, 20), anchor='mm', stroke_width=2, stroke_fill=(255, 255, 255)); continue
            d.text((cx, cy), f"{NAME[k]} {len(ys_):,}칸", font=ImageFont.truetype(FONT, 13), fill=(20, 20, 20), anchor='mm', stroke_width=2, stroke_fill=(255, 255, 255))
    for v in villages:
        cx, cy = (v['center'][0] - X0 - x0) * S, (v['center'][1] - Y0 - y0) * S
        d.text((cx, cy), v['label'], font=f, fill=(30, 24, 20), anchor='mm', stroke_width=3, stroke_fill=(255, 250, 230))
    for e in exits:
        if 'at' not in e: continue
        cx, cy = (e['at'][0] - X0 - x0) * S, (e['at'][1] - Y0 - y0) * S
        d.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], fill=(220, 40, 40), outline='white', width=2)
        if e['at'][0] < -20: d.text((cx - 10, cy), e['label'] + ' ←', font=fs, fill=(150, 20, 20), anchor='rm', stroke_width=2, stroke_fill='white')
        else: d.text((cx + 10, cy), '→ ' + e['label'], font=fs, fill=(150, 20, 20), anchor='lm', stroke_width=2, stroke_fill='white')
    items = [SHALLOW, GRAVEL, ROCKCOAST, RIVER, SPRING, BOG, FIELD, GRASS, TUNDRA, HILL, FOREST, SNOW, MOUNTAIN, PEAK]
    lx, ly = 8, img.height - 17 * len(items) - 34
    d.rectangle((lx - 4, ly - 26, lx + 170, img.height - 6), fill=(250, 248, 240))
    d.text((lx, ly - 22), title, font=fs, fill=(30, 30, 30))
    for i, k in enumerate(items):
        d.rectangle((lx, ly + i * 17, lx + 12, ly + i * 17 + 12), fill=COLORS[k]); d.text((lx + 18, ly + i * 17 - 1), NAMES[k], font=fs, fill=(30, 30, 30))
    i = len(items); d.rectangle((lx, ly + i * 17, lx + 12, ly + i * 17 + 12), outline=(230, 200, 90), width=2); d.text((lx + 18, ly + i * 17 - 1), '마을 범위', font=fs, fill=(30, 30, 30))
    img.save(path)
render(OUT / 'terrain-villages-roads.png', title='북유럽 N1 지형 + 마을 + 길')
render(OUT / 'maps.png', tint=True, title='맵 경계(색=맵)')
counts = {NAMES[k]: int((t == k).sum()) for k in range(1, 17)}
maps = [dict(key=k, name=NAME[k], n0Target=TARGET[k], area=int((mapid == i).sum()), land=int(((mapid == i) & land).sum())) for i, k in enumerate(KEYS)]
report = dict(seed=SEED, classCounts=counts, rivers=river_info, springCells=int(spring.sum()), villages=villages, roads=records, exits=exits, maps=maps)
(OUT / 'report.json').write_text(json.dumps(report, ensure_ascii=False, indent=1))
np.savez_compressed(OUT / 'n1_grid.npz', X0=X0, Y0=Y0, t=t, elev=elev, mapid=mapid, road=wide, village=village, vlabel=vlabel)
for m in maps: print(m['name'], m['n0Target'], m['area'], m['land'])
for r in records: print(NAME[r['fromKey']], '→', NAME[r['toKey']], r['cells'], '경계', r['boundaryCrossings'], '다리', r['bridgeCells'], '눈', r['snowCells'], '산', r['mountainPassCells'])
print(exits); print([(v['label'], v['area'], v['moved']) for v in villages])
