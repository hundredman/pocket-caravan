"""Approved B1 footprint must survive serialization without rectangle padding or map loss."""
import copy,json,math,sys,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from world_layout import WorldLayout,decode_runs,DEFAULT

class WorldLayoutSourceChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):cls.layout=WorldLayout.read();cls.data=cls.layout.data

    def test_exact_corrected_approved_areas_and_all_assigned_cells(self):
        report=self.layout.report()
        self.assertEqual(report['mapsByArea'],{'개경':5001,'장단':4298,'나루':3997,'닥목':3402,'송악':3302,'동문 마을':3200,'북동 포구':3200,'남동 포구':3200,'중앙 길목 마을':3200,'북동 길':3050,'남동 길':3051,'북쪽 내륙길':3047,'남쪽 내륙길':3052})
        self.assertEqual(report['regions'],{'goryeo':20000,'east':25000})
        self.assertEqual((report['assignedCells'],report['landCells'],report['nearSeaCells']),(45000,38253,6747))

    def test_every_owned_cell_roundtrips_through_local_coordinates(self):
        for key,cells in self.layout.map_cells.items():
            bx,by,_,_=self.layout.maps[key]['bounds']
            for x,y in cells:
                self.assertEqual(self.layout.owner_at(x+.5,y+.5),key)
                lx,ly=self.layout.local_cell(key,x+.5,y+.5)
                self.assertEqual((lx+bx,ly+by),(x,y))
        padded=sum((m['bounds'][2]-m['bounds'][0])*(m['bounds'][3]-m['bounds'][1]) for m in self.data['maps'])
        self.assertGreater(padded,45000) # bounding-box padding is not owned land/sea
        self.assertIsNone(self.layout.owner_at(-1000,-1000))

    def test_nine_town_diagram_connected_main_quest_and_pending_external_links(self):
        self.assertEqual(len(self.layout.towns),9);self.assertEqual(len(self.data['roads']),12)
        for start in self.layout.towns:
            seen=set();todo=[start]
            while todo:
                n=todo.pop()
                if n in seen:continue
                seen.add(n);todo+=list(self.layout.roads[n]-seen)
            self.assertEqual(seen,set(self.layout.towns))
        self.assertEqual(self.data['mainQuest'],['goryeo/나루','goryeo/장단','goryeo/송악','goryeo/닥목','goryeo/개경'])
        self.assertTrue(all(l['runtimeBinding'] is None for l in self.data['externalLinks']))
        self.assertFalse(any('goryeo/개경' in pair and any(p.startswith('east/') for p in pair) for pair in self.layout.shared_edges))

    def test_corrupt_missing_cells_or_duplicate_ownership_is_rejected(self):
        d=copy.deepcopy(self.data);d['maps'][0]['runs'].pop()
        with self.assertRaises(ValueError):WorldLayout(d)
        d=copy.deepcopy(self.data);m=copy.deepcopy(d['maps'][0]);m['key']='goryeo/invalid_duplicate';d['maps'].append(m)
        with self.assertRaisesRegex(ValueError,'ownership overlaps'):WorldLayout(d)

    def test_town_in_sea_or_missing_road_node_is_rejected(self):
        d=copy.deepcopy(self.data);d['towns'][0]['position']=[-1000,-1000]
        with self.assertRaisesRegex(ValueError,'own land'):WorldLayout(d)
        d=copy.deepcopy(self.data);d['roads'][0]['toKey']='missing'
        with self.assertRaisesRegex(ValueError,'endpoint missing'):WorldLayout(d)

    def test_river_waypoints_preserved_but_no_continuous_flow_claim(self):
        self.assertEqual(len(self.data['rivers']),3)
        for river in self.data['rivers']:
            points=[tuple(math.floor(v) for v in p) for p in river['waypoints']]
            self.assertTrue(all(p in self.layout.land for p in points[:-1]))
            self.assertNotIn(points[-1],self.layout.land)
            self.assertIn('diagram',river['geometry'])

    def test_invalid_runs_rejected_and_source_load_is_path_explicit(self):
        for runs in ([[0,2,1]],[[0,False,2]],[[0,0.5,2]],[[0,0,1],[0,1,2]]):
            with self.assertRaises(ValueError):decode_runs(runs)
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'source.json';p.write_text(json.dumps(self.data))
            self.assertEqual(WorldLayout.read(p).report(),self.layout.report())

if __name__=='__main__':unittest.main()
