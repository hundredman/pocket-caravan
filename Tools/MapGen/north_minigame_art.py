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


def small_icons():
    """이끼 더미, 반짝이는 주머니, 전시대, 전시품(호박·은빛 뿔·룬 탁본), 리액션 책, 오로라 병"""
    out = {}
    im = Image.new("RGBA", (14, 8), (0, 0, 0, 0)); d = ImageDraw.Draw(im)        # 이끼 한 줌 (연두·회녹)
    d.ellipse((0, 2, 13, 7), fill=(126, 156, 92, 255), outline=(74, 98, 56, 255))
    for (x, y) in [(3, 3), (6, 2), (9, 3), (5, 5), (10, 5)]:
        d.point((x, y), fill=(176, 204, 128, 255))
    out["moss"] = im
    im = Image.new("RGBA", (10, 10), (0, 0, 0, 0)); d = ImageDraw.Draw(im)       # 끈 묶은 주머니
    d.ellipse((1, 3, 8, 9), fill=(196, 150, 96, 255), outline=(120, 84, 52, 255))
    d.rectangle((3, 1, 6, 3), fill=(176, 130, 80, 255), outline=(120, 84, 52, 255)); d.line((2, 3, 7, 3), fill=(190, 58, 52, 255))
    d.point((3, 5), fill=(240, 210, 150, 255))
    out["pouch"] = im
    im = Image.new("RGBA", (16, 10), (0, 0, 0, 0)); d = ImageDraw.Draw(im)       # 나무 전시대
    d.rectangle((1, 2, 14, 4), fill=(170, 112, 66, 255), outline=(100, 64, 40, 255))
    d.rectangle((3, 5, 12, 9), fill=(140, 92, 56, 255), outline=(100, 64, 40, 255))
    d.line((2, 2, 13, 2), fill=(200, 146, 96, 255))
    out["pedestal"] = im
    im = Image.new("RGBA", (10, 10), (0, 0, 0, 0)); d = ImageDraw.Draw(im)       # 호박
    d.polygon(((5, 0), (9, 4), (7, 9), (2, 9), (0, 4)), fill=(232, 150, 40, 255), outline=(150, 84, 20, 255))
    d.point((4, 3), fill=(255, 226, 150, 255)); d.point((3, 4), fill=(255, 210, 120, 255))
    out["amber"] = im
    im = Image.new("RGBA", (14, 12), (0, 0, 0, 0)); d = ImageDraw.Draw(im)       # 은빛 순록 뿔
    for seg in [((7, 11), (7, 6)), ((7, 6), (3, 2)), ((7, 6), (11, 2)), ((5, 4), (4, 0)), ((9, 4), (10, 0)), ((3, 2), (1, 1)), ((11, 2), (13, 1))]:
        d.line(seg[0] + seg[1], fill=(210, 230, 240, 255))
    d.point((7, 8), fill=(255, 255, 255, 255))
    out["antler"] = im
    im = Image.new("RGBA", (12, 14), (0, 0, 0, 0)); d = ImageDraw.Draw(im)       # 룬 탁본 (종이에 찍은 룬)
    d.rectangle((0, 0, 11, 13), fill=(236, 226, 196, 255), outline=(150, 130, 96, 255))
    for i, g in enumerate(["101", "111", "010", "110"]):
        for j, ch in enumerate(g):
            if ch == "1":
                d.point((3 + j * 2, 2 + i * 3), fill=(60, 52, 60, 255)); d.point((3 + j * 2, 3 + i * 3), fill=(60, 52, 60, 255))
    out["rubbing"] = im
    im = Image.new("RGBA", (10, 12), (0, 0, 0, 0)); d = ImageDraw.Draw(im)       # 리액션 책
    d.rectangle((0, 1, 9, 11), fill=(124, 70, 140, 255), outline=(70, 40, 80, 255)); d.line((1, 1, 1, 11), fill=(232, 196, 110, 255))
    d.rectangle((3, 4, 7, 6), fill=(236, 226, 196, 255))
    out["book"] = im
    im = Image.new("RGBA", (8, 12), (0, 0, 0, 0)); d = ImageDraw.Draw(im)        # 오로라 병
    d.rectangle((2, 0, 5, 2), fill=(150, 110, 80, 255)); d.ellipse((0, 2, 7, 11), fill=(90, 220, 170, 255), outline=(40, 110, 100, 255))
    d.point((2, 5), fill=(220, 255, 240, 255)); d.line((1, 8, 6, 7), fill=(150, 255, 210, 255))
    out["bottle"] = im
    im = Image.new("RGBA", (12, 6), (0, 0, 0, 0)); d = ImageDraw.Draw(im)        # 황금 미끼
    d.ellipse((0, 1, 9, 5), fill=(240, 200, 70, 255), outline=(150, 110, 30, 255)); d.polygon(((9, 3), (11, 0), (11, 5)), fill=(240, 200, 70, 255))
    out["bait"] = im
    return out


def char_frames(root, src, prefix):
    """다른 캐릭터 그림을 런타임용으로 복사 (방향 d/l/r/u × 2프레임)"""
    out = {}
    base = os.path.join(root, "Assets/PocketCaravan/Art/Characters/" + src + "_tall_{}{}.png")
    for d_ in "dlru":
        for i in (0, 1):
            pth = base.format(d_, i)
            if os.path.exists(pth):
                out[f"{prefix}_{d_}{i}"] = Image.open(pth).convert("RGBA")
    return out


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


def fair_art():
    """10/26 큰 장: 줄무늬 천막 좌판(3x2칸)과 삼각 깃발 줄(4칸)"""
    out = {}
    im = Image.new("RGBA", (48, 32), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for i in range(6):                                   # 빨강·크림 줄무늬 천막
        d.rectangle((i * 8, 2, i * 8 + 7, 11), fill=(196, 64, 52, 255) if i % 2 == 0 else (240, 226, 196, 255))
    for i in range(6):                                   # 천막 끝 물결
        d.pieslice((i * 8, 7, i * 8 + 7, 15), 0, 180, fill=(196, 64, 52, 255) if i % 2 == 0 else (240, 226, 196, 255))
    d.line((0, 2, 47, 2), fill=(120, 40, 34, 255))
    d.rectangle((2, 12, 4, 31), fill=(110, 78, 50, 255)); d.rectangle((43, 12, 45, 31), fill=(110, 78, 50, 255))   # 기둥
    d.rectangle((1, 21, 46, 26), fill=(150, 108, 66, 255), outline=(98, 68, 42, 255))                          # 판대
    for (x, c) in [(7, (236, 186, 60)), (14, (210, 90, 60)), (21, (150, 160, 150)), (28, (126, 156, 92)), (35, (120, 86, 60)), (40, (230, 120, 80))]:
        d.ellipse((x, 17, x + 5, 22), fill=c + (255,), outline=(70, 50, 30, 255))                             # 물건
    out["fair_stall"] = im
    im = Image.new("RGBA", (64, 12), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.arc((-8, -10, 72, 8), 20, 160, fill=(90, 70, 50, 255))
    cols = [(196, 64, 52), (236, 186, 60), (70, 120, 170), (126, 156, 92)]
    for i in range(8):
        x = 3 + i * 8; y = 3 + int(2 * (1 - abs(i - 3.5) / 3.5))
        d.polygon(((x, y), (x + 6, y), (x + 3, y + 7)), fill=cols[i % 4] + (255,), outline=(60, 40, 30, 255))
    out["fair_flags"] = im
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
        **small_icons(),
        **(char_frames(root, "north_p_trader", "wanderer") if root else {}),     # 떠돌이 상인
        **(char_frames(root, "north_fox_woman", "dealer") if root else {}),      # 오늘의 특가 좌판 장수
        **(char_frames(root, "north_owl_elder", "fortune") if root else {}),     # 룬 점쟁이
        **(char_frames(root, "north_p_fisher", "ferry") if root else {}),        # 나룻배 사공
        **fair_art(),                                                             # 10/26 큰 장 좌판·깃발
        **__import__("north_craft_art").game_sprites(),                          # 10/26 공방 마당 (덕장 틀·털옷 진열·양조장)
        **(char_frames(root, "north_p_herder", "craft_fish") if root else {}),   # 덕장 일꾼
        **(char_frames(root, "north_p_smith", "craft_fur") if root else {}),     # 털옷 장인
        **(char_frames(root, "north_p_granny", "craft_brew") if root else {}),   # 양조장 할머니
        **(char_frames(root, "north_p_girl", "fair_a") if root else {}),         # 큰 장 구경꾼
        **(char_frames(root, "north_p_oldman", "fair_b") if root else {}),
        **(char_frames(root, "north_p_boy", "fair_c") if root else {}),
        **(char_frames(root, "north_p_bard", "fair_d") if root else {}),
    }
