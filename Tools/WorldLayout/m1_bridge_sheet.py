"""ArtReview/M1-Tiles/report.json의 다리마다 둘레를 잘라 한 장으로(bridges.png)."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
HERE=Path(__file__).resolve().parent;sys.path.insert(0,str(HERE))
import terrain_v3 as v
D=HERE.parents[1]/'ArtReview/M1-Tiles'
FONT=sys.argv[1] if len(sys.argv)>1 else '/usr/share/fonts/opentype/noto/NotoSerifCJK-Bold.ttc'
r=json.loads((D/'report.json').read_text());im=Image.open(D/'world-full.png')
f=ImageFont.truetype(FONT,20,index=1)
o=Image.new('RGB',(720*2+30,(432+40)*3+10),(30,34,40));d=ImageDraw.Draw(o)
for i,b in enumerate(r['bridges']):
    iy,ix=v.idx(int(b['x']),int(b['y']))
    c=im.crop(((ix-15)*16,(iy-9)*16,(ix+15)*16,(iy+9)*16)).resize((720,432),Image.NEAREST)
    x=10+(i%2)*730;y=10+(i//2)*472
    d.text((x,y),f"다리 {i+1}: 각도 {b['angle']}°, 길이 {b['length']}칸",fill='white',font=f);o.paste(c,(x,y+32))
o.save(D/'bridges.png')
