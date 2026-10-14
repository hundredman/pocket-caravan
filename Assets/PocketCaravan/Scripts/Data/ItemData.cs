using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Item", fileName = "Item")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        [Min(0.1f)] public float weight = 1f;
        [Tooltip("산지에서의 기준가")] public float basePrice = 10f;
        [Tooltip("산지 마을. 비우면 어디서나 나는 생필품 (거리 0)")] public TownData originTown;
        [Tooltip("산지에서 맵 하나 멀어질 때마다 기준가에 더해지는 비율")] public float pricePerMap = 0.25f;

        /// <summary>기준가 = 산지 기준가 × (1 + 맵당 비율 × 산지에서 거쳐 가는 맵 수)</summary>
        public float BasePriceAt(TownData town)
        {
            int maps = 0;
            if (originTown != null && town != null)
            {
                var d = originTown.worldCoord - town.worldCoord;
                maps = Mathf.Abs(d.x) + Mathf.Abs(d.y);
            }
            return basePrice * (1f + pricePerMap * maps);
        }
    }
}
