"""Re-render approved v3 geometry with coherent material outlines and shared color families."""
from pathlib import Path
import sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
import npc_faces_v3 as V3
C=V3.C
OUT=ROOT/'ArtReview/NPC-v4';OUT.mkdir(parents=True,exist_ok=True)
ACTIVE=''


def clean_render(rows,pal):
    # Semantic palette changes retain individual skin/clothing colors and all pixel positions.
    pal.update(Q=(248,219,186),S=(207,153,119),e=(34,27,32),E=(46,33,32),w=(253,247,233),N=(175,109,85))
    if ACTIVE=='chief':pal.update(q=(237,193,157),p=(226,149,140),r=(159,150,136),R=(204,195,177),B=(241,233,216),Z=(202,190,169),c=(230,225,234),a=(189,180,198),b=(157,117,168))
    elif ACTIVE=='woman':pal.update(q=(237,193,157),p=(228,147,144),r=(57,43,42),R=(91,68,57),c=(230,153,174),a=(194,119,143),b=(235,210,147),v=(87,107,144),j=(124,85,59),J=(174,127,85),K=(221,185,137),D=(79,55,41),g=(208,172,108))
    else:pal.update(q=(231,182,141),p=(215,138,112),r=(57,43,42),R=(92,69,57),c=(154,109,76),a=(120,82,56),b=(224,186,112),t=(179,131,74),T=(227,191,120),d=(108,76,45))
    image=Image.new('RGBA',(16,len(rows)))
    def at(x,y):return rows[y][x] if 0<=x<16 and 0<=y<len(rows) else '.'
    def family(ch):
        if ch in 'qQSNp':return 'skin'
        if ch in 'rR':return 'hair'
        if ch in 'hH':return 'hat'
        if ch in 'jJKDtTd':return 'wood'
        return 'clothes'
    for y,row in enumerate(rows):
        for x,ch in enumerate(row):
            if ch=='.':continue
            if ch=='o':
                neighbors=[at(x+dx,y+dy) for dx,dy in ((0,1),(0,-1),(-1,0),(1,0))]
                groups=[family(v) for v in neighbors if v not in '.o']
                group=max(dict.fromkeys(groups),key=groups.count) if groups else 'clothes'
                if group=='skin':color=(119,83,69)
                elif group=='hair':color=(70,56,48) if ACTIVE=='chief' else (38,30,33)
                elif group=='hat':color=(31,28,37)
                elif group=='wood':color=(78,55,39)
                elif ACTIVE=='trader':color=(76,53,39)
                else:color=(79,66,70)
            else:
                color=pal[ch]
                if ch in C.SHADEABLE and (at(x+1,y)=='o' or at(x,y+1)=='o'):
                    if ch=='q':color=pal['S']
                    elif ch=='c':color=pal['a']
                    elif ch=='r':color=(126,117,105) if ACTIVE=='chief' else (42,32,33)
                    else:color=C.shade(color)
            image.putpixel((x,y),(*color,255))
    return image


def generate():
    global ACTIVE
    old=C.render
    for name in ('chief','woman','trader'):
        ACTIVE=name;C.render=clean_render
        try:image=V3.render(name)
        finally:C.render=old
        # The v3 painter adds props after the renderer; map their equivalent colors into the same families.
        mapping={(76,54,38):(78,55,39),(152,111,66):(154,109,76),(226,190,112):(224,186,112),
                 (103,70,46):(108,76,45),(169,124,84):(174,127,85),(200,120,142):(194,119,143)}
        image.putdata([(*mapping.get(pixel[:3],pixel[:3]),pixel[3]) if pixel[3] else (0,0,0,0) for pixel in image.get_flattened_data()])
        image.save(OUT/f'{name}-front.png')

if __name__=='__main__':generate()
