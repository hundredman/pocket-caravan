"""Arrange genuine Unity captures, without altering game pixels."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A7-20261105'
FONT='/System/Library/Fonts/AppleSDGothicNeo.ttc'
def font(size):return ImageFont.truetype(FONT,size)
def board():
 ids=[('Naru','예성 나루 · 닭/연기/석등'),('Dakmok','닥목 · 마당 개/연기/석등'),('goryeo_jangdan','장단 · 닭/마당/석등'),('transit_woodtown','목재 장터 · 사람/짐수레/개')]
 b=Image.new('RGB',(1464,1280),'#eee9dc');d=ImageDraw.Draw(b);d.text((12,9),'A7 생활 연출 · 실제 Unity 480×270 · 검토 전용/본편 미적용',font=font(23),fill='#2f291f')
 for j,title in enumerate(('기존','수정안 · 아침','수정안 · 밤')):d.text((12+j*488,46),title,font=font(20),fill='#2f291f')
 for i,(id,label)in enumerate(ids):
  y=78+i*300;d.text((12,y),label,font=font(18),fill='#2f291f')
  for j,state in enumerate(('before','after','night')):
   with Image.open(OUT/'Unity'/f'{id}-{state}.png')as im:
    assert im.size==(480,270);b.paste(im.convert('RGB'),(j*488,y+25))
 b.save(OUT/'game-comparison-final.png')
 b=Image.new('RGB',(976,650),'#eee9dc');d=ImageDraw.Draw(b);d.text((10,8),'물가·논 가까이 보기 · 실제 Unity 480×270 · 기존 / 수정안',font=font(23),fill='#2f291f')
 for i,(id,label)in enumerate([('Naru','나루 · 승인된 배 전체/갈매기'),('goryeo_jangdan','장단 · 기존 논 위 새')]):
  y=46+i*300;d.text((12,y),label,font=font(18),fill='#2f291f')
  for j,state in enumerate(('before','after')):
   with Image.open(OUT/'Unity'/f'{id}-detail-{state}.png')as im:assert im.size==(480,270);b.paste(im.convert('RGB'),(j*488,y+25))
 b.save(OUT/'detail-comparison-final.png')
if __name__=='__main__':board()
