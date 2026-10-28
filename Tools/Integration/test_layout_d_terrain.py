"""B2 isolation, conserved ownership, hydrology and connected walkable geometry."""
import os,sys as _sys;_sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)));from m4_superseded import superseded
import copy
import json
from pathlib import Path
import sys
import unittest

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
sys.path.insert(0,str(ROOT/'Tools/Routing'))
from layout_d import terrain, split_maps, validate, WorldLayout, OUT, neighbors, walking_routes, EIGHT
from layout_d_art import prepare
from asset_manifest import validate as validate_assets
from export_routes import build, serialized, BINDINGS
from check_distance_calibration import graph_from_roads, shortest_path


class LayoutDTerrainChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.layout=WorldLayout.read();cls.t=terrain(cls.layout);cls.maps=split_maps(cls.layout,cls.t)
        cls.stored=[json.loads(f.read_text()) for f in sorted((OUT/'Maps').glob('*.json'))]

    def test_all_assigned_cells_exist_once_and_no_bounding_rectangle_is_added(self):
        r=validate(self.layout,self.t,self.maps)
        self.assertEqual((r['maps'],r['assignedCells'],r['sourceLandCells'],r['nearSeaCells']),(13,45000,38253,6747))
        self.assertEqual(sum(sum(m['owned']) for m in self.maps),45000)
        self.assertEqual({m['id']:sum(m['owned']) for m in self.maps},{m['id']:sum(m['owned']) for m in self.stored})
        self.assertTrue(all(m['previewOnly'] and not any(o['kind'] in ('market','npc','exit') for o in m['objects']) for m in self.stored))

    def test_every_town_is_connected_by_actual_passable_cells(self):
        allowed=self.t['owned']-self.t['blocked'];start=next(iter(self.t['towns'].values()))
        seen={start};todo=[start]
        while todo:
            for q in neighbors(todo.pop()):
                if q in allowed and q not in seen:seen.add(q);todo.append(q)
        self.assertTrue(set(self.t['towns'].values())<=seen)
        self.assertGreater(len(seen),30000)
        self.assertEqual(len(self.t['roadRecords']),12)

    def test_rivers_have_mountain_sources_ordered_waypoints_and_sea_mouths(self):
        for original,river in zip(self.layout.data['rivers'],self.t['riverRecords']):
            path=[tuple(p) for p in river['cells']]
            self.assertIn(path[0],self.t['mountains'])
            self.assertNotIn(path[-1],self.layout.land)
            self.assertTrue(all(p in self.layout.owners for p in path))
            indices=[path.index(tuple(int(v//1) for v in p)) for p in original['waypoints']]
            self.assertEqual(indices,sorted(indices))
            self.assertTrue(all(sum(abs(x-y) for x,y in zip(a,b))==1 for a,b in zip(path,path[1:])))
        self.assertTrue(self.t['bridges']<=self.t['water'])
        self.assertFalse(self.t['bridges']&self.t['blocked'])
        self.assertTrue((self.t['water']-self.t['bridges'])<=self.t['blocked'])

    def test_mask_corruption_or_blocked_road_is_rejected(self):
        maps=copy.deepcopy(self.maps);i=next(i for i,v in enumerate(maps[0]['owned']) if v)
        maps[0]['owned'][i]=0
        with self.assertRaises(ValueError):validate(self.layout,self.t,maps)
        t=dict(self.t,blocked=self.t['blocked']|{tuple(self.t['roadRecords'][0]['cells'][0])})
        with self.assertRaisesRegex(ValueError,'Road blocked'):validate(self.layout,t,self.maps)

    def test_full_rendered_footprints_are_registered_and_unapproved(self):
        catalogue=json.loads((OUT/'catalogue.json').read_text())
        self.assertEqual(validate_assets(catalogue,[(m['id'],m) for m in self.stored],ROOT,False),[])
        self.assertTrue(all(a['review']['status']=='pending' for a in catalogue['assets']))
        self.assertNotIn('palette',catalogue['profiles']['native16']) # no D3 choice
        sample=self.stored[0]
        errors=validate_assets(catalogue,[(sample['id'],sample)],ROOT,True)
        self.assertTrue(any(e['code']=='approval_required' for e in errors))

    @superseded
    def test_actual_route_export_is_reproducible_and_production_mode_stays_current(self):
        self.assertEqual((OUT/'layoutD-routes.json').read_text(),serialized(build(actual_roads=self.t['roadRecords'],actual_distances=walking_routes(self.t,3.8))))
        self.assertEqual(json.loads((ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text())['mode'],'current')
        broken=copy.deepcopy(self.t['roadRecords']);broken[0]['cells'][1][0]+=10
        with self.assertRaises(ValueError):build(actual_roads=broken)

    def test_compiled_actual_distances_match_independent_path_times(self):
        config=json.loads((OUT/'layoutD-routes.json').read_text())
        # Independent graph built from saved compact-map road cells, not generator paths.
        cells=set()
        for m in self.stored:
            cells.update((m['terrainX']+i%m['w'],m['terrainY']+i//m['w']) for i,v in enumerate(m['road']) if v and not m['blocked'][i])
        graph={p:[] for p in cells}
        import math
        for x,y in cells:
            for dx,dy in EIGHT:
                q=x+dx,y+dy
                if q not in cells:continue
                if dx and dy and ((x+dx,y) not in cells or (x,y+dy) not in cells):continue
                graph[x,y].append((q,math.hypot(dx,dy)/config['walkingCellsPerSecond']))
        positions={BINDINGS[k]:v for k,v in self.t['towns'].items()}
        result=json.loads((OUT/'calibration.layoutD.json').read_text())
        self.assertEqual(len(result['routes']),81)
        for row in result['routes']:
            seconds,_=shortest_path(graph,positions[row['from']],positions[row['to']])
            self.assertAlmostEqual(seconds,row['seconds'],places=4)
            self.assertEqual(row['distance'],0 if row['from']==row['to'] else max(1,round(seconds*config['ratio'])))


if __name__=='__main__':unittest.main()
