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
        songak=self.areas['goryeo_songak'];capital=self.areas['Gaegyeong'];central=self.areas['transit_crossroads']
        self.assertLess(songak['x'],capital['x'])
        self.assertLess(central['x']+central['w'],songak['x'])
        self.assertLess(central['y']+central['h']/2,capital['y']+capital['h']/2)
        jangdan=json.loads((ROOT/'Assets/PocketCaravan/Maps/goryeo_jangdan.json').read_text())
        self.assertFalse(any(o['kind']=='exit' and o['target'].startswith('transit_') for o in jangdan['objects']))

    def test_all_playable_maps_are_on_land_and_context_is_not_extra_maps(self):
        self.assertEqual(len(self.areas),11)
        self.assertEqual(self.atlas['geography'],'continuous_tiles')
        for mid,a in self.areas.items():
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            w,h=m['w'],m['h']
            self.assertEqual((a['w'],a['h']),(w,h))
            if not mid.startswith('transit_'):self.assertGreaterEqual(w*h,3000)
            sp=next(o for o in m['objects'] if o['kind']=='spawn' and o['id']=='S0')
            self.assertEqual(m['blocked'][sp['y']*w+sp['x']],0)
        for mid in ['Naru','Dakmok','goryeo_jangdan']:
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            self.assertGreater(sum(m['water']),0)
            self.assertLess(sum(m['water']),m['w']*m['h'])

    def test_inland_palette_progresses_from_original_goryeo_art(self):
        for mid in ['transit_central_approach','transit_crossroads']:
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            self.assertTrue(any('skin_steppe' in key for key in m['palette']))
        original=json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text())
        self.assertFalse(any('skin_steppe' in key for key in original['palette']))

if __name__=='__main__':unittest.main()
