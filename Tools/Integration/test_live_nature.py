"""Approved nature is live: complete paddies and preserved external connections."""
import json,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import m4_nature
class LiveNature(unittest.TestCase):
    def setUp(self):
        self.before=json.loads((ROOT/'Docs/Validation/A7d-Integration-2026-10-30/before-map-gameplay.json').read_text())
        self.maps={mid:json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())for mid in self.before}
    def test_live_snapshot_has_approved_whole_source_art_and_exact_review_placement(self):
        live,assets=m4_nature.load_layout(m4_nature.LIVE,production=True)
        review=json.loads((ROOT/'Tools/WorldLayout/nature_a7d.json').read_text())
        for key in ['placements','clearings','connections','headwaters']:self.assertEqual(live[key],review[key])
    def test_eight_complete_paddies_are_live_and_walkable(self):
        total=0
        for mid,d in self.maps.items():
            self.assertFalse(d.get('previewOnly',False))
            for c in d.get('natureCrops',[]):
                total+=1
                for y in range(c['y'],c['y']+c['h']):
                    for x in range(c['x'],c['x']+c['w']):
                        i=y*d['w']+x
                        self.assertGreater(d['ground'][i],0,(mid,x,y))
                        self.assertEqual((d['blocked'][i],d['water'][i],d['road'][i]),(0,0,0),(mid,x,y))
        self.assertEqual(total,8)
    def test_existing_other_civilization_connections_are_unchanged(self):
        for mid,b in self.before.items():
            old=[o for o in b['objects']if o['kind']=='exit'and o['target']not in self.maps]
            now=[o for o in self.maps[mid]['objects']if o['kind']=='exit'and o['target']not in self.maps]
            self.assertEqual(old,now,mid)
if __name__=='__main__':unittest.main()
