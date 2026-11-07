"""T3 physical-world conservation and the live production coordinate binding."""
import sys,json,unittest,copy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
try:
 import t3_install as install
except ModuleNotFoundError as e:
 if e.name not in ('numpy','scipy'):raise
 install=None
@unittest.skipIf(install is None,'Authoring requires NumPy/SciPy')
class Production(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.out,cls.report=install.proposal();cls.base=install.baseline();cls.maps={m:cls.out[p]for m,p in cls.base['paths'].items()}
 def test_every_live_export_reproduces_from_locked_source(self):
  for p,d in self.out.items():self.assertEqual(json.loads((install.BASE/p).read_text()),d,p)
 def test_actual_production_flags_and_exterior_exits(self):
  count=0
  for m,d in self.maps.items():
   self.assertFalse(d['previewOnly']);self.assertEqual(d['boundaryLayoutVersion'],3)
   for o in d['objects']:
    if o['kind']!='exit':continue
    count+=1;self.assertEqual({'W':o['x'],'E':d['w']-o['x']-o['w'],'N':o['y'],'S':d['h']-o['y']-o['h']}[o['edge']],0)
  self.assertEqual(count,635)
 def points(self,rows,key,old,cell=False):
  result=[]
  for row in rows:
   d=(self.base['maps']if old else self.maps)[row['id']]
   for p in row[key]:
    q=copy.deepcopy(p);q['x']=round(d['terrainX']+p['x'],5);q['y']=round(d['terrainY']+d['h']-p['y']-(1 if cell else 0),5)
    if 'bx'in p:q['bx']=round(d['terrainX']+p['bx'],5);q['by']=round(d['terrainY']+d['h']-p['by']-(1 if cell else 0),5)
    result.append(json.dumps(q,sort_keys=True))
  return sorted(result)
 def test_labels_audio_and_life_keep_world_positions_and_policy(self):
  for p,key,cell in [('Resources/Goryeo/PlaceLabels.json','places',False),('Resources/Goryeo/Life/Anchors.json','points',True),('Resources/Goryeo/VillageLife/Anchors.json','points',False)]:
   self.assertEqual(self.points(self.base['resources'][p]['maps'],key,True,cell),self.points(self.out[p]['maps'],key,False,cell),p)
 def test_stride_all_nonempty_materials_stay_at_same_world_cell(self):
  def world(rows,maps):
   return {(maps[r['id']]['terrainX']+i%r['w'],maps[r['id']]['terrainY']+maps[r['id']]['h']-1-i//r['w']):c for r in rows for i,c in enumerate(r['data'])if c!='O'}
  p='Resources/Goryeo/Stride/Surfaces.json';before=world(self.base['resources'][p]['maps'],self.base['maps']);after=world(self.out[p]['maps'],self.maps)
  removed=set(before)-set(after);self.assertEqual(removed,{(2,y)for y in range(-2,2)})
  self.assertEqual({k:v for k,v in before.items()if k not in removed},after)
 def test_whole_market_props_keep_pixels_size_and_world_position(self):
  p='Data/Goryeo/MarketLayout.json'
  def props(layout,maps):
   return sorted((q['asset'],q['w'],q['h'],maps[r['mapId']]['terrainX']+q['x'],maps[r['mapId']]['terrainY']+q['y'])for r in layout['maps']for s in r['spaces']for q in s['props'])
  self.assertEqual(self.base['resources'][p]['assets'],self.out[p]['assets']);self.assertEqual(props(self.base['resources'][p],self.base['maps']),props(self.out[p],self.maps))
 def test_well_ferry_and_seats_keep_approved_physical_location_and_policy(self):
  def at(mid,x,y,old):
   d=(self.base['maps']if old else self.maps)[mid];return d['terrainX']+x,d['terrainY']+y
  p='Resources/Goryeo/Daily/Config.json';a=self.base['resources'][p];b=self.out[p]
  self.assertEqual(at(a['mapId'],a['x'],a['y'],True),at(b['mapId'],b['x'],b['y'],False));self.assertEqual((a['reward'],a['seconds'],a['memoKey']),(b['reward'],b['seconds'],b['memoKey']))
  p='Resources/Goryeo/Ferry/Config.json'
  for a,b in zip(self.base['resources'][p]['ports'],self.out[p]['ports']):
   for k in ('npc','arrival'):self.assertEqual(at(a['mapId'],a[k+'X'],a[k+'Y'],True),at(b['mapId'],b[k+'X'],b[k+'Y'],False))
  p='Resources/Goryeo/Seats/Config.json'
  for a,b in zip(self.base['resources'][p]['seats'],self.out[p]['seats']):
   self.assertEqual(at(a['mapId'],a['x'],a['y'],True),at(b['mapId'],b['x'],b['y'],False));self.assertEqual(a['id'],b['id']);self.assertEqual(a['lift'],b['lift'])
 def test_frozen_legacy_aliases_and_safe_recovery_are_complete(self):
  self.assertEqual(self.base['resources']['Resources/GoryeoLayout.json']['aliases'],self.out['Resources/GoryeoLayout.json']['aliases'])
  for f in self.out['Resources/Goryeo/BoundaryLayout.json']['maps']:
   self.assertIn('1',f['safe']);self.assertEqual(len(f['main']),f['width']*f['height'])
   for i,c in enumerate(f['safe']):
    if c=='1':self.assertEqual(f['main'][i],'1');self.assertEqual(f['stay'][i],'1')
 def test_resource_dimensions_match_real_map(self):
  for p in ('Resources/Goryeo/PlaceLabels.json','Resources/Goryeo/Life/Anchors.json','Resources/Goryeo/VillageLife/Anchors.json','Resources/Goryeo/Stride/Surfaces.json'):
   for r in self.out[p]['maps']:self.assertEqual((r['w'],r['h']),(self.maps[r['id']]['w'],self.maps[r['id']]['h']))
 def test_shared_connection_source_preserves_every_foreign_endpoint_and_sign(self):
  p='World/Connections/region_links.json';old=self.base['resources'][p];now=self.out[p]
  for a,b in zip(old['links'],now['links']):
   for side,other in [('a','b'),('b','a')]:
    if a[side]['map']not in self.maps:self.assertEqual(a[side],b[side])
    else:
     d=self.maps[b[side]['map']];e=next(o for o in d['objects']if o['kind']=='exit'and o['target']==b[other]['map'])
     self.assertEqual([b[side][k]for k in ('x','y','w','h','edge')],[e[k]for k in ('x','y','w','h','edge')])
     self.assertEqual(b[side]['by'],'Tools/WorldLayout/t3_install.py')
  self.assertEqual([s for s in old['signs']if s['map']not in self.maps],[s for s in now['signs']if s['map']not in self.maps])
 def test_historical_replay_rejects_unrecognized_live_map_mutations(self):
  from t3_history import restore_map
  d=copy.deepcopy(self.maps['Naru']);self.assertEqual(restore_map(d),self.base['maps']['Naru'])
  d['blocked'][0]=1-d['blocked'][0]
  with self.assertRaises(ValueError):restore_map(d)
if __name__=='__main__':unittest.main()
