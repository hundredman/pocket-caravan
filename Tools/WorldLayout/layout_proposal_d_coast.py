"""배치안 D(확정)에 자연스러운 해안선을 입힌 초안.

둥근 도식 대신 여러 크기의 잡음으로 만·곶·작은 섬이 있는 해안을 만든다.
- 면적 규칙은 그대로: 고려 20,000 / 중앙 동쪽 25,000 (육지 + 해안 10칸 앞바다)
- 고려와 대륙은 넓은 면으로 붙어 있게 유지한다
- 다른 팀 구역(중앙 대륙의 나머지 3/4)의 해안선은 건드리지 않는다
사용: python layout_proposal_d_coast.py [출력.json] [seed]   (출력.json.land.npy에 육지 격자도 저장)
필요: numpy, opencv-python
"""
import math, json, sys
import numpy as np, cv2

COAST = 10
SEED = int(sys.argv[2]) if len(sys.argv) > 2 else 7
X0, Y0, X1, Y1 = -230, -230, 440, 230
H, W = Y1 - Y0, X1 - X0
ys, xs = np.mgrid[Y0:Y1, X0:X1] + 0.5
clock = (np.degrees(np.arctan2(xs, -ys)) + 360) % 360

def fractal(seed, scales=(48, 24, 12, 6), weights=(1.0, 0.55, 0.3, 0.15)):
    rng = np.random.default_rng(seed); out = np.zeros((H, W), np.float32)
    for s, w in zip(scales, weights):
        n = rng.standard_normal((H, W)).astype(np.float32)
        n = cv2.GaussianBlur(n, (0, 0), s / 2.5)
        out += w * n / (n.std() + 1e-6)
    return out / sum(weights)
N_G = fractal(SEED); N_C = fractal(SEED + 101)
N_F = fractal(SEED + 7, scales=(14, 7, 3.5), weights=(1.0, 0.6, 0.35))

def dist_to(mask): return cv2.distanceTransform((~mask).astype(np.uint8), cv2.DIST_L2, 5)
def bump(cx, cy, r): return np.exp(-((xs - cx) ** 2 + (ys - cy) ** 2) / (2 * r * r))

def cleanup(m, min_island=8):
    n, lab, st, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), 4)
    keep = np.zeros_like(m)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] >= min_island: keep |= lab == i
    # fill small inland holes (keep large ones as lakes)
    inv = (~keep).astype(np.uint8); n2, lab2, st2, _ = cv2.connectedComponentsWithStats(inv, 4)
    for i in range(1, n2):
        if st2[i, cv2.CC_STAT_AREA] < 40: keep |= lab2 == i
    return keep

# 고려 기본 윤곽: 대륙에 서쪽 면이 붙은 반도. (x는 대륙 해안에서 동쪽으로의 거리, y는 남쪽 +)
GORYEO_OUTLINE = [(-16, -56), (0, -62), (16, -71), (30, -64), (40, -70), (50, -82), (62, -76), (74, -80), (86, -88),
                  (100, -96), (112, -92), (110, -78), (102, -66), (114, -54), (126, -40), (128, -24), (116, -14), (124, -4),
                  (132, 10), (131, 28), (122, 44), (110, 52), (100, 48), (94, 58), (102, 72), (98, 86), (86, 90),
                  (76, 82), (68, 96), (58, 106), (48, 96), (42, 86), (32, 94), (22, 86), (14, 74), (2, 66), (-16, 58)]
# 남해안·동해안 앞 작은 섬 (기본 윤곽 좌표, 반지름)
ISLANDS = [(72, 108, 3.5), (36, 104, 3.0), (90, 100, 3.0), (110, 86, 2.5), (20, 96, 2.5), (140, 16, 2.5)]
def chaikin(pts, it=3):
    for _ in range(it):
        out = []
        for i in range(len(pts)):
            p, q = pts[i], pts[(i + 1) % len(pts)]
            out += [(0.75 * p[0] + 0.25 * q[0], 0.75 * p[1] + 0.25 * q[1]), (0.25 * p[0] + 0.75 * q[0], 0.25 * p[1] + 0.75 * q[1])]
        pts = out
    return pts
SMOOTH = chaikin(GORYEO_OUTLINE)
def sdf(mask):
    inside = cv2.distanceTransform(mask.astype(np.uint8), cv2.DIST_L2, 5)
    outside = cv2.distanceTransform((~mask).astype(np.uint8), cv2.DIST_L2, 5)
    return inside - outside
# 해안별 거칠기(칸): 남해안 리아스 > 서·북 > 동해안
def coast_amp(r, s):
    gx = (xs - r) / s
    amp = np.full((H, W), 3.5, np.float32)
    amp = np.where(ys > 40 * s, 5.5, amp)                    # 남해안: 복잡한 리아스식
    amp = np.where((gx > 105) & (np.abs(ys) < 50 * s), 1.8, amp)   # 동해안: 매끈
    return cv2.GaussianBlur(amp, (0, 0), 6)

def build(r, s, amp_c=0.11):
    wfade = np.clip(1 - np.abs(clock - 90) / 50, 0, 1) ** 0.7
    port_bays = 0.10 * (bump(r * 0.74, -r * 0.62, 10) + bump(r * 0.74, r * 0.62, 10))
    field_c = 1 - np.hypot(xs, ys) / r + wfade * (amp_c * N_C - port_bays)
    cl = cleanup(field_c > 0)
    base = np.zeros((H, W), np.uint8)
    cv2.fillPoly(base, [np.array([[r + x * s - X0, y * s - Y0] for x, y in SMOOTH], np.int32)], 1)
    for (ix, iy, ir) in ISLANDS:
        cv2.circle(base, (int(r + ix * s - X0), int(iy * s - Y0)), int(round(ir * s)), 1, -1)
    field_g = sdf(base.astype(bool)) + coast_amp(r, s) * (0.6 * N_G + 0.8 * N_F)
    gl = cleanup((field_g > 0) & ~cl)
    land = cl | gl
    dc, dg = dist_to(cl), dist_to(gl)
    band = (~land) & (np.minimum(dc, dg) <= COAST)
    return cl, gl, cl | (band & (dc <= dg)), gl | (band & (dg < dc))

r, s = 170.0, 1.0
for _ in range(5):
    lo, hi = 150.0, 190.0
    for _ in range(30):
        m = (lo + hi) / 2
        if build(m, s)[2].sum() < 100_000: lo = m
        else: hi = m
    r = hi
    lo, hi = 0.7, 1.4
    for _ in range(32):
        m = (lo + hi) / 2
        if build(r, m)[3].sum() < 20_000: lo = m
        else: hi = m
    s = hi
cl, gl, creg, greg = build(r, s)
# 노이즈 해안 때문에 남는 몇 칸은 앞바다 띠의 가장 바깥 칸부터 덜어 정확히 맞춘다
def trim(region, landm, target):
    extra = int(region.sum()) - target
    if extra <= 0: return region
    d = np.where(region & ~landm, dist_to(landm), -1).ravel()
    idx = np.argsort(-d)[:extra]; out = region.copy().ravel(); out[idx] = False
    return out.reshape(region.shape)
greg = trim(greg, gl, 20_000); creg = trim(creg, cl, 100_000)
lo, hi = 30.0, 60.0
for _ in range(40):
    h = (lo + hi) / 2
    if (creg & (np.abs(clock - 90) <= h)).sum() < 25_000: lo = h
    else: hi = h
half = hi; wedge = creg & (np.abs(clock - 90) <= half)

def partition(region, seeds, targets, iters=900):
    names = list(seeds); wt = {k: 0.0 for k in names}
    for _ in range(iters):
        dd = np.stack([np.hypot(xs - seeds[k][0], ys - seeds[k][1]) - wt[k] for k in names])
        lab = np.argmin(dd, 0)
        cnt = {k: int((region & (lab == i)).sum()) for i, k in enumerate(names)}
        if all(abs(cnt[k] - targets[k]) <= 3 for k in names): break
        for k in names: wt[k] += (targets[k] - cnt[k]) * 0.002
    return {k: region & (lab == i) for i, k in enumerate(names)}

gx = xs[gl]; gxmin, gxmax = float(gx.min()), float(gx.max())
g_seeds = {"송악": (gxmin + 22, -8), "닥목": (gxmin + 75, -62), "개경": (gxmax - 22, -12),
           "나루": (gxmax - 30, 52), "장단": (gxmin + 62, 52)}
g_target = {"개경": 5000, "장단": 4300, "나루": 4000, "닥목": 3400, "송악": 3300}
gz = partition(greg, g_seeds, g_target)

V = {"동문 마을": (r - 32, 0), "북동 포구": (r * 0.68, -r * 0.56), "남동 포구": (r * 0.68, r * 0.56), "중앙 길목 마을": (r * 0.30, 0)}
ez = {}; taken = np.zeros_like(wedge)
for k, (vx, vy) in V.items():
    dd = np.where(wedge & ~taken, np.hypot(xs - vx, ys - vy), np.inf).ravel()
    mm = np.zeros(wedge.size, bool); mm[np.argsort(dd)[:3200]] = True; mm = mm.reshape(wedge.shape)
    ez[k] = mm; taken |= mm
rest = wedge & ~taken; tot = int(rest.sum()); q = tot // 4
ez.update(partition(rest, {"북동 길": (r * 0.78, -r * 0.25), "남동 길": (r * 0.78, r * 0.25),
                           "북쪽 내륙길": (r * 0.40, -r * 0.33), "남쪽 내륙길": (r * 0.40, r * 0.33)},
                    {"북동 길": q, "남동 길": q, "북쪽 내륙길": q, "남쪽 내륙길": tot - 3 * q}))

def poly(mask, eps=0.9, min_area=6):
    cs, _ = cv2.findContours(mask.astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    out = []
    for c in cs:
        if cv2.contourArea(c) < min_area: continue
        a = cv2.approxPolyDP(c, eps, True)[:, 0, :]
        out.append([[int(p[0]) + X0, int(p[1]) + Y0] for p in a])
    return out
def info(m, landm):
    return {"cells": int(m.sum()), "land": int((m & landm).sum()), "sea": int((m & ~landm).sum()),
            "poly": poly(m, 1.2), "land_poly": poly(m & landm),
            "land_centroid": [round(float(xs[m & landm].mean()), 1), round(float(ys[m & landm].mean()), 1)]}
islands = cv2.connectedComponentsWithStats(gl.astype(np.uint8), 4)[0] - 2
border = gl & (np.roll(cl, 1, 1) | np.roll(cl, 1, 0) | np.roll(cl, -1, 0))
touch = {k: bool((np.roll(cl, 1, 1) & v).sum() > 0) for k, v in gz.items()}
res = {"outline_scale": s, "COAST": COAST, "seed": SEED, "R_land": r, "goryeo_scale": s, "wedge_half_deg": half,
       "continent": {"cells": int(creg.sum()), "land": int(cl.sum())},
       "goryeo_total": {"cells": int(greg.sum()), "land": int(gl.sum()), "sea": int((greg & ~gl).sum()), "islands": int(islands)},
       "wedge_total": {"cells": int(wedge.sum()), "land": int((wedge & cl).sum()), "sea": int((wedge & ~cl).sum())},
       "border_cells": int(border.sum()), "touches_continent": touch,
       "goryeo": {k: info(v, gl) for k, v in gz.items()}, "east": {k: info(v, cl) for k, v in ez.items()},
       "villages": {k: [round(v[0], 1), round(v[1], 1)] for k, v in V.items()},
       "goryeo_land_poly": poly(gl), "continent_land_poly": poly(cl, 0.9, 30),
       "continent_region_poly": poly(creg, 1.2, 30), "goryeo_region_poly": poly(greg, 1.2)}
json.dump(res, open(sys.argv[1] if len(sys.argv) > 1 else "layout_proposal_d_coast.json", "w"), ensure_ascii=False)
np.save((sys.argv[1] if len(sys.argv) > 1 else "layout_proposal_d_coast.json") + ".land.npy", (cl | gl))
print({k: res[k] for k in ("R_land", "goryeo_scale", "wedge_half_deg", "continent", "goryeo_total", "wedge_total", "border_cells", "touches_continent")})
print({k: (v["cells"], v["land"], v["sea"]) for k, v in res["goryeo"].items()})
print({k: (v["cells"], v["land"]) for k, v in res["east"].items()})
