"""Build native-scale and nearest enlarged review boards; no runtime asset writes."""
from pathlib import Path
import json,subprocess
from PIL import Image,ImageDraw,ImageFont
from npc_sample import ROOT,OUT,generate
FONT='/System/Library/Fonts/AppleSDGothicNeo.ttc'
def font(size):return ImageFont.truetype(FONT,size)
def label(draw,pos,text,size=17,color='#332d2a'):draw.text(pos,text,font=font(size),fill=color)
def paste(canvas,image,xy,scale=1):
    im=image.resize((image.width*scale,image.height*scale),Image.Resampling.NEAREST)
    canvas.paste(im,xy,im)

def review():
    frames=generate();old_dir=ROOT/'Assets/PocketCaravan/Art/Characters'
    names=[('naru_chief','나루 촌장 · 갓 / 수염 / 지팡이','elder'),('woman','아낙 · 물동이 / 쪽머리 / 치마','villager_rabbit'),('trader','행상 · 패랭이 / 큰 지게 짐','trader')]
    board=Image.new('RGB',(960,450),'#f5f0e6');d=ImageDraw.Draw(board)
    label(d,(22,12),'C1 사람 NPC 3명 · 전/후 같은 배율 · 승인 전 시안',23)
    for i,(name,title,old) in enumerate(names):
        x=20+i*315;label(d,(x,52),title,16);label(d,(x+28,81),'전',16);label(d,(x+160,81),'후',16)
        original=Image.open(old_dir/f'{old}_tall_d0.png').convert('RGBA');new=frames[name][0]
        for xx,im in ((x+14,original),(x+150,new)):
            d.rectangle((xx-5,105,xx+117,282),fill='#ddd7c8');paste(board,im,(xx,110),7)
        label(d,(x+26,299),'실루엣',15)
        silhouette=Image.new('RGBA',(16,24),'#221a22');silhouette.putalpha(new.getchannel('A'));paste(board,silhouette,(x+150,297),4)
        paste(board,Image.open(old_dir/'tanuki_tall_d0.png').convert('RGBA'),(x+37,330),3)
        label(d,(x+17,408),'플레이어 기준',13)
    board.save(OUT/'comparison.png')
    # Both panels are genuine 480x270 source-pixel canvases; candidates are composited, not installed in Unity.
    base=Image.open(ROOT/'Docs/Images/GoryeoArt/capital-polish-day.png').convert('RGBA').resize((480,270),Image.Resampling.NEAREST)
    before=base.copy();after=base.copy();points=[(164,180),(275,182),(329,185)]
    for (name,title,old),point in zip(names,points):
        before.alpha_composite(Image.open(old_dir/f'{old}_tall_d0.png').convert('RGBA'),point)
        after.alpha_composite(frames[name][0],point)
    before.save(OUT/'game-before-480x270.png');after.save(OUT/'game-after-480x270.png')
    screen=Image.new('RGB',(1000,345),'#f5f0e6');sd=ImageDraw.Draw(screen)
    label(sd,(14,9),'게임 화면 크기 비교 · 각 480×270 · 기존 캡처에 시안 합성 (본편 미반영)',20)
    label(sd,(14,40),'전 · 기존 동물 NPC',16);label(sd,(510,40),'후 · 사람 NPC 후보',16)
    screen.paste(before.convert('RGB'),(12,66));screen.paste(after.convert('RGB'),(508,66));screen.save(OUT/'game-comparison.png')
    # All four directions, all four walk phases; ordered dulr.
    sheetboard=Image.new('RGB',(930,450),'#ddd7c8');sd=ImageDraw.Draw(sheetboard)
    label(sd,(16,8),'4방향 × 4프레임 · dulr · 같은 발밑 기준',22)
    for i,(name,title,_) in enumerate(names):
        label(sd,(16+i*306,45),title,15);paste(sheetboard,Image.open(OUT/f'{name}-sheet.png').convert('RGBA'),(24+i*306,63),4)
    sheetboard.save(OUT/'directions.png')
    walk=[]
    for f in range(4):
        image=Image.new('RGB',(270,170),'#ddd7c8')
        for i,(name,_,_) in enumerate(names):paste(image,frames[name][f],(10+i*88,10),6)
        walk.append(image)
    walk[0].save(OUT/'walk.gif',save_all=True,append_images=walk[1:],duration=170,loop=0)
    return frames
if __name__=='__main__':review()
