"""Port sea context stays visual-only and reapplication preserves every object/ID."""
import json,sys,copy,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import sys
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from t3_history import map_at,resource_at
import port_water
class PortWater(unittest.TestCase):
 def test_approved_binding_preserves_walkability_and_every_other_field(self):
  for mid,sea,ripples in [('transit_southport',1128,0),('transit_ferry',727,20)]:
   before=json.loads((ROOT/'ArtReview/PortWater-Task6-20261104/Before'/(mid+'.json')).read_text());after,report=port_water.attach(before)
   self.assertEqual(after,map_at(ROOT/'Assets/PocketCaravan/Maps'/(mid+'.json')))
   self.assertEqual((len(report['seaCells']),len(report['rippleObjects'])),(sea,ripples))
   for key in before:
    if key not in ('ground','objects'):self.assertEqual(before[key],after[key],(mid,key))
   for x,y in report['seaCells']:
    i=y*before['w']+x;self.assertEqual(before['ground'][i],0);self.assertTrue(before['blocked'][i]);self.assertEqual(after['palette'][after['ground'][i]],'pond_center')
   objs=copy.deepcopy(before['objects'])
   for i in report['rippleObjects']:self.assertEqual(objs[i]['sheet'],'ripples');objs[i]['sheet']='goryeo_ripples'
   self.assertEqual(objs,after['objects'])
 def test_binding_is_idempotent_and_other_maps_are_untouched(self):
  for mid in ['transit_southport','transit_ferry','Naru']:
   data=map_at(ROOT/'Assets/PocketCaravan/Maps'/(mid+'.json'));after,r=port_water.attach(data);self.assertEqual(data,after);self.assertEqual(r['seaCells'],[]);self.assertEqual(r['rippleObjects'],[])
 def test_historical_normalization_only_undoes_the_exact_approved_visuals(self):
  mid='transit_ferry';data=map_at(ROOT/'Assets/PocketCaravan/Maps'/(mid+'.json'));before=json.loads((ROOT/'ArtReview/PortWater-Task6-20261104/Before'/(mid+'.json')).read_text())
  self.assertEqual(port_water.restore_for_historical_checks(data),before)
  changed=copy.deepcopy(data);changed['name']='unapproved edit';self.assertEqual(port_water.restore_for_historical_checks(changed)['name'],'unapproved edit')
  _,report=port_water.attach(before);x,y=report['seaCells'][0];changed=copy.deepcopy(data);changed['ground'][y*changed['w']+x]=0
  with self.assertRaises(ValueError):port_water.restore_for_historical_checks(changed)
  changed=copy.deepcopy(data);changed['objects'][report['rippleObjects'][0]]['fps']+=.1
  with self.assertRaises(ValueError):port_water.restore_for_historical_checks(changed)
if __name__=='__main__':unittest.main()
