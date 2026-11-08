"""Whole-source and approval preservation, plus actual Unity capture integrity."""
import json,sys,subprocess,tempfile,unittest
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'Tools/ArtTrials'),str(ROOT/'Tools/MapGen')]
from c7_cart_roof import OUT,COLORS,DEFS
from c7_catalogue import rgba_evidence
from asset_manifest import validate

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
class CartRoofChecks(unittest.TestCase):
    def test_whole_objects_and_review_gate(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('trial',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('trial',layout)],ROOT,True))
        for a,d in zip(doc['assets'],DEFS):
            self.assertEqual(a['review']['status'],json.loads((OUT/'review-decisions.json').read_text())[a['id']]['status'] if (OUT/'review-decisions.json').exists() else 'pending')
            target=Image.open(ROOT/a['image']).convert('RGBA');self.assertEqual(target.size,tuple(d['size']))
            actual=Image.new('RGBA',target.size)
            for t in a['tiles']:
                actual.alpha_composite(Image.open(ROOT/t['image']).convert('RGBA'),(16*t['dx'],16*t['dy']))
            self.assertEqual(actual.tobytes(),target.tobytes())
        for place in layout['assetPlacements']:
            partial=json.loads(json.dumps(layout));partial['obj'][place['y']*12+place['x']]=0
            self.assertTrue(validate(doc,[('partial',partial)],ROOT,False))
    def test_native_reexports_and_palette(self):
        allowed={tuple(bytes.fromhex(c)) for c in COLORS}
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable')
        for d in DEFS:
            n=d['name'];im=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA')
            for p in im.get_flattened_data():
                self.assertIn(p[3],(0,255))
                if p[3]:self.assertIn(p[:3],allowed)
            self.assertEqual(im.getbbox()[3],30 if n=='cart' else 47)
            with tempfile.TemporaryDirectory() as temp:
                output=Path(temp)/(n+'.png')
                run_aseprite([str(binary),'-b',str(OUT/'Native'/(n+'.aseprite')),'--save-as',str(output)],capture_output=True,check=True)
                self.assertEqual(rgba_evidence(output),rgba_evidence(OUT/'Native'/(n+'.png')))
    def test_house_only_roof_changed(self):
        a=Image.open(ROOT/DEFS[1]['old']).convert('RGBA');b=Image.open(OUT/'Native/tile-house.png').convert('RGBA')
        self.assertEqual(a.size,b.size);self.assertEqual(a.getchannel('A').tobytes(),b.getchannel('A').tobytes())
        diff=ImageChops.difference(a.convert('RGB'),b.convert('RGB')).getbbox()
        self.assertTrue(diff and 14<=diff[1]<diff[3]<=27,diff)
        self.assertEqual(a.crop((0,27,64,48)).tobytes(),b.crop((0,27,64,48)).tobytes())
    def test_existing_approved_and_sibling_assets_preserved(self):
        for path,evidence in json.loads((OUT/'context-evidence.json').read_text()).items():
            self.assertEqual(rgba_evidence(ROOT/path),evidence)
        approval=json.loads((ROOT/'ArtReview/Approved/C7-Buildings-v5/approval.json').read_text())
        for a in approval['assets']:
            self.assertEqual(rgba_evidence(ROOT/a['image'])['rgbaSHA256'],a['rgbaSHA256'])
        self.assertEqual(json.loads((ROOT/'ArtReview/C7-CartRebuild-20261027/review-decision.json').read_text())['status'],'changes_requested')
    def test_actual_play_previous_frame_and_changed_regions(self):
        self.assertIn('previewOnly true',(OUT/'Screens/complete.txt').read_text())
        self.assertEqual(rgba_evidence(OUT/'Screens/before.png'),rgba_evidence(ROOT/'ArtReview/C7-CartRebuild-20261027/Screens/trial.png'))
        a=Image.open(OUT/'Screens/before.png').convert('RGB');b=Image.open(OUT/'Screens/trial.png').convert('RGB')
        self.assertEqual(a.size,(480,270));self.assertEqual(a.size,b.size)
        for box,n in [((80,110,112,142),'cart'),((192,62,256,110),'tile-house')]:
            self.assertTrue(ImageChops.difference(a.crop(box),b.crop(box)).getbbox())
            source=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA');actual=b.crop(box)
            for p,q in zip(source.get_flattened_data(),actual.get_flattened_data()):
                if p[3]:self.assertLessEqual(max(abs(c-v) for c,v in zip(p[:3],q)),1)
            a.paste(b.crop(box),box)
        self.assertIsNone(ImageChops.difference(a,b).getbbox())
if __name__=='__main__':unittest.main()
