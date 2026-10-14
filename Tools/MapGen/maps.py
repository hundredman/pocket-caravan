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
    m.road_path([(21, 10), (27, 10), (31, 9), (35, 9)], width=2, wobble=0)  # 동쪽 예성 길로 (9~10행)
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
    m.add("spawn", 16, 11, id="S0")   # 게임 시작 자리
    m.link("E", 8, 4, "Route01", "W", "E")      # 8~11행, 길은 9~10행
    # 숲과 나무
    m.forest_border(thickness=2)
    m.scatter("tree", 5, area=(3, 2, 33, 17), margin=1)
    m.scatter("pine", 4, area=(3, 2, 33, 17), margin=1)
    m.finish()
    return m


def route01():
    """예성 길: 서쪽 나루 마을에서 들어와 시내를 건너 북쪽 개경 남문으로 올라간다."""
    m = Map("Route01", "예성 길", 40, 30, seed=7)
    m.water_rect(14, 0, 16, 29)                  # 시내 (남북으로 흐름)
    m.road_path([(0, 9), (8, 9), (12, 10), (20, 10), (26, 12), (30, 14)], width=2)
    m.bridge(14, 10, 16, 11)
    m.road_path([(30, 14), (30, 8), (27, 4), (27, 0)], width=4, grade=2)   # 개경 남문 쪽 큰길 (폭 4)
    m.link("W", 8, 4, "Naru", "E", "W")          # 나루 마을과 같은 8~11행, 길 9~10행
    m.link("N", 27, 4, "Gaegyeong", "S", "N")    # 개경 남문 폭 4
    m.forest_border(thickness=2)
    m.scatter("pine", 12, area=(2, 2, 37, 27), margin=1)
    m.scatter("tree", 10, area=(2, 2, 37, 27), margin=1)
    m.scatter("cherry", 4, area=(2, 2, 37, 27), margin=1)
    m.finish()
    return m


def dakmok():
    """닥나무 마을: 종이를 뜨는 마을. 개경 동문 밖."""
    m = Map("Dakmok", "닥나무 마을", 34, 22, seed=11)
    m.road_rect(0, 10, 10, 12, grade=2)          # 개경 동문 큰길(폭 3)에 이어짐
    m.road_path([(10, 10), (14, 10)], width=3)
    m.road_rect(13, 8, 21, 12, grade=1)
    m.water_rect(26, 14, 31, 18)                 # 종이 뜨는 연못
    m.stamp("house_thatch", 6, 3)
    m.stamp("house_plain", 14, 3)
    m.stamp("house_thatch", 22, 4)
    m.stamp("house_plain", 5, 14)
    m.stamp("house_thatch", 15, 15)
    m.add("npc", 11, 9, npc="dakmok_chief")
    m.add("market", 18, 13, town="dakmok")
    m.prop(PROPS[3], 19, 14); m.prop(PROPS[1], 20, 14)
    m.link("W", 10, 3, "Gaegyeong", "E", "W")
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
