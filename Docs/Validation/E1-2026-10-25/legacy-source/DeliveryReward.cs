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

        /// <summary>등록된 최단 연결 구간 수. 실측 초/가격용 좌표/플레이어가 우회한 거리가 아니다.</summary>
        public static int RouteSteps(WorldIndex world, string fromTown, string toTown)
        {
            if (world == null) return -1;
            string from = world.MapOf("market", fromTown), to = world.MapOf("market", toTown);
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || world.Map(from) == null || world.Map(to) == null) return -1;
            var queue = new Queue<string>(); var seen = new Dictionary<string, int> { [from] = 0 };
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (id == to) return seen[id];
                var map = world.Map(id);
                if (map == null) continue;
                foreach (string neighbor in map.neighbors)
                    if (!seen.ContainsKey(neighbor)) { seen[neighbor] = seen[id] + 1; queue.Enqueue(neighbor); }
            }
            return -1;
        }

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
