"""ATM-B3 native pixel drawing, extending chars.py's text/palette source.

No standing PNG crop, resize, squash, recolour or overwrite. Review only.
"""
import json
from pathlib import Path
import chars
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtReview/Goryeo-Seats-20261105'


def pose():
    rows = chars.norm(['.' * 16] * 8 + chars.HAT + chars.FACE_FRONT + [
        '...oaacllcaao...',
        '..ofabbbbbafo...',
        '...ockkkkco.....',
        '..okkkookkko....',
        '..osssooossso...',
    ])
    assert len(rows) == 24
    return chars.render(rows, chars.BASE)


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    pose().save(OUT / 'tanuki-seated-front.png')
    config = dict(status='review', originId='goryeo', sprite='tanuki-seated-front.png',
                  seats=[
                      dict(id='north_platform', mapId='transit_central_approach', name='평상',
                           x=41, y=35, lift=.4, source='M2 북쪽 쉼터 마을 평상(79,-40), 물건 없는 오른쪽 끝'),
                      dict(id='north_porch', mapId='transit_central_approach', name='주막 마루',
                           x=46, y=44, lift=.9, source='M2 쉼터 주막(85,-33), 출입구 바로 옆 후보'),
                      dict(id='north_fire', mapId='transit_central_approach', name='모닥불',
                           x=44, y=36, lift=0, fire=True, propX=43, propY=34,
                           source='신규 위치 후보. NinjaAdventure camp (12,5) 2×2 원래 모닥불 조립'),
                  ])
    (OUT / 'source.json').write_text(json.dumps(config, ensure_ascii=False, indent=2) + '\n')
    return config


def board():
    page=Image.new('RGB',(984,1100),'#eee9df')
    draw=ImageDraw.Draw(page)
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
    draw.text((12,8),'ATM-B3 앉기 검토 · 본편 미적용',font=font,fill='#302e2a')
    draw.text((12,40),'왼쪽: 서기 / 오른쪽: 앉기 · 실제 Unity 480×270',font=font,fill='#302e2a')
    for i,(key,label) in enumerate([('north_platform','기존 평상'),('north_porch','기존 주막 앞 마루'),('north_fire','모닥불 새 배치 후보')]):
        y=78+i*304
        draw.text((12,y),label,font=font,fill='#302e2a')
        for j,state in enumerate(['before','after']):
            with Image.open(OUT/f'Unity/{key}-{state}.png') as im:page.paste(im,(8+j*488,y+28))
    draw.text((12,996),'자세 상세 (4배, 최근접 확대) · 왼쪽 기존 / 오른쪽 새 자세',font=font,fill='#302e2a')
    for i,p in enumerate([ROOT/'Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png',OUT/'tanuki-seated-front.png']):
        with Image.open(p) as im:
            im=im.convert('RGBA').resize((64,96),Image.Resampling.NEAREST)
            page.paste(im,(700+i*120,994),im)
    page.save(OUT/'comparison.png')


if __name__ == '__main__':
    import sys
    if '--board' in sys.argv:board()
    else:build()
