"""Batched art candidates and editor-camera evidence. No historical team-data pinning."""
import json,sys,tempfile,subprocess,unittest
from pathlib import Path
from PIL import Image,ImageChops,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'Tools/ArtTrials'),str(ROOT/'Tools/MapGen')]
from c7_remaining_batch import OUT,DEFS,PALETTES
from c7_catalogue import rgba_evidence
from asset_manifest import validate
class RemainingBatchChecks(unittest.TestCase):
    def test_all_twelve_original_variants_are_in_one_batch(self):
        expected={r['assetId']for r in json.loads((OUT/'input-scope.json').read_text())}
        self.assertEqual(len(DEFS),12);self.assertEqual({d['oldAssetId']for d in DEFS},expected)
        self.assertEqual(len({d['name']for d in DEFS}),12)
        render=json.loads((OUT/'render-plan.json').read_text())['assets'];self.assertEqual({e['name']for e in render},{d['name']for d in DEFS})
        for page in range(3):self.assertEqual(sum(e['page']==page for e in render),4)
    def test_preview_valid_and_each_unapproved_item_remains_gated(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(validate(doc,[('review',layout)],ROOT,False),[])
        self.assertTrue(validate(doc,[('review',layout)],ROOT,True))
        self.assertEqual({a['review']['status']for a in doc['assets']},{'pending'})
        self.assertEqual(len(doc['assets']),12)
    def test_all_native_sizes_feet_palettes_binary_alpha_and_contour(self):
        for d in DEFS:
            im=Image.open(OUT/'Native'/(d['name']+'.png')).convert('RGBA');self.assertEqual(list(im.size),d['size']);self.assertEqual(im.getbbox()[3],d['bottom'])
            allowed={tuple(bytes.fromhex(c))for c in PALETTES[d['profile']]};w,h=im.size
            for y in range(h):
                for x in range(w):
                    p=im.getpixel((x,y));self.assertIn(p[3],(0,255))
                    if not p[3]:continue
                    self.assertIn(p[:3],allowed)
                    if any(xx<0 or yy<0 or xx>=w or yy>=h or not im.getpixel((xx,yy))[3]for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):self.assertEqual(p[:3],(46,36,29),(d['name'],x,y))
    def test_complete_render_cells_reassemble_and_missing_any_object_is_rejected(self):
        doc=json.loads((OUT/'catalogue.json').read_text());layout=json.loads((OUT/'layout.json').read_text())
        self.assertEqual(sum(len(a['tiles'])for a in doc['assets']),87)
        for a,placement in zip(doc['assets'],layout['assetPlacements']):
            result=Image.new('RGBA',tuple(16*v for v in a['size']))
            for t in a['tiles']:result.alpha_composite(Image.open(ROOT/t['image']).convert('RGBA'),(16*t['dx'],16*t['dy']))
            self.assertEqual(result.tobytes(),Image.open(ROOT/a['image']).convert('RGBA').tobytes())
            partial=json.loads(json.dumps(layout));partial['obj'][placement['y']*layout['w']+placement['x']]=0
            self.assertTrue(validate(doc,[('partial',partial)],ROOT,False),a['id'])
    def test_editable_sources_reexport_same_decoded_pixels(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite not installed; roundtrip not run')
        with tempfile.TemporaryDirectory()as temp:
            for d in DEFS:
                n=d['name'];p=Path(temp)/(n+'.png');subprocess.run([str(binary),'-b',str(OUT/'Native'/(n+'.aseprite')),'--save-as',str(p)],check=True,capture_output=True)
                self.assertEqual(rgba_evidence(p),rgba_evidence(OUT/'Native'/(n+'.png')))
    def test_editor_camera_all_pages_latest_pixels_and_only_item_rectangles_changed(self):
        meta=json.loads((OUT/'Screens/complete.json').read_text())
        self.assertTrue(meta['editorCamera']);self.assertFalse(meta['playModeRun']);self.assertFalse(meta['productionSceneOpened']);self.assertFalse(meta['sceneSaved']);self.assertEqual(meta['resolution'],[480,270]);self.assertEqual(meta['ppu'],16)
        for page in range(3):
            a=Image.open(OUT/'Screens'/f'page{page}_before.png').convert('RGB');b=Image.open(OUT/'Screens'/f'page{page}_trial.png').convert('RGB')
            self.assertEqual(a.size,(480,270));self.assertEqual(a.size,b.size)
            self.assertEqual(rgba_evidence(OUT/'Screens'/f'page{page}_before.png'),rgba_evidence(OUT/'Smoke'/f'page{page}_before.png'))
            diff=ImageChops.difference(a,b);outside=diff.copy();draw=ImageDraw.Draw(outside)
            for d in DEFS:
                if d['page']!=page:continue
                x,y=d['view'];w,h=d['size'];box=(x*16,270-y*16-h,x*16+w,270-y*16)
                self.assertIsNotNone(diff.crop(box).getbbox(),d['name'])
                im=Image.open(OUT/'Native'/(d['name']+'.png')).convert('RGBA');actual=b.crop(box)
                for p,q in zip(im.get_flattened_data(),actual.get_flattened_data()):
                    if p[3]:self.assertLessEqual(max(abs(c-v)for c,v in zip(p[:3],q)),1,d['name'])
                draw.rectangle((box[0],box[1],box[2]-1,box[3]-1),fill=0)
            self.assertIsNone(outside.getbbox())
    def test_original_art_and_previous_approval_pixels_preserved(self):
        for p,evidence in json.loads((OUT/'source-evidence.json').read_text()).items():self.assertEqual(rgba_evidence(ROOT/p),evidence,p)
    def test_single_sheaves_have_a_broad_body_without_a_long_thin_waist(self):
        for n in ['sheaf_naru','sheaf_jangdan']:
            im=Image.open(OUT/'Native'/(n+'.png')).convert('RGBA')
            for y in range(14,26):
                xs=[x for x in range(16)if im.getpixel((x,y))[3]]
                self.assertGreaterEqual(max(xs)-min(xs)+1,10,(n,y))
if __name__=='__main__':unittest.main()
