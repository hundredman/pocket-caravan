"""Integrity of the review-only R2 experiment, without selecting balance values."""
from pathlib import Path
import json,unittest
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Validation/R2-2026-10-25'

class BalanceReportChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source=json.loads((OUT/'source-snapshot.json').read_text())
        cls.data=json.loads((OUT/'calculations.json').read_text())

    def test_snapshot_coverage_excludes_quest_cargo_and_resolves_counterparties(self):
        s,d=self.source,self.data;goods={i['id'] for i in s['items'] if not i['questItem']};towns={t['id'] for t in s['towns']}
        self.assertEqual(set(d['goods']),goods)
        self.assertEqual(d['oracleCases'],len(s['oracle']))
        self.assertEqual(d['oracleCases'],len(towns)*len(goods)*3*5*2)
        self.assertEqual(len(d['quotes']),len(goods)*len(towns)*len(d['policies'])*6)
        for r in d['quotes']:
            self.assertIn(r['item'],goods);self.assertIn(r['town'],towns)

    def test_baseline_has_no_hidden_discount_or_haggle_and_remains_review_only(self):
        d=self.data;self.assertFalse(d['appliedToGame'])
        for r in d['quotes']:
            if r['policy']!='baseline':continue
            self.assertEqual(r['buyBase'],r['buyRep']);self.assertEqual(r['buyBase'],r['buyMaxHaggle'])
            self.assertEqual(r['sellBase'],r['sellRep']);self.assertEqual(r['sellBase'],r['sellMaxHaggle'])

    def test_unchanged_baseline_is_identical_across_candidate_stage_labels(self):
        groups=[g for g in self.data['groups'] if g['policy']=='baseline']
        for growth in (False,True):
            for order in ('buy_then_sell','sell_then_buy'):
                rows=[g for g in groups if g['growth']==growth and g['order']==order]
                self.assertEqual(len(rows),18)
                keys=('valid','ineligible','profitable','newProfitable','baselineProfitable','worst')
                self.assertTrue(all(all(r[k]==rows[0][k] for k in keys) for r in rows))
        self.assertTrue(all(r['buyBase']>=1 and r['buyMaxHaggle']>=1 and r['sellBase']>=1 and r['sellMaxHaggle']>=1 for r in self.data['quotes']))

    def test_reported_profitable_example_preserves_cash_and_market_state_evidence(self):
        examples=[g['worst'] for g in self.data['groups'] if g['worst'] is not None]
        self.assertTrue(any(e['profit']>0 and e['baselineProfit']<=0 for e in examples))
        for e in examples:self.assertEqual(e['profit'],e['sellTotal']-e['buyTotal'])
        self.assertTrue(any(abs(e['initialStock']-e['finalStock'])>.01 for e in examples if e['origin']=='venice'))
        for g in self.data['groups']:
            self.assertLessEqual(g['newProfitable'],g['profitable']);self.assertLessEqual(g['profitable'],g['valid'])

if __name__=='__main__':unittest.main()
