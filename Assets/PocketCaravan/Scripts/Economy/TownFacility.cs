using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    public enum FacilityKind { Rest, RouteInformation, Warehouse }

    [Serializable]
    public class TownFacility
    {
        public string id, displayName;
        public FacilityKind kind;
        [Range(1, 4)] public int minTier = 1;
        public int minCredit;
        [TextArea] public string description;
        [Min(1)] public float storageCapacity = 120f;
    }

    /// <summary>개인 신용과 거점 등급의 순수 판정. 서버 연결 시 위치/세션 검증은 호출 경계에서 추가한다.</summary>
    public static class FacilityAccess
    {
        public static string Denial(TownData town, TownFacility facility, PlayerState player)
        {
            if (town == null || facility == null || player == null || town.facilities == null
                || !town.facilities.Contains(facility)) return "이 마을에서 제공하지 않는 시설입니다.";
            if (town.SettlementTier < facility.minTier) return $"거점 {facility.minTier}등급부터 제공하는 시설입니다.";
            if (player.credit < facility.minCredit) return $"신용 {facility.minCredit} 이상 필요 (현재 {player.credit})";
            return null;
        }
    }

    [Serializable]
    public class StoredCargo
    {
        public string townId, itemId;
        public int count;
        public float totalCost;
    }

    /// <summary>캐릭터별·마을별 보관함. 알 수 없는 상품 ID도 저장에서 보존한다.</summary>
    public class WarehouseStore
    {
        readonly List<StoredCargo> rows = new List<StoredCargo>();
        public IEnumerable<StoredCargo> At(string town) => rows.FindAll(r => r.townId == town);
        public List<StoredCargo> Export()
        {
            var copy = new List<StoredCargo>();
            foreach (var r in rows) copy.Add(new StoredCargo { townId = r.townId, itemId = r.itemId, count = r.count, totalCost = r.totalCost });
            return copy;
        }
        public void Import(List<StoredCargo> saved)
        {
            var valid = new List<StoredCargo>();
            foreach (var r in saved ?? new List<StoredCargo>())
            {
                if (r == null || string.IsNullOrWhiteSpace(r.townId) || string.IsNullOrWhiteSpace(r.itemId)
                    || r.count <= 0 || float.IsNaN(r.totalCost) || float.IsInfinity(r.totalCost) || r.totalCost < 0)
                    throw new InvalidOperationException("보관 화물 저장값이 올바르지 않습니다.");
                if (valid.Exists(x => x.townId == r.townId && x.itemId == r.itemId))
                    throw new InvalidOperationException("보관 화물 ID가 중복됐습니다.");
                valid.Add(new StoredCargo { townId = r.townId, itemId = r.itemId, count = r.count, totalCost = r.totalCost });
            }
            rows.Clear(); rows.AddRange(valid);
        }
        public string Transfer(TownData town, TownFacility facility, PlayerState player, ItemData item,
            bool deposit, Func<string, ItemData> findItem)
        {
            string denial = FacilityAccess.Denial(town, facility, player);
            if (denial != null) return denial;
            if (facility.kind != FacilityKind.Warehouse) return "보관 시설이 아닙니다.";
            if (item == null || item.questItem || findItem == null) return "일반 교역품만 보관할 수 있습니다.";
            var row = rows.Find(x => x.townId == town.id && x.itemId == item.id);
            if (deposit)
            {
                if (player.cargo.Count(item) < 1) return "보관할 물건이 없습니다.";
                float weight = 0;
                foreach (var s in At(town.id))
                {
                    var known = findItem(s.itemId);
                    if (known == null) return "확인되지 않은 보관품이 있어 새 입고를 멈췄습니다.";
                    weight += known.weight * s.count;
                }
                if (weight + item.weight > facility.storageCapacity) return "보관 한도가 가득 찼습니다.";
                if (row != null && row.count == int.MaxValue) return "보관 수량 한도를 넘었습니다.";
                float cost = player.cargo.AverageCost(item);
                if (!player.cargo.Remove(item)) return "보관할 물건이 없습니다.";
                if (row == null) { row = new StoredCargo { townId = town.id, itemId = item.id }; rows.Add(row); }
                row.count++; row.totalCost += cost;
            }
            else
            {
                if (row == null || row.count < 1) return "찾을 물건이 없습니다.";
                if (!player.CanCarry(item)) return "짐을 더 실을 수 없습니다.";
                float cost = row.totalCost / row.count;
                player.cargo.Add(item, 1, cost);
                row.count--; row.totalCost -= cost;
                if (row.count == 0) rows.Remove(row);
            }
            return null;
        }
    }
}
