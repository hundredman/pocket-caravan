"""베네치아·본토 서부 M 지도 (2026-10-29): 고려 지도(Tools/WorldLayout/m4_export.py)와 같은 그림체.
    실제 맵 JSON의 타일 그림(Ground·Deco·Objects·Above)을 칸당 4px로 줄여(BOX) 한 장으로 붙인다.
    짜임은 북유럽 교역도와 같다: 고향(석호 섬들)은 바다 위, 본토 서부는 대륙 가운데를 꼭짓점으로 하는 피자 조각(점선 경계),
    조각 밖 이웃 땅은 흐린 종이색과 숲 기호. 맵 자리는 출구가 맞닿게 풀어(최소제곱) 무리별로 옮긴다(실제와 조금 달라도 된다).
    사용: python Tools/MapGen/venice_tilemap.py   (맵 JSON을 다시 만든 뒤 실행)
    출력: Assets/PocketCaravan/Resources/WorldMap/VeniceTerrain.png, VeniceLayout.json (VeniceMapUI가 읽는다)
    배치는 보기 위한 축약이며 가격용 worldCoord·실제 이동과 무관하다. 예전 종이 그림 지도는 venice_worldmap.py에 남아 있다."""
import glob, json, os
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MAPS = os.path.join(ROOT, "Assets/PocketCaravan/Maps")
TILES = os.path.join(ROOT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")
OUT = os.path.join(ROOT, "Assets/PocketCaravan/Resources/WorldMap")
T, PPT = 16, 4
PAPER = (225, 211, 171)
ORDER = ["venice_rialto", "venice_santerasmo", "venice_burano", "venice_murano", "venice_chioggia",
         "venice_c_landing", "venice_c_vineyard", "venice_c_marsh", "venice_c_pass", "venice_c_post",
         "venice_c_bridge", "venice_c_gate"]
SEA_GAP = {("venice_chioggia", "venice_c_landing"): 6}     # 키오자 ↔ 본토 나루는 뱃길이라 바다를 조금 둔다


def load():
    maps = {}
    for f in glob.glob(os.path.join(MAPS, "Venice*", "*.json")):
        with open(f, encoding="utf-8") as fp:
            d = json.load(fp)
        if d["id"] in ORDER:
            maps[d["id"]] = d
    return maps


def exits(d):
    for o in d["objects"]:
        if o.get("type") == "exit" or (o.get("target") and o.get("edge")):
            yield o


def solve(maps):
    """맞닿은 출구끼리 자리를 맞춘다: 변수 = 맵마다 (x, y), 리알토 고정."""
    ids = ORDER
    rows, rhs = [], []
    for a in ids:
        A = maps[a]
        for o in exits(A):
            b = o["target"]
            if b not in maps or ids.index(b) < ids.index(a):
                continue
            B = maps[b]
            back = [e for e in exits(B) if e["target"] == a]
            if not back:
                continue
            e = min(back, key=lambda e: abs(e["y"] - o["y"]) + abs(e["x"] - o["x"]))
            gap = SEA_GAP.get((a, b), SEA_GAP.get((b, a), 0))
            ax, ay = o["x"] + o["w"] / 2, o["y"] + o["h"] / 2
            bx, by = e["x"] + e["w"] / 2, e["y"] + e["h"] / 2
            edge = o["edge"]
            if edge == "E":
                dx, dy = A["w"] + gap, ay - by
            elif edge == "W":
                dx, dy = -B["w"] - gap, ay - by
            elif edge == "N":
                dx, dy = ax - bx, -B["h"] - gap
            else:
                dx, dy = ax - bx, A["h"] + gap
            for axis, delta in ((0, dx), (1, dy)):
                r = np.zeros(2 * len(ids))
                r[2 * ids.index(b) + axis] = 1
                r[2 * ids.index(a) + axis] = -1
                rows.append(r)
                rhs.append(delta)
    for axis in (0, 1):                                     # 리알토 고정
        r = np.zeros(2 * len(ids)); r[axis] = 1
        rows.append(r * 100); rhs.append(0)
    sol = np.linalg.lstsq(np.array(rows), np.array(rhs), rcond=None)[0]
    pos = {m: [int(round(sol[2 * i])), int(round(sol[2 * i + 1]))] for i, m in enumerate(ids)}
    # 겹치면 늦게 놓인 맵을 겹친 만큼 밀어낸다 (작은 오차 정리)
    for _ in range(50):
        moved = False
        for i, a in enumerate(ids):
            for b in ids[i + 1:]:
                ax0, ay0 = pos[a]; bx0, by0 = pos[b]
                ox = min(ax0 + maps[a]["w"], bx0 + maps[b]["w"]) - max(ax0, bx0)
                oy = min(ay0 + maps[a]["h"], by0 + maps[b]["h"]) - max(ay0, by0)
                if ox > 0 and oy > 0:
                    if ox <= oy:
                        pos[b][0] += ox if bx0 >= ax0 else -ox
                    else:
                        pos[b][1] += oy if by0 >= ay0 else -oy
                    moved = True
        if not moved:
            break
    return pos


tile_cache = {}


def tile(name):
    if name not in tile_cache:
        p = os.path.join(TILES, name + ".png")
        tile_cache[name] = Image.open(p).convert("RGBA") if os.path.exists(p) else None
    return tile_cache[name]


def render(d):
    im = Image.new("RGBA", (d["w"] * T, d["h"] * T), (0, 0, 0, 0))
    pal = d["palette"]
    for layer in ("ground", "deco", "obj", "high"):
        cells = d[layer]
        for i, v in enumerate(cells):
            if not v:
                continue
            t = tile(pal[v])
            if t is None:
                continue
            x, y = i % d["w"], i // d["w"]
            if t.size != (T, T):                        # 큰 그림은 칸 왼쪽 아래 기준으로
                im.alpha_composite(t, (x * T, (y + 1) * T - t.size[1]))
            else:
                im.alpha_composite(t, (x * T, y * T))
    return im


def main():
    maps = load()
    pos = solve(maps)
    lag = [m for m in ORDER if not m.startswith("venice_c_")]
    main_ = [m for m in ORDER if m.startswith("venice_c_")]

    def box(ms):
        return (min(pos[m][0] for m in ms), min(pos[m][1] for m in ms),
                max(pos[m][0] + maps[m]["w"] for m in ms), max(pos[m][1] + maps[m]["h"] for m in ms))
    # 북유럽 교역도와 같은 짜임: 고향(석호)은 바다 위, 본토 서부는 대륙 가운데를 꼭짓점으로 하는 피자 조각.
    # 실제 출구 자리와 조금 달라도 된다(보기 위한 축약). 석호 무리와 본토 무리를 각각 통째로 옮긴다.
    lx0, ly0, lx1, ly1 = box(lag)
    # 본토는 조각 모양에 맞게 세 줄로 다시 쌓는다: 해안 줄(포도밭·나루·습지) → 가운데 줄(고개·숙소·돌다리) → 꼭짓점 쪽(동문)
    COLS = [["venice_c_vineyard", "venice_c_landing", "venice_c_marsh"],
            ["venice_c_pass", "venice_c_post", "venice_c_bridge"],
            ["venice_c_gate"]]
    half = 120                                                              # 해안에서 조각 반폭(칸)
    H = int(2 * half + 12)
    cy = H / 2
    for m in lag:
        pos[m] = [pos[m][0] - lx0 + 12, int(pos[m][1] - (ly0 + ly1) / 2 + cy)]
    coast = 12 + (lx1 - lx0) + 9                                            # 해안선(칸)
    x = coast + 1
    for col in COLS:
        total = sum(maps[m]["h"] for m in col) + 2 * (len(col) - 1)
        y = int(cy - total / 2)
        for m in col:
            pos[m] = [x, y]
            y += maps[m]["h"] + 2
        x += max(maps[m]["w"] for m in col) + 2
    # 꼭짓점: 본토 맵 모서리가 모두 조각 안에 들도록 가장 가까운 x
    apex = coast + 40
    for m in main_:
        x, y = pos[m]
        for cx, cy_ in ((x + maps[m]["w"], y), (x + maps[m]["w"], y + maps[m]["h"]), (x, y), (x, y + maps[m]["h"])):
            dy = abs(cy_ - cy) + 3
            if dy < half:
                apex = max(apex, (cx * half - coast * dy) / (half - dy))
    apex = int(apex) + 2
    W = apex + 14

    # 맵 그림만 투명 바탕에 붙여 칸당 4px로 줄인다
    big = Image.new("RGBA", (W * T, H * T), (0, 0, 0, 0))
    for m, (x, y) in pos.items():
        big.alpha_composite(render(maps[m]), (x * T, y * T))
    tiles_small = big.resize((W * PPT, H * PPT), Image.BOX)
    sw, sh = W * PPT, H * PPT
    # 바탕: 바다 / 조각 안 풀 / 조각 밖 이웃 땅(흐린 종이색 + 숲 기호)
    sea4 = np.asarray(tile("vq_W_sea_0_--------").convert("RGB").resize((PPT, PPT), Image.BOX))
    grass4 = np.asarray(tile(maps["venice_c_post"]["palette"][1]).convert("RGB").resize((PPT, PPT), Image.BOX))
    yy, xx = np.mgrid[0:sh, 0:sw]
    cell_y = yy / PPT
    shore = (coast + 2.2 * np.sin(cell_y / 7.0) + 1.1 * np.sin(cell_y / 2.9)) * PPT
    land = xx >= shore
    t = np.clip((xx / PPT - coast) / (apex - coast), 0, 1)
    inside = land & (np.abs(cell_y - cy) <= half * (1 - t)) & (xx / PPT <= apex)
    px = np.zeros((sh, sw, 3), np.uint8)
    px[:] = np.tile(sea4, (sh // PPT + 1, sw // PPT + 1, 1))[:sh, :sw]
    gr = np.tile(grass4, (sh // PPT + 1, sw // PPT + 1, 1))[:sh, :sw]
    px[land] = gr[land]
    neigh = land & ~inside
    px[neigh] = (gr[neigh] * .25 + np.array((216, 210, 194)) * .75).astype(np.uint8)
    shallow = ~land & (xx >= shore - 2 * PPT)                               # 해안 얕은 물
    px[shallow] = np.clip(px[shallow].astype(int) + 26, 0, 255)
    img = Image.fromarray(px)
    base = img.convert("RGBA")
    base.alpha_composite(tiles_small)
    img = base.convert("RGB")
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(5)
    for gy in range(6, H - 4, 9):                                            # 이웃 조각 숲 기호 (북유럽 지도와 같은 뜻)
        for gx in range(coast + 4, W - 2, 11):
            x = (gx + (5 if (gy // 9) % 2 else 0) + rng.integers(-2, 3)) * PPT
            y = (gy + rng.integers(-2, 3)) * PPT
            if 0 <= x < sw and 0 <= y < sh and neigh[y, x] and not tiles_small.getpixel((x, y))[3]:
                for ox in (-5, 5):
                    d.polygon(((x + ox, y - 7), (x + ox - 5, y + 3), (x + ox + 5, y + 3)), fill=(170, 176, 140), outline=(140, 148, 112))
    for sign in (-1, 1):                                                     # 조각 경계 점선
        p0 = np.array((coast * PPT, (cy + sign * half) * PPT)); p1 = np.array((apex * PPT, cy * PPT))
        n = int(np.linalg.norm(p1 - p0) / 14)
        for i in range(0, n, 2):
            a_, b_ = p0 + (p1 - p0) * i / n, p0 + (p1 - p0) * (i + 1) / n
            d.line((tuple(a_), tuple(b_)), fill=(150, 104, 66), width=5)
    small = img
    os.makedirs(OUT, exist_ok=True)
    small.save(os.path.join(OUT, "VeniceTerrain.png"))
    notes = [dict(text="베네치아 석호", x=12, y=H - 16),
             dict(text="본토 서부 (중앙 서쪽 조각)", x=coast + 4, y=2),
             dict(text="대륙 중심 →", x=apex - 46, y=cy + 8)]

    entries = []
    for m in ORDER:
        d = maps[m]; x, y = pos[m]
        market = next((o for o in d["objects"] if o.get("kind") == "market"), None)
        if market:
            lx, ly = market["x"] + .5, market["y"] + .5
        else:
            lx, ly = d["w"] / 2, d["h"] / 2
        entries.append(dict(mapId=m, x=x, y=y, w=d["w"], h=d["h"], labelX=round(x + lx, 1), labelY=round(y + ly, 1)))
    layout = dict(width=W, height=H, maps=entries, pixelsPerTile=PPT, geography="continuous_tiles",
                  terrainSource="Tools/MapGen/venice_tilemap.py", annotations=notes)
    with open(os.path.join(OUT, "VeniceLayout.json"), "w", encoding="utf-8", newline="\n") as fp:
        json.dump(layout, fp, ensure_ascii=False, indent=1)
    print("VeniceTerrain", small.size, "cells", W, H)
    for m in ORDER:
        print(" ", m, pos[m])


if __name__ == "__main__":
    main()
