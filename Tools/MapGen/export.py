"""맵 JSON과 사용한 타일 PNG를 Unity 프로젝트 구조로 내보낸다."""
import json, os, sys, shutil
import maps, gaegyeong
from tiles import tile_image

# 사용법: python3 export.py <저장소 루트>  (기본: 이 파일 기준 저장소 루트)
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..")
tile_dir = os.path.join(OUT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")
map_dir = os.path.join(OUT, "Assets/PocketCaravan/Maps")
os.makedirs(tile_dir, exist_ok=True); os.makedirs(map_dir, exist_ok=True)

def name_of(key):
    return key.replace(":", "_").replace("@", "_")

used = set()
for f in (maps.naru, maps.route01, gaegyeong.build, maps.dakmok):
    m = f()
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
    d = dict(id=m.id, name=m.name, w=m.w, h=m.h,
             ground=flat(m.ground), deco=flat(m.deco), obj=flat(m.obj), high=flat(m.high),
             road=[g for row in m.road for g in row],
             blocked=[1 if b else 0 for row in m.blocked for b in row],
             objects=[dict(kind=o["kind"], x=o["x"], y=o["y"], w=o.get("w", 1), h=o.get("h", 1),
                           id=o.get("id", ""), npc=o.get("npc", ""), town=o.get("town", ""),
                           target=o.get("target", ""), spawn=o.get("spawn", ""), variant=o.get("variant", 0), idx=o.get("idx", 0))
                      for o in m.objects])
    d["palette"] = palette
    with open(os.path.join(map_dir, m.id + ".json"), "w") as fp:
        json.dump(d, fp, ensure_ascii=False, separators=(",", ":"))
    m.render(os.path.join(os.path.dirname(__file__), "preview_" + m.id + ".png"))
for k in used:
    tile_image(k).save(os.path.join(tile_dir, name_of(k) + ".png"))
print(len(used), "tiles")
