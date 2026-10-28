"""Review boards and native image checks. Does not replace any production asset."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/BuildingShip-Renewal'
LABELS={'cottage':'큰 초가집','pavilion':'정자','thatch':'작은 초가집','tile-house':'기와집','gaekju':'객주','gate':'성문','palace':'궁궐','shop':'시전','small_store':'곳간','boat':'배'}

def run():
    doc=json.loads((OUT/'manifest.json').read_text());assets=doc['assets'];checks=[]
    board=Image.new('RGB',(1440,1480),'#adbc3a')
    d=ImageDraw.Draw(board);font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',20)
    d.text((16,8),'건물 9종·배 1종 리뉴얼 — 각 항목 왼쪽 기존 / 오른쪽 수정 (동일 2배)',font=font,fill='#242421')
    for i,a in enumerate(assets):
        name=a['name'];old=Image.open(ROOT/a['original']).convert('RGBA');new=Image.open(OUT/'Native'/f'{name}.png').convert('RGBA')
        assert old.size==new.size==(a['width'],a['height'])
        assert set(new.getchannel('A').getdata())<={0,255}
        assert new.getbbox()is not None
        x=16+(i%2)*712;y=48+(i//2)*284
        d.text((x,y),LABELS[name]+f"  {a['width']}×{a['height']}",font=font,fill='#242421')
        baseline=y+250
        for j,im in enumerate((old,new)):
            z=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)
            board.paste(z,(x+12+j*350,baseline-z.height),z)
        colours=len(set(new.getdata())-{(0,0,0,0)})
        assert colours<=30,(name,colours)
        checks.append(dict(name=name,size=list(new.size),colors=colours,binaryAlpha=True,originalBBox=old.getbbox(),newBBox=new.getbbox(),
                           originalRGBA=hashlib.sha256(old.tobytes()).hexdigest(),newRGBA=hashlib.sha256(new.tobytes()).hexdigest()))
    board.save(OUT/'comparison.png')
    freeze=ROOT/'ArtReview/Approved/BuildingShip-Renewal-v1/approval.json'
    status='pending'
    if freeze.exists():
        approval=json.loads(freeze.read_text())
        hashes={a['name']:a['rgbaSha256']for a in approval['assets']}
        if approval['status']=='approved'and len(hashes)==len(checks)and all(hashes.get(a['name'])==a['newRGBA']for a in checks):
            status='approved'
    (OUT/'native-checks.json').write_text(json.dumps(dict(assets=checks,review=status,productionReplaced=False,mode='built-in imagegen + native Aseprite NEAREST and existing C7 draft colors'),ensure_ascii=False,indent=2)+'\n')
    if (OUT/'Unity/page-0-before.png').exists():
        gallery=Image.new('RGB',(984,924),'#20262c');d=ImageDraw.Draw(gallery)
        groups=['주거·곳간','정자·객주·시전','궁궐·성문·배']
        for page,label in enumerate(groups):
            y=page*308;d.text((10,y+3),label+' — 기존',font=font,fill='white');d.text((504,y+3),label+' — 수정',font=font,fill='white')
            for j,version in enumerate(('before','after')):
                gallery.paste(Image.open(OUT/f'Unity/page-{page}-{version}.png'),(8+j*488,y+32))
        gallery.save(OUT/'Unity/comparison.png')
    print('Native checks: 10 objects, same native canvases, binary alpha, <=30 draft colors. Approval: '+status)

if __name__=='__main__':run()
