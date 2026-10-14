"""고려 시작 지역 맵 설계. python3 maps.py 로 JSON과 미리보기 PNG를 만든다."""
import json, os, sys
from tiles import Map, K, PROPS

OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
os.makedirs(OUT, exist_ok=True)


def naru():
    """예성 나루 마을: 예성강 나루터의 작은 촌락. 튜토리얼 시작."""
    m = Map("Naru", "예성 나루 마을", 36, 24, seed=3)
    # 남쪽 강과 나루터
    m.water_rect(0, 19, 35, 23)
    m.bridge(7, 17, 9, 20)                       # 나루 판자
    m.stamp("boat", 10, 20, force=True)
    m.road_path([(8, 16), (8, 11)], width=2)     # 나루에서 마을로
    m.road_rect(12, 9, 21, 12, grade=1)          # 마을 마당
    m.road_path([(8, 11), (12, 11)], width=2)
    m.road_path([(21, 10), (27, 10), (31, 9), (35, 9)], width=2, wobble=0)  # 동쪽 1번 길로
    m.reserve(30, 8, 35, 12)
    # 집
    m.stamp("house_thatch", 3, 4)
    m.stamp("house_plain", 10, 4)
    m.stamp("house_tile", 18, 4)                 # 촌장 댁
    m.stamp("house_thatch", 25, 4)
    m.stamp("house_thatch", 2, 12)
    m.stamp("house_plain", 23, 13)
    # 장터 한쪽 (짐, 항아리)
    m.prop(PROPS[0], 13, 13); m.prop(PROPS[1], 14, 13); m.prop(PROPS[2], 16, 14)
    m.stamp("cart", 18, 13)
    # 사람
    m.add("npc", 19, 8, npc="naru_chief")        # 촌장 (집 앞)
    m.add("market", 15, 13, town="naru")         # 나루 장터
    m.add("spawn", 16, 11, id="S")
    m.add("spawn", 34, 9, id="E")
    m.add("exit", 35, 8, w=1, h=4, target="Route01", spawn="W")
    # 숲과 나무
    m.forest_border(thickness=2)
    m.scatter("tree", 5, area=(3, 2, 33, 17), margin=1)
    m.scatter("pine", 4, area=(3, 2, 33, 17), margin=1)
    m.finish()
    return m


def route01():
    """예성 길: 나루 마을에서 개경 남문까지. 시내와 다리."""
    m = Map("Route01", "예성 길", 44, 22, seed=7)
    m.water_rect(24, 0, 26, 21)                  # 시내
    m.road_path([(0, 10), (8, 10), (14, 8), (20, 9), (28, 9), (34, 11), (43, 11)], width=2, wobble=0)
    m.bridge(24, 9, 26, 10)
    m.reserve(0, 8, 2, 13); m.reserve(41, 9, 43, 13)
    m.add("spawn", 1, 10, id="W")
    m.add("spawn", 42, 11, id="E")
    m.add("exit", 0, 9, w=1, h=4, target="Naru", spawn="E")
    m.add("exit", 43, 10, w=1, h=4, target="Gaegyeong", spawn="S")
    m.forest_border(thickness=2)
    m.scatter("pine", 10, area=(2, 2, 41, 19), margin=1)
    m.scatter("tree", 8, area=(2, 2, 41, 19), margin=1)
    m.scatter("cherry", 3, area=(2, 2, 41, 19), margin=1)
    m.finish()
    return m


def dakmok():
    """닥나무 마을: 종이를 뜨는 마을. 개경 동문 밖."""
    m = Map("Dakmok", "닥나무 마을", 34, 22, seed=11)
    m.road_path([(0, 11), (10, 11), (14, 10)], width=2, wobble=0)
    m.road_rect(13, 8, 21, 12, grade=1)
    m.reserve(0, 9, 2, 13)
    m.water_rect(26, 14, 31, 18)                 # 종이 뜨는 연못
    m.stamp("house_thatch", 6, 3)
    m.stamp("house_plain", 14, 3)
    m.stamp("house_thatch", 22, 4)
    m.stamp("house_plain", 5, 14)
    m.stamp("house_thatch", 15, 15)
    m.add("npc", 11, 9, npc="dakmok_chief")
    m.add("market", 18, 13, town="dakmok")
    m.prop(PROPS[3], 19, 14); m.prop(PROPS[1], 20, 14)
    m.add("spawn", 1, 11, id="W")
    m.add("exit", 0, 10, w=1, h=4, target="Gaegyeong", spawn="E")
    m.forest_border(thickness=2)
    m.scatter("tree", 6, area=(3, 2, 30, 18), margin=1)
    m.finish()
    return m


if __name__ == "__main__":  # 미리보기만 (내보내기는 export.py)
    import gaegyeong
    for f in (naru, route01, gaegyeong.build, dakmok):
        m = f()
        with open(os.path.join(OUT, m.id + ".json"), "w") as fp:
            json.dump(m.to_json(), fp, ensure_ascii=False)
        m.render(os.path.join(OUT, m.id + ".png"))
        print(m.id, m.w, m.h)
