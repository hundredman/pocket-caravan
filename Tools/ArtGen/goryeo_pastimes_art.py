"""고려 놀이(투호·떡메치기·제기차기) 창 그림 (2026-11-25).

배경 3장은 이미 있는 고려 그림(Art/Baked/Objects, Art/Goryeo/Markets, Resources/Goryeo/VillageLife/Art)을
한 장에 모아 굽는다. 소품(항아리·화살·떡메·떡판·제기·별)만 여기서 새로 찍는다.
결과: Assets/PocketCaravan/Resources/Goryeo/Pastimes/*.png (창에서는 2배로 그린다. 놀이 칸 352x132 = 176x66)

    python Tools/ArtGen/goryeo_pastimes_art.py
"""
import os
import random
import uuid
from PIL import Image, ImageDraw

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "PocketCaravan"))
OUT = os.path.join(ROOT, "Resources", "Goryeo", "Pastimes")
BAKED = os.path.join(ROOT, "Art", "Baked", "Objects")
MARKET = os.path.join(ROOT, "Art", "Goryeo", "Markets")
LIFE = os.path.join(ROOT, "Resources", "Goryeo", "VillageLife", "Art")
W, H, GROUND = 176, 66, 44          # 배경 크기와 땅이 시작하는 줄
INK = (58, 40, 36, 255)


def baked(prefix):
    for n in sorted(os.listdir(BAKED)):
        if n.startswith(prefix) and n.endswith(".png"):
            return Image.open(os.path.join(BAKED, n)).convert("RGBA")
    raise FileNotFoundError(prefix)


def art(folder, name):
    return Image.open(os.path.join(folder, name + ".png")).convert("RGBA")


def sky(top, bottom):
    im = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(im)
    bands = 6
    for i in range(bands):                      # 계단식 그라데이션 (픽셀 그림답게)
        t = i / (bands - 1)
        c = tuple(round(top[k] + (bottom[k] - top[k]) * t) for k in range(3)) + (255,)
        d.rectangle([0, i * GROUND // bands, W, (i + 1) * GROUND // bands], fill=c)
    return im


def hills(im, color, seed, base, amp):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(im)
    y = base
    pts = []
    for x in range(0, W + 8, 8):
        y = max(base - amp, min(base + 2, y + rnd.randint(-4, 4)))
        pts.append((x, y))
    d.polygon([(0, GROUND)] + pts + [(W, GROUND)], fill=color)


def cloud(im, x, y, w):
    d = ImageDraw.Draw(im)
    c = (255, 253, 244, 255)
    d.rectangle([x, y + 2, x + w, y + 4], fill=c)
    d.rectangle([x + 3, y, x + w - 5, y + 2], fill=c)
    d.rectangle([x + w // 2, y - 2, x + w - 8, y], fill=c)


def ground(im, base, dark, light, seed):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(im)
    d.rectangle([0, GROUND, W, H], fill=base)
    d.rectangle([0, GROUND, W, GROUND], fill=dark)
    for _ in range(70):                          # 잔무늬
        x, y = rnd.randrange(W), rnd.randrange(GROUND + 2, H)
        c = dark if rnd.random() < .5 else light
        d.rectangle([x, y, x + rnd.choice([1, 2, 3]), y], fill=c)


def put(im, sp, x, bottom, fade=0.0, tint=(214, 226, 214)):
    """bottom 줄에 발이 닿게 놓는다. fade만큼 하늘빛을 섞어 뒤로 물러나 보이게"""
    if fade > 0:
        sp = sp.copy()
        px = sp.load()
        for yy in range(sp.height):
            for xx in range(sp.width):
                r, g, b, a = px[xx, yy]
                if a:
                    px[xx, yy] = (round(r + (tint[0] - r) * fade), round(g + (tint[1] - g) * fade), round(b + (tint[2] - b) * fade), a)
    im.alpha_composite(sp, (x, bottom - sp.height)) if 0 <= x and bottom - sp.height >= 0 else im.paste(sp, (x, bottom - sp.height), sp)


def shadow(im, cx, y, w):
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rectangle([cx - w // 2 + 2, y - 1, cx + w // 2 - 2, y + 2], fill=(60, 40, 40, 46))
    d.rectangle([cx - w // 2, y, cx + w // 2, y + 1], fill=(60, 40, 40, 46))
    im.alpha_composite(layer)


def bg_tuho():
    im = sky((178, 214, 226), (238, 236, 214))
    cloud(im, 18, 6, 22); cloud(im, 120, 11, 18)
    hills(im, (160, 190, 182, 255), 3, 30, 10)
    wall = baked("c6_goryeo_wall")
    for x in range(-10, W, wall.width - 2):
        put(im, wall, x, GROUND + 3, .18)
    put(im, baked("c6_goryeo_manor"), 62, GROUND + 4, .10)
    put(im, baked("c6_goryeo_woodland1"), -8, GROUND + 5, .05)
    ground(im, (216, 196, 160, 255), (190, 166, 132, 255), (232, 216, 184, 255), 11)
    lantern = baked("c6_goryeo_lantern")
    shadow(im, 52, GROUND + 6, 12); shadow(im, 168, GROUND + 6, 12)
    put(im, lantern, 44, GROUND + 7); put(im, lantern, 160, GROUND + 7)
    return im


def bg_tteok():
    im = sky((186, 218, 222), (240, 236, 212))
    cloud(im, 70, 5, 24); cloud(im, 150, 12, 16)
    hills(im, (150, 186, 160, 255), 7, 28, 12)
    put(im, baked("c6_goryeo_woodland5"), 52, GROUND + 3, .14)
    put(im, baked("c6_goryeo_cottage"), -22, GROUND + 5, .08)
    put(im, art(MARKET, "grain_stall"), 128, GROUND + 5, .04)
    ground(im, (198, 158, 136, 255), (170, 130, 112, 255), (214, 178, 156, 255), 5)
    shadow(im, 112, GROUND + 5, 28)
    put(im, art(LIFE, "jars"), 96, GROUND + 6)
    return im


def bg_jegi():
    im = sky((172, 212, 228), (232, 238, 216))
    cloud(im, 30, 8, 20); cloud(im, 96, 4, 26)
    hills(im, (140, 178, 150, 255), 9, 26, 14)
    put(im, baked("c6_goryeo_woodland2"), -6, GROUND + 3, .16)
    put(im, baked("c6_goryeo_woodland4"), 30, GROUND + 4, .10)
    put(im, baked("c6_goryeo_cottage"), 92, GROUND + 4, .08)
    put(im, baked("c6_goryeo_woodland7"), 150, GROUND + 5, .04)
    ground(im, (128, 168, 104, 255), (104, 146, 88, 255), (150, 186, 120, 255), 2)
    d = ImageDraw.Draw(im)                       # 아이들이 놀아서 다져진 흙 마당
    d.rectangle([34, GROUND + 6, 142, H - 3], fill=(206, 176, 140, 255))
    d.rectangle([40, GROUND + 4, 136, H - 1], fill=(206, 176, 140, 255))
    rnd = random.Random(4)
    for _ in range(26):
        x, y = rnd.randrange(38, 138), rnd.randrange(GROUND + 6, H - 1)
        d.rectangle([x, y, x + 2, y], fill=(184, 152, 118, 255) if rnd.random() < .6 else (222, 196, 162, 255))
    return im


def outlined(im):
    """투명한 칸 중 그림과 맞닿은 칸에 테두리"""
    src = im.load()
    out = im.copy()
    o = out.load()
    for y in range(im.height):
        for x in range(im.width):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < im.width and 0 <= ny < im.height and src[nx, ny][3]:
                    o[x, y] = INK
                    break
    return out


def canvas(w, h):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def pot():
    """투호 병: 목이 좁고 귀가 둘 달린 청자빛 병"""
    im, d = canvas(18, 22)
    body, hi, lo, band = (118, 164, 150, 255), (176, 210, 196, 255), (84, 126, 118, 255), (70, 104, 100, 255)
    d.ellipse([3, 9, 14, 20], fill=body)
    d.rectangle([5, 19, 12, 20], fill=lo)
    d.rectangle([7, 3, 10, 10], fill=body)
    d.rectangle([6, 1, 11, 2], fill=hi)
    d.rectangle([7, 2, 10, 2], fill=(40, 32, 34, 255))          # 병 입
    d.rectangle([4, 4, 5, 8], fill=body); d.rectangle([12, 4, 13, 8], fill=body)   # 귀
    d.rectangle([4, 4, 5, 4], fill=(40, 32, 34, 255)); d.rectangle([12, 4, 13, 4], fill=(40, 32, 34, 255))
    d.rectangle([4, 13, 13, 13], fill=band)                      # 띠 무늬
    for x in (5, 8, 11):
        d.point((x, 15), fill=hi)
    d.rectangle([5, 11, 5, 17], fill=hi); d.point((6, 10), fill=hi)
    d.rectangle([13, 12, 13, 17], fill=lo); d.rectangle([10, 4, 10, 9], fill=lo)
    return outlined(im)


def arrow(dx, dy, w, h):
    """dx·dy 방향으로 날아가는 화살 (살대 나무, 촉 쇠, 깃 붉은색)"""
    im, d = canvas(w, h)
    x0, y0 = (1, h // 2 - dy * (h // 2 - 2))
    x1, y1 = (w - 2, h // 2 + dy * (h // 2 - 2))
    d.line([x0, y0, x1, y1], fill=(170, 124, 76, 255), width=1)
    d.rectangle([x1 - 1, y1 - 1, x1, y1], fill=(92, 92, 104, 255))
    d.rectangle([x0, y0 - 1, x0 + 2, y0 + 1], fill=(206, 76, 62, 255))
    d.point((x0 + 1, y0), fill=(250, 240, 230, 255))
    return outlined(im)


def arrow_at(deg):
    """deg만큼 기운 화살 (0 = 오른쪽으로 수평, +는 아래로). 19x19 한가운데가 화살 가운데"""
    import math
    im, d = canvas(19, 19)
    c, r = 9, 7
    dx, dy = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    x0, y0, x1, y1 = round(c - dx * r), round(c - dy * r), round(c + dx * r), round(c + dy * r)
    d.line([x0, y0, x1, y1], fill=(170, 124, 76, 255), width=1)
    d.point((x1, y1), fill=(92, 92, 104, 255)); d.point((round(c + dx * (r - 1)), round(c + dy * (r - 1))), fill=(92, 92, 104, 255))
    fx, fy = round(c - dx * (r - 1)), round(c - dy * (r - 1))
    d.rectangle([fx - 1, fy - 1, fx + 1, fy + 1], fill=(206, 76, 62, 255))
    d.point((fx, fy), fill=(250, 240, 230, 255))
    return outlined(im)


def mallet(deg):
    """자루째 그린 떡메. 그림 한가운데가 손(돌리는 축), deg는 자루가 수평에서 위로 선 각도"""
    import math
    size, c, ln = 64, 32, 23
    big = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    ux, uy = math.cos(math.radians(deg)), -math.sin(math.radians(deg))      # 자루 방향
    vx, vy = -uy, ux                                                         # 메 머리 방향 (자루와 직각)
    ex, ey = c + ux * ln, c + uy * ln
    d.line([c - ux * 3, c - uy * 3, ex, ey], fill=(150, 108, 66, 255), width=2)
    def quad(a0, a1, b0, b1):
        return [(ex + ux * a0 + vx * b0, ey + uy * a0 + vy * b0), (ex + ux * a1 + vx * b0, ey + uy * a1 + vy * b0),
                (ex + ux * a1 + vx * b1, ey + uy * a1 + vy * b1), (ex + ux * a0 + vx * b1, ey + uy * a0 + vy * b1)]
    d.polygon(quad(-4, 4, -6, 7), fill=(196, 152, 100, 255))
    d.polygon(quad(-4, -2, -6, 7), fill=(222, 184, 132, 255))                # 밝은 쪽
    d.polygon(quad(-4, 4, 5, 7), fill=(150, 108, 66, 255))                   # 떡을 치는 면
    return outlined(big)


def map_tteok():
    """맵에서 아주머니 앞에 놓는 작은 떡판과 떡 (18x10)"""
    im, d = canvas(18, 10)
    d.rectangle([1, 5, 16, 7], fill=(176, 130, 84, 255)); d.rectangle([1, 5, 16, 5], fill=(210, 168, 116, 255))
    d.rectangle([2, 8, 4, 8], fill=(138, 98, 62, 255)); d.rectangle([13, 8, 15, 8], fill=(138, 98, 62, 255))
    d.rectangle([4, 2, 13, 4], fill=(250, 246, 236, 255)); d.rectangle([6, 1, 11, 1], fill=(250, 246, 236, 255))
    d.rectangle([5, 4, 12, 4], fill=(220, 210, 196, 255))
    return outlined(im)


def gauge():
    """투호 힘 게이지 틀 (속은 코드에서 채운다): 12x40, 안쪽 8x36"""
    im, d = canvas(12, 40)
    d.rectangle([1, 1, 10, 38], fill=(255, 252, 244, 255))
    d.rectangle([2, 2, 9, 37], fill=(232, 222, 204, 255))
    return outlined(im)


def arrow_stuck():
    im, d = canvas(7, 13)
    d.line([4, 11, 2, 2], fill=(170, 124, 76, 255), width=1)
    d.rectangle([1, 1, 3, 3], fill=(206, 76, 62, 255))
    d.point((2, 2), fill=(250, 240, 230, 255))
    return outlined(im)


def mallet_head():
    im, d = canvas(12, 10)
    d.rectangle([1, 1, 10, 8], fill=(196, 152, 100, 255))
    d.rectangle([1, 1, 10, 2], fill=(222, 184, 132, 255))
    d.rectangle([1, 7, 10, 8], fill=(150, 108, 66, 255))
    d.rectangle([4, 3, 4, 6], fill=(170, 126, 80, 255)); d.rectangle([8, 4, 8, 6], fill=(170, 126, 80, 255))
    return outlined(im)


def board():
    """떡판 (안반): 땅에 놓는 낮고 넓은 판"""
    im, d = canvas(40, 8)
    d.rectangle([1, 1, 38, 4], fill=(176, 130, 84, 255))
    d.rectangle([1, 1, 38, 2], fill=(210, 168, 116, 255))
    d.rectangle([1, 4, 38, 4], fill=(138, 98, 62, 255))
    for x in (7, 17, 28):
        d.rectangle([x, 3, x + 5, 3], fill=(150, 108, 70, 255))
    d.rectangle([3, 5, 7, 6], fill=(138, 98, 62, 255)); d.rectangle([32, 5, 36, 6], fill=(138, 98, 62, 255))
    return outlined(im)


def tteok(stage):
    """떡 반죽: 0 거친 찐쌀 → 1 뭉쳐짐 → 2 매끈한 떡"""
    im, d = canvas(26, 10)
    white, hi, lo = (250, 246, 236, 255), (255, 255, 255, 255), (220, 210, 196, 255)
    if stage == 0:
        rnd = random.Random(1)
        d.rectangle([3, 5, 22, 8], fill=white)
        for x in range(3, 22, 2):
            d.rectangle([x, 4 - rnd.randint(0, 2), x + 1, 5], fill=white)
        for _ in range(12):
            d.point((rnd.randrange(4, 22), rnd.randrange(4, 8)), fill=lo)
    elif stage == 1:
        d.rectangle([2, 4, 23, 8], fill=white); d.rectangle([4, 3, 12, 4], fill=white); d.rectangle([15, 2, 20, 4], fill=white)
        d.rectangle([3, 8, 22, 8], fill=lo); d.point((8, 5), fill=lo); d.point((17, 6), fill=lo)
    else:
        d.rectangle([2, 4, 23, 8], fill=white); d.rectangle([4, 2, 21, 4], fill=white); d.rectangle([7, 1, 18, 2], fill=white)
        d.rectangle([3, 8, 22, 8], fill=lo); d.rectangle([8, 2, 13, 2], fill=hi); d.point((6, 4), fill=hi)
    return outlined(im)


def jegi(up):
    """제기: 엽전에 한지 술 (작게, 7x9). up이면 술이 위로 (떨어질 때), 아니면 아래로 날린다 (차올린 직후)"""
    im, d = canvas(7, 9)
    coin, hole = (226, 180, 70, 255), (120, 84, 40, 255)
    paper = [(250, 246, 236, 255), (120, 170, 214, 255), (236, 120, 110, 255)]
    cy = 6 if up else 1
    d.rectangle([1, cy, 5, cy + 1], fill=coin); d.point((3, cy), fill=hole)
    for i, x in enumerate((1, 3, 5)):
        if up:
            d.rectangle([x, 1 + (i % 2), x, 5], fill=paper[i])
        else:
            d.rectangle([x, 3, x, 7 - (i % 2)], fill=paper[i])
    return outlined(im)


def star(on):
    im, d = canvas(11, 11)
    c = (246, 196, 72, 255) if on else (214, 204, 190, 255)
    hi = (255, 236, 150, 255) if on else (232, 224, 212, 255)
    d.polygon([(5, 1), (6, 4), (9, 4), (7, 6), (8, 9), (5, 7), (2, 9), (3, 6), (1, 4), (4, 4)], fill=c)
    d.point((5, 3), fill=hi); d.point((4, 5), fill=hi)
    return outlined(im)


def note(kind):
    """박자 표시: hit = 떡메(치기), hand = 손(치면 안 됨)"""
    im, d = canvas(13, 13)
    if kind == "hit":
        d.ellipse([1, 1, 11, 11], fill=(112, 176, 108, 255))
        d.rectangle([4, 4, 8, 7], fill=(250, 246, 236, 255)); d.rectangle([6, 8, 6, 9], fill=(250, 246, 236, 255))
    else:
        d.ellipse([1, 1, 11, 11], fill=(214, 84, 72, 255))
        d.rectangle([4, 5, 8, 9], fill=(250, 232, 214, 255))
        for x in (4, 6, 8):
            d.rectangle([x, 3, x, 5], fill=(250, 232, 214, 255))
    return outlined(im)


def reticle():
    im, d = canvas(13, 7)
    c = (214, 84, 72, 255)
    d.polygon([(6, 1), (10, 5), (2, 5)], fill=c)
    d.point((6, 3), fill=(255, 220, 200, 255))
    return outlined(im)


META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 0
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 16
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: {sprite}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def save(name, im):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    im.save(path)
    meta = path + ".meta"
    if not os.path.exists(meta):                 # guid는 한 번 정하면 그대로 둔다
        with open(meta, "w", encoding="utf-8", newline="\n") as f:
            f.write(META.format(guid=uuid.uuid4().hex, sprite=uuid.uuid4().hex))
    else:                                        # 꽉 찬 네모 그물로 (Tight면 Unity가 투명한 가장자리를 잘라 내 창에서 그림이 늘어난다)
        with open(meta, encoding="utf-8") as f:
            text = f.read()
        if "spriteMeshType: 1" in text:
            with open(meta, "w", encoding="utf-8", newline="\n") as f:
                f.write(text.replace("spriteMeshType: 1", "spriteMeshType: 0"))



# ---------------- 나룻배 노 젓기 (2026-11-28) ----------------
WATER = 30                                       # 물이 시작하는 줄 (배경 176x66 기준)
NPC = os.path.join(ROOT, "Art", "Characters", "NPC")


def bg_ferry():
    """하늘·먼 산·강물. 건너편 강둑은 따로 흘러가게 ferry_bank로 굽는다"""
    im = sky((176, 214, 228), (236, 238, 218))
    cloud(im, 24, 5, 22); cloud(im, 108, 9, 18); cloud(im, 148, 4, 18)
    d = ImageDraw.Draw(im)
    rnd = random.Random(21)
    y, pts = 20, []
    for x in range(0, W + 8, 8):
        y = max(12, min(24, y + rnd.randint(-4, 4)))
        pts.append((x, y))
    d.polygon([(0, WATER)] + pts + [(W, WATER)], fill=(158, 192, 184, 255))
    bands = [(104, 168, 184), (96, 160, 178), (88, 152, 172), (80, 144, 166)]
    step = (H - WATER) // len(bands) + 1
    for i, c in enumerate(bands):
        d.rectangle([0, WATER + i * step, W, H], fill=c + (255,))
    for _ in range(46):                           # 가만히 있는 잔물결 (흐르는 물결은 놀이에서 그린다)
        x, yy = rnd.randrange(W), rnd.randrange(WATER + 3, H)
        d.rectangle([x, yy, x + rnd.choice([2, 3, 4]), yy], fill=(124, 184, 196, 255) if rnd.random() < .6 else (72, 134, 158, 255))
    return im


def ferry_bank():
    """건너편 강둑 한 바퀴 (352x38, 양 끝이 이어진다). 놀이에서 옆으로 흘려 배가 나아가는 것처럼 보이게 한다"""
    w, h, land = 352, 38, 33
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rnd = random.Random(8)
    def place(sp, x, bottom, fade):
        sp = sp.copy(); px = sp.load()
        for yy in range(sp.height):
            for xx in range(sp.width):
                r, g, b, a = px[xx, yy]
                if a:
                    px[xx, yy] = (round(r + (214 - r) * fade), round(g + (226 - g) * fade), round(b + (214 - b) * fade), a)
        top = bottom - sp.height
        crop = sp.crop((0, max(0, -top), sp.width, sp.height))
        for ox in (x, x - w, x + w):              # 이음매에 걸친 그림은 반대쪽 끝에도 찍는다
            layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
            layer.paste(crop, (ox, max(0, top)))
            im.alpha_composite(layer)
    half = lambda sp: sp.resize((sp.width // 2, sp.height // 2), Image.NEAREST)   # 멀리 있어 절반 크기
    trees = [baked("c6_goryeo_woodland%d" % i) for i in (5, 2, 4, 7, 1)]
    xs = [6, 52, 84, 150, 196, 236, 300]
    for i, x in enumerate(xs):
        place(half(trees[i % len(trees)]), x, land + 1, .30)
    place(half(baked("c6_goryeo_cottage")), 112, land + 1, .26)
    place(half(baked("c6_goryeo_pavilion")), 262, land + 1, .26)
    d.rectangle([0, land, w, h - 1], fill=(132, 168, 120, 255))                    # 강둑 풀밭
    d.rectangle([0, land, w, land], fill=(108, 148, 100, 255))
    d.rectangle([0, h - 2, w, h - 1], fill=(170, 160, 122, 255))                   # 물가 흙
    for _ in range(60):                           # 갈대
        x = rnd.randrange(w); t = rnd.randrange(2, 5)
        d.rectangle([x, h - 2 - t, x, h - 2], fill=(150, 150, 96, 255) if rnd.random() < .6 else (186, 176, 120, 255))
    return im


def ferry_boat(front):
    """나룻배(마을 그림 boat). front면 앞쪽 뱃전과 옆구리만 남겨 사람 발 위에 덧그린다"""
    im = art(LIFE, "boat")
    if front:
        px = im.load()
        for y in range(17):
            for x in range(im.width):
                px[x, y] = (0, 0, 0, 0)
    return im


def oar(deg):
    """긴 노. 그림 한가운데가 손(돌리는 축), deg는 수평에서 아래로 내려간 각도 (0 = 오른쪽, 90 = 곧장 아래)"""
    import math
    size, c, ln = 64, 32, 19
    big = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    ux, uy = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    vx, vy = -uy, ux
    d.line([c - ux * 5, c - uy * 5, c + ux * ln, c + uy * ln], fill=(150, 108, 66, 255), width=2)
    ex, ey = c + ux * (ln - 6), c + uy * (ln - 6)
    blade = [(ex + vx * -1, ey + vy * -1), (ex + ux * 7 + vx * -2, ey + uy * 7 + vy * -2), (ex + ux * 7 + vx * 2, ey + uy * 7 + vy * 2), (ex + vx * 1, ey + vy * 1)]
    d.polygon(blade, fill=(196, 152, 100, 255))
    d.line([ex, ey, ex + ux * 7, ey + uy * 7], fill=(222, 184, 132, 255), width=1)
    return outlined(big)


def ferry_all():
    save("bg_ferry", bg_ferry()); save("ferry_bank", ferry_bank())
    save("ferry_boat", ferry_boat(False)); save("ferry_boat_front", ferry_boat(True))
    for i, deg in enumerate((18, 52, 90, 128)):   # 들어 올림 → 물에 꽂음 → 당김 → 뒤로 뺌
        save(f"ferry_oar_{i}", oar(deg))
    for i in range(2):
        save(f"ferry_boatman_{i}", art(os.path.join(NPC, "npc_naru_boatman"), f"npc_naru_boatman_tall_r{i * 2}"))
    save("ferry_lotus", Image.open(os.path.join(BAKED, "lotus_2_1.png")).convert("RGBA"))


def main():
    save("bg_tuho", bg_tuho()); save("bg_tteok", bg_tteok()); save("bg_jegi", bg_jegi())
    save("pot", pot())
    save("arrow_flat", arrow(1, 0, 16, 5)); save("arrow_up", arrow(1, -1, 14, 11)); save("arrow_down", arrow(1, 1, 14, 11))
    save("arrow_stuck", arrow_stuck())
    for i in range(10):                                   # -60도(위로)부터 15도씩 75도(아래로)까지
        save(f"arrow_a{i}", arrow_at(-60 + i * 15))
    for i, deg in enumerate((128, 82, 40, 12)):          # 치켜든 자세 → 내려친 자세
        save(f"mallet_{i}", mallet(deg))
    save("gauge", gauge()); save("map_tteok", map_tteok())
    # 제기 차는 아이: 이미 있는 나루 아이 그림(정면)을 그대로 가져온다
    save("child", art(os.path.join(ROOT, "Art", "Characters", "NPC", "npc_naru_fishing_child"), "npc_naru_fishing_child_tall_d0"))
    save("mallet_head", mallet_head()); save("board", board())
    for i in range(3):
        save(f"tteok_{i}", tteok(i))
    save("jegi_up", jegi(True)); save("jegi_down", jegi(False))
    save("star_on", star(True)); save("star_off", star(False))
    save("note_hit", note("hit")); save("note_hand", note("hand")); save("reticle", reticle())
    ferry_all()
    print("wrote", len(os.listdir(OUT)) // 2, "sprites to", OUT)


if __name__ == "__main__":
    main()
