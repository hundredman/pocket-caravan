"""배치안 D의 면적 계산 (Docs/WorldLayout.md 3절).

- 면적 = 육지 + 육지에 붙은 앞바다(해안에서 COAST칸 이내). 먼 바다는 지도에만 그린다.
- 고려: 개경을 동해안에, 남쪽 장단을 곡창지대로. 서쪽 송악이 중앙 대륙과 맞닿는 관문.
- 중앙 대륙 동쪽: 마을 4곳(각 3,200칸, 장터·이벤트 공간 확보) + 이동 구역 4곳.
칸 단위. 대륙 중심 = (0,0), x 동쪽, y 남쪽.
사용: python layout_proposal_d.py [출력.json]   필요: numpy, opencv-python
"""
import math, json, sys
import numpy as np, cv2

COAST = 10                       # 앞바다 폭(칸)
X0, Y0, X1, Y1 = -230, -230, 440, 230
ys, xs = np.mgrid[Y0:Y1, X0:X1] + 0.5
clock = (np.degrees(np.arctan2(xs, -ys)) + 360) % 360

def dist_to(mask):
    return cv2.distanceTransform((~mask).astype(np.uint8), cv2.DIST_L2, 5)

def build(r, s):
    cl = xs ** 2 + ys ** 2 <= r * r
    cx, a, b, n = r + 52 * s, 92 * s, 95 * s, 2.3
    gl = (~cl) & ((np.abs((xs - cx) / a) ** n + np.abs(ys / b) ** n) <= 1)
    land = cl | gl
    dc, dg = dist_to(cl), dist_to(gl)
    band = (~land) & (np.minimum(dc, dg) <= COAST)
    return cl, gl, cl | (band & (dc <= dg)), gl | (band & (dg < dc))

r, s = 170.0, 1.0
for _ in range(6):
    lo, hi = 140.0, 190.0
    for _ in range(30):
        m = (lo + hi) / 2
        if build(m, s)[2].sum() < 100_000: lo = m
        else: hi = m
    r = hi
    lo, hi = 0.5, 1.5
    for _ in range(30):
        m = (lo + hi) / 2
        if build(r, m)[3].sum() < 20_000: lo = m
        else: hi = m
    s = hi
cl, gl, creg, greg = build(r, s)

# 동쪽 담당: 3시 중심 대칭 부채꼴, 25,000칸이 되도록 각도 조정
lo, hi = 30.0, 60.0
for _ in range(40):
    h = (lo + hi) / 2
    w = creg & (np.abs(clock - 90) <= h)
    if w.sum() < 25_000: lo = h
    else: hi = h
half = hi; wedge = creg & (np.abs(clock - 90) <= half)

def partition(region, seeds, targets, iters=800):
    names = list(seeds); wt = {k: 0.0 for k in names}
    for _ in range(iters):
        dd = np.stack([np.hypot(xs - seeds[k][0], ys - seeds[k][1]) - wt[k] for k in names])
        lab = np.argmin(dd, 0)
        cnt = {k: int((region & (lab == i)).sum()) for i, k in enumerate(names)}
        if all(abs(cnt[k] - targets[k]) <= 3 for k in names): break
        for k in names: wt[k] += (targets[k] - cnt[k]) * 0.002
    return {k: region & (lab == i) for i, k in enumerate(names)}

# 고려 5맵
gx = xs[gl]; gxmin, gxmax = float(gx.min()), float(gx.max())
g_seeds = {"송악": (gxmin + 22, -8), "닥목": (gxmin + 75, -62), "개경": (gxmax - 22, -12),
           "나루": (gxmax - 30, 52), "장단": (gxmin + 62, 52)}
g_target = {"개경": 5000, "장단": 4500, "송악": 3500, "닥목": 3500, "나루": 3500}
gz = partition(greg, g_seeds, g_target)

# 중앙 동쪽: 마을 6곳(각 2,000) + 이동 구역 4곳
V = {"동문 마을": (r - 32, 0), "북동 포구": (r * 0.68, -r * 0.56), "남동 포구": (r * 0.68, r * 0.56),
     "중앙 길목 마을": (r * 0.30, 0)}
VILLAGE = 3200
ez = {}; taken = np.zeros_like(wedge)
for k, (vx, vy) in V.items():
    dd = np.where(wedge & ~taken, np.hypot(xs - vx, ys - vy), np.inf).ravel()
    mm = np.zeros(wedge.size, bool); mm[np.argsort(dd)[:VILLAGE]] = True; mm = mm.reshape(wedge.shape)
    ez[k] = mm; taken |= mm
rest = wedge & ~taken; tot = int(rest.sum())
t_seeds = {"북동 길": (r * 0.78, -r * 0.25), "남동 길": (r * 0.78, r * 0.25),
           "북쪽 내륙길": (r * 0.40, -r * 0.33), "남쪽 내륙길": (r * 0.40, r * 0.33)}
q = tot // 4
t_target = {"북동 길": q, "남동 길": q, "북쪽 내륙길": q, "남쪽 내륙길": tot - 3 * q}
ez.update(partition(rest, t_seeds, t_target))

def poly(mask, eps=1.2):
    cs, _ = cv2.findContours(mask.astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    out = []
    for c in cs:
        if cv2.contourArea(c) < 20: continue
        a = cv2.approxPolyDP(c, eps, True)[:, 0, :]
        out.append([[int(p[0]) + X0, int(p[1]) + Y0] for p in a])
    return out
def info(m, landm):
    return {"cells": int(m.sum()), "land": int((m & landm).sum()), "sea": int((m & ~landm).sum()),
            "poly": poly(m), "land_poly": poly(m & landm), "centroid": [round(float(xs[m].mean()), 1), round(float(ys[m].mean()), 1)],
            "land_centroid": [round(float(xs[m & landm].mean()), 1), round(float(ys[m & landm].mean()), 1)]}

border = gl & (np.roll(cl, 1, 1) | np.roll(cl, 1, 0) | np.roll(cl, -1, 0))
touch = {k: bool((np.roll(cl, 1, 1) & v).sum() > 0) for k, v in gz.items()}
res = {"COAST": COAST, "R_land": r, "goryeo_scale": s, "wedge_half_deg": half,
       "continent": {"cells": int(creg.sum()), "land": int(cl.sum()), "sea": int((creg & ~cl).sum())},
       "goryeo_total": {"cells": int(greg.sum()), "land": int(gl.sum()), "sea": int((greg & ~gl).sum())},
       "wedge_total": {"cells": int(wedge.sum()), "land": int((wedge & cl).sum()), "sea": int((wedge & ~cl).sum())},
       "border_cells": int(border.sum()), "touches_continent": touch,
       "goryeo": {k: info(v, gl) for k, v in gz.items()},
       "east": {k: info(v, cl) for k, v in ez.items()},
       "villages": {k: [round(v[0], 1), round(v[1], 1)] for k, v in V.items()},
       "goryeo_land_poly": poly(gl), "continent_region_poly": poly(creg), "goryeo_region_poly": poly(greg)}
vs = list(V)
res["spacing"] = {f"{a}-{b}": round(math.dist(V[a], V[b]), 1) for i, a in enumerate(vs) for b in vs[i + 1:] if math.dist(V[a], V[b]) < 140}
json.dump(res, open(sys.argv[1] if len(sys.argv) > 1 else "layout_proposal_d.json", "w"), ensure_ascii=False)
print({k: res[k] for k in ("R_land", "goryeo_scale", "wedge_half_deg", "continent", "goryeo_total", "wedge_total", "border_cells", "touches_continent")})
print({k: (v["cells"], v["land"], v["sea"]) for k, v in res["goryeo"].items()})
print({k: (v["cells"], v["land"], v["sea"]) for k, v in res["east"].items()})
print(res["spacing"])
