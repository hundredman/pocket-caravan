"""Native reference-tile overview of T3 boundary proposal (no artwork editing)."""
from pathlib import Path
import json,sys
import numpy as np
from PIL import Image,ImageDraw,ImageFont
sys.path.insert(0,str(Path(__file__).resolve().parent))
import t3_boundaries as t
from m4_export import MAPID
FONT='/System/Library/Fonts/AppleSDGothicNeo.ttc'
def main():
    config,src=t.read_sources();maps,report,tiles,owners,known=t.generate(config,src)
    cache={};canvas=Image.new('RGBA',(4800,4160),(34,43,34,255))
    def tile(name):
        if name not in cache:
            p=t.ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/f'{name}.png'
            cache[name]=Image.open(p).convert('RGBA')
        return cache[name]
    for y,x in zip(*np.nonzero(known)):
        for layer in t.LAYERS:
            name=tiles[layer][y,x]
            if name:canvas.alpha_composite(tile(name),(int(x)*16,int(y)*16))
    t.REVIEW.mkdir(parents=True,exist_ok=True)
    # The 3px/cell overview is for geometry; Unity captures retain game screen detail.
    base=canvas.convert('RGB').resize((900,780),Image.Resampling.NEAREST)
    old=np.load(t.ROOT/'ArtReview/M1-Tiles/masks.npz');oldkeys=[str(k)for k in old['keys']];mask=old['mapid'];own=old['own']
    left=base.copy();right=base.copy();dl,dr=ImageDraw.Draw(left),ImageDraw.Draw(right)
    edge=np.zeros_like(own)
    edge[1:]|=own[1:]&own[:-1]&(mask[1:]!=mask[:-1]);edge[:,1:]|=own[:,1:]&own[:,:-1]&(mask[:,1:]!=mask[:,:-1])
    for y,x in zip(*np.nonzero(edge)):dl.rectangle((x*3,y*3,x*3+1,y*3+1),fill='#ffe18a')
    f=ImageFont.truetype(FONT,16)
    for row in report['maps']:
        mid=row['mapId'];x,y,w,h=row['box'];dr.rectangle((x*3,(y+130)*3,(x+w)*3-1,(y+h+130)*3-1),outline='#ffe18a',width=2)
        d=maps[mid];ref=next((o for o in d['objects']if o['kind']=='market'),next(o for o in d['objects']if o['kind']=='spawn'and o['id']=='S0'))
        p=((x+ref['x'])*3,(y+130+ref['y'])*3)
        label=d['name'].replace(' 마을','').replace('예성 나루','나루').replace('닥나무','닥목').replace(' 들마을','').replace(' 산록','')
        for draw in (dl,dr):draw.text(p,label,font=f,fill='white',stroke_width=2,stroke_fill='#1e2920')
    out=Image.new('RGB',(1836,856),'#eee9df');draw=ImageDraw.Draw(out);title=ImageFont.truetype(FONT,22)
    draw.text((12,8),'현재: 불규칙한 내부 경계',font=title,fill='#292824');draw.text((930,8),'검토안: 마을을 유지하고 외곽 길목에서 전환',font=title,fill='#292824')
    out.paste(left,(12,42));out.paste(right,(930,42));draw.text((12,827),'노란 선은 구역 경계 · 마을/주요 지형 유지 · 서쪽 끝 4칸 축소 시안 · 본편 미적용',font=f,fill='#292824')
    out.save(t.REVIEW/'boundary-overview.png')
    # Per-map full terrain at native pixels, useful to inspect real extent/cropping.
    for mid,d in maps.items():
        x,y,w,h=d['terrainX'],d['terrainY']+130,d['w'],d['h']
        canvas.crop((x*16,y*16,(x+w)*16,(y+h)*16)).save(t.REVIEW/f'{mid}-terrain.png')
    if all((t.REVIEW/f'{mid}-game.png').exists()for mid in maps):
        sheet=Image.new('RGB',(1316,912),'#eee9df');draw=ImageDraw.Draw(sheet)
        draw.text((12,8),'13맵 경계 검토본 · 실제 Unity 화면 · 본편 미적용',font=title,fill='#292824')
        for i,(mid,data)in enumerate(maps.items()):
            x=12+(i%4)*326;y=42+(i//4)*216
            draw.text((x,y),data['name'],font=f,fill='#292824')
            image=Image.open(t.REVIEW/f'{mid}-game.png').convert('RGB').resize((320,180),Image.Resampling.NEAREST)
            sheet.paste(image,(x,y+26))
        sheet.save(t.REVIEW/'13maps-game-mosaic.png')
        before=t.ROOT/'ArtReview/Map-Boundary-Audit-20261107/Naru-current.png'
        after=t.REVIEW/'Naru-edge.png'
        if before.exists()and after.exists():
            comparison=Image.new('RGB',(984,340),'#eee9df');draw=ImageDraw.Draw(comparison)
            for x,path,label in ((12,before,'현재: 화면 중앙에서 전환'),(498,after,'검토본: 카메라가 멈추고 왼쪽 끝에서 전환')):
                draw.text((x,8),label,font=f,fill='#292824')
                image=Image.open(path).convert('RGB').resize((474,267),Image.Resampling.NEAREST);comparison.paste(image,(x,36))
            draw.text((12,311),'실제 외곽까지 걸어서 이동 · 건물/숲/논 유지 · 페이드 변경 없음 · 본편 미적용',font=f,fill='#292824')
            comparison.save(t.REVIEW/'Naru-before-after.png')
    print('Rendered 13 native terrain views and overview; source artwork unchanged')
if __name__=='__main__':main()
