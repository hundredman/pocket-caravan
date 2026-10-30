"""본토 서부(베네치아 본토 7맵) 시골 소품 그림 (2026-10-30, 제안·임시 아트).
    석호 마을에 비해 본토가 풀밭만 넓어 보여서, 시골 길가 소품을 16px 칸 그림으로 그린다.
    나무 울타리(가로·세로) · 둥근 건초 · 큰 건초 더미 · 길가 성모상(capitello) · 이정석 · 밀밭 바닥 · 양 두 마리 · 허수아비
    그림은 venice_atlas와 같은 짙은 갈색 외곽선·따뜻한 색을 쓴다.
    사용: python Tools/ArtGen/venice_mainland_art.py → Assets/PocketCaravan/Art/NinjaAdventure/Tiles/vml_*.png
    (타일 에셋은 Unity '베네치아 씬 12개 다시 만들기'에서 MapJsonBuilder가 만든다)"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")

PAL = {
    ".": None,
    "k": (58, 36, 30), "K": (40, 28, 26),                       # 외곽선
    "w": (112, 70, 40), "W": (150, 98, 56), "L": (186, 132, 78),  # 나무
    "h": (150, 104, 40), "H": (206, 160, 70), "Y": (236, 202, 104), "y": (250, 228, 150),  # 건초·밀
    "s": (118, 112, 104), "S": (158, 152, 140), "T": (200, 196, 184), "t": (226, 222, 210),  # 돌
    "r": (176, 64, 48), "R": (214, 96, 66),                      # 지붕 기와
    "b": (64, 96, 168), "B": (120, 156, 214), "g": (236, 196, 90),  # 성모상 옷·후광
    "f": (232, 96, 112), "F": (250, 236, 120), "v": (86, 140, 60), "V": (120, 176, 76),  # 꽃·잎
    "o": (246, 242, 230), "O": (212, 206, 192), "n": (64, 56, 56), "N": (96, 84, 80),    # 양털·얼굴
    "c": (120, 84, 52), "C": (198, 72, 56), "x": (60, 82, 140),  # 허수아비 옷
    "d": (126, 96, 58), "D": (150, 118, 72),                     # 밭 흙
}


def img(rows):
    h, w = len(rows), len(rows[0])
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == w, (row, w)
        for x, ch in enumerate(row):
            c = PAL[ch]
            if c:
                im.putpixel((x, y), c + (255,))
    return im


FENCE_H = [
    "................",
    "................",
    "................",
    "................",
    ".kk..........kk.",
    "kWLk........kWLk",
    "kWWkkkkkkkkkkWWk",
    "kWWLLLLLLLLLLWWk",
    "kWWwwwwwwwwwwWWk",
    "kWWkkkkkkkkkkWWk",
    "kWWkkkkkkkkkkWWk",
    "kWWLLLLLLLLLLWWk",
    "kWWwwwwwwwwwwWWk",
    "kwwkkkkkkkkkkwwk",
    "kwwk........kwwk",
    ".kk..........kk.",
]

FENCE_V = [
    "......kkkk......",
    ".....kWLLWk.....",
    ".....kWWWWk.....",
    ".....kWLWWk.....",
    ".....kWWWwk.....",
    ".....kWLWwk.....",
    ".....kWWWwk.....",
    ".....kWWWwk.....",
    ".....kWLWwk.....",
    ".....kWWWwk.....",
    ".....kWWWwk.....",
    ".....kWLWwk.....",
    ".....kWWWwk.....",
    ".....kwWwwk.....",
    ".....kwwwwk.....",
    "......kkkk......",
]

HAYBALE = [
    "................",
    "................",
    "................",
    ".....kkkkkk.....",
    "...kkHYYYYHkk...",
    "..kHYyYHHYyYHk..",
    ".kHYHkkkkkkHYHk.",
    ".kYHkHYYYYHkHYk.",
    ".kYHkYHkkHYkHYk.",
    ".kYHkYHkkHYkHHk.",
    ".kHHkHYYYYHkHhk.",
    ".kHHHkkkkkkHHhk.",
    "..khHHHHHHHHhk..",
    "...kkhhhhhhkk...",
    ".....kkkkkk.....",
    "................",
]

# 큰 건초 더미 2칸 너비 × 2칸 높이 (위 줄은 위층)
HAYSTACK = [
    "..........kkkkkk................",
    "........kkYyYYYYkk..............",
    "......kkYYyYYHYYYYkk............",
    ".....kYYyYYHYYYyYHYYk...........",
    "....kYyYYHYYYyYYHYYYk...........",
    "...kYYYHYYYyYHYYYYyYYk..........",
    "...kYHYYYyYYHYYYyYHYYk..........",
    "..kYYYyYHYYYYYHYYYYYHYk.........",
    "..kYyYYYYHYYyYYYYHYyYYk.........",
    ".kYYHYYyYYYHYYYyYYYHYYYk........",
    ".kYYYYHYYYYYYHYYYYyYYHYk........",
    ".kHYYyYYHYYyYYYYHYYYYYHk........",
    "kHYHYYYYYHYYYHYYYYHYYHYHk.......",
    "kHHYYHYYYYYyYYYHYYYYYHHHk.......",
    "kHHHYYYHYYYYYHYYYyYHHHHhk.......",
    "khHHHHYYYHYYYYHYYHHHHHhhk.......",
    "khhHHHHHHHHHHHHHHHHHHhhhk.......",
    "kkhhhHHHHHHHHHHHHHHhhhhk........",
    ".kkhhhhhHHHHHHHHHhhhhhkk........",
    "..kkkhhhhhhhhhhhhhhhkkk.........",
    "....kkkkkkkkkkkkkkkkk...........",
    "................................",
    "......y.......H.........y.......",
    "....H...y.........y.........H...",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
]

HAYSTACK = ["." * 32] * 9 + HAYSTACK[:21] + ["...." + "H...y" * 5 + "...", "." * 32]   # 발밑이 아래 칸 바닥에 오게 내린다

# 길가 성모상 1칸 × 2칸 (위 칸은 위층)
SHRINE = [
    "................",
    ".......kk.......",
    "......kRRk......",
    ".....kRRRRk.....",
    "....kRrRRrRk....",
    "...kRrrrrrrRk...",
    "..kkkkkkkkkkkk..",
    "...kTTTTTTTTk...",
    "...kTkkkkkkTk...",
    "...kTkgggggkTk..",
    "...kTkgBBgkTk...",
    "...kTkbBBbkTk...",
    "...kTkbbbbkTk...",
    "...kTkxbbxkTk...",
    "...kTkkkkkkTk...",
    "..ktTTTTTTTTtk..",
    "..kfFkTTTTkfVk..",
    "..kVvkTSSTkvVk..",
    "...kkkTSSTkkk...",
    "....kTTSSTTk....",
    "....kTSSSSTk....",
    "....kTSSSSTk....",
    "....kTSsSSTk....",
    "....kTSSSsTk....",
    "....kTSSSSTk....",
    "...ktTSSSSTtk...",
    "..kfktTTTTtkfk..",
    "..kVvkkkkkkVVk..",
    "..kkVvfFfVvVkk..",
    "...kkkkkkkkkk...",
    "................",
    "................",
]
SHRINE = [r.replace("kTkgggggkTk..", "kTkggggkTk...") for r in SHRINE]

MILESTONE = [
    "................",
    "................",
    "................",
    "................",
    "................",
    "......kkkk......",
    ".....kTttTk.....",
    "....kTtTTTSk....",
    "....kTTTTTSk....",
    "....kTSSSSSk....",
    "....kTTTTTSk....",
    "....kTSSSTSk....",
    "....kTTTTSSk....",
    "...vkSSSSSskv...",
    "..vVkkkkkkkkVv..",
    "................",
]

# 밀밭 바닥 2×2 무늬 (흙 위에 밀 줄)
WHEAT = [
    "dYyHdYyHdHyYdYyH",
    "dYHHdHYHdHYHdHYH",
    "DhYHDhYHDhHYDhYH",
    "dHyhdHYhdyHhdHyh",
    "DYHhDYHhDYhhDYHh",
    "dhYHdhyHdhYHdhYH",
    "DHYhDHYhDHyhDHYh",
    "dYHhdyHhdYHhdYHh",
    "DhYHDhYHDhYHDhyH",
    "dHyhdHYhdHYhdHYh",
    "DYHhDYHhDyHhDYHh",
    "dhYHdhYHdhYHdhYH",
    "DHyhDHYhDHYhDHyh",
    "dYHhdYHhdyHhdYHh",
    "DhYHDhyHDhYHDhYH",
    "dHYhdHYhdHYhdHYh",
] * 2
WHEAT = [r + r.replace("Y", "y", 1) for r in WHEAT]

SHEEP = [
    "................",
    "................",
    "................",
    "................",
    "......kkkkk.....",
    "....kkoOooOkk...",
    "...koOooOooOok..",
    "..kkOooOooOooOk.",
    ".kNnkoOooOoooOk.",
    "kNnnNkooOooOoOk.",
    "knnkNkOooOooOOk.",
    ".kkNkkOOOOOOOk..",
    "...kkOkkOkkkOk..",
    "....knk.knk.kn..",
    "....kkk.kkk.kk..",
    "................",
]

SCARECROW = [
    "................",
    "......kkkk......",
    ".....kHHHHk.....",
    "...kkHHHHHHkk...",
    "..khhhhhhhhhhk..",
    ".....kyyyyk.....",
    ".....kynnyk.....",
    ".....kyyyyk.....",
    "..kkkkkCCkkkkk..",
    ".kHkCCCCCCCCkHk.",
    "kHkkCCxCCxCCkkHk",
    ".k..kCCCCCCk..k.",
    ".....kCxCCCk....",
    ".....kxxxxxk....",
    "......kxkxk.....",
    ".......kWk......",
    ".......kWk......",
    ".......kWk......",
    ".......kWk......",
    ".......kWk......",
    ".......kWk......",
    ".......kWk......",
    ".......kwk......",
    "......kwwwk.....",
    ".....vkkkkkv....",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
]


def cut(name, im):
    """큰 그림을 16px 칸으로 잘라 vml_<이름>_<x>_<y>.png로 쓴다. 빈 칸은 쓰지 않는다."""
    out = []
    for j in range(im.height // 16):
        for i in range(im.width // 16):
            c = im.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16))
            if c.getbbox() is None:
                continue
            n = f"vml_{name}_{i}_{j}"
            c.save(os.path.join(OUT, n + ".png"))
            out.append(n)
    return out


def main():
    made = []
    made += cut("fence_h", img(FENCE_H))
    made += cut("fence_v", img(FENCE_V))
    made += cut("haybale", img(HAYBALE))
    made += cut("haystack", img(HAYSTACK))
    made += cut("shrine", img(SHRINE))
    made += cut("milestone", img(MILESTONE))
    made += cut("wheat", img(WHEAT))
    sheep = img(SHEEP)
    made += cut("sheep", sheep)
    made += cut("sheepl", sheep.transpose(Image.FLIP_LEFT_RIGHT))
    made += cut("scarecrow", img(SCARECROW))
    print(len(made), "칸:", " ".join(made))
    # 확인용 한 장
    sheet = Image.new("RGBA", (16 * 14, 32), (92, 132, 64, 255))
    x = 0
    for n in made:
        sheet.alpha_composite(Image.open(os.path.join(OUT, n + ".png")), (x % (16 * 14), 16 * (x // (16 * 14))))
        x += 16
    return made


if __name__ == "__main__":
    main()
