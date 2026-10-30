"""Review an unchanged pre-C7 game asset; no new art or production mutation."""
import json
from PIL import Image,ImageDraw
from c7_catalogue import ROOT,rgba_evidence
from c7_review import text,sprite
OUT=ROOT/'ArtReview/C7-CartOriginal-20261027'
ORIGINAL=ROOT/'Assets/PocketCaravan/Art/Baked/Objects/c6_cart_e1a728ea.png'
def board():
    page=Image.new('RGBA',(1000,760),'#f6f0e3');d=ImageDraw.Draw(page)
    text(d,(16,12),'수레 복원 대상 정정 · C7 이전 기존 게임 그림',25)
    for x,p,label in [(30,ROOT/'ArtReview/C7-CartRoof-v2-20261027/Native/cart.png','직전 · 잘못 짚은 첫 C7 시안'),(550,ORIGINAL,'복원 · 기존 게임 수레')]:
        text(d,(x,62),label,21);sprite(page,p,(x,100),8);sprite(page,p,(x+300,280),1)
    for x,n in [(12,'before'),(508,'trial')]:page.alpha_composite(Image.open(OUT/'Screens'/(n+'.png')).convert('RGBA'),(x,405))
    text(d,(16,365),'위: 8배 확대와 실제1배 / 아래: 실제 Unity 카메라480×270',19)
    text(d,(16,705),'기와집 청회색 용마루 유지 · 수레만 변경 · 본편의 원래 수레는 계속 유지',18)
    page.convert('RGB').save(OUT/'comparison.png')
    (OUT/'selection.json').write_text(json.dumps(dict(userQuote='수레는 이것보다 더 처음 버전을 말한거야.',selectedSource=str(ORIGINAL.relative_to(ROOT)),sourceEvidence=rgba_evidence(ORIGINAL),productionChanged=False,roofChanged=False),ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':board()
