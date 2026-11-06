"""Collate production game captures; source art is never edited."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/Goryeo-Seats-Binding-20261106'


def build():
    page=Image.new('RGB',(984,648),'#eee9df');draw=ImageDraw.Draw(page)
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',19)
    draw.text((12,8),'북쪽 쉼터 · 승인된 앉기 본편 연결 (실제 게임 480×270)',font=font,fill='#302e2a')
    for i,(key,label)in enumerate([
        ('production-north_platform','평상'),('production-north_porch','주막 마루'),
        ('production-goryeo','모닥불 · 너구리'),('production-arabia','모닥불 · 사막여우'),
    ]):
        x=8+(i%2)*488;y=42+(i//2)*302
        draw.text((x,y),label,font=font,fill='#302e2a')
        with Image.open(OUT/f'{key}.png')as im:page.paste(im,(x,y+26))
    page.save(OUT/'production-contact-sheet.png')


if __name__=='__main__':build()
