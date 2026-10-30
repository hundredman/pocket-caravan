"""Cart-only revision: native integrity and unchanged sibling props/context."""
import json,sys,subprocess,tempfile,unittest
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'Tools/ArtTrials'),str(ROOT/'Tools/MapGen')]
from c7_cart_revision import OUT,PREVIOUS,BUILDING
from c7_catalogue import rgba_evidence
from asset_manifest import validate
class CartRevisionChecks(unittest.TestCase):
    def test_whole_cart_and_pending_production(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('cart',layout)],ROOT,False),[]);self.assertTrue(validate(doc,[('cart',layout)],ROOT,True))
        a=doc['assets'][0];self.assertEqual(len(a['tiles']),4);result=Image.new('RGBA',(32,32))
        for t in a['tiles']:
            with Image.open(ROOT/t['image']) as im:result.alpha_composite(im.convert('RGBA'),(16*t['dx'],16*t['dy']))
        target=Image.open(OUT/'Native/cart.png').convert('RGBA');self.assertEqual(result.tobytes(),target.tobytes());self.assertEqual(target.getchannel('A').getbbox()[3],30)
        allowed={tuple(bytes.fromhex(c)) for c in BUILDING}
        for p in target.get_flattened_data():
            self.assertIn(p[3],(0,255))
            if p[3]:self.assertIn(p[:3],allowed)
        layout['obj'][5]=0;self.assertTrue(validate(doc,[('partial',layout)],ROOT,False))
    def test_native_source_reexports_rgba(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable')
        with tempfile.TemporaryDirectory() as temp:
            p=Path(temp)/'cart.png';subprocess.run([str(binary),'-b',str(OUT/'Native/cart.aseprite'),'--save-as',str(p)],check=True,capture_output=True)
            self.assertEqual(rgba_evidence(p),rgba_evidence(OUT/'Native/cart.png'))
    def test_sibling_pixels_preserved(self):
        for p,evidence in json.loads((OUT/'context-evidence.json').read_text()).items():self.assertEqual(rgba_evidence(ROOT/p),evidence)
    def test_actual_play_same_previous_and_only_cart_changed(self):
        self.assertIn('previewOnly true',(OUT/'Screens/complete.txt').read_text())
        self.assertEqual(rgba_evidence(OUT/'Screens/before.png'),rgba_evidence(PREVIOUS/'Screens/trial.png'))
        a=Image.open(OUT/'Screens/before.png').convert('RGB');b=Image.open(OUT/'Screens/trial.png').convert('RGB');self.assertEqual(a.size,(480,270));self.assertEqual(a.size,b.size)
        box=ImageChops.difference(a,b).getbbox();self.assertTrue(box and 80<=box[0]<box[2]<=112 and 110<=box[1]<box[3]<=142,box)
        native=Image.open(OUT/'Native/cart.png').convert('RGBA');actual=b.crop((80,110,112,142))
        for p,q in zip(native.get_flattened_data(),actual.get_flattened_data()):
            if p[3]:self.assertLessEqual(max(abs(c-v) for c,v in zip(p[:3],q)),1)
if __name__=='__main__':unittest.main()
