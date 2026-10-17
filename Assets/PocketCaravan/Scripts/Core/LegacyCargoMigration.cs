using System;
using System.Collections.Generic;

namespace PocketCaravan
{
    /// <summary>이전 낙하물을 1회 복원한다. 알 수 없는 ID/원가 오류 기록은 지우지 않고 다음 저장에 보존한다.</summary>
    public static class LegacyCargoMigration
    {
        public static int Restore(PlayerState player, List<DropRecord> pending, Func<string, ItemData> findItem)
        {
            int restored = 0;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var row = pending[i];
                if (row == null) continue;
                if (row.contractId != 0)
                {
                    var contract = player.contracts.Find(c => c.id == row.contractId);
                    if (contract == null) continue; // 누락/종료 의뢰 기록은 보존해 조사 가능하게 둔다.
                    contract.dropped = false;
                }
                else
                {
                    var item = findItem(row.itemId);
                    if (item == null || row.unitCost < 0 || float.IsNaN(row.unitCost) || float.IsInfinity(row.unitCost)) continue;
                    // 기존 재산 이주에서는 적재 초과도 보존한다. 추가 매입은 기존 한도 검사로 막는다.
                    player.cargo.Add(item, 1, row.unitCost);
                }
                pending.RemoveAt(i); restored++;
            }
            player.shake = 0;
            return restored;
        }
    }
}
