"""Approved fair sprites keep the everyday layout and affect only the two existing east fair maps."""
import hashlib,json,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
class FairArt(unittest.TestCase):
 def test_approved_whole_pixels_and_unchanged_footprint(self):
  spec=json.loads((ROOT/'Assets/PocketCaravan/Data/Goryeo/FairLayout.json').read_text())
  approval=json.loads((ROOT/spec['reviewSource']).read_text())
  self.assertEqual(spec['status'],'approved');self.assertEqual(approval['status'],'approved')
  self.assertEqual(spec['mapIds'],['transit_woodtown','transit_ferry']);self.assertEqual(spec['mapIds'],approval['mapIds']);self.assertEqual(spec['target'],'grain_stall')
  rows={r['asset']:r for r in approval['variants']}
  self.assertEqual({a['id']for a in spec['assets']},{'fair_indigo','fair_teal'})
  for a in spec['assets']:
   with Image.open(ROOT/a['image'])as im,Image.open(ROOT/'ArtReview/CentralFair-Task4-20261104/Native'/(a['id']+'.png'))as ref:
    im=im.convert('RGBA');ref=ref.convert('RGBA');self.assertEqual(im.size,(48,48));self.assertEqual(im.tobytes(),ref.tobytes());self.assertLessEqual(set(im.getchannel('A').getdata()),{0,255});self.assertEqual(hashlib.sha256(im.tobytes()).hexdigest(),rows[a['id']]['rgbaSha256']);self.assertEqual(a['size'],[3,3])
 def test_only_existing_two_fair_scenes_gain_component(self):
  guid=(ROOT/'Assets/PocketCaravan/Scripts/Map/GoryeoFairDressing.cs.meta').read_text().split('guid: ')[1].splitlines()[0]
  expected={'transit_woodtown','transit_ferry'};actual=set()
  for reg in (ROOT/'Assets/PocketCaravan/World/Regions').glob('*.json'):
   for m in json.loads(reg.read_text())['maps']:
    scene=(ROOT/m['scenePath']).read_text()
    if 'guid: '+guid in scene:actual.add(m['mapId']);self.assertNotIn('previewOnly: 1',scene)
  self.assertEqual(actual,expected)
if __name__=='__main__':unittest.main()
