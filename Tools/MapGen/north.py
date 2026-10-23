"""북유럽 지역 맵 설계. 고려(maps.py)와 따로 두고, 합칠 때 출구만 잇는다.
맵 id는 소문자 north_ 접두어 (팀 규칙: Docs/TeamIntegrationContract.md). 씬 이름 = 맵 id.
등록: Assets/PocketCaravan/World/Regions/north.json
10/23 개편: 마을마다 광장 중심으로 배치. 북유럽 전체는 1만 칸 이내 (FitMap: 설계 좌표를 실제 크기에 비율대로 맞춤)."""
import json, os, sys
from tiles import K, PROPS
import north_tiles
from north_tiles import SnowMap, FitMap

OUT = sys.argv[1] if len(sys.argv) > 1 else "out_north"


def birka():
    """비르카: 북유럽의 수도. 얄(왕)의 큰 회관과 룬돌 광장, 상인 길드, 훈련장, 장터 거리, 부두 세 개의 큰 항구.
    10/23: 고려(수도 개경 1 + 마을 4)와 규격을 맞추려고 시작 마을을 수도로 키웠다 (72x50)."""
    m = FitMap("north_birka", "비르카", 72, 50, seed=31)
    # 남쪽 피오르 항구
    m.river_h(41, 49, wiggle=2, fixed=range(14, 60))
    for dx in (20, 34, 48):
        m.road_path([(dx + 1, 36), (dx + 1, 39)], width=2)
        m.bridge(dx, 40, dx + 2, 47, dock=True)
    for (bx, by) in [(23, 44), (37, 43), (51, 45), (13, 46), (58, 44)]:
        m.stamp("boat", bx, by, force=True, over_road=True)

    # 큰길: 서쪽 옛 관문(닫힘) ─ 도성 가운데 ─ 동쪽 피오르 길
    m.road_path([(0, 25), (20, 25), (50, 25), (71, 25)], width=2, grade=2)
    m.link("E", 24, 4, "north_fjord_road", "W", "E")
    m.reserve(0, 22, 8, 29)
    # 얄의 회관 앞 룬돌 광장
    m.road_blob(35, 16, 10, 4, grade=1)
    m.road_path([(35, 19), (35, 25)], width=2)
    # 남쪽 장터 거리와 항구 길
    m.road_path([(14, 33), (35, 33), (56, 33)], width=2)
    m.road_path([(35, 26), (35, 33)], width=2)
    m.road_path([(21, 33), (21, 36)], width=2); m.road_path([(49, 33), (49, 36)], width=2)

    # 얄(왕)의 큰 회관: 회관 두 채를 나란히 (가운데 문이 정문)
    m.stamp("longhall", 31, 2, force=True); m.door_path(31, 2, 4, 7, 12, door_dx=1)
    m.stamp("longhall", 36, 2, force=True); m.door_path(36, 2, 4, 7, 12, door_dx=1)
    for bx in (30, 40):
        m.deco_stamp("banner", bx, 7)
    m.deco_stamp("totem", 29, 10); m.deco_stamp("totem2", 42, 10)
    m.deco_stamp("lamp", 28, 12); m.deco_stamp("lamp", 43, 12)
    # 광장: 큰 모닥불과 룬돌 고리, 통나무 의자
    m.deco_stamp("fire", 34, 15)
    for (rx, ry) in [(29, 15), (41, 15), (31, 13), (39, 13), (31, 18), (39, 18)]:
        m.deco_stamp("runestone", rx, ry)
    m.deco_stamp("logbench", 32, 18); m.deco_stamp("stump", 37, 17); m.deco_stamp("stump", 33, 13)
    m.flag(26, 19); m.flag(45, 19)

    # 서쪽 마을 구역: 집과 여관
    m.stamp("h_dark", 5, 4, force=True); m.door_path(5, 4, 4, 4, 11, door_dx=1)
    m.stamp("woodhouse", 12, 3, force=True); m.door_path(12, 3, 4, 5, 11, door_dx=1)
    m.stamp("h_dark", 19, 4, force=True); m.door_path(19, 4, 4, 4, 11, door_dx=1)      # 여관
    m.road_path([(6, 11), (20, 11), (26, 14)], width=1)
    m.stamp("h_shed", 6, 15, force=True); m.door_path(6, 15, 3, 3, 21, door_dx=1)
    m.stamp("woodhouse", 13, 15, force=True); m.door_path(13, 15, 4, 5, 22, door_dx=1)
    m.road_path([(7, 21), (14, 22), (24, 24)], width=1)
    m.stamp("snowroundbig", 21, 16, force=True); m.deco_stamp("runestone", 25, 19)   # 수호 나무
    # 동쪽 구역: 상인 길드 회관과 훈련장
    m.stamp("longhall", 50, 2, force=True); m.door_path(50, 2, 4, 7, 12, door_dx=1)    # 상인 길드
    m.deco_stamp("banner", 49, 7); m.deco_stamp("banner", 54, 7)
    m.stamp("h_dark", 57, 4, force=True); m.door_path(57, 4, 4, 4, 12, door_dx=1)
    m.stamp("h_shed", 63, 5, force=True); m.door_path(63, 5, 3, 3, 12, door_dx=1)
    m.road_path([(51, 12), (64, 12)], width=1); m.road_path([(51, 12), (45, 15)], width=1)
    m.fence(52, 15, 63, 21, gap=(56, 21))                                               # 훈련장
    m.deco_stamp("weaponrack", 54, 16); m.deco_stamp("weaponrack", 59, 16)
    m.deco_stamp("bigbarrel", 60, 19); m.deco_stamp("helm", 54, 19); m.deco_stamp("scarecrow", 57, 18)
    m.deco_stamp("fire", 66, 16); m.deco_stamp("firewood", 67, 19)

    # 남쪽 장터 거리: 길 양옆으로 좌판
    m.add("market", 36, 31, town="north_birka")
    for (sx, sy) in [(17, 29), (24, 29), (40, 29), (47, 29), (24, 35), (40, 35)]:
        m.deco_stamp("stall", sx, sy)
    for (k, x, y) in [("basket_veg", 17, 31), ("basket_apple", 19, 31), ("crate_berry", 25, 31), ("fishbasket", 26, 31),
                      ("sack", 41, 31), ("crate_goods", 42, 31), ("pelt", 47, 31), ("bigbarrel", 29, 30),
                      ("chest", 44, 31), ("potion", 49, 31), ("sack_big", 30, 36), ("barrel", 44, 37)]:
        m.deco_stamp(k, x, y)
    m.board(33, 30, "north_birka")
    m.stamp("h_dark", 6, 28, force=True); m.door_path(6, 28, 4, 4, 33, door_dx=1)
    m.road_path([(7, 33), (14, 33)], width=1)
    m.stamp("house3", 59, 29, force=True); m.door_path(59, 29, 3, 3, 33, door_dx=1)
    m.road_path([(60, 33), (56, 33)], width=1)
    # 항구: 보트 창고·짐·그물 말리는 틀
    m.stamp("h_shed", 60, 35, force=True); m.door_path(60, 35, 3, 3, 38, door_dx=1); m.road_path([(61, 38), (50, 37)], width=1)
    for (k, x, y) in [("rope", 18, 37), ("crate", 24, 38), ("box", 25, 38), ("bigbarrel", 27, 38), ("logs", 38, 38),
                      ("rope", 45, 37), ("sack", 52, 38), ("crate", 53, 38), ("lamp", 23, 38), ("lamp", 33, 38), ("lamp", 47, 38)]:
        m.deco_stamp(k, x, y)
    m.deco_stamp("rack", 14, 39); m.deco_stamp("rack", 54, 35)

    # 서쪽 관문: 국경 고개로 (고개 너머 남쪽이 중앙)
    m.link("W", 24, 4, "north_pass", "E", "W")
    m.sign(4, 23, "← 국경 고개\n남쪽 중앙 교역지로 가는 길")
    m.sign(66, 23, "→ 피오르 길\n모피 사냥꾼 마을 · 피오르 나루 · 온천")

    # 사람
    m.add("npc", 21, 12, npc="north_innkeeper")    # 여관 할머니: 서쪽 여관 앞 (Q2, Q5)
    m.add("npc", 7, 12, npc="north_husband")       # 집 보는 남편: 서쪽 집 앞 (Q1)
    m.add("npc", 8, 35, npc="north_har")           # 목수 하르: 남서쪽 집 앞 (Q7)
    m.add("npc", 28, 17, npc="north_bold")         # 욕심쟁이 형 볼드: 광장 (Q6)
    m.talker(35, 11, "얄 시그리드", "north_wolf_viking_tall", "비르카에 온 걸 환영하네. 이 도성은 바다와 숲의 물건이 모두 모이는 곳이지.",
             "정직한 상인에게는 회관 문이 늘 열려 있네. 피오르 너머 마을들과 거래를 넓혀 주게.")
    m.talker(52, 13, "상인 길드장", "north_lynx_tall", "길드 장부엔 모든 마을의 시세가 적혀 있지. 싸게 사서 먼 곳에 팔면 길드가 자네를 기억할 걸세.")
    m.talker(64, 24, "성문 경비", "north_bear_sailor_tall", "동쪽 문을 나서면 피오르 길이야. 밤엔 다리 밑 트롤을 조심하게.")
    m.talker(57, 20, "훈련하는 전사", "north_wolf_tall", "하나, 둘! 상인도 팔 힘이 있어야 짐을 지지.")
    m.talker(27, 38, "뱃사공", "north_bear_sailor_tall", "피오르가 꽁꽁 얼어서 배가 묶였어. 얼음이 녹을 때까지는 걸어서 다녀야지.", "얼음이 녹으면 이 배로 나루 마을까지 금방이야.")
    m.talker(16, 12, "사냥꾼", "north_wolf_tall", "숲 너머 사냥꾼 마을에 가면 모피가 싸. 여기서는 값을 두 배는 쳐주지.",
             "길 없는 눈밭도 걸어서 가면 지름길이야. 수레를 끌 땐 큰길로 돌아가는 게 낫고.")
    m.talker(36, 19, "불 쬐는 노인", "north_owl_elder_tall", "겨울밤엔 다들 회관 앞 불가에 모여 옛 항해 이야기를 하지.",
             "광장의 룬돌에 새긴 건 이 도성을 처음 연 상인들 이름이라네.")
    m.add("spawn", 37, 22, id="S0")
    m.walker([(10, 26), (22, 26), (35, 26), (50, 26), (64, 26), (50, 25), (35, 25), (20, 25)], idx=2)
    m.walker([(16, 34), (28, 34), (40, 34), (54, 34), (40, 33), (28, 33)], idx=7)
    m.critters("raven", (27, 13, 44, 19), 3); m.critters("gull", (14, 38, 58, 40), 4)

    # 가장자리 숲
    m.dense_border("pine")
    m.scatter("snowpine", 3, area=(3, 3, 68, 34), margin=1)
    m.scatter("snowboulder", 2, area=(3, 3, 68, 34), margin=1)
    m.snow_props([(3, 12), (27, 6), (45, 6), (66, 10), (4, 33), (66, 30)])
    m.finish()
    return m


def fjord_road():
    """피오르 길: 세 마을을 잇는 길. 갈림길에서 큰길(트롤 다리를 건너 돌아감)과 숲 지름길(길 없음)이 나뉜다.
    누구에게나 의미 있는 선택: 걸어서 가볍게 가면 지름길, 손수레에 많이 실었으면 큰길 (팀 결정 Docs/DesignUpdate_2026-10-17.md)."""
    m = FitMap("north_fjord_road", "피오르 길", 66, 42, seed=37)
    m.hours = 3

    # 서쪽 비르카에서 들어와 갈림길까지
    m.road_path([(0, 18), (12, 18), (22, 19), (30, 19)], width=2)
    m.link("W", 17, 4, "north_birka", "E", "W")
    # 큰길: 북쪽으로 돌아 얼어붙은 개울의 트롤 다리를 건넌다
    m.water_rect(45, 0, 47, 8)
    m.water_blob(46, 12, 4.2, 2.6)
    m.road_path([(30, 19), (34, 14), (40, 9), (52, 9), (58, 13), (62, 18), (65, 18)], width=2, grade=2)
    m.bridge(43, 9, 49, 10)
    m.link("E", 17, 4, "north_hunter", "W", "E")
    # 북쪽 갈래: 절벽과 얼음 개울 사이 골짜기로 올라가면 온천 마을
    m.road_path([(40, 9), (40, 4), (41, 0)], width=2)
    m.link("N", 39, 4, "north_laugar", "S", "N")
    # 숲 지름길: 길은 없지만 나무가 비어 있는 눈밭 (갈림길에서 동쪽으로 곧장)
    m.reserve(32, 19, 61, 23)
    # 남쪽 갈래: 피오르 나루 마을
    m.road_path([(30, 19), (31, 27), (30, 35), (30, 41)], width=2)
    m.link("S", 29, 4, "north_naru", "N", "S")

    # 서남쪽 얼어붙은 호수와 사냥꾼 야영지
    m.water_blob(12, 32, 6, 3.2)
    m.deco_stamp("tent", 18, 25); m.deco_stamp("fire", 22, 27); m.deco_stamp("logbench", 21, 25)
    m.deco_stamp("logs", 17, 29); m.deco_stamp("pelt", 23, 30)
    m.road_path([(22, 24), (25, 21)], width=1)

    # 갈림길 이정표와 길손
    m.sign(27, 15, "↗ 큰길: 다리 건너 사냥꾼 마을 (↑ 골짜기: 온천 마을)\n→ 숲길: 길 없음, 가깝지만 수레는 힘듦\n↓ 피오르 나루 마을")
    m.sign(6, 16, "← 비르카")
    m.talker(34, 24, "길손", "north_hare_tall", "다리 밑에 트롤 패거리가 산다더군. 통행료를 내거나 냅다 달려야 해.",
             "숲으로 가로질러 가면 다리를 안 건너도 돼. 대신 수레는 눈에 빠지지.")
    m.talker(51, 12, "얼음 낚시꾼", "north_bear_sailor_tall", "개울이 얼면 물고기가 굼떠져. 오늘 저녁은 생선 수프야.")
    m.talker(20, 28, "야영하는 사냥꾼", "north_wolf_tall", "호수가 얼면 건너편 숲까지 곧장 갈 수 있지. 그래도 얼음 가운데는 피하게.")
    # 트롤 패거리 (도적 기능을 그대로 쓴다. 그림은 나중에 트롤로). 다리 서쪽 물가
    m.bandit(41, 12, radius=5)
    for (ex, ey) in [(14, 19), (31, 30), (57, 11)]:
        m.event(ex, ey)
    m.walker([(2, 19), (12, 19), (22, 20), (30, 20), (31, 27), (30, 38)], idx=0)
    m.walker([(33, 15), (40, 10), (52, 10), (58, 14), (63, 19)], idx=1)
    m.critters("raven", (5, 10, 25, 16), 2); m.critters("raven", (34, 19, 58, 23), 2)
    m.deco_stamp("runestone", 33, 24); m.deco_stamp("snowpost", 60, 23)   # 숲길 입구·출구 표지 돌
    m.deco_stamp("logs", 9, 14); m.deco_stamp("stump", 11, 14)

    m.dense_border("mixed")
    m.grove(12, 7, 6, 3, "mixed"); m.grove(52, 30, 6, 4, "pine"); m.grove(57, 5, 4, 2.5, "mixed")
    m.scatter("snowpinebig", 3, area=(33, 25, 62, 38), margin=0)
    m.scatter("snowpine", 10, area=(33, 12, 44, 17), margin=0)
    m.scatter("snowpine_g", 8, area=(33, 25, 62, 31), margin=0)
    m.scatter("snowpine", 8, area=(3, 8, 62, 39), margin=1)
    m.scatter("snowpine_g", 6, area=(3, 8, 62, 39), margin=1)
    m.scatter("snowround", 10, area=(3, 8, 62, 39), margin=1)
    m.scatter("deadtree", 4, area=(3, 8, 62, 39), margin=1)
    m.scatter("snowboulder", 3, area=(3, 8, 62, 39), margin=1)
    m.snow_props([(8, 10), (15, 23), (38, 30), (55, 28), (60, 35), (14, 38), (40, 16), (55, 21), (47, 24)])
    m.finish()
    return m


def hunter():
    """모피 사냥꾼 마을: 숲속 오두막 마을. 모피·말린 고기 산지. 남서쪽 사냥꾼 야영지, 남쪽 들판(Q1 아내), 동쪽 염소 목장(Q5)."""
    m = FitMap("north_hunter", "모피 사냥꾼 마을", 46, 32, seed=41)
    m.road_path([(0, 15), (8, 15), (15, 16)], width=2, grade=2)
    m.link("W", 14, 4, "north_fjord_road", "E", "W")
    m.road_blob(23, 16, 7.5, 3, grade=1)

    m.stamp("h_turf", 8, 6, force=True); m.door_path(8, 6, 3, 2, 13, door_dx=1)       # 잔디 지붕 움집
    m.stamp("h_shed", 21, 5, force=True); m.door_path(21, 5, 3, 3, 13, door_dx=1)      # 가죽 손질 헛간
    m.stamp("h_turf", 33, 6, force=True); m.door_path(33, 6, 3, 2, 13, door_dx=1)
    m.road_path([(9, 13), (16, 15)], width=1)
    m.road_path([(33, 13), (29, 15)], width=1)
    m.deco_stamp("tent", 15, 5); m.deco_stamp("tent", 27, 5)
    m.deco_stamp("helm", 20, 11); m.deco_stamp("firewood", 12, 10); m.deco_stamp("firewood", 37, 9); m.deco_stamp("pot", 19, 9)
    m.scatter_deco(north_tiles.BONES, (3, 18, 16, 29), 6)
    # 가죽 말리는 틀·무기 걸이·장작
    m.stamp("rack", 4, 19, force=True); m.stamp("rack", 8, 19, force=True)
    m.deco_stamp("pelt", 12, 20); m.deco_stamp("pelt", 31, 20)
    m.deco_stamp("weaponrack", 13, 11); m.deco_stamp("weaponrack", 28, 11)
    m.deco_stamp("logs", 13, 8); m.deco_stamp("logs_birch", 37, 10); m.deco_stamp("logs", 5, 10)
    # 남서쪽 사냥꾼 야영지
    m.deco_stamp("tent", 3, 24); m.deco_stamp("fire", 8, 25); m.deco_stamp("spit", 11, 24)
    m.deco_stamp("logbench", 7, 28); m.deco_stamp("stump", 10, 27); m.deco_stamp("kettle", 10, 26); m.deco_stamp("bedroll", 3, 28)
    m.road_path([(10, 23), (16, 18)], width=1)
    # 광장: 가운데 고기 화덕, 깃대
    m.deco_stamp("spit", 22, 14)
    m.flag(16, 13); m.flag(30, 13)
    m.deco_stamp("lamp", 19, 12); m.deco_stamp("lamp", 27, 12)
    # 남쪽 들판 (건초 더미)
    m.road_path([(21, 19), (21, 24)], width=1)
    m.deco_stamp("haybale", 16, 25); m.deco_stamp("haybale_s", 24, 25); m.deco_stamp("haybale_s", 26, 27)
    m.deco_stamp("scarecrow", 20, 26); m.deco_stamp("sled", 23, 29)
    # 동쪽: 순록 고원 마을로 가는 큰길 (염소 목장 위로 지나감)
    m.road_path([(30, 16), (38, 15), (45, 15)], width=2, grade=2)
    m.link("E", 14, 4, "north_herd", "W", "E")
    m.sign(41, 13, "→ 순록 고원 마을\n썰매 장인이 산다")
    # 동쪽 염소 목장
    m.fence(32, 18, 40, 25, gap=(35, 18)); m.reserve(33, 19, 39, 24)   # 우리 안은 숲이 먹지 않게
    m.deco_stamp("haybale", 33, 23); m.deco_stamp("haybale_s", 38, 19)

    m.add("market", 24, 19, town="north_hunter")
    m.board(26, 12, "north_hunter")
    m.sign(5, 13, "← 피오르 길\n비르카 · 피오르 나루")
    m.talker(18, 12, "늙은 사냥꾼", "north_owl_elder_tall", "여우 털은 겨울에 받아야 제값이지. 여기서 사면 싸고, 바닷가에 가져가면 비싸.",
             "모피는 가볍지만 비싸서, 도둑맞으면 속이 쓰리다네.")
    m.add("npc", 36, 21, npc="north_goats")        # 염소 맏형: 목장 안 (Q5)
    m.add("npc", 21, 25, npc="north_wife")         # 들일 하는 아내: 건초 들판 (Q1)
    m.talker(33, 13, "염소 치는 아이", "north_fox_kid_tall", "우리 염소들은 소금 덩이를 좋아해. 다리 건너 오는 상인이 가져다주곤 했는데…")
    m.walker([(3, 16), (12, 16), (20, 16), (26, 16), (36, 16), (44, 16), (36, 15), (26, 17), (16, 17)], idx=3)
    m.critters("goat", (34, 19, 39, 23), 3); m.critters("raven", (16, 13, 30, 19), 2)

    m.dense_border("hunt")
    m.grove(40, 28, 3, 2, "hunt")
    m.scatter("snowpine", 4, area=(3, 2, 42, 29), margin=1)
    m.scatter("deadtree", 3, area=(3, 2, 42, 29), margin=1)
    m.snow_props([(12, 4), (29, 3), (3, 21), (30, 27), (42, 12), (15, 29)])
    m.finish()
    return m


def naru():
    """피오르 나루 마을: 피오르 물가의 작은 항구. 배가 모이는 곳이라 모피를 비싸게 산다. 톰테 자리(Q3)."""
    m = FitMap("north_naru", "피오르 나루 마을", 52, 36, seed=43)
    m.road_path([(25, 0), (25, 8), (26, 12)], width=2)
    m.link("N", 24, 4, "north_fjord_road", "S", "N")
    m.road_blob(26, 15.5, 8.5, 3, grade=1)
    # 남쪽 피오르와 선착장 두 개
    m.river_h(26, 35, wiggle=2, fixed=range(10, 42))
    m.road_path([(15, 18), (15, 23)], width=2)
    m.bridge(14, 24, 16, 33, dock=True)
    m.road_path([(37, 18), (37, 23)], width=2)
    m.bridge(36, 24, 38, 33, dock=True)
    m.stamp("boat", 17, 29, force=True, over_road=True)
    m.stamp("boat", 39, 28, force=True, over_road=True)
    m.stamp("boat", 26, 31, force=True, over_road=True)
    # 부두를 잇는 길
    m.road_path([(15, 21), (26, 19), (37, 21)], width=2)

    # 창고·집
    m.stamp("longhall", 4, 4, force=True); m.door_path(4, 4, 4, 7, 13, door_dx=1)
    m.road_path([(5, 13), (18, 15)], width=1)
    m.stamp("h_dark", 30, 4, force=True); m.door_path(30, 4, 4, 4, 12, door_dx=1)
    m.stamp("h_shed", 39, 5, force=True); m.door_path(39, 5, 3, 3, 13, door_dx=1)     # 보트 창고
    m.road_path([(40, 13), (34, 15)], width=1)
    m.deco_stamp("banner", 3, 9); m.deco_stamp("banner", 8, 9)
    # 꿀술 창고 앞 통
    for (bx, by) in [(9, 12), (10, 12), (9, 13)]:
        m.prop(K("element", 3, 0), bx, by)
    m.deco_stamp("bigbarrel", 10, 15); m.deco_stamp("crate", 13, 13); m.deco_stamp("sack", 12, 15); m.deco_stamp("box", 9, 17)

    # 광장·부두 장식: 생선 좌판, 말리는 틀, 그물·밧줄, 등불
    m.deco_stamp("fishbasket", 20, 20); m.deco_stamp("fishbasket", 33, 20)
    m.stamp("rack", 42, 20, force=True); m.stamp("rack", 7, 20, force=True)
    m.deco_stamp("rope", 11, 22); m.deco_stamp("rope", 40, 22)
    m.deco_stamp("crate", 18, 20); m.deco_stamp("barrel", 30, 19); m.deco_stamp("barrel_s", 31, 19); m.deco_stamp("crate_goods", 32, 19)
    m.deco_stamp("lamp", 13, 23); m.deco_stamp("lamp", 39, 23)
    m.deco_stamp("fire", 45, 11); m.deco_stamp("logs", 47, 9); m.deco_stamp("logbench", 44, 14)
    m.flag(19, 12); m.flag(33, 12)
    # 광장 가운데: 생선 장터 (좌판 둘, 말린 생선 틀, 통) — 사람들이 모이는 곳
    m.deco_stamp("stall", 21, 14); m.deco_stamp("stall", 29, 14); m.deco_stamp("fishbasket", 25, 15)
    m.deco_stamp("bigbarrel", 24, 17); m.deco_stamp("crate", 32, 17); m.deco_stamp("basket", 27, 17); m.deco_stamp("sack", 28, 17)

    m.add("market", 28, 19, town="north_naru")
    m.board(31, 13, "north_naru")
    m.sign(22, 4, "↑ 피오르 길\n비르카 · 사냥꾼 마을")
    m.talker(17, 19, "선장", "north_bear_sailor_tall", "남쪽 바다로 나가는 배는 모피를 잔뜩 실어 가. 그래서 여기선 모피가 금값이지.")
    m.add("npc", 7, 14, npc="north_warehouse")     # 창고지기 (Q3 납품처)
    m.talker(11, 10, "톰테 할아버지", "north_owl_elder_tall", "흠흠. 예의 바른 손님은 언제나 환영이지.")   # Q3 정직 시험 (NorthQuestDirector가 바꿔 끼움)
    m.talker(41, 19, "그물 깁는 사람", "north_fox_woman_tall", "얼음 사이로 그물을 내리면 대구가 올라와.")
    m.walker([(16, 14), (21, 13), (27, 13), (33, 14), (37, 17), (30, 21), (20, 21)], idx=4)
    m.critters("gull", (12, 20, 40, 24), 4); m.critters("raven", (18, 12, 34, 17), 1)

    m.dense_border("coast")
    m.scatter("snowpine", 3, area=(3, 2, 48, 22), margin=1)
    m.scatter("snowboulder", 2, area=(3, 2, 48, 22), margin=1)
    m.snow_props([(16, 7), (44, 17), (4, 18), (47, 20), (34, 8)])
    m.finish()
    return m


def laugar():
    """라우가르 온천 마을: 눈 덮인 산 아래 김이 오르는 온천. 목욕 회관과 약초밭. 쉬어 가는 마을 (아늑함 담당).
    특산: 온천 약초. 피오르 길 북쪽 골짜기에서 올라온다."""
    m = FitMap("north_laugar", "라우가르 온천 마을", 48, 36, seed=53)
    # 남쪽 골짜기에서 들어오는 길
    m.road_path([(23, 35), (23, 27), (24, 21)], width=2)
    m.link("S", 22, 4, "north_fjord_road", "N", "S")
    m.road_blob(24, 18.5, 7.5, 3, grade=1)
    # 북쪽 산길: 룬돌 언덕으로
    m.road_path([(18, 15), (16, 9), (13, 4), (13, 0)], width=2)
    m.link("N", 12, 4, "north_runehill", "S", "N")
    m.sign(16, 4, "↑ 룬돌 언덕\n옛 사람들의 돌")
    # 온천 세 곳: 큰 노천탕(서쪽), 작은 탕(동쪽), 약초밭 옆 웅덩이
    m.hot_spring(9, 16, 5.2, 3.2, steam=5)
    m.hot_spring(39, 20, 4.2, 2.6, steam=3)
    m.hot_spring(13, 27, 3, 2, steam=2)
    # 목욕 회관과 집
    m.stamp("h_round", 22, 6, force=True); m.door_path(22, 6, 3, 4, 15, door_dx=1)    # 둥근 지붕 목욕 회관
    m.deco_stamp("banner", 21, 8); m.deco_stamp("banner", 25, 8); m.deco_stamp("bucket", 21, 10); m.deco_stamp("tub", 25, 10)
    m.stamp("h_dome", 30, 8, force=True); m.door_path(30, 8, 3, 3, 15, door_dx=1)      # 돌 둥근 집
    m.road_path([(31, 15), (29, 17)], width=1)
    m.stamp("h_stoneA", 39, 10, force=True); m.door_path(39, 10, 2, 2, 15, door_dx=1)
    m.road_path([(39, 15), (31, 17)], width=1)
    m.stamp("h_stoneB", 5, 24, force=True); m.door_path(5, 24, 2, 2, 30, door_dx=1)
    m.road_path([(5, 30), (12, 31), (22, 30)], width=1)
    # 노천탕 둘레: 등불·통나무 의자·수건 줄
    m.deco_stamp("lamp", 3, 12); m.deco_stamp("lamp", 15, 12); m.deco_stamp("lamp", 34, 18)
    m.deco_stamp("logbench", 6, 20); m.deco_stamp("stump", 14, 19); m.deco_stamp("stump", 2, 17)
    m.laundry(16, 14); m.laundry(43, 17)
    m.deco_stamp("logbench", 36, 23); m.deco_stamp("bigbarrel", 44, 22); m.deco_stamp("cauldron", 15, 17); m.deco_stamp("bucket", 3, 19); m.deco_stamp("cauldron_snow", 42, 18)
    # 약초밭과 약초 말리는 틀
    m.deco_stamp("rack", 17, 24); m.deco_stamp("rack", 17, 28)
    m.deco_stamp("basket_veg", 20, 23); m.deco_stamp("potion", 21, 23); m.deco_stamp("basket", 16, 27); m.deco_stamp("potion", 28, 22)
    m.deco_stamp("haybale_s", 22, 25)
    # 광장: 가운데 모닥불, 깃대
    m.deco_stamp("fire", 23, 17)
    m.flag(17, 20); m.flag(31, 20)
    m.deco_stamp("runestone", 27, 21); m.deco_stamp("logs", 27, 14)
    m.add("market", 26, 22, town="north_laugar")
    m.board(29, 16, "north_laugar")
    m.sign(25, 31, "↓ 피오르 길\n비르카 · 사냥꾼 마을 · 나루")
    m.talker(24, 15, "목욕 회관 주인", "north_bear_granny_tall", "얼어붙은 길을 건너왔구먼. 몸부터 녹이고 가게.",
             "온천물에 담근 약초는 잘 상하지 않아서, 멀리 가져가도 값이 덜 떨어지지.")
    m.talker(20, 26, "약초꾼", "north_hare_tall", "눈 밑에서도 이 온천 둘레에선 약초가 자라. 바닷가 사람들이 비싸게 사 가.")
    m.talker(35, 21, "쉬어 가는 상인", "north_lynx_tall", "밤에 여기서 하늘을 보면 초록빛 강이 흐른다더군. 난 아직 못 봤지만.")
    m.walker([(23, 33), (23, 27), (19, 20), (24, 20), (30, 19), (24, 21)], idx=5)
    m.critters("raven", (17, 16, 31, 21), 2)
    m.dense_border("pine")
    m.scatter("snowpine", 3, area=(3, 7, 44, 32), margin=1)
    m.scatter("snowboulder", 2, area=(3, 7, 44, 32), margin=1)
    m.snow_props([(44, 12), (29, 28), (35, 30), (3, 9)])
    m.finish()
    return m


def herd():
    """순록 고원 마을: 나무가 드문 바람 부는 고원. 순록 떼 목장, 가죽 천막, 썰매 공방. 특산: 순록 치즈.
    사냥꾼 마을 동쪽 큰길로 온다. 나중에 순록 썰매 퀘스트의 무대."""
    m = FitMap("north_herd", "순록 고원 마을", 50, 34, seed=59)
    m.road_path([(0, 15), (10, 15), (18, 17)], width=2, grade=2)
    m.link("W", 14, 4, "north_hunter", "E", "W")
    m.road_blob(24, 17.5, 7, 3, grade=1)
    # 북쪽 골짜기: 얼음 동굴로
    m.road_path([(26, 14), (25, 8), (25, 0)], width=2)
    m.link("N", 24, 4, "north_icecave", "S", "N")
    m.sign(28, 4, "↑ 얼음 동굴 골짜기\n얼어붙은 폭포")
    # 순록 목장 (북동쪽, 큰 울타리)
    m.road_path([(30, 16), (34, 15)], width=1)
    m.fence(31, 4, 45, 14, gap=(34, 14)); m.reserve(32, 5, 44, 13)
    m.deco_stamp("haybale", 37, 6); m.deco_stamp("haybale", 41, 10); m.deco_stamp("haybale_s", 44, 7)
    m.deco_stamp("rock", 33, 6); m.deco_stamp("stumpbig", 38, 11)
    m.talker(35, 9, "순록", "north_reindeer_tall", "푸르르… (코로 눈을 헤집는다)")
    m.talker(38, 9, "순록", "north_reindeer_tall", "(이끼를 오물오물 씹고 있다)")
    m.talker(42, 11, "순록", "north_reindeer_tall", "(귀를 쫑긋 세우고 쳐다본다)")
    # 썰매 공방 (북서쪽)
    m.stamp("h_shed", 8, 5, force=True); m.door_path(8, 5, 3, 3, 13, door_dx=1)       # 썰매 공방
    m.road_path([(9, 13), (17, 16)], width=1)
    m.deco_stamp("logs", 13, 8); m.deco_stamp("logs_birch", 13, 10); m.deco_stamp("crate", 6, 10); m.deco_stamp("rope", 3, 10)
    m.deco_stamp("sled", 12, 12); m.deco_stamp("sled", 4, 12); m.deco_stamp("firewood", 15, 9)
    m.stamp("h_stoneB", 40, 20, force=True); m.deco_stamp("kettle", 25, 27); m.deco_stamp("bedroll", 19, 30); m.deco_stamp("pot", 22, 27)
    m.stamp("h_turf", 20, 7, force=True); m.door_path(20, 7, 3, 2, 15, door_dx=1)
    # 가죽 천막 마을 (남쪽, 모닥불을 둘러쌈)
    m.road_path([(24, 20), (24, 25)], width=1)
    m.deco_stamp("fire", 23, 26)
    for (tx, ty) in [(14, 23), (29, 22), (15, 27), (30, 28)]:
        m.deco_stamp("tent", tx, ty)
    m.deco_stamp("logbench", 20, 25); m.deco_stamp("logbench", 26, 28); m.deco_stamp("stump", 21, 28)
    m.deco_stamp("pelt", 11, 26); m.deco_stamp("pelt", 35, 25); m.stamp("rack", 36, 20, force=True)
    # 광장
    m.deco_stamp("spit", 23, 15); m.flag(17, 19); m.flag(31, 19)
    m.deco_stamp("bigbarrel", 28, 12); m.deco_stamp("runestone", 18, 15)
    # 남동쪽 얼어붙은 호수
    m.water_blob(42, 27, 4.5, 2.5)
    m.add("market", 25, 21, town="north_herd")
    m.board(27, 15, "north_herd")
    m.sign(6, 13, "← 모피 사냥꾼 마을\n피오르 길 · 비르카")
    m.talker(32, 17, "순록 목동 할머니", "north_owl_elder_tall", "순록은 눈 밑 이끼 냄새를 기가 막히게 맡지. 길을 잃으면 순록을 따라가.",
             "우리 치즈는 순록 젖으로 만들어. 진하고 오래 가지.")
    m.talker(11, 14, "썰매 장인", "north_moose_tall", "비르카의 하르 녀석이 손수레만 만든다고? 눈길엔 썰매지! 언젠가 제대로 된 걸 보여 주지.",
             "썰매는 눈보라 속에서도 잘 미끄러진다네. 걷는 것과는 비교가 안 되지.")
    m.talker(22, 24, "불가의 아이", "north_fox_kid_tall", "어젯밤에 하늘이 초록색으로 춤췄어! 할머니는 조상님들이 지나가는 거래.")
    m.walker([(3, 16), (10, 16), (17, 18), (24, 19), (30, 18), (24, 20)], idx=6)
    m.critters("reindeer", (32, 5, 44, 13), 5); m.critters("raven", (18, 15, 30, 20), 1)
    m.dense_border("tundra")
    m.scatter("rock", 3, area=(3, 3, 46, 30), margin=1)
    m.scatter("deadtree", 3, area=(3, 3, 46, 30), margin=1)
    m.snow_props([(6, 20), (45, 18), (38, 31), (12, 31), (27, 3)])
    m.finish()
    return m


def runehill():
    """룬돌 언덕: 온천 마을 북쪽 언덕 위 옛 사람들의 성지. 큰 룬돌 고리, 배 모양 돌무덤, 이끼 낀 옛 회관.
    밤이면 룬돌이 푸르게 빛난다 (NorthLife). 나중에 룬돌 7개 수집의 끝자리."""
    m = FitMap("north_runehill", "룬돌 언덕", 52, 38, seed=61)
    m.road_path([(14, 37), (14, 31), (18, 26), (24, 22)], width=2)
    m.link("S", 13, 4, "north_laugar", "N", "S")
    m.road_blob(26, 17, 9, 4.2, grade=1)                       # 순례자들이 다진 언덕 꼭대기
    # 큰 룬돌 고리: 가운데 높은 돌, 둘레 여덟 개
    m.deco_stamp("snowpost", 26, 14)
    for (rx, ry) in [(20, 17), (32, 17), (22, 14), (30, 14), (22, 20), (30, 20), (26, 12), (26, 21)]:
        m.deco_stamp("runestone", rx, ry)
    # 배 모양 돌무덤 둘
    m.stone_ship(12, 11, 6, beam=3)
    m.stone_ship(40, 28, 6, beam=3)
    # 이끼 낀 옛 회관과 무너진 돌집·돌담
    m.stamp("ruinhall", 38, 4, force=True)
    m.stamp("ruinhut", 5, 24, force=True)
    m.stamp("ruinwall", 30, 30, force=True); m.stamp("ruinwall", 44, 15, force=True)
    m.deco_stamp("totem", 36, 10); m.deco_stamp("totem2", 43, 10)
    # 음유시인의 모닥불
    m.deco_stamp("fire", 15, 20); m.deco_stamp("logbench", 14, 23); m.deco_stamp("stump", 18, 21)
    m.deco_stamp("lantern", 21, 24); m.deco_stamp("lantern", 31, 24)
    m.sign(16, 32, "↓ 라우가르 온천 마을")
    m.talker(26, 23, "룬 지기 노인", "north_owl_elder_tall", "이 고리의 룬돌들은 오래된 이야기를 품고 있지. 북쪽 땅 곳곳에 흩어진 룬돌을 찾아 읽어 보게.",
             "밤이 되면 돌에 새긴 글자가 푸르게 빛난다네. 조상들이 아직 길을 지켜보는 게야.")
    m.talker(17, 22, "음유시인", "north_fox_woman_tall", "♪ 얼음 바다를 건넌 상인들, 별을 따라 길을 냈네… ♪",
             "옛 회관에 살던 사람들은 배를 땅에 묻었대. 저기 돌로 둘러싼 배 모양이 그 무덤이야.")
    m.critters("raven", (19, 13, 33, 21), 4)
    m.dense_border("mixed")
    m.grove(8, 32, 5, 3, "pine"); m.grove(46, 33, 4, 2.5, "mixed")
    m.scatter("deadtree", 4, area=(3, 3, 48, 34), margin=1)
    m.scatter("snowboulder", 3, area=(3, 3, 48, 34), margin=1)
    m.snow_props([(8, 4), (47, 22), (24, 33), (34, 6)])
    m.finish()
    return m


def icecave():
    """얼음 동굴 골짜기: 순록 고원 북쪽. 바위 절벽에서 얼어붙은 폭포가 호수로 흘러내리고,
    동굴 입구 둘레에 호박빛·푸른 수정이 돋아 있다 (밤에 빛남). 광부들의 야영지."""
    m = FitMap("north_icecave", "얼음 동굴 골짜기", 48, 36, seed=67)
    m.road_path([(25, 35), (25, 28), (22, 22), (18, 17), (14, 13)], width=2)
    m.link("S", 24, 4, "north_herd", "N", "S")
    # 바위 절벽과 얼어붙은 폭포, 얼어붙은 호수
    # 폭포 양옆을 바위 무더기로 감싸 바위 틈에서 얼음이 흘러내리게
    for (bx, by) in [(26, 2), (32, 2), (22, 4), (36, 4), (24, 5), (34, 5)]:
        m.stamp("rockbig", bx, by, force=True)
    m.icefall(30, 2, 9); m.icefall(31, 2, 9)
    m.water_blob(30, 13, 7, 4)
    # 동굴 입구: 바위로 둘러싼 깊은 구멍
    for (bx, by) in [(8, 7), (13, 7), (10, 5)]:
        m.stamp("rockbig", bx, by, force=True)
    m.prop(K("nature", 7, 13), 12, 10)                        # 바위 사이 동굴 구멍 (한 칸)
    m.deco_stamp("lantern", 10, 10); m.deco_stamp("lantern", 14, 10)
    m.sign(16, 12, "얼음 동굴\n(아래층은 아직 무너져 막혀 있음)")
    # 수정과 호박
    for (k, x, y) in [("crys_b", 10, 12), ("crys_p", 15, 11), ("crys_o", 9, 9), ("gem_b", 16, 9), ("crys_b", 22, 9),
                      ("gem_o", 11, 14), ("crys_p", 35, 18), ("crys_b", 37, 9), ("gem_b", 24, 17), ("crys_o", 27, 19),
                      ("gem_p", 19, 12), ("crys_b", 39, 15)]:
        m.deco_stamp(k, x, y)
    # 광부 야영지
    m.deco_stamp("tent", 5, 21); m.deco_stamp("fire", 10, 22); m.deco_stamp("logbench", 9, 25)
    m.deco_stamp("sack", 14, 21); m.deco_stamp("box", 15, 21); m.deco_stamp("crate", 5, 25); m.deco_stamp("lantern", 13, 18)
    m.talker(12, 20, "광부 할아범", "north_moose_tall", "동굴 깊은 곳엔 호박이 얼음에 갇혀 있지. 무너진 길만 치우면 다시 캘 수 있을 텐데.",
             "푸른 수정은 밤에 혼자 빛나. 길 잃은 사람한텐 등불 대신이야.")
    m.talker(33, 19, "얼음 낚시꾼", "north_bear_sailor_tall", "폭포가 얼면 호수 밑 물고기가 모여들어. 겨울 대구 맛이 제일이지.")
    m.critters("raven", (18, 18, 32, 26), 2)
    m.dense_border("tundra")
    m.scatter("rock", 3, area=(3, 15, 44, 32), margin=1)
    m.scatter("snowpine", 4, area=(3, 15, 44, 32), margin=1)
    m.snow_props([(40, 26), (6, 30), (30, 30)])
    m.finish()
    return m


def border_pass():
    """국경 고개: 비르카 서쪽 관문에서 남쪽 중앙 교역지로 내려가는 바위 고개와 국경 초소.
    남쪽 출구는 중앙 경유 길(팀 공용)이 생기면 연다. 지금은 막아 둔다."""
    m = FitMap("north_pass", "국경 고개", 52, 36, seed=71)
    m.road_path([(51, 25), (40, 25), (32, 22), (24, 24), (18, 29), (16, 35)], width=2, grade=2)
    m.link("E", 24, 4, "north_birka", "W", "E")
    # 남쪽 출구 자리 (중앙 경유 길과 이을 곳): 지금은 통나무 바리케이드
    m.reserve(12, 30, 21, 35)
    for (bx, by) in [(15, 34), (16, 34), (17, 34), (15, 35), (16, 35), (17, 35)]:
        m.prop(K("element", 3, 0), bx, by)
    m.sign(19, 31, "↓ 남쪽 중앙 교역지\n(길을 닦는 중, 아직 지날 수 없음)")
    # 국경 초소: 판자 울타리 안에 경비 막사·창고, 깃발
    m.fence(20, 7, 37, 18, gap=(27, 18)); m.reserve(21, 8, 36, 17)
    m.road_path([(28, 19), (29, 22)], width=2)
    m.stamp("h_dark", 22, 9, force=True); m.door_path(22, 9, 4, 4, 16, door_dx=1)
    m.stamp("h_shed", 31, 10, force=True); m.door_path(31, 10, 3, 3, 16, door_dx=1)
    m.deco_stamp("banner", 21, 8); m.deco_stamp("banner", 36, 8)
    m.deco_stamp("weaponrack", 27, 9); m.deco_stamp("fire", 28, 14); m.deco_stamp("bigbarrel", 34, 14)
    m.deco_stamp("lantern", 26, 15); m.deco_stamp("helm", 30, 14); m.deco_stamp("firewood", 24, 15)
    m.flag(19, 19); m.flag(38, 19)
    # 바위 고개
    for (bx, by) in [(6, 6), (42, 5), (8, 16), (44, 13), (40, 30)]:
        m.stamp("rockbig", bx, by, force=True)
    m.talker(30, 20, "국경 경비대장", "north_wolf_viking_tall", "여기서부터 남쪽은 북유럽 땅이 아니야. 중앙 교역지로 가는 길을 닦는 중이지.",
             "길이 열리면 네 문명의 상인들이 다 모인다더군. 그때 모피 값 좀 보라고.")
    m.talker(22, 27, "남쪽에서 온 상인", "north_lynx_tall", "남쪽 큰 장터엔 비단이며 향신료며 없는 게 없어. 북쪽 호박은 거기서 금값이지.")
    m.event(36, 24); m.event(20, 27)
    m.walker([(48, 26), (40, 26), (32, 23), (25, 25), (20, 28)], idx=8)
    m.critters("raven", (24, 20, 44, 27), 2)
    m.dense_border("tundra")
    m.scatter("deadtree", 4, area=(3, 3, 48, 32), margin=1)
    m.scatter("snowboulder", 3, area=(3, 3, 48, 32), margin=1)
    m.snow_props([(14, 10), (46, 20), (10, 24)])
    m.finish()
    return m


MAPS = [birka, fjord_road, hunter, naru, laugar, herd, runehill, icecave, border_pass]


if __name__ == "__main__":  # 미리보기만 (내보내기는 export_north.py)
    os.makedirs(OUT, exist_ok=True)
    for f in MAPS:
        m = f()
        with open(os.path.join(OUT, m.id + ".json"), "w") as fp:
            json.dump(m.to_json(), fp, ensure_ascii=False)
        m.render(os.path.join(OUT, m.id + ".png"), scale=2)
        print(m.id, m.w, m.h)
