"""The user-defined ownership boundary counts sea and blocked cells, not just walkable land."""
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

class MeetingAreaBudget(unittest.TestCase):
    def test_full_owned_area_and_complete_ground(self):
        # M4(2026-10-28): 맵 직사각형이 겹치므로 세계 칸으로 센다. 담당 칸(바닥 그림이 있는 칸)은 고려 20,000 + 중앙 동쪽 25,000에서
        # 다른 문명 연결 자리를 곧게 깎은 7칸(m4_links)을 뺀 값. 담당 칸은 모두 바닥 그림이 있다.
        cells=set();per={}
        for region in ('goryeo','transit'):
            manifest=json.loads((ROOT/f'Assets/PocketCaravan/World/Regions/{region}.json').read_text())
            for entry in manifest['maps']:
                m=json.loads((ROOT/entry['jsonPath']).read_text())
                self.assertEqual(len(m['ground']),m['w']*m['h'])
                for i,g in enumerate(m['ground']):
                    if g:
                        self.assertTrue(m['palette'][g],m['id'])
                        cells.add((m['terrainX']+i%m['w'],m['terrainY']+i//m['w']))
                    elif not m['blocked'][i]:self.fail(('walkable cell without ground',m['id'],i))
        self.assertEqual(len(cells),45000-7)
        budget=json.loads((ROOT/'Assets/PocketCaravan/World/Terrain/area_budget.json').read_text())
        self.assertEqual(budget['centralContinentTiles'],100000)
        self.assertEqual(budget['centralEastTiles']+budget['centralOtherTeamsTiles'],budget['centralContinentTiles'])
        self.assertEqual(budget['centralEastTiles'],25000)

    def test_capital_uses_authored_pixel_architecture(self):
        # M4(2026-10-28): 개경 궁궐은 승인된 C7 궁궐 그림을 통째로 놓고 칸으로 나눈 그림(m4_*, 승인 원본에서 다시 그려 인증)
        m=json.loads((ROOT/'Assets/PocketCaravan/Maps/Gaegyeong.json').read_text())
        manifest=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json').read_text())
        palace={sid for sid,src in manifest['sources'].items() if 'palace' in src['path']}
        self.assertTrue(palace)
        used=[n for n in m['palette'] if n in manifest['tiles'] and any(p[0] in palace for p in manifest['tiles'][n]['parts'])]
        self.assertGreaterEqual(len(used),20)

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
