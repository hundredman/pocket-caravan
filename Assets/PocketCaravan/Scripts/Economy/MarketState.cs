using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>마을 장터의 실행 중 상태 (재고, 누적 거래액, 등급). 씬을 떠나도 GameManager에 남는다.</summary>
    public class MarketState
    {
        public readonly TownData town;
        readonly GameDatabase db;
        readonly Dictionary<ItemData, float> stock = new Dictionary<ItemData, float>();

        public int Level { get; private set; }
        public float TradeVolume { get; private set; }

        public MarketState(TownData town, GameDatabase db)
        {
            this.town = town;
            this.db = db;
            Level = Mathf.Clamp(town.startLevel, 1, db.maxTownLevel);
        }

        public string LevelName => Level < db.townLevelNames.Length ? db.townLevelNames[Level] : Level.ToString();

        // 등급이 높을수록 적정 재고가 커서 많이 팔아도 값이 덜 떨어진다.
        float LevelMultiplier => 1f + 0.5f * (Level - 1);

        public float Optimal(ItemData item) => town.OptimalFor(item) * LevelMultiplier;

        public float Stock(ItemData item)
        {
            if (!stock.TryGetValue(item, out var s))
            {
                s = Optimal(item);
                stock[item] = s;
            }
            return s;
        }

        float Demand => GameManager.I != null && GameManager.I.Clock.IsMarketDay ? db.marketDayDemand : 1f;

        /// <summary>플레이어가 살 때 값</summary>
        public int BuyPrice(ItemData item) => MarketMath.Price(item.BasePriceAt(town), Optimal(item), Stock(item), Demand);

        /// <summary>플레이어가 팔 때 받는 값 (한 개를 더 넣었을 때 기준)</summary>
        public int SellPrice(ItemData item) =>
            MarketMath.Buyback(MarketMath.Price(item.BasePriceAt(town), Optimal(item), Stock(item) + 1f, Demand));

        public bool CanBuy(ItemData item) => Stock(item) >= 1f;

        public void RecordBuy(ItemData item, int price)
        {
            stock[item] = Stock(item) - 1f;
            AddVolume(price);
        }

        public void RecordSell(ItemData item, int price)
        {
            stock[item] = Stock(item) + 1f;
            AddVolume(price);
        }

        void AddVolume(int amount)
        {
            TradeVolume += amount;
            // 추후: 3등급은 '길' 이상, 4등급은 '큰길' 이상 연결 조건
            while (Level < db.maxTownLevel && Level < db.townLevelVolume.Length && TradeVolume >= db.townLevelVolume[Level])
            {
                Level++;
                Debug.Log($"[마을 발전] {town.displayName}이(가) {LevelName}(으)로 커졌습니다.");
            }
        }

        /// <summary>산지는 생산으로, 소비지는 소비로 재고가 적정치에 돌아간다.</summary>
        public void RecoverOneHour()
        {
            var keys = new List<ItemData>(stock.Keys);
            foreach (var k in keys)
            {
                float opt = Optimal(k);
                stock[k] = Mathf.Lerp(stock[k], opt, db.stockRecoveryPerHour);
            }
        }
    }
}
