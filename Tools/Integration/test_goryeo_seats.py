"""Review geometry and native-pixel contract, without requiring Unity."""
import json
import unittest
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ArtReview/Goryeo-Seats-20261105'


class GoryeoSeatsTests(unittest.TestCase):
    def test_seat_feet_are_on_clear_ground_away_from_transition_cells(self):
        config=json.loads((REVIEW/'source.json').read_text())
        self.assertEqual(config['status'],'review')
        for s in config['seats']:
            m=json.loads((ROOT/f"Assets/PocketCaravan/Maps/{s['mapId']}.json").read_text())
            x,y=s['x'],s['y'];i=y*m['w']+x
            self.assertGreater(m['ground'][i],0)
            self.assertEqual(m['blocked'][i],0)
            for o in m['objects']:
                if o['kind'] not in ['exit','spawn']:continue
                self.assertFalse(o['x']<=x<o['x']+o['w'] and o['y']<=y<o['y']+o['h'])

    def test_pose_is_native_16_by_24_with_binary_alpha(self):
        with Image.open(REVIEW/'tanuki-seated-front.png') as im:
            self.assertEqual(im.size,(16,24))
            self.assertEqual(im.mode,'RGBA')
            alpha=im.getchannel('A').tobytes()
            self.assertEqual(set(alpha),{0,255})
            self.assertGreater(alpha.count(255),100)


if __name__=='__main__':unittest.main()
