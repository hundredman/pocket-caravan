"""11/09 은샘 요청: 손수레 그림 업그레이드 2차 초안. 장터 좌판(wares_counter)과 같은 색: 테두리 (46,36,29), 나무 6단, 짐은 좌판 위 물건처럼
무명 자루·옥색 항아리·보따리. 3/4 내려다보기(윗면 안쪽이 조금 보임), 모서리 기둥, 쇠 테 바퀴(바큇살·굴대·왼쪽 위 빛).
손잡이는 나란한 두 막대(=자). 세 방향: side(왼쪽으로 갈 때, 오른쪽은 뒤집기) / front(아래로 갈 때, 수레가 위·손잡이가 아래) / back(위로 갈 때). 짐(나무 상자) 0~3단계. 손잡이 끝은 사람 옆구리에 온다.
실행: python Tools/ArtGen/handcart_v3.py <출력 폴더>
"""
import sys,math
from pathlib import Path
from PIL import Image
P=dict(O=(46,36,29),i=(52,39,31),d=(91,67,47),m=(104,69,45),w=(134,81,59),l=(152,101,59),h=(183,124,62),H=(193,139,82),
       k=(52,48,44),g=(88,82,74),s=(132,124,112),
       c=(221,198,150),C=(242,223,179),X=(246,228,191),b=(193,139,82),
       j=(84,126,106),J=(109,148,130),K=(134,162,139),q=(65,102,91),
       P=(240,214,158),p=(222,188,128),n=(196,150,88),N=(160,112,62),e=(120,82,48))
class C:
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
    def outline(s):
        o=P['O']+(255,)
        pts=[(x,y) for y in range(s.h) for x in range(s.w) if s.get(x,y)[3]==0 and any(s.get(x+a,y+b)[3] and s.get(x+a,y+b)!=o for a,b in((1,0),(-1,0),(0,1),(0,-1)))]
        for x,y in pts:s.p[x,y]=o
    def paste(s,o,x,y):s.im.alpha_composite(o.im,(x,y));s.p=s.im.load()
    def flip(s):
        n=C(s.w,s.h);n.im=s.im.transpose(Image.FLIP_LEFT_RIGHT);n.p=n.im.load();return n

# ---------------- 짐 ----------------
def ascii(rows):
    c=C(len(rows[0])+2,len(rows)+2)
    for y,r in enumerate(rows):
        for x,ch in enumerate(r):
            if ch!='.':c.px(x+1,y+1,ch)
    c.outline();return c
def sack():   # 무명 자루: 위를 갈색 끈으로 묶어 귀가 선다
    return ascii(['...c.c...',
                  '...bbb...',
                  '..XCCCc..',
                  '.XCCCCCc.',
                  '.XCCCCCc.',
                  '.CCCCCcc.',
                  '..ccccc..'])
def jar():
    c=C(8,9)    # 옥색 항아리 (좌판의 청자색)
    rows=[(2,5),(1,6),(1,6),(1,6),(2,5)]
    c.hline(2,5,1,'q');c.hline(3,4,1,'J')
    for y,(a,b) in enumerate(rows,start=2):
        for x in range(a,b+1):
            u=(x-a)/max(1,b-a);c.px(x,y,'K' if u<.3 else 'J' if u<.7 else 'j')
    c.px(2,3,'X');c.outline();return c
def bundle():   # 보따리: 매듭 귀 두 개
    return ascii(['...c...c...',
                  '...CbbbC...',
                  '..XCCCCCc..',
                  '.XCCCCCCCc.',
                  '.CCCCCCCcc.',
                  '..ccccccc..'])
def crate(big=True):   # 나무 상자: 밝은 윗면, 판자 두 장 앞면, 모서리 못, 짐칸 나무보다 밝은 색
    if big:
        return ascii(['PPPPPPPPP',
                      'pPPPPPPPn',
                      'nnnnnnnnN',
                      'peppppepN',
                      'ppppppppN',
                      'NNNNNNNNN',
                      'peppppepN',
                      'nnnnnnnnN'])
    return ascii(['PPPPPPP',
                  'pPPPPPn',
                  'nnnnnnN',
                  'pepppeN',
                  'NNNNNNN',
                  'pppppnN'])
def cargo(stage,width,height=16):
    """상자 0~3단계: 하나 → 둘 나란히 → 위에 하나 더"""
    g=C(width,height);base=height
    if stage>=1:g.paste(crate(),0,base-10)
    if stage>=2:g.paste(crate(),width-11,base-10)
    if stage>=3:g.paste(crate(False),(width-9)//2,base-15)
    return g

# ---------------- 바퀴 ----------------
def wheel(c,cx,cy,r):
    for y in range(int(cy-r)-1,int(cy+r)+2):
        for x in range(int(cx-r)-1,int(cx+r)+2):
            dx,dy=x+.5-cx,y+.5-cy;d=math.hypot(dx,dy)
            if d>r:continue
            if d>r-1.1:k='s' if (dx<-.3 and dy<-.3 and d>r-.6) else 'k'
            elif d>r-2.0:k='m'
            elif d<1.3:k='g' if d<0.8 else 'k'
            else:
                k='l' if (abs(dx)<.6 or abs(dy)<.6) else ('w' if r>5 and abs(abs(dx)-abs(dy))<.5 and d<r-2.6 else 'i')
            if k:c.px(x,y,k)
def edge_wheel(c,x0,y0,h):
    """정면에서 본 바퀴(두께만 보임)"""
    for y in range(y0,y0+h):
        t=(y-y0)/(h-1)
        c.px(x0,y,'k');c.px(x0+2,y,'k')
        c.px(x0+1,y,'s' if t<.15 else 'g' if t>.85 else 'm')
    c.px(x0+1,y0+h//2,'k');c.px(x0+1,y0+h//2-1,'l')

# ---------------- 옆 (왼쪽으로 갈 때) ----------------
# 놓는 법(게임): 그림 가운데를 사람 발밑+0.9375칸 높이, 사람 뒤 1.25칸 → 바퀴 바닥 = 사람 발바닥.
# 손잡이는 짐칸 아래 모서리(땅에서 8~9px = 사람 허리)에서 수평으로 사람 쪽으로 뻗는다. 바퀴를 줄여 짐칸을 허리 높이로 내렸다.
def side(stage=0):
    c=C(32,30)
    for x in range(0,10):c.px(x,21,'h');c.px(x,22,'d')     # 수평 손잡이
    c.px(0,21,'m')
    c.hline(9,28,14,'i');c.hline(9,28,15,'d')               # 짐칸 안쪽
    if stage:c.paste(cargo(stage,20),9,1)
    c.hline(9,28,16,'H');c.hline(9,28,17,'h');c.hline(9,28,18,'w');c.hline(9,28,19,'d')
    c.hline(9,28,20,'w');c.hline(9,28,21,'m');c.hline(9,28,22,'d')
    for x in (13,22):c.px(x,18,'m');c.px(x+1,21,'d')
    for x in (9,18,27):
        c.vline(x,13,22,'l');c.vline(x+1,13,22,'m');c.px(x,13,'H');c.px(x+1,13,'h')
    c.hline(11,26,23,'i')
    c.vline(26,23,26,'m');c.vline(27,23,26,'d')             # 뒤 받침목: 바퀴보다 짧게(끌고 갈 때 땅에서 뜬다)
    c.outline()
    w=C(32,30);wheel(w,17.5,24.5,4.6);w.outline();c.paste(w,0,0)
    return c

def side_handle():
    """옆모습 손잡이 앞 겹 (사람 몸보다 앞에 그린다 → 옆구리를 가린다). 캔버스 37x30, 수레 그림보다 왼쪽으로 5px 넓다.
    놓는 법: 수레 그림과 같은 높이, 가운데를 수레 가운데에서 사람 쪽으로 2.5px. 손잡이 끝이 사람 몸 가운데까지 온다"""
    c=C(37,30)
    for x in range(1,15):c.px(x,21,'h');c.px(x,22,'d')
    c.px(1,21,'m');c.px(2,21,'m')
    c.outline();return c

# ---------------- 앞 (아래로 갈 때: v3 자리 그대로, 수레가 사람 위) ----------------
# 놓는 법: 그림 가운데를 사람 위 1.0칸. 손잡이 두 막대는 사람 몸(14px) 바로 바깥에서 곧게 내려와 허리 조금 아래에서 끝난다
def front(stage=0):
    c=C(28,32);oy=6
    edge_wheel(c,0,oy+5,13);edge_wheel(c,25,oy+5,13)
    c.rect(4,oy,23,oy+7,'d')
    for x in range(5,23):
        if (x-5)%4==3:c.vline(x,oy+1,oy+7,'i')
    c.hline(4,23,oy,'i');c.hline(4,23,oy+1,'i')
    if stage:c.paste(cargo(stage,20),4,oy-8)
    c.hline(3,24,oy+8,'H');c.hline(3,24,oy+9,'h');c.hline(3,24,oy+10,'w');c.hline(3,24,oy+11,'d')
    c.hline(3,24,oy+12,'w');c.hline(3,24,oy+13,'m');c.hline(3,24,oy+14,'d')
    for x in (8,17):c.px(x,oy+10,'m');c.px(x+2,oy+13,'d')
    for x in (3,23):
        c.vline(x,oy-1,oy+14,'l');c.vline(x+1,oy-1,oy+14,'m');c.px(x,oy-1,'H');c.px(x+1,oy-1,'h')
    for x0 in (5,21):
        for y in range(oy+15,oy+24):c.px(x0,y,'h');c.px(x0+1,y,'d')
        c.px(x0,oy+24,'m');c.px(x0+1,oy+24,'d')
    c.outline();return c

# ---------------- 뒤 (위로 갈 때: v3 자리 그대로, 수레가 사람 아래) ----------------
# 놓는 법: 그림 가운데(위에서 19줄)를 사람 아래 0.95칸. 손잡이가 3줄 더 길어 사람 허리까지 올라온다
def back(stage=0):
    c=C(28,35);oy=13
    for x0 in (5,21):
        for y in range(1,oy):c.px(x0,y,'h');c.px(x0+1,y,'d')
        c.px(x0,1,'m');c.px(x0+1,1,'d')
    c.rect(4,oy,23,oy+6,'d')
    for x in range(5,23):
        if (x-5)%4==3:c.vline(x,oy+1,oy+6,'i')
    c.hline(4,23,oy,'i')
    if stage:c.paste(cargo(stage,20),4,oy-9)
    c.hline(3,24,oy+7,'H');c.hline(3,24,oy+8,'h');c.hline(3,24,oy+9,'w');c.hline(3,24,oy+10,'d')
    c.hline(3,24,oy+11,'w');c.hline(3,24,oy+12,'m');c.hline(3,24,oy+13,'d')
    for x in (9,16):c.px(x,oy+9,'m');c.px(x+2,oy+12,'d')
    for x in (3,23):
        c.vline(x,oy-1,oy+13,'l');c.vline(x+1,oy-1,oy+13,'m');c.px(x,oy-1,'H');c.px(x+1,oy-1,'h')
    for x in (6,21):c.vline(x,oy+14,oy+16,'d')
    c.outline()
    w=C(28,35);edge_wheel(w,0,oy+4,14);edge_wheel(w,25,oy+4,14);w.outline();c.paste(w,0,0)
    return c

if __name__=='__main__':
    out=Path(sys.argv[1] if len(sys.argv)>1 else '.');out.mkdir(parents=True,exist_ok=True)
    for s in range(4):
        side(s).im.save(out/f'handcart_side_{s}.png');front(s).im.save(out/f'handcart_front_{s}.png');back(s).im.save(out/f'handcart_back_{s}.png')
    side_handle().im.save(out/'handcart_side_handle.png')
