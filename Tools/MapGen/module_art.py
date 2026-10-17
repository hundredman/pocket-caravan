"""기존 16px 타일 축척의 편집 가능한 간단 소품. 원형 라이브러리용 임시 아트."""
from PIL import Image,ImageDraw

def image_for(key):
    _,tx,ty=key.split(':');tx,ty=int(tx),int(ty)
    t=Image.new('RGBA',(112,48));d=ImageDraw.Draw(t)
    d.ellipse((1,23,31,31),fill='#313b3944')
    d.polygon([(2,25),(5,15),(12,10),(24,12),(30,22),(26,29),(10,29)],fill='#555d60')
    d.polygon([(5,17),(12,11),(24,13),(27,21),(12,21)],fill='#939e9b')
    d.polygon([(12,21),(27,21),(24,27),(10,27)],fill='#747e7a')
    d.line([(6,17),(12,13),(20,14)],fill='#bbc3ab',width=2)
    d.ellipse((35,40,61,46),fill='#313b3944')
    d.rectangle((46,6,52,43),fill='#4f7450');d.rectangle((47,5,50,42),fill='#89a85b')
    d.rectangle((37,19,46,25),fill='#5b824d');d.rectangle((36,13,40,23),fill='#769957')
    d.rectangle((52,27,60,32),fill='#5b824d');d.rectangle((57,19,61,29),fill='#769957')
    for x,y in [(48,11),(50,22),(48,34),(38,17),(59,24)]:d.point((x,y),fill='#e8d592')
    # Broken stone wall: complete 3x3 stamp, cracks do not change collision semantics.
    for x,y,w,h in [(66,18,12,25),(82,10,12,33),(96,23,13,20)]:
        d.rectangle((x,y,x+w,y+h),fill='#66716c');d.rectangle((x+1,y+1,x+w-2,y+6),fill='#a3ac92')
        for yy in range(y+8,y+h,8):d.line((x,yy,x+w,yy),fill='#424d49')
    d.line([(87,14),(85,22),(89,28),(86,38)],fill='#3c4840')
    return t.crop((tx*16,ty*16,(tx+1)*16,(ty+1)*16))
