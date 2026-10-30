"""중앙 대륙 북쪽 1/4 (기우 담당, 2만5천 칸, 길 포함). 10/24 처음, 10/26 피자 구성으로 다시 짬.
팀 합의: 중앙 10만 칸을 X자 대각선으로 4등분, 각자 자기 문명을 마주 보는 쪽(피자 한 조각)을 만든다.
고려 동쪽 지도와 같은 방식: 마을 4곳(장터) + 마을 사이 길 5곳. 넓은 쪽(위)은 북유럽 국경 고개, 뾰족한 쪽(아래)은 대륙 중심.
위에서 아래로 내려갈수록 눈이 녹고 북유럽 색이 옅어진다. 자세한 배치는 아래 "10/26 피자 구성" 주석.
아직 안 이어진 곳(화요일 합치기 전): 북서 포구(베네치아)·북동 포구(고려)·대륙 중심 → 통나무 바리케이드와 표지판으로 막아 둠.
맵 id는 north_c_ (북유럽 연출 스크립트가 그대로 적용됨).
등록은 북유럽(north.json)과 따로: World/Regions/center_north.json, 맵 JSON은 Maps/Center, 씬은 Scenes/Center."""
from tiles import K
from north_tiles import ThawMap
import tiles
tiles.STAMPS.setdefault("rocksmall", ("nature", 18, 9, 1, 1, 0, 0, None))



def closed_exit(m, edge, start, text, road_from=None, length=4):
    """나중에 이을 출구 자리: 길 끝을 맵 가장자리까지 내고 통나무로 막고 표지판을 세운다."""
    if road_from:
        if edge in "EW":
            ex = m.w - 1 if edge == "E" else 0
            m.road_path([road_from, (ex, start + 1)], width=2)
        else:
            ey = m.h - 1 if edge == "S" else 0
            m.road_path([road_from, (start + 1, ey)], width=2)
    if edge in "EW":
        x0 = m.w - 2 if edge == "E" else 0
        m.reserve(x0 - (2 if edge == "E" else 0), start - 1, x0 + 1 + (2 if edge == "W" else 0), start + length)
        for y in range(start, start + length):          # 통나무를 가로로 쌓은 바리케이드
            m.stamp("logs" if y % 2 else "logs_birch", x0, y, force=True, over_road=True)
            m.blocked[y][x0] = m.blocked[y][x0 + 1] = True
        sx = x0 - 2 if edge == "E" else x0 + 3
        m.sign(sx, start - 1, text)
    else:
        y0 = m.h - 2 if edge == "S" else 0
        m.reserve(start - 1, y0 - (2 if edge == "S" else 0), start + length, y0 + 1 + (2 if edge == "N" else 0))
        for x in range(start, start + length, 2):       # 통나무 두 줄 바리케이드
            for y in (y0, y0 + 1):
                m.stamp("logs" if (x // 2 + y) % 2 else "logs_birch", x, y, force=True, over_road=True)
                m.blocked[y][x] = m.blocked[y][x + 1] = True
        m.sign(start + length + 1, y0 - 2 if edge == "S" else y0 + 3, text)



# ---------- 10/24 발견거리: 빈 눈밭을 10칸마다 작은 이야기로 채운다 (칸 수는 그대로) ----------
TRAVELLERS = [
    ("길 잃은 짐꾼", "north_goat_tall", "썰매 끈이 끊어져서 짐을 반쯤 버렸어. 누가 주워 가면 좋겠는데.", "눈밭 지름길은 걸어서는 빨라도 수레는 바퀴가 빠져."),
    ("떠돌이 음유시인", "north_p_bard_tall", "얼음 밑 물고기는 겨울에 더 달다네. 얼음을 깨는 손맛이 일품이지.", "룬돌 일곱 개를 다 찾으면 노래 한 곡을 지어 주지."),
    ("순록 치는 소녀", "north_p_girl_tall", "순록들이 눈 녹은 자리의 이끼를 찾아 내려왔어.", "썰매 정류장에 가면 썰매를 빌릴 수 있대!"),
    ("남쪽 행상", "north_p_trader_tall", "중앙 도시에선 북쪽 모피가 금값이라더군. 초소에서 소금을 사 가면 사냥꾼들이 반겨.", "큰길엔 눈늑대가 나온다니 짐이 많으면 개울 큰길로 돌아가."),
    ("얼음 낚시꾼", "north_bear_sailor_tall", "얼음 구멍 옆 바구니 보이지? 거기가 대구가 잘 무는 자리야.", "얼음은 세 번은 내리쳐야 깨져."),
    ("나무꾼", "north_moose_tall", "자작나무는 불이 오래가. 상인들 야영지에 팔면 쏠쏠하지.",),
    ("길 지키는 노인", "north_p_oldman_tall", "옛날엔 이 돌무더기가 길잡이였어. 눈보라가 치면 돌만 보고 걸었지.",),
    ("사냥꾼", "north_wolf_tall", "눈 위 발자국을 보면 여우가 어디로 갔는지 다 보여.",),
]


def discover(m, kind, x, y, trail=None, talk=None):
    """빈 땅에 놓는 작은 장면. trail=(x, y)면 그 길에서 한 칸 오솔길을 낸다. talk=번호면 길손 한 명."""
    if trail:
        m.road_path([trail, (x, y + 2)], width=1)
    d = m.deco_stamp
    if kind == "cairn":        # 돌무더기 길잡이
        d("rock", x, y); d("runestone_b", x + 2, y - 1); d("rocksmall", x - 1, y + 1); d("rocksmall", x + 3, y + 1)
    elif kind == "hut":        # 10/27 외딴 흙집 한 채 (개울 나루: 돌무더기·룬돌 대신)
        # 오솔길(x열)을 막지 않게 한 칸 오른쪽에 짓고, 문 앞 길을 오솔길 끝에 잇는다
        if m.stamp("h_turf", x + 1, y - 1, force=True):
            m.door_path(x + 1, y - 1, 3, 2, y + 2, door_dx=1); m.road_path([(x, y + 2), (x + 2, y + 2)], width=1)
    elif kind == "cargo":      # 버려진 짐 (돌발 사건 자리)
        d("sled", x, y); d("crate", x + 2, y); d("sack", x + 2, y + 1); d("barrel_s", x - 1, y); m.event(x + 1, y + 2)
    elif kind == "icehole":    # 얼음 낚시 구멍 (낚시 미니게임 자리)
        m.water_blob(x, y, 2.6, 1.7)
        d("fishbasket", x + 3, y); d("bucket", x + 3, y + 1); d("stump", x - 4, y)
        m.fishing.append((x, y))
    elif kind == "camp":       # 길손 야영지
        d("fire", x, y); d("bedroll", x + 2, y)   # 10/27 긴 의자는 빼고 모닥불 옆 통나무 의자(런타임)로
    elif kind == "herd":       # 이끼 찾아 내려온 순록 무리
        d("haybale_s", x, y); m.critters("reindeer", (x - 5, y - 2, x + 5, y + 3), 3)
    elif kind == "hunt":       # 사냥꾼 덫 자리
        d("deadtree", x, y); d("pelt", x + 2, y + 1); d("stump", x - 1, y + 1); d("firewood", x + 2, y)
    elif kind == "stones":     # 작은 선돌 셋
        d("runestone_b", x, y); d("runestone", x + 2, y + 1); d("rock", x + 4, y)
    elif kind == "crystal":    # 눈 밖으로 드러난 수정
        d("rock", x, y); d("crys_b", x + 2, y + 1); d("crys_o", x - 1, y + 1); d("gem_b", x + 2, y)
    elif kind == "shrine":     # 길손 제단 (나무 기둥과 등불, 공물)
        d("totem", x, y); d("lantern", x + 1, y); d("crate_berry", x + 2, y + 1); d("basket", x - 1, y + 1)
    elif kind == "woodpile":   # 장작 쌓는 곳
        d("logs", x, y); d("logs_birch", x, y + 1); d("stumpbig", x + 2, y); d("firewood", x - 1, y)
    elif kind == "lookout":    # 눈 위 망루 표지
        d("snowpost", x, y); d("rocksmall", x - 1, y + 1)   # 10/27 깃발 뺌
    if talk is not None:
        name, who, *lines = TRAVELLERS[talk % len(TRAVELLERS)]
        m.talker(x + 1, y + 3, name, who, *lines)



def snow_trail(m, pts, every=8):
    """길을 깔지 않은 눈밭 지름길: 나무가 안 자라게 비워 두고 표지 기둥만 세운다.
    걸으면 가장 짧지만 노면이 없어 수레는 느리다 (팀 규칙: 등짐은 짧은 길, 수레는 좋은 길)."""
    cells = []
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(n + 1):
            cells.append((round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n)))
    for (x, y) in cells:
        m.reserve(max(0, x - 1), max(0, y - 1), min(m.w - 1, x + 1), min(m.h - 1, y + 1))
        if getattr(m, "base", "") == "grass":          # 풀밭 맵에선 지름길을 따라 눈을 깔아 눈밭 길로 보이게
            m.patch_area(max(0, x - 2), max(0, y - 2), min(m.w - 1, x + 2), min(m.h - 1, y + 2))
    for i in range(every // 2, len(cells), every):
        x, y = cells[i]
        if 2 < x + 2 < m.w - 3 and 2 < y < m.h - 3:
            m.deco_stamp("snowpost", x + 2, y)
    return cells


_NATURE = None


def auto_fill(m, kinds, talks=(), margin=6, radius=9, max_n=14):
    """맵 안쪽에서 볼거리와 가장 먼 빈 눈밭부터 발견거리를 하나씩 놓는다 (모든 걷는 칸이 볼거리에서 radius칸 안쪽이 될 때까지).
    맵 가장자리 margin칸은 나중에 숲이 되므로 뺀다. 가까운 길(14칸 안)에서 오솔길을 낸다."""
    global _NATURE
    if _NATURE is None:
        _NATURE = {k for k, v in tiles.STAMPS.items() if v[0] == "nature" and not k.startswith(("crys", "gem", "pit"))} | {"logs", "logs_birch", "stump", "stumpbig"}
    talks = list(talks)
    for i in range(max_n):
        pois = [(x, y) for (n, x, y) in m.stamps if n not in _NATURE]
        pois += [(o["x"], o["y"]) for o in m.objects if o["kind"] in ("talker", "npc", "market", "board", "sign", "event", "exit")]
        pois += [(x, y) for y in range(m.h) for x in range(m.w) if m.water[y][x] and (x + y) % 5 == 0]
        best, bd = None, radius
        for y in range(margin, m.h - margin - 3):
            for x in range(margin + 1, m.w - margin - 5):
                if any(m.reserved[y + b][x + a] or m.road[y + b][x + a] or m.water[y + b][x + a] or m.blocked[y + b][x + a]
                       for a in range(-1, 5) for b in range(-1, 4)):
                    continue
                dmin = min((abs(x - a) + abs(y - b) for a, b in pois), default=99)
                if dmin > bd:
                    best, bd = (x, y), dmin
        if not best:
            break
        x, y = best
        roads = [(a, b) for b in range(max(0, y - 14), min(m.h, y + 15)) for a in range(max(0, x - 14), min(m.w, x + 15)) if m.road[b][a] > 0]
        trail = min(roads, key=lambda r: abs(r[0] - x) + abs(r[1] - y)) if roads else None
        discover(m, kinds[i % len(kinds)], x, y, trail=trail, talk=talks.pop(0) if talks else None)
        m.reserve(x - 1, y - 1, x + 4, y + 3)


# ======================================================================
# 10/26 피자 구성 (고려 동쪽 지도와 같은 방식): 마을 4곳 + 마을 사이 길 5곳 = 25,000칸
# 넓은 쪽(위) = 북유럽 국경 고개, 뾰족한 쪽(아래) = 대륙 중심. 맵은 네모라서, 피자 조각의 대각선 경계 밖은
# 빽빽한 숲으로 막는다 (wedge_forest). 걸어 다니는 땅이 아래로 갈수록 좁아지는 삼각형이 된다.
# 전체 위치 (북문 마을 왼쪽 위 = 0,0)
#   국경 고개(북유럽)  x14..66  y-50..0          비르카 x66..138 y-50..0
#   ① 눈숲 고갯길 62x46  x-62..0   y0..46     북문 마을 60x46  x0..60  y0..46     ② 개울 다리길 62x46  x60..122 y0..46
#   눈숲 마을 52x40      x-40..12  y46..86    ③ 룬돌 큰길 36x96 x12..48 y46..142  개울 나루 마을 52x40 x48..100 y46..86
#   ④ 서남 솔숲길 40x77  x-40..0   y86..163                                       ⑤ 동남 물가길 40x77  x60..100 y86..163
#                                  북쪽 길목 마을 60x46  x0..60  y142..188  (↓ 대륙 중심)
#   2760 + 2852×2 + 2080×2 + 3456 + 3080×2 + 2760 = 25,000
#   대각선 경계: 왼쪽 x = -62 + 0.32·y, 오른쪽 x = 122 - 0.32·y (x=30을 축으로 좌우 대칭)
# 장터 4곳: 북문 마을(north_post) · 눈숲 마을(north_cwood) · 개울 나루 마을(north_ccreek) · 북쪽 길목 마을(north_cross)
# 이웃 구역과 잇는 자리 (화요일): 눈숲 마을 서쪽 → 북서 포구(베네치아), 개울 나루 마을 동쪽 → 북동 포구(고려), 길목 마을 남쪽 → 대륙 중심
#   (두 포구 출구는 대각선 경계 바로 안쪽에 있다)
# ======================================================================
import math
from north_tiles import DENSE_KINDS


def wedge_forest(m, ox, oy, theme="pine"):
    """피자 조각 대각선 경계 밖 칸을 빽빽한 숲으로 막는다. ox, oy = 이 맵 왼쪽 위의 전체 좌표 (위 표)."""
    mask = set()
    for y in range(m.h):
        gy = oy + y
        xl = -62 + 0.32 * gy + 1.5 * math.sin(gy * 0.21) + math.sin(gy * 0.07 + 1)
        xr = 122 - 0.32 * gy - 1.5 * math.sin(gy * 0.19 + 2) - math.sin(gy * 0.05)
        for x in range(m.w):
            gx = ox + x
            if (gx < xl or gx > xr) and not m.reserved[y][x] and m.road[y][x] == 0 and not m.water[y][x] \
                    and m.obj[y][x] is None and m.high[y][x] is None:
                mask.add((x, y))
    if mask:
        m._plant_dense(mask, DENSE_KINDS[theme])
    return len(mask)


def gate_town():
    """북문 마을: 국경 고개를 내려오면 처음 만나는 중앙 대륙 마을. 북쪽 물건의 수출 장터 (모피·호박을 가장 비싸게 삼, 남쪽 소금 특산).
    여관(롱하우스)·교역소·네 문명 좌판·순록 마구간. 국경 표석이 마을 북쪽 입구에 있다."""
    m = ThawMap("north_c_gatetown", "북문 마을", 60, 46, base="snow", cover=(0.3, 0.3), seed=201)
    m.hours = 1
    m.link("N", 26, 4, "north_pass", "S", "N")
    m.link("W", 20, 4, "north_c_road_nw", "E", "W")
    m.link("E", 20, 4, "north_c_road_ne", "W", "E")
    m.link("S", 28, 4, "north_c_runeway", "N", "S")
    m.road_path([(27, 0), (28, 8), (30, 16)], width=2, grade=2)
    m.road_blob(30, 21.5, 10, 5, grade=1)                       # 장터 광장
    m.road_path([(0, 21), (10, 21), (20, 22)], width=2, grade=2)
    m.road_path([(40, 22), (50, 21), (59, 21)], width=2, grade=2)
    m.road_path([(30, 27), (29, 36), (29, 45)], width=2, grade=2)

    # 국경 표석: 여기서부터 중앙 대륙
    m.deco_stamp("runestone", 35, 4); m.deco_stamp("snowpost", 24, 4)
    m.sign(27, 13, "↑ 국경 고개 · 북유럽\n↓ 룬돌 큰길 · 북쪽 길목 마을\n← 눈숲 마을   → 개울 나루 마을")
    # 여관(롱하우스)과 교역소, 마을 장식
    m.stamp("longhall", 8, 7, force=True); m.door_path(8, 7, 4, 7, 20, door_dx=1)
    m.stamp("h_dark", 41, 8, force=True); m.door_path(41, 8, 4, 4, 18, door_dx=1)
    m.stamp("h_turf", 18, 10, force=True); m.door_path(18, 10, 3, 2, 19, door_dx=1)
    m.deco_stamp("banner", 7, 14); m.deco_stamp("banner", 13, 14)
    # 장터: 네 문명 좌판 (북쪽 모피·호박, 남쪽 향신료, 동쪽 비단, 서쪽 유리)
    m.deco_stamp("stall", 23, 18); m.deco_stamp("stall", 34, 18)
    m.deco_stamp("stall", 23, 24); m.deco_stamp("stall", 34, 24)
    m.deco_stamp("pelt", 27, 19); m.deco_stamp("crate_goods", 32, 20); m.deco_stamp("potion", 27, 25)
    m.deco_stamp("sack_big", 38, 20); m.deco_stamp("basket_apple", 22, 21); m.deco_stamp("jar_red", 32, 25)
    m.deco_stamp("lamp", 20, 17); m.deco_stamp("lamp", 40, 17); m.deco_stamp("lamp", 20, 26); m.deco_stamp("lamp", 40, 26)
    m.add("market", 30, 22, town="north_post")
    m.board(38, 16, "north_post")
    # 순록 마구간
    m.fence(42, 29, 54, 38, gap=(46, 29)); m.reserve(43, 30, 53, 37)
    m.stamp("h_shed", 50, 30, force=True)
    m.deco_stamp("haybale", 43, 35); m.deco_stamp("bucket", 47, 36)
    m.critters("reindeer", (44, 31, 49, 34), 2)
    m.road_path([(46, 28), (42, 24)], width=1)
    # 서쪽 상인 천막촌
    m.deco_stamp("tent", 6, 31); m.deco_stamp("tent", 12, 33); m.deco_stamp("fire", 10, 29)
    m.deco_stamp("bedroll", 16, 30); m.deco_stamp("box", 5, 35); m.deco_stamp("bigbarrel", 16, 35)
    m.road_path([(10, 28), (19, 25)], width=1)

    m.talker(31, 13, "북문 마을 촌장", "north_wolf_viking_tall", "어서 와, 북문 마을이야. 북쪽 물건이 중앙으로 나가는 첫 장터지. 모피랑 호박은 여기서 제일 비싸게 사 줘.",
             "대신 남쪽 소금은 여기서만 싸. 사냥꾼 마을에 가져가면 고기 절인다고 서로 달라고 할걸.")
    m.talker(32, 5, "국경 표석 지기", "north_p_shield_tall", "이 돌부터 남쪽은 중앙 대륙이야. 눈이 녹는 게 보이지?",
             "북쪽 상인들은 여기서 모피 짐을 한 번 더 단단히 묶고 내려가.")
    m.talker(13, 21, "여관 주인", "north_bear_granny_tall", "고개 넘느라 춥지? 꿀술 한 잔에 화롯가 자리는 공짜야.")
    m.talker(36, 28, "남쪽에서 온 향신료 상인", "north_p_trader_tall", "북쪽은 처음이라 털옷부터 샀어. 대신 후추는 여기서 금값이더군.")
    m.talker(9, 27, "천막 상인 아이", "north_p_boy_tall", "우리 아빠는 네 나라 말을 다 할 줄 알아!")
    m.walker([(27, 2), (28, 9), (30, 16), (27, 21), (33, 22), (30, 28), (29, 36), (29, 44)], idx=6)
    m.walker([(2, 21), (10, 21), (20, 22), (40, 22), (50, 21), (57, 21)], idx=7)
    m.critters("raven", (4, 38, 30, 42), 1)
    auto_fill(m, ["woodpile", "camp", "cairn"], max_n=4)

    m.dense_border("pine")
    m.grove(52, 6, 4, 2.5, "pine"); m.grove(6, 41, 4, 2, "thaw")
    m.scatter("snowpine", 4, area=(3, 3, 56, 42), margin=1)
    m.scatter("snowpine_g", 4, area=(3, 3, 56, 42), margin=1)
    m.snow_props([(52, 14), (18, 40)])
    m.finish()
    return m


def road_nw():
    """① 눈숲 고갯길: 북문 마을에서 서쪽 눈숲 마을로 비스듬히 내려가는 숲길. 나무꾼 야영지, 얼어붙은 웅덩이.
    큰길은 돌아가고, 눈밭 지름길은 걸어서 빠르지만 수레가 느리다. 쓰러진 통나무가 길을 막을 수 있다.
    왼쪽 아래는 피자 조각 경계 밖이라 숲."""
    m = ThawMap("north_c_road_nw", "눈숲 고갯길", 62, 46, base="snow", cover=(0.05, 0.4), seed=203)
    m.hours = 1
    m.link("E", 20, 4, "north_c_gatetown", "W", "E")
    m.link("S", 46, 4, "north_c_snowwood", "N", "S")
    main = [(61, 21), (52, 21), (44, 17), (34, 19), (28, 25), (32, 33), (42, 38), (47, 45)]
    m.road_path(main, width=2, grade=2)
    # 10/26 북쪽 숲길: 바로 위 북유럽 모피 사냥꾼 마을로
    m.road_path([(22, 0), (22, 8), (25, 17), (28, 24)], width=2)
    m.link("N", 21, 4, "north_hunter", "S", "N")
    m.sign(25, 4, "↑ 모피 사냥꾼 마을 (북유럽)\n↘ 눈숲 마을   → 북문 마을")
    snow_trail(m, [(52, 22), (50, 30), (47, 40)])                  # 눈밭 지름길 (걸어서 빠름, 수레는 느림)
    wedge_forest(m, -62, 0, "pine")
    # 나무꾼 야영지
    m.deco_stamp("tent", 38, 6); m.deco_stamp("fire", 43, 8)
    m.deco_stamp("logs", 46, 7); m.deco_stamp("logs_birch", 46, 9); m.deco_stamp("logs", 34, 10)
    m.deco_stamp("stumpbig", 49, 11); m.deco_stamp("stump", 37, 12); m.deco_stamp("sled", 32, 13); m.deco_stamp("firewood", 44, 12)
    m.road_path([(41, 13), (43, 17)], width=1)
    m.stamp("h_shed", 50, 4, force=True); m.door_path(50, 4, 3, 3, 9, door_dx=1); m.road_path([(51, 9), (46, 12)], width=1)   # 나무꾼 오두막
    # 얼어붙은 웅덩이와 무너진 사냥 오두막
    m.water_blob(14, 12, 5, 3)
    m.deco_stamp("ruinhut", 21, 30); m.deco_stamp("pelt", 25, 34)
    m.road_path([(24, 33), (28, 28)], width=1)

    m.sign(55, 18, "← 눈숲 고갯길: 눈숲 마을 (썰매 정류장)\n  큰길은 돌아가고, 눈밭 지름길은 수레가 느림")
    m.talker(39, 14, "나무꾼", "north_moose_tall", "여기서부터 남쪽은 자작나무랑 참나무가 섞여. 땔감이 좋아지지.",
             "바람 부는 날엔 큰 나무가 길에 쓰러져. 치우든지 돌아가든지 해야지.")
    m.walker([(60, 21), (52, 21), (44, 18), (34, 19), (28, 25), (32, 33), (42, 38), (47, 44)], idx=1)
    m.event(30, 22); m.event(37, 36)                                 # 쓰러진 통나무 자리
    m.critters("raven", (14, 18, 26, 26), 2)
    auto_fill(m, ["woodpile", "cairn", "hunt", "icehole", "camp", "lookout"], talks=[6])   # 10/27 나무꾼은 하나만

    m.dense_border("pine")
    m.grove(56, 38, 3, 2.5, "pine")
    m.scatter("snowpine", 8, area=(3, 3, 58, 42), margin=1)
    m.scatter("snowround", 4, area=(3, 3, 58, 42), margin=1)
    m.scatter("snowboulder", 3, area=(3, 3, 58, 42), margin=1)
    m.snow_props([(10, 6), (56, 6)])
    m.finish()
    return m


def road_ne():
    """② 개울 다리길: 북문 마을에서 동쪽 개울 나루 마을로. 큰길은 개울 서쪽을 따라 곧장 내려가고,
    개울 다리를 건너면 바닷가 낚시터 (봄 눈 녹은 물에 개울이 넘칠 수 있다). 바다 건너가 피오르 나루 마을.
    오른쪽 아래는 피자 조각 경계 밖이라 숲."""
    m = ThawMap("north_c_road_ne", "개울 다리길", 62, 46, base="snow", cover=(0.08, 0.42), seed=207)
    m.hours = 1
    m.link("W", 20, 4, "north_c_gatetown", "E", "W")
    m.link("S", 12, 4, "north_c_creek", "N", "S")
    # 북쪽 바닷가 (10/26 얼음 나루길은 비르카 → 국경 고개로 옮김. 건너편이 피오르 나루 마을)
    m.river_h(0, 2, wiggle=1, fixed=range(37, 46))
    # 개울: 피오르에서 남쪽 개울 나루 마을로 흐름 (개울 나루 마을의 개울과 이어지는 자리)
    m.river_v(26, 29, wiggle=1, fixed_rows=range(14, 21))
    m.bridge(24, 16, 31, 18)                                       # 개울 건너 바닷가 낚시터로
    m.road_path([(23, 17), (16, 21)], width=2)
    m.road_path([(32, 17), (38, 12), (41, 7)], width=1)
    m.deco_stamp("fishbasket", 42, 5)
    m.stamp("h_turf", 45, 7, force=True); m.door_path(45, 7, 3, 2, 11, door_dx=1); m.road_path([(46, 11), (40, 9)], width=1)   # 어부 움집
    m.fishing.append((41, 3))
    # 큰길: 북문 마을 → 개울 나루 마을
    m.road_path([(0, 21), (10, 21), (15, 26), (13, 35), (13, 45)], width=2, grade=2)
    m.sign(18, 24, "↓ 개울 나루 마을 (낚시 · 나룻배)\n← 북문 마을\n→ 개울 다리 건너 바닷가 낚시터")
    m.sign(33, 20, "개울 다리\n눈 녹은 물이 불면 다리가 잠김")
    wedge_forest(m, 60, 0, "coast")
    m.talker(21, 13, "다리지기", "north_p_oldman_tall", "봄엔 눈 녹은 물이 불어서 다리가 잠길 때가 있어. 그땐 물이 빠질 때까지 기다리게.")
    m.talker(7, 26, "길손 음유시인", "north_p_bard_tall", "얼음 밑 물고기는 겨울에 더 달다네. 개울 나루 마을에 가면 얼음 낚시를 할 수 있지.")
    m.walker([(2, 21), (10, 21), (15, 26), (13, 35), (13, 44)], idx=2)
    m.event(22, 18); m.event(33, 15)                                 # 강 범람 자리 (다리 양쪽)
    m.critters("gull", (30, 4, 56, 10), 2); m.critters("raven", (4, 30, 10, 40), 1)
    auto_fill(m, ["icehole", "cairn", "camp", "stones", "cargo"], talks=[4])

    m.dense_border("coast")
    m.grove(6, 8, 4, 3, "pine")
    m.scatter("snowpine", 6, area=(3, 5, 58, 42), margin=1)
    m.scatter("snowpine_g", 4, area=(3, 20, 58, 42), margin=1)
    m.scatter("snowboulder", 3, area=(3, 5, 58, 42), margin=1)
    m.snow_props([(6, 34), (50, 12)])
    m.finish()
    return m


def snow_wood():
    """눈숲 마을: 서쪽 숲 속 썰매 마을. 썰매 트랙 출발대 = 썰매 언덕 미니게임 (20냥, NorthMinigames.cs).
    나무·고기가 나는 마을이라 말린 고기·모피가 싸고 소금·꿀술이 귀하다. 서쪽은 북서 포구(베네치아) 쪽 (합칠 때 연다)."""
    m = ThawMap("north_c_snowwood", "눈숲 마을", 52, 40, base="snow", cover=(0.25, 0.35), seed=211)
    m.hours = 1
    m.link("N", 24, 4, "north_c_road_nw", "S", "N")
    m.link("S", 24, 4, "north_c_road_sw", "N", "S")
    m.road_path([(25, 0), (25, 8)], width=2, grade=2)
    m.road_blob(25, 12.5, 7, 3.5, grade=1)                         # 작은 장터 마당
    m.road_path([(25, 16), (25, 26), (25, 39)], width=2, grade=2)
    # 10/27 통합 ①: 서쪽 → 베네치아 포도밭 북쪽 성문 길 (World/Connections/region_links.json)
    m.road_path([(18, 13), (0, 12)], width=2)
    m.link("W", 11, 4, "venice_c_vineyard", "link_north_c_snowwood", "link_venice_c_vineyard")
    m.sign(3, 10, "← 북서 포구 (베네치아 쪽)\n포도밭 성문 길")
    # 10/26 동쪽 샛길: 룬돌 큰길로
    m.road_path([(29, 15), (36, 18), (44, 19), (51, 19)], width=2)
    m.link("E", 18, 4, "north_c_runeway", "W", "E")
    m.sign(46, 16, "→ 룬돌 큰길\n북문 마을 · 길목 마을")
    wedge_forest(m, -40, 46, "pine")
    # 장터와 집들
    m.deco_stamp("stall", 20, 9); m.deco_stamp("stall", 29, 9)
    m.deco_stamp("pelt", 23, 10); m.deco_stamp("firewood", 28, 11); m.deco_stamp("lamp", 18, 8); m.deco_stamp("lamp", 32, 8)
    m.add("market", 25, 13, town="north_cwood")
    m.board(31, 13, "north_cwood")
    m.stamp("h_turf", 34, 4, force=True); m.door_path(34, 4, 3, 2, 12, door_dx=1)
    m.stamp("h_shed", 12, 3, force=True); m.door_path(12, 3, 3, 3, 10, door_dx=1)
    m.stamp("woodhouse", 38, 13, force=True); m.door_path(38, 13, 3, 3, 19, door_dx=0)
    m.road_path([(29, 13), (36, 13)], width=1)
    # 썰매 눈밭과 트랙
    m.patch_area(4, 22, 22, 34)
    m.sled_track = [(6, 24), (14, 23), (20, 25), (20, 31), (12, 33), (6, 31), (6, 24)]
    m.road_path(m.sled_track, width=1)
    m.deco_stamp("sled", 8, 20); m.deco_stamp("sled", 15, 20); m.deco_stamp("rope", 4, 20); m.deco_stamp("lantern", 11, 20)
    # 순록 우리
    m.fence(32, 24, 46, 34, gap=(36, 24)); m.reserve(33, 25, 45, 33)
    m.critters("reindeer", (34, 26, 44, 32), 3)
    m.deco_stamp("haybale", 41, 30); m.deco_stamp("bucket", 34, 30)
    m.road_path([(27, 22), (36, 23)], width=1)

    m.sign(28, 6, "눈숲 마을\n↑ 눈숲 고갯길 · 북문 마을\n↓ 서남 솔숲길 · 북쪽 길목 마을")
    m.talker(17, 17, "썰매꾼", "north_p_herder_tall", "저기 트랙 출발대에서 20냥 내면 언덕 썰매 한 판! 나무 피하고 눈송이 모으면 상금을 줘.",
             "통나무는 Space로 뛰어넘어. 내 최고 기록은 아직 아무도 못 깼지.",
             "짐 싣는 썰매는 순록 고원의 하르 님이 허락해야 빌려줄 수 있어.")
    m.talker(22, 5, "숲 마을 장터지기", "north_p_smith_tall", "여긴 나무랑 고기가 넘쳐. 대신 소금이랑 꿀술은 늘 모자라지.")   # 10/27 좌판 지붕 바로 위(22,7)에서 두 칸 위로: 이름표·외침 안 겹치게
    m.talker(37, 21, "순록 돌보는 아이", "north_p_girl_tall", "이 순록은 루돌프 아니야. 이름은 '눈송이'야!")
    m.walker([(25, 1), (25, 8), (22, 12), (28, 13), (25, 18), (25, 30), (25, 38)], idx=8)
    m.critters("raven", (38, 4, 48, 10), 1)
    m.deco_stamp("sled", 28, 34); m.deco_stamp("crate", 30, 34); m.deco_stamp("lantern", 22, 36)
    auto_fill(m, ["woodpile", "hunt", "herd", "camp"], max_n=6)

    m.dense_border("pine")
    m.grove(46, 36, 3, 2, "pine")
    m.scatter("snowpine", 3, area=(3, 3, 48, 36), margin=1)
    m.finish()
    return m


def creek_town():
    """개울 나루 마을: 큰 개울가의 물방앗간 마을. 얼음 낚시터 두 곳, 피오르 나루 마을로 가는 나룻배 (NorthDaily.cs).
    생선이 싸고 치즈·약초가 귀하다. 10/28 고려 쪽은 남쪽 동남 물가길 아래로 간다 (동쪽 다리길 ②는 뺌)."""
    m = ThawMap("north_c_creek", "개울 나루 마을", 52, 40, base="snow", cover=(0.25, 0.35), seed=213)
    m.hours = 1
    m.link("N", 24, 4, "north_c_road_ne", "S", "N")
    m.link("S", 24, 4, "north_c_road_se", "N", "S")
    m.road_path([(25, 0), (25, 8)], width=2, grade=2)
    m.road_blob(24, 12.5, 7, 3.5, grade=1)
    m.road_path([(25, 16), (25, 26), (25, 39)], width=2, grade=2)
    # 큰 개울 (동쪽). 10/28 동쪽 다리길(②)은 빼고, 고려 쪽은 남쪽 동남 물가길 아래로
    m.river_v(38, 41, wiggle=1, fixed_rows=range(9, 16))
    m.sign(31, 18, "고려 쪽 (내륙 교역 길목)\n↓ 남쪽 동남 물가길 아래로")
    wedge_forest(m, 48, 46, "coast")
    # 장터와 집들
    m.deco_stamp("stall", 19, 9); m.deco_stamp("stall", 16, 16)   # 10/27 오른쪽 좌판(군나르)은 게시판 아래로 두 줄 띄워 옮김 (가로등·사람과 안 겹치게)
    m.deco_stamp("fishbasket", 22, 10); m.deco_stamp("barrel_s", 27, 11); m.deco_stamp("lamp", 17, 8); m.deco_stamp("lamp", 31, 8)
    m.add("market", 24, 13, town="north_ccreek")
    m.board(18, 13, "north_ccreek")
    m.stamp("h_stoneB", 8, 4, force=True); m.door_path(8, 4, 2, 2, 11, door_dx=0)
    m.stamp("h_round", 30, 3, force=True); m.door_path(30, 3, 2, 3, 9, door_dx=0)
    # 10/27 물방앗간: 둥근 회관 오른쪽 개울가 (물레방아는 개울 쪽). 문(가운데 칸)에서 다리 길까지 샛길
    m.stamp("watermill", 35, 4, force=True); m.door_path(35, 4, 3, 3, 11, door_dx=1)
    m.stamp("woodhouse", 8, 15, force=True); m.door_path(8, 15, 3, 3, 21, door_dx=0)
    m.road_path([(9, 11), (18, 12)], width=1)
    # 10/26 서쪽 샛길: 룬돌 큰길로
    m.road_path([(0, 21), (9, 21), (24, 21), (25, 17)], width=2)
    m.link("W", 20, 4, "north_c_runeway", "E", "W")
    m.sign(4, 18, "← 룬돌 큰길\n북문 마을 · 길목 마을")
    # 얼음 낚시터 (개울가 둘)
    m.deco_stamp("fishbasket", 34, 22); m.deco_stamp("bucket", 34, 23)   # 10/27 모닥불은 서쪽 얼음 낚시터 옆으로 옮김
    m.fishing.append((38, 22))
    m.fishing.append((39, 31))
    m.road_path([(25, 24), (35, 24)], width=1); m.road_path([(25, 31), (36, 31)], width=1)
    # 10/27 큰길과 얼음 낚시터 사이 빈터에 상인 집 하나 + 앞마당 샛길 (기우 피드백)
    m.stamp("h_dark", 30, 16, force=True); m.door_path(30, 16, 4, 4, 24, door_dx=1)
    m.road_path([(26, 21), (31, 21)], width=1)
    # 나룻배: 개울 아래로 피오르까지 (피오르 나루 마을 서쪽 부두)
    m.spot("ferry", 36, 34)
    m.add("spawn", 35, 33, id="ferry")
    m.stamp("boat", 38, 35, force=True, over_road=True)
    m.sign(29, 32, "나룻배\n피오르 나루 마을 12냥")   # 10/27 숲 구석(33,36)에서 나룻배 가는 가로길 바로 아래 길가로

    m.sign(28, 3, "개울 나루 마을\n↑ 개울 다리길 · 북문 마을\n↓ 동남 물가길 · 북쪽 길목 마을")
    m.talker(34, 7, "물방앗간 주인", "north_p_fisher_tall", "개울 물고기는 여기서 제일 싸. 대신 치즈랑 약초는 귀해서 비싸게 사 주지.")
    m.talker(30, 27, "얼음 낚시꾼", "north_bear_sailor_tall", "얼음은 세 번은 내리쳐야 깨져. 바구니 옆이 대구가 잘 무는 자리야.")
    m.talker(13, 23, "빨래하는 아주머니", "north_fox_woman_tall", "나룻배 타면 피오르 나루까지 금방이야. 비르카를 돌아가는 것보다 훨씬 빠르지.")
    m.walker([(25, 1), (25, 8), (21, 12), (28, 13), (25, 18), (25, 30), (25, 38)], idx=9)
    m.critters("gull", (30, 30, 48, 38), 2)
    auto_fill(m, ["icehole", "hut", "camp"], max_n=3)   # 10/27 빨래터 아래 돌무더기·룬돌 → 흙집
    m.deco_stamp("fire", 14, 30)        # 10/27 빨래하는 아주머니 아래 얼음 낚시터 오른쪽 넓은 빈터에 모닥불 (의자 둘 자리 넉넉히)

    m.dense_border("coast")
    m.grove(4, 34, 3, 2, "thaw")
    m.scatter("snowpine_g", 3, area=(3, 3, 48, 36), margin=1)
    m.finish()
    return m


def rune_way():
    """③ 룬돌 큰길: 북문 마을에서 북쪽 길목 마을까지 피자 조각 한가운데를 곧게 내려가는 가장 짧은 큰길.
    언덕 위 룬돌 둥지, 숨은 룬돌 하나 (7개 중 1번). 가장 짧지만 눈늑대 패거리(도적)가 나오고, 쓰러진 통나무가 막을 때가 있다."""
    m = ThawMap("north_c_runeway", "룬돌 큰길", 36, 96, base="grass", cover=(0.5, 0.25), seed=217)
    m.hours = 1
    m.link("N", 16, 4, "north_c_gatetown", "S", "N")
    m.link("S", 16, 4, "north_c_crossing", "N", "S")
    main = [(17, 0), (17, 8), (14, 18), (15, 30), (20, 40), (19, 52), (15, 62), (18, 74), (17, 84), (17, 95)]
    m.road_path(main, width=3, grade=3)
    # 10/26 양옆 샛길: 서쪽 눈숲 마을, 동쪽 개울 나루 마을
    m.road_path([(0, 19), (8, 19), (14, 20)], width=2)
    m.link("W", 18, 4, "north_c_snowwood", "E", "W")
    m.road_path([(35, 21), (31, 23), (22, 24), (15, 26)], width=2)
    m.link("E", 20, 4, "north_c_creek", "W", "E")
    # 10/26 길손 역참: 큰길 중간의 작은 마을 (여관·마구간·우물)
    m.stamp("longhall", 23, 76, force=True); m.door_path(23, 76, 4, 7, 85, door_dx=1)
    m.stamp("h_turf", 28, 78, force=True); m.door_path(28, 78, 3, 2, 85, door_dx=1)
    m.stamp("h_shed", 22, 87, force=True); m.door_path(22, 87, 3, 3, 91, door_dx=1)
    m.road_path([(29, 85), (24, 85), (18, 86)], width=1); m.road_path([(23, 91), (18, 91)], width=1)
    m.stamp("well", 27, 88, force=True)
    m.deco_stamp("haybale", 29, 90); m.deco_stamp("lamp", 21, 83); m.deco_stamp("banner", 27, 75)
    # 언덕 위 룬돌 둥지 (작은 원)
    m.road_blob(26, 16, 4.5, 3, grade=1)
    m.road_path([(16, 16), (21, 16)], width=1)
    for i, (rx, ry) in enumerate([(22, 14), (26, 12), (30, 14), (30, 18), (26, 20), (22, 18)]):
        m.deco_stamp("runestone_b" if i % 2 else "runestone", rx, ry)
    m.deco_stamp("picturestone", 25, 15)       # 가운데: 배·태양 무늬 큰 그림돌
    m.deco_stamp("crys_b", 28, 26); m.deco_stamp("crys_p", 29, 27)
    # 숨은 룬돌: 서쪽 숲 틈 (길 없이 나무 사이로 들어가야 함)
    m.secret_rune(1, 5, 46); m.deco_stamp("gem_b", 7, 48)
    m.patch_area(3, 45, 9, 50)
    # 길손 쉼터 (중간)
    m.deco_stamp("tent", 25, 50); m.deco_stamp("fire", 26, 54)
    m.road_path([(21, 52), (24, 53)], width=1)

    m.sign(20, 4, "룬돌 큰길\n↓ 가장 빠른 길: 북쪽 길목 마을\n  (눈늑대 패거리 주의)")
    m.sign(4, 16, "← 눈숲 마을")
    m.sign(30, 26, "→ 개울 나루 마을")
    m.sign(20, 80, "길손 역참\n쉬어 가는 곳 · 마구간")
    m.talker(21, 88, "역참지기", "north_p_herder_tall", "큰길 한가운데라 다들 여기서 말에게 물을 먹이고 가지.",
             "눈늑대 패거리는 이 아래쪽에 자주 나와. 짐이 무거우면 양옆 마을로 돌아가는 게 나아.")
    m.sign(19, 12, "→ 룬돌 둥지\n옛 사람들이 길을 비는 곳")
    m.talker(28, 16, "룬 읽는 노인", "north_owl_elder_tall", "이 돌들은 길 떠나는 상인을 지켜 줬지. 북쪽 땅엔 이런 돌이 일곱 개 숨어 있다네.",
             "하나는 이 길 서쪽, 나무 사이에 숨어 있지. 눈 녹은 자리를 잘 보게.")
    m.talker(28, 52, "길손 행상", "north_p_trader_tall", "중앙에선 북쪽 모피가 금값이라더군. 북문 마을에서 소금을 사 가면 사냥꾼들이 반겨.",
             "이 큰길엔 눈늑대가 나온다니 짐이 많으면 양옆 마을로 돌아가.")
    m.walker([(17, 2), (17, 9), (14, 19), (15, 31), (20, 41), (19, 53), (15, 63), (18, 75), (17, 94)], idx=3)
    m.bandit(17, 70, radius=5)                                       # 눈늑대 패거리 (도적 기능)
    m.event(14, 24); m.event(17, 82)                                 # 쓰러진 통나무 자리
    m.critters("raven", (22, 70, 32, 80), 2)
    auto_fill(m, ["stones", "shrine", "crystal", "herd", "cairn", "camp"], margin=4)   # 10/27 역참 마을이 있어 3명까지

    m.dense_border("thaw")
    m.grove(28, 38, 3, 2.5, "green"); m.grove(7, 26, 3, 2.5, "thaw"); m.grove(6, 70, 3, 2.5, "green")
    m.scatter("snowpine_g", 4, area=(3, 3, 32, 40), margin=1)
    m.scatter("tree", 5, area=(3, 40, 32, 92), margin=1)
    m.scatter("pine", 4, area=(3, 3, 32, 92), margin=1)
    m.finish()
    return m


def road_sw():
    """④ 서남 솔숲길: 눈숲 마을에서 북쪽 길목 마을로 비스듬히 내려가는 솔숲 길. 눈이 거의 녹은 숲.
    숲속 산적 패거리와 쓰러진 통나무. 대신 길이 다져져 있어 수레가 편하다. 왼쪽은 피자 조각 경계 밖이라 숲."""
    m = ThawMap("north_c_road_sw", "서남 솔숲길", 40, 77, base="grass", cover=(0.45, 0.15), seed=219)
    m.hours = 1
    m.link("N", 24, 4, "north_c_snowwood", "S", "N")
    m.link("E", 66, 4, "north_c_crossing", "W", "E")
    main = [(25, 0), (24, 10), (27, 22), (29, 36), (31, 50), (34, 60), (39, 67)]
    m.road_path(main, width=3, grade=3)
    # 10/28 통합: 가운데 갈림길에서 서쪽 숲길로 베네치아 본토 산기슭 고갯길 (북↔서 두 번째 길, ⑨)
    m.road_path([(29, 40), (20, 41), (10, 40), (0, 40)], width=2, grade=2)
    m.link("W", 39, 3, "venice_c_pass", "link_north_c_road_sw", "link_venice_c_pass")
    m.sign(25, 38, "← 산기슭 고갯길 · 베네치아 본토\n↘ 북쪽 길목 마을")
    # 10/28 통합: 아래쪽 갈림길에서 남쪽 숲길로 베네치아 본토 동문 큰길 (⑩)
    m.road_path([(33, 57), (29, 66), (27, 76)], width=2, grade=2)
    m.link("S", 26, 4, "venice_c_gate", "link_north_c_road_sw", "link_venice_c_gate")
    m.sign(30, 63, "↓ 동문 큰길 (베네치아 본토)\n→ 북쪽 길목 마을")
    wedge_forest(m, -40, 86, "green")
    # 숲속 산적 은신처 (무너진 오두막과 모닥불)
    m.deco_stamp("ruinhut", 31, 9); m.deco_stamp("fire", 33, 15); m.deco_stamp("crate", 30, 16); m.deco_stamp("sack", 35, 16)
    m.road_path([(31, 16), (27, 20)], width=1)
    # 숯 굽는 가마터
    m.deco_stamp("logs", 12, 6); m.deco_stamp("logs_birch", 12, 8); m.deco_stamp("stumpbig", 15, 10); m.deco_stamp("firewood", 10, 10)
    m.road_path([(16, 8), (23, 9)], width=1)
    # 10/26 숯쟁이 마을: 숯 가마 옆 움집 둘
    m.stamp("h_turf", 18, 14, force=True); m.door_path(18, 14, 3, 2, 18, door_dx=1); m.road_path([(19, 18), (25, 18)], width=1)
    m.stamp("h_shed", 16, 23, force=True); m.door_path(16, 23, 3, 3, 27, door_dx=1); m.road_path([(17, 27), (28, 26)], width=1)
    m.deco_stamp("firewood", 16, 17); m.deco_stamp("logs_birch", 21, 13)
    m.sign(21, 20, "숯쟁이 마을")

    m.sign(28, 3, "서남 솔숲길\n↘ 북쪽 길목 마을 (다져진 길, 수레 편함)\n← 가운데 갈림길: 베네치아 본토\n  숲속 산적 주의")
    m.talker(18, 12, "숯쟁이", "north_p_smith_tall", "솔숲 깊은 데엔 산적이 숨어 있어. 짐이 무거우면 걸음이 느려서 쫓기기 쉬워.")
    m.talker(34, 46, "순록 치는 소녀", "north_p_girl_tall", "순록들이 눈 녹은 자리의 이끼를 찾아 내려왔어.")
    m.walker([(25, 2), (24, 11), (27, 23), (29, 37), (31, 51), (34, 61), (38, 67)], idx=4)
    m.bandit(28, 30, radius=6)                                       # 숲속 산적
    m.deco_stamp("sled", 32, 33); m.deco_stamp("crate", 34, 33); m.deco_stamp("sack", 34, 35); m.deco_stamp("barrel_s", 31, 35)   # 털린 짐 자리
    m.event(25, 16); m.event(32, 56)                                 # 쓰러진 통나무 자리
    m.critters("raven", (28, 24, 36, 32), 2); m.critters("reindeer", (30, 38, 36, 44), 2)
    auto_fill(m, ["hunt", "woodpile", "cairn", "camp", "herd", "stones"], talks=[1], margin=4)

    m.dense_border("green")
    m.scatter("pine", 6, area=(3, 3, 36, 73), margin=1)
    m.scatter("tree", 4, area=(3, 20, 36, 73), margin=1)
    m.finish()
    return m


def road_se():
    """⑤ 동남 물가길: 개울 나루 마을에서 북쪽 길목 마을로 비스듬히 내려가는 강가 길. 위쪽은 개울을 바짝 따라가는 다져진 큰길.
    눈이 녹으면 개울이 넘쳐 길이 잠긴다 (강 범람) → 서쪽 숲 지름길로 돌아갈 수 있다 (수레는 느림). 오른쪽은 피자 조각 경계 밖이라 숲."""
    m = ThawMap("north_c_road_se", "동남 물가길", 40, 77, base="grass", cover=(0.4, 0.12), seed=223)
    m.hours = 1
    m.link("N", 12, 4, "north_c_creek", "S", "N")
    m.link("W", 66, 4, "north_c_crossing", "E", "W")
    m.river_v(26, 29, wiggle=1, fixed_rows=range(0, 4))           # 개울 나루 마을에서 내려오는 개울
    main = [(13, 0), (18, 8), (21, 18), (16, 30), (11, 42), (6, 55), (0, 67)]
    m.road_path(main, width=3, grade=3)                            # 개울 둑을 따라 내려가는 큰길
    snow_trail(m, [(13, 3), (8, 20), (5, 40), (3, 55)])            # 개울에서 먼 숲 지름길 (넘칠 걱정 없지만 수레는 느림)
    # 10/28 통합: 큰길 아래쪽에서 남쪽으로 고려 쪽 내륙 교역 길목 (⑪, 예전 개울 나루 동쪽 다리길 ② 대신)
    m.road_path([(9, 48), (14, 62), (16, 76)], width=2, grade=2)
    m.link("S", 15, 4, "transit_crossroads", "link_north_c_road_se", "link_transit_crossroads")
    wedge_forest(m, 60, 86, "green")
    # 물레방아 돌집과 빨래터
    m.stamp("h_stoneA", 7, 8, force=True); m.door_path(7, 8, 2, 2, 12, door_dx=0)
    m.road_path([(8, 12), (17, 10)], width=1)
    m.deco_stamp("barrel", 8, 8); m.deco_stamp("basket_veg", 2, 13)
    # 10/26 물레방아 마을: 강가 집 둘 더
    m.stamp("h_turf", 12, 24, force=True); m.door_path(12, 24, 3, 2, 27, door_dx=1); m.road_path([(13, 27), (17, 27)], width=1)
    m.stamp("woodhouse", 2, 26, force=True); m.door_path(2, 26, 4, 5, 33, door_dx=1); m.road_path([(3, 33), (14, 33)], width=1)
    m.sign(9, 22, "물레방아 마을")
    m.fishing.append((26, 12))                                     # 개울가 낚시 자리
    m.deco_stamp("fishbasket", 23, 10)

    m.sign(16, 3, "동남 물가길\n↓ 개울 둑 큰길: 북쪽 길목 마을 (눈 녹으면 넘침)\n↙ 숲 지름길: 넘칠 걱정 없지만 수레는 느림\n↓ 아래쪽 갈림길: 고려 쪽 내륙 교역 길목")
    m.sign(17, 60, "↓ 내륙 교역 길목 (고려 쪽)\n← 북쪽 길목 마을")
    m.talker(14, 24, "강지기", "north_p_oldman_tall", "개울이 넘치면 물이 빠질 때까지 기다리게. 급하면 서쪽 숲 지름길로 돌아가고.")
    m.talker(9, 15, "물레방아 할머니", "north_p_granny_tall", "젊었을 땐 북쪽에서 살았지. 이젠 눈 대신 물소리가 좋아.")
    m.walker([(13, 2), (18, 9), (21, 19), (16, 31), (11, 43), (6, 56), (1, 67)], idx=5)
    m.event(22, 14); m.event(19, 24)                                 # 강 범람 자리 (개울 둑 큰길)
    m.critters("gull", (18, 4, 30, 10), 2)
    auto_fill(m, ["icehole", "camp", "cairn", "cargo", "stones"], talks=[3], margin=4)

    m.dense_border("green")
    m.scatter("tree", 5, area=(3, 3, 36, 73), margin=1)
    m.scatter("pine", 4, area=(3, 3, 36, 73), margin=1)
    m.finish()
    return m


def crossing_town():
    """북쪽 길목 마을: 북쪽 1/4의 뾰족한 끝, 대륙 중심으로 내려가기 전 마지막 마을. 세 길(솔숲길·룬돌 큰길·물가길)이 모인다.
    네 문명 상단이 묵어 가는 큰 장터: 북쪽 물건(모피·호박·치즈·꿀술)을 중앙으로 넘기는 곳이라 비싸게 사 준다."""
    m = ThawMap("north_c_crossing", "북쪽 길목 마을", 60, 46, base="grass", cover=(0.12, 0.0), seed=227)
    m.hours = 1
    m.link("N", 28, 4, "north_c_runeway", "S", "N")
    m.link("W", 10, 4, "north_c_road_sw", "E", "W")
    m.link("E", 10, 4, "north_c_road_se", "W", "E")
    m.road_path([(29, 0), (29, 8), (30, 14)], width=3, grade=3)
    m.road_path([(0, 11), (10, 12), (21, 17)], width=3, grade=3)       # 서남 솔숲길에서 비스듬히
    m.road_path([(59, 11), (50, 12), (39, 17)], width=3, grade=3)      # 동남 물가길에서 비스듬히
    m.road_blob(30, 19.5, 10, 5, grade=1)                         # 큰 장터 광장 (10명 넘게 모여도 넉넉)
    m.road_path([(30, 25), (30, 34), (30, 45)], width=3, grade=3)
    # 10/27 통합: 남쪽 → 대륙 한가운데 초원 (아라비아 중앙, 만국 장터 쪽) (World/Connections/region_links.json)
    m.link("S", 29, 4, "arabia_central_steppe", "link_north_c_crossing", "link_arabia_central_steppe")
    m.sign(34, 42, "↓ 대륙 중심 · 한가운데 초원\n만국 장터 · 베네치아 · 고려 쪽")
    # 장터: 네 문명 좌판
    m.deco_stamp("stall", 22, 15); m.deco_stamp("stall", 35, 15); m.deco_stamp("stall", 22, 22); m.deco_stamp("stall", 35, 22)
    m.deco_stamp("crate_goods", 26, 16); m.deco_stamp("pelt", 33, 17); m.deco_stamp("jar_red", 26, 23); m.deco_stamp("sack_big", 39, 21)
    m.deco_stamp("lamp", 19, 14); m.deco_stamp("lamp", 41, 14); m.deco_stamp("lamp", 19, 24); m.deco_stamp("lamp", 41, 24)
    m.add("market", 30, 20, town="north_cross")
    m.board(24, 12, "north_cross")
    # 여관과 상단 숙소, 창고
    m.stamp("longhall", 4, 15, force=True); m.door_path(4, 15, 4, 7, 26, door_dx=1)
    m.stamp("house3", 44, 3, force=True); m.door_path(44, 3, 3, 3, 13, door_dx=0)
    m.stamp("h_dark", 44, 27, force=True); m.door_path(44, 27, 4, 4, 33, door_dx=1)
    m.road_path([(45, 33), (32, 30)], width=1)
    # 대륙 중심 쪽 큰길 장식 (북유럽식 기둥과 등불)
    for y in (34, 40):
        m.deco_stamp("totem", 27, y); m.deco_stamp("totem2", 33, y)
    m.deco_stamp("lamp", 27, 37); m.deco_stamp("lamp", 33, 37)
    # 상단 야영지
    m.deco_stamp("tent", 8, 28); m.deco_stamp("tent", 14, 30); m.deco_stamp("fire", 12, 34)
    m.deco_stamp("bigbarrel", 18, 33); m.deco_stamp("spit", 15, 36)
    m.road_path([(13, 27), (22, 22)], width=1)

    m.sign(33, 4, "북쪽 길목 마을\n↑ 룬돌 큰길 · 북문 마을\n← 서남 솔숲길 · 눈숲 마을\n→ 동남 물가길 · 개울 나루 마을")
    m.talker(30, 11, "상단 우두머리", "north_p_trader_tall", "네 나라 상단이 다 이 장터에서 하룻밤 묵고 대륙 중심으로 내려가지.",
             "북쪽 모피, 동쪽 비단, 남쪽 향신료, 서쪽 유리… 여기서 다 만나.")
    m.talker(34, 39, "길목 문지기", "north_wolf_viking_tall", "남쪽으로 내려가면 대륙 한가운데 초원이야. 네 나라 상단이 다 거기 만국 장터로 모여.")   # 10/27 통합: 남쪽 길 열림
    m.talker(10, 24, "여관 주인", "north_bear_granny_tall", "먼 길 왔지? 북쪽 꿀술 한 잔 하고 가.")
    m.walker([(29, 1), (29, 9), (30, 15), (25, 20), (35, 20), (30, 26), (30, 40)], idx=10)
    m.walker([(1, 11), (10, 12), (21, 17), (39, 17), (50, 12), (58, 11)], idx=11)
    # 10/26 수입품 좌판: 네 문명 상단 (화요일 통합 때 실제 물건으로 연결)
    m.deco_stamp("stall", 45, 20); m.deco_stamp("stall", 50, 20); m.deco_stamp("stall", 55, 20)
    m.deco_stamp("crate_goods", 48, 22); m.deco_stamp("jar_red", 53, 22); m.deco_stamp("sack_big", 58, 22)
    m.sign(50, 18, "수입품 좌판\n고려 · 베네치아 · 아라비아 상단\n(배가 들어오면 문을 엶)")
    m.talker(46, 23, "고려 상단 행수", "north_p_trader_tall", "동쪽 고려에서 비단이랑 인삼을 싣고 오는 중이야. 북쪽 모피랑 바꾸면 좋겠어.")
    m.talker(51, 23, "베네치아 유리 상인", "north_p_bard_tall", "서쪽 운하 나라의 유리 구슬이 곧 도착해. 호박이랑 나란히 두면 반짝반짝하지.")
    m.talker(54, 24, "아라비아 향신료 상인", "north_p_boy_tall", "남쪽 사막을 건너온 후추랑 계피야! 아직은 맛보기만 보여 줄 수 있어.")
    # 10/26 만남의 광장: 우물·벤치·모닥불. 여기서 리액션(R)을 하면 근처 사람들이 따라 함
    m.stamp("well", 21, 31, force=True)
    m.deco_stamp("lamp", 19, 30)
    m.sign(25, 31, "만남의 광장\n함께 장사할 사람과 여기서 만나요\n(R: 리액션)")
    m.critters("raven", (44, 36, 56, 42), 1)
    auto_fill(m, ["camp", "cargo", "shrine"], max_n=3)

    m.dense_border("green")
    m.grove(54, 40, 3, 2, "green"); m.grove(4, 42, 3, 2, "green")
    m.scatter("tree", 4, area=(3, 3, 56, 42), margin=1)
    m.scatter("pine", 3, area=(3, 3, 56, 42), margin=1)
    m.finish()
    return m


MAPS = [gate_town, road_nw, road_ne, snow_wood, creek_town, rune_way, road_sw, road_se, crossing_town]
