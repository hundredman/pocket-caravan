"""북유럽 N3 미리보기: N2 구역도 자리에 실제 그림(에셋 팩 STAMPS, Minigame 그림, N3 새 그림)을 얹어 맵마다 그린다. 검토용.
바닥은 N1 지형 색(타일은 N4에서), 숲 칸에는 눈 덮인 전나무를 듬성듬성 세운다.
python3 Tools/MapGen/northworld/n3_preview.py  (저장소 루트 아무 데서나)"""
import os, re, json, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont
R = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
G = R + '/Tools/MapGen'; TL = R + '/Assets/PocketCaravan/Art/NinjaAdventure/Tiles'; MG = R + '/Assets/PocketCaravan/Resources/North/Minigame'
NEW = R + '/ArtReview/North-N3/art'; OUT = R + '/ArtReview/North-N3'
FONT = R + '/Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf'
src = open(G + '/north_tiles.py').read() + open(G + '/tiles.py').read()
ST = {}
for m in re.finditer(r'"(\w+)":\s*\("(\w+)",\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)', src): ST.setdefault(m.group(1), (m.group(2), *map(int, m.groups()[2:])))
ART_SHEET = Image.open(G + '/north_art.png').convert('RGBA')
_cache = {}
def art(name):
    if name in _cache: return _cache[name]
    if name.startswith('new:'): im = Image.open(f'{NEW}/{name[4:]}.png').convert('RGBA')
    elif name.startswith('Minigame:'): im = Image.open(f'{MG}/{name[9:]}.png').convert('RGBA')
    elif name == 'laundry': im = art('tub')
    else:
        sh, x, y, w, h = ST[name]; im = Image.new('RGBA', (w * 16, h * 16))
        for j in range(h):
            for i in range(w):
                if sh == 'northart': t = ART_SHEET.crop(((x + i) * 16, (y + j) * 16, (x + i) * 16 + 16, (y + j) * 16 + 16))
                else:
                    p = f'{TL}/{sh}_{x + i}_{y + j}.png'
                    if not os.path.exists(p): continue
                    t = Image.open(p).convert('RGBA')
                im.alpha_composite(t, (i * 16, j * 16))
    _cache[name] = im; return im
VOID, DEEP, SHALLOW, GRAVEL, ROCKCOAST, RIVER, BOG, FIELD, GRASS, TUNDRA, HILL, FOREST, SNOW, MOUNTAIN, PEAK, LAKE, SPRING = range(17)
COL = {DEEP: (52, 98, 140), SHALLOW: (96, 156, 186), GRAVEL: (196, 190, 172), ROCKCOAST: (120, 118, 112), RIVER: (84, 146, 196),
       BOG: (104, 120, 92), FIELD: (196, 176, 96), GRASS: (122, 160, 96), TUNDRA: (176, 182, 150), HILL: (140, 152, 108),
       FOREST: (88, 120, 84), SNOW: (232, 238, 242), MOUNTAIN: (118, 110, 106), PEAK: (214, 230, 244), LAKE: (70, 136, 186),
       SPRING: (110, 200, 196), VOID: (28, 34, 40)}
g = np.load(R + '/ArtReview/North-N1/n1_grid.npz'); X0, Y0 = int(g['X0']), int(g['Y0'])
T_, MID, ROAD, VIL = g['t'], g['mapid'], g['road'], g['village']
Z = json.load(open(R + '/ArtReview/North-N2/zones.json'))
KEYS = list(Z)
KEYS_N1 = ['north_birka', 'north_herd', 'north_hunter', 'north_naru', 'north_laugar', 'north_icecave', 'north_runehill',
           'north_c_gatetown', 'north_c_snowwood', 'north_c_creek', 'north_c_crossing',
           'north_c_road_nw', 'north_c_road_ne', 'north_c_runeway', 'north_c_road_sw', 'north_c_road_se']
P = 16
def render(key):
    i = KEYS_N1.index(key); m = MID == i
    ys, xs = np.nonzero(m); y0, y1, x0, x1 = ys.min() - 1, ys.max() + 2, xs.min() - 1, xs.max() + 2
    sub = T_[y0:y1, x0:x1]; rgb = np.zeros(sub.shape + (3,), np.uint8)
    for k, c in COL.items(): rgb[sub == k] = c
    rgb[ROAD[y0:y1, x0:x1] & ~np.isin(sub, [RIVER, LAKE, DEEP, SHALLOW])] = (186, 150, 100)
    rgb[ROAD[y0:y1, x0:x1] & (sub == RIVER)] = (150, 110, 70)
    rng = np.random.default_rng(i); rgb = np.clip(rgb.astype(int) + rng.integers(-6, 7, rgb.shape[:2])[..., None], 0, 255).astype(np.uint8)
    im = Image.fromarray(rgb).resize(((x1 - x0) * P, (y1 - y0) * P), Image.NEAREST).convert('RGBA')
    d = ImageDraw.Draw(im, 'RGBA')
    out = ~m[y0:y1, x0:x1]
    for yy, xx in np.argwhere(out): d.rectangle((xx * P, yy * P, xx * P + P - 1, yy * P + P - 1), fill=(10, 12, 16, 150))
    items = [it for it in Z[key]['items'] if not it.get('missing')]
    taken = np.zeros_like(m)
    for it in items:
        if 'x' in it:
            iy, ix = it['y'] - Y0, it['x'] - X0; taken[max(0, iy - 1):iy + it['h'] + 1, max(0, ix - 1):ix + it['w'] + 1] = True
    # 나무: 숲 칸에 3칸 간격으로 (마을·길·자리 피함)
    sprites = []
    rr = random.Random(i)
    north = key.startswith('north_') and not key.startswith('north_c_')
    for yy in range(y0, y1 - 1, 2):
        for xx in range(x0 + (yy // 2) % 2, x1 - 1, 3):
            if T_[yy, xx] == FOREST and T_[yy + 1, xx] == FOREST and m[yy, xx] and not VIL[yy, xx] and not ROAD[yy:yy + 2, xx:xx + 2].any() and not taken[yy, xx]:
                name = rr.choice(['snowpine', 'snowpine', 'snowpine_g'] if north or rr.random() < .5 else ['snowpine_g', 'snowround'])
                sprites.append((yy + 2, xx, name, 2, 2))
    for it in items:
        a = it.get('art')
        if not a or 'x' not in it: continue
        sprites.append((it['y'] - Y0 + it['h'], it['x'] - X0, a, it['w'], it['h']))
    # 마당·광장·쉼터·부두 바닥
    for it in items:
        if 'x' not in it: continue
        bx, by = (it['x'] - X0 - x0) * P, (it['y'] - Y0 - y0) * P
        if it['kind'] in ('plaza', 'yard', 'rest'):
            d.rectangle((bx, by, bx + it['w'] * P - 1, by + it['h'] * P - 1), fill=(204, 176, 128, 255) if it['kind'] == 'plaza' else (190, 168, 120, 255))
            if it['name'] == '룬돌 원': d.ellipse((bx, by, bx + it['w'] * P - 1, by + it['h'] * P - 1), outline=(120, 120, 130, 255), width=4)
        if it['kind'] == 'harbor': d.rectangle((bx, by, bx + it['w'] * P - 1, by + it['h'] * P - 1), fill=(150, 104, 64, 255))
    for (by, bx, name, w, h) in sorted(sprites):
        sp = art(name); px = (bx - x0) * P + (w * P - sp.width) // 2; py = (by - y0) * P - sp.height
        im.alpha_composite(sp, (max(0, px), max(0, py)) if px >= 0 and py >= 0 else (max(0, px), max(0, py)))
    d = ImageDraw.Draw(im); f = ImageFont.truetype(FONT, 12)
    for it in items:
        if 'x' not in it: continue
        bx, by = (it['x'] - X0 - x0) * P, (it['y'] - Y0 - y0) * P
        if it['kind'] in ('npc',):
            d.ellipse((bx + 3, by + 3, bx + 13, by + 13), fill=(255, 120, 220), outline=(40, 20, 40))
        elif it['kind'] in ('event', 'bandit', 'gather', 'growth'):
            c = {'event': (230, 60, 60), 'bandit': (30, 20, 20), 'gather': (90, 220, 90), 'growth': (170, 240, 150)}[it['kind']]
            d.ellipse((bx + 4, by + 4, bx + 12, by + 12), fill=c, outline=(255, 255, 255))
        elif it['kind'] == 'spot' and not it.get('art'):
            d.rectangle((bx + 2, by + 2, bx + 13, by + 13), fill=(240, 220, 160), outline=(60, 40, 20))
    for it in items:
        if 'x' not in it or it['kind'] in ('npc', 'growth', 'lantern') or it['name'].startswith(('집 ', '횃불', '룬석', '이벤트', '옮긴 이벤트')): continue
        bx, by = (it['x'] - X0 - x0) * P, (it['y'] - Y0 - y0) * P
        d.text((bx, by - 14), it['name'], font=f, fill='white', stroke_width=2, stroke_fill='black')
    hd = Image.new('RGBA', (im.width, 26), (20, 20, 24, 255)); ImageDraw.Draw(hd).text((6, 5), f"{Z[key]['name']} ({key})", fill='white', font=ImageFont.truetype(FONT, 14))
    full = Image.new('RGBA', (im.width, im.height + 26)); full.paste(hd, (0, 0)); full.paste(im, (0, 26))
    full.save(f'{OUT}/{key}.png'); return full

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True); pics = {k: render(k) for k in KEYS_N1}
    for name, ks in (('sheet_mainland', KEYS_N1[:7]), ('sheet_center_towns', KEYS_N1[7:11]), ('sheet_travel', KEYS_N1[11:])):
        ims = [pics[k] for k in ks]; th = 900; ims = [x.resize((int(x.width * th / x.height), th), Image.LANCZOS) for x in ims]
        rows = [[]]
        for x in ims:
            if rows[-1] and sum(z.width + 10 for z in rows[-1]) + x.width > 3200: rows.append([])
            rows[-1].append(x)
        Wd = max(sum(z.width + 10 for z in r) for r in rows); sh = Image.new('RGBA', (Wd, len(rows) * (th + 10)), (255, 255, 255, 255))
        for ri, r in enumerate(rows):
            xx = 0
            for x in r: sh.alpha_composite(x, (xx, ri * (th + 10))); xx += x.width + 10
        sh.save(f'{OUT}/{name}.png')
    print('ok', len(pics))
