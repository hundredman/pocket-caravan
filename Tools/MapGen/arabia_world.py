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
        o['x'] += ox; o['y'] += oy
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
    if c in ((1, 0), (1, 1)):
        return 'rock' if n > 0.62 else 'gravel' if n > 0.48 else 'sand'
    if c == (2, 1):
        return 'dry' if n > 0.42 else 'grass' if n < 0.22 else 'sand'
    if c == (0, 1):
        return 'dune' if n > 0.38 else 'sand'
    if c == (1, 2):
        return 'gravel' if n > 0.76 else 'sand'
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
    routes = [
        ('road', 3, 3, end('arabia_ahsa', 'E'), (CW * 1 + 30, CH * 2 + 22), '나즈드 대상로 서쪽'),
        ('road', 3, 3, (CW * 1 + 30, CH * 2 + 22), end('arabia_jeddah', 'W'), '나즈드 대상로 동쪽'),
        ('road', 3, 3, end('arabia_jeddah', 'N'), road_in('arabia_zabid', (CW * 2 + 32, CH * 2)), '홍해 해안길'),
        ('road', 3, 3, end('arabia_zabid', 'W'), end('arabia_taiz', 'E'), '자비드–타이즈'),
        ('paving', 3, 3, road_in('arabia_taiz', (CW + 32, CH)), sanaa_s, '고원길'),
        ('gravel', 2, 1, road_in('arabia_ahsa', (16, CH * 2)), (20, CH + 26), '빈 사막 질러가기'),
        ('gravel', 2, 1, (20, CH + 26), end('arabia_taiz', 'W'), '빈 사막 → 타이즈'),
        ('gravel', 2, 1, (20, CH + 26), (GATES[('arabia_shortcut', 'n_01')] + 1.5, CH), '빈 사막 북쪽 모래길'),
        ('road', 3, 2, road_in('arabia_zabid', (CW * 2 + 32, CH)), (CW * 2 + GATES[('arabia_zabid', 'n_01')] + 1.5, CH), '자비드 북쪽 협곡길'),
    ]
    for kind, width, grade, a, b, label in routes:
        _paint_route(w, _astar(w, a, b), kind, width, grade)
    # 사나 북문 앞은 성문 그대로
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
    # ---- 나즈드 대상로 (가운데 아래): 카라반사라이 · 이정석 · 아카시아 ----
    bx, by = CW, 2 * CH
    w.paint('paving', bx + 24, by + 10, bx + 36, by + 16, grade=2)
    _put(w, 'building_caravanserai', bx + 27, by + 6, foot=2, force=True)
    _put(w, 'drawn_well', bx + 34, by + 10, foot=1, force=True)
    _put(w, 'drawn_camelRest', bx + 22, by + 11, foot=1)
    _put(w, 'drawn_camel', bx + 38, by + 12, foot=1)
    T(bx + 25, by + 14, '나즈드의 대상 숙소 주인', 'arabia_woman_grandma_tall',
      '알 아흐사에서 제다까지 사막을 가로지르는 길이에요. 한가운데 여기서 꼭 쉬어 가요.',
      '북쪽 빈 사막으로 가면 타이즈까지 질러갈 수 있지만, 모래가 깊고 길을 잃기 쉬워요.')
    w.asign(bx + 20, by + 16, '나즈드 대상로\n서: 알 아흐사 · 동: 제다\n북쪽 모래 너머: 룹알할리 빈 사막')
    for k, x in enumerate(range(bx + 4, bx + 62, 12)):
        _put(w, 'drawn_milestone', x, by + 27 + (k % 2) * 3, foot=1)
    for (x, y) in [(bx + 8, by + 6), (bx + 50, by + 34), (bx + 14, by + 36), (bx + 46, by + 5), (bx + 56, by + 18)]:
        _put(w, ['drawn_acaciaRound', 'drawn_olive', 'drawn_acaciaRound'][x % 3], x, y)
    for (x0, y0) in [(bx + 4, by + 34), (bx + 44, by + 30)]:
        for k in range(6):
            _put(w, ['drawn_rockWall', 'drawn_boulder', 'drawn_boulderSmall'][k % 3], x0 + (k * 5) % 14, y0 + (k * 3) % 7, foot=1)
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
    # ---- 홍해 해안 (자비드·제다 사이): 야자 · 다우 배 ----
    for k in range(8):
        _put(w, ['drawn_datePalm', 'drawn_datePalm2'][k % 2], 2 * CW + 52 + (k % 3) * 3, CH + 28 + k * 5)
    for (x, y) in [(FW - 3, CH + 36), (FW - 3, 2 * CH + 12)]:
        _put(w, 'drawn_dhow', x - 2, y, layer='deco', force=True)


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
    m.objects = [dict(o, x=o['x'] - ox, y=o['y'] - oy) for o in w.objects
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
