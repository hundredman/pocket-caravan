"""Review-only village dressing: native art, existing functions, event-space separation."""
import contextlib,copy,hashlib,io,json,sys,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/VillageFill-20261030';PREV=ROOT/'Assets/PocketCaravan/Editor/B2Preview/VillageFill'
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
@unittest.skipUnless((OUT/'manifest.json').exists(),'Village dressing review not generated')
class VillageFillReview(unittest.TestCase):
 def setUp(self):self.manifest=json.loads((OUT/'manifest.json').read_text())
 def test_whole_approved_images_retain_pixels_dimensions_and_purpose(self):
  for a in self.manifest['assets']:
   with Image.open(ROOT/a['source'])as im:src=im.convert('RGBA')
   with Image.open(ROOT/a['reviewImage'])as im:dst=im.convert('RGBA')
   self.assertEqual(src.size,dst.size,a['id']);self.assertEqual(dst.size,tuple(x*16 for x in a['size']))
   self.assertEqual(src.tobytes(),dst.tobytes(),a['id']);self.assertEqual(hashlib.sha256(dst.tobytes()).hexdigest(),a['rgbaSha256'])
 def test_pending_hamlet_functions_are_not_invented(self):
  for m in self.manifest['maps']:
   kinds=self.manifest['inputMaps'][m['mapId']]['kinds']
   board=[s for s in m['spaces']if s['id']=='board']
   self.assertEqual(bool(board),'board' in kinds)
   if 'market' not in kinds:
    self.assertFalse(any(p['asset']=='stall'for s in m['spaces']for p in s['props']))
 def test_default_event_yards_have_a_separate_restoreable_group(self):
  for m in self.manifest['maps']:
   yard=next((s for s in m['spaces']if s['id']=='event-yard'),None)
   if yard:self.assertGreaterEqual(len(yard['props']),2,m['village'])
   else:self.assertEqual([s['id']for s in m['spaces']],['signs'])
 def test_added_rice_never_replaces_a_major_forest(self):
  for p in self.manifest['addedRice']:
   self.assertLessEqual(p['removedTrees'],3,p)
   self.assertLessEqual(p['forestBarrierFraction'],.10,p)
 def test_forest_maps_are_restored_to_the_input_art(self):
  if not(PREV/'Maps/Naru.json').exists():self.skipTest('Preview maps not generated')
  for mid in ['goryeo_jangdan','goryeo_songak']:
   d=json.loads((PREV/f'Maps/{mid}.json').read_text())
   layers={layer:[d['palette'][i]for i in d[layer]]for layer in ['ground','deco','obj','high']}
   self.assertEqual(hashlib.sha256(json.dumps(layers,sort_keys=True,separators=(',',':')).encode()).hexdigest(),self.manifest['inputMaps'][mid]['visibleLayersSha256'],mid)
 def test_original_map_shape_and_function_objects_preserved(self):
  if not(PREV/'Maps/Naru.json').exists():self.skipTest('Preview maps not generated')
  digest=lambda o:hashlib.sha256(json.dumps(o,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()).hexdigest()
  for mid,baseline in self.manifest['inputMaps'].items():
   md=json.loads((PREV/f'Maps/{mid}.json').read_text())
   self.assertEqual({k:md[k]for k in baseline['metadata']},baseline['metadata'],mid)
   actual={digest(o)for o in md['objects']}
   self.assertTrue(set(baseline['objectSignatures'])<=actual,mid)
 def test_all_existing_functions_remain_reachable_with_default_collisions(self):
  if not(PREV/'Maps/Naru.json').exists():self.skipTest('Preview maps not generated')
  import m4_reach
  with tempfile.TemporaryDirectory()as td:
   maps={p.stem:json.loads(p.read_text())for p in (PREV/'Maps').glob('*.json')}
   for m in self.manifest['maps']:
    d=maps[m['mapId']];w=d['w']
    for s in m['spaces']:
     for p in s['props']:
      rows=max(1,p['h']-1)if p['asset']=='thatch'else 1
      for dy in range(rows):
       for dx in range(p['w']):d['blocked'][(p['y']+p['h']-1-dy)*w+p['x']+dx]=1
   for mid,d in maps.items():Path(td,f'{mid}.json').write_text(json.dumps(d))
   stream=io.StringIO()
   with patch.object(sys,'argv',['m4_reach','--maps',td]),contextlib.redirect_stdout(stream):result=m4_reach.main()
   self.assertEqual(result,0,stream.getvalue())
if __name__=='__main__':unittest.main()
