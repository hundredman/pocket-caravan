"""Reproduce the preserved original grass/dirt autotile selection in a review stage.

Reads existing complete modular tiles; writes no production data or art.
"""
from pathlib import Path
import json,math,sys
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from tiles import autotile,DIRT_ORIGIN,GRASS

def grass(x,y):
    road_y=7+round(math.sin((x-5)*.22)*1.3)
    branch_x=11.5+y*.32+math.sin(y*.55)*.55
    return not (abs(y-road_y)<=1 or y<9 and abs(x-branch_x)<1.3)

def run():
    entries=[]
    for y in range(17):
        for x in range(30):
            # Original map arrays grow down; review world coordinates grow up.
            key=GRASS if grass(x,y) else autotile(lambda a,b:not grass(a,-b),x,-y,*DIRT_ORIGIN)
            image='Assets/PocketCaravan/Art/NinjaAdventure/Tiles/'+key.replace(':','_')+'_goryeo_jade.png'
            assert (ROOT/image).is_file(),image
            entries.append(dict(x=x,y=y,image=image))
    dest=ROOT/'ArtReview/C7-TeamStyle-20261027/GroundOrigin-v7'
    dest.mkdir(exist_ok=True)
    (dest/'original-ground.json').write_text(json.dumps(dict(source='Tools/MapGen/tiles.py autotile / DIRT_ORIGIN / GRASS; existing frozen jade tiles',entries=entries),indent=2)+'\n')
    print('Original ground selection:',len(entries),'cells;',len({e['image'] for e in entries}),'existing tiles; no art changed')
if __name__=='__main__':run()
