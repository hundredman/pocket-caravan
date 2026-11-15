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
        [Header("주인공 그림")]
        [Tooltip("이 출신으로 시작하면 어느 지역 맵에서든 쓰는 주인공 걷기 그림 16장 [방향×4 + 프레임], 방향은 아래·위·왼쪽·오른쪽. 메뉴 Pocket Caravan/통합/주인공 그림 연결이 채운다. 비우면 씬에 있는 그림 그대로")]
        public Sprite[] playerFrames = new Sprite[0];
        [Header("강점 (11/15 다시 정함). 기준: 북유럽 이동 +10% = 시간당 이익 약 +5~7%. 다른 지역도 이만큼")]
        [Tooltip("모든 이동 속도 배율. 북유럽 1.1")] public float speedMultiplier = 1f;
        [Tooltip("길 없는 곳(풀밭·숲)에서의 추가 속도 배율. 11/15부터 모두 1 (북유럽도 기본 속도로 옮김)")] public float offroadSpeedMultiplier = 1f;
        [Tooltip("베네치아: 실을 수 있는 짐 무게 비율. 0.1이면 +10% (손수레 30 → 33)")] public float capacityBonus = 0f;
        [Tooltip("고려: 이익이 난 판매마다 이익에 더해 주는 비율. 0.06이면 이익 50냥에 +3냥")] public float profitBonus = 0f;
        [Tooltip("아라비아: 물건이 난 문명과 다른 문명에서 팔 때 이익에 더해 주는 비율 (OriginPerks.CrossCiv)")] public float crossCivProfitBonus = 0f;
    }
}
