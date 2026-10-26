"""항구길 — 타이즈에서 바닷가를 따라 제다로 가는 해안길 (48×32).

2026-10-26 회의(스타팅 지역 2만 칸 이내)에 맞춰 60×40 → 48×32로 줄였다. 두 갈래 길은 그대로.
- 언덕길: 북쪽 모래 언덕 위 자갈길. 곧고 단단하다 (옛 아치 터, 이정석).
- 해변길: 남쪽 바닷가 모래길. 조금 돌아가지만 어촌·선착장·등대 망루를 지난다.
바다(남쪽)와 물가는 걸을 수 없고, 어촌의 선착장(판자)은 걸을 수 있다.
"""
import math
import arabia_art as A

W, H = 48, 32
WEST_Y, EAST_Y = 18, 11
HILL_Y = 7          # 언덕길 가로 구간 (7~9)
BEACH_Y = 21        # 해변길 가로 구간 (21~22)


def build(L):
    m = A.ArabiaMap('arabia_portroad', '항구길', W, H, seed=811, theme='town')
    m.hours = 2

    # ---- 바다 (남쪽, 물결 모양 해안선) ----
    for x in range(W):
        top = 26 + int(round(1.4 * math.sin(x / 4.5) + 1.0 * math.sin(x / 11.0 + 1)))
        m.paint('water', x, top, x, H - 1)
    m.paint('dune', 0, 0, W - 1, 2, reserve=False)

    # ---- 길 ----
    m.exit(L, 'w_01', WEST_Y)
    m.exit(L, 'e_01', EAST_Y)
    m.paint('gravel', 0, WEST_Y, 8, WEST_Y + 3, grade=2)
    m.paint_path('gravel', [(6, WEST_Y), (6, HILL_Y), (37, HILL_Y), (37, EAST_Y), (W - 1, EAST_Y)], width=3, grade=3)
    m.paint_path('road', [(6, WEST_Y + 2), (6, BEACH_Y), (40, BEACH_Y), (40, EAST_Y + 3)], width=2, grade=2)
    m.bridge(18, BEACH_Y + 2, 20, H - 3, 'v', grade=1)                     # 선착장

    # ---- 언덕길 풍경 ----
    m.maybe('wall_ruin', 17, 4, foot=2)
    m.maybe('wall_ruin', 26, 4, foot=2)
    for x in (12, 22, 32):
        m.maybe('drawn_milestone', x, HILL_Y + 3, foot=1)
    m.talker(25, HILL_Y + 3, '언덕길의 상인', 'arabia_npc_merchant_tall',
             '언덕길은 곧고 단단해서 짐이 무거울 때 좋아.',
             '해변길은 바람이 좋지만 모래가 발을 잡지. 대신 어촌 사람들이 소식을 잘 알아.')

    # ---- 가운데 모래 언덕밭 (길 없이 가로지를 수 있음 · 난파선 잔해) ----
    m.ellipse('dune', 23, 14.5, 13, 3.4, reserve=False)
    for x, y in [(11, 13), (16, 15), (29, 13), (32, 15)]:
        m.maybe('nature_dune', x, y, foot=1)
    m.maybe('drawn_dhow', 23, 12, foot=1)
    m.talker(20, 14, '조개 줍는 아이', 'arabia_woman_girl_tall',
             '이 배는 오래전에 폭풍에 밀려왔대요. 지금은 갈매기 집이에요.')
    for x, y in [(13, 16), (30, 11), (35, 15)]:
        m.maybe('drawn_datePalm2', x, y - 2)

    # ---- 어촌 (해변길 가운데) ----
    m.put('building_arish', 11, 17, foot=2)
    m.put('building_arish', 22, 17, foot=2)
    m.maybe('drawn_netRack', 15, 18, foot=1)
    m.maybe('drawn_barrel', 20, 18, foot=1)
    m.maybe('drawn_crates', 26, 18, foot=1)
    m.talker(17, 20, '어부', 'arabia_npc_sailor_tall',
             '오늘은 물이 잔잔해. 제다 항구까지 배로 짐을 나르는 사람도 있지.',
             '선착장 끝에 서면 다우 배들이 잘 보여.')
    for x, y in [(22, 27), (33, 28), (6, 28), (42, 27)]:
        m.maybe('drawn_dhow', x, y, layer='deco')

    # ---- 등대 망루 (동쪽 곶) ----
    m.paint('rock', 42, 15, W - 2, 23, reserve=False)
    m.maybe('building_windTower', 43, 16, foot=2)
    m.talker(44, 21, '망루지기', 'arabia_npc_bedouin_tall',
             '밤이면 이 망루에 불을 켜서 배들에게 항구 쪽을 알려 준다네.')

    # ---- 야자·덤불 (해안) ----
    for x in (2, 9, 30, 34, 37):
        m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, BEACH_Y + 2)
    for x, y in [(10, 11), (17, 12), (27, 12), (33, 17), (14, 4), (32, 4)]:
        m.maybe(['drawn_sage', 'drawn_bush', 'drawn_oleanderBush'][x % 3], x, y, foot=1)
    for x, y in [(2, 10), (43, 5), (9, 13)]:
        m.maybe(['drawn_acaciaRound', 'drawn_olive'][x % 2], x, y)
    m.rocks(42, 2, 46, 6)
    m.rocks(1, 3, 4, 7)

    # ---- 가장자리 ----
    A.block_rect(m, 0, 0, W - 1, 0)
    A.block_rect(m, 0, 0, 0, WEST_Y - 1); A.block_rect(m, 0, WEST_Y + 4, 0, H - 1)
    A.block_rect(m, W - 1, 0, W - 1, EAST_Y - 1); A.block_rect(m, W - 1, EAST_Y + 4, W - 1, H - 1)
    m.border(None, 8, 2, 40, 2)

    m.asign(2, WEST_Y - 3, '항구길\n북쪽 언덕길: 곧고 단단함\n남쪽 해변길: 어촌·선착장·망루를 지나 조금 돌아감')
    m.asign(43, EAST_Y - 3, '← 타이즈 / 제다 →\n항구 도시')
    m.texture(0.04)
    m.finish_arabia()
    return m
