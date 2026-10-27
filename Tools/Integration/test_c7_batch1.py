"""C7 three-art batch integrity; human approval remains per image."""
import json,sys,subprocess,tempfile,unittest
from pathlib import Path
from PIL import Image,ImageChops,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'Tools/ArtTrials'),str(ROOT/'Tools/MapGen')]
from c7_batch1 import OUT,DEFS,BUILDING,NATURE
from c7_catalogue import rgba_evidence
from asset_manifest import validate
class Batch1Checks(unittest.TestCase):
    def test_three_whole_objects_and_independent_pending(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('batch',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('batch',layout)],ROOT,True))
        self.assertEqual(len(layout['terrainTags']),layout['w']*layout['h'])
        self.assertEqual(len(doc['assets']),3)
        self.assertEqual(sum(len(a['tiles']) for a in doc['assets']),61)
        for a in doc['assets']:
            self.assertEqual(a['review']['status'],'pending');w,h=a['size'];result=Image.new('RGBA',(w*16,h*16))
            for t in a['tiles']:
                with Image.open(ROOT/t['image']) as im:result.alpha_composite(im.convert('RGBA'),(16*t['dx'],16*t['dy']))
            self.assertEqual(result.tobytes(),Image.open(ROOT/a['image']).convert('RGBA').tobytes())
        layout['obj'][21]=0;self.assertTrue(validate(doc,[('partial',layout)],ROOT,False))
    def test_native_sizes_feet_palette_and_contours(self):
        for d in DEFS:
            im=Image.open(OUT/'Native'/(d['name']+'.png')).convert('RGBA');self.assertEqual(list(im.size),d['size']);self.assertEqual(im.getchannel('A').getbbox()[3],d['bottom'])
            colors=BUILDING if d['profile']=='building' else NATURE;allowed={tuple(bytes.fromhex(c)) for c in colors};w,h=im.size
            for y in range(h):
                for x in range(w):
                    p=im.getpixel((x,y));self.assertIn(p[3],(0,255))
                    if p[3]:
                        self.assertIn(p[:3],allowed)
                        if any(xx<0 or yy<0 or xx>=w or yy>=h or not im.getpixel((xx,yy))[3] for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):self.assertEqual(p[:3],(46,36,29))
    def test_three_editable_sources_reexport_rgba(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable; source roundtrip not run')
        with tempfile.TemporaryDirectory() as temp:
            for d in DEFS:
                n=d['name'];p=Path(temp)/(n+'.png');subprocess.run([str(binary),'-b',str(OUT/'Native'/(n+'.aseprite')),'--save-as',str(p)],check=True,capture_output=True)
                self.assertEqual(rgba_evidence(p),rgba_evidence(OUT/'Native'/(n+'.png')))
    def test_actual_play_latest_pixels_and_only_three_areas(self):
        self.assertIn('previewOnly true',(OUT/'Screens/complete.txt').read_text())
        a=Image.open(OUT/'Screens/before.png').convert('RGB');b=Image.open(OUT/'Screens/trial.png').convert('RGB');self.assertEqual(a.size,(480,270));self.assertEqual(a.size,b.size)
        diff=ImageChops.difference(a,b);outside=diff.copy();draw=ImageDraw.Draw(outside)
        for d in DEFS:
            x,y=[v*16 for v in d['view']];w,h=d['size'];box=(x,270-y-h,x+w,270-y)
            self.assertIsNotNone(diff.crop(box).getbbox(),d['name'])
            im=Image.open(OUT/'Native'/(d['name']+'.png')).convert('RGBA');actual=b.crop(box)
            for p,q in zip(im.get_flattened_data(),actual.get_flattened_data()):
                if p[3]:self.assertLessEqual(max(abs(c-v) for c,v in zip(p[:3],q)),1,d['name'])
            draw.rectangle((box[0],box[1],box[2]-1,box[3]-1),fill=0)
        self.assertIsNone(outside.getbbox())
if __name__=='__main__':unittest.main()
