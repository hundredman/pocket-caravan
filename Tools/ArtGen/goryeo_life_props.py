"""11/08 은샘 요청: 고려 마을 생활 소품(장독대·빨래줄 2장) 코드 도트. 장터 좌판·수레와 같은 팔레트(#2e241d 테두리, 옹기 갈색, 돌 회색, 쪽빛·무명색), 왼쪽 위 빛, 16px 칸.
실행: python Tools/ArtGen/goryeo_life_props.py <출력 폴더>
"""
from PIL import Image
def hx(s):return tuple(int(s[i:i+2],16) for i in (1,3,5))+(255,)
O=hx('#2e241d')
class C:
    def __init__(s,w,h):s.im=Image.new('RGBA',(w,h),(0,0,0,0));s.w,s.h=w,h
    def px(s,x,y,c):
        if 0<=x<s.w and 0<=y<s.h:s.im.putpixel((x,y),c)
    def get(s,x,y):return s.im.getpixel((x,y)) if 0<=x<s.w and 0<=y<s.h else (0,0,0,0)
    def outline(s,col=O):
        pts=[]
        for y in range(s.h):
            for x in range(s.w):
                if s.get(x,y)[3]==0 and any(s.get(x+dx,y+dy)[3]>0 and s.get(x+dx,y+dy)!=col for dx,dy in((1,0),(-1,0),(0,1),(0,-1))):pts.append((x,y))
        for x,y in pts:s.px(x,y,col)
def ell(c,cx,cy,rx,ry,shade):
    # shade(dx,dy) -> color ; dx,dy normalized
    for y in range(int(cy-ry)-1,int(cy+ry)+2):
        for x in range(int(cx-rx)-1,int(cx+rx)+2):
            dx=(x+.5-cx)/rx;dy=(y+.5-cy)/ry
            if dx*dx+dy*dy<=1:c.px(x,y,shade(dx,dy))

J=[hx('#34271f'),hx('#5b432f'),hx('#68452d'),hx('#86513b'),hx('#98653b'),hx('#b77c3e')]
ST=[hx('#77746c'),hx('#a29b87'),hx('#c4bca7')]
def prof(t):
    pts=[(0,.55),(.12,.8),(.32,1.0),(.6,.92),(1,.62)]
    for (a,wa),(b,wb) in zip(pts,pts[1:]):
        if a<=t<=b:return wa+(wb-wa)*(t-a)/(b-a)
    return .62
def jar(c,cx,base,hw,h):
    top=base-h
    for y in range(top,base):
        t=(y-top)/(h-1)
        half=hw*prof(t)
        x0=round(cx-half);x1=round(cx+half)
        for x in range(x0,x1):
            u=(x-x0)/max(1,x1-x0-1)
            col=J[4] if u<.22 else J[3] if u<.45 else J[2] if u<.75 else J[1]
            if t>.85:col=J[1] if u>.3 else J[2]
            c.px(x,y,col)
    # 하이라이트 점
    c.px(round(cx-hw*.55),top+round(h*.3),J[5]);c.px(round(cx-hw*.55),top+round(h*.3)+1,J[4])
    # 뚜껑
    lw=round(hw*.55)
    for x in range(round(cx-lw)-1,round(cx+lw)+1):c.px(x,top-1,J[2])
    for x in range(round(cx-lw),round(cx+lw)):c.px(x,top-2,J[3])
    c.px(round(cx),top-3,J[1]);c.px(round(cx)-1,top-3,J[1])
def jangdok():
    c=C(32,32)
    jar(c,10,27,6.5,13)
    jar(c,22,27,7,15)
    c.outline()
    jar(c,16,29,4.5,8)   # 앞 작은 단지
    c.outline()
    # 돌 단
    for y in range(28,31):
        for x in range(1,31):
            if c.get(x,y)[3]==0 or c.get(x,y)==O:
                c.px(x,y,ST[2] if y==28 else ST[1] if y==29 else ST[0])
    for x in (6,13,20,27):c.px(x,29,ST[0])
    c.outline();return c.im
W=[hx('#5b432f'),hx('#86513b'),hx('#b77c3e')]
CL=[hx('#ddc696'),hx('#f2dfb3'),hx('#f6e4bf')]
BL=[hx('#3d4a6b'),hx('#576b90'),hx('#7c97b5')]
ROPE=hx('#c4bca7')
def laundry(f):
    c=C(48,32)
    sag=lambda x:7+round(2*(1-((x-24)/21)**2))
    def cloth(x0,x1,length,pal):
        for x in range(x0,x1):
            top=sag(x)+1
            for k in range(length):
                y=top+k
                xx=x+(1 if f and k>=length-4 else 0)
                col=pal[2] if x==x0 else pal[0] if (x==x1-1 or k>=length-2) else pal[1]
                c.px(xx,y,col)
            # 가운데 접힌 주름 한 줄
        for k in range(2,length-3):c.px((x0+x1)//2,sag((x0+x1)//2)+1+k,pal[0])
    cloth(8,17,12,CL)
    cloth(19,26,10,BL)
    cloth(28,39,11,CL)
    c.outline()
    for px_ in (3,44):
        for y in range(5,31):c.px(px_,y,W[1]);c.px(px_+1,y,W[0])
        c.px(px_,5,W[2]);c.px(px_-1,30,O);c.px(px_+2,30,O)
    c.outline()
    for x in range(5,44):
        if c.get(x,sag(x)) in (O,(0,0,0,0)):c.px(x,sag(x),ROPE)
    return c.im


def jar2(cx,base,hw,h):
    c=C(32,32);top=base-h
    for y in range(top,base):
        t=(y-top)/(h-1);half=hw*prof(t);x0=round(cx-half);x1=round(cx+half)
        for x in range(x0,x1):
            u=(x-x0)/max(1,x1-x0-1)
            col=J[4] if u<.22 else J[3] if u<.45 else J[2] if u<.75 else J[1]
            if t>.85:col=J[1] if u>.3 else J[2]
            c.px(x,y,col)
    c.px(round(cx-hw*.55),top+round(h*.3),J[5]);c.px(round(cx-hw*.55),top+round(h*.3)+1,J[4])
    lw=round(hw*.6)
    for x in range(round(cx-lw),round(cx+lw)):c.px(x,top-1,J[2]);c.px(x,top-2,J[3])
    c.px(round(cx-lw),top-2,J[5])
    for x in range(round(cx-2),round(cx+1)):c.px(x,top-3,J[2])
    c.outline();return c.im
def jars():
    c=C(32,32)
    for y in range(28,31):
        for x in range(1,31):c.px(x,y,ST[2] if y==28 else ST[1] if y==29 else ST[0])
    for x in (6,13,20,27):c.px(x,29,ST[0])
    c.outline();im=c.im
    for args in [(10,28,6.5,13),(22,28,7,15),(16,30,4.5,8)]:im.alpha_composite(jar2(*args))
    return im

SACK=[hx('#98653b'),hx('#c18b52'),hx('#ddc696'),hx('#f2dfb3')]
def sack():
    """짐 꾸러미(볏짚 자루) 16x16: 묶은 목, 불룩한 몸, 왼쪽 위 빛"""
    c=C(16,16)
    rows={4:(6,9),5:(5,10),6:(4,11),7:(3,12),8:(3,12),9:(2,13),10:(2,13),11:(2,13),12:(3,12),13:(3,12)}
    for y,(x0,x1) in rows.items():
        for x in range(x0,x1+1):
            u=(x-x0)/max(1,x1-x0)
            col=SACK[3] if (u<.3 and y<10) else SACK[2] if u<.6 else SACK[1] if u<.85 else SACK[0]
            if y>=12:col=SACK[1] if u<.7 else SACK[0]
            c.px(x,y,col)
    for x in range(6,10):c.px(x,5,SACK[0])          # 묶은 끈
    c.px(7,2,SACK[2]);c.px(8,2,SACK[2]);c.px(6,3,SACK[2]);c.px(9,3,SACK[1])   # 위로 삐죽 나온 자루 끝
    c.px(5,8,SACK[0]);c.px(10,10,SACK[0])          # 천 주름
    c.outline();return c.im
def tidy(l):
    px=l.load()
    for y in range(l.height):
        for x in range(l.width):
            if px[x,y]!=O:continue
            nb=[px[x+dx,y+dy] for dx,dy in((1,0),(-1,0),(0,1),(0,-1)) if 0<=x+dx<l.width and 0<=y+dy<l.height]
            fill=[p for p in nb if p[3]>0 and p!=O]
            if len(fill)>=3:px[x,y]=fill[0]
            elif not fill:px[x,y]=(0,0,0,0)
    return l
if __name__=='__main__':
    import sys,os
    out=sys.argv[1] if len(sys.argv)>1 else '.'
    jars().save(os.path.join(out,'jars.png'))
    for f in (0,1):tidy(laundry(f)).save(os.path.join(out,f'laundry_{f}.png'))
    sack().save(os.path.join(out,'sack.png'))
