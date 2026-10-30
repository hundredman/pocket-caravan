"""베네치아 교역 균형 시뮬레이션 (읽기 전용, Unity 불필요).
MarketMath·MarketState 공식을 그대로 옮겨, 등짐(10)·손수레(30)로 한 번 오갈 때의 이익을 계산한다.
수치 출처는 VeniceRegionSetup.cs와 같은 표(아래 TOWNS·GOODS)이며, 둘을 함께 고친다.
사용: python Tools/Integration/venice_trade_sim.py"""
import math

TOWNS = {   # id: (좌표, 등급)
    "rialto": ((12, 1), 1), "chioggia": ((13, 2), 1), "murano": ((13, 0), 1),
    "santerasmo": ((11, 0), 1), "burano": ((11, 2), 1),
}
# 상품: (무게, 기준가, 맵당 비율, 산지)
GOODS = {
    "salt": (2, 8, 0.5, "chioggia"), "glass": (1, 24, 0.35, "murano"),
    "fruit": (2, 7, 0.5, "santerasmo"), "fish": (2, 9, 0.5, "burano"),
}
# 마을별 재고: 상품 → (적정 재고, 지역 수요 배율)
STOCK = {
    "rialto": {"salt": (30, 1), "glass": (20, 1), "fruit": (30, 1), "fish": (30, 1)},
    "chioggia": {"salt": (80, 1), "fish": (14, 1.15), "glass": (8, 1), "fruit": (14, 1.15)},
    "murano": {"glass": (40, 1), "salt": (14, 1), "fruit": (14, 1.1), "fish": (14, 1.1)},
    "santerasmo": {"fruit": (80, 1), "glass": (8, 1.2), "fish": (14, 1.15), "salt": (14, 1)},
    "burano": {"fish": (80, 1), "salt": (14, 1.3), "fruit": (14, 1), "glass": (8, 1)},
}
DEFAULT_OPTIMAL = 3
BUYBACK = 0.85


def dist(a, b):
    (ax, ay), (bx, by) = TOWNS[a][0], TOWNS[b][0]
    return abs(ax - bx) + abs(ay - by)


def base(good, town):
    w, b, ppm, origin = GOODS[good]
    mult = STOCK[town].get(good, (DEFAULT_OPTIMAL, 1))[1]
    return b * (1 + ppm * dist(origin, town)) * mult


def optimal(good, town):
    return STOCK[town].get(good, (DEFAULT_OPTIMAL, 1))[0] * (1 + 0.5 * (TOWNS[town][1] - 1))


def price(b, opt, stock):
    m = 2 if stock <= 0.01 else min(2, max(0.5, opt / stock))
    return max(1, round(b * m))


def trip(good, src, dst, capacity, money):
    """src에서 살 수 있는 만큼 사서 dst에 모두 판다 → (개수, 비용, 수익)"""
    w = GOODS[good][0]
    s_src, s_dst = optimal(good, src), optimal(good, dst)
    n, cost, income = 0, 0, 0
    while (n + 1) * w <= capacity and s_src >= 1:
        p = price(base(good, src), optimal(good, src), s_src)
        if cost + p > money:
            break
        cost += p; s_src -= 1; n += 1
    for _ in range(n):
        income += max(1, math.floor(price(base(good, dst), s_dst + 0, s_dst + 1) * BUYBACK)) if False else \
            max(1, math.floor(price(base(good, dst), optimal(good, dst), s_dst + 1) * BUYBACK))
        s_dst += 1
    return n, cost, income


def report():
    print(f"{'상품':6s} {'산지':10s} {'→ 판매처':12s} {'거리':>3s} {'등짐(100냥)':>14s} {'손수레(400냥)':>15s}")
    rows = []
    for good, (w, b, ppm, origin) in GOODS.items():
        for dst in TOWNS:
            if dst == origin:
                continue
            n1, c1, i1 = trip(good, origin, dst, 10, 100)
            n2, c2, i2 = trip(good, origin, dst, 30, 400)
            rows.append((good, origin, dst, dist(origin, dst), i1 - c1, n1, i2 - c2, n2))
    for g, o, d, k, p1, n1, p2, n2 in rows:
        print(f"{g:6s} {o:10s} → {d:10s} {k:3d} {p1:6d}냥({n1:2d}개) {p2:8d}냥({n2:2d}개)")
    return rows


if __name__ == "__main__":
    report()
