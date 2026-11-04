"""Lay out unmodified Unity captures for fair-day art approval."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/CentralFair-Task4-20261104'
font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',25)
for key,title in [('transit_woodtown','솔재 길목 · 장터'),('transit_woodtown-event-yard','솔재 길목 · 큰 장 마당'),('transit_ferry','버들나루 · 장터와 마당')]:
 board=Image.new('RGB',(1920,580),'#263035');d=ImageDraw.Draw(board)
 for i,(suffix,label)in enumerate([('normal','평상시'),('fair','큰 장 날 · 좌판 시안')]):
  with Image.open(OUT/'Unity'/f'{key}-{suffix}.png')as im:
   assert im.size==(960,540);board.paste(im.convert('RGB'),(960*i,40))
  d.text((960*i+12,5),f'{title} / {label}',fill='white',font=font)
 board.save(OUT/f'{key}-comparison.png')
