"""Contact sheet of actual Play captures; pixel evidence for the changed silhouette."""
from PIL import Image,ImageDraw,ImageChops
import json
from c7_team_prepare import ROOT,OUT
from c7_review import text
from c7_catalogue import rgba_evidence

def run():
    folder=OUT/'PondShape-v10'
    with Image.open(folder/'before.png') as im:before=im.convert('RGBA')
    with Image.open(folder/'trial.png') as im:after=im.convert('RGBA')
    board=Image.new('RGBA',(1000,770),'#f6f0e3');d=ImageDraw.Draw(board)
    text(d,(14,10),'풀밭 연못 모양 수정 · 양옆 돌출부 제거',25)
    text(d,(12,54),'기존: 양옆으로 튀어나온 물가',20)
    text(d,(508,54),'수정안: 완만하게 이어지는 둥근 물가',20)
    board.alpha_composite(before,(12,90));board.alpha_composite(after,(508,90))
    text(d,(14,383),'실제 Unity Play480×270 · 같은 지형·건물·위치 / 개경 승인본과 본편은 보존',17)
    text(d,(14,418),'물색은 기존3색 유지. 풀밭용 고정배치 수정안이며 사용자 승인 대기입니다.',17)
    text(d,(14,454),'위 실제 화면의 연못 부분 · 정수3배 확대',19)
    for im,x in [(before,52),(after,552)]:board.alpha_composite(im.crop((300,174,420,254)).resize((360,240),Image.Resampling.NEAREST),(x,486))
    text(d,(14,742),'같은5×3칸 범위 안에서 새 타원형으로 제작했습니다. 임의 확대·다른 물과 연결하지 않습니다.',16)
    board.convert('RGB').save(folder/'comparison.png')
    box=ImageChops.difference(before.convert('RGB'),after.convert('RGB')).getbbox()
    assert box and 320<=box[0]<box[2]<=400 and 190<=box[1]<box[3]<=238,box
    (folder/'submission.json').write_text(json.dumps(dict(status='pending',scope='fixed oval grass pond only; original Gaegyeong pond/production preserved',changedCameraPixelBounds=box,files={str(p.relative_to(ROOT)):rgba_evidence(p) for p in folder.glob('*.png')}),indent=2)+'\n')
    print('Actual Play comparison: only pond80x48rectangle changes; approval pending')
if __name__=='__main__':run()
