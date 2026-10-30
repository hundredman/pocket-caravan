"""Review native profile hair changes against the preceding committed walk trial."""
from pathlib import Path
import io,json,subprocess
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/C1-SideHair';OUT.mkdir(parents=True,exist_ok=True)
BASE='a8bc528e'
PEOPLE=[('chief','나루 촌장'),('woman','아낙'),('trader','행상'),('farmer','농부'),('herbalist','약재상'),('dakmok_chief','닥목 촌장'),('gaekju','객주'),('child','아이'),('boatman','뱃사공'),('bandit','도적')]
def before(path):return Image.open(io.BytesIO(subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT))).convert('RGBA')
def generate():
 font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
 board=Image.new('RGB',(850,1190),'#f5f0e6');d=ImageDraw.Draw(board)
 d.text((18,9),'옆머리 보완 · 정면/뒤/크기는 유지',font=font(23),fill='#332d2a')
 for x,title in [(230,'왼쪽 전'),(360,'왼쪽 후'),(550,'오른쪽 전'),(680,'오른쪽 후')]:d.text((x,47),title,font=font(17),fill='#332d2a')
 checks=[]
 for row,(name,title) in enumerate(PEOPLE):
  y=80+row*110;d.text((18,y+32),title,font=font(18),fill='#332d2a')
  for x,direction,old in [(235,'l',True),(365,'l',False),(555,'r',True),(685,'r',False)]:
   path=f'ArtReview/C1-Walk/frames/{name}_{direction}0.png';im=before(path) if old else Image.open(ROOT/path).convert('RGBA');z=im.resize((64,96),Image.Resampling.NEAREST);board.paste(z,(x,y),z);board.paste(im,(x+72,y+62),im)
  for direction in 'dulr':
   for f in range(4):
    path=f'ArtReview/C1-Walk/frames/{name}_{direction}{f}.png';old=before(path);new=Image.open(ROOT/path).convert('RGBA')
    changed=sum(a!=b for a,b in zip(old.get_flattened_data(),new.get_flattened_data()))
    assert old.getchannel('A').tobytes()==new.getchannel('A').tobytes()
    if direction in 'du':assert old.tobytes()==new.tobytes()
    else:assert changed>0
    checks.append(dict(name=name,direction=direction,frame=f,changedPixels=changed,alphaIdentical=True))
 board.save(OUT/'comparison.png')
 (OUT/'checks.json').write_text(json.dumps(dict(baseCommit=BASE,framesChecked=160,changedSideFrames=80,unchangedFrontBackFrames=80,alphaIdenticalAll=True,frames=checks),ensure_ascii=False,indent=2)+'\n')
 print('80 side frames changed; 80 front/back frames and all alpha silhouettes identical')
if __name__=='__main__':generate()
