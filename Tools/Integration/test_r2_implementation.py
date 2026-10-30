"""Approved R2 runtime acceptance, with protected returns separated from existing-cargo trading."""
import hashlib,json,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Validation/R2-Implementation-2026-10-26'
sys.path.insert(0,str(ROOT/'Tools/Routing'))
from export_routes import serialized

class R2ImplementationChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data=json.loads((OUT/'calculations.json').read_text());cls.unity=json.loads((OUT/'unity.json').read_text())

    def test_protected_local_buy_return_cycles_have_no_profit_across_full_grid(self):
        d=self.data;self.assertEqual(d['cycleCases'],1290240)
        protected=[g for g in d['groups'] if g['order']=='buy_then_sell']
        self.assertEqual(len(protected),36)
        self.assertEqual(sum(g['valid']+g['ineligible'] for g in protected),645120)
        self.assertEqual(sum(g['profitable'] for g in protected),0)
        self.assertEqual(d['safetySpreadViolations'],0)

    def test_sale_first_initial_cargo_is_separately_reported_and_not_repeatable(self):
        d=self.data;initial=sum(g['profitable'] for g in d['groups'] if g['order']=='sell_then_buy')
        self.assertEqual(initial,d['repeatChecked']);self.assertGreater(initial,0)
        self.assertEqual(d['repeatProfitable'],0)

    def test_previous_fixed_stock_counterexample_now_loses_one(self):
        r=self.data['knownCounterexample'];self.assertEqual((r['Buys'],r['Sells'],r['Profit'],r['FinalStock']),(81,80,-1,3))
        self.assertEqual((self.unity['buy'],self.unity['sell']),(144,143))

    def test_unity_exercises_records_expiry_reload_rare_unlock_and_daily_reputation(self):
        passed=self.unity['passed']
        for name in ('Unity purchase record roundtrip','exactly 12 hours expires','old save has empty activity',
                     'actual market blocks Friendly rare purchase','actual Trusted rare purchase','Hostile can sell rare cargo',
                     'same day cannot replay','daily amount survives reload','preview is read-only'):
            self.assertIn(name,passed)
        self.assertEqual(sum(x.startswith('basic facility ignores reputation') for x in passed),13)

    def test_legacy_progress_credit_and_save_checks_still_pass_in_engine(self):
        for file,count in [('q1.json',25),('r6.json',87),('save.json',27)]:
            report=json.loads((OUT/file).read_text());self.assertEqual(len(report['passed']),count)

    def test_route_serialization_rounds_recursively_to_six_places(self):
        x={'roads':[{'from_':'a','to':'b','seconds':3.123456789,'waypoints':[[1.111111111,2.222222222]]}], 'ratio':.123456789}
        result=json.loads(serialized(x));self.assertEqual(result['ratio'],.123457)
        self.assertEqual(result['roads'][0]['seconds'],3.123457)
        self.assertEqual(result['roads'][0]['waypoints'],[[1.111111,2.222222]])

    def test_user_screen_approval_matches_reviewed_images(self):
        a=json.loads((OUT/'approval.json').read_text());self.assertEqual(a['statement'],'R2 화면 승인')
        for row in a['images']:self.assertEqual(hashlib.sha256((OUT/row['file']).read_bytes()).hexdigest(),row['sha256'])

if __name__=='__main__':unittest.main()
