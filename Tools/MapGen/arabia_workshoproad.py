"""공방길 — 자비드에서 타이즈로 오르는 고원 와디(마른 골짜기) 길 (60×40).

탐험 요소
- 큰 길: 자갈길이 바위 언덕을 따라 굽이굽이 오른다. 골짜기 개울은 판자 다리로 건넌다.
- 샛길 1 (남쪽): 바위 틈 오솔길 → 숨은 샘과 야자 그늘, 목동.
- 샛길 2 (북동): 언덕 위 옛 망루 터 → 멀리 타이즈가 보이는 전망대, 노인.
- 북서 비탈: 돌담 계단밭과 커피나무 (고원 풍경).
"""
import arabia_art as A

W, H = 60, 40
WEST_Y, EAST_Y = 18, 16


def build(L):
    m = A.ArabiaMap('arabia_workshoproad', '공방길', W, H, seed=611, theme='town')
    m.hours = 2

    # ---- 고원 바닥 (바위 땅) ----
    m.paint('rock', 0, 0, W - 1, 8, reserve=False)
    m.paint('rock', 44, 0, W - 1, 14, reserve=False)
    m.paint('rock', 0, 30, 10, H - 1, reserve=False)
    m.paint('rock', 30, 30, W - 1, H - 1, reserve=False)

    # ---- 골짜기 개울 (남북) ----
    m.paint('water', 33, 0, 34, H - 1)
    m.paint('grass', 31, 0, 32, H - 1, reserve=False)
    m.paint('grass', 35, 0, 36, H - 1, reserve=False)
    m.paint('water', 33, 0, 34, H - 1)

    # ---- 큰 길 (자갈, 굽이길) ----
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    m.paint_path('gravel', [(0, WEST_Y), (12, WEST_Y), (12, 25), (26, 25), (26, 12), (44, 12), (44, EAST_Y), (59, EAST_Y)], width=4, grade=2)
    m.bridge(30, 12, 37, 15, 'h', grade=2)

    # ---- 샛길 1: 남쪽 숨은 샘 ----
    m.paint_path('road', [(18, 28), (18, 33), (22, 33)], width=2, grade=1)
    m.ellipse('grass', 24.5, 34.5, 6.5, 4)
    m.ellipse('water', 25, 34.8, 3.2, 2)
    for x, y in [(19, 33), (28, 31), (29, 35), (20, 37), (26, 38), (17, 36)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, y - 2, force=True)
    for x, y in [(23, 37), (27, 33)]:
        m.maybe('drawn_oleanderBush', x, y, layer='deco')
    m.maybe('drawn_lily', 24, 34, layer='deco')
    m.talker(22, 31, '목동', 'arabia_npc_child_tall',
             '이 샘은 길에서 안 보여서 아는 사람만 와요. 염소들이 먼저 찾았죠.',
             '타이즈 사람들은 이 물로 염료를 헹군대요.')

    # ---- 샛길 2: 북동 언덕 망루 터 ----
    m.paint_path('road', [(50, 16), (50, 5), (54, 5)], width=2, grade=1)
    m.maybe('wall_ruin', 52, 1, foot=2, force=True)
    m.maybe('wall_tower', 56, 1, foot=3, force=True)
    m.talker(51, 4, '망루 터의 노인', 'arabia_npc_scholar_tall',
             '옛날엔 여기서 대상 행렬을 세었지. 동쪽 고개 너머가 타이즈, 그 너머 바다 쪽이 제다라네.',
             '제다에서 사나로 가는 길은 둘이야. 험한 지름길과 돌아가는 큰길. 짐이 무거우면 잘 골라야 해.')
    m.maybe('drawn_milestone', 48, 9, foot=1)

    # ---- 북서 비탈: 돌담 계단밭과 커피나무 ----
    for row, y in enumerate([4, 8, 12]):
        x0, x1 = 3, 20 - row * 2
        m.paint('grass', x0, y - 2, x1, y - 1, reserve=False)
        for x in range(x0, x1 + 1):
            A.place(m, 'drawn_terrace', x, y, foot=1, force=True)
        for x in range(x0 + 1, x1, 2):
            m.maybe('drawn_coffee', x, y - 3)
    m.talker(22, 9, '커피 따는 농부', 'arabia_woman_weaver_tall',
             '높은 데 계단밭은 볕도 바람도 좋아요. 붉은 열매가 익으면 손으로 하나씩 따요.')

    # ---- 바위 언덕 (길 양옆을 감싸는 절벽 대신) ----
    m.rocks(24, 0, 31, 2)
    m.rocks(38, 0, 46, 2)
    m.rocks(17, 13, 24, 21)
    m.rocks(38, 18, 42, 26)
    m.rocks(45, 22, 56, 27)
    m.rocks(0, 33, 8, 38)
    m.rocks(38, 32, 58, 38)
    for x, y in [(6, 23), (9, 28), (28, 20), (40, 9), (55, 10), (24, 15), (47, 30)]:
        m.maybe('drawn_boulderSmall', x, y, foot=1)
    for x, y in [(3, 26), (28, 28), (47, 20), (5, 12), (40, 4), (16, 26), (55, 28), (21, 2)]:
        m.maybe(['drawn_acaciaRound', 'drawn_olive'][x % 2], x, y)
    for x, y in [(9, 23), (22, 23), (30, 9), (37, 27), (43, 29), (11, 31)]:
        m.maybe('drawn_sage', x, y, foot=1)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, W - 1, 0); A.block_rect(m, 0, H - 1, W - 1, H - 1)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, H - 1)
    m.border(None, 1, WEST_Y + 6, 2, H - 3, kind='rock')
    m.border(None, 57, EAST_Y + 6, 58, H - 3, kind='rock')

    m.asign(4, WEST_Y + 4, '공방길\n동쪽 고개를 넘으면 타이즈입니다.\n남쪽 바위 틈과 북동 언덕에 샛길이 있어요.')
    m.asign(53, EAST_Y + 4, '← 자비드 / 타이즈 →\n염색 공방의 마을')
    m.texture(0.04)
    m.finish_arabia()
    return m
