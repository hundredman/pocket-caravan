"""배치안 B의 면적 계산 (Docs/WorldLayout.md 3절).

개경을 중서부로 치우치게 두고, 고려는 마을 맵 5개(크기 다름), 중앙 동쪽은 마을 3개 + 이동 구역 3개로 나눈다.
칸 단위. 대륙 중심 = (0,0), x 동쪽, y 남쪽.
사용: python layout_proposal_b.py [출력.json]
필요: numpy, opencv-python
검토안이므로 실제 지형 생성기의 원본이 아니다. 승인되면 작업 B1에서 정식 원본으로 옮긴다.
"""
import math, json, sys
import numpy as np, cv2

R = math.sqrt(100_000 / math.pi)
X0, Y0, X1, Y1 = -200, -200, 420, 200
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

def partition(region, seeds, targets, iters=600):
    names = list(seeds); w = {n: 0.0 for n in names}
    for _ in range(iters):
        dist = np.stack([np.hypot(xs - seeds[n][0], ys - seeds[n][1]) - w[n] for n in names])
        lab = np.argmin(dist, 0)
        cnt = {n: int((region & (lab == i)).sum()) for i, n in enumerate(names)}
        if all(abs(cnt[n] - targets[n]) <= 2 for n in names): break
        for n in names: w[n] += (targets[n] - cnt[n]) * 0.002
    return {n: region & (lab == i) for i, n in enumerate(names)}

# Goryeo: capital pulled to the west-centre river plain, backed by Songak hills to the north
g_seeds = {"개경": (206, 8), "송악": (214, -55), "나루": (222, 62), "장단": (275, 30), "닥목": (272, -42)}
g_target = {"개경": 5500, "송악": 3500, "나루": 3000, "장단": 4000, "닥목": 4000}
gz = partition(gor, g_seeds, g_target)

# Central east: three villages at a regular spacing + travel maps between them
V = {"동문 마을": (140, 2), "북동 마을": (76, -62), "남동 마을": (76, 64)}
wz = {}; taken = np.zeros_like(wedge)
for n, (vx, vy) in V.items():
    dd = np.where(wedge & ~taken, np.hypot(xs - vx, ys - vy), np.inf).ravel()
    m = np.zeros(wedge.size, bool); m[np.argsort(dd)[:3000]] = True; m = m.reshape(wedge.shape)
    wz[n] = m; taken |= m
t_seeds = {"북동 길": (128, -78), "남동 길": (128, 80), "내륙 길": (35, 0)}
t_target = {"북동 길": 5300, "남동 길": 5300, "내륙 길": int((wedge & ~taken).sum()) - 10600}
wz.update(partition(wedge & ~taken, t_seeds, t_target))

def poly(mask, eps=1.2):
    cs, _ = cv2.findContours(mask.astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    out = []
    for c in cs:
        if cv2.contourArea(c) < 20: continue
        a = cv2.approxPolyDP(c, eps, True)[:, 0, :]
        out.append([[int(p[0]) + X0, int(p[1]) + Y0] for p in a])
    return out
def cen(m): return [round(float(xs[m].mean()), 1), round(float(ys[m].mean()), 1)]

res = {"R": R, "goryeo_total": int(gor.sum()), "wedge_total": int(wedge.sum()),
       "goryeo": {k: {"cells": int(v.sum()), "poly": poly(v), "centroid": cen(v)} for k, v in gz.items()},
       "east": {k: {"cells": int(v.sum()), "poly": poly(v), "centroid": cen(v)} for k, v in wz.items()},
       "villages": V,
       "spacing": {f"{a}-{b}": round(math.dist(V[a], V[b]), 1) for a, b in [("동문 마을", "북동 마을"), ("동문 마을", "남동 마을"), ("북동 마을", "남동 마을")]}}
json.dump(res, open(sys.argv[1] if len(sys.argv) > 1 else "layout_b.json", "w"), ensure_ascii=False)
print(res["goryeo_total"], res["wedge_total"])
print({k: (v["cells"], v["centroid"], len(v["poly"])) for k, v in res["goryeo"].items()})
print({k: (v["cells"], v["centroid"], len(v["poly"])) for k, v in res["east"].items()})
print(res["spacing"])
