"""Build review material/payloads for native water modules; source maps remain untouched."""
from pathlib import Path
import json,sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/A4-Water-v1'
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'));sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from water_modules_a4 import fixtures,names,topology
import tiles

def payloads():
    (OUT/'reference-tiles').mkdir(exist_ok=True);wet=fixtures()['lake'];rows=[]
    for x,y in sorted(wet):
        key=tiles.autotile(lambda a,b:(a,b) in wet,x,y,*tiles.WATER_ORIGIN)+'@goryeo_jade'
        file=key.replace(':','_').replace('@','_')+'.png';tiles.tile_image(key).save(OUT/'reference-tiles'/file)
        rows.append(f'{x},{16-y},{names()[topology(wet,x,y)]},{file}')
    (OUT/'unity-lake-placement.csv').write_text('\n'.join(rows)+'\n')

def boards():
    font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
    board=Image.new('RGB',(1000,680),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((16,8),'A4 물·연못 조립 시안 · 실제 Unity 화면',font=font(24),fill='#332d2a')
    for row,(slug,title) in enumerate([('pond','개경 연못 · 기존 물 영역/연꽃 유지'),('lake','호수 조립 시험 · 같은 형태의 풀밭 시험장(본편 아님)')]):
        y=46+row*312;d.text((16,y),title,font=font(19),fill='#332d2a')
        for x,version,label in [(12,'before','기존 조각'),(508,'after','새 조립 조각 · 승인 대기')]:
            path=OUT/f'game-{slug}-{version}.png'
            if not path.exists():continue
            im=Image.open(path).convert('RGB');assert im.size==(1920,1080)
            im=im.resize((480,270),Image.Resampling.NEAREST);im.save(OUT/f'native-{slug}-{version}.png')
            d.text((x,y+24),label,font=font(14),fill='#685b51');board.paste(im,(x,y+42))
    board.save(OUT/'game-comparison.png')
    board=Image.new('RGB',(1000,520),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((16,10),'개경 연못 확대 · 동일 4배 · 주변 바닥과 연결',font=font(24),fill='#332d2a')
    for x,version,label in [(16,'before','기존'),(512,'after','시안')]:
        im=Image.open(OUT/f'native-pond-{version}.png');crop=im.crop((20,142,142,220)).resize((488,312),Image.Resampling.NEAREST)
        d.text((x,52),label,font=font(19),fill='#332d2a');board.paste(crop,(x,82))
    d.text((16,414),'물·연꽃 위치 유지 / 녹색 네모 바닥·흰 테두리 대신 주변 돌마당과 연결',font=font(20),fill='#332d2a')
    d.text((16,450),'실제 게임 픽셀은 위 전체 화면의 각 480×270 패널에서 확인',font=font(17),fill='#685b51');board.save(OUT/'pond-detail-comparison.png')
    # Actual native modules and approved player reference at the same integer scale.
    sheet=Image.open(OUT/'water-sheet.png').convert('RGBA');board=Image.new('RGB',(1000,560),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((16,10),'조립 원본 · 47종 + 물결 4프레임 · 16px / PPU16',font=font(24),fill='#332d2a')
    board.paste(sheet.resize((512,448),Image.Resampling.NEAREST),(20,70),sheet.resize((512,448),Image.Resampling.NEAREST))
    player=Image.open(ROOT/'Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png').convert('RGBA');board.paste(player.resize((64,96),Image.Resampling.NEAREST),(580,100),player.resize((64,96),Image.Resampling.NEAREST));d.text((568,204),'기존 플레이어 · 같은 4배',font=font(16),fill='#332d2a')
    for yy,text in enumerate(['가장자리 4방향','바깥/안쪽 모서리 각 4방향','단독·끝·좁은 연결 조각 포함','맵 생성 중 회전/색 변환 없음','색은 시안용 · D3 미확정']):d.text((570,265+yy*38),text,font=font(20),fill='#332d2a')
    board.save(OUT/'module-sheet-review.png')
    if (OUT/'review-decision.json').exists():
        board=Image.new('RGB',(1000,690),'#f5f0e6');d=ImageDraw.Draw(board)
        d.text((16,10),'피드백 반영 · 작은 연못만 검토 / 호수는 기존 표현 유지',font=font(23),fill='#332d2a')
        for row,(title,items) in enumerate([
            ('작은 연못 · 최종 승인 대기',[(12,'pond-before','기존'),(508,'pond-after','경계 수정 시안 · 검토 유지')]),
            ('큰 호수 · 새 시안 미채택',[(12,'lake-after','미채택 시안'),(508,'lake-before','기존 표현 유지')])]):
            y=48+row*314;d.text((16,y),title,font=font(19),fill='#332d2a')
            for x,slug,label in items:
                d.text((x,y+24),label,font=font(15),fill='#685b51');board.paste(Image.open(OUT/f'native-{slug}.png').convert('RGB'),(x,y+46))
        board.save(OUT/'feedback-comparison.png')

    if all((OUT/f'game-lake-ripple-{i}.png').exists() for i in range(4)):
        sequence=[Image.open(OUT/f'game-lake-ripple-{i}.png').convert('RGB').resize((480,270),Image.Resampling.NEAREST) for i in range(4)]
        sequence[0].save(OUT/'native-lake-ripples.gif',save_all=True,append_images=sequence[1:],duration=180,loop=0,disposal=2)

if __name__=='__main__':
    payloads()
    if (OUT/'game-lake-after.png').exists():boards()
