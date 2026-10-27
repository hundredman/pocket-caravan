"""10/27 장터 좌판 새 그림 (48x48, 3칸 폭 · 3칸 높이): 빨강·크림 줄무늬 천 지붕(물결 가장자리), 나무 기둥 둘, 판자 계산대.
계산대 위 물건은 장꾼마다 다름 (생선·모피·꿀술·소금). 장꾼은 계산대 뒤에 서고(머리·몸 위쪽만 보임),
stall_*_front 는 계산대 앞판만 남긴 그림이라 장꾼 앞에 한 번 더 그려 몸 아래를 가린다 (NorthCraft.cs).
가운데(x 18~29)는 장꾼 자리라 물건을 두지 않는다."""
import os, sys
from PIL import Image, ImageDraw
O = (46, 34, 40, 255)

def stall(goods):
    W,H=48,48
    im=Image.new("RGBA",(W,H),(0,0,0,0)); px=im.load(); d=ImageDraw.Draw(im)
    RED,RED_D,RED_L=(196,58,52,255),(140,36,40,255),(226,98,82,255)
    CRM,CRM_D=(246,232,206,255),(214,192,160,255)
    WOOD,WOOD_D,WOOD_L=(150,96,58,255),(104,64,40,255),(186,128,80,255)
    # 기둥 (뒤 기둥 살짝 어둡게)
    for x in (4,5,42,43):
        for y in (10,37): pass
    d.rectangle((3,10,5,45),fill=WOOD_D,outline=O); d.rectangle((42,10,44,45),fill=WOOD_D,outline=O)
    d.line((4,11,4,44),fill=WOOD); d.line((43,11,43,44),fill=WOOD)
    # 지붕 천: 위로 볼록한 경사 + 줄무늬 6칸
    for y in range(2,12):
        inset=max(0,(5-y))
        for x in range(1+inset,47-inset):
            stripe=((x-1)//8)%2
            c=RED if stripe==0 else CRM
            if y>=9: c=RED_D if stripe==0 else CRM_D
            elif y==2 or (y<=4 and x in (1+inset,46-inset)): c=O
            elif y<=4: c=RED_L if stripe==0 else (255,246,228,255)
            px[x,y]=c
    for x in range(1,47): px[x,12]=O if True else 0
    for y in range(2,13): px[1 if y>=5 else 1+(5-y),y]=O; px[46 if y>=5 else 46-(5-y),y]=O
    # 물결 늘어진 가장자리 (반원 8칸마다)
    for i in range(6):
        cx=1+i*8+4
        col=RED if i%2==0 else CRM; dk=RED_D if i%2==0 else CRM_D
        for x in range(max(1,cx-4),min(47,cx+4)):
            dy=int(3-abs(x-cx+0.5)*0.75)
            for y in range(13,13+dy+1): px[x,y]=col if y<13+dy else dk
            px[x,13+dy+1]=O
    # 꼭대기 장식 장대
    d.line((24,0,24,2),fill=WOOD_D); px[24,0]=(240,200,80,255)
    # 계산대 (앞판)
    d.rectangle((2,35,45,46),fill=WOOD,outline=O)
    d.line((3,37,44,37),fill=WOOD_L)
    for x in (10,18,26,34): d.line((x,38,x,45),fill=WOOD_D)
    d.line((3,45,44,45),fill=WOOD_D)
    d.rectangle((1,33,46,35),fill=WOOD_L,outline=O)   # 계산대 윗판
    goods(im,d)
    return im
def fur(im,d):
    for (x,c,cd) in [(6,(170,120,80,255),(120,82,56,255)),(12,(222,214,200,255),(170,160,150,255)),(30,(140,96,64,255),(100,66,44,255)),(36,(196,150,100,255),(150,108,70,255))]:
        d.ellipse((x,27,x+7,33),fill=c,outline=O); d.line((x+2,29,x+4,28),fill=(255,250,240,255)); d.line((x+2,31,x+5,31),fill=cd)
def fish(im,d):
    for x in (5,30):
        for k in range(2):
            y=28+k*3
            d.ellipse((x,y,x+7,y+3),fill=(150,170,190,255),outline=O); d.polygon(((x+7,y+1),(x+10,y-1),(x+10,y+4)),fill=(110,130,150,255),outline=O); im.putpixel((x+2,y+1),O)
def mead(im,d):
    for x in (6,12,31,37):
        d.rectangle((x,26,x+4,33),fill=(176,110,60,255),outline=O); d.rectangle((x+1,24,x+3,26),fill=(120,80,50,255),outline=O); d.line((x+1,28,x+3,28),fill=(230,190,90,255))
def salt(im,d):
    for x in (5,11,30,36):
        d.polygon(((x,33),(x+1,28),(x+2,26),(x+5,26),(x+6,28),(x+7,33)),fill=(232,226,214,255),outline=O); d.line((x+2,28,x+5,28),fill=(150,110,80,255))


FRONT_Y = 33     # 계산대 윗판부터 아래 (장꾼 앞을 가리는 부분)


def sprites():
    out = {}
    for k, g in (("fish", fish), ("fur", fur), ("mead", mead), ("salt", salt)):
        im = stall(g)
        out["stall_" + k] = im
        front = Image.new("RGBA", im.size, (0, 0, 0, 0))
        front.alpha_composite(im.crop((0, FRONT_Y, 48, 48)), (0, FRONT_Y))
        out["stall_" + k + "_front"] = front
    return out
