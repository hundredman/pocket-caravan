"""A2 registration of complete C7 rebuilt objects and purpose-designed modules."""
from pathlib import Path
import json
import sys
from PIL import Image
from c7_catalogue import rgba_evidence

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/C7-Rebuild-20261027'
OLD=ROOT/'ArtReview/C7-Trial4-20261026'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from asset_manifest import validate_catalogue


def rel(p):return str(p.relative_to(ROOT))


def shape(name,layer='obj'):
    image=OUT/'Native'/(name+'.png')
    with Image.open(image) as source:im=source.convert('RGBA')
    w,h=im.width//16,im.height//16
    assert im.size==(w*16,h*16)
    tiles=[]
    for y in range(h):
        for x in range(w):
            key=f'c7r3_{name.replace("-","_")}_{x}_{y}'
            p=OUT/'Cells'/(key+'.png')
            # The whole object is declared once; none of its render cells is reused.
            im.crop((x*16,y*16,(x+1)*16,(y+1)*16)).save(p)
            tiles.append(dict(dx=x,dy=y,layer=layer,tile=key,image=rel(p)))
    return dict(size=[w,h],image=rel(image),tiles=tiles)


def run():
    assert rgba_evidence(OUT/'Native/pine.png')==rgba_evidence(OLD/'Native/pine.png')
    profiles={}
    for family,names in [('house',['thatch','tile-house']),('ground',[p.stem for p in (OUT/'Native').glob('grass-*.png')]+['dirt']),('pine',['pine'])]:
        colors=set()
        for name in names:
            with Image.open(OUT/'Native'/(name+'.png')) as im:
                colors.update('#%02x%02x%02x'%v[:3] for v in im.convert('RGBA').get_flattened_data() if v[3])
        profiles[family]=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=sorted(colors))
    assets=[]
    for name in ['thatch','tile-house','pine']:
        assets.append(dict(id='c7r3_'+name.replace('-','_'),purpose=name,kind='object',profile='pine' if name=='pine' else 'house',source=dict(kind='ai_processed',path=rel(OUT/'Native'/(name+'.aseprite'))),review=dict(status='pending'),**shape(name)))
    pieces={p.stem:shape(p.stem,'ground') for p in sorted((OUT/'Native').glob('grass-*.png'))}
    aliases=dict(center='grass-255',edge='grass-110',outer_corner='grass-38',inner_corner='grass-127')
    pieces.update({k:pieces[v] for k,v in aliases.items()});pieces['dirt']=shape('dirt','ground')
    assets.append(dict(id='c7r3_grass_dirt',purpose='grass_dirt_boundary',kind='modular',moduleType='ground_boundary',profile='ground',source=dict(kind='ai_processed',path='Tools/ArtTrials/c7_rebuild_finish.lua'),review=dict(status='pending'),requiredPieces=list(aliases),pieces=pieces))
    doc=dict(schemaVersion=1,profiles=profiles,assets=assets)
    (OUT/'catalogue.json').write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n')
    errors,_=validate_catalogue(doc,ROOT)
    (OUT/'a2-result.json').write_text(json.dumps(dict(errors=errors,userApproval='pending'),indent=2)+'\n')
    (OUT/'native-rgba.json').write_text(json.dumps({rel(p):rgba_evidence(p) for p in sorted((OUT/'Native').glob('*.png'))},indent=2)+'\n')
    if errors:raise RuntimeError(errors)
    print('C7 rebuild A2: 4 assets / 47 boundary masks + dirt / 0 errors; user approval pending')


if __name__=='__main__':run()
