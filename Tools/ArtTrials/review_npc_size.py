from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
root=Path(__file__).resolve().parents[2];out=root/'ArtReview/C1-Expand-1';font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
items=[('플레이어 너구리','Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png'),('기존 동물 촌장','Assets/PocketCaravan/Art/Characters/elder_tall_d0.png'),('사람 촌장','ArtReview/C1-Expand-1/chief-front.png'),('기존 동물 행상','Assets/PocketCaravan/Art/Characters/trader_tall_d0.png'),('사람 행상','ArtReview/C1-Expand-1/trader-front.png'),('승인된 아낙','ArtReview/Approved/woman-front-v4.png')]
board=Image.new('RGB',(1020,320),'#f5f0e6');d=ImageDraw.Draw(board);d.text((18,10),'원본 크기 직접 비교 · 모두 16×24 / 같은 발밑 / 같은 6배 확대',font=font(22),fill='#332d2a');report=[]
for i,(label,path) in enumerate(items):
 im=Image.open(root/path).convert('RGBA');a=im.getchannel('A');box=a.getbbox();x=18+i*168;y=85
 d.text((x,50),label,font=font(16),fill='#332d2a');d.rectangle((x,y,x+95,y+143),fill='#d8d2c4');d.line((x,y+144,x+95,y+144),fill='#ad6f62');board.paste(im.resize((96,144),Image.Resampling.NEAREST),(x,y),im.resize((96,144),Image.Resampling.NEAREST));board.paste(im,(x+40,242),im)
 d.text((x,278),f'그림 {box[2]-box[0]}×{box[3]-box[1]}px',font=font(15),fill='#332d2a');report.append({'name':label,'path':path,'canvas':im.size,'visibleBox':box,'opaquePixels':sum(v>0 for v in a.get_flattened_data())})
board.save(out/'size-comparison.png');(out/'size-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
