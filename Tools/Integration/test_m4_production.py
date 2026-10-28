"""M4 새 맵 본편 전환(2026-10-28): 겹친 칸 인증, 등록·출입구 도달, 거리(layoutD), 옛 저장 도착 자리."""
import json,shutil,sys,tempfile,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import m4_certify

MAPS=['Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan','transit_pass','transit_woodtown','transit_wetroad','transit_ferry',
      'transit_central_approach','transit_crossroads','transit_southport','transit_south_inland']

def load(mid):return json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())

class Composites(unittest.TestCase):
    def test_every_composite_tile_recomposes_from_approved_sources(self):
        errors,info=m4_certify.verify()
        self.assertEqual(errors,[]);self.assertGreater(info['tiles'],1000)
    def test_one_pixel_change_is_rejected(self):
        m=json.loads(m4_certify.MANIFEST.read_text());name=next(iter(m['tiles']))
        with tempfile.TemporaryDirectory() as t:
            t=Path(t);shutil.copy(ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{name}.png',t/f'{name}.png')
            im=Image.open(t/f'{name}.png').convert('RGBA');p=im.getpixel((0,0));im.putpixel((0,0),((p[0]+1)%256,)+p[1:]);im.save(t/f'{name}.png')
            m['tiles']={name:m['tiles'][name]};(t/'m.json').write_text(json.dumps(m))
            errors,_=m4_certify.verify(t/'m.json',t)
        self.assertTrue(any(name in e for e in errors))
    def test_unapproved_source_is_rejected(self):
        with self.assertRaises(ValueError):m4_certify.approval_for('x','ArtReview/C7-Batch9/new.png')
    def test_every_palette_entry_is_a_registered_or_certified_tile(self):
        m=json.loads(m4_certify.MANIFEST.read_text())
        for mid in MAPS:
            for n in load(mid)['palette'][1:]:
                self.assertTrue((ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{n}.png').is_file(),(mid,n))
                if n.startswith('m4_'):self.assertIn(n,m['tiles'])

class Registration(unittest.TestCase):
    def test_thirteen_maps_registered_with_scenes(self):
        ids=[e['mapId'] for name in('goryeo.json','transit.json') for e in json.loads((ROOT/f'Assets/PocketCaravan/World/Regions/{name}').read_text())['maps']]
        self.assertEqual(sorted(ids),sorted(MAPS))
    def test_start_spawn_reaches_every_map_and_function_object(self):
        import m4_reach,io,contextlib
        out=io.StringIO()
        argv=sys.argv;sys.argv=['m4_reach.py','--maps',str(ROOT/'Assets/PocketCaravan/Maps')]
        try:
            with contextlib.redirect_stdout(out):code=m4_reach.main()
        finally:sys.argv=argv
        self.assertEqual(code,0,out.getvalue())
    def test_arrival_spots_never_stand_on_an_exit(self):
        # 출구는 서서 경계 쪽을 보기만 해도 쓰인다. 도착 자리가 출구 위면 도착하자마자 다른 맵으로 넘어간다.
        for mid in MAPS:
            d=load(mid);ex={(o['x']+i,o['y']+j) for o in d['objects'] if o['kind']=='exit' for i in range(o['w']) for j in range(o['h'])}
            for o in d['objects']:
                if o['kind']=='spawn':
                    self.assertFalse(any((o['x']+i,o['y']+j) in ex for i in range(o['w']) for j in range(o['h'])),(mid,o['id']))
    def test_old_ids_kept(self):
        towns={o['town'] for mid in MAPS for o in load(mid)['objects'] if o['kind']=='market'}
        npcs={o['npc'] for mid in MAPS for o in load(mid)['objects'] if o['kind']=='npc'}
        self.assertEqual(towns,{'naru','goryeo_jangdan','goryeo_songak','dakmok','gaegyeong','transit_woodtown','transit_ferry'})
        self.assertEqual(npcs,{'naru_chief','naru_well_woman','gaekju','dakmok_chief','goryeo_songak_merchant','goryeo_jangdan_farmer'})

class Distances(unittest.TestCase):
    def test_layoutD_has_all_pairs_and_keeps_averages(self):
        d=json.loads((ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text())
        self.assertEqual(d['mode'],'layoutD');self.assertEqual(len(d['walkingDistances']),36)
        r=json.loads((ROOT/'Docs/Validation/M4-E1-2026-10-28.json').read_text())
        self.assertTrue(all(r['pass'].values()),r['pass'])

class SaveMigration(unittest.TestCase):
    def test_every_map_has_a_walkable_arrival_and_aliases_are_kept(self):
        g=json.loads((ROOT/'Assets/PocketCaravan/Resources/GoryeoLayout.json').read_text())
        self.assertEqual(len(g['aliases']),8);self.assertEqual(sorted(m['id'] for m in g['maps']),sorted(MAPS))
        for e in g['maps']:
            d=load(e['id']);
            for x,y in((e['spawnX'],e['spawnY']),(e['dropX'],e['dropY'])):
                cx,cy=int(x),d['h']-1-int(round(y-.82))
                self.assertEqual(d['blocked'][cy*d['w']+cx],0,(e['id'],x,y))

if __name__=='__main__':unittest.main()
