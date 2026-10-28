"""수원지마다 둘레를 잘라 한 장으로(sources.png)."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;sys.path.insert(0,str(HERE))
import terrain_v3 as v
D=HERE.parents[1]/'ArtReview/M1-Tiles'
FONT=sys.argv[1] if len(sys.argv)>1 else '/usr/share/fonts/opentype/noto/NotoSerifCJK-Bold.ttc'
r=json.loads((D/'report.json').read_text());im=Image.open(D/'world-full.png')
f=ImageFont.truetype(FONT,20,index=1)
src=r['objects']['source']['sources'];n=len(src)
o=Image.new('RGB',(720*2+30,(432+40)*((n+1)//2)+10),(30,34,40));d=ImageDraw.Draw(o)
names={(66,-48):'북쪽 쉼터 마을 위 개울',(226,-62):'닥목 북쪽(개경 강)',(191,-48):'송악 북쪽(큰 강)',(117,-9):'호수로 드는 강(수원 산)'}
for i,(x_,y_) in enumerate(src):
    iy,ix=v.idx(x_,y_)
    c=im.crop(((ix-15)*16,(iy-9)*16,(ix+15)*16,(iy+9)*16)).resize((720,432),Image.NEAREST)
    x=10+(i%2)*730;y=10+(i//2)*472
    d.text((x,y),f"수원지 {i+1}: {names.get((x_,y_),'')}",fill='white',font=f);o.paste(c,(x,y+32))
o.save(D/'sources.png')
