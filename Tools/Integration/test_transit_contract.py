"""공용 등록부에 실수로 외국 수도를 직결하거나 별도 데이터 참조를 누락하는 회귀 방지."""
import json
import re
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

class TransitContract(unittest.TestCase):
    def test_intermediate_journey_and_independent_towns(self):
        # M4(2026-10-28): 중앙 동쪽 8맵(남동 포구·남쪽 내륙길 추가). 출구는 중앙 동쪽·고려 맵으로만
        region=json.loads((ROOT/'Assets/PocketCaravan/World/Regions/transit.json').read_text())
        goryeo={e['mapId'] for e in json.loads((ROOT/'Assets/PocketCaravan/World/Regions/goryeo.json').read_text())['maps']}
        maps={e['mapId']:json.loads((ROOT/e['jsonPath']).read_text()) for e in region['maps']}
        self.assertEqual(len(maps),8)
        for mid,data in maps.items():
            self.assertTrue(mid.startswith('transit_'))
            for o in data['objects']:
                if o['kind']=='exit' and not o['spawn'].startswith('link_'):self.assertIn(o['target'],set(maps)|goryeo)   # link_: 통합 브랜치의 다른 문명 연결(region_links.json)
        database=(ROOT/'Assets/PocketCaravan/Resources/GameDatabase.asset').read_text()
        towns={o['town'] for m in maps.values() for o in m['objects'] if o['kind']=='market'}
        self.assertEqual(towns,{'transit_woodtown','transit_ferry','transit_southport','transit_crossroads'})   # 11/03 E2
        for p in (ROOT/'Assets/PocketCaravan/Data/Transit').glob('*.asset'):
            if not p.name.startswith(('Town_','Item_')):continue
            guid=re.search(r'^guid: (\S+)$',Path(str(p)+'.meta').read_text(),re.M)[1]
            self.assertIn(guid,database,f'Unregistered trade data: {p.name}')
            self.assertRegex(p.read_text(),r'(?m)^  id: transit_')

if __name__=='__main__':unittest.main()
