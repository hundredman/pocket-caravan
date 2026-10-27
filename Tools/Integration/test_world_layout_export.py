"""Lossless coordinate export and a guard against silently moving approved boundaries."""
from pathlib import Path
import sys,unittest
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from export_world_layout import runs,require_approved_counts

class LayoutExportChecks(unittest.TestCase):
    def test_runs_keep_negative_coordinates_holes_and_inclusive_ends(self):
        image=[[True,True,False,True],[False,False,False,False],[True,False,True,True]]
        result=runs(image,-3,-2)
        self.assertEqual(result,[[-2,-3,-2],[-2,0,0],[0,-3,-3],[0,-1,0]])
        points={(x,y) for y,lo,hi in result for x in range(lo,hi+1)}
        expected={(x-3,y-2) for y,row in enumerate(image) for x,on in enumerate(row) if on}
        self.assertEqual(points,expected)

    def test_equal_total_does_not_override_different_approved_map_areas(self):
        approved={'a':4999,'b':4000};actual={'a':5001,'b':3998};before=actual.copy()
        self.assertEqual(sum(approved.values()),sum(actual.values()))
        with self.assertRaisesRegex(ValueError,'Claude review required'):require_approved_counts(actual,approved)
        self.assertEqual(actual,before)

    def test_only_exact_named_map_counts_pass(self):
        approved={'a':4,'b':3};require_approved_counts(dict(approved),approved)
        for actual in ({'a':4},{'a':4,'b':3,'c':0},{'a':3,'b':4}):
            with self.assertRaises(ValueError):require_approved_counts(actual,approved)

if __name__=='__main__':unittest.main()
