"""제다 — 교역 중계 항구 마을 (60×44).

- 서쪽 항구길로 들어오면 큰길이 마을을 가로질러 동쪽 '험한 지름길'(사나)로 이어진다.
- 북쪽 출구: 사나로 돌아가는 '안전한 큰길'.
- 남쪽: 바다와 판자 선착장 3곳, 정박한 다우 배, 물류 마당(짐 상자·통), 생선 노점.
- 가운데: 항구 광장(장터·게시판), 수레 장인 공방(수레·바퀴).
- 북쪽: 산호석 마슈라비야 집들.
"""
import arabia_art as A

W, H = 60, 44
WEST_Y, EAST_Y, NORTH_X = 14, 20, 28


def build(L):
    m = A.ArabiaMap('arabia_jeddah', '제다', W, H, seed=901, theme='town')

    # ---- 바다·부두 ----
    m.paint('water', 0, 35, W - 1, H - 1)
    m.paint('paving', 2, 31, 57, 34, grade=2)                         # 부두 거리 (석축)
    for x0 in (8, 26, 44):
        m.bridge(x0, 35, x0 + 2, 41, 'v', grade=1)                    # 선착장
    for x, y in [(11, 37), (29, 38), (47, 37), (17, 39), (38, 40)]:
        m.maybe('drawn_dhow', x, y - 2, layer='deco')

    # ---- 길 ----
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    m.exit(L, 'n_01', NORTH_X)
    m.paint('paving', 0, WEST_Y, 40, WEST_Y + 3, grade=3)             # 서쪽 큰길
    m.paint('paving', 37, WEST_Y, 40, EAST_Y + 3, grade=3)
    m.paint('paving', 37, EAST_Y, W - 1, EAST_Y + 3, grade=3)         # 동쪽 → 지름길
    m.paint('paving', NORTH_X, 0, NORTH_X + 3, WEST_Y, grade=3)       # 북쪽 → 큰길
    m.paint('paving', 41, WEST_Y, 56, WEST_Y, grade=2)                # 북동 골목
    m.paint('paving', 18, 19, 34, 30, grade=3)                        # 항구 광장
    m.paint('zellige', 23, 22, 29, 27)
    m.paint('paving', 24, WEST_Y + 4, 27, 18, grade=2)
    m.paint('paving', 12, WEST_Y + 4, 13, 30, grade=2)                # 서쪽 골목 → 부두
    m.paint('paving', 48, EAST_Y + 4, 49, 30, grade=2)                # 동쪽 골목 → 부두

    # ---- 항구 광장 ----
    m.put('drawn_well', 25, 22, foot=1, force=True)
    m.add('market', 20, 28, town='arabia_jeddah')
    m.put('stall_fruit', 18, 25, foot=1, force=True)
    m.put('stall_spice', 31, 25, foot=1, force=True)
    m.put('plaza_board', 31, 19, foot=1, solid=0.05, force=True)
    A.block_rect(m, 31, 21, 32, 21)
    m.add('board', 31, 21, town='arabia_jeddah')
    for x, y in [(18, 18), (34, 18), (18, 29), (34, 29)]:
        m.maybe('plaza_lamp', x, y - 1, foot=1, force=True)
    m.talker(28, 29, '물류 담당', 'arabia_npc_sailor_tall',
             '배로 들어온 짐을 나눠서 사나로 보내는 게 내 일이야. 큰길은 안전하지만 멀고, 지름길은 험해.',
             '사나에 갈 짐이 무거우면 큰길이 결국 빠를 수도 있어.')
    m.asign(16, 19, '제다 항구\n북: 안전한 큰길(사나) · 동: 험한 지름길(사나) · 서: 항구길(타이즈) · 남: 부두')

    # ---- 수레 장인 공방 (동쪽 큰길가) ----
    m.building('house_shopHouse', 42, 15, EAST_Y)
    m.maybe('drawn_cart', 47, 17, foot=1)
    m.maybe('drawn_cart', 51, 17, foot=1)
    m.maybe('drawn_barrel', 55, 18, foot=1)
    m.talker(46, 19, '수레 장인 유수프', 'arabia_npc_bedouin_tall',
             '등짐만으론 한계가 있지. 손수레가 있으면 훨씬 많이 실을 수 있어.',
             '바퀴는 사나 가는 큰길에서 제일 잘 굴러가.')

    # ---- 물류 마당·생선 노점 (부두) ----
    for x, y in [(3, 28), (6, 28), (52, 28), (55, 28)]:
        m.maybe('drawn_crates', x, y, foot=1)
    for x in (16, 36, 40, 42):
        m.maybe('drawn_barrel', x, 30, foot=1)
    m.maybe('drawn_netRack', 4, 23, foot=1)
    m.maybe('stall_bread', 38, 27, foot=1)
    m.maybe('prop_grainsacks', 43, 28, foot=1)
    m.add('villager', 22, 32, variant=0)
    m.add('villager', 52, 32, variant=2)

    # ---- 북쪽 산호석 집 ----
    for x, h in [(3, 'house_mashrabiya'), (8, 'house_white'), (13, 'house_mashrabiya'), (19, 'house_sand')]:
        m.building(h, x, 8 if h != 'house_mashrabiya' else 7, WEST_Y)
    m.building('house_courtyardManor', 33, 7, WEST_Y)
    m.building('house_mashrabiya', 41, 7, WEST_Y + 0)
    m.building('house_vineHouse', 47, 7, WEST_Y)
    m.maybe('mosque_minaretSquare', 54, 6, foot=2)
    m.add('villager', 36, 13, variant=1)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, NORTH_X - 1, 0); A.block_rect(m, NORTH_X + 4, 0, W - 1, 0)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, 34)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, 34)
    m.border(None, 1, 3, NORTH_X - 2, 3)
    m.border(None, NORTH_X + 5, 3, W - 2, 3)
    m.border(None, 1, WEST_Y + 6, 2, 26)
    m.border(None, 57, 3, 58, EAST_Y - 2)
    for x, y in [(6, 18), (20, 15), (52, 24), (34, 12), (9, 26)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2'][x % 2], x, y - 2)
    m.texture(0.035)
    m.finish_arabia()
    return m
