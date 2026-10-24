using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class Q1Checks
    {
        [Serializable] public class Report { public string unityVersion; public int grainBuy, grainSell, grainProfit, finalMoney; public List<string> passed = new List<string>(); }
        static void Set(GameManager gm,string name,object value) => typeof(GameManager).GetProperty(name).SetValue(gm,value);
        public static void RunBatch()
        {
            var report = Run(); File.WriteAllText("Docs/Validation/Q1-Unity-2026-10-24.json",JsonUtility.ToJson(report,true));
            File.WriteAllText("Docs/Validation/Q1-R6Regression-2026-10-24.json", JsonUtility.ToJson(R6Checks.Run(), true));
            File.WriteAllText("Docs/Validation/Q1-SaveRegression-2026-10-24.json", JsonUtility.ToJson(SaveCompatibilityChecks.RunData(), true));
        }
        public static Report Run()
        {
            if(Application.isPlaying || GameManager.I != null) throw new InvalidOperationException("Use isolated editor only.");
            var r=new Report{unityVersion=Application.unityVersion};
            void Check(string name,bool ok){if(!ok)throw new Exception(name);r.passed.Add(name);}
            var db=Resources.Load<GameDatabase>("GameDatabase");
            QuestData Q(string id)=>db.quests.Find(q=>q.id==id);
            var ids=new[]{"q1_naru","q1_jangdan","q1_songak","q1_dakmok","q1_gaegyeong"};
            Check("17 quests, unique IDs",db.quests.Count==17&&db.quests.Select(q=>q.id).Distinct().Count()==17);
            var go=new GameObject("Q1 isolated tests");go.SetActive(false);var gm=go.AddComponent<GameManager>();
            typeof(GameManager).GetProperty("I").SetValue(null,gm);Set(gm,"Db",db);Set(gm,"Clock",new GameClock());
            PlayerState Fresh(){var p=new PlayerState{money=100,vehicle=db.startVehicle,origin=db.startOrigin};Set(gm,"Player",p);GameEvents.ClearAll();return p;}
            TownMarket Market(string id){var m=go.AddComponent<TownMarket>();m.town=db.towns.Find(t=>t.id==id);return m;}
            void Trade(TownMarket m,string action,ItemData item)=>typeof(TownMarket).GetMethod(action,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,new object[]{item});
            try
            {
                var player=Fresh();var qm=new QuestManager(db.quests);Set(gm,"Quests",qm);
                var jangdan=Market("goryeo_jangdan");var songak=Market("goryeo_songak");var dakmok=Market("dakmok");var capital=Market("gaegyeong");
                var grain=db.items.Find(i=>i.id=="grain");var paper=db.items.Find(i=>i.id=="paper");
                // Actual trade/board functions work without ever accepting a main quest.
                Trade(jangdan,"Buy",grain);Check("market buy without main",player.cargo.Count(grain)==1);Trade(songak,"Sell",grain);Check("market sell without main",player.cargo.Count(grain)==0);
                var contracts=new ContractManager(db,gm.Clock,player,null,21);contracts.InitBoards();var offer=contracts.Available("goryeo_songak").Find(c=>!c.Premium);
                Check("contract available without main",offer!=null&&contracts.Accept("goryeo_songak",offer));
                var delivery=contracts.Deliver(offer,offer.toTown);Check("contract completion without main",delivery.success&&player.credit==1);
                // Reset both player and market state before the 100-money scenario.
                var marketField=typeof(GameManager).GetField("markets",BindingFlags.Instance|BindingFlags.NonPublic);
                ((Dictionary<string,MarketState>)marketField.GetValue(gm)).Clear();player=Fresh();qm=new QuestManager(db.quests);Set(gm,"Quests",qm);
                foreach(var q in db.quests.Where(q=>q.retired))Check("retired hidden "+q.id,!qm.CanStart(q));
                Check("first offer is Naru",qm.CanStart(Q(ids[0]))&&ids.Skip(1).All(id=>!qm.CanStart(Q(id))));
                qm.Accept(Q(ids[0]));qm.TryCompleteAt("goryeo_jangdan_farmer",out _);Check("first delivery completes",qm.IsFinished(Q(ids[0]))&&player.credit==1);
                qm.Accept(Q(ids[1]));int cash=player.money;
                for(int i=0;i<3;i++)Trade(jangdan,"Buy",grain);r.grainBuy=cash-player.money;
                Check("3 grain affordable with real capacity/prices",player.cargo.Count(grain)==3&&player.money>=0);
                cash=player.money;for(int i=0;i<3;i++)Trade(songak,"Sell",grain);r.grainSell=player.money-cash-Q(ids[1]).rewardMoney;r.grainProfit=r.grainSell-r.grainBuy;
                Check("Jangdan to Songak actual sequential trade profitable",r.grainProfit>0&&qm.IsFinished(Q(ids[1])));
                qm.Accept(Q(ids[2]));contracts=new ContractManager(db,gm.Clock,player,null,21);contracts.InitBoards();offer=contracts.Available("goryeo_songak").Find(c=>!c.Premium);
                Check("tutorial board contract can be accepted",contracts.Accept("goryeo_songak",offer));contracts.Deliver(offer,offer.toTown);qm.TryCompleteAt("goryeo_songak_merchant",out _);
                Check("board tutorial does not double credit",qm.IsFinished(Q(ids[2]))&&player.credit==2);
                qm.Accept(Q(ids[3]));for(int i=0;i<2;i++)Trade(dakmok,"Buy",paper);qm.TryCompleteAt("dakmok_chief",out _);
                Check("paper affordable and quest completed",player.cargo.Count(paper)==2&&qm.IsFinished(Q(ids[3]))&&player.money>=0);
                qm.Accept(Q(ids[4]));GameEvents.RaiseMapEntered("Gaegyeong");qm.TryCompleteAt("gaekju",out _);for(int i=0;i<2;i++)Trade(capital,"Sell",paper);
                Check("100-money scenario all five complete",ids.All(id=>qm.IsFinished(Q(id)))&&player.money>=0&&player.credit==3);r.finalMoney=player.money;
                var full=new SaveData{version=2,money=100,credit=-7,finishedQuests=new List<string>{"tutorial_2","tutorial_5"}};
                player=Fresh();qm=new QuestManager(db.quests);int before=player.money;
                qm.Import(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(full)));qm.MigrateMainProgress();
                Check("real Unity JSON legacy full maps without payout",ids.All(id=>qm.IsFinished(Q(id)))&&player.money==before&&player.credit==0);
                var roundtrip=new SaveData();qm.Export(roundtrip);qm.Import(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(roundtrip)));
                Check("real Unity JSON mapped records persist without duplicates",ids.All(id=>qm.IsFinished(Q(id)))&&player.money==before);
                var save=new SaveData{version=2,finishedQuests=new List<string>{"tutorial_1"},quests=new List<QuestManager.Save>{new QuestManager.Save{id="tutorial_2",step=1,rewardCaptured=true,acceptedRewardMoney=45,acceptedRewardCredit=1}}};
                player=Fresh();qm=new QuestManager(db.quests);qm.Import(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)));
                player.cargo.Add(paper,2);qm.TryCompleteAt("gaekju",out _);
                Check("old active retired quest still finishes",qm.IsFinished(Q("tutorial_2"))&&player.credit==1);
                Check("legacy gap next is second and later record retained",qm.IsFinished(Q("q1_dakmok"))&&qm.CanStart(Q("q1_jangdan"))&&!qm.CanStart(Q("q1_gaegyeong")));
                player=Fresh();qm=new QuestManager(db.quests);
                qm.Import(new SaveData{q1MigrationVersion=1,finishedQuests=new List<string>{"q1_naru","q1_jangdan","q1_songak","q1_dakmok"}});
                qm.Accept(Q("tutorial_5"));player.vehicle=db.vehicles.Find(v=>v.id=="handcart");GameEvents.RaiseVehicleBought(player.vehicle);qm.TryCompleteAt("gaekju",out _);
                Check("new cart side does not auto-finish capital",!qm.IsFinished(Q("q1_gaegyeong")));
                var modern=new SaveData();qm.Export(modern);qm.Import(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(modern)));
                Check("new side cannot become legacy through real JSON reload",modern.q1MigrationVersion==1&&modern.legacyMainFinishedIds.Count==0&&!qm.IsFinished(Q("q1_gaegyeong")));
                // Literal pre-Q1 JSON has neither the migration marker nor the new history/eligibility fields.
                var literal=JsonUtility.FromJson<SaveData>("{\"version\":2,\"credit\":-7,\"quests\":[{\"id\":\"tutorial_2\",\"step\":1,\"rewardCaptured\":true,\"acceptedRewardMoney\":45,\"acceptedRewardCredit\":1}],\"finishedQuests\":[\"tutorial_1\"]}");
                qm.Import(literal);var migratedPending=new SaveData();qm.Export(migratedPending);qm.Import(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(migratedPending)));
                Check("literal old JSON pending eligibility survives new reload",qm.Active.Single().legacyMainMigrationEligible&&qm.Active.Single().RewardCredit==1&&literal.credit==-7);
                var mapObject=new GameObject("Q1 previsited capital test");mapObject.SetActive(false);var map=mapObject.AddComponent<MapRoot>();map.mapId="Gaegyeong";
                typeof(MapRoot).GetProperty("Current").SetValue(null,map);
                try
                {
                    player=Fresh();qm=new QuestManager(db.quests);qm.Import(new SaveData{q1MigrationVersion=1,finishedQuests=new List<string>{"q1_naru","q1_jangdan","q1_songak","q1_dakmok"}});qm.Accept(Q("q1_gaegyeong"));
                    Check("actual MapRoot already in capital advances VisitMap on accept",qm.Active.Single().step==1);
                }
                finally{typeof(MapRoot).GetProperty("Current").SetValue(null,null);UnityEngine.Object.DestroyImmediate(mapObject);}

            }
            finally{GameEvents.ClearAll();typeof(GameManager).GetProperty("I").SetValue(null,null);UnityEngine.Object.DestroyImmediate(go);}
            return r;
        }
    }
}
