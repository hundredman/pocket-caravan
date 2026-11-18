"""한가운데 초원 (arabia_central_steppe) — 세 지역이 만나는 '사방 광장' 배치 (2026-11-18).

요청: 세 지역(북유럽·베네치아·중간권)으로 이어지는 중심 맵답게, 만국 장터 가는 길까지 잘 이어지게.
  - 관문을 방향대로 나눈다: 북유럽 = 위(꼭짓점), 베네치아 = 서쪽 대각선 변, 중간권 = 동쪽 대각선 변.
  - 가운데 '사방 광장' (돌바닥 + 큰 이정표 + 우물 + 야영 게르).
  - 광장에서 남쪽으로 곧게 내려가는 돌 깐 큰길 → 만국 장터 아치 → 장터 북문 대로(장터 x46..49 = 여기 x54..57).
  - 서쪽 포도밭·동쪽 고원은 큰길에서 갈라지는 흙길.
만국 장터와 다른 맵은 건드리지 않는다: 이웃과 맞닿는 남쪽 이음새(출구·도착 자리)는 지금 JSON 그대로 옮겨 쓴다.
다른 지역 맵은 이 맵의 도착 자리를 id(link_*)로 찾으므로 자리만 옮기면 된다.

사용법 (저장소 루트):  python3 Tools/MapGen/arabia_steppe_hub.py [--write]
  --write 없이 돌리면 미리보기만 (ArtReview/Arabia-Steppe-20261118/).
arabia_central.py의 다른 조각 생성과는 따로 돈다 (그 파일의 steppe 부분은 예전 배치 그대로).
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import arabia_art as A          # noqa: E402
import arabia_central as C      # noqa: E402
import arabia_export as E       # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
MID = 'arabia_central_steppe'
MAP = os.path.join(ROOT, 'Assets/PocketCaravan/Maps', MID + '.json')
TILES = os.path.join(ROOT, 'Assets/PocketCaravan/Art/NinjaAdventure/Tiles')
OX = 104                         # 틀 좌표 = 맵 좌표 + 104 (y는 같음)

# 관문 (맵 좌표). 북쪽은 꼭짓점이 좁아 y=7 줄에 둔다 (예전과 같은 자리).
GATE_N = (54, 7, 4, 1)           # x, y, w, h
GATE_W = (37, 20, 1, 3)
GATE_E = (74, 20, 1, 3)
PLAZA = (55.5, 20.5)             # 광장 가운데
SQ = (46, 14, 65, 27)            # 네모 광장 x0, y0, x1, y1 (가운데 x 55.5)
AVE = (54, 57)                   # 큰길 x 범위 (장터 북문 대로와 맞춤)


def F(x):                         # 맵 x → 틀 x
    return x + OX


def stream_y(xf):
    """개울: 예전 모양 그대로, 큰길 다리(틀 x 159.5) 둘레만 곧게."""
    import math
    raw = 31 + 2.2 * math.sin(xf / 9.0) + 1.2 * math.sin(xf / 4.1 + 1)
    k = min(1.0, max(0.0, (abs(xf - 159.5) - 4) / 6))
    return int(round(31 * (1 - k) + raw * k)) if abs(xf - 159.5) < 11 else int(round(raw))


def build():
    P = C.Piece(MID, {})
    m = P.m
    m.hours = 1
    # ---- 바닥: 기후 그라데이션 (예전과 같음)
    for ly in range(m.h):
        for lx in range(m.w):
            z = C.zone_of(lx + OX, ly)
            if z != 'sand':
                m.paint(z, lx, ly, lx, ly, reserve=False)
    # ---- 개울
    water_rows = {}
    for xf in range(OX, OX + m.w):
        yc, prev = stream_y(xf), stream_y(xf - 1)
        for yy in range(min(yc, prev), max(yc, prev) + 2):
            if C.tri_e(xf, yy) >= 1:
                P.cell('water', xf, yy)
                water_rows.setdefault(xf - OX, []).append(yy)
    # ---- 세 관문 길: 곧게 (네모 광장 각 변 가운데로 들어온다)
    P.rect('road', F(54), 7, F(57), 15, grade=3)                                   # 북유럽
    P.rect('road', F(37), 20, F(47), 22, grade=3)                                  # 베네치아
    P.rect('road', F(64), 20, F(74), 22, grade=3)                                  # 중간권
    # ---- 사방 광장: 반듯한 네모 돌바닥 (x 46..65 · y 14..27, 가운데 55.5, 20.5)
    P.rect('paving', F(SQ[0]), SQ[1], F(SQ[2]), SQ[3], grade=3)
    # ---- 큰길에서 갈라지는 흙길: 서쪽 포도밭 · 동쪽 고원 (남쪽 이음새 출구 자리로)
    P.curve('road', [(F(54), 43.5), (F(42), 44.5), (F(28), 47), (F(15), 50.5), (F(5.5), 55)], 3, 2)
    P.curve('road', [(F(57), 43.5), (F(69), 44.5), (F(83), 47), (F(96), 50.5), (F(105.5), 55)], 3, 2)
    # ---- 돌 깐 큰길 → 만국 장터 (가장자리까지)
    P.rect('paving', F(AVE[0]), 25, F(AVE[1]), m.h - 1, grade=3)
    rows = sorted({y for x in range(AVE[0], AVE[1] + 1) for y in water_rows.get(x, [])})
    b0, b1 = rows[0] - 1, rows[-1] + 1
    P.bridge(F(AVE[0]), b0, F(AVE[1]), b1, 'v', 3)
    wall = C.walls(P)
    dress(P)
    C.dress_walls(P, wall)
    # 관문 너머로 길이 이어지는 것처럼: 숲 띠 안으로 몇 칸 길을 더 그리고 그 위 나무를 치운다 (막힘은 그대로)
    strips = [(30, 36, GATE_W[1], GATE_W[1] + 2), (75, 81, GATE_E[1], GATE_E[1] + 2), (GATE_N[0], GATE_N[0] + 3, 1, GATE_N[1] - 1)]
    cells = set()
    for x0, x1, y0, y1 in strips:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                m.paint('road', x, y, x, y, reserve=False)
                cells.add((x, y))
    near = {(x + dx, y + dy) for (x, y) in cells for dx in (-1, 0, 1) for dy in (-1, 0, 1)}
    for name, x, y in list(m.stamps):
        if not name.startswith('drawn_') or name in ('drawn_runestone',):
            continue
        cw, ch, parts = A.cells(name)
        if any((x + cx, y + cy) in near for (cx, cy) in parts):
            A.unplace(m, name, x, y)
    return P, wall, (b0, b1)


def tryput(P, name, x, y, tries=((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (1, 1), (-1, 1)), **kw):
    for dx, dy in tries:
        if P.put(name, F(x + dx), y + dy, **kw):
            return (x + dx, y + dy)
    P.m.warnings.append(f'배치 못함 {name} @({x},{y})')
    return None


def dress(P):
    m = P.m
    # 광장 가운데: 큰 이정표 (네 방향)
    P.put('drawn_well', F(55), 18, force=True)                                   # 광장 가운데 우물 (모두가 들르는 자리)
    P.sign(F(51), 19, '사방 광장 · 대륙의 한가운데\n세 지역의 길이 여기서 만납니다.\n'
                      '↑ 북유럽 · ← 베네치아 · → 중간권(고려 쪽)\n↓ 돌 깐 큰길 → 만국 장터')
    # 야영 게르 (광장 북쪽 양옆)
    tryput(P, 'drawn_yurt', 43, 23, tries=((0, 0), (0, 1), (-1, 0)))
    tryput(P, 'drawn_yurt2', 66, 23, tries=((0, 0), (0, 1), (1, 0)))
    tryput(P, 'drawn_pavilion3', 36, 25, tries=((0, 0), (1, 0), (0, 1), (-1, 1)))   # 여행자 쉼터 천막
    # 광장 위: 네 귀퉁이 화분 · 양쪽 벤치 (대칭으로)
    for name, x, y in [('drawn_pottedOrange', 47, 15), ('drawn_pottedOrange', 64, 15), ('drawn_pottedOrange', 47, 25), ('drawn_pottedOrange', 64, 25),
                       ('drawn_benchWood', 49, 25), ('drawn_benchWood', 61, 25), ('drawn_benchWood', 49, 16), ('drawn_benchWood', 61, 16)]:
        P.put(name, F(x), y, force=True)
    # 관문 표식
    tryput(P, 'drawn_runestone', 52, 8, tries=((0, 0), (0, 1), (-1, 1)))     # 북유럽: 룬돌 한 쌍
    tryput(P, 'drawn_runestone', 59, 8, tries=((0, 0), (0, 1), (1, 1)))
    tryput(P, 'drawn_guildFlagScale', 41, 15, tries=((0, 0), (1, 0), (1, 1), (2, 0)))   # 베네치아: 상인 깃발 한 쌍
    tryput(P, 'drawn_guildFlagScale', 40, 23, tries=((0, 0), (1, 0), (0, 1)))
    tryput(P, 'drawn_guildFlagWind', 68, 15, tries=((0, 0), (-1, 0), (-1, 1), (-2, 0)))  # 중간권: 바람 깃발 + 등불
    tryput(P, 'plaza_lamp', 72, 23, tries=((0, 0), (-1, 0)))
    # 광장 둘레: 그늘 나무 · 쉬는 낙타 · 짐수레 · 항아리 (사람이 오가는 쉼터 느낌)
    tryput(P, 'drawn_oasisTree', 67, 16, tries=((0, 0), (-1, 0), (0, -1)))
    tryput(P, 'drawn_oasisTree2', 44, 28)
    tryput(P, 'drawn_acaciaRound', 66, 28)
    tryput(P, 'drawn_crates', 40, 27)
    tryput(P, 'drawn_jar', 39, 25)
    tryput(P, 'drawn_jarSmall', 42, 27)
    tryput(P, 'drawn_barrel', 62, 13)
    # 큰길 가로등 (양쪽, 개울·갈림길·이음새 줄은 비운다)
    for y in (26, 38, 48):
        for x in (AVE[0] - 1, AVE[1] + 1):
            P.put('plaza_lamp', F(x), y)
    # 만국 장터 아치: 큰길 끝 (가운데 두 칸이 열린 문)
    gate_y = 50
    if P.put('plaza_gate', F(AVE[0]), gate_y, foot=1, force=True):
        for x in (AVE[0] + 1, AVE[0] + 2):
            for y in range(gate_y, gate_y + 4):
                m.blocked[y][x] = False
    P.sign(F(AVE[1] + 2), 46, '↓ 만국 장터 (돌 깐 큰길을 따라)\n← 서쪽 포도밭 언덕 · → 동쪽 소나무 고원')
    # 풀밭 숲 덩어리 (길 사이를 나눠 길이 잘 보이게)
    for name, x, y in [('drawn_grove', 22, 36), ('drawn_oliveGrove', 30, 39), ('drawn_grove', 38, 35),
                       ('drawn_almond', 46, 37), ('drawn_oliveGrove', 18, 41),
                       ('drawn_grove', 72, 36), ('drawn_oliveGrove', 80, 39),
                       ('drawn_almond', 64, 37), ('drawn_oliveGrove', 92, 41), ('drawn_grove', 84, 34),
                       ('drawn_oasisTree', 34, 27), ('drawn_oasisTree', 76, 27), ('drawn_olive', 46, 28), ('drawn_olive', 66, 28),
                       ('drawn_grove', 30, 50), ('drawn_grove', 80, 50), ('drawn_almond', 46, 51), ('drawn_almond', 64, 51)]:
        tryput(P, name, x, y)
    # 양 (풀밭)
    for name, x, y in [('drawn_sheep', 26, 31), ('drawn_sheep2', 29, 32), ('drawn_sheep', 84, 31), ('drawn_sheep2', 81, 33),
                       ('drawn_sheep', 40, 47), ('drawn_sheep2', 70, 48)]:
        tryput(P, name, x, y)
    # 사람
    P.talker(F(45), 17, '게르의 할아버지', 'arabia_npc_scholar_tall',
             '어서 오게. 여기가 대륙 한가운데, 사방 광장일세.',
             '위로 가면 북유럽, 서쪽 깃발 너머는 베네치아, 동쪽 등불 너머는 중간권이지.')
    P.talker(F(53), 9, '북쪽 모피 상인', 'north_p_trader_tall',
             '룬돌 사이로 올라가면 북유럽 길이오. 털가죽을 팔러 장터까지 내려가는 중이라오.')
    P.talker(F(44), 19, '베네치아 상인', 'venice_merchant_tall',
             '노란 깃발 쪽이 우리 운하 도시로 가는 길이랍니다. 유리 구슬 구경하실래요?')
    P.talker(F(66), 19, '등불지기', 'trader_tall',
             '동쪽 등불을 따라가면 중간권 교역로요. 밤에도 이 등불은 꺼지지 않소.')
    P.talker(F(62), 35, '개울가 아이', 'arabia_woman_girl_tall',
             '개울 물이 차가워요! 큰 돌다리를 건너면 만국 장터예요.')
    P.villager(F(52), 40, variant=1)


def finish(P, bridge_rows):
    m = P.m
    # 바닥 결 · 성긴 나무 (arabia_central.build_piece와 같은 규칙)
    deco = {'dry': (0.055, ['drawn_dryTuft', 'drawn_tuft', 'drawn_dryTuft', 'drawn_tuft2', 'drawn_pebbles']),
            'grass': (0.070, ['drawn_grassTuft', 'drawn_grassTuft2', 'drawn_grassTuft', 'drawn_wildflowerY',
                              'drawn_wildflowerW', 'drawn_wildflowerB', 'drawn_wildflowerP'])}
    for ly in range(1, m.h - 1):
        for lx in range(1, m.w - 1):
            x, y = lx + OX, ly
            z = m.zone[ly][lx]
            if z not in deco or C.tri_e(x, y) < 3 or m.road[ly][lx] or m.obj[ly][lx] or m.high[ly][lx] or m.deco[ly][lx] or m.blocked[ly][lx]:
                continue
            p, names = deco[z]
            if C._h('dc', x, y) < p:
                A.place(m, names[int(C._h('dn', x, y) * len(names))], lx, ly, layer='deco')
    m.finish_arabia()
    BLOCK = 3
    for ly in range(m.h):
        for lx in range(m.w):
            e = C.tri_e(lx + OX, ly)
            if e >= 2:
                continue
            if e >= -BLOCK:
                m.blocked[ly][lx] = True
                continue
            if -e > C.VIEW:
                m.ground[ly][lx] = m.deco[ly][lx] = m.obj[ly][lx] = m.high[ly][lx] = None
                m.road[ly][lx] = 0
            m.blocked[ly][lx] = False
    # 북쪽 관문 위(꼭짓점)는 막는다: 관문 줄에서 바로 나가게
    for ly in range(0, GATE_N[1]):
        for lx in range(m.w):
            m.blocked[ly][lx] = True


def merge(m, old):
    d = E.to_json(m)
    keep = [o for o in old['objects'] if o['kind'] in ('exit', 'spawn') and '_seam_' in (o.get('spawn', '') + o.get('id', ''))]
    links = {o['target']: o for o in old['objects'] if o['kind'] == 'exit' and o['target'] in ('north_c_crossing', 'venice_c_gate', 'transit_crossroads')}
    spawns = {o['id']: o for o in old['objects'] if o['kind'] == 'spawn' and o['id'].startswith('link_')}
    new = []
    for target, (x, y, w, h), sid, (sx, sy) in [
            ('north_c_crossing', GATE_N, 'link_north_c_crossing', (GATE_N[0], GATE_N[1] + 1)),
            ('venice_c_gate', GATE_W, 'link_venice_c_gate', (GATE_W[0] + 1, GATE_W[1])),
            ('transit_crossroads', GATE_E, 'link_transit_crossroads', (GATE_E[0] - 1, GATE_E[1]))]:
        ex = dict(links[target]); ex.update(x=x, y=y, w=w, h=h); new.append(ex)
        sp = dict(spawns[sid]); sp.update(x=sx, y=sy, w=w, h=h); new.append(sp)
    objs = [o for o in d['objects'] if o['kind'] not in ('exit', 'spawn')]
    d['objects'] = objs + keep + new
    # 출구·도착 자리는 걸을 수 있게, 관문 바깥 한 칸은 막아 관문으로만 나가게
    W = d['w']
    for o in keep + new:
        for yy in range(o['y'], o['y'] + o['h']):
            for xx in range(o['x'], o['x'] + o['w']):
                d['blocked'][yy * W + xx] = 0
    for (x, y, w, h), dx in ((GATE_W, -1), (GATE_E, 1)):
        for yy in range(y - 1, y + h + 1):
            d['blocked'][yy * W + x + dx] = 1
    return d


def main():
    write = '--write' in sys.argv
    A.rescan()
    old = json.load(open(MAP, encoding='utf-8'))
    P, wall, brows = build()
    finish(P, brows)
    d = merge(P.m, old)
    errs = []
    E.walk_check(d, errs)
    for w in P.m.warnings:
        print('WARN', w)
    for e in errs:
        print('ERROR', e)
    # 새 타일 (없는 것만 쓴다: 이미 있는 그림은 건드리지 않음)
    new_tiles = []
    for k in d['palette']:
        if k and not os.path.exists(os.path.join(TILES, k + '.png')):
            im = A.USED.get(k)
            if im is None:
                errs.append('그림 없음 ' + k)
            else:
                new_tiles.append((k, im))
    out = os.path.join(ROOT, 'ArtReview/Arabia-Steppe-20261118')
    os.makedirs(out, exist_ok=True)
    json.dump(d, open(os.path.join(out, 'preview.json'), 'w', encoding='utf-8'), ensure_ascii=False, separators=(",", ":"))
    for k, im in new_tiles:
        im.save(os.path.join(out, 'tile_' + k + '.png'))
    print(f'objects {len(d["objects"])}  new tiles {len(new_tiles)}  errors {len(errs)}  bridge rows {brows}')
    if write and not errs:
        for k, im in new_tiles:
            im.save(os.path.join(TILES, k + '.png'))
        open(MAP, 'w', encoding='utf-8').write(json.dumps(d, ensure_ascii=False, separators=(",", ":")))
        print('written', MAP)
    return 1 if errs else 0


if __name__ == '__main__':
    sys.exit(main())
