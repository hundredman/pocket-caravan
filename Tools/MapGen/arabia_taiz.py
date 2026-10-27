"""타이즈 — 언덕 위 염색 공방 마을 (56×40).

- 서쪽 고개(공방길)에서 들어오면 큰길이 마을 광장을 지나 동쪽 항구길로 이어진다.
- 남서 염색 마당: 쪽빛·꼭두서니·사프란 염료 통과 천 말리는 대, 염색 장인.
- 북쪽 언덕: 성채 터와 바위, 돌계단 오솔길 (전망).
- 남쪽 비탈: 커피 계단밭.
- 광장: 장터(타이즈)·게시판·분수, 창고(살마)·직물 상인(라시드)은 이야기꾼으로 먼저 배치 (Q3·Q6는 다음 단계).
"""
import arabia_art as A

W, H = 56, 40
WEST_Y, EAST_Y = 16, 22


def build(L):
    m = A.ArabiaMap('arabia_taiz', '타이즈', W, H, seed=701, theme='town')

    # ---- 언덕 바닥 ----
    m.paint('rock', 0, 0, W - 1, 9, reserve=False)
    m.paint('rock', 0, 32, W - 1, H - 1, reserve=False)

    # ---- 길 ----
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    m.paint('paving', 0, WEST_Y, 21, WEST_Y + 3, grade=3)            # 서쪽 큰길
    m.paint('paving', 18, WEST_Y, 21, EAST_Y + 3, grade=3)           # 광장 쪽으로 꺾임
    m.paint('paving', 18, EAST_Y, W - 1, EAST_Y + 3, grade=3)        # 동쪽 큰길 → 항구길
    m.paint('paving', 22, 11, 38, 21, grade=3)                       # 광장
    m.paint('zellige', 27, 13, 33, 18)
    m.paint('paving', 4, 11, 52, 12, grade=2)                        # 북쪽 골목
    m.paint('road', 40, 4, 41, 10, grade=1)                          # 성채 터 오르는 길
    m.paint('paving', 44, 13, 45, EAST_Y - 1, grade=2)               # 동쪽 세로 골목
    m.paint('road', 30, 26, 31, 31, grade=1)                         # 남쪽 계단밭 길

    # ---- 광장 ----
    m.put('building_fountain', 29, 14, foot=2, force=True)
    m.add('market', 25, 20, town='arabia_taiz')
    m.put('stall_cloth', 23, 17, foot=1, force=True)
    m.put('plaza_board', 35, 12, foot=1, solid=0.05, force=True)
    A.block_rect(m, 35, 14, 36, 14)
    m.add('board', 35, 14, town='arabia_taiz')
    for x, y in [(22, 10), (38, 10), (22, 20), (38, 20)]:
        m.maybe('plaza_lamp', x, y - 1, foot=1, force=True)
    m.maybe('drawn_pottedOrange', 26, 12, foot=1, force=True)
    m.maybe('drawn_pottedOrange', 34, 18, foot=1, force=True)
    m.talker(33, 20, '직물 상인 라시드', 'arabia_npc_merchant_tall',
             '타이즈 천은 사나에서 값이 좋아. 다만 다들 같은 걸 들고 가면 값이 내려가지.',
             '제다 항구 상인들도 천을 찾는다네. 어디에 팔지는 장부를 보고 정하게.')
    m.asign(19, 12, '타이즈 광장\n북: 성채 터 · 고원길 → 사나 · 동: 커피 오르막 → 자비드\n서: 룹알할리 빈 사막 · 남서: 염색 마당')

    # ---- 북쪽 집들 (골목 위) ----
    for x, h in [(4, 'house_rose'), (9, 'house_white'), (14, 'house_mudbrick')]:
        m.building(h, x, 7, 11)
    m.building('house_mashrabiya', 44, 6, 11)
    m.building('house_windTowerHouse', 49, 6, 11)
    # 성채 터
    for x in range(30, 38, 3):
        m.maybe('wall_wall', x, 2, foot=2, force=True)
    m.maybe('wall_tower', 38, 0, foot=3, force=True)
    m.maybe('wall_tower', 27, 0, foot=3, force=True)
    m.talker(41, 3, '성채 터 경비', 'arabia_npc_bedouin_tall', '여기서 보면 서쪽 고개와 동쪽 바닷길이 다 보여.')

    # ---- 남서 염색 마당 ----
    m.paint('brick', 2, 21, 15, 29, reserve=False)
    for i, x in enumerate(range(3, 14, 3)):
        m.maybe(['drawn_vatIndigo', 'drawn_vatMadder', 'drawn_vatSaffron'][i % 3], x, 22, foot=1)
        m.maybe(['drawn_vatSaffron', 'drawn_vatIndigo', 'drawn_vatMadder'][i % 3], x, 25, foot=1)
    m.maybe('drawn_clothRack', 3, 27, foot=1)
    m.maybe('drawn_clothRack2', 9, 27, foot=1)
    m.put('house_shopHouse', 8, 31, foot=2)   # 공방
    m.talker(14, 24, '염색 장인', 'arabia_woman_weaver_tall',
             '쪽빛은 세 번, 꼭두서니는 두 번 담가야 고운 색이 나와요.',
             '말린 천은 꾸러미로 묶어 광장 장터에 내놓지요.')

    # ---- 창고 (살마) ----
    m.building('house_courtyardManor', 46, 26, EAST_Y + 4)
    m.maybe('drawn_crates', 42, 27, foot=1)
    m.maybe('drawn_barrel', 53, 29, foot=1)
    m.talker(44, 30, '창고 관리인 살마', 'arabia_woman_calligrapher_tall',
             '장부에 적힌 수량과 실제 짐이 맞아야 해요. 하나라도 다르면 다시 세지요.')

    # ---- 남쪽 커피 계단밭 ----
    for row, y in enumerate([33, 36]):
        m.paint('grass', 20, y - 2, 42, y - 1, reserve=False)
        for x in range(20, 43):
            if x not in (30, 31):
                A.place(m, 'drawn_terrace', x, y, foot=1, force=True)
        for x in range(21, 42, 2):
            if x not in (29, 30, 31):
                m.maybe('drawn_coffee', x, y - 2)
    m.add('villager', 33, 29, variant=1)

    m.building('house_awning', 39, 14, EAST_Y)
    m.maybe('drawn_clothRack2', 39, 19, foot=1)

    # ---- 동쪽 길가 ----
    m.building('house_cottage', 48, 15, EAST_Y)
    m.add('villager', 50, 20, variant=2)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, W - 1, 0); A.block_rect(m, 0, H - 1, W - 1, H - 1)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, H - 1)
    m.border(None, 1, 6, 2, WEST_Y - 2, kind='rock')
    m.border(None, 1, WEST_Y + 6, 2, H - 2, kind='rock')
    m.border(None, 53, 3, 54, EAST_Y - 2)
    m.border(None, 53, EAST_Y + 6, 54, H - 2)
    m.rocks(16, 1, 25, 5)
    m.rocks(3, 33, 17, 38)
    m.rocks(44, 34, 52, 38)
    for x, y in [(18, 27), (38, 27), (24, 6), (6, 18)]:
        m.maybe(['drawn_olive', 'drawn_acaciaRound'][x % 2], x, y)
    m.texture(0.04)
    m.finish_arabia()
    return m
