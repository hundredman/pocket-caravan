"""QA compositions of unmodified renders, and scope/save checks. No asset painting."""
import hashlib,json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/BuildingNatureBinding-20261029'
EVIDENCE=ROOT/'Docs/Validation/Building-Nature-Binding-2026-10-29'
def font(n):return ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
def board(h,title):
    im=Image.new('RGB',(992,h),(29,34,38));text(im,(16,10),title,20);return im
def text(im,pos,value,n=16):ImageDraw.Draw(im).text(pos,value,font=font(n),fill='white')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    points=json.loads((OUT/'asset-views.json').read_text())['points']
    old=Image.open(ROOT/'ArtReview/NatureBinding-20261029/Base/world-full.png');new=Image.open(OUT/'BuildingBase/world-full.png')
    im=board(48+306*len(points),'건물·배 교체 전 / 후 — 같은 좌표·480×270 원본 조립 (Unity는 별도)')
    for i,p in enumerate(points):
        x,y=p['x'],p['y']-2;box=(int(x*16-240),int((y+130)*16-135),int(x*16+240),int((y+130)*16+135));top=50+i*306
        text(im,(16,top),p['asset']+' · 이전');text(im,(512,top),p['asset']+' · 교체')
        im.paste(old.crop(box),(16,top+25));im.paste(new.crop(box),(512,top+25))
    im.save(OUT/'buildings-before-after.png')
    im=board(970,'실제 Unity Play · 480×270 — 위 두 줄 본편 / 아래 숲·산맥 검토안')
    cases=[('본편 궁궐','ProductionUnity','asset-palace'),('본편 배','ProductionUnity','asset-boat'),('본편 객주','ProductionUnity','asset-gaekju'),('본편 초가집','ProductionUnity','asset-thatch'),('검토: 송악 산자락 숲','Unity','songak-ridge'),('검토: 수원·숲','Unity','source-upper')]
    for i,(label,folder,name) in enumerate(cases):
        x=16+496*(i%2);y=52+306*(i//2);text(im,(x,y),label);im.paste(Image.open(OUT/f'{folder}/{name}.png'),(x,y+25))
    im.save(OUT/'game-review.png')
    before=Image.open(ROOT/'ArtReview/NatureBinding-20261029/Layers/composite.png');after=Image.open(OUT/'Layers/composite.png')
    im=board(666,'산자락 숲 — 왼쪽 이전 검토 / 오른쪽 윤곽에 맞춘 숲 + 새 건물')
    for i,(x,y) in enumerate([(182,-36),(116,-18)]):
        box=(x*16-240,(y+130)*16-135,x*16+240,(y+130)*16+135);top=50+i*306
        text(im,(16,top),'송악' if i==0 else '수원');im.paste(before.crop(box),(16,top+25));im.paste(after.crop(box),(512,top+25))
    im.save(OUT/'forest-before-after.png')
    ids=[r.split('\t')[0] for r in (ROOT/'Docs/Validation/Forest-Performance-2026-10-29/all-scenes.tsv').read_text().splitlines()]
    for mode,title in [('production','13맵 실제 Unity Edit 카메라 — 왼쪽 교체 전 / 오른쪽 건물·배 본편 적용'),('review','13맵 실제 Unity Edit 카메라 — 왼쪽 현재 본편 / 오른쪽 자연 배치 검토')]:
        im=board(45+300*len(ids),title)
        for i,mid in enumerate(ids):
            y=48+300*i;text(im,(16,y),mid)
            a=OUT/f'Unity/overview-{mid}-before.png' if mode=='production' else OUT/f'ProductionUnity/overview-{mid}-production.png'
            b=OUT/f'ProductionUnity/overview-{mid}-production.png' if mode=='production' else OUT/f'Unity/overview-{mid}-after.png'
            im.paste(Image.open(a),(16,y+24));im.paste(Image.open(b),(512,y+24))
        im.save(OUT/f'{mode}-13-maps.png')
    saved=json.loads((EVIDENCE/'game-save-before.json').read_text());parent=Path(next(iter(saved))).parent
    current={str(p):sha(p) for p in parent.iterdir() if p.is_file()};(EVIDENCE/'game-save-after.json').write_text(json.dumps(current,indent=2))
    protected=json.loads((EVIDENCE/'protected-before.json').read_text());after={p:sha(ROOT/p) for p in protected}
    result={'gameSaveFilesUnchanged':saved==current,'gameSaveFiles':len(saved),'protectedFilesUnchanged':after==protected,
            'preview':json.loads((OUT/'Unity/play-checks.json').read_text()),'production':json.loads((OUT/'ProductionUnity/play-checks.json').read_text())}
    (EVIDENCE/'result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));(EVIDENCE/'protected-after.json').write_text(json.dumps(after,indent=2))
    assert result['gameSaveFilesUnchanged'] and result['protectedFilesUnchanged']
    print('Production/preview checks:',len(result['production']['checks']),len(result['preview']['checks']),'saves/protected files unchanged')
if __name__=='__main__':main()
