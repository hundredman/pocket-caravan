"""중앙 경유 구역 - 북쪽 삼각형 (기우 담당, 2만5천 칸, 길 포함). 10/24.
팀 합의: 중앙 10만 칸을 X자 대각선으로 4등분, 각자 자기 지역을 마주 보는 삼각형을 만든다.
위(북유럽 국경 고개)에서 아래(중앙 도시 북문)로 내려갈수록 눈이 녹고 북유럽 색이 옅어진다.
  윗줄  ① 서쪽 눈숲 갈림길 63x56  ② 눈 녹는 고갯길 80x56  ③ 동쪽 개울 갈림길 63x56   (눈밭에 풀 조각)
  가운데 ⑤ 순록 썰매 정류장 60x44  ④ 교역 초소 마을 64x48  ⑥ 룬돌 언덕 오솔길 60x44    (풀밭에 큰 눈 조각)
  아래  ⑦ 북문 큰길 72x71                                                          (거의 풀밭)
  합계 3528+4480+3528+2640+3072+2640+5112 = 25,000
  출구 자리는 맵끼리 실제로 맞닿게 맞춤 (cartography.layout이 확인: 지도 위에서 겹치지 않고 이어짐)
아직 안 이어진 곳(화요일 합치기 전): 베네치아·고려 방면, 윤채·은샘 구역, 중앙 도시 북문 → 통나무 바리케이드와 표지판으로 막아 둠.
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
    elif kind == "cargo":      # 버려진 짐 (돌발 사건 자리)
        d("sled", x, y); d("crate", x + 2, y); d("sack", x + 2, y + 1); d("barrel_s", x - 1, y); m.event(x + 1, y + 2)
    elif kind == "icehole":    # 얼음 낚시 구멍 (낚시 미니게임 자리)
        m.water_blob(x, y, 2.6, 1.7)
        d("fishbasket", x + 3, y); d("bucket", x + 3, y + 1); d("stump", x - 4, y)
        m.fishing.append((x, y))
    elif kind == "camp":       # 길손 야영지
        d("fire", x, y); d("logbench", x - 1, y + 2); d("bedroll", x + 2, y)
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
        d("snowpost", x, y); m.flag(x + 1, y + 1); d("rocksmall", x - 1, y + 1)
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


def thaw_road():
    """② 눈 녹는 고갯길: 국경 고개에서 내려오는 S자 큰길. 북유럽 땅이 끝나는 국경 표석, 길손 쉼터, 녹기 시작한 연못."""
    m = ThawMap("north_c_thaw", "눈 녹는 고갯길", 80, 56, base="snow", cover=(0.04, 0.5), seed=101)
    m.hours = 1
    m.link("N", 40, 4, "north_pass", "S", "N")
    main = [(41, 0), (41, 6), (36, 12), (30, 20), (35, 28), (45, 34), (48, 42), (43, 49), (41, 55)]
    m.road_path(main, width=2, grade=2)
    m.link("S", 40, 4, "north_c_post", "N", "S")
    m.road_path([(30, 20), (19, 22), (8, 20), (0, 20)], width=2)
    m.link("W", 19, 4, "north_c_west", "E", "W")
    m.road_path([(45, 34), (58, 32), (70, 28), (79, 28)], width=2)
    m.link("E", 27, 4, "north_c_east", "W", "E")

    # 국경 표석: 여기서부터 중앙 교역지 (깃발 한 쌍과 룬 표석)
    m.flag(37, 3); m.flag(45, 3)
    m.deco_stamp("runestone", 47, 6); m.deco_stamp("snowpost", 36, 5)
    m.sign(48, 8, "↑ 국경 고개 · 북유럽\n↓ 중앙 교역지")
    # 녹기 시작한 연못 (가장자리만 얼음)
    m.water_blob(62, 13, 6, 3.4)
    m.deco_stamp("rockbig", 54, 8)
    m.deco_stamp("snowboulder", 68, 16)
    # 길손 쉼터: 천막·모닥불·벤치·썰매
    m.deco_stamp("tent", 49, 17); m.deco_stamp("fire", 50, 21); m.deco_stamp("logbench", 46, 22)
    m.deco_stamp("sled", 53, 21); m.deco_stamp("logs", 46, 19); m.deco_stamp("kettle", 52, 20)
    m.road_path([(48, 23), (42, 29)], width=1)
    # 서쪽 갈림길 이정표, 동쪽 갈림길 이정표, 아래쪽 교역 초소
    # 세 갈래 길: 무엇을 얼마나 실었느냐에 따라 고르는 길
    m.sign(27, 17, "← 서쪽 눈밭 지름길 (썰매 정류장)\n  걸어서 가장 빠름, 수레는 눈에 빠짐\n↓ 큰길: 초소까지 곧장 (눈늑대 주의)")
    m.sign(48, 31, "→ 동쪽 개울 큰길 (룬돌 오솔길)\n  멀지만 다져진 길이라 수레가 편함\n↓ 큰길: 초소까지 곧장 (눈늑대 주의)")
    m.bandit(45, 40, radius=5)          # 가운데 큰길: 가장 짧지만 눈늑대 패거리 (도적 기능)
    m.sign(46, 47, "↓ 눈길 교역 초소\n중앙 도시까지 반나절")
    # 녹는 땅: 아래쪽 풀밭에 버려진 짐수레 자리
    m.deco_stamp("crate", 20, 40); m.deco_stamp("sack", 21, 41); m.deco_stamp("stumpbig", 17, 38)
    m.deco_stamp("deadtree", 60, 44)

    m.talker(44, 9, "국경 표석 지기", "north_p_shield_tall", "이 돌부터 남쪽은 중앙 교역지야. 눈이 녹는 게 보이지?",
             "북쪽 상인들은 여기서 모피 짐을 한 번 더 단단히 묶고 내려가.")
    m.talker(51, 23, "쉬어 가는 짐꾼", "north_goat_tall", "고개를 넘으니 다리가 후들거려. 불 좀 쬐고 가야겠어.")
    m.walker([(41, 2), (41, 7), (36, 13), (31, 20), (36, 28), (45, 34), (48, 42), (42, 52)], idx=2)
    m.event(32, 24); m.event(47, 40)
    m.critters("raven", (10, 26, 30, 36), 2); m.critters("reindeer", (56, 36, 72, 46), 2)
    auto_fill(m, ["cargo", "cairn", "herd", "icehole", "camp", "stones", "woodpile", "hunt", "lookout"], talks=[3, 0, 4])

    m.dense_border("pine")
    m.grove(14, 9, 7, 4, "pine"); m.grove(66, 40, 5, 3, "thaw"); m.grove(16, 46, 6, 3.5, "thaw")
    m.scatter("snowpine", 10, area=(3, 3, 76, 30), margin=1)
    m.scatter("snowpine_g", 8, area=(3, 20, 76, 52), margin=1)
    m.scatter("pine", 6, area=(3, 34, 76, 52), margin=1)
    m.scatter("snowround", 6, area=(3, 3, 76, 52), margin=1)
    m.scatter("snowboulder", 4, area=(3, 3, 76, 52), margin=1)
    m.snow_props([(10, 14), (24, 8), (58, 25), (70, 8), (8, 32), (66, 22)])
    m.finish()
    return m


def west_fork():
    """① 서쪽 눈숲 갈림길: 침엽수가 활엽수로 바뀌는 숲. 나무꾼 야영지, 얼어붙은 웅덩이. 서쪽은 베네치아 방면(막힘)."""
    m = ThawMap("north_c_west", "서쪽 눈숲 갈림길", 63, 56, base="snow", cover=(0.1, 0.48), seed=103)
    m.hours = 1
    m.link("E", 19, 4, "north_c_thaw", "W", "E")
    m.road_path([(62, 20), (57, 21), (49, 18), (39, 22), (31, 24)], width=2)
    snow_trail(m, [(31, 25), (28, 33), (25, 42), (24, 50), (24, 55)])    # 썰매 정류장으로 가는 눈밭 지름길
    m.link("S", 23, 4, "north_c_sled", "N", "S")
    m.road_path([(31, 24), (20, 22), (8, 24), (3, 24)], width=2)
    closed_exit(m, "W", 23, "← 베네치아 방면 운하 길\n(길을 닦는 중, 아직 지날 수 없음)")

    # 나무꾼 야영지: 천막, 장작, 그루터기, 썰매에 실은 통나무
    m.deco_stamp("tent", 44, 7); m.deco_stamp("fire", 49, 9); m.deco_stamp("logbench", 46, 12)
    m.deco_stamp("logs", 52, 8); m.deco_stamp("logs_birch", 52, 10); m.deco_stamp("logs", 40, 11)
    m.deco_stamp("stumpbig", 55, 12); m.deco_stamp("stump", 43, 13); m.deco_stamp("stump", 58, 9)
    m.deco_stamp("sled", 38, 14); m.deco_stamp("firewood", 50, 13)
    m.road_path([(47, 14), (49, 18)], width=1)
    # 얼어붙은 웅덩이와 무너진 사냥 오두막
    m.water_blob(14, 10, 5.5, 3)
    m.deco_stamp("ruinhut", 18, 31); m.deco_stamp("pelt", 23, 35)
    m.road_path([(20, 34), (24, 30), (28, 31)], width=1)

    m.sign(34, 19, "→ 눈 녹는 고갯길\n↓ 눈밭 지름길: 순록 썰매 정류장\n  (길이 없어 수레는 느림)\n← 베네치아 방면 (공사 중)")
    m.talker(45, 15, "나무꾼", "north_moose_tall", "여기서부터 남쪽은 자작나무랑 참나무가 섞여. 땔감이 좋아지지.",
             "서쪽 운하 나라 사람들은 배 만들 나무를 사러 온다더군.")
    m.talker(9, 21, "길 닦는 인부", "north_p_smith_tall", "서쪽 길은 아직 공사 중이야. 운하 나라까지 이으려면 며칠 걸려.")
    m.walker([(60, 21), (57, 22), (49, 19), (39, 23), (31, 25), (28, 33), (25, 43), (24, 52)], idx=3)
    m.event(36, 23)
    m.critters("raven", (40, 26, 66, 40), 2)
    auto_fill(m, ["woodpile", "hunt", "icehole", "stones", "cairn", "herd", "camp"], talks=[5, 6])

    m.dense_border("mixed")
    m.grove(52, 36, 6, 4.5, "thaw"); m.grove(12, 45, 6, 3.5, "thaw"); m.grove(18, 44 - 30, 3, 2, "pine")
    m.scatter("snowpine", 8, area=(3, 3, 59, 22), margin=1)
    m.scatter("snowpine_g", 9, area=(3, 22, 59, 52), margin=1)
    m.scatter("tree", 6, area=(3, 30, 59, 52), margin=1)
    m.scatter("deadtree", 3, area=(3, 3, 59, 52), margin=1)
    m.snow_props([(28, 8), (56, 28), (44, 40), (6, 32), (20, 50)])
    m.finish()
    return m


def east_fork():
    """③ 동쪽 개울 갈림길: 얼음이 풀리는 개울과 나무다리, 얼음 낚시터, 강지기 돌집. 동쪽은 고려 방면(막힘)."""
    m = ThawMap("north_c_east", "동쪽 개울 갈림길", 63, 56, base="snow", cover=(0.1, 0.48), seed=107)
    m.hours = 1
    m.link("W", 27, 4, "north_c_thaw", "E", "W")
    m.road_path([(0, 28), (12, 28), (22, 24), (33, 22)], width=3, grade=3)
    m.road_path([(33, 22), (38, 30), (31, 37), (37, 45), (27, 50), (26, 55)], width=3, grade=3)   # 개울 큰길: 굽이져 멀지만 수레가 편함
    m.link("S", 25, 4, "north_c_rune", "N", "S")
    # 얼음 나루길: 비르카 항구에서 언 피오르를 건너 내려오는 두 번째 길 (사냥꾼·고원 상인이 비르카 큰길을 안 돌아도 됨)
    m.river_h(0, 2, wiggle=1, fixed=range(29, 38))
    m.bridge(32, 0, 34, 4, dock=True)
    m.link("N", 32, 3, "north_birka", "S", "N")
    m.road_path([(33, 5), (33, 12), (33, 22)], width=2)
    m.sign(36, 6, "↑ 얼음 나루길\n언 피오르를 건너 비르카 항구")
    # 개울: 위에서 아래로 흐름, 다리 자리는 곧게
    m.river_v(45, 47, wiggle=2, fixed_rows=range(17, 23))
    m.road_path([(33, 22), (42, 20)], width=2)
    m.bridge(43, 19, 49, 20)
    m.road_path([(50, 20), (56, 18), (60, 18)], width=2)
    closed_exit(m, "E", 17, "→ 고려 방면 솔숲 길\n(길을 닦는 중, 아직 지날 수 없음)")

    # 얼음 낚시터 (개울 서쪽 기슭)
    m.deco_stamp("fishbasket", 43, 26); m.deco_stamp("bucket", 43, 27); m.deco_stamp("rope", 41, 24)
    m.deco_stamp("logbench", 40, 29); m.deco_stamp("fire", 41, 26)
    m.fishing.append((45, 27))                          # 개울 얼음 낚시터 (낚시 미니게임 자리)
    # 강지기 돌집과 텃밭
    m.stamp("h_stoneB", 55, 28, force=True); m.door_path(55, 28, 2, 2, 33, door_dx=0)
    m.road_path([(55, 33), (52, 30), (50, 22)], width=1)
    m.deco_stamp("firewood", 58, 29); m.deco_stamp("barrel", 58, 30); m.deco_stamp("basket_veg", 53, 31)
    m.deco_stamp("rockbig", 18, 8); m.deco_stamp("rock", 60, 40)

    m.sign(20, 21, "← 눈 녹는 고갯길\n↓ 개울 큰길: 룬돌 언덕 오솔길\n↑ 얼음 나루길: 비르카 항구\n→ 고려 방면 (공사 중)")
    m.talker(43, 29, "얼음 낚시꾼", "north_p_fisher_tall", "봄이 오면 이 개울로 연어가 올라와. 그땐 남쪽 사람들도 줄을 서지.")
    m.talker(57, 33, "강지기", "north_p_oldman_tall", "다리가 얼어 미끄러우니 천천히 건너게. 수레는 특히.")
    m.walker([(3, 29), (12, 29), (22, 25), (33, 23), (37, 30), (31, 37), (36, 45), (27, 50), (26, 53)], idx=5)
    m.event(26, 26)
    m.critters("raven", (6, 34, 22, 44), 2); m.critters("gull", (40, 4, 52, 14), 2)
    auto_fill(m, ["icehole", "cargo", "cairn", "crystal", "camp", "stones"], talks=[4, 1])

    m.dense_border("coast")
    m.grove(12, 40, 6, 4, "thaw"); m.grove(56, 8, 4, 3, "pine"); m.grove(50, 46, 5, 3.5, "thaw")
    m.scatter("snowpine", 8, area=(3, 3, 59, 24), margin=1)
    m.scatter("snowpine_g", 8, area=(3, 24, 59, 52), margin=1)
    m.scatter("tree", 5, area=(3, 32, 59, 52), margin=1)
    m.scatter("snowboulder", 4, area=(3, 3, 59, 52), margin=1)
    m.snow_props([(10, 26), (28, 6), (58, 26), (18, 50)])
    m.finish()
    return m


def trade_post():
    """④ 눈길 교역 초소: 중앙으로 가는 상인들의 첫 쉼터이자 북쪽 물건의 수출항 (모피·호박을 가장 비싸게 삼, 남쪽 소금 특산).
    네 문명 상인이 섞이는 장터, 여관(롱하우스), 순록 마구간."""
    m = ThawMap("north_c_post", "눈길 교역 초소", 64, 48, base="grass", cover=(0.55, 0.25), seed=109)
    m.link("N", 32, 4, "north_c_thaw", "S", "N")
    m.link("S", 30, 4, "north_c_gate", "N", "S")
    m.link("W", 22, 4, "north_c_sled", "E", "W")
    m.link("E", 22, 4, "north_c_rune", "W", "E")
    m.road_path([(33, 0), (32, 10), (32, 18)], width=2, grade=2)
    m.road_blob(32, 23.5, 10, 5, grade=1)
    m.road_path([(32, 29), (31, 38), (31, 47)], width=2, grade=2)
    m.road_path([(0, 23), (12, 23), (22, 24)], width=2)
    m.road_path([(42, 24), (52, 23), (63, 23)], width=2)

    # 여관(롱하우스)과 교역소(짙은 목조), 초소 망대 대신 깃발
    m.stamp("longhall", 12, 8, force=True); m.door_path(12, 8, 4, 7, 21, door_dx=1)
    m.stamp("h_dark", 42, 9, force=True); m.door_path(42, 9, 4, 4, 19, door_dx=1)
    m.stamp("h_turf", 20, 11, force=True); m.door_path(20, 11, 3, 2, 19, door_dx=1)
    m.deco_stamp("banner", 11, 15); m.deco_stamp("banner", 17, 15)
    m.flag(28, 13); m.flag(36, 13)
    # 장터: 네 문명 좌판 (북쪽 모피·호박, 남쪽 향신료, 동쪽 비단, 서쪽 유리)
    m.deco_stamp("stall", 25, 20); m.deco_stamp("stall", 36, 20)
    m.deco_stamp("stall", 25, 26); m.deco_stamp("stall", 36, 26)
    m.deco_stamp("pelt", 29, 21); m.deco_stamp("crate_goods", 34, 22); m.deco_stamp("potion", 29, 27)
    m.deco_stamp("sack_big", 40, 22); m.deco_stamp("basket_apple", 24, 23); m.deco_stamp("jar_red", 34, 27)
    m.deco_stamp("lamp", 22, 19); m.deco_stamp("lamp", 42, 19); m.deco_stamp("lamp", 22, 28); m.deco_stamp("lamp", 42, 28)
    m.add("market", 32, 24, town="north_post")
    m.board(39, 18, "north_post")
    # 순록 마구간: 울타리 우리, 건초, 헛간
    m.fence(44, 31, 56, 40, gap=(48, 31)); m.reserve(45, 32, 55, 39)
    m.stamp("h_shed", 52, 32, force=True)
    m.deco_stamp("haybale", 45, 37); m.deco_stamp("bucket", 49, 38)
    m.critters("reindeer", (46, 33, 51, 36), 2)
    m.road_path([(48, 30), (45, 26)], width=1)
    # 서쪽 상인 천막촌 (먼 길 온 상인들이 묵는 곳)
    m.deco_stamp("tent", 8, 32); m.deco_stamp("tent", 14, 34); m.deco_stamp("fire", 12, 30)
    m.deco_stamp("bedroll", 18, 31); m.deco_stamp("box", 7, 36); m.deco_stamp("bigbarrel", 18, 36)
    m.road_path([(12, 29), (20, 26)], width=1)

    m.sign(27, 4, "↑ 눈 녹는 고갯길 · 북유럽\n↓ 북문 큰길 · 중앙 도시")
    m.sign(5, 20, "← 순록 썰매 정류장")
    m.sign(57, 19, "→ 룬돌 언덕 오솔길")
    m.talker(30, 15, "초소 대장", "north_wolf_viking_tall", "어서 와, 눈길 교역 초소야. 북쪽 물건이 남쪽으로 나가는 수출항이지. 모피랑 호박은 여기서 제일 비싸게 사 줘.",
             "대신 남쪽 소금은 여기서만 싸. 사냥꾼 마을에 가져가면 고기 절인다고 서로 달라고 할걸.")
    m.talker(15, 22, "여관 주인", "north_bear_granny_tall", "고개 넘느라 춥지? 꿀술 한 잔에 화롯가 자리는 공짜야.")
    m.talker(38, 30, "남쪽에서 온 향신료 상인", "north_p_trader_tall", "북쪽은 처음이라 털옷부터 샀어. 대신 후추는 여기서 금값이더군.")
    m.talker(10, 29, "천막 상인 아이", "north_p_boy_tall", "우리 아빠는 네 나라 말을 다 할 줄 알아!")
    m.walker([(33, 2), (32, 12), (31, 19), (29, 24), (34, 24), (32, 29), (31, 36), (31, 45)], idx=6)
    m.walker([(2, 24), (12, 24), (22, 24), (42, 25), (52, 24), (61, 23)], idx=7)
    m.critters("raven", (4, 38, 30, 44), 1)
    auto_fill(m, ["woodpile", "camp", "cargo"], max_n=4)

    m.dense_border("thaw")
    m.grove(52, 6, 4, 2.5, "thaw"); m.grove(8, 42, 4, 2.5, "green")
    m.scatter("pine", 4, area=(3, 3, 60, 44), margin=1)
    m.scatter("snowpine_g", 4, area=(3, 3, 60, 44), margin=1)
    m.finish()
    return m


def sled_station():
    """⑤ 순록 썰매 정류장: 넓은 눈밭과 썰매 트랙, 순록 우리, 썰매 창고. 트랙 출발대 = 썰매 언덕 미니게임(20냥, NorthMinigames.cs).
    (짐 싣는 썰매는 Q7 보상과 나중에 연결)"""
    m = ThawMap("north_c_sled", "순록 썰매 정류장", 60, 44, base="grass", cover=(0.55, 0.3), seed=113)
    m.hours = 1
    m.link("N", 12, 4, "north_c_west", "S", "N")
    m.link("E", 22, 4, "north_c_post", "W", "E")
    snow_trail(m, [(13, 0), (15, 5), (21, 10), (32, 16), (42, 21), (59, 23)])   # 눈밭 지름길 (길 없음: 걸어서 빠름, 수레는 느림)
    closed_exit(m, "W", 26, "← 서쪽 교역로\n(이웃 구역과 잇는 중)", road_from=(22, 27))
    m.road_path([(32, 16), (26, 22), (22, 27)], width=2)
    # 썰매 눈밭: 일부러 눈을 깔고, 둘레를 도는 썰매 길
    m.patch_area(6, 30, 40, 40)
    m.sled_track = [(8, 32), (20, 31), (36, 33), (37, 38), (20, 40), (8, 38), (8, 32)]
    m.road_path(m.sled_track, width=1)                 # 썰매 한 바퀴 트랙 (썰매 미니게임)
    # 순록 우리와 썰매 창고, 정류장 지기 집
    m.fence(40, 28, 54, 38, gap=(46, 28)); m.reserve(41, 29, 53, 37)
    m.critters("reindeer", (42, 30, 52, 36), 4)
    m.deco_stamp("haybale", 49, 34); m.deco_stamp("haybale_s", 42, 35); m.deco_stamp("bucket", 44, 30)
    m.stamp("h_shed", 4, 8, force=True); m.door_path(4, 8, 3, 3, 15, door_dx=1)
    m.stamp("h_turf", 38, 8, force=True); m.door_path(38, 8, 3, 2, 17, door_dx=1)
    m.road_path([(5, 15), (14, 15), (24, 15)], width=1)
    m.road_path([(39, 17), (34, 18)], width=1)
    m.deco_stamp("sled", 8, 12); m.deco_stamp("sled", 10, 14); m.deco_stamp("sled", 24, 34)
    m.deco_stamp("rope", 2, 12); m.deco_stamp("logs", 4, 12); m.deco_stamp("lantern", 13, 12)
    m.deco_stamp("totem", 36, 12); m.flag(30, 20)

    m.sign(16, 3, "↑ 서쪽 눈숲 갈림길\n→ 눈길 교역 초소")
    m.talker(19, 17, "썰매꾼", "north_p_herder_tall", "아래쪽 트랙 출발대에서 20냥 내면 언덕 썰매 한 판! 나무 피하고 눈송이 모으면 상금을 줘.",
             "통나무는 Space로 뛰어넘어. 내 최고 기록은 아직 아무도 못 깼지.",
             "짐 싣는 썰매는 순록 고원의 하르 님이 허락해야 빌려줄 수 있어.")
    m.talker(41, 18, "순록 돌보는 아이", "north_p_girl_tall", "이 순록은 루돌프 아니야. 이름은 '눈송이'야!")
    m.walker([(13, 2), (15, 6), (21, 11), (33, 17), (41, 22), (56, 23)], idx=8)
    m.critters("raven", (4, 16, 22, 26), 1)
    auto_fill(m, ["herd", "woodpile", "cargo", "camp", "cairn"], talks=[2])

    m.dense_border("thaw")
    m.grove(52, 8, 4, 3, "thaw"); m.grove(6, 20, 3, 3, "pine")
    m.scatter("snowpine_g", 4, area=(3, 3, 56, 28), margin=1)
    m.scatter("pine", 3, area=(3, 3, 56, 28), margin=1)
    m.finish()
    return m


def rune_path():
    """⑥ 룬돌 언덕 오솔길: 낮은 언덕을 감아 오르는 길, 이끼 낀 옛 담, 작은 룬돌 둥지. 숨은 룬돌 하나 (7개 수집 중)."""
    m = ThawMap("north_c_rune", "룬돌 언덕 오솔길", 60, 44, base="grass", cover=(0.5, 0.22), seed=127)
    m.hours = 1
    m.link("N", 33, 4, "north_c_east", "S", "N")
    m.link("W", 22, 4, "north_c_post", "E", "W")
    m.road_path([(34, 0), (37, 5), (32, 10), (26, 13), (22, 20)], width=3, grade=3)    # 개울 큰길 이어짐 (수레 편함)
    m.road_path([(22, 20), (14, 25), (6, 22), (0, 23)], width=3, grade=3)
    closed_exit(m, "E", 26, "→ 동쪽 교역로\n(이웃 구역과 잇는 중)", road_from=(30, 26))
    m.road_path([(22, 20), (30, 26)], width=2)
    # 언덕 위 룬돌 둥지 (작은 원)
    m.road_blob(42, 12, 5, 3.2, grade=1)
    m.road_path([(33, 8), (37, 11)], width=1)
    for i, (rx, ry) in enumerate([(38, 10), (42, 8), (46, 10), (46, 14), (42, 16), (38, 14)]):
        m.deco_stamp("runestone_b" if i % 2 else "runestone", rx, ry)
    m.deco_stamp("picturestone", 41, 11)       # 가운데: 배·태양 무늬 큰 그림돌
    m.deco_stamp("ruinwall", 50, 6); m.deco_stamp("ruinwall", 6, 30)
    m.deco_stamp("crys_b", 52, 14); m.deco_stamp("crys_p", 53, 15)
    # 숨은 룬돌: 남쪽 숲 틈 (길 없이 나무 사이로 들어가야 함)
    m.secret_rune(1, 12, 36); m.deco_stamp("gem_b", 14, 38)
    m.patch_area(9, 35, 16, 40)

    m.sign(36, 5, "↑ 동쪽 개울 갈림길\n← 눈길 교역 초소")
    m.sign(30, 10, "↗ 룬돌 둥지\n옛 사람들이 길을 비는 곳")
    m.talker(44, 12, "룬 읽는 노인", "north_owl_elder_tall", "이 돌들은 길 떠나는 상인을 지켜 줬지. 북쪽 땅엔 이런 돌이 일곱 개 숨어 있다네.",
             "하나는 이 언덕 남쪽, 나무 사이에 숨어 있지. 눈 녹은 자리를 잘 보게.")
    m.talker(20, 26, "약초 캐는 사람", "north_fox_woman_tall", "눈이 녹은 자리엔 봄 약초가 먼저 올라와.")
    m.walker([(34, 2), (33, 9), (24, 15), (22, 21), (12, 23), (3, 24)], idx=9)
    m.event(24, 18)
    m.critters("raven", (32, 26, 54, 36), 2)
    auto_fill(m, ["stones", "shrine", "crystal", "herd", "cairn", "camp"], talks=[2])

    m.dense_border("thaw")
    m.grove(50, 30, 6, 4, "green"); m.grove(12, 9, 5, 3, "thaw")
    m.scatter("tree", 4, area=(3, 26, 56, 40), margin=1)
    m.scatter("snowpine_g", 4, area=(3, 3, 56, 26), margin=1)
    m.finish()
    return m


def north_gate():
    """⑦ 북문 큰길: 중앙 도시 북문까지 이어지는 넓은 큰길. 큰 강과 돌다리, 네 문명 상단의 야영지, 북유럽식 장식 기둥.
    아래쪽 끝(중앙 도시 북문)은 공동 구역과 합칠 때 연다."""
    m = ThawMap("north_c_gate", "북문 큰길", 72, 71, base="grass", cover=(0.2, 0.0), seed=131)
    m.hours = 1
    m.link("N", 34, 4, "north_c_post", "S", "N")
    main = [(35, 0), (35, 8), (30, 16), (31, 24), (38, 31), (37, 40), (36, 52), (36, 62), (36, 70)]
    m.road_path(main, width=3, grade=3)
    # 큰 강과 돌다리
    m.river_h(44, 47, wiggle=2, fixed=range(30, 44))
    m.bridge(34, 43, 38, 48)
    closed_exit(m, "S", 35, "↓ 중앙 도시 북문\n(성문 공사 중, 곧 열림)")
    # 북유럽식 장식 기둥과 등불이 줄지은 큰길 (도시에 가까울수록)
    for y in (54, 60, 66):
        m.deco_stamp("totem", 33, y); m.deco_stamp("totem2", 40, y)
    for y in (57, 63):
        m.deco_stamp("lamp", 33, y); m.deco_stamp("lamp", 40, y)
    m.flag(32, 50); m.flag(41, 50)
    # 상단 야영지 (강 건너기 전 쉬어 가는 곳)
    m.deco_stamp("tent", 12, 26); m.deco_stamp("tent", 18, 28); m.deco_stamp("fire", 16, 32)
    m.deco_stamp("logbench", 13, 34); m.deco_stamp("bigbarrel", 22, 31); m.deco_stamp("crate", 21, 34)
    m.deco_stamp("sack", 10, 31); m.deco_stamp("spit", 19, 34)
    m.road_path([(17, 36), (30, 30)], width=1)
    # 옛 이정표 돌무더기와 버려진 수레 바퀴 자리
    m.deco_stamp("runestone", 44, 12); m.deco_stamp("rockbig", 52, 18); m.deco_stamp("rock", 22, 58)
    m.stamp("h_stoneA", 52, 56, force=True); m.door_path(52, 56, 2, 2, 60, door_dx=0)
    m.road_path([(52, 60), (40, 60)], width=1)
    m.deco_stamp("firewood", 55, 56); m.deco_stamp("bucket", 55, 57)
    # 고려·베네치아 쪽 이웃 구역과 잇는 자리 (막아 둠)
    closed_exit(m, "W", 58, "← 서쪽 교역로\n(이웃 구역과 잇는 중)", road_from=(33, 59))
    closed_exit(m, "E", 64, "→ 동쪽 교역로\n(이웃 구역과 잇는 중)", road_from=(40, 65))

    m.sign(38, 4, "↑ 눈길 교역 초소 · 북유럽\n↓ 중앙 도시")
    m.sign(40, 40, "큰 강 돌다리\n수레는 한 대씩 건너시오")
    m.talker(15, 30, "상단 우두머리", "north_p_trader_tall", "네 나라 상단이 다 이 강가에서 하룻밤 묵고 도시로 들어가지.",
             "북쪽 모피, 동쪽 비단, 남쪽 향신료, 서쪽 유리… 여기서 다 만나.")
    m.talker(42, 66, "북문 문지기", "north_wolf_viking_tall", "성문은 아직 공사 중이야. 곧 네 나라 길이 전부 여기로 모일 거야.")
    m.talker(50, 60, "돌집 할머니", "north_p_granny_tall", "젊었을 땐 북쪽에서 살았지. 이젠 눈 대신 강물 소리가 좋아.")
    m.walker([(35, 2), (35, 9), (30, 17), (32, 25), (38, 32), (37, 41), (36, 53), (36, 66)], idx=10)
    m.event(31, 20); m.event(37, 36); m.event(36, 56)
    m.critters("raven", (46, 22, 66, 36), 2); m.critters("gull", (10, 44, 30, 47), 2)
    m.fishing.append((22, 45))                          # 큰 강가 낚시 자리 (얼음 없이 강물)
    auto_fill(m, ["camp", "cairn", "herd", "shrine", "cargo", "stones", "woodpile", "hunt"], talks=[3, 7, 1])

    m.dense_border("green")
    m.grove(56, 28, 7, 5, "green"); m.grove(12, 56, 7, 5, "green"); m.grove(64, 16, 3, 2.5, "thaw"); m.grove(10, 12, 5, 3.5, "thaw")
    m.scatter("pine", 6, area=(3, 3, 68, 67), margin=1)
    m.scatter("tree", 8, area=(3, 3, 68, 67), margin=1)
    m.scatter("roundtree", 4, area=(3, 20, 68, 67), margin=1)
    m.scatter("snowpine_g", 4, area=(3, 3, 68, 20), margin=1)
    m.finish()
    return m


MAPS = [thaw_road, west_fork, east_fork, trade_post, sled_station, rune_path, north_gate]
