using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace PocketCaravan.EditorTools
{
    /// <summary>Reproduce literal D10 spread failures in isolated Edit mode. No runtime policy installed.</summary>
    public static class R2SafetyChecks
    {
        [Serializable] class Case {public string name;public int buyBefore,sellBefore,buyAfter,sellAfter,profit;public float stockBefore,stockAfter;public bool affordable,carryFits,sameQuoteSpreadHolds;}
        [Serializable] class Report {public string unity;public int checks;public Case liveNoHaggle,knownFutureHaggle;public bool runtimeApplied=false,playerSaveWritten=false;}
        public static void RunBatch()
        {
            if(Application.isPlaying||GameManager.I!=null)throw new InvalidOperationException("Use isolated Edit mode");
            var report=new Report{unity=Application.unityVersion};
            void Check(bool value,string name){if(!value)throw new Exception(name);report.checks++;}
            var live=Resources.Load<GameDatabase>("GameDatabase");var db=UnityEngine.Object.Instantiate(live);db.maxTownLevel=1;
            var go=new GameObject("R2 isolated quote experiment");go.SetActive(false);var gm=go.AddComponent<GameManager>();
            typeof(GameManager).GetProperty("I").SetValue(null,gm);
            var player=new PlayerState{money=200,origin=AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Origins/Origin_Venice.asset"),vehicle=live.startVehicle};
            void Set(string key,object value)=>typeof(GameManager).GetProperty(key).SetValue(gm,value);
            Set("Player",player);Set("Clock",new GameClock());Set("Db",db);
            var syntheticTown=ScriptableObject.CreateInstance<TownData>();var syntheticItem=ScriptableObject.CreateInstance<ItemData>();
            try
            {
                (int buy,int sell) Quote(MarketState m,ItemData item,bool haggle)
                {
                    int buy=Mathf.Max(1,Mathf.RoundToInt(m.BuyPrice(item)*.9f)),sell=m.SellPrice(item);
                    if(haggle)buy=Mathf.Max(1,Mathf.RoundToInt(buy*.9f));
                    return(Mathf.Max(buy,sell+1),sell);
                }
                Case Reproduce(string name,TownData town,ItemData item,float stock,bool haggle)
                {
                    var m=new MarketState(town,db);m.Import(new MarketState.Save{town=town.id,level=1,items=new System.Collections.Generic.List<string>{item.id},stocks=new System.Collections.Generic.List<float>{stock}},id=>item);
                    player.money=200;player.cargo.Clear();var before=Quote(m,item,haggle);
                    var result=new Case{name=name,buyBefore=before.buy,sellBefore=before.sell,stockBefore=m.Stock(item),affordable=player.money>=before.buy,carryFits=player.CanCarry(item)};
                    Check(result.affordable&&result.carryFits,"Cash/capacity witness");
                    player.money-=before.buy;player.cargo.Add(item,1,before.buy);m.RecordBuy(item,before.buy);
                    var after=Quote(m,item,haggle);result.buyAfter=after.buy;result.sellAfter=after.sell;
                    Check(player.cargo.Remove(item),"Witness has cargo");player.money+=after.sell;m.RecordSell(item,after.sell);
                    result.profit=player.money-200;result.stockAfter=m.Stock(item);result.sameQuoteSpreadHolds=before.buy>=before.sell+1&&after.buy>=after.sell+1;
                    Check(result.sameQuoteSpreadHolds&&result.profit>0,"Literal spread holds but cycle profits");
                    return result;
                }
                var ginseng=live.items.Find(i=>i.id=="ginseng");var naru=live.towns.Find(t=>t.id=="naru");var ferry=live.towns.Find(t=>t.id=="transit_ferry");
                Check(new MarketState(naru,db).SellStockDelta(ginseng)==1,"Real same-civilization identity");
                Check(new MarketState(ferry,db).SellStockDelta(ginseng)==.8f,"Real foreign Venice identity");
                report.liveNoHaggle=Reproduce("real assets: Venice/ginseng/transit_ferry, Revered, no haggle",ferry,ginseng,3,false);
                Check(report.liveNoHaggle.buyBefore==144&&report.liveNoHaggle.sellAfter==145,"Actual asset known answer");
                player.origin=live.startOrigin;
                syntheticTown.id="r2_counterexample";syntheticTown.defaultOptimal=3;syntheticItem.id="r2_counterexample";syntheticItem.basePrice=100;syntheticItem.weight=1;
                report.knownFutureHaggle=Reproduce("synthetic fixed stock: Revered plus buy-only max haggle",syntheticTown,syntheticItem,3,true);
                Check(report.knownFutureHaggle.profit==4&&report.knownFutureHaggle.stockAfter==3,"Restored-stock known answer");
                File.WriteAllText("Docs/Validation/R2-Safety-2026-10-25/unity.json",JsonUtility.ToJson(report,true));
                Debug.Log("R2 safety counterexamples reproduced: "+report.checks+" checks");
            }
            finally {GameEvents.ClearAll();typeof(GameManager).GetProperty("I").SetValue(null,null);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(db);UnityEngine.Object.DestroyImmediate(syntheticTown);UnityEngine.Object.DestroyImmediate(syntheticItem);}
        }
    }
}
