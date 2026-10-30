"""Contact sheets from actual Unity captures at the native 480x270 game resolution."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Environment-Expand-v2'
font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
for file,views in [('game-comparison.png',[('palace','개경 · 궁궐'),('forest','개경 · 버드나무/숲 · 캐릭터 검토 위치 오류(가림 판정 제외)')]),('cottage-game-comparison.png',[('cottage','개경 · 초가집'),('cottage-small','나루 · 작은 초가집')])]:
 board=Image.new('RGB',(1000,685),'#f5f0e6');d=ImageDraw.Draw(board)
 d.text((15,8),'실제 Unity 화면 · 원본 / 약한 보정 · 각 480×270',font=font(22),fill='#332d2a')
 for row,(name,title) in enumerate(views):
  y=42+row*318;d.text((15,y),title,font=font(18),fill='#332d2a')
  for x,version,label in [(12,'before','원본'),(508,'after','보정')]:
   im=Image.open(OUT/f'game-{name}-{version}.png').convert('RGB');assert im.size==(1920,1080)
   im=im.resize((480,270),Image.Resampling.NEAREST);d.text((x,y+23),label,font=font(14),fill='#685b51');board.paste(im,(x,y+44))
 board.save(OUT/file)
print('Four before/after pairs, same camera/PPU; review panels at native game pixels')
