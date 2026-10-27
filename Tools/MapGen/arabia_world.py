"""아라비아 지역 — 오픈월드 연속 지형 (2026-10-27).

MMORPG 오픈월드 느낌을 위해 팀 고려 지역(continuous.py)과 같은 방식을 쓴다:
지형 하나(192×132)를 통째로 그린 뒤 같은 크기 조각(64×44)으로 자르고, 조각 경계는 양쪽 다 걸을 수 있는 곳이면
모두 출구로 잇는다 (문 하나가 아니라 경계 어디로든 걸어서 넘어감). 조각 경계에서 그림도 그대로 이어진다.

배치 (실제 지리 계산 추천안 ②: 반도를 돌려 수도 사나를 중앙 대륙 쪽으로)
          [  -  ] [ 사나 ] [  -  ]          맨 위 = 중앙 대륙 쪽 (사나 북문)
          [빈사막] [타이즈] [자비드]
          [아흐사] [나즈드] [ 제다 ]         왼쪽 바다 = 걸프, 오른쪽 바다 = 홍해
- 큰길: 알 아흐사(시작) → 나즈드 대상로 → 제다 → 홍해 해안 → 자비드 → 타이즈 → 고원길 → 사나 → 북문
- 질러가기: 알 아흐사 → 룹알할리 빈 사막(모래가 깊음) → 타이즈
- 중앙 대륙으로 가는 문 3곳: 사나 북문(큰 문), 빈 사막 북쪽 모래길, 자비드 북쪽 협곡길 (회의: 길이 여러 개)
회의 기준: 7조각 × 2,816칸 = 19,712칸 (2만 칸 이내). 마을 5곳의 안쪽(퀘스트 NPC·장터·게시판)은 기존 맵 그대로 옮긴다.
마을의 경제 좌표(worldX/worldY)는 팀 원칙대로 방위·거리와 무관하므로 바꾸지 않는다.
"""
import heapq
import math
import arabia_art as A
from tiles import Map
from arabia_central import vnoise, catmull, _h

CW, CH = 64, 44
FW, FH = 3 * CW, 3 * CH
CHUNKS = {  # (열, 행): (맵 ID, 이름, 종류)  — 맵 ID는 기존 것을 이어 쓴다
    (1, 0): ('arabia_sanaa', '사나', 'town'),
    (0, 1): ('arabia_shortcut', '룹알할리 빈 사막', 'wild'),
    (1, 1): ('arabia_taiz', '타이즈', 'town'),
    (2, 1): ('arabia_zabid', '자비드', 'town'),
    (0, 2): ('arabia_ahsa', '알 아흐사', 'town'),
    (1, 2): ('arabia_farmroad', '나즈드 대상로', 'wild'),
    (2, 2): ('arabia_jeddah', '제다', 'town'),
}
BY_ID = {v[0]: k for k, v in CHUNKS.items()}
HOURS = {'arabia_shortcut': 2, 'arabia_farmroad': 3}
# 중앙 대륙으로 가는 문: (조각 맵, 경계, 키, 조각 안 시작 칸)
GATES = {('arabia_sanaa', 'n_01'): 30, ('arabia_shortcut', 'n_01'): 30, ('arabia_zabid', 'n_01'): 30}
_CACHE = {}


def chunk_of(x, y):
    return (x // CW, y // CH) if 0 <= x < FW and 0 <= y < FH else None


def in_world(x, y):
    return chunk_of(x, y) in CHUNKS


class _FakeLinks(dict):
    """기존 마을 생성기가 부르는 출구를 임시 이름으로 받는다 (끝점만 알아내고 지운다)."""
    def __getitem__(self, k):
        return (k[0].upper(), 'X', 'X', f'tmp_{k}')


def _build_town(mid):
    """기존 마을 생성기를 그대로 돌리되, 가장자리 숲·가장자리 막기·마무리는 건너뛴다 (세계 지도에서 한 번에)."""
    import arabia_ahsa, arabia_zabid, arabia_taiz, arabia_jeddah, arabia_sanaa
    keep = (A.ArabiaMap.border, A.ArabiaMap.finish_arabia, A.block_rect, arabia_sanaa.H)
    A.ArabiaMap.border = lambda self, *a, **k: None
    A.ArabiaMap.finish_arabia = lambda self: None

    def edge_only_block(m, x0, y0, x1, y1):
        if (x0 == x1 and x0 in (0, m.w - 1)) or (y0 == y1 and y0 in (0, m.h - 1)):
            return                                             # 맵 테두리 한 줄 막기 = 세계 지도에선 열린 땅
        keep[2](m, x0, y0, x1, y1)
    A.block_rect = edge_only_block
    arabia_sanaa.H = CH                                        # 사나 64×48 → 64×44 (조각 크기)
    try:
        if mid == 'arabia_ahsa':
            t = arabia_ahsa.build('X', 'X', 'tmp_e_01')
        elif mid == 'arabia_zabid':
            t = arabia_zabid.build(('X', 'X', 'tmp_w_01'), ('X', 'X', 'tmp_e_01'))
        else:
            mod = {'arabia_taiz': arabia_taiz, 'arabia_jeddah': arabia_jeddah, 'arabia_sanaa': arabia_sanaa}[mid]
            t = mod.build(_FakeLinks())
    finally:
        A.ArabiaMap.border, A.ArabiaMap.finish_arabia, A.block_rect, arabia_sanaa.H = keep
    return t


def _blit(w, t, ox, oy):
    """마을 맵 t를 세계 지도 w의 (ox,oy)에 옮긴다. 출구는 끝점으로만 돌려준다."""
    ends = []
    for y in range(t.h):
        for x in range(t.w):
            X, Y = x + ox, y + oy
            w.zone[Y][X] = t.zone[y][x]
            w.road[Y][X] = t.road[y][x]
            w.water[Y][X] = t.water[y][x]
            w.blocked[Y][X] = t.blocked[y][x]
            w.reserved[Y][X] = True
            w.obj[Y][X], w.high[Y][X], w.deco[Y][X] = t.obj[y][x], t.high[y][x], t.deco[y][x]
            if (x, y) in t.bridges:
                w.bridges[(X, Y)] = t.bridges[(x, y)]
    w.hard |= {(x + ox, y + oy) for (x, y) in t.hard}
    w.stamps += [(n, x + ox, y + oy) for (n, x, y) in t.stamps]
    w.pending_plants += [(n, x + ox, y + oy) for (n, x, y) in t.pending_plants]
    for o in t.objects:
        o = dict(o)
        if o['kind'] == 'exit':
            ends.append((o['edge'], o['x'] + ox + (o['w'] - 1) / 2, o['y'] + oy + (o['h'] - 1) / 2))
            continue
        if o['kind'] == 'spawn' and str(o.get('id', '')).startswith('tmp'):
            continue
        A.shift_obj(o, ox, oy)
        w.objects.append(o)
    return ends


def _astar(w, a, b, avoid_town=True):
    """세계 지도 위 최소 비용 경로 (막힌 칸·물 피하고, 기존 길은 싸게)."""
    def cost(x, y):
        if not in_world(x, y) or w.blocked[y][x] or w.water[y][x] or w.obj[y][x]:
            return None
        z = w.zone[y][x]
        c = {'dune': 1.6, 'rock': 1.3, 'gravel': 1.0, 'sand': 1.1, 'dry': 1.0, 'grass': 1.0}.get(z, 1.0)
        c += 0.6 * vnoise(x, y, 9, 'ar')                       # 살짝 굽이치게
        if w.road[y][x]:
            c = 0.35
        if avoid_town and w.reserved[y][x] and not w.road[y][x]:
            c += 2.5                                            # 마을 안은 길로만 다니게
        return c
    a, b = (int(round(a[0])), int(round(a[1]))), (int(round(b[0])), int(round(b[1])))
    pq, came, g = [(0, a)], {a: None}, {a: 0}
    while pq:
        _, cur = heapq.heappop(pq)
        if cur == b:
            break
        for nx, ny in ((cur[0] + 1, cur[1]), (cur[0] - 1, cur[1]), (cur[0], cur[1] + 1), (cur[0], cur[1] - 1)):
            c = cost(nx, ny) if (nx, ny) != b else 1
            if c is None:
                continue
            ng = g[cur] + c
            if ng < g.get((nx, ny), 1e9):
                g[(nx, ny)] = ng; came[(nx, ny)] = cur
                heapq.heappush(pq, (ng + abs(nx - b[0]) + abs(ny - b[1]), (nx, ny)))
    if b not in came:
        raise ValueError(f'세계 길 연결 실패 {a}→{b}')
    path, cur = [], b
    while cur:
        path.append(cur); cur = came[cur]
    return path[::-1]


def _paint_route(w, path, kind, width, grade):
    """A* 칸 경로를 몇 칸마다 하나씩 골라 부드러운 곡선으로 다시 그린다 (계단 모양·덩어리 길 방지)."""
    pts = [(x + 0.5, y + 0.5) for (x, y) in path[::5]]
    if pts[-1] != (path[-1][0] + 0.5, path[-1][1] + 0.5):
        pts.append((path[-1][0] + 0.5, path[-1][1] + 0.5))
    r = width / 2
    done = set()
    for (px, py) in catmull(pts) + [(x + 0.5, y + 0.5) for (x, y) in (path[0], path[-1])]:
        for y in range(int(py - r) - 1, int(py + r) + 2):
            for x in range(int(px - r) - 1, int(px + r) + 2):
                if (x, y) in done or not in_world(x, y) or w.blocked[y][x] or w.water[y][x] or w.obj[y][x]:
                    continue
                if (x + 0.5 - px) ** 2 + (y + 0.5 - py) ** 2 <= r * r + 0.3:
                    done.add((x, y))
                    if not w.road[y][x]:                       # 마을 안 기존 포장은 그대로
                        w.paint(kind, x, y, x, y, grade=grade)
                    else:
                        w.road[y][x] = max(w.road[y][x], grade)
    return catmull(pts)


def _nearest_road(w, p, rect):
    x0, y0, x1, y1 = rect
    best = None
    for y in range(y0, y1):
        for x in range(x0, x1):
            if w.road[y][x] >= 2 and not w.blocked[y][x]:
                d = (x - p[0]) ** 2 + (y - p[1]) ** 2
                if best is None or d < best[0]:
                    best = (d, (x, y))
    return best[1]


def zone_at(x, y):
    """세계 바닥: 고원(사나·타이즈) · 티하마 해안(자비드) · 빈 사막 · 나즈드 · 오아시스(아흐사)."""
    c = chunk_of(x, y)
    n = vnoise(x, y, 11, 'wz')
    if c in ((1, 0), (1, 1)):                          # 고원: 바위 언덕과 산풀 (자갈색은 길과 헷갈려 쓰지 않음)
        return 'rock' if n > 0.68 else 'dry' if n > 0.5 else 'sand'
    if c == (2, 1):
        return 'dry' if n > 0.42 else 'grass' if n < 0.22 else 'sand'
    if c == (0, 1):
        return 'dune' if n > 0.38 else 'sand'
    if c == (1, 2):
        return 'sand'                                  # 나즈드: 모래 사막 (풀은 와디 둑에만)
    if c == (0, 2):
        return 'grass' if n < 0.18 else 'sand'
    if c == (2, 2):
        return 'sand'
    return 'sand'


def build_world():
    w = A.ArabiaMap('arabia_world', '아라비아', FW, FH, seed=2701, theme='town')
    w.soft_under = True
    w.obstacles = []
    # ---- 바닥 ----
    for y in range(FH):
        for x in range(FW):
            if in_world(x, y):
                z = zone_at(x, y)
                if z != 'sand':
                    w.paint(z, x, y, x, y, reserve=False)
    # ---- 바다: 왼쪽 아래 걸프, 오른쪽 홍해 (해안선은 물결) ----
    for y in range(FH):
        for x in range(FW):
            c = chunk_of(x, y)
            if c is None or c not in CHUNKS:
                continue
            gulf = c == (0, 2) and x < 4 + 3 * vnoise(x, y, 6, 'gf') and y > 2 * CH + 4
            red = c in ((2, 1), (2, 2)) and x > FW - 5 - 3 * vnoise(x, y, 6, 'rs')
            if gulf or red:
                w.paint('water', x, y, x, y)
    # ---- 마을 옮기기 ----
    ends, rects = {}, {}
    for (cx, cy), (mid, name, kind) in CHUNKS.items():
        if kind != 'town':
            continue
        t = _build_town(mid)
        ox = cx * CW + (CW - t.w) // 2
        oy = cy * CH + (CH - t.h) // 2
        rects[mid] = (ox, oy, ox + t.w, oy + t.h)
        ends[mid] = _blit(w, t, ox, oy)
    # ---- 길 잇기 (큰길 · 고원길 · 질러가기) ----
    def end(mid, edge):
        e = [p for p in ends[mid] if p[0] == edge]
        return (e[0][1], e[0][2])
    def road_in(mid, p):
        return _nearest_road(w, p, rects[mid])
    sanaa_s = end('arabia_sanaa', 'S')
    najd_mid = (CW * 1 + 31, CH * 2 + 21)          # 나즈드 한가운데 카라반사라이 앞
    fork = (22, CH + 27)                          # 빈 사막 갈림길 (돌탑)
    # 길 성격 (남아라비아 향료길: 남쪽 바다·사막에서 북쪽 고원 사나를 거쳐 중앙 대륙으로 올라간다)
    #   trunk = 사막 대상로(흙길) · coast = 홍해 해안길 · climb = 해안 → 고원 오르막 · highland = 고원 돌길
    #   track = 빈 사막 모랫길(돌무더기로 길 표시) · canyon = 협곡길
    routes = [
        ('road', 3, 3, end('arabia_ahsa', 'E'), najd_mid, 'trunk', '나즈드 대상로 서쪽'),
        ('road', 3, 3, najd_mid, end('arabia_jeddah', 'W'), 'trunk', '나즈드 대상로 동쪽'),
        ('road', 3, 3, end('arabia_jeddah', 'N'), road_in('arabia_zabid', (CW * 2 + 32, CH * 2)), 'coast', '홍해 해안길'),
        ('road', 3, 3, end('arabia_zabid', 'W'), end('arabia_taiz', 'E'), 'climb', '자비드–타이즈 커피 오르막'),
        ('paving', 3, 3, road_in('arabia_taiz', (CW + 32, CH)), sanaa_s, 'highland', '고원길'),
        ('gravel', 2, 1, road_in('arabia_ahsa', (16, CH * 2)), fork, 'track', '빈 사막 질러가기'),
        ('gravel', 2, 1, fork, end('arabia_taiz', 'W'), 'track', '빈 사막 → 타이즈'),
        ('gravel', 2, 1, fork, (GATES[('arabia_shortcut', 'n_01')] + 1.5, CH), 'track', '빈 사막 북쪽 모래길'),
        ('road', 3, 2, road_in('arabia_zabid', (CW * 2 + 32, CH)), (CW * 2 + GATES[('arabia_zabid', 'n_01')] + 1.5, CH), 'canyon', '자비드 북쪽 협곡길'),
    ]
    _wadi(w)                                      # 길보다 먼저: 마른 강바닥 (길은 그 위를 건넌다)
    w.routes = []
    for kind, width, grade, a, b, style, label in routes:
        line = _paint_route(w, _astar(w, a, b), kind, width, grade)
        w.routes.append((style, label, width, line))
    w.rects, w.najd_mid, w.fork = rects, najd_mid, fork
    # ---- 야생 칸 꾸미기 ----
    _dress_wild(w)
    # ---- 가장자리: 세계 바깥(빈 칸·지도 끝)과 맞닿는 곳은 자연 지형 띠 ----
    gate_keep = set()
    for (mid, key), sx in GATES.items():
        cx, cy = BY_ID[mid]
        for a in range(sx - 2, sx + 6):
            for d in range(0, 7):
                gate_keep.add((cx * CW + a, cy * CH + d))
    band = set()
    for y in range(FH):
        for x in range(FW):
            if not in_world(x, y) or (x, y) in gate_keep or w.water[y][x]:
                continue
            # 바깥까지 거리 (조각 단위로 간단히): 네 방향으로 몇 칸 안에 세계 바깥이 있는지
            near = 99
            for k in range(1, 7):
                if any(not in_world(x + dx * k, y + dy * k) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    near = k
                    break
            if near <= 2 + int(4 * vnoise(x, y, 7, 'eb')) and not (w.road[y][x] and near > 1):
                band.add((x, y))
    for (x, y) in band:
        if not w.reserved[y][x] or near_town_edge(w, x, y):
            A.block_rect(w, x, y, x, y)
    _dress_band(w, band)
    # ---- 바닥 결 ----
    deco = {'sand': (0.030, ['drawn_tuft', 'drawn_pebbles', 'drawn_tuft2']), 'dune': (0.012, ['drawn_pebbles']),
            'rock': (0.020, ['drawn_pebbles', 'drawn_tuft']), 'gravel': (0.025, ['drawn_pebbles', 'drawn_tuft2']),
            'dry': (0.055, ['drawn_dryTuft', 'drawn_tuft', 'drawn_dryTuft']),
            'grass': (0.060, ['drawn_grassTuft', 'drawn_grassTuft2', 'drawn_wildflowerY', 'drawn_wildflowerW'])}
    sparse = {'sand': ['drawn_sage', 'drawn_acaciaRound'], 'gravel': ['drawn_acaciaRound', 'drawn_boulderSmall', 'drawn_sage'],
              'rock': ['drawn_boulder', 'drawn_boulderSmall'], 'dry': ['drawn_acaciaRound', 'drawn_olive'],
              'grass': ['drawn_oasisTree', 'drawn_datePalm2'], 'dune': ['drawn_sage']}
    for y in range(2, FH - 2):
        for x in range(2, FW - 2):
            z = w.zone[y][x]
            if (not in_world(x, y) or z not in sparse or w.reserved[y][x] or w.road[y][x] or w.blocked[y][x]
                    or _h('sp', x, y) > 0.010):
                continue
            if any(w.road[yy][xx] for yy in range(y - 2, y + 3) for xx in range(x - 2, x + 3)):
                continue
            names = sparse[z]
            _put(w, names[int(_h('sn', x, y) * len(names))], x, y, foot=1)
    for y in range(1, FH - 1):
        for x in range(1, FW - 1):
            z = w.zone[y][x]
            if not in_world(x, y) or z not in deco or w.reserved[y][x] or w.road[y][x] or w.obj[y][x] or w.high[y][x] or w.deco[y][x] or w.blocked[y][x]:
                continue
            p, names = deco[z]
            if _h('wd', x, y) < p:
                A.place(w, names[int(_h('wn', x, y) * len(names))], x, y, layer='deco')
    w.finish_arabia()
    return w


def near_town_edge(w, x, y):
    return False


def _put(w, name, x, y, **kw):
    cw, ch = A.size(A.alias(name, x, y))
    if all(in_world(xx, yy) for xx in (x, x + cw - 1) for yy in (y, y + ch - 1)):
        return A.place(w, name, x, y, **kw)
    return False


def _dress_wild(w):
    T = lambda x, y, name, spr, *lines: w.talker(x, y, name, spr, *lines)
    _set_pieces(w)
    # ---- 룹알할리 빈 사막 (왼쪽 가운데): 거대한 모래 언덕 · 숨은 오아시스 · 옛 탑 ----
    ex, ey = 0, CH
    for (cx, cy, rx, ry) in [(14, 10, 10, 3.5), (44, 14, 12, 3.5), (30, 32, 14, 4), (10, 38, 8, 3)]:
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1 and in_world(ex + x, ey + y) and not w.road[ey + y][ex + x]:
                    w.paint('dune', ex + x, ey + y, ex + x, ey + y, reserve=False)
    for k in range(18):
        _put(w, 'nature_dune', ex + 3 + (k * 13) % 56, ey + 4 + (k * 7) % 36, foot=1)
    # 숨은 오아시스 (모래 언덕 사이)
    ox_, oy_ = ex + 48, ey + 34
    for y in range(oy_ - 4, oy_ + 5):
        for x in range(ox_ - 6, ox_ + 7):
            d = ((x + .5 - ox_) / 6.2) ** 2 + ((y + .5 - oy_) / 4.2) ** 2
            if d <= 1 and in_world(x, y) and not w.road[y][x]:
                w.paint('water' if d < 0.32 else 'grass', x, y, x, y)
    for (x, y) in [(ox_ - 6, oy_ - 4), (ox_ + 4, oy_ - 5), (ox_ - 7, oy_ + 2), (ox_ + 5, oy_ + 2)]:
        _put(w, ['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, y, force=True)
    T(ox_ - 1, oy_ + 4, '빈 사막의 은자', 'arabia_npc_scholar_tall',
      '여기까지 왔군. 이 오아시스는 대상들도 잘 몰라.',
      '북쪽 모래길을 따라가면 중앙 대륙 오아시스가 나온다네. 사나를 거치지 않는 옛길이지.')
    _put(w, 'building_malwiya', ex + 26, ey + 18, foot=2)
    w.asign(ex + 30, ey + 22, '룹알할리 빈 사막\n모래가 깊어 느립니다. 탑을 보고 방향을 잡으세요.\n동: 타이즈 · 남: 알 아흐사 · 북: 중앙 대륙 모래길')
    w.obstacles += [dict(id='arabia_shortcut_sand1', definitionId='loose_sand', x=ex + 14, y=ey + 26, width=5, height=3, enabled=True, phaseOffset=0.0),
                    dict(id='arabia_shortcut_sand2', definitionId='loose_sand', x=ex + 34, y=ey + 6, width=4, height=3, enabled=True, phaseOffset=0.0)]
    # ---- 고원 (사나·타이즈 둘레): 계단밭 · 커피 ----
    for (x0, y0) in [(CW + 2, CH - 2), (CW + 48, CH - 3)]:
        for k in range(3):
            for x in range(x0, x0 + 12):
                if in_world(x, y0 + k * 3) and not w.reserved[y0 + k * 3][x]:
                    A.place(w, 'drawn_terrace', x, y0 + k * 3, foot=1)
    # ---- 홍해: 다우 배 ----
    for (x, y) in [(FW - 3, CH + 36), (FW - 3, 2 * CH + 12)]:
        _put(w, 'drawn_dhow', x - 2, y, layer='deco', force=True)
    _dress_routes(w)


# =====================================================================
#  길 꾸미기 (2026-10-27): 길마다 성격에 맞는 길가 + 길 위 쉼터·볼거리
#  남아라비아 향료길: 남쪽 바닷가·사막에서 거둔 유향·커피·대추야자가 북쪽 고원 사나를 지나 중앙 대륙으로 올라간다.
# =====================================================================
def _wadi(w):
    """마른 강바닥(와디): 북쪽 고원(타이즈 아래)에서 남쪽 사막으로 흘러내린다. 큰길이 그 위를 건넌다.
    바닥은 모래+자갈(길과 헷갈리지 않게 흙길 색을 쓰지 않음), 양쪽 둑은 물기가 남아 마른 풀이 자란다."""
    w.wadi = []
    for y in range(2 * CH, FH):
        cx = CW + 47 + 4 * math.sin(y / 6.5) + 2 * vnoise(0, y, 9, 'wd')
        bank = 3 + 2 * vnoise(cx, y, 5, 'wb')
        for x in range(int(cx - bank) - 1, int(cx + bank) + 2):
            if not in_world(x, y) or w.reserved[y][x] or w.water[y][x]:
                continue
            d = abs(x + 0.5 - cx)
            if d <= 1.3:
                w.wadi.append((x, y))                  # 강바닥 (모래 그대로, 자갈 그림)
            elif d <= bank:
                w.paint('dry', x, y, x, y, reserve=False)


def _in_town(w, x, y, margin=1):
    return any(x0 - margin <= x < x1 + margin and y0 - margin <= y < y1 + margin for (x0, y0, x1, y1) in w.rects.values())


def _free(w, x, y, wild=True):
    return (in_world(x, y) and not w.road[y][x] and not w.blocked[y][x] and not w.water[y][x]
            and not w.reserved[y][x] and not w.obj[y][x] and not w.high[y][x] and not (wild and _in_town(w, x, y)))


def _spot(w, x, y, cw=1, ch=1, r=6, below=0, town=False):
    """(x,y) 둘레에서 cw×ch(+아래 below줄)가 모두 빈 가장 가까운 왼쪽 위 칸."""
    best = None
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d = dx * dx + dy * dy
            if best is not None and d >= best[0]:
                continue
            X, Y = int(x) + dx, int(y) + dy
            if all(_free(w, X + i, Y + j, wild=not town) for i in range(cw) for j in range(ch + below)):
                best = (d, (X, Y))
    return best[1] if best else None


def _at(line, f):
    """곡선의 f(0~1) 지점과 그 지점의 진행 방향 (단위 벡터)."""
    i = min(len(line) - 2, max(0, int(f * (len(line) - 1))))
    (ax, ay), (bx, by) = line[max(0, i - 3)], line[min(i + 3, len(line) - 1)]
    d = math.hypot(bx - ax, by - ay) or 1
    return line[i], ((bx - ax) / d, (by - ay) / d)


def _side(p, t, k):
    """곡선 점 p에서 진행 방향 t의 한쪽(+)/반대쪽(−)으로 k칸."""
    return (p[0] - t[1] * k, p[1] + t[0] * k)


def _thing(w, name, x, y, r=4, foot=1, **kw):
    cw, ch = A.size(A.alias(name, int(x), int(y)))
    s = _spot(w, x - cw / 2, y - ch + 1, cw, ch, r)
    return bool(s) and A.place(w, name, s[0], s[1], foot=foot, force=True, **kw)


def _who(w, x, y, name, spr, *lines, r=4):
    s = _spot(w, x, y, 1, 1, r, below=1)
    if s:
        w.talker(s[0], s[1], name, spr, *lines)
        w.reserved[s[1]][s[0]] = True
    return s


def _sign(w, x, y, text, r=6):
    s = _spot(w, x - 1, y - 1, 2, 2, r, below=1) or _spot(w, x - 1, y - 1, 2, 2, r + 2, below=1, town=True)
    if s:
        w.asign(s[0], s[1], text)
    return s


def _set_pieces(w):
    """길 위의 쉼터·볼거리 (먼저 자리를 잡고, 남은 길가를 _dress_routes가 채운다)."""
    R = {label: (style, width, line) for style, label, width, line in w.routes}
    # ---- 나즈드 한가운데 카라반사라이: 큰길 북쪽에 붙여 짓고 마당을 길과 잇는다 ----
    mx, my = w.najd_mid
    w.paint('paving', mx - 4, my - 3, mx + 4, my - 2, grade=2)
    A.place(w, 'building_caravanserai', mx - 2, my - 7, foot=2, force=True)
    _thing(w, 'drawn_well', mx + 7, my - 3)
    _thing(w, 'plaza_trough', mx - 6, my - 2)
    _thing(w, 'drawn_camelRest', mx - 9, my - 4)
    _who(w, mx - 4, my - 4, '나즈드의 대상 숙소 주인', 'arabia_woman_grandma_tall',
         '알 아흐사에서 제다까지, 사막 한가운데 쉴 곳은 여기뿐이에요. 물 한 바가지 하고 가요.',
         '남쪽 바닷가에서 거둔 유향은 모두 이 길을 지나 북쪽 사나, 그리고 중앙 대륙으로 올라가요.')
    # ---- 향료 대상 야영지 (아흐사 → 나즈드 사이, 길 남쪽) ----
    st, wd, line = R['나즈드 대상로 서쪽']
    p, t = _at(line, 0.62)
    s = 1 if _side(p, t, 1)[1] > p[1] else -1            # 길 남쪽 (y가 큰 쪽)
    c = _side(p, t, s * (wd / 2 + 5))
    _thing(w, 'building_tent', c[0], c[1], r=5, foot=2)
    for k, n in enumerate(['drawn_camel', 'drawn_camel', 'drawn_camelRest', 'drawn_cart', 'prop_saddle', 'prop_spiceSack', 'prop_frankincense']):
        _thing(w, n, c[0] - 7 + k * 2.6, c[1] + 3 + (k % 2) * 2, r=4)
    _who(w, c[0] + 4, c[1] - 1, '향료 대상의 우두머리', 'arabia_npc_bedouin_tall',
         '남쪽 바닷가에서 거둔 유향을 싣고 북쪽 중앙 대륙으로 가는 길이오.',
         '해가 지면 길가 이정석만 보고 가시오. 이정석이 끊기는 곳은 길이 아니오.')
    # ---- 마른 와디를 건너는 곳 ----
    st, wd, line = R['나즈드 대상로 동쪽']
    wset = set(w.wadi)
    cross = next((q for q in line if (int(q[0]), int(q[1])) in wset), line[len(line) // 3])
    _sign(w, cross[0] + 4, cross[1] - 4, '마른 와디 (강바닥)\n북쪽 고원에 비가 오면 물이 불어나요.\n강바닥을 따라 남쪽으로: 유향나무 숲')
    # 와디 아래쪽 끝: 유향나무 숲 (남아라비아 향료의 시작)
    wx, wy = max(w.wadi, key=lambda c: c[1])
    for k in range(6):
        _thing(w, 'nature_frankincenseTree', wx - 9 + k * 3.4, wy - 7 - (k % 2) * 3, r=3)
    _who(w, wx - 2, wy - 11, '유향 채집꾼', 'arabia_npc_child_tall',
         '나무에 상처를 내면 하얀 눈물이 맺혀요. 그게 유향이에요.',
         '이 향이 사나를 지나 중앙 대륙 장터까지 가요. 거기선 금만큼 비싸대요!')
    # ---- 무너진 봉화 망루 (큰길 동쪽에서 남쪽으로 샛길) ----
    p, t = _at(line, 0.62)
    ruin = _spot(w, p[0] - 3, p[1] + 9, 8, 5, r=5)
    if ruin:
        rx, ry = ruin
        _paint_route(w, _astar(w, p, (rx + 4, ry), avoid_town=True), 'gravel', 1, 1)
        A.place(w, 'wall_tower', rx, ry + 1, foot=2, force=True)
        A.place(w, 'wall_ruin', rx + 3, ry + 3, foot=1, force=True)
        A.place(w, 'wall_ruin', rx + 5, ry + 2, foot=1, force=True)
        _thing(w, 'prop_chest', rx + 6, ry + 1, r=3)
        _who(w, rx + 3, ry, '망루 터를 살피는 학자', 'arabia_npc_scholar_tall',
             '옛날엔 여기서 봉화를 올려 대상이 오는 걸 알렸다네. 아흐사부터 제다까지 망루가 줄지어 있었지.',
             '상자는 비었더군. 그래도 길에서 벗어난 곳엔 늘 뭔가가 있지.')
    # ---- 홍해 해안 어촌 (해안길 바다 쪽) ----
    st, wd, line = R['홍해 해안길']
    p, t = _at(line, 0.45)
    sx, sy = FW - 11, p[1]
    _thing(w, 'building_arish', sx, sy - 1, r=5, foot=2)
    _thing(w, 'building_arish', sx - 1, sy + 5, r=5, foot=2)
    _thing(w, 'drawn_netRack', sx + 3, sy + 2, r=4)
    _who(w, sx - 3, sy + 2, '홍해 어부', 'arabia_npc_bedouin_tall',
         '이 길은 제다에서 자비드까지 바다를 끼고 올라가요. 바닷바람이 짐을 식혀 줘서 대추야자가 덜 상하지.',
         '배로 가면 빠르지만 바람이 안 불면 꼼짝 못 해요. 걸어가는 게 마음 편하지.')
    p, t = _at(line, 0.15)
    _thing(w, 'wall_tower', FW - 7, p[1], r=4, foot=2)
    # ---- 자비드 → 타이즈: 커피 계단밭 오르막 ----
    st, wd, line = R['자비드–타이즈 커피 오르막']
    for f in [k / 40 for k in range(5, 36)]:
        p, t = _at(line, f)
        for sgn in (1, -1):
            for k in (wd / 2 + 2, wd / 2 + 4):
                x, y = (int(v) for v in _side(p, t, sgn * k))
                if _free(w, x, y) and _free(w, x, y - 1):
                    A.place(w, 'drawn_coffee', x, y - 1, foot=1)
    p, t = _at(line, 0.5)
    _who(w, *_side(p, t, wd / 2 + 1.2), '커피 농부', 'arabia_woman_grandma_tall',
         '바닷가 자비드에서 고원 타이즈로 올라갈수록 커피가 맛있어져요. 안개가 나무를 키우거든요.',
         '빨간 열매를 따서 말리면 모카 커피! 제다 항구로 내려가 바다 건너까지 팔려요.', r=6)
    _sign(w, *_side(p, t, -(wd / 2 + 2)), '커피 오르막\n서: 고원 도시 타이즈 · 동: 해안 도시 자비드\n자비드 북쪽 협곡길 → 중앙 대륙 붉은 바위 협곡')
    # ---- 고원길 끝: 사나 남문 앞 검문소 ----
    st, wd, line = R['고원길']
    p, t = _at(line, 0.8)
    for sgn in (1, -1):
        _thing(w, 'wall_tower', *_side(p, t, sgn * (wd / 2 + 2)), r=2, foot=2)
    _who(w, *_side(p, t, wd / 2 + 1.2), '사나 남문 경비', 'arabia_npc_bedouin_tall',
         '사나에 들어가는 짐은 여기서 한 번 봅니다. 성 안을 지나 북문으로 나가면 중앙 대륙이오.', r=4)
    # ---- 빈 사막 갈림길: 돌탑 + 길 잃은 나그네 ----
    fx, fy = w.fork
    _thing(w, 'drawn_rockWall', fx + 4, fy + 2, r=3)
    _sign(w, fx - 3, fy - 3, '빈 사막 갈림길\n북: 모래길 → 중앙 대륙 (사나를 거치지 않음)\n동: 타이즈 · 남: 알 아흐사\n돌무더기를 따라가세요.')
    _thing(w, 'nature_deadwood', fx + 7, fy - 5, r=4)
    _who(w, fx + 5, fy - 3, '길 잃은 나그네', 'arabia_npc_child_tall',
         '돌무더기를 놓쳐서 하루를 헤맸어요… 물주머니가 거의 비었어요.',
         '동쪽 모래 언덕 사이에 숨은 샘이 있다던데, 정말일까요?')
    _thing(w, 'prop_waterskin', fx + 7, fy - 1, r=3)
    # ---- 길 시작점 이정표 (방향은 지도 그대로: 북 = 사나·중앙 대륙) ----
    st, wd, line = R['나즈드 대상로 서쪽']
    p, t = _at(line, 0.1)
    _sign(w, *_side(p, t, -(wd / 2 + 2)), '향료길 · 나즈드 대상로\n동: 사막 한가운데 카라반사라이 → 제다\n북: 빈 사막 질러가기 → 타이즈 (모래가 깊음)')
    st, wd, line = R['홍해 해안길']
    p, t = _at(line, 0.08)
    _sign(w, *_side(p, t, wd / 2 + 2), '홍해 해안길\n북: 자비드 → 커피 오르막 → 타이즈 → 사나\n남: 제다 항구', r=9)
    st, wd, line = R['자비드 북쪽 협곡길']
    p, t = _at(line, 0.45)
    _sign(w, *_side(p, t, wd / 2 + 2), '자비드 북쪽 협곡길\n북: 중앙 대륙 붉은 바위 협곡\n(사나를 거치지 않는 지름길)', r=9)
    st, wd, line = R['고원길']
    p, t = _at(line, 0.25)
    _sign(w, *_side(p, t, wd / 2 + 2), '고원길\n북: 수도 사나 → 북문 → 중앙 대륙\n남: 타이즈', r=9)


def _dress_routes(w):
    """길 성격별 길가: 대상로 = 이정석·가프 나무 · 해안 = 둠 야자 · 오르막 = 과일나무 · 고원 = 사이프러스·돌담·등불
    · 모랫길 = 돌무더기(길 표시) · 협곡 = 바위."""
    every = {'trunk': 13, 'coast': 6, 'climb': 9, 'highland': 7, 'track': 8, 'canyon': 5}
    for style, label, wd, line in w.routes:
        L = [0]
        for a, b in zip(line, line[1:]):
            L.append(L[-1] + math.dist(a, b))
        nxt, k = every[style] / 2, 0
        for i, p in enumerate(line):
            if L[i] < nxt:
                continue
            nxt += every[style]
            k += 1
            if _in_town(w, int(p[0]), int(p[1]), margin=2):
                continue
            _, t = _at(line, i / (len(line) - 1))
            sgn = 1 if k % 2 else -1

            def at(dist):
                x, y = _side(p, t, sgn * dist)
                return int(x), int(y)
            if style == 'trunk':
                x, y = at(wd / 2 + 1.2)
                if _free(w, x, y) and _free(w, x, y - 1):
                    A.place(w, 'drawn_milestone', x, y - 1, foot=1)
                if k % 2 == 0:
                    x, y = at(-(wd / 2 + 3.5))
                    _thing(w, ['tree_ghaf', 'drawn_acaciaRound', 'tree_sidr'][k % 3], x, y, r=1)
            elif style == 'coast':
                x, y = at(wd / 2 + 2)
                _thing(w, ['tree_doumPalm', 'drawn_datePalm', 'drawn_datePalm2'][k % 3], x, y, r=1)
            elif style == 'highland':
                x, y = at(wd / 2 + 1.5)
                _thing(w, 'drawn_cypress' if k % 3 else 'drawn_rockWall', x, y, r=1)
                if k % 3 == 0:
                    x, y = at(-(wd / 2 + 1.5))
                    _thing(w, 'prop_lantern', x, y, r=1)
            elif style == 'climb':
                x, y = at(wd / 2 + 6)
                _thing(w, ['drawn_almond', 'drawn_pomegranate', 'drawn_olive'][k % 3], x, y, r=1)
            elif style == 'track':
                x, y = at(wd / 2 + 1)
                if _free(w, x, y):
                    A.place(w, 'nature_rocks', x, y, foot=1)          # 돌무더기: 모래 위 길 표시
                if k % 5 == 0:
                    x, y = at(-(wd / 2 + 3))
                    _thing(w, 'nature_deadwood', x, y, r=1)
            elif style == 'canyon':
                for sg in (1, -1):
                    x, y = _side(p, t, sg * (wd / 2 + 1.5))
                    _thing(w, ['drawn_boulder', 'drawn_rockWall', 'drawn_boulderSmall'][(k + sg) % 3], x, y, r=1)
    # 와디 둑: 위성류(타마리스크)·시드르, 강바닥 자갈
    for j, (x, y) in enumerate(w.wadi):
        if j % 13 == 0:
            for dx in (-4, 4):
                _thing(w, 'nature_tamarisk' if (j // 13) % 2 else 'tree_sidr', x + dx, y, r=1)
        if _h('wp', x, y) < 0.55 and not w.road[y][x] and not w.deco[y][x] and not w.obj[y][x]:
            A.place(w, 'drawn_pebbles', x, y, layer='deco')
        elif _h('wq', x, y) < 0.06 and _free(w, x, y):
            A.place(w, 'drawn_boulderSmall', x, y, foot=1)


def _dress_band(w, band):
    """세계 가장자리 띠를 칸별 지형으로 덮는다 (고원 = 바위, 사막 = 모래 언덕·바위, 해안·오아시스 = 야자)."""
    names_for = {(1, 0): ['drawn_rockWall', 'drawn_boulder', 'drawn_rockWall'], (1, 1): ['drawn_rockWall', 'drawn_boulder'],
                 (0, 1): ['drawn_boulder', 'nature_dune', 'drawn_rockWall', 'drawn_datePalm2'],
                 (2, 1): ['drawn_datePalm', 'drawn_datePalm2', 'drawn_oasisTree'], (0, 2): ['drawn_datePalm', 'drawn_oasisTree', 'drawn_datePalm3'],
                 (1, 2): ['drawn_boulder', 'drawn_rockWall', 'drawn_acaciaRound'], (2, 2): ['drawn_datePalm2', 'drawn_boulder']}
    for (x, y) in sorted(band, key=lambda c: _h('bn', c)):
        names = names_for.get(chunk_of(x, y), ['drawn_boulder'])
        name = names[int(_h('bw', x, y) * len(names))]
        cw, ch = A.size(A.alias(name, x, y))
        if all((x + dx, y) in band for dx in range(cw)):
            A.place(w, name, x, y - ch + 1, foot=1, force=True)


def _cut(w, mid, links):
    (cx, cy) = BY_ID[mid]
    name = CHUNKS[(cx, cy)][1]
    m = A.ArabiaMap(mid, name, CW, CH, seed=1, theme='town')
    m.hours = HOURS.get(mid, 0)
    ox, oy = cx * CW, cy * CH
    for y in range(CH):
        for x in range(CW):
            X, Y = x + ox, y + oy
            for layer in ('ground', 'deco', 'obj', 'high', 'road', 'blocked', 'water', 'zone', 'reserved'):
                getattr(m, layer)[y][x] = getattr(w, layer)[Y][X]
    m.objects = [A.shift_obj(dict(o), -ox, -oy) for o in w.objects
                 if ox <= o['x'] < ox + CW and oy <= o['y'] < oy + CH]
    m.obstacles = [dict(o, x=o['x'] - ox, y=o['y'] - oy) for o in w.obstacles
                   if ox <= o['x'] < ox + CW and oy <= o['y'] < oy + CH]
    m.stamps = []
    m.warnings = list(w.warnings) if mid == 'arabia_ahsa' else []
    for (gm, key), sx in GATES.items():
        if gm == mid:
            m.exit(links, key, sx)
    return m


def get_chunk(mid, links_fn):
    if not _CACHE:
        w = build_world()
        maps = {v[0]: _cut(w, v[0], links_fn(v[0])) for v in CHUNKS.values()}
        _CACHE['seams'] = _seams(maps)
        _CACHE.update(maps)
    return _CACHE[mid]


def _seams(maps):
    """조각 경계에서 양쪽 다 걸을 수 있는 칸은 모두 출구로 (가장 큰 걸을 수 있는 덩어리끼리만)."""
    from arabia_central import _walk_grid, _main_area
    info = {mid: _main_area(m, _walk_grid(m)) for mid, m in maps.items()}
    short = {v[0]: v[0].split('_', 1)[1] for v in CHUNKS.values()}
    n = 0
    for (ax, ay), (aid, _, _) in CHUNKS.items():
        for (dx, dy, edge, other) in ((1, 0, 'E', 'W'), (0, 1, 'S', 'N')):
            b = (ax + dx, ay + dy)
            if b not in CHUNKS:
                continue
            bid = CHUNKS[b][0]
            ma, mb = info[aid], info[bid]
            if edge == 'E':
                ok = [t for t in range(CH) if (CW - 1, t) in ma and (CW - 2, t) in ma and (0, t) in mb and (1, t) in mb]
            else:
                ok = [t for t in range(CW) if (t, CH - 1) in ma and (t, CH - 2) in ma and (t, 0) in mb and (t, 1) in mb]
            runs, cur = [], []
            for t in ok:
                if cur and t != cur[-1] + 1:
                    runs.append(cur); cur = []
                cur.append(t)
            if cur:
                runs.append(cur)
            for i, run in enumerate(runs):
                sa, sb = f'{aid}_spawn_seam_{short[bid]}_{i}', f'{bid}_spawn_seam_{short[aid]}_{i}'
                maps[aid].link(edge, run[0], len(run), bid, sb, sa)
                maps[bid].link(other, run[0], len(run), aid, sa, sb)
                n += 1
    return n
