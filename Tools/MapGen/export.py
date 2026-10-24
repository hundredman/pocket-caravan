"""맵 JSON과 사용한 타일 PNG를 Unity 프로젝트 구조로 내보낸다."""
import json, os, sys, shutil
from world_registry import atlas_content, map_entries
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
# Validate complete authored silhouettes before writing any map/output.
from goryeo_art import CUSTOM, sprite
for asset,w,h in CUSTOM.values():sprite(asset,w,h)
entries, world_maps = atlas_content()
from goryeo_art import export_ripples
export_ripples(OUT)
for entry, m in zip(entries, world_maps):
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
    d["legacyHeight"] = getattr(m,"legacy_height",m.h)
    d["palette"] = palette
    if hasattr(m,"world_reachable"):d["reachable"]=[int((x,y) in m.world_reachable) for y in range(m.h) for x in range(m.w)]
    d["water"] = [int(v) for row in m.water for v in row]
    if hasattr(m,"world_origin"):d["terrainX"],d["terrainY"]=m.world_origin
    if getattr(m,"visual_skin",None): d["visualSkin"] = m.visual_skin
    with open(os.path.join(OUT, entry["jsonPath"]), "w", encoding="utf-8") as fp:
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
# 발표 및 지역 기획용 확대 지도. 게임 내 전체 지도와 같은 실제 타일을 사용한다.
cartography.export([m for e,m in zip(entries,world_maps) if e["mapId"] in {entry["mapId"] for entry in map_entries("goryeo")}], OUT, stem="GoryeoRegional")
print(len(used), "tiles")

# Runtime save migration shares the same layout definition as map generation.
from broad_regions import REGIONS, ALIASES
migration={"maps":[],"aliases":[{"oldId":a,"newId":b} for a,b in ALIASES.items()]}
for m in world_maps:
    if m.id not in REGIONS:continue
    sp=next(o for o in m.objects if o['kind']=='spawn' and o['id']=='S0')
    dx,dy=m.core_offset
    migration['maps'].append(dict(id=m.id,offsetX=dx,offsetY=dy,height=m.h,legacyHeight=m.legacy_height,spawnX=sp['x']+.5,spawnY=m.h-1-sp['y']+.82))
Path=__import__('pathlib').Path
Path(OUT,'Assets/PocketCaravan/Resources/GoryeoLayout.json').write_text(json.dumps(migration,indent=2)+'\n')
