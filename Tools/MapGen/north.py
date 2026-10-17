"""북유럽 지역 맵 설계. 고려(maps.py)와 따로 두고, 합칠 때 출구만 잇는다.
맵 id는 소문자 north_ 접두어 (팀 규칙: Docs/TeamIntegrationContract.md). 씬 이름 = 맵 id.
등록: Assets/PocketCaravan/World/Regions/north.json"""
import json, os, sys
from tiles import K, PROPS
from north_tiles import SnowMap

OUT = sys.argv[1] if len(sys.argv) > 1 else "out_north"


def birka():
    """비르카: 피오르 물가의 교역 마을. 큰 회관(여관) 앞 마당을 중심으로 집 몇 채와 선착장. 북유럽 시작 마을."""
    m = SnowMap("north_birka", "비르카", 40, 28, seed=31)
    # 남쪽 피오르 물길과 선착장
    m.river_h(22, 27, wiggle=2, fixed=range(15, 23))
    m.bridge(18, 19, 20, 24)
    m.stamp("boat", 21, 23, force=True, over_road=True)

    # 마당과 길: 회관 앞 마당 → 선착장, 동쪽 피오르 길, 서쪽 관문(닫힘)
    m.road_blob(20, 12.5, 6.5, 2.6, grade=1)
    m.road_path([(19, 15), (19, 19)], width=2)
    m.road_path([(26, 13), (33, 13), (39, 13)], width=2)
    m.link("E", 12, 4, "north_fjord_road", "W", "E")
    m.road_path([(0, 13), (7, 13), (13, 13)], width=2)
    m.reserve(0, 12, 2, 15)

    # 큰 회관(여관)과 집
    m.stamp("longhall", 18, 1, force=True)
    m.door_path(18, 1, 4, 7, 10, door_dx=1)
    m.stamp("woodhouse", 5, 3, force=True)
    m.door_path(5, 3, 4, 5, 11, door_dx=1)
    m.road_path([(6, 11), (13, 12)], width=1)
    m.stamp("woodhouse", 29, 2, force=True)
    m.door_path(29, 2, 4, 5, 10, door_dx=1)
    m.road_path([(30, 10), (26, 12)], width=1)
    m.stamp("house3", 9, 16, force=True)
    m.door_path(9, 16, 3, 3, 19, door_dx=1)
    m.road_path([(10, 19), (18, 19)], width=1)

    # 장터 한쪽: 모피 말리는 틀, 통과 상자
    m.stamp("rack", 28, 16, force=True)
    m.prop(K("element", 3, 0), 26, 16); m.prop(K("element", 0, 1), 27, 16); m.prop(K("element", 1, 1), 25, 17)
    m.board(25, 10, "north_birka")

    # 닫힌 서쪽 관문: 통으로 막아 둔다 (합칠 때 출구로 바꿈)
    for (bx, by) in [(0, 13), (0, 14), (1, 13), (1, 14)]:
        m.prop(K("element", 3, 0), bx, by)
    m.sign(3, 11, "← 남쪽 관문\n(아직 닫혀 있음)")
    m.sign(31, 11, "→ 피오르 길\n모피 사냥꾼 마을 · 피오르 나루")

    # 사람
    m.add("npc", 21, 9, npc="north_innkeeper")     # 여관 할머니: 회관 앞 (Q2, Q5)
    m.add("npc", 8, 9, npc="north_husband")        # 집 보는 남편: 서쪽 집 앞 (Q1)
    m.add("npc", 13, 18, npc="north_har")          # 목수 하르: 남쪽 작은 집 앞 (Q7)
    m.add("npc", 15, 11, npc="north_bold")         # 욕심쟁이 형 볼드: 마당 (Q6)
    m.add("market", 24, 15, town="north_birka")
    m.talker(17, 18, "뱃사공", "villager_bear_tall", "피오르가 꽁꽁 얼어서 배가 묶였어. 얼음이 녹을 때까지는 걸어서 다녀야지.", "얼음이 녹으면 이 배로 나루 마을까지 금방이야.")
    m.talker(8, 10, "사냥꾼", "villager_fox_tall", "숲 너머 사냥꾼 마을에 가면 모피가 싸. 여기서는 값을 두 배는 쳐주지.",
             "길 없는 눈밭이 무섭다고? 우리 북쪽 사람들은 숲을 가로질러 다니는 게 더 빨라.")
    m.add("spawn", 20, 13, id="S0")

    # 숲과 눈
    # 가장자리: 꽉 찬 둥근 눈나무 (세모난 전나무는 겹치면 빈칸이 보여서 안쪽에만 띄엄띄엄)
    m.forest_border(thickness=2, kinds=("snowround",), big=("snowroundbig",))
    m.scatter("snowpine", 3, area=(3, 2, 36, 18), margin=1)
    m.scatter("snowpine_g", 2, area=(3, 2, 36, 18), margin=1)
    m.scatter("snowboulder", 2, area=(3, 2, 36, 18), margin=1)
    m.snow_props([(4, 9), (12, 8), (27, 8), (34, 9), (3, 19), (14, 17), (35, 18), (24, 4), (15, 4)])
    m.finish()
    return m


def fjord_road():
    """피오르 길: 세 마을을 잇는 길. 갈림길에서 큰길(트롤 다리를 건너 돌아감)과 숲 지름길(길 없음)이 나뉜다.
    바이킹은 길 없는 곳에서 빨라 지름길이 유리하고, 손수레는 큰길이 낫다."""
    m = SnowMap("north_fjord_road", "피오르 길", 54, 34, seed=37)
    m.hours = 3
    # 서쪽 비르카에서 들어와 갈림길까지
    m.road_path([(0, 13), (10, 13), (18, 14), (24, 14)], width=2)
    m.link("W", 12, 4, "north_birka", "E", "W")
    # 큰길: 북쪽으로 돌아 얼어붙은 개울의 트롤 다리를 건넌다
    m.water_rect(36, 0, 38, 7)
    m.water_blob(37, 10, 3.4, 2.3)
    m.road_path([(24, 14), (28, 10), (32, 6), (42, 6), (47, 10), (50, 13), (53, 13)], width=2, grade=2)
    m.bridge(35, 6, 39, 7)
    m.link("E", 12, 4, "north_hunter", "W", "E")
    # 숲 지름길: 길은 없지만 나무가 비어 있는 눈밭 (갈림길에서 동쪽으로 곧장)
    m.reserve(26, 14, 49, 18)
    # 남쪽 갈래: 피오르 나루 마을
    m.road_path([(24, 14), (25, 20), (24, 27), (24, 33)], width=2)
    m.link("S", 23, 4, "north_naru", "N", "S")

    # 갈림길 이정표와 길손
    m.sign(21, 11, "↗ 큰길: 다리 건너 사냥꾼 마을\n→ 숲길: 길 없음, 가깝지만 수레는 힘듦\n↓ 피오르 나루 마을")
    m.sign(6, 16, "← 비르카")
    m.talker(28, 15, "길손", "villager_dog_tall", "다리 밑에 트롤 패거리가 산다더군. 통행료를 내거나 냅다 달려야 해.",
             "숲으로 가로질러 가면 다리를 안 건너도 돼. 대신 수레는 눈에 빠지지.")
    m.talker(41, 9, "얼음 낚시꾼", "villager_bear_tall", "개울이 얼면 물고기가 굼떠져. 오늘 저녁은 생선 수프야.")
    # 트롤 패거리 (도적 기능을 그대로 쓴다. 그림은 나중에 트롤로)
    m.bandit(37, 3, radius=5)
    for (ex, ey) in [(12, 14), (25, 24), (46, 9)]:
        m.event(ex, ey)
    m.walker([(2, 14), (10, 14), (18, 15), (24, 15), (25, 21), (24, 31)], idx=0)
    m.walker([(26, 13), (32, 7), (42, 7), (50, 14), (52, 14)], idx=1)

    m.forest_border(thickness=2, kinds=("snowround",), big=("snowroundbig",))
    m.scatter("snowpine", 8, area=(3, 2, 50, 31), margin=1)
    m.scatter("snowpine_g", 6, area=(3, 2, 50, 31), margin=1)
    m.scatter("snowround", 10, area=(3, 2, 50, 31), margin=1)
    m.scatter("snowboulder", 3, area=(3, 2, 50, 31), margin=1)
    m.snow_props([(8, 6), (15, 20), (30, 25), (44, 22), (48, 28), (12, 27), (33, 12), (45, 15), (40, 17)])
    m.finish()
    return m


def hunter():
    """모피 사냥꾼 마을: 숲속 오두막 마을. 모피·말린 고기 산지. 남쪽 들판(Q1 아내), 동쪽 염소 목장(Q5)."""
    m = SnowMap("north_hunter", "모피 사냥꾼 마을", 36, 24, seed=41)
    m.road_path([(0, 11), (6, 11), (11, 12)], width=2, grade=2)
    m.link("W", 10, 4, "north_fjord_road", "E", "W")
    m.road_blob(17, 11.5, 6, 2.4, grade=1)

    m.stamp("woodhouse", 6, 2, force=True); m.door_path(6, 2, 4, 5, 10, door_dx=1)
    m.stamp("house3", 15, 3, force=True); m.door_path(15, 3, 3, 3, 9, door_dx=1)
    m.stamp("woodhouse", 24, 2, force=True); m.door_path(24, 2, 4, 5, 10, door_dx=1)
    m.road_path([(25, 10), (22, 11)], width=1)
    # 가죽 말리는 틀과 장작
    m.stamp("rack", 4, 14, force=True); m.stamp("rack", 8, 14, force=True)
    m.prop(K("element", 3, 0), 12, 15); m.prop(K("element", 1, 1), 13, 15)
    # 남쪽 들판 (건초 더미)
    m.road_path([(15, 14), (15, 18)], width=1)
    for (hx, hy) in [(12, 19), (13, 19), (17, 19), (18, 20), (14, 21)]:
        m.stamp("hay", hx, hy, force=True)
    # 동쪽 염소 목장 (울타리 대신 통·상자로 테두리)
    m.road_path([(23, 12), (29, 12)], width=1)
    for (fx, fy) in [(28, 14), (29, 14), (30, 14), (31, 14), (32, 14), (28, 18), (29, 18), (30, 18), (31, 18), (32, 18), (28, 15), (28, 17), (32, 15), (32, 16), (32, 17)]:
        m.prop(K("element", 1, 1), fx, fy)
    m.stamp("hay", 30, 16, force=True)

    m.add("market", 19, 14, town="north_hunter")
    m.board(21, 9, "north_hunter")
    m.sign(3, 9, "← 피오르 길\n비르카 · 피오르 나루")
    m.talker(14, 9, "늙은 사냥꾼", "elder_tall", "여우 털은 겨울에 받아야 제값이지. 여기서 사면 싸고, 바닷가에 가져가면 비싸.",
             "모피는 가볍지만 비싸서, 도둑맞으면 속이 쓰리다네.")
    m.add("npc", 30, 17, npc="north_goats")        # 염소 맏형: 목장 안 (Q5)
    m.add("npc", 16, 20, npc="north_wife")         # 들일 하는 아내: 건초 들판 (Q1)
    m.talker(29, 12, "염소 치는 아이", "villager_tanuki_tall", "우리 염소들은 소금 덩이를 좋아해. 다리 건너 오는 상인이 가져다주곤 했는데…")

    m.forest_border(thickness=2, kinds=("snowround",), big=("snowroundbig",))
    m.scatter("snowpine", 4, area=(3, 2, 32, 20), margin=1)
    m.scatter("snowpine_g", 3, area=(3, 2, 32, 20), margin=1)
    m.snow_props([(11, 7), (21, 6), (3, 18), (25, 20), (33, 9)])
    m.finish()
    return m


def naru():
    """피오르 나루 마을: 피오르 물가의 작은 항구. 배가 모이는 곳이라 모피를 비싸게 산다. 톰테 자리(Q3)."""
    m = SnowMap("north_naru", "피오르 나루 마을", 40, 28, seed=43)
    m.road_path([(19, 0), (19, 6), (20, 10)], width=2)
    m.link("N", 18, 4, "north_fjord_road", "S", "N")
    m.road_blob(20, 12, 7, 2.6, grade=1)
    # 남쪽 피오르와 선착장 두 개
    m.river_h(20, 27, wiggle=2, fixed=range(8, 32))
    m.road_path([(12, 14), (12, 17)], width=2)
    m.bridge(11, 18, 13, 24)
    m.road_path([(28, 14), (28, 17)], width=2)
    m.bridge(27, 18, 29, 24)
    m.stamp("boat", 14, 22, force=True, over_road=True)
    m.stamp("boat", 30, 21, force=True, over_road=True)

    # 창고·집
    m.stamp("longhall", 3, 3, force=True); m.door_path(3, 3, 4, 7, 11, door_dx=1)
    m.road_path([(4, 11), (13, 12)], width=1)
    m.stamp("woodhouse", 29, 2, force=True); m.door_path(29, 2, 4, 5, 10, door_dx=1)
    m.road_path([(30, 10), (27, 11)], width=1)
    m.stamp("house3", 23, 3, force=True); m.door_path(23, 3, 3, 3, 9, door_dx=1)
    # 꿀술 창고 앞 통
    for (bx, by) in [(8, 11), (9, 11), (8, 12)]:
        m.prop(K("element", 3, 0), bx, by)
    m.prop(K("element", 0, 1), 16, 16); m.prop(K("element", 1, 1), 17, 16)

    m.add("market", 21, 15, town="north_naru")
    m.board(24, 9, "north_naru")
    m.sign(17, 3, "↑ 피오르 길\n비르카 · 사냥꾼 마을")
    m.talker(13, 16, "선장", "villager_bear_tall", "남쪽 바다로 나가는 배는 모피를 잔뜩 실어 가. 그래서 여기선 모피가 금값이지.")
    m.add("npc", 6, 12, npc="north_warehouse")     # 창고지기 (Q3 납품처)
    m.talker(7, 9, "톰테 할아버지", "elder_tall", "흠흠. 예의 바른 손님은 언제나 환영이지.")   # Q3 정직 시험 (NorthQuestDirector가 바꿔 끼움)
    m.talker(29, 16, "그물 깁는 사람", "villager_fox_tall", "얼음 사이로 그물을 내리면 대구가 올라와.")

    m.forest_border(thickness=2, kinds=("snowround",), big=("snowroundbig",))
    m.scatter("snowpine", 3, area=(3, 2, 36, 16), margin=1)
    m.scatter("snowboulder", 1, area=(3, 2, 36, 16), margin=1)
    m.snow_props([(15, 6), (34, 12), (4, 15), (36, 17), (26, 7)])
    m.finish()
    return m


MAPS = [birka, fjord_road, hunter, naru]


if __name__ == "__main__":  # 미리보기만 (내보내기는 export_north.py)
    os.makedirs(OUT, exist_ok=True)
    for f in MAPS:
        m = f()
        with open(os.path.join(OUT, m.id + ".json"), "w") as fp:
            json.dump(m.to_json(), fp, ensure_ascii=False)
        m.render(os.path.join(OUT, m.id + ".png"), scale=2)
        print(m.id, m.w, m.h)
