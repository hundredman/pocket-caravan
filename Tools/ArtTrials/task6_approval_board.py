"""Arrange unchanged pending assets and explicitly dated historic Unity captures."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];SOURCE=ROOT/'ArtReview/E3-E2-Art-20261103';OUT=ROOT/'ArtReview/Task6-Approval-20261104'
def main():
 OUT.mkdir(exist_ok=True);board=Image.new('RGB',(960,650),'#344537');d=ImageDraw.Draw(board);font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',23)
 for x,name,label,scale in [(0,'fallen_log','길막음 통나무 · 48×48',4),(320,'transit_spice','향신료 아이콘 · 16×16',8),(640,'transit_wool','양모 아이콘 · 16×16',8)]:
  d.text((x+12,10),label,fill='white',font=font)
  with Image.open(SOURCE/'Native'/f'{name}.png')as im:
   im=im.convert('RGBA');large=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST);board.paste(large,(x+45,60),large);board.paste(im,(x+45,260),im)
 for x,name,label in [(0,'log','11/03 Unity · 통나무 크기 검토'),(480,'item-ui','11/03 Unity · 아이콘 비교(검토 UI)')]:
  d.text((x+10,330),label,fill='white',font=font)
  with Image.open(SOURCE/'Unity'/f'{name}.png')as im:
   assert im.size==(480,270);board.paste(im.convert('RGB'),(x,370))
 board.save(OUT/'assets-review.png')
if __name__=='__main__':main()
