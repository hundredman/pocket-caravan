"""Five new native front assets specified by Docs/C1-Binding.md, pending review."""
from pathlib import Path
import hashlib,json,subprocess,sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
import npc_expand_remaining as R
OUT=ROOT/'ArtReview/C1-Binding-Fronts'
ASE='/Applications/Aseprite.app/Contents/MacOS/aseprite'
ROLES=[('porter','지게꾼'),('merchant','시전 상인'),('merchant_silk','비단 장수'),('scholar','선비'),('woman_ricecake','떡 장수')]

def paint(rows,pal):
    im=Image.new('RGBA',(16,24))
    def at(x,y):return rows[y][x] if 0<=x<16 and 0<=y<24 else '.'
    def family(ch):
        if ch in 'qQSNp':return 'skin'
        if ch in 'rR':return 'hair'
        if ch in 'hH':return 'hat'
        if ch in 'jJDtTd':return 'wood'
        return 'clothes'
    for y,row in enumerate(rows):
        for x,ch in enumerate(row):
            if ch=='.':continue
            if ch=='o':
                groups=[family(v) for v in (at(x,y+1),at(x,y-1),at(x-1,y),at(x+1,y)) if v not in '.o']
                group=max(dict.fromkeys(groups),key=groups.count) if groups else 'clothes'
                color={'skin':(119,83,69),'hair':(38,30,33),'hat':(31,28,37),'wood':(78,55,39),'clothes':(79,66,70)}[group]
            else:
                color=pal[ch]
                if at(x+1,y)=='o' or at(x,y+1)=='o':
                    if ch=='q':color=pal['S']
                    elif ch=='c':color=pal['a']
                    elif ch=='r':color=(42,32,33)
            im.putpixel((x,y),(*color,255))
    return im

def render(name, *, layout=False):
    if name=='woman_ricecake':
        # Keep the actual approved woman body and face. Only replace the prop regions.
        im=Image.open(ROOT/'ArtReview/Approved/woman-front-v4.png').convert('RGBA')
        d=ImageDraw.Draw(im);d.rectangle((0,0,15,6),fill=(0,0,0,0))
        d.line((1,19,14,19),fill=(78,55,39,255));d.line((2,20,13,20),fill=(174,127,85,255))
        d.line((2,18,13,18),fill=(221,185,137,255))
        for x in (3,8):
            d.rectangle((x,16,x+2,17),fill=(204,195,177,255));d.line((x,16,x+1,16),fill=(253,247,233,255));d.point((x,17),fill=(241,233,216,255))
        d.point((1,18),fill=(237,193,157,255));d.point((14,18),fill=(207,153,119,255))
        return im
    rows,pal=R.render('binding_base',layout=True)
    def point(x,y,ch):rows[y][x]=ch
    def run(y,a,b,ch):
        for x in range(a,b+1):point(x,y,ch)
    def box(x0,y0,x1,y1,ch):
        for y in range(y0,y1+1):run(y,x0,x1,ch)
    # All new adult faces retain the approved face layout and a clear gap over pupils.
    run(7,4,11,'r');run(8,3,12,'r');run(8,4,9,'R');point(3,9,'r');point(12,9,'r')
    if name=='porter':
        pal.update(c=(208,185,137),a=(174,148,107),b=(137,105,70))
        run(6,5,10,'r');run(7,3,12,'r');run(8,2,13,'u');run(8,3,11,'U');point(1,9,'u')
        # Open wooden A-frame, with visible empty spaces; no merchant's packed load.
        for y in range(12,22):
            point(0,y,'D');point(3,y,'j')
        run(13,0,3,'J');run(17,0,3,'D');run(21,0,4,'D');point(2,14,'T')
        run(18,4,6,'D');point(3,19,'q')
    elif name in ('merchant','merchant_silk'):
        pal.update(c=(124,151,181),a=(87,107,144),b=(221,185,137))
        # Topknot and open vest: cream shirt inside two navy panels.
        box(7,4,9,5,'r');point(7,4,'R');run(6,5,10,'r');point(6,6,'R')
        for y in range(16,20):
            point(5,y,'a');point(10,y,'a')
        run(16,6,9,'u');run(17,7,8,'u');point(7,18,'u')
        # Both variants share every body pixel outside this prop box.
        if name=='merchant':
            box(11,17,15,21,'D');box(12,18,14,20,'u');point(13,18,'U');point(13,19,'D');point(12,20,'U')
        else:
            pal.update(P=(230,153,174),A=(194,119,143))
            run(16,12,14,'D');box(11,17,15,20,'A');box(12,17,14,20,'P');run(21,12,14,'D')
            run(17,12,14,'u');point(13,17,'A');run(19,11,15,'T')
        point(11,19,'q')
    elif name=='scholar':
        pal.update(c=(241,233,216),a=(204,195,177),b=(137,105,70))
        # Broad flat cloth cap and narrow tails distinguish it from the herbalist's pointed cap.
        box(5,3,10,4,'h');run(3,6,9,'H');box(4,5,11,7,'h');run(5,5,6,'H');point(3,7,'h');point(12,7,'h')
        point(3,8,'h');point(12,8,'h');run(16,6,8,'u');point(8,17,'b')
        box(0,17,4,21,'D');box(1,18,3,20,'v');point(2,18,'V');point(2,19,'u');point(4,19,'q')
    else:raise ValueError(name)
    if layout:return rows,pal
    im=paint(rows,pal)
    if name=='merchant_silk':
        body=render('merchant')
        # Preserve the common body even where prop-adjacent outline tint would differ.
        for y in range(24):
            for x in range(16):
                if not(11<=x<=15 and 16<=y<=21):im.putpixel((x,y),body.getpixel((x,y)))
    return im

def generate():
    OUT.mkdir(parents=True,exist_ok=True);assets=[];profiles={};checks=[]
    for name,title in ROLES:
        im=render(name);im.save(OUT/f'{name}-front.png')
        subprocess.run([ASE,'-b',str(OUT/f'{name}-front.png'),'--save-as',str(OUT/f'{name}-front.aseprite')],check=True)
        reopened=OUT/f'{name}-reopened.png';subprocess.run([ASE,'-b',str(OUT/f'{name}-front.aseprite'),'--save-as',str(reopened)],check=True)
        assert Image.open(reopened).convert('RGBA').tobytes()==im.tobytes();reopened.unlink()
        assert im.size==(16,24) and set(im.getchannel('A').get_flattened_data())=={0,255} and im.getchannel('A').getbbox()[3]==24
        profiles[name]=dict(pixelsPerCell=[16,24],binaryAlpha=True,palette=sorted({'#%02x%02x%02x'%p[:3] for p in im.get_flattened_data() if p[3]}))
        aid='trial_binding_'+name;path=f'ArtReview/C1-Binding-Fronts/{name}-front.png'
        assets.append(dict(id=aid,kind='object',purpose='character',size=[1,1],image=path,source=dict(kind='authored',path=f'ArtReview/C1-Binding-Fronts/{name}-front.aseprite'),review=dict(status='pending'),profile=name,tiles=[dict(dx=0,dy=0,layer='obj',tile=aid,image=path)]))
        checks.append(dict(name=name,size=[16,24],binaryAlpha=True,bottom=24,asepriteReopenedExact=True,colors=len(profiles[name]['palette'])))
    merchant=render('merchant');silk=render('merchant_silk')
    for y in range(24):
        for x in range(16):
            if not(11<=x<=15 and 16<=y<=21):assert merchant.getpixel((x,y))==silk.getpixel((x,y))
    woman=Image.open(ROOT/'ArtReview/Approved/woman-front-v4.png').convert('RGBA');rice=render('woman_ricecake')
    for y in range(24):
        for x in range(16):
            if not(y<=6 or (1<=x<=14 and 16<=y<=20)):assert woman.getpixel((x,y))==rice.getpixel((x,y))
    (OUT/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=profiles,assets=assets),ensure_ascii=False,indent=2)+'\n')
    (OUT/'checks.json').write_text(json.dumps(dict(characters=checks,merchantVariantBodyExactOutsideProp=True,womanVariantExactOutsideProps=True,userApproval='pending',D3PaletteApproval=False),ensure_ascii=False,indent=2)+'\n')
    review()
    print('5 native front PNG/Aseprite pairs reopened; body variants preserved outside prop regions')

def review():
    font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
    old=[('chief','나루 촌장'),('woman','아낙'),('trader','행상')]+[(n,t) for n,t,_,_ in R.ROLES]
    people=[(n,t,ROOT/f'ArtReview/Approved/C1-Walk/frames/{n}_d0.png') for n,t in old]+[(n,t,OUT/f'{n}-front.png') for n,t in ROLES]
    board=Image.new('RGB',(1000,805),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((18,10),'C1 역할 보완 · 승인 10명과 새 정면 5명',font=font(25),fill='#332d2a')
    for row in range(3):
        y=75+row*240;d.text((18,y-22),'승인된 인물' if row<2 else '새 시안 · 승인 대기',font=font(17),fill='#685b51')
        for col,(name,title,path) in enumerate(people[row*5:row*5+5]):
            x=25+col*200;im=Image.open(path).convert('RGBA');z=im.resize((96,144),Image.Resampling.NEAREST)
            d.text((x,y+2),title,font=font(18),fill='#332d2a');d.rectangle((x-3,y+29,x+98,y+176),fill='#d8d2c4');board.paste(z,(x,y+31),z);board.paste(im,(x+40,y+185),im)
            sil=Image.new('RGBA',im.size,(55,48,43));sil.putalpha(im.getchannel('A'));sil=sil.resize((48,72),Image.Resampling.NEAREST);board.paste(sil,(x+112,y+68),sil)
    d.text((18,783),'모두 16×24 · 같은 6배 확대 / 실제 크기 / 실루엣',font=font(15),fill='#685b51')
    board.save(OUT/'all-fifteen-fronts.png')
    # Body-preserving variant pairs are shown explicitly.
    pairs=[('시전 상인 → 비단 장수',render('merchant'),render('merchant_silk')),('아낙 → 떡 장수',Image.open(ROOT/'ArtReview/Approved/woman-front-v4.png').convert('RGBA'),render('woman_ricecake'))]
    b=Image.new('RGB',(700,430),'#f5f0e6');draw=ImageDraw.Draw(b)
    for row,(title,a,z) in enumerate(pairs):
        y=20+row*205;draw.text((18,y),title,font=font(20),fill='#332d2a')
        for x,im in ((250,a),(470,z)):
            s=im.resize((96,144),Image.Resampling.NEAREST);b.paste(s,(x,y+35),s)
    b.save(OUT/'prop-variants.png')

if __name__=='__main__':generate()
