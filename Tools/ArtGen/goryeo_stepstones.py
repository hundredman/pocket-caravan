"""11/09 은샘 요청: 장단 강 징검다리 돌 (16x16, 16px 칸). 뾰족하지 않은 납작하고 둥근 돌.
장독대·좌판과 같은 #2e241d 테두리, 돌 회색 4단(#5f5c55·#77746c·#a29b87·#c4bca7), 왼쪽 위 빛, 아래 2줄은 젖은 옆면.
돌 밑에 물에 잠긴 1줄(강물보다 밝은 청록 #a8d5ca)만 두고, 튀어나온 물살 점은 없다.
실행: python Tools/ArtGen/goryeo_stepstones.py <출력 폴더>
"""
import sys
from pathlib import Path
from PIL import Image
def hx(s):return tuple(int(s[i:i+2],16) for i in (1,3,5))+(255,)
O=hx('#2e241d');ST=[hx('#5f5c55'),hx('#77746c'),hx('#a29b87'),hx('#c4bca7')];WET=hx('#a8d5ca')
def stone(w,h,top=6,lean=0.0):
    """w×h 몸통(테두리 포함 전), 그림 아래에서 3px 위에 앉는다"""
    im=Image.new('RGBA',(16,16),(0,0,0,0));p=im.load()
    x0=(16-w)//2;y0=top
    cx=x0+w/2;cy=y0+h/2;rx=w/2;ry=h/2
    inside=set()
    for y in range(y0,y0+h):
        for x in range(x0,x0+w):
            dx=(x+.5-cx)/rx-lean*((y+.5-cy)/ry);dy=(y+.5-cy)/ry
            if abs(dx)**2.6+abs(dy)**2.6<=1:inside.add((x,y))
    bottom={}
    for x,y in inside:bottom[x]=max(bottom.get(x,-1),y)
    topy={}
    for x,y in inside:topy[x]=min(topy.get(x,99),y)
    for x,y in inside:
        if y>=bottom[x]-1:c=ST[0] if y==bottom[x] else ST[1]   # 젖은 옆면 2줄
        elif y==topy[x] and x<cx+1:c=ST[3]                   # 위 가장자리 빛
        elif (x-x0)<w*.4 and (y-y0)<h*.45:c=ST[3] if (x+y)%3==0 else ST[2]
        else:c=ST[2]
        p[x,y]=c
    # 테두리
    ring=set()
    for x,y in inside:
        for a,b in((1,0),(-1,0),(0,1),(0,-1)):
            n=(x+a,y+b)
            if n not in inside:ring.add(n)
    for x,y in ring:
        if 0<=x<16 and 0<=y<16:p[x,y]=O
    # 물에 잠긴 줄: 테두리 바로 밑, 양끝 1칸 안쪽
    xs=sorted(bottom);ly=max(bottom.values())+2
    for x in range(xs[0],xs[-1]+1):
        if 0<=ly<16 and p[x,ly][3]==0:p[x,ly]=WET
    return im
VARIANTS=[(9,5,7,0.0),(8,4,8,0.08),(9,5,7,-0.08)]   # 11/09 돌 사이에 물이 보이게 작게
if __name__=='__main__':
    out=Path(sys.argv[1] if len(sys.argv)>1 else '.');out.mkdir(parents=True,exist_ok=True)
    for i,v in enumerate(VARIANTS):stone(*v).save(out/f'stepstone_{i}.png')
