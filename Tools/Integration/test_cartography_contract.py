"""내보낸 지도와 실제 출구의 불일치·겹침을 Unity 없이 검출한다."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class CartographyContract(unittest.TestCase):
    def check_atlas(self, stem):
        atlas = json.loads((ROOT / f'Assets/PocketCaravan/Resources/WorldMap/{stem}Layout.json').read_text())
        entries = {m['mapId']: m for m in atlas['maps']}
        maps = {}
        for path in (ROOT / 'Assets/PocketCaravan/World/Regions').glob('*.json'):
            for entry in json.loads(path.read_text())['maps']:
                if entry['mapId'] in entries:
                    maps[entry['mapId']] = json.loads((ROOT / entry['jsonPath']).read_text())
        self.assertEqual(set(entries), set(maps))
        for mid, a in entries.items():
            self.assertEqual((a['w'], a['h']), (maps[mid]['w'], maps[mid]['h']))
            self.assertTrue(0 <= a['x'] < a['x'] + a['w'] <= atlas['width'])
            self.assertTrue(0 <= a['y'] < a['y'] + a['h'] <= atlas['height'])
            for bid, b in entries.items():
                if mid >= bid:
                    continue
                overlap = max(a['x'], b['x']) < min(a['x']+a['w'], b['x']+b['w']) and max(a['y'], b['y']) < min(a['y']+a['h'], b['y']+b['h'])
                self.assertFalse(overlap, (mid, bid))
            for exit in maps[mid]['objects']:
                if exit['kind'] != 'exit' or exit['target'] not in entries:
                    continue
                b = entries[exit['target']]
                spawn = next(o for o in maps[exit['target']]['objects'] if o['kind'] == 'spawn' and o['id'] == exit['spawn'])
                edge = exit['edge']
                if edge in ('E', 'W'):
                    self.assertEqual(b['x'], a['x']+a['w'] if edge == 'E' else a['x']-b['w'])
                    self.assertEqual(b['y']+spawn['y'], a['y']+exit['y'])
                else:
                    self.assertEqual(b['y'], a['y']-b['h'] if edge == 'N' else a['y']+a['h'])
                    self.assertEqual(b['x']+spawn['x'], a['x']+exit['x'])
        return entries

    def test_world_atlas_matches_playable_exits(self):
        self.assertEqual(len(self.check_atlas('Goryeo')), 11)

    def test_goryeo_detail_has_only_connected_regional_maps(self):
        entries = self.check_atlas('GoryeoRegional')
        self.assertEqual(set(entries), {'Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan'})
        self.assertLess(entries['goryeo_songak']['y'], entries['Naru']['y'])
        self.assertGreater(entries['goryeo_jangdan']['y'], entries['Gaegyeong']['y'])


if __name__ == '__main__':
    unittest.main()
