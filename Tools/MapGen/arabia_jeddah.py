"""제다 — 교역 중계 항구 마을 (56×40).

2026-10-26 회의(스타팅 지역 2만 칸 이내)에 맞춰 60×44 → 56×40으로 줄였다.
- 서쪽 항구길로 들어오면 큰길이 마을을 가로질러 동쪽 '험한 지름길'(사나 남문)로 이어진다.
- 북쪽 출구: 사나 서문으로 돌아가는 '안전한 큰길'.
- 남쪽: 바다와 판자 선착장 3곳, 정박한 다우 배, 물류 마당(짐 상자·통), 생선 노점.
- 가운데: 항구 광장(장터·게시판), 수레 장인 공방(수레·바퀴).
- 북쪽: 산호석 마슈라비야 집들.
"""
import arabia_art as A

W, H = 56, 40
WEST_Y, EAST_Y, NORTH_X = 12, 18, 26
SEA_Y = 32


def build(L):
    m = A.ArabiaMap('arabia_jeddah', '제다', W, H, seed=901, theme='town')

    # ---- 바다·부두 ----
    m.paint('water', 0, SEA_Y, W - 1, H - 1)
    m.paint('paving', 2, SEA_Y - 3, W - 3, SEA_Y - 1, grade=2)            # 부두 거리 (석축)
    for x0 in (7, 24, 41):
        m.bridge(x0, SEA_Y, x0 + 2, SEA_Y + 5, 'v', grade=1)              # 선착장
    for x, y in [(10, 34), (27, 35), (44, 34), (16, 36), (35, 36)]:
        m.maybe('drawn_dhow', x, y, layer='deco')

    # ---- 길 ----
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    m.exit(L, 'n_01', NORTH_X)
    m.paint('paving', 0, WEST_Y, 36, WEST_Y + 3, grade=3)               # 서쪽 큰길
    m.paint('paving', 33, WEST_Y, 36, EAST_Y + 3, grade=3)
    m.paint('paving', 33, EAST_Y, W - 1, EAST_Y + 3, grade=3)           # 동쪽 → 지름길
    m.paint('paving', NORTH_X, 0, NORTH_X + 3, WEST_Y, grade=3)         # 북쪽 → 큰길
    m.paint('paving', 37, WEST_Y, 52, WEST_Y, grade=2)                  # 북동 골목
    m.paint('paving', 16, 17, 31, SEA_Y - 4, grade=3)                   # 항구 광장
    m.paint('zellige', 21, 20, 26, 24)
    m.paint('paving', 22, WEST_Y + 4, 25, 16, grade=2)
    m.paint('paving', 10, WEST_Y + 4, 11, SEA_Y - 4, grade=2)           # 서쪽 골목 → 부두
    m.paint('paving', 44, EAST_Y + 4, 45, SEA_Y - 4, grade=2)           # 동쪽 골목 → 부두

    # ---- 항구 광장 ----
    m.put('drawn_well', 23, 20, foot=1, force=True)
    m.add('market', 18, 26, town='arabia_jeddah')
    m.put('stall_fruit', 16, 22, foot=1, force=True)
    m.put('stall_spice', 29, 22, foot=1, force=True)
    m.put('plaza_board', 29, 17, foot=1, solid=0.05, force=True)
    A.block_rect(m, 29, 19, 30, 19)
    m.add('board', 29, 19, town='arabia_jeddah')
    for x, y in [(16, 17), (31, 17), (16, 27), (31, 27)]:
        m.maybe('plaza_lamp', x, y - 1, foot=1, force=True)
    m.maybe('drawn_pottedOrange', 20, 18, foot=1, force=True)
    m.maybe('drawn_pottedOrange', 27, 25, foot=1, force=True)
    m.talker(26, 26, '물류 담당', 'arabia_npc_sailor_tall',
             '배로 들어온 짐을 나눠서 사나로 보내는 게 내 일이야. 해안길로 자비드를 거쳐 고원으로 올라가지.',
             '급하면 알 아흐사에서 빈 사막을 질러가는 사람도 있지만, 모래가 깊어서 무거운 짐엔 안 맞아.')
    m.asign(13, 17, '제다 항구\n북: 홍해 해안길 → 자비드 → 타이즈 → 사나\n서: 나즈드 대상로 → 알 아흐사 · 남: 부두')

    # ---- 수레 장인 공방 (동쪽 큰길가) ----
    m.building('house_shopHouse', 38, 13, EAST_Y)
    m.maybe('drawn_cart', 43, 15, foot=1)
    m.maybe('drawn_cart', 47, 15, foot=1)
    m.maybe('drawn_barrel', 51, 16, foot=1)
    m.talker(42, 17, '수레 장인 유수프', 'arabia_npc_bedouin_tall',
             '등짐만으론 한계가 있지. 손수레가 있으면 훨씬 많이 실을 수 있어.',
             '바퀴는 사나 가는 고원 돌길에서 제일 잘 굴러가. 빈 사막 모래에선 헛돌지.')

    # ---- 물류 마당·생선 노점 (부두) ----
    for x, y in [(3, 26), (6, 26), (48, 26), (51, 26)]:
        m.maybe('drawn_crates', x, y, foot=1)
    for x in (14, 33, 36, 38):
        m.maybe('drawn_barrel', x, SEA_Y - 5, foot=1)
    m.maybe('drawn_netRack', 4, 21, foot=1)
    m.maybe('stall_bread', 35, 24, foot=1)
    m.maybe('prop_grainsacks', 40, 25, foot=1)
    m.add('villager', 20, SEA_Y - 2, variant=0)
    m.add('villager', 48, SEA_Y - 2, variant=2)

    # ---- 북쪽 산호석 집 ----
    for x, h in [(3, 'house_mashrabiya'), (8, 'house_white'), (12, 'house_mashrabiya'), (17, 'house_sand')]:
        m.building(h, x, 9 if h != 'house_mashrabiya' else 7, WEST_Y)
    m.building('house_courtyardManor', 30, 7, WEST_Y)
    m.building('house_mashrabiya', 38, 7, WEST_Y)
    m.building('house_vineHouse', 43, 8, WEST_Y)
    m.maybe('mosque_minaretSquare', 50, 6, foot=2)
    m.add('villager', 32, WEST_Y + 1, variant=1)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, NORTH_X - 1, 0); A.block_rect(m, NORTH_X + 4, 0, W - 1, 0)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, SEA_Y - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, SEA_Y - 1)
    m.border(None, 1, 3, NORTH_X - 2, 3)
    m.border(None, NORTH_X + 5, 3, W - 2, 3)
    m.border(None, 1, WEST_Y + 6, 2, 24)
    m.border(None, W - 3, 3, W - 2, EAST_Y - 2)
    for x, y in [(6, 16), (19, 14), (50, 23), (14, 25), (41, 23)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2'][x % 2], x, y - 2)
    m.patrol('제다 항구 경비병', [(3, 13), (27, 13), (27, 3), (27, 13), (45, 19)], '배에서 내린 짐은 세금 장부부터 확인하시오.',
             '밤바다에 등불 단 다우 배가 들어오는 걸 보면 꽤 근사하다오.')
    m.texture(0.035)
    m.finish_arabia()
    return m
