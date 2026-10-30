"""배치안 D의 강·산·길·메인 퀘스트 도식과 강 흐름 검사 (Docs/WorldLayout.md 3절).

먼저 layout_proposal_d.py로 면적 JSON을 만든 뒤 실행한다.
사용: python layout_proposal_d_terrain.py [면적.json] [그림데이터.json]
- 강마다 발원지가 육지인지, 물길이 육지 위인지, 하구가 바다인지 출력한다.
- 길과 강이 만나는 곳(다리)을 출력한다.
- 그림데이터.json은 Figma 검토안 그림에 쓴 SVG·라벨이다.
필요: numpy, opencv-python
"""
import sys
import json, math
import numpy as np, cv2
g = json.load(open(sys.argv[1] if len(sys.argv) > 1 else "layout_proposal_d.json")); R = g["R_land"]
COL = {"far": "#DCEBF1", "near": "#A9D3E4", "cont": "#F3EBD3",
       "개경": "#E59A7E", "송악": "#B9A78C", "나루": "#7FC3D6", "장단": "#F1CF6E", "닥목": "#8DC48A",
       "village": "#F6D9A6", "travel": "#D9C08E"}
# ---------- land masks for validation ----------
X0, Y0 = -230, -230
gland = np.zeros((460, 670), np.uint8)
for p in g["goryeo_land_poly"]: cv2.fillPoly(gland, [np.array([[x - X0, y - Y0] for x, y in p], np.int32)], 1)
def on_land(x, y):
    return x * x + y * y <= R * R or gland[int(y - Y0), int(x - X0)] == 1
def zone_of(x, y):
    for grp in ("goryeo", "east"):
        for k, v in g[grp].items():
            for p in v["poly"]:
                if cv2.pointPolygonTest(np.array(p, np.float32), (x, y), False) >= 0: return k
    return None

RIVERS = {
    "큰 강 (곡창을 지나 나루 하구로)": [(198, -64), (203, -40), (207, -15), (209, 10), (214, 32), (226, 48), (242, 56), (256, 63), (262, 74)],
    "개경 강 (닥목 계곡 → 개경 항구)": [(224, -56), (236, -42), (250, -30), (264, -22), (278, -19), (293, -18)],
    "대륙 동쪽 강 (북동 길)": [(86, -22), (104, -34), (122, -50), (140, -66), (150, -76), (162, -86)],
}
report = {}
for name, pts_ in RIVERS.items():
    land = [on_land(*p) for p in pts_]
    report[name] = {"source_on_land": bool(land[0]), "body_on_land": bool(all(land[:-1])), "mouth_in_sea": bool(not land[-1]),
                    "zones": sorted({zone_of(*p) for p in pts_[:-1]} - {None})}

class F:
    def __init__(s, ox, oy, sc): s.ox, s.oy, s.s = ox, oy, sc
    def __call__(s, x, y): return ((x - s.ox) * s.s, (y - s.oy) * s.s)
def pts(p, f): return " ".join("%.1f,%.1f" % f(x, y) for x, y in p)
def path(ps, f): return "M" + " L".join("%.1f,%.1f" % f(*p) for p in ps)
def smooth(ps, f):
    q = [f(*p) for p in ps]; d = f"M{q[0][0]:.1f},{q[0][1]:.1f}"
    for i in range(1, len(q) - 1):
        mx, my = (q[i][0] + q[i + 1][0]) / 2, (q[i][1] + q[i + 1][1]) / 2
        d += f" Q{q[i][0]:.1f},{q[i][1]:.1f} {mx:.1f},{my:.1f}"
    return d + f" L{q[-1][0]:.1f},{q[-1][1]:.1f}"
def poly(p, f, fill, stroke="none", sw=0):
    return f'<polygon points="{pts(p, f)}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" stroke-linejoin="round"/>'
def tint(hexc, k=0.55):
    h = int(hexc[1:], 16); r, gg, b = (h >> 16) & 255, (h >> 8) & 255, h & 255
    return "#%02X%02X%02X" % (int(r * (1 - k) + 0xA9 * k), int(gg * (1 - k) + 0xD3 * k), int(b * (1 - k) + 0xE4 * k))
def is_v(k): return "마을" in k or "포구" in k
half = g["wedge_half_deg"]
def wedge_lines(f, r):
    out = []
    for a in (90 - half, 90 + half):
        t = math.radians(a); out.append(f'<path d="{path([(0, 0), (r * math.sin(t), -r * math.cos(t))], f)}" stroke="#9B8357" stroke-width="1.5" stroke-dasharray="6 5" fill="none"/>')
    return "".join(out)

# ---------- overview ----------
f1 = F(-330, -330, 1.0); W1 = H1 = 660
o = [f'<svg width="{W1}" height="{H1}" viewBox="0 0 {W1} {H1}" xmlns="http://www.w3.org/2000/svg"><rect width="{W1}" height="{H1}" fill="{COL["far"]}"/>']
for (x, y, w, h) in [(-70, -R - 110, 140, 90), (-70, R + 20, 140, 90), (-R - 110, -70, 90, 140)]:
    a = f1(x, y); o.append(f'<rect x="{a[0]:.1f}" y="{a[1]:.1f}" width="{w}" height="{h}" rx="14" fill="#E3DED3" stroke="#B9B2A3" stroke-width="1.5" stroke-dasharray="5 4"/>')
for p in g["continent_region_poly"] + g["goryeo_region_poly"]: o.append(poly(p, f1, COL["near"]))
c = f1(0, 0); o.append(f'<circle cx="{c[0]}" cy="{c[1]}" r="{R:.1f}" fill="{COL["cont"]}"/>')
for k, v in g["east"].items():
    for p in v["land_poly"]: o.append(poly(p, f1, COL["village"] if is_v(k) else COL["travel"], "#FFFFFF", 0.5))
for k, v in g["goryeo"].items():
    for p in v["land_poly"]: o.append(poly(p, f1, COL[k], "#FFFFFF", 0.5))
for pts_ in RIVERS.values(): o.append(f'<path d="{smooth(pts_, f1)}" stroke="#4E9CC9" stroke-width="2" fill="none"/>')
o.append(wedge_lines(f1, R + 12))
for p in g["villages"].values():
    q = f1(*p); o.append(f'<circle cx="{q[0]:.1f}" cy="{q[1]:.1f}" r="4" fill="#FFFDF6" stroke="#3B2F25" stroke-width="1.5"/>')
o.append("</svg>"); svg1 = "".join(o)
lab1 = [("고려 20,000", *f1(225, -100), 13, "#5A2A18"), ("동쪽 담당 25,000", *f1(110, -150), 12, "#7A5A1E"),
        ("중앙 대륙 100,000", *f1(-85, 100), 13, "#6B5B3E"), ("북유럽", *f1(0, -R - 65), 12, "#7A7468"),
        ("아라비아", *f1(0, R + 65), 12, "#7A7468"), ("베네치아", *f1(-R - 65, 0), 12, "#7A7468")]

# ---------- detail ----------
s2 = 2.4; f2 = F(-20, -200, s2); W2, H2 = 360 * s2, 400 * s2
d = [f'<svg width="{W2:.0f}" height="{H2:.0f}" viewBox="0 0 {W2:.0f} {H2:.0f}" xmlns="http://www.w3.org/2000/svg"><rect width="{W2:.0f}" height="{H2:.0f}" fill="{COL["far"]}"/>']
for p in g["continent_region_poly"]: d.append(poly(p, f2, COL["near"]))
c2 = f2(0, 0); d.append(f'<circle cx="{c2[0]:.1f}" cy="{c2[1]:.1f}" r="{R*s2:.1f}" fill="{COL["cont"]}"/>')
for k, v in g["east"].items():
    fill = COL["village"] if is_v(k) else COL["travel"]
    for p in v["poly"]: d.append(poly(p, f2, tint(fill), "#FFFFFF", 2.5))
    for p in v["land_poly"]: d.append(poly(p, f2, fill, "#FFFFFF", 2.5))
for k, v in g["goryeo"].items():
    for p in v["poly"]: d.append(poly(p, f2, tint(COL[k]), "#FFFFFF", 2.5))
    for p in v["land_poly"]: d.append(poly(p, f2, COL[k], "#FFFFFF", 2.5))
d.append(wedge_lines(f2, R + 14))
def mount(x, y, sz=6):
    a = f2(x - sz, y + sz * 0.6); b = f2(x, y - sz * 0.8); e = f2(x + sz, y + sz * 0.6)
    return f'<polygon points="{a[0]:.1f},{a[1]:.1f} {b[0]:.1f},{b[1]:.1f} {e[0]:.1f},{e[1]:.1f}" fill="#8C7B66" stroke="#FFFFFF" stroke-width="1.5"/>'
MOUNTAINS = [(204, -70), (216, -73), (229, -72), (242, -66), (192, -60),       # 북쪽 해안 산맥 (닥목)
             (184, -44), (178, -32), (190, -50),                              # 송악 산록
             (70, -60), (58, -48), (82, -70), (66, 60), (54, 48)]             # 대륙 내륙 산지
for (x, y) in MOUNTAINS: d.append(mount(x, y))
# fields (granary) as small dotted rows
for (x, y) in [(196, 34), (206, 40), (186, 44), (198, 52), (214, 58), (226, 62)]:
    q = f2(x, y); d.append(f'<rect x="{q[0]-9:.1f}" y="{q[1]-5:.1f}" width="18" height="10" rx="2" fill="#E3B84A" stroke="#FFFFFF" stroke-width="1"/>')
for pts_ in RIVERS.values():
    d.append(f'<path d="{smooth(pts_, f2)}" fill="none" stroke="#4E9CC9" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"/>')

GC = {k: tuple(v["land_centroid"]) for k, v in g["goryeo"].items()}
T = {"개경": (262, -12), "송악": GC["송악"], "닥목": (220, -52), "나루": (254, 44), "장단": (200, 38)}
T.update({k: tuple(v) for k, v in g["villages"].items()})
ROADS = [("나루", "장단"), ("장단", "송악"), ("송악", "닥목"), ("닥목", "개경"), ("개경", "나루"), ("개경", "송악"), ("송악", "동문 마을"),
         ("동문 마을", "북동 포구"), ("동문 마을", "남동 포구"), ("동문 마을", "중앙 길목 마을"), ("북동 포구", "중앙 길목 마을"), ("남동 포구", "중앙 길목 마을")]
for a, b in ROADS:
    d.append(f'<path d="{path([T[a], T[b]], f2)}" stroke="#8A5A3A" stroke-width="5" stroke-linecap="round" fill="none" opacity="0.85"/>')
for n, tgt in [("중앙 길목 마을", (-15, 0)), ("북동 포구", (70, -150)), ("남동 포구", (70, 150))]:
    d.append(f'<path d="{path([T[n], tgt], f2)}" stroke="#8A5A3A" stroke-width="4" stroke-dasharray="9 7" stroke-linecap="round" fill="none"/>')
# bridges where roads cross rivers
def seg_x(p1, p2, p3, p4):
    d_ = (p2[0]-p1[0])*(p4[1]-p3[1]) - (p2[1]-p1[1])*(p4[0]-p3[0])
    if abs(d_) < 1e-9: return None
    t = ((p3[0]-p1[0])*(p4[1]-p3[1]) - (p3[1]-p1[1])*(p4[0]-p3[0])) / d_
    u = ((p3[0]-p1[0])*(p2[1]-p1[1]) - (p3[1]-p1[1])*(p2[0]-p1[0])) / d_
    return (p1[0]+t*(p2[0]-p1[0]), p1[1]+t*(p2[1]-p1[1])) if 0 <= t <= 1 and 0 <= u <= 1 else None
bridges = []
for a, b in ROADS:
    for pts_ in RIVERS.values():
        for i in range(len(pts_) - 1):
            x = seg_x(T[a], T[b], pts_[i], pts_[i + 1])
            if x: bridges.append((a, b, x))
for _, _, x in bridges:
    q = f2(*x); d.append(f'<rect x="{q[0]-8:.1f}" y="{q[1]-5:.1f}" width="16" height="10" rx="2" fill="#6B4A2E" stroke="#FFFFFF" stroke-width="1.5"/>')
# main quest route: 나루 → 장단 → 송악 → 닥목 → 개경
MQ = ["나루", "장단", "송악", "닥목", "개경"]
off = lambda p: (p[0] + 2.5, p[1] + 2.5)
d.append(f'<path d="{path([off(T[k]) for k in MQ], f2)}" stroke="#D2463A" stroke-width="4" stroke-dasharray="2 7" stroke-linecap="round" fill="none"/>')
for n, p in T.items():
    q = f2(*p)
    if n == "개경": d.append(f'<rect x="{q[0]-11:.1f}" y="{q[1]-11:.1f}" width="22" height="22" rx="4" fill="#FFFDF6" stroke="#7A1E10" stroke-width="4"/>')
    else: d.append(f'<circle cx="{q[0]:.1f}" cy="{q[1]:.1f}" r="8" fill="#FFFDF6" stroke="#3B2F25" stroke-width="3"/>')
for i, k in enumerate(MQ):
    q = f2(*T[k]); d.append(f'<circle cx="{q[0]-17:.1f}" cy="{q[1]-15:.1f}" r="10" fill="#D2463A" stroke="#FFFFFF" stroke-width="2"/>')
ev = [((T["동문 마을"][0]+T["남동 포구"][0])/2, (T["동문 마을"][1]+T["남동 포구"][1])/2),
      ((T["동문 마을"][0]+T["중앙 길목 마을"][0])/2, (T["동문 마을"][1]+T["중앙 길목 마을"][1])/2 - 6)]
for m in ev:
    q = f2(*m); d.append(f'<circle cx="{q[0]:.1f}" cy="{q[1]:.1f}" r="9" fill="#C0563A" stroke="#FFFFFF" stroke-width="2"/>')
d.append("</svg>"); svg2 = "".join(d)

lab2 = []
def L(name, sub, p, size=16, col="#2E241B", dy=22):
    x, y = f2(*p); return [(name, x, y - dy, size, col), (sub, x, y + dy, 11, "#5C4D3E")]
def cs(k, grp): return f"{g[grp][k]['cells']:,}칸"
lab2 += L(f"개경 (수도) {cs('개경','goryeo')}", "동해안 강 하구 항구 · 궁성 · 시전", T["개경"], 17, "#6E1A0C", 24)
lab2 += L(f"장단 {cs('장단','goryeo')}", "남쪽 곡창 · 큰 강 유역 논", T["장단"])
lab2 += L(f"나루 {cs('나루','goryeo')}", "큰 강 하구 나루터 · 시작", T["나루"])
lab2 += L(f"닥목 {cs('닥목','goryeo')}", "숲 계곡 · 한지 · 두 강의 발원", T["닥목"])
lab2 += L(f"송악 {cs('송악','goryeo')}", "산록 · 대륙 관문", T["송악"])
subs = {"동문 마을": "고려와 가장 가까운 마을", "북동 포구": "해안 교역 · 장터 후보", "남동 포구": "해안 교역 · 장터 후보", "중앙 길목 마을": "대륙 중심 쪽 · 장터 후보"}
for k in g["villages"]: lab2 += L(f"{k} {cs(k,'east')}", subs[k], T[k], 14)
for i, k in enumerate(MQ):
    x, y = f2(*T[k]); lab2.append((str(i + 1), x - 17 * 1, y - 15, 11, "#FFFFFF"))
for txt, p in [("북동 길", (150, -40)), ("남동 길", (150, 45)), ("북쪽 내륙길", (55, -75)), ("남쪽 내륙길", (55, 80))]:
    x, y = f2(*p); lab2.append((txt + f" {g['east'][txt]['cells']:,}칸", x, y, 11, "#7A5A1E"))
x, y = f2(*ev[0]); lab2.append(("산적 · 통나무", x + 44, y, 11, "#A0432C"))
x, y = f2(122, -50); lab2.append(("강 범람 구간", x - 40, y - 16, 11, "#A0432C"))
for txt, p in [("→ 북유럽 쪽", (75, -160)), ("→ 아라비아 쪽", (75, 160)), ("→ 중앙", (-8, 14))]:
    x, y = f2(*p); lab2.append((txt, x, y, 11, "#7A5A1E"))
x, y = f2(300, 125); lab2.append(("먼 바다 (지도만)", x, y, 12, "#3F7C98"))
x, y = f2(298, -86); lab2.append(("앞바다 (면적 포함)", x, y, 12, "#2F6A86"))
x, y = f2(190, 82); lab2.append(("곡창지대", x, y, 13, "#8A6A10"))
json.dump({"svg1": svg1, "svg2": svg2, "lab1": lab1, "lab2": lab2, "W1": W1, "H1": H1, "W2": W2, "H2": H2},
          open(sys.argv[2] if len(sys.argv) > 2 else "layout_proposal_d_figma.json", "w"), ensure_ascii=False)
print(json.dumps(report, ensure_ascii=False, indent=1))
print("bridges", [(a, b) for a, b, _ in bridges])
print(len(svg1), len(svg2))
