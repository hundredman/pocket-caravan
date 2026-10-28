"""ArtReview/M1-Tiles/cam-*.png를 이름표와 함께 한 장으로 모은다."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import sys
ROOT=Path(__file__).resolve().parents[2];D=ROOT/'ArtReview/M1-Tiles'
FONT=sys.argv[1] if len(sys.argv)>1 else '/usr/share/fonts/opentype/noto/NotoSerifCJK-Bold.ttc'
names=[('lake','① 남쪽 호숫가 마을 자리와 호수(연꽃)'),('source','② 수원 산(소나무 무리 타일)과 강'),('songak','③ 송악 산과 다리'),('dakmok','④ 닥목 북쪽 산맥'),
       ('naru','⑤ 나루 강 하구와 다리'),('jangdan','⑥ 장단 들판(논)과 큰 강'),('wetroad','⑦ 북동 길 강과 갈대'),('forest','⑧ 숲 타일과 풀밭')]
f=ImageFont.truetype(FONT,26,index=1)
ims=[Image.open(D/f'cam-{k}.png') for k,_ in names];W,H=ims[0].size
o=Image.new('RGB',(W*2+30,(H+50)*4+10),(30,34,40));d=ImageDraw.Draw(o)
for i,(im,(k,t)) in enumerate(zip(ims,names)):
    x=10+(i%2)*(W+10);y=10+(i//2)*(H+50);d.text((x,y),t,fill='white',font=f);o.paste(im,(x,y+40))
o.save(D/'cams-sheet.png')
