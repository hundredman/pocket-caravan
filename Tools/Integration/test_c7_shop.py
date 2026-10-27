"""Whole shop review safeguards; no aesthetic acceptance implied."""
import json,sys,tempfile,subprocess,unittest
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'Tools/ArtTrials'),str(ROOT/'Tools/MapGen')]
from c7_shop import OUT,COLORS
from c7_catalogue import rgba_evidence
from asset_manifest import validate
class ShopChecks(unittest.TestCase):
    def test_whole24_cells_and_pending_production(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('trial',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('trial',layout)],ROOT,True))
        result=Image.new('RGBA',(96,64));tiles=doc['assets'][0]['tiles']
        self.assertEqual(len(tiles),24)
        for t in tiles:
            with Image.open(ROOT/t['image']) as im:result.alpha_composite(im.convert('RGBA'),(t['dx']*16,t['dy']*16))
        self.assertEqual(result.tobytes(),Image.open(OUT/'Native/shop.png').convert('RGBA').tobytes())
        # Losing even one rendered cell must fail the whole-object placement check.
        layout['obj'][13]=0
        self.assertTrue(validate(doc,[('partial',layout)],ROOT,False))
    def test_canvas_foot_colors_and_single_pixel_outer_contour(self):
        im=Image.open(OUT/'Native/shop.png').convert('RGBA');self.assertEqual(im.size,(96,64))
        self.assertEqual(im.getchannel('A').getbbox()[3],63)
        allowed={tuple(bytes.fromhex(c)) for c in COLORS}
        for y in range(64):
            for x in range(96):
                pixel=im.getpixel((x,y));self.assertIn(pixel[3],(0,255))
                if not pixel[3]:continue
                self.assertIn(pixel[:3],allowed)
                if any(xx<0 or yy<0 or xx>=96 or yy>=64 or not im.getpixel((xx,yy))[3] for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):self.assertEqual(pixel[:3],(46,36,29))
    def test_editable_native_reexports_rgba(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable; native reexport not run')
        with tempfile.TemporaryDirectory() as temp:
            output=Path(temp)/'shop.png'
            subprocess.run([str(binary),'-b',str(OUT/'Native/shop.aseprite'),'--save-as',str(output)],check=True,capture_output=True)
            self.assertEqual(rgba_evidence(output),rgba_evidence(OUT/'Native/shop.png'))
    def test_actual_play_only_shop_changed(self):
        folder=OUT/'Screens';self.assertIn('previewOnly true',(folder/'complete.txt').read_text())
        a=Image.open(folder/'before.png').convert('RGB');b=Image.open(folder/'trial.png').convert('RGB')
        self.assertEqual(a.size,(480,270));self.assertEqual(a.size,b.size)
        source=Image.open(OUT/'Native/shop.png').convert('RGBA')
        actual=b.crop((48,46,144,110))
        for pixel,screen in zip(source.get_flattened_data(),actual.get_flattened_data()):
            if pixel[3]:self.assertLessEqual(max(abs(x-y) for x,y in zip(pixel[:3],screen)),1)
        box=ImageChops.difference(a,b).getbbox()
        self.assertTrue(box and 48<=box[0]<box[2]<=144 and 46<=box[1]<box[3]<=110,box)
if __name__=='__main__':unittest.main()
