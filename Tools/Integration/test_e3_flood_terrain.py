"""Connected terrain flooding: state geometry and production preservation."""
import json,sys,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
import sys
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from t3_history import map_at,resource_at
try:   # GitHub 검사(content-check.yml)는 Pillow만 깐다. numpy·scipy가 없으면 건너뛴다.
    import numpy as np
    from scipy import ndimage
    from e3_flood_terrain import states,TILES
    HAVE=True
except ImportError:HAVE=False
@unittest.skipUnless(HAVE,'numpy/scipy not installed')
class FloodTerrain(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.dry=map_at(ROOT/'Assets/PocketCaravan/Maps/transit_wetroad.json');cls.wet,cls.report=states(cls.dry)
 def test_expansion_is_joined_to_river_and_submerges_road(self):
  d,a=self.dry,self.wet;h,w=d['h'],d['w'];old=np.array(d['water'],bool).reshape(h,w);wet=np.array(a['water'],bool).reshape(h,w);new=wet&~old
  self.assertTrue(wet[old].all());self.assertEqual(new.sum(),62)
  labels,_=ndimage.label(wet);self.assertTrue(set(labels[new]).issubset(set(labels[old])))
  self.assertEqual(sum(bool(d['road'][i])and bool(a['water'][i])and not bool(d['water'][i])for i in range(w*h)),22)
  for y,x in np.argwhere(new):
   i=y*w+x;self.assertEqual(a['blocked'][i],1);self.assertEqual(a['road'][i],0)
 def test_no_white_shore_inside_merged_water(self):
  a=self.wet;h,w=a['h'],a['w'];mask=np.array(a['water'],bool).reshape(h,w)
  for x,y in self.report['changedCells']:
   if mask[y-1:y+2,x-1:x+2].all():
    with Image.open(TILES/(a['palette'][a['ground'][y*w+x]]+'.png'))as im:
     self.assertEqual(set(im.convert('RGBA').getdata()),{(127,185,175,255)},(x,y))
 def test_ids_objects_canopies_and_outside_footprint_preserved(self):
  d,a=self.dry,self.wet
  for key in d:
   if key not in ['ground','deco','blocked','water','road','palette']:self.assertEqual(d[key],a[key],key)
  changed={y*d['w']+x for x,y in self.report['changedCells']}
  for layer in ['ground','deco','blocked','water','road']:
   self.assertTrue(all(d[layer][i]==a[layer][i]for i in range(d['w']*d['h'])if i not in changed),layer)
 def test_direct_road_segment_cut_and_deterministic(self):
  def connected(d):
   road=np.array(d['road']).reshape(d['h'],d['w'])>0;mask=np.zeros_like(road);mask[14:22,42:47]=road[14:22,42:47];lab,_=ndimage.label(mask)
   return lab[14,44]>0 and lab[14,44]==lab[21,43]
  self.assertTrue(connected(self.dry));self.assertFalse(connected(self.wet))
  wet,rep=states(self.dry);self.assertEqual(wet,self.wet);self.assertEqual(rep,self.report)
if __name__=='__main__':unittest.main()
