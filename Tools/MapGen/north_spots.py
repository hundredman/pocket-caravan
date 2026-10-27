"""10/27 북유럽 맵의 런타임 자리 계산 (장꾼·좌판·떠돌이 상인·꼬치 장수·여관·낚시 등)과 이름표 겹침 정리.
export_north.py에서 옮겨 옴: 맵 JSON을 쓰기 전에 이름표 정리(declutter)를 하려면 먼저 정의돼 있어야 해서."""
import tiles


def _half(label):
    return max(1.0, len(label) * 0.18 + 0.4)      # 이름표 반폭 (칸): 한글 한 글자 ≈ 0.36칸


def _clash(a, b):
    (ax, ay, at), (bx, by, bt) = a, b
    return abs(ay - by) <= 1 and abs(ax - bx) < _half(at) + _half(bt)


YARD_SIZE = {"yard_fish": (11, 17), "yard_fur": (11, 9), "yard_brew": (14, 8)}


def in_yard(m, x, y):
    """공방 마당(울타리) 안이면 True — 좌판·떠돌이 상인을 마당 안에 세우지 않게"""
    for (k, ax, ay) in getattr(m, "spots", []):
        if k in YARD_SIZE:
            w, h = YARD_SIZE[k]
            if ax - 1 <= x <= ax + w and ay - 1 <= y <= ay + h:
                return True
    return False


def base_labels(m):
    """맵에 박힌 이름표: 장터 상인·게시판·퀘스트 NPC·마을 사람·이정표 (x, y, 글자)"""
    out = []
    for o in m.objects:
        k = o["kind"]
        if k == "market": out.append((o["x"], o["y"], m.name + " 상인"))
        elif k == "board": out.append((o["x"], o["y"], "의뢰 게시판"))
        elif k == "npc": out.append((o["x"], o["y"], "욕심쟁이 할 볼드"))
        elif k == "talker": out.append((o["x"], o["y"], o.get("name", "")))
        elif k == "sign": out.append((o["x"], o["y"], "이정표"))
    return out


def _clear(x, y, label, labels):
    return not any(_clash((x, y, label), l) for l in labels)


def nearest_walk(m, fx, fy, water=False, avoid=()):
    """(fx, fy)에서 가장 가까운 걷는 칸 (water=True면 물에 닿은 칸만)"""
    best = None
    for y in range(m.h):
        for x in range(m.w):
            if m.blocked[y][x] or m.water[y][x] or (x, y) in avoid:
                continue
            if water and not any(0 <= x + a < m.w and 0 <= y + b < m.h and m.water[y + b][x + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                continue
            d = abs(x - fx) + abs(y - fy)
            if best is None or d < best[0]:
                best = (d, x, y)
    return (best[1], best[2]) if best else None


def museum_row(m):
    """전시대 6개를 놓을 빈 줄: 전시실 자리 근처에서 걷는 칸이 가로 6칸 연속인 줄 (x0, y)"""
    sp = [(x, y) for (k, x, y) in getattr(m, "spots", []) if k == "museum"]
    if not sp:
        return ()
    fx, fy = sp[0]
    best = None
    for y in range(max(1, fy - 6), min(m.h - 1, fy + 2)):
        for x in range(max(1, fx - 8), min(m.w - 7, fx + 4)):
            if all(not m.blocked[y][x + i] and not m.water[y][x + i] and m.obj[y][x + i] is None for i in range(6)):
                d = abs(x + 3 - fx) + abs(y - (fy - 2))
                if best is None or d < best[0]:
                    best = (d, x, y)
    return (best[1], best[2]) if best else ()


def market_spots(m):
    """10/27 장터 둘레 자리: 좌판 둘(장터 왼쪽·오른쪽, 장꾼은 좌판 오른쪽 옆)과 큰 장 날 구경꾼 넷.
    좌판은 길이 아니고 막히지 않은 가로 2칸 + 그 위 칸(장꾼)이 빈 곳, 구경꾼은 길 옆 빈 칸. 장터 칸에서 가까운 순.
    돌려주는 값: (stalls [x, y, ...], crowd [x, y, ...])"""
    mk = [o for o in m.objects if o["kind"] == "market"]
    if not mk:
        return [], []
    mx, my = mk[0]["x"], mk[0]["y"]
    used = {(o["x"], o["y"]) for o in m.objects}
    kp = keeper_spots(m)
    used |= {(kp[i] + a, kp[i + 1] + b) for i in range(0, len(kp), 2) for a in (-1, 0, 1) for b in (-1, 0, 1)}   # 장꾼 자리와 그 둘레는 비움
    used |= {(x + a, y + b) for (n, x, y) in m.stamps if n == "stall" for a in range(-1, 4) for b in range(-1, 3)}   # 10/27 원래 좌판(3x2) 둘레엔 줄무늬 좌판을 붙이지 않음
    def free(x, y):
        return 1 <= x < m.w - 1 and 1 <= y < m.h - 1 and not m.blocked[y][x] and not m.water[y][x] and m.obj[y][x] is None and m.high[y][x] is None and (x, y) not in used and not in_yard(m, x, y)
    stalls = []
    people = [(o["x"], o["y"]) for o in m.objects if o["kind"] in ("talker", "npc", "board", "sign")]
    labels = base_labels(m) + [(kp[i], kp[i + 1], "잉그리드 (꿀술 좌판)") for i in range(0, len(kp), 2)]
    for side in (-1, 1):
        best = None
        for y in range(my - 4, my + 6):
            for x in range(mx + side * 3, mx + side * 11, side):
                # 10/27 좌판 그림은 3칸 폭(x-1 ~ x+1), 장꾼은 좌판 오른쪽 옆 칸(x+2)에 선다
                # 장꾼은 좌판 바로 아래 칸(x, y+1)에 선다: 길이 아니어야 함
                if all(free(x + a, y) and m.road[y][x + a] == 0 for a in (-1, 0, 1)) and free(x, y + 1) and m.road[y + 1][x] == 0 and free(x, y - 1) \
                        and not any(x - 2 <= px <= x + 2 and y - 3 <= py <= y + 1 for (px, py) in people) \
                        and _clear(x, y + 1, "잉그리드 (꿀술 좌판)", labels + [(stalls[i], stalls[i + 1] + 1, "잉그리드 (꿀술 좌판)") for i in range(0, len(stalls), 2)]):   # 좌판 그림 위에 사람이 겹치지 않게, 장꾼 이름표도 안 겹치게
                    d = abs(x - (mx + side * 6)) + abs(y - (my + 2)) * 2
                    if best is None or d < best[0]:
                        best = (d, x, y)
        if best:
            stalls += [best[1], best[2]]
            used |= {(best[1] + a, best[2]) for a in (-1, 0, 1)} | {(best[1], best[2] - 1), (best[1], best[2] + 1)}
    crowd = []
    cand = []
    for y in range(my - 4, my + 7):
        for x in range(mx - 9, mx + 10):
            if free(x, y) and m.road[y][x] == 0 and any(m.inside(x + a, y + b) and m.road[y + b][x + a] > 0 for a in (-1, 0, 1) for b in (-1, 0, 1)):
                cand.append((abs(x - mx) + abs(y - my), x, y))
    for d, x, y in sorted(cand):
        if len(crowd) >= 8:
            break
        if all(abs(x - crowd[i]) + abs(y - crowd[i + 1]) >= 3 for i in range(0, len(crowd), 2)):
            crowd += [x, y]
    return stalls, crowd


def keeper_spots(m):
    """10/27 이미 있는 장터 좌판(stall, 3x2)마다 장꾼 한 명: 좌판 바로 아래(앞) → 옆 순서로 길이 아닌 빈 칸.
    장터에서 가까운 좌판 넷까지. 수입품 좌판처럼 바로 옆에 이미 사람이 있으면 건너뜀."""
    mk = [o for o in m.objects if o["kind"] == "market"]
    if not mk:
        m._keeper_stalls = []
        return []
    mx, my = mk[0]["x"], mk[0]["y"]
    people = [(o["x"], o["y"]) for o in m.objects if o["kind"] in ("talker", "npc")]
    used = {(o["x"], o["y"]) for o in m.objects}
    def free(x, y):
        return 1 <= x < m.w - 1 and 1 <= y < m.h - 1 and not m.blocked[y][x] and not m.water[y][x] and m.obj[y][x] is None and m.high[y][x] is None and (x, y) not in used
    out, pairs = [], []
    stalls = sorted([(abs(x + 1 - mx) + abs(y - my), x, y) for (n, x, y) in m.stamps if n == "stall"])
    for d, x, y in stalls:
        if len(out) >= 8:
            break
        if any(x - 2 <= px <= x + 4 and y - 1 <= py <= y + 3 for (px, py) in people):
            continue
        # 10/27 좌판 바로 아래(앞) 가운데 → 아래 양옆 → 옆. 길 위에는 세우지 않는다 (길을 막지 않게)
        cands = [(x + 1, y + 2), (x, y + 2), (x + 2, y + 2), (x - 1, y + 1), (x + 3, y + 1)]
        labels = base_labels(m) + [(out[i], out[i + 1], "잉그리드 (꿀술 좌판)") for i in range(0, len(out), 2)]
        pick = next((c for c in cands if free(*c) and m.road[c[1]][c[0]] == 0 and _clear(c[0], c[1], "잉그리드 (꿀술 좌판)", labels)), None)
        if pick:
            out += [pick[0], pick[1]]
            used.add(pick)
            pairs += [x + 1, y + 1]          # 이 장꾼이 맡은 좌판 (가운데 칸, 아랫줄)
    m._keeper_stalls = pairs
    return out


def keeper_stalls(m):
    """10/27 keeper_spots와 같은 순서로, 장꾼마다 맡은 좌판 [가운데 x, 아랫줄 y, ...] (장꾼 그림은 그 계산대 뒤에 선다)"""
    keeper_spots(m)
    return m._keeper_stalls


def stall_art(m):
    """10/27 맵에 놓인 좌판(stall 3x2) 자리 [가운데 x, 아랫줄 y, ...]: 타일 대신 새 좌판 그림을 런타임에 그린다"""
    return [v for (n, x, y) in m.stamps if n == "stall" for v in (x + 1, y + 1)]


def side_spots(m):
    """10/27 장터 옆 떠돌이 상인·오늘의 특가 자리 둘: 장터에서 3~7칸, 길이 아니고, 장꾼·좌판·다른 사람과 3칸 이상 떨어진 빈 칸"""
    mk = [o for o in m.objects if o["kind"] == "market"]
    if not mk:
        return []
    mx, my = mk[0]["x"], mk[0]["y"]
    taken = [(o["x"], o["y"]) for o in m.objects if o["kind"] in ("talker", "npc", "board", "sign", "market")]
    kp = keeper_spots(m); st, cr = market_spots(m)
    taken += [(kp[i], kp[i + 1]) for i in range(0, len(kp), 2)] + [(st[i] + a, st[i + 1] + b) for i in range(0, len(st), 2) for a in (-1, 0, 1) for b in (-2, -1, 0, 1)]
    lab = base_labels(m) + [(kp[i], kp[i + 1], "잉그리드 (꿀술 좌판)") for i in range(0, len(kp), 2)] + [(st[i], st[i + 1] + 1, "잉그리드 (꿀술 좌판)") for i in range(0, len(st), 2)]
    cand = []
    for y in range(my - 6, my + 7):
        for x in range(mx - 8, mx + 9):
            d = abs(x - mx) + abs(y - my)
            if not (3 <= d <= 8) or not (1 <= x < m.w - 1 and 1 <= y < m.h - 1):
                continue
            if m.blocked[y][x] or m.water[y][x] or m.obj[y][x] is not None or m.high[y][x] is not None or m.road[y][x] > 0 or in_yard(m, x, y):
                continue
            if any(abs(x - a) + abs(y - b) < 3 for (a, b) in taken):
                continue
            if not _clear(x, y, "오늘의 특가 좌판", lab):
                continue                     # 이름표가 옆 사람·장터·장꾼 이름표와 겹치지 않게
            cand.append((d, x, y))
    out = []
    for d, x, y in sorted(cand):
        if len(out) >= 4:
            break
        if all(abs(x - out[i]) + abs(y - out[i + 1]) >= 4 and _clear(x, y, "오늘의 특가 좌판", [(out[i], out[i + 1], "오늘의 특가 좌판")]) for i in range(0, len(out), 2)):
            out += [x, y]
    return out


def spit_sellers(m):
    """10/27 고기 꼬치 화덕(spit, 2x2) 옆 꼬치 장수 자리: 화덕 오른쪽 → 왼쪽 → 아래, 길이 아닌 빈 칸"""
    used = {(o["x"], o["y"]) for o in m.objects}
    out = []
    for (n, x, y) in m.stamps:
        if n != "spit":
            continue
        for (cx, cy) in [(x + 2, y + 1), (x - 1, y + 1), (x, y + 2), (x + 1, y + 2), (x + 2, y), (x - 1, y)]:
            if not (1 <= cx < m.w - 1 and 1 <= cy < m.h - 1) or m.blocked[cy][cx] or m.water[cy][cx] or m.obj[cy][cx] is not None or m.high[cy][cx] is not None or (cx, cy) in used:
                continue
            # 길이 아니거나, 넓은 광장(둘레 8칸이 다 길)이면 서도 길을 막지 않는다
            wide = sum(1 for a in (-1, 0, 1) for b in (-1, 0, 1) if m.road[cy + b][cx + a] > 0) >= 9
            if (m.road[cy][cx] == 0 or wide) and _clear(cx, cy, "꼬치 장수 토르", base_labels(m)):
                out += [cx, cy]; used.add((cx, cy)); break
    return out


def inn_spots(m):
    """10/27 여관 주인 옆에서 쉬어 가는 자리 (여관 주인·여관 할머니 바로 옆 걷는 칸)"""
    out = []
    for o in m.objects:
        if (o["kind"] == "talker" and "여관" in o.get("name", "")) or (o["kind"] == "npc" and o.get("npc") == "north_innkeeper"):
            dx = 1 if o["kind"] == "talker" else 3     # 퀘스트 NPC(여관 할머니) 옆은 이름표가 겹치지 않게 세 칸 떨어진 손님방 자리
            c = nearest_walk(m, o["x"] + dx, o["y"], avoid={(o["x"], o["y"])})
            if c:
                out += [c[0], c[1]]
    return out


def secret_cells(m):
    """숨은 룬돌마다 읽는 칸 (돌 바로 아래 걷는 칸) + 번호"""
    out = []
    for (x, y, idx) in getattr(m, "secrets", []):
        c = nearest_walk(m, x, y + 2)
        out.append((c[0], c[1], idx))
    return out


def spot_cells(m, kind):
    return [nearest_walk(m, x, y, water=(kind == "bath")) for (k, x, y) in getattr(m, "spots", []) if k == kind]


def fishing_cells(m):
    """낚시 자리마다 물에 닿은 가장 가까운 걷는 칸 (그 칸에 서서 Space)"""
    out = []
    for (fx, fy) in getattr(m, "fishing", []):
        best = None
        for y in range(m.h):
            for x in range(m.w):
                if m.blocked[y][x] or m.water[y][x]:
                    continue
                if not any(0 <= x + a < m.w and 0 <= y + b < m.h and m.water[y + b][x + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    continue
                d = abs(x - fx) + abs(y - fy)
                if best is None or d < best[0]:
                    best = (d, x, y)
        if best:
            out.append((best[1], best[2]))
    return out




def fish_edge(m):
    """10/27 얼음 낚시: 낚시 자리가 있는 갇힌 호수·웅덩이의 둘레 걷는 칸 전부 [x, y, ...] — 둘레 어디서나 Space로 낚시"""
    body = set()
    for (fx, fy) in getattr(m, "fishing", []):
        if not (m.inside(fx, fy) and m.water[fy][fx]) or (fx, fy) in body:
            continue
        lake, stack = set(), [(fx, fy)]
        while stack:
            x, y = stack.pop()
            if (x, y) in lake or not m.inside(x, y) or not m.water[y][x]:
                continue
            lake.add((x, y))
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
        # 바다·개울처럼 맵 밖으로 이어진 물은 뺀다 (부두·나룻배 앞에서 말 걸기를 가로채지 않게): 갇힌 호수·웅덩이만
        if not any(x in (0, m.w - 1) or y in (0, m.h - 1) for (x, y) in lake):
            body |= lake
    out = set()
    for (x, y) in body:
        for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            cx, cy = x + a, y + b
            if 1 <= cx < m.w - 1 and 1 <= cy < m.h - 1 and not m.water[cy][cx] and not m.blocked[cy][cx]:
                out.add((cx, cy))
    return [v for c in sorted(out) for v in c]


# ---------- 10/27 이름표 겹침 정리 ----------
# 게임에서 사람·표지판·자리 이름표는 머리 위 한 줄로 가운데 맞춰 그려진다. 글자 수로 폭을 어림해서
# 같은 줄(위아래 1칸 안)에 폭이 겹치면 "겹침"으로 본다. 마을 사람(talker)과 이정표(sign)만 옮긴다
# (퀘스트 NPC·장터·게시판·공방·장꾼 같은 자리는 그대로 두고 기준으로 씀).

def label_anchors(m):
    """옮기지 않는 이름표들: (x, y, 글자)"""
    out = []
    for o in m.objects:
        k = o["kind"]
        if k == "market": out.append((o["x"], o["y"], m.name + " 상인"))
        elif k == "board": out.append((o["x"], o["y"], "의뢰 게시판"))
        elif k == "npc": out.append((o["x"], o["y"], "욕심쟁이 할 볼드"))
    kp = keeper_spots(m)
    out += [(kp[i], kp[i + 1], "잉그리드 (꿀술 좌판)") for i in range(0, len(kp), 2)]
    st, _ = market_spots(m)
    out += [(st[i], st[i + 1] + 1, "잉그리드 (꿀술 좌판)") for i in range(0, len(st), 2)]
    sd = side_spots(m)
    out += [(sd[i], sd[i + 1], "오늘의 특가 좌판") for i in range(0, len(sd), 2)]
    sp = spit_sellers(m)
    out += [(sp[i], sp[i + 1], "꼬치 장수 토르") for i in range(0, len(sp), 2)]
    names = {"craft_fish": "덕장 일꾼 (얼음 대구 3 → 말린 대구)", "craft_fur": "털옷 장인 (모피 2 → 털옷)", "craft_brew": "양조장 할머니 (꿀술 2 + 약초)",
             "ferry": "나룻배 사공", "fortune": "룬 점쟁이", "museum": "회관 전시실 (0/6)", "guild": "낚시꾼 조합", "feed": "순록 우리 (이끼 주기)",
             "festival": "축제 생선 장수", "seat": "불 쬐기", "ring": "룬돌 고리"}
    for (k, x, y) in getattr(m, "spots", []):
        if k in names:
            out.append((x, y, names[k]))
    for (x, y, i) in getattr(m, "secrets", []):
        out.append((x, y + 2, "이끼 낀 룬돌"))
    return out


def declutter(m):
    """겹치는 마을 사람·이정표를 가까운 빈 땅으로 옮긴다 (길 옆을 먼저, 맵 끝·길 위·물·막힌 칸 제외)."""
    fixed = label_anchors(m)
    movers = [o for o in m.objects if o["kind"] in ("talker", "sign")]
    def label(o):
        return o.get("name") or "이정표"
    placed = list(fixed)
    taken = {(o["x"], o["y"]) for o in m.objects}
    taken |= {(x, y) for (x, y, t) in fixed}
    # 10/27 줄무늬 좌판 그림(3칸 폭·2줄)과 그 바로 위 줄, 기존 좌판 앞 장꾼 자리 둘레에는 사람을 옮겨 놓지 않는다
    st, _ = market_spots(m)
    for i in range(0, len(st), 2):
        taken |= {(st[i] + a, st[i + 1] + b) for a in (-2, -1, 0, 1, 2) for b in (-3, -2, -1, 0, 1)}
    moves = []
    for o in movers:
        me = (o["x"], o["y"], label(o))
        if not any(_clash(me, p) for p in placed):
            placed.append(me); continue
        best = None
        for r in range(1, 8):
            for y in range(o["y"] - r, o["y"] + r + 1):
                for x in range(o["x"] - r, o["x"] + r + 1):
                    if max(abs(x - o["x"]), abs(y - o["y"])) != r or not (4 <= x <= m.w - 5 and 3 <= y <= m.h - 3):
                        continue
                    if m.blocked[y][x] or m.water[y][x] or m.obj[y][x] is not None or m.high[y][x] is not None or m.road[y][x] > 0 or (x, y) in taken:
                        continue
                    if (x, y) in getattr(m, "decks", {}) or (x, y) in getattr(m, "_seats", ()):
                        continue
                    if not any(0 <= x + a < m.w and 0 <= y + b < m.h and m.road[y + b][x + a] > 0 for a in (-2, -1, 0, 1, 2) for b in (-2, -1, 0, 1, 2)):
                        continue                       # 길에서 2칸 안 (찾아가기 쉽게)
                    cand = (x, y, label(o))
                    if any(_clash(cand, p) for p in placed):
                        continue
                    by_road = any(m.road[y + b][x + a] > 0 for a in (-1, 0, 1) for b in (-1, 0, 1) if 0 <= x + a < m.w and 0 <= y + b < m.h)
                    key = (0 if by_road else 1, abs(x - o["x"]) + abs(y - o["y"]))
                    if best is None or key < best[0]:
                        best = (key, x, y)
            if best is not None:
                break
        if best:
            taken.discard((o["x"], o["y"]))
            moves.append((label(o), (o["x"], o["y"]), (best[1], best[2])))
            o["x"], o["y"] = best[1], best[2]
            taken.add((o["x"], o["y"]))
        placed.append((o["x"], o["y"], label(o)))
    m.label_moves = moves
    return m


TREES = {"snowpine", "snowpine_g", "snowround", "deadtree", "snowpinebig", "snowpinebig_g", "snowroundbig", "deadtreebig"}
SMALL = ["sack", "basket", "crate", "firewood", "barrel_s"]   # 나무 뺀 자리에 놓는 1칸 소품


def _stall_zones(m):
    """좌판·장꾼·꼬치 장수 둘레 (이 안에 나무가 있으면 뺀다)"""
    st, _ = market_spots(m)
    z = [(st[i] - 3, st[i + 1] - 3, st[i] + 3, st[i + 1] + 2) for i in range(0, len(st), 2)]          # 줄무늬 좌판(3칸) + 장꾼
    z += [(x - 2, y - 2, x + 4, y + 3) for (n, x, y) in m.stamps if n == "stall"]                       # 원래 있던 좌판 3x2
    kp = keeper_spots(m) + spit_sellers(m)
    z += [(kp[i] - 2, kp[i + 1] - 2, kp[i] + 2, kp[i + 1] + 1) for i in range(0, len(kp), 2)]
    return z


def _snapshot(m):
    return (market_spots(m), keeper_spots(m), side_spots(m), spit_sellers(m))


def _unstamp(m, name, x, y):
    w, h = tiles.STAMPS[name][3], tiles.STAMPS[name][4]
    for dy in range(h):
        for dx in range(w):
            if m.inside(x + dx, y + dy):
                m.obj[y + dy][x + dx] = None; m.high[y + dy][x + dx] = None; m.blocked[y + dy][x + dx] = False
    m.stamps.remove((name, x, y))


def clear_stall_trees(m):
    """10/27 좌판 옆에 나무가 붙어 있으면 별로라 뺀다 (모든 장터 마을). 맵 가장자리 숲(바깥 3칸)은 그대로.
    뺀 나무 밑동 자리에 1칸 소품(자루·바구니·상자·장작·작은 통)을 놓는다 — 단 좌판·장꾼 자리가 바뀌면 소품은 안 놓음."""
    if not any(o["kind"] == "market" for o in m.objects):
        return m
    removed = []
    for _ in range(4):
        zones = _stall_zones(m)
        hit = []
        for (n, x, y) in list(m.stamps):
            if n not in TREES:
                continue
            w, h = tiles.STAMPS[n][3], tiles.STAMPS[n][4]
            if x < 3 or y < 3 or x + w > m.w - 3 or y + h > m.h - 3:
                continue
            if any(x <= x1 and x + w - 1 >= x0 and y <= y1 and y + h - 1 >= y0 for (x0, y0, x1, y1) in zones):
                hit.append((n, x, y))
        if not hit:
            break
        for t in hit:
            _unstamp(m, *t); removed.append(t)
    m.removed_trees = removed
    base = _snapshot(m)
    for i, (n, x, y) in enumerate(removed):
        before = _reach_count(m)
        h = tiles.STAMPS[n][4]
        cx, cy = x, y + h - 1
        if m.road[cy][cx] > 0 or m.obj[cy][cx] is not None or m.blocked[cy][cx]:
            continue
        (st, cr), kp, sd, sp = base
        ppl = [(st[j], st[j + 1] + 1) for j in range(0, len(st), 2)] + [(st[j] + a, st[j + 1]) for j in range(0, len(st), 2) for a in (-1, 0, 1)]
        ppl += [(v[j], v[j + 1]) for v in (kp, sd, sp) for j in range(0, len(v), 2)] + [(o["x"], o["y"]) for o in m.objects if o["kind"] in ("talker", "npc")]
        if any(abs(cx - a) <= 1 and abs(cy - b) <= 1 for (a, b) in ppl):
            continue                      # 사람·좌판 바로 옆엔 소품을 두지 않는다 (붙어 보여 답답함)
        name = SMALL[(x * 7 + y * 3) % len(SMALL)]
        if m.deco_stamp(name, cx, cy):
            if _snapshot(m) != base or _reach_count(m) < before - 1:
                _unstamp(m, name, cx, cy)
    return m


def _reach_count(m):
    from validate import reachable
    mk = [o for o in m.objects if o["kind"] == "market"][0]
    return len(reachable(m, (mk["x"], mk["y"])))


def build(f):
    """맵을 만들고 이름표 겹침을 정리한다 (JSON·지도·생활 정보 모두 같은 결과를 쓰도록 여기서만 만든다)."""
    return clear_stall_trees(declutter(f()))


def label_report(m):
    """남은 겹침 (확인용)"""
    items = label_anchors(m) + [(o["x"], o["y"], o.get("name") or "이정표") for o in m.objects if o["kind"] in ("talker", "sign")]
    bad = []
    for i in range(len(items)):
        for j in range(i + 1, len(items)):
            if _clash(items[i], items[j]) and (items[i][0], items[i][1]) != (items[j][0], items[j][1]):
                bad.append((items[i][2][:8], items[i][:2], items[j][2][:8], items[j][:2]))
    return bad
