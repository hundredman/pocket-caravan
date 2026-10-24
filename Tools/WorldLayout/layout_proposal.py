"""고려 + 중앙 대륙 동쪽 배치 검토안 A의 면적 계산 (Docs/WorldLayout.md 3절).

칸 단위로 계산한다. 대륙 중심 = (0,0), x 동쪽, y 남쪽.
사용: python layout_proposal.py [출력.json]
필요: numpy, opencv-python
검토안이므로 실제 지형 생성기의 원본이 아니다. 승인되면 작업 B1에서 정식 원본으로 옮긴다.
"""
import math, json, sys
import numpy as np, cv2

R = math.sqrt(100_000 / math.pi)
X0, Y0, X1, Y1 = -200, -200, 420, 200
W, H = X1 - X0, Y1 - Y0
ys, xs = np.mgrid[Y0:Y1, X0:X1] + 0.5
clock = (np.degrees(np.arctan2(xs, -ys)) + 360) % 360
cont = xs ** 2 + ys ** 2 <= R * R
wedge = cont & (clock >= 45) & (clock < 135)

def goryeo(s):
    cx, a, b, n = R + 58 * s, 95 * s, 98 * s, 2.3
    return (~cont) & ((np.abs((xs - cx) / a) ** n + np.abs(ys / b) ** n) <= 1)
lo, hi = 0.5, 1.5
for _ in range(40):
    m = (lo + hi) / 2
    if goryeo(m).sum() < 20_000: lo = m
    else: hi = m
gor = goryeo(hi)
# shared land border length (Goryeo cells touching continent)
border = gor & (np.roll(cont, 1, 1) | np.roll(cont, 1, 0) | np.roll(cont, -1, 0))
by = ys[border]
gx, gy = xs[gor].mean(), ys[gor].mean()

cap_c = (gx - 4, gy)
d = np.hypot(xs - cap_c[0], ys - cap_c[1])
dg = np.where(gor, d, np.inf).ravel()
cap = np.zeros_like(gor).ravel(); cap[np.argsort(dg)[:5000]] = True; cap = cap.reshape(gor.shape)
rest = gor & ~cap

names = ["닥목", "나루", "장단", "송악"]  # N E S W
seeds = {"닥목": (cap_c[0] + 4, cap_c[1] - 75), "나루": (cap_c[0] + 75, cap_c[1] + 4),
         "장단": (cap_c[0] - 4, cap_c[1] + 75), "송악": (cap_c[0] - 75, cap_c[1] - 4)}
w = {n: 0.0 for n in names}
for _ in range(200):
    dist = np.stack([np.hypot(xs - seeds[n][0], ys - seeds[n][1]) - w[n] for n in names])
    lab = np.argmin(dist, 0)
    cnt = {n: int((rest & (lab == i)).sum()) for i, n in enumerate(names)}
    if all(abs(c - 3750) <= 1 for c in cnt.values()): break
    for n in names: w[n] += (3750 - cnt[n]) * 0.003
zones = {"개경": cap}
for i, n in enumerate(names): zones[n] = rest & (lab == i)

def poly(mask, eps=1.2):
    cs, _ = cv2.findContours(mask.astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    out = []
    for c in cs:
        if cv2.contourArea(c) < 20: continue
        a = cv2.approxPolyDP(c, eps, True)[:, 0, :]
        out.append([[int(p[0]) + X0, int(p[1]) + Y0] for p in a])
    return out

def zone_centroid(m):
    return [float(xs[m].mean()), float(ys[m].mean())]

res = {
    "R": R, "counts": {k: int(v.sum()) for k, v in zones.items()},
    "continent": int(cont.sum()), "wedge": int(wedge.sum()), "goryeo": int(gor.sum()),
    "border_cells": int(border.sum()), "border_span": [float(by.min()), float(by.max())],
    "capital": [float(cap_c[0]), float(cap_c[1])],
    "gor_bbox": [float(xs[gor].min()), float(ys[gor].min()), float(xs[gor].max()), float(ys[gor].max())],
    "polys": {k: poly(v) for k, v in zones.items()},
    "centroids": {k: zone_centroid(v) for k, v in zones.items()},
}
json.dump(res, open(sys.argv[1] if len(sys.argv) > 1 else "layout_proposal.json", "w"), ensure_ascii=False)
print({k: res[k] for k in ("counts", "continent", "wedge", "goryeo", "border_cells", "border_span", "capital", "gor_bbox")})
print({k: [len(p) for p in v] for k, v in res["polys"].items()}, res["centroids"])
