from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[2]
out=Image.new('RGB',(980,960),(122,156,102));draw=ImageDraw.Draw(out)
font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',22)
for i,version in enumerate(('v1','v2')):
    draw.text((16+i*490,12),'이전'if i==0 else'수정 — 황금색·모은 포기',font=font,fill='white')
    im=Image.open(root/f'ArtReview/RipeRice-{version}/rice-ripe.png').convert('RGBA')
    enlarged=im.resize((256,256),Image.Resampling.NEAREST);out.paste(enlarged,(96+i*490,52),enlarged)
    field=Image.open(root/f'ArtReview/RiceField-Tiles-{version}/assembled-2x2.png').convert('RGBA')
    enlarged=field.resize((448,480),Image.Resampling.NEAREST);out.paste(enlarged,(16+i*490,392),enlarged)
draw.text((16,345),'같은 간격으로 심은 논 비교',font=font,fill='white')
out.save(Path(__file__).resolve().parent/'revision-comparison.png')
