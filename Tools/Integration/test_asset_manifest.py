"""A2 negative and roundtrip cases. Temporary art is synthetic, never a style approval."""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/MapGen'))
from asset_manifest import validate, validate_catalogue


class AssetManifestChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        # 2px per cell is test input, not a proposed D2 resolution.
        image = Image.new('RGBA', (4, 2), '#339944')
        image.save(self.root / 'tree.png')
        image.crop((0, 0, 2, 2)).save(self.root / 'left.png')
        image.crop((2, 0, 4, 2)).save(self.root / 'right.png')
        (self.root / 'review.txt').write_text('SYNTHETIC TEST EVIDENCE ONLY')
        self.asset = dict(id='test_tree', kind='object', purpose='tree', size=[2, 1],
                          image='tree.png', source=dict(kind='authored', path='tree.png'),
                          review=dict(status='approved', evidence='review.txt'),
                          rules=dict(onTerrain=['grass'], forbidRoad=True),
                          tiles=[dict(dx=0, dy=0, layer='obj', tile='tree_left', image='left.png'),
                                 dict(dx=1, dy=0, layer='high', tile='tree_right', image='right.png')])
        self.doc = dict(schemaVersion=1, profiles=dict(test=dict(pixelsPerCell=[2, 2], palette=['#339944'])),
                        assets=[self.asset])
        self.asset['profile'] = 'test'
        self.map = dict(assetPlacementVersion=1, w=3, h=2, palette=['', 'tree_left', 'tree_right'],
                        ground=[0]*6, deco=[0]*6, obj=[1, 0, 0, 0, 0, 0], high=[0, 2, 0, 0, 0, 0],
                        road=[0]*6, terrainTags=['grass']*6,
                        assetPlacements=[dict(id='tree_1', assetId='test_tree', purpose='tree', x=0, y=0)])

    def errors(self, doc=None, map_data=None, preview=False):
        return validate(self.doc if doc is None else doc,
                        [('test_map', self.map if map_data is None else map_data)], self.root, not preview)

    def codes(self, **kwargs):
        return {e['code'] for e in self.errors(**kwargs)}

    def test_valid_split_object_roundtrip_and_no_mutation(self):
        before = copy.deepcopy((self.doc, self.map))
        self.assertEqual(self.errors(), [])
        encoded = json.loads(json.dumps(self.map))
        self.assertEqual(self.errors(map_data=encoded), [])
        self.assertEqual((self.doc, self.map), before)

    def test_unknown_asset_and_unknown_rendered_tile(self):
        self.map['assetPlacements'][0]['assetId'] = 'missing'
        self.assertIn('unregistered_asset', self.codes())
        self.map['palette'][1] = 'not_registered'
        self.assertIn('unregistered_tile', self.codes())

    def test_registered_tile_without_placement_is_rejected(self):
        self.map['assetPlacements'] = []
        self.assertIn('unrecorded_tile', self.codes())

    def test_clipped_overwritten_and_swapped_cells(self):
        for change in ('missing', 'swapped'):
            with self.subTest(change=change):
                m = copy.deepcopy(self.map)
                m['high'][1] = 0 if change == 'missing' else 1
                self.assertIn('incomplete_placement', self.codes(map_data=m))

    def test_boundary_cannot_crop_whole_object(self):
        self.map['assetPlacements'][0]['x'] = 2
        self.assertIn('placement_bounds', self.codes())

    def test_partial_definition_is_rejected(self):
        self.asset['tiles'].pop()
        self.assertIn('incomplete_definition', self.codes())

    def test_wrong_purpose_and_terrain(self):
        self.map['assetPlacements'][0]['purpose'] = 'field'
        self.map['terrainTags'][0] = 'water'
        self.assertTrue({'wrong_purpose', 'wrong_terrain'} <= self.codes())

    def test_road_and_missing_context(self):
        self.map['road'][0] = 2
        self.assertIn('road_forbidden', self.codes())
        del self.map['road']
        del self.map['terrainTags']
        self.assertTrue({'missing_road_context', 'missing_terrain_context'} <= self.codes())

    def test_declared_water_adjacency_uses_external_neighbors(self):
        self.asset['rules']['adjacentTo'] = ['water']
        self.assertIn('wrong_adjacency', self.codes())
        self.map['terrainTags'][3] = 'water'
        self.assertEqual(self.errors(), [])

    def test_overlap_and_duplicate_instance_id(self):
        self.map['assetPlacements'].append(copy.deepcopy(self.map['assetPlacements'][0]))
        self.assertTrue({'placement_overlap', 'placement_id'} <= self.codes())

    def test_inline_transforms_are_rejected(self):
        for field in ('scale', 'crop', 'rotation', 'tint'):
            with self.subTest(field=field):
                m = copy.deepcopy(self.map)
                m['assetPlacements'][0][field] = 2
                self.assertIn('placement_transform', self.codes(map_data=m))

    def test_unapproved_art_requires_explicit_preview(self):
        self.asset['review'] = dict(status='pending')
        self.assertIn('approval_required', self.codes())
        self.assertEqual(self.errors(preview=True), [])

    def test_approval_requires_evidence_and_source_requires_license(self):
        self.asset['review']['evidence'] = 'missing.txt'
        self.assertIn('asset_file', self.codes())
        self.asset['source']['kind'] = 'external'
        self.assertIn('source_license', self.codes())

    def test_modular_set_missing_piece_and_complete_piece(self):
        base = copy.deepcopy(self.asset)
        definition = {key: base[key] for key in ('size', 'tiles', 'image')}
        for key in ('size', 'tiles', 'image'):
            self.asset.pop(key)
        self.asset.update(kind='modular', moduleType='wall', requiredPieces=['center', 'edge', 'outer_corner', 'inner_corner', 'end'],
                          pieces={name: copy.deepcopy(definition) for name in ('center', 'edge', 'outer_corner', 'inner_corner', 'straight')})
        self.assertIn('missing_piece', self.codes())
        self.asset['pieces']['end'] = copy.deepcopy(definition)
        self.map['assetPlacements'][0]['piece'] = 'straight'
        self.assertEqual(self.errors(), [])
        self.map['assetPlacements'][0]['piece'] = 'missing'
        self.assertIn('unknown_piece', self.codes())

    def two_objects(self, x, y=0):
        m = copy.deepcopy(self.map)
        m.update(w=7, h=4, road=[0]*28, terrainTags=['grass']*28)
        for layer in ('ground', 'deco', 'obj', 'high'):
            m[layer] = [0]*28
        m['assetPlacements'].append(dict(id='tree_2', assetId='test_tree', purpose='tree', x=x, y=y))
        for p in m['assetPlacements']:
            m['obj'][p['y']*7+p['x']] = 1
            m['high'][p['y']*7+p['x']+1] = 2
        return m

    def test_same_object_requires_requested_empty_gap(self):
        self.asset['rules']['separateFromSame'] = 1
        self.assertIn('same_asset_spacing', self.codes(map_data=self.two_objects(2)))
        self.assertEqual(self.errors(map_data=self.two_objects(3)), [])
        self.asset['rules']['separateFromSame'] = 2
        self.assertIn('same_asset_spacing', self.codes(map_data=self.two_objects(3)))
        self.assertEqual(self.errors(map_data=self.two_objects(4)), [])

    def test_corner_contact_and_vertical_contact_are_rejected(self):
        self.asset['rules']['separateFromSame'] = 1
        for x, y in ((2, 1), (0, 1)):
            with self.subTest(x=x, y=y):
                self.assertIn('same_asset_spacing', self.codes(map_data=self.two_objects(x, y)))

    def test_objects_without_separation_rule_can_cluster(self):
        self.assertEqual(self.errors(map_data=self.two_objects(2)), [])
        self.asset['rules']['separateFromSame'] = 0
        self.assertEqual(self.errors(map_data=self.two_objects(2)), [])

    def test_spacing_rule_is_per_asset_not_global(self):
        self.asset['rules']['separateFromSame'] = 3
        other = copy.deepcopy(self.asset); other['id'] = 'other_tree'
        other['tiles'][0]['tile'] = 'other_left'; other['tiles'][1]['tile'] = 'other_right'
        self.doc['assets'].append(other)
        m = self.two_objects(2); m['palette'] += ['other_left', 'other_right']
        m['assetPlacements'][1]['assetId'] = 'other_tree'; m['obj'][2] = 3; m['high'][3] = 4
        self.assertEqual(self.errors(map_data=m), [])

    def test_invalid_spacing_rule_cannot_be_ignored(self):
        for value in (-1, True, '1', 0.5):
            with self.subTest(value=value):
                self.asset['rules']['separateFromSame'] = value
                self.assertIn('asset_rules', self.codes())

    def test_all_modular_types_have_minimum_piece_roles(self):
        definition = {key: copy.deepcopy(self.asset[key]) for key in ('size', 'tiles', 'image')}
        for key in ('size', 'tiles', 'image'):
            self.asset.pop(key)
        self.map['assetPlacements'][0]['piece'] = 'center'
        for module_type in ('water', 'coast', 'river', 'road', 'wall', 'field', 'ground_boundary', 'cliff', 'bridge'):
            with self.subTest(module_type=module_type):
                names = ['start', 'center', 'end'] if module_type == 'bridge' else ['center', 'edge', 'outer_corner', 'inner_corner']
                if module_type in ('river', 'road', 'wall'): names += ['end']
                self.asset.update(kind='modular', moduleType=module_type, requiredPieces=names[:], pieces={n: copy.deepcopy(definition) for n in names})
                self.assertEqual(self.errors(), [])
                self.asset['requiredPieces'].remove('start' if module_type == 'bridge' else 'inner_corner')
                self.assertIn('modular_minimum', self.codes())
                self.asset['requiredPieces'] = names[:]
                if 'end' in names:
                    self.asset['requiredPieces'].remove('end')
                    self.assertIn('modular_minimum', self.codes())
                self.asset['requiredPieces'] = names[:]
                self.asset['pieces'].pop('start' if module_type == 'bridge' else 'edge')
                self.assertIn('missing_piece', self.codes())

    def test_duplicate_asset_and_tile_ownership(self):
        self.doc['assets'].append(copy.deepcopy(self.asset))
        self.assertIn('asset_id', self.codes())
        self.doc['assets'][1]['id'] = 'another_tree'
        self.assertIn('duplicate_tile_owner', self.codes())

    def test_wrong_source_tile_pixels(self):
        Image.new('RGBA', (2, 2), '#339945').save(self.root / 'right.png')
        self.assertIn('tile_pixels', self.codes())

    def test_pixel_profile_size_palette_alpha(self):
        self.doc['profiles']['test']['pixelsPerCell'] = [3, 3]
        self.assertIn('image_size', self.codes())
        self.doc['profiles']['test']['palette'] = ['#000000']
        self.assertIn('image_palette', self.codes())
        Image.new('RGBA', (4, 2), (51, 153, 68, 128)).save(self.root / 'tree.png')
        self.assertIn('image_alpha', self.codes())

    def test_unconfigured_profile_does_not_choose_resolution_or_colors(self):
        del self.asset['profile']
        self.doc['profiles'] = {}
        self.assertEqual(self.errors(), [])

    def test_outside_root_paths_and_missing_files(self):
        for path in ('../outside.png', '/outside.png', 'missing.png'):
            with self.subTest(path=path):
                self.asset['image'] = path
                self.assertIn('asset_file', self.codes())

    def test_malformed_input_is_not_silently_accepted(self):
        for m in (None, {}, {'assetPlacementVersion': True}, dict(self.map, obj=[99]*6), dict(self.map, w=True)):
            with self.subTest(map=m):
                if m is None:
                    from asset_manifest import validate_map
                    self.assertTrue(validate_map(None, {}))
                else:
                    self.assertTrue(self.errors(map_data=m))
        for doc in ([], {'schemaVersion': True}, {'schemaVersion': 1, 'assets': []}):
            self.assertTrue(validate_catalogue(doc, self.root)[0])

    def test_invalid_profile_shapes_report_errors_instead_of_crashing(self):
        for bad in ([], '16', {'x': 16}, [16], [True, 16], [0, 16]):
            with self.subTest(pixels=bad):
                d = copy.deepcopy(self.doc)
                d['profiles']['test']['pixelsPerCell'] = bad
                self.assertIn('profile_size', self.codes(doc=d))

    def test_misspelled_rules_are_not_silently_skipped(self):
        self.asset['rules']['forbidRaod'] = True
        self.assertIn('asset_rules', self.codes())
        self.doc['profiles']['test']['pallete'] = ['#339944']
        self.assertIn('profile_fields', self.codes())

    def test_cannot_disable_binary_alpha_rule(self):
        self.doc['profiles']['test']['binaryAlpha'] = False
        self.assertIn('profile_alpha', self.codes())

    def test_legacy_maps_are_not_certified(self):
        del self.map['assetPlacementVersion']
        self.assertIn('placement_schema', self.codes())

    def test_current_production_map_is_explicitly_not_certified(self):
        current = json.loads((ROOT/'Assets/PocketCaravan/Maps/Naru.json').read_text())
        self.assertEqual(self.codes(map_data=current), {'placement_schema'})

    def test_cli_readonly_valid_failure_and_bad_json(self):
        manifest, target = self.root/'catalogue.json', self.root/'map.json'
        manifest.write_text(json.dumps(self.doc)); target.write_text(json.dumps(self.map))
        before = (manifest.read_bytes(), target.read_bytes())
        command = [sys.executable, str(ROOT/'Tools/Integration/validate_assets.py'), str(manifest), '--root', str(self.root), '--map', str(target)]
        result = subprocess.run(command, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertEqual(json.loads(result.stdout)['errorCount'], 0)
        self.assertEqual(before, (manifest.read_bytes(), target.read_bytes()))
        for protected in (manifest, target, self.root/'tree.png', self.root/'right.png', self.root/'review.txt'):
            with self.subTest(protected=protected.name):
                original = protected.read_bytes()
                result = subprocess.run(command + ['--report', str(protected)], capture_output=True)
                self.assertEqual(result.returncode, 2)
                self.assertEqual(original, protected.read_bytes())
        report = self.root/'report.json'
        self.assertEqual(subprocess.run(command + ['--report', str(report)], capture_output=True).returncode, 0)
        self.assertTrue(json.loads(report.read_text())['placementsChecked'])
        self.map['obj'][0] = 0; target.write_text(json.dumps(self.map))
        self.assertEqual(subprocess.run(command, capture_output=True).returncode, 1)
        manifest.write_text('{')
        result = subprocess.run(command, capture_output=True, text=True)
        self.assertEqual(result.returncode, 1)
        self.assertEqual(json.loads(result.stdout)['errors'][0]['code'], 'input_read')


if __name__ == '__main__':
    unittest.main()
