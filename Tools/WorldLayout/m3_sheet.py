"""M3 마을 화면 모음(게임 크기 480×272, 1배). 출력: ArtReview/M3-Village/sheet.png"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];D=ROOT/'ArtReview/M3-Village'
F=ImageFont.truetype(str(ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf'),16)
L=[('naru','나루'),('jangdan','장단'),('songak','송악'),('dakmok','닥목'),('gaegyeong','개경'),('woodtown','동문 마을'),
   ('ferry','북동 포구'),('southport','남동 포구'),('crossroads','중앙 길목 마을'),('rest_north','북쪽 쉼터 마을'),('rest_lake','남쪽 호숫가 마을')]
C=3;W,H=480,272;pad=24
sh=Image.new('RGB',(C*(W+8),((len(L)+C-1)//C)*(H+pad+8)),(20,20,24));d=ImageDraw.Draw(sh)
for i,(s,n) in enumerate(L):
    x,y=(i%C)*(W+8),(i//C)*(H+pad+8)
    im=Image.open(D/f'cam-{s}.png').resize((W,H),Image.NEAREST);sh.paste(im,(x,y+pad));d.text((x+4,y+3),n,fill='white',font=F)
sh.save(D/'sheet.png');print(sh.size)
