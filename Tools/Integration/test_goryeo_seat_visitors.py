"""Native visitor-art contracts and approved motion provenance; production remains gated."""
import hashlib
import json
import unittest
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ArtReview/Goryeo-Seats-20261105'


class GoryeoSeatVisitorTests(unittest.TestCase):
    def test_native_pose_catalog_preserves_each_original_envelope(self):
        d=json.loads((REVIEW/'Expanded/source.json').read_text())
        self.assertEqual(d['status'],'review')
        self.assertEqual({p['originId']for p in d['poses']},{'goryeo','venice','viking','arabia'})
        names={'goryeo':'tanuki','venice':'venice_cat','viking':'north_reindeer','arabia':'arabia_player_fennec'}
        for p in d['poses']:
            with Image.open(ROOT/p['file']) as pose,Image.open(ROOT/f"Assets/PocketCaravan/Art/Characters/{names[p['originId']]}_tall_d0.png") as old:
                self.assertEqual(pose.size,old.size)
                self.assertEqual(set(pose.convert('RGBA').getchannel('A').tobytes()),{0,255})
            self.assertEqual(p['ppu'],32 if p['originId']=='arabia' else 16)

    def test_fennec_retains_actual_face_ears_coat_and_belt_pixels(self):
        with Image.open(REVIEW/'Expanded/arabia-seated-front.png') as new,Image.open(ROOT/'Assets/PocketCaravan/Art/Characters/arabia_player_fennec_tall_d0.png') as old:
            # Comparison only: never cuts a source asset for use in the game.
            a=new.convert('RGBA');b=old.convert('RGBA')
            for y in range(31):
                for x in range(32):self.assertEqual(a.getpixel((x,y)),b.getpixel((x,y)),(x,y))
            self.assertNotEqual(a.tobytes(),b.tobytes())

    def test_approved_motion_keeps_original_whole_frame_pixels(self):
        approval=json.loads((REVIEW/'Motion/approval.json').read_text())
        self.assertEqual(approval['status'],'approved')
        for ref in approval['originalFlamePixels']:
            with Image.open(ROOT/ref['path']) as im:
                self.assertEqual(hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest(),ref['rgbaSha256'])


if __name__=='__main__':unittest.main()
