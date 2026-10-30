"""Compare actual Unity Play captures; assemble presentation without modifying source art."""
from pathlib import Path
from PIL import Image,ImageChops,ImageDraw,ImageFont
import hashlib,json
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'Docs/Validation/C6-Production-2026-10-26'
def main():
 rows=[]
 for a in sorted((F/'before').glob('*.png')):
  b=F/'after'/a.name;old=Image.open(a).convert('RGBA');new=Image.open(b).convert('RGBA')
  if old.size!=new.size:raise ValueError('Camera size mismatch')
  diff=ImageChops.difference(old,new);count=sum(any(px)for px in diff.get_flattened_data())
  rows.append(dict(map=a.stem,size=list(old.size),differentPixels=count,beforeSHA256=hashlib.sha256(a.read_bytes()).hexdigest(),afterSHA256=hashlib.sha256(b.read_bytes()).hexdigest()))
 report=dict(mode='Actual Unity previewOnly Play camera; authoring poses frozen on EnteredPlayMode/before Start; OnGUI excluded',maps=rows,allPixelIdentical=all(r['differentPixels']==0 for r in rows))
 (F/'screen-comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',21)
 canvas=Image.new('RGB',(1000,672),'#eeeae1');d=ImageDraw.Draw(canvas);d.text((12,7),'C6 · Unity 실제 Play 카메라 (480×270) · 왼쪽 연결 전 / 오른쪽 연결 후',font=font,fill='#222222')
 for row,mid in enumerate(('Naru','Gaegyeong')):
  y=42+row*310;d.text((12,y),f'{mid} — 바뀐 픽셀 '+str(next(r['differentPixels']for r in rows if r['map']==mid)),font=font,fill='#222222')
  for col,variant in enumerate(('before','after')):canvas.paste(Image.open(F/variant/(mid+'.png')).convert('RGB'),(12+col*496,y+30))
 canvas.save(F/'comparison.png')
 print(json.dumps(report,ensure_ascii=False));return 0 if report['allPixelIdentical']and len(rows)==11 else 1
if __name__=='__main__':raise SystemExit(main())
