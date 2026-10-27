"""C1 authored pixel trials, isolated from Assets. 16x24; distinct bodies and props."""
from pathlib import Path
import json
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtPrep'))
from prepare_art import prepare,load_palette
OUT=ROOT/'ArtReview/C1'
COLORS=dict(ink='#503c2d',hair='#242c38',hat='#141f2e',hatlight='#4a5662',skin='#f8c47f',skinshade='#f0b869',cheek='#ea9192',white='#f6f2ec',cream='#f0eadc',creamshade='#d8c2a2',blue='#2c3947',bluelight='#4a5662',pink='#b18987',pinklight='#ea9192',wood='#764e34',woodlight='#ae8d6d',straw='#e8a958',strawlight='#f0b869',brown='#835e3f',brownlight='#b27a52',green='#658555',greendark='#394b28')

def render(name,direction,frame):
    if direction=='l':
        return render(name,'r',frame).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    im=Image.new('RGBA',(16,24));d=ImageDraw.Draw(im)
    c=lambda name:COLORS[name]
    def rect(box,fill,outline=None): d.rectangle(box,fill=c(fill),outline=c(outline) if outline else None)
    def poly(points,fill,outline='ink'): d.polygon(points,fill=c(fill));d.line(points+[points[0]],fill=c(outline))
    def face(box,beard=False):
        x0,y0,x1,y1=box
        if direction=='r': x0+=2
        poly([(x0+2,y0),(x1-2,y0),(x1,y0+2),(x1,y1-2),(x1-2,y1),(x0+2,y1),(x0,y1-2),(x0,y0+2)],'skinshade')
        rect((x0+1,y0+2,x1-1,y1-2),'skin');rect((x0+2,y0+1,x1-2,y1-1),'skin')
        if direction=='u':rect((x0+1,y0+1,x1-1,y1-1),'hair');return
        ey=y0+3
        eyes=[x0+2,x1-2] if direction=='d' else [x0+2] if direction=='l' else [x1-2]
        for x in eyes:
            rect((x,ey,x+1,ey+1),'ink');d.point((x,ey),fill=c('white'))
        if direction=='d':
            d.point((x0+1,ey+2),fill=c('cheek'));d.point((x1-1,ey+2),fill=c('cheek'))
            d.point(((x0+x1)//2,y1-1),fill=c('wood'))
        if direction=='r' and x1<15:
            d.point((x1+1,y0+4),fill=c('skinshade'));d.point((x1+1,y0+5),fill=c('ink'))
        if beard:
            poly([(x0+1,y1-2),(x1-1,y1-2),(x1-2,y1+1),((x0+x1)//2,y1+3),(x0+2,y1+1)],'cream')
            rect((x0+2,y1-2,x1-2,y1-1),'white')
    step=(0,1,0,-1)[frame]
    if name=='naru_chief':
        # Slender robe, projecting cane, angular long beard; not the common villager body.
        poly([(4,14),(11,14),(12,21),(3,21)],'cream')
        rect((4,15,5,19),'white');rect((10,16,11,20),'creamshade')
        rect((6,18,10,18),'bluelight');d.line((6,14,8,17,10,14),fill=c('creamshade'))
        rect((4,22-(step>0),6,23-(step>0)),'wood','ink');rect((9,22-(step<0),11,23-(step<0)),'wood','ink')
        face((2,7,12,14),True)
        rect((5,2,9,5),'hat','ink');rect((5,2,6,4),'hatlight');rect((1,5,14,6),'hat','ink');rect((2,5,12,5),'hatlight')
        if direction=='u':rect((5,10,10,13),'creamshade');rect((6,12,9,14),'cream')
        rect((14,11,14,22),'wood');rect((13,10,15,11),'woodlight','ink');rect((11,15,13,16),'skin','ink')
    elif name=='woman':
        # Broad skirt, raised arm and large water jar balanced above the head.
        poly([(5,14),(10,14),(14,22),(1,22)],'blue')
        poly([(5,16),(7,16),(5,21),(3,21)],'bluelight',outline='bluelight')
        rect((3,14,12,17),'pink','ink');rect((4,14,7,15),'pinklight');rect((8,16,11,17),'creamshade')
        rect((2,10,3,15),'pink','ink');rect((2,7,3,10),'skin','ink')
        rect((4,23-(step>0),6,23),'wood');rect((10,23-(step<0),12,23),'wood')
        face((3,6,13,13))
        rect((4,5,11,6),'hair','ink');rect((3,6,4,8),'hair');rect((12,6,13,8),'hair');rect((14,7,15,9),'hair','ink');rect((12,8,15,8),'straw')
        if direction=='u':rect((5,8,11,12),'hair');rect((11,8,13,10),'hair')
        poly([(5,1),(10,1),(12,3),(11,5),(5,5),(3,3)],'brown')
        rect((5,1,10,1),'woodlight');rect((4,2,6,3),'strawlight');rect((7,2,11,3),'brownlight');rect((6,0,9,0),'ink')
    else:
        # Large asymmetric pack, narrow bent torso, woven hat and walking stick.
        rect((1,7,7,19),'greendark','ink');rect((2,8,6,17),'green')
        rect((0,9,7,13),'brown','ink');rect((1,9,6,10),'straw');rect((3,7,4,18),'woodlight')
        poly([(8,15),(11,15),(13,20),(7,20)],'brown')
        rect((8,14,9,17),'brownlight');rect((7,18,12,19),'wood')
        rect((7,21-(step>0),8,23-(step>0)),'wood','ink');rect((11,21-(step<0),13,23-(step<0)),'wood','ink')
        face((5,8,14,15))
        rect((8,7,14,8),'hair')
        if direction!='u':
            rect((direction=='l' and 8 or 12,12,direction=='l' and 9 or 13,13),'wood')
            d.point((10,11),fill=c('brown'))
        poly([(4,6),(7,3),(10,2),(13,4),(15,6),(15,7),(4,7)],'straw')
        d.line((7,5,12,3),fill=c('strawlight'));d.line((5,6,14,6),fill=c('woodlight'))
        rect((14,16,14,23),'wood');rect((12,16,14,17),'skin','ink')
    # Side direction uses a narrower visible eye layout; every frame stays on the same 16x24 baseline.
    return prepare(im,(16,24),load_palette(ROOT/'Tools/ArtGen/current_palette_candidates.txt'))[0]


def generate():
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'frames').mkdir(exist_ok=True)
    all_frames={}
    for name in ('naru_chief','woman','trader'):
        sheet=Image.new('RGBA',(64,96));frames=[]
        for row,direction in enumerate('dulr'):
            for frame in range(4):
                im=render(name,direction,frame);im.save(OUT/'frames'/f'{name}_{direction}{frame}.png');sheet.alpha_composite(im,(frame*16,row*24));frames.append(im)
        sheet.save(OUT/f'{name}-sheet.png');all_frames[name]=frames
    return all_frames


if __name__=='__main__':generate()
