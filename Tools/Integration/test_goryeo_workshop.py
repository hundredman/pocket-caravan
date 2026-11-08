"""Approved workshop metadata must be additive, repeatable and safe for existing maps."""
import copy,json,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import goryeo_life as life
class WorkshopPlacement(unittest.TestCase):
 def test_approved_workshop_is_idempotent_and_preserves_every_other_field(self):
  maps={mid:life.Map(path,historical=True)for mid,path in life.regions()};before={mid:copy.deepcopy(m.d)for mid,m in maps.items()}
  life.apply_workshops(maps)
  for mid,m in maps.items():
   for key in before[mid]:
    if key!='objects':self.assertEqual(m.d[key],before[mid][key],(mid,key))
   ordinary=lambda d:[o for o in d['objects']if not str(o.get('id','')).startswith('life_workshop_')]
   self.assertEqual(ordinary(m.d),ordinary(before[mid]))
  w=[o for o in maps['Dakmok'].d['objects']if o['kind']=='workshop'];self.assertEqual(len(w),1)
  self.assertEqual((w[0]['x'],w[0]['y'],w[0]['type'],w[0]['town']),(37,53,'goryeo_hanji','dakmok'))
  self.assertEqual(life.apply_workshops(maps),[])
 def test_moving_approved_position_or_blocking_lane_is_rejected(self):
  old=copy.deepcopy(life.SPEC)
  try:
   for change in [dict(x=38),dict(blocksMovement=True)]:
    life.SPEC=copy.deepcopy(old);life.SPEC['workshops'][0].update(change)
    with self.assertRaises(ValueError):life.apply_workshops({'Dakmok':life.Map(ROOT/'Assets/PocketCaravan/Maps/Dakmok.json',historical=True)})
  finally:life.SPEC=old
if __name__=='__main__':unittest.main()
