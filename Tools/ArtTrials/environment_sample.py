"""C2/C3 whole-object redraw trials, same 64x48 canvas; production stays untouched."""
from pathlib import Path
import sys,json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtPrep'))
from prepare_art import prepare,load_palette
OUT=ROOT/'ArtReview/C2-C3'
P=dict(ink='#2c1a12',roofdark='#141f2e',roof='#2c3947',rooflight='#4a5662',wooddark='#51301e',wood='#835e3f',woodlight='#b27a52',plaster='#d8c2a2',light='#f0eadc',stone='#ac9c89',stoneshadow='#5d5d58',door='#503c2d',barklight='#ae8d6d',leafdark='#394b28',leaf='#658555',leaflight='#729362')

def house():
    im=Image.new('RGBA',(64,48));d=ImageDraw.Draw(im)
    def rect(box,color,outline=None):d.rectangle(box,fill=P[color],outline=P[outline] if outline else None)
    def poly(points,color,outline='ink'):d.polygon(points,fill=P[color]);d.line(points+[points[0]],fill=P[outline])
    # Existing house geometry: two wings, central recessed door, upturned tiled eaves.
    poly([(2,27),(58,27),(60,46),(2,46)],'wooddark')
    rect((4,29,16,40),'plaster');rect((44,29,57,40),'plaster')
    rect((7,30,13,39),'door','wood');rect((47,30,54,39),'door','wood')
    for x in (8,11,48,51):rect((x,31,x,37),'woodlight')
    rect((5,29,15,29),'light');rect((45,29,56,29),'light')
    # Few broad beams rather than fine wood grain.
    for x in (3,16,20,39,43,57):rect((x,26,x+1,41),'wood');rect((x,27,x,38),'woodlight')
    rect((22,24,37,36),'wood','ink');rect((24,25,35,30),'woodlight')
    rect((28,25,30,34),'door');rect((25,27,26,29),'plaster');rect((32,27,33,29),'plaster')
    rect((21,35,39,38),'woodlight','wooddark');rect((18,39,42,41),'wood','ink')
    # Three restrained roof tones and six large ribs.
    poly([(10,1),(13,3),(47,3),(50,1),(51,7),(57,13),(60,22),(60,28),(47,25),(39,21),(21,21),(15,25),(0,28),(2,20),(6,13),(9,7)],'roofdark')
    poly([(11,4),(48,4),(51,13),(57,23),(46,21),(38,20),(22,20),(15,21),(4,24),(9,12)],'roof',outline='roof')
    d.line((12,4,47,4),fill=P['rooflight'],width=2)
    for x in (13,19,25,31,37,43,49):
        endpoint=x+(2 if x>31 else -2)
        d.line((x,7,endpoint,20),fill=P['rooflight']);d.line((x+1,7,endpoint+1,20),fill=P['roofdark'])
    d.line((1,26,15,23,22,21,38,21,47,23,59,26),fill=P['rooflight'])
    rect((10,2,11,6),'rooflight');rect((49,2,50,6),'rooflight')
    # Stairs and foot baseline match the original whole-object footprint.
    rect((3,42,16,46),'stone','stoneshadow');rect((44,42,59,46),'stone','stoneshadow')
    rect((4,42,15,42),'light');rect((45,42,58,42),'light')
    rect((22,41,38,43),'stone','stoneshadow');rect((20,44,40,46),'plaster','stoneshadow');rect((21,44,39,44),'light')
    return im


def pine():
    im=Image.new('RGBA',(64,48));d=ImageDraw.Draw(im)
    def poly(points,color,outline='ink'):d.polygon(points,fill=P[color]);d.line(points+[points[0]],fill=P[outline])
    # Crooked mature trunk, four long boughs, exposed roots: keep the original pose.
    poly([(27,46),(30,38),(29,32),(34,26),(37,19),(36,12),(31,8),(33,6),(40,11),(43,20),(40,30),(36,37),(39,41),(45,46),(38,44),(34,40),(32,46)],'wood')
    poly([(30,33),(34,27),(38,20),(39,13),(41,20),(37,31),(33,37),(32,43),(29,46)],'woodlight',outline='woodlight')
    d.line((31,34,34,29,39,20),fill=P['barklight'],width=2)
    poly([(34,40),(23,44),(21,45),(25,41),(29,39)],'wooddark')
    poly([(36,36),(43,39),(46,44),(41,41),(35,40)],'wooddark')
    poly([(37,23),(30,24),(23,23),(19,25),(18,23),(26,20),(36,20)],'wooddark')
    poly([(40,19),(49,18),(53,21),(52,23),(48,21),(41,23)],'wooddark')
    poly([(38,29),(46,30),(50,34),(47,35),(43,32),(37,31)],'wooddark')
    # Large clean leaf masses, each with three tones; no random dithering.
    clusters=[
      [(22,9),(24,5),(28,4),(28,1),(33,2),(36,4),(39,4),(42,8),(40,11),(31,10),(26,12),(18,12)],
      [(18,17),(22,14),(27,14),(30,12),(34,14),(36,18),(33,21),(23,20),(18,22),(15,21)],
      [(8,26),(12,23),(16,22),(18,19),(24,20),(29,22),(34,23),(35,26),(29,29),(22,27),(16,29),(11,28)],
      [(43,17),(46,15),(50,16),(52,17),(54,20),(52,22),(45,21),(41,21)],
      [(45,29),(48,28),(52,31),(53,34),(49,35),(44,32),(42,31)],
    ]
    for pts in clusters:
        poly(pts,'leafdark',outline='leafdark')
        x0=min(x for x,y in pts);y0=min(y for x,y in pts);x1=max(x for x,y in pts);y1=max(y for x,y in pts)
        # Broad upper planes leave a continuous dark lower edge.
        middle=[(x0+2,(y0+y1)//2),(x0+5,y0+2),(x1-5,y0+2),(x1-1,y1-3),(x1-5,y1-2),(x0+3,y1-2)]
        poly(middle,'leaf',outline='leaf')
        d.line((x0+5,(y0+y1)//2-1,x1-5,(y0+y1)//2-1),fill=P['leaflight'],width=2)
    return im


def generate():
    OUT.mkdir(parents=True,exist_ok=True);palette=load_palette(ROOT/'Tools/ArtGen/current_palette_candidates.txt')
    for name,fn in [('house',house),('tree',pine)]:
        image,report=prepare(fn(),(64,48),palette);image.save(OUT/f'{name}-after.png');(OUT/f'{name}-prep.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    # Legacy rendering cells are assembled only to show the existing complete object before the change.
    tiles=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    for name,key in [('house','goryeo_house_4_3'),('tree','goryeo_woodland0')]:
        im=Image.new('RGBA',(64,48))
        for y in range(3):
            for x in range(4):
                with Image.open(tiles/f'{key}_{x}_{y}.png') as tile:im.alpha_composite(tile.convert('RGBA'),(x*16,y*16))
        im.save(OUT/f'{name}-before.png')
    font=lambda size:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',size)
    board=Image.new('RGB',(1040,520),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((22,12),'C2 · C3 시험 · 같은 64×48 크기, 전/후 같은 배율 · 승인 전 시안',font=font(22),fill='#332d2a')
    for row,(name,label) in enumerate([('house','기와집'),('tree','소나무')]):
        y=58+row*230;d.text((22,y),label,font=font(18),fill='#332d2a')
        for col,phase in enumerate(('before','after')):
            x=180+col*410;im=Image.open(OUT/f'{name}-{phase}.png').convert('RGBA');d.text((x,y),'전' if col==0 else '후',font=font(18),fill='#332d2a')
            d.rectangle((x,y+26,x+319,y+217),fill='#bfc4ac');large=im.resize((320,192),Image.Resampling.NEAREST);board.paste(large,(x,y+26),large)
    board.save(OUT/'comparison.png')
    # Same scene background and unchanged player at 480x270; whole objects added for review.
    base=Image.open(ROOT/'Docs/Images/GoryeoArt/capital-polish-day.png').convert('RGBA').resize((480,270),Image.Resampling.NEAREST)
    before=base.copy();after=base.copy()
    for name,point in [('house',(135,175)),('tree',(292,175))]:
        before.alpha_composite(Image.open(OUT/f'{name}-before.png').convert('RGBA'),point)
        after.alpha_composite(Image.open(OUT/f'{name}-after.png').convert('RGBA'),point)
    before.save(OUT/'game-before-480x270.png');after.save(OUT/'game-after-480x270.png')
    review=Image.new('RGB',(1000,345),'#f5f0e6');rd=ImageDraw.Draw(review)
    rd.text((14,9),'게임 화면 크기 비교 · 각 480×270 · 기존 캡처에 전체 개체 합성 (본편 미반영)',font=font(19),fill='#332d2a')
    for x,im,text in [(12,before,'전 · 기존 개체'),(508,after,'후 · 단순화 후보')]:
        rd.text((x+2,40),text,font=font(16),fill='#332d2a');review.paste(im.convert('RGB'),(x,66))
    review.save(OUT/'game-comparison.png')

if __name__=='__main__':generate()
