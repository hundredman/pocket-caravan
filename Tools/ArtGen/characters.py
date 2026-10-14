"""에셋 팩 캐릭터(16x16, 4방향 x 4프레임 걷기)를 바탕으로 우리 캐릭터를 만든다.
사용법: python3 characters.py [저장소 루트]
결과: Assets/PocketCaravan/Art/Characters/<이름>_<방향><프레임>.png  (방향 d/u/l/r, 프레임 0~3)
"""
import os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PACK = os.environ.get("NA_PACK", os.path.join(HERE, "..", "..", "..", "Ninja Adventure - Asset Pack"))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "..", "..")
OUT = os.path.join(ROOT, "Assets/PocketCaravan/Art/Characters")
DIRS = "dulr"   # 팩 Walk.png 열 순서: 아래, 위, 왼쪽, 오른쪽

HAT = (46, 44, 54, 255)
HAT_HI = (74, 72, 88, 255)
BEARD = (244, 240, 232, 255)

# 이름: (팩 캐릭터, 갓, 흰 수염)
CHARACTERS = {
    "tanuki":      ("MaskRacoon", True, False),      # 플레이어: 갓 쓴 너구리 행상
    "elder":       ("MaskGoldRacoon", True, True),   # 나루 마을 촌장
    "gaekju":      ("Pig", True, False),             # 개경 객주
    "dakmokchief": ("Monkey", True, True),           # 닥나무 마을 촌장
    "merchant":    ("LionYellow", False, False),     # 장터 상인
    "villager_pig": ("Pig", False, False),
    "villager_monkey": ("Monkey", False, False),
    "villager_lion": ("Lion", False, False),
    "villager_frog": ("MaskFrog", False, False),
    "villager_gold": ("MaskGoldRacoon", False, False),
}


def head_box(fr):
    """프레임에서 불투명한 맨 윗줄과 그 줄의 좌우 끝"""
    px = fr.load()
    for y in range(16):
        xs = [x for x in range(16) if px[x, y][3] > 0]
        if xs:
            return y, min(xs), max(xs)
    return 0, 4, 11


def add_gat(fr, direction):
    """갓: 머리 위 둥근 모자와 넓은 챙"""
    px = fr.load()
    top, x0, x1 = head_box(fr)
    # 머리 폭 (윗줄에서 두 줄 아래가 더 정확)
    y_brim = min(top + 2, 15)
    xs = [x for x in range(16) if px[x, y_brim][3] > 0]
    if xs:
        x0, x1 = min(xs), max(xs)
    cx = (x0 + x1) / 2
    if direction == "l": cx -= 0.5
    if direction == "r": cx += 0.5
    # 챙: 머리보다 양쪽 1칸 넓게
    for x in range(max(0, x0 - 1), min(16, x1 + 2)):
        px[x, y_brim] = HAT
    # 모자 꼭대기: 폭 5, 높이 3
    c0 = int(round(cx - 2))
    for y in range(max(0, y_brim - 3), y_brim):
        for x in range(c0, c0 + 5):
            if 0 <= x < 16:
                px[x, y] = HAT
    for x in range(c0 + 1, c0 + 4):
        if 0 <= x < 16 and y_brim - 3 >= 0:
            px[x, y_brim - 3] = HAT_HI
    return fr


def add_beard(fr, direction):
    if direction == "u":
        return fr
    px = fr.load()
    top, x0, x1 = head_box(fr)
    cx = (x0 + x1) // 2 + (-1 if direction == "l" else 1 if direction == "r" else 0)
    yb = min(top + 9, 14)
    for (dx, dy) in [(0, 0), (-1, 0), (1, 0), (0, 1)]:
        x, y = cx + dx, yb + dy
        if 0 <= x < 16 and 0 <= y < 16 and px[x, y][3] > 0:
            px[x, y] = BEARD
    return fr


def build():
    os.makedirs(OUT, exist_ok=True)
    for name, (src, gat, beard) in CHARACTERS.items():
        sheet = Image.open(os.path.join(PACK, "Actor/Character", src, "SeparateAnim/Walk.png")).convert("RGBA")
        for di, d in enumerate(DIRS):
            for f in range(4):
                fr = sheet.crop((di * 16, f * 16, di * 16 + 16, f * 16 + 16))
                if beard: fr = add_beard(fr, d)
                if gat: fr = add_gat(fr, d)
                fr.save(os.path.join(OUT, f"{name}_{d}{f}.png"))
    print(len(CHARACTERS), "characters ->", OUT)


def preview(path):
    names = list(CHARACTERS)
    s = 6
    img = Image.new("RGBA", (len(names) * (64 * s + 12), 64 * s), (140, 180, 110, 255))
    for i, n in enumerate(names):
        for di, d in enumerate(DIRS):
            for f in range(4):
                fr = Image.open(os.path.join(OUT, f"{n}_{d}{f}.png")).resize((16 * s, 16 * s), Image.NEAREST)
                img.alpha_composite(fr, (i * (64 * s + 12) + di * 16 * s, f * 16 * s))
    img.save(path)


if __name__ == "__main__":
    build()
    if len(sys.argv) > 2:
        preview(sys.argv[2])
