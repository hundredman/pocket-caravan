"""공방길 — 자비드에서 타이즈로 오르는 고원 와디(마른 골짜기) 길 (48×32).

2026-10-26 회의(스타팅 지역 2만 칸 이내)에 맞춰 60×40 → 48×32로 줄였다. 길 구조와 샛길은 그대로.
- 큰 길: 자갈길이 바위 언덕을 따라 굽이굽이 오른다. 골짜기 개울은 판자 다리로 건넌다.
- 샛길 1 (남쪽): 바위 틈 오솔길 → 숨은 샘과 야자 그늘, 목동.
- 샛길 2 (북동): 언덕 위 옛 망루 터 → 전망, 노인.
- 북서 비탈: 돌담 계단밭과 커피나무 (고원 풍경).
"""
import arabia_art as A

W, H = 48, 32
WEST_Y, EAST_Y = 14, 12
CREEK = 27


def build(L):
    m = A.ArabiaMap('arabia_workshoproad', '공방길', W, H, seed=611, theme='town')
    m.hours = 2

    # ---- 고원 바닥 (바위 땅) ----
    m.paint('rock', 0, 0, W - 1, 6, reserve=False)
    m.paint('rock', 36, 0, W - 1, 11, reserve=False)
    m.paint('rock', 0, 25, 8, H - 1, reserve=False)
    m.paint('rock', 30, 25, W - 1, H - 1, reserve=False)

    # ---- 골짜기 개울 (남북) ----
    m.paint('grass', CREEK - 2, 0, CREEK - 1, H - 1, reserve=False)
    m.paint('grass', CREEK + 2, 0, CREEK + 3, H - 1, reserve=False)
    m.paint('water', CREEK, 0, CREEK + 1, H - 1)

    # ---- 큰 길 (자갈, 굽이길) ----
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    m.paint_path('gravel', [(0, WEST_Y), (10, WEST_Y), (10, 20), (21, 20), (21, 9), (36, 9), (36, EAST_Y), (W - 1, EAST_Y)],
                 width=4, grade=2)
    m.bridge(CREEK - 2, 9, CREEK + 3, 12, 'h', grade=2)

    # ---- 샛길 1: 남쪽 숨은 샘 ----
    m.paint_path('road', [(15, 24), (15, 27), (17, 27)], width=2, grade=1)
    m.ellipse('grass', 20.5, 28, 5.2, 3.2)
    m.ellipse('water', 21, 28.4, 2.6, 1.5)
    for x, y in [(16, 30), (24, 26), (25, 29), (18, 31)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, y - 2, force=True)
    m.maybe('drawn_oleanderBush', 19, 30, layer='deco')
    m.maybe('drawn_oleanderBush', 23, 26, layer='deco')
    m.maybe('drawn_lily', 20, 28, layer='deco')
    m.talker(18, 25, '목동', 'arabia_npc_child_tall',
             '이 샘은 길에서 안 보여서 아는 사람만 와요. 염소들이 먼저 찾았죠.',
             '타이즈 사람들은 이 물로 염료를 헹군대요.')

    # ---- 샛길 2: 북동 언덕 망루 터 ----
    m.paint_path('road', [(41, EAST_Y), (41, 4), (44, 4)], width=2, grade=1)
    m.maybe('wall_ruin', 41, 1, foot=2, force=True)
    m.maybe('wall_tower', 45, 0, foot=3, force=True)
    m.talker(43, 3, '망루 터의 노인', 'arabia_npc_scholar_tall',
             '옛날엔 여기서 대상 행렬을 세었지. 동쪽 고개 너머가 타이즈, 그 너머 바다 쪽이 제다라네.',
             '제다에서 사나로 가는 길은 둘이야. 험한 지름길과 돌아가는 큰길. 짐이 무거우면 잘 골라야 해.')
    m.maybe('drawn_milestone', 39, 6, foot=1)

    # ---- 북서 비탈: 돌담 계단밭과 커피나무 ----
    for row, y in enumerate([4, 8, 12]):
        x0, x1 = 3, 16 - row * 2
        m.paint('grass', x0, y - 2, x1, y - 1, reserve=False)
        for x in range(x0, x1 + 1):
            A.place(m, 'drawn_terrace', x, y, foot=1, force=True)
        for x in range(x0 + 1, x1, 2):
            m.maybe('drawn_coffee', x, y - 3)
    m.talker(18, 7, '커피 따는 농부', 'arabia_woman_weaver_tall',
             '높은 데 계단밭은 볕도 바람도 좋아요. 붉은 열매가 익으면 손으로 하나씩 따요.')

    # ---- 바위 언덕 ----
    m.rocks(19, 0, 24, 2)
    m.rocks(31, 0, 35, 2)
    m.rocks(14, 10, 19, 18)
    m.rocks(31, 14, 34, 22)
    m.rocks(37, 18, 45, 23)
    m.rocks(0, 27, 7, 30)
    m.rocks(31, 26, 46, 30)
    for x, y in [(5, 22), (8, 25), (23, 15), (33, 6), (44, 9), (3, 18)]:
        m.maybe('drawn_boulderSmall', x, y, foot=1)
    for x, y in [(3, 22), (25, 24), (40, 15), (4, 12), (34, 4), (12, 25), (17, 2)]:
        m.maybe(['drawn_acaciaRound', 'drawn_olive'][x % 2], x, y)
    for x, y in [(7, 19), (18, 22), (24, 7), (31, 23), (36, 24), (9, 26)]:
        m.maybe('drawn_sage', x, y, foot=1)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, W - 1, 0); A.block_rect(m, 0, H - 1, W - 1, H - 1)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, H - 1)
    m.border(None, 1, WEST_Y + 6, 2, H - 3, kind='rock')
    m.border(None, W - 3, EAST_Y + 6, W - 2, H - 3, kind='rock')

    m.asign(4, WEST_Y + 4, '공방길\n동쪽 고개를 넘으면 타이즈입니다.\n남쪽 바위 틈과 북동 언덕에 샛길이 있어요.')
    m.asign(42, EAST_Y + 4, '← 자비드 / 타이즈 →\n염색 공방의 마을')
    m.texture(0.04)
    m.finish_arabia()
    return m
