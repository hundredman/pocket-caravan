"""고려 시작 지역 맵 설계. python3 maps.py 로 JSON과 미리보기 PNG를 만든다."""
import json, os, sys
from tiles import Map, K, PROPS

OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
os.makedirs(OUT, exist_ok=True)


def naru():
    """예성 나루 마을: 예성강 나루터의 작은 촌락. 정자나무 마당을 중심으로 집 몇 채와 밭. 튜토리얼 시작."""
    m = Map("Naru", "예성 나루 마을", 40, 28, seed=5)
    # 남쪽 강과 나루
    m.river_h(22, 27, wiggle=2, fixed=range(8, 15))              # 구불구불한 강가 (나루 앞은 반듯)
    m.bridge(9, 19, 11, 24, dock=True)                           # 기둥·난간을 갖춘 선착장
    m.stamp("boat", 12, 23, force=True, over_road=True)

    # 마당과 길: 나루 → 마당 → 동쪽 예성 길
    m.road_blob(20, 13.5, 6.2, 2.4, grade=1)                     # 정자나무 앞 둥근 마당
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
    m.road_path([(18, 7), (17, 7), (17, 11), (17, 13)], width=1)  # 나무를 돌아 마당까지
    m.door_path(5, 5, 4, 3, 12)
    m.road_path([(6, 12), (10, 12), (10, 14)], width=1)
    m.door_path(27, 5, 4, 3, 12)
    m.door_path(3, 13, 4, 3, 17); m.road_path([(4, 17), (9, 17)], width=1)
    # 선착장 → 하역 마당 → 장터로 이어지는 운송 흔적. 접근로 중앙은 비워 둔다.
    m.road_path([(10, 18), (7, 18)], width=1, grade=1)
    m.stamp("cart", 5, 18, force=True)
    for px, py, prop in [(7, 19, 0), (8, 19, 1), (12, 17, 0), (12, 18, 2), (13, 16, 1)]:
        m.prop(PROPS[prop], px, py)
    m.sign(12, 16, "나루 하역장\n이곳 소금은 개경·닥나무 마을에서 거래할 수 있다.\n돌아올 때 실을 물건도 장터에서 살펴보자.")
    m.add("worker", 10, 20, name="나루 짐꾼", character="villager_dog_tall",
          lines=["배에서 내린 소금은 저 위 장터로 옮기고 있소. 길을 비워 두었으니 짐수레도 지나갈 수 있소."],
          path=[(10, 20), (10, 17)])
    m.add("ambience", 11, 23, id="river", radius=12)
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
    m.talker(19, 20, "농부", "villager_rabbit_tall", "올해는 볕이 좋아 곡식이 잘 여물었어. 장날에 개경에 내다 팔 참이야.")
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
    m.river_v(28, 30, wiggle=1, fixed_rows=(*range(3, 7), *range(12, 19)))   # 구불구불한 시내 (다리 근처는 반듯)
    m.road_path([(0, 9), (9, 9), (13, 12), (22, 14), (34, 15), (42, 16)], width=2, grade=3)
    m.bridge(27, 14, 31, 16)                      # 물가까지 닿는 양쪽 교대
    m.road_path([(42, 16), (42, 10), (39, 6), (39, 0)], width=4, grade=3)   # 개경 남문 쪽 (폭 4)
    # 두 길은 같은 분기/합류점을 잇는다. 큰길은 수레, 짧은 숲길은 등짐에 유리하다.
    # 별도 속도 보너스를 만들지 않고 본편 VehicleData의 노면별 속도를 그대로 사용한다.
    m.road_path([(10, 10), (5, 5), (16, 3), (21, 2), (25, 4), (34, 4), (39, 6)], width=1, grade=1)
    m.bridge(27, 4, 31, 5)                          # 숲길의 작은 목교. 새 문명 연결이 아님
    for by in (4, 5):                              # 좁은 목교도 숲길과 같은 노면 등급
        for bx in range(27, 32):
            m.road[by][bx] = 1
    m.link("W", 8, 4, "Naru", "E", "W")           # 나루 마을과 같은 8~11행, 길 9~10행
    m.link("N", 39, 4, "Gaegyeong", "S", "N")     # 개경 남문 폭 4

    # 주막: 길손이 쉬어 가는 곳
    m.stamp("house_thatch", 16, 7, force=True)
    m.door_path(16, 7, 4, 3, 13)                 # 주막 입구에서 교역로까지
    m.stamp("bench", 21, 10, force=True)
    m.prop(K("element", 3, 0), 15, 10); m.prop(K("element", 1, 1), 20, 9)
    m.talker(18, 11, "주모", "villager_tanuki_tall", "먼 길 가시오? 개경 남문까지는 시내 건너 한참이오. 쉬었다 가시구려.",
             "짐수레라면 이 앞 큰 다리로 가시오. 넓은 길이라 바퀴가 잘 구르오.",
             "등짐이 가벼우면 서쪽 갈림길에서 숲길로 가도 되오. 작은 다리를 건너면 남문 길에서 다시 만나오.")

    # 이정표
    m.sign(8, 11, "개경 남문 가는 두 길\n↑ 숲길 · 가까운 길 / 등짐 권장\n→ 주막 큰길 · 돌아가지만 수레가 빠름")
    m.sign(45, 19, "↑ 개경 남문 10리\n← 예성 나루 마을 15리")
    m.sign(37, 5, "← 작은 다리 · 나루 숲길\n↓ 큰 다리 · 주막 큰길\n↑ 개경 남문")
    m.sign(25, 5, "작은 다리 숲길\n짧지만 좁고 울퉁불퉁하다.\n짐이 흔들리면 걷거나 잠시 쉬어 가자.")

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
    # 숲길 주변은 나무를 군집 배치한다. 통행 예약 칸에는 묶음 전체를 놓지 않는다.
    for tx, ty in [(13, 5), (21, 6), (33, 7), (35, 1)]:
        m.stamp("pinebig", tx, ty)
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
    """닥나무 마을: 종이를 뜨는 마을. 개경 동문 밖. 둥근 마당을 집들이 둘러싸고, 동쪽에 종이 뜨는 연못."""
    m = Map("Dakmok", "닥나무 마을", 34, 22, seed=11)
    m.road_path([(0, 10), (5, 10), (9, 11), (13, 10)], width=3, grade=2)   # 개경 동문 큰길에서 이어짐
    m.link("W", 10, 3, "Gaegyeong", "E", "W")
    m.road_blob(18, 10.5, 5.5, 2.4, grade=1)        # 마을 마당
    m.water_blob(27, 16, 3.6, 2.6)                  # 종이 뜨는 연못

    # 집: 마당을 둘러싸고, 문 앞에서 마당까지 좁은 길
    m.stamp("house_thatch", 6, 3); m.door_path(6, 3, 4, 3, 9)
    m.stamp("house_plain", 14, 3); m.door_path(14, 3, 4, 3, 8)
    m.stamp("house_thatch", 23, 3); m.door_path(23, 3, 4, 3, 9); m.road_path([(24, 9), (22, 10)], width=1)
    m.stamp("house_plain", 4, 14); m.road_path([(5, 17), (9, 17), (12, 13), (13, 12)], width=1)
    m.stamp("house_thatch", 13, 16); m.road_path([(14, 19), (21, 19), (23, 13), (21, 12)], width=1)

    # 종이 말리는 틀과 빨래, 장독
    m.stamp("paper_rack", 28, 9); m.stamp("paper_rack", 28, 5)
    m.road_path([(22, 11), (26, 11), (26, 8)], width=1, grade=1)
    m.road_path([(26, 11), (26, 12)], width=1, grade=1)
    m.prop(PROPS[0], 27, 11); m.prop(PROPS[2], 28, 12)
    m.sign(28, 7, "종이 작업장\n닥나무 재료를 씻고 종이를 떠서 말리는 곳.\n마른 종이는 마을 장터에서 거래한다.")
    m.add("worker", 26, 8, name="종이 일꾼", character="villager_rabbit_tall",
          lines=["뜬 종이는 저 건조대에서 말려요. 개경에 다녀올 생각이면 종이 값을 살펴보세요.",
                 "돌아오는 길에는 소금이나 생필품을 살펴봐요. 다만 장터 재고에 따라 값이 달라지니 장부만 믿지는 말고요."],
          path=[(26, 8), (26, 12)])
    m.add("ambience", 27, 16, id="river", radius=7)
    m.laundry(10, 6)
    m.jars(2, 8, ("11", "10"))

    m.add("npc", 13, 9, npc="dakmok_chief")
    m.board(21, 7, "dakmok")
    m.add("market", 18, 13, town="dakmok")
    m.prop(PROPS[3], 19, 14); m.prop(PROPS[1], 20, 14)
    m.sign(4, 13, "← 개경 동문 5리")

    m.reeds(13, 23, 31, 0.6); m.reeds(19, 23, 31, 0.5)
    m.bushes([(3, 6), (12, 7), (26, 8), (9, 15), (19, 17)])
    m.flowers(2, 2, 31, 19, 16)
    for (fx, fy) in [(8, 13), (25, 12), (16, 7)]:
        if not m.reserved[fy][fx]:
            m.anim("plant", fx, fy, frames=4, fps=2.5)
    m.forest_border(thickness=2, big=("pinebig", "roundbig", "fruitbig"))
    m.scatter("tree", 3, area=(3, 2, 30, 18), margin=1)
    m.scatter("blossom", 1, area=(3, 2, 30, 18), margin=1)
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
