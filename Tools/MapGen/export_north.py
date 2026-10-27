"""북유럽 맵 JSON과 사용한 타일 PNG를 내보낸다 (팀 규칙: Docs/TeamDevelopment.md).
    맵: Maps/North/<mapId>.json (World/Regions/north.json에 등록)
    타일: 공용 Art/NinjaAdventure/Tiles (공용 빌더가 여기서 읽음. 고려와 같은 그림은 이미 있어 그대로 둔다)
    다음: Unity 메뉴 Pocket Caravan > 2. 맵 씬 다시 만들기"""
import json, os, sys
import north
from tiles import tile_image

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
tile_dir = os.path.join(OUT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")
map_dir = os.path.join(OUT, "Assets/PocketCaravan/Maps/North")
center_dir = os.path.join(OUT, "Assets/PocketCaravan/Maps/Center")   # 10/24 중앙 북쪽 삼각형 (World/Regions/center_north.json)
os.makedirs(tile_dir, exist_ok=True); os.makedirs(map_dir, exist_ok=True); os.makedirs(center_dir, exist_ok=True)

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

from north_spots import *   # 10/27 자리 계산·이름표 정리 (north_spots.py)

for f in north.MAPS:
    m = build(f)
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
    with open(os.path.join(center_dir if m.id.startswith("north_c_") else map_dir, m.id + ".json"), "w") as fp:
        json.dump(d, fp, ensure_ascii=False, separators=(",", ":"))
    m.render(os.path.join(os.path.dirname(os.path.abspath(__file__)), "preview_" + m.id + ".png"))
    print(m.id, m.w, m.h)
# M 지도용 북유럽 교역도 (Resources/WorldMap/NorthTerrain.png, NorthLayout.json)
import north_cartography
print("north map", north_cartography.export([build(f) for f in north.MAPS], OUT))

# 움직이는 깃발 (Ninja Adventure Animated/Flag, 16x16 4프레임). 북유럽 전용 이름이라 공용 그림과 겹치지 않는다
from tiles import PACK
from PIL import Image
anim_dir = os.path.join(OUT, "Assets/PocketCaravan/Art/NinjaAdventure/Animated")
flag = Image.open(os.path.join(PACK, "Backgrounds/Animated/Flag/FlagRed16x16.png")).convert("RGBA")
for i in range(4):
    path = os.path.join(anim_dir, f"north_flag_{i}.png")
    if not os.path.exists(path):
        flag.crop((i * 16, 0, i * 16 + 16, 16)).save(path)

# 온천 김 (북유럽 전용, 코드로 그림): 하얀 김 덩어리가 위로 피어오르며 옅어지는 4프레임
from PIL import ImageDraw
for i in range(4):
    path = os.path.join(anim_dir, f"north_steam_{i}.png")
    if os.path.exists(path):
        continue
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0)); dr = ImageDraw.Draw(img)
    for (bx, by, r, a) in [(5, 12, 3, 150), (10, 9, 3, 120), (7, 5, 2, 90)]:
        y = by - i * 2; alpha = max(30, a - i * 25)
        if y - r >= 0:
            dr.ellipse((bx - r + (i % 2), y - r, bx + r + (i % 2), y + r), fill=(255, 255, 255, alpha))
    img.save(path)

# 실행 중에 쓰는 북유럽 그림·자료 (Resources/North): 돌아다니는 동물, 굴뚝 연기, 불빛 자리
# NorthLife.cs가 Resources.Load로 읽는다. .meta는 고려 지도 그림의 가져오기 설정(점 필터·무압축·읽기 가능)을 새 GUID로 복사
import uuid, importlib.util
res_dir = os.path.join(OUT, "Assets/PocketCaravan/Resources/North")
os.makedirs(os.path.join(res_dir, "Critters"), exist_ok=True)
meta_src = os.path.join(OUT, "Assets/PocketCaravan/Resources/WorldMap/GoryeoTerrain.png.meta")
def save_runtime(img, path):
    img.save(path)
    meta = path + ".meta"
    if not os.path.exists(meta) and os.path.exists(meta_src):
        lines = open(meta_src).read().splitlines()
        open(meta, "w").write("\n".join("guid: " + uuid.uuid4().hex if l.startswith("guid:") else l for l in lines) + "\n")
spec = importlib.util.spec_from_file_location("north_critters", os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ArtGen", "north_critters.py"))
crit = importlib.util.module_from_spec(spec); spec.loader.exec_module(crit)
import tempfile
with tempfile.TemporaryDirectory() as td:
    for (name, f), img in crit.build(td).items():
        save_runtime(img, os.path.join(res_dir, "Critters", f"{name}_{f}.png"))
for i in range(4):   # 굴뚝 연기: 회색 김이 피어오름 (온천 김보다 진하고 큼)
    img = Image.new("RGBA", (16, 24), (0, 0, 0, 0)); dr = ImageDraw.Draw(img)
    for (bx, by, r, a) in [(7, 20, 3, 170), (9, 15, 3, 140), (7, 10, 3, 110), (9, 5, 2, 80)]:
        y = by - i * 2; alpha = max(20, a - i * 20)
        if y - r >= 0:
            dr.ellipse((bx - r + (i % 2), y - r, bx + r + (i % 2), y + r), fill=(214, 214, 222, alpha))
    save_runtime(img, os.path.join(res_dir, f"smoke_{i}.png"))
import random as _rnd
for i in range(4):   # 모닥불 불꽃: 아래는 주황, 위로 갈수록 노랗고 가늘게 흔들림
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0)); px = img.load(); rr = _rnd.Random(40 + i)
    for y in range(16):
        k = y / 15.0                                   # 0 위 ~ 1 아래
        half = 1 + int(5 * k ** 0.8)
        cx = 8 + int(round(1.2 * __import__("math").sin(i * 1.6 + y * 0.7) * (1 - k)))
        for x in range(cx - half, cx + half):
            if 0 <= x < 16 and rr.random() < 0.8 + 0.2 * k:
                inner = abs(x - cx + 0.5) < half * 0.5
                col = (255, 236, 140, 255) if inner and k > 0.35 else (255, 160, 60, 235) if k > 0.25 else (255, 110, 40, 200)
                if y > 2 or rr.random() < 0.4:
                    px[x, y] = col
    save_runtime(img, os.path.join(res_dir, f"flame_{i}.png"))
for i in range(4):   # 눈 반짝임: 대부분 비어 있고 한 프레임에만 작은 별
    img = Image.new("RGBA", (8, 8), (0, 0, 0, 0)); dr = ImageDraw.Draw(img)
    if i == 1:
        dr.point((4, 4), fill=(255, 255, 255, 230))
    if i == 2:
        for p in [(4, 2), (4, 3), (4, 4), (4, 5), (4, 6), (2, 4), (3, 4), (5, 4), (6, 4)]:
            dr.point(p, fill=(255, 255, 255, 240))
        dr.point((4, 4), fill=(200, 240, 255, 255))
    if i == 3:
        dr.point((4, 4), fill=(255, 255, 255, 150))
    save_runtime(img, os.path.join(res_dir, f"glint_{i}.png"))

import north_minigame_art
os.makedirs(os.path.join(res_dir, "Minigame"), exist_ok=True)
for name, img in north_minigame_art.make(OUT).items():
    save_runtime(img, os.path.join(res_dir, "Minigame", name + ".png"))

CHIMNEY = {"longhall": (2, 3), "woodhouse": (2, 0), "house3": (2, 0), "h_dark": (3, 0), "h_turf": (1, 0), "h_round": (1, 0), "h_dome": (1, 0)}
life = []
for f in north.MAPS:
    m = build(f)
    st = m.stamps
    life.append(dict(mapId=m.id, h=m.h,
        chimneys=[[x + CHIMNEY[n][0], y + CHIMNEY[n][1]] for (n, x, y) in st if n in CHIMNEY],
        fires=[[x + 1, y + 1] for (n, x, y) in st if n == "fire"],
        spits=[[x + 1, y + 1] for (n, x, y) in st if n == "spit"],
        glints=[[x, y] for (x, y) in _rnd.Random(m.id).sample(
            [(x, y) for y in range(m.h) for x in range(m.w) if not m.blocked[y][x] and not m.water[y][x]
             and not m.road[y][x] and m.obj[y][x] is None and m.high[y][x] is None], 70)],
        lamps=[[x, y] for (n, x, y) in st if n in ("lamp", "lantern")],
        springs=[[x, y] for (x, y) in sorted(m._springs)][::6],
        runes=[[x, y] for (n, x, y) in st if m.id in ("north_runehill", "north_c_runeway") and n in ("runestone", "runestone_b", "picturestone", "snowpost")] + [[x, y] for (x, y, i) in getattr(m, "secrets", [])],
        crystals=[[x, y] for (n, x, y) in st if n.startswith(("crys_", "gem_"))],
        fishing=[list(c) for c in fishing_cells(m)],                       # 10/24 얼음 낚시: 플레이어가 서는 물가 칸
        fishingWater=[[x, y] for (x, y) in getattr(m, "fishing", [])],      #               찌를 던지는 물 칸
        sledStart=list(m.sled_track[0]) if getattr(m, "sled_track", None) else [],
        secrets=[list(c) for c in secret_cells(m)],                        # 10/24 숨은 룬돌: 읽는 칸 x, y, 번호
        secretStones=[[x, y] for (x, y, i) in getattr(m, "secrets", [])], # 밤에 빛나는 돌 위치
        ring=[v for c in spot_cells(m, "ring") for v in c],
        baths=[list(c) for c in spot_cells(m, "bath")],
        bathEdge=[v for (x, y) in sorted({(x + a, y + b) for (x, y) in m._springs for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))
                                          if 0 <= x + a < m.w and 0 <= y + b < m.h and (x + a, y + b) not in m._springs
                                          and not m.blocked[y + b][x + a] and not m.water[y + b][x + a]}) for v in (x, y)],   # 10/27 온천 둘레 어디서나 몸 담그기
        feed=[v for c in spot_cells(m, "feed") for v in c],
        guild=[v for c in spot_cells(m, "guild") for v in c],
        festival=[v for c in spot_cells(m, "festival") for v in c],
        events=[[o["x"], o["y"]] for o in m.objects if o["kind"] == "event"],
        museum=[v for c in spot_cells(m, "museum") for v in c] + list(museum_row(m)),
        fortune=[v for c in spot_cells(m, "fortune") for v in c],
        ferry=[v for c in spot_cells(m, "ferry") for v in c],
        seats=[v for (k, x, y) in getattr(m, "spots", []) if k == "seat" for v in (x, y)],   # 10/27 모닥불 옆 앉는 자리
        stalls=market_spots(m)[0],          # 10/27 장터 좌판 두 자리 (장꾼은 그 위 칸)
        crowd=market_spots(m)[1],           # 10/27 큰 장 날 구경꾼 자리
        inns=inn_spots(m),                  # 10/27 여관에서 쉬어 가기
        keepers=keeper_spots(m),            # 10/27 이미 있는 좌판 옆 장꾼 자리 (최대 넷)
        sides=side_spots(m),                # 10/27 떠돌이 상인·오늘의 특가 자리 (장터 옆 빈 땅)
        spitters=spit_sellers(m),           # 10/27 꼬치 장수 자리
        craft=[v for i, k in enumerate(("craft_fish", "craft_fur", "craft_brew")) for c in spot_cells(m, k) for v in (c[0], c[1], i)],   # 10/26 공방 x, y, 종류
        yard=[v for i, k in enumerate(("yard_fish", "yard_fur", "yard_brew")) for (kk, x, y) in getattr(m, "spots", []) if kk == k for v in (x, y, i)],   # 10/26 공방 마당 왼쪽 위 (그림 기준점)
        market=[v for o in m.objects if o["kind"] == "market" for v in nearest_walk(m, o["x"] + 2, o["y"] + 1, avoid={(o["x"], o["y"])})],
        sledTrack=[[x, y] for (x, y) in getattr(m, "sled_track", [])],     # 10/24 썰매 트랙 꼭짓점
        critters=m.life))
with open(os.path.join(res_dir, "NorthLife.json"), "w") as fp:
    json.dump(dict(maps=life), fp, ensure_ascii=False, indent=1)
print("life", sum(len(e["critters"]) for e in life), "critter groups,", sum(len(e["fires"]) + len(e["spits"]) for e in life), "fires")

new = 0
for k in used:
    path = os.path.join(tile_dir, name_of(k) + ".png")
    if os.path.exists(path):
        continue          # 이미 있는 공용 타일은 덮어쓰지 않는다 (.meta GUID 유지)
    tile_image(k).save(path); new += 1
print(len(used), "tiles used,", new, "new")
