using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 은샘 "돈을 벌어서 접근 가능한 미니 목표(마일스톤)": 다음에 살 운반 수단. 등짐 → 손수레(썰매) → 짐말(낙타·순록) → 마차(순록 썰매).
    /// 지금 운반 수단보다 짐을 더 싣는 것 중 가장 작은 단계. 같은 단계가 둘이면 출신 지역 것 (북유럽 썰매 · 아라비아 낙타).
    /// 평소 화면에는 없고, 여정 수첩(Q) "성장" 탭 맨 위 운반 수단 카드에 단계 · 다음 목표 돈 막대 · 파는 곳이 보인다 (QuestTracker.DrawGrowth, 11/19 은샘).
    /// </summary>
    public static class VehicleGoal
    {
        /// <summary>11/19 은샘 "마일스톤에 좌판까지": 마차 다음 목표. 기능은 아직 없어 수첩에 "준비 중"으로만 보인다.
        /// 값·신용은 성장 속도 측정(처음 하는 유저 마차 약 62분 → 좌판 약 105분, 신용 5단계 약 84분)으로 정함 (Docs/CreditLevelDesign.md)</summary>
        public const string StallName = "좌판";
        public const int StallPrice = 1000, StallCredit = 5;

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

        /// <summary>내 출신의 운반 수단 단계 (짐 한도 순: 등짐 → 손수레(썰매) → 짐말(낙타) → 마차)</summary>
        public static System.Collections.Generic.List<VehicleData> Ladder(PlayerState p, GameDatabase db)
        {
            var list = new System.Collections.Generic.List<VehicleData>();
            if (db == null) return list;
            string origin = p != null && p.origin != null ? p.origin.id : "";
            foreach (var v in db.vehicles)
            {
                if (v == null) continue;
                int at = list.FindIndex(o => Mathf.Approximately(o.capacity, v.capacity));
                if (at >= 0) { if (Home(v, origin) && !Home(list[at], origin)) list[at] = v; continue; }
                list.Add(v);
            }
            list.Sort((a, b) => a.capacity.CompareTo(b.capacity));
            return list;
        }

        /// <summary>이 수단을 파는 장터 이름. 지금 있는 지역(없으면 출신 지역) 장터가 먼저, 최대 max곳</summary>
        public static string WhereSold(VehicleData v, PlayerState p, GameDatabase db, int max = 3)
        {
            if (v == null || db == null) return "";
            var here = MapRoot.Current != null && MapRoot.Current.region != null ? MapRoot.Current.region : p != null && p.origin != null ? p.origin.homeRegion : null;
            var near = new System.Collections.Generic.List<string>();
            var far = new System.Collections.Generic.List<string>();
            foreach (var t in db.towns)
            {
                if (t == null || !TownMarket.ForSale(t).Contains(v) || near.Contains(t.displayName) || far.Contains(t.displayName)) continue;
                (here != null && t.region == here ? near : far).Add(t.displayName);
            }
            near.AddRange(far);
            if (near.Count > max) return string.Join(" · ", near.GetRange(0, max)) + " 등";
            return string.Join(" · ", near);
        }

        // 같은 단계가 둘이면 출신 지역에서 파는 것 (북유럽 썰매 · 아라비아 낙타, 나머지는 손수레 · 짐말)
        static bool Home(VehicleData v, string origin)
        {
            if (v.id == "north_sled") return origin == "viking";      // 북유럽: 손수레 대신 썰매 (비르카)
            if (v.id == "handcart") return origin != "viking";
            if (v.id == PackAnimal.CamelId) return origin == "arabia"; // 아라비아: 짐말 대신 낙타 (사나 · 제다)
            if (v.id == PackAnimal.ReindeerId) return origin == "viking";   // 11/19 북유럽: 짐말 대신 순록 (순록 고원 마을)
            if (v.id == PackAnimal.HorseId) return origin != "arabia" && origin != "viking";
            if (v.id == PackAnimal.ReindeerSledId) return origin == "viking";   // 북유럽: 마차 대신 순록 썰매
            if (v.id == PackAnimal.CamelCaravanId) return origin == "arabia";   // 11/19 아라비아: 마차 대신 낙타 대상
            if (v.id == PackAnimal.WagonId) return origin != "viking" && origin != "arabia";
            return true;
        }
    }
}
