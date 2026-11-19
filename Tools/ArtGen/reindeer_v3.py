"""11/19 은샘 순록 3판 (v1 "말에 뿔만 붙인 것 같다", v2 "비율·방향별 구도가 안 맞다" 다음).
실제 순록 생김새와 북유럽 순록 동물 그림(Resources/North/Critters/reindeer_0.png) 색을 따르고, 그리는 법은 짐말·낙타(pack_animals_v1)와 같다:
글자 칸 그림 + 부위마다 따로 두른 테두리(몸 · 뿔 · 앞 얼굴), 다리는 몸에 붙고 발굽은 다리 폭 그대로.
- 생김새: 머리를 낮게 앞으로 · 어깨가 솟고 등은 평평 · 목 아래 늘어진 흰 털 · 넓은 회색 주둥이 · 크고 흰 엉덩이와 짧은 꼬리 ·
  짧고 튼튼한 다리 · 발굽 위 흰 털 · 뿔은 뒤 위로 굵게 오르다 끝이 앞으로 굽고, 이마 앞으로 눈가지.
- 은샘 검토 반영: 앞 얼굴은 지난 시안처럼 크게(흰 수염털이 주둥이를 감싼다) · 발굽은 테두리보다 밝게(뾰족해 보이지 않게) ·
  옆모습 뒷다리는 흰 엉덩이 앞에서 끝나게 · 허리 살짝 길게.
- 크기: 옆 44x40 (짐말 40x32보다 조금 크다, 뿔 자리 포함), 앞·뒤 23x44 (짐말과 같다). 썰매 옆 72x42, 앞·뒤 30x58 (마차와 같다).
- 짐: 짐말과 같은 길마·짐 그림, 안장 천만 북유럽 파랑. 순록 썰매: 마차 짐칸을 썰매 날 위에 (reindeer_v1과 같다), 끌개 끈 파랑.
실행: python Tools/ArtGen/reindeer_v3.py → Assets/PocketCaravan/Resources/Transport/Reindeer · ReindeerSled (side/front/back_0~3 + _a/_b) + 미리보기
"""
import sys
from pathlib import Path
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
import pack_animals_v1 as PA
import wagon_v1 as WG
import reindeer_v1 as V1   # 썰매 날·짐칸 끝면 그림을 그대로 쓴다

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/PocketCaravan/Resources/Transport'
P = PA.P
# 순록 색 (북유럽 순록 동물 그림에서). pack_animals_v1에 없는 글자만 쓴다
P.update({'0': (196, 186, 170), 'G': (164, 150, 132),                       # 회색 주둥이 · 그늘
          '1': (138, 98, 70), '2': (172, 130, 94), '3': (106, 72, 50),       # 몸 · 밝은 등 · 그늘
          '4': (80, 56, 42), 'D': (64, 46, 36),                               # 다리 · 먼 다리
          '5': (242, 236, 222), '6': (212, 202, 184),                         # 흰 털 · 그늘
          '7': (224, 204, 160), '8': (252, 242, 214), 'F': (190, 170, 130), 'g': (158, 138, 104),   # 뿔 · 끝 · 그늘 · 먼 뿔 그늘
          '9': (34, 26, 30), 'E': (30, 22, 26),                               # 코 · 눈
          'Q': (52, 96, 150), 'q': (78, 128, 182)})                           # 북유럽 파랑 (안장 천 · 끌개 끈)
HOOF = (96, 84, 74, 255)   # 발굽: 테두리보다 밝게. 걷기 프레임(PA.lift)이 'k'를 찾으므로 'k'로 그리고 마지막에 바꾼다
BLUE = {'r': 'Q', 'R': 'q'}

SIDE = [
    "...........33...............................",
    "......1111333...............................",
    "....111111113...............................",
    "..000111E1111...............................",
    ".000001111111....22222......................",
    "09000001111111222111112.....................",
    "09000001111111111111111222222222............",
    ".0GGG0111111111111111111111111112222........",
    "..000....6611111111111111111111111112.55....",
    ".........6556131111111111111111111111555....",
    ".........66555631111111111111111111115555...",
    "..........6555561111111111111111111115555...",
    "...........655553111111111111111111115555...",
    "...........655556311111111111111111115555...",
    "............6555561111111111111111111555....",
    ".............555644D11111111111144D555......",
    "..............56644D11111111111144D.........",
    "...............6644D3DD311333DD344D.........",
    ".................44D.DD333333DD.44D.........",
    ".................44D.DD......DD.44D.........",
    ".................44D.DD......DD.44D.........",
    ".................44D.DD......DD.44D.........",
    ".................44D.DD......DD.44D.........",
    ".................44D.DD......DD.44D.........",
    ".................555.66......66.555.........",
    ".................kkk.kk......kk.kkk.........",
]
FRONT = [
    "....33333....",
    "..331111133..",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31665556613.",
    ".36555555563.",
    ".36555555563.",
    ".36555555563.",
    ".36655555663.",
    ".D4D65556D4D.",
    ".D4D.666.D4D.",
    ".D4D.....D4D.",
    ".D4D.....D4D.",
    ".D4D.....D4D.",
    ".D4D.....D4D.",
    ".D4D.....D4D.",
    ".555.....555.",
    ".kkk.....kkk.",
]
FACE = [
    "3.........3",
    "33.11111.33",
    ".311111113.",
    "..1111111..",
    ".1E11111E1.",
    ".111111111.",
    ".110000011.",
    ".600000006.",
    ".500909005.",
    ".550GGG055.",
    ".655000556.",
    "..5555555..",
    "...65556...",
]
BACK = [
    "..3.......3..",
    "..33.111.33..",
    "....11111....",
    "....11111....",
    "....31113....",
    "...3111113...",
    "..311111113..",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31111111113.",
    ".31155555113.",
    ".31555665513.",
    ".35555555553.",
    ".35555555553.",
    ".35555555553.",
    ".35555555553.",
    ".35555555553.",
    ".36555555563.",
    "..D4D555D4D..",
    "..D4D.5.D4D..",
    "..D4D...D4D..",
    "..D4D...D4D..",
    "..D4D...D4D..",
    "..D4D...D4D..",
    "..D4D...D4D..",
    "..D4D...D4D..",
    "..555...555..",
    "..kkk...kkk..",
]


def blank(w, h): return ['.' * w for _ in range(h)]


def layer(rows, ox, oy, w, h, outline=True):
    c = PA.Cv(w, h); c.draw(rows, ox, oy)
    if outline: c.outline()
    return c


def hoofs(im):
    px = im.load(); k = PA.P['k'] + (255,)
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y] == k: px[x, y] = HOOF
    return im


def cells(c, w, h):
    g = [['.'] * w for _ in range(h)]
    for (x, y), ch in c.items():
        if 0 <= x < w and 0 <= y < h: g[y][x] = ch
    return [''.join(r) for r in g]


def blue(rows): return [''.join(BLUE.get(ch, ch) for ch in r) for r in rows]


# ---------------- 옆모습 (왼쪽을 봄) 44x40 ----------------
SW, SH = 44, 40
SY = 13   # SIDE 첫 줄이 놓이는 높이 (위로 뿔 자리)


def side_antler(dx=0, dy=0, near=True):
    """굵은 줄기(두 칸)가 이마에서 뒤 위로 오르고, 꼭대기는 앞으로 굽은 손바닥 + 곧게 선 뒤 가지. 가까운 뿔은 수평 앞 가지와 이마 앞 눈가지"""
    A, S, T = ('7', 'F', '8') if near else ('F', 'g', '7')
    c = {}
    for x, y in [(11, 10), (12, 9), (13, 8), (14, 7), (15, 6), (16, 5), (17, 4), (17, 3)]:
        c[(x, y)] = A; c[(x + 1, y)] = S
    c[(10, 10)] = A
    for x in range(14, 18): c[(x, 2)] = A
    c[(18, 2)] = S; c[(14, 1)] = T; c[(14, 2)] = T
    for y in range(0, 3): c[(20, y)] = A
    c[(19, 2)] = S; c[(20, 0)] = T
    if near:
        for x in range(10, 14): c[(x, 6)] = A
        c[(10, 6)] = T
        for x in range(6, 10): c[(x, 9)] = A
        c[(9, 10)] = A; c[(6, 9)] = T; c[(6, 10)] = A
    return cells({(x + dx, y + dy + 2): v for (x, y), v in c.items()}, SW, SH)


SADDLE = (21, SY + 6)   # 안장 천 왼쪽 위 (등 줄). 길마·짐은 짐말(PA.make)과 같은 상대 위치


def side_parts(stage):
    x, y = SADDLE
    parts = []
    if stage == 1: parts += [(PA.SACK, x + 2, y)]
    if stage >= 2: parts += [(PA.SACK, x - 1, y), (PA.SACK, x + 6, y), (PA.BUNDLE, x, y - 6)]
    if stage >= 3: parts += [(PA.SMALL, x + 1, y - 10), (PA.JAR, x, y - 4), (PA.JAR, x + 7, y - 4)]
    return parts


def side_body(frame='', saddle=True, harness=False):
    c = PA.Cv(SW, SH); c.draw(PA.lift(SIDE, frame), 0, SY)
    if saddle:
        c.draw(blue(PA.CLOTH), SADDLE[0], SADDLE[1]); c.draw(PA.FRAME, SADDLE[0] + 1, SADDLE[1] - 2)
    if harness:   # 썰매 끌개: 목 아래 목줄 + 앞다리 뒤 배띠 (몸 위에만)
        for y in range(SY + 3, SY + 13):
            for x, k in ((13, 'Q'), (14, 'q'), (24, 'Q'), (25, 'q')):
                if c.get(x, y)[3] and c.get(x, y)[:3] not in (P['4'], P['D'], P['k']): c.px(x, y, k)
    c.outline()
    return hoofs(c.im)


def side(stage, frame='', saddle=True, harness=False):
    out = Image.new('RGBA', (SW, SH), (0, 0, 0, 0))
    out.alpha_composite(layer(side_antler(4, 1, False), 0, 0, SW, SH).im)
    out.alpha_composite(side_body(frame, saddle, harness))
    for rows, x, y in side_parts(stage): out.alpha_composite(layer(rows, x, y, SW, SH).im)
    out.alpha_composite(layer(side_antler(), 0, 0, SW, SH).im)
    return out


# ---------------- 앞·뒤 23x44 ----------------
EW, EH, OX = 23, 44, 5


def end_antler(cx, base):
    """앞·뒤에서 본 뿔: 머리 위에서 바깥 위로 굵게 오르다 곧게 서고 끝이 안으로 굽는다. 가지는 곧게 (안쪽 수평 · 바깥 위로)"""
    c = {}
    for s in (-1, 1):
        x0 = cx + s * 2
        path = [(x0, base), (x0 + s, base - 1), (x0 + 2 * s, base - 2), (x0 + 3 * s, base - 3)] + [(x0 + 4 * s, base - 4 - i) for i in range(5)]
        for x, y in path:
            c[(x, y)] = '7'
            if (x - s, y) not in c: c[(x - s, y)] = 'F'
        top = base - 9
        c[(x0 + 3 * s, top)] = '7'; c[(x0 + 2 * s, top)] = '8'; c[(x0 + 4 * s, top)] = '7'
        for i in range(1, 3): c[(x0 + 4 * s - i * s, base - 6)] = '7'
        c[(x0 + 2 * s, base - 6)] = '8'
        c[(x0 + 5 * s, base - 5)] = '7'; c[(x0 + 6 * s, base - 6)] = '7'; c[(x0 + 6 * s, base - 7)] = '8'
    return cells(c, EW, EH)


BAND = {'front': (1, 'QqqqQ'[:4]), 'back': (8, 'QqqqQ')}   # 안장 천(끌개 띠) 줄과 색. 앞은 얼굴 위로 보이는 등에 4줄
PACK = {'front': 1, 'back': 8}                           # 옆구리 자루를 매다는 줄


def end_body(view, frame, band):
    rows = FRONT if view == 'front' else BACK
    oy = EH - 2 - len(rows)
    c = PA.Cv(EW, EH); c.draw(PA.lift(rows, frame), OX, oy)
    if band:
        by, cols = BAND[view]
        for i, k in enumerate(cols):
            for x in range(EW):
                if c.get(x, oy + by + i)[3]: c.px(x, oy + by + i, k)
    c.outline()
    return hoofs(c.im), oy


def end_parts(view, stage, oy):
    py = oy + PACK[view]; parts = []
    if stage >= 1: parts += [(PA.SACK_L, OX - 3, py), (PA.SACK_R, OX + 11, py)]
    if stage >= 2: parts += [(PA.BUNDLE_END, OX + 3, py - 2)]
    if stage >= 3: parts += [(PA.SMALL_END, OX + 4, py - 4), (PA.JAR, OX - 4, py - 1), (PA.JAR, OX + 13, py - 1)]
    return parts


def end(view, stage, frame='', band=True):
    out = Image.new('RGBA', (EW, EH), (0, 0, 0, 0))
    body, oy = end_body(view, frame, band)
    if view == 'front':   # 몸 → 짐 → 뿔 → 얼굴 (얼굴은 따로 테두리)
        out.alpha_composite(body)
        for rows, x, y in end_parts(view, stage, oy): out.alpha_composite(layer(rows, x, y, EW, EH).im)
        fy = oy + 5
        out.alpha_composite(layer(end_antler(11, fy + 1), 0, 0, EW, EH).im)
        out.alpha_composite(layer(FACE, 6, fy, EW, EH).im)
    else:                 # 뿔 → 몸(뒤통수 포함) → 짐
        out.alpha_composite(layer(end_antler(11, oy + 1), 0, 0, EW, EH).im)
        out.alpha_composite(body)
        for rows, x, y in end_parts(view, stage, oy): out.alpha_composite(layer(rows, x, y, EW, EH).im)
    return out


def front(stage, frame=''): return end('front', stage, frame)
def back(stage, frame=''): return end('back', stage, frame)


# ---------------- 순록 썰매 (마차 짐칸 + 썰매 날) ----------------
LW, LH = 72, 42
BX0, BX1, BTOP = 38, 68, 27   # 짐칸 앞·뒤 · 윗면 줄


def sled_side(stage, frame=''):
    out = Image.new('RGBA', (LW, LH), (0, 0, 0, 0))
    out.alpha_composite(side(0, frame, saddle=False, harness=True), (0, LH - SH))
    c = WG.Cv(LW, LH)
    for x in range(14, BX0 + 1): c.px(x, BTOP + 2, 'h'); c.px(x, BTOP + 3, 'd')   # 끌채: 목줄에서 짐칸 앞까지
    c.hline(BX0, BX1, BTOP - 1, 'i'); c.hline(BX0, BX1, BTOP, 'd')
    if stage: c.paste(WG.cargo(stage), BX0 + 1, BTOP - 21)
    for i, k in enumerate(['H', 'h', 'w', 'd', 'w', 'm', 'd']): c.hline(BX0, BX1, BTOP + 1 + i, k)
    for x in range(BX0 + 4, BX1, 9): c.px(x, BTOP + 3, 'm'); c.px(x + 1, BTOP + 6, 'd')
    for x in (BX0, (BX0 + BX1) // 2, BX1 - 1):
        c.vline(x, BTOP - 2, BTOP + 7, 'l'); c.vline(x + 1, BTOP - 2, BTOP + 7, 'm'); c.px(x, BTOP - 2, 'H'); c.px(x + 1, BTOP - 2, 'h')
    V1.runner_side(c, BX0 - 1, BX1, BTOP + 13)
    c.outline()
    out.alpha_composite(c.im)
    return out


def sled_front(stage, frame=''):
    out = Image.new('RGBA', (WG.EW, WG.EH), (0, 0, 0, 0))
    oy = 16
    cv = WG.Cv(WG.EW, WG.EH); V1.sled_box_end(cv, oy, stage, True); WG.shafts(cv, oy + 13, oy + 24); cv.outline()
    out.alpha_composite(cv.im)
    out.alpha_composite(end('front', 0, frame), (3, WG.EH - EH - 2))
    return out


def sled_back(stage, frame=''):
    out = Image.new('RGBA', (WG.EW, WG.EH), (0, 0, 0, 0))
    out.alpha_composite(end('back', 0, frame), (3, 2))
    cv = WG.Cv(WG.EW, WG.EH); WG.shafts(cv, 22, WG.EH - 20); cv.outline(); out.alpha_composite(cv.im)
    cv = WG.Cv(WG.EW, WG.EH); V1.sled_box_end(cv, WG.EH - 18, stage, back=True); cv.outline()
    out.alpha_composite(cv.im)
    return out


def preview(path):
    names = ['PackHorse/side_2', 'Reindeer/side_2', 'PackHorse/front_2', 'Reindeer/front_2', 'PackHorse/back_2', 'Reindeer/back_2',
             'Wagon/side_3', 'ReindeerSled/side_3', 'Wagon/front_3', 'ReindeerSled/front_3', 'Wagon/back_3', 'ReindeerSled/back_3']
    ims = [Image.open(OUT / (n + '.png')).convert('RGBA') for n in names]
    W = sum(i.width + 6 for i in ims); H = max(i.height for i in ims)
    pv = Image.new('RGBA', (W, H), (236, 232, 240, 255)); x = 0
    for i in ims: pv.alpha_composite(i, (x, H - i.height)); x += i.width + 6
    pv.resize((W * 4, H * 4), Image.NEAREST).save(path)


if __name__ == '__main__':
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else OUT
    sets = (('Reindeer', (('side', side), ('front', front), ('back', back))),
            ('ReindeerSled', (('side', sled_side), ('front', sled_front), ('back', sled_back))))
    for folder, views in sets:
        d = out / folder; d.mkdir(parents=True, exist_ok=True)
        for view, fn in views:
            for s in range(4):
                fn(s).save(d / f'{view}_{s}.png')
                for fr in 'ab': fn(s, fr).save(d / f'{view}_{s}_{fr}.png')
    if out == OUT: preview(ROOT / 'Tools/ArtGen/reindeer_v3_preview.png')
    print('ok')
