"""Validate whole art/unchanged placement and compose review-only comparison boards."""
import json,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/SpecialtyMarkets-Task5-20261104'
def main():
 data=json.loads((OUT/'layout.json').read_text());old=json.loads((ROOT/data['source']).read_text());checks=[]
 assert data['status']in('review','approved');assert len(data['maps'])==len(old['maps'])==2
 for a,b in zip(data['maps'],old['maps']):
  assert a['mapId']==b['mapId'];assert len(a['spaces'])==len(b['spaces'])
  for sa,sb in zip(a['spaces'],b['spaces']):
   assert sa['id']==sb['id'] and len(sa['props'])==len(sb['props'])
   for p,q in zip(sa['props'],sb['props']):
    assert tuple(p[k]for k in ['x','y','w','h'])==tuple(q[k]for k in ['x','y','w','h']);assert p['oldAsset']==q['asset']
 checks.append(dict(unchangedAllPositions=True,unchangedAllFootprints=True,productionApplied=False))
 for a in data['assets']:
  with Image.open(ROOT/a['image'])as im:
   im=im.convert('RGBA');assert im.size==(a['w']*16,a['h']*16);assert set(im.getchannel('A').getdata())<={0,255}
   checks.append(dict(asset=a['id'],size=im.size,rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest()))
 font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',25);small=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
 board=Image.new('RGB',(1200,760),'#39483b');draw=ImageDraw.Draw(board)
 labels={'fish_rack':'생선 덕장','salt_stall':'소금 좌판','cloth_stall':'피륙·양모 좌판','spice_stall':'향신료 좌판'}
 for i,a in enumerate(data['assets']):
  x=i*300;draw.text((x+15,12),labels[a['id']],font=font,fill='white')
  ref='wares_counter'if a['h']==2 else 'grain_stall'
  for y,file,label in [(90,ROOT/'Assets/PocketCaravan/Art/Goryeo/Markets'/(ref+'.png'),'기존 크기 기준'),(435,ROOT/a['image'],'새 그림 · 5배 확대')]:
   draw.text((x+15,y-35),label,font=small,fill='white')
   with Image.open(file)as im:
    im=im.convert('RGBA');board.paste(im.resize((im.width*5,im.height*5),Image.Resampling.NEAREST),(x+30,y),im.resize((im.width*5,im.height*5),Image.Resampling.NEAREST))
   with Image.open(file)as im:board.paste(im,(x+30,y+245),im)
 board.save(OUT/'native-comparison.png')
 for mid,title in [('transit_southport','남동 포구'),('transit_crossroads','내륙 교역 길목')]:
  board=Image.new('RGB',(1920,1160),'#263035');draw=ImageDraw.Draw(board)
  for j,space in enumerate(['market','event-yard']):
   for i,(suffix,label)in enumerate([('before','기존'),('after','전용 좌판 시안')]):
    x,y=i*960,j*580
    with Image.open(OUT/'Unity'/f'{mid}-{space}-{suffix}.png')as im:
     assert im.size==(960,540);board.paste(im.convert('RGB'),(x,y+40))
    draw.text((x+12,y+5),f'{title} · '+('장터'if j==0 else '마당')+' / '+label,font=font,fill='white')
  board.save(OUT/f'{mid}-comparison.png')
 (OUT/'checks.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
