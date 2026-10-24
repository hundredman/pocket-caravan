"""북유럽 캐릭터 (chars.py 규칙 그대로: 16x24, 2등신, 색 테두리). 공용 chars.py는 고치지 않는다.
사용: python3 north_chars.py [출력 폴더, 기본 Assets/PocketCaravan/Art/Characters] [미리보기.png]
- north_reindeer: 북유럽 주인공 순록 (팀 투표 후보 중 선택)"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chars
from PIL import Image

chars.BASE.setdefault("A", (222, 202, 160))   # 뿔
chars.SHADEABLE |= set("A")

# 순록: 짧은 흰 꼬리
TAIL_SIDE = [(18, 2, "oLo"), (19, 2, "ooo")]
TAIL_BACK = [(20, 7, "oLLo"), (21, 7, "oooo")]
# 뿔: 머리 위 빈칸(0~7줄)에 덧그림
ANTLERS = {   # 10/23 다시 그림: 테두리 없이 가는 선으로 갈라진 뿔 (A 밝은 뿔, V 그늘)
    "d": ["V..V........V..V", "A..A..V..V..A..A", "AA.A..A..A..A.AA", ".AAA.AA..AA.AAA.", "..AAAA....AAAA..", "...AA......AA...", "...VA......AV...", "....V......V...."],
    "r": ["...V..V.........", "...A..A..V......", "..AA.AA..A..V...", "...AAA..AA..A...", "....AA.AAAAA....", ".....AAA.AA.....", ".....VA..V......", "......V........."],
}
chars.BASE.setdefault("V", (176, 146, 106))   # 뿔 밑동
chars.BASE.setdefault("F", (244, 240, 230))   # 목 둘레 흰 털
chars.SHADEABLE |= set("VF")

SPECIES = {
    # 갈색 털, 하얀 주둥이, 뿔, 파란 웃옷에 가죽 띠
    "north_reindeer": {"f": (142, 100, 72), "L": (186, 146, 108), "l": (246, 238, 224), "n": (72, 40, 44), "p": (240, 140, 130),
                       "c": (58, 104, 160), "a": (42, 78, 126), "b": (222, 180, 92), "k": (92, 74, 62), "s": (60, 44, 40),
                       "A": (222, 200, 160), "V": (150, 116, 82)},
}


def build(out_dir, name, pal_over):
    pal = dict(chars.BASE); pal.update(pal_over)
    saved = chars.TAIL_SIDE, chars.TAIL_BACK
    chars.TAIL_SIDE, chars.TAIL_BACK = TAIL_SIDE, TAIL_BACK
    frames = {}
    try:
        for f in range(4):
            for d in ("d", "u", "r"):
                rows = chars.compose(d, f, "round", False, False)
                art = ANTLERS["r" if d == "r" else "d"]
                shift = 1 if f in (1, 3) else 0     # 걸을 때 머리와 같이 내려감
                for y, line in enumerate(art):
                    for x, ch in enumerate(line):
                        if ch != "." and rows[y + shift][x] == ".":
                            rows[y + shift][x] = ch
                # 순록다운 흰 목털: 어깨 첫 줄을 흰 털로
                y = 16 + shift
                rows[y] = [("F" if ch in "cla" else ch) for ch in rows[y]]
                frames[(d, f)] = chars.render(rows, pal)
            frames[("l", f)] = frames[("r", f)].transpose(Image.FLIP_LEFT_RIGHT)
    finally:
        chars.TAIL_SIDE, chars.TAIL_BACK = saved
    for (d, f), img in frames.items():
        img.save(os.path.join(out_dir, f"{name}_tall_{d}{f}.png"))
    return frames


# ---------- 북유럽 NPC (10/23): 북쪽 동물 + 바이킹 복장 ----------
chars.BASE.update({
    "M": (150, 156, 170),   # 투구 쇠
    "F": (236, 230, 216),   # 털 깃 (망토 둘레)
    "B": (204, 150, 84),    # 땋은 수염 (털보다 진한 금갈색)
    "R": (168, 64, 56),     # 두건·스카프
})
chars.SHADEABLE |= set("MFR")

# 머리 위 3줄(5~7줄)을 덮는 것들. d=앞·뒤, r=옆
HELMET = {  # 뿔 달린 투구 (바이킹 상징으로 쓰는 게임식 표현): 3~8줄, 얼굴 윗줄까지 덮어 머리에 얹힌 느낌
    "d": [".o............o.", "oAo..........oAo", "oAAo.oooooo.oAAo", ".oAAoMHHMMMoAAo.", "..oAMMMMMMMMAo..", "..oMMMMMMMMMMo.."],
    "r": ["................", "..o.............", ".oAo.oooooo.....", ".oAAoMHHMMMo....", "..oAMMMMMMMMo...", "..oMMMMMMMMMMo.."],
}
HOOD = {  # 털 두른 두건
    "d": ["....oooooooo....", "..ooRRRRRRRRoo..", ".oRRFFFFFFFFRRo."],
    "r": ["....oooooooo....", "...oRRRRRRRRo...", "..oRRRFFFFFFo..."],
}
GOAT_HORNS = {"d": [".oo..........oo.", "oAAo........oAAo", ".oAAo......oAAo.", "..oAo......oAo.."],
              "r": ["..ooo...........", ".oAAAo..........", "..oAAAo.........", "...oAo.........."]}
MOOSE = {  # 넓적한 무스 뿔 (0~7줄)
    "d": ["................", "................", ".oo.o......o.oo.", "oAAoAo....oAoAAo", "oAAAAo....oAAAAo", ".oAAAoo..ooAAAo.", "..ooAAo..oAAoo..", "....oo....oo...."],
    "r": ["................", "................", "..oo.o.o........", ".oAAoAoAo.......", ".oAAAAAAo.......", "..oAAAAo........", "...ooAAo........", ".....oo........."],
}
LYNX_TUFT = {"d": [(5, 2, "n"), (5, 13, "n")], "r": [(5, 4, "n")]}

NPCS = {
    # 이름: (팔레트, 귀, 머리 위 장식, 수염, 털 깃)
    "north_bear_granny": ({"f": (120, 78, 52), "L": (158, 112, 78), "l": (214, 180, 140), "c": (74, 112, 74), "a": (56, 88, 58), "b": (214, 170, 80), "R": (168, 64, 56)}, "round", "hood", False, True),
    "north_fox_man":     ({"f": (236, 236, 240), "L": (255, 255, 255), "l": (250, 250, 252), "n": (60, 56, 70), "c": (54, 86, 140), "a": (40, 66, 112), "b": (110, 78, 52)}, "pointy", None, False, True),
    "north_fox_woman":   ({"f": (236, 236, 240), "L": (255, 255, 255), "l": (250, 250, 252), "n": (60, 56, 70), "c": (176, 92, 70), "a": (140, 70, 54), "b": (232, 196, 110), "R": (74, 112, 74)}, "pointy", "hood", False, True),
    "north_goat":        ({"f": (232, 226, 214), "L": (250, 246, 238), "l": (255, 252, 244), "c": (132, 98, 70), "a": (104, 76, 54), "b": (74, 112, 74)}, "round", "goat", True, False),
    "north_moose":       ({"f": (98, 70, 52), "L": (132, 98, 74), "l": (176, 146, 120), "c": (150, 116, 84), "a": (122, 92, 66), "b": (88, 64, 46), "A": (210, 190, 150)}, "round", "moose", False, False),
    "north_lynx":        ({"f": (196, 160, 112), "L": (224, 196, 150), "l": (246, 236, 220), "c": (96, 84, 120), "a": (74, 64, 96), "b": (214, 170, 80)}, "pointy", "lynx", False, True),
    "north_wolf_viking": ({"f": (136, 138, 150), "L": (176, 178, 190), "l": (226, 226, 232), "c": (164, 62, 52), "a": (128, 46, 40), "b": (214, 170, 80)}, "pointy", "helmet", True, True),
    "north_bear_sailor": ({"f": (128, 86, 58), "L": (166, 120, 84), "l": (220, 186, 146), "c": (54, 86, 140), "a": (40, 66, 112), "b": (110, 78, 52)}, "round", "helmet", True, False),
    "north_wolf":        ({"f": (136, 138, 150), "L": (176, 178, 190), "l": (226, 226, 232), "c": (132, 98, 70), "a": (104, 76, 54), "b": (74, 112, 74)}, "pointy", None, False, True),
    "north_hare":        ({"f": (246, 246, 250), "L": (255, 255, 255), "l": (255, 230, 236), "c": (74, 112, 140), "a": (56, 88, 112), "b": (232, 196, 110), "R": (176, 92, 70)}, "long", None, False, True),
    "north_owl_elder":   ({"f": (214, 208, 196), "L": (240, 236, 228), "l": (250, 248, 240), "c": (96, 84, 120), "a": (74, 64, 96), "b": (214, 170, 80), "R": (96, 84, 120)}, "round", "hood", True, True),
    "north_fox_kid":     ({"f": (236, 236, 240), "L": (255, 255, 255), "l": (250, 250, 252), "n": (60, 56, 70), "c": (214, 170, 80), "a": (176, 136, 60), "b": (164, 62, 52), "R": (164, 62, 52)}, "pointy", "hood", False, False),
}


def overlay(rows, art, y0, only_empty=True):
    for i, line in enumerate(art):
        for x, ch in enumerate(line):
            if ch != "." and 0 <= y0 + i < 24 and (not only_empty or rows[y0 + i][x] in ".o"):
                rows[y0 + i][x] = ch


def build_npc(out_dir, name, spec):
    pal_over, ears, top, beard, collar = spec
    pal = dict(chars.BASE); pal.update(pal_over)
    frames = {}
    saved = chars.TAIL_SIDE, chars.TAIL_BACK
    chars.TAIL_SIDE, chars.TAIL_BACK = [], []   # 너구리 줄무늬 꼬리는 북쪽 동물에 안 맞음
    for f in range(4):
        sh = 1 if f in (1, 3) else 0
        for d in ("d", "u", "r"):
            rows = chars.compose(d, f, ears, False, False)
            k = "r" if d == "r" else "d"
            if top == "helmet":
                for y in range(3, 8):
                    rows[y + sh] = list("." * 16)
                overlay(rows, HELMET[k], 3 + sh, only_empty=False)
            elif top == "hood":
                for y in range(5, 8):
                    rows[y + sh] = list("." * 16)
                overlay(rows, HOOD[k], 5 + sh, only_empty=False)
            elif top == "goat":
                overlay(rows, GOAT_HORNS[k], 4 + sh)
            elif top == "moose":
                overlay(rows, MOOSE[k], 0 + sh)
            elif top == "lynx":
                for (y, x, ch) in LYNX_TUFT[k]:
                    rows[y + sh][x] = ch
            if beard and d == "d":       # 땋은 수염: 턱 아래로 내려옴
                y = 14 + sh
                rows[y] = [("B" if ch == "l" else ch) for ch in rows[y]]
                for (yy, xx) in ((15 + sh, 6), (15 + sh, 7), (15 + sh, 8), (15 + sh, 9), (16 + sh, 6), (16 + sh, 9), (17 + sh, 6), (17 + sh, 9)):
                    rows[yy][xx] = "B"   # 두 갈래로 땋은 수염
            if beard and d == "r":
                y = 14 + sh
                rows[y] = [("B" if ch == "l" else ch) for ch in rows[y]]
            if collar:                    # 털 깃: 어깨 첫 줄을 흰 털로
                y = 16 + sh
                rows[y] = [("F" if ch in "cla" else ch) for ch in rows[y]]
            frames[(d, f)] = chars.render(rows, pal)
        frames[("l", f)] = frames[("r", f)].transpose(Image.FLIP_LEFT_RIGHT)
    chars.TAIL_SIDE, chars.TAIL_BACK = saved
    for (d, f), img in frames.items():
        img.save(os.path.join(out_dir, f"{name}_tall_{d}{f}.png"))
    return frames


# ---------- 10/25 주인공 순록 다시 그림: 길쭉한 밝은 주둥이·검은 코, 옆으로 뻗은 귀, 붉은 목도리, 더 갈라진 뿔 ----------
RD_FACE = {
    "d": ["...offffffffo...",
          "..offLLffffffo..",
          ".offffffffffffo.",
          ".offweffffweffo.",
          ".offeeffffeeffo.",
          ".off" + "fllnnllf" + "ffo.",     # 밝은 주둥이 위쪽 가운데에 검은 코
          "..of" + "llllllll" + "fo..",
          "...oolllllloo..."],
    "u": ["...offffffffo...",
          "..offLLffffffo..",
          ".offffffffffffo.",
          ".offffffffffffo.",
          ".offffffffffffo.",
          ".offffffffffffo.",
          "..offffffffffo..",
          "....oooooooo...."],
    "r": ["...offffffffo...",
          "..offLLffffffo..",
          ".offffffffffffo.",
          ".offffffffffweo.",
          ".offffffffffeelo",
          ".offffffffllllno",
          "..offffffllllloo",
          "...oooooooooo..."],
}
RD_EARS = {"d": ["................", "oo............oo", "offo........offo"],
           "r": ["................", ".oo.............", ".offo..........."]}
RD_ANTLERS = {   # 굵은 줄기에서 가지가 세 갈래, 끝은 밝게
    "d": ["W.W.W......W.W.W", ".AAA.V....V.AAA.", "..AAAV....VAAA..", ".W.AA......AA.W.", ".AAAA......AAAA.", "...AAA....AAA...", "....VA....AV....", ".....V....V....."],
    "r": ["..W.W.W.........", "...AAA.V.W......", "....AAAVAA......", "..W.AA.AA.......", "..AAAAAA........", "....AAA.........", ".....VA.........", "......V........."],
}
RD_SCARF = {"d": ["....oRRRRRRo....", "...oaRRRRRRao..."], "u": ["....oRRRRRRo....", "...oaRRRRRRao..."],
            "r": [".....oRRRo......", "....oRRRRRRo...."]}
chars.BASE.setdefault("W", (250, 240, 214))   # 뿔 끝
chars.BASE.setdefault("R", (190, 58, 52))


def build_reindeer(out_dir, name, pal_over):
    pal = dict(chars.BASE); pal.update(pal_over)
    frames = {}
    saved = chars.TAIL_SIDE, chars.TAIL_BACK
    chars.TAIL_SIDE, chars.TAIL_BACK = TAIL_SIDE, TAIL_BACK
    try:
        for f in range(4):
            sh = 1 if f in (1, 3) else 0
            for d in ("d", "u", "r"):
                rows = chars.compose(d, f, "round", False, False)
                k = "r" if d == "r" else "d"
                for y in range(5, 16):
                    rows[y + sh] = list("." * 16)
                overlay(rows, RD_EARS[k], 5 + sh, only_empty=False)
                overlay(rows, RD_FACE[d], 8 + sh, only_empty=False)
                overlay(rows, RD_ANTLERS[k], 0 + sh)
                overlay(rows, RD_SCARF[d], 16 + sh, only_empty=False)
                if d == "r":       # 목도리 끝이 뒤로 날림
                    for (yy, xx) in ((17 + sh, 3), (18 + sh, 2)):
                        rows[yy][xx] = "R"
                frames[(d, f)] = chars.render(rows, pal)
            frames[("l", f)] = frames[("r", f)].transpose(Image.FLIP_LEFT_RIGHT)
    finally:
        chars.TAIL_SIDE, chars.TAIL_BACK = saved
    for (d, f), img in frames.items():
        img.save(os.path.join(out_dir, f"{name}_tall_{d}{f}.png"))
    return frames


# ---------- 10/25 팀 결정: NPC는 사람 (플레이어는 동물 상인이라 구분되게). 북유럽 사람 12종 ----------
chars.BASE.update({"u": (240, 200, 164), "U": (212, 166, 130), "y": (226, 190, 110), "Y": (246, 222, 156), "z": (200, 160, 90)})
chars.SHADEABLE |= set("uyz")
HU_FACE = {
    "d": ["...oyyyyyyyyo...",
          "..oyyYYyyyyyyo..",
          ".oyyyyyyyyyyyyo.",
          ".oyuuuuuuuuuuyo.",
          ".oyuweuuuuweuyo.",
          ".ouuueuuuueuuuo.",
          "..opuuuuuuuupo..",
          "...oouuuuuuoo..."],
    "u": ["...oyyyyyyyyo...",
          "..oyyYYyyyyyyo..",
          ".oyyyyyyyyyyyyo.",
          ".oyyyyyyyyyyyyo.",
          ".oyyyyyyyyyyyyo.",
          ".oyyyyyyyyyyyyo.",
          "..oyyyyyyyyyyo..",
          "...oooyyyyyooo.."],
    "r": ["...oyyyyyyyyo...",
          "..oyyYYyyyyyyo..",
          ".oyyyyyyyyyyyyo.",
          ".oyyyyyyyuuuuo..",
          ".oyyyyyyuuuweo..",
          ".oyyyyyyuuuueuo.",
          "..oyyyyuuuuupo..",
          "...oooouuuuoo..."],
}
HU_TOP = {"d": ["................", "....oooooooo....", "..ooyyyyyyyyoo.."], "r": ["................", "....oooooooo....", "..ooyyyyyyyyoo.."]}
BALD = {"d": ["................", "................", "....oooooooo...."], "r": ["................", "................", "....oooooooo...."]}
BUN = {"d": ["......oooo......", ".....oyYyyo.....", "..oooyyyyyyooo.."], "r": ["...oooo.........", "..oyYyyo........", "..ooyyyyyyyoo..."]}
BRAIDS = {"d": [(14, 1), (15, 1), (16, 1), (17, 1), (14, 14), (15, 14), (16, 14), (17, 14)], "r": [(14, 2), (15, 2), (16, 2), (17, 2), (18, 2)]}
CAP = {  # 털실 모자 (뱃사람)
    "d": ["................", "....oooooooo....", "..ooRRRRRRRRoo..", ".oRRRRRRRRRRRRo."],
    "r": ["................", "....oooooooo....", "..ooRRRRRRRRoo..", ".oRRRRRRRRRRRo.."],
}


def human_head(rows, d, sh, hair_top):
    k = "r" if d == "r" else "d"
    for y in range(5, 16):
        rows[y + sh] = list("." * 16)
    overlay(rows, hair_top[k], 5 + sh, only_empty=False)
    overlay(rows, HU_FACE[d], 8 + sh, only_empty=False)


def human_beard(rows, d, sh, long=False):
    """수염: 볼 아래 살색을 수염색으로, 턱 아래로 두 갈래 (길면 배까지)"""
    if d == "u":
        return
    for y in (13 + sh, 14 + sh, 15 + sh):
        rows[y] = [("z" if ch in "up" else ch) for ch in rows[y]]
    if d == "d":
        for i in range(3 if long else 2):
            for xx in (5, 6, 9, 10):
                if 16 + sh + i < 24:
                    rows[16 + sh + i][xx] = "z"
    else:
        for i in range(3 if long else 2):
            if 16 + sh + i < 24:
                rows[16 + sh + i][11] = "z"; rows[16 + sh + i][12] = "z"


HUMANS = {
    # 이름(파일 이름은 그대로 = 맵·퀘스트 수정 없음): 팔레트, 머리 위, 수염(0 없음 1 짧게 2 길게), 땋은 머리, 털 깃
    "north_bear_granny": ({"y": (196, 196, 204), "Y": (232, 232, 238), "c": (74, 112, 74), "a": (56, 88, 58), "b": (214, 170, 80), "R": (168, 64, 56)}, "hood", 0, False, True),     # 여관 할머니
    "north_fox_man":     ({"y": (232, 196, 110), "Y": (250, 226, 160), "c": (54, 86, 140), "a": (40, 66, 112), "b": (110, 78, 52)}, "hair", 1, False, True),                        # 젊은 상인
    "north_fox_woman":   ({"y": (236, 200, 120), "Y": (252, 230, 170), "c": (176, 92, 70), "a": (140, 70, 54), "b": (232, 196, 110)}, "hair", 0, True, True),                      # 그물 깁는 여자
    "north_goat":        ({"y": (122, 84, 58), "Y": (160, 116, 82), "z": (122, 84, 58), "c": (132, 98, 70), "a": (104, 76, 54), "b": (74, 112, 74)}, "hair", 1, False, False),       # 농부·짐꾼
    "north_moose":       ({"y": (170, 84, 46), "Y": (206, 120, 70), "z": (176, 88, 48), "c": (150, 116, 84), "a": (122, 92, 66), "b": (88, 64, 46)}, "bald", 2, False, False),        # 나무꾼·광부
    "north_lynx":        ({"u": (204, 156, 116), "U": (176, 128, 92), "y": (60, 46, 42), "Y": (96, 76, 66), "c": (96, 84, 120), "a": (74, 64, 96), "b": (214, 170, 80), "R": (190, 150, 70)}, "hood", 1, False, True),  # 먼 길 온 행상
    "north_wolf_viking": ({"y": (214, 170, 96), "Y": (240, 206, 140), "z": (214, 170, 96), "c": (164, 62, 52), "a": (128, 46, 40), "b": (214, 170, 80)}, "helmet", 2, False, True),  # 전사·경비대장
    "north_bear_sailor": ({"y": (70, 56, 50), "Y": (110, 92, 80), "z": (70, 56, 50), "c": (54, 86, 140), "a": (40, 66, 112), "b": (110, 78, 52), "R": (52, 78, 124)}, "cap", 2, False, False),  # 뱃사람·낚시꾼
    "north_wolf":        ({"y": (110, 80, 56), "Y": (150, 116, 84), "z": (110, 80, 56), "c": (132, 98, 70), "a": (104, 76, 54), "b": (74, 112, 74), "R": (150, 118, 86)}, "hood", 1, False, True),  # 사냥꾼
    "north_hare":        ({"y": (196, 92, 52), "Y": (232, 132, 84), "c": (74, 112, 140), "a": (56, 88, 112), "b": (232, 196, 110)}, "hair", 0, True, True),                       # 약초꾼
    "north_owl_elder":   ({"y": (226, 226, 232), "Y": (250, 250, 252), "z": (236, 236, 240), "c": (96, 84, 120), "a": (74, 64, 96), "b": (214, 170, 80), "R": (96, 84, 120)}, "hood", 2, False, True),  # 노인·룬 지기
    "north_fox_kid":     ({"y": (220, 120, 60), "Y": (246, 160, 96), "c": (214, 170, 80), "a": (176, 136, 60), "b": (164, 62, 52)}, "bun", 0, False, False),                      # 아이
}


# 10/25 사람 10명 더 (역할에 맞게 다양하게). 피부색·머리 모양·옷·소품을 다르게. 마지막 칸 = 덧붙임 (apron 앞치마, shawl 숄)
chars.BASE.setdefault("q", (236, 230, 214))   # 앞치마
chars.SHADEABLE |= set("q")
HUMANS2 = {
    "north_p_granny":  ({"y": (236, 236, 240), "Y": (252, 252, 255), "c": (118, 82, 132), "a": (92, 62, 104), "b": (214, 170, 80), "q": (226, 220, 206)}, "bun", 0, False, False, {"apron", "shawl"}),   # 순록 목동·돌집 할머니
    "north_p_smith":   ({"u": (214, 164, 124), "U": (186, 136, 100), "y": (50, 40, 38), "z": (50, 40, 38), "c": (150, 150, 160), "a": (120, 120, 130), "b": (90, 66, 48), "q": (120, 84, 56)}, "bald", 1, False, False, {"apron"}),  # 인부·광부
    "north_p_fisher":  ({"y": (150, 104, 66), "Y": (186, 140, 96), "c": (150, 156, 170), "a": (118, 124, 138), "b": (60, 90, 140), "R": (60, 96, 156)}, "cap", 0, False, False, set()),        # 젊은 어부
    "north_p_shield":  ({"y": (240, 206, 120), "Y": (252, 232, 170), "c": (70, 118, 86), "a": (52, 92, 66), "b": (164, 62, 52)}, "helmet", 0, True, True, set()),                                  # 방패 처녀 (전사)
    "north_p_bard":    ({"y": (120, 70, 46), "Y": (160, 104, 70), "c": (124, 70, 140), "a": (96, 52, 110), "b": (232, 196, 110), "R": (186, 52, 60)}, "hood", 0, False, True, set()),                  # 음유시인
    "north_p_oldman":  ({"y": (220, 220, 226), "Y": (246, 246, 250), "z": (232, 232, 236), "c": (120, 92, 66), "a": (96, 72, 52), "b": (74, 112, 74)}, "hair", 1, False, False, set()),                # 늙은 농부·강지기
    "north_p_girl":    ({"u": (196, 146, 108), "U": (168, 120, 86), "y": (46, 36, 40), "Y": (86, 70, 74), "c": (226, 160, 60), "a": (192, 128, 44), "b": (164, 62, 52)}, "hair", 0, True, False, set()),  # 소녀
    "north_p_herder":  ({"u": (220, 172, 130), "U": (192, 144, 106), "y": (96, 64, 44), "c": (176, 64, 56), "a": (140, 48, 42), "b": (232, 196, 110), "R": (236, 230, 216)}, "hood", 0, False, True, set()),  # 순록 목동 청년·썰매꾼
    "north_p_trader":  ({"u": (168, 116, 80), "U": (140, 94, 64), "y": (36, 30, 32), "z": (36, 30, 32), "c": (40, 110, 120), "a": (30, 86, 96), "b": (214, 170, 80), "R": (214, 170, 80)}, "cap", 3, False, True, set()),  # 먼 남쪽 상인
    "north_p_boy":     ({"y": (240, 210, 120), "Y": (255, 236, 170), "c": (70, 110, 170), "a": (52, 86, 140), "b": (164, 62, 52)}, "hair", 0, False, False, set()),                                # 꼬마
}


def build_human(out_dir, name, spec):
    pal_over, top, beard, braids, collar = spec[:5]
    extras = spec[5] if len(spec) > 5 else set()
    pal = dict(chars.BASE); pal.update(pal_over)
    frames = {}
    saved = chars.TAIL_SIDE, chars.TAIL_BACK
    chars.TAIL_SIDE, chars.TAIL_BACK = [], []
    try:
        for f in range(4):
            sh = 1 if f in (1, 3) else 0
            for d in ("d", "u", "r"):
                rows = chars.compose(d, f, "round", False, False)
                k = "r" if d == "r" else "d"
                human_head(rows, d, sh, {"bald": BALD, "bun": BUN}.get(top, HU_TOP))
                if top == "bald":                         # 대머리: 정수리는 살색, 옆머리만 남김
                    for yy in (8, 9, 10):
                        r = rows[yy + sh]
                        for xx in range(3, 13):
                            if r[xx] in "yY" and (yy < 10 or 4 <= xx <= 11):
                                r[xx] = "U" if (yy == 8 and xx in (5, 6)) else "u"
                    if d == "u":
                        for yy in (11, 12):
                            for xx in range(4, 12):
                                if rows[yy + sh][xx] == "y":
                                    rows[yy + sh][xx] = "u"
                if top == "helmet":
                    for y in range(3, 8):
                        rows[y + sh] = list("." * 16)
                    overlay(rows, HELMET[k], 3 + sh, only_empty=False)
                elif top == "hood":
                    for y in range(5, 8):
                        rows[y + sh] = list("." * 16)
                    overlay(rows, HOOD[k], 5 + sh, only_empty=False)
                elif top == "cap":
                    for y in range(5, 9):
                        rows[y + sh] = list("." * 16)
                    overlay(rows, CAP[k], 5 + sh, only_empty=False)
                if braids and d != "u":
                    for (yy, xx) in BRAIDS[k]:
                        rows[yy + sh][xx] = "y"
                if braids and d == "u":
                    for yy in range(16, 19):
                        rows[yy + sh][7] = rows[yy + sh][8] = "y"
                if collar:
                    y = 16 + sh
                    rows[y] = [("F" if ch in "cla" else ch) for ch in rows[y]]
                if beard == 3 and d != "u":                 # 콧수염만
                    for xx in ((6, 7, 8, 9) if d == "d" else (11, 12)):
                        rows[14 + sh][xx] = "z"
                elif beard:
                    human_beard(rows, d, sh, long=beard == 2)
                if "apron" in extras and d != "u":         # 앞치마: 몸 아래쪽 가운데
                    for yy in (18, 19, 20):
                        for xx in ((5, 6, 7, 8, 9, 10) if d == "d" else (7, 8, 9)):
                            if rows[yy + sh][xx] in "cab":
                                rows[yy + sh][xx] = "q"
                if "shawl" in extras:                       # 숄: 어깨를 두른 진한 천
                    rows[16 + sh] = [("R" if ch in "clabF" else ch) for ch in rows[16 + sh]]
                    rows[17 + sh] = [("R" if ch in "a" else ch) for ch in rows[17 + sh]]
                frames[(d, f)] = chars.render(rows, pal)
            frames[("l", f)] = frames[("r", f)].transpose(Image.FLIP_LEFT_RIGHT)
    finally:
        chars.TAIL_SIDE, chars.TAIL_BACK = saved
    for (d, f), img in frames.items():
        img.save(os.path.join(out_dir, f"{name}_tall_{d}{f}.png"))
    return frames


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "..", "Assets/PocketCaravan/Art/Characters")
    os.makedirs(out, exist_ok=True)
    allf = {n: build_reindeer(out, n, p) for n, p in SPECIES.items()}       # 10/25 새 순록
    allf.update({n: build_human(out, n, sp) for n, sp in HUMANS.items()})
    allf.update({n: build_human(out, n, sp) for n, sp in HUMANS2.items()})   # 10/25 NPC는 사람 (동물 NPC 그림 NPCS/build_npc는 남겨 둠)
    if len(sys.argv) > 2:
        chars.preview(allf, sys.argv[2])
    print(", ".join(allf))
