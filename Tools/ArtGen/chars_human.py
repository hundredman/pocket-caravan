"""
사람 NPC (16x24, 2등신, 4방향 x 4프레임). 지금 캐릭터 생성기(chars.py)의 몸·다리·뒤처리를 그대로 쓰고,
머리만 사람(머리카락·피부 얼굴)으로 바꾼다. 그래서 그림체·크기·명암·테두리가 지금 캐릭터와 같다.
사용: python3 chars_human.py <chars.py가 있는 폴더> <출력 폴더> [비교.png] [게임화면.png]
"""
import os, sys
from PIL import Image

sys.path.insert(0, sys.argv[1])
import chars as C

# 글자: r 머리카락, q 피부, e 눈, w 눈 반짝임, p 볼, n 입, h/H 갓
FACE_FRONT = [
    "...orrrrrrrro...",
    "..orrrrrrrrrro..",
    ".orrrqqqqqqrrro.",
    ".orqqweqqweqqro.",
    ".orqqeeqqeeqqro.",
    ".opqqqqqqqqqqpo.",
    "..oqqqqnnqqqqo..",
    "....oooooooo....",
]
FACE_BACK = [
    "...orrrrrrrro...",
    "..orrrrrrrrrro..",
    ".orrrrrrrrrrrro.",
    ".orrrrrrrrrrrro.",
    ".orrrrrrrrrrrro.",
    ".orrrrrrrrrrrro.",
    "..orrrrrrrrrro..",
    "....oooooooo....",
]
FACE_SIDE = [
    "...orrrrrrrro...",
    "..orrrrrrrrrro..",
    ".orrrrrrrqqqo...",
    ".orrrrrqqqweo...",
    ".orrrrqqqqqeqo..",
    ".orrrqqqqqqpqo..",
    "..orrqqqqqqnqo..",
    "....ooooooooo...",
]
TOPS = {  # 머리 위 3줄
    "topknot": {"d": ["................", "......orro......", ".....orrrro....."],
                "u": ["................", "......orro......", ".....orrrro....."],
                "r": ["................", ".....orro.......", "....orrrro......"]},
    "bun":     {"d": ["................", "................", "................"],
                "u": ["................", ".....orrrro.....", "....orrrrrro...."],
                "r": ["................", "..orro..........", ".orrrro........."]},
    "hat":     {k: C.HAT for k in "dur"},
}

def compose(direction, frame, top, mask_band=False):
    arm = {0: 0, 1: 1, 2: 0, 3: 2}[frame]
    key = "d" if direction == "d" else "u" if direction == "u" else "r"
    face = {"d": FACE_FRONT, "u": FACE_BACK, "r": FACE_SIDE}[key]
    if mask_band and key != "u":   # 도적: 눈가를 가리는 천
        face = list(face)
        for i in (3, 4): face[i] = face[i].replace("q", "m")
    body = (C.BODY_FRONT if key == "d" else C.BODY_BACK if key == "u" else C.BODY_SIDE)[arm]
    legs = (C.LEGS_FRONT if key in "du" else C.LEGS_SIDE)[frame]
    rows = C.norm(["." * 16] * C.PAD) + C.norm(TOPS[top][key]) + C.norm(face) + C.norm(body) + C.norm(legs)
    rows = [list(r) for r in rows]
    if frame in (1, 3):
        rows = [["."] * 16] + rows[:20] + rows[21:]
    return rows

HUMAN = {  # 이름: (팔레트, 머리 위, 복면)
    "human_chief":    ({"r": (60, 50, 52), "c": (240, 236, 226), "a": (206, 200, 186), "b": (124, 104, 84)}, "hat", False),
    "human_gaekju":   ({"r": (52, 42, 44), "c": (118, 150, 200), "a": (90, 116, 164), "b": (232, 196, 110)}, "hat", False),
    "human_merchant": ({"r": (48, 38, 40), "c": (126, 176, 114), "a": (96, 140, 88), "b": (214, 84, 72)}, "topknot", False),
    "human_farmer":   ({"r": (58, 44, 40), "c": (214, 196, 160), "a": (176, 158, 124), "b": (120, 150, 96)}, "topknot", False),
    "human_villager": ({"r": (44, 36, 40), "c": (236, 156, 176), "a": (200, 120, 142), "b": (250, 230, 160), "k": (92, 112, 160)}, "bun", False),
    "human_bandit":   ({"r": (40, 36, 44), "m": (46, 40, 52), "c": (72, 66, 86), "a": (52, 48, 64), "b": (196, 64, 60), "k": (54, 46, 52)}, "topknot", True),
}
SKIN = {"q": (248, 216, 186), "p": (242, 160, 150), "n": (150, 84, 74), "r": (52, 42, 44)}
C.SHADEABLE.update(set("qr"))

def build(out_dir):
    os.makedirs(out_dir, exist_ok=True); allf = {}
    for name, (pal_over, top, band) in HUMAN.items():
        pal = dict(C.BASE); pal.update(SKIN); pal.update(pal_over)
        fr = {}
        for f in range(4):
            for d in ("d", "u", "r"):
                fr[(d, f)] = C.render(compose(d, f, top, band), pal)
            fr[("l", f)] = fr[("r", f)].transpose(Image.FLIP_LEFT_RIGHT)
        for (d, f), im in fr.items(): im.save(os.path.join(out_dir, f"{name}_tall_{d}{f}.png"))
        allf[name] = fr
    return allf

if __name__ == "__main__":
    allf = build(sys.argv[2])
    if len(sys.argv) > 3:
        C.preview(allf, sys.argv[3])
