"""베네치아 운하 집 그림 (2026-10-26, 임시 아트 제안·팀 아트 승인 전).
기존 GPT 아틀라스(venice_atlas.png) 그림처럼 위에서 비스듬히 내려다본 큰 지붕 + 진한 외곽선 스타일.
높고 좁은 회벽 집: 테라코타 지붕, 깔때기 굴뚝, 뾰족 아치 창과 초록 덧창, 꽃 발코니, 아치 문·가게 차양.
같은 씨앗이면 같은 그림이 나온다.
    사용: python Tools/ArtGen/venice_houses_art.py [미리보기.png]
    출력: Tools/ArtGen/venice_houses.png + venice_houses.json (이름: [x, y, 너비, 높이] 칸)
    venice_tiles.py가 'vhse' 시트, 'vh_<이름>' 묶음으로 등록한다."""
import json, os, random, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_PNG = os.path.join(HERE, "venice_houses.png")
OUT_JSON = os.path.join(HERE, "venice_houses.json")

INK = (52, 44, 52, 255)
STONE = (236, 228, 208)        # 이스트리아 석재 창틀·띠
STONE_SH = (196, 186, 166)
GLASS = (46, 56, 78)
GLASS_HI = (120, 150, 176)
WOOD = (104, 68, 44)
WOOD_DK = (72, 46, 32)
SHUTTER = (74, 124, 92)
SHUTTER_DK = (52, 92, 68)
ROOF = ((222, 116, 70), (180, 82, 50), (244, 156, 104))     # 바탕·그늘·밝음
ROOF_OLD = ((196, 110, 78), (150, 80, 58), (222, 146, 108))
FLOWERS = [(226, 70, 90), (246, 140, 170), (250, 236, 240), (244, 180, 60)]
LEAF = (86, 140, 70)

# 회벽 색 (베네치아 집들의 황토·연어·장밋빛·크림·붉은 벽돌색·세이지·하늘색)
WALLS = {
    "ochre": (232, 184, 100), "salmon": (230, 146, 116), "rose": (224, 160, 160), "cream": (238, 218, 176),
    "brick": (196, 98, 74), "sage": (168, 182, 146), "sky": (160, 188, 200),
}


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (255,)


class Canvas:
    def __init__(self, w, h):
        self.im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.px = self.im.load()
        self.w, self.h = w, h

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[x, y] = c if len(c) == 4 else c + (255,)

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, c)

    def outline(self):
        """그림 가장자리를 진한 선으로 (안쪽 한 겹)"""
        a = [[self.px[x, y][3] > 0 for x in range(self.w)] for y in range(self.h)]
        for y in range(self.h):
            for x in range(self.w):
                if a[y][x] and any(not (0 <= x + dx < self.w and 0 <= y + dy < self.h) or not a[y + dy][x + dx]
                                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    self.px[x, y] = INK


def roof(cv, rng, x0, x1, top, eave, old=False):
    """비스듬히 보이는 우진각 지붕: 위는 좁고 처마는 넓다. 기와 줄무늬와 엇갈린 이음매."""
    base, dark, light = ROOF_OLD if old else ROOF
    depth = eave - top
    for y in range(top, eave + 1):
        inset = max(0, round((eave - y) * 3 / max(1, depth)))
        for x in range(x0 + inset, x1 - inset + 1):
            row = (y - top)
            c = base
            if row % 3 == 2:
                c = dark
            elif (x + (row // 3) * 2) % 4 == 0 and row % 3 == 1:
                c = dark
            if row == 0:
                c = light
            if x >= x1 - inset - 1 and row > 0:
                c = shade(c, .86)          # 오른쪽 경사면 그늘
            if rng.random() < .04:
                c = light
            cv.put(x, y, c)
    for x in range(x0, x1 + 1):            # 처마 그늘
        cv.put(x, eave, dark)


def chimney(cv, rng, cx, roof_y):
    """베네치아식 깔때기 굴뚝: 가는 기둥 위에 거꾸로 선 원뿔"""
    col = (226, 210, 180)
    cv.rect(cx, roof_y - 5, cx + 1, roof_y + 1, col)
    cv.put(cx + 1, roof_y - 4, shade(col, .8)); cv.put(cx + 1, roof_y - 3, shade(col, .8))
    cv.rect(cx - 2, roof_y - 8, cx + 3, roof_y - 7, col)
    cv.rect(cx - 1, roof_y - 6, cx + 2, roof_y - 6, col)
    cv.put(cx + 3, roof_y - 7, shade(col, .78)); cv.put(cx + 2, roof_y - 6, shade(col, .78))
    for x in (cx - 2, cx + 3):
        cv.put(x, roof_y - 9, INK)
    cv.rect(cx - 1, roof_y - 9, cx + 2, roof_y - 9, INK)


def wall(cv, rng, x0, x1, y0, y1, color):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c = color
            if x >= x1 - 1:
                c = shade(color, .85)              # 오른쪽 그늘
            r = rng.random()
            if r < .06:
                c = shade(c, .93)
            elif r < .09:
                c = shade(c, 1.05)
            cv.put(x, y, c)
    # 회벽이 벗겨져 벽돌이 보이는 자리
    for _ in range(rng.randrange(0, 3)):
        bx, by = rng.randrange(x0 + 2, max(x0 + 3, x1 - 5)), rng.randrange(y0 + 2, max(y0 + 3, y1 - 4))
        for yy in range(3):
            for xx in range(4):
                if (xx + yy) % 3 != 2:
                    cv.put(bx + xx, by + yy, (176, 96, 70) if (xx + yy * 2) % 4 else (150, 80, 60))


def window(cv, x, y, shutters, tall=7):
    """뾰족 아치 창 (창틀 포함 6×(tall+2)), x·y는 창틀 왼쪽 위"""
    cv.rect(x, y + 1, x + 5, y + tall + 1, STONE)
    cv.rect(x + 1, y, x + 4, y, STONE)
    cv.rect(x + 1, y + 2, x + 4, y + tall, GLASS)
    cv.put(x + 2, y + 1, GLASS); cv.put(x + 3, y + 1, GLASS)
    cv.put(x + 2, y + 2, GLASS_HI)
    cv.rect(x + 1, y + tall + 1, x + 4, y + tall + 1, STONE_SH)
    if shutters:
        for sx in (x - 2, x + 6):
            cv.rect(sx, y + 2, sx + 1, y + tall + 1, SHUTTER)
            cv.put(sx + (1 if sx < x else 0), y + 4, SHUTTER_DK)
            cv.put(sx + (1 if sx < x else 0), y + tall, SHUTTER_DK)


def flower_box(cv, rng, x0, x1, y):
    cv.rect(x0, y, x1, y + 1, WOOD)
    for x in range(x0, x1 + 1):
        if rng.random() < .8:
            cv.put(x, y - 1, LEAF)
        if rng.random() < .5:
            cv.put(x, y - 1 - (x % 2), rng.choice(FLOWERS))


def balcony(cv, rng, x0, x1, y):
    """돌 난간 발코니 (y는 바닥 판)"""
    cv.rect(x0, y, x1, y, STONE)
    cv.rect(x0, y + 1, x1, y + 1, STONE_SH)
    cv.rect(x0, y - 3, x1, y - 3, STONE)
    for x in range(x0, x1 + 1, 2):
        cv.rect(x, y - 2, x, y - 1, STONE)
    if rng.random() < .7:
        for x in range(x0, x1 + 1):
            if rng.random() < .55:
                cv.put(x, y - 4, rng.choice(FLOWERS + [LEAF, LEAF]))


def door(cv, x, y_bottom, w=6, h=11):
    """아치 문: 석재 틀 + 나무 문"""
    top = y_bottom - h
    cv.rect(x, top + 1, x + w - 1, y_bottom, STONE)
    cv.rect(x + 1, top, x + w - 2, top, STONE)
    cv.rect(x + 1, top + 2, x + w - 2, y_bottom, WOOD)
    cv.rect(x + 2, top + 1, x + w - 3, top + 1, WOOD)
    for yy in range(top + 3, y_bottom + 1, 2):
        cv.put(x + w // 2, yy, WOOD_DK)
    cv.put(x + w - 3, top + h // 2 + 1, (230, 190, 90))


def shop(cv, rng, x0, x1, y_bottom):
    """가게: 줄무늬 차양 + 진열창"""
    stripe = rng.choice([((200, 60, 60), (246, 240, 228)), ((60, 120, 90), (246, 240, 228)), ((60, 90, 160), (240, 220, 160))])
    top = y_bottom - 10
    cv.rect(x0, top + 3, x1, y_bottom, (60, 46, 40))
    for x in range(x0 + 1, x1):
        cv.put(x, top + 5 + (x % 2), rng.choice([(226, 184, 80), (210, 90, 70), (120, 170, 200), (240, 236, 220)]))
        cv.put(x, top + 8, rng.choice([(176, 120, 70), (226, 200, 140), (150, 190, 120)]))
    for x in range(x0 - 1, x1 + 2):
        c = stripe[((x - x0) // 2) % 2]
        cv.rect(x, top, x, top + 2, c)
        cv.put(x, top + 3, shade(c, .7) if (x - x0) % 2 == 0 else (0, 0, 0, 0))


def house(seed, wt, ht, color, kind="house"):
    """wt×ht 칸 집 한 채. kind: house / shop / palazzo / narrow"""
    rng = random.Random(seed)
    W, H = wt * 16, ht * 16
    cv = Canvas(W, H)
    x0, x1 = 1, W - 2
    top = 9                                # 굴뚝 자리
    eave = top + (14 if ht <= 4 else 16)
    base = H - 1
    roof(cv, rng, x0, x1, top, eave, old=rng.random() < .35)
    wall(cv, rng, x0 + 1, x1 - 1, eave + 1, base - 2, WALLS[color])
    cv.rect(x0 + 1, eave + 1, x1 - 1, eave + 1, STONE)                 # 처마 밑 돌림띠
    cv.rect(x0 + 1, eave + 2, x1 - 1, eave + 2, shade(WALLS[color], .7))
    cv.rect(x0 + 1, base - 1, x1 - 1, base, STONE_SH)                 # 석재 기단
    for n in range(rng.randrange(1, 3 if wt >= 3 else 2)):
        chimney(cv, rng, x0 + 3 + rng.randrange(0, max(1, W - 10)), top + 2 + rng.randrange(0, 3))

    ground_top = base - 17
    cv.rect(x0 + 1, ground_top, x1 - 1, ground_top, STONE)             # 층 사이 띠
    inner = (x1 - 1) - (x0 + 1) + 1
    # 위층 창: 칸마다 하나 (덧창 자리 포함 10px 간격)
    floors = []
    y = eave + 4
    while y + 10 <= ground_top - 1:
        floors.append(y)
        y += 12
    if kind == "palazzo" and floors:
        # 피아노 노빌레: 뾰족 아치 여러 개가 붙은 창 + 긴 발코니
        f = floors[0]
        n = max(3, inner // 6 - 1)
        start = x0 + 1 + (inner - n * 5 - 1) // 2
        cv.rect(start - 1, f, start + n * 5, f + 9, STONE)
        for i in range(n):
            wx = start + i * 5
            cv.rect(wx + 1, f + 2, wx + 3, f + 8, GLASS)
            cv.put(wx + 2, f + 1, GLASS)
            cv.put(wx + 1, f + 3, GLASS_HI)
        balcony(cv, rng, start - 2, start + n * 5 + 1, f + 11)
        floors = floors[1:]
    count = max(1, (inner + 2) // 10)
    gap = (inner - count * 6) // (count + 1)
    for i, fy in enumerate(floors):
        shutters = rng.random() < .65 and gap >= 3
        for c in range(count):
            wx = x0 + 1 + gap + c * (6 + gap)
            window(cv, wx, fy, shutters)
            if rng.random() < .35 and i == len(floors) - 1:
                flower_box(cv, rng, wx, wx + 5, fy + 9)
        if kind != "palazzo" and i == 0 and rng.random() < .5 and count >= 1:
            wx = x0 + 1 + gap
            balcony(cv, rng, wx - 1, wx + 6, fy + 11)
    # 아래층
    if kind == "shop" and inner >= 20:
        shop(cv, rng, x0 + 3, x0 + 3 + inner // 2, base - 2)
        door(cv, x1 - 9, base - 2)
    elif kind == "palazzo":
        n = max(2, inner // 9)
        step = inner // n
        for i in range(n):
            door(cv, x0 + 1 + i * step + (step - 7) // 2, base - 2, w=7, h=12)
    else:
        dx = x0 + 1 + (inner - 6) // 2 if inner < 30 else x0 + 4 + rng.randrange(0, inner - 12)
        door(cv, dx, base - 2)
        if inner >= 26:
            wx = x1 - 9 if dx < W // 2 else x0 + 4
            window(cv, wx, ground_top + 4, rng.random() < .5, tall=6)
    # 빨래줄 (가끔)
    if rng.random() < .2 and floors:
        ly = floors[-1] + 10
        for x in range(x0 + 2, x1 - 1):
            cv.put(x, ly + (1 if x0 + 5 < x < x1 - 5 else 0), (90, 80, 80))
        for i in range(3):
            lx = x0 + 5 + i * (inner // 4)
            cv.rect(lx, ly + 2, lx + 2, ly + 4, rng.choice([(246, 246, 240), (120, 160, 210), (220, 110, 110)]))
    cv.outline()
    return cv.im


BRICK_W = ((186, 96, 70), (150, 76, 58), (214, 128, 96))     # 성벽 벽돌: 바탕·줄눈·밝음


def bricks(cv, x0, y0, x1, y1, base=BRICK_W):
    b, joint, hi = base
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            row = (y - y0) // 3
            c = joint if (y - y0) % 3 == 2 or (x + row * 3) % 6 == 0 else b
            if c is b and (x * 7 + y * 3) % 11 == 0:
                c = hi
            cv.put(x, y, c)


def merlons(cv, x0, x1, y, step=6):
    """제비꼬리 성가퀴 (베네토 성곽): 위가 V자로 갈라진 돌출"""
    for x in range(x0, x1 + 1, step):
        cv.rect(x, y, x + 3, y + 3, BRICK_W[0])
        cv.put(x + 1, y, (0, 0, 0, 0)); cv.put(x + 2, y, (0, 0, 0, 0))
        cv.put(x + 3, y + 3, BRICK_W[1])


def wall_v():
    """세로 성벽 한 조각 2×1칸: 위에서 본 성벽 길(돌)과 양쪽 성가퀴. 위아래로 이어 붙여도 이음매가 없다."""
    cv = Canvas(32, 16)
    bricks(cv, 0, 0, 31, 15)
    cv.rect(7, 0, 24, 15, (200, 186, 160))                      # 성벽 위 돌길
    for y in range(0, 16, 4):
        cv.rect(7, y, 24, y, (176, 162, 138))
    for x in (3, 26):
        for y in range(1, 16, 8):
            cv.rect(x, y, x + 2, y + 4, BRICK_W[2])
            cv.put(x + 1, y, BRICK_W[1])
    for y in range(16):
        cv.put(0, y, INK); cv.put(31, y, INK); cv.put(6, y, BRICK_W[1]); cv.put(25, y, BRICK_W[1])
    return cv.im


def tower(seed):
    """망루 4×6칸: 벽돌 몸체, 화살 구멍, 제비꼬리 성가퀴, 아래 돌 기단"""
    rng = random.Random(seed)
    cv = Canvas(64, 96)
    top = 10
    bricks(cv, 2, top + 4, 61, 93)
    for x in range(2, 62):                         # 오른쪽 그늘
        pass
    for y in range(top + 4, 94):
        for x in range(54, 62):
            r, g, b, a = cv.px[x, y]
            cv.px[x, y] = (int(r * .82), int(g * .82), int(b * .82), a)
    cv.rect(0, top, 63, top + 5, (200, 186, 160))               # 성가퀴 아래 돌림띠
    cv.rect(0, top + 5, 63, top + 6, (150, 136, 116))
    merlons(cv, 1, 60, top - 4, step=7)
    for y in range(top + 16, 80, 20):                          # 화살 구멍
        for x in (16, 30, 44):
            cv.rect(x, y, x + 1, y + 7, (40, 34, 40))
            cv.rect(x - 1, y + 3, x + 2, y + 3, (40, 34, 40))
    cv.rect(2, 88, 61, 95, STONE_SH)                             # 돌 기단
    for x in range(2, 62, 5):
        cv.put(x, 90, (170, 160, 140))
    # 사자 문장 깃발
    cv.rect(31, 2, 31, top + 3, WOOD_DK)
    cv.rect(32, 2, 38, 6, (196, 56, 60))
    cv.put(35, 4, (240, 200, 90)); cv.put(37, 6, (0, 0, 0, 0))
    cv.outline()
    return cv.im


# 이름: (씨앗, 너비 칸, 높이 칸, 벽색, 종류)
HOUSES = {}
_specs = [
    ("narrow", 2, 4, "house"), ("narrow_tall", 2, 5, "house"),
    ("house", 3, 4, "house"), ("house_tall", 3, 5, "house"), ("shop", 3, 4, "shop"),
    ("wide", 4, 4, "house"), ("wide_shop", 4, 4, "shop"), ("palazzo", 5, 5, "palazzo"),
]
_colors = list(WALLS)
for i, (base, w, h, kind) in enumerate(_specs):
    picks = _colors if kind != "palazzo" else ["rose", "cream", "salmon", "ochre"]
    for j, col in enumerate(picks):
        if kind in ("shop", "wide_shop") and col in ("brick", "sky"):
            continue
        HOUSES[f"{base}_{col}"] = (1000 + i * 37 + j * 11, w, h, col, kind)


def build():
    names = list(HOUSES)
    cols = 16                          # 시트 한 줄에 놓을 칸 수
    layout, x, y, rowh = {}, 0, 0, 0
    for n in names:
        _, w, h, _, _ = HOUSES[n]
        if x + w > cols:
            x, y, rowh = 0, y + rowh, 0
        layout[n] = [x, y, w, h]
        x += w
        rowh = max(rowh, h)
    extra = {"wall_v": (wall_v(), 2, 1), "tower": (tower(7), 4, 6)}     # 동문 성벽·망루
    for n, (im, w, h) in extra.items():
        if x + w > cols:
            x, y, rowh = 0, y + rowh, 0
        layout[n] = [x, y, w, h]
        x += w
        rowh = max(rowh, h)
    sheet = Image.new("RGBA", (cols * 16, (y + rowh) * 16), (0, 0, 0, 0))
    for n, (x, y, w, h) in layout.items():
        sheet.alpha_composite(extra[n][0] if n in extra else house(*HOUSES[n]), (x * 16, y * 16))
    sheet.save(OUT_PNG)
    with open(OUT_JSON, "w", encoding="utf-8", newline="\n") as fp:
        json.dump(layout, fp, ensure_ascii=False, indent=1)
    return sheet, layout


if __name__ == "__main__":
    sheet, layout = build()
    print("houses", len(layout), "sheet", sheet.size)
    if len(sys.argv) > 1:
        bg = Image.new("RGBA", sheet.size, (150, 140, 120, 255))
        bg.alpha_composite(sheet)
        bg.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(sys.argv[1])
