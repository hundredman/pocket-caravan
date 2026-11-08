"""11/08 승인된 고려 생활 소품(장독대·빨래줄·쌀자루): 원본 .aseprite와 게임 그림이 같아야 한다."""
import json, sys, unittest
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[2]
PKG = ROOT / 'ArtReview/Approved/GoryeoLifeProps-v1'
sys.path.insert(0, str(ROOT / 'Tools/MapGen')); sys.path.insert(0, str(ROOT / 'Tools/ArtGen'))


class GoryeoLifeProps(unittest.TestCase):
    def test_catalogue_valid(self):
        from asset_manifest import validate_catalogue
        errors, _ = validate_catalogue(json.loads((PKG / 'catalogue.json').read_text()), ROOT)
        self.assertEqual(errors, [])

    def test_sources_match_production(self):
        import aseprite_rgba
        prod = {a['key']: a['production'] for a in json.loads((PKG / 'approval.json').read_text())['assets']}
        frames = {'jars': ['jars'], 'laundry': ['laundry_0', 'laundry_1'], 'sack': ['sack']}
        for name, keys in frames.items():
            got = aseprite_rgba.read(PKG / f'{name}.aseprite')
            self.assertEqual(len(got), len(keys))
            for im, key in zip(got, keys):
                with Image.open(ROOT / prod[key]) as p:
                    self.assertEqual(im.tobytes(), p.convert('RGBA').tobytes(), key)


if __name__ == '__main__':
    unittest.main()
