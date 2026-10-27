"""Native 16x24 face study. Keep the original animal renderer's body/shading, draw human features intentionally."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtGen'))
import chars as C
OUT=ROOT/'ArtReview/NPC-v3';OUT.mkdir(parents=True,exist_ok=True)


def render(name, *, raised_arm=True, separate_right_eye=False):
    species={'chief':'elder','woman':'villager_rabbit','trader':'trader'}[name]
    over,top,hat,mask=C.SPECIES[species]
    rows=C.compose('d',0,top,hat,mask)
    for y in range(5,16):rows[y]=list('.'*16)
    pal=dict(C.BASE);pal.update(over)
    pal.update(q=(237,192,155),Q=(250,220,187),S=(205,147,113),r=(54,40,39),R=(93,64,54),e=(34,26,34),E=(48,32,30),w=(255,250,234),p=(235,153,143),N=(188,112,88),B=(239,233,217),Z=(201,194,179))
    if name=='chief':pal.update(r=(160,151,137),R=(207,200,182))
    if name=='trader':pal.update(q=(229,178,132),Q=(245,205,155),S=(189,130,90),p=(216,132,100))
    def point(x,y,ch):rows[y][x]=ch
    def run(y,a,b,ch):
        for x in range(a,b+1):point(x,y,ch)
    # A round face with small ears and a separate chin, rather than a rectangular skin patch.
    for y,a,b in [(8,4,11),(9,3,12),(10,2,13),(11,2,13),(12,2,13),(13,3,12),(14,4,11),(15,6,9)]:
        run(y,a,b,'q');point(a,y,'o');point(b,y,'o')
    for y in (9,10):run(y,4,10,'Q')
    point(1,12,'S');point(14,12,'S');point(2,12,'q');point(13,12,'S')
    run(14,5,10,'Q');run(15,6,9,'S')
    # Eye highlight/pupil/brown lower rim; warm nose and a small offset smile.
    for x in (5,9):
        point(x,11,'w');point(x+1,11,'e');point(x,12,'e');point(x+1,12,'E')
    point(7,12,'S');point(3,13,'p');point(12,13,'p');point(7,14,'N');point(8,14,'q')
    if name=='chief':
        run(8,3,12,'r');point(4,9,'R');point(11,9,'r')
        run(10,5,6,'B');run(10,9,10,'B')
        for y,a,b in [(5,6,9),(6,5,10)]:run(y,a,b,'h')
        point(6,5,'H');run(7,1,14,'h');run(7,2,12,'H')
        # Beard wraps below the mouth, leaving cheeks/nose visible. No straight white mask band.
        for x in (4,5,10,11):point(x,13,'B')
        for x in (5,6,9,10):point(x,14,'B')
        run(15,6,9,'B');run(16,7,8,'Z')
    elif name=='woman':
        run(8,4,11,'r');run(9,3,5,'r');run(9,10,12,'r');point(4,8,'R');point(5,9,'R');point(10,10,'r');point(3,10,'r')
        point(13,8,'r');point(14,8,'R');point(14,9,'r');point(13,9,'g')
        if separate_right_eye:point(10,10,'Q')
        run(7,5,10,'r')
        pal.update(j=(118,80,56),J=(177,127,82),K=(226,190,146),D=(66,45,35))
        run(2,5,10,'D');run(3,4,11,'j');run(4,4,11,'J');run(5,5,10,'j');run(6,6,9,'D');point(5,3,'K');point(5,4,'K')
        # Skirt preserves the existing rounded garment, with a dark hem rather than trouser blocks.
        pal['v']=(92,112,150);run(21,4,11,'v')
    else:
        run(8,3,12,'r');run(9,3,5,'r');point(11,9,'r');point(12,10,'r');point(4,9,'R')
        pal.update(t=(174,124,67),T=(229,190,112),d=(104,74,44))
        run(4,6,9,'d');run(5,4,11,'t');run(6,3,12,'t');run(7,1,14,'d');run(6,4,10,'T');run(7,2,12,'t')
        point(6,14,'E');point(9,14,'E');point(7,15,'N')
    C.SHADEABLE.update('qr')
    image=C.render(rows,pal)
    # Put props behind the body, with a hand at the contact point, avoiding pasted-on facial bands.
    if name=='trader':
        pack=Image.new('RGBA',(16,24));d=ImageDraw.Draw(pack);d.rectangle((0,13,4,21),fill=(76,54,38));d.rectangle((1,14,3,20),fill=(152,111,66));d.line((2,13,2,21),fill=(226,190,112));pack.alpha_composite(image);image=pack
    d=ImageDraw.Draw(image)
    if name=='chief':
        d.line((15,15,15,22),fill=(103,70,46));d.point((14,15),fill=(169,124,84));d.line((12,18,13,18),fill=pal['c']);d.point((14,18),fill=pal['q'])
    elif name=='woman' and raised_arm:
        # A visible raised hand supports the jar rim; keep the head outline readable.
        d.line((0,17,0,11),fill=(200,120,142));d.point((1,10),fill=pal['q']);d.point((2,9),fill=pal['q']);d.point((3,8),fill=pal['q']);d.point((4,7),fill=pal['q'])
    return image


if __name__=='__main__':
    for name in ('chief','woman','trader'):render(name).save(OUT/f'{name}-front.png')
