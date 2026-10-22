"""자비드 — 곡물을 기르는 농원 마을 (56×40, 고려 마을 규모).

서쪽 입구 → 공동 부엌(Q1 수령) → 곡물 장터(Q2 구매) → 마을 광장 순으로 가깝게 이어진다.
북쪽 주택가는 골목 격자, 남쪽은 관개 수로가 흐르는 밭과 과수원. 동쪽 출구는 공방길(타이즈)로 이어진다.
"""
import arabia_art as A

W, H = 56, 40
PORT = 18
PALMS = ['tree_palmDates', 'nature_datePalm', 'tree_doumPalm', 'nature_palmYoung']


def build(west, east):
    m = A.ArabiaMap('arabia_zabid', '자비드', W, H, seed=503, theme='town')
    m.paint('grass', 3, 27, 52, 37, reserve=False)            # 남쪽 밭
    m.link('W', PORT, 4, *west)
    m.link('E', PORT, 4, *east)
    m.paint('gravel', 45, PORT, W - 1, PORT + 3, grade=2)           # 동쪽 공방길로 나가는 자갈길
    m.paint('paving', 0, PORT, 41, PORT + 3, grade=3)          # 마을 큰길
    m.paint('paving', 3, 12, 50, 13, grade=2)                  # 북쪽 골목
    m.paint('paving', 18, 12, 19, PORT, grade=2)               # 세로 골목
    m.paint('paving', 31, 11, 44, 26, grade=3)                 # 마을 광장
    m.paint('zellige', 35, 16, 40, 21)
    m.paint('road', 20, PORT + 4, 21, 27, grade=1)             # 밭으로 가는 길
    m.paint('road', 3, 27, 52, 28, grade=1)                    # 밭 둑길
    m.paint('water', 3, 32, 52, 33)                            # 관개 수로
    m.bridge(20, 31, 21, 34, 'v', grade=1)                     # 수로 판자 다리
    m.paint('road', 20, 29, 21, 30, grade=1)                   # 둑길 → 다리

    # 가장자리
    A.block_rect(m, 0, 0, 55, 0); A.block_rect(m, 0, 39, 55, 39)
    A.block_rect(m, 0, 0, 0, PORT - 1); A.block_rect(m, 0, PORT + 4, 0, 39)
    A.block_rect(m, 55, 0, 55, PORT - 1); A.block_rect(m, 55, PORT + 4, 55, 39)
    m.border(PALMS + ['nature_acacia'], 1, 3, 54, 3, step=(2, 2))
    m.border(PALMS + ['tree_ghaf'], 1, 6, 2, PORT - 2, step=(2, 3))
    m.border(PALMS + ['tree_ghaf'], 1, PORT + 7, 2, 38, step=(2, 3))
    m.border(None, 53, 6, 54, PORT - 2)
    m.border(None, 53, PORT + 7, 54, 38)
    m.border(PALMS, 4, 38, 52, 38, step=(3, 1))

    # ---- 입구: 공동 부엌 (Q1) ----
    m.building('house_farmhouse', 5, 13, PORT, door=1)
    m.add('npc', 9, PORT - 1, npc='arabia_kitchen_keeper')
    m.maybe('prop_zir', 10, 15, foot=1)
    m.maybe('prop_qulla', 12, 15, foot=1)
    m.maybe('prop_rugseat', 13, 16, layer='deco')
    m.asign(3, 22, '자비드 농원 마을\n공동 부엌은 바로 오른쪽 위, 곡물 장터는 큰길 남쪽입니다.')

    # ---- 곡물 장터 (Q2) ----
    m.put('stall_bread', 9, PORT + 4, foot=1)
    m.add('market', 10, PORT + 3, town='arabia_zabid')
    m.put('stall_fruit', 13, PORT + 4, foot=1)
    m.maybe('prop_grainsacks', 4, PORT + 5, foot=1)
    m.maybe('prop_grainsacks', 6, PORT + 7, foot=1)
    m.maybe('prop_watermelons', 16, PORT + 5, foot=1)
    m.talker(7, PORT + 3, '곡물 장수', 'arabia_woman_bedouinWoman_tall', '갓 거둔 곡물이에요. 알 아흐사 여관에서도 이 곡물을 기다리지요.')

    # ---- 북쪽 주택가 (문 앞 길 → 북쪽 골목) ----
    for x, h in [(4, 'house_rose'), (8, 'house_mudbrick'), (21, 'house_sand'), (25, 'house_laundry'), (46, 'house_dates')]:
        m.building(h, x, 8, 12)
    m.building('house_cottage', 13, 8, 12)
    m.building('house_vineHouse', 37, 6, 12)
    m.building('house_windTowerHouse', 30, 6, 12, door=2)
    for x, y in [(42, 6), (11, 4), (17, 4)]:
        m.maybe('tree_orange', x, y)
    m.maybe('tree_pottedOrange', 7, 10, foot=1)
    m.add('villager', 24, 15, variant=1)

    # ---- 광장 ----
    m.put('plaza_well', 37, 18, foot=1, force=True)
    m.put('plaza_board', 42, 13, foot=1, solid=0.05, force=True)
    A.block_rect(m, 42, 15, 43, 15)
    m.add('board', 42, 15, town='arabia_zabid')
    m.maybe('plaza_majlis', 32, 23, foot=1, force=True)
    for x, y in [(31, 10), (44, 10), (31, 25), (44, 25)]:
        m.maybe('plaza_lamp', x, y, foot=1, force=True)
    m.add('villager', 34, 20, variant=2)
    m.asign(46, 15, '동쪽 → 공방길 · 타이즈\n고원 와디를 따라 오르는 길입니다.')

    # ---- 광장 동쪽 ----
    m.maybe('building_mudHouse', 46, 23, foot=2)
    m.maybe('tree_cypress', 46, 15)
    m.maybe('nature_palmYoung', 49, 14)

    # ---- 남쪽 밭·과수원 ----
    for x in range(5, 52, 4):
        if 19 <= x <= 22:
            continue
        m.maybe(['tree_banana', 'tree_orange', 'nature_pomegranateTree'][x % 3], x, 28)
        m.maybe(['tree_palmDates', 'tree_banana', 'tree_almond'][x % 3], x + 1, 34)
    m.maybe('prop_grainsacks', 24, 30, foot=1)
    m.maybe('animal_donkey', 27, 29, foot=1)
    m.talker(23, 29, '밭일하는 농부', 'arabia_npc_weaver_tall', '수로 물이 마르지 않게 돌보는 게 제일 중요해요.')

    # ---- 가운데 작은 안뜰 (빈 블록) ----
    m.maybe('plaza_majlis', 22, 14, foot=1)
    m.maybe('tree_pottedOrange', 26, 14, foot=1)
    m.maybe('tree_pottedOrange', 29, 14, foot=1)
    m.maybe('prop_spiceTray', 27, 16, foot=1)
    m.talker(25, 17, '차 마시는 노인', 'arabia_npc_scholar_tall', '자비드 곡물은 수로 덕분에 해마다 잘 여문답니다.')

    # ---- 사프란 밭 (수로 남쪽, 보라 꽃 줄) ----
    for y in (34,):
        for x in list(range(5, 19, 2)) + list(range(24, 52, 2)):
            m.maybe('flower_saffron', x, y, layer='deco')
    for x in (8, 30, 44):
        m.maybe('prop_qulla', x, 30, foot=1)

    m.texture()          # 모래 바닥 마른 풀·자갈 (무작위 장식 대신, 막지 않음)

    m.finish_arabia()
    return m
