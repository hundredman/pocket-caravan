"""Geographic direction, playable/coastal context separation, and inland visual progression."""
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

class PeninsulaGeography(unittest.TestCase):
    def setUp(self):
        self.atlas=json.loads((ROOT/'Assets/PocketCaravan/Resources/WorldMap/GoryeoLayout.json').read_text())
        self.areas={a['mapId']:a for a in self.atlas['maps']}
        self.profile=json.loads((ROOT/'Assets/PocketCaravan/World/Terrain/continuous.json').read_text())

    def test_continental_gate_is_in_northwest_starting_region(self):
        # M4(2026-10-28): 대륙(중앙 동쪽)으로 나가는 고려 맵은 서쪽의 송악·장단뿐. 나루·개경·닥목은 고려 안쪽
        songak=self.areas['goryeo_songak'];capital=self.areas['Gaegyeong'];central=self.areas['transit_crossroads']
        self.assertLess(songak['x'],capital['x'])
        self.assertLess(central['x']+central['w'],songak['x'])
        gates=set()
        for mid in ['Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan']:
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            if any(o['kind']=='exit' and o['target'].startswith('transit_') for o in m['objects']):gates.add(mid)
        self.assertEqual(gates,{'goryeo_songak','goryeo_jangdan'})

    def test_all_playable_maps_are_on_land_and_context_is_not_extra_maps(self):
        # M4(2026-10-28): 13맵. 시작 위치(S0)는 나루에만 있고 걸을 수 있는 칸
        self.assertEqual(len(self.areas),13)
        self.assertEqual(self.atlas['geography'],'continuous_tiles')
        for mid,a in self.areas.items():
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            w,h=m['w'],m['h']
            self.assertEqual((a['w'],a['h']),(w,h))
            if not mid.startswith('transit_'):self.assertGreaterEqual(w*h,3000)
            self.assertGreater(len(m['blocked'])-sum(m['blocked']),0)
        m=json.loads((ROOT/'Assets/PocketCaravan/Maps/Naru.json').read_text())
        sp=next(o for o in m['objects'] if o['kind']=='spawn' and o['id']=='S0')
        self.assertEqual(m['blocked'][sp['y']*m['w']+sp['x']],0)
        for mid in ['Naru','Dakmok','goryeo_jangdan']:
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            self.assertGreater(sum(m['water']),0)
            self.assertLess(sum(m['water']),m['w']*m['h'])

    def test_goryeo_is_compact_and_entirely_east_of_owned_continent(self):
        # M4(2026-10-28): 맵 직사각형이 겹치므로 가운데 점으로 본다. 고려 맵은 모두 중앙 동쪽 맵보다 동쪽
        center=lambda a:a['x']+a['w']/2
        east=[a for mid,a in self.areas.items() if not mid.startswith('transit_')]
        central=[a for mid,a in self.areas.items() if mid.startswith('transit_')]
        self.assertGreater(min(center(a) for a in east),max(center(a) for a in central))
        width=max(a['x']+a['w'] for a in east)-min(a['x'] for a in east)
        height=max(a['y']+a['h'] for a in east)-min(a['y'] for a in east)
        self.assertLessEqual(max(width,height)/min(width,height),1.8)
        self.assertEqual(self.atlas['centralContinentTotalCells'],100000)
        self.assertEqual(self.atlas['centralEastOwnedCells'],25000)

    @unittest.skip('M4 새 맵의 땅은 승인된 M1 지형 타일(풀밭·물·흙길 조립)을 쓴다. 옛 내륙 초원(skin_steppe) 단계는 은샘 M1 승인(2026-10-28)으로 대체')
    def test_inland_palette_progresses_from_original_goryeo_art(self):
        for mid in ['transit_central_approach','transit_crossroads']:
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            self.assertTrue(any('skin_steppe' in key for key in m['palette']))
        original=json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text())
        self.assertFalse(any('skin_steppe' in key for key in original['palette']))

if __name__=='__main__':unittest.main()
