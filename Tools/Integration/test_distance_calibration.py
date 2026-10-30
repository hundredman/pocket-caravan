"""E1: detect infeasible requirements without altering production economy."""
import json
import math
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/Routing'))
from check_distance_calibration import (
    allowed_ratio, deadline, fit_ratio, graph_from_roads, intersection, make_report, shortest_path,
)


class DistanceCalibrationChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.report = make_report()

    def test_weighted_shortest_path_uses_detour_and_is_symmetric(self):
        graph = {'a': [('b', 10), ('c', 1)], 'b': [('a', 10), ('c', 2)],
                 'c': [('a', 1), ('b', 2)]}
        self.assertEqual(shortest_path(graph, 'a', 'b'), (3, ['a', 'c', 'b']))
        self.assertEqual(shortest_path(graph, 'b', 'a')[0], 3)
        self.assertEqual(shortest_path(graph, 'a', 'a'), (0, ['a']))
        with self.assertRaisesRegex(ValueError, 'Unknown'):
            shortest_path(graph, 'a', 'missing')
        graph['d'] = []
        with self.assertRaisesRegex(ValueError, 'Disconnected'):
            shortest_path(graph, 'a', 'd')

    def test_polyline_length_and_invalid_speed(self):
        data = {'towns': [{'key': 'a'}, {'key': 'b'}], 'roads': [
            {'fromKey': 'a', 'toKey': 'b', 'waypoints': [[0, 0], [3, 0], [3, 4]]}]}
        self.assertEqual(shortest_path(graph_from_roads(data, 2), 'a', 'b')[0], 3.5)
        for speed in (0, -1, math.inf, math.nan):
            with self.assertRaises(ValueError):
                graph_from_roads(data, speed)
        data['roads'][0]['waypoints'] = [[0, 0], [0, 0]]
        with self.assertRaises(ValueError):
            graph_from_roads(data, 2)

    def test_all_36_internal_routes_match_independent_floyd_warshall(self):
        data = json.loads((ROOT / 'Tools/WorldLayout/world-layout.json').read_text())
        graph = graph_from_roads(data, self.report['walkingCellsPerSecond'])
        nodes = list(graph)
        distance = {(a, b): (0 if a == b else math.inf) for a in nodes for b in nodes}
        for a in nodes:
            for b, cost in graph[a]:
                distance[a, b] = min(distance[a, b], cost)
        for k in nodes:
            for a in nodes:
                for b in nodes:
                    distance[a, b] = min(distance[a, b], distance[a, k] + distance[k, b])
        self.assertEqual(len(self.report['internalRoutes']), 36)
        for route in self.report['internalRoutes']:
            self.assertAlmostEqual(route['seconds'], distance[route['from'], route['to']])
            self.assertAlmostEqual(route['seconds'], shortest_path(graph, route['to'], route['from'])[0])

    def test_least_squares_is_optimal_and_speed_scaling_cancels(self):
        samples = [(r['seconds'], r['legacyDistance']) for r in self.report['goryeoPairs']]
        k = fit_ratio(samples)
        def error(ratio):
            return sum((ratio * t - d) ** 2 for t, d in samples)
        self.assertLess(error(k), error(k * .99))
        self.assertLess(error(k), error(k * 1.01))
        scaled = [(t * 2, d) for t, d in samples]
        self.assertAlmostEqual(fit_ratio(scaled), k / 2)
        self.assertAlmostEqual(sum(t * (k * t - d) for t, d in samples), 0)
        with self.assertRaises(ValueError):
            fit_ratio([])

    def test_deadline_interval_includes_clamped_distance_one(self):
        self.assertEqual(deadline(0), 17)
        self.assertEqual(deadline(4), 44)
        low, high = allowed_ratio(10, 1)
        self.assertEqual(low, 0)
        self.assertAlmostEqual(deadline(high * 10), 17 * 1.1)
        low, high = allowed_ratio(10, 4)
        self.assertAlmostEqual(deadline(low * 10), 44 * .9)
        self.assertAlmostEqual(deadline(high * 10), 44 * 1.1)

    def test_two_pair_witness_disproves_every_single_ratio(self):
        rows = self.report['goryeoPairs']
        a = next(r for r in rows if (r['from'], r['to']) == ('나루', '장단'))
        b = next(r for r in rows if (r['from'], r['to']) == ('나루', '송악'))
        for key in ('allowedRatioContinuous', 'allowedRatioNearestSuperset'):
            lo, hi = intersection([a[key], b[key]])
            self.assertGreater(lo, hi)
        self.assertFalse(self.report['feasibleContinuous'])
        self.assertFalse(self.report['feasibleNearestRounding'])
        self.assertEqual(len(rows), 10)
        self.assertEqual(sum(abs(r['continuousDeltaPercent']) <= 10 for r in rows), 1)

    def test_committed_report_matches_current_production_inputs(self):
        stored = json.loads((ROOT / 'Docs/Validation/E1-2026-10-25/calibration.json').read_text())
        self.assertEqual(stored, self.report)

    def test_production_formula_change_fails_instead_of_using_stale_assumptions(self):
        # No fixture copy of Assets: override only the single read under analysis.
        from unittest.mock import patch
        original = Path.read_text
        def changed(path, *args, **kwargs):
            text = original(path, *args, **kwargs)
            if path.name == 'ContractManager.cs':
                return text.replace('hours = dist * 9 + 8', 'hours = dist * 8 + 8')
            return text
        with patch.object(Path, 'read_text', changed):
            with self.assertRaisesRegex(ValueError, 'Deadline formula changed'):
                make_report()


if __name__ == '__main__':
    unittest.main()
