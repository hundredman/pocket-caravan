"""10/27 통합: 북유럽 생성기용 타일 그림 읽기 (예전 공용 tiles.tile_image 그대로).
고려 브랜치(C6)에서 공용 tiles.tile_image가 '굽고 등록한 PNG만 읽기'로 바뀌어, 등록 안 된 북유럽 타일에서 내보내기가 멈췄다.
north_tiles.py가 공용 tiles.tile_image를 감싸서: 등록된 타일은 고려 방식(frozen_art) 그대로, 없으면 여기 예전 방식으로 에셋 팩에서 읽는다.
공용 tiles.py는 고치지 않는다. 팀이 북유럽 타일도 등록(굽기)하기로 하면 이 파일은 지운다."""
import os, colorsys
from PIL import Image, ImageDraw
from tiles import SHEETS, PACK

_img = {}


def sheet(name):
    if name not in _img:
        _img[name] = Image.open(os.path.join(PACK, SHEETS[name])).convert("RGBA")
    return _img[name]


def recolor(tile, mode):
    """기와 지붕 같은 색 바꾸기. 'slate': 붉은·주황 계열을 청회색 기와로."""
    px = tile.load()
    w, h = tile.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            hh, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if mode == "slate" and s > 0.35 and (hh < 0.06 or hh > 0.93):  # 붉은 기와만
                nh, ns = 0.62, min(s * 0.35, 0.3)
                nr, ng, nb = colorsys.hsv_to_rgb(nh, ns, v * 0.85)
                px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return tile


def tile_image(key):
    """key = 'sheet:x:y' 또는 'sheet:x:y@recolor'"""
    if key.startswith("module:"):
        from module_art import image_for
        return image_for(key)
    if key.startswith("work:"):
        # 3x2칸 종이 건조대. 기존 타일 축척·팔레트에 맞춘 목재 틀과 한지 묶음.
        _, tx, ty = key.split(":")
        t = Image.new("RGBA", (48, 32))
        d = ImageDraw.Draw(t)
        d.rectangle((4, 27, 44, 30), fill="#65544255")
        for x in (3, 42):
            d.rectangle((x, 1, x + 3, 29), fill="#70513b")
            d.line((x, 2, x, 27), fill="#bb9660")
        for y in (3, 23):
            d.rectangle((3, y, 45, y + 2), fill="#795439")
            d.line((4, y, 44, y), fill="#c19a63")
        for x in (8, 20, 32):
            d.rectangle((x, 7, x + 8, 22), fill="#c7be97")
            d.rectangle((x, 6, x + 7, 20), fill="#eee2b6")
            d.line((x + 1, 7, x + 6, 7), fill="#fff2cc")
            d.point((x + 3, 12), fill="#d8ca9f")
            d.point((x + 5, 17), fill="#d8ca9f")
        x, y = int(tx) * 16, int(ty) * 16
        return t.crop((x, y, x + 16, y + 16))
    if key.startswith("wood:"):
        _, direction, post = key.split(":")
        t = Image.new("RGBA", (16, 16))
        d = ImageDraw.Draw(t)
        d.rectangle((1, 1, 3, 15), fill="#533d2f")
        d.rectangle((1, 1, 2, 15), fill="#ad8050")
        if post == "1":
            d.rectangle((0, 1, 5, 6), fill="#4c372c")
            d.rectangle((1, 1, 4, 4), fill="#d0a66c")
            d.point((2, 2), fill="#f0cd8c")
        if direction == "railhtop":
            return t.transpose(Image.Transpose.ROTATE_270)
        if direction == "railhbottom":
            return t.transpose(Image.Transpose.ROTATE_90)
        if direction == "railvright":
            return t.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        return t
    base, _, rc = key.partition("@")
    s, x, y = base.split(":")
    x, y = int(x), int(y)
    t = sheet(s).crop((x * 16, y * 16, x * 16 + 16, y * 16 + 16))
    if rc.startswith("wood"):
        t = t.copy()
        px = t.load()
        for yy in range(16):
            for xx in range(16):
                r, g, b, a = px[xx, yy]
                px[xx, yy] = (int(r * .67), int(g * .76), min(255, int(b * .9 + 9)), a)
        if rc == "woodturn":
            t = t.transpose(Image.Transpose.ROTATE_90)
    elif rc:
        t = recolor(t.copy(), rc)
    return t
