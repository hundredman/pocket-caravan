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
        [TextArea] public string note;
        [Tooltip("켜진 지역만 신규/미수락 배달 보수 재조정. 수치 변경 시 revision도 올린다")]
        public DeliveryRewardRules deliveryRewards = new DeliveryRewardRules();
    }
}
