"""Screen-edge framing adds visual context without moving gameplay boundaries."""
import json,hashlib,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from camera_backdrop import exterior_cells
class CameraBoundary(unittest.TestCase):
    def test_visual_context_stays_outside_playable_rectangle(self):
        rows=json.loads((ROOT/'Docs/Validation/T1a-2026-10-30/map-baseline.json').read_text())
        for mid in rows:
            d=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text());seen=set()
            for c in d.get('cameraBackdrop',[]):
                self.assertFalse(0<=c['x']<d['w']and 0<=c['y']<d['h'],(mid,c))
                self.assertTrue(0<c['tile']<len(d['palette']))
                self.assertNotIn((c['x'],c['y']),seen);seen.add((c['x'],c['y']))
    def test_gameplay_and_original_art_are_unchanged(self):
        rows=json.loads((ROOT/'Docs/Validation/T1a-2026-10-30/map-baseline.json').read_text())
        sha=lambda d:hashlib.sha256(json.dumps(d,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()).hexdigest()
        for mid,r in rows.items():
            d=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            stable={k:v for k,v in d.items()if k not in {'palette','ground','deco','obj','high','cameraBackdrop'}}
            art={k:[d['palette'][i]for i in d[k]]for k in ['ground','deco','obj','high']}
            self.assertEqual(sha(stable),r['gameplaySHA'],mid);self.assertEqual(sha(art),r['visibleLayersSHA'],mid)
    def test_corner_context_covers_outgoing_frame_without_painting_walkable_cells(self):
        e=dict(kind='exit',target='neighbor',edge='E',x=1,y=5,w=1,h=1)
        cells=set(exterior_cells([e],70,70,{'neighbor'},32,18))
        self.assertIn((-30,5),cells);self.assertNotIn((0,5),cells)
        self.assertEqual(exterior_cells([e],70,70,set()),[])
    def test_no_neighbor_context_without_scene_exits(self):
        self.assertEqual(exterior_cells([],70,70,{'neighbor'}),[])
if __name__=='__main__':unittest.main()
