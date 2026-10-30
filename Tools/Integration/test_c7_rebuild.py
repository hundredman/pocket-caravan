"""Safeguards for redesigned C7 art; no automated art approval."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from c7_rebuild_catalogue import OUT, OLD
from c7_catalogue import rgba_evidence
from asset_manifest import validate_catalogue


class C7RebuildChecks(unittest.TestCase):
    def test_whole_objects_and_complete_boundary_kit_keep_user_feedback(self):
        doc=json.loads((OUT/'catalogue.json').read_text())
        errors,assets=validate_catalogue(doc,ROOT)
        self.assertEqual(errors,[])
        self.assertEqual(len(assets),4)
        self.assertEqual(assets['c7r3_pine']['review']['status'],'pending')
        self.assertEqual({a['review']['status'] for k,a in assets.items() if k!='c7r3_pine'},{'changes_requested'})
        self.assertEqual(len([p for p in assets['c7r3_grass_dirt']['pieces'] if p.startswith('grass-')]),47)

    def test_canvas_feet_and_pine_preservation(self):
        self.assertEqual(rgba_evidence(OUT/'Native/pine.png'),rgba_evidence(OLD/'Native/pine.png'))
        for name in ['thatch','tile-house','pine']:
            with Image.open(OUT/'Native'/(name+'.png')) as source:
                im=source.convert('RGBA')
                self.assertEqual(im.size,(64,48))
                box=im.getchannel('A').getbbox()
                self.assertEqual(box[3],47)
                self.assertGreaterEqual(box[3]-box[1],44)

    def test_ground_centers_are_quiet_and_binary(self):
        for name in ['grass-255','dirt']:
            with Image.open(OUT/'Native'/(name+'.png')) as source:
                pixels=list(source.convert('RGBA').get_flattened_data())
            self.assertEqual({p[3] for p in pixels},{255})
            for c in range(3):
                self.assertLessEqual(max(p[c] for p in pixels)-min(p[c] for p in pixels),3)

    def test_native_sources_reexport_identical_decoded_pixels(self):
        binary=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
        if not binary.exists():self.skipTest('Aseprite unavailable; native-source roundtrip not run')
        names=[p.stem for p in sorted((OUT/'Native').glob('*.aseprite'))]
        self.assertEqual(len(names),51)
        with tempfile.TemporaryDirectory() as temp:
            script=Path(temp)/'roundtrip.lua'
            script.write_text('\n'.join(f"local s=app.open({json.dumps(str(OUT/'Native'/(name+'.aseprite')),ensure_ascii=False)});s:saveCopyAs({json.dumps(str(Path(temp)/(name+'.png')),ensure_ascii=False)});s:close()" for name in names)+'\n')
            subprocess.run([str(binary),'-b','--script',str(script)],check=True,capture_output=True)
            for name in names:
                self.assertEqual(rgba_evidence(OUT/'Native'/(name+'.png')),rgba_evidence(Path(temp)/(name+'.png')),name)

    def test_actual_capture_and_source_evidence(self):
        self.assertIn('previewOnly true',(OUT/'Screens-v4/complete.txt').read_text())
        for name in ['before','trial']:
            with Image.open(OUT/'Screens-v4'/(name+'.png')) as im:self.assertEqual(im.size,(480,270))
        for path,evidence in json.loads((OUT/'native-rgba.json').read_text()).items():
            self.assertEqual(rgba_evidence(ROOT/path),evidence)


if __name__=='__main__':unittest.main()
