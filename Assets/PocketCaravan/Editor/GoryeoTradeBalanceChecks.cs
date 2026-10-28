using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class GoryeoTradeBalanceChecks
    {
        [Serializable] public class Row
        {
            public string policy, from, to, vehicle, strategy;
            public int capital, cost, revenue, delivery, profit, outwardCount, returnCount;
            public float outwardWeight, returnWeight;
        }
        [Serializable] public class Report
        {
            public string scope = "Normal initial stock; sequential prices; selected outward/return goods; stop nonpositive marginal trades using known destination prices (informed counterfactual). No NPC, recovery, market day, origin bonus, human/online play. Delivery=one matched ordinary weight4 offer each leg; neutral roll and fast bonus15%, not a guarantee of board availability.";
            public List<string> passed = new List<string>();
            public List<Row> rows = new List<Row>();
        }
        static void Check(Report r, string name, bool ok) { if (!ok) throw new Exception(name); r.passed.Add(name); }

        [MenuItem("Pocket Caravan/Tests/고려 배달·교역 균형 검사")]
        public static void RunMenu()
        {
            var report = RunData(); System.IO.Directory.CreateDirectory("output/goryeo-economy");
            System.IO.File.WriteAllText("output/goryeo-economy/balance-data.json", JsonUtility.ToJson(report,true));
            Debug.Log("고려 경제 검사 완료. output/goryeo-economy/balance-data.json · 계산 시나리오 " + report.rows.Count);
        }

        public static Report RunData()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first");
            var r = new Report();
            var db = Resources.Load<GameDatabase>("GameDatabase"); var world = WorldIndex.Get();
            TownData Town(string id) => db.towns.Find(t => t != null && t.id == id);
            var rules = DeliveryReward.Rules(Town("gaegyeong"));
            Check(r, "Goryeo-only reward profile enabled", rules != null && rules.revision == 1 && DeliveryReward.Rules(Town("transit_ferry")) == null);
            // M4(2026-10-28) layoutD: 배달 보수 거리 = 새 맵 실제 보행 초 × 환산 비율(TownRoutes.json). 이웃 수가 아니다.
            Check(r, "capital-Dakmok layoutD distance 2", DeliveryReward.RouteSteps(world,"gaegyeong","dakmok")==2);
            Check(r, "capital-Jangdan layoutD distance 4 (longer than Dakmok)", DeliveryReward.RouteSteps(world,"gaegyeong","goryeo_jangdan")==4);
            // 표에 없는 마을(다른 지역 포함)은 예전 방식으로 계산: 등록 안 된 마을은 경로 없음(-1)
            Check(r, "unknown destination cannot receive route quote", DeliveryReward.RouteSteps(world,"gaegyeong","unknown")<1);
            for (int distance = 1; distance <= 5; distance++)
                for (int weight = 2; weight <= 6; weight++)
                    Check(r, "legacy quote exact d"+distance+" w"+weight,
                        DeliveryReward.Quote(null,distance,0,weight,false,1.1f)==Mathf.RoundToInt((14+16*distance+3*weight)*1.1f));
            Check(r, "longer route pays more for identical load", DeliveryReward.Quote(rules,1,4,4,false)>DeliveryReward.Quote(rules,1,1,4,false));
            Check(r, "ordinary delivery positive without capital", DeliveryReward.Quote(rules,1,1,2,false)>0);
            Check(r, "new premium shipments use meaningful capacity", rules.premiumMinWeight >= 8 && rules.premiumMaxWeight <= 30);
            var p = new PlayerState { vehicle=db.vehicles.Find(v=>v.id=="walk"), money=0, credit=-2 };
            var cm = new ContractManager(db,new GameClock(),p,world,42); cm.InitBoards();
            var offer=cm.Available("naru").Find(c=>!c.Premium);
            Check(r,"zero money negative credit can accept an ordinary offer",offer!=null && cm.CanAccept(offer)==null);
            int initial=offer.reward; cm.Accept("naru",offer); int active= p.contracts.Count;
            cm.InitBoards();
            Check(r,"accepted contract untouched during profile refresh",p.contracts.Count==active && p.contracts[0].reward==initial);
            var result=cm.Deliver(offer,offer.toTown); int paid=p.money;
            Check(r,"delivery pays and increases credit",result.success && paid>0 && p.credit==-1);
            Check(r,"repeat delivery cannot pay twice",!cm.Deliver(offer,offer.toTown).success && p.money==paid);
            var oldOffer = new Contract { id=99990,fromTown="naru",toTown="gaegyeong",weight=4,reward=999,hours=26,minCredit=0 };
            cm.BoardOf("naru").list.Clear(); cm.BoardOf("naru").list.Add(oldOffer);
            var oldAccepted = new Contract { id=99991,fromTown="naru",toTown="gaegyeong",weight=4,reward=97,hours=26,deadline=26,finalDeadline=39,condition=1 };
            p.contracts.Add(oldAccepted); cm.InitBoards();
            Check(r,"old unaccepted quote updates in place",oldOffer.id==99990 && oldOffer.weight==4 && oldOffer.hours==26 && oldOffer.reward<999 && oldOffer.rewardRuleVersion==1);
            int refreshed=oldOffer.reward; cm.InitBoards();
            Check(r,"reload cannot reroll quote",oldOffer.reward==refreshed);
            Check(r,"old accepted price/deadline preserved",oldAccepted.reward==97 && oldAccepted.deadline==26 && oldAccepted.rewardRuleVersion==0);
            var saved=JsonUtility.FromJson<ContractManager.Board>(JsonUtility.ToJson(cm.BoardOf("naru")));
            cm.boards=new List<ContractManager.Board>{saved}; cm.InitBoards();
            Check(r,"new quote survives board JSON reload",cm.BoardOf("naru").list.Find(c=>c.id==99990).reward==refreshed);
            Check(r,"old accepted payout retains original fast bonus",cm.Deliver(oldAccepted,"gaegyeong").paid==Mathf.RoundToInt(97*1.15f));
            for(int seed=0;seed<30;seed++)
            {
                var seeded=new ContractManager(db,new GameClock(),new PlayerState(),world,seed); seeded.InitBoards();
                foreach(var town in db.towns)
                {
                    if(DeliveryReward.Rules(town)==null)continue;
                    var offers=seeded.Available(town.id);
                    Check(r,"seed"+seed+" "+town.id+" basic recovery offer",offers.Exists(c=>!c.Premium && c.weight<=10 && c.reward>0));
                    Check(r,"seed"+seed+" "+town.id+" premium capacity/quote stamp",offers.TrueForAll(c=>c.rewardRuleVersion==1 && (!c.Premium || (c.weight>=8 && c.weight<=12))));
                }
            }
            foreach (string policy in new[]{"legacy","goryeo-v1"})
                foreach (string vehicle in new[]{"walk","handcart"})
                    foreach(int capital in new[]{0,30,100})
                        foreach(string strategy in new[]{"delivery","trade","mixed"})
                        {
                            r.rows.Add(Simulate(db,world,policy,vehicle,capital,strategy,"goryeo_jangdan","gaegyeong","grain","paper"));
                            r.rows.Add(Simulate(db,world,policy,vehicle,capital,strategy,"goryeo_jangdan","dakmok","firewood","paper"));
                            r.rows.Add(Simulate(db,world,policy,vehicle,capital,strategy,"goryeo_songak","gaegyeong","ginseng","salt"));
                        }
            foreach(var row in r.rows) Check(r,"legal load/capital "+row.policy+" "+row.from+" "+row.vehicle+" "+row.capital+" "+row.strategy,row.outwardWeight<=(row.vehicle=="walk"?10:30) && row.returnWeight<=(row.vehicle=="walk"?10:30) && row.cost<=row.capital+row.revenue+row.delivery && row.profit==row.revenue+row.delivery-row.cost);
            Notify.Clear();
            return r;
        }

        public static Report RunPlay()
        {
            var gm=GameManager.I;
            if(!EditorApplication.isPlaying || gm==null || MapRoot.Current==null || UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex==0)
                throw new InvalidOperationException("Initialized non-start Play session required");
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            if((bool)typeof(GameManager).GetField("autosave",flags).GetValue(gm)) throw new InvalidOperationException("Autosave must be disabled");
            var r=new Report(); var p=gm.Player; var db=gm.Db;
            int money=p.money,credit=p.credit; var vehicle=p.vehicle;var origin=p.origin;
            var cargo=new List<Tuple<ItemData,int,float>>(); foreach(var pair in p.cargo.Items)cargo.Add(Tuple.Create(pair.Key,pair.Value,p.cargo.AverageCost(pair.Key)));
            var questSave=new SaveData();gm.Quests.Export(questSave);
            var markets=new List<MarketState.Save>(); foreach(var town in db.towns)markets.Add(gm.GetMarket(town).Export());
            var contracts=new List<Contract>(p.contracts);
            string boardJson=JsonUtility.ToJson(new SaveData{boards=gm.Contracts.boards,nextContractId=gm.Contracts.nextId});
            var temps=new List<GameObject>(); bool blocked=UIState.Blocking;
            QuestData Quest(string id)=>db.quests.Find(q=>q.id==id);
            SaveData Intro()=>new SaveData{finishedQuests=new List<string>{"tutorial_1","tutorial_2","tutorial_5","goryeo_jangdan_trade"}};
            try
            {
                UIState.Blocking=true;p.money=200;p.credit=3;p.origin=null;p.cargo.Clear();p.contracts.Clear();p.vehicle=db.vehicles.Find(v=>v.id=="handcart");
                var basic=Quest("dakmok_firewood");var supply=Quest("goryeo_cart_supply");var returning=Quest("goryeo_return_trade");
                gm.Quests.Import(Intro());
                Check(r,"both introduction alternatives initially available",gm.Quests.CanStart(basic)&&gm.Quests.CanStart(supply));
                Check(r,"return trade is locked before either freight completion",!gm.Quests.CanStart(returning));
                gm.Quests.Accept(basic);
                Check(r,"new freight acceptance captures current equal reward",gm.Quests.Tracked.RewardMoney==20 && gm.Quests.Tracked.RewardMoney==supply.rewardMoney);
                var newSave=new SaveData();gm.Quests.Export(newSave);
                gm.Quests.Import(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(newSave)));
                Check(r,"new accepted reward stays twenty after reload",gm.Quests.Tracked.RewardMoney==20);
                var oldRewardSave=Intro();oldRewardSave.quests.Add(new QuestManager.Save{id=basic.id,step=0,amount=0});gm.Quests.Import(oldRewardSave);
                Check(r,"legacy freight promise migrates to sixty",gm.Quests.Tracked.RewardMoney==60);
                var woodLegacy=gm.ItemById("firewood");p.cargo.Add(woodLegacy,4,2);int legacyBefore=p.money;string legacyFail;gm.Quests.TryCompleteAt("dakmok_chief",out legacyFail);
                Check(r,"old freight actually pays its original promise",legacyFail==null && p.money==legacyBefore+60);
                gm.Quests.Import(Intro());gm.Quests.Accept(supply);
                Check(r,"accepting one freight hides duplicate new offer",!gm.Quests.CanStart(basic));
                var save=new SaveData();gm.Quests.Export(save);gm.Quests.Import(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)));
                Check(r,"alternative choice survives quest JSON reload",!gm.Quests.CanStart(basic) && gm.Quests.Tracked.quest==supply);
                var legacy=Intro();legacy.quests.Add(new QuestManager.Save{id=basic.id,step=0,amount=2});legacy.quests.Add(new QuestManager.Save{id=supply.id,step=1,amount=3});gm.Quests.Import(legacy);
                var exported=new SaveData();gm.Quests.Export(exported);
                Check(r,"previously accepted alternatives both preserved",exported.quests.Count==2 && exported.quests.Exists(q=>q.id==basic.id&&q.step==0&&q.amount==2) && exported.quests.Exists(q=>q.id==supply.id&&q.step==1&&q.amount==3));
                var viaBasic=Intro();viaBasic.finishedQuests.Add(basic.id);gm.Quests.Import(viaBasic);
                Check(r,"old freight finish unlocks same return trade",gm.Quests.CanStart(returning) && !gm.Quests.CanStart(supply));
                var pending=Intro();pending.quests.Add(new QuestManager.Save{id=supply.id,step=1,amount=0});gm.Quests.Import(pending);
                var wood=gm.ItemById("firewood");p.cargo.Add(wood,4,2);string why;gm.Quests.TryCompleteAt("dakmok_chief",out why);
                Check(r,"supply consumes freight and unlocks return trade",why==null && p.cargo.Count(wood)==0 && gm.Quests.IsFinished(supply) && gm.Quests.CanStart(returning));
                Check(r,"finished freight blocks duplicate introduction",!gm.Quests.CanStart(basic));
                foreach(var town in db.towns)gm.GetMarket(town).Import(new MarketState.Save{town=town.id,level=town.startLevel},gm.ItemById);
                TownMarket Market(string id){var go=new GameObject("Isolated return trade "+id);temps.Add(go);var m=go.AddComponent<TownMarket>();m.town=db.towns.Find(t=>t.id==id);return m;}
                var dakmok=Market("dakmok");var jangdan=Market("goryeo_jangdan");var capital=Market("gaegyeong");
                var buy=typeof(TownMarket).GetMethod("Buy",flags);var sell=typeof(TownMarket).GetMethod("Sell",flags);var paper=gm.ItemById("paper");
                gm.Quests.Accept(returning);buy.Invoke(capital,new object[]{paper});
                Check(r,"return purchase only counts at Dakmok",gm.Quests.Tracked.quest==returning && gm.Quests.Tracked.step==0 && gm.Quests.Tracked.amount==0);
                p.cargo.Clear();p.money=200;
                buy.Invoke(dakmok,new object[]{paper});buy.Invoke(dakmok,new object[]{paper});
                Check(r,"return trade advances after two real purchases",gm.Quests.Tracked.step==1 && p.cargo.Count(paper)==2);
                sell.Invoke(jangdan,new object[]{paper});sell.Invoke(jangdan,new object[]{paper});
                Check(r,"actual return sales finish quest and yield net profit",gm.Quests.IsFinished(returning) && p.money>200 && p.cargo.Count(paper)==0);
                int after=p.money;gm.Quests.Accept(returning);gm.Quests.Tick();
                Check(r,"return trade reward is one-time",p.money==after && !gm.Quests.CanStart(returning));
                p.money=0;p.credit=-2;p.cargo.Clear();p.vehicle=db.vehicles.Find(v=>v.id=="walk");
                var offer=gm.Contracts.Available("naru").Find(c=>!c.Premium);
                Check(r,"real board allows recovery without capital",offer!=null && gm.Contracts.Accept("naru",offer));
                Check(r,"accepted delivery reserves actual capacity",p.ContractWeight==offer.weight && p.TotalWeight<=p.Capacity);
                var result=gm.Contracts.Deliver(offer,offer.toTown); int recovery=p.money;
                Check(r,"recovery pays usable currency and frees capacity",result.success && recovery>0 && p.ContractWeight==0);
                gm.Contracts.Deliver(offer,offer.toTown);
                Check(r,"real recovery payout cannot be duplicated",p.money==recovery);
                return r;
            }
            finally
            {
                p.money=money;p.credit=credit;p.vehicle=vehicle;p.origin=origin;p.cargo.Clear();foreach(var row in cargo)p.cargo.Add(row.Item1,row.Item2,row.Item3);
                p.contracts.Clear();p.contracts.AddRange(contracts);gm.Quests.Import(questSave);
                var savedBoards=JsonUtility.FromJson<SaveData>(boardJson);gm.Contracts.boards=savedBoards.boards;gm.Contracts.nextId=savedBoards.nextContractId;
                foreach(var market in markets){var town=db.towns.Find(t=>t.id==market.town);gm.GetMarket(town).Import(market,gm.ItemById);}
                foreach(var go in temps)UnityEngine.Object.Destroy(go);UIState.Blocking=blocked;Notify.Clear();
            }
        }

        static Row Simulate(GameDatabase db, WorldIndex world, string policy,string vehicle,int capital,string strategy,string fromId,string toId,string outId,string returnId)
        {
            var from=db.towns.Find(t=>t.id==fromId); var to=db.towns.Find(t=>t.id==toId);
            var a=new MarketState(from,db); var b=new MarketState(to,db);
            var row=new Row{policy=policy,vehicle=vehicle,capital=capital,strategy=strategy,from=fromId,to=toId};
            float capacity=db.vehicles.Find(v=>v.id==vehicle).capacity;
            bool hasDelivery=strategy!="trade", hasTrade=strategy!="delivery";
            var rules=policy=="legacy"?null:DeliveryReward.Rules(from);
            int fee=hasDelivery?Mathf.RoundToInt(DeliveryReward.Quote(rules,ContractManager.Distance(from,to),DeliveryReward.RouteSteps(world,fromId,toId),4,false)*1.15f):0;
            int money=capital;
            int Trade(MarketState source,MarketState destination,string itemId,out float weight)
            {
                var item=db.items.Find(i=>i.id==itemId); weight=0; int count=0;
                int spent=0,earned=0;
                // Buy all cargo before selling. Quotes are stepped in both markets to evaluate the full batch.
                while(hasTrade && source.CanBuy(item) && weight+item.weight+(hasDelivery?4:0)<=capacity+0.001f)
                {
                    int buy=source.BuyPrice(item),sell=destination.SellPrice(item);
                    if(spent+buy>money || sell<=buy)break;
                    spent+=buy; earned+=sell; weight+=item.weight;count++;
                    source.RecordBuy(item,buy); destination.RecordSell(item,sell);
                }
                row.cost+=spent; row.revenue+=earned; money+=earned-spent;
                return count;
            }
            row.outwardCount=Trade(a,b,outId,out float w1); row.outwardWeight=w1+(hasDelivery?4:0); row.delivery+=fee; money+=fee;
            row.returnCount=Trade(b,a,returnId,out float w2); row.returnWeight=w2+(hasDelivery?4:0); row.delivery+=fee;
            row.profit=row.revenue+row.delivery-row.cost;
            return row;
        }
    }
}
