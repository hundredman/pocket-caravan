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
    img = Image.new("RGBA", (64, 32), (0, 0, 0, 0))
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
    img.save(PATH)
    return PATH


if __name__ == "__main__":
    print(make())
