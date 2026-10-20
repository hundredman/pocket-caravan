using System.Collections.Generic;

namespace PocketCaravan
{
    /// <summary>현재 장터에서 수량별 가격을 계산한 로컬 견적. 원격 시세/퀘스트 보상은 포함하지 않는다.</summary>
    public sealed class MarketEstimate
    {
        public readonly TownData town;
        public readonly ItemData item;
        public readonly bool selling;
        public readonly int count, total, wallet;
        public readonly float profit;
        public readonly string blocker;
        readonly int[] prices;

        internal MarketEstimate(TownData town, ItemData item, bool selling, List<int> prices, float cost, string blocker, int wallet)
        {
            this.town=town; this.item=item; this.selling=selling; this.wallet=wallet; this.prices=prices.ToArray();
            count=prices.Count; foreach(int price in prices)total+=price;
            profit=selling?total-cost*count:0; this.blocker=blocker;
        }
        public int PriceAt(int index) => prices[index];
        public bool Matches(MarketEstimate other)
        {
            if(other==null || town!=other.town || item!=other.item || selling!=other.selling || count!=other.count || total!=other.total || wallet!=other.wallet || profit!=other.profit) return false;
            for(int i=0;i<count;i++)if(prices[i]!=other.prices[i])return false;
            return true;
        }
    }
}
