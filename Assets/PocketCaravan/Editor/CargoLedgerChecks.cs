using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace PocketCaravan.EditorTools
{
    public static class CargoLedgerChecks
    {
        [Serializable] public class Report { public List<string> passed=new List<string>(); }
        static void Check(Report r,string name,bool ok) { if(!ok)throw new Exception(name);r.passed.Add(name); }

        public static Report RunData()
        {
            var r=new Report();var world=ScriptableObject.CreateInstance<WorldIndex>();
            var item=ScriptableObject.CreateInstance<ItemData>();item.id="test_cargo";
            var towns=new List<TownData>();
            TownData Town(string id,string map)
            {
                var town=ScriptableObject.CreateInstance<TownData>();town.id=id;towns.Add(town);
                world.places.Add(new WorldIndex.Place{kind="market",id=id,mapId=map});return town;
            }
            var near=Town("near","B");var far=Town("far","C");var unknown=Town("unknown","A");var island=Town("island","D");
            world.maps.Add(new WorldIndex.MapInfo{mapId="A",neighbors=new List<string>{"B","missing"}});
            world.maps.Add(new WorldIndex.MapInfo{mapId="B",neighbors=new List<string>{"A","C"}});
            world.maps.Add(new WorldIndex.MapInfo{mapId="C",neighbors=new List<string>{"B"}});
            world.maps.Add(new WorldIndex.MapInfo{mapId="D"});
            var ledger=new TradeLedger();ledger.Import(new List<TradeLedger.Quote>{new TradeLedger.Quote{town="near",item=item.id,sell=12},new TradeLedger.Quote{town="far",item=item.id,sell=15},new TradeLedger.Quote{town="island",item=item.id,sell=12}});
            try
            {
                string saved=JsonUtility.ToJson(new Quotes{quotes=ledger.Export()});
                var rows=CargoSaleGuide.Observed(ledger,towns,item,world,"A");
                Check(r,"unobserved market does not leak prices",rows.Count==3 && !rows.Exists(c=>c.town==unknown));
                Check(r,"observed price order keeps expensive distant choice",rows[0].town==far && rows[0].sell==15);
                Check(r,"equal price prefers confirmed nearer connection",rows[1].town==near && rows[2].town==island);
                Check(r,"distance follows actual registered edges",rows[0].hops==2 && rows[0].nextMap=="B");
                Check(r,"disconnected quote retained without false route",rows[2].hops==-1 && rows[2].nextMap==null);
                Check(r,"same-map market has zero transitions",CargoSaleGuide.Distance(world,"B","B")==0);
                Check(r,"missing target not treated as reachable",CargoSaleGuide.Distance(world,"A","missing")==-1);
                Check(r,"missing current map has no invented distance",CargoSaleGuide.Distance(world,"missing","B")==-1);
                Check(r,"unknown world still permits observed price comparison",CargoSaleGuide.Observed(ledger,towns,item,null,null).TrueForAll(c=>c.hops==-1));
                towns.Add(near);Check(r,"duplicate database reference not repeated",CargoSaleGuide.Observed(ledger,towns,item,world,"A").Count==3);
                Check(r,"guide leaves saved quotes unchanged",saved==JsonUtility.ToJson(new Quotes{quotes=ledger.Export()}));
                item.questItem=true;Check(r,"delivery goods excluded",CargoSaleGuide.Observed(ledger,towns,item,world,"A").Count==0);
                item.questItem=false;Check(r,"empty ledger offers no phantom seller",CargoSaleGuide.Observed(new TradeLedger(),towns,item,world,"A").Count==0);
                return r;
            }
            finally{towns.Remove(near);foreach(var town in towns)UnityEngine.Object.DestroyImmediate(town);UnityEngine.Object.DestroyImmediate(item);UnityEngine.Object.DestroyImmediate(world);}
        }
        [Serializable] class Quotes { public List<TradeLedger.Quote> quotes; }

        public static Report RunPlay()
        {
#if !ENABLE_INPUT_SYSTEM
            throw new InvalidOperationException("Input System check requires enabled Input System");
#else
            var gm=GameManager.I;var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            if(!EditorApplication.isPlaying || gm==null || MapRoot.Current==null || (bool)typeof(GameManager).GetField("autosave",flags).GetValue(gm))throw new InvalidOperationException("Initialized non-start session required");
            var r=new Report();var ledgerUI=gm.GetComponent<TradeLedgerUI>();
            if(UIState.Blocking || ledgerUI.IsOpen)throw new InvalidOperationException("Close current modal before check");
            var town=gm.Db.towns.Find(t=>t.id=="gaegyeong");var market=gm.GetMarket(town);var snapshot=market.Export();var quotes=gm.Player.ledger.Export();
            int closed=UIState.ClosedFrame;var keyboard=Keyboard.current;Keyboard temporary=null;GameObject go=null;
            try
            {
                temporary=InputSystem.AddDevice<Keyboard>("IsolatedLedgerCheck");temporary.MakeCurrent();
                go=new GameObject("Isolated market ledger check");var shop=go.AddComponent<TownMarket>();shop.town=town;shop.Interact(null);
                typeof(TownMarket).GetField("openedFrame",flags).SetValue(shop,Time.frameCount-1);
                InputSystem.QueueStateEvent(temporary,new KeyboardState(Key.L));InputSystem.Update();
                Check(r,"synthetic L is received by real input reader",InputReader.LedgerPressed());
                var updateLedger=typeof(TradeLedgerUI).GetMethod("Update",flags);var updateMarket=typeof(TownMarket).GetMethod("Update",flags);
                updateLedger.Invoke(ledgerUI,null);
                Check(r,"ledger cannot overlap blocking market",!ledgerUI.IsOpen && UIState.Blocking);
                updateMarket.Invoke(shop,null);
                Check(r,"L transitions from market to ledger",ledgerUI.IsOpen && UIState.Blocking && !(bool)typeof(TownMarket).GetField("open",flags).GetValue(shop));
                updateLedger.Invoke(ledgerUI,null);
                Check(r,"same opening-frame L cannot immediately close ledger",ledgerUI.IsOpen && UIState.Blocking);
                typeof(TradeLedgerUI).GetField("openedFrame",flags).SetValue(ledgerUI,Time.frameCount-1);
                updateLedger.Invoke(ledgerUI,null);
                Check(r,"later L closes ledger and releases movement",!ledgerUI.IsOpen && !UIState.Blocking && UIState.JustClosed);
                updateLedger.Invoke(ledgerUI,null);
                Check(r,"closing input cannot reopen ledger in same frame",!ledgerUI.IsOpen);
                return r;
            }
            finally
            {
                ledgerUI.Close();if(go!=null)UnityEngine.Object.DestroyImmediate(go);
                if(temporary!=null)InputSystem.RemoveDevice(temporary);if(keyboard!=null && keyboard.added)keyboard.MakeCurrent();
                market.Import(snapshot,gm.ItemById);gm.Player.ledger.Import(quotes);UIState.Blocking=false;UIState.ClosedFrame=closed;Notify.Clear();
            }
#endif
        }
    }
}
