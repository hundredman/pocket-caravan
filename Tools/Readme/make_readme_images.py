"""README 그림 만들기 (2026-11-08 윤채).

게임 속 그림(캐릭터·물건 아이콘·시작 화면 지도)과 글꼴(갈무리)로 README에 넣을 그림을 그린다.
색과 모양은 게임 UI(CuteUI 종이 카드)와 같게 맞췄다.

    python Tools/Readme/make_readme_images.py                 # 그림만 다시 그리기
    python Tools/Readme/make_readme_images.py --shots <폴더>   # 게임 캡처(1920x1080 PNG)도 줄여서 넣기

결과: .github/readme/
"""
import argparse
import os
import shutil

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, '.github', 'readme')
ART = os.path.join(ROOT, 'Assets', 'PocketCaravan', 'Art')
RES = os.path.join(ROOT, 'Assets', 'PocketCaravan', 'Resources')
FONT = os.path.join(RES, 'Fonts', 'Galmuri11.ttf')
FONT_SMALL = os.path.join(RES, 'Fonts', 'Galmuri9.ttf')


def hexc(v, a=255):
    return ((v >> 16) & 255, (v >> 8) & 255, v & 255, a)


# CuteUI 색 (역할)
INK, PAPER, PAPER2, LINE = hexc(0x2A1A2E), hexc(0xFFFAF2), hexc(0xFFF0DC), hexc(0xECD3B0)
FRAME, FRAME_HI, FRAME_LO, DECO = hexc(0xFF9A4D), hexc(0xFFC58F), hexc(0xE0702A), hexc(0xFFCF33)
SUB, TEAL, TEAL_LO, TEAL_HI, WHITE = hexc(0x6A5464), hexc(0x2E8C86), hexc(0x1F625E), hexc(0x6BBDB5), hexc(0xFFFFFF)
BACK_A, BACK_B = hexc(0xFFF0DC), hexc(0xFBE5C8)
SHADOW = (42, 26, 46, 70)

# CuteUI 픽셀 아이콘 (CuteUI.cs pics 그대로)
COIN_PAL = {'#': 0x6B3F10, 'h': 0xFFF3B0, 'y': 0xF7C843, 'd': 0xD08F22}
PICS = {
    'coin': (["..#####..", ".#hhyyy#.", "#hyyyyyd#", "#hy###yd#", "#yy#.#yd#", "#yy###dd#", "#yyyyydd#", ".#yyddd#.", "..#####.."], COIN_PAL),
    'compass': (["...#####...", ".##ppppp##.", ".#pppRppp#.", "#ppppRpppp#", "#pppRRRppp#", "#pppDoDppp#", "#pppDDDppp#", "#ppppDpppp#", ".#pppDppp#.", ".##ppppp##.", "...#####..."],
                {'#': 0x2A2A3A, 'p': 0xEEF6F4, 'R': 0xE04A3A, 'D': 0x34466A, 'o': 0xF7C843}),
    'sack': (["..#####..", "...#t#...", "..#bbb#..", ".#bhbbb#.", "#bhbbbbb#", "#bbbbbbd#", "#bbbbbdd#", ".#######."],
             {'#': 0x5A3216, 't': 0x8A5A2B, 'b': 0xE8B06C, 'h': 0xFBDCA8, 'd': 0xC08445}),
    'chart': (["......##.", "......##.", "...##.##.", "...##.##.", "##.##.##.", "##.##.##.", "#########"], {'#': 0x22A84A}),
    'star': (["...y...", "...y...", "..yWy..", "yyWWWyy", "..yWy..", "...y...", "...y..."], {'y': 0xFFCF33, 'W': 0xFFFFFF}),
    'arrow': (["....#....", "....##...", "#######..", "########.", "#######..", "....##...", "....#...."], {'#': 0x2A1A2E}),
    'people': (["..##...##..", ".#hh#.#hh#.", ".#hh#.#hh#.", "..##...##..", ".####.####.", "#hhhh#hhhh#", "#hhhh#hhhh#"], {'#': 0x2A1A2E, 'h': 0x6A5464}),
}

# 네 문명 (StartChoices.asset 순서·글과 같게)
REGIONS = [
    dict(key='north', region='북유럽', town='비르카', animal='순록', sprite='north_reindeer_tall', preview='start_north',
         item='north_fur', ground='snow'),
    dict(key='venice', region='베네치아', town='리알토', animal='흰 고양이', sprite='venice_cat_tall', preview='start_venice',
         item='venice_glass', ground='stone'),
    dict(key='goryeo', region='고려', town='예성 나루', animal='너구리', sprite='tanuki_tall', preview='start_goryeo',
         item='ginseng', ground='grass'),
    dict(key='arabia', region='아라비아', town='알 아흐사', animal='사막여우', sprite='arabia_player_fennec_tall', preview='start_arabia',
         item='arabia_carpet', ground='sand'),
]


def font(size, small=False):
    return ImageFont.truetype(FONT_SMALL if small else FONT, size)


def canvas(w, h):
    im = Image.new('RGBA', (w, h), PAPER)
    d = ImageDraw.Draw(im)
    d.fontmode = '1'   # 픽셀 글꼴: 흐림 없이
    return im, d


def checker(d, w, h, cell=8):
    for y in range(0, h, cell):
        for x in range(0, w, cell):
            d.rectangle([x, y, x + cell - 1, y + cell - 1], fill=BACK_A if (x // cell + y // cell) % 2 == 0 else BACK_B)


def box(d, r, radius, fill, line=INK, lw=1, shadow=0, shadow_col=SHADOW):
    x0, y0, x1, y1 = r
    if shadow:
        d.rounded_rectangle([x0, y0 + shadow, x1, y1 + shadow], radius, fill=shadow_col)
    d.rounded_rectangle([x0, y0, x1, y1], radius, fill=fill, outline=line if lw else None, width=lw)


def text_w(d, s, f):
    return d.textlength(s, font=f)


def label(d, x, y, s, f, col=INK):
    d.text((x, y), s, font=f, fill=col)


def label_center(d, cx, y, s, f, col=INK, shadow=None):
    x = round(cx - text_w(d, s, f) / 2)
    if shadow:
        d.text((x, y + shadow[0]), s, font=f, fill=shadow[1])
    d.text((x, y), s, font=f, fill=col)


def pic(im, name, x, y, scale=1):
    rows, pal = PICS[name]
    for yy, row in enumerate(rows):
        for xx, ch in enumerate(row):
            if ch in pal:
                c = hexc(pal[ch])
                for sy in range(scale):
                    for sx in range(scale):
                        im.putpixel((x + xx * scale + sx, y + yy * scale + sy), c)


def sprite(name):
    return Image.open(os.path.join(ART, 'Characters', name + '.png')).convert('RGBA')


def hero(region, frame='d0', unit=2):
    """캐릭터를 게임 속 키로 맞춰 키운다 (16x24는 unit배, 32x48 사막여우는 unit/2배)."""
    im = sprite(f"{region['sprite']}_{frame}")
    im = im.crop((0, 0, im.width, im.getbbox()[3]))   # 발 아래 빈칸 자르기 (사막여우 그림은 아래 8칸이 비어 있다)
    k = unit if im.width <= 16 else max(1, unit // 2)
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def item(name, scale=1):
    im = Image.open(os.path.join(ART, 'Items', name + '.png')).convert('RGBA')
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def upscale(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


def ground(d, kind, x0, y0, w, h, cell=2):
    """지역 바닥 (간단한 픽셀 무늬). cell = 게임 한 픽셀 크기"""
    import random
    rnd = random.Random(kind)
    base = {'snow': 0xF4F6FB, 'stone': 0xE9D9BE, 'grass': 0x8CC46A, 'sand': 0xF0D49A}[kind]
    dots = {'snow': [0xD8E2F2, 0xFFFFFF], 'stone': [0xD6C2A0, 0xF5E9D2], 'grass': [0x6FA552, 0xA9DA82], 'sand': [0xE0BE7C, 0xF8E3B6]}[kind]
    d.rectangle([x0, y0, x0 + w - 1, y0 + h - 1], fill=hexc(base))
    if kind == 'stone':   # 돌바닥 줄눈
        for yy in range(y0, y0 + h, 8 * cell // 2):
            d.line([x0, yy, x0 + w - 1, yy], fill=hexc(0xD6C2A0))
        for i, yy in enumerate(range(y0, y0 + h, 8 * cell // 2)):
            off = (i % 2) * 6
            for xx in range(x0 + off, x0 + w, 12):
                d.line([xx, yy, xx, min(y0 + h - 1, yy + 8 * cell // 2)], fill=hexc(0xD6C2A0))
        return
    for _ in range(w * h // 40):
        px = x0 + rnd.randrange(0, w // cell) * cell
        py = y0 + rnd.randrange(0, h // cell) * cell
        d.rectangle([px, py, px + cell - 1, py + cell - 1], fill=hexc(rnd.choice(dots)))
    if kind == 'grass':   # 풀포기
        for _ in range(w // 12):
            px = x0 + rnd.randrange(2, w - 6) // cell * cell
            py = y0 + rnd.randrange(2, h - 6) // cell * cell
            for dx, dy in ((0, 2), (cell, 0), (2 * cell, 2)):
                d.rectangle([px + dx, py + dy, px + dx + cell - 1, py + dy + cell + 1], fill=hexc(0x5E9446))


def bubble(im, d, cx, y, icon):
    """작은 종이 말풍선 + 물건 아이콘"""
    w, h = icon.width + 8, icon.height + 6
    x = round(cx - w / 2)
    box(d, [x, y, x + w, y + h], 4, PAPER, INK, 1, 1)
    d.polygon([(cx - 3, y + h), (cx + 3, y + h), (cx, y + h + 4)], fill=PAPER, outline=None)
    d.line([cx - 3, y + h + 1, cx, y + h + 4], fill=INK)
    d.line([cx + 3, y + h + 1, cx, y + h + 4], fill=INK)
    im.alpha_composite(icon, (x + 4, y + 3))


# ---------------- 1) 머리 그림 ----------------
def banner(S=3):
    W, H = 400, 168
    im, d = canvas(W, H)
    checker(d, W, H)
    big, f = font(24), font(12)
    # 제목 리본: 주황 판 + 양쪽 별
    title = '바리바리'
    tw = text_w(d, title, big)
    pw = round(tw + 70)
    px = (W - pw) // 2
    box(d, [px, 10, px + pw, 50], 8, FRAME, INK, 2, 3, INK)
    d.line([px + 6, 14, px + pw - 6, 14], fill=FRAME_HI)
    pic(im, 'star', px + 12, 27)
    pic(im, 'star', px + pw - 19, 27)
    label_center(d, W / 2, 16, title, big, WHITE, shadow=(2, INK))
    label_center(d, W / 2, 58, '짐을 바리바리 싸 들고 떠나는 2D 픽셀 교역 게임', f, INK)
    # 바닥 네 칸 + 캐릭터 + 물건 말풍선 + 이름 알약
    gy, seg = 124, W // 4
    for i, r in enumerate(REGIONS):
        x0 = i * seg
        ground(d, r['ground'], x0, gy, seg, H - gy)
        d.line([x0, gy, x0 + seg - 1, gy], fill=INK)
        if i:
            d.line([x0, gy, x0, H - 1], fill=INK)
        cx = x0 + seg // 2
        hm = hero(r)
        d.ellipse([cx - 11, gy + 8, cx + 11, gy + 13], fill=(42, 26, 46, 60))
        im.alpha_composite(hm, (cx - hm.width // 2, gy + 11 - hm.height))
        bubble(im, d, cx + 22, gy - 52, item(r['item']))
        name = r['region']
        nw = round(text_w(d, name, f) + 14)
        box(d, [cx - nw // 2, H - 22, cx - nw // 2 + nw, H - 6], 7, PAPER, INK, 1, 1)
        label_center(d, cx, H - 20, name, f, INK)
    d.rectangle([0, 0, W - 1, H - 1], outline=INK)
    upscale(im, S).save(os.path.join(OUT, 'banner.png'), optimize=True)


# ---------------- 2) 걷는 상인들 (움직이는 그림) ----------------
def walkers(S=3):
    W, H = 320, 92
    frames = []
    for fr in range(4):
        im, d = canvas(W, H)
        checker(d, W, H)
        f = font(12)
        seg = W // 4
        for i, r in enumerate(REGIONS):
            cx = i * seg + seg // 2
            box(d, [i * seg + 6, 6, (i + 1) * seg - 6, H - 8], 8, PAPER, INK, 1, 2)
            hm = hero(r, f'd{fr}')
            d.ellipse([cx - 10, 60, cx + 10, 65], fill=(42, 26, 46, 55))
            bob = 0
            im.alpha_composite(hm, (cx - hm.width // 2, 63 - hm.height + bob))
            label_center(d, cx, 68, r['animal'], f, INK)
        d.rectangle([0, 0, W - 1, H - 1], outline=INK)
        frames.append(upscale(im, S).convert('P', palette=Image.ADAPTIVE, colors=128))
    frames[0].save(os.path.join(OUT, 'merchants.gif'), save_all=True, append_images=frames[1:], duration=170, loop=0, disposal=2, optimize=True)


# ---------------- 3) 문명 카드 (시작 화면 카드를 크게) ----------------
def region_cards():
    f, big = font(24), font(24)
    for r in REGIONS:
        W, H = 760, 420
        im, d = canvas(W, H)
        d.rectangle([0, 0, W - 1, H - 1], fill=(0, 0, 0, 0))
        box(d, [4, 4, W - 6, H - 10], 18, FRAME, INK, 4, 6, INK)
        box(d, [16, 16, W - 18, H - 22], 12, PAPER, INK, 2)
        prev = Image.open(os.path.join(RES, 'StartScreen', r['preview'] + '.png')).convert('RGBA')
        prev = prev.resize((476, 362), Image.LANCZOS)
        im.alpha_composite(prev, (28, 28))
        d.rectangle([27, 27, 28 + 476, 28 + 362], outline=INK, width=2)
        # 주인공 칸
        cx0, cx1 = 28 + 476 + 14, W - 30
        box(d, [cx0, 28, cx1, 28 + 362], 10, PAPER2, LINE, 2)
        hm = hero(r, 'd0', unit=8)
        ccx = (cx0 + cx1) // 2
        d.ellipse([ccx - 40, 300, ccx + 40, 318], fill=(42, 26, 46, 50))
        im.alpha_composite(hm, (ccx - hm.width // 2, 310 - hm.height))
        label_center(d, ccx, 336, r['animal'], f, SUB)
        # 이름표 (청록 알약)
        name = f"{r['region']} · {r['town']}"
        nw = round(text_w(d, name, big) + 36)
        box(d, [40, 28 + 362 - 56, 40 + nw, 28 + 362 - 14], 18, TEAL, INK, 3, 3, TEAL_LO)
        label_center(d, 40 + nw / 2, 28 + 362 - 48, name, big, WHITE)
        im.quantize(256, method=Image.Quantize.FASTOCTREE).save(os.path.join(OUT, f"region_{r['key']}.png"), optimize=True)   # 모서리 투명 유지


# ---------------- 4) 놀이 흐름: 사고 → 고르고 → 나르고 → 판다 ----------------
def loop(S=3):
    W, H = 400, 84
    im, d = canvas(W, H)
    checker(d, W, H)
    f, fs = font(12), font(10, small=True)
    steps = [('coin', '사고', '싼 마을에서'), ('compass', '고르고', '큰길·지름길'), ('sack', '나르고', '직접 걸어서'), ('chart', '판다', '비싼 마을에')]
    cw, gap = 80, 22
    x = (W - (4 * cw + 3 * gap)) // 2
    for i, (p, head, sub) in enumerate(steps):
        on = i == 3
        box(d, [x, 12, x + cw, 72], 8, PAPER, TEAL if on else INK, 2 if on else 1, 2)
        rows, _ = PICS[p]
        pic(im, p, x + cw // 2 - len(rows[0]), 19, 2)
        label_center(d, x + cw / 2, 43, head, f, INK)
        label_center(d, x + cw / 2, 58, sub, fs, SUB)
        if i < 3:
            pic(im, 'arrow', x + cw + (gap - 9) // 2, 39)
        x += cw + gap
    d.rectangle([0, 0, W - 1, H - 1], outline=INK)
    upscale(im, S).save(os.path.join(OUT, 'loop.png'), optimize=True)


# ---------------- 4-1) 팀 표 얼굴 ----------------
def avatars(S=2):
    for r in REGIONS:
        W, H = 52, 60
        im, d = canvas(W, H)
        d.rectangle([0, 0, W - 1, H - 1], fill=(0, 0, 0, 0))
        box(d, [1, 1, W - 3, H - 4], 10, PAPER2, INK, 1, 2)
        ground(d, r['ground'], 3, 44, W - 7, 11)
        d.rounded_rectangle([1, 1, W - 3, H - 4], 10, outline=INK, width=1)
        hm = hero(r)
        im.alpha_composite(hm, ((W - 2) // 2 - hm.width // 2, 50 - hm.height))
        upscale(im, S).save(os.path.join(OUT, f"face_{r['key']}.png"), optimize=True)


# ---------------- 5) 게임 캡처 줄이기 ----------------
SHOTS = {
    '01_start.png': 'shot_start.png', '02_goryeo.png': 'shot_goryeo.png', '03_venice.png': 'shot_venice.png',
    '04_arabia.png': 'shot_arabia.png', '05_north.png': 'shot_north.png', '06_market.png': 'shot_market.png',
    '07_map.png': 'shot_map.png', '08_minigame.png': 'shot_minigame.png', '09_online.png': 'shot_online.png',
    '10_rank.png': 'shot_online_panel.png',
}


def shots(src):
    for a, b in SHOTS.items():
        p = os.path.join(src, a)
        if not os.path.exists(p):
            print('없음:', p)
            continue
        # 1920x1080 → 1280x720: 게임 화면은 정수배라 가까운 점 고르기(2/3)로 줄이면 픽셀이 흐려지지 않는다. 256색으로 크기를 줄인다
        im = Image.open(p).convert('RGB').resize((1280, 720), Image.NEAREST)
        im.quantize(256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).save(os.path.join(OUT, b), optimize=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--shots', help='게임 캡처 폴더 (01_start.png …)')
    a = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    banner()
    walkers()
    region_cards()
    loop()
    avatars()
    if a.shots:
        shots(a.shots)
    total = 0
    for fn in sorted(os.listdir(OUT)):
        sz = os.path.getsize(os.path.join(OUT, fn))
        total += sz
        print(f'{fn:28s} {sz // 1024:5d} KB')
    print(f'합계 {total // 1024} KB')


if __name__ == '__main__':
    main()
