"""돌아다니는 순록 (10/25 다시 그림): 26x24 옆모습(오른쪽을 봄), 걷기 4프레임 + 풀 뜯기 1프레임.
점을 직접 찍고, 바깥에 짙은 갈색 테두리를 자동으로 두른다. 빛은 왼쪽 위.
- 큰 몸통(위는 밝은 털, 배는 흰 털), 목 아래로 늘어진 흰 갈기, 짧은 흰 꼬리
- 길쭉한 머리에 밝은 주둥이·검은 코, 옆으로 뻗은 귀, 뒤로 휘어 갈라진 큰 뿔(끝이 밝음)
- 다리는 앞뒤가 엇갈려 움직이고, 발굽은 짙은 색"""
from PIL import Image

W, H = 26, 24
C = {
    "f": (138, 98, 70), "F": (172, 130, 94), "d": (106, 72, 50),      # 털, 밝은 털, 그늘
    "w": (242, 236, 222), "W": (212, 202, 184),                       # 흰 털(갈기·배·꼬리), 흰 털 그늘
    "m": (226, 214, 196), "n": (34, 26, 30), "e": (30, 22, 26),       # 주둥이, 코, 눈
    "a": (224, 204, 160), "A": (252, 242, 214), "b": (170, 140, 98),  # 뿔, 뿔 끝, 뿔 밑동
    "k": (96, 66, 48), "K": (70, 48, 36), "s": (40, 32, 32),          # 앞쪽 다리, 뒤쪽 다리, 발굽
}
OUT = (52, 34, 28)
THIN = set("abAkKs")    # 뿔·다리는 테두리 없이 가늘게 (테두리를 두르면 뭉개짐)


def blank():
    return [["."] * W for _ in range(H)]


def put(g, x, y, ch):
    if 0 <= x < W and 0 <= y < H:
        g[y][x] = ch


def rect(g, x0, y0, x1, y1, ch):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(g, x, y, ch)


def torso(g):
    # 몸통 (x 4~17, y 11~16), 모서리 둥글게. 위는 밝게, 배는 흰 털, 엉덩이 쪽 그늘
    for y in range(11, 17):
        x0, x1 = (5, 16) if y in (11, 16) else (4, 17)
        for x in range(x0, x1 + 1):
            ch = "F" if y == 11 or (y == 12 and x < 13) else "w" if y == 16 and 7 <= x <= 15 else "d" if (y == 15 and x < 7) else "f"
            put(g, x, y, ch)
    put(g, 3, 11, "w"); put(g, 3, 12, "w"); put(g, 2, 11, "W")       # 짧은 흰 꼬리


def antler(g, x, y):
    """(x, y) = 뿔 밑동. 뒤로 휘는 큰 줄기 두 개에서 가지가 위로 갈라진다"""
    trunk_back = [(x, y), (x - 1, y - 1), (x - 2, y - 2), (x - 3, y - 3), (x - 4, y - 4), (x - 5, y - 4)]
    trunk_front = [(x + 1, y), (x + 1, y - 1), (x + 2, y - 2), (x + 2, y - 3), (x + 3, y - 4), (x + 3, y - 5)]
    tines = [(x - 1, y - 2), (x - 1, y - 3), (x, y - 4), (x, y - 5),          # 뒷줄기에서 위로
             (x - 3, y - 4), (x - 3, y - 5), (x - 2, y - 6),
             (x + 3, y - 1), (x + 4, y - 2),                                    # 앞으로 뻗은 눈가지
             (x + 2, y - 5), (x + 2, y - 6), (x + 4, y - 6)]
    for (px, py) in trunk_back + trunk_front:
        put(g, px, py, "a")
    for (px, py) in tines:
        put(g, px, py, "a")
    for (px, py) in [(x - 5, y - 4), (x - 2, y - 6), (x, y - 5), (x + 2, y - 6), (x + 4, y - 6), (x + 3, y - 5), (x + 4, y - 2)]:
        put(g, px, py, "A")                                                     # 끝은 밝게
    put(g, x, y, "b"); put(g, x + 1, y, "b")


def head_up(g):
    rect(g, 15, 8, 18, 12, "f"); put(g, 15, 8, "F"); put(g, 16, 8, "F")       # 목 (비스듬히 위로)
    rect(g, 15, 12, 17, 15, "w"); put(g, 16, 16, "w"); put(g, 15, 15, "W")    # 늘어진 흰 갈기
    rect(g, 17, 6, 21, 9, "f"); put(g, 17, 6, "F"); put(g, 18, 6, "F")        # 머리
    rect(g, 21, 7, 24, 9, "m"); put(g, 24, 7, "n"); put(g, 24, 8, "n"); put(g, 22, 9, "W")   # 길쭉한 주둥이·코
    put(g, 19, 7, "e")
    put(g, 16, 6, "f"); put(g, 15, 6, "d"); put(g, 14, 5, "d")               # 옆으로 뻗은 귀
    antler(g, 18, 5)


def head_down(g):
    rect(g, 16, 11, 19, 15, "f"); put(g, 16, 11, "F")                         # 목이 앞으로 숙여짐
    rect(g, 16, 14, 18, 16, "w")                                               # 갈기
    rect(g, 19, 15, 22, 18, "f"); put(g, 19, 15, "F")                         # 머리 (땅 가까이)
    rect(g, 22, 17, 24, 19, "m"); put(g, 24, 19, "n"); put(g, 25, 19, "n")
    put(g, 20, 16, "e")
    put(g, 18, 14, "d"); put(g, 17, 13, "d")                                  # 귀
    antler(g, 20, 14)


LEGS = [   # 다리 네 개 (x, 무릎 아래 기울기): 뒤(먼 쪽), 뒤(가까운 쪽), 앞(먼 쪽), 앞(가까운 쪽)
    ((6, 0), (8, 0), (14, 0), (16, 0)),
    ((5, -1), (9, 1), (15, 1), (15, -1)),
    ((6, 0), (8, 0), (14, 0), (16, 0)),
    ((7, 1), (7, -1), (13, -1), (17, 1)),
]


def legs(g, frame):
    for i, (x, tilt) in enumerate(LEGS[frame]):
        near = i in (1, 3)
        for k, y in enumerate(range(17, 23)):
            xx = x + (tilt if k >= 3 else 0)
            put(g, xx, y, "s" if y == 22 else ("k" if near else "K"))
            if k < 2:
                put(g, xx + (1 if near else -1), y, "k" if near else "K")      # 허벅지는 두껍게


def outline(g):
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()
    filled = {(x, y) for y in range(H) for x in range(W) if g[y][x] != "."}
    body_px = {(x, y) for (x, y) in filled if g[y][x] not in THIN}
    for (x, y) in filled:
        px[x, y] = C[g[y][x]] + (255,)
    for y in range(H):
        for x in range(W):
            if (x, y) in filled:
                continue
            if any((x + a, y + b) in body_px for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = OUT + (255,)
    return img


def frames():
    """{"walk": [4장], "graze": 1장} — 오른쪽을 봄"""
    walk = []
    for f in range(4):
        g = blank()
        legs(g, f); torso(g); head_up(g)
        img = outline(g)
        walk.append(img)
    g = blank(); legs(g, 0); torso(g); head_down(g)
    return {"walk": walk, "graze": outline(g)}


if __name__ == "__main__":
    import sys
    fr = frames(); S = 10
    sheet = Image.new("RGBA", (5 * (W + 2) * S, (H + 2) * S), (120, 140, 170, 255))
    for i, im in enumerate(fr["walk"] + [fr["graze"]]):
        sheet.alpha_composite(im.resize((W * S, H * S), Image.NEAREST), (i * (W + 2) * S, S))
    sheet.save(sys.argv[1])
