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
            [Tooltip("지역 공급·수요 기준가 배율. 미설정/0은 기존 가격(1배)과 호환")] public float localPriceMultiplier = 1f;
        }

        public string id;
        public string displayName;
        public RegionData region;
        [Tooltip("맵 좌표. 마을 사이 거리(거쳐 가는 맵 수)를 이 값으로 잰다")] public Vector2Int worldCoord;
        [Range(1, 4)] public int startLevel = 1;
        [Header("시설 (시장 거래량 성장과 별도)")]
        [Tooltip("0이면 기존 startLevel 사용. 신규 프로토타입의 기본 거점 등급")]
        [Range(0, 4)] public int settlementTier;
        public int SettlementTier => Mathf.Clamp(settlementTier > 0 ? settlementTier : startLevel, 1, 4);
        public List<TownFacility> facilities = new List<TownFacility>();
        public List<StockEntry> stock = new List<StockEntry>();
        [Tooltip("목록에 없는 물건을 팔 때의 적정 재고")] public float defaultOptimal = 3f;
        public List<VehicleData> vehiclesForSale = new List<VehicleData>();

        public float BasePriceFor(ItemData item)
        {
            float multiplier = 1f;
            foreach (var entry in stock)
                if (entry.item == item)
                {
                    float value = entry.localPriceMultiplier;
                    if (!float.IsNaN(value) && !float.IsInfinity(value) && value > 0f)
                        multiplier = Mathf.Clamp(value, 0.1f, 3f);
                    break;
                }
            return item.BasePriceAt(this) * multiplier;
        }

        public float OptimalFor(ItemData item)
        {
            foreach (var s in stock) if (s.item == item) return s.optimal;
            return defaultOptimal;
        }
    }
}
