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
    if stage >= 2:
        g.paste(HC.sack(), 9, base - 9)
        g.paste(HC.jar(), 15, base - 9)
    if stage >= 3:
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
    out.alpha_composite(hb, (0, H - 32))
    out.alpha_composite(cart(stage, frame).im, (0, 0))
    return out


if __name__ == '__main__':
    out = Path(sys.argv[1] if len(sys.argv) > 1 else '.'); out.mkdir(parents=True, exist_ok=True)
    for s in range(4):
        side(s).save(out / f'side_{s}.png')
        for fr in 'ab':
            side(s, fr).save(out / f'side_{s}_{fr}.png')
