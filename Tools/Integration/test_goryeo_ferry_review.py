import importlib.util,json,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('ferry_trial',ROOT/'Tools/WorldLayout/goryeo_ferry_trial.py');trial=importlib.util.module_from_spec(spec);spec.loader.exec_module(trial)
class FerryReview(unittest.TestCase):
 def test_review_routes_reproduce_existing_people_and_walkable_landings(self):
  source=json.loads((ROOT/'ArtReview/Goryeo-Ferry-20261105/source.json').read_text());self.assertEqual(trial.build(),source);self.assertEqual(len(source['ports']),3)
  for p in source['ports']:
   m=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{p['mapId']}.json").read_text());self.assertEqual((m['w'],m['h']),(p['width'],p['height']));self.assertTrue(any(o['kind']=='talker'and o['name']=='뱃사공'and(o['x'],o['y'])==(p['npcX'],p['npcY'])for o in m['objects']));self.assertFalse(any(o['kind']=='exit'and(o['x'],o['y'])==(p['arrivalX'],p['arrivalY'])for o in m['objects']))
 def test_economic_proposal_is_still_review_only(self):
  s=trial.build();self.assertEqual(s['status'],'review');self.assertEqual((s['fare'],s['hours']),(8,1));self.assertEqual(len(s['walkingSeconds']),3);self.assertTrue(all(v['seconds']>0 for v in s['walkingSeconds']));self.assertFalse((ROOT/'Assets/PocketCaravan/Resources/Goryeo/Ferry').exists())
if __name__=='__main__':unittest.main()
