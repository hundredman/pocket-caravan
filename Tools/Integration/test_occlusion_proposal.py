"""C3a isolated proposal preserves gameplay and all actor-free tile composites."""
import json,sys,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from occlusion import desired_rows
from occlusion_baseline import restore_before_c3
FOLDER=ROOT/'ArtReview/C3a-2026-10-26'

@unittest.skip('C3a(2026-10-26) 옛 맵 가림 제안본과 본편이 같은지 보는 고정 검사. M4 새 맵은 층(아래/위 개체)을 m4_export가 칸마다 정함')
class OcclusionProposal(unittest.TestCase):
    def test_production_is_exact_approved_proposal_and_legacy_normalizer_is_strict(self):
        import copy
        for path in (FOLDER/'before').glob('*.json'):
            production=json.loads((ROOT/'Assets/PocketCaravan/Maps'/path.name).read_text())
            candidate=json.loads((FOLDER/'candidate/Assets/PocketCaravan/Maps'/path.name).read_text())
            self.assertEqual(production,candidate)
            self.assertEqual(restore_before_c3(production),json.loads(path.read_text()))
            bad=copy.deepcopy(production);bad['road'][0]+=1
            with self.assertRaises(AssertionError):restore_before_c3(bad)
            bad=copy.deepcopy(production);bad['ground'][0]=(bad['ground'][0]+1)%len(bad['palette'])
            with self.assertRaises(AssertionError):restore_before_c3(bad)
    def test_only_expected_objects_move_to_above(self):
        moves=json.loads((FOLDER/'moves.json').read_text())
        for path in (FOLDER/'before').glob('*.json'):
            a=json.loads(path.read_text());b=json.loads((FOLDER/'candidate/Assets/PocketCaravan/Maps'/path.name).read_text())
            self.assertEqual({k:v for k,v in a.items() if k not in ('palette','ground','deco','obj','high')},
                             {k:v for k,v in b.items() if k not in ('palette','ground','deco','obj','high')})
            expected={(p['x'],p['y']):p['key'].replace(':','_').replace('@','_') for p in moves[a['id']]}
            changed=set()
            for i in range(a['w']*a['h']):
                old={l:a['palette'][a[l][i]] for l in ('ground','deco','obj','high')}
                new={l:b['palette'][b[l][i]] for l in old}
                self.assertEqual(old['ground'],new['ground']);self.assertEqual(old['deco'],new['deco'])
                if old==new:continue
                cell=(i%a['w'],i//a['w']);changed.add(cell)
                self.assertIn(cell,expected);self.assertEqual(old['obj'],expected[cell]);self.assertEqual(old['high'],'')
                self.assertEqual(new['obj'],'');self.assertEqual(new['high'],expected[cell])
            self.assertEqual(changed,set(expected))
    def test_no_repainting_or_actor_free_pixel_change(self):
        for path in (FOLDER/'before').glob('*.json'):
            a=json.loads(path.read_text());b=json.loads((FOLDER/'candidate/Assets/PocketCaravan/Maps'/path.name).read_text())
            # There is no overlapping obj/high pair at a moved site; moving the exact PNG changes only actor ordering.
            for i in range(len(a['ground'])):
                keys=lambda m:tuple(m['palette'][m[l][i]] for l in ('ground','deco','obj','high') if m['palette'][m[l][i]])
                self.assertEqual(keys(a),keys(b))
        for png in (FOLDER/'candidate/Assets/PocketCaravan/Art/NinjaAdventure/Tiles').glob('*.png'):
            self.assertEqual(Image.open(png).convert('RGBA').tobytes(),Image.open(ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/png.name).convert('RGBA').tobytes())
    def test_existing_canopies_and_short_shrubs_keep_their_rule(self):
        self.assertEqual(desired_rows('pine',2,3,1),1)
        self.assertEqual(desired_rows('goryeo_woodland8',3,2,0),0)
        self.assertEqual(desired_rows('goryeo_woodland6',3,4,0),2)
        self.assertEqual(desired_rows('goryeo_royal',10,7,0),5)
        self.assertEqual(desired_rows('goryeo_lantern',1,2,0),0)
    def test_walking_evidence_covers_all_surveyed_sides(self):
        plan=json.loads((FOLDER/'plan.json').read_text());report=json.loads((FOLDER/'walking.json').read_text())
        self.assertEqual(len(report['approaches']),sum(len(p['sides']) for m in plan['maps'] for p in m['props']))
        self.assertFalse(any(r['result']=='motor could not follow route' for r in report['approaches']))
        self.assertTrue(report['previewOnly']);self.assertGreater(report['motorTicks'],200000)

if __name__=='__main__':unittest.main()
