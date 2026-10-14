"""
캐릭터 그림 (16x24, 4방향 x 4프레임). 글자 그림으로 모양을 잡고, 뒤처리로 다듬는다.

뒤처리:
- 색 테두리(sel-out): 검은 선 대신 닿은 색을 어둡게 한 선. 바깥선은 진하게, 안쪽 선은 연하게.
- 명암: 빛은 왼쪽 위. 오른쪽·아래 가장자리 한 줄을 그늘색으로.
사용: python3 chars.py <출력 폴더> [미리보기.png]
"""
import os
import sys
from PIL import Image

# ---------- 기본 팔레트 (종마다 일부를 바꾼다) ----------
BASE = {
    "f": (178, 122, 82),    # 털
    "L": (214, 166, 118),   # 털 밝은 곳
    "m": (86, 58, 46),      # 너구리 눈 둘레
    "l": (246, 230, 204),   # 주둥이·배
    "n": (52, 36, 34),      # 코
    "e": (34, 26, 34),      # 눈
    "w": (255, 255, 255),   # 눈 반짝임
    "p": (242, 150, 146),   # 볼
    "h": (44, 42, 56),      # 갓
    "H": (96, 96, 122),     # 갓 반짝임
    "c": (240, 234, 220),   # 옷
    "a": (214, 204, 186),   # 소매(옷 그늘)
    "b": (92, 128, 186),    # 띠
    "k": (96, 72, 58),      # 바지·다리
    "s": (66, 48, 42),      # 신발
    "t": (120, 82, 56),     # 꼬리 줄무늬
    "g": (210, 168, 92),    # 등짐 (지게)
    "G": (160, 120, 64),
    "o": (0, 0, 0),         # 선 (뒤처리에서 색이 정해짐)
}
OUTLINE_TINT = (36, 24, 40)

# ---------- 머리 (13줄) ----------
HAT = [  # 갓: 0~4줄
    "......oooo......",
    ".....ohHhho.....",
    ".....ohhhho.....",
    "..oooohhhhoooo..",
    ".ohhhhhhhhhhhho.",
]
HAT_SIDE = [
    "......oooo......",
    ".....ohHhho.....",
    ".....ohhhho.....",
    "..oooohhhhoooo..",
    ".ohhhhhhhhhhhho.",
]
HAT_BACK = HAT

FACE_FRONT = [  # 5~12줄
    "..ooffffffffoo..",
    ".offLLffffffffo.",
    "offmmmffffmmmffo",
    "ofmmwemffmwemmfo",
    "ofmmeemllmeemmfo",
    "opflllllnnlllfpo",
    ".offllllllllffo.",
    "...ooffffffoo...",
]
FACE_BACK = [
    "..ooffffffffoo..",
    ".offLLffffffffo.",
    "offffffffffffffo",
    "offffffffffffffo",
    "offffffffffffffo",
    "offffffffffffffo",
    ".offffffffffffo.",
    "...ooffffffoo...",
]
FACE_SIDE = [  # 오른쪽을 봄
    "...ooffffffoo...",
    "..offLLffffffo..",
    ".offffffffmmmfo.",
    ".offffffffmwemo.",
    ".offffffffmeemlo",
    ".opfffffffllllno",
    "..offffffflllloo",
    "...oofffffooo...",
]

# 귀 (모자 없는 종). 0~4줄을 바꾼다
EARS = {
    "round": {
        "d": ["................", "................", "..ooo......ooo..", ".offo......offo.", ".offfoooooofffo."],
        "r": ["................", "................", "....ooo.........", "...offo.........", "...offfooooo...."],
    },
    "pointy": {
        "d": ["................", "..o..........o..", ".ofo........ofo.", ".offo......offo.", ".offfoooooofffo."],
        "r": ["................", "......o.........", ".....ofo........", "....offo........", "...offfoooo....."],
    },
    "big": {  # 사막여우처럼 큰 귀
        "d": [".o............o.", ".ofo........ofo.", ".offo......offo.", ".offfo....offfo.", ".offffoooofffffo"],
        "r": ["....o...........", "....ofo.........", "....offo........", "....offfo.......", "...offffooooo..."],
    },
    "long": {
        "d": ["...oo......oo...", "..oflo....olfo..", "..oflo....olfo..", "..oflo....olfo..", ".ooffoooooooffoo"],
        "r": ["....oo..........", "...oflo.........", "...oflo.........", "...oflo.........", "..ooffooooooo..."],
    },
}

# ---------- 몸 (8줄, 13~20) : 팔 흔들기 3가지 ----------
BODY_FRONT = {
    0: [
        "....oclcccco....",
        "...occclcccco...",
        "..oaccccllccao..",
        ".oaabbbbbbbbaao.",
        ".ofacccccccccafo",
        "..ooccccccccoo..",
        "...oaaaaaaaao...",
        "...oooooooooo...",
    ],
    1: [  # 왼팔 앞(아래로), 오른팔 뒤
        "....oclcccco....",
        "...occclcccco...",
        "..oaccccllccao..",
        ".oaabbbbbbbbaao.",
        ".oaacccccccccao.",
        ".ofoccccccccofo.",
        "...oaaaaaaaao...",
        "...oooooooooo...",
    ],
    2: [  # 오른팔 앞
        "....oclcccco....",
        "...occclcccco...",
        "..oaccccllccao..",
        ".oaabbbbbbbbaao.",
        ".oacccccccccaao.",
        ".ofoccccccccofo.",
        "...oaaaaaaaao...",
        "...oooooooooo...",
    ],
}
BODY_BACK = {
    k: [r.replace("l", "c") for r in v] for k, v in BODY_FRONT.items()
}
BODY_SIDE = {
    0: [
        ".....occcco.....",
        "....occcccco....",
        "....occcccco....",
        "....obbbbbbo....",
        "....ocacccco....",
        "....ocafcccco...",
        "....oaaaaaao....",
        "....oooooooo....",
    ],
    1: [  # 팔 앞으로
        ".....occcco.....",
        "....occcccco....",
        "....occcccco....",
        "....obbbbbbo....",
        "....occcaaco....",
        "....occccafo....",
        "....oaaaaaao....",
        "....oooooooo....",
    ],
    2: [  # 팔 뒤로
        ".....occcco.....",
        "....occcccco....",
        "....occcccco....",
        "....obbbbbbo....",
        "...oacccccco....",
        "...ofaccccco....",
        "....oaaaaaao....",
        "....oooooooo....",
    ],
}

# ---------- 다리 (3줄, 21~23) ----------
LEGS_FRONT = [
    ["....okko.okko...", "....okko.okko...", "....osso.osso..."],
    ["....okko.okko...", "....osso.okko...", ".........osso..."],
    ["....okko.okko...", "....okko.okko...", "....osso.osso..."],
    ["....okko.okko...", "....okko.osso...", "....osso......."],
]
LEGS_SIDE = [
    [".....okkko......", ".....okkko......", ".....osssoo.....".replace("oo.", "o..")],
    ["....okko.okko...", "...okko...okko..", "...osso...osso.."],
    [".....okkko......", ".....okkko......", ".....ossso......"],
    ["....okkookko....", "....okko.okko...", "....osso.osso..."],
]

# 꼬리 (옆·뒤에서 보임). (줄, 왼쪽 x, 글자)
TAIL_SIDE = [(16, 1, "ofo"), (17, 0, "oftfo"), (18, 0, "otfto"), (19, 1, "ooo")]
TAIL_BACK = [(19, 6, "oftfo"), (20, 6, "otfto"), (21, 7, "ooo")]


def norm(rows):
    for r in rows:
        assert len(r) <= 16, (len(r), r)
    return [(r + "." * 16)[:16] for r in rows]


def compose(direction, frame, top, hat, mask):
    """글자 그림 24줄을 만든다."""
    arm = {0: 0, 1: 1, 2: 0, 3: 2}[frame]
    if direction in ("d", "u"):
        head_top = HAT if hat else EARS[top]["d"]
        face = FACE_FRONT if direction == "d" else FACE_BACK
        body = (BODY_FRONT if direction == "d" else BODY_BACK)[arm]
        legs = LEGS_FRONT[frame]
    else:
        head_top = HAT_SIDE if hat else EARS[top]["r"]
        face = FACE_SIDE
        body = BODY_SIDE[arm]
        legs = LEGS_SIDE[frame]
    face = list(face)
    if not mask:
        face = [r.replace("m", "f") for r in face]
    rows = norm(head_top) + norm(face) + norm(body) + norm(legs)
    assert len(rows) == 24
    rows = [list(r) for r in rows]
    tail = TAIL_SIDE if direction == "r" else TAIL_BACK if direction == "u" else []
    for y, x0, s in tail:
        for i, ch in enumerate(s):
            if rows[y][x0 + i] == ".":
                rows[y][x0 + i] = ch
    if frame in (1, 3):  # 발 디딜 때 몸이 한 칸 내려감
        rows = [["."] * 16] + rows[:20] + rows[21:]
    return rows


def darken(c, k):
    return tuple(int(v * k + t * (1 - k) * 0.35) for v, t in zip(c, OUTLINE_TINT))


def shade(c):
    return tuple(max(0, int(v * 0.82)) for v in c)


SHADEABLE = set("fclbkhg")


def render(rows, pal):
    H, W = len(rows), 16
    col = [[pal.get(ch) if ch not in ".o" else None for ch in r] for r in rows]
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()

    def at(x, y):
        return rows[y][x] if 0 <= x < W and 0 <= y < H else "."

    for y in range(H):
        for x in range(W):
            ch = rows[y][x]
            if ch == ".":
                continue
            if ch == "o":
                # 닿은 색 중 가장 많은 것을 어둡게. 바깥(투명)과 닿으면 더 진하게
                near, outer = [], False
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    n = at(x + dx, y + dy)
                    if n == ".":
                        outer = True
                    elif n != "o":
                        near.append(pal[n])
                if not near:
                    for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
                        n = at(x + dx, y + dy)
                        if n not in ".o":
                            near.append(pal[n])
                base = max(set(near), key=near.count) if near else (60, 50, 60)
                c = darken(base, 0.32 if outer else 0.55)
                px[x, y] = c + (255,)
                continue
            c = pal[ch]
            # 명암: 오른쪽이나 아래가 선이면 그늘
            if ch in SHADEABLE and (at(x + 1, y) == "o" or at(x, y + 1) == "o"):
                c = shade(c)
            px[x, y] = c + (255,)
    return img


# ---------- 종 ----------
# 이름: (팔레트, 귀 모양, 갓, 너구리 눈 둘레, 꼬리)
SPECIES = {
    "tanuki":      ({}, "round", True, True),
    "elder":       ({"f": (156, 146, 134), "L": (196, 188, 178), "m": (96, 88, 82), "c": (226, 222, 238), "a": (190, 184, 208), "b": (150, 108, 170)}, "round", True, True),
    "gaekju":      ({"f": (128, 86, 58), "L": (166, 120, 84), "l": (220, 186, 146), "c": (118, 150, 200), "a": (90, 116, 164), "b": (232, 196, 110)}, "round", True, False),
    "merchant":    ({"f": (228, 134, 66), "L": (246, 178, 108), "l": (252, 242, 228), "c": (126, 176, 114), "a": (96, 140, 88), "b": (214, 84, 72)}, "pointy", False, False),
    "dakmokchief": ({"f": (224, 198, 152), "L": (242, 224, 190), "l": (252, 246, 234), "c": (206, 194, 162), "a": (170, 158, 128), "b": (124, 104, 84)}, "pointy", True, False),
    "villager_bear":   ({"f": (128, 86, 58), "L": (166, 120, 84), "l": (220, 186, 146), "c": (206, 156, 114), "a": (168, 122, 88), "b": (120, 150, 96)}, "round", False, False),
    "villager_rabbit": ({"f": (246, 242, 236), "L": (255, 255, 255), "l": (255, 224, 230), "c": (236, 156, 176), "a": (200, 120, 142), "b": (250, 230, 160)}, "long", False, False),
    "villager_fox":    ({"f": (228, 134, 66), "L": (246, 178, 108), "l": (252, 242, 228), "c": (174, 126, 204), "a": (136, 94, 164), "b": (250, 214, 120)}, "pointy", False, False),
    "villager_dog":    ({"f": (224, 198, 152), "L": (242, 224, 190), "l": (252, 246, 234), "c": (114, 166, 176), "a": (84, 128, 138), "b": (232, 120, 96)}, "pointy", False, False),
    "villager_tanuki": ({"c": (222, 194, 154), "a": (184, 154, 116), "b": (94, 126, 84)}, "round", False, True),
    # 도적: 잿빛 늑대, 눈가 복면, 어두운 옷
    "bandit":      ({"f": (128, 128, 140), "L": (168, 168, 180), "m": (46, 40, 52), "l": (214, 212, 220), "c": (72, 66, 86), "a": (52, 48, 64), "b": (196, 64, 60), "k": (54, 46, 52)}, "pointy", False, True),
    # NPC 상인: 염소빛 개, 갈색 옷에 노란 띠
    "trader":      ({"f": (196, 170, 132), "L": (226, 206, 172), "l": (248, 240, 224), "c": (150, 104, 72), "a": (118, 80, 56), "b": (236, 196, 92)}, "round", True, False),
}


def build_one(out_dir, name, pal_over, top, hat, mask):
    pal = dict(BASE)
    pal.update(pal_over)
    frames = {}
    for f in range(4):
        for d in ("d", "u", "r"):
            frames[(d, f)] = render(compose(d, f, top, hat, mask), pal)
        frames[("l", f)] = frames[("r", f)].transpose(Image.FLIP_LEFT_RIGHT)
    for (d, f), img in frames.items():
        img.save(os.path.join(out_dir, f"{name}_tall_{d}{f}.png"))
    return frames


def shadow(out_dir):
    img = Image.new("RGBA", (12, 5), (0, 0, 0, 0))
    px = img.load()
    for y in range(5):
        for x in range(12):
            dx, dy = (x - 5.5) / 6, (y - 2) / 2.5
            if dx * dx + dy * dy <= 1:
                px[x, y] = (30, 20, 30, 80)
    img.save(os.path.join(out_dir, "shadow.png"))


def build_all(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    allf = {}
    for name, (pal, top, hat, mask) in SPECIES.items():
        allf[name] = build_one(out_dir, name, pal, top, hat, mask)
    shadow(out_dir)
    return allf


def preview(allf, path, scale=6):
    names = list(allf)
    W, H = len(names) * 20, 4 * 28
    img = Image.new("RGBA", (W, H), (126, 178, 94, 255))
    for i, n in enumerate(names):
        for r, d in enumerate("dulr"):
            img.alpha_composite(allf[n][(d, 1 if r else 0)], (i * 20 + 2, r * 28 + 2))
    img.resize((W * scale, H * scale), Image.NEAREST).save(path)


if __name__ == "__main__":
    out = sys.argv[1]
    allf = build_all(out)
    if len(sys.argv) > 2:
        preview(allf, sys.argv[2])
