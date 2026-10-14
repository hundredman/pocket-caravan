using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Origin", fileName = "Origin")]
    public class OriginData : ScriptableObject
    {
        public string id;
        public string displayName;
        public RegionData homeRegion;
        public int startingCredit;
        [Header("강점 (1 = 보통)")]
        [Tooltip("바이킹: 1.5")] public float footfallMultiplier = 1f;
        [Tooltip("고려: 장부가 낡는 속도. 0.5면 두 배 오래감")] public float ledgerStaleMultiplier = 1f;
        [Tooltip("베네치아: 다른 지역 물건을 팔 때 값이 떨어지는 정도. 0.5면 절반만 떨어짐")] public float resaleDropMultiplier = 1f;
    }
}
