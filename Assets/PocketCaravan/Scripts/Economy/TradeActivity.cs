using System;
using System.Collections.Generic;
using System.IO;

namespace PocketCaravan
{
    /// <summary>Character-owned recent purchases and daily eligible sales; survives map changes.</summary>
    public sealed class TradeActivity
    {
        [Serializable] public class Purchase { public string town, item; public int quantity; public double averagePaid; public float lastBought; }
        [Serializable] public class Daily { public string town; public int day; public long revenue; public bool foreignIncluded, awarded; }
        [Serializable] public class Save { public int schemaVersion; public List<Purchase> purchases = new List<Purchase>(); public List<Daily> daily = new List<Daily>(); }
        readonly List<Purchase> purchases = new List<Purchase>();
        readonly List<Daily> daily = new List<Daily>();
        public const float WindowHours = 12;
        static Purchase Copy(Purchase p) => new Purchase { town=p.town,item=p.item,quantity=p.quantity,averagePaid=p.averagePaid,lastBought=p.lastBought };
        static Daily Copy(Daily d) => new Daily {town=d.town,day=d.day,revenue=d.revenue,foreignIncluded=d.foreignIncluded,awarded=d.awarded};
        public void Clear() { purchases.Clear(); daily.Clear(); }
        Purchase Active(string town,string item,float now) => purchases.Find(p=>p.town==town && p.item==item && p.quantity>0 && now>=p.lastBought && now-p.lastBought<WindowHours);
        public bool Restricted(string town,string item,float now) => Active(town,item,now)!=null;
        public int LimitSale(string town,string item,float now,int quote)
        {
            var p=Active(town,item,now);
            return p==null?quote:Math.Min(quote,Math.Max(0,(int)Math.Floor(p.averagePaid-1)));
        }
        public void Bought(string town,string item,float now,int price)
        {
            if(string.IsNullOrWhiteSpace(town)||string.IsNullOrWhiteSpace(item)||price<1||float.IsNaN(now)||float.IsInfinity(now)||now<0)throw new ArgumentException("매입 기록이 올바르지 않습니다.");
            var p=Active(town,item,now);
            if(p==null) {purchases.RemoveAll(x=>x.town==town && x.item==item);p=new Purchase{town=town,item=item,lastBought=now};purchases.Add(p);}
            if(p.quantity==int.MaxValue)throw new InvalidOperationException("매입 기록 수량 한도입니다.");
            p.averagePaid=(p.averagePaid*p.quantity+price)/(p.quantity+1);p.quantity++;p.lastBought=now;
        }
        /// <summary>Average basis stays constant when consuming part of the record.</summary>
        public bool Sold(string town,string item,float now)
        {
            var p=Active(town,item,now);if(p==null)return false;
            if(--p.quantity==0)purchases.Remove(p);return true;
        }
        public ReputationSize EligibleSale(string town,int day,int revenue,bool foreign,bool restricted)
        {
            if(restricted||revenue<=0)return ReputationSize.None;
            var d=daily.Find(x=>x.town==town);
            if(d==null){d=new Daily{town=town,day=day};daily.Add(d);}
            if(d.day!=day){d.day=day;d.revenue=0;d.foreignIncluded=false;d.awarded=false;}
            if(d.awarded)return ReputationSize.None;
            d.revenue=Math.Min(int.MaxValue,d.revenue+(long)revenue);d.foreignIncluded|=foreign;
            if(d.revenue<50)return ReputationSize.None;
            d.awarded=true;return d.foreignIncluded?ReputationSize.Medium:ReputationSize.Small;
        }
        public Save Export()
        {
            var s=new Save{schemaVersion=1};foreach(var p in purchases)s.purchases.Add(Copy(p));foreach(var d in daily)s.daily.Add(Copy(d));
            s.purchases.Sort((a,b)=>{int c=StringComparer.Ordinal.Compare(a.town,b.town);return c!=0?c:StringComparer.Ordinal.Compare(a.item,b.item);});
            s.daily.Sort((a,b)=>StringComparer.Ordinal.Compare(a.town,b.town));return s;
        }
        public void Import(Save saved) {Validate(saved);Clear();if(saved==null||saved.schemaVersion==0)return;foreach(var p in saved.purchases)purchases.Add(Copy(p));foreach(var d in saved.daily)daily.Add(Copy(d));}
        public TradeActivity Clone(){var result=new TradeActivity();result.Import(Export());return result;}
        public static void Validate(Save s)
        {
            if(s==null)return;
            if(s.schemaVersion==0 && (s.purchases==null||s.purchases.Count==0) && (s.daily==null||s.daily.Count==0))return;
            if(s.schemaVersion!=1||s.purchases==null||s.daily==null)throw new InvalidDataException("지원하지 않는 거래 기록입니다.");
            var keys=new HashSet<string>();
            foreach(var p in s.purchases)
                if(p==null||string.IsNullOrWhiteSpace(p.town)||string.IsNullOrWhiteSpace(p.item)||p.quantity<1||p.averagePaid<1||p.averagePaid>int.MaxValue||double.IsNaN(p.averagePaid)||double.IsInfinity(p.averagePaid)||float.IsNaN(p.lastBought)||float.IsInfinity(p.lastBought)||p.lastBought<0||!keys.Add(p.town+"\n"+p.item))throw new InvalidDataException("최근 매입 기록이 올바르지 않습니다.");
            keys.Clear();foreach(var d in s.daily)
                if(d==null||string.IsNullOrWhiteSpace(d.town)||d.day<1||d.revenue<0||d.revenue>int.MaxValue||(!d.awarded&&d.revenue>=50)||!keys.Add(d.town))throw new InvalidDataException("일일 판매 기록이 올바르지 않습니다.");
        }
    }
}
