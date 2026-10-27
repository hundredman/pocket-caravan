"""안전한 큰길 — 제다 북문과 사나 서문을 잇는 넓은 포장 대상로 (52×32).

2026-10-26: 사나 북문이 중앙 대륙 관문이 되면서 큰길은 사나 서문으로 들어간다. 64×40 → 52×32.
- 남서 출구(제다 북문)에서 북쪽으로 올라 언덕 아래를 동쪽으로 지나, 동쪽 출구(사나 서문)로 내려간다.
  거리는 길지만 노면이 좋고 넓다 (짐이 무겁거나 수레가 있을 때 유리).
- 길가: 야자 가로수·등불·이정석, 북쪽 가운데 카라반사라이(대상 숙소)와 우물, 동쪽 경비 망루.
- 탐험: 큰길 안쪽 서편의 정원 오아시스(샛길), 동편의 대상 야영지.
  정원과 야영지 사이는 나무 울타리로 막아 큰길 안쪽을 질러가지 못한다.
"""
import arabia_art as A

W, H = 52, 32
SW_X = 5            # 남쪽 출구 (제다 북문) x 5~8
EAST_Y = 22         # 동쪽 출구 (사나 서문) y 22~25
ROAD_Y = 8          # 북쪽 가로 구간 y 8~11
EAST_X = 44         # 동쪽 세로 구간 x 44~47
HEDGE_X = 30


def build(L):
    m = A.ArabiaMap('arabia_highroad', '안전한 큰길', W, H, seed=1101, theme='town')
    m.hours = 3

    m.exit(L, 's_01', SW_X)
    m.exit(L, 'e_01', EAST_Y)
    # 북쪽 옛길 (2026-10-26): 바위 언덕 사이로 중앙 대륙 남부(모래바람 오아시스)에 바로 닿는 지름길.
    # 낙석(팀 장애물 시스템)을 치워야 지난다. 큰길 자체는 언제나 열려 있다.
    TRAIL_X = 11
    m.exit(L, 'n_01', TRAIL_X)
    m.paint('gravel', TRAIL_X, 0, TRAIL_X + 3, ROAD_Y - 1, grade=1)
    m.obstacles = [dict(id='arabia_highroad_rockslide', definitionId='rockslide', x=TRAIL_X, y=3, width=4, height=3,
                        enabled=True, phaseOffset=0.0)]
    m.paint_path('paving', [(SW_X, H - 1), (SW_X, ROAD_Y), (EAST_X, ROAD_Y), (EAST_X, EAST_Y), (W - 1, EAST_Y)], width=4, grade=3)

    m.asign(SW_X + 5, H - 6, '안전한 큰길\n넓고 평평한 포장길 · 거리는 깁니다.\n북쪽 가운데에 카라반사라이(쉼터)가 있어요.')
    m.asign(W - 4, EAST_Y - 3, '사나 서문 →')

    # ---- 북쪽 언덕 (바위) ----
    m.paint('rock', 0, 0, 15, 4, reserve=False)
    m.paint('gravel', TRAIL_X, 0, TRAIL_X + 3, ROAD_Y - 1, grade=1)   # 옛길은 바위 언덕 위에 다시
    m.rocks(1, 1, 14, 4)
    A.block_rect(m, 0, 0, TRAIL_X - 1, ROAD_Y - 1); A.block_rect(m, TRAIL_X + 4, 0, 17, ROAD_Y - 1)   # 옛길만 지나게
    m.asign(TRAIL_X + 4, 5, '북쪽 옛길 → 중앙 대륙 남부 (모래바람 오아시스)\n낙석을 치우면 사나를 거치지 않고 갈 수 있어요.')
    m.paint('rock', 36, 0, W - 1, 5, reserve=False)
    m.rocks(37, 1, 50, 5)

    # ---- 카라반사라이 (큰길 가운데 북쪽) ----
    m.paint('paving', 18, 5, 31, ROAD_Y - 1, grade=2)
    m.put('building_caravanserai', 22, 2, foot=2, force=True)
    m.put('drawn_well', 28, 3, foot=1, force=True)
    m.maybe('drawn_camelRest', 18, 3, foot=1, force=True)
    m.maybe('drawn_camel', 31, 4, foot=1, force=True)
    for x in (17, 32):
        m.maybe('drawn_cypress', x, 4)
    m.talker(21, 6, '카라반사라이 주인', 'arabia_woman_grandma_tall',
             '큰길은 멀어도 길이 넓고 평평해서 수레도 잘 굴러가요. 쉬었다 가세요.',
             '제다에서 오는 짐은 대부분 이 길로 사나에 가요. 사나 북문 너머로는 중앙 대륙 길이 난다지요.')

    # ---- 길가 가로수·등불·이정석 ----
    for x in range(11, EAST_X - 2, 5):
        if HEDGE_X - 2 <= x <= HEDGE_X + 1:
            continue
        m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, ROAD_Y + 4)
    for x in (14, 24, 36):
        m.maybe('plaza_lamp', x, ROAD_Y - 2, foot=1)
    for y in range(ROAD_Y + 6, H - 3, 6):
        if abs(y - 21) > 2:
            m.maybe('drawn_cypress', SW_X + 5, y)
        m.maybe('drawn_cypress', EAST_X - 2, y)
    for x, y in [(SW_X + 5, ROAD_Y + 5), (EAST_X - 2, ROAD_Y + 5), (26, ROAD_Y + 4)]:
        m.maybe('drawn_milestone', x, y, foot=1)
    m.maybe('wall_tower', EAST_X + 5, ROAD_Y + 4, foot=3)
    m.talker(EAST_X + 5, ROAD_Y + 9, '큰길 경비', 'arabia_npc_bedouin_tall', '이 큰길은 우리가 지켜. 도적 걱정 없이 다녀도 돼. 조금만 더 가면 사나 서문이야.')

    # ---- 큰길 안쪽 서편: 정원 오아시스 (샛길) ----
    m.paint_path('road', [(SW_X + 4, 21), (12, 21)], width=2, grade=1)
    m.ellipse('grass', 20, 21.5, 8.2, 5.4)
    m.ellipse('water', 20.5, 22, 3.6, 2.1)
    for x, y in [(13, 18), (25, 17), (13, 24), (26, 24), (19, 26)]:
        m.maybe(['drawn_orange', 'drawn_pomegranate', 'drawn_almond', 'drawn_lemon', 'drawn_oasisTree'][x % 5], x, y, force=True)
    for x, y in [(17, 19), (23, 19), (16, 24), (24, 25)]:
        m.maybe(['drawn_roseBush', 'drawn_jasmineBush', 'drawn_oleanderBush', 'drawn_marigold'][x % 4], x, y, layer='deco', force=True)
    m.maybe('drawn_lily', 19, 22, layer='deco', force=True)
    m.talker(15, 21, '정원지기', 'arabia_npc_child_tall', '큰길 사람들도 이 정원은 잘 몰라요. 물소리 들리죠?')

    # ---- 큰길 안쪽을 가로지르지 못하게: 가운데 나무 울타리 ----
    for y in range(ROAD_Y + 5, H - 1, 2):
        A.place(m, ['drawn_oasisTree', 'drawn_olive', 'drawn_oasisTree2'][y % 3], HEDGE_X, y - 1, foot=1, force=True)
    A.block_rect(m, HEDGE_X, ROAD_Y + 4, HEDGE_X + 1, H - 1)

    # ---- 큰길 안쪽 동편: 대상 야영지 ----
    m.put('building_tent', 34, 18, foot=2)
    m.maybe('drawn_camelRest', 39, 20, foot=1)
    m.maybe('drawn_cart', 35, 22, foot=1)
    m.maybe('prop_carpet', 38, 23, layer='deco')
    m.add('villager', 37, 22, variant=1)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, TRAIL_X - 1, 0); A.block_rect(m, TRAIL_X + 4, 0, W - 1, 0)
    A.block_rect(m, 0, 0, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, H - 1)
    A.block_rect(m, 0, H - 1, SW_X - 1, H - 1); A.block_rect(m, SW_X + 4, H - 1, W - 1, H - 1)
    m.border(None, SW_X + 6, H - 2, HEDGE_X - 2, H - 2)
    m.border(None, HEDGE_X + 3, H - 2, W - 2, H - 2)
    m.border(None, 1, ROAD_Y + 5, 2, H - 3)
    for x, y in [(34, 14), (40, 15), (12, 14), (36, 27)]:
        m.maybe(['drawn_olive', 'drawn_acaciaRound', 'drawn_grove'][x % 3], x, y)

    m.texture(0.04)
    m.finish_arabia()
    return m
