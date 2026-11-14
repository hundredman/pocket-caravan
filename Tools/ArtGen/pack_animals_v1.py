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
# 11/14 v2: 3/4 내려다보기라 몸통(등)이 화면 위아래로 길게 보인다 (손수레 앞·뒤 그림처럼 위에서 본 짐칸).
# 앞: 머리·가슴이 아래(가까이), 등과 짐이 그 위, 엉덩이·꼬리가 맨 위(멀리). 뒤: 반대로 머리가 위, 엉덩이·꼬리가 아래.
# 다리는 몸 아래로 짧게만 보인다. 자루는 양옆구리, 짐 더미는 등 위 가운데. 캔버스 20 x 44, 발바닥이 맨 아래에서 2줄 위.
HEAD_FRONT={'horse':[
"BM....MB",
"BBM..MBB",
"LBBMMBBb",
"LBBMMBBb",
"LeBBBBeb",
".LBBBBb.",
".LBBBBb.",
"..cccc..",
"..cccc..",
"..ecce..",
"...cc..."],'camel':[
".t....t.",
".tTTTTt.",
".TTTTTT.",
".TeTTeT.",
".TTTTTT.",
"..TTTT..",
"..cccc..",
"..ecce..",
"..cccc.."]}
HEAD_BACK={'horse':[
"M....M",
"BM..MB",
"BBMMBB",
".BMMB.",
".BMMB.",
"..MM.."],'camel':[
"t....t",
"tTTTTt",
"TTTTTT",
".TTTT.",
"..TT..",
"..TT.."]}
KIND={'horse':dict(base='B',light='L',dark='b',leg='B',legd='b',tail='M',band='rRr',legs=5,neck=3,hump=False),
      'camel':dict(base='T',light='U',dark='t',leg='T',legd='t',tail='t',band='aAy',legs=8,neck=6,hump=True)}
def body_mask(c,cx,cy,rx,ry,k):
    pts=[]
    for y in range(int(cy-ry)-1,int(cy+ry)+2):
        for x in range(int(cx-rx)-1,int(cx+rx)+2):
            if ((x+.5-cx)/rx)**2+((y+.5-cy)/ry)**2<=1:pts.append((x,y))
    for x,y in pts:
        dx=(x+.5-cx)/rx
        c.px(x,y,k['light'] if dx<-.45 else k['dark'] if dx>.55 else k['base'])
    return set(pts)
def end_view2(kind,stage,front):
    """부위마다 따로 그려 테두리를 두르고 겹친다 (머리·자루·짐이 몸통과 섞이지 않게)"""
    from PIL import Image
    k=KIND[kind];W,H=20,44;cx=10;ground=41
    legtop=ground-k['legs'];ry=8.5
    cy=(legtop-ry+1-(k['neck']-1)) if front else (legtop-ry+3)
    layers=[]
    def layer(fn):
        L=Cv(W,H);fn(L);L.outline();layers.append(L.im)
    def legs(L):
        for x in (5,13):
            L.vline(x,legtop,ground-1,k['leg']);L.vline(x+1,legtop,ground-1,k['legd']);L.px(x,ground,'k');L.px(x+1,ground,'k')
            if kind=='camel':L.px(x,legtop+k['legs']//2,'U')
    layer(legs)
    if not front:
        h=HEAD_BACK[kind];layer(lambda L:L.draw(h,cx-3,int(cy-ry)-len(h)+3))
    by=int(cy)-1
    def body(L):
        m=body_mask(L,cx,cy,6.5,ry,k)
        if k['hump']:
            for x,y in m:
                if ((x+.5-cx)/3.5)**2+((y+.5-cy+1)/3.5)**2<=1:L.px(x,y,'U' if x<cx else 'T')
        for i,ch in enumerate(k['band']):
            for x in range(W):
                if (x,by+i) in m:L.px(x,by+i,ch)
        if front:
            t=int(cy-ry);L.px(cx-1,t,k['tail']);L.px(cx,t,k['tail']);L.px(cx-1,t-1,k['tail'])
        else:
            bot=int(cy+ry)
            for y in range(bot-4,bot+4):L.px(cx-1,y,k['tail']);L.px(cx,y,k['tail'])
            if kind=='horse':L.px(cx-2,bot+2,'M');L.px(cx+1,bot+2,'M');L.px(cx-2,bot+3,'M');L.px(cx+1,bot+3,'M')
    layer(body)
    if stage>=1:
        layer(lambda L:L.draw(SACK,-1,by-2));layer(lambda L:L.draw(SACK,15,by-2))
    if stage>=2:layer(lambda L:L.draw(BUNDLE,cx-5,by-3))
    if stage>=3:layer(lambda L:L.draw(SMALL,cx-4,by-6))
    if front:
        top=int(cy+ry)-2
        def head(L):
            L.rect(cx-2,top-k['neck']+3,cx+1,top+2,k['base']);L.vline(cx+1,top-k['neck']+3,top+2,k['dark'])
            L.draw(HEAD_FRONT[kind],cx-4,top+1)
        layer(head)
    out=Image.new('RGBA',(W,H),(0,0,0,0))
    for im in layers:out.alpha_composite(im)
    return out
def horse_front(stage):return end_view2('horse',stage,True)
def horse_back(stage):return end_view2('horse',stage,False)
def camel_front(stage):return end_view2('camel',stage,True)
def camel_back(stage):return end_view2('camel',stage,False)

if __name__=='__main__':
    import os
    out=sys.argv[1]
    for folder,views in (('PackHorse',(('side',make),('front',horse_front),('back',horse_back))),('Camel',(('side',make_camel),('front',camel_front),('back',camel_back)))):
        os.makedirs(f'{out}/{folder}',exist_ok=True)
        for view,fn in views:
            for i in range(4): fn(i).save(f'{out}/{folder}/{view}_{i}.png')
