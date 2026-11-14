"""11/14 은샘: 북유럽 짐 썰매 (2단계, 손수레 대신). 손수레(handcart_v3.py)와 같은 크기·짐칸·손잡이·짐 상자 0~3단계,
바퀴 대신 쇠를 댄 썰매 날(앞이 말려 올라감). 게임에서는 HandcartLook이 손수레와 같은 자리에 놓는다 (폴더만 Sled).
옆(side, 왼쪽으로 갈 때) · 앞(front, 아래로 갈 때 썰매가 사람 위) · 뒤(back, 위로 갈 때 썰매가 사람 아래) + 옆모습 손잡이 앞 겹.
실행: python Tools/ArtGen/sled_v1.py <출력 폴더>
"""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from handcart_v3 import C,cargo,side_handle

def runner_side(c):
    """옆모습 썰매 날: 바닥 한 줄(사람 발바닥 높이 = 29줄), 앞(왼쪽)이 말려 올라가 짐칸 앞까지"""
    c.hline(7,29,29,'k');c.hline(7,29,28,'g');c.px(29,28,'k')
    for x,y in ((6,28),(5,27),(4,26),(4,25),(5,24),(6,24)):c.px(x,y,'k')
    for x,y in ((6,27),(5,26),(5,25)):c.px(x,y,'s')
    for x in (12,19,26):c.vline(x,26,27,'d');c.vline(x+1,26,27,'m')   # 날과 짐칸을 잇는 짧은 다리

def side(stage=0):
    c=C(32,30)
    for x in range(0,10):c.px(x,21,'h');c.px(x,22,'d')     # 수평 손잡이 (손수레와 같은 자리)
    c.px(0,21,'m')
    oy=7
    c.hline(9,28,10+oy,'i');c.hline(9,28,11+oy,'d')
    if stage:c.paste(cargo(stage,20),9,1+oy-4)
    c.hline(9,28,12+oy,'H');c.hline(9,28,13+oy,'h');c.hline(9,28,14+oy,'w');c.hline(9,28,15+oy,'d')
    c.hline(9,28,16+oy,'w');c.hline(9,28,17+oy,'m');c.hline(9,28,18+oy,'d')
    for x in (13,22):c.px(x,14+oy,'m');c.px(x+1,17+oy,'d')
    for x in (9,18,27):
        c.vline(x,9+oy,18+oy,'l');c.vline(x+1,9+oy,18+oy,'m');c.px(x,9+oy,'H');c.px(x+1,9+oy,'h')
    runner_side(c)
    c.outline();return c

def rails(c,x0,y0,y1,curl_down):
    """앞·뒤에서 본 썰매 날 두 줄 (짐칸 양옆 바깥). curl_down이면 아래 끝(사람 쪽)이 말려 있다"""
    for x in (x0,):
        for y in range(y0,y1+1):
            c.px(x,y,'k');c.px(x+1,y,'s' if y==y0 else 'g');c.px(x+2,y,'k')
        end=y1+1 if curl_down else y0-1
        c.px(x+1,end,'k')

def front(stage=0):
    c=C(28,32);oy=6
    rails(c,0,oy+1,oy+16,True);rails(c,25,oy+1,oy+16,True)
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
    c.outline()
    w=C(28,35);rails(w,0,oy+2,oy+17,False);rails(w,25,oy+2,oy+17,False);w.outline();c.paste(w,0,0)
    return c

if __name__=='__main__':
    out=Path(sys.argv[1] if len(sys.argv)>1 else '.');out.mkdir(parents=True,exist_ok=True)
    for s in range(4):
        side(s).im.save(out/f'side_{s}.png');front(s).im.save(out/f'front_{s}.png');back(s).im.save(out/f'back_{s}.png')
    side_handle().im.save(out/'side_handle.png')
