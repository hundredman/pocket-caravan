"""베네치아 최소 교역 동선 (제안 초안). 키오자 → 석호 둑길 → 리알토.
지명은 실제 지명을 빌린 게임 각색이다. 인물·대사는 창작이며 실제 전승이 아니다.
등록: Assets/PocketCaravan/World/Regions/venice.json. 내보내기: export_venice.py
운하는 물 타일(통행 불가)과 다리로 표현한다. 배 이동 기능은 없다.
다른 지역으로 가는 출구는 연결표 합의 전까지 만들지 않는다."""
from tiles import Map, K, PROPS


def chioggia():
    """키오자: 석호 남쪽 염전 마을. 베네치아 시작 마을. 소금 산지."""
    m = Map("venice_chioggia", "키오자", 40, 28, seed=201)
    # 남쪽 석호와 선착장
    m.river_h(22, 27, wiggle=1, fixed=range(13, 23))
    # 마당과 길: 선착장 → 마당 → 동쪽 석호 둑길
    m.road_blob(20, 13, 6.5, 2.5, grade=2)
    m.road_path([(26, 13), (39, 13)], width=3, grade=2)
    m.link("E", 12, 4, "venice_lagoon_road", "W", "E")
    m.road_path([(17, 15), (17, 19)], width=2, grade=2)
    m.bridge(16, 19, 18, 24, dock=True)
    m.stamp("boat", 19, 23, force=True, over_road=True)
    # 서쪽 염전: 얕은 소금물 웅덩이 (통행 불가)와 소금 자루
    m.road_path([(14, 13), (5, 13)], width=2, grade=1)
    m.road_path([(9, 13), (9, 16)], width=2, grade=1)
    m.water_rect(3, 17, 7, 19)
    m.water_rect(11, 17, 14, 19)
    for (px, py) in [(8, 17), (8, 18)]:
        m.prop(PROPS[2], px, py)
    m.sign(6, 16, "키오자 염전\n볕에 말린 소금을 자루에 담아 장터로 옮긴다.")
    # 집과 소금 창고
    for name, hx, hy in [("house_plain", 4, 4), ("house3", 12, 3), ("store", 27, 4), ("house_plain", 33, 6)]:
        m.stamp(name, hx, hy, force=True)
    m.road_path([(5, 7), (5, 13)], width=1, grade=1)
    m.road_path([(13, 6), (13, 13)], width=1, grade=1)
    m.road_path([(28, 7), (28, 13)], width=1, grade=1)
    m.road_path([(34, 9), (34, 13)], width=1, grade=1)
    m.stamp("cart", 23, 17, force=True)
    for px, py, prop in [(31, 8, 0), (32, 8, 1), (25, 17, 0)]:
        m.prop(PROPS[prop], px, py)
    # 사람과 시설
    m.add("market", 22, 15, town="venice_chioggia")
    m.board(25, 11, "venice_chioggia")
    m.talker(11, 16, "염전 일꾼", "villager_dog_tall",
             "여기 소금은 리알토 장터로 많이 실려 가지.",
             "자루가 무거우니 한꺼번에 많이 지면 걸음이 느려져.")
    m.talker(20, 20, "뱃사공", "villager_bear_tall",
             "배를 타는 일은 아직 없어. 리알토까지는 동쪽 둑길로 걸어가게.")
    m.sign(30, 11, "→ 석호 둑길\n리알토 장터로 이어진다.")
    m.add("spawn", 20, 13, id="S0")   # 베네치아 씬 직접 시험용 시작 자리
    m.add("ambience", 17, 24, id="river", radius=12)
    m.reeds(21, 0, 13, 0.5); m.reeds(21, 22, 39, 0.5)
    # 숲
    m.forest_border(thickness=2, big=("roundbig", "fruitbig"))
    m.flowers(2, 2, 37, 19, 12)
    m.finish()
    return m


def lagoon_road():
    """석호 둑길: 운하 두 줄기를 건너는 길. 남쪽 큰길(넓은 다리, 돌아감)과 둑 샛길(좁은 목교, 짧음)."""
    m = Map("venice_lagoon_road", "석호 둑길", 54, 32, seed=202)
    m.hours = 2
    fixed = (*range(11, 17), *range(20, 27))
    m.river_v(17, 19, wiggle=1, fixed_rows=fixed)
    m.river_v(35, 37, wiggle=1, fixed_rows=fixed)
    m.link("W", 12, 4, "venice_chioggia", "E", "W")
    m.link("E", 12, 4, "venice_rialto", "W", "E")
    m.road_path([(0, 13), (6, 13)], width=3, grade=3)
    m.road_path([(47, 13), (53, 13)], width=3, grade=3)
    # 둑 샛길: 짧지만 좁고 노면이 거칠다
    m.road_path([(6, 13), (47, 13)], width=2, grade=1)
    # 큰길: 남쪽으로 돌아가지만 넓고 고르다
    m.road_path([(6, 13), (6, 22), (47, 22), (47, 13)], width=3, grade=3)
    m.bridge(16, 13, 20, 14)
    m.bridge(34, 13, 38, 14)
    for by in (13, 14):
        for bx in (*range(16, 21), *range(34, 39)):
            m.road[by][bx] = 1
    m.bridge(16, 22, 20, 24)
    m.bridge(34, 22, 38, 24)
    # 큰길 쉼터
    m.stamp("house_plain", 24, 26, force=True)
    m.stamp("bench", 29, 26, force=True)
    m.talker(27, 25, "쉼터 주인", "villager_tanuki_tall",
             "짐수레라면 이 큰길로 가시게. 다리가 넓어 바퀴가 잘 굴러.",
             "등짐이 가벼우면 북쪽 둑길이 짧지.")
    m.sign(3, 10, "석호 둑길\n곧장: 좁은 둑 샛길 / 남쪽: 넓은 큰길.\n같은 짐으로 두 길을 비교해 보자.")
    m.sign(49, 10, "← 키오자 / → 리알토 장터")
    m.add("ambience", 18, 18, id="river", radius=10)
    m.add("ambience", 36, 18, id="river", radius=10)
    m.forest_border(thickness=2, big=("roundbig", "fruitbig"))
    m.scatter("roundtree", 6, area=(3, 3, 48, 28), margin=1)
    m.flowers(3, 3, 48, 28, 14)
    m.finish()
    return m


def rialto():
    """리알토 장터: 큰 운하를 사이에 둔 수도 장터. 목조 다리로 건넌다. 유리 공방."""
    m = Map("venice_rialto", "리알토 장터", 46, 32, seed=203, theme="town")
    m.river_v(20, 24, wiggle=0)                       # 곧은 큰 운하
    m.link("W", 12, 4, "venice_lagoon_road", "E", "W")
    m.road_path([(0, 13), (19, 13)], width=3, grade=3)
    m.road_path([(25, 13), (30, 13)], width=3, grade=3)
    m.bridge(19, 13, 25, 15)                          # 리알토 목조 다리
    m.road_rect(27, 11, 41, 17, grade=2)              # 네모난 장터 광장
    # 서쪽 강변: 창고와 집
    for name, hx, hy in [("house_plain", 3, 4), ("house3", 11, 4)]:
        m.stamp(name, hx, hy, force=True)
        m.road_path([(hx + 1, hy + 3), (hx + 1, 13)], width=2, grade=1)
    m.road_path([(15, 15), (15, 24), (18, 24)], width=2, grade=1)
    m.bridge(18, 23, 21, 26, dock=True)               # 운하 선착장
    m.stamp("boat", 22, 26, force=True, over_road=True)
    # 동쪽: 유리 공방과 상점
    for name, hx, hy in [("store", 28, 4), ("house3", 37, 4), ("house_plain", 40, 21)]:
        m.stamp(name, hx, hy, force=True)
    m.road_path([(29, 7), (29, 11)], width=2, grade=1)
    m.road_path([(38, 7), (38, 11)], width=2, grade=1)
    for px, py, prop in [(32, 7, 0), (33, 7, 1), (26, 18, 2), (27, 18, 0)]:
        m.prop(PROPS[prop], px, py)
    m.stamp("cart", 42, 15, force=True)
    # 사람과 시설
    m.add("market", 34, 16, town="venice_rialto")
    m.board(38, 12, "venice_rialto")
    m.talker(31, 9, "유리 장인", "villager_fox_tall",
             "공방에서 갓 나온 유리그릇이야. 가볍지만 값이 나가지.",
             "키오자 사람들도 좋은 그릇을 찾더군.")
    m.talker(17, 22, "뱃사공", "villager_bear_tall",
             "운하는 배로 다니지만 자네는 다리로 건너게.",
             "다리를 건너면 바로 장터 광장이야.")
    m.sign(3, 10, "← 석호 둑길\n키오자 염전으로 돌아간다.")
    m.sign(27, 10, "리알토 장터\n운하 건너 서쪽은 석호 둑길.")
    m.add("ambience", 22, 20, id="river", radius=12)
    m.forest_border(thickness=2, big=("roundbig", "fruitbig"))
    m.finish()
    return m
