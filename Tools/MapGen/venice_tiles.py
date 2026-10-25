"""베네치아 전용 바닥·물가·묶음. 공용 tiles.py 파일은 바꾸지 않는다. 임시 아트이며 팀 아트 승인 전이다.
- 'vgpt:x:y'  GPT 생성 에셋 아틀라스(Tools/ArtGen/venice_atlas.png) 칸. 묶음 이름은 'vg_<이름>'
- 'vq:L:...'  땅 타일. 2×2타일 바닥 무늬(m_*) 한 조각 + 물가·구역 테두리 덧그림
- 'vq:W:...'  물 타일. 한 가지 바탕색 + 낮은 대비의 물결 + 물가 접촉선
물가 색은 GPT 지형 시트(석축·젖은 모래)에서 땄다. 그림은 export_venice.py가 공용 Tiles 폴더에 PNG로 내보낸다."""
import json, os, random
import tiles
from tiles import Map, K

# GPT 생성 에셋 아틀라스. 이 프로세스 안에서만 시트·묶음을 추가한다.
_ATLAS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ArtGen", "venice_atlas")
tiles.SHEETS.setdefault("vgpt", os.path.abspath(_ATLAS + ".png"))
_BLOCK = {  # 이름: (막는 줄 시작, 위층 줄 수). 나머지는 전체를 막는다.
    "campanile": (4, 4), "poles": (1, 1), "pole_striped": (1, 1), "banner": (1, 1),
    "cypress": (2, 2), "olive": (2, 2), "tree": (2, 2), "lemon_pot": (1, 1), "vine": (1, 1),
    "salt_rake": (1, 1), "lemon_tree": (1, 1), "px_cap_flag_red": (2, 2), "px_cap_flag_blue": (2, 2),
    "px_prt_trade_flag": (2, 2), "cp_lion_column": (2, 2), "cp_saint_column": (2, 2), "cp_brazier": (1, 1),
    "cp_mooring": (1, 1), "w_shelter": (2, 2), "w_vine_bench": (2, 2), "w_bunting": (1, 1), "w_flower_cart": (1, 1),
    "w_office": (1, 1), "w_lemon_pots": (1, 1), "n_orc_fig": (2, 2), "n_orc_olive": (2, 2), "n_orc_lemon": (2, 2), "n_prt_crane": (1, 1), "cp_pennant": (1, 1), "cp_tapestry": (1, 1), "px_orc_lemon_tree2": (2, 2), "px_orc_ladder_basket": (1, 1), "flower_arch": (1, 1), "reeds_tall": (1, 1),
    "laundry": (1, 1), "net_rack": (1, 1), "ladder_rope": (1, 1),
}
with open(_ATLAS + ".json", encoding="utf-8") as _fp:
    _CELLS = json.load(_fp)
for _name, (_x, _y, _w, _h) in _CELLS.items():
    _bf, _hr = _BLOCK.get(_name, (0, 0))
    tiles.STAMPS["vg_" + _name] = ("vgpt", _x, _y, _w, _h, _bf, _hr, None)
GPT_OWN = ("ven:", "vgpt:", "vq:")   # 베네치아가 소유하는 그림 키 (내보낼 때 바뀌면 다시 씀)

# 색 (GPT 지형 시트에서 표본)
WATER = (81, 140, 147)
RIPPLE = (99, 155, 160)
CONTACT = (43, 92, 107)          # 석축 아래 어두운 접촉선
CONTACT2 = (62, 116, 128)
SHALLOW = (73, 130, 137)         # 모래 물가 쪽 물
CURB = (207, 193, 166)           # 석축 윗면
CURB_HI = (230, 215, 187)
CURB_FACE = (121, 121, 114)      # 남쪽으로 보이는 석축 옆면
JOINT = (130, 124, 109)
WET = (179, 144, 100)            # 젖은 모래
MUD = (113, 94, 76)
BANK = (150, 114, 78)            # 염전 흙둑
BANK_DARK = (106, 82, 58)
PAN = (139, 179, 173)            # 염전 소금물
BRICK = (183, 111, 78)
BRICK_DARK = (135, 89, 71)
STONE_LINE = (226, 216, 194)
SAND = (213, 178, 117)           # GPT 모래 표본
SAND_DOTS = [(190, 156, 100), (182, 159, 108), (226, 195, 140)]
PATH = (222, 196, 143)           # 다져진 모래길
MEADOW = (152, 164, 106)         # 옅은 세이지 풀밭
MEADOW_DOTS = [(136, 150, 94), (168, 178, 122)]
MEADOW_EDGE = (124, 138, 86)
DIRT = (186, 152, 110)           # 흙길·작업 마당
DIRT_DOTS = [(162, 130, 94), (200, 168, 126)]
PATH_DOT = (190, 160, 110)
GRASS = [(125, 143, 79), (95, 115, 64), (148, 160, 90)]
ROCK = (178, 170, 150)           # 본토 석회암 능선 (밟을 수 없음)
ROCK_DOTS = [(150, 142, 124), (198, 191, 172), (162, 154, 136)]
ROCK_RIM = (206, 199, 180)
ROCK_FACE = (118, 110, 98)       # 남쪽으로 보이는 바위 옆면
ROCK_FACE_DARK = (92, 86, 78)
MUDLAND = (126, 104, 80)         # 습지 진흙길
MUD_DOTS = [(108, 89, 68), (146, 122, 92)]

_img_cache = {}


def _atlas_img():
    if "atlas" not in _img_cache:
        from PIL import Image
        _img_cache["atlas"] = Image.open(tiles.SHEETS["vgpt"]).convert("RGBA")
    return _img_cache["atlas"]


def _macro(name, rot):
    """2×2타일 바닥 무늬. 석판은 대비를 낮춰 시선을 덜 끈다."""
    from PIL import Image
    key = (name, rot)
    if key not in _img_cache:
        x, y, w, h = _CELLS[name]
        img = _atlas_img().crop((x * 16, y * 16, (x + w) * 16, (y + h) * 16))
        if name.startswith("m_slab"):
            px = img.load()
            avg = [sum(px[i, j][c] for i in range(32) for j in range(32)) / 1024 for c in range(3)]
            for j in range(32):
                for i in range(32):
                    r, g, b, a = px[i, j]
                    px[i, j] = tuple(round(avg[c] + (v - avg[c]) * .55) for c, v in enumerate((r, g, b))) + (a,)
        if rot:
            img = img.transpose(Image.Transpose.ROTATE_90)
        _img_cache[key] = img
    return _img_cache[key]


def _band(px, side, width, colors):
    """side 쪽 가장자리부터 안쪽으로 colors[0], colors[1]... 줄을 칠한다."""
    for d in range(width):
        c = colors[min(d, len(colors) - 1)]
        for t in range(16):
            x, y = {"N": (t, d), "S": (t, 15 - d), "W": (d, t), "E": (15 - d, t)}[side]
            if c is not None:
                px[x, y] = c + (255,)


def _corner(px, corner, size, color):
    x0 = 0 if "W" in corner else 16 - size
    y0 = 0 if "N" in corner else 16 - size
    for y in range(y0, y0 + size):
        for x in range(x0, x0 + size):
            px[x, y] = color + (255,)


LAND_EDGE = {   # 물가·테두리 종류: 바깥(물 쪽)부터 안쪽으로 칠할 줄
    "q": [CURB, CURB, CURB_HI, JOINT],                  # 운하 석축 윗면
    "Q": [CURB_FACE, CURB_FACE, CURB, CURB_HI, JOINT],  # 남쪽 물: 석축 옆면이 보인다
    "b": [MUD, WET, WET],                               # 모래 → 젖은 흙 → 물
    "d": [BANK_DARK, BANK, BANK],                       # 염전 흙둑
    "k": [BRICK_DARK, BRICK, BRICK_DARK],               # 시장 벽돌 테두리
    "l": [(176, 166, 146)],                             # 안뜰 경계: 옅은 한 줄
    "p": [PATH_DOT],                                    # 모래길 가장자리
    "e": [DIRT_DOTS[0]],                                # 흙길 가장자리
    "g": [MEADOW_EDGE],                                 # 풀밭 가장자리 (흙길 쪽)
    "r": [ROCK_RIM, ROCK_DOTS[0]],                      # 바위 능선 윗 테두리
    "R": [ROCK_FACE_DARK, ROCK_FACE, ROCK_FACE, ROCK_DOTS[0]],   # 남쪽: 바위 옆면이 보인다
    "w": [JOINT, CURB_HI],                              # 석축·성벽 윗면 테두리
    "W": [CONTACT, CURB_FACE, CURB_FACE, JOINT, CURB],  # 남쪽: 쌓은 돌 옆면
}
WATER_EDGE = {"q": [CONTACT, CONTACT2], "b": [SHALLOW, SHALLOW], "d": [BANK_DARK]}


def _land(parts):
    _, _, mat, var, qx, qy, rot, edges = parts
    var, qx, qy, rot = int(var), int(qx), int(qy), int(rot)
    from PIL import Image
    if mat == "wall":         # 쌓은 석재 윗면: 엇갈린 돌 줄눈
        img = Image.new("RGBA", (16, 16), CURB + (255,))
        px = img.load()
        for y in range(16):
            for x in range(16):
                if y % 5 == 4 or (x + (4 if (y // 5) % 2 else 0) + var * 3) % 8 == 7:
                    px[x, y] = JOINT + (255,)
                elif y % 5 == 0:
                    px[x, y] = CURB_HI + (255,)
    elif mat in ("path", "sand", "dune", "meadow", "dirt", "rock", "mud"):
        # 칸마다 다른 점을 찍어 2×2 무늬 반복을 피한다 (모래색은 GPT 모래 표본)
        base, dots = {"path": (PATH, [PATH_DOT]), "sand": (SAND, SAND_DOTS), "dune": (SAND, SAND_DOTS),
                      "meadow": (MEADOW, MEADOW_DOTS), "dirt": (DIRT, DIRT_DOTS),
                      "rock": (ROCK, ROCK_DOTS), "mud": (MUDLAND, MUD_DOTS)}[mat]
        img = Image.new("RGBA", (16, 16), base + (255,))
        rng = random.Random(var * 7 + 3)
        px = img.load()
        for _ in range(4 if mat in ("path", "dirt") else 6):
            px[rng.randrange(1, 15), rng.randrange(1, 15)] = rng.choice(dots) + (255,)
        if mat == "rock":     # 석회암 금: 짧은 사선 두 줄
            for _ in range(2):
                x, y = rng.randrange(2, 12), rng.randrange(2, 12)
                for i in range(rng.choice((3, 4))):
                    px[x + i, y + (i // 2)] = ROCK_DOTS[0] + (255,)
        if mat == "dune" or (mat == "meadow" and var >= 6):   # 나무 밑·담장 옆·물가에만 풀 무더기
            for _ in range(3):
                x, y = rng.randrange(1, 13), rng.randrange(2, 15)
                for dx, dy in ((0, 0), (1, -1), (2, 0), (1, 0)):
                    px[x + dx, y + dy] = rng.choice(GRASS) + (255,)
    elif mat.startswith("g_"):   # 지역별 바닥 질감 (2×2타일 무늬 한 조각)
        img = _macro(mat, rot).crop((qx * 16, qy * 16, qx * 16 + 16, qy * 16 + 16)).copy()
        px = img.load()
    else:
        img = _macro({"slab": f"m_slab{var % 3}", "worn": f"m_slab{var % 3}", "herring": "m_herring", "mosaic": "m_mosaic"}[mat], rot).crop((qx * 16, qy * 16, qx * 16 + 16, qy * 16 + 16)).copy()
        px = img.load()
        if mat == "worn":   # 닳은 돌길: 석판에 모래색을 섞어 대비를 더 낮춘다
            for yy in range(16):
                for xx in range(16):
                    r, g, b, a = px[xx, yy]
                    px[xx, yy] = (round(r * .55 + SAND[0] * .45), round(g * .55 + SAND[1] * .45), round(b * .55 + SAND[2] * .45), a)
    sides = dict(zip("NESW", edges[:4]))
    for side, kind in sides.items():
        if kind != "-":
            kind = "Q" if kind == "q" and side == "S" else "R" if kind == "r" and side == "S" else "W" if kind == "w" and side == "S" else kind
            cols = LAND_EDGE[kind]
            _band(px, side, len(cols), cols)
    for corner, kind in zip(("NE", "SE", "SW", "NW"), edges[4:]):
        if kind != "-":
            _corner(px, corner, 3 if kind in "qd" else 2, LAND_EDGE[kind][0])
    return img


def _water(parts):
    _, _, kind, var, edges = parts
    from PIL import Image
    img = Image.new("RGBA", (16, 16), (PAN if kind == "pan" else WATER) + (255,))
    px = img.load()
    rng = random.Random(int(var))
    if kind != "pan" and int(var) % 3:          # 1/3은 잔잔한 칸
        for _ in range(int(var) % 3):
            x, y, n = rng.randrange(2, 11), rng.randrange(3, 14), rng.choice((3, 4))
            for i in range(n):
                px[x + i, y] = RIPPLE + (255,)
    for side, k in zip("NESW", edges[:4]):
        if k != "-":
            cols = WATER_EDGE[k]
            _band(px, side, len(cols), cols)
    for corner, k in zip(("NE", "SE", "SW", "NW"), edges[4:]):
        if k != "-":
            _corner(px, corner, 2, WATER_EDGE[k][0])
    return img


def ven_image(key):
    parts = key.split(":")
    if parts[0] == "vq":
        return _land(parts) if parts[1] == "L" else _water(parts)
    raise KeyError(key)


# 지역별 바닥: (지역, 구역) → 무늬 후보. 없는 조합은 기본 규칙(석판·풀밭·모래 등)을 쓴다.
STYLE_FLOOR = {
    "cap": {"street": ["g_cap_slab0", "g_cap_slab1"], "court": ["g_cap_slab0", "g_cap_slab1"],
            "quay": ["g_cap_edge"], "mosaic": ["g_cap_inlay"], "market": ["g_cap_inlay"]},
    "gls": {"street": ["g_gls_brick0", "g_gls_brick1"], "court": ["g_gls_brick0", "g_gls_brick1"],
            "quay": ["g_gls_brick0", "g_gls_brick1"], "market": ["g_gls_soot"], "dirt": ["g_gls_earth"]},
    "orc": {"meadow": ["g_orc_grass0", "g_orc_grass1"], "dirt": ["g_orc_dirt"], "path": ["g_orc_dirt"]},
    "fsh": {"worn": ["g_fsh_cobble0", "g_fsh_cobble1"], "street": ["g_fsh_cobble0", "g_fsh_cobble1"],
            "quay": ["g_fsh_cobble0", "g_fsh_cobble1"], "sand": ["g_fsh_wetsand"], "dirt": ["g_orc_dirt"]},
    "prt": {"street": ["g_prt_granite0", "g_prt_granite1"], "quay": ["g_prt_granite0", "g_prt_granite1"],
            "court": ["g_prt_granite0", "g_prt_granite1"], "dirt": ["g_prt_granite1"]},
}


class VeniceMap(Map):
    """구역(바닥 종류):
    도시 — 'street' 큰 석판 / 'court' 안뜰(흰 돌줄 테두리) / 'quay' 물가(석판 방향 90도) / 'mosaic' 우물 둘레 / 'market' 헤링본(입구 포인트용)
    자연 — 'meadow' 풀밭 / 'dirt' 흙길·작업 마당 / 'sand' 모래(물가 2칸 안의 풀밭은 자동으로 모래) / 'path' 다져진 모래길
    도시 구역의 길은 포장 그대로 그리고, 길 등급(속도)만 따로 기록한다."""
    PAVED = ("street", "market", "court", "quay", "mosaic", "worn")
    NATURAL = ("sand", "dune", "meadow", "dirt", "path", "rock", "mud")

    def __init__(self, mid, name, w, h, seed=1, zone="street"):
        super().__init__(mid, name, w, h, seed=seed)
        self.zone = [[zone] * w for _ in range(h)]
        self.pan = set()
        self.lush = set()        # 풀 무더기를 모아 둘 칸 (나무 밑·담장 옆·물가)
        self.doors = []          # (건물 이름, 문 앞 칸) — 검사용
        self.over_water = set()  # 물 위 부두·다리 칸 (물 그림은 유지, 통행 가능)
        self.style = [[None] * w for _ in range(h)]   # 지역(cap/gls/orc/fsh/prt) — 바닥 무늬 선택용

    def set_style(self, x0, y0, x1, y1, style):
        for y in range(max(0, y0), min(self.h, y1 + 1)):
            for x in range(max(0, x0), min(self.w, x1 + 1)):
                self.style[y][x] = style

    def set_zone(self, x0, y0, x1, y1, zone):
        for y in range(max(0, y0), min(self.h, y1 + 1)):
            for x in range(max(0, x0), min(self.w, x1 + 1)):
                self.zone[y][x] = zone

    def pave_streets(self, grade=2):
        """포장 구역에서 막히지 않은 땅 전체를 골목 길 등급으로 (수레 속도용)."""
        for y in range(self.h):
            for x in range(self.w):
                if self.zone[y][x] in self.PAVED and not self.water[y][x] and not self.blocked[y][x]                         and self.obj[y][x] is None:
                    self.road[y][x] = max(self.road[y][x], grade)

    # ---- 그림 칸별 덮인 정도 ----
    def _cell_stats(self, name):
        s, sx, sy, w, h, *_ = tiles.STAMPS[name]
        img = _atlas_img()
        out = {}
        for dy in range(h):
            for dx in range(w):
                data = list(img.crop(((sx + dx) * 16, (sy + dy) * 16, (sx + dx) * 16 + 16, (sy + dy) * 16 + 16)).getdata())
                cover = sum(1 for p in data if p[3] > 128) / 256
                dark = sum(1 for p in data if p[3] > 128 and p[0] < 60 and p[1] < 60 and p[2] < 60) / 256
                out[(dx, dy)] = (cover, dark)
        return s, sx, sy, w, h, out

    def building(self, name, x, y, high=0, open_cells=()):
        """그림이 실제로 차 있는 칸만 막는다 (ㄱ자 집의 빈 모퉁이·퍼걸러 통로는 통과).
        high: 위쪽 몇 줄을 플레이어 위에 그린다(덩굴 지붕·종탑 윗부분). open_cells: 일부러 통과시키는 (dx,dy)."""
        s, sx, sy, w, h, stats = self._cell_stats(name)
        if not (self.inside(x, y) and self.inside(x + w - 1, y + h - 1)):
            raise ValueError(f"맵 밖 건물: {name} {x},{y}")
        for (dx, dy), (cover, _) in stats.items():     # 먼저 전부 검사한다 (일부만 놓이지 않게)
            cx, cy = x + dx, y + dy
            if cover and (self.obj[cy][cx] or self.high[cy][cx] or self.water[cy][cx]):
                raise ValueError(f"겹침: {name} {cx},{cy}")
        for (dx, dy), (cover, _) in stats.items():
            cx, cy = x + dx, y + dy
            if cover == 0:
                continue
            key = K(s, sx + dx, sy + dy)
            if dy < high or (dy == 0 and cover < 0.3):   # 굴뚝 끝처럼 조금 걸친 맨 윗줄은 플레이어 위에 그린다
                self.high[cy][cx] = key
            else:
                self.obj[cy][cx] = key
                if cover >= 0.3 and (dx, dy) not in open_cells:
                    self.blocked[cy][cx] = True
                    self.road[cy][cx] = 0
            self.reserved[cy][cx] = True
        # 문: 맨 아래 줄의 어두운 칸 → 바로 아래 칸이 문 앞 (나무·담·퍼걸러·종탑 제외)
        plants = ("olive", "tree", "cypress", "lemon_tree", "garden_wall", "pergola", "campanile")
        for dx in (range(w) if not any(p in name for p in plants) else ()):
            cover, dark = stats[(dx, h - 1)]
            if dark > 0.18 and cover > 0.5:
                self.doors.append((name, (x + dx, y + h)))
        self.stamps.append((name, x, y))

    def deck_sprite(self, name, x, y):
        """물 위 부두·다리 그림. 판자가 충분히 덮인 칸만 걸을 수 있다. 물 그림은 아래에 그대로 둔다."""
        s, sx, sy, w, h, stats = self._cell_stats(name)
        for (dx, dy), (cover, _) in stats.items():
            cx, cy = x + dx, y + dy
            if cover == 0:
                continue
            self.deco[cy][cx] = K(s, sx + dx, sy + dy)
            if cover >= 0.45:
                self.over_water.add((cx, cy))
                self.road[cy][cx] = 2
            self.reserved[cy][cx] = True

    def deco_stamp(self, name, x, y):
        """밟고 지나갈 수 있는 바닥 장식(풀·돌·조개). 길·물·물체 위에는 놓지 않는다."""
        s, sx, sy, w, h, *_ = tiles.STAMPS[name]
        cells = [(x + dx, y + dy) for dy in range(h) for dx in range(w)]
        if not all(self.inside(*c) and not self.water[c[1]][c[0]] and not self.road[c[1]][c[0]]
                   and self.obj[c[1]][c[0]] is None and self.deco[c[1]][c[0]] is None for c in cells):
            return False
        for dy in range(h):
            for dx in range(w):
                self.deco[y + dy][x + dx] = K(s, sx + dx, sy + dy)
        return True

    def cluster_deco(self, names, cx, cy, radius, count):
        """한 지점 둘레에 바닥 장식을 모은다 (전체 무작위 살포 대신)."""
        placed = 0
        for _ in range(count * 25):
            if placed >= count:
                break
            if self.deco_stamp(self.rng.choice(names), cx + self.rng.randint(-radius, radius), cy + self.rng.randint(-radius, radius)):
                placed += 1

    def lush_around(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if self.inside(x, y):
                    self.lush.add((x, y))

    def _water_kind(self, x, y):
        if (x, y) in self.pan:
            return "pan"
        return "canal" if self.zone[y][x] in self.PAVED else "sea"

    def _is_water(self, x, y):
        return self.water[y][x] and (x, y) not in self.decks

    def _edge_kind(self, x, y, nx, ny):
        """땅 칸 (x,y)에서 이웃 (nx,ny)를 향한 테두리 종류"""
        if not self.inside(nx, ny):
            return "-"
        here, there = self.zone[y][x], self.zone[ny][nx]
        if self._is_water(nx, ny):
            if (nx, ny) in self.pan:
                return "d"
            return "q" if here in self.PAVED else "b"
        if here == "market" and there != "market" and there in self.PAVED:
            return "k"
        if here == "court" and there not in ("court", "mosaic") and there in self.PAVED:
            return "l"
        if here == "path" and there in ("sand", "dune", "meadow"):
            return "p"
        if here == "dirt" and there in ("meadow", "sand"):
            return "e"
        if here == "rock" and there != "rock":
            return "r"
        if here == "wall" and there != "wall":
            return "w"
        if here == "mud" and there in ("meadow", "sand", "dirt"):
            return "e"
        return "-"

    def prune_unreachable(self):
        """출구·스폰에서 갈 수 없는 칸(건물 그림 윗줄의 빈칸 등)은 길로 세지 않는다."""
        from collections import deque
        starts = [(o["x"], o["y"]) for o in self.objects if o["kind"] in ("spawn", "exit")]
        seen, q = set(starts), deque(starts)
        while q:
            x, y = q.popleft()
            for p in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if p not in seen and self.inside(*p) and not self.blocked[p[1]][p[0]] and                         (not self.water[p[1]][p[0]] or p in self.over_water or p in self.decks):
                    seen.add(p); q.append(p)
        for y in range(self.h):
            for x in range(self.w):
                if (x, y) not in seen and not self.water[y][x]:
                    self.road[y][x] = 0

    def finish(self):
        # 공용 smooth()는 대각선 길을 넓은 덩어리로 메우므로 쓰지 않는다
        self.prune_unreachable()
        for y in range(self.h):
            for x in range(self.w):
                z = self.zone[y][x]
                if z in ("sand", "dune") and self.road[y][x] > 0 and not self.water[y][x]:
                    self.zone[y][x] = "path"          # 모래 위 길
                elif z == "meadow" and self.road[y][x] > 0 and not self.water[y][x]:
                    self.zone[y][x] = "dirt"          # 풀밭 위 길은 흙길
        # 물가 2칸 안의 풀밭은 모래 (풀밭 → 모래 → 젖은 흙 → 물)
        shore = [(x, y) for y in range(self.h) for x in range(self.w)
                 if self.zone[y][x] == "meadow" and not self.water[y][x] and any(
                     self.inside(x + dx, y + dy) and self._is_water(x + dx, y + dy) and (x + dx, y + dy) not in self.pan
                     for dx in range(-2, 3) for dy in range(-2, 3))]
        for x, y in shore:
            self.zone[y][x] = "sand"
        N8 = (("N", 0, -1), ("E", 1, 0), ("S", 0, 1), ("W", -1, 0))
        D4 = ((1, -1), (1, 1), (-1, 1), (-1, -1))   # NE SE SW NW
        for y in range(self.h):
            for x in range(self.w):
                if (x, y) in self.decks:
                    continue
                z = self.zone[y][x]
                if self.water[y][x]:
                    kind = self._water_kind(x, y)
                    sides = []
                    for _, dx, dy in N8:
                        nx, ny = x + dx, y + dy
                        if not self.inside(nx, ny) or self.water[ny][nx] or (nx, ny) in self.decks:
                            sides.append("-")
                        else:
                            sides.append("d" if kind == "pan" else ("q" if self.zone[ny][nx] in self.PAVED else "b"))
                    corners = []
                    for dx, dy in D4:
                        nx, ny = x + dx, y + dy
                        side_open = (self.inside(x + dx, y) and not self.water[y][x + dx]) or (self.inside(x, y + dy) and not self.water[y + dy][x])
                        land = self.inside(nx, ny) and not self.water[ny][nx] and (nx, ny) not in self.decks
                        corners.append(("d" if kind == "pan" else "q" if self.zone[ny][nx] in self.PAVED else "b") if land and not side_open else "-")
                    edge = ''.join(sides) + ''.join(corners)
                    var = self.rng.randrange(9) if edge == '-' * 8 else 0   # 물가 칸은 잔잔한 한 종 (타일 수 절약)
                    self.ground[y][x] = f"vq:W:{kind}:{var}:{edge}"
                    self.blocked[y][x] = (x, y) not in self.over_water
                    continue
                if self.ground[y][x] is not None and self.road[y][x] == 0:
                    continue
                mat = {"street": "slab", "court": "slab", "quay": "slab", "market": "herring", "mosaic": "mosaic", "worn": "worn",
                       "sand": "sand", "dune": "dune", "path": "path", "meadow": "meadow", "dirt": "dirt",
                       "rock": "rock", "mud": "mud", "wall": "wall"}[z]
                bx, by = x // 2, y // 2
                var = (bx * 7 + by * 13 + (bx * by) % 5) % 3 if mat in ("slab", "worn") else (bx + by) % 4
                rot = 1 if z == "quay" else 0
                if mat in self.NATURAL:    # 칸마다 무작위 점: 무늬 묶음 위치는 쓰지 않는다
                    var = (x * 7 + y * 13 + (x * y) % 11) % 6
                    if mat == "meadow" and (x, y) in self.lush:
                        var = 6 + var % 3
                sides = "".join(self._edge_kind(x, y, x + dx, y + dy) for _, dx, dy in N8)
                corners = ""
                for dx, dy in D4:
                    k = self._edge_kind(x, y, x + dx, y + dy)
                    straight = self._edge_kind(x, y, x + dx, y) != "-" or self._edge_kind(x, y, x, y + dy) != "-"
                    corners += k if k in "qbd" and not straight else "-"
                st = self.style[y][x]
                cands = STYLE_FLOOR.get(st, {}).get(z)
                if st == "orc" and z == "meadow" and (x, y) in self.lush and (bx * 3 + by) % 3 == 0:
                    cands = ["g_orc_petal"]                              # 나무 밑 꽃잎 풀밭 (일부만)
                if st == "prt" and z in ("street", "quay", "dirt") and self.road[y][x] >= 3:
                    cands = ["g_prt_ruts"]                               # 수레 길 바퀴 자국
                    rot = 1 if (self.inside(x - 1, y) and self.road[y][x - 1] >= 3) or (self.inside(x + 1, y) and self.road[y][x + 1] >= 3) else 0
                elif st == "prt" and z == "street" and (bx * 7 + by * 11) % 23 == 0:
                    cands = ["g_prt_stain"]                              # 드문 기름·물 얼룩
                if cands:
                    mat = cands[(bx * 7 + by * 13 + (bx * by) % 5) % len(cands)]
                    var = 0
                qx, qy = (0, 0) if mat in self.NATURAL else (x % 2, y % 2)
                self.ground[y][x] = f"vq:L:{mat}:{var}:{qx}:{qy}:{rot}:{sides}{corners}"
        # 원본 물결 애니메이션(ripples)은 원래 하늘색 물 바탕이 그림에 들어 있어 청록 물 위에서 네모로 보인다 → 쓰지 않는다
