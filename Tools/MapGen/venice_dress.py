"""석호 마을의 빈 포장 바닥에 운하 집(vh_*)을 줄지어 세운다 (2026-10-26, 제안).
    - 손으로 놓은 건물·광장·장터·NPC·출구는 그대로 두고, 남은 빈 바닥만 채운다.
    - 뒤가 물이나 다른 건물에 닿는 자리(운하 가·골목 끝)부터 고른다. 옆 집과는 붙여도 된다(줄지은 집).
    - 집 앞(문 쪽) 한 줄은 비운다. 집 하나를 놓을 때마다 출구·NPC·장터·문 앞이 모두 이어져 있는지 확인하고,
      끊기면 되돌린다. 걸을 수 있는 칸이 고립되어도 되돌린다.
    마을마다 벽색 비율이 다르다(부라노는 알록달록, 무라노는 벽돌·황토 등). 색 조합은 각색 제안이다."""
from collections import deque

# 마을: (벽색들, 종류 비율, 남길 빈 바닥 비율)
THEMES = {
    "venice_rialto": (["ochre", "salmon", "rose", "cream", "brick"], dict(narrow=2, house=3, shop=3, wide=2, wide_shop=2, palazzo=2), 1.0),   # 2026-10-29: 리알토는 빈 바닥을 집으로 채우지 않는다
    "venice_murano": (["brick", "ochre", "salmon", "cream"], dict(narrow=2, house=3, shop=2, wide=2, wide_shop=1), .78),
    "venice_burano": (["rose", "sky", "sage", "ochre", "salmon", "brick"], dict(narrow=3, house=3, shop=1, wide=1), .82),
    "venice_chioggia": (["cream", "ochre", "brick", "salmon"], dict(narrow=2, house=3, shop=2, wide=2, wide_shop=2), .78),
    "venice_santerasmo": (["cream", "ochre", "sage"], dict(house=2, wide=1), .75),
}
KEEP_PAD = {"spawn": 2, "exit": 3, "market": 3, "npc": 3, "board": 3, "talker": 3, "sign": 1, "worker": 3,
            "villager": 3, "walker": 1, "event": 2, "bandit": 2}      # 2026-10-29: 사람·장터 둘레를 넓게 비워 붐비지 않게


def _walk(m, x, y):
    return m.inside(x, y) and not m.blocked[y][x] and (not m.water[y][x] or (x, y) in m.decks or (x, y) in m.over_water)


def _reach(m, starts):
    seen = set(s for s in starts if _walk(m, *s))
    q = deque(seen)
    while q:
        x, y = q.popleft()
        for p in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if p not in seen and _walk(m, *p):
                seen.add(p)
                q.append(p)
    return seen


def _keep(m):
    """비워야 하는 칸: 사람·장치 둘레, 출구·도착 자리, 기존 건물 문 앞"""
    keep = set()
    for o in m.objects:
        pad = KEEP_PAD.get(o["kind"], 1)
        for dy in range(-pad, o.get("h", 1) + pad):
            for dx in range(-pad, o.get("w", 1) + pad):
                keep.add((o["x"] + dx, o["y"] + dy))
    for _, (x, y) in m.doors:
        for dy in range(0, 3):
            for dx in (-1, 0, 1):
                keep.add((x + dx, y + dy))
    return keep


def _targets(m):
    """이어져 있어야 하는 칸: 출구·도착 자리·사람/장치 옆·문 앞"""
    out = []
    for o in m.objects:
        cells = [(o["x"] + dx, o["y"] + dy) for dy in range(-1, o.get("h", 1) + 1) for dx in range(-1, o.get("w", 1) + 1)]
        walk = [c for c in cells if _walk(m, *c)]
        if walk:
            out.append(walk)
    for _, c in m.doors:
        if _walk(m, *c):
            out.append([c])
    return out


class Guard:
    """놓기 전후로 길이 끊기지 않는지 지킨다. place()가 실패하면 놓은 칸을 그대로 되돌린다."""

    def __init__(self, m):
        self.m = m
        start = next(((o["x"], o["y"]) for o in m.objects if o["kind"] == "spawn"), None)
        self.start = start
        self.reach = _reach(m, [start]) if start else set()
        self.targets = [t for t in _targets(m) if any(c in self.reach for c in t)]   # 처음부터 닿던 곳만 지킨다

    def place(self, name, x, y, high=1, stamp=False):
        import tiles
        m = self.m
        w, h = tiles.STAMPS[name][3:5]
        cells = [(x + dx, y + dy) for dy in range(h) for dx in range(w)]
        if not all(m.inside(*c) for c in cells):
            return None
        snap = {c: (m.obj[c[1]][c[0]], m.high[c[1]][c[0]], m.blocked[c[1]][c[0]], m.road[c[1]][c[0]], m.reserved[c[1]][c[0]]) for c in cells}
        nd, ns = len(m.doors), len(m.stamps)
        try:
            m.building(name, x, y, high=high)
        except ValueError:
            return None
        reach = _reach(m, [self.start]) if self.start else set()
        ok = not self.start or (not (self.reach - reach - set(cells)) and all(any(c in reach for c in t) for t in self.targets))
        if not ok:
            for (cx, cy), (o, hi, b, r, rs) in snap.items():
                m.obj[cy][cx], m.high[cy][cx], m.blocked[cy][cx], m.road[cy][cx], m.reserved[cy][cx] = o, hi, b, r, rs
            del m.doors[nd:]
            del m.stamps[ns:]
            return None
        self.reach = reach
        return [c for c in cells if snap[c][2] is False and m.blocked[c[1]][c[0]]]


def town_area(m, radius):
    """장터에서 물을 건너지 않고 radius걸음 안의 칸 (장터가 없으면 None)"""
    market = next((o for o in m.objects if o["kind"] == "market"), None)
    if not market:
        return None
    town, q = {}, deque()
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            c = (market["x"] + dx, market["y"] + dy)
            if _walk(m, *c):
                town[c] = 0; q.append(c)
    while q:
        c = q.popleft()
        if town[c] >= radius:
            continue
        for p in ((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)):
            if p not in town and m.inside(*p) and not m.water[p[1]][p[0]]:
                town[p] = town[c] + 1; q.append(p)
    return town


def dress(m, theme=None, zones=None, radius=22):
    theme = theme or THEMES.get(m.id)
    if not theme:
        return 0
    import tiles
    colors, kinds, keep_ratio = theme
    zones = zones or ("street", "court", "worn") + (("meadow",) if m.id == "venice_burano" else ())   # 어촌은 풀밭에도 (과수원 섬은 시골로 둔다)
    rng = m.rng
    keep = _keep(m)
    town = town_area(m, radius)       # 마을 안만 채운다. 둑길 징검다리 섬·먼 풀밭에는 세우지 않는다.

    def free(x, y):
        return (m.inside(x, y) and (town is None or (x, y) in town) and m.zone[y][x] in zones and not m.water[y][x]
                and not m.blocked[y][x] and m.obj[y][x] is None and m.high[y][x] is None and not m.reserved[y][x]
                and (x, y) not in keep and (x, y) not in m.decks)

    open0 = sum(1 for y in range(m.h) for x in range(m.w) if free(x, y))
    if open0 == 0:
        return 0
    guard = Guard(m)

    pool = []
    for kind, weight in kinds.items():
        for col in colors:
            for suffix in ("", "_tall") if kind in ("narrow", "house") else ("",):
                name = f"vh_{kind}{suffix}_{col}"
                if name in tiles.STAMPS:
                    pool += [name] * weight
    placed, opened = 0, open0
    houses = set()                                     # 새 집이 차지한 칸 (옆에 붙여도 되는 칸)

    def fits(name, x, y):
        w, h = tiles.STAMPS[name][3:5]
        if not all(free(x + dx, y + dy) for dy in range(h) for dx in range(w)):
            return False
        for dx in range(w):                            # 집 앞 한 줄은 걸을 수 있어야 (문 앞)
            c = (x + dx, y + h)
            if not (_walk(m, *c) and c not in houses and not m.water[c[1]][c[0]]):
                return False
        for dy in range(h):                            # 옆은 물만 아니면 된다 (기존 건물·새 집과 벽을 맞댄 줄집)
            for c in ((x - 1, y + dy), (x + w, y + dy)):
                if not m.inside(*c) or m.water[c[1]][c[0]]:
                    return False
        return True

    def back_score(name, x, y):
        w = tiles.STAMPS[name][3]
        s = sum(1 for dx in range(w) if not m.inside(x + dx, y - 1) or m.water[y - 1][x + dx] or m.blocked[y - 1][x + dx])
        s += sum(1 for dy in range(2) for c in ((x - 1, y + dy), (x + w, y + dy)) if c in houses) * 4
        return s

    for _round in range(400):
        if opened <= open0 * keep_ratio:
            break
        cand = [(x, y) for y in range(1, m.h - 2) for x in range(1, m.w - 2) if free(x, y)]
        if not cand:
            break
        best = None
        for _try in range(120):
            name = rng.choice(pool)
            x, y = rng.choice(cand)
            if not fits(name, x, y):
                continue
            sc = back_score(name, x, y) + rng.random()
            if best is None or sc > best[0]:
                best = (sc, name, x, y)
        if best is None:
            continue
        _, name, x, y = best
        w, h = tiles.STAMPS[name][3:5]
        nd = len(m.doors)
        blocked = guard.place(name, x, y)
        if blocked is None:
            continue
        cells = [(x + dx, y + dy) for dy in range(h) for dx in range(w)]
        houses.update(cells)
        for _, (dx, dy) in m.doors[nd:]:               # 새 집 문 앞도 비워 둔다
            keep.update((dx + a, dy + b) for a in (-1, 0, 1) for b in (0, 1))
        placed += 1
        opened -= len(blocked)
    return placed
