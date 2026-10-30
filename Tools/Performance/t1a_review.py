"""QA-only: compare real Unity capture frames, without editing game artwork."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];P=ROOT/'ArtReview/TransitionBoundary-20261030';O=P/'Review';O.mkdir(exist_ok=True)
font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
def latest(prefix):
    paths=sorted(P.glob(prefix+'*.png'));start=paths[0].stat().st_mtime
    return [p for p in paths if p.stat().st_mtime>=start]
a,b=latest('motion-before-'),latest('motion-after-');frames=[]
for i in range(max(len(a),len(b))):
    im=Image.new('RGB',(960,300),(28,35,40));d=ImageDraw.Draw(im)
    d.text((8,4),'기존: 화면 중간에서 전환',font=font,fill='white');d.text((488,4),'수정: 화면 끝까지 걸어간 뒤 전환',font=font,fill='white')
    im.paste(Image.open(a[min(i,len(a)-1)]),(0,30));im.paste(Image.open(b[min(i,len(b)-1)]),(480,30));frames.append(im)
frames[0].save(O/'walking-before-after.gif',save_all=True,append_images=frames[1:],duration=90,loop=0,optimize=False)
board=Image.new('RGB',(960,1200),(28,35,40));d=ImageDraw.Draw(board)
for row,edge in enumerate(['W','E','N','S']):
    for col,stage in enumerate(['before','after']):
        d.text((col*480+8,row*300+4),edge+' · '+('기존 판정/카메라 재현'if stage=='before'else'수정'),font=font,fill='white');board.paste(Image.open(P/(edge+'-'+stage+'.png')),(col*480,row*300+30))
board.save(O/'four-directions.png')
print('Actual capture frames:',len(a),len(b))
