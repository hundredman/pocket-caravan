"""북유럽 마을을 돌아다니는 작은 동물 그림 (옆모습, 오른쪽을 봄, 걷기 2프레임). chars.py의 색 테두리 렌더러를 쓴다.
출력: Art/Characters/north_critter_<종>_<r|l><0|1>.png  (NorthLife.cs가 읽음)"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chars
from PIL import Image

PAL = dict(chars.BASE)
PAL.update({"A": (222, 202, 160), "d": (40, 40, 52), "D": (70, 72, 90), "y": (236, 190, 80), "g": (190, 190, 200)})

BODY = {
    # 순록: 갈색 몸, 흰 목털, 뿔
    "reindeer": ({"f": (150, 110, 80), "L": (194, 156, 118), "l": (238, 226, 206)}, [
        "..........o..o..",
        ".........oAooAo.",
        "..........oAAo..",
        "..........offo..",
        "...oooooooffeLo.",
        "..offLLLLffflnno",
        ".offffffffffloo.",
        ".offffffffffo...",
        "..olllllllllo...",
    ], ["...ok..ok.ok..ok", "...ok..ok.ok..ok", "...os..os.os..os"],
       ["..ok..ok..ok.ok.", "..ok..ok..ok.ok.", "..os..os..os.os."]),
    # 염소: 흰 몸, 회색 굽은 뿔, 턱수염
    "goat": ({"f": (236, 232, 222), "L": (252, 250, 244), "l": (255, 255, 250), "A": (150, 146, 140)}, [
        "................",
        "..........ooo...",
        ".........oAAAo..",
        "..........offo..",
        "....oooooooffeo.",
        "...offLLLffffnno",
        "..offffffffflo..",
        "..offffffffflo..",
        "...ollllllllo...",
    ], ["....ok.ok.ok.ok.", "....ok.ok.ok.ok.", "....os.os.os.os."],
       ["...ok.ok..ok.ok.", "...ok.ok..ok.ok.", "...os.os..os.os."]),
}
BIRD = {
    # 까마귀(검정), 갈매기(흰·회색). 깡충 2프레임
    "raven": ({"f": (40, 40, 52), "L": (78, 80, 100), "l": (60, 60, 76), "n": (236, 190, 80)}, [
        "......ooo.......",
        ".....offfo......",
        "....offfeLon....",
        "...offfffoo.....",
        "..offLLfffo.....",
        ".offfffffo......",
        "..ooofffo.......",
        ".....oko........",
    ]),
    "gull": ({"f": (244, 244, 248), "L": (255, 255, 255), "l": (176, 180, 196), "n": (236, 190, 80)}, [
        "......ooo.......",
        ".....offfo......",
        "....offfeLon....",
        "...offfffoo.....",
        "..olllLffffo....",
        ".ollllffffo.....",
        "..ooofffo.......",
        ".....oko........",
    ]),
}


def grid(lines, h):
    rows = [list((l + "." * 16)[:16]) for l in lines]
    while len(rows) < h:
        rows.insert(0, list("." * 16))
    return rows


def build(out):
    os.makedirs(out, exist_ok=True)
    made = {}
    for name, (pal_over, body, legs0, legs1) in BODY.items():
        pal = dict(PAL); pal.update(pal_over)
        for f, legs in enumerate((legs0, legs1)):
            img = chars.render(grid(body + legs, 16), pal)
            made[(name, f)] = img
    for name, (pal_over, body) in BIRD.items():
        pal = dict(PAL); pal.update(pal_over)
        img0 = chars.render(grid(body, 16), pal)
        img1 = Image.new("RGBA", img0.size); img1.alpha_composite(img0, (0, -2))   # 깡충
        made[(name, 0)], made[(name, 1)] = img0, img1
    # 10/25 순록은 더 크고 자세한 새 그림 (north_reindeer_art.py): 걷기 4프레임 + 풀 뜯기
    import north_reindeer_art
    rd = north_reindeer_art.frames()
    for f, img in enumerate(rd["walk"]):
        made[("reindeer", f)] = img
    made[("reindeer_graze", 0)] = rd["graze"]
    for (name, f), img in made.items():
        img.save(os.path.join(out, f"north_critter_{name}_r{f}.png"))
        img.transpose(Image.FLIP_LEFT_RIGHT).save(os.path.join(out, f"north_critter_{name}_l{f}.png"))
    return made


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "..", "Assets/PocketCaravan/Art/Characters")
    made = build(out)
    if len(sys.argv) > 2:
        S = 8; names = sorted({n for n, _ in made})
        sheet = Image.new("RGBA", (len(names) * 2 * 18 * S, 18 * S), (236, 232, 238, 255))
        for i, n in enumerate(names):
            for f in (0, 1):
                sheet.alpha_composite(made[(n, f)].resize((16 * S, 16 * S), Image.NEAREST), ((i * 2 + f) * 18 * S, S))
        sheet.save(sys.argv[2])
    print(", ".join(sorted({n for n, _ in made})))
