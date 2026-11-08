"""11/08 고려 장승(숨은 수집물) 고정 자리: 걸어서 닿고, 맵 가장자리(위 상태줄 밑)에 있지 않아야 한다. 장단은 GoryeoBridges 징검다리로 건넌다."""
import json, re, unittest
from collections import deque
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
A = ROOT / 'Assets/PocketCaravan'


class GoryeoJangseung(unittest.TestCase):
    def fixed(self):
        code = (A / 'Scripts/World/Collectibles.cs').read_text()
        block = code[code.index('"말 없는 장승"'):code.index('Reactions.Region.Arabia')]
        return {m: (int(x), int(y)) for m, x, y in re.findall(r'\{ "(\w+)", new Vector2Int\((\d+), (\d+)\) \}', block)}

    def test_fixed_spots_reachable_and_visible(self):
        bridges = json.loads((A / 'Resources/Goryeo/Bridges/Config.json').read_text())['bridges']
        spots = self.fixed()
        self.assertEqual(set(spots), {'goryeo_jangdan', 'transit_ferry', 'Naru'})
        for mid, (x, yj) in spots.items():
            d = json.loads((A / f'Maps/{mid}.json').read_text()); w, h = d['w'], d['h']
            open_ = set()
            for b in bridges:
                if b['mapId'] == mid: open_ |= {(b['walk'][i], b['walk'][i + 1]) for i in range(0, len(b['walk']), 2)}
            walk = lambda c: 0 <= c[0] < w and 0 <= c[1] < h and (not d['blocked'][(h - 1 - c[1]) * w + c[0]] or c in open_)
            start = [(i % w, h - 1 - i // w) for i in range(w * h) if d['reachable'][i]]
            seen = set(start); q = deque(start)
            while q:
                cx, cy = q.popleft()
                for n in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if n not in seen and walk(n): seen.add(n); q.append(n)
            c = (x, h - 1 - yj)
            self.assertTrue(walk(c), mid)
            self.assertTrue(any(n in seen for n in ((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1))), mid)
            self.assertGreaterEqual(yj, 5, mid)   # 위 상태줄(약 3칸) 밑에 가리지 않게
            self.assertTrue(2 <= x < w - 1, mid)   # 오른쪽·왼쪽은 상태줄이 없어 가장자리 한 칸만 피한다

    def test_stepping_stones_cross_only_water_in_one_line(self):
        from PIL import Image
        for b in json.loads((A / 'Resources/Goryeo/Bridges/Config.json').read_text())['bridges']:
            for p in b['pieces']:
                with Image.open(A / f"Resources/Goryeo/Bridges/Art/{p['art']}.png") as im:
                    self.assertEqual(im.size, (16, 16)); self.assertLessEqual(set(im.convert('RGBA').getchannel('A').tobytes()), {0, 255})
            d = json.loads((A / f"Maps/{b['mapId']}.json").read_text()); w, h = d['w'], d['h']
            cells = [(b['walk'][i], b['walk'][i + 1]) for i in range(0, len(b['walk']), 2)]
            for x, y in cells: self.assertTrue(d['water'][(h - 1 - y) * w + x], (x, y))
            for a, c in zip(cells, cells[1:]): self.assertEqual(abs(a[0] - c[0]) + abs(a[1] - c[1]), 1)


if __name__ == '__main__':
    unittest.main()
