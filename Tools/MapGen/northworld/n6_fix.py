"""2026-10-30 유저테스트 반영 (사용자 플레이 기록 · n4_build.build 마지막에 부른다)
1) 길 위에 막힌 칸 치우기          2) 끊긴 길 조각을 큰 길에 잇기 / 아무 데도 안 가는 짧은 조각 지우기
3) 대각선으로 꺾이는 길 두껍게      4) 이름표 겹침: 사람을 옆 빈 칸으로 옮기기 (낚시 자리·우리·나룻배 등과도)
5) 순록 우리: 먹이 자리 위에 울타리 우리 + 순록이 우리 안에서 지내게
6) 출구 너머가 물(얼음)이면 땅·길로 이어 보이게
7) 길을 막는 1칸 소품, 마을 안 지나치게 뭉친 소품 솎기"""
from collections import deque
import north_tiles as nt

PLUGS = {'nature:10:12', 'nature:4:12', 'nature:5:12', 'nature:10:13', 'nature:18:9'}
D4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
SPOT_LABEL = {'feed': '순록 우리 (이끼 주기)', 'guild': '낚시꾼 조합', 'museum': '회관 전시실 (0/6)', 'fortune': '룬 점쟁이',
              'ferry': '나룻배 사공', 'bath': '온천', 'sled': '썰매 언덕', 'festival': '축제', 'craft': '공방'}


def _w(name): return 0.62 * max(len(t) for t in (name or ' ').split('\n')) + 0.6


def ux_fixes(m, key, mine):
    rep = {}
    W, H = m.w, m.h
    def ins(x, y): return 0 <= x < W and 0 <= y < H
    def walk(x, y): return ins(x, y) and not m.blocked[y][x] and not m.water[y][x]
    def road(x, y): return ins(x, y) and m.road[y][x] > 0 and not m.water[y][x]
    def clear_plug(x, y):
        if m.obj[y][x] in PLUGS and m.high[y][x] is None:
            m.obj[y][x] = None; m.blocked[y][x] = False; return True
        return False
    occ = lambda: {(o['x'], o['y']) for o in m.objects}
    road0 = [row[:] for row in m.road]; water0 = [row[:] for row in m.water]

    # 6) 출구 너머가 물(얼음)이면: 출구 폭만큼 길 + 양옆 눈밭 1칸 + 덤불 1칸으로 맵 끝까지 둑길을 낸다
    #    (사용자 10/30: 얼음 동굴 골짜기 위쪽 출구가 호수에 막힌 것처럼 보임 → 호수를 줄이고 길로 튼다)
    bridged = 0
    for o in [o for o in m.objects if o['kind'] == 'exit']:
        e = o.get('edge'); x0, y0, w, h = o['x'], o['y'], o.get('w', 1), o.get('h', 1)
        if e in ('N', 'S'):
            span = range(0, y0) if e == 'N' else range(y0 + 1, H)
            cells = [(x, y, 0 if x0 <= x < x0 + w else (1 if x in (x0 - 1, x0 + w) else 2)) for y in span for x in range(x0 - 2, x0 + w + 2)]
        else:
            span = range(x0 + 1, W) if e == 'E' else range(0, x0)
            cells = [(x, y, 0 if y0 <= y < y0 + h else (1 if y in (y0 - 1, y0 + h) else 2)) for x in span for y in range(y0 - 2, y0 + h + 2)]
        for (x, y, ring) in cells:
            if not ins(x, y) or mine[y, x] or not m.water[y][x]: continue
            m.water[y][x] = False; m.ground[y][x] = nt.SNOW; m.obj[y][x] = None; m.high[y][x] = None
            if ring == 0: m.road[y][x] = max(m.road[y][x], 1); m.blocked[y][x] = False
            elif ring == 2: m.obj[y][x] = 'nature:5:12' if (x + y) % 2 else 'nature:18:9'; m.blocked[y][x] = True
            bridged += 1
    rep['bridged'] = bridged

    # 1) 길 위 막힌 칸
    rb = []
    for y in range(H):
        for x in range(W):
            if m.road[y][x] > 0 and m.blocked[y][x] and not m.water[y][x]:
                if clear_plug(x, y) or (m.obj[y][x] is None and m.high[y][x] is None and not str(m.ground[y][x] or '').startswith('relief')):
                    m.blocked[y][x] = False; rb.append(('open', x, y))
                else:
                    m.road[y][x] = 0; rb.append(('unroad', x, y))
    rep['roadBlocked'] = rb

    # 7a) 길 바로 옆 1칸 소품(길을 좁히거나 막음) 치우기 — 내 땅 안, 경계에서 3칸 이상 떨어진 곳만
    def interior(x, y, r=3): return all(ins(x + a, y + b) and mine[y + b, x + a] for a in range(-r, r + 1) for b in range(-r, r + 1))
    cleared = 0
    for y in range(H):
        for x in range(W):
            if m.obj[y][x] in PLUGS and m.high[y][x] is None and mine[y, x] and interior(x, y) and any(road(x + a, y + b) for a, b in D4):
                if clear_plug(x, y): cleared += 1
    # 7b) 뭉친 소품 솎기: 5×5 안 1칸 소품이 10개 넘으면 길에서 가까운 것부터 덜어 낸다(숲 테두리는 경계 3칸 안이라 건드리지 않음)
    thinned = 0
    for y in range(2, H - 2):
        for x in range(2, W - 2):
            if not (mine[y, x] and interior(x, y)): continue
            cs = [(x + a, y + b) for a in range(-2, 3) for b in range(-2, 3) if m.obj[y + b][x + a] in PLUGS and m.high[y + b][x + a] is None]
            if len(cs) > 10:
                for (cx, cy) in cs[::3]:
                    if clear_plug(cx, cy): thinned += 1
    rep['plugCleared'] = (cleared, thinned)

    # 2) 길 조각 잇기 / 지우기
    exits = [(o['x'] + a, o['y'] + b) for o in m.objects if o['kind'] == 'exit' for a in range(o.get('w', 1)) for b in range(o.get('h', 1))]
    def comps():
        seen = set(); out = []
        for y in range(H):
            for x in range(W):
                if road(x, y) and not m.blocked[y][x] and (x, y) not in seen:
                    q = deque([(x, y)]); seen.add((x, y)); cs = []
                    while q:
                        c = q.popleft(); cs.append(c)
                        for a, b in D4:
                            n = (c[0] + a, c[1] + b)
                            if n not in seen and road(*n) and not m.blocked[n[1]][n[0]]: seen.add(n); q.append(n)
                    out.append(cs)
        return sorted(out, key=len, reverse=True)
    joined, removed, kept = [], [], []
    for _ in range(6):
        cl = comps()
        if len(cl) <= 1: break
        main = set(cl[0]); changed = False
        for cs in cl[1:]:
            # 조각에서 걸을 수 있는 칸을 따라 큰 길까지 (14칸 안)
            near_exit = any(abs(cx - ex) + abs(cy - ey) <= 3 for (cx, cy) in cs for (ex, ey) in exits)
            lim = 80 if near_exit else 14   # 출구로 나가는 길 조각은 멀어도 마을 길까지 잇는다(사용자: 룬돌 언덕~얼음 동굴 길 끊김)
            prev = {c: None for c in cs}; q = deque((c, 0) for c in cs); hit = None
            while q:
                c, d = q.popleft()
                if c in main: hit = c; break
                if d >= lim: continue
                for a, b in D4:
                    n = (c[0] + a, c[1] + b)
                    if n in prev or not ins(*n): continue
                    if not walk(*n) and not (m.obj[n[1]][n[0]] in PLUGS and m.high[n[1]][n[0]] is None): continue
                    prev[n] = c; q.append((n, d + 1))
            if hit:
                c = hit; path = []
                while c is not None: path.append(c); c = prev[c]
                for (x, y) in path:
                    clear_plug(x, y)
                    if not m.road[y][x]: m.road[y][x] = 1
                    # 폭 2: 옆 칸 하나 더(걷는 칸일 때)
                    for a, b in ((1, 0), (0, 1)):
                        if walk(x + a, y + b) and m.obj[y + b][x + a] is None and m.high[y + b][x + a] is None and not m.road[y + b][x + a]: m.road[y + b][x + a] = 1; break
                joined.append((cs[0], len(path))); changed = True
            else:
                if len(cs) <= 24 and not near_exit:
                    for (x, y) in cs: m.road[y][x] = 0
                    removed.append((cs[0], len(cs))); changed = True
                else: kept.append((cs[0], len(cs)))
        if not changed: break
    rep['roads'] = dict(joined=joined, removed=removed, kept=kept)

    # 3) 대각선 길 두껍게: 2×2 안에 길이 대각선 둘 또는 ㄱ자 셋이면 나머지 칸도 길로(걷는 빈 칸만)
    thick = 0
    for _ in range(2):
        add = set()
        for y in range(H - 1):
            for x in range(W - 1):
                blk = [(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)]
                r = [c for c in blk if road(*c)]
                if len(r) == 2 and (r[0][0] != r[1][0] and r[0][1] != r[1][1]) or len(r) == 3:
                    for c in blk:
                        if c not in r and walk(*c) and m.obj[c[1]][c[0]] is None and m.high[c[1]][c[0]] is None and mine[c[1], c[0]] and c not in occ(): add.add(c)
        for (x, y) in add: m.road[y][x] = 1; thick += 1
    rep['thick'] = thick

    # 3b) 대각선 길 넓히기 (사용자 10/30: 대각선 길이 여전히 얇음): 마을 광장이 아닌 좁은 길에서 진행 방향이 대각선이면
    #     바깥쪽 옆 두 칸(가로·세로)을 길로 더해 눈에 보이는 폭을 두 칸 반 정도로
    def narrow(x, y): return sum(road(x + a, y + b) for a in range(-2, 3) for b in range(-2, 3)) <= 13
    widen = set()
    for y in range(2, H - 2):
        for x in range(2, W - 2):
            if not road(x, y) or not mine[y, x] or not narrow(x, y): continue
            cs = [(a, b) for a in range(-3, 4) for b in range(-3, 4) if road(x + a, y + b)]
            sxy = sum(a * b for a, b in cs); sxx = sum(a * a for a, b in cs); syy = sum(b * b for a, b in cs)
            if abs(sxy) < 0.45 * (sxx + syy) / 2: continue   # 가로·세로로 뻗은 길
            side = ((1, 0), (0, -1)) if sxy > 0 else ((1, 0), (0, 1))   # ↘ 길이면 오른쪽 위, ↗ 길이면 오른쪽 아래로 한 겹
            for a, b in side:
                c = (x + a, y + b)
                if walk(*c) and mine[c[1], c[0]] and not road(*c) and m.obj[c[1]][c[0]] is None and m.high[c[1]][c[0]] is None and c not in occ(): widen.add(c)
    for (x, y) in widen: m.road[y][x] = 1
    rep['widen'] = len(widen)

    # 5) 순록 우리: 먹이 자리 근처 빈 눈밭에 울타리 우리(아래쪽 문). 먹이 자리는 문 앞으로 옮긴다
    pen = None
    for si, sp in enumerate(m.spots):
        if sp[0] != 'feed': continue
        fx, fy = int(sp[1]), int(sp[2]); taken = occ(); best = None
        for (pw, ph) in ((11, 7), (9, 6), (8, 5)):
            for x0 in range(fx - 16, fx + 8):
                for y0 in range(fy - 16, fy + 6):
                    x1, y1 = x0 + pw - 1, y0 + ph - 1; gx = x0 + pw // 2 - 1
                    cells = [(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1)]
                    rim = [(x, y) for (x, y) in cells if x in (x0, x1) or y in (y0, y1)]
                    if not all(ins(x, y) and mine[y, x] and not m.water[y][x] and m.high[y][x] is None and (x, y) not in taken for (x, y) in cells): continue
                    if not all((m.obj[y][x] is None or m.obj[y][x] in PLUGS) and not m.road[y][x] for (x, y) in rim): continue   # 울타리 칸은 비어 있어야(안쪽 소품은 그대로 둬도 됨)
                    front = [(gx + i, y1 + 1) for i in range(3)]
                    if not all(walk(*c) or (ins(*c) and m.obj[c[1]][c[0]] in PLUGS) for c in front): continue
                    d = abs(gx + 1 - fx) + abs(y1 + 1 - fy)
                    if best is None or d < best[0]: best = (d, (x0, y0, x1, y1), gx)
            if best: break
        if best:
            _, (x0, y0, x1, y1), gx = best; pen = best[1]
            for (x, y) in [(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 2)]: clear_plug(x, y)
            m.fence(x0, y0, x1, y1, gap=(gx, y1), gap_w=3)
            m.spots[si] = ('feed', gx + 1, y1 + 1) + tuple(sp[3:])
            for c in m.life:
                if c.get('kind') == 'reindeer': c.update(x0=x0 + 1, y0=y0 + 1, x1=x1 - 1, y1=y1 - 2, count=min(c.get('count', 4), 3))
            if not any(c.get('kind') == 'reindeer' for c in m.life): m.critters('reindeer', (x0 + 1, y0 + 1, x1 - 1, y1 - 2), 4)
    rep['pen'] = pen

    # 4a) 고려 가는 길 안내 (사용자 10/30: 개울 다리길에서 고려로 이어져야 하지 않나, 이정표도)
    #     고려 연결(⑪)은 동남 물가길 남쪽 출구 하나뿐 → 갈림 안내판·끝 이정표에 경로를 적고, 안내판의 동남 물가길에 '고려 쪽'을 붙인다
    for o in m.objects:
        if o['kind'] in ('sign', 'board') and o.get('text'):
            o['text'] = '\n'.join(t + ' (고려 쪽)' if '동남 물가길' in t and '고려' not in t and '\n' in o['text'] else t for t in o['text'].split('\n'))
    if key == 'north_c_road_ne':
        def put_sign(tx, ty, text):
            taken = occ()
            for r in range(0, 6):
                for dx in range(-r, r + 1):
                    for dy in range(-r, r + 1):
                        x, y = tx + dx, ty + dy
                        if walk(x, y) and mine[y, x] and not road(x, y) and m.obj[y][x] is None and m.high[y][x] is None and (x, y) not in taken \
                           and any(road(x + a, y + b) for a, b in D4):
                            import tiles as _tt; m.prop(_tt.SIGN, x, y); m.add('sign', x, y, text=text, _new=True, _tag='exit'); return (x, y)
            return None
        rep['goryeoSigns'] = [put_sign(42, 26, '갈림길\n↓ 개울 나루 마을 → 동남 물가길 → 고려\n↘ 물가 쉼터 (막다른 길)'),
                              put_sign(80, 46, '여기는 물가 쉼터 (길 끝)\n고려는 ↙ 개울 나루 마을 → 동남 물가길')]

    # 4) 이름표 겹침
    labs = []   # (x, y, text, movable_obj or None)
    for o in m.objects:
        if o['kind'] in ('talker', 'npc') and (o.get('name') or '') and not o.get('path'): labs.append([o['x'], o['y'], o['name'], o])
        elif o['kind'] in ('sign', 'board', 'market') and (o.get('text') or o.get('name')): labs.append([o['x'], o['y'], o.get('text') or o.get('name'), None])
    from north_spots import fishing_cells, spot_cells
    for c in fishing_cells(m):   # 낚시 자리 이름표는 물에 닿은 걷는 칸(내보낼 때와 같은 칸)에 뜬다
        if c: labs.append([c[0], c[1], '얼음 낚시 (다시 어는 중)', None])
    for k in SPOT_LABEL:
        for c in spot_cells(m, k):
            if c: labs.append([c[0], c[1], SPOT_LABEL[k], None])
    def clash(a, b): return abs(a[1] - b[1]) <= 1 and abs(a[0] - b[0]) < (_w(a[2]) + _w(b[2])) / 2
    moved = []
    for i, a in enumerate(labs):
        if a[3] is None: continue
        if not any(clash(a, b) for j, b in enumerate(labs) if j != i): continue
        taken = occ(); best = None
        for r in range(1, 8):
            for dx in range(-r, r + 1):
                for dy in range(-r, r + 1):
                    if max(abs(dx), abs(dy)) != r: continue
                    x, y = a[0] + dx, a[1] + dy
                    if not (walk(x, y) and mine[y, x] and m.obj[y][x] is None and m.high[y][x] is None and (x, y) not in taken and not m.road[y][x]): continue
                    if not any(walk(x + p, y + q) for p, q in D4): continue
                    cand = [x, y, a[2], a[3]]
                    if any(clash(cand, b) for j, b in enumerate(labs) if j != i): continue
                    best = (x, y); break
                if best: break
            if best: break
        if best:
            o = a[3]; moved.append((o['name'], (o['x'], o['y']), best)); o['x'], o['y'] = best; a[0], a[1] = best
    rep['labelMoved'] = moved

    # 8) 보이지 않는 벽 막기 (사용자 10/30: 라우가르 캐릭터 밑으로 못 감): 담당 밖 막힌 칸 중 그림이 없는 칸(언덕 윗면 relief 포함)이
    #    걷는 칸에 닿아 있으면 덤불을 심어 막힌 게 보이게 한다. 담당 안 칸은 런타임 그림(벌통·대상 짐 등) 자리라 건드리지 않는다.
    sealed = 0
    for y in range(H):
        for x in range(W):
            if mine[y, x] or not m.blocked[y][x] or m.water[y][x] or m.obj[y][x] or m.high[y][x]: continue
            if any(walk(x + a, y + b) for a, b in D4):
                m.obj[y][x] = 'nature:5:12' if (x + y) % 2 else 'nature:18:9'
                if str(m.ground[y][x] or '').startswith('relief'): m.ground[y][x] = nt.SNOW
                sealed += 1
    rep['sealed'] = sealed

    # 9) 길·물이 바뀐 칸과 이웃은 바닥 그림을 다시 맞춘다(finish 뒤라 그림이 옛 상태로 남음)
    import tiles as _t
    ch = {(x, y) for y in range(H) for x in range(W) if (m.road[y][x] > 0) != (road0[y][x] > 0) or m.water[y][x] != water0[y][x]}
    near = {(x + a, y + b) for (x, y) in ch for a in (-1, 0, 1) for b in (-1, 0, 1) if ins(x + a, y + b)}
    rmask = lambda a, b: m.road[min(max(b, 0), H - 1)][min(max(a, 0), W - 1)] > 0
    wmask = lambda a, b: (not ins(a, b)) or m.water[b][a]
    for (x, y) in near:
        if m.water[y][x]: m.ground[y][x] = _t.autotile(wmask, x, y, *nt.SNOW_WATER_ORIGIN)
        elif m.road[y][x] > 0: m.ground[y][x] = _t.autotile(rmask, x, y, *nt.SNOW_PATH_ORIGIN)
        elif (road0[y][x] > 0 or water0[y][x]) and not str(m.ground[y][x] or '').startswith('relief'): m.ground[y][x] = nt.SNOW
    rep['retiled'] = len(near)
    return rep
