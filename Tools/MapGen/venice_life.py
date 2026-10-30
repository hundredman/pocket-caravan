"""베네치아·본토 서부 생활 연출·미니게임 자리 (2026-10-26). export_venice.py가 맵과 함께 부른다.
    출력: Assets/PocketCaravan/Resources/Venice/VeniceLife.json  {mapId: {...}}  (좌표는 맵 JSON과 같이 왼쪽 위가 0)
    - pigeons: 광장·안뜰(mosaic·court)의 걸을 수 있는 칸  - lamps: 건물 옆 포장길 칸(간격을 두고)
    - smoke: 굴뚝 있는 건물(공방·가마·집)의 지붕 위   - gondolas: 물 위 곧은 뱃길 [[x0,y],[x1,y]] (다리·부두와 겹치지 않게)
    - gulls: 물이 넓은 맵이면 true                     - glass / net: 미니게임 자리 (무라노 가마 앞 / 부라노 잔교 끝)
    맵 칸을 바꾸지 않는다(그리기·충돌과 무관). VeniceLife·VeniceMinigames가 읽는다."""
import json, os
import tiles

OUT_REL = "Assets/PocketCaravan/Resources/Venice/VeniceLife.json"


def _walk(m, x, y):
    return m.inside(x, y) and not m.blocked[y][x] and (not m.water[y][x] or (x, y) in m.decks or (x, y) in m.over_water)


def _spaced(cells, gap, limit):
    out = []
    for c in cells:
        if all(abs(c[0] - o[0]) + abs(c[1] - o[1]) >= gap for o in out):
            out.append(c)
            if len(out) >= limit:
                break
    return out


def _occupied(m):
    taken = {(o["x"], o["y"]) for o in m.objects}
    return taken


def life(m):
    taken = _occupied(m)
    cells = [(x, y) for y in range(m.h) for x in range(m.w)]
    d = {"h": m.h}
    plaza = [c for c in cells if m.zone[c[1]][c[0]] in ("mosaic", "court") and _walk(m, *c) and c not in taken]
    d["pigeons"] = _spaced(plaza[::3], 3, 10)
    paved = ("street", "court", "quay", "market", "worn")
    beside = []
    for x, y in cells:
        if m.zone[y][x] in paved and _walk(m, x, y) and (x, y) not in taken and (x, y) not in m.decks:
            if any(m.inside(x + dx, y + dy) and m.blocked[y + dy][x + dx] and not m.water[y + dy][x + dx] and m.obj[y + dy][x + dx]
                   for dx, dy in ((0, -1), (1, 0), (-1, 0))):
                beside.append((x, y))
    d["lamps"] = _spaced(beside, 9, 14)
    smoke = []
    for name, x, y in m.stamps:
        n = name.replace("vg_", "")
        if any(k in n for k in ("kiln", "furnace", "workshop", "farmhouse", "cottage", "inn", "fsh_house", "orc_house")):
            w = tiles.STAMPS[name][3]
            smoke.append((x + w // 2, max(0, y)))
    d["smoke"] = _spaced(smoke, 4, 6)
    # 곧은 뱃길: 위·아래 줄까지 물인 가로 줄 (다리·부두·배 소품을 피한다)
    def open_water(x, y):
        return m.inside(x, y) and m.water[y][x] and (x, y) not in m.decks and (x, y) not in m.over_water and not m.obj[y][x]
    lanes = []
    for y in range(2, m.h - 2):
        run = []
        for x in range(m.w + 1):
            if x < m.w and all(open_water(x, y + k) for k in (-1, 0, 1)):
                run.append(x)
                continue
            if len(run) >= 16:
                lanes.append((len(run), [[run[1], y], [run[-2], y]]))
            run = []
    lanes.sort(key=lambda t: -t[0])
    picked = []
    for _, lane in lanes:
        if all(abs(lane[0][1] - p[0][1]) >= 8 for p in picked):
            picked.append(lane)
        if len(picked) >= 3:
            break
    d["gondolas"] = picked
    water = sum(1 for x, y in cells if m.water[y][x])
    d["gulls"] = water > m.w * m.h * 0.2
    # 미니게임 자리
    # Q3 정직 시험의 부두 검수원 자리: 키오자 창고지기 옆 2~4칸의 걸을 수 있는 칸
    if m.id == "venice_chioggia":
        keeper = next((o for o in m.objects if o.get("npc") == "venice_warehouse"), None)
        if keeper:
            ring = sorted(((x, y) for y in range(keeper["y"] - 4, keeper["y"] + 5) for x in range(keeper["x"] - 4, keeper["x"] + 5)
                           if _walk(m, x, y) and (x, y) not in taken and 2 <= abs(x - keeper["x"]) + abs(y - keeper["y"]) <= 4),
                          key=lambda c: abs(c[0] - keeper["x"] + 3) + abs(c[1] - keeper["y"]))
            if ring:
                d["inspector"] = list(ring[0])
    # 숨은 사자 문장 자리: 걸어서 닿고 길·출구에서 가장 먼 칸 (건물·나무 뒤 구석)
    if m.id in LION_MAPS:
        d["lion"] = _hidden(m, taken)
    if m.id == "venice_murano":
        kiln = next(((x, y, n) for n, x, y in m.stamps if "furnace" in n), None) or next(((x, y, n) for n, x, y in m.stamps if "kiln" in n), None)
        if kiln:
            x, y, n = kiln
            w, h = tiles.STAMPS[n][3], tiles.STAMPS[n][4]
            # 가마 아래쪽(앞) 걸을 수 있는 칸 옆의, 가마 그림 맨 아래 칸에 놓는다 (사람 대신 '작업대'처럼 말을 건다)
            spot = next(((x + dx, y + h - 1) for dx in range(w) if _walk(m, x + dx, y + h)), (x, y + h - 1))
            d["glass"] = list(spot)
    def shop(names):
        """그 건물 그림 맨 아래 줄 가운데 칸(앞에 걸을 수 있는 칸이 있는 곳)"""
        for n, x, y in m.stamps:
            if any(k == n.replace("vg_", "") for k in names):
                w, h = tiles.STAMPS[n][3], tiles.STAMPS[n][4]
                for dx in sorted(range(w), key=lambda v: abs(v - w // 2)):
                    if _walk(m, x + dx, y + h) and (x + dx, y + h - 1) not in taken:
                        return [x + dx, y + h - 1]
        return None
    if m.id == "venice_rialto":
        d["museum"] = shop(("cap_hall",))
        # 모이 주머니: 사람(NPC·장터 등)에서 3칸 넘게 떨어진 비둘기 자리 (이름표가 겹치지 않게)
        far = [p for p in d["pigeons"] if all(abs(p[0] - o["x"]) + abs(p[1] - o["y"]) > 3 for o in m.objects
                                              if o["kind"] in ("npc", "market", "board", "talker", "sign"))]
        d["feed"] = list(far[len(far) // 2]) if far else (list(d["pigeons"][len(d["pigeons"]) // 2]) if d["pigeons"] else None)
    if m.id == "venice_murano":
        d["workshop_glass"] = shop(("n_gls_shop", "gls_artisan", "glass_workshop"))
    if m.id == "venice_burano":
        d["workshop_lace"] = shop(("n_fsh_netshed", "fsh_netshed", "n_fsh_shelter", "fsh_shelter"))
    if m.id == "venice_burano":
        ends = [(x, y) for (x, y) in m.decks if m.inside(x, y + 1) and m.water[y + 1][x] and (x, y + 1) not in m.decks
                and _walk(m, x, y - 1) and y > m.h * 0.6]
        if ends:
            ends.sort(key=lambda c: (-c[1], c[0]))
            d["net"] = list(ends[0])
    d.update(daily(m, taken))
    return d


def daily(m, taken):
    """2026-10-27 생활 자리: 앉는 벤치 앞 칸, 떠돌이 상인 자리, 곤돌라 나루, 물 차오르는 칸, 도착 자리 id"""
    from collections import deque
    out = {}
    seats = []
    for n, x, y in m.stamps:
        if "bench" in n:
            w, h = tiles.STAMPS[n][3], tiles.STAMPS[n][4]
            c = (x + w // 2, y + h)
            if _walk(m, *c) and c not in taken:
                seats.append(c)
    out["seats"] = _spaced(seats, 3, 8)
    market = next((o for o in m.objects if o["kind"] == "market"), None)
    if market:
        busy = {(o["x"] + dx, o["y"] + dy) for o in m.objects for dx in range(-4, 5) for dy in range(-4, 5)
                if o["kind"] not in ("anim", "growth", "ambience")}      # 2026-10-29: 사람 둘레 4칸을 피해 붐비지 않게
        start = next(((market["x"] + dx, market["y"] + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1, 2) if _walk(m, market["x"] + dx, market["y"] + dy)), None)
        dist, q = ({start: 0}, deque([start])) if start else ({}, deque())
        while q:
            c = q.popleft()
            for p in ((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)):
                if p not in dist and _walk(m, *p) and dist[c] < 18:
                    dist[p] = dist[c] + 1; q.append(p)
        def open_cell(c):
            return c not in busy and c not in m.decks and not m.water[c[1]][c[0]] and all(_walk(m, c[0] + dx, c[1] + dy) for dx in (-1, 0, 1) for dy in (0, 1))
        wander = sorted((d, c) for c, d in dist.items() if 5 <= d <= 14 and open_cell(c))
        out["wander"] = list(wander[0][1]) if wander else []
        # 곤돌라 나루: 장터에서 걸어서 닿는, 바로 옆(아래·좌우)이 물인 물가 칸
        def water_next(c):
            return any(m.inside(c[0] + dx, c[1] + dy) and m.water[c[1] + dy][c[0] + dx] and (c[0] + dx, c[1] + dy) not in m.decks
                       and (c[0] + dx, c[1] + dy) not in m.over_water for dx, dy in ((0, 1), (1, 0), (-1, 0)))
        docks = sorted((d, c) for c, d in dist.items() if 3 <= d and c not in busy and water_next(c) and not m.water[c[1]][c[0]])
        out["dock"] = list(docks[0][1]) if docks and not m.id.startswith("venice_c_") else []
    if not m.id.startswith("venice_c_"):
        flood = [(x, y) for y in range(m.h) for x in range(m.w)
                 if _walk(m, x, y) and not m.water[y][x] and (x, y) not in m.decks
                 and any(m.inside(x + dx, y + dy) and m.water[y + dy][x + dx] and (x + dx, y + dy) not in m.decks
                         for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2))]
        out["flood"] = flood
    # 길 잃은 순례자 자리 (장터 없는 본토 길 맵): 맵 가운데에 가까운, 큰길 바로 옆 풀밭 칸
    if m.id.startswith("venice_c_") and not market:
        cx, cy = m.w // 2, m.h // 2
        cand = [(x, y) for y in range(2, m.h - 2) for x in range(2, m.w - 2)
                if _walk(m, x, y) and not m.road[y][x] and (x, y) not in taken
                and any(m.inside(x + dx, y + dy) and m.road[y + dy][x + dx] >= 2 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        cand.sort(key=lambda c: abs(c[0] - cx) + abs(c[1] - cy))
        out["lost"] = list(cand[0]) if cand else []
    spawns = [o for o in m.objects if o["kind"] == "spawn" and o.get("id")]
    out["spawn_ids"] = [o["id"] for o in spawns]
    out["spawns"] = [(o["x"], o["y"]) for o in spawns]
    return out


LION_MAPS = ("venice_rialto", "venice_santerasmo", "venice_murano", "venice_burano", "venice_chioggia", "venice_c_vineyard", "venice_c_marsh")


def _hidden(m, taken):
    from collections import deque
    starts = [(o["x"], o["y"]) for o in m.objects if o["kind"] == "spawn"]
    seen = set(starts)
    q = deque(starts)
    while q:                                   # 걸어서 닿는 칸
        x, y = q.popleft()
        for p in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if p not in seen and _walk(m, *p):
                seen.add(p); q.append(p)
    road = [(x, y) for (x, y) in seen if m.road[y][x] >= 2]
    dist = {c: 0 for c in road}
    q = deque(road)
    while q:                                   # 길에서 떨어진 걸음 수
        c = q.popleft()
        for p in ((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)):
            if p in seen and p not in dist:
                dist[p] = dist[c] + 1; q.append(p)
    cands = [c for c in seen if c not in taken and (c not in m.decks) and not m.water[c[1]][c[0]]
             and 2 <= c[0] < m.w - 2 and 2 <= c[1] < m.h - 2]
    if not cands:
        return []
    best = max(cands, key=lambda c: (dist.get(c, 0), -abs(c[0] - m.w // 2)))
    return [best[0], best[1]]


def _market(m):
    """장터 자리 [x, y] (큰 장 장식을 그 둘레에 놓는다)"""
    o = next((o for o in m.objects if o["kind"] == "market"), None)
    return [o["x"], o["y"]] if o else []


def _flat(points):
    return [v for p in points for v in p]


def export(maps, root):
    """Unity JsonUtility로 읽도록 평평한 정수 배열로 쓴다: {"maps":[{"id","h","pigeons":[x,y,...],"gondolas":[x0,y0,x1,y1,...],...}]}"""
    out = []
    for m in maps:
        d = life(m)
        out.append(dict(id=m.id, h=d["h"], pigeons=_flat(d["pigeons"]), lamps=_flat(d["lamps"]), smoke=_flat(d["smoke"]),
                        gondolas=[v for a, b in d["gondolas"] for v in (a[0], a[1], b[0], b[1])], gulls=d["gulls"],
                        glass=d.get("glass", []), net=d.get("net", []),
                        workshop_glass=d.get("workshop_glass") or [], workshop_lace=d.get("workshop_lace") or [],
                        market=_market(m), lion=d.get("lion") or [], inspector=d.get("inspector") or [],
                        museum=d.get("museum") or [], feed=d.get("feed") or [],
                        seats=_flat(d.get("seats", [])), wander=d.get("wander") or [], dock=d.get("dock") or [],
                        flood=_flat(d.get("flood", [])), lost=d.get("lost") or [], spawn_ids=d.get("spawn_ids", []), spawns=_flat(d.get("spawns", []))))
    data = {"maps": out}
    path = os.path.join(root, OUT_REL)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as fp:
        json.dump(data, fp, ensure_ascii=False, separators=(",", ":"))
    return data
