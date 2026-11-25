using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>배달 견적 규칙. 11/25부터 전 지역이 DeliveryReward.Unified 하나를 쓴다 (지역 에셋 값은 옛 기록).</summary>
    [Serializable]
    public class DeliveryRewardRules
    {
        public bool enabled;
        [Min(1)] public int revision = 1;
        [Min(0)] public float dispatchFee = 1.5f;
        [Min(0)] public float perWeight = 0.65f;
        [Min(0)] public float perWeightPerStep = 0.25f;
        [Min(1)] public float premiumMultiplier = 1.8f;
        [Min(1)] public int premiumMinWeight = 8, premiumMaxWeight = 12;
    }

    public static class DeliveryReward
    {
        /// <summary>
        /// 11/25 은샘: 배달 보수를 전 지역 한 가지 규칙으로 (전에는 고려만 무게 기준 3~9냥, 다른 지역은 거리 기준 36냥~).
        /// 기준(고려 배달·교역 균형 검사의 왕복 비교): 무게 4짜리 배달 왕복 약 58냥 = 걸어서 하는 교역 왕복(19~66냥)과 비슷하고,
        /// 손수레 교역(48~79냥)보다는 조금 낮다. 밑천 없이도 벌이가 되고, 남는 짐칸에 물건을 같이 실으면 가장 많이 남는다.
        /// 보통 짐(무게 2~6): 가까운 마을(거리 1) 9~17냥, 거리 4는 15~35냥. 예전에는 같은 왕복이 고려 16냥 · 다른 지역 208냥이었다.
        /// 지역 에셋의 deliveryRewards 값은 더 쓰지 않는다.
        /// </summary>
        public static readonly DeliveryRewardRules Unified = new DeliveryRewardRules
        { enabled = true, revision = 2, dispatchFee = 5f, perWeight = 2f, perWeightPerStep = 1f, premiumMultiplier = 1.8f, premiumMinWeight = 8, premiumMaxWeight = 12 };

        public static DeliveryRewardRules Rules(TownData town) => town != null && town.region != null ? Unified : null;

        /// <summary>current는 기존 연결 수, layoutD는 가격·기한과 같은 환산 거리.</summary>
        public static int RouteSteps(WorldIndex world, string fromTown, string toTown) =>
            TownDistance.Between(fromTown, toTown, world: world, legacy: TownDistance.LegacyMetric.MapConnections);

        public static int Quote(DeliveryRewardRules rules, int economicDistance, int routeSteps, float weight, bool premium, float variation = 1f)
        {
            float reward;
            if (rules == null || !rules.enabled)
                reward = 14 + 16 * Mathf.Max(1, economicDistance) + weight * 3;
            else
            {
                if (routeSteps < 0) throw new InvalidOperationException("배달 견적의 연결 경로가 없습니다.");
                reward = rules.dispatchFee + weight * (rules.perWeight + rules.perWeightPerStep * Mathf.Max(0, routeSteps - 1));
            }
            reward *= Mathf.Clamp(variation, 0.85f, 1.15f);
            if (premium) reward *= rules != null && rules.enabled ? rules.premiumMultiplier : 1.8f;
            return Mathf.Max(1, Mathf.RoundToInt(reward));
        }
    }
}
