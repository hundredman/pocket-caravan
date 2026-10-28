"""C6 feasibility evidence is pixel exact and does not silently weaken A2."""
import hashlib,json,sys,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from asset_manifest import validate_catalogue
FOLDER=ROOT/'ArtReview/C6-Preflight-2026-10-26'

class C6Preflight(unittest.TestCase):
    def test_whole_objects_preserve_every_source_tile_including_alpha(self):
        doc=json.loads((FOLDER/'catalogue-draft.json').read_text())
        self.assertEqual(len(doc['assets']),46)
        for asset in doc['assets']:
            with Image.open(ROOT/asset['image']) as whole:
                self.assertEqual(whole.size,(asset['size'][0]*16,asset['size'][1]*16))
                self.assertEqual(len(asset['tiles']),asset['size'][0]*asset['size'][1])
                for tile in asset['tiles']:
                    x,y=tile['dx']*16,tile['dy']*16
                    with Image.open(ROOT/tile['image']) as source:
                        self.assertEqual(whole.convert('RGBA').crop((x,y,x+16,y+16)).tobytes(),source.convert('RGBA').tobytes())
    def test_existing_alpha_conflict_is_real_and_no_other_catalogue_errors_are_hidden(self):
        doc=json.loads((FOLDER/'catalogue-draft.json').read_text())
        legacy={}
        exec((ROOT/'Docs/Validation/C6-Production-2026-10-26/LegacyA2.py.txt').read_text(),legacy)
        errors,_=legacy['validate_catalogue'](doc,ROOT)
        self.assertEqual(len(errors),7);self.assertEqual({e['code'] for e in errors},{'image_alpha'})
        report=json.loads((FOLDER/'preflight.json').read_text())
        self.assertEqual(report['draftCatalogueErrors'],errors)
        self.assertEqual((report['stampPlacements'],report['wholeObjectTileKeys'],report['allTerrainAndGrowthTileKeys']),(634,496,654))
        self.assertEqual(len(report['semiTransparentTerrainTiles']),5)
        self.assertEqual(sum(bool(v['nonBinaryAlpha']) for v in report['visualObjects']),5)
        self.assertFalse(report['productionChanged'])
    @unittest.skip('C6 당시 본편 입력 파일 해시 고정. M4 본편 전환으로 입력이 바뀜. 옛 저장 별칭은 test_m4_production(SaveMigration)이 확인')
    def test_production_inputs_and_old_save_aliases_stay_intact(self):
        inputs=json.loads((FOLDER/'input-hashes.json').read_text())
        for path,expected in inputs.items():self.assertEqual(hashlib.sha256((ROOT/path).read_bytes()).hexdigest(),expected,path)
        report=json.loads((FOLDER/'preflight.json').read_text())
        aliases={a['oldId']:a['newId'] for a in json.loads((ROOT/'Assets/PocketCaravan/Resources/GoryeoLayout.json').read_text())['aliases']}
        self.assertEqual(len(report['unusedMaps']),8)
        for row in report['unusedMaps']:self.assertIn(row['map'],aliases)

if __name__=='__main__':unittest.main()
