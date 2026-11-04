"""Approved literal D10 spread is insufficient; do not mistake a passing harness for R2 acceptance."""
import hashlib
import json
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Validation/R2-Safety-2026-10-25'
STALE_INFO_FIXED={'Assets/PocketCaravan/Data/Transit/Town_Ferry.asset','Assets/PocketCaravan/Data/Transit/Town_Woodtown.asset'}
INFO_OLD=b'\\uB2E4\\uB978 \\uBB38\\uBA85\\uC758\n      \\uAD50\\uC5ED\\uB85C\\uB294 \\uC544\\uC9C1 \\uC5F0\\uACB0\\uB418\\uC9C0 \\uC54A\\uC558\\uC2B5\\uB2C8\\uB2E4.'
INFO_NEW=b'\\uB2E4\\uB978 \\uBB38\\uBA85\\uC73C\\uB85C \\uAC00\\uB294 \\uAD50\\uC5ED\\uB85C\\uB294 \\uC11C\\uCABD \\uB0B4\\uB959 \\uAD50\\uC5ED \\uAE38\\uBAA9\\uC5D0\\uC11C \\uC774\\uC5B4\\uC9D1\\uB2C8\\uB2E4.'

# Task 2: remove only the exact newly added journeyNote line before the frozen economy comparison.
JOURNEY_NOTES_ADDED = {
    'Assets/PocketCaravan/Data/Transit/Town_Woodtown.asset': b'  journeyNote: "\\uace0\\ub824\\uc640 \\uc911\\uc559 \\ub300\\ub959\\uc744 \\uc787\\ub294 \\uae38\\ubaa9\\uc785\\ub2c8\\ub2e4. \\ub554\\uac10\\u00b7\\uc1a1\\uc9c4\\uc744 \\uc0b4\\ud3b4\\ubcf4\\uace0, \\uadc0\\ud658\\ud560 \\ub54c\\ub294 \\uace0\\ub824\\ub85c \\uac00\\uc838\\uac08 \\uc9d0\\uc758 \\uc2dc\\uc138\\ub97c \\ud655\\uc778\\ud558\\uc138\\uc694."\n',
    'Assets/PocketCaravan/Data/Transit/Town_Ferry.asset': b'  journeyNote: "\\uac15\\uacfc \\ubc14\\ub2f7\\uae38\\uc774 \\ub9cc\\ub098\\ub294 \\ud3ec\\uad6c\\uc785\\ub2c8\\ub2e4. \\ub9d0\\ub9b0 \\uc0dd\\uc120\\u00b7\\uc18c\\uae08\\uacfc \\uace0\\ub824\\uc5d0\\uc11c \\uac00\\uc838\\uc628 \\uc9d0\\uc744 \\ube44\\uad50\\ud574 \\ubcf4\\uc138\\uc694. \\uadc0\\ud658 \\uc9d0\\uc740 \\ud604\\uc7ac \\uc2dc\\uc138\\ub97c \\ubcf4\\uace0 \\uace0\\ub974\\uc138\\uc694."\n',
}

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
                data=(ROOT/name).read_bytes()
                # 11/03: 대륙 마을 길 안내 문구(시설 설명)의 '교역로 아직 연결 안 됨' 한 문장만 고쳤다. 그 문장을 되돌리면 예전 파일과 같아야 한다.
                if name in JOURNEY_NOTES_ADDED:
                    line=JOURNEY_NOTES_ADDED[name]
                    self.assertEqual(data.count(line),1,name+' exact approved journeyNote line')
                    data=data.replace(line,b'',1)
                if name in STALE_INFO_FIXED:data=data.replace(INFO_NEW,INFO_OLD)
                # Task6: undo only the exact approved icon ref / newly added goods, retaining the historic fixture.
                deltas=json.loads((ROOT/'Docs/Validation/TradeGoods-Task6-2026-11-04/approved-r2-deltas.json').read_text())['files']
                self.assertEqual(set(deltas),{'Assets/PocketCaravan/Data/Goryeo/Item_grain.asset','Assets/PocketCaravan/Data/Goryeo/Town_Gaegyeong.asset'})
                for change in deltas.get(name,[]):
                    new,old=change['new'].encode(),change['old'].encode()
                    self.assertEqual(data.count(new),1,name+' exact approved Task6 change')
                    data=data.replace(new,old,1)
                self.assertEqual(hashlib.sha256(data).hexdigest(),digest,name)
        frozen=OUT/'LegacyMarketState.cs.txt'
        self.assertEqual(hashlib.sha256(frozen.read_bytes()).hexdigest(),manifest['files']['Assets/PocketCaravan/Scripts/Economy/MarketState.cs'])


if __name__=='__main__':unittest.main()
