"""통합 연결 자리까지 다른 팀 맵에 흙길을 이어 그린다 (2026-10-28, 기우 확인).
그 맵에 이미 있는 길 타일의 이름 규칙(…_<변형>_<북동남서 가장자리 e>----)을 그대로 따라, 새 길 칸과 맞닿은 칸의 가장자리만 다시 고른다.
생성기 원본은 건드리지 않는다. 그 팀이 맵을 다시 만들면 connect_regions.py 다음에 이 스크립트를 다시 실행한다.
사용: python Tools/Integration/paint_link_roads.py"""
import json, os, random

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
TILES = os.path.join(ROOT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")

# 맵 JSON, 새 길 칸 (x0, y0, x1, y1 직사각형 여러 개), 길 등급, (선택) 쓸 길 타일 묶음. 비우면 그 맵에서 가장 많이 쓴 길 타일
PAINTS = [
    ("Assets/PocketCaravan/Maps/VeniceMainland/venice_c_pass.json", [(26, 27, 59, 28)], 1),   # ⑨ 큰길 모퉁이 → 동쪽 끝 (서남 솔숲길)
    ("Assets/PocketCaravan/Maps/VeniceMainland/venice_c_gate.json", [(31, 0, 32, 29)], 1, "vq_L_g_orc_dirt"),   # ⑩ 동문 돌길에서 갈라지는 흙 샛길 → 북쪽 끝 (서남 솔숲길)
    # ⑪ 교역 길목 쪽 흙길은 고려 생성기(Tools/WorldLayout/m4_links.py)가 그린다 (10/28)
]


# 고려·공용 맵(Ninja Adventure 바닥 흙길, 원점 floor 0,7): 북동남서 이웃 → 타일 칸, 네 방향이 다 길이면 빈 대각선 → 안쪽 모서리 칸
NINJA = {"0110": (0, 7), "0111": (1, 7), "0011": (2, 7), "1110": (0, 8), "1111": (1, 8), "1011": (2, 8),
         "1100": (0, 9), "1101": (1, 9), "1001": (2, 9), "1010": (3, 8)}
NINJA_INNER = {"NW": (6, 9), "NE": (5, 9), "SE": (5, 8), "SW": (6, 8)}


def paint_ninja(j, touched, road, index, pal):
    W = j["w"]
    def inside_or_road(x, y):
        return road(x, y) or not (0 <= x < W and 0 <= y < j["h"])   # 맵 밖은 길이 이어지는 것으로 본다
    changed = 0
    for (x, y) in sorted(touched):
        if not road(x, y):
            continue
        key = "".join("1" if inside_or_road(x + dx, y + dy) else "0" for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)))
        cx, cy = NINJA.get(key, (1, 8))
        if key == "1111":
            for name, (dx, dy) in (("NW", (-1, -1)), ("NE", (1, -1)), ("SE", (1, 1)), ("SW", (-1, 1))):
                if not inside_or_road(x + dx, y + dy):
                    cx, cy = NINJA_INNER[name]; break
        tile = f"floor_{cx}_{cy}"
        if tile not in index:
            index[tile] = len(pal); pal.append(tile)
        if j["ground"][y * W + x] != index[tile]:
            j["ground"][y * W + x] = index[tile]; changed += 1
    return changed


def paint(path, rects, grade, family=None):
    p = os.path.join(ROOT, path)
    j = json.load(open(p, encoding="utf-8"))
    W, H, pal = j["w"], j["h"], j["palette"]
    road = lambda x, y: 0 <= x < W and 0 <= y < H and j["road"][y * W + x] > 0
    # 이 맵의 길 타일 묶음 이름 (가장 많이 쓴 것)
    from collections import Counter
    fams = Counter("_".join(pal[j["ground"][i]].split("_")[:-5]) for i in range(W * H) if j["road"][i] and pal[j["ground"][i]])
    fam = family or fams.most_common(1)[0][0]
    new = set()
    for (x0, y0, x1, y1) in rects:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if not road(x, y):
                    new.add((x, y))
                for layer in ("deco", "obj", "high"):
                    j[layer][y * W + x] = 0
                j["blocked"][y * W + x] = 0
                j["road"][y * W + x] = max(j["road"][y * W + x], grade)
    touched = {(x + dx, y + dy) for (x, y) in new for dx in (-1, 0, 1) for dy in (-1, 0, 1)}
    rng = random.Random(path)
    index = {n: i for i, n in enumerate(pal)}
    import re
    top = Counter(pal[j["ground"][i]] for i in range(W * H) if j["road"][i] and j["ground"][i]).most_common(1)[0][0]
    if family is None and re.fullmatch(r"floor_\d+_\d+", top):
        changed = paint_ninja(j, touched | {(x, y) for (x0, y0, x1, y1) in rects for x in range(x0, x1 + 1) for y in range(y0, y1 + 1)}, road, index, pal)
        with open(p, "w", encoding="utf-8") as fp:
            json.dump(j, fp, ensure_ascii=False, separators=(",", ":"))
        print(f"{path}: 새 길 {len(new)}칸, 타일 다시 고른 칸 {changed} (흙길 오토타일)")
        return
    changed = 0
    for (x, y) in sorted(touched):
        if not road(x, y):
            continue
        cur = pal[j["ground"][y * W + x]] if j["ground"][y * W + x] else ""
        flags = "".join("e" if not road(x + dx, y + dy) else "-" for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0))) + "----"
        if cur.startswith(fam) and cur.endswith(flags):
            continue
        variant = "_".join(cur.split("_")[-5:-1]) if cur.startswith(fam) else None
        cands = [f[:-4] for f in os.listdir(TILES) if f.startswith(fam + "_") and f.endswith("_" + flags + ".png")]
        if not cands:
            raise SystemExit(f"{path}: 길 타일 없음 {fam} {flags}")
        want = f"{fam}_{variant}_{flags}" if variant else None
        name = want if want in cands else rng.choice(sorted(cands))
        if name not in index:
            index[name] = len(pal); pal.append(name)
        j["ground"][y * W + x] = index[name]
        changed += 1
    with open(p, "w", encoding="utf-8") as fp:
        json.dump(j, fp, ensure_ascii=False, separators=(",", ":"))
    print(f"{path}: 새 길 {len(new)}칸, 타일 다시 고른 칸 {changed}")


if __name__ == "__main__":
    for entry in PAINTS:
        paint(*entry)
