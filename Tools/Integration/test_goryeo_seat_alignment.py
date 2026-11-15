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
        # 2026-11-15 기우: 아라비아 캐릭터를 16x24(PPU 16)로 통일(팀 승인). 앉은 사막여우도 순록(viking)과 같은 방법:
        # 머리·목(0~15줄)은 그대로 2줄 내리고, 위 2줄은 비우고, 접은 무릎은 서 있을 때보다 넓다
        with Image.open(REVIEW/'arabia-seated-front.png')as new,Image.open(ROOT/'Assets/PocketCaravan/Art/Characters/arabia_player_fennec_tall_d0.png')as old:
            a=new.convert('RGBA');b=old.convert('RGBA')
            self.assertEqual(a.size,b.size)
            self.assertEqual(a.size,(16,24))
            for y in range(16):
                for x in range(16):self.assertEqual(a.getpixel((x,y+2)),b.getpixel((x,y)),(x,y))
            self.assertFalse(any(a.getpixel((x,y))[3]for y in range(2)for x in range(16)))
            pants={(59,75,126)}
            folded=sum(a.getpixel((x,22))[:3]in pants for x in range(16))
            standing=sum(b.getpixel((x,22))[:3]in pants for x in range(16))
            self.assertGreater(folded,standing)

if __name__=='__main__':unittest.main()
