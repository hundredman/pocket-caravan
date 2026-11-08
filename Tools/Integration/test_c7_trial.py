"""C7 visual-review safeguards; technical checks never approve the art."""
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
from c7_catalogue import OUT, rgba_evidence
from asset_manifest import validate_catalogue

import sys as _ase_sys
_ase_sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from Tools.Aseprite.cli import run as run_aseprite


class C7TrialChecks(unittest.TestCase):
    def test_catalogue_keeps_user_review_decisions(self):
        doc=json.loads((OUT/'catalogue.json').read_text())
        errors, assets=validate_catalogue(doc,ROOT)
        self.assertEqual(errors,[])
        self.assertEqual(len(assets),4)
        self.assertEqual(assets['c7_pine']['review']['status'],'pending')
        self.assertEqual({a['review']['status'] for k,a in assets.items() if k!='c7_pine'},{'changes_requested'})
        self.assertEqual(len([p for p in assets['c7_grass_dirt']['pieces'] if p.startswith('grass-')]),47)

    def test_original_envelope_and_feet_preserved(self):
        for name,original in [('thatch','c6_house_thatch_a2295da0'),('tile-house','c6_house_tile_fb336ff1'),('pine','c6_goryeo_woodland0_47258e21')]:
            with self.subTest(name=name):
                a=Image.open(OUT/'Native'/(name+'.png')).convert('RGBA')
                b=Image.open(ROOT/'Assets/PocketCaravan/Art/Baked/Objects'/(original+'.png')).convert('RGBA')
                self.assertEqual(a.size,b.size)
                self.assertEqual(a.getchannel('A').getbbox()[3],b.getchannel('A').getbbox()[3])
                self.assertLessEqual(abs(a.getchannel('A').getbbox()[2]-b.getchannel('A').getbbox()[2]),1)

    def test_all_aseprite_sources_reexport_identical_rgba(self):
        ase='/Applications/Aseprite.app/Contents/MacOS/aseprite'
        if not Path(ase).exists():
            self.skipTest('Aseprite unavailable; source roundtrip not executed')
        with tempfile.TemporaryDirectory() as temp:
            lua=Path(temp)/'roundtrip.lua'
            names=[p.stem for p in sorted((OUT/'Native').glob('*.aseprite'))]
            code=['local root=app.params.root;local out=app.params.output']
            for name in names:
                code.append(f"local s=app.open(root..'/Native/{name}.aseprite');s:saveCopyAs(out..'/{name}.png');s:close()")
            lua.write_text('\n'.join(code)+'\n')
            run_aseprite([ase,'-b','--script-param','root='+str(OUT),'--script-param','output='+temp,'--script',str(lua)],check=True,capture_output=True)
            for name in names:
                with self.subTest(name=name):
                    self.assertEqual(rgba_evidence(OUT/'Native'/(name+'.png')),rgba_evidence(Path(temp)/(name+'.png')))

    def test_capture_evidence_is_actual_native_camera_and_not_manual_play_claim(self):
        complete=(OUT/'Screens-v2/complete.txt').read_text()
        self.assertIn('previewOnly true',complete)
        self.assertIn('no navigation or manual-play claim',complete)
        for name in ['before','trial']:
            self.assertEqual(Image.open(OUT/'Screens-v2'/(name+'.png')).size,(480,270))


if __name__=='__main__':unittest.main()
