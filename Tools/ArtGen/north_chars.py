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


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "..", "Assets/PocketCaravan/Art/Characters")
    os.makedirs(out, exist_ok=True)
    allf = {n: build(out, n, p) for n, p in SPECIES.items()}
    allf.update({n: build_npc(out, n, sp) for n, sp in NPCS.items()})
    if len(sys.argv) > 2:
        chars.preview(allf, sys.argv[2])
    print(", ".join(allf))
