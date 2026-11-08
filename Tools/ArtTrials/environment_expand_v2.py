"""Extend the approved gentle native cleanup to palace, cottage, willow and forest trees."""
from pathlib import Path
import json,subprocess,sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
import goryeo_art as G

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
OUT=ROOT/'ArtReview/Environment-Expand-v2'
ASE='/Applications/Aseprite.app/Contents/MacOS/aseprite'
ASSETS=[('palace','궁궐','hall',10,7),('cottage','초가집','thatch',5,4),('willow','버드나무','woodland5',4,4)]+[(f'forest_{i}',f'숲 나무 {i}',f'woodland{i}',*G.NATURE_SIZES[i]) for i in (1,2,3,4,6,7)]+[('cottage_small','초가집(작은형)','thatch',4,3)]
# woodland5's current native canvas is NATURE_SIZES[5], explicitly keep that rather than fit anew.
ASSETS[2]=('willow','버드나무','woodland5',*G.NATURE_SIZES[5])

def generate():
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'tiles').mkdir(exist_ok=True)
    assets=[];profiles={};checks=[]
    for key,title,source,w,h in ASSETS:
        original=G.sprite(source,w,h).convert('RGBA');original.save(OUT/f'{key}-before.png')
        output=OUT/key
        run_aseprite([ASE,'-b','--script-param',f'source={OUT/f"{key}-before.png"}','--script-param',f'output={output}','--script-param','strength=gentle','--script',str(ROOT/'Tools/ArtTrials/gentle_cleanup.lua')],check=True)
        after=Image.open(OUT/f'{key}.png').convert('RGBA')
        assert after.size==original.size and after.getchannel('A').tobytes()==original.getchannel('A').tobytes()
        colors={p[:3] for p in original.get_flattened_data() if p[3]};aftercolors={p[:3] for p in after.get_flattened_data() if p[3]}
        assert aftercolors<=colors and set(after.getchannel('A').get_flattened_data())=={0,255}
        opaque=sum(p[3]>0 for p in original.get_flattened_data());changed=sum(a[:3]!=b[:3] for a,b in zip(original.get_flattened_data(),after.get_flattened_data()) if a[3])
        assert changed<=int(opaque*.04)
        reopened=OUT/f'{key}-reopened.png';run_aseprite([ASE,'-b',str(OUT/f'{key}.aseprite'),'--save-as',str(reopened)],check=True)
        assert Image.open(reopened).convert('RGBA').tobytes()==after.tobytes();reopened.unlink()
        profile=f'{key}_native';profiles[profile]=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#%02x%02x%02x'%c for c in sorted(colors)])
        tiles=[]
        for y in range(h):
            for x in range(w):
                name=f'{key}_{x}_{y}';path=OUT/'tiles'/f'{name}.png';after.crop((x*16,y*16,(x+1)*16,(y+1)*16)).save(path)
                tiles.append(dict(dx=x,dy=y,layer='obj',tile=name,image=str(path.relative_to(ROOT))))
        assets.append(dict(id='trial_env_'+key,kind='object',purpose='building' if key in ('palace','cottage','cottage_small') else 'tree',size=[w,h],image=f'ArtReview/Environment-Expand-v2/{key}.png',source=dict(kind='ai_processed',path=f'ArtReview/Environment-Expand-v2/{key}.aseprite'),review=dict(status='pending'),profile=profile,tiles=tiles))
        checks.append(dict(asset=key,title=title,legacySource=source,size=list(after.size),cells=[w,h],opaquePixels=opaque,changedPixels=changed,retainedPercent=round(100*(opaque-changed)/opaque,3),alphaSilhouetteExact=True,paletteSubsetOfOriginal=True,asepriteReopenedExact=True))
    (OUT/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=profiles,assets=assets),ensure_ascii=False,indent=2)+'\n')
    (OUT/'checks.json').write_text(json.dumps(dict(method='gentle_cleanup.lua, unchanged v2 gentle settings, at most 4% of opaque pixels',assets=checks,userApproval='pending',runtimeApplied=False,D3PaletteApproval=False),ensure_ascii=False,indent=2)+'\n')
    review(checks);print(json.dumps(checks,ensure_ascii=False))

def review(checks):
    font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
    board=Image.new('RGB',(920,860),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((18,10),'C2·C3 확대 · 원본 보존 v2',font=font(25),fill='#332d2a')
    d.text((200,49),'기존 원본',font=font(18),fill='#332d2a');d.text((560,49),'약한 보정',font=font(18),fill='#332d2a')
    y=86
    for (key,title,_,_,_),check in [(ASSETS[i],checks[i]) for i in (0,1,2,9)]:
        d.text((18,y+20),title,font=font(20),fill='#332d2a');d.text((18,y+50),f'{check["size"][0]}×{check["size"][1]}',font=font(15),fill='#685b51')
        d.text((18,y+74),f'{check["retainedPercent"]}% 유지',font=font(14),fill='#685b51')
        for x,suffix in [(200,'-before'),(560,'')]:
            im=Image.open(OUT/f'{key}{suffix}.png').convert('RGBA');z=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST);board.paste(z,(x,y),z)
        y+=check['size'][1]*2+34
    d.text((18,832),'동일한 2배 확대 · 모양/알파/원본색 유지 · 승인 전 본편 미반영',font=font(16),fill='#685b51');board.save(OUT/'main-comparison.png')
    board=Image.new('RGB',(1000,625),'#f5f0e6');d=ImageDraw.Draw(board);d.text((18,10),'숲 확대 · 기존 나무 6종을 각각 온전한 개체로 보정',font=font(23),fill='#332d2a')
    for i,((key,title,_,_,_),check) in enumerate(zip(ASSETS[3:9],checks[3:9])):
        x=18+(i%2)*500;y=60+(i//2)*185
        d.text((x,y),f'{title} · {check["retainedPercent"]}% 유지',font=font(17),fill='#332d2a')
        for dx,suffix,label in [(70,'-before','원본'),(280,'','보정')]:
            im=Image.open(OUT/f'{key}{suffix}.png').convert('RGBA');z=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST);board.paste(z,(x+dx,y+32),z);d.text((x+dx,y+155),label,font=font(14),fill='#685b51')
    board.save(OUT/'forest-comparison.png')

if __name__=='__main__':generate()
