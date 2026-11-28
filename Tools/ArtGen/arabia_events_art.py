"""아라비아 마을 작은 사건(ArabiaTownEvents) 그림. 이미 있는 아라비아 그림만 옮기거나 줄여서 쓴다.

사용법 (저장소 루트):  python3 Tools/ArtGen/arabia_events_art.py
- 사람: Art/Characters의 정면 그림(d0)을 그대로 복사.
- 양: Art/Arabia/Sprites/drawn의 양 두 장을 그대로 복사.
- 자루: 큰 장 그림(Resources/Arabia/Fair)의 향신료·후추 자루를 절반으로 줄여(사람 크기에 맞게) 16x16 칸 아래 가운데에 놓는다.
.meta는 같은 종류의 기존 그림 것을 본떠 새 guid로 만든다(이미 있으면 그대로 둔다).
"""
import hashlib
import os
import re
import shutil
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
A = os.path.join(ROOT, 'Assets/PocketCaravan')
OUT = os.path.join(A, 'Resources/Arabia/Events')
PERSON_META = os.path.join(A, 'Resources/Arabia/Fair/People/arabia_npc_merchant_tall_d0.png.meta')
PROP_META = os.path.join(A, 'Resources/Arabia/Fair/prop_spiceSack.png.meta')

PEOPLE = {'traveler': 'arabia_npc_bedouin_tall_d0', 'shepherd': 'arabia_woman_grandma_tall_d0', 'porter': 'arabia_npc_sailor_tall_d0'}
SHEEP = {'sheep_0': 'drawn_sheep', 'sheep_1': 'drawn_sheep2'}
SACKS = {'sack_spice': 'prop_spiceSack', 'sack_pepper': 'prop_pepperSack'}


def meta(path, template):
    if os.path.exists(path + '.meta'):
        return
    guid = hashlib.md5(('arabia_events/' + os.path.basename(path)).encode()).hexdigest()
    text = open(template, encoding='utf-8').read()
    open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(re.sub(r'^guid: \w+', 'guid: ' + guid, text, count=1, flags=re.M))


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, src in PEOPLE.items():
        dst = os.path.join(OUT, name + '.png')
        shutil.copyfile(os.path.join(A, 'Art/Characters', src + '.png'), dst)
        meta(dst, PERSON_META)
    for name, src in SHEEP.items():
        dst = os.path.join(OUT, name + '.png')
        img = Image.open(os.path.join(A, 'Art/Arabia/Sprites/drawn', src + '.png')).convert('RGBA')
        cell = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
        cell.alpha_composite(img, ((16 - img.width) // 2, 16 - img.height))
        cell.save(dst)
        meta(dst, PROP_META)
    for name, src in SACKS.items():
        dst = os.path.join(OUT, name + '.png')
        img = Image.open(os.path.join(A, 'Resources/Arabia/Fair', src + '.png')).convert('RGBA')
        small = img.resize((img.width // 2, img.height // 2), Image.NEAREST)
        cell = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
        cell.alpha_composite(small, ((16 - small.width) // 2, 16 - small.height - 2))
        cell.save(dst)
        meta(dst, PROP_META)
    print('ok', sorted(os.listdir(OUT)))


if __name__ == '__main__':
    main()
