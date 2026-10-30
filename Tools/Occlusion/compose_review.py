"""Compose unchanged Play captures; crops/enlargement are review presentation only."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageChops
import json
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'ArtReview/C3a-2026-10-26';P=F/'screens'
font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',24)
small=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
# 1920x1080 Play output, camera's 480x270 target view. Nearest-only presentation.
canvas=Image.new('RGB',(1040,1340),'#e8e5dc');d=ImageDraw.Draw(canvas)
d.text((20,12),'C3a · 정상 보행 위치의 가림 순서 시험',font=font,fill='#202020')
d.text((20,49),'같은 위치 · 같은 그림 · 발밑 충돌 그대로 / 왼쪽 기존 · 오른쪽 수정안',font=small,fill='#303030')
boxes=[(880,400,1330,650),(960,330,1410,580),(650,390,1100,640),(730,380,1180,630)]
labels=['나무 옆 — 잎이 캐릭터보다 앞에 보이도록','나무 앞 — 앞에 선 캐릭터는 계속 보임','궁궐 옆 — 처마가 캐릭터보다 앞에 보이도록','궁궐 앞 — 앞에 선 캐릭터는 계속 보임']
for i,box in enumerate(boxes):
 y=92+i*310;d.text((20,y),labels[i],font=small,fill='#202020')
 for col,idx in enumerate((i,i+4)):
  im=Image.open(P/f'pose-{idx}.png').convert('RGB').crop(box)
  panel=Image.new('RGB',(494,268),'#d4d0c8');panel.paste(im,((494-im.width)//2,9));canvas.paste(panel,(20+col*506,y+27))
canvas.save(F/'comparison.png')
# Full camera captures at their intended 480x270 target view (no filtering).
board=Image.new('RGB',(980,620),'#e8e5dc');d=ImageDraw.Draw(board)
for row,i in enumerate((0,2)):
 d.text((10,row*310),('나무 옆' if row==0 else '궁궐 옆')+' · 왼쪽 기존 / 오른쪽 수정안',font=small,fill='#202020')
 for col,idx in enumerate((i,i+4)):
  im=Image.open(P/f'pose-{idx}.png').convert('RGB').resize((480,270),Image.Resampling.NEAREST);board.paste(im,(10+col*490,row*310+32))
board.save(F/'camera-comparison.png')
for i in range(4):
 a=json.load(open(P/f'pose-{i}.json'));b=json.load(open(P/f'pose-{i+4}.json'));assert a==b
 print(i,ImageChops.difference(Image.open(P/f'pose-{i}.png').convert('RGB'),Image.open(P/f'pose-{i+4}.png').convert('RGB')).getbbox())
