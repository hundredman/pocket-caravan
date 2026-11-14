"""11/14 은샘: 고려 짐말(과하마) 그림 1차. 왼쪽을 보는 옆모습 한 장 + 짐 단계 0~3 (안장 천·길마 나무틀 → 자루 1 → 자루 2·보따리 → 보따리 2단·항아리).
손수레(handcart_v3.py)와 같은 짐 색. 실행: python Tools/ArtGen/packhorse_v1.py <출력 폴더>
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
if __name__=='__main__':
    out=sys.argv[1]
    ims=[make(i) for i in range(4)]
    for i,im in enumerate(ims): im.save(f'{out}/side_{i}.png')
    W=sum(i.width+8 for i in ims)+8;H=ims[0].height+8
    sheet=Image.new('RGBA',(W,H),(120,160,100,255));x=8
    for im in ims: sheet.alpha_composite(im,(x,4)); x+=im.width+8
    sheet.resize((W*6,H*6),Image.NEAREST).save(f'{out}/sheet.png')
