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

    def test_graphics_only_replacement_keeps_all_gameplay_fields(self):
        record=json.loads((ROOT/'ArtReview/BuildingNatureBinding-20261029/building-binding.json').read_text())
        self.assertEqual(len(record['gameplaySHA256']),13)
        for mid,sha in record['gameplaySHA256'].items():
            d=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            gameplay={k:v for k,v in d.items() if k not in ['ground','deco','obj','high','palette']}
            self.assertEqual(hashlib.sha256(json.dumps(gameplay,sort_keys=True,separators=(',',':')).encode()).hexdigest(),sha,mid)
if __name__=='__main__':unittest.main()
