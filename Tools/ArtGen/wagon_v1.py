"""11/19 은샘: 마차 (운반 수단 3단계, 네 지역 공용). 짐말(pack_animals_v1 + packhorse_side_1116)과 같은 말이
두 바퀴 짐수레를 끈다. 수레 색·짐(자루·항아리·보따리·상자)은 손수레(handcart_v3)와 같다.
고증: 고려 『고려도경』 권15 우거(바퀴 둘, 앞 멍에, 짐은 새끼줄로 묶음) · 유럽 본토 짐수레 모두 두 바퀴 한 마리 수레가 흔했다.
시안 단계: 옆모습(왼쪽으로 갈 때) 짐 0~3 + 걸음 a/b.
실행: python Tools/ArtGen/wagon_v1.py <출력 폴더>  → <출력>/side_0~3.png (+ _a/_b)
"""
import sys, math
from pathlib import Path
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
import pack_animals_v1 as PA
import handcart_v3 as HC
from packhorse_side_1116 import stretch

W, H = 68, 34
P = dict(HC.P)
P.update(r=(150, 58, 52), R=(176, 78, 66), y=(214, 170, 70), o=(46, 36, 29))


class Cv(HC.C):
    pass


def horse(frame=''):
    """짐말 몸 (안장·짐 없이) + 목줄(붉은 가죽 띠)과 등 띠. 40x32, 왼쪽을 본다"""
    c = PA.Cv(40, 32)
    oy = 8
    c.draw(PA.lift(PA.HORSE, frame), 1, oy)
    # 목줄(멍에 대신 가죽 목걸이): 목 아래쪽을 비스듬히 감는다
    for i, (x, y) in enumerate([(8, oy + 9), (9, oy + 10), (9, oy + 11), (10, oy + 12), (10, oy + 13), (11, oy + 14)]):
        c.px(x, y, 'r'); c.px(x + 1, y, 'R' if i % 2 == 0 else 'r')
    # 등 띠: 몸통 가운데를 두르는 띠
    for y in range(oy + 10, oy + 16):
        c.px(20, y, 'r'); c.px(21, y, 'R')
    c.outline()
    return stretch(c.im)


def tied(img, color='n'):
    """짐 위로 새끼줄 두 가닥 (고려도경: 짐은 새끼줄로 단단히 묶는다)"""
    return img


def cargo(stage):
    """수레 짐 0~3: 상자 둘 → 자루 둘 더 → 보따리·항아리 → 위에 상자·보따리 산더미"""
    g = HC.C(30, 22)
    base = 22
    if stage >= 1:
        g.paste(HC.crate(), 0, base - 10); g.paste(HC.crate(), 20, base - 10)
    if stage >= 2:   # 11/19 앞·뒤와 같은 짐: 2단계는 상자 둘 + 자루 하나 (항아리는 3단계부터)
        g.paste(HC.sack(), 9, base - 9)
    if stage >= 3:
        g.paste(HC.jar(), 15, base - 9)
        g.paste(HC.bundle(), 2, base - 15); g.paste(HC.crate(False), 12, base - 17); g.paste(HC.bundle(), 18, base - 15)
        for x in (7, 22):   # 새끼줄 (고려도경: 짐은 새끼줄로 단단히 묶는다)
            for y in range(base - 14, base - 1):
                if g.get(x, y)[3]: g.px(x, y, 'n')
    return g


def cart(stage, frame=''):
    """두 바퀴 짐수레 (왼쪽 끝에서 끌채가 말 쪽으로). 캔버스 W x H 기준 오른쪽에 놓인다"""
    c = Cv(W, H)
    x0, x1 = 34, 64          # 짐칸 앞·뒤
    top = 19                 # 짐칸 윗면 줄
    # 끌채 (말 옆구리 높이에서 짐칸 앞 아래로)
    for x in range(11, x0 + 1):   # 끌채: 목줄에서 말 옆구리를 따라 짐칸 앞까지
        c.px(x, 21, 'h'); c.px(x, 22, 'd')
    c.hline(x0, x1, top - 1, 'i'); c.hline(x0, x1, top, 'd')     # 짐칸 안쪽
    if stage:
        c.paste(cargo(stage), x0 + 1, top - 21)
    # 옆판 (손수레와 같은 나무 6단)
    for i, k in enumerate(['H', 'h', 'w', 'd', 'w', 'm', 'd']):
        c.hline(x0, x1, top + 1 + i, k)
    for x in range(x0 + 4, x1, 9):
        c.px(x, top + 3, 'm'); c.px(x + 1, top + 6, 'd')
    for x in (x0, (x0 + x1) // 2, x1 - 1):
        c.vline(x, top - 2, top + 7, 'l'); c.vline(x + 1, top - 2, top + 7, 'm'); c.px(x, top - 2, 'H'); c.px(x + 1, top - 2, 'h')
    c.hline(x0 + 2, x1 - 2, top + 8, 'i')
    c.vline(x1 - 2, top + 8, top + 11, 'm'); c.vline(x1 - 1, top + 8, top + 11, 'd')   # 뒤 받침목
    c.outline()
    # 큰 바퀴 (손수레보다 크게). 걸을 때 바큇살이 반 칸 돈다
    w = Cv(W, H)
    HC.wheel(w, 49.5, 27.0, 6.4)
    if frame == 'a':   # 바큇살 돌린 모양: 대각선 살
        for d in range(-4, 5):
            if abs(d) > 1:
                w.px(int(49.5 + d * .7), int(27 + d * .7), 'l'); w.px(int(49.5 + d * .7), int(27 - d * .7), 'l')
    w.outline()
    c.paste(w, 0, 0)
    return c


def side(stage, frame=''):
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    hb = horse(frame)
    # 먼 쪽 끌채는 말 몸 뒤 (말보다 먼저), 가까운 쪽 끌채·수레는 말 위에 그린다
    out.alpha_composite(harness(hb), (0, H - 32))
    out.alpha_composite(cart(stage, frame).im, (0, 0))
    return out


# ---------------- 앞·뒤 (짐말 앞·뒷모습 + 수레 짐칸 끝면) ----------------
# 11/19 은샘 "짐 실은 앞뒤가 이상해" 2판: 말을 짐칸에 붙이고(끌채 두 개가 말 양옆으로), 짐은 틈 없이 고르게 쌓고(뒤 말 꼬리가 짐 틈으로 비치지 않게),
# 짐말의 붉은 안장 천은 마차에선 가죽 끈 색으로.
EW, EH = 30, 58
STRAP = {(150, 58, 52): (91, 67, 47), (176, 78, 66): (120, 82, 48)}   # 붉은 안장 천 → 가죽 끈 (d · e)


def harness(im):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            c = px[x, y]
            if c[3] and c[:3] in STRAP: px[x, y] = STRAP[c[:3]] + (255,)
    return im


def end_cargo(stage):
    """앞·뒤에서 본 짐 (폭 24): 1 상자 둘 나란히 → 2 그 위로 자루 하나 → 3 위에 자루·작은 상자·자루를 한 단 더, 새끼줄 두 가닥"""
    g = HC.C(24, 22)
    # 11/19 옆모습과 같은 짐: 1 상자 둘 → 2 + 자루 하나(두 상자 사이 위로 보인다) → 3 위 한 단
    if stage >= 1: g.paste(HC.crate(), 1, 11); g.paste(HC.crate(), 12, 11)
    if stage == 2: g.paste(HC.sack(), 7, 6)
    if stage >= 3:
        g.paste(HC.sack(), 0, 4); g.paste(HC.crate(False), 8, 5); g.paste(HC.sack(), 15, 4)
        for x in (6, 17):
            for y in range(6, 21):
                if g.get(x, y)[3] and g.get(x, y)[:3] != P['O']: g.px(x, y, 'n')
    return g


def box_end(c, oy, stage):
    """짐칸 끝면 + 양옆 바퀴 두께. 짐칸 너비 26 (x 2~27). oy = 짐칸 윗면 줄"""
    x0, x1 = 2, 27
    HC.edge_wheel(c, 0, oy + 4, 14); HC.edge_wheel(c, x1, oy + 4, 14)
    c.rect(x0 + 1, oy - 2, x1 - 1, oy + 6, 'd')                  # 짐칸 안쪽 (짐 아래·사이가 비치지 않게 조금 높게)
    for x in range(x0 + 2, x1 - 1):
        if (x - x0) % 4 == 3: c.vline(x, oy - 1, oy + 6, 'i')
    c.hline(x0 + 1, x1 - 1, oy - 2, 'i')
    if stage: c.paste(end_cargo(stage), x0 + 1, oy - 14)
    for i, k in enumerate(['H', 'h', 'w', 'd', 'w', 'm', 'd']):
        c.hline(x0, x1, oy + 7 + i, k)
    for x in (x0 + 6, x1 - 8): c.px(x, oy + 9, 'm'); c.px(x + 2, oy + 12, 'd')
    for x in (x0, x1 - 1):
        c.vline(x, oy - 3, oy + 13, 'l'); c.vline(x + 1, oy - 3, oy + 13, 'm'); c.px(x, oy - 3, 'H'); c.px(x + 1, oy - 3, 'h')


def shafts(c, y0, y1):
    """끌채 두 개 (말 양옆, 앞·뒷모습)"""
    for x in (4, 24):
        c.vline(x, y0, y1, 'h'); c.vline(x + 1, y0, y1, 'd')


def front(stage, frame=''):
    """아래로 올 때: 말이 앞(아래)에서 나를 보고, 짐칸이 말 바로 뒤(위). 말 등이 짐칸 앞판 아래쪽을 가린다"""
    out = Image.new('RGBA', (EW, EH), (0, 0, 0, 0))
    oy = 16
    cv = Cv(EW, EH); box_end(cv, oy, stage); shafts(cv, oy + 13, oy + 24); cv.outline()
    out.alpha_composite(cv.im, (0, 0))
    out.alpha_composite(harness(PA.end_view3('horse', 'front', 0, frame)), (3, EH - 44 - 2))
    return out


def back(stage, frame=''):
    """위로 갈 때: 짐칸 뒤판이 앞(아래), 말은 짐칸 너머(위). 짐이 많으면 말 몸은 짐에 가리고 머리·귀만 보인다"""
    out = Image.new('RGBA', (EW, EH), (0, 0, 0, 0))
    out.alpha_composite(harness(PA.end_view3('horse', 'back', 0, frame)), (3, 2))
    cv = Cv(EW, EH); shafts(cv, 22, EH - 20); cv.outline(); out.alpha_composite(cv.im, (0, 0))
    cv = Cv(EW, EH); box_end(cv, EH - 18, stage); cv.outline()
    out.alpha_composite(cv.im, (0, 0))
    return out


if __name__ == '__main__':
    out = Path(sys.argv[1] if len(sys.argv) > 1 else '.'); out.mkdir(parents=True, exist_ok=True)
    for s in range(4):
        for view, fn in (('side', side), ('front', front), ('back', back)):
            fn(s).save(out / f'{view}_{s}.png')
            for fr in 'ab':
                fn(s, fr).save(out / f'{view}_{s}_{fr}.png')
