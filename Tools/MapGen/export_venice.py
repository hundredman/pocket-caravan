"""베네치아 맵 JSON만 내보낸다. 공용 export.py(atlasRegions)와 지형 지도는 건드리지 않는다.
    사용: python Tools/MapGen/export_venice.py
    - 맵: venice.json·center_venice.json의 jsonPath
    - 타일: 이미 있는 공용 타일 PNG는 덮어쓰지 않는다(.meta GUID 유지).
      없는 원본 타일은 원본 팩(NA_PACK)이 있을 때만 새로 잘라 내고, 없으면 목록을 알리고 멈춘다.
      베네치아 전용 그림(ven_*)은 venice_tiles.py가 그리며, 그림이 바뀐 경우에만 다시 쓴다.
    - 미리보기: Tools/MapGen/preview_<mapId>.png(게임처럼 표시 없음), _debug.png(스폰·출구·NPC 위치 점). 저장소에 올리지 않음.
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


def preview(m, path, markers=True):
    img = Image.new("RGBA", (m.w * 16, m.h * 16), (0, 0, 0, 255))
    cache = {}
    def draw(layer):
        for y in range(m.h):
            for x in range(m.w):
                k = layer[y][x]
                if k:
                    if k not in cache:
                        if os.path.exists(tile_png(k)):
                            cache[k] = Image.open(tile_png(k)).convert("RGBA")
                        else:   # 아직 내보내지 않은 타일 (배치 확인용 미리보기)
                            from venice_tiles import ven_image
                            from tiles import tile_image
                            cache[k] = (ven_image(k) if k.startswith(("ven:", "vq:")) else tile_image(k)).convert("RGBA")
                    img.alpha_composite(cache[k], (x * 16, y * 16))
    for layer in (m.ground, m.deco, m.obj):
        draw(layer)
    colors = {"spawn": (60, 140, 255, 200), "exit": (255, 140, 0, 160), "market": (255, 80, 80, 230),
              "board": (255, 230, 60, 230), "talker": (255, 255, 255, 230), "sign": (180, 120, 60, 230)}
    for o in (m.objects if markers else ()):
        c = colors.get(o["kind"])
        if c:
            img.alpha_composite(Image.new("RGBA", (10, 10), c), (o["x"] * 16 + 3, o["y"] * 16 + 3))
    draw(m.high)
    img.save(path)


REGIONS = ("venice", "center_venice")   # 베네치아 5씬 + 본토 서부 7씬 (venice_mainland.py)


def main():
    entries, maps = [], []
    for region in REGIONS:
        entries += map_entries(region)
        maps += generated_maps(region)
    used = set()
    for m in maps:
        for layer in (m.ground, m.deco, m.obj, m.high):
            used.update(k for row in layer for k in row if k)
        used.update(o["id"] for o in m.objects if o["kind"] == "growth")
    from venice_tiles import ven_image, GPT_OWN
    from tiles import tile_image
    for k in sorted(k for k in used if k.startswith(GPT_OWN)):
        try:
            image = ven_image(k) if k.startswith(("ven:", "vq:")) else tile_image(k)
        except OSError:          # 원본 팩 없음: 이미 내보낸 그림을 그대로 쓴다
            if os.path.exists(tile_png(k)):
                continue
            raise
        if os.path.exists(tile_png(k)):
            with Image.open(tile_png(k)) as old:
                if old.convert("RGBA").tobytes() == image.tobytes():
                    continue
        image.save(tile_png(k))
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
        if getattr(m, "obstacles", None):   # 공용 장애물 배치 (예: 습지 진흙 'mud'). MapJsonBuilder가 ObstacleField로 붙인다
            d["obstacles"] = m.obstacles
        if getattr(m, "districts", None):   # 베네치아 구역 이름 (VeniceRegionSetup이 씬에 붙인다. 공용 빌더는 무시)
            d["districts"] = [dict(name=n, x0=a, y0=b, x1=c, y1=e) for n, a, b, c, e in m.districts]
            d["defaultDistrict"] = m.default_district
        out = os.path.join(ROOT, entry["jsonPath"])
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with open(out, "w", encoding="utf-8") as fp:
            json.dump(d, fp, ensure_ascii=False, separators=(",", ":"))
        preview(m, os.path.join(HERE, "preview_" + m.id + ".png"), markers=False)
        preview(m, os.path.join(HERE, "preview_" + m.id + "_debug.png"))
        print(m.id, m.w, m.h)
    print(len(used), "tiles used")
    return 0


if __name__ == "__main__":
    sys.exit(main())
