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

        // 출구로 이어지지 않은 지역의 장터는 거래·소문 대상에서 뺀다 (연결된 기존 마을끼리는 그대로).
        static bool Connected(TownData a, TownData b)
        {
            var world = WorldIndex.Get();
            return world == null || world.MarketsConnected(a.id, b.id);
        }

        static bool ReachableFromHere(TownData town)
        {
            var world = WorldIndex.Get();
            string here = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (world == null || string.IsNullOrEmpty(here) || world.Map(here) == null) return true;
            string target = world.MapOf("market", town.id);
            if (string.IsNullOrEmpty(target) || world.Map(target) == null) return true;
            return target == here || world.NextStep(here, target) != null;
        }
        ItemData Item(string id) => db.items.Find(i => i != null && i.id == id);

        /// <summary>
        /// 온라인에서 서버가 공유 장터 재고를 들고 있으면 NPC 상인은 그 장터에서 사고팔지 않는다 (2026-11-08).
        /// NPC는 각자 게임에서 따로 돌아서, 공유 장터에서 거래하면 사람마다 재고가 어긋난다. 혼자 하기에서는 예전처럼 거래한다.
        /// </summary>
        static bool OffLimits(TownData town) => NetMarket.ServerShared && WorldClock.Active && NetMarket.IsShared(town);

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
            if (OffLimits(town)) { tr.lastNews = $"{town.displayName} 큰 장은 사람이 붐벼서 짐을 풀지 못했소."; tr.count = 0; tr.itemId = null; tr.paid = 0; return; }
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
            if (OffLimits(from))
            {
                // 공유 장터에서는 사지 않고 가장 가까운 다른 장터로 빈 수레로 옮겨 간다
                TownData near = null; int nearSteps = int.MaxValue;
                foreach (var to in db.towns)
                {
                    if (to == null || to == from || OffLimits(to) || !Connected(from, to)) continue;
                    int steps = Mathf.Max(1, ContractManager.Distance(from, to));
                    if (steps < nearSteps) { near = to; nearSteps = steps; }
                }
                if (near == null) { tr.arriveAt = now + 3f; return; }
                tr.destination = near.id; tr.itemId = null; tr.count = 0; tr.paid = 0;
                tr.arriveAt = now + 4f * nearSteps;
                return;
            }
            var mf = market(from);
            ItemData best = null; TownData bestTo = null; float bestGain = 0;
            foreach (var s in from.stock)
            {
                if (s.item == null || s.item.questItem || mf.Stock(s.item) < 3f) continue;
                int buy = mf.BaseBuyPrice(s.item);
                foreach (var to in db.towns)
                {
                    if (to == null || to == from || OffLimits(to) || !Connected(from, to)) continue;
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
                int price = mf.BaseBuyPrice(best);
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
                if (town == null || !ReachableFromHere(town)) continue;
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
