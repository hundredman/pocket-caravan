"""장터 기준 공급품 규칙 (2026-11-05, 통합·기우. Docs/EconomyModel.md §8)
마을마다: 5개 이상 · 자기 지역 산지 물건(적정 재고 20↑) 1개 이상 · 생필품(산지 없는 공용 물건) 1개 이상 · 다른 지역 물건 0개.
같은 문명권(북유럽+중앙북, 베네치아+본토, 고려+중간권)은 자기 지역으로 친다. 중앙 조각(허브)은 산지가 없어도 되고 다른 문명권 물건을 4개까지 둘 수 있다.
"""
from pathlib import Path
import re, glob, os, unittest

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'Assets/PocketCaravan/Data'
HUB = {'center_north', 'center_venice', 'transit'}
# R2 고정 자산(은샘 검사가 해시로 묶음)이라 통합 담당이 못 고치는 마을 — 담당이 채울 때까지 개수 조건만 봐준다
FROZEN_COUNT_OK = {'Town_GoryeoJangdan.asset'}
# 같은 문명권 = 자기 지역으로 친다 (북유럽 소금 산지가 북문 마을(중앙북)이듯 산지가 중앙 조각에 있는 물건이 있다)
GROUP = {'north': 'N', 'center_north': 'N', 'venice': 'V', 'center_venice': 'V', 'goryeo': 'G', 'transit': 'G', 'arabia': 'A'}


def field(text, key):
    m = re.search(r'^  ' + key + r': (.*)$', text, re.M)
    return m.group(1) if m else ''


class MarketRule(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.by_guid = {}
        for meta in glob.glob(str(DATA / '*/*.asset.meta')):
            cls.by_guid[Path(meta).read_text().split('guid: ')[1].split()[0]] = meta[:-5]
        cls.region_id = {}
        for p in glob.glob(str(DATA / '*/Region_*.asset')):
            cls.region_id[p] = field(Path(p).read_text(encoding='utf-8'), 'id')

    def town_region(self, town_path):
        g = re.search(r'guid: (\w+)', field(Path(town_path).read_text(encoding='utf-8'), 'region'))
        return self.region_id.get(self.by_guid.get(g.group(1), ''), '?') if g else '?'

    def item_region(self, item_path):
        text = Path(item_path).read_text(encoding='utf-8')
        g = re.search(r'guid: (\w+)', field(text, 'originTown'))
        if not g: return None          # 생필품
        return self.town_region(self.by_guid[g.group(1)])

    def test_every_market_follows_rule(self):
        problems = []
        for town in sorted(glob.glob(str(DATA / '*/Town_*.asset'))):
            text = Path(town).read_text(encoding='utf-8')
            rows = re.findall(r'- item: \{fileID: \d+, guid: (\w+).*\}\n    optimal: ([\d.]+)', text)
            if not rows: continue   # 장터 없는 마을
            own = self.town_region(town)
            name = os.path.basename(town)
            foreign, staple, local_src = [], 0, 0
            for g, opt in rows:
                r = self.item_region(self.by_guid[g])
                if r is None:
                    staple += 1
                    if float(opt) >= 20: local_src += 1   # 생필품 산지(장단 곡식·눈숲 땔감)도 산지로 친다
                elif GROUP.get(r) == GROUP.get(own):
                    if float(opt) >= 20: local_src += 1
                else: foreign.append(os.path.basename(self.by_guid[g]))
            if len(rows) < 5 and name not in FROZEN_COUNT_OK: problems.append(f'{name}: 품목 {len(rows)}개 (5개 이상이어야 함)')
            if local_src == 0 and own not in HUB: problems.append(f'{name}: 산지 물건(적정 재고 20 이상) 없음')   # 길목 마을(허브)은 산지가 없어도 된다
            if staple == 0: problems.append(f'{name}: 생필품(땔감·곡식) 없음')
            limit = 4 if own in HUB else 0
            if len(foreign) > limit: problems.append(f'{name}: 다른 지역 물건 {foreign} ({limit}개까지)')
        self.assertEqual(problems, [], '\n' + '\n'.join(problems))


if __name__ == '__main__':
    unittest.main()
