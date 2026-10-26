"""농원길 — 알 아흐사와 자비드 사이 오아시스 농원 (48×28).

2026-10-26 회의(스타팅 지역 2만 칸 이내)에 맞춰 60×36 → 48×28로 줄였다. 길 구조는 그대로.
두 갈래 길 (회의: 두 개 이상 루트):
- 큰 대상로: 남쪽으로 돌아가는 다져진 넓은 길 (노면 좋음, 거리 김)
- 야자숲 지름길: 대추야자 숲 한가운데를 곧장 지나는 좁은 모래길 (거리 짧음, 노면 거침)
가운데로 관개 수로(팔라즈)가 남북으로 흐르고, 길과 만나는 곳은 판자 다리.
"""
import arabia_art as A

W, H = 48, 28
PORT = 12          # 동·서 출구 행 (12~15)
LOOP_Y = 18        # 큰 대상로 남쪽 구간 (18~21)
CANAL = 23         # 수로 x (23~24)
PALMS = ['tree_palmDates', 'nature_datePalm', 'tree_doumPalm', 'nature_palmYoung']


def build(west, east):
    """west/east = (목적 씬, 목적 스폰, 이 맵 스폰)"""
    m = A.ArabiaMap('arabia_farmroad', '농원길', W, H, seed=402, theme='town')
    m.hours = 2

    # ---- 농원 풀밭 ----
    m.paint('grass', 12, 3, 35, 10, reserve=False)
    m.paint('grass', 13, 16, 34, 17, reserve=False)
    # ---- 길 ----
    m.link('W', PORT, 4, *west)
    m.link('E', PORT, 4, *east)
    m.paint_path('road', [(0, PORT), (9, PORT), (9, LOOP_Y), (36, LOOP_Y), (36, PORT), (W - 1, PORT)], width=4, grade=3)
    m.paint('road', 13, PORT + 1, 35, PORT + 2, grade=1)                     # 야자숲 지름길 (2칸)
    # ---- 관개 수로와 판자 다리 ----
    m.paint('water', CANAL, 2, CANAL + 1, H - 3)
    m.bridge(CANAL - 1, PORT + 1, CANAL + 2, PORT + 2, 'h', grade=1)       # 지름길 다리 (좁음)
    m.bridge(CANAL - 1, LOOP_Y, CANAL + 2, LOOP_Y + 3, 'h', grade=3)       # 대상로 다리 (넓음)
    m.paint('water', CANAL - 3, 5, CANAL + 4, 8)                           # 수로 저수지

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, W - 1, 0); A.block_rect(m, 0, H - 1, W - 1, H - 1)
    A.block_rect(m, 0, 0, 0, PORT - 1); A.block_rect(m, 0, PORT + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, PORT - 1); A.block_rect(m, W - 1, PORT + 4, W - 1, H - 1)
    m.border(PALMS + ['nature_acacia'], 1, 3, W - 2, 3, step=(2, 2))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], 1, 6, 2, PORT - 2, step=(2, 3))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], 1, PORT + 7, 2, H - 3, step=(2, 3))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], W - 3, 6, W - 2, PORT - 2, step=(2, 3))
    m.border(PALMS + ['nature_acacia', 'tree_ghaf'], W - 3, PORT + 7, W - 2, H - 3, step=(2, 3))
    for x in range(3, W - 4, 3):                                          # 남쪽 모래 언덕
        m.maybe('nature_dune', x, H - 2, foot=1, force=True)

    # ---- 서쪽 입구: 농가 · 일꾼 · 사프란 밭 ----
    m.put('house_farmhouse', 3, 5, foot=2)
    m.paint('road', 5, 9, 5, PORT - 1, grade=1)
    for y in (5, 7):
        for x in range(9, 12):
            m.maybe('flower_saffron', x, y, layer='deco')
    m.maybe('prop_grainsacks', 9, 9, foot=1)
    m.talker(7, 11, '농원 일꾼', 'arabia_npc_weaver_tall',
             '대추야자는 알 아흐사에서, 곡물은 자비드에서 많이 나와요.',
             '큰 대상로는 돌아가지만 넓고 평평해요. 야자숲 지름길은 짧은 대신 모래가 깊지요.')
    m.asign(3, 17, '농원길\n동쪽 자비드까지 두 갈래: 남쪽 큰 대상로(넓고 평평) / 야자숲 지름길(짧고 거침)\n짐이 무거울수록 느려집니다. 낙하·날씨 사건은 없습니다.')

    # ---- 북쪽 과수원 (가지런한 줄) ----
    for row, y in enumerate([5, 9]):
        for x in (13, 17):
            m.maybe(['tree_orange', 'nature_pomegranateTree', 'tree_banana'][(x // 4 + row) % 3], x, y - 2)
        for x in (28, 32):
            m.maybe(['tree_almond', 'tree_orange', 'tree_mulberry'][(x // 4 + row) % 3], x, y - 2)
    for x, y in [(19, 4), (28, 4), (19, 9), (28, 9)]:
        m.maybe('flower_lotus', x, y, layer='deco', force=True)

    # ---- 야자숲 (지름길 양옆) ----
    for x in range(13, 36, 3):
        if CANAL - 2 <= x <= CANAL + 2:
            continue
        m.maybe(PALMS[x % 3], x, PORT - 3)
        m.maybe(PALMS[(x + 1) % 3], x, PORT + 3)

    # ---- 남쪽: 대상 야영지 · 야자잎 쉼터 ----
    m.put('building_tent', 12, 22, foot=2)
    m.maybe('animal_camelRest', 17, 23, foot=1)
    m.maybe('prop_carpet', 10, 23, layer='deco')
    m.maybe('prop_waterskin', 20, 24, foot=1)
    m.talker(16, 22, '야영하는 대상', 'arabia_npc_bedouin_tall',
             '해 지기 전에 자비드에 닿으려면 대상로를 따라가게. 길이 넓어 수레도 편하지.')
    m.put('building_arish', 27, 22, foot=2)
    m.put('building_well', 32, 22, foot=1)
    m.maybe('prop_saddle', 35, 24, foot=1)
    m.maybe('nature_frankincenseTree', 38, 21)
    m.maybe('nature_acacia', 4, 21)
    m.maybe('tree_ghaf', 41, 22)

    # ---- 동쪽 ----
    m.maybe('nature_figTree', 38, 5)
    m.maybe('nature_tamarisk', 41, 7)
    m.asign(41, 17, '← 알 아흐사 / 자비드 →\n짐을 많이 실을수록 느려집니다.')

    # ---- 대상로 등불 (다리 양쪽) ----
    for x, y in [(CANAL - 3, LOOP_Y - 2), (CANAL + 4, LOOP_Y - 2), (8, PORT - 2), (39, PORT - 2)]:
        m.maybe('plaza_lamp', x, y, foot=1)

    m.texture()
    m.finish_arabia()
    return m
