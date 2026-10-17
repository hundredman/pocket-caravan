"""생성된 원형 썸네일40종을 한 장으로 확인한다."""
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from catalogue import ROOT

def render():
    entries=json.loads((ROOT/'Assets/PocketCaravan/MapModules/catalogue.json').read_text())['entries']
    font=ImageFont.truetype(str(ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf'),13)
    rows=(len(entries)+4)//5;out=Image.new('RGB',(1000,rows*232),'#1d282a');d=ImageDraw.Draw(out)
    for i,e in enumerate(entries):
        x=i%5*200;y=i//5*232;im=Image.open(ROOT/e['previewPath']).resize((192,192),Image.Resampling.NEAREST);out.paste(im,(x+4,y+3));d.text((x+6,y+199),e['label'],font=font,fill='#e8e7d3');d.text((x+6,y+216),e['category'],font=font,fill='#9fbaae')
    dest=ROOT/'output/module-library';dest.mkdir(parents=True,exist_ok=True);out.save(dest/'contact-sheet.png')
if __name__=='__main__':render()
