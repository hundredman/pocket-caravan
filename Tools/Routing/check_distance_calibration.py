"""Read-only E1 feasibility check. Exit 2 means the approved tolerance is infeasible.

This does not select production rounding or alter roads, prices, contracts or saves.
B1 schematic paths are deliberately used as required by Decisions section 4.
"""
import argparse
import hashlib
import heapq
import itertools
import json
import math
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/WorldLayout'))
from world_layout import WorldLayout

TOWNS = {
    '나루': 'Naru', '장단': 'GoryeoJangdan', '송악': 'GoryeoSongak',
    '닥목': 'Dakmok', '개경': 'Gaegyeong',
}


def graph_from_roads(data, speed):
    if not math.isfinite(speed) or speed <= 0:
        raise ValueError('Walking speed must be positive and finite')
    graph = {t['key']: [] for t in data['towns']}
    for road in data['roads']:
        a, b = road['fromKey'], road['toKey']
        points = road['waypoints']
        if len(points) < 2 or a not in graph or b not in graph:
            raise ValueError('Invalid road')
        cost = sum(math.dist(p, q) for p, q in zip(points, points[1:])) / speed
        if not math.isfinite(cost) or cost <= 0:
            raise ValueError('Road time must be positive and finite')
        graph[a].append((b, cost))
        graph[b].append((a, cost))
    return graph


def shortest_path(graph, start, end):
    if start not in graph or end not in graph:
        raise ValueError('Unknown town')
    queue = [(0.0, start, [start])]
    best = {start: 0.0}
    while queue:
        cost, town, path = heapq.heappop(queue)
        if cost > best[town]:
            continue
        if town == end:
            return cost, path
        for neighbor, time in graph[town]:
            candidate = cost + time
            if candidate < best.get(neighbor, math.inf):
                best[neighbor] = candidate
                heapq.heappush(queue, (candidate, neighbor, path + [neighbor]))
    raise ValueError('Disconnected towns')


def fit_ratio(samples):
    """Unweighted least squares through origin: distance = k * walking seconds."""
    denominator = sum(seconds * seconds for seconds, old_distance in samples)
    if not denominator or not samples:
        raise ValueError('No nonzero travel-time samples')
    return sum(seconds * distance for seconds, distance in samples) / denominator


def deadline(distance):
    return 9 * max(1, distance) + 8


def allowed_ratio(seconds, old_distance, rounded=False):
    """Tolerance interval; rounded variant is a conservative closed superset.

    Rounding intervals include half-integer endpoints regardless of tie policy.
    Empty intersection of these supersets proves infeasibility for nearest rounding.
    """
    if seconds <= 0 or old_distance < 1:
        raise ValueError('Expected positive time and legacy distance')
    old_hours = deadline(old_distance)
    low_distance = (old_hours * .9 - 8) / 9
    high_distance = (old_hours * 1.1 - 8) / 9
    if rounded:
        first = max(1, math.ceil(low_distance))
        last = math.floor(high_distance)
        if first > last:
            return [math.inf, -math.inf]
        return [0 if first == 1 else (first - .5) / seconds,
                (last + .5) / seconds]
    return [0 if low_distance <= 1 else low_distance / seconds,
            high_distance / seconds]


def intersection(intervals):
    return [max(a for a, b in intervals), min(b for a, b in intervals)]


def make_report(root=ROOT):
    inputs = []

    def read(relative):
        # Historical feasibility report uses frozen pre-E1 formulas after the service refactor.
        if relative in ('Assets/PocketCaravan/Scripts/Contracts/ContractManager.cs',
                        'Assets/PocketCaravan/Scripts/Data/ItemData.cs'):
            relative = 'Docs/Validation/E1-2026-10-25/legacy-source/' + Path(relative).name
        path = root / relative
        inputs.append(relative)
        return path.read_text(encoding='utf-8')

    layout_relative = 'Tools/WorldLayout/world-layout.json'
    data = json.loads(read(layout_relative))
    WorldLayout(data)  # existing B1 validation before computing any route
    motor = read('Assets/PocketCaravan/Scripts/Player/PlayerMotor.cs')
    speed = float(re.search(r'public float walkSpeed\s*=\s*([\d.]+)f', motor)[1])
    contracts = read('Assets/PocketCaravan/Scripts/Contracts/ContractManager.cs')
    items = read('Assets/PocketCaravan/Scripts/Data/ItemData.cs')
    # Fail rather than silently analyze stale formulas after a production change.
    if not re.search(r'hours\s*=\s*dist\s*\*\s*9\s*\+\s*8', contracts):
        raise ValueError('Deadline formula changed; review the probe')
    if 'int dist = Mathf.Max(1, Distance(from, to));' not in contracts:
        raise ValueError('Contract distance clamp changed')
    for source in (contracts, items):
        if 'Mathf.Abs(d.x) + Mathf.Abs(d.y)' not in source:
            raise ValueError('Legacy Manhattan formula changed')
    coordinates = {}
    for label, filename in TOWNS.items():
        text = read(f'Assets/PocketCaravan/Data/Goryeo/Town_{filename}.asset')
        match = re.search(r'worldCoord:\s*\{x:\s*(-?\d+),\s*y:\s*(-?\d+)\}', text)
        if not match:
            raise ValueError('Missing town coordinates')
        coordinates[label] = tuple(map(int, match.groups()))
    graph = graph_from_roads(data, speed)
    # All 9 internal towns must be connected; external placeholders are excluded.
    all_routes = []
    for a, b in itertools.combinations(sorted(graph), 2):
        seconds, path = shortest_path(graph, a, b)
        all_routes.append({'from': a, 'to': b, 'seconds': seconds, 'path': path})
    rows = []
    for a, b in itertools.combinations(TOWNS, 2):
        seconds, path = shortest_path(graph, 'goryeo/' + a, 'goryeo/' + b)
        distance = sum(abs(x - y) for x, y in zip(coordinates[a], coordinates[b]))
        rows.append({'from': a, 'to': b, 'legacyDistance': distance,
                     'seconds': seconds, 'path': path})
    ratio = fit_ratio([(r['seconds'], r['legacyDistance']) for r in rows])
    for row in rows:
        distance = row['seconds'] * ratio
        old = deadline(row['legacyDistance'])
        row.update(calibratedDistance=distance, oldHours=old,
                   continuousHours=deadline(distance),
                   continuousDeltaPercent=100 * (deadline(distance) / old - 1),
                   nearestHours=deadline(round(distance)),
                   nearestDeltaPercent=100 * (deadline(round(distance)) / old - 1),
                   allowedRatioContinuous=allowed_ratio(row['seconds'], row['legacyDistance']),
                   allowedRatioNearestSuperset=allowed_ratio(row['seconds'], row['legacyDistance'], True))
    ranges = {key: intersection([r[key] for r in rows]) for key in
              ('allowedRatioContinuous', 'allowedRatioNearestSuperset')}
    return {
        'schemaVersion': 1, 'mode': 'analysis_only_no_runtime_changes',
        'basis': 'B1 schematic polylines; uniform walking speed; no events, acceleration or fade overhead',
        'walkingCellsPerSecond': speed, 'leastSquaresRatio': ratio,
        'deadlineFormula': '9 * max(1, distance) + 8',
        'feasibleContinuous': ranges['allowedRatioContinuous'][0] <= ranges['allowedRatioContinuous'][1],
        'feasibleNearestRounding': ranges['allowedRatioNearestSuperset'][0] <= ranges['allowedRatioNearestSuperset'][1],
        'ratioIntersection': ranges, 'goryeoPairs': rows, 'internalRoutes': all_routes,
        'inputsSha256': {p: hashlib.sha256((root / p).read_bytes()).hexdigest() for p in sorted(inputs)},
        'limitations': [
            'Fractional vs integer runtime distance is not selected; both alternatives fail.',
            'No production pricing/reward/deadline change; full economic acceptance not completed.',
            'No Unity execution, physical pathfinding, save regression or human play in this check.',
            'B2 must recalculate from traversable road lengths after the design conflict is resolved.',
        ],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, help='Explicit analysis output path')
    args = parser.parse_args()
    report = make_report()
    if args.out:
        args.out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: report[key] for key in (
        'leastSquaresRatio', 'feasibleContinuous', 'feasibleNearestRounding', 'ratioIntersection')},
        ensure_ascii=False, indent=2))
    return 0 if report['feasibleContinuous'] else 2


if __name__ == '__main__':
    raise SystemExit(main())
