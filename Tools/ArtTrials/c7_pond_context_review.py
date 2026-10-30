"""Compare the same approved water geometry over paving versus natural grass.

No PNG editing: only existing complete water modules and ground composition.
"""
from pathlib import Path
import csv,json
from PIL import Image,ImageDraw,ImageChops
from c7_team_prepare import ROOT,OUT
from c7_catalogue import rgba_evidence
from c7_original_ground import grass
from c7_review import text


def run():
    folder=OUT/'PondContext-v9'
    with Image.open(folder/'before.png') as im:before=im.convert('RGB')
    with Image.open(folder/'trial.png') as im:after=im.convert('RGB')
    assert before.size==after.size==(480,270)
    cells=[];water_pixels=0
    source=ROOT/'ArtReview/Approved/A4-Pond'
    for p in csv.reader((source/'unity-pond-placement.csv').read_text().splitlines()):
        if int(p[0])>30:continue
        x,y=int(p[0])-24+20,int(p[1])-36
        assert grass(x,y),'Review pond must be off the staged road'
        original=source/'pieces'/(p[2]+'.png')
        with Image.open(original) as im:mask=im.convert('RGBA').getchannel('A')
        x0,y0=x*16,270-y*16-16
        diff=ImageChops.difference(before.crop((x0,y0,x0+16,y0+16)),after.crop((x0,y0,x0+16,y0+16)))
        assert Image.composite(diff,Image.new('RGB',(16,16)),mask).getbbox() is None,(x,y)
        water_pixels+=sum(1 for a in mask.get_flattened_data() if a)
        cells.append(dict(x=x,y=y,piece=p[2],originalImage=str(original.relative_to(ROOT)),**rgba_evidence(original)))
    assert len(cells)==11
    houses=[]
    for name,x,y in [('thatch',5,10),('tile-house',11,10),('pine',20,10)]:
        with Image.open(OUT/'Native'/(name+'.png')) as im:mask=im.convert('RGBA').getchannel('A')
        x0,y0=x*16,270-y*16-48
        diff=ImageChops.difference(before.crop((x0,y0,x0+64,y0+48)),after.crop((x0,y0,x0+64,y0+48)))
        assert Image.composite(diff,Image.new('RGB',(64,48)),mask).getbbox() is None,name
        houses.append(dict(name=name,opaquePixelDifferences=0))
    board=Image.new('RGBA',(1000,770),'#f6f0e3');draw=ImageDraw.Draw(board)
    text(draw,(14,10),'연못 바닥 맥락 비교 · 원래 지형과 같은 물 모양',24)
    text(draw,(12,54),'기존: 개경 포장 바닥 포함',20)
    text(draw,(508,54),'제안: 포장 없이 풀밭 위에 배치',20)
    board.alpha_composite(before.convert('RGBA'),(12,90));board.alpha_composite(after.convert('RGBA'),(508,90))
    text(draw,(14,382),'같은 물색·물가 11조각 / 건물·인물·연못 위치 동일 / 기존 개경 승인본 보존',17)
    text(draw,(14,413),'실제 Unity Play480×270 · 길 밖 풀밭으로 양쪽 모두 2칸 이동 · 제안은 승인 대기',16)
    text(draw,(14,453),'위 실제 화면의 연못 부분 · 정수3배 확대',19)
    for image,x in [(before,52),(after,552)]:
        crop=image.crop((300,174,420,254)).resize((360,240),Image.Resampling.NEAREST)
        board.alpha_composite(crop.convert('RGBA'),(x,486))
    text(draw,(14,741),'물 그림은 그대로이며, 연못의 투명한 바깥 부분에 보이던 포장 타일만 제거했습니다.',16)
    board.convert('RGB').save(folder/'comparison.png')
    doc=dict(status='pending user approval for natural-ground context',waterOpaquePixelsCompared=water_pixels,
             waterOpaquePixelDifferences=0,pondCells=cells,otherObjects=houses,
             change='omit Gaegyeong paving underlay; original ground and all water art unchanged',
             placement='complete first 11-cell pond; both variants shifted2cells south off the road',
             originalApprovalScope='Gaegyeong22cells/11combinations with paving and lotus, preserved',
             files={str(p.relative_to(ROOT)):rgba_evidence(p) for p in folder.glob('*.png')})
    (folder/'checks.json').write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n')
    print(f'Pond context: {len(cells)}cells off road; {water_pixels}opaque water pixels unchanged; buildings/pine unchanged')


if __name__=='__main__':run()
