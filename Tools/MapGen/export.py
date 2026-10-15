"""맵 JSON과 사용한 타일 PNG를 Unity 프로젝트 구조로 내보낸다."""
import json, os, sys, shutil
import maps, gaegyeong
import cartography
from tiles import tile_image
from PIL import Image

# 사용법: python3 export.py <저장소 루트>  (기본: 이 파일 기준 저장소 루트)
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..")
tile_dir = os.path.join(OUT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")
map_dir = os.path.join(OUT, "Assets/PocketCaravan/Maps")
os.makedirs(tile_dir, exist_ok=True); os.makedirs(map_dir, exist_ok=True)

def name_of(key):
    return key.replace(":", "_").replace("@", "_")

DEFAULTS = dict(w=1, h=1, id="", npc="", town="", target="", spawn="", edge="", text="", name="", character="",
                lines=[], variant=0, idx=0, sheet="", frames=0, fps=0.0, radius=0.0, type="", path=[])


def obj_json(o):
    d = dict(DEFAULTS)
    d.update(o)
    d["path"] = [v for pt in o.get("path", []) for v in pt]   # [x0, y0, x1, y1, ...]
    if o["kind"] == "growth":   # 타일 이름으로 바꾸고 그림도 내보낸다
        used.add(o["id"])
        d["id"] = name_of(o["id"])
    return d


used = set()
world_maps = []
for f in (maps.naru, maps.route01, gaegyeong.build, maps.dakmok):
    m = f()
    world_maps.append(m)
    palette = [""]
    index = {"": 0}
    def flat(layer):
        out = []
        for row in layer:          # 위쪽 줄부터 (Unity에서 y를 뒤집음)
            for k in row:
                k = k or ""
                if k:
                    used.add(k)
                n = name_of(k) if k else ""
                if n not in index:
                    index[n] = len(palette); palette.append(n)
                out.append(index[n])
        return out
    d = dict(id=m.id, name=m.name, w=m.w, h=m.h, hours=m.hours,
             ground=flat(m.ground), deco=flat(m.deco), obj=flat(m.obj), high=flat(m.high),
             road=[g for row in m.road for g in row],
             blocked=[1 if b else 0 for row in m.blocked for b in row],
             objects=[obj_json(o) for o in m.objects])
    d["palette"] = palette
    with open(os.path.join(map_dir, m.id + ".json"), "w") as fp:
        json.dump(d, fp, ensure_ascii=False, separators=(",", ":"))
    m.render(os.path.join(os.path.dirname(__file__), "preview_" + m.id + ".png"))
for k in used:
    path = os.path.join(tile_dir, name_of(k) + ".png")
    image = tile_image(k)
    # Pillow 버전별 PNG 압축 차이만으로 모든 원본 타일이 변경되지 않게 한다.
    if os.path.exists(path):
        with Image.open(path) as old:
            if old.size == image.size and old.convert("RGBA").tobytes() == image.tobytes():
                continue
    image.save(path)
cartography.export(world_maps, OUT)
print(len(used), "tiles")
