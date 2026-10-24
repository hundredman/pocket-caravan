"""실제 내보낸 콘텐츠에 선택 경로·학습 안전·새 거래처 목표가 남아 있는지 검사."""
import json
import subprocess
import sys
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

class GoryeoExperienceContract(unittest.TestCase):
    def test_broad_regions_keep_route_loops_and_vehicle_reachability(self):
        result=subprocess.run([sys.executable,str(ROOT/'Tools/Integration/analyze_journey.py')],capture_output=True,text=True,check=True)
        data=json.loads(result.stdout)
        self.assertGreaterEqual(data['goryeoGraph']['independentCycles'],2)
        route=next(r for r in data['routes'] if r['from']=='goryeo_songak' and r['to']=='Dakmok')
        # Retired road scenes are now inside the five larger maps. Vehicle differences
        # can occur within a map, so a different scene-name sequence is no longer required.
        self.assertEqual(data['goryeoGraph']['nodes'],5)
        for vehicle in ('Walk','Handcart'):
            self.assertEqual(route[vehicle]['maps'][0],'goryeo_songak')
            self.assertEqual(route[vehicle]['maps'][-1],'Dakmok')
            self.assertGreater(route[vehicle]['seconds'],0)
            self.assertNotIn('Route01',route[vehicle]['maps'])

    def test_intro_region_has_no_random_bandit_loss_and_frontier_is_signposted(self):
        region=json.loads((ROOT/'Assets/PocketCaravan/World/Regions/goryeo.json').read_text())
        for entry in region['maps']:
            data=json.loads((ROOT/entry['jsonPath']).read_text())
            self.assertFalse(any(o['kind']=='bandit' for o in data['objects']),entry['mapId'])
        data=json.loads((ROOT/'Assets/PocketCaravan/Maps/transit_pass.json').read_text())
        self.assertTrue(any(o['kind']=='bandit' for o in data['objects']))
        self.assertTrue(any(o['kind']=='sign' and '도적' in o['text'] for o in data['objects']))

    def test_new_optional_quests_are_registered_with_stable_region_givers(self):
        database=(ROOT/'Assets/PocketCaravan/Resources/GameDatabase.asset').read_text()
        manifest=json.loads((ROOT/'Assets/PocketCaravan/World/Regions/goryeo.json').read_text())
        givers={o['id'] for o in manifest['npcs']}
        for name in ['Goryeo_SongakTrade','Goryeo_JangdanTrade','Goryeo_CartSupply','Goryeo_ReturnTrade']:
            p=ROOT/f'Assets/PocketCaravan/Data/Goryeo/Quests/{name}.asset'
            text=p.read_text()
            import re
            giver=re.search(r'^  giverNpc: (\S+)$',text,re.M)[1]
            self.assertIn(giver,givers)
            guid=re.search(r'^guid: (\S+)$',Path(str(p)+'.meta').read_text(),re.M)[1]
            self.assertIn(guid,database)

if __name__=='__main__':unittest.main()
