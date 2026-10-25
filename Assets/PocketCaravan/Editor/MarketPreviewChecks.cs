using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class MarketPreviewChecks
    {
        [Serializable] public class Report { public List<string> passed=new List<string>(); }
        static void Check(Report r,string name,bool ok){if(!ok)throw new Exception(name);r.passed.Add(name);}
        public static Report RunData()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            var r=new Report();var db=Resources.Load<GameDatabase>("GameDatabase");
            var town=db.towns.Find(t=>t.id=="dakmok");var paper=db.items.Find(i=>i.id=="paper");
            var m=new MarketState(town,db);var p=new PlayerState{money=500,vehicle=db.vehicles.Find(v=>v.id=="handcart")};
            m.Import(new MarketState.Save{town=town.id,level=1,volume=599},id=>db.items.Find(i=>i.id==id));
            string before=JsonUtility.ToJson(m.Export());int events=0;Action<TownData,int> raised=(t,l)=>events++;
            GameEvents.TownLeveled+=raised;
            try
            {
                var buy=m.Preview(paper,p,5,false);
                Check(r,"five-unit local quote computed",buy.count==5 && buy.total>0);
                Check(r,"preview leaves live stock/volume/level untouched",JsonUtility.ToJson(m.Export())==before && m.Level==1);
                Check(r,"preview crossing growth threshold emits no event",events==0);
                Check(r,"preview leaves wallet/cargo untouched",p.money==500 && p.cargo.Count(paper)==0);
                p.money=0;Check(r,"zero capital produces no purchase",m.Preview(paper,p,5,false).count==0);
                p.money=500;p.contracts.Add(new Contract{weight=29});Check(r,"contract load reserves real capacity",m.Preview(paper,p,5,false).count==0);
                p.contracts.Clear();p.cargo.Add(paper,3,29f/3);
                var sell=m.Preview(paper,p,5,true);
                Check(r,"sell quote caps at owned cargo",sell.count==3);
                Check(r,"sale preview preserves actual total cost basis",Mathf.Abs(sell.profit-(sell.total-29))<0.001f);
                p.money++;Check(r,"quote invalidates changed wallet",!sell.Matches(m.Preview(paper,p,3,true)));
                p.money--;p.cargo.Clear();p.cargo.Add(paper,3,11);
                Check(r,"quote invalidates changed average cost",!sell.Matches(m.Preview(paper,p,3,true)));
                p.money=int.MaxValue;Check(r,"sell quote prevents wallet overflow",m.Preview(paper,p,3,true).count==0);
                p.money=0;p.cargo.Clear();p.cargo.Add(paper,1000,1);
                Check(r,"large legacy cargo sale stays bounded",m.Preview(paper,p,int.MaxValue,true).count==MarketState.MaxBatch);
                Check(r,"nonpositive request rejected",m.Preview(paper,p,0,true).count==0);
                var questItem=db.items.Find(i=>i.questItem);Check(r,"quest cargo cannot be quoted as merchandise",m.Preview(questItem,p,100,true).count==0);
                Check(r,"invalid item cannot be quoted",m.Preview(null,p,1,false).count==0);
                return r;
            }
            finally{GameEvents.TownLeveled-=raised;Notify.Clear();}
        }

        public static Report RunPlay()
        {
            var gm=GameManager.I;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            if(!EditorApplication.isPlaying || gm==null || MapRoot.Current==null || (bool)typeof(GameManager).GetField("autosave",flags).GetValue(gm))
                throw new InvalidOperationException("Initialized non-start Play with autosave disabled required");
            var r=new Report();var p=gm.Player;var town=gm.Db.towns.Find(t=>t.id=="dakmok");var market=gm.GetMarket(town);var paper=gm.ItemById("paper");
            int money=p.money,credit=p.credit;var vehicle=p.vehicle;var origin=p.origin;bool blocked=UIState.Blocking;
            var inventory=new List<Tuple<ItemData,int,float>>();foreach(var pair in p.cargo.Items)inventory.Add(Tuple.Create(pair.Key,pair.Value,p.cargo.AverageCost(pair.Key)));
            var saved=market.Export();var returnTown=gm.Db.towns.Find(t=>t.id=="goryeo_jangdan");var returnMarket=gm.GetMarket(returnTown);var savedReturn=returnMarket.Export();var quests=new SaveData();gm.Quests.Export(quests);var contracts=new List<Contract>(p.contracts);
            var go=new GameObject("Isolated batch market");var shop=go.AddComponent<TownMarket>();shop.town=town;
            int bought=0,sold=0,levels=0;Action<ItemData,TownData,int> onBuy=(i,t,v)=>{if(i==paper && t==town)bought++;};
            Action<ItemData,TownData,int,int> onSell=(i,t,v,g)=>{if(i==paper && t==town)sold++;};Action<TownData,int> onLevel=(t,l)=>{if(t==town)levels++;};
            var execute=typeof(TownMarket).GetMethod("TradeBatch",flags);
            int Trade(MarketEstimate q)=>(int)execute.Invoke(shop,new object[]{q});
            void ResetMarket()=>market.Import(new MarketState.Save{town=town.id,level=1,volume=0},gm.ItemById);
            try
            {
                UIState.Blocking=true;p.money=500;p.credit=0;p.origin=null;p.vehicle=gm.Db.vehicles.Find(v=>v.id=="handcart");p.cargo.Clear();p.contracts.Clear();
                gm.Quests.Import(new SaveData{finishedQuests=new List<string>{"tutorial_1"}});ResetMarket();
                var intro=gm.Db.quests.Find(q=>q.id=="tutorial_2");gm.Quests.Accept(intro);
                GameEvents.ItemBought+=onBuy;GameEvents.ItemSold+=onSell;GameEvents.TownLeveled+=onLevel;
                var quote=market.Preview(paper,p,5,false);int start=p.money;
                Check(r,"batch quote does not advance active quest",gm.Quests.Tracked.step==0 && gm.Quests.Tracked.amount==0 && bought==0);
                Check(r,"actual five-unit purchase fills quoted count",Trade(quote)==5 && p.cargo.Count(paper)==5);
                Check(r,"actual total charges stepped prices exactly",p.money==start-quote.total && Mathf.Abs(p.cargo.TotalCost(paper)-quote.total)<0.001f);
                Check(r,"per-unit purchase events and tutorial preserved",bought==5 && gm.Quests.Tracked.step==1);
                var sellQuote=market.Preview(paper,p,5,true);start=p.money;
                Check(r,"actual sale fills quoted count and revenue",Trade(sellQuote)==5 && p.money==start+sellQuote.total && p.cargo.Count(paper)==0 && sold==5);
                int same=p.money;Check(r,"same sold-cargo quote cannot pay twice",Trade(sellQuote)==0 && p.money==same);
                ResetMarket();p.money=500;p.cargo.Clear();quote=market.Preview(paper,p,5,false);
                market.RecordNpcSell(paper,market.NpcSellPrice(paper));var state=JsonUtility.ToJson(market.Export());same=p.money;
                Check(r,"stale price quote rejected before any transaction",Trade(quote)==0 && p.money==same && p.cargo.Count(paper)==0 && JsonUtility.ToJson(market.Export())==state);
                ResetMarket();quote=market.Preview(paper,p,5,false);p.money--;
                Check(r,"changed funds reject stale displayed remainder",Trade(quote)==0 && p.cargo.Count(paper)==0);
                p.money=500;p.contracts.Add(new Contract{weight=29});quote=market.Preview(paper,p,5,false);
                Check(r,"batch cannot displace delivery cargo",quote.count==0 && Trade(quote)==0 && p.ContractWeight==29);
                p.contracts.Clear();p.cargo.Clear();p.money=500;
                market.Import(new MarketState.Save{town=town.id,level=1,volume=599},gm.ItemById);levels=0;
                quote=market.Preview(paper,p,5,false);Check(r,"growth preview has no live side effects",levels==0 && market.Level==1);
                same=p.money;Trade(quote);
                Check(r,"actual growth still matches preview total",levels>0 && market.Level>1 && p.money==same-quote.total);
                p.cargo.Clear();p.cargo.Add(paper,3,29f/3);p.money=0;ResetMarket();quote=market.Preview(paper,p,3,true);
                float basis=p.cargo.TotalCost(paper);Trade(quote);
                Check(r,"batch sale money and cost-basis profit agree",Mathf.Abs(quote.profit-(p.money-basis))<0.001f);
                p.cargo.Add(paper,1,2);p.money=int.MaxValue;quote=market.Preview(paper,p,1,true);
                Check(r,"max wallet cannot overflow via batch sale",Trade(quote)==0 && p.money==int.MaxValue && p.cargo.Count(paper)==1);
                p.money=500;p.cargo.Clear();ResetMarket();quote=market.Preview(paper,p,5,false);
                bool disruptOnce=true;Action<ItemData,TownData,int> disrupt=(i,t,v)=>{if(i==paper&&t==town&&disruptOnce){disruptOnce=false;for(int j=0;j<40;j++)market.RecordNpcSell(paper,market.NpcSellPrice(paper));}};
                GameEvents.ItemBought+=disrupt;
                try{Check(r,"mid-batch price change stops before extra charge",Trade(quote)==1 && p.cargo.Count(paper)==1 && p.money==500-quote.PriceAt(0));}
                finally{GameEvents.ItemBought-=disrupt;}
                var returning=gm.Db.quests.Find(q=>q.id=="goryeo_return_trade");
                gm.Quests.Import(new SaveData{quests=new List<QuestManager.Save>{new QuestManager.Save{id=returning.id,step=1}}});
                p.cargo.Clear();p.cargo.Add(paper,3,29f/3);p.money=0;returnMarket.Import(new MarketState.Save{town=returnTown.id,level=1},gm.ItemById);shop.town=returnTown;
                quote=returnMarket.Preview(paper,p,3,true);int completed=Trade(quote);
                Check(r,"quest reward during batch remains separate from sale total",completed==3 && gm.Quests.IsFinished(returning) && p.money==quote.total+returning.rewardMoney);
                shop.town=town;
                // Purchase budget and shop eligibility without changing common vehicle data/prices.
                var cart=gm.Db.vehicles.Find(v=>v.id=="handcart");p.cargo.Clear();p.vehicle=gm.Db.vehicles.Find(v=>v.id=="walk");p.money=cart.price;
                typeof(TownMarket).GetMethod("BuyVehicle",flags).Invoke(shop,new object[]{cart});
                Check(r,"unlisted vehicle cannot be purchased",p.vehicle.id=="walk" && p.money==cart.price);
                return r;
            }
            finally
            {
                GameEvents.ItemBought-=onBuy;GameEvents.ItemSold-=onSell;GameEvents.TownLeveled-=onLevel;
                p.money=money;p.credit=credit;p.vehicle=vehicle;p.origin=origin;p.cargo.Clear();foreach(var row in inventory)p.cargo.Add(row.Item1,row.Item2,row.Item3);
                p.contracts.Clear();p.contracts.AddRange(contracts);market.Import(saved,gm.ItemById);returnMarket.Import(savedReturn,gm.ItemById);gm.Quests.Import(quests);
                UIState.Blocking=blocked;UnityEngine.Object.Destroy(go);Notify.Clear();
            }
        }
    }
}
