"""Whole decorations, water-only placements and isolated review changes."""
from pathlib import Path
import json,sys,subprocess,tempfile,unittest
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'));sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from c7_pond_decor import OUT
from c7_catalogue import rgba_evidence
from asset_manifest import validate

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite

class C7PondDecorChecks(unittest.TestCase):
    def test_a2_water_only_whole_objects_pending(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('decor',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('decor',layout)],ROOT,True))
        layout['terrainTags']=['grass']*15
        self.assertTrue(validate(doc,[('decor',layout)],ROOT,False))
        self.assertEqual({a['review']['status'] for a in doc['assets']},{'pending'})
        checks=json.loads((OUT/'checks.json').read_text());self.assertGreater(checks['coverageFraction'],0);self.assertLess(checks['coverageFraction'],.15)

    def test_aseprite_sources_reexport_same_rgba(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable; native roundtrip not run')
        with tempfile.TemporaryDirectory() as temp:
            for name in ['leaves','flower']:
                output=Path(temp)/(name+'.png')
                run_aseprite([str(binary),'-b',str(OUT/'Native'/(name+'.aseprite')),'--save-as',str(output)],check=True,capture_output=True)
                self.assertEqual(rgba_evidence(output),rgba_evidence(OUT/'Native'/(name+'.png')))
                self.assertEqual(rgba_evidence(output)['size'],[16,16])

    def test_play_variants_only_change_decorations(self):
        folder=ROOT/'ArtReview/C7-TeamStyle-20261027/PondDecor-v11'
        self.assertIn('previewOnly true',(folder/'complete.txt').read_text())
        images=[]
        for name in ['plain','leaves','lotus']:
            with Image.open(folder/(name+'.png')) as im:images.append(im.convert('RGB'))
        for i,bounds in [(1,(336,206,352,222)),(2,(352,206,368,222))]:
            self.assertEqual(images[i].size,(480,270))
            box=ImageChops.difference(images[i-1],images[i]).getbbox();self.assertIsNotNone(box)
            self.assertTrue(bounds[0]<=box[0]<box[2]<=bounds[2] and bounds[1]<=box[1]<box[3]<=bounds[3],box)

if __name__=='__main__':unittest.main()
