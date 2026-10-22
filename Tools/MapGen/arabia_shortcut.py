"""험한 지름길 — 제다에서 사나로 곧장 오르는 바위 협곡 (60×36).

- 거리는 짧지만 좁고 거친 자갈길(노면 1)이라 짐이 무거우면 느리다.
- 협곡 벽(바위)이 길을 좁히고, 굽이마다 쉬어 갈 바위 그늘이 있다.
- 탐험: 북쪽 바위 틈의 은자 동굴 터(은자), 남쪽 마른 우물과 낙타 뼈 대신 버려진 짐수레.
아직 낙하·도적 사건은 넣지 않는다 (공용 위험 규칙 확정 전).
"""
import arabia_art as A

W, H = 60, 36
WEST_Y, EAST_Y = 20, 14


def build(L):
    m = A.ArabiaMap('arabia_shortcut', '험한 지름길', W, H, seed=1001, theme='town')
    m.hours = 1

    m.paint('rock', 0, 0, W - 1, H - 1, reserve=False)
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    # 좁은 협곡길 (폭 2, 지그재그)
    m.paint('gravel', 0, WEST_Y, 3, WEST_Y + 3, grade=1)
    m.paint_path('gravel', [(2, WEST_Y + 1), (14, WEST_Y + 1), (14, 26), (24, 26), (24, 10), (36, 10), (36, 22),
                            (46, 22), (46, EAST_Y + 1), (59, EAST_Y + 1)], width=2, grade=1)
    m.paint('gravel', W - 4, EAST_Y, W - 1, EAST_Y + 3, grade=1)

    m.asign(4, WEST_Y + 4, '험한 지름길 (사나)\n짧지만 좁고 거친 돌길입니다.\n짐이 무거우면 북쪽 큰길이 나을 수 있어요.')
    m.asign(54, EAST_Y + 3, '← 제다 / 사나 성문 →')
    m.paint_path('road', [(30, 10), (30, 8)], width=1, grade=1)       # 은자 동굴 터로 가는 틈길
    m.paint('gravel', 28, 8, 30, 8, grade=1)
    # 굽이마다 쉼터
    m.maybe('drawn_acaciaRound', 15, 22)
    m.maybe('drawn_acaciaRound', 37, 18)
    m.maybe('drawn_olive', 25, 6)
    # 은자 동굴 터 (북쪽 바위 틈)
    m.talker(29, 8, '바위 틈의 은자', 'arabia_npc_scholar_tall',
             '지름길은 짧아도 돌이 많아 발이 무겁지. 짐이 많으면 큰길로 돌아가는 게 나을 때도 있어.',
             '여기서 동쪽으로 조금만 더 가면 사나 성문이 보인다네.')
    # 버려진 짐수레 (남쪽 굽이)
    m.maybe('drawn_cart', 18, 24, foot=1)
    m.maybe('drawn_barrel', 21, 24, foot=1)
    m.talker(40, 23, '쉬고 있는 짐꾼', 'arabia_npc_sailor_tall',
             '어휴, 수레를 끌고 오기엔 너무 좁고 울퉁불퉁해. 다음엔 큰길로 가야겠어.')

    for x, y in [(8, 18), (20, 8), (34, 26), (44, 16), (52, 24), (12, 30), (56, 8)]:
        m.maybe(['drawn_sage', 'drawn_acaciaRound', 'drawn_olive'][x % 3], x, y)
    # 협곡 벽: 길 밖을 바위로 채운다 (길과 1칸 띄움은 rocks가 길 칸을 피함)
    for (x0, y0, x1, y1) in [(1, 1, 22, 17), (1, 24, 11, 34), (16, 28, 34, 34), (17, 13, 21, 24),
                             (27, 13, 33, 24), (26, 1, 58, 7), (38, 9, 58, 12), (38, 25, 58, 34), (49, 17, 58, 21)]:
        m.rocks(x0, y0, x1, y1, density=0.7)

    A.block_rect(m, 0, 0, W - 1, 0); A.block_rect(m, 0, H - 1, W - 1, H - 1)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, H - 1)
    m.texture(0.03)
    m.finish_arabia()
    return m
