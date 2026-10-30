"""Package actual Play captures for human review; never approves art."""
import json
from PIL import Image,ImageDraw
from c7_catalogue import rgba_evidence
from c7_review import text
from c7_round_pond import ROOT,run as register
OUT=ROOT/'ArtReview/C7-SunkenPond-20261027'
COLORS=['7a9c66','6a8b56','91ad77','b99887','9d826e','6f705c','98b9a9','7fb9af','619a98']
def run():
    register(OUT,'c7_sunken',[tuple(bytes.fromhex(c)) for c in COLORS],str((OUT/'inner-corner.png').relative_to(ROOT)))
    folder=ROOT/'ArtReview/C7-TeamStyle-20261027/PondDepth-v12'
    board=Image.new('RGBA',(1488,752),'#f6f0e3');d=ImageDraw.Draw(board)
    text(d,(14,12),'연못 수정 · 흙둑과 얕은 물로 깊이 표현',27)
    for i,(name,label) in enumerate([('before','이전: 평평한 타원'),('trial','수정: 흙둑 + 물가 그늘'),('decorated','수정 + 선택 장식')]):
        x=12+i*492;text(d,(x,58),label,21)
        with Image.open(folder/(name+'.png')) as im:im=im.convert('RGBA')
        assert im.size==(480,270)
        board.alpha_composite(im,(x,92))
        board.alpha_composite(im.crop((300,174,420,254)).resize((360,240),Image.Resampling.NEAREST),(x+60,450))
    text(d,(14,385),'위: 실제 Unity Play 화면 480×270 / 아래: 같은 화면의 연못 부분 3배 확대',19)
    text(d,(14,418),'같은 지형·건물·인물·위치. 물가는 약한 비대칭, 먼 쪽은 그늘, 가까운 쪽은 얕은 물.',18)
    text(d,(14,713),'검토용 시안 · 본편 및 개경 승인본 유지 · 그림 승인 대기',19)
    board.convert('RGB').save(folder/'comparison.png')
    (folder/'submission.json').write_text(json.dumps(dict(status='pending',files={p.name:rgba_evidence(p) for p in folder.glob('*.png')}),indent=2)+'\n')
if __name__=='__main__':run()
