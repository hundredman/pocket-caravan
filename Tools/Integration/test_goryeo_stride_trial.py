import collections,json,sys,unittest,wave,array,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A4-v3-20261105';sys.path.insert(0,str(ROOT/'Tools/Atmosphere'))
import goryeo_stride_trial as stride
class StrideTrial(unittest.TestCase):
 def test_certified_surfaces_match_source_and_do_not_confuse_grade_with_material(self):
  source=json.loads((OUT/'source.json').read_text());self.assertEqual(source['maps'],stride.surfaces());self.assertEqual(len(source['maps']),13)
  for row in source['maps']:
   d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/(row['id']+'.json')).read_text());self.assertEqual(len(row['data']),row['w']*row['h']);self.assertEqual(row['counts'].get('S',0),0)
   for i,t in enumerate(row['data']):
    x,y=i%row['w'],i//row['w'];j=(row['h']-y-1)*row['w']+x
    if t=='W':self.assertFalse(d['blocked'][j]);self.assertGreater(d['road'][j],0)
  naru=next(m for m in source['maps']if m['id']=='Naru');self.assertEqual(naru['counts']['W'],35)
  d=json.loads((ROOT/'Assets/PocketCaravan/Maps/Naru.json').read_text());self.assertTrue(any(d['road'][i]==2 and naru['data'][(d['h']-i//d['w']-1)*d['w']+i%d['w']]=='D'for i in range(len(d['road']))))
 def test_material_foley_is_mono_pcm_without_clipping_or_edge_click(self):
  source=json.loads((OUT/'source.json').read_text());self.assertEqual([c['kind']for c in source['audio']],list(stride.KINDS));fingerprints=set()
  for clip in source['audio']:
   with wave.open(str(ROOT/clip['path']))as f:
    self.assertEqual((f.getnchannels(),f.getsampwidth(),f.getframerate()),(1,2,22050));pcm=array.array('h',f.readframes(f.getnframes()));self.assertGreater(len(pcm),2205);self.assertLess(len(pcm),44100)
   self.assertEqual((pcm[0],pcm[-1]),(0,0));self.assertLess(max(abs(x)for x in pcm),7000);self.assertLess(abs(sum(pcm)/len(pcm)),300);fingerprints.add(pcm.tobytes())
  self.assertEqual(len(fingerprints),5)
 def test_separate_material_sources_match_their_pinned_public_blobs(self):
  source=json.loads((OUT/'Sources/provenance.json').read_text());self.assertEqual({r['kind']for r in source},set(stride.KINDS));hashes=set()
  for row in source:
   file=OUT/'Sources'/(row['kind']+('.wav'if row['kind']=='splash'else'.ogg'));raw=file.read_bytes();sha=hashlib.sha1(b'blob '+str(len(raw)).encode()+b'\0'+raw).hexdigest();self.assertEqual(sha,row['gitBlobSHA1']);self.assertEqual(row['license'],'CC0');hashes.add(sha)
  self.assertEqual(len(hashes),5)
 def test_demo_order_matches_the_review_labels(self):
  source=json.loads((OUT/'source.json').read_text());self.assertEqual([t['kind']for t in source['timeline']],['dirt','grass','stone','wood','splash'])
  self.assertEqual(source['timeline'][0]['startSeconds'],0)
  for a,b in zip(source['timeline'],source['timeline'][1:]):self.assertAlmostEqual(a['startSeconds']+a['durationSeconds'],b['startSeconds'])
  self.assertTrue((OUT/'footsteps-review.mp3').is_file());self.assertEqual(source['status'],'review')
if __name__=='__main__':unittest.main()
