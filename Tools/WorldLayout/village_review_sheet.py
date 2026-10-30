"""QA layout of actual Unity PNG captures; no gameplay art editing."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
R=Path(__file__).resolve().parents[2];OUT=R/'ArtReview/VillageFill-20261030';D=OUT/'Unity';F=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',22)
shots=json.loads((OUT/'shots.json').read_text())['shots'];names={v['slug']:k.split('/')[-1]for k,v in json.loads((R/'ArtReview/M2-Zones/zones.json').read_text())['villages'].items()}
def paste(im,p,x,y):
 with Image.open(p)as src:
  assert src.size==(1280,720)
  im.paste(src.resize((640,360),Image.Resampling.NEAREST),(x,y))
for row in shots:
 s=row['slug'];im=Image.new('RGB',(1280,396),(29,37,40));d=ImageDraw.Draw(im);d.text((12,5),'기존',font=F,fill='white');d.text((652,5),'검토용 수정',font=F,fill='white');paste(im,D/f'{s}-before.png',0,36);paste(im,D/f'{s}-after.png',640,36);im.save(OUT/f'{s}-comparison.png')
board=Image.new('RGB',(1920,4*396),(29,37,40));dr=ImageDraw.Draw(board)
for i,row in enumerate(shots[:11]):
 x,y=i%3*640,i//3*396;dr.text((x+12,y+5),names[row['slug']],font=F,fill='white');paste(board,D/f"{row['slug']}-after.png",x,y+36)
board.save(OUT/'all-settlements.png')
rice=Image.new('RGB',(1280,3*396),(29,37,40));dr=ImageDraw.Draw(rice)
for i,row in enumerate(shots[11:]):
 dr.text((12,i*396+5),['장단 · 기존','개경 · 기존','송악 · 기존'][i],font=F,fill='white');dr.text((652,i*396+5),'논 추가 검토',font=F,fill='white');paste(rice,D/f"{row['slug']}-before.png",0,i*396+36);paste(rice,D/f"{row['slug']}-after.png",640,i*396+36)
rice.save(OUT/'rice-comparison.png')
print('11 settlement sheet, 14 paired photos, 3 rice comparisons assembled from actual PNGs')
