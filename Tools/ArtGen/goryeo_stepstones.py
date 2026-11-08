"""11/09 은샘 요청: 장단 강 징검다리 돌 2가지 (16x16, 16px 칸). 장독대 돌 단과 같은 돌 팔레트(#77746c·#a29b87·#c4bca7), #2e241d 테두리,
왼쪽 위 빛, 물에 닿는 아래쪽은 어둡게, 양옆에 물살 흰 점(게임 강 기슭의 흰 물거품 색).
실행: python Tools/ArtGen/goryeo_stepstones.py <출력 폴더>
"""
import sys
from pathlib import Path
from PIL import Image
def hx(s):return tuple(int(s[i:i+2],16) for i in (1,3,5))+(255,)
O=hx('#2e241d');ST=[hx('#5f5c55'),hx('#77746c'),hx('#a29b87'),hx('#c4bca7')];FOAM=(255,252,248,255)
def stone(rx,ry,cx=8,cy=10.5):
    im=Image.new('RGBA',(16,16),(0,0,0,0));p=im.load()
    for y in range(16):
        for x in range(16):
            dx=(x+.5-cx)/rx;dy=(y+.5-cy)/ry
            if dx*dx+dy*dy<=1:
                c=ST[2]
                if dy>.35:c=ST[0] if dy>.7 else ST[1]
                elif dx<-.2 and dy<-.1:c=ST[3]
                p[x,y]=c
    src=im.copy();s=src.load()
    for y in range(16):
        for x in range(16):
            if s[x,y][3]==0 and any(0<=x+a<16 and 0<=y+b<16 and s[x+a,y+b][3] for a,b in((1,0),(-1,0),(0,1),(0,-1))):p[x,y]=O
    # 물살: 돌 양 끝 아래에 흰 점 두세 개
    l=round(cx-rx)-1;r=round(cx+rx)
    for x,y in ((l-1,round(cy+ry*.4)),(l-2,round(cy+ry*.4)+1),(r+1,round(cy+ry*.4)),(r+2,round(cy+ry*.4)+1)):
        if 0<=x<16 and 0<=y<16 and p[x,y][3]==0:p[x,y]=FOAM
    return im
if __name__=='__main__':
    out=Path(sys.argv[1] if len(sys.argv)>1 else '.');out.mkdir(parents=True,exist_ok=True)
    stone(5.5,3.4).save(out/'stepstone_0.png')
    stone(4.6,3.0,cx=8.2,cy=10.8).save(out/'stepstone_1.png')
