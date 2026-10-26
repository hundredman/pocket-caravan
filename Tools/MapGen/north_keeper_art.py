"""10/27 장터 장꾼 넷 (사용자 선택: 북유럽 사람 얼굴 — 밝은 살빛·금발/적갈색 머리·파란 눈·턱수염).
마을 사람 그림(Art/Characters/north_p_*_tall_d0)을 바탕으로 살빛·머리색을 바꾸고 모자·앞치마·들고 있는 물건을 덧그린다.
d0 = 가만히, d1 = 물건을 가슴 높이로 들어 보임. sprites()가 게임용 묶음 (Resources/North/Minigame/keeper_*)."""
from PIL import Image, ImageDraw
import os
CH=os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'PocketCaravan', 'Art', 'Characters') + os.sep
O=(46,34,32,255)
def base(n): return Image.open(CH+n+'_tall_d0.png').convert('RGBA')
def P(im,x,y,c):
    if 0<=x<16 and 0<=y<24: im.putpixel((x,y),c)
def H(s): s=s.lstrip('#'); return tuple(int(s[i:i+2],16) for i in (0,2,4))+(255,)
def paint(im,rows,pal,x0,y0):
    for j,r in enumerate(rows):
        for i,ch in enumerate(r):
            if ch!='.': P(im,x0+i,y0+j,pal[ch] if ch in pal else O)
def recolor(im,mp):
    px=im.load()
    for y in range(24):
        for x in range(16):
            c=px[x,y]
            if c[3] and c[:3] in mp: px[x,y]=mp[c[:3]]+(255,)
    return im
SKIN=(246,214,190); SKIN_D=(216,176,150); EYE=(44,62,112)
def nordic(im, mp, beard=None):
    """10/27 북유럽 사람: 밝은 살빛, 금발·적갈색 머리, 파란 눈, 볼 홍조. beard=(밝은색, 어두운색)이면 턱수염"""
    m=dict(mp); m[(34,26,34)]=EYE
    recolor(im,m)
    if beard:
        b,bd=beard
        for (x,y) in [(5,14),(10,14),(5,15),(6,15),(9,15),(10,15),(6,16),(7,16),(8,16),(9,16)]: P(im,x,y,b+(255,))
        for (x,y) in [(4,14),(11,14),(5,16),(10,16),(7,17),(8,17)]:
            if im.getpixel((x,y))[3]: P(im,x,y,bd+(255,))
    return im
def apron(im,col,dark,y0=17,y1=20):
    for y in range(y0,y1+1):
        for x in range(5,11): P(im,x,y,col)
        P(im,4,y,dark); P(im,11,y,dark)
    for x in range(5,11): P(im,x,y1,dark)
    P(im,6,y0,H('fff6e0')); 
def hand_item(im,rows,pal,raised):
    # 오른손(화면 왼쪽 아님: 캐릭터 오른손 = 화면 오른쪽 x 12~15), 들면 y 13~, 아니면 y 17~
    w=len(rows[0]); paint(im,rows,pal,8-w//2,15 if raised else 17)
# 1 생선 장수: 파란 모자(그대로) + 흰 앞치마 + 은빛 대구
def fishmonger(raised):
    im=nordic(base('north_p_fisher'),{(150,104,66):(214,120,64),(186,140,96):(236,160,96),(122,85,54):(170,88,46),(240,200,164):SKIN,(196,164,134):SKIN_D},beard=((214,120,64),(150,74,40))); apron(im,H('f4f1ea'),H('b9b4a8'))
    hand_item(im,["..o.o","oSSSo","ooSSSo",".oooo"][:3] if False else ["..oooooo.o",".oSSsSSSoTo","okSSSSSSSTo","oLLLLLLLoTo",".oooooo..o"],{'S':H('9fb0ac'),'s':H('e6ece8'),'k':H('101010'),'L':H('e4e6d8'),'T':H('7f8f88')},raised)
    return im
# 2 모피 장수: 털모자(갈색 + 흰 테) + 어깨 털망토 + 팔에 걸친 여우 가죽
def furrier(raised):
    im=nordic(base('north_p_trader'),{(168,116,80):SKIN,(137,95,65):SKIN_D,(36,30,32):(232,196,110),(20,15,19):(150,108,52)},beard=((232,196,110),(176,136,70)))
    paint(im,["...oooooooooo...","..oBBBBBBBBBBo..",".oBbBBBBBBBBBBo.",".oWWWWWWWWWWWWo."],{'B':H('8a5a34'),'b':H('b07a4a'),'W':H('f2ece0')},0,5)
    for x in range(3,13): P(im,x,17,H('8a5a34'))
    for x in (2,13): P(im,x,17,H('5e3a22'))
    hand_item(im,["oo.....oo","oFoooooFo","oFFFFFFFo","oFFfFFFFo",".oFFFFFo.","..oooooTT"],{'F':H('e07a2a'),'f':H('f2a860'),'T':H('f7f2ea')},raised)
    return im
# 3 꿀술 아주머니: 빨간 물방울 두건 + 초록 앞치마 + 꿀술 단지
def meadwife(raised):
    im=nordic(base('north_p_girl'),{(196,146,108):SKIN,(160,119,88):SKIN_D,(46,36,40):(238,204,120),(23,17,22):(184,136,64),(37,29,32):(214,176,96),(27,21,28):(150,108,52),(86,70,74):(250,226,160)})
    paint(im,["....oooooooo....","..ooRRRRRRRRoo..",".oRRwRRRRwRRRRo.",".oRRRRRwRRRRRRo."],{'R':H('c83c30'),'w':H('fff2e8')},0,6)
    apron(im,H('5f9a4a'),H('3f6a32'))
    hand_item(im,["..oCo..",".oJJJo.","oJYYJJo","oJYyYJo","oJJJJJo",".ooooo."],{'C':H('c9a06a'),'J':H('b8743a'),'Y':H('f2c84b'),'y':H('fde79a')},raised)
    return im
# 4 소금 장수: 넓은 밀짚모자 + 등에 흰 소금 자루
def saltman(raised):
    im=nordic(base('north_p_oldman'),{(240,200,164):SKIN})
    paint(im,[".....oooooo.....","....oSSSSSSo....","oooooSSSSSSooooo","oSSSSSSSSSSSSSSo","oooooooooooooooo"],{'S':H('e8c46a')},0,5)
    hand_item(im,["..o.o..",".oBBBo.","oWWWWWo","oWWwWWo","oWWWWWo",".ooooo."],{'W':H('f4f2ee'),'w':H('c8ccd6'),'B':H('4a6fb0')},raised)
    return im

def sprites():
    out = {}
    for key, f in (("keeper_fish", fishmonger), ("keeper_fur", furrier), ("keeper_mead", meadwife), ("keeper_salt", saltman)):
        out[key + "_d0"] = f(False)
        out[key + "_d1"] = f(True)
    return out
