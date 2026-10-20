"""베네치아 맵 JSON만 내보낸다. 공용 export.py(atlasRegions)와 지형 지도는 건드리지 않는다.
    사용: python Tools/MapGen/export_venice.py
    - 맵: venice.json의 jsonPath
    - 타일: 이미 있는 공용 타일 PNG만 쓴다. 덮어쓰지 않는다(.meta GUID 유지).
      없는 타일이 있으면 원본 팩(NA_PACK)이 있을 때만 새로 잘라 내고, 없으면 목록을 알리고 멈춘다.
    - 미리보기: Tools/MapGen/preview_<mapId>.png (저장소에 올리지 않음). 공용 타일 PNG로 그린다.
    다음: Unity 메뉴 Pocket Caravan > 베네치아 > 데이터·맵 만들기"""
import json, os, sys
from PIL import Image
from world_registry import ROOT, map_entries, generated_maps

HERE = os.path.dirname(os.path.abspath(__file__))
TILE_DIR = os.path.join(ROOT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")
DEFAULTS = dict(w=1, h=1, id="", npc="", town="", target="", spawn="", edge="", text="", name="", character="",
                lines=[], variant=0, idx=0, sheet="", frames=0, fps=0.0, radius=0.0, type="", path=[])


def name_of(key):
    return key.replace(":", "_").replace("@", "_")


def tile_png(key):
    return os.path.join(TILE_DIR, name_of(key) + ".png")


def preview(m, path):
    img = Image.new("RGBA", (m.w * 16, m.h * 16), (0, 0, 0, 255))
    cache = {}
    def draw(layer):
        for y in range(m.h):
            for x in range(m.w):
                k = layer[y][x]
                if k:
                    if k not in cache:
                        cache[k] = Image.open(tile_png(k)).convert("RGBA")
                    img.alpha_composite(cache[k], (x * 16, y * 16))
    for layer in (m.ground, m.deco, m.obj):
        draw(layer)
    colors = {"spawn": (60, 140, 255, 200), "exit": (255, 140, 0, 160), "market": (255, 80, 80, 230),
              "board": (255, 230, 60, 230), "talker": (255, 255, 255, 230), "sign": (180, 120, 60, 230)}
    for o in m.objects:
        c = colors.get(o["kind"])
        if c:
            img.alpha_composite(Image.new("RGBA", (10, 10), c), (o["x"] * 16 + 3, o["y"] * 16 + 3))
    draw(m.high)
    img.save(path)


def main():
    entries = map_entries("venice")
    maps = generated_maps("venice")
    used = set()
    for m in maps:
        for layer in (m.ground, m.deco, m.obj, m.high):
            used.update(k for row in layer for k in row if k)
        used.update(o["id"] for o in m.objects if o["kind"] == "growth")
    missing = sorted(k for k in used if not os.path.exists(tile_png(k)))
    if missing:
        try:
            from tiles import tile_image
            for k in missing:
                tile_image(k).save(tile_png(k))
            print(len(missing), "new tiles from asset pack")
        except OSError:
            print("공용 타일 PNG에 없는 타일 (원본 팩 없음):", ", ".join(name_of(k) for k in missing))
            return 1
    for entry, m in zip(entries, maps):
        palette, index = [""], {"": 0}
        def flat(layer):
            out = []
            for row in layer:
                for k in row:
                    n = name_of(k) if k else ""
                    if n not in index:
                        index[n] = len(palette); palette.append(n)
                    out.append(index[n])
            return out
        objects = []
        for o in m.objects:
            d = dict(DEFAULTS); d.update(o)
            d["path"] = [v for pt in o.get("path", []) for v in pt]
            if o["kind"] == "growth":
                d["id"] = name_of(o["id"])
            objects.append(d)
        d = dict(id=m.id, name=m.name, w=m.w, h=m.h, hours=m.hours,
                 ground=flat(m.ground), deco=flat(m.deco), obj=flat(m.obj), high=flat(m.high),
                 road=[g for row in m.road for g in row],
                 blocked=[1 if b else 0 for row in m.blocked for b in row],
                 objects=objects)
        d["palette"] = palette
        out = os.path.join(ROOT, entry["jsonPath"])
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with open(out, "w", encoding="utf-8") as fp:
            json.dump(d, fp, ensure_ascii=False, separators=(",", ":"))
        preview(m, os.path.join(HERE, "preview_" + m.id + ".png"))
        print(m.id, m.w, m.h)
    print(len(used), "tiles used")
    return 0


if __name__ == "__main__":
    sys.exit(main())
