"""11/14 은샘: 짐 싣는 짐승 그림 (짐말·낙타). 왼쪽을 보는 옆모습 한 장 + 짐 단계 0~3
(안장 → 자루 1 → 자루 2·보따리 → 보따리 2단·항아리). 짐 색은 손수레(handcart_v3.py)와 같다.
짐말: 과하마처럼 작고 다부진 말, 붉은 안장 천과 나무 길마. 낙타: 단봉낙타, 파란 줄무늬 안장 천. 낙타가 말보다 키가 크다.
실행: python Tools/ArtGen/pack_animals_v1.py <출력 폴더>  → <출력>/PackHorse/side_0~3.png, <출력>/Camel/side_0~3.png
"""
import sys
from PIL import Image
P=dict(O=(46,36,29),B=(150,94,56),b=(116,70,42),L=(181,124,78),M=(62,44,34),m=(88,62,46),k=(52,48,44),
       c=(212,176,132),e=(30,22,18),
       w=(104,69,45),h=(183,124,62),H=(193,139,82),d=(91,67,47),
       C=(242,223,179),X=(246,228,191),s=(221,198,150),n=(196,150,88),r=(150,58,52),R=(176,78,66),
       j=(84,126,106),J=(109,148,130),K=(134,162,139))
HORSE=[
"......MM.............................",
".....MBBM............................",
"....MBBBBM...........................",
"...LBBBBBBMM.........................",
"..LBBeBBBBBMM........................",
".LBBBBBBBBBBMM.......................",
"LBBBBBBBBBBBBMM......................",
"LcBBBBbBBBBBBBMM.....................",
"ccBBBb.bBBBBBBBMM....................",
"ccc.....bBBBBBBBBMLLLLLLLLLLLLL......",
"........bBBBBBBBBBBBBBBBBBBBBBBBM....",
".........BBBBBBBBBBBBBBBBBBBBBBBBMM..",
".........BBBBBBBBBBBBBBBBBBBBBBBBBMM.",
".........BBBBBBBBBBBBBBBBBBBBBBBBBmM.",
".........bBBBBBBBBBBBBBBBBBBBBBBBbmMM",
".........bbBBBBBBBBBBBBBBBBBBBBBbbmmM",
"..........bbbbbbbbbbbbbbbbbbbbbbbb.mM",
"...........bBBb.mmm.....mmm.bBBb...mM",
"...........bBBb.mmm.....mmm.bBBb...mM",
"...........bBBb.mmm.....mmm.bBBb....M",
"...........bBBb.mmm.....mmm.bBBb.....",
"...........bbbb.mmm.....mmm.bbbb.....",
"...........kkkk.kkk.....kkk.kkkk.....",
]
class Cv:
    def __init__(s,w,h):s.im=Image.new('RGBA',(w,h),(0,0,0,0));s.p=s.im.load();s.w,s.h=w,h
    def px(s,x,y,k):
        if 0<=x<s.w and 0<=y<s.h:s.p[x,y]=P[k]+(255,)
    def get(s,x,y):return s.p[x,y] if 0<=x<s.w and 0<=y<s.h else (0,0,0,0)
    def hline(s,x0,x1,y,k):
        for x in range(x0,x1+1):s.px(x,y,k)
    def vline(s,x,y0,y1,k):
        for y in range(y0,y1+1):s.px(x,y,k)
    def rect(s,x0,y0,x1,y1,k):
        for y in range(y0,y1+1):s.hline(x0,x1,y,k)
    def draw(s,rows,ox,oy):
        for y,r in enumerate(rows):
            for x,ch in enumerate(r):
                if ch!='.':s.px(ox+x,oy+y,ch)
    def outline(s):
        o=P['O']+(255,)
        pts=[(x,y) for y in range(s.h) for x in range(s.w) if s.get(x,y)[3]==0 and any(s.get(x+a,y+b)[3] and s.get(x+a,y+b)!=o for a,b in((1,0),(-1,0),(0,1),(0,-1)))]
        for x,y in pts:s.p[x,y]=o
# 길마(나무 안장틀)·안장 천과 짐
CLOTH=["rrrrrrrrrrrrrrr","RRRRRRRRRRRRRRR","RRRRRRRRRRRRRRR","rrrrrrrrrrrrrrr"]
FRAME=["dHd.....dHd","dhHdddddHhd","ddddddddddd"]
SACK=[".s..s.",".nnnn.","XCCCCs","CCCCCs","CCCCCs","CCCCss",".ssss."]
JAR=[".JJ.","KJJj","KJJj",".jj."]
BUNDLE=["..s....s..",".CnnnnnnC.","XCCCCCCCCs","CCCCCCCCss",".ssssssss."]
SMALL=["..s..s..",".CnnnnC.","XCCCCCCs",".ssssss."]
def make(stage):
    c=Cv(40,32)
    oy=8
    c.draw(HORSE,1,oy)
    c.draw(CLOTH,13,oy+9)
    c.draw(FRAME,15,oy+7)
    if stage==1: c.draw(SACK,17,oy+9)
    if stage>=2: c.draw(SACK,14,oy+9); c.draw(SACK,21,oy+9)
    if stage>=2: c.draw(BUNDLE,15,oy+3)
    if stage>=3: c.draw(SMALL,17,oy-1); c.draw(JAR,24,oy)
    c.outline();return c.im

# ---------------- 낙타 ----------------
P.update(T=(208,160,104),t=(170,124,76),U=(228,190,138),f=(140,100,62),
         a=(52,96,150),A=(78,128,182),y=(214,170,70))
CAMEL=[
"....tTT.............................",
"...UTTTt............................",
"..UTeTTTt...........................",
"UUTTTTTTTt..........................",
"cTTTTTTTTTt.........................",
".cTTt..tTTTt........................",
"........tTTTt.......................",
".........tTTt........UUUU...........",
".........tTTt......UTTTTTTt.........",
".........tTTTt....UTTTTTTTTTt.......",
".........tTTTTt..UTTTTTTTTTTTt......",
"..........tTTTTUUTTTTTTTTTTTTTTt....",
"..........tTTTTTTTTTTTTTTTTTTTTTt...",
"..........tTTTTTTTTTTTTTTTTTTTTTTt..",
"..........tTTTTTTTTTTTTTTTTTTTTTTtt.",
"...........tTTTTTTTTTTTTTTTTTTTTTt.t",
"...........ttTTTTTTTTTTTTTTTTTTTtt.t",
"............tttttttttttttttttttttt.t",
".............tTt.fff.......fff.tTt..",
".............tTt.fff.......fff.tTt..",
".............tTt.fff.......fff.tTt..",
".............tTt.fff.......fff.tTt..",
".............tTt.fff.......fff.tTt..",
".............UTU.fUf.......fUf.UTU..",
".............tTt.fff.......fff.tTt..",
".............tTt.fff.......fff.tTt..",
".............tTt.fff.......fff.tTt..",
".............kkk.kkk.......kkk.kkk..",
]
BLANKET=["..aaaaaaa..",".aAAAAAAAa.","rrrrrrrrrrr","aAAAAAAAAAa","yayayayayay"]
def make_camel(stage):
    c=Cv(40,40); oy=10
    c.draw(CAMEL,1,oy)
    c.draw(BLANKET,16,oy+8)
    if stage==1: c.draw(SACK,19,oy+11)
    if stage>=2: c.draw(SACK,15,oy+11); c.draw(SACK,23,oy+11)
    if stage>=2: c.draw(BUNDLE,16,oy+3)
    if stage>=3: c.draw(SMALL,17,oy-1); c.draw(JAR,24,oy)
    c.outline(); return c.im


# ---------------- 앞(아래로 올 때)·뒤(위로 갈 때) ----------------
# 옆모습과 같은 작은 몸 비율. 앞: 머리가 가장 앞(가깝게 크게), 머리 양옆으로 어깨·자루, 짐 더미는 머리 너머 등 위로 보인다.
# 뒤: 엉덩이·꼬리가 앞, 등 위 안장·짐, 머리는 등 너머 귀만. 캔버스 폭 20, 발바닥이 맨 아래에서 2줄 위.
HORSE_FRONT=[
"....bM......Mb....",
"....bBM....MBb....",
"....bBBBMMBBBb....",
"....bBBMMMMBBb....",
"....bBBBMMBBBb....",
"....bLeBBBBeBb....",
"..LLbBBBBBBBBbBB..",
".LBBbBBBBBBBBbBBb.",
".LBBBbBBBBBBbBBBb.",
".LBBBbBccccBbBBBb.",
".LBBBbccccccbBBBb.",
".LBBBbceccecbBBBb.",
".bBBBBbccccbBBBBb.",
"..bBBBBbbbbBBBBb..",
"..bbBBBBBBBBBBbb..",
"...bBBBBBBBBBBb...",
"....bBb....bBb....",
"....bBb....bBb....",
"....bBb....bBb....",
"....bBb....bBb....",
"....bBb....bBb....",
"....kkk....kkk....",
]
HORSE_BACK=[
".....M......M.....",
".....MB....BM.....",
"......BBBBBB......",
"....LLMMMMMMLL....",
"..LLBBBMMMMBBBb...",
".LBBBBBBBBBBBBBb..",
".LBBBBBBBBBBBBBb..",
"LBBBBBBBBBBBBBBBb.",
"LBBBBBBBBBBBBBBBb.",
"LBBBBBBBMMBBBBBBb.",
"LBBBBBBMMMMBBBBBb.",
".bBBBBBMMMMBBBBb..",
".bBBBBbMMMMbBBBb..",
"..bBBbbMMMMbbBBb..",
"..bBBb.MMMM.bBBb..",
"..bBBb.MMMM.bBBb..",
"..bBBb..MM..bBBb..",
"..bBBb......bBBb..",
"..bBBb......bBBb..",
"..bBBb......bBBb..",
"..kkkk......kkkk..",
]
CAMEL_FRONT=[
"......t......t....",
"......tTTTTTTt....",
"......TTTTTTTT....",
"......TeTTTTeT....",
"......TTTTTTTT....",
".......TccccT.....",
".......cecccT.....",
"........tTTt......",
"...UUU..tTTt.UUU..",
"..UTTTU.tTTt.TTTt.",
".UTTTTTttTTttTTTTt",
".TTTTTTTTTTTTTTTTt",
".TTTTTTTTTTTTTTTTt",
".tTTTTTTTTTTTTTTtt",
"..tTTTTTTTTTTTTtt.",
"...tttttttttttt...",
"....tTt....tTt....",
"....tTt....tTt....",
"....tTt....tTt....",
"....tTt....tTt....",
"....UTU....UTU....",
"....tTt....tTt....",
"....tTt....tTt....",
"....tTt....tTt....",
"....tTt....tTt....",
"....kkk....kkk....",
]
CAMEL_BACK=[
"......t.....t.....",
"......tTTTTTt.....",
"......tTTTTTt.....",
".......tTTTt......",
"........tTt.......",
".....UUUUTUUUU....",
"....UTTTTTTTTTTt..",
"...UTTTTTTTTTTTTt.",
"..UTTTTTTTTTTTTTt.",
".UTTTTTTTTTTTTTTTt",
".TTTTTTTTTTTTTTTTt",
".TTTTTTTTtTTTTTTTt",
".tTTTTTTTtTTTTTTtt",
"..tTTTTTTtTTTTTtt.",
"...tttttttttttt...",
"....tTt..t.tTt....",
"....tTt..k.tTt....",
"....tTt....tTt....",
"....tTt....tTt....",
"....UTU....UTU....",
"....tTt....tTt....",
"....tTt....tTt....",
"....tTt....tTt....",
"....tTt....tTt....",
"....kkk....kkk....",
]
def end_view(rows,stage,sack_y,band_y,band,top_y,front):
    """짐 높이 자리를 위에 비워 둔 캔버스(20 x 몸+12)에 몸·안장 띠·자루·짐 더미를 그린다"""
    c=Cv(20,len(rows)+14);oy=12;ox=1
    if not front:
        c.draw(rows,ox,oy)
        for x in range(ox+2,ox+16):
            for i,k in enumerate(band):c.px(x,oy+band_y+i,k)
    if stage>=2:c.draw(BUNDLE,ox+4,oy+top_y)
    if stage>=3:c.draw(SMALL,ox+5,oy+top_y-4)
    if front:
        c.draw(rows,ox,oy)
    if stage>=1:c.draw(SACK,ox-1,oy+sack_y);c.draw(SACK,ox+13,oy+sack_y)
    c.outline();return c.im
def horse_front(stage):return end_view(HORSE_FRONT,stage,6,0,'',-4,True)
def horse_back(stage):return end_view(HORSE_BACK,stage,4,4,'rRr',-1,False)
def camel_front(stage):
    """낙타 앞: 머리가 목 위 높이 있어 등짐이 가려지므로, 혹 위 짐을 목 양옆 바구니처럼 그린다"""
    c=Cv(20,len(CAMEL_FRONT)+14);oy=12;ox=1
    if stage>=2:c.draw(SMALL,ox-1,oy+5);c.draw(SMALL,ox+11,oy+5)
    if stage>=3:c.draw(SMALL,ox,oy+1);c.draw(SMALL,ox+10,oy+1)
    c.draw(CAMEL_FRONT,ox,oy)
    for x in list(range(ox+1,ox+8))+list(range(ox+12,ox+17)):
        for i,k in enumerate('aAy'):c.px(x,oy+10+i,k)
    if stage>=1:c.draw(SACK,ox-1,oy+9);c.draw(SACK,ox+13,oy+9)
    c.outline();return c.im
def camel_back(stage):return end_view(CAMEL_BACK,stage,6,5,'aAy',1,False)

if __name__=='__main__':
    import os
    out=sys.argv[1]
    for folder,views in (('PackHorse',(('side',make),('front',horse_front),('back',horse_back))),('Camel',(('side',make_camel),('front',camel_front),('back',camel_back)))):
        os.makedirs(f'{out}/{folder}',exist_ok=True)
        for view,fn in views:
            for i in range(4): fn(i).save(f'{out}/{folder}/{view}_{i}.png')
