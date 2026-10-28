"""Approved literal D10 spread is insufficient; do not mistake a passing harness for R2 acceptance."""
import hashlib
import json
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Validation/R2-Safety-2026-10-25'


class R2SafetyChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data=json.loads((OUT/'calculations.json').read_text())
        cls.unity=json.loads((OUT/'unity.json').read_text())

    def test_grid_covers_approved_stages_and_original_stress_dimensions(self):
        d=self.data
        self.assertEqual(d['cycleCases'],7*8*4*2*5*2*2*4*2*6*3)
        self.assertEqual(len(d['groups']),72)
        self.assertEqual(sum(g['valid']+g['ineligible'] for g in d['groups']),d['cycleCases'])
        self.assertEqual({g['stage'] for g in d['groups']},set(range(6)))
        self.assertEqual({g['haggle'] for g in d['groups']},{'none','buy_only','both'})
        self.assertEqual(d['oracleCases'],1680)

    def test_simultaneous_spread_holds_even_when_actual_cycles_profit(self):
        d=self.data
        self.assertEqual(d['safetySpreadViolations'],0)
        self.assertGreater(sum(g['profitable'] for g in d['groups'] if g['haggle']=='none'),0)
        for witness in (d['singleCurrent'],d['singleFuture']):
            result=witness['result'];self.assertEqual(result['Profit'],result['Sells']-result['Buys'])
            self.assertGreater(result['Profit'],0)
            self.assertTrue(all(t['buyQuote']>=t['sellQuote']+1 for t in result['Trace']))

    def test_no_haggle_real_asset_counterexample_matches_unity(self):
        a=self.data['singleCurrent'];b=self.unity['liveNoHaggle']
        self.assertEqual((a['town'],a['item'],a['origin'],a['haggle']),('transit_ferry','ginseng','venice','none'))
        self.assertEqual((a['result']['Buys'],a['result']['Sells'],a['result']['Profit']),(b['buyBefore'],b['sellAfter'],b['profit']))
        self.assertTrue(b['affordable'] and b['carryFits'] and b['sameQuoteSpreadHolds'])
        self.assertFalse(self.unity['playerSaveWritten'])
        self.assertFalse(self.unity['runtimeApplied'])

    def test_counterexample_can_restore_stock_exactly_with_future_haggle(self):
        a=self.data['knownCounterexample'];b=self.unity['knownFutureHaggle']
        self.assertEqual((a['Buys'],a['Sells'],a['FinalStock']),(81,85,3))
        self.assertEqual((b['profit'],b['stockBefore'],b['stockAfter']),(4,3,3))
        self.assertFalse(self.data['appliedToGame'])
        self.assertTrue(self.data['sharedRegionIdentity'])

    def test_historical_input_assets_and_frozen_market_model_are_preserved(self):
        manifest=json.loads((OUT/'input-hashes.json').read_text())
        for name,digest in manifest['files'].items():
            # 팀 공용 파일(GameDatabase, Origins)은 다른 지역이 항목을 더하므로 고려·transit 소유 파일만 고정한다.
            if name.startswith(('Assets/PocketCaravan/Resources/GameDatabase.asset','Assets/PocketCaravan/Data/Origins/')) or '/Origin_' in name:continue   # 출신 데이터는 통합 때 주인공 그림 칸이 더해진다
            if name.endswith('.asset') or name.endswith('source-snapshot.json'):
                self.assertEqual(hashlib.sha256((ROOT/name).read_bytes()).hexdigest(),digest,name)
        frozen=OUT/'LegacyMarketState.cs.txt'
        self.assertEqual(hashlib.sha256(frozen.read_bytes()).hexdigest(),manifest['files']['Assets/PocketCaravan/Scripts/Economy/MarketState.cs'])


if __name__=='__main__':unittest.main()
