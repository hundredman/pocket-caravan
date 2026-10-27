"""Contact sheets from complete sprites and actual Unity Play captures.

Team board crops below are reference-only illustrations, never imported as art.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont
from c7_rebuild_catalogue import OUT, ROOT, OLD
from c7_catalogue import rgba_evidence

FONT = '/System/Library/Fonts/AppleSDGothicNeo.ttc'


def text(draw, xy, message, size=20):
    draw.text(xy, message, font=ImageFont.truetype(FONT, size), fill='#34271f')


def sprite(board, image, xy, scale=3):
    im = Image.open(image).convert('RGBA') if isinstance(image, Path) else image.convert('RGBA')
    board.alpha_composite(im.resize((im.width*scale, im.height*scale), Image.Resampling.NEAREST), xy)


def run():
    submission = OUT/'Submission-v4'
    submission.mkdir(exist_ok=True)
    board = Image.new('RGBA', (1400, 1010), '#f6f0e3')
    d = ImageDraw.Draw(board)
    text(d, (22, 12), 'C7 재제작 · 이전 시안 / 수정안 / 팀 스타일 참고', 27)
    text(d, (22, 50), '고려 그림: 원래 크기 ×3 · 팀 자료: 기준표의 표시 크기(원본 도트 배율 미확인)', 17)
    for x, label in ((24, '이전 시안 · 수정 요청'), (300, '새 구조 · 승인 대기'), (610, '팀 스타일 · 참고용')):
        text(d, (x, 82), label)
    ref = Image.open(ROOT/'Docs/Images/team-style-reference.png').convert('RGBA')
    objects = [('초가집', 'thatch', 'thatch.png'),
               ('기와집', 'tile-house', 'tile-house.png'),
               ('소나무', 'pine', 'pine.png')]
    for y, (label, name, old) in zip((116, 326, 536), objects):
        text(d, (24, y), label)
        sprite(board, OLD/'Native'/old, (24,y+34))
        sprite(board, OUT/'Native'/(name+'.png'), (300,y+34))
    # Presentation reference panels only: no team fragments enter any catalogue/scene.
    board.alpha_composite(ref.crop((24,94,325,319)), (610,120))
    board.alpha_composite(ref.crop((640,94,879,354)), (940,120))
    board.alpha_composite(ref.crop((24,418,284,678)), (610,420))
    board.alpha_composite(ref.crop((310,418,451,678)), (940,420))
    text(d, (24, 756), '풀·흙 조립 세트 · 16×16 · 47가지 경계 + 흙 중심', 21)
    for i, n in enumerate(('grass-255','grass-110','grass-38','grass-127','dirt')):
        sprite(board, OUT/'Native'/(n+'.png'), (24+i*110,800),4)
        text(d,(24+i*110,870),n,13)
    text(d,(24,926),'건물·지형 색은 이번 재제작 시안용 후보. 팀 공통 D3 확정이나 본편 교체가 아닙니다.',18)
    text(d,(24,956),'AI 생성 → A5 정리 → Aseprite 실제 해상도 손질. 건물·나무는 64×48 / 발밑 유지. 나무 픽셀 보존.',18)
    board.convert('RGB').save(submission/'style-comparison.png')

    game=Image.new('RGBA',(1000,698),'#f6f0e3');d=ImageDraw.Draw(game)
    text(d,(16,12),'Unity 실제 Play · 이전 시안 / 재제작 · 480×270',25)
    for x, name, label in ((12,'before','이전 시안'),(508,'trial','재제작 · 승인 대기')):
        text(d,(x,54),label,20)
        im=Image.open(OUT/'Screens-v4'/(name+'.png')).convert('RGBA')
        assert im.size==(480,270)
        game.alpha_composite(im,(x,88))
    text(d,(14,380),'플레이어·아낙·촌장·작은 연못은 양쪽 모두 기존 승인본입니다.',20)
    text(d,(14,416),'PPU 16 / Point / 같은 완만한 시험장 길. 실제 B3 마을 배치는 아닙니다.',17)
    text(d,(14,457),'현재 판단: 플레이어·사람 NPC 수정 불필요',21)
    text(d,(14,491),'작은 크기에서 윤곽과 얼굴이 또렷하고 밝은 색 덩어리가 새 건물과 이어집니다.',17)
    text(d,(14,538),'현재 판단: 작은 연못 수정 불필요',21)
    text(d,(14,572),'단순한 물색과 낮은 돌 경계가 시안과 어울립니다. 물·해안 전체 C7 때 다시 확인합니다.',17)
    text(d,(14,628),'이 판단은 제작자 의견입니다. 사용자 승인 전에는 승인본과 본편을 바꾸지 않습니다.',17)
    game.convert('RGB').save(submission/'game-comparison.png')
    # Enlarge actual capture only with integer nearest-neighbor, retain native copy too.
    im=Image.open(OUT/'Screens-v4/trial.png')
    im.save(submission/'game-native.png')
    im.resize((1440,810),Image.Resampling.NEAREST).save(submission/'game-3x.png')
    files=list(submission.glob('*.png'))+list((OUT/'Screens-v4').glob('*.png'))
    (submission/'submission.json').write_text(json.dumps(dict(status='pending',
        scope='C7 rebuilt houses/ground approval pending; pine retained; production unchanged; B3 layout approval separate',
        pngIdentity='dimensions + decoded RGBA SHA256',
        files={str(p.relative_to(ROOT)):rgba_evidence(p) for p in files}),ensure_ascii=False,indent=2)+'\n')
    print('C7 Submission-v4 written from actual Screens-v4; pending user approval')


if __name__ == '__main__':
    run()
