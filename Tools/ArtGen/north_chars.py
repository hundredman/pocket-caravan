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
ANTLERS = {
    "d": ["................", ".o.o........o.o.", ".oAo.o....o.oAo.", "..oAoAo..oAoAo..", "...oAAo..oAAo...", "....oAo..oAo....", "................", "................"],
    "r": ["................", "......o.o.......", "......oAo.o.....", ".......oAoAo....", ".......oAAo.....", "........oAo.....", "................", "................"],
}

SPECIES = {
    # 갈색 털, 하얀 주둥이, 뿔, 파란 웃옷에 가죽 띠
    "north_reindeer": {"f": (150, 110, 80), "L": (194, 156, 118), "l": (238, 226, 206), "n": (60, 42, 40),
                       "c": (66, 108, 156), "a": (48, 82, 122), "b": (204, 170, 120), "k": (98, 80, 64), "s": (62, 46, 40)},
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
    allf = {n: build(out, n, p) for n, p in SPECIES.items()}
    if len(sys.argv) > 2:
        chars.preview(allf, sys.argv[2])
    print(", ".join(allf))
