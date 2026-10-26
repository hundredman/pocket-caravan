"""중앙 대륙 — 남쪽 구역 (아라비아 담당, 2026-10-26 회의).

중앙 대륙(가운데 네모 약 10만 칸)을 대각선 X로 4등분한 아래쪽 삼각형. 회의 그림 기준 남쪽 = 아라비아.
- 씬 크기 320×160 (제안 틀: 중앙 320×320). 걸을 수 있는 땅은 삼각형 안쪽 약 2만 5천 칸.
  밑변(남쪽 끝, y=159) = 사나 북문과 맞닿는 경계, 꼭짓점(북쪽 끝, y=0) = 중앙 대륙 한가운데.
  삼각형 바깥은 나중에 베네치아·고려 구역이 붙을 자리라 막아 두고 경계에 나무·바위를 심는다.
- 기후 그라데이션: 사나 쪽 사막 → 건초지(스텝) → 한가운데 초원(회의: 초원). 경계는 얼룩지게 섞인다.
- 길: 사나 북문에서 오는 큰 대상로가 굽이굽이 꼭짓점까지. 장터 마을에서 양쪽 대각선(이웃 구역)으로 샛길.
- 장터 마을(오아시스 장터): 오일장처럼 장이 서는 곳. 장터·게시판 데이터와 장날은 다음 단계 (지금은 배치만).
- 탐험: 사막 오아시스, 와디(마른 강)와 모래 위 배(우기에 강이 생기는 설정), 대상 야영지, 목동 천막,
  초원 개울과 판자 다리, 게르 마을, 한가운데 돌무더기 서낭(어워).
지명·배치·NPC는 제안(1안)이다. 합칠 때 팀 틀에 맞춰 옮기거나 다듬는다.
"""
import math
import zlib
import arabia_art as A

W, H = 320, 160
GATE_X = 158                    # 남쪽 출구 (사나 북문) x 158~161
MARGIN = 2                      # 대각선 경계 안쪽으로 막는 폭


def inside(x, y, margin=0):
    """삼각형 안 (꼭짓점 (159.5, 0), 밑변 y=159)."""
    return 0 <= y < H and (159 - y + margin) <= x <= (160 + y - margin)


def box_inside(x, y, w, h, margin=MARGIN + 1):
    return all(inside(xx, yy, margin) for yy in (y, y + h - 1) for xx in (x, x + w - 1))


def _h(*a):
    return (zlib.crc32(repr(a).encode()) & 0xFFFFFF) / float(0xFFFFFF)


def vnoise(x, y, scale, seed):
    """부드러운 값 잡음 0~1."""
    gx, gy = x / scale, y / scale
    x0, y0 = int(math.floor(gx)), int(math.floor(gy))
    fx, fy = gx - x0, gy - y0
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = _h(seed, x0, y0), _h(seed, x0 + 1, y0)
    c, d = _h(seed, x0, y0 + 1), _h(seed, x0 + 1, y0 + 1)
    return (a * (1 - sx) + b * sx) * (1 - sy) + (c * (1 - sx) + d * sx) * sy


def climate(x, y):
    """0 = 한가운데(초원) … 1 = 사나 쪽(사막)."""
    return y / 159 + 0.16 * (vnoise(x, y, 16, 'c1') - 0.5) + 0.07 * (vnoise(x, y, 6, 'c2') - 0.5)


def zone_of(t):
    return 'sand' if t > 0.70 else 'dry' if t > 0.36 else 'grass'


def build(L):
    m = A.ArabiaMap('arabia_central', '중앙 대륙 남부', W, H, seed=1301, theme='town')
    m.hours = 4
    m.soft_under = True          # 사막·건초지·초원 경계가 서로 자연스럽게 겹치도록

    def ok(x, y, w=1, h=1):
        return box_inside(x, y, w, h)

    def put_in(name, x, y, **kw):
        cw, ch = A.size(A.alias(name, x, y))
        return ok(x, y, cw, ch) and m.maybe(name, x, y, **kw)

    # ---- 바닥: 기후 그라데이션 ----
    for y in range(H):
        for x in range(W):
            z = zone_of(climate(x, y))
            if z != 'sand':
                m.paint(z, x, y, x, y, reserve=False)
    # 사막 모래 언덕
    for (cx, cy, rx, ry) in [(92, 150, 15, 4.2), (236, 151, 17, 4.2), (125, 132, 9, 3), (212, 138, 10, 3.2)]:
        m.ellipse('dune', cx, cy, rx, ry, reserve=False)

    # ---- 와디 (마른 강바닥, 사막을 동서로) ----
    wadi = {}
    for x in range(W):
        yc = 124 + int(round(3.2 * math.sin(x / 13.0) + 1.6 * math.sin(x / 5.3 + 2)))
        wadi[x] = yc
        for yy in range(yc, yc + 3):
            if inside(x, yy):
                m.paint('gravel', x, yy, x, yy, reserve=False)

    # ---- 초원 개울 (동서로 흐름, 큰길과 만나는 곳은 판자 다리) ----
    stream, prev = {}, None
    for x in range(W):
        raw = 31 + 2.2 * math.sin(x / 9.0) + 1.2 * math.sin(x / 4.1 + 1)
        k = min(1.0, max(0.0, (abs(x - 171.5) - 8) / 6))      # 다리 근처는 곧게
        yc = int(round(31 * (1 - k) + raw * k))
        stream[x] = yc
        lo, hi = (min(yc, prev), max(yc, prev)) if prev is not None else (yc, yc)
        for yy in range(lo, hi + 2):
            if inside(x, yy, 1):
                m.paint('water', x, yy, x, yy)
        prev = yc

    # ---- 출구·큰 대상로 ----
    m.exit(L, 's_01', GATE_X)
    m.paint('paving', 150, 150, 169, H - 1, grade=3)                    # 사나 북문 밖 광장
    m.paint('zellige', 155, 153, 164, 156)
    road = [(GATE_X, H - 1), (GATE_X, 140), (146, 140), (146, 112), (166, 112), (166, 92)]
    m.paint_path('road', road, width=4, grade=3)
    m.paint_path('road', [(GATE_X, 70), (GATE_X, 42), (170, 42), (170, 22), (GATE_X, 22), (GATE_X, 5)], width=4, grade=3)
    m.bridge(170, 30, 173, 33, 'v', grade=3)

    # ---- 오아시스 장터 마을 (건초지 한가운데) ----
    TOWN = (144, 71, 175, 89)
    m.paint('paving', TOWN[0], TOWN[1], TOWN[2], TOWN[3], grade=3)
    m.paint('zellige', 153, 76, 166, 84)
    m.paint('paving', 166, 90, 169, 92, grade=3)
    # 마을 서쪽 작은 오아시스
    m.ellipse('grass', 127.5, 80.5, 8, 6)
    m.ellipse('water', 127.5, 75.8, 3.6, 2.0)
    # 이웃 구역으로 가는 샛길 (양쪽 대각선까지, 합칠 때 이어 붙인다)
    m.paint_path('road', [(84, 79), (TOWN[0], 79)], width=3, grade=2)
    m.paint_path('road', [(TOWN[2], 79), (236, 79)], width=3, grade=2)

    m.put('drawn_well', 159, 78, foot=1, force=True)
    stalls = ['stall_spice', 'stall_cloth', 'stall_pottery', 'stall_dates', 'stall_fruit', 'stall_bread', 'stall_perfume', 'stall_fruit']
    for i, (x, y) in enumerate([(146, 72), (150, 72), (166, 72), (170, 72), (146, 86), (150, 86), (162, 86), (171, 86)]):
        m.put(stalls[i], x, y, foot=1, force=True)
    goods = ['prop_spiceSack', 'prop_grainsacks', 'prop_pomegranates', 'prop_watermelons', 'prop_carpetstack', 'prop_pepperSack']
    for i, (x, y) in enumerate([(146, 76), (150, 76), (167, 76), (171, 76), (146, 83), (171, 83)]):
        m.maybe(goods[i], x, y, foot=1, force=True)
    for x, y in [(154, 74), (162, 74)]:
        m.maybe('prop_carpet', x, y, layer='deco', force=True)
    for x, y in [(144, 71), (175, 71), (144, 89), (175, 89)]:
        m.maybe('plaza_lamp', x, y - 1, foot=1, force=True)
    for x, y in [(153, 75), (166, 75), (153, 84), (166, 84)]:
        m.maybe('drawn_pottedOrange', x, y - 1, foot=1, force=True)
    for x in (143, 149, 166, 172):
        m.put('building_tent', x, 64, foot=2)
    m.put('building_caravanserai', 181, 74, foot=2)
    m.paint('paving', 176, 77, 180, 78, grade=2)
    m.put('building_arish', 181, 83, foot=2)
    # 길가 집들 (문 앞 → 샛길)
    for x, h in [(97, 'house_mudbrick'), (101, 'house_sand'), (105, 'house_dome'), (109, 'house_white')]:
        m.building(h, x, 76, 79, kind='road')
    for x, h in [(196, 'house_rose'), (200, 'house_palmshade'), (204, 'house_mudbrick'), (208, 'house_awning')]:
        m.building(h, x, 76, 79, kind='road')
    # 가축 시장 (낙타·양 우리)
    for x in range(176, 191, 2):
        m.maybe('prop_fence', x, 93, foot=1, force=True)
    for i, (x, y) in enumerate([(178, 96), (182, 97), (186, 96), (180, 99), (185, 100)]):
        put_in(['drawn_camel', 'drawn_sheep', 'drawn_camelRest', 'drawn_sheep2', 'drawn_sheep'][i], x, y, foot=1)
    m.talker(174, 97, '가축 상인', 'arabia_npc_bedouin_tall', '장날엔 낙타랑 양이 이 우리에 가득 차지. 짐을 많이 실을 낙타를 찾나?')
    for x, y in [(135, 70), (121, 72), (120, 86), (134, 88), (127, 90)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, y, force=True)
    for x, y in [(124, 82), (131, 83)]:
        m.maybe('drawn_oleanderBush', x, y, layer='deco')
    m.maybe('drawn_lily', 127, 75, layer='deco', force=True)
    m.talker(156, 87, '장터지기 카림', 'arabia_npc_merchant_tall',
             '닷새마다 이 오아시스에 장이 서요. 사나 상인도, 이웃 구역 상인도 다 이리로 모이지요.',
             '장날이 언제인지는 게시판에 적어 둘게요. (장날·장터 거래는 다음 단계)')
    m.talker(169, 80, '향신료 상인', 'arabia_woman_perfumer_tall',
             '사막을 건너온 향신료는 초원 사람들한테 인기가 좋아요. 대신 양털을 받아 가지요.')
    m.talker(186, 78, '대상 숙소 주인', 'arabia_woman_grandma_tall',
             '사나에서 여기까지 한나절, 여기서 한가운데 초원까지 또 한나절이에요. 쉬었다 가요.')
    m.asign(140, 90, '오아시스 장터 마을\n닷새마다 장이 서요 · 북: 한가운데 초원 · 남: 사나 북문\n서: 베네치아 쪽 구역 · 동: 고려 쪽 구역')
    m.add('villager', 149, 80, variant=0)
    m.add('villager', 165, 89, variant=2)
    m.add('villager', 156, 67, variant=1)

    # ---- 사나 북문 밖 광장 ----
    for x in (150, 169):
        m.maybe('plaza_lamp', x, 149, foot=1, force=True)
    for x, y in [(147, 151), (171, 151), (147, 155), (171, 155)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2'][y % 2], x, y, force=True)
    m.asign(152, 152, '중앙 대륙 남부\n북쪽으로 가면 건초지와 오아시스 장터, 그 너머 한가운데 초원.\n남쪽: 사나 북문')
    m.talker(165, 151, '북문 밖 낙타몰이', 'arabia_npc_bedouin_tall',
             '여기서부터 사막이 조금씩 풀밭으로 바뀌어. 북쪽으로 갈수록 바람이 시원해지지.')

    # ---- 사막: 오아시스·와디의 모래 배·대상 야영지 ----
    m.ellipse('grass', 110, 141, 9, 5.2)
    m.ellipse('water', 110, 141.5, 4.6, 2.4)
    for x, y in [(99, 137), (119, 136), (101, 144), (118, 145), (108, 133)]:
        m.maybe(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, y, force=True)
    m.paint_path('road', [(110, 147), (110, 150), (150, 150)], width=2, grade=1)
    m.talker(105, 146, '오아시스 물지기', 'arabia_npc_child_tall', '사막 한가운데라도 여긴 물이 마르지 않아요. 낙타들이 길을 먼저 알아요.')
    m.maybe('drawn_dhow', 122, wadi[122] - 1, foot=1, force=True)
    m.talker(126, wadi[126] + 3, '모래 배 주인', 'arabia_npc_sailor_tall',
             '이 와디는 지금은 마른 강이지만, 우기가 오면 물이 차서 배가 다닌다네.',
             '그땐 이 배로 장터까지 짐을 실어 나르지. 지금은 기다리는 중이야.')
    m.put('building_tent', 178, 129, foot=2)
    m.maybe('drawn_camelRest', 183, 131, foot=1)
    m.maybe('drawn_camel', 175, 132, foot=1)
    m.maybe('drawn_crates', 183, 128, foot=1)
    m.maybe('prop_carpet', 177, 133, layer='deco')
    m.talker(180, 133, '쉬어 가는 대상', 'arabia_npc_bedouin_tall', '와디를 건널 땐 돌이 많아 천천히 가게. 짐 떨어뜨리지 말고.')
    for (x0, y0, x1, y1) in [(62, 141, 76, 149), (224, 132, 242, 142), (196, 114, 206, 120), (118, 108, 128, 116)]:
        m.rocks(x0, y0, x1, y1, density=0.55)

    # ---- 건초지: 목동 천막·양 떼·아카시아 ----
    m.put('building_tent', 206, 98, foot=2)
    m.maybe('drawn_camelRest', 212, 100, foot=1)
    for i, (x, y) in enumerate([(200, 104), (203, 106), (206, 103), (209, 107), (213, 105), (198, 108)]):
        put_in('drawn_sheep' if i % 2 else 'drawn_sheep2', x, y, foot=1)
    m.talker(204, 102, '목동 하미드', 'arabia_npc_weaver_tall', '여기 풀은 짧고 질겨서 양들이 좋아해. 북쪽으로 가면 풀이 더 푸르러지지.')
    m.put('building_arish', 100, 100, foot=2)
    m.talker(103, 104, '약초 캐는 할머니', 'arabia_woman_grandma_tall', '사막과 초원 사이에서만 자라는 풀이 있어요. 장터에 가져가면 값을 쳐 줘요.')
    m.maybe('wall_ruin', 80, 108, foot=2)
    m.maybe('wall_ruin', 88, 109, foot=2)
    m.maybe('wall_tower', 85, 104, foot=3)
    m.talker(84, 112, '옛 망루 터의 나그네', 'arabia_npc_scholar_tall', '옛날 대상들이 여기서 사막 쪽을 살폈다지. 지금은 바람만 지나가.')
    m.maybe('nature_figTree', 232, 108)
    m.talker(236, 112, '나무 그늘의 상인', 'arabia_npc_merchant_tall', '이 무화과나무가 사막 끝을 알려 주는 표지야. 여기서부턴 풀이 이어지지.')
    for i, (x, y) in enumerate([(244, 98), (248, 101), (252, 97), (241, 103)]):
        put_in(['drawn_camel', 'drawn_camelRest'][i % 2], x, y, foot=1)
    for y in range(57, 140, 16):
        m.maybe('drawn_milestone', GATE_X - 2 if y > 112 else 164, y, foot=1)

    # ---- 초원: 게르 마을·양 떼·숲·꽃 ----
    for x, y in [(153, 10), (162, 10), (146, 17), (170, 16)]:
        put_in('drawn_yurt' if x % 2 == 0 else 'drawn_yurt2', x, y, foot=2)
    m.maybe('drawn_ovoo', 159, 1, foot=1, force=True)
    m.asign(162, 5, '대륙의 한가운데 (초원)\n네 지역의 구역이 여기서 만납니다.')
    m.talker(156, 19, '게르의 할아버지', 'arabia_npc_scholar_tall',
             '어서 오게, 남쪽 사막에서 왔구먼. 여기가 대륙 한가운데 초원일세.',
             '서쪽엔 운하 도시 사람들, 동쪽엔 반도 사람들이 이 초원으로 장사하러 온다네.')
    for i, (x, y) in enumerate([(140, 26), (143, 28), (147, 25), (182, 26), (186, 28), (179, 29), (132, 40), (136, 43), (190, 44), (194, 41)]):
        put_in('drawn_sheep' if i % 2 else 'drawn_sheep2', x, y, foot=1)
    m.talker(177, 37, '개울가 아이', 'arabia_woman_girl_tall', '개울 물이 차가워요! 양들도 여기서 물을 마셔요.')

    # ---- 나무·바위 흩뿌리기 (기후별) ----
    rng = m.rng
    trees = {'grass': ['drawn_oasisTree', 'drawn_oasisTree2', 'drawn_olive', 'drawn_grove', 'drawn_oliveGrove', 'drawn_almond'],
             'dry': ['drawn_acaciaRound', 'drawn_olive', 'drawn_acaciaRound', 'drawn_sage', 'drawn_boulderSmall'],
             'sand': ['drawn_datePalm2', 'drawn_boulderSmall', 'drawn_sage', 'drawn_boulder']}
    density = {'grass': 0.016, 'dry': 0.011, 'sand': 0.004}
    for y in range(4, H - 4):
        for x in range(W):
            if not inside(x, y, MARGIN + 3):
                continue
            z = m.zone[y][x]
            if z not in trees or m.road[y][x] or m.reserved[y][x]:
                continue
            if _h('tr', x, y) < density[z]:
                put_in(trees[z][int(_h('tk', x, y) * len(trees[z]))], x, y, foot=1)
    # 초원 숲 덩어리 (군데군데)
    for (cx, cy) in [(122, 46), (196, 52), (150, 47), (128, 22), (190, 18), (112, 56), (210, 60), (100, 40), (220, 44)]:
        for k in range(9):
            x = cx + int((_h('gx', cx, k) - 0.5) * 14)
            y = cy + int((_h('gy', cy, k) - 0.5) * 8)
            put_in(['drawn_oasisTree', 'drawn_grove', 'drawn_oasisTree2', 'drawn_olive', 'drawn_almond'][k % 5], x, y, foot=1)
    # 건초지 아카시아 덤불 숲
    for (cx, cy) in [(90, 92), (225, 92), (120, 60), (205, 62), (60, 118), (262, 120), (140, 104), (186, 112)]:
        for k in range(6):
            x = cx + int((_h('ax', cx, k) - 0.5) * 12)
            y = cy + int((_h('ay', cy, k) - 0.5) * 7)
            put_in(['drawn_acaciaRound', 'drawn_olive', 'drawn_acaciaRound', 'drawn_sage'][k % 4], x, y, foot=1)
    # 초원 꽃밭 (무리지어)
    for (cx, cy) in [(140, 36), (182, 50), (150, 14), (200, 34), (118, 32), (166, 56)]:
        for k in range(26):
            x = cx + int((_h('fx', cx, k) - 0.5) * 10)
            y = cy + int((_h('fy', cy, k) - 0.5) * 6)
            if inside(x, y, MARGIN + 1) and m.zone[y][x] == 'grass' and not m.road[y][x]:
                A.place(m, ['drawn_wildflowerY', 'drawn_wildflowerW', 'drawn_wildflowerP', 'drawn_wildflowerB'][(cx + k) % 4], x, y, layer='deco')

    # ---- 대각선 경계: 나무·바위 띠 (바깥은 이웃 구역 자리) ----
    edge_tree = {'grass': ['drawn_oasisTree', 'drawn_olive', 'drawn_oasisTree2'],
                 'dry': ['drawn_acaciaRound', 'drawn_olive', 'drawn_acaciaRound'],
                 'sand': ['drawn_boulder', 'drawn_datePalm2', 'drawn_boulder'],
                 'dune': ['drawn_boulder'], 'gravel': ['drawn_boulder'], 'water': ['drawn_boulder']}
    for y in range(4, H - 1, 2):
        for side in (0, 1):
            x = 159 - y if side == 0 else 160 + y - 1
            z = m.zone[y][min(W - 1, max(0, x))]
            names = edge_tree.get(z, ['drawn_boulder'])
            A.place(m, names[(y // 2) % len(names)], x, y - 1, foot=1, force=True)
    for y in range(H):
        for x in range(W):
            if not inside(x, y, MARGIN):
                A.block_rect(m, x, y, x, y)
    # 이웃 구역 샛길 끝 표지
    m.asign(86, 76, '← 베네치아 쪽 구역\n(중앙 대륙을 합칠 때 이어집니다)')
    m.asign(232, 76, '고려 쪽 구역 →\n(중앙 대륙을 합칠 때 이어집니다)')
    # 밑변 (사나 쪽 끝): 출구만 열고 막는다
    for x in range(W):
        if not (GATE_X <= x < GATE_X + 4):
            A.block_rect(m, x, H - 1, x, H - 1)
    for x in range(12, 148, 4):
        if inside(x, H - 2, MARGIN):
            m.maybe('nature_dune', x, H - 2, foot=1, force=True)
    for x in range(173, 308, 4):
        if inside(x + 2, H - 2, MARGIN):
            m.maybe('nature_dune', x, H - 2, foot=1, force=True)

    # ---- 바닥 결 (기후별 풀·꽃) ----
    deco = {'sand': (0.030, ['drawn_tuft', 'drawn_pebbles', 'drawn_tuft2']),
            'dune': (0.010, ['drawn_pebbles']),
            'dry': (0.060, ['drawn_dryTuft', 'drawn_tuft', 'drawn_dryTuft', 'drawn_tuft2', 'drawn_pebbles']),
            'grass': (0.075, ['drawn_grassTuft', 'drawn_grassTuft2', 'drawn_grassTuft', 'drawn_wildflowerY',
                              'drawn_wildflowerW', 'drawn_wildflowerB', 'drawn_wildflowerP'])}
    for y in range(1, H - 1):
        for x in range(1, W - 1):
            z = m.zone[y][x]
            if z not in deco or not inside(x, y, MARGIN) or m.road[y][x] or m.obj[y][x] or m.high[y][x] or m.deco[y][x]:
                continue
            p, names = deco[z]
            n = _h('dc', x, y)
            if n < p:
                A.place(m, names[int(_h('dn', x, y) * len(names))], x, y, layer='deco')

    m.finish_arabia()
    # 삼각형 바깥: 화면에 보이는 띠(VIEW칸)만 바닥을 남기고, 그 너머는 비운다 (씬 파일 크기 절약).
    # 막힘은 경계 바로 바깥 BLOCK칸 띠면 충분하다 (안쪽 MARGIN칸 + 바깥 BLOCK칸).
    VIEW, BLOCK = 14, 3
    for y in range(H):
        for x in range(W):
            d = max((159 - y) - x, x - (160 + y))          # 바깥으로 몇 칸
            if d <= 0:
                continue
            if d > VIEW:
                m.ground[y][x] = m.deco[y][x] = m.obj[y][x] = m.high[y][x] = None
                m.blocked[y][x] = False
                m.road[y][x] = 0
            elif d > BLOCK:
                m.blocked[y][x] = False
                if not str(m.high[y][x] or '').startswith('arabia:drawn'):
                    m.obj[y][x] = None
    return m
