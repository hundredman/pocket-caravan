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

        /// <summary>2026-11-08 최근 거래 (장부 "최근 거래" 칸). 같은 장터에서 같은 물건을 이어서 사고팔면 한 줄로 묶는다. 최대 5줄</summary>
        [Serializable]
        public class Trade
        {
            public bool sell;
            public string town, item;
            public int count, total, profit, day;
        }
        public const int RecentMax = 5;
        readonly List<Trade> recent = new List<Trade>();
        public IReadOnlyList<Trade> Recent => recent;

        public void Record(bool sell, TownData town, ItemData item, int price, int profit, int day)
        {
            if (town == null || item == null) return;
            var last = recent.Count > 0 ? recent[0] : null;
            if (last != null && last.sell == sell && last.town == town.id && last.item == item.id && last.day == day)
            { last.count++; last.total += price; last.profit += profit; return; }
            recent.Insert(0, new Trade { sell = sell, town = town.id, item = item.id, count = 1, total = price, profit = profit, day = day });
            if (recent.Count > RecentMax) recent.RemoveRange(RecentMax, recent.Count - RecentMax);
        }
        public List<Trade> ExportRecent()
        {
            var r = new List<Trade>();
            foreach (var t in recent) r.Add(new Trade { sell = t.sell, town = t.town, item = t.item, count = t.count, total = t.total, profit = t.profit, day = t.day });
            return r;
        }
        public void ImportRecent(List<Trade> saved)
        {
            recent.Clear();
            if (saved == null) return;   // 11/08 전 저장에는 없다
            foreach (var t in saved) if (t != null && !string.IsNullOrEmpty(t.town) && !string.IsNullOrEmpty(t.item) && t.count > 0 && recent.Count < RecentMax) recent.Add(t);
        }

        /// <summary>
        /// 2026-11-08 "N일 전" 글. 온라인 세상 시계는 처음 접속하거나 오래 쉬었다 오면 날짜가 크게 건너뛴다(혼자 하던 5일째 → 세상 1700일째 등).
        /// 그만큼 지난 값은 날 수가 의미 없으니 LongAgoDays일 넘으면 "오래 전"으로 쓴다.
        /// </summary>
        public const int LongAgoDays = 10;
        public static string Ago(int today, int day)
        {
            int days = today - day;
            if (days <= 0) return "오늘";
            return days > LongAgoDays ? "오래 전" : $"{days}일 전";
        }

        public Quote Find(string town, string item) => quotes.Find(q => q.town == town && q.item == item);
        public void Clear() { quotes.Clear(); recent.Clear(); }

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
