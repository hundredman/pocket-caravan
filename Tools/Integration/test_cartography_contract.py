"""내보낸 지도와 실제 출구의 불일치·겹침을 Unity 없이 검출한다."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class CartographyContract(unittest.TestCase):
    def check_atlas(self, stem):
        """M4(2026-10-28): 맵은 구역을 감싸는 직사각형이라 서로 겹친다. 지도 자리는 세계 좌표(terrainX·Y)와 같은 차이로 놓이고,
        출구는 경계 칸에서 출구 방향으로 한 칸 건너간 칸이 대상 맵에서 걸을 수 있는 칸이어야 한다."""
        atlas = json.loads((ROOT / f'Assets/PocketCaravan/Resources/WorldMap/{stem}Layout.json').read_text())
        entries = {m['mapId']: m for m in atlas['maps']}
        maps = {}
        for path in (ROOT / 'Assets/PocketCaravan/World/Regions').glob('*.json'):
            for entry in json.loads(path.read_text())['maps']:
                maps[entry['mapId']] = json.loads((ROOT / entry['jsonPath']).read_text())
        self.assertTrue(set(entries) <= set(maps))
        offsets = set()
        for mid, a in entries.items():
            m = maps[mid]
            self.assertEqual((a['w'], a['h']), (m['w'], m['h']))
            self.assertTrue(0 <= a['x'] < a['x'] + a['w'] <= atlas['width'])
            self.assertTrue(0 <= a['y'] < a['y'] + a['h'] <= atlas['height'])
            offsets.add((m['terrainX'] - a['x'], m['terrainY'] - a['y']))
        self.assertEqual(len(offsets), 1, offsets)
        step = {'N': (0, -1), 'S': (0, 1), 'W': (-1, 0), 'E': (1, 0)}
        for mid in entries:
            a = maps[mid]
            for exit in a['objects']:
                if exit['kind'] != 'exit' or exit['target'] not in maps:
                    continue
                b = maps[exit['target']]
                self.assertTrue(any(o['kind'] == 'spawn' and o['id'] == exit['spawn'] for o in b['objects']), (mid, exit['spawn']))
                dx, dy = step[exit['edge']]
                for i in range(exit['w']):
                    for j in range(exit['h']):
                        wx, wy = a['terrainX'] + exit['x'] + i + dx, a['terrainY'] + exit['y'] + j + dy
                        bx, by = wx - b['terrainX'], wy - b['terrainY']
                        self.assertTrue(0 <= bx < b['w'] and 0 <= by < b['h'] and not b['blocked'][by * b['w'] + bx], (mid, exit['target'], wx, wy))
        return entries

    def test_world_atlas_matches_playable_exits(self):
        self.assertEqual(len(self.check_atlas('Goryeo')), 13)

    def test_goryeo_detail_has_only_connected_regional_maps(self):
        entries = self.check_atlas('GoryeoRegional')
        self.assertEqual(set(entries), {'Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan'})
        self.assertLess(entries['goryeo_songak']['y'], entries['Naru']['y'])
        self.assertGreater(entries['goryeo_jangdan']['y'], entries['Gaegyeong']['y'])


if __name__ == '__main__':
    unittest.main()
