"""Assemble review boards from unchanged native art and actual Unity screenshots."""
from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/E3-E2-Art-20261103';P=ROOT/'Assets/PocketCaravan/Editor/B2Preview/E3Art'
font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
def run():
 names=[('fallen_log','통나무 48×48',4),('river_flood_high','범람 · 넘침 80×48',3),('river_flood_low','범람 · 빠짐 80×48',3),('transit_spice','향신료 16×16',8),('transit_wool','양모 16×16',8)]
 board=Image.new('RGB',(1440,670),'#789764');d=ImageDraw.Draw(board)
 for i,(name,label,scale)in enumerate(names):
  x=i*288;d.text((x+10,8),label,font=font(20),fill='white');im=Image.open(OUT/'Native'/(name+'.png')).convert('RGBA');z=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST);board.paste(z,(x+15,58),z);board.paste(im,(x+20,245),im);d.text((x+110,250),'← 실제 픽셀 크기',font=font(17),fill='white')
 for i,(name,label)in enumerate([('log','Unity · 통나무와 플레이어'),('flood-high','Unity · 길에 넘친 물'),('flood-low','Unity · 물이 빠진 길')]):
  x=i*480;d.text((x+10,305),label,font=font(21),fill='white');im=Image.open(OUT/'Unity'/(name+'.png')).convert('RGB');assert im.size==(480,270);board.paste(im,(x,340))
 d.text((12,625),'검토용 · 본편 미적용 · 하단은 실제 Unity480×270 · 실제 사건 배치/작동은 승인 후 Claude 연결',font=font(21),fill='white');board.save(OUT/'review-assets.png')
 board=Image.new('RGB',(960,660),'#263035');d=ImageDraw.Draw(board)
 for row,(slug,title)in enumerate([('southport','남동 포구 · 바다 쪽 빈 모서리'),('ferry','북동 포구 · 잔물결 바탕색')]):
  for col,version in enumerate(['before','after']):
   x,y=col*480,row*330;d.text((x+8,y+6),title+(' / 이전'if col==0 else' / 수정'),font=font(19),fill='white');im=Image.open(OUT/'Unity'/(slug+'-'+version+'.png')).convert('RGB');assert im.size==(480,270);board.paste(im,(x,y+40))
 board.save(OUT/'review-ports.png')
 # Static full-map pair: render existing native tiles only, no screenshot claims.
 images=[]
 for stage in ['before','after']:
  data=json.loads((P/'Maps'/('southport-'+stage+'.json')).read_text());w,h=data['w'],data['h'];im=Image.new('RGBA',(w*16,h*16),'#1c2228');cache={}
  for layer in ['ground','deco','obj','high']:
   for j,index in enumerate(data[layer]):
    if not index:continue
    name=data['palette'][index]
    if name not in cache:cache[name]=Image.open(P/'Tiles'/(name+'.png')).convert('RGBA')
    im.alpha_composite(cache[name],(j%w*16,j//w*16))
  im.save(OUT/('southport-map-'+stage+'.png'));images.append(im)
 wide=Image.new('RGB',(1376,580),'#263035');d=ImageDraw.Draw(wide)
 for i,im in enumerate(images):wide.paste(im.resize((688,544),Image.Resampling.NEAREST),(i*688,36));d.text((i*688+10,5),'남동 포구 지형 원본 렌더 · '+('이전'if i==0 else'수정'),font=font(21),fill='white')
 wide.save(OUT/'southport-wide-comparison.png')
 print('Review boards: assets + two port pairs + static full-map pair')
if __name__=='__main__':run()
