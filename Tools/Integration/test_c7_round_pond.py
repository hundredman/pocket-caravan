"""Rounded pond fixed-layout safeguards, not an aesthetic approval."""
from pathlib import Path
import json,subprocess,sys,tempfile,unittest
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
from asset_manifest import validate
from c7_catalogue import rgba_evidence

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
OUT=ROOT/'ArtReview/C7-RoundPond-20261027'

class C7RoundPondChecks(unittest.TestCase):
    def test_preview_valid_and_unapproved_production_rejected(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('trial',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('trial',layout)],ROOT,True))
        self.assertEqual(doc['assets'][0]['review']['status'],'pending')

    def test_all15_modules_reassemble_full_oval(self):
        result=Image.new('RGBA',(80,48))
        for y in range(3):
            for x in range(5):
                with Image.open(OUT/'pieces'/f'oval_{x}_{y}.png') as im:result.alpha_composite(im.convert('RGBA'),(x*16,y*16))
        with Image.open(OUT/'pond.png') as im:
            target=im.convert('RGBA');self.assertEqual(result.tobytes(),target.tobytes())
        spans=[]
        for y in range(48):
            xs=[x for x in range(80) if result.getpixel((x,y))[3]]
            if xs:
                self.assertEqual(xs,list(range(xs[0],xs[-1]+1)))
                spans.append(len(xs))
        mid=len(spans)//2
        self.assertEqual(spans[:mid],sorted(spans[:mid]))
        self.assertEqual(spans[mid:],sorted(spans[mid:],reverse=True))
        self.assertLessEqual(max(abs(a-b) for a,b in zip(spans,spans[1:])),12)

    def test_editable_native_roundtrip_and_existing_colors(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable; native roundtrip not run')
        with tempfile.TemporaryDirectory() as temp:
            output=Path(temp)/'pond.png'
            run_aseprite([str(binary),'-b',str(OUT/'pond.aseprite'),'--save-as',str(output)],check=True,capture_output=True)
            self.assertEqual(rgba_evidence(output),rgba_evidence(OUT/'pond.png'))
        with Image.open(OUT/'pond.png') as im:
            pixels=list(im.convert('RGBA').get_flattened_data())
        self.assertEqual({p[3] for p in pixels},{0,255})
        self.assertEqual({p[:3] for p in pixels if p[3]},{(132,103,94),(120,160,153),(127,185,175)})

    def test_play_comparison_changes_only_pond_rectangle(self):
        folder=ROOT/'ArtReview/C7-TeamStyle-20261027/PondShape-v10'
        self.assertIn('previewOnly true',(folder/'complete.txt').read_text())
        with Image.open(folder/'before.png') as im:a=im.convert('RGB')
        with Image.open(folder/'trial.png') as im:b=im.convert('RGB')
        self.assertEqual(a.size,b.size);self.assertEqual(a.size,(480,270))
        box=ImageChops.difference(a,b).getbbox();self.assertIsNotNone(box)
        self.assertTrue(320<=box[0]<box[2]<=400 and 190<=box[1]<box[3]<=238,box)

if __name__=='__main__':unittest.main()
