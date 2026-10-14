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
        public float FootfallPerStep => origin != null ? origin.footfallMultiplier : 1f;
    }

    /// <summary>짐. 적재는 무게 하나로만 잰다 (4주 범위).</summary>
    public class Inventory
    {
        readonly Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

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

        public void Add(ItemData item, int count = 1) => items[item] = Count(item) + count;

        public bool Remove(ItemData item, int count = 1)
        {
            int c = Count(item);
            if (c < count) return false;
            if (c == count) items.Remove(item); else items[item] = c - count;
            return true;
        }
    }
}
