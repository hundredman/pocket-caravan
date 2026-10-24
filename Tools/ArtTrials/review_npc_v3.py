from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/NPC-v3'
font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)

def review():
 board=Image.new('RGB',(800,740),'#f5f0e6');d=ImageDraw.Draw(board);d.text((20,12),'사람 NPC · 얼굴과 소품 연결 수정',font=font(23),fill='#332d2a')
 people=[('chief','elder','촌장'),('woman','villager_rabbit','아낙'),('trader','trader','행상')]
 for row,(name,old,label) in enumerate(people):
  y=80+row*160;d.text((15,y+52),label,font=font(18),fill='#332d2a')
  for col,(path,title) in enumerate([(f'Assets/PocketCaravan/Art/Characters/{old}_tall_d0.png','동물 기준'),(f'ArtReview/Refine-v2/NPC/{name}-front.png','이전 사람'),(f'ArtReview/NPC-v3/{name}-front.png','수정 사람')]):
   x=125+col*225
   if row==0:d.text((x,48),title,font=font(17),fill='#332d2a')
   im=Image.open(ROOT/path).convert('RGBA');z=im.resize((96,144),Image.Resampling.NEAREST);d.rectangle((x-4,y-3,x+99,y+147),fill='#d8d2c4');board.paste(z,(x,y),z)
 d.text((20,575),'수정 시안 실루엣',font=font(18),fill='#332d2a')
 for i,(name,_,_) in enumerate(people):
  im=Image.open(OUT/f'{name}-front.png').convert('RGBA');sil=Image.new('RGBA',im.size,'#221a22');sil.putalpha(im.getchannel('A'));z=sil.resize((64,96),Image.Resampling.NEAREST);board.paste(z,(125+i*225,615),z)
 board.save(OUT/'comparison.png')
 base=Image.open(ROOT/'Docs/Images/GoryeoArt/capital-polish-day.png').convert('RGBA').resize((480,270),Image.Resampling.NEAREST)
 before=base.copy();after=base.copy()
 # Use exactly the same accepted environment candidates in both panels.
 for name,point in [('house',(135,175)),('tree',(292,175))]:
  asset=Image.open(ROOT/f'ArtReview/Refine-v2/{name}-gentle.png').convert('RGBA');before.alpha_composite(asset,point);after.alpha_composite(asset,point)
 for (name,_,_),point in zip(people,[(208,183),(245,187),(366,185)]):
  before.alpha_composite(Image.open(ROOT/f'ArtReview/Refine-v2/NPC/{name}-front.png').convert('RGBA'),point);after.alpha_composite(Image.open(OUT/f'{name}-front.png').convert('RGBA'),point)
 before.save(OUT/'game-before-480x270.png');after.save(OUT/'game-after-480x270.png')
 board=Image.new('RGB',(1000,345),'#f5f0e6');d=ImageDraw.Draw(board);d.text((15,10),'게임 화면 크기 · 각 480×270 · 기존 캡처 합성 · 건물/나무 동일',font=font(20),fill='#332d2a')
 for x,im,text in [(12,before,'이전 사람 시안'),(508,after,'수정 사람 시안')]:d.text((x,40),text,font=font(16),fill='#332d2a');board.paste(im.convert('RGB'),(x,66))
 board.save(OUT/'game-comparison.png')
if __name__=='__main__':review()
