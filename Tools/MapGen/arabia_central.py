"""중앙 대륙 — 남쪽 구역 (아라비아 담당). 여러 씬으로 이어지는 탐험형 구역.

회의(2026-10-26): 중앙 대륙(가운데 네모 약 10만 칸)을 대각선 X로 4등분, 아래쪽 삼각형 = 아라비아.
기후는 사나 쪽 사막 → 건초지 → 한가운데 초원. 중앙 대륙은 나중에 네 구역을 한 틀에 붙여 합친다.

요청(2026-10-26 저녁): 삼각형 면적은 유지하되 삼각형이 보이지 않게, 여러 씬이 자연스럽게 이어지는 탐험형 세계,
갈림길·숨은 길·지름길·랜드마크, 중간 지점에 여러 문화가 섞인 큰 장터 (아라비아풍 고급 상점은 2~3곳만).

설계
- 같은 틀 좌표(320×160, 밑변 y=159 = 사나 쪽, 꼭짓점 (159.5, 0) = 대륙 한가운데)에서 그린 뒤 6조각으로 자른다.
  조각을 다시 붙이면 원래 틀이 되므로 회의의 '중앙 틀에 붙여 합치기'와 어긋나지 않는다.
    dunes   모래바람 오아시스 (x0..167,  y112..159)  사나 북문·큰길 옛길로 들어옴
    canyon  붉은 바위 협곡   (x168..319, y112..159)
    westvale 서쪽 포도밭 언덕 (x48..111, y56..111)   베네치아 쪽 구역과 맞닿음
    bazaar  만국 장터        (x112..207, y56..111)   중간 지점
    eastvale 동쪽 소나무 고원 (x208..271, y56..111)  고려 쪽 구역과 맞닿음
    steppe  한가운데 초원     (x104..215, y0..55)    꼭짓점 = 대륙 한가운데
- 경계는 대각선을 따르지 않고 모래 언덕·붉은 바위·숲 덩어리로 불규칙하게 (경계 두께 잡음).
- 큰길은 언제나 열려 있다 (회의: 중앙 진입을 막지 않음). 장애물(팀 장애물 시스템)은 샛길·지름길에만.
- 아이템 보상·수집 요소는 회의대로 맵 완성 뒤. 지금은 숨은 장소·지름길·이야기꾼이 탐험 보상.
"""
import math
import zlib
import arabia_art as A

FW, FH = 320, 160
PIECES = {
    'arabia_central_dunes': (0, 112, 168, 48, '모래바람 오아시스'),
    'arabia_central_canyon': (168, 112, 152, 48, '붉은 바위 협곡'),
    'arabia_central_westvale': (48, 56, 64, 56, '서쪽 포도밭 언덕'),
    'arabia_central_bazaar': (112, 56, 96, 56, '만국 장터'),
    'arabia_central_eastvale': (208, 56, 64, 56, '동쪽 소나무 고원'),
    'arabia_central_steppe': (104, 0, 112, 56, '한가운데 초원'),
}
# 출구: (조각, 키) → 틀 좌표 시작 (남북 경계는 x, 동서 경계는 y). 폭 4칸.
EXITS = {
    ('arabia_central_dunes', 's_01'): 158, ('arabia_central_dunes', 's_02'): 100,
    ('arabia_central_dunes', 'e_01'): 125, ('arabia_central_dunes', 'e_02'): 145,
    ('arabia_central_dunes', 'n_01'): 84, ('arabia_central_dunes', 'n_02'): 140,
    ('arabia_central_canyon', 'w_01'): 125, ('arabia_central_canyon', 'w_02'): 145,
    ('arabia_central_canyon', 'n_01'): 186, ('arabia_central_canyon', 'n_02'): 236,
    ('arabia_central_westvale', 's_01'): 84, ('arabia_central_westvale', 'e_01'): 79,
    ('arabia_central_westvale', 'n_01'): 108,
    ('arabia_central_bazaar', 's_01'): 140, ('arabia_central_bazaar', 's_02'): 186,
    ('arabia_central_bazaar', 'w_01'): 79, ('arabia_central_bazaar', 'e_01'): 86,
    ('arabia_central_bazaar', 'n_01'): 158,
    ('arabia_central_eastvale', 's_01'): 236, ('arabia_central_eastvale', 'w_01'): 86,
    ('arabia_central_eastvale', 'n_01'): 209,
    ('arabia_central_steppe', 's_01'): 108, ('arabia_central_steppe', 's_02'): 158,
    ('arabia_central_steppe', 's_03'): 209,
}
OUTER_EXITS = {('arabia_central_dunes', 's_01'), ('arabia_central_dunes', 's_02')}   # 아라비아 지역과 맞닿는 출구


def tri_e(x, y):
    """삼각형 안에서 대각선 경계까지 칸 수 (음수 = 바깥)."""
    return min(x - (159 - y), (160 + y) - x)


def _h(*a):
    return (zlib.crc32(repr(a).encode()) & 0xFFFFFF) / float(0xFFFFFF)


def vnoise(x, y, scale, seed):
    gx, gy = x / scale, y / scale
    x0, y0 = int(math.floor(gx)), int(math.floor(gy))
    fx, fy = gx - x0, gy - y0
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = _h(seed, x0, y0), _h(seed, x0 + 1, y0)
    c, d = _h(seed, x0, y0 + 1), _h(seed, x0 + 1, y0 + 1)
    return (a * (1 - sx) + b * sx) * (1 - sy) + (c * (1 - sx) + d * sx) * sy


def climate(x, y):
    """0 = 한가운데(초원) … 1 = 사나 쪽(사막). 회의의 기후 그라데이션."""
    return y / 159 + 0.16 * (vnoise(x, y, 16, 'c1') - 0.5) + 0.07 * (vnoise(x, y, 6, 'c2') - 0.5)


def zone_of(x, y):
    t = climate(x, y)
    if y >= 112 and x >= 168:
        return 'red'                                  # 붉은 바위 협곡
    return 'sand' if t > 0.70 else 'dry' if t > 0.36 else 'grass'


def wadi_y(x):
    return 125 + int(round(2.6 * math.sin(x / 13.0) + 1.3 * math.sin(x / 5.3 + 2)))


def stream_y(x):
    raw = 31 + 2.2 * math.sin(x / 9.0) + 1.2 * math.sin(x / 4.1 + 1)
    for bx in (138, 171):                             # 다리 근처는 곧게
        k = min(1.0, max(0.0, (abs(x - bx) - 4) / 5))
        if abs(x - bx) < 10:
            return int(round(31 * (1 - k) + raw * k))
    return int(round(raw))


def catmull(pts, step=0.35):
    """제어점을 지나는 부드러운 곡선 위의 점들."""
    out = []
    P = [pts[0]] + list(pts) + [pts[-1]]
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        seg = max(1.0, math.dist(p1, p2))
        n = int(seg / step) + 1
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in (0, 1)))
    out.append(pts[-1])
    return out


class Piece:
    """틀 좌표로 그리면 이 조각 안에 드는 것만 그려지는 붓."""

    def __init__(self, mid, L):
        self.mid = mid
        self.ox, self.oy, w, h, name = PIECES[mid]
        self.m = A.ArabiaMap(mid, name, w, h, seed=zlib.crc32(mid.encode()) & 0xFFFF, theme='town')
        self.m.soft_under = True
        self.m.obstacles = []
        self.L = L
        self.roads = set()

    # 좌표
    def has(self, x, y):
        return self.ox <= x < self.ox + self.m.w and self.oy <= y < self.oy + self.m.h

    def lc(self, x, y):
        return x - self.ox, y - self.oy

    # 바닥
    def cell(self, kind, x, y, grade=0, reserve=True):
        if self.has(x, y) and tri_e(x, y) >= 0:
            lx, ly = self.lc(x, y)
            self.m.paint(kind, lx, ly, lx, ly, grade=grade, reserve=reserve)
            if grade:
                self.roads.add((x, y))

    def rect(self, kind, x0, y0, x1, y1, grade=0, reserve=True):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.cell(kind, x, y, grade, reserve)

    def ellipse(self, kind, cx, cy, rx, ry, grade=0, reserve=True):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                    self.cell(kind, x, y, grade, reserve)

    def curve(self, kind, pts, width=4, grade=3):
        """부드럽게 굽은 길 (폭 width)."""
        r = width / 2
        done = set()
        for (px, py) in catmull(pts):
            for y in range(int(py - r) - 1, int(py + r) + 2):
                for x in range(int(px - r) - 1, int(px + r) + 2):
                    if (x, y) in done:
                        continue
                    if (x + 0.5 - px) ** 2 + (y + 0.5 - py) ** 2 <= r * r + 0.3:
                        done.add((x, y))
                        self.cell(kind, x, y, grade)

    def bridge(self, x0, y0, x1, y1, axis, grade=2):
        if all(self.has(x, y) for x in (x0, x1) for y in (y0, y1)):
            a, b = self.lc(x0, y0)
            c, d = self.lc(x1, y1)
            self.m.bridge(a, b, c, d, axis, grade)
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    self.roads.add((x, y))

    # 그림·사람
    def fits(self, x, y, w, h, margin=2):
        return all(self.has(xx, yy) and tri_e(xx, yy) >= margin for xx in (x, x + w - 1) for yy in (y, y + h - 1))

    def put(self, name, x, y, **kw):
        a = A.alias(name, x, y)
        cw, ch = A.size(a)
        if not self.fits(x, y, cw, ch):
            return False
        lx, ly = self.lc(x, y)
        return A.place(self.m, name, lx, ly, **kw)

    def stamp(self, name, x, y, force=False):
        from tiles import STAMPS
        w, h = STAMPS[name][3], STAMPS[name][4]
        if not self.fits(x, y, w, h):
            return False
        lx, ly = self.lc(x, y)
        return self.m.stamp(name, lx, ly, force=force)

    def talker(self, x, y, name, sprite, *lines):
        if self.has(x, y) and tri_e(x, y) >= 2:
            lx, ly = self.lc(x, y)
            self.m.talker(lx, ly, name, sprite, *lines)

    def villager(self, x, y, variant=0):
        if self.has(x, y) and tri_e(x, y) >= 2:
            lx, ly = self.lc(x, y)
            self.m.add('villager', lx, ly, variant=variant)

    def sign(self, x, y, text):
        if self.fits(x, y, 2, 2):
            lx, ly = self.lc(x, y)
            self.m.asign(lx, ly, text)

    def obstacle(self, defid, x, y, w, h, tag):
        if self.fits(x, y, w, h, margin=0):
            lx, ly = self.lc(x, y)
            self.m.obstacles.append(dict(id=f'{self.mid}_{tag}', definitionId=defid, x=lx, y=ly, width=w, height=h,
                                         enabled=True, phaseOffset=0.0))


# =====================================================================
#  1. 땅 모양 (바닥·길·물) — 모든 조각에 같은 틀 좌표로
# =====================================================================
def geometry(P):
    # ---- 바닥: 기후 그라데이션 ----
    for ly in range(P.m.h):
        for lx in range(P.m.w):
            x, y = lx + P.ox, ly + P.oy
            z = zone_of(x, y)
            if z != 'sand':
                P.m.paint(z, lx, ly, lx, ly, reserve=False)

    # ---- 모래바람 오아시스 (dunes) ----
    for (cx, cy, rx, ry) in [(40, 150, 18, 5), (70, 140, 14, 4), (30, 136, 9, 3), (96, 152, 12, 3.6), (126, 148, 8, 2.6)]:
        P.ellipse('dune', cx, cy, rx, ry, reserve=False)
    for x in range(0, 168):                           # 와디 (마른 강바닥) → 협곡으로
        for yy in range(wadi_y(x), wadi_y(x) + 3):
            P.cell('gravel', x, yy, reserve=False)
    P.rect('paving', 150, 150, 167, 159, grade=3)     # 사나 북문 밖 광장
    P.curve('road', [(159.5, 159), (157, 151), (148, 143), (145, 135), (150, 128), (147, 120), (141.5, 113), (141.5, 111)], 4, 3)
    P.curve('gravel', [(150, 128), (158, 127), (168, 126.5)], 4, 2)        # 와디 길 → 협곡
    P.curve('road', [(148, 143), (130, 146), (114, 141), (98, 134), (90, 124), (85.5, 116), (85.5, 111)], 3, 2)
    P.curve('gravel', [(162, 152), (165, 149), (168, 146.5)], 2, 1)        # 동쪽 좁은 틈 → 협곡 아랫길
    P.curve('gravel', [(101.5, 159), (103, 152), (108, 147), (114, 144)], 2, 1)   # 큰길 옛길 (아라비아 큰길에서)
    P.ellipse('grass', 113, 141, 9, 5.4)
    P.ellipse('water', 113.5, 140.5, 4.6, 2.3)
    # 서쪽 숨은 샘 (모래 언덕 너머)
    P.curve('dune', [(90, 135), (76, 140), (62, 143), (52, 146)], 2, 0)
    P.ellipse('grass', 46, 147, 5.5, 3.6)
    P.ellipse('water', 46, 147, 2.6, 1.6)

    # ---- 붉은 바위 협곡 (canyon) ----
    for x in range(168, 320):
        for yy in range(wadi_y(x), wadi_y(x) + 3):
            P.cell('gravel', x, yy, reserve=False)
    P.curve('gravel', [(168, 126.5), (178, 125), (184, 119), (187.5, 113), (187.5, 111)], 4, 3)
    P.curve('gravel', [(178, 125), (196, 129), (212, 126), (226, 120), (237.5, 114), (237.5, 111)], 3, 2)
    P.curve('gravel', [(168, 146.5), (180, 150), (196, 148), (206, 139), (212, 127)], 2, 1)    # 좁은 협곡 아랫길
    P.curve('gravel', [(226, 120), (244, 128), (258, 134), (266, 136)], 2, 1)                # 협곡 마을 터
    P.ellipse('brick', 265, 135, 8, 3.2)
    P.curve('gravel', [(210, 129), (204, 136), (199, 141)], 1, 1)                             # 아치 전망대 샛길

    # ---- 서쪽 포도밭 언덕 (westvale) ----
    P.curve('road', [(85.5, 111), (83, 100), (88, 90), (100, 84), (112, 80.5)], 3, 2)
    P.curve('road', [(83, 100), (72, 103), (64, 104)], 2, 1)                  # 베네치아 쪽 구역으로 (합칠 때)
    P.curve('road', [(100, 84), (101, 75), (106, 65), (109.5, 57), (109.5, 55)], 2, 1)    # 숨은 고갯길 → 초원
    P.ellipse('water', 92, 70, 4, 2.4)

    # ---- 만국 장터 (bazaar) ----
    P.curve('paving', [(141.5, 111), (142, 102), (150, 95), (154, 91)], 4, 3)        # 남문 → 광장
    P.curve('paving', [(187.5, 111), (184, 101), (176, 94), (172, 91)], 3, 3)        # 남동문 → 광장
    P.rect('cobble', 148, 74, 172, 90, grade=3)                                        # 시계탑 광장
    P.rect('cobble', 158, 56, 161, 73, grade=3)                                        # 북문 거리
    P.rect('cobble', 112, 79, 147, 81, grade=3)                                        # 서문 거리
    P.rect('road', 173, 86, 207, 88, grade=3)                                          # 동문 거리 (흙길)
    # 북서: 운하 거리 (유리 공방)
    P.rect('boards', 113, 64, 146, 65, grade=2)
    P.rect('water', 113, 66, 146, 67)
    P.rect('boards', 113, 68, 146, 69, grade=2)
    P.bridge(121, 66, 122, 67, 'v', 2)
    P.bridge(137, 66, 138, 67, 'v', 2)
    P.rect('cobble', 113, 74, 146, 75, grade=2)                                        # 운하 남쪽 골목
    P.rect('cobble', 133, 70, 133, 73, grade=1)                                        # 집 사이 쪽골목
    P.rect('cobble', 146, 64, 146, 74, grade=2)
    # 북동: 나무 장원 거리 (북유럽)
    P.rect('boards', 174, 64, 205, 65, grade=2)
    P.rect('boards', 174, 64, 175, 73, grade=2)
    # 동쪽: 기와 골목 (뒷골목 포함)
    P.rect('road', 176, 80, 205, 81, grade=1)
    P.rect('road', 176, 80, 177, 85, grade=1)
    # 남서: 공연 천막 마당
    P.ellipse('dry', 125, 97, 11, 8, reserve=False)
    P.curve('road', [(131, 82), (132, 92), (130, 101), (124, 107)], 2, 1)
    P.ellipse('cobble', 161, 100, 10, 5.5, grade=2)                                    # 남쪽 공연 마당
    P.curve('cobble', [(160, 90), (161, 95)], 3, 2)
    # 숨은 정원 (운하 남쪽, 울타리 안)
    P.rect('grass', 124, 76, 131, 78)
    # 남동: 가축 우리 마당
    P.rect('dry', 190, 100, 205, 108, reserve=False)

    # ---- 동쪽 소나무 고원 (eastvale) ----
    P.curve('road', [(237.5, 111), (234, 100), (226, 94), (216, 89), (208, 87.5)], 3, 2)
    P.curve('road', [(234, 100), (246, 100), (254, 99)], 2, 1)                 # 고려 쪽 구역으로 (합칠 때)
    P.curve('road', [(226, 94), (223, 82), (215, 70), (210.5, 58), (210.5, 55)], 2, 1)    # 숨은 고갯길 → 초원
    P.ellipse('water', 244, 84, 3.5, 2.2)

    # ---- 한가운데 초원 (steppe) ----
    for x in range(104, 216):
        yc = stream_y(x)
        prev = stream_y(x - 1)
        for yy in range(min(yc, prev), max(yc, prev) + 2):
            if tri_e(x, yy) >= 1:
                P.cell('water', x, yy)
    P.curve('road', [(159.5, 55), (157, 46), (166, 39), (171.5, 34), (171.5, 29), (163, 22), (159.5, 14), (159.5, 9)], 4, 3)
    P.curve('road', [(109.5, 55), (116, 48), (134, 42), (150, 41), (160, 42)], 2, 2)
    P.curve('road', [(210.5, 55), (204, 47), (186, 42), (168, 40)], 2, 2)
    P.curve('road', [(134, 42), (138, 36), (138.5, 29), (144, 22), (152, 16)], 2, 1)        # 서쪽 둘째 다리
    P.bridge(170, 30, 173, 33, 'v', 3)
    P.bridge(138, 30, 139, 33, 'v', 1)


# =====================================================================
#  2. 경계 (삼각형이 보이지 않게, 조각 사이도 자연 지형으로)
# =====================================================================
def walls(P):
    """막는 칸: 삼각형 경계 띠(두께 잡음) + 조각 가장자리 띠. 출구 앞은 비우고, 길은 지나가게 둔다."""
    m = P.m
    keep = set()
    for (mid, key), start in EXITS.items():
        if mid != P.mid:
            continue
        edge = P.L[key][0]
        for a in range(start - 2, start + 6):
            for d in range(0, 7):
                if edge == 'N': keep.add((a, P.oy + d))
                if edge == 'S': keep.add((a, P.oy + m.h - 1 - d))
                if edge == 'W': keep.add((P.ox + d, a))
                if edge == 'E': keep.add((P.ox + m.w - 1 - d, a))
    wall = set()
    for ly in range(m.h):
        for lx in range(m.w):
            x, y = lx + P.ox, ly + P.oy
            e = tri_e(x, y)
            if e < 0 or (x, y) in keep:
                continue
            bnd = 3 + 13 * vnoise(x, y, 13, 'bd') + 4 * vnoise(x, y, 4, 'bd2')
            cut = 2 + 6 * vnoise(x, y, 9, 'ct') + 2 * vnoise(x, y, 3, 'ct2')
            if P.mid in ('arabia_central_westvale', 'arabia_central_eastvale', 'arabia_central_steppe'):   # 좁은 곳은 경계를 얇게
                bnd = 2 + 6 * vnoise(x, y, 13, 'bd') + 2 * vnoise(x, y, 4, 'bd2')
            if P.mid == 'arabia_central_steppe':        # 초원: 경계를 얇게, 꼭짓점 둘레는 더 얇게 (트인 풀밭)
                bnd = 2 + (2 if y < 24 else 4) * vnoise(x, y, 11, 'bd')
            if P.mid == 'arabia_central_bazaar':        # 장터는 마을이라 가장자리 띠를 얇게
                cut = 1 + 2 * vnoise(x, y, 7, 'ct')
            edge_d = min(lx, ly, m.w - 1 - lx, m.h - 1 - ly)
            if (e < bnd or edge_d < cut) and ((x, y) not in P.roads or e < 2):
                wall.add((lx, ly))
    for (lx, ly) in wall:
        A.block_rect(m, lx, ly, lx, ly)
    return wall


THEME = {
    'arabia_central_dunes': (['drawn_boulder', 'drawn_datePalm2', 'drawn_rockWall', 'drawn_datePalm3', 'nature_dune'], 'dune'),
    'arabia_central_canyon': (['drawn_redRockWall', 'drawn_redBoulder', 'drawn_redRockWall', 'drawn_redRockWall'], 'red'),
    'arabia_central_westvale': (['drawn_oliveGrove', 'drawn_cypress', 'drawn_olive', 'drawn_cypress', 'drawn_grove'], None),
    'arabia_central_bazaar': (['drawn_cypress', 'drawn_oasisTree', 'drawn_datePalm2', 'drawn_olive', 'drawn_cypress'], None),
    'arabia_central_eastvale': (['@pinebig', 'drawn_cypress', 'drawn_boulder', 'drawn_oasisTree2', 'drawn_olive', 'drawn_rockWall'], None),
    'arabia_central_steppe': (['drawn_grove', 'drawn_oasisTree', 'drawn_oliveGrove', 'drawn_oasisTree2', 'drawn_almond', 'drawn_olive'], None),
}


VIEW = 22


def dress_walls(P, wall):
    m = P.m
    names, floor = THEME[P.mid]
    outside = {(lx, ly) for ly in range(m.h) for lx in range(m.w)
               if -VIEW <= tri_e(lx + P.ox, ly + P.oy) < 0}
    wall = set(wall) | outside
    if floor:
        for (lx, ly) in wall:
            if m.zone[ly][lx] not in ('water',) and (lx + P.ox, ly + P.oy) not in P.roads:
                m.paint(floor, lx, ly, lx, ly, reserve=False)
    cells = sorted(wall, key=lambda c: _h('wd', P.mid, c))
    sparse = 0.45 if P.mid == 'arabia_central_steppe' else 1.0     # 초원 경계는 나무를 듬성듬성
    for (lx, ly) in cells:
        if _h('ws', lx, ly) > sparse:
            continue
        name = names[int(_h('wn', lx, ly) * len(names))]
        if name.startswith('@'):
            from tiles import STAMPS
            w, h = STAMPS[name[1:]][3], STAMPS[name[1:]][4]
            if all((lx + dx, ly + dy) in wall or not m.inside(lx + dx, ly + dy) for dx in range(w) for dy in (h - 1,)):
                if m.inside(lx, ly) and m.inside(lx + w - 1, ly + h - 1):
                    m.stamp(name[1:], lx, ly, force=True, over_road=True)
            continue
        cw, ch = A.size(A.alias(name, lx, ly))
        foot = [(lx + dx, ly + ch - 1) for dx in range(cw)]
        if all(c in wall for c in foot):
            A.place(m, name, lx, ly, foot=1, force=True)


# =====================================================================
#  3. 꾸밈·사람 (조각별로 자기 안의 것만 그려진다)
# =====================================================================
def dress(P):
    T = P.talker
    # ---------------- 모래바람 오아시스 ----------------
    for x in (150, 167):
        P.put('plaza_lamp', x if x < 167 else 166, 149, foot=1, force=True)
    for x, y in [(147, 151), (147, 155)]:
        P.put('drawn_datePalm', x, y, force=True)
    P.sign(152, 151, '중앙 대륙 남부 — 모래바람 오아시스\n북: 와디를 건너 만국 장터 · 서: 오아시스와 서쪽 언덕 · 동: 와디 따라 붉은 협곡\n남: 사나 북문')
    T(165, 156, '북문 밖 낙타몰이', 'arabia_npc_bedouin_tall',
      '여기서부터 사막이 조금씩 풀밭으로 바뀌어. 북쪽 장터까지 가는 길은 하나가 아니야.',
      '와디를 따라 동쪽으로 가면 붉은 협곡, 오아시스를 지나 서쪽으로 가면 언덕길이지.')
    P.put('building_malwiya', 124, 128, foot=2)                     # 랜드마크: 옛 길잡이 탑
    P.sign(128, 133, '옛 길잡이 탑\n대상들은 이 탑을 보고 와디를 건넜다.\n탑에서 북쪽 = 장터, 서쪽 = 오아시스')
    P.put('drawn_dhow', 131, wadi_y(131) - 1, foot=1, force=True)
    T(135, wadi_y(135) + 3, '모래 배 주인', 'arabia_npc_sailor_tall',
      '이 와디는 지금은 마른 강이지만, 우기가 오면 물이 차서 배가 다닌다네.',
      '그땐 이 배로 장터까지 짐을 실어 나르지. 지금은 기다리는 중이야.')
    for x, y in [(102, 137), (122, 136), (104, 145), (121, 146), (111, 133)]:
        P.put(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][x % 3], x, y, force=True)
    T(108, 146, '오아시스 물지기', 'arabia_npc_child_tall', '사막 한가운데라도 여긴 물이 마르지 않아요.',
      '서쪽 모래 언덕 사이에 숨은 샘이 하나 더 있대요. 언덕 사이 좁은 틈을 찾아보세요.')
    P.put('building_tent', 150, 135, foot=2)
    P.put('drawn_camelRest', 154, 137, foot=1)
    T(152, 139, '쉬어 가는 대상', 'arabia_npc_bedouin_tall', '장터로 곧장 가려면 탑을 지나 북쪽으로. 짐이 무거우면 쉬었다 가게.')
    P.sign(104, 153, '큰길 옛길\n남쪽으로 내려가면 아라비아의 안전한 큰길 (낙석을 치워야 지날 수 있음)')
    # 서쪽 숨은 샘
    for x, y in [(41, 144), (50, 143), (43, 149), (49, 149)]:
        P.put(['drawn_datePalm2', 'drawn_datePalm3'][x % 2], x, y, force=True)
    T(45, 150, '숨은 샘의 시인', 'arabia_npc_scholar_tall',
      '여기까지 찾아왔군. 이 샘은 지도에도 없지.',
      '장터의 시계탑이 보이나? 길을 잃으면 그 탑을 기준으로 삼게.')
    # 모래에 묻힌 옛 성벽 · 버려진 대상 터
    for x in (80, 84, 88):
        P.put('wall_ruin', x, 144 + (x % 3), foot=2)
    P.put('wall_tower', 92, 141, foot=3)
    T(86, 149, '성벽 터의 나그네', 'arabia_npc_merchant_tall', '모래에 반쯤 묻힌 이 성벽은 아주 옛날 대상 요새래.',
      '여기서 서쪽 모래 언덕을 넘으면 숨은 샘이 있다는 소문이야. 모래가 깊으니 천천히 가.')
    for (n, x, y) in [('drawn_cart', 64, 128), ('drawn_crates', 67, 129), ('drawn_barrel', 70, 128), ('drawn_camelRest', 62, 131)]:
        P.put(n, x, y, foot=1)
    for (x, y) in [(36, 140), (100, 128), (118, 152), (56, 155), (138, 140), (28, 152)]:
        P.put('nature_dune', x, y, foot=1)
    for (cx, cy) in [(70, 120), (30, 146), (140, 118)]:
        for k in range(5):
            P.put(['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3'][k % 3],
                  cx + int((_h('px', cx, k) - .5) * 10), cy + int((_h('py', cy, k) - .5) * 6))
    for (x, y) in [(52, 132), (96, 144), (134, 152), (78, 126), (40, 128)]:
        P.stamp('module_cactus', x, y)
    P.obstacle('loose_sand', 60, 142, 5, 3, 'sand1')
    P.obstacle('loose_sand', 76, 139, 5, 3, 'sand2')
    P.obstacle('sand_gust', 138, 147, 3, 3, 'gust')
    for (x0, y0, x1, y1) in [(60, 124, 74, 132), (20, 128, 34, 134), (98, 118, 112, 126)]:
        rockfield(P, x0, y0, x1, y1, ['drawn_rockWall', 'drawn_boulder', 'drawn_boulderSmall'], 0.5)

    # ---------------- 붉은 바위 협곡 ----------------
    P.put('drawn_rockArch', 196, 135, foot=1)
    P.sign(201, 140, '붉은 바위 아치 전망대\n아치 아래로 지나가면 협곡 아랫길이 보인다.')
    P.obstacle('rockslide', 186, 148, 4, 3, 'slot')
    reds = ['drawn_redRockWall', 'drawn_redBoulder', 'drawn_redBoulderSmall']
    barrier(P, [(x, 131) for x in range(168, 190)], reds)
    barrier(P, [(188, y) for y in range(131, 160) if not 148 <= y <= 150] + [(189, y) for y in range(131, 160) if not 148 <= y <= 150],
            ['drawn_redBoulder'])
    P.sign(176, 152, '좁은 협곡 아랫길\n낙석을 치우면 동쪽 고원 쪽으로 질러갈 수 있다.')
    P.put('building_tent', 228, 140, foot=2)
    P.put('drawn_camel', 233, 142, foot=1)
    P.put('drawn_camelRest', 224, 143, foot=1)
    T(231, 145, '협곡 안내인', 'arabia_npc_bedouin_tall',
      '북쪽으로 오르면 장터, 북동쪽 고원으로 가면 소나무 숲이 나와.',
      '아래쪽 좁은 협곡은 낙석이 자주 막혀. 치우면 지름길이 되지.')
    for x in (258, 263, 268):
        P.put(['house_mudbrick', 'house_sand', 'house_dome'][x % 3], x, 129, foot=2)
    P.put('drawn_well', 265, 136, foot=1)
    T(262, 137, '협곡 마을 할머니', 'arabia_woman_grandma_tall', '바위 그늘에 마을을 지으면 한낮에도 시원하단다.',
      '장터 사람들은 이 마을을 잘 몰라. 길이 협곡 안쪽으로 숨어 있거든.')
    for (x0, y0, x1, y1) in [(190, 113, 205, 122), (214, 130, 226, 138), (240, 113, 256, 124), (272, 128, 290, 146),
                             (238, 146, 262, 156), (200, 150, 225, 157), (175, 132, 190, 143)]:
        rockfield(P, x0, y0, x1, y1, ['drawn_redRockWall', 'drawn_redBoulder'], 0.45)
    for (x, y) in [(214, 142), (206, 118), (246, 140), (222, 150), (196, 152), (270, 122), (180, 140), (232, 128)]:
        P.stamp('module_cactus', x, y)
    for (x, y) in [(220, 134), (250, 130), (210, 152), (236, 152)]:
        P.put('drawn_redRockWall', x, y, foot=1)
    P.put('drawn_crates', 270, 134, foot=1)
    P.put('drawn_jar', 259, 136, foot=1)

    # ---------------- 서쪽 포도밭 언덕 ----------------
    for row in range(4):                                             # 포도밭 (가지런한 줄)
        y = 96 + row * 3
        for x in range(89, 99, 2):
            P.put('drawn_bush', x, y, foot=1)
        P.put('drawn_cypress', 87, y - 2)
    P.put('wall_tower', 101, 95, foot=3)                             # 랜드마크: 옛 망루
    P.put('wall_ruin', 99, 103, foot=2)
    T(96, 94, '포도밭 지기', 'arabia_woman_weaver_tall',
      '서쪽 운하 도시 사람들이 이 포도를 사 가요. 서쪽 길 끝에서 그쪽 구역과 이어질 거래요.',
      '북쪽 고갯길은 가시덤불에 막혀 있어요. 치우면 초원으로 바로 가요.')
    P.sign(64, 100, '← 베네치아 쪽 구역\n(중앙 대륙을 합칠 때 이어집니다)')
    P.obstacle('thorn_brush', 100, 72, 3, 3, 'thorn')
    barrier(P, [(x, 73) for x in range(80, 112) if not 100 <= x <= 102], ['drawn_bush', 'drawn_oleanderBush', 'drawn_sage'])
    for x, y in [(88, 72), (96, 72), (90, 67)]:
        P.put('drawn_oleanderBush', x, y, layer='deco')
    P.villager(90, 92, 2)

    # ---------------- 만국 장터 ----------------
    bazaar(P)

    # ---------------- 동쪽 소나무 고원 ----------------
    P.stamp('bigtree', 238, 86)
    P.put('drawn_ovoo', 235, 88, foot=1)
    T(242, 90, '고원의 나무꾼', 'arabia_npc_weaver_tall',
      '이 큰 나무가 고원의 이정표야. 동쪽으로 가면 반도 사람들 구역과 이어질 거래.',
      '북쪽 고갯길엔 바위가 굴러 내려와 있어. 치우면 초원으로 질러가지.')
    P.sign(252, 96, '고려 쪽 구역 →\n(중앙 대륙을 합칠 때 이어집니다)')
    P.obstacle('boulder', 221, 76, 3, 3, 'boulder')
    barrier(P, [(x, 77) for x in range(208, 245) if not 221 <= x <= 223], ['drawn_boulder', 'drawn_boulderSmall', 'drawn_rockWall'])
    P.put('building_tent', 228, 102, foot=2)
    for i, (x, y) in enumerate([(220, 104), (224, 106), (218, 107)]):
        P.put('drawn_sheep' if i % 2 else 'drawn_sheep2', x, y, foot=1)
    for (x, y) in [(228, 70), (234, 66), (244, 92), (216, 64), (226, 82), (214, 80)]:
        P.stamp('pinebig', x, y)
    rockfield(P, 244, 92, 256, 98, ['drawn_rockWall', 'drawn_boulder', 'drawn_boulderSmall'], 0.5)

    # ---------------- 한가운데 초원 ----------------
    for x, y in [(153, 10), (163, 10), (147, 18), (168, 17)]:
        P.put('drawn_yurt' if x % 2 == 0 else 'drawn_yurt2', x, y, foot=2, force=True)
    P.put('drawn_ovoo', 159, 4, foot=1, force=True)                 # 대륙 한가운데 표지
    P.sign(161, 5, '대륙의 한가운데 (초원)\n네 지역의 구역이 여기서 만납니다.')
    T(156, 19, '게르의 할아버지', 'arabia_npc_scholar_tall',
      '어서 오게, 남쪽 사막에서 왔구먼. 여기가 대륙 한가운데 초원일세.',
      '서쪽엔 운하 도시 사람들, 동쪽엔 반도 사람들이 이 초원으로 장사하러 온다네.')
    T(177, 36, '개울가 아이', 'arabia_woman_girl_tall', '개울 물이 차가워요! 서쪽에도 작은 다리가 하나 더 있어요.')
    for i, (x, y) in enumerate([(140, 26), (143, 28), (182, 26), (179, 29), (132, 44), (190, 46), (148, 48), (176, 50)]):
        P.put('drawn_sheep' if i % 2 else 'drawn_sheep2', x, y, foot=1)
    P.villager(165, 45, 1)
    for (cx, cy) in [(126, 46), (194, 48), (186, 24)]:
        for k in range(5):
            P.put(['drawn_oasisTree', 'drawn_grove', 'drawn_oasisTree2', 'drawn_olive', 'drawn_almond'][k % 5],
                  cx + int((_h('gx', cx, k) - 0.5) * 12), cy + int((_h('gy', cy, k) - 0.5) * 7), foot=1)
    for (cx, cy) in [(140, 36), (182, 50), (150, 14), (200, 44), (124, 52), (166, 26)]:
        for k in range(22):
            x = cx + int((_h('fx', cx, k) - 0.5) * 10)
            y = cy + int((_h('fy', cy, k) - 0.5) * 6)
            P.put(['drawn_wildflowerY', 'drawn_wildflowerW', 'drawn_wildflowerP', 'drawn_wildflowerB'][(cx + k) % 4], x, y, layer='deco')


def barrier(P, cells, names):
    """장애물을 돌아가지 못하게 막는 줄 (바위·덤불). 칸은 막고, 그 칸을 발로 하는 그림을 촘촘히 놓는다."""
    cells = [c for c in cells if P.has(*c) and tri_e(*c) >= 0]
    for i, (x, y) in enumerate(cells):
        lx, ly = P.lc(x, y)
        A.block_rect(P.m, lx, ly, lx, ly)
    cs = set(cells)
    for i, (x, y) in enumerate(cells):
        lx, ly = P.lc(x, y)
        for name in names[i % len(names):] + names[:i % len(names)]:
            cw, ch = A.size(name)
            if all((x + dx, y) in cs for dx in range(cw)):      # 틈(장애물 자리)으로 삐져나오지 않게
                if A.place(P.m, name, lx, ly - ch + 1, foot=1, force=True):
                    break


def rockfield(P, x0, y0, x1, y1, names, density):
    cells = [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)]
    cells.sort(key=lambda c: _h('rf', c))
    for name in names:
        cw, ch = A.size(name)
        for (x, y) in cells:
            if x + cw - 1 > x1 or y + ch - 1 > y1 or _h('rd', x, y, name) > density:
                continue
            P.put(name, x, y, foot=1)


def bazaar(P):
    """만국 장터: 여러 문화가 섞인 판타지 장터. 아라비아풍 고급 상점은 3곳만 (비단 상관·향료 궁전·등불 찻집)."""
    T = P.talker
    # ---- 시계탑 광장 (가운데, 자갈 포장) ----
    P.put('drawn_clockTower', 146, 66, foot=1, force=True)                  # 랜드마크: 장터 어디서나 보인다
    P.put('building_fountain', 159, 81, foot=2, force=True)
    for x, y in [(148, 74), (172, 74), (148, 90), (172, 90)]:
        P.put('plaza_lamp', x, y - 1, foot=1, force=True)
    for (name, x, y) in [('drawn_nordShop', 150, 76), ('drawn_tentStripe3', 168, 76), ('drawn_venGlass2', 150, 85),
                         ('drawn_tentStripe2', 168, 86), ('stall_spice', 154, 77), ('drawn_nordShop', 164, 77)]:
        P.put(name, x, y, foot=1, force=True)
    for (x, y) in [(155, 86), (164, 86)]:
        P.stamp('cart', x, y, force=True)
    for (x, y) in [(157, 84), (161, 84)]:
        P.put('drawn_pottedOrange', x, y, foot=1, force=True)
    T(160, 88, '장터지기 카림', 'arabia_npc_merchant_tall',
      '만국 장터에 온 걸 환영해요! 닷새마다 큰 장이 서면 네 대륙 상인이 다 모여요.',
      '길을 잃으면 시계탑을 봐요. 북쪽은 초원, 서쪽은 운하 거리, 동쪽은 기와 골목이에요.')
    P.sign(144, 106, '만국 장터 안내\n가운데: 시계탑 광장 · 북서: 운하 거리(유리 공방) · 북동: 나무 장원 거리\n동: 기와 골목 · 남: 공연 마당 · 남서: 떠돌이 상인 야영지 · 북문: 한가운데 초원')
    P.villager(158, 79, 0)
    P.villager(166, 89, 2)
    # ---- 아라비아풍 고급 상점 1·2 (광장 북쪽) ----
    P.put('house_courtyardManor', 150, 67, foot=2)                          # 비단·양탄자 상관
    P.put('civic_palace', 163, 66, foot=2)                                  # 황금 향료 궁전
    for (n, x, y) in [('prop_lustre', 149, 72), ('prop_lustre', 156, 72), ('prop_carpetRoll', 152, 72),
                      ('prop_lantern', 162, 72), ('prop_lantern', 171, 72), ('prop_perfume', 165, 72), ('prop_incense', 168, 72)]:
        P.put(n, x, y, foot=1, force=True)
    T(154, 73, '비단 상인 레일라', 'arabia_woman_perfumer_tall', '사막을 건너온 비단과 양탄자예요. 등불 아래서 보면 더 반짝이죠.')
    T(167, 74, '향료 궁전 주인', 'arabia_npc_merchant_tall', '이 궁전의 향료는 사나에서도 귀해요. 금빛 상자에 담아 드리지요.')
    # ---- 북서: 운하 거리 (베네치아 풍 유리 공방) ----
    for (n, x) in [('drawn_venHouse2', 114), ('drawn_venGlass', 117), ('drawn_venHouse', 121), ('drawn_venHouse3', 124),
                   ('drawn_venGlass2', 127), ('drawn_venHouse', 131), ('drawn_venHouse2', 135), ('drawn_venHouse3', 139),
                   ('drawn_venHouse', 142)]:
        P.put(n, x, 60, foot=1)
    for (n, x) in [('drawn_venHouse3', 114), ('drawn_venHouse', 117), ('drawn_venGlass2', 120), ('drawn_venHouse2', 124),
                   ('drawn_venHouse', 128), ('drawn_venGlass', 134), ('drawn_venHouse3', 138), ('drawn_venHouse', 141)]:
        P.put(n, x, 70, foot=1)
    P.put('drawn_gondola', 126, 66, layer='deco', force=True)
    P.put('drawn_gondola', 141, 66, layer='deco', force=True)
    P.obstacle('drawbridge', 137, 66, 2, 2, 'drawbridge')
    T(119, 65, '유리 장인 루치아', 'arabia_woman_girl_tall', '운하 도시에서 배운 솜씨예요. 불빛에 비춰 보면 색이 달라져요.',
      '도개교는 배가 지나갈 때 잠깐 올라가요. 기다리거나 다른 다리로 돌아가요.')
    # 숨은 정원 (운하 남쪽 골목 아래, 덤불 울타리 안 · 쪽골목으로만)
    for x in range(123, 133):
        if x != 127:
            P.put('drawn_bush', x, 79, foot=1, force=True)
    for y in (76, 77, 78):
        P.put('drawn_bush', 123, y, foot=1, force=True)
        P.put('drawn_bush', 132, y, foot=1, force=True)
    P.put('drawn_roseBush', 125, 76, layer='deco', force=True)
    P.put('drawn_jasmineBush', 130, 76, layer='deco', force=True)
    P.put('drawn_pottedOrange', 128, 76, foot=1, force=True)
    T(126, 77, '숨은 정원의 정원사', 'arabia_woman_grandma_tall', '어머, 이 정원을 찾았네요. 골목 사이로 들어오는 사람은 드물어요.',
      '운하 다리 두 곳, 북문 거리, 서문 거리… 장터 어디서든 시계탑으로 돌아갈 수 있어요.')
    # ---- 북동: 나무 장원 거리 (북유럽 풍) ----
    for (n, x, y) in [('drawn_longhouse', 176, 60), ('drawn_nordShop', 182, 61), ('drawn_longhouse2', 186, 60),
                      ('drawn_nordShop', 192, 61), ('drawn_longhouse', 196, 60)]:
        P.put(n, x, y, foot=1)
    P.put('drawn_runestone', 185, 68, foot=1)
    P.put('drawn_longhouse2', 181, 73, foot=1)
    for (x, y) in [(179, 69), (195, 69), (202, 72), (190, 74)]:
        P.stamp('pinebig', x, y)
    P.put('drawn_netRack', 188, 70, foot=1)
    T(187, 67, '털가죽 상인 비요른', 'arabia_npc_bedouin_tall', '북쪽 눈 나라에서 온 털가죽이야. 사막 밤은 생각보다 춥거든.',
      '룬 돌 뒤로 돌아가면 기와 골목 뒷길로 이어져.')
    # ---- 동쪽: 기와 골목 (고려 풍, 공용 닌자 타일) ----
    for (n, x) in [('shop', 178), ('house_tile', 182), ('store', 187), ('house3', 192), ('house_tile', 196)]:
        P.stamp(n, x, 83)
    for (n, x) in [('house_tile', 178), ('shop', 183), ('house3', 187)]:
        P.stamp(n, x, 90)
    P.stamp('bigtree', 196, 92)
    for (x, y) in [(192, 92), (201, 84)]:
        P.stamp('cherry', x, y)
    P.stamp('paper_rack', 202, 90)
    T(194, 89, '종이 장수', 'arabia_npc_scholar_tall', '반도에서 온 한지예요. 장부를 쓰기엔 이만한 게 없지요.')
    T(200, 80, '뒷골목 찻집 노인', 'arabia_npc_weaver_tall', '뒷골목까지 들어왔구먼. 여기서 동문으로 나가면 소나무 고원이야.')
    # ---- 아라비아풍 고급 상점 3: 등불 찻집 (남동 거리) ----
    P.put('house_windTowerHouse', 188, 91, foot=2)
    for (n, x, y) in [('prop_lantern', 187, 96), ('prop_lantern', 192, 96), ('prop_ewer', 194, 95)]:
        P.put(n, x, y, foot=1, force=True)
    P.put('prop_rugseat', 189, 96, layer='deco', force=True)
    T(190, 98, '등불 찻집 주인', 'arabia_woman_qayna_tall', '사막 장미차 한 잔 하고 가요. 등불이 켜지면 장터가 제일 예뻐요.')
    # ---- 남쪽: 공연 마당 (떠돌이 공연단) ----
    P.put('drawn_stage', 158, 94, foot=1, force=True)
    for (n, x, y) in [('drawn_tentStripe', 151, 98), ('drawn_tentStripe3', 168, 98)]:
        P.put(n, x, y, foot=1, force=True)
    for (x, y) in [(156, 102), (163, 102)]:
        P.stamp('bench', x, y, force=True)
    T(160, 99, '떠돌이 곡예사', 'arabia_woman_girl_tall', '우린 장이 서는 곳마다 따라다녀요! 다음 장날에 공연 보러 와요.',
      '서남쪽 상인 야영지 뒤 덤불을 치우면 서문 거리로 빠지는 지름길이 있어요.')
    # ---- 남서: 떠돌이 상인 야영지 ----
    for (n, x, y) in [('drawn_tentStripe2', 115, 89), ('drawn_tentStripe', 121, 92), ('drawn_tentStripe3', 115, 97),
                      ('building_tent', 121, 100)]:
        P.put(n, x, y, foot=1)
    for (n, x, y) in [('drawn_crates', 126, 90), ('drawn_barrel', 127, 96), ('drawn_cart', 116, 104), ('drawn_camelRest', 126, 103)]:
        P.put(n, x, y, foot=1)
    barrier(P, [(x, 84) for x in range(113, 130) if x not in (116, 117)], ['drawn_bush', 'drawn_sage', 'drawn_bush', 'drawn_oleanderBush'])
    T(124, 97, '떠돌이 상인 오마르', 'arabia_npc_sailor_tall', '우린 장이 끝나면 다음 장터로 떠나요. 사막 너머, 초원 너머로.',
      '천막 뒤 덤불을 치우면 서문 거리로 빠지는 뒷길이 있지.')
    P.obstacle('thorn_brush', 116, 85, 2, 2, 'tents')
    # ---- 남동: 가축 우리 ----
    for x in range(192, 205, 2):
        P.put('prop_fence', x, 100, foot=1, force=True)
    for i, (x, y) in enumerate([(193, 103), (197, 104), (201, 103), (195, 107), (200, 107)]):
        P.put(['drawn_camel', 'drawn_sheep', 'drawn_camelRest', 'drawn_sheep2', 'drawn_sheep'][i], x, y, foot=1)
    T(190, 105, '가축 상인', 'arabia_npc_bedouin_tall', '장날엔 낙타랑 양이 이 우리에 가득 차지. 짐을 많이 실을 낙타를 찾나?')
    # ---- 길가 나무·집 ----
    for (x, y) in [(138, 104), (146, 98), (183, 106), (139, 88), (176, 76), (134, 90), (147, 92)]:
        P.put(['drawn_oasisTree', 'drawn_olive', 'drawn_cypress'][x % 3], x, y)
    for (n, x, y) in [('house_thatch', 136, 94), ('house_plain', 172, 104)]:
        P.stamp(n, x, y)


# =====================================================================
#  4. 조각 하나 만들기
# =====================================================================
def build_piece(mid, L):
    P = Piece(mid, L)
    m = P.m
    m.hours = 1
    geometry(P)
    for (pm, key), start in EXITS.items():
        if pm == mid:
            edge = L[key][0]
            local = start - (P.ox if edge in 'NS' else P.oy)
            m.exit(L, key, local)
    wall = walls(P)
    dress(P)
    dress_walls(P, wall)
    # 바닥 결 (기후별 풀·꽃)
    deco = {'sand': (0.030, ['drawn_tuft', 'drawn_pebbles', 'drawn_tuft2']), 'dune': (0.010, ['drawn_pebbles']),
            'red': (0.020, ['drawn_pebbles', 'drawn_tuft']),
            'dry': (0.055, ['drawn_dryTuft', 'drawn_tuft', 'drawn_dryTuft', 'drawn_tuft2', 'drawn_pebbles']),
            'grass': (0.070, ['drawn_grassTuft', 'drawn_grassTuft2', 'drawn_grassTuft', 'drawn_wildflowerY',
                              'drawn_wildflowerW', 'drawn_wildflowerB', 'drawn_wildflowerP'])}
    sparse = {'dry': 'drawn_acaciaRound', 'grass': 'drawn_oasisTree', 'sand': 'drawn_sage', 'red': 'drawn_redBoulderSmall'}
    for ly in range(1, m.h - 1):
        for lx in range(1, m.w - 1):
            x, y = lx + P.ox, ly + P.oy
            z = m.zone[ly][lx]
            if z not in deco or tri_e(x, y) < 3 or m.road[ly][lx] or m.obj[ly][lx] or m.high[ly][lx] or m.deco[ly][lx] or m.blocked[ly][lx]:
                continue
            if _h('tr', x, y) < 0.007 and z in sparse and not m.reserved[ly][lx]:
                A.place(m, sparse[z], lx, ly, foot=1)
                continue
            p, names = deco[z]
            if _h('dc', x, y) < p:
                A.place(m, names[int(_h('dn', x, y) * len(names))], lx, ly, layer='deco')
    m.finish_arabia()
    # 삼각형 바깥: 화면에 보이는 띠만 바닥을 남기고 그 너머는 비운다. 안쪽 2칸·바깥 3칸 띠는 막는다.
    BLOCK = 3
    for ly in range(m.h):
        for lx in range(m.w):
            e = tri_e(lx + P.ox, ly + P.oy)
            if e >= 2:
                continue
            if e >= -BLOCK:
                m.blocked[ly][lx] = True
                continue
            if -e > VIEW:
                m.ground[ly][lx] = m.deco[ly][lx] = m.obj[ly][lx] = m.high[ly][lx] = None
                m.road[ly][lx] = 0
            m.blocked[ly][lx] = False
    return m


def area_report():
    """조각별 크기와 삼각형 안 칸 수 (회의: 약 2만 5천 칸)."""
    out, total = [], 0
    for mid, (x0, y0, w, h, name) in PIECES.items():
        n = sum(1 for y in range(y0, y0 + h) for x in range(x0, x0 + w) if tri_e(x, y) >= 0)
        total += n
        out.append((mid, name, w, h, n))
    return out, total
