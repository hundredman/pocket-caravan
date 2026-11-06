using System;
using System.Collections.Generic;

namespace PocketCaravan
{
    /// <summary>이 플레이어가 직접 확인한 시세. 원격 시장을 실시간 조회하거나 수익을 보장하지 않는다.</summary>
    public sealed class TradeLedger
    {
        [Serializable]
        public class Quote
        {
            public string town, item;
            public int buy, sell;
            /// <summary>2026-11-05 이 값을 본 날 (게임 날짜). 0이면 모름 (그 전 저장)</summary>
            public int seenDay;
        }

        readonly List<Quote> quotes = new List<Quote>();

        public Quote Find(string town, string item) => quotes.Find(q => q.town == town && q.item == item);
        public void Clear() => quotes.Clear();

        /// <summary>장터를 열었을 때 그곳 값을 적는다. day = 오늘 게임 날짜 (장부에 "N일 전 확인"으로 나온다)</summary>
        public void Observe(TownData town, MarketState market, IEnumerable<ItemData> items, int day = 0)
        {
            if (town == null || market == null) return;
            foreach (var item in items)
            {
                if (item == null || item.questItem) continue;
                var q = Find(town.id, item.id);
                if (q == null) { q = new Quote { town = town.id, item = item.id }; quotes.Add(q); }
                q.buy = market.BuyPrice(item);
                q.sell = market.SellPrice(item);
                q.seenDay = day;
            }
        }

        public List<Quote> Export()
        {
            var result = new List<Quote>();
            foreach (var q in quotes) result.Add(new Quote { town = q.town, item = q.item, buy = q.buy, sell = q.sell, seenDay = q.seenDay });
            return result;
        }

        public void Import(List<Quote> saved)
        {
            Clear();
            if (saved == null) return; // 이전 저장에는 장부가 없다.
            foreach (var q in saved)
            {
                if (q == null || string.IsNullOrEmpty(q.town) || string.IsNullOrEmpty(q.item) || q.buy < 0 || q.sell < 0) continue;
                var old = Find(q.town, q.item);
                if (old != null) quotes.Remove(old);
                quotes.Add(new Quote { town = q.town, item = q.item, buy = q.buy, sell = q.sell, seenDay = Math.Max(0, q.seenDay) });   // 옛 저장은 0 (모름)
            }
        }
    }
}
