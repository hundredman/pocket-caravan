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
    # 동쪽 피오르 길은 아직 없음: 팀 규칙대로 가짜 출구를 만들지 않고 통으로 막아 둔다
    m.reserve(37, 12, 39, 15)
    for (bx, by) in [(39, 13), (39, 14), (38, 13), (38, 14)]:
        m.prop(K("element", 3, 0), bx, by)
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
    m.sign(31, 11, "→ 피오르 길\n(눈을 치우는 중)")

    # 사람
    m.add("npc", 21, 9, npc="north_innkeeper")     # 여관 할머니: 회관 앞
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


MAPS = [birka]

if __name__ == "__main__":  # 미리보기만 (내보내기는 export_north.py)
    os.makedirs(OUT, exist_ok=True)
    for f in MAPS:
        m = f()
        with open(os.path.join(OUT, m.id + ".json"), "w") as fp:
            json.dump(m.to_json(), fp, ensure_ascii=False)
        m.render(os.path.join(OUT, m.id + ".png"), scale=2)
        print(m.id, m.w, m.h)
