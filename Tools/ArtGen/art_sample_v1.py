"""아트 시험 샘플 v1: 공통 팔레트 제안 + 타일·나무·초가집·너구리·사람 NPC, 외곽선 두 방식 비교."""
from PIL import Image, ImageDraw, ImageFont
import math, random

def hx(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

# ---------- 팔레트: 공통 32색 + 고려 강조 8색 ----------
BASE = {
    # 외곽선·어두운 색
    'ink': '#2a2024', 'shadow': '#43343a',
    # 흙·나무 (밝음 → 어두움)
    'earth1': '#f2e4c4', 'earth2': '#dcc08e', 'earth3': '#b8925e', 'earth4': '#86643e', 'earth5': '#5a4030',
    # 풀·잎
    'leaf1': '#d8e48a', 'leaf2': '#a6cc62', 'leaf3': '#6fa84e', 'leaf4': '#477c48', 'leaf5': '#2f5440',
    # 물·하늘
    'water1': '#dff2f2', 'water2': '#a6d8e2', 'water3': '#64aac8', 'water4': '#3e70a0', 'water5': '#2c4670',
    # 초가·짚·노랑
    'straw1': '#f8e6a8', 'straw2': '#e9c46e', 'straw3': '#c8984a', 'straw4': '#946a34',
    # 빨강·주황
    'red1': '#f4bca4', 'red2': '#dc7458', 'red3': '#a8463e', 'red4': '#6c2c30',
    # 피부
    'skin1': '#fbe2ca', 'skin2': '#e9b892', 'skin3': '#c48e6c',
    # 회색·돌
    'stone1': '#e2ded6', 'stone2': '#aca69e', 'stone3': '#726c6a',
    'white': '#fffaf0',
}
GORYEO = {  # 고려 강조색
    'dan_red': '#c4403a', 'dan_green': '#3f8c6c', 'dan_blue': '#2f5c8c', 'dan_yellow': '#e4b648',
    'celadon': '#a6ceb6', 'hanji': '#f6efdc', 'meok': '#262224', 'giwa': '#4a5664',
}
PAL = {**BASE, **GORYEO}
RGB = {k: hx(v) for k, v in PAL.items()}
assert len(BASE) == 32, len(BASE)

def nearest(c, keys=None):
    keys = keys or list(BASE)
    return min(keys, key=lambda k: sum((a - b) ** 2 for a, b in zip(RGB[k], c)))

# ---------- 스프라이트 = 이름 격자 ----------
class Spr:
    def __init__(s, w, h): s.w, s.h = w, h; s.g = [[None] * w for _ in range(h)]
    def set(s, x, y, k):
        if 0 <= x < s.w and 0 <= y < s.h: s.g[y][x] = k
    def get(s, x, y): return s.g[y][x] if 0 <= x < s.w and 0 <= y < s.h else None

def from_half(rows, key, w=16):
    s = Spr(w, len(rows))
    for y, r in enumerate(rows):
        r = r.ljust(w // 2, '.')
        full = r + r[::-1]
        for x, ch in enumerate(full):
            if ch != '.': s.set(x, y, key[ch])
    return s

DARKER = {}  # 방식 B: 같은 계열의 더 어두운 색
ramps = [['earth1', 'earth2', 'earth3', 'earth4', 'earth5'], ['leaf1', 'leaf2', 'leaf3', 'leaf4', 'leaf5'],
         ['water1', 'water2', 'water3', 'water4', 'water5'], ['straw1', 'straw2', 'straw3', 'straw4', 'earth5'],
         ['red1', 'red2', 'red3', 'red4', 'red4'], ['skin1', 'skin2', 'earth3', 'earth4'], ['stone1', 'stone2', 'stone3', 'shadow'],
         ['white', 'stone1', 'stone2', 'stone3'], ['hanji', 'earth1', 'earth3', 'earth4'], ['celadon', 'leaf3', 'leaf4'],
         ['dan_red', 'red3', 'red4'], ['dan_green', 'leaf4', 'leaf5'], ['dan_blue', 'water4', 'water5'], ['dan_yellow', 'straw3', 'straw4'],
         ['giwa', 'shadow', 'ink'], ['meok', 'ink'], ['shadow', 'ink'], ['ink', 'ink']]
for r in ramps:
    for i, k in enumerate(r):
        DARKER.setdefault(k, r[min(len(r) - 1, i + 3)] if r[0] not in ('ink', 'meok', 'shadow') else 'ink')

def outlined(s, mode):
    o = Spr(s.w, s.h); o.g = [row[:] for row in s.g]
    for y in range(s.h):
        for x in range(s.w):
            if s.get(x, y) is not None: continue
            nb = [s.get(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
            nb = [n for n in nb if n is not None]
            if nb: o.set(x, y, 'ink' if mode == 'A' else DARKER.get(nb[0], 'ink'))
    return o

def img(s):
    im = Image.new('RGBA', (s.w, s.h), (0, 0, 0, 0))
    for y in range(s.h):
        for x in range(s.w):
            k = s.g[y][x]
            if k: im.putpixel((x, y), RGB[k] + (255,))
    return im

# ---------- 너구리 플레이어 16x32 (2.5등신) ----------
RAC_KEY = {'m': 'stone2', 'l': 'stone1', 'd': 'stone3', 'k': 'shadow', 'w': 'white', 'e': 'ink', 'h': 'white',
           'v': 'dan_red', 'V': 'red3', 's': 'hanji', 'b': 'earth4', 'p': 'giwa', 'f': 'earth5', 'n': 'ink'}
RAC = ["........", "........",
       "..mm....", ".mdm....", ".mdmmmmm", ".mmmmmmm", "mmllllll", "mkkkklll",
       "mkkekkww", "mkkekkww", "mmkkkwww", ".mmwwwww", ".mmwwwwn", "..mmwwww", "...mmmmm",
       "....ssss", "...vvvss", "..vvvvss", ".mvvvVss", ".mvvvVss", ".mvvvVss", ".lvvvVss",
       "..bbbbbb", "..pppppp", "..pppppp", "..ppppp.", "..ppppp.", "..ppppp.", "..mmmmm.", ".ffffff.", ".ffffff.", "........"]
def raccoon():
    s = from_half(RAC, RAC_KEY)
    s.set(4, 8, 'white'); s.set(11, 8, 'ink'); s.set(10, 8, 'white')   # 눈 하이라이트를 둘 다 왼쪽 위로
    for i, y in enumerate(range(21, 28)):                                # 줄무늬 꼬리 (오른쪽 뒤)
        for x in (13, 14): s.set(x, y, 'shadow' if i % 2 else 'stone2')
    return s

# ---------- 사람 NPC 16x32 (고려 상인, 등신은 미정이라 같은 키로 비교용) ----------
NPC_KEY = {'H': 'meok', 'S': 'skin1', 'T': 'skin2', 'e': 'ink', 'c': 'hanji', 'C': 'earth2', 'r': 'dan_red',
           'n': 'dan_blue', 'w': 'white', 'f': 'earth5'}
NPC = ["........", ".......H", "......HH", "....HHHH", "...HHHHH", "..HHHHHH", "..HSSSSS", "..SSSSSS",
       "..SSeSSS", "..SSeSSS", "..TSSSSS", "...SSSSS", "....TTTT", "...ccccc", "..cccccc", ".ccccccr", ".cCccccr",
       ".cCccccc", ".cCccccc", ".SCccccc", "..rrrrrr", "..nnnnnn", "..nnnnnn", "..nnnnnn", "..nnnnnn", "..nnnnnn",
       "..nnnnn.", "..nnnnn.", "..wwwww.", ".ffffff.", ".ffffff.", "........"]
def npc(): return from_half(NPC, NPC_KEY)

# ---------- 소나무 32x48 ----------
def pine():
    s = Spr(32, 48); cx = 15.5
    for y in range(37, 46):
        for x in range(14, 18): s.set(x, y, 'earth4' if x < 16 else 'earth5')
    tiers = [(2, 14, 6), (9, 22, 9), (16, 30, 12), (24, 39, 14)]
    for top, bot, hw in tiers:
        for y in range(top, bot + 1):
            t = (y - top) / max(1, bot - top); half = 1 + t * hw
            for x in range(32):
                d = x + 0.5 - cx
                if abs(d) <= half:
                    edge = bot - y < 2
                    k = 'leaf3'
                    if d < -half * 0.35 and not edge: k = 'leaf2'
                    if d < -half * 0.75 and t < 0.6: k = 'leaf1'
                    if d > half * 0.4: k = 'leaf4'
                    if edge: k = 'leaf4' if d < half * 0.4 else 'leaf5'
                    s.set(x, y, k)
    return s

# ---------- 초가집 48x48 ----------
def house():
    s = Spr(48, 48)
    for y in range(43, 47):                       # 기단(돌)
        for x in range(4, 44): s.set(x, y, 'stone2' if y < 45 else 'stone3')
    for y in range(26, 43):                       # 흙벽
        for x in range(7, 41): s.set(x, y, 'earth1' if x < 30 else 'earth2')
    for x in (7, 17, 30, 40):                     # 기둥
        for y in range(26, 43): s.set(x, y, 'earth4'); s.set(x + 1 if x < 40 else x - 1, y, 'earth4')
    for x in range(7, 41): s.set(x, 26, 'earth4'); s.set(x, 27, 'earth4')
    for y in range(31, 43):                       # 문 (나무)
        for x in range(21, 27): s.set(x, y, 'earth3' if x < 24 else 'earth4')
    for y in range(31, 37):                       # 창 (한지 + 살)
        for x in range(10, 16):
            s.set(x, y, 'earth4' if (x - 10) % 3 == 0 or (y - 31) % 3 == 0 or x == 15 or y == 36 else 'hanji')
        for x in range(32, 38):
            s.set(x, y, 'earth4' if (x - 32) % 3 == 0 or (y - 31) % 3 == 0 or x == 37 or y == 36 else 'hanji')
    cx, top, bot = 23.5, 6, 28                    # 둥근 초가지붕
    for y in range(top, bot + 1):
        t = (y - top) / (bot - top)
        half = 22 * math.sqrt(max(0.0, 1 - (1 - t) ** 2 * 1.0)) if t < 1 else 22
        half = max(4, min(23, 6 + 17 * math.sin(min(1, t * 1.6) * math.pi / 2)))
        for x in range(48):
            d = x + 0.5 - cx
            if abs(d) <= half:
                k = 'straw2'
                if d < -half * 0.3 and t < 0.75: k = 'straw1'
                if d > half * 0.45: k = 'straw3'
                if (y - top) % 5 == 4 and abs(d) < half - 1: k = 'straw3' if k != 'straw3' else 'straw4'
                if bot - y < 2: k = 'straw4'
                s.set(x, y, k)
    for x in range(20, 28): s.set(x, top, 'straw4')   # 용마름(지붕 꼭대기)
    return s

# ---------- 타일 16x16 ----------
def tile(kind, seed):
    rnd = random.Random(seed); s = Spr(16, 16)
    base = {'grass': 'leaf2', 'dirt': 'earth2'}[kind]
    for y in range(16):
        for x in range(16): s.set(x, y, base)
    for _ in range(7):
        x, y = rnd.randrange(16), rnd.randrange(1, 16)
        if kind == 'grass':
            s.set(x, y, 'leaf3'); s.set(x, y - 1, 'leaf3'); s.set(x + 1, y, 'leaf3')
            if rnd.random() < 0.4: s.set(x + 1, y - 1, 'leaf1')
        else:
            s.set(x, y, 'earth3'); s.set(x + 1, y, 'earth1')
    return s

# ---------- 출력 ----------
FONT = '/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc'
def font(sz): return ImageFont.truetype(FONT, sz, index=1)

def zoom(im, k): return im.resize((im.width * k, im.height * k), Image.NEAREST)

def sheet(mode):
    items = [('바닥: 풀', tile('grass', 1)), ('바닥: 흙', tile('dirt', 2)), ('나무 32×48', pine()),
             ('초가집 48×48', house()), ('너구리 16×32', raccoon()), ('사람 NPC 16×32', npc())]
    Z = 5; pad = 28; W = sum(max(s.w * Z, 150) + pad for _, s in items) + pad; H = 48 * Z + 90
    out = Image.new('RGBA', (W, H), hx('#f3eee2') + (255,)); dr = ImageDraw.Draw(out); x = pad
    for name, s in items:
        im = zoom(img(outlined(s, mode) if 'tile' not in name and '바닥' not in name else s), Z)
        out.alpha_composite(im, (x, 60 + (48 * Z - im.height)))
        dr.text((x, 20), name, font=font(20), fill=hx('#2a2024')); x += max(im.width, 150) + pad
    return out

def scene(mode):
    W, H = 30, 17; sc = Image.new('RGBA', (W * 16, H * 16))
    g, d = img(tile('grass', 1)), img(tile('dirt', 2)); g2 = img(tile('grass', 7))
    for ty in range(H):
        for tx in range(W):
            sc.alpha_composite(d if ty in (10, 11) else (g if (tx * 7 + ty * 3) % 5 else g2), (tx * 16, ty * 16))
    P = lambda s: img(outlined(s, mode))
    sc.alpha_composite(P(house()), (8 * 16, 3 * 16 - 8))
    for (tx, ty) in [(2, 1), (4, 4), (19, 2), (22, 5), (25, 1), (14, 12), (26, 11)]:
        sc.alpha_composite(P(pine()), (tx * 16, ty * 16))
    sc.alpha_composite(P(raccoon()), (16 * 16, 9 * 16 - 4))
    sc.alpha_composite(P(npc()), (18 * 16 + 4, 9 * 16 - 4))
    return sc

def swatches():
    cols = 8; Z = 44; keys = list(BASE) + list(GORYEO)
    out = Image.new('RGBA', (cols * (Z + 96) + 24, 7 * (Z + 30) + 70), hx('#f3eee2') + (255,)); dr = ImageDraw.Draw(out)
    dr.text((24, 14), '공통 32색 (위 4줄)  +  고려 강조 8색 (아래 줄)', font=font(20), fill=hx('#2a2024'))
    for i, k in enumerate(keys):
        r = i // cols + (1 if i >= 32 else 0); c = i % cols
        x, y = 24 + c * (Z + 96), 56 + r * (Z + 30)
        dr.rectangle([x, y, x + Z, y + Z], fill=RGB[k], outline=hx('#2a2024'))
        dr.text((x + Z + 6, y + 4), k, font=font(13), fill=hx('#2a2024'))
        dr.text((x + Z + 6, y + 22), PAL[k], font=font(12), fill=hx('#6b5b4a'))
    return out

if __name__ == '__main__':
    for m in 'AB':
        sheet(m).save(f'sheet_{m}.png'); zoom(scene(m), 2).save(f'scene_{m}.png')
    swatches().save('palette.png')
    # 원본(1배) 저장
    for name, s in [('raccoon', raccoon()), ('npc', npc()), ('pine', pine()), ('house', house())]:
        img(outlined(s, 'A')).save(f'{name}_A_1x.png')
    print('ok')

def review():
    from PIL import ImageDraw
    parts=[('외곽선 A: 모두 같은 진한 색', sheet('A')),('외곽선 B: 물체 색보다 진한 같은 계열 색', sheet('B')),
           ('게임 화면 크기 (A, 2배 확대)', zoom(scene('A'),2)),('게임 화면 크기 (B, 2배 확대)', zoom(scene('B'),2)),('팔레트 제안', swatches())]
    W=max(p.width for _,p in parts)+48; H=sum(p.height+60 for _,p in parts)+90
    out=Image.new('RGBA',(W,H),hx('#fffdf6')+(255,)); dr=ImageDraw.Draw(out)
    dr.text((24,20),'아트 시험 샘플 v1 — 타일 16px · 모든 그림 같은 도트 크기',font=font(30),fill=hx('#2a2024'))
    y=90
    for t,p in parts:
        dr.text((24,y),t,font=font(22),fill=hx('#6b3a20')); out.alpha_composite(p,(24,y+40)); y+=p.height+60
    out.convert('RGB').save('review_v1.png')
review()
