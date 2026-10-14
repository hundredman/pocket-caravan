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
        [Tooltip("퀘스트로 맡은 물건. 장터에 팔 수 없다")] public bool questItem;

        /// <summary>산지에서 이 마을까지 거쳐 가는 맵 수 (생필품은 0)</summary>
        public int MapsFrom(TownData town)
        {
            if (originTown == null || town == null) return 0;
            var d = originTown.worldCoord - town.worldCoord;
            return Mathf.Abs(d.x) + Mathf.Abs(d.y);
        }

        /// <summary>기준가 = 산지 기준가 × (1 + 맵당 비율 × 산지에서 거쳐 가는 맵 수)</summary>
        public float BasePriceAt(TownData town) => basePrice * (1f + pricePerMap * MapsFrom(town));
    }
}
