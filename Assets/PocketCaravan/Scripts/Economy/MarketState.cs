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

        public float Optimal(ItemData item) => town.OptimalFor(item) * LevelMultiplier * NetMarket.ScaleFor(town);

        public IEnumerable<ItemData> KnownItems => stock.Keys;

        /// <summary>현재 재고. 재고표에 없는 물건은 적정치로 본다 — 2026-11-05(기우): 값만 물어볼 때는 재고표에 끼워 넣지 않는다.
        /// (NPC 상인이 모든 장터의 모든 물건 값을 훑는 바람에 어느 장터든 전 세계 품목 53개가 다 떴었다. 실제 거래가 있을 때만 표에 오른다.)</summary>
        public float Stock(ItemData item) => stock.TryGetValue(item, out var s) ? s : Optimal(item);

        /// <summary>재고를 정한다 (공유 장터 서버 값 맞추기 · 남의 거래 반영, 2026-11-08). 재고표에 없던 물건이면 올린다.</summary>
        public void SetStock(ItemData item, float value) { if (item != null && !float.IsNaN(value) && !float.IsInfinity(value)) stock[item] = Mathf.Max(0f, value); }

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

        public int BuyPrice(ItemData item) => BuyPrice(item,0);
        public int SellPrice(ItemData item) => SellPrice(item,0);
        // haggle: 이번 거래에만 쓰는 흥정 폭(R3). 평판 상한과 동시 견적 규칙은 Quote가 다시 적용한다.
        public int BuyPrice(ItemData item,float haggle) => ReputationPricing.Quote(this,item,false,GameManager.I?.Player,GameManager.I?.Player?.tradeActivity,GameManager.I?.Clock.TotalHours??0,haggle,0);
        public int SellPrice(ItemData item,float haggle) => ReputationPricing.Quote(this,item,true,GameManager.I?.Player,GameManager.I?.Player?.tradeActivity,GameManager.I?.Clock.TotalHours??0,0,haggle);

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
        public MarketEstimate Preview(ItemData item, PlayerState player, int requested, bool selling) => Preview(item,player,requested,selling,0);
        public MarketEstimate Preview(ItemData item, PlayerState player, int requested, bool selling, float haggle)
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
                        int price=ReputationPricing.Quote(copy,item,true,player,activity,now,0,haggle);
                        if(price<0 || total+price>int.MaxValue || (long)player.money+total+price>int.MaxValue){blocker="보유금 한도를 확인하세요.";break;}
                        total+=price; prices.Add(price); activity.Sold(town.id,item.id,now);copy.RecordSell(item,price,player.origin);
                    }
                    else
                    {
                        if(!copy.CanBuy(item)){blocker="재고가 없습니다.";break;}
                        int price=ReputationPricing.Quote(copy,item,false,player,activity,now,haggle,0);
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

        /// <summary>저장 파일용: 온라인 공유 장터 배율을 뺀 재고(차이는 그대로)로 남긴다. 혼자 하기로 불러와도 값이 튀지 않는다.</summary>
        public Save ExportForSave()
        {
            float scale = NetMarket.ScaleFor(town);
            if (scale == 1f) return Export();
            var s = new Save { town = town.id, level = Level, volume = TradeVolume };
            foreach (var kv in stock)
            {
                float opt = Optimal(kv.Key);
                s.items.Add(kv.Key.id); s.stocks.Add(Mathf.Max(0f, kv.Value - opt + opt / scale));
            }
            return s;
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

        /// <summary>
        /// 여러 시간 몫의 회복을 한 번에 (2026-11-08 접속으로 세상 시간이 건너뛸 때). RecoverOneHour를 hours번 한 것과 같다:
        /// 적정치와의 차이가 (1 − 비율)^hours 배가 된다.
        /// </summary>
        public void RecoverHours(float hours)
        {
            if (!(hours > 0f)) return;
            float keep = Mathf.Pow(1f - Mathf.Clamp01(db.stockRecoveryPerHour), hours);
            var keys = new List<ItemData>(stock.Keys);
            foreach (var k in keys)
            {
                float opt = Optimal(k);
                stock[k] = opt + (stock[k] - opt) * keep;
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
