"""C1 expansion stage 1: native front sprites, retaining the frozen woman reference."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/ArtTrials'))
import npc_polish_v4 as V4
OUT = ROOT / 'ArtReview/C1-Expand-1'
ASEPRITE = '/Applications/Aseprite.app/Contents/MacOS/aseprite'
FONT = '/System/Library/Fonts/AppleSDGothicNeo.ttc'

def render(name):
    original = V4.C.render
    def refined(rows, palette):
        rows = [row[:] for row in rows]
        if name == 'chief':
            # Keep elderly eyebrows but leave a skin gap over the dark pupil.
            rows[10][6] = rows[10][10] = 'Q'
        else:
            # Remove the floating dark mouth corners; use the woman's small warm smile.
            rows[14][6] = 'Q'
            rows[14][9] = 'Q'
            rows[14][7] = 'N'
            rows[14][8] = 'q'
            rows[15][7] = 'S'
        im = V4.clean_render(rows, palette)
        if name == 'chief':
            # White robe from the existing cream/beard family, rather than lilac blocks.
            colors = {(230,225,234):(241,233,216), (189,180,198):(204,195,177),
                      (157,117,168):(202,190,169)}
            im.putdata([(*colors.get(p[:3],p[:3]), p[3]) for p in im.get_flattened_data()])
        return im
    V4.ACTIVE = name
    V4.C.render = refined
    try:
        im = V4.V3.render(name)
    finally:
        V4.C.render = original
    colors = {(76,54,38):(78,55,39),(152,111,66):(154,109,76),(226,190,112):(224,186,112),
              (103,70,46):(108,76,45),(169,124,84):(174,127,85),(200,120,142):(194,119,143)}
    im.putdata([(*colors.get(p[:3],p[:3]),p[3]) if p[3] else (0,0,0,0) for p in im.get_flattened_data()])
    return im

def draw_sprite(board, im, xy, scale):
    board.paste(im.resize((16*scale,24*scale),Image.Resampling.NEAREST),xy,
                im.resize((16*scale,24*scale),Image.Resampling.NEAREST))

def generate():
    OUT.mkdir(parents=True,exist_ok=True)
    catalogue={'schemaVersion':1,'profiles':{},'assets':[]}
    checks=[]
    for name in ('chief','trader'):
        im=render(name);im.save(OUT/f'{name}-front.png')
        subprocess.run([ASEPRITE,'-b',str(OUT/f'{name}-front.png'),'--save-as',str(OUT/f'{name}-front.aseprite')],check=True)
        subprocess.run([ASEPRITE,'-b',str(OUT/f'{name}-front.aseprite'),'--save-as',str(OUT/f'{name}-reopened.png')],check=True)
        reopened=Image.open(OUT/f'{name}-reopened.png').convert('RGBA')
        before=Image.open(ROOT/f'ArtReview/NPC-v4/{name}-front.png').convert('RGBA')
        assert im.size==(16,24) and reopened.tobytes()==im.tobytes()
        assert set(im.getchannel('A').get_flattened_data())=={0,255}
        assert im.getchannel('A').tobytes()==before.getchannel('A').tobytes()
        changes=[{'x':x,'y':y} for y in range(24) for x in range(16) if im.getpixel((x,y))!=before.getpixel((x,y))]
        checks.append({'character':name,'size':[16,24],'changedPixels':len(changes),'coordinates':changes,
                       'alphaSilhouetteBaselinePreserved':True,'binaryAlpha':True,'asepriteReopenedPixelEqual':True})
        (OUT/f'{name}-reopened.png').unlink()
        palette=sorted({'#%02x%02x%02x'%p[:3] for p in im.get_flattened_data() if p[3]})
        catalogue['profiles'][name]={'pixelsPerCell':[16,24],'palette':palette,'binaryAlpha':True}
        asset_id=f'trial_{name}_c1_expand_1'
        catalogue['assets'].append({'id':asset_id,'kind':'object','purpose':'character','size':[1,1],
            'image':f'ArtReview/C1-Expand-1/{name}-front.png',
            'source':{'kind':'authored','path':f'ArtReview/C1-Expand-1/{name}-front.aseprite'},
            'review':{'status':'pending'},'profile':name,
            'tiles':[{'dx':0,'dy':0,'layer':'obj','tile':asset_id,'image':f'ArtReview/C1-Expand-1/{name}-front.png'}]})
    (OUT/'catalogue.json').write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+'\n')
    woman_path=ROOT/'ArtReview/Approved/woman-front-v4.png'
    woman=Image.open(woman_path).convert('RGBA')
    report={'characters':checks,'womanReferenceSHA256':hashlib.sha256(woman_path.read_bytes()).hexdigest(),
            'scope':'native front sprites only; no Unity import or gameplay test',
            'userApproval':'pending','D3PaletteApproval':False}
    (OUT/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    font=lambda n:ImageFont.truetype(FONT,n)
    board=Image.new('RGB',(850,565),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((18,10),'C1 확대 ① · 촌장과 행상 정면',font=font(23),fill='#332d2a')
    for x,label in [(125,'이전 v4'),(360,'수정안'),(625,'승인 기준 · 아낙')]:
        d.text((x,50),label,font=font(17),fill='#332d2a')
    for row,(name,title) in enumerate([('chief','촌장'),('trader','행상')]):
        y=85+row*222;d.text((18,y+42),title,font=font(18),fill='#332d2a')
        images=[Image.open(ROOT/f'ArtReview/NPC-v4/{name}-front.png').convert('RGBA'),
                Image.open(OUT/f'{name}-front.png').convert('RGBA'),woman]
        for x,im in zip((135,370,640),images):
            d.rectangle((x-4,y-4,x+99,y+147),fill='#d8d2c4');draw_sprite(board,im,(x,y),6)
            draw_sprite(board,im,(x+40,y+158),1)
            silhouette=Image.new('RGBA',im.size,(55,48,43));silhouette.putalpha(im.getchannel('A'))
            draw_sprite(board,silhouette,(x+110,y+48),3)
    d.text((18,537),'16×24 · 확대 6배 / 원래 크기 / 실루엣 · 정면만 검토',font=font(16),fill='#685b51')
    board.save(OUT/'comparison.png')
    base=Image.open(ROOT/'ArtReview/NPC-v4/game-after-480x270.png').convert('RGBA')
    # Use precisely the previous review scene. Paint new sprites at their original compositing points.
    before=base.copy();after=base.copy()
    for name,point in [('chief',(208,183)),('trader',(366,185))]:
        old=Image.open(ROOT/f'ArtReview/NPC-v4/{name}-front.png').convert('RGBA')
        new=Image.open(OUT/f'{name}-front.png').convert('RGBA')
        before.alpha_composite(old,point);after.alpha_composite(new,point)
    before.save(OUT/'game-before-480x270.png');after.save(OUT/'game-after-480x270.png')
    board=Image.new('RGB',(1000,345),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((15,8),'게임 화면 크기 · 각 480×270 · 기존 캡처에 합성한 검토 그림',font=font(21),fill='#332d2a')
    for x,im,title in [(12,before,'이전 v4'),(508,after,'수정안 · 아낙/환경 동일')]:
        d.text((x,40),title,font=font(16),fill='#332d2a');board.paste(im.convert('RGB'),(x,66))
    board.save(OUT/'game-comparison.png')
    print(json.dumps(report,ensure_ascii=False))

if __name__=='__main__':generate()
