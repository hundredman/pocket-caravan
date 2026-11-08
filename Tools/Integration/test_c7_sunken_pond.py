"""Sunken pond review integrity, not aesthetic approval."""
import json,sys,subprocess,tempfile,unittest
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'Tools/MapGen'),str(ROOT/'Tools/ArtTrials')]
from asset_manifest import validate
from c7_catalogue import rgba_evidence

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
OUT=ROOT/'ArtReview/C7-SunkenPond-20261027'
class SunkenPondChecks(unittest.TestCase):
    def test_manifest_and_whole_preset(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('trial',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('trial',layout)],ROOT,True))
        result=Image.new('RGBA',(80,48))
        for y in range(3):
            for x in range(5):
                with Image.open(OUT/'pieces'/f'oval_{x}_{y}.png') as im:result.alpha_composite(im.convert('RGBA'),(x*16,y*16))
        self.assertEqual(result.tobytes(),Image.open(OUT/'pond.png').convert('RGBA').tobytes())
    def test_water_mask_and_decoration(self):
        pond=Image.open(OUT/'pond.png').convert('RGBA');mask=Image.open(OUT/'water-mask.png').convert('RGBA')
        self.assertEqual({p[3] for p in pond.get_flattened_data()},{0,255})
        from c7_sunken_pond_review import COLORS
        used={p[:3] for p in pond.get_flattened_data() if p[3]}
        self.assertTrue(used <= {tuple(bytes.fromhex(c)) for c in COLORS})
        self.assertTrue({tuple(bytes.fromhex(c)) for c in ['98b9a9','7fb9af','619a98']} <= used)
        for y in range(48):
            xs=[x for x in range(80) if pond.getpixel((x,y))[3]]
            if xs:self.assertEqual(xs,list(range(xs[0],xs[-1]+1)))
        for name,dx in [('leaves',16),('flower',32)]:
            im=Image.open(ROOT/'ArtReview/C7-PondDecor-20261027/Native'/f'{name}.png').convert('RGBA')
            for y in range(16):
                for x in range(16):
                    if im.getpixel((x,y))[3]:self.assertEqual(mask.getpixel((x+dx,y+16))[3],255)
    def test_aseprite_roundtrip(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable')
        with tempfile.TemporaryDirectory() as temp:
            for name in ['pond','inner-corner']:
                output=Path(temp)/(name+'.png')
                run_aseprite([str(binary),'-b',str(OUT/(name+'.aseprite')),'--save-as',str(output)],check=True,capture_output=True)
                self.assertEqual(rgba_evidence(output),rgba_evidence(OUT/(name+'.png')))
    def test_actual_play_change_bounds(self):
        folder=ROOT/'ArtReview/C7-TeamStyle-20261027/PondDepth-v12'
        self.assertIn('previewOnly true',(folder/'complete.txt').read_text())
        images=[Image.open(folder/(n+'.png')).convert('RGB') for n in ['before','trial','decorated']]
        for im in images:self.assertEqual(im.size,(480,270))
        box=ImageChops.difference(images[0],images[1]).getbbox()
        self.assertTrue(box and 320<=box[0]<box[2]<=400 and 190<=box[1]<box[3]<=238,box)
        diff=ImageChops.difference(images[1],images[2])
        self.assertIsNotNone(diff.getbbox())
        for y in range(270):
            for x in range(480):
                if diff.getpixel((x,y))!=(0,0,0):self.assertTrue(336<=x<368 and 206<=y<222)
if __name__=='__main__':unittest.main()
