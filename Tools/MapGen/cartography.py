"""맵 출구를 맞춰 배치한 지형 지도. 게임의 가격용 worldCoord와 독립적이다."""
import json
import random
from pathlib import Path
from PIL import Image, ImageDraw

from tiles import STAMPS


def layout(maps):
    if all(hasattr(m,'world_origin') for m in maps):
        return {m.id:m.world_origin for m in maps}
    by_id = {m.id: m for m in maps}
    positions = {maps[0].id: (0, 0)}
    queue = [maps[0].id]
    for mid in queue:
        m = by_id[mid]
        mx, my = positions[mid]
        for exit in (o for o in m.objects if o["kind"] == "exit"):
            if exit["target"] not in by_id:  # 지역 확대 지도에서는 외부 연결을 생략
                continue
            target = by_id[exit["target"]]
            spawn = next(o for o in target.objects if o["kind"] == "spawn" and o["id"] == exit["spawn"])
            edge = exit["edge"]
            if edge == "E":
                pos = (mx + m.w, my + exit["y"] - spawn["y"])
            elif edge == "W":
                pos = (mx - target.w, my + exit["y"] - spawn["y"])
            elif edge == "N":
                pos = (mx + exit["x"] - spawn["x"], my - target.h)
            else:
                pos = (mx + exit["x"] - spawn["x"], my + m.h)
            if target.id in positions:
                if positions[target.id] != pos:
                    raise ValueError(f"지형 지도 연결 위치 불일치: {mid} -> {target.id}")
            else:
                positions[target.id] = pos
                queue.append(target.id)
    if len(positions) != len(maps):
        raise ValueError("지형 지도에 연결되지 않은 맵이 있습니다")
    return positions


def export(maps, root, stem="Goryeo"):
    positions = layout(maps)
    pad, scale = 12, 4
    context = None  # Only exported, playable terrain is mapped.
    minx = min(p[0] for p in positions.values()) - pad
    miny = min(p[1] for p in positions.values()) - pad
    width = max(positions[m.id][0] + m.w for m in maps) - minx + pad
    height = max(positions[m.id][1] + m.h for m in maps) - miny + pad
    im = Image.new("RGB", (width * scale, height * scale))
    rng = random.Random(108)
    pixels = im.load()
    for y in range(im.height):
        for x in range(im.width):
            noise = rng.choice((-2, -1, 0, 0, 1, 2))
            # 가장자리는 낡은 종이처럼 조금 어둡게. 구역별 사각 경계는 그리지 않는다.
            rim = min(x, y, im.width - 1 - x, im.height - 1 - y)
            shade = max(0, 8 - rim) * 2
            pixels[x, y] = (224 + noise - shade, 210 + noise - shade, 167 + noise - shade)
    d = ImageDraw.Draw(im)
    entries = []
    water_mask = Image.new("L",im.size,0);water_draw=ImageDraw.Draw(water_mask)
    for m in maps:
        mx, my = positions[m.id]
        mx, my = mx - minx, my - miny
        def point(x, y):
            return (int((mx + x) * scale), int((my + y) * scale))
        # 지형과 실제 길/물의 실루엣. 장식 타일의 세부 색은 지도에 복사하지 않는다.
        for y in range(m.h):
            for x in range(m.w):
                px, py = point(x, y)
                rect = (px, py, px + scale - 1, py + scale - 1)
                wx=getattr(m,"world_origin",(0,0))[0]+x;dry=max(0,min(1,(-wx-120)/100))
                base=tuple(round(a+(b-a)*dry) for a,b in zip((202,208,163),(211,198,155)))
                d.rectangle(rect,fill=base)
                if m.water[y][x]:
                    d.rectangle(rect, fill="#82a8ab");water_draw.rectangle(rect,fill=255)
                elif (x, y) in m.decks:
                    d.rectangle(rect, fill="#8a6547")
                elif m.road[y][x]:
                    d.rectangle(rect, fill="#ba9461" if m.road[y][x] < 3 else "#aa7e4d")
                elif m.high[y][x] and m.high[y][x].startswith("nature:") and (not context or 3<x<m.w-4 and 3<y<m.h-4):
                    d.rectangle(rect, fill="#a7b184")
                elif m.obj[y][x] and m.obj[y][x].startswith("house:10:"):
                    d.rectangle(rect, fill="#766c56")
        for name, x, y in m.stamps:
            _, _, _, w, h, *_ = STAMPS[name]
            px, py = point(x + w / 2, y + h / 2)
            if name.startswith("goryeo_woodland") and int(name[15:])<=7 or name in ("pine", "tree", "cherry", "bigtree", "roundtree", "pinebig", "roundbig", "fruitbig", "blossom", "greenbig"):
                if context and (x<=2 or y<=2 or x+w>=m.w-2 or y+h>=m.h-2):continue
                for offset in ((-3, 1), (3, -1)) if w >= 3 else ((0, 0),):
                    tx, ty = px + offset[0], py + offset[1]
                    d.line((tx, ty, tx, ty + 4), fill="#776f4e")
                    d.polygon(((tx, ty - 5), (tx - 4, ty + 2), (tx + 4, ty + 2)), fill="#6f855f", outline="#627854")
            elif name.startswith("house") or name in ("shop", "store", "hall", "goryeo_royal", "goryeo_manor", "goryeo_stall", "goryeo_cottage", "goryeo_pavilion"):
                size = 5 if name != "hall" else 7
                d.rectangle((px - size, py - 2, px + size, py + 4), fill="#c5b084", outline="#887052")
                d.polygon(((px - size - 1, py - 2), (px, py - 6), (px + size + 1, py - 2)), fill="#687776")
            elif name == "module_rock":
                d.polygon(((px-3,py+2),(px-2,py-2),(px+2,py-3),(px+4,py+2)),fill="#8c907d",outline="#777d70")
            elif name == "wheat":
                for off in (-3, 0, 3):
                    d.line((px + off, py - 4, px + off, py + 3), fill="#b7a065")
            elif name == "boat":
                d.ellipse((px - 7, py - 3, px + 7, py + 3), fill="#9b7554", outline="#695842")
        market = next((o for o in m.objects if o["kind"] == "market"), None)
        label = market or getattr(m, "atlas_label", {"x": 22, "y": 19})
        entries.append(dict(mapId=m.id, x=mx, y=my, w=m.w, h=m.h,
                            labelX=mx + label["x"] + .5, labelY=my + label["y"] + .5))
    dest = Path(root) / "Assets/PocketCaravan/Resources/WorldMap"
    dest.mkdir(parents=True, exist_ok=True)
    im.save(dest / f"{stem}Terrain.png")
    water_mask.save(dest / f"{stem}WaterMask.png")
    data=dict(width=width,height=height,maps=entries,pixelsPerTile=scale,waterMask=f"{stem}WaterMask.png")
    if all(hasattr(m,'world_origin') for m in maps):
        data['geography']='continuous_tiles'
        data['terrainSource']='Tools/MapGen/broad_regions.py'
        data['scope']='goryeo_20000_and_central_east_25000' if stem=='Goryeo' else 'goryeo_20000'
        data['centralContinentTotalCells']=100000
        data['centralEastOwnedCells']=25000
        data['annotations']=[]
    (dest / f"{stem}Layout.json").write_text(json.dumps(data, ensure_ascii=False, indent=2))
