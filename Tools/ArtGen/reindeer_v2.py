"""11/19 은샘 "순록이 말에 뿔만 붙인 것 같아, 디자인부터": 순록 몸을 새로 그린다 (짐말 틀을 쓰지 않음).
순록다운 점: 짧고 굵은 다리 · 낮게 든 긴 머리와 뭉툭한 주둥이 · 목 아래로 늘어진 흰 목털 · 흰 엉덩이와 짧은 꼬리 ·
발굽 위 흰 띠 · 넓은 발굽 · 뒤로 휘었다 앞으로 굽는 큰 뿔과 이마 앞으로 뻗는 눈가지. 털은 등이 짙은 밤색, 옆구리가 밝은 회갈색.
테두리·짐(자루·보따리·항아리)·안장 틀은 짐말과 같은 그림(pack_animals_v1)을 써서 화풍을 맞춘다.
실행: python Tools/ArtGen/reindeer_v2.py <미리보기 폴더>   (시안 단계: 그림만 만들어 보여 준다)
"""
import sys
from pathlib import Path
from PIL import Image
sys.path.insert(0, str(Path(__file__).parent))
import pack_animals_v1 as PA

P = PA.P
P.update(D=(104, 76, 56), d2=(80, 58, 44), G=(150, 126, 100), g=(124, 102, 80), W=(238, 232, 220), w2=(206, 198, 184),
         Q=(66, 52, 44), q=(92, 74, 60), n=(54, 40, 34), u=(176, 160, 140),
         A=(224, 206, 168), a=(190, 168, 128), Z=(52, 96, 150), z=(78, 128, 182), Y=(214, 170, 70))
P['d2'] = (80, 58, 44)

# 옆모습 (왼쪽을 봄). D 짙은 등 · G 옆구리 · g 그늘 · W 흰 털 · w2 흰 털 그늘 · Q 가까운 다리 · q 먼 다리 · n 코 · u 주둥이
SIDE = [
"...DDDD.........................",
"..DGGGGD........................",
".DGeGGGGD.......................",
"nuGGGGGGGD......................",
"nuuGGg.DGGD.....................",
".uug...WGGGD..DDDDDDDDDDDDD.....",
".......WWGGGDDDDDDDDDDDDDDDDDD..",
".......WWWGGGGGGGGGGGGGGGGGGGDW.",
"......WWWWGGGGGGGGGGGGGGGGGGGWWW",
"......WWWWGGGGGGGGGGGGGGGGGGGWWW",
"......WWWWgGGGGGGGGGGGGGGGGGGWWW",
".......WWWggGGGGGGGGGGGGGGGGgWW.",
"........vv.ggggggggggggggggg....",
"..........QQQ.qq.....qq.QQQ.....",
"..........QQQ.qq.....qq.QQQ.....",
"..........QQQ.qq.....qq.QQQ.....",
"..........QQQ.qq.....qq.QQQ.....",
"..........WWW.ww.....ww.WWW.....",
"..........kkkk.kk....kk.kkkk....",
]
# 2글자 키(w2)를 한 글자로: 'v'
SIDE = [r.replace('w2', 'vv') for r in SIDE]
P['v'] = (206, 198, 184); P['w'] = (206, 198, 184)

ANTLER_SIDE = [   # 머리 위 (오른쪽이 등 쪽). 뒤로 휘었다 끝이 앞으로, 이마 앞 눈가지
"......A...A.",
".A....A..A..",
".A...AA.A...",
"..A..A.AA...",
"..AAA.AA....",
"...A..A.....",
"..A..AA.....",
".AA.AA......",
"A..AA.......",
]

def side(stage, frame='', saddle=True):
    c = PA.Cv(36, 34)
    oy = 13
    c.draw(PA.lift(SIDE, frame), 1, oy)
    c.draw(ANTLER_SIDE, 1, oy - 9)
    # 안장: 파란 천 + 나무 틀 (짐말 그림과 같은 틀, 순록 등 높이에)
    sx = 14
    if not saddle:   # 썰매 끌 때: 목줄 · 등 띠만 (가죽 끈, 마차 말과 같다)
        for (x, y) in ((9, oy + 6), (10, oy + 7), (10, oy + 8), (11, oy + 9), (11, oy + 10)): c.px(x, y, 'd'); c.px(x + 1, y, 'H')
        for y in range(oy + 6, oy + 12): c.px(19, y, 'd'); c.px(20, y, 'H')
        return PA.with_parts(c, [])
    c.draw(["ZZZZZZZZZZZ", "zzzzzzzzzzz", "YZYZYZYZYZY"], sx, oy + 6)
    c.draw(PA.FRAME, sx, oy + 4)
    parts = []
    if stage == 1: parts += [(PA.SACK, sx + 2, oy + 6)]
    if stage >= 2: parts += [(PA.SACK, sx - 1, oy + 6), (PA.SACK, sx + 6, oy + 6), (PA.BUNDLE, sx, oy)]
    if stage >= 3: parts += [(PA.SMALL, sx + 1, oy - 4), (PA.JAR, sx, oy + 2), (PA.JAR, sx + 7, oy + 2)]
    return PA.with_parts(c, parts)


# ---------------- 앞(아래로 올 때) · 뒤(위로 갈 때). 몸 폭 13 (짐말과 같은 캔버스 23 x 44) ----------------
FRONT_BODY = [          # 위에서 아래: 흰 꼬리 끝 · 엉덩이 · 등 · 어깨 · 흰 목털 · 앞다리
"......W......",
".....WWW.....",
"...DDWWWDD...",
"..DGGGGGGGD..",
".DGGGGGGGGGD.",
".DGGGDDDGGGD.",
".DGGGDDDGGGD.",
".DGGGDDDGGGD.",
".DGGGGGGGGGD.",
".gGGGGGGGGGg.",
".gGGGGGGGGGg.",
".gGGWWWWWGGg.",
"..gWWWWWWWg..",
"..gWWWWWWWg..",
"..QQ.....QQ..",
"..QQ.....QQ..",
"..QQ.....QQ..",
"..WW.....WW..",
".kkk.....kkk.",
]
FRONT_FACE = [          # 아래를 보는 머리: 넓은 이마 · 눈 · 밝은 주둥이 · 검은 코 · 턱 밑 흰 목털 (따로 테두리)
".DGGGGGD.",
"DGGGGGGGD",
"GeGGGGGeG",
"GGGGGGGGG",
".GGuuuGG.",
".WuuuuuW.",
".WWunuWW.",
"..WWWWW..",
"...WWW...",
]
ANT_FRONT = [           # 앞에서 본 뿔: 머리 양쪽에서 바깥으로 벌어진다. 가운데는 비워 등·짐이 보이게 (폭 17)
"A.A...........A.A",
".AA...........AA.",
"..A.A.......A.A..",
"...AA.......AA...",
"....A.......A....",
"....AA.....AA....",
".....A.....A.....",
]
BACK_BODY = [           # 위에서 아래: 머리 뒤 · 목 · 등 · 엉덩이 · 흰 엉덩이 · 짧은 흰 꼬리 · 뒷다리
"....DDDDD....",
"....DGGGD....",
"....DGGGD....",
"...DGGGGGD...",
"..DGGGGGGGD..",
".DGGGGGGGGGD.",
".DGGGGGGGGGD.",
".DGGGGGGGGGD.",
".DGGGGGGGGGD.",
".gGGGGGGGGGg.",
".gGGWWWWWGGg.",
".gGWWWWWWWGg.",
"..gWWWWWWWg..",
"..gWWwWwWWg..",
"..QQ.WWW.QQ..",
"..QQ.....QQ..",
"..QQ.....QQ..",
"..WW.....WW..",
".kkk.....kkk.",
]
ANT_BACK = [
"A...A.........A...A",
".A.AA.........AA.A.",
"..A..A.......A..A..",
"..AA..A.....A..AA..",
"...AA.A.....A.AA...",
".....AA.....AA.....",
"......A.....A......",
]

def end(view, stage, frame='', saddle=True):
    W, H = 23, 44
    ox = 5
    body = FRONT_BODY if view == 'front' else BACK_BODY
    oy = H - 2 - len(body)
    layers = []
    def layer(fn):
        L = PA.Cv(W, H); fn(L); L.outline(); layers.append(L.im)
    if view == 'back':   # 뒤: 뿔은 몸 뒤(위)로 솟는다 → 먼저
        layer(lambda L: L.draw(ANT_BACK, ox - 3, oy - 7))
    def b(L):
        L.draw(PA.lift(body, frame), ox, oy)
        band = oy + (5 if view == 'front' else 6)
        for i, k in enumerate(('Z', 'z', 'Y') if saddle else ('d', 'H')):   # 안장 천 (썰매 끌 땐 가죽 등 띠) (몸 위에만)
            for x in range(W):
                if L.get(x, band + i)[3]: L.px(x, band + i, ('Y' if x % 2 else 'Z') if k == 'Y' else k)
    layer(b)
    py = oy + (5 if view == 'front' else 6)
    if not saddle: stage = 0
    if stage >= 1: layer(lambda L: L.draw(PA.SACK_L, ox - 3, py)); layer(lambda L: L.draw(PA.SACK_R, ox + 11, py))
    if stage >= 2: layer(lambda L: L.draw(PA.BUNDLE_END, ox + 3, py - 2))
    if stage >= 3: layer(lambda L: L.draw(PA.SMALL_END, ox + 4, py - 4)); layer(lambda L: L.draw(PA.JAR, ox - 4, py - 1)); layer(lambda L: L.draw(PA.JAR, ox + 13, py - 1))
    if view == 'front':   # 앞: 머리가 흰 목털 위에, 뿔은 머리 위로 몸을 덮고 양옆으로
        layer(lambda L: L.draw(ANT_BACK, ox - 3, oy + 3))   # 뒷모습과 같은 큰 뿔이 머리 위로 (몸을 덮는다)
        layer(lambda L: L.draw(FRONT_FACE, ox + 2, oy + 10))
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for im in layers: out.alpha_composite(im)
    return out


# ---------------- 순록 썰매 (마차 대신): 마차 짐칸 + 썰매 날, 새 순록이 끈다 ----------------
import wagon_v1 as WG
import handcart_v3 as HC
RUN, RUN2 = HC.P['k'] + (255,), HC.P['g'] + (255,)

def sled_side(stage, frame=''):
    W, H = WG.W, WG.H
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    out.alpha_composite(side(0, frame, saddle=False), (0, 0))
    c = WG.Cv(W, H)
    x0, x1, top = 34, 64, 19
    for x in range(12, x0 + 1): c.px(x, 21, 'h'); c.px(x, 22, 'd')   # 끌채
    c.hline(x0, x1, top - 1, 'i'); c.hline(x0, x1, top, 'd')
    if stage: c.paste(WG.cargo(stage), x0 + 1, top - 21)
    for i, k in enumerate(['H', 'h', 'w', 'd', 'w', 'm', 'd']): c.hline(x0, x1, top + 1 + i, k)
    for x in range(x0 + 4, x1, 9): c.px(x, top + 3, 'm'); c.px(x + 1, top + 6, 'd')
    for x in (x0, (x0 + x1) // 2, x1 - 1):
        c.vline(x, top - 2, top + 7, 'l'); c.vline(x + 1, top - 2, top + 7, 'm'); c.px(x, top - 2, 'H'); c.px(x + 1, top - 2, 'h')
    y = top + 13   # 썰매 날: 앞(왼쪽) 끝이 말려 올라간다
    for x in range(x0 - 1, x1 + 1): c.p[x, y] = RUN; c.p[x, y - 1] = RUN2
    for dx, dy in ((-1, -2), (-2, -3), (-2, -4), (-1, -5)): c.p[x0 - 1 + dx, y + dy] = RUN
    for x in (x0 + 3, (x0 + x1) // 2, x1 - 3):
        for yy in range(y - 4, y - 1): c.px(x, yy, 'm')
    c.outline()
    out.alpha_composite(c.im, (0, 0))
    return out

def sled_box_end(c, oy, stage, front=False, back=False):
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
    out.alpha_composite(end('front', 0, frame, saddle=False), (3, EH - 44 - 2))
    return out

def sled_back(stage, frame=''):
    EW, EH = WG.EW, WG.EH
    out = Image.new('RGBA', (EW, EH), (0, 0, 0, 0))
    out.alpha_composite(end('back', 0, frame, saddle=False), (3, -2))
    cv = WG.Cv(EW, EH); WG.shafts(cv, 22, EH - 20); cv.outline(); out.alpha_composite(cv.im, (0, 0))
    cv = WG.Cv(EW, EH); sled_box_end(cv, EH - 18, stage, back=True); cv.outline()
    out.alpha_composite(cv.im, (0, 0))
    return out

def preview(path):
    """시안 비교: 위 줄 지난번(짐말 틀) · 아래 줄 새 순록. 짐 실은 순록(옆 0·2·3 · 앞 · 뒤) · 순록 썰매(옆 · 앞 · 뒤)"""
    T = Path(__file__).resolve().parents[2] / 'Assets/PocketCaravan/Resources/Transport'
    old = [Image.open(T / f).convert('RGBA') for f in ('Reindeer/side_0.png', 'Reindeer/side_2.png', 'Reindeer/side_3.png', 'Reindeer/front_2.png', 'Reindeer/back_2.png', 'ReindeerSled/side_3.png', 'ReindeerSled/front_3.png', 'ReindeerSled/back_3.png')]
    new = [side(0), side(2), side(3), end('front', 2), end('back', 2), sled_side(3), sled_front(3), sled_back(3)]
    W = sum(max(a.width, b.width) + 8 for a, b in zip(old, new)); H = 60 * 2 + 12
    pv = Image.new('RGBA', (W, H), (236, 232, 240, 255)); x = 0
    for a, b in zip(old, new):
        cw = max(a.width, b.width) + 8
        pv.alpha_composite(a, (x + (cw - a.width) // 2, 60 - a.height)); pv.alpha_composite(b, (x + (cw - b.width) // 2, H - b.height)); x += cw
    pv.resize((W * 5, H * 5), Image.NEAREST).save(path)

if __name__ == '__main__':
    out = Path(sys.argv[1]); out.mkdir(parents=True, exist_ok=True)
    ims = [side(s) for s in range(4)] + [side(0, 'a')] + [end('front', s) for s in (0, 2, 3)] + [end('back', s) for s in (0, 2, 3)]
    W = sum(i.width + 4 for i in ims); H = max(i.height for i in ims)
    pv = Image.new('RGBA', (W, H), (236, 232, 240, 255)); x = 0
    for i in ims: pv.alpha_composite(i, (x, H - i.height)); x += i.width + 4
    pv.resize((W * 8, H * 8), Image.NEAREST).save(out / 'side.png')
    preview(out / 'compare.png')
    print('ok')
