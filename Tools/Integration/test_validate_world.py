"""Regressions: reject integration mistakes without writing project assets."""
import json
import sys
import tempfile
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_world import validate


class IntegrationChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.base = self.root / 'Assets/PocketCaravan'
        (self.base / 'World/Regions').mkdir(parents=True)
        (self.base / 'Maps').mkdir()
        (self.base / 'Data').mkdir()
        (self.base / 'Scenes').mkdir()
        (self.base / 'Data/Region.asset').write_text('  id: test\n')
        self.region = dict(schemaVersion=1, regionId='test', regionAssetPath='Assets/PocketCaravan/Data/Region.asset', maps=[], npcs=[], villagers=[])
        self.maps = {}
        for i, mid in enumerate(('test_a', 'test_b')):
            self.region['maps'].append(dict(mapId=mid, jsonPath=f'Assets/PocketCaravan/Maps/{mid}.json', scenePath=f'Assets/PocketCaravan/Scenes/{mid}.unity', worldX=i, worldY=0))
            (self.base / f'Scenes/{mid}.unity').touch()
            self.maps[mid] = dict(id=mid, w=4, h=4, palette=[''], **{k:[0]*16 for k in ('ground','deco','obj','high','road','blocked')}, objects=[dict(kind='spawn',id='S0',x=1,y=1),dict(kind='exit',target='test_b' if i==0 else 'test_a',spawn='S0',x=0,y=1)])
        (self.base/'World/settings.json').write_text(json.dumps(dict(schemaVersion=1,startMapId='test_a')))

    def run_check(self):
        (self.base/'World/Regions/test.json').write_text(json.dumps(self.region))
        for mid, data in self.maps.items():
            (self.base/f'Maps/{mid}.json').write_text(json.dumps(data))
        return validate(self.root)[0]

    def test_valid_round_trip(self):
        self.assertEqual(self.run_check(), [])

    def test_duplicate_coordinate(self):
        self.region['maps'][1]['worldX']=0
        self.assertTrue(any('Duplicate coordinate' in e for e in self.run_check()))

    def test_unknown_destination_spawn(self):
        self.maps['test_a']['objects'][1]['spawn']='missing'
        self.assertTrue(any('Unknown destination spawn' in e for e in self.run_check()))

    def test_duplicate_map_ignoring_case(self):
        self.region['maps'][1]['mapId']='TEST_A'
        self.assertTrue(any('Map ID missing/duplicate' in e for e in self.run_check()))

    def test_missing_return_exit(self):
        self.maps['test_b']['objects'].pop()
        self.assertTrue(any('Missing return exit' in e for e in self.run_check()))

    def test_mismatched_port_width(self):
        self.maps['test_a']['objects'][1]['h']=2
        self.assertTrue(any('Port width/axis mismatch' in e for e in self.run_check()))

    def test_duplicate_content_id_across_files(self):
        for name in ('one','two'):
            (self.base/f'Data/{name}.asset').write_text('  m_EditorClassIdentifier: Assembly-CSharp::PocketCaravan.ItemData\n  id: salt\n')
        self.assertTrue(any('Duplicate ItemData ID' in e for e in self.run_check()))

    def test_start_spawn_required(self):
        self.maps['test_a']['objects'][0]['id']='other'
        self.assertTrue(any('Start map has no S0' in e for e in self.run_check()))

    def test_scene_must_be_included(self):
        (self.base/'Scenes/test_b.unity').unlink()
        self.assertTrue(any('Missing generated scene' in e for e in self.run_check()))

    def test_unregistered_npc(self):
        self.maps['test_a']['objects'].append(dict(kind='npc',npc='test_missing'))
        self.assertTrue(any('Unregistered NPC' in e for e in self.run_check()))


if __name__ == '__main__':
    unittest.main()
