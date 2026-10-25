"""Decisions10 alpha exceptions, complete baked placement coverage and unchanged production export."""
import copy,hashlib,json,sys,tempfile,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'));sys.path.insert(0,str(ROOT/'Tools/ArtBake'))
from asset_manifest import validate_catalogue,validate_map
import frozen_art
from verify_frozen import verify

class AlphaPolicy(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
        Image.new('RGBA',(2,2),(30,40,50,127)).save(self.root/'effect.png')
        (self.root/'decision.md').write_text('Approved existing alpha fixture')
        self.asset=dict(id='test',purpose='shadow',kind='object',size=[1,1],image='effect.png',profile='native',source=dict(kind='authored',path='effect.png'),review=dict(status='approved',evidence='decision.md'),tiles=[dict(dx=0,dy=0,layer='deco',tile='shadow',image='effect.png')])
        self.doc=dict(schemaVersion=1,profiles=dict(native=dict(pixelsPerCell=[2,2],binaryAlpha=True)),assets=[self.asset])
    def codes(self):return {e['code'] for e in validate_catalogue(self.doc,self.root)[0]}
    def lock(self):
        p=self.root/'Assets/PocketCaravan/Art/Baked/legacy-alpha-exceptions.json';p.parent.mkdir(parents=True,exist_ok=True)
        p.write_text(json.dumps(dict(schemaVersion=1,decision='decision.md',files=[dict(path='effect.png',sha256=hashlib.sha256((self.root/'effect.png').read_bytes()).hexdigest())])))
        return p
    def test_ordinary_new_asset_cannot_have_partial_alpha(self):self.assertIn('image_alpha',self.codes())
    def test_effect_is_allowed_and_remains_a_whole_placement(self):
        self.asset['kind']='effect';errors,assets=validate_catalogue(self.doc,self.root);self.assertEqual(errors,[])
        m=dict(assetPlacementVersion=1,w=1,h=1,palette=['','shadow'],ground=[0],deco=[1],obj=[0],high=[0],assetPlacements=[dict(id='one',assetId='test',purpose='shadow',x=0,y=0)])
        self.assertEqual(validate_map(m,assets),[])
        m['deco'][0]=0;self.assertIn('incomplete_placement',{e['code']for e in validate_map(m,assets)})
    def test_frozen_ordinary_file_is_allowed_but_one_pixel_change_is_rejected(self):
        self.lock();self.assertEqual(self.codes(),set())
        with Image.open(self.root/'effect.png')as source:im=source.convert('RGBA')
        im.putpixel((0,0),(31,40,50,127));im.save(self.root/'effect.png')
        self.assertIn('frozen_alpha_hash',self.codes())
        self.asset['kind']='effect';self.assertIn('frozen_alpha_hash',self.codes())
    def test_exception_cannot_be_reused_for_another_path(self):
        self.lock();(self.root/'other.png').write_bytes((self.root/'effect.png').read_bytes())
        self.asset['image']='other.png';self.asset['tiles'][0]['image']='other.png';self.assertIn('image_alpha',self.codes())
    def test_effect_does_not_bypass_size_palette_or_profile_validation(self):
        self.asset['kind']='effect';self.doc['profiles']['native']['pixelsPerCell']=[3,3]
        self.doc['profiles']['native']['palette']=['#ffffff'];codes=self.codes();self.assertTrue({'image_size','image_palette'}<=codes)
        self.doc['profiles']['native']['binaryAlpha']=False;self.assertIn('profile_alpha',self.codes())
    def test_malformed_missing_and_outside_root_exception_records_fail(self):
        p=self.lock()
        for data in ([],dict(schemaVersion=1,decision='decision.md',files=[None]),dict(schemaVersion=1,decision='decision.md',files=[dict(path='../effect.png',sha256='0'*64)])):
            with self.subTest(data=data):p.write_text(json.dumps(data));self.assertIn('alpha_registry',self.codes())
        p.write_text(json.dumps(dict(schemaVersion=1,decision='missing.md',files=[])));self.assertIn('alpha_registry',self.codes())
    def test_surface_patterns_require_center_without_invented_boundary_pieces(self):
        self.asset['kind']='modular';self.asset['moduleType']='surface_pattern';self.asset['requiredPieces']=['variant'];self.asset['pieces']={'variant':{k:self.asset[k]for k in ('size','image','tiles')}}
        self.assertIn('modular_minimum',self.codes())

class FrozenProduction(unittest.TestCase):
    @classmethod
    def setUpClass(cls):cls.report=verify()
    def test_full_export_has_zero_live_extraction_or_color_calls(self):
        self.assertEqual([self.report[k]for k in ('cropCalls','recolorCalls','toneCalls')],[0,0,0])
        self.assertEqual((len(self.report['maps']),sum(m['cells']for m in self.report['maps'])),(11,45000))
        self.assertEqual((self.report['catalogueAssets'],self.report['renderTileImages'],self.report['visualFrames']),(669,654,579))
        self.assertTrue(self.report['resourcesByteIdentical'])
    def test_registered_objects_cannot_be_exported_as_fragments_or_wrong_purpose(self):
        from world_registry import atlas_content
        entries,maps=atlas_content();m=maps[0];data=json.loads((ROOT/entries[0]['jsonPath']).read_text());bound=frozen_art.validate_export(m,data)
        first=next(p for p in bound['assetPlacements']if frozen_art.catalogue()[p['assetId']]['kind']=='object')
        definition=frozen_art.catalogue()[first['assetId']];tile=definition['tiles'][0];slot=(first['y']+tile['dy'])*m.w+first['x']+tile['dx']
        broken=copy.deepcopy(bound);broken[tile['layer']][slot]=0
        self.assertIn('incomplete_placement',{e['code']for e in validate_map(broken,frozen_art.catalogue())})
        broken=copy.deepcopy(bound);broken['assetPlacements'][0]['purpose']='other_purpose'
        self.assertIn('wrong_purpose',{e['code']for e in validate_map(broken,frozen_art.catalogue())})
    def test_every_serialized_scene_game_png_is_registered(self):
        doc=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/catalogue.json').read_text());images=set()
        for asset in doc['assets']:
            for shape in asset['pieces'].values()if asset['kind']=='modular'else[asset]:
                images.add(shape['image']);images.update(t['image']for t in shape['tiles'])
        dependencies=json.loads((ROOT/'Docs/Validation/C6-Production-2026-10-26/scene-image-dependencies.json').read_text())['pngs']
        editor_icon='Packages/com.unity.render-pipelines.universal/Editor/2D/Resources/PixelPerfectCamera.png'
        self.assertEqual(set(dependencies)-images,{editor_icon})

    def test_unknown_tile_never_falls_back_to_source_extraction(self):
        with self.assertRaisesRegex(ValueError,'Unbaked tile'):frozen_art.tile('floor:999:999')
    def test_cleanup_preserves_save_aliases_and_registered_files(self):
        report=json.loads((ROOT/'Docs/Validation/C6-Production-2026-10-26/cleanup-plan.json').read_text());self.assertEqual(report['removedCount'],32)
        for r in report['files']:self.assertFalse((ROOT/r['path']).exists())
        aliases=json.loads((ROOT/'Assets/PocketCaravan/Resources/GoryeoLayout.json').read_text())['aliases'];self.assertEqual(len(aliases),8)
        for p in ('maps.py','gaegyeong.py','frontier.py','goryeo_roads.py','settlement_details.py'):
            self.assertNotRegex((ROOT/'Tools/MapGen'/p).read_text(),r'villager_[a-z]+_tall')

if __name__=='__main__':unittest.main()
