using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    public class PlayerState
    {
        public int money;
        public int credit;
        public VehicleData vehicle;
        public OriginData origin;
        public readonly Inventory cargo = new Inventory();
        public readonly TradeLedger ledger = new TradeLedger();
        public readonly ReputationState reputation = new ReputationState();
        public readonly WarehouseStore warehouse = new WarehouseStore();
        /// <summary>맡은 배달 의뢰 (짐 무게에 들어간다)</summary>
        public readonly List<Contract> contracts = new List<Contract>();

        [Tooltip("달리기 체력 0~1")] public float stamina = 1f;
        [Tooltip("짐 흔들림 0~1. 노면·적재 감속의 시각 표현이며 주행 중 낙하는 없다")] public float shake;

        public float Capacity => vehicle != null ? vehicle.capacity : 10f;

        public float ContractWeight
        {
            get
            {
                float w = 0;
                foreach (var c in contracts) if (!c.dropped) w += c.weight;
                return w;
            }
        }

        /// <summary>물건 + 의뢰 짐</summary>
        public float TotalWeight => cargo.Weight + ContractWeight;
        public bool HasAnyCargo => cargo.Weight > 0.01f || ContractWeight > 0.01f;
        public float LoadRatio => Mathf.Clamp01(TotalWeight / Mathf.Max(0.01f, Capacity));
        public bool CanCarry(ItemData item, int count = 1) => TotalWeight + item.weight * count <= Capacity + 0.001f;
        public bool CanCarryWeight(float w) => TotalWeight + w <= Capacity + 0.001f;
    }

    /// <summary>짐. 적재는 무게 하나로만 잰다 (4주 범위).</summary>
    public class Inventory
    {
        readonly Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();
        readonly Dictionary<ItemData, float> totalCost = new Dictionary<ItemData, float>(); // 이익 계산용 (산 값의 합)

        public IEnumerable<KeyValuePair<ItemData, int>> Items => items;
        public float TotalCost(ItemData item) => totalCost.TryGetValue(item, out var t) ? t : 0f;
        public void Clear() { items.Clear(); totalCost.Clear(); }

        public int Count(ItemData item) => item != null && items.TryGetValue(item, out var n) ? n : 0;

        public float Weight
        {
            get
            {
                float w = 0;
                foreach (var kv in items) w += kv.Key.weight * kv.Value;
                return w;
            }
        }

        /// <summary>한 개 평균 원가. 퀘스트로 받은 물건이나 공짜로 얻은 것은 0.</summary>
        public float AverageCost(ItemData item)
        {
            int c = Count(item);
            return c > 0 && totalCost.TryGetValue(item, out var t) ? t / c : 0f;
        }

        public void Add(ItemData item, int count = 1, float unitCost = 0f)
        {
            items[item] = Count(item) + count;
            totalCost.TryGetValue(item, out var t);
            totalCost[item] = t + unitCost * count;
        }

        public bool Remove(ItemData item, int count = 1)
        {
            int c = Count(item);
            if (c < count) return false;
            float avg = AverageCost(item);
            if (c == count) { items.Remove(item); totalCost.Remove(item); }
            else { items[item] = c - count; totalCost[item] = avg * (c - count); }
            return true;
        }
    }
}
