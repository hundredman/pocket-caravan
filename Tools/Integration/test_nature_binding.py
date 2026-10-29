"""Whole-source nature integration: approval gate, ports, masks and current-map safety."""
import copy,json,sys,tempfile,unittest
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import m4_nature,m4_certify
PREVIEW=ROOT/'Assets/PocketCaravan/Editor/B2Preview/A7'

class NatureSources(unittest.TestCase):
    def test_pending_layout_cannot_enter_production(self):
        with self.assertRaisesRegex(ValueError,'approval'):m4_nature.load_layout(production=True)

    def test_approved_images_masks_and_active_ports(self):
        config,assets=m4_nature.load_layout()
        for p in config['placements']:
            if p.get('enabled',True):self.assertIn('mask',assets[p['asset']])

    def test_disconnected_port_is_rejected(self):
        config=json.loads(m4_nature.LAYOUT.read_text());config['placements'][1]['x']+=1
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/'layout.json';p.write_text(json.dumps(config))
            with self.assertRaisesRegex(ValueError,'Disconnected'):m4_nature.load_layout(p)

    def test_invalid_purpose_is_rejected(self):
        config=json.loads(m4_nature.LAYOUT.read_text());config['placements'][0]['role']='stretched_wall'
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/'layout.json';p.write_text(json.dumps(config))
            with self.assertRaisesRegex(ValueError,'placement'):m4_nature.load_layout(p)

class NaturePreview(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not (PREVIEW/'m4-composites.json').exists():raise unittest.SkipTest('Generate the isolated A7 preview before preview tests')
        cls.maps={p.stem:json.loads(p.read_text()) for p in (PREVIEW/'Maps').glob('*.json')}

    def test_all_composites_match_approved_whole_sources(self):
        errors,info=m4_certify.verify(PREVIEW/'m4-composites.json',PREVIEW/'Tiles')
        self.assertEqual(errors,[]);self.assertGreater(info['tiles'],1000)

    def test_thirteen_preview_maps_protect_saves_and_roads(self):
        self.assertEqual(len(self.maps),13)
        for mid,d in self.maps.items():
            before=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            self.assertTrue(d['previewOnly']);self.assertEqual(before['road'],d['road'],mid)
            for key in ['id','w','h','terrainX','terrainY']:self.assertEqual(before[key],d[key],(mid,key))
            for obj in before['objects']:
                if obj['kind']=='exit':self.assertTrue(any(o['kind']=='exit' and o['target']==obj['target'] and o['spawn']==obj['spawn'] for o in d['objects']),(mid,obj['spawn']))
                elif obj['kind']=='spawn':self.assertTrue(any(o['kind']=='spawn' and o['id']==obj['id'] for o in d['objects']),(mid,obj['id']))
                else:self.assertIn(obj,d['objects'],(mid,obj['kind'],obj.get('id')))

    def test_every_arrival_is_walkable_and_outside_exits(self):
        for mid,d in self.maps.items():
            w=d['w'];exits={(o['x']+dx,o['y']+dy) for o in d['objects'] if o['kind']=='exit' for dy in range(o.get('h',1)) for dx in range(o.get('w',1))}
            for o in d['objects']:
                if o['kind']!='spawn':continue
                for dy in range(o.get('h',1)):
                    for dx in range(o.get('w',1)):
                        x,y=o['x']+dx,o['y']+dy
                        self.assertEqual(d['blocked'][y*w+x],0,(mid,o['id']));self.assertNotIn((x,y),exits,(mid,o['id']))

    def test_rice_uses_full_footprint_without_new_barrier(self):
        d=self.maps['goryeo_jangdan'];before=json.loads((ROOT/'Assets/PocketCaravan/Maps/goryeo_jangdan.json').read_text())
        self.assertEqual(before['blocked'],d['blocked'])
        self.assertEqual(len(d['natureCrops']),2)
        for p in d['natureCrops']:self.assertEqual((p['w'],p['h']),(12,8))

    def test_only_listed_waterfall_entrances_move_locally(self):
        changes=json.loads((ROOT/'ArtReview/NatureBinding-20261029/Maps/nature-exits.json').read_text())['relocations']
        self.assertEqual(len(changes),6)
        exits={(c['map'],c['target'],c['spawnId']) for c in changes}
        arrivals={(c['target'],c['spawnId']) for c in changes}
        for c in changes:
            for role in ['exit','arrival']:
                self.assertLessEqual(sum(abs(a-b) for a,b in zip(c['old'][role],c['new'][role])),12)
        for mid,d in self.maps.items():
            before=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            for obj in before['objects']:
                if obj['kind']=='exit' and (mid,obj['target'],obj['spawn']) in exits:continue
                if obj['kind']=='spawn' and (mid,obj['id']) in arrivals:continue
                self.assertIn(obj,d['objects'],(mid,obj))

if __name__=='__main__':unittest.main()
