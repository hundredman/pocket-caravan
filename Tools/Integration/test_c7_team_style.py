"""New team-style asset safeguards; user decides aesthetic acceptance."""
import json,re,subprocess,sys,tempfile,unittest
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from c7_team_prepare import OUT,HOUSE,GROUND
from c7_catalogue import rgba_evidence
from asset_manifest import validate_catalogue


class C7TeamStyleChecks(unittest.TestCase):
    def test_catalogue_pending_and_complete(self):
        errors,assets=validate_catalogue(json.loads((OUT/'catalogue.json').read_text()),ROOT)
        self.assertEqual(errors,[]);self.assertEqual(len(assets),4)
        self.assertEqual({assets[k]['review']['status'] for k in ['c7ts_thatch','c7ts_tile_house']},{'approved'})
        self.assertEqual({assets[k]['review']['status'] for k in ['c7ts_pine','c7ts_grass_dirt']},{'pending'})
        pieces=assets['c7ts_grass_dirt']['pieces']
        self.assertEqual(len([p for p in pieces if re.fullmatch(r'grass-\d+',p)]),47)
        self.assertTrue({f'{prefix}-{v}' for prefix in ['grass-center','dirt'] for v in [1,2,3]}<=pieces.keys())

    def test_original_canvas_feet_pine_and_explicit_colors(self):
        old=ROOT/'ArtReview/C7-Trial4-20261026/Native/pine.png'
        self.assertEqual(rgba_evidence(old),rgba_evidence(OUT/'Native/pine.png'))
        for name in ['thatch','tile-house']:
            self.assertEqual(rgba_evidence(OUT/'Native'/(name+'.png')),rgba_evidence(ROOT/'ArtReview/Approved/C7-Buildings-v5'/(name+'.png')))
        for name in ['thatch','tile-house','pine']:
            with Image.open(OUT/'Native'/(name+'.png')) as im:
                self.assertEqual(im.size,(64,48));self.assertEqual(im.convert('RGBA').getchannel('A').getbbox()[3],47)
        for p in (OUT/'Native').glob('*.png'):
            if p.stem=='pine':continue
            colors=HOUSE if p.stem in ['thatch','tile-house'] else GROUND
            allowed={tuple(bytes.fromhex(c)) for c in colors}
            with Image.open(p) as im:
                for pixel in im.convert('RGBA').get_flattened_data():
                    self.assertIn(pixel[3],(0,255))
                    if pixel[3]:self.assertIn(pixel[:3],allowed)

    def test_aseprite57_sources_reexport_identical_rgba(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable; source roundtrip not run')
        names=[p.stem for p in sorted((OUT/'Native').glob('*.aseprite'))]
        self.assertEqual(len(names),57)
        with tempfile.TemporaryDirectory() as temp:
            script=Path(temp)/'roundtrip.lua'
            script.write_text('\n'.join(f"local s=app.open({json.dumps(str(OUT/'Native'/(n+'.aseprite')),ensure_ascii=False)});s:saveCopyAs({json.dumps(str(Path(temp)/(n+'.png')),ensure_ascii=False)});s:close()" for n in names)+'\n')
            subprocess.run([str(binary),'-b','--script',str(script)],check=True,capture_output=True)
            for n in names:self.assertEqual(rgba_evidence(OUT/'Native'/(n+'.png')),rgba_evidence(Path(temp)/(n+'.png')),n)

    def test_actual_figma_reference_evidence(self):
        index=json.loads((OUT/'Reference/index.json').read_text())
        self.assertEqual({n['nodeId'] for n in index['nodes']},{'77:9','78:11','92:31','92:32','92:102','92:107'})
        for node in index['nodes']:
            with Image.open(OUT/'Reference'/node['file']) as im:self.assertEqual(im.size,(node['width'],node['height']))

    def test_actual_play_capture_and_stored_native_pixels(self):
        self.assertIn('previewOnly true',(OUT/'Screens-v5/complete.txt').read_text())
        for name in ['before','trial']:
            with Image.open(OUT/'Screens-v5'/(name+'.png')) as im:self.assertEqual(im.size,(480,270))
        for path,expected in json.loads((OUT/'native-rgba.json').read_text()).items():self.assertEqual(rgba_evidence(ROOT/path),expected)


if __name__=='__main__':unittest.main()
