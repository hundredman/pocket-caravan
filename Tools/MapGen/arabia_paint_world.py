"""중앙 대륙 남쪽 구역용 그림: 만국 장터(베네치아·북유럽·떠돌이 상인), 협곡, 바닥 무늬.

아라비아 그림(arabia_paint)과 같은 화풍: 굵은 윤곽 + 3~4단 명암, 왼쪽 위 빛. 출력은 drawn/ 폴더.
장터는 특정 문화로 통일하지 않는다 (2026-10-26 요청). 고려 풍 건물은 공용 닌자 타일 스탬프를 쓴다.
"""
import math
import random
from PIL import Image
from arabia_paint import (canvas, shadow, save, paint_mask, _mask, _ell, _rect, OUTLINE, TRUNK, GREENS,
                          ground_texture, boulder, STONE, WOOD)

# ---------------- 팔레트 ----------------
TERRACOTTA = [(126, 52, 38, 255), (168, 74, 50, 255), (204, 104, 66, 255), (232, 146, 98, 255)]
SLATE = [(52, 60, 82, 255), (76, 88, 116, 255), (104, 120, 150, 255), (146, 162, 190, 255)]
TIMBER = [(56, 36, 26, 255), (82, 54, 36, 255), (110, 74, 48, 255), (142, 98, 64, 255)]
SHINGLE = [(60, 44, 36, 255), (88, 64, 48, 255), (118, 88, 64, 255), (150, 116, 86, 255)]
TURF = [(52, 92, 46, 255), (76, 122, 58, 255), (104, 152, 72, 255), (140, 184, 96, 255)]
REDROCK = [(112, 50, 36, 255), (156, 74, 48, 255), (194, 104, 66, 255), (226, 146, 100, 255)]
GREYSTONE = [(92, 92, 100, 255), (128, 128, 136, 255), (164, 164, 170, 255), (200, 200, 204, 255)]


def _fill(px, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = c


def _box(px, x0, y0, x1, y1, c, edge=OUTLINE):
    _fill(px, x0, y0, x1, y1, c)
    for x in range(x0, x1 + 1):
        px[x, y0] = edge; px[x, y1] = edge
    for y in range(y0, y1 + 1):
        px[x0, y] = edge; px[x1, y] = edge


def _shade_wall(px, x0, y0, x1, y1, base, rng, grain=0.06):
    """벽면: 위는 밝고 아래로 살짝 어둡게, 잔무늬."""
    for y in range(y0, y1 + 1):
        k = 1.04 - 0.12 * (y - y0) / max(1, y1 - y0)
        for x in range(x0, x1 + 1):
            j = k * (0.94 if rng.random() < grain else 1.0) * (0.92 if x == x1 - 1 else 1.0)
            px[x, y] = tuple(min(255, int(c * j)) for c in base[:3]) + (255,)


def _arch_window(px, x0, y0, w, h, glass, frame=(250, 244, 230, 255), shutters=None):
    """둥근 아치 창 (x0,y0 = 왼쪽 위)."""
    r = w / 2
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            dy = y - (y0 + r)
            inside = (y >= y0 + r) or ((x + 0.5 - (x0 + r)) ** 2 + (dy + 0.5) ** 2 <= r * r)
            if not inside:
                continue
            edge = (x in (x0, x0 + w - 1) or y == y0 + h - 1 or
                    (y < y0 + r and (x + 0.5 - (x0 + r)) ** 2 + (dy + 0.5) ** 2 > (r - 1) ** 2))
            px[x, y] = OUTLINE if edge else glass
    # 창살 + 반사
    mid = x0 + w // 2
    for y in range(y0 + 2, y0 + h - 1):
        if px[mid, y] != OUTLINE:
            px[mid, y] = frame
    for y in range(y0 + int(r) + 1, y0 + int(r) + 2):
        for x in range(x0 + 1, x0 + w - 1):
            if px[x, y] != OUTLINE:
                px[x, y] = frame
    if w > 3:
        px[x0 + 1, y0 + int(r)] = (255, 255, 255, 255)
    for x in range(x0 - 1, x0 + w + 1):          # 창턱
        px[x, y0 + h] = frame
    if shutters:
        for y in range(y0 + 2, y0 + h):
            px[x0 - 2, y] = shutters; px[x0 + w + 1, y] = shutters
            px[x0 - 3, y] = OUTLINE; px[x0 + w + 2, y] = OUTLINE


def _rect_window(px, x0, y0, w, h, glass=(250, 214, 120, 255)):
    _box(px, x0, y0, x0 + w - 1, y0 + h - 1, glass)
    for y in range(y0 + 1, y0 + h - 1):
        px[x0 + w // 2, y] = TIMBER[0]


def _roof_tiles(px, x0, x1, y0, y1, pal, rng, inset=4, rows=3):
    """앞으로 기운 기와 지붕 (사다리꼴). 위가 inset만큼 좁다. 줄마다 기와 이음."""
    h = y1 - y0
    for y in range(y0, y1 + 1):
        t = (y - y0) / max(1, h)
        a = x0 + int(round(inset * (1 - t)))
        b = x1 - int(round(inset * (1 - t)))
        for x in range(a, b + 1):
            if x in (a, b) or y in (y0, y1):
                px[x, y] = OUTLINE
                continue
            row = (y - y0) % rows
            k = 3 if row == 0 else 2 if row == 1 else 1
            if (x + (y // rows) * 2) % 5 == 0 and row != 0:
                k = 0
            if rng.random() < 0.04:
                k = max(0, k - 1)
            px[x, y] = pal[k]


def _gable_roof(px, x0, x1, ytop, yeave, pal, rng, plank=True):
    """뾰족 박공 지붕 (정면에서 본 삼각형 + 지붕면 판자결)."""
    cx = (x0 + x1) / 2
    for y in range(ytop, yeave + 1):
        t = (y - ytop) / max(1, yeave - ytop)
        half = (x1 - x0) / 2 * t
        a, b = int(round(cx - half)), int(round(cx + half))
        for x in range(a, b + 1):
            if x in (a, b) or y == yeave:
                px[x, y] = OUTLINE
            else:
                k = 2 if x < cx else 1
                if plank and (y - ytop) % 3 == 0:
                    k = max(0, k - 1)
                if rng.random() < 0.03:
                    k = 3
                px[x, y] = pal[k]


def _door(px, x0, y0, w, h, col, arch=True, knob=(236, 196, 80, 255)):
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            if arch and y < y0 + w // 2 and (x + 0.5 - (x0 + w / 2)) ** 2 + (y + 0.5 - (y0 + w / 2)) ** 2 > (w / 2) ** 2:
                continue
            edge = x in (x0, x0 + w - 1) or y == y0 + h - 1 or (arch and y < y0 + w // 2 and (x + 0.5 - (x0 + w / 2)) ** 2 + (y + 0.5 - (y0 + w / 2)) ** 2 > (w / 2 - 1) ** 2) or (not arch and y == y0)
            px[x, y] = OUTLINE if edge else (col if (x - x0) % 3 else tuple(int(c * 0.82) for c in col[:3]) + (255,))
    px[x0 + w - 3, y0 + h // 2 + 1] = knob


def _awning(px, x0, x1, y0, depth, c1, c2):
    for y in range(y0, y0 + depth):
        for x in range(x0, x1 + 1):
            px[x, y] = OUTLINE if y == y0 else (c1 if ((x - x0) // 3) % 2 == 0 else c2)
    for x in range(x0, x1 + 1):                   # 물결 끝단
        if ((x - x0) % 3) != 1:
            px[x, y0 + depth] = c1 if ((x - x0) // 3) % 2 == 0 else c2
        px[x, y0 + depth + (0 if ((x - x0) % 3) != 1 else -1)] = px[x, y0 + depth] if ((x - x0) % 3) != 1 else OUTLINE


def _flowerbox(px, x0, x1, y0):
    for x in range(x0, x1 + 1):
        px[x, y0 + 1] = TIMBER[2] if x not in (x0, x1) else OUTLINE
        px[x, y0 + 2] = OUTLINE
        px[x, y0] = [(226, 64, 70, 255), (70, 140, 60, 255), (250, 220, 90, 255), (70, 140, 60, 255)][(x - x0) % 4]


# ---------------- 베네치아 풍 ----------------
def ven_glass(name, seed=201, wall=(232, 170, 140, 255), awn=((40, 140, 140, 255), (244, 240, 228, 255))):
    """유리 공방 상점 (64×64): 테라코타 지붕, 큰 아치 진열창에 색유리, 줄무늬 차양, 꽃상자."""
    rng = random.Random(seed)
    w, h = 64, 64
    im = canvas(w, h)
    shadow(im, 32, h - 2, 30, 2)
    px = im.load()
    _shade_wall(px, 3, 24, 60, h - 4, wall, rng)
    _box(px, 2, 24, 61, h - 4, (0, 0, 0, 0)) if False else None
    for y in range(24, h - 3):
        px[2, y] = OUTLINE; px[61, y] = OUTLINE
    for x in range(2, 62):
        px[x, h - 4] = OUTLINE
    _roof_tiles(px, 0, 63, 6, 25, TERRACOTTA, rng, inset=5)
    _fill(px, 46, 0, 51, 8, (196, 92, 64, 255))        # 굴뚝
    _box(px, 45, 0, 52, 3, (150, 66, 44, 255))
    for x in range(46, 52):
        px[x, 9] = OUTLINE
    for x in (46, 51):
        for y in range(4, 9):
            px[x, y] = OUTLINE
    glass = [(70, 130, 210, 255), (90, 190, 150, 255), (236, 170, 60, 255)]
    for i, x0 in enumerate((7, 43)):
        _arch_window(px, x0, 33, 12, 18, glass[i])
        for k in range(5):                             # 진열된 유리병
            gx, gy = x0 + 2 + k * 2, 46
            px[gx, gy] = glass[(k + i + 1) % 3]; px[gx, gy - 1] = glass[(k + i + 1) % 3]
    _door(px, 26, 38, 12, 22, (54, 110, 86, 255))
    _awning(px, 5, 58, 28, 4, *awn)
    _flowerbox(px, 6, 20, 52)
    _flowerbox(px, 42, 56, 52)
    # 간판: 유리 꽃병
    _box(px, 27, 33, 36, 36, (250, 236, 196, 255))
    px[31, 34] = (70, 130, 210, 255); px[32, 34] = (70, 130, 210, 255); px[31, 35] = (90, 190, 150, 255); px[32, 35] = (90, 190, 150, 255)
    return save(name, im)


def ven_house(name, seed=202, wall=(236, 198, 124, 255), shutter=(60, 120, 80, 255)):
    """베네치아 2층 집 (48×64): 오커 벽, 아치 창 + 초록 덧창, 작은 발코니, 깔때기 굴뚝."""
    rng = random.Random(seed)
    w, h = 48, 64
    im = canvas(w, h)
    shadow(im, 24, h - 2, 22, 2)
    px = im.load()
    _shade_wall(px, 3, 20, 44, h - 4, wall, rng)
    for y in range(20, h - 3):
        px[2, y] = OUTLINE; px[45, y] = OUTLINE
    for x in range(2, 46):
        px[x, h - 4] = OUTLINE
    _roof_tiles(px, 0, 47, 4, 21, TERRACOTTA, rng, inset=4)
    _fill(px, 34, 0, 39, 5, (210, 110, 78, 255))       # 깔때기 굴뚝
    for x in range(33, 41):
        px[x, 0] = OUTLINE
    for y in range(0, 6):
        px[33, y] = OUTLINE; px[40, y] = OUTLINE
    for x0 in (9, 30):
        _arch_window(px, x0, 25, 8, 11, (90, 140, 190, 255), shutters=shutter)
    for x in range(12, 36):                             # 발코니
        px[x, 38] = OUTLINE
        px[x, 41] = OUTLINE if x in (12, 35) or x % 3 == 0 else (240, 232, 214, 255)
    for x in range(13, 35):
        px[x, 39] = (226, 64, 70, 255) if x % 4 == 0 else (70, 140, 60, 255)
    _door(px, 18, 44, 11, 16, (110, 62, 40, 255))
    _arch_window(px, 6, 46, 6, 9, (90, 140, 190, 255))
    _arch_window(px, 36, 46, 6, 9, (90, 140, 190, 255))
    return save(name, im)


def clock_tower(name, seed=203):
    """시계탑 랜드마크 (48×112): 회색 돌탑, 흰 시계판, 종 창, 뾰족한 청석 지붕과 깃발."""
    rng = random.Random(seed)
    w, h = 48, 112
    im = canvas(w, h)
    shadow(im, 24, h - 3, 20, 2.5)
    px = im.load()
    x0, x1 = 10, 37
    for y in range(30, h - 4):                          # 돌벽 (벽돌 무늬)
        for x in range(x0, x1 + 1):
            if x in (x0, x1):
                px[x, y] = OUTLINE
                continue
            row = (y - 30) // 5
            seam = (y - 30) % 5 == 0 or (x + (row % 2) * 4) % 8 == 0
            base = GREYSTONE[2] if x < x1 - 6 else GREYSTONE[1]
            if rng.random() < 0.05:
                base = GREYSTONE[3]
            px[x, y] = GREYSTONE[0] if seam else base
    for x in range(x0 - 2, x1 + 3):                     # 받침 띠
        for y in (h - 6, h - 5, h - 4):
            px[x, y] = OUTLINE if y in (h - 6, h - 4) or x in (x0 - 2, x1 + 2) else GREYSTONE[3]
    for y in (60, 61):
        for x in range(x0 - 1, x1 + 2):
            px[x, y] = OUTLINE if y == 61 or x in (x0 - 1, x1 + 1) else GREYSTONE[3]
    # 시계판
    cx, cy, r = 23.5, 46.5, 8.5
    for y in range(36, 58):
        for x in range(12, 36):
            d = ((x + .5 - cx) ** 2 + (y + .5 - cy) ** 2) ** 0.5
            if d <= r:
                px[x, y] = OUTLINE if d > r - 1.2 else (250, 246, 232, 255) if d > r - 2.2 or d < r - 3 else (210, 180, 90, 255)
    for k in range(12):
        import math
        a = k / 12 * 6.283
        px[int(cx + math.cos(a) * (r - 2.6)), int(cy + math.sin(a) * (r - 2.6))] = OUTLINE
    for t in range(0, 6):                               # 바늘
        px[23, 46 - t] = OUTLINE
    for t in range(0, 5):
        px[23 + t, 46] = OUTLINE
    # 종 창
    _arch_window(px, 19, 66, 10, 12, (40, 40, 52, 255), frame=(40, 40, 52, 255))
    _fill(px, 21, 72, 26, 76, (220, 176, 70, 255))
    _door(px, 18, 90, 12, 17, (96, 56, 36, 255))
    # 뾰족 지붕 + 깃발
    _gable_roof(px, 6, 41, 6, 31, SLATE, rng)
    for y in range(0, 8):
        px[23, y] = OUTLINE
    for y in range(0, 4):
        for x in range(24, 31 - y):
            px[x, y] = (210, 50, 56, 255) if y < 2 else (236, 196, 80, 255)
    return save(name, im)


def gondola(name, seed=204):
    """곤돌라 (48×16)."""
    im = canvas(48, 16)
    px = im.load()
    for x in range(4, 44):
        t = abs(x - 24) / 20
        top = 7 + int(2 * (1 - t * t))
        for y in range(top, 13):
            px[x, y] = OUTLINE if y in (top, 12) or x in (4, 43) else (34, 32, 40, 255) if y > top + 1 else (70, 64, 78, 255)
    for y in range(2, 8):                               # 뱃머리 장식
        px[42 + (8 - y) // 3, y] = OUTLINE; px[43 + (8 - y) // 3, y] = (220, 210, 190, 255)
    for x in range(20, 28):                             # 빨간 방석
        px[x, 8] = (190, 50, 56, 255); px[x, 7] = OUTLINE
    return save('drawn_gondola' if name is None else name, im)


# ---------------- 북유럽 풍 ----------------
def longhouse(name, seed=205, shields=((180, 50, 50, 255), (50, 90, 170, 255), (236, 196, 80, 255))):
    """북유럽 통나무 장원 (80×64): 가파른 널판 지붕 + 교차 박공(용머리), 세로 통나무 벽, 둥근 방패."""
    rng = random.Random(seed)
    w, h = 80, 64
    im = canvas(w, h)
    shadow(im, 40, h - 2, 38, 2)
    px = im.load()
    for y in range(30, h - 4):                          # 세로 판자 벽
        for x in range(4, 76):
            if x in (4, 75):
                px[x, y] = OUTLINE
                continue
            k = 2 if (x // 4) % 2 else 1
            if x % 4 == 0:
                k = 0
            if rng.random() < 0.04:
                k = 3
            px[x, y] = TIMBER[k]
    for x in range(4, 76):
        px[x, h - 4] = OUTLINE
        px[x, 30] = OUTLINE
    # 지붕: 넓은 사다리꼴 널판 (가파름)
    _roof_tiles(px, 0, 79, 8, 31, SHINGLE, rng, inset=10, rows=3)
    # 박공 교차 기둥 (용머리)
    for t in range(12):
        for (sx, d) in ((30, 1), (49, -1)):
            x, y = sx + d * t, 9 - t // 2 if t < 12 else 0
            if 0 <= y < h:
                px[x, y] = OUTLINE
                px[x, y + 1] = TIMBER[3]
    px[19, 3] = (236, 196, 80, 255); px[60, 3] = (236, 196, 80, 255)
    for i, x in enumerate((10, 24, 52, 64)):            # 둥근 방패
        c = shields[i % 3]
        for y in range(36, 46):
            for xx in range(x, x + 10):
                d = ((xx + .5 - x - 5) ** 2 + (y + .5 - 41) ** 2) ** 0.5
                if d <= 5:
                    px[xx, y] = OUTLINE if d > 4 else (220, 220, 214, 255) if d < 1.3 else c if (xx + y) % 2 or d < 3 else tuple(int(v * .8) for v in c[:3]) + (255,)
    _door(px, 34, 40, 12, 20, (66, 44, 30, 255), arch=False, knob=(170, 170, 170, 255))
    for y in (44, 50, 56):                              # 쇠띠
        for x in range(35, 45):
            px[x, y] = (60, 60, 64, 255)
    _rect_window(px, 22, 50, 6, 5)
    _rect_window(px, 52, 50, 6, 5)
    return save(name, im)


def nord_shop(name, seed=206):
    """북유럽 노점 (48×48): 잔디 지붕 + 기둥, 매단 생선·모피."""
    rng = random.Random(seed)
    w, h = 48, 48
    im = canvas(w, h)
    shadow(im, 24, h - 2, 22, 2)
    px = im.load()
    for x in (5, 6, 41, 42):                            # 기둥
        for y in range(16, h - 4):
            px[x, y] = OUTLINE if x in (5, 42) else TIMBER[2]
    _fill(px, 5, 34, 42, h - 4, TIMBER[1])              # 판매대
    _box(px, 4, 34, 43, 38, TIMBER[2])
    for x in range(8, 40, 3):                           # 판매대 위 생선·버섯·치즈
        px[x, 33] = [(200, 200, 210, 255), (236, 196, 80, 255), (160, 100, 70, 255)][(x // 3) % 3]
        px[x + 1, 33] = OUTLINE
    m = _mask(w, h)                                     # 잔디 지붕
    for y in range(4, 18):
        half = 12 + (y - 4) * 0.9
        for x in range(int(24 - half), int(24 + half)):
            if 0 <= x < w:
                m[y][x] = True
    paint_mask(im, m, TURF, rng, speck=0.12)
    for x in range(10, 38, 6):                          # 매단 생선
        for y in range(19, 27):
            px[x, y] = OUTLINE if y == 19 else (176, 186, 200, 255) if y < 25 else OUTLINE
        px[x - 1, 25] = (176, 186, 200, 255); px[x + 1, 25] = (176, 186, 200, 255)
    for x in range(22, 28):                             # 모피
        for y in range(19, 29):
            px[x, y] = OUTLINE if x in (22, 27) or y == 28 else (150, 112, 76, 255) if (x + y) % 3 else (120, 86, 58, 255)
    return save(name, im)


def runestone(name, seed=207):
    """룬 돌 (16×32): 회색 돌판 + 붉은 룬 띠."""
    rng = random.Random(seed)
    im = canvas(16, 32)
    shadow(im, 8, 29, 7, 1.8)
    m = _mask(16, 32)
    _ell(m, 8, 9, 6, 7)
    _rect(m, 2, 9, 13, 28)
    paint_mask(im, m, GREYSTONE, rng, speck=0.1)
    px = im.load()
    for y in range(6, 27):
        x = 5 + int(3 * ((y - 6) / 20.0))
        if px[x, y] != OUTLINE:
            px[x, y] = (180, 50, 50, 255)
    for y in range(9, 25, 3):
        px[9, y] = (180, 50, 50, 255); px[10, y + 1] = (180, 50, 50, 255)
    return save(name, im)


# ---------------- 떠돌이 상인·공연단 천막 ----------------
def tent_stripe(name, c1, c2, seed=208):
    """줄무늬 둥근 천막 (48×48): 원뿔 지붕 + 입구 휘장 + 깃발."""
    rng = random.Random(seed)
    w, h = 48, 48
    im = canvas(w, h)
    shadow(im, 24, h - 2, 21, 2.2)
    px = im.load()
    for y in range(24, h - 4):                          # 벽
        for x in range(5, 43):
            px[x, y] = OUTLINE if x in (5, 42) else (c1 if ((x - 5) // 4) % 2 == 0 else c2)
    for x in range(5, 43):
        px[x, h - 4] = OUTLINE
    for y in range(4, 25):                              # 원뿔 지붕 (줄무늬가 꼭대기로 모임)
        t = (y - 4) / 20
        half = 3 + 18 * t
        for x in range(int(24 - half), int(24 + half) + 1):
            if not 0 <= x < w:
                continue
            u = (x - 24) / max(1, half)
            edge = x in (int(24 - half), int(24 + half)) or y == 24
            stripe = int((u + 1) * 4) % 2
            col = c1 if stripe == 0 else c2
            if x > 24:
                col = tuple(int(v * 0.86) for v in col[:3]) + (255,)
            px[x, y] = OUTLINE if edge else col
    for x in range(5, 43):                              # 처마 장식
        px[x, 25] = (236, 196, 80, 255) if x % 3 else OUTLINE
    for y in range(0, 6):                               # 깃대·깃발
        px[24, y] = OUTLINE
    for y in range(0, 3):
        for x in range(25, 30 - y):
            px[x, y] = c1
    # 입구 휘장
    for y in range(31, h - 4):
        for x in range(19, 29):
            if abs(x - 23.5) < (y - 29) * 0.45 + 0.5:
                px[x, y] = (60, 30, 40, 255)
            elif abs(x - 23.5) < (y - 29) * 0.45 + 1.5:
                px[x, y] = OUTLINE
    return save(name, im)


def stage(name, seed=209):
    """공연 무대 (64×40): 나무 단 + 붉은 휘장."""
    rng = random.Random(seed)
    w, h = 64, 40
    im = canvas(w, h)
    shadow(im, 32, h - 2, 30, 2)
    px = im.load()
    _fill(px, 2, 26, 61, h - 4, TIMBER[2])
    for x in range(2, 62):
        px[x, 26] = OUTLINE; px[x, h - 4] = OUTLINE
        if x % 6 == 0:
            for y in range(27, h - 4):
                px[x, y] = TIMBER[0]
    for y in range(26, h - 3):
        px[2, y] = OUTLINE; px[61, y] = OUTLINE
    for y in range(2, 27):                              # 휘장
        for x in range(4, 60):
            open_ = 18 + (y - 2) * 0.3 < x < 46 - (y - 2) * 0.3
            if open_ and y > 8:
                continue
            px[x, y] = OUTLINE if y == 2 or x in (4, 59) else (170, 40, 52, 255) if (x // 3) % 2 else (196, 56, 64, 255)
    for x in range(4, 60):
        px[x, 6] = (236, 196, 80, 255) if x % 4 else OUTLINE
    return save(name, im)


# ---------------- 협곡 ----------------
def rock_arch(name, seed=210):
    """붉은 바위 아치 (80×64): 두 바위 기둥 위로 두꺼운 바위 다리. 아래로 지나간다 (랜드마크)."""
    rng = random.Random(seed)
    w, h = 80, 64
    im = canvas(w, h)
    shadow(im, 40, h - 3, 36, 2.5)
    m = _mask(w, h)
    for y in range(4, h - 3):
        for x in range(2, w - 2):
            jag = (rng.random() - 0.5) * 2.2
            left = 4 + jag <= x <= 22 + jag + (3 if y > 40 else 0)
            right = 57 + jag - (3 if y > 40 else 0) <= x <= 75 + jag
            top_band = y <= 26 + jag and ((x + .5 - 40) / 37) ** 2 + ((y + .5 - 30) / 27) ** 2 <= 1
            opening = ((x + .5 - 40) / 18) ** 2 + ((y + .5 - 48) / 26) ** 2 <= 1
            if (left and y >= 14) or (right and y >= 14) or (top_band and not opening):
                m[y][x] = True
    paint_mask(im, m, REDROCK, rng, speck=0.05)
    px = im.load()
    for y in range(8, h - 6, 5):                        # 지층 줄
        for x in range(2, w - 2):
            if px[x, y][3] and px[x, y] != OUTLINE and (x + y) % 7 < 4:
                px[x, y] = REDROCK[1]
    for x in range(10, 70):                             # 꼭대기 마른 풀
        if px[x, 5][3] and rng.random() < 0.3:
            px[x, 4] = (150, 150, 72, 255)
    return save(name, im)


def tex_cobble(name, base=(152, 150, 156), mortar=(96, 92, 98), seed=211):
    """잿빛 자갈 포장 64×64 (줄마다 엇갈린 둥근 돌, 이어 붙여도 이음새 없음)."""
    rng = random.Random(seed)
    im = Image.new('RGBA', (64, 64), mortar + (255,))
    px = im.load()
    y = 0
    row = 0
    while y < 64:
        hgt = 8
        x = (row % 2) * 5
        while x < 64 + 8:
            wid = rng.choice((7, 8, 9))
            shade = rng.uniform(0.86, 1.1)
            for yy in range(y + 1, y + hgt - 1):
                for xx in range(x + 1, x + wid - 1):
                    corner = (xx in (x + 1, x + wid - 2)) and (yy in (y + 1, y + hgt - 2))
                    if corner:
                        continue
                    hl = 1.12 if yy == y + 1 or xx == x + 1 else 0.88 if yy == y + hgt - 2 else 1.0
                    c = tuple(max(0, min(255, int(v * shade * hl))) for v in base)
                    px[xx % 64, yy % 64] = c + (255,)
            x += wid
        y += hgt
        row += 1
    return save(name, im)


def tex_boards(name, seed=212):
    """나무 데크 64×64 (가로 판자)."""
    rng = random.Random(seed)
    im = Image.new('RGBA', (64, 64), (0, 0, 0, 255))
    px = im.load()
    for y in range(64):
        plank = y // 8
        for x in range(64):
            off = (plank * 23) % 64
            seam = (y % 8 == 7) or ((x + off) % 32 == 0)
            k = 2 if (plank % 2) else 3
            c = WOOD[k] if not seam else WOOD[0]
            if not seam and rng.random() < 0.05:
                c = WOOD[k - 1]
            px[x, y] = c
    return save(name, im)


def build_world():
    ven_glass('drawn_venGlass', seed=201)
    ven_glass('drawn_venGlass2', seed=213, wall=(170, 196, 214, 255), awn=((176, 52, 60, 255), (244, 240, 228, 255)))
    ven_house('drawn_venHouse', seed=202)
    ven_house('drawn_venHouse2', seed=214, wall=(222, 150, 150, 255), shutter=(56, 96, 150, 255))
    ven_house('drawn_venHouse3', seed=215, wall=(240, 226, 196, 255), shutter=(150, 70, 60, 255))
    clock_tower('drawn_clockTower')
    gondola('drawn_gondola', seed=204)
    longhouse('drawn_longhouse', seed=205)
    longhouse('drawn_longhouse2', seed=216, shields=((60, 130, 80, 255), (236, 196, 80, 255), (180, 50, 50, 255)))
    nord_shop('drawn_nordShop')
    runestone('drawn_runestone')
    tent_stripe('drawn_tentStripe', (204, 52, 60, 255), (246, 240, 226, 255), seed=208)
    tent_stripe('drawn_tentStripe2', (56, 90, 170, 255), (240, 200, 80, 255), seed=217)
    tent_stripe('drawn_tentStripe3', (80, 140, 90, 255), (246, 240, 226, 255), seed=218)
    stage('drawn_stage')
    rock_arch('drawn_rockArch')
    import arabia_paint as P
    keep = P.SANDSTONE
    P.SANDSTONE = REDROCK                               # 같은 바위 그리기를 붉은 색으로
    try:
        boulder('drawn_redBoulder', seed=219)
        boulder('drawn_redRockWall', w=48, h=36, seed=220,
                blobs=[(12, 22, 11, 11), (26, 16, 13, 13), (38, 23, 9.5, 10), (22, 27, 12, 7)])
        boulder('drawn_redBoulderSmall', w=16, h=14, seed=221, blobs=[(8, 8, 6.5, 5)])
    finally:
        P.SANDSTONE = keep
    ground_texture('drawn_texRed', (196, 120, 84, 255), seed=222,
                   spots=[((172, 100, 70, 255), 100, 2), ((214, 146, 108, 255), 70, 2), ((150, 84, 60, 255), 40, 1)])
    tex_cobble('drawn_texCobble')
    tex_boards('drawn_texBoards')
    build_plaza()


# =====================================================================
#  만국 장터 대광장 (2026-10-27): 특정 나라 풍이 아닌 화려한 축제 광장
#  분수 · 등불 탑 · 열기구 · 축제 천막 · 깃발 줄 · 여행자 동상 · 놀이 노점 · 광장 바닥
# =====================================================================
GOLD = [(150, 104, 30, 255), (200, 150, 44, 255), (236, 196, 80, 255), (252, 230, 150, 255)]
WATER = [(40, 96, 150, 255), (64, 140, 196, 255), (110, 186, 226, 255), (214, 242, 250, 255)]
MARBLE = [(150, 146, 150, 255), (190, 186, 188, 255), (222, 218, 216, 255), (246, 244, 240, 255)]
BRONZE = [(46, 92, 84, 255), (66, 128, 112, 255), (96, 164, 140, 255), (150, 206, 180, 255)]
FESTIVE = [(212, 58, 66, 255), (240, 196, 70, 255), (54, 150, 160, 255), (230, 120, 160, 255), (96, 170, 80, 255), (120, 90, 190, 255)]


def _ellipse_px(px, w, h, cx, cy, rx, ry, fill, edge=OUTLINE, shade=None):
    for y in range(h):
        for x in range(w):
            d = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
            if d <= 1:
                inner = ((x + .5 - cx) / max(1, rx - 1.2)) ** 2 + ((y + .5 - cy) / max(1, ry - 1.2)) ** 2
                if inner > 1:
                    px[x, y] = edge
                else:
                    px[x, y] = shade(x, y) if shade else fill


def grand_fountain(name, seed=301):
    """3단 대리석 분수 (128×96 = 8×6칸): 금테 받침, 떨어지는 물줄기, 꼭대기 금 구슬, 수반 둘레 꽃."""
    rng = random.Random(seed)
    w, h = 128, 96
    im = canvas(w, h)
    shadow(im, 64, h - 6, 62, 6)
    px = im.load()
    cx = 64
    for y in range(66, 90):                              # 큰 수반 옆면
        for x in range(4, 124):
            if ((x + .5 - cx) / 60) ** 2 + ((y + .5 - 76) / 14) ** 2 <= 1 and y >= 74:
                px[x, y] = OUTLINE if y == 89 else MARBLE[2] if x < 84 else MARBLE[1]
    _ellipse_px(px, w, h, cx, 74, 60, 13, MARBLE[3])
    _ellipse_px(px, w, h, cx, 74, 55, 10, WATER[1], edge=GOLD[2],
                shade=lambda x, y: WATER[3] if rng.random() < 0.05 else WATER[2] if (x + y) % 9 == 0 else WATER[1])
    for y in range(40, 74):                              # 가운데 기둥
        for x in range(57, 72):
            px[x, y] = OUTLINE if x in (57, 71) else MARBLE[3] if x < 63 else MARBLE[2]
    _ellipse_px(px, w, h, cx, 42, 28, 8, MARBLE[3])
    _ellipse_px(px, w, h, cx, 41, 23, 5.5, WATER[1], edge=GOLD[2], shade=lambda x, y: WATER[2] if (x * 3 + y) % 7 == 0 else WATER[1])
    for x in range(36, 93):
        if ((x + .5 - cx) / 28) ** 2 <= 1:
            px[x, 50] = MARBLE[1]; px[x, 51] = OUTLINE
    for y in range(18, 40):                              # 꼭대기 기둥
        for x in range(60, 69):
            px[x, y] = OUTLINE if x in (60, 68) else MARBLE[3]
    _ellipse_px(px, w, h, cx, 20, 13, 4, MARBLE[3])
    _ellipse_px(px, w, h, cx, 19.5, 9, 2.5, WATER[2], edge=GOLD[2])
    _ellipse_px(px, w, h, cx, 10, 5, 5, GOLD[2], shade=lambda x, y: GOLD[3] if x < 64 and y < 10 else GOLD[2] if x < 65 else GOLD[1])
    for side in (-1, 1):                                 # 물줄기
        for t in range(0, 50):
            u = t / 49
            x = cx + side * (11 + 13 * u)
            y = 20 + 21 * u * u
            px[int(x), int(y)] = WATER[3] if t % 3 else WATER[2]
        for t in range(0, 80):
            u = t / 79
            x = cx + side * (25 + 24 * u)
            y = 42 + 31 * u * u
            px[int(x), int(y)] = WATER[3] if t % 4 else WATER[2]
            if t % 5 == 0:
                px[int(x) + side, int(y)] = WATER[2]
    for k in range(16):                                  # 물보라
        a = rng.uniform(0, 6.28)
        px[int(cx + math.cos(a) * rng.uniform(40, 52)), int(73 + math.sin(a) * rng.uniform(5, 8))] = WATER[3]
    for k, x in enumerate(range(10, 120, 9)):            # 수반 둘레 꽃
        y = int(76 + 13 * (1 - ((x - cx) / 60) ** 2) ** 0.5) if abs(x - cx) < 60 else 76
        if y < h - 2:
            px[x, y] = FESTIVE[k % 4]; px[x + 1, y] = FESTIVE[k % 4]; px[x, y + 1] = (70, 140, 60, 255)
    return save(name, im)


def lantern_tower(name, seed=302):
    """등불 탑 랜드마크 (48×128 = 3×8칸): 테라코타 탑, 열린 전망대, 금빛 돔, 색색 등불 줄과 긴 깃발."""
    rng = random.Random(seed)
    w, h = 48, 128
    im = canvas(w, h)
    shadow(im, 24, h - 3, 20, 2.5)
    px = im.load()
    x0, x1 = 12, 35
    for y in range(46, h - 6):                           # 탑 몸통 (벽돌)
        for x in range(x0, x1 + 1):
            if x in (x0, x1):
                px[x, y] = OUTLINE
                continue
            row = (y - 46) // 4
            seam = (y - 46) % 4 == 0 or (x + (row % 2) * 3) % 7 == 0
            base = CLAY_[2] if x < x1 - 6 else CLAY_[1]
            px[x, y] = CLAY_[0] if seam else base
    for yb in (70, 94):                                  # 금빛 띠
        for x in range(x0 - 1, x1 + 2):
            px[x, yb] = OUTLINE
            px[x, yb + 1] = GOLD[2] if x % 3 else GOLD[1]
            px[x, yb + 2] = OUTLINE
    for (wx, wy) in ((19, 52), (19, 76), (19, 100)):
        _arch_window(px, wx, wy, 10, 13, (250, 214, 120, 255))
    for x in range(x0 - 3, x1 + 4):                      # 받침
        for y in range(h - 8, h - 3):
            px[x, y] = OUTLINE if y in (h - 8, h - 4) or x in (x0 - 3, x1 + 3) else MARBLE[2]
    # 전망대: 기둥 4개 사이 금빛 종
    for y in range(28, 46):
        for x in (x0, x0 + 7, x1 - 7, x1):
            px[x, y] = OUTLINE
            if x + 1 <= x1:
                px[x + 1, y] = MARBLE[3]
    for x in range(x0 - 2, x1 + 3):
        px[x, 27] = OUTLINE; px[x, 26] = GOLD[2]; px[x, 46] = OUTLINE; px[x, 45] = MARBLE[2]
    _ellipse_px(px, w, h, 24, 37, 4.5, 5, GOLD[2], shade=lambda x, y: GOLD[3] if x < 24 else GOLD[1])
    # 금빛 돔 + 첨탑
    for y in range(8, 27):
        t = (y - 8) / 18
        half = 3 + 13 * math.sin(min(1, t * 1.1) * 1.57)
        for x in range(int(24 - half), int(24 + half) + 1):
            edge = x in (int(24 - half), int(24 + half)) or y == 26
            px[x, y] = OUTLINE if edge else (GOLD[3] if x < 21 else GOLD[2] if x < 27 else GOLD[1])
    for y in range(0, 9):
        px[24, y] = OUTLINE
    for k, c in enumerate(FESTIVE[:4]):                  # 긴 깃발 넷
        for y in range(1 + k, 4 + k):
            for x in range(25, 25 + 9 - (y - 1 - k) * 2 - k):
                if x < w:
                    px[x, y] = c
    # 색색 등불 줄 (돔 아래에서 양쪽으로 늘어진다)
    for side in (-1, 1):
        for t in range(0, 24):
            x = 24 + side * (14 + t * 0.4)
            y = 28 + t * 1.6 + math.sin(t / 23 * 3.14) * 4
            if 0 <= int(x) < w:
                px[int(x), int(y)] = OUTLINE
                if t % 4 == 2:
                    c = FESTIVE[(t // 4 + (side > 0)) % len(FESTIVE)]
                    for dy in (1, 2, 3):
                        for dx in (-1, 0, 1):
                            xx = int(x) + dx
                            if 0 <= xx < w:
                                px[xx, int(y) + dy] = OUTLINE if dy == 3 or abs(dx) == 1 and dy == 1 else c
    return save(name, im)


def balloon(name, c1, c2, seed=303):
    """매어 둔 열기구 (80×144 = 5×9칸): 줄무늬 풍선, 고리버들 바구니, 땅에 박은 말뚝과 모래주머니."""
    rng = random.Random(seed)
    w, h = 80, 144
    im = canvas(w, h)
    shadow(im, 40, h - 5, 30, 4)
    px = im.load()
    cx, cy, rx, ry = 40, 46, 34, 42
    for y in range(0, 100):
        for x in range(w):
            # 아래로 갈수록 좁아지는 물방울 모양
            k = 1.0 if y < cy else max(0.18, 1 - ((y - cy) / 54) ** 1.6)
            r = rx * k * (1 - max(0, (cy - y) / (ry + 4)) ** 2) ** 0.5 if y < cy else rx * k
            if r <= 0 or abs(x + .5 - cx) > r or y < 3:
                continue
            u = (x + .5 - cx) / r
            gore = int((math.asin(max(-1, min(1, u))) / 3.1416 + 0.5) * 8)
            col = c1 if gore % 2 == 0 else c2
            l = -u * 0.6 - (y - cy) / 80
            col = tuple(int(min(255, v * (1.12 if l > 0.3 else 0.84 if l < -0.3 else 1.0))) for v in col[:3]) + (255,)
            edge = abs(x + .5 - cx) > r - 1.2 or y == 3
            px[x, y] = OUTLINE if edge else col
    for y in range(3, 100):                              # 위쪽 테두리 다듬기
        row = [x for x in range(w) if px[x, y][3] and px[x, y] != OUTLINE]
        if row and y < 8:
            for x in row:
                px[x, y] = OUTLINE if y == min(yy for yy in range(3, 10) if px[x, yy][3]) else px[x, y]
    for x in range(14, 66):                              # 허리띠 장식
        y = int(52 + 2 * math.sin((x - 14) / 52 * 3.14))
        if px[x, y][3]:
            px[x, y] = GOLD[2]; px[x, y + 1] = OUTLINE
    # 줄 + 바구니
    for (ax, bx) in ((30, 33), (50, 47), (36, 37), (44, 43)):
        for y in range(96, 112):
            t = (y - 96) / 16
            px[int(ax + (bx - ax) * t), y] = OUTLINE
    for y in range(112, 126):
        for x in range(30, 51):
            edge = x in (30, 50) or y in (112, 125)
            px[x, y] = OUTLINE if edge else (WOOD[2] if (x + y) % 3 else WOOD[1]) if y != 115 else WOOD[3]
    for x in range(29, 52):
        px[x, 112] = OUTLINE; px[x, 113] = FESTIVE[0] if x % 4 < 2 else FESTIVE[1]
    # 땅에 매는 줄 · 말뚝 · 모래주머니
    for (sx, sy) in ((8, 138), (72, 138)):
        for t in range(0, 30):
            u = t / 29
            x, y = 31 + (sx - 31) * u if sx < 40 else 50 + (sx - 50) * u, 122 + (sy - 122) * u
            px[int(x), int(y)] = OUTLINE
        for y in range(sy - 3, sy + 2):
            px[sx, y] = WOOD[0]; px[sx + 1, y] = WOOD[2]
    for (bx, by) in ((26, 132), (52, 134)):
        _ellipse_px(px, w, h, bx, by, 4, 3, (196, 170, 120, 255))
    return save(name, im)


def pavilion(name, c1, c2, seed=304):
    """축제 천막 정자 (64×64 = 4×4칸): 줄무늬 뾰족 지붕, 물결 처마, 기둥, 앞 진열대, 꼭대기 깃발."""
    rng = random.Random(seed)
    w, h = 64, 64
    im = canvas(w, h)
    shadow(im, 32, h - 3, 29, 2.6)
    px = im.load()
    for y in range(6, 30):                               # 지붕
        t = (y - 6) / 23
        half = 2 + 28 * t
        for x in range(int(32 - half), int(32 + half) + 1):
            if not 0 <= x < w:
                continue
            u = (x - 32) / max(1, half)
            edge = x in (int(32 - half), int(32 + half))
            col = c1 if int((u + 1) * 5) % 2 == 0 else c2
            if x > 32:
                col = tuple(int(v * 0.86) for v in col[:3]) + (255,)
            px[x, y] = OUTLINE if edge else col
    for x in range(2, 62):                               # 물결 처마
        k = (x - 2) % 6
        depth = 4 if k in (2, 3) else 3 if k in (1, 4) else 2
        for y in range(30, 30 + depth):
            px[x, y] = c1 if ((x - 2) // 6) % 2 == 0 else c2
        px[x, 30 + depth] = OUTLINE
        px[x, 30] = GOLD[2] if x % 2 else OUTLINE
    for xpost in (6, 57):                                # 기둥
        for y in range(34, h - 4):
            px[xpost, y] = OUTLINE; px[xpost + 1, y] = WOOD[3]; px[xpost + 2, y] = OUTLINE
    for y in range(36, h - 12):                          # 안쪽 그늘
        for x in range(9, 57):
            px[x, y] = (70, 46, 52, 255) if (x + y) % 11 else (84, 56, 60, 255)
    for y in range(h - 12, h - 4):                       # 진열대 + 물건
        for x in range(9, 57):
            px[x, y] = OUTLINE if y in (h - 12, h - 5) or x in (9, 56) else WOOD[2]
    for i, x in enumerate(range(12, 54, 6)):
        c = FESTIVE[(i + seed) % len(FESTIVE)]
        for y in range(h - 17, h - 12):
            for xx in range(x, x + 4):
                px[xx, y] = OUTLINE if y == h - 17 or xx in (x, x + 3) else c
    for y in range(0, 7):                                # 깃발
        px[32, y] = OUTLINE
    for y in range(0, 3):
        for x in range(33, 39 - y):
            px[x, y] = FESTIVE[seed % len(FESTIVE)]
    return save(name, im)


def bunting(name, seed=305):
    """깃발 줄 (96×16 = 6×1칸, 사람 위로 그림): 처진 줄에 색색 삼각 깃발."""
    w, h = 96, 16
    im = canvas(w, h)
    px = im.load()
    for x in range(w):
        y = int(2 + 5 * math.sin(x / (w - 1) * 3.1416))
        px[x, y] = OUTLINE
        if x % 8 == 3 and x < w - 4:
            c = FESTIVE[(x // 8 + seed) % len(FESTIVE)]
            for dy in range(1, 7):
                for dx in range(-3 + dy // 2, 4 - dy // 2):
                    xx = x + dx
                    if 0 <= xx < w and y + dy < h:
                        edge = dx in (-3 + dy // 2, 3 - dy // 2) or dy == 6
                        px[xx, y + dy] = OUTLINE if edge else c
    return save(name, im)


def statue(name, seed=306):
    """여행자 동상 (32×64 = 2×4칸): 등불을 든 여행자, 청동(녹청) + 대리석 받침 + 금 명판."""
    rng = random.Random(seed)
    w, h = 32, 64
    im = canvas(w, h)
    shadow(im, 16, h - 3, 14, 2)
    px = im.load()
    for y in range(42, h - 4):                           # 받침
        for x in range(4, 28):
            px[x, y] = OUTLINE if x in (4, 27) or y in (42, h - 5) else MARBLE[3] if x < 20 else MARBLE[2]
    for x in range(10, 22):
        px[x, 50] = GOLD[2]; px[x, 51] = GOLD[1]
    m = _mask(w, h)
    _ell(m, 15, 10, 3.5, 3.5)                             # 머리
    _rect(m, 11, 14, 20, 30)                             # 몸
    for y in range(30, 42):                              # 망토 자락
        for x in range(9 - (y - 30) // 4, 23 + (y - 30) // 4):
            m[y][x] = True
    _rect(m, 20, 15, 22, 25)                             # 등불 든 팔 (아래로)
    _rect(m, 9, 16, 11, 30)                              # 지팡이 쪽 팔
    _rect(m, 18, 13, 24, 24)                             # 등짐 (상인의 짐 보따리)
    paint_mask(im, m, BRONZE, rng)
    for y in range(4, 42):                               # 지팡이
        px[8, y] = OUTLINE
    _ellipse_px(px, w, h, 24.5, 28, 2.5, 2.5, GOLD[3], shade=lambda x, y: GOLD[3] if y < 28 else GOLD[2])   # 등불
    return save(name, im)


def booth(name, c1, c2, seed=307):
    """놀이 노점 (48×48 = 3×3칸): 줄무늬 차양, 뒤 선반의 인형 경품, 앞 계산대."""
    rng = random.Random(seed)
    w, h = 48, 48
    im = canvas(w, h)
    shadow(im, 24, h - 2, 22, 2)
    px = im.load()
    for y in range(12, h - 4):
        for x in range(4, 44):
            px[x, y] = OUTLINE if x in (4, 43) else WOOD[1] if (x // 6) % 2 else WOOD[2]
    for row, yy in enumerate((16, 24)):                  # 선반 + 경품
        for x in range(6, 42):
            px[x, yy + 5] = OUTLINE
        for i, x in enumerate(range(7, 40, 6)):
            c = FESTIVE[(i + row * 2 + seed) % len(FESTIVE)]
            _ellipse_px(px, w, h, x + 2, yy + 2.5, 2.5, 2.5, c)
    _awning(px, 2, 45, 6, 5, c1, c2)
    for y in range(h - 16, h - 4):                       # 계산대
        for x in range(3, 45):
            px[x, y] = OUTLINE if y in (h - 16, h - 5) or x in (3, 44) else (c2 if (x // 4) % 2 else c1)
    for x in range(14, 34):                              # 간판
        px[x, h - 12] = GOLD[2]; px[x, h - 11] = GOLD[2]; px[x, h - 10] = OUTLINE
    return save(name, im)


def tex_plaza(name, seed=308):
    """광장 바닥 64×64: 크림색 석판(1칸) 4×4 묶음을 테라코타 띠로 두르고, 띠가 만나는 곳에 금 마름모."""
    rng = random.Random(seed)
    im = Image.new('RGBA', (64, 64), (0, 0, 0, 255))
    px = im.load()
    creams = [(236, 220, 186), (228, 210, 174), (240, 228, 198)]
    terra = (196, 120, 82)
    for y in range(64):
        for x in range(64):
            band = x < 3 or y < 3
            if band:
                c = terra if (x + y) % 7 else (176, 104, 70)
            else:
                cream = creams[((x // 16) * 3 + (y // 16) * 5) % 3]
                k = rng.uniform(0.97, 1.03)
                c = tuple(int(min(255, v * k)) for v in cream)
                if x % 16 == 3 or y % 16 == 3 or x % 16 == 0 or y % 16 == 0:
                    c = (204, 182, 146)
            px[x, y] = c + (255,)
    for dy in range(-3, 4):                              # 띠가 만나는 곳 금 마름모
        for dx in range(-3 + abs(dy), 4 - abs(dy)):
            px[(1 + dx) % 64, (1 + dy) % 64] = GOLD[2] if abs(dx) + abs(dy) < 2 else GOLD[1]
    return save(name, im)


CLAY_ = [(126, 58, 40, 255), (172, 86, 54, 255), (206, 120, 74, 255), (234, 160, 108, 255)]


def build_plaza():
    grand_fountain('drawn_grandFountain')
    lantern_tower('drawn_lanternTower')
    balloon('drawn_balloon', (212, 58, 66, 255), (240, 196, 70, 255), seed=303)
    balloon('drawn_balloon2', (54, 150, 160, 255), (246, 238, 220, 255), seed=313)
    pavilion('drawn_pavilion', (212, 58, 66, 255), (246, 238, 220, 255), seed=304)
    pavilion('drawn_pavilion2', (120, 90, 190, 255), (240, 196, 70, 255), seed=314)
    pavilion('drawn_pavilion3', (54, 150, 160, 255), (246, 238, 220, 255), seed=324)
    bunting('drawn_bunting', seed=305)
    bunting('drawn_bunting2', seed=315)
    statue('drawn_statue')
    booth('drawn_booth', (230, 120, 160, 255), (246, 238, 220, 255), seed=307)
    booth('drawn_booth2', (96, 170, 80, 255), (240, 196, 70, 255), seed=317)
    tex_plaza('drawn_texPlaza')
