using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Region", fileName = "Region")]
    public class RegionData : ScriptableObject
    {
        public string id = "goryeo";
        public string displayName = "고려";
        [Tooltip("중앙 대륙은 마을별 평판, 스타팅 문명은 문명별 평판")] public bool townReputation;
        [TextArea] public string note;
        [Tooltip("켜진 지역만 신규/미수락 배달 보수 재조정. 수치 변경 시 revision도 올린다")]
        public DeliveryRewardRules deliveryRewards = new DeliveryRewardRules();
    }
}
