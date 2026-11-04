"""Whole approved graphics and production gameplay preservation after art replacement."""
import hashlib,json,sys,unittest
from pathlib import Path
from unittest.mock import patch
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import building_bindings as binding
class BuildingBindings(unittest.TestCase):
    def test_all_ten_whole_approved_sources_load(self):
        self.assertEqual(len(binding.ASSETS),10)
        for name,entry in binding.ASSETS.items():
            actual=binding.image(name)
            self.assertEqual(list(actual.size),entry['size'])
            self.assertEqual(hashlib.sha256(actual.tobytes()).hexdigest(),entry['rgbaSha256'])

    def test_changed_pixel_is_rejected(self):
        original=Image.open
        def changed(*args,**kwargs):
            im=original(*args,**kwargs).convert('RGBA');p=im.getpixel((0,0));im.putpixel((0,0),((p[0]+1)%256,)+p[1:]);return im
        with patch.object(binding.Image,'open',side_effect=changed):
            with self.assertRaisesRegex(ValueError,'changed'):binding.image('boat')

    def test_production_uses_new_approved_building_and_boat_paths(self):
        manifest=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json').read_text())
        expected=set(binding.ASSETS)-{'gate'} # Current M2 has no placed gate.
        for name in expected:
            self.assertEqual(manifest['sources']['sprite:c7:'+name]['path'],binding.ASSETS[name]['png'])

    def test_integration_preserves_gameplay_except_approved_nature_and_entrances(self):
        baseline=json.loads((ROOT/'Docs/Validation/A7d-Integration-2026-10-30/before-map-gameplay.json').read_text())
        changes=json.loads((ROOT/'Docs/Validation/A7d-Integration-2026-10-30/nature-exits.json').read_text())['relocations']
        moved_exits={(c['map'],c['target'],c['spawnId'])for c in changes}
        moved_spawns={(c['target'],c['spawnId'])for c in changes}
        for mid,before in baseline.items():
            d=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            import sys
            sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
            from port_water import restore_for_historical_checks
            d=restore_for_historical_checks(d)
            for key,value in before['stable'].items():self.assertEqual(d[key],value,(mid,key))
            for obj in before['objects']:
                if obj['kind']=='exit'and(mid,obj['target'],obj['spawn'])in moved_exits:
                    self.assertTrue(any(o['kind']=='exit'and o['target']==obj['target']and o['spawn']==obj['spawn']for o in d['objects']))
                elif obj['kind']=='spawn'and(mid,obj['id'])in moved_spawns:
                    self.assertTrue(any(o['kind']=='spawn'and o['id']==obj['id']for o in d['objects']))
                else:self.assertIn(obj,d['objects'],(mid,obj))
if __name__=='__main__':unittest.main()
