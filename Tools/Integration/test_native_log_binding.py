"""Approved art preservation, exact occupied cells, and preview-only placement safety."""
import json,sys,unittest,hashlib
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/WorldLayout'));sys.path.insert(0,str(ROOT/'Tools/MapGen'))
import native_log_binding as logs
from collections import deque
from asset_manifest import validate_catalogue
class NativeLogs(unittest.TestCase):
 def test_approved_art_and_whole_object_catalogue(self):
  errors,_=validate_catalogue(json.loads((logs.ART/'catalogue.json').read_text()),ROOT);self.assertEqual(errors,[])
  approval=json.loads((logs.REVIEW/'approval.json').read_text())
  for a in approval['assets']:
   image=Image.open(logs.ART/(a['name']+'.png')).convert('RGBA');self.assertEqual(image.size,(48,48));self.assertEqual(hashlib.sha256(image.tobytes()).hexdigest(),a['decodedRGBAsha256'])
 def test_collision_is_only_tiles_with_visible_pixels(self):
  data=json.loads((ROOT/'Assets/PocketCaravan/Data/Goryeo/LogArt.json').read_text());self.assertEqual(data['status'],'approved');self.assertEqual(len(data['assets']),4)
  for a in data['assets']:
   image=Image.open(ROOT/a['image']).convert('RGBA');expected={'horizontal':{(0,0),(1,0),(2,0),(1,1),(2,1)},'diagonal_ne':{(0,0),(1,0),(1,1),(2,1),(2,2)},'diagonal_nw':{(0,2),(0,1),(1,1),(1,0),(2,0)},'vertical':{(1,0),(1,1),(1,2)}}[a['variant']]
   actual={(c['x'],c['y'])for c in a['occupied']};self.assertEqual(actual,expected);self.assertLess(len(actual),9);self.assertTrue(actual)
 def test_candidates_preserve_current_map_and_cannot_apply_elsewhere(self):
  source=json.loads((ROOT/'Assets/PocketCaravan/Maps/transit_south_inland.json').read_text())
  for n in logs.NAMES:
   data=logs.candidate(source,n);self.assertTrue(data['previewOnly']);self.assertNotIn('nativeLogs',source)
   for k in source:
    if k!='previewOnly':self.assertEqual(data[k],source[k])
   self.assertEqual(data,json.loads((logs.OUT/(n+'.json')).read_text()));self.assertEqual(data['obstacles'][0]['definitionId'],'fallen_log');self.assertEqual(data['nativeLogs'][0]['variant'],n)
  with self.assertRaises(ValueError):logs.candidate(dict(source,id='Naru'))
  with self.assertRaises(ValueError):logs.candidate(logs.candidate(source))
  with self.assertRaises(ValueError):logs.candidate(source,'unknown')
 def test_revised_forest_candidates_close_an_existing_corridor_without_terrain_edits(self):
  spec=json.loads((logs.OUT/'revised-layout.json').read_text());arts=json.loads((ROOT/'Assets/PocketCaravan/Data/Goryeo/LogArt.json').read_text())['assets']
  for row in spec['options']:
   data=logs.revised_candidates()[row['name']];self.assertEqual(data,json.loads((logs.OUT/(row['name']+'.json')).read_text()));before=json.loads((ROOT/'Assets/PocketCaravan/Maps'/(row['mapId']+'.json')).read_text())
   for k in before:
    if k!='previewOnly':self.assertEqual(data[k],before[k])
   w,h=data['w'],data['h'];walk={(x,y)for y in range(h)for x in range(w)if data['ground'][y*w+x]and not data['blocked'][y*w+x]};art=next(a for a in arts if a['variant']==row['variant']);mask={(row['x']+c['x'],row['y']+2-c['y'])for c in art['occupied']}
   a,b=tuple(row['a']),tuple(row['b'])
   def connected(blocked):
    q=deque([a]);seen={a}
    while q:
     p=q.popleft()
     if p==b:return True
     for dx,dy in [(0,1),(0,-1),(1,0),(-1,0)]:
      n=p[0]+dx,p[1]+dy
      if n not in seen and n in walk and n not in blocked and abs(n[0]-row['x']-1)<=7 and abs(n[1]-row['y']-1)<=7:seen.add(n);q.append(n)
    return False
   self.assertTrue(mask<=walk);self.assertTrue(connected(set()));self.assertFalse(connected(mask))
 def test_production_binding_is_approved_idempotent_and_metadata_only(self):
  before=json.loads((logs.OUT/'production-source-before.json').read_text());actual=json.loads((ROOT/'Assets/PocketCaravan/Maps/transit_central_approach.json').read_text());expected=logs.bind_approved(before)
  self.assertEqual(expected,actual);self.assertEqual(logs.bind_approved(actual),actual);self.assertEqual(logs.restore_for_historical_checks(actual),before)
  for key in before:self.assertEqual(actual[key],before[key])
  self.assertEqual(set(actual)-set(before),{'obstacles','nativeLogs'});self.assertEqual(actual['nativeLogs'][0]['variant'],'diagonal_nw');self.assertFalse(actual.get('previewOnly',False))
  changed=json.loads(json.dumps(actual));changed['nativeLogs'][0]['variant']='vertical'
  with self.assertRaises(ValueError):logs.bind_approved(changed)
  with self.assertRaises(ValueError):logs.restore_for_historical_checks(changed)
  naru=json.loads((ROOT/'Assets/PocketCaravan/Maps/Naru.json').read_text());self.assertEqual(logs.bind_approved(naru),naru)
if __name__=='__main__':unittest.main()
