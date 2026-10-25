"""Approved E1 configuration preserves production and all future internal endpoints."""
import json
from pathlib import Path
import sys
import unittest

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/Routing'))
from export_routes import build, serialized, BINDINGS
from check_distance_calibration import graph_from_roads, shortest_path


class DistanceServiceSourceChecks(unittest.TestCase):
    def test_generated_routes_match_the_single_source_and_production_stays_current(self):
        actual=(ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text()
        self.assertEqual(actual,serialized(build()))
        data=json.loads(actual)
        self.assertEqual(data['mode'],'current')
        self.assertEqual(len(data['towns']),9)
        self.assertEqual(len(data['roads']),12)
        self.assertTrue(all(not x['active'] and x['runtimeBinding'] is None for x in data['externalLinks']))
        self.assertEqual(len(data['externalLinks']),3)
        self.assertEqual(BINDINGS['east/동문 마을'],'transit_woodtown')
        self.assertEqual(BINDINGS['east/북동 포구'],'transit_ferry')

    def test_linked_csharp_regression_has_exact_legacy_output_not_tolerance(self):
        r=json.loads((ROOT/'Docs/Validation/E1-Unified-2026-10-25/regression.json').read_text())
        self.assertTrue(r['baselineCurrentBytesIdentical'])
        self.assertEqual(r['baselineSha256'],r['currentSha256'])
        self.assertEqual((r['oracleCases'],r['rewardCases'],r['seededOffers']),(1680,6468,2100))

    def test_compiled_layout_results_match_python_dijkstra_for_all_81_pairs(self):
        data=build();layout=json.loads((ROOT/'Tools/WorldLayout/world-layout.json').read_text())
        graph=graph_from_roads(layout,data['walkingCellsPerSecond'])
        reverse={v:k for k,v in BINDINGS.items()}
        result=json.loads((ROOT/'Docs/Validation/E1-Unified-2026-10-25/current.layoutD.json').read_text())
        self.assertEqual(len(result['routes']),81)
        for row in result['routes']:
            seconds,_=shortest_path(graph,reverse[row['from']],reverse[row['to']])
            self.assertAlmostEqual(seconds,row['seconds'],places=4)
            self.assertEqual(row['distance'],0 if row['from']==row['to'] else max(1,round(seconds*data['ratio'])))

    def test_speed_unit_changes_do_not_change_calibrated_distances(self):
        a=build(walking_speed=3.8);b=build(walking_speed=7.6)
        self.assertAlmostEqual(b['ratio'],a['ratio']*2)
        for x,y in zip(a['roads'],b['roads']):
            self.assertAlmostEqual(x['seconds']*a['ratio'],y['seconds']*b['ratio'])


if __name__=='__main__':unittest.main()
