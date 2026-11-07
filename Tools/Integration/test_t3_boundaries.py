"""Review exporter: native world preservation, real exterior ports, safe arrivals."""
import sys,unittest,json
from pathlib import Path
from collections import deque
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
try:
    import numpy as np
    from scipy import ndimage
    import t3_boundaries as t3
except ModuleNotFoundError as error:
    if error.name not in ('numpy','scipy'):raise
    t3=None

@unittest.skipIf(t3 is None,'T3 authoring checks need optional NumPy/SciPy; team CI installs Pillow only')
class BoundaryReview(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.config,cls.sources=t3.read_sources()
        cls.maps,cls.report,cls.tiles,cls.owners,cls.known=t3.generate(cls.config,cls.sources)
    def test_all_13_ids_preserved_and_every_output_is_preview_only(self):
        self.assertEqual(set(self.maps),set(self.sources));self.assertEqual(len(self.maps),13)
        self.assertTrue(all(d['previewOnly']for d in self.maps.values()))
    def test_all_exits_are_on_the_actual_outward_face(self):
        count=0
        for mid,d in self.maps.items():
            for o in d['objects']:
                if o['kind']!='exit':continue
                count+=1;edge=o['edge']
                inset={'W':o['x'],'E':d['w']-o['x']-o['w'],'N':o['y'],'S':d['h']-o['y']-o['h']}[edge]
                self.assertEqual(inset,0,(mid,o))
        self.assertGreater(count,13)
    def test_no_two_destinations_share_the_same_outward_cell(self):
        for mid,d in self.maps.items():
            seen={}
            for e in d['objects']:
                if e['kind']!='exit':continue
                for y in range(e['y'],e['y']+e['h']):
                    for x in range(e['x'],e['x']+e['w']):
                        key=(x,y,e['edge'])
                        self.assertNotIn(key,seen,(mid,key,e['target'],seen.get(key)))
                        seen[key]=e['target']
    def test_paired_arrivals_fit_are_walkable_and_not_exit_cells(self):
        for mid,d in self.maps.items():
            for e in d['objects']:
                if e['kind']!='exit':continue
                target=self.maps.get(e['target'])
                if target is None:target=json.loads(next((ROOT/'Assets/PocketCaravan/Maps').rglob(e['target']+'.json')).read_text())
                spawns=[o for o in target['objects']if o['kind']=='spawn'and o['id']==e['spawn']]
                self.assertEqual(len(spawns),1);spawn=spawns[0];self.assertEqual((spawn['w'],spawn['h']),(e['w'],e['h']))
                exits={(o['x']+dx,o['y']+dy)for o in target['objects']if o['kind']=='exit'for dx in range(o['w'])for dy in range(o['h'])}
                for y in range(spawn['y'],spawn['y']+spawn['h']):
                    for x in range(spawn['x'],spawn['x']+spawn['w']):
                        self.assertTrue(0<=x<target['w']and 0<=y<target['h']);self.assertFalse(target['blocked'][y*target['w']+x]);self.assertNotIn((x,y),exits)
    def test_connected_13_map_road_network_and_return_exits(self):
        graph={mid:{o['target']for o in d['objects']if o['kind']=='exit'and o['target']in self.maps}for mid,d in self.maps.items()}
        seen={'Naru'};queue=deque(seen)
        while queue:
            for target in graph[queue.popleft()]:
                if target not in seen:seen.add(target);queue.append(target)
        self.assertEqual(seen,set(self.maps))
        for mid,targets in graph.items():
            for target in targets:self.assertIn(mid,graph[target])
    def test_default_starts_and_functional_content_share_walk_component(self):
        for mid,d in self.maps.items():
            labels,_=ndimage.label(np.array(d['blocked']).reshape(d['h'],d['w'])==0)
            spawn=next(o for o in d['objects']if o['kind']=='spawn'and o['id']=='S0');component=labels[spawn['y'],spawn['x']];self.assertGreater(component,0)
            for o in d['objects']:
                if o['kind']in('market','board','npc','talker','worker','pastime','workshop','exit'):
                    self.assertEqual(labels[o['y'],o['x']],component,(mid,o['kind'],o.get('name')))
    def test_original_function_ids_and_world_coordinates_preserved(self):
        def functions(d):
            return sorted((o['kind'],o.get('npc')or o.get('town')or o.get('id')or o.get('name')or'',d['terrainX']+o['x'],d['terrainY']+o['y'])for o in d['objects']if o['kind']not in t3.MOVABLE|{'exit','spawn'})
        for mid,d in self.maps.items():self.assertEqual(functions(d),functions(self.sources[mid]))
    def test_native_world_preserved_except_four_explicit_west_tip_cells(self):
        restored={layer:np.full(self.known.shape,'',object)for layer in t3.LAYERS};walk=np.zeros(self.known.shape,bool)
        for d in self.maps.values():
            x,y,w,h=d['terrainX'],d['terrainY']+130,d['w'],d['h'];region=np.s_[y:y+h,x:x+w]
            for layer in t3.LAYERS:restored[layer][region]=np.array(d['palette'],object)[np.array(d[layer]).reshape(h,w)]
            walk[region]|=np.array(d['blocked']).reshape(h,w)==0
        self.assertEqual(self.report['trimmedReviewCells'],[[2,-2],[2,-1],[2,0],[2,1]])
        for layer in t3.LAYERS:
            expected=self.tiles[layer].copy()
            for x,y in self.report['trimmedReviewCells']:expected[y+130,x]=''
            np.testing.assert_array_equal(restored[layer],expected)
        self.assertEqual(int(walk.sum()),sum(sum(not b for b in d['blocked'])for d in self.sources.values())-4)
    def test_existing_three_foreign_links_and_incoming_ids_are_preserved(self):
        baseline=self.report['foreignBaseline'];self.assertEqual(len(baseline),3)
        expected={'north_c_road_se','arabia_central_eastvale','arabia_central_steppe'}
        ports=self.report['externalReviewPorts'];self.assertEqual({e['target']for e in ports},expected)
        d=self.maps['transit_crossroads']
        for p in ports:
            old=next(o for o in self.sources['transit_crossroads']['objects']if o['kind']=='spawn'and o['id']==p['incomingSpawn'])
            new=next(o for o in d['objects']if o['kind']=='spawn'and o['id']==p['incomingSpawn'])
            self.assertEqual((old['w'],old['h']),(new['w'],new['h']))

if __name__=='__main__':unittest.main()
