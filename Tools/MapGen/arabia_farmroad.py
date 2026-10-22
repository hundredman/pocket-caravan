"""농원길 — 알 아흐사와 자비드 사이 오아시스 농원 (60×36, 고려 예성 길과 비슷한 규모).

두 갈래 길 (회의: 두 개 이상 루트):
- 큰 대상로: 남쪽으로 돌아가는 다져진 넓은 길 (노면 좋음, 거리 김)
- 야자숲 지름길: 대추야자 숲 한가운데를 곧장 지나는 좁은 모래길 (거리 짧음, 노면 거침)
가운데로 관개 수로(팔라즈)가 남북으로 흐르고, 길과 만나는 곳은 돌다리.
아직 낙하·날씨·도적 사건은 없다 (첫 구간은 안전한 길).
"""
import arabia_art as A

W, H = 60, 36
PORT = 16          # 동·서 출구 행 (16~19)
PALMS = ['tree_palmDates', 'nature_datePalm', 'tree_doumPalm', 'nature_palmYoung']


def build(west, east):
    """west/east = (목적 씬, 목적 스폰, 이 맵 스폰)"""
    m = A.ArabiaMap('arabia_farmroad', '농원길', W, H, seed=402, theme='town')
    m.hours = 2

    # ---- 농원 풀밭 ----
    m.paint('grass', 15, 3, 44, 14, reserve=False)
    m.paint('grass', 17, 20, 42, 23, reserve=False)
    # ---- 길 ----
    m.link('W', PORT, 4, *west)
    m.link('E', PORT, 4, *east)
    m.paint_path('road', [(0, PORT), (14, PORT), (14, 26), (44, 26), (44, PORT), (59, PORT)], width=4, grade=3)
    m.paint('road', 18, PORT + 1, 43, PORT + 2, grade=1)                   # 야자숲 지름길 (2칸)
    # ---- 관개 수로와 돌다리 ----
    m.paint('water', 29, 2, 30, 33)
    m.bridge(28, PORT + 1, 31, PORT + 2, 'h', grade=1)                     # 지름길 판자 다리 (좁음)
    m.bridge(28, 26, 31, 29, 'h', grade=3)                                 # 대상로 판자 다리 (넓음)
    m.paint('water', 26, 6, 33, 10)                                        # 수로 저수지

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, 59, 0); A.block_rect(m, 0, 35, 59, 35)
    A.block_rect(m, 0, 0, 0, PORT - 1); A.block_rect(m, 0, PORT + 4, 0, 35)
    A.block_rect(m, 59, 0, 59, PORT - 1); A.block_rect(m, 59, PORT + 4, 59, 35)
    m.border(PALMS + ['nature_acacia'], 1, 3, 58, 3, step=(2, 2))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], 1, 6, 2, PORT - 2, step=(2, 3))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], 1, PORT + 7, 2, 33, step=(2, 3))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], 57, 6, 58, PORT - 2, step=(2, 3))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], 57, PORT + 7, 58, 33, step=(2, 3))
    for x in range(2, 58, 3):                                             # 남쪽 모래 언덕
        m.maybe('nature_dune', x, 34, foot=1, force=True)

    # ---- 북쪽 과수원 (가지런한 줄) ----
    for row, y in enumerate([5, 10]):
        for x in range(16, 27, 4):
            m.maybe(['tree_orange', 'nature_pomegranateTree', 'tree_banana'][(x // 4 + row) % 3], x, y - 2)
        for x in range(34, 44, 4):
            m.maybe(['tree_almond', 'tree_orange', 'tree_mulberry'][(x // 4 + row) % 3], x, y - 2)
    for x, y in [(27, 5), (32, 5), (27, 11), (32, 11)]:
        m.maybe('flower_lotus', x, y, layer='deco', force=True)

    # ---- 야자숲 (지름길 양옆) ----
    for x in range(18, 43, 3):
        if 27 <= x <= 32:
            continue
        m.maybe(PALMS[x % 3], x, PORT - 3)
        m.maybe(PALMS[(x + 1) % 3], x, PORT + 3)

    # ---- 서쪽 입구: 농가 · 일꾼 ----
    m.put('house_farmhouse', 4, 9, foot=2)
    m.paint('road', 6, 13, 6, PORT - 1, grade=1)
    m.maybe('prop_grainsacks', 9, 11, foot=1)
    m.maybe('prop_zir', 3, 13, foot=1)
    m.talker(8, 13, '농원 일꾼', 'arabia_npc_weaver_tall',
             '대추야자는 알 아흐사에서, 곡물은 자비드에서 많이 나와요.',
             '큰 대상로는 돌아가지만 넓고 평평해요. 야자숲 지름길은 짧은 대신 모래가 깊지요.')
    m.asign(4, 21, '농원길\n동쪽 자비드까지 두 갈래: 남쪽 큰 대상로(넓고 평평) / 야자숲 지름길(짧고 거침)\n짐이 무거울수록 느려집니다. 낙하·날씨 사건은 없습니다.')

    # ---- 남쪽: 대상 야영지 · 쉼터 ----
    m.put('building_tent', 18, 30, foot=2)
    m.maybe('animal_camelRest', 23, 31, foot=1)
    m.maybe('prop_carpet', 16, 31, layer='deco')
    m.maybe('prop_waterskin', 26, 33, foot=1)
    m.talker(22, 33, '야영하는 대상', 'arabia_npc_bedouin_tall',
             '해 지기 전에 자비드에 닿으려면 대상로를 따라가게. 길이 넓어 수레도 편하지.')
    m.put('building_arish', 35, 30, foot=2)           # 야자잎 쉼터
    m.put('building_well', 40, 30, foot=1)
    m.maybe('prop_qulla', 39, 33, foot=1)
    m.maybe('nature_frankincenseTree', 47, 29)
    m.maybe('nature_acacia', 9, 28)
    m.maybe('nature_acacia', 52, 25)
    m.maybe('tree_ghaf', 50, 29)

    # ---- 동쪽 ----
    m.maybe('nature_figTree', 48, 9)
    m.maybe('nature_tamarisk', 52, 10)
    m.asign(53, 21, '← 알 아흐사 / 자비드 →\n짐을 많이 실을수록 느려집니다.')

    # ---- 사프란 밭 (농가 옆) ----
    for y in (5, 7):
        for x in range(5, 13, 2):
            m.maybe('flower_saffron', x, y, layer='deco')
    m.maybe('prop_carpet', 10, 11, layer='deco')
    m.maybe('prop_dates', 10, 10, foot=1)

    # ---- 대상로 등불·쉼터 (다리 양쪽) ----
    for x, y in [(26, 24), (33, 24), (12, 14), (47, 14)]:
        m.maybe('plaza_lamp', x, y, foot=1)
    m.maybe('prop_waterskin', 34, 31, foot=1)
    m.maybe('prop_saddle', 37, 33, foot=1)

    m.texture()          # 모래 바닥 마른 풀·자갈 (무작위 장식 대신, 막지 않음)

    m.finish_arabia()
    return m
