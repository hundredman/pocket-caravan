"""Native module coverage, border continuity and isolated review outputs."""
import json
import sys
import unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
from water_modules_a4 import canonical,masks,names,render_piece,topology,seam_check,fixtures
OUT=ROOT/'ArtReview/A4-Water-v1'

class WaterModuleChecks(unittest.TestCase):
    def test_all_neighbor_states_resolve_to_one_of_47_registered_modules(self):
        registered=set(names())
        self.assertEqual(len(masks()),47)
        self.assertEqual({canonical(i) for i in range(256)},set(masks()))
        self.assertTrue(set(masks())<=registered)
        for i in range(256):
            self.assertEqual(canonical(canonical(i)),canonical(i))
        self.assertEqual(canonical(16|32|64|128),0)

    def test_four_directions_of_edges_and_both_corner_roles_are_present(self):
        lookup=names()
        for mask in (110,205,155,55):self.assertTrue(lookup[mask].startswith('edge'))
        for mask in (38,76,137,19):self.assertTrue(lookup[mask].startswith('outer_corner'))
        for mask in (127,239,223,191):self.assertTrue(lookup[mask].startswith('inner_corner'))
        self.assertEqual(lookup[255],'center')

    def test_all_shared_borders_match_including_narrow_and_diagonal_cases(self):
        images={mask:render_piece(mask) for mask in masks()}
        result=seam_check(images)
        self.assertEqual(result['pixelPairs'],32768)
        self.assertEqual(result['mismatches'],0)
        images[255].putpixel((15,8),(255,0,255,255))
        self.assertGreater(seam_check(images)['mismatches'],0)

    def test_sheet_cells_match_each_individually_registered_piece(self):
        data=json.loads((OUT/'piece-index.json').read_text())
        sheet=Image.open(OUT/'water-sheet.png').convert('RGBA')
        catalogue=json.loads((OUT/'catalogue.json').read_text())
        registered=catalogue['assets'][0]['pieces']
        self.assertEqual(set(registered),{r['piece'] for r in data['cells']})
        self.assertEqual(len(registered),51)
        for row in data['cells']:
            x,y,w,h=row['rect'];im=Image.open(ROOT/registered[row['piece']]['image']).convert('RGBA')
            self.assertEqual(im.size,(16,16))
            self.assertEqual(sheet.crop((x,y,x+w,y+h)).tobytes(),im.tobytes())
            self.assertTrue(set(im.getchannel('A').get_flattened_data())<={0,255})
        self.assertEqual(catalogue['assets'][0]['review']['status'],'changes_requested')

    def test_preview_coordinates_use_the_declared_topology_without_transforms(self):
        for label,wet in fixtures().items():
            data=json.loads((OUT/f'{label}-preview.json').read_text())
            self.assertEqual(len(data['assetPlacements']),len(wet))
            for p in data['assetPlacements']:
                self.assertEqual(p['piece'],names()[topology(wet,p['x'],p['y'])])
                self.assertEqual(set(p),{'id','assetId','piece','x','y','purpose'})
                self.assertEqual(p['assetId'],'trial_pond_lake_water')

if __name__=='__main__':unittest.main()
