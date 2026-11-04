"""Compose unchanged Unity screenshots and check the proposed workshop approach."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/GoryeoWorkshop-Task3-20261104'
def main():
 sys.path.insert(0,str(ROOT/'Tools/WorldLayout'));import goryeo_life
 m=goryeo_life.Map(ROOT/'Assets/PocketCaravan/Maps/Dakmok.json');p=json.loads((OUT/'proposal.json').read_text());cell=(p['x'],p['y'])
 assert m.walk(*cell)and cell not in m.occupied()and cell not in m.exits()
 assert m.d['reachable'][m.i(*cell)]
 font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',25)
 for name,frames in [('placement-comparison',[('placement-before','기존 작업장'),('placement-after','수정안 · 접수 기능 추가')]),('workflow-comparison',[('order-dialogue','주문 · 비용/시간'),('collect-dialogue','6시간 뒤 · 수령')])]:
  board=Image.new('RGB',(1920,580),'#263035');draw=ImageDraw.Draw(board)
  for i,(file,label)in enumerate(frames):
   im=Image.open(OUT/'Unity'/(file+'.png')).convert('RGB');assert im.size==(960,540);board.paste(im,(i*960,40));draw.text((i*960+12,5),label,font=font,fill='white')
  board.save(OUT/(name+'.png'))
 (OUT/'placement-checks.json').write_text(json.dumps(dict(cell=cell,walkable=True,reachable=True,exitOverlap=False,existingObjectOverlap=False,blocksMovement=False,productionApplied=False),ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
