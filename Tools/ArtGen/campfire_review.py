"""Collate actual Unity captures. Never paints/changes the fire or flame assets."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/Goryeo-Seats-20261105/Motion'


def build():
    samples=json.loads((OUT/'samples.json').read_text())
    frames=Path(samples['framesPath'])
    font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',18)
    sequence=[]
    for i in range(20):
        page=Image.new('RGB',(968,312),'#eee9df');d=ImageDraw.Draw(page)
        for col,(key,label) in enumerate([('day','낮 · 실제 게임 480×270'),('night','밤 · 실제 게임 480×270')]):
            d.text((col*488+4,6),label,font=font,fill='#302e2a')
            with Image.open(frames/f'{key}-{i:02}.png') as im:page.paste(im,(col*488,36))
        sequence.append(page)
    sequence[0].save(OUT/'campfire-motion.gif',save_all=True,append_images=sequence[1:],duration=120,loop=0,optimize=True)
    board=Image.new('RGB',(984,646),'#eee9df');d=ImageDraw.Draw(board)
    d.text((12,8),'모닥불 검토 · 왼쪽 기존 고정 / 오른쪽 움직임 추가',font=font,fill='#302e2a')
    for row,phase in enumerate(['day','night']):
        y=40+row*302;d.text((12,y),'낮' if phase=='day' else '밤',font=font,fill='#302e2a')
        for col,state in enumerate(['static','animated']):
            with Image.open(OUT/f'{phase}-{state}.png') as im:board.paste(im,(8+488*col,y+25))
    board.save(OUT/'comparison.png')
    art=[]
    for i in range(4):
        p=ROOT/f'Assets/PocketCaravan/Resources/North/flame_{i}.png'
        with Image.open(p) as im:
            rgba=im.convert('RGBA');art.append(dict(path=str(p.relative_to(ROOT)),size=list(rgba.size),rgbaSha256=hashlib.sha256(rgba.tobytes()).hexdigest()))
    (OUT/'submission.json').write_text(json.dumps(dict(status='review',production=False,flameFps=7,emberLimit=3,lights=1,animationReplayMs=120,art=art),ensure_ascii=False,indent=2)+'\n')


if __name__=='__main__':build()
