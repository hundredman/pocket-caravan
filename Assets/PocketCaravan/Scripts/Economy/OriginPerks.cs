using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 출신 강점 (2026-11-15 은샘, UT "강점이 애매하다"). 한 문장·숫자 하나로, 기준은 북유럽 이동 +10%.
    /// - 북유럽: 모든 이동 +10% (OriginData.speedMultiplier, PlayerController)
    /// - 베네치아: 장터 수수료 15% → 13% (marketFee)
    /// - 아라비아: 물건이 난 문명과 다른 문명의 장터에서 팔면 +5% (crossCivSellBonus)
    /// - 고려: 짐 +10%, 지게 (capacityBonus, PlayerState.CapacityOf)
    /// 판매 강점은 MarketState.BaseSellPrice(item, origin)에 들어가 장터 값 표시·미리보기·실제 판매가 같은 값을 쓴다.
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

        /// <summary>장터 수수료 (출신이 없으면 기본 15%)</summary>
        public static float Fee(OriginData o) => o != null ? Mathf.Clamp(o.marketFee, 0f, 0.5f) : 1f - MarketMath.BuybackRate;

        /// <summary>판매가에 더하는 비율 (아라비아: 다른 문명 물건)</summary>
        public static float SellBonus(OriginData o, ItemData item, TownData town) =>
            o != null && o.crossCivSellBonus > 0f && CrossCiv(item, town) ? o.crossCivSellBonus : 0f;

        /// <summary>장터 판매 칸에 붙이는 짧은 글. 강점이 이 판매에 들어가면 "아라비아 강점 +5%"·"베네치아 수수료 13%", 아니면 빈 글</summary>
        public static string SellTag(OriginData o, ItemData item, TownData town)
        {
            if (o == null) return "";
            float b = SellBonus(o, item, town);
            if (b > 0f) return $"아라비아 강점 +{b * 100f:0}%";
            float fee = Fee(o);
            if (fee < 1f - MarketMath.BuybackRate - 0.001f) return $"베네치아 수수료 {fee * 100f:0}%";
            return "";
        }
    }
}
