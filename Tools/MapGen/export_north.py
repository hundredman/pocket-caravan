"""북유럽 맵 JSON과 사용한 타일 PNG를 내보낸다. 고려(export.py)와 폴더를 나눠 합칠 때 충돌이 없게 한다.
    Maps/North/*.json, Art/North/Tiles/*.png  →  Unity 메뉴 Pocket Caravan > 북유럽 > 맵 씬 만들기"""
import json, os, sys
import north
from tiles import tile_image

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
tile_dir = os.path.join(OUT, "Assets/PocketCaravan/Art/North/Tiles")
map_dir = os.path.join(OUT, "Assets/PocketCaravan/Maps/North")
os.makedirs(tile_dir, exist_ok=True); os.makedirs(map_dir, exist_ok=True)

def name_of(key):
    return key.replace(":", "_").replace("@", "_")

DEFAULTS = dict(w=1, h=1, id="", npc="", town="", target="", spawn="", edge="", text="", name="", character="",
                lines=[], variant=0, idx=0, sheet="", frames=0, fps=0.0, radius=0.0, type="", path=[])
used = set()

def obj_json(o):
    d = dict(DEFAULTS); d.update(o)
    d["path"] = [v for pt in o.get("path", []) for v in pt]
    if o["kind"] == "growth":
        used.add(o["id"]); d["id"] = name_of(o["id"])
    return d

for f in north.MAPS:
    m = f()
    palette, index = [""], {"": 0}
    def flat(layer):
        out = []
        for row in layer:
            for k in row:
                k = k or ""
                if k: used.add(k)
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
    m.render(os.path.join(os.path.dirname(os.path.abspath(__file__)), "preview_" + m.id + ".png"))
    print(m.id, m.w, m.h)
for k in used:
    tile_image(k).save(os.path.join(tile_dir, name_of(k) + ".png"))
print(len(used), "tiles")
