"""북유럽 교역도 (M 지도). 고려 지도(cartography.py)와 같은 종이 그림체로, 북유럽 4맵을 출구 기준으로 이어 붙인다.
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
    pad, scale = 10, 4
    minx = min(p[0] for p in positions.values()) - pad
    miny = min(p[1] for p in positions.values()) - pad
    width = max(positions[m.id][0] + m.w for m in maps) - minx + pad
    height = max(positions[m.id][1] + m.h for m in maps) - miny + pad
    im = Image.new("RGB", (width * scale, height * scale))
    rng = random.Random(30)
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            n = rng.choice((-2, -1, 0, 0, 1, 2))
            rim = min(x, y, im.width - 1 - x, im.height - 1 - y)
            shade = max(0, 8 - rim) * 2
            px[x, y] = (224 + n - shade, 210 + n - shade, 167 + n - shade)
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
                else:
                    d.rectangle(r, fill="#e6e0cf")            # 눈 덮인 땅: 종이보다 밝게
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
            if y > height * 0.62:                      # 아래쪽: 바다 물결
                d.arc((px_ - 6, py_ - 3, px_, py_ + 3), 200, 340, fill="#8fb0b6")
                d.arc((px_, py_ - 3, px_ + 6, py_ + 3), 200, 340, fill="#8fb0b6")
            elif r < 0.55:                              # 눈 덮인 산
                d.polygon(((px_ - 9, py_ + 6), (px_, py_ - 9), (px_ + 9, py_ + 6)), fill="#d8d0b8", outline="#a59a7c")
                d.polygon(((px_ - 3, py_ - 4), (px_, py_ - 9), (px_ + 3, py_ - 4)), fill="#f4f1e8")
            elif r < 0.85:                              # 숲
                for ox in (-4, 3):
                    d.polygon(((px_ + ox, py_ - 5), (px_ + ox - 3, py_ + 2), (px_ + ox + 3, py_ + 2)), fill="#8a9a78")
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
