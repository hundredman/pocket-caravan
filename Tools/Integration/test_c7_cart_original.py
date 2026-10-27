import sys,unittest
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
from c7_cart_original import OUT,ORIGINAL
from c7_catalogue import rgba_evidence
class OriginalCartChecks(unittest.TestCase):
    def test_unchanged_original_pixels(self):
        self.assertEqual(rgba_evidence(ORIGINAL),rgba_evidence(OUT/'cart.png'))
    def test_actual_capture_only_cart_changed(self):
        self.assertIn('previewOnly true',(OUT/'Screens/complete.txt').read_text())
        self.assertEqual(rgba_evidence(OUT/'Screens/before.png'),rgba_evidence(ROOT/'ArtReview/C7-CartRoof-v2-20261027/Screens/trial.png'))
        a=Image.open(OUT/'Screens/before.png').convert('RGB');b=Image.open(OUT/'Screens/trial.png').convert('RGB')
        self.assertEqual(a.size,(480,270));self.assertEqual(a.size,b.size)
        box=(80,110,112,142);self.assertTrue(ImageChops.difference(a,b).getbbox())
        native=Image.open(ORIGINAL).convert('RGBA');actual=b.crop(box)
        for p,q in zip(native.get_flattened_data(),actual.get_flattened_data()):
            if p[3]:self.assertLessEqual(max(abs(c-v) for c,v in zip(p[:3],q)),1)
        a.paste(b.crop(box),box);self.assertIsNone(ImageChops.difference(a,b).getbbox())
if __name__=='__main__':unittest.main()
