"""Geographic direction, playable/coastal context separation, and inland visual progression."""
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

class PeninsulaGeography(unittest.TestCase):
    def setUp(self):
        self.atlas=json.loads((ROOT/'Assets/PocketCaravan/Resources/WorldMap/GoryeoLayout.json').read_text())
        self.areas={a['mapId']:a for a in self.atlas['maps']}
        self.profile=json.loads((ROOT/'Assets/PocketCaravan/World/Cartography/peninsula.json').read_text())

    def test_continental_gate_is_in_northwest_starting_region(self):
        songak=self.areas['goryeo_songak'];capital=self.areas['Gaegyeong'];central=self.areas['transit_crossroads']
        self.assertLess(songak['y'],capital['y'])
        self.assertLess(central['x']+central['w'],songak['x'])
        self.assertLess(central['y']+central['h']/2,capital['y']+capital['h']/2)
        jangdan=json.loads((ROOT/'Assets/PocketCaravan/Maps/goryeo_jangdan.json').read_text())
        self.assertFalse(any(o['kind']=='exit' and o['target'].startswith('transit_') for o in jangdan['objects']))

    def test_all_playable_maps_are_on_land_and_context_is_not_extra_maps(self):
        anchor=self.areas[self.profile['anchorMapId']]
        polygons=[p['points'] for p in self.profile['landPolygons']]
        def inside(x,y,poly):
            result=False
            for i,(ax,ay) in enumerate(poly):
                bx,by=poly[(i+1)%len(poly)]
                if (ay>y)!=(by>y) and x < (bx-ax)*(y-ay)/(by-ay)+ax:result=not result
            return result
        for a in self.areas.values():
            for x in (a['x']+.5,a['x']+a['w']-.5):
                for y in (a['y']+.5,a['y']+a['h']-.5):
                    self.assertTrue(any(inside(x-anchor['x'],y-anchor['y'],p) for p in polygons),a['mapId'])
        self.assertEqual(len(self.areas),15)
        self.assertEqual(self.atlas['geography'],'peninsula_inland')
        self.assertTrue(any(n['text']=='고려 반도' for n in self.atlas['annotations']))
        self.assertTrue(any(n['text']=='중앙아시아 방면' for n in self.atlas['annotations']))

    def test_inland_palette_progresses_from_original_goryeo_art(self):
        for mid in ['transit_central_approach','transit_crossroads']:
            m=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            self.assertEqual(m['visualSkin'],'steppe')
            self.assertTrue(any('skin_steppe' in key for key in m['palette']))
        original=json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text())
        self.assertFalse(any('skin_steppe' in key for key in original['palette']))

if __name__=='__main__':unittest.main()
