"""아라비아 맵용 직접 그린 그림 (나무·숲·관목·지붕). 고려 맵처럼 굵은 윤곽 + 3단 명암의 둥근 형태.

출력: Assets/PocketCaravan/Art/Arabia/Sprites/drawn/drawn_*.png  (아라비아 전용 새 그림, 원본 에셋은 건드리지 않음)
arabia_art.sprite()가 Sprites 폴더 전체를 찾으므로 이름만으로 쓸 수 있다.
"""
import math
import os
import random
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'Assets/PocketCaravan/Art/Arabia/Sprites/drawn')

OUTLINE = (52, 40, 28, 255)
SHADOW = (176, 134, 84, 150)       # 모래 위 그림자 (반투명)
TRUNK = [(92, 60, 36, 255), (128, 86, 50, 255), (160, 112, 66, 255)]

GREENS = {   # 어두움 → 밝음 4단
    'oasis': [(46, 92, 46, 255), (70, 128, 56, 255), (104, 162, 70, 255), (150, 196, 92, 255)],
    'olive': [(64, 88, 60, 255), (94, 120, 78, 255), (128, 152, 98, 255), (170, 188, 128, 255)],
    'palm': [(40, 96, 52, 255), (62, 132, 62, 255), (98, 170, 74, 255), (150, 206, 96, 255)],
    'sage': [(84, 104, 70, 255), (116, 138, 88, 255), (150, 168, 110, 255), (190, 204, 150, 255)],
    'acacia': [(70, 110, 44, 255), (104, 146, 56, 255), (140, 176, 72, 255), (184, 208, 108, 255)],
}


def canvas(w, h):
    return Image.new('RGBA', (w, h), (0, 0, 0, 0))


def shadow(im, cx, cy, rx, ry):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1 and px[x, y][3] == 0:
                px[x, y] = SHADOW


def blob(mask, cx, cy, rx, ry, rng, rough=0.0):
    for y in range(len(mask)):
        for x in range(len(mask[0])):
            dx, dy = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            r = 1 + (rng.random() - .5) * rough
            if dx * dx + dy * dy <= r:
                mask[y][x] = True


def paint_mask(im, mask, pal, rng, light=(-0.6, -0.8), speck=0.03, cx=None, cy=None):
    """마스크를 윤곽 + 명암으로 칠한다. 빛은 왼쪽 위."""
    h, w = len(mask), len(mask[0])
    px = im.load()
    ys = [y for y in range(h) for x in range(w) if mask[y][x]]
    xs = [x for y in range(h) for x in range(w) if mask[y][x]]
    if not xs:
        return
    cx = cx if cx is not None else (min(xs) + max(xs) + 1) / 2
    cy = cy if cy is not None else (min(ys) + max(ys) + 1) / 2
    rx, ry = (max(xs) - min(xs) + 1) / 2, (max(ys) - min(ys) + 1) / 2
    for y in range(h):
        for x in range(w):
            if not mask[y][x]:
                continue
            edge = any(not (0 <= x + dx < w and 0 <= y + dy < h and mask[y + dy][x + dx])
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge:
                px[x, y] = OUTLINE
                continue
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            l = -(nx * light[0] + ny * light[1]) + rng.uniform(-0.08, 0.08)
            k = 0 if l < -0.45 else 1 if l < 0.05 else 2 if l < 0.5 else 3
            if rng.random() < speck:
                k = max(0, min(3, k + rng.choice((-1, 1))))
            px[x, y] = pal[k]


def trunk(im, x0, y0, x1, y1, ring=True):
    px = im.load()
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if x in (x0, x1):
                px[x, y] = OUTLINE
            else:
                k = 2 if x == x0 + 1 else 1
                if ring and (y - y0) % 3 == 2:
                    k = 0
                px[x, y] = TRUNK[k]
    for x in range(x0, x1 + 1):
        px[x, y1] = OUTLINE


def round_tree(name, w=32, h=32, kind='oasis', seed=1, crowns=None, trunk_h=5, fruit=None, blossom=None):
    """고려 둥근 나무처럼: 둥근 수관(여럿이면 무리) + 짧은 줄기 + 그림자."""
    rng = random.Random(seed)
    im = canvas(w, h)
    crowns = crowns or [(w / 2, h / 2 - 3, w / 2 - 1.5, h / 2 - 5)]
    for cx, cy, rx, ry in crowns:
        shadow(im, cx, h - 3, rx * 0.8, 2.6)
    for cx, cy, rx, ry in sorted(crowns, key=lambda c: c[1]):
        bottom = int(cy + ry)
        trunk(im, int(cx) - 2, bottom - 2, int(cx) + 1, min(h - 2, bottom + trunk_h), ring=False)
    for cx, cy, rx, ry in sorted(crowns, key=lambda c: c[1]):
        mask = [[False] * w for _ in range(h)]
        blob(mask, cx, cy, rx * 0.86, ry * 0.86, rng)
        n = 9
        for k in range(n):                       # 가장자리 잎 뭉치 (둥근 혹)
            a = 2 * math.pi * k / n + 0.3
            blob(mask, cx + math.cos(a) * rx * 0.62, cy + math.sin(a) * ry * 0.62, rx * 0.4, ry * 0.4, rng)
        paint_mask(im, mask, GREENS[kind], rng, cx=cx, cy=cy)
        # 잎 뭉치 하이라이트 (왼쪽 위 둥근 반점)
        px = im.load()
        for hx, hy, r in ((cx - rx * 0.35, cy - ry * 0.35, rx * 0.28), (cx + rx * 0.2, cy - ry * 0.05, rx * 0.18)):
            for y in range(h):
                for x in range(w):
                    if mask[y][x] and px[x, y] != OUTLINE and (x + .5 - hx) ** 2 + (y + .5 - hy) ** 2 <= r * r:
                        px[x, y] = GREENS[kind][3]
        # 아래쪽 어두운 띠
        for x in range(w):
            ys = [y for y in range(h) if mask[y][x] and px[x, y] != OUTLINE]
            if ys and ys[-1] > cy:
                for y in ys[-2:]:
                    px[x, y] = GREENS[kind][0]
        if blossom:      # 꽃 핀 나무: 수관 위에 꽃 점
            for y in range(h):
                for x in range(w):
                    if mask[y][x] and px[x, y] != OUTLINE and rng.random() < 0.22:
                        px[x, y] = blossom if rng.random() < 0.7 else (255, 240, 244, 255)
        if fruit:        # 열매: 2×2 점 + 밝은 반짝임
            for _ in range(int(rx * ry / 9)):
                for _try in range(20):
                    x, y = rng.randint(int(cx - rx) + 2, int(cx + rx) - 3), rng.randint(int(cy - ry) + 2, int(cy + ry) - 3)
                    if all(mask[y + j][x + i] and px[x + i, y + j] != OUTLINE for i in (0, 1) for j in (0, 1)):
                        px[x, y] = fruit[1]; px[x + 1, y] = fruit[0]; px[x, y + 1] = fruit[0]; px[x + 1, y + 1] = fruit[0]
                        break
    return save(name, im)


def date_palm(name, h=48, seed=2, lean=0, dates=True):
    """굵은 윤곽의 대추야자: 마디 줄기 + 잎마다 따로 그린 처진 잎(톱니 잎끝) + 대추 송이."""
    rng = random.Random(seed)
    w = 32
    im = canvas(w, h)
    shadow(im, 16, h - 3, 9, 2.6)
    top = 15
    px = im.load()
    for y in range(top, h - 2):
        t = (y - top) / (h - 2 - top)
        x0 = int(round(14 + lean * (1 - t) ** 2 * 3))
        for x in range(x0, x0 + 4):
            px[x, y] = OUTLINE if x in (x0, x0 + 3) or y == h - 3 else (TRUNK[0] if (y - top) % 3 == 0 else TRUNK[1 if x == x0 + 2 else 2])
    pal = GREENS['palm']
    cx, cy = 16 + lean * 3, top
    # (각도, 길이, 앞/뒤) 뒤쪽 잎 먼저
    fronds = [(-150, 12, 0), (-30, 12, 0), (-110, 11, 0), (-65, 11, 0), (-88, 10, 0),
              (180, 14, 1), (0, 14, 1), (150, 12, 1), (30, 12, 1), (100, 8, 1)]
    for a, L, front in fronds:
        L += rng.uniform(-1, 1)
        ar = math.radians(a)
        mask = [[False] * w for _ in range(h)]
        steps = int(L * 4)
        for st in range(steps + 1):
            t = st / steps
            x = cx + math.cos(ar) * L * t + (t ** 2) * 3 * (1 if math.cos(ar) >= 0 else -1) * (1 if math.sin(ar) < -0.8 else 0)
            y = cy - 2 + math.sin(ar) * L * t * (0.6 if math.sin(ar) > -0.8 else 0.85) + (t ** 1.8) * (8 if abs(math.cos(ar)) > 0.5 else 4)
            wdt = 2.6 * (1 - t) ** 0.6 + 0.6
            for yy in range(int(round(y - wdt * 0.55)), int(round(y + wdt * 0.55)) + 1):
                for xx in range(int(round(x - 0.6)), int(round(x + 0.6)) + 1):
                    if 0 <= xx < w and 0 <= yy < h:
                        mask[yy][xx] = True
            # 아래쪽 톱니 잎끝
            if st % 3 == 0 and 0.15 < t < 0.95:
                xx, yy = int(round(x)), int(round(y + wdt * 0.55 + 1))
                if 0 <= xx < w and 0 <= yy < h:
                    mask[yy][xx] = True
        shade = [pal[0], pal[0], pal[1], pal[2]] if not front else pal
        paint_mask(im, mask, shade, rng, light=(-0.2, -1.0), speck=0.0)
        # 잎 가운데 줄기 (밝은 선)
        for st in range(2, steps - 2):
            t = st / steps
            x = cx + math.cos(ar) * L * t + (t ** 2) * 3 * (1 if math.cos(ar) >= 0 else -1) * (1 if math.sin(ar) < -0.8 else 0)
            y = cy - 2 + math.sin(ar) * L * t * (0.6 if math.sin(ar) > -0.8 else 0.85) + (t ** 1.8) * (8 if abs(math.cos(ar)) > 0.5 else 4)
            xx, yy = int(round(x)), int(round(y))
            if 0 <= xx < w and 0 <= yy < h and px[xx, yy] != OUTLINE:
                px[xx, yy] = pal[3] if front else pal[2]
    if dates:
        for dx in (-3, 2):
            for k in range(6):
                x, y = int(cx + dx + (k % 3)), int(cy + 1 + k // 3)
                if 0 <= x < w:
                    px[x, y] = (196, 108, 40, 255) if (k + dx) % 2 else (236, 156, 60, 255)
    return save(name, im)


def cypress(name, seed=3):
    rng = random.Random(seed)
    w, h = 16, 40
    im = canvas(w, h)
    shadow(im, 8, h - 3, 6, 2.2)
    trunk(im, 6, h - 8, 9, h - 2, ring=False)
    mask = [[False] * w for _ in range(h)]
    for y in range(2, h - 6):
        t = (y - 2) / (h - 8)
        r = 1.5 + 5.2 * math.sin(math.pi * min(1, t * 1.15)) ** 0.8
        for x in range(w):
            if abs(x + .5 - 8) <= r:
                mask[y][x] = True
    paint_mask(im, mask, GREENS['oasis'], rng, speck=0.25)
    return save(name, im)


def shrub(name, kind='sage', seed=4, flowers=None, w=16, h=16):
    rng = random.Random(seed)
    im = canvas(w, h)
    shadow(im, w / 2, h - 3, w / 2 - 2, 2)
    mask = [[False] * w for _ in range(h)]
    blob(mask, w / 2, h / 2 + 1, w / 2 - 1.5, h / 2 - 3, rng, rough=0.2)
    paint_mask(im, mask, GREENS[kind], rng)
    if flowers:
        px = im.load()
        for _ in range(4):
            x, y = rng.randint(4, w - 5), rng.randint(4, h - 6)
            if px[x, y][3] and px[x, y] != OUTLINE:
                px[x, y] = flowers
    return save(name, im)


def roofed(name, facade_name, seed=5, extra=None):
    """평평한 지붕 집: 정면 그림 위에 위에서 내려다본 옥상(난간·물항아리·빨래·작은 돔)을 얹어 3/4 시점 깊이를 만든다."""
    import arabia_art as A
    rng = random.Random(seed)
    f = A.sprite(facade_name)
    bb = f.getbbox()
    f = f.crop(bb)
    depth = 14
    w, h = f.width, f.height + depth
    im = canvas(w, h)
    px_f = f.load()
    # 옥상 색: 정면 위쪽 벽 색에서
    wall = max(((px_f[x, y], 1) for x in range(w) for y in range(2, min(8, f.height)) if px_f[x, y][3] == 255 and sum(px_f[x, y][:3]) > 300),
               key=lambda c: sum(c[0][:3]), default=((220, 190, 140, 255), 1))[0]
    top = tuple(min(255, int(c * 1.04)) for c in wall[:3]) + (255,)
    lip = tuple(int(c * 0.82) for c in wall[:3]) + (255,)
    px = im.load()
    for y in range(depth + 1):
        for x in range(w):
            inset = 0
            if x in (0, w - 1) or y == 0:
                px[x, y] = OUTLINE
            elif x in (1, w - 2) or y == 1:
                px[x, y] = lip
            else:
                px[x, y] = top if (x * 3 + y * 5) % 17 else tuple(int(c * 0.95) for c in top[:3]) + (255,)
    # 옥상 소품
    items = extra or rng.sample(['jar', 'laundry', 'dome', 'rug', 'plant'], 2)
    for k, it in enumerate(items):
        x0 = 3 + k * (w // 2)
        if it == 'jar':
            for (dx, dy, c) in [(1, 3, OUTLINE), (2, 3, OUTLINE), (0, 4, OUTLINE), (1, 4, (196, 112, 70, 255)), (2, 4, (214, 132, 84, 255)), (3, 4, OUTLINE),
                                (0, 5, OUTLINE), (1, 5, (176, 96, 60, 255)), (2, 5, (196, 112, 70, 255)), (3, 5, OUTLINE), (1, 6, OUTLINE), (2, 6, OUTLINE)]:
                if 0 <= x0 + dx < w: px[x0 + dx, 2 + dy] = c
        elif it == 'laundry':
            for dx in range(0, min(12, w - x0 - 2)):
                px[x0 + dx, 5] = (110, 80, 50, 255)
            cols = [(214, 74, 60, 255), (70, 120, 190, 255), (240, 214, 120, 255)]
            for j in range(3):
                for yy in range(6, 9):
                    for xx in range(2):
                        if x0 + 1 + j * 4 + xx < w - 2: px[x0 + 1 + j * 4 + xx, yy] = cols[j]
        elif it == 'dome':
            for yy in range(3, 11):
                for xx in range(-4, 5):
                    if xx * xx / 20 + (yy - 10) ** 2 / 50 <= 1 and 0 <= x0 + 4 + xx < w - 2:
                        edge = xx * xx / 20 + (yy - 10) ** 2 / 50 > 0.6
                        px[x0 + 4 + xx, yy] = OUTLINE if edge and (abs(xx) == 4 or yy == 3) else (236, 232, 222, 255) if xx < 1 else (206, 200, 188, 255)
        elif it == 'rug':
            for yy in range(4, 10):
                for xx in range(0, 7):
                    if x0 + xx < w - 2:
                        px[x0 + xx, yy] = OUTLINE if yy in (4, 9) or xx in (0, 6) else ((160, 44, 52, 255) if (xx + yy) % 3 else (236, 196, 90, 255))
        elif it == 'plant':
            for (dx, dy) in [(1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (3, 1), (1, 2), (2, 2)]:
                if x0 + dx < w - 2: px[x0 + dx, 4 + dy] = GREENS['oasis'][1 + (dx + dy) % 2]
            for (dx, dy) in [(1, 3), (2, 3), (0, 4), (3, 4), (1, 5), (2, 5)]:
                if x0 + dx < w - 2: px[x0 + dx, 4 + dy] = (190, 104, 66, 255) if 0 < dx < 3 else OUTLINE
    im.alpha_composite(f, (0, depth))
    return save(name, im)


def tuft(name, seed=40):
    """모래 위 마른 풀 한 포기 (바닥 장식, 막지 않음)."""
    rng = random.Random(seed)
    im = canvas(16, 16)
    px = im.load()
    base = 12
    for k in range(5):
        x = 4 + k * 2 + rng.randint(0, 1)
        hgt = rng.randint(3, 6)
        lean = rng.choice((-1, 0, 1))
        for j in range(hgt):
            xx = x + (lean if j > hgt // 2 else 0)
            px[xx, base - j] = (150, 150, 72, 255) if j < hgt - 1 else (196, 190, 104, 255)
    for x in range(4, 14):
        if px[x, base][3]:
            px[x, base + 1] = (186, 146, 96, 160)
    return save(name, im)


def pebbles(name, seed=44):
    rng = random.Random(seed)
    im = canvas(16, 16)
    px = im.load()
    for (x, y, r) in ((5, 10, 1.6), (9, 11, 1.2), (11, 8, 1.0)):
        for yy in range(16):
            for xx in range(16):
                d = ((xx + .5 - x) / r) ** 2 + ((yy + .5 - y) / (r * 0.8)) ** 2
                if d <= 1:
                    px[xx, yy] = (150, 124, 96, 255) if d > 0.5 else (196, 172, 140, 255)
                elif d <= 1.6 and yy > y and not px[xx, yy][3]:
                    px[xx, yy] = (176, 136, 90, 120)
    return save(name, im)


def save(name, im):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.png')
    old = Image.open(path).convert('RGBA') if os.path.exists(path) else None
    if old is None or old.tobytes() != im.tobytes() or old.size != im.size:
        im.save(path)
    return im


def build_all():
    round_tree('drawn_oasisTree', kind='oasis', seed=11)
    round_tree('drawn_oasisTree2', kind='oasis', seed=12)
    round_tree('drawn_olive', kind='olive', seed=13)
    round_tree('drawn_acaciaRound', kind='acacia', seed=14)
    round_tree('drawn_grove', w=48, h=40, kind='oasis', seed=15,
               crowns=[(12, 21, 10.5, 9), (36, 21, 10.5, 9), (24, 12, 11, 9.5)])
    round_tree('drawn_oliveGrove', w=48, h=40, kind='olive', seed=16,
               crowns=[(12, 21, 10.5, 9), (36, 21, 10.5, 9), (24, 12, 11, 9.5)])
    date_palm('drawn_datePalm', seed=21)
    date_palm('drawn_datePalm2', seed=22, h=44)
    date_palm('drawn_datePalm3', seed=23, h=52, dates=False)
    round_tree('drawn_orange', kind='oasis', seed=17, fruit=((232, 132, 32, 255), (255, 214, 120, 255)))
    round_tree('drawn_pomegranate', kind='oasis', seed=18, fruit=((196, 40, 52, 255), (250, 140, 140, 255)))
    round_tree('drawn_lemon', kind='palm', seed=19, fruit=((236, 210, 60, 255), (255, 248, 180, 255)))
    round_tree('drawn_almond', kind='oasis', seed=20, blossom=(244, 170, 196, 255))
    cypress('drawn_cypress')
    tuft('drawn_tuft', seed=41)
    tuft('drawn_tuft2', seed=42)
    pebbles('drawn_pebbles', seed=43)
    build_extra()
    build_regions()
    shrub('drawn_sage', 'sage', seed=31)
    shrub('drawn_bush', 'oasis', seed=32)
    shrub('drawn_oleander', 'oasis', seed=33, flowers=(236, 120, 160, 255))
    shrub('drawn_jasmine', 'palm', seed=34, flowers=(250, 248, 236, 255))
    build_central()


# ---------------- 동물·우물·꽃·소품 (나무와 같은 화풍) ----------------
SANDCOAT = [(150, 104, 62, 255), (190, 142, 88, 255), (218, 176, 116, 255), (240, 208, 150, 255)]
GREYCOAT = [(98, 92, 96, 255), (132, 126, 128, 255), (166, 160, 158, 255), (204, 198, 192, 255)]
STONE = [(104, 96, 90, 255), (146, 136, 124, 255), (184, 172, 156, 255), (216, 206, 190, 255)]
CLAY = [(128, 62, 40, 255), (176, 92, 56, 255), (208, 124, 74, 255), (236, 164, 108, 255)]
WOOD = [(86, 56, 34, 255), (122, 82, 48, 255), (156, 110, 66, 255), (190, 144, 92, 255)]


def _mask(w, h):
    return [[False] * w for _ in range(h)]


def _ell(mask, cx, cy, rx, ry):
    for y in range(len(mask)):
        for x in range(len(mask[0])):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1:
                mask[y][x] = True


def _rect(mask, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if 0 <= y < len(mask) and 0 <= x < len(mask[0]):
                mask[y][x] = True


def camel(name, rest=False, seed=51):
    """작고 귀여운 단봉낙타 (오른쪽 보기). 서 있는 것 28×24 / 앉은 것 28×18."""
    rng = random.Random(seed)
    w, h = 32, 26 if not rest else 20
    im = canvas(w, h)
    shadow(im, 15, h - 2.5, 11, 2)
    m = _mask(w, h)
    base = h - 3
    if rest:
        _ell(m, 14, base - 4, 10, 4.5)            # 몸 (앉음)
        _ell(m, 13, base - 8, 5, 4)               # 혹
        _rect(m, 21, base - 12, 23, base - 5)     # 목
        _ell(m, 24.5, base - 12, 3.5, 2.4)        # 머리
    else:
        for lx in (7, 10, 17, 20):                # 다리
            _rect(m, lx, base - 7, lx + 1, base)
        _ell(m, 14, base - 9, 9.5, 4.5)           # 몸
        _ell(m, 13, base - 13, 5, 4)              # 혹
        _rect(m, 21, base - 17, 23, base - 9)     # 목
        _ell(m, 24.5, base - 17, 3.5, 2.4)        # 머리
    paint_mask(im, m, SANDCOAT, rng, speck=0.0)
    px = im.load()
    hy = base - (12 if rest else 17)
    px[25, hy - 1] = OUTLINE                      # 눈
    px[27, hy + 1] = SANDCOAT[0]                  # 코
    px[22, hy - 3] = OUTLINE; px[23, hy - 3] = OUTLINE   # 귀
    # 혹 위 안장 담요 (빨강 + 금색 술)
    sy = base - (11 if rest else 16)
    for x in range(9, 18):
        for y in range(sy, sy + 4):
            if px[x, y][3] and px[x, y] != OUTLINE:
                px[x, y] = (186, 52, 52, 255) if y < sy + 3 else (236, 188, 72, 255)
    for x in (9, 12, 15):
        if px[x, sy + 4][3]:
            px[x, sy + 4] = (236, 188, 72, 255)
    # 꼬리
    px[4, base - (6 if rest else 10)] = OUTLINE
    px[4, base - (5 if rest else 9)] = SANDCOAT[0]
    return save(name, im)


def donkey(name, seed=52):
    rng = random.Random(seed)
    w, h = 24, 22
    im = canvas(w, h)
    shadow(im, 11, h - 2.5, 8, 1.8)
    m = _mask(w, h)
    base = h - 3
    for lx in (5, 8, 13, 16):
        _rect(m, lx, base - 6, lx + 1, base)
    _ell(m, 11, base - 8, 7.5, 3.8)
    _rect(m, 16, base - 13, 18, base - 8)
    _ell(m, 19.5, base - 12, 3, 2.3)
    _rect(m, 16, base - 17, 16, base - 14); _rect(m, 18, base - 17, 18, base - 14)   # 귀
    paint_mask(im, m, GREYCOAT, rng, speck=0.0)
    px = im.load()
    px[20, base - 13] = OUTLINE
    for x in range(7, 15):                         # 짐 자루
        for y in range(base - 14, base - 9):
            if (x - 11) ** 2 / 16 + (y - base + 11.5) ** 2 / 6 <= 1:
                px[x, y] = OUTLINE if (x - 11) ** 2 / 16 + (y - base + 11.5) ** 2 / 6 > 0.7 else (200, 160, 100, 255)
    return save(name, im)


def well(name, seed=53):
    """돌 우물: 둥근 돌 테두리 + 물 + 나무 기둥·지붕 + 두레박. 32×40."""
    rng = random.Random(seed)
    w, h = 32, 40
    im = canvas(w, h)
    shadow(im, 16, h - 3, 13, 2.6)
    px = im.load()
    # 돌 몸통 (원기둥)
    body = _mask(w, h)
    _rect(body, 4, 24, 27, 35)
    _ell(body, 16, 35, 12, 3)
    _ell(body, 16, 24, 12, 4)
    paint_mask(im, body, STONE, rng, light=(-0.9, -0.2), speck=0.0)
    for y in range(26, 36, 3):                     # 돌 줄눈
        off = 0 if (y // 3) % 2 else 3
        for x in range(5, 27):
            if px[x, y] != OUTLINE and px[x, y][3]:
                px[x, y] = STONE[0] if (x + off) % 6 else px[x, y]
        for x in range(5 + off, 27, 6):
            for yy in (y + 1, y + 2):
                if yy < 36 and px[x, yy] != OUTLINE and px[x, yy][3]:
                    px[x, yy] = STONE[0]
    # 위 테두리와 물
    for y in range(19, 29):
        for x in range(3, 29):
            d = ((x + .5 - 16) / 12.5) ** 2 + ((y + .5 - 24) / 4.6) ** 2
            if d <= 1:
                px[x, y] = OUTLINE if d > 0.86 else STONE[3] if d > 0.62 else ((70, 140, 196, 255) if d > 0.3 else (120, 190, 226, 255))
    px[13, 23] = (230, 248, 255, 255); px[14, 23] = (230, 248, 255, 255)
    # 기둥과 가로대
    for x0 in (4, 25):
        for y in range(5, 25):
            for x in range(x0, x0 + 3):
                px[x, y] = OUTLINE if x in (x0, x0 + 2) else WOOD[2]
    for x in range(4, 28):
        for y in (9, 10, 11):
            px[x, y] = OUTLINE if y != 10 else WOOD[1]
    # 지붕 (붉은 기와, 작은 박공)
    for y in range(0, 8):
        half = 6 + y * 1.6
        for x in range(w):
            if abs(x + .5 - 16) <= half:
                edge = abs(x + .5 - 16) > half - 1 or y in (0, 7)
                px[x, y] = OUTLINE if edge else ((176, 64, 50, 255) if (x + y) % 4 else (210, 96, 70, 255))
    # 밧줄 + 두레박
    for y in range(11, 19):
        px[16, y] = (196, 168, 112, 255)
    for y in range(18, 22):
        for x in range(14, 19):
            px[x, y] = OUTLINE if x in (14, 18) or y == 21 else WOOD[2]
    return save(name, im)


def flower(name, petals, bush=True, seed=60, w=16, h=16, center=(250, 214, 90, 255), n=5):
    """나무와 어울리는 꽃 덤불: 둥근 잎 덤불 위에 꽃송이(2×2 + 가운데)."""
    rng = random.Random(seed)
    im = canvas(w, h)
    shadow(im, w / 2, h - 2.5, w / 2 - 2.5, 1.6)
    px = im.load()
    if bush:
        m = _mask(w, h)
        _ell(m, w / 2, h / 2 + 1.5, w / 2 - 1.5, h / 2 - 3)
        paint_mask(im, m, GREENS['oasis'], rng, speck=0.0)
        spots = [(4, 6), (9, 5), (6, 9), (11, 9), (8, 7), (3, 10), (12, 6)][:n]
    else:      # 키 작은 꽃무리 (사프란·크로커스)
        spots = [(3, 8), (7, 6), (11, 8), (5, 11), (10, 11)][:n]
        for (x, y) in spots:
            for yy in range(y + 2, h - 3):
                px[x + 1, yy] = GREENS['palm'][1]
            px[x, h - 4] = GREENS['palm'][2]; px[x + 2, h - 4] = GREENS['palm'][2]
    dark = tuple(int(c * 0.7) for c in petals[:3]) + (255,)
    for (x, y) in spots:
        for (dx, dy, c) in [(1, 0, petals), (0, 1, petals), (2, 1, petals), (1, 2, dark), (0, 0, OUTLINE if not bush else petals),
                            (1, 1, center)]:
            if 0 <= x + dx < w and 0 <= y + dy < h:
                px[x + dx, y + dy] = c
    return save(name, im)


def lily(name, seed=61):
    rng = random.Random(seed)
    im = canvas(16, 16)
    m = _mask(16, 16)
    _ell(m, 8, 10, 6.5, 3.5)
    paint_mask(im, m, GREENS['palm'], rng, speck=0.0)
    px = im.load()
    px[8, 10] = OUTLINE; px[9, 11] = OUTLINE           # 잎 갈라짐
    for (dx, dy, c) in [(7, 5, (250, 200, 220, 255)), (8, 5, (250, 200, 220, 255)), (6, 6, (240, 160, 196, 255)), (9, 6, (240, 160, 196, 255)),
                        (7, 6, (255, 240, 246, 255)), (8, 6, (255, 240, 246, 255)), (7, 7, (240, 160, 196, 255)), (8, 7, (240, 160, 196, 255))]:
        px[dx, dy] = c
    return save(name, im)


def jar(name, seed=62, tall=True):
    rng = random.Random(seed)
    w, h = 16, 18 if tall else 14
    im = canvas(w, h)
    shadow(im, 8, h - 2.5, 5, 1.4)
    m = _mask(w, h)
    _ell(m, 8, h - 7, 5, 5 if tall else 4)
    _rect(m, 6, 1, 9, h - 10)
    _rect(m, 5, 1, 10, 2)
    paint_mask(im, m, CLAY, rng, speck=0.0)
    px = im.load()
    for x in range(4, 13):                              # 무늬 띠
        y = h - 8
        if px[x, y][3] and px[x, y] != OUTLINE:
            px[x, y] = (236, 210, 160, 255)
    return save(name, im)


def potted_tree(name, seed=63):
    rng = random.Random(seed)
    w, h = 16, 26
    im = canvas(w, h)
    shadow(im, 8, h - 2.5, 6, 1.6)
    m = _mask(w, h)
    _ell(m, 8, 7, 6.5, 6)
    paint_mask(im, m, GREENS['oasis'], rng, speck=0.0)
    px = im.load()
    for (x, y) in [(5, 5), (9, 4), (10, 8), (6, 9)]:
        px[x, y] = (236, 140, 40, 255); px[x + 1, y] = (255, 200, 110, 255)
    for y in range(13, 17):
        px[7, y] = OUTLINE; px[8, y] = TRUNK[1]
    pm = _mask(w, h)
    for y in range(16, h - 2):
        t = (y - 16) / (h - 18)
        _rect(pm, int(3 + t * 1.5), y, int(12 - t * 1.5), y)
    paint_mask(im, pm, CLAY, rng, speck=0.0)
    for x in range(3, 13):
        if px[x, 18][3] and px[x, 18] != OUTLINE:
            px[x, 18] = (70, 130, 170, 255)               # 파란 띠 (아라비아 도기)
    return save(name, im)


def rocks(name, seed=64):
    rng = random.Random(seed)
    im = canvas(16, 16)
    shadow(im, 8, 13, 6, 1.6)
    m = _mask(16, 16)
    _ell(m, 6, 10, 4, 3); _ell(m, 11, 11, 3, 2.3)
    paint_mask(im, m, STONE, rng, speck=0.0)
    return save(name, im)


def build_extra():
    camel('drawn_camel')
    camel('drawn_camelRest', rest=True)
    donkey('drawn_donkey')
    well('drawn_well')
    flower('drawn_roseBush', (214, 58, 76, 255), seed=70)
    flower('drawn_oleanderBush', (244, 128, 172, 255), seed=71)
    flower('drawn_jasmineBush', (252, 250, 240, 255), seed=72, center=(244, 210, 90, 255))
    flower('drawn_adenium', (236, 84, 120, 255), seed=73, n=4)
    flower('drawn_marigold', (244, 160, 40, 255), seed=74, center=(196, 90, 30, 255), n=6)
    flower('drawn_saffron', (150, 96, 200, 255), bush=False, seed=75, center=(236, 120, 40, 255))
    flower('drawn_saffron2', (176, 120, 220, 255), bush=False, seed=76, center=(236, 120, 40, 255), n=4)
    lily('drawn_lily')
    jar('drawn_jar')
    jar('drawn_jarSmall', tall=False)
    potted_tree('drawn_pottedOrange')
    rocks('drawn_rocks')


# ---------------- 확장 지역용 (산길·항구·수도) ----------------
SANDSTONE = [(112, 76, 54, 255), (156, 112, 78, 255), (194, 148, 104, 255), (226, 190, 144, 255)]


def boulder(name, w=32, h=26, seed=80, blobs=None):
    rng = random.Random(seed)
    im = canvas(w, h)
    shadow(im, w / 2, h - 3, w / 2 - 2, 2.2)
    m = _mask(w, h)
    for (cx, cy, rx, ry) in (blobs or [(w * 0.42, h * 0.55, w * 0.32, h * 0.38), (w * 0.66, h * 0.62, w * 0.26, h * 0.3)]):
        _ell(m, cx, cy, rx, ry)
    paint_mask(im, m, SANDSTONE, rng, speck=0.02)
    px = im.load()
    for _ in range(3):                                  # 금
        x, y = rng.randint(6, w - 7), rng.randint(6, h - 8)
        for k in range(3):
            if px[x + k, y + (k % 2)][3] and px[x + k, y + (k % 2)] != OUTLINE:
                px[x + k, y + (k % 2)] = SANDSTONE[0]
    return save(name, im)


def coffee(name, seed=81):
    """커피나무 덤불 (붉은 열매) — 고원 계단밭."""
    rng = random.Random(seed)
    im = canvas(16, 20)
    shadow(im, 8, 17, 6, 1.6)
    m = _mask(16, 20)
    _ell(m, 8, 9, 6.5, 7.5)
    paint_mask(im, m, GREENS['palm'], rng, speck=0.0)
    px = im.load()
    for (x, y) in [(5, 6), (10, 7), (7, 11), (11, 12), (4, 10)]:
        px[x, y] = (200, 40, 40, 255); px[x + 1, y] = (236, 90, 70, 255)
    return save(name, im)


def terrace_wall(name, seed=82):
    """계단밭 돌담 한 칸 (16×16)."""
    rng = random.Random(seed)
    im = canvas(16, 16)
    px = im.load()
    for y in range(6, 16):
        for x in range(16):
            row = (y - 6) // 3
            joint = (x + (row % 2) * 3) % 6 == 0 or (y - 6) % 3 == 2
            px[x, y] = OUTLINE if y == 6 or y == 15 else (SANDSTONE[0] if joint else SANDSTONE[2 if (x + y) % 5 else 3])
    return save(name, im)


def dye_vat(name, color, seed=83):
    """염색 항아리 통 (위에서 보이는 염료)."""
    rng = random.Random(seed)
    im = canvas(16, 18)
    shadow(im, 8, 15.5, 6.5, 1.5)
    m = _mask(16, 18)
    _rect(m, 2, 6, 13, 14); _ell(m, 7.5, 14, 6, 2); _ell(m, 7.5, 6, 6, 2.5)
    paint_mask(im, m, CLAY, rng, light=(-0.9, -0.1), speck=0.0)
    px = im.load()
    dark = tuple(int(c * 0.7) for c in color[:3]) + (255,)
    for y in range(4, 9):
        for x in range(2, 14):
            d = ((x + .5 - 7.5) / 5.6) ** 2 + ((y + .5 - 6) / 2.2) ** 2
            if d <= 1:
                px[x, y] = OUTLINE if d > 0.8 else (color if d < 0.45 else dark)
    return save(name, im)


def cloth_rack(name, colors, seed=84):
    """천 말리는 대 (두 기둥 + 색 천 3장), 32×28."""
    im = canvas(32, 28)
    shadow(im, 16, 25, 13, 1.8)
    px = im.load()
    for x0 in (2, 28):
        for y in range(3, 25):
            for x in (x0, x0 + 1):
                px[x, y] = OUTLINE if x == x0 else WOOD[2]
    for x in range(2, 30):
        px[x, 4] = OUTLINE; px[x, 5] = WOOD[1]
    for i, c in enumerate(colors):
        x0 = 5 + i * 8
        dark = tuple(int(v * 0.75) for v in c[:3]) + (255,)
        for y in range(6, 6 + 13 - (i % 2) * 3):
            for x in range(x0, x0 + 6):
                px[x, y] = OUTLINE if x in (x0, x0 + 5) or y == 6 + 12 - (i % 2) * 3 else (dark if (y + x) % 7 == 0 or x == x0 + 4 else c)
    return save(name, im)


def dhow(name, seed=85):
    """작은 다우 배 (돛 하나), 48×40 — 물 위 장식."""
    rng = random.Random(seed)
    w, h = 48, 40
    im = canvas(w, h)
    px = im.load()
    hull = _mask(w, h)
    for y in range(26, 36):
        t = (y - 26) / 9
        x0, x1 = int(4 + t * 7), int(44 - t * 4)
        _rect(hull, x0, y, x1, y)
    paint_mask(im, hull, WOOD, rng, light=(0, -1), speck=0.0)
    for x in range(5, 44):                             # 흰 띠
        if px[x, 28][3] and px[x, 28] != OUTLINE:
            px[x, 28] = (236, 226, 200, 255)
    for y in range(4, 27):                             # 돛대
        px[24, y] = OUTLINE; px[25, y] = WOOD[2]
    sail = _mask(w, h)
    for y in range(5, 24):
        t = (y - 5) / 18
        _rect(sail, int(25 - t * 2), y, int(26 + 14 * (1 - abs(t - 0.45) * 1.2)), y)
    paint_mask(im, sail, [(196, 180, 150, 255), (226, 212, 184, 255), (240, 232, 210, 255), (252, 248, 236, 255)], rng, light=(-1, 0), speck=0.0)
    for x in range(2, 47):                             # 물결
        if x % 6 < 3:
            px[x, 37] = (236, 244, 236, 200)
    return save(name, im)


def barrel(name, seed=86):
    rng = random.Random(seed)
    im = canvas(16, 18)
    shadow(im, 8, 15.5, 6, 1.5)
    m = _mask(16, 18)
    _rect(m, 3, 4, 12, 14); _ell(m, 7.5, 4, 4.8, 2); _ell(m, 7.5, 14, 4.8, 2)
    paint_mask(im, m, WOOD, rng, light=(-0.9, -0.2), speck=0.0)
    px = im.load()
    for y in (6, 12):
        for x in range(3, 13):
            if px[x, y][3]:
                px[x, y] = (96, 92, 90, 255)
    return save(name, im)


def crates(name, seed=87):
    rng = random.Random(seed)
    im = canvas(32, 28)
    shadow(im, 16, 25, 13, 1.8)
    px = im.load()
    for (x0, y0, x1, y1) in [(2, 12, 15, 25), (16, 12, 29, 25), (9, 1, 22, 13)]:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                edge = x in (x0, x1) or y in (y0, y1)
                diag = abs((x - x0) - (y - y0)) < 1 or abs((x1 - x) - (y - y0)) < 1
                px[x, y] = OUTLINE if edge else (WOOD[1] if diag else WOOD[3 if (y - y0) % 4 == 1 else 2])
    return save(name, im)


def cart(name, seed=88):
    """나무 손수레 (바퀴 둘), 32×24."""
    rng = random.Random(seed)
    im = canvas(32, 24)
    shadow(im, 16, 21, 13, 1.8)
    px = im.load()
    for y in range(6, 15):
        for x in range(3, 26):
            px[x, y] = OUTLINE if y in (6, 14) or x in (3, 25) else (WOOD[3] if y == 7 else WOOD[2] if (x // 4) % 2 else WOOD[1])
    for x in range(25, 31):
        px[x, 11] = OUTLINE; px[x, 12] = WOOD[2]
    for cx in (8, 20):
        for y in range(12, 24):
            for x in range(cx - 5, cx + 6):
                d = ((x + .5 - cx) ** 2 + (y + .5 - 18) ** 2) ** 0.5
                if d <= 5:
                    px[x, y] = OUTLINE if d > 4 or d < 1.2 else (WOOD[0] if abs(x + .5 - cx) < 1 or abs(y + .5 - 18) < 1 else WOOD[2])
    return save(name, im)


def milestone(name, seed=89):
    rng = random.Random(seed)
    im = canvas(16, 20)
    shadow(im, 8, 17.5, 5.5, 1.4)
    m = _mask(16, 20)
    _rect(m, 4, 5, 11, 17); _ell(m, 7.5, 5, 4, 3)
    paint_mask(im, m, STONE, rng, speck=0.0)
    px = im.load()
    for (x, y) in [(6, 8), (7, 8), (8, 8), (7, 9), (6, 11), (8, 11), (7, 12)]:
        px[x, y] = STONE[0]
    return save(name, im)


def net_rack(name, seed=90):
    """어망 말리는 대 (32×24)."""
    im = canvas(32, 24)
    shadow(im, 16, 21.5, 13, 1.6)
    px = im.load()
    for x0 in (2, 28):
        for y in range(4, 21):
            px[x0, y] = OUTLINE; px[x0 + 1, y] = WOOD[2]
    for x in range(2, 30):
        px[x, 4] = OUTLINE
    for y in range(5, 18):
        for x in range(4, 28):
            if (x + y) % 4 == 0 or (x - y) % 4 == 0:
                px[x, y] = (110, 96, 70, 255) if y < 17 else OUTLINE
    for x in (8, 15, 22):
        px[x, 17] = (230, 120, 60, 255)                 # 부표
    return save(name, im)


def ground_texture(name, base, seed=91, spots=None):
    """64×64 바닥 무늬 (이어 붙여도 이음새 없게 점무늬만)."""
    rng = random.Random(seed)
    im = Image.new('RGBA', (64, 64), base)
    px = im.load()
    for (c, n, size) in (spots or []):
        for _ in range(n):
            x, y = rng.randrange(64), rng.randrange(64)
            for dx in range(size):
                for dy in range(max(1, size - 1)):
                    px[(x + dx) % 64, (y + dy) % 64] = c
    return save(name, im)


def build_regions():
    boulder('drawn_boulder', seed=80)
    boulder('drawn_boulderSmall', w=16, h=14, seed=94, blobs=[(8, 8, 6.5, 5)])
    boulder('drawn_rockWall', w=48, h=36, seed=95,
            blobs=[(12, 22, 11, 11), (26, 16, 13, 13), (38, 23, 9.5, 10), (22, 27, 12, 7)])
    coffee('drawn_coffee')
    terrace_wall('drawn_terrace')
    dye_vat('drawn_vatIndigo', (52, 70, 160, 255), seed=83)
    dye_vat('drawn_vatMadder', (190, 46, 52, 255), seed=92)
    dye_vat('drawn_vatSaffron', (236, 170, 40, 255), seed=93)
    cloth_rack('drawn_clothRack', [(52, 82, 170, 255), (196, 54, 60, 255), (236, 176, 52, 255)])
    cloth_rack('drawn_clothRack2', [(60, 140, 110, 255), (150, 70, 160, 255), (240, 236, 220, 255)], seed=96)
    dhow('drawn_dhow')
    barrel('drawn_barrel')
    crates('drawn_crates')
    cart('drawn_cart')
    milestone('drawn_milestone')
    net_rack('drawn_netRack')
    ground_texture('drawn_texRock', (184, 152, 118, 255), spots=[((160, 128, 98, 255), 90, 2), ((206, 178, 142, 255), 70, 2), ((138, 110, 86, 255), 40, 1), ((150, 140, 110, 255), 20, 3)])
    ground_texture('drawn_texGravel', (196, 168, 128, 255), seed=97, spots=[((160, 136, 104, 255), 90, 1), ((220, 200, 166, 255), 60, 1), ((140, 120, 96, 255), 30, 2)])



# ---------------- 중앙 대륙 남쪽 구역 (사막 → 건초지 → 초원) ----------------
WOOL = [(150, 142, 132, 255), (204, 198, 186, 255), (232, 228, 216, 255), (250, 248, 240, 255)]
FELT = [(168, 156, 136, 255), (214, 204, 182, 255), (236, 228, 208, 255), (250, 246, 232, 255)]


def tuft_colored(name, stem, tip, seed=45):
    """풀 한 포기 (색 지정). 초원용 초록 풀."""
    rng = random.Random(seed)
    im = canvas(16, 16)
    px = im.load()
    base = 12
    for k in range(6):
        x = 3 + k * 2 + rng.randint(0, 1)
        hgt = rng.randint(3, 7)
        lean = rng.choice((-1, 0, 1))
        for j in range(hgt):
            xx = min(15, max(0, x + (lean if j > hgt // 2 else 0)))
            px[xx, base - j] = stem if j < hgt - 1 else tip
    return save(name, im)


def sheep(name, seed=101, flip=False):
    """작은 양 (16×14). 둥근 털 + 검은 얼굴·다리."""
    rng = random.Random(seed)
    w, h = 16, 14
    im = canvas(w, h)
    shadow(im, 8, h - 2, 6, 1.5)
    px = im.load()
    for lx in (4, 6, 10, 12):
        for y in range(h - 5, h - 2):
            px[lx, y] = OUTLINE
    m = _mask(w, h)
    _ell(m, 7.5, 6.5, 6, 4.2)
    _ell(m, 4.5, 5, 3, 2.6)
    _ell(m, 10, 4.6, 3.2, 2.8)
    paint_mask(im, m, WOOL, rng, speck=0.08)
    fx = 13 if not flip else 1
    for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2)):
        px[fx + dx if not flip else fx + dx, 4 + dy] = (60, 50, 46, 255)
    px[fx + (1 if not flip else 0), 4] = (120, 100, 90, 255)
    if flip:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
    return save(name, im)


def yurt(name, seed=102, door=(176, 48, 44, 255)):
    """초원의 둥근 천막 (게르) 48×40: 흰 펠트 벽 + 둥근 지붕 + 붉은 문."""
    rng = random.Random(seed)
    w, h = 48, 40
    im = canvas(w, h)
    shadow(im, 24, h - 3, 20, 3)
    px = im.load()
    wall = _mask(w, h)
    _rect(wall, 5, 18, 42, h - 4)
    paint_mask(im, wall, FELT, rng, light=(-0.9, -0.2), speck=0.0)
    roof = _mask(w, h)
    for y in range(4, 21):
        half = int(round(19.5 * ((y - 3) / 17) ** 0.55))
        for x in range(24 - half, 24 + half):
            roof[y][x] = True
    paint_mask(im, roof, FELT, rng, light=(-0.7, -0.7), speck=0.0)
    for x in range(6, 42):                     # 벽 위아래 끈 (파랑 무늬)
        for y in (21, h - 7):
            if px[x, y] != OUTLINE:
                px[x, y] = (60, 96, 170, 255) if (x // 2) % 2 else (236, 196, 80, 255)
    for y in range(6, 20):                      # 지붕 살
        for x0 in (16, 24, 32):
            xx = x0 + int((x0 - 24) * (20 - y) / 30)
            if 0 <= xx < w and px[xx, y][3] and px[xx, y] != OUTLINE:
                px[xx, y] = FELT[0]
    for x in range(21, 27):                     # 꼭대기 창 (토노)
        for y in range(3, 6):
            px[x, y] = OUTLINE if y == 3 or x in (21, 26) else (120, 84, 52, 255)
    for x in range(19, 29):                     # 문
        for y in range(h - 15, h - 4):
            edge = x in (19, 28) or y == h - 15
            px[x, y] = OUTLINE if edge else door if (x + y) % 5 else tuple(int(c * 0.8) for c in door[:3]) + (255,)
    px[26, h - 9] = (236, 196, 80, 255)
    return save(name, im)


def ovoo(name, seed=103):
    """돌무더기 서낭 (어워) 32×40: 쌓은 돌 + 장대 + 파란 천. 대륙 한가운데 표지."""
    rng = random.Random(seed)
    w, h = 32, 40
    im = canvas(w, h)
    shadow(im, 16, h - 3, 13, 2.5)
    px = im.load()
    for y in range(2, h - 12):                  # 장대
        px[15, y] = OUTLINE; px[16, y] = TRUNK[1]
    for i, c in enumerate([(52, 104, 196, 255), (236, 236, 228, 255), (220, 70, 60, 255), (240, 200, 70, 255), (70, 160, 90, 255)]):
        for y in range(4 + i * 3, 6 + i * 3):     # 천 (바람에 날림)
            for x in range(17, 25 - i):
                px[x, y] = c
    m = _mask(w, h)
    _ell(m, 16, h - 9, 13, 6)
    _ell(m, 16, h - 14, 9, 5)
    _ell(m, 16, h - 18, 5, 3.5)
    paint_mask(im, m, STONE, rng, speck=0.18)
    for _ in range(14):                         # 돌 이음
        x, y = rng.randrange(5, 27), rng.randrange(h - 20, h - 5)
        if px[x, y][3] and px[x, y] != OUTLINE:
            px[x, y] = STONE[0]
    return save(name, im)


def build_central():
    ground_texture('drawn_texDry', (198, 184, 112, 255), seed=104,
                   spots=[((176, 162, 92, 255), 110, 1), ((214, 204, 140, 255), 80, 1),
                          ((156, 160, 84, 255), 50, 2), ((186, 150, 96, 255), 20, 1)])
    tuft_colored('drawn_grassTuft', (78, 140, 60, 255), (138, 190, 84, 255), seed=46)
    tuft_colored('drawn_grassTuft2', (64, 124, 58, 255), (120, 176, 76, 255), seed=47)
    tuft_colored('drawn_dryTuft', (164, 150, 72, 255), (210, 196, 112, 255), seed=48)
    flower('drawn_wildflowerY', (246, 214, 70, 255), bush=False, seed=105, center=(255, 248, 200, 255))
    flower('drawn_wildflowerW', (244, 244, 236, 255), bush=False, seed=106, center=(240, 200, 80, 255))
    flower('drawn_wildflowerB', (110, 140, 226, 255), bush=False, seed=107, center=(250, 240, 200, 255))
    flower('drawn_wildflowerP', (226, 120, 176, 255), bush=False, seed=108, center=(255, 230, 120, 255))
    sheep('drawn_sheep', seed=101)
    sheep('drawn_sheep2', seed=109, flip=True)
    yurt('drawn_yurt', seed=102)
    yurt('drawn_yurt2', seed=110, door=(52, 96, 168, 255))
    ovoo('drawn_ovoo')
    import arabia_paint_world
    arabia_paint_world.build_world()


if __name__ == '__main__':
    build_all()
    print('drawn:', sorted(os.listdir(OUT)))
