import unittest,json,sys,wave,array,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A3-20261105';sys.path.insert(0,str(ROOT/'Tools/Atmosphere'))
import goryeo_life_sound_trial as life
class LifeSoundTrial(unittest.TestCase):
 def test_anchors_follow_existing_maps_and_markets(self):
  s=json.loads((OUT/'source.json').read_text());self.assertEqual(s['status'],'review');self.assertEqual(s['maps'],life.maps());self.assertEqual(len(s['maps']),13)
  kinds=set()
  for m in s['maps']:
   d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{m['id']}.json").read_text());markets={(o['x'],d['h']-o['y']-1)for o in d['objects']if o['kind']=='market'}
   self.assertEqual({(p['x'],p['y'])for p in m['points']if p['kind']=='crowd'},markets)
   for p in m['points']:
    kinds.add(p['kind']);self.assertTrue(0<=p['x']<d['w']and 0<=p['y']<d['h']);self.assertLessEqual(p['radius'],14);self.assertLess(p['startHour'],p['endHour'])
    if p['kind']in ('chicken','cow','dog'):
     i=(d['h']-p['y']-1)*d['w']+p['x'];self.assertTrue(d['reachable'][i]);self.assertFalse(d['blocked'][i]);self.assertFalse(d['water'][i])
  self.assertEqual(kinds,set(life.KINDS))
 def test_source_hashes_and_pcm_levels(self):
  for row in json.loads((OUT/'Sources/provenance.json').read_text()):
   self.assertEqual(row['license'],'CC0');self.assertEqual(hashlib.sha256((OUT/'Sources'/f"{row['kind']}.mp3").read_bytes()).hexdigest(),row['sha256'])
   with wave.open(str(OUT/'Native'/f"{row['kind']}.wav"))as w:
    self.assertEqual((w.getnchannels(),w.getsampwidth(),w.getframerate()),(1,2,22050));a=array.array('h',w.readframes(w.getnframes()));self.assertGreater(len(a),4000);self.assertLess(max(abs(v)for v in a),7300)
   if row['kind']=='crowd':self.assertLess(abs(a[0]-a[-1]),2000)
   else:self.assertEqual((a[0],a[-1]),(0,0))
 def test_listening_order_and_approved_production_audio(self):
  s=json.loads((OUT/'source.json').read_text());self.assertEqual([v['kind']for v in s['timeline'][:5]],list(life.KINDS));self.assertTrue((OUT/'life-review.mp3').exists());target=ROOT/'Assets/PocketCaravan/Resources/Goryeo/Life';approval=json.loads((OUT/'approval.json').read_text());self.assertEqual(approval['status'],'approved');production=json.loads((target/'Anchors.json').read_text());self.assertEqual(production['status'],'approved')
  self.assertEqual(len(production['maps']),len(s['maps']))
  for a,b in zip(production['maps'],s['maps']):
   for key in ('id','w','h'):self.assertEqual(a[key],b[key])
   self.assertEqual(len(a['points']),len(b['points']))
   for p,q in zip(a['points'],b['points']):
    for key in q:
     if key=='level':self.assertAlmostEqual(p[key],q[key],places=6) # Unity float32 JSON serialization
     else:self.assertEqual(p[key],q[key])
  for kind in life.KINDS:
   raw=(target/f'{kind}.wav').read_bytes();self.assertEqual(raw,(OUT/'Native'/f'{kind}.wav').read_bytes());self.assertEqual(hashlib.sha256(raw).hexdigest(),approval['sha256'][f'{kind}.wav'])
if __name__=='__main__':unittest.main()
