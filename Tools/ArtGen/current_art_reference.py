"""현재 게임 아트 기준표: 실제 게임 화면(4배 캡처)을 원래 도트 크기로 되돌려 모으고, 많이 쓰인 색을 뽑는다."""
import sys
from collections import Counter
from PIL import Image, ImageDraw, ImageFont

ROOT = sys.argv[1]  # 저장소 루트
OUT = sys.argv[2] if len(sys.argv) > 2 else 'current_reference.png'
FONT = '/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc'
F = lambda s: ImageFont.truetype(FONT, s, index=1)

naru = Image.open(ROOT + '/Docs/Images/BroadGoryeo/Naru.png').convert('RGB')
cap = Image.open(ROOT + '/Docs/Images/GoryeoArt/capital-play.png').convert('RGB')

def native(im, box):
    x0, y0, x1, y1 = [v // 4 * 4 for v in box]
    c = im.crop((x0, y0, x1, y1)); return c.resize((c.width // 4, c.height // 4), Image.NEAREST)

crops = [('초가집', native(naru, (560, 128, 800, 312))), ('기와집', native(naru, (1056, 128, 1300, 312))),
         ('버드나무', native(naru, (236, 272, 460, 440))), ('수레', native(naru, (1184, 324, 1300, 436))),
         ('벚나무', native(cap, (280, 428, 440, 576))), ('궁궐', native(cap, (572, 0, 1240, 420)))]
names = ['tanuki_tall_d0', 'tanuki_tall_r0', 'tanuki_tall_u0', 'villager_fox_tall_d0', 'villager_rabbit_tall_d0',
         'merchant_tall_d0', 'elder_tall_d0', 'gaekju_tall_d0']
chars = [Image.open(f'{ROOT}/Assets/PocketCaravan/Art/Characters/{n}.png').convert('RGBA') for n in names]

Z, W = 3, 1500
out = Image.new('RGB', (W, 1600), (255, 253, 246)); d = ImageDraw.Draw(out)
d.text((24, 16), '현재 게임 아트 기준표 — 실제 게임 화면의 원래 도트 크기 × 3', font=F(26), fill=(42, 32, 36))
x, y, rowh = 24, 70, 0
for n, im in crops:
    z = im.resize((im.width * Z, im.height * Z), Image.NEAREST)
    if x + z.width > W - 24: x, y, rowh = 24, y + rowh + 30, 0
    d.text((x, y), f'{n} {im.width}×{im.height}', font=F(18), fill=(107, 58, 32))
    out.paste(z, (x, y + 28)); x += max(z.width, 150) + 28; rowh = max(rowh, z.height + 28)
y += rowh + 40
d.text((24, y), '캐릭터 16×24 × 4 — 플레이어 너구리(정면·옆·뒤), 주민 여우·토끼, 상인, 원로, 객주', font=F(18), fill=(107, 58, 32)); y += 30; x = 24
for im in chars:
    z = im.resize((im.width * 4, im.height * 4), Image.NEAREST)
    bg = Image.new('RGB', z.size, (208, 200, 180)); bg.paste(z, (0, 0), z); out.paste(bg, (x, y)); x += z.width + 20
y += 24 * 4 + 40
cnt = Counter()
for _, im in crops: cnt.update(list(im.getdata()))
for im in chars: cnt.update([p[:3] for p in im.getdata() if p[3] > 0])
pal = []
for c, _ in cnt.most_common():
    if all(sum((a - b) ** 2 for a, b in zip(c, p)) > 18 ** 2 for p in pal): pal.append(c)
    if len(pal) >= 48: break
d.text((24, y), '현재 아트에서 많이 쓰인 색 48개 — 공통 팔레트의 출발점', font=F(18), fill=(107, 58, 32)); y += 30
for i, c in enumerate(pal):
    cx, cy = 24 + (i % 16) * 88, y + (i // 16) * 56
    d.rectangle([cx, cy, cx + 40, cy + 40], fill=c, outline=(42, 32, 36)); d.text((cx, cy + 42), '#%02x%02x%02x' % c, font=F(11), fill=(60, 50, 40))
y += 3 * 56 + 20
out.crop((0, 0, W, y)).save(OUT)
open(OUT.rsplit('.', 1)[0] + '_palette.txt', 'w').write('\n'.join('#%02x%02x%02x' % c for c in pal) + '\n')
print('ok')
