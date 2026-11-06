"""Native visitor pose sources for Goryeo seats. Other regions' generators are read-only imports."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import chars
import venice_chars
import north_chars

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/Goryeo-Seats-20261105/Expanded'
BODY=['...oaacllcaao...','..ofabbbbbafo...','...ockkkkco.....','..okkkookkko....','..osssooossso...']


def build():
    OUT.mkdir(parents=True,exist_ok=True)
    # Draw from the original textual head/palette definitions, not slices of standing PNGs.
    face=[r.replace('m','f') for r in chars.FACE_FRONT]
    rows=chars.norm(['.'*16]*8+chars.EARS['cat_cap']['d']+face+BODY)
    pal=dict(chars.BASE);pal.update(venice_chars.VENICE_CAT)
    chars.render(rows,pal).save(OUT/'venice-seated-front.png')
    rows=[list(r) for r in chars.norm(['.'*16]*7+north_chars.RD_EARS['d']+north_chars.RD_FACE['d']+north_chars.RD_SCARF['d']+BODY[1:])]
    assert len(rows)==24
    north_chars.overlay(rows,north_chars.RD_ANTLERS['d'],2)
    pal=dict(chars.BASE);pal.update(north_chars.SPECIES['north_reindeer'])
    chars.render(rows,pal).save(OUT/'viking-seated-front.png')
    config=json.loads((OUT.parent/'source.json').read_text())
    config['poses']=[dict(originId=k,file=str(p),ppu=ppu,pivotY=pivot) for k,p,ppu,pivot in [
        ('goryeo','ArtReview/Goryeo-Seats-20261105/tanuki-seated-front.png',16,.25),
        ('venice','ArtReview/Goryeo-Seats-20261105/Expanded/venice-seated-front.png',16,.25),
        ('viking','ArtReview/Goryeo-Seats-20261105/Expanded/viking-seated-front.png',16,.25),
        ('arabia','ArtReview/Goryeo-Seats-20261105/Expanded/arabia-seated-front.png',32,20/48),
    ]]
    (OUT/'source.json').write_text(json.dumps(config,ensure_ascii=False,indent=2)+'\n')


def board():
    page=Image.new('RGB',(984,1474),'#eee9df');d=ImageDraw.Draw(page)
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',19)
    d.text((12,8),'고려 쉼터 · 출신별 앉기 검토 / 왼쪽 서기 → 오른쪽 앉기',font=font,fill='#302e2a')
    names=[('goryeo','고려 너구리'),('venice','베네치아 고양이'),('viking','북유럽 순록'),('arabia','아라비아 사막여우')]
    for i,(key,name) in enumerate(names):
        y=42+i*305;d.text((12,y),name,font=font,fill='#302e2a')
        for col,state in enumerate(['before','after']):
            with Image.open(OUT/f'Unity/{key}-{state}.png') as im:page.paste(im,(8+488*col,y+26))
    d.text((12,1260),'자세 상세 · 원래 그림 → 새 자세 (최근접 확대, 출신별 원래 PPU 유지)',font=font,fill='#302e2a')
    stems={'goryeo':'tanuki','venice':'venice_cat','viking':'north_reindeer','arabia':'arabia_player_fennec'}
    for i,(key,name) in enumerate(names):
        x=12+i*244;d.text((x,1292),name,font=font,fill='#302e2a')
        before=ROOT/f'Assets/PocketCaravan/Art/Characters/{stems[key]}_tall_d0.png'
        after=OUT.parent/'tanuki-seated-front.png' if key=='goryeo' else OUT/f'{key}-seated-front.png'
        for col,path in enumerate([before,after]):
            with Image.open(path) as im:
                k=4 if im.width==16 else 2;large=im.convert('RGBA').resize((im.width*k,im.height*k),Image.Resampling.NEAREST)
                page.paste(large,(x+16+col*102,1330),large)
    page.save(OUT/'comparison.png')


if __name__=='__main__':
    import sys
    if '--board' in sys.argv:board()
    else:build()
