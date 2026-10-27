using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>지역별 신규 배달 견적. 비활성/미설정 지역은 기존 공식을 유지한다.</summary>
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
        public static DeliveryRewardRules Rules(TownData town)
        {
            var r = town != null && town.region != null ? town.region.deliveryRewards : null;
            return r != null && r.enabled ? r : null;
        }

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
