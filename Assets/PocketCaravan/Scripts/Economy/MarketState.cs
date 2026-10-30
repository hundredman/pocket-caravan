using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>마을 장터의 실행 중 상태 (재고, 누적 거래액, 등급). 씬을 떠나도 GameManager에 남는다.</summary>
    public class MarketState
    {
        public readonly TownData town;
        readonly GameDatabase db;
        readonly bool publishGrowth;
        public const int MaxBatch = 100;
        readonly Dictionary<ItemData, float> stock = new Dictionary<ItemData, float>();

        public int Level { get; private set; }
        public float TradeVolume { get; private set; }

        public MarketState(TownData town, GameDatabase db) : this(town,db,true) { }

        MarketState(TownData town, GameDatabase db, bool publishGrowth)
        {
            this.town = town;
            this.db = db;
            this.publishGrowth = publishGrowth;
            Level = Mathf.Clamp(town.startLevel, 1, db.maxTownLevel);
        }

        public string LevelName => Level < db.townLevelNames.Length ? db.townLevelNames[Level] : Level.ToString();

        // 등급이 높을수록 적정 재고가 커서 많이 팔아도 값이 덜 떨어진다.
        float LevelMultiplier => 1f + 0.5f * (Level - 1);

        public float Optimal(ItemData item) => town.OptimalFor(item) * LevelMultiplier;

        public IEnumerable<ItemData> KnownItems => stock.Keys;

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
        public int BaseBuyPrice(ItemData item) => MarketMath.Price(town.BasePriceFor(item), Optimal(item), Stock(item), Demand);

        static OriginData Origin => GameManager.I != null ? GameManager.I.Player.origin : null;

        /// <summary>팔 때 재고가 늘어나는 양. 베네치아는 다른 지역 물건을 팔 때 덜 늘어 값이 덜 떨어진다.</summary>
        public float SellStockDelta(ItemData item)
        {
            return SellStockDelta(item,Origin);
        }
        float SellStockDelta(ItemData item,OriginData o)
        {
            bool foreign = item.originTown != null && item.originTown.region != town.region;
            return foreign && o != null ? o.resaleDropMultiplier : 1f;
        }

        /// <summary>플레이어가 팔 때 받는 값 (한 개를 더 넣었을 때 기준)</summary>
        public int BaseSellPrice(ItemData item) => BaseSellPrice(item,Origin);
        public int BaseSellPrice(ItemData item,OriginData o)
        {
            int p = MarketMath.Buyback(MarketMath.Price(town.BasePriceFor(item), Optimal(item), Stock(item) + SellStockDelta(item,o), Demand));
            if (o != null && o.longHaulBonus > 0 && item.MapsFrom(town) >= o.longHaulMinMaps)
                p = Mathf.RoundToInt(p * (1f + o.longHaulBonus));
            return p;
        }

        public int BuyPrice(ItemData item) => ReputationPricing.Quote(this,item,false,GameManager.I?.Player,GameManager.I?.Player?.tradeActivity,GameManager.I?.Clock.TotalHours??0);
        public int SellPrice(ItemData item) => ReputationPricing.Quote(this,item,true,GameManager.I?.Player,GameManager.I?.Player?.tradeActivity,GameManager.I?.Clock.TotalHours??0);

        public bool CanBuy(ItemData item) => Stock(item) >= 1f;

        /// <summary>NPC 상인이 팔 때 받는 값 (출신 강점 없음)</summary>
        public int NpcSellPrice(ItemData item) => MarketMath.Buyback(MarketMath.Price(town.BasePriceFor(item), Optimal(item), Stock(item) + 1f, Demand));

        public void RecordNpcSell(ItemData item, int price)
        {
            stock[item] = Stock(item) + 1f;
            AddVolume(price);
        }

        public void RecordBuy(ItemData item, int price)
        {
            stock[item] = Stock(item) - 1f;
            AddVolume(price);
        }

        public void RecordSell(ItemData item, int price) => RecordSell(item,price,Origin);
        public void RecordSell(ItemData item,int price,OriginData origin)
        {
            stock[item] = Stock(item) + SellStockDelta(item,origin);
            AddVolume(price);
        }

        void AddVolume(int amount)
        {
            TradeVolume += amount;
            while (Level < db.maxTownLevel && Level < db.townLevelVolume.Length && TradeVolume >= db.townLevelVolume[Level])
            {
                Level++;
                if (publishGrowth) GameEvents.RaiseTownLeveled(town, Level);
            }
        }

        /// <summary>가격·재고·시장 등급 변화를 복제 상태에서 계산한다. 화물/잔고/성장 이벤트를 변경하지 않는다.</summary>
        public MarketEstimate Preview(ItemData item, PlayerState player, int requested, bool selling)
        {
            var prices=new List<int>(); string blocker=null;
            if(item==null || item.questItem) blocker="일반 교역품을 선택하세요.";
            else if(player==null || requested<=0) blocker="수량을 확인하세요.";
            else if(float.IsNaN(item.weight) || float.IsInfinity(item.weight) || item.weight<=0) blocker="물품 무게를 확인하세요.";
            else if(!selling && ReputationPricing.BuyDenial(player,town,item)!=null) blocker=ReputationPricing.BuyDenial(player,town,item);
            else
            {
                var activity=player.tradeActivity.Clone();float now=GameManager.I?.Clock.TotalHours??0;
                var copy=new MarketState(town,db,false) { Level=Level, TradeVolume=TradeVolume };
                foreach(var row in stock)copy.stock[row.Key]=row.Value;
                long total=0; int limit=Mathf.Min(requested,MaxBatch);
                for(int i=0;i<limit;i++)
                {
                    if(selling)
                    {
                        if(i>=player.cargo.Count(item)){blocker="팔 물건이 없습니다.";break;}
                        int price=ReputationPricing.Quote(copy,item,true,player,activity,now);
                        if(price<0 || total+price>int.MaxValue || (long)player.money+total+price>int.MaxValue){blocker="보유금 한도를 확인하세요.";break;}
                        total+=price; prices.Add(price); activity.Sold(town.id,item.id,now);copy.RecordSell(item,price,player.origin);
                    }
                    else
                    {
                        if(!copy.CanBuy(item)){blocker="재고가 없습니다.";break;}
                        int price=ReputationPricing.Quote(copy,item,false,player,activity,now);
                        if(price<=0 || total+price>player.money){blocker="매입 자금이 부족합니다.";break;}
                        if(!player.CanCarry(item,i+1)){blocker="남은 적재 공간이 부족합니다.";break;}
                        total+=price; prices.Add(price); activity.Bought(town.id,item.id,now,price);copy.RecordBuy(item,price);
                    }
                }
            }
            return new MarketEstimate(town,item,selling,prices,player!=null?player.cargo.AverageCost(item):0,blocker,player!=null?player.money:0);
        }

        // ---------- 저장 ----------
        [System.Serializable]
        public class Save
        {
            public string town;
            public int level;
            public float volume;
            public List<string> items = new List<string>();
            public List<float> stocks = new List<float>();
        }

        public Save Export()
        {
            var s = new Save { town = town.id, level = Level, volume = TradeVolume };
            foreach (var kv in stock) { s.items.Add(kv.Key.id); s.stocks.Add(kv.Value); }
            return s;
        }

        public void Import(Save s, System.Func<string, ItemData> item)
        {
            Level = Mathf.Clamp(s.level, 1, db.maxTownLevel);
            TradeVolume = s.volume;
            stock.Clear();
            for (int i = 0; i < s.items.Count && i < s.stocks.Count; i++)
            {
                var it = item(s.items[i]);
                if (it != null) stock[it] = s.stocks[i];
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
