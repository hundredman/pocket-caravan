"""B3 v2: redraw the seated body on the head's centerline; keep rejected v1 files."""
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import chars
import venice_chars
import north_chars

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'ArtReview/Goryeo-Seats-20261105'
OUT=BASE/'AlignedV2'
BODY=['...oaacllcaao...','..ofabbbbbbafo..','....ockkkkco....','...okkkookkko...','...osssoossso...']


def build():
    OUT.mkdir(parents=True,exist_ok=True)
    for origin,top,face,overrides in [
        ('goryeo',chars.HAT,chars.FACE_FRONT,{}),
        ('venice',chars.EARS['cat_cap']['d'],[r.replace('m','f')for r in chars.FACE_FRONT],venice_chars.VENICE_CAT),
    ]:
        rows=chars.norm(['.'*16]*8+top+face+BODY);pal=dict(chars.BASE);pal.update(overrides)
        chars.render(rows,pal).save(OUT/f'{origin}-seated-front.png')
    rows=[list(r)for r in chars.norm(['.'*16]*7+north_chars.RD_EARS['d']+north_chars.RD_FACE['d']+north_chars.RD_SCARF['d']+BODY[1:])]
    north_chars.overlay(rows,north_chars.RD_ANTLERS['d'],2)
    pal=dict(chars.BASE);pal.update(north_chars.SPECIES['north_reindeer'])
    chars.render(rows,pal).save(OUT/'viking-seated-front.png')
    data=json.loads((BASE/'Expanded/source.json').read_text())
    data['sprite']='goryeo-seated-front.png'
    for pose in data['poses']:pose['file']=str((OUT/f"{pose['originId']}-seated-front.png").relative_to(ROOT))
    (OUT/'source.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')


def board():
    page=Image.new('RGB',(984,1518),'#eee9df');d=ImageDraw.Draw(page)
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',19)
    names=[('goryeo','너구리'),('venice','고양이'),('viking','순록'),('arabia','사막여우')]
    detail=Image.new('RGB',(984,248),'#eee9df');dd=ImageDraw.Draw(detail)
    dd.text((12,8),'앉기 수정 · 출신마다 왼쪽 이전 / 오른쪽 수정 (최근접 확대)',font=font,fill='#302e2a')
    for i,(key,name)in enumerate(names):
        x=12+i*244;dd.text((x,43),name,font=font,fill='#302e2a')
        old=BASE/'tanuki-seated-front.png'if key=='goryeo'else BASE/f'Expanded/{key}-seated-front.png'
        for col,path in enumerate([old,OUT/f'{key}-seated-front.png']):
            with Image.open(path)as im:
                k=6 if im.width==16 else 3;large=im.convert('RGBA').resize((im.width*k,im.height*k),Image.Resampling.NEAREST)
                detail.paste(large,(x+2+118*col,79),large)
    detail.save(OUT/'pose-comparison.png');page.paste(detail,(0,0))
    d.text((12,253),'실제 게임 480×270 · 왼쪽 이전 시안 / 오른쪽 수정본',font=font,fill='#302e2a')
    for i,(key,name)in enumerate(names):
        y=285+i*305;d.text((12,y),name,font=font,fill='#302e2a')
        for col,folder in enumerate([BASE/'Expanded/Unity',OUT/'Unity']):
            with Image.open(folder/f'{key}-after.png')as im:page.paste(im,(8+488*col,y+26))
    page.save(OUT/'comparison.png')


if __name__=='__main__':
    import sys
    if '--board'in sys.argv:board()
    else:build()
