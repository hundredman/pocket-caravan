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
HORSE=[   # 11/14 허리를 3칸 줄여 뒷몸을 앞으로 (짧고 다부진 과하마)
"......MM..........................",
".....MBBM.........................",
"....MBBBBM........................",
"...LBBBBBBMM......................",
"..LBBeBBBBBMM.....................",
".LBBBBBBBBBBMM....................",
"LBBBBBBBBBBBBMM...................",
"LcBBBBbBBBBBBBMM..................",
"ccBBBb.bBBBBBBBMM.................",
"ccc.....bBBBBBBBBMLLLLLLLLLL......",
"........bBBBBBBBBBBBBBBBBBBBBM....",
".........BBBBBBBBBBBBBBBBBBBBBMM..",
".........BBBBBBBBBBBBBBBBBBBBBBMM.",
".........BBBBBBBBBBBBBBBBBBBBBBmM.",
".........bBBBBBBBBBBBBBBBBBBBBbmMM",
".........bbBBBBBBBBBBBBBBBBBBbbmmM",
"..........bbbbbbbbbbbbbbbbbbbbb.mM",
"...........bBBb.mmm..mmm.bBBb...mM",
"...........bBBb.mmm..mmm.bBBb...mM",
"...........bBBb.mmm..mmm.bBBb....M",
"...........bBBb.mmm..mmm.bBBb.....",
"...........bbbb.mmm..mmm.bbbb.....",
"...........kkkk.kkk..kkk.kkkk.....",
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
CLOTH=["rrrrrrrrrrrrr","RRRRRRRRRRRRR","RRRRRRRRRRRRR","rrrrrrrrrrrrr"]
FRAME=["dHd.....dHd","dhHdddddHhd","ddddddddddd"]
SACK=[".s..s.",".nnnn.","XCCCCs","CCCCCs","CCCCCs","CCCCss",".ssss."]
JAR=[".JJ.","KJJj","KJJj",".jj."]
BUNDLE=["..s....s..",".CnnnnnnC.","XCCCCCCCCs","CCCCCCCCss",".ssssssss."]
SMALL=["..s..s..",".CnnnnC.","XCCCCCCs",".ssssss."]

# ---------------- 걸음 프레임 (11/14) ----------------
# 맨 아래 발굽 줄(k)의 다리 묶음을 왼쪽부터 센다. 옆모습 4개(가까운 앞·먼 앞·먼 뒤·가까운 뒤), 앞·뒷모습 2개.
# 'a' 프레임: 0·2번(옆) 또는 0번(앞뒤) 다리를 한 칸 든다, 'b': 1·3번 또는 1번. 걷는 순서는 기본 → a → 기본 → b.
def lift(rows,frame):
    if not frame:return rows
    rows=[list(r) for r in rows]
    last=len(rows)-1
    groups=[];x=0;w=len(rows[last])
    while x<w:
        if rows[last][x]=='k':
            x0=x
            while x<w and rows[last][x]=='k':x+=1
            groups.append((x0,x-1))
        else:x+=1
    pick=([0,2] if frame=='a' else [1,3]) if len(groups)>=4 else ([0] if frame=='a' else [1])
    for gi in pick:
        if gi>=len(groups):continue
        x0,x1=groups[gi]
        for x in range(x0,x1+1):
            rows[last][x]='.'
            if rows[last-1][x]!='.':rows[last-1][x]='k'
    return [''.join(r) for r in rows]

def with_parts(c,parts):
    """11/14 몸 그림(c)에 테두리를 두르고, 짐 하나하나도 따로 테두리를 둘러 순서대로 얹는다 (짐이 몸·서로와 섞이지 않게)"""
    c.outline();out=c.im
    for rows,x,y in parts:
        L=Cv(c.w,c.h);L.draw(rows,x,y);L.outline();out.alpha_composite(L.im)
    return out

def make(stage,frame=''):
    c=Cv(40,32)
    oy=8
    c.draw(lift(HORSE,frame),1,oy)
    c.draw(CLOTH,14,oy+9)   # 11/14 안장 천 앞(머리 쪽) 2칸을 잘라 길마 앞으로 튀어나오지 않게. 뒤끝은 그대로
    c.draw(FRAME,14,oy+7)
    parts=[]
    if stage==1: parts+=[(SACK,16,oy+9)]
    if stage>=2: parts+=[(SACK,13,oy+9),(SACK,20,oy+9),(BUNDLE,14,oy+3)]   # 원래 간격·크기 그대로, 안장 가운데(x 19)에 맞춘다
    if stage>=3: parts+=[(SMALL,15,oy-1),(JAR,14,oy+5),(JAR,21,oy+5)]   # 11/14 항아리는 옆 자루 위에 (낙타와 같게)
    return with_parts(c,parts)

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
def make_camel(stage,frame=''):
    c=Cv(40,40); oy=10
    c.draw(lift(CAMEL,frame),1,oy)
    # 11/14 안장 천이 혹을 위에서부터 덮는다: 혹 칸마다 맨 위 줄부터 밝은 파랑·파랑·빨강 띠·파랑·술(노랑)
    body=[P[k]+(255,) for k in 'TtU']
    for x in range(1+17,1+30):
        top=next((y for y in range(oy,oy+14) if c.get(x,y) in body),None)
        if top is None or top>oy+10:continue
        for i,k in enumerate(('A','a','r','a','y' if x%2 else 'a')):
            if c.get(x,top+i) in body:c.px(x,top+i,k)
    hx=1+23                                                          # 혹 꼭대기 가운데 (짐을 여기 맞춘다)
    parts=[]
    if stage==1: parts+=[(SACK,hx-3,oy+11)]
    if stage>=2: parts+=[(SACK,hx-7,oy+11),(SACK,hx+1,oy+11),(BUNDLE,hx-5,oy+2)]
    if stage>=3: parts+=[(SMALL,hx-4,oy-2),(JAR,hx-6,oy+7),(JAR,hx+2,oy+7)]   # 항아리는 옆 자루 위에 얹는다 (앞·뒷모습과 같게)
    return with_parts(c,parts)


# ---------------- 앞(아래로 올 때)·뒤(위로 갈 때) ----------------
# 11/14 v3: LPC horses·OGA horse(opengameart) 4방향 그림을 참고. 위에서 비스듬히 본 말은 폭이 좁고(옆 길이의 1/3쯤)
# 위아래로 길다(옆모습 키의 1.3배쯤). 뒤: 귀·뒤통수 → 갈기 있는 목 → 안장 → 넓은 엉덩이 → 꼬리 → 뒷다리.
# 앞: 꼬리 끝 → 엉덩이·등·안장 → 그 앞에 귀·이마·얼굴(아래를 봄) → 양옆 어깨 아래 앞다리. 몸 폭 13, 캔버스 21 x 42.
HORSE_BACK=[            # 11/14 v6: 귀 사이부터 목 뒤 기갑까지 갈기를 풍성하게, 몸 폭 고르게
"....M...M....",
"....BMMMB....",
"....BmMmB....",
"....bmMmb....",
"....bmMmb....",
"...bmMMMmb...",
"...bmMMMmb...",
"..bBmMMMmBb..",
".bBBBmMmBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBMBBBBb.",
".bBBBMMMBBBb.",
".bBBBMMMBBBb.",
"..bBBMMMBBb..",
"..bBbMMMbBb..",
"..bBb.M.bBb..",
"..bBb.M.bBb..",
"..bBb...bBb..",
"..bBb...bBb..",
"..bBb...bBb..",
"..kkk...kkk..",
]
HORSE_FRONT=[           # 몸(꼬리 끝·엉덩이·등·갈기·가슴·앞다리). 머리는 HORSE_FACE를 따로 테두리 둘러 위에 얹는다
"......M......",
"......M......",
".....MMM.....",
"..bBBBBBBBb..",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBmMmBBBb.",
".bBBmMMMmBBb.",
".bBBmMMMmBBb.",
".bBBmMMMmBBb.",
".bBBBmMmBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
".bBBBBBBBBBb.",
"..bBBBBBBBb..",
".bBb.....bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".bBb.....bBb.",
".kkk.....kkk.",
]
HORSE_FACE=[            # 11/14 앞머리는 귀 사이 작은 술만 (갈기는 목 뒤로 풍성하게: HORSE_FRONT)
"M.....M",
"BM...MB",
"BBBMBBB",
"LBBMBBb",
"LeBBBeb",
"LBBBBBb",
".LBBBb.",
".LBBBb.",
".ccccc.",
".ccccc.",
"..ccc..",
]
CAMEL_BACK=[
".............",
".............",
".............",
".............",
".............",
".............",
".............",
"...tTTTTTt...",
"..tTTTTTTTt..",
".tTTUUUUTTTt.",
".tTUUUUUUTTt.",
".tTUUUUUUTTt.",
".tTTUUUUTTTt.",
".tTTTTTTTTTt.",
".tTTTTTTTTTt.",
".tTTTTTTTTTt.",
".tTTTTtTTTTt.",
"..tTTTtTTTt..",
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
CAMEL_BACK_HEAD=[            # 뒤: 등 너머 머리·목 (몸 뒤에 따로 그려 몸 테두리가 목 아래를 가른다). 짐을 쌓아도 머리가 짐 위로 솟게 목을 조금 길게
"....t...t....",
"....tTTTt....",
"....tTTTt....",
".....tTt.....",
".....tTt.....",
".....tTt.....",
".....tTt.....",
".....tTt.....",
"....tTTTt....",
]
CAMEL_FRONT=[           # 몸(꼬리 끝·엉덩이·혹·가슴·긴 앞다리). 목·머리는 CAMEL_FACE를 따로 얹는다
"......t......",
"......k......",
"...tTTTTTt...",
"..tTTTTTTTt..",
".tTTTTTTTTTt.",
".tTTUUUUTTTt.",
".tTUUUUUUTTt.",
".tTUUUUUUTTt.",
".tTTUUUUTTTt.",
".tTTTTTTTTTt.",
".tTTTTTTTTTt.",
"..tTTTTTTTt..",
"..tTt...tTt..",
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
CAMEL_FACE=[           # 낙타는 머리를 높이 들어 위에서 보면 머리가 혹 근처에 온다. 머리는 따로 테두리 (목·몸과 구분)
".t...t.",
".tTTTt.",
"tTTTTTt",
"TeTTTeT",
"tTTTTTt",
".tTTTt.",
".ccccc.",
"..ccc..",
]
CAMEL_NECK=[           # 긴 목 전체. 위쪽 절반만 테두리를 남기고 아래 절반은 지운다 (아래는 CAMEL_NECK_LOW가 몸과 한 덩어리로)
"..tTt..",
"..tTt..",
"..tTt..",
"..tTt..",
"..tTt..",
"..tTt..",
"..tTt..",
]
CAMEL_NECK_LOW=[       # 목 아래쪽 절반: 몸과 한 덩어리로 (테두리 없이 가슴에 이어진다)
"..tTt..",
"..tTt..",
"..tTt..",
]
SACK_L=["...nn","..XCn",".XCCC","XCCCs","XCCCs","XCCss",".sss."]   # 11/14 앞·뒤에서 본 왼쪽 옆구리 자루: 위는 몸 가장자리에 매달리고 아래로 갈수록 바깥으로 불룩
SACK_R=["nn...","nCs..","CCCs.","XCCCs","XCCCs","XCCss",".sss."]   # 오른쪽 (거울)
BUNDLE_END=["..s.s..",".XCCCC.","XCnnnCs","XCCCCCs","XCnnnCs",".sssss."]   # 11/14 등을 따라 놓인 보따리를 위에서 비스듬히: 폭 7, 윗면이 보인다 (끈 두 줄)
SMALL_END=[".XCC.","Xnnns","XCCCs",".sss."]                      # 그 위 작은 보따리 (폭 5, 위아래로 길게)
BUNDLE_C=["..s.....s..",".CnnnnnnnC.","XCCCCCCCCCs","CCCCCCCCCss",".sssssssss."]   # 11/14 앞·뒤 가운데 짐: 폭 11 (좌우 대칭)
SMALL_C=["..s...s..",".CnnnnnC.","XCCCCCCCs",".sssssss."]                                   # 폭 9
VIEW={  # 그림, 안장 띠 줄, 띠 색, 짐 기준 줄, 몸 앞에 얹는 부위[(그림, 줄)], 몸 뒤에 두는 부위, 몸에 테두리 없이 섞는 부위
 ('horse','back'):(HORSE_BACK,9,'rRr',8,[],[],[]),
 ('horse','front'):(HORSE_FRONT,6,'rRr',6,[(HORSE_FACE,12)],[],[]),
 ('camel','back'):(CAMEL_BACK,7,'AaaraY',8,[(CAMEL_BACK_HEAD[:3],-2,1)],[(CAMEL_BACK_HEAD,-2)],[]),   # 머리(위 3줄)는 짐 더미보다 위에 다시 얹는다 (높이 든 머리가 짐 너머로 보인다)
 ('camel','front'):(CAMEL_FRONT,3,'AaaraY',4,[(CAMEL_NECK,6,10),(CAMEL_FACE,-2)],[],[]),   # 머리를 높이 든 모습 (목 위쪽 절반만 테두리)
}
def end_view3(kind,view,stage,frame=''):
    from PIL import Image
    rows,band_y,band,pack_y,over,under,inline=VIEW[(kind,view)]
    W,H=23,44;ox=5;oy=H-2-len(rows)   # 11/14 양옆 자루 바깥 테두리가 캔버스 끝에 잘리지 않게 폭 23
    for r in rows:assert len(r)==13,(kind,view,r)
    layers=[]
    def layer(fn):
        L=Cv(W,H);fn(L);L.outline();layers.append(L.im)
    for part,py in under:layer(lambda L,part=part,py=py:L.draw(part,ox+(13-len(part[0]))//2,oy+py))
    def body(L):
        L.draw(lift(rows,frame),ox,oy)
        for i,ch in enumerate(band):            # 안장 천: 몸 위에만 ('Y'는 노랑·파랑이 번갈아 드리운 술)
            y=oy+band_y+i
            for x in range(W):
                if L.get(x,y)[3] and L.get(x,y)[:3] not in (P['M'],P['e'],P['k'],P['c']):L.px(x,y,('y' if x%2 else 'a') if ch=='Y' else ch)
        for part,py in inline:L.draw(part,ox+(13-len(part[0]))//2,oy+py)
    layer(body)
    if stage>=1 and kind=='camel':
        layer(lambda L:L.draw(SACK,ox-4,oy+pack_y));layer(lambda L:L.draw(SACK,ox+11,oy+pack_y))
    if kind=='camel':
        # 11/14 낙타는 머리·목이 등 가운데를 가리므로 (앞) 또는 짐 더미가 머리를 덮지 않게 (뒤) 짐을 양옆 자루 위로 쌓는다
        # 옆모습과 같은 순서: 1 양옆 자루 → 2 혹 위 가운데 보따리 → 3 그 위 작은 보따리 + 양옆 항아리
        # 앞모습은 머리가 혹 앞을 가리므로 혹 위 짐이 머리 너머(위)로 보인다
        # 앞: 혹 위 짐이 목 뒤에서 양옆으로 보인다 (목·머리가 앞에 그려진다). 3단계는 보따리를 한 단 더
        # 뒤: 혹 꼭대기 위에 쌓고, 긴 목·머리가 짐 너머로 솟는다
        if view=='front':
            if stage>=2:layer(lambda L:L.draw(BUNDLE_C,ox+1,oy-1))
            if stage>=3:layer(lambda L:L.draw(SMALL_C,ox+2,oy-5))
        else:
            if stage>=2:layer(lambda L:L.draw(BUNDLE_C,ox+1,oy+pack_y-4))
            if stage>=3:layer(lambda L:L.draw(SMALL_C,ox+2,oy+pack_y-8))
        if stage>=3:layer(lambda L:L.draw(JAR,ox-3,oy+pack_y-3));layer(lambda L:L.draw(JAR,ox+12,oy+pack_y-3))
    else:
        # 11/14 짐말 앞·뒤: 옆모습 짐 그림을 그대로 쓰지 않고 앞·뒤에서 본 모양 (옆구리 자루 모서리, 보따리 끝면). 순서는 낙타와 같다
        if stage>=1:layer(lambda L:L.draw(SACK_L,ox-3,oy+pack_y));layer(lambda L:L.draw(SACK_R,ox+11,oy+pack_y))   # 안장 띠 높이에 매달려 아래로
        if stage>=2:layer(lambda L:L.draw(BUNDLE_END,ox+3,oy+pack_y-2))   # 안장 위 등을 따라
        if stage>=3:layer(lambda L:L.draw(SMALL_END,ox+4,oy+pack_y-4))
        if stage>=3:layer(lambda L:L.draw(JAR,ox-4,oy+pack_y-1));layer(lambda L:L.draw(JAR,ox+13,oy+pack_y-1))   # 항아리는 옆 자루 위 바깥쪽
    for item in over:   # 얼굴·목은 따로 테두리 → 몸·짐과 구분된다. (그림, 줄, 자를 줄)이면 그 줄부터 아래는 지운다
        part,py=item[0],item[1];cut=item[2] if len(item)>2 else None
        if kind=='camel' and view=='back' and stage>=3:continue   # 3단계는 위 보따리가 머리 앞에 온다 (머리를 덮어 그리면 보따리 끈·띠가 가려진다). 머리는 몸 뒤 그림이 짐 위로 귀·정수리만 보인다
        L=Cv(W,H);L.draw(part,ox+(13-len(part[0]))//2,oy+py);L.outline()
        if cut is not None:   # 이 줄부터 아래는 테두리만 지운다 (목 색은 남아 가슴에 이어진다)
            O=P['O']+(255,)
            for y in range(oy+cut,H):
                for x in range(W):
                    if L.p[x,y]==O:L.p[x,y]=(0,0,0,0)
        layers.append(L.im)
    out=Image.new('RGBA',(W,H),(0,0,0,0))
    for im in layers:out.alpha_composite(im)
    return out
def horse_front(stage,frame=''):return end_view3('horse','front',stage,frame)
def horse_back(stage,frame=''):return end_view3('horse','back',stage,frame)
def camel_front(stage,frame=''):return end_view3('camel','front',stage,frame)
def camel_back(stage,frame=''):return end_view3('camel','back',stage,frame)

if __name__=='__main__':
    import os
    out=sys.argv[1]
    for folder,views in (('PackHorse',(('side',make),('front',horse_front),('back',horse_back))),('Camel',(('side',make_camel),('front',camel_front),('back',camel_back)))):
        os.makedirs(f'{out}/{folder}',exist_ok=True)
        for view,fn in views:
            for i in range(4):
                fn(i).save(f'{out}/{folder}/{view}_{i}.png')
                for fr in 'ab':fn(i,fr).save(f'{out}/{folder}/{view}_{i}_{fr}.png')   # 걸음 프레임
