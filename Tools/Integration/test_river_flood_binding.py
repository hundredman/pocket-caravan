import copy,json,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MapGen'))
try:   # GitHub 검사(content-check.yml)는 Pillow만 깐다. numpy·scipy가 없으면 건너뛴다.
    from river_flood_binding import attach,ID
    HAVE=True
except ImportError:HAVE=False
@unittest.skipUnless(HAVE,'numpy/scipy not installed')
class FloodBinding(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.source=json.loads((ROOT/'Assets/PocketCaravan/Maps/transit_wetroad.json').read_text())
 def test_binding_preserves_existing_fields_and_ids(self):
  source=copy.deepcopy(self.source);bound=attach(source)
  for key in source:self.assertEqual(bound[key],source[key],key)
  self.assertEqual(source,self.source);self.assertEqual(attach(bound),bound)
  placement=bound['terrainFloods'][0];self.assertEqual(placement['id'],ID);self.assertEqual(placement['definitionId'],'river_flood')
  self.assertEqual(sum(c['dryGrade']>0 and c['wetGrade']==0 for c in placement['cells']),22)
  self.assertEqual(sum(not c['dryBlocked'] and c['wetBlocked']for c in placement['cells']),62)
 def test_rendered_pair_matches_approved_rgba(self):
  from e3_flood_terrain import render
  from PIL import Image
  # 승인 그림 뒤에 덧붙인 생활 보강(goryeo_life.py: 이정표 칸)은 기록대로 되돌린 뒤 비교한다(범람 칸과 겹치지 않음).
  source=copy.deepcopy(self.source);life=ROOT/'Tools/WorldLayout/goryeo_life_applied.json'
  for c in (json.loads(life.read_text()) if life.exists() else {}).get(source['id'],[]):
   i=c['y']*source['w']+c['x'];source['obj'][i]=source['palette'].index(c['obj']) if c['obj'] else 0
   source['blocked'][i]=c['blocked'];source['reachable'][i]=c['reachable']
  data=attach(source);wet=copy.deepcopy(data)
  for cell in data['terrainFloods'][0]['cells']:
   i=cell['y']*data['w']+cell['x']
   for layer,key in [('ground','wetGround'),('deco','wetDeco')]:
    name=cell[key]
    if name not in wet['palette']:wet['palette'].append(name)
    wet[layer][i]=wet['palette'].index(name)
   wet['blocked'][i]=int(cell['wetBlocked']);wet['road'][i]=cell['wetGrade']
  for stage,value in [('normal',data),('flood',wet)]:
   with Image.open(ROOT/'ArtReview/E3-FloodTerrain-20261103'/(stage+'-map.png'))as approved:
    self.assertEqual(render(value).tobytes(),approved.convert('RGBA').tobytes(),stage)
 def test_export_entry_point_regenerates_metadata_without_changing_live_map(self):
  import tempfile,subprocess
  source=ROOT/'Assets/PocketCaravan/Maps/transit_wetroad.json';before=source.read_bytes()
  base=ROOT/'Assets/PocketCaravan/Editor/B2Preview';base.mkdir(parents=True,exist_ok=True)
  with tempfile.TemporaryDirectory(prefix='flood-test-',dir=base)as folder:
   subprocess.run([sys.executable,str(ROOT/'Tools/WorldLayout/m4_export.py'),'--bind-approved-flood','--preview-root',folder],check=True,capture_output=True)
   generated=json.loads((Path(folder)/'Maps/transit_wetroad.json').read_text())
   expected=attach(self.source);expected['previewOnly']=True
   self.assertEqual(generated,expected);self.assertTrue(generated['previewOnly']);self.assertEqual(source.read_bytes(),before)
 def test_other_maps_unchanged(self):
  for path in (ROOT/'Assets/PocketCaravan/Maps').glob('*.json'):
   data=json.loads(path.read_text())
   if data['id']!='transit_wetroad':self.assertEqual(attach(data),data)
 def test_conflicting_placements_and_changed_geometry_rejected(self):
  for change in [dict(obstacles=[dict(definitionId='river_flood')]),dict(terrainFloods=[dict(id='different')]),dict(w=79),dict(terrainX=87)]:
   data=copy.deepcopy(self.source);data.update(change)
   with self.assertRaises(ValueError):attach(data)
  with self.assertRaises(ValueError):attach(self.source,'not_managed')
if __name__=='__main__':unittest.main()
