from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A5-20261105'
def board():
 font=lambda s:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',s)
 b=Image.new('RGB',(1464,1280),'#eee9dc');d=ImageDraw.Draw(b);d.text((12,8),'A5 장소 이름표 · 실제 Unity480×270 · 검토 전용/본편 미적용',font=font(23),fill='#30291f')
 for j,title in enumerate(('기존 · 멀리서도 표시','수정 · 4칸 안','수정 · 4칸 밖')):d.text((12+j*488,46),title,font=font(20),fill='#30291f')
 for i,(id,label)in enumerate([('Naru','예성 나루 · 장터'),('Gaegyeong','개경 · 객주'),('Dakmok','닥목 · 한지 공방'),('transit_woodtown','목재 장터 · 장터/게시판')]):
  y=78+i*300;d.text((12,y),label,font=font(18),fill='#30291f')
  for j,state in enumerate(('before','near','far')):
   with Image.open(OUT/'Unity'/f'{id}-{state}.png')as im:assert im.size==(480,270);b.paste(im.convert('RGB'),(j*488,y+25))
 b.save(OUT/'game-comparison-final.png')
if __name__=='__main__':board()
