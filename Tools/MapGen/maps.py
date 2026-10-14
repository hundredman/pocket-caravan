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
    # 논: 누렇게 익은 벼 (나루 위, 동쪽 언덕 아래)
    for (px, py) in [(13, 17), (16, 17), (33, 6), (33, 9)]:
        m.stamp("wheat", px, py, force=True)
    m.stamp("sheaf", 19, 17, force=True); m.stamp("sheaf", 20, 17, force=True)
    m.stamp("hay", 36, 12, force=True); m.stamp("hay", 37, 12, force=True)
    # 집 둘레 살림: 장독대, 우물, 빨래
    m.jars(9, 6, ("11", "01"))
    m.jars(24, 6, ("11",))
    m.stamp("well", 13, 8, force=True)
    m.laundry(1, 11)
    m.laundry(32, 16)
    # 덤불과 꽃
    m.bushes([(4, 9), (5, 9), (26, 9), (27, 9), (31, 9), (2, 18), (3, 18), (7, 18), (23, 3), (24, 3)])
    m.flowers(2, 2, 37, 20, 18)
    for (fx, fy) in [(8, 10), (11, 3), (29, 10), (35, 4), (6, 16), (24, 19), (15, 10)]:
        if not m.reserved[fy][fx]:
            m.anim("plant", fx, fy, frames=4, fps=2.5)
    # 물가 갈대
    m.reeds(21, 7, 30, 0.6)
    m.reeds(20, 0, 6, 0.5); m.reeds(20, 31, 39, 0.5)
    # 장터 한쪽
    m.prop(K("element", 3, 0), 25, 16); m.prop(K("element", 0, 1), 26, 16); m.prop(K("element", 1, 1), 27, 16)
    m.stamp("cart", 22, 17, force=True)
    m.sign(30, 11, "→ 예성 길\n개경 남문 25리")
    m.board(27, 12, "naru")
    # 마을이 커지면: 2등급(장터 마을)에 노점 짐, 3등급(상업 고을)에 더 많이
    for (gx, gy, k, lv) in [(21, 16, K("element", 3, 0), 2), (20, 16, K("element", 0, 1), 2), (28, 16, K("element", 1, 1), 2),
                            (23, 11, K("element", 4, 0), 2), (16, 11, K("element", 3, 0), 3), (17, 11, K("element", 0, 1), 3),
                            (26, 18, K("element", 4, 0), 3), (27, 18, K("element", 4, 0), 3)]:
        m.growth(gx, gy, k, "naru", lv)

    # 사람
    m.add("npc", 21, 13, npc="naru_chief")       # 촌장: 정자나무 아래
    m.add("market", 24, 15, town="naru")         # 나루 장터
    m.talker(12, 20, "뱃사공", "villager_bear_tall", "예성강 물길을 따라 내려가면 벽란도 포구가 나와. 송나라 배도 드나들지.")
    m.talker(16, 19, "농부", "villager_rabbit_tall", "올해는 볕이 좋아 곡식이 잘 여물었어. 장날에 개경에 내다 팔 참이야.")
    m.add("spawn", 17, 14, id="S0")              # 게임 시작 자리 (마당)

    # 숲
    m.forest_border(thickness=2, big=("pinebig", "roundbig", "fruitbig"))
    m.stamp("blossom", 35, 1, force=False)
    m.scatter("roundtree", 2, area=(3, 2, 36, 19), margin=1)
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
    m.talker(45, 21, "수레꾼", "villager_dog_tall", "바퀴가 돌부리에 걸렸지 뭐요. 손수레는 길 없는 풀밭에서 영 힘을 못 써.",
             "시내 건너 숲 근처에 도적이 숨어 있다더군. 짐을 싣고 지날 땐 달릴 준비를 하시오.")

    # 도적: 시내 동쪽 숲 그늘 (밤에 더 잦음)
    m.bandit(36, 22, radius=6)
    # 돌발 사건 자리 (길가)
    for (ex, ey) in [(11, 13), (24, 17), (39, 13)]:
        m.event(ex, ey)
    # 길을 오가는 NPC 상인
    m.walker([(3, 10), (9, 10), (13, 13), (22, 15), (27, 15)], idx=0)
    m.walker([(31, 16), (36, 16), (41, 16), (42, 11), (40, 3)], idx=1)

    m.forest_border(thickness=2, big=("pinebig", "roundbig", "pinebig"))
    # 숲 덩어리와 꽃나무: 길 양쪽 경치
    m.scatter("pinebig", 4, area=(3, 18, 50, 29), margin=1)
    m.scatter("roundbig", 3, area=(3, 2, 50, 7), margin=1)
    m.scatter("blossom", 3, area=(3, 2, 50, 30), margin=1)
    m.scatter("pine", 10, area=(2, 2, 51, 31), margin=1)
    m.scatter("tree", 8, area=(2, 2, 51, 31), margin=1)
    m.bushes([(x, y) for x in range(4, 50, 5) for y in (11, 19)])
    m.flowers(2, 2, 51, 31, 30)
    m.reeds(13, 27, 27, 1.0); m.reeds(17, 31, 31, 1.0)
    for (fx, fy) in [(8, 15), (19, 18), (33, 20), (47, 9), (25, 5)]:
        if not m.reserved[fy][fx]:
            m.anim("plant", fx, fy, frames=4, fps=2.5)
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
    m.board(22, 11, "dakmok")
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
