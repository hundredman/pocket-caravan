using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 은샘 "돈을 벌어서 접근 가능한 미니 목표(마일스톤)": 다음에 살 운반 수단. 등짐 → 손수레(썰매) → 짐말(낙타) → 마차.
    /// 지금 운반 수단보다 짐을 더 싣는 것 중 가장 작은 단계. 같은 단계가 둘이면 출신 지역 것 (북유럽 썰매 · 아라비아 낙타).
    /// 평소 화면에는 없고, Tab으로 상태 카드를 펼치면 "다음 목표 · 짐말 [막대] 210/360" 한 줄로 보인다 (HUD.DrawExpanded, 11/19 은샘).
    /// </summary>
    public static class VehicleGoal
    {
        /// <summary>다음 목표 운반 수단 (없으면 null: 마차까지 샀다)</summary>
        public static VehicleData Next(PlayerState p, GameDatabase db)
        {
            if (p == null || db == null) return null;
            float have = Mathf.Max(p.vehicle != null ? p.vehicle.capacity : 0f, p.stowedVehicle != null ? p.stowedVehicle.capacity : 0f);
            string origin = p.origin != null ? p.origin.id : "";
            VehicleData best = null;
            foreach (var v in db.vehicles)
            {
                if (v == null || v.price <= 0 || v.capacity <= have) continue;
                if (best == null || v.capacity < best.capacity || (Mathf.Approximately(v.capacity, best.capacity) && Home(v, origin) && !Home(best, origin))) best = v;
            }
            return best;
        }

        // 같은 단계가 둘이면 출신 지역에서 파는 것 (북유럽 썰매 · 아라비아 낙타, 나머지는 손수레 · 짐말)
        static bool Home(VehicleData v, string origin)
        {
            if (v.id == "north_sled") return origin == "viking";      // 북유럽: 손수레 대신 썰매 (비르카)
            if (v.id == "handcart") return origin != "viking";
            if (v.id == PackAnimal.CamelId) return origin == "arabia"; // 아라비아: 짐말 대신 낙타 (사나 · 제다)
            if (v.id == PackAnimal.HorseId) return origin != "arabia";
            return true;
        }
    }
}
