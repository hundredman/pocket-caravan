"""A5/A2 of separate whole water decorations. The rounded pond is unchanged."""
from pathlib import Path
import json,sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/C7-PondDecor-20261027'
sys.path.insert(0,str(ROOT/'Tools/ArtPrep'));sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from prepare_art import prepare
from asset_manifest import validate
from c7_catalogue import rgba_evidence
PALETTE=['2d4536','3e6345','65874b','96b55f','9c536e','d88da4','f3bfcb','edc878']

def prep():
    for name in ['leaves','flower']:
        with Image.open(OUT/'Prepared'/(name+'-seed.png')) as source:
            im,report=prepare(source,(16,16),[tuple(bytes.fromhex(c)) for c in PALETTE])
        im.save(OUT/'Prepared'/(name+'.png'))
        (OUT/'Prepared'/(name+'-a5.json')).write_text(json.dumps(report,indent=2)+'\n')
    (OUT/'candidate-palette.txt').write_text('// Candidate colors only; not D3 approval\n'+'\n'.join('#'+c for c in PALETTE)+'\n')

def register():
    assets=[]
    for name in ['leaves','flower']:
        path='ArtReview/C7-PondDecor-20261027/Native/'+name
        assets.append(dict(id='c7_pond_'+name,kind='object',purpose='floating_'+name,profile='native16',size=[1,1],image=path+'.png',source=dict(kind='ai_processed',path=path+'.aseprite'),review=dict(status='pending'),rules=dict(onTerrain=['water'],forbidRoad=True),tiles=[dict(dx=0,dy=0,layer='obj',tile='c7_pond_'+name,image=path+'.png')]))
    doc=dict(schemaVersion=1,profiles=dict(native16=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#'+c for c in PALETTE])),assets=assets)
    layout=dict(id='pond_decor_trial',assetPlacementVersion=1,w=5,h=3,palette=['','c7_pond_leaves','c7_pond_flower'],ground=[0]*15,deco=[0]*15,obj=[0]*15,high=[0]*15,road=[0]*15,terrainTags=['water']*15,assetPlacements=[])
    for x,name,key in [(1,'leaves',1),(2,'flower',2)]:
        layout['obj'][5+x]=key;layout['assetPlacements'].append(dict(id=name,assetId='c7_pond_'+name,purpose='floating_'+name,x=x,y=1))
    errors=validate(doc,[('pond_decor',layout)],ROOT,False);assert not errors,errors
    with Image.open(ROOT/'ArtReview/C7-RoundPond-20261027/pond.png') as source:pond=source.convert('RGBA')
    count=0
    for x,name in [(16,'leaves'),(32,'flower')]:
        with Image.open(OUT/'Native'/(name+'.png')) as source:im=source.convert('RGBA')
        for y in range(16):
            for xx in range(16):
                if im.getpixel((xx,y))[3]:
                    assert pond.getpixel((x+xx,16+y))==(127,185,175,255),(name,xx,y)
                    count+=1
    pond_count=sum(1 for p in pond.get_flattened_data() if p[3])
    (OUT/'catalogue.json').write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n')
    (OUT/'layout.json').write_text(json.dumps(layout,indent=2)+'\n')
    (OUT/'checks.json').write_text(json.dumps(dict(errors=errors,approval='pending',decorationOpaquePixels=count,pondOpaquePixels=pond_count,coverageFraction=count/pond_count,pondImage=rgba_evidence(ROOT/'ArtReview/C7-RoundPond-20261027/pond.png'),objects={n:rgba_evidence(OUT/'Native'/(n+'.png')) for n in ['leaves','flower']}),indent=2)+'\n')
    print('A2 preview0errors; whole decorations fully inside open water; coverage %.1f%%'%(100*count/pond_count))

if __name__=='__main__':
    prep() if '--prep' in sys.argv else register()
