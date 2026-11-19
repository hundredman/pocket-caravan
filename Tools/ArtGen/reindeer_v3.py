"""11/19 은샘 "말에 뿔만 붙인 것 같다" → v2 "비율·방향별 구도가 안 맞다" → v3.
기준 그림 둘을 따른다:
- 생김새·색: 북유럽 맵에 이미 있는 순록 (Resources/North/Critters/reindeer_0.png, 기우 그림). 밤색 몸 · 밝은 등 · 흰 가슴 털 · 흰 배 줄 ·
  흰 짧은 꼬리 · 가는 짙은 다리 · 흰 주둥이 · 위로 솟는 크림색 뿔.
- 크기·구도: 짐말·낙타(pack_animals_v1) 세 방향. 옆 40x32 (왼쪽을 봄), 앞·뒤 23x44 (몸 폭 13). 짐·안장 틀·테두리도 같은 그림.
순록은 말보다 목이 짧고 굵게 서고 머리를 수평으로 든다, 다리가 가늘다, 뿔이 머리 위로 크게 솟는다.
실행: python Tools/ArtGen/reindeer_v3.py <출력 폴더>   (시안: 그림과 비교 미리보기만)
"""
import sys
from pathlib import Path
from PIL import Image
sys.path.insert(0, str(Path(__file__).parent))
import pack_animals_v1 as PA

P = PA.P
# 북유럽 순록(Critters/reindeer_0) 색
P.update({'0': (196, 186, 170), 'G': (164, 150, 132), '1': (138, 98, 70), '2': (172, 130, 94), '3': (106, 72, 50), '4': (70, 48, 36), '5': (242, 236, 222), '6': (212, 202, 184),
          '7': (224, 204, 160), '8': (252, 242, 214), '9': (34, 26, 30), 'E': (40, 32, 32), 'Z': (52, 96, 150), 'z': (78, 128, 182), 'Y': (214, 170, 70)})

SIDE = [   # 왼쪽을 봄. 실제 순록: 다부진 몸 · 수평 등에 어깨가 조금 솟음 · 굵은 목을 앞으로 뻗어 머리를 등 높이쯤 · 목 아래 늘어진 밝은 목털 ·
           # 길고 털 많은 회색 주둥이 · 짧은 둥근 귀 · 흰 엉덩이와 짧은 꼬리 · 굵은 짙은 다리 · 발굽 위 흰 털 · 넓은 발굽 ·
           # 뿔은 뒤로 휘어 올라갔다 끝이 앞으로 굽고, 이마 앞으로 납작한 눈가지. 색은 북유럽 순록 그림(Critters/reindeer_0)
           # 0 밝은 회색 주둥이 · G 그 그늘 · 1 몸 · 2 밝은 등 · 3 그늘 · 4 짙은 다리 · 5/6 흰 털 · 7/8 뿔 · 9 코 · E 눈
"...........88.8...........................",
"..........877.7.8.........................",
"..............7.7.........................",
"...............77.8.......................",
"................7.7.......................",
"................777.......................",
"................77........................",
"..88............7.........................",
"..877..........77.........................",
"....77.....3..77..........................",
".....777.2112777..........................",
"......7.211111123.........................",
"....2211E11111111223......................",
".900011111111111111122222222222222222.....",
".900G111111111111111111111111111111115....",
"..00GG66111111111111111111111111111155....",
"...GG..666111111111111111111111111155.....",
"........666611111111111111111111111113....",
".........6666111111111111111111111113.....",
"..........66631111111111111111111111133...",
"...........663311111111111111111111133....",
"............33333111111111111111113333....",
".............3366666666666666666663.......",
".............4433.4433.......4433.4433....",
".............4433.4433.......4433.4433....",
".............4433.4433.......4433.4433....",
".............4433.4433.......4433.4433....",
".............4433.4433.......4433.4433....",
".............5555.5555.......5555.5555....",
"............kkkkk.kkkk.......kkkk.kkkkk...",
]

def side(stage, frame='', saddle=True):
    c = PA.Cv(44, 34)
    oy = 34 - len(SIDE)
    c.draw(PA.lift(SIDE, frame), 0, oy)
    sx = 20; by = oy + 13   # 안장: 등(줄 13) 위
    if saddle:
        c.draw(["ZZZZZZZZZZZ", "zzzzzzzzzzz", "zzzzzzzzzzz", "YZYZYZYZYZY"], sx, by + 1)
        c.draw(PA.FRAME, sx, by - 1)
    else:   # 썰매 끌 때: 가죽 목줄 · 등 띠
        for (x, y) in ((12, oy + 12), (13, oy + 13), (13, oy + 14), (14, oy + 15)): c.px(x, y, 'd'); c.px(x + 1, y, 'H')
        for y in range(by, by + 9): c.px(22, y, 'd'); c.px(23, y, 'H')
    parts = []
    if saddle:
        if stage == 1: parts += [(PA.SACK, sx + 2, by + 1)]
        if stage >= 2: parts += [(PA.SACK, sx - 1, by + 1), (PA.SACK, sx + 6, by + 1), (PA.BUNDLE, sx, by - 5)]
        if stage >= 3: parts += [(PA.SMALL, sx + 1, by - 9), (PA.JAR, sx, by - 3), (PA.JAR, sx + 7, by - 3)]
    return PA.with_parts(c, parts)

# ---------------- 앞(아래로 올 때) · 뒤(위로 갈 때): 짐말 앞·뒤와 같은 몸 폭 13 · 캔버스 23x44 ----------------
FRONT_BODY = [   # 꼬리 끝(흰) · 엉덩이 · 등 · 가슴(흰 털) · 앞다리 (가늘게)
"......5......",
".....555.....",
"..3222222223.",
".312222222213",
".311111111113",
".311111111113",
".311111111113",
".311111111113",
".311111111113",
".311111111113",
".311111111113",
".311111111113",
".311111111113",
".335555555333",
"..3555555553.",
"..3.......3..",
"..43.....34..",
"..43.....34..",
"..43.....34..",
"..43.....34..",
"..55.....55..",
"..kk.....kk..",
]
FRONT_HEAD = [   # 아래(나)를 보는 머리: 이마 · 눈 · 흰 주둥이 · 코 · 턱 밑 흰 털 (따로 테두리)
"..21112..",
".2111112.",
"1E11111E1",
"111111111",
".1155511.",
".1555551.",
"..59995..",
"..55555..",
"...555...",
]
ANTLER = [   # 앞·뒤에서 본 뿔: 머리 위 두 줄기가 바깥 위로, 가지 둘씩 (북유럽 순록 뿔처럼 위로 솟는다)
"8.8.......8.8",
".77..8.8..77.",
"..7.77.77.7..",
"..77.7.7.77..",
"...7.7.7.7...",
"....77.77....",
".....7.7.....",
]
BACK_BODY = [    # 머리 뒤(귀) · 목 · 등 · 흰 엉덩이 · 흰 꼬리 · 뒷다리
"....3...3....",
"....31113....",
"....21112....",
"....31113....",
"...3111113...",
"..322222223..",
".31111111113.",
".31111111113.",
".31111111113.",
".31111111113.",
".31111111113.",
".31111111113.",
".31155555113.",
".31555555513.",
"..355595553..",
"..43.555.34..",
"..43.....34..",
"..43.....34..",
"..43.....34..",
"..55.....55..",
"..kk.....kk..",
]
BACK_BODY = [r.replace('9', '5') for r in BACK_BODY]

def end(view, stage, frame='', saddle=True):
    W, H = 23, 44
    ox = 5
    body = FRONT_BODY if view == 'front' else BACK_BODY
    oy = H - 2 - len(body)
    layers = []
    def layer(fn):
        L = PA.Cv(W, H); fn(L); L.outline(); layers.append(L.im)
    band = oy + (5 if view == 'front' else 7)
    if view == 'back': layer(lambda L: L.draw(ANTLER, ox, oy - 7))   # 뒤: 뿔은 머리 뒤에서 위로 (몸보다 먼저)
    def b(L):
        L.draw(PA.lift(body, frame), ox, oy)
        keys = ('Z', 'z', 'Y') if saddle else ('d', 'H')
        for i, k in enumerate(keys):
            for x in range(W):
                if L.get(x, band + i)[3]: L.px(x, band + i, ('Y' if x % 2 else 'Z') if k == 'Y' else k)
    layer(b)
    if saddle:
        if stage >= 1: layer(lambda L: L.draw(PA.SACK_L, ox - 3, band)); layer(lambda L: L.draw(PA.SACK_R, ox + 11, band))
        if stage >= 2: layer(lambda L: L.draw(PA.BUNDLE_END, ox + 3, band - 2))
        if stage >= 3: layer(lambda L: L.draw(PA.SMALL_END, ox + 4, band - 4)); layer(lambda L: L.draw(PA.JAR, ox - 4, band - 1)); layer(lambda L: L.draw(PA.JAR, ox + 13, band - 1))
    if view == 'front':   # 앞: 머리를 들어 가슴 위에, 뿔은 머리 위로 등·짐을 덮는다
        hy = oy + 9
        layer(lambda L: L.draw(ANTLER, ox, hy - 7))
        layer(lambda L: L.draw(FRONT_HEAD, ox + 2, hy))
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for im in layers: out.alpha_composite(im)
    return out

if __name__ == '__main__':
    out = Path(sys.argv[1]); out.mkdir(parents=True, exist_ok=True)
    T = Path(__file__).resolve().parents[2] / 'Assets/PocketCaravan/Resources'
    ref = [Image.open(T / 'North/Critters/reindeer_0.png').convert('RGBA').transpose(Image.FLIP_LEFT_RIGHT),
           Image.open(T / 'Transport/PackHorse/side_2.png').convert('RGBA'), Image.open(T / 'Transport/PackHorse/front_2.png').convert('RGBA'), Image.open(T / 'Transport/PackHorse/back_2.png').convert('RGBA')]
    new = [side(0), side(2), side(3), side(0, 'a'), end('front', 0), end('front', 2), end('back', 0), end('back', 2)]
    rows = [ref, new]
    W = max(sum(i.width + 8 for i in r) for r in rows); H = 50
    pv = Image.new('RGBA', (W, H * 2 + 6), (236, 232, 240, 255))
    for ri, r in enumerate(rows):
        x = 0
        for i in r: pv.alpha_composite(i, (x, ri * (H + 6) + H - i.height)); x += i.width + 8
    pv.resize((pv.width * 6, pv.height * 6), Image.NEAREST).save(out / 'v3.png')
    print('ok')
