"""Front-only style checkpoint: reuse existing native character renderer, not HD downsampling."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtGen'))
import chars as C
sys.argv=[sys.argv[0],str(ROOT/'Tools/ArtGen')]
import chars_human as H
OUT=ROOT/'ArtReview/Refine-v2/NPC'
OUT.mkdir(parents=True,exist_ok=True)
for name,species,top in [('chief','elder','hat'),('woman','villager_rabbit','bun'),('trader','trader','hat')]:
    over,_,_,_=C.SPECIES[species]
    palette=dict(C.BASE);palette.update(H.SKIN);palette.update(over)
    palette['r']=(150,140,132) if name=='chief' else (52,42,44)
    if name=='trader':palette.update(h=(166,120,64),H=(226,190,116))
    image=C.render(H.compose('d',0,top),palette)
    d=ImageDraw.Draw(image)
    if name=='chief':
        d.line((4,14,11,14),fill=(240,234,220));d.line((5,15,10,15),fill=(240,234,220));d.line((6,16,9,16),fill=(214,204,186))
        d.line((15,12,15,22),fill=(92,64,42));d.point((14,12),fill=(166,120,84));d.point((14,17),fill=H.SKIN['q'])
    elif name=='woman':
        d.rectangle((6,5,9,7),fill=(52,42,44))
        d.polygon([(5,2),(10,2),(11,3),(10,5),(5,5),(4,3)],fill=(118,80,56))
        d.line((6,1,9,1),fill=(66,48,42));d.line((5,2,9,2),fill=(196,170,132));d.point((5,3),fill=(226,206,172))
        d.rectangle((4,21,11,21),fill=(92,112,160));d.rectangle((5,22,10,22),fill=(66,80,118))
    else:
        pack=Image.new('RGBA',(16,24));p=ImageDraw.Draw(pack)
        p.rectangle((0,12,4,21),fill=(66,48,42));p.rectangle((1,13,3,20),fill=(150,104,72));p.line((1,14,3,14),fill=(226,206,172));p.line((2,12,2,21),fill=(160,120,64))
        pack.alpha_composite(image);image=pack;d=ImageDraw.Draw(image)
        d.line((6,14,9,14),fill=(86,58,46));d.point((7,15),fill=(150,84,74))
    image.save(OUT/f'{name}-front.png')
