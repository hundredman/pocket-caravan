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


def build(source=None, walking_speed=None):
    layout = WorldLayout.read(source) if source else WorldLayout.read()
    motor = ROOT / 'Assets/PocketCaravan/Scripts/Player/PlayerMotor.cs'
    speed = walking_speed if walking_speed is not None else float(re.search(r'public float walkSpeed\s*=\s*([\d.]+)f', motor.read_text())[1])
    graph = graph_from_roads(layout.data, speed)
    coordinates = {}
    for label, filename in TOWNS.items():
        text = (ROOT / f'Assets/PocketCaravan/Data/Goryeo/Town_{filename}.asset').read_text()
        coordinates[label] = tuple(map(int, re.search(r'worldCoord:\s*\{x:\s*(-?\d+),\s*y:\s*(-?\d+)\}', text).groups()))
    samples = []
    for a, b in itertools.combinations(TOWNS, 2):
        seconds, _ = shortest_path(graph, 'goryeo/' + a, 'goryeo/' + b)
        old = sum(abs(x - y) for x, y in zip(coordinates[a], coordinates[b]))
        samples.append((seconds, old))
    ratio = fit_ratio(samples)
    return dict(mode='current', ratio=ratio, walkingCellsPerSecond=speed,
                source='B1 schematic; regenerate from B2 actual routes before B5',
                towns=[dict(id=BINDINGS[t['key']], layoutKey=t['key'], position=t['position']) for t in layout.data['towns']],
                roads=[dict(from_=BINDINGS[r['fromKey']], to=BINDINGS[r['toKey']],
                            seconds=sum(math.dist(p, q) for p, q in zip(r['waypoints'], r['waypoints'][1:])) / speed,
                            waypoints=r['waypoints']) for r in layout.data['roads']],
                externalLinks=[dict(x, active=False) for x in layout.data['externalLinks']])


def serialized(data):
    # `from` is reserved in Python, but the production DTO uses it as a JSON key.
    data = dict(data, roads=[{('from' if k == 'from_' else k): v for k, v in r.items()} for r in data['roads']])
    return json.dumps(data, ensure_ascii=False, indent=2) + '\n'


if __name__ == '__main__':
    (ROOT / 'Assets/PocketCaravan/Resources/TownRoutes.json').write_text(serialized(build()))
