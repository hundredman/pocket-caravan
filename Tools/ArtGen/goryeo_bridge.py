"""11/08 은샘 요청: 장단 강 건너 논으로 가는 작은 나무 다리 코드 도트 (80x32 = 5x2칸, 16px).
장터 좌판·장독대와 같은 팔레트(#2e241d 테두리, 나무 갈색 4단), 왼쪽 위 빛. 판자는 세로(가로로 건너는 다리), 뒤 난간은 낮게, 앞은 테두리만(사람을 가리지 않게).
실행: python Tools/ArtGen/goryeo_bridge.py <출력 폴더>
"""
import sys
from pathlib import Path
from PIL import Image
def hx(s):return tuple(int(s[i:i+2],16) for i in (1,3,5))+(255,)
O=hx('#2e241d');D=hx('#34271f');J=[hx('#5b432f'),hx('#68452d'),hx('#86513b'),hx('#98653b'),hx('#b77c3e')]
W,H=80,32
def bridge():
    im=Image.new('RGBA',(W,H),(0,0,0,0));px=im.load()
    def put(x,y,c):
        if 0<=x<W and 0<=y<H:px[x,y]=c
    x0,x1=3,77
    # 물에 박힌 기둥 (데크 아래)
    for bx in (12,38,64):
        for y in range(23,30):
            for k in range(3):put(bx+k,y,J[0] if k else J[1])
        put(bx-1,29,O);put(bx+3,29,O)
        for k in range(-1,4):put(bx+k,30,O)
    # 데크: 세로 판자 4px
    for x in range(x0,x1):
        slat=(x-x0)//4;u=(x-x0)%4
        base=J[3] if slat%2==0 else J[2]
        for y in range(10,22):
            c=base
            if u==3:c=J[0]
            elif y==10:c=J[4] if u<2 else J[3]
            elif y>=20:c=J[1]
            put(x,y,c)
        # 판자 못
        if u==1:put(x,12,J[1]);put(x,18,J[1])
    # 앞 보(낮게) 
    for x in range(x0,x1):put(x,22,J[1]);put(x,23,O)
    # 뒤 난간: 기둥 + 가로대
    posts=(x0+1,26,50,x1-3)
    for p in posts:
        for y in range(2,10):
            put(p,y,J[3]);put(p+1,y,J[1])
        put(p,2,J[4])
    for x in range(x0+1,x1-1):
        put(x,4,J[3]);put(x,5,J[1])
        if x%7==0:put(x,4,J[4])
    # 테두리
    src=im.copy();sp=src.load()
    for y in range(H):
        for x in range(W):
            if sp[x,y][3]==0 and any(0<=x+a<W and 0<=y+b<H and sp[x+a,y+b][3] and sp[x+a,y+b]!=O for a,b in((1,0),(-1,0),(0,1),(0,-1))):px[x,y]=O
    return im
if __name__=='__main__':
    out=Path(sys.argv[1] if len(sys.argv)>1 else '.');out.mkdir(parents=True,exist_ok=True)
    bridge().save(out/'bridge.png')
