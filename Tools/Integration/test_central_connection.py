"""A chain of independently editable maps reaches the junction; foreign arms stay unconnected."""
import collections
import json
import heapq
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class CentralConnection(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.maps = {}
        for path in (ROOT/'Assets/PocketCaravan/World/Regions').glob('*.json'):
            for entry in json.loads(path.read_text())['maps']:
                cls.maps[entry['mapId']] = json.loads((ROOT/entry['jsonPath']).read_text())

    def test_goryeo_reaches_central_through_multiple_distinct_maps(self):
        expected=['goryeo_jangdan','transit_pass','transit_woodtown','transit_wetroad','transit_ferry','transit_central_approach','transit_crossroads']
        queue=collections.deque([['goryeo_jangdan']]);seen=set()
        while queue:
            route=queue.popleft();mid=route[-1]
            if mid in seen:continue
            seen.add(mid)
            if mid=='transit_crossroads':break
            queue.extend(route+[o['target']] for o in self.maps[mid]['objects'] if o['kind']=='exit')
        else:self.fail('Central junction unreachable')
        self.assertEqual(route,expected)
        self.assertTrue(all(self.maps[mid]['w']<=72 for mid in expected))

    def test_new_connections_have_reciprocal_equal_width_spawns(self):
        for mid in ['transit_ferry','transit_central_approach','transit_crossroads']:
            m=self.maps[mid]
            for e in m['objects']:
                if e['kind']!='exit':continue
                target=self.maps[e['target']]
                spawn=next(s for s in target['objects'] if s['kind']=='spawn' and s['id']==e['spawn'])
                self.assertEqual(max(e['w'],e['h']),max(spawn['w'],spawn['h']))
                self.assertTrue(any(s['kind']=='exit' and s['target']==mid for s in target['objects']))

    def test_three_future_arms_have_no_invalid_scene_exit(self):
        m=self.maps['transit_crossroads'];ports=json.loads((ROOT/'Assets/PocketCaravan/World/Connections/central_ports.json').read_text())
        exits=[o for o in m['objects'] if o['kind']=='exit']
        self.assertEqual([(e['edge'],e['target']) for e in exits],[('S','transit_central_approach')])
        self.assertEqual({p['edge'] for p in ports['ports']},{'N','E','W','S'})
        self.assertEqual({p['edge'] for p in ports['ports'] if p['status']=='closed'},{'N','E','W'})
        for port in ports['ports']:
            if port['status']=='open':
                e=next(e for e in exits if e['edge']==port['edge'])
                self.assertEqual(e['spawn'],port['targetSpawn']);self.assertEqual(max(e['w'],e['h']),port['width'])
            else:
                self.assertIsNone(port['target'])
                self.assertTrue(any(o['kind']=='sign' and {'N':'북쪽','E':'동쪽','W':'서쪽'}[port['edge']] in o['text'] and '통행 불가' in o['text'] for o in m['objects']))

    def test_ferry_is_no_longer_signposted_as_terminus(self):
        text=' '.join(o.get('text','') for o in self.maps['transit_ferry']['objects'])
        self.assertIn('중앙 십자로',text)
        self.assertNotIn('시험 여정의 끝',text)

    def test_approach_supports_different_vehicle_path_choices(self):
        m=self.maps['transit_central_approach'];w,h=m['w'],m['h']
        def cost(speeds,branch):
            queue=[(0.,(7,20))];seen=set()
            while queue:
                c,(x,y)=heapq.heappop(queue)
                if (x,y) in seen:continue
                seen.add((x,y))
                if (x,y)==(35,20):return c
                for a,b in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
                    if not (0<=a<w and 0<=b<h) or m['blocked'][b*w+a] or not m['road'][b*w+a]:continue
                    if 12<=a<=32 and ((b>=23)!=(branch=='main')):continue
                    heapq.heappush(queue,(c+.5/speeds[m['road'][y*w+x]]+.5/speeds[m['road'][b*w+a]],(a,b)))
            self.fail('unreachable branch '+branch)
        for vehicle in ['Walk','Handcart']:
            text=(ROOT/f'Assets/PocketCaravan/Data/Goryeo/Vehicle_{vehicle}.asset').read_text()
            speeds=[float(v) for v in re.findall(r'^  - ([0-9.]+)$',text.split('speedByGrade:')[1],re.M)]
            short,main=cost(speeds,'short'),cost(speeds,'main')
            if vehicle=='Walk':self.assertLess(short,main)
            else:self.assertLess(main,short)

    def test_no_extra_trade_town_or_bandit_added_in_connector_maps(self):
        for mid in ['transit_central_approach','transit_crossroads']:
            self.assertFalse(any(o['kind'] in ['market','board','bandit'] for o in self.maps[mid]['objects']))

if __name__=='__main__':unittest.main()
