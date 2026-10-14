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
        [Header("강점 (1 = 보통). 목표: 자기 분야에서 시간당 이익 +10~15%, 다른 곳에서는 0")]
        [Tooltip("모든 이동 속도 배율. 바이킹 1.05")] public float speedMultiplier = 1f;
        [Tooltip("길 없는 곳(풀밭·숲)에서의 추가 속도 배율. 바이킹 1.3")] public float offroadSpeedMultiplier = 1f;
        [Tooltip("고려: 장부가 낡는 속도. 0.5면 두 배 오래감 (장부 시스템에서 사용)")] public float ledgerStaleMultiplier = 1f;
        [Tooltip("베네치아: 다른 지역 물건을 팔 때 재고가 늘어 값이 떨어지는 정도. 0.8이면 20% 덜 떨어짐")] public float resaleDropMultiplier = 1f;
        [Tooltip("아라비아: 산지에서 이만큼 이상 먼 곳에 팔 때 보너스를 받음")] public int longHaulMinMaps = 3;
        [Tooltip("아라비아: 장거리 판매 보너스. 0.05면 +5%")] public float longHaulBonus = 0f;
    }
}
