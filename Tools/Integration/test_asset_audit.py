"""Audit evidence must be lossless, reproducible and never classify art automatically."""
import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/Audit'))
sys.path.insert(0, str(ROOT / 'Tools/MapGen'))
from audit_assets import scan_python, raster_runs, adjacent_rectangles, Observe
from occlusion_baseline import restore_before_c3


class AssetAuditChecks(unittest.TestCase):
    def test_source_location_preserves_expression_without_art_verdict(self):
        source = 'def render(im, cell):\n    return im.crop((cell*16, 0, cell*16+16, 16))\n'
        row, = scan_python(source, 'sample.py')
        self.assertEqual((row['file'], row['line'], row['function']), ('sample.py', 2, 'render'))
        self.assertEqual(row['coordinates'], ['(cell * 16, 0, cell * 16 + 16, 16)'])
        self.assertEqual(row['expression'], 'im.crop((cell*16, 0, cell*16+16, 16))')
        self.assertEqual(row['review'], 'candidate_only')
        self.assertEqual(scan_python('crop_label = 3', 'sample.py'), [])

    def test_runs_preserve_sparse_coordinates_and_do_not_bridge_holes(self):
        points = [(4, 3), (1, 3), (2, 3), (2, 3), (0, 7), (-1, 7)]
        runs = raster_runs(points)
        self.assertEqual(runs, [[3, 1, 2], [3, 4, 4], [7, -1, 0]])
        restored = {(x, y) for y, lo, hi in runs for x in range(lo, hi + 1)}
        self.assertEqual(restored, set(points))
        self.assertEqual(raster_runs([]), [])

    def test_adjacency_includes_contact_but_excludes_other_maps_and_assets(self):
        rows = [dict(map=m, name=n, rect=r) for m, n, r in [
            ('a', 'tree', [0, 0, 2, 2]), ('a', 'tree', [2, 0, 2, 2]),
            ('a', 'tree', [5, 0, 2, 2]), ('b', 'tree', [0, 0, 2, 2]),
            ('a', 'house', [0, 0, 2, 2])]]
        pair, = adjacent_rectangles(rows)
        self.assertEqual(pair['first'], [0, 0, 2, 2])
        self.assertEqual(pair['second'], [2, 0, 2, 2])

    def test_observer_restores_image_and_game_functions_after_failure(self):
        from PIL import Image
        import tiles, goryeo_art
        before = (Image.Image.crop, Image.Image.resize, tiles.recolor, goryeo_art.tone)
        with self.assertRaisesRegex(RuntimeError, 'probe'):
            with Observe(ROOT):
                self.assertIsNot(Image.Image.crop, before[0])
                raise RuntimeError('probe')
        self.assertEqual(before, (Image.Image.crop, Image.Image.resize, tiles.recolor, goryeo_art.tone))

    def test_report_positions_match_committed_maps_exactly(self):
        data = json.loads((ROOT / 'Docs/Validation/A3a-2026-10-25/inventory.json').read_text())['runtime']
        maps = {json.loads(p.read_text())['id']: json.loads(p.read_text())
                for p in (ROOT / 'Assets/PocketCaravan/Maps').glob('*.json')}
        reported = {}
        for item in data['tileUsage']:
            export_name = Path(item['exportImage']).stem
            self.assertTrue((ROOT / item['exportImage']).is_file(), item['exportImage'])
            for usage in item['uses']:
                if usage['layer'] == 'growth':
                    continue
                key = (usage['map'], usage['layer'], export_name)
                coords = {(x, y) for y, lo, hi in usage['rowRuns'] for x in range(lo, hi + 1)}
                self.assertEqual(len(coords), usage['count'])
                reported[key] = coords
        expected = {}
        for mid in data['registeredMaps']:
            m = restore_before_c3(maps[mid])
            for layer in ('ground', 'deco', 'obj', 'high'):
                for index, pi in enumerate(m[layer]):
                    if pi:
                        expected.setdefault((mid, layer, m['palette'][pi]), set()).add((index % m['w'], index // m['w']))
        self.assertEqual(reported, expected)
        self.assertFalse(set(data['registeredMaps']) & {m['id'] for m in data['unregisteredMaps']})

    def test_observed_rectangles_have_source_and_trigger_key(self):
        data = json.loads((ROOT / 'Docs/Validation/A3a-2026-10-25/inventory.json').read_text())['runtime']
        keys = {row['key'] for row in data['tileUsage']}
        self.assertTrue(data['operations'])
        for op in data['operations']:
            self.assertTrue((ROOT / op['file']).is_file())
            self.assertGreater(op['line'], 0)
            self.assertIn(op['exportKey'], keys | {None})
        self.assertTrue(any(op['operation'] == 'crop' and op['parameters'].get('arguments') for op in data['operations']))


if __name__ == '__main__':
    unittest.main()
