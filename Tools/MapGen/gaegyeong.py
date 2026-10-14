"""고려의 수도 개경. 남문으로 들어와 큰길을 따라 올라가면 만월대, 가로지르는 시전 거리."""
from tiles import Map, K, PROPS

WALL_TOP = K("house", 10, 8)
WALL_BOTTOM = K("house", 10, 9)


def build():
    W, H = 64, 46
    m = Map("Gaegyeong", "개경", W, H, seed=21, theme="town")
    gate_x0, gate_x1 = 30, 33
    wall_y = 40

    # 큰길 (남문 → 만월대), 시전 거리 (동서)
    m.road_rect(gate_x0, 12, gate_x1, H - 1, grade=3)
    m.road_rect(4, 22, W - 1, 24, grade=3)
    # 골목
    m.road_rect(12, 12, 13, 38, grade=2)
    m.road_rect(50, 12, 51, 38, grade=2)
    m.road_rect(12, 31, 51, 32, grade=2)
    m.road_rect(12, 12, 51, 13, grade=2)

    # 성벽과 남문
    for x in range(W):
        if gate_x0 <= x <= gate_x1:
            continue
        m.obj[wall_y][x] = WALL_TOP
        m.obj[wall_y + 1][x] = WALL_BOTTOM
        m.blocked[wall_y][x] = m.blocked[wall_y + 1][x] = True
        m.reserved[wall_y][x] = m.reserved[wall_y + 1][x] = True
    m.reserve(0, wall_y + 2, W - 1, H - 1)  # 성 밖은 길과 풀밭만

    # 만월대: 송악산 아래 궁궐
    m.plateau(20, 0, 43, 10, opening=(gate_x0, gate_x1))   # 송악산 아래 높은 터
    m.stamp("hall", 28, 1, force=True)
    m.stamp("hall", 32, 1, force=True)
    m.stamp("house_tile", 23, 5, force=True)
    m.stamp("house_tile", 37, 5, force=True)
    m.stamp("cherry", 25, 1, force=True)
    m.stamp("cherry", 38, 1, force=True)
    m.road_rect(gate_x0, 8, gate_x1, 11, grade=3)   # 만월대 오르는 길

    # 시전 거리 가게 (북쪽 줄, 남쪽 줄)
    x = 5
    north_shops = ["shop", "store", "shop", "house_tile", "store", "shop"]
    for i, s in enumerate(north_shops):
        w = 3 if s == "shop" else 4
        if x + w > gate_x0 - 1:
            break
        m.stamp(s, x, 18, force=True); x += w + 1
    x = gate_x1 + 2
    for s in ["store", "shop", "store", "shop", "house_tile", "shop"]:
        w = 3 if s == "shop" else 4
        if x + w > W - 3:
            break
        m.stamp(s, x, 18, force=True); x += w + 1
    x = 5
    for s in ["shop", "house_tile", "shop", "store", "shop"]:
        w = 3 if s == "shop" else 4
        if x + w > gate_x0 - 1:
            break
        m.stamp(s, x, 26, force=True); x += w + 1
    x = gate_x1 + 2
    for s in ["shop", "store", "shop", "house_tile", "store"]:
        w = 3 if s == "shop" else 4
        if x + w > W - 3:
            break
        m.stamp(s, x, 26, force=True); x += w + 1

    # 주택가 (기와집)
    for (hx, hy) in [(3, 14), (17, 14), (22, 14), (40, 14), (45, 14), (55, 14),
                     (3, 34), (17, 34), (22, 34), (37, 34), (42, 34), (55, 34)]:
        m.stamp("house_tile", hx, hy)
    m.stamp("house3", 56, 3); m.stamp("house3", 4, 4); m.stamp("house_tile", 14, 4); m.stamp("house_tile", 45, 4)

    # 장터 짐과 수레
    for (px, py, k) in [(28, 21, 0), (27, 25, 1), (35, 21, 2), (36, 25, 3), (26, 21, 1), (37, 25, 0)]:
        if not m.reserved[py][px]:
            m.prop(PROPS[k], px, py)
    m.stamp("cart", 46, 29)

    # 남문 망루
    for tx in (gate_x0 - 2, gate_x1 + 1):
        for dy in range(2):
            for dx in range(2):
                m.high[wall_y - 1 + dy][tx + dx] = None
                m.obj[wall_y - 1 + dy][tx + dx] = K("towers", 12 + dx, 0 + dy)
                m.blocked[wall_y - 1 + dy][tx + dx] = True
    # 사람

    m.add("npc", 28, 26, npc="gaekju")                 # 객주: 남문에서 올라오면 바로 보이는 곳
    m.add("market", 36, 21, town="gaegyeong")          # 시전 상인
    m.link("S", gate_x0, 4, "Route01", "N", "S")      # 남문 (폭 4) ↔ 예성 길 북쪽
    m.link("E", 22, 3, "Dakmok", "W", "E")            # 시전 거리 동쪽 끝 (폭 3) ↔ 닥나무 마을
    m.sign(29, 38, "↑ 만월대 · ← 서문 · 시전 거리 →\n동문 밖 닥나무 마을 5리")
    villagers = [(20, 23, 0), (40, 24, 1), (31, 15, 2), (44, 22, 0), (16, 32, 1), (47, 32, 2), (25, 12, 0), (38, 13, 1)]
    for i, (vx, vy, sp) in enumerate(villagers):
        m.add("villager", vx, vy, variant=sp, idx=i)

    # 나무와 숲 (성 안 가로수, 바깥 숲)
    for tx in range(15, 50, 7):
        if not (gate_x0 - 2 <= tx <= gate_x1 + 1):
            m.stamp("cherry", tx, 9)
    m.forest_border(thickness=2)
    m.scatter("pine", 6, area=(2, 2, W - 3, 11), margin=1)
    m.scatter("tree", 6, area=(2, 33, W - 3, 39), margin=0)
    m.finish()
    return m
