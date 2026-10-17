import json, sys, unittest
from pathlib import Path
from unittest.mock import patch
from obstacle_validation import validate_placements
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'Tools/MapGen/Modules'))
from compose import compose
BASE = ROOT/'Assets/PocketCaravan/Obstacles'

class ObstacleContract(unittest.TestCase):
    def setUp(self):
        self.defs = json.loads((BASE/'definitions.json').read_text())['definitions']
        self.index = {d['id']: d for d in self.defs}

    def test_presets_and_imported_assets(self):
        self.assertEqual(len(self.defs), 48)
        self.assertEqual(len(self.index), 48)
        modes = {'Barrier','Slow','Threat','TimedBarrier','TimedSlow','Toll','VehicleGate','LoadGate'}
        for d in self.defs:
            self.assertIn(d['mode'], modes)
            self.assertIn(d['action'], {'None','Clear','Repair','Pay'})
            self.assertTrue((BASE/'Art'/(d['visual']+'.png')).is_file())
            asset = (BASE/'Definitions'/(d['id']+'.asset')).read_text()
            self.assertIn('  id: '+d['id']+'\n', asset)
            self.assertGreater(d['width'], 0); self.assertGreater(d['height'], 0)
            for k in ('walkSpeed','cartSpeed','traction','visibility'):
                self.assertGreaterEqual(d[k], .05); self.assertLessEqual(d[k], 1)
            for k in ('money','itemCount','workSeconds','staminaDrain','threatSpeed'):
                self.assertGreaterEqual(d[k], 0)
            if d['itemCount']:
                self.assertEqual(d['itemId'], 'firewood')
            if d['mode'].startswith('Timed'):
                self.assertGreater(d['activeSeconds'], 0); self.assertGreater(d['openSeconds'], 0)

    def test_matching_monobehaviour_script_identity(self):
        meta = (ROOT/'Assets/PocketCaravan/Scripts/Obstacles/ObstacleLibrary.cs.meta').read_text()
        guid = next(l.split(': ')[1] for l in meta.splitlines() if l.startswith('guid:'))
        self.assertIn('guid: '+guid, (ROOT/'Assets/PocketCaravan/Resources/Obstacles/ObstacleLibrary.asset').read_text())

    def test_bad_placements_rejected(self):
        good = dict(id='trial', definitionId='fallen_log', x=18, y=18)
        self.assertEqual(validate_placements(dict(w=40,h=40,obstacles=[good]),self.index), [])
        for edits in [dict(definitionId='missing'),dict(x=39),dict(width=-1),dict(x=1.5),dict(enabled='false'),dict(phaseOffset=float('nan'))]:
            self.assertTrue(validate_placements(dict(w=40,h=40,obstacles=[dict(good,**edits)]),self.index))
        self.assertTrue(validate_placements(dict(w=40,h=40,obstacles=[good,good]),self.index))

    def test_composed_placements_keep_coordinates_and_unique_ids(self):
        original = Path.read_text
        def with_hazard(path, *args, **kwargs):
            text = original(path, *args, **kwargs)
            if path.parent.name == 'Layouts' and path.stem == 'meadow_lane':
                d = json.loads(text); d['obstacles'] = [dict(id='same',definitionId='mud',x=18,y=18)]
                return json.dumps(d)
            return text
        with patch.object(Path,'read_text',with_hazard):
            f = compose(dict(id='test',name='test',cells=[dict(x=0,y=0,module='meadow_lane'),dict(x=1,y=0,module='meadow_lane')]))
        self.assertEqual([p['x'] for p in f['obstacles']], [18,58])
        self.assertEqual([p['y'] for p in f['obstacles']], [18,18])
        self.assertEqual(len({p['id'] for p in f['obstacles']}), 2)
        self.assertEqual(validate_placements(f,self.index), [])

if __name__ == '__main__': unittest.main()
