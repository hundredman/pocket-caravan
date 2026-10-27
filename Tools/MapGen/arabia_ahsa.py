"""알 아흐사 — 아라비아 시작 마을 (56×40).

회의 기준(Docs/Arabia/05): 이미 생활과 상업이 자리 잡은 '발전된 지역'에서 시작한다.
2026-10-26 회의(스타팅 지역 2만 칸 이내)에 맞춰 64×46 → 56×40으로 줄이고, 구획을 촘촘하게 다시 맞췄다.
모든 집의 문 앞 길이 골목에 닿는다.

  (북) 대추야자 숲 ──────────────────────────────────────
       주택가 ┃ 주택가      오아시스 호수      저택 · 모스크 · 바람탑 집
  ━━━━━━━━━━━━━ 북쪽 골목 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
       수크 노점 5 ┃ 노점   중앙 광장(우물·게시판)   카라반사라이 ┃ 낙타 우리
  ━━━━━━━━━━━━━ 큰길 (동쪽 출구 → 농원길) ━━━━━━━━━━━━━━━━━━
       덮인 시장 ┃ 빵 노점   광장 남쪽         화물 마당 ┃ 목욕탕
  ━━━━━━━━━━━━━ 남쪽 골목 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
       대추야자 과수원·건조장  ┃남북 큰길┃  주택가 (남동)
  (남) 성벽 ─────────────── 남문 (S0 시작) ──────────────
지명·건물 역할·NPC는 제안이며 실제 도시 구조의 재현이 아니다. 그림은 사용자 제작 아라비아 에셋.
"""
import arabia_art as A

W, H = 56, 40
EXIT_Y = 20            # 동쪽 출구 4칸 (20~23) = 큰길
AVE = 26               # 남북 큰길 x (26~29)
PALMS = ['tree_palmDates', 'nature_datePalm', 'tree_doumPalm', 'tree_palmDates', 'nature_palmYoung']


def build(link_target, link_spawn, my_spawn):
    m = A.ArabiaMap('arabia_ahsa', '알 아흐사', W, H, seed=307, theme='town')

    # ---- 오아시스 (먼저: 이후 골목이 남쪽 가장자리를 덮는다) ----
    m.ellipse('grass', 27.5, 5.6, 8.6, 5.4)
    m.ellipse('water', 27.5, 5.2, 5.2, 2.7)

    # ---- 거리 격자 ----
    m.link('E', EXIT_Y, 4, link_target, link_spawn, my_spawn)
    m.paint('paving', 3, EXIT_Y, W - 1, EXIT_Y + 3, grade=3)    # 큰길 (동쪽 출구까지)
    m.paint('paving', 3, 11, 52, 12, grade=2)                    # 북쪽 골목
    m.paint('paving', 3, 30, 52, 31, grade=2)                    # 남쪽 골목
    m.paint('paving', 30, 35, 52, 36, grade=1)                   # 남동 주택가 골목
    m.paint('paving', AVE, 11, AVE + 3, 37, grade=3)             # 남문 → 북쪽 골목 큰길
    m.paint('paving', 10, 11, 11, 31, grade=2)                   # 서쪽 세로 골목
    m.paint('paving', 44, 11, 45, 31, grade=2)                   # 동쪽 세로 골목
    m.paint('paving', 21, 14, 34, 27, grade=3)                   # 중앙 광장
    m.paint('zellige', 25, 17, 30, 24)                           # 광장 가운데 타일 무늬
    m.add('spawn', AVE + 1, 35, id='S0')

    # ---- 성벽·남문 ----
    m.put('plaza_gate', AVE, 36, foot=2, force=True)
    for x, y in [(0, 36), (54, 36)]:
        m.maybe('wall_tower', x, y, foot=4, force=True)
    for x in list(range(2, 24, 3)) + list(range(30, 52, 3)):
        m.maybe('wall_wall', x, 38, foot=2, force=True)
    A.block_rect(m, 0, 38, AVE - 1, 39); A.block_rect(m, AVE + 4, 38, W - 1, 39)

    # ---- 가장자리 야자 숲 (빈틈 없이) ----
    A.block_rect(m, 0, 0, W - 1, 0)
    A.block_rect(m, 0, 0, 0, 37)
    A.block_rect(m, W - 1, 0, W - 1, EXIT_Y - 1); A.block_rect(m, W - 1, EXIT_Y + 4, W - 1, 37)
    m.border(PALMS + ['nature_acacia'], 1, 3, 54, 3, step=(2, 2))         # 북쪽
    m.border(PALMS, 1, 6, 2, 35, step=(2, 3))                            # 서쪽
    m.border(PALMS, 53, 6, 54, EXIT_Y - 2)                              # 동쪽 (출구 비움)
    m.border(PALMS, 53, EXIT_Y + 7, 54, 33)

    # ---- 북쪽 주택가 (문 앞 → 북쪽 골목) ----
    for x, h in [(3, 'house_sand'), (7, 'house_rose'), (13, 'house_white'), (17, 'house_mudbrick')]:
        m.building(h, x, 8, 11)
    for x, y in [(3, 4), (7, 4), (12, 4), (16, 4)]:
        m.maybe(['tree_orange', 'tree_olive', 'nature_pomegranateTree', 'tree_almond'][(x // 4) % 4], x, y)
    m.maybe('tree_pottedOrange', 10, 9, foot=1, force=True)
    m.maybe('flower_rose', 16, 10, layer='deco')
    m.building('house_courtyardManor', 37, 6, 11)
    m.maybe('tree_cypress', 42, 7)
    m.building('mosque_mosqueSmall', 44, 6, 11)
    m.building('house_windTowerHouse', 49, 6, 11)

    # ---- 오아시스 꾸밈 ----
    for x, y in [(18, 6), (35, 6), (20, 1), (33, 1), (21, 8), (32, 8)]:
        m.maybe(PALMS[(x + y) % len(PALMS)], x, y)
    for x, y in [(22, 4), (33, 4), (27, 8)]:
        m.maybe('flower_lotus', x, y, layer='deco', force=True)
    m.maybe('prop_rugseat', 23, 8, layer='deco')
    m.maybe('flower_oleander', 19, 9, layer='deco')
    m.maybe('flower_jasmine', 35, 9, layer='deco')
    m.add('villager', 30, 10, variant=0)

    # ---- 수크: 큰길 북쪽 노점 줄 ----
    stalls = ['stall_spice', 'stall_perfume', 'stall_cloth', 'stall_fruit', 'stall_dates']
    for x, s in zip([3, 6, 12, 15, 18], stalls):
        m.put(s, x, 17, foot=1)
    m.add('market', 19, EXIT_Y, town='arabia_ahsa')        # 대추야자 노점 앞 상인
    # 노점 뒤 창고 마당
    m.put('prop_carpethang', 3, 13, foot=1)
    m.put('prop_carpetstack', 6, 14, foot=1)
    m.put('prop_grainsacks', 12, 14, foot=1)
    m.put('prop_watermelons', 15, 14, foot=1)
    m.put('prop_spiceSack', 18, 14, foot=1)
    m.add('villager', 9, 14, variant=1)
    m.talker(9, 19, '향수 장수', 'arabia_woman_perfumer_tall', '장미와 유향을 섞은 향이에요. 멀리 갈수록 귀하게 쳐 주지요.')

    # ---- 중앙 광장 ----
    m.put('plaza_well', 27, 19, foot=1, force=True)
    m.put('plaza_trough', 27, 22, foot=1, force=True)
    m.put('plaza_board', 32, 14, foot=1, solid=0.05, force=True)
    A.block_rect(m, 32, 16, 33, 16)
    m.add('board', 32, 16, town='arabia_ahsa')
    m.put('plaza_majlis', 22, 14, foot=1, force=True)
    m.put('prop_rugseat', 31, 25, layer='deco', force=True)
    for x, y in [(21, 15), (34, 16), (21, 26), (34, 26)]:
        m.maybe('plaza_lamp', x, y - 1, foot=1, force=True)
    for x, y in [(24, 16), (31, 16), (24, 24), (31, 23)]:
        m.maybe('tree_pottedOrange', x, y, foot=1, force=True)
    m.talker(26, 21, '우물가 아이', 'arabia_npc_child_tall', '우물물은 오아시스에서 끌어와요. 낙타도 여기서 물을 마셔요.')
    m.asign(22, 24, '알 아흐사 중앙 광장\n북: 오아시스 → 빈 사막 질러가기 · 서: 수크 · 동: 나즈드 대상로 · 남: 남문')

    # ---- 카라반사라이(여관) · 낙타 우리 ----
    m.put('building_caravanserai', 37, 16, foot=2)
    m.add('npc', 36, 19, npc='arabia_maryam')
    m.asign(42, 17, '카라반사라이 (여관)\n대상과 짐꾼이 묵어 가는 곳. 주인 마리암.')
    m.maybe('nature_palmYoung', 38, 13)
    for x in range(46, 53, 2):
        m.maybe('prop_fence', x, 13, foot=1, force=True)
    m.put('animal_camel', 46, 15, foot=1)
    m.put('animal_camelRest', 49, 15, foot=1)
    m.put('plaza_trough', 47, 18, foot=1)
    m.maybe('prop_saddle', 51, 17, foot=1)
    m.talker(50, 19, '낙타몰이', 'arabia_npc_bedouin_tall', '짐을 더 싣고 싶다면 묶는 법부터 배워야지. 무게가 늘면 걸음은 느려진다.')

    # ---- 큰길 남쪽: 덮인 시장 · 빵 노점 ----
    m.building('civic_bazaar', 3, 25, 30)
    m.put('stall_bread', 13, 25, foot=1)
    m.put('prop_carpetstack', 16, 26, foot=1)
    m.put('prop_rugseat', 17, 24, layer='deco')
    m.talker(15, 28, '빵 굽는 아주머니', 'arabia_woman_qayna_tall', '여관에 곡물이 모자라면 빵도 줄어요. 자비드 곡물이 들어와야 해요.')
    m.maybe('tree_pottedOrange', 19, 27, foot=1)

    # ---- 화물 마당 (힌드바드, Q1) · 목욕탕 ----
    m.add('npc', 37, 25, npc='arabia_hindbad')
    m.put('prop_grainsacks', 39, 25, foot=1)
    m.put('prop_chest', 42, 25, foot=1)
    m.put('prop_zir', 39, 28, foot=1)
    m.put('animal_donkey', 41, 27, foot=1)
    m.put('prop_rope', 36, 28, foot=1)
    m.building('building_hammam', 47, 25, 30)
    m.asign(51, 24, '동쪽: 나즈드 대상로 → 사막 카라반사라이 → 제다\n짐을 많이 실을수록 걸음이 느려집니다.')

    # ---- 남서: 대추야자 과수원·건조장 ----
    m.building('house_dates', 3, 32, 35, kind='road')
    m.maybe('building_tent', 16, 33)
    m.maybe('prop_dates', 9, 35, foot=1)
    m.maybe('prop_dates', 21, 35, foot=1)
    m.talker(14, 36, '대추야자 농부', 'arabia_npc_weaver_tall', '갓 딴 대추야자는 알 아흐사 장터로, 말린 것은 멀리 자비드까지 가요.')
    for row, y in enumerate([32, 34]):
        for x in range(7, 25, 4):
            m.maybe(PALMS[(x // 4 + row) % 3], x + (2 if row % 2 else 0), y)

    # ---- 남동 주택가 (문 앞 → 남동 골목) ----
    for x, h in [(33, 'house_laundry'), (37, 'house_palmshade'), (41, 'house_dome'), (45, 'house_awning'), (49, 'house_windtower')]:
        m.building(h, x, 32, 35)
    m.add('villager', 40, 29, variant=1)

    # ---- 남문 안쪽 ----
    m.add('npc', AVE - 1, 34, npc='arabia_hasan')
    m.asign(AVE + 4, 32, '알 아흐사 오아시스 마을\n선대 장부의 첫 줄: "짐은 목적지까지, 약속은 사람에게까지."\n북쪽 광장 → 동쪽 큰길 → 나즈드 대상로 → 제다 → 해안길 → 자비드')
    m.maybe('plaza_lamp', AVE - 1, 31 + 1, foot=1, force=True)

    # ---- 큰길 등불 ----
    for x in (40, 48):
        m.maybe('plaza_lamp', x, EXIT_Y - 2, foot=1)

    m.texture()          # 모래 바닥 마른 풀·자갈 (막지 않음)
    m.finish_arabia()
    return m
