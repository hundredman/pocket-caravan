"""북유럽 N5: 옛 맵(Tools/MapGen/north.py·north_center.py가 지금도 만드는 것)의 꾸밈·생활 자리를 새 맵으로 옮긴다. 2026-10-29.
n4_build.build()가 건물을 놓은 뒤, 숲을 심기 전에 transfer()를, 마무리(finish) 뒤에 after_finish()를 부른다.

옮기는 것 (옛 맵 → 같은 ID 새 맵. 국경 고개 → 비르카, 피오르 길 → 피오르 나루)
- 꾸밈 소품(통·상자·울타리 조각·깃발·토템·짚단·무기 걸이·좌판·꼬치 화덕·모닥불·폐허·돌배·수정 등): 가까이 붙은 것끼리 묶음째,
  옛 장터(없으면 옛 길 가운데)에서의 상대 위치를 새 장터(쉼터) 기준으로 옮기고, 안 맞으면 가장 가까운 빈 자리로
- 상호작용 자리(축제·박물관·점집·조합·순록 먹이·룬돌 고리·공방·공방 마당·나룻배·온천 목욕), 숨은 룬돌, 동물 무리, 썰매 길
- 낚시: 같은 수만큼 새 맵의 물가에
- 이정표: 맵 밖으로 나가는 길마다 하나 + 장터(쉼터)에 하나. 글은 새 길 기준
- 길을 오가는 상인: 옛 맵과 같은 수, 새 길 위를 오감
- 이벤트: 옛 맵보다 적으면 길 위에 더함
- 깃발 애니메이션
NorthLife.json의 장꾼·구경꾼·좌판 그림 자리 등은 옛 생성기(north_spots.py)의 같은 계산을 새 맵에 그대로 돌려 얻는다."""
import random, sys, os
from collections import deque
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__)); G = os.path.dirname(HERE)
sys.path.insert(0, G)
import tiles

HOUSES = {'longhall', 'longhall_roof', 'longhall_front', 'woodhouse', 'house3', 'h_dark', 'h_turf', 'h_round', 'h_dome', 'h_shed', 'h_stoneA', 'h_stoneB'}
SCENERY = {'snowpine', 'snowpine_g', 'snowround', 'snowpinebig', 'snowpinebig_g', 'snowroundbig', 'deadtree', 'deadtreebig',
           'rock', 'rockbig', 'rocksmall', 'snowboulder', 'stumpbig', 'baretree', 'boat', 'well', 'lantern'}
SPOT_KINDS = {'festival', 'museum', 'fortune', 'guild', 'feed', 'ring', 'ferry', 'bath', 'craft_fish', 'craft_fur', 'craft_brew',
              'yard_fish', 'yard_fur', 'yard_brew'}
YARD_SIZE = {'yard_fish': (11, 17), 'yard_fur': (11, 9), 'yard_brew': (14, 8)}
# 공방 마당 그림 아랫줄을 막는 칸(옛 north.py와 같음): (dx, dy, 폭)
YARD_BLOCK = {'yard_fish': [(1, 5, 4), (6, 5, 4), (1, 12, 4), (6, 12, 4)], 'yard_fur': [(1, 7, 4)],
              'yard_brew': [(1, 6, 2), (5, 6, 2), (9, 5, 2), (10, 2, 3)]}
SNOWIFY = {'tree': 'snowround', 'pine': 'snowpine', 'roundtree': 'snowround', 'cherry': 'snowround', 'bigtree': 'snowroundbig', 'blossom': 'snowroundbig', 'greenbig': 'snowroundbig'}
MERGED = {'north_birka': ['north_pass'], 'north_naru': ['north_fjord_road']}
_OLD = {}

def old_maps():
    if not _OLD:
        import north, north_spots
        for f in north.MAPS:
            m = north_spots.build(f); _OLD[m.id] = m
    return _OLD

def _road_center(m):
    pts = [(x, y) for y in range(m.h) for x in range(m.w) if m.road[y][x] > 0]
    if not pts: return (m.w // 2, m.h // 2)
    cx = sum(p[0] for p in pts) / len(pts); cy = sum(p[1] for p in pts) / len(pts)
    return min(pts, key=lambda p: (p[0] - cx) ** 2 + (p[1] - cy) ** 2)

def old_anchor(om):
    mk = [o for o in om.objects if o['kind'] == 'market']
    return (mk[0]['x'], mk[0]['y']) if mk else _road_center(om)

def features(om):
    """옛 맵의 옮길 것: (종류, 이름, x, y, w, h, 덧) 목록"""
    out = []
    secret_pos = {(x, y) for (x, y, i) in getattr(om, 'secrets', [])}
    for (n, x, y) in om.stamps:
        n = SNOWIFY.get(n, n)   # 2026-10-29(사용자): 눈밭에 초록 여름 나무·덤불이 섞여 보여서 눈 덮인 것으로
        if n in HOUSES or n in SCENERY or n not in tiles.STAMPS: continue
        if n == 'runestone_b' and (x, y) in secret_pos: continue   # 숨은 룬돌은 아래 'secret'으로 한 번만
        s = tiles.STAMPS[n]; out.append(('stamp', n, x, y, s[3], s[4], None))
    for (k, x, y) in getattr(om, 'spots', []):
        if k not in SPOT_KINDS: continue
        if k in YARD_SIZE: w, h = YARD_SIZE[k]; out.append(('yard', k, x, y, w, h, None))
        else: out.append(('spot', k, x, y, 1, 1, None))
    for (x, y, i) in getattr(om, 'secrets', []): out.append(('secret', 'runestone_b', x, y, 1, 2, i))
    for o in om.objects:
        if o['kind'] == 'anim' and o.get('sheet') == 'north_flag': out.append(('flag', 'flag', o['x'], o['y'], 1, 1, None))
    st = getattr(om, 'sled_track', None)
    if st:
        xs = [p[0] for p in st]; ys = [p[1] for p in st]
        out.append(('sled', 'sled', min(xs), min(ys), max(xs) - min(xs) + 1, max(ys) - min(ys) + 1, [(p[0] - min(xs), p[1] - min(ys)) for p in st]))
    return out

def clusters(fs, gap=3):
    """가까이 붙은 것끼리 묶음(모서리 거리 gap칸 이하)"""
    n = len(fs); par = list(range(n))
    def f(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    for i in range(n):
        for j in range(i + 1, n):
            a, b = fs[i], fs[j]
            if a[0] == 'sled' or b[0] == 'sled': continue   # 썰매 길은 따로
            dx = max(0, max(a[2], b[2]) - min(a[2] + a[4], b[2] + b[4])); dy = max(0, max(a[3], b[3]) - min(a[3] + a[5], b[3] + b[5]))
            if dx <= gap and dy <= gap: par[f(i)] = f(j)
    g = {}
    for i in range(n): g.setdefault(f(i), []).append(fs[i])
    return list(g.values())

class Placer:
    def __init__(s, m, mine, extra_block=None):
        s.m = m; s.mine = mine; s.H, s.W = mine.shape; s.used = np.zeros_like(mine)
        # 2026-10-29: 숲·산·다른 지역 땅과 N2 자리(사람·집) 둘레에는 놓지 않는다(나무 속에 묻혀 못 가던 문제)
        s.terrain = extra_block if extra_block is not None else np.zeros_like(mine)
        s.foot = []   # 놓은 것 (종류, 이름, x, y, w, h): n4_build가 둘레 비우기·길 검사에 씀
    def free(s, x, y, walk_only=False):
        m = s.m
        if not (0 <= x < s.W and 0 <= y < s.H) or not s.mine[y, x] or s.used[y, x] or s.terrain[y, x]: return False
        if m.blocked[y][x] or m.water[y][x] or m.obj[y][x] is not None or m.high[y][x] is not None: return False
        if walk_only: return True
        return m.road[y][x] == 0 and not m.reserved[y][x]
    def ok(s, cl, ox, oy):
        for (t, n, x, y, w, h, extra) in cl:
            X, Y = x + ox, y + oy
            if t == 'stamp' or t == 'secret':
                if not all(s.free(X + a, Y + b) for a in range(w) for b in range(h)): return False
                if t == 'secret' and not s.free(X, Y + 2, walk_only=True): return False
            elif t == 'yard':   # 마당 안에 길이 지나가도 됨(옛 맵도 그랬음). 그림 아래 막는 칸만 빈 땅이어야
                if not all(s.free(X + a, Y + b, walk_only=True) for a in range(w) for b in range(h)): return False
                if not all(s.free(X + dx + k, Y + dy) for (dx, dy, ww) in YARD_BLOCK[n] for k in range(ww)): return False
            elif t == 'sled':
                if not all(s.free(X + a, Y + b, walk_only=True) for (a, b) in extra): return False
            else:
                if not s.free(X, Y, walk_only=True): return False
        return True
    def put(s, cl, ox, oy, rec):
        m = s.m
        for (t, n, x, y, w, h, extra) in cl:
            X, Y = x + ox, y + oy
            if t == 'stamp':
                m.stamp(n, X, Y, force=True, over_road=True)
            elif t == 'secret':
                if m.stamp('runestone_b', X, Y, force=True): m.secrets.append((int(X), int(Y), extra))
                else: print('  숨은 룬돌 실패', m.id, X, Y, [(m.obj[Y + b][X], m.high[Y + b][X], m.road[Y + b][X], m.water[Y + b][X]) for b in range(2)])
            elif t == 'yard':
                m.spots.append((n, X, Y)); m.reserve(X, Y, X + w - 1, Y + h - 1)
                for (dx, dy, ww) in YARD_BLOCK[n]:
                    for k in range(ww): m.blocked[Y + dy][X + dx + k] = True
            elif t == 'sled':
                m.sled_track = [(X + a, Y + b) for (a, b) in extra]; m.road_path(m.sled_track, width=1)
            elif t == 'flag':
                m.flag(X, Y)
            else:
                m.spots.append((n, X, Y))
            s.used[max(0, Y - 2):Y + h + 2, max(0, X - 2):X + w + 2] = True   # 묶음끼리 2칸 띄움
            s.foot.append((t, n, X, Y, w, h))
            rec.append((t, n))
    def search(s, cl, ox, oy, R=30):
        for r in range(0, R + 1):
            ring = [(ox + a, oy + b) for a in range(-r, r + 1) for b in range(-r, r + 1) if max(abs(a), abs(b)) == r]
            for (a, b) in ring:
                if s.ok(cl, a, b): return (a, b)
        return None

def carve_yard(m, P, f_, ox, oy, R=60):
    """마당 자리: 물이 아닌 담당 땅(산·절벽 포함)만으로 w×h가 되는 곳 중 산 칸이 가장 적은 곳. 산 칸은 눈밭으로 깎는다"""
    t, n, x, y, w, h, _ = f_; best = None
    for Y in range(1, P.H - h - 1):
        for X in range(1, P.W - w - 1):
            cells = [(X + a, Y + b) for a in range(w) for b in range(h)]
            if not all(P.mine[c[1], c[0]] and not m.water[c[1]][c[0]] and not P.used[c[1], c[0]] and m.obj[c[1]][c[0]] is None and m.high[c[1]][c[0]] is None for c in cells): continue
            cut = sum(1 for c in cells if m.blocked[c[1]][c[0]] or P.terrain[c[1], c[0]])
            d = abs(X - (x + ox)) + abs(Y - (y + oy))
            if best is None or (cut, d) < best[0]: best = ((cut, d), X, Y, cells)
    if not best: return None
    for (cx, cy) in best[3]:
        P.terrain[cy, cx] = False
        if m.blocked[cy][cx]: m.blocked[cy][cx] = False; m.ground[cy][cx] = None; m.reserved[cy][cx] = False
    return (best[1] - x, best[2] - y)

def transfer(m, key, mine, new_anchor, rng_seed=0):
    """건물을 놓은 뒤, 숲 심기 전에. 반환: 보고(옮김·못 옮김 목록)"""
    rep = dict(placed=[], dropped=[], fishing=0, critters=0)
    OLD = old_maps(); P = Placer(m, mine, EXTRA_BLOCK.get(key)); LAST_PLACER[key] = P
    for oid in [key] + MERGED.get(key, []):
        om = OLD.get(oid)
        if om is None: continue
        oax, oay = old_anchor(om)
        nax, nay = new_anchor if oid == key else MERGED_ANCHOR.get(oid, new_anchor)
        fs = features(om)
        for cl in sorted(clusters(fs), key=lambda c: (0 if any(f[0] in ('yard', 'secret', 'spot', 'sled') for f in c) else 1, -sum(f[4] * f[5] for f in c))):   # 2026-10-29: 숨은 룬돌·상호작용 자리 먼저(땅이 좁으면 꾸밈 소품이 빠짐)
            ox, oy = nax - oax, nay - oay
            off = P.search(cl, ox, oy, R=60 if any(f_[0] == 'yard' for f_ in cl) else 30)
            if off: P.put(cl, off[0], off[1], rep['placed']); continue
            for f_ in cl:   # 묶음이 안 맞으면 하나씩
                off = P.search([f_], ox, oy, R=60)   # 2026-10-29: 숲·산을 막으니 30칸 안에 자리가 없을 때가 있어 60칸까지
                if off: P.put([f_], off[0], off[1], rep['placed']); continue
                if f_[0] != 'yard' and HARD_BLOCK.get(key) is not None:   # 숲 칸까지 써서라도(둘레·길은 n4_build가 비움)
                    soft = P.terrain; P.terrain = HARD_BLOCK[key]
                    off = P.search([f_], ox, oy, R=60); P.terrain = soft
                    if off: P.put([f_], off[0], off[1], rep['placed']); rep.setdefault('in_forest', []).append(f_[1]); continue
                    # 2026-10-29: 그래도 없으면 맵 전체에서 새 장터와 가장 가까운 빈 자리(숨은 룬돌이 빠지지 않게)
                    P.terrain = HARD_BLOCK[key]
                    cand = sorted(((X - f_[2], Y - f_[3]) for Y in range(P.H) for X in range(P.W)), key=lambda o: (o[0] + f_[2] - nax) ** 2 + (o[1] + f_[3] - nay) ** 2)
                    off = next((o for o in cand if P.ok([f_], *o)), None); P.terrain = soft
                    if off: P.put([f_], off[0], off[1], rep['placed']); rep.setdefault('in_forest', []).append(f_[1]); continue
                if f_[0] == 'yard':   # 2026-10-29(사용자 결정): 빈 땅이 없으면 산·절벽 칸을 깎아 자리를 낸다
                    off = carve_yard(m, P, f_, ox, oy)
                    if off: P.put([f_], off[0], off[1], rep['placed']); rep.setdefault('carved', []).append((f_[1], off)); continue
                rep['dropped'].append((oid, f_[0], f_[1]))
        # 동물 무리: 옛 자리를 같은 만큼 옮기고 맵 안으로
        for g in om.life:
            gx0, gy0, gx1, gy1 = g['x0'] + nax - oax, g['y0'] + nay - oay, g['x1'] + nax - oax, g['y1'] + nay - oay
            cells = [(x, y) for y in range(max(0, gy0), min(P.H, gy1 + 1)) for x in range(max(0, gx0), min(P.W, gx1 + 1)) if P.free(x, y, True)]
            if len(cells) < 4:   # 맵 밖이면 맵 안 빈 땅 가운데로
                pts = np.argwhere(mine); cy, cx = pts.mean(0); w_, h_ = gx1 - gx0, gy1 - gy0
                gx0, gy0 = int(cx - w_ / 2), int(cy - h_ / 2); gx1, gy1 = gx0 + w_, gy0 + h_
            m.life.append(dict(kind=g['kind'], x0=int(max(0, gx0)), y0=int(max(0, gy0)), x1=int(min(P.W - 1, gx1)), y1=int(min(P.H - 1, gy1)), count=g['count']))
            rep['critters'] += 1
        # 낚시: 같은 수만큼 물가(물 칸 + 옆 걷는 칸)
        need = len(getattr(om, 'fishing', []))
        if need:
            shore = [(x, y) for y in range(P.H) for x in range(P.W) if m.water[y][x] and mine[y, x] and (x, y) not in m._springs
                     and any(0 <= y + b < P.H and 0 <= x + a < P.W and mine[y + b, x + a] and not m.blocked[y + b][x + a] and not m.water[y + b][x + a] and m.obj[y + b][x + a] is None and not P.terrain[y + b, x + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]   # 낚시 자리 옆은 걸을 수만 있으면 됨
            rnd = random.Random(rng_seed + len(oid)); rnd.shuffle(shore)
            # 마을 가까운 물가 먼저
            shore.sort(key=lambda c: (c[0] - nax) ** 2 + (c[1] - nay) ** 2)
            got = []
            for c in shore:
                if len(got) >= need: break
                if all(abs(c[0] - g[0]) + abs(c[1] - g[1]) > 10 for g in got): got.append(c)
            m.fishing.extend(got); rep['fishing'] += len(got)
            if len(got) < need: rep['dropped'].append((oid, 'fishing', need - len(got)))
    return rep

MERGED_ANCHOR = {}
EXTRA_BLOCK = {}
HARD_BLOCK = {}   # 숲 칸은 허용하는 마지막 시도용(다른 지역 땅·N2 자리 둘레만 막음)   # n4_build가 맵마다 넣어 줌: 놓으면 안 되는 칸(숲·산·다른 지역·N2 자리 둘레)
LAST_PLACER = {}
LAKE_CELLS = {}   # n4_build가 맵마다 호수(LAKE) 칸을 넣어 줌

def after_finish(m, key, mine, seams, names, new_anchor, rng_seed=0):
    """마무리 뒤: 이정표, 오가는 상인, 모자란 이벤트"""
    OLD = old_maps(); rep = dict(signs=0, walkers=0, events_added=0)
    H, W = mine.shape
    def walk(x, y): return 0 <= x < W and 0 <= y < H and mine[y, x] and not m.blocked[y][x] and not m.water[y][x]
    road = [(x, y) for y in range(H) for x in range(W) if m.road[y][x] > 0 and walk(x, y)]
    taken = {(o['x'], o['y']) for o in m.objects}
    # 이정표: 나가는 길(출구 칸이 길 위)마다
    groups = {}
    for o in m.objects:
        if o['kind'] != 'exit': continue
        cells = [(o['x'] + a, o['y'] + b) for a in range(o.get('w', 1)) for b in range(o.get('h', 1))]
        if not any(m.inside(*c) and m.road[c[1]][c[0]] > 0 for c in cells): continue
        c_ = cells[len(cells) // 2]
        # 2026-10-31(기우: 사냥꾼 마을 아래 출구에 이정표 없음) 같은 곳으로 가는 출구라도 8칸 넘게 떨어져 있으면 따로 세운다
        k_ = next((k for k, v in groups.items() if k[0] == o['target'] and abs(v[0][0] - c_[0]) + abs(v[0][1] - c_[1]) <= 8), (o['target'], len(groups)))
        groups.setdefault(k_, []).append(c_)
    ARROW = {'N': '↑', 'S': '↓', 'W': '←', 'E': '→'}
    def side(edge): return ARROW.get(edge, '·')
    edge_of = {}
    for o in m.objects:
        if o['kind'] == 'exit': edge_of.setdefault(o['target'], o['edge'])
    for (tgt_, _), cs_ in groups.items():
        o = dict(target=tgt_, edge=next((q['edge'] for q in m.objects if q['kind'] == 'exit' and q['target'] == tgt_
                                         and q['x'] <= cs_[0][0] < q['x'] + q.get('w', 1) and q['y'] <= cs_[0][1] < q['y'] + q.get('h', 1)), edge_of.get(tgt_)))
        ex, ey = cs_[0]
        # 출구에서 안쪽으로 5~8칸 들어온 길가
        best = None
        for (x, y) in road:
            d = abs(x - ex) + abs(y - ey)
            if 5 <= d <= 9 and (best is None or d < best[0]): best = (d, x, y)
        if not best: continue
        x, y = best[1] + 1, best[2]
        while m.inside(x, y) and (m.road[y][x] > 0 or m.water[y][x] or m.blocked[y][x] or (x, y) in taken): x += 1
        if not walk(x, y): continue
        m.sign(x, y, f"{side(o['edge'])} {names.get(o['target'], o['target'])}"); taken.add((x, y)); rep['signs'] += 1
    # 장터(쉼터) 이정표: 이 맵에서 갈 수 있는 곳 모두
    dests = []
    for o in m.objects:
        if o['kind'] == 'exit' and any(k[0] == o['target'] for k in groups) and o['target'] not in [d[1] for d in dests]: dests.append((o['edge'], o['target']))
    if dests:
        ax, ay = new_anchor
        for r in range(2, 9):
            cand = [(x, y) for y in range(ay - r, ay + r + 1) for x in range(ax - r, ax + r + 1)
                    if walk(x, y) and m.road[y][x] == 0 and m.obj[y][x] is None and (x, y) not in taken and m.inside(x, y + 1)]
            if cand:
                x, y = cand[0]
                m.sign(x, y, names.get(key, key) + '\n' + '\n'.join(f"{side(e)} {names.get(t, t)}" for e, t in dests[:4])); rep['signs'] += 1; break
    # 오가는 상인: 옛 맵(과 합쳐진 맵)과 같은 수
    need_w = sum(1 for oid in [key] + MERGED.get(key, []) if oid in OLD for o in OLD[oid].objects if o['kind'] == 'walker')
    if need_w and road:
        rset = set(road); rnd = random.Random(rng_seed)
        ax, ay = new_anchor
        start = min(road, key=lambda c: (c[0] - ax) ** 2 + (c[1] - ay) ** 2)
        prev = {start: None}; q = deque([start]); far = start
        while q:
            c = q.popleft(); far = c
            for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (c[0] + a, c[1] + b)
                if n in rset and n not in prev: prev[n] = c; q.append(n)
        # 2026-10-29(사용자: 상인들이 몰려다님): 장터에서 다 같이 출발하지 않게, 길 위에서 서로 먼 끝점들을 골라 상인마다 다른 두 끝점 사이를 오가게
        reach_r = list(prev); pts_ = [max(reach_r, key=lambda c: abs(c[0] - start[0]) + abs(c[1] - start[1]))]
        while len(pts_) < min(len(reach_r), 2 * need_w + 1):
            pts_.append(max(reach_r, key=lambda c: min(abs(c[0] - p[0]) + abs(c[1] - p[1]) for p in pts_)))
        def bfs_path(a, b):
            pv = {a: None}; q2 = deque([a])
            while q2:
                c = q2.popleft()
                if c == b: break
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    n = (c[0] + dx, c[1] + dy)
                    if n in rset and n not in pv: pv[n] = c; q2.append(n)
            out = []; c = b
            while c is not None: out.append(c); c = pv.get(c)
            return out[::-1]
        for i in range(need_w):
            a, b = pts_[(2 * i) % len(pts_)], pts_[(2 * i + 1) % len(pts_)]
            if a == b: b = start
            path = bfs_path(a, b)
            if len(path) < 2: path = bfs_path(start, a)
            pts = path[::max(1, len(path) // 6)] + [path[-1]]
            pts = pts + pts[-2::-1]
            m.walker([list(p) for p in pts], idx=i)
            rep['walkers'] += 1
    # 물결 애니메이션: 옛 맵과 같은 수만큼 호수·온천 가운데 물 칸에(얼어붙은 피오르 바다는 그대로 둠)
    old_rip = sum(1 for oid in [key] + MERGED.get(key, []) if oid in OLD for o in OLD[oid].objects if o['kind'] == 'anim' and o.get('sheet') == 'ripples')
    have_rip = sum(1 for o in m.objects if o['kind'] == 'anim' and o.get('sheet') == 'ripples')
    if old_rip > have_rip:
        import tiles as _t
        inner = [(x, y) for y in range(1, H - 1) for x in range(1, W - 1) if mine[y, x] and all(m.water[y + b][x + a] for a in (-1, 0, 1) for b in (-1, 0, 1))
                 and (str(m.ground[y][x]).startswith('water:1:7') or (x, y) in m._springs or str(m.ground[y][x]).endswith('@thaw') or True)]
        lake_like = [c for c in inner if c in m._springs or LAKE_CELLS.get(key, set()).__contains__(c)]
        rr = random.Random(rng_seed + 9); rr.shuffle(lake_like)
        for (x, y) in lake_like[:old_rip - have_rip]:
            m.objects.append(dict(kind='anim', x=x, y=y, sheet='ripples', frames=4, fps=3.0 + rr.random()))
    # 이벤트: 옛 맵보다 적으면 길 위에 더함
    old_ev = sum(1 for oid in [key] + MERGED.get(key, []) if oid in OLD for o in OLD[oid].objects if o['kind'] == 'event')
    have = sum(1 for o in m.objects if o['kind'] == 'event')
    rnd = random.Random(rng_seed + 5); cand = [c for c in road if c not in taken]; rnd.shuffle(cand)
    for c in cand:
        if have >= old_ev: break
        if all(abs(c[0] - o['x']) + abs(c[1] - o['y']) > 12 for o in m.objects if o['kind'] == 'event'):
            m.event(*c); have += 1; rep['events_added'] += 1
    return rep
