import hashlib
import json
import re
import unittest
from collections import deque
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
import sys
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from t3_history import map_at,resource_at
RESOURCE=ROOT/'Assets/PocketCaravan/Resources/Goryeo/Seats'
REVIEW=ROOT/'ArtReview/Goryeo-Seats-20261105/AlignedV2'


class SeatBindingTests(unittest.TestCase):
    def test_approved_native_pixels_and_import_profiles(self):
        approval=json.loads((REVIEW/'approval.json').read_text())
        config=resource_at(RESOURCE/'Config.json')
        self.assertEqual(approval['status'],'approved');self.assertEqual(config['status'],'approved')
        self.assertEqual(hashlib.sha256((REVIEW/'source.json').read_bytes()).hexdigest(),approval['sourceSha256'])
        original=json.loads((REVIEW/'source.json').read_text())
        self.assertEqual({p['originId']for p in config['poses']},{'goryeo','venice','viking','arabia'})
        self.assertEqual(len(config['poses']),4)
        for seat in original['seats']:
            for key,value in [('propX',0),('propY',0),('fire',False)]:seat.setdefault(key,value)
        for approved,live in zip(original['seats'],config['seats']):
            self.assertAlmostEqual(approved['lift'],live['lift'],places=6)
            self.assertEqual({k:v for k,v in approved.items()if k!='lift'},{k:v for k,v in live.items()if k!='lift'})
        refs={p['originId']:p for p in original['poses']}
        for pose in config['poses']:
            path=ROOT/'Assets/PocketCaravan/Resources'/Path(pose['file']+'.png')
            with Image.open(path)as live,Image.open(ROOT/refs[pose['originId']]['file'])as approved:
                self.assertEqual(live.size,approved.size)
                self.assertEqual(live.convert('RGBA').tobytes(),approved.convert('RGBA').tobytes())
            meta=path.with_suffix('.png.meta').read_text()
            self.assertIn('filterMode: 0',meta)
            self.assertEqual(float(re.search(r'spritePixelsToUnits: ([\d.]+)',meta)[1]),pose['ppu'])
        for i in range(4):
            with Image.open(RESOURCE/f'Fire/{i}.png')as live,Image.open(ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/camp_{12+i%2}_{5+i//2}.png')as old:
                self.assertEqual(live.convert('RGBA').tobytes(),old.convert('RGBA').tobytes())

    def test_fire_blocks_no_route_exit_or_new_land_component(self):
        cfg=resource_at(RESOURCE/'Config.json');m=map_at(ROOT/'Assets/PocketCaravan/Maps/transit_central_approach.json');w,h=m['w'],m['h']
        walk={(i%w,i//w)for i,g in enumerate(m['ground'])if g and not m['blocked'][i]}
        fire=next(s for s in cfg['seats']if s.get('fire'));cells={(fire['propX']+x,fire['propY']+y)for x in range(2)for y in range(2)}
        self.assertTrue(cells<=walk)
        self.assertTrue(all(m['road'][y*w+x]==0 for x,y in cells))
        for o in m['objects']:
            if o['kind']not in ['spawn','exit','npc','talker','market','board']:continue
            area={(x,y)for x in range(o['x'],o['x']+o.get('w',1))for y in range(o['y'],o['y']+o.get('h',1))}
            self.assertFalse(cells&area,o)
        def reach(allowed):
            start=(cfg['seats'][0]['x'],cfg['seats'][0]['y']);q=deque([start]);seen={start}
            while q:
                x,y=q.popleft()
                for p in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
                    if p in allowed and p not in seen:seen.add(p);q.append(p)
            return seen
        self.assertEqual(reach(walk)-cells,reach(walk-cells))


if __name__=='__main__':unittest.main()
