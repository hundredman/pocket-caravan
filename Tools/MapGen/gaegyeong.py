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
    m.roughen(4, 22, W - 1, 24, grade=3, every=6)     # 시전 거리 가장자리를 덜 반듯하게
    # 북쪽 산록 길은 궁성 서편을 돌아 마을로 이어진다.
    m.road_path([(8, 0), (8, 9), (11, 12)], width=3, grade=1)
    m.link("N", 8, 3, "goryeo_songak", "S", "N")
    # 골목: 구불구불, 시전 거리에서 주택가로
    m.road_path([(5, 13), (12, 12), (20, 13), (29, 12)], width=2, grade=2)
    m.road_path([(34, 12), (42, 13), (51, 12), (58, 13)], width=2, grade=2)
    m.road_path([(5, 32), (14, 31), (22, 32), (29, 31)], width=2, grade=2)
    m.road_path([(34, 31), (44, 32), (52, 31), (58, 32)], width=2, grade=2)
    for (x, top, bottom) in [(15, 13, 22), (47, 13, 22)]:      # 시전 거리와 이어지는 샛길
        m.road_path([(x, top), (x + 1, (top + bottom) // 2), (x, bottom)], width=2, grade=2)
    for (x, top, bottom) in [(17, 24, 31), (45, 24, 31)]:
        m.road_path([(x, top), (x - 1, (top + bottom) // 2), (x, bottom)], width=2, grade=2)
    m.road_blob(31.5, 23, 7, 3.2, grade=3, jitter=0.1)   # 남문 큰길과 시전 거리가 만나는 장터 마당

    # 동쪽 물류 마당: 시전과 생산 마을 사이에서 짐을 내려 분류하는 공간.
    # 새 경제 기능이 아니라, 기존 교역로의 용도를 읽게 하는 프로토타입 배치다.
    m.road_path([(50, 23), (50, 30), (45, 32)], width=2, grade=2)
    layout_rng = m.rng.getstate()
    m.road_blob(52, 31, 4.2, 1.8, grade=2, jitter=0.05)
    m.rng.setstate(layout_rng)  # 새 마당 때문에 기존 가게·식생 난수열까지 바꾸지 않는다.
    m.stamp("house_tile", 54, 27, force=True)
    m.door_path(54, 27, 4, 3, 31)

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
    m.road_path([(29, 8), (35, 8)], width=1, grade=3)  # 전각 앞에서 중앙 진입로로 합류

    # 시전 거리 가게 (북쪽 줄, 남쪽 줄)
    def row(names, x, y, x_end):
        """가게를 줄지어 놓되, 샛길·마당이 있으면 건너뛴다"""
        for name in names:
            w = 3 if name == "shop" else 4
            while x + w <= x_end and not m.stamp(name, x, y + m.rng.choice([0, 0, -1]), force=True) \
                    and not m.stamp(name, x, y, force=True):
                x += 1
            if x + w > x_end:
                break
            x += w + m.rng.choice([1, 1, 2])
    row(["shop", "store", "shop", "house_tile", "store", "shop"], 4, 18, gate_x0 - 2)
    row(["store", "shop", "store", "shop", "house_tile", "shop"], gate_x1 + 3, 18, W - 3)
    row(["shop", "house_tile", "shop", "store", "shop"], 4, 26, gate_x0 - 2)
    row(["shop", "store", "shop", "house_tile", "store"], gate_x1 + 3, 26, W - 3)

    # 주택가 (기와집)
    for (hx, hy) in [(3, 15), (18, 14), (23, 15), (38, 15), (42, 14), (54, 15),
                     (4, 34), (19, 34), (24, 35), (37, 35), (41, 34), (54, 34)]:
        m.stamp("house_tile", hx, hy)
    m.scatter("cherry", 4, area=(3, 14, W - 4, 17), margin=1)
    m.scatter("tree", 5, area=(3, 33, W - 4, 38), margin=1)
    m.bushes([(x, y) for x in range(4, W - 4, 4) for y in (16, 29, 36)])
    m.flowers(3, 13, W - 4, 38, 30)
    m.stamp("house3", 56, 3); m.stamp("house3", 4, 4); m.stamp("house_tile", 14, 4); m.stamp("house_tile", 45, 4)

    # 장터 짐과 수레
    for (px, py, k) in [(28, 21, 0), (27, 25, 1), (35, 21, 2), (36, 25, 3), (26, 21, 1), (37, 25, 0)]:
        if not m.reserved[py][px]:
            m.prop(PROPS[k], px, py)
    m.stamp("cart", 46, 29)

    # 남문: 기존의 판타지 조각상 타일 대신 성벽 기둥과 통행 가능한 기와 처마.
    # 실존 개성 남대문의 복원이 아닌, 기존 자산을 재조합한 시각 비교안이다.
    for tx in (gate_x0 - 2, gate_x1 + 1):
        for dy in range(2):
            for dx in range(2):
                m.high[wall_y - 1 + dy][tx + dx] = None
                m.obj[wall_y - 1 + dy][tx + dx] = WALL_TOP if dy == 0 else WALL_BOTTOM
                m.blocked[wall_y - 1 + dy][tx + dx] = True
                m.reserved[wall_y - 1 + dy][tx + dx] = True
    for dx, sx in enumerate((12, 13, 14, 13, 14, 15)):
        for dy in range(2):
            m.high[wall_y - 2 + dy][gate_x0 - 1 + dx] = K("house", sx, dy, "slate")
            m.reserved[wall_y - 2 + dy][gate_x0 - 1 + dx] = True

    # 물건과 작업자를 한 장소에 묶어 수도의 물류 동선을 보여 준다.
    m.stamp("cart", 58, 34, force=True)
    for px, py, prop in [(48, 28, 0), (48, 29, 1), (53, 27, 2), (58, 30, 0), (58, 31, 1)]:
        if not m.obj[py][px] and not m.high[py][px] and not m.road[py][px]:
            m.prop(PROPS[prop], px, py)
    m.add("worker", 50, 25, name="시전 짐꾼", character='NPC/npc_capital_porter/npc_capital_porter_tall',
          lines=["동문 쪽 짐은 이 마당에서 나눠 싣소. 종이는 닥나무 마을에서 들어오고, 다른 짐은 나루 길로 나가지.",
                 "장터는 이 길 서쪽이오. 한꺼번에 많이 팔면 값이 내려가니, 몇 개를 팔지 먼저 살펴보시오."],
          path=[(50, 25), (50, 30)])
    m.add("worker", 38, 23, name="시전 행상", character='NPC/npc_capital_peddler/npc_capital_peddler_tall',
          lines=["나루에서 소금을 가져왔소? 여기서 팔 값과 돌아갈 때 살 물건을 함께 살펴보시오.",
                 "나는 남이 알려 준 값보다 직접 본 장부를 먼저 보오. 그 값도 다음번에는 달라질 수 있지."],
          path=[(38, 23), (45, 23)])
    # 사람

    m.add("npc", 28, 26, npc="gaekju")                 # 객주: 남문에서 올라오면 바로 보이는 곳
    m.add("market", 36, 21, town="gaegyeong")          # 시전 상인
    m.board(25, 25, "gaegyeong")                       # 객주 옆 의뢰 게시판
    m.walker([(14, 23), (24, 23), (29, 23)], idx=2)    # 시전 거리를 오가는 NPC 상인
    m.walker([(32, 30), (32, 37), (32, 44)], idx=3)
    m.link("S", gate_x0, 4, "Route01", "N", "S")      # 남문 (폭 4) ↔ 예성 길 북쪽
    m.link("E", 22, 3, "Dakmok", "W", "E")            # 시전 거리 동쪽 끝 (폭 3) ↔ 닥나무 마을
    m.sign(29, 36, "개경 남문 안\n↑ 시전 장터 · 객주 · 의뢰 게시판\n시전에서 동쪽으로 가면 닥나무 마을, 남문 밖은 나루 가는 길.")
    m.sign(34, 26, "시전 갈림길\n↑ 궁성 앞마당 · → 동문과 닥나무 마을\n← 객주와 게시판 · ↓ 남문과 예성 나루")
    m.sign(52, 25, "동문 짐 마당\n← 개경 장터 · → 닥나무 마을\n종이와 생활재를 실어 나르는 길. 적치된 짐은 통행로 밖에 두자.")
    m.sign(34, 11, "궁성 앞마당\n↓ 시전 거리 · 남문\n높은 터에서 내려다보면 시전과 남문으로 이어지는 큰길이 보인다.")
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
