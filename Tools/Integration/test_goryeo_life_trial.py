import unittest,json,tempfile,subprocess
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A7-20261105'
import sys
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from t3_history import map_at,resource_at
class VillageLifeTrial(unittest.TestCase):
 def test_verified_road_and_boat_corridors_preserve_land(self):
  s=json.loads((OUT/'source.json').read_text());self.assertEqual(s['status'],'review');self.assertEqual(len(s['maps']),13)
  for m in s['maps']:
   d=map_at(ROOT/'Assets/PocketCaravan/Maps'/f"{m['id']}.json");w,h=d['w'],d['h']
   self.assertLessEqual(sum(4 if p['kind']=='smoke'else 1 for p in m['points']),s['renderLimit'])
   for p in m['points']:
    self.assertTrue(0<=p['x']<w and 0<=p['y']<h)
    if p['kind']in ('walker','cart'):
     x=int(p['x']);y=h-1-int(p['y'])
     for yy in range(y-1,y+2):
      for xx in range(x,x+7):
       i=yy*w+xx;self.assertTrue(d['reachable'][i]);self.assertFalse(d['blocked'][i]);self.assertGreaterEqual(d['road'][i],1)
    if p['kind']=='boat':
     x=int(p['x']-2.5);y=round(h-p['y']-1.5)
     for yy in range(y,y+3):
      for xx in range(x,x+8):
       i=yy*w+xx;self.assertTrue(d['water'][i]);self.assertFalse(d['obj'][i]);self.assertFalse(d['high'][i])
 def test_new_native_animal_frames_are_crisp_and_smaller_than_player(self):
  for kind in ('chicken','dog'):
   states=[]
   for f in range(2):
    with Image.open(OUT/'Native'/f'{kind}_{f}.png')as im:
     im=im.convert('RGBA');self.assertEqual(im.size,(32,32));self.assertLessEqual(set(im.getchannel('A').getdata()),{0,255});self.assertLessEqual(len(set(im.getdata())),10);bbox=im.getbbox();self.assertLessEqual(bbox[3]-bbox[1],16);self.assertEqual(bbox[3],30);states.append(im.tobytes())
   self.assertNotEqual(*states)
 def test_aseprite_two_frames_reexport_identical_decoded_rgba(self):
  ase=Path('/Applications/Aseprite.app/Contents/MacOS/aseprite')
  if not ase.exists():self.skipTest('Aseprite unavailable')
  with tempfile.TemporaryDirectory()as temp:
   t=Path(temp);code=[f"local root={json.dumps(str(OUT),ensure_ascii=False)};local out={json.dumps(str(t),ensure_ascii=False)}"]
   for kind in ('chicken','dog'):
    code.append(f"local s=app.open(root..'/Native/{kind}.aseprite');assert(#s.frames==2);for f=1,2 do local d=Sprite(32,32,ColorMode.RGB);d.cels[1].image:drawImage(s.cels[f].image,Point(0,0));d:saveAs(out..'/{kind}_'..(f-1)..'.png');d:close()end;s:close()")
   script=t/'export.lua';script.write_text('\n'.join(code));subprocess.run([str(ase),'-b','--script',str(script)],check=True,capture_output=True,timeout=60)
   for p in (OUT/'Native').glob('*.png'):
    with Image.open(p)as a,Image.open(t/p.name)as b:self.assertEqual(a.convert('RGBA').tobytes(),b.convert('RGBA').tobytes())
 def test_same_purpose_whole_existing_assets_and_preview_gate(self):
  s=json.loads((OUT/'source.json').read_text());assets={a['key']:a['path']for a in s['assets']};self.assertIn('BuildingShip-Renewal-v1/boat.png',assets['boat']);self.assertIn('c6_cart_e1a728ea.png',assets['cart'])
  for a in s['assets']:self.assertTrue((ROOT/a['path']).is_file())
  code=(ROOT/'Assets/PocketCaravan/Scripts/Goryeo/GoryeoVillageLife.cs').read_text();self.assertNotIn('RuntimeInitializeOnLoadMethod',code);self.assertIn('map.previewOnly||approvedForProduction',code);self.assertNotIn('Random.Range',code);target=ROOT/'Assets/PocketCaravan/Resources/Goryeo/VillageLife';config=resource_at(target/'Anchors.json');approval=json.loads((OUT/'approval.json').read_text());self.assertEqual(config['status'],'approved');self.assertEqual(approval['status'],'approved');self.assertEqual(len(config['maps']),13)
  for a,b in zip(config['maps'],s['maps']):
   for k in ('id','w','h'):self.assertEqual(a[k],b[k])
   self.assertEqual(len(a['points']),len(b['points']))
   for p,q in zip(a['points'],b['points']):
    for k in q:
     if isinstance(q[k],(float,int)):self.assertAlmostEqual(p[k],q[k],places=4)
     else:self.assertEqual(p[k],q[k])
  for a in approval['assets']:
   with Image.open(ROOT/a['path'])as original,Image.open(target/'Art'/f"{a['key']}.png")as copied:self.assertEqual(original.convert('RGBA').tobytes(),copied.convert('RGBA').tobytes())
 def test_approved_a2_frames_and_preserved_trader(self):
  sys_path=str(ROOT/'Tools/MapGen');import sys;sys.path.insert(0,sys_path)
  from asset_manifest import validate_catalogue
  catalogue=json.loads((ROOT/'ArtReview/Approved/GoryeoAtmosphereA7/catalogue.json').read_text());errors,_=validate_catalogue(catalogue,ROOT);self.assertEqual(errors,[]);self.assertEqual(len(catalogue['assets']),4)
  for d,direction in enumerate('dulr'):
   for f in range(4):
    with Image.open(ROOT/'Assets/PocketCaravan/Art/Characters/NPC/npc_goryeo_songak_merchant'/f'npc_goryeo_songak_merchant_tall_{direction}{f}.png')as a,Image.open(ROOT/'Assets/PocketCaravan/Resources/Goryeo/VillageLife/Trader'/f'{d*4+f}.png')as b:self.assertEqual(a.convert('RGBA').tobytes(),b.convert('RGBA').tobytes())
if __name__=='__main__':unittest.main()
