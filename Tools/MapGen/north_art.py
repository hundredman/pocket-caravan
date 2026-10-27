"""북유럽 전용으로 코드로 그린 타일 묶음 (Tools/MapGen/north_art.png). 10/24.
룬돌: 기존 묘비 같은 그림 대신, 위가 둥글고 아래가 넓은 길쭉한 선돌에 룬 문자 띠가 새겨진 북유럽 룬스톤 (16x32, 1x2칸).
  (0,0)-(0,1) 룬돌 A: 가운데 한 줄 룬 + 위에 눈
  (1,0)-(1,1) 룬돌 B: 뱀 모양 띠 안에 룬 + 이끼
  (2,0)-(3,1) 큰 룬 바위 2x2: 들쭉날쭉한 바위에 돛단배와 룬 두 줄 (룬돌 둥지 가운데용)
룬 홈은 청록색이라 밤에 NorthLife 불빛과 함께 빛나 보인다."""
import os
from PIL import Image

PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "north_art.png")
OUT = (52, 55, 66, 255); DARK = (92, 98, 112, 255); MID = (128, 134, 146, 255); LIGHT = (164, 170, 182, 255)
HI = (196, 201, 210, 255); GROOVE = (44, 58, 66, 255); RUNE = (96, 214, 204, 255); RUNE_D = (60, 150, 150, 255)
SNOW = (246, 244, 250, 255); SNOW_S = (210, 204, 222, 255); MOSS = (112, 138, 84, 255); MOSS_D = (84, 106, 64, 255)

GLYPHS = {   # 3x4 룬 (1 = 홈)
    "f": ["100", "111", "110", "100"], "r": ["110", "101", "110", "101"], "z": ["101", "111", "010", "010"],
    "t": ["111", "010", "010", "010"], "s": ["100", "110", "011", "001"], "o": ["010", "101", "010", "101"],
    "h": ["101", "111", "101", "101"], "k": ["101", "110", "110", "101"],
}


def stone(img, ox, oy, w, h, edges, seed):
    """edges(y) -> (l, r): 줄마다 돌 폭. 왼쪽 밝게, 오른쪽 어둡게, 테두리."""
    px = img.load()
    rows = {}
    for y in range(h):
        e = edges(y)
        if e:
            rows[y] = e
    for y, (l, r) in rows.items():
        for x in range(l, r + 1):
            c = MID
            if x <= l + 1: c = LIGHT
            if x == l + 1 and y % 5 == seed % 5: c = HI
            if x >= r - 1: c = DARK
            if (x * 7 + y * 13 + seed) % 11 == 0: c = LIGHT if c == MID else c   # 화강암 알갱이
            px[ox + x, oy + y] = c
    for y, (l, r) in rows.items():   # 테두리
        up, dn = rows.get(y - 1), rows.get(y + 1)
        for x in range(l, r + 1):
            if x in (l, r) or not up or not dn or x < up[0] or x > up[1] or x < dn[0] or x > dn[1]:
                px[ox + x, oy + y] = OUT
    return rows


def carve(img, ox, oy, x, y, word):
    px = img.load()
    for i, ch in enumerate(word):
        g = GLYPHS[ch]
        for gy, row in enumerate(g):
            for gx, v in enumerate(row):
                if v == "1":
                    px[ox + x + gx, oy + y + i * 5 + gy] = RUNE if (gx + gy + i) % 3 else RUNE_D


def snow_base(img, ox, oy, x0, x1, y):
    px = img.load()
    for x in range(x0, x1 + 1):
        d = min(x - x0, x1 - x)
        top = y - (2 if d > 3 else 1 if d > 1 else 0)
        for yy in range(top, y + 2):
            px[ox + x, oy + yy] = SNOW if yy < y + 1 else SNOW_S


def make():
    img = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    px = img.load()
    # 룬돌 A: 끝이 비스듬히 뾰족한 선돌 (아래로 갈수록 넓음)
    A = {1: (7, 8), 2: (6, 9), 3: (5, 10), 4: (4, 11)}
    def ea(y):
        if y < 1 or y > 28: return None
        if y in A: return A[y]
        if y < 10: return (3, 11)
        if y < 21: return (3, 12)
        return (2, 13)
    stone(img, 0, 0, 16, 32, ea, 1)
    carve(img, 0, 0, 6, 7, "fzrt")
    for x, y in [(7, 1), (8, 1), (6, 2), (7, 2), (8, 2), (5, 3), (6, 3), (7, 3)]:
        px[x, y] = SNOW
    snow_base(img, 0, 0, 0, 15, 29)
    # 룬돌 B: 어깨가 기운 두툼한 돌, 뱀 띠 안에 룬, 이끼
    B = {2: (8, 11), 3: (6, 12), 4: (5, 12)}
    def eb(y):
        if y < 2 or y > 28: return None
        if y in B: return B[y]
        if y < 15: return (4, 12)
        return (3, 13)
    stone(img, 16, 0, 16, 32, eb, 4)
    for y in range(6, 27):                     # 뱀 띠: 룬 둘레를 감싸는 홈 (위는 둥글게)
        px[16 + 5, y] = GROOVE; px[16 + 11, y] = GROOVE
    for x in range(6, 11):
        px[16 + x, 5] = GROOVE
    px[16 + 12, 25] = GROOVE; px[16 + 13, 26] = RUNE_D       # 뱀 꼬리
    carve(img, 16, 0, 7, 7, "hok")
    for (x, y) in [(4, 24), (4, 25), (3, 26), (4, 27), (12, 22), (13, 24), (12, 26), (6, 28), (11, 28)]:
        px[16 + x, y] = MOSS if (x + y) % 2 else MOSS_D
    for x, y in [(8, 2), (9, 2), (10, 2), (7, 3), (8, 3), (9, 3), (6, 4), (7, 4)]:
        px[16 + x, y] = SNOW
    snow_base(img, 16, 0, 1, 14, 29)
    # 큰 룬 바위 2x2: 들쭉날쭉한 큰 바위에 둥근 뱀 띠와 룬, 가운데 배 무늬
    C = {4: (13, 19), 5: (9, 23), 6: (7, 25), 7: (5, 26)}
    def ec(y):
        if y < 4 or y > 28: return None
        if y in C: return C[y]
        if y < 13: return (3, 27)
        return (2, 29)
    stone(img, 32, 0, 32, 32, ec, 7)
    # 돛 단 배 (위)와 룬 두 줄 (아래), 왼쪽 위에서 오른쪽 아래로 흐르는 뱀 띠 한 줄
    boat = [(9, 14), (10, 15), (11, 16)] + [(x, 16) for x in range(12, 21)] + [(21, 15), (22, 14), (8, 13), (23, 13)]
    sail = [(x, y) for y in range(8, 14) for x in range(13, 20) if (x + y) % 2 == 0 and abs(x - 16) <= (y - 6)]
    for x, y in sail:
        px[32 + x, y] = RUNE_D
    for y in range(7, 16):
        px[32 + 16, y] = GROOVE
    for x, y in boat:
        px[32 + x, y] = RUNE
    for i, ch in enumerate("fzrtsk"):
        for gy, row in enumerate(GLYPHS[ch]):
            for gx, v in enumerate(row):
                if v == "1":
                    px[32 + 4 + i * 4 + gx, 19 + gy] = RUNE if (gx + gy) % 2 else RUNE_D
    for x in range(4, 28):
        px[32 + x, 24] = GROOVE
        if x % 3 == 0: px[32 + x, 25] = GROOVE
    for x, y in [(12, 4), (13, 4), (14, 4), (15, 4), (16, 4), (17, 4), (18, 4), (9, 5), (10, 5), (11, 5), (12, 5), (13, 5), (14, 5), (20, 5), (21, 5), (22, 5)]:
        px[32 + x, y] = SNOW
    snow_base(img, 32, 0, 0, 31, 29)
    sled(img, 64, 0)
    icefall(img, 0, 32)
    watermill(img, 48, 32)   # 10/27 물방앗간 (3~5, 2~4)
    img.save(PATH)
    return PATH


def sled(img, ox, oy):
    """10/27 짐 실은 나무 썰매 2x1 (32x16): 앞이 말려 올라간 썰매 날 둘, 판자 바닥, 끈으로 묶은 모피 짐 + 눈.
    (원래 element 시트 그림은 상자 두 개를 붙인 것 같아 잘려 보였다)"""
    from PIL import ImageDraw
    d = ImageDraw.Draw(img)
    O = (52, 40, 36, 255); W = (176, 116, 68, 255); WD = (124, 80, 48, 255); WL = (206, 150, 96, 255)
    RUN = (82, 74, 84, 255); RUNL = (150, 146, 160, 255); FUR = (150, 108, 72, 255); FURD = (110, 78, 54, 255)
    FURL = (236, 226, 210, 255); ROPE = (208, 72, 56, 255); SH = (200, 194, 214, 255)
    X = lambda x: ox + x; Y = lambda y: oy + y
    d.line((X(4), Y(15), X(29), Y(15)), fill=SH)                                  # 그림자
    for ry in (12, 14):                                                           # 썰매 날 두 줄 (앞이 말림)
        d.line((X(3), Y(ry), X(27), Y(ry)), fill=RUN)
        d.line((X(4), Y(ry), X(25), Y(ry)), fill=RUNL if ry == 12 else RUN)
        d.line((X(27), Y(ry), X(29), Y(ry - 2)), fill=RUN); d.line((X(29), Y(ry - 2), X(29), Y(ry - 4)), fill=RUN)
        d.point((X(28), Y(ry - 4)), fill=RUN)
    for sx in (6, 13, 20):                                                        # 버팀대
        d.line((X(sx), Y(10), X(sx), Y(14)), fill=WD)
    d.rectangle((X(3), Y(8), X(26), Y(10)), fill=W, outline=O)                    # 판자 바닥
    d.line((X(4), Y(8), X(25), Y(8)), fill=WL)
    for px_ in (9, 15, 21):
        d.point((X(px_), Y(9)), fill=WD)
    d.ellipse((X(5), Y(1), X(16), Y(9)), fill=FUR, outline=O)                     # 둥글게 만 모피 짐
    d.ellipse((X(13), Y(2), X(24), Y(9)), fill=FURD, outline=O)
    d.line((X(7), Y(3), X(12), Y(2)), fill=FURL); d.line((X(15), Y(4), X(19), Y(3)), fill=FUR)
    d.line((X(11), Y(1), X(11), Y(9)), fill=ROPE); d.line((X(19), Y(2), X(19), Y(9)), fill=ROPE)  # 묶은 끈
    for x, y in [(7, 1), (8, 1), (9, 1), (6, 2), (15, 2), (16, 2), (17, 2), (18, 2)]:   # 짐 위 눈
        img.putpixel((X(x), Y(y)), (246, 244, 250, 255))


if __name__ == "__main__":
    print(make())



def icefall(img, ox, oy):
    """10/27 얼어붙은 폭포 3칸 폭: (0~2, 2) 눈 덮인 턱 + 고드름, (0~2, 3)/(0~2, 4) 번갈아 쓰는 얼음 기둥 (세로로 이어짐),
    (0~2, 5) 아래 끝: 얼음 물보라 더미. 세로 줄무늬(흘러내리다 언 물줄기) + 흰 반짝임 + 들쭉날쭉한 가장자리.
    (원래 relief 타일은 납작한 하늘색 띠라 폭포 느낌이 안 났다)"""
    import math
    px = img.load()
    OUTL = (58, 92, 132, 255); D = (104, 160, 204, 255); M = (150, 202, 232, 255); L = (196, 230, 246, 255); W = (244, 252, 255, 255)
    SN = (246, 244, 250, 255); SNS = (204, 200, 220, 255)
    def edges(gy):          # gy: 기둥 안 전체 y (위 0부터). 32px마다 반복 → 가운데 두 장이 이음매 없이 이어짐
        t = 2 * math.pi * gy / 32
        return 4 + round(2 * math.sin(t) + math.sin(2 * t)), 43 - round(2 * math.sin(t + 1.3) + math.sin(2 * t + 0.5))
    def shade(x, gy):
        band = (x * 5 // 7)
        k = (band * 37 + (gy // 32) * 0) % 9
        c = M if k < 4 else L if k < 7 else D
        # 흰 물줄기 반짝임: 몇 줄만, 위아래로 끊기며 흐름
        if x in (11, 12, 23, 31, 32, 38) and (gy + x * 3) % 32 < 18:
            c = W
        if x in (7, 19, 27, 41) and (gy + x * 5) % 32 < 22:
            c = D
        return c
    def column(y0, y1, gy0):
        for y in range(y0, y1):
            gy = gy0 + (y - y0)
            l, r = edges(gy)
            for x in range(l, r + 1):
                px[ox + x, oy + y] = OUTL if x in (l, r) else (L if x == l + 1 else D if x == r - 1 else shade(x, gy))
    # (2) 위: 눈 덮인 턱 + 고드름
    column(4, 16, 4)
    for x in range(1, 47):
        d = min(x - 1, 46 - x)
        top = 1 if d > 6 else 2 if d > 2 else 3
        bot = 8 + (1 if (x // 4) % 3 == 0 else 0)          # 아래가 살짝 물결진 눈 턱
        for y in range(top, bot):
            px[ox + x, oy + y] = SN if y < bot - 2 else SNS
        px[ox + x, oy + top - 1] = OUTL
        px[ox + x, oy + bot] = OUTL if x in (1, 46) else px[ox + x, oy + bot]
    for y in range(3, 9):
        px[ox, oy + y] = OUTL; px[ox + 47, oy + y] = OUTL
    for (x, ln) in [(6, 5), (9, 3), (15, 6), (21, 4), (28, 6), (34, 3), (39, 5)]:   # 고드름
        for y in range(9, 9 + ln):
            px[ox + x, oy + y] = W if y < 9 + ln - 1 else L
            if y < 9 + ln - 2: px[ox + x + 1, oy + y] = L
    # (3)(4) 가운데 기둥 두 장 (gy 16~47 → 다음 장으로 이어짐)
    column(16, 48, 16)
    # (5) 아래: 기둥 끝 + 얼음 물보라 더미
    column(48, 56, 48)
    blobs = [(10, 57, 9, 5), (24, 55, 12, 7), (38, 57, 9, 5), (17, 59, 7, 4), (31, 59, 7, 4)]
    for (cx, cy, rx, ry) in blobs:
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if 0 <= x < 48 and 48 <= y < 64 and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1:
                    px[ox + x, oy + y] = W if y < cy - ry / 3 else L if y < cy + ry / 3 else M
    for x in range(48):                      # 더미 테두리
        for y in range(48, 64):
            if px[ox + x, oy + y][3] and px[ox + x, oy + y] in (W, L, M):
                if y + 1 >= 64 or px[ox + x, oy + y + 1][3] == 0 or (x + 1 < 48 and px[ox + x + 1, oy + y][3] == 0) or (x > 0 and px[ox + x - 1, oy + y][3] == 0):
                    px[ox + x, oy + y] = OUTL if y > 56 else D


def watermill(img, ox, oy):
    """10/27 물방앗간 3x3 (48x48): 돌 기초 위 나무 판자 집 + 눈 덮인 가파른 박공 지붕, 오른쪽에 물레방아 바퀴 (아래 물길 없음).
    문은 가운데 칸 (스탬프 기준 door_dx=1). 개울 나루 마을 (물방앗간 주인) 용."""
    import math
    from PIL import ImageDraw
    px = img.load()
    d = ImageDraw.Draw(img)
    O = (52, 40, 36, 255)
    WD = (98, 62, 42, 255); W = (138, 88, 56, 255); WL = (172, 116, 74, 255)        # 판자 벽
    ST = (128, 134, 146, 255); STD = (92, 98, 112, 255); STL = (164, 170, 182, 255)  # 돌 기초
    SN = (246, 244, 250, 255); SNS = (210, 204, 222, 255); SNO = (150, 146, 170, 255)
    TH = (150, 112, 64, 255); THD = (112, 82, 48, 255)                                  # 지붕 처마(이엉)
    DOOR = (44, 30, 26, 255); WIN = (250, 214, 120, 255); WINF = (98, 62, 42, 255)
    RIM = (120, 78, 48, 255); RIML = (166, 112, 70, 255); HUB = (60, 44, 38, 255)
    WA = (150, 202, 232, 255); WAL = (196, 230, 246, 255); WAW = (244, 252, 255, 255); WAD = (104, 160, 204, 255)
    X = lambda x: ox + x; Y = lambda y: oy + y
    # 바닥 그림자
    d.line((X(2), Y(46), X(27), Y(46)), fill=SNS)
    # 벽 (판자 세로줄)
    d.rectangle((X(3), Y(21), X(26), Y(40)), fill=W, outline=O)
    for x in range(5, 26, 4):
        d.line((X(x), Y(22), X(x), Y(39)), fill=WD)
        d.line((X(x + 1), Y(22), X(x + 1), Y(39)), fill=WL)
    # 돌 기초
    d.rectangle((X(2), Y(40), X(27), Y(45)), fill=ST, outline=O)
    for i, x in enumerate(range(3, 26, 5)):
        d.line((X(x), Y(41 + (i % 2) * 2), X(x + 3), Y(41 + (i % 2) * 2)), fill=STL)
        d.point((X(x + 2), Y(44)), fill=STD)
    # 문 (가운데 아래, 둥근 윗머리)
    d.rectangle((X(16), Y(31), X(23), Y(42)), fill=DOOR, outline=O)
    d.line((X(17), Y(30), X(22), Y(30)), fill=O)
    d.line((X(19), Y(32), X(19), Y(41)), fill=(70, 48, 40, 255))
    # 창 (따뜻한 불빛)
    d.rectangle((X(6), Y(26), X(10), Y(30)), fill=WIN, outline=WINF)
    d.line((X(8), Y(26), X(8), Y(30)), fill=WINF)
    # 지붕: 가파른 박공, 처마는 이엉, 위는 눈
    for y in range(2, 24):
        half = int((y - 2) * 15 / 21) + 1
        l, r = 14 - half, 15 + half
        for x in range(max(l, 0), min(r, 30) + 1):
            if y >= 20:
                c = THD if (x + y) % 3 == 0 else TH
            elif y >= 17 + (1 if (x // 3) % 2 else 0):
                c = SNS
            else:
                c = SN if x <= 14 else (SN if (x - 14) < (y - 2) * 0.4 else SNS)
            px[X(x), Y(y)] = c
        if 0 <= l: px[X(l), Y(y)] = O
        if r <= 30: px[X(r), Y(y)] = O
    d.line((X(0), Y(24), X(30), Y(24)), fill=O)
    # 고드름
    for x, ln in [(3, 2), (8, 3), (13, 2), (20, 3), (26, 2)]:
        d.line((X(x), Y(25), X(x), Y(24 + ln)), fill=WAL)
    # 용마루 끝 장식 (바이킹 박공 X 장식)
    d.line((X(11), Y(0), X(14), Y(3)), fill=WD); d.line((X(18), Y(0), X(15), Y(3)), fill=WD)
    # 굴대 (벽 → 바퀴)
    d.rectangle((X(26), Y(30), X(36), Y(32)), fill=WD, outline=O)
    # 바퀴 (6살, 테 두 줄, 날개 10개)
    cx, cy, R = 36.5, 31.5, 8
    for a in range(0, 180, 60):
        t = math.radians(a + 15)
        dx, dy = math.cos(t) * (R - 1), math.sin(t) * (R - 1)
        d.line((X(cx - dx), Y(cy - dy), X(cx + dx), Y(cy + dy)), fill=RIM, width=1)
    d.ellipse((X(cx - R), Y(cy - R), X(cx + R), Y(cy + R)), outline=O)
    d.ellipse((X(cx - R + 1), Y(cy - R + 1), X(cx + R - 1), Y(cy + R - 1)), outline=RIML)
    d.ellipse((X(cx - R + 2), Y(cy - R + 2), X(cx + R - 2), Y(cy + R - 2)), outline=RIM)
    for a in range(0, 360, 36):
        t = math.radians(a)
        bx, by = round(cx + math.cos(t) * (R + 1)), round(cy + math.sin(t) * (R + 1))
        d.rectangle((X(bx - 1), Y(by - 1), X(bx + 1), Y(by + 1)), fill=RIML, outline=O)
    d.ellipse((X(cx - 2), Y(cy - 2), X(cx + 2), Y(cy + 2)), fill=HUB)
    for x in range(32, 42):                       # 바퀴 위 눈
        if abs(x - cx) < 4: px[X(x), Y(cy - R - 2)] = SN; px[X(x), Y(cy - R - 1)] = SNS if abs(x - cx) > 2 else SN
    # 10/27 바퀴 아래 물길은 뺌 (기우 피드백). 눈 위 그림자만
    d.line((X(30), Y(44), X(43), Y(44)), fill=SNS)
