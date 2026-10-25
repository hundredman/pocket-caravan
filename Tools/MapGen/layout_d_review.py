"""Assemble comparison sheets from frozen/current map tiles, without changing assets."""
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from layout_d import ROOT,OUT

FONT=ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf'


def overview(maps,tiles,source=None):
    x0=min(m['terrainX'] for m in maps);y0=min(m['terrainY'] for m in maps)
    x1=max(m['terrainX']+m['w'] for m in maps);y1=max(m['terrainY']+m['h'] for m in maps)
    result=Image.new('RGB',((x1-x0)*16,(y1-y0)*16),'#1d292d');cache={}
    for m in maps:
        for layer in ('ground','deco','obj','high'):
            for i,index in enumerate(m[layer]):
                if not index:continue
                key=m['palette'][index]
                if key not in cache:cache[key]=Image.open(tiles/(key+'.png')).convert('RGBA')
                image=cache[key];px=(m['terrainX']+i%m['w']-x0)*16;py=(m['terrainY']+i//m['w']-y0)*16
                result.paste(image,(px,py),image)
    return result


def main():
    previous=[json.loads(f.read_text()) for f in sorted((ROOT/'Assets/PocketCaravan/Maps').glob('*.json'))]
    revised=[json.loads(f.read_text()) for f in sorted((OUT/'Maps').glob('*.json'))]
    before=overview(previous,ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles')
    after=overview(revised,OUT/'Tiles')
    before.save(OUT/'current-world-native.png');after.save(OUT/'layoutD-world-native.png')
    sheet=Image.new('RGB',(1328,760),'#172025');d=ImageDraw.Draw(sheet);font=ImageFont.truetype(str(FONT),18)
    for title,im,x in [('현재 본편 · 건물/NPC 포함',before,16),('B2 새 지형 검토 · 건물/NPC는 B3·B4',after,680)]:
        d.text((x,14),title,fill='white',font=font)
        scale=min(632/im.width,650/im.height);im=im.resize((int(im.width*scale),int(im.height*scale)),Image.Resampling.NEAREST)
        sheet.paste(im,(x+(632-im.width)//2,58))
    d.text((16,718),'전체 모양 비교용 축소 그림 · 실제 16px 게임 크기는 별도 Unity 카메라 그림',fill='#d9e5df',font=font)
    sheet.save(OUT/'comparison-world.png')
    shots=['current-naru-camera.png','layoutD_goryeo_나루-camera.png','layoutD_goryeo_닥목-camera.png']
    if all((OUT/f).is_file() for f in shots):
        sheet=Image.new('RGB',(1480,346),'#172025');d=ImageDraw.Draw(sheet)
        for i,(file,title) in enumerate(zip(shots,['현재 본편 나루','B2 나루 지형 검토','B2 닥목 지형 검토'])):
            d.text((12+i*492,12),title,fill='white',font=font)
            sheet.paste(Image.open(OUT/file),(12+i*492,48))
        d.text((12,324),'Unity Edit mode 카메라 480×270 · 새 지형은 미승인/본편 미적용',fill='#d9e5df',font=ImageFont.truetype(str(FONT),14))
        sheet.save(OUT/'comparison-game-size.png')


def natural_comparison():
    before=Image.open(OUT/'rejected-straight-world-native.png').convert('RGB')
    after=Image.open(OUT/'layoutD-world-native.png').convert('RGB')
    font=ImageFont.truetype(str(FONT),18)
    sheet=Image.new('RGB',(1328,720),'#172025');d=ImageDraw.Draw(sheet)
    for title,im,x in [('이전 시안 · 직선 위주의 길 (미승인)',before,16),('수정 시안 · 완만한 둑길과 합류',after,680)]:
        d.text((x,14),title,fill='white',font=font)
        scale=min(632/im.width,610/im.height)
        im=im.resize((int(im.width*scale),int(im.height*scale)),Image.Resampling.NEAREST)
        sheet.paste(im,(x+(632-im.width)//2,58))
    d.text((16,676),'마을 위치·해안·강·산맥은 유지 · 길 모양과 실제 보행 거리를 다시 계산',fill='#d9e5df',font=font)
    sheet.save(OUT/'natural-roads-comparison-world.png')
    sample=json.loads((OUT/'camera-sample.json').read_text());cx,cy=sample['globalCell']
    left=(cx-sample['terrainX'])*16+8-240;top=(cy-sample['terrainY'])*16+8-135
    sheet=Image.new('RGB',(1008,356),'#172025');d=ImageDraw.Draw(sheet)
    for title,im,x in [('이전 길 · 같은 위치 480×270',before,16),('수정 길 · 같은 위치 480×270',after,512)]:
        d.text((x,12),title,fill='white',font=font);sheet.paste(im.crop((left,top,left+480,top+270)),(x,48))
    d.text((16,326),'원본 타일 배치로 만든 실제 크기 비교 · 별도 Unity 카메라 그림도 제공',fill='#d9e5df',font=ImageFont.truetype(str(FONT),14))
    sheet.save(OUT/'natural-roads-comparison-native.png')


if __name__=='__main__':
    main()
    if (OUT/'rejected-straight-world-native.png').is_file():natural_comparison()
