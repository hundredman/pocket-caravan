"""북유럽 미니게임 그림 (10/24). export_north.py가 Resources/North/Minigame/에 저장한다.
썰매(순록이 탄 나무 썰매, 뒤에서 본 모습), 코스 장애물(눈 덮인 전나무·바위·통나무), 눈송이·룬 보너스, 하트, 물고기 3종, 찌."""
import os
from PIL import Image, ImageDraw
from tiles import tile_image, K
import north_art


def compose(cells, w, h):
    img = Image.new("RGBA", (w * 16, h * 16), (0, 0, 0, 0))
    for (dx, dy), key in cells.items():
        img.alpha_composite(tile_image(key), (dx * 16, dy * 16))
    return img


def block(sheet, x, y, w, h):
    return compose({(a, b): K(sheet, x + a, y + b) for a in range(w) for b in range(h)}, w, h)


def sled():
    """24x32: 위에서 내려다본 썰매에 앉은 순록 (뒤통수·뿔·목도리), 썰매 날 두 줄"""
    im = Image.new("RGBA", (24, 32), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    WOOD, WOOD_D, RED, RUN = (170, 112, 66), (120, 76, 44), (196, 64, 52), (70, 62, 70)
    BODY, BODY_D, FUR, ANT = (150, 104, 70), (112, 76, 52), (238, 232, 222), (220, 196, 150)
    d.line((4, 6, 4, 31), fill=RUN); d.line((19, 6, 19, 31), fill=RUN)          # 썰매 날
    d.arc((2, 0, 8, 10), 180, 270, fill=RUN); d.arc((15, 0, 21, 10), 270, 360, fill=RUN)
    d.rectangle((5, 12, 18, 30), fill=WOOD, outline=WOOD_D)                       # 썰매 바닥
    for y in (15, 19, 23, 27):
        d.line((6, y, 17, y), fill=WOOD_D)
    d.rectangle((5, 28, 18, 30), fill=RED)                                        # 뒤 등받이 띠
    d.ellipse((7, 11, 16, 24), fill=BODY, outline=BODY_D)                         # 순록 몸 (뒤에서)
    d.ellipse((8, 5, 15, 13), fill=BODY, outline=BODY_D)                          # 머리
    d.rectangle((8, 12, 15, 14), fill=FUR)                                        # 흰 목도리
    d.line((9, 6, 6, 1), fill=ANT); d.line((7, 3, 5, 3), fill=ANT)                # 뿔
    d.line((14, 6, 17, 1), fill=ANT); d.line((16, 3, 18, 3), fill=ANT)
    d.point((7, 9), fill=BODY_D); d.point((16, 9), fill=BODY_D)                   # 귀
    return im


def flake():
    im = Image.new("RGBA", (9, 9), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    C, L = (150, 220, 255, 255), (255, 255, 255, 255)
    d.line((4, 0, 4, 8), fill=C); d.line((0, 4, 8, 4), fill=C); d.line((1, 1, 7, 7), fill=C); d.line((1, 7, 7, 1), fill=C)
    d.point((4, 4), fill=L)
    return im


def heart(full=True):
    im = Image.new("RGBA", (9, 8), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    c = (220, 60, 70, 255) if full else (90, 80, 100, 255)
    d.polygon(((0, 2), (2, 0), (4, 2), (6, 0), (8, 2), (8, 3), (4, 7), (0, 3)), fill=c)
    d.point((2, 2), fill=(255, 200, 200, 255) if full else c)
    return im


def fish(body, belly, fin, spots=None, w=16, h=8):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.ellipse((1, 1, w - 5, h - 2), fill=body)
    d.rectangle((3, h // 2, w - 7, h - 3), fill=belly)
    d.polygon(((w - 5, h // 2), (w - 1, 0), (w - 1, h - 1)), fill=fin)              # 꼬리
    d.point((3, 2), fill=(20, 20, 30, 255))                                         # 눈
    for s in spots or []:
        d.point(s, fill=fin)
    return im


def bobber():
    im = Image.new("RGBA", (6, 9), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.line((3, 0, 3, 2), fill=(60, 50, 50, 255))
    d.ellipse((0, 2, 5, 8), fill=(230, 60, 50, 255)); d.rectangle((0, 5, 5, 8), fill=(250, 250, 250, 255))
    d.ellipse((0, 2, 5, 8), outline=(80, 30, 30, 255))
    return im


def cargo_sled(horizontal):
    """짐 싣는 순록 썰매 (플레이어 뒤에 끌려옴). 가로: 20x12 옆모습, 세로: 14x18 위에서 본 모습. 모피 짐 위에 붉은 끈"""
    WOOD, WOOD_D, RUN, FUR, FUR_D, RED, SACK = (170, 112, 66, 255), (110, 70, 40, 255), (70, 62, 70, 255), (196, 160, 120, 255), (150, 118, 86, 255), (196, 64, 52, 255), (214, 196, 150, 255)
    if horizontal:
        im = Image.new("RGBA", (20, 12), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
        d.line((1, 11, 18, 11), fill=RUN); d.arc((14, 6, 20, 12), 270, 90, fill=RUN)          # 날 (앞쪽이 휘어 올라감)
        d.rectangle((2, 7, 17, 9), fill=WOOD, outline=WOOD_D)
        for x in (4, 9, 14): d.line((x, 10, x, 11), fill=WOOD_D)
        d.ellipse((3, 1, 10, 8), fill=FUR, outline=FUR_D); d.ellipse((8, 2, 15, 8), fill=SACK, outline=FUR_D)
        d.line((2, 5, 16, 5), fill=RED)
    else:
        im = Image.new("RGBA", (14, 18), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
        d.line((1, 2, 1, 17), fill=RUN); d.line((12, 2, 12, 17), fill=RUN)
        d.rectangle((2, 3, 11, 16), fill=WOOD, outline=WOOD_D)
        d.ellipse((3, 4, 10, 10), fill=FUR, outline=FUR_D); d.ellipse((3, 9, 10, 15), fill=SACK, outline=FUR_D)
        d.line((2, 9, 11, 9), fill=RED)
    return im


def merchant_frames(root):
    """눈보라에 길 잃은 상인 (스라소니 상인 그림을 그대로 씀): 방향 d/l/r/u × 2프레임"""
    out = {}
    base = os.path.join(root, "Assets/PocketCaravan/Art/Characters/north_lynx_tall_{}{}.png")
    for d_ in "dlru":
        for i in (0, 1):
            pth = base.format(d_, i)
            if os.path.exists(pth):
                out[f"merchant_{d_}{i}"] = Image.open(pth).convert("RGBA")
    return out


def make(root=None):
    """{파일 이름: 그림}"""
    art = Image.open(north_art.PATH).convert("RGBA")
    return {
        "sled": sled(),
        "tree": block("nature", 10, 0, 2, 2),          # 눈 덮인 전나무
        "tree2": block("nature", 8, 0, 2, 2),          # 눈 덜 덮인 전나무
        "tree3": block("nature", 12, 0, 2, 2),         # 눈 덮인 둥근 나무
        "rock": block("nature", 2, 12, 2, 2),          # 눈 얹힌 바위
        "log": block("camp", 0, 0, 2, 1),              # 쓰러진 통나무 (뛰어넘기)
        "rune": art.crop((0, 0, 16, 32)),              # 룬돌 (큰 보너스)
        "flake": flake(),
        "heart": heart(True), "heart_empty": heart(False),
        "fish_cod": fish((150, 160, 150, 255), (220, 224, 210, 255), (110, 120, 110, 255), [(6, 2), (8, 3)]),
        "fish_char": fish((60, 90, 110, 255), (232, 120, 80, 255), (210, 90, 60, 255), [(6, 2), (9, 2), (7, 4)]),
        "fish_salmon": fish((236, 186, 60, 255), (255, 232, 150, 255), (200, 130, 40, 255), [(5, 2), (8, 2), (10, 3)], w=20, h=9),
        "bobber": bobber(),
        "cargo_h": cargo_sled(True), "cargo_v": cargo_sled(False),
        **(merchant_frames(root) if root else {}),
    }
