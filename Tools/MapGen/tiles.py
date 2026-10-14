"""Ninja Adventure 타일 카탈로그, 자동 타일(오토타일), 미리보기."""
import os, random, colorsys
from PIL import Image

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
    base, _, rc = key.partition("@")
    s, x, y = base.split(":")
    x, y = int(x), int(y)
    t = sheet(s).crop((x * 16, y * 16, x * 16 + 16, y * 16 + 16))
    if rc:
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
PLANK = K("water", 1, 13)          # 다리·나루 판자
DECO = [K("detail", 0, 2), K("detail", 1, 2), K("detail", 2, 2), K("detail", 3, 2), K("detail", 5, 2), K("detail", 6, 2)]
FLOWERS = [K("detail", 5, 2), K("detail", 2, 0)]


# ---------- 여러 칸짜리 묶음 (스탬프) ----------
# (sheet, x0, y0, w, h, 막는 줄 시작, 위층 줄 수, 색 바꾸기)
STAMPS = {
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
}
PROPS = [K("element", 3, 0), K("element", 0, 1), K("element", 1, 1), K("element", 4, 0)]
SIGN = K("element", 0, 2)          # 이정표 나무판
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
                off = self.rng.choice([0, 0, 0, 1]) if wobble else 0
                for dx in range(width):
                    for dy in range(width):
                        x, y = cx + dx, cy + dy + (off if ax != bx else 0)
                        if self.inside(x, y):
                            self.road[y][x] = max(self.road[y][x], grade)
                            self.reserved[y][x] = True

    def water_rect(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.inside(x, y):
                    self.water[y][x] = True
                    self.reserved[y][x] = True

    def bridge(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.water[y][x] = False
                self.ground[y][x] = PLANK
                self.road[y][x] = max(self.road[y][x], 2)
                self.reserved[y][x] = True

    def can_place(self, x, y, w, h, margin=0):
        for yy in range(y - margin, y + h + margin):
            for xx in range(x - margin, x + w + margin):
                if not self.inside(xx, yy) or self.reserved[yy][xx]:
                    return False
        return True

    def stamp(self, name, x, y, force=False):
        s, sx, sy, w, h, block_from, high_rows, rc = STAMPS[name]
        if not force and not self.can_place(x, y, w, h):
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

    def forest_border(self, thickness=2, density=1.0, kinds=("pine", "tree")):
        """맵 가장자리를 나무로 두른다 (빈틈 없이 막히게)."""
        for y in range(-1, self.h, 2):
            for x in range(-1, self.w, 2):
                edge = x < thickness or y < thickness or x >= self.w - thickness - 1 or y >= self.h - thickness - 1
                if edge and self.rng.random() < density:
                    self.stamp_clipped(self.rng.choice(kinds), x, y)

    def stamp_clipped(self, name, x, y):
        s, sx, sy, w, h, block_from, high_rows, rc = STAMPS[name]
        for dy in range(h):
            for dx in range(w):
                xx, yy = x + dx, y + dy
                if not self.inside(xx, yy) or self.reserved[yy][xx]:
                    continue
                key = K(s, sx + dx, sy + dy, rc)
                if dy < high_rows:
                    self.high[yy][xx] = key
                    self.blocked[yy][xx] = True  # 가장자리는 위쪽도 막는다
                else:
                    self.obj[yy][xx] = key
                    self.blocked[yy][xx] = True

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

    def sign(self, x, y, text):
        """이정표: 말을 걸면 거리(리)를 알려 준다."""
        self.prop(SIGN, x, y)
        self.add("sign", x, y, text=text)

    def talker(self, x, y, name, character, *lines):
        self.add("talker", x, y, name=name, character=character, lines=list(lines))

    def add(self, kind, x, y, **kw):
        self.objects.append(dict(kind=kind, x=x, y=y, **kw))
        self.reserve(x, y, x, y)

    # --- 마무리 ---
    def finish(self):
        def road_mask(a, b):
            if not self.inside(a, b):
                # 맵 밖은 가장자리 칸을 그대로 이어 본다 (길이 맵 경계에서 끊겨 보이지 않게)
                a = min(max(a, 0), self.w - 1); b = min(max(b, 0), self.h - 1)
            return self.road[b][a] > 0 or self.ground[b][a] == PLANK

        def water_mask(a, b):
            if not self.inside(a, b):
                return True  # 맵 밖으로 이어지는 물은 가장자리 없이
            return self.water[b][a] or self.ground[b][a] == PLANK  # 다리 밑으로 물이 이어지게

        for y in range(self.h):
            for x in range(self.w):
                if self.ground[y][x] == PLANK:
                    continue
                if self.ground[y][x] is not None and self.road[y][x] == 0 and not self.water[y][x]:
                    continue  # 언덕처럼 미리 정한 바닥
                if self.water[y][x]:
                    self.ground[y][x] = autotile(water_mask, x, y, *WATER_ORIGIN)
                    self.blocked[y][x] = True
                elif self.road[y][x] > 0:
                    origin = SAND_ORIGIN if self.theme == "town" else DIRT_ORIGIN
                    self.ground[y][x] = autotile(road_mask, x, y, *origin)
                elif self.theme == "town":
                    self.ground[y][x] = self.rng.choice(TOWN_VARIANTS) if self.rng.random() < 0.15 else TOWN
                else:
                    self.ground[y][x] = self.rng.choice(GRASS_VARIANTS) if self.rng.random() < 0.12 else GRASS
                    if self.obj[y][x] is None and not self.reserved[y][x] and self.rng.random() < 0.06:
                        self.deco[y][x] = self.rng.choice(DECO)

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
