"""Register a fixed rounded pond module arrangement; no production writes."""
from pathlib import Path
import json,sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/C7-RoundPond-20261027'
sys.path.insert(0,str(ROOT/'Tools/ArtPrep'))
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from prepare_art import prepare
from asset_manifest import validate
from c7_catalogue import rgba_evidence

def run(out=OUT,prefix="c7_rounded",colors=None,inner_path="ArtReview/Approved/A4-Pond/pieces/inner_corner.png"):
    colors=colors or [(132,103,94),(120,160,153),(127,185,175)]
    with Image.open(out/'pond-source.png') as source:im,prep=prepare(source,(80,48),colors)
    im.save(out/'pond.png');(out/'a5.json').write_text(json.dumps(prep,indent=2)+'\n')
    (out/'pieces').mkdir(exist_ok=True)
    pieces={};palette=[''];deco=[0]*35;placements=[]
    for y in range(3):
        for x in range(5):
            name=f'oval_{x}_{y}';key=prefix+'_'+name;p=out/'pieces'/(name+'.png')
            im.crop((x*16,y*16,x*16+16,y*16+16)).save(p)
            pieces[name]=dict(size=[1,1],image=str(p.relative_to(ROOT)),tiles=[dict(dx=0,dy=0,layer='deco',tile=key,image=str(p.relative_to(ROOT)))])
            palette.append(key);deco[(y+1)*7+x+1]=len(palette)-1
            placements.append(dict(id=name,assetId=prefix+'_pond',piece=name,purpose='small_grass_pond',x=x+1,y=y+1))
    pieces.update(center=pieces['oval_2_1'],edge=pieces['oval_2_0'],outer_corner=pieces['oval_0_0'])
    # Original modular inner-corner definition is retained as a complete piece;
    # the fixed oval has no concave corner and does not place it.
    inner=inner_path
    pieces['inner_corner']=dict(size=[1,1],image=inner,tiles=[dict(dx=0,dy=0,layer='deco',tile=prefix+'_inner_corner',image=inner)])
    doc=dict(schemaVersion=1,profiles=dict(native16=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#%02x%02x%02x'%c for c in colors])),assets=[dict(id=prefix+'_pond',kind='modular',moduleType='water',purpose='small_grass_pond',profile='native16',source=dict(kind='authored',path=str((out/'pond.aseprite').relative_to(ROOT))),review=dict(status='pending'),rules=dict(onTerrain=['grass'],forbidRoad=True),requiredPieces=list(pieces),pieces=pieces)])
    layout=dict(id=prefix+'_pond_trial',assetPlacementVersion=1,w=7,h=5,palette=palette,ground=[0]*35,deco=deco,obj=[0]*35,high=[0]*35,road=[0]*35,terrainTags=['grass']*35,assetPlacements=placements)
    errors=validate(doc,[('rounded_pond',layout)],ROOT,False)
    assert not errors,errors
    (out/'catalogue.json').write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n')
    (out/'layout.json').write_text(json.dumps(layout,indent=2)+'\n')
    (out/'a2.json').write_text(json.dumps(dict(errors=errors,approval='pending; fixed oval arrangement only'),indent=2)+'\n')
    (out/'pixels.json').write_text(json.dumps(dict(image=rgba_evidence(out/'pond.png'),originalApprovalPreserved=True),indent=2)+'\n')
    print('Rounded pond: A5 + A2 preview0errors, 15 complete cells, pending user approval')
if __name__=='__main__':run()
