using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Vehicle", fileName = "Vehicle")]
    public class VehicleData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite sprite;
        public float capacity = 10f;
        public int price;
        [Tooltip("이 등급 이상 마을에서 판다")] public int minTownLevel = 1;
        [Tooltip("길 등급 0(없음)·1(오솔길)·2(길)·3(큰길)별 속도 배율. 걷기 = 1 기준")]
        public float[] speedByGrade = { 1f, 1f, 1f, 1f };

        public float SpeedOn(int grade)
        {
            if (speedByGrade == null || speedByGrade.Length == 0) return 1f;
            return speedByGrade[Mathf.Clamp(grade, 0, speedByGrade.Length - 1)];
        }
    }
}
