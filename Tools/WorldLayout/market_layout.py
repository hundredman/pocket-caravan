"""Review-only structured market courts. No market/NPC/quest functions are added.
Authored slots follow plaza edges; the central customer aisle remains clear.
"""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2]

def arrange(mid,world_rect,place,group):
    x,y,w,h=world_rect
    # Canopied vendors at the back, low selling counters at the front.
    # A two-tile customer aisle separates them, rather than scattered yard props.
    for px in range(x,x+w-2,4):
        place('grain_stall',px,y,group)
    if h>=7:
        for px in range(x,x+w-2,4):
            place('wares_counter',px,y+h-2,group)
    elif h>=6:
        # Keep the lower approach open in shallow courts.
        place('wares_counter',x+w-3,y+h-2,group)


def event_edge(world_rect,place,group):
    x,y,w,h=world_rect
    # Wider event courts form a second compact market lane, not an empty yard.
    # Two rows leave a two-tile vertical customer aisle between left/right booths.
    if w>=8 and h>=5:
        place('grain_stall',x,y,group)
        place('grain_stall',x+w-3,y,group)
        place('wares_counter',x,y+h-2,group)
        place('wares_counter',x+w-3,y+h-2,group)
    else:
        for px,py in [(x,y),(x+w-3,y),(x,y+h-2),(x+w-3,y+h-2)]:
            if len(group['props'])>=2:break
            place('wares_counter',px,py,group)
