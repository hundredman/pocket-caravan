"""11/19 은샘 "지역 그림에 맞춰야지": 내 좌판을 지역마다 그 지역 장터 좌판 그림을 바탕으로 만든다.
진열칸의 물건만 비우고(게임에서 실제 올린 짐 그림을 얹는다, StallSpot) 작은 엽전 패로 내 좌판임을 표시.
- 고려·중앙 동쪽(transit): stall_0 (player_stall_v2.py, 고려 곡식 좌판)
- 베네치아·중앙 서쪽: stall_venice  ← 리알토 장터 천막 좌판 (NinjaAdventure/Tiles vgpt_*_115·116, 노란 칸)
- 아라비아: stall_arabia ← 아라비아 줄무늬 차양 좌판 (Art/Arabia/Sprites/sheet/stall_cloth.png)
- 북유럽·중앙 북쪽: stall_north ← 비르카 장터에 서 있는 빨강·흰 차양 좌판 (Resources/North/Minigame/stall_fish.png)
python Tools/ArtGen/player_stall_regions.py  → Assets/PocketCaravan/Resources/Stall/stall_*.png (+ 미리보기)
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
A = ROOT / 'Assets/PocketCaravan'
OUT = A / 'Resources/Stall'

def hexc(h): return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)
CLEAR = (0, 0, 0, 0)

def coin_plaque(px, x0, y0, wood, ink, gold, goldhi):
    """5×4 작은 나무 패 + 엽전 (고려 좌판과 같은 표시)"""
    for y in range(y0, y0 + 4):
        for x in range(x0, x0 + 7):
            px[x, y] = ink if y in (y0, y0 + 3) or x in (x0, x0 + 6) else wood
    for (x, y) in ((x0 + 2, y0 + 1), (x0 + 3, y0 + 1), (x0 + 4, y0 + 1), (x0 + 2, y0 + 2), (x0 + 3, y0 + 2), (x0 + 4, y0 + 2)):
        px[x, y] = gold
    px[x0 + 2, y0 + 1] = goldhi; px[x0 + 3, y0 + 1] = ink

# ---------------------------------------------------------------- 베네치아
def venice():
    T = A / 'Art/NinjaAdventure/Tiles'
    src = Image.new('RGBA', (96, 32), CLEAR)
    for x in range(6):
        for yi, y in enumerate((115, 116)):
            src.paste(Image.open(T / f'vgpt_{x}_{y}.png').convert('RGBA'), (x * 16, yi * 16))
    s = src.load()
    # 노란 칸: 왼쪽 기둥 x 29..33, 차양 몸 x 34..58, 오른쪽 기둥 x 59..63 → 몸을 늘려 너비 46
    right = list(range(59, 64)); left = right[::-1]; body = list(range(34, 59))   # 왼쪽 기둥은 오른쪽 기둥을 뒤집어 (옆 칸 주황이 묻지 않게)
    want = 46 - len(left) - len(right)
    mid = body[len(body) // 2 - 4: len(body) // 2 + 4]   # 무늬 없는 가운데 8줄을 되풀이
    cols = left + body[:9] + []
    while len(cols) + len(body[-9:]) + len(right) < 46: cols += mid[:min(len(mid), 46 - len(cols) - len(body[-9:]) - len(right))]
    cols += body[-9:] + right
    img = Image.new('RGBA', (48, 32), CLEAR); p = img.load()
    for nx, sx in enumerate(cols):
        for y in range(32): p[nx + 1, y] = s[sx, y]
    # 왼쪽 기둥은 뒤집은 것이라 그대로 쓰면 그림자가 반대 → 괜찮음 (기둥은 좌우가 거의 같다)
    INK, BACK, BACK2, TOP, WOOD, WOODLO = hexc('1d1310'), hexc('3b2418'), hexc('4d3020'), hexc('c88a3e'), hexc('8a5530'), hexc('6b3f24')
    # 진열칸(물건) 자리를 비운다: 기둥 안쪽 x 6..41, y 16..22 → 어두운 천막 안쪽, y 23 판 윗면, 24..27 판 앞
    for y in range(16, 28):
        for x in range(6, 42):
            if p[x, y][3] == 0: continue
            if y <= 21: p[x, y] = BACK if (x + y) % 9 else BACK2
            elif y == 22: p[x, y] = INK
            elif y == 23: p[x, y] = TOP
            else: p[x, y] = WOODLO if (x - 6) % 9 == 8 or y == 27 else WOOD
    coin_plaque(p, 21, 11, hexc('c88a3e'), INK, hexc('f0c365'), hexc('fff0a8'))
    img.save(OUT / 'stall_venice.png')
    return img

# ---------------------------------------------------------------- 아라비아
def arabia():
    src = Image.open(A / 'Art/Arabia/Sprites/sheet/stall_cloth.png').convert('RGBA')
    img = Image.new('RGBA', (40, 38), CLEAR); img.paste(src, (0, 0)); p = img.load()
    shade = p[20, 13]   # 차양 아래 그늘색
    shade2 = p[6, 14]
    for y in range(16, 25):
        for x in range(4, 36):
            p[x, y] = shade if (y + x) % 11 else shade2
    INK = hexc('2b1a12')
    coin_plaque(p, 17, 11, hexc('c99a52'), INK, hexc('f0c365'), hexc('fff0a8'))
    img.save(OUT / 'stall_arabia.png')
    return img

# ---------------------------------------------------------------- 북유럽
def north():
    src = Image.open(A / 'Resources/North/Minigame/stall_fish.png').convert('RGBA')
    img = src.copy(); p = img.load(); w, h = img.size
    # 11/19 비르카·북유럽 장터에 실제로 이 빨강·흰 차양 좌판이 서 있다 → 색은 그대로 두고 진열칸만 비운다
    INKN = (46, 34, 40, 255)
    # 진열칸의 생선 지우기 (y 26..32, 기둥 안쪽 x 6..41) · 판 윗면(y 33)은 테두리색으로
    for y in range(26, 33):
        for x in range(6, 42): p[x, y] = CLEAR
    for x in range(6, 42): p[x, 33] = INKN
    coin_plaque(p, 21, 17, hexc('a8763f'), hexc('2b1a12'), hexc('f0c365'), hexc('fff0a8'))
    img.save(OUT / 'stall_north.png')
    return img

if __name__ == '__main__':
    ims = [Image.open(OUT / 'stall_0.png').convert('RGBA'), venice(), arabia(), north()]
    prev = Image.new('RGBA', (60 * len(ims), 52), (120, 110, 100, 255))
    for i, im in enumerate(ims): prev.paste(im, (i * 60 + (60 - im.width) // 2, 52 - im.height), im)
    prev.resize((prev.width * 6, prev.height * 6), Image.NEAREST).save(ROOT / 'Tools/ArtGen/player_stall_regions_preview.png')
    print('ok')
