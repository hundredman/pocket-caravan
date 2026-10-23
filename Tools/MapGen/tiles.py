"""Ninja Adventure 타일 카탈로그, 자동 타일(오토타일), 미리보기."""
import os, random, colorsys
from PIL import Image, ImageDraw

# 에셋 팩 위치: 환경 변수 NA_PACK, 없으면 저장소 옆 폴더 (팀 프로젝트/Ninja Adventure - Asset Pack)
PACK = os.environ.get("NA_PACK", os.path.join(os.path.dirname(__file__), "..", "..", "..", "Ninja Adventure - Asset Pack"))
SHEETS = {
    "floor": "Backgrounds/Tilesets/TilesetFloor.png",
    "detail": "Backgrounds/Tilesets/TilesetFloorDetail.png",
    "house": "Backgrounds/Tilesets/TilesetHouse.png",
    "nature": "Backgrounds/Tilesets/TilesetNature.png",
    "element": "Backgrounds/Tilesets/TilesetElement.png",
    "water": "Backgrounds/Tilesets/TilesetWater.png",
    "field": "Backgrounds/Tilesets/TilesetField.png",
    "boat": "Backgrounds/Vehicles/Boat.png",
    "towers": "Backgrounds/Tilesets/TilesetTowers.png",
}
_img = {}


def sheet(name):
    if name not in _img:
        _img[name] = Image.open(os.path.join(PACK, SHEETS[name])).convert("RGBA")
    return _img[name]


def recolor(tile, mode):
    """기와 지붕 같은 색 바꾸기. 'slate': 붉은·주황 계열을 청회색 기와로."""
    px = tile.load()
    w, h = tile.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            hh, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if mode == "slate" and s > 0.35 and (hh < 0.06 or hh > 0.93):  # 붉은 기와만
                nh, ns = 0.62, min(s * 0.35, 0.3)
                nr, ng, nb = colorsys.hsv_to_rgb(nh, ns, v * 0.85)
                px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return tile


def tile_image(key):
    """key = 'sheet:x:y' 또는 'sheet:x:y@recolor'"""
    if key.startswith(("goryeo:", "goryeo_")):
        from goryeo_art import image_for
        return image_for(key)
    if "@goryeo_" in key:
        from goryeo_art import tone
        base, theme = key.rsplit("@goryeo_", 1)
        return tone(base, theme)
    if key.startswith("module:"):
        from module_art import image_for
        return image_for(key)
    if key.startswith("work:"):
        # 3x2칸 종이 건조대. 기존 타일 축척·팔레트에 맞춘 목재 틀과 한지 묶음.
        _, tx, ty = key.split(":")
        t = Image.new("RGBA", (48, 32))
        d = ImageDraw.Draw(t)
        d.rectangle((4, 27, 44, 30), fill="#65544255")
        for x in (3, 42):
            d.rectangle((x, 1, x + 3, 29), fill="#70513b")
            d.line((x, 2, x, 27), fill="#bb9660")
        for y in (3, 23):
            d.rectangle((3, y, 45, y + 2), fill="#795439")
            d.line((4, y, 44, y), fill="#c19a63")
        for x in (8, 20, 32):
            d.rectangle((x, 7, x + 8, 22), fill="#c7be97")
            d.rectangle((x, 6, x + 7, 20), fill="#eee2b6")
            d.line((x + 1, 7, x + 6, 7), fill="#fff2cc")
            d.point((x + 3, 12), fill="#d8ca9f")
            d.point((x + 5, 17), fill="#d8ca9f")
        x, y = int(tx) * 16, int(ty) * 16
        return t.crop((x, y, x + 16, y + 16))
    if key.startswith("wood:"):
        _, direction, post = key.split(":")
        t = Image.new("RGBA", (16, 16))
        d = ImageDraw.Draw(t)
        d.rectangle((1, 1, 3, 15), fill="#533d2f")
        d.rectangle((1, 1, 2, 15), fill="#ad8050")
        if post == "1":
            d.rectangle((0, 1, 5, 6), fill="#4c372c")
            d.rectangle((1, 1, 4, 4), fill="#d0a66c")
            d.point((2, 2), fill="#f0cd8c")
        if direction == "railhtop":
            return t.transpose(Image.Transpose.ROTATE_270)
        if direction == "railhbottom":
            return t.transpose(Image.Transpose.ROTATE_90)
        if direction == "railvright":
            return t.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        return t
    base, _, rc = key.partition("@")
    if rc.startswith("blend_"):
        from Modules.skins import SKINS, recolor as skin_recolor
        _,skin_id,amount=rc.split('_')
        original=tile_image(base)
        return Image.blend(original,skin_recolor(original,base,next(s for s in SKINS if s[0]==skin_id)),int(amount)/100)
    if rc.startswith("skin_"):
        from Modules.skins import SKINS, recolor as skin_recolor
        skin = next(s for s in SKINS if s[0] == rc[5:])
        return skin_recolor(tile_image(base), base, skin)
    s, x, y = base.split(":")
    x, y = int(x), int(y)
    t = sheet(s).crop((x * 16, y * 16, x * 16 + 16, y * 16 + 16))
    if rc.startswith("wood"):
        t = t.copy()
        px = t.load()
        for yy in range(16):
            for xx in range(16):
                r, g, b, a = px[xx, yy]
                px[xx, yy] = (int(r * .67), int(g * .76), min(255, int(b * .9 + 9)), a)
        if rc == "woodturn":
            t = t.transpose(Image.Transpose.ROTATE_90)
    elif rc:
        t = recolor(t.copy(), rc)
    return t


def K(s, x, y, rc=None):
    return f"{s}:{x}:{y}" + (f"@{rc}" if rc else "")


# ---------- 오토타일 ----------
# 블록 배치 (원점 기준): 3x3 덩어리, 세로 띠, 가로 띠, 외톨이, 안쪽 모서리 4개
def autotile(mask, x, y, sheet_name, ox, oy):
    def m(dx, dy):
        return mask(x + dx, y + dy)
    n, s, w, e = m(0, -1), m(0, 1), m(-1, 0), m(1, 0)
    rel = None
    if n and s and w and e:
        if not m(1, 1): rel = (5, 1)
        elif not m(-1, 1): rel = (6, 1)
        elif not m(1, -1): rel = (5, 2)
        elif not m(-1, -1): rel = (6, 2)
        else: rel = (1, 1)
    elif n and s and e: rel = (0, 1)
    elif n and s and w: rel = (2, 1)
    elif s and w and e: rel = (1, 0)
    elif n and w and e: rel = (1, 2)
    elif s and e: rel = (0, 0)
    elif s and w: rel = (2, 0)
    elif n and e: rel = (0, 2)
    elif n and w: rel = (2, 2)
    elif n and s: rel = (3, 1)
    elif w and e: rel = (1, 3)
    elif s: rel = (3, 0)
    elif n: rel = (3, 2)
    elif e: rel = (0, 3)
    elif w: rel = (2, 3)
    else: rel = (3, 3)
    return K(sheet_name, ox + rel[0], oy + rel[1])


GRASS = K("floor", 0, 12)
GRASS_VARIANTS = [K("floor", 1, 12), K("floor", 2, 12), K("floor", 3, 12), K("floor", 4, 12), K("floor", 2, 11), K("floor", 3, 11)]
DIRT_ORIGIN = ("floor", 0, 7)      # 풀밭 위 흙길
TOWN = K("floor", 0, 5)            # 도시 바닥 (다져진 흙)
TOWN_VARIANTS = [K("floor", 1, 5), K("floor", 2, 5), K("floor", 3, 5), K("floor", 4, 5)]
SAND_ORIGIN = ("floor", 0, 0)      # 도시 바닥 위 큰길
WATER_ORIGIN = ("water", 0, 6)     # 풀밭 위 물
PLANK = K("water", 1, 13)          # 이전 맵 호환용 판자
DECO = [K("detail", 0, 2), K("detail", 1, 2), K("detail", 2, 2), K("detail", 3, 2), K("detail", 5, 2), K("detail", 6, 2)]
FLOWERS = [K("detail", 5, 2), K("detail", 2, 0)]


# ---------- 여러 칸짜리 묶음 (스탬프) ----------
# (sheet, x0, y0, w, h, 막는 줄 시작, 위층 줄 수, 색 바꾸기)
STAMPS = {
    "module_rock": ("module",0,0,2,2,1,1,None),
    "module_cactus": ("module",2,0,2,3,2,2,None),
    "module_ruin": ("module",4,0,3,3,1,1,None),
    "house_tile": ("house", 12, 0, 4, 3, 0, 0, "slate"),   # 기와집
    "house_thatch": ("house", 0, 0, 4, 3, 0, 0, None),     # 초가
    "house_plain": ("house", 4, 0, 4, 3, 0, 0, None),      # 흙벽집
    "house_wall": ("house", 8, 0, 4, 3, 0, 0, None),
    "shop": ("house", 16, 0, 3, 3, 0, 0, "slate"),         # 간판 가게
    "store": ("house", 19, 0, 4, 3, 0, 0, None),           # 큰 상점
    "house3": ("house", 26, 0, 3, 3, 0, 0, None),
    "hall": ("house", 25, 7, 4, 7, 0, 0, "slate"),         # 궁궐 전각
    "pine": ("nature", 0, 0, 2, 2, 1, 1, None),            # 소나무: 아랫줄만 막음, 윗줄은 플레이어 위로
    "tree": ("nature", 2, 0, 2, 2, 1, 1, None),
    "cherry": ("nature", 14, 0, 2, 2, 1, 1, None),
    "cart": ("element", 0, 3, 2, 2, 0, 0, None),
    "bench": ("element", 11, 1, 3, 1, 0, 0, None),
    "bigtree": ("nature", 3, 18, 3, 3, 2, 2, None),        # 마을 정자나무: 밑동만 막음
    "roundtree": ("nature", 6, 8, 2, 2, 1, 1, None),
    "plot": ("field", 0, 0, 3, 3, 0, 0, None),             # 밭 (높은 흙더미)
    "boat": ("boat", 0, 0, 5, 2, 0, 0, None),
    # 큰 나무 덩어리 (숲 가장자리를 덜 반복적으로)
    "pinebig": ("nature", 0, 2, 4, 3, 2, 2, None),
    "roundbig": ("nature", 4, 2, 4, 3, 2, 2, None),
    "fruitbig": ("nature", 16, 2, 4, 3, 2, 2, None),
    "blossom": ("nature", 0, 18, 3, 3, 2, 2, None),       # 꽃나무
    "greenbig": ("nature", 4, 18, 3, 3, 2, 2, None),
    "wheat": ("element", 13, 4, 3, 3, 0, 0, None),         # 누렇게 익은 논
    "sheaf": ("element", 12, 4, 1, 2, 1, 1, None),         # 볏단
    "paper_rack": ("work", 0, 0, 3, 2, 1, 1, None),      # 한지 건조대
    "rack": ("element", 11, 2, 3, 2, 1, 1, None),          # 빨랫줄 기둥
    "well": ("element", 7, 1, 1, 2, 1, 1, None),           # 우물
    "hay": ("nature", 9, 8, 1, 1, 0, 0, None),             # 짚더미
    "laundry": ("element", 14, 2, 2, 2, 1, 1, None),       # 빨래
}
JAR = K("element", 4, 0)            # 장독
LAUNDRY_TOP = [K("element", 14, 2), K("element", 15, 2)]
LAUNDRY_SHADOW = [K("element", 14, 3), K("element", 15, 3)]
REED = [K("detail", 3, 2), K("detail", 0, 2), K("detail", 2, 2)]
PROPS = [K("element", 3, 0), K("element", 0, 1), K("element", 1, 1), K("element", 4, 0)]
SIGN = K("element", 0, 2)          # 이정표 나무판
BOARD = K("element", 1, 2)         # 의뢰 게시판 (종이가 붙은 판)
BUSHES = [K("nature", 0, 10), K("nature", 1, 10), K("nature", 2, 10), K("nature", 6, 10)]
FLOWER_DECO = [K("nature", 0, 11), K("nature", 1, 11), K("nature", 3, 11), K("nature", 6, 11), K("detail", 5, 2)]
SPROUT = K("detail", 6, 2)
FIELD = ("field", 0, 3)            # 높은 풀밭(언덕) 3x3 조각의 왼쪽 위


class Map:
    def __init__(self, mid, name, w, h, seed=1, theme="grass"):
        self.id, self.name, self.w, self.h = mid, name, w, h
        self.theme = theme
        self.rng = random.Random(seed)
        self.ground = [[None] * w for _ in range(h)]
        self.road = [[0] * w for _ in range(h)]       # 길 등급
        self.water = [[False] * w for _ in range(h)]
        self.deco = [[None] * w for _ in range(h)]
        self.obj = [[None] * w for _ in range(h)]
        self.high = [[None] * w for _ in range(h)]
        self.blocked = [[False] * w for _ in range(h)]
        self.reserved = [[False] * w for _ in range(h)]  # 자동 배치(나무 등)를 막는 칸
        self.objects = []   # 스폰, 출구, NPC
        self.decks = {}     # 판자 바닥을 길/물 오토타일에서 보호
        self.stamps = []    # 배치한 묶음 기록 (조각 누락 검증용)
        self.hours = 0      # 이 맵을 지날 때 흐르는 게임 시간 (길 맵)

    def inside(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    # --- 그리기 ---
    def road_rect(self, x0, y0, x1, y1, grade=2):
        for y in range(min(y0, y1), max(y0, y1) + 1):
            for x in range(min(x0, x1), max(x0, x1) + 1):
                if self.inside(x, y):
                    self.road[y][x] = max(self.road[y][x], grade)
                    self.reserved[y][x] = True

    def road_path(self, pts, width=2, grade=1, wobble=0):
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            steps = max(abs(bx - ax), abs(by - ay))
            for i in range(steps + 1):
                t = i / max(1, steps)
                cx, cy = round(ax + (bx - ax) * t), round(ay + (by - ay) * t)
                # 폭 1 대각선도 상하좌우로 연결한다. 모서리만 닿는 길을 만들지 않는다.
                if i and cx != prev_x and cy != prev_y:
                    self.road_rect(prev_x, cy, prev_x + width - 1, cy + width - 1, grade)
                off = self.rng.choice([0, 0, 0, 1]) if wobble else 0
                for dx in range(width):
                    for dy in range(width):
                        x, y = cx + dx, cy + dy + (off if ax != bx else 0)
                        if self.inside(x, y):
                            self.road[y][x] = max(self.road[y][x], grade)
                            self.reserved[y][x] = True
                prev_x, prev_y = cx, cy

    def road_blob(self, cx, cy, rx, ry, grade=1, jitter=0.15):
        """둥근 마당·광장 (타원, 가장자리를 조금 들쭉날쭉)"""
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if not self.inside(x, y):
                    continue
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if d <= 1 + self.rng.uniform(-jitter, jitter):
                    self.road[y][x] = max(self.road[y][x], grade)
                    self.reserved[y][x] = True

    def water_blob(self, cx, cy, rx, ry, jitter=0.2):
        """연못 (둥글고 들쭉날쭉)"""
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if self.inside(x, y) and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1 + self.rng.uniform(-jitter, jitter):
                    self.water[y][x] = True
                    self.reserved[y][x] = True

    def _walk(self, n, lo, hi, start, fixed=()):
        """천천히 흔들리는 값 (강가 굴곡). fixed 위치에서는 start로 고정"""
        v, out = start, []
        for i in range(n):
            if i in fixed:
                v = start
            elif self.rng.random() < 0.35:
                v = min(hi, max(lo, v + self.rng.choice([-1, 1])))
            out.append(v)
        return out

    def river_h(self, top, bottom, x0=0, x1=None, wiggle=1, fixed=()):
        """가로로 흐르는 강. 윗물가가 구불구불"""
        x1 = self.w - 1 if x1 is None else x1
        tops = self._walk(x1 - x0 + 1, top - wiggle, top + wiggle, top, {f - x0 for f in fixed})
        for i, x in enumerate(range(x0, x1 + 1)):
            self.water_rect(x, tops[i], x, bottom)

    def river_v(self, left, right, wiggle=1, fixed_rows=()):
        """세로로 흐르는 시내. 양쪽 물가가 함께 구불구불 (다리 줄은 고정)"""
        offs = self._walk(self.h, -wiggle, wiggle, 0, set(fixed_rows))
        for y in range(self.h):
            self.water_rect(left + offs[y], y, right + offs[y], y)

    def roughen(self, x0, y0, x1, y1, grade=2, every=5):
        """곧은 큰길 가장자리에 군데군데 2칸짜리 턱을 내 덜 반듯하게"""
        for x in range(x0, x1 + 1, every):
            if self.rng.random() < 0.6:
                side = self.rng.choice([y0 - 1, y1 + 1])
                for dx in range(2):
                    xx = x + dx + self.rng.randint(0, 2)
                    if self.inside(xx, side) and not self.blocked[side][xx] and not self.water[side][xx]:
                        self.road[side][xx] = max(self.road[side][xx], grade)
                        self.reserved[side][xx] = True

    def water_rect(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.inside(x, y):
                    self.water[y][x] = True
                    self.reserved[y][x] = True

    def bridge(self, x0, y0, x1, y1, dock=False):
        """방향에 맞는 외곽 판자·난간·기둥을 갖춘 목조 다리/선착장."""
        horizontal = not dock and (x1 - x0 >= y1 - y0)
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.water[y][x] = False
                # 원본 에셋의 3x3 목재 플랫폼. 세로 다리에는 판재 방향도 돌린다.
                sx = 0 if x == x0 else 2 if x == x1 else 1
                sy = 12 if y == y0 else 14 if y == y1 else 13
                if horizontal:
                    sx = 0 if y == y1 else 2 if y == y0 else 1
                    sy = 12 if x == x0 else 14 if x == x1 else 13
                key = K("water", sx, sy, "woodturn" if horizontal else "wood")
                self.ground[y][x] = key
                self.decks[(x, y)] = key
                self.road[y][x] = max(self.road[y][x], 2)
                self.reserved[y][x] = True
        # 난간은 이동 가능한 칸 가장자리에만 그린다. 충돌을 추가하지 않는다.
        edges = [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)
                 if (y in (y0, y1) if horizontal else x in (x0, x1))]
        for x, y in edges:
            end = x in (x0, x1) if horizontal else y in (y0, y1)
            direction = ("railhtop" if y == y0 else "railhbottom") if horizontal else ("railvleft" if x == x0 else "railvright")
            self.deco[y][x] = f"wood:{direction}:{1 if end else 0}"

    def can_place(self, x, y, w, h, margin=0):
        for yy in range(y - margin, y + h + margin):
            for xx in range(x - margin, x + w + margin):
                if not self.inside(xx, yy) or self.reserved[yy][xx]:
                    return False
        return True

    def stamp(self, name, x, y, force=False, over_road=False):
        """force: 예약 칸이어도 놓음. 그래도 막히는 칸이 길 위면 놓지 않는다 (over_road=True만 예외)."""
        s, sx, sy, w, h, block_from, high_rows, rc = STAMPS[name]
        if not self.inside(x, y) or not self.inside(x + w - 1, y + h - 1):
            return False
        # force도 이미 놓은 집/나무를 타일 단위로 덮어쓰지는 않는다.
        if any(self.obj[y + dy][x + dx] or self.high[y + dy][x + dx]
               for dy in range(h) for dx in range(w)):
            return False
        if not force and not self.can_place(x, y, w, h):
            return False
        if not over_road:
            for dy in range(block_from, h):
                for dx in range(w):
                    xx, yy = x + dx, y + dy
                    if self.inside(xx, yy) and (self.road[yy][xx] > 0 or self.water[yy][xx]):
                        return False
        for dy in range(h):
            for dx in range(w):
                key = K(s, sx + dx, sy + dy, rc)
                if dy < high_rows:
                    self.high[y + dy][x + dx] = key
                else:
                    self.obj[y + dy][x + dx] = key
                if dy >= block_from:
                    self.blocked[y + dy][x + dx] = True
                    self.road[y + dy][x + dx] = 0   # 집·나무 밑으로 길이 지나가지 않게
                self.reserved[y + dy][x + dx] = True
        self.stamps.append((name, x, y))
        return True

    def door_path(self, hx, hy, w, h, to_y, door_dx=1):
        """집 문 앞에서 아래로 좁은 길을 낸다 (문은 아래쪽 줄 door_dx 칸째)."""
        x = hx + door_dx
        for y in range(hy + h, to_y + 1):
            if self.inside(x, y) and not self.blocked[y][x]:
                self.road[y][x] = max(self.road[y][x], 1)
                self.reserved[y][x] = True

    def bushes(self, pts):
        for (x, y) in pts:
            if self.inside(x, y) and not self.reserved[y][x]:
                self.prop(self.rng.choice(BUSHES), x, y)

    def flowers(self, x0, y0, x1, y1, n):
        for _ in range(n):
            x, y = self.rng.randint(x0, x1), self.rng.randint(y0, y1)
            if self.inside(x, y) and not self.reserved[y][x] and self.obj[y][x] is None:
                self.deco[y][x] = self.rng.choice(FLOWER_DECO)
                self.reserved[y][x] = True

    def prop(self, key, x, y, block=True):
        self.obj[y][x] = key
        self.blocked[y][x] = block
        self.reserved[y][x] = True

    def reserve(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.inside(x, y):
                    self.reserved[y][x] = True

    def forest_border(self, thickness=2, density=1.0, kinds=("pine", "tree"), big=("pinebig", "roundbig")):
        """맵 가장자리를 나무로 두른다 (빈틈 없이 막히게). 먼저 큰 나무 덩어리, 빈틈은 작은 나무로."""
        if big:
            for y in range(0, self.h - 2, 3):
                for x in range(0, self.w - 3, 4):
                    edge = x < thickness or y < thickness or x + 4 > self.w - thickness or y + 3 > self.h - thickness
                    if edge and self.rng.random() < 0.55:
                        self.stamp_clipped(self.rng.choice(big), x + self.rng.randint(0, 1), y)
        for y in range(0, self.h - 1, 2):
            for x in range(0, self.w - 1, 2):
                edge = x < thickness or y < thickness or x >= self.w - thickness - 1 or y >= self.h - thickness - 1
                if edge and self.rng.random() < density:
                    self.stamp_clipped(self.rng.choice(kinds), x, y)

    def stamp_clipped(self, name, x, y):
        # 나무는 한 그루 단위로 놓거나 건너뛴다. 길 옆에 수관/밑동 조각을 남기지 않는다.
        if not self.stamp(name, x, y):
            return False
        _, _, _, w, h, *_ = STAMPS[name]
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.blocked[yy][xx] = True
        return True

    def scatter(self, name, count, area=None, margin=1):
        x0, y0, x1, y1 = area or (0, 0, self.w - 1, self.h - 1)
        placed = tries = 0
        while placed < count and tries < count * 40:
            tries += 1
            x, y = self.rng.randint(x0, x1), self.rng.randint(y0, y1)
            s, sx, sy, w, h, *_ = STAMPS[name]
            if self.can_place(x, y, w, h, margin):
                self.stamp(name, x, y)
                placed += 1

    def link(self, edge, start, length, target, target_spawn, my_spawn):
        """맵 가장자리 연결. edge: 'N','S','E','W'. start부터 length칸이 출구,
        한 칸 안쪽 같은 폭이 이 맵에 들어올 때 서는 자리(my_spawn)다.
        이웃 맵의 같은 폭 출구와 맞추면, 나간 자리 그대로 이어서 들어온다."""
        if edge in "EW":
            x = self.w - 1 if edge == "E" else 0
            sx = x - 1 if edge == "E" else 1
            self.add("exit", x, start, w=1, h=length, target=target, spawn=target_spawn, edge=edge)
            self.add("spawn", sx, start, w=1, h=length, id=my_spawn, edge=edge)
            self.reserve(min(x, sx), start, max(x, sx), start + length - 1)
        else:
            y = self.h - 1 if edge == "S" else 0
            sy = y - 1 if edge == "S" else 1
            self.add("exit", start, y, w=length, h=1, target=target, spawn=target_spawn, edge=edge)
            self.add("spawn", start, sy, w=length, h=1, id=my_spawn, edge=edge)
            self.reserve(start, min(y, sy), start + length - 1, max(y, sy))

    def plateau(self, x0, y0, x1, y1, opening=None):
        """높은 터(언덕): 가장자리는 막히고, opening=(xa, xb)인 아래쪽 칸으로 오르내린다."""
        fs, fx, fy = FIELD
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                cx = 0 if x == x0 else 2 if x == x1 else 1
                cy = 0 if y == y0 else 2 if y == y1 else 1
                self.ground[y][x] = K(fs, fx + cx, fy + cy)
                edge = x in (x0, x1) or y in (y0, y1)
                if edge and not (opening and y == y1 and opening[0] <= x <= opening[1]):
                    self.blocked[y][x] = True
                self.reserved[y][x] = True
        if opening:
            for x in range(opening[0], opening[1] + 1):
                self.ground[y1][x] = None   # 계단 자리는 길로
                self.road[y1][x] = 3

    def jars(self, x, y, pattern=("110", "011")):
        """장독대: pattern의 1 자리에 장독"""
        for dy, row in enumerate(pattern):
            for dx, ch in enumerate(row):
                if ch == "1" and self.inside(x + dx, y + dy):
                    self.prop(JAR, x + dx, y + dy)

    def laundry(self, x, y):
        """빨래 널린 줄 (2칸)"""
        self.stamp("laundry", x, y, force=True)

    def reeds(self, y, x0, x1, chance=0.5):
        """물가 갈대: y줄에서 물 바로 위 칸"""
        for x in range(x0, x1 + 1):
            if self.inside(x, y) and not self.reserved[y][x] and self.rng.random() < chance:
                self.deco[y][x] = self.rng.choice(REED)

    def anim(self, sheet, x, y, frames=4, fps=4.0):
        """움직이는 장식 (Unity에서 스프라이트 애니메이션으로)"""
        self.objects.append(dict(kind="anim", x=x, y=y, sheet=sheet, frames=frames, fps=fps))
        self.reserved[y][x] = True

    def board(self, x, y, town):
        """의뢰 게시판 (막힌 칸, 앞에서 Space)"""
        self.prop(BOARD, x, y)
        self.add("board", x, y, town=town)

    def bandit(self, x, y, radius=6):
        """도적 출몰 자리"""
        self.add("bandit", x, y, radius=radius)

    def event(self, x, y):
        """돌발 사건이 일어날 수 있는 길가 자리"""
        self.add("event", x, y)

    def growth(self, x, y, key, town, level=2):
        """마을 등급이 level 이상이 되면 나타나는 소품 (장터가 커지는 모습)"""
        self.objects.append(dict(kind="growth", x=x, y=y, id=key, town=town, variant=level))

    def walker(self, path, idx=0):
        """길을 오가는 NPC 상인. path는 길 위 칸 목록"""
        self.objects.append(dict(kind="walker", x=path[0][0], y=path[0][1], path=path, idx=idx))

    def sign(self, x, y, text):
        """이정표: 말을 걸면 거리(리)를 알려 준다. 길 위면 길가로 옮긴다."""
        while self.inside(x, y) and (self.road[y][x] > 0 or self.water[y][x]):
            x += 1
        self.prop(SIGN, x, y)
        self.add("sign", x, y, text=text)

    def talker(self, x, y, name, character, *lines):
        self.add("talker", x, y, name=name, character=character, lines=list(lines))

    def add(self, kind, x, y, **kw):
        self.objects.append(dict(kind=kind, x=x, y=y, **kw))
        self.reserve(x, y, x, y)

    # --- 마무리 ---
    def smooth(self, passes=2):
        """길·물에 파인 한 칸짜리 홈을 메운다 (이웃 8칸 중 5칸 이상이 같으면)"""
        for _ in range(passes):
            for layer, val in ((self.road, None), (self.water, True)):
                add = []
                for y in range(1, self.h - 1):
                    for x in range(1, self.w - 1):
                        if self.blocked[y][x] or self.obj[y][x] is not None:
                            continue
                        if val is None and (self.road[y][x] > 0 or self.water[y][x] or self.ground[y][x] is not None):
                            continue
                        if val and (self.water[y][x] or self.road[y][x] > 0 or self.ground[y][x] is not None):
                            continue
                        nb = [layer[y + dy][x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dx or dy]
                        on = [v for v in nb if v]
                        n4 = [layer[y - 1][x], layer[y + 1][x], layer[y][x - 1], layer[y][x + 1]]
                        if len(on) >= 5 or (n4[0] and n4[1]) or (n4[2] and n4[3]):
                            add.append((x, y, max(on) if val is None else True))
                for x, y, v in add:
                    layer[y][x] = v
                    self.reserved[y][x] = True

    def finish(self):
        if not getattr(self,"skip_smooth",False): self.smooth()

        def road_mask(a, b):
            if not self.inside(a, b):
                if hasattr(self,"outside_road"):return self.outside_road(a,b)
                # 맵 밖은 가장자리 칸을 그대로 이어 본다 (길이 맵 경계에서 끊겨 보이지 않게)
                a = min(max(a, 0), self.w - 1); b = min(max(b, 0), self.h - 1)
            return self.road[b][a] > 0 or (a, b) in self.decks

        def water_mask(a, b):
            if not self.inside(a, b):
                return self.outside_water(a,b) if hasattr(self,"outside_water") else True  # 공통 월드 물가 샘플
            return self.water[b][a] or (a, b) in self.decks  # 다리 밑으로 물이 이어지게

        for y in range(self.h):
            for x in range(self.w):
                if (x, y) in self.decks:
                    continue
                if self.ground[y][x] is not None and self.road[y][x] == 0 and not self.water[y][x]:
                    continue  # 언덕처럼 미리 정한 바닥
                if self.water[y][x]:
                    self.ground[y][x] = autotile(water_mask, x, y, *WATER_ORIGIN)
                    self.blocked[y][x] = True
                elif self.road[y][x] > 0:
                    origin = self.road_origin(x,y) if hasattr(self,"road_origin") else SAND_ORIGIN if self.theme == "town" else DIRT_ORIGIN
                    self.ground[y][x] = autotile(road_mask, x, y, *origin)
                elif self.theme == "town":
                    self.ground[y][x] = self.rng.choice(TOWN_VARIANTS) if self.rng.random() < 0.15 else TOWN
                else:
                    self.ground[y][x] = self.rng.choice(GRASS_VARIANTS) if self.rng.random() < 0.12 else GRASS
                    if self.obj[y][x] is None and not self.reserved[y][x] and self.rng.random() < 0.06:
                        self.deco[y][x] = self.rng.choice(DECO)
        # 물결: 사방이 물인 칸 일부에 반짝이는 물결 애니메이션
        for y in range(1, self.h - 1):
            for x in range(1, self.w - 1):
                if all(self.water[y + dy][x + dx] for dx in (-1, 0, 1) for dy in (-1, 0, 1)) and self.rng.random() < 0.12:
                    self.objects.append(dict(kind="anim", x=x, y=y, sheet="ripples", frames=4, fps=3.0 + self.rng.random()))

    def apply_visual_skin(self, skin_id):
        """Reuse the module library's visual palettes without modifying traversal data."""
        from Modules.skins import SKINS
        if skin_id not in {s[0] for s in SKINS}:raise ValueError(skin_id)
        self.visual_skin = skin_id
        for layer in (self.ground,self.deco,self.obj,self.high):
            for row in layer:
                for x,key in enumerate(row):
                    if key and '@' not in key and key.split(':')[0] in ('floor','detail','nature','field','water'):
                        row[x]=key+'@skin_'+skin_id

    def render(self, path, scale=1):
        img = Image.new("RGBA", (self.w * 16, self.h * 16), (0, 0, 0, 255))
        for layer in (self.ground, self.deco, self.obj):
            for y in range(self.h):
                for x in range(self.w):
                    if layer[y][x]:
                        img.alpha_composite(tile_image(layer[y][x]), (x * 16, y * 16))
        colors = {"spawn": (60, 140, 255, 200), "exit": (255, 140, 0, 160), "npc": (255, 230, 60, 230), "market": (255, 80, 80, 230)}
        for o in self.objects:
            c = colors.get(o["kind"], (255, 255, 255, 200))
            dot = Image.new("RGBA", (10, 10), c)
            img.alpha_composite(dot, (o["x"] * 16 + 3, o["y"] * 16 + 3))
        for y in range(self.h):
            for x in range(self.w):
                if self.high[y][x]:
                    img.alpha_composite(tile_image(self.high[y][x]), (x * 16, y * 16))
        if scale != 1:
            img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
        img.save(path)

    def to_json(self):
        return dict(id=self.id, name=self.name, w=self.w, h=self.h,
                    ground=self.ground, deco=self.deco, obj=self.obj, high=self.high,
                    road=self.road, blocked=self.blocked, objects=self.objects)
