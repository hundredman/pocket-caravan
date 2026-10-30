"""QA boards only: unmodified existing renders, no new or retouched game art."""
import hashlib,json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/NatureBinding-20261029'
FONT='/System/Library/Fonts/AppleSDGothicNeo.ttc'
def font(n):return ImageFont.truetype(FONT,n)
def board(w,h,title):
    im=Image.new('RGB',(w,h),(29,34,38));ImageDraw.Draw(im).text((16,10),title,font=font(20),fill='white');return im
def text(im,pos,value,n=16):ImageDraw.Draw(im).text(pos,value,font=font(n),fill='white')
def main():
    before=Image.open(OUT/'Base/world-full.png').convert('RGBA');after=Image.open(OUT/'Layers/composite.png').convert('RGBA')
    im=board(992,980,'원본 조립 전 / 후 — 같은 세계 좌표·480×270 (Unity 화면은 별도)')
    for i,(name,x,y) in enumerate([('송악 능선',182,-36),('큰 폭포·강 연결',116,-18),('장단 논',217,24)]):
        top=50+i*306;text(im,(16,top),name+' · 이전');text(im,(512,top),name+' · 검토안')
        box=(x*16-240,(y+130)*16-135,x*16+240,(y+130)*16+135)
        im.paste(before.crop(box),(16,top+25));im.paste(after.crop(box),(512,top+25))
    im.save(OUT/'nature-before-after.png')
    im=board(992,970,'실제 Unity Play · 480×270 원본 — 검토 전용, 본편 미적용')
    cases=[('송악 능선','songak-ridge'),('논 안 가림','rice-inside'),('큰 폭포 상부','source-upper'),('강 하류 연결','source-lower'),('중앙 대륙 도착','arrival-transit_woodtown'),('고려 도착','arrival-Naru')]
    for i,(label,name) in enumerate(cases):
        x=16+(i%2)*496;y=52+(i//2)*306;text(im,(x,y),label);im.paste(Image.open(OUT/f'Unity/{name}.png'),(x,y+25))
    im.save(OUT/'game-review.png')
    ids=[r.split('\t')[0] for r in (ROOT/'Docs/Validation/Forest-Performance-2026-10-29/all-scenes.tsv').read_text().splitlines()]
    im=board(992,45+len(ids)*300,'13맵 실제 Unity 씬 전체보기 · 왼쪽 본편 / 오른쪽 검토안 (Edit 카메라)')
    for i,mid in enumerate(ids):
        y=48+i*300;text(im,(16,y),mid)
        for col,stage in enumerate(['before','after']):im.paste(Image.open(OUT/f'Unity/overview-{mid}-{stage}.png'),(16+496*col,y+24))
    im.save(OUT/'thirteen-maps.png')
    evidence=ROOT/'Docs/Validation/Nature-Integration-2026-10-29'
    previous=json.loads((evidence/'game-save-before.json').read_text());parent=Path(next(iter(previous))).parent
    current={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in parent.iterdir() if p.is_file()}
    (evidence/'game-save-after.json').write_text(json.dumps(current,indent=2))
    result={'gameSaveFiles':len(previous),'unchanged':previous==current,'unityPlay':json.loads((OUT/'Unity/play-checks.json').read_text())}
    (evidence/'result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
    print('Game save files unchanged:',result['unchanged'],'Play checks:',len(result['unityPlay']['checks']))
if __name__=='__main__':main()
