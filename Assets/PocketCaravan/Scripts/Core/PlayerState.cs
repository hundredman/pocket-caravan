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

        public float Capacity => vehicle != null ? vehicle.capacity : 10f;
        public float LoadRatio => Mathf.Clamp01(cargo.Weight / Mathf.Max(0.01f, Capacity));
        public bool CanCarry(ItemData item, int count = 1) => cargo.Weight + item.weight * count <= Capacity + 0.001f;
    }

    /// <summary>짐. 적재는 무게 하나로만 잰다 (4주 범위).</summary>
    public class Inventory
    {
        readonly Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();
        readonly Dictionary<ItemData, float> totalCost = new Dictionary<ItemData, float>(); // 이익 계산용 (산 값의 합)

        public IEnumerable<KeyValuePair<ItemData, int>> Items => items;

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
