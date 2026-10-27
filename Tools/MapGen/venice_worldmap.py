"""베네치아·본토 서부 교역도 (M 지도). 북유럽 교역도(north_cartography.py)와 같은 종이 그림체를 따르되, 게임 화면 크기에서
읽히도록 단순하게 그린다 (2026-10-26 수정: 칸 단위 소품 대신 윤곽·굵은 길·장소 아이콘).
    사용: python Tools/MapGen/venice_worldmap.py   (맵 생성기를 직접 불러 그린다)
    출력: Assets/PocketCaravan/Resources/WorldMap/VeniceTerrain.png, VeniceLayout.json (VeniceMapUI가 읽는다)
    - 서쪽 석호: 섬은 맵의 땅 칸을 부드럽게 다듬은 윤곽. 동쪽 중앙 대륙: 서쪽 피자 조각(따뜻한 색)·이웃 조각(회색+숲 기호)·경계 점선.
    - 길: 각 씬 안에서 출구 → 중심(장터)까지 실제로 걸을 수 있는 최단 경로를 매끈하게 줄인 굵은 선. 씬 사이는 출구끼리(뱃길은 점선).
    - 장소: 씬마다 큰 아이콘 하나(도시·마을·나루·포도밭·습지·고개·돌다리·관문). 이름표는 VeniceMapUI가 아이콘 아래에 단다.
    - 배치는 보기 위한 축약이며 가격용 worldCoord와 무관하다. 고려·북유럽 지도 파일은 건드리지 않는다."""
import json, math, os, random, uuid
from collections import deque
from PIL import Image, ImageDraw, ImageFilter
from world_registry import ROOT, generated_maps
import venice_mainland

OUT = os.path.join(ROOT, "Assets/PocketCaravan/Resources/WorldMap")
S = 4                       # 그림 픽셀 / 칸
POS = {
    "venice_santerasmo": (10, 80), "venice_burano": (10, 150),
    "venice_rialto": (82, 110), "venice_murano": (171, 70), "venice_chioggia": (171, 140),
}
MAIN_X, MAIN_Y = 250, -3
for _k, (_x, _y) in venice_mainland.ORIGIN.items():
    POS[_k] = (MAIN_X + _x, MAIN_Y + _y)
W, H = 506, 270
ICON = {
    "venice_rialto": "capital", "venice_santerasmo": "orchard", "venice_murano": "glass", "venice_burano": "fishing",
    "venice_chioggia": "port", "venice_c_landing": "dock", "venice_c_vineyard": "vine", "venice_c_marsh": "marsh",
    "venice_c_pass": "hills", "venice_c_bridge": "bridge", "venice_c_post": "inn", "venice_c_gate": "gate",
}
# 색 (종이 그림체)
SEA = (176, 210, 206); WAVE = (128, 170, 172)
LAND = (238, 226, 192); NEIGHBOR = (216, 210, 194); ISLAND = (240, 226, 186)
COAST = (120, 100, 74); LAKE = (170, 208, 208); LAKE_EDGE = (106, 150, 156)
ROAD_EDGE = (122, 88, 54); ROAD = (222, 186, 128); BOAT = (140, 90, 60)
ROOF = (178, 82, 56); WALL = (240, 228, 200); INK = (92, 72, 50); GREEN = (122, 146, 96); GREEN_DARK = (92, 116, 72)


def walkable(m, x, y):
    return m.inside(x, y) and not m.blocked[y][x] and (not m.water[y][x] or (x, y) in m.decks or (x, y) in m.over_water)


def path(m, a, b):
    """칸 a → b 최단 걷기 경로"""
    prev = {a: None}
    q = deque([a])
    while q:
        c = q.popleft()
        if c == b:
            break
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (c[0] + dx, c[1] + dy)
            if n not in prev and walkable(m, *n):
                prev[n] = c
                q.append(n)
    if b not in prev:
        return None
    out, c = [], b
    while c is not None:
        out.append(c)
        c = prev[c]
    return out[::-1]


def simplify(pts, eps):
    """Douglas-Peucker: 계단 모양 칸 경로를 몇 개의 꺾은선으로"""
    if len(pts) < 3:
        return pts
    (x0, y0), (x1, y1) = pts[0], pts[-1]
    L = math.hypot(x1 - x0, y1 - y0) or 1
    best, idx = 0, 0
    for i in range(1, len(pts) - 1):
        x, y = pts[i]
        dist = abs((x1 - x0) * (y0 - y) - (x0 - x) * (y1 - y0)) / L
        if dist > best:
            best, idx = dist, i
    if best <= eps:
        return [pts[0], pts[-1]]
    return simplify(pts[:idx + 1], eps)[:-1] + simplify(pts[idx:], eps)


def hub_of(m):
    """씬의 중심점: 장터 옆 걸을 수 있는 칸, 없으면 가운데에서 가장 가까운 길."""
    market = next((o for o in m.objects if o["kind"] == "market"), None)
    tx, ty = (market["x"], market["y"]) if market else (m.w // 2, m.h // 2)
    best = None
    for y in range(m.h):
        for x in range(m.w):
            if walkable(m, x, y) and (market or m.road[y][x]):
                dd = (x - tx) ** 2 + (y - ty) ** 2
                if best is None or dd < best[0]:
                    best = (dd, (x, y))
    return best[1]


def exit_cell(o):
    """출구 줄의 가운데 칸"""
    return (o["x"] + o["w"] // 2, o["y"] + o["h"] // 2)


def main():
    maps = {m.id: m for region in ("venice", "center_venice") for m in generated_maps(region)}
    rng = random.Random(41)
    big = (W * S, H * S)
    # ---------- 바탕: 바다 / 대륙(서쪽 조각은 따뜻한 색, 이웃 조각은 회색) ----------
    zone = Image.new("L", (W, H), 0)          # 0 바다, 128 이웃 조각, 255 서쪽 조각
    zp = zone.load()
    for y in range(H):
        for x in range(W):
            gx, gy = x - MAIN_X, y - MAIN_Y
            if gx >= 13 + 3.0 * math.sin(gy / 9.0) + 1.5 * math.sin(gy / 3.7):
                zp[x, y] = 255 if venice_mainland.edge_top(gx) <= gy <= venice_mainland.edge_bottom(gx) else 128
    im = Image.new("RGB", big, SEA)
    d = ImageDraw.Draw(im)
    for i in range(10):                       # 종이 지도 테두리처럼 바깥을 조금 짙게
        d.rectangle((i, i, big[0] - 1 - i, big[1] - 1 - i), outline=tuple(int(c - (10 - i) * 1.6) for c in SEA))
    cont = zone.point(lambda v: 255 if v else 0).resize(big, Image.BILINEAR).filter(ImageFilter.GaussianBlur(6)).point(lambda v: 255 if v > 127 else 0)
    wedge = zone.point(lambda v: 255 if v == 255 else 0).resize(big, Image.NEAREST)
    im.paste(NEIGHBOR, mask=cont)
    im.paste(LAND, mask=Image.composite(wedge, Image.new("L", big, 0), cont))
    im.paste(COAST, mask=cont.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(3)))
    # ---------- 석호 섬 윤곽 / 본토 물(습지·강) ----------
    for mid, m in maps.items():
        mx, my = POS[mid]
        mask = Image.new("L", (m.w, m.h), 0)
        mp = mask.load()
        lagoon = not mid.startswith("venice_c_")
        for y in range(m.h):
            for x in range(m.w):
                water = m.water[y][x] and (x, y) not in m.decks
                if (lagoon and not water) or (not lagoon and water and mid != "venice_c_landing"):
                    mp[x, y] = 255
        mask = mask.resize((m.w * S, m.h * S), Image.BILINEAR).filter(ImageFilter.GaussianBlur(5)).point(lambda v: 255 if v > 110 else 0)
        outline = mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.MaxFilter(3))
        im.paste(ISLAND if lagoon else LAKE, (mx * S, my * S, (mx + m.w) * S, (my + m.h) * S), mask)
        im.paste(COAST if lagoon else LAKE_EDGE, (mx * S, my * S, (mx + m.w) * S, (my + m.h) * S), outline)
    # ---------- 빈 종이 기호: 바다 물결, 이웃 조각 숲, 조각 안 들판 ----------
    covered = Image.new("L", (W, H), 0)
    cdraw = ImageDraw.Draw(covered)
    for mid, m in maps.items():
        mx, my = POS[mid]
        cdraw.rectangle((mx - 4, my - 4, mx + m.w + 4, my + m.h + 4), fill=255)
    cp = covered.load()
    for gy in range(10, H - 8, 16):
        for gx in range(10, W - 8, 20):
            x, y = gx + rng.randint(-3, 3) + (8 if (gy // 16) % 2 else 0), gy + rng.randint(-2, 2)
            if not (0 <= x < W and 0 <= y < H) or cp[x, y]:
                continue
            p, q = x * S, y * S
            if zp[x, y] == 0:
                for ox in (-7, 7):
                    d.arc((p + ox - 8, q - 4, p + ox + 8, q + 4), 200, 340, fill=WAVE, width=2)
            elif zp[x, y] == 128:
                for ox in (-6, 6):
                    d.polygon(((p + ox, q - 9), (p + ox - 6, q + 4), (p + ox + 6, q + 4)), fill=(170, 176, 140), outline=(140, 148, 112))
            elif rng.random() < 0.35:
                d.ellipse((p - 6, q - 5, p + 6, q + 5), fill=(196, 200, 150), outline=(160, 166, 118))
    def P(gx, gy):
        return (MAIN_X + gx) * S, (MAIN_Y + gy) * S
    for fn in (venice_mainland.edge_top, venice_mainland.edge_bottom):     # 피자 조각 경계 (점선)
        for gx in range(10, W - MAIN_X, 8):
            a, b = P(gx, fn(gx)), P(gx + 4, fn(gx + 4))
            if 0 <= a[1] < big[1] and 0 <= b[1] < big[1]:
                d.line((a, b), fill=(150, 104, 66), width=4)
    # ---------- 길: 씬 안(출구 → 중심 실제 경로), 씬 사이(출구끼리) ----------
    strokes, boats, hubs = [], [], {}
    for mid, m in maps.items():
        mx, my = POS[mid]
        hub = hub_of(m)
        hubs[mid] = (mx + hub[0], my + hub[1])
        for o in m.objects:
            if o["kind"] != "exit":
                continue
            start = exit_cell(o)
            pts = simplify(path(m, start, hub) or [start, hub], 2.5)
            strokes.append([((mx + x + .5) * S, (my + y + .5) * S) for x, y in pts])
    done = set()
    for k, m in maps.items():
        for o in m.objects:
            if o["kind"] != "exit" or o["target"] not in maps:
                continue
            t = o["target"]
            cand = [b for b in maps[t].objects if b["kind"] == "exit" and b["target"] == k]
            suf = o["spawn"].rsplit("_", 1)[-1]
            b = next((b for b in cand if b["spawn"].rsplit("_", 1)[-1] == suf), cand[0])
            pair = tuple(sorted([(k, o["x"], o["y"]), (t, b["x"], b["y"])]))
            if pair in done:
                continue
            done.add(pair)
            (ax, ay), (bx, by) = exit_cell(o), exit_cell(b)
            p1 = ((POS[k][0] + ax + .5) * S, (POS[k][1] + ay + .5) * S)
            p2 = ((POS[t][0] + bx + .5) * S, (POS[t][1] + by + .5) * S)
            (boats if {k, t} == {"venice_chioggia", "venice_c_landing"} else strokes).append([p1, p2])
    for width, color, r in ((11, ROAD_EDGE, 5), (6, ROAD, 3)):      # 테두리 → 속 (두 번 그려 길처럼)
        for pts in strokes:
            d.line(pts, fill=color, width=width, joint="curve")
            for p in pts:
                d.ellipse((p[0] - r, p[1] - r, p[0] + r, p[1] + r), fill=color)
    for p, q in boats:                       # 뱃길: 점선 + 가운데 돛배
        n = 14
        for i in range(0, n, 2):
            a = (p[0] + (q[0] - p[0]) * i / n, p[1] + (q[1] - p[1]) * i / n)
            c = (p[0] + (q[0] - p[0]) * (i + 1) / n, p[1] + (q[1] - p[1]) * (i + 1) / n)
            d.line((a, c), fill=BOAT, width=5)
        cx, cy = (p[0] + q[0]) / 2, (p[1] + q[1]) / 2 - 10
        d.polygon(((cx - 14, cy), (cx + 14, cy), (cx + 9, cy + 7), (cx - 9, cy + 7)), fill=(150, 98, 64), outline=INK)
        d.line((cx, cy, cx, cy - 18), fill=INK, width=2)
        d.polygon(((cx + 1, cy - 17), (cx + 12, cy - 4), (cx + 1, cy - 4)), fill=(244, 236, 214), outline=INK)

    # ---------- 장소 아이콘: 작은 판에 그린 뒤 2배로 키워 붙인다 (화면에서 읽히는 크기, 픽셀은 또렷하게) ----------
    g = [None]
    def house(x, y, s=1.0, roof=ROOF):
        w2, h = 7 * s, 8 * s
        g[0].rectangle((x - w2, y - h * .2, x + w2, y + h * .8), fill=WALL, outline=INK, width=2)
        g[0].polygon(((x - w2 - 3 * s, y - h * .2), (x, y - h * 1.1), (x + w2 + 3 * s, y - h * .2)), fill=roof, outline=INK)
        g[0].rectangle((x - 2 * s, y + h * .25, x + 2 * s, y + h * .8), fill=INK)

    def tree(x, y, s=1.0):
        g[0].line((x, y, x, y + 8 * s), fill=INK, width=3)
        g[0].ellipse((x - 9 * s, y - 10 * s, x + 9 * s, y + 4 * s), fill=GREEN, outline=INK, width=2)

    def icon(kind, x, y):
        if kind == "capital":
            for ox in (-22, 0, 22):
                house(x + ox, y + (6 if ox else 0), 1.15)
            g[0].rectangle((x + 30, y - 30, x + 38, y + 10), fill=(196, 92, 64), outline=INK, width=2)        # 종탑
            g[0].polygon(((x + 28, y - 30), (x + 34, y - 42), (x + 40, y - 30)), fill=(120, 140, 120), outline=INK)
            g[0].pieslice((x - 12, y - 30, x + 12, y - 8), 180, 360, fill=(214, 206, 186), outline=INK)      # 둥근 지붕
        elif kind == "orchard":
            house(x - 10, y + 4); tree(x + 12, y - 2); tree(x + 4, y + 12, .8); tree(x - 26, y + 8, .8)
        elif kind == "glass":
            house(x - 6, y + 4, 1.1, roof=(150, 78, 60))
            g[0].rectangle((x + 10, y - 22, x + 17, y + 6), fill=(150, 90, 70), outline=INK, width=2)        # 굴뚝
            for i, r in enumerate((5, 7, 9)):
                g[0].ellipse((x + 14 - r + i * 4, y - 30 - i * 8 - r, x + 14 + r + i * 4, y - 30 - i * 8 + r), fill=(214, 214, 206))
        elif kind == "fishing":
            for ox, roof in ((-14, (196, 80, 80)), (0, (84, 140, 196)), (14, (230, 180, 60))):
                house(x + ox, y, .8, roof=roof)
            g[0].polygon(((x - 16, y + 16), (x + 16, y + 16), (x + 10, y + 23), (x - 10, y + 23)), fill=(150, 98, 64), outline=INK)
        elif kind == "port":
            g[0].rectangle((x - 22, y - 8, x + 8, y + 8), fill=WALL, outline=INK, width=2)
            g[0].polygon(((x - 25, y - 8), (x - 7, y - 18), (x + 11, y - 8)), fill=ROOF, outline=INK)
            for i in range(3):
                g[0].rectangle((x + 12 + i * 7, y + 2, x + 17 + i * 7, y + 8), fill=(190, 140, 90), outline=INK)
            g[0].line((x + 22, y + 4, x + 22, y - 22), fill=INK, width=3)
            g[0].line((x + 22, y - 22, x + 34, y - 10), fill=INK, width=2)
        elif kind == "dock":
            house(x + 10, y - 2, 1.0)
            g[0].rectangle((x - 30, y + 6, x - 4, y + 11), fill=(150, 104, 66), outline=INK)
            g[0].polygon(((x - 36, y - 2), (x - 18, y - 2), (x - 22, y + 4), (x - 32, y + 4)), fill=(150, 98, 64), outline=INK)
        elif kind == "vine":
            house(x + 16, y - 4, .9)
            for i in range(4):
                yy = y - 10 + i * 7
                g[0].line((x - 26, yy, x + 2, yy), fill=GREEN_DARK, width=3)
                for j in range(5):
                    g[0].ellipse((x - 26 + j * 7 - 2, yy - 3, x - 26 + j * 7 + 2, yy + 1), fill=(120, 70, 120))
        elif kind == "marsh":
            for ox in (-14, -4, 6, 16):
                for k2 in (-1, 0, 1):
                    g[0].line((x + ox, y + 8, x + ox + k2 * 4, y - 8 + abs(k2) * 4), fill=GREEN_DARK, width=2)
            g[0].line((x - 24, y + 10, x + 24, y + 10), fill=(150, 104, 66), width=4)
        elif kind == "hills":
            for ox, s in ((-14, 1.0), (10, 1.3)):
                g[0].polygon(((x + ox - 16 * s, y + 10), (x + ox, y - 14 * s), (x + ox + 16 * s, y + 10)), fill=(196, 188, 168), outline=INK)
                g[0].polygon(((x + ox - 5 * s, y - 7 * s), (x + ox, y - 14 * s), (x + ox + 5 * s, y - 7 * s)), fill=(240, 236, 224))
        elif kind == "bridge":
            g[0].rectangle((x - 30, y - 4, x + 30, y + 4), fill=(214, 206, 184), outline=INK, width=2)
            for ox in (-15, 0, 15):
                g[0].pieslice((x + ox - 7, y - 2, x + ox + 7, y + 14), 180, 360, fill=LAKE, outline=INK)
        elif kind == "inn":
            g[0].rectangle((x - 26, y - 12, x + 26, y + 12), fill=(230, 210, 170), outline=INK, width=2)     # 마당
            house(x - 14, y - 12, 1.0)
            house(x + 12, y - 12, 1.0)
            g[0].ellipse((x - 4, y - 2, x + 4, y + 6), fill=LAKE, outline=INK)                               # 우물
            for ox, c in ((-20, (196, 80, 80)), (16, (84, 140, 196))):
                g[0].rectangle((x + ox, y + 2, x + ox + 8, y + 9), fill=c, outline=INK)                       # 가판대
        elif kind == "gate":
            for ox in (-16, 10):
                g[0].rectangle((x + ox, y - 22, x + ox + 8, y + 8), fill=(220, 208, 182), outline=INK, width=2)
                g[0].polygon(((x + ox - 2, y - 22), (x + ox + 4, y - 32), (x + ox + 10, y - 22)), fill=ROOF, outline=INK)
            g[0].rectangle((x - 8, y - 10, x + 10, y - 4), fill=(220, 208, 182), outline=INK)
            g[0].rectangle((x - 3, y - 18, x + 3, y - 10), fill=(60, 80, 150), outline=INK)                  # 깃발

    entries = []
    for mid, m in maps.items():
        hx, hy = hubs[mid]
        board = Image.new("RGBA", (140, 140), (0, 0, 0, 0))
        g[0] = ImageDraw.Draw(board)
        icon(ICON[mid], 70, 80)
        board = board.resize((280, 280), Image.NEAREST)
        im.paste(board, (int(hx * S) - 140, int(hy * S) - 160), board)
        entries.append(dict(mapId=mid, x=POS[mid][0], y=POS[mid][1], w=m.w, h=m.h, labelX=hx + .5, labelY=hy + 18))
    cx_, cy_ = (W - 14) * S, 14 * S                                                                      # 나침반
    d.ellipse((cx_ - 26, cy_ - 26, cx_ + 26, cy_ + 26), fill=(236, 226, 198), outline=INK, width=2)
    d.polygon(((cx_, cy_ - 22), (cx_ - 6, cy_), (cx_ + 6, cy_)), fill=(150, 90, 60))
    d.polygon(((cx_, cy_ + 22), (cx_ - 6, cy_), (cx_ + 6, cy_)), fill=(200, 184, 140))
    os.makedirs(OUT, exist_ok=True)
    png = os.path.join(OUT, "VeniceTerrain.png")
    im.save(png)
    gx_port, gy_port = POS["venice_c_vineyard"]
    layout = dict(width=W, height=H, maps=entries, annotations=[
        dict(text="베네치아 석호", x=14, y=10), dict(text="본토 서부 (중앙 서쪽 조각)", x=MAIN_X + 30, y=10),
        dict(text="↑ 북서 포구 (통합 후)", x=gx_port - 14, y=gy_port - 22),
        dict(text="→ 대륙 중심", x=W - 40, y=MAIN_Y + venice_mainland.CENTER_Y + 10)])
    with open(os.path.join(OUT, "VeniceLayout.json"), "w", encoding="utf-8", newline="\n") as fp:
        json.dump(layout, fp, ensure_ascii=False, indent=1)
    meta = png + ".meta"
    src = os.path.join(OUT, "GoryeoTerrain.png.meta")
    if not os.path.exists(meta) and os.path.exists(src):     # 처음만: 고려 지도와 같은 가져오기 설정, 새 GUID
        with open(src, encoding="utf-8") as fp:
            lines = ["guid: " + uuid.uuid4().hex if l.startswith("guid:") else l for l in fp.read().splitlines()]
        with open(meta, "w", encoding="utf-8", newline="\n") as fp:
            fp.write("\n".join(lines) + "\n")
    print("VeniceTerrain", im.size, "tiles", W, H)


if __name__ == "__main__":
    main()
