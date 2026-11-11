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
    bazaar  만국 장터 대광장  (x112..207, y56..111)   중간 지점 (축제 광장)
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
EXITS = {   # 2026-10-27 오픈월드: 조각 사이는 이음새(seam)로 자동 연결. 여기엔 아라비아 지역과 맞닿는 문만.
    ('arabia_central_dunes', 's_01'): 158, ('arabia_central_dunes', 's_02'): 100,
    ('arabia_central_canyon', 's_01'): 214,     # 자비드 북쪽 협곡길 (2026-10-27 오픈월드 지역)
}
OUTER_EXITS = set(EXITS)   # 아라비아 지역과 맞닿는 출구


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
    if y >= 100 + 16 * vnoise(x, 0, 13, 'rt') + 4 * vnoise(x, y, 5, 'rt2') and x >= 176 + 22 * (vnoise(x, y, 14, 'rb') - 0.5) + 6 * (vnoise(x, y, 4, 'rb2') - 0.5):
        return 'red'                                  # 붉은 바위 협곡 (사막과 들쭉날쭉하게 섞임)
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


# 11/11 만국 장터 시각 개선: 장터 조각만 광장 바닥(plaza)을 큰 사암 판석(slab)으로, 회색 자갈(cobble)을 따뜻한 사암 연석(curb)으로.
#   다른 맵이 쓰는 plaza·cobble 타일 그림은 그대로 두고 장터 전용 바닥 종류를 쓴다 (arabia_art.TEX). 통행(막힘·길 등급)은 바뀌지 않는다.
BAZAAR_FLOOR = {'plaza': 'slab', 'cobble': 'curb'}


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
        kind = BAZAAR_FLOOR.get(kind, kind) if self.mid == 'arabia_central_bazaar' else kind
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

    def patrol(self, name, waypoints, *lines):
        x, y = waypoints[0]
        if self.has(x, y) and all(self.has(px, py) for px, py in waypoints):
            self.m.patrol(name, [self.lc(px, py) for px, py in waypoints], *lines)

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
    P.curve('road', [(215.5, 159), (214, 152), (209, 145), (206, 139)], 3, 2)                  # 자비드 북쪽 협곡길 → 아랫길
    P.curve('gravel', [(226, 120), (244, 128), (258, 134), (266, 136)], 2, 1)                # 협곡 마을 터
    P.ellipse('brick', 265, 135, 8, 3.2)
    P.curve('gravel', [(210, 129), (204, 136), (199, 141)], 1, 1)                             # 아치 전망대 샛길

    # ---- 서쪽 포도밭 언덕 (westvale) ----
    P.curve('road', [(85.5, 111), (83, 100), (88, 90), (100, 84), (112, 80.5)], 3, 2)
    P.curve('road', [(83, 100), (72, 103), (64, 104)], 2, 1)                  # 베네치아 쪽 구역으로 (합칠 때)
    P.curve('road', [(100, 84), (101, 75), (106, 65), (109.5, 57), (109.5, 55)], 2, 1)    # 숨은 고갯길 → 초원
    P.ellipse('water', 92, 70, 4, 2.4)

    # ---- 만국 장터 대광장 (bazaar) ----
    P.curve('paving', [(141.5, 111), (142, 102), (150, 95), (154, 92)], 4, 3)        # 남문 → 광장
    P.curve('paving', [(187.5, 111), (184, 101), (176, 94), (170, 91)], 3, 3)        # 남동문 → 광장
    # 북서: 축제 마당 (2026-11-11: 거울 연못 정원 → 축제 천막·점술·열기구. 북동 천막 거리에서 옮겨 옴)
    P.rect('dry', 114, 59, 145, 77, reserve=False)
    P.rect('cobble', 114, 65, 145, 66, grade=2)                                        # 천막 줄 사이 골목
    P.rect('cobble', 114, 73, 145, 74, grade=2)                                        # 노점 줄 앞 골목
    P.rect('plaza', 114, 63, 115, 78, grade=2)                                         # 서쪽 끝 → 서문 거리
    P.rect('plaza', 144, 63, 145, 78, grade=2)                                         # 동쪽 끝 → 광장
    # 가운데: 별빛 대광장 (자갈 테두리 + 석판 바닥)
    P.ellipse('cobble', 160, 82, 19, 12, grade=3)
    P.ellipse('plaza', 160, 82, 17, 10.5, grade=3)
    P.rect('mosaic', 151, 76, 168, 87, grade=3)                                        # 11/11 분수 바닥 문양 (18×12칸, 중심 160,82 = 분수 수반 바닥)
    P.rect('plaza', 158, 56, 161, 72, grade=3)                                         # 북문 대로 → 한가운데 초원
    P.rect('cobble', 112, 79, 142, 81, grade=3)                                        # 서문 거리
    P.rect('road', 176, 86, 207, 88, grade=3)                                          # 동문 거리 = 야시장
    # 북동: 축제 천막 거리
    P.rect('cobble', 176, 70, 205, 72, grade=2)                                        # 축제 골목
    P.rect('cobble', 176, 62, 177, 86, grade=2)                                        # 광장 동쪽 → 축제 골목 · 야시장
    P.rect('dry', 178, 60, 206, 69, reserve=False)
    # 남쪽 가운데: 만국 상인회 광장 (2026-11-11: 공연 마당 + 조합 회관 자리를 합쳐 넓힘. NetGuild 상인회 마당이 여기 선다)
    #   가장자리 두 줄(y110~111)은 건드리지 않는다: 이음새(seam) 번호가 바뀌면 이웃 맵·NetGuild 도착 자리가 어긋난다
    #   11/11 오후 2차: 회관이 길을 막지 않게. 대광장 → 남쪽 끝 큰길(x157~163)은 곧게 비우고,
    #   상인회 마당은 큰길 서쪽 안뜰(x137~156 × 92~106)로. 회관은 안뜰 위쪽, 문이 안뜰을 본다.
    #   11/11 3차: 안뜰을 서쪽으로 넓힘 (20칸 → 28칸 폭) — 네 귀퉁이에 춤·놀이판·사진관·노점
    P.rect('cobble', 129, 92, 156, 107, grade=2)                                       # 상인회 안뜰 테두리
    P.rect('plaza', 130, 93, 155, 106, grade=3)
    P.rect('plaza', 156, 97, 156, 103, grade=3)                                        # 큰길 쪽 안뜰 어귀 (담 없이 열어 둠)
    P.rect('plaza', 157, 92, 163, 109, grade=3)                                        # 대광장 → 남쪽 끝 큰길 (가장자리 두 줄 앞에서 멈춤)
    P.rect('cobble', 164, 92, 164, 109, grade=2)
    # 남서: 상인 야영지
    P.ellipse('dry', 125, 97, 11, 8, reserve=False)
    P.curve('road', [(131, 82), (132, 92), (130, 101), (124, 107)], 2, 1)
    # 큰 건물 앞마당 (2026-10-27 저녁): 만국 백화점 · 큰 상점 둘 (조합 회관은 11/11 상인회 광장으로)
    P.rect('plaza', 128, 100, 136, 101, grade=2)                                       # 야영지 → 상인회 안뜰
    P.rect('plaza', 192, 96, 205, 97, grade=2)                                         # 백화점 앞
    P.rect('plaza', 181, 97, 193, 98, grade=2)                                         # 백화점 앞 → 남동문 길
    P.rect('plaza', 187, 106, 205, 107, grade=2)                                       # 큰 상점 앞 → 남동문 길

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
            edge_d = FH - 1 - y                         # 오픈월드: 조각 사이는 열고, 아라비아 지역과 맞닿는 밑변만 띠
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
    # 11/08 (기우) 오아시스가 휑해서: 쉬는 낙타·물항아리·줄무늬 천막·관목·바위로 대상 쉼터 느낌
    for name, x, y, kw in [
        ('drawn_camelRest', 103, 140, dict(foot=1)), ('drawn_camelRest', 121, 143, dict(foot=1)),
        ('drawn_tentStripe', 118, 136, dict(foot=2)),
        ('drawn_jar', 103, 144, dict(foot=1)), ('drawn_jarSmall', 104, 145, dict(foot=1)),
        ('drawn_acaciaRound', 98, 145, dict()), ('drawn_acaciaRound', 127, 143, dict()),
        ('nature_palmYoung', 116, 146, dict()), ('nature_palmYoung', 107, 136, dict()),
        ('drawn_rocks', 95, 141, dict()), ('drawn_boulderSmall', 128, 147, dict()),
        ('drawn_sage', 100, 139, dict(foot=0)), ('drawn_sage', 125, 139, dict(foot=0)), ('drawn_tuft', 103, 149, dict(foot=0)),
        ('drawn_tuft2', 120, 148, dict(foot=0)), ('drawn_tuft', 96, 142, dict(foot=0)), ('drawn_pebbles', 110, 145, dict(foot=0)),
        ('drawn_pebbles', 117, 145, dict(foot=0)),
        # 오아시스 둘레 더: 야자 셋, 서 있는 낙타, 천막 옆 등불·항아리
        ('drawn_datePalm3', 101, 148, dict()), ('drawn_datePalm2', 124, 140, dict()), ('drawn_datePalm', 112, 148, dict()),
        ('drawn_camel', 124, 145, dict(foot=1)), ('prop_lantern', 121, 137, dict(foot=1)), ('drawn_jar', 117, 139, dict(foot=1)),
        ('building_tent', 96, 138, dict(foot=2)),
        # 사막 전체: 길가 바위·마른 풀, 서쪽에 말라붙은 우물 터(돌담)
        ('drawn_boulder', 30, 147, dict()), ('drawn_boulder', 55, 142, dict()), ('drawn_boulder', 78, 150, dict()), ('drawn_boulder', 92, 128, dict()),
        ('drawn_boulder', 135, 147, dict()), ('drawn_boulder', 140, 128, dict()), ('drawn_boulder', 110, 121, dict()), ('drawn_boulder', 125, 149, dict()),
        ('drawn_boulderSmall', 85, 148, dict()), ('drawn_boulderSmall', 60, 148, dict()), ('drawn_boulderSmall', 100, 125, dict()), ('drawn_boulderSmall', 145, 149, dict()),
        ('drawn_rocks', 36, 148, dict()), ('drawn_rocks', 118, 128, dict()), ('drawn_rocks', 155, 142, dict()), ('drawn_rocks', 72, 147, dict()),
        ('drawn_rockWall', 57, 147, dict()), ('drawn_rockWall', 64, 146, dict()), ('drawn_well', 66, 149, dict(foot=1)),
        ('drawn_sage', 40, 148, dict(foot=0)), ('drawn_sage', 75, 146, dict(foot=0)), ('drawn_sage', 95, 149, dict(foot=0)), ('drawn_sage', 130, 134, dict(foot=0)),
        ('drawn_sage', 158, 135, dict(foot=0)), ('drawn_sage', 105, 118, dict(foot=0)),
        ('drawn_tuft', 27, 149, dict(foot=0)), ('drawn_tuft', 50, 150, dict(foot=0)), ('drawn_tuft', 88, 140, dict(foot=0)), ('drawn_tuft', 120, 148, dict(foot=0)),
        ('drawn_tuft2', 33, 150, dict(foot=0)), ('drawn_tuft2', 68, 144, dict(foot=0)), ('drawn_tuft2', 137, 140, dict(foot=0)), ('drawn_tuft2', 150, 123, dict(foot=0)),
        ('drawn_tuft2', 98, 150, dict(foot=0)), ('drawn_tuft', 115, 125, dict(foot=0)),
    ]:
        if P.has(x, y) and not P.put(name, x, y, force=True, **kw):   # 이 맵 창 안인데도 못 놓은 것만 알림
            print('WARN dunes 소품 자리 없음', name, x, y)
    T(119, 139, '대상 길잡이 바심', 'arabia_npc_bedouin_tall',
      '오아시스에서 하룻밤 묵고 새벽에 떠나는 게 사막의 법이지. 낮의 모래바람은 짐을 다 날려 버리거든.',
      '서쪽 돌담은 옛날 우물 터야. 말라붙은 지 오래지만, 대상들은 아직도 저길 보고 길을 잡아.')
    # 11/08 이정표 셋: 오아시스 남쪽 · 마른 우물 터 · 숨은 샘
    P.sign(110, 147, '모래바람 오아시스\n북: 옛 길잡이 탑을 지나 만국 장터 · 동: 와디 따라 붉은 협곡\n서: 마른 우물 터 → 모래 언덕 너머 숨은 샘 · 남: 큰길 옛길')
    P.sign(70, 148, '마른 우물 터\n동: 모래바람 오아시스 (반나절) · 서: 모래 언덕 사이로 숨은 샘\n북서: 서쪽 포도밭 언덕')
    # 11/08 출구마다 이정표: 북서 포도밭 언덕 · 북 만국 장터 · 동 붉은 협곡(위·아래)
    P.sign(66, 115, '북쪽 길 — 서쪽 포도밭 언덕\n이 길로 올라가면 포도밭과 올리브 언덕. 베네치아 쪽 본토로 이어진다\n남동: 모래바람 오아시스')
    P.sign(121, 115, '북쪽 길 — 만국 장터\n와디를 건너 북으로 가면 만국 장터 대광장\n남: 옛 길잡이 탑 · 모래바람 오아시스')
    P.sign(163, 123, '동쪽 와디 길 — 붉은 바위 협곡\n마른 강바닥을 따라가면 붉은 협곡. 낙석 조심\n서: 길잡이 탑 · 오아시스')
    P.sign(160, 148, '동쪽 좁은 틈 — 협곡 아랫길\n바위 틈으로 들어가면 협곡 아래쪽으로 빠진다\n서: 사나 북문 밖 광장')
    P.sign(54, 148, '숨은 샘\n지도에 없는 샘. 동: 마른 우물 터 → 오아시스')
    T(68, 150, '마른 우물의 노인', 'arabia_npc_scholar_tall',
      '이 우물이 마르기 전엔 여기가 쉼터였지. 지금은 돌담만 남았어.',
      '동쪽 오아시스까지는 반나절. 물주머니는 채웠나?')
    P.put('building_tent', 150, 135, foot=2)
    P.put('drawn_camelRest', 154, 137, foot=1)
    T(152, 139, '쉬어 가는 대상', 'arabia_npc_bedouin_tall', '장터로 곧장 가려면 탑을 지나 북쪽으로. 짐이 무거우면 쉬었다 가게.')
    P.sign(104, 153, '큰길 옛길\n남쪽으로 내려가면 아라비아의 안전한 큰길 (낙석을 치워야 지날 수 있음)')
    # 서쪽 숨은 샘
    for x, y in [(41, 144), (50, 143), (43, 149), (49, 149)]:
        P.put(['drawn_datePalm2', 'drawn_datePalm3'][x % 2], x, y, force=True)
    T(45, 150, '숨은 샘의 시인', 'arabia_npc_scholar_tall',
      '여기까지 찾아왔군. 이 샘은 지도에도 없지.',
      '장터의 등불 탑이 보이나? 길을 잃으면 그 탑을 기준으로 삼게.')
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
    # 11/08 (기우) 협곡 채우기: 마을 사람 셋 + 출구 이정표 넷
    T(260, 133, '협곡 마을 아이', 'arabia_npc_child_tall',
      '아치 위에 올라가면 협곡이 다 보여요! 할머니는 위험하다고 하지만요.',
      '밤에는 바위가 식으면서 딱딱 소리가 나요. 무서운 거 아니에요.')
    T(270, 136, '협곡 양치기', 'arabia_npc_bedouin_tall',
      '염소들은 바위 틈 풀을 잘도 찾아 먹지. 나는 그늘만 찾고.',
      '낙석이 길을 막으면 양들이 먼저 알아. 울음소리가 달라지거든.')
    T(268, 141, '붉은 돌 캐는 사람', 'arabia_npc_weaver_tall',
      '이 붉은 돌을 갈면 염료가 돼. 타이즈 염색 장인이 사 가.',
      '협곡 아랫길 바위는 내가 치울 수 있는 크기가 아니야. 수레 있는 사람이 와야지.')
    # 11/08 협곡 마을 생활 소품 (사람 수는 그대로)
    for name, x, y, kw in [
        ('drawn_donkey', 251, 137, dict(foot=1)), ('drawn_jar', 256, 134, dict(foot=1)), ('drawn_jarSmall', 257, 135, dict(foot=1)),
        ('drawn_pottedOrange', 262, 131, dict(foot=1)), ('drawn_pottedOrange', 267, 131, dict(foot=1)),
        ('prop_lantern', 264, 132, dict(foot=1)), ('drawn_crates', 272, 141, dict(foot=1)), ('drawn_jar', 266, 141, dict(foot=1)),
        ('drawn_redBoulderSmall', 254, 138, dict()), ('drawn_redBoulderSmall', 255, 131, dict()),
        ('drawn_sage', 253, 141, dict(foot=0)), ('drawn_sage', 275, 130, dict(foot=0)), ('nature_palmYoung', 250, 132, dict()),
        ('drawn_tuft', 258, 140, dict(foot=0)), ('drawn_tuft2', 271, 132, dict(foot=0)),
    ]:
        if P.has(x, y) and not P.put(name, x, y, force=True, **kw):
            print('WARN canyon 소품 자리 없음', name, x, y)
    P.sign(172, 134, '서쪽 와디 길 — 모래바람 오아시스\n마른 강을 거슬러 서쪽으로 가면 옛 길잡이 탑과 오아시스\n동: 붉은 바위 아치 · 협곡 마을')
    P.sign(172, 153, '좁은 틈 — 모래바람 오아시스 남쪽\n바위 틈을 빠져나가면 사나 북문 밖 광장 쪽\n동: 협곡 아랫길 (낙석)')
    P.sign(205, 115, '북쪽 오르막 — 동쪽 고원\n올라가면 소나무 숲과 고원 마을\n북서: 만국 장터 · 남: 붉은 바위 아치')
    P.sign(216, 156, '남쪽 협곡길 — 자비드\n협곡을 따라 내려가면 자비드 곡물 장터\n북: 협곡 안내인 천막')

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
    """만국 장터 대광장 (2026-10-27 개편, 2026-11-11 재배치): 특정 나라 마을 대신 화려한 축제 광장과 볼거리.
    가운데 3단 대분수와 별빛 바닥 · 북쪽 등불 탑(랜드마크) · 북서 축제 마당(천막·점술·열기구) · 북동 관람차와 놀이 노점
    · 동쪽 야시장 · 남쪽 가운데 만국 상인회 광장(조합 회관·길드 깃발 넷, NetGuild 상인회 마당 자리) · 남서 떠돌이 상인 야영지
    · 남동 만국 백화점(유리 지붕) · 보석 상회 · 여행 장비 상회.
    11/11 3차: 상인회 안뜰 칸 (틀 좌표) 접수처 143,100 · 노점 152,105 · 놀이판 131,105 · 사진 자리 152,99
           · 상인 154,102 / 131,100(춤 양탄자) / 135,103 은 NetGuild가 실행 중에 세우므로 비워 둔다 (NetGuild.cs와 같이 고칠 것).
    회의 기준: 아라비아풍 고급 상점은 3곳만 (비단 상관·향료 궁전·등불 찻집)."""
    T = P.talker
    miss = []

    def put(name, x, y, **kw):
        if not P.put(name, x, y, **kw):
            miss.append((name, x, y))

    def flags(x, y, k=0):                         # 깃발 줄: 사람 위로 그려지고 길을 막지 않는다
        put('drawn_bunting' if k % 2 == 0 else 'drawn_bunting2', x, y, foot=0, force=True)

    # ---- 가운데: 별빛 대광장 · 3단 대분수 · 여행자 동상 넷 ----
    put('drawn_grandFountain', 156, 77, foot=3, force=True)
    P.m.mosaic = ('drawn_floorMedallion', 151 - P.ox, 76 - P.oy)          # 11/11 분수 바닥 문양 (geometry의 mosaic 칸 151~168 × 76~87)
    for (x, y) in [(147, 74), (171, 74)]:                                   # 11/11 넷 → 둘 (광장 둘레를 덜 빽빽하게)
        put('drawn_statue', x, y, foot=1, force=True)
    for (x, y) in [(151, 73), (168, 73), (151, 90), (168, 90), (144, 81), (176, 81)]:
        put('plaza_lamp', x, y - 1, foot=1, force=True)
    for (x, y) in [(147, 83), (171, 83)]:                                   # 분수를 보는 벤치 (11/11 문양 밖 양옆으로)
        put('drawn_benchWood', x, y, foot=1, force=True)
    # 장날 양탄자 노점 (11/11: 분수 문양 밖 광장 양옆으로 한 칸씩 물림. 노점상·구슬 장수 자리는 그대로)
    for (cx, cy, jar, tray) in [(146, 80, (148, 79), (146, 78)), (172, 80, (171, 79), (173, 78))]:
        put('prop_carpet', cx, cy, layer='deco', force=True)
        put('drawn_jar', jar[0], jar[1], foot=1, force=True)
        put('prop_spiceTray', tray[0], tray[1], foot=1, force=True)
    T(150, 79, '양탄자 위의 노점상', 'arabia_npc_weaver_tall', '장날이면 이 광장에 양탄자를 깔고 장사해요. 자리는 해 뜨기 전에 맡아야 해요!')
    T(170, 79, '구슬 장수', 'arabia_woman_girl_tall', '바다 건너 유리구슬, 초원의 뼈 구슬, 사막의 호박… 하나씩 골라 봐요.')
    # 11/11 분수 옆 잔디 화단 둘은 바닥 문양을 가려서 뺌. 대신 광장 가장자리에 쉼터(낮은 화단 + 벤치 + 그늘나무)를 묶어 둔다.
    for (n_, x, y) in [('drawn_planterRose', 147, 87), ('drawn_planterSaffron', 171, 87),
                       ('drawn_planterSaffron', 153, 71), ('drawn_planterRose', 165, 71)]:
        put(n_, x, y, foot=1, force=True)
    for (x, y) in [(147, 89), (171, 89)]:
        put('drawn_benchWood', x, y, foot=1, force=True)
    for (n_, x, y) in [('tree_palmDates', 137, 86), ('tree_palmDates', 182, 89), ('drawn_oasisTree', 138, 83), ('drawn_oasisTree', 178, 77)]:
        put(n_, x, y, foot=1)
    for k, (x, y) in enumerate([(146, 71), (167, 71)]):                     # 11/11 남쪽 둘은 상인회 광장 깃발 줄과 겹쳐서 뺌
        flags(x, y, k)
    T(160, 88, '장터지기 카림', 'arabia_npc_merchant_tall',
      '만국 장터 대광장에 온 걸 환영해요! 닷새마다 큰 장이 서면 네 대륙 상인이 다 모여요.',
      '길을 잃으면 등불 탑을 봐요. 북쪽은 초원 대로, 북서는 축제 마당, 북동은 관람차, 남쪽이 만국 상인회 광장이에요.')
    T(150, 84, '동상 닦는 아이', 'arabia_npc_child_tall',
      '이 동상들은 처음으로 이 장터까지 걸어온 상인들이래요. 먼 동쪽과 서쪽에서 한 명씩!',
      '등불을 든 손을 만지면 길을 잃지 않는대요. 다들 그래서 반들반들해요.')
    P.sign(141, 84, '만국 장터 대광장\n가운데: 대분수 · 북: 등불 탑과 초원 대로 · 북서: 축제 마당(천막·점술·열기구·공연)\n북동: 관람차·놀이 노점 · 동: 야시장 · 남동: 만국 백화점·보석·여행 장비\n남: 만국 상인회 광장(조합 회관·길드 깃발) · 남서: 떠돌이 상인 야영지')
    P.villager(158, 86, 0)
    P.villager(166, 79, 2)
    P.villager(152, 80, 1)
    # ---- 북쪽: 등불 탑 + 아라비아풍 고급 상점 1·2 ----
    put('drawn_lanternTower', 152, 63, foot=1, force=True)                 # 랜드마크: 장터 어디서나 보인다
    put('house_courtyardManor', 146, 64, foot=2, force=True)                           # 비단·양탄자 상관
    put('civic_palace', 163, 63, foot=2)                                   # 황금 향료 궁전
    for (n, x, y) in [('prop_lustre', 146, 69), ('prop_carpetRoll', 149, 69), ('prop_lantern', 163, 69),
                      ('prop_perfume', 166, 69), ('prop_incense', 169, 69)]:
        put(n, x, y, foot=1, force=True)
    T(149, 70, '비단 상인 레일라', 'arabia_woman_perfumer_tall', '사막을 건너온 비단과 양탄자예요. 등불 아래서 보면 더 반짝이죠.')
    T(168, 71, '향료 궁전 주인', 'arabia_npc_merchant_tall', '이 궁전의 향료는 사나에서도 귀해요. 금빛 상자에 담아 드리지요.')
    for k, y in enumerate((57, 61, 65)):                                    # 초원 대로 위 깃발 줄
        flags(157, y, k)
    # ---- 북서: 축제 마당 (11/11 거울 연못 정원 자리. 큰 천막 셋 · 열기구 · 줄무늬 천막 노점 · 깃발 줄) ----
    for k, (n, x) in enumerate([('drawn_pavilion', 118), ('drawn_pavilion2', 124), ('drawn_pavilion3', 130)]):
        put(n, x, 60, foot=2, force=True)
    put('drawn_balloon2', 137, 58, foot=2, force=True)                 # y58~66: 골목(65~66) 위에 바구니가 닿지 않게
    for k, (n, x) in enumerate([('drawn_tentStripe', 117), ('drawn_booth', 122), ('drawn_tentStripe3', 127),
                                ('drawn_booth2', 132), ('drawn_tentStripe2', 137)]):
        put(n, x, 69, foot=1, force=True)
    for k, x in enumerate((116, 123, 130)):
        flags(x, 64, k)                                                     # 137은 열기구 자리
    for k, x in enumerate((116, 123, 130, 137)):
        flags(x, 72, k + 1)
    for (x, y) in [(116, 67), (143, 67), (116, 76), (143, 76)]:
        put('plaza_lamp', x, y - 1, foot=1, force=True)
    for x in (120, 134):
        P.stamp('bench', x, 76, force=True)
    for x in (126, 140):
        put('prop_lantern', x, 77, foot=1, force=True)
    T(127, 67, '불 재주꾼 자이드', 'arabia_npc_sailor_tall', '횃불 세 개! 아니 네 개! 장날 밤엔 다섯 개까지 돌려요!',
      '경품 노점에서 고리 던지기 해 봐요. 잘 던지면 저 인형이 당신 거예요.')
    T(121, 75, '별 읽는 점술가', 'arabia_woman_qayna_tall', '당신 짐 속에서 먼 길 냄새가 나는군요… 다음 장은 북쪽 초원에서 운이 좋겠어요.',
      '별은 장터 시세는 안 알려 줘요. 그건 발로 뛰어야 알지요.')
    T(140, 67, '열기구 사공', 'arabia_npc_bedouin_tall', '바람이 좋은 날엔 이걸 타고 초원 너머까지 내려다본다오.',
      '아직은 줄에 묶어 둔 채라오. 언젠가 하늘길이 열리면 태워 주지.')
    T(124, 75, '피리 부는 악사', 'arabia_npc_scholar_tall', '이 가락은 사막 대상들이 밤마다 부르던 노래요. 초원 사람들은 손뼉으로 박자를 맞추지.')
    T(138, 75, '방울 춤꾼', 'arabia_woman_perfumer_tall', '발목 방울 소리 들려요? 축제 마당 끝까지 들리게 출 거예요!')
    T(134, 67, '떠돌이 곡예사', 'arabia_woman_girl_tall', '우린 장이 서는 곳마다 따라다녀요! 축제 마당에서 공연 보고 가요.',
      '서남쪽 상인 야영지 뒤 덤불을 치우면 서문 거리로 빠지는 지름길이 있어요.')
    T(131, 75, '축제 마당지기', 'arabia_woman_grandma_tall', '장날이면 이 마당에 천막이 가득 서요. 밤엔 등불이 줄줄이 켜지지요.',
      '남쪽 상인회 광장에 가 봤어요? 새내기 상인은 거기서 수첩을 받는대요.')
    # ---- 북동: 관람차 (11/11 남동 끝에서 올림: 장터 어디서나 보이는 두 번째 랜드마크) · 놀이 노점 ----
    put('drawn_ferrisWheel', 192, 60, foot=2, force=True)                  # 192~199 × 60~68, 매표소가 골목(70~72)을 본다
    T(190, 69, '관람차 사공', 'arabia_npc_sailor_tall', '꼭대기에 오르면 등불 탑, 열기구, 저 멀리 사막까지 다 보여요!',
      '한 바퀴 도는 데 노래 한 곡이에요. 장날 밤엔 불꽃이 바로 눈앞에서 터지죠.')
    put('drawn_pavilion3', 181, 62, foot=2, force=True)
    put('drawn_booth', 180, 74, foot=1, force=True)
    put('drawn_booth2', 185, 74, foot=1, force=True)
    put('drawn_tentStripe2', 190, 74, foot=1, force=True)
    for k, x in enumerate((178, 185)):
        flags(x, 69, k)
    # ---- 동쪽: 야시장 (먹거리 노점 · 등불 줄) + 아라비아풍 고급 상점 3 ----
    # 11/11 노점 여섯을 셋씩 두 무리로 묶고 (먹거리 / 공예품), 무리 사이와 뒤에 상자·항아리·자루·직물. 앞 길(86~88)은 비운다.
    for (n, x) in [('stall_bread', 179), ('stall_fruit', 182), ('stall_dates', 185), ('stall_pottery', 192), ('stall_cloth', 195), ('stall_spice', 198)]:
        put(n, x, 83)
    for (n_, x, y) in [('drawn_crates', 188, 83), ('drawn_jar', 190, 83), ('drawn_jarSmall', 191, 84),
                       ('prop_carpetstack', 201, 83), ('drawn_barrel', 203, 83),
                       ('prop_orangecrate', 180, 82), ('prop_grainsacks', 183, 81), ('drawn_barrel', 186, 81),
                       ('prop_watermelons', 192, 81), ('prop_silk', 195, 81), ('prop_qulla', 198, 81)]:
        put(n_, x, y, foot=1, force=True)
    for x in (178, 186):
        put('prop_lantern', x, 89, foot=1)
    for k, x in enumerate((179, 187, 195)):
        flags(x, 86, k + 1)
    T(190, 89, '야시장 꼬치 장수', 'arabia_npc_merchant_tall', '해가 지면 이 골목이 제일 밝아요. 꼬치 하나 들고 구경하고 가요!')
    put('house_windTowerHouse', 188, 91, foot=2)                           # 등불 찻집
    put('prop_lantern', 186, 95, foot=1, force=True)
    put('prop_rugseat', 189, 96, layer='deco', force=True)
    T(190, 98, '등불 찻집 주인', 'arabia_woman_qayna_tall', '사막 장미차 한 잔 하고 가요. 등불이 켜지면 장터가 제일 예뻐요.')
    # ---- 남쪽 가운데: 만국 상인회 안뜰 (11/11 2차. 큰길 서쪽. 회관 · 길드 깃발 넷 · 춤 양탄자.
    #      접수처·노점·놀이판·사진 자리·상인은 NetGuild가 세운다 — NetGuild.cs BuildYard 칸과 같이 고칠 것) ----
    P.rect('cobble', 129, 92, 156, 107, grade=2)                           # 안뜰 바닥을 옛 흙길 위에 다시 깐다 (북서 귀퉁이)
    P.rect('plaza', 130, 93, 155, 106, grade=3)
    P.rect('plaza', 156, 97, 156, 103, grade=3)
    put('drawn_guildHall', 139, 93, foot=2, force=True)                    # 139~146 × 93~98, 문 앞 가운데에 접수처(143,100)
    for n_, x in [('drawn_guildFlagScale', 134), ('drawn_guildFlagWind', 136),   # 회관 양옆 길드 깃발 넷
                  ('drawn_guildFlagCraft', 147), ('drawn_guildFlagKin', 149)]:
        put(n_, x, 94, foot=1, force=True)
    T(138, 101, '상인 조합장 하킴', 'arabia_npc_scholar_tall', '네 대륙 상인이 모두 이 회관에 장부를 맡기지. 신용이 쌓이면 문이 더 넓게 열린다네.',
      '깃발 넷은 상인회의 네 길드라네. 금저울, 바람길, 솜씨, 동무회. 패를 받으면 어느 깃발 아래 설지 골라 보게.')
    # 북서 귀퉁이: 춤 마당 (합동 공연. 비요른이 양탄자 위에 선다)
    for (x, y) in [(130, 99), (132, 99), (130, 101), (132, 101)]:
        put('prop_carpet', x, y, layer='deco', force=True)
    put('prop_lantern', 134, 98, foot=1, force=True)
    put('drawn_planterRose', 130, 97, foot=1, force=True)
    # 남서 귀퉁이: 짐 쌓기 놀이판 (상자·자루를 쌓아 둔 놀이 자리)
    put('drawn_crates', 130, 103, foot=1, force=True)
    put('prop_grainsacks', 133, 105, foot=1, force=True)
    put('plaza_board', 137, 104, foot=1, force=True)                       # 상인회 게시판
    # 북동 귀퉁이: 사진관 (아치 문 배경 + 화분 + 화단)
    put('plaza_gate', 151, 95, foot=1, force=True)
    put('drawn_planterSaffron', 149, 99, foot=1, force=True)
    put('drawn_pottedOrange', 155, 98, foot=1, force=True)
    # 남동 귀퉁이: 상인회 노점 (짐 상자·통)
    put('drawn_crates', 148, 104, foot=1, force=True)
    put('drawn_barrel', 155, 104, foot=1, force=True)
    for k, x in enumerate((136, 144)):                                      # 안뜰 아래쪽 깃발 줄
        flags(x, 107, k)
    for (x, y) in [(156, 95), (156, 104)]:                                  # 큰길 쪽 안뜰 어귀 양쪽 등불
        put('prop_lantern', x, y, foot=1, force=True)
    for k, x in enumerate((146, 165)):                                      # 대광장에서 내려오는 큰길 양옆 깃발 줄
        flags(x, 91, k + 2)
    put('drawn_cart', 126, 94, foot=1, force=True)                         # 안뜰 밖 서쪽: 짐수레
    # 큰길 동쪽: 쉼터 (벤치 · 나무)
    for (x, y) in [(168, 97), (168, 103)]:
        P.stamp('bench', x, y, force=True)
    for (x, y) in [(173, 95), (173, 101)]:
        put('drawn_olive', x, y, foot=1)
    # ---- 남서: 떠돌이 상인 야영지 + 열기구 ----
    for (n, x, y) in [('drawn_tentStripe2', 115, 89), ('drawn_tentStripe', 121, 92), ('drawn_tentStripe3', 115, 97),
                      ('building_tent', 121, 100)]:
        put(n, x, y, foot=1)
    for (n, x, y) in [('drawn_crates', 126, 90), ('drawn_barrel', 127, 96), ('drawn_cart', 116, 104), ('drawn_camelRest', 126, 103)]:
        put(n, x, y, foot=1)
    # (상인 조합 회관과 조합장 하킴은 11/11 남쪽 가운데 만국 상인회 광장으로 옮김)
    barrier(P, [(x, 84) for x in range(113, 130) if x not in (116, 117)], ['drawn_bush', 'drawn_sage', 'drawn_bush', 'drawn_oleanderBush'])
    T(124, 97, '떠돌이 상인 오마르', 'arabia_npc_sailor_tall', '우린 장이 끝나면 다음 장터로 떠나요. 사막 너머, 초원 너머로.',
      '천막 뒤 덤불을 치우면 서문 거리로 빠지는 뒷길이 있지.')
    P.obstacle('thorn_brush', 116, 85, 2, 2, 'tents')
    # ---- 남동: 만국 백화점 · 큰 상점 둘 ----
    put('drawn_grandArcade', 194, 89, foot=2, force=True)
    T(200, 98, '백화점 지배인 셀린', 'arabia_woman_perfumer_tall', '만국 백화점에 어서 오세요! 네 대륙 물건이 한 지붕 아래 다 있어요.',
      '유리 지붕 아래선 비가 와도 장을 볼 수 있지요. 사막엔 비가 잘 안 오지만요.')
    # (관람차는 11/11 북동 높은 자리로 옮김)
    put('drawn_shopJewel', 192, 101, foot=2, force=True)
    put('drawn_shopOutfitter', 199, 101, foot=2, force=True)
    T(195, 108, '보석 상인 야스민', 'arabia_woman_girl_tall', '자수정, 호박, 바다 진주… 상인의 행운은 반짝이는 데서 온대요.')
    T(202, 108, '여행 장비 상인 바쉬르', 'arabia_npc_bedouin_tall', '물주머니, 나침반, 튼튼한 짐끈! 먼 길 가는 상인한테 필요한 건 다 있지.',
      '짐이 무거우면 걸음이 느려져. 좋은 짐끈 하나가 하루를 벌어 준다네.')
    # ---- 길가 나무 ----
    for (x, y) in [(179, 92), (170, 108), (150, 108)]:                    # 11/11 (138,104)·(146,99)는 원래도 못 놓이던 자리라 뺌
        put(['drawn_oasisTree', 'drawn_olive', 'drawn_cypress'][x % 3], x, y)
    # ---- 경비병 순찰: 광장 둘레 · 초원 대로 → 축제 거리 → 야시장 ----
    P.patrol('대광장 경비병', [(143, 82), (150, 73), (160, 71), (170, 73), (177, 82), (170, 91), (160, 93), (150, 91)],
             '대광장은 우리가 지키오. 장날엔 소매치기가 늘어나니 짐 단단히 챙기시오.',
             '밤에 불꽃이 오르면 다들 하늘만 보지. 그때가 제일 바쁘다오.')
    P.patrol('장터 경비대장', [(159, 60), (159, 70), (176, 71), (195, 71), (176, 72), (176, 86), (200, 87), (177, 87)],
             '초원 대로부터 야시장까지가 내 순찰길이오. 길 잃은 상인은 등불 탑으로 데려다 주지.',
             '조합 회관에서 수상한 장부가 돌면 알려 주시오.')
    P.m.bazaar_missing = miss


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


# =====================================================================
#  5. 오픈월드 이음새: 조각 경계에서 양쪽 다 걸을 수 있는 곳은 모두 출구로 (팀 고려 continuous 방식)
# =====================================================================
_CACHE = {}
SHORT = {'arabia_central_dunes': 'dunes', 'arabia_central_canyon': 'canyon', 'arabia_central_westvale': 'west',
         'arabia_central_bazaar': 'bazaar', 'arabia_central_eastvale': 'east', 'arabia_central_steppe': 'steppe'}
BLOCKING = {'npc', 'market', 'talker', 'villager', 'sign', 'board', 'exit', 'spawn'}


def _walk_grid(m):
    occ = {(o['x'] + dx, o['y'] + dy) for o in m.objects if o['kind'] in BLOCKING
           for dx in range(o.get('w', 1)) for dy in range(o.get('h', 1))}
    return lambda x, y: (0 <= x < m.w and 0 <= y < m.h and m.ground[y][x] and not m.blocked[y][x]
                         and not m.water[y][x] and (x, y) not in occ)


def _main_area(m, walk):
    """맵에서 가장 큰 걸을 수 있는 덩어리 (이음새가 갇힌 구석으로 이어지지 않게)."""
    from collections import deque
    seen, best = set(), set()
    for y in range(m.h):
        for x in range(m.w):
            if (x, y) in seen or not walk(x, y):
                continue
            comp, q = {(x, y)}, deque([(x, y)])
            seen.add((x, y))
            while q:
                cx, cy = q.popleft()
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if (nx, ny) not in seen and walk(nx, ny):
                        seen.add((nx, ny)); comp.add((nx, ny)); q.append((nx, ny))
            if len(comp) > len(best):
                best = comp
    return best


def add_seams(maps):
    info = {}
    for mid, m in maps.items():
        walk = _walk_grid(m)
        info[mid] = (walk, _main_area(m, walk))
    count = 0
    ids = list(maps)
    for ai in ids:
        for bi in ids:
            ax, ay, aw, ah, _ = PIECES[ai]
            bx, by, bw, bh, _ = PIECES[bi]
            if ax + aw == bx and max(ay, by) < min(ay + ah, by + bh):          # A 동쪽 = B 서쪽
                edge, other, span = 'E', 'W', range(max(ay, by), min(ay + ah, by + bh))
                cells = lambda t: (((aw - 1, t - ay), (aw - 2, t - ay)), ((0, t - by), (1, t - by)))
                start = lambda t, o: t - o
                oa, ob = ay, by
            elif ay + ah == by and max(ax, bx) < min(ax + aw, bx + bw):        # A 남쪽 = B 북쪽
                edge, other, span = 'S', 'N', range(max(ax, bx), min(ax + aw, bx + bw))
                cells = lambda t: (((t - ax, ah - 1), (t - ax, ah - 2)), ((t - bx, 0), (t - bx, 1)))
                oa, ob = ax, bx
            else:
                continue
            (wa, ma), (wb, mb) = info[ai], info[bi]
            ok = [t for t in span if all(c in ma for c in cells(t)[0]) and all(c in mb for c in cells(t)[1])]
            runs, cur = [], []
            for t in ok:
                if cur and t != cur[-1] + 1:
                    runs.append(cur); cur = []
                cur.append(t)
            if cur:
                runs.append(cur)
            for i, run in enumerate(runs):
                sa = f'{ai}_spawn_seam_{SHORT[bi]}_{i}'
                sb = f'{bi}_spawn_seam_{SHORT[ai]}_{i}'
                maps[ai].link(edge, run[0] - oa, len(run), bi, sb, sa)
                maps[bi].link(other, run[0] - ob, len(run), ai, sa, sb)
                count += 1
    return count


def get_piece(mid, links):
    """6조각을 한 번에 짓고 이음새를 이은 뒤 하나를 돌려준다 (같은 실행 안에서는 다시 짓지 않음)."""
    if not _CACHE:
        maps = {m_: build_piece(m_, links(m_)) for m_ in PIECES}
        _CACHE['seams'] = add_seams(maps)
        _CACHE.update(maps)
    return _CACHE[mid]


def area_report():
    """조각별 크기와 삼각형 안 칸 수 (회의: 약 2만 5천 칸)."""
    out, total = [], 0
    for mid, (x0, y0, w, h, name) in PIECES.items():
        n = sum(1 for y in range(y0, y0 + h) for x in range(x0, x0 + w) if tri_e(x, y) >= 0)
        total += n
        out.append((mid, name, w, h, n))
    return out, total
