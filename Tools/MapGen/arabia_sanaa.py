"""사나 — 성벽으로 둘러싼 수도 (64×48). 여러 산지 상품의 큰 소비처.

- 서문: 험한 지름길(제다)에서 들어온다. 북문: 안전한 큰길(제다).
- 십자 대로가 만나는 큰 광장: 분수·장터(사나)·게시판·나디아(먼 마을 의뢰인, Q4·Q5는 다음 단계).
- 북서: 큰 모스크·마드라사 / 북동: 궁전·도서관 / 남서: 수크(덮인 시장과 노점 거리) / 남동: 탑집 골목과 정원(부스탄).
"""
import arabia_art as A

W, H = 64, 48
WEST_Y, NORTH_X = 20, 30


def build(L):
    m = A.ArabiaMap('arabia_sanaa', '사나', W, H, seed=1201, theme='town')

    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'n_01', NORTH_X)
    # ---- 대로 ----
    m.paint('paving', 0, WEST_Y, 60, WEST_Y + 3, grade=3)              # 서문 → 동쪽 대로
    m.paint('paving', NORTH_X, 0, NORTH_X + 3, 43, grade=3)           # 북문 → 남쪽 대로
    m.paint('paving', 22, 15, 42, 29, grade=3)                         # 큰 광장
    m.paint('zellige', 27, 17, 37, 27)
    m.paint('paving', 4, 13, 60, 14, grade=2)                          # 북쪽 골목
    m.paint('paving', 4, 33, 60, 34, grade=2)                          # 남쪽 골목
    m.paint('paving', 12, 13, 13, 43, grade=2)
    m.paint('paving', 50, 13, 51, 43, grade=2)

    # ---- 성벽 ----
    for x in list(range(2, NORTH_X - 2, 3)) + list(range(NORTH_X + 5, W - 3, 3)):
        m.maybe('wall_wall', x, 0, foot=2, force=True)
    for x in range(2, W - 3, 3):
        m.maybe('wall_wall', x, H - 2, foot=2, force=True)
    m.maybe('wall_tower', NORTH_X - 2, 0, foot=3, force=True)
    m.maybe('wall_tower', NORTH_X + 4, 0, foot=3, force=True)
    for y in list(range(3, WEST_Y - 3, 4)) + list(range(WEST_Y + 6, H - 4, 4)):
        m.maybe('wall_tower', 0, y, foot=4, force=True)
        m.maybe('wall_tower', W - 2, y, foot=4, force=True)
    m.maybe('wall_tower', 0, WEST_Y - 4, foot=4, force=True)
    m.maybe('wall_tower', 0, WEST_Y + 4, foot=4, force=True)
    A.block_rect(m, 0, 0, NORTH_X - 1, 1); A.block_rect(m, NORTH_X + 4, 0, W - 1, 1)
    A.block_rect(m, 0, H - 2, W - 1, H - 1)
    A.block_rect(m, 0, 0, 1, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 1, H - 1)
    A.block_rect(m, W - 2, 0, W - 1, H - 1)

    # ---- 큰 광장 ----
    m.put('building_fountain', 31, 21, foot=2, force=True)
    m.add('market', 25, 27, town='arabia_sanaa')
    m.put('stall_spice', 23, 24, foot=1, force=True)
    m.put('stall_cloth', 39, 24, foot=1, force=True)
    m.put('plaza_board', 39, 16, foot=1, solid=0.05, force=True)
    A.block_rect(m, 39, 18, 40, 18)
    m.add('board', 39, 18, town='arabia_sanaa')
    for x, y in [(22, 15), (42, 15), (22, 29), (42, 29)]:
        m.maybe('plaza_lamp', x, y - 1, foot=1, force=True)
    for x, y in [(26, 16), (36, 16), (26, 28), (36, 28)]:
        m.maybe('drawn_pottedOrange', x, y - 1, foot=1, force=True)
    m.talker(29, 28, '시장 상인 나디아', 'arabia_woman_perfumer_tall',
             '사나 시장엔 여러 마을 물건이 모여요. 대추야자, 곡물, 타이즈 천, 제다 향신료까지.',
             '먼 길로 납품해 줄 짐꾼을 늘 찾고 있지요.')
    m.asign(20, 17, '사나 큰 광장\n서문: 험한 지름길(제다) · 북문: 안전한 큰길(제다)\n북서: 큰 모스크 · 북동: 궁전 · 남서: 수크 · 남동: 정원')

    # ---- 북서: 모스크·마드라사 ----
    m.building('mosque_greatMosque', 3, 7, 13)
    m.building('mosque_madrasa', 14, 7, 13)
    m.building('house_windTowerHouse', 22, 8, 13)
    # ---- 북동: 궁전·도서관 ----
    m.building('civic_palace', 38, 6, 13)
    m.building('civic_library', 52, 7, 13)
    m.maybe('drawn_cypress', 36, 9)
    m.maybe('drawn_cypress', 47, 9)
    m.talker(45, 15, '궁전 문지기', 'arabia_npc_bedouin_tall', '수도 사나에 온 걸 환영하오. 큰 광장 시장은 언제나 붐비지.')

    # ---- 남서: 수크 ----
    m.building('civic_bazaar', 3, 27, 33)
    for i, x in enumerate(range(15, 28, 4)):
        m.maybe(['stall_fruit', 'stall_pottery', 'stall_perfume', 'stall_dates'][i % 4], x, 30, foot=1)
    m.maybe('civic_hammam', 15, 37, foot=2)
    m.maybe('house_windTowerHouse', 3, 37, foot=2)
    m.maybe('house_mashrabiya', 22, 37, foot=2)
    for x, y in [(8, 41), (26, 42), (11, 38)]:
        m.maybe(['drawn_crates', 'drawn_barrel', 'prop_grainsacks'][x % 3], x, y, foot=1)
    for x, y in [(9, 44), (20, 44)]:
        m.maybe('drawn_datePalm2', x, y - 2)
    m.add('villager', 10, 25, variant=0)
    m.add('villager', 20, 37, variant=1)

    # ---- 남동: 탑집 골목과 정원 ----
    m.building('house_windTowerHouse', 44, 28, 33)
    m.building('civic_bimaristan', 54, 27, 33)
    m.paint('grass', 36, 37, 58, 43, reserve=False)
    for x, y in [(37, 38), (41, 40), (45, 38), (49, 40), (53, 38), (57, 40)]:
        m.maybe(['drawn_orange', 'drawn_pomegranate', 'drawn_almond', 'drawn_lemon', 'drawn_oasisTree', 'drawn_olive'][x % 6], x, y, force=True)
    for x, y in [(39, 42), (47, 42), (55, 42), (43, 37)]:
        m.maybe(['drawn_roseBush', 'drawn_jasmineBush', 'drawn_marigold', 'drawn_oleanderBush'][x % 4], x, y, layer='deco', force=True)
    m.talker(44, 41, '정원사', 'arabia_woman_grandma_tall', '이 정원 열매는 궁전 부엌으로 가요. 물은 깊은 우물에서 길어 오지요.')
    m.add('villager', 56, 22, variant=2)

    for x, y in [(6, 17), (46, 25), (17, 25), (57, 17)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2'][x % 2], x, y - 2)
    m.texture(0.03)
    m.finish_arabia()
    return m
