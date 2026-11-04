"""Art-review contracts: exact native grids, river joins and unchanged gameplay data."""
import json,sys,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/E3-E2-Art-20261103'
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'));sys.path.insert(0,str(ROOT/'Tools/MapGen'))
@unittest.skipUnless((OUT/'catalogue.json').exists(),'E3/E2 artwork not generated')
class E3Art(unittest.TestCase):
 def test_native_catalogue_and_exact_sizes(self):
  from asset_manifest import validate_catalogue
  errors,_=validate_catalogue(json.loads((OUT/'catalogue.json').read_text()),ROOT);self.assertEqual(errors,[])
  sizes={'fallen_log':(48,48),'river_flood_high':(80,48),'river_flood_low':(80,48),'transit_spice':(16,16),'transit_wool':(16,16)}
  for name,size in sizes.items():
   with Image.open(OUT/'Native'/(name+'.png'))as im:
    self.assertEqual(im.size,size);self.assertLessEqual(set(im.convert('RGBA').getchannel('A').getdata()),{0,255})
 def test_flood_matches_original_river_and_recedes(self):
  with Image.open(OUT/'Native/river_flood_high.png')as hi,Image.open(OUT/'Native/river_flood_low.png')as lo:
   hi=hi.convert('RGBA');lo=lo.convert('RGBA')
   for y in range(5,43):
    self.assertEqual(hi.getpixel((79,y)),(127,185,175,255));self.assertEqual(lo.getpixel((79,y)),(127,185,175,255))
   for im in [hi,lo]:
    self.assertTrue(all(im.getpixel((x,0))[3]==im.getpixel((x,47))[3]==0 for x in range(80)))
    self.assertTrue(all(im.getpixel((0,y))[3]==0 for y in range(48)))
    self.assertTrue({(255,255,255,255),(132,103,94,255),(120,160,153,255)}.issubset(set(im.getdata())))
   self.assertGreaterEqual(sum(c[3]>0 for c in hi.getdata()),2700)
   self.assertLess(sum(c[3]>0 for c in lo.getdata()),sum(c[3]>0 for c in hi.getdata()))
   self.assertTrue(all(not lo.getpixel((x,y))[3]or hi.getpixel((x,y))[3]for y in range(48)for x in range(80)))
 def test_sea_context_never_creates_new_land_or_walkable_cells(self):
  import e3_e2_preview
  for name in ['transit_southport','transit_ferry']:
   before=json.loads((ROOT/'ArtReview/PortWater-Task6-20261104/Before'/(name+'.json')).read_text());after,cells=e3_e2_preview.coast_context(before)
   self.assertGreater(len(cells),0)
   for key in before:
    if key!='ground':self.assertEqual(before[key],after[key],key)
   for x,y in cells:
    i=y*before['w']+x;self.assertEqual(before['ground'][i],0);self.assertTrue(before['blocked'][i]);self.assertEqual(after['palette'][after['ground'][i]],'pond_center')
   again,changes=e3_e2_preview.coast_context(after);self.assertEqual(again,after);self.assertEqual(changes,[])
if __name__=='__main__':unittest.main()
