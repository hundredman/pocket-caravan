"""11/09 승인된 손수레 그림(HandcartV2): 게임 그림 = 코드 도트 생성기 결과 = 원본 .aseprite."""
import json, sys, tempfile, unittest
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[2]
PKG = ROOT / 'ArtReview/Approved/HandcartV2'
sys.path.insert(0, str(ROOT / 'Tools/ArtGen'))


class HandcartArt(unittest.TestCase):
    def test_sources_and_generator_match_production(self):
        import aseprite_rgba, handcart_v3
        assets = json.loads((PKG / 'approval.json').read_text())['assets']
        frames = {}
        for a in assets:
            frames.setdefault(a['source'], []).append(a)
        for src, rows in frames.items():
            got = aseprite_rgba.read(ROOT / src)
            self.assertEqual(len(got), len(rows), src)
            for im, a in zip(got, rows):
                with Image.open(ROOT / a['production']) as p:
                    self.assertEqual(im.tobytes(), p.convert('RGBA').tobytes(), a['key'])
        gen = {f'{v}_{s}': getattr(handcart_v3, v)(s).im for v in ('side', 'front', 'back') for s in range(4)}
        gen['side_handle'] = handcart_v3.side_handle().im
        for a in assets:
            with Image.open(ROOT / a['production']) as p:
                self.assertEqual(gen[a['key']].tobytes(), p.convert('RGBA').tobytes(), a['key'])
            self.assertEqual(set(gen[a['key']].getchannel('A').tobytes()) - {0, 255}, set())


if __name__ == '__main__':
    unittest.main()
