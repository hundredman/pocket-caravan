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
# 11/14 v3: LPC horses·OGA horse(opengameart) 4방향 그림을 참고. 위에서 비스듬히 본 말은 폭이 좁고(옆 길이의 1/3쯤)
# 위아래로 길다(옆모습 키의 1.3배쯤). 뒤: 귀·뒤통수 → 갈기 있는 목 → 안장 → 넓은 엉덩이 → 꼬리 → 뒷다리.
# 앞: 꼬리 끝 → 엉덩이·등·안장 → 그 앞에 귀·이마·얼굴(아래를 봄) → 양옆 어깨 아래 앞다리. 몸 폭 13, 캔버스 21 x 42.
HORSE_BACK=[
"....M...M....",
"....BM.MB....",
"....BBMBB....",
"....bBMBb....",
"....bBMBb....",
"...bBBMBBb...",
"...bBBMBBb...",
"..bBBBMBBBb..",
"..bBBBBBBBb..",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bLBBBBBBBLb.",
"bLBBBBBBBBBLb",
"bBBBBBBBBBBBb",
"bBBBBBMBBBBBb",
"bBBBBMMMBBBBb",
".bBBBMMMBBBb.",
".bBBbMMMbBBb.",
"..bBbMMMbBb..",
"..bBb.MM.bBb.",
"..bBb.MM.bBb.",
"..bBb.M..bBb.",
"..bBb....bBb.",
"..bBb....bBb.",
"..kkk....kkk.",
]
HORSE_FRONT=[
"......M......",
"......M......",
".....MMM.....",
"..bBBBBBBBb..",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBM.....MBb.",
".bBMB...BMBb.",
".bBBBBMBBBBb.",
".bBBBBMBBBBb.",
".bBBeBBBeBBb.",
".bbBBBBBBBbb.",
".bBbBBBBBbBb.",
".bBbBBBBBbBb.",
".bBbcccccbBb.",
".bBbceccebBb.",
".bBb.ccc.bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".kkk.....kkk.",
]
CAMEL_BACK=[
"....t...t....",
"....tTTTt....",
"....tTTTt....",
".....tTt.....",
".....tTt.....",
".....tTt.....",
"....tTTTt....",
"...tTTTTTt...",
"..tTTTTTTTt..",
".tTTUUUUTTTt.",
".tTUUUUUUTTt.",
".tTUUUUUUTTt.",
".tTTUUUUTTTt.",
".tTTTTTTTTTt.",
".tTTTTTTTTTt.",
"tUTTTTTTTTTUt",
"tTTTTTTTTTTTt",
"tTTTTTtTTTTTt",
".tTTTTtTTTTt.",
".tTTTttTTTTt.",
"..tTt.t.tTt..",
"..tTt.k.tTt..",
"..tTt...tTt..",
"..tTt...tTt..",
"..UTU...UTU..",
"..tTt...tTt..",
"..tTt...tTt..",
"..tTt...tTt..",
"..tTt...tTt..",
"..kkk...kkk..",
]
CAMEL_FRONT=[
"......t......",
"......k......",
"..tTTTTTTTt..",
".tTTTTTTTTTt.",
".tTTUUUUTTTt.",
".tTUUUUUUTTt.",
".tTUUUUUUTTt.",
".tTTUUUUTTTt.",
".tTTTTTTTTTt.",
".tTTtTTTtTTt.",
".tTt.tTt.tTt.",
"..t..tTt..t..",
".....tTt.....",
"...t.....t...",
"...tTTTTTt...",
"...TeTTTeT...",
"...TTTTTTT...",
"...tTTTTTt...",
"....ccccc....",
".....ccc.....",
"..tTt...tTt..",
"..tTt...tTt..",
"..tTt...tTt..",
"..UTU...UTU..",
"..tTt...tTt..",
"..tTt...tTt..",
"..tTt...tTt..",
"..tTt...tTt..",
"..kkk...kkk..",
]
VIEW={  # 그림, 안장 띠 줄(시작), 띠 색, 짐(자루·더미) 기준 줄
 ('horse','back'):(HORSE_BACK,8,'rRr',8),
 ('horse','front'):(HORSE_FRONT,6,'rRr',6),
 ('camel','back'):(CAMEL_BACK,8,'aAy',8),
 ('camel','front'):(CAMEL_FRONT,3,'aAy',3),
}
def end_view3(kind,view,stage):
    from PIL import Image
    rows,band_y,band,pack_y=VIEW[(kind,view)]
    W,H=21,44;ox=4;oy=H-2-len(rows)
    for r in rows:assert len(r)==13,(kind,view,r)
    layers=[]
    def layer(fn):
        L=Cv(W,H);fn(L);L.outline();layers.append(L.im)
    def body(L):
        L.draw(rows,ox,oy)
        for i,ch in enumerate(band):            # 안장 천: 몸 위에만
            y=oy+band_y+i
            for x in range(W):
                if L.get(x,y)[3] and L.get(x,y)[:3] not in (P['M'],P['e'],P['k'],P['c']):L.px(x,y,ch)
    layer(body)
    if view=='front':                           # 앞모습은 얼굴이 안장 앞에 오도록 얼굴 부분을 다시 위에 그린다
        face=[r if oy+band_y<=0 else r for r in rows]
    if stage>=1:
        layer(lambda L:L.draw(SACK,ox-4,oy+pack_y));layer(lambda L:L.draw(SACK,ox+11,oy+pack_y))
    if stage>=2:layer(lambda L:L.draw(BUNDLE,ox+1,oy+pack_y-1))
    if stage>=3:layer(lambda L:L.draw(SMALL,ox+2,oy+pack_y-5))
    out=Image.new('RGBA',(W,H),(0,0,0,0))
    for im in layers:out.alpha_composite(im)
    return out
def horse_front(stage):return end_view3('horse','front',stage)
def horse_back(stage):return end_view3('horse','back',stage)
def camel_front(stage):return end_view3('camel','front',stage)
def camel_back(stage):return end_view3('camel','back',stage)

if __name__=='__main__':
    import os
    out=sys.argv[1]
    for folder,views in (('PackHorse',(('side',make),('front',horse_front),('back',horse_back))),('Camel',(('side',make_camel),('front',camel_front),('back',camel_back)))):
        os.makedirs(f'{out}/{folder}',exist_ok=True)
        for view,fn in views:
            for i in range(4): fn(i).save(f'{out}/{folder}/{view}_{i}.png')
