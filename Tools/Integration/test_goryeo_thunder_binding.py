import hashlib,json,unittest,wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ArtReview/Goryeo-StormLogs-20261105'
PROD=ROOT/'Assets/PocketCaravan/Resources/Goryeo/Thunder'
class ThunderBinding(unittest.TestCase):
 def test_approved_policy_is_bound_to_single_existing_event(self):
  a=json.loads((REVIEW/'thunder-approval.json').read_text());c=json.loads((PROD/'Config.json').read_text())
  self.assertEqual(a['status'],'approved');self.assertEqual(a['userReply'],'이 표현과 빈도로 승인');self.assertEqual(c['status'],'approved')
  self.assertEqual(c['mapId'],a['mapId']);self.assertEqual(c['placementId'],a['placementId']);self.assertTrue(c['oncePerWindowPerSession']);self.assertTrue(a['oncePerWindowPerSession']);self.assertEqual(c['windowHours'],8)
  data=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{c['mapId']}.json").read_text());self.assertTrue(any(p['id']==c['placementId']and p['definitionId']=='fallen_log'for p in data['obstacles']))
 def test_approved_native_audio_is_unchanged_pcm_in_production(self):
  a=json.loads((REVIEW/'thunder-approval.json').read_text())
  for f,key in [('Native/distant-thunder.wav','audioSha256'),('source.json','sourceSha256'),('approved-effect-source.txt','effectSha256')]:self.assertEqual(hashlib.sha256((REVIEW/f).read_bytes()).hexdigest(),a[key])
  def decoded(path):
   with wave.open(str(path))as w:return (w.getnchannels(),w.getsampwidth(),w.getframerate(),w.getnframes()),w.readframes(w.getnframes())
  native=decoded(REVIEW/'Native/distant-thunder.wav');self.assertEqual(native[0][:3],(2,2,32000));self.assertEqual(decoded(PROD/'distant-thunder.wav'),native)
if __name__=='__main__':unittest.main()
