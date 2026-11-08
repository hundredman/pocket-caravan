"""Daily trial anchors must refer to existing art without changing generated maps."""
import json
import hashlib
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
import sys
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from t3_history import map_at,resource_at


class DailyWellReview(unittest.TestCase):
    def test_production_policy_is_approved_source(self):
        source = ROOT / 'ArtReview/Goryeo-Daily-20261106/approved-source.json'
        approval = json.loads((source.parent / 'approval.json').read_text())
        bound = ROOT / 'Assets/PocketCaravan/Resources/Goryeo/Daily/Config.json'
        self.assertEqual(approval['status'], 'approved')
        self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(), approval['sourceSha256'])
        self.assertEqual(json.loads(source.read_text()), resource_at(bound))

    def test_well_and_approach_match_layout_source(self):
        config = json.loads((ROOT / 'ArtReview/Goryeo-Daily-20261106/source.json').read_text())
        world = json.loads((ROOT / 'ArtReview/M2-Zones/zones.json').read_text())
        village = world['villages']['goryeo/나루']
        well = next(item for item in village['items'] if item['asset'] == 'well')
        data = map_at(ROOT / 'Assets/PocketCaravan/Maps/Naru.json')
        x, y = config['x'], config['y']
        self.assertEqual((well['x'] - data['terrainX'], well['y'] - data['terrainY'] + well['h'] - 1), (x, y))
        self.assertEqual((config['width'], config['height']), (data['w'], data['h']))
        self.assertNotEqual(data['obj'][y * data['w'] + x], 0)
        self.assertEqual(data['blocked'][y * data['w'] + x], 1)
        self.assertEqual(data['blocked'][(y + 1) * data['w'] + x], 0)
        self.assertFalse(any(o['x'] == x and o['y'] == y + 1 for o in data['objects']))


if __name__ == '__main__':
    unittest.main()
