"""베네치아: 수도 1 + 마을 4가 화면 전환 없이 걸어서 이어지는 하나의 맵 (설계 제안 초안).
기준 설계: Downloads/Venice_World_Claude_Pack (01_venice_world_plan.png, 04_route_plan.json).
    가운데 수도(리알토) · 북서 과수원(산테라스모) · 북동 유리공방(무라노) · 남서 어촌(부라노) · 남동 교역항(키오자)
    보행 연결: 수도↔네 마을(넓은 다리), 과수원↔유리공방·과수원↔어촌·유리공방↔교역항(외곽 둑길, 좁은 다리)
지명은 실제 지명을 빌린 게임 각색이며 무라노·산테라스모·부라노는 제안이다. 인물·대사는 창작이다.
실제 역사 배치와 다르다(키오자는 석호 남쪽 끝, 무라노·부라노·산테라스모는 북쪽). 실제로는 배로 다녔고 다리·둑길은 각색이다.
건물·소품·바닥 무늬 'vg_*'는 GPT 생성 임시 아트(Tools/ArtGen/venice_atlas.png). 팀 아트 승인 전이다.
배치 원칙: 건물 3~5채를 붙여 묶고 그 사이 골목, 좁은 골목 끝에 작은 광장. 건물은 그림이 실제로 차 있는 칸만 막는다.
다른 문명으로 가는 출구는 연결표 합의 전까지 만들지 않는다."""
from venice_tiles import VeniceMap
import tiles

W, H = 112, 84

# 구역 이름 (위에 있는 것이 먼저). 어느 구역에도 들지 않는 칸은 '석호 길'.
DISTRICTS = [
    ("리알토", 38, 16, 73, 57),
    ("리알토 남쪽 수변", 28, 58, 85, 72),
    ("산테라스모 과수원", 0, 6, 33, 31),
    ("무라노 유리공방", 80, 6, 111, 31),
    ("부라노 어촌", 0, 47, 33, 83),
    ("키오자 교역항", 80, 47, 111, 83),
]
DEFAULT_DISTRICT = "석호 길"


def land(m, x0, y0, x1, y1, zone):
    """물을 메워 땅으로 (구역 바닥 지정)"""
    for y in range(max(0, y0), min(m.h, y1 + 1)):
        for x in range(max(0, x0), min(m.w, x1 + 1)):
            m.water[y][x] = False
            m.reserved[y][x] = False
            m.zone[y][x] = zone


def island(m, cx, cy, rx, ry, zone, jitter=0.12):
    """들쭉날쭉한 자연 섬"""
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if m.inside(x, y) and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1 + m.rng.uniform(-jitter, jitter):
                land(m, x, y, x, y, zone)


def water(m, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if m.inside(x, y):
                m.water[y][x] = True


def trees(m, items):
    """나무는 물가·건물과 겹치면 건너뛴다 (군집 위치만 정하고 칸은 유연하게)."""
    for name, x, y in items:
        try:
            m.building(name, x, y, high=2)
        except ValueError:
            pass


def reuse(m, items):
    """기존 에셋 재활용: 자리가 맞으면 놓고, 겹치면 건너뛴다 (억지로 넣지 않는다)."""
    placed = []
    for name, x, y, *high in items:
        try:
            m.building(name, x, y, high=high[0] if high else 0)
            placed.append(name)
        except ValueError:
            pass
    return placed


def swap(m, new, old, x, y, high=0):
    """새 마을 시트 건물을 우선 놓고, 자리가 맞지 않으면 기존 건물을 쓴다."""
    try:
        m.building(new, x, y, high=high)
    except ValueError:
        m.building(old, x, y, high=high)


def put(m, items):
    """[(묶음, x, y), ...] 막는 소품. 이미 놓인 것과 겹치면 놓지 않는다.
    2026-10-26: 그림이 실제로 덮은 칸만 막도록 building()으로 놓는다(투명한 가장자리가 골목을 막지 않게). 안 맞으면 예전 방식."""
    for name, x, y in items:
        try:
            m.building(name, x, y, high=tiles.STAMPS[name][6])
        except (ValueError, IndexError):
            m.stamp(name, x, y, force=True)


def row(m, x, y, parts):
    """운하 집(vh_*)을 왼쪽부터 벽을 맞대고 줄지어 놓는다. parts: [(종류, 벽색), ...] (2026-10-26)"""
    import tiles
    for kind, color in parts:
        name = f"vh_{kind}_{color}"
        m.building(name, x, y, high=1)
        x += tiles.STAMPS[name][3]


def boats(m, items):
    for name, x, y in items:
        m.stamp(name, x, y, force=True, over_road=True)


def capital(m):
    """수도(리알토): 대성당·종탑이 북쪽을, 궁전이 남쪽을 막는 큰 광장. 운하 쪽 입구에 사자·성인 기둥(피아체타 각색).
    광장 가운데 화려한 우물과 모자이크, 가장자리 비단 노점. 서쪽·남쪽 주택 골목과 안뜰, 운하 동쪽 상인회관·여관."""
    land(m, 38, 24, 73, 58, "street")
    water(m, 57, 24, 59, 58)                       # 남북 운하
    m.set_zone(55, 24, 56, 58, "quay")
    m.set_zone(60, 24, 61, 58, "quay")
    m.bridge(55, 33, 61, 35)                       # 광장 ↔ 동쪽 상인회관
    m.bridge(55, 48, 61, 50)
    # ---- 광장 (x43~54, y31~42) ----
    m.set_zone(43, 31, 54, 43, "court")
    m.set_zone(47, 35, 50, 38, "mosaic")
    m.building("vg_cp_cathedral", 45, 24)          # 북쪽: 대성당 (문은 광장으로)
    m.building("vg_cp_tower", 52, 24, high=5)      # 종탑은 대성당 옆에 붙인다
    row(m, 54, 25, [("narrow_tall", "rose")])
    put(m, [("vg_cp_well", 48, 36)])               # 광장 중심
    put(m, [("vg_cp_brazier", 45, 32), ("vg_cp_brazier", 52, 32)])            # 성당 앞 화로
    put(m, [("vg_cp_lion_column", 54, 37), ("vg_cp_saint_column", 54, 41)])   # 운하 쪽 두 기둥
    # 가장자리: 서쪽 벤치·화분, 남쪽 비단 노점 줄과 비단 탁자·태피스트리
    put(m, [("vg_cp_bench", 43, 34), ("vg_cp_planter", 43, 37)])
    put(m, [("vg_cp_silk_stalls", 43, 41), ("vg_cp_silk_table", 50, 42), ("vg_cp_tapestry", 52, 40)])
    m.add("market", 46, 40, town="venice_rialto")
    m.building("vg_cap_board", 50, 32)
    m.add("board", 51, 34, town="venice_rialto")
    # ---- 남쪽: 궁전과 그 서쪽 안뜰 ----
    m.building("vg_cp_palace", 44, 44)
    m.set_zone(38, 42, 43, 50, "court")
    row(m, 38, 42, [("narrow_tall", "ochre")])
    put(m, [("vg_laundry", 40, 44), ("vg_well", 41, 47), ("vg_pot_plant", 38, 48), ("vg_cp_pennant", 52, 51)])
    # ---- 서쪽 주택 골목 (북서 모서리와 y51~52 골목은 둑길 입구로 비운다) ----
    row(m, 38, 32, [("narrow_tall", "salmon"), ("house_tall", "cream")])
    row(m, 38, 37, [("narrow_tall", "brick")])
    put(m, [("vg_flower_pot2", 41, 38), ("vg_cp_planter", 40, 40)])
    row(m, 38, 54, [("palazzo", "rose")])
    row(m, 44, 54, [("narrow", "sky"), ("shop", "ochre")])
    row(m, 49, 53, [("narrow_tall", "salmon"), ("narrow_tall", "cream"), ("house_tall", "brick")])
    # ---- 운하 동쪽: 상인회관, 여관, 주택 묶음 ----
    m.building("vg_cap_inn", 62, 25)               # 북동 모서리(67~73, 24~29)는 유리공방 둑길 입구
    m.set_zone(62, 30, 72, 35, "court")
    put(m, [("vg_well", 66, 31), ("vg_flower_shelf", 70, 32), ("vg_cp_bench", 63, 33)])
    m.building("vg_cap_hall", 63, 37)              # 상인회관
    row(m, 69, 37, [("narrow_tall", "ochre"), ("narrow_tall", "rose")])
    put(m, [("vg_px_cap_flag_red", 62, 41), ("vg_px_cap_flag_blue", 69, 42)])
    row(m, 62, 44, [("narrow", "cream"), ("shop", "salmon")])
    row(m, 67, 44, [("narrow_tall", "brick")])
    row(m, 70, 44, [("house_tall", "ochre")])     # 69열은 파란 깃발 자리
    row(m, 62, 51, [("narrow_tall", "rose")])
    row(m, 66, 52, [("wide_shop", "cream"), ("narrow", "sage")])
    boats(m, [("vg_gondola", 57, 28), ("vg_cp_mooring", 57, 38), ("vg_cp_mooring", 59, 41), ("vg_gondola", 57, 44),
              ("vg_gondola", 57, 54)])
    reuse(m, [("vg_shop_sign", 61, 29), ("vg_cp_planter", 66, 35), ("vg_amphora", 72, 50)])
    m.add("npc", 60, 30, npc="venice_innkeeper")   # 퀘스트 NPC (venice.json npcs)
    m.add("npc", 46, 33, npc="venice_clerk")   # 퀘스트 NPC (venice.json npcs)
    m.add("spawn", 49, 39, id="S0")              # 베네치아 씬 직접 시험용 시작 자리


def links(m):
    """수도↔네 마을 둑길(넓은 다리)과 외곽 둑길(좁은 다리)"""
    # 수도 ↔ 과수원 (북서)
    land(m, 22, 26, 37, 29, "street"); m.set_zone(22, 26, 32, 29, "meadow"); water(m, 33, 26, 35, 29)
    m.road_rect(30, 27, 37, 28, grade=3)
    m.bridge(32, 26, 36, 29)
    # 수도 ↔ 유리공방 (북동)
    land(m, 74, 26, 82, 29, "street"); water(m, 77, 26, 79, 29)
    m.road_rect(74, 27, 82, 28, grade=3)
    m.bridge(76, 26, 80, 29)
    # 수도 ↔ 어촌 (남서)
    land(m, 22, 50, 37, 53, "street"); m.set_zone(22, 50, 32, 53, "meadow"); water(m, 33, 50, 35, 53)
    m.road_rect(30, 51, 37, 52, grade=3)
    m.bridge(32, 50, 36, 53)
    # 수도 ↔ 교역항 (남동)
    land(m, 74, 50, 82, 53, "street"); water(m, 77, 50, 79, 53)
    m.road_rect(74, 51, 82, 52, grade=3)
    m.bridge(76, 50, 80, 53)
    # 외곽: 과수원 ↔ 유리공방 (북쪽 모래톱)
    land(m, 14, 1, 97, 4, "meadow")
    water(m, 54, 0, 57, 5)
    m.road_rect(14, 2, 53, 2, grade=1); m.road_rect(58, 2, 97, 2, grade=1)
    m.bridge(53, 1, 58, 3)                          # 모래톱 사이 목교 (양쪽 땅에 한 칸씩 걸친다)
    # 외곽: 과수원 ↔ 어촌 (서쪽 모래톱)
    land(m, 1, 30, 5, 48, "meadow")
    water(m, 0, 38, 5, 40)
    m.road_rect(3, 30, 3, 37, grade=1); m.road_rect(3, 41, 3, 48, grade=1)
    m.bridge(2, 37, 4, 41)
    # 외곽: 유리공방 ↔ 교역항 (동쪽 모래톱)
    land(m, 106, 30, 110, 48, "meadow")
    water(m, 106, 38, 111, 40)
    m.road_rect(108, 30, 108, 37, grade=1); m.road_rect(108, 41, 108, 48, grade=1)
    m.bridge(107, 37, 109, 41)
    # 연결로 생활감: 과수원 쪽엔 과일 상자·나무, 유리공방 쪽엔 유리 조각 상자, 어촌 쪽엔 그물, 항구 쪽엔 화물
    put(m, [("vg_lemon_crate", 24, 3), ("vg_gls_cullet", 86, 3), ("vg_px_cap_stone_bench", 44, 3),
            ("vg_net_rack", 1, 42), ("vg_px_fsh_traps", 1, 44), ("vg_prt_cargo", 109, 43), ("vg_barrel", 109, 34)])
    trees(m, [("vg_cypress", 30, 1), ("vg_olive", 64, 1), ("vg_cypress", 1, 33), ("vg_cypress", 110, 31)])
    for cx, cy in [(38, 4), (70, 4), (4, 36), (106, 44)]:
        m.cluster_deco(["vg_reeds", "vg_rocks", "vg_tuft"], cx, cy, 2, 2)


def orchard(m):
    """산테라스모 과수원(제안): 낮은 집, 포장 창고 앞 작은 수확 마당, 나무 군집 사이 굽은 흙길, 생울타리 문과 벌통."""
    island(m, 17, 17, 15, 13, "meadow")
    land(m, 14, 4, 20, 8, "meadow")                  # 북쪽 모래톱으로 이어짐
    land(m, 1, 26, 8, 31, "meadow")                  # 서쪽 모래톱으로 이어짐
    # 굽은 흙길: 수도 둑길 → 수확 마당 → 집들 → 북쪽·서쪽 모래톱
    m.road_path([(30, 27), (26, 25), (22, 21), (17, 20)], width=2, grade=2)
    m.road_path([(17, 20), (12, 18), (9, 14), (11, 12), (11, 6), (17, 6), (17, 4)], width=1, grade=1)   # 생울타리 문을 지난다
    m.road_path([(12, 18), (8, 22), (5, 26), (3, 30)], width=1, grade=1)
    m.set_zone(14, 18, 22, 21, "dirt")              # 수확 마당
    # 집과 창고 (낮고 넓은 지붕)
    swap(m, "vg_n_orc_shed", "vg_orc_shed", 15, 14)               # 포장 창고: 앞마당에서 과일을 고르고 싣는다
    swap(m, "vg_n_orc_farmhouse", "vg_orc_house", 4, 11)
    swap(m, "vg_n_orc_cottage", "vg_orc_cottage", 20, 9)
    m.building("vg_orc_house", 24, 14)
    # 장면 1: 수확 마당 — 건조대·압착기·과일 수레·상자
    put(m, [("vg_px_orc_drying_rack", 21, 15), ("vg_px_orc_press", 21, 18), ("vg_orc_cart", 13, 21),
            ("vg_lemon_crate", 23, 18)])
    m.add("market", 19, 18, town="venice_santerasmo")
    m.board(13, 16, "venice_santerasmo")
    # 장면 2: 남쪽 과수 군집과 사다리 바구니
    trees(m, [("vg_n_orc_olive", 2, 17), ("vg_n_orc_lemon", 6, 19), ("vg_n_orc_fig", 9, 23), ("vg_px_orc_lemon_tree2", 12, 22),
              ("vg_olive", 21, 24), ("vg_tree", 25, 21)])
    put(m, [("vg_px_orc_ladder_basket", 10, 21)])
    # 장면 3: 생울타리 문 안쪽 정원과 벌통
    m.building("vg_px_orc_hedge_gate", 10, 7, high=2, open_cells={(1, 2)})
    put(m, [("vg_px_orc_beehive", 13, 8), ("vg_px_orc_beehive", 14, 10), ("vg_wall_low", 7, 9)])
    trees(m, [("vg_cypress", 7, 6), ("vg_tree", 14, 5), ("vg_n_orc_olive", 25, 7), ("vg_n_orc_olive", 27, 21), ("vg_olive", 25, 8)])
    for area in [(2, 17, 15, 26), (19, 20, 29, 26), (6, 5, 16, 11), (22, 6, 30, 12)]:
        m.lush_around(*area)
    reuse(m, [("vg_n_orc_vegbed", 22, 22), ("vg_n_orc_hay", 13, 13), ("vg_n_orc_wall", 2, 15), ("vg_n_orc_wall", 7, 16), ("vg_n_orc_wallcorner", 26, 11),
              ("vg_house_ochre", 27, 18), ("vg_trellis", 25, 11), ("vg_flower_box", 20, 13), ("vg_pergola", 2, 21, 3)])
    m.add("npc", 16, 22, npc="venice_gardener")   # 퀘스트 NPC (venice.json npcs)


def glassworks(m):
    """무라노 유리공방(제안): 운하 선착장에서 모래 수레 → 작업대 → 가마 → 냉각 선반 → 진열대로 이어지는 작업 마당.
    짧은 운하 건너 청록 벽 장인 주택 골목. 모서리는 물로 깎아 큰 사각 섬을 피한다."""
    land(m, 82, 5, 106, 30, "street")
    land(m, 88, 3, 96, 5, "street")                  # 북쪽 모래톱과 이어지는 선착장 길
    water(m, 82, 5, 85, 8); water(m, 101, 5, 106, 7); water(m, 103, 27, 105, 30)   # 둥근 모서리와 작은 만
    m.set_zone(82, 16, 92, 25, "dirt")              # 흙 작업 마당
    m.set_zone(84, 15, 92, 16, "market")            # 공방·가마 앞에만 벽돌 띠
    water(m, 94, 8, 95, 30)                         # 짧은 운하
    m.set_zone(93, 5, 96, 30, "quay")
    m.bridge(93, 19, 96, 21)
    swap(m, "vg_n_gls_workshop", "vg_gls_workshop", 83, 10)
    swap(m, "vg_n_gls_kilnshed", "vg_gls_kiln", 89, 11)
    # 원료 반입 → 전시: 모래 수레(운하 쪽) → 작업대 → 야외 가마 → 냉각 선반 → 진열대(장터 앞)
    put(m, [("vg_px_gls_sand_cart", 91, 17), ("vg_px_gls_craft_bench", 87, 18), ("vg_px_gls_molds", 86, 17),
            ("vg_px_gls_cooling", 83, 22), ("vg_px_gls_bottle_rack", 87, 23), ("vg_px_gls_ornaments", 90, 23)])
    m.building("vg_gls_furnace", 83, 18)
    put(m, [("vg_gls_cullet", 91, 21)])
    m.add("market", 89, 21, town="venice_murano")
    m.board(86, 25, "venice_murano")
    boats(m, [("vg_fishing_boat", 94, 12)])
    # 장인 주택 골목
    swap(m, "vg_n_gls_shop", "vg_gls_artisan", 97, 8)
    row(m, 101, 9, [("narrow_tall", "brick")])
    m.building("vg_gls_row", 97, 14)
    m.building("vg_gls_artisan", 102, 14)
    m.building("vg_gls_row", 97, 24)
    row(m, 102, 21, [("narrow_tall", "ochre")])
    put(m, [("vg_flower_pot2", 100, 19)])           # x104~106은 동쪽 모래톱(키오자 방면)으로 가는 수레 길
    reuse(m, [("vg_n_gls_sand", 91, 15), ("vg_n_gls_wood", 82, 15), ("vg_n_gls_blowbench", 84, 25), ("vg_n_gls_bowls", 99, 12),
              ("vg_n_gls_cups", 86, 22), ("vg_stall_glass", 82, 24), ("vg_glass_workshop", 97, 19)])
    m.add("npc", 85, 16, npc="venice_glassmaker")   # 퀘스트 NPC (venice.json npcs)


def fishing(m):
    """부라노 어촌(제안): 물가를 따라 길게 이어지는 작업 공간. 집 앞 닳은 돌길에서
    그물 손질 → 생선 판매(잔교 옆) → 배 수리로 이어진다. 뒤쪽은 풀밭과 작은 집, 나무 그늘."""
    island(m, 16, 61, 14, 12, "meadow")
    land(m, 1, 46, 5, 68, "meadow")                  # 서쪽 모래톱 (과수원 쪽)
    land(m, 4, 66, 28, 71, "worn")                   # 물가 닳은 돌길
    water(m, 0, 72, 33, 83)                          # 남쪽 물가는 곧게
    # 길: 수도 둑길 → 집 사이 골목 → 물가 / 서쪽 모래톱 → 물가
    m.road_path([(30, 52), (20, 52), (20, 66)], width=2, grade=2)
    m.road_path([(3, 48), (3, 66), (5, 66)], width=1, grade=1)
    # 물가를 향한 집 줄 (문은 남쪽 돌길로). x19~20은 수도에서 내려오는 골목
    swap(m, "vg_n_fsh_house", "vg_fsh_house", 4, 62)
    m.building("vg_fsh_small", 9, 62)
    swap(m, "vg_n_fsh_netshed", "vg_fsh_netshed", 14, 62)
    swap(m, "vg_n_fsh_shelter", "vg_fsh_shelter", 21, 61)
    # 뒤쪽: 작은 집과 나무 그늘, 그물 건조대
    m.building("vg_fsh_house", 8, 52)
    trees(m, [("vg_olive", 14, 51), ("vg_tree", 3, 56), ("vg_cypress", 26, 56)])
    put(m, [("vg_net_rack", 14, 56), ("vg_barrel", 7, 57)])
    m.lush_around(12, 50, 18, 58)
    m.lush_around(2, 54, 7, 60)
    # 장면 1: 그물 손질 (집 앞)
    put(m, [("vg_px_fsh_net_mend", 5, 67), ("vg_px_fsh_traps", 9, 67)])
    # 장면 2: 생선 판매 (잔교 옆). 어부 집 앞까지 수레가 지나가도록 x11~12는 비운다.
    put(m, [("vg_px_fsh_fish_stall", 15, 67), ("vg_px_fsh_catch", 9, 70)])
    m.add("market", 14, 69, town="venice_burano")
    m.board(13, 66, "venice_burano")
    # 장면 3: 배 수리 (수리막 앞)
    put(m, [("vg_px_prt_hull_cradle", 22, 66), ("vg_planks", 25, 68), ("vg_rope", 27, 67),
            ("vg_px_fsh_fish_dry", 6, 70)])
    # 길이가 다른 잔교와 계류 말뚝
    m.bridge(18, 72, 19, 75, dock=True)
    m.bridge(10, 72, 11, 77, dock=True)
    m.bridge(24, 72, 25, 74, dock=True)
    put(m, [("vg_px_fsh_mooring", 21, 71)])
    boats(m, [("vg_fsh_dinghy", 12, 75), ("vg_fsh_dinghy", 20, 76), ("vg_fishing_boat", 26, 76)])
    reuse(m, [("vg_n_fsh_boat", 29, 66), ("vg_n_fsh_boat", 1, 64), ("vg_n_fsh_netpile", 12, 67), ("vg_n_fsh_traps", 24, 70), ("vg_n_fsh_fishrack", 6, 58),
              ("vg_n_fsh_driftwood", 28, 63), ("vg_n_fsh_anchor", 27, 69), ("vg_anchor", 27, 70), ("vg_crate_fish", 2, 69), ("vg_ladder_rope", 2, 66)])
    m.add("npc", 8, 69, npc="venice_fisher")   # 퀘스트 NPC (venice.json npcs)
    for cx, cy in [(3, 70), (29, 64)]:
        m.cluster_deco(["vg_reeds", "vg_rocks", "vg_tuft"], cx, cy, 2, 3)


def port(m):
    """키오자 교역항: 안쪽 정박장의 배 → 승선판 → 저울 → 화물 수레 → 긴 창고 하역문.
    관문과 상인 길드가 입구를 이루고, 가운데 사람 통로는 화물 마당과 나눈다."""
    land(m, 82, 47, 106, 74, "street")
    land(m, 104, 46, 110, 50, "street")              # 동쪽 모래톱과 이어짐
    water(m, 82, 70, 86, 74)                        # 모서리를 깎아 큰 사각 섬을 피한다
    m.set_zone(82, 65, 106, 74, "quay")             # 하역 부두
    m.set_zone(84, 58, 92, 63, "dirt")              # 화물 작업 마당 (서쪽)
    water(m, 95, 66, 103, 74)                       # 안쪽 정박장
    m.building("vg_prt_gate", 83, 47)               # 수도에서 들어오는 관문
    swap(m, "vg_n_prt_guild", "vg_prt_guild", 89, 48)
    swap(m, "vg_n_prt_customs", "vg_prt_gate", 93, 48)
    swap(m, "vg_n_prt_warehouse", "vg_prt_warehouse", 85, 54)
    m.building("vg_prt_warehouse", 93, 54)
    put(m, [("vg_px_prt_trade_flag", 99, 51), ("vg_px_prt_sailcloth", 100, 55)])
    # 하역 흐름: 정박장 배 → 승선판 → 저울 → 화물 수레 → 창고 문 (사람은 x93~94 가운데 길)
    boats(m, [("vg_barge_loaded", 97, 68)])
    put(m, [("vg_px_prt_gangplank", 96, 64), ("vg_prt_crane", 100, 62), ("vg_px_prt_scale", 89, 60),
            ("vg_px_prt_cargo_cart", 86, 59), ("vg_prt_cargo", 85, 62), ("vg_sacks_row", 89, 63)])
    m.road_rect(82, 52, 100, 53, grade=3)           # 관문 → 창고 앞 수레 길 (바퀴 자국)
    m.add("market", 91, 59, town="venice_chioggia")       # 석호 소금 배가 닿는 창고 앞
    m.board(97, 52, "venice_chioggia")
    # 바깥 부두: 길이가 다른 부두와 ㄱ자 화물 부두
    m.bridge(88, 74, 89, 78, dock=True)             # ㄱ자 화물 부두: 세로 + 가로
    m.bridge(90, 77, 94, 78)
    m.bridge(104, 74, 105, 79, dock=True)
    boats(m, [("vg_barge_empty", 95, 77), ("vg_pole_striped", 102, 76)])
    reuse(m, [("vg_n_prt_bales", 98, 59), ("vg_n_prt_spice", 101, 62), ("vg_n_prt_amphorae", 85, 64), ("vg_n_prt_bollard", 94, 66),
              ("vg_fondaco", 100, 57), ("vg_jars", 92, 62), ("vg_anchor", 103, 64), ("vg_crate", 84, 65)])
    m.add("npc", 103, 61, npc="venice_warehouse")   # 퀘스트 NPC (venice.json npcs)


def fill(m, x0, y0, x1, y1, zone):
    """물 칸만 땅으로 메운다 (이미 있는 땅·건물은 건드리지 않는다)."""
    for y in range(max(0, y0), min(m.h, y1 + 1)):
        for x in range(max(0, x0), min(m.w, x1 + 1)):
            if m.water[y][x] and (x, y) not in m.decks:
                m.water[y][x] = False
                m.reserved[y][x] = False
                m.zone[y][x] = zone


def waterfront(m):
    """섬 사이를 수변 거리로 잇는다 (Venice_Waterfront_Pack 지시).
    - 수도 남쪽: 어촌 입구 → 수도 남쪽 장터 → 교역항 화물 대기장. 육지와 선착장이 번갈아 나오고,
      중앙 운하·서남·동남 정박 수로가 바다로 나가는 자리에 짧고 넓은 다리.
    - 네 연결부를 넓힌다: 과수 정원 수변길 / 공방 앞 거리 / 어시장 나루 / 하역 거리.
    - 수도 북쪽 정원 수변과 위쪽 외곽길의 폭 변화·쉼터.
    부두 그림을 늘리지 않고 목재 다리 타일로 조립한다. 큰 건물은 석축 땅 위에만 둔다."""
    # ---------- 수도 남쪽 수변 거리 ----------
    fill(m, 28, 59, 84, 63, "quay")
    fill(m, 36, 64, 50, 66, "quay")                 # 장터 앞마당 (넓게)
    fill(m, 63, 64, 74, 65, "quay")                 # 화물 대기장 (조금 좁게)
    fill(m, 80, 64, 84, 66, "street")               # 교역항 입구
    m.set_zone(28, 59, 39, 66, "worn")              # 어촌 쪽은 닳은 돌길
    m.set_zone(76, 59, 84, 66, "street")
    water(m, 33, 54, 35, 70)                        # 서남 정박 수로 → 바다
    water(m, 57, 59, 59, 83)                        # 중앙 운하 → 남쪽 외해
    water(m, 77, 54, 79, 70)                        # 동남 정박 수로 → 바다
    m.bridge(32, 59, 36, 63)
    m.bridge(55, 59, 61, 63)                        # 운하 출구의 짧고 넓은 목교
    m.bridge(76, 59, 80, 63)
    # 선착장 (길이가 서로 다르게)
    m.bridge(40, 67, 41, 71, dock=True)
    m.bridge(66, 66, 67, 69, dock=True)
    m.bridge(82, 67, 83, 72, dock=True)
    # 어촌 쪽: 그물 수선장 (수선대·통발·생선 노점·건조대)
    # 수변 거리 y60~63은 수레 길(부라노 ↔ 리알토 씬 연결)로 비우고 소품은 가장자리에 둔다.
    put(m, [("vg_px_fsh_net_mend", 28, 59), ("vg_n_fsh_netpile", 31, 59), ("vg_n_fsh_traps", 24, 57),
            ("vg_px_fsh_fish_stall", 36, 64), ("vg_px_fsh_fish_dry", 38, 64), ("vg_n_fsh_anchor", 39, 66)])
    boats(m, [("vg_fsh_dinghy", 42, 69), ("vg_px_fsh_mooring", 38, 67)])
    # 수도 남쪽 장터: 꽃수레·벤치·노점·절제된 깃발 장식 하나
    put(m, [("vg_w_flower_cart", 43, 59), ("vg_stall_red", 47, 59), ("vg_px_cap_bread_stall", 50, 59),
            ("vg_w_fruit_bench", 44, 65), ("vg_w_flower_box", 48, 65), ("vg_w_bunting", 52, 59),
            ("vg_cp_bench", 53, 63)])
    # 운하 동쪽: 대기 쉼터와 화물 대기장 (가운데 수레 길은 비운다)
    put(m, [("vg_w_shelter", 62, 59), ("vg_w_mooring_set", 64, 64), ("vg_w_office", 70, 59),
            ("vg_w_cart", 68, 64), ("vg_n_prt_bales", 72, 64), ("vg_crate", 74, 63)])
    boats(m, [("vg_w_cargo_boat", 68, 67), ("vg_gondola", 57, 66), ("vg_barge_loaded", 84, 69)])
    m.talker(46, 63, "꽃 파는 아이", "villager_rabbit_tall",
             "남쪽 물가는 장날이면 북적여요.",
             "왼쪽은 어촌, 오른쪽은 항구로 이어져요.")
    m.talker(73, 61, "부두 관리인", "villager_dog_tall",
             "화물은 저기 대기장에 내려 두시게.",
             "수레 길은 가운데로 비워 두고.")

    # ---------- 수도 ↔ 과수: 정원 수변길 (석재 → 흙길 → 풀) ----------
    fill(m, 22, 17, 37, 34, "meadow")
    fill(m, 38, 16, 54, 23, "meadow")
    m.set_zone(38, 20, 44, 23, "street")            # 수도 쪽은 석재
    water(m, 33, 24, 35, 40)                        # 남쪽에서 들어오는 수로 (북쪽은 육지로 이어짐)
    m.bridge(32, 25, 36, 29)
    m.road_path([(22, 20), (37, 20), (44, 21)], width=2, grade=1)
    trees(m, [("vg_n_orc_lemon", 40, 16), ("vg_n_orc_olive", 47, 16), ("vg_n_orc_fig", 51, 17), ("vg_cypress", 26, 31),
              ("vg_n_orc_lemon", 24, 31)])
    put(m, [("vg_w_vine_bench", 44, 17), ("vg_w_lemon_pots", 50, 21), ("vg_w_fruit_bench", 28, 33),
            ("vg_px_orc_ladder_basket", 37, 17), ("vg_lemon_crate", 31, 32)])
    m.lush_around(36, 15, 54, 23)

    # ---------- 수도 ↔ 유리공방: 공방 앞 거리 ----------
    fill(m, 72, 16, 83, 33, "street")
    m.set_zone(78, 16, 83, 33, "market")            # 공방 쪽은 벽돌
    water(m, 77, 24, 79, 40)
    m.bridge(76, 25, 80, 30)
    m.bridge(73, 12, 74, 15, dock=True)             # 작은 화물 선착장 (북쪽 만으로)
    put(m, [("vg_n_gls_cups", 72, 20), ("vg_n_gls_bowls", 74, 18), ("vg_n_gls_sand", 81, 19),
            ("vg_px_gls_sand_cart", 81, 22), ("vg_w_mooring_set", 75, 16)])
    boats(m, [("vg_w_cargo_boat", 75, 11)])
    m.talker(73, 22, "유리 상인", "villager_fox_tall",
             "공방에서 나온 그릇은 여기서 먼저 팔지.",
             "모래는 배로 들어와.")

    # ---------- 수도 ↔ 어촌 / 교역항: 넓힌 연결부 ----------
    fill(m, 22, 46, 37, 58, "worn")
    fill(m, 74, 44, 81, 58, "street")
    water(m, 33, 46, 35, 58); water(m, 77, 44, 79, 58)
    m.bridge(32, 49, 36, 53)
    m.bridge(76, 49, 80, 53)
    put(m, [("vg_n_fsh_fishrack", 25, 54), ("vg_px_fsh_catch", 29, 55), ("vg_px_fsh_traps", 24, 47)])
    put(m, [("vg_n_prt_scale", 74, 55), ("vg_px_prt_cargo_cart", 74, 46), ("vg_n_prt_spice", 81, 56)])

    # ---------- 위쪽 외곽길: 폭 변화와 중간 쉼터 ----------
    fill(m, 30, 4, 42, 8, "meadow")
    fill(m, 68, 4, 80, 8, "meadow")
    fill(m, 44, 4, 52, 7, "meadow"); fill(m, 59, 4, 63, 6, "meadow")   # 목교 양쪽 쉼터 터
    put(m, [("vg_w_shelter", 46, 4), ("vg_w_rope_rail", 60, 4)])
    trees(m, [("vg_n_orc_olive", 33, 5), ("vg_cypress", 39, 5), ("vg_n_orc_fig", 72, 5), ("vg_cypress", 78, 5)])
    m.lush_around(30, 4, 42, 8); m.lush_around(68, 4, 80, 8)


def venice_lagoon():
    """예전 한 맵 판 (분할 전 비교용). venice.json에는 등록하지 않는다."""
    m = _master()
    m.finish()
    return m


def _master():
    """석호 전체 원본. 마을별 씬은 이 원본을 잘라 만든다 (아래 '마을별 씬')."""
    m = VeniceMap("venice_lagoon", "베네치아", W, H, seed=301, zone="sand")
    m.hours = 0
    m.water_rect(0, 0, W - 1, H - 1)
    for y in range(H):
        for x in range(W):
            m.reserved[y][x] = False
    capital(m)
    links(m)
    orchard(m)
    glassworks(m)
    fishing(m)
    port(m)
    waterfront(m)
    # 지역별 바닥 무늬 (연결로는 기본 풀밭·모래)
    for style, x0, y0, x1, y1 in [("cap", 38, 14, 73, 58), ("cap", 40, 59, 75, 72), ("fsh", 22, 46, 39, 72),
                                  ("prt", 74, 44, 85, 72), ("orc", 22, 15, 37, 34), ("gls", 76, 10, 83, 33), ("orc", 0, 4, 33, 31), ("gls", 80, 3, 107, 31),
                                  ("fsh", 0, 46, 33, 83), ("prt", 80, 44, 111, 83)]:
        m.set_style(x0, y0, x1, y1, style)
    # 물 위에 떨어져 남은 1~2칸짜리 땅 조각을 물로 되돌린다 (자연 섬 윤곽의 잔여물)
    for _ in range(2):
        for y in range(m.h):
            for x in range(m.w):
                if m.water[y][x] or m.zone[y][x] not in ("meadow", "sand", "dune") or m.road[y][x]                         or m.obj[y][x] or m.high[y][x] or (x, y) in m.decks or any(o["x"] == x and o["y"] == y for o in m.objects):
                    continue
                land_n = sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                             if m.inside(x + dx, y + dy) and not m.water[y + dy][x + dx])
                if land_n <= 1:
                    m.water[y][x] = True
    m.districts = DISTRICTS
    m.default_district = DEFAULT_DISTRICT
    m.add("ambience", 58, 40, id="river", radius=12)
    m.pave_streets(2)
    return m


# ---------------- 마을별 씬 ----------------
# 석호 원본(112×84)을 다리 한가운데에서 잘라 수도 1 + 마을 4 씬으로 나눈다. 씬을 넘을 때 공용 이동(페이드)이 일어난다.
# 잘린 쪽은 잘린 줄의 지형(물·다리·둑길)을 GAP칸 늘여 붙인다. 두 씬이 같은 줄을 늘이므로 출구 폭이 저절로 맞는다.
# 넓이: 원본 9,408칸 → 다섯 씬 합계 19,755칸 (팀 결정: 베네치아 2만 칸 이내).
GAP = 16          # 2026-10-26 팀 기준(베네치아 2만 칸 이내): 20→16, 합계 19,755칸
CUT_W, CUT_E, CUT_Y = 35, 77, 39       # 서쪽·동쪽 세로 자름 줄(다리 가운데), 마을 사이 가로 자름 줄
SCENES = {
    # mapId: (이름, 원본 창 x0, y0, x1, y1, 늘이는 쪽)
    "venice_santerasmo": ("산테라스모 과수원", 0, 0, CUT_W, CUT_Y, "ES"),
    "venice_burano": ("부라노 어촌", 0, CUT_Y, CUT_W, H - 1, "EN"),
    "venice_rialto": ("리알토", CUT_W, 0, CUT_E, H - 1, "WE"),
    "venice_murano": ("무라노 유리공방", CUT_E, 0, W - 1, CUT_Y, "WS"),
    "venice_chioggia": ("키오자 교역항", CUT_E, CUT_Y, W - 1, H - 1, "WN"),
}
OPPOSITE = {"E": "W", "W": "E", "N": "S", "S": "N"}
OUTER = {"venice_santerasmo": "W", "venice_burano": "W", "venice_murano": "E", "venice_chioggia": "E"}
OUTER_PAD = 6
# 씬 사이 연결은 이웃마다 하나만 남긴다 (자름 줄 위 원본 시작 칸). 나머지 다리는 지우고 땅줄기는 물가에서 끝낸다.
KEEP = {CUT_W: (1, 25, 59), CUT_E: (1, 25, 49), CUT_Y: (2, 107)}
# 1: 북쪽 모래톱 바깥길(Q5 "수도를 지나는 길 vs 모래톱 길"), 25: 수도 다리, 59: 어촌은 남쪽 수변 다리(수레가 어부 집 앞 돌길까지 닿는 길)
# 늘인 물길 가운데 쉼터 섬 (씬 이름, 쪽) → 소품 테마. 다리 → 섬 → 다리로 걷는다.
ISLET_THEME = {"venice_santerasmo": "orc", "venice_burano": "fsh", "venice_murano": "gls", "venice_chioggia": "salt",
               "venice_rialto": "cap"}
ISLET_PROPS = {
    # (윗줄 소품, 아랫줄 소품). 섬 크기에 맞지 않으면 건너뛴다.
    "orc": (["vg_n_orc_lemon", "vg_lemon_crate", "vg_cypress"], ["vg_n_orc_olive", "vg_w_vine_bench"]),
    "fsh": (["vg_net_rack", "vg_px_fsh_traps", "vg_cypress"], ["vg_olive", "vg_barrel"]),
    "gls": (["vg_n_gls_sand", "vg_gls_cullet", "vg_cypress"], ["vg_olive", "vg_crate"]),
    "salt": (["vg_salt_mound", "vg_salt_rake", "vg_salt_heap"], ["vg_salt_hut", "vg_salt_heap"]),
    "cap": (["vg_cypress", "vg_cp_bench", "vg_cypress"], ["vg_stone_planter", "vg_olive"]),
}


def _cut_runs(src, cut, vertical):
    """자름 줄 위 걸을 수 있는 구간 [(시작, 끝)]"""
    n = src.h if vertical else src.w
    cells = [_walk(src, cut, i) if vertical else _walk(src, i, cut) for i in range(n)]
    runs, start = [], None
    for i, ok in enumerate(cells + [False]):
        if ok and start is None:
            start = i
        elif not ok and start is not None:
            runs.append((start, i - 1)); start = None
    return runs


def _drop_crossings(src):
    """남기지 않는 연결의 다리를 통째로 지운다 (양쪽 씬 모두에서 물로 끝나게)."""
    for cut, vertical in ((CUT_W, True), (CUT_E, True), (CUT_Y, False)):
        span = range(0, CUT_W + 1) if cut == CUT_Y else None
        for a, b in _cut_runs(src, cut, vertical):
            if a in KEEP[cut]:
                continue
            seeds = [(cut, i) if vertical else (i, cut) for i in range(a, b + 1)]
            if cut == CUT_Y and not (seeds[0][0] in span or seeds[0][0] >= CUT_E):
                continue
            todo = [c for c in seeds if c in src.decks]
            seen = set(todo)
            while todo:
                x, y = todo.pop()
                src.decks.pop((x, y), None)
                src.water[y][x] = True
                src.ground[y][x] = None
                src.road[y][x] = 0
                if src.deco[y][x] and src.deco[y][x].startswith("wood:"):
                    src.deco[y][x] = None
                for p in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if p in src.decks and p not in seen:
                        seen.add(p); todo.append(p)


def _owner(x, y, edge):
    """원본 칸 (x, y)를 edge 쪽으로 늘인 씬 (자름 줄 위의 칸은 두 씬이 함께 가진다)"""
    for mid, (_, x0, y0, x1, y1, grow) in SCENES.items():
        if x0 <= x <= x1 and y0 <= y <= y1 and edge in grow:
            return mid
    return None


def _walk(m, x, y):
    return not m.blocked[y][x] and (not m.water[y][x] or (x, y) in m.decks or (x, y) in m.over_water)


def _cut(src, mid):
    name, x0, y0, x1, y1, grow = SCENES[mid]
    gw, ge, gn, gs = (GAP if e in grow else 0 for e in "WENS")
    # 바깥 가장자리의 모래톱 길이 맵 끝에 붙지 않게 물을 덧댄다 (자름 줄이 아니므로 늘이지 않고 물만)
    gw += OUTER_PAD if OUTER.get(mid) == "W" else 0
    ge += OUTER_PAD if OUTER.get(mid) == "E" else 0
    w, h = x1 - x0 + 1 + gw + ge, y1 - y0 + 1 + gn + gs
    m = VeniceMap(mid, name, w, h, seed=301 + len(mid), zone="sand")
    m.hours = 0

    def source(x, y):
        return min(max(x - gw + x0, x0), x1), min(max(y - gn + y0, y0), y1)

    kept = set()
    for cut, vertical in ((CUT_W, True), (CUT_E, True), (CUT_Y, False)):
        for a, b in _cut_runs(src, cut, vertical):
            if a in KEEP[cut]:
                kept.update((cut, i) if vertical else (i, cut) for i in range(a, b + 1))

    for y in range(h):
        for x in range(w):
            sx, sy = source(x, y)
            inside = (sx, sy) == (x - gw + x0, y - gn + y0)
            if not inside and (sx, sy) not in kept:       # 늘인 물길: 남긴 연결만 이어지고 나머지는 물
                m.water[y][x] = True
                m.style[y][x] = src.style[sy][sx]
                continue
            m.water[y][x] = src.water[sy][sx]
            m.zone[y][x] = src.zone[sy][sx]
            m.style[y][x] = src.style[sy][sx]
            m.road[y][x] = src.road[sy][sx]
            for attr in ("pan", "over_water", "lush"):
                if (sx, sy) in getattr(src, attr):
                    getattr(m, attr).add((x, y))
            if (sx, sy) in src.decks:
                m.decks[(x, y)] = src.decks[(sx, sy)]
                m.ground[y][x] = src.ground[sy][sx]
            deco = src.deco[sy][sx]
            if inside:
                m.deco[y][x], m.obj[y][x], m.high[y][x] = deco, src.obj[sy][sx], src.high[sy][sx]
                m.blocked[y][x] = src.blocked[sy][sx]
                m.ground[y][x] = src.ground[sy][sx]
            elif deco and deco.startswith("wood:"):      # 늘인 다리의 난간
                m.deco[y][x] = deco
    shift = lambda x, y: (x - x0 + gw, y - y0 + gn)
    inwin = lambda x, y: x0 <= x <= x1 and y0 <= y <= y1
    for o in src.objects:
        if inwin(o["x"], o["y"]):
            d = dict(o); d["x"], d["y"] = shift(o["x"], o["y"])
            if "path" in d:
                d["path"] = [shift(*p) for p in d["path"]]
            m.objects.append(d)
    m.stamps = [(n, *shift(x, y)) for n, x, y in src.stamps if inwin(x, y)]
    m.doors = [(n, shift(*c)) for n, c in src.doors if inwin(*c)]
    m.districts = []
    for dn, a, b, c, e in DISTRICTS:
        a, b, c, e = max(a, x0), max(b, y0), min(c, x1), min(e, y1)
        if a <= c and b <= e:
            m.districts.append((dn, *shift(a, b), *shift(c, e)))
    m.default_district = DEFAULT_DISTRICT
    # 잘린 쪽 가장자리의 걸을 수 있는 구간마다 이웃 씬으로 가는 출구 (원본 줄 번호로 짝을 맞춘다)
    for edge in grow:
        cells = [(w - 1 if edge == "E" else 0, y) for y in range(h)] if edge in "EW" else \
                [(x, h - 1 if edge == "S" else 0) for x in range(w)]
        run = []
        for c in cells + [None]:
            if c is not None and _walk(m, *c):
                run.append(c); continue
            if run:
                along = 1 if edge in "EW" else 0
                start = run[0][along]
                ox, oy = source(*run[0])
                cut = {"E": (CUT_E if x1 == CUT_E else CUT_W, oy), "W": (x0, oy), "N": (ox, y0), "S": (ox, y1)}[edge]
                other = next(k for k in SCENES if k != mid and _owner(*cut, OPPOSITE[edge]) and
                             SCENES[k][1] <= cut[0] <= SCENES[k][3] and SCENES[k][2] <= cut[1] <= SCENES[k][4]
                             and OPPOSITE[edge] in SCENES[k][5])
                key = oy if edge in "EW" else ox
                m.link(edge, start, len(run), other, f"from_{mid}_{key}", f"from_{other}_{key}")
                _islet(m, edge, start, start + len(run) - 1, ISLET_THEME[mid])
                run = []
    return m


def _islet(m, edge, a, b, theme):
    """늘인 물길 가운데 쉼터 섬. 다리 끝은 섬에 닿는 쪽만 마감 그림·난간 끝으로 바꾼다."""
    horizontal = edge in "EW"
    g0 = {"E": m.w - GAP, "W": 0, "S": m.h - GAP, "N": 0}[edge]
    lo, hi = g0 + 5, g0 + 13                         # 물길 방향 섬 범위
    side0, side1 = a - 4, b + 4                       # 가로지르는 방향 섬 범위
    cells = []
    for u in range(lo, hi + 1):
        for v in range(side0, side1 + 1):
            if (u in (lo, hi)) and (v in (side0, side1)):
                continue                                  # 모서리는 둥글게
            x, y = (u, v) if horizontal else (v, u)
            if m.inside(x, y):
                cells.append((x, y))
    for x, y in cells:
        on_route = a <= (y if horizontal else x) <= b
        m.decks.pop((x, y), None)
        m.over_water.discard((x, y))
        m.water[y][x] = False
        m.ground[y][x] = None
        m.deco[y][x] = None
        m.zone[y][x] = "meadow"
        mid_route = a + 1 <= (y if horizontal else x) <= b - 1 if b - a >= 3 else on_route
        m.road[y][x] = 2 if mid_route else 0         # 섬 위 흙길은 가운데만, 양옆은 풀밭
        m.reserved[y][x] = False
        m.lush.add((x, y))
    # 섬에 닿는 다리 칸: 끝 그림과 난간 끝
    ends = [((u, v) if horizontal else (v, u)) for u in (lo - 1, hi + 1) for v in range(a, b + 1)]
    for x, y in [c for c in ends if c in m.decks]:
        step = ((1, 0), (-1, 0)) if horizontal else ((0, 1), (0, -1))
        for dx, dy in step:
            nx, ny = x + dx, y + dy
            if not m.inside(nx, ny) or m.water[ny][nx] or (nx, ny) in m.decks:
                continue
            if horizontal:
                sx = 0 if y == b else 2 if y == a else 1
                sy = 14 if dx > 0 else 12
                m.decks[(x, y)] = m.ground[y][x] = tiles.K("water", sx, sy, "woodturn")
                if y in (a, b):
                    m.deco[y][x] = f"wood:{'railhtop' if y == a else 'railhbottom'}:1"
            else:
                sx = 0 if x == a else 2 if x == b else 1
                sy = 14 if dy > 0 else 12
                m.decks[(x, y)] = m.ground[y][x] = tiles.K("water", sx, sy, "wood")
                if x in (a, b):
                    m.deco[y][x] = f"wood:{'railvleft' if x == a else 'railvright'}:1"
    # 소품: 길 양옆 띠에 하나씩 (자리가 맞지 않으면 건너뛴다)
    top, bottom = ISLET_PROPS[theme]
    for items, v in ((top, side0 + 1), (bottom, b + 2)):
        u = lo + 1
        for name in items:
            s = tiles.STAMPS[name]
            pw, ph = (s[3], s[4]) if horizontal else (s[4], s[3])
            vv = v if items is bottom else a - 1 - (s[4] if horizontal else s[3]) + 1
            x, y = (u, vv) if horizontal else (vv, u)
            try:
                m.building(name, x, y, high=s[6])
            except (ValueError, IndexError):
                pass
            u += pw + 1
    mx, my = ((lo + hi) // 2, side1 - 1) if horizontal else (side1 - 1, (lo + hi) // 2)
    m.cluster_deco(["vg_reeds", "vg_tuft", "vg_rocks"], mx, my, 2, 3)


def _lagoon_life(m):
    """넓은 물에 배·말뚝·갈대 모래톱을 드문드문 (길과 다리에서 떨어진 곳만)."""
    def clear(x0, y0, x1, y1, pad=2):
        return all(m.inside(x, y) and m.water[y][x] and (x, y) not in m.decks and not m.obj[y][x]
                   for y in range(y0 - pad, y1 + pad + 1) for x in range(x0 - pad, x1 + pad + 1))
    water_cells = sum(1 for row in m.water for c in row if c)
    for _ in range(water_cells // 260):
        for _try in range(40):
            x, y = m.rng.randrange(2, m.w - 6), m.rng.randrange(2, m.h - 4)
            kind = m.rng.random()
            if kind < 0.45 and clear(x, y, x + 3, y):
                m.stamp(m.rng.choice(["vg_fishing_boat", "vg_fsh_dinghy", "vg_gondola"]), x, y, force=True, over_road=True)
                break
            if kind < 0.7 and clear(x, y, x, y + 1):
                m.stamp(m.rng.choice(["vg_poles", "vg_pole_striped"]), x, y, force=True, over_road=True)
                break
            if clear(x, y, x + 3, y + 2, pad=3):        # 갈대 모래톱 (걸어서 닿지 않는 작은 땅)
                for yy in range(y, y + 3):
                    for xx in range(x, x + 4):
                        if (xx - x, yy - y) not in ((0, 0), (3, 0), (0, 2), (3, 2)):
                            m.water[yy][xx] = False; m.zone[yy][xx] = "sand"
                m.deco_stamp("vg_reeds_tall", x + 1, y)
                m.deco_stamp("vg_reeds", x + 2, y + 1)
                break


def _split_fixes(src):
    """분할하면 북쪽 모래톱이 양 끝 마을과 끊기므로 정원 수변에서 모래톱으로 건너는 목교를 놓는다."""
    src.bridge(40, 8, 42, 16)
    # 무라노 → 동쪽 모래톱(키오자 방면) 입구가 한 칸이라 수레가 못 지나간다. 두 칸 더 메운다.
    land(src, 107, 25, 108, 30, "meadow")
    # 북쪽 모래톱 → 무라노 공방 마당도 한 칸 길뿐이라 공방 서쪽 물가를 두 칸 메운다.
    land(src, 80, 7, 81, 15, "street")


def _chioggia_ferry(m):
    """키오자 동쪽 부두 → 본토 나루터 배 (venice_mainland.FERRY와 짝). 잔교 끝 3칸이 출구이고, 넘어가는 동안 물결 효과가 배 이동을 보여 준다."""
    # 원본 좌표(동쪽 부두 끝 x105, y69)를 씬 좌표로 옮긴다 (물길 폭 GAP이 바뀌어도 같은 자리)
    x, y = 105 - CUT_E + GAP, 69 - CUT_Y + GAP
    m.bridge(x, y, m.w - 1, y + 2)
    m.stamp("vg_barge_loaded", x + 4, y - 3, force=True, over_road=True)
    m.stamp("vg_px_fsh_mooring", x + 2, y + 3, force=True, over_road=True)
    m.link("E", y, 3, "venice_c_landing", "from_venice_chioggia", "from_venice_c_landing")
    m.talker(x, y - 2, "본토행 뱃사공", "villager_bear_tall",
             "본토 나루터로 가는 배요. 잔교 끝에서 타시오.",
             "건너편에서는 포도밭, 습지, 대상 숙소로 길이 갈라진다오.")


SCENE_EXTRAS = {"venice_chioggia": _chioggia_ferry}


def _scene(mid):
    src = _master()
    _split_fixes(src)
    _drop_crossings(src)
    m = _cut(src, mid)
    if mid in SCENE_EXTRAS:
        SCENE_EXTRAS[mid](m)
    _lagoon_life(m)
    import venice_dress             # 빈 포장 바닥에 운하 집을 줄지어 세운다 (2026-10-26)
    venice_dress.dress(m)
    m.finish()
    import venice_people            # 일하는 사람·마을 사람·길 위 상인·성장 소품 (공용 장치)
    venice_people.populate(m)
    return m


def venice_rialto(): return _scene("venice_rialto")
def venice_santerasmo(): return _scene("venice_santerasmo")
def venice_murano(): return _scene("venice_murano")
def venice_burano(): return _scene("venice_burano")
def venice_chioggia(): return _scene("venice_chioggia")


CAMERA_POINTS = {
    "venice_lagoon": [(49, 62), (38, 22), (76, 22), (30, 55), (78, 55), (14, 67), (92, 60), (55, 5), (49, 38)],
}
