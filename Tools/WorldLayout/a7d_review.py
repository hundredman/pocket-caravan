"""Review contact sheets only; never alters source artwork or game textures."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];P=ROOT/'ArtReview/A7d-20261030';O=P/'Review';O.mkdir(exist_ok=True)
font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
def label(im,text,xy):ImageDraw.Draw(im).text(xy,text,font=font,fill='white')
def world(folder):
    a=Image.open(folder/'ground.png').convert('RGBA')
    for n in ['deco','obj','high']:a=Image.alpha_composite(a,Image.open(folder/(n+'.png')).convert('RGBA'))
    return a
before=world(ROOT/'ArtReview/BuildingNatureBinding-20261029/BuildingLayers');after=world(P/'Layers')
board=Image.new('RGB',(1800,820),(28,35,40));board.paste(before.resize((900,780)),(0,40));board.paste(after.resize((900,780)),(900,40));label(board,'현재 본편', (16,8));label(board,'검토본 · 수원 4곳 / 논 8구획', (916,8));board.save(O/'01-world-before-after.png')
after.save(O/'world-full.png')
def grid(filename,rows):
    board=Image.new('RGB',(960,600),(28,35,40))
    for i,(title,path)in enumerate(rows):
        x=(i%2)*480;y=(i//2)*300;label(board,title,(x+8,y+3));board.paste(Image.open(path).convert('RGB'),(x,y+30))
    board.save(O/filename)
grid('02-sources-unity.png',[(title,P/'Unity'/('asset-'+key+'.png'))for title,key in [('서쪽 수원','west'),('북동 수원','capital'),('송악 북쪽 수원','goryeo'),('호수 수원 · 승인 큰 폭포 유지','lake')]])
grid('03-rice-unity.png',[(title,P/'Unity'/('asset-'+key+'.png'))for title,key in [('장단 강변 논','granary-north'),('개경 남쪽 논','capital-fields'),('송악 평지 논','songak-fields'),('나루 북쪽 논','naru-fields')]])
board=Image.new('RGB',(960,900),(28,35,40))
for row,kind in enumerate(['hud','map','market']):
    for col,stage in enumerate(['Before','After']):
        label(board,('이전'if col==0 else'수정')+' · '+dict(hud='HUD',map='지도 이름',market='장터')[kind],(col*480+8,row*300+3))
        board.paste(Image.open(P/'UI'/stage/('480x270-'+kind+'.png')),(col*480,row*300+30))
board.save(O/'04-ui-before-after.png')
print('Review boards:',O)
