using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>현재 지역의 연결된 거래처와 실제 시설 조건. 시세를 원격으로 읽거나 새 진행값을 저장하지 않는다.</summary>
    public static class JourneyGuide
    {
        public static List<TownData> Destinations(GameDatabase db, RegionData region, WorldIndex world, string mapId)
        {
            var result = new List<TownData>();
            if (db == null || region == null || string.IsNullOrEmpty(region.id) || world == null || world.Map(mapId) == null) return result;
            foreach (var town in db.towns)
            {
                if (town == null || town.region == null || town.region.id != region.id || result.Contains(town)) continue;
                string target = world.MapOf("market", town.id);
                if (string.IsNullOrEmpty(target) || world.Map(target) == null) continue;
                if (target == mapId || world.NextStep(mapId, target) != null) result.Add(town);
            }
            return result;
        }

        public static bool MarketRecorded(TradeLedger ledger, string town)
        {
            foreach (var quote in ledger.Export()) if (quote.town == town) return true;
            return false;
        }

        public static string NextUnlock(List<TownData> towns, PlayerState player)
        {
            int next = int.MaxValue;
            var names = new List<string>();
            foreach (var t in towns)
                foreach (var f in t.facilities)
                {
                    if (f == null || t.SettlementTier < f.minTier || f.minCredit <= player.credit) continue;
                    if (f.minCredit < next) { next = f.minCredit; names.Clear(); }
                    if (f.minCredit == next) names.Add(t.displayName + " " + f.displayName);
                }
            if (towns.Count > 0 && player.credit < ContractManager.PremiumCredit && ContractManager.PremiumCredit <= next)
            {
                if (ContractManager.PremiumCredit < next) { next = ContractManager.PremiumCredit; names.Clear(); }
                next = ContractManager.PremiumCredit;
                names.Add("게시판의 귀한 짐 의뢰 (제안이 있을 때)");
            }
            return next == int.MaxValue ? "현재 지역에 신용으로 추가 해금할 시설은 없습니다. 물량·귀환품·경로를 바꿔 보세요."
                : $"다음 신용 {next} (현재 {player.credit}): " + string.Join(" / ", names);
        }

        public static string FacilityEffect(TownFacility facility)
        {
            switch (facility.kind)
            {
                case FacilityKind.Rest: return "체력 회복";
                case FacilityKind.RouteInformation: return "주변 길·거래처 안내";
                default: return $"이 마을에 일반 교역품 {facility.storageCapacity:0}무게 보관 · 현지에서 회수";
            }
        }
    }
}
