from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/NPC-v4'
font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)

def review():
 people=[('chief','촌장'),('woman','아낙'),('trader','행상')]
 board=Image.new('RGB',(840,550),'#f5f0e6');d=ImageDraw.Draw(board);d.text((20,12),'사람 NPC · 얼굴과 비율 유지, 선·색 정리',font=font(23),fill='#332d2a')
 for row,(name,title) in enumerate(people):
  y=75+row*152;d.text((18,y+48),title,font=font(18),fill='#332d2a');counts=[]
  for col,(version,label) in enumerate([('NPC-v3','정리 전'),('NPC-v4','정리 후')]):
   x=160+col*340
   if row==0:d.text((x,46),label,font=font(17),fill='#332d2a')
   im=Image.open(ROOT/f'ArtReview/{version}/{name}-front.png').convert('RGBA');counts.append(len({p[:3] for p in im.get_flattened_data() if p[3]}));z=im.resize((96,144),Image.Resampling.NEAREST);d.rectangle((x-4,y-2,x+99,y+146),fill='#d8d2c4');board.paste(z,(x,y),z)
  d.text((18,y+76),f'{counts[0]} → {counts[1]}색',font=font(14),fill='#685b51')
 board.save(OUT/'comparison.png')
 base=Image.open(ROOT/'Docs/Images/GoryeoArt/capital-polish-day.png').convert('RGBA').resize((480,270),Image.Resampling.NEAREST)
 before=base.copy();after=base.copy()
 for name,point in [('house',(135,175)),('tree',(292,175))]:
  asset=Image.open(ROOT/f'ArtReview/Refine-v2/{name}-gentle.png').convert('RGBA');before.alpha_composite(asset,point);after.alpha_composite(asset,point)
 for (name,_),point in zip(people,[(208,183),(245,187),(366,185)]):
  before.alpha_composite(Image.open(ROOT/f'ArtReview/NPC-v3/{name}-front.png').convert('RGBA'),point);after.alpha_composite(Image.open(OUT/f'{name}-front.png').convert('RGBA'),point)
 before.save(OUT/'game-before-480x270.png');after.save(OUT/'game-after-480x270.png')
 board=Image.new('RGB',(1000,345),'#f5f0e6');d=ImageDraw.Draw(board);d.text((15,10),'게임 화면 크기 · 각 480×270 · 기존 캡처 합성 · 건물/나무 동일',font=font(20),fill='#332d2a')
 for x,im,title in [(12,before,'선·색 정리 전'),(508,after,'선·색 정리 후')]:d.text((x,40),title,font=font(16),fill='#332d2a');board.paste(im.convert('RGB'),(x,66))
 board.save(OUT/'game-comparison.png')

if __name__=='__main__':review()
