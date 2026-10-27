"""Three actual Play views, with native-scale and integer enlarged pond crops."""
import json
from PIL import Image,ImageDraw,ImageChops
from c7_pond_decor import ROOT,OUT
from c7_review import text
from c7_catalogue import rgba_evidence

def run():
    folder=ROOT/'ArtReview/C7-TeamStyle-20261027/PondDecor-v11'
    board=Image.new('RGBA',(1488,752),'#f6f0e3');d=ImageDraw.Draw(board)
    text(d,(14,12),'같은 둥근 연못 · 장식 바리에이션 3안',27)
    names=['plain','leaves','lotus'];images=[]
    for i,(name,label) in enumerate(zip(names,['1. 민 연못','2. 수련잎만','3. 수련잎 + 꽃 한 송이'])):
        x=12+i*492;text(d,(x,58),label,21)
        with Image.open(folder/(name+'.png')) as im:image=im.convert('RGBA')
        assert image.size==(480,270);images.append(image)
        board.alpha_composite(image,(x,92))
        board.alpha_composite(image.crop((300,174,420,254)).resize((360,240),Image.Resampling.NEAREST),(x+60,450))
    text(d,(14,385),'위: 실제 Unity Play480×270 / 아래: 같은 화면의 연못 부분 정수3배 확대',19)
    text(d,(14,418),'연못 모양·지형·건물은 동일. 3안은 물 표면의 약5.5%만 장식하며 모두 승인 대기입니다.',18)
    text(d,(14,713),'기존 연꽃을 잘라 쓰지 않고 작은 장식2개를 새로 제작했습니다. 본편·개경 승인본은 유지합니다.',18)
    board.convert('RGB').save(folder/'comparison.png')
    boxes=[]
    for i,bounds in [(1,(336,206,352,222)),(2,(352,206,368,222))]:
        box=ImageChops.difference(images[i-1].convert('RGB'),images[i].convert('RGB')).getbbox()
        assert box and bounds[0]<=box[0]<box[2]<=bounds[2] and bounds[1]<=box[1]<box[3]<=bounds[3],box
        boxes.append(box)
    (folder/'submission.json').write_text(json.dumps(dict(status='pending',variants=names,addedDecorationPixelBounds=boxes,files={str(p.relative_to(ROOT)):rgba_evidence(p) for p in folder.glob('*.png')}),indent=2)+'\n')
    print('3 actual Play captures; each difference limited to added whole-decoration area; approval pending')
if __name__=='__main__':run()
