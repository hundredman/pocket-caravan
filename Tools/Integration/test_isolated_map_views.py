"""Scene-view ownership changes visibility only, never movement or stored object identity."""
from pathlib import Path
import json,unittest
ROOT=Path(__file__).resolve().parents[2];PREV=ROOT/'Assets/PocketCaravan/Editor/B2Preview/VillageFill/Maps'
@unittest.skipUnless((PREV/'Naru.json').exists(),'Isolated view previews not generated')
class IsolatedMapViews(unittest.TestCase):
 def test_ownership_and_removal_of_extended_backdrops(self):
  for p in PREV.glob('*.json'):
   d=json.loads(p.read_text())
   if 'viewOwnership'not in d:self.skipTest('Isolated view option not generated')
   self.assertEqual(len(d['viewOwnership']),d['w']*d['h'],p.stem)
   self.assertLessEqual(set(d['viewOwnership']),{0,1})
   self.assertTrue(any(d['viewOwnership']),p.stem)
   self.assertFalse(d.get('cameraBackdrop'),p.stem)
   self.assertTrue(all(own or block for own,block in zip(d['viewOwnership'],d['blocked'])),p.stem)
 def test_far_neighbor_visibility_is_actually_reduced(self):
  total=kept=0
  for p in PREV.glob('*.json'):
   d=json.loads(p.read_text());own=d.get('viewOwnership')
   if own is None:self.skipTest('Isolated view option not generated')
   w,h=d['w'],d['h']
   for y in range(h):
    for x in range(w):
     visible=any(0<=x+dx<w and 0<=y+dy<h and own[(y+dy)*w+x+dx]for dx in(-1,0,1)for dy in(-1,0,1))
     count=sum(bool(d[layer][y*w+x])for layer in ['ground','deco','obj','high']);total+=count
     if visible:kept+=count
   # Static rendering arrays remain whole-source art; clipping is scene visibility only.
   self.assertEqual(len(d['ground']),w*h)
  self.assertLess(kept,total)
if __name__=='__main__':unittest.main()
