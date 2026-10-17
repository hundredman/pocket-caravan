"""Product-scope regressions: roads cannot substitute for the four starting villages."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class GoryeoScopeContract(unittest.TestCase):
    def test_capital_and_four_distinct_villages_are_registered_and_in_database(self):
        region = json.loads((ROOT / 'Assets/PocketCaravan/World/Regions/goryeo.json').read_text())
        markets = set()
        for entry in region['maps']:
            data = json.loads((ROOT / entry['jsonPath']).read_text())
            markets.update(obj['town'] for obj in data['objects'] if obj['kind'] == 'market')
        self.assertIn('gaegyeong', markets)
        self.assertGreaterEqual(len(markets - {'gaegyeong'}), 4, 'Need four villages; road scenes do not count')
        database = (ROOT / 'Assets/PocketCaravan/Resources/GameDatabase.asset').read_text()
        towns = {}
        for path in (ROOT / 'Assets/PocketCaravan/Data/Goryeo').glob('Town_*.asset'):
            text = path.read_text()
            ident = re.search(r'^  id: (\S+)$', text, re.M).group(1)
            guid = re.search(r'^guid: (\S+)$', Path(str(path)+'.meta').read_text(), re.M).group(1)
            towns[ident] = guid
        for ident in markets:
            self.assertIn(ident, towns, f'Market without TownData: {ident}')
            self.assertIn(towns[ident], database, f'TownData not included in common database: {ident}')

    def test_town_tiers_and_personal_credit_facilities_have_real_data(self):
        facility_ids = []
        kinds = set()
        credit_gates = set()
        for path in (ROOT / 'Assets/PocketCaravan/Data/Goryeo').glob('Town_*.asset'):
            text = path.read_text()
            tier = re.search(r'^  settlementTier: (\d+)$', text, re.M)
            self.assertIsNotNone(tier, f'Missing settlement tier: {path.name}')
            self.assertIn(int(tier.group(1)), range(1,5))
            entries = re.findall(r'^  - id: (\S+)\n(.*?)(?=^  - id:|^  stock:)', text, re.M|re.S)
            self.assertTrue(entries, f'No facilities: {path.name}')
            for ident, block in entries:
                facility_ids.append(ident)
                kind = int(re.search(r'^    kind: (\d+)$', block, re.M).group(1))
                minimum = int(re.search(r'^    minTier: (\d+)$', block, re.M).group(1))
                credit = int(re.search(r'^    minCredit: (-?\d+)$', block, re.M).group(1))
                kinds.add(kind)
                self.assertLessEqual(minimum, int(tier.group(1)), f'Facility never available in its town: {ident}')
                if credit > 0: credit_gates.add(kind)
        self.assertEqual(len(facility_ids),len(set(facility_ids)), 'Duplicate facility IDs')
        self.assertTrue({0,1,2} <= kinds, 'Rest/information/storage service data missing')
        self.assertIn(2,credit_gates, 'Storage should demonstrate a personal-credit gate')


if __name__ == '__main__':
    unittest.main()
