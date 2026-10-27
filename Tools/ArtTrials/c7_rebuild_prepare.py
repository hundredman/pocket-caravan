"""A5 cleanup of whole generated C7 rebuilds; palettes remain trial proposals."""
import json
from pathlib import Path
import sys
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/C7-Rebuild-20261027'
sys.path.insert(0,str(ROOT/'Tools/ArtPrep'))
from prepare_art import prepare

HOUSE=['34271f','b77c3e','e2ac52','f7d17b','69452f','9b653c','c18b52',
       'bfa679','ddc696','f2dfb3','7d7970','a59d89','c5bba3','516877','8198a4','b0c1c6']
GROUND=['8ca174','899e72','cdb48a','cab187']


def run():
    for name in ['thatch','tile-house','ground-source']:
        colors=GROUND if name=='ground-source' else HOUSE
        palette=[tuple(bytes.fromhex(c)) for c in colors]
        with Image.open(OUT/'Prepared'/(name+'-seed.png')) as source:
            im,report=prepare(source,source.size,palette)
        im.save(OUT/'Prepared'/(name+'.png'))
        (OUT/'Prepared'/(name+'-a5.json')).write_text(json.dumps(report,indent=2)+'\n')
    for name,colors in [('house',HOUSE),('ground',GROUND)]:
        (OUT/(name+'-trial-palette.txt')).write_text('// Trial proposal only; not D3 approval\n'+'\n'.join('#'+c for c in colors)+'\n')
    print('A5: 3 new sources; native grid, explicit palettes, binary alpha')


if __name__=='__main__':run()
