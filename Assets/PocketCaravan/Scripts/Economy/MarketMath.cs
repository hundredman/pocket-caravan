using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 시세 공식. 현재가 = 기준가 × (적정 재고 ÷ 현재 재고), 기준가의 0.5~2배.
    /// 마을이 사 주는 값은 파는 값의 85% (장터 수수료).
    /// </summary>
    public static class MarketMath
    {
        public const float MinMultiplier = 0.5f;
        public const float MaxMultiplier = 2f;
        public const float BuybackRate = 0.85f;

        public static float Multiplier(float optimal, float stock)
        {
            if (stock <= 0.01f) return MaxMultiplier;
            return Mathf.Clamp(optimal / stock, MinMultiplier, MaxMultiplier);
        }

        public static int Price(float basePrice, float optimal, float stock, float demand = 1f) =>
            Mathf.Max(1, Mathf.RoundToInt(basePrice * Multiplier(optimal, stock) * demand));

        public static int Buyback(int price) => Mathf.Max(1, Mathf.FloorToInt(price * BuybackRate));
    }
}
