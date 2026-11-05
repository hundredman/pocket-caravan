"""Regress the rejected crooked torso and near-standing fennec pose, in decoded pixels."""
from pathlib import Path
import unittest
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ArtReview/Goryeo-Seats-20261105/AlignedV2'


class SeatAlignmentTests(unittest.TestCase):
    def test_torso_knees_and_shoes_follow_head_centerline(self):
        for key in ['goryeo','venice','viking']:
            with Image.open(REVIEW/f'{key}-seated-front.png')as im:
                rgba=im.convert('RGBA')
                for y in range(19,24):
                    alpha=[rgba.getpixel((x,y))[3] for x in range(16)]
                    self.assertEqual(alpha,alpha[::-1],(key,y))
                    xs=[x for x in range(16)if alpha[x]]
                    self.assertEqual((min(xs)+max(xs))/2,7.5,(key,y))

    def test_fennec_lowers_intact_head_and_widens_folded_knees(self):
        with Image.open(REVIEW/'arabia-seated-front.png')as new,Image.open(ROOT/'Assets/PocketCaravan/Art/Characters/arabia_player_fennec_tall_d0.png')as old:
            a=new.convert('RGBA');b=old.convert('RGBA')
            self.assertEqual(a.size,b.size)
            for y in range(26):
                for x in range(32):self.assertEqual(a.getpixel((x,y+4)),b.getpixel((x,y)),(x,y))
            self.assertFalse(any(a.getpixel((x,y))[3]for y in range(4)for x in range(32)))
            pants={(59,75,126),(108,118,152),(59,61,90)}
            folded=sum(a.getpixel((x,34))[:3]in pants for x in range(32))
            standing=sum(b.getpixel((x,34))[:3]in pants for x in range(32))
            self.assertGreater(folded,standing)


if __name__=='__main__':unittest.main()
