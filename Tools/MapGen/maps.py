"""고려 시작 지역 맵 설계. python3 maps.py 로 JSON과 미리보기 PNG를 만든다."""
import json, os, sys
from tiles import Map, K, PROPS

OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
os.makedirs(OUT, exist_ok=True)


def naru():
    """예성 나루 마을: 예성강 나루터의 작은 촌락. 정자나무 마당을 중심으로 집 몇 채와 밭. 튜토리얼 시작."""
    m = Map("Naru", "예성 나루 마을", 40, 28, seed=5)
    # 남쪽 강과 나루
    m.water_rect(0, 22, 39, 27)
    m.water_rect(0, 21, 6, 21); m.water_rect(31, 21, 39, 21)     # 강가 굴곡
    m.bridge(9, 19, 11, 24)                                      # 나루 판자
    m.stamp("boat", 12, 23, force=True)

    # 마당과 길: 나루 → 마당 → 동쪽 예성 길
    m.road_rect(15, 12, 25, 15, grade=1)                         # 정자나무 앞 마당
    m.road_path([(10, 18), (10, 14), (15, 14)], width=2)
    m.road_path([(25, 13), (31, 13), (35, 14), (39, 13)], width=2)
    m.link("E", 12, 4, "Route01", "W", "E")                      # 12~15행, 길 13~14행

    # 정자나무와 평상
    m.stamp("bigtree", 19, 8, force=True)
    m.stamp("bench", 19, 11, force=True)

    # 집과 문 앞 길
    houses = [("house_tile", 17, 3), ("house_thatch", 5, 5), ("house_plain", 27, 5), ("house_thatch", 3, 13), ("house_thatch", 28, 17)]
    for name, hx, hy in houses:
        m.stamp(name, hx, hy, force=True)
    m.door_path(17, 3, 4, 3, 7)        # 촌장 댁 → 정자나무
    m.door_path(5, 5, 4, 3, 13)
    m.road_path([(6, 13), (10, 13)], width=1)
    m.door_path(27, 5, 4, 3, 12)
    m.door_path(3, 13, 4, 3, 17); m.road_path([(4, 17), (9, 17)], width=1)
    # 밭 (나루 위쪽 왼편, 오른쪽 언덕 아래)
    for (px, py) in [(14, 17), (17, 17), (33, 6)]:
        m.stamp("plot", px, py, force=True)
        for dx in range(3):
            m.high[py + 1][px + dx] = K("detail", 6, 2)   # 새싹 (흙더미 위에 보이게)
    # 울타리 대신 덤불, 꽃밭
    m.bushes([(4, 9), (5, 9), (8, 9), (9, 9), (26, 9), (27, 9), (31, 9), (2, 18), (3, 18), (7, 18)])
    m.flowers(2, 2, 37, 20, 22)
    # 장터 한쪽
    m.prop(K("element", 3, 0), 25, 16); m.prop(K("element", 0, 1), 26, 16); m.prop(K("element", 1, 1), 27, 16)
    m.stamp("cart", 22, 17, force=True)
    m.sign(30, 11, "→ 예성 길\n개경 남문 25리")

    # 사람
    m.add("npc", 21, 13, npc="naru_chief")       # 촌장: 정자나무 아래
    m.add("market", 24, 15, town="naru")         # 나루 장터
    m.talker(12, 20, "뱃사공", "villager_bear_tall", "예성강 물길을 따라 내려가면 벽란도 포구가 나와. 송나라 배도 드나들지.")
    m.talker(16, 19, "농부", "villager_rabbit_tall", "올해는 볕이 좋아 곡식이 잘 여물었어. 장날에 개경에 내다 팔 참이야.")
    m.add("spawn", 17, 14, id="S0")              # 게임 시작 자리 (마당)

    # 숲
    m.forest_border(thickness=2)
    m.scatter("roundtree", 3, area=(3, 2, 36, 19), margin=1)
    m.finish()
    return m


def route01():
    """예성 길: 서쪽 나루 마을에서 들어와 주막에서 쉬고, 시내를 건너 북쪽 개경 남문으로 올라간다."""
    m = Map("Route01", "예성 길", 54, 34, seed=7)
    m.hours = 3                                   # 지나는 데 반나절쯤 걸리는 길
    m.water_rect(28, 0, 30, 33)                   # 시내 (남북으로 흐름)
    m.road_path([(0, 9), (9, 9), (13, 12), (22, 14), (34, 15), (42, 16)], width=2)
    m.bridge(28, 14, 30, 16)
    m.road_path([(42, 16), (42, 10), (39, 6), (39, 0)], width=4, grade=2)   # 개경 남문 쪽 (폭 4)
    m.link("W", 8, 4, "Naru", "E", "W")           # 나루 마을과 같은 8~11행, 길 9~10행
    m.link("N", 39, 4, "Gaegyeong", "S", "N")     # 개경 남문 폭 4

    # 주막: 길손이 쉬어 가는 곳
    m.stamp("house_thatch", 16, 7, force=True)
    m.stamp("bench", 21, 10, force=True)
    m.prop(K("element", 3, 0), 15, 10); m.prop(K("element", 1, 1), 20, 9)
    m.talker(18, 11, "주모", "villager_tanuki_tall", "먼 길 가시오? 개경 남문까지는 시내 건너 한참이오. 쉬었다 가시구려.",
             "개경에 처음 가면 남문 앞 큰길만 따라가시오. 길 잃을 일 없소.")

    # 이정표
    m.sign(6, 12, "← 예성 나루 마을 5리\n개경 남문 20리 →")
    m.sign(44, 19, "↑ 개경 남문 10리\n← 예성 나루 마을 15리")

    # 길에서 만나는 사람
    m.talker(35, 17, "길손", "villager_fox_tall", "벽란도에서 오는 길이오. 거기는 송나라 배가 드나들어 별 물건이 다 있소.")
    m.stamp("cart", 46, 22, force=True)
    m.talker(45, 21, "수레꾼", "villager_dog_tall", "바퀴가 돌부리에 걸렸지 뭐요. 손수레는 길 없는 풀밭에서 영 힘을 못 써.")

    m.forest_border(thickness=2)
    m.scatter("pine", 18, area=(2, 2, 51, 31), margin=1)
    m.scatter("tree", 14, area=(2, 2, 51, 31), margin=1)
    m.scatter("cherry", 6, area=(2, 2, 51, 31), margin=1)
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
    m.sign(4, 14, "← 개경 동문 5리")
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
