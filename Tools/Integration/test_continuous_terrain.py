"""Runtime geography, full scene-border walking, and atlas water footprint must agree."""
import collections
import json
import unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]

class ContinuousTerrain(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.config=json.loads((ROOT/'Assets/PocketCaravan/World/Terrain/continuous.json').read_text())
        cls.maps={mid:json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text()) for mid in cls.config['maps']}

    def test_open_ground_across_each_shared_border_has_reciprocal_exits(self):
        by_pos={tuple(p):mid for mid,p in self.config['maps'].items()}
        for (gx,gy),mid in by_pos.items():
            a=self.maps[mid];w,h=a['w'],a['h']
            for edge,delta in [('E',(1,0)),('S',(0,1))]:
                tid=by_pos.get((gx+delta[0],gy+delta[1]))
                if tid is None:continue
                b=self.maps[tid];valid=set();covered=set()
                for t in range(h if edge=='E' else w):
                    ps=[(w-1,t),(w-2,t),(0,t),(1,t)] if edge=='E' else [(t,h-1),(t,h-2),(t,0),(t,1)]
                    if all(m['reachable'][y*m['w']+x] for m,coords in ((a,ps[:2]),(b,ps[2:])) for x,y in coords):valid.add(t)
                for e in a['objects']:
                    if e['kind']!='exit' or e['target']!=tid:continue
                    start=e['y'] if edge=='E' else e['x'];length=e['h'] if edge=='E' else e['w']
                    covered.update(range(start,start+length))
                    target=next(s for s in b['objects'] if s['kind']=='spawn' and s['id']==e['spawn'])
                    self.assertEqual((e['w'],e['h']),(target['w'],target['h']))
                self.assertEqual(covered,valid,(mid,tid))

    def test_reachability_snapshot_matches_world_blocking_geometry(self):
        ground={(m['terrainX']+x,m['terrainY']+y) for m in self.maps.values() for y in range(m['h']) for x in range(m['w']) if not m['blocked'][y*m['w']+x]}
        m=self.maps['Naru'];sp=next(o for o in m['objects'] if o['kind']=='spawn' and o['id']=='S0')
        seen={(m['terrainX']+sp['x'],m['terrainY']+sp['y'])};queue=collections.deque(seen)
        while queue:
            x,y=queue.popleft()
            for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if p in ground and p not in seen:seen.add(p);queue.append(p)
        for m in self.maps.values():
            for y in range(m['h']):
                for x in range(m['w']):self.assertEqual(bool(m['reachable'][y*m['w']+x]),(m['terrainX']+x,m['terrainY']+y) in seen,(m['id'],x,y))

    def test_atlas_hydrology_pixels_equal_all_runtime_water_cells(self):
        base=ROOT/'Assets/PocketCaravan/Resources/WorldMap'
        for stem in ['Goryeo','GoryeoRegional']:
            atlas=json.loads((base/(stem+'Layout.json')).read_text());scale=atlas['pixelsPerTile']
            with Image.open(base/atlas['waterMask']) as image:
                mask=image.convert('L')
                for a in atlas['maps']:
                    m=self.maps[a['mapId']]
                    for y in range(m['h']):
                        for x in range(m['w']):
                            actual=mask.getpixel((int((a['x']+x)*scale+scale/2),int((a['y']+y)*scale+scale/2)))
                            self.assertEqual(actual,255 if m['water'][y*m['w']+x] else 0,(m['id'],x,y))

    def test_settlements_and_cargo_scope_are_not_replaced_by_landscape_sectors(self):
        markets={o['town'] for m in self.maps.values() for o in m['objects'] if o['kind']=='market'}
        self.assertEqual(markets,{'naru','gaegyeong','dakmok','goryeo_songak','goryeo_jangdan','transit_woodtown','transit_ferry'})
        for mid in ['goryeo_westcoast','goryeo_tidalcoast','goryeo_rivervalley','goryeo_southcoast']:
            self.assertFalse(any(o['kind'] in ('market','board','bandit') for o in self.maps[mid]['objects']))

if __name__=='__main__':unittest.main()
