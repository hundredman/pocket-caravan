"""베네치아 생활 연출·미니게임용 작은 픽셀 그림 (2026-10-26, 임시 아트 제안).
    사용: python Tools/ArtGen/venice_life_art.py
    출력: Assets/PocketCaravan/Resources/Venice/Life/*.png (+ .meta: 스프라이트·점 필터·무압축·PPU 16, 처음 만들 때만)
    VeniceLife·VeniceMinigames가 Resources.Load로 읽는다. 곤돌라는 베네치아 아틀라스(vg_gondola)를 잘라 쓴다."""
import json, os, uuid
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets/PocketCaravan/Resources/Venice/Life")
META = os.path.join(ROOT, "Assets/PocketCaravan/Art/Items/venice_glass.png.meta")
ATLAS = os.path.join(os.path.dirname(__file__), "venice_atlas")

INK = (52, 44, 52, 255)


def save(name, im):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    im.save(path)
    meta = path + ".meta"
    if not os.path.exists(meta):
        with open(META, encoding="utf-8") as fp:
            lines = ["guid: " + uuid.uuid4().hex if l.startswith("guid:") else l for l in fp.read().splitlines()]
        with open(meta, "w", encoding="utf-8", newline="\n") as fp:
            fp.write("\n".join(lines) + "\n")


def pigeon(frame):
    """비둘기 8×8: 0 서 있기, 1 모이 쪼기, 2 날기"""
    im = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    p = im.load()
    body, dark, neck, foot = (150, 154, 168, 255), (96, 100, 116, 255), (110, 150, 140, 255), (214, 120, 120, 255)
    if frame == 2:
        for x in range(1, 7):
            p[x, 3] = body
        for x in (0, 1, 6, 7):
            p[x, 2] = dark
        p[3, 4] = p[4, 4] = body
        p[4, 2] = neck
        return im
    hy = 2 if frame == 0 else 4
    for x in range(2, 6):
        for y in range(4, 6):
            p[x, y] = body
    p[1, 5] = dark; p[6, 4] = dark
    p[5, hy] = neck; p[5, hy + 1] = neck if frame == 0 else body
    p[6, hy] = dark
    p[3, 6] = p[4, 6] = foot
    p[5, hy - 1 if frame == 0 else hy] = dark
    return im


def gull(frame):
    """갈매기 12×7: 날개 위·아래"""
    im = Image.new("RGBA", (12, 7), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    white, grey = (248, 248, 244, 255), (150, 156, 164, 255)
    d.rectangle((4, 3, 7, 4), fill=white)
    d.point((8, 3), fill=(232, 180, 60, 255))
    if frame == 0:
        d.line((0, 1, 4, 3), fill=white); d.line((7, 3, 11, 1), fill=white)
        d.point((0, 1), fill=grey); d.point((11, 1), fill=grey)
    else:
        d.line((0, 5, 4, 3), fill=white); d.line((7, 3, 11, 5), fill=white)
        d.point((0, 5), fill=grey); d.point((11, 5), fill=grey)
    return im


def puff(size):
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((0, 0, size - 1, size - 1), fill=(222, 222, 216, 170))
    d.ellipse((1, 1, size // 2, size // 2), fill=(240, 240, 236, 200))
    return im


def glow():
    """등불 빛 24×24: 계단식 둥근 빛 (가산처럼 보이게 밝은 노랑, 바깥은 투명)"""
    im = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for r, a in ((11, 40), (8, 70), (5, 110), (3, 170)):
        d.ellipse((12 - r, 12 - r, 12 + r, 12 + r), fill=(255, 214, 140, a))
    return im


def lantern():
    """가로등 6×14: 쇠기둥 + 등"""
    im = Image.new("RGBA", (6, 14), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((2, 5, 3, 13), fill=(62, 58, 66, 255))
    d.rectangle((1, 1, 4, 4), fill=(255, 220, 140, 255), outline=(62, 58, 66, 255))
    d.point((2, 0), fill=(62, 58, 66, 255)); d.point((3, 0), fill=(62, 58, 66, 255))
    d.rectangle((1, 13, 4, 13), fill=(62, 58, 66, 255))
    return im


def gondola():
    """아틀라스의 vg_gondola(4×1칸)를 잘라 뱃사공을 얹는다 (64×24)"""
    cells = json.load(open(ATLAS + ".json", encoding="utf-8"))
    atlas = Image.open(ATLAS + ".png").convert("RGBA")
    c = cells["gondola"] if isinstance(cells, dict) and "gondola" in cells else None
    if c is None:
        for e in (cells if isinstance(cells, list) else cells.get("cells", [])):
            if e.get("name") == "gondola":
                c = e
                break
    x, y, w, h = (c["x"], c["y"], c["w"], c["h"]) if isinstance(c, dict) else c[:4]
    boat = atlas.crop((x * 16, y * 16, (x + w) * 16, (y + h) * 16))
    im = Image.new("RGBA", (boat.width, boat.height + 8), (0, 0, 0, 0))
    im.alpha_composite(boat, (0, 8))
    d = ImageDraw.Draw(im)
    gx = boat.width - 18                      # 뒤쪽에 선 뱃사공 (줄무늬 셔츠·밀짚모자)
    o = 5                                     # 배 위에 발이 닿게 내린다
    d.rectangle((gx, 3 + o, gx + 3, 9 + o), fill=(240, 240, 240, 255))
    d.line((gx, 5 + o, gx + 3, 5 + o), fill=(40, 60, 120, 255)); d.line((gx, 7 + o, gx + 3, 7 + o), fill=(40, 60, 120, 255))
    d.rectangle((gx, o, gx + 3, 2 + o), fill=(232, 196, 120, 255))
    d.line((gx - 1, o, gx + 4, o), fill=(200, 160, 90, 255))
    d.line((gx + 4, 4 + o, gx + 8, 18), fill=(110, 80, 50, 255))   # 노
    return im


def wine():
    """포도주 병 16×16 (아이콘)"""
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((6, 1, 9, 4), fill=(60, 90, 60, 255), outline=INK)
    d.rounded_rectangle((4, 4, 11, 14), 2, fill=(110, 34, 58, 255), outline=INK)
    d.rectangle((5, 8, 10, 11), fill=(236, 220, 180, 255))
    d.point((7, 9), fill=(110, 34, 58, 255)); d.point((8, 10), fill=(110, 34, 58, 255))
    d.line((5, 5, 5, 7), fill=(170, 80, 110, 255))
    return im


def glass_art():
    """유리 장식(꽃병) 16×16"""
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon(((6, 2), (9, 2), (12, 9), (10, 14), (5, 14), (3, 9)), fill=(70, 170, 200, 255), outline=INK)
    d.line((5, 7, 10, 7), fill=(230, 120, 60, 255)); d.line((4, 10, 11, 10), fill=(240, 200, 80, 255))
    d.point((6, 4), fill=(230, 250, 255, 255)); d.point((5, 8), fill=(230, 250, 255, 255))
    return im


def lace():
    """레이스 손수건 16×16"""
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon(((2, 3), (14, 3), (13, 13), (3, 13)), fill=(248, 246, 238, 255), outline=(150, 140, 130, 255))
    for x in range(3, 14, 2):
        d.point((x, 2), fill=(248, 246, 238, 255)); d.point((x, 14), fill=(248, 246, 238, 255))
    d.ellipse((6, 6, 10, 10), outline=(200, 120, 150, 255))
    d.point((8, 8), fill=(200, 120, 150, 255))
    return im


def bunting():
    """큰 장 깃발 줄 32×8"""
    im = Image.new("RGBA", (32, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.line((0, 1, 31, 1), fill=(90, 70, 50, 255))
    cols = [(200, 60, 60, 255), (240, 200, 70, 255), (60, 120, 190, 255), (90, 160, 90, 255)]
    for i, x in enumerate(range(1, 31, 5)):
        d.polygon(((x, 2), (x + 4, 2), (x + 2, 6)), fill=cols[i % 4])
    return im


def fair_stall():
    """큰 장 좌판 24×20: 줄무늬 차양 + 상자"""
    im = Image.new("RGBA", (24, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((2, 7, 3, 19), fill=(110, 80, 50, 255)); d.rectangle((20, 7, 21, 19), fill=(110, 80, 50, 255))
    for i in range(6):
        d.rectangle((i * 4, 2, i * 4 + 3, 7), fill=(200, 60, 60, 255) if i % 2 == 0 else (244, 236, 214, 255))
    d.line((0, 7, 23, 7), fill=INK)
    d.rectangle((3, 13, 20, 18), fill=(170, 120, 70, 255), outline=INK)
    for x, c in ((5, (240, 200, 70, 255)), (10, (110, 34, 58, 255)), (15, (70, 170, 200, 255))):
        d.rectangle((x, 10, x + 3, 12), fill=c, outline=INK)
    return im


def lion():
    """날개 달린 사자 문장 돌판 12×12 (이끼 낀 돌 + 금빛 사자)"""
    im = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 1, 11, 11), 2, fill=(176, 168, 150, 255), outline=(96, 90, 80, 255))
    gold = (226, 182, 70, 255)
    d.rectangle((3, 5, 7, 8), fill=gold)            # 몸
    d.rectangle((7, 3, 9, 6), fill=gold)            # 머리
    d.line((2, 4, 5, 2), fill=gold); d.line((2, 5, 5, 3), fill=gold)   # 날개
    d.point((3, 9), fill=gold); d.point((7, 9), fill=gold)
    d.point((1, 10), fill=(110, 150, 90, 255)); d.point((10, 2), fill=(110, 150, 90, 255))
    return im


def confetti():
    """카니발 색종이 4×4"""
    im = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 1, 1), fill=(230, 80, 90, 255)); d.rectangle((2, 2, 3, 3), fill=(250, 210, 80, 255))
    return im


def mask():
    """카니발 가면 12×8"""
    im = Image.new("RGBA", (12, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon(((0, 2), (11, 2), (10, 6), (6, 5), (5, 5), (1, 6)), fill=(250, 246, 236, 255), outline=(140, 40, 60, 255))
    d.ellipse((2, 3, 4, 4), fill=(40, 30, 40, 255)); d.ellipse((7, 3, 9, 4), fill=(40, 30, 40, 255))
    d.line((0, 1, 3, 0), fill=(226, 182, 70, 255)); d.line((11, 1, 8, 0), fill=(226, 182, 70, 255))
    return im


def emote(kind):
    """리액션 말풍선 12×11: wave 인사 / heart 하트 / note 음표 / laugh 웃음"""
    im = Image.new("RGBA", (12, 11), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, 11, 8), 2, fill=(250, 248, 240, 255), outline=INK)
    d.polygon(((4, 8), (6, 8), (4, 10)), fill=(250, 248, 240, 255), outline=INK)
    if kind == "heart":
        c = (220, 70, 90, 255)
        d.rectangle((3, 3, 4, 4), fill=c); d.rectangle((7, 3, 8, 4), fill=c); d.rectangle((4, 4, 7, 5), fill=c); d.rectangle((5, 6, 6, 6), fill=c)
    elif kind == "note":
        c = (60, 90, 170, 255)
        d.line((6, 2, 6, 6), fill=c); d.line((6, 2, 8, 3), fill=c); d.rectangle((4, 5, 5, 6), fill=c)
    elif kind == "laugh":
        c = (230, 160, 40, 255)
        d.ellipse((3, 1, 8, 6), fill=c); d.line((4, 4, 7, 4), fill=INK); d.point((4, 2), fill=INK); d.point((7, 2), fill=INK)
    else:
        c = (90, 150, 90, 255)
        d.line((3, 6, 4, 2), fill=c); d.line((5, 6, 6, 1), fill=c); d.line((7, 6, 8, 2), fill=c)
    return im


def fx(kind):
    """리액션 동작 때 떠오르는 작은 조각 7×7: heart 하트 / note 음표 / z 졸음 / star 반짝 / dust 먼지"""
    im = Image.new("RGBA", (7, 7), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if kind == "heart":
        c, hi = (232, 84, 110, 255), (255, 190, 200, 255)
        d.rectangle((0, 1, 2, 3), fill=c); d.rectangle((4, 1, 6, 3), fill=c); d.rectangle((1, 3, 5, 4), fill=c)
        d.rectangle((2, 5, 4, 5), fill=c); d.point((3, 6), fill=c); d.point((1, 2), fill=hi)
    elif kind == "note":
        c = (70, 100, 190, 255)
        d.line((4, 0, 4, 5), fill=c); d.line((4, 0, 6, 1), fill=c); d.rectangle((2, 4, 4, 6), fill=c); d.point((2, 4), fill=(0, 0, 0, 0))
    elif kind == "z":
        c = (110, 120, 200, 255)
        d.line((1, 1, 5, 1), fill=c); d.line((5, 1, 1, 5), fill=c); d.line((1, 5, 5, 5), fill=c)
    elif kind == "star":
        c, hi = (250, 210, 80, 255), (255, 250, 210, 255)
        d.line((3, 0, 3, 6), fill=c); d.line((0, 3, 6, 3), fill=c); d.rectangle((2, 2, 4, 4), fill=c); d.point((3, 3), fill=hi)
    else:
        c = (236, 228, 214, 220)
        d.ellipse((1, 2, 5, 6), fill=c); d.point((2, 3), fill=(255, 255, 255, 255))
    return im


def seed_sack():
    """모이 주머니 8×8"""
    im = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon(((1, 3), (6, 3), (7, 7), (0, 7)), fill=(200, 170, 110, 255), outline=INK)
    d.line((2, 2, 5, 2), fill=INK)
    d.point((3, 5), fill=(150, 110, 60, 255)); d.point((5, 6), fill=(150, 110, 60, 255))
    return im


def main():
    items = os.path.join(ROOT, "Assets/PocketCaravan/Art/Items")   # 아이콘은 Art 폴더(PixelArtImporter가 가져오기 설정을 맞춘다)
    for name, im in (("venice_wine", wine()), ("venice_glass_art", glass_art()), ("venice_lace", lace())):
        im.save(os.path.join(items, name + ".png"))
    save("bunting", bunting())
    save("lion", lion())
    for k in ("wave", "heart", "note", "laugh"):
        save("emote_" + k, emote(k))
    for k in ("heart", "note", "z", "star", "dust"):
        save("fx_" + k, fx(k))
    save("seed_sack", seed_sack())
    save("confetti", confetti())
    save("mask", mask())
    save("fair_stall", fair_stall())
    for i in range(3):
        save(f"pigeon_{i}", pigeon(i))
    for i in range(2):
        save(f"gull_{i}", gull(i))
    save("puff", puff(6))
    save("glow", glow())
    save("lantern", lantern())
    save("gondola", gondola())
    print("saved to", OUT)


if __name__ == "__main__":
    main()
