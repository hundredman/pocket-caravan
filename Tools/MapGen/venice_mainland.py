"""본토 서부(중앙 대륙 서쪽 1/4) 7씬: 키오자에서 배로 건너는 본토 나루터부터 동문 큰길까지 (설계 제안 초안).
기준: 2026-10-25 사용자 원본 계획도(지역·크기·연결) + 전체 지도 참고(배치·지형·색감). 크기·연결은 계획도를 따른다.

    연결(모두 양방향, 이 목록 밖의 지역 간 연결은 만들지 않는다)
      키오자 ↔ 본토 나루터 (배)          나루터 ↔ 포도밭 언덕 ↔ 산기슭 고갯길 ↔ 대상 숙소
      나루터 ↔ 강 하구 습지길 ↔ 돌다리 나루 ↔ 대상 숙소      나루터 ↔ 대상 숙소 (중앙 직행로)
      대상 숙소 ↔ 동문 큰길.  동문 큰길 동쪽은 중앙 권역 접점(안내만, 출구 없음)

그림은 베네치아와 같은 'vg_*' 임시 아트와 절차 바닥(vq:)을 쓴다. 지명·인물·대사는 창작 제안이다.
지형(물·바위·바닥)·소품·충돌·상호작용·출구는 모두 이 파일의 데이터로 만들고 MapJsonBuilder가 씬으로 바꾼다."""
import math
import tiles
from venice_tiles import VeniceMap

# mapId: (이름, 가로, 세로)  — 사용자 계획도 크기 그대로. 합계 25,000칸
SIZES = {
    "venice_c_landing": ("본토 나루터", 80, 56),
    "venice_c_vineyard": ("포도밭 언덕", 63, 56),
    "venice_c_marsh": ("강 하구 습지길", 63, 56),
    "venice_c_pass": ("산기슭 고갯길", 60, 44),
    "venice_c_bridge": ("돌다리 나루", 60, 44),
    "venice_c_post": ("대상 숙소", 64, 48),
    "venice_c_gate": ("동문 큰길", 72, 71),
}
# (맵 A, 가장자리, 시작 칸, 맵 B, 가장자리, 시작 칸, 폭). 양쪽에 같은 폭의 출구·도착 자리를 만든다.
LINKS = [
    ("venice_c_landing", "N", 52, "venice_c_vineyard", "S", 20, 3),
    ("venice_c_vineyard", "E", 16, "venice_c_pass", "W", 16, 3),
    ("venice_c_pass", "S", 40, "venice_c_post", "N", 30, 3),
    ("venice_c_landing", "S", 52, "venice_c_marsh", "N", 12, 3),
    ("venice_c_marsh", "E", 30, "venice_c_bridge", "W", 20, 3),
    ("venice_c_bridge", "N", 44, "venice_c_post", "S", 30, 3),
    ("venice_c_landing", "E", 27, "venice_c_post", "W", 22, 3),
    ("venice_c_post", "E", 22, "venice_c_gate", "W", 32, 3),
]
# 키오자 ↔ 나루터 배: 키오자 쪽은 venice.py(_chioggia_ferry)가 만든다. 나루터 쪽 서쪽 잔교 끝 3칸.
FERRY = ("venice_c_landing", "W", 27, 3)


def _base(mid, seed):
    name, w, h = SIZES[mid]
    m = VeniceMap(mid, name, w, h, seed=seed, zone="meadow")
    m.hours = 0
    m.obstacles = []
    m.fails = []
    for y in range(h):
        for x in range(w):
            m.reserved[y][x] = False
    m.set_style(0, 0, w - 1, h - 1, "orc")       # 올리브색 풀밭·흙길
    return m


def _links(m):
    for a, ea, sa, b, eb, sb, n in LINKS:
        if m.id == a:
            m.link(ea, sa, n, b, f"from_{a}", f"from_{b}")
            _edge_road(m, ea, sa, n)
        elif m.id == b:
            m.link(eb, sb, n, a, f"from_{b}", f"from_{a}")
            _edge_road(m, eb, sb, n)


def _edge_road(m, edge, start, n, depth=4):
    """출구 앞 몇 칸은 길·땅으로 확실히 비운다 (도착 자리가 물·건물 위가 되지 않게)."""
    for i in range(n):
        for d in range(depth):
            x, y = {"N": (start + i, d), "S": (start + i, m.h - 1 - d), "W": (d, start + i), "E": (m.w - 1 - d, start + i)}[edge]
            if (x, y) not in m.decks:
                m.water[y][x] = False
                if m.zone[y][x] == "rock":
                    m.zone[y][x] = "meadow"
            m.blocked[y][x] = False
            m.road[y][x] = max(m.road[y][x], 2)
            m.reserved[y][x] = True


# ---------------- 공용 도우미 ----------------
def put(m, name, x, y, high=None, free=True, over_water=False):
    """소품·건물 하나. free면 길·출구·다른 소품 자리(reserved)를 피한다. 못 놓으면 False (m.fails에 기록)."""
    s = tiles.STAMPS[name]
    w, h = s[3], s[4]
    if not (m.inside(x, y) and m.inside(x + w - 1, y + h - 1)):
        m.fails.append((name, x, y, "밖")); return False
    if free and any(m.reserved[yy][xx] for yy in range(y, y + h) for xx in range(x, x + w)):
        m.fails.append((name, x, y, "자리")); return False
    if over_water:
        return m.stamp(name, x, y, force=True, over_road=True)
    try:
        m.building(name, x, y, high=s[6] if high is None else high)
        return True
    except ValueError:
        m.fails.append((name, x, y, "겹침")); return False


def scatter(m, names, area, count, margin=1, avoid_road=2):
    """area 안 빈 풀밭에 나무·소품을 흩뿌린다 (길에서 avoid_road칸 떨어진 곳만)."""
    x0, y0, x1, y1 = area
    placed = 0
    for _ in range(count * 30):
        if placed >= count:
            break
        name = m.rng.choice(names)
        s = tiles.STAMPS[name]
        x, y = m.rng.randint(x0, max(x0, x1 - s[3] + 1)), m.rng.randint(y0, max(y0, y1 - s[4] + 1))
        cells = [(xx, yy) for yy in range(y - margin, y + s[4] + margin) for xx in range(x - margin, x + s[3] + margin)]
        if not all(m.inside(*c) and not m.reserved[c[1]][c[0]] and not m.water[c[1]][c[0]] and m.zone[c[1]][c[0]] != "rock" for c in cells):
            continue
        if any(m.inside(xx, yy) and m.road[yy][xx] for xx in range(x - avoid_road, x + s[3] + avoid_road)
               for yy in range(y - avoid_road, y + s[4] + avoid_road)):
            continue
        try:
            m.building(name, x, y, high=s[6])
            placed += 1
        except ValueError:
            pass


def deco(m, area, count, names=("vg_tuft", "vg_rocks", "vg_reeds")):
    """밟고 지나가는 바닥 장식 (풀·돌). 길 위에는 놓지 않는다."""
    x0, y0, x1, y1 = area
    for _ in range(count):
        m.deco_stamp(m.rng.choice(names), m.rng.randint(x0, x1), m.rng.randint(y0, y1))


def water_cells(m, cells):
    for x, y in cells:
        if m.inside(x, y):
            m.water[y][x] = True
            m.reserved[y][x] = True


def land_rect(m, x0, y0, x1, y1, zone="meadow"):
    for y in range(max(0, y0), min(m.h, y1 + 1)):
        for x in range(max(0, x0), min(m.w, x1 + 1)):
            m.water[y][x] = False
            m.zone[y][x] = zone


def rock(m, x, y):
    if m.inside(x, y) and not m.reserved[y][x] and not m.road[y][x]:
        m.zone[y][x] = "rock"
        m.blocked[y][x] = True
        m.reserved[y][x] = True


def wall(m, cells):
    """쌓은 석재(석축·성벽): 막히는 칸. 남쪽 면에는 돌 옆면이 그려져 단차처럼 보인다."""
    for x, y in cells:
        if m.inside(x, y) and not m.reserved[y][x] and not m.road[y][x]:
            m.zone[y][x] = "wall"
            m.blocked[y][x] = True
            m.reserved[y][x] = True


def grove(m, cx, cy, r, n, kinds=("vg_olive", "vg_n_orc_olive", "vg_cypress")):
    """나무 무리 (넓은 풀밭을 고르게 흩뿌리지 않고 몇 군데에 모은다). 나무 밑은 풀 무더기."""
    scatter(m, list(kinds), (cx - r, cy - r, cx + r, cy + r), n, margin=0)
    deco(m, (cx - r, cy - r, cx + r, cy + r), n * 2, ("vg_tuft", "vg_tuft", "vg_rocks"))


def mud(m, oid, x, y, w, h):
    """범람 진흙 구간: 공용 장애물 'mud'(통과 가능, 감속). 바닥도 진흙으로 칠한다."""
    m.obstacles.append(dict(id=oid, definitionId="mud", x=x, y=y, width=w, height=h, enabled=True, phaseOffset=0.0))
    for yy in range(y, y + h):
        for xx in range(x, x + w):
            if m.inside(xx, yy) and not m.water[yy][xx] and (xx, yy) not in m.decks and m.road[yy][xx]:
                m.zone[yy][xx] = "mud"


# ---------------- 피자 조각 (2026-10-26 팀 합의) ----------------
# 중앙 10만 칸을 X자 대각선으로 4등분한 서쪽 조각. 넓은 쪽(서) = 베네치아 바다, 뾰족한 쪽(동) = 대륙 중심.
# 씬은 네모라서 조각 경계(대각선) 밖은 빽빽한 숲으로 막는다(북유럽 north_center.wedge_forest와 같은 방식).
# 조각 좌표에서 각 씬의 왼쪽 위 위치. 씬 크기·연결은 사용자 계획도 그대로이고 위치만 조각 안에 맞춘다.
ORIGIN = {
    "venice_c_landing": (0, 110), "venice_c_vineyard": (30, 54), "venice_c_marsh": (40, 166),
    "venice_c_pass": (93, 54), "venice_c_bridge": (103, 176), "venice_c_post": (103, 115), "venice_c_gate": (167, 105),
}
CENTER_Y = 138                      # 조각의 가운데 줄 (나루터 → 대상 숙소 → 동문 큰길)


def edge_top(gx):
    """조각 위쪽 경계(북서 대각선). 동쪽(대륙 중심)으로 갈수록 가운데로 좁아진다."""
    return 13.3 + 0.438 * gx


def edge_bottom(gx):
    return 2 * CENTER_Y - edge_top(gx)


def wedge_forest(m):
    """조각 경계 밖(길·출구·건물·물 제외)을 숲으로 막는다."""
    ox, oy = ORIGIN[m.id]
    outside = set()
    for y in range(m.h):
        for x in range(m.w):
            gx, gy = ox + x, oy + y
            if (gy < edge_top(gx) or gy > edge_bottom(gx)) and not m.reserved[y][x] and not m.road[y][x] and not m.water[y][x]:
                outside.add((x, y))
    for x, y in outside:
        m.blocked[y][x] = True
        m.zone[y][x] = "meadow"
        m.lush.add((x, y))
    kinds = ["vg_olive", "vg_n_orc_olive", "vg_cypress", "vg_olive"]
    for y in range(0, m.h, 2):
        for x in range(0, m.w, 2):
            name = kinds[(x * 7 + y * 3) % len(kinds)]
            st = tiles.STAMPS[name]
            tx, ty = x + (y // 2) % 2, y - 1
            cells = [(tx + dx, ty + dy) for dy in range(st[4]) for dx in range(st[3])]
            if all(c in outside for c in cells):
                try:
                    m.building(name, tx, ty, high=st[6])
                except ValueError:
                    pass


def closed_port(m, edge, start, n, text):
    """통합 전까지 막아 두는 이웃 조각 연결 자리: 길 끝을 석축으로 막고 표지판."""
    for i in range(n):
        for d in range(2):
            x, y = {"N": (start + i, d), "S": (start + i, m.h - 1 - d), "W": (d, start + i), "E": (m.w - 1 - d, start + i)}[edge]
            m.zone[y][x] = "wall"
            m.blocked[y][x] = True
            m.road[y][x] = 0
            m.reserved[y][x] = True
    sx, sy = {"N": (start + n + 1, 3), "S": (start + n + 1, m.h - 4), "W": (3, start + n + 1), "E": (m.w - 4, start + n + 1)}[edge]
    m.sign(sx, sy, text)


def finish(m):
    wedge_forest(m)
    import venice_country           # 시골 풍경: 장터 둘레 집·사이프러스 가로수·농가 (2026-10-26)
    m.country = venice_country.dress(m)
    m.pave_streets(2)
    m.finish()
    import venice_people            # 일하는 사람·마을 사람·길 위 상인·성장 소품 (공용 장치)
    venice_people.populate(m)
    return m


# ---------------- ① 본토 나루터 ----------------
def venice_c_landing():
    m = _base("venice_c_landing", 401)
    w, h = m.w, m.h
    # 서쪽 바다와 굽은 해안선 (하역 마당 앞은 곧은 석축)
    for y in range(h):
        shore = 14 + round(2 * math.sin(y / 5.0) + math.sin(y / 2.3))
        if 14 <= y <= 42:
            shore = 16
        water_cells(m, [(x, y) for x in range(shore)])
    # 하역 마당 (화강암 바닥)
    land_rect(m, 16, 14, 38, 42, "street")
    m.set_style(16, 14, 38, 42, "prt")
    # 배 잔교(키오자 배가 닿는 곳) + 옆 잔교 둘
    m.bridge(0, 27, 17, 29)
    m.bridge(5, 19, 17, 20)
    m.bridge(5, 36, 17, 37)
    put(m, "vg_barge_loaded", 3, 24, over_water=True, free=False)
    put(m, "vg_w_cargo_boat", 7, 16, over_water=True, free=False)
    put(m, "vg_barge_empty", 6, 39, over_water=True, free=False)
    put(m, "vg_fishing_boat", 4, 32, over_water=True, free=False)
    m.link("W", 27, 3, "venice_chioggia", "from_venice_c_landing", "from_venice_chioggia")
    # 세 갈래 길: 북쪽 포도밭, 남쪽 습지, 동쪽 대상 숙소 직행로
    m.road_rect(38, 27, w - 1, 29, grade=3)                                         # 중앙 직행로 (넓고 곧다)
    m.road_path([(36, 25), (42, 21), (46, 13), (50, 6), (52, 0)], width=3, grade=2)  # 북쪽 갈래
    m.road_path([(36, 31), (42, 36), (46, 44), (50, 50), (52, 53)], width=3, grade=2)  # 남쪽 갈래
    _links(m)
    # 창고·하역 마당 (가운데 y26~31 하역 통로는 비운다)
    put(m, "vg_n_prt_warehouse", 18, 15)
    put(m, "vg_prt_warehouse", 27, 14)
    put(m, "vg_fondaco", 18, 38)
    put(m, "vg_w_office", 30, 38)
    for name, x, y in [("vg_n_prt_crane", 18, 24), ("vg_sacks_row", 21, 22), ("vg_prt_cargo", 24, 21), ("vg_n_prt_bales", 28, 21),
                       ("vg_crate", 31, 22), ("vg_barrel", 32, 22), ("vg_px_prt_cargo_cart", 34, 21), ("vg_n_prt_amphorae", 20, 33),
                       ("vg_prt_cargo", 23, 33), ("vg_barrel", 26, 33), ("vg_barrel", 27, 34), ("vg_sack_grain", 29, 33),
                       ("vg_luggage", 32, 34), ("vg_n_prt_bollard", 17, 26), ("vg_n_prt_bollard", 17, 30), ("vg_px_prt_trade_flag", 35, 15)]:
        put(m, name, x, y)
    m.add("market", 26, 36, town="venice_c_landing")       # 본토 나루터 장터 (하역 마당 남쪽)
    # 갈림길 이정표
    # 길 고르기 (2026-10-26): 직행로는 짧고 넓지만 밤 도적, 북쪽은 좁고 거친 고갯길(수레 느림), 남쪽은 진흙 습지(느리지만 안전)
    m.sign(40, 23, "북쪽 · 포도밭·고갯길 — 좁고 거친 길, 수레는 느림")
    m.sign(40, 33, "남쪽 · 습지·돌다리 — 진흙길이라 느리지만 도적 없음")
    m.sign(44, 25, "동쪽 · 대상 숙소 직행로 — 가장 빠름, 밤엔 도적 주의")
    m.bandit(64, 28, radius=7)                               # 직행로 동쪽 끝 숲 그늘
    m.event(47, 12)                                          # 북쪽 갈래: 쓰러진 행인·떠돌이 상인
    m.talker(35, 24, "나루터 짐꾼", "villager_dog_tall",
             "키오자 배는 서쪽 잔교에 닿는다오.",
             "북쪽은 포도밭, 남쪽은 습지, 동쪽 큰길은 대상 숙소로 바로 가지.")
    # 언덕의 집과 나무, 해안 갈대
    # 갈림길 언덕의 작은 마을 (집·텃밭·석축)
    for name, x, y in [("vg_house_ochre", 60, 8), ("vg_orc_cottage", 66, 9), ("vg_n_orc_vegbed", 60, 13), ("vg_n_orc_vegbed", 63, 13),
                       ("vg_orc_house", 62, 40), ("vg_house_brick", 70, 41), ("vg_n_orc_vegbed", 62, 45), ("vg_n_orc_vegbed", 65, 45),
                       ("vg_n_orc_hay", 69, 46), ("vg_well", 67, 34), ("vg_orc_cart", 56, 33)]:
        put(m, name, x, y)
    wall(m, [(x, 16) for x in range(58, 72)] + [(x, 48) for x in range(60, 74)])
    for cx, cy, r, n in ((48, 44, 5, 6), (72, 22, 6, 7), (58, 2, 4, 3), (22, 49, 4, 3), (74, 52, 4, 4), (40, 8, 4, 4)):
        grove(m, cx, cy, r, n)
    scatter(m, ["vg_olive", "vg_n_orc_olive", "vg_cypress", "vg_cypress"], (40, 0, w - 1, h - 1), 16)
    scatter(m, ["vg_cypress"], (17, 0, 38, 12), 4)
    scatter(m, ["vg_cypress", "vg_olive"], (17, 44, 38, h - 1), 5)
    deco(m, (14, 0, w - 1, h - 1), 60)
    deco(m, (12, 0, 20, h - 1), 25, ("vg_reeds", "vg_reeds_tall"))
    return finish(m)


# ---------------- ② 포도밭 언덕 ----------------
def _vines(m, x0, y0, x1, y1, step_y=3):
    """포도나무 열: 두 칸마다 한 그루, 세 줄마다 한 열. 열 사이는 흙 고랑."""
    for y in range(y0, y1 - 1, step_y):
        for x in range(x0, x1 + 1, 2):
            put(m, "vg_vine", x, y)
        for x in range(x0, x1 + 1):
            if m.inside(x, y + 2) and not m.reserved[y + 2][x]:
                m.zone[y + 2][x] = "dirt"


def _terrace(m, y, x0, x1, gaps):
    """낮은 석축 한 줄(단차 표현: 윗면 + 남쪽 돌 옆면). gaps의 칸부터 3칸은 통로로 비운다."""
    wall(m, [(x, y) for x in range(x0, x1 + 1) if not any(g <= x <= g + 2 for g in gaps)])


def venice_c_vineyard():
    m = _base("venice_c_vineyard", 402)
    w, h = m.w, m.h
    # 남쪽 나루터에서 올라와 양조장 마당을 지나 동쪽 고갯길로
    m.road_path([(20, h - 3), (20, 46), (24, 40), (28, 33)], width=3, grade=2)
    m.road_blob(36, 29, 9, 5, grade=2)                                   # 양조장 마당
    m.set_zone(27, 24, 45, 34, "dirt")
    m.road_path([(44, 26), (50, 21), (56, 17), (w - 1, 16)], width=3, grade=2)
    # 북서 포구 자리 (서쪽 조각 ↔ 북쪽 조각, 북유럽 눈숲 마을 서쪽과 3칸 폭으로 잇는다. 통합 전에는 막아 둔다)
    m.road_path([(29, 26), (22, 18), (14, 8), (12, 0)], width=3, grade=2)
    closed_port(m, "N", 12, 3, "북서 포구 · 북쪽 조각(눈숲 마을) 방면 — 통합 후 연결")
    _links(m)
    # 양조장: 농가 + 압착장 + 술통·수레
    put(m, "vg_n_orc_farmhouse", 29, 20)
    put(m, "vg_workshop2", 37, 19)
    put(m, "vg_orc_pergola", 42, 21)
    for name, x, y in [("vg_barrel", 30, 25), ("vg_barrel", 31, 25), ("vg_barrel", 32, 25), ("vg_jars", 41, 25),
                       ("vg_amphora", 42, 26), ("vg_px_orc_press", 28, 24), ("vg_orc_cart", 33, 33), ("vg_trellis", 44, 31),
                       ("vg_n_orc_hay", 27, 31), ("vg_barrow_sacks", 39, 33)]:
        put(m, name, x, y, free=False)
    # 계단식 포도밭 (석축 단차 사이에 포도 열). 길이 지나는 칸은 비운다.
    _vines(m, 2, 3, 16, 50)
    _vines(m, 48, 26, 60, 52)
    _vines(m, 24, 3, 44, 15)
    _vines(m, 26, 38, 40, 52)
    for y in (17, 35):
        _terrace(m, y, 1, 18, gaps=(10,))
    _terrace(m, 37, 24, 44, gaps=(34,))
    _terrace(m, 16, 22, 46, gaps=(30,))
    _terrace(m, 24, 47, 62, gaps=(53,))
    scatter(m, ["vg_cypress", "vg_olive", "vg_n_orc_olive"], (0, 0, w - 1, h - 1), 18)
    grove(m, 54, 8, 6, 6)
    grove(m, 8, 52, 4, 3)
    m.sign(23, 47, "북동쪽 · 양조장, 산기슭 고갯길")
    m.sign(56, 20, "동쪽 · 산기슭 고갯길")
    m.add("market", 36, 27, town="venice_c_vineyard")      # 포도밭 양조장 장터 (마당 가운데)
    m.talker(35, 31, "양조장 일꾼", "villager_bear_tall",
             "올해 포도는 달다네. 통은 수레로 나루터까지 내려가지.",
             "동쪽 고갯길은 좁지만 수레도 지나간다오.")
    deco(m, (0, 0, w - 1, h - 1), 50)
    return finish(m)


# ---------------- ③ 강 하구 습지길 ----------------
def venice_c_marsh():
    m = _base("venice_c_marsh", 403)
    w, h = m.w, m.h
    water_cells(m, [(x, y) for y in range(h) for x in range(w)])
    # 마른 땅(징검 섬)과 목재 데크. 데크는 섬과 섬에 한 칸씩 걸쳐 끊기지 않게 잇는다.
    islands = [(7, 0, 19, 8), (7, 18, 20, 27), (24, 15, 39, 24), (24, 34, 38, 44), (45, 24, w - 1, 42)]
    for x0, y0, x1, y1 in islands:
        land_rect(m, x0, y0, x1, y1, "meadow")
    # 섬 가장자리를 들쭉날쭉하게 (모서리 몇 칸을 물로)
    for x0, y0, x1, y1 in islands:
        for (cx, cy) in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
            if 0 < cx < w - 1 and 0 < cy < h - 1:
                water_cells(m, [(cx, cy)])
                m.reserved[cy][cx] = False
    m.bridge(12, 8, 14, 18)        # 북쪽 섬 → 둘째 섬 (세로 데크)
    m.bridge(20, 20, 24, 22)       # 둘째 섬 → 가운데 섬
    m.bridge(30, 24, 32, 34)       # 가운데 섬 → 남쪽 섬
    m.bridge(38, 38, 45, 40)       # 남쪽 섬 → 동쪽 물가
    # 섬 위 흙길
    m.road_path([(12, 0), (12, 7)], width=3, grade=2)
    m.road_path([(12, 19), (12, 20), (19, 20)], width=3, grade=2)
    m.road_path([(25, 20), (30, 20), (30, 23)], width=3, grade=2)
    m.road_path([(30, 35), (30, 38), (37, 38)], width=3, grade=2)
    m.road_path([(46, 38), (52, 34), (56, 30), (w - 1, 30)], width=3, grade=2)
    _links(m)
    # 2026-10-30 진흙 장애물(공용 ObstacleField 창)은 재미가 없다는 의견으로 뺐다. mud()는 남겨 둔다.
    # 갈대·나무·작은 소품
    for x0, y0, x1, y1 in islands:
        deco(m, (x0, y0, x1, y1), 14, ("vg_reeds", "vg_reeds_tall", "vg_tuft"))
        scatter(m, ["vg_olive", "vg_cypress", "vg_n_orc_olive"], (x0, y0, x1, y1), 2, margin=0, avoid_road=1)
    put(m, "vg_poles", 21, 18, over_water=True, free=False)
    put(m, "vg_fsh_dinghy", 40, 31, over_water=True, free=False)
    put(m, "vg_net_rack", 34, 41)
    m.sign(16, 4, "남쪽 데크 · 습지 건너 돌다리 나루")
    m.event(12, 22)                                          # 둘째 섬 길: 길 잃은 행인
    m.talker(51, 37, "갈대 베는 사람", "villager_rabbit_tall",
             "물이 불면 섬 흙길이 진창이 된다오.",
             "데크는 미끄럽지 않으니 걱정 말게.")
    _reed_islets(m, 18)
    return finish(m)


def _reed_islets(m, count):
    """물 위 작은 갈대 모래톱 (걸어서 닿지 않음)"""
    for _ in range(count):
        for _try in range(30):
            x, y = m.rng.randrange(1, m.w - 4), m.rng.randrange(1, m.h - 3)
            if all(m.inside(xx, yy) and m.water[yy][xx] and (xx, yy) not in m.decks and not m.obj[yy][xx]
                   for yy in range(y - 2, y + 4) for xx in range(x - 2, x + 5)):
                for yy in range(y, y + 2):
                    for xx in range(x, x + 3):
                        m.water[yy][xx] = False; m.zone[yy][xx] = "sand"
                m.deco_stamp("vg_reeds_tall", x + 1, y)
                m.deco_stamp("vg_reeds", x, y + 1)
                break


# ---------------- ④ 산기슭 고갯길 ----------------
def venice_c_pass():
    m = _base("venice_c_pass", 404)
    w, h = m.w, m.h
    path = [(0, 17), (10, 17), (16, 12), (24, 12), (28, 20), (22, 27), (30, 33), (40, 30), (41, h - 1)]
    m.road_path(path, width=2, grade=1)
    _links(m)
    # 길에서 떨어진 곳에 낮은 석회암 능선(사선 띠). 길 양옆 2칸은 풀밭으로 남긴다.
    road = {(x, y) for y in range(h) for x in range(w) if m.road[y][x]}
    near = set()
    for (x, y) in road:
        for dy in range(-3, 4):
            for dx in range(-3, 4):
                near.add((x + dx, y + dy))
    for y in range(h):
        for x in range(w):
            if (x, y) in near:
                continue
            band = math.sin(x * 0.22 + y * 0.35) + 0.6 * math.sin(x * 0.07 - y * 0.18) + m.rng.uniform(-0.25, 0.25)
            if band > 0.55:
                rock(m, x, y)
    rock_tile = tiles.K(*tiles.STAMPS["vg_rocks"][:3])
    for _ in range(140):          # 바위 위 돌무더기 질감 (밟을 수 없는 칸이라 통로와 무관)
        x, y = m.rng.randrange(w), m.rng.randrange(h)
        if m.zone[y][x] == "rock" and m.deco[y][x] is None and m.rng.random() < .5:
            m.deco[y][x] = rock_tile
    for cx, cy, r, n in ((48, 8, 6, 6), (8, 34, 5, 5), (52, 36, 5, 4), (20, 3, 4, 3)):
        grove(m, cx, cy, r, n)
    # 능선에서 떨어져 나온 바위·관목, 길가 쉼터
    scatter(m, ["vg_olive", "vg_cypress", "vg_n_orc_olive"], (0, 0, w - 1, h - 1), 20, margin=0)
    deco(m, (0, 0, w - 1, h - 1), 70, ("vg_rocks", "vg_rocks", "vg_tuft"))
    put(m, "vg_px_cap_stone_bench", 18, 16)
    m.sign(12, 20, "남동쪽 · 대상 숙소")
    m.event(30, 32)                                          # 고갯마루 떠돌이 상인
    m.talker(21, 16, "양치기", "villager_fox_tall",
             "능선은 낮아도 바위가 날카롭지. 길로만 다니게.",
             "고개를 넘으면 대상 숙소 마당이 보일 거야.")
    return finish(m)


# ---------------- ⑤ 돌다리 나루 ----------------
def venice_c_bridge():
    m = _base("venice_c_bridge", 405)
    w, h = m.w, m.h
    # 남북으로 흐르는 강 (폭 7, 조금 굽는다)
    for y in range(h):
        c = 29 + round(2 * math.sin(y / 6.0))
        water_cells(m, [(x, y) for x in range(c - 3, c + 4)])
    # 동서로 건너는 돌다리 상판 (석재 바닥. 물 쪽 가장자리는 석축 난간)
    for x in range(22, 37):
        for y in range(19, 24):
            if m.water[y][x] or 24 <= x <= 34:
                m.water[y][x] = False
                m.zone[y][x] = "street"
                m.reserved[y][x] = True
    m.set_style(22, 19, 36, 23, "cap")
    m.road_rect(0, 20, 37, 22, grade=3)
    m.road_path([(37, 20), (44, 20), (44, 10), (44, 0)], width=3, grade=2)
    _links(m)
    # 다리 끝 기둥, 강가 작은 선착장과 배, 갈대·바위
    for x, y in ((23, 18), (35, 18), (23, 24), (35, 24)):
        put(m, "vg_n_prt_bollard", x, y)
    c = 29 + round(2 * math.sin(32 / 6.0))
    m.bridge(c + 1, 31, c + 4, 32)
    put(m, "vg_fsh_dinghy", c - 2, 34, over_water=True, free=False)
    for cx, cy, r, n in ((10, 8, 6, 7), (52, 34, 6, 7), (10, 34, 5, 5), (54, 8, 4, 3)):
        grove(m, cx, cy, r, n)
    scatter(m, ["vg_olive", "vg_n_orc_olive", "vg_cypress"], (0, 0, w - 1, h - 1), 10)
    put(m, "vg_orc_house", 48, 25)
    put(m, "vg_n_orc_vegbed", 49, 29)
    deco(m, (0, 0, w - 1, h - 1), 40)
    deco(m, (20, 0, 38, h - 1), 30, ("vg_reeds", "vg_reeds_tall", "vg_rocks"))
    m.sign(18, 18, "동쪽 돌다리 건너 · 대상 숙소")
    m.talker(39, 24, "다리지기", "villager_bear_tall",
             "이 다리는 마차 둘이 비켜 갈 만큼 넓다네.",
             "북쪽 길로 가면 대상 숙소 남문이 나오지.")
    return finish(m)


# ---------------- ⑥ 대상 숙소 ----------------
def venice_c_post():
    m = _base("venice_c_post", 406)
    w, h = m.w, m.h
    # 열린 가운데 마당 (흙). 네 방향 길이 마당에서 만난다.
    m.set_zone(17, 12, 46, 35, "dirt")
    m.road_rect(17, 12, 46, 35, grade=2)
    m.set_zone(27, 17, 36, 27, "court")       # 우물 둘레 돌바닥
    m.set_style(27, 17, 36, 27, "cap")
    m.road_rect(0, 22, 17, 24, grade=3)        # 서쪽 직행로
    m.road_rect(46, 22, w - 1, 24, grade=3)    # 동쪽 큰길
    m.road_rect(30, 0, 32, 12, grade=2)        # 북쪽 고갯길
    m.road_rect(30, 35, 32, h - 1, grade=2)    # 남쪽 돌다리길
    _links(m)
    # 북쪽: 여관·객실 회랑·창고 (북문 x29~33은 비운다). 동쪽: 마구간
    put(m, "vg_house_ochre", 18, 8, free=False)
    put(m, "vg_loggia", 23, 7, free=False)
    put(m, "vg_fondaco", 34, 8, free=False)
    put(m, "vg_inn", 40, 7, free=False)
    put(m, "vg_n_orc_shed", 42, 27, free=False)
    for name, x, y in [("vg_n_orc_hay", 41, 31), ("vg_n_orc_hay", 46, 30), ("vg_orc_cart", 37, 32),
                       ("vg_stall_red", 19, 26), ("vg_stall_blue", 22, 28), ("vg_stall_spice", 19, 30), ("vg_awning_table", 24, 31),
                       ("vg_cap_well", 31, 18), ("vg_stall_red", 38, 30), ("vg_n_prt_bales", 25, 33), ("vg_crate", 27, 33),
                       ("vg_sacks_row", 34, 33), ("vg_n_prt_spice", 20, 33), ("vg_px_cap_bread_stall", 19, 21), ("vg_barrel", 44, 13), ("vg_barrel", 45, 13), ("vg_jars", 18, 13),
                       ("vg_px_cap_stone_bench", 26, 15), ("vg_cap_board", 38, 13), ("vg_banner", 22, 12), ("vg_banner", 39, 12)]:
        put(m, name, x, y, free=False)
    m.add("market", 22, 25, town="venice_c_caravanserai")
    m.board(40, 15, "venice_c_caravanserai")
    m.talker(33, 18, "숙소 주인", "villager_tanuki_tall",
             "먼 길 오셨소. 쉬어 가시오. 장터에서 시세도 보고.",
             "서쪽은 나루터, 북쪽은 고갯길, 남쪽은 돌다리, 동쪽은 큰길이오.")
    # 마당 둘레 낮은 담 (길 자리 비움)과 바깥 나무
    for x in range(17, 46, 2):
        if not (28 <= x <= 33):
            put(m, "vg_wall_low", x, 36)
    for y in range(13, 35):
        for x in (15, 47):
            if not (21 <= y <= 25) and y % 3 == 0:
                put(m, "vg_cypress", x, y - 2)
    for cx, cy, r, n in ((7, 8, 5, 6), (56, 8, 5, 6), (8, 38, 5, 6), (56, 38, 5, 6)):
        grove(m, cx, cy, r, n)
    scatter(m, ["vg_olive", "vg_n_orc_olive", "vg_cypress"], (0, 0, w - 1, h - 1), 10)
    deco(m, (0, 0, w - 1, h - 1), 45)
    m.sign(28, 39, "남쪽 · 돌다리 나루")
    m.bandit(56, 23, radius=6)                               # 동쪽 큰길 나가는 길목 (밤)
    m.sign(34, 4, "북쪽 · 산기슭 고갯길")
    return finish(m)


_WALL = tuple(tiles.STAMPS["vh_wall_v"][1:3])      # 성벽 한 조각의 시트 칸


# ---------------- ⑦ 동문 큰길 ----------------
def venice_c_gate():
    m = _base("venice_c_gate", 407)
    w, h = m.w, m.h
    # 넓은 동서 대상로 (크림색 석재). 서쪽 숙소 길에서 이어져 관문을 지나 동쪽 끝까지
    m.set_zone(0, 30, w - 1, 36, "street")
    m.set_style(0, 30, w - 1, 36, "cap")
    m.road_rect(0, 30, w - 1, 36, grade=3)
    _links(m)
    # 성벽(x44~45 세로)과 관문 탑 둘. 길(y30~36)은 탑 사이로 지난다.
    # 2026-10-26: 제비꼬리 성가퀴 벽돌 성벽·망루 그림(vh_wall_v·vh_tower, 제안). 막는 칸은 wall()이 그대로 정한다.
    wall(m, [(x, y) for x in (44, 45) for y in list(range(3, 29)) + list(range(37, h - 3))])
    put(m, "vh_tower", 43, 23, high=1, free=False)
    put(m, "vh_tower", 43, 37, high=1, free=False)
    for y in list(range(3, 23)) + list(range(43, h - 3)):
        sx, sy = _WALL
        m.obj[y][44], m.obj[y][45] = tiles.K("vhse", sx, sy), tiles.K("vhse", sx + 1, sy)
    # 검문소와 경비
    put(m, "vg_w_office", 36, 25, free=False)
    put(m, "vg_n_prt_bollard", 40, 29, free=False)
    put(m, "vg_n_prt_bollard", 40, 37, free=False)
    m.talker(39, 28, "관문 경비", "villager_dog_tall",
             "동문 검문소요. 짐은 열어 보지 않으니 지나가시오.",
             "이 길 동쪽이 중앙 권역이라 하오. 아직 길을 닦는 중이지.")
    # 동쪽 끝: 중앙 권역 접점 (연결 전까지 안내만. 존재하지 않는 씬으로 보내지 않는다)
    m.sign(66, 28, "동쪽 · 중앙 권역 (길 닦는 중, 통합 후 연결)")
    m.talker(68, 38, "길 닦는 일꾼", "villager_bear_tall",
             "이 너머는 아직 돌을 깔고 있다오.",
             "중앙 권역 장터가 열리면 이 길로 대상이 오갈 거요.")
    for x, y in ((70, 29), (70, 37)):
        put(m, "vg_n_prt_bollard", x, y, free=False)
    # 길가 사이프러스 줄, 수레, 과수원·밭·농가
    for x in range(3, w - 3, 5):
        if not (36 <= x <= 50):
            put(m, "vg_cypress", x, 26)
            put(m, "vg_cypress", x, 38)
    for name, x, y in [("vg_orc_cart", 18, 39), ("vg_w_cart", 56, 39), ("vg_barrow_sacks", 26, 26),
                       ("vg_n_orc_farmhouse", 10, 10), ("vg_house_ochre", 56, 50), ("vg_salt_hut", 22, 52),
                       ("vg_n_orc_vegbed", 12, 16), ("vg_n_orc_vegbed", 15, 16), ("vg_n_orc_vegbed", 58, 55), ("vg_n_orc_vegbed", 61, 55)]:
        put(m, name, x, y)
    for cx, cy, r, n in ((24, 10, 7, 9), (60, 12, 7, 9), (30, 56, 7, 9), (62, 62, 5, 5), (8, 60, 5, 5)):
        grove(m, cx, cy, r, n)
    scatter(m, ["vg_olive", "vg_n_orc_olive", "vg_olive", "vg_cypress"], (0, 0, w - 1, 24), 10)
    scatter(m, ["vg_olive", "vg_n_orc_olive", "vg_olive", "vg_cypress"], (0, 42, w - 1, h - 1), 10)
    deco(m, (0, 0, w - 1, h - 1), 80)
    return finish(m)


GENERATORS = [venice_c_landing, venice_c_vineyard, venice_c_marsh, venice_c_pass, venice_c_bridge, venice_c_post, venice_c_gate]
