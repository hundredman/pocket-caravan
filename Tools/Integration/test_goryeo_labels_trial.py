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
 def test_labels_keep_scope_and_common_interaction_logic(self):
  code=(ROOT/'Assets/PocketCaravan/Scripts/Goryeo/GoryeoPlaceLabels.cs').read_text();self.assertNotIn('RuntimeInitializeOnLoadMethod',code);self.assertIn('map.previewOnly||approvedForProduction',code);self.assertIn('original.enabled=originalEnabled',code);self.assertNotIn('.Interact(',code);self.assertNotIn('.DisplayName =',code)
  production=json.loads((ROOT/'Assets/PocketCaravan/Resources/Goryeo/PlaceLabels.json').read_text());source=json.loads((OUT/'source.json').read_text());approval=json.loads((OUT/'approval.json').read_text());self.assertEqual(production['status'],'approved');self.assertEqual(approval['userReply'],'A5 이대로 승인');self.assertEqual(production['near'],4);self.assertEqual(len(production['maps']),13)
  for a,b in zip(production['maps'],source['maps']):
   for k in ('id','w','h'):self.assertEqual(a[k],b[k])
   self.assertEqual(a['places'],[{k:p[k]for k in ('text','x','y')}for p in b['places']])
  prompt=code[code.index('            // 2) 말 걸기 안내'):code.index('            // 3) 퀘스트 길 안내')]
  self.assertIn('player != null && player.TalkTarget != null && !UIState.Blocking',prompt)
  self.assertIn('compactPrompt=DrawSpace(cam,player.TalkTarget);',prompt)
  self.assertNotIn('Interact(',prompt)
  common=(ROOT/'Assets/PocketCaravan/Scripts/UI/WorldLabels.cs').read_text()
  start='            // 3) 퀘스트 길 안내';end='        static Vector2 ToGui'
  self.assertEqual(code[code.index(start):code.index(end)],common[common.index(start):common.index(end)])
if __name__=='__main__':unittest.main()
