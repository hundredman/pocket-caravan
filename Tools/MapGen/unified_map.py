"""모든 지역 M 지도를 한 그림체로 (2026-11-05, UI B안 통일).
    지역마다 그림체가 달랐다(베네치아 타일 축소+피자 조각, 북유럽 눈 지도, 고려 녹색 섬, 아라비아 타일 축소).
    이제 같은 방법 하나로 그린다: 실제 맵 JSON을 그려 칸마다 평균 색을 재고, 공용 파스텔 팔레트 중 가장 가까운 색으로 칠한다.
    그 위에 같은 규칙으로 길(맵 road 칸)·해안선·나무 머리·건물 테두리·맵 바깥 선을 얹는다. 맵 밖은 종이색.
    자리(배치)는 지역별 기존 배치 JSON을 그대로 쓰고 맵이 있는 곳만 남게 잘라 낸다.
    사용: python Tools/MapGen/unified_map.py   (맵 JSON이나 배치를 다시 만든 뒤 실행)
    출력: Assets/PocketCaravan/Resources/WorldMap/Unified/{지역}Map.png, {지역}MapLayout.json (각 지역 지도 UI가 읽는다)
    아라비아 배치는 이 파일이 직접 푼다(출구 맞춤 + 남부 바둑판, 옛 arabia_tilemap.py와 같은 방식)."""
import glob, json, os
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MAPS = os.path.join(ROOT, "Assets/PocketCaravan/Maps")
TILES = os.path.join(ROOT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")
WM = os.path.join(ROOT, "Assets/PocketCaravan/Resources/WorldMap")
OUT = os.path.join(WM, "Unified")
T, PPT, PAD = 16, 4, 4

# ---------------- 공용 팔레트 (UI B안 종이·파스텔) ----------------
PAL = {
    "water": (166, 212, 230), "deep": (140, 195, 219),
    "grass": (197, 223, 160), "tree": (147, 194, 122),
    "sand": (239, 221, 168), "dirt": (221, 193, 149),
    "stone": (218, 212, 200), "snow": (242, 244, 242),
    "rock": (224, 172, 136), "roof": (232, 160, 126), "wood": (201, 162, 122),
}
NAMES = list(PAL)
COLORS = np.array([PAL[n] for n in NAMES], float)
PAPER, PAPER_DOT = (244, 232, 207), (232, 216, 180)
ROAD, ROAD_EDGE = (215, 172, 116), (184, 138, 85)
COAST_LAND, COAST_WATER = (139, 107, 78), (111, 159, 184)
OUTLINE, BUILD_LINE = (179, 154, 112), (122, 90, 72)
TREE_DARK, WAVE = (111, 159, 90), (205, 231, 240)
LAND_CLASSES = {"grass", "tree", "sand", "dirt", "stone", "snow", "rock", "roof", "wood"}


def load_maps():
    files = {}
    for f in glob.glob(os.path.join(MAPS, "**", "*.json"), recursive=True):
        try:
            with open(f, encoding="utf-8") as fp:
                d = json.load(fp)
        except Exception:
            continue
        if isinstance(d, dict) and "id" in d and "ground" in d:
            files[d["id"]] = d
    return files


tile_cache = {}


def tile(name):
    if name not in tile_cache:
        p = os.path.join(TILES, name + ".png")
        tile_cache[name] = Image.open(p).convert("RGBA") if os.path.exists(p) else None
    return tile_cache[name]


def render(d, layers):
    im = Image.new("RGBA", (d["w"] * T, d["h"] * T), (0, 0, 0, 0))
    pal = d["palette"]
    for layer in layers:
        for i, v in enumerate(d.get(layer) or []):
            if not v:
                continue
            t = tile(pal[v])
            if t is None:
                continue
            x, y = i % d["w"], i // d["w"]
            if t.size != (T, T):
                im.alpha_composite(t, (x * T, (y + 1) * T - t.size[1]))
            else:
                im.alpha_composite(t, (x * T, y * T))
    return np.asarray(im).astype(float)


def cell_means(a, w, h):
    """칸(16×16)마다 알파 가중 평균 색과 덮인 정도"""
    a = a[:h * T, :w * T].reshape(h, T, w, T, 4)
    alpha = a[..., 3:4] / 255.0
    cover = alpha.mean(axis=(1, 3))[..., 0]
    rgb = (a[..., :3] * alpha).sum(axis=(1, 3)) / np.maximum(alpha.sum(axis=(1, 3)), 1e-6)
    return rgb, cover


def hsv(rgb):
    """색 → 색상(도)·채도·밝기 (칸 배열 그대로)"""
    c = rgb / 255.0
    mx, mn = c.max(-1), c.min(-1)
    v, delta = mx, mx - mn
    s = np.where(mx > 0, delta / np.maximum(mx, 1e-6), 0)
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    dd = np.maximum(delta, 1e-6)
    h = np.where(mx == r, ((g - b) / dd) % 6, np.where(mx == g, (b - r) / dd + 2, (r - g) / dd + 4)) * 60
    return np.where(delta > 0, h, 0), s, v


def I(name): return NAMES.index(name)


def ground_class(rgb):
    """바닥 색 → 종류 (색상 기준이라 지역마다 다른 풀색·모래색도 같은 칸으로 모인다)"""
    h, s, v = hsv(rgb)
    out = np.full(h.shape, I("dirt"))
    out[(h >= 15) & (h < 60) & (v >= .75)] = I("sand")
    out[(h < 15) | (h >= 330)] = I("rock")
    green = (h >= 60) & (h < 165)
    out[green] = I("grass")
    out[green & (v < .36)] = I("tree")                     # 아주 짙은 초록 바닥 = 숲
    out[s < .15] = I("stone")
    out[(s <= .10) & (v >= .85)] = I("snow")
    water = (h >= 165) & (h <= 250) & (s >= .18)
    out[water] = I("water")
    out[water & (v < .55)] = I("deep")
    return out


def object_class(rgb):
    h, s, v = hsv(rgb)
    out = np.full(h.shape, I("wood"))
    out[(h < 20) | (h >= 330)] = I("roof")
    out[(h >= 60) & (h < 170) & (s >= .15)] = I("tree")
    out[s < .15] = I("stone")
    return out


def classify(d):
    """칸마다 팔레트 번호. 물건(건물·나무·바위)이 칸을 반 넘게 덮으면 물건 색, 아니면 바닥 색"""
    w, h = d["w"], d["h"]
    g_rgb, g_cov = cell_means(render(d, ("ground", "deco")), w, h)
    o_rgb, o_cov = cell_means(render(d, ("obj", "high")), w, h)
    ground = ground_class(g_rgb)
    objs = object_class(o_rgb)
    cls = np.where(o_cov > 0.4, objs, ground)
    cls[(g_cov < 0.3) & (o_cov < 0.3)] = -1                 # 아무것도 안 그린 칸 = 맵 밖 (종이)
    road = np.array(d.get("road") or [0] * (w * h)).reshape(h, w) > 0
    water = np.isin(cls, [NAMES.index("water"), NAMES.index("deep")])
    return cls, road & ~water & (cls >= 0)


def compose(layout, maps):
    W, H = int(layout["width"]), int(layout["height"])
    grid = np.full((H, W), -1, int)
    road = np.zeros((H, W), bool)
    for a in layout["maps"]:
        d = maps[a["mapId"]]
        cls, rd = classify(d)
        x, y = int(round(a["x"])), int(round(a["y"]))
        h, w = cls.shape
        x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
        sub = cls[y0 - y:y1 - y, x0 - x:x1 - x]
        view = grid[y0:y1, x0:x1]
        view[sub >= 0] = sub[sub >= 0]
        road[y0:y1, x0:x1] |= rd[y0 - y:y1 - y, x0 - x:x1 - x]
    return fill_gaps(grid), road


def shift_or(m, r):
    """r칸 안에 참이 하나라도 있으면 참 (네모 팽창)"""
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def fill_gaps(grid, r=3):
    """맵 사이 좁은 틈(r칸 이하)은 옆 땅으로 메워 한 덩어리로 보이게 (닫기 = 팽창 후 침식)"""
    have = grid >= 0
    closed = ~shift_or(~shift_or(have, r), r)
    todo = closed & ~have
    for _ in range(r + 1):
        if not todo.any():
            break
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            src = np.roll(np.roll(grid, dy, 0), dx, 1)
            take = todo & (src >= 0) & (grid < 0)
            grid[take] = src[take]
        todo &= grid < 0
    return grid


def crop(layout, grid, road):
    ys, xs = np.where(grid >= 0)
    x0, y0 = int(max(0, xs.min() - PAD)), int(max(0, ys.min() - PAD))
    x1, y1 = int(min(grid.shape[1], xs.max() + 1 + PAD)), int(min(grid.shape[0], ys.max() + 1 + PAD))
    out = json.loads(json.dumps(layout))
    out["width"], out["height"] = x1 - x0, y1 - y0
    for m in out["maps"]:
        for k, o in (("x", x0), ("y", y0), ("labelX", x0), ("labelY", y0)):
            m[k] = round(m[k] - o, 1)
    for key in ("annotations", "settlements"):
        keep = []
        for n in out.get(key) or []:
            n["x"], n["y"] = round(n["x"] - x0, 1), round(n["y"] - y0, 1)
            if 0 <= n["x"] < out["width"] and 0 <= n["y"] < out["height"]:
                keep.append(n)
        if key in out:
            out[key] = keep
    out["terrainSource"] = "Tools/MapGen/unified_map.py"
    out["pixelsPerTile"] = PPT
    return out, grid[y0:y1, x0:x1], road[y0:y1, x0:x1]


def draw(grid, road):
    H, W = grid.shape
    px = np.zeros((H * PPT, W * PPT, 3), np.uint8)
    col = np.array([PAL[n] for n in NAMES] + [PAPER], np.uint8)
    g = np.where(grid < 0, len(NAMES), grid)
    tree, roof, wood, stone, rock = (NAMES.index(n) for n in ("tree", "roof", "wood", "stone", "rock"))
    base = g.copy()
    base[g == tree] = NAMES.index("grass")                           # 나무 칸 바닥은 풀, 나무는 위에 동그라미로
    px[:] = np.repeat(np.repeat(col[base], PPT, 0), PPT, 1)
    land = np.isin(grid, [NAMES.index(n) for n in LAND_CLASSES])
    water = np.isin(grid, [NAMES.index("water"), NAMES.index("deep")])
    empty = grid < 0
    building = np.isin(grid, [roof, wood]) | ((grid == stone) & ~road)

    def neighbors(mask):
        n = np.zeros_like(mask)
        n[1:, :] |= mask[:-1, :]; n[:-1, :] |= mask[1:, :]; n[:, 1:] |= mask[:, :-1]; n[:, :-1] |= mask[:, 1:]
        return n
    img = Image.fromarray(px)
    d = ImageDraw.Draw(img)
    # 종이 바탕 점
    for y in range(0, H * PPT, 9):
        for x in range((y // 9 % 2) * 4, W * PPT, 9):
            if empty[min(H - 1, y // PPT), min(W - 1, x // PPT)]:
                d.point((x, y), fill=PAPER_DOT)
    # 길: 칸을 칠하고 바깥 가장자리만 진하게
    road_edge = road & neighbors(~road)
    for y, x in zip(*np.where(road)):
        d.rectangle((x * PPT, y * PPT, x * PPT + PPT - 1, y * PPT + PPT - 1), fill=ROAD)
    def edge_lines(mask, other, color):
        """mask 칸 중 other 칸과 맞닿은 변에 1px 선"""
        for y, x in zip(*np.where(mask)):
            X, Y = x * PPT, y * PPT
            if y > 0 and other[y - 1, x]: d.line((X, Y, X + PPT - 1, Y), fill=color)
            if y < H - 1 and other[y + 1, x]: d.line((X, Y + PPT - 1, X + PPT - 1, Y + PPT - 1), fill=color)
            if x > 0 and other[y, x - 1]: d.line((X, Y, X, Y + PPT - 1), fill=color)
            if x < W - 1 and other[y, x + 1]: d.line((X + PPT - 1, Y, X + PPT - 1, Y + PPT - 1), fill=color)
    edge_lines(road, ~road & ~water & ~empty, ROAD_EDGE)
    # 물결 무늬 (드문드문)
    for y, x in zip(*np.where(water)):
        if (x * 7 + y * 13) % 29 == 0 and x + 2 < W and water[y, min(W - 1, x + 2)]:
            X, Y = x * PPT, y * PPT + 2
            d.line((X, Y, X + 2, Y - 1), fill=WAVE); d.line((X + 2, Y - 1, X + 5, Y), fill=WAVE)
    # 건물: 묶음 바깥 테두리
    edge_lines(building, ~building & ~empty, BUILD_LINE)
    # 해안선: 물 쪽 옅은 선 + 땅 쪽 진한 선
    edge_lines(water, land, COAST_WATER)
    edge_lines(land, water, COAST_LAND)
    # 맵 바깥 선
    edge_lines(land | water, empty, OUTLINE)
    # 나무 머리: 나무 칸 두 칸마다 동그라미
    for y, x in zip(*np.where(grid == tree)):
        if (x + y) % 2:
            continue
        cx, cy = x * PPT + 2, y * PPT + 2
        d.ellipse((cx - 3, cy - 3, cx + 3, cy + 3), fill=PAL["tree"], outline=TREE_DARK)
        d.point((cx - 1, cy - 1), fill=(186, 220, 160))
    return img


# ---------------- 아라비아 배치 (출구 맞춤 + 남부 바둑판) ----------------
HOME_GRID = [[None, "arabia_sanaa", None],
             ["arabia_shortcut", "arabia_taiz", "arabia_zabid"],
             ["arabia_ahsa", "arabia_farmroad", "arabia_jeddah"]]


def arabia_layout(maps):
    ids = sorted([m for m in maps if m.startswith("arabia_")], key=lambda m: (m != "arabia_central_bazaar", m))
    central = [m for m in ids if m.startswith("arabia_central_")]

    def exits(dd):
        for o in dd["objects"]:
            if o.get("type") == "exit" or (o.get("target") and o.get("edge")):
                yield o
    rows, rhs = [], []
    for a in central:
        A = maps[a]
        for o in exits(A):
            b = o["target"]
            if b not in central or central.index(b) < central.index(a):
                continue
            B = maps[b]
            back = [e for e in exits(B) if e["target"] == a]
            if not back:
                continue
            e = min(back, key=lambda e: abs(e["y"] - o["y"]) + abs(e["x"] - o["x"]))
            ax, ay = o["x"] + o["w"] / 2, o["y"] + o["h"] / 2
            bx, by = e["x"] + e["w"] / 2, e["y"] + e["h"] / 2
            dx, dy = {"E": (A["w"], ay - by), "W": (-B["w"], ay - by), "N": (ax - bx, -B["h"])}.get(o["edge"], (ax - bx, A["h"]))
            for axis, delta in ((0, dx), (1, dy)):
                r = np.zeros(2 * len(central)); r[2 * central.index(b) + axis] = 1; r[2 * central.index(a) + axis] = -1
                rows.append(r); rhs.append(delta)
    for axis in (0, 1):
        r = np.zeros(2 * len(central)); r[axis] = 1; rows.append(r * 100); rhs.append(0)
    sol = np.linalg.lstsq(np.array(rows), np.array(rhs), rcond=None)[0]
    pos = {m: [int(round(sol[2 * i])), int(round(sol[2 * i + 1]))] for i, m in enumerate(central)}
    left = min(pos[m][0] for m in central); right = max(pos[m][0] + maps[m]["w"] for m in central)
    top = max(pos[m][1] + maps[m]["h"] for m in ("arabia_central_dunes", "arabia_central_canyon"))
    gx = int((left + right) / 2 - 64 * 1.5)
    for r_, row in enumerate(HOME_GRID):
        for c_, m in enumerate(row):
            if m in maps:
                pos[m] = [gx + c_ * 64, top + r_ * 44]
    x0 = min(p[0] for p in pos.values()); y0 = min(p[1] for p in pos.values())
    entries = []
    for m, (x, y) in pos.items():
        dd = maps[m]; x -= x0; y -= y0
        market = next((o for o in dd["objects"] if o.get("kind") == "market"), None)
        lx, ly = (market["x"] + .5, market["y"] + .5) if market else (dd["w"] / 2, dd["h"] / 2)
        entries.append(dict(mapId=m, x=x, y=y, w=dd["w"], h=dd["h"], labelX=round(x + lx, 1), labelY=round(y + ly, 1)))
    W = max(e["x"] + e["w"] for e in entries); H = max(e["y"] + e["h"] for e in entries)
    home = [e for e in entries if not e["mapId"].startswith("arabia_central_")]
    notes = [dict(text="중앙 대륙 남쪽 조각", x=2, y=2),
             dict(text="아라비아 남부 (예멘·홍해 길)", x=min(e["x"] for e in home), y=min(e["y"] for e in home) - 4)]
    return dict(width=W, height=H, maps=entries, annotations=notes, geography="unified")


REGIONS = [
    ("Goryeo", "GoryeoLayout.json"),
    ("GoryeoRegional", "GoryeoRegionalLayout.json"),
    ("Venice", "VeniceLayout.json"),
    ("North", "NorthLayout.json"),
    ("Arabia", None),
]


def main():
    maps = load_maps()
    os.makedirs(OUT, exist_ok=True)
    for name, src in REGIONS:
        if src:
            with open(os.path.join(WM, src), encoding="utf-8") as fp:
                layout = json.load(fp)
        else:
            layout = arabia_layout(maps)
        grid, road = compose(layout, maps)
        out, grid, road = crop(layout, grid, road)
        img = draw(grid, road)
        img.save(os.path.join(OUT, f"{name}Map.png"))
        with open(os.path.join(OUT, f"{name}MapLayout.json"), "w", encoding="utf-8", newline="\n") as fp:
            json.dump(out, fp, ensure_ascii=False, indent=1)
        print(name, img.size, "cells", out["width"], out["height"])


if __name__ == "__main__":
    main()
