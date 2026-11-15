using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 출신 강점 (2026-11-15 은샘, UT "강점이 애매하다"). 네 강점을 한 문장·숫자 하나로, 기준은 북유럽 이동 +10%.
    /// - 북유럽: 모든 이동 +10% (OriginData.speedMultiplier, PlayerController)
    /// - 베네치아: 짐 +10% (capacityBonus, PlayerState.CapacityOf)
    /// - 고려: 이익이 난 판매마다 이익 +6% (profitBonus)
    /// - 아라비아: 물건이 난 문명과 다른 문명에서 팔면 이익 +10% (crossCivProfitBonus)
    /// 판매 보너스는 ReputationPricing.Quote에서 판매가에 더해, 장터 값 표시·미리보기·실제 판매가 같은 값을 쓴다.
    /// </summary>
    public static class OriginPerks
    {
        /// <summary>문명 다섯: 고려·베네치아·아라비아·북유럽은 지역 그대로, 가운데 대륙(중간 교역권·중앙 북쪽·본토 서부)은 하나로 "중앙 대륙"</summary>
        public static string Civ(RegionData region)
        {
            if (region == null) return "";
            switch (region.id) { case "goryeo": case "venice": case "arabia": case "north": return region.id; default: return "center"; }
        }

        public static string CivName(string civ)
        {
            switch (civ) { case "goryeo": return "고려"; case "venice": return "베네치아"; case "arabia": return "아라비아"; case "north": return "북유럽"; case "center": return "중앙 대륙"; default: return ""; }
        }

        /// <summary>물건이 난 문명 이름 (특산 마을이 없는 생필품은 빈 글)</summary>
        public static string ItemCivName(ItemData item) => item != null && item.originTown != null ? CivName(Civ(item.originTown.region)) : "";

        /// <summary>물건이 난 문명과 파는 마을의 문명이 다른지. 특산 마을이 없는 생필품은 아니다</summary>
        public static bool CrossCiv(ItemData item, TownData town) =>
            item != null && item.originTown != null && item.originTown.region != null && town != null && town.region != null
            && Civ(item.originTown.region) != Civ(town.region);

        /// <summary>이 판매의 이익에 더해 주는 비율 (고려 + 아라비아 조건)</summary>
        public static float ProfitRate(OriginData o, ItemData item, TownData town)
        {
            if (o == null) return 0f;
            float r = o.profitBonus;
            if (o.crossCivProfitBonus > 0f && CrossCiv(item, town)) r += o.crossCivProfitBonus;
            return r;
        }

        /// <summary>판매가 price에 붙는 강점 몫 (냥). 이익 = 판 값 − 산 값 평균, 이익이 없으면 0</summary>
        public static int SellExtra(PlayerState p, ItemData item, TownData town, int price)
        {
            if (p == null || item == null) return 0;
            float rate = ProfitRate(p.origin, item, town);
            if (rate <= 0f) return 0;
            float profit = price - p.cargo.AverageCost(item);
            return profit > 0f ? Mathf.RoundToInt(profit * rate) : 0;
        }

        /// <summary>강점 몫이 이미 들어간 판매가 price에서 그 몫만 되짚는다 (표시용). price = 기본 + (기본 − 산 값)×비율 이므로 몫 = (price − 산 값)×비율÷(1+비율)</summary>
        public static int PartOf(PlayerState p, ItemData item, TownData town, int price)
        {
            if (p == null || item == null) return 0;
            float rate = ProfitRate(p.origin, item, town);
            float profit = price - p.cargo.AverageCost(item);
            return rate > 0f && profit > 0f ? Mathf.RoundToInt(profit * rate / (1f + rate)) : 0;
        }

        /// <summary>장터 판매 줄에 붙이는 짧은 글 ("고려 +3냥")</summary>
        public static string SellTag(PlayerState p, int extra) =>
            extra > 0 && p != null && p.origin != null ? $"{CivName(p.origin.id == "viking" ? "north" : p.origin.id)} 강점 +{extra}냥" : "";
    }
}
