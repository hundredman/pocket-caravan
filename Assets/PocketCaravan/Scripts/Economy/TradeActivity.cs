using System;
using System.Collections.Generic;
using System.IO;

namespace PocketCaravan
{
    /// <summary>Character-owned recent purchases, daily eligible sales, haggling limits and reputation-recovery progress; survives map changes.</summary>
    public sealed class TradeActivity
    {
        [Serializable] public class Purchase { public string town, item; public int quantity; public double averagePaid; public float lastBought; }
        [Serializable] public class Daily { public string town; public int day; public long revenue; public bool foreignIncluded, awarded; }
        // R3: 흥정 실패한 날(마을별)과 거래 거절 시각. R4: 진행 중인 마을 일(하나)과 속죄 의뢰 재개방 시각.
        [Serializable] public class HaggleLock { public string town; public int day; }
        [Serializable] public class TradeBan { public string town; public float until; }
        [Serializable] public class Chore { public string town; public int kind; public float acceptedAt; }
        [Serializable] public class Reopen { public int scope; public string target; public float until; }
        [Serializable] public class Save
        {
            public int schemaVersion; public List<Purchase> purchases = new List<Purchase>(); public List<Daily> daily = new List<Daily>();
            // 이 목록들이 없던 저장은 빈 목록으로 읽는다(형식 버전은 그대로 1).
            public List<HaggleLock> haggleLocks = new List<HaggleLock>(); public List<TradeBan> bans = new List<TradeBan>();
            public List<Chore> chores = new List<Chore>(); public List<Reopen> atonementReopen = new List<Reopen>();
        }
        readonly List<Purchase> purchases = new List<Purchase>();
        readonly List<Daily> daily = new List<Daily>();
        readonly List<HaggleLock> haggleLocks = new List<HaggleLock>();
        readonly List<TradeBan> bans = new List<TradeBan>();
        readonly List<Chore> chores = new List<Chore>();
        readonly List<Reopen> reopen = new List<Reopen>();
        public const float WindowHours = 12;
        static Purchase Copy(Purchase p) => new Purchase { town=p.town,item=p.item,quantity=p.quantity,averagePaid=p.averagePaid,lastBought=p.lastBought };
        static Daily Copy(Daily d) => new Daily {town=d.town,day=d.day,revenue=d.revenue,foreignIncluded=d.foreignIncluded,awarded=d.awarded};
        public void Clear() { purchases.Clear(); daily.Clear(); haggleLocks.Clear(); bans.Clear(); chores.Clear(); reopen.Clear(); }

        // ---------- R3 흥정 ----------
        public bool HaggleLocked(string town,int day) => haggleLocks.Exists(x=>x.town==town && x.day==day);
        public void LockHaggle(string town,int day)
        {
            Require(town);haggleLocks.RemoveAll(x=>x.town==town);haggleLocks.Add(new HaggleLock{town=town,day=day});
        }
        public float BanRemaining(string town,float now)
        {
            var b=bans.Find(x=>x.town==town);return b==null||now>=b.until?0:b.until-now;
        }
        public void Ban(string town,float until)
        {
            Require(town);if(float.IsNaN(until)||float.IsInfinity(until)||until<0)throw new ArgumentException("거래 거절 시각이 올바르지 않습니다.");
            bans.RemoveAll(x=>x.town==town);bans.Add(new TradeBan{town=town,until=until});
        }

        // ---------- R4 마을 일·속죄 의뢰 ----------
        public Chore ActiveChore => chores.Count>0?CopyChore(chores[0]):null;
        public void StartChore(string town,int kind,float now)
        {
            Require(town);if(chores.Count>0)throw new InvalidOperationException("마을 일은 한 번에 하나만 맡습니다.");
            chores.Add(new Chore{town=town,kind=kind,acceptedAt=now});
        }
        /// <summary>완료 처리는 한 번만 성공한다(같은 완료를 다시 보고해도 중복 보상 없음).</summary>
        public bool FinishChore(string town)
        {
            int removed=chores.RemoveAll(x=>x.town==town);return removed>0;
        }
        public float AtonementReopensAt(int scope,string target)
        {
            var r=reopen.Find(x=>x.scope==scope && x.target==target);return r!=null?r.until:0;
        }
        public void SetAtonementReopen(int scope,string target,float until)
        {
            Require(target);reopen.RemoveAll(x=>x.scope==scope && x.target==target);reopen.Add(new Reopen{scope=scope,target=target,until=until});
        }
        static void Require(string id){if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("마을/대상 ID가 없습니다.");}
        static Chore CopyChore(Chore c)=>new Chore{town=c.town,kind=c.kind,acceptedAt=c.acceptedAt};
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
            foreach(var x in haggleLocks)s.haggleLocks.Add(new HaggleLock{town=x.town,day=x.day});
            foreach(var x in bans)s.bans.Add(new TradeBan{town=x.town,until=x.until});
            foreach(var x in chores)s.chores.Add(CopyChore(x));
            foreach(var x in reopen)s.atonementReopen.Add(new Reopen{scope=x.scope,target=x.target,until=x.until});
            s.haggleLocks.Sort((a,b)=>StringComparer.Ordinal.Compare(a.town,b.town));s.bans.Sort((a,b)=>StringComparer.Ordinal.Compare(a.town,b.town));
            s.atonementReopen.Sort((a,b)=>a.scope!=b.scope?a.scope.CompareTo(b.scope):StringComparer.Ordinal.Compare(a.target,b.target));
            s.purchases.Sort((a,b)=>{int c=StringComparer.Ordinal.Compare(a.town,b.town);return c!=0?c:StringComparer.Ordinal.Compare(a.item,b.item);});
            s.daily.Sort((a,b)=>StringComparer.Ordinal.Compare(a.town,b.town));return s;
        }
        public void Import(Save saved)
        {
            Validate(saved);Clear();if(saved==null||saved.schemaVersion==0)return;
            foreach(var p in saved.purchases)purchases.Add(Copy(p));foreach(var d in saved.daily)daily.Add(Copy(d));
            if(saved.haggleLocks!=null)foreach(var x in saved.haggleLocks)haggleLocks.Add(new HaggleLock{town=x.town,day=x.day});
            if(saved.bans!=null)foreach(var x in saved.bans)bans.Add(new TradeBan{town=x.town,until=x.until});
            if(saved.chores!=null)foreach(var x in saved.chores)chores.Add(CopyChore(x));
            if(saved.atonementReopen!=null)foreach(var x in saved.atonementReopen)reopen.Add(new Reopen{scope=x.scope,target=x.target,until=x.until});
        }
        public TradeActivity Clone(){var result=new TradeActivity();result.Import(Export());return result;}
        public static void Validate(Save s)
        {
            if(s==null)return;
            bool Empty<T>(List<T> l)=>l==null||l.Count==0;
            if(s.schemaVersion==0 && Empty(s.purchases) && Empty(s.daily) && Empty(s.haggleLocks) && Empty(s.bans) && Empty(s.chores) && Empty(s.atonementReopen))return;
            if(s.schemaVersion!=1||s.purchases==null||s.daily==null)throw new InvalidDataException("지원하지 않는 거래 기록입니다.");
            var keys=new HashSet<string>();
            foreach(var p in s.purchases)
                if(p==null||string.IsNullOrWhiteSpace(p.town)||string.IsNullOrWhiteSpace(p.item)||p.quantity<1||p.averagePaid<1||p.averagePaid>int.MaxValue||double.IsNaN(p.averagePaid)||double.IsInfinity(p.averagePaid)||float.IsNaN(p.lastBought)||float.IsInfinity(p.lastBought)||p.lastBought<0||!keys.Add(p.town+"\n"+p.item))throw new InvalidDataException("최근 매입 기록이 올바르지 않습니다.");
            keys.Clear();foreach(var d in s.daily)
                if(d==null||string.IsNullOrWhiteSpace(d.town)||d.day<1||d.revenue<0||d.revenue>int.MaxValue||(!d.awarded&&d.revenue>=50)||!keys.Add(d.town))throw new InvalidDataException("일일 판매 기록이 올바르지 않습니다.");
            bool Bad(float f)=>float.IsNaN(f)||float.IsInfinity(f)||f<0;
            keys.Clear();if(s.haggleLocks!=null)foreach(var x in s.haggleLocks)
                if(x==null||string.IsNullOrWhiteSpace(x.town)||x.day<0||!keys.Add(x.town))throw new InvalidDataException("흥정 기록이 올바르지 않습니다.");
            keys.Clear();if(s.bans!=null)foreach(var x in s.bans)
                if(x==null||string.IsNullOrWhiteSpace(x.town)||Bad(x.until)||!keys.Add(x.town))throw new InvalidDataException("거래 거절 기록이 올바르지 않습니다.");
            if(s.chores!=null)
            {
                if(s.chores.Count>1)throw new InvalidDataException("마을 일은 하나만 저장할 수 있습니다.");
                foreach(var x in s.chores)if(x==null||string.IsNullOrWhiteSpace(x.town)||x.kind<0||Bad(x.acceptedAt))throw new InvalidDataException("마을 일 기록이 올바르지 않습니다.");
            }
            keys.Clear();if(s.atonementReopen!=null)foreach(var x in s.atonementReopen)
                if(x==null||(x.scope!=0&&x.scope!=1)||string.IsNullOrWhiteSpace(x.target)||Bad(x.until)||!keys.Add(x.scope+":"+x.target))throw new InvalidDataException("속죄 의뢰 기록이 올바르지 않습니다.");
        }
    }
}
