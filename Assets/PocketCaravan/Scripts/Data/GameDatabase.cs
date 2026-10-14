using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [Header("시작")]
        public int startMoney = 100;
        public VehicleData startVehicle;
        public OriginData startOrigin;

        [Header("목록")]
        public List<ItemData> items = new List<ItemData>();
        public List<TownData> towns = new List<TownData>();
        public List<VehicleData> vehicles = new List<VehicleData>();
        public List<QuestData> quests = new List<QuestData>();

        [Header("그림")]
        [Tooltip("의뢰 짐 (떨어졌을 때)")] public Sprite crateIcon;

        [Header("시간")]
        public float realSecondsPerDay = 360f;

        [Header("시세")]
        [Tooltip("시간당 재고가 적정치로 돌아가는 비율")] public float stockRecoveryPerHour = 0.04f;
        [Tooltip("장날 수요 배율")] public float marketDayDemand = 1.15f;

        [Header("마을 등급 (누적 거래액 기준, 1~4등급)")]
        public int[] townLevelVolume = { 0, 600, 2500, 8000 };
        [Tooltip("시연은 3등급까지")] public int maxTownLevel = 3;
        public string[] townLevelNames = { "", "촌락", "장터 마을", "상업 고을", "교역 도시" };

        [Header("길")]
        public RoadSettings roads = new RoadSettings();
    }

    [Serializable]
    public class RoadSettings
    {
        public string[] gradeNames = { "길 없음", "오솔길", "길", "큰길" };
    }
}
