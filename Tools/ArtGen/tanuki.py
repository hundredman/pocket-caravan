"""플레이어 캐릭터: 갓 쓴 너구리 행상 (16x24, 4방향 x 4프레임). 글자 그림으로 직접 그린다."""
import os
from PIL import Image

PAL = {
    ".": None,
    "o": (58, 40, 34), "h": (40, 38, 48), "H": (78, 76, 94),
    "f": (182, 126, 84), "F": (128, 84, 56), "l": (244, 226, 196),
    "m": (78, 54, 42), "e": (28, 22, 22), "w": (255, 255, 255), "n": (44, 30, 26),
    "p": (240, 160, 150), "c": (246, 241, 230), "C": (214, 205, 190),
    "b": (104, 132, 186), "k": (92, 64, 48), "t": (60, 42, 34),
}

HEAD_DOWN = [
    "................",
    "......oooo......",
    ".....ohHHho.....",
    ".....ohhhho.....",
    ".ohhhhhhhhhhhho.",
    "oFoffffffffffoFo",
    ".ooffffffffffoo.",
    ".offmmffffmmffo.",
    ".ofmmewffewmmfo.",
    ".ofmmmllllmmmfo.",
    ".opflllnnlllfpo.",
    "..offllllllffo..",
    "...ooffffffoo...",
]
HEAD_UP = [
    "................",
    "......oooo......",
    ".....ohHHho.....",
    ".....ohhhho.....",
    ".ohhhhhhhhhhhho.",
    "oFoffffffffffoFo",
    ".ooffffffffffoo.",
    ".offffffffffffo.",
    ".offffffffffffo.",
    ".offffffffffffo.",
    ".offffffffffffo.",
    "..offFffffFffo..",
    "...ooffffffoo...",
]
HEAD_SIDE = [  # 오른쪽을 봄
    "................",
    ".......oooo.....",
    "......ohHHho....",
    "......ohhhho....",
    "..ohhhhhhhhhhho.",
    "..oFofffffffo...",
    "..offfffffffo...",
    "..offffffmmmfo..",
    "..offfffmewmllo.",
    "..offffffmmlllno",
    "..opfffffllllo..",
    "...offffflllo...",
    "....oooooooo....",
]
BODY_DOWN = [
    ".....occcco.....",
    "...occcCCccco...",
    "..occcccccccco..",
    ".ocbbbbbbbbbbco.",
    ".olcccccccccclo.",
    "..occcccccccco..",
    "..oCcccccccCco..",
    "...oCCCCCCCCo...",
]
BODY_UP = [
    ".....occcco.....",
    "...occcccccco...",
    "..occcccccccco..",
    ".ocbbbbbbbbbbco.",
    ".olcccccccccclo.",
    "..occcFtFtccco..",
    "..oCccFtFtcCco..",
    "...oCCtFtFCCo...",
]
BODY_SIDE = [
    "......occco.....",
    ".....occcCco....",
    "....occcccco....",
    "....obbbbbbo....",
    ".tFtocccccclo...",
    "tFtFoccccccco...",
    ".tFtoCcccccCo...",
    "....oCCCCCCo....",
]
# 다리: 4프레임 (서기, 왼발, 서기, 오른발)
LEGS_FRONT = [
    ["....okko..okko..", "....okko..okko..", "....oooo..oooo.."],
    ["....okko..okko..", "....okko...ooo..", "....oooo........"],
    ["....okko..okko..", "....okko..okko..", "....oooo..oooo.."],
    ["....okko..okko..", "....ooo...okko..", "..........oooo.."],
]
LEGS_SIDE = [
    [".....okkko......", ".....okkko......", ".....ooooo......"],
    ["....okko.okko...", "...okko...okko..", "...oooo...oooo.."],
    [".....okkko......", ".....okkko......", ".....ooooo......"],
    ["....okkookko....", "....okko.okko...", "....oooo.oooo..."],
]


def norm(rows):
    return [(r + "." * 16)[:16] for r in rows]


def frame(head, body, legs, bob):
    rows = norm(head) + norm(body) + norm(legs)
    assert len(rows) == 24, len(rows)
    if bob:  # 발을 디딜 때 머리와 몸이 한 픽셀 내려감
        rows = ["." * 16] + rows[:-4] + rows[-3:]
    img = Image.new("RGBA", (16, 24), (0, 0, 0, 0))
    px = img.load()
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            c = PAL.get(ch)
            if c:
                px[x, y] = c + (255,)
    return img


def build(out_dir, name="tanuki"):
    os.makedirs(out_dir, exist_ok=True)
    frames = {}
    for f in range(4):
        bob = f in (1, 3)
        frames[("d", f)] = frame(HEAD_DOWN, BODY_DOWN, LEGS_FRONT[f], bob)
        frames[("u", f)] = frame(HEAD_UP, BODY_UP, LEGS_FRONT[f], bob)
        r = frame(HEAD_SIDE, BODY_SIDE, LEGS_SIDE[f], bob)
        frames[("r", f)] = r
        frames[("l", f)] = r.transpose(Image.FLIP_LEFT_RIGHT)
    for (d, f), img in frames.items():
        img.save(os.path.join(out_dir, f"{name}_{d}{f}.png"))
    return frames


# ---------- 다른 동물: 같은 몸에 머리 윗부분(귀·모자)과 색만 바꾼다 ----------
TOPS = {
    # 모자 없이 둥근 귀 (곰)
    "round": {
        "d": ["................", "................", "..ooo......ooo..", ".oFFo......oFFo.", ".oFFooooooooFFo."],
        "u": ["................", "................", "..ooo......ooo..", ".oFFo......oFFo.", ".oFFooooooooFFo."],
        "r": ["................", "................", "...ooo..........", "..oFFo..........", "..oFFoooooooo..."],
    },
    # 뾰족 귀 (여우·개)
    "pointy": {
        "d": ["................", "..o..........o..", ".oFo........oFo.", ".oFFo......oFFo.", ".oFFFooooooFFFo."],
        "u": ["................", "..o..........o..", ".oFo........oFo.", ".oFFo......oFFo.", ".oFFFooooooFFFo."],
        "r": ["................", "....o...........", "...oFo..........", "...oFFo.........", "..oFFFoooooo...."],
    },
    # 긴 귀 (토끼)
    "long": {
        "d": ["...oo......oo...", "..oFlo....olFo..", "..oFlo....olFo..", "..oFlo....olFo..", ".ooFFoooooooFFoo"[:16]],
        "u": ["...oo......oo...", "..oFFo....oFFo..", "..oFFo....oFFo..", "..oFFo....oFFo..", ".ooFFoooooooFFoo"[:16]],
        "r": ["....oo..........", "...oFlo.........", "...oFlo.........", "...oFlo.........", "..ooFFooooooo..."],
    },
}

SPECIES = {
    # 이름: (팔레트 바꾸기, 윗부분(None=갓), 너구리 눈 무늬, 흰 수염)
    "elder":    ({"f": (150, 140, 128), "F": (104, 96, 88), "m": (70, 64, 60), "c": (230, 226, 240), "C": (196, 190, 210), "b": (150, 110, 170)}, None, True, True),
    "gaekju":   ({"f": (122, 82, 56), "F": (86, 56, 38), "l": (214, 180, 140), "c": (110, 140, 190), "C": (80, 106, 150), "b": (232, 196, 110)}, None, False, False),
    "merchant": ({"f": (226, 132, 64), "F": (176, 92, 42), "l": (250, 240, 226), "c": (120, 170, 110), "C": (90, 132, 84), "b": (210, 80, 70)}, "pointy", False, False),
    "dakmokchief": ({"f": (222, 196, 150), "F": (176, 148, 104), "l": (250, 244, 232), "c": (200, 190, 160), "C": (166, 156, 126), "b": (120, 100, 80)}, None, False, True),
    "villager_bear":   ({"f": (122, 82, 56), "F": (86, 56, 38), "l": (214, 180, 140), "c": (200, 150, 110), "C": (160, 116, 84)}, "round", False, False),
    "villager_rabbit": ({"f": (246, 242, 236), "F": (214, 206, 198), "l": (255, 220, 226), "c": (232, 150, 170), "C": (196, 116, 138), "b": (250, 230, 160)}, "long", False, False),
    "villager_fox":    ({"f": (226, 132, 64), "F": (176, 92, 42), "l": (250, 240, 226), "c": (170, 120, 200), "C": (132, 90, 160)}, "pointy", False, False),
    "villager_dog":    ({"f": (222, 196, 150), "F": (176, 148, 104), "l": (250, 244, 232), "c": (110, 160, 170), "C": (80, 124, 132)}, "pointy", False, False),
    "villager_tanuki": ({"c": (220, 190, 150), "C": (180, 150, 112), "b": (90, 120, 80)}, "round", True, False),
}


def species_rows(top, mask, beard, d):
    base = {"d": HEAD_DOWN, "u": HEAD_UP, "r": HEAD_SIDE}[d]
    rows = list(base)
    if top:
        t = TOPS[top]["d" if d == "d" else d]
        rows[0:5] = [(r + "." * 16)[:16] for r in t]
    if not mask:
        rows = [r.replace("m", "f").replace("ew", "ew") for r in rows]
    if beard and d != "u":
        rows[11] = rows[11].replace("ll", "ww", 1) if "ll" in rows[11] else rows[11]
    return rows


def build_species(out_dir):
    global PAL
    base_pal = dict(PAL)
    for name, (pal, top, mask, beard) in SPECIES.items():
        PAL = dict(base_pal); PAL.update(pal)
        frames = {}
        heads = {d: species_rows(top, mask, beard, d) for d in "dur"}
        for f in range(4):
            bob = f in (1, 3)
            frames[("d", f)] = frame(heads["d"], BODY_DOWN, LEGS_FRONT[f], bob)
            frames[("u", f)] = frame(heads["u"], BODY_UP if top != "long" else BODY_UP, LEGS_FRONT[f], bob)
            r = frame(heads["r"], BODY_SIDE, LEGS_SIDE[f], bob)
            frames[("r", f)] = r
            frames[("l", f)] = r.transpose(Image.FLIP_LEFT_RIGHT)
        for (d, f), img in frames.items():
            img.save(os.path.join(out_dir, f"{name}_tall_{d}{f}.png"))
    PAL = base_pal


def shadow(out_dir):
    """캐릭터 발밑 그림자 (반투명 타원)"""
    img = Image.new("RGBA", (12, 5), (0, 0, 0, 0))
    px = img.load()
    for y in range(5):
        for x in range(12):
            dx, dy = (x - 5.5) / 6, (y - 2) / 2.5
            if dx * dx + dy * dy <= 1:
                px[x, y] = (30, 20, 30, 80)
    img.save(os.path.join(out_dir, "shadow.png"))


def build_all(out_dir):
    build(out_dir, "tanuki_tall")
    build_species(out_dir)
    shadow(out_dir)


if __name__ == "__main__":
    import sys
    out = sys.argv[1]
    build_all(out)
    fr = {(d, f): Image.open(os.path.join(out, f"tanuki_tall_{d}{f}.png")) for d in "dulr" for f in range(4)}
    s = 10
    sheet = Image.new("RGBA", (16 * 4 * s + 30, 24 * 4 * s), (150, 180, 110, 255))
    for di, d in enumerate("dulr"):
        for f in range(4):
            sheet.alpha_composite(fr[(d, f)].resize((16 * s, 24 * s), Image.NEAREST), (di * (16 * s + 10), f * 24 * s))
    sheet.save(os.path.join(out, "_preview.png"))


