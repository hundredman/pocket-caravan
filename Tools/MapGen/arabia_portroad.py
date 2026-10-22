"""항구길 — 타이즈에서 바닷가를 따라 제다로 가는 해안길 (60×40).

두 갈래 (탐험·비교)
- 언덕길: 북쪽 모래 언덕 위 자갈길. 곧고 단단하다 (옛 아치 터, 이정석).
- 해변길: 남쪽 바닷가 모래길. 조금 돌아가지만 어촌·배·등대 망루를 지난다.
바다(남쪽)와 물가는 걸을 수 없고, 어촌의 선착장(판자)은 걸을 수 있다.
"""
import arabia_art as A

W, H = 60, 40
WEST_Y, EAST_Y = 22, 14


def build(L):
    m = A.ArabiaMap('arabia_portroad', '항구길', W, H, seed=811, theme='town')
    m.hours = 2

    # ---- 바다 (남쪽, 물결 모양 해안선) ----
    import math
    for x in range(W):
        top = 31 + int(round(1.6 * math.sin(x / 4.5) + 1.2 * math.sin(x / 11.0 + 1)))
        m.paint('water', x, top, x, H - 1)
    m.paint('dune', 0, 0, W - 1, 3, reserve=False)

    # ---- 길 ----
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    m.paint('gravel', 0, WEST_Y, 10, WEST_Y + 3, grade=2)
    # 언덕길 (북쪽, 곧음)
    m.paint_path('gravel', [(8, WEST_Y), (8, 9), (46, 9), (46, EAST_Y), (59, EAST_Y)], width=3, grade=3)
    # 해변길 (남쪽, 돌아감)
    m.paint_path('road', [(8, WEST_Y + 3), (8, 27), (50, 27), (50, EAST_Y + 3)], width=2, grade=2)
    # 선착장 (어촌 앞 판자)
    m.bridge(22, 29, 24, 37, 'v', grade=1)

    # ---- 언덕길 풍경 ----
    m.maybe('wall_ruin', 24, 6, foot=2)
    m.maybe('wall_ruin', 33, 6, foot=2)
    for x in (14, 26, 38):
        m.maybe('drawn_milestone', x, 12, foot=1)
    m.talker(30, 12, '언덕길의 상인', 'arabia_npc_merchant_tall',
             '언덕길은 곧고 단단해서 짐이 무거울 때 좋아.',
             '해변길은 바람이 좋지만 모래가 발을 잡지. 대신 어촌 사람들이 소식을 잘 알아.')

    # ---- 어촌 (해변길 가운데) ----
    m.put('building_arish', 16, 23, foot=2)
    m.put('building_arish', 27, 23, foot=2)
    m.maybe('drawn_netRack', 20, 24, foot=1)
    m.maybe('drawn_barrel', 25, 25, foot=1)
    m.maybe('drawn_crates', 31, 24, foot=1)
    m.talker(23, 28, '어부', 'arabia_npc_sailor_tall',
             '오늘은 물이 잔잔해. 제다 항구까지 배로 짐을 나르는 사람도 있지.',
             '선착장 끝에 서면 다우 배들이 잘 보여.')
    m.maybe('drawn_dhow', 26, 33, layer='deco')
    m.maybe('drawn_dhow', 40, 34, layer='deco')
    m.maybe('drawn_dhow', 8, 35, layer='deco')

    # ---- 가운데 모래 언덕밭 (길 없이 가로지를 수 있음 · 난파선 잔해) ----
    m.ellipse('dune', 28, 18, 16, 5.2, reserve=False)
    for x, y in [(15, 15), (21, 19), (34, 15), (39, 20), (27, 21)]:
        m.maybe('nature_dune', x, y, foot=1)
    m.maybe('drawn_dhow', 27, 14, foot=1)
    m.talker(26, 19, '조개 줍는 아이', 'arabia_woman_girl_tall',
             '이 배는 오래전에 폭풍에 밀려왔대요. 지금은 갈매기 집이에요.')
    for x, y in [(17, 21), (37, 17), (42, 22)]:
        m.maybe('drawn_datePalm2', x, y - 2)

    # ---- 등대 망루 (동쪽 곶) ----
    m.paint('rock', 52, 22, 58, 30, reserve=False)
    m.maybe('building_windTower', 54, 23, foot=2)
    m.talker(53, 28, '망루지기', 'arabia_npc_bedouin_tall',
             '밤이면 이 망루에 불을 켜서 배들에게 항구 쪽을 알려 준다네.')

    # ---- 야자·덤불 (해안) ----
    for x in range(4, 56, 5):
        if not 15 <= x <= 33:
            m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, 28)
    for x, y in [(12, 16), (20, 17), (36, 18), (42, 20), (28, 16), (16, 5), (40, 4)]:
        m.maybe(['drawn_sage', 'drawn_bush', 'drawn_oleanderBush'][x % 3], x, y, foot=1)
    for x, y in [(4, 12), (54, 6), (20, 13), (40, 15)]:
        m.maybe(['drawn_acaciaRound', 'drawn_olive'][x % 2], x, y)
    m.rocks(53, 4, 58, 8)
    m.rocks(1, 1, 6, 6)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, W - 1, 0)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, H - 1)
    m.border(None, 8, 3, 52, 3)

    m.asign(3, WEST_Y - 2, '항구길\n북쪽 언덕길: 곧고 단단함\n남쪽 해변길: 어촌·선착장·망루를 지나 조금 돌아감')
    m.asign(55, EAST_Y - 3, '← 타이즈 / 제다 →\n항구 도시')
    m.texture(0.04)
    m.finish_arabia()
    return m
