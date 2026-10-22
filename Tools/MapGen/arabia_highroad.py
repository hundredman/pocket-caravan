"""안전한 큰길 — 제다 북문과 사나 북문을 잇는 넓은 포장 대상로 (64×40).

- 남쪽 두 출구: 서쪽(제다 북문), 동쪽(사나 북문). 큰길은 북쪽으로 크게 돌아가 거리는 길지만 노면이 좋고 넓다.
- 길가: 야자 가로수·등불·이정석, 가운데 카라반사라이(대상 숙소)와 우물, 경비 망루.
- 탐험: 큰길 안쪽의 정원 오아시스(샛길), 북쪽 언덕의 별 보는 노인.
"""
import arabia_art as A

W, H = 64, 40
SW_X, SE_X = 6, 54
ROAD_Y = 9


def build(L):
    m = A.ArabiaMap('arabia_highroad', '안전한 큰길', W, H, seed=1101, theme='town')
    m.hours = 3

    m.exit(L, 's_01', SW_X)
    m.exit(L, 's_02', SE_X)
    m.paint_path('paving', [(SW_X, H - 1), (SW_X, ROAD_Y), (SE_X, ROAD_Y), (SE_X, H - 1)], width=4, grade=3)

    # ---- 카라반사라이 (큰길 가운데 북쪽) ----
    m.paint('paving', 26, 4, 37, ROAD_Y - 1, grade=2)
    m.put('building_caravanserai', 29, 1, foot=2, force=True)
    m.put('drawn_well', 34, 4, foot=1, force=True)
    m.maybe('drawn_camelRest', 24, 6, foot=1, force=True)
    m.maybe('drawn_camel', 38, 5, foot=1, force=True)
    m.talker(28, 7, '카라반사라이 주인', 'arabia_woman_grandma_tall',
             '큰길은 멀어도 길이 넓고 평평해서 수레도 잘 굴러가요. 쉬었다 가세요.',
             '제다에서 오는 짐은 대부분 이 길로 사나에 가요.')

    # ---- 길가 가로수·등불·이정석 ----
    for x in range(SW_X + 6, SE_X - 1, 6):
        m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, ROAD_Y + 4)
        if x % 12 == 0:
            m.maybe('plaza_lamp', x + 2, ROAD_Y - 2, foot=1)
    for y in range(ROAD_Y + 6, H - 3, 7):
        for x in (SW_X - 2, SW_X + 5, SE_X - 2, SE_X + 5):
            m.maybe('drawn_cypress', x, y)
    for x, y in [(SW_X + 5, ROAD_Y + 5), (SE_X - 1, ROAD_Y + 5), (30, ROAD_Y + 4)]:
        m.maybe('drawn_milestone', x, y, foot=1)
    m.maybe('wall_tower', SE_X + 6, 2, foot=3)
    m.talker(SE_X + 5, 7, '큰길 경비', 'arabia_npc_bedouin_tall', '이 큰길은 우리가 지켜. 도적 걱정 없이 다녀도 돼.')

    # ---- 큰길 안쪽 정원 오아시스 (샛길) ----
    m.paint_path('road', [(20, ROAD_Y + 4), (20, 22), (28, 22)], width=2, grade=1)
    m.ellipse('grass', 33, 25, 11, 7)
    m.ellipse('water', 33, 26, 4.5, 2.6)
    for x, y in [(24, 21), (41, 22), (26, 30), (40, 30), (33, 32)]:
        m.maybe(['drawn_orange', 'drawn_pomegranate', 'drawn_almond', 'drawn_lemon', 'drawn_oasisTree'][x % 5], x, y, force=True)
    for x, y in [(29, 23), (37, 23), (30, 29), (36, 29)]:
        m.maybe(['drawn_roseBush', 'drawn_jasmineBush', 'drawn_oleanderBush', 'drawn_marigold'][x % 4], x, y, layer='deco', force=True)
    m.maybe('drawn_lily', 32, 26, layer='deco', force=True)
    m.talker(28, 27, '정원지기', 'arabia_npc_child_tall',
             '큰길 사람들도 이 정원은 잘 몰라요. 물소리 들리죠?')

    # ---- 큰길 안쪽을 가로지르지 못하게: 정원 동쪽 나무 울타리 (큰길을 따라 돌아가야 함) ----
    for y in range(ROAD_Y + 5, H - 1, 2):
        A.place(m, ['drawn_oasisTree', 'drawn_olive', 'drawn_oasisTree2'][y % 3], 45, y - 1, foot=1, force=True)
    A.block_rect(m, 45, ROAD_Y + 4, 46, H - 1)

    # ---- 북서 언덕 ----
    m.paint('rock', 0, 0, 20, 5, reserve=False)
    m.rocks(1, 1, 18, 4)
    m.paint('rock', 44, 0, W - 1, 5, reserve=False)
    m.rocks(42, 1, 52, 4)

    # ---- 가장자리 숲 ----
    A.block_rect(m, 0, 0, W - 1, 0)
    A.block_rect(m, 0, 0, 0, H - 1); A.block_rect(m, W - 1, 0, W - 1, H - 1)
    A.block_rect(m, 0, H - 1, SW_X - 1, H - 1); A.block_rect(m, SW_X + 4, H - 1, SE_X - 1, H - 1); A.block_rect(m, SE_X + 4, H - 1, W - 1, H - 1)
    m.border(None, SW_X + 6, H - 2, SE_X - 3, H - 2)
    m.border(None, 1, ROAD_Y + 6, 2, H - 3)
    m.border(None, W - 3, ROAD_Y + 6, W - 2, H - 3)
    for x, y in [(14, 20), (46, 18), (12, 32), (48, 30), (16, 27), (44, 26)]:
        m.maybe(['drawn_olive', 'drawn_acaciaRound', 'drawn_grove'][x % 3], x, y)

    m.asign(SW_X + 4, H - 6, '안전한 큰길\n넓고 평평한 포장길 · 거리는 깁니다.\n가운데에 카라반사라이(쉼터)가 있어요.')
    m.asign(SE_X - 2, H - 6, '사나 북문 ↓')
    m.texture(0.04)
    m.finish_arabia()
    return m
