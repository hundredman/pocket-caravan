"""Compare original, rejected v2 and current native exports; never paint asset pixels."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/ItemIcons-v3-20261104';V2=ROOT/'ArtReview/ItemIcons-v2-20261104';OLD=ROOT/'ArtReview/E3-E2-Art-20261103/Native'
def main():
 font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',24);small=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
 board=Image.new('RGB',(960,900),'#344537');draw=ImageDraw.Draw(board)
 specs=[('grain','볏단 · 줄기 두께 보강',ROOT/'Assets/PocketCaravan/Art/Items/grain.png'),('transit_spice','향신료 · 기존 그림 유지',OLD/'transit_spice.png'),('transit_wool','양모 · 윗중앙 홈만 보정',OLD/'transit_wool.png')]
 checks=[]
 for i,(n,label,original)in enumerate(specs):
  x=i*320;draw.text((x+12,10),label,font=font,fill='white')
  for y,file,title in [(90,original,'기존 스타일'),(370,V2/'Native'/f'{n}.png','직전 시안 v2'),(650,OUT/'Native'/f'{n}.png','이번 수정 v3')]:
   draw.text((x+16,y-36),title,font=small,fill='white')
   with Image.open(file)as im:
    im=im.convert('RGBA');assert im.size==(16,16);large=im.resize((160,160),Image.Resampling.NEAREST);board.paste(large,(x+76,y),large);board.paste(im,(x+148,y+175),im)
  with Image.open(OUT/'Native'/f'{n}.png')as im:
   im=im.convert('RGBA');assert set(im.getchannel('A').getdata())<={0,255};checks.append(dict(id=n,size=[16,16],rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest()))
 with Image.open(OUT/'Native/transit_spice.png')as a,Image.open(OLD/'transit_spice.png')as b:assert a.convert('RGBA').tobytes()==b.convert('RGBA').tobytes()
 with Image.open(OUT/'Native/transit_wool.png')as a,Image.open(OLD/'transit_wool.png')as b:
  a=a.convert('RGBA');b=b.convert('RGBA');changed=[(x,y)for y in range(16)for x in range(16)if a.getpixel((x,y))!=b.getpixel((x,y))];assert set(changed)=={(7,3),(8,3),(8,4)};checks.append(dict(woolChangedPixels=changed,otherWoolPixelsUnchanged=True))
 with Image.open(OUT/'Native/grain.png')as a,Image.open(V2/'Native/grain.png')as b:
  a=a.convert('RGBA');b=b.convert('RGBA');count=lambda im:sum(im.getpixel((x,y))[3]>0 for y in range(10,15)for x in range(16))
  now,old=count(a),count(b);assert now>old,(now,old);checks.append(dict(grainLowerBodyOpaquePixelsBefore=old,grainLowerBodyOpaquePixelsAfter=now))
 checks.append(dict(spiceEqualsOriginalRGBA=True));(OUT/'checks.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n');board.save(OUT/'native-comparison.png')
 if '--native-only' in __import__('sys').argv:return
 board=Image.new('RGB',(1920,580),'#263035');draw=ImageDraw.Draw(board)
 for i,(n,label)in enumerate([('before','실제 소지품 · 직전 시안 v2'),('after','실제 소지품 · 이번 수정 v3(검토용)')]):
  with Image.open(OUT/'Unity'/f'inventory-{n}.png')as im:
   assert im.size==(960,540);board.paste(im.convert('RGB'),(i*960,40))
  draw.text((i*960+12,5),label,font=font,fill='white')
 board.save(OUT/'inventory-comparison.png')
if __name__=='__main__':main()
