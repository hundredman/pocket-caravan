"""10/26 북유럽 특산 공방 마당 그림 (사용자 선택: 덕장 A자 틀, 털옷 C-2, 양조장 솥·발효통·약초·벌통 셋·꼭지 술통). game_sprites()가 게임용 묶음."""
from PIL import Image, ImageDraw, ImageFont
import math, random
O=(46,34,32,255)            # 외곽선
WD=(96,62,40,255); WM=(140,94,58,255); WL=(184,134,82,255); WH=(214,170,110,255)
def img(w,h): return Image.new("RGBA",(w,h),(0,0,0,0))
def px(im,x,y,c):
    if 0<=x<im.width and 0<=y<im.height: im.putpixel((x,y),c)
def rect(d,x0,y0,x1,y1,fill,ol=O): d.rectangle((x0,y0,x1,y1),fill=fill,outline=ol)
def post(d,x,y0,y1,w=2):
    d.rectangle((x,y0,x+w-1,y1),fill=WM,outline=None); d.line((x,y0,x,y1),fill=WL); d.line((x+w,y0,x+w,y1),fill=O); d.line((x-1,y0,x-1,y1),fill=O)
def pole(d,x0,x1,y):
    d.line((x0,y-1,x1,y-1),fill=O); d.line((x0,y,x1,y),fill=WL); d.line((x0,y+1,x1,y+1),fill=WD); d.line((x0,y+2,x1,y+2),fill=O)
# ---- 대구 한 쌍 (꼬리로 묶여 매달림): fresh / dried
def cod(im,x,y,state,flip=0):
    if state=="fresh": body,belly,dark=(168,180,176,255),(226,230,222,255),(118,130,128,255)
    else: body,belly,dark=(196,160,104,255),(226,196,140,255),(150,112,66,255)
    d=ImageDraw.Draw(im)
    # 꼬리(위) → 머리(아래), 길이 9
    px(im,x,y,O); px(im,x+1,y,O)
    for i in range(1,9):
        w=1 if i<3 else 2 if i<7 else 1
        for dx in range(-w+1,w+1):
            c=body if dx<=0 else belly
            px(im,x+dx,y+i,c)
        px(im,x-w,y+i,O); px(im,x+w+1,y+i,O)
    px(im,x,y+9,O); px(im,x+1,y+9,O); px(im,x,y+8,dark)
    if state=="dried": px(im,x+1,y+5,dark)
def hjell_a(state="dried"):
    """A자 틀: 양끝 A자 다리, 가로대 두 줄에 대구 쌍"""
    im=img(48,40); d=ImageDraw.Draw(im)
    for bx in (3,42):          # A자 다리
        d.line((bx,38,bx+3,4),fill=O,width=1); d.line((bx+1,38,bx+3,6),fill=WM); 
        d.line((bx+6,38,bx+3,4),fill=O); d.line((bx+5,38,bx+3,6),fill=WL)
        d.line((bx+1,26,bx+5,26),fill=WD)
    pole(d,3,45,7); pole(d,3,45,22)
    if state!="empty":
        for row,yy in ((0,9),(1,24)):
            for k in range(8):
                x=8+k*4+(row*2)
                if x<42: cod(im,x,yy,state)
    # 바닥 그림자
    for x in range(4,45): 
        if x%2==0: px(im,x,39,(0,0,0,40))
    return im
def hjell_b(state="dried"):
    """긴 평틀: 기둥 셋, 가로대 셋, 대구가 빽빽"""
    im=img(48,36); d=ImageDraw.Draw(im)
    for bx in (2,23,44): post(d,bx,3,35)
    for yy in (4,15,26): pole(d,1,47,yy)
    if state!="empty":
        for yy in (6,17):
            for k in range(10):
                x=4+k*4+(1 if yy==17 else 0)
                if x<44 and abs(x-23)>2 and abs(x-24)>2: cod(im,x,yy,state)
    return im
# ---- 털옷 진열
FD,FM,FL=(92,60,40,255),(132,90,58,255),(170,124,82,255); FW,FWs=(238,234,224,255),(200,196,188,255)
def coat(im,x,y,w=10,h=17):
    d=ImageDraw.Draw(im)
    # 몸통 (아래로 퍼지는 사다리꼴)
    for i in range(h):
        half=w//2-2+ i*3//h
        for dx in range(-half,half+1):
            c=FM if dx<-half//3 else FL if dx>half//3 else FM
            if (i+dx)%5==0: c=FD
            px(im,x+dx,y+i,c)
        px(im,x-half-1,y+i,O); px(im,x+half+1,y+i,O)
    d.line((x-w//2-1,y+h,x+w//2+1,y+h),fill=O)
    # 소매
    for i in range(10):
        px(im,x-w//2-2,y+2+i,FD); px(im,x+w//2+2,y+2+i,FL); px(im,x-w//2-3,y+2+i,O); px(im,x+w//2+3,y+2+i,O)
    # 흰 털 깃·소맷부리·밑단
    for dx in range(-3,4): px(im,x+dx,y,FW); px(im,x+dx,y+1,FWs if abs(dx)>1 else FW)
    px(im,x-4,y,O); px(im,x+4,y,O)
    for dx in range(-w//2-1,w//2+2): px(im,x+dx,y+h-1,FW if dx%2 else FWs)
    for k in (-w//2-2,w//2+2): px(im,x+k,y+11,FW); px(im,x+k,y+12,FWs)
    # 가운데 여밈 끈
    for i in range(3,h-2,3): px(im,x,y+i,(200,170,90,255))
def coat_a():
    """나무 마네킹 진열대에 걸린 긴 털옷"""
    im=img(20,32); d=ImageDraw.Draw(im)
    post(d,9,22,29)                        # 기둥
    d.rectangle((5,29,15,30),fill=WM,outline=O)   # 받침
    d.ellipse((7,0,12,5),fill=(196,160,112,255),outline=O)   # 나무 머리
    coat(im,10,6)
    return im
def coat_b():
    """옷걸이 막대에 털옷 둘 (짙은 갈색·흰 여우털)"""
    im=img(32,32); d=ImageDraw.Draw(im)
    post(d,2,3,30); post(d,28,3,30); pole(d,2,30,3)
    coat(im,10,6,w=9,h=16)
    im2=img(32,32); coat(im2,22,6,w=9,h=16)
    # 두 번째 옷은 흰 여우털 색
    for yy in range(32):
        for xx in range(16,32):
            p=im2.getpixel((xx,yy))
            if p[3] and p[:3] not in [O[:3]]:
                g=int(sum(p[:3])/3); im2.putpixel((xx,yy),(min(255,g+70),min(255,g+68),min(255,g+62),255))
    im.alpha_composite(im2)
    for x in (10,22): px(im,x,4,O); px(im,x,5,WD)
    d.line((4,31,30,31),fill=(0,0,0,50))
    return im
def coat_c():
    """십자 나무틀에 털옷 + 털모자, 발밑에 털 더미"""
    im=img(24,34); d=ImageDraw.Draw(im)
    post(d,11,2,30); pole(d,3,21,8)
    d.ellipse((7,0,16,6),fill=FM,outline=O); d.line((8,5,15,5),fill=FW); d.line((8,6,15,6),fill=FWs)   # 털모자
    coat(im,12,9,w=10,h=17)
    for (x,y,c) in [(3,30,FD),(5,29,FL),(7,31,FM),(17,30,FW),(19,31,FWs),(20,29,FM)]:
        d.ellipse((x-2,y-1,x+2,y+2),fill=c,outline=O)
    return im
# ---- 짚 벌통
S1,S2,S3,S4=(122,86,40,255),(176,130,60,255),(214,172,92,255),(238,210,140,255)
def skep(im,x,y,r=6,h=11):
    """돔 모양 짚 벌통: 가로 띠가 층층이"""
    for i in range(h):
        t=i/(h-1); half=int(round(r*math.sqrt(max(0,1-(1-t)**2)) )) if i<h-2 else r
        half=max(1,half)
        for dx in range(-half,half+1):
            band=(i%3)
            c=[S2,S3,S2][band] if dx>-half//2 else [S1,S2,S1][band]
            if dx>half//2: c=S4 if band==1 else S3
            px(im,x+dx,y+i,c)
        px(im,x-half-1,y+i,O); px(im,x+half+1,y+i,O)
    for dx in range(-1,2): px(im,x+dx,y-1,O)
    # 입구
    px(im,x,y+h-2,(40,26,14,255)); px(im,x+1,y+h-2,(40,26,14,255)); px(im,x,y+h-1,(40,26,14,255)); px(im,x+1,y+h-1,(40,26,14,255))
def bee(im,x,y):
    px(im,x,y,(240,200,40,255)); px(im,x+1,y,(30,24,20,255)); px(im,x,y-1,(230,240,250,255))
def skep_a():
    """나무 받침대 위 벌통 하나 + 벌"""
    im=img(18,20); d=ImageDraw.Draw(im)
    skep(im,9,2)
    d.rectangle((2,13,16,14),fill=WL,outline=O); post(d,4,15,19,1); post(d,13,15,19,1)
    for (x,y) in [(2,3),(15,1),(16,7)]: bee(im,x,y)
    return im
def skep_b():
    """긴 벤치 위 벌통 셋 + 벌 떼"""
    im=img(40,20); d=ImageDraw.Draw(im)
    for x in (8,20,32): skep(im,x,3,r=5,h=10)
    d.rectangle((1,13,38,14),fill=WL,outline=O); post(d,3,15,19,1); post(d,19,15,19,1); post(d,35,15,19,1)
    for (x,y) in [(2,2),(14,1),(27,0),(38,4),(25,6)]: bee(im,x,y)
    return im
# ---- 술병 상자
def bottle(im,x,y,col):
    dark=tuple(int(c*0.65) for c in col[:3])+(255,); lite=tuple(min(255,int(c*1.3)) for c in col[:3])+(255,)
    px(im,x,y,(150,110,70,255)); px(im,x,y+1,dark)
    for i in range(2,8):
        px(im,x-1,y+i,dark); px(im,x,y+i,col); px(im,x+1,y+i,lite if i<5 else col)
        px(im,x-2,y+i,O); px(im,x+2,y+i,O)
    px(im,x-1,y+1,O); px(im,x+1,y+1,O); px(im,x,y-1,O)
def crate_a():
    im=img(20,18); d=ImageDraw.Draw(im)
    for i,x in enumerate((4,8,12,16)): bottle(im,x,1,(80,132,84,255) if i%2==0 else (200,140,60,255))
    rect(d,1,8,18,17,WM); d.line((2,12,17,12),fill=WD); d.line((2,9,17,9),fill=WL)
    for x in (2,17): d.line((x,9,x,16),fill=WL)
    return im
def crate_b():
    """상자 둘 쌓고 옆에 꿀단지"""
    im=img(28,24); d=ImageDraw.Draw(im)
    rect(d,1,14,16,23,WM); d.line((2,18,15,18),fill=WD)
    rect(d,3,6,14,13,WL); d.line((4,9,13,9),fill=WM)
    for x in (5,9,13): bottle(im,x,0,(80,132,84,255))
    # 꿀단지
    d.ellipse((18,13,27,23),fill=(212,150,60,255),outline=O); d.rectangle((20,11,25,13),fill=(170,110,50,255),outline=O)
    d.line((21,16,22,15),fill=(248,210,120,255)); d.line((20,17,20,19),fill=(248,210,120,255))
    return im
# ---- 양조 솥 + 김
CU1,CU2,CU3=(132,70,40,255),(192,112,62,255),(236,160,98,255)
def kettle(frame=0, strong=False):
    im=img(28,34); d=ImageDraw.Draw(im)
    # 김
    rng=random.Random(frame)
    n=7 if strong else 4
    for k in range(n):
        yy=12-k*2-(frame%3); xx=14+int(4*math.sin((k+frame)*1.3))
        r=2+k//2
        d.ellipse((xx-r,yy-r,xx+r,yy+r),fill=(240,244,246,170 if strong else 130))
    # 솥
    d.ellipse((4,16,24,30),fill=CU2,outline=O)
    d.ellipse((6,15,22,20),fill=CU1,outline=O); d.ellipse((8,16,20,19),fill=(180,140,80,255))  # 안쪽 술
    d.arc((5,17,23,29),200,250,fill=CU3); d.line((7,22,7,25),fill=CU3)
    # 다리·불
    for x in (7,20): d.line((x,29,x-1,33),fill=O); d.line((x+1,29,x+1,33),fill=O)
    for (x,c) in [(12,(240,120,40,255)),(15,(250,190,60,255)),(18,(240,120,40,255))]:
        d.polygon(((x,33),(x-2,33),(x-1,29+(frame%2))),fill=c)
    d.line((9,33,20,33),fill=WD)
    return im
def vat():
    """큰 나무 술통(발효통) + 사다리"""
    im=img(28,32); d=ImageDraw.Draw(im)
    d.rectangle((4,8,23,31),fill=WM,outline=O)
    for x in (8,13,18): d.line((x,9,x,30),fill=WD)
    for y in (12,20,28): d.line((4,y,23,y),fill=(90,90,96,255))
    d.ellipse((4,4,23,11),fill=WL,outline=O); d.ellipse((7,5,20,10),fill=(196,150,80,255))
    for k in range(3): d.ellipse((10+k*3,0+k,14+k*3,4+k),fill=(240,244,246,140))
    d.line((24,31,27,6),fill=O); d.line((25,31,28,6),fill=WL)   # 사다리
    for y in range(10,31,5): d.line((24,y,27,y),fill=WD)
    return im
def herbs():
    """약초 말리는 다발 (줄에 매단 초록 다발 셋)"""
    im=img(24,14); d=ImageDraw.Draw(im)
    pole(d,0,23,2)
    for x in (4,11,18):
        d.polygon(((x,4),(x-3,12),(x+3,12)),fill=(110,150,80,255),outline=O)
        d.line((x-1,6,x-2,11),fill=(150,190,110,255)); px(im,x,4,(150,110,70,255))
    return im
# ---- 간판
def sign(text_w=36):
    im=img(text_w+8,22); d=ImageDraw.Draw(im)
    post(d,4,8,21,2); post(d,text_w+2,8,21,2)
    d.rectangle((1,1,text_w+6,10),fill=WL,outline=O); d.line((2,2,text_w+5,2),fill=WH)
    for x in range(5,text_w+3,3): px(im,x,6,WD)
    return im

# ===== 다듬은 버전 =====
FRESH=dict(back=(110,124,122,255),body=(160,174,170,255),belly=(222,228,220,255),fin=(94,106,104,255))
DRIED=dict(back=(150,108,62,255),body=(196,156,98,255),belly=(232,204,148,255),fin=(126,88,50,255))
def fish2(im,x,y,pal,h=12):
    """꼬리를 위로 매단 대구 한 마리 (꼬리 지느러미 갈라짐·몸통·아가미·머리)"""
    # 꼬리 지느러미
    for dx in (-2,-1,1,2): px(im,x+dx,y,pal["fin"])
    px(im,x-3,y,O); px(im,x+3,y,O); px(im,x,y,O)
    for dx in (-1,0,1): px(im,x+dx,y+1,pal["fin"])
    px(im,x-2,y+1,O); px(im,x+2,y+1,O)
    px(im,x,y+2,pal["back"]); px(im,x-1,y+2,O); px(im,x+1,y+2,O)
    widths=[1,1,2,2,2,2,2,2,1]
    for i,w in enumerate(widths):
        yy=y+3+i
        for dx in range(-w,w+1):
            c=pal["back"] if dx==-w else pal["belly"] if dx==w else pal["body"]
            px(im,x+dx,yy,c)
        px(im,x-w-1,yy,O); px(im,x+w+1,yy,O)
    # 아가미 선·눈·주둥이
    gy=y+3+len(widths)-3
    px(im,x,gy,pal["fin"]); px(im,x+1,gy,pal["fin"])
    px(im,x-1,gy+1,(30,24,22,255))
    px(im,x,y+3+len(widths),O); px(im,x-1,y+3+len(widths),O); px(im,x+1,y+3+len(widths),O)
def hang(im,x,y,pal):
    d=ImageDraw.Draw(im); px(im,x,y-1,(120,96,70,255)); px(im,x+3,y-1,(120,96,70,255))
    fish2(im,x,y,pal); fish2(im,x+4,y+1,pal)
def aframe(d,cx,top,bottom):
    """A자 다리 한 쌍: 꼭대기에서 엇갈려 위로 살짝 튀어나옴"""
    for (x0,x1) in ((cx-1,cx-6),(cx+1,cx+6)):
        d.line((x0,top-2,x1,bottom),fill=O,width=3)
    d.line((cx-1,top-1,cx-5,bottom-1),fill=WL); d.line((cx+1,top-1,cx+5,bottom-1),fill=WM)
    d.line((cx-4,bottom-12,cx+4,bottom-12),fill=O,width=2); d.line((cx-3,bottom-12,cx+3,bottom-12),fill=WD)
def hjell_a2(state="dried"):
    im=img(56,46); d=ImageDraw.Draw(im)
    for x in range(2,55,2): px(im,x,45,(60,60,80,40))
    aframe(d,7,4,44); aframe(d,48,4,44)
    pole(d,2,53,7); pole(d,2,53,24)
    if state!="empty":
        pal=FRESH if state=="fresh" else DRIED
        for k in range(5): hang(im,12+k*7,10,pal)
        for k in range(5): hang(im,14+k*7,27,pal)
    return im
def hjell_b2(state="dried"):
    im=img(56,40); d=ImageDraw.Draw(im)
    for bx in (3,27,51): post(d,bx,3,39)
    for yy in (4,20): pole(d,1,55,yy)
    if state!="empty":
        pal=FRESH if state=="fresh" else DRIED
        for yy in (7,23):
            for k in range(6):
                x=6+k*8 + (0 if k<3 else 2)
                if abs(x-27)>3 and abs(x+4-27)>3: hang(im,x,yy,pal)
    return im
FOX=dict(d=(150,146,140,255),m=(206,202,194,255),l=(238,236,230,255),trim=(120,90,60,255))
BEAR=dict(d=(92,60,40,255),m=(132,90,58,255),l=(170,124,82,255),trim=(238,234,224,255))
def coat2(im,x,y,pal,w=10,h=17):
    for i in range(h):
        half=w//2-2+ i*3//h
        for dx in range(-half,half+1):
            c=pal["m"] if dx<-half//3 else pal["l"] if dx>half//3 else pal["m"]
            if (i+dx)%5==0: c=pal["d"]
            px(im,x+dx,y+i,c)
        px(im,x-half-1,y+i,O); px(im,x+half+1,y+i,O)
    for dx in range(-w//2-1,w//2+2): px(im,x+dx,y+h,O)
    for i in range(10):
        px(im,x-w//2-2,y+2+i,pal["d"]); px(im,x+w//2+2,y+2+i,pal["l"]); px(im,x-w//2-3,y+2+i,O); px(im,x+w//2+3,y+2+i,O)
    for dx in range(-3,4): px(im,x+dx,y,pal["trim"]); px(im,x+dx,y+1,pal["trim"])
    px(im,x-4,y,O); px(im,x+4,y,O)
    for dx in range(-w//2-1,w//2+2): px(im,x+dx,y+h-1,pal["trim"])
    for k in (-w//2-2,w//2+2): px(im,x+k,y+11,pal["trim"]); px(im,x+k,y+12,pal["trim"])
    for i in range(3,h-2,3): px(im,x,y+i,(200,170,90,255))
def coat_b2():
    im=img(34,34); d=ImageDraw.Draw(im)
    post(d,2,3,32); post(d,30,3,32); pole(d,2,32,3)
    for x,p in ((10,BEAR),(23,FOX)):
        px(im,x,4,O); px(im,x,5,WD); coat2(im,x,6,p,w=9,h=16)
    d.line((4,33,31,33),fill=(0,0,0,50))
    return im
def coat_c2():
    im=img(24,34); d=ImageDraw.Draw(im)
    post(d,11,2,30); pole(d,3,21,8)
    d.ellipse((8,1,15,7),fill=(132,90,58,255),outline=O); d.rectangle((7,5,16,7),fill=(238,234,224,255),outline=O)   # 털모자
    coat2(im,12,9,BEAR)
    for (x,y,c) in [(4,30,(92,60,40,255)),(7,31,(170,124,82,255)),(17,30,(238,236,230,255)),(20,31,(206,202,194,255))]:
        d.ellipse((x-3,y-2,x+3,y+2),fill=c,outline=O)
    return im
def kettle2(frame=0, strong=False):
    im=img(30,36); d=ImageDraw.Draw(im)
    n=8 if strong else 4
    for k in range(n):
        yy=14-k*2-(frame%3); xx=15+int(4*math.sin((k+frame)*1.3)); r=2+k//2
        d.ellipse((xx-r,yy-r,xx+r,yy+r),fill=(250,250,252,220 if strong else 150),outline=(200,204,214,200 if strong else 120))
    d.ellipse((5,18,25,32),fill=CU2,outline=O)
    d.ellipse((7,17,23,22),fill=CU1,outline=O); d.ellipse((9,18,21,21),fill=(180,140,80,255))
    d.arc((6,19,24,31),200,250,fill=CU3); d.line((8,24,8,27),fill=CU3)
    for x in (8,21): d.line((x,31,x-1,35),fill=O); d.line((x+1,31,x+1,35),fill=O)
    for (x,c) in [(13,(240,120,40,255)),(16,(250,190,60,255)),(19,(240,120,40,255))]:
        d.polygon(((x,35),(x-2,35),(x-1,31+(frame%2))),fill=c)
    d.line((10,35,21,35),fill=WD)
    return im

def aframe3(d,cx,top,bottom):
    for s in (-1,1):
        d.line((cx,top-3,cx+s*8,bottom),fill=O,width=3)
        d.line((cx,top-2,cx+s*7,bottom-1),fill=WL if s<0 else WM)
    d.line((cx-6,bottom-10,cx+6,bottom-10),fill=O,width=2); d.line((cx-5,bottom-10,cx+5,bottom-10),fill=WD)
def hjell_a3(state="dried"):
    im=img(60,48); d=ImageDraw.Draw(im)
    for x in range(2,59,2): px(im,x,47,(60,60,80,40))
    aframe3(d,9,5,46); aframe3(d,50,5,46)
    pole(d,3,56,8); pole(d,3,56,26)
    if state!="empty":
        pal=FRESH if state=="fresh" else DRIED
        for k in range(4): hang(im,16+k*9,11,pal)
        for k in range(4): hang(im,18+k*9,29,pal)
    return im
def hjell_b3(state="dried"):
    im=img(58,40); d=ImageDraw.Draw(im)
    for bx in (3,28,53): post(d,bx,3,39)
    for yy in (4,21): pole(d,1,57,yy)
    if state!="empty":
        pal=FRESH if state=="fresh" else DRIED
        for yy in (7,24):
            for x in (8,17,35,44): hang(im,x,yy,pal)
    return im

# ===== 털옷 C 발전 =====
def furcoat3(im,x,y,pal,w=12,h=20):
    """복슬복슬한 털옷: 가장자리 털 뭉치, 허리띠, 털 깃·소맷부리·밑단"""
    rng=random.Random(x*7+y)
    for i in range(h):
        half=w//2-2+i*4//h
        for dx in range(-half,half+1):
            c=pal["m"]
            if dx<-half//2: c=pal["d"]
            elif dx>half//2: c=pal["l"]
            if rng.random()<0.18: c=pal["d"] if c!=pal["d"] else pal["m"]
            px(im,x+dx,y+i,c)
        # 털 뭉치 외곽 (들쭉날쭉)
        jl=1 if (i%3==0) else 0; jr=1 if (i%3==1) else 0
        px(im,x-half-1-jl,y+i,O); px(im,x+half+1+jr,y+i,O)
        if jl: px(im,x-half-1,y+i,pal["d"])
        if jr: px(im,x+half+1,y+i,pal["l"])
    for dx in range(-w//2-2,w//2+3): px(im,x+dx,y+h,O)
    # 소매 (털이 복슬한 원통)
    for i in range(12):
        for k,c in ((-w//2-2,pal["d"]),(-w//2-3,pal["m"]),(w//2+2,pal["l"]),(w//2+3,pal["m"])):
            px(im,x+k,y+2+i,c)
        px(im,x-w//2-4,y+2+i,O); px(im,x+w//2+4,y+2+i,O)
    # 흰 털 깃 (둥글게 부푼)
    T=pal["trim"]; Ts=tuple(max(0,c-34) for c in T[:3])+(255,)
    for dx in range(-4,5):
        px(im,x+dx,y-1,O if abs(dx)==4 else T); px(im,x+dx,y,T); px(im,x+dx,y+1,Ts if abs(dx)>2 else T)
    px(im,x-5,y,O); px(im,x+5,y,O); px(im,x-5,y+1,O); px(im,x+5,y+1,O)
    # 소맷부리·밑단 털
    for k in (-w//2-3,-w//2-2,w//2+2,w//2+3): px(im,x+k,y+13,T); px(im,x+k,y+14,Ts)
    for dx in range(-w//2-1,w//2+2): px(im,x+dx,y+h-1,T if dx%2 else Ts); px(im,x+dx,y+h-2,T if dx%3 else Ts)
    # 가죽 허리띠와 뼈 단추
    for dx in range(-w//2+1,w//2): px(im,x+dx,y+9,(70,46,30,255))
    px(im,x,y+9,(220,200,150,255))
    for yy in (4,6,12,15): px(im,x,y+yy,(230,214,170,255))
def furhat(im,x,y,pal):
    d=ImageDraw.Draw(im)
    d.ellipse((x-5,y,x+5,y+8),fill=pal["m"],outline=O)
    d.rectangle((x-6,y+5,x+6,y+8),fill=pal["trim"],outline=O)
    d.rectangle((x-7,y+7,x-5,y+11),fill=pal["trim"],outline=O); d.rectangle((x+5,y+7,x+7,y+11),fill=pal["trim"],outline=O)   # 귀덮개
    px(im,x-2,y+2,pal["l"]); px(im,x-1,y+1,pal["l"])
def boots(im,x,y,pal):
    d=ImageDraw.Draw(im)
    for bx in (x-4,x+2):
        d.rectangle((bx,y,bx+3,y+5),fill=pal["d"],outline=O); d.rectangle((bx,y+5,bx+5,y+7),fill=pal["m"],outline=O)
        d.line((bx+1,y+1,bx+2,y+1),fill=pal["trim"])
def hidepile(im,x,y):
    d=ImageDraw.Draw(im)
    for (dx,dy,c) in [(0,2,(92,60,40,255)),(3,0,(170,124,82,255)),(7,2,(238,236,230,255)),(4,4,(132,90,58,255))]:
        d.ellipse((x+dx-4,y+dy-2,x+dx+4,y+dy+2),fill=c,outline=O)
def coat_c3():
    """C 발전 1: 십자틀에 복슬 털옷 + 귀덮개 털모자 + 털장화 + 가죽 더미"""
    im=img(36,44); d=ImageDraw.Draw(im)
    for x in range(4,33,2): px(im,x,43,(60,60,80,40))
    post(d,17,4,40); pole(d,6,30,12)
    d.rectangle((12,40,24,42),fill=WM,outline=O)
    furhat(im,18,1,BEAR)
    furcoat3(im,18,12,BEAR)
    hidepile(im,6,38); boots(im,29,34,BEAR)
    return im
def tanframe():
    """가죽 펴는 나무틀 (팽팽히 묶인 가죽)"""
    im=img(26,30); d=ImageDraw.Draw(im)
    post(d,2,2,29); post(d,22,2,29); pole(d,1,24,3); pole(d,1,24,22)
    d.polygon(((6,7),(19,6),(21,13),(18,20),(6,20),(4,13)),fill=(196,150,100,255),outline=O)
    d.polygon(((8,9),(17,8),(18,13),(16,18),(8,18),(6,13)),fill=(214,172,120,255))
    for (a,b) in [((4,7),(6,7)),((19,6),(22,6)),((4,20),(6,20)),((18,20),(22,20)),((2,13),(4,13)),((21,13),(23,13))]:
        d.line((a,b),fill=(230,214,170,255))
    return im
def coat_c4():
    """C 발전 2: 털옷 진열 + 옆에 가죽 펴는 틀 + 작업대(가위·실)"""
    im=img(64,46); d=ImageDraw.Draw(im)
    im.alpha_composite(tanframe(),(36,12))
    c=coat_c3(); im.alpha_composite(c,(0,2))
    d.rectangle((40,38,62,41),fill=WL,outline=O); post(d,42,42,45,1); post(d,59,42,45,1)
    px(im,46,37,(180,180,190,255)); px(im,47,36,(180,180,190,255)); px(im,48,37,(180,180,190,255))   # 가위
    d.ellipse((52,35,56,38),fill=(200,60,50,255),outline=O)   # 실타래
    return im
# ===== 양조장 '다 됨' 표시 후보 =====
def jugs():
    """밀랍 봉한 꿀술 항아리 줄 (다 되면 하나씩 늘어남)"""
    im=img(30,16); d=ImageDraw.Draw(im)
    for i,x in enumerate((5,13,21)):
        d.ellipse((x-4,4,x+4,15),fill=(186,120,64,255),outline=O)
        d.rectangle((x-2,1,x+2,4),fill=(160,100,52,255),outline=O)
        d.rectangle((x-2,0,x+2,1),fill=(212,60,52,255))          # 붉은 밀랍
        d.line((x-2,8,x-2,11),fill=(226,170,100,255))
    return im
def tapbarrel():
    """꼭지 달린 꿀술 통: 다 되면 꼭지에서 금빛 꿀술이 똑똑"""
    im=img(26,22); d=ImageDraw.Draw(im)
    d.ellipse((2,3,22,19),fill=WM,outline=O)
    for x in (8,12,16): d.line((x,4,x,18),fill=WD)
    d.line((5,5,5,17),fill=(90,90,96,255)); d.line((19,5,19,17),fill=(90,90,96,255))
    d.rectangle((22,9,25,11),fill=(150,150,156,255),outline=O)
    for y in (13,16,19): px(im,24,y,(240,190,70,255))
    d.rectangle((0,19,24,21),fill=WD,outline=O)
    return im

# ===== 게임용 묶음 (10/26 사용자 선택: 덕장 A자 틀, 털옷 C-2, 양조장 솥·발효통·약초·벌통 셋·꼭지 술통) =====
def coat_c4_work():
    """털옷 C-2 작업 중: 진열대는 비고, 가죽 펴는 틀에 새 가죽이 걸림"""
    im=img(64,46); d=ImageDraw.Draw(im)
    im.alpha_composite(tanframe(),(36,12))
    for x in range(4,33,2): px(im,x,45,(60,60,80,40))
    post(d,17,6,42); pole(d,6,30,14); d.rectangle((12,42,24,44),fill=WM,outline=O)
    hidepile(im,6,40); boots(im,29,36,BEAR)
    d.rectangle((40,38,62,41),fill=WL,outline=O); post(d,42,42,45,1); post(d,59,42,45,1)
    d.ellipse((44,35,52,39),fill=(132,90,58,255),outline=O)          # 작업대 위 재단 중인 털
    px(im,54,37,(180,180,190,255)); px(im,55,36,(180,180,190,255)); px(im,56,37,(180,180,190,255))
    return im
def tapbarrel2(frame=0, ready=True):
    im=img(26,22); d=ImageDraw.Draw(im)
    d.ellipse((2,3,22,19),fill=WM,outline=O)
    for x in (8,12,16): d.line((x,4,x,18),fill=WD)
    d.line((5,5,5,17),fill=(90,90,96,255)); d.line((19,5,19,17),fill=(90,90,96,255))
    d.rectangle((22,9,25,11),fill=(150,150,156,255),outline=O)
    if ready:
        for y in ((12,15,18) if frame==0 else (13,16,19)): px(im,24,y,(240,190,70,255))
        px(im,23,20,(240,190,70,255)); px(im,24,20,(250,214,110,255)); px(im,25,20,(240,190,70,255))
    d.rectangle((0,19,22,21),fill=WD,outline=O)
    return im
def fire_seat():
    """10/27 모닥불 옆 통나무 의자 (16x10): 가로로 누운 통나무, 나이테 단면이 보임"""
    im=img(16,10); d=ImageDraw.Draw(im)
    d.rounded_rectangle((1,2,14,8),radius=2,fill=WM,outline=O)
    d.line((3,3,12,3),fill=WH); d.line((3,4,12,4),fill=WL); d.line((3,7,12,7),fill=WD)
    for x in (5,9): px(im,x,5,WD)
    d.ellipse((11,2,15,8),fill=WL,outline=O); px(im,13,5,WD); px(im,13,4,WH)
    d.line((2,9,13,9),fill=(0,0,0,60))
    return im

def ferry_boat(stroke):
    """10/27 나룻배 노 젓기: 위에서 본 작은 배 (뱃머리가 위). stroke 0 = 노를 옆으로 편 채, 1 = 노를 뒤로 당긴 순간"""
    W,Hh=30,34
    im=img(W,Hh); d=ImageDraw.Draw(im)
    cx=15
    # 노 (배 아래에 먼저)
    oar=(150,104,66,255); blade=(196,150,96,255)
    if stroke==0:
        d.line((cx-6,17,1,15),fill=O,width=3); d.line((cx-6,17,1,15),fill=oar,width=1); d.rectangle((0,13,3,17),fill=blade,outline=O)
        d.line((cx+6,17,28,15),fill=O,width=3); d.line((cx+6,17,28,15),fill=oar,width=1); d.rectangle((26,13,29,17),fill=blade,outline=O)
    else:
        d.line((cx-6,16,2,23),fill=O,width=3); d.line((cx-6,16,2,23),fill=oar,width=1); d.rectangle((0,22,3,27),fill=blade,outline=O)
        d.line((cx+6,16,27,23),fill=O,width=3); d.line((cx+6,16,27,23),fill=oar,width=1); d.rectangle((26,22,29,27),fill=blade,outline=O)
    # 배 몸통: 뱃머리 뾰족
    hull=[(cx,1),(cx+6,8),(cx+7,22),(cx+5,31),(cx-5,31),(cx-7,22),(cx-6,8)]
    d.polygon(hull,fill=WM,outline=O)
    inner=[(cx,5),(cx+4,10),(cx+5,22),(cx+3,28),(cx-3,28),(cx-5,22),(cx-4,10)]
    d.polygon(inner,fill=WD)
    for y in (12,20,26): d.line((cx-4,y,cx+4,y),fill=WL)          # 가로 앉을 판
    d.line((cx,3,cx,6),fill=WH)
    # 노 젓는 사람 (가운데): 파란 옷, 갈색 털모자, 작은 뿔
    d.ellipse((cx-4,14,cx+4,21),fill=(60,96,156,255),outline=O)
    d.ellipse((cx-3,11,cx+3,16),fill=(150,104,66,255),outline=O)
    px(im,cx-3,10,(214,190,140,255)); px(im,cx+3,10,(214,190,140,255)); px(im,cx-4,9,(214,190,140,255)); px(im,cx+4,9,(214,190,140,255))
    # 짐 꾸러미 (뒤쪽)
    d.rectangle((cx-3,23,cx+3,27),fill=(214,190,140,255),outline=O); d.line((cx,23,cx,27),fill=(150,104,66,255))
    return im

def ferry_ice(big):
    """떠내려오는 얼음 덩어리 (위에서 본 모양)"""
    W,Hh=(30,18) if big else (18,13)
    im=img(W,Hh); d=ImageDraw.Draw(im)
    pts=[(3,4),(W*0.4,0),(W-4,2),(W-1,Hh*0.55),(W-6,Hh-1),(W*0.35,Hh-2),(0,Hh*0.6)]
    d.polygon([(int(x),int(y)) for x,y in pts],fill=(226,240,248,255),outline=(96,128,156,255))
    d.polygon([(int(x),int(y)) for x,y in [(4,5),(W*0.4,2),(W*0.6,4),(W*0.3,Hh*0.5)]],fill=(255,255,255,255))
    d.line((W*0.55,Hh*0.6,W-6,Hh*0.5),fill=(170,200,220,255))
    return im

def game_sprites():
    out={}
    for st in ("empty","fresh","dried"): out["yard_hjell_"+st]=hjell_a3(st)
    out["yard_fur_full"]=coat_c4(); out["yard_fur_work"]=coat_c4_work()
    for i in range(3):
        out[f"yard_kettle_a{i}"]=kettle2(i,False); out[f"yard_kettle_b{i}"]=kettle2(i,True)
    out["yard_vat"]=vat(); out["yard_herbs"]=herbs(); out["yard_skeps"]=skep_b()
    out["yard_tap_idle"]=tapbarrel2(0,False); out["yard_tap0"]=tapbarrel2(0,True); out["yard_tap1"]=tapbarrel2(1,True)
    out["fire_seat"]=fire_seat()
    out["ferry_boat_0"]=ferry_boat(0); out["ferry_boat_1"]=ferry_boat(1)   # 10/27 나룻배 노 젓기
    out["ferry_ice_0"]=ferry_ice(True); out["ferry_ice_1"]=ferry_ice(False)
    out.update(__import__("north_keeper_art").sprites())   # 10/27 장터 장꾼
    return out
