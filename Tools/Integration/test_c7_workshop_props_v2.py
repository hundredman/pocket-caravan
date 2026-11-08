"""C7 three-art batch integrity; human approval remains per image."""
import json,sys,subprocess,tempfile,unittest
from pathlib import Path
from PIL import Image,ImageChops,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'Tools/ArtTrials'),str(ROOT/'Tools/MapGen')]
from c7_workshop_props_v2 import OUT,DEFS,PALETTES
from c7_catalogue import rgba_evidence
from asset_manifest import validate

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite
class WorkshopPropsV2Checks(unittest.TestCase):
    def test_three_whole_objects_and_independent_pending(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('batch',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('batch',layout)],ROOT,True))
        self.assertEqual(len(layout['terrainTags']),layout['w']*layout['h'])
        self.assertEqual(len(doc['assets']),3)
        self.assertEqual(sum(len(a['tiles']) for a in doc['assets']),18)
        for a in doc['assets']:
            self.assertEqual(a['review']['status'],json.loads((OUT/'review-decisions.json').read_text()).get(a['id'],dict(status='pending'))['status']);w,h=a['size'];result=Image.new('RGBA',(w*16,h*16))
            for t in a['tiles']:
                with Image.open(ROOT/t['image']) as im:result.alpha_composite(im.convert('RGBA'),(16*t['dx'],16*t['dy']))
            self.assertEqual(result.tobytes(),Image.open(ROOT/a['image']).convert('RGBA').tobytes())
        layout['obj'][21]=0;self.assertTrue(validate(doc,[('partial',layout)],ROOT,False))
    def test_native_sizes_feet_palette_and_contours(self):
        for d in DEFS:
            im=Image.open(OUT/'Native'/(d['name']+'.png')).convert('RGBA');self.assertEqual(list(im.size),d['size']);self.assertEqual(im.getchannel('A').getbbox()[3],d['bottom'])
            colors=PALETTES[d['profile']];allowed={tuple(bytes.fromhex(c)) for c in colors};w,h=im.size
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
                n=d['name'];p=Path(temp)/(n+'.png');run_aseprite([str(binary),'-b',str(OUT/'Native'/(n+'.aseprite')),'--save-as',str(p)],check=True,capture_output=True)
                self.assertEqual(rgba_evidence(p),rgba_evidence(OUT/'Native'/(n+'.png')))
    def test_actual_play_latest_pixels_and_only_three_areas(self):
        self.assertIn('previewOnly true',(OUT/'Screens/complete.txt').read_text())
        self.assertEqual(rgba_evidence(OUT/'Screens/before.png'),rgba_evidence(ROOT/'ArtReview/C7-WorkshopProps-20261027/Screens/trial.png'))
        a=Image.open(OUT/'Screens/before.png').convert('RGB');b=Image.open(OUT/'Screens/trial.png').convert('RGB');self.assertEqual(a.size,(480,270));self.assertEqual(a.size,b.size)
        diff=ImageChops.difference(a,b);outside=diff.copy();draw=ImageDraw.Draw(outside)
        for d in DEFS:
            x,y=[v*16 for v in d['view']];w,h=d['size'];box=(x,270-y-h,x+w,270-y)
            self.assertIsNone(diff.crop(box).getbbox(),d['name']) if d['name']=='paper_rack' else self.assertIsNotNone(diff.crop(box).getbbox(),d['name'])
            im=Image.open(OUT/'Native'/(d['name']+'.png')).convert('RGBA');actual=b.crop(box)
            for p,q in zip(im.get_flattened_data(),actual.get_flattened_data()):
                if p[3]:self.assertLessEqual(max(abs(c-v) for c,v in zip(p[:3],q)),1,d['name'])
            draw.rectangle((box[0],box[1],box[2]-1,box[3]-1),fill=0)
        self.assertIsNone(outside.getbbox())
    def test_context_approvals_preserved(self):
        self.assertEqual(rgba_evidence(OUT/'Native/paper_rack.png'),rgba_evidence(ROOT/'ArtReview/C7-WorkshopProps-20261027/Native/paper_rack.png'))
        for n in ['harvest','celadon']:
            im=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA');bbox=im.getbbox();self.assertLessEqual(bbox[3]-bbox[1],24)
        for p,evidence in json.loads((OUT/'context-evidence.json').read_text()).items():
            self.assertEqual(rgba_evidence(ROOT/p),evidence)
        approvals=json.loads((ROOT/'ArtReview/Approved/C7-GateProps-v1/approval.json').read_text())
        for a in approvals:self.assertEqual(rgba_evidence(ROOT/'ArtReview/Approved/C7-GateProps-v1'/(a['name']+'.png')),a['rgbaEvidence'])
if __name__=='__main__':unittest.main()
