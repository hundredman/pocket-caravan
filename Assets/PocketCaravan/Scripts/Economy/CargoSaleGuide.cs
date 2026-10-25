using System.Collections.Generic;

namespace PocketCaravan
{
    /// <summary>Player-observed first-unit quotes and registered graph distance, never live remote markets.</summary>
    public static class CargoSaleGuide
    {
        public sealed class Choice
        {
            public TownData town;
            public int sell, hops;
            public string nextMap;
        }

        public static List<Choice> Observed(TradeLedger ledger, List<TownData> towns, ItemData item, WorldIndex world, string here)
        {
            var result=new List<Choice>();
            if(ledger==null || towns==null || item==null || item.questItem)return result;
            var seen=new HashSet<string>();
            foreach(var town in towns)
            {
                if(town==null || string.IsNullOrEmpty(town.id) || !seen.Add(town.id))continue;
                var quote=ledger.Find(town.id,item.id);
                if(quote==null)continue;
                string target=world!=null?world.MapOf("market",town.id):null;
                int hops=Distance(world,here,target);
                result.Add(new Choice {town=town,sell=quote.sell,hops=hops,nextMap=hops>0?world.NextStep(here,target):null});
            }
            result.Sort((a,b)=>{
                int price=b.sell.CompareTo(a.sell);
                if(price!=0)return price;
                int distance=(a.hops<0?int.MaxValue:a.hops).CompareTo(b.hops<0?int.MaxValue:b.hops);
                return distance!=0?distance:string.CompareOrdinal(a.town.id,b.town.id);
            });
            return result;
        }

        // Each edge is one registered map transition, not walking time or terrain cost.
        public static int Distance(WorldIndex world,string from,string to)
        {
            if(world==null || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || world.Map(from)==null || world.Map(to)==null)return -1;
            var distances=new Dictionary<string,int> {{from,0}};
            var queue=new Queue<string>();queue.Enqueue(from);
            while(queue.Count>0)
            {
                string current=queue.Dequeue();
                if(current==to)return distances[current];
                var map=world.Map(current);
                if(map==null || map.neighbors==null)continue;
                foreach(string next in map.neighbors)
                {
                    if(string.IsNullOrEmpty(next) || world.Map(next)==null || distances.ContainsKey(next))continue;
                    distances[next]=distances[current]+1;queue.Enqueue(next);
                }
            }
            return -1;
        }
    }
}
