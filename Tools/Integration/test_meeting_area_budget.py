"""The user-defined ownership boundary counts sea and blocked cells, not just walkable land."""
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

class MeetingAreaBudget(unittest.TestCase):
    def test_full_owned_area_and_complete_ground(self):
        totals={}
        for region in ('goryeo','transit'):
            manifest=json.loads((ROOT/f'Assets/PocketCaravan/World/Regions/{region}.json').read_text())
            total=0
            for entry in manifest['maps']:
                m=json.loads((ROOT/entry['jsonPath']).read_text())
                cells=m['w']*m['h'];total+=cells
                self.assertEqual(len(m['ground']),cells)
                self.assertTrue(all(i>0 for i in m['ground']),m['id'])
                self.assertTrue(all(m['palette'][i] for i in m['ground']),m['id'])
            totals[region]=total
        self.assertEqual(totals,{'goryeo':20000,'transit':25000})
        self.assertEqual(sum(totals.values()),45000)
        budget=json.loads((ROOT/'Assets/PocketCaravan/World/Terrain/area_budget.json').read_text())
        self.assertEqual(budget['centralContinentTiles'],100000)
        self.assertEqual(budget['centralEastTiles']+budget['centralOtherTeamsTiles'],budget['centralContinentTiles'])
        self.assertEqual(totals['transit'],budget['centralEastTiles'])

    def test_capital_uses_authored_pixel_architecture(self):
        m=json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text())
        royal=[k for k in m['palette'] if k.startswith('goryeo_royal_')]
        self.assertEqual(len(royal),70)
        from PIL import Image
        for name in royal:
            with Image.open(ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{name}.png') as im:
                self.assertEqual(im.size,(16,16))
                self.assertLessEqual(len(im.getchannel('A').getcolors()),2)

    def test_complete_nature_silhouettes_survive_tile_export(self):
        import sys
        sys.path.insert(0,str(ROOT/'Tools/MapGen'))
        from goryeo_art import sprite, NATURE_SIZES
        from PIL import Image
        for i,(w,h) in enumerate(NATURE_SIZES):
            expected=sprite('woodland'+str(i),w,h)
            assembled=Image.new('RGBA',expected.size)
            for y in range(h):
                for x in range(w):
                    path=ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/goryeo_woodland{i}_{x}_{y}.png'
                    with Image.open(path) as tile:
                        self.assertEqual(tile.size,(16,16))
                        assembled.paste(tile,(x*16,y*16))
            self.assertEqual(assembled.tobytes(),expected.tobytes(),i)
            left,top,right,bottom=assembled.getchannel('A').getbbox()
            self.assertTrue(0<left<right<assembled.width and 0<top<bottom<assembled.height,i)

    def test_goryeo_ripples_have_no_opaque_water_backplate(self):
        from PIL import Image
        m=json.loads((ROOT/'Assets/PocketCaravan/Maps/Dakmok.json').read_text())
        waves=[o for o in m['objects'] if o['kind']=='anim' and 'ripples' in o['sheet']]
        self.assertTrue(waves)
        self.assertTrue(all(o['sheet']=='goryeo_ripples' for o in waves))
        for i in range(4):
            with Image.open(ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Animated/goryeo_ripples_{i}.png') as im:
                self.assertEqual(im.size,(16,16))
                self.assertEqual(im.getpixel((0,0))[3],0)
                self.assertGreater(im.getchannel('A').getextrema()[1],0)
                self.assertLess(im.getchannel('A').getextrema()[1],255)

if __name__=='__main__':unittest.main()
