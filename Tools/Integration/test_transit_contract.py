"""공용 등록부에 실수로 외국 수도를 직결하거나 별도 데이터 참조를 누락하는 회귀 방지."""
import json
import re
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

class TransitContract(unittest.TestCase):
    def test_intermediate_journey_and_independent_towns(self):
        region=json.loads((ROOT/'Assets/PocketCaravan/World/Regions/transit.json').read_text())
        maps={e['mapId']:json.loads((ROOT/e['jsonPath']).read_text()) for e in region['maps']}
        self.assertEqual(len(maps),4)
        for mid,data in maps.items():
            self.assertTrue(mid.startswith('transit_'))
            for o in data['objects']:
                if o['kind']=='exit':self.assertIn(o['target'],set(maps)|{'goryeo_jangdan'})
        database=(ROOT/'Assets/PocketCaravan/Resources/GameDatabase.asset').read_text()
        towns={o['town'] for m in maps.values() for o in m['objects'] if o['kind']=='market'}
        self.assertEqual(towns,{'transit_woodtown','transit_ferry'})
        for p in (ROOT/'Assets/PocketCaravan/Data/Transit').glob('*.asset'):
            if not p.name.startswith(('Town_','Item_')):continue
            guid=re.search(r'^guid: (\S+)$',Path(str(p)+'.meta').read_text(),re.M)[1]
            self.assertIn(guid,database,f'Unregistered trade data: {p.name}')
            self.assertRegex(p.read_text(),r'(?m)^  id: transit_')

if __name__=='__main__':unittest.main()
