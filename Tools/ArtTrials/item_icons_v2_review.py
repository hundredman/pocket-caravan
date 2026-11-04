"""Arrange unchanged native icon exports and actual inventory screenshots for approval."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import hashlib,json
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/ItemIcons-v2-20261104'
def main():
 font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',25);small=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
 rows=[('grain','곡식 → 볏단',ROOT/'Assets/PocketCaravan/Art/Items/grain.png'),('transit_spice','향신료 · 둥근 밑부분',ROOT/'ArtReview/E3-E2-Art-20261103/Native/transit_spice.png'),('transit_wool','양모 · 부드러운 덩어리',ROOT/'ArtReview/E3-E2-Art-20261103/Native/transit_wool.png')]
 board=Image.new('RGB',(960,600),'#344537');draw=ImageDraw.Draw(board);checks=[]
 for i,(n,label,old)in enumerate(rows):
  x=i*320;draw.text((x+14,12),label,font=font,fill='white')
  for y,p,title in [(90,old,'기존 · 10배'),(350,OUT/'Native'/f'{n}.png','수정안 · 10배')]:
   draw.text((x+16,y-36),title,font=small,fill='white')
   with Image.open(p)as im:
    im=im.convert('RGBA');assert im.size==(16,16);large=im.resize((160,160),Image.Resampling.NEAREST);board.paste(large,(x+76,y),large);board.paste(im,(x+148,y+175),im)
  with Image.open(OUT/'Native'/f'{n}.png')as im:
   im=im.convert('RGBA');assert set(im.getchannel('A').getdata())<={0,255};checks.append(dict(id=n,size=[16,16],rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest(),status='pending user approval'))
 board.save(OUT/'native-comparison.png')
 if '--native-only' in __import__('sys').argv:return
 board=Image.new('RGB',(1920,580),'#263035');draw=ImageDraw.Draw(board)
 for i,(n,label)in enumerate([('before','실제 소지품 창 · 기존'),('after','실제 소지품 창 · 수정안(검토용 아이템)')]):
  with Image.open(OUT/'Unity'/f'inventory-{n}.png')as im:
   assert im.size==(960,540);board.paste(im.convert('RGB'),(i*960,40))
  draw.text((i*960+12,5),label,font=font,fill='white')
 board.save(OUT/'inventory-comparison.png');(OUT/'checks.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
