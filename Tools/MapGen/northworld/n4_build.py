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
    def free(x, y):
        return ((x, y) in reach and m.inside(x, y + 1) and not m.blocked[y][x] and m.obj[y][x] is None and m.high[y][x] is None
                and m.road[y][x] == 0 and (x, y) not in occ and (x, y) not in zone and 2 <= x < m.w - 2 and y >= 2)
    for o in m.objects:
        if o['kind'] not in LABELED: continue
        x, y = o['x'], o['y']
        bad_reach = o['kind'] in ('talker', 'npc') and not any((x + a, y + b) in reach for a, b in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)))
        if not clash(x, y) and not bad_reach and (x, y) not in zone: placed.append((x, y)); continue
        best = None
        for r in range(1, 9):
            for yy in range(y - r, y + r + 1):
                for xx in range(x - r, x + r + 1):
                    if max(abs(xx - x), abs(yy - y)) != r or not free(xx, yy) or clash(xx, yy): continue
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
    for s_ in SEAMS.get(key, []):
        cy_, cx_ = s_['cell']; sx_, sy_ = cx_ - x0, cy_ - y0; no_widen[max(0, sy_):max(0, sy_ + s_['h']), max(0, sx_):max(0, sx_ + s_['w'])] = True
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
    lots = []
    for it in items:
        a = it.get('art'); x, y = L(it['y'], it['x'])
        if not a or a.startswith('Minigame:'): continue
        name = 'new_' + a.split(':')[1].split('_')[0] if a.startswith('new:') else a
        if name == 'laundry': m.laundry(x, y); continue
        if it['kind'] in ('lot', 'cliff'):
            if m.stamp(name, x, y, force=True, over_road=True): lots.append(it)
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
        cy, cx = s['cell']; sx, sy = cx - x0, cy - y0
        taken[max(0, sy - 1):max(0, sy + s['h'] + 1), max(0, sx - 1):max(0, sx + s['w'] + 1)] = True
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
    # 6) 숲(길·자리·물에서 1칸 띄움). 심을 칸을 먼저 모은 뒤, 갈 수 없는 자리가 생기지 않게 길을 내고 심는다
    pending = []   # (칸 집합, 나무 종류, 비울 수 있나)
    forest = set()
    for yy, xx in zip(*np.nonzero((sub == FOREST) & ~road)):
        if taken[yy, xx] or keep[yy, xx] or any(0 <= yy + b < Hd and 0 <= xx + a < Wd and (road[yy + b, xx + a] or m.road[yy + b][xx + a]) for a in (-1, 0, 1) for b in (-1, 0, 1)): continue
        if m.obj[yy][xx] or m.high[yy][xx] or m.water[yy][xx] or not mine[yy, xx]: continue
        forest.add((xx, yy))
    north = not key.startswith('north_c_')
    pending.append((forest, nt.DENSE_KINDS['pine' if north else 'mixed'], True))
    pending.append(({(xx, yy) for yy, xx in zip(*np.nonzero(sub == VOID))}, nt.DENSE_KINDS['pine' if north else 'mixed'], False))   # 다른 지역 땅: 숲 벽
    # 담당 밖 북유럽 땅의 숲·산도 그림으로(막힌 칸)
    pending.append(({(xx, yy) for yy, xx in zip(*np.nonzero(np.isin(sub, [FOREST, MOUNTAIN, PEAK]) & ~mine & ~road))}, nt.DENSE_KINDS['pine' if north else 'mixed'], False))
    # 산·빙하: 절벽 대신 바위·눈 바위·눈 나무 덩어리
    mtn_cells = {(xx, yy) for yy, xx in zip(*np.nonzero(mtn & mine)) if not keep[yy, xx] and not taken[yy, xx] and m.obj[yy][xx] is None and m.high[yy][xx] is None
                 and not m.water[yy][xx] and m.road[yy][xx] == 0 and not m.reserved[yy][xx]}
    pending.append((mtn_cells, ['rock', 'snowboulder', 'rock', 'snowboulder', 'snowpine', 'snowpine_g', 'snowround', 'deadtree'], True))
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
        m._edge_targets.append((f"{s['kind']}→{s.get('target') or s.get('id')}", {c for c in cells if 0 <= c[0] < Wd and 0 <= c[1] < Hd and mine[c[1], c[0]]}))
    for e in N1R['exits']:
        if e['map'] == key: ex, ey = L(e['at'][1], e['at'][0]); m._edge_targets.append(('지역출구 ' + e['label'], {(ex + a, ey + b) for a in (-2, -1, 0, 1, 2) for b in (-2, -1, 0, 1, 2)}))
    targets = reach_targets(m, items, L, P5)
    # 2026-10-29(사용자: 눈숲 고갯길로 가면 갇힘): 경계 출입구·도착 자리도 길과 이어지게
    for s in SEAMS.get(key, []):
        cy, cx = s['cell']; sx, sy = cx - x0, cy - y0
        targets.append(('경계 ' + s.get('target', s.get('id', '')), {(sx + a, sy + b) for a in range(s['w']) for b in range(s['h'])}))
    report['opened'] = open_paths(m, mine, pending, targets)
    for (mask, kinds, _) in pending:
        real_plant({c for c in mask if m.inside(*c)}, kinds)
    plan = set().union(*[p[0] for p in pending])
    props = [(x, y) for (x, y) in opens if open_(x, y) and (x, y) not in plan and far_road(x, y, 2)]
    m.snow_props(rn.sample(props, min(len(props), len(opens) // 70)))
    # 2026-10-29(갇힘): 눈 더미(1칸 소품)가 좁은 목을 막아 주머니 땅이 생기면, 막은 눈 더미만 치운다
    report['unplug'] = unplug_props(m, mine, targets)
    tuft_ok = [(x, y) for (x, y) in opens if open_(x, y) and (x, y) not in plan]
    m._late.extend(((x, y), rn.choice(nt.TUFTS)) for (x, y) in rn.sample(tuft_ok, min(len(tuft_ok), len(opens) // 18)))
    # 7) 바닥 조각: 밭·자갈은 흙, 늪은 녹은 풀
    # 2026-10-29(사용자): 밭이 큰 갈색 흙 덩어리로 보여서 흙 바닥은 깔지 않는다(눈 덮인 밭)
    m.ground_patch([(xx, yy) for yy, xx in zip(*np.nonzero((sub == BOG) & ~road & mine)) if not m.blocked[yy][xx]], 'grass')
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
            o = old_talker(key, it['name'])
            if o: m.talker(x, y, it['name'], o.get('character', ''), *o.get('lines', []))
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
    # 다른 지역 출구: region_links.json에서 북유럽 쪽("by")은 이 생성기가 만든다. 폭은 상대 쪽과 같게, 방향은 links의 edge
    for lk in LINKS:
        if lk['a']['map'] != key: continue
        e = next((e for e in N1R['exits'] if e['map'] == key and e['label'].startswith(lk['id'])), None)
        if not e: report.setdefault('linkMissing', []).append(lk['id']); continue
        b = lk['b']; edge = lk['a']['edge']; n = b['h'] if edge in 'EW' and b['edge'] in 'EW' else b['w'] if b['edge'] in 'NS' else b['h']
        ax, ay = L(e['at'][1], e['at'][0])
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
    report['n5after'] = n5.after_finish(m, key, mine, None, NAMES, report['anchor'], rng_seed=i)
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
    report['moved'] = tidy_people(m, mine)
    report['unreach'] = final_check(m, mine)
    return m, report

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
        stalls=ms[0], crowd=ms[1], inns=inn_spots(m), keepers=keeper_spots(m), keeperStall=keeper_stalls(m), stallArt=stall_art(m),
        sides=side_spots(m), spitters=spit_sellers(m),
        craft=[v for i, k in enumerate(("craft_fish", "craft_fur", "craft_brew")) for c in spot_cells(m, k) for v in (c[0], c[1], i)],
        yard=[v for i, k in enumerate(("yard_fish", "yard_fur", "yard_brew")) for (kk, x, y) in getattr(m, "spots", []) if kk == k for v in (x, y, i)],
        market=[v for o in m.objects if o["kind"] == "market" for v in nearest_walk(m, o["x"] + 2, o["y"] + 1, avoid={(o["x"], o["y"])})],
        sledTrack=[[x, y] for (x, y) in getattr(m, "sled_track", []) or []],
        critters=m.life)

def world_outputs(maps):
    """M 지도(Resources/WorldMap/NorthTerrain.png, NorthLayout.json)와 NorthLife.json을 새 맵으로"""
    S = 4
    minx = min(m.box[0] for m in maps); miny = min(m.box[1] for m in maps)
    maxx = max(m.box[0] + m.w for m in maps); maxy = max(m.box[1] + m.h for m in maps)
    W, H = maxx - minx, maxy - miny
    img = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0)); lay = []
    for m in maps:
        big = Image.new('RGBA', (m.w * 16, m.h * 16), (0, 0, 0, 0))
        for layer in (m.ground, m.deco, m.obj, m.high):
            for y in range(m.h):
                for x in range(m.w):
                    k = layer[y][x]
                    if k and m.mine[y, x]:
                        try: big.alpha_composite(_tile_from_dir(k), (x * 16, y * 16))
                        except Exception: pass
        small = big.resize((m.w * S, m.h * S), Image.BOX)   # 담당 칸만 그려서 겹치는 둘레가 옆 맵을 덮지 않음
        ox, oy = m.box[0] - minx, m.box[1] - miny
        img.alpha_composite(small, (int(ox) * S, int(oy) * S))
        ys, xs = np.nonzero(m.mine)
        lay.append(dict(mapId=m.id, x=int(ox), y=int(oy), w=m.w, h=m.h, labelX=float(ox + xs.mean()), labelY=float(oy + ys.mean())))
    img.save(R + '/Assets/PocketCaravan/Resources/WorldMap/NorthTerrain.png')
    J = lambda o, **k: json.dumps(o, ensure_ascii=False, default=lambda v: v.item() if hasattr(v, 'item') else str(v), **k)
    t1 = J(dict(width=int(W), height=int(H), maps=lay), indent=1); t2 = J(dict(maps=[life_of(m) for m in maps]))
    open(R + '/Assets/PocketCaravan/Resources/WorldMap/NorthLayout.json', 'w').write(t1)
    open(R + '/Assets/PocketCaravan/Resources/North/NorthLife.json', 'w').write(t2)
    print('M 지도', W, H, 'NorthLife', len(maps))

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
        print('   소품', len(rep.get('dressed', [])), '길냄', len(rep.get('opened', {}).get('opened', [])), '옮긴사람', len(rep.get('moved', [])), '숲속', rep.get('n5', {}).get('in_forest'), '채움', rep.get('filled'), '못감', len(rep.get('unreach', [])), sorted({(u[0], str(u[1])[:24]) for u in rep.get('unreach', [])})[:8], '막힘', rep.get('opened', {}).get('stuck'), '경계치움', len(rep.get('edgeClear', [])), '경계남음', rep.get('edgeStuck'), '건물밂', rep.get('lotShift'))
    if len(built) == len(KEYS): world_outputs(built)
