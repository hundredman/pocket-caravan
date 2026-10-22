"""알 아흐사 — 아라비아 시작 마을 (64×46, 고려 개경과 같은 규모).

회의 기준(Docs/Arabia/05): 이미 생활과 상업이 자리 잡은 '발전된 지역'에서 시작한다.
고려 개경처럼 큰길·골목 격자로 구획을 나누고, 모든 집의 문 앞 길이 골목에 닿게 정돈했다.

  (북) 대추야자 숲 ─────────────────────────────────────────────
       주택가 ┃ 주택가        오아시스 호수              저택 ┃ 모스크·바람탑 집
  ━━━━━━━━━━━━━━━ 북쪽 골목 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
       수크 노점 6 ┃ 노점      중앙 광장(우물·게시판)     카라반사라이 ┃ 낙타 우리
  ━━━━━━━━━━━━━━━ 큰길 (동쪽 출구 → 농원길) ━━━━━━━━━━━━━━━━━━━━━
       덮인 시장 ┃ 빵 노점      광장 남쪽 쉼터           화물 마당 ┃ 목욕탕
  ━━━━━━━━━━━━━━━ 남쪽 골목 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
       대추야자 과수원·건조장          ┃남북 큰길┃       주택가 (남동)
  (남) 성벽 ──────────────────── 남문 (S0 시작) ────────────────
지명·건물 역할·NPC는 제안이며 실제 도시 구조의 재현이 아니다. 그림은 사용자 제작 아라비아 에셋.
"""
import arabia_art as A

W, H = 64, 46
EXIT_Y = 23            # 동쪽 출구 4칸 (23~26) = 큰길
PALMS = ['tree_palmDates', 'nature_datePalm', 'tree_doumPalm', 'tree_palmDates', 'nature_palmYoung']


def build(link_target, link_spawn, my_spawn):
    m = A.ArabiaMap('arabia_ahsa', '알 아흐사', W, H, seed=307, theme='town')

    # ---- 오아시스 (먼저: 이후 골목이 남쪽 가장자리를 덮는다) ----
    m.ellipse('grass', 31.5, 6.5, 10.5, 6.2)
    m.ellipse('water', 31.5, 6.2, 6.2, 3.2)

    # ---- 거리 격자 ----
    m.link('E', EXIT_Y, 4, link_target, link_spawn, my_spawn)
    m.paint('paving', 3, EXIT_Y, 63, EXIT_Y + 3, grade=3)      # 큰길 (동쪽 출구까지)
    m.paint('paving', 3, 13, 60, 14, grade=2)                  # 북쪽 골목
    m.paint('paving', 3, 34, 60, 35, grade=2)                  # 남쪽 골목
    m.paint('paving', 35, 41, 60, 42, grade=1)                 # 남동 주택가 골목
    m.paint('paving', 30, 13, 33, 43, grade=3)                 # 남문 → 북쪽 골목 큰길
    m.paint('paving', 12, 13, 13, 35, grade=2)                 # 서쪽 세로 골목
    m.paint('paving', 50, 13, 51, 35, grade=2)                 # 동쪽 세로 골목
    m.paint('paving', 24, 17, 39, 30, grade=3)                 # 중앙 광장
    m.paint('zellige', 29, 21, 34, 26)                         # 광장 가운데 타일 무늬
    m.add('spawn', 31, 41, id='S0')

    # ---- 성벽·남문 ----
    m.put('plaza_gate', 30, 42, foot=2, force=True)
    for x, y in [(0, 42), (62, 42)]:
        m.maybe('wall_tower', x, y, foot=4, force=True)
    for x in list(range(2, 28, 3)) + list(range(34, 61, 3)):
        m.maybe('wall_wall', x, 44, foot=2, force=True)
    A.block_rect(m, 0, 44, 29, 45); A.block_rect(m, 34, 44, 63, 45)

    # ---- 가장자리 야자 숲 (빈틈 없이) ----
    A.block_rect(m, 0, 0, 63, 0)
    A.block_rect(m, 0, 0, 0, 43)
    A.block_rect(m, 63, 0, 63, EXIT_Y - 1); A.block_rect(m, 63, EXIT_Y + 4, 63, 43)
    m.border(PALMS + ['nature_acacia'], 1, 3, 62, 3, step=(2, 2))          # 북쪽
    m.border(PALMS, 1, 6, 2, 41, step=(2, 3))                             # 서쪽
    m.border(PALMS, 61, 6, 62, EXIT_Y - 2)                               # 동쪽 (출구 비움)
    m.border(PALMS, 61, EXIT_Y + 7, 62, 41)

    # ---- 오아시스 꾸밈 ----
    for x, y in [(22, 7), (39, 7), (24, 2), (37, 2), (26, 9), (35, 9)]:
        m.maybe(PALMS[(x + y) % len(PALMS)], x, y)
    for x, y in [(25, 5), (37, 6), (30, 10)]:
        m.maybe('flower_lotus', x, y, layer='deco', force=True)
    m.maybe('prop_rugseat', 28, 10, layer='deco')
    m.maybe('flower_oleander', 23, 11, layer='deco')
    m.maybe('flower_jasmine', 39, 11, layer='deco')
    m.add('villager', 33, 11, variant=0)

    # ---- 북쪽 주택가 (문 앞 길 → 북쪽 골목) ----
    for x, h in [(4, 'house_sand'), (8, 'house_rose'), (14, 'house_white'), (18, 'house_mudbrick')]:
        m.building(h, x, 9, 13)
    for x, y in [(3, 5), (7, 5), (11, 5), (15, 5), (19, 5)]:
        m.maybe(['tree_orange', 'tree_olive', 'nature_pomegranateTree', 'tree_almond', 'tree_orange'][x % 5], x, y)
    m.maybe('prop_zir', 7, 11, foot=1, force=True)
    m.maybe('tree_pottedOrange', 17, 10, foot=1, force=True)
    m.maybe('flower_rose', 11, 11, layer='deco')
    m.building('house_courtyardManor', 42, 7, 13)
    m.building('mosque_mosqueSmall', 52, 7, 13)
    m.building('house_windTowerHouse', 57, 7, 13)
    m.maybe('tree_cypress', 48, 8)
    m.maybe('tree_cypress', 40, 9)

    # ---- 북쪽 골목과 수크 사이 작은 정원 ----
    m.maybe('nature_palmYoung', 41, 15)
    m.maybe('nature_palmYoung', 46, 15)

    # ---- 수크: 큰길 북쪽 노점 줄 ----
    stalls = ['stall_spice', 'stall_perfume', 'stall_cloth', 'stall_fruit', 'stall_dates']
    for x, s in zip([3, 8, 14, 17, 20], stalls):
        m.put(s, x, 20, foot=1)
    m.add('market', 21, EXIT_Y, town='arabia_ahsa')        # 대추야자 노점 앞 상인
    # 노점 뒤 창고 마당
    m.put('prop_carpethang', 3, 16, foot=1)
    m.put('prop_carpetstack', 6, 17, foot=1)
    m.put('prop_grainsacks', 9, 17, foot=1)
    m.put('prop_watermelons', 15, 17, foot=1)
    m.put('prop_orangecrate', 18, 18, foot=1)
    m.put('prop_spiceSack', 20, 16, foot=1)
    m.add('villager', 8, 15, variant=1)
    m.talker(6, 22, '향수 장수', 'arabia_woman_perfumer_tall', '장미와 유향을 섞은 향이에요. 멀리 갈수록 귀하게 쳐 주지요.')

    # ---- 중앙 광장 ----
    m.put('plaza_well', 31, 23, foot=1, force=True)
    m.put('plaza_trough', 33, 25, foot=1, force=True)
    m.put('plaza_board', 36, 18, foot=1, solid=0.05, force=True)
    A.block_rect(m, 36, 20, 37, 20)
    m.add('board', 36, 20, town='arabia_ahsa')
    m.put('plaza_majlis', 25, 18, foot=1, force=True)
    m.put('prop_rugseat', 25, 28, layer='deco', force=True)
    for x, y in [(24, 16), (39, 16), (24, 29), (39, 29)]:
        m.maybe('plaza_lamp', x, y, foot=1, force=True)
    for x, y in [(28, 18), (34, 18), (28, 28), (34, 28)]:
        m.maybe('tree_pottedOrange', x, y, foot=1, force=True)
    m.talker(29, 24, '우물가 아이', 'arabia_npc_child_tall', '우물물은 오아시스에서 끌어와요. 낙타도 여기서 물을 마셔요.')
    m.asign(22, 27, '알 아흐사 중앙 광장\n북: 오아시스 · 서: 수크 · 동: 카라반사라이와 농원길 · 남: 남문')

    # ---- 카라반사라이(여관) · 낙타 우리 ----
    m.put('building_caravanserai', 42, 19, foot=2)
    m.add('npc', 41, 22, npc='arabia_maryam')
    m.asign(47, 21, '카라반사라이 (여관)\n대상과 짐꾼이 묵어 가는 곳. 주인 마리암.')
    for x in range(52, 61, 2):
        m.maybe('prop_fence', x, 16, foot=1, force=True)
    m.put('animal_camel', 53, 18, foot=1)
    m.put('animal_camelRest', 57, 19, foot=1)
    m.put('plaza_trough', 54, 21, foot=1)
    m.maybe('prop_saddle', 56, 22, foot=1)
    m.talker(60, 21, '낙타몰이', 'arabia_npc_bedouin_tall', '짐을 더 싣고 싶다면 묶는 법부터 배워야지. 무게가 늘면 걸음은 느려진다.')

    # ---- 큰길 남쪽: 덮인 시장 · 빵 노점 ----
    m.building('civic_bazaar', 3, 28, 34)
    m.put('stall_bread', 15, 29, foot=1)
    m.put('prop_carpetstack', 18, 30, foot=1)
    m.put('prop_rugseat', 19, 28, layer='deco')
    m.talker(17, 32, '빵 굽는 아주머니', 'arabia_woman_qayna_tall', '여관에 곡물이 모자라면 빵도 줄어요. 자비드 곡물이 들어와야 해요.')
    m.maybe('tree_pottedOrange', 10, 30, foot=1)

    # ---- 화물 마당 (힌드바드, Q1) · 목욕탕 ----
    m.add('npc', 42, 28, npc='arabia_hindbad')
    m.put('prop_grainsacks', 44, 28, foot=1)
    m.put('prop_chest', 47, 28, foot=1)
    m.put('prop_zir', 44, 31, foot=1)
    m.put('animal_donkey', 46, 30, foot=1)
    m.put('prop_rope', 41, 31, foot=1)
    m.building('building_hammam', 53, 28, 34)
    m.maybe('tree_orange', 57, 29)
    m.asign(58, 27, '동쪽: 농원길 → 자비드\n짐을 많이 실을수록 걸음이 느려집니다.')

    # ---- 남서: 대추야자 과수원·건조장 ----
    m.building('house_dates', 4, 36, 41, kind='road')
    for row, y in enumerate([35, 38, 41]):
        for x in range(8, 28, 4):
            m.maybe(PALMS[(x // 4 + row) % 3], x + (2 if row % 2 else 0), y - 3)
    m.maybe('building_tent', 18, 40)
    m.maybe('prop_dates', 11, 37, foot=1)
    m.maybe('prop_dates', 24, 38, foot=1)
    m.talker(15, 37, '대추야자 농부', 'arabia_npc_weaver_tall', '갓 딴 대추야자는 알 아흐사 장터로, 말린 것은 멀리 자비드까지 가요.')

    # ---- 남동 주택가 (문 앞 길 → 남동 골목) ----
    for x, h in [(36, 'house_laundry'), (40, 'house_palmshade'), (44, 'house_dome'), (48, 'house_awning'), (57, 'house_windtower')]:
        m.building(h, x, 37, 41)
    m.building('house_vineHouse', 52, 36, 41)
    for x, y in [(39, 36), (47, 36), (56, 36)]:
        m.maybe('tree_pottedOrange', x, y, foot=1)
    m.maybe('flower_rose', 43, 39, layer='deco')
    m.add('villager', 46, 33, variant=1)

    # ---- 남문 안쪽 ----
    m.add('npc', 29, 39, npc='arabia_hasan')
    m.asign(34, 38, '알 아흐사 오아시스 마을\n선대 장부의 첫 줄: "짐은 목적지까지, 약속은 사람에게까지."\n북쪽 광장 → 동쪽 큰길 → 농원길 → 자비드')
    for x, y in [(29, 37), (34, 36)]:
        m.maybe('plaza_lamp', x, y - 1, foot=1, force=True)

    # ---- 큰길 가로수 (어린 야자) · 등불 ----
    for x in range(5, 23, 6):
        m.maybe('nature_palmYoung', x, 27 - 2 + 3)
    for x in (41, 47, 53):
        m.maybe('plaza_lamp', x, 27, foot=1)

    # ---- 빈 모래 군데군데 작은 식물 ----
    m.texture()          # 모래 바닥 마른 풀·자갈 (무작위 장식 대신, 막지 않음)

    m.finish_arabia()
    return m
