"""북유럽 교역도 (M 지도). 고려 지도(cartography.py)와 같은 종이 그림체로, 북유럽 맵을 출구 기준으로 이어 붙인다.
10/24: 네모 맵들이 따로 붙은 느낌 대신 둥근 땅덩이 하나로 감싸고 바깥은 바다. 중앙 북쪽 구역도 북유럽식(눈 땅)으로 같이 그린다.
공용 cartography.py·고려 지도 파일은 건드리지 않고 Resources/WorldMap/NorthTerrain.png, NorthLayout.json만 만든다."""
import json
import random
import uuid
from pathlib import Path
from PIL import Image, ImageDraw
from cartography import layout
from tiles import STAMPS

TREES = ("snowpine", "snowpine_g", "snowround", "snowpinebig", "snowpinebig_g", "snowroundbig")
BARE = ("deadtree", "deadtreebig")
ROCKS = ("rock", "rockbig", "snowboulder")
HOUSES = ("longhall", "woodhouse", "house3", "h_dark", "h_shed", "h_turf", "h_round", "h_dome", "h_stoneA", "h_stoneB", "ruinhall", "ruinhut", "tent")


def export(maps, root):
    positions = layout(maps)
    pad, scale = 34, 4      # 둥근 땅덩이와 바다가 들어갈 여백
    minx = min(p[0] for p in positions.values()) - pad
    miny = min(p[1] for p in positions.values()) - pad
    width = max(positions[m.id][0] + m.w for m in maps) - minx + pad
    height = max(positions[m.id][1] + m.h for m in maps) - miny + pad
    # 10/24: 맵마다 네모로 붙은 느낌 대신, 모든 맵을 감싸는 둥근 땅덩이 하나 (바깥은 바다)
    from PIL import ImageFilter
    land_small = Image.new("L", (width, height), 0)
    ld = ImageDraw.Draw(land_small)
    for m in maps:
        if m.id.startswith("north_c_"):
            continue                        # 10/26 중앙 대륙은 따로 (아래 피자 조각)
        x0, y0 = positions[m.id][0] - minx, positions[m.id][1] - miny
        ld.rectangle((x0, y0, x0 + m.w - 1, y0 + m.h - 1), fill=255)
    blur = land_small.filter(ImageFilter.GaussianBlur(20))
    land_small = blur.point(lambda v: 255 if v > 26 else 0)
    # 10/26 중앙 대륙: 북문 마을 윗변(국경 고개 아래)부터 남쪽은 모두 대륙 땅. 그 안에서 피자 조각(북쪽 1/4)만 따뜻한 색
    gpos = positions.get("north_c_gatetown")
    cont = None
    if gpos:
        gx0, gy0 = gpos[0] - minx, gpos[1] - miny          # 북문 마을 왼쪽 위 (작은 지도 칸)
        def wedge(gx, gy):                                 # 대각선 경계 (north_center.wedge_forest와 같은 식)
            return -62 + 0.32 * gy <= gx <= 122 - 0.32 * gy
        def coast_y(gx):                                   # 대륙 북쪽 해안: 가운데가 조금 솟고 모서리로 갈수록 내려감.
            base = -3 + 0.0009 * (gx - 30) ** 2            # 피오르 나루(북유럽 남동쪽 끝)와 대륙 사이엔 바다가 지나게, 조각 동쪽 바깥은 남쪽으로 물러남
            if gx > 124:
                base = max(base, min(52, (gx - 124) * 2.6))
            return base
        ld = ImageDraw.Draw(land_small)
        cont = Image.new("L", (width, height), 0)
        cd_ = cont.load()
        for yy in range(height):
            for xx in range(width):
                gx, gy = xx - gx0, yy - gy0
                if gy >= coast_y(gx):
                    cd_[xx, yy] = 255 if wedge(gx, gy) else 128
        north_land = land_small.copy()                    # 북유럽 땅 (대륙보다 먼저 칠함)
        nl = north_land.load()
        for yy in range(height):
            for xx in range(width):
                if nl[xx, yy] and cd_[xx, yy] == 128:
                    cd_[xx, yy] = 0
        land_small.paste(255, mask=cont.point(lambda v: 255 if v else 0))
    for m in maps:   # 맵 칸은 반드시 땅
        x0, y0 = positions[m.id][0] - minx, positions[m.id][1] - miny
        ImageDraw.Draw(land_small).rectangle((x0, y0, x0 + m.w - 1, y0 + m.h - 1), fill=255)
    land = land_small.resize((width * scale, height * scale), Image.BILINEAR).filter(ImageFilter.GaussianBlur(3)).point(lambda v: 255 if v > 127 else 0)
    coast = land.filter(ImageFilter.MaxFilter(9))
    im = Image.new("RGB", (width * scale, height * scale))
    rng = random.Random(30)
    px = im.load(); lp = land.load(); cp = coast.load()
    contp = cont.load() if cont is not None else None
    for y in range(im.height):
        for x in range(im.width):
            n = rng.choice((-2, -1, 0, 0, 1, 2))
            c = contp[x // scale, y // scale] if contp is not None else 0
            if lp[x, y] and c == 255:
                gy = y / scale - gy0
                t = max(0.0, min(1.0, gy / 160))                   # 남쪽으로 갈수록 눈이 녹아 따뜻한 색
                px[x, y] = (int(232 - 10 * t) + n, int(222 - 14 * t) + n, int(200 - 40 * t) + n)   # 중앙 대륙 북쪽 조각
            elif lp[x, y] and c == 128:
                px[x, y] = (214 + n, 208 + n, 192 + n)          # 이웃 조각 (다른 사람 담당)
            elif lp[x, y]:
                px[x, y] = (230 + n, 224 + n, 207 + n)          # 눈 덮인 땅
            elif cp[x, y]:
                px[x, y] = (170 + n, 196 + n, 198 + n)          # 얕은 바다 (해안 띠)
            else:
                rim = min(x, y, im.width - 1 - x, im.height - 1 - y)
                shade = max(0, 8 - rim) * 2
                px[x, y] = (196 + n - shade, 208 + n - shade, 200 + n - shade)   # 먼 바다
    edge = land.filter(ImageFilter.FIND_EDGES)
    ep = edge.load()
    for y in range(im.height):
        for x in range(im.width):
            if ep[x, y]:
                px[x, y] = (122, 104, 78)                         # 해안선
    self_land = land_small.load()
    d = ImageDraw.Draw(im)
    entries = []
    for m in maps:
        mx, my = positions[m.id][0] - minx, positions[m.id][1] - miny

        def pt(x, y):
            return int((mx + x) * scale), int((my + y) * scale)
        for y in range(m.h):
            for x in range(m.w):
                a, b = pt(x, y)
                r = (a, b, a + scale - 1, b + scale - 1)
                g = m.ground[y][x] or ""
                if m.water[y][x]:
                    d.rectangle(r, fill="#9fbfc6")            # 얼어붙은 물: 고려보다 옅은 청회색
                elif (x, y) in m.decks:
                    d.rectangle(r, fill="#8a6547")
                elif g.startswith("relief:"):
                    _, sx, sy = g.split(":")[:3]
                    face = int(sx) >= 4 or int(sy) == 2          # 절벽면은 회녹색, 윗면은 눈
                    d.rectangle(r, fill="#8f9a86" if face else "#efeadc")
                elif m.road[y][x]:
                    d.rectangle(r, fill="#ba9461")
        for name, x, y in m.stamps:
            w, h = STAMPS[name][3], STAMPS[name][4]
            cx, cy = pt(x + w / 2, y + h / 2)
            if name in TREES:
                for ox, oy in (((-3, 1), (3, -1)) if w >= 3 else ((0, 0),)):
                    tx, ty = cx + ox, cy + oy
                    d.line((tx, ty, tx, ty + 4), fill="#776f4e")
                    d.polygon(((tx, ty - 5), (tx - 4, ty + 2), (tx + 4, ty + 2)), fill="#6f855f", outline="#627854")
                    d.line((tx - 1, ty - 4, tx + 1, ty - 4), fill="#f4f1e8")   # 눈 얹힌 끝
            elif name in BARE:
                d.line((cx, cy - 4, cx, cy + 4), fill="#7a5d44")
                d.line((cx, cy - 1, cx - 3, cy - 4), fill="#7a5d44"); d.line((cx, cy, cx + 3, cy - 3), fill="#7a5d44")
            elif name in ROCKS:
                d.ellipse((cx - 3, cy - 2, cx + 3, cy + 2), fill="#9a9a8a", outline="#77776a")
            elif name in HOUSES:
                s = 7 if name in ("longhall", "ruinhall") else 4 if name in ("tent", "h_stoneA", "h_stoneB", "h_turf") else 5
                d.rectangle((cx - s, cy - 2, cx + s, cy + 4), fill="#c5b084", outline="#887052")
                d.polygon(((cx - s - 1, cy - 2), (cx, cy - 6), (cx + s + 1, cy - 2)), fill="#8c5a3c")
            elif name == "boat":
                d.ellipse((cx - 7, cy - 3, cx + 7, cy + 3), fill="#9b7554", outline="#695842")
        market = next((o for o in m.objects if o["kind"] == "market"), None)
        label = market or {"x": m.w // 2, "y": m.h // 2 - 2}
        entries.append(dict(mapId=m.id, x=mx, y=my, w=m.w, h=m.h, labelX=mx + label["x"] + .5, labelY=my + label["y"] + .5))
    # 빈 종이: 아직 안 만든 땅이라는 걸 해치지 않는 선에서 산맥·숲·물결 기호로 북방 느낌을 낸다 (실제 칸은 아님)
    covered = set()
    for m in maps:
        mx, my = positions[m.id][0] - minx, positions[m.id][1] - miny
        for y in range(my - 3, my + m.h + 3):
            for x in range(mx - 3, mx + m.w + 3):
                covered.add((x, y))
    lowest_water = max(positions[m.id][1] - miny + m.h for m in maps)
    for gy in range(4, height - 4, 7):
        for gx in range(4, width - 4, 9):
            x, y = gx + rng.randint(-2, 2), gy + rng.randint(-2, 2)
            if any((x + a, y + b) in covered for a in (-3, 0, 3) for b in (-3, 0, 3)):
                continue
            px_, py_ = x * scale, y * scale
            r = rng.random()
            if not (0 <= x < width and 0 <= y < height):
                continue
            if not self_land[x, y]:                    # 바다: 물결
                d.arc((px_ - 6, py_ - 3, px_, py_ + 3), 200, 340, fill="#8fb0b6")
                d.arc((px_, py_ - 3, px_ + 6, py_ + 3), 200, 340, fill="#8fb0b6")
            elif cont is not None and cont.getpixel((x, y)) and r < 0.75:   # 중앙 대륙: 들판 숲 기호
                for ox in (-4, 3):
                    d.polygon(((px_ + ox, py_ - 5), (px_ + ox - 3, py_ + 2), (px_ + ox + 3, py_ + 2)), fill="#9aa47e")
            elif cont is not None and cont.getpixel((x, y)):
                continue
            elif r < 0.55:                              # 눈 덮인 산
                d.polygon(((px_ - 9, py_ + 6), (px_, py_ - 9), (px_ + 9, py_ + 6)), fill="#d8d0b8", outline="#a59a7c")
                d.polygon(((px_ - 3, py_ - 4), (px_, py_ - 9), (px_ + 3, py_ - 4)), fill="#f4f1e8")
            elif r < 0.85:                              # 숲
                for ox in (-4, 3):
                    d.polygon(((px_ + ox, py_ - 5), (px_ + ox - 3, py_ + 2), (px_ + ox + 3, py_ + 2)), fill="#8a9a78")
    # 10/26 조각 안 맵 사이 빈 땅(걸어 들어갈 수 없는 숲)은 빽빽한 숲 기호로 채운다
    if gpos:
        inner = set()
        for m in maps:
            mx, my = positions[m.id][0] - minx, positions[m.id][1] - miny
            for y in range(my - 1, my + m.h + 1):
                for x in range(mx - 1, mx + m.w + 1):
                    inner.add((x, y))
        for yy in range(0, height, 4):
            for xx in range((yy // 4) % 2 * 2, width, 4):
                gx, gy = xx - gx0, yy - gy0
                if gy < 2 or (xx, yy) in inner or not (-62 + 0.32 * gy + 2 <= gx <= 122 - 0.32 * gy - 2):
                    continue
                px_, py_ = xx * scale, yy * scale
                d.polygon(((px_, py_ - 6), (px_ - 4, py_ + 3), (px_ + 4, py_ + 3)), fill="#7f9568", outline="#6b7f57")
    # 10/26 피자 조각 경계: 대각선 점선 두 줄 (이웃 조각과의 경계), 북유럽과의 국경 점선
    if gpos:
        def P(gx, gy):
            return (gx + gx0) * scale, (gy + gy0) * scale
        for sgn in (-1, 1):
            for i in range(0, 400, 6):
                gy0_, gy1_ = i, i + 3
                xa = -62 + 0.32 * gy0_ if sgn < 0 else 122 - 0.32 * gy0_
                xb = -62 + 0.32 * gy1_ if sgn < 0 else 122 - 0.32 * gy1_
                a, b = P(xa, gy0_ - 4), P(xb, gy1_ - 4)
                if 0 <= b[1] < im.height:
                    d.line((a, b), fill="#8c5a3c", width=3)
        for gx in range(-60, 122, 6):                      # 국경 고개·비르카와 대륙 사이 국경
            d.line((P(gx, 0), P(gx + 3, 0)), fill="#8c5a3c", width=2)
    # 나침반 (오른쪽 위)
    cx_, cy_ = (width - 9) * scale, 9 * scale
    d.polygon(((cx_, cy_ - 16), (cx_ - 4, cy_), (cx_ + 4, cy_)), fill="#8c5a3c")
    d.polygon(((cx_, cy_ + 16), (cx_ - 4, cy_), (cx_ + 4, cy_)), fill="#c5b084")
    d.line((cx_ - 14, cy_, cx_ + 14, cy_), fill="#887052")
    d.text((cx_ - 3, cy_ - 30), "N", fill="#5a4630")
    dest = Path(root) / "Assets/PocketCaravan/Resources/WorldMap"
    dest.mkdir(parents=True, exist_ok=True)
    png = dest / "NorthTerrain.png"
    im.save(png)
    (dest / "NorthLayout.json").write_text(json.dumps(dict(width=width, height=height, maps=entries), ensure_ascii=False, indent=2))
    # 처음 만들 때만 고려 지도와 같은 가져오기 설정(.meta: 읽기 가능·점 필터·무압축)을 새 GUID로 복사
    meta = png.with_suffix(".png.meta")
    src = dest / "GoryeoTerrain.png.meta"
    if not meta.exists() and src.exists():
        lines = src.read_text().splitlines()
        lines = ["guid: " + uuid.uuid4().hex if l.startswith("guid:") else l for l in lines]
        meta.write_text("\n".join(lines) + "\n")
    return width, height
