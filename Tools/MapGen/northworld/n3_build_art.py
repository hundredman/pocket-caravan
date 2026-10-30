"""북유럽 N3 새 건물 그림 (코드로 그림, north_art.py와 같은 방식). 2026-10-29.
에셋 팩(Ninja Adventure) 북유럽 건물과 같은 색·외곽선: 외곽선 141b1b, 나무 a3754e/965340/61372e, 밝은 판 eecf9b,
초가 f38c4c/ffad5d, 돌 5f7160/8d977f/56634d/b6beb0, 불 d78b4a/f1c471/91522c.
후보마다 A·B 두 가지: 대장간 4x4, 숯가마 3x3, 동굴 입구 5x3, 얄의 장원 8x6 (칸=16px).
make(out_dir) → 각 후보 PNG와 비교판."""
import os, sys, math, random
from PIL import Image, ImageDraw, ImageFont
H_ = lambda s: tuple(int(s[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
OL = H_('141b1b'); W1, W2, W3, WL = H_('a3754e'), H_('965340'), H_('61372e'), H_('eecf9b')
T1, T2, T3 = H_('ffad5d'), H_('f38c4c'), H_('d9733a')
S1, S2, S3, S4 = H_('b6beb0'), H_('8d977f'), H_('5f7160'), H_('56634d')
F1, F2, F3 = H_('f1c471'), H_('d78b4a'), H_('91522c')
SN1, SN2, SN3 = H_('ffffff'), H_('e6ecf2'), H_('b8c6d6')
IC1, IC2, IC3 = H_('d8f4ff'), H_('8fd0ec'), H_('4f8fb8')
DK = H_('2a2420'); SM = (150, 150, 150, 170); RED = H_('b8423a'); GOLD = H_('e8b44a')
TURF1, TURF2 = H_('7f9a52'), H_('5e7a3e')

def canvas(w, h): return Image.new('RGBA', (w, h), (0, 0, 0, 0))
def outline(im):
    """불투명 칸 바깥 1px에 외곽선(에셋 팩처럼 굵은 테두리)"""
    a = im.split()[3]; px = im.load(); W, H = im.size; edge = []
    for y in range(H):
        for x in range(W):
            if a.getpixel((x, y)) == 0:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H and a.getpixel((nx, ny)) > 0: edge.append((x, y)); break
    for x, y in edge: px[x, y] = OL
    return im
def inner_edge(im, col=OL):
    """그림 가장자리 칸을 외곽선 색으로(캔버스를 꽉 채운 그림용)"""
    a = im.split()[3]; px = im.load(); W, H = im.size
    pts = [(x, y) for y in range(H) for x in range(W) if a.getpixel((x, y)) > 0 and any(
        not (0 <= x + dx < W and 0 <= y + dy < H) or a.getpixel((x + dx, y + dy)) == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p in pts: px[p] = col
    return im
def planks_v(d, x0, y0, x1, y1, base=W1, line=W2, step=4, rng=None):
    d.rectangle((x0, y0, x1, y1), fill=base)
    for x in range(x0 + step, x1, step): d.line((x, y0, x, y1), fill=line)
    if rng:
        for _ in range((x1 - x0) * (y1 - y0) // 60):
            xx, yy = rng.randint(x0, x1), rng.randint(y0, y1); d.point((xx, yy), fill=W3)
def planks_h(d, x0, y0, x1, y1, base=W1, line=W2, step=3):
    d.rectangle((x0, y0, x1, y1), fill=base)
    for y in range(y0 + step, y1, step): d.line((x0, y, x1, y), fill=line)
def door(d, cx, y1, w=8, h=10, col=DK):
    d.rectangle((cx - w // 2 - 1, y1 - h - 1, cx + w // 2, y1), fill=W3)
    d.rectangle((cx - w // 2, y1 - h, cx + w // 2 - 1, y1), fill=col)
def stones(d, x0, y0, x1, y1, rng, big=4):
    d.rectangle((x0, y0, x1, y1), fill=S3)
    y = y0; row = 0
    while y <= y1:
        x = x0 - (row % 2) * big // 2
        while x <= x1:
            w = big + rng.randint(-1, 1)
            d.rectangle((max(x0, x), y, min(x1, x + w - 1), min(y1, y + big - 2)), fill=rng.choice([S2, S2, S1, S3]))
            x += w + 1
        y += big; row += 1
def snowcap(d, pts, rng, thick=3):
    for (x, y) in pts:
        h = thick + rng.randint(-1, 1)
        d.line((x, y - 1, x, y + h - 2), fill=SN1); d.point((x, y + h - 1), fill=SN3)
def smoke(d, x, y, n=3):
    for i in range(n):
        r = 2 + i; d.ellipse((x - r + i, y - 5 * i - r, x + r + i, y - 5 * i + r), fill=SM)

# ── 대장간 4x4 (64x64) ──
def smithy_a():
    """A: 앞이 트인 판자 지붕 대장간. 안쪽 돌 화덕이 빛나고 앞에 모루"""
    rng = random.Random(1); im = canvas(64, 64); d = ImageDraw.Draw(im)
    # 지붕(박공 판자, 에셋 h_dark처럼)
    d.polygon([(4, 22), (32, 4), (60, 22), (60, 30), (4, 30)], fill=W1)
    for i in range(6): d.line((4 + i * 5, 30, 32, 6), fill=W2)
    for i in range(6): d.line((60 - i * 5, 30, 32, 6), fill=W2)
    d.line((4, 22, 32, 4), fill=W3); d.line((60, 22, 32, 4), fill=W3); d.rectangle((3, 29, 61, 31), fill=W3)
    # 굴뚝 + 연기
    d.rectangle((44, 6, 50, 18), fill=S2); d.rectangle((44, 6, 50, 7), fill=S1); smoke(d, 47, 2, 2)
    # 기둥 둘, 안쪽 어두움
    d.rectangle((6, 32, 58, 58), fill=DK)
    d.rectangle((6, 32, 9, 58), fill=W2); d.rectangle((55, 32, 58, 58), fill=W2)
    # 안쪽 돌 화덕(빛)
    stones(d, 12, 38, 30, 54, rng, 4); d.rectangle((15, 44, 27, 52), fill=F3); d.rectangle((17, 46, 25, 52), fill=F2); d.rectangle((19, 48, 23, 52), fill=F1)
    # 연장 걸이
    for x in (36, 40, 44): d.line((x, 36, x, 44), fill=S1); d.point((x, 45), fill=S2)
    d.line((34, 35, 48, 35), fill=W2)
    # 바닥 판, 모루
    d.rectangle((6, 58, 58, 61), fill=W2)
    d.rectangle((38, 50, 52, 53), fill=S3); d.rectangle((36, 49, 54, 50), fill=S2); d.rectangle((42, 54, 48, 58), fill=S4); d.rectangle((39, 58, 51, 59), fill=S3)
    d.point((40, 49), fill=S1); d.point((41, 49), fill=S1)
    return outline(im)
def smithy_b():
    """B: 돌벽에 흙(잔디) 지붕 대장간. 옆 굴뚝 연기, 문 옆 모루와 물통"""
    rng = random.Random(2); im = canvas(64, 64); d = ImageDraw.Draw(im)
    d.polygon([(2, 26), (14, 8), (50, 8), (62, 26)], fill=TURF1)
    for _ in range(80): d.point((rng.randint(8, 56), rng.randint(10, 25)), fill=TURF2)
    d.line((2, 26, 14, 8), fill=TURF2); d.line((62, 26, 50, 8), fill=TURF2); snowcap(d, [(x, 9) for x in range(16, 49)], rng, 2)
    stones(d, 4, 27, 60, 58, rng, 5)
    d.rectangle((4, 27, 60, 28), fill=S4)
    door(d, 24, 58, 10, 14)
    d.rectangle((19, 41, 29, 43), fill=W3)
    # 빛나는 창(화덕 불)
    d.rectangle((40, 36, 50, 44), fill=F2); d.rectangle((42, 38, 48, 44), fill=F1); d.rectangle((44, 36, 46, 44), fill=W3)
    # 굴뚝
    d.rectangle((52, 2, 58, 22), fill=S2); d.rectangle((52, 2, 58, 3), fill=S1); smoke(d, 55, -1, 2)
    # 모루, 물통
    d.rectangle((34, 54, 44, 56), fill=S2); d.rectangle((37, 57, 41, 60), fill=S4)
    d.ellipse((47, 53, 55, 60), fill=W2); d.ellipse((48, 54, 54, 56), fill=IC2)
    return outline(im)

# ── 숯가마 3x3 (48x48) ──
def kiln_a():
    """A: 흙 무덤 숯가마. 잔디 덮인 둥근 흙더미, 꼭대기 연기 구멍, 아래 아궁이 불빛, 옆 장작"""
    rng = random.Random(3); im = canvas(48, 48); d = ImageDraw.Draw(im)
    d.ellipse((3, 10, 45, 46), fill=H_('8a6a48'))
    d.ellipse((6, 10, 42, 34), fill=H_('9c7a52'))
    for _ in range(60):
        x, y = rng.randint(6, 42), rng.randint(12, 40)
        if (x - 24) ** 2 / 400 + (y - 28) ** 2 / 300 < 1: d.point((x, y), fill=rng.choice([TURF1, TURF2, H_('7a5c3e')]))
    d.ellipse((20, 10, 28, 15), fill=DK); smoke(d, 24, 6, 3)
    d.pieslice((17, 34, 31, 50), 180, 360, fill=DK); d.rectangle((20, 40, 28, 43), fill=F2); d.rectangle((22, 41, 26, 43), fill=F1)
    for i, (x, y) in enumerate([(36, 40), (40, 42), (34, 43)]): d.rectangle((x, y, x + 8, y + 2), fill=W1); d.point((x + 8, y + 1), fill=WL)
    return outline(im)
def kiln_b():
    """B: 돌로 쌓은 벌집 모양 숯가마. 아치 아궁이에 불, 꼭대기 연기"""
    rng = random.Random(4); im = canvas(48, 48); d = ImageDraw.Draw(im)
    d.pieslice((4, 6, 44, 70), 180, 360, fill=S3)
    for y in range(8, 40, 4):
        for x in range(4 + (y // 4 % 2) * 2, 44, 5):
            if (x + 2 - 24) ** 2 / 400 + (y + 1 - 38) ** 2 / 1024 < 1: d.rectangle((x, y, x + 3, y + 2), fill=rng.choice([S2, S2, S1]))
    d.rectangle((4, 38, 44, 44), fill=S3)
    for x in range(5, 44, 6): d.rectangle((x, 39, x + 4, 43), fill=S2)
    d.rectangle((21, 4, 27, 8), fill=S2); smoke(d, 24, 1, 2)
    d.pieslice((15, 28, 33, 52), 180, 360, fill=DK); d.rectangle((15, 40, 33, 44), fill=DK)
    d.rectangle((18, 37, 30, 44), fill=F3); d.rectangle((20, 39, 28, 44), fill=F2); d.rectangle((22, 41, 26, 44), fill=F1)
    return outline(im)

# ── 동굴 입구 5x3 (80x48) ──
def cave_a():
    """A: 회색 바위벽에 어두운 동굴 아치, 위에 눈, 고드름"""
    rng = random.Random(5); im = canvas(80, 48); d = ImageDraw.Draw(im)
    d.polygon([(0, 47), (0, 14), (8, 6), (20, 2), (40, 0), (60, 2), (72, 6), (79, 14), (79, 47)], fill=S3)
    for _ in range(14):
        x, y = rng.randint(4, 70), rng.randint(6, 40); w, h = rng.randint(6, 14), rng.randint(4, 8)
        d.rectangle((x, y, x + w, y + h), fill=rng.choice([S2, S2, S4, S1]))
    d.pieslice((22, 14, 58, 74), 180, 360, fill=DK); d.rectangle((22, 44, 58, 47), fill=DK)
    d.arc((21, 13, 59, 75), 180, 360, fill=S4, width=2)
    for x in range(26, 56, 4): d.polygon([(x, 15 + abs(x - 40) // 3), (x + 2, 15 + abs(x - 40) // 3), (x + 1, 20 + abs(x - 40) // 3 + rng.randint(0, 3))], fill=IC1)
    snowcap(d, [(x, int(14 - 12 * math.sin(math.pi * x / 79)) + 1) for x in range(2, 78)], rng, 3)
    d.rectangle((26, 44, 54, 47), fill=H_('3a3a44'))
    return outline(im)
def cave_b():
    """B: 푸른 얼음 동굴. 얼음 결정이 둘러싼 입구, 안쪽 푸른 빛"""
    rng = random.Random(6); im = canvas(80, 48); d = ImageDraw.Draw(im)
    d.polygon([(0, 47), (2, 16), (12, 6), (28, 2), (52, 2), (68, 6), (78, 16), (79, 47)], fill=S3)
    for _ in range(12):
        x, y = rng.randint(2, 70), rng.randint(6, 40); d.rectangle((x, y, x + rng.randint(5, 12), y + rng.randint(3, 7)), fill=rng.choice([S2, S4]))
    for i in range(9):
        x = 18 + i * 5; h = 12 + rng.randint(0, 8)
        d.polygon([(x, 30), (x + 3, 30 - h), (x + 6, 30)], fill=rng.choice([IC1, IC2]))
        d.line((x + 3, 30 - h, x + 3, 29), fill=IC1)
    d.pieslice((24, 18, 56, 74), 180, 360, fill=IC3); d.pieslice((28, 22, 52, 74), 180, 360, fill=H_('1f3a52'))
    d.rectangle((24, 44, 56, 47), fill=H_('1f3a52'))
    for p in ((34, 34), (45, 30), (40, 40)): d.point(p, fill=IC1)
    snowcap(d, [(x, int(16 - 13 * math.sin(math.pi * x / 79)) + 1) for x in range(3, 77)], rng, 3)
    return outline(im)

# ── 얄의 장원 8x6 (128x96) ──
def hall_a():
    """A: 긴 초가 지붕 롱하우스(옆모습). 양 끝 박공에 용머리, 가운데 큰 문, 벽에 방패"""
    rng = random.Random(7); im = canvas(128, 96); d = ImageDraw.Draw(im)
    # 지붕(긴 초가, 위가 살짝 휜 배 모양)
    top = [(x, int(14 + 6 * ((x - 64) / 64) ** 2 * -1 + 6)) for x in range(6, 123)]
    d.polygon([(2, 56), (8, 20)] + [(x, y) for x, y in top] + [(120, 20), (126, 56)], fill=T2)
    for i in range(40):
        x = rng.randint(10, 118); y = rng.randint(22, 52); d.line((x, y, x + rng.choice([-2, 2]), y + 4), fill=T1)
    for y in range(26, 56, 6): d.line((6, y, 122, y), fill=T3)
    d.line((8, 20, 120, 20), fill=T3)
    # 용머리(양 끝)
    for sx, dirn in ((8, -1), (120, 1)):
        d.line((sx, 20, sx + 4 * dirn, 10), fill=W3, width=3); d.polygon([(sx + 4 * dirn, 8), (sx + 9 * dirn, 10), (sx + 4 * dirn, 13)], fill=W2)
        d.point((sx + 5 * dirn, 10), fill=GOLD)
    snowcap(d, [(x, y + 1) for x, y in top[4:-4:1]], rng, 3)
    # 벽(세로 판자)
    planks_v(d, 6, 57, 122, 92, W1, W2, 4, rng); d.rectangle((6, 57, 122, 59), fill=W3); d.rectangle((6, 90, 122, 92), fill=W3)
    # 큰 문 + 문 위 장식
    door(d, 64, 92, 16, 22); d.rectangle((55, 67, 73, 69), fill=W3); d.polygon([(58, 66), (64, 61), (70, 66)], fill=GOLD)
    # 방패 넷
    for cx in (22, 40, 88, 106):
        col = RED if cx in (22, 88) else H_('3c6a9a')
        d.ellipse((cx - 6, 66, cx + 6, 78), fill=col); d.ellipse((cx - 2, 70, cx + 2, 74), fill=S1); d.line((cx - 6, 72, cx + 6, 72), fill=WL)
    # 창
    for cx in (30, 98): d.rectangle((cx - 3, 80, cx + 3, 85), fill=WL); d.line((cx, 80, cx, 85), fill=W3)
    return outline(im)
def hall_b():
    """B: 판자 지붕 큰 회관(앞모습 박공). 높은 박공에 엇갈린 용머리, 양 옆 낮은 날개, 가운데 큰 문"""
    rng = random.Random(8); im = canvas(128, 96); d = ImageDraw.Draw(im)
    # 양 날개(낮은 지붕)
    for x0, x1 in ((2, 40), (88, 126)):
        d.polygon([(x0, 50), (x0 + 6, 34), (x1 - 6, 34), (x1, 50)], fill=W2)
        for x in range(x0 + 4, x1 - 2, 4): d.line((x, 36, x, 49), fill=W3)
        snowcap(d, [(x, 35) for x in range(x0 + 7, x1 - 6)], rng, 2)
        planks_v(d, x0 + 2, 51, x1 - 2, 92, W1, W2, 4, rng); d.rectangle((x0 + 2, 51, x1 - 2, 52), fill=W3)
        cx = (x0 + x1) // 2; d.rectangle((cx - 4, 64, cx + 4, 71), fill=WL); d.line((cx, 64, cx, 71), fill=W3)
    # 가운데 높은 박공
    d.polygon([(30, 58), (64, 6), (98, 58)], fill=W1)
    for i in range(1, 9): d.line((64, 8, 30 + i * 8, 58), fill=W2)
    d.line((30, 58, 64, 6), fill=W3, width=2); d.line((98, 58, 64, 6), fill=W3, width=2)
    # 엇갈린 용머리(박공 꼭대기)
    d.line((64, 8, 56, 0), fill=W3, width=3); d.line((64, 8, 72, 0), fill=W3, width=3)
    d.polygon([(52, 0), (56, 4), (58, 0)], fill=GOLD); d.polygon([(76, 0), (72, 4), (70, 0)], fill=GOLD)
    snowcap(d, [(x, int(58 - (52 * (1 - abs(x - 64) / 34)))) for x in range(34, 95)], rng, 2)
    planks_v(d, 36, 58, 92, 92, W1, W2, 4, rng); d.rectangle((36, 58, 92, 60), fill=W3)
    door(d, 64, 92, 18, 24); d.rectangle((54, 64, 74, 66), fill=GOLD)
    for cx in (44, 84): d.ellipse((cx - 5, 68, cx + 5, 78), fill=RED); d.ellipse((cx - 2, 71, cx + 2, 75), fill=S1)
    d.rectangle((2, 92, 126, 94), fill=S3)
    return outline(im)

ITEMS = [('smithy', '대장간 4×4', smithy_a, smithy_b), ('kiln', '숯가마 3×3', kiln_a, kiln_b),
         ('cave', '동굴 입구 5×3', cave_a, cave_b), ('jarlhall', '얄의 장원 8×6', hall_a, hall_b)]

def make(out, font, refs=None):
    os.makedirs(out, exist_ok=True); S = 4; pics = []
    for key, label, fa, fb in ITEMS:
        a, b = fa(), fb(); a.save(f'{out}/{key}_A.png'); b.save(f'{out}/{key}_B.png'); pics.append((label, a, b))
    f = ImageFont.truetype(font, 26); fs = ImageFont.truetype(font, 18)
    rowh = [max(a.height, b.height) * S + 60 for _, a, b in pics]
    refw = 0; ref_ims = []
    if refs:
        ref_ims = [Image.open(p).convert('RGBA') for p in refs]; refw = sum(r.width * S + 20 for r in ref_ims)
    Wd = max(max(a.width * S + b.width * S + 120 for _, a, b in pics) + 260, refw + 40); Hh = sum(rowh) + (200 if refs else 0) + 20
    sheet = Image.new('RGBA', (Wd, Hh), (74, 96, 74, 255)); d = ImageDraw.Draw(sheet); y = 10
    for (label, a, b), rh in zip(pics, rowh):
        d.text((10, y + 10), label, fill='white', font=f)
        x = 260
        for tag, im in (('A', a), ('B', b)):
            d.text((x, y + 4), tag, fill=(255, 230, 140), font=f)
            sheet.alpha_composite(im.resize((im.width * S, im.height * S), Image.NEAREST), (x, y + 40)); x += im.width * S + 60
        y += rh
    if refs:
        d.text((10, y + 10), '비교: 지금 북유럽 건물(에셋 팩)', fill='white', font=fs); x = 10
        for r in ref_ims:
            r2 = r.resize((r.width * S // 2, r.height * S // 2), Image.NEAREST); sheet.alpha_composite(r2, (x, y + 40)); x += r2.width + 20
    sheet.save(f'{out}/candidates.png'); return f'{out}/candidates.png'

if __name__ == '__main__':
    print(make(sys.argv[1], sys.argv[2], sys.argv[3:] or None))
