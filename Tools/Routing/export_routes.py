"""Export approved B1 walking-time graph. Production stays current until B5."""
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/WorldLayout'))
from world_layout import WorldLayout
from check_distance_calibration import graph_from_roads, fit_ratio, shortest_path, TOWNS
import itertools
import math
import re

BINDINGS = {
 'goryeo/나루': 'naru', 'goryeo/장단': 'goryeo_jangdan', 'goryeo/송악': 'goryeo_songak',
 'goryeo/닥목': 'dakmok', 'goryeo/개경': 'gaegyeong', 'east/동문 마을': 'transit_woodtown',
 'east/북동 포구': 'transit_ferry', 'east/남동 포구': 'transit_southport',
 'east/중앙 길목 마을': 'transit_crossroads',
}


def build(source=None, walking_speed=None, actual_roads=None, actual_distances=None):
    layout = WorldLayout.read(source) if source else WorldLayout.read()
    motor = ROOT / 'Assets/PocketCaravan/Scripts/Player/PlayerMotor.cs'
    speed = walking_speed if walking_speed is not None else float(re.search(r'public float walkSpeed\s*=\s*([\d.]+)f', motor.read_text())[1])
    data = layout.data
    if actual_roads is not None:
        pairs = {frozenset((r['fromKey'], r['toKey'])) for r in data['roads']}
        if len(actual_roads) != len(pairs) or {frozenset((r['fromKey'], r['toKey'])) for r in actual_roads} != pairs:
            raise ValueError('Actual roads must cover each approved edge exactly once')
        converted = []
        for road in actual_roads:
            cells = [tuple(p) for p in road['cells']]
            if not cells or cells[0] != tuple(math.floor(v) for v in layout.towns[road['fromKey']]['position']) or cells[-1] != tuple(math.floor(v) for v in layout.towns[road['toKey']]['position']):
                raise ValueError('Actual road town endpoints differ')
            if any(p not in layout.land or p not in layout.owners for p in cells) or any(max(abs(x-y) for x,y in zip(a,b)) != 1 for a,b in zip(cells,cells[1:])):
                raise ValueError('Actual route leaves assigned land or skips walkable cells')
            converted.append(dict(fromKey=road['fromKey'],toKey=road['toKey'],waypoints=[[x+.5,y+.5] for x,y in cells]))
        data = dict(data, roads=converted)
    graph = graph_from_roads(data, speed)
    coordinates = {}
    for label, filename in TOWNS.items():
        text = (ROOT / f'Assets/PocketCaravan/Data/Goryeo/Town_{filename}.asset').read_text()
        coordinates[label] = tuple(map(int, re.search(r'worldCoord:\s*\{x:\s*(-?\d+),\s*y:\s*(-?\d+)\}', text).groups()))
    samples = []
    for a, b in itertools.combinations(TOWNS, 2):
        seconds, _ = shortest_path(graph, 'goryeo/' + a, 'goryeo/' + b)
        old = sum(abs(x - y) for x, y in zip(coordinates[a], coordinates[b]))
        samples.append((seconds, old))
    if actual_distances is not None:
        expected={frozenset(pair) for pair in itertools.combinations(BINDINGS,2)}
        if len(actual_distances)!=len(expected) or {frozenset((r['fromKey'],r['toKey'])) for r in actual_distances}!=expected:
            raise ValueError('Actual shortest-walk table must cover all 36 pairs')
        lookup={frozenset((r['fromKey'],r['toKey'])):r['seconds'] for r in actual_distances}
        if any(not math.isfinite(v) or v<=0 for v in lookup.values()):raise ValueError('Invalid walking time')
        samples=[(lookup[frozenset(('goryeo/'+a,'goryeo/'+b))],sum(abs(x-y) for x,y in zip(coordinates[a],coordinates[b]))) for a,b in itertools.combinations(TOWNS,2)]
    ratio = fit_ratio(samples)
    result=dict(mode='current', ratio=ratio, walkingCellsPerSecond=speed,
                source='B2 generated cell routes; production mode stays current' if actual_roads is not None else 'B1 schematic; regenerate from B2 actual routes before B5',
                towns=[dict(id=BINDINGS[t['key']], layoutKey=t['key'], position=t['position']) for t in layout.data['towns']],
                roads=[dict(from_=BINDINGS[r['fromKey']], to=BINDINGS[r['toKey']],
                            seconds=sum(math.dist(p, q) for p, q in zip(r['waypoints'], r['waypoints'][1:])) / speed,
                            waypoints=r['waypoints']) for r in data['roads']],
                externalLinks=[dict(x, active=False) for x in layout.data['externalLinks']])
    if actual_distances is not None:
        result['walkingDistances']=[{'from':BINDINGS[r['fromKey']],'to':BINDINGS[r['toKey']],'seconds':r['seconds']} for r in actual_distances]
        result['source']='B2 shortest walks on the complete generated road corridor, including shared junctions; production stays current'
    return result


def serialized(data):
    # `from` is reserved in Python, but the production DTO uses it as a JSON key.
    data = dict(data, roads=[{('from' if k == 'from_' else k): v for k, v in r.items()} for r in data['roads']])
    def fixed(value):
        if isinstance(value,float):return round(value,6)
        if isinstance(value,list):return [fixed(v) for v in value]
        if isinstance(value,dict):return {k:fixed(v) for k,v in value.items()}
        return value
    return json.dumps(fixed(data), ensure_ascii=False, indent=2) + '\n'


if __name__ == '__main__':
    (ROOT / 'Assets/PocketCaravan/Resources/TownRoutes.json').write_text(serialized(build()))
