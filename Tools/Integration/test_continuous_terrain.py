"""Runtime geography, full scene-border walking, and atlas water footprint must agree."""
import collections
import json
import unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]

class ContinuousTerrain(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        entries=[]
        for region in ('goryeo','transit'):
            entries+=json.loads((ROOT/f'Assets/PocketCaravan/World/Regions/{region}.json').read_text())['maps']
        cls.maps={e['mapId']:json.loads((ROOT/e['jsonPath']).read_text()) for e in entries}

    def test_open_ground_across_each_shared_border_has_reciprocal_exits(self):
        # M4(2026-10-28): 맵 직사각형이 겹치므로 세계 좌표로 본다. 맵 A의 걸어서 닿는 칸(reachable) 옆(상하좌우)이 맵 B의 걸어서 닿는 칸이면
        # 그 칸에는 B로 가는 출구가 있어야 하고(보이지 않는 벽 없음), B에도 A행 출구가 있어야 한다.
        # 대각선으로만 닿는 1칸 돌기처럼 자기 맵에서도 닿지 않는 칸은 뺀다.
        step={'N':(0,-1),'S':(0,1),'W':(-1,0),'E':(1,0)}
        owner={}
        for mid,m in self.maps.items():
            for i,b in enumerate(m['blocked']):
                if not b and m['reachable'][i]:owner[(m['terrainX']+i%m['w'],m['terrainY']+i//m['w'])]=mid
        covered={}
        for mid,m in self.maps.items():
            for e in m['objects']:
                if e['kind']!='exit':continue
                for i in range(e['w']):
                    for j in range(e['h']):covered.setdefault((mid,m['terrainX']+e['x']+i,m['terrainY']+e['y']+j),set()).add(e['target'])
        missing=[]
        for (x,y),a in owner.items():
            for edge,(dx,dy) in step.items():
                b=owner.get((x+dx,y+dy))
                if b and b!=a and b not in covered.get((a,x,y),()):missing.append((a,b,x,y,edge))
        self.assertEqual(missing[:5],[])
        for mid,m in self.maps.items():
            for e in m['objects']:
                if e['kind']=='exit':
                    target=self.maps[e['target']]
                    self.assertTrue(any(o['kind']=='exit' and o['target']==mid for o in target['objects']),(mid,e['target']))

    def test_reachability_snapshot_matches_world_blocking_geometry(self):
        # M4(2026-10-28): 맵의 reachable은 그 맵 안에서 길과 이어진 칸이다. 그런 칸은 모두 시작 위치(나루 S0)에서 걸어서 닿아야 한다.
        ground={(m['terrainX']+x,m['terrainY']+y) for m in self.maps.values() for y in range(m['h']) for x in range(m['w']) if not m['blocked'][y*m['w']+x]}
        m=self.maps['Naru'];sp=next(o for o in m['objects'] if o['kind']=='spawn' and o['id']=='S0')
        seen={(m['terrainX']+sp['x'],m['terrainY']+sp['y'])};queue=collections.deque(seen)
        while queue:
            x,y=queue.popleft()
            for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if p in ground and p not in seen:seen.add(p);queue.append(p)
        for m in self.maps.values():
            for y in range(m['h']):
                for x in range(m['w']):
                    i=y*m['w']+x
                    if m['reachable'][i]:
                        self.assertEqual(m['blocked'][i],0,(m['id'],x,y))
                        self.assertIn((m['terrainX']+x,m['terrainY']+y),seen,(m['id'],x,y))
                    if m['road'][i] and not m['blocked'][i]:self.assertTrue(m['reachable'][i],(m['id'],x,y))

    def test_atlas_hydrology_pixels_equal_all_runtime_water_cells(self):
        # M4(2026-10-28): 전체 지도(Goryeo)는 담당 칸(바닥 그림이 있는 칸)마다 물 표시가 맵 물과 같다.
        # 고려 확대 지도(GoryeoRegional)는 고려 5맵 칸만 그리므로, 물로 표시된 칸은 맵에서도 물이어야 한다.
        base=ROOT/'Assets/PocketCaravan/Resources/WorldMap'
        for stem in ['Goryeo','GoryeoRegional']:
            atlas=json.loads((base/(stem+'Layout.json')).read_text());scale=atlas['pixelsPerTile']
            with Image.open(base/atlas['waterMask']) as image:
                mask=image.convert('L')
                for a in atlas['maps']:
                    m=self.maps[a['mapId']]
                    for y in range(m['h']):
                        for x in range(m['w']):
                            i=y*m['w']+x
                            actual=mask.getpixel((int((a['x']+x)*scale+scale/2),int((a['y']+y)*scale+scale/2)))
                            if stem=='Goryeo' and m['ground'][i]:self.assertEqual(actual,255 if m['water'][i] else 0,(m['id'],x,y))
                            if actual:self.assertTrue(m['water'][i],(stem,m['id'],x,y))

    def test_settlements_and_cargo_scope_are_not_replaced_by_landscape_sectors(self):
        markets={o['town'] for m in self.maps.values() for o in m['objects'] if o['kind']=='market'}
        self.assertEqual(markets,{'naru','gaegyeong','dakmok','goryeo_songak','goryeo_jangdan','transit_woodtown','transit_ferry'})
        self.assertEqual(len([m for m in self.maps if not m.startswith('transit_')]),5)
        self.assertNotIn('Route01',self.maps)

if __name__=='__main__':unittest.main()
