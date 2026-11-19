"""11/19 은샘 "북유럽 통일부터, 순록으로": 북유럽 운반 수단 짐 썰매(150) → 순록(360, 짐말 대신) → 순록 썰매(800, 마차 대신).
화풍은 짐말·낙타(pack_animals_v1 + packhorse_side_1116)·마차(wagon_v1)와 같게: 같은 몸 틀과 테두리·짐 그림을 쓰고
털빛(회갈색 몸 · 흰 목털 · 흰 주둥이)과 뿔, 북유럽 파란 안장 천(노란 줄)만 바꾼다. 순록 썰매는 마차의 짐칸을 바퀴 대신 썰매 날 위에 얹는다
(북유럽 짐 썰매 Resources/Transport/Sled와 같은 회색 날, 앞이 말려 올라간다).
실행: python Tools/ArtGen/reindeer_v1.py → Assets/PocketCaravan/Resources/Transport/Reindeer · ReindeerSled (side/front/back_0~3 + _a/_b) + 미리보기
"""
import sys
from pathlib import Path
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
import pack_animals_v1 as PA
import handcart_v3 as HC
import wagon_v1 as WG
from packhorse_side_1116 import stretch

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/PocketCaravan/Resources/Transport'

# 짐말 털빛 → 순록 털빛 (테두리·짐·나무는 그대로)
COAT = {
    PA.P['B']: (132, 110, 92),    # 몸: 회갈색
    PA.P['b']: (100, 82, 68),     # 몸 그늘
    PA.P['L']: (176, 158, 138),   # 밝은 쪽
    PA.P['M']: (226, 220, 206),   # 갈기·꼬리 자리 → 흰 목털·흰 꼬리
    PA.P['m']: (84, 70, 60),      # 먼 다리
    PA.P['c']: (232, 226, 214),   # 주둥이: 흰빛
    PA.P['r']: (52, 96, 150),     # 안장 천 붉은색 → 북유럽 파랑
    PA.P['R']: (78, 128, 182),
}
ANT, ANT2, O = (222, 204, 166, 255), (186, 164, 124, 255), PA.P['O'] + (255,)
STRAP = {(91, 67, 47): (52, 96, 150), (120, 82, 48): (78, 128, 182)}   # 마차 가죽 끈 → 썰매 끌개 끈도 파랑


def recolor(im, table=COAT):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            c = px[x, y]
            if c[3] and c[:3] in table: px[x, y] = table[c[:3]] + (255,)
    return im


def antlers(im, pts, over=False):
    """뿔: pts = 뿔 점 목록. 밝은 뿔색 + 끝은 조금 어둡게 + 바깥 테두리 (over: 몸 위에 겹칠 때도 테두리를 둘러 몸과 갈라 보이게)"""
    px = im.load(); w, h = im.size; s = set(pts)
    for (x, y) in pts:
        if 0 <= x < w and 0 <= y < h: px[x, y] = ANT2 if (x, y - 1) not in s else ANT
    for (x, y) in pts:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            a, b = x + dx, y + dy
            if 0 <= a < w and 0 <= b < h and (a, b) not in s and (px[a, b][3] == 0 or over): px[a, b] = O
    return im


def side_antlers(x0, y0):
    """옆모습(왼쪽을 봄) 뿔: (x0, y0) = 귀 자리. 뒤(오른쪽)로 휘어 올라가며 가지 둘"""
    rel = [(0, 0), (0, -1), (1, -2), (1, -3), (2, -4), (2, -5), (3, -6), (3, -7),   # 큰 줄기
           (-1, -3), (-2, -4), (-2, -5),                                             # 앞 가지
           (3, -4), (4, -5), (5, -5),                                                # 뒤 가지
           (4, -8), (2, -8)]
    return [(x0 + a, y0 + b) for a, b in rel]


def end_antlers(cx, y0, small=False):
    """앞·뒤에서 본 뿔: 가운데 cx, 귀 줄 y0. 양쪽으로 벌어져 올라간다 (좌우 대칭).
    small: 앞모습 (머리가 아래를 봐서 뿔이 몸 위에 겹친다) → 옆으로 낮게 벌어져 몸 밖으로 나오게 (실루엣으로 읽히게)"""
    half = ([(2, 0), (3, -1), (4, -2), (5, -3), (6, -4), (7, -5), (8, -6), (4, -4), (4, -5), (6, -6), (6, -7), (9, -7)] if small else
            [(2, 0), (3, -1), (4, -2), (5, -3), (5, -4), (6, -5), (6, -6), (3, -3), (2, -4), (7, -3), (8, -4)])
    pts = []
    for a, b in half: pts += [(cx + a, y0 + b), (cx - a - 1, y0 + b)]
    return pts


# ---------------- 순록 (짐 싣는 짐승) ----------------
def side_raw(stage, frame=''):
    im = PA.make(stage, frame)   # 짐말 옆모습 (40x32) 그대로 틀로 쓴다
    return recolor(im)


def side(stage, frame=''):
    """packhorse_side_1116과 같이 허리·다리 사이를 늘리고 짐을 두 칸 뒤로"""
    base = stretch(side_raw(0, frame))
    if stage == 0: out = base.copy()
    else:
        im = stretch(side_raw(stage, frame)); out = base.copy()
        for y in range(base.height):
            for x in range(base.width - 2):
                p = im.getpixel((x, y))
                if p != base.getpixel((x, y)): out.putpixel((x + 2, y), p)
    return antlers(out, side_antlers(7, 7))


def front(stage, frame=''):
    im = recolor(PA.end_view3('horse', 'front', stage, frame))
    return antlers(im, end_antlers(11, 27, True), over=True)   # 앞: 머리가 아래를 보고 뿔은 목·어깨 위로 벌어진다


def back(stage, frame=''):
    im = recolor(PA.end_view3('horse', 'back', stage, frame))
    return antlers(im, end_antlers(11, 14))


# ---------------- 순록 썰매 (마차 대신) ----------------
RUN, RUN2 = HC.P['k'] + (255,), HC.P['g'] + (255,)


def runner_side(c, x0, x1, y):
    """옆에서 본 썰매 날: 짐칸 아래 기둥 셋 + 긴 날, 앞(왼쪽) 끝이 말려 올라간다"""
    for x in range(x0, x1 + 1): c.p[x, y] = RUN; c.p[x, y - 1] = RUN2
    for k, (dx, dy) in enumerate([(-1, -2), (-2, -3), (-2, -4), (-1, -5)]): c.p[x0 + dx, y + dy] = RUN
    for x in (x0 + 3, (x0 + x1) // 2, x1 - 3):
        for yy in range(y - 4, y - 1): c.px(x, yy, 'm')


def sled_side(stage, frame=''):
    W, H = WG.W, WG.H
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    deer = antlers(recolor(WG.horse(frame)), side_antlers(7, 7))   # 마차 말과 같은 몸(목줄·등 띠, 안장 없음) → 순록 털빛 + 뿔
    out.alpha_composite(deer, (0, H - 32))
    c = WG.Cv(W, H)
    x0, x1, top = 34, 64, 19
    for x in range(11, x0 + 1): c.px(x, 21, 'h'); c.px(x, 22, 'd')   # 끌채
    c.hline(x0, x1, top - 1, 'i'); c.hline(x0, x1, top, 'd')
    if stage: c.paste(WG.cargo(stage), x0 + 1, top - 21)
    for i, k in enumerate(['H', 'h', 'w', 'd', 'w', 'm', 'd']): c.hline(x0, x1, top + 1 + i, k)
    for x in range(x0 + 4, x1, 9): c.px(x, top + 3, 'm'); c.px(x + 1, top + 6, 'd')
    for x in (x0, (x0 + x1) // 2, x1 - 1):
        c.vline(x, top - 2, top + 7, 'l'); c.vline(x + 1, top - 2, top + 7, 'm'); c.px(x, top - 2, 'H'); c.px(x + 1, top - 2, 'h')
    runner_side(c, x0 - 1, x1, top + 13)
    c.outline()
    out.alpha_composite(c.im, (0, 0))
    return out


def sled_box_end(c, oy, stage, front=False, back=False):
    """마차 짐칸 끝면과 같고, 양옆 바퀴 대신 썰매 날 끝(아래로 긴 회색 막대)"""
    x0, x1 = 2, 27
    for x in (0, 1, x1 + 1, x1 + 2):
        for y in range(oy + 6, oy + 16): c.p[x, y] = RUN if x in (0, x1 + 2) else RUN2
    c.rect(x0 + 1, oy - 2, x1 - 1, oy + 6, 'd')
    for x in range(x0 + 2, x1 - 1):
        if (x - x0) % 4 == 3: c.vline(x, oy - 1, oy + 6, 'i')
    c.hline(x0 + 1, x1 - 1, oy - 2, 'i')
    if stage: c.paste(WG.end_cargo(stage, front, back), x0 + 1, oy - 14)
    for i, k in enumerate(['H', 'h', 'w', 'd', 'w', 'm', 'd']): c.hline(x0, x1, oy + 7 + i, k)
    for x in (x0 + 6, x1 - 8): c.px(x, oy + 9, 'm'); c.px(x + 2, oy + 12, 'd')
    for x in (x0, x1 - 1):
        c.vline(x, oy - 3, oy + 13, 'l'); c.vline(x + 1, oy - 3, oy + 13, 'm'); c.px(x, oy - 3, 'H'); c.px(x + 1, oy - 3, 'h')


def sled_front(stage, frame=''):
    EW, EH = WG.EW, WG.EH
    out = Image.new('RGBA', (EW, EH), (0, 0, 0, 0))
    oy = 16
    cv = WG.Cv(EW, EH); sled_box_end(cv, oy, stage, True); WG.shafts(cv, oy + 13, oy + 24); cv.outline()
    out.alpha_composite(cv.im, (0, 0))
    deer = WG.harness(PA.end_view3('horse', 'front', 0, frame))   # 끌개 끈은 마차처럼 가죽색
    out.alpha_composite(antlers(recolor(deer), end_antlers(11, 27, True), over=True), (3, EH - 44 - 2))
    return out


def sled_back(stage, frame=''):
    EW, EH = WG.EW, WG.EH
    out = Image.new('RGBA', (EW, EH), (0, 0, 0, 0))
    deer = WG.harness(PA.end_view3('horse', 'back', 0, frame))
    out.alpha_composite(antlers(recolor(deer), end_antlers(11, 14)), (3, 6))
    cv = WG.Cv(EW, EH); WG.shafts(cv, 22, EH - 20); cv.outline(); out.alpha_composite(cv.im, (0, 0))
    cv = WG.Cv(EW, EH); sled_box_end(cv, EH - 18, stage, back=True); cv.outline()
    out.alpha_composite(cv.im, (0, 0))
    return out


if __name__ == '__main__':
    sets = (('Reindeer', (('side', side), ('front', front), ('back', back))),
            ('ReindeerSled', (('side', sled_side), ('front', sled_front), ('back', sled_back))))
    for folder, views in sets:
        d = OUT / folder; d.mkdir(parents=True, exist_ok=True)
        for view, fn in views:
            for s in range(4):
                fn(s).save(d / f'{view}_{s}.png')
                for fr in 'ab': fn(s, fr).save(d / f'{view}_{s}_{fr}.png')
    # 미리보기: 짐말 · 순록 · 마차 · 순록 썰매 (짐 2단계, 옆·앞·뒤)
    T = OUT
    names = ['PackHorse/side_2', 'Reindeer/side_2', 'PackHorse/front_2', 'Reindeer/front_2', 'PackHorse/back_2', 'Reindeer/back_2',
             'Wagon/side_3', 'ReindeerSled/side_3', 'Wagon/front_3', 'ReindeerSled/front_3', 'Wagon/back_3', 'ReindeerSled/back_3']
    ims = [Image.open(T / (n + '.png')).convert('RGBA') for n in names]
    W = sum(i.width + 6 for i in ims); H = max(i.height for i in ims)
    pv = Image.new('RGBA', (W, H), (236, 232, 240, 255)); x = 0
    for i in ims: pv.alpha_composite(i, (x, H - i.height)); x += i.width + 6
    pv.resize((W * 4, H * 4), Image.NEAREST).save(ROOT / 'Tools/ArtGen/reindeer_v1_preview.png')
    print('ok')
