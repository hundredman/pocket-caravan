using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Town", fileName = "Town")]
    public class TownData : ScriptableObject
    {
        [Serializable]
        public class StockEntry
        {
            public ItemData item;
            [Tooltip("적정 재고. 산지는 크게, 귀한 곳은 작게")] public float optimal = 10f;
        }

        public string id;
        public string displayName;
        public RegionData region;
        [Tooltip("맵 좌표. 마을 사이 거리(거쳐 가는 맵 수)를 이 값으로 잰다")] public Vector2Int worldCoord;
        [Range(1, 4)] public int startLevel = 1;
        public List<StockEntry> stock = new List<StockEntry>();
        [Tooltip("목록에 없는 물건을 팔 때의 적정 재고")] public float defaultOptimal = 3f;
        public List<VehicleData> vehiclesForSale = new List<VehicleData>();

        public float OptimalFor(ItemData item)
        {
            foreach (var s in stock) if (s.item == item) return s.optimal;
            return defaultOptimal;
        }
    }
}
