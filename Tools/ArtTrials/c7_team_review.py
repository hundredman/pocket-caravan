"""Read-only Figma reference / previous trial / new trial comparison boards."""
import json
from PIL import Image,ImageDraw
from c7_team_prepare import ROOT,OUT
from c7_catalogue import rgba_evidence
from c7_review import text,sprite

OLD=ROOT/'ArtReview/C7-Rebuild-20261027'


def ref(board,name,xy,size):
    with Image.open(OUT/'Reference'/name) as im:
        board.alpha_composite(im.convert('RGBA').resize(size,Image.Resampling.NEAREST),xy)


def run():
    dest=OUT/'Submission-v5';dest.mkdir(exist_ok=True)
    board=Image.new('RGBA',(1400,1070),'#f6f0e3');d=ImageDraw.Draw(board)
    text(d,(22,12),'C7 팀 그림체 다시 맞추기 · 직전 시안 / 수정안 / Figma 원본',26)
    text(d,(22,50),'고려 그림은 같은 도트 크기 ×3. 팀 자료는 Figma 캡처 축소 참고(원본 도트 배율 미확인).',17)
    for x,label in [(24,'직전 시안 · 수정 요청'),(300,'이번 수정안 · 승인 대기'),(610,'Figma 팀 에셋 · 읽기 전용 확인')]:text(d,(x,83),label,19)
    for y,label,name in [(116,'초가집','thatch'),(326,'기와집','tile-house'),(536,'소나무 · 이전 픽셀 유지','pine')]:
        text(d,(24,y),label,19)
        sprite(board,OLD/'Native'/(name+'.png'),(24,y+34))
        sprite(board,OUT/'Native'/(name+'.png'),(300,y+34))
    ref(board,'figma-78-11.png',(610,120),(384,288))
    ref(board,'figma-92-31.png',(1030,120),(204,223))
    text(d,(610,416),'베네치아 78:11 / 아라비아 92:31 · 큰 색면과 분명한 모양',16)
    ref(board,'figma-92-32.png',(610,450),(315,241))
    ref(board,'figma-92-102.png',(1010,470),(128,128))
    ref(board,'figma-92-107.png',(1170,470),(128,128))
    text(d,(1000,620),'아라비아 길·풀 · 짧은 정형 무늬',16)
    text(d,(24,756),'풀·흙 · 경계47종 / 가운데 변형6종 · 새 시안',21)
    for i,name in enumerate(['grass-255','grass-center-1','grass-110','grass-38','grass-127','dirt','dirt-1']):
        sprite(board,OUT/'Native'/(name+'.png'),(24+i*110,800),4)
        text(d,(24+i*110,872),name,12)
    text(d,(24,934),'이번 변경: 작은 명암·빽빽한 지붕 선 축소 → 넓은 색면, 소수의 장식, 또렷한 외곽선.',18)
    text(d,(24,968),'64×48 캔버스·발밑 유지. 집의 가시 높이는 새 형태에 따라 39/37px(직전보다 낮음).',18)
    text(d,(24,1002),'시안용 팔레트이며 D3 확정 아님. 팀 그림은 참고로만 사용했고 게임에 복사하지 않았습니다.',17)
    board.convert('RGB').save(dest/'style-comparison.png')
    game=Image.new('RGBA',(1000,700),'#f6f0e3');d=ImageDraw.Draw(game)
    text(d,(14,12),'실제 Unity Play · 직전 시안 / 팀 그림체 수정 · 480×270',24)
    for x,name,label in [(12,'before','직전 시안'),(508,'trial','이번 수정안 · 승인 대기')]:
        text(d,(x,54),label,20)
        with Image.open(OUT/'Screens-v5'/(name+'.png')) as im:
            assert im.size==(480,270);game.alpha_composite(im.convert('RGBA'),(x,88))
    text(d,(14,383),'같은 시험장 길·캐릭터·연못 / PPU16 / Point. 실제 B3 마을 배치는 아닙니다.',17)
    text(d,(14,424),'플레이어·아낙·촌장·작은 연못은 기존 승인본, 나무는 직전 시안 그대로입니다.',17)
    text(d,(14,473),'제작자 판단: 플레이어·사람 NPC 수정 불필요',21)
    text(d,(14,508),'얼굴과 윤곽이 구분되고 밝은 큰 색 덩어리가 새 건물 옆에서도 읽힙니다.',17)
    text(d,(14,555),'제작자 판단: 작은 연못 수정 불필요',21)
    text(d,(14,590),'단순한 물색과 낮은 돌 경계가 이어집니다. 물·해안 전체 C7 때도 재검토합니다.',17)
    text(d,(14,644),'사용자 그림 승인 대기이며 본편 그림·승인 해시는 바꾸지 않았습니다.',17)
    game.convert('RGB').save(dest/'game-comparison.png')
    with Image.open(OUT/'Screens-v5/trial.png') as im:
        im.save(dest/'game-native.png');im.resize((1440,810),Image.Resampling.NEAREST).save(dest/'game-3x.png')
    files=list(dest.glob('*.png'))+list((OUT/'Screens-v5').glob('*.png'))
    (dest/'submission.json').write_text(json.dumps(dict(status='pending',scope='C7 teammate drawing style trial; production unchanged; visible house proportions require review',pngIdentity='dimensions + decoded RGBA',files={str(p.relative_to(ROOT)):rgba_evidence(p) for p in files}),ensure_ascii=False,indent=2)+'\n')
    print('Submission-v5: latest Unity before/after and observed Figma references, approval pending')
if __name__=='__main__':run()
