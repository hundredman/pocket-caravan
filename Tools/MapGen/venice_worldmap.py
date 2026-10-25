"""M 지도(베네치아·본토 서부)용 축약 지형 그림과 배치 JSON을 만든다.
    사용: python Tools/MapGen/export_venice.py  (미리보기 PNG가 먼저 있어야 한다)
          python Tools/MapGen/venice_worldmap.py
    출력: Assets/PocketCaravan/Resources/WorldMap/VeniceTerrain.png, VeniceLayout.json
    - 각 씬의 실제 타일 미리보기(preview_<mapId>.png)를 칸당 PX픽셀로 줄여 지리 순서(서→동)대로 놓는다.
    - 씬 사이 선은 실제 출구끼리 잇는다(같은 쌍이 둘이면 둘 다). 키오자↔본토 나루터는 뱃길(점선).
    - 배치는 보기 위한 축약이며 가격용 worldCoord와 무관하다. 고려 지도(cartography.py)는 건드리지 않는다."""
import json, os
from PIL import Image, ImageDraw
from world_registry import ROOT, map_entries

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "Assets/PocketCaravan/Resources/WorldMap")
PX = 2          # 칸당 픽셀
GAP = 14
# 지도 위 배치 (칸 단위 왼쪽 위). 서쪽 석호 → 동쪽 본토
POS = {
    "venice_santerasmo": (10, 52), "venice_burano": (10, 166),
    "venice_rialto": (86, 82), "venice_murano": (183, 32), "venice_chioggia": (183, 146),
    "venice_c_landing": (288, 172), "venice_c_vineyard": (296, 72), "venice_c_marsh": (296, 274),
    "venice_c_pass": (402, 74), "venice_c_post": (402, 176), "venice_c_bridge": (402, 284),
    "venice_c_gate": (500, 160),
}
PAPER = (225, 211, 171)
LAGOON = (150, 186, 184)
ROUTE = (124, 78, 40)
SEA_ROUTE = (176, 64, 52)


def main():
    maps = {}
    for region in ("venice", "center_venice"):
        for e in map_entries(region):
            with open(os.path.join(ROOT, e["jsonPath"]), encoding="utf-8") as fp:
                maps[e["mapId"]] = json.load(fp)
    W = max(x + maps[k]["w"] for k, (x, y) in POS.items()) + 20
    H = max(y + maps[k]["h"] for k, (x, y) in POS.items()) + 12
    img = Image.new("RGB", (W * PX, H * PX), PAPER)
    d = ImageDraw.Draw(img)
    # 석호 쪽 바탕은 옅은 물빛 (본토와 구분)
    lagoon_x = POS["venice_chioggia"][0] + maps["venice_chioggia"]["w"] + 8
    d.rectangle((0, 0, lagoon_x * PX, H * PX), fill=LAGOON)
    for k, (x, y) in POS.items():
        m = maps[k]
        pv = Image.open(os.path.join(HERE, f"preview_{k}.png")).convert("RGB")
        img.paste(pv.resize((m["w"] * PX, m["h"] * PX), Image.LANCZOS), (x * PX, y * PX))
        d.rectangle((x * PX - 1, y * PX - 1, (x + m["w"]) * PX, (y + m["h"]) * PX), outline=(92, 72, 48), width=2)

    def center(k, o):
        x, y = POS[k]
        return ((x + o["x"] + o["w"] / 2) * PX, (y + o["y"] + o["h"] / 2) * PX)

    done = set()
    for k, m in maps.items():
        for o in m["objects"]:
            if o["kind"] != "exit" or o["target"] not in maps:
                continue
            t = o["target"]
            cand = [b for b in maps[t]["objects"] if b["kind"] == "exit" and b["target"] == k]
            suf = o["spawn"].rsplit("_", 1)[-1]
            b = next((b for b in cand if b["spawn"].rsplit("_", 1)[-1] == suf), cand[0])
            pair = tuple(sorted([(k, o["x"], o["y"]), (t, b["x"], b["y"])]))
            if pair in done:
                continue
            done.add(pair)
            p, q = center(k, o), center(t, b)
            boat = {k, t} == {"venice_chioggia", "venice_c_landing"}
            if boat:      # 뱃길: 점선
                n = 10
                for i in range(0, n, 2):
                    a = (p[0] + (q[0] - p[0]) * i / n, p[1] + (q[1] - p[1]) * i / n)
                    c = (p[0] + (q[0] - p[0]) * (i + 1) / n, p[1] + (q[1] - p[1]) * (i + 1) / n)
                    d.line([a, c], fill=SEA_ROUTE, width=4)
            else:
                d.line([p, q], fill=ROUTE, width=4)
            for pt in (p, q):
                d.ellipse((pt[0] - 4, pt[1] - 4, pt[0] + 4, pt[1] + 4), fill=SEA_ROUTE if boat else ROUTE)
    # 동문 큰길 동쪽: 중앙 권역 방향 화살표 (연결 전)
    gx, gy = POS["venice_c_gate"]
    gm = maps["venice_c_gate"]
    ay = (gy + 33) * PX
    ax = (gx + gm["w"]) * PX
    d.line([(ax, ay), (ax + 12 * PX, ay)], fill=(120, 110, 96), width=5)
    d.polygon([(ax + 12 * PX, ay - 8), (ax + 12 * PX, ay + 8), (ax + 17 * PX, ay)], fill=(120, 110, 96))
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, "VeniceTerrain.png"))
    layout = dict(width=W, height=H, maps=[], annotations=[
        dict(text="베네치아 석호", x=12, y=8), dict(text="본토 서부", x=lagoon_x + 8, y=8),
        dict(text="→ 중앙 권역 (길 닦는 중)", x=gx + gm["w"] - 34, y=gy + 40)])
    for k, (x, y) in POS.items():
        m = maps[k]
        layout["maps"].append(dict(mapId=k, x=x, y=y, w=m["w"], h=m["h"], labelX=x + m["w"] / 2, labelY=y + 6))
    with open(os.path.join(OUT, "VeniceLayout.json"), "w", encoding="utf-8", newline="\n") as fp:
        json.dump(layout, fp, ensure_ascii=False, indent=1)
    print("VeniceTerrain", img.size, "tiles", W, H)


if __name__ == "__main__":
    main()
