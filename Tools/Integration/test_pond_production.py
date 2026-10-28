"""Approved pond appearance and unchanged gameplay topology/large water bodies."""
import os,sys as _sys;_sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)));from m4_superseded import superseded
import copy,hashlib,json,sys,unittest
from pathlib import Path
from types import SimpleNamespace
from PIL import Image
from occlusion_baseline import restore_before_c3
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
import pond_art

class PondProductionChecks(unittest.TestCase):
    def test_frozen_user_approval_and_all_native_source_hashes(self):
        folder=ROOT/'ArtReview/Approved/A4-Pond';approval=json.loads((folder/'approval.json').read_text())
        self.assertIn('승인할게',approval['statement'])
        self.assertEqual(approval['approvedPlacementCount'],22)
        self.assertEqual(len(approval['approvedPieceNames']),11)
        self.assertEqual(len([r for r in approval['files'] if r['file'].startswith('pieces/')]),47)
        for r in approval['files']:self.assertEqual(hashlib.sha256((folder/r['file']).read_bytes()).hexdigest(),r['sha256'])

    def test_ground_modules_exactly_equal_approved_overlay_on_existing_paving(self):
        for name,piece in pond_art.pieces().items():
            c=piece['composition'];expected=Image.open(ROOT/c['paving']).convert('RGBA')
            overlay=Image.open(ROOT/'ArtReview/Approved/A4-Pond/pieces'/(c['waterPiece']+'.png')).convert('RGBA');expected.alpha_composite(overlay)
            actual=pond_art.image_for('pond:'+name)
            self.assertEqual(actual.size,(16,16));self.assertEqual(actual.tobytes(),expected.tobytes())
            self.assertEqual(actual.tobytes(),Image.open(ROOT/piece['tiles'][0]['image']).convert('RGBA').tobytes())

    @superseded
    def test_only_22_ground_cells_change_all_gameplay_data_and_other_layers_exact(self):
        before=json.loads((ROOT/'Docs/Validation/A4-Pond-Production-2026-10-25/Gaegyeong-before.json').read_text())
        after=restore_before_c3(json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text()));visual={'palette','ground','deco','obj','high'}
        self.assertEqual({k:v for k,v in before.items() if k not in visual},{k:v for k,v in after.items() if k not in visual})
        expected={(p['x'],p['y']) for p in json.loads(pond_art.LAYOUT.read_text())['placements']};changed=set()
        for layer in ('ground','deco','obj','high'):
            for index,(a,b) in enumerate(zip(before[layer],after[layer])):
                if before['palette'][a]!=after['palette'][b]:changed.add((layer,index%70,index//70))
        self.assertEqual(changed,{('ground',x,y) for x,y in expected});self.assertEqual(len(expected),22)
        for path in (ROOT/'Assets/PocketCaravan/Maps').glob('*.json'):
            if path.name!='Gaegyeong.json':self.assertFalse(any(p.startswith('pond_') for p in json.loads(path.read_text()).get('palette',[])),path.name)

    def test_apply_rejects_changed_dimensions_or_non_water_site(self):
        wrong=SimpleNamespace(id='Gaegyeong',w=71,h=60)
        with self.assertRaisesRegex(ValueError,'rebinding'):pond_art.apply([wrong])
        dry=SimpleNamespace(id='Gaegyeong',w=70,h=60,water=[[False]*70 for _ in range(60)],blocked=[[False]*70 for _ in range(60)])
        with self.assertRaisesRegex(ValueError,'blocked water'):pond_art.apply([dry])
        other=SimpleNamespace(id='Naru')
        self.assertIs(pond_art.apply([other])[0],other)

    def test_production_catalogue_approved_scope_is_pond_without_ripple_animation(self):
        c=json.loads(pond_art.CATALOGUE.read_text());a=c['assets'][0]
        self.assertEqual(a['review']['status'],'approved');self.assertEqual(a['purpose'],'capital_pond')
        self.assertTrue((ROOT/a['review']['evidence']).is_file())
        self.assertTrue({'center','edge','outer_corner','inner_corner'}<=set(a['pieces']))
        self.assertFalse(any(name.startswith('ripple') for name in a['pieces']))

if __name__=='__main__':unittest.main()
