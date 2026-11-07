"""Compose factual Unity captures; never alters artwork/game assets."""
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/T3-Production-20261108'
def main():
 ids=json.loads((ROOT/'Tools/WorldLayout/t3_boundaries.json').read_text())['cuts']
 font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',16);title=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',22)
 sheet=Image.new('RGB',(1316,912),'#eee9df');draw=ImageDraw.Draw(sheet);draw.text((12,8),'본편 13맵 · 승인한 길·풀밭 외곽 경계 적용',font=title,fill='#292824')
 for i,mid in enumerate(ids):
  d=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text());x=12+i%4*326;y=42+i//4*216
  draw.text((x,y),d['name'],font=font,fill='#292824')
  with Image.open(OUT/(mid+'-game.png'))as im:sheet.paste(im.convert('RGB').resize((320,180),Image.Resampling.NEAREST),(x,y+26))
 sheet.save(OUT/'13maps-game-mosaic.png')
 sheet=Image.new('RGB',(984,652),'#eee9df');draw=ImageDraw.Draw(sheet)
 for i,(file,text)in enumerate([('grass-before','풀밭에서 경계로 접근'),('grass-edge','실제 맵 끝 접촉'),('grass-arrival','장단 풀밭 도착'),('grass-return','같은 구간으로 나루 귀환')]):
  x=12+i%2*486;y=12+i//2*320;draw.text((x,y),text,font=font,fill='#292824')
  with Image.open(OUT/(file+'.png'))as im:sheet.paste(im.convert('RGB').resize((474,267),Image.Resampling.NEAREST),(x,y+27))
 sheet.save(OUT/'grass-crossing.png')
if __name__=='__main__':main()
