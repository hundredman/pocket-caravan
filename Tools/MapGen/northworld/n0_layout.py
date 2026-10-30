"""북유럽 N0 땅 모양 검토안 (Docs/WorldLayout.md 좌표 약속: 중앙 대륙 중심 0,0 · x 동 · y 남).
- 중앙 대륙 땅: 고려 원본 Tools/WorldLayout/world-layout.json의 landRuns (B1)
- 중앙 북쪽 담당: 12시 중심 부채꼴, 고려 동쪽 구역과 겹치지 않게. 육지 + 해안 10칸 앞바다 = 25,000칸이 되도록 각도를 고른다
- 북유럽 본토: 대륙 북쪽 해안에 좁은 고개(국경)로 붙은 피오르 반도. 육지 + 앞바다 = 20,000칸
- 맵 나누기: 마을·장소 씨앗에서 목표 면적만큼 땅을 고르게 넓혀 나눈다 (검토용 초안. N1에서 길·마을 기준으로 다시 다듬음)
본편 Maps/·Scenes/는 바꾸지 않는다. 결과: ArtReview/North-N0/"""
import json, math, sys, heapq
from pathlib import Path
import numpy as np, cv2
from PIL import Image, ImageDraw, ImageFont

SRC = Path(sys.argv[1]); OUT = Path(sys.argv[2]); FONT = sys.argv[3] if len(sys.argv) > 3 else None
OUT.mkdir(parents=True, exist_ok=True)
SEED = 28
COAST = 10
X0, Y0, X1, Y1 = -230, -430, 300, 200          # 배열 범위 (세계 좌표)
H, W = Y1 - Y0, X1 - X0
ys, xs = np.mgrid[Y0:Y1, X0:X1] + 0.5
clock = (np.degrees(np.arctan2(xs, -ys)) + 360) % 360

def runs(rows):
    m = np.zeros((H, W), bool)
    for y, a, b in rows:
        if Y0 <= y < Y1: m[y - Y0, max(a, X0) - X0:min(b, X1 - 1) - X0 + 1] = True
    return m
data = json.loads(SRC.read_text())
central_land = runs(data['landRuns'])
east = np.zeros((H, W), bool)
for mm in data['maps']:
    east |= runs(mm['runs'])
goryeo_side = east.copy()

def fractal(seed, scales=(40, 20, 10, 5), weights=(1, .55, .3, .15)):
    rng = np.random.default_rng(seed); out = np.zeros((H, W), np.float32)
    for s, w in zip(scales, weights):
        n = rng.standard_normal((H, W)).astype(np.float32)
        out += w * cv2.GaussianBlur(n, (0, 0), s / 2.5) / 1.0
    return out / out.std()

def smooth(mask, k=5):
    ker = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k))
    m = cv2.morphologyEx(mask.astype(np.uint8), cv2.MORPH_OPEN, ker)
    return cv2.morphologyEx(m, cv2.MORPH_CLOSE, ker).astype(bool)

def shelf(land):
    d = cv2.distanceTransform((~land).astype(np.uint8), cv2.DIST_L2, 5)
    return (~land) & (d <= COAST)

# ---------- 북유럽 반도 ----------
NOISE = fractal(SEED)
def peninsula(scale):
    # 목: 대륙 해안(y=-166)에서 y=-200까지, 반폭 13칸. 몸통: 그 위로 둥글게 넓어진다
    neck = (ys <= -160) & (ys > -202) & (np.abs(xs - 2 * np.sin((ys + 160) / 8)) <= 13 + 1.5 * NOISE)
    cy = -200 - 105 * scale                               # 몸통 중심
    ry, rx = 105 * scale, 92 * scale
    r = np.sqrt(((xs - 4) / rx) ** 2 + ((ys - cy) / ry) ** 2)
    body = r <= 1 + 0.07 * NOISE
    land = (neck | body)
    for (sx, sy, dx, dy, L, wdt) in FJORDS(scale, cy, rx):
        for k in range(int(L)):
            px, py = sx + dx * k, sy + dy * k + 3 * math.sin(k / 9)
            w = wdt * (1 - k / L * 0.6)
            land &= ~(((xs - px) ** 2 + (ys - py) ** 2) <= w * w)
    land = smooth(land, 5)
    k, lab, st, _ = cv2.connectedComponentsWithStats((~land).astype(np.uint8), connectivity=4)
    for j in range(1, k):                                # 작은 호수·구멍 메우기 (피오르는 바다와 이어져 있어 안 메워짐)
        if st[j, cv2.CC_STAT_AREA] < 400: land[lab == j] = True
    return land & ~central_land | (neck & (ys > -175) & ~central_land & (ys < -165))

def FJORDS(s, cy, rx):
    # 서쪽 큰 피오르(비르카 항구 쪽), 동쪽 피오르(피오르 나루), 북서·북동 작은 피오르
    return [(-rx - 8, cy + 48 * s, 1, -0.10, 0.70 * rx, 5),
            (rx + 8, cy + 30 * s, -1, -0.18, 0.62 * rx, 5),
            (-rx - 8, cy - 38 * s, 1, 0.14, 0.50 * rx, 4.5),
            (rx + 8, cy - 55 * s, -1, 0.12, 0.45 * rx, 4.5),
            (-30 * s, cy - 1.02 * 105 * s - 8, 0.15, 1, 0.35 * 105 * s, 4.5)]

def area_of(land):
    return int(land.sum() + (shelf(land) & ~central_land & ~shelf(central_land)).sum())

lo, hi = 0.4, 1.3
for _ in range(22):
    mid = (lo + hi) / 2
    if area_of(peninsula(mid)) < 20000: lo = mid
    else: hi = mid
SCALE = hi
north_land = peninsula(SCALE)
north_shelf = shelf(north_land) & ~central_land & ~shelf(central_land) & ~goryeo_side
# 정확히 20,000: 앞바다 가장 먼 칸부터 빼서 맞춘다
nl_area = int(north_land.sum() + north_shelf.sum())
if nl_area > 20000:
    d = cv2.distanceTransform((~north_land).astype(np.uint8), cv2.DIST_L2, 5)
    cand = np.argwhere(north_shelf); order = np.argsort(-d[north_shelf])
    for i in order[:nl_area - 20000]:
        y, x = cand[i]; north_shelf[y, x] = False
north = north_land | north_shelf

# ---------- 중앙 북쪽 부채꼴 ----------
cshelf = shelf(central_land) & ~north_land
def wedge(half):
    ang = ((clock + 180) % 360) - 180
    return (np.abs(ang) <= half) & (central_land | cshelf) & ~east & ~north
lo, hi = 20, 70
for _ in range(30):
    mid = (lo + hi) / 2
    if wedge(mid).sum() < 25000: lo = mid
    else: hi = mid
cnorth = wedge(hi)
extra = int(cnorth.sum()) - 25000
if extra > 0:                                           # 각도 끝 칸부터 빼서 정확히
    ang = np.abs(((clock + 180) % 360) - 180)
    cand = np.argwhere(cnorth); order = np.argsort(-ang[cnorth])
    for i in order[:extra]:
        y, x = cand[i]; cnorth[y, x] = False
HALF = hi

# ---------- 맵 씨앗 (세계 좌표) ----------
def W2(x, y): return (int(y - Y0), int(x - X0))
BY = -200 - 105 * SCALE; RY = 105 * SCALE; RX = 92 * SCALE        # 몸통 중심·반지름
def P(fx, fy): return (4 + fx * RX, BY + fy * RY)        # fx -1..1 서~동, fy -1(북)..1(남)
MAIN = [  # 키, 이름, 상대 위치, 목표 면적
    ('north_birka', '비르카 (수도)', (0.0, 0.62), 3800),
    ('north_herd', '순록 고원 마을', (-0.62, 0.15), 2700),
    ('north_hunter', '모피 사냥꾼 마을', (-0.55, 0.72), 2600),
    ('north_naru', '피오르 나루 마을', (0.58, 0.68), 2800),
    ('north_laugar', '라우가르 온천 마을', (0.0, 0.0), 2800),
    ('north_icecave', '얼음 동굴 골짜기', (0.6, 0.05), 2700),
    ('north_runehill', '룬돌 언덕', (0.0, -0.62), 2600),
]
CENTER = [
    ('north_c_gatetown', '북문 마을', (0, -150), 3200, 'town'),
    ('north_c_snowwood', '눈숲 마을', (-52, -108), 3200, 'town'),
    ('north_c_creek', '개울 나루 마을', (52, -108), 3200, 'town'),
    ('north_c_crossing', '북쪽 길목 마을', (0, -58), 3200, 'town'),
    ('north_c_road_nw', '눈숲 고갯길', (-40, -150), 2440, 'road'),
    ('north_c_road_ne', '개울 다리길', (42, -148), 2440, 'road'),
    ('north_c_runeway', '룬돌 큰길', (0, -104), 2440, 'road'),
    ('north_c_road_sw', '서남 솔숲길', (-36, -74), 2440, 'road'),
    ('north_c_road_se', '동남 물가길', (36, -74), 2440, 'road'),
]
seeds = []
for k, nm, rel, area in MAIN:
    x, y = P(*rel); seeds.append((k, nm, x, y, area, 'main'))
for k, nm, (x, y), area, kind in CENTER:
    seeds.append((k, nm, x, y, area, kind))

def grow(region, items):
    pts = np.argwhere(region); n = len(items)
    sx = np.array([W2(it[2], it[3])[1] for it in items], float); sy = np.array([W2(it[2], it[3])[0] for it in items], float)
    tgt = np.array([it[4] for it in items], float); tgt *= len(pts) / tgt.sum()
    D = (pts[:, 1:2] - sx[None]) ** 2 + (pts[:, 0:1] - sy[None]) ** 2
    w = np.zeros(n)
    step = 2.0
    for it in range(6000):
        a = np.argmin(D - w[None], axis=1)
        cnt = np.bincount(a, minlength=n)
        err = tgt - cnt
        if np.abs(err).max() <= 3: break
        w += err * step
        if it % 500 == 499: step *= 0.7
    print('partition', it, np.abs(err).max(), file=sys.stderr)
    owner = -np.ones((H, W), int); owner[pts[:, 0], pts[:, 1]] = a
    for _ in range(4):                                   # 떨어진 조각은 옆 맵으로
        for i in range(n):
            k, lab = cv2.connectedComponents((owner == i).astype(np.uint8), connectivity=4)
            if k <= 2: continue
            sizes = np.bincount(lab.ravel()); sizes[0] = 0; big = sizes.argmax()
            for j in range(1, k):
                if j == big: continue
                frag = lab == j
                ring = cv2.dilate(frag.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & ~frag & region
                nb = owner[ring]; nb = nb[(nb >= 0) & (nb != i)]
                if len(nb): owner[frag] = np.bincount(nb).argmax()
    return owner

main_items = [s for s in seeds if s[5] == 'main']
cen_items = [s for s in seeds if s[5] != 'main']
own_main = grow(north, main_items)
own_cen = grow(cnorth, cen_items)

# ---------- 그림 ----------
S = 3
col = np.zeros((H, W, 3), np.uint8); col[:] = (52, 98, 140)                      # 먼 바다
allshelf = shelf(central_land | north_land); col[allshelf] = (96, 156, 186)
col[central_land] = (198, 188, 160)
col[east & central_land] = (150, 178, 120)
col[north_land] = (232, 238, 244)
col[cnorth & central_land] = (214, 222, 206)
img = Image.fromarray(col).resize((W * S, H * S), Image.NEAREST); d = ImageDraw.Draw(img)
f = (lambda s: ImageFont.truetype(FONT, s)) if FONT else (lambda s: ImageFont.load_default())
def borders(owner, colr):
    e = np.zeros((H, W), bool)
    e[:, 1:] |= (owner[:, 1:] != owner[:, :-1]) & (owner[:, 1:] >= 0) & (owner[:, :-1] >= 0)
    e[1:, :] |= (owner[1:, :] != owner[:-1, :]) & (owner[1:, :] >= 0) & (owner[:-1, :] >= 0)
    for r, c in np.argwhere(e): d.rectangle([c * S, r * S, c * S + S - 1, r * S + S - 1], fill=colr)
borders(own_main, (90, 110, 150)); borders(own_cen, (120, 90, 150))
reg_e = np.zeros((H, W), bool)
for m_ in (north, cnorth):
    e = m_ & ~cv2.erode(m_.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
    for r, c in np.argwhere(e): d.rectangle([c * S, r * S, c * S + S - 1, r * S + S - 1], fill=(200, 60, 50))
report = dict(seed=SEED, coordinate='center (0,0), x east, y south (Docs/WorldLayout.md)', wedgeHalfAngle=round(HALF, 2),
              northMainland=dict(land=int(north_land.sum()), nearSea=int(north_shelf.sum()), total=int(north.sum())),
              centralNorth=dict(land=int((cnorth & central_land).sum()), nearSea=int((cnorth & ~central_land).sum()), total=int(cnorth.sum())),
              maps=[])
for items, owner, region in ((main_items, own_main, north), (cen_items, own_cen, cnorth)):
    for i, (k, nm, x, y, area, kind) in enumerate(items):
        m_ = owner == i
        if not m_.any(): continue
        rr, cc = np.argwhere(m_).mean(axis=0)
        land_n = int((m_ & (north_land | central_land)).sum())
        report['maps'].append(dict(key=k, name=nm, kind=kind, area=int(m_.sum()), land=land_n, seed=[round(x, 1), round(y, 1)]))
        cxp, cyp = (x - X0) * S, (y - Y0) * S
        d.ellipse([cxp - 7, cyp - 7, cxp + 7, cyp + 7], fill=(192, 57, 43) if kind != 'road' else (140, 110, 80), outline='white', width=2)
        lx, ly = cc * S, rr * S + 18
        txt = f"{nm}\n{int(m_.sum()):,}칸"
        d.multiline_text((lx, ly), txt, font=f(15), fill=(40, 30, 20), anchor='ma', align='center', stroke_width=3, stroke_fill=(255, 255, 255))
d.text((10, 8), f"북유럽 N0 땅 모양 검토안 · 본토 {int(north.sum()):,}칸(육지 {int(north_land.sum()):,}) · 중앙 북쪽 {int(cnorth.sum()):,}칸, 12시 ±{HALF:.1f}°",
       font=f(22), fill='white', stroke_width=3, stroke_fill=(30, 40, 60))
d.text((10, 40), "흰 땅 = 북유럽 본토(피오르 반도) · 연한 녹회색 = 중앙 북쪽 · 녹색 = 고려·중앙 동쪽(고려 원본) · 베이지 = 다른 지역 중앙 대륙 · 빨간 선 = 담당 경계",
       font=f(14), fill='white', stroke_width=2, stroke_fill=(30, 40, 60))
crop = img.crop((0, 0, (200 - X0) * S, (60 - Y0) * S))
crop.save(OUT / 'n0_layout.png')
(OUT / 'n0_report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
print(json.dumps(report, ensure_ascii=False, indent=1))
# N1이 읽는 칸 정보 (세계 좌표 원점: X0, Y0)
np.savez_compressed(OUT / 'n0_masks.npz', X0=X0, Y0=Y0, north_land=north_land, north=north, cnorth=cnorth,
                    central_land=central_land, east=east, own_main=own_main, own_cen=own_cen)
(OUT / 'n0_seeds.json').write_text(json.dumps([dict(key=k, name=nm, x=x, y=y, area=a, kind=kd) for k, nm, x, y, a, kd in seeds], ensure_ascii=False, indent=1))
