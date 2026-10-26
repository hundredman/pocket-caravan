"""험한 지름길 — 제다 동문에서 사나 남문으로 곧장 오르는 바위 협곡 (48×28).

2026-10-26: 사나 북문이 중앙 대륙 관문이 되면서 지름길은 사나 남문으로 올라간다. 60×36 → 48×28.
- 거리는 짧지만 좁고 거친 자갈길(노면 1)이라 짐이 무거우면 느리다.
- 협곡 벽(바위)이 길을 좁히고, 굽이마다 쉬어 갈 바위 그늘이 있다.
- 탐험: 바위 틈의 은자 동굴 터(은자), 남쪽 굽이의 버려진 짐수레.
아직 낙하·도적 사건은 넣지 않는다 (공용 위험 규칙 확정 전).
"""
import arabia_art as A

W, H = 48, 28
WEST_Y, NORTH_X = 16, 38


def build(L):
    m = A.ArabiaMap('arabia_shortcut', '험한 지름길', W, H, seed=1001, theme='town')
    m.hours = 1

    m.paint('rock', 0, 0, W - 1, H - 1, reserve=False)
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'n_01', NORTH_X)
    # 좁은 협곡길 (폭 2, 지그재그) — 서쪽 입구에서 북쪽 성문으로
    m.paint('gravel', 0, WEST_Y, 3, WEST_Y + 3, grade=1)
    m.paint_path('gravel', [(2, WEST_Y + 1), (10, WEST_Y + 1), (10, 22), (20, 22), (20, 8), (30, 8), (30, 17),
                            (39, 17), (39, 0)], width=2, grade=1)
    m.paint('gravel', NORTH_X, 0, NORTH_X + 3, 3, grade=1)

    m.asign(4, WEST_Y + 4, '험한 지름길 (사나 남문)\n짧지만 좁고 거친 돌길입니다.\n짐이 무거우면 북쪽 큰길이 나을 수 있어요.')
    m.asign(NORTH_X + 4, 4, '사나 남문 ↑\n← 제다')
    # 은자 동굴 터 (북쪽 바위 틈)
    m.paint('gravel', 24, 5, 26, 7, grade=1)
    m.talker(25, 5, '바위 틈의 은자', 'arabia_npc_scholar_tall',
             '지름길은 짧아도 돌이 많아 발이 무겁지. 짐이 많으면 큰길로 돌아가는 게 나을 때도 있어.',
             '여기서 동쪽 굽이를 돌아 북쪽으로 오르면 사나 남문이 보인다네.')
    # 버려진 짐수레 (남쪽 굽이)
    m.maybe('drawn_cart', 13, 20, foot=1)
    m.maybe('drawn_barrel', 16, 20, foot=1)
    m.talker(34, 19, '쉬고 있는 짐꾼', 'arabia_npc_sailor_tall',
             '어휴, 수레를 끌고 오기엔 너무 좁고 울퉁불퉁해. 다음엔 큰길로 가야겠어.')
    # 굽이마다 그늘
    m.maybe('drawn_acaciaRound', 11, 18)
    m.maybe('drawn_acaciaRound', 31, 14)
    m.maybe('drawn_olive', 22, 4)
    for x, y in [(6, 13), (16, 6), (27, 21), (35, 12), (44, 20), (8, 25), (44, 8)]:
        m.maybe(['drawn_sage', 'drawn_acaciaRound', 'drawn_olive'][x % 3], x, y)
    # 협곡 벽: 길 밖을 바위로 채운다
    for (x0, y0, x1, y1) in [(1, 1, 18, 14), (1, 20, 8, 26), (12, 24, 28, 26), (13, 10, 18, 20),
                             (22, 10, 28, 20), (27, 1, 36, 6), (32, 8, 37, 15), (32, 20, 46, 26), (42, 6, 46, 18)]:
        m.rocks(x0, y0, x1, y1, density=0.7)

    A.block_rect(m, 0, 0, NORTH_X - 1, 0); A.block_rect(m, NORTH_X + 4, 0, W - 1, 0)
    A.block_rect(m, 0, H - 1, W - 1, H - 1)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, H - 1)
    m.texture(0.03)
    m.finish_arabia()
    return m
