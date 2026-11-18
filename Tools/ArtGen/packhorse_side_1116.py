"""짐말 옆모습 다듬기 (11/16 은샘): 허리 2칸 늘리고 앞뒤 다리 사이 2칸 띄우고, 짐을 2칸 뒤로.

pack_animals_v1.py가 만든 PackHorse/side_*.png(걷기 _a/_b 포함)에 덮어쓴다. 그림 크기(40×32)는 그대로.
실행: python Tools/ArtGen/packhorse_side_1116.py Assets/PocketCaravan/Resources/Transport/PackHorse
한 번만 돌린다 (이미 고친 그림에 다시 돌리면 또 늘어난다).
"""
import sys
from pathlib import Path
from PIL import Image

BELLY = 25   # 이 줄부터 다리
WAIST = 27   # 짐 뒤 몸통: 이 열을 두 번 더 넣는다
LEGS = 20    # 이 열 뒤에 다리 사이 빈칸 두 칸


def stretch(im):
    w, h = im.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        for x in range(w):
            if y < BELLY:
                p = im.getpixel((x, y)) if x <= WAIST else im.getpixel((WAIST, y)) if x <= WAIST + 2 else im.getpixel((x - 2, y))
            else:
                p = im.getpixel((x, y)) if x <= LEGS else (0, 0, 0, 0) if x <= LEGS + 2 else im.getpixel((x - 2, y))
            out.putpixel((x, y), p)
    return out


def main(folder):
    d = Path(folder)
    for step in ("", "_a", "_b"):
        base = stretch(Image.open(d / f"side_0{step}.png").convert("RGBA"))
        loads = {n: stretch(Image.open(d / f"side_{n}{step}.png").convert("RGBA")) for n in (1, 2, 3)}
        base.save(d / f"side_0{step}.png")
        w, h = base.size
        for n, im in loads.items():
            # 짐 = 빈 말 그림과 다른 칸. 두 칸 뒤로 옮겨 빈 말 위에 얹는다
            out = base.copy()
            for y in range(h):
                for x in range(w - 2):
                    p = im.getpixel((x, y))
                    if p != base.getpixel((x, y)):
                        out.putpixel((x + 2, y), p)
            out.save(d / f"side_{n}{step}.png")


if __name__ == "__main__":
    main(sys.argv[1])
