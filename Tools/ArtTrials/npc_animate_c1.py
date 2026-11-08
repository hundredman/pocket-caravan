"""C1 native four-direction walk trials. Frozen down/idle fronts remain exact."""
from pathlib import Path
import hashlib,json,subprocess,sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
import npc_expand_remaining as R
import npc_polish_v4 as V

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
OUT=ROOT/'ArtReview/C1-Walk';ASE='/Applications/Aseprite.app/Contents/MacOS/aseprite'
PEOPLE=[('chief','나루 촌장','chief-front-c1-expand-1'),('woman','아낙','woman-front-v4'),('trader','행상','trader-front-c1-expand-1')]+[(n,t,n+'-front-c1-expand-2') for n,t,_,_ in R.ROLES]

def layout(name):
    if name not in ('chief','woman','trader'):return R.render(name,layout=True)
    result={};old=V.C.render;V.ACTIVE=name
    def capture(rows,pal):
        im=V.clean_render(rows,pal);result['rows']=[r[:] for r in rows];result['pal']=dict(pal);return im
    V.C.render=capture
    try:V.V3.render(name,raised_arm=False,separate_right_eye=True)
    finally:V.C.render=old
    rows,pal=result['rows'],result['pal']
    if name=='chief':
        pal.update(c=(241,233,216),a=(204,195,177),b=(202,190,169))
        rows[10][6]=rows[10][10]='Q'
    for row in rows:
        for x,ch in enumerate(row):
            if ch=='f':row[x]='q'
            elif ch=='l':row[x]='c'
    pal.update(D=(78,55,39),j=(174,127,85),J=(221,185,137),t=(179,131,74),T=(227,191,120),d=(108,76,45),u=(241,233,216),U=(204,195,177),v=(87,107,144))
    return rows,pal

def pose(name,direction):
    front,pal=layout(name);rows=[r[:] for r in front]
    child=name=='child';offset=2 if child else 0
    if direction=='u':
        # Same skull/ears, with a genuine back of head instead of facial marks.
        for y in range(8+offset,16+offset):
            for x in range(16):
                if rows[y][x] in 'qQSpNeEwBZ':rows[y][x]='r'
        for x,y in [(5,9+offset),(6,9+offset),(5,10+offset),(6,10+offset)]:
            if rows[y][x]!='.':rows[y][x]='R'
        if name=='woman':
            for y in (10,11,12):rows[y][12]='r';rows[y][13]='R'
        if name=='chief':rows[16][7]=rows[16][8]='c'
        # Remove front lapels/ties, keep hands where connected to a sleeve.
        for y in range(16+offset,21):
            for x,ch in enumerate(rows[y]):
                if ch in 'uUb':rows[y][x]='c'
        return rows,pal
    # Right profile: narrower face, one eye, visible nose. Keep hat/jar silhouette above.
    for y in range(8+offset,16+offset):rows[y]=list('.'*16)
    for y,a,b in [(8,6,10),(9,5,11),(10,4,12),(11,4,12),(12,4,13),(13,5,13),(14,6,12),(15,7,10)]:
        y+=offset
        for x in range(a,b+1):rows[y][x]='q'
        rows[y][a]=rows[y][b]='o'
    for y in (9+offset,10+offset):
        for x in range(6,11):rows[y][x]='Q'
    for x in range(6,11):rows[8+offset][x]='r'
    for y in range(9+offset,14+offset):rows[y][4 if y<12+offset else 5]='r'
    # Cover crown, temple and nape as connected hair masses; leave skin above the eye.
    for y,a,b in [(9,5,10),(10,4,8),(11,4,7),(12,4,6),(13,5,6)]:
        for x in range(a,b+1):rows[y+offset][x]='r'
    rows[9+offset][6]='R';rows[10+offset][5]='R'
    rows[12+offset][6]='q'  # small visible ear, framed by hair rather than a bare scalp

    for x,y,ch in [(10,11,'w'),(11,11,'e'),(10,12,'e'),(11,12,'E'),(7,13,'p'),(12,13,'S'),(11,14,'N')]:rows[y+offset][x]=ch
    if name=='chief':
        rows[10][10]='B'
        for x,y in [(9,14),(10,14),(11,15),(9,15),(10,15),(9,16)]:rows[y][x]='B'
    if name=='woman':
        for y in (8,9,10):rows[y][3]='r'
        rows[9][2]='R';rows[10][3]='r';rows[9][4]='g'
    if name=='boatman':
        for x in range(4,12):rows[8][x]='u'
        rows[8][5]='U'
    if name=='bandit':
        for y in (13,14):
            for x in range(6,13):rows[y][x]='h'
        rows[13][9]='H'
    start=18 if child else 16
    for y in range(start,24):rows[y]=list('.'*16)
    body=['.....occco......','....occcccco....','....obbbbbbo....','....ocqcccco....','....oaaaaaao....']
    if child:body=['......occco.....','.....ogcbao.....','.....ocqcco.....','.....oaaaao.....']
    for y,row in enumerate(body,start):
        if y<22:rows[y]=list(row)
    if name=='woman':
        rows[20]=list('....ovvvvvvo....');rows[21]=list('....ovvvvvvo....')
    if child:
        rows[22]=list('......okko......');rows[23]=list('......osso......')
    else:
        for y,row in enumerate(V.C.LEGS_SIDE[0],21):rows[y]=list(row)
    if name=='gaekju':
        # A broader belly survives in profile.
        for y in (17,18,19):rows[y][11]='c';rows[y][12]='o'
    if name=='chief':rows[16][9]='B'
    return rows,pal

def add_props(rows,name,direction):
    def rect(x0,y0,x1,y1,ch):
        for y in range(y0,y1+1):
            for x in range(x0,x1+1):rows[y][x]=ch
    # Props turn with the person; they are authored once per pose, not stretched textures.
    if name=='chief':rect(14,16,14,22,'d');rows[18][13]='q';rows[16][14]='j'
    elif name=='trader':
        rect(0,12,4,21,'D');rect(1,13,3,20,'j');rect(2,12,2,21,'T')
        rows[18][5]='q'
    elif name=='farmer' and direction!='u':
        rect(0,16,3,21,'t');rect(1,15,1,20,'T');rect(2,15,2,20,'T');rect(0,18,3,18,'d');rows[19][4]='q'
    elif name=='herbalist' and direction!='u':
        rect(1,17,3,20,'G');rows[16][2]='g';rows[15][3]='g';rect(1,18,3,18,'b');rows[19][4]='q'
    elif name=='dakmok_chief' and direction!='u':
        rect(12,18,14,21,'U');rect(13,18,13,21,'u');rect(12,17,14,17,'d');rows[19][11]='q'
    elif name=='gaekju' and direction!='u':
        rect(12,18,15,21,'D');rect(13,19,14,20,'u');rows[19][12]='q'
    elif name=='boatman':
        rect(14,14,15,17,'j');rect(14,18,14,22,'D');rows[18][13]='q';rows[15][14]='J'
    elif name=='bandit':
        rect(14,15,15,17,'D');rect(14,18,14,22,'d');rows[18][13]='q';rows[15][14]='j'
    elif name=='child' and direction!='u':
        rows[20][11]='q';rect(12,20,14,20,'t');rows[21][13]='T';rows[22][13]='d'

def paint(rows,pal):
    im=Image.new('RGBA',(16,24))
    def at(x,y):return rows[y][x] if 0<=x<16 and 0<=y<24 else '.'
    for y,row in enumerate(rows):
        for x,ch in enumerate(row):
            if ch=='.':continue
            if ch=='o':color=(79,66,70)
            else:
                color=pal[ch]
                if at(x+1,y)=='o' or at(x,y+1)=='o':
                    if ch=='q':color=pal['S']
                    elif ch=='c':color=pal['a']
            im.putpixel((x,y),(*color,255))
    return im

def legs(im,pal,direction,frame,child=False,woman=False):
    # Plant one foot while the other lifts; head bob is at most one native pixel.
    if frame in (0,2):return im
    out=Image.new('RGBA',(16,24))
    for y in range(21):
        for x in range(16):
            p=im.getpixel((x,y))
            if p[3]:out.putpixel((x,y+1),p)
    for y in range(21,24):
        for x in range(16):
            # Keep held props extending below the body.
            if x<=3 or x>=13:out.putpixel((x,y),im.getpixel((x,y)))
    if child:
        for x in (5,6,9,10):out.putpixel((x,22),(*pal['k'],255))
        planted=(9,10) if frame==1 else (5,6)
        lifted=(5,6) if frame==1 else (9,10)
        for x in planted:out.putpixel((x,23),(*pal['s'],255))
        for x in lifted:out.putpixel((x,22),(*pal['s'],255))
        return out
    rows=V.C.LEGS_FRONT[frame] if direction in 'du' else V.C.LEGS_SIDE[frame]
    for j,row in enumerate(rows):
        for x,ch in enumerate(row):
            if ch!='.':out.putpixel((x,21+j),(*(pal.get(ch,(79,66,70)) if ch!='o' else (79,66,70)),255))
    if woman:
        # Keep the long skirt over the lifted knee, without altering the foot baseline.
        for x in range(5,11):out.putpixel((x,21),(*pal['v'],255))
    return out

def quantize(im,palette):
    mapping={}
    for p in im.get_flattened_data():
        if p[3] and p[:3] not in mapping:mapping[p[:3]]=min(palette,key=lambda c:sum((c[i]-p[i])**2 for i in range(3)))
    result=Image.new('RGBA',im.size);result.putdata([(*mapping[p[:3]],255) if p[3] else (0,0,0,0) for p in im.get_flattened_data()]);return result

def generate():
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'frames').mkdir(exist_ok=True)
    profiles={};assets=[];report=[]
    for name,title,approved in PEOPLE:
        front=Image.open(ROOT/f'ArtReview/Approved/{approved}.png').convert('RGBA');palette=sorted({p[:3] for p in front.get_flattened_data() if p[3]});_,pal=layout(name)
        poses={'d':front}
        for direction in 'ur':
            rows,pal=pose(name,direction);add_props(rows,name,direction);poses[direction]=paint(rows,pal)
        # Mirror geometry only; reapply palette/outline on the reflected semantic grid.
        rows,pal=pose(name,'r');add_props(rows,name,'r');rows=[r[::-1] for r in rows];poses['l']=paint(rows,pal)
        sheet=Image.new('RGBA',(64,96));frames=[]
        for di,direction in enumerate('dulr'):
            for f in range(4):
                im=quantize(legs(poses[direction],pal,direction,f,name=='child',name=='woman'),palette)
                if direction=='d' and f in (0,2):im=front.copy()
                im.save(OUT/'frames'/f'{name}_{direction}{f}.png');sheet.alpha_composite(im,(f*16,di*24));frames.append(im)
                assert im.size==(16,24) and set(im.getchannel('A').get_flattened_data())=={0,255}
                assert im.getchannel('A').getbbox()[3]==24
                assert {p[:3] for p in im.get_flattened_data() if p[3]}<=set(palette)
        assert frames[0].tobytes()==front.tobytes()
        assert all(len({frames[i*4+f].tobytes() for f in range(4)})>=3 for i in range(4))
        sheet.save(OUT/f'{name}-sheet.png')
        run_aseprite([ASE,'-b','--script-param',f'frames={OUT/"frames"}','--script-param',f'name={name}','--script-param',f'output={OUT/f"{name}.aseprite"}','--script',str(ROOT/'Tools/ArtTrials/npc_animate_c1.lua')],check=True,stdout=subprocess.DEVNULL)
        reopened=OUT/f'{name}-reopened.png'
        run_aseprite([ASE,'-b',str(OUT/f'{name}.aseprite'),'--sheet',str(reopened),'--sheet-columns','4','--data',str(OUT/f'{name}-animation.json'),'--format','json-array','--list-tags'],check=True)
        assert Image.open(reopened).convert('RGBA').tobytes()==sheet.tobytes();reopened.unlink()
        info=json.loads((OUT/f'{name}-animation.json').read_text());assert len(info['frames'])==16;assert [(t['name'],t['from'],t['to']) for t in info['meta']['frameTags']]==[(ch,i*4,i*4+3) for i,ch in enumerate('dulr')];assert all(f['duration']==125 for f in info['frames'])
        profile={'pixelsPerCell':[16,24],'binaryAlpha':True,'palette':['#%02x%02x%02x'%p for p in palette]};profiles[name]=profile
        # Register each full native frame individually; sheets are authoring/contact views.
        for di,direction in enumerate('dulr'):
            for f in range(4):
                aid=f'trial_walk_{name}_{direction}{f}';path=f'ArtReview/C1-Walk/frames/{name}_{direction}{f}.png'
                assets.append(dict(id=aid,kind='object',purpose='character',size=[1,1],image=path,source=dict(kind='authored',path=f'ArtReview/C1-Walk/{name}.aseprite'),review=dict(status='pending'),profile=name,tiles=[dict(dx=0,dy=0,layer='obj',tile=aid,image=path)]))
        report.append(dict(name=name,frames=16,approvedFrontExact=True,paletteSubsetOfApprovedFront=True,binaryAlpha=True,bottom=24,asepriteReopenedSheetExact=True,tags='dulr',durationMs=125,frontSHA256=hashlib.sha256(front.tobytes()).hexdigest()))
    (OUT/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=profiles,assets=assets),ensure_ascii=False,indent=2)+'\n')
    (OUT/'checks.json').write_text(json.dumps(dict(characters=report,scope='160 native frames; art review pending',D3PaletteApproval=False),ensure_ascii=False,indent=2)+'\n')
    review()
    print('160 frames, 10 tagged Aseprite animations reopened; approved down idle exact')

def review():
    font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
    for page,people in enumerate((PEOPLE[:5],PEOPLE[5:]),1):
        board=Image.new('RGB',(850,730),'#f5f0e6');d=ImageDraw.Draw(board)
        d.text((18,10),f'C1 4방향 · 정면 승인본과 비교 ({page}/2)',font=font(23),fill='#332d2a')
        for x,title in [(180,'승인 정면'),(330,'아래'),(450,'위'),(570,'왼쪽'),(690,'오른쪽')]:d.text((x,48),title,font=font(17),fill='#332d2a')
        for row,(name,title,approved) in enumerate(people):
            y=80+row*126;d.text((18,y+40),title,font=font(17),fill='#332d2a')
            paths=[ROOT/f'ArtReview/Approved/{approved}.png']+[OUT/'frames'/f'{name}_{direction}0.png' for direction in 'dulr']
            for x,path in zip((180,330,450,570,690),paths):
                im=Image.open(path).convert('RGBA');z=im.resize((64,96),Image.Resampling.NEAREST);board.paste(z,(x,y),z);board.paste(im,(x+24,y+99),im)
        board.save(OUT/f'directions-{page}.png')
    walk=[]
    for f in range(4):
        board=Image.new('RGB',(850,690),'#f5f0e6');d=ImageDraw.Draw(board);d.text((15,8),'10명 걷기 · 8fps · 아래 / 위 / 왼쪽 / 오른쪽',font=font(21),fill='#332d2a')
        for row,(name,title,_) in enumerate(PEOPLE):
            y=45+row*63;d.text((15,y+16),title,font=font(16),fill='#332d2a')
            for x,direction in zip((250,390,530,670),'dulr'):
                im=Image.open(OUT/'frames'/f'{name}_{direction}{f}.png').convert('RGBA');z=im.resize((32,48),Image.Resampling.NEAREST);board.paste(z,(x,y),z)
        walk.append(board)
    walk[0].save(OUT/'walk.gif',save_all=True,append_images=walk[1:],duration=125,loop=0)

if __name__=='__main__':generate()
