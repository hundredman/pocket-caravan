using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// NPC 상인. 플레이어와 같은 장터 규칙으로 싼 곳에서 사서 비싼 곳에 판다.
    /// 화면 밖에서는 시간 계산만 하고 (마을 사이 이동 = 거리 × 4시간), 거래는 재고·값·마을 거래액에 그대로 반영된다.
    /// 신중파는 이익이 넉넉할 때만, 투기파는 조금만 남아도 산다.
    /// </summary>
    public class NpcTraders
    {
        [Serializable]
        public class Trader
        {
            public string name;
            public bool cautious;
            public string town;          // 있는 곳 (이동 중이면 출발지)
            public string destination;   // 이동 중이면 목적지
            public float arriveAt;
            public string itemId;
            public int count;
            public float paid;
            public string lastNews = "";
        }

        public List<Trader> traders = new List<Trader>();
        readonly GameDatabase db;
        readonly GameClock clock;
        readonly Func<TownData, MarketState> market;

        public NpcTraders(GameDatabase db, GameClock clock, Func<TownData, MarketState> market)
        {
            this.db = db; this.clock = clock; this.market = market;
        }

        public void Init()
        {
            if (traders.Count > 0 || db.towns.Count < 2) return;
            string[] names = { "쇠돌", "막순", "개똥", "언년" };
            for (int i = 0; i < names.Length; i++)
            {
                var t = db.towns[i % db.towns.Count];
                traders.Add(new Trader { name = names[i], cautious = i % 2 == 0, town = t.id, arriveAt = clock.TotalHours + i });
            }
        }

        TownData Town(string id) => db.towns.Find(t => t != null && t.id == id);
        ItemData Item(string id) => db.items.Find(i => i != null && i.id == id);

        public void OnHour()
        {
            float now = clock.TotalHours;
            foreach (var tr in traders)
            {
                if (now < tr.arriveAt) continue;
                if (!string.IsNullOrEmpty(tr.destination)) { tr.town = tr.destination; tr.destination = null; Sell(tr); }
                PlanNext(tr, now);
            }
        }

        void Sell(Trader tr)
        {
            var town = Town(tr.town);
            var item = Item(tr.itemId);
            if (town == null || item == null || tr.count <= 0) return;
            var m = market(town);
            int total = 0;
            for (int i = 0; i < tr.count; i++)
            {
                int price = m.NpcSellPrice(item);
                m.RecordNpcSell(item, price);
                total += price;
            }
            int profit = Mathf.RoundToInt(total - tr.paid);
            tr.lastNews = $"{town.displayName}에 {item.displayName} {tr.count}개를 팔아 {(profit >= 0 ? profit + "냥 남겼소" : -profit + "냥 밑졌소")}.";
            tr.count = 0; tr.itemId = null; tr.paid = 0;
        }

        void PlanNext(Trader tr, float now)
        {
            var from = Town(tr.town);
            if (from == null) return;
            var mf = market(from);
            ItemData best = null; TownData bestTo = null; float bestGain = 0;
            foreach (var s in from.stock)
            {
                if (s.item == null || s.item.questItem || mf.Stock(s.item) < 3f) continue;
                int buy = mf.BuyPrice(s.item);
                foreach (var to in db.towns)
                {
                    if (to == null || to == from) continue;
                    float gain = (market(to).NpcSellPrice(s.item) - buy) / (float)buy;
                    gain /= Mathf.Max(1, ContractManager.Distance(from, to));   // 가까울수록 좋음
                    if (gain > bestGain) { bestGain = gain; best = s.item; bestTo = to; }
                }
            }
            float need = tr.cautious ? 0.15f : 0.04f;
            if (best == null || bestGain < need)
            {
                tr.arriveAt = now + 3f;   // 쉬었다가 다시 본다
                return;
            }
            int n = 0; float paid = 0;
            for (int i = 0; i < 4 && mf.Stock(best) >= 2f; i++)
            {
                int price = mf.BuyPrice(best);
                mf.RecordBuy(best, price);
                paid += price; n++;
            }
            tr.itemId = best.id; tr.count = n; tr.paid = paid;
            tr.destination = bestTo.id;
            tr.arriveAt = now + 4f * Mathf.Max(1, ContractManager.Distance(from, bestTo));
            tr.lastNews = $"{from.displayName}에서 {Josa.Eul(best.displayName)} 사서 {bestTo.displayName}에 팔러 가는 길이오.";
        }

        /// <summary>장터 소문: 지금 산지보다 가장 비싸게 팔리는 곳</summary>
        public string PriceTip()
        {
            string tip = null; float bestRatio = 1.15f;
            foreach (var town in db.towns)
            {
                if (town == null) continue;
                var m = market(town);
                foreach (var item in db.items)
                {
                    if (item == null || item.questItem) continue;
                    float ratio = m.SellPrice(item) / Mathf.Max(1f, item.basePrice);
                    if (ratio > bestRatio) { bestRatio = ratio; tip = $"요즘 {town.displayName}에서 {Josa.Eul(item.displayName)} 비싸게 쳐 준다더군. 하나에 {m.SellPrice(item)}냥쯤."; }
                }
            }
            return tip ?? "요즘은 값이 다들 고만고만하오.";
        }
    }
}
