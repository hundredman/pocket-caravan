import unittest,json,wave,array,math,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A2-20261105'
class TimeMusicTrial(unittest.TestCase):
 def test_four_soft_stereo_loops_and_non_clicking_boundaries(self):
  source=json.loads((OUT/'source.json').read_text());self.assertEqual(source['status'],'review');self.assertEqual([t['name']for t in source['tracks']],['morning','day','evening','night']);fingerprints=set()
  for t in source['tracks']:
   with wave.open(str(ROOT/t['path']))as w:
    self.assertEqual((w.getnchannels(),w.getsampwidth(),w.getframerate()),(2,2,32000));self.assertAlmostEqual(w.getnframes()/32000,t['durationSeconds']);a=array.array('h',w.readframes(w.getnframes()))
   self.assertLess(max(abs(v)for v in a),12000);rms=math.sqrt(sum(v*v for v in a)/len(a))/32767;self.assertTrue(.05<rms<.07);self.assertLess(max(abs(a[c]-a[-2+c])for c in (0,1))/32767,.005);self.assertLess(abs(sum(a)/len(a)),3);fingerprints.add(a.tobytes()[:320000])
  self.assertEqual(len(fingerprints),4)
 def test_approved_score_policy_and_production_audio(self):
  s=json.loads((OUT/'source.json').read_text());self.assertEqual(len(s['timeSlots']),4);self.assertEqual([t['kind']for t in s['timeline']],['morning','day','evening','night'])
  for t in s['tracks']:
   self.assertTrue(t['score']);self.assertTrue((OUT/(t['name']+'-review.mp3')).is_file());self.assertTrue(all(0<=n['beat']<64 for n in t['score']))
  self.assertTrue((OUT/'time-music-review.mp3').is_file());target=ROOT/'Assets/PocketCaravan/Resources/Goryeo/TimeMusic'
  a=json.loads((OUT/'approval.json').read_text());self.assertEqual(a['status'],'approved');self.assertEqual(a['sourceSha256'],hashlib.sha256((OUT/'source.json').read_bytes()).hexdigest())
  self.assertEqual(len(json.loads((target/'Config.json').read_text())['maps']),13)
  for name,h in a['tracks'].items():
   self.assertEqual(hashlib.sha256((target/(name+'.wav')).read_bytes()).hexdigest(),h);self.assertEqual((target/(name+'.wav')).read_bytes(),(OUT/'Native'/(name+'.wav')).read_bytes())
  code=(ROOT/'Assets/PocketCaravan/Scripts/Goryeo/GoryeoTimeMusic.cs').read_text();self.assertNotIn('RuntimeInitializeOnLoadMethod',code);self.assertNotIn('SetValue(',code);self.assertNotIn('PlayerPrefs',code);self.assertNotIn('master.musicVolume=',code);self.assertIn('oldMuteA',code)
if __name__=='__main__':unittest.main()
