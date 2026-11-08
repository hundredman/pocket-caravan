"""Review art preparation: complete generated objects, native grid, explicit draft palette.
No production art is replaced. New images stay pending user approval.
"""
from pathlib import Path
from collections import Counter
import hashlib,json,subprocess
from PIL import Image

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/MarketCourts-20261030'
def run():
    OUT.joinpath('Native').mkdir(parents=True,exist_ok=True)
    colors=Counter()
    for p in ['ArtReview/Approved/BuildingShip-Renewal-v1/shop.png','ArtReview/Approved/C7-WorkshopProps-v4/harvest.png','ArtReview/Approved/C7-WorkshopProps-v4/celadon.png']:
        with Image.open(ROOT/p)as im:colors.update(p[:3]for p in im.convert('RGBA').getdata()if p[3])
    palette=[p for p,n in colors.most_common(28)]+[(246,228,191),(212,184,140),(109,148,130),(65,102,91)]
    checks=[]
    for name,size in [('grain_stall',(48,48)),('wares_counter',(48,32))]:
        p=OUT/'Raw'/f'{name}.png'
        if not p.exists():continue
        with Image.open(p)as im:im=im.convert('RGBA').resize(size,Image.Resampling.NEAREST)
        cache={};data=[]
        for c in im.getdata():
            if c[3]<128:data.append((0,0,0,0));continue
            rgb=c[:3]
            if rgb not in cache:cache[rgb]=min(palette,key=lambda v:sum((a-b)**2 for a,b in zip(rgb,v)))
            data.append((*cache[rgb],255))
        im.putdata(data);im.save(OUT/'Native'/f'{name}.png')
        run_aseprite(['/Applications/Aseprite.app/Contents/MacOS/aseprite','--batch',str(OUT/'Native'/f'{name}.png'),'--save-as',str(OUT/'Native'/f'{name}.aseprite')],check=True)
        checks.append(dict(id=name,size=size,rgbaSha256=hashlib.sha256(im.tobytes()).hexdigest(),binaryAlpha=set(im.getchannel('A').getdata())<={0,255},colors=len(set(im.getdata()))-1))
    OUT.joinpath('native-checks.json').write_text(json.dumps(dict(status='pending user visual approval',mode='built-in imagegen; offline nearest native-grid reduction; explicit existing art draft colors; Aseprite native file',palette=palette,assets=checks),indent=2)+'\n')
if __name__=='__main__':run()
