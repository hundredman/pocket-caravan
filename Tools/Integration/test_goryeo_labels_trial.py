import unittest,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A5-20261105'
class LabelsTrial(unittest.TestCase):
 def test_only_existing_whole_sprites_and_bank_anchors(self):
  data=json.loads((OUT/'source.json').read_text());self.assertEqual(data['status'],'review');self.assertEqual(data['near'],4);self.assertEqual(len(data['maps']),13)
  count=0
  for row in data['maps']:
   d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{row['id']}.json").read_text());self.assertEqual((row['w'],row['h']),(d['w'],d['h']))
   for p in row['places']:
    count+=1;self.assertIn(p['text'],('우물','객주','선착장'));self.assertTrue(0<=p['x']<=d['w']and 0<=p['y']<=d['h']);self.assertTrue(p['source'].startswith('sprite:c7:')or p['source'].startswith('existing waterside'))
  self.assertGreater(count,10)
 def test_scoped_preview_no_new_functionality_or_shared_ui_edits(self):
  code=(ROOT/'Assets/PocketCaravan/Scripts/Goryeo/GoryeoPlaceLabels.cs').read_text();self.assertNotIn('RuntimeInitializeOnLoadMethod',code);self.assertIn('map.previewOnly||approvedForProduction',code);self.assertIn('original.enabled=originalEnabled',code);self.assertNotIn('.Interact(',code);self.assertNotIn('.DisplayName =',code)
  self.assertFalse((ROOT/'Assets/PocketCaravan/Resources/Goryeo/PlaceLabels.json').exists())
  # Quest navigation and Space prompts retain the common implementation, after the name loop.
  common=(ROOT/'Assets/PocketCaravan/Scripts/UI/WorldLabels.cs').read_text()
  for start,end in [('            // 2) 말 걸기 안내','            // 3) 퀘스트 길 안내'),('            // 3) 퀘스트 길 안내','        static Vector2 ToGui')]:
   self.assertEqual(code[code.index(start):code.index(end)],common[common.index(start):common.index(end)])
if __name__=='__main__':unittest.main()
