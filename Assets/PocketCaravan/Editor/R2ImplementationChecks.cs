using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PocketCaravan.EditorTools
{
    public static class R2ImplementationChecks
    {
        [Serializable] class Report { public string unity;public List<string> passed=new List<string>();public int buy,sell; }
        public static void RunBatch()
        {
            if(Application.isPlaying||GameManager.I!=null)throw new InvalidOperationException("Use isolated Edit mode");
            var r=new Report{unity=Application.unityVersion};
            void Check(string message,bool ok){if(!ok)throw new Exception(message);r.passed.Add(message);}
            var source=Resources.Load<GameDatabase>("GameDatabase");var db=UnityEngine.Object.Instantiate(source);db.maxTownLevel=1;
            var go=new GameObject("R2 isolated runtime checks");go.SetActive(false);var gm=go.AddComponent<GameManager>();
            typeof(GameManager).GetProperty("I").SetValue(null,gm);
            var p=new PlayerState{money=10000,origin=AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Origins/Origin_Venice.asset"),vehicle=source.startVehicle};
            var clock=new GameClock();void Set(string name,object value)=>typeof(GameManager).GetProperty(name).SetValue(gm,value);
            Set("Player",p);Set("Clock",clock);Set("Db",db);
            try
            {
                var ferry=db.towns.Find(t=>t.id=="transit_ferry");var naru=db.towns.Find(t=>t.id=="naru");var ginseng=db.items.Find(i=>i.id=="ginseng");var najeon=db.items.Find(i=>i.id=="najeon");
                p.reputation.Initialize("goryeo");Check("home friendly starts at 3000",ReputationState.Score(p.reputation.Get(ReputationScope.Civilization,"goryeo"))==3000);
                p.reputation.Set(ReputationScope.Town,ferry.id,ReputationStanding.Respected,0);
                var m=new MarketState(ferry,db);m.Import(new MarketState.Save{town=ferry.id,level=1,items=new List<string>{ginseng.id},stocks=new List<float>{3}},id=>ginseng);
                r.buy=m.BuyPrice(ginseng);ReputationPricing.Bought(p,ferry,ginseng,r.buy,clock.TotalHours);m.RecordBuy(ginseng,r.buy);
                r.sell=m.SellPrice(ginseng);Check("144 to 145 counterexample is capped at 143",r.buy==144&&r.sell==143);
                ReputationPricing.Sold(p,ferry,ginseng,r.sell,clock.TotalHours,clock.day);m.RecordSell(ginseng,r.sell);
                Check("same-town return consumes protected count",!p.tradeActivity.Restricted(ferry.id,ginseng.id,clock.TotalHours));
                Check("restricted sale gives no daily contribution",p.tradeActivity.Export().daily.Count==0);
                var activity=p.tradeActivity;activity.Bought(ferry.id,ginseng.id,8,10);activity.Bought(ferry.id,ginseng.id,8,100);
                int first=activity.LimitSale(ferry.id,ginseng.id,8,1000);activity.Sold(ferry.id,ginseng.id,8);int second=activity.LimitSale(ferry.id,ginseng.id,8,1000);
                Check("partial returns keep weighted mean basis",first==54&&second==54);
                var saved=new SaveData{day=1,minute=480};p.reputation.ExportTo(saved);saved.tradeActivity=activity.Export();
                string json=JsonUtility.ToJson(saved);var restored=JsonUtility.FromJson<SaveData>(json);var copy=new TradeActivity();copy.Import(restored.tradeActivity);
                Check("Unity purchase record roundtrip",copy.LimitSale(ferry.id,ginseng.id,8,1000)==54);
                Check("exactly 12 hours expires",!copy.Restricted(ferry.id,ginseng.id,20));
                Check("other town unaffected",copy.LimitSale(naru.id,ginseng.id,8,200)==200);
                copy.Import(null);Check("old save has empty activity",copy.Export().purchases.Count==0);
                var standings=new[]{ReputationStanding.Hostile,ReputationStanding.Wary,ReputationStanding.Neutral,ReputationStanding.Friendly,ReputationStanding.Trusted,ReputationStanding.Respected};
                int[] minima={-6000,-3000,0,3000,9000,21000};
                for(int i=0;i<6;i++){p.reputation.Set(ReputationScope.Civilization,"goryeo",standings[i],0);Check("standing minimum "+i,ReputationState.Score(p.reputation.Get(ReputationScope.Civilization,"goryeo"))==minima[i]);}
                p.reputation.Set(ReputationScope.Civilization,"goryeo",ReputationStanding.Friendly,5990);p.reputation.Add(ReputationScope.Civilization,"goryeo",25);
                Check("threshold crosses to Trusted",p.reputation.Get(ReputationScope.Civilization,"goryeo").standing==ReputationStanding.Trusted&&p.reputation.Get(ReputationScope.Civilization,"goryeo").progress==15);
                p.reputation.Add(ReputationScope.Civilization,"goryeo",int.MinValue);Check("negative clamp",ReputationState.Score(p.reputation.Get(ReputationScope.Civilization,"goryeo"))==-6000);
                p.reputation.Add(ReputationScope.Civilization,"goryeo",int.MaxValue);Check("positive clamp",ReputationState.Score(p.reputation.Get(ReputationScope.Civilization,"goryeo"))==42000);
                p.reputation.Initialize("goryeo");Check("home Friendly cannot buy najeon",ReputationPricing.BuyDenial(p,naru,najeon)!=null);
                p.reputation.Set(ReputationScope.Civilization,"goryeo",ReputationStanding.Trusted,0);Check("Trusted unlocks najeon",ReputationPricing.BuyDenial(p,naru,najeon)==null);
                Check("all other goods unrestricted",db.items.Where(i=>!i.questItem&&i.id!="najeon").All(i=>ReputationPricing.BuyDenial(p,naru,i)==null));
                var capital=db.towns.Find(t=>t.id=="gaegyeong");var actualMarket=go.AddComponent<TownMarket>();actualMarket.town=capital;
                p.reputation.Initialize("goryeo");int wallet=p.money;
                typeof(TownMarket).GetMethod("Buy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(actualMarket,new object[]{najeon});
                Check("actual market blocks Friendly rare purchase",p.money==wallet&&p.cargo.Count(najeon)==0);
                p.reputation.Set(ReputationScope.Civilization,"goryeo",ReputationStanding.Trusted,0);
                typeof(TownMarket).GetMethod("Buy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(actualMarket,new object[]{najeon});
                Check("actual Trusted rare purchase",p.cargo.Count(najeon)==1&&p.money<wallet);
                p.reputation.Set(ReputationScope.Civilization,"goryeo",ReputationStanding.Hostile,0);
                typeof(TownMarket).GetMethod("Sell",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(actualMarket,new object[]{najeon});
                Check("Hostile can sell rare cargo",p.cargo.Count(najeon)==0);
                foreach(var town in db.towns)foreach(var facility in town.facilities)
                {
                    p.reputation.Set(ReputationPricing.Scope(town),ReputationPricing.Target(town),ReputationStanding.Hostile,0);
                    string a=FacilityAccess.Denial(town,facility,p);
                    p.reputation.Set(ReputationPricing.Scope(town),ReputationPricing.Target(town),ReputationStanding.Respected,0);
                    Check("basic facility ignores reputation "+facility.id,a==FacilityAccess.Denial(town,facility,p));
                }
                activity.Clear();Check("below daily threshold",activity.EligibleSale(naru.id,1,49,false,false)==ReputationSize.None);
                Check("first threshold pays Small",activity.EligibleSale(naru.id,1,1,false,false)==ReputationSize.Small);
                Check("same day cannot replay",activity.EligibleSale(naru.id,1,100,true,false)==ReputationSize.None);
                Check("other town independent",activity.EligibleSale(ferry.id,1,50,true,false)==ReputationSize.Medium);
                Check("next day resets",activity.EligibleSale(naru.id,2,50,false,false)==ReputationSize.Small);
                activity.EligibleSale(ferry.id,2,49,true,false);var dailyCopy=new TradeActivity();dailyCopy.Import(activity.Export());
                Check("daily amount survives reload",dailyCopy.EligibleSale(ferry.id,2,1,false,false)==ReputationSize.Medium);
                Check("VerySmall is 10",ReputationAwards.PointsFor(ReputationSize.VerySmall)==10);
                foreach(var e in p.reputation.Export().entries)Check("progress text "+e.targetId,!string.IsNullOrEmpty(ReputationState.ProgressText(e)));
                // Live batch preview must consume a cloned protection record and keep the original unchanged.
                activity.Clear();activity.Bought(ferry.id,ginseng.id,clock.TotalHours,1);p.cargo.Add(ginseng,1,1);
                string before=JsonUtility.ToJson(activity.Export());var estimate=m.Preview(ginseng,p,1,true);
                Check("zero-coin protected sale is a valid batch",estimate.count==1&&estimate.total==0);
                Check("preview is read-only",before==JsonUtility.ToJson(activity.Export()));
                File.WriteAllText("Docs/Validation/R2-Implementation-2026-10-26/unity.json",JsonUtility.ToJson(r,true));
                Debug.Log("R2 implementation checks passed: "+r.passed.Count);
            }
            finally {GameEvents.ClearAll();typeof(GameManager).GetProperty("I").SetValue(null,null);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(db);}
            const string output="Docs/Validation/R2-Implementation-2026-10-26/";
            File.WriteAllText(output+"q1.json",JsonUtility.ToJson(Q1Checks.Run(),true));
            File.WriteAllText(output+"r6.json",JsonUtility.ToJson(R6Checks.Run(),true));
            File.WriteAllText(output+"save.json",JsonUtility.ToJson(SaveCompatibilityChecks.RunData(),true));
        }
    }
}
