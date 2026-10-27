"""아라비아 전용 그림 → 맵 타일(16px 조각) 도구와, 정돈된 아라비아 마을을 짜기 위한 ArabiaMap.

원본 그림: Assets/PocketCaravan/Art/Arabia/Sprites/** (사용자 제작 에셋. 시트 추출본은 sheet/).
조각 출력: Assets/PocketCaravan/Art/NinjaAdventure/Tiles/arabia_*.png
  (공용 MapJsonBuilder가 이 폴더의 타일만 읽기 때문. 이름은 모두 arabia_ 접두어라 다른 지역과 겹치지 않는다.)

그림 배치 (탑다운 3/4 시점)
- 그림을 16px 격자로 나눈다. 가로는 가운데, 세로는 아래쪽에 맞춘다.
- 아래 foot 줄 = 바닥에 닿는 부분: Objects 층(플레이어 아래). 가려진 비율이 기준 이상이면 막힘.
- 그 위 줄 = 지붕·수관: Above 층(플레이어 위), 막지 않음 → 뒤로 지나가면 가려진다.
- layer='deco' = 바닥 장식(깔개·꽃): Deco 층, 막지 않음.
- clip=True = 이미 찬 칸은 건너뛴다 (나무가 잘려 보이므로 숲 띠에는 쓰지 않는다).

바닥 (고려 맵처럼 경계를 다듬은 아라비아식 길)
- paving: 돌 포장 거리 + 사암 연석, 모서리 둥글게 (마을 안 거리)
- zellige: 광장 가운데 타일 무늬 + 얇은 남색 테두리
- road: 다져진 대상로(모래길), 들쭉날쭉한 부드러운 가장자리 (마을 밖·농원)
- grass: 오아시스 풀밭, 모래 쪽 가장자리를 부드럽게
- water: 호수·수로, 물거품 띠와 젖은 모래 가장자리
- bridge: 야자나무 판자 다리 + 난간 (물 위, 걸을 수 있음)
"""
import os
import zlib
from PIL import Image
from tiles import Map

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(ROOT, 'Assets/PocketCaravan/Art/Arabia/Sprites')
TILE_DIR = os.path.join(ROOT, 'Assets/PocketCaravan/Art/NinjaAdventure/Tiles')
T = 16

_paths = {}


def rescan():
    """그림 목록을 다시 읽는다 (arabia_paint가 drawn/ 그림을 새로 쓴 뒤)."""
    for r, _, fs in os.walk(SRC):
        for f in fs:
            if f.endswith('.png'):
                _paths.setdefault(f[:-4], os.path.join(r, f))


rescan()

USED = {}       # 타일 이름 → PIL 이미지 (내보낼 조각)
_cache = {}
_cells = {}


def sprite(name):
    if name not in _cache:
        if name not in _paths:
            raise KeyError(f'아라비아 그림 없음: {name}')
        _cache[name] = Image.open(_paths[name]).convert('RGBA')
    return _cache[name]


def cells(name):
    """그림을 16px 조각으로 나눈다. 반환: (가로칸, 세로칸, {(cx,cy): (조각, 불투명 비율)})"""
    if name in _cells:
        return _cells[name]
    im = sprite(name)
    cw, ch = -(-im.width // T), -(-im.height // T)
    canvas = Image.new('RGBA', (cw * T, ch * T))
    canvas.alpha_composite(im, ((cw * T - im.width) // 2, ch * T - im.height))
    out = {}
    for cy in range(ch):
        for cx in range(cw):
            tile = canvas.crop((cx * T, cy * T, cx * T + T, cy * T + T))
            alpha = tile.getchannel('A')
            if alpha.getbbox() is None:
                continue
            solid = sum(1 for a in alpha.getdata() if a >= 200) / (T * T)
            out[(cx, cy)] = (tile, solid)
    _cells[name] = (cw, ch, out)
    return _cells[name]


def key(name, cx, cy):
    # tiles.Map은 'sheet:x:y' 형식 키를 쓰고, 내보낼 때 ':'→'_'로 파일 이름이 된다.
    return f'arabia:{name}:{cx}:{cy}'


def size(name):
    cw, ch, _ = cells(name)
    return cw, ch


# 나무·덤불은 직접 그린 그림(arabia_paint.py, 고려 맵처럼 굵은 윤곽·둥근 형태)으로 통일한다.
# 맵 원본에서 예전 이름을 써도 여기서 바꾼다. 야자는 자리마다 세 가지 모양을 섞는다.
TREE_ALIAS = {
    'tree_palmDates': 'PALM', 'nature_datePalm': 'PALM', 'tree_doumPalm': 'PALM', 'nature_palmYoung': 'PALM',
    'nature_acacia': 'drawn_acaciaRound', 'tree_ghaf': 'drawn_acaciaRound', 'tree_sidr': 'drawn_acaciaRound',
    'tree_orange': 'drawn_orange', 'nature_pomegranateTree': 'drawn_pomegranate', 'tree_almond': 'drawn_almond',
    'tree_mulberry': 'drawn_oasisTree2', 'nature_figTree': 'drawn_oasisTree', 'tree_banana': 'drawn_lemon',
    'tree_olive': 'drawn_olive', 'nature_tamarisk': 'drawn_olive', 'nature_frankincenseTree': 'drawn_olive',
    'tree_dragonBlood': 'drawn_acaciaRound', 'tree_cypress': 'drawn_cypress', 'nature_shrub': 'drawn_sage',
    # 꽃·동물·우물·항아리도 같은 화풍으로 (사용자 피드백: 낙타가 너무 크고 픽셀이 거침, 우물이 잘 안 보임)
    'flower_rose': 'drawn_roseBush', 'flower_oleander': 'drawn_oleanderBush', 'flower_jasmine': 'drawn_jasmineBush',
    'flower_adenium': 'drawn_adenium', 'flower_saffron': 'SAFFRON', 'flower_lotus': 'drawn_lily',
    'animal_camel': 'drawn_camel', 'animal_camelRest': 'drawn_camelRest', 'animal_donkey': 'drawn_donkey',
    'plaza_well': 'drawn_well', 'building_well': 'drawn_well',
    'prop_zir': 'drawn_jar', 'prop_qulla': 'drawn_jarSmall', 'tree_pottedOrange': 'drawn_pottedOrange',
    'nature_rocks': 'drawn_rocks',
}
PALM_VARIANTS = ['drawn_datePalm', 'drawn_datePalm2', 'drawn_datePalm3']
# 가장자리 숲 (고려 맵처럼 둥근 나무를 빽빽하게): 위·아래 띠는 숲 덩어리와 야자, 옆 띠는 둥근 나무 기둥
EDGE_TOP = ['drawn_grove', 'drawn_oliveGrove', 'drawn_datePalm', 'drawn_datePalm3', 'drawn_oasisTree', 'drawn_datePalm2', 'drawn_olive']
EDGE_SIDE = ['drawn_oasisTree', 'drawn_oasisTree2', 'drawn_olive', 'drawn_acaciaRound', 'drawn_oasisTree']
ROCK_TOP = ['drawn_rockWall', 'drawn_boulder', 'drawn_rockWall', 'drawn_acaciaRound']
ROCK_SIDE = ['drawn_boulder', 'drawn_rockWall', 'drawn_boulder', 'drawn_olive']
HOUSE_PLANTS = ['drawn_oleander', 'drawn_sage', 'drawn_jasmine', 'drawn_bush']
DECOR_PREFIX = ('drawn_', 'tree_', 'nature_', 'flower_', 'prop_', 'animal_')


def alias(name, x, y):
    a = TREE_ALIAS.get(name, name)
    if a == 'PALM':
        a = PALM_VARIANTS[(x * 7 + y * 3) % 3]
    elif a == 'SAFFRON':
        a = ('drawn_saffron', 'drawn_saffron2')[(x + y) % 2]
    return a


def place(m, name, x, y, foot=1, block=True, solid=0.25, layer='auto', force=False, clip=False):
    """(x,y)=그림 격자의 왼쪽 위 칸. 겹치거나 길을 막게 되면 놓지 않고 False (clip이면 찬 칸만 건너뜀).
    나무 이름은 TREE_ALIAS로 직접 그린 나무로 바뀌고, 크기가 달라도 원래 자리의 아래·가운데에 맞춘다."""
    a = alias(name, x, y)
    if a != name:
        ow, oh = size(name)
        nw, nh = size(a)
        x, y, name = x + (ow - nw) // 2, y + (oh - nh), a
    cw, ch, parts = cells(name)
    if not clip and (x < 0 or y < 0 or x + cw > m.w or y + ch > m.h):
        return False
    ok = {}
    for (cx, cy), part in parts.items():
        xx, yy = x + cx, y + cy
        bottom = cy >= ch - foot
        bad = (not m.inside(xx, yy) or m.obj[yy][xx] or m.high[yy][xx]
               or (layer == 'deco' and name.startswith(('drawn_', 'flower_')) and m.inside(xx, yy)
                   and (m.road[yy][xx] > 0 or m.zone[yy][xx] in ('paving', 'zellige', 'road', 'bridge')))
               or (layer != 'deco' and (m.water[yy][xx] or getattr(m, 'bridges', {}).get((xx, yy))))
               or (not force and m.reserved[yy][xx] and layer != 'deco')
               or (bottom and block and layer == 'auto' and part[1] >= solid and m.road[yy][xx] > 0 and not force))
        if bad:
            if not clip:
                return False
            continue
        ok[(cx, cy)] = part
    if clip and not any(cy >= ch - foot for (_, cy) in ok):
        return False
    for (cx, cy), (tile, s) in ok.items():
        xx, yy = x + cx, y + cy
        k = key(name, cx, cy)
        USED[k.replace(':', '_')] = tile
        if layer == 'deco':
            m.deco[yy][xx] = k
        elif cy >= ch - foot:
            m.obj[yy][xx] = k
            if block and s >= solid:
                m.blocked[yy][xx] = True
                m.road[yy][xx] = 0
        else:
            m.high[yy][xx] = k
        m.reserved[yy][xx] = True
    m.stamps.append((name, x, y))
    return True


def block_rect(m, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if m.inside(x, y):
                m.blocked[y][x] = True
                m.reserved[y][x] = True
                if hasattr(m, 'hard'):
                    m.hard.add((x, y))


def unplace(m, name, x, y):
    """놓았던 그림을 지운다 (사람을 가리는 장식 정리용)."""
    cw, ch, parts = cells(name)
    for (cx, cy) in parts:
        xx, yy = x + cx, y + cy
        k = key(name, cx, cy)
        for layer in (m.obj, m.high, m.deco):
            if m.inside(xx, yy) and layer[yy][xx] == k:
                layer[yy][xx] = None
                if layer is m.obj and (xx, yy) not in m.hard and m.zone[yy][xx] != 'water':
                    m.blocked[yy][xx] = False
    m.stamps.remove((name, x, y))


# ---------- 바닥 무늬와 경계 ----------
TEX = {'sand': 'tile_sand', 'road': 'tile_road', 'paving': 'tile_paving', 'grass': 'tile_oasisGrass',
       'water': 'tile_water', 'zellige': 'tile_zellige', 'brick': 'tile_mudBrick', 'dune': 'tile_dune',
       'rock': 'drawn_texRock', 'gravel': 'drawn_texGravel', 'dry': 'drawn_texDry',
       'red': 'drawn_texRed', 'cobble': 'drawn_texCobble', 'boards': 'drawn_texBoards'}
STREET = {'paving', 'zellige'}
# 경계를 그리지 않고 이어지는 바닥 묶음
JOIN = {'paving': {'paving', 'zellige', 'road', 'bridge'}, 'zellige': {'zellige'},
        'road': {'road', 'paving', 'zellige', 'bridge', 'gravel'},
        'gravel': {'gravel', 'road', 'paving', 'bridge'}, 'rock': {'rock'},
        'grass': {'grass', 'water', 'bridge'}, 'water': {'water', 'bridge'}, 'brick': {'brick'}, 'dune': {'dune'},
        'dry': {'dry', 'grass', 'water', 'bridge'},   # 건초지: 초원 쪽 경계는 초원이 그린다
        'red': {'red'}, 'cobble': {'cobble', 'boards', 'bridge', 'zellige'}, 'boards': {'boards', 'bridge'}}
UNDER = ('grass', 'dry', 'sand', 'red')       # 경계 바깥에 깔 바닥 후보


def tex(kind, x, y):
    im = sprite(TEX[kind])
    n = im.width // T
    cx, cy = x % n, y % n
    return im.crop((cx * T, cy * T, cx * T + T, cy * T + T))


def _noise(*a):
    return zlib.crc32(repr(a).encode()) & 0xFFFF


def compose(kind, under, mask, x, y):
    """mask: 8방향 이웃이 같은 묶음인지 (N,E,S,W,NE,SE,SW,NW 순 비트). 이웃 쪽이 다르면 경계를 그린다."""
    N, E, S, W, NE, SE, SW, NW = [(mask >> i) & 1 for i in range(8)]
    tile = tex(kind, x, y).copy()
    base = tex(under, x, y)
    px, bp = tile.load(), base.load()
    open_side = {'N': not N, 'E': not E, 'S': not S, 'W': not W}

    def dist(i, j):
        """가장 가까운 열린 변까지 거리 (모서리 둥글림 포함)."""
        d = 99
        if open_side['N']: d = min(d, j)
        if open_side['S']: d = min(d, T - 1 - j)
        if open_side['W']: d = min(d, i)
        if open_side['E']: d = min(d, T - 1 - i)
        # 바깥 모서리 둥글게 (두 변이 모두 열림)
        for ox, oy, a, b in (('W', 'N', i, j), ('E', 'N', T - 1 - i, j), ('W', 'S', i, T - 1 - j), ('E', 'S', T - 1 - i, T - 1 - j)):
            if open_side[ox] and open_side[oy] and a < 4 and b < 4:
                r = 4 - ((4 - a) ** 2 + (4 - b) ** 2) ** 0.5
                d = min(d, r)
        # 안쪽 모서리 (대각선만 열림)
        for diag, (a, b) in (((N and E and not NE), (T - 1 - i, j)), ((N and W and not NW), (i, j)),
                             ((S and E and not SE), (T - 1 - i, T - 1 - j)), ((S and W and not SW), (i, T - 1 - j))):
            if diag and a < 3 and b < 3:
                d = min(d, max(a, b))
        return d

    for j in range(T):
        for i in range(T):
            d = dist(i, j)
            if d >= 6:
                continue
            if kind in STREET:
                if kind == 'zellige':
                    if d < 1: px[i, j] = (44, 78, 112, 255)
                    continue
                if d < 0.5: px[i, j] = bp[i, j]
                elif d < 1.5: px[i, j] = (104, 74, 46, 255)          # 연석 바깥 윤곽
                elif d < 3.5: px[i, j] = (222, 190, 136, 255) if (i + j) % 5 else (204, 168, 116, 255)  # 사암 연석
                elif d < 4.5: px[i, j] = (170, 140, 100, 255)        # 연석 안쪽 그늘
            elif kind in ('cobble', 'boards'):
                if d < 0.5: px[i, j] = bp[i, j]
                elif d < 1.5: px[i, j] = (64, 58, 56, 255) if kind == 'cobble' else (70, 46, 30, 255)
                elif d < 2.5 and kind == 'cobble': px[i, j] = (118, 114, 120, 255)
            elif kind in ('road', 'gravel', 'rock', 'dune', 'brick', 'red'):
                jag = (_noise(x % 4, y % 4, i, j) % 3)
                if d < jag: px[i, j] = bp[i, j]
                elif d < jag + 1: px[i, j] = (176, 140, 96, 255)
            elif kind == 'grass':
                jag = (_noise('g', x % 4, y % 4, i, j) % 3)
                if d < jag: px[i, j] = bp[i, j]
                elif d < jag + 1: px[i, j] = (92, 128, 58, 255)
            elif kind == 'dry':
                jag = (_noise('y', x % 4, y % 4, i, j) % 3)
                if d < jag: px[i, j] = bp[i, j]
                elif d < jag + 1: px[i, j] = (160, 146, 78, 255)
            elif kind == 'water':
                if d < 1: px[i, j] = bp[i, j]
                elif d < 2: px[i, j] = (222, 196, 150, 255)         # 젖은 모래
                elif d < 3: px[i, j] = (236, 244, 236, 255)         # 물거품
                elif d < 4: px[i, j] = (120, 190, 200, 255)
            else:
                if d < 1: px[i, j] = (110, 80, 50, 255)
    return tile


def bridge_tile(axis, edge, x, y):
    """야자나무 판자 다리. axis='h'(동서로 건넘)/'v'(남북). edge: 'a'/'b'=난간 쪽(위·아래 또는 왼·오른), 'm'=가운데.
    판자는 건너는 방향과 직각, 바깥 변에 난간과 기둥."""
    tile = tex('water', x, y).copy()
    px = tile.load()
    wood, seam, dark = (190, 136, 84, 255), (146, 98, 58, 255), (92, 60, 36, 255)
    rail, post = (124, 82, 48, 255), (84, 54, 32, 255)
    for j in range(T):
        for i in range(T):
            u, v = (i, j) if axis == 'h' else (j, i)      # u = 건너는 방향, v = 다리 폭 방향
            px[i, j] = seam if u % 4 == 3 else wood
            if (u * 7 + v * 3 + x * 5 + y) % 23 == 0:
                px[i, j] = seam                            # 나뭇결 점
            if (edge == 'a' and v < 4) or (edge == 'b' and v > T - 5):
                vv = v if edge == 'a' else T - 1 - v
                if vv == 0:
                    px[i, j] = dark
                elif vv in (1, 2):
                    px[i, j] = post if u % 8 in (0, 1) else rail
                else:
                    px[i, j] = (110, 72, 42, 255)          # 난간 그림자
    return tile


def bridge_key(axis, edge, x, y):
    k = f'arabia:tbridge{axis}{edge}:{x % 4}:{y % 4}'
    name = k.replace(':', '_')
    if name not in USED:
        USED[name] = bridge_tile(axis, edge, x % 4, y % 4)
    return k


def ground_key(kind, under, mask, x, y):
    n = sprite(TEX[kind]).width // T
    u = {'dry': 'y', 'red': 'r'}.get(under, under[0])   # 예전 이름(s·g) 유지, 건초지 y, 붉은 바위 r
    k = f'arabia:t{kind}{u}{mask}:{x % n}:{y % n}'
    name = k.replace(':', '_')
    if name not in USED:
        USED[name] = compose(kind, under, mask, x, y) if mask != 255 else tex(kind, x, y)
    return k


class ArabiaMap(Map):
    """아라비아 맵: 바닥 구역(zone) + 그림 배치 + 정돈 도구. 마지막에 finish_arabia()."""

    def __init__(self, *a, **k):
        super().__init__(*a, **k)
        self.zone = [['sand'] * self.w for _ in range(self.h)]
        self.bridges = {}     # (x,y) → (축, 난간 쪽)
        self.hard = set()     # block_rect로 막은 칸 (그림을 지워도 막힘 유지)
        self.warnings = []
        self.pending_plants = []

    # 바닥
    def paint(self, kind, x0, y0, x1, y1, grade=0, reserve=True):
        for y in range(min(y0, y1), max(y0, y1) + 1):
            for x in range(min(x0, x1), max(x0, x1) + 1):
                if self.inside(x, y):
                    self.zone[y][x] = kind
                    if kind == 'water':
                        self.water[y][x] = True
                        self.reserved[y][x] = True
                    else:
                        self.water[y][x] = False      # 물 위에 칠하면 물이 아니다 (막힘 해제)
                    if grade:
                        self.road[y][x] = max(self.road[y][x], grade)
                    if reserve:
                        self.reserved[y][x] = True

    def bridge(self, x0, y0, x1, y1, axis='h', grade=2):
        """판자 다리. axis='h'면 동서로 건넌다 (위·아래 변에 난간). 걸을 수 있는 길로 친다."""
        self.paint('bridge', x0, y0, x1, y1, grade=grade)
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if axis == 'h':
                    edge = 'a' if y == y0 else 'b' if y == y1 else 'm'
                else:
                    edge = 'a' if x == x0 else 'b' if x == x1 else 'm'
                self.bridges[(x, y)] = (axis, edge)
                self.blocked[y][x] = False

    def paint_path(self, kind, pts, width=2, grade=2):
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            if ax == bx:
                self.paint(kind, ax, ay, ax + width - 1, by, grade)
            elif ay == by:
                self.paint(kind, ax, ay, bx, ay + width - 1, grade)
            else:
                raise ValueError('경로는 가로·세로 구간만')

    def ellipse(self, kind, cx, cy, rx, ry, grade=0, reserve=True):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if self.inside(x, y) and ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                    self.paint(kind, x, y, x, y, grade, reserve=reserve)

    # 그림
    def put(self, name, x, y, **kw):
        if not place(self, name, x, y, **kw):
            raise ValueError(f'배치 실패 {name} @({x},{y})')

    def maybe(self, name, x, y, **kw):
        return place(self, name, x, y, **kw)

    def building(self, name, x, y, lane_y, kind='paving', foot=2, door=None):
        """건물 + 문 앞에서 길(lane_y)까지 이어지는 1칸 포장길. door=문 x(기본 가운데)."""
        cw, ch = size(name)
        self.put(name, x, y, foot=foot)
        # 집 양옆 작은 화단 (있을 자리만)
        plant = HOUSE_PLANTS[(x + y) % len(HOUSE_PLANTS)]
        self.pending_plants += [(plant, x - 1, y + ch - 1), (plant, x + cw, y + ch - 1)]   # 마무리 때 빈 자리만
        dx = x + (cw // 2 if door is None else door)
        y0 = y + ch
        if lane_y > y0:
            self.paint(kind, dx, y0, dx, lane_y - 1, grade=1)
        return dx, y0

    def asign(self, x, y, text):
        """아라비아 이정표 그림(2×2) + 말 걸 수 있는 이정표. 아래 왼쪽 칸이 대화 지점."""
        self.put('prop_signpost', x, y, foot=1, force=True)
        block_rect(self, x, y + 1, x + 1, y + 1)
        self.add('sign', x, y + 1, text=text)

    def exit(self, L, key, start):
        """연결표(arabia.links)의 출구 하나를 4칸 폭으로 만든다."""
        edge, target, tspawn, myspawn = L[key]
        self.link(edge, start, 4, target, tspawn, myspawn)

    def rocks(self, x0, y0, x1, y1, density=0.75):
        """자연스러운 바위 무더기: 큰 바위 → 중간 → 작은 돌 순으로 엇갈려 채운다 (보이는 바위 발밑만 막힘)."""
        cells_ = [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)]
        cells_.sort(key=lambda c: _noise('rk', c[0], c[1], x0, y0))
        for name in ('drawn_rockWall', 'drawn_boulder', 'drawn_boulderSmall'):
            cw, ch = size(name)
            for (x, y) in cells_:
                dens = density if name != 'drawn_boulderSmall' else density * 0.25
                if x + cw - 1 > x1 or y + ch - 1 > y1 or _noise('rd', x, y, name) % 100 > dens * 100:
                    continue
                place(self, name, x, y, foot=1)

    def border(self, names, x0, y0, x1, y1, step=None, block=True, kind='tree'):
        """맵 가장자리 숲 띠. 나무를 자르지 않고 통째로 나란히 심는다 (겹치거나 맵 밖으로 나가면 다른 나무·다음 자리).
        가로 띠는 (x0..x1, 줄기 줄 y1), 세로 띠는 (x0..x1 가운데 열, 줄기 y0..y1).
        block=True면 띠 영역(맵 끝까지)을 막아 숲 뒤로 돌아가는 샛길을 없앤다."""
        horizontal = (x1 - x0) >= (y1 - y0)
        if kind == 'rock':
            order = list(ROCK_TOP if horizontal else ROCK_SIDE)
        else:
            order = list(EDGE_TOP if horizontal else EDGE_SIDE)   # 그림은 통일된 숲 묶음으로
        if horizontal:
            x = x0
            while x <= x1:
                self.rng.shuffle(order)
                for name in order:
                    a = alias(name, x, y1)
                    cw, ch = size(a)
                    if x + cw - 1 > min(x1 + 1, self.w - 1):
                        continue
                    if place(self, a, x, y1 - ch + 1, foot=1, force=True):
                        x += cw
                        break
                else:
                    x += 1
            if block:
                if y1 < self.h // 2:
                    block_rect(self, x0, 0, x1, y1)
                else:
                    block_rect(self, x0, y0, x1, self.h - 1)
        else:
            y = y0
            while y <= y1:
                self.rng.shuffle(order)
                for name in order:
                    a = alias(name, x0, y)
                    cw, ch = size(a)
                    left = 0 if x1 < self.w // 2 else self.w - cw     # 맵 끝에 붙여 통째로
                    if place(self, a, left, y - ch + 1, foot=1, force=True):
                        y += 2
                        break
                else:
                    y += 1
            if block:
                if x1 < self.w // 2:
                    block_rect(self, 0, y0, x1, y1)
                else:
                    block_rect(self, x0, y0, self.w - 1, y1)

    def texture(self, ratio=0.05):
        """모래 칸에 마른 풀·자갈을 드문드문 (고려 맵의 풀 무늬처럼 바닥에 결을 준다)."""
        names = ['drawn_tuft', 'drawn_tuft2', 'drawn_tuft', 'drawn_pebbles']
        for y in range(1, self.h - 1):
            for x in range(1, self.w - 1):
                if self.zone[y][x] != 'sand' or self.road[y][x] or self.obj[y][x] or self.high[y][x] or self.deco[y][x]:
                    continue
                n = _noise('tx', x, y)
                if n % 1000 < ratio * 1000 and not any(self.deco[y + dy][x + dx] for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                    place(self, names[n % len(names)], x, y, layer='deco')

    def scatter(self, name, count, area, margin=1, **kw):
        placed = tries = 0
        cw, ch = size(name)
        while placed < count and tries < count * 60:
            tries += 1
            x, y = self.rng.randint(area[0], area[2]), self.rng.randint(area[1], area[3])
            if self.can_place(x - margin, y - margin, cw + 2 * margin, ch + 2 * margin) and place(self, name, x, y, **kw):
                placed += 1
        return placed

    PEOPLE = ('npc', 'talker', 'villager', 'market', 'board', 'sign', 'spawn')

    def keep_people_visible(self):
        """사람·게시판·이정표 칸과 그 위 칸(머리)을 가리는 그림을 정리한다.
        장식(나무·소품·꽃)은 지우고, 건물이면 경고로 남긴다."""
        feet, heads = set(), set()
        for o in self.objects:
            if o['kind'] in self.PEOPLE:
                for yy in range(o['y'], o['y'] + o.get('h', 1)):
                    for xx in range(o['x'], o['x'] + o.get('w', 1)):
                        feet.add((xx, yy))
                        if o['kind'] not in ('spawn', 'sign', 'board'):
                            heads.add((xx, yy - 1))
        for name, x, y in list(self.stamps):
            if name in ('prop_signpost', 'plaza_board'):
                continue          # 이정표·게시판 그림은 그 표지 자신
            if name not in _paths:
                continue          # 공용 닌자 타일 스탬프 (소나무·기와집 등): 아라비아 그림이 아님
            cw, ch, parts = cells(name)
            hit = []
            for (cx, cy) in parts:
                c = (x + cx, y + cy)
                k = key(name, cx, cy)
                if (c in feet and (self.high[c[1]][c[0]] == k or self.obj[c[1]][c[0]] == k)) or \
                        (c in heads and self.high[c[1]][c[0]] == k):
                    hit.append(c)
            if not hit:
                continue
            if name.startswith(DECOR_PREFIX) and not name.startswith('prop_signpost'):
                unplace(self, name, x, y)
            else:
                self.warnings.append(f'{self.id}: {name}@({x},{y})가 사람/표지 칸 {hit[:2]}을 가림')

    def finish_arabia(self):
        for plant, x, y in self.pending_plants:
            if self.inside(x, y) and self.inside(x, y + 1) and self.road[y][x] == 0:
                place(self, plant, x, y, foot=1)
        self.keep_people_visible()
        self.finish()   # 공용: 길 홈 메우기·물 막기 (닌자 바닥은 아래에서 아라비아 바닥으로 바꾼다)
        self.objects = [o for o in self.objects if o['kind'] != 'anim']
        for y in range(self.h):
            for x in range(self.w):
                if self.deco[y][x] and not str(self.deco[y][x]).startswith('arabia:'):
                    self.deco[y][x] = None
                z = self.zone[y][x]
                if z == 'water':
                    self.blocked[y][x] = True
                if z == 'sand':
                    self.ground[y][x] = ground_key('sand', 'sand', 255, x, y)
                    continue
                if z == 'bridge':
                    self.ground[y][x] = bridge_key(*self.bridges.get((x, y), ('h', 'm')), x, y)
                    self.blocked[y][x] = False
                    continue
                join = JOIN.get(z, {z})
                mask = 0
                for bit, (dx, dy) in enumerate(((0, -1), (1, 0), (0, 1), (-1, 0), (1, -1), (1, 1), (-1, 1), (-1, -1))):
                    nx, ny = x + dx, y + dy
                    same = (not self.inside(nx, ny)) or self.zone[ny][nx] in join
                    mask |= same << bit
                near = [self.zone[y + dy][x + dx] for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0))
                        if self.inside(x + dx, y + dy) and self.zone[y + dy][x + dx] not in join]
                if z == 'water':
                    under = 'grass' if 'grass' in [self.zone[y + dy][x + dx] for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0))
                                                   if self.inside(x + dx, y + dy)] else 'sand'
                elif getattr(self, 'soft_under', False):
                    under = max(UNDER, key=lambda u: (near.count(u), u == 'sand'))
                else:
                    under = 'sand'
                self.ground[y][x] = ground_key(z, under, mask, x, y)


def export_tiles():
    """사용한 조각만 Tiles 폴더에 쓴다. 내용이 같으면 다시 쓰지 않는다 (.meta·GUID 유지)."""
    written = 0
    for name, tile in USED.items():
        path = os.path.join(TILE_DIR, name + '.png')
        if os.path.exists(path):
            with Image.open(path) as old:
                if old.size == tile.size and old.convert('RGBA').tobytes() == tile.tobytes():
                    continue
        tile.save(path)
        written += 1
    return written


# ---------- 캐릭터 프레임 ----------
CHAR_SRC = os.path.join(ROOT, 'Assets/PocketCaravan/Art/Arabia/Characters')
CHAR_DIR = os.path.join(ROOT, 'Assets/PocketCaravan/Art/Characters')


def _is_skin(c):
    import colorsys
    r, g, b, a = c
    if a < 128:
        return False
    h, sat, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return 0.03 < h < 0.12 and 0.2 < sat < 0.65 and v > 0.45


def _face_box(im):
    """머리 부분에서 피부가 가장 많이 모인 영역 (얼굴) 상자."""
    px = im.load()
    w, h = im.size
    bb = im.getbbox()
    top = bb[1]
    rows = range(top + 6, min(h, top + 21))
    pts = [(x, y) for y in rows for x in range(w) if _is_skin(px[x, y])]
    if not pts:
        return None
    # 가운데 기둥에 가까운 피부만 (손·소품 제외)
    cx = (bb[0] + bb[2]) / 2
    pts = [(x, y) for x, y in pts if abs(x + .5 - cx) < 10]
    if not pts:
        return None
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def _hair_color(im, box):
    from collections import Counter
    px = im.load()
    cnt = Counter()
    for y in range(max(0, box[1] - 3), box[1]):
        for x in range(box[0], box[2] + 1):
            c = px[x, y]
            if c[3] == 255 and not _is_skin(c) and sum(c[:3]) > 120:
                cnt[c] += 1
    if not cnt:
        return (60, 40, 30, 255)
    return cnt.most_common(1)[0][0]


def _edge(im, x, y):
    px = im.load()
    w, h = im.size
    return any(not (0 <= x + dx < w and 0 <= y + dy < h) or px[x + dx, y + dy][3] < 128
               for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))


def _back_view(base):
    im = base.copy()
    box = _face_box(im)
    if not box:
        return im
    hair = _hair_color(im, box)
    dark = tuple(int(c * 0.82) for c in hair[:3]) + (255,)
    px = im.load()
    x0, y0, x1, y1 = box[0] - 1, box[1] - 1, box[2] + 1, box[3]
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if px[x, y][3] >= 128 and not _edge(base, x, y):
                px[x, y] = dark if y >= y1 - 1 else hair
    return im


def _side_view(base):
    """오른쪽을 보는 옆모습: 얼굴을 오른쪽으로 2px 옮기고 왼쪽은 머리카락·두건으로 덮는다."""
    im = base.copy()
    box = _face_box(im)
    if not box:
        return im
    hair = _hair_color(im, box)
    src = base.load()
    px = im.load()
    x0, y0, x1, y1 = box
    for y in range(y0, y1 + 1):
        for x in range(x1, x0 - 1, -1):
            sx = x - 2
            if px[x, y][3] < 128 or _edge(base, x, y):
                continue
            if sx >= x0 and src[sx, y][3] >= 128:
                px[x, y] = src[sx, y]
            else:
                px[x, y] = hair
    return im


def _walk(im, frame, side=False):
    """걷기 프레임: 1·3에서 몸이 1px 뜨고, 발 한쪽씩 들린다 (옆모습은 앞뒤로 벌림)."""
    if frame % 2 == 0:
        return im
    out = Image.new('RGBA', im.size)
    out.alpha_composite(im, (0, -1))
    bb = im.getbbox()
    feet_top = bb[3] - 3
    cx = (bb[0] + bb[2]) // 2
    src = im.load()
    px = out.load()
    for y in range(feet_top, bb[3]):
        for x in range(bb[0], bb[2]):
            px[x, y - 1] = (0, 0, 0, 0)
    for y in range(feet_top, bb[3]):
        for x in range(bb[0], bb[2]):
            c = src[x, y]
            if c[3] == 0:
                continue
            left = x < cx
            lift = (left if frame == 1 else not left)
            if side:
                dx = (1 if left else -1) * (1 if frame == 1 else -1)
                tx, ty = x + dx, y
            else:
                tx, ty = x, y - (1 if lift else 0)
            if 0 <= tx < im.width and 0 <= ty < im.height:
                px[tx, ty] = c
    return out


def export_characters():
    """정면 1장짜리 아라비아 캐릭터(32×40)로 4방향 걷기 프레임(공용 로더 형식 16장)을 만든다.
    이름: arabia_<원본이름>_tall_[dulr][0-3].png. 32×48 캔버스 아래 8px를 비운다.
    - 아래: 원본 / 위: 얼굴을 머리카락·두건 색으로 덮은 뒷모습 / 오른쪽: 얼굴을 2px 옮긴 옆모습 / 왼쪽: 그 좌우 반전
    - 걷기: 1·3 프레임에서 1px 들썩이고 발을 번갈아 든다.
    Unity에서는 ArabiaSceneDressing이 PPU 32·기준점(0.5,0.42)으로 가져와 주인공 너구리와 같은 크기로 보이게 한다."""
    written = 0
    for f in sorted(os.listdir(CHAR_SRC)):
        if not f.endswith('.png'):
            continue
        key_ = f[:-4]
        src = Image.open(os.path.join(CHAR_SRC, f)).convert('RGBA')
        base = Image.new('RGBA', (32, 48))
        base.alpha_composite(src, ((32 - src.width) // 2, 48 - 8 - src.height))
        views = {'d': base, 'u': _back_view(base), 'r': _side_view(base)}
        views['l'] = views['r'].transpose(Image.FLIP_LEFT_RIGHT)
        for d in 'dulr':
            for i in range(4):
                c = _walk(views[d], i, side=d in 'lr')
                path = os.path.join(CHAR_DIR, f'arabia_{key_}_tall_{d}{i}.png')
                if os.path.exists(path):
                    with Image.open(path) as old:
                        if old.convert('RGBA').tobytes() == c.tobytes():
                            continue
                c.save(path)
                written += 1
    return written
