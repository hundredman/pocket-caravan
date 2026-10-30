"""Native directional walk for the five additional C1 binding roles."""
from pathlib import Path
import json,subprocess,sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
import npc_binding_fronts as F
import npc_animate_c1 as A
OUT=ROOT/'ArtReview/C1-Binding-Walk';ASE=F.ASE
ORIGINAL_LAYOUT=A.layout
ACTIVE=''

def layout(name):
    global ACTIVE
    # A.pose uses the woman's profile/bun/skirt rules for the ricecake seller.
    ACTIVE='woman_ricecake' if name=='woman' else name
    if ACTIVE!='woman_ricecake':return F.render(name,layout=True)
    rows,pal=ORIGINAL_LAYOUT('woman')
    for y in range(7):rows[y]=list('.'*16)
    def run(y,a,b,ch):
        for x in range(a,b+1):rows[y][x]=ch
    run(19,1,14,'D');run(20,2,13,'J');run(18,2,13,'K')
    for x in (3,8):
        run(16,x,x+2,'U');run(17,x,x+2,'U');run(16,x,x+1,'w');rows[17][x]='u'
    rows[18][1]='q';rows[18][14]='S'
    return rows,pal

def pose(name,direction):
    A.layout=layout
    try:rows,pal=A.pose('woman' if name=='woman_ricecake' else name,direction)
    finally:A.layout=ORIGINAL_LAYOUT
    # Specific hats/vests survive rotation without borrowing another role's props.
    if direction!='u' and name=='porter':
        for x in range(4,12):rows[8][x]='u'
        rows[8][5]='U'
    if direction!='u' and name in ('merchant','merchant_silk'):
        for y in range(16,20):rows[y][5]='a'
        rows[16][7]='u';rows[17][7]='u';rows[18][8]='u'
    return rows,pal

def props(rows,name,direction):
    def rect(x0,y0,x1,y1,ch):
        for y in range(y0,y1+1):
            for x in range(x0,x1+1):rows[y][x]=ch
    if name=='porter':
        # A-frame remains open and empty from back and side.
        for y in range(12,22):rows[y][0]='D';rows[y][3]='j'
        for y in (13,17,21):rect(0,y,3,y,'D')
        rows[14][2]='T';rows[18][4]='q'
    elif name=='merchant':
        rect(12,17,15,21,'D')
        if direction!='u':rect(13,18,14,20,'u');rows[19][13]='D';rows[19][11]='q'
        else:rect(13,18,14,20,'v')
    elif name=='merchant_silk':
        rect(12,17,15,21,'A');rect(13,17,14,21,'P');rect(12,19,15,19,'T');rows[17][13]='u'
        if direction!='u':rows[19][11]='q'
    elif name=='scholar':
        if direction=='u':rect(0,17,3,21,'D');rect(1,18,2,20,'v')
        else:rect(12,17,15,21,'D');rect(13,18,14,20,'v');rows[18][13]='V';rows[19][13]='u';rows[19][11]='q'
    elif name=='woman_ricecake':
        if direction=='u':
            # The front-held tray is occluded by the back; only its ends peek out.
            base,_=ORIGINAL_LAYOUT('woman')
            for y in range(16,21):
                rows[y]=[('c' if ch in 'uUb' else ch) for ch in base[y]]
            for x0,x1 in ((1,3),(12,14)):
                rect(x0,18,x1,18,'K');rect(x0,19,x1,19,'D')
            rows[17][3]='u';rows[17][12]='U'
        else:
            rect(9,18,15,18,'K');rect(9,19,15,19,'D');rect(10,20,14,20,'J');rect(11,16,13,17,'U');rect(11,16,12,16,'w');rows[17][11]='u';rows[18][9]='q'

def generate():
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'frames').mkdir(exist_ok=True)
    profiles={};assets=[];checks=[]
    for name,title in F.ROLES:
        front=Image.open(ROOT/f'ArtReview/Approved/C1-Binding-Fronts/{name}.png').convert('RGBA')
        palette=sorted({p[:3] for p in front.get_flattened_data() if p[3]});_,pal=layout('woman' if name=='woman_ricecake' else name)
        poses={'d':front}
        for direction in 'ur':
            rows,pal=pose(name,direction);props(rows,name,direction);poses[direction]=A.paint(rows,pal)
        rows,pal=pose(name,'r');props(rows,name,'r');poses['l']=A.paint([r[::-1] for r in rows],pal)
        sheet=Image.new('RGBA',(64,96));frames=[]
        for di,direction in enumerate('dulr'):
            for f in range(4):
                im=A.quantize(A.legs(poses[direction],pal,direction,f,woman=name=='woman_ricecake'),palette)
                if direction=='d' and f in (0,2):im=front.copy()
                im.save(OUT/'frames'/f'{name}_{direction}{f}.png');sheet.alpha_composite(im,(f*16,di*24));frames.append(im)
                assert im.size==(16,24) and set(im.getchannel('A').get_flattened_data())=={0,255} and im.getchannel('A').getbbox()[3]==24
                assert {p[:3] for p in im.get_flattened_data() if p[3]}<=set(palette)
        assert frames[0].tobytes()==front.tobytes() and all(len({frames[di*4+f].tobytes() for f in range(4)})>=3 for di in range(4))
        sheet.save(OUT/f'{name}-sheet.png')
        subprocess.run([ASE,'-b','--script-param',f'frames={OUT/"frames"}','--script-param',f'name={name}','--script-param',f'output={OUT/f"{name}.aseprite"}','--script',str(ROOT/'Tools/ArtTrials/npc_animate_c1.lua')],check=True,stdout=subprocess.DEVNULL)
        reopened=OUT/f'{name}-reopened.png';meta=OUT/f'{name}-animation.json'
        subprocess.run([ASE,'-b',str(OUT/f'{name}.aseprite'),'--sheet',str(reopened),'--sheet-columns','4','--data',str(meta),'--format','json-array','--list-tags'],check=True)
        assert Image.open(reopened).convert('RGBA').tobytes()==sheet.tobytes();reopened.unlink()
        d=json.loads(meta.read_text());assert len(d['frames'])==16 and all(f['duration']==125 for f in d['frames'])
        assert [(t['name'],t['from'],t['to']) for t in d['meta']['frameTags']]==[(ch,i*4,i*4+3) for i,ch in enumerate('dulr')]
        profiles[name]=dict(pixelsPerCell=[16,24],binaryAlpha=True,palette=['#%02x%02x%02x'%p for p in palette])
        for di,direction in enumerate('dulr'):
            for f in range(4):
                aid=f'trial_binding_walk_{name}_{direction}{f}';path=f'ArtReview/C1-Binding-Walk/frames/{name}_{direction}{f}.png'
                assets.append(dict(id=aid,kind='object',purpose='character',size=[1,1],image=path,source=dict(kind='authored',path=f'ArtReview/C1-Binding-Walk/{name}.aseprite'),profile=name,review=dict(status='pending'),tiles=[dict(dx=0,dy=0,layer='obj',tile=aid,image=path)]))
        checks.append(dict(name=name,frames=16,approvedFrontExact=True,binaryAlpha=True,bottom=24,paletteSubsetOfApprovedFront=True,asepriteReopenedSheetExact=True,tags='dulr',durationMs=125))
    (OUT/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=profiles,assets=assets),ensure_ascii=False,indent=2)+'\n')
    (OUT/'checks.json').write_text(json.dumps(dict(characters=checks,userApproval='pending for directions and walk',D3PaletteApproval=False),ensure_ascii=False,indent=2)+'\n')
    review();print('80 frames and 5 tagged Aseprite animations validated')

def review():
    font=lambda n:ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',n)
    board=Image.new('RGB',(850,735),'#f5f0e6');d=ImageDraw.Draw(board)
    d.text((18,10),'새 역할5명 · 승인 정면 / 4방향 비교',font=font(23),fill='#332d2a')
    for x,t in [(180,'승인 정면'),(330,'아래'),(450,'위'),(570,'왼쪽'),(690,'오른쪽')]:d.text((x,48),t,font=font(17),fill='#332d2a')
    for row,(name,title) in enumerate(F.ROLES):
        y=80+row*128;d.text((18,y+40),title,font=font(17),fill='#332d2a')
        paths=[ROOT/f'ArtReview/Approved/C1-Binding-Fronts/{name}.png']+[OUT/'frames'/f'{name}_{dr}0.png' for dr in 'dulr']
        for x,path in zip((180,330,450,570,690),paths):
            im=Image.open(path).convert('RGBA');z=im.resize((64,96),Image.Resampling.NEAREST);board.paste(z,(x,y),z);board.paste(im,(x+24,y+100),im)
    board.save(OUT/'directions.png')
    frames=[]
    for f in range(4):
        board=Image.new('RGB',(850,420),'#f5f0e6');d=ImageDraw.Draw(board);d.text((18,10),'새 역할5명 걷기 · 아래 / 위 / 왼쪽 / 오른쪽 · 8fps',font=font(21),fill='#332d2a')
        for row,(name,title) in enumerate(F.ROLES):
            y=55+row*70;d.text((18,y+16),title,font=font(17),fill='#332d2a')
            for x,direction in zip((250,390,530,670),'dulr'):
                im=Image.open(OUT/'frames'/f'{name}_{direction}{f}.png').convert('RGBA');z=im.resize((32,48),Image.Resampling.NEAREST);board.paste(z,(x,y),z)
        frames.append(board)
    frames[0].save(OUT/'walk.gif',save_all=True,append_images=frames[1:],duration=125,loop=0)

def encode_unity():
    frames=[]
    for direction in 'dulr':
        for f in range(4):
            im=Image.open(OUT/f'unity-{direction}{f}.png').convert('RGB')
            assert im.size==(1920,1080)
            frames.append(im)
    frames[0].save(OUT/'unity-walk.gif',save_all=True,append_images=frames[1:],duration=125,loop=0)
    print('16 actual Unity captures encoded as 8fps GIF')

if __name__=='__main__':
    if '--unity-gif' in sys.argv:encode_unity()
    else:generate()
