"""New whole-image style transfer inputs; A5 only, no facade redraw."""
from pathlib import Path
import json,sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/C7-TeamStyle-20261027'
sys.path.insert(0,str(ROOT/'Tools/ArtPrep'))
from prepare_art import prepare
HOUSE=['2e241d','b78137','daa34a','f0c365','68452d','98653b','bc8850',
       'c4a779','e0c99b','f4dfb5','77746c','a29b87','c4bca7','4a627a','718da3','a5b9c3']
GROUND=['8aab4d','b2cc73','6d8b3a','c4a277','e2c496','a2835e']

def run():
    for name in ['thatch','tile-house','ground-source']:
        colors=GROUND if name=='ground-source' else HOUSE
        with Image.open(OUT/'Prepared'/(name+'-seed.png')) as im:
            result,report=prepare(im,im.size,[tuple(bytes.fromhex(v)) for v in colors])
        result.save(OUT/'Prepared'/(name+'.png'))
        (OUT/'Prepared'/(name+'-a5.json')).write_text(json.dumps(report,indent=2)+'\n')
    for name,colors in [('house',HOUSE),('ground',GROUND)]:
        (OUT/(name+'-candidate-palette.txt')).write_text('// C7 proposal; not D3 shared-palette approval\n'+'\n'.join('#'+c for c in colors)+'\n')
    print('A5 team-style cleanup: 3 sources, explicit candidate colors, binary alpha')
if __name__=='__main__':run()
