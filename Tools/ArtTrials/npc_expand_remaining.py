"""Seven native front NPCs; pending visual review, isolated from runtime assets."""
from pathlib import Path
import json
import subprocess
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/C1-Expand-2'
ASE='/Applications/Aseprite.app/Contents/MacOS/aseprite'
ROLES=[('farmer','장단 농부','villager_dog','goryeo_jangdan_farmer'),
       ('herbalist','송악 약재상','villager_rabbit','goryeo_songak_merchant'),
       ('dakmok_chief','닥목 촌장','dakmokchief','dakmok_chief'),
       ('gaekju','개경 객주','gaekju','gaekju'),
       ('child','아이','villager_fox',None),
       ('boatman','뱃사공','villager_bear',None),
       ('bandit','도적','bandit','bandit')]
BASE=dict(q=(237,193,157),Q=(248,219,186),S=(207,153,119),p=(228,147,144),N=(175,109,85),
          r=(57,43,42),R=(91,68,57),e=(34,27,32),E=(46,33,32),w=(253,247,233),
          h=(44,42,56),H=(96,96,122),c=(230,225,234),a=(189,180,198),b=(202,190,169),
          k=(96,72,58),s=(66,48,42),t=(179,131,74),T=(227,191,120),d=(108,76,45),
          j=(174,127,85),J=(221,185,137),D=(78,55,39),v=(87,107,144),V=(124,151,181),
          g=(126,159,109),G=(83,119,77),u=(241,233,216),U=(204,195,177),o=(79,66,70))

def render(name):
    rows=[list('.'*16) for _ in range(24)];pal=dict(BASE)
    def point(x,y,ch):
        assert 0<=x<16 and 0<=y<24
        rows[y][x]=ch
    def run(y,a,b,ch):
        for x in range(a,b+1):point(x,y,ch)
    def box(x0,y0,x1,y1,ch):
        for y in range(y0,y1+1):run(y,x0,x1,ch)
    # Shared woman's rounded face, separated eyes/hair, warm small mouth.
    for y,a,b in [(8,4,11),(9,3,12),(10,2,13),(11,2,13),(12,2,13),(13,3,12),(14,4,11),(15,6,9)]:
        run(y,a,b,'q');point(a,y,'o');point(b,y,'o')
    box(4,9,10,10,'Q');point(1,12,'S');point(14,12,'S');point(2,12,'q');point(13,12,'S')
    run(14,5,10,'Q');run(15,6,9,'S')
    for x in (5,9):
        point(x,11,'w');point(x+1,11,'e');point(x,12,'e');point(x+1,12,'E')
    point(7,12,'S');point(3,13,'p');point(12,13,'p');point(7,14,'N');point(8,14,'q')
    run(8,4,11,'r');run(9,3,4,'r');run(9,11,12,'r');point(4,8,'R')
    # Default robe, separate hands, three native rows of legs.
    for y,shape in enumerate(['....occuccco....','...oaacuucaao...','..oaabbbbbbaao..','..oqaccccccqSo..','...oaaaaaaaao...'],16):
        rows[y]=list(shape)
    for y,shape in enumerate(['....okkookko....','....okkookko....','....ossoosso....'],21):rows[y]=list(shape)
    if name=='farmer':
        pal.update(c=(208,185,137),a=(174,148,107),b=(137,105,70),k=(120,82,56))
        run(4,6,9,'d');run(5,4,11,'t');run(6,3,12,'t');run(6,4,10,'T');run(7,1,14,'d');run(7,2,12,'t')
        # Rolled cuffs and a tied bundle held close to the right hand.
        point(3,18,'u');point(12,18,'u');box(12,15,14,21,'t');run(14,13,14,'T')
        for y in (15,16,17,20):point(13,y,'T')
        run(18,12,14,'d');point(12,19,'q');point(11,19,'Q')
    elif name=='herbalist':
        pal.update(c=(126,159,109),a=(83,119,77),b=(204,195,177))
        run(4,7,8,'h');run(5,5,10,'h');run(6,4,11,'h');run(7,3,12,'h');run(6,5,6,'H');point(6,5,'H')
        # Tied herb cloth bundle; two leaf tips connect directly to its opening.
        box(0,17,3,20,'G');box(1,17,2,19,'g');point(1,16,'G');point(2,15,'g');point(2,16,'g');run(18,0,3,'b');point(3,19,'q')
    elif name=='dakmok_chief':
        pal.update(c=(174,127,85),a=(137,96,66),b=(221,185,137),r=(126,117,105),R=(159,150,136))
        box(5,4,10,6,'h');run(4,6,9,'H');run(7,4,11,'h');point(5,5,'H')
        point(5,10,'u');point(9,10,'u');point(5,14,'R');point(10,14,'r')
        # Closed paper scroll kept separate from the face.
        box(12,17,15,20,'U');box(13,17,14,20,'u');run(16,12,15,'d');run(21,12,15,'d');point(12,19,'q')
    elif name=='gaekju':
        pal.update(c=(124,151,181),a=(87,107,144),b=(221,185,137))
        box(5,5,10,6,'h');point(6,5,'H');run(7,1,14,'h');run(7,2,12,'H')
        # Wider shoulders/waist than the villagers, with a ledger against the torso.
        for y,shape in enumerate(['...occcucccco...','..oaaccuuccaao..','.oaabbbbbbbbaao.','.oqaccccccccqSo.','..oaaaaaaaaaao..'],16):rows[y]=list(shape)
        box(0,17,4,21,'D');box(1,18,3,20,'u');point(2,18,'U');point(2,19,'D');point(2,20,'U');point(4,19,'q')
    elif name=='child':
        pal.update(c=(230,153,174),a=(194,119,143),b=(215,138,112))
        # A lower, narrower head and shorter body; same foot baseline and pixel density.
        adult=[row[:] for row in rows]
        rows=[list('.'*16) for _ in range(24)]
        for y in range(8,16):
            for x in range(3,13):rows[y+2][x]=adult[y][x]
        run(9,5,10,'r');run(10,4,11,'r');point(5,10,'R');point(12,10,'r');point(12,11,'r');point(13,11,'b')
        point(13,12,'b');point(13,13,'b')
        for y,shape in enumerate(['.....occco......','....oaucbao.....','...oQacbccqSo...','....oaaaaaao....'],18):rows[y]=list(shape)
        run(19,4,5,'g');run(19,10,11,'v');run(20,4,5,'v');run(20,10,11,'b')
        run(22,5,6,'k');run(22,9,10,'k');run(23,5,6,'s');run(23,9,10,'s')
        # Small spinning top held at one side, connected by the hand.
        point(2,19,'q');run(20,1,3,'t');point(2,21,'T');point(2,22,'d')
    elif name=='boatman':
        pal.update(c=(124,169,184),a=(84,128,145),b=(241,233,216))
        run(6,5,10,'r');run(7,3,12,'r');run(8,3,12,'u');run(8,4,10,'U');point(2,8,'u');point(1,9,'U')
        # One complete oar, with a blade and a hand touching its shaft.
        box(14,14,15,17,'j');box(14,18,14,22,'D');point(13,18,'q');point(14,15,'J');point(14,16,'J')
    elif name=='bandit':
        pal.update(c=(72,66,86),a=(52,48,64),b=(157,117,168),k=(54,46,52),t=(137,105,70),T=(179,131,74))
        run(4,6,9,'d');run(5,4,11,'t');run(6,3,12,'t');run(6,4,10,'T');run(7,1,14,'d');run(7,2,12,'t')
        # Cloth below eyes; eye highlights stay visible and no animal muzzle remains.
        run(13,3,12,'h');run(14,4,11,'h');run(15,6,9,'h');run(13,5,10,'H')
        box(14,15,15,17,'D');point(14,15,'j');box(14,18,14,22,'d');point(13,18,'q')
    assert all(len(row)==16 for row in rows)
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
                groups=[family(v) for v in [at(x+dx,y+dy) for dx,dy in ((0,1),(0,-1),(-1,0),(1,0))] if v not in '.o']
                group=max(dict.fromkeys(groups),key=groups.count) if groups else 'clothes'
                color={'skin':(119,83,69),'hair':(38,30,33),'hat':(31,28,37),'wood':(78,55,39),'clothes':(79,66,70)}[group]
            else:
                color=pal[ch]
                if at(x+1,y)=='o' or at(x,y+1)=='o':
                    if ch=='q':color=pal['S']
                    elif ch=='c':color=pal['a']
                    elif ch=='r':color=(42,32,33) if name!='dakmok_chief' else (96,88,82)
            im.putpixel((x,y),(*color,255))
    return im

def generate():
    OUT.mkdir(parents=True,exist_ok=True);assets=[];profiles={};checks=[]
    for name,title,animal,npcid in ROLES:
        im=render(name);im.save(OUT/f'{name}-front.png')
        subprocess.run([ASE,'-b',str(OUT/f'{name}-front.png'),'--save-as',str(OUT/f'{name}-front.aseprite')],check=True)
        reopened=OUT/f'{name}-reopened.png';subprocess.run([ASE,'-b',str(OUT/f'{name}-front.aseprite'),'--save-as',str(reopened)],check=True)
        assert Image.open(reopened).convert('RGBA').tobytes()==im.tobytes();reopened.unlink()
        a=im.getchannel('A');assert set(a.get_flattened_data())=={0,255};assert a.getbbox()[3]==24
        profiles[name]={'pixelsPerCell':[16,24],'binaryAlpha':True,'palette':sorted({'#%02x%02x%02x'%p[:3] for p in im.get_flattened_data() if p[3]})}
        aid=f'trial_c1_expand_2_{name}';path=f'ArtReview/C1-Expand-2/{name}-front.png'
        assets.append(dict(id=aid,kind='object',purpose='character',size=[1,1],image=path,source=dict(kind='authored',path=f'ArtReview/C1-Expand-2/{name}-front.aseprite'),review=dict(status='pending'),profile=name,tiles=[dict(dx=0,dy=0,layer='obj',tile=aid,image=path)]))
        checks.append(dict(name=name,title=title,existingNpcId=npcid,animalComparison=animal,size=[16,24],visibleBox=a.getbbox(),colors=len(profiles[name]['palette']),binaryAlpha=True,bottom=24,asepriteReopenedPixelEqual=True))
    (OUT/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=profiles,assets=assets),ensure_ascii=False,indent=2)+'\n')
    (OUT/'checks.json').write_text(json.dumps(dict(characters=checks,userApproval='pending',scope='seven front sprites only',D3PaletteApproval=False),ensure_ascii=False,indent=2)+'\n')
    review()
    print('7 PNG/Aseprite pairs: size, binary alpha, bottom baseline, reopen verified')

def review():
    font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
    # Split into two legible pages, old animal vs new human, and frozen reference.
    for page,roles in enumerate((ROLES[:4],ROLES[4:]),1):
        board=Image.new('RGB',(1000,90+len(roles)*190),'#f5f0e6');d=ImageDraw.Draw(board)
        d.text((18,10),f'C1 확대 ② · 나머지 NPC 정면 ({page}/2)',font=font(23),fill='#332d2a')
        for x,title in [(220,'기존 동물'),(450,'사람 시안'),(760,'승인 기준 · 아낙')]:d.text((x,51),title,font=font(17),fill='#332d2a')
        for row,(name,title,animal,npcid) in enumerate(roles):
            y=85+row*190;d.text((15,y+45),title,font=font(20),fill='#332d2a')
            paths=[ROOT/f'Assets/PocketCaravan/Art/Characters/{animal}_tall_d0.png',OUT/f'{name}-front.png',ROOT/'ArtReview/Approved/woman-front-v4.png']
            for x,path in zip((225,455,765),paths):
                im=Image.open(path).convert('RGBA');z=im.resize((96,144),Image.Resampling.NEAREST);d.rectangle((x-4,y-2,x+99,y+145),fill='#d8d2c4');board.paste(z,(x,y),z);board.paste(im,(x+40,y+154),im)
                if x==455:
                    sil=Image.new('RGBA',im.size,(55,48,43));sil.putalpha(im.getchannel('A'));z=sil.resize((48,72),Image.Resampling.NEAREST);board.paste(z,(x+116,y+44),z)
        board.save(OUT/f'comparison-{page}.png')

if __name__=='__main__':generate()
