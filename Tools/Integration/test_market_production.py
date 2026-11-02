"""Production market contracts: preserve reviewed art/layout and access to existing functions."""
import contextlib,hashlib,io,json,sys,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
LAYOUT=ROOT/'Assets/PocketCaravan/Data/Goryeo/MarketLayout.json'
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
@unittest.skipUnless(LAYOUT.exists(),'Production markets not promoted')
class ProductionMarkets(unittest.TestCase):
 def setUp(self):self.data=json.loads(LAYOUT.read_text())
 def test_approved_review_art_and_all_positions_are_preserved(self):
  review=json.loads((ROOT/self.data['reviewSource']).read_text())
  expected={m['mapId']:[s for s in m['spaces']if s['props']]for m in review['maps']if any(s['props']for s in m['spaces'])}
  self.assertEqual({m['mapId']:m['spaces']for m in self.data['maps']},expected)
  for a in self.data['assets']:
   self.assertTrue(a['image'].startswith('Assets/PocketCaravan/Art/Goryeo/Markets/'))
   with Image.open(ROOT/a['image'])as im:dest=im.convert('RGBA')
   with Image.open(ROOT/a['source'])as im:src=im.convert('RGBA')
   self.assertEqual(dest.size,tuple(n*16 for n in a['size']))
   self.assertEqual(src.tobytes(),dest.tobytes());self.assertEqual(hashlib.sha256(dest.tobytes()).hexdigest(),a['rgbaSha256'])
 def test_no_new_market_functions_or_foreign_maps(self):
  import market_production
  for m in self.data['maps']:
   self.assertIn(m['mapId'],market_production.ALLOWED)
   d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f"{m['mapId']}.json").read_text())
   self.assertEqual((m['width'],m['height']),(d['w'],d['h']))
   if not any(o['kind']=='market'for o in d['objects']):
    self.assertEqual({p['asset']for s in m['spaces']for p in s['props']},{'sign'})
 def test_real_production_scenes_reference_dressing_and_native_sprites(self):
  script_guid=(ROOT/'Assets/PocketCaravan/Scripts/Map/VillageDressing.cs.meta').read_text().split('guid: ')[1].strip()
  guids={a['id']:(ROOT/(a['image']+'.meta')).read_text().split('guid: ')[1].splitlines()[0]for a in self.data['assets']}
  regions=[json.loads(p.read_text())for p in (ROOT/'Assets/PocketCaravan/World/Regions').glob('*.json')]
  entries={m['mapId']:m for r in regions for m in r['maps']}
  for m in self.data['maps']:
   scene=(ROOT/entries[m['mapId']]['scenePath']).read_text()
   self.assertIn('guid: '+script_guid,scene,m['mapId']);self.assertNotIn('previewOnly: 1',scene)
   for p in [p for s in m['spaces']for p in s['props']]:self.assertIn('guid: '+guids[p['asset']],scene,m['mapId'])
 def test_all_existing_functions_and_exits_remain_reachable(self):
  import m4_reach
  maps={p.stem:json.loads(p.read_text())for p in (ROOT/'Assets/PocketCaravan/Maps').glob('*.json')}
  for m in self.data['maps']:
   d=maps[m['mapId']];w=d['w']
   for s in m['spaces']:
    for p in s['props']:
     for dx in range(p['w']):d['blocked'][(p['y']+p['h']-1)*w+p['x']+dx]=1
  with tempfile.TemporaryDirectory()as td:
   for mid,d in maps.items():Path(td,f'{mid}.json').write_text(json.dumps(d))
   log=io.StringIO()
   with patch.object(sys,'argv',['m4_reach','--maps',td]),contextlib.redirect_stdout(log):result=m4_reach.main()
   self.assertEqual(result,0,log.getvalue())
if __name__=='__main__':unittest.main()
