"""The runtime guide must follow the generated map frame and stay reachable."""
import json,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
class IntroAnchor(unittest.TestCase):
 def test_original_world_cell_and_current_dimensions_match(self):
  source=json.loads((ROOT/'Tools/WorldLayout/goryeo_intro.json').read_text())
  anchor=json.loads((ROOT/'Assets/PocketCaravan/Resources/Goryeo/Intro.json').read_text())
  paths=list((ROOT/'Assets/PocketCaravan/Maps').rglob(anchor['mapId']+'.json'));self.assertEqual(len(paths),1)
  d=json.loads(paths[0].read_text())
  self.assertEqual((anchor['width'],anchor['height']),(d['w'],d['h']))
  self.assertEqual((d['terrainX']+anchor['x'],d['terrainY']+anchor['y']),(source['worldX'],source['worldY']))
  self.assertEqual(anchor['mapId'],source['mapId'])
 def test_guide_and_adjacent_approach_are_on_main_walkable_land(self):
  a=json.loads((ROOT/'Assets/PocketCaravan/Resources/Goryeo/Intro.json').read_text())
  d=json.loads(next((ROOT/'Assets/PocketCaravan/Maps').rglob(a['mapId']+'.json')).read_text())
  i=a['y']*d['w']+a['x'];self.assertFalse(d['blocked'][i]);self.assertTrue(d['reachable'][i]);self.assertFalse(d['water'][i])
  # A clear adjacent cell is necessary for the 1.8-cell interaction radius.
  approaches=[(a['x']+dx,a['y']+dy)for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))]
  self.assertTrue(any(0<=x<d['w']and 0<=y<d['h']and not d['blocked'][y*d['w']+x]and d['reachable'][y*d['w']+x]for x,y in approaches))
if __name__=='__main__':unittest.main()
